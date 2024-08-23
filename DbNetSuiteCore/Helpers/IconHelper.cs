using Microsoft.AspNetCore.Html;
using System.Drawing;

namespace DbNetSuiteCore.Helpers
{
    public static class IconHelper
    {
        public const string MaterialSvgTemplate = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"{size}\" viewBox=\"0 -960 960 960\" width=\"{size}\" fill=\"{colour}\"><path d=\"{data}\" /></svg>";

        public static HtmlString ArrowDown()
        {
            return MaterialSVG("M480-344 240-584l56-56 184 184 184-184 56 56-240 240Z");
        }

        public static HtmlString ArrowUp()
        {
            return MaterialSVG("M480-528 296-344l-56-56 240-240 240 240-56 56-184-184Z");
        }

        public static HtmlString Info()
        {
            return MaterialSVG("M440-280h80v-240h-80v240Zm40-320q17 0 28.5-11.5T520-640q0-17-11.5-28.5T480-680q-17 0-28.5 11.5T440-640q0 17 11.5 28.5T480-600Zm0 520q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Zm0-80q134 0 227-93t93-227q0-134-93-227t-227-93q-134 0-227 93t-93 227q0 134 93 227t227 93Zm0-320Z");
        }

        public static HtmlString Search()
        {
            return MaterialSVG("M784-120 532-372q-30 24-69 38t-83 14q-109 0-184.5-75.5T120-580q0-109 75.5-184.5T380-840q109 0 184.5 75.5T640-580q0 44-14 83t-38 69l252 252-56 56ZM380-400q75 0 127.5-52.5T560-580q0-75-52.5-127.5T380-760q-75 0-127.5 52.5T200-580q0 75 52.5 127.5T380-400Z");
        }

        public static HtmlString First()
        {
            return MaterialSVG("M240-240v-480h80v480h-80Zm440 0L440-480l240-240 56 56-184 184 184 184-56 56Z");
        }

        public static HtmlString Last()
        {
            return MaterialSVG("m280-240-56-56 184-184-184-184 56-56 240 240-240 240Zm360 0v-480h80v480h-80Z");
        }

        public static HtmlString Previous()
        {
            return MaterialSVG("M560-240 320-480l240-240 56 56-184 184 184 184-56 56Z");
        }

        public static HtmlString Next()
        {
            return MaterialSVG("M504-480 320-664l56-56 240 240-240 240-56-56 184-184Z");
        }

        public static HtmlString Checked()
        {
            return MaterialSVG("m424-312 282-282-56-56-226 226-114-114-56 56 170 170ZM200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Zm0-560v560-560Z", "#666666");
        }

        public static HtmlString Unchecked()
        {
            return MaterialSVG("M200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Z", "#666666");
        }

        public static HtmlString Copy()
        {
            return MaterialSVG("M360-240q-33 0-56.5-23.5T280-320v-480q0-33 23.5-56.5T360-880h360q33 0 56.5 23.5T800-800v480q0 33-23.5 56.5T720-240H360Zm0-80h360v-480H360v480ZM200-80q-33 0-56.5-23.5T120-160v-560h80v560h440v80H200Zm160-240v-480 480Z");
        }

        public static HtmlString Download()
        {
            return MaterialSVG("M480-320 280-520l56-58 104 104v-326h80v326l104-104 56 58-200 200ZM240-160q-33 0-56.5-23.5T160-240v-120h80v120h480v-120h80v120q0 33-23.5 56.5T720-160H240Z");
        }

        public static HtmlString ExpandDown()
        {
            return MaterialSVG("m480-328 190-189-79-78-111 111-111-111-79 78 190 189Zm0 266q-87 0-162.99-32.58-75.98-32.59-132.91-89.52-56.93-56.93-89.52-132.91Q62-393 62-480q0-88 32.58-163.49 32.59-75.48 89.52-132.41 56.93-56.93 132.91-89.52Q393-898 480-898q88 0 163.49 32.58 75.48 32.59 132.41 89.52 56.93 56.93 89.52 132.41Q898-568 898-480q0 87-32.58 162.99-32.59 75.98-89.52 132.91-56.93 56.93-132.41 89.52Q568-62 480-62Zm0-118q126 0 213-87t87-213q0-126-87-213t-213-87q-126 0-213 87t-87 213q0 126 87 213t213 87Zm0-300Z", "#336699", "20px");
        }
        
        public static HtmlString ExpandUp()
        {
            return MaterialSVG("m369-365 111-111 111 111 79-78-190-189-190 189 79 78ZM480-62q-87 0-162.99-32.58-75.98-32.59-132.91-89.52-56.93-56.93-89.52-132.91Q62-393 62-480q0-88 32.58-163.49 32.59-75.48 89.52-132.41 56.93-56.93 132.91-89.52Q393-898 480-898q88 0 163.49 32.58 75.48 32.59 132.41 89.52 56.93 56.93 89.52 132.41Q898-568 898-480q0 87-32.58 162.99-32.59 75.98-89.52 132.91-56.93 56.93-132.41 89.52Q568-62 480-62Zm0-118q126 0 213-87t87-213q0-126-87-213t-213-87q-126 0-213 87t-87 213q0 126 87 213t213 87Zm0-300Z", "#336699","20px");
        }

        public static HtmlString FolderOpen()
        {
            return MaterialSVG("M160-160q-33 0-56.5-23.5T80-240v-480q0-33 23.5-56.5T160-800h240l80 80h320q33 0 56.5 23.5T880-640H447l-80-80H160v480l96-320h684L837-217q-8 26-29.5 41.5T760-160H160Zm84-80h516l72-240H316l-72 240Zm0 0 72-240-72 240Zm-84-400v-80 80Z");
        }

        public static HtmlString Folder()
        {
            return MaterialSVG("M160-160q-33 0-56.5-23.5T80-240v-480q0-33 23.5-56.5T160-800h240l80 80h320q33 0 56.5 23.5T880-640v400q0 33-23.5 56.5T800-160H160Zm0-80h640v-400H447l-80-80H160v480Zm0 0v-480 480Z");
        }

        public static HtmlString Document()
        {
            return MaterialSVG("M320-240h320v-80H320v80Zm0-160h320v-80H320v80ZM240-80q-33 0-56.5-23.5T160-160v-640q0-33 23.5-56.5T240-880h320l240 240v480q0 33-23.5 56.5T720-80H240Zm280-520v-200H240v640h480v-440H520ZM240-800v200-200 640-640Z");
        }

        private static HtmlString MaterialSVG(string data, string colour = "#336699", string size = "24px" )
        {
            return new HtmlString(IconHelper.MaterialSvgTemplate.Replace("{data}", data).Replace("{colour}", colour).Replace("{size}", size));
        }
    }
}
