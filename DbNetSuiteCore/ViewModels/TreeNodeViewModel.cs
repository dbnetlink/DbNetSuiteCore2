using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Html;
using System.Data;
using DataTableExtensions = DbNetSuiteCore.Extensions.DataTableExtensions;

namespace DbNetSuiteCore.ViewModels
{
    public class TreeNodeViewModel
    {
        public DataRow ParentRow { get; set; }
        public int Level { get; set; }
        public TreeViewModel TreeViewModel { get; set; }
        public TreeModel CurrentLevel => TreeViewModel.Levels[Level];
        public TreeModel ChildLevel => TreeViewModel.Levels[Level+1];
        public TreeNodeViewModel(DataRow parentRow, int level, TreeViewModel treeViewModel)
        {
            ParentRow = parentRow;
            Level = level;
            TreeViewModel = treeViewModel;
        }

        public bool HasChildren()
        {
            if (LeafLevel)
            {
                return false;
            }

            return ChildRows().Any();
        }

        public DataRow[] ChildRows()
        {
            if (CurrentLevel.DataSourceType == DataSourceType.FileSystem)
            {
                if (Convert.ToBoolean(ParentRow.RowValue(FileSystemColumn.IsDirectory)) == false)
                {
                    return Array.Empty<DataRow>();
                }
            }

            if (ChildLevel.Data.Rows.Count == 0)
            {
                return Array.Empty<DataRow>();
            }

            var primaryKeyColumn = ChildLevel.Columns.FirstOrDefault(c => c.PrimaryKey) ?? ChildLevel.Columns.First();

            List<string> filter = new List<string>() { $"{ChildLevel.ForeignKeyName} = {DataTableExtensions.Quoted(primaryKeyColumn)}{CurrentLevel.PrimaryKeyValue(ParentRow)}{DataTableExtensions.Quoted(primaryKeyColumn)}" };

            if (TreeViewModel.TreeModel.DataSourceType == DataSourceType.FileSystem) 
            {
                filter.Add($"{FileSystemColumn.Path} like '{CurrentLevel.PathValue(ParentRow)}%'");
            }

            DataRow[] childRows = Array.Empty<DataRow>();

            try
            {
                childRows = ChildLevel.Data.Select(string.Join(" and ", filter));
            }
            catch
            {
                return childRows;
            }

            return childRows;
        }

        public bool LeafLevel => Level == TreeViewModel.Levels.Count - 1;
        public bool LeafParent => Level == TreeViewModel.Levels.Count - 2;
        public bool Expand => TreeViewModel.TreeModel.Expand;

        public string ParentRowDescription()
        {
            return RowDescription(ParentRow, CurrentLevel);
        }

        public string ChildRowDescription(DataRow row)
        {
            return RowDescription(row, ChildLevel);
        }

        private object RowValue(DataRow row, TreeModel level)
        {
            return level.PrimaryKeyValue(row);
        }

        private string RowDescription(DataRow row, TreeModel level)
        {
            return level.Description(row);
        }

        public HtmlString ParentRowDataAttributes()
        {
            return RowDataAttributes(ParentRow, CurrentLevel);
        }

        public HtmlString ChildRowDataAttributes(DataRow row)
        {
            return RowDataAttributes(row, ChildLevel);
        }
        private HtmlString RowDataAttributes(DataRow row, TreeModel level)
        {
            var attributes = new Dictionary<string, string> 
            {
                {"data-value", RowValue(row, level).ToString()},
                {"data-description", RowDescription(row, level) }
            };

            var dataOnlyAttributes = level.DataOnlyAttributeValues(row);

            foreach (string key in dataOnlyAttributes.Keys)
            {
                if (attributes.ContainsKey(key) == false)
                {
                    attributes.Add(key, dataOnlyAttributes[key]);
                }
            }
            return RazorHelper.Attributes(attributes);
        }
    }
}