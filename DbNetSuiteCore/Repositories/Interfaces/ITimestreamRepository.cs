﻿using TQ.Models;
using System.Data;

namespace DbNetTimeCore.Repositories
{
    public interface ITimestreamRepository
    {
        public Task<DataTable> GetRecords(GridModel gridModel);
        public Task<DataTable> GetColumns(GridModel gridModel);
    }
}