using System.ComponentModel;

namespace DbNetSuiteCore.Enums
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
