#nullable disable

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;

namespace SKShop
{
    public static class SkeletonShopManager
    {
        private class PlayerShopSession
        {
            public SKConfig.Shop Shop { get; set; }
            public List<SKConfig.SKItem> AllItems { get; set; } = new List<SKConfig.SKItem>();
            public int CurrentPage { get; set; }
            public int TotalPages { get; set; }
            public int NpcIndex { get; set; }
            public bool IsActive { get; set; } = true;
            public Task UpdateLoopTask { get; set; }
        }

        private class PendingSell
        {
            public Dictionary<int, int> LastTotals = new Dictionary<int, int>();
            public long LastMoney;
            public List<(int type, int stack)> RemovedItems = new List<(int type, int stack)>();
        }

        private static readonly Dictionary<int, PlayerShopSession> _sessions = new Dictionary<int, PlayerShopSession>();

        private static readonly Dictionary<int, Dictionary<int, int>> _skeletonItemSnapshot =
            new Dictionary<int, Dictionary<int, int>>();

        private static readonly Dictionary<int, long> _skeletonMoneySnapshot =
            new Dictionary<int, long>();

        private static readonly Dictionary<int, PendingSell> _pendingSells =
            new Dictionary<int, PendingSell>();

        private static readonly bool[] _lastControlUseItemForSkeleton = new bool[256];
        private static readonly Dictionary<int, int> _lastUsedItemTypeForSkeleton = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> _lastUsedItemTimeForSkeleton = new Dictionary<int, int>();
        private const int UseItemWindow = 10;

        public static void CleanupSessions()
        {
            foreach (var pair in _sessions.ToList()) { try { pair.Value.IsActive = false; } catch { } }
            _sessions.Clear();
            _skeletonItemSnapshot.Clear();
            _skeletonMoneySnapshot.Clear();
            _pendingSells.Clear();
            _lastUsedItemTypeForSkeleton.Clear();
            _lastUsedItemTimeForSkeleton.Clear();
            Array.Clear(_lastControlUseItemForSkeleton, 0, _lastControlUseItemForSkeleton.Length);
        }

        public static void HandleItemUse(TSPlayer plr, Player player)
        {
            if (plr == null || player == null) return;
            int index = plr.Index;
            if (index < 0 || index >= _lastControlUseItemForSkeleton.Length) return;

            bool currentUse = player.controlUseItem;
            bool lastUse = _lastControlUseItemForSkeleton[index];

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
                            _lastUsedItemTypeForSkeleton[index] = itemType;
                            _lastUsedItemTimeForSkeleton[index] = (int)Main.GameUpdateCount;
                        }

                        if (_sessions.TryGetValue(index, out var session) && session != null && session.IsActive)
                        {
                            if (itemType == SKPlugin.Config.NextPageItemID)
                            {
                                if (session.CurrentPage < session.TotalPages - 1)
                                {
                                    session.CurrentPage++;
                                    SendShopPage(plr, session);
                                }
                            }
                            else if (itemType == SKPlugin.Config.PrevPageItemID)
                            {
                                if (session.CurrentPage > 0)
                                {
                                    session.CurrentPage--;
                                    SendShopPage(plr, session);
                                }
                            }
                        }
                    }
                }
            }
            _lastControlUseItemForSkeleton[index] = currentUse;
        }

        public static bool HandleNpcTalk(GetDataEventArgs args, TSPlayer plr, int npcIndex)
        {
            if (!SKPlugin.Config.EnableCustomShop) return false;
            if (plr == null) return false;
            if (npcIndex < 0 || npcIndex >= Main.npc.Length) return false;
            NPC npc = Main.npc[npcIndex];
            if (npc == null || !npc.active || npc.type != 453) return false;

            var availableShop = SKPlugin.AviliableShops.FirstOrDefault(s =>
                (s.Groups == null || s.Groups.Count == 0 || s.Groups.Contains(plr.Group.Name)) && s.Enabled);

            bool hasShop = SKPlugin.AviliableShops.Any(s =>
                (s.Groups == null || s.Groups.Count == 0 || s.Groups.Contains(plr.Group.Name)) && s.Enabled);

            if (!hasShop) return false;
            var shop = availableShop;

            if (_sessions.TryGetValue(plr.Index, out var existingSession) && existingSession != null && existingSession.IsActive && existingSession.NpcIndex == npcIndex)
            {
                var newItems = shop.Items.Where(item => CheckConditions(plr, item.Conditions)).ToList();
                int newTotalPages;
                GetPageItems(newItems, existingSession.CurrentPage, out newTotalPages);
                if (newTotalPages <= 0) newTotalPages = 1;
                existingSession.AllItems = newItems;
                existingSession.TotalPages = newTotalPages;
                if (existingSession.CurrentPage >= newTotalPages) existingSession.CurrentPage = newTotalPages - 1;
                if (existingSession.CurrentPage < 0) existingSession.CurrentPage = 0;
                SendShopPage(plr, existingSession);
                return true;
            }

            if (_sessions.TryGetValue(plr.Index, out var oldSession))
            {
                if (oldSession != null) oldSession.IsActive = false;
                _sessions.Remove(plr.Index);
            }
            _pendingSells.Remove(plr.Index);
            ShopHelper.InitializePricePool(plr);
            InitializeSkeletonSnapshot(plr);

            plr.TPlayer.talkNPC = npcIndex;

            NetMessage.SendData((int)PacketTypes.NpcTalk, -1, plr.Index,
                NetworkText.FromLiteral(npc.GivenName ?? npc.TypeName), npcIndex, 0, 0, 0, 0);

            var allItems = shop.Items.Where(item => CheckConditions(plr, item.Conditions)).ToList();
            int totalPages;
            GetPageItems(allItems, 0, out totalPages);
            if (totalPages <= 0) totalPages = 1;

            var session = new PlayerShopSession
            {
                Shop = shop,
                AllItems = allItems,
                CurrentPage = 0,
                TotalPages = totalPages,
                NpcIndex = npcIndex,
                IsActive = true
            };
            _sessions[plr.Index] = session;

            if (shop.OpenMessage != null && shop.OpenMessage.Count > 0)
            {
                foreach (var msg in shop.OpenMessage) plr.SendMessage(msg, Color.Yellow);
            }

         
            SendShopPage(plr, session);


            var retrySession = session;
            int retryNpcIndex = npcIndex;
            _ = Task.Run(async () =>
            {
                int[] resendDelaysMs = { 30, 80, 160, 320, 640, 1280 };
                foreach (int delay in resendDelaysMs)
                {
                    await Task.Delay(delay);
                    if (!plr.Active || plr.Dead || !retrySession.IsActive) return;
                    if (plr.TPlayer.talkNPC != retryNpcIndex) return;
                    try { SendShopPage(plr, retrySession); } catch { }
                }
            });

     
            int interval = 500;
            var currentSession = session;
            session.UpdateLoopTask = Task.Run(async () =>
            {
                int targetNpc = npcIndex;
                while (plr.Active && !plr.Dead && currentSession.IsActive && plr.TPlayer.talkNPC == targetNpc)
                {
                    await Task.Delay(interval);
                    if (!plr.Active || plr.Dead || !currentSession.IsActive || plr.TPlayer.talkNPC != targetNpc) break;
                    try
                    {
                        var refreshedItems = shop.Items.Where(item => CheckConditions(plr, item.Conditions)).ToList();
                        int newTotalPages;
                        GetPageItems(refreshedItems, currentSession.CurrentPage, out newTotalPages);
                        if (newTotalPages <= 0) newTotalPages = 1;
                        currentSession.AllItems = refreshedItems;
                        currentSession.TotalPages = newTotalPages;
                        if (currentSession.CurrentPage >= newTotalPages) currentSession.CurrentPage = newTotalPages - 1;
                        if (currentSession.CurrentPage < 0) currentSession.CurrentPage = 0;
                        SendShopPage(plr, currentSession);
                    }
                    catch { break; }
                }
                if (_sessions.TryGetValue(plr.Index, out var existing) && existing == currentSession)
                {
                    currentSession.IsActive = false;
                    _sessions.Remove(plr.Index);
                    ClearSnapshot(plr.Index);
                }
            });

            return true;
        }

        public static void MonitorSkeletonTrading(TSPlayer plr)
        {
            if (plr == null) return;
            int index = plr.Index;
            Player player = plr.TPlayer;
            if (player == null || player.inventory == null) return;

            if (player.talkNPC < 0 || player.talkNPC >= Main.npc.Length ||
                !Main.npc[player.talkNPC].active || Main.npc[player.talkNPC].type != 453)
            {
                ClearSnapshot(index);
                return;
            }

            if (!_sessions.TryGetValue(index, out var session) || session == null || !session.IsActive)
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
                    _skeletonItemSnapshot[index] = currentTotalsNow;
                    _skeletonMoneySnapshot[index] = currentMoneyNow;
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
                    _skeletonItemSnapshot[index] = ShopHelper.GetAllCustomItemTotals(player);
                    _skeletonMoneySnapshot[index] = ShopHelper.GetPlayerCopperCoins(player);
                    _pendingSells.Remove(index);
                    ShopHelper.SyncPricePool(plr);
                    _lastUsedItemTypeForSkeleton.Remove(index);
                    _lastUsedItemTimeForSkeleton.Remove(index);
                    ShopHelper.UpdateCooldown(index, cooldownMs);
                    return;
                }
                return;
            }

            if (!_skeletonItemSnapshot.TryGetValue(index, out var lastTotals))
            {
                _skeletonItemSnapshot[index] = currentTotals;
                _skeletonMoneySnapshot[index] = currentMoney;
                ShopHelper.InitializePricePool(plr);
                return;
            }
            if (!_skeletonMoneySnapshot.TryGetValue(index, out var lastMoney))
            {
                _skeletonMoneySnapshot[index] = currentMoney;
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
                    _skeletonMoneySnapshot[index] = lastMoney;
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
                    _skeletonItemSnapshot[index] = currentTotals;
                    _skeletonMoneySnapshot[index] = currentMoney;
                    _lastUsedItemTypeForSkeleton.Remove(index);
                    _lastUsedItemTimeForSkeleton.Remove(index);
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

                _skeletonItemSnapshot[index] = ShopHelper.GetAllCustomItemTotals(player);
                _skeletonMoneySnapshot[index] = ShopHelper.GetPlayerCopperCoins(player);
                _lastUsedItemTypeForSkeleton.Remove(index);
                _lastUsedItemTimeForSkeleton.Remove(index);
                ShopHelper.SyncPricePool(plr);
                ShopHelper.UpdateCooldown(index, cooldownMs);
                return;
            }

            
            if (boughtItems.Count > 0)
            {
                ShopHelper.RecordPurchasePrices(plr, boughtItems);
                _skeletonItemSnapshot[index] = new Dictionary<int, int>(currentTotals);
                _skeletonMoneySnapshot[index] = currentMoney;
                ShopHelper.SyncPricePool(plr);
                ShopHelper.UpdateCooldown(index, cooldownMs);
                return;
            }
        }

        private static bool IsRecentlyUsedCustomItem(int index, List<(int type, int stack)> soldItems)
        {
            if (!_lastUsedItemTypeForSkeleton.TryGetValue(index, out int usedType)) return false;
            if (!_lastUsedItemTimeForSkeleton.TryGetValue(index, out int usedTime)) return false;
            int elapsed = (int)Main.GameUpdateCount - usedTime;
            if (elapsed < 0 || elapsed > UseItemWindow) return false;
            foreach (var sold in soldItems) if (sold.type == usedType) return true;
            return false;
        }

        private static void InitializeSkeletonSnapshot(TSPlayer plr)
        {
            if (plr == null) return;
            int index = plr.Index;
            Player player = plr.TPlayer;
            if (player == null) return;
            _skeletonItemSnapshot[index] = ShopHelper.GetAllCustomItemTotals(player);
            _skeletonMoneySnapshot[index] = ShopHelper.GetPlayerCopperCoins(player);
            _pendingSells.Remove(index);
        }

        private static void ClearSnapshot(int index)
        {
            _skeletonItemSnapshot.Remove(index);
            _skeletonMoneySnapshot.Remove(index);
            _pendingSells.Remove(index);
            _lastUsedItemTypeForSkeleton.Remove(index);
            _lastUsedItemTimeForSkeleton.Remove(index);
        }

        private static void SendShopPage(TSPlayer plr, PlayerShopSession session)
        {
            if (plr == null || session == null) return;
            List<SKConfig.SKItem> pageItems;
            int totalPages;
            GetPageItems(session.AllItems, session.CurrentPage, out totalPages, out pageItems);
            if (totalPages <= 0) totalPages = 1;
            session.TotalPages = totalPages;
            SendShopItems(plr, pageItems);
        }

        private static void SendShopItems(TSPlayer plr, List<SKConfig.SKItem> items)
        {
            try
            {
                int slotCount = SKPlugin.Config.ItemsPerPage;
                if (slotCount < 0) slotCount = 0;
                if (slotCount > 40) slotCount = 40;

                for (byte i = 0; i < slotCount; i++)
                {
                    if (i < items.Count)
                    {
                        var packet = BuildShopItemPacket(i, items[i]);
                        plr.SendRawData(packet);
                    }
                    else
                    {
                        byte[] emptyPacket = { 14, 0, 104, i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                        plr.SendRawData(emptyPacket);
                    }
                }

                for (byte i = (byte)slotCount; i < 40; i++)
                {
                    byte[] emptyPacket = { 14, 0, 104, i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                    plr.SendRawData(emptyPacket);
                }
            }
            catch { }
        }

        private static byte[] BuildShopItemPacket(byte index, SKConfig.SKItem item)
        {
            int price = Item.buyPrice(item.Price.Platinum, item.Price.Gold, item.Price.Silver, item.Price.Copper);
            if (price < 0) price = 0;
            byte[] id = BitConverter.GetBytes(item.NetID);
            byte[] stack = BitConverter.GetBytes(1);
            byte[] cost = BitConverter.GetBytes(price);
            return new byte[] { 14, 0, 104, index, id[0], id[1], stack[0], stack[1], item.Prefix, cost[0], cost[1], cost[2], cost[3], 0 };
        }

        private static void GetPageItems(List<SKConfig.SKItem> allItems, int currentPageIndex, out int totalPages, out List<SKConfig.SKItem> pageItems)
        {
            int perPage = SKPlugin.Config.ItemsPerPage;
            if (perPage <= 0) perPage = 1;
            pageItems = new List<SKConfig.SKItem>();
            totalPages = 1;
            if (allItems == null || allItems.Count == 0) return;

            var groups = allItems.GroupBy(item => item.Page ?? -1).OrderBy(g => g.Key).ToList();
            if (groups.Count == 1 && groups[0].Key == -1)
            {
                totalPages = (allItems.Count + perPage - 1) / perPage;
                if (totalPages <= 0) totalPages = 1;
                if (currentPageIndex < 0 || currentPageIndex >= totalPages) currentPageIndex = 0;
                int start = currentPageIndex * perPage;
                if (start < 0 || start >= allItems.Count) return;
                int count = Math.Min(perPage, allItems.Count - start);
                if (count <= 0) return;
                pageItems = allItems.GetRange(start, count);
                return;
            }

            var pageGroups = groups.Select(g => g.ToList()).ToList();
            totalPages = pageGroups.Count;
            if (totalPages <= 0) totalPages = 1;
            if (currentPageIndex < 0 || currentPageIndex >= totalPages) currentPageIndex = 0;
            var currentGroup = pageGroups[currentPageIndex];
            var sorted = currentGroup.OrderBy(item => item.SlotIndex ?? 9999).ToList();
            var slotArray = new SKConfig.SKItem?[perPage];
            var usedSlots = new HashSet<int>();
            var unassignedItems = new List<SKConfig.SKItem>();
            foreach (var item in sorted)
            {
                if (item.SlotIndex.HasValue)
                {
                    int slot = item.SlotIndex.Value - 1;
                    if (slot >= 0 && slot < perPage && !usedSlots.Contains(slot))
                    {
                        slotArray[slot] = item;
                        usedSlots.Add(slot);
                    }
                    else unassignedItems.Add(item);
                }
                else unassignedItems.Add(item);
            }
            int nextSlot = 0;
            foreach (var item in unassignedItems)
            {
                while (nextSlot < perPage && slotArray[nextSlot].HasValue) nextSlot++;
                if (nextSlot < perPage) { slotArray[nextSlot] = item; nextSlot++; }
            }
            pageItems = slotArray.Where(s => s.HasValue).Select(s => s.Value).ToList();
        }

        private static void GetPageItems(List<SKConfig.SKItem> allItems, int currentPageIndex, out int totalPages)
        {
            List<SKConfig.SKItem> dummy;
            GetPageItems(allItems, currentPageIndex, out totalPages, out dummy);
        }

        private static bool CheckConditions(TSPlayer plr, string[] conditions)
        {
            if (conditions == null || conditions.Length == 0) return true;
            foreach (var cond in conditions)
            {
                if (cond == null) continue;
                if (CheckSingleCondition(plr, cond.Trim())) return true;
            }
            return false;
        }

        private static bool CheckSingleCondition(TSPlayer plr, string condition)
        {
            if (string.IsNullOrEmpty(condition)) return true;
            var lower = condition.ToLowerInvariant();

            if (lower.Contains("击败"))
            {
                var bossName = condition.Replace("击败", "").Trim();
                switch (bossName)
                {
                    case "史莱姆王": return NPC.downedSlimeKing;
                    case "克苏鲁之眼": return NPC.downedBoss1;
                    case "世界吞噬者": return NPC.downedBoss2;
                    case "克苏鲁之脑": return NPC.downedBoss2;
                    case "骷髅王": return NPC.downedBoss3;
                    case "蜂王": return NPC.downedQueenBee;
                    case "鹿角怪": return NPC.downedDeerclops;
                    case "血肉墙": return Main.hardMode;
                    case "毁灭者": return NPC.downedMechBoss1;
                    case "双子魔眼": return NPC.downedMechBoss2;
                    case "机械骷髅王": return NPC.downedMechBoss3;
                    case "史莱姆皇后": return NPC.downedQueenSlime;
                    case "世纪之花": return NPC.downedPlantBoss;
                    case "石巨人": return NPC.downedGolemBoss;
                    case "光之女皇": return NPC.downedEmpressOfLight;
                    case "猪龙鱼公爵": return NPC.downedFishron;
                    case "拜月教邪教徒": return NPC.downedAncientCultist;
                    case "月亮领主": return NPC.downedMoonlord;
                    default: return false;
                }
            }

            switch (lower)
            {
                case "肉后": return Main.hardMode;
                case "花后": return NPC.downedPlantBoss;
                case "石后": return NPC.downedGolemBoss;
                case "月后": return NPC.downedMoonlord;
            }

            if (lower == "白天") return Main.dayTime;
            if (lower == "晚上") return !Main.dayTime;
            if (lower == "中午") return Main.dayTime && Main.time >= 27000 && Main.time < 48600;
            if (lower == "午夜") return !Main.dayTime && Main.time >= 16200 && Main.time < 32400;

            switch (lower)
            {
                case "满月": return Main.moonPhase == 0;
                case "亏凸月": return Main.moonPhase == 1;
                case "下弦月": return Main.moonPhase == 2;
                case "残月": return Main.moonPhase == 3;
                case "新月": return Main.moonPhase == 4;
                case "娥眉月": return Main.moonPhase == 5;
                case "上弦月": return Main.moonPhase == 6;
                case "盈凸月": return Main.moonPhase == 7;
            }

            if (lower == "下雨" || lower == "雨天") return Main.raining;
            if (lower == "血月") return Main.bloodMoon;
            if (lower == "日食") return Main.eclipse;
            if (lower == "派对") return BirthdayParty._wasCelebrating;
            if (lower == "沙尘暴") return Sandstorm.Happening;
            if (lower == "大风天") return Main.IsItAHappyWindyDay;
            if (lower == "雷雨" || lower == "暴风雨") return Main.IsItStorming;
            if (lower == "史莱姆雨") return Main.slimeRain;
            if (lower == "流星雨") return Star.starfallBoost > 3f;
            if (lower == "灯笼夜") return LanternNight.LanternsUp;
            if (lower == "哥布林军队") return Main.invasionType == 1;
            if (lower == "海盗入侵") return Main.invasionType == 3;
            if (lower == "霜月") return Main.invasionType == 2;
            if (lower == "火星暴乱") return Main.invasionType == 4;
            if (lower == "南瓜月") return Main.pumpkinMoon;
            if (lower == "雪人军团") return Main.snowMoon;
            if (lower == "撒旦军队") return DD2Event.Ongoing;
            if (lower == "月亮事件") return NPC.LunarApocalypseIsUp;

            if (lower == "森林") return plr.TPlayer.ShoppingZone_Forest;
            if (lower == "丛林") return plr.TPlayer.ZoneJungle;
            if (lower == "沙漠") return plr.TPlayer.ZoneDesert;
            if (lower == "雪原") return plr.TPlayer.ZoneSnow;
            if (lower == "洞穴") return plr.TPlayer.ZoneUnderworldHeight;
            if (lower == "海洋") return plr.TPlayer.ZoneBeach;
            if (lower == "神圣") return plr.TPlayer.ZoneHallow;
            if (lower == "蘑菇") return plr.TPlayer.ZoneGlowshroom;
            if (lower == "腐化") return plr.TPlayer.ZoneCorrupt;
            if (lower == "猩红") return plr.TPlayer.ZoneCrimson;
            if (lower == "地牢") return plr.TPlayer.ZoneDungeon;
            if (lower == "墓地") return plr.TPlayer.ZoneGraveyard;
            if (lower == "蜂巢") return plr.TPlayer.ZoneHive;
            if (lower == "神庙") return plr.TPlayer.ZoneLihzhardTemple;
            if (lower == "沙尘暴(生物群落)") return plr.TPlayer.sandStorm;
            if (lower == "天空") return plr.TPlayer.ZoneSkyHeight;
            if (lower == "池塘") return CheckPond(plr);
            if (lower == "生命<400") return plr.TPlayer.statLifeMax < 400;
            if (lower == "生命≥400") return plr.TPlayer.statLifeMax >= 400;

            return false;
        }

        private static bool CheckPond(TSPlayer plr)
        {
            if (plr == null || plr.TPlayer == null) return false;
            Rectangle rect = new Rectangle(plr.TileX - 61, plr.TileY - 34 + 3, 122, 68);
            int count = 0;
            for (int x = rect.X; x < rect.Right; x++)
            {
                if (x < 0 || x >= Main.maxTilesX) continue;
                for (int y = rect.Y; y < rect.Bottom; y++)
                {
                    if (y < 0 || y >= Main.maxTilesY) continue;
                    if (Main.tile[x, y] == null) continue;
                    if (Main.tile[x, y].liquid == byte.MaxValue)
                    {
                        count++;
                        if (count >= 200) return true;
                    }
                }
            }
            return false;
        }
    }
}