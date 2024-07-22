namespace DbNetTimeCore.Models
{
    public class GridColumnModel : ColumnModel
    {
        public bool Searchable { get; set; } = false;
        public bool Editable { get; set; } = false;
        public int? MaxTextLength { get; set; }
        public GridColumnModel()
        {
        }
        public GridColumnModel(string name, string label, bool searchable = false) : base (name, label) 
        {
            Searchable = searchable;
        }

        public GridColumnModel(string name) : base(name, name)
        {
        }
    }
}
