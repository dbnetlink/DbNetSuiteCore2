using DbNetSuiteCore.CustomisationHelpers.Interfaces;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Web.Models
{
    public class NobelPrizeTransform : IJsonTransformPlugin
    {
        public List<NobelPrize> prizes { get; set; }

        public object Transform(GridModel gridModel, HttpContext httpContext, IConfiguration configuration)
        {
            List<TransformedNobelPrizeList> list = prizes.Select(p => new TransformedNobelPrizeList(p.year, p.category, p.laureates) { }).ToList();
            return list;
        }


    }
    public class NobelPrize
    {
        public string year { get; set; }
        public string category { get; set; }
        public List<Laureate> laureates { get; set; }
    }

    public class TransformedNobelPrizeList
    {
        public TransformedNobelPrizeList(string year, string category, List<Laureate> laureates)
        {
            this.year = year;
            this.category = category;
            this.laureates = LaureatesMarkUp(laureates);

        }
        public string year { get; set; }
        public string category { get; set; }
        public string laureates { get; set; }

        private string LaureatesMarkUp(List<Laureate> laureates)
        {
            if (laureates == null)
            {
                return string.Empty;
            }
            return $"{string.Join("", laureates.Select(l => $"<p><b>{l.surname}, {l.firstname}</b> - {l.motivation.Replace("\"","")}</p>").ToList())}";
        }
    }

    public class Laureate
    {
        public string id { get; set; }
        public string firstname { get; set; }
        public string surname { get; set; }
        public string motivation { get; set; }
        public string share { get; set; }
    }
}
