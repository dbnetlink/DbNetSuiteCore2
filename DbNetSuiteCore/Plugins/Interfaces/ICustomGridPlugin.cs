using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DbNetSuiteCore.CustomisationHelpers.Interfaces
{
    public interface ICustomGridPlugin
    {
        public bool ValidateUpdate(GridModel gridModel, HttpContext httpContext, IConfiguration configuration);
        public void Initialisation(GridModel gridModel, HttpContext httpContext, IConfiguration configuration);
    }
}