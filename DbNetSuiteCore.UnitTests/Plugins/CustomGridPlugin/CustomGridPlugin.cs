using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.UnitTests.Plugins.CustomGridPlugin
{
    public class CustomGridPlugin : CustomGridPluginBase    
    {
        public override void Initialisation(GridModel gridModel)
        {
            gridModel.Columns = new List<GridColumn>() { new GridColumn("TEST") };
        }
        public override bool ValidateUpdate(GridModel gridModel)
        {
            return true;
        }
        public override void TransformDataTable(GridModel gridModel)
        {
            foreach (DataRow row in gridModel.Data.Rows)
            {
                foreach (DataColumn column in gridModel.Data.Columns)
                {
                    row[column] = "TRANSFORMED";
                }
            }
        }
    }
}
