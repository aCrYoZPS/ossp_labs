using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using ChatLibrary;
using ChatLibrary.Entities;

namespace ChatServer;

public class ChatServer
{
    private readonly Guid id;
    private readonly ConcurrentDictionary<string, NamedPipeServerStream> clients = new();

    public ChatServer()
    {
        id = Guid.NewGuid();
    }

    public async Task RunAsync()
    {
        Console.WriteLine($"Server with id {id} has started");

        while (true)
        {
            var pipeServerStream = new NamedPipeServerStream($"{Globals.PipeNamePrefix}_{id}",
                                                             PipeDirection.InOut,
                                                             10,
                                                             PipeTransmissionMode.Message,
                                                             PipeOptions.Asynchronous);

            await pipeServerStream.WaitForConnectionAsync();
            Task.Run(() => HandleClientAsync(pipeServerStream));
        }
    }

    private async Task<string?> ReadFromPipeAsync(NamedPipeServerStream client)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[Globals.BufferSize];
        while (true)
        {
            int bytesRead = await client.ReadAsync(buffer);
            if (bytesRead == 0)
            {
                return null;
            }

            ms.Write(buffer, 0, bytesRead);

            if (client.IsMessageComplete)
            {
                try
                {
                    byte[] completeMessage = ms.ToArray();
                    var message = Encoding.UTF8.GetString(completeMessage);
                    return message;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Caught exception {ex} while trying to decode message");
                    return "Strange bytest";
                }
            }
        }
    }


    private static Message? ParseMessage(string content)
    {
        try
        {
            return JsonSerializer.Deserialize<Message>(content);
        }
        catch
        {
            return null;
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream client)
    {
        try
        {
            while (client.IsConnected)
            {
                var messageString = await ReadFromPipeAsync(client);
                if (messageString is null)
                {
                    break;
                }

                var message = ParseMessage(messageString);

                if (message is null)
                {
                    continue;
                }


                if (message.MessageType == MessageType.Disconnect)
                {
                    Console.WriteLine($"[{message.Sender} -> Server] Bye, Server");
                    break;
                }
                else if (message.MessageType == MessageType.Connect)
                {
                    clients[message.Sender] = client;
                    Console.WriteLine($"[{message.Sender} -> Server] Hello, Server");
                    var response = new MessageBuilder().SetType(MessageType.Regular)
                                                       .WithRecipient(message.Sender)
                                                       .WithContent($"Hello, {message.Sender}")
                                                       .WithSender("Server")
                                                       .Build();

                    await SendMessageAsync(message.Sender, JsonSerializer.Serialize(response));
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
                        var success = await SendMessageAsync(message.Recipient, JsonSerializer.Serialize(response));
                        if (!success)
                        {
                            var errorResponse = new MessageBuilder().SetType(MessageType.Regular)
                                                                    .WithRecipient(message.Sender)
                                                                    .WithContent($"User with name {message.Recipient} not found.")
                                                                    .WithSender("Server")
                                                                    .Build();

                            await SendMessageAsync(message.Sender, JsonSerializer.Serialize(errorResponse));
                        }
                    }
                }
            }

            Console.WriteLine("Client disconnected");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client: {ex.Message}");
        }
        finally
        {
            var item = clients.First(kvp => kvp.Value == client);

            clients.TryRemove(item.Key, out _);
            client.Dispose();
        }
    }

    private async Task<bool> SendMessageAsync(string recipient, string message)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message + "\n");
        var clientExists = clients.TryGetValue(recipient, out var client);

        if (!clientExists)
        {
            return false;
        }

        if (client!.IsConnected)
        {
            try
            {
                await client.WriteAsync(messageBytes);
                await client.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Caught {ex.Message} while broadcasting");
            }
        }

        return true;
    }

    private async Task BroadcastMessageAsync(string message)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message + "\n");
        foreach (var client in clients.Values)
        {
            if (client.IsConnected)
            {
                try
                {
                    await client.WriteAsync(messageBytes);
                    await client.FlushAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Caught {ex.Message} while broadcasting");
                }
            }
        }
    }
}
