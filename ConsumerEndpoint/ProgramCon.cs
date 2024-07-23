using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<ConsumerBot>();
builder.Services.AddHttpClient<IPowergrid, Powergrid>(x => x.BaseAddress = new Uri("https://localhost:7272/Powergrid/"));
builder.Services.AddLogging(
    builder =>
    {
        builder.AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("System", LogLevel.Warning)
            .AddFilter("NToastNotify", LogLevel.Warning)
            .AddConsole();
    });

builder.Services.Configure<ApplicationOptions>(
    builder.Configuration.GetSection("HubCon"));
builder.Services.ConfigureOptions<ApplicationOptionsSetup>();

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddSingleton(sp =>
{
    var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<ApplicationOptions>>();
    var hubConnection = new HubConnectionBuilder()
        .WithUrl(optionsMonitor.CurrentValue.HubAddress)
        .Build();


    return hubConnection;
});

builder.Services.AddTransient<ConsumerBot>();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<ApplicationOptions>>();

optionsMonitor.OnChange(options =>
{
    logger.LogInformation("Application Address Updated: {HubAddress}", options.HubAddress);
});

app.Run();

app.Run();
Console.ReadKey();