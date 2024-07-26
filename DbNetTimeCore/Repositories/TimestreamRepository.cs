using System.Data;
using Amazon.TimestreamQuery;
using Amazon.TimestreamQuery.Model;
using Amazon;
using DbNetTimeCore.Models;
using static DbNetTimeCore.Utilities.DbNetDataCore;

namespace DbNetTimeCore.Repositories
{
    public class TimestreamRepository : ITimestreamRepository
    {
        private readonly IConfiguration _configuration;
        public TimestreamRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<DataTable> GetRecords(GridModel gridModel)
        {
            var query = BuildQuery(gridModel);
            return await RunQuery(gridModel.ConnectionAlias, $"{query} LIMIT 1000");
        }

        public async Task<DataTable> GetColumns(GridModel gridModel)
        {
            return await RunQuery(gridModel.ConnectionAlias, $"SELECT * FROM {QuotedTableName(gridModel.TableName)} WHERE 1=2");
        }

        private string QuotedTableName(string tableName)
        {
            return $"\"{string.Join("\".\"", tableName.Split("."))}\"";
        }

        private string BuildQuery(GridModel gridModel)
        {
            string columns = "*";
            if (gridModel.Columns.Any())
            {
                columns = string.Join(",", gridModel.Columns.Select(c => c.Name).ToList());
            }

            string sql = $"select {columns} from {QuotedTableName(gridModel.TableName)}";
            QueryCommandConfig query = new QueryCommandConfig(sql);

            sql = AddFilterPart(sql, gridModel);

            if (gridModel is GridModel)
            {
                if (!string.IsNullOrEmpty(gridModel.SortKey) || !string.IsNullOrEmpty(gridModel.CurrentSortKey))
                {
                    sql = AddOrderPart(sql, gridModel);
                }
            }

            return sql;
        }

        private string AddFilterPart(string sql, ComponentModel componentModel)
        {
            var gridModel = (GridModel)componentModel;

            if (string.IsNullOrEmpty(gridModel.SearchInput) == false)
            {
                List<string> filterPart = new List<string>();

                foreach (var col in gridModel.GridColumns.Where(c => c.Searchable).Select(c => c.Name).ToList())
                {
                    filterPart.Add($"{col} like '%{gridModel.SearchInput}%'");
                }

                if (filterPart.Any())
                {
                    sql += $" where {string.Join(" or ", filterPart)}";
                }
            }

            return sql;
        }

        private string AddOrderPart(string sql, GridModel gridModel)
        {
            return $"{sql} order by {(!string.IsNullOrEmpty(gridModel.SortKey) ? gridModel.SortColumn : gridModel.CurrentSortColumn)} {gridModel.SortSequence}";
        }

        private async Task<DataTable> RunQuery(string connectionAlias, string queryString)
        {
            var amazonTimestreamQueryClient = new AmazonTimestreamQueryClient(_configuration.GetValue<string>($"{connectionAlias}:AccessKeyId"), _configuration.GetValue<string>($"{connectionAlias}:SecretAccessKey"), RegionEndpoint.GetBySystemName(_configuration.GetValue<string>($"{connectionAlias}:Region")));
            var dataTable = new DataTable();
            try
            {
                QueryRequest queryRequest = new QueryRequest();
                queryRequest.QueryString = queryString;
                QueryResponse queryResponse = await amazonTimestreamQueryClient.QueryAsync(queryRequest);
                foreach (var column in queryResponse.ColumnInfo)
                {
                    dataTable.Columns.Add(new DataColumn(column.Name, ConvertTimestreamTypeSystemType(column.Type)));
                }

                foreach (var row in queryResponse.Rows)
                {
                    dataTable.Rows.Add(ConvertToDataRow(row, dataTable));
                }

                while (queryResponse.NextToken != null)
                {
                    queryRequest.NextToken = queryResponse.NextToken;
                    queryResponse = await amazonTimestreamQueryClient.QueryAsync(queryRequest);
                    dataTable.Rows.Add(queryResponse.Rows);
                }
            }
            catch (Exception ex)
            {

            }
            amazonTimestreamQueryClient.Dispose();
            return dataTable;
        }

        private DataRow ConvertToDataRow(Row row, DataTable datatable)
        {
            DataRow dataRow = datatable.NewRow();

            int i = 0;

            foreach (var datumn in row.Data)
            {
                if (string.IsNullOrEmpty(datumn.ScalarValue?.ToString()) == false)
                {
                    switch (System.Type.GetTypeCode(datatable.Columns[i].GetType()))
                    {
                        case TypeCode.DateTime:
                            dataRow[i] = DateTime.Parse(datumn.ScalarValue.ToString());
                            break;
                        case TypeCode.Int64:
                            dataRow[i] = Int64.Parse(datumn.ScalarValue.ToString());
                            break;
                        case TypeCode.Double:
                            dataRow[i] = Double.Parse(datumn.ScalarValue.ToString());
                            break;
                        case TypeCode.Boolean:
                            dataRow[i] = Boolean.Parse(datumn.ScalarValue.ToString());
                            break;
                        default:
                            dataRow[i] = datumn.ScalarValue.ToString();
                            break;
                    }
                }

                i++;
            }

            return dataRow;
        }

        private System.Type ConvertTimestreamTypeSystemType(Amazon.TimestreamQuery.Model.Type type)
        {
            switch (type.ScalarType.Value)
            {
                case "TIMESTAMP":
                    return typeof(System.DateTime);
                case "BIGINT":
                    return typeof(System.Int64);
                case "DOUBLE":
                    return typeof(System.Double);
                case "BOOLEAN":
                    return typeof(System.Boolean);
                default:
                    return typeof(System.String);
            }
        }

    }
}
