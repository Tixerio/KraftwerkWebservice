using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Powergrid2.PowerGrid;
using Powergrid2.Controllers;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the Grid as a singleton
builder.Services.AddSingleton<Grid>();

// Register the HttpClient for GridRequester
builder.Services.AddHttpClient<IGridRequester, GridRequester>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7258/Consumer/");
});

// Register the GridRequester as a hosted service
builder.Services.AddHostedService<GridRequester>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Host.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();