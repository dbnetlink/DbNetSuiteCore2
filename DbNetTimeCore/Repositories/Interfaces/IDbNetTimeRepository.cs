using System.Data;

namespace DbNetTimeCore.Repositories
{
    public interface IDbNetTimeRepository
    {
        public DataTable GetCustomers();
    }
}