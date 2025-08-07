using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Repositories;
using DocumentFormat.OpenXml.Office.CoverPageProps;
using Microsoft.AspNetCore.Html;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using System.Data;
using System.Text.RegularExpressions;

namespace DbNetSuiteCore.Models
{
    public class ParentColumnModel
    {
        public string Name { get; set; } = string.Empty;
        public bool PrimaryKey { get; set; } = false;
    }
}