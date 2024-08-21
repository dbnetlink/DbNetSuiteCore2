using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;

namespace DbNetSuiteCore.Helpers
{
    public static class ColumnInfoHelper
    {
        public static List<GridColumnModel> CustomerGridColumns()
        {
            return new List<GridColumnModel>()
            {
                new GridColumnModel("customer.customer_id", "CustomerID"),
                new GridColumnModel("customer.first_name", "Forename"),
                new GridColumnModel("customer.last_name", "Surname"),
                new GridColumnModel("customer.email", "Email Address") {Format = "email" },
                new GridColumnModel("address.address", "Address"),
                new GridColumnModel("city.city", "City"),
                new GridColumnModel("address.postal_code", "Post Code"),
                new GridColumnModel("customer.active", "Active") {DataType = typeof(Boolean)},
                new GridColumnModel("customer.create_date", "Created") {Format = "dd/MM/yy", DataType = typeof(DateTime)},
                new GridColumnModel("customer.last_update", "Last Updated") {Format = "dd/MM/yy", DataType = typeof(DateTime)},
            };
        }

        public static List<GridColumnModel> FilmGridColumns()
        {
            return new List<GridColumnModel>()
            {
                new GridColumnModel("film.film_id", "FilmID"),
                new GridColumnModel("film.title", "Title"),
                new GridColumnModel("film.description", "Description") {MaxTextLength = 40},
                new GridColumnModel("film.release_year", "Year Of Release"),
                new GridColumnModel("language.name", "Language"),
                new GridColumnModel("film.rental_duration", "Duration"),
                new GridColumnModel("film.rental_rate", "Rental Rate"){Format = "C" },
                new GridColumnModel("film.length", "Length"),
                new GridColumnModel("film.replacement_cost", "Replacement Cost"){Format = "C" },
                new GridColumnModel("film.rating", "Rating") ,
                new GridColumnModel("film.special_features", "Special Features"),
                new GridColumnModel("film.last_update", "Last Updated") {Format = "dd/MM/yy", DataType = typeof(DateTime)},
            };
        }

        public static List<GridColumnModel> ActorGridColumns()
        {
            return new List<GridColumnModel>()
            {
                new GridColumnModel("actor_id", "ActorID") ,
                new GridColumnModel("first_name", "Forename"),
                new GridColumnModel("last_name", "Surname"),
                new GridColumnModel("last_update", "Last Updated") {Format = "dd/MM/yy", DataType = typeof(DateTime)},
            };
        }

    }
}