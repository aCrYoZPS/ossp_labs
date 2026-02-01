using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ChatLibrary.Entities;

namespace ChatClient;

class ChatClient
{
    private readonly string _serverIp = "127.0.0.1";
    private readonly int _serverPort;
    private readonly string _userName;

    public ChatClient(string userName, int port)
    {
        _userName = userName;
        _serverPort = port;
    }

    private static async Task SendMessageAsync(StreamWriter writer, Message message)
    {
        await writer.WriteLineAsync(JsonSerializer.Serialize(message));
        await writer.FlushAsync();
    }

    public async Task RunAsync()
    {
        using var tcpClient = new TcpClient();
        Console.WriteLine($"Connecting to {_serverIp}:{_serverPort}...");
        await tcpClient.ConnectAsync(_serverIp, _serverPort);

        using var stream = tcpClient.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        Console.WriteLine($"Connected as {_userName}.");

        var readingTask = Task.Run(async () =>
        {
            try
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var msg = JsonSerializer.Deserialize<Message>(line);
                    if (msg != null)
                    {
                        Console.WriteLine($"[{msg.Sender} -> {msg.Recipient}]: {msg.Content}");
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Disconnected from server.");
            }
        });

        var connectMessage = new MessageBuilder().SetType(MessageType.Connect)
                                                 .WithSender(_userName)
                                                 .WithContent("Hello")
                                                 .WithRecipient("Server")
                                                 .Build();

        await SendMessageAsync(writer, connectMessage);

        string recipient = "all";
        while (true)
        {
            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input)) continue;

            if (input.StartsWith("/q"))
            {
                var quitMsg = new MessageBuilder().SetType(MessageType.Disconnect)
                                                  .WithSender(_userName)
                                                  .WithRecipient("Server")
                                                  .WithContent("Bye")
                                                  .Build();
                await SendMessageAsync(writer, quitMsg);
                await Task.Delay(200);
                return;
            }
            else if (input.StartsWith("/c "))
            {
                recipient = input.Split(' ')[1];
                Console.WriteLine($"Recipient set to: {recipient}");
            }
            else
            {
                var msg = new MessageBuilder().SetType(MessageType.Regular)
                                              .WithSender(_userName)
                                              .WithRecipient(recipient)
                                              .WithContent(input)
                                              .Build();

                await SendMessageAsync(writer, msg);
            }
        }
    }
}
