using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PolyhydraGames.Sims4.Bridge;

public static class Setup
{
    public const string EnvironmentPrefix = "SIMS4_";
    public const string AppSettingsFileName = "appsettings.json";

    public static IServiceCollection AddConfig(this IServiceCollection services, string? basePath = null)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(string.IsNullOrWhiteSpace(basePath) ? AppContext.BaseDirectory : basePath)
            .AddJsonFile(AppSettingsFileName, optional: true, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: EnvironmentPrefix)
            .Build();

        services.AddSingleton<IConfiguration>(config);
        services.Configure<SimsBridgeOptions>(config);
        return services;
    }
}
