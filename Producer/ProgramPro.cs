// See https://aka.ms/new-console-template for more information


using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

var builder = Host.CreateApplicationBuilder(args);

// Add services to the container.
builder.Services.AddHostedService<PowerPlant>();
builder.Services.AddHttpClient<IPowergrid, Powergrid>(x => x.BaseAddress = new Uri("https://localhost:7272/Powergrid/"));
//über transient oder eigene extension method
builder.Services.AddLogging(
    builder =>
    {
        builder.AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("System", LogLevel.Warning)
            .AddFilter("NToastNotify", LogLevel.Warning)
            .AddConsole();
    });
builder.Services.AddTransient<HubConnection>((sp) => new HubConnectionBuilder()
    .WithUrl("https://localhost:7272/Powergrid")
    .Build());

var app = builder.Build();
app.Run();
Console.ReadKey();