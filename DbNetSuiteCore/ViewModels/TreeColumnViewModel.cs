using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.ViewModels
{
    public class TreeColumnViewModel : ColumnViewModel
    {
        public TreeColumn Column { get; set; }
        public TreeColumnViewModel(TreeColumn column) : base(column)
        {
            Column = column;
        }
    }
}
