﻿using DbNetSuiteCore.Enums;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class SelectColumn : ColumnModel
    {
        public bool OptionGroup { get; set; } = false;

        public SelectColumn()
        {
        }
        public SelectColumn(string expression) : base(expression)
        {
        }

        internal SelectColumn(DataColumn dataColumn, DataSourceType dataSourceType) : base(dataColumn, dataSourceType)
        {
        }

        internal SelectColumn(DataRow dataRow) : base(dataRow)
        {
        }
    }
}