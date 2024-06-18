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

        private readonly IPowergrid powergrid;
        private double LastCheckedEnergy { get; set; }


        public Consumer(IPowergrid powergrid)
        {
            this.powergrid = powergrid;
        }


        public async Task ConsumeEnergy(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (!Consuming.isConsuming)
                {
                    Console.WriteLine("Not currently consuming.");
                    await Task.Delay(2500, ct);
                    continue;
                }
                await powergrid.ChangeEnergy(ct);
                Console.WriteLine("Has consumed");
                await Task.Delay(2500, ct);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ConsumeEnergy(stoppingToken);
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
        private readonly HttpClient httpClient;
        public Task Initialization { get; private set; }

        private int Pulses { get; set; } = 1;

        private String? ID { get; set; }

        public Powergrid(HttpClient httpClient)
        {
            this.httpClient = httpClient;
            Initialization = InitializationAync();
        }

        public Task InitializationAync()
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

            String registered = await result.Content.ReadAsStringAsync(ct);
            Console.WriteLine(await result.Content.ReadAsStringAsync(ct));
            if (registered != "Registered")
            {
                Pulses = 1;
                Console.WriteLine(registered + "\nPress any button to register...");
                Console.ReadKey(true);
                Register(ct);
            }

            //result.IsSuccessStatusCode
            result.EnsureSuccessStatusCode();
        }


        public async Task Register(CancellationToken ct)
        {

            var member = new MemberObject() { Name = "Konsument", Type = "Consumer" };

            var result = await httpClient.PostAsJsonAsync("Register", member);
            this.ID = await result.Content.ReadAsStringAsync();
        }

        public class MemberObject
        {
            public string Name { get; set; }
            public string Type { get; set; }
        }

    }
}
