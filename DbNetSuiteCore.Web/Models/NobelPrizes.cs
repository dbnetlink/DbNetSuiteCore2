namespace DbNetSuiteCore.Web.Models
{
    public class NobelPrizes
    {
        public List<NobelPrize> prizes { get; set; } = new List<NobelPrize> { };
    }
    public class NobelPrize
    {
        public string year { get; set; } = string.Empty;
        public string category { get; set; } = string.Empty;
        public List<Laureate> laureates { get; set; } = new List<Laureate> { };
    }

    public class Laureate
    {
        public string id { get; set; } = string.Empty;
        public string firstname { get; set; } = string.Empty;
        public string surname { get; set; } = string.Empty;
        public string motivation { get; set; } = string.Empty;
        public string share { get; set; } = string.Empty;
    }

    public class NobelPrizeLaureate
    { 
        public NobelPrizeLaureate(NobelPrize prize, Laureate laureate)
        {
            year = Convert.ToInt32(prize.year);
            category = prize.category;
            id = Convert.ToInt32(laureate.id);
            firstname = laureate.firstname;
            surname = laureate.surname;
            motivation = laureate.motivation;
            share = Convert.ToInt32(laureate.share);
        }

        public int year { get; set; }
        public string category { get; set; } = string.Empty;
        public int id { get; set; }
        public string firstname { get; set; } = string.Empty;
        public string surname { get; set; } = string.Empty;
        public string motivation { get; set; } = string.Empty;
        public int share { get; set; }
    }

}
