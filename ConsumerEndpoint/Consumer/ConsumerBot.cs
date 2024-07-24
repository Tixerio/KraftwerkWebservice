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
                await Task.Delay(1000, stoppingToken);
                if (powergrid.getID() == null) continue;
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

public class Powergrid : IPowergrid
{
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
        _logger = logger;
        this.hub = hub;
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
