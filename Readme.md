1. Nuget Runterladen fuer alle Projekte
    -> Microsoft.AspNetCore.SignalR.Client

2. Setup server
    -> Erstelle Interface:

        public interface IExampleHubInterface
        {
            Task ReceiveMessage(string message);
        }

    -> Erstelle Hub Class:

        public class ExampleHub : Hub<IExampleHubInterface>
        {
            private Grid grid;

            public PowergridHub(Grid grid)
            {
                this.grid = grid;
            }

            public async Task BroadcastMessage()
            {
                Clients.All.ReceiveMessage("Not registered");
            }

            public override async Task OnConnectedAsync()
            {
                await base.OnConnectedAsync();
                // Example: send a welcome message to all clients when a new client connects
                
            }

            public override async Task OnDisconnectedAsync(Exception? exception)
            {
                await base.OnDisconnectedAsync(exception);
            }
        }

       

2. Setup Client
    -> Erstelle ein Hub Objekt:

        HubConnection hub = new HubConnectionBuilder()
        .WithUrl("https://ExampleURL:Port")
        .Build();

    -> Verbindung herstellen:

        await hub.StartAsync();

3. Kommunikation Client -> Server (Beispiel)

    await hub.SendAsync("MethodenName", OptionaleDaten);

        //Der MethodenName ist gleich zu der im Server verwendeten Methode, z.B. wenn man "BroadcastMessage" verwendet, wird die Funktion mit selben Namen auf dem
        //Server aufgerufen, diese spezifische Methode schickt dann eine Nachricht an alle Clients

4. Kommunikation Server -> Client

    -> Die im Interface definierten Methoden, werden hier aequivalent zur Client- -> Server-Kommunikation verwendet
       Beispiel:

        Server: 

            Client.[Option].ReceiveMessage(Daten);

            //Option: "All", "Caller",...

        Client (Listener):

            hub.on<string>("ReceiveMessage", 
              message => Console.WriteLine(message));
            
                
           