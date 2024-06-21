using System.ComponentModel;

namespace DbNetTimeCore.Enums
{
    public enum SpecialFeature
    {

        Trailers, 
        Commentaries,
        [Description("Deleted Scenes")]
        DeletedScenes,
        [Description("Behind the Scenes")]
        BehindTheScenes
    }
}
