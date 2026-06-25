using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories.Interfaces
{
    /// <summary>
    /// Base repository interface for all data sources.
    /// Provides common data retrieval operations.
    /// </summary>
    public interface IRepository
    {
        /// <summary>
        /// Retrieves records for the specified component model.
        /// </summary>
        Task GetRecords(ComponentModel componentModel);

        /// <summary>
        /// Gets column metadata for the specified component model.
        /// </summary>
        Task<DataTable> GetColumns(ComponentModel componentModel);
    }
}
