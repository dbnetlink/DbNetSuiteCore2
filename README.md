### Introduction ###
This project is a set of data driven ASP.NET Core components based on Razor pages, HTMX, Tailwind CSS and JavaScript. The components can be added to any ASP.NET Core web application as middleware and the components can be added to any existing web page. The goal is to creats a set of components that are lightweight, easy to add to any web page and highly configurable.

Currently the components support the following data sources:

 - MSSQL
 - SQLite
 - MySql
 - PostgreSql
 - JSON
 - File System

### To add **DbNetSuiteCore** to your web application add the following to your **program.cs** file ###
```
var builder = WebApplication.CreateBuilder(args);
...
builder.Services.AddDbNetSuiteCore()
...
app.UseDbNetSuiteCore();
...
```
### To add to your Razor page ###
```
@page
@using DbNetSuiteCore.Enums
@using DbNetSuiteCore.Models
@{
   Layout = null;
   var customerGrid = new GridModel(DataSourceType.SQlite, "Sakila", "Customer"); <-- Configure the component
}
<!DOCTYPE html>
<html lang="en">
<head>
    @DbNetSuiteCore.Resources.StyleSheet() <-- Add the stylesheet
</head>
<body>
    <main style="padding:20px">
        @(await new DbNetSuiteCore.GridControl(HttpContext).Render(customerGrid)) <-- Render the component
   </main>
    @DbNetSuiteCore.Resources.ClientScript() <-- Render the client script
</body>
</html>
```
