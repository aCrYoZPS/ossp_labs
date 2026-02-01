namespace lab2_dotnet;

public class FileReader
{
    public Action<byte[]>? Action;

    public void ReadFileChunks(string path)
    {
        var buffer = new byte[Globals.BUFFER_SIZE];
        using var fs = File.OpenRead(path);

        while (true)
        {
            var readBytes = fs.Read(buffer, 0, Globals.BUFFER_SIZE);
            if (readBytes == 0)
            {
                return;
            }

            Action?.Invoke(buffer[..readBytes]);
        }
    }

    public void ReadFileFull(string path)
    {
        var bytes = File.ReadAllBytes(path);
        Action?.Invoke(bytes);
    }
}
