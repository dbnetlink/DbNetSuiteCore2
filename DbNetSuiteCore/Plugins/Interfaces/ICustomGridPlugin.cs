using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Plugins.Interfaces
{
    public interface ICustomGridPlugin
    {
        public bool ValidateUpdate(GridModel gridModel);
        public void Initialisation(GridModel gridModel);
        public void TransformDataTable(GridModel gridModel);
    }
}