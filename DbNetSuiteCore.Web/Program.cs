using DbNetSuiteCore.Middleware;
using DbNetSuiteCore.Web.Helpers;
using Microsoft.AspNetCore.DataProtection;

using StackExchange.Redis;
using NRedisStack;
using NRedisStack.RedisStackCommands;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbNetSuiteCore();  // make web reporting part of the web application middleware

string? redisServer = builder.Configuration.GetConnectionString("RedisServer");

/*
if (string.IsNullOrEmpty(redisServer) == false)
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisServer;
        options.InstanceName = "DbNetSuiteCore";
    });
}
*/


if (string.IsNullOrEmpty(redisServer) == false)
{
    var muxer = ConnectionMultiplexer.Connect(
      new ConfigurationOptions
      {
          EndPoints = { { "redis-16198.c283.us-east-1-4.ec2.cloud.redislabs.com", 16198 } },
          User = "default",
          Password = "xfq5RSUQDSp0dDusLgU17iPmn8NrRnZt"
      } );
    builder.Services.AddDataProtection().PersistKeysToStackExchangeRedis(muxer, "DataProtection-Keys");
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseDbNetSuiteCore(); // configure web application middleware
app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapGet("/customers", () =>
    FileHelper.GetJson("/data/json/customers.json", builder.Environment));
app.MapGet("/employees", () =>
    FileHelper.GetJson("/data/json/employees.json", builder.Environment));
app.MapGet("/orders", () =>
    FileHelper.GetJson("/data/json/orders.json", builder.Environment));
app.MapGet("/superstore", () =>
    FileHelper.GetJson("/data/json/superstore.json", builder.Environment));

app.UseRouting();

app.UseAuthorization();
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action}/{id?}");

app.Run();

public partial class Program
{
}
