namespace PolyhydraGames.Sims4.Bridge;

public static class Setup
{
 
    public static IServiceCollection AddConfig(this IServiceCollection services)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, true)
            .AddUserSecrets("65a2f916-1765-44e8-8d59-2d2ddcd7cc9b") // Use the UserSecretsId generated earlier
            .Build();
        services.AddSingleton<IConfiguration>(config);
        return services;
    }

}