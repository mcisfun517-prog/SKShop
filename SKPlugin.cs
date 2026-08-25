#nullable disable

using Microsoft.Xna.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace SKShop
{
    [ApiVersion(2, 1)]
    public class SKPlugin : TerrariaPlugin
    {
        public override string Name => "自定义骷髅商人商店";
        public override string Author => "百事#0";
        public override Version Version => new Version(3, 5, 0);
        public override string Description => GetString("自定义骷髅商人商店");

        public SKPlugin(Main game) : base(game) { }

        public static SKConfig Config { get; internal set; } = new SKConfig();
        public static List<SKConfig.Shop> AviliableShops { get; internal set; } = new List<SKConfig.Shop>();

        private int _npcIndex = -1;
        private bool _respawning = false;
        private int _lastSpawnX = 0;
        private int _lastSpawnY = 0;

        private bool[] _lastControlUseItem = new bool[256];
        private int _autoSpawnCheckTimer = 0;

        private class PlayerShopSession
        {
            public SKConfig.Shop Shop { get; set; }
            public List<SKConfig.SKItem> AllItems { get; set; } = new List<SKConfig.SKItem>();
            public int CurrentPage { get; set; }
            public int TotalPages { get; set; }
            public int NpcIndex { get; set; }
            public bool IsActive { get; set; } = true;
            public Task UpdateLoopTask { get; set; } = null!;
        }

        private Dictionary<int, PlayerShopSession> _sessions = new Dictionary<int, PlayerShopSession>();

        public override void Initialize()
        {
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled);
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
            GeneralHooks.ReloadEvent += OnReload;
            Commands.ChatCommands.Add(new Command(SpawnCommand, "skeleton"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilled);
                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
                GeneralHooks.ReloadEvent -= OnReload;
                AviliableShops.Clear();
                _sessions.Clear();
            }
            base.Dispose(disposing);
        }

        private void OnPostInitialize(EventArgs args) => ReloadConfig();

        private void OnReload(ReloadEventArgs args)
        {
            ReloadConfig();
            args.Player?.SendSuccessMessage("[SKShop] 配置已重载");
        }

        private void ReloadConfig()
        {
            Config = SKConfig.Load();
            AviliableShops.Clear();
            foreach (var container in Config.Shops)
            {
                if (!container.Enabled) continue;
                for (int i = 0; i < container.Shops.Length; i++)
                {
                    var shop = container.Shops[i];
                    if (!shop.Enabled) continue;
                    if (shop.Items == null || shop.Items.Length == 0)
                        shop.Items = ShopData.GetDefaultShopItems();
                    AviliableShops.Add(shop);
                }
            }
        }

        // ========================== 骷髅商人管理 ==========================
        private void OnNpcKilled(NpcKilledEventArgs args)
        {
            if (args.npc.whoAmI == _npcIndex && !_respawning)
            {
                _respawning = true;
                Task.Delay(Config.RespawnDelay * 1000).ContinueWith(_ =>
                {
                    _respawning = false;
                    if (_lastSpawnX != 0 || _lastSpawnY != 0)
                    {
                        Main.QueueMainThreadAction(() => SpawnSkeleton(_lastSpawnX, _lastSpawnY));
                    }
                    else
                    {
                        _npcIndex = -1;
                    }
                });
            }
        }

        private void OnUpdate(EventArgs args)
        {
            for (int i = 0; i < Main.player.Length; i++)
            {
                Player player = Main.player[i];
                if (player == null || !player.active || player.dead) continue;

                TSPlayer tsPlayer = TShock.Players[i];
                if (tsPlayer == null || !tsPlayer.Active || !tsPlayer.IsLoggedIn) continue;

                bool currentUse = player.controlUseItem;
                bool lastUse = _lastControlUseItem[i];

                if (currentUse && !lastUse)
                {
                    var selectedItem = player.inventory[player.selectedItem];
                    if (selectedItem != null)
                    {
                        int itemType = selectedItem.type;

                        if (Config.TriggerItemID > 0 && itemType == Config.TriggerItemID)
                        {
                            int x = (int)(player.Center.X / 16f);
                            int y = (int)(player.Center.Y / 16f);

                            if (_npcIndex != -1 && Main.npc[_npcIndex].active)
                            {
                                Main.npc[_npcIndex].active = false;
                                NetMessage.SendData(23, -1, -1, null, _npcIndex);
                                _npcIndex = -1;
                            }

                            Main.QueueMainThreadAction(() => SpawnSkeleton(x, y));
                            tsPlayer.SendSuccessMessage($"你召唤了骷髅商人于 ({x}, {y})");
                        }

                        if (_sessions.TryGetValue(i, out var session) && session.IsActive)
                        {
                            if (itemType == Config.NextPageItemID && session.CurrentPage < session.TotalPages - 1)
                            {
                                session.CurrentPage++;
                                SendShopPage(tsPlayer, session);
                            }
                            else if (itemType == Config.PrevPageItemID && session.CurrentPage > 0)
                            {
                                session.CurrentPage--;
                                SendShopPage(tsPlayer, session);
                            }
                        }
                    }
                }

                _lastControlUseItem[i] = currentUse;
            }

            _autoSpawnCheckTimer++;
            if (_autoSpawnCheckTimer >= 60)
            {
                _autoSpawnCheckTimer = 0;
                CheckAutoSpawn();
            }

            List<int> toRemove = new List<int>();
            foreach (var kvp in _sessions)
            {
                var plr = TShock.Players[kvp.Key];
                if (plr == null || !plr.Active || plr.Dead || plr.TPlayer.talkNPC != kvp.Value.NpcIndex)
                {
                    if (plr != null && plr.Active)
                    {
                        var closeMessages = kvp.Value.Shop.CloseMessage;
                        if (closeMessages != null && closeMessages.Count > 0)
                        {
                            foreach (var msg in closeMessages)
                                plr.SendMessage(msg, Color.Yellow);
                        }
                    }
                    kvp.Value.IsActive = false;
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var key in toRemove)
                _sessions.Remove(key);
        }

        private void CheckAutoSpawn()
        {
            if (!Config.EnableAutoSpawn) return;
            if (_npcIndex != -1 && Main.npc[_npcIndex].active) return;

            bool conditionMet = false;
            foreach (string cond in Config.AutoSpawnConditions)
            {
                if (CheckGlobalCondition(cond.Trim()))
                {
                    conditionMet = true;
                    break;
                }
            }
            if (!conditionMet) return;

            var townCenter = FindTownCenter();
            if (townCenter == null) return;

            int spawnX = townCenter.Value.X;
            int spawnY = townCenter.Value.Y;

            Main.QueueMainThreadAction(() =>
            {
                SpawnSkeleton(spawnX, spawnY);
                if (_npcIndex != -1 && Main.npc[_npcIndex].active)
                {
                    var npc = Main.npc[_npcIndex];
                    npc.homeless = true;
                    npc.netUpdate = true;
                    NetMessage.SendData(23, -1, -1, null, _npcIndex);
                }
                if (_npcIndex != -1 && Main.npc[_npcIndex].active)
                {
                    string npcName = Main.npc[_npcIndex].GivenName ?? Main.npc[_npcIndex].TypeName;
                    TSPlayer.All.SendMessage($"骷髅商人{npcName}已到达城镇附近! ({spawnX}, {spawnY})", new Color(0x32, 0x7D, 0xFF));
                }
                else
                {
                    TSPlayer.All.SendMessage($"骷髅商人已到达城镇附近 ({spawnX}, {spawnY})", new Color(0x32, 0x7D, 0xFF));
                }
            });
        }

        private (int X, int Y)? FindTownCenter()
        {
            var townNPCs = new List<NPC>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.townNPC && !npc.homeless)
                    townNPCs.Add(npc);
            }
            if (townNPCs.Count == 0) return null;

            float avgX = 0, avgY = 0;
            foreach (var npc in townNPCs)
            {
                avgX += npc.Center.X;
                avgY += npc.Center.Y;
            }
            avgX /= townNPCs.Count;
            avgY /= townNPCs.Count;
            return ((int)(avgX / 16f), (int)(avgY / 16f));
        }

        private bool CheckGlobalCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition)) return false;
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
                default: return false;
            }
        }

        private void SpawnSkeleton(int x, int y)
        {
            (int adjX, int adjY) = FindStandablePosition(x, y);
            x = adjX; y = adjY;

            if (_npcIndex != -1 && Main.npc[_npcIndex].active)
            {
                Main.npc[_npcIndex].active = false;
                NetMessage.SendData(23, -1, -1, null, _npcIndex);
                _npcIndex = -1;
            }

            int index = NPC.NewNPC(null, x * 16, y * 16, 453);
            if (index < 0 || index >= Main.maxNPCs) return;

            NPC npc = Main.npc[index];
            npc.townNPC = true;
            npc.homeless = true;
            npc.homeTileX = x;
            npc.homeTileY = y;
            npc.timeLeft = 0;
            npc.netUpdate = true;
            NetMessage.SendData(23, -1, -1, null, index);

            _npcIndex = index;
            _respawning = false;
            _lastSpawnX = x;
            _lastSpawnY = y;
        }

        private (int X, int Y) FindStandablePosition(int startX, int startY)
        {
            if (startX < 0 || startX >= Main.maxTilesX || startY < 0 || startY >= Main.maxTilesY)
                return (startX, startY);

            bool IsSolid(int x, int y)
            {
                if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) return true;
                var tile = Main.tile[x, y];
                return tile != null && tile.active() && Main.tileSolid[tile.type];
            }
            bool IsPlatform(int x, int y)
            {
                if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) return false;
                var tile = Main.tile[x, y];
                return tile != null && tile.active() && tile.type == 19;
            }

            if (!IsSolid(startX, startY) && (IsSolid(startX, startY + 1) || IsPlatform(startX, startY + 1)))
                return (startX, startY);

            for (int offset = 1; offset <= 20; offset++)
            {
                int checkY = startY - offset;
                if (checkY < 0) break;
                if (!IsSolid(startX, checkY) && (IsSolid(startX, checkY + 1) || IsPlatform(startX, checkY + 1)))
                    return (startX, checkY);
            }

            for (int offset = 1; offset <= 10; offset++)
            {
                int checkY = startY + offset;
                if (checkY >= Main.maxTilesY - 1) break;
                if (!IsSolid(startX, checkY) && (IsSolid(startX, checkY + 1) || IsPlatform(startX, checkY + 1)))
                    return (startX, checkY);
            }

            return (startX, startY);
        }

        private void SpawnCommand(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("用法: /skeleton <x> <y>  或 /skeleton remove");
                return;
            }

            if (args.Parameters[0].ToLower() == "remove")
            {
                if (_npcIndex != -1 && Main.npc[_npcIndex].active)
                {
                    Main.npc[_npcIndex].active = false;
                    NetMessage.SendData(23, -1, -1, null, _npcIndex);
                    _npcIndex = -1;
                    args.Player.SendSuccessMessage("已移除骷髅商人");
                }
                else
                    args.Player.SendErrorMessage("当前没有骷髅商人");
                return;
            }

            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("用法: /skeleton <x> <y>  或 /skeleton remove");
                return;
            }

            if (!int.TryParse(args.Parameters[0], out int x) || !int.TryParse(args.Parameters[1], out int y))
            {
                args.Player.SendErrorMessage("坐标必须为整数");
                return;
            }

            if (_npcIndex != -1 && Main.npc[_npcIndex].active)
            {
                Main.npc[_npcIndex].active = false;
                NetMessage.SendData(23, -1, -1, null, _npcIndex);
                _npcIndex = -1;
            }

            SpawnSkeleton(x, y);
            args.Player.SendSuccessMessage($"已在 ({x},{y}) 生成骷髅商人");
        }

        // ========================== 商店数据包处理 ==========================
        private void OnGetData(GetDataEventArgs args)
        {
            if (args.Handled) return;

            if (args.MsgID == PacketTypes.NpcTalk)
            {
                int playerIndex = args.Msg.readBuffer[args.Index];
                int npcIndex = args.Msg.readBuffer[args.Index + 1] + (args.Msg.readBuffer[args.Index + 2] << 8);

                if (playerIndex != args.Msg.whoAmI || npcIndex < 0 || npcIndex >= Main.maxNPCs)
                    return;

                var plr = TShock.Players[playerIndex];
                if (plr == null || !plr.Active) return;

                NPC npc = Main.npc[npcIndex];
                if (!npc.active || npc.type != 453) return;

                if (!Config.EnableCustomShop) return;

                var shop = AviliableShops.FirstOrDefault(s =>
                    (s.Groups.Count == 0 || s.Groups.Contains(plr.Group.Name)) && s.Enabled
                );
                if (shop.Enabled == false) return;

                if (_sessions.TryGetValue(playerIndex, out var existingSession) && existingSession.IsActive && existingSession.NpcIndex == npcIndex)
                {
                    var newItems = shop.Items.Where(item => CheckConditions(plr, item.Conditions)).ToList();
                    int newTotalPages;
                    GetPageItems(newItems, existingSession.CurrentPage, out newTotalPages);
                    existingSession.AllItems = newItems;
                    existingSession.TotalPages = newTotalPages;
                    if (existingSession.CurrentPage >= newTotalPages)
                        existingSession.CurrentPage = newTotalPages - 1;
                    if (existingSession.CurrentPage < 0) existingSession.CurrentPage = 0;

                    SendShopPage(plr, existingSession);
                    args.Handled = true;
                    return;
                }

                if (_sessions.TryGetValue(playerIndex, out var oldSession))
                {
                    oldSession.IsActive = false;
                    _sessions.Remove(playerIndex);
                }

                plr.TPlayer.talkNPC = npcIndex;

                NetMessage.SendData((int)PacketTypes.NpcTalk, plr.Index, -1,
                    NetworkText.FromLiteral(npc.GivenName ?? npc.TypeName),
                    npcIndex, 0, 0, 0, 0);

                var allItems = shop.Items.Where(item => CheckConditions(plr, item.Conditions)).ToList();
                int totalPages;
                GetPageItems(allItems, 0, out totalPages);
                if (totalPages == 0) totalPages = 1;

                var session = new PlayerShopSession
                {
                    Shop = shop,
                    AllItems = allItems,
                    CurrentPage = 0,
                    TotalPages = totalPages,
                    NpcIndex = npcIndex,
                    IsActive = true
                };
                _sessions[playerIndex] = session;

                if (shop.OpenMessage != null && shop.OpenMessage.Count > 0)
                {
                    foreach (var msg in shop.OpenMessage)
                        plr.SendMessage(msg, Color.Yellow);
                }

                SendShopPage(plr, session);

                int interval = 600;
                var currentSession = session;
                session.UpdateLoopTask = Task.Run(async () =>
                {
                    int targetNpc = npcIndex;
                    while (plr.Active && !plr.Dead && currentSession.IsActive && plr.TPlayer.talkNPC == targetNpc)
                    {
                        await Task.Delay(interval);
                        if (plr.Active && !plr.Dead && currentSession.IsActive && plr.TPlayer.talkNPC == targetNpc)
                        {
                            try
                            {
                                var refreshedItems = shop.Items.Where(item => CheckConditions(plr, item.Conditions)).ToList();
                                int newTotalPages;
                                GetPageItems(refreshedItems, currentSession.CurrentPage, out newTotalPages);
                                currentSession.AllItems = refreshedItems;
                                currentSession.TotalPages = newTotalPages;
                                if (currentSession.CurrentPage >= newTotalPages)
                                    currentSession.CurrentPage = newTotalPages - 1;
                                if (currentSession.CurrentPage < 0) currentSession.CurrentPage = 0;
                                SendShopPage(plr, currentSession);
                            }
                            catch (ObjectDisposedException) { break; }
                            catch { break; }
                        }
                    }
                    if (_sessions.TryGetValue(playerIndex, out var existing) && existing == currentSession)
                    {
                        currentSession.IsActive = false;
                        _sessions.Remove(playerIndex);
                    }
                });

                args.Handled = true;
                return;
            }
        }

        // ========================== 分页逻辑（支持槽位指定，已修复空比较） ==========================
        private void GetPageItems(List<SKConfig.SKItem> allItems, int currentPageIndex, out int totalPages, out List<SKConfig.SKItem> pageItems)
        {
            int perPage = Config.ItemsPerPage;
            pageItems = new List<SKConfig.SKItem>();
            totalPages = 1;

            if (allItems.Count == 0) return;

            
            var groups = allItems.GroupBy(item => item.Page ?? -1).OrderBy(g => g.Key).ToList();
            if (groups.Count == 1 && groups[0].Key == -1)
            {
               
                totalPages = (allItems.Count + perPage - 1) / perPage;
                if (totalPages == 0) totalPages = 1;
                if (currentPageIndex < 0 || currentPageIndex >= totalPages) currentPageIndex = 0;
                int start = currentPageIndex * perPage;
                int count = Math.Min(perPage, allItems.Count - start);
                pageItems = allItems.GetRange(start, count);
                return;
            }

            
            var pageGroups = groups.Select(g => g.ToList()).ToList();
            totalPages = pageGroups.Count;
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
                    else
                    {
                        unassignedItems.Add(item);
                    }
                }
                else
                {
                    unassignedItems.Add(item);
                }
            }


            int nextSlot = 0;
            foreach (var item in unassignedItems)
            {
                while (nextSlot < perPage && slotArray[nextSlot].HasValue)
                    nextSlot++;
                if (nextSlot < perPage)
                {
                    slotArray[nextSlot] = item;
                    nextSlot++;
                }
            }

          
            pageItems = slotArray.Where(s => s.HasValue).Select(s => s.Value).ToList();
        }

        private void GetPageItems(List<SKConfig.SKItem> allItems, int currentPageIndex, out int totalPages)
        {
            var dummy = new List<SKConfig.SKItem>();
            GetPageItems(allItems, currentPageIndex, out totalPages, out dummy);
        }

        private void SendShopPage(TSPlayer plr, PlayerShopSession session)
        {
            List<SKConfig.SKItem> pageItems;
            int totalPages;
            GetPageItems(session.AllItems, session.CurrentPage, out totalPages, out pageItems);
            session.TotalPages = totalPages;
            SendShopItems(plr, pageItems);
        }

        private void SendShopItems(TSPlayer plr, List<SKConfig.SKItem> items)
        {
            try
            {
                int slotCount = Config.ItemsPerPage;
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
            catch (ObjectDisposedException) { }
            catch { }
        }

        private byte[] BuildShopItemPacket(byte index, SKConfig.SKItem item)
        {
            int price = Item.buyPrice(item.Price.Platinum, item.Price.Gold, item.Price.Silver, item.Price.Copper);
            if (price < 0) price = 0;
            var id = BitConverter.GetBytes(item.NetID);
            var stack = BitConverter.GetBytes(1);
            var cost = BitConverter.GetBytes(price);

            return new byte[]
            {
                14, 0, 104, index,
                id[0], id[1],
                stack[0], stack[1],
                item.Prefix,
                cost[0], cost[1], cost[2], cost[3],
                0
            };
        }

        // ========================== 商品条件检查 ==========================
        private bool CheckConditions(TSPlayer plr, string[] conditions)
        {
            if (conditions == null || conditions.Length == 0) return true;
            foreach (var cond in conditions)
                if (CheckSingleCondition(plr, cond.Trim()))
                    return true;
            return false;
        }

        private bool CheckSingleCondition(TSPlayer plr, string condition)
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

        private bool CheckPond(TSPlayer plr)
        {
            Rectangle rect = new Rectangle(plr.TileX - 61, plr.TileY - 34 + 3, 122, 68);
            int count = 0;
            for (int x = rect.X; x < rect.Right; x++)
            {
                for (int y = rect.Y; y < rect.Bottom; y++)
                {
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