// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;

var builder = Host.CreateApplicationBuilder(args);

// Add services to the container.
builder.Services.AddHostedService<PowerPlant>();
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

public class PowerPlant : BackgroundService
{

    private readonly IPowergrid powergrid;
    private CancellationToken stoppingToken;
    private ILogger<PowerPlant> _logger;

    public PowerPlant(IPowergrid powergrid, ILogger<PowerPlant> logger)
    {
        this.powergrid = powergrid;
        _logger = logger;
        stoppingToken = new CancellationToken();
        powergrid.Register(stoppingToken);
    }
    

    public async Task ProduceEnergy(CancellationToken ct)
    {

        while (!stoppingToken.IsCancellationRequested)
        {
            await powergrid.ChangeEnergy(stoppingToken);
            _logger.LogInformation("Has produced");
            await Task.Delay(2000, stoppingToken);
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
    public Task ChangeEnergy(CancellationToken ct);
    public Task PulseChange(CancellationToken ct);
    public Task Register(CancellationToken ct);
}
public interface IAsyncInitialization
{
    public Task Initialization { get; }
}

public class Powergrid : IPowergrid, IAsyncInitialization
{
    private readonly HttpClient httpClient;
    public Task Initialization { get; private set; }

    private int Pulses { get; set; } = 1;

    public ILogger<Powergrid> _logger;

    private String? ID { get; set; }

    public Powergrid(HttpClient httpClient, ILogger<Powergrid> logger)
    {
        this.httpClient = httpClient;
        _logger = logger;
        Initialization = InitializationAsync();
    }

    public Task InitializationAsync()
    {
        Task.Run(async () => await Register(new CancellationToken()));
        return Task.CompletedTask;
    }
    public async Task ChangeEnergy(CancellationToken ct)
    {
        if (this.ID == null)
        {
            return;
        }
        var result = await httpClient.PostAsync("ChangeEnergy", JsonContent.Create(this.ID), ct);
        String registered = await result.Content.ReadAsStringAsync(ct);
        if (registered != "Registered")
        {
            Pulses = 1;
            _logger.LogInformation(registered + "\nPress any button to register...");
            Console.ReadKey(true);
            await Register(ct);
        }
        result.EnsureSuccessStatusCode();
    }

    public Task PulseChange(CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task Register(CancellationToken ct)
    {
        var member = new MemberObject("Kraftwerk", "Powerplant");

        var result = await httpClient.PostAsync("Register", JsonContent.Create(member));
        this.ID = await result.Content.ReadAsStringAsync(ct);
    }

    public class MemberObject
    {
        public MemberObject(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; set; }
        public string Type { get; set; }
    }
}