using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using static System.Net.WebRequestMethods;

Console.WriteLine("Please specify the URL of SignalR Hub");

var url = "https://localhost:7272/Powergrid";

var hubConnection = new HubConnectionBuilder()
    .WithUrl(url)
    .Build();

hubConnection.On<string>("ReceiveMessage",
    message => Console.WriteLine($"SignalR Hub Message: {message}"));

try
{
    await hubConnection.StartAsync();

    while (true)
    {
        var message = string.Empty;

        Console.WriteLine("Please specify the action:");
        Console.WriteLine("0 - broadcast to all");
        Console.WriteLine("exit - Exit the program");

        var action = Console.ReadLine();

        Console.WriteLine("Please specify the message:");
        message = Console.ReadLine();

        if (action == "exit")
            break;

        await hubConnection.SendAsync("BroadcastMessage");
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
    return;
}

public class Test
{
    public string Name = "fabi";
    public int Id = 2;

 
}

public interface ILearningHubClient
{
    Task ReceiveMessage(string message);
}
