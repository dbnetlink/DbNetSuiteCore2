using DbNetSuiteCore.Models;
using System.Data;
using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Enums;
using MongoDB.Bson;
using MongoDB.Driver;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Repositories
{
    public class MongoDbRepository : IMongoDbRepository
    {
        private readonly IConfiguration _configuration;

        private static readonly HttpClient _httpClient = new HttpClient();
        public MongoDbRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task GetRecords(ComponentModel componentModel)
        {
            var database = GetDatabase(componentModel);
            componentModel.Data = await CreateDataTableFromPipeline(database, componentModel);

            if (componentModel is GridModel)
            {
                var gridModel = (GridModel)componentModel;
                foreach (var gridColumn in gridModel.Columns.Where(c => string.IsNullOrEmpty(c.Lookup?.TableName) == false && c.LookupOptions == null))
                {
                    await GetLookupOptions(gridModel, gridColumn, database);
                }
                gridModel.ConvertEnumLookups();
                gridModel.GetDistinctLookups();
            }
        }

        public async Task<DataTable> GetColumns(ComponentModel componentModel)
        {
            var database = GetDatabase(componentModel);
            componentModel.TableName = CheckCollectionName(database, componentModel.TableName);
            if (componentModel.GetColumns().Any() == false)
            {
                AddSchemaColumns(componentModel, database);
            }
            componentModel.Data = await CreateDataTableSampleFromPipeline(database, componentModel);
            return componentModel.Data;
        }

        public async Task GetRecord(GridModel gridModel)
        {
            var database = GetDatabase(gridModel);
            var dataTable = await CreateDataTableFromPipeline(database, gridModel);
            dataTable.FilterWithPrimaryKey(gridModel);
            gridModel.ConvertEnumLookups();
        }

        public void GetRecords(SelectModel selectModel)
        {
        }

        private async Task<DataTable> CreateDataTableFromPipeline(IMongoDatabase database, ComponentModel componentModel)
        {
            var collection = database.GetCollection<BsonDocument>(componentModel.TableName);
            var pipeline = new List<BsonDocument>();
            if (componentModel is GridModel)
            {
                var gridModel = (GridModel)componentModel;
                pipeline.Add(new BsonDocument("$match", BuildMatchStage(gridModel)));

            }
            if (string.IsNullOrEmpty(componentModel.SortColumnName) == false)
            {
                pipeline.Add(new BsonDocument("$sort", BuildSortStage(componentModel)));
            }
            pipeline.Add(new BsonDocument("$project", BuildProjectStage(componentModel)));

            if (componentModel.QueryLimit > 0)
            {
                pipeline.Add(new BsonDocument("$limit", componentModel.QueryLimit));
            }

            return await BuildDataTableFromCursor(pipeline, collection, componentModel.GetColumns());
        }

        private async Task<DataTable> CreateDataTableSampleFromPipeline(IMongoDatabase database, ComponentModel componentModel)
        {
            var collection = database.GetCollection<BsonDocument>(componentModel.TableName);
            var pipeline = new List<BsonDocument>
            {
                new BsonDocument("$project", BuildProjectStage(componentModel)),
                new BsonDocument("$limit", 100)
            };

            return await BuildDataTableFromCursor(pipeline, collection, componentModel.GetColumns());
        }
        private async Task<DataTable> BuildDataTableFromCursor(List<BsonDocument> pipeline, IMongoCollection<BsonDocument> collection, IEnumerable<ColumnModel> columns)
        {
            var dataTable = new DataTable();

            var cursor = await collection.AggregateAsync<BsonDocument>(pipeline);

            await cursor.ForEachAsync(document =>
            {
                if (dataTable.Rows.Count == 0)
                {
                    foreach (var column in columns)
                    {
                        var value = document.GetValue(column.ColumnAlias, BsonNull.Value);
                        dataTable.Columns.Add(column.ColumnAlias, GetColumnDataType(value, column));
                    }
                }

                var row = dataTable.NewRow();
                foreach (var column in columns)
                {
                    var value = document.GetValue(column.ColumnAlias, BsonNull.Value);
                    try
                    {
                        row[column.ColumnAlias] = GetColumnValue(value);
                    }
                    catch (Exception)
                    {
                        row[column.ColumnAlias] = DBNull.Value;
                    }

                }
                dataTable.Rows.Add(row);
            });

            if (dataTable.Rows.Count == 0)
            {
                foreach (var column in columns)
                {
                    dataTable.Columns.Add(column.ColumnAlias, GetColumnDataType(BsonNull.Value, column));
                }
            }

            return dataTable;
        }

        private async Task GetLookupOptions(GridModel gridModel, GridColumn gridColumn, IMongoDatabase database)
        {
            DataColumn? dataColumn = gridModel.GetDataColumn(gridColumn);

            gridColumn.DbLookupOptions = new List<KeyValuePair<string, string>>();

            if (dataColumn == null || gridModel.Data.Rows.Count == 0)
            {
                return;
            }

            var lookupValues = gridModel.Data.DefaultView.ToTable(true, dataColumn.ColumnName).Rows.Cast<DataRow>().Select(dr => dr[0]).ToList();

            var lookup = gridColumn.Lookup!;

            DataTable lookupData = await CreateLookupOptionsFromPipeline(database, gridColumn, gridModel, lookupValues);

            gridColumn.DbLookupOptions = lookupData.AsEnumerable().Select(row => new KeyValuePair<string, string>(row[0]?.ToString() ?? string.Empty, row[1]?.ToString() ?? string.Empty)).ToList();
            gridModel.Data.ConvertLookupColumn(dataColumn, gridColumn, gridModel);
        }

        private async Task<DataTable> CreateLookupOptionsFromPipeline(IMongoDatabase database, GridColumn gridColumn, GridModel gridModel, List<object> values)
        {
            var collection = database.GetCollection<BsonDocument>(gridColumn.Lookup.TableName);

            var pipeline = new List<BsonDocument>
            {
                new BsonDocument("$match", new BsonDocument(gridColumn.Lookup.KeyColumn, new BsonDocument("$in", new BsonArray(values))))
            };

            var project = new BsonDocument
            {
                { gridColumn.Lookup.KeyColumn, $"${gridColumn.Lookup.KeyColumn}" }
            };

            var descriptionColumn = gridColumn.Lookup.DescriptionColumn;

            if (gridColumn.Lookup.DescriptionColumn.Contains(":"))
            {
                descriptionColumn = "description";
                project.AddRange(new BsonDocument(descriptionColumn, new BsonDocument("$concat", new BsonArray(FormatConcatFields(gridColumn.Lookup.DescriptionColumn)))));
            }
            else
            {
                project.Add(gridColumn.Lookup.DescriptionColumn, $"${descriptionColumn}");
            }

            pipeline.Add(new BsonDocument("$project", project));

            var columns = new List<GridColumn> { new GridColumn(gridColumn.Lookup.KeyColumn), new GridColumn(descriptionColumn) };

            return await BuildDataTableFromCursor(pipeline, collection, columns);
        }

        private List<string> FormatConcatFields(string lookupDescription)
        {
            return lookupDescription.Split(":").Select(s => char.IsLetter(s.FirstOrDefault()) ? $"${s}" : s).ToList();
        }

        private Type GetColumnDataType(BsonValue value, ColumnModel column)
        {
            return value.IsBsonNull || value.IsBsonArray ? column.DataType : BsonTypeMapper.MapToDotNetValue(value).GetType();
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

        public void AddSchemaColumns(ComponentModel componentModel, IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>(componentModel.TableName);

            var document = collection.Find(new BsonDocument()).FirstOrDefault();
            var schema = new Dictionary<string, string>();

            List<ColumnModel> columns = new List<ColumnModel>();

            if (document != null)
            {
                foreach (var element in document.Elements)
                {
                    //schema[element.Name] = element.Value.BsonType.ToString();
                    columns.Add(new GridColumn(element.Name));
                }
            }

            componentModel.SetColumns(columns);
        }

        private IMongoDatabase GetDatabase(ComponentModel componentModel)
        {
            string connectionString = _configuration.GetConnectionString(componentModel.ConnectionAlias);

            if (connectionString == null)
            {
                connectionString = componentModel.ConnectionAlias;
            }

            var client = new MongoClient(connectionString);

            try
            {
                componentModel.DatabaseName = CheckNameList(client.ListDatabaseNames().ToList(), componentModel.DatabaseName);
            }
            catch (Exception)
            {
                throw new Exception($"Database => [{componentModel.DatabaseName}] does not exist on this connection");
            }

            return client.GetDatabase(componentModel.DatabaseName);
        }

        private string CheckCollectionName(IMongoDatabase database, string collectionName)
        {
            try
            {
                collectionName = CheckNameList(database.ListCollectionNames().ToList(), collectionName);
            }
            catch (Exception)
            {
                throw new Exception($"Collection => [{collectionName}] does not exist in database [{database.DatabaseNamespace.DatabaseName}]");
            }

            return collectionName.ToString();
        }

        private string CheckNameList(List<string> list, string name)
        {
            if (list.Contains(name.ToString()) == false)
            {
                if (list.Select(c => c.ToLower()).Contains(name.ToLower()))
                {
                    name = list.First(c => c.ToLower() == name.ToLower());
                }
                else
                {
                    throw new Exception();
                }
            }

            return name;
        }

        private BsonDocument BuildMatchStage(GridModel gridModel)
        {
            var filter = new BsonDocument();
            if (string.IsNullOrEmpty(gridModel.SearchInput) == false)
            {
                var match = new List<BsonDocument>();

                foreach (var expression in gridModel.Columns.Where(c => c.Searchable).Select(c => c.Expression).ToList())
                {
                    match.Add(new BsonDocument(expression, new BsonDocument(GetFilterOperator(MongoDbFilterOperator.regex), new BsonRegularExpression(gridModel.SearchInput, "i"))));
                }

                if (match.Any())
                {
                    filter.AddRange(new BsonDocument(new Dictionary<string, object>() { { GetFilterOperator(MongoDbFilterOperator.or), match.ToArray() } }));
                }
            }

            if (gridModel.FilterColumns.Any())
            {
                var match = new List<BsonDocument>();
                for (var i = 0; i < gridModel.ColumnFilter.Count; i++)
                {
                    if (string.IsNullOrEmpty(gridModel.ColumnFilter[i]))
                    {
                        continue;
                    }

                    var column = gridModel.Columns.Where(c => c.Filter != FilterType.None).Skip(i).First();

                    var columnFilter = ParseFilterColumnValue(gridModel.ColumnFilter[i], column);

                    if (columnFilter != null)
                    {
                        if (columnFilter.Value != null)
                        {
                            match.Add(ColumnFilterExpression(column, columnFilter));
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
                    filter.AddRange(new BsonDocument(new Dictionary<string, object>() { { GetFilterOperator(MongoDbFilterOperator.and), match.ToArray() } }));
                }
            }

            return filter;
        }

        private BsonDocument ColumnFilterExpression(GridColumn column, ColumnFilter columnFilter)
        {
            return new BsonDocument(column.Expression, new BsonDocument(GetFilterOperator(columnFilter.Operator), columnFilter.Value));
        }

        private static ColumnFilter? ParseFilterColumnValue(string filterColumnValue, GridColumn gridColumn)
        {
            MongoDbFilterOperator comparisionOperator = MongoDbFilterOperator.eq;

            if (filterColumnValue.StartsWith(">="))
            {
                comparisionOperator = MongoDbFilterOperator.gte;
            }
            else if (filterColumnValue.StartsWith("<="))
            {
                comparisionOperator = MongoDbFilterOperator.lte;
            }
            else if (filterColumnValue.StartsWith("<>") || filterColumnValue.StartsWith("!="))
            {
                comparisionOperator = MongoDbFilterOperator.ne;
            }
            else if (filterColumnValue.StartsWith(">"))
            {
                comparisionOperator = MongoDbFilterOperator.gt;
            }
            else if (filterColumnValue.StartsWith("<"))
            {
                comparisionOperator = MongoDbFilterOperator.lt;
            }
            if (comparisionOperator != MongoDbFilterOperator.eq)
            {
                var ltOrGt = new List<MongoDbFilterOperator> { MongoDbFilterOperator.lt, MongoDbFilterOperator.gt };
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
                    return new ColumnFilter(MongoDbFilterOperator.eq, GridModelExtensions.ParseBoolean(filterColumnValue));
                case nameof(DateTime):
                    try
                    {
                        return new ColumnFilter(comparisionOperator, TypedValue(gridColumn.DataTypeName, filterColumnValue));
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
                    return new ColumnFilter(MongoDbFilterOperator.regex, new BsonRegularExpression(filterColumnValue, "i"));
            }
        }

        public static BsonValue? TypedValue(string dataTypeName, object value)
        {
            try
            {
                switch (dataTypeName)
                {
                    case nameof(DateTime):
                        var d = Convert.ToDateTime(value);
                        return new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);
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
            catch (Exception)
            {
                return null;
            }
        }

        private string GetFilterOperator(MongoDbFilterOperator filterOperator)
        {
            return $"${filterOperator}";
        }

        private BsonDocument BuildSortStage(ComponentModel componentModel)
        {
            return new BsonDocument
            {
                { componentModel.SortColumnName, componentModel.SortSequence == Enums.SortOrder.Asc ? 1 : -1 }
            };
        }

        private BsonDocument BuildProjectStage(ComponentModel componentModel)
        {
            var project = new BsonDocument();
            foreach (var column in componentModel.GetColumns())
            {
                if (column.DataType == typeof(DateTime))
                {
                    //    project.Add(gridColumn.ColumnAlias, new BsonDocument("$toDate", $"${gridColumn.Expression}"));
                    var dateTruncExpression = new BsonDocument
                    {
                        { "$dateTrunc", new BsonDocument
                            {
                                { "date", new BsonDocument("$toDate", $"${column.Expression}") },
                                { "unit", "day" }
                            }
                        }
                    };
                    project.Add(column.ColumnAlias, dateTruncExpression);
                }
                else
                {
                    project.Add(column.ColumnAlias, $"${column.Expression}");
                }
            }
            return project;
        }
    }

    public class ColumnFilter
    {
        public ColumnFilter(MongoDbFilterOperator @operator, BsonValue? value)
        {
            Operator = @operator;
            Value = value;
        }

        public MongoDbFilterOperator Operator { get; set; } = MongoDbFilterOperator.eq;
        public BsonValue? Value { get; set; }
    }
}
