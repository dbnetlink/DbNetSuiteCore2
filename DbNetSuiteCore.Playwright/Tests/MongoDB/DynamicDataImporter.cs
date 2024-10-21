using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;

namespace DbNetSuiteCore.Playwright.Tests.MongoDB
{
    public class DynamicDataImporter
    {
        private readonly IMongoDatabase _database;
        private readonly JsonSerializerSettings _jsonSettings;

        public DynamicDataImporter(IMongoDatabase database)
        {
            _database = database;

            // Register conventions for date handling
            var conventionPack = new ConventionPack
        {
            new IgnoreIfNullConvention(true),
            new IgnoreExtraElementsConvention(true),
            new EnumRepresentationConvention(BsonType.String),
            new ImmutableTypeClassMapConvention(),
            new CustomDateTimeSerializationConvention(DateTimeKind.Utc)
        };
            ConventionRegistry.Register("DateTimeConventions", conventionPack, t => true);

            // Configure JSON settings
            _jsonSettings = new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.DateTime,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };
        }

        public void ImportJsonToMongoDB(string jsonContent, string collectionName)
        {
            // Parse JSON to JArray to handle dynamic content
            var jsonArray = JArray.Parse(jsonContent);

            if (!jsonArray.Any())
            {
                throw new ArgumentException("JSON array is empty");
            }

            // Convert JArray to BsonDocument array with proper date handling
            var bsonDocuments = jsonArray.Select(jObject => ConvertToBsonDocumentWithDateHandling(jObject)).ToList();

            // Get collection
            var collection = _database.GetCollection<BsonDocument>(collectionName);

            // Insert documents
            collection.InsertMany(bsonDocuments);
        }

        private BsonDocument ConvertToBsonDocumentWithDateHandling(JToken token)
        {
            var bsonDoc = new BsonDocument();

            foreach (var prop in (token as JObject).Properties())
            {
                var value = prop.Value;
                bsonDoc.Add(prop.Name, ConvertToBsonValue(value));
            }

            return bsonDoc;
        }

        private BsonValue ConvertToBsonValue(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    return ConvertToBsonDocumentWithDateHandling(token);

                case JTokenType.Array:
                    var array = new BsonArray();
                    foreach (var item in token)
                    {
                        array.Add(ConvertToBsonValue(item));
                    }
                    return array;

                case JTokenType.Date:
                    return new BsonDateTime(((DateTime)token).ToUniversalTime());

                case JTokenType.String:
                    // Try to parse string as date if it matches common date formats
                    string stringValue = token.Value<string>();
                    if (TryParseDate(stringValue, out DateTime dateValue))
                    {
                        return new BsonDateTime(dateValue);
                    }
                    return new BsonString(stringValue);

                case JTokenType.Integer:
                    return new BsonInt64(token.Value<long>());

                case JTokenType.Float:
                    return new BsonDouble(token.Value<double>());

                case JTokenType.Boolean:
                    return new BsonBoolean(token.Value<bool>());

                case JTokenType.Null:
                    return BsonNull.Value;

                default:
                    return BsonValue.Create(token.ToString());
            }
        }

        private bool TryParseDate(string value, out DateTime result)
        {
            // List of common date formats to try
            var formats = new[]
            {
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd",
            "MM/dd/yyyy HH:mm:ss",
            "MM/dd/yyyy"
            // Add more formats as needed
        };

            return DateTime.TryParseExact(
                value,
                formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal |
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out result);
        }
    }

    public class CustomDateTimeSerializationConvention : ConventionBase, IMemberMapConvention
    {
        private readonly DateTimeKind _dateTimeKind;

        public CustomDateTimeSerializationConvention(DateTimeKind dateTimeKind)
        {
            _dateTimeKind = dateTimeKind;
        }

        public void Apply(BsonMemberMap memberMap)
        {
            if (memberMap.MemberType == typeof(DateTime))
            {
                memberMap.SetSerializer(new DateTimeSerializer(_dateTimeKind));
            }
            else if (memberMap.MemberType == typeof(DateTime?))
            {
                memberMap.SetSerializer(new NullableSerializer<DateTime>(new DateTimeSerializer(_dateTimeKind)));
            }
            else if (memberMap.MemberType == typeof(DateTimeOffset))
            {
                memberMap.SetSerializer(new DateTimeOffsetSerializer());
            }
            else if (memberMap.MemberType == typeof(DateTimeOffset?))
            {
                memberMap.SetSerializer(new NullableSerializer<DateTimeOffset>(new DateTimeOffsetSerializer()));
            }
        }
    }
}
