using Microsoft.AspNetCore.SignalR.Client;
using Console = System.Console;

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

    private HubConnection hub;

    public Powergrid(HttpClient httpClient, ILogger<Powergrid> logger, HubConnection hub)
    {
        this.httpClient = httpClient;
        _logger = logger;
        this.hub = hub;
    }

    public async Task LogEnergy()
    {
     
        double energy = 0;
        try
        {
            await hub.SendAsync("GetEnergyR");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        //Getenergy geht nit :(
        hub.On<string>("GetEnergyResponse",
            message => energy = float.Parse(message));
    

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
        Console.ReadKey(true);
        await hub.StartAsync();
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
