using DbNetTimeCore.Models;
using System.Data;

namespace DbNetTimeCore.Repositories
{
    public interface IDbNetTimeRepository
    {
        public Task<DataTable> GetCustomers(GridParameters gridParameters);
        public Task<DataTable> GetCustomer(GridParameters gridParameters);
        public Task SaveCustomer(GridParameters gridParameters);
        public Task<DataTable> GetFilms(GridParameters gridParameters);
        public Task<DataTable> GetFilm(GridParameters gridParameters);
        public Task SaveFilm(GridParameters gridParameters);
        public Task<DataTable> GetActors(GridParameters gridParameters);
        public Task<DataTable> GetActor(GridParameters gridParameters);
    }
}