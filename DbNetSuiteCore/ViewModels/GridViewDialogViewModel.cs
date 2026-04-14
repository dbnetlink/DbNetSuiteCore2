using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.ViewModels
{
    public class GridViewDialogViewModel : ComponentViewModel
    {
        public DataRow Record => _gridViewModel.Rows.First();
        public IEnumerable<GridColumnViewModel> Columns => _gridViewModel.Columns.Where(c => c.Column.Viewable);
        public int ColumnCount => Columns.Count();
        public IEnumerable<GridColumnViewModel> VisibleColumns => _gridViewModel.VisibleColumns;

        private readonly GridViewModel _gridViewModel;
        public GridViewModel GridViewModel => _gridViewModel;
       
        public GridViewDialogViewModel(GridModel gridModel) : base(gridModel)
        {
            _gridViewModel = new GridViewModel(gridModel);
        }
    }
}
