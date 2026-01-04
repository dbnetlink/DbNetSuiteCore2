namespace DbNetSuiteCore.Models
{
    public class SearchDialog
    {
        public int LayoutColumns { get; set; } = 1;
        public int? MaxWidth { get; set; } 
        public int? MaxHeight { get; set; }
        public SearchDialog() { }
    }
}
