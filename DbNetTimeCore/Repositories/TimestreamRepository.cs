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
        private readonly AmazonTimestreamQueryClient _amazonTimestreamQueryClient;

        public TimestreamRepository(IConfiguration configuration)
        {
            _amazonTimestreamQueryClient = new AmazonTimestreamQueryClient(configuration.GetValue<string>("AWS:AccessKeyId"), configuration.GetValue<string>("AWS:SecretAccessKey"), RegionEndpoint.GetBySystemName(configuration.GetValue<string>("AWS:Region")));
        }

        public async Task<DataTable> GetRecords(string database, string table, GridModel gridModel)
        {
            return await RunQuery($"SELECT * FROM \"{database}\".\"{table}\" LIMIT 1000");
        }

        public async Task<DataTable> GetColumns(string database, string table)
        {
            return await RunQuery($"SELECT * FROM \"{database}\".\"{table}\" WHERE 1=2");
        }

        private string BuildQuery(string fromPart, ComponentModel componentModel)
        {
            string columns = "*";
            if (componentModel.Columns.Any())
            {
                columns = string.Join(",", componentModel.Columns.Select(c => c.Name).ToList());
            }

            string sql = $"select {columns} from {fromPart}";
            QueryCommandConfig query = new QueryCommandConfig(sql);

            sql = AddFilterPart(sql, componentModel);

            if (componentModel is GridModel)
            {
                var gridModel = (GridModel)componentModel;

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

            List<string> filterPart = new List<string>();

            foreach (var col in gridModel.GridColumns.Where(c => c.Searchable).Select(c => c.Name).ToList())
            {
                filterPart.Add($"{col} like '%{gridModel.SearchInput}%'");
            }

            if (filterPart.Any())
            {
               sql += $" where {string.Join(" or ", filterPart)}";
            }

            return sql;
        }

        private string AddOrderPart(string sql, GridModel gridModel)
        {
            return $"{sql} order by {(!string.IsNullOrEmpty(gridModel.SortKey) ? gridModel.SortColumn : gridModel.CurrentSortColumn)} {gridModel.SortSequence}";
        }

        private async Task<DataTable> RunQuery(string queryString)
        {
            var dataTable = new DataTable();

            try
            {
                QueryRequest queryRequest = new QueryRequest();
                queryRequest.QueryString = queryString;

                QueryResponse queryResponse = await _amazonTimestreamQueryClient.QueryAsync(queryRequest);

                foreach(var column in queryResponse.ColumnInfo)
                {
                    dataTable.Columns.Add(new DataColumn(column.Name, ConvertTimestreamTypeSystemType(column.Type)));
                }
                
                foreach(var row in queryResponse.Rows)
                {
                    dataTable.Rows.Add(ConvertToDataRow(row, dataTable));
                }

                while (queryResponse.NextToken != null)
                {
                    queryRequest.NextToken = queryResponse.NextToken;
                    queryResponse = await _amazonTimestreamQueryClient.QueryAsync(queryRequest);
                    dataTable.Rows.Add(queryResponse.Rows);
                }
            }
            catch (Exception ex)
            {
                
            }
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
