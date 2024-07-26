using DbNetLink.Middleware;
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Development.json");
builder.Services.AddDbNetTimeCore();

builder.Services.AddRazorPages();
var app = builder.Build();
app.UseDbNetTimeCore();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.Run();