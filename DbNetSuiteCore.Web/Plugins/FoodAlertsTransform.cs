using DbNetSuiteCore.Plugins.Interfaces;
using Newtonsoft.Json;
using DbNetSuiteCore.Models;

using System.Diagnostics.Contracts;


namespace DbNetSuiteCore.Web.Plugins
{
    public class FoodAlertsTransform : IJsonTransformPlugin
    {
        public Meta meta { get; set; }
        public List<Item> items { get; set; }

        public object Transform(GridModel gridModel, HttpContext httpContext, IConfiguration configuration)
        {
            return items.Select(i => new
            {
                Id = i.id ?? string.Empty,
                Title = i.title ?? string.Empty,
                Notation = i.notation ?? string.Empty,
                Created = i.created ?? string.Empty,
                Modified = i.modified,
                Type = string.Join(",", (i.type ?? new List<string>()).Select(t => t)),
                ShortTitle = i.shortTitle,
                Status = i.status?.label ?? string.Empty,
                AlertURL = i.alertURL,
                ReportingBusiness = GetReportingBusiness(i.reportingBusiness),
                Problem = i.problem?.FirstOrDefault()?.riskStatement ?? string.Empty,
                ProductDetails = i.productDetails?.FirstOrDefault()?.productName ?? string.Empty,
                Country = string.Join("/",(i.country ?? new List<Country>()).Select(c => c.label.FirstOrDefault() ?? string.Empty))
            });
        }

        private string GetReportingBusiness(ReportingBusiness ? reportingBusiness)
        {
            string name = reportingBusiness?.commonName ?? string.Empty;
            return name.Length < 30 ? name : string.Empty;
        }
    }

    public class Allergen
    {
        [JsonProperty("@id")]
        public string id { get; set; }
        public string label { get; set; }
    }

    public class Country
    {
        [JsonProperty("@id")]
        public string id { get; set; }
        public List<string> label { get; set; }
    }

    public class Item
    {
        [JsonProperty("@id")]
        public string id { get; set; }
        public string title { get; set; }
        public string notation { get; set; }
        public string created { get; set; }
        public DateTime modified { get; set; }
        public List<string> type { get; set; }
        public string shortTitle { get; set; }
        public Status status { get; set; }
        public string alertURL { get; set; }
        public ReportingBusiness reportingBusiness { get; set; }
        public List<Problem> problem { get; set; }
        public List<ProductDetail> productDetails { get; set; }
        public List<Country> country { get; set; }
    }

    public class Meta
    {
        [JsonProperty("@id")]
        public string id { get; set; }
        public string publisher { get; set; }
        public string license { get; set; }
        public string licenseName { get; set; }
        public string comment { get; set; }
        public string version { get; set; }
        public List<string> hasFormat { get; set; }
        public int limit { get; set; }
    }

    public class Problem
    {
        [JsonProperty("@id")]
        public string id { get; set; }
        public string riskStatement { get; set; }
        public List<Allergen> allergen { get; set; }
    }

    public class ProductDetail
    {
        [JsonProperty("@id")]
        public string id { get; set; }
        public string productName { get; set; }
    }

    public class ReportingBusiness
    {
        public string commonName { get; set; }
    }


    public class Status
    {
        [JsonProperty("@id")]
        public string id { get; set; }
        public string label { get; set; }
    }

}
