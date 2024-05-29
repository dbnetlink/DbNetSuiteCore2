using DbNetLink.Middleware;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbNetSuiteCore();
builder.Services.AddDbNetTimeCore();
var app = builder.Build();
app.UseDbNetTimeCore();
app.UseDbNetSuiteCore();
app.MapRazorPages();
app.Run();