using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public class ParquetRepository : DuckDbInMemoryRepository, IParquetRepository
    {
        public ParquetRepository(IConfiguration configuration, IWebHostEnvironment env) : base(configuration, env, Enums.DataSourceType.Parquet)
        {
        }
        public override async Task CreateTable(ComponentModel componentModel, IDbConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"CREATE TABLE {componentModel.TableName} AS FROM read_parquet('{FilePath(componentModel.Url)}')";
                cmd.ExecuteNonQuery();
            }
        }
    }
}