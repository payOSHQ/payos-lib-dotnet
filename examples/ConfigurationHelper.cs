using Microsoft.Extensions.Configuration;

namespace PayOS.Examples;

public static class ConfigurationHelper
{
    private static IConfiguration? _configuration;

    public static IConfiguration Configuration
    {
        get
        {
            _configuration ??= new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();
            return _configuration;
        }
    }

    public static IConfigurationSection GetConfigurationSection(string section)
    {
        return Configuration.GetSection(section);
    }
}