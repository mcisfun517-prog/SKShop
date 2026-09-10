#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;

namespace SKShop
{
    public static class UniversalPriceManager
    {
        private class PendingSell
        {
            public Dictionary<int, int> LastTotals = new Dictionary<int, int>();
            public long LastMoney;
            public DateTime StartTime;
        }

        private static readonly Dictionary<int, Dictionary<int, int>> _itemSnapshot =
            new Dictionary<int, Dictionary<int, int>>();

        private static readonly Dictionary<int, long> _moneySnapshot =
            new Dictionary<int, long>();

        private static readonly Dictionary<int, PendingSell> _pendingSells =
            new Dictionary<int, PendingSell>();

        private static readonly bool[] _lastControlUseItemForUniversal = new bool[256];
        private static readonly Dictionary<int, int> _lastUsedItemTypeForUniversal = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> _lastUsedItemTimeForUniversal = new Dictionary<int, int>();
        private const int UseItemWindow = 10;

        private const int PendingTimeoutMs = 1000;

        public static void Cleanup()
        {
            _itemSnapshot.Clear();
            _moneySnapshot.Clear();
            _pendingSells.Clear();
            _lastUsedItemTypeForUniversal.Clear();
            _lastUsedItemTimeForUniversal.Clear();
            Array.Clear(_lastControlUseItemForUniversal, 0, _lastControlUseItemForUniversal.Length);
        }

        public static void HandleItemUse(TSPlayer plr, Player player)
        {
            if (plr == null || player == null) return;
            int index = plr.Index;
            if (index < 0 || index >= _lastControlUseItemForUniversal.Length) return;

            bool currentUse = player.controlUseItem;
            bool lastUse = _lastControlUseItemForUniversal[index];

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
                            _lastUsedItemTypeForUniversal[index] = itemType;
                            _lastUsedItemTimeForUniversal[index] = (int)Main.GameUpdateCount;
                        }
                    }
                }
            }
            _lastControlUseItemForUniversal[index] = currentUse;
        }

        private static bool IsRecentlyUsedCustomItem(int index, List<(int type, int stack)> soldItems)
        {
            if (!_lastUsedItemTypeForUniversal.TryGetValue(index, out int usedType)) return false;
            if (!_lastUsedItemTimeForUniversal.TryGetValue(index, out int usedTime)) return false;
            int elapsed = (int)Main.GameUpdateCount - usedTime;
            if (elapsed < 0 || elapsed > UseItemWindow) return false;
            foreach (var sold in soldItems) if (sold.type == usedType) return true;
            return false;
        }

        private static void ClearSnapshot(int index)
        {
            _itemSnapshot.Remove(index);
            _moneySnapshot.Remove(index);
            _pendingSells.Remove(index);
            _lastUsedItemTypeForUniversal.Remove(index);
            _lastUsedItemTimeForUniversal.Remove(index);
        }

        public static void MonitorAllNPCs(TSPlayer plr)
        {
            if (plr == null) return;
            int index = plr.Index;
            Player player = plr.TPlayer;
            if (player == null || player.inventory == null) return;

            int talkNpc = player.talkNPC;
            if (talkNpc < 0 || talkNpc >= Main.npc.Length)
            {
                ClearSnapshot(index);
                return;
            }

            NPC npc = Main.npc[talkNpc];
            if (npc == null || !npc.active || !npc.townNPC)
            {
                ClearSnapshot(index);
                return;
            }

            if (npc.type == 453)
                return;

            var currentTotals = ShopHelper.GetAllCustomItemTotals(player);
            long currentMoney = ShopHelper.GetPlayerCopperCoins(player);

            int cooldownMs = SKPlugin.Config.TransactionCooldownMs;
            if (ShopHelper.CheckCooldown(index, cooldownMs, out _))
                return;


            if (_pendingSells.TryGetValue(index, out var pending) && pending != null)
            {
                var curTotals = ShopHelper.GetAllCustomItemTotals(player);
                long curMoney = ShopHelper.GetPlayerCopperCoins(player);

                bool itemsChanged = !ShopHelper.AreTotalsEqual(curTotals, pending.LastTotals);
                long moneyDelta = curMoney - pending.LastMoney;

                
                if (itemsChanged && moneyDelta > 0)
                {
                    var soldItemsNow = ShopHelper.GetSoldItems(pending.LastTotals, curTotals);
                    if (soldItemsNow.Count > 0)
                    {
                        long expectedIncome = ShopHelper.GetSaleIncomeFromPricePool(plr, soldItemsNow);
                        long targetMoney = pending.LastMoney + expectedIncome;
                        if (targetMoney < 0) targetMoney = 0;
                        ShopHelper.SetPlayerCopperCoins(plr, targetMoney);
                    }
                    _itemSnapshot[index] = ShopHelper.GetAllCustomItemTotals(player);
                    _moneySnapshot[index] = ShopHelper.GetPlayerCopperCoins(player);
                    _pendingSells.Remove(index);
                    _lastUsedItemTypeForUniversal.Remove(index);
                    _lastUsedItemTimeForUniversal.Remove(index);
                    ShopHelper.UpdateCooldown(index, cooldownMs);
                    return;
                }

                
                if (itemsChanged && moneyDelta == 0)
                {
                    _itemSnapshot[index] = curTotals;
                    _moneySnapshot[index] = curMoney;
                    _pendingSells.Remove(index);
                    _lastUsedItemTypeForUniversal.Remove(index);
                    _lastUsedItemTimeForUniversal.Remove(index);
                    ShopHelper.UpdateCooldown(index, cooldownMs);
                    return;
                }

                
                if ((DateTime.UtcNow - pending.StartTime).TotalMilliseconds > PendingTimeoutMs)
                {
                    if (!itemsChanged && moneyDelta > 0)
                    {
                        ShopHelper.SetPlayerCopperCoins(plr, pending.LastMoney);
                        _moneySnapshot[index] = pending.LastMoney;
                    }
                    else
                    {
                        _itemSnapshot[index] = curTotals;
                        _moneySnapshot[index] = curMoney;
                    }
                    _pendingSells.Remove(index);
                }
                return;
            }

         
            if (!_itemSnapshot.TryGetValue(index, out var lastTotals))
            {
                _itemSnapshot[index] = currentTotals;
                _moneySnapshot[index] = currentMoney;
                return;
            }
            if (!_moneySnapshot.TryGetValue(index, out var lastMoney))
            {
                _moneySnapshot[index] = currentMoney;
                return;
            }

            var allTypes = new HashSet<int>(lastTotals.Keys);
            foreach (int key in currentTotals.Keys) allTypes.Add(key);
            var soldItems = new List<(int type, int stack)>();
            var boughtItems = new List<(int type, int stack)>();

            foreach (int type in allTypes)
            {
                int oldTotal = lastTotals.TryGetValue(type, out var oldVal) ? oldVal : 0;
                int newTotal = currentTotals.TryGetValue(type, out var newVal) ? newVal : 0;
                if (newTotal > oldTotal) boughtItems.Add((type, newTotal - oldTotal));
                else if (newTotal < oldTotal) soldItems.Add((type, oldTotal - newTotal));
            }

            
            if (soldItems.Count == 0 && boughtItems.Count == 0)
            {
                if (currentMoney > lastMoney)
                {
                    _pendingSells[index] = new PendingSell
                    {
                        LastTotals = new Dictionary<int, int>(lastTotals),
                        LastMoney = lastMoney,
                        StartTime = DateTime.UtcNow
                    };
                    return;
                }
                if (currentMoney < lastMoney)
                {
                    return;
                }
                return;
            }

            
            if (soldItems.Count > 0 && currentMoney == lastMoney)
            {
                bool isConsume = IsRecentlyUsedCustomItem(index, soldItems);
                if (isConsume)
                {
                    _itemSnapshot[index] = currentTotals;
                    _moneySnapshot[index] = currentMoney;
                    _lastUsedItemTypeForUniversal.Remove(index);
                    _lastUsedItemTimeForUniversal.Remove(index);
                    ShopHelper.UpdateCooldown(index, cooldownMs);
                    return;
                }
                _pendingSells[index] = new PendingSell
                {
                    LastTotals = new Dictionary<int, int>(lastTotals),
                    LastMoney = lastMoney,
                    StartTime = DateTime.UtcNow
                };
                return;
            }

            
            if (soldItems.Count > 0 && currentMoney > lastMoney)
            {
                long expectedIncome = ShopHelper.GetSaleIncomeFromPricePool(plr, soldItems);
                long targetMoney = lastMoney + expectedIncome;
                if (targetMoney < 0) targetMoney = 0;
                ShopHelper.SetPlayerCopperCoins(plr, targetMoney);

                _itemSnapshot[index] = ShopHelper.GetAllCustomItemTotals(player);
                _moneySnapshot[index] = ShopHelper.GetPlayerCopperCoins(player);
                _lastUsedItemTypeForUniversal.Remove(index);
                _lastUsedItemTimeForUniversal.Remove(index);
                ShopHelper.UpdateCooldown(index, cooldownMs);
                return;
            }

           
            if (boughtItems.Count > 0)
            {
                _itemSnapshot[index] = new Dictionary<int, int>(currentTotals);
                _moneySnapshot[index] = currentMoney;
                ShopHelper.UpdateCooldown(index, cooldownMs);
                return;
            }
        }
    }
}