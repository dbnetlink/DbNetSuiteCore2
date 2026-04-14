using Dapper;
using DbNetSuiteCore.Web.Models;

namespace DbNetSuiteCore.Web.Helpers
{
    public static class DbHelper
    {
        static public string GetJson(string name, IConfiguration configuration,IWebHostEnvironment env)
        {
            switch(name.ToLower())
            {
               case "statecity":
                    return StateCity(configuration, env);
                default:
                    return string.Empty;    
            }
        }

        private static string StateCity(IConfiguration configuration, IWebHostEnvironment env)
        {
            var connection = DbNetSuiteCore.Helpers.DbHelper.GetConnection("DbNetSuiteCore(sqlite)", DbNetSuiteCore.Enums.DataSourceType.SQLite, configuration, env);
            List<StateCity> cities = connection.Query<StateCity>("select cityid, cityname, statename from Cities c join States s on c.StateId  = s.StateId where s.CountryId in (\r\nselect countryId from countries where countrycode = 'NZ') order by statename, cityname").ToList();
            return System.Text.Json.JsonSerializer.Serialize(cities);
        }
    }
}