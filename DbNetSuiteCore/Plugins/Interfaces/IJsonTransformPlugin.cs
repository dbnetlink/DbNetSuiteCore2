using DbNetSuiteCore.Models;
using System.Collections;

namespace DbNetSuiteCore.Plugins.Interfaces
{
    public interface IJsonTransformPlugin    
    {
        public IEnumerable Transform(GridModel gridModel); 
    }
}