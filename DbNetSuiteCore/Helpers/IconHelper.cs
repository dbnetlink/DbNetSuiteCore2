using Microsoft.AspNetCore.Html;

namespace DbNetSuiteCore.Helpers
{
    public static class IconHelper
    {
        public const string MaterialSvgTemplate = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"{colour}\"><path d=\"{data}\" /></svg>";

        public static HtmlString ArrowDownIcon()
        {
            return MaterialSVG("M480-344 240-584l56-56 184 184 184-184 56 56-240 240Z");
        }

        public static HtmlString ArrowUpIcon()
        {
            return MaterialSVG("M480-528 296-344l-56-56 240-240 240 240-56 56-184-184Z");
        }

        public static HtmlString InfoIcon()
        {
            return MaterialSVG("M440-280h80v-240h-80v240Zm40-320q17 0 28.5-11.5T520-640q0-17-11.5-28.5T480-680q-17 0-28.5 11.5T440-640q0 17 11.5 28.5T480-600Zm0 520q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Zm0-80q134 0 227-93t93-227q0-134-93-227t-227-93q-134 0-227 93t-93 227q0 134 93 227t227 93Zm0-320Z");
        }

        public static HtmlString SearchIcon()
        {
            return MaterialSVG("M784-120 532-372q-30 24-69 38t-83 14q-109 0-184.5-75.5T120-580q0-109 75.5-184.5T380-840q109 0 184.5 75.5T640-580q0 44-14 83t-38 69l252 252-56 56ZM380-400q75 0 127.5-52.5T560-580q0-75-52.5-127.5T380-760q-75 0-127.5 52.5T200-580q0 75 52.5 127.5T380-400Z");
        }

        public static HtmlString FirstIcon()
        {
            return MaterialSVG("M240-240v-480h80v480h-80Zm440 0L440-480l240-240 56 56-184 184 184 184-56 56Z");
        }

        public static HtmlString LastIcon()
        {
            return MaterialSVG("m280-240-56-56 184-184-184-184 56-56 240 240-240 240Zm360 0v-480h80v480h-80Z");
        }

        public static HtmlString PreviousIcon()
        {
            return MaterialSVG("M560-240 320-480l240-240 56 56-184 184 184 184-56 56Z");
        }

        public static HtmlString NextIcon()
        {
            return MaterialSVG("M504-480 320-664l56-56 240 240-240 240-56-56 184-184Z");
        }

        public static HtmlString CheckedIcon()
        {
            return MaterialSVG("m424-312 282-282-56-56-226 226-114-114-56 56 170 170ZM200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Zm0-560v560-560Z", "#666666");
        }

        public static HtmlString UncheckedIcon()
        {
            return MaterialSVG("M200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Z", "#666666");
        }

        public static HtmlString CopyIcon()
        {
            return MaterialSVG("M360-240q-33 0-56.5-23.5T280-320v-480q0-33 23.5-56.5T360-880h360q33 0 56.5 23.5T800-800v480q0 33-23.5 56.5T720-240H360Zm0-80h360v-480H360v480ZM200-80q-33 0-56.5-23.5T120-160v-560h80v560h440v80H200Zm160-240v-480 480Z");
        }

        public static HtmlString DownloadIcon()
        {
            return MaterialSVG("M480-320 280-520l56-58 104 104v-326h80v326l104-104 56 58-200 200ZM240-160q-33 0-56.5-23.5T160-240v-120h80v120h480v-120h80v120q0 33-23.5 56.5T720-160H240Z");
        }

        private static HtmlString MaterialSVG(string data, string colour = "#336699")
        {
            return new HtmlString(IconHelper.MaterialSvgTemplate.Replace("{data}", data).Replace("{colour}", colour));
        }
    }
}
