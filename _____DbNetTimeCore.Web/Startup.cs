using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    IConfigurationRoot Configuration { get; }

    public Startup()
    {
        var builder = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json");
        Configuration = builder.Build();
    }

}