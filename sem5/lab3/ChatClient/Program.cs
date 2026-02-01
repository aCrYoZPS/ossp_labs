using ChatLibrary;

var namedPipes = Directory.GetFiles(@"\\.\pipe\")
                          .Select(p => p.Replace(@"\\.\pipe\", ""))
                          .Where(p => p.StartsWith(Globals.PipeNamePrefix))
                          .ToList();

if (namedPipes.Count == 0)
{
    Console.WriteLine("Failed to find any servers to connect to. Aborting...");
    return;
}

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

Console.WriteLine($"Connecting to {namedPipes[0]}");
var client = new ChatClient.ChatClient(userName, namedPipes[0]);
await client.RunAsync();
