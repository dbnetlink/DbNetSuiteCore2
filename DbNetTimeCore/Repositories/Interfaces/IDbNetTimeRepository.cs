using DbNetTimeCore.Models;
using System.Data;

namespace DbNetTimeCore.Repositories
{
    public interface IDbNetTimeRepository
    {
        public DataTable GetProjects();
    }
}