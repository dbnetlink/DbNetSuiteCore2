using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Plugins.Interfaces
{
    public interface ICustomGridPlugin
    {
        public bool ValidateUpdate(GridModel gridModel);
        public void Initialisation(GridModel gridModel);
        public void TransformDataTable(GridModel gridModel);
    }
}