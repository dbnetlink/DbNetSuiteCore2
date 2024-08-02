using System.ComponentModel;

namespace DbNetTimeCore.Enums
{
    public enum FilmRating
    {
        PG,
        G,
        [Description("NC-17")]
        NC17,
        [Description("PG-11")]
        PG13,
        R
    }
}
