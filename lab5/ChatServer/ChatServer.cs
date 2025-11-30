using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ChatLibrary.Entities;

namespace ChatServer;

public class ConnectedClient : IDisposable
{
    public TcpClient Client { get; }
    public string UserName { get; }

    private readonly StreamWriter _writer;

    private readonly SemaphoreSlim _lock = new(1, 1);

    public ConnectedClient(TcpClient client, string userName)
    {
        Client = client;
        UserName = userName;
        var stream = client.GetStream();
        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
    }

    public async Task SendMessageAsync(string message)
    {
        await _lock.WaitAsync();
        try
        {
            await _writer.WriteLineAsync(message);
            await _writer.FlushAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _writer.Dispose();
        Client.Dispose();
        _lock.Dispose();
    }
}

public class ChatServer
{
    private readonly int _port;
    private readonly ConcurrentDictionary<string, ConnectedClient> _clients = new();

    public ChatServer(int port)
    {
        _port = port;
    }

    public async Task RunAsync()
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        Console.WriteLine($"Server started on port {_port}");

        while (true)
        {
            var tcpClient = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClientAsync(tcpClient));
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient)
    {
        string? currentClientName = null;
        ConnectedClient? connectedClientWrapper = null;

        try
        {
            using var stream = tcpClient.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            string? messageString;
            while ((messageString = await reader.ReadLineAsync()) != null)
            {
                var message = JsonSerializer.Deserialize<Message>(messageString);
                if (message == null) continue;

                if (message.MessageType == MessageType.Connect)
                {
                    currentClientName = message.Sender;

                    connectedClientWrapper = new ConnectedClient(tcpClient, currentClientName);
                    _clients[currentClientName] = connectedClientWrapper;

                    Console.WriteLine($"[{message.Sender} -> Server] Hello, Server!");

                    var response = new MessageBuilder().SetType(MessageType.Regular)
                                                       .WithRecipient(message.Sender)
                                                       .WithContent($"Hello, {message.Sender}!")
                                                       .WithSender("Server")
                                                       .Build();

                    await connectedClientWrapper.SendMessageAsync(JsonSerializer.Serialize(response));
                }
                else if (message.MessageType == MessageType.Disconnect)
                {
                    Console.WriteLine($"[{message.Sender} -> Server] Bye, Server!");
                    break;
                }
                else
                {
                    var response = new MessageBuilder().SetType(MessageType.Regular)
                                                       .WithRecipient(message.Recipient)
                                                       .WithContent(message.Content)
                                                       .WithSender(message.Sender)
                                                       .Build();

                    if (message.Recipient == "all")
                    {
                        Console.WriteLine($"[{message.Sender} -> all] {message.Content}");
                        await BroadcastMessageAsync(JsonSerializer.Serialize(response));
                    }
                    else
                    {
                        Console.WriteLine($"[{message.Sender} -> {message.Recipient}] {message.Content}");
                        var success = await SendDirectMessageAsync(message.Recipient, JsonSerializer.Serialize(response));

                        if (!success && connectedClientWrapper != null)
                        {
                            var error = new MessageBuilder().SetType(MessageType.Regular)
                                                            .WithRecipient(message.Sender)
                                                            .WithContent($"User {message.Recipient} not found.")
                                                            .WithSender("Server")
                                                            .Build();
                            await connectedClientWrapper.SendMessageAsync(JsonSerializer.Serialize(error));
                        }
                    }
                }
            }
        }
        catch (IOException ex)
        {
            // Connection lost
            Console.WriteLine($"Connection lost :( Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            if (currentClientName != null)
            {
                _clients.TryRemove(currentClientName, out _);
                Console.WriteLine($"{currentClientName} disconnected");
            }

            connectedClientWrapper?.Dispose();
        }
    }

    private async Task<bool> SendDirectMessageAsync(string recipientName, string jsonMessage)
    {
        if (_clients.TryGetValue(recipientName, out var clientWrapper))
        {
            try
            {
                await clientWrapper.SendMessageAsync(jsonMessage);
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    private async Task BroadcastMessageAsync(string jsonMessage)
    {
        foreach (var clientWrapper in _clients.Values)
        {
            try
            {
                await clientWrapper.SendMessageAsync(jsonMessage);
            }
            catch
            {
                // Ignore send failures during broadcast
            }
        }
    }
}
