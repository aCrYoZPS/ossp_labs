using System.Text.Json;
using System.IO.Pipes;
using System.Text;
using ChatLibrary.Entities;
namespace ChatClient;

class ChatClient
{
    private readonly string pipeName;
    private readonly string userName;

    public ChatClient(string userName, string pipeName)
    {
        this.pipeName = pipeName;
        this.userName = userName;
    }

    private static async Task SendMessageAsync(StreamWriter writer, Message message)
    {
        await writer.WriteLineAsync(JsonSerializer.Serialize(message));
        await writer.FlushAsync();
    }

    private static Command ProcessCommand(string command)
    {
        var argList = command.Split(' ');

        switch (argList[0])
        {
            case "/q":
                if (argList.Length > 1)
                {
                    throw new Exception("Invalid command arguments: /q does not take any.");
                }

                return new Command { CommandType = CommandType.Quit };
            case "/c":
                if (argList.Length != 2)
                {
                    throw new Exception("Invalid command arguments: /c takes one argument (name of recipient or all).");
                }

                return new Command { CommandType = CommandType.ChangeRecipient, CommandArgument = argList[1] };
            default:
                throw new Exception("Unknown command");

        }
    }

    public async Task RunAsync()
    {
        using var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipeClient.ConnectAsync();

        using var reader = new StreamReader(pipeClient, Encoding.UTF8);
        using var writer = new StreamWriter(pipeClient, Encoding.UTF8) { AutoFlush = true };

        string recipient = "all";

        var connectMessage = new MessageBuilder().SetType(MessageType.Connect)
                                                 .WithSender(userName)
                                                 .WithContent("Hello, Server")
                                                 .WithRecipient("Server")
                                                 .Build();

        var disconnectMessage = new MessageBuilder().SetType(MessageType.Disconnect)
                                                    .WithSender(userName)
                                                    .WithContent("Bye, Server")
                                                    .WithRecipient("Server")
                                                    .Build();

        await SendMessageAsync(writer, connectMessage);

        Console.WriteLine($"Connected to server, {userName}. You can start sending messages:");

        var readingTask = Task.Run(() =>
        {
            string? messageString;
            while ((messageString = reader.ReadLine()) != null)
            {
                if (messageString == null)
                {
                    break;
                }

                var message = JsonSerializer.Deserialize<Message>(messageString);
                if (message != null)
                {
                    Console.WriteLine($"[{message.Sender} -> {message.Recipient}]: {message.Content}");
                }
            }
        });


        while (true)
        {
            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
                continue;

            if (input.StartsWith('/'))
            {
                try
                {
                    var command = ProcessCommand(input);

                    switch (command.CommandType)
                    {
                        case CommandType.ChangeRecipient:
                            recipient = command.CommandArgument ?? "all";
                            Console.WriteLine($"Recipient changed to: {recipient}");
                            break;
                        case CommandType.Quit:
                            await SendMessageAsync(writer, disconnectMessage);
                            return;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                var message = new MessageBuilder().SetType(MessageType.Regular)
                                                  .WithSender(userName)
                                                  .WithContent(input)
                                                  .WithRecipient(recipient)
                                                  .Build();

                await SendMessageAsync(writer, message);
            }
        }
    }
}
