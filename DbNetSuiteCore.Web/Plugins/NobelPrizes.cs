using DbNetSuiteCore.Plugins.Interfaces;
using DbNetSuiteCore.Models;
using System.Collections;

namespace DbNetSuiteCore.Web.Plugins
{
    public class NobelPrizeTransform : IJsonTransformPlugin
    {
        public List<NobelPrize>? prizes { get; set; }

        public IEnumerable Transform(GridModel gridModel)
        {
            List<TransformedNobelPrizeList> list = (prizes ?? new List<NobelPrize>()).Select(p => new TransformedNobelPrizeList((p.year ?? string.Empty), (p.category ?? string.Empty), p.laureates ?? new List<Laureate>()) { }).ToList();
            return list;
        }
    }

    public class NobelPrize
    {
        public string? year { get; set; }
        public string? category { get; set; }
        public List<Laureate>? laureates { get; set; }
    }

    public class TransformedNobelPrizeList
    {
        public TransformedNobelPrizeList(string year, string category, List<Laureate> laureates)
        {
            this.year = year;
            this.category = category;
            this.laureates = LaureatesMarkUp(laureates);

        }
        public string? year { get; set; }
        public string? category { get; set; }
        public string? laureates { get; set; }

        private string LaureatesMarkUp(List<Laureate> laureates)
        {
            if (laureates == null)
            {
                return string.Empty;
            }
            return $"{string.Join("", laureates.Select(l => $"<p><b>{l.surname}, {l.firstname}</b> - {(l.motivation??string.Empty).Replace("\"","")}</p>").ToList())}";
        }
    }

    public class Laureate
    {
        public string? id { get; set; }
        public string? firstname { get; set; }
        public string? surname { get; set; }
        public string? motivation { get; set; }
        public string? share { get; set; }
    }
}
