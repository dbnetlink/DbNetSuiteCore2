using DbNetSuiteCore.Models;
using System.Data;
using DbNetSuiteCore.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Repositories
{
    public enum FilterOperator
    {
        eq,
        gt,
        gte,
        lt,
        lte,
        ne,
        nin,
        and,
        or,
        regex
    }

    public class MongoDbRepository : IMongoDbRepository
    {
        private readonly IConfiguration _configuration;

        private static readonly HttpClient _httpClient = new HttpClient();
        public MongoDbRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task GetRecords(GridModel gridModel)
        {
            var database = GetDatabase(gridModel);
            var reportConfiguration = BuildReportConfiguration(gridModel);
            gridModel.Data = await CreateDataTable(database, reportConfiguration, gridModel);
            // dataTable.FilterAndSort(gridModel, false);
            gridModel.ConvertEnumLookups();
            gridModel.GetDistinctLookups();
        }

        public async Task GetRecord(GridModel gridModel)
        {
            var database = GetDatabase(gridModel);
            var reportConfiguration = BuildReportConfiguration(gridModel);
            var dataTable = await CreateDataTable(database, reportConfiguration, gridModel);
            dataTable.FilterWithPrimaryKey(gridModel);
            gridModel.ConvertEnumLookups();
        }

        public async Task<DataTable> GetColumns(GridModel gridModel)
        {
            var database = GetDatabase(gridModel);
            if (gridModel.Columns.Any() == false)
            {
                AddSchemaColumns(gridModel, database);
            }
            var reportConfiguration = BuildReportConfiguration(gridModel);
            gridModel.Data = await CreateDataTable(database, reportConfiguration, gridModel);
            return gridModel.Data;
        }

        private async Task<DataTable> CreateDataTable(IMongoDatabase database, ReportConfiguration config, GridModel gridModel)
        {
            return await CreateDataTableFromPipeline(database, config, gridModel);
            if (config.Fields.Any(f => f.Name.Contains(".")))
            {
                return await CreateDataTableFromPipeline(database, config, gridModel);
            }
            else
            {
                return await CreateDataTableFromProjection(database, config);
            }
        }

        private async Task<DataTable> CreateDataTableFromProjection(IMongoDatabase database, ReportConfiguration config)
        {
            var collection = database.GetCollection<BsonDocument>(config.CollectionName);
            var projectionDefBuilder = Builders<BsonDocument>.Projection;
            var projection = projectionDefBuilder.Include(config.Fields.First().Name);

            foreach (Field field in config.Fields.Skip(1))
            {
                projection = projection.Include(field.Name);
            }

            IAsyncCursor<BsonDocument> cursor = await collection.Find(new BsonDocument()).Project(projection).Limit(10).ToCursorAsync();
            return await BuildDataTableFromCursor(cursor, config);
        }


        private async Task<DataTable> CreateDataTableFromPipeline(IMongoDatabase database, ReportConfiguration config, GridModel gridModel)
        {
            var collection = database.GetCollection<BsonDocument>(config.CollectionName);

            var pipeline = new List<BsonDocument>
            {
                new BsonDocument("$match", BuildMatchStage(gridModel)),
            };

            if (string.IsNullOrEmpty(gridModel.SortColumnName) == false)
            {
                pipeline.Add(new BsonDocument("$sort", BuildSortStage(gridModel)));
            }
            pipeline.Add(new BsonDocument("$project", BuildProjectStage(gridModel)));

            var cursor = await collection.AggregateAsync<BsonDocument>(pipeline);

            return await BuildDataTableFromCursor(cursor, config);
        }

        private async Task<DataTable> BuildDataTableFromCursor(IAsyncCursor<BsonDocument> cursor, ReportConfiguration config)
        {
            var dataTable = new DataTable();

            await cursor.ForEachAsync(document =>
            {
                if (dataTable.Rows.Count == 0)
                {
                    foreach (var field in config.Fields)
                    {
                        var value = document.GetValue(field.Alias, BsonNull.Value);
                        dataTable.Columns.Add(field.Alias, GetColumnDataType(value, field));
                    }
                }

                var row = dataTable.NewRow();
                foreach (var field in config.Fields)
                {
                    var value = document.GetValue(field.Alias, BsonNull.Value);
                    try
                    {
                        row[field.Alias] = GetColumnValue(value);
                    }
                    catch (Exception)
                    {
                        row[field.Alias] = DBNull.Value;
                    }

                }
                dataTable.Rows.Add(row);
            });

            if (dataTable.Rows.Count == 0)
            {
                foreach (var field in config.Fields)
                {
                    dataTable.Columns.Add(field.Alias, GetColumnDataType(BsonNull.Value, field));
                }
            }

            return dataTable;
        }

        private Type GetColumnDataType(BsonValue value, Field field)
        {
            return value.IsBsonNull || value.IsBsonArray ? field.DataType : BsonTypeMapper.MapToDotNetValue(value).GetType();
        }

        public object GetColumnValue(BsonValue value)
        {
            if (value.IsBsonNull || value.ToString() == string.Empty)
            {
                return DBNull.Value;
            }

            switch (value.BsonType)
            {
                case BsonType.Array:
                    return string.Join(", ", value.AsBsonArray.Select(value => value.AsString));
                default:
                    return BsonTypeMapper.MapToDotNetValue(value);
            }
        }

        public void AddSchemaColumns(GridModel gridModel, IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>(gridModel.TableName);

            var document = collection.Find(new BsonDocument()).FirstOrDefault();
            var schema = new Dictionary<string, string>();

            List<GridColumn> columns = new List<GridColumn>();

            if (document != null)
            {
                foreach (var element in document.Elements)
                {
                    //schema[element.Name] = element.Value.BsonType.ToString();
                    columns.Add(new GridColumn(element.Name));
                }
            }

            gridModel.Columns = columns;
        }

        private IMongoDatabase GetDatabase(GridModel gridModel)
        {
            string connectionString = _configuration.GetConnectionString(gridModel.ConnectionAlias);

            if (connectionString == null)
            {
                connectionString = gridModel.ConnectionAlias;
            }

            var client = new MongoClient(connectionString);

            return client.GetDatabase(gridModel.DatabaseName);
        }

        private BsonDocument BuildMatchStage(GridModel gridModel)
        {
            var filter = new BsonDocument();
            if (string.IsNullOrEmpty(gridModel.SearchInput) == false)
            {
                var match = new List<BsonDocument>();

                foreach (var expression in gridModel.Columns.Where(c => c.Searchable).Select(c => c.Expression).ToList())
                {
                    match.Add(new BsonDocument(expression, new BsonDocument(GetFilterOperator(FilterOperator.regex), new BsonRegularExpression(gridModel.SearchInput, "i"))));
                }

                if (match.Any())
                {
                    filter.AddRange(new BsonDocument(new Dictionary<string, object>() { { GetFilterOperator(FilterOperator.or), match.ToArray() } }));
                }
            }

            if (gridModel.Columns.Any(c => c.Filter))
            {
                var match = new List<BsonDocument>();
                for (var i = 0; i < gridModel.ColumnFilter.Count; i++)
                {
                    if (string.IsNullOrEmpty(gridModel.ColumnFilter[i]))
                    {
                        continue;
                    }

                    var column = gridModel.Columns.Where(c => c.Filter).Skip(i).First();

                    var columnFilter = ParseFilterColumnValue(gridModel.ColumnFilter[i], column);

                    if (columnFilter != null)
                    {
                        if (columnFilter.Value != null)
                        {
                            match.Add(new BsonDocument(column.Expression, new BsonDocument(GetFilterOperator(columnFilter.Operator), columnFilter.Value)));
                        }
                        else
                        {
                            column.FilterError = ResourceHelper.GetResourceString(ResourceNames.ColumnFilterNoData);
                        }
                    }
                    else
                    {
                        column.FilterError = ResourceHelper.GetResourceString(ResourceNames.ColumnFilterDataError);
                    }
                }

                if (match.Any())
                {
                    filter.AddRange(new BsonDocument(new Dictionary<string, object>() { { GetFilterOperator(FilterOperator.and), match.ToArray() } }));
                }
            }

            return filter;
        }

        private static ColumnFilter? ParseFilterColumnValue(string filterColumnValue, GridColumn gridColumn)
        {
            FilterOperator comparisionOperator = FilterOperator.eq;

            if (filterColumnValue.StartsWith(">="))
            {
                comparisionOperator = FilterOperator.gte;
            }
            else if (filterColumnValue.StartsWith("<="))
            {
                comparisionOperator = FilterOperator.lte;
            }
            else if (filterColumnValue.StartsWith("<>") || filterColumnValue.StartsWith("!="))
            {
                comparisionOperator = FilterOperator.ne;
            }
            else if (filterColumnValue.StartsWith(">"))
            {
                comparisionOperator = FilterOperator.gt;
            }
            else if (filterColumnValue.StartsWith("<"))
            {
                comparisionOperator = FilterOperator.lt;
            }
            if (comparisionOperator != FilterOperator.eq)
            {
                var ltOrGt = new List<FilterOperator> { FilterOperator.lt, FilterOperator.gt };
                filterColumnValue = filterColumnValue.Substring(ltOrGt.Contains(comparisionOperator) ? 1 : 2);
            }

            if (string.IsNullOrEmpty(filterColumnValue))
            {
                return new ColumnFilter(comparisionOperator, null);
            }

            if (gridColumn.IsNumeric)
            {
                return new ColumnFilter(comparisionOperator, TypedValue(gridColumn.DataTypeName, filterColumnValue));
            }
            switch (gridColumn.DataTypeName)
            {
                case nameof(System.Boolean):
                    return new ColumnFilter(FilterOperator.eq, GridModelExtensions.ParseBoolean(filterColumnValue));
                case nameof(DateTime):
                    try
                    {
                        return new ColumnFilter(comparisionOperator, BsonDateTime.Create(TypedValue(gridColumn.DataTypeName, filterColumnValue)));
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                case nameof(TimeSpan):
                    try
                    {
                        return new ColumnFilter(comparisionOperator, Convert.ToDateTime(TimeSpan.Parse(filterColumnValue)));
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                default:
                    return new ColumnFilter(FilterOperator.regex, new BsonRegularExpression(filterColumnValue, "i"));
            }
        }


        public static BsonValue TypedValue(string dataTypeName, object value)
        {
            //   return Convert.ToInt32(value);
            switch (dataTypeName)
            {
                case nameof(DateTime):
                    return Convert.ToDateTime(value);
                case nameof(Int16):
                    return Convert.ToInt16(value);
                case nameof(Int32):
                    return Convert.ToInt32(value);
                case nameof(Int64):
                case nameof(Int128):
                    return Convert.ToInt64(value);
                case nameof(Double):
                    return Convert.ToDouble(value);
                case nameof(Single):
                    return Convert.ToSingle(value);
                case nameof(Decimal):
                    return Convert.ToDecimal(value);
            }

            return value.ToString();
        }

        private string GetFilterOperator(FilterOperator filterOperator)
        {
            return $"${filterOperator}";
        }

        private BsonDocument BuildSortStage(GridModel gridModel)
        {
            return new BsonDocument
            {
                { gridModel.SortColumnName, gridModel.SortSequence == Enums.SortOrder.Asc ? 1 : -1 }
            };
        }

        private BsonDocument BuildProjectStage(GridModel gridModel)
        {
            var project = new BsonDocument();
            foreach (var gridColumn in gridModel.Columns)
            {
                project.Add(gridColumn.ColumnAlias, $"${gridColumn.Expression}");
            }
            return project;
        }

        private ReportConfiguration BuildReportConfiguration(GridModel gridModel)
        {
            var config = new ReportConfiguration
            {
                CollectionName = gridModel.TableName,
                Filters = new List<Filter>
                {
                    new Filter { Field = "location.address.street1", Operator = "$regex", Value = "Lone" },
                    new Filter { Field = "location.address.state", Operator = "$regex", Value = "CA" }
                },
                SortFields = new List<SortField>
                {
                    new SortField { Field = "year", Ascending = false }
                },
                Fields = gridModel.Columns.Select(gc => new Field { Name = gc.Expression, Alias = gc.ColumnAlias, DataType = gc.DataType }).ToList(),
                Limit = 100
            };

            return config;
        }
    }

    public class ColumnFilter
    {
        public ColumnFilter(FilterOperator @operator, BsonValue? value)
        {
            Operator = @operator;
            Value = value;
        }

        public FilterOperator Operator { get; set; } = FilterOperator.eq;
        public BsonValue? Value { get; set; }
    }

    public class ReportConfiguration
    {
        public string CollectionName { get; set; } = string.Empty;
        public List<Filter> Filters { get; set; } = new List<Filter>();
        public List<SortField> SortFields { get; set; } = new List<SortField>();
        public List<Field> Fields { get; set; } = new List<Field>();
        public int Limit { get; set; } = 1000;
    }

    public class Filter
    {
        public string Field { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public object Value { get; set; } = string.Empty;
    }

    public class SortField
    {
        public string Field { get; set; } = string.Empty;
        public bool Ascending { get; set; } = true;
    }

    public class Field
    {
        public string Name { get; set; } = string.Empty;
        public string Alias { get; set; } = null;
        public Type DataType { get; set; } = typeof(System.Object);
    }
}
