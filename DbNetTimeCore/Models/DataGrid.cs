namespace DbNetTimeCore.Models
{
    public class DataGrid
    {
        public List<DataColumn> Columns { get; set; } = new List<DataColumn>();
        public List<DataRow> Rows { get; set; } = new List<DataRow>();  
    }
}
