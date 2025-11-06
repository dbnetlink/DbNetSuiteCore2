using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Collections;

namespace DbNetSuiteCore.Plugins.Interfaces
{
    public interface IJsonTransformPlugin    
    {
        public IEnumerable Transform(GridModel gridModel, HttpContext httpContext, IConfiguration configuration); 
    }
}