using ConsumerEndpoint.Consumer;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<IPowergrid, Powergrid>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7272/Powergrid/");
});

builder.Services.AddLogging(
    loggingBuilder =>
    {
        loggingBuilder.AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("System", LogLevel.Warning)
            .AddFilter("NToastNotify", LogLevel.Warning)
            .AddConsole();
    });

builder.Services.AddSingleton<ConsumerListener>();

builder.Services.AddHostedService<Consumer>();
builder.Services.AddTransient<HubConnection>((sp) => new HubConnectionBuilder()
    .WithUrl("https://localhost:7272/Powergrid")
    .Build());



var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();