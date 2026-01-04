using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using System.Collections;

namespace DbNetSuiteCore.UnitTests.Plugins.CustomGridPlugin
{
    public class CustomGridPluginNotImplemented : CustomGridPluginBase    
    {
        public override void Initialisation(GridModel gridModel)
        {
            throw new NotImplementedException();
        }
        public override bool ValidateUpdate(GridModel gridModel)
        {
            throw new NotImplementedException();
        }
        public override void TransformDataTable(GridModel gridModel)
        {             
            throw new NotImplementedException();
        }
    }
}
