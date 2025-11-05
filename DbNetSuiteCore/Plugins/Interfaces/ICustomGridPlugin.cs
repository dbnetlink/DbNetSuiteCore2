using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DbNetSuiteCore.Plugins.Interfaces
{
    public interface ICustomGridPlugin
    {
        public bool ValidateUpdate(GridModel gridModel, HttpContext httpContext, IConfiguration configuration);
        public void Initialisation(GridModel gridModel, HttpContext httpContext, IConfiguration configuration);
    }
}