using DbNetSuiteCore.Middleware;
using DbNetSuiteCore.Web.Helpers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbNetSuiteCore();  // make web reporting part of the web application middleware
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.Configure<DbNetSuiteCoreOptions>(options =>
{
    options.FormUpdateValidationDelegate = async (formModel, httpContext, configuration) =>
    {
        return ValidationHelper.ValidateFormUpdate(formModel, httpContext, configuration);
    };
    options.FormInsertValidationDelegate = async (formModel, httpContext, configuration) =>
    {
        return ValidationHelper.ValidateFormInsert(formModel, httpContext, configuration);
    };
    options.FormDeleteValidationDelegate = async (formModel, httpContext, configuration) =>
    {
        return ValidationHelper.ValidateFormDelete(formModel, httpContext, configuration);
    };
    options.GridUpdateValidationDelegate = async (gridModel, httpContext, configuration) =>
    {
        return ValidationHelper.ValidateGridUpdate(gridModel, httpContext, configuration);
    };
});


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
