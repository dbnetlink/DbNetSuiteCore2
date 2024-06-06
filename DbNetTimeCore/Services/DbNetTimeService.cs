using DbNetTimeCore.Services.Interfaces;
using DbNetTimeCore.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection;
using System.Text;
using DbNetTimeCore.Pages;

namespace DbNetTimeCore.Services
{
    public class DbNetTimeService : IDbNetTimeService
    {   
        private readonly IDbNetTimeRepository _dbNetTimeRepository;
        private readonly RazorViewToStringRenderer _razorRendererService;
        private HttpContext? _context = null;
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
                    return await ProjectPage();
                case "users":
                    return await UsersPage();
            }

            return GetResource(page);
        }

        private async Task<Byte[]> ProjectPage()
        {
            var model = new IndexModel();
            model.Data = _dbNetTimeRepository.GetProjects();

            return await Page("index", model);
        }

        private async Task<Byte[]> UsersPage()
        {
            return await Page("users", new UsersModel());
        }

        private async Task<Byte[]> Page(string page, PageModel pageModel)
        {
            return Encoding.UTF8.GetBytes(await _razorRendererService.RenderViewToStringAsync($"Pages/{page}.cshtml", pageModel));
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