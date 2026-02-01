string userName;

while (true)
{
    Console.WriteLine("Enter your username:");
    var input = Console.ReadLine();

    if (input?.ToLower() == "all")
    {
        Console.WriteLine($"Invalid username. Username 'all' is used for broadcasting.");
        continue;
    }

    if (input?.ToLower() == "server")
    {
        Console.WriteLine($"Invalid username. Username 'server' is used for server communication.");
        continue;
    }

    if (input?.Length > 0 && input?.Length < 100)
    {
        userName = input;
        break;
    }

    Console.WriteLine("Invalid username. Username should consist from 1-100 ASCII characters.");
}

var port = 5000;
var client = new ChatClient.ChatClient(userName, port);
await client.RunAsync();
