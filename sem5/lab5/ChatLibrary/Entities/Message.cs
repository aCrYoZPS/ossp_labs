namespace ChatLibrary.Entities;

public class Message
{
    public string Sender { get; set; }
    public string Content { get; set; }
    public string Recipient { get; set; } = "all";
    public MessageType MessageType { get; set; } = MessageType.Regular;
}
