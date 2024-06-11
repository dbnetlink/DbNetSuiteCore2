using Microsoft.Extensions.FileProviders;
using System.Net;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using System.Text;
using DbNetTimeCore.Repositories;
using Microsoft.Extensions.Configuration;
using DbNetTimeCore.Services.Interfaces;
using DbNetTimeCore.Services;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Diagnostics;

namespace DbNetLink.Middleware
{
    public class DbNetTimeCore
    {
        private RequestDelegate _next;
        private IDbNetTimeService _dbNetTimeService;


        public DbNetTimeCore(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IDbNetTimeService dbNetTimeService )
        {
            _dbNetTimeService = dbNetTimeService;
            if (context.Request.Path.ToString().EndsWith(DbNetTimeExtensions.PathExtension) == false)
            {
                await GenerateResponse(context);
            }
            else
            {
                await _next.Invoke(context);
            }
        }

        private async Task GenerateResponse(HttpContext context)
        {

            string requestContent = string.Empty;
            var request = context.Request;
            var resp = context.Response;
            /*
            if (request.Method == HttpMethods.Post && request.ContentLength > 0)
            {
                request.EnableBuffering();
                var buffer = new byte[Convert.ToInt32(request.ContentLength)];
                await request.Body.ReadAsync(buffer, 0, buffer.Length);
                requestContent = Encoding.UTF8.GetString(buffer);

                request.Body.Position = 0;  //rewinding the stream to 0
            }
            */
            string page = request.Path.ToString().Split('/')[1];

            if (page == string.Empty)
            {
                page = "index";
            }

            var response = await _dbNetTimeService.Process(context, page);

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

    public static class DbNetTimeExtensions
    {
        public static string PathExtension => ".dbnetsuite";

        public static IApplicationBuilder UseDbNetTimeCore(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DbNetTimeCore>();
        }

        public static IServiceCollection AddDbNetTimeCore(this IServiceCollection services)
        {
            services.Configure<MvcRazorRuntimeCompilationOptions>(options =>
            {
                //options.FileProviders.Clear();
                var embeddedFileProvider = new EmbeddedFileProvider(typeof(DbNetTimeCore).Assembly);
                options.FileProviders.Add(embeddedFileProvider);
            });

            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddControllersWithViews();
            services.AddHttpContextAccessor();
            services.AddScoped<IDbNetTimeService, DbNetTimeService>();
            services.AddScoped<IDbNetTimeRepository, DbNetTimeRepository>();
            services.AddScoped<RazorViewToStringRenderer>();
            
            return services;
        }
    }
}