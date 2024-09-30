
# DbNetSuiteCore

**DbNetSuiteCore** is a set of ASP.Net Core application development components designed to enable the rapid development of database driven web applications. **DbNetSuiteCore** currently supports MSSQL, MySQL, MariaDB, PostgreSQL and SQLite databases along with JSON (files and API), CSV and Excel files and the file system itself.

Simply add DbNetSuiteCore to your pipeline as follows:
```c#
{
    using DbNetLink.Middleware;           // <= Add this line

    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
    builder.Services.AddRazorPages();
    builder.Services.AddDbNetSuiteCore(); // <= Add this line
    
    WebApplication app = builder.Build();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthorization();
    app.UseDbNetSuiteCore();              // <= Add this line
       
    app.UseEndpoints(endpoints =>
    {
	    endpoints.MapRazorPages();
    });

    app.Run();
}
```
You can then add a component to your Razor page as follows:
```c#
@page
@using DbNetSuiteCore.Components;
<!DOCTYPE html>
<html lang="en">
<head>
    <title>Customers</title>
    @DbNetSuiteCore.StyleSheet()
</head>
<body>
    <div>
        <main>
            @{
                DbNetGridCore customersGrid = new DbNetGridCore("northwind","customers");
                @customersGrid.Render()
            }
        </main>
    </div>
    @DbNetSuiteCore.ClientScript()
</body>
</html>
```
For a comprehensive set of demos [click here](https://www.dbnetsuitecore.com/) and for the documentation  [click here](https://dbnetsuitecore.z35.web.core.windows.net/) 