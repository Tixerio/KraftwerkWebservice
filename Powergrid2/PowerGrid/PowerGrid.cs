using Microsoft.Extensions.Logging;
using Powergrid2.Controllers;

namespace Powergrid2.PowerGrid;
using Powergrid2.Utilities;

public interface IMember
{
    public string Name { get; set; }
    public double Energy { get; set; }
}

public interface IGridRequester
{
    public Task GetIsConsuming();
}

public class GridRequester : BackgroundService, IGridRequester
{
    private readonly HttpClient httpClient;
    public GridRequester(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
    }

    public async Task GetIsConsuming()
    {
        var result = await httpClient.GetAsync("ChangeIsConnected");
        Console.WriteLine(await result.Content.ReadAsStringAsync());
    }
}

public class Grid
{
    private IGridRequester requester;
    private readonly ILogger<Grid> _logger;
    public bool Started { get; set; } = false;

    public Dictionary<int, double> Plan { get; set; } = new();
    public Dictionary<String, IMember> Members { get; set; } = new();
    public Environment Env { get; set; } = new(1, 1);

    public Grid(ILogger<Grid> logger, IGridRequester requester)
    {
        _logger = logger;
        this.requester = requester;

        InitPlan();
    }

    public double AvailableEnergy { get; set; }

    public static double ConsumeRandomEnergy()
    {
        return new Random().Next(-700, -300);
    }

    public async void ChangeEnergy(String ID)
    {
        var energy = Members.Where(x => x.Key == ID).Select(x => x.Value).FirstOrDefault()!.Energy;
        AvailableEnergy += energy;
    }

    private void InitPlan()
    {
        for (int i = 0; i < 24; i++)
        {
            Plan[i] = new Random().Next(40000, 60000);
        }
    }

    private double GetPossibleProduction()
    {
        double possibleProduction = 0;
        foreach (var (key, value) in Members.Where(x => (Type)x.Value == typeof(Powerplant)))
        {
            possibleProduction += value.Energy;
        }

        return possibleProduction;
    }

    public Dictionary<int, double> GetIndividualPlan(String ID)
    {
        var energy = Members.Where(x => x.Key == ID).Select(x => x.Value).FirstOrDefault()!.Energy;
        var possibleProduction = GetPossibleProduction();
        Dictionary<int, double> indPlan = new();

        for (int i = 0; i < 24; i++)
        {
            indPlan[i] = energy / possibleProduction * Plan[i];
            Console.WriteLine($"{energy} {possibleProduction} {Plan[i]}");
            Console.WriteLine(indPlan[i]);
        }

        return indPlan;
    }

    public async Task Start()
    {
        Members.Clear();
        AvailableEnergy = 0;
        Started = true;
    }

    public async Task Blackout()
    {
        Started = false;
    }
}


public class Powerplant : IMember
{
    public double Energy { get; set; } = 500;
    public string Name { get; set;  }

    public Powerplant(string name)
    {
        this.Name = name;
    }
}

public class Consumer : IMember
{
    public double Energy { get; set; } = 500;
    public string Name { get; set; }

    public Consumer(string name)
    {
        this.Name = name;
    }
}

