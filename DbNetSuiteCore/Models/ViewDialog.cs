namespace DbNetSuiteCore.Models
{
    public class ViewDialog
    {
        public int LayoutColumns { get; set; } = 1;
        public int? MaxWidth { get; set; } 
        public int? MaxHeight { get; set; }
        public bool InlinePane { get; set; } = false;
        public ViewDialog() { }
    }
}
