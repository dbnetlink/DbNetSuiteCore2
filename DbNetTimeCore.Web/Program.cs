using DbNetLink.Middleware;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbNetTimeCore();
builder.Services.AddControllersWithViews();
var app = builder.Build();
app.UseDbNetTimeCore();
app.MapRazorPages();
app.Run();