using System;
using System.Collections.Generic;

namespace DbNetSuiteCore.Web.Models
{
    public class NobelPrizes
    {
        public List<NobelPrize> prizes { get; set; }
    }
    public class NobelPrize
    {
        public string year { get; set; }
        public string category { get; set; }
        public List<Laureate> laureates { get; set; }
    }

    public class Laureate
    {
        public string id { get; set; }
        public string firstname { get; set; }
        public string surname { get; set; }
        public string motivation { get; set; }
        public string share { get; set; }
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
        public string category { get; set; }
        public int id { get; set; }
        public string firstname { get; set; }
        public string surname { get; set; }
        public string motivation { get; set; }
        public int share { get; set; }
    }

}
