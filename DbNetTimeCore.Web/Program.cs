using DbNetLink.Middleware;
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Development.json");
builder.Services.AddDbNetTimeCore();
builder.Services.AddControllersWithViews();
var app = builder.Build();
app.UseDbNetTimeCore();
app.MapRazorPages();
app.Run();