using DbNetSuiteCore.Enums;
using System.Data;
using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Html;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.ViewModels
{
    public class TreeViewModel : ComponentViewModel
    {
        public IEnumerable<TreeColumnViewModel> Columns => _treeModel.Columns.Select(c => new TreeColumnViewModel(c));
        private readonly TreeModel _treeModel = new TreeModel();
        public TreeModel TreeModel => _treeModel;

        public TreeViewModel(TreeModel treeModel) : base(treeModel)
        {
            _treeModel = treeModel;
        }

        public SelectColumn GetColumnInfo(DataColumn column)
        {
            return _GetColumnInfo(column, _treeModel.Columns.Cast<ColumnModel>()) as SelectColumn;
        }

        public HtmlString RenderNoRecordsOption()
        {
            return new HtmlString($"<option disabled selected value=\"\">{ResourceHelper.GetResourceString(ResourceNames.NoRecordsFound)}</option>");
        }

        public HtmlString RenderLeaf(DataRow row, int rowNumber)
        {
            return new HtmlString($" <label class=\"leaf\"><input type=\"radio\" name=\"location\" value=\"Houston\"> Houston</label>");
        }

        public List<TreeModel> Levels => _treeModel.Levels;

        public TreeNodeViewModel GetTreeNodeViewModel(DataRow parentRow, int level) => new TreeNodeViewModel(parentRow, level, this);

        public List<DataTable> TieredData()
        {
            var tables = new List<DataTable>();
            DataTable countries = new DataTable("Countries");
            countries.Columns.Add("ID", typeof(int));
            countries.Columns.Add("Name", typeof(string));
            countries.Rows.Add(1, "USA");
            countries.Rows.Add(2, "United Kingdom");
            tables.Add(countries);

            DataTable areas = new DataTable("Areas");
            areas.Columns.Add("ID", typeof(int));
            areas.Columns.Add("ParentID", typeof(int));
            areas.Columns.Add("Name", typeof(string));
            areas.Rows.Add(1, 1, "California");
            areas.Rows.Add(2, 1, "Texas");
            areas.Rows.Add(3, 2, "England");
            tables.Add(areas);

            DataTable towns = new DataTable("Towns");
            towns.Columns.Add("ID", typeof(int));
            towns.Columns.Add("ParentID", typeof(int));
            towns.Columns.Add("Name", typeof(string));
            towns.Rows.Add(1, 1, "San Fransisco");
            towns.Rows.Add(2, 1, "Los Angeles");
            towns.Rows.Add(3, 2, "Austin");
            towns.Rows.Add(4, 2, "Houston");
            towns.Rows.Add(5, 3, "London");
            towns.Rows.Add(6, 3, "Manchester");
            tables.Add(towns);


            return tables;

        }
    }
}
