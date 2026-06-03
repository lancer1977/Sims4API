using AppDomain = System.AppDomain;
using Directory = System.IO.Directory;

using PolyhydraGames.Sims4.Bridge;

Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddConfig();
        services.AddSingleton<Connection>();
    })
    .Build();

var bridge = host.Services.GetRequiredService<Connection>();
await bridge.StartAsync();
await host.RunAsync();
