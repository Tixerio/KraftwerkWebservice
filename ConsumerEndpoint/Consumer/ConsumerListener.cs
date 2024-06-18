using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly IPowergrid _powergrid;
        private readonly ILogger<Consumer> _logger;

        public Consumer(IPowergrid powergrid, ILogger<Consumer> logger)
        {
            _powergrid = powergrid;
            _logger = logger;
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
                await _powergrid.ChangeEnergy(ct);
                _logger.LogInformation("Has consumed");
                await Task.Delay(2500, ct);
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
    }

    public interface IAsyncInitialization
    {
        public Task Initialization { get; }
    }

    public class Powergrid : IPowergrid, IAsyncInitialization
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<Powergrid> _logger;
        public Task Initialization { get; private set; }

        private int Pulses { get; set; } = 1;

        private string? ID { get; set; }

        public Powergrid(HttpClient httpClient, ILogger<Powergrid> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            Initialization = InitializationAsync();
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

            var result = await _httpClient.PostAsync("ChangeEnergy", JsonContent.Create(this.ID), ct);
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

            var result = await _httpClient.PostAsJsonAsync("Register", member, cancellationToken: ct);
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

