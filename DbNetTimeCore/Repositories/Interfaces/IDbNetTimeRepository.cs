using DbNetTimeCore.Models;
using System.Data;

namespace DbNetTimeCore.Repositories
{
    public interface IDbNetTimeRepository
    {
        public Task<DataTable> GetCustomers(GridModel gridModel);
        public Task<DataTable> GetCustomer(FormModel formModel);
        public Task SaveCustomer(FormModel formModel);
        public Task<DataTable> GetFilms(GridModel gridModel);
        public Task<DataTable> GetFilm(FormModel formModel);
        public Task SaveFilm(FormModel formModel);
        public Task<DataTable> GetActors(GridModel gridModel);
        public Task<DataTable> GetActor(FormModel formModel);
        public Task SaveActor(FormModel formModel);
    }
}