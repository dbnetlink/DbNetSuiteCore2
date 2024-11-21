using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.ViewModels
{
    public class GridViewDialogViewModel : ComponentViewModel
    {
        public DataRow Record => GridModel.Data.Rows[0];
        public IEnumerable<GridColumn> Columns => _gridModel.Columns.Where(gc => gc.Viewable);
        public int ColumnCount => Columns.Count();
        public IEnumerable<GridColumn> VisibleColumns => _gridModel.VisbleColumns;

        private readonly GridModel _gridModel = new GridModel();
        public GridModel GridModel => _gridModel;
       
        public GridViewDialogViewModel(GridModel gridModel) : base(gridModel)
        {
            _gridModel = gridModel;
        }
    }
}
