using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DbNetSuiteCore.Plugins.Interfaces
{
    public interface ICustomFormPlugin
    {
        public bool ValidateUpdate(FormModel formMode);
        public bool ValidateDelete(FormModel formMode);
        public bool ValidateInsert(FormModel formMode);
        public void Initialisation(FormModel formMode);
        public void CustomCommit(FormModel formMode);
    }
}