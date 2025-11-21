using DbNetSuiteCore.Plugins.Interfaces;
using DbNetSuiteCore.Models;
using System.Collections;
using Humanizer;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Web.Models
{
    public class FoodAlertTransform : IJsonTransformPlugin
    {
        public Meta? meta { get; set; }
        public List<Item>? items { get; set; }

        public IEnumerable Transform(GridModel gridModel, HttpContext httpContext, IConfiguration _configuration)
        {
            return (items ?? new List<Item>()).Select(i => new
            {

                Description = i.description,
                LastModified = i.modified,
                HumanizedLastModified = i.modified.Humanize(),
                Type = i.type,
                RelatedAlerts = i.relatedAlerts,
                ReportingBusiness = i.reportingBusiness?.commonName,
                AlertURL = i.alertURL,
                ActionTaken = i.actionTaken,
                ConsumerAdvice = i.consumerAdvice,
                Products = string.Join("", (i.productDetails ?? new List<ProductDetail>()).Select(pd => $"<p>{pd.productName}</p>").ToList())
            });
        }
    }
    public class BatchDescription
    {
        [JsonPropertyName("@id")]
        public string? id { get; set; }
        public string? bestBeforeDescription { get; set; }
        public string? batchCode { get; set; }
    }

    public class Country
    {
        [JsonPropertyName("@id")]
        public string? id { get; set; }
        public List<string>? label { get; set; }
        public List<string>? type { get; set; }
        public string? seeAlso { get; set; }
        public string? description { get; set; }
        public string? broader { get; set; }
        public string? inScheme { get; set; }
    }

    public class Item
    {
        [JsonPropertyName("@id")]
        public string? id { get; set; }
        public string? notation { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
        public string? created { get; set; }
        public DateTime?  modified { get; set; }
        public List<string>? type { get; set; }
        public List<string>? relatedAlerts { get; set; }
        public ReportingBusiness? reportingBusiness { get; set; }
        public string? SMStext { get; set; }
        public string? twitterText { get; set; }
        public string? alertURL { get; set; }
        public string? shortURL { get; set; }
        public string? actionTaken { get; set; }
        public string? consumerAdvice { get; set; }
        public List<RelatedMedium>? relatedMedia { get; set; }
        public List<Problem>? problem { get; set; }
        public List<ProductDetail>? productDetails { get; set; }
        public Status? status { get; set; }
        public List<Country>? country { get; set; }
    }

    public class Meta
    {
        [JsonPropertyName("@id")]
        public string? id { get; set; }
        public string? publisher { get; set; }
        public string? license { get; set; }
        public string? licenseName { get; set; }
        public string? comment { get; set; }
        public string? version { get; set; }
        public List<string>? hasFormat { get; set; }
        public int? limit { get; set; }
    }

    public class PathogenRisk
    {
        [JsonPropertyName("@id")]
        public string? id { get; set; }
        public List<string>? label { get; set; }
        public string? notation { get; set; }
        public List<string>? riskStatement { get; set; }
        public string? pathogen { get; set; }
        public List<string>? type { get; set; }
        public List<string>? prefLabel { get; set; }
        public object? inScheme { get; set; }
    }

    public class Problem
    {
        [JsonPropertyName("@id")]
        public string? id { get; set; }
        public string? riskStatement { get; set; }
        public PathogenRisk? pathogenRisk { get; set; }
    }

    public class ProductDetail
    {
        [JsonPropertyName("@id")]
        public string? id { get; set; }
        public string? productName { get; set; }
        public string? packSizeDescription { get; set; }
        public List<BatchDescription>? batchDescription { get; set; }
        public string? productCategory { get; set; }
    }

    public class RelatedMedium
    {
        [JsonPropertyName("@id")]
        public string? id { get; set; }
        public string? title { get; set; }
    }

    public class ReportingBusiness
    {
        public string? commonName { get; set; }
    }
  
    public class Status
    {
        [JsonPropertyName("@id")]
        public string? id { get; set; }
        public string? label { get; set; }
    }
}
