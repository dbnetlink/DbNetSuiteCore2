﻿using System.Data;
namespace DbNetTimeCore.Models
{
    public class ComponentViewModel
    {
        public IEnumerable<DataColumn> Columns { get; set; } = new List<DataColumn>();
        private ComponentModel _componentModel;

        public string SearchUrl => $"/gridcontrol.htmx?handler=search";
        public string EditUrl(DataRow row)
        {
            return $"/gridcontrol.htmx?handler=edit&pk={PrimaryKeyValue(row)}";
        }
      
        public List<GridColumnModel> ColumnInfo => _componentModel.Columns;
        public bool HasPrimaryKey => ColumnInfo.Any(c => c.IsPrimaryKey);
        public ColumnModel? PrimaryKey => ColumnInfo.FirstOrDefault(c => c.IsPrimaryKey);
        public ComponentViewModel(DataTable dataTable, ComponentModel componentModel)
        {
            _componentModel = componentModel;
            Columns = dataTable.Columns.Cast<DataColumn>();

            foreach (DataColumn column in Columns)
            {
                ColumnModel? columnInfo = _GetColumnInfo(column);

                if (columnInfo != null)
                {
                    if (columnInfo.DataType == typeof(string))
                    {
                        columnInfo.DataType = column.DataType;
                    }
                }
            }
        }


        protected ColumnModel? _GetColumnInfo(DataColumn column)
        {
            return ColumnInfo.FirstOrDefault(c => c.Name.Split(".").Last() == column.ColumnName);
        }



        public object? PrimaryKeyValue(DataRow dataRow)
        {
            if (!HasPrimaryKey)
            {
                return null;
            }

            ColumnModel column = PrimaryKey! as ColumnModel;

            DataColumn? dataColumn = Columns.FirstOrDefault(c => c.ColumnName == column.Name.Split(".").Last());

            if (dataColumn == null)
            {
                return null;
            }

            return dataRow[dataColumn];
        }
    }
}