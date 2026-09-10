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
            public List<(int type, int stack)> RemovedItems = new List<(int type, int stack)>();
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

           
            if (_pendingSells.TryGetValue(index, out var pendingSell) && pendingSell != null)
            {
                var currentTotalsNow = ShopHelper.GetAllCustomItemTotals(player);
                long currentMoneyNow = ShopHelper.GetPlayerCopperCoins(player);

                if (ShopHelper.AreTotalsEqual(currentTotalsNow, pendingSell.LastTotals))
                {
                    _itemSnapshot[index] = currentTotalsNow;
                    _moneySnapshot[index] = currentMoneyNow;
                    _pendingSells.Remove(index);
                    return;
                }

                if (currentMoneyNow > pendingSell.LastMoney)
                {
                    var soldItemsNow = ShopHelper.GetSoldItems(pendingSell.LastTotals, currentTotalsNow);
                    if (soldItemsNow.Count > 0)
                    {
                        long expectedIncome = ShopHelper.GetSaleIncomeFromPricePool(plr, soldItemsNow);
                        long targetMoney = pendingSell.LastMoney + expectedIncome;
                        if (targetMoney < 0) targetMoney = 0;
                        ShopHelper.SetPlayerCopperCoins(plr, targetMoney);
                    }
                    _itemSnapshot[index] = ShopHelper.GetAllCustomItemTotals(player);
                    _moneySnapshot[index] = ShopHelper.GetPlayerCopperCoins(player);
                    _pendingSells.Remove(index);
                    ShopHelper.SyncPricePool(plr);
                    _lastUsedItemTypeForUniversal.Remove(index);
                    _lastUsedItemTimeForUniversal.Remove(index);
                    ShopHelper.UpdateCooldown(index, cooldownMs);
                    return;
                }
                return;
            }

           
            if (!_itemSnapshot.TryGetValue(index, out var lastTotals))
            {
                _itemSnapshot[index] = currentTotals;
                _moneySnapshot[index] = currentMoney;
                ShopHelper.InitializePricePool(plr);
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
                  
                    ShopHelper.SetPlayerCopperCoins(plr, lastMoney);
                    _moneySnapshot[index] = lastMoney;
                }
                else if (currentMoney == lastMoney)
                {
                    
                }
               
                return;
            }

            
            if (soldItems.Count > 0 && currentMoney == lastMoney)
            {
                bool isConsume = IsRecentlyUsedCustomItem(index, soldItems);
                if (isConsume)
                {
                    ShopHelper.ConsumePriceRecords(plr, soldItems);
                    _itemSnapshot[index] = currentTotals;
                    _moneySnapshot[index] = currentMoney;
                    _lastUsedItemTypeForUniversal.Remove(index);
                    _lastUsedItemTimeForUniversal.Remove(index);
                    ShopHelper.SyncPricePool(plr);
                    ShopHelper.UpdateCooldown(index, cooldownMs);
                    return;
                }
                else
                {
                    _pendingSells[index] = new PendingSell
                    {
                        LastTotals = new Dictionary<int, int>(lastTotals),
                        LastMoney = lastMoney,
                        RemovedItems = new List<(int type, int stack)>(soldItems)
                    };
                    return;
                }
            }

         
            if (soldItems.Count > 0 && currentMoney > lastMoney)
            {
                long expectedIncome = ShopHelper.GetSaleIncomeFromPricePool(plr, soldItems);
                long targetMoney = lastMoney + expectedIncome;
                if (targetMoney < 0) targetMoney = 0;
                ShopHelper.SetPlayerCopperCoins(plr, targetMoney);

                if (boughtItems.Count > 0)
                    ShopHelper.RecordPurchasePrices(plr, boughtItems);

                _itemSnapshot[index] = ShopHelper.GetAllCustomItemTotals(player);
                _moneySnapshot[index] = ShopHelper.GetPlayerCopperCoins(player);
                _lastUsedItemTypeForUniversal.Remove(index);
                _lastUsedItemTimeForUniversal.Remove(index);
                ShopHelper.SyncPricePool(plr);
                ShopHelper.UpdateCooldown(index, cooldownMs);
                return;
            }

            
            if (boughtItems.Count > 0)
            {
                ShopHelper.RecordPurchasePrices(plr, boughtItems);
                _itemSnapshot[index] = new Dictionary<int, int>(currentTotals);
                _moneySnapshot[index] = currentMoney;
                ShopHelper.SyncPricePool(plr);
                ShopHelper.UpdateCooldown(index, cooldownMs);
                return;
            }
        }
    }
}