using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

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
    private double energy = 0;

    private HubConnection hub;

    public Powergrid(HttpClient httpClient, ILogger<Powergrid> logger, HubConnection hub)
    {
        this.httpClient = httpClient;
        _logger = logger;
        this.hub = hub;
        

    }

    public async Task LogEnergy()
    {
        try
        {
            await hub.SendAsync("GetEnergyR");
            hub.On<string>("ReceiveMessage",
                message =>
                {
                    Console.WriteLine(message);
                    this.energy = float.Parse(message);
                    hub.Remove("ReceiveMessage");
                });

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

     

        var frequency = energy / 10000 + 50;
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
        this.energy = 0;
        Console.ReadKey(true);
        await hub.StartAsync();
    
    }

    public async Task Blackout()
    {
        await hub.SendAsync("ResetEnergy");
        Console.WriteLine("Blackout\n\n" +
                          "  .-\"\"\"\"\"\"-.\n /          \\\n|   >_<      |\n \\          /\n  '-......-'");
        await hub.StopAsync();
        await this.Start();
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
