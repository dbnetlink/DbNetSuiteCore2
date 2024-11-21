using Microsoft.Extensions.FileProviders;
using System.Net;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Services.Interfaces;
using DbNetSuiteCore.Services;

namespace DbNetSuiteCore.Middleware
{
    public class DbNetSuiteCore
    {
        private RequestDelegate _next;

        private string _extension = ".htmx";


        public DbNetSuiteCore(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IResourceService resourceService, GridService gridService, SelectService selectService, FormService formService)
        {
            if (context.Request.Path.ToString().EndsWith(_extension))
            {
                await GenerateResponse(context, resourceService, gridService, selectService, formService);
            }
            else
            {
                await _next.Invoke(context);
            }
        }

        private async Task GenerateResponse(HttpContext context, IResourceService resourceService, GridService gridService, SelectService selectService, FormService formService)
        {
            var request = context.Request;
            var resp = context.Response;

            string page = request.Path.ToString().Split('/').Last().Replace(_extension,string.Empty);

            byte[] response = null;

            switch(page.ToLower())
            {
                case "gridcontrol":
                    response = await gridService.Process(context, page);
                    break;
                case "selectcontrol":
                    response = await selectService.Process(context, page);
                    break;
                case "formcontrol":
                    response = await formService.Process(context, page);
                    break;
                default:
                    response = resourceService.Process(context, page);
                    break;
            }

            if (response == null)
            {
                resp.StatusCode = (int)HttpStatusCode.NotFound;
                await resp.CompleteAsync();
            }
            else if (response is byte[])
            {
                await resp.Body.WriteAsync(response as byte[]);
            }
            else
            {
                await resp.WriteAsync(response.ToString());
            }
        }
    }

    public static class DbNetSuiteCoreExtensions
    {
        public static IApplicationBuilder UseDbNetSuiteCore(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DbNetSuiteCore>();
        }

        public static IServiceCollection AddDbNetSuiteCore(this IServiceCollection services)
        {
            services.Configure<MvcRazorRuntimeCompilationOptions>(options =>
            {
                //options.FileProviders.Clear();
                var embeddedFileProvider = new EmbeddedFileProvider(typeof(DbNetSuiteCore).Assembly);
                options.FileProviders.Add(embeddedFileProvider);
            });

            services.AddHttpContextAccessor();
            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddControllersWithViews();
            services.AddMemoryCache();
            services.AddHttpContextAccessor();
            services.AddScoped<GridService, GridService>();
            services.AddScoped<SelectService, SelectService>();
            services.AddScoped<FormService, FormService>();
            services.AddScoped<IResourceService, ResourceService>();
            services.AddScoped<IMSSQLRepository, MSSQLRepository>();
            services.AddScoped<ISQLiteRepository, SQLiteRepository>();
            services.AddScoped<IJSONRepository, JSONRepository>();
            services.AddScoped<IFileSystemRepository, FileSystemRepository>();
            services.AddScoped<RazorViewToStringRenderer>();
            services.AddScoped<IMySqlRepository, MySqlRepository>();
            services.AddScoped<IPostgreSqlRepository, PostgreSqlRepository>();
            services.AddScoped<IExcelRepository, ExcelRepository>();
            services.AddScoped<IMongoDbRepository, MongoDbRepository>();

            return services;
        }
    }
}