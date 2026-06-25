using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories.Interfaces;

namespace DbNetSuiteCore.Repositories
{
    public interface IJSONRepository : IRepository
    {
        public Task GetRecord(GridSelectModel gridSelectModel);
    }
}