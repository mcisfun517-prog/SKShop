#nullable disable

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using TShockAPI;

namespace SKShop
{
    public class SKConfig
    {
        [JsonProperty("作者", Order = 1)]
        public string Author { get; set; } = "百事#0";

        [JsonProperty("说明1", Order = 2)]
        public string Note1 { get; set; } =
            "本配置文件为自定义骷髅商人商店插件配置，修改后请重启服务器或输入/reload重载配置。";

        [JsonProperty("说明2", Order = 3)]
        public string Note2 { get; set; } =
            "不支持自定义货币";

        private static readonly string[] DefaultConditionsDescription =
        {
            "击败史莱姆王, 击败克苏鲁之眼, 击败世界吞噬者, 击败克苏鲁之脑, 击败蜂王, 击败骷髅王, 击败鹿角怪, 击败血肉墙, 击败毁灭者, 击败双子魔眼, 击败机械骷髅王, 击败史莱姆皇后, 击败世纪之花, 击败石巨人, 击败光之女皇, 击败猪龙鱼公爵, 击败拜月教邪教徒, 击败月亮领主",
            "肉后, 花后, 石后, 月后",
            "白天, 晚上, 中午, 午夜",
            "满月, 亏凸月, 下弦月, 残月, 新月, 娥眉月, 上弦月, 盈凸月",
            "下雨/雨天, 血月, 日食, 派对, 沙尘暴, 大风天, 雷雨/暴风雨, 史莱姆雨, 流星雨, 灯笼夜",
            "哥布林军队, 海盗入侵, 霜月, 火星暴乱, 南瓜月, 雪人军团, 撒旦军队, 月亮事件",
            "森林, 丛林, 沙漠, 雪原, 洞穴, 海洋, 神圣, 蘑菇, 腐化, 猩红, 地牢, 墓地, 蜂巢, 神庙, 天空, 池塘",
            "生命<400, 生命≥400",
            "若限制条件数组为空，则商品始终显示。"
        };

        [JsonProperty("限制与生成条件说明", Order = 4)]
        public string[] ConditionsDescription { get; set; } = DefaultConditionsDescription;

        [JsonProperty("骷髅商人生成使用物品ID (0=禁用)", Order = 5)]
        public int TriggerItemID { get; set; } = 75;

        [JsonProperty("重生延迟(秒)", Order = 6)]
        public int RespawnDelay { get; set; } = 60;

        [JsonProperty("是否启用自动生成", Order = 7)]
        public bool EnableAutoSpawn { get; set; } = true;

        [JsonProperty("自动生成条件(满足任一条件即可)", Order = 8)]
        public string[] AutoSpawnConditions { get; set; } = new string[] { "击败史莱姆王" };

        [JsonProperty("是否启用自定义商店 (false=使用原版商店)", Order = 9)]
        public bool EnableCustomShop { get; set; } = true;

        [JsonProperty("上一页物品ID (0=禁用翻页)", Order = 10)]
        public int PrevPageItemID { get; set; } = 72;

        [JsonProperty("下一页物品ID (0=禁用翻页)", Order = 11)]
        public int NextPageItemID { get; set; } = 71;

        [JsonProperty("每页物品数量 (1-40, 默认40)", Order = 12)]
        public int ItemsPerPage { get; set; } = 40;

        [JsonProperty("交易冷却时间(毫秒，0=禁用冷却)", Order = 13)]
        public int TransactionCooldownMs { get; set; } = 200;

        [JsonProperty("卖出返还比例 (百分比, 如50表示50%)", Order = 14)]
        public int SellReturnPercent { get; set; } = 1;

        [JsonProperty("总列表", Order = 15)]
        public List<ShopContainer> Shops { get; set; } = new List<ShopContainer>();

        public static SKConfig Load()
        {
            string path = Path.Combine(TShock.SavePath, "SKShop.json");
            if (!File.Exists(path))
            {
                SKConfig config = new SKConfig
                {
                    Shops = new List<ShopContainer>
                    {
                        new ShopContainer
                        {
                            Enabled = true,
                            Shops = new Shop[]
                            {
                                new Shop
                                {
                                    Enabled = true,
                                    Groups = new List<string>(),
                                    OpenMessage = new List<string> { "使用铜币下一页，使用银币上一页" },
                                    CloseMessage = new List<string>(),
                                    Items = ShopData.GetDefaultShopItems()
                                }
                            }
                        }
                    }
                };
                File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
                return config;
            }

            SKConfig loaded = JsonConvert.DeserializeObject<SKConfig>(File.ReadAllText(path));
            if (loaded == null) loaded = new SKConfig();

            if (string.IsNullOrEmpty(loaded.Author)) loaded.Author = "百事#0";
            if (string.IsNullOrEmpty(loaded.Note1)) loaded.Note1 = "本配置文件为自定义骷髅商人商店配置，修改后请重启服务器或输入/reload重载配置。";
            if (string.IsNullOrEmpty(loaded.Note2)) loaded.Note2 = "不支持自定义货币";
            if (loaded.ConditionsDescription == null || loaded.ConditionsDescription.Length == 0)
                loaded.ConditionsDescription = DefaultConditionsDescription;
            if (loaded.AutoSpawnConditions == null) loaded.AutoSpawnConditions = new string[] { "击败史莱姆王" };
            if (loaded.Shops == null) loaded.Shops = new List<ShopContainer>();
            if (loaded.ItemsPerPage < 1 || loaded.ItemsPerPage > 40) { Console.WriteLine("[SKShop] 每页物品数量超出范围 (1-40)，已重置为 40"); loaded.ItemsPerPage = 40; }
            if (loaded.TransactionCooldownMs < 0) loaded.TransactionCooldownMs = 0;
            if (loaded.SellReturnPercent < 0) loaded.SellReturnPercent = 0;

            for (int containerIndex = 0; containerIndex < loaded.Shops.Count; containerIndex++)
            {
                var container = loaded.Shops[containerIndex];
                if (container.Shops == null) container.Shops = new Shop[0];
                for (int shopIndex = 0; shopIndex < container.Shops.Length; shopIndex++)
                {
                    var shop = container.Shops[shopIndex];
                    if (shop.Groups == null) shop.Groups = new List<string>();
                    if (shop.OpenMessage == null) shop.OpenMessage = new List<string>();
                    if (shop.CloseMessage == null) shop.CloseMessage = new List<string>();
                    if (shop.Items == null || shop.Items.Length == 0)
                        shop.Items = ShopData.GetDefaultShopItems();

                  
                    for (int i = 0; i < shop.Items.Length; i++)
                    {
                        var item = shop.Items[i];
                        if (!item.SellPrice.HasValue)
                        {
                            item.SellPrice = new Price { Copper = 0, Silver = 0, Gold = 0, Platinum = 0 };
                            shop.Items[i] = item;
                        }
                    }

                    container.Shops[shopIndex] = shop;
                }
                loaded.Shops[containerIndex] = container;
            }

            return loaded;
        }

        public void Save()
        {
            string path = Path.Combine(TShock.SavePath, "SKShop.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        #region 数据结构
        public struct ShopContainer
        {
            [JsonProperty("启用")]
            public bool Enabled;
            [JsonProperty("商店列表")]
            public Shop[] Shops;
        }

        public struct Shop
        {
            [JsonProperty("启用")]
            public bool Enabled;
            [JsonProperty("允许打开商店的用户组[留空则允许所有]")]
            public List<string> Groups;
            [JsonProperty("点击NPC聊天栏消息")]
            public List<string> OpenMessage;
            [JsonProperty("关闭商店消息")]
            public List<string> CloseMessage;
            [JsonProperty("商品")]
            public SKItem[] Items;
        }

        public struct SKItem
        {
            [JsonProperty("物品ID")]
            public short NetID;
            [JsonProperty("前缀")]
            public byte Prefix;
            [JsonProperty("价格")]
            public Price Price;
            [JsonProperty("限制条件")]
            public string[] Conditions;
            [JsonProperty("页码(可选，若未设置则自动按顺序分页)")]
            public int? Page;
            [JsonProperty("显示槽位 (可选，1-40，同一页内不可重复)")]
            public int? SlotIndex;
            [JsonProperty("备注(仅供阅读)")]
            public string Note;

            [JsonProperty("卖出价格（可选，未设置则按全局比例返还）")]
            public Price? SellPrice;
        }

        public struct Price
        {
            [JsonProperty("铜币")]
            public byte Copper;
            [JsonProperty("银币")]
            public byte Silver;
            [JsonProperty("金币")]
            public byte Gold;
            [JsonProperty("铂金币")]
            public short Platinum;
        }
        #endregion
    }
}