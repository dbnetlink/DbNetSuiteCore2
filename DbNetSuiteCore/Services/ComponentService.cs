using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using System.Data;
using System.Text;


namespace DbNetSuiteCore.Services
{
    public class ComponentService
    {
        protected readonly IMSSQLRepository _msSqlRepository;
        protected readonly ISQLiteRepository _sqliteRepository;
        protected readonly RazorViewToStringRenderer _razorRendererService;
        protected readonly IJSONRepository _jsonRepository;
        protected readonly IFileSystemRepository _fileSystemRepository;
        protected readonly IMySqlRepository _mySqlRepository;
        protected readonly IPostgreSqlRepository _postgreSqlRepository;
        protected readonly IExcelRepository _excelRepository;
        protected readonly IMongoDbRepository _mongoDbRepository;
        protected HttpContext? _context = null;

        public ComponentService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository, IFileSystemRepository fileSystemRepository, IMySqlRepository mySqlRepository, IPostgreSqlRepository postgreSqlRepository, IExcelRepository excelRepository, IMongoDbRepository mongoDbRepository)
        {
            _msSqlRepository = msSqlRepository;
            _razorRendererService = razorRendererService;
            _sqliteRepository = sqliteRepository;
            _jsonRepository = jsonRepository;
            _fileSystemRepository = fileSystemRepository;
            _mySqlRepository = mySqlRepository;
            _postgreSqlRepository = postgreSqlRepository;
            _excelRepository = excelRepository;
            _mongoDbRepository = mongoDbRepository;
        }

        protected async Task<Byte[]> View<TModel>(string viewName, TModel model)
        {
            return Encoding.UTF8.GetBytes(await _razorRendererService.RenderViewToStringAsync($"Views/{viewName}.cshtml", model));
        }

       

    }
}