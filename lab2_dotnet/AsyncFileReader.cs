namespace lab2_dotnet;

class AsyncFileReader
{
    public Action<byte[]>? Action;

    async public Task ReadFileChunksAsync(string path)
    {
        var tasks = new List<Task>();
        var fileSize = new FileInfo(path).Length;
        var chunkCount = Math.Ceiling((double)fileSize / Globals.BUFFER_SIZE);
        using var fs = File.OpenRead(path);

        for (var i = 0; i < chunkCount; ++i)
        {
            long offset = i * Globals.BUFFER_SIZE;
            tasks.Add(ReadChunkAsync(fs, offset));
        }

        while (tasks.Count > 0)
        {
            Task finishedTask = await Task.WhenAny(tasks);
            tasks.Remove(finishedTask);

            await finishedTask;
        }
    }

    async private Task ReadChunkAsync(FileStream fs, long offset)
    {
        var buffer = new byte[Globals.BUFFER_SIZE];
        fs.Seek(offset, SeekOrigin.Begin);
        var readBytes = await fs.ReadAsync(buffer, 0, Globals.BUFFER_SIZE);
        Action?.Invoke(buffer[..readBytes]);
    }
}
