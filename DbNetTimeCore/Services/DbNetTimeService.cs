using DbNetTime.Services.Interfaces;
using DbNetTimeCore.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection;
using System.Text;

namespace DbNetTime.Services
{
    public class DbNetTimeService : IDbNetTimeService
    {   
        public DbNetTimeService()  
        {
        }

        public async Task<Byte[]> Process(HttpContext context, string page)
        {
            switch (page.ToLower())
            {
                case "index":
                    return await Page(context, page, new IndexModel());
                case "users":
                    return await Page(context, page, new UsersModel());
            }

            return GetResource(page);
        }

        private async Task<Byte[]> Page(HttpContext context, string page, PageModel pageModel)
        {
            return Encoding.UTF8.GetBytes(await context.RenderToString($"Pages/{page}.cshtml", pageModel));
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