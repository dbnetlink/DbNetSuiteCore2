using DbNetTimeCore.Services.Interfaces;
using DbNetTimeCore.Repositories;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection;
using System.Text;
using DbNetTimeCore.Pages;
using DbNetTimeCore.Models;
using System.Data;
using Microsoft.Extensions.Primitives;

namespace DbNetTimeCore.Services
{
    public class DbNetTimeService : IDbNetTimeService
    {   
        private readonly IDbNetTimeRepository _dbNetTimeRepository;
        private readonly RazorViewToStringRenderer _razorRendererService;
        private HttpContext? _context = null;
        private bool isAjaxCall => _context.Request.Headers["hx-request"] == "true";

        public DbNetTimeService(IDbNetTimeRepository dbNetTimeRepository, RazorViewToStringRenderer razorRendererService)  
        {
            _dbNetTimeRepository = dbNetTimeRepository;
            _razorRendererService = razorRendererService;
        }

        public async Task<Byte[]> Process(HttpContext context, string page)
        {
            _context = context;
            switch (page.ToLower())
            {
                case "index":
                case "customers":
                    return await CustomersPage();
                case "users":
                    return await UsersPage();
            }

            return GetResource(page);
        }

        private async Task<Byte[]> CustomersPage()
        {
            var model = new IndexModel();

            DataTable dataTable = _dbNetTimeRepository.GetCustomers();

            model.DataGrid = new DataGrid(dataTable, "customers", GetPageNumber());

            if (isAjaxCall)
            {
                return await View("_gridRows", model.DataGrid);
            }

            return await Page("index", model);
        }

        private async Task<Byte[]> UsersPage()
        {
            return await Page("users", new UsersModel());
        }

        private async Task<Byte[]> Page(string pageName, PageModel pageModel)
        {
            return Encoding.UTF8.GetBytes(await _razorRendererService.RenderPageToStringAsync<PageModel>($"Pages/{pageName}.cshtml", pageModel));
        }

        private async Task<Byte[]> View<TModel>(string viewName, TModel model)
        {
            return Encoding.UTF8.GetBytes(await _razorRendererService.RenderViewToStringAsync($"Pages/Shared/{viewName}.cshtml", model));
        }

        private int GetPageNumber()
        {
            StringValues page = string.Empty;

            if (_context.Request.Query.TryGetValue("page", out page))
            {
            }

            try
            {
                return Convert.ToInt32(page);
            }
            catch
            {
                return 1;
            }
        }

        public Byte[] GetResource(string resource)
        {
            string folder = resource.Split(".").Last().ToUpper();
            var assembly = Assembly.GetExecutingAssembly();

            var resourceName = $"DbNetTimeCore.Resources.{folder}.{resource}";

            byte[] bytes = new byte[0];

            using (Stream stream = assembly.GetManifestResourceStream(resourceName) ?? new MemoryStream())
            using (var binaryReader = new BinaryReader(stream))
            {
                bytes = binaryReader.ReadBytes((int)stream.Length);
            }

            return bytes;
        }
    }
}