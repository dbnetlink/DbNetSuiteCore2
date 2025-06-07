using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Middleware;
using DbNetSuiteCore.Web.Helpers;
using Microsoft.AspNetCore.Localization;
using System.Configuration;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbNetSuiteCore();  // make web reporting part of the web application middleware
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseDbNetSuiteCore(DbNetSuiteCore.Enums.Culture.zh_Hans); // configure web application middleware
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
