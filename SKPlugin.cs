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
        public new string Name => "自定义骷髅商人商店";
        public new string Author => "百事#0";
        public new Version Version => new Version(7, 5, 0);
        public new string Description => GetString("自定义骷髅商人商店");

        public SKPlugin(Main game) : base(game) { }

        public static SKConfig Config { get; internal set; } = new SKConfig();

        public static List<SKConfig.Shop> AviliableShops { get; internal set; }
            = new List<SKConfig.Shop>();

        public static HashSet<short> CustomShopItemIds { get; internal set; }
            = new HashSet<short>();

        public static Dictionary<int, int> CustomItemPriceCopper { get; internal set; }
            = new Dictionary<int, int>();

        public static Dictionary<int, int?> CustomItemSellPriceCopper { get; internal set; }
            = new Dictionary<int, int?>();

        private int _npcIndex = -1;
        private bool _respawning = false;
        private int _lastSpawnX = 0;
        private int _lastSpawnY = 0;

        private int _autoSpawnCheckTimer = 0;

        private bool[] _lastUseItemForSpawn = new bool[256];

        public override void Initialize()
        {
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled);
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
            GeneralHooks.ReloadEvent += OnReload;

            Commands.ChatCommands.Add(
                new Command(SpawnCommand, "skeleton")
            );
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilled);
                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
                GeneralHooks.ReloadEvent -= OnReload;

                SkeletonShopManager.CleanupSessions();
                UniversalPriceManager.Cleanup();
                ShopHelper.Cleanup();

                AviliableShops.Clear();
                CustomShopItemIds.Clear();
                CustomItemPriceCopper.Clear();
                CustomItemSellPriceCopper.Clear();
            }

            base.Dispose(disposing);
        }

        
        private void OnServerLeave(LeaveEventArgs args)
        {
            
        }

        private void OnPostInitialize(EventArgs args)
        {
            ReloadConfig();
        }

        private void OnReload(ReloadEventArgs args)
        {
            ReloadConfig();
            ShopHelper.Cleanup();
            args.Player?.SendSuccessMessage("[SKShop] 配置已重载");
        }

        private void ReloadConfig()
        {
            Config = SKConfig.Load();

            AviliableShops.Clear();
            CustomShopItemIds.Clear();
            CustomItemPriceCopper.Clear();
            CustomItemSellPriceCopper.Clear();

            foreach (var container in Config.Shops)
            {
                if (!container.Enabled)
                    continue;

                for (int i = 0; i < container.Shops.Length; i++)
                {
                    var shop = container.Shops[i];
                    if (!shop.Enabled)
                        continue;

                    if (shop.Items == null || shop.Items.Length == 0)
                    {
                        shop.Items = ShopData.GetDefaultShopItems();
                    }

                    AviliableShops.Add(shop);
                }
            }

            
            foreach (var shop in AviliableShops)
            {
                if (shop.Items == null)
                    continue;

                foreach (var item in shop.Items)
                {
                    CustomShopItemIds.Add(item.NetID);

                    if (!CustomItemPriceCopper.ContainsKey(item.NetID))
                    {
                        int copper = Item.buyPrice(
                            item.Price.Platinum,
                            item.Price.Gold,
                            item.Price.Silver,
                            item.Price.Copper
                        );
                        if (copper < 0) copper = 0;
                        CustomItemPriceCopper[item.NetID] = copper;
                    }
                }
            }

            
            foreach (var shop in AviliableShops)
            {
                if (shop.Items == null) continue;
                foreach (var item in shop.Items)
                {
                    if (CustomItemSellPriceCopper.ContainsKey(item.NetID))
                        continue;

                    if (item.SellPrice.HasValue)
                    {
                        int copper = Item.buyPrice(
                            item.SellPrice.Value.Platinum,
                            item.SellPrice.Value.Gold,
                            item.SellPrice.Value.Silver,
                            item.SellPrice.Value.Copper
                        );
                        CustomItemSellPriceCopper[item.NetID] = copper >= 0 ? copper : 0;
                    }
                    else
                    {
                        CustomItemSellPriceCopper[item.NetID] = null;
                    }
                }
            }
        }


        private void OnUpdate(EventArgs args)
        {
            for (int i = 0; i < Main.player.Length; i++)
            {
                Player player = Main.player[i];
                if (player == null || !player.active || player.dead)
                    continue;

                TSPlayer plr = TShock.Players[i];
                if (plr == null || !plr.Active || !plr.IsLoggedIn)
                    continue;

                ShopHelper.RecordItemUse(plr, player);
                SkeletonShopManager.HandleItemUse(plr, player);
                UniversalPriceManager.HandleItemUse(plr, player);

                int talkNpc = player.talkNPC;
                if (talkNpc >= 0 && talkNpc < Main.maxNPCs && Main.npc[talkNpc].active)
                {
                    if (Main.npc[talkNpc].type == 453)
                    {
                        SkeletonShopManager.MonitorSkeletonTrading(plr);
                    }
                    else
                    {
                        UniversalPriceManager.MonitorAllNPCs(plr);
                    }
                }

                bool currentUse = player.controlUseItem;
                bool lastUse = _lastUseItemForSpawn[i];
                if (currentUse && !lastUse)
                {
                    if (player.inventory != null &&
                        player.selectedItem >= 0 &&
                        player.selectedItem < player.inventory.Length)
                    {
                        var selectedItem = player.inventory[player.selectedItem];
                        if (selectedItem != null)
                        {
                            int itemType = selectedItem.type;
                            if (Config.TriggerItemID > 0 && itemType == Config.TriggerItemID)
                            {
                                int x = (int)(player.Center.X / 16f);
                                int y = (int)(player.Center.Y / 16f);

                                if (_npcIndex != -1 && _npcIndex < Main.maxNPCs && Main.npc[_npcIndex].active)
                                {
                                    Main.npc[_npcIndex].active = false;
                                    NetMessage.SendData(23, -1, -1, null, _npcIndex);
                                    _npcIndex = -1;
                                }

                                Main.QueueMainThreadAction(() => SpawnSkeleton(x, y));
                                plr.SendSuccessMessage($"你召唤了骷髅商人于 ({x}, {y})");
                            }
                        }
                    }
                }
                _lastUseItemForSpawn[i] = currentUse;
            }

            _autoSpawnCheckTimer++;
            if (_autoSpawnCheckTimer >= 60)
            {
                _autoSpawnCheckTimer = 0;
                CheckAutoSpawn();
            }
        }

       
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

      
        private void CheckAutoSpawn()
        {
            if (!Config.EnableAutoSpawn)
                return;

            if (_npcIndex != -1 && _npcIndex < Main.maxNPCs && Main.npc[_npcIndex].active)
                return;

            bool conditionMet = false;
            foreach (string cond in Config.AutoSpawnConditions)
            {
                if (CheckGlobalCondition(cond.Trim()))
                {
                    conditionMet = true;
                    break;
                }
            }
            if (!conditionMet)
                return;

            var townCenter = FindTownCenter();
            if (townCenter == null)
                return;

            int spawnX = townCenter.Value.X;
            int spawnY = townCenter.Value.Y;

            Main.QueueMainThreadAction(() =>
            {
                SpawnSkeleton(spawnX, spawnY);

                if (_npcIndex != -1 && _npcIndex < Main.maxNPCs && Main.npc[_npcIndex].active)
                {
                    var npc = Main.npc[_npcIndex];
                    npc.homeless = true;
                    npc.netUpdate = true;
                    NetMessage.SendData(23, -1, -1, null, _npcIndex);

                    string npcName = npc.GivenName ?? npc.TypeName;
                    TSPlayer.All.SendMessage($"骷髅商人{npcName}已到达城镇附近! ({spawnX}, {spawnY})",
                        new Color(0x32, 0x7D, 0xFF));
                }
                else
                {
                    TSPlayer.All.SendMessage($"骷髅商人已到达城镇附近 ({spawnX}, {spawnY})",
                        new Color(0x32, 0x7D, 0xFF));
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
            if (townNPCs.Count == 0)
                return null;

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
            if (string.IsNullOrEmpty(condition))
                return false;

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
            x = adjX;
            y = adjY;

            if (_npcIndex != -1 && _npcIndex < Main.maxNPCs && Main.npc[_npcIndex].active)
            {
                Main.npc[_npcIndex].active = false;
                NetMessage.SendData(23, -1, -1, null, _npcIndex);
                _npcIndex = -1;
            }

            int index = NPC.NewNPC(null, x * 16, y * 16, 453);
            if (index < 0 || index >= Main.maxNPCs)
                return;

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
                if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY)
                    return true;
                var tile = Main.tile[x, y];
                return tile != null && tile.active() && Main.tileSolid[tile.type];
            }

            bool IsPlatform(int x, int y)
            {
                if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY)
                    return false;
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
                if (_npcIndex != -1 && _npcIndex < Main.maxNPCs && Main.npc[_npcIndex].active)
                {
                    Main.npc[_npcIndex].active = false;
                    NetMessage.SendData(23, -1, -1, null, _npcIndex);
                    _npcIndex = -1;
                    args.Player.SendSuccessMessage("已移除骷髅商人");
                }
                else
                {
                    args.Player.SendErrorMessage("当前没有骷髅商人");
                }
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

            if (_npcIndex != -1 && _npcIndex < Main.maxNPCs && Main.npc[_npcIndex].active)
            {
                Main.npc[_npcIndex].active = false;
                NetMessage.SendData(23, -1, -1, null, _npcIndex);
                _npcIndex = -1;
            }

            SpawnSkeleton(x, y);
            args.Player.SendSuccessMessage($"已在 ({x},{y}) 生成骷髅商人");
        }

       
        private void OnGetData(GetDataEventArgs args)
        {
            if (args.Handled)
                return;

            if (args.MsgID == PacketTypes.NpcTalk)
            {
                int playerIndex = args.Msg.readBuffer[args.Index];
                int npcIndex = args.Msg.readBuffer[args.Index + 1] + (args.Msg.readBuffer[args.Index + 2] << 8);

                if (playerIndex != args.Msg.whoAmI || npcIndex < 0 || npcIndex >= Main.maxNPCs)
                    return;

                var plr = TShock.Players[playerIndex];
                if (plr == null || !plr.Active)
                    return;

                if (Main.npc[npcIndex].type == 453 && SKPlugin.Config.EnableCustomShop)
                {
                    if (SkeletonShopManager.HandleNpcTalk(args, plr, npcIndex))
                    {
                        args.Handled = true;
                    }
                }
            }
        }
    }
}