using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;


public class Consumer : BackgroundService
{

    private readonly IPowergrid powergrid;
    private CancellationToken stoppingToken;
    private ILogger<Consumer> _logger;
    public Consumer(IPowergrid powergrid, ILogger<Consumer> logger)
    {
        this.powergrid = powergrid;
        _logger = logger;
        stoppingToken = new CancellationToken();

        powergrid.StartClient();
    }


    public async Task ProduceEnergy(CancellationToken ct)
    {

        while (!stoppingToken.IsCancellationRequested)
        {
            if (powergrid.getID() == null)
            {
                Console.WriteLine("Not Registered, push any button to register");
                Console.ReadKey(true);
                powergrid.RegisterR();
                while (powergrid.getID() == null)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            powergrid.ChangeEnergyR();
            _logger.LogInformation("Has consumed");
            await Task.Delay(2000, stoppingToken);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProduceEnergy(stoppingToken);
    }

}

public interface IPowergrid
{
    //public Task ChangeEnergy(CancellationToken ct);
    public Task PulseChange(CancellationToken ct);
    public Task Register(CancellationToken ct);
    public void StartClient();
    public void ChangeEnergyR();
    public void RegisterR();
    public String getID();

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

    public String? ID { get; set; }

    public String getID()
    {
        return (this.ID);
    }

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
        if (started == false)
        {

            started = true;
        }
    }

    public async void RegisterR()
    {
        await hub.SendAsync("RegisterR", new MemberObject("Fabi", "Consumer"));
        hub.On<string>("ReceiveMessage",
            message => this.ID = message);
    }

    public async void ChangeEnergyR()
    {

        await hub.SendAsync("ChangeEnergyR", this.ID);
        hub.On<string>("ReceiveMessage",
            message =>
            {
                if (message != "Registered")
                {
                    Pulses = 1;
                    this.ID = null;
                }
            });
    }

    public Task InitializationAsync()
    {
        hub.StartAsync();
        return Task.CompletedTask;
    }
    /*public async Task ChangeEnergy(CancellationToken ct)
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
    }*/

    public Task PulseChange(CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task Register(CancellationToken ct)
    {
        var member = new MemberObject("Kraftwerk", "Consumer");

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

public class ApplicationOptions
{
    public const string Key = "HubCon"; // Defines the key for the options section

    [Required(ErrorMessage = "Address Required")]
    public string HubAddress { get; set; } // Stores the HubAddress with a validation attribute
}

// ApplicationOptionsSetup class
public class ApplicationOptionsSetup : IConfigureOptions<ApplicationOptions>
{
    private const string SectionName = "HubCon"; // Ensures this matches the JSON section name
    private readonly IConfiguration _configuration;

    public ApplicationOptionsSetup(IConfiguration configuration)
    {
        _configuration = configuration; // Injects the configuration
    }

    public void Configure(ApplicationOptions options)
    {
        _configuration.GetSection(SectionName).Bind(options); // Binds the configuration section to the ApplicationOptions instance
    }
}
