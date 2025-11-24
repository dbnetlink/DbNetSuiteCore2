using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.UnitTests.Plugins.CustomGridPlugin
{
    public class CustomGridPluginException : CustomGridPluginBase    
    {
        public override void Initialisation(GridModel gridModel)
        {
            throw new Exception(TestExceptionMessage);
        }
        public override bool ValidateUpdate(GridModel gridModel)
        {
            throw new Exception(TestExceptionMessage);
        }
        public override void TransformDataTable(GridModel gridModel)
        {
            throw new Exception(TestExceptionMessage);
        }
    }
}
