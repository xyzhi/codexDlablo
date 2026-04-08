using UnityEngine;

namespace Wuxing.UI
{
    public static class UIElementPalette
    {
        public static Color GetBorderColor(string element)
        {
            switch ((element ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "金":
                case "metal":
                    return Parse("#E5C36A");
                case "木":
                case "wood":
                    return Parse("#5BC16A");
                case "水":
                case "water":
                    return Parse("#59A7FF");
                case "火":
                case "fire":
                    return Parse("#FF6B5D");
                case "土":
                case "earth":
                    return Parse("#E6D25A");
                case "圣":
                case "holy":
                case "无":
                case "none":
                default:
                    return Parse("#F2F2F2");
            }
        }

        public static Color GetQualityColor(string quality)
        {
            switch ((quality ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "绿":
                case "优秀":
                case "uncommon":
                case "green":
                    return Parse("#63D66E");
                case "蓝":
                case "稀有":
                case "rare":
                case "blue":
                    return Parse("#5BA8FF");
                case "紫":
                case "史诗":
                case "epic":
                case "purple":
                    return Parse("#B77CFF");
                case "金":
                case "绝品":
                case "legendary":
                case "gold":
                    return Parse("#F0D45C");
                case "白":
                case "普通":
                case "common":
                case "white":
                default:
                    return Parse("#F2F2F2");
            }
        }

        private static Color Parse(string html)
        {
            Color color;
            return ColorUtility.TryParseHtmlString(html, out color) ? color : Color.white;
        }
    }
}
