using ConsumerEndpoint.Controllers;
using ConsumerEndpoint.Consumer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<IPowergrid, Powergrid>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7272/Powergrid/");
});

builder.Services.AddSingleton<ConsumerListener>();

builder.Services.AddHostedService<Consumer>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();