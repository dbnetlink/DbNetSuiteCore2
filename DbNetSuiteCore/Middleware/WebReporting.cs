﻿using Microsoft.Extensions.FileProviders;
using System.Net;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using DbNetTimeCore.Repositories;
using DbNetTimeCore.Services.Interfaces;
using DbNetTimeCore.Services;

namespace TQ.Middleware
{
    public class WebReporting
    {
        private RequestDelegate _next;
        private IReportService _reportService;
        private string _extension = ".htmx";


        public WebReporting(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IReportService reportService)
        {
            if (context.Request.Path.ToString().EndsWith(_extension))
            {
                _reportService = reportService;
                await GenerateResponse(context);
            }
            else
            {
                await _next.Invoke(context);
            }
        }

        private async Task GenerateResponse(HttpContext context)
        {
            var request = context.Request;
            var resp = context.Response;

            string page = request.Path.ToString().Split('/')[1].Replace(_extension,string.Empty);

            var response = await _reportService.Process(context, page);

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

    public static class WebReportingExtensions
    {
        public static IApplicationBuilder UseWebReporting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<WebReporting>();
        }

        public static IServiceCollection AddWebReporting(this IServiceCollection services)
        {
            services.Configure<MvcRazorRuntimeCompilationOptions>(options =>
            {
                //options.FileProviders.Clear();
                var embeddedFileProvider = new EmbeddedFileProvider(typeof(WebReporting).Assembly);
                options.FileProviders.Add(embeddedFileProvider);
            });

            services.AddHttpContextAccessor();
            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddControllersWithViews();
            services.AddHttpContextAccessor();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IMSSQLRepository, MSSQLRepository>();
            services.AddScoped<ISQLiteRepository, SQLiteRepository>();
            services.AddScoped<IJSONRepository, JSONRepository>();
            services.AddScoped<ITimestreamRepository, TimestreamRepository>();
            services.AddScoped<RazorViewToStringRenderer>();
            
            return services;
        }
    }
}