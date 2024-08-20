namespace DbNetSuiteCore.Models
{
    public class CustomDataProvider
    {
        public string AssemblyName { get; set; }
        public string ConnectionTypeSuffix { get; set; }
        public string ConnectionTypeName => $"{AssemblyName}.{ConnectionTypeSuffix}";
        public string AdapterTypeName => $"{AssemblyName}.{ConnectionTypeSuffix.Replace("Connection", "DataAdapter")}";
        public string PackageName => AssemblyName;

        public CustomDataProvider(string assemblyName, string connectionTypeSuffix) 
        {
            AssemblyName = assemblyName;
            ConnectionTypeSuffix = connectionTypeSuffix;
        }
    }
}
