// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using System.Reflection.PortableExecutable;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<LoggingService>();
builder.Services.AddHttpClient<IPowergrid, Powergrid>(x => x.BaseAddress = new Uri("https://localhost:7272/Powergrid/"));
builder.Services.AddLogging(
    builder =>
    {
        builder.AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("System", LogLevel.Warning)
            .AddFilter("NToastNotify", LogLevel.Warning)
            .AddConsole();
    });
var app = builder.Build();
app.Run();
Console.ReadKey();

public class LoggingService : BackgroundService
{
    private readonly IPowergrid powergrid;
    private CancellationToken stoppingToken;

    public LoggingService(IPowergrid powergrid)
    {
        this.powergrid = powergrid;
        stoppingToken = new CancellationToken();
       
    }

    public async Task ProduceEnergy(CancellationToken ct)
    {
        await powergrid.Start();
        while (!stoppingToken.IsCancellationRequested)
        {
             await powergrid.LogEnergy();
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ProduceEnergy(stoppingToken);
        return Task.CompletedTask;
    }

}

public interface IPowergrid
{
    public Task LogEnergy();
    public Task Start();
}

public class Powergrid : IPowergrid
{
    private readonly HttpClient httpClient;
    private readonly ILogger<Powergrid> _logger;

    public Powergrid(HttpClient httpClient, ILogger<Powergrid> logger)
    {
        this.httpClient = httpClient;
        _logger = logger;
    }

    public async Task LogEnergy()
    {
        var result = await httpClient.GetAsync("GetEnergy");
        string? energy = await result.Content.ReadAsStringAsync();
        var frequency = float.Parse(energy) / 10000 + 50;
        _logger.LogInformation("Energy: " + energy + "  |  Frequency: " + frequency);
        if (frequency <= 47.5 || frequency >= 52.5)
        {
            await this.Blackout();
        }
        await Task.Delay(1000);
    }

    public async Task Start()
    {
        Console.WriteLine("Press any button to start...");
        Console.ReadKey(true);
        await httpClient.GetAsync("Start");
    }

    public async Task Blackout()
    {
        Console.WriteLine("Blackout\n\n" +
                          "  .-\"\"\"\"\"\"-.\n /          \\\n|   >_<      |\n \\          /\n  '-......-'");
        await httpClient.GetAsync("BlackoutScenario");
        await this.Start();
    }
}