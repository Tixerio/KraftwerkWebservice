using Powergrid2.Controllers;
using Console = System.Console;

using Powergrid2.Utilities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNet.SignalR.Messaging;


public interface IPowergridHubClient
{
    Task ReceiveMessage(string message);
    Task ReceiveMembers(Dictionary<string, string> message);
    Task ReceiveMemberData(Dictionary<string, int> data);
}

public class PowergridHub : Hub<IPowergridHubClient>
{
    private Grid grid;

    public PowergridHub(Grid grid)
    {
        this.grid = grid;
    }

    public async Task ChangeEnergyR(string? ID)
    {
        if (ID != null && !grid.Members.ContainsKey(ID))
        {
            await Clients.Caller.ReceiveMessage("Not registered");
            return;
        }
        grid.ChangeEnergy(ID);
    }

    public async Task ChangeMultiplicatorAmountR(string id, int request)
    {
        Console.WriteLine(request);
        grid.MultiplicatorAmount[id] = request;
    }

    public async Task GetMemberDataR()
    {
        Dictionary<string, string> transformedMembers = new();
        foreach (var (key, value) in grid.Members)
        {
            transformedMembers.Add(key, $"{value.Name}({value.GetType()})");
        }

        await Clients.All.ReceiveMembers(transformedMembers);
        await Clients.Caller.ReceiveMemberData(grid.MultiplicatorAmount);
    }

    public async Task RegisterR(PowergridController.MemberObject request)
    {
        var ID = Guid.NewGuid().ToString();
        switch (request.Type)
        {
            case "Powerplant":
                grid.Members.Add(ID, new Powerplant(request.Name));
                grid.MultiplicatorAmount.Add(ID, 5);
                break;
            case "Consumer":
                grid.Members.Add(ID, new Consumer(request.Name));
                grid.MultiplicatorAmount.Add(ID, 500);
                break;
       
        }
        Console.WriteLine("Registered");
        Dictionary<string, string> transformedMembers = new();
        foreach (var (key, value) in grid.Members)
        {
            transformedMembers.Add(key, $"{value.Name}({value.GetType()})");
        }

        await Clients.All.ReceiveMembers(transformedMembers);
        await Clients.Caller.ReceiveMessage(ID);
    }

    public async Task GetEnergyR()
    {
        await Clients.Caller.ReceiveMessage(grid.AvailableEnergy.ToString());
    }

    public async Task ResetEnergy()
    {
        grid.Members.Clear();
        grid.AvailableEnergy = 0;
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine("connected");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}

public interface IMember
{
    public string Name { get; set; }
    public double Energy { get; }
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
    public Dictionary<string, IMember> Members { get; set; } = new();
    public Environment Env { get; set; } = new(1, 1);
    public Dictionary<string, int> PulseCounter { get; set; } = new();
    public Dictionary<string, int> MultiplicatorAmount { get; set; } = new();

    public Grid(ILogger<Grid> logger, IGridRequester requester)
    {
        _logger = logger;
        this.requester = requester;
        InitPlan();
    }

    public double AvailableEnergy { get; set; }


    public async void ChangeEnergy(String ID)
    {
        
        var member = Members.Where(x => x.Key == ID).Select(x => x.Value).FirstOrDefault();
        if (member.GetType() == typeof(Consumer))
        {
            Consumer consumer = (Consumer)member;
            consumer.Hour = Env.GetTimeInTimeSpan().Hours;
            AvailableEnergy += member.Energy * MultiplicatorAmount.FirstOrDefault(x => x.Key == ID).Value;
            return;
        }
        AvailableEnergy += member.Energy * MultiplicatorAmount.FirstOrDefault(x => x.Key == ID).Value;
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
        Console.WriteLine("Started");
        AvailableEnergy = 0;
        Started = true;
    }

    public async Task Blackout()
    {
        Members.Clear();
        Started = false;
    }
}





