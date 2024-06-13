using DbNetTimeCore.Models;
using System.Data;

namespace DbNetTimeCore.Repositories
{
    public interface IDbNetTimeRepository
    {
        public DataTable GetCustomers(GridParameters gridParameters);
        public DataTable GetCustomer(GridParameters gridParameters);
        public DataTable GetFilms(GridParameters gridParameters); 
        public DataTable GetActors(GridParameters gridParameters);
    }
}