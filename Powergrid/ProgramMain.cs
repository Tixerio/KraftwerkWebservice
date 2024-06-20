using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Powergrid.PowerGrid;
using Microsoft.AspNetCore.SignalR.Client;

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
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<PowergridHub>("/Powergrid");


// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();