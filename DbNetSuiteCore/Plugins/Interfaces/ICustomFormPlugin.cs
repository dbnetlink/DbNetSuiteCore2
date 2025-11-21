using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DbNetSuiteCore.Plugins.Interfaces
{
    public interface ICustomFormPlugin
    {
        public bool ValidateUpdate(FormModel formModel, HttpContext httpContext, IConfiguration configuration);
        public bool ValidateDelete(FormModel formModel, HttpContext httpContext, IConfiguration configuration);
        public bool ValidateInsert(FormModel formModel, HttpContext httpContext, IConfiguration configuration);
        public void Initialisation(FormModel formModel, HttpContext httpContext, IConfiguration configuration);
        public void CustomCommit(FormModel formModel, HttpContext httpContext, IConfiguration configuration);
    }
}