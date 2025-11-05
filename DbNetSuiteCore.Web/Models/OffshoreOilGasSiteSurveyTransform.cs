using DbNetSuiteCore.Plugins.Interfaces;
using DbNetSuiteCore.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Web.Models
{
    public class OffshoreOilGasSiteSurveyTransform : IJsonTransformPlugin
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("features")]
        public List<Feature> Features { get; set; }

        public object Transform(GridModel gridModel, HttpContext httpContext, IConfiguration _configuration)
        {
            // A new anonymous object is created with a new structure
            return Features.Select(f => new
            {
                Title = f.Type,
                Id = f.Id,
                DeccRefNo = f.Properties.DeccRefNo,
                Originator = f.Properties.Originator,
                SvyStartDate = f.Properties.SvyStartDate,
                SurveyYear = f.Properties.SvyStartDate.Year,
                Abstract = f.Properties.Abstract,
                Custodian = f.Properties.Custodian,
                CustodianEmail = f.Properties.CustodianEmail,
                BgsRefNo = f.Properties.BgsRefNo,
                MdfileidNercGuid = f.Properties.MdfileidNercGuid,
                SiteSvyName = f.Properties.SiteSvyName,
                Contractor = f.Properties.Contractor,
                SvyEndDate = f.Properties.SvyEndDate,
                AdditionalInfo = f.Properties.AdditionalInfo,
                CustodianName = f.Properties.CustodianName,
                CustodianTel = f.Properties.CustodianTel,
                Geometry = f.Geometry
            });
        }
    }
    public class Feature
    {
        // These properties are for deserialization from the initial JSON
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("properties")]
        public Properties Properties { get; set; }

        [JsonPropertyName("id")]
        public decimal Id { get; set; }
        [JsonPropertyName("geometry")]
        public Geometry Geometry { get; set; }

        // This method performs the transformation using the object's own properties

    }

    public class Properties
    {
        [JsonPropertyName("decc_ref_no")]
        public string DeccRefNo { get; set; }
        [JsonPropertyName("originator")]
        public string Originator { get; set; }
        [JsonPropertyName("svy_start_date")]
        public DateTime SvyStartDate { get; set; }
        [JsonPropertyName("abstract")]
        public string Abstract { get; set; }
        [JsonPropertyName("custodian")]
        public string Custodian { get; set; }
        [JsonPropertyName("custodian_email")]
        public string CustodianEmail { get; set; }
        [JsonPropertyName("bgs_ref_no")]
        public string BgsRefNo { get; set; }
        [JsonPropertyName("mdfileid_nerc_guid")]
        public string MdfileidNercGuid { get; set; }
        [JsonPropertyName("site_svy_name")]
        public string SiteSvyName { get; set; }
        [JsonPropertyName("contractor")]
        public string Contractor { get; set; }
        [JsonPropertyName("svy_end_date")]
        public DateTime SvyEndDate { get; set; }
        [JsonPropertyName("additional_info")]
        public string AdditionalInfo { get; set; }
        [JsonPropertyName("custodian_name")]
        public string CustodianName { get; set; }
        [JsonPropertyName("custodian_tel")]
        public string CustodianTel { get; set; }
    }

    public class Geometry
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("coordinates")]
        public List<List<List<double>>> Coordinates { get; set; }
    }
}

