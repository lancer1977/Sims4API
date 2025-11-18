using PolyhydraGames.Sims4.Bridge;
using AppDomain = System.AppDomain;
using Directory = System.IO.Directory;

Console.WriteLine("Hello, World!");
// See https://aka.ms/new-console-template for more information


Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);



var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddConfig();
        services.AddSingleton<Connection>();


    }).Build();



var scope = host.Services;
//var listener = scope.GetRequiredService<CompanionPortListener>();
//await listener.Start();
var web = scope.GetRequiredService<Connection>();
await web!.StartAsync();


await host.RunAsync();
