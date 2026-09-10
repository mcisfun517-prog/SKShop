#nullable disable

using System;
using System.Collections.Generic;
using Terraria;
using TShockAPI;

namespace SKShop
{
    public static class ShopHelper
    {
        private const int CopperSlot = 50;
        private const int SilverSlot = 51;
        private const int GoldSlot = 52;
        private const int PlatinumSlot = 53;

        private static bool IsCoin(int type)
        {
            return type == 71 || type == 72 || type == 73 || type == 74;
        }

        
        public static long GetPlayerCopperCoins(Player player)
        {
            if (player?.inventory == null) return 0;
            long total = 0;
            foreach (Item item in player.inventory)
            {
                if (item == null || item.IsAir || item.stack <= 0) continue;
                switch (item.type)
                {
                    case 71: total += item.stack; break;
                    case 72: total += (long)item.stack * 100; break;
                    case 73: total += (long)item.stack * 10000; break;
                    case 74: total += (long)item.stack * 1000000; break;
                }
            }
            return total;
        }

        public static void SetPlayerCopperCoins(TSPlayer plr, long targetCopper)
        {
            if (plr?.TPlayer == null) return;
            if (targetCopper < 0) targetCopper = 0;
            Player player = plr.TPlayer;
            if (player.inventory == null) return;

            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item item = player.inventory[i];
                if (item != null && !item.IsAir && IsCoin(item.type))
                {
                    player.inventory[i] = new Item();
                    NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, null, plr.Index, i);
                }
            }

            int platinum = (int)(targetCopper / 1000000L);
            long remaining = targetCopper % 1000000L;
            int gold = (int)(remaining / 10000L);
            remaining %= 10000L;
            int silver = (int)(remaining / 100L);
            int copper = (int)(remaining % 100L);

            SetCoinSlot(plr, PlatinumSlot, 74, platinum);
            SetCoinSlot(plr, GoldSlot, 73, gold);
            SetCoinSlot(plr, SilverSlot, 72, silver);
            SetCoinSlot(plr, CopperSlot, 71, copper);
        }

        private static void SetCoinSlot(TSPlayer plr, int slot, int coinType, int amount)
        {
            if (plr?.TPlayer == null) return;
            Player player = plr.TPlayer;
            if (player.inventory == null || slot < 0 || slot >= player.inventory.Length) return;

            if (amount <= 0)
                player.inventory[slot] = new Item();
            else
            {
                Item coin = new Item();
                coin.SetDefaults(coinType);
                coin.stack = amount;
                player.inventory[slot] = coin;
            }
            NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, null, plr.Index, slot);
        }

        public static void RemoveCopperCoins(TSPlayer plr, long copperAmount)
        {
            if (plr?.TPlayer == null || copperAmount <= 0) return;
            long current = GetPlayerCopperCoins(plr.TPlayer);
            long target = current - copperAmount;
            if (target < 0) target = 0;
            SetPlayerCopperCoins(plr, target);
        }

        public static void RestoreSoldItems(TSPlayer plr, List<(int type, int soldStack)> soldItems)
        {
            if (plr?.TPlayer == null || soldItems == null || soldItems.Count == 0) return;
            Player player = plr.TPlayer;
            if (player.inventory == null) return;

            foreach (var sold in soldItems)
            {
                int type = sold.type;
                int remaining = sold.soldStack;
                if (type <= 0 || remaining <= 0) continue;

                for (int slot = 0; slot < player.inventory.Length && remaining > 0; slot++)
                {
                    Item item = player.inventory[slot];
                    if (item == null || item.IsAir || item.type != type) continue;
                    int maxStack = item.maxStack;
                    if (maxStack <= item.stack) continue;
                    int add = Math.Min(remaining, maxStack - item.stack);
                    if (add <= 0) continue;
                    item.stack += add;
                    remaining -= add;
                    NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, null, plr.Index, slot);
                }

                for (int slot = 0; slot < player.inventory.Length && remaining > 0; slot++)
                {
                    Item item = player.inventory[slot];
                    if (item != null && !item.IsAir) continue;
                    Item newItem = new Item();
                    newItem.SetDefaults(type);
                    int amount = Math.Min(remaining, newItem.maxStack);
                    newItem.stack = amount;
                    player.inventory[slot] = newItem;
                    remaining -= amount;
                    NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, null, plr.Index, slot);
                }
            }
        }

      
        private static readonly Dictionary<int, int> _lastUsedItemType = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> _lastUsedItemTime = new Dictionary<int, int>();
        private static readonly bool[] _lastControlUseItem = new bool[256];
        private const int UseItemWindow = 10;

        private static readonly Dictionary<int, DateTime> _lastProcessTime = new Dictionary<int, DateTime>();

        public static void RecordItemUse(TSPlayer plr, Player player)
        {
            if (plr == null || player == null) return;
            int index = plr.Index;
            if (index < 0 || index >= _lastControlUseItem.Length) return;

            bool currentUse = player.controlUseItem;
            bool lastUse = _lastControlUseItem[index];

            if (currentUse && !lastUse)
            {
                if (player.inventory != null && player.selectedItem >= 0 && player.selectedItem < player.inventory.Length)
                {
                    Item selectedItem = player.inventory[player.selectedItem];
                    if (selectedItem != null)
                    {
                        int itemType = selectedItem.type;
                        if (SKPlugin.CustomShopItemIds.Contains((short)itemType))
                        {
                            _lastUsedItemType[index] = itemType;
                            _lastUsedItemTime[index] = (int)Main.GameUpdateCount;
                        }
                    }
                }
            }
            _lastControlUseItem[index] = currentUse;
        }

        public static bool IsRecentlyUsedCustomItem(int index, List<(int type, int stack)> soldItems)
        {
            if (!_lastUsedItemType.TryGetValue(index, out int usedType)) return false;
            if (!_lastUsedItemTime.TryGetValue(index, out int usedTime)) return false;
            int elapsed = (int)Main.GameUpdateCount - usedTime;
            if (elapsed < 0 || elapsed > UseItemWindow) return false;
            foreach (var sold in soldItems) if (sold.type == usedType) return true;
            return false;
        }

        
        public static Dictionary<int, int> GetAllCustomItemTotals(Player player)
        {
            var totals = new Dictionary<int, int>();
            if (player == null) return totals;

            void ProcessItems(Item[] items)
            {
                if (items == null) return;
                foreach (Item item in items)
                {
                    if (item == null || item.IsAir || item.stack <= 0) continue;
                    int type = item.type;
                    if (!SKPlugin.CustomShopItemIds.Contains((short)type)) continue;
                    if (totals.TryGetValue(type, out int oldCount))
                        totals[type] = oldCount + item.stack;
                    else
                        totals[type] = item.stack;
                }
            }

            if (player.inventory != null)
                ProcessItems(player.inventory);

            ProcessItems(player.armor);
            ProcessItems(player.dye);
            ProcessItems(player.miscEquips);
            ProcessItems(player.miscDyes);

            try
            {
                Item trash = player.trashItem;
                if (trash != null && !trash.IsAir && trash.stack > 0)
                {
                    int type = trash.type;
                    if (SKPlugin.CustomShopItemIds.Contains((short)type))
                    {
                        if (totals.TryGetValue(type, out int oldCount))
                            totals[type] = oldCount + trash.stack;
                        else
                            totals[type] = trash.stack;
                    }
                }
            }
            catch { }

            ProcessItems(player.bank?.item);
            ProcessItems(player.bank2?.item);
            ProcessItems(player.bank3?.item);
            ProcessItems(player.bank4?.item);

            return totals;
        }

     
        public static int GetConfiguredItemPrice(int itemType)
        {
            if (SKPlugin.CustomItemPriceCopper.TryGetValue(itemType, out int price))
                return Math.Max(0, price);
            return 0;
        }

      
        public static long GetSaleIncomeFromPricePool(TSPlayer plr, List<(int type, int stack)> soldItems)
        {
            if (plr == null || soldItems == null) return 0;
            long total = 0;

            int percent = SKPlugin.Config.SellReturnPercent;
            if (percent < 0) percent = 0;

            foreach (var sold in soldItems)
            {
                int type = sold.type, stack = sold.stack;
                if (stack <= 0) continue;

                
                if (SKPlugin.CustomItemSellPriceCopper.TryGetValue(type, out var val) && val.HasValue)
                {
                    total += (long)val.Value * stack;
                    continue;
                }

                
                int configured = GetConfiguredItemPrice(type);
                total += (int)((long)configured * percent / 100) * stack;
            }
            return total;
        }

     
        public static bool AreTotalsEqual(Dictionary<int, int> a, Dictionary<int, int> b)
        {
            if (a == null || b == null) return a == b;
            if (a.Count != b.Count) return false;
            foreach (var kvp in a)
            {
                if (!b.TryGetValue(kvp.Key, out int value)) return false;
                if (value != kvp.Value) return false;
            }
            return true;
        }

        public static List<(int type, int stack)> GetSoldItems(Dictionary<int, int> beforeTotals, Dictionary<int, int> afterTotals)
        {
            var result = new List<(int type, int stack)>();
            if (beforeTotals == null) beforeTotals = new Dictionary<int, int>();
            if (afterTotals == null) afterTotals = new Dictionary<int, int>();
            var allTypes = new HashSet<int>(beforeTotals.Keys);
            foreach (int key in afterTotals.Keys) allTypes.Add(key);
            foreach (int type in allTypes)
            {
                int oldCount = beforeTotals.TryGetValue(type, out var oldVal) ? oldVal : 0;
                int newCount = afterTotals.TryGetValue(type, out var newVal) ? newVal : 0;
                if (newCount < oldCount) result.Add((type, oldCount - newCount));
            }
            return result;
        }

        
        public static void Cleanup()
        {
            _lastUsedItemType.Clear();
            _lastUsedItemTime.Clear();
            Array.Clear(_lastControlUseItem, 0, _lastControlUseItem.Length);
            _lastProcessTime.Clear();
        }

      
        public static bool CheckCooldown(int playerIndex, int cooldownMs, out DateTime lastTime)
        {
            lastTime = default;
            if (cooldownMs <= 0) return false;
            if (_lastProcessTime.TryGetValue(playerIndex, out var time))
            {
                if ((DateTime.UtcNow - time).TotalMilliseconds < cooldownMs)
                    return true;
            }
            return false;
        }

        public static void UpdateCooldown(int playerIndex, int cooldownMs)
        {
            if (cooldownMs > 0)
                _lastProcessTime[playerIndex] = DateTime.UtcNow;
        }
    }
}