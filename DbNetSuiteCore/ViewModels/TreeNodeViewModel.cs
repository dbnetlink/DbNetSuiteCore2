using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Models;
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

            var primaryKeyColumn = ChildLevel.Columns.FirstOrDefault(c => c.PrimaryKey) ?? ChildLevel.Columns.First();

            string filter = $"{ChildLevel.ForeignKeyName} = {DataTableExtensions.Quoted(primaryKeyColumn)}{CurrentLevel.PrimaryKeyValue(ParentRow)}{DataTableExtensions.Quoted(primaryKeyColumn)}";
            return ChildLevel.Data.Select(filter);
        }

        public bool LeafLevel => Level == TreeViewModel.Levels.Count - 1;
        public bool LeafParent => Level == TreeViewModel.Levels.Count - 2;
        public bool Expand => TreeViewModel.TreeModel.Expand;

        public object ParentRowValue()
        {
            return CurrentLevel.PrimaryKeyValue(ParentRow);
        }
        public string ParentRowDescription()
        {
            return CurrentLevel.Description(ParentRow);
        }

        public object RowValue(DataRow row)
        {
            return ChildLevel.PrimaryKeyValue(row);
        }

        public string RowDescription(DataRow row)
        {
            return ChildLevel.Description(row);
        }
    }
}