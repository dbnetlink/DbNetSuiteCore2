using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public class MongoDbRepository : IMongoDbRepository
    {
        public const string PrimaryKeyName = "_id";
        private readonly IConfiguration _configuration;

        private static readonly HttpClient _httpClient = new HttpClient();
        public MongoDbRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task GetRecords(ComponentModel componentModel)
        {
            var database = GetDatabase(componentModel);
            if (componentModel is FormModel)
            {
                componentModel.Data = await GetPrimaryKeyValues(database, (FormModel)componentModel);
                return;
            }
            else
            {
                componentModel.Data = await CreateDataTableFromPipeline(database, componentModel);
            }

            foreach (var column in componentModel.GetColumns().Where(c => string.IsNullOrEmpty(c.Lookup?.TableName) == false))
            {
                await GetLookupOptions(componentModel, column, database);
            }

            componentModel.ConvertEnumLookups();

            if (componentModel is GridModel)
            {
                ((GridModel)componentModel).GetDistinctLookups();
            }
        }

        public async Task<List<object>> GetPrimaryKeyValues(GridModel gridModel)
        {
            await GetRecords(gridModel);
            return gridModel.PrimaryKeyValues;
        }

        public async Task<DataTable> GetColumns(ComponentModel componentModel)
        {
            var database = GetDatabase(componentModel);
            componentModel.TableName = CheckCollectionName(database, componentModel.TableName);
            if (componentModel.GetColumns().Any() == false)
            {
                AddSchemaColumns(componentModel, database);
            }
            else
            {
                if (componentModel is FormModel)
                { 
                    var formModel = (FormModel)componentModel;
                    if (formModel.Columns.Any(c => c.Expression == MongoDbRepository.PrimaryKeyName) == false)
                    {
                        var columns = formModel.Columns.ToList();
                        columns.Add(new FormColumn(MongoDbRepository.PrimaryKeyName) { PrimaryKey = true, Autoincrement = true });
                        formModel.Columns = columns;
                    }
                }
            }
            componentModel.Data = await CreateDataTableSampleFromPipeline(database, componentModel);
            return componentModel.Data;
        }

        public async Task GetRecord(ComponentModel componentModel)
        {
            if (componentModel is FormModel)
            {
                await GetRecord((FormModel)componentModel);
                return;
            }
            var database = GetDatabase(componentModel);
            componentModel.Data = await CreateDataTableFromPipeline(database, componentModel);
            componentModel.Data.FilterWithPrimaryKey(componentModel);
            componentModel.ConvertEnumLookups();
        }

        public async Task GetRecord(FormModel formModel)
        {
            var database = GetDatabase(formModel);
            var collection = database.GetCollection<BsonDocument>(formModel.TableName);
            var filter = new BsonDocument
            {
                { PrimaryKeyName, new BsonDocument { { "$eq", PrimaryKeyValue(formModel) } } }
            };

            var options = new FindOptions<BsonDocument> { Projection = BuildProjectStage(formModel) };
            var cursor = await collection.FindAsync(filter, options);
            formModel.Data = await BuildDataTableFromCursor(cursor, formModel.GetColumns());
            formModel.ConvertEnumLookups();
        }

        private BsonValue PrimaryKeyValue(FormModel formModel)
        {
            switch (formModel.PrimaryKeyColumn!.DbDataType)
            {
                case nameof(BsonType.ObjectId):
                    return ObjectId.Parse(formModel.RecordId.ToString());
                default:
                    return formModel.RecordId?.ToString() ?? string.Empty;
            }
        }

        public async Task UpdateRecord(FormModel formModel)
        {
            CheckUpdateDisabled();
            var database = GetDatabase(formModel);
            var collection = database.GetCollection<BsonDocument>(formModel.TableName);
            var filter = Builders<BsonDocument>.Filter.Eq(MongoDbRepository.PrimaryKeyName, PrimaryKeyValue(formModel));
            var update = new BsonDocument("$set", new BsonDocument(GetValueDictionary(formModel)));
            await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = false });
        }

        public async Task UpdateRecords(GridModel gridModel)
        {
            var primaryKeysValues = await GetPrimaryKeyValues(gridModel);

            for (var r = 0; r < gridModel.FormValues[gridModel.FirstEditableColumnName].Count; r++)
            {
                var database = GetDatabase(gridModel);
                var collection = database.GetCollection<BsonDocument>(gridModel.TableName);
                var filter = Builders<BsonDocument>.Filter.Eq(MongoDbRepository.PrimaryKeyName, primaryKeysValues[r]);
                var update = new BsonDocument("$set", new BsonDocument(GetValueDictionary(gridModel,r)));
                await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = false });
            }
        }

        public async Task InsertRecord(FormModel formModel)
        {
            CheckUpdateDisabled();
            var database = GetDatabase(formModel);
            var collection = database.GetCollection<BsonDocument>(formModel.TableName);
            var insert = new BsonDocument(GetValueDictionary(formModel));
            await collection.InsertOneAsync(insert);
        }

        public async Task DeleteRecord(FormModel formModel)
        {
            CheckUpdateDisabled();
            var database = GetDatabase(formModel);
            var collection = database.GetCollection<BsonDocument>(formModel.TableName);
            var filter = Builders<BsonDocument>.Filter.Eq(MongoDbRepository.PrimaryKeyName, PrimaryKeyValue(formModel));
            await collection.DeleteOneAsync(filter);
        }

        private void CheckUpdateDisabled()
        {
            if (_configuration.ConfigValue(ConfigurationHelper.AppSetting.UpdateDisabled).ToLower() == "true")
            {
                throw new Exception("Update has been disabled by configuration");
            }
        }

        private Dictionary<string, object> GetValueDictionary(FormModel formModel)
        {
            Dictionary<string, object> values = new Dictionary<string, object>();

            foreach (FormColumn formColumn in formModel.Columns.Where(c => c.PrimaryKey == false))
            {
                if (formColumn.IsReadOnly(formModel.Mode) || formColumn.Disabled)
                {
                    continue;
                };
                if (formModel.FormValues.Keys.Contains(formColumn.ColumnName))
                {
                    if (formColumn.DbDataType == nameof(BsonType.Array))
                    {
                        string splitToken = formColumn.LookupOptions != null ? "," : "\n";
                        BsonArray bsonArray = new BsonArray();
                        formModel.FormValues[formColumn.ColumnName].Split(splitToken).ToList().ForEach(token => bsonArray.Add(token));
                        values[formColumn.Expression] = bsonArray;
                    }
                    else
                    {
                        values[formColumn.Expression] = FormModelExtensions.GetParamValue(formModel, formColumn);
                    }
                }
              
            }

            return values;
        }

        private Dictionary<string, object> GetValueDictionary(GridModel gridModel, int r)
        {
            Dictionary<string, object> values = new Dictionary<string, object>();

            foreach (GridColumn gridColumn in gridModel.Columns.Where(c => c.Editable))
            {
                if (gridModel.FormValues.ContainsKey(gridColumn.ColumnName))
                {
                    if (gridColumn.DbDataType == nameof(BsonType.Array))
                    {
                        string splitToken = gridColumn.LookupOptions != null ? "," : "\n";
                        BsonArray bsonArray = new BsonArray();
                        gridModel.FormValues[gridColumn.ColumnName][r].Split(splitToken).ToList().ForEach(token => bsonArray.Add(token));
                        values[gridColumn.Expression] = bsonArray;
                    }
                    else
                    {
                        values[gridColumn.Expression] = GridModelExtensions.GetParamValue(gridModel, gridColumn, r);
                    }
                }

            }

            return values;
        }

        private async Task<DataTable> CreateDataTableFromPipeline(IMongoDatabase database, ComponentModel componentModel, object? recordId = null)
        {
            var collection = database.GetCollection<BsonDocument>(componentModel.TableName);
            var pipeline = new List<BsonDocument>();
            if (componentModel is GridModel)
            {
                var gridModel = (GridModel)componentModel;
                pipeline.Add(new BsonDocument("$match", BuildMatchStage(gridModel)));
                if (string.IsNullOrEmpty(componentModel.SortColumnName) == false)
                {
                    pipeline.Add(new BsonDocument("$sort", BuildSortStage(gridModel)));
                }
            }
            if (componentModel is SelectModel)
            {
                var selectModel = (SelectModel)componentModel;
                pipeline.Add(new BsonDocument("$match", BuildMatchStage(selectModel)));
                pipeline.Add(new BsonDocument("$sort", BuildSortStage(selectModel)));
            }

            if (componentModel is FormModel)
            {
                var formModel = (FormModel)componentModel;
                pipeline.Add(new BsonDocument("$match", BuildMatchStage(formModel, recordId)));
                pipeline.Add(new BsonDocument("$sort", BuildSortStage(formModel)));
            }

            var projectStage = (componentModel is FormModel) ? BuildProjectStage((FormModel)componentModel, recordId) : BuildProjectStage(componentModel);

            pipeline.Add(new BsonDocument("$project", projectStage));

            if (componentModel.QueryLimit > 0)
            {
                pipeline.Add(new BsonDocument("$limit", componentModel.QueryLimit));
            }

            var cursor = await collection.AggregateAsync<BsonDocument>(pipeline);

            return await BuildDataTableFromCursor(cursor, componentModel.GetColumns());
        }

        private async Task<DataTable> CreateDataTableSampleFromPipeline(IMongoDatabase database, ComponentModel componentModel)
        {
            var collection = database.GetCollection<BsonDocument>(componentModel.TableName);
            var pipeline = new List<BsonDocument>
            {
                new BsonDocument("$project", BuildProjectStage(componentModel)),
                new BsonDocument("$limit", 100)
            };

            var cursor = await collection.AggregateAsync<BsonDocument>(pipeline);

            return await BuildDataTableFromCursor(cursor, componentModel.GetColumns());
        }
        private async Task<DataTable> BuildDataTableFromCursor(IAsyncCursor<BsonDocument> cursor, IEnumerable<ColumnModel> columns)
        {
            var dataTable = new DataTable();

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

                    column.Update(value);

                    try
                    {
                        string arrayJoin = column is FormColumn ? Environment.NewLine : "<br/>";
                        row[column.ColumnAlias] = GetColumnValue(value, arrayJoin);
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

        private async Task<DataTable> GetPrimaryKeyValues(IMongoDatabase database, FormModel formModel)
        {
            var collection = database.GetCollection<BsonDocument>(formModel.TableName);
            var filter = new BsonDocument();
            if (string.IsNullOrEmpty(formModel.SearchInput) == false)
            {
                AddSearchFilter(formModel, filter);
            }
            else
            {
                filter = new BsonDocument
                {
                    { PrimaryKeyName, new BsonDocument { { "$ne", ObjectId.Empty } } }
                };
            }

            var options = new FindOptions<BsonDocument> { Projection = new BsonDocument { { "_id", 1 } }  };

            if (formModel.QueryLimit > -1)
            {
                options.Limit = formModel.QueryLimit;
            }
            
            var cursor = await collection.FindAsync(filter, options);
            return await BuildDataTableFromCursor(cursor, formModel.GetColumns().Where(c => c.PrimaryKey));
        }

        private async Task GetLookupOptions(ComponentModel componentModel, ColumnModel column, IMongoDatabase database)
        {
            DataColumn? dataColumn = componentModel.GetDataColumn(column);

            if (dataColumn == null || componentModel.Data.Rows.Count == 0)
            {
                return;
            }

            if (column.LookupOptions == null)
            {
                column.DbLookupOptions = new List<KeyValuePair<string, string>>();
                List<object>? lookupValues = null;

                if (componentModel is GridModel)
                {
                    componentModel.Data.DefaultView.ToTable(true, dataColumn.ColumnName).Rows.Cast<DataRow>().Where(dr => dr[0] != DBNull.Value).Select(dr => dr[0]).ToList();
                }

                var lookup = column.Lookup!;

                DataTable lookupData = await CreateLookupOptionsFromPipeline(database, column, lookupValues);

                column.DbLookupOptions = lookupData.AsEnumerable().Select(row => new KeyValuePair<string, string>(row[0]?.ToString() ?? string.Empty, row[1]?.ToString() ?? string.Empty)).ToList();
            }

            componentModel.Data.ConvertLookupColumn(dataColumn, column, componentModel);
        }

        private async Task<DataTable> CreateLookupOptionsFromPipeline(IMongoDatabase database, ColumnModel columnModel, List<object>? values)
        {
            var collection = database.GetCollection<BsonDocument>(columnModel.Lookup.TableName);

            var pipeline = new List<BsonDocument>();

            if (values != null)
            {
                pipeline.Add(new BsonDocument("$match", new BsonDocument(columnModel.Lookup.KeyColumn, new BsonDocument("$in", new BsonArray(values)))));
            };

            var project = new BsonDocument
            {
                { columnModel.Lookup.KeyColumn, $"${columnModel.Lookup.KeyColumn}" }
            };

            var descriptionColumn = columnModel.Lookup.DescriptionColumn;

            if (columnModel.Lookup.DescriptionColumn.Contains(":"))
            {
                descriptionColumn = "description";
                project.AddRange(new BsonDocument(descriptionColumn, new BsonDocument("$concat", new BsonArray(FormatConcatFields(columnModel.Lookup.DescriptionColumn)))));
            }
            else
            {
                project.Add(columnModel.Lookup.DescriptionColumn, $"${descriptionColumn}");
            }

            pipeline.Add(new BsonDocument("$project", project));

            var columns = new List<ColumnModel> { new ColumnModel(columnModel.Lookup.KeyColumn), new ColumnModel(descriptionColumn) };

            var cursor = await collection.AggregateAsync<BsonDocument>(pipeline);
            return await BuildDataTableFromCursor(cursor, columns);
        }

        private List<string> FormatConcatFields(string lookupDescription)
        {
            return lookupDescription.Split(":").Select(s => char.IsLetter(s.FirstOrDefault()) ? $"${s}" : s).ToList();
        }

        private Type GetColumnDataType(BsonValue value, ColumnModel column)
        {
            return value.IsBsonNull || value.IsBsonArray ? column.DataType : BsonTypeMapper.MapToDotNetValue(value).GetType();
        }

        public object GetColumnValue(BsonValue value, string arrayJoin)
        {
            if (value.IsBsonNull || value.ToString() == string.Empty)
            {
                return DBNull.Value;
            }

            switch (value.BsonType)
            {
                case BsonType.Array:
                    return string.Join(arrayJoin, value.AsBsonArray.Select(v => v.AsString).OrderBy(v => v));
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
                    columns.Add(componentModel.NewColumn(element));
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
            name = name ?? string.Empty;
            if (list.Contains(name) == false)
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
                AddSearchFilter(gridModel, filter);
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
                        column.FilterError = ResourceHelper.GetResourceString(ResourceNames.DataFormatError);
                    }
                }

                if (match.Any())
                {
                    filter.AddRange(new BsonDocument(new Dictionary<string, object>() { { GetFilterOperator(MongoDbFilterOperator.and), match.ToArray() } }));
                }
            }

            if (string.IsNullOrEmpty(gridModel.FixedFilter) == false)
            {
                filter.AddRange(BsonDocument.Parse(gridModel.FixedFilter));
            }

            return filter;
        }

        private BsonDocument BuildMatchStage(SelectModel selectModel)
        {
            var filter = new BsonDocument();
            if (string.IsNullOrEmpty(selectModel.SearchInput) == false)
            {
                AddSearchFilter(selectModel, filter);
            }

            if (string.IsNullOrEmpty(selectModel.FixedFilter) == false)
            {
                filter.AddRange(BsonDocument.Parse(selectModel.FixedFilter));
            }

            return filter;
        }

        private BsonDocument BuildMatchStage(FormModel formModel, object? recordId = null)
        {
            var filter = new BsonDocument();

            if (recordId != null)
            {
                return new BsonDocument(formModel.Columns.First(c => c.PrimaryKey).ColumnName, new BsonDocument(GetFilterOperator(MongoDbFilterOperator.eq), PrimaryKeyValue(formModel)));
            }
            else if (string.IsNullOrEmpty(formModel.SearchInput) == false)
            {
                AddSearchFilter(formModel, filter);
            }

            return filter;
        }

        private void AddSearchFilter(ComponentModel componentModel, BsonDocument filter)
        {
            var match = new List<BsonDocument>();

            List<ColumnModel> columns = new List<ColumnModel>();

            if (componentModel is SelectModel)
            {
                columns = ((SelectModel)componentModel).SearchableColumns.Cast<ColumnModel>().ToList();
            }
            else
            {
                columns = componentModel.SearchableColumns.ToList();
            }

            foreach (var expression in columns.Select(c => c.Expression).ToList())
            {
                match.Add(new BsonDocument(expression, new BsonDocument(GetFilterOperator(MongoDbFilterOperator.regex), new BsonRegularExpression(componentModel.SearchInput, "i"))));
            }

            if (match.Any())
            {
                filter.AddRange(new BsonDocument(new Dictionary<string, object>() { { GetFilterOperator(MongoDbFilterOperator.or), match.ToArray() } }));
            }
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
                    return new ColumnFilter(MongoDbFilterOperator.eq, ComponentModelExtensions.ParseBoolean(filterColumnValue));
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

        private BsonDocument BuildSortStage(GridModel gridModel)
        {
            return new BsonDocument
            {
                { gridModel.SortColumnName, gridModel.SortSequence == Enums.SortOrder.Asc ? 1 : -1 }
            };
        }

        private BsonDocument BuildSortStage(FormModel formModel)
        {
            string columnName = formModel.Columns.First(c => c.PrimaryKey).ColumnName;
            var sequence = 1;

            var initialSortColumn = formModel.Columns.FirstOrDefault(c => c.InitialSortOrder.HasValue);

            if (initialSortColumn != null)
            {
                columnName = initialSortColumn.ColumnName;
                sequence = initialSortColumn.InitialSortOrder == Enums.SortOrder.Asc ? 1 : -1;
            }

            return new BsonDocument
            {
                { columnName, sequence }
            };
        }

        private BsonDocument BuildSortStage(SelectModel selectModel)
        {
            return selectModel.IsGrouped ? new BsonDocument { { selectModel.OptionGroupColumn.ColumnName, 1 }, { selectModel.SortColumnName, 1 } } : new BsonDocument { { selectModel.SortColumnName, 1 } };
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

        private BsonDocument BuildProjectStage(FormModel formModel, object? recordId = null)
        {
            var project = new BsonDocument();

            foreach (var column in formModel.GetColumns())
            {
                project.Add(column.ColumnAlias, $"${column.Expression}");
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
