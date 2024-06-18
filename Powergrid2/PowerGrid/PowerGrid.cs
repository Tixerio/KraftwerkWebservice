namespace Powergrid2.PowerGrid;
using Powergrid2.Utilities;

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
        
        var member = Members.Where(x => x.Key == ID).Select(x => x.Value).FirstOrDefault();
        if (member.GetType() == typeof(Consumer))
        {
            Console.WriteLine(Env.GetTimeInTimeSpan());
            Consumer consumer = (Consumer)member;
            consumer.Hour = Env.GetTimeInTimeSpan().Hours;
        }
        AvailableEnergy += member.Energy;
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
    public double Energy { get;  } = 500;
    public string Name { get; set;  }

    public Powerplant(string name)
    {
        this.Name = name;
    }
}

public class Consumer : IMember
{
    private readonly double[] consumePercentDuringDayNight =
    [
        0.125, 0.1875, 0.25, 0.1875, 0.375, 0.5, 0.75, 0.875, 0.8125, 0.875, 1, 1, 0.625, 0.6875, 0.5, 0.375, 0.375, 0.75, 0.5,
        0.5, 0.625, 0.3125, 0.1875, 0.1875
    ];

    public int Hour { get; set; } = 0;
    private double energy;

    public virtual double Energy
    {
        get
        {
            Console.WriteLine(this.energy * consumePercentDuringDayNight[Hour]);
            return this.energy * consumePercentDuringDayNight[Hour];
        }
        set
        {
            this.energy = value;
        }
    }

    public string Name { get; set; }
     
    public Consumer(string name)
    {
        this.Energy = -1000;
        Console.WriteLine(Energy);
        this.Name = name;
    }

    public class Household : Consumer
    {
        public Household(string name) : base(name)
        {
        }
    }

    public class HouseholdPV : Consumer
    {
        public override double Energy
        {
            get
            {

                Console.WriteLine(this.energy * consumePercentDuringDayNight[Hour] + " PV: " + RandomPVValue());
                return this.energy * consumePercentDuringDayNight[Hour] + RandomPVValue();
            }

            set
            {
                this.energy = value;
            }
        }

        public HouseholdPV(string name) : base(name)
        {
        }

        private double RandomPVValue()
        {
            return new Random().Next(0, 1000);
        }

    }
}


