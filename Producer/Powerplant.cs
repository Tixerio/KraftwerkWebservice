using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;


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
        powergrid.StartClient();
        powergrid.RegisterR();

    }


    public async Task ProduceEnergy(CancellationToken ct)
    {
        
        while (!stoppingToken.IsCancellationRequested)
        {
            powergrid.ChangeEnergyR();
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
    public void StartClient();
    public void ChangeEnergyR();
    public void RegisterR();

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

    private HubConnection hub;
    private bool started = false;



    public Powergrid(HttpClient httpClient, ILogger<Powergrid> logger, HubConnection hub)
    {
        this.httpClient = httpClient;
        _logger = logger;
        this.hub = hub;
        Initialization = InitializationAsync();

    }

    public async void StartClient()
    {

        hub.On<string>("ReceiveMessage",
            message => Console.WriteLine($"SignalR Hub Message: {message}"));

        if (started == false)
        {
            await hub.StartAsync();
            started = true;
        }
    }

    public async void RegisterR()
    {
        await hub.SendAsync("RegisterR", new MemberObject("Fabi", "Powerplant"));

        hub.On<string>("RegisterResponse",
            message => this.ID = message);
    }

    public async void ChangeEnergyR()
    {
        if (this.ID == null)
        {
            return;
        }
        await hub.SendAsync("ChangeEnergyMessage", this.ID);

        hub.On<string>("ChangeEnergyResponse",
            message =>
            {
                
                if (message != "Registered")
                {
                    Pulses = 1;
                    _logger.LogInformation(message + "\nPress any button to register...");
                    Console.ReadKey(true);
                    RegisterR();
                }
            });
       
        Console.WriteLine("ID: " + this.ID);
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