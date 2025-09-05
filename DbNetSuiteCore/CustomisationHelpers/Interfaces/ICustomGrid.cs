using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DbNetSuiteCore.CustomisationHelpers.Interfaces
{
    public interface ICustomGrid
    {
        public bool ValidateGridUpdate(GridModel gridModel, HttpContext httpContext, IConfiguration configuration);
        public void GridInitialisation(GridModel gridModel, HttpContext httpContext, IConfiguration configuration);
    }
}