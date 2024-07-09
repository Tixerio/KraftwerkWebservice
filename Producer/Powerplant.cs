using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;


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
            if (powergrid.GetId() == null)
            {
                Console.WriteLine("Not Registered, push any button to register");
                Console.ReadKey(true);
                powergrid.Register();
                while (powergrid.GetId() == null)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            powergrid.ChangeEnergy();
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
    public void StartClient();
    public void ChangeEnergy();
    public void Register();
    public String GetId();
}

public class Powergrid : IPowergrid
{
    private readonly HttpClient httpClient;
    private int Pulses { get; set; } = 1;

    public ILogger<Powergrid> _logger;

    public String? ID { get; set; }

    public String GetId()
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

    public async void Register()
    {
        HubConnection hub = CreateHub();
        await hub.SendAsync("RegisterR", new MemberObject("Test", "Powerplant"));
        hub.On<string>("ReceiveMessage",
            message => this.ID = message);
    }

    public async void ChangeEnergy()
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
    public const string Key = "HubCon";

    [Required(ErrorMessage = "Address Required")]
    public string HubAddress { get; set; }
}

public class ApplicationOptionsSetup : IConfigureOptions<ApplicationOptions>
{
    private const string SectionName = "HubCon"; 
    private readonly IConfiguration _configuration;

    public ApplicationOptionsSetup(IConfiguration configuration)
    {
        _configuration = configuration; 
    }

    public void Configure(ApplicationOptions options)
    {
        _configuration.GetSection(SectionName).Bind(options);

    }
}
