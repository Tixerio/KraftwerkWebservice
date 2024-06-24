using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<ApplicationOptions>(
    builder.Configuration.GetSection("HubCon"));
builder.Services.ConfigureOptions<ApplicationOptionsSetup>();

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Register HubConnection as a singleton
builder.Services.AddSingleton(sp =>
{
    var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<ApplicationOptions>>();
    var hubConnection = new HubConnectionBuilder()
        .WithUrl(optionsMonitor.CurrentValue.HubAddress)
        .Build();
        

    return hubConnection;
});

// Register MyProgram as a transient service
builder.Services.AddTransient<MyProgram>();

// Build the application.
var app = builder.Build();

// Retrieve and log the ApplicationOptions during startup.
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<ApplicationOptions>>();

optionsMonitor.OnChange(options =>
{
    logger.LogInformation("Application Address Updated: {HubAddress}", options.HubAddress);
    // Optionally handle the reconnection logic here if needed
});

app.Run();

Task.Run(async () =>
{
    Console.WriteLine("Test");
    var myProgram = app.Services.GetRequiredService<MyProgram>();
    await myProgram.MyMethod();
});



public class MyProgram
{
    private readonly HubConnection _hub;

    public MyProgram(HubConnection hub)
    {
        _hub = hub;
    }

    public async Task MyMethod()
    {
        await _hub.StartAsync();
        try
        {
            await _hub.SendAsync("GetEnergyR");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        _hub.On<string>("ReceiveMessage",
            message => Console.WriteLine($"SignalR Hub Message: {message}"));
    }
}

public class ApplicationOptions
{
    public const string Key = "HubCon";

    [Required(ErrorMessage = "Address Required")]
    public string HubAddress { get; set; }
}

public class ApplicationOptionsSetup : IConfigureOptions<ApplicationOptions>
{
    private const string SectionName = "HubCon"; // Ensure this matches your JSON section
    private readonly IConfiguration _configuration;

    public ApplicationOptionsSetup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(ApplicationOptions options)
    {
        _configuration.GetSection(SectionName).Bind(options);
    }
}
