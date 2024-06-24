using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
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
            _logger.LogInformation("Has produced");
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

public class Powergrid : IPowergrid
{
    private readonly HttpClient httpClient;
    private int Pulses { get; set; } = 1;

    public ILogger<Powergrid> _logger;

    public String? ID { get; set; }

    public String getID()
    {
        return (this.ID);
    }

    private bool started = false;
    public IOptionsMonitor<ApplicationOptions> Options { get; set; }



    public Powergrid(HttpClient httpClient, ILogger<Powergrid> logger, IOptionsMonitor<ApplicationOptions> options)
    {
        this.httpClient = httpClient;
        _logger = logger;
        this.Options = options;
    }

    private HubConnection CreateHub()
    {
        HubConnection hub = new HubConnectionBuilder().WithUrl(Options.CurrentValue.HubAddress)
            .Build();
        hub.StartAsync();
        return hub;
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
        HubConnection hub = CreateHub();
        await hub.SendAsync("RegisterR", new MemberObject("Fabi", "Powerplant"));
        hub.On<string>("ReceiveMessage",
            message => this.ID = message);
    }

    public async void ChangeEnergyR()
    {
        HubConnection hub = CreateHub();
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
