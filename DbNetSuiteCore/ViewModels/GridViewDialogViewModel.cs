using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.ViewModels
{
    public class GridViewDialogViewModel : ComponentViewModel
    {
        public IEnumerable<GridColumn> Columns => _gridModel.Columns.Where(gc => gc.Viewable);
        public IEnumerable<GridColumn> VisibleColumns => _gridModel.VisbleColumns;

        private readonly GridModel _gridModel = new GridModel();
        public GridModel GridModel => _gridModel;
       
        public GridViewDialogViewModel(GridModel gridModel) : base(gridModel)
        {
            _gridModel = gridModel;
        }
    }
}
