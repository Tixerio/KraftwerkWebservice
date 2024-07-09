using Powergrid2.Controllers;
using Console = System.Console;

using Powergrid2.Utilities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNet.SignalR.Messaging;
using Newtonsoft.Json;


public interface IPowergridHubClient
{
    Task ReceiveMessage(string message);
    Task ReceiveMembers(Dictionary<string, string> message);
    Task ReceiveMemberData(Dictionary<string, int> data);
    Task ReceiveTime(int hours);
    Task ReceiveStop(bool stopped);
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

    public async Task StartStop()
    {
        grid.Stopped = grid.Stopped == false ? true : false;
        Console.WriteLine(grid.Stopped);
        await Clients.Caller.ReceiveStop(grid.Stopped);
    }

    public async Task ChangeMultiplicatorAmountR(string id, int request)
    {
        Console.WriteLine(request);
        grid.MultiplicatorAmount[id] = request;
    }

    public async Task GetCurrentTimeR()
    {
        await grid.Env.DayCycle();
        await Clients.Caller.ReceiveTime(grid.Env.GetTimeInTimeSpan().Hours);
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
        grid.Stopped = true;
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

    public Dictionary<int, double> Plan { get; set; } = new();
    public Dictionary<string, IMember> Members { get; set; } = new();
    public Environment Env { get; set; } = new(1, 1);
    public Dictionary<string, int> PulseCounter { get; set; } = new();
    public Dictionary<string, int> MultiplicatorAmount { get; set; } = new();

    public bool Stopped { get; set; } = false;

    public Grid(ILogger<Grid> logger, IGridRequester requester)
    {
        _logger = logger;
        this.requester = requester;
    }

    public double AvailableEnergy { get; set; }

    public async void ChangeEnergy(String ID)
    {
        if (!Stopped)
        {
            var member = Members.Where(x => x.Key == ID).Select(x => x.Value).FirstOrDefault();
            if (member?.GetType() == typeof(Consumer))
            {
                var consumer = (Consumer)member;
                consumer.Hour = Env.GetTimeInTimeSpan().Hours;
                AvailableEnergy += member.Energy * MultiplicatorAmount.FirstOrDefault(x => x.Key == ID).Value;
                return;
            }
            AvailableEnergy += member!.Energy * MultiplicatorAmount.FirstOrDefault(x => x.Key == ID).Value;
        }
    }

    private void InitPlan()
    {
        for (int i = 0; i < 24; i++)
        {
            Plan.Add(i,0);
            foreach (var consumer in Members.Where(x => x.Value.GetType() == typeof(Consumer)))
            {
                Plan[i] += consumer.Value.Energy * MultiplicatorAmount.FirstOrDefault(x => x.Key == consumer.Key).Value;
                Env.IncrementTime();
                ((Consumer)consumer.Value).Hour = Env.GetTimeInTimeSpan().Hours;
            }
        }
    
    }

    public Dictionary<int, double> GetExpectedConsume()
    {

        if (!Plan.Any())
        {
            InitPlan();
        }
        return (Plan);
    }



    public async Task Start()
    {
        Console.WriteLine("Started");
        AvailableEnergy = 0;
    }

    public async Task Blackout()
    {
        Members.Clear();
    }
}