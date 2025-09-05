using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DbNetSuiteCore.CustomisationHelpers.Interfaces
{
    public interface ICustomForm
    {
        public bool ValidateFormUpdate(FormModel formModel, HttpContext httpContext, IConfiguration configuration);
        public bool ValidateFormDelete(FormModel formModel, HttpContext httpContext, IConfiguration configuration);
        public bool ValidateFormInsert(FormModel formModel, HttpContext httpContext, IConfiguration configuration);
        public void FormInitialisation(FormModel formModel, HttpContext httpContext, IConfiguration configuration);
    }
}