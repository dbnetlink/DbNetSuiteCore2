using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.ViewModels
{
    public class SelectColumnViewModel : ColumnViewModel
    {
        public SelectColumn Column { get; set; }
        public SelectColumnViewModel(SelectColumn column) : base(column)
        {
            Column = column;
        }
    }
}
