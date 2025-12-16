using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Services;
using DbNetSuiteCore.Services.Interfaces;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.FileProviders;
using System.Globalization;
using System.Net;

namespace DbNetSuiteCore.Middleware
{
    public delegate Task<bool> FormValidationDelegate(FormModel formModel, HttpContext context, IConfiguration configuration);
    public delegate Task<bool> GridValidationDelegate(GridModel gridModel, HttpContext context, IConfiguration configuration);
    public class DbNetSuiteCore
    {
        private RequestDelegate _next;

        public static string Extension = ".dbnetsuite";


        public DbNetSuiteCore(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IResourceService resourceService, GridService gridService, SelectService selectService, FormService formService)
        {
            if (context.Request.Path.ToString().EndsWith(Extension))
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

            string page = request.Path.ToString().Split('/').Last().Replace(Extension, string.Empty);

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
                await resp.WriteAsync(response?.ToString() ?? string.Empty);
            }
        }
    }

    public static class DbNetSuiteCoreExtensions
    {
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
            services.AddScoped<IOracleRepository, OracleRepository>();
            services.AddSingleton<DataProtectionService>();
            services.AddScoped<ICacheService, CacheService>();

            return services;
        }
        public static IApplicationBuilder UseDbNetSuiteCore(this WebApplication app, Culture? culture = null)
        {
            if (culture.HasValue)
            {
                string locale = culture.Value.ToString().Replace("_", "-");
                RequestLocalizationOptions localizationOptions = new RequestLocalizationOptions
                {
                    SupportedCultures = new List<CultureInfo> { new CultureInfo(locale) },
                    SupportedUICultures = new List<CultureInfo> { new CultureInfo(locale) },
                    DefaultRequestCulture = new RequestCulture(locale)
                };

                app.UseRequestLocalization(localizationOptions);
            }

            return app.UseMiddleware<DbNetSuiteCore>();
        }
    }
}