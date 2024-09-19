using DbNetSuiteCore.Middleware;
using DbNetSuiteCore.Web.Helpers;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Development.json");
builder.Services.AddDbNetSuiteCore();  // make web reporting part of the web application middleware

builder.Services.AddRazorPages();

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
    FileHelper.GetJson("/data/customers.json", builder.Environment));
app.MapGet("/employees", () =>
    FileHelper.GetJson("/data/employees.json", builder.Environment));
app.MapGet("/orders", () =>
    FileHelper.GetJson("/data/orders.json", builder.Environment));

app.UseRouting();

app.UseAuthorization();
app.MapRazorPages();

app.Run();
