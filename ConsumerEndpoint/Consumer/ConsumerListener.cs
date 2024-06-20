using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace ConsumerEndpoint.Consumer
{
    static class Consuming
    {
        public static bool isConsuming = true;
    }

    public class ConsumerListener
    {
        private readonly ILogger<ConsumerListener> _logger;

        public ConsumerListener(ILogger<ConsumerListener> logger)
        {
            _logger = logger;
        }
    }

    public class Consumer : BackgroundService
    {
        private readonly IPowergrid powergrid;
        private readonly ILogger<Consumer> _logger;

        public Consumer(IPowergrid powergrid, ILogger<Consumer> logger, HubConnection hub)
        {
            this.powergrid = powergrid;
            _logger = logger;
            powergrid.StartClient();
            powergrid.RegisterR();
        }

        public async Task ConsumeEnergy(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (!Consuming.isConsuming)
                {
                    _logger.LogInformation("Not currently consuming.");
                    await Task.Delay(2500, ct);
                    continue;
                }
                powergrid.ChangeEnergyR();
                await powergrid.ChangeEnergy(ct);
                _logger.LogInformation("Has produced");
                await Task.Delay(2000, ct);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ConsumeEnergy(stoppingToken);
        }
    }

    public interface IPowergrid
    {
        public Task ChangeEnergy(CancellationToken ct);
        //public Task PulseChange(CancellationToken ct);
        public Task Register(CancellationToken ct);
        public void StartClient();
        public void ChangeEnergyR();
        public void RegisterR();
    }

    public interface IAsyncInitialization
    {
        public Task Initialization { get; }
    }

    public class Powergrid : IPowergrid, IAsyncInitialization
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<Powergrid> _logger;
        public Task Initialization { get; private set; }

        private int Pulses { get; set; } = 1;

        private HubConnection hub;
        private string? ID { get; set; }
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

            hub.On<string>("ReceiveMessage",
                message => Console.WriteLine($"SignalR Hub Message: {message}"));

            if (started == false)
            {
                await hub.StartAsync();
                started = true;
            }
        }

        public async void RegisterR()
        {
            await hub.SendAsync("RegisterR", new MemberObject("Fabi", "Powerplant"));

            hub.On<string>("RegisterResponse",
                message => this.ID = message);
        }

        public async void ChangeEnergyR()
        {
            if (this.ID == null)
            {
                return;
            }
            await hub.SendAsync("ChangeEnergyMessage", this.ID);

            hub.On<string>("ChangeEnergyResponse",
                message =>
                {

                    if (message != "Registered")
                    {
                        Pulses = 1;
                        _logger.LogInformation(message + "\nPress any button to register...");
                        Console.ReadKey(true);
                        RegisterR();
                    }
                });

            Console.WriteLine("ID: " + this.ID);
        }

        public Task InitializationAsync()
        {
            Task.Run(async () => await Register(new CancellationToken()));
            return Task.CompletedTask;
        }

        public async Task ChangeEnergy(CancellationToken ct)
        {
            if (this.ID == null)
            {
                return;
            }

            var result = await httpClient.PostAsync("ChangeEnergy", JsonContent.Create(this.ID), ct);
            string registered = await result.Content.ReadAsStringAsync(ct);

            if (registered != "Registered")
            {
                Pulses = 1;
                _logger.LogInformation(registered + "\nPress any button to register...");
                Console.ReadKey(true);
                await Register(ct);
            }

            result.EnsureSuccessStatusCode();
        }

        public async Task Register(CancellationToken ct)
        {
            var member = new MemberObject("Konsument", "Consumer");

            var result = await httpClient.PostAsJsonAsync("Register", member, cancellationToken: ct);
            this.ID = await result.Content.ReadAsStringAsync(ct);
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
}

