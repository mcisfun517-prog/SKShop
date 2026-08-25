﻿using System;
using System.Collections.Generic;
using SKShop;

namespace SKShop
{
    public static class ShopData
    {
        public static SKConfig.SKItem[] GetDefaultShopItems()
        {
            var items = new List<SKConfig.SKItem>();

            // ========== 第 1 页（40 个物品） ==========
            items.Add(new SKConfig.SKItem() { NetID = 284,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 1, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 1,  Note = "木回旋镖" });
            items.Add(new SKConfig.SKItem() { NetID = 946,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 1, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 2,  Note = "伞" });
            items.Add(new SKConfig.SKItem() { NetID = 3069, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 1, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 3,  Note = "火花魔棒" });
            items.Add(new SKConfig.SKItem() { NetID = 4341, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 50, Gold = 2, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 4,  Note = "梯凳" });
            items.Add(new SKConfig.SKItem() { NetID = 285,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 50, Gold = 2, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 5,  Note = "鞋带束头" });
            items.Add(new SKConfig.SKItem() { NetID = 3068, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 50, Gold = 2, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 6,  Note = "植物纤维绳索指南" });
            items.Add(new SKConfig.SKItem() { NetID = 3084, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 50, Gold = 2, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 7,  Note = "雷达" });
            items.Add(new SKConfig.SKItem() { NetID = 976,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 5, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 8,  Note = "猛虎攀爬装备" });
            items.Add(new SKConfig.SKItem() { NetID = 3001, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 5, Gold = 0, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 9,  Note = "诡药" });
            items.Add(new SKConfig.SKItem() { NetID = 28,   Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 3, Gold = 0, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 10, Note = "弱效治疗药水" });
            items.Add(new SKConfig.SKItem() { NetID = 188,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 10, Gold = 0, Platinum = 0 }, Conditions = new[] { "击败克苏鲁之脑", "击败世界吞噬者", "击败蜂王" }, Page = 1, SlotIndex = 11, Note = "治疗药水" });
            items.Add(new SKConfig.SKItem() { NetID = 499,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 50, Gold = 0, Platinum = 0 }, Conditions = new[] { "肉后" }, Page = 1, SlotIndex = 12, Note = "强效治疗药水" });
            items.Add(new SKConfig.SKItem() { NetID = 110,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 1, Gold = 0, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 13, Note = "弱效魔力药水" });
            items.Add(new SKConfig.SKItem() { NetID = 189,  Prefix = 0, Price = new SKConfig.Price { Copper = 50, Silver = 2, Gold = 0, Platinum = 0 }, Conditions = new[] { "击败克苏鲁之脑", "击败世界吞噬者", "击败蜂王" }, Page = 1, SlotIndex = 14, Note = "魔力药水" });
            items.Add(new SKConfig.SKItem() { NetID = 500,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 5, Gold = 0, Platinum = 0 }, Conditions = new[] { "肉后" }, Page = 1, SlotIndex = 15, Note = "强效魔力药水" });
            items.Add(new SKConfig.SKItem() { NetID = 2209, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 15, Gold = 0, Platinum = 0 }, Conditions = new[] { "花后" }, Page = 1, SlotIndex = 16, Note = "超级魔力药水" });
            items.Add(new SKConfig.SKItem() { NetID = 3002, Prefix = 0, Price = new SKConfig.Price { Copper = 50, Silver = 1, Gold = 0, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 17, Note = "洞穴探险荧光棒" });
            items.Add(new SKConfig.SKItem() { NetID = 5377, Prefix = 0, Price = new SKConfig.Price { Copper = 50, Silver = 1, Gold = 0, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 18, Note = "洞穴探险照明弹" });
            items.Add(new SKConfig.SKItem() { NetID = 282,  Prefix = 0, Price = new SKConfig.Price { Copper = 10, Silver = 0, Gold = 0, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 19, Note = "荧光棒" });
            items.Add(new SKConfig.SKItem() { NetID = 3004, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 1, Gold = 0, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 20, Note = "骨头火把" });
            items.Add(new SKConfig.SKItem() { NetID = 8,    Prefix = 0, Price = new SKConfig.Price { Copper = 50, Silver = 0, Gold = 0, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 21, Note = "火把" });
            items.Add(new SKConfig.SKItem() { NetID = 3003, Prefix = 0, Price = new SKConfig.Price { Copper = 15, Silver = 0, Gold = 0, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 22, Note = "骨箭" });
            items.Add(new SKConfig.SKItem() { NetID = 3309, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 5, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 23, Note = "黑平衡锤" });
            items.Add(new SKConfig.SKItem() { NetID = 3310, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 5, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 24, Note = "蓝平衡锤" });
            items.Add(new SKConfig.SKItem() { NetID = 3311, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 5, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 25, Note = "绿平衡锤" });
            items.Add(new SKConfig.SKItem() { NetID = 3312, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 5, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 26, Note = "紫平衡锤" });
            items.Add(new SKConfig.SKItem() { NetID = 3313, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 5, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 27, Note = "红平衡锤" });
            items.Add(new SKConfig.SKItem() { NetID = 3314, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 5, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 28, Note = "黄平衡锤" });
            items.Add(new SKConfig.SKItem() { NetID = 5600, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 15, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 29, Note = "蓝溜冰鞋" });
            items.Add(new SKConfig.SKItem() { NetID = 5640, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 10, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 30, Note = "绿溜冰鞋" });
            items.Add(new SKConfig.SKItem() { NetID = 5641, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 10, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 31, Note = "经典溜冰鞋" });
            items.Add(new SKConfig.SKItem() { NetID = 5642, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 10, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 32, Note = "派对溜冰鞋" });
            items.Add(new SKConfig.SKItem() { NetID = 3316, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 20, Platinum = 0 }, Conditions = new[] { "肉后" }, Page = 1, SlotIndex = 33, Note = "渐变球" });
            items.Add(new SKConfig.SKItem() { NetID = 3334, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 50, Platinum = 0 }, Conditions = new[] { "肉后" }, Page = 1, SlotIndex = 34, Note = "悠悠球手套" });
            items.Add(new SKConfig.SKItem() { NetID = 5540, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 0, Platinum = 1 }, Conditions = new[] { "击败毁灭者", "击败双子魔眼", "击败机械骷髅王" }, Page = 1, SlotIndex = 35, Note = "魔法绳" });
            items.Add(new SKConfig.SKItem() { NetID = 3258, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 25, Platinum = 0 }, Conditions = new[] { "肉后" }, Page = 1, SlotIndex = 36, Note = "拍拍手" });
            items.Add(new SKConfig.SKItem() { NetID = 3043, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 10, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 37, Note = "魔法灯笼" });
            items.Add(new SKConfig.SKItem() { NetID = 5326, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 10, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 38, Note = "工匠面包" });
            items.Add(new SKConfig.SKItem() { NetID = 757,  Prefix = 81, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 0, Platinum = 5 }, Conditions = new[] { "花后" }, Page = 1, SlotIndex = 39, Note = "泰拉刃" });
            items.Add(new SKConfig.SKItem() { NetID = 75,   Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 25, Gold = 0, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 1, SlotIndex = 40, Note = "坠落之星" });

            // ========== 第 2 页（23 个物品） ==========
            items.Add(new SKConfig.SKItem() { NetID = 29,   Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 10, Platinum = 0 }, Conditions = new[] { "肉后" }, Page = 2, SlotIndex = 1, Note = "生命水晶" });
            items.Add(new SKConfig.SKItem() { NetID = 1293, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 25, Platinum = 0 }, Conditions = new[] { "花后" }, Page = 2, SlotIndex = 2, Note = "丛林蜥蜴电池" });
            items.Add(new SKConfig.SKItem() { NetID = 1328, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 50, Platinum = 0 }, Conditions = new[] { "花后" }, Page = 2, SlotIndex = 3, Note = "海龟壳" });
            items.Add(new SKConfig.SKItem() { NetID = 1253, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 50, Platinum = 0 }, Conditions = new[] { "肉后" }, Page = 2, SlotIndex = 4, Note = "冰冻海龟壳" });
            items.Add(new SKConfig.SKItem() { NetID = 156,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 50, Platinum = 0 }, Conditions = new[] { "击败骷髅王" }, Page = 2, SlotIndex = 5, Note = "钴护盾" });
            items.Add(new SKConfig.SKItem() { NetID = 155,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 50, Platinum = 0 }, Conditions = new[] { "击败骷髅王" }, Page = 2, SlotIndex = 6, Note = "村正" });
            items.Add(new SKConfig.SKItem() { NetID = 164,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 50, Platinum = 0 }, Conditions = new[] { "击败骷髅王" }, Page = 2, SlotIndex = 7, Note = "手枪" });
            items.Add(new SKConfig.SKItem() { NetID = 1254, Prefix = 82, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 0, Platinum = 2 }, Conditions = new[] { "花后" }, Page = 2, SlotIndex = 8, Note = "狙击步枪" });
            items.Add(new SKConfig.SKItem() { NetID = 938,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 50, Platinum = 0 }, Conditions = new[] { "花后" }, Page = 2, SlotIndex = 9, Note = "圣骑士护盾" });
            items.Add(new SKConfig.SKItem() { NetID = 1326, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 0, Platinum = 5 }, Conditions = new[] { "肉后" }, Page = 2, SlotIndex = 10, Note = "混沌传送杖" });
            items.Add(new SKConfig.SKItem() { NetID = 3016, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 50, Platinum = 0 }, Conditions = new[] { "肉后" }, Page = 2, SlotIndex = 11, Note = "血肉指虎" });
            items.Add(new SKConfig.SKItem() { NetID = 897,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 50, Platinum = 0 }, Conditions = new[] { "肉后" }, Page = 2, SlotIndex = 12, Note = "强力手套" });
            items.Add(new SKConfig.SKItem() { NetID = 860,  Prefix = 65, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 30, Platinum = 0 }, Conditions = new[] { "肉后" }, Page = 2, SlotIndex = 13, Note = "神话护身符" });
            items.Add(new SKConfig.SKItem() { NetID = 984,  Prefix = 65, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 75, Platinum = 0 }, Conditions = new[] { "花后" }, Page = 2, SlotIndex = 14, Note = "忍者大师装备" });
            items.Add(new SKConfig.SKItem() { NetID = 4269, Prefix = 85, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 0, Platinum = 1 }, Conditions = new[] { "肉后" }, Page = 2, SlotIndex = 15, Note = "血红法杖" });
            items.Add(new SKConfig.SKItem() { NetID = 1323, Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 25, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 2, SlotIndex = 16, Note = "黑曜石玫瑰" });
            items.Add(new SKConfig.SKItem() { NetID = 906,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 30, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 2, SlotIndex = 17, Note = "熔岩护身符" });
            items.Add(new SKConfig.SKItem() { NetID = 863,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 25, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 2, SlotIndex = 18, Note = "水上漂靴" });
            items.Add(new SKConfig.SKItem() { NetID = 4029,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 2, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 2, SlotIndex = 19, Note = "披萨" });
            items.Add(new SKConfig.SKItem() { NetID = 4015,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 2, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 2, SlotIndex = 20, Note = "汉堡" });
            items.Add(new SKConfig.SKItem() { NetID = 4036,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 2, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 2, SlotIndex = 21, Note = "意大利面" });
            items.Add(new SKConfig.SKItem() { NetID = 5041,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 1, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 2, SlotIndex = 22, Note = "盒装牛奶" });
            items.Add(new SKConfig.SKItem() { NetID = 4618,  Prefix = 0, Price = new SKConfig.Price { Copper = 0, Silver = 0, Gold = 1, Platinum = 0 }, Conditions = Array.Empty<string>(), Page = 2, SlotIndex = 23, Note = "桃子果酒" });

            return items.ToArray();
        }
    }
}