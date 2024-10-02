using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Web.Helpers
{
    public static class ColumnInfoHelper
    {
        public static List<GridColumn> CustomerGridColumns()
        {
            return new List<GridColumn>()
            {
                new GridColumn("customer.customer_id", "CustomerID"),
                new GridColumn("customer.first_name", "Forename"),
                new GridColumn("customer.last_name", "Surname"),
                new GridColumn("customer.email", "Email Address") {Format = FormatType.Email },
                new GridColumn("address.address", "Address"),
                new GridColumn("city.city", "City"),
                new GridColumn("address.postal_code", "Post Code"),
                new GridColumn("customer.active", "Active") {DataType = typeof(Boolean)},
                new GridColumn("customer.create_date", "Created") {Format = "dd/MM/yy", DataType = typeof(DateTime)},
                new GridColumn("customer.last_update", "Last Updated") {Format = "dd/MM/yy", DataType = typeof(DateTime)},
            };
        }

        public static List<GridColumn> FilmGridColumns()
        {
            return new List<GridColumn>()
            {
                new GridColumn("film.film_id", "FilmID") { PrimaryKey = true},
                new GridColumn("film.title", "Title"),
                new GridColumn("film.description", "Description"),
                new GridColumn("film.release_year", "Year Of Release"),
                new GridColumn("language.name", "Language"),
                new GridColumn("film.rental_duration", "Duration"),
                new GridColumn("film.rental_rate", "Rental Rate"){Format = "C" },
                new GridColumn("film.length", "Length"),
                new GridColumn("film.replacement_cost", "Replacement Cost"){Format = "C" },
                new GridColumn("film.rating", "Rating") ,
                new GridColumn("film.special_features", "Special Features"),
                new GridColumn("film.last_update", "Last Updated") {Format = "dd/MM/yy", DataType = typeof(DateTime)},
            };
        }

        public static List<GridColumn> ActorGridColumns()
        {
            return new List<GridColumn>()
            {
                new GridColumn("actor_id", "ActorID") ,
                new GridColumn("first_name", "Forename"),
                new GridColumn("last_name", "Surname"),
                new GridColumn("last_update", "Last Updated") {Format = "dd/MM/yy", DataType = typeof(DateTime)},
            };
        }

    }
}