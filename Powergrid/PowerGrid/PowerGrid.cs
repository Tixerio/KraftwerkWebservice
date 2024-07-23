using Powergrid2.Controllers;
using Console = System.Console;
using Microsoft.AspNetCore.SignalR;
// ReSharper disable All

public class MarketShare
{
    public string Name { get; set; }
    public double Value { get; set; }
}

public interface IPowergridHubClient
{
    Task ReceiveMessage(string message);
    Task ReceiveMembers(Dictionary<string, string> message);
    Task ReceiveMemberData(Dictionary<string, int> data);
    Task ReceiveTime(int hours);
    Task ReceiveStop(bool stopped);
    Task ReceiveEnergy(double energy);
    Task ReceiveExpectedConsume(Dictionary<int, double> Plan);
    Task ReceiveBlackout();

    Task ReceivePieChartData(Dictionary<string, MarketShare> UserProduction);
}

public class PowergridHub : Hub<IPowergridHubClient>
{
    private Grid grid;

    public PowergridHub(Grid grid)
    {
        this.grid = grid;
    }

    public async Task ChangeEnergyR(string? id)
    {
        if (id != null && !grid.Members.ContainsKey(id))
        {
            await Clients.Caller.ReceiveMessage("Not registered");
            return;
        }
        grid.ChangeEnergy(id);
    }

    public async Task StartStop()
    {
        grid.Stopped = grid.Stopped == false ? true : false;
        if (!grid.Stopped)
        {
            grid.TimeLoop(Clients);
        }

        await Clients.Caller.ReceiveStop(grid.Stopped);
    }

    public async Task ChangeMultiplicatorAmountR(string id, int request)
    {
        grid.MultiplicatorAmount[id] = request;
        grid.InitPlanMember();
    }

    public async Task GetMemberDataR()
    {
        Dictionary<string, string> transformedMembersDic = new();
        foreach (var (key, value) in grid.Members)
        {
            transformedMembersDic.Add(key, $"{value.Name}({value.GetType()})");
        }
        await Clients.All.ReceiveMembers(transformedMembersDic);
        await Clients.Caller.ReceiveMemberData(grid.MultiplicatorAmount);
    }

    public async Task RegisterR(PowergridController.MemberObject request)
    {
        var id = Guid.NewGuid().ToString();
        switch (request.Type)
        {
            case "Powerplant":
                grid.Members.Add(id, new Powerplant(request.Name));
                grid.MultiplicatorAmount.Add(id, 5);
                break;
            case "Consumer":
                grid.Members.Add(id, new Consumer(request.Name));
                grid.MultiplicatorAmount.Add(id, 500);
                grid.InitPlanMember();
                break;
        }

        Console.WriteLine("Registered");
        Dictionary<string, string> transformedMembers = new();
        foreach (var (key, value) in grid.Members)
        {
            transformedMembers.Add(key, $"{value.Name}({value.GetType()})");
        }
        Console.WriteLine(transformedMembers.Count());
        await Clients.All.ReceiveMembers(transformedMembers);
        await Clients.Caller.ReceiveMessage(id);
    }

    public async Task GetEnergyR()
    {
        await Clients.Caller.ReceiveEnergy(grid.AvailableEnergy);
    }

    public Task ResetEnergyR()
    {
        grid.Members.Clear();
        grid.Stopped = false;
        grid.TimeInInt = 0;
        grid.AvailableEnergy = 0;
        return Task.CompletedTask;
    }

    public async Task GetExpectedConsume()
    {
        await Clients.All.ReceiveExpectedConsume(grid.GetExpectedConsume());
        Console.WriteLine("Called endpoint");
    }

    public override async Task OnConnectedAsync()
    {
        this.grid.GetClients(Clients);
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

public class Grid
{
    private readonly ILogger<Grid> _logger;
    public Dictionary<int, double> Plan_User { get; set; } = new();
    public Dictionary<int, double> Plan_Member { get; set; } = new();
    public Dictionary<string, IMember> Members { get; set; } = new();
    public Environment Env { get; set; } = new(1, 1);
    public Dictionary<string, int> MultiplicatorAmount { get; set; } = new();
    public int TimeInInt { get; set; } = 0;
    public bool Stopped { get; set; } = false;
    public bool ThreadStarted { get; set; } = false;
    public double AvailableEnergy { get; set; }
    private IHubCallerClients<IPowergridHubClient>? _clients;

    public IHubCallerClients<IPowergridHubClient> Clients
    {
        get
        {
            if (_clients == null)
            {
                throw new InvalidOperationException("Clients have not been initialized. Ensure GetClients is called before accessing this property.");
            }
            return _clients;
        }
        set
        {
            _clients = value;
        }
    }

    public Grid(ILogger<Grid> logger)
    {
        _logger = logger;
    }

    public void GetClients(IHubCallerClients<IPowergridHubClient> clients)
    {
        Clients = clients;
    }

    public void ChangeEnergy(string ID)
    {
        if (!Stopped)
        {
            var member = Members.Where(x => x.Key == ID).Select(x => x.Value).FirstOrDefault();
            if (member?.GetType() == typeof(Consumer))
            {
                if (!Plan_Member.Any())
                {
                    InitPlanMember();
                }
                var consumer = (Consumer)member;
                consumer.Hour = Convert.ToInt32(Math.Floor((double)(TimeInInt / 60 % 24)));
                AvailableEnergy += consumer.getCalculatedEnergy(Plan_Member[consumer.Hour]);
                return;
            }

            var powerplant = (Powerplant)member;
            powerplant.Produced += member!.Energy * MultiplicatorAmount.FirstOrDefault(x => x.Key == ID).Value;
            AvailableEnergy += member!.Energy * MultiplicatorAmount.FirstOrDefault(x => x.Key == ID).Value;
        }
    }

    public void TimeLoop(IHubCallerClients<IPowergridHubClient> clients)
    {
        Thread threadTime = new Thread(async () =>
        {
            int currentMembers = Members.Count();
            if (!ThreadStarted)
            {
                ThreadStarted = true;
                while (!Stopped)
                {
                    await clients.All.ReceiveTime(TimeInInt);
                    if (TimeInInt % 1440 == 0)
                    {
                        Plan_User.Clear();
                        TimeInInt = 0;
                        await clients.All.ReceiveExpectedConsume(GetExpectedConsume());
                    }

                    if (TimeInInt % 60 == 0)
                    {
                        var UserProduction = new Dictionary<string, MarketShare>();
                        foreach (var (key, value) in Members.Where(x => x.Value.GetType() == typeof(Powerplant)))
                        {
                            var powerplant = (Powerplant)value;
                            UserProduction.Add(key, new MarketShare{ Name = value.Name, Value = powerplant.Produced});
                            powerplant.Produced = 0;
                        }
                        await clients.All.ReceivePieChartData(UserProduction);
                    }
                    await Clients.All.ReceiveEnergy(this.AvailableEnergy);
                    TimeInInt += 5;
                    if ((this.AvailableEnergy / 10000) + 50 > 52.5 || (this.AvailableEnergy / 10000) + 50 < 47.5)
                    {
                        this.Members.Clear();
                        this.Stopped = false;
                        this.TimeInInt = 0;
                        this.AvailableEnergy = 0;
                        await Clients.All.ReceiveBlackout();
                    }

                    Thread.Sleep(1000);
                }

                ThreadStarted = false;
            }
        });
        threadTime.Start();
    }

    public void InitPlanMember()
    {
        Plan_Member.Clear();
        for (int i = 0; i < 24; i++)
        {
            Plan_Member.Add(i, 0);
            foreach (var (key, value) in Members.Where(x => x.Value.GetType() == typeof(Consumer)))
            {
                Plan_Member[i] -= (value.Energy *
                    MultiplicatorAmount.FirstOrDefault(x => x.Key == key).Value * new Random().Next(9, 11) /
                    10 * ((Consumer)value).ConsumePercentDuringDayNight[i]);
                ((Consumer)value).Hour = i;
            }
        }
    }

    public Dictionary<int, double> GetExpectedConsume()
    {
        if (!Plan_Member.Any())
        {
            InitPlanMember();
        }
        if (Members.Where(x => x.Value.GetType() == typeof(Consumer)).Count() == 0)
        {
            return Plan_Member;
        }

        if (!Plan_User.Any())
        {
            foreach (var (key, value) in Plan_Member)
            {
                Plan_User.Add(key, value);
            }
            return Plan_User;
        }
        return Plan_User;
    }
}

