using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Html;

namespace DbNetSuiteCore.Models
{
    public class Image
    {
        public ImageType ImageType { get; set; }
        public int MaxHeight { get; set; } = 30;
        public Image() { }
        public Image(ImageType imageType, int maxHeight = 30)
        {
            ImageType = imageType;
            MaxHeight = maxHeight;
        }

        public HtmlString Img(byte[] data)
        {
            return new HtmlString($"<img src=\"data:image/{ImageType};base64,{Convert.ToBase64String(data)}\" style=\"max-height:{MaxHeight}px\"/>");
        }
    }
}
