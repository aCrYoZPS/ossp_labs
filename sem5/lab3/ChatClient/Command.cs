namespace ChatClient;

public class Command
{
    public CommandType CommandType { get; set; }
    public string? CommandArgument { get; set; } = null;
}
