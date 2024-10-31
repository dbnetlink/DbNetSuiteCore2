
# DbNetSuiteCore

**DbNetSuiteCore** is a set of ASP.Net Core application UI development components designed to enable the rapid development of data driven web applications. **DbNetSuiteCore** currently supports MSSQL, MySQL, MariaDB, PostgreSQL, MongoDB and SQLite databases along with JSON (files and API), CSV and Excel files and the file system itself.

Simply add DbNetSuiteCore to your pipeline as follows:
```c#
{
    using DbNetSuiteCore.Middleware;                    // <= Add this line

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddDbNetSuiteCore();               // <= Add this line

    builder.Services.AddRazorPages();

    var app = builder.Build();

    app.UseDbNetSuiteCore();                            // <= Add this line

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthorization();
    app.MapRazorPages();
    app.Run();
}
```
You can then add a component to your Razor page as follows:
```c#
@page
@using DbNetSuiteCore.Enums
@using DbNetSuiteCore.Models

<!DOCTYPE html>
<html lang="en">
<head>
    @DbNetSuiteCore.Resources.StyleSheet() @* Add the stylesheet *@
</head>
<body>
    <main>
@{
    GridModel customerGrid = new GridModel(DataSourceType.SQLite, "Northwind", "Customers");
    @(await new DbNetSuiteCore.GridControl(HttpContext).Render(customerGrid))
}
    </main>
    @DbNetSuiteCore.Resources.ClientScript() @* Add the client-side library *@
</body>
</html>
```

For demos [click here](https://dbnetsuitecore.com/) and for the documentation [click here](https://dbnetsuitecore.document360.io/docs) 