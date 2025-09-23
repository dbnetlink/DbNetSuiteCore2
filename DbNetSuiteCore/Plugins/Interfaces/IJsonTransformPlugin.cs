using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DbNetSuiteCore.CustomisationHelpers.Interfaces
{
    public interface IJsonTransformPlugin    
    {
        public object Transform(GridModel gridModel, HttpContext httpContext, IConfiguration _configuration); 
    }
}