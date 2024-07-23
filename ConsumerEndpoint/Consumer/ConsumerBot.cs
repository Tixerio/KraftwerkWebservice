using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

public class ConsumerBot : BackgroundService
{
    private readonly IPowergrid powergrid;
    private CancellationToken stoppingToken;
    private ILogger<ConsumerBot> _logger;
    public ConsumerBot(IPowergrid powergrid, ILogger<ConsumerBot> logger)
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
        return this.ID;
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
       
        if (hub.State == HubConnectionState.Disconnected)
        {
            try
            {
                await hub.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: Server not running or not possible to connect...\n" +
                                  "Wait a short moment and try again.");
                return;
            }
        }
        
        await hub.SendAsync("RegisterR", new MemberObject("Household", "Consumer"));
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
        return Task.CompletedTask;
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
    public string HubAddress { get; set; } }


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
