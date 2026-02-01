namespace ChatLibrary.Entities;

public class MessageBuilder
{
    private Message message = new Message();

    public MessageBuilder WithRecipient(string recipient)
    {
        message.Recipient = recipient;
        return this;
    }

    public MessageBuilder WithContent(string content)
    {
        message.Content = content;
        return this;
    }

    public MessageBuilder WithSender(string sender)
    {
        message.Sender = sender;
        return this;
    }

    public MessageBuilder SetType(MessageType messageType)
    {
        message.MessageType = messageType;
        return this;
    }

    public Message Build()
    {
        return message;
    }
}
