namespace lab2_dotnet;

class AsyncMultithreadedFileReader
{
    public Action<byte[]>? Action;
    public int MaxConcurrentTasks = 2;

    async public Task ReadFileChunksMultithreadedAsync(string path)
    {
        var tasks = new List<Task<(int, byte[])>>();
        var threads = new List<Thread>();
        var fileSize = new FileInfo(path).Length;
        var chunkCount = Math.Ceiling((double)fileSize / Globals.BUFFER_SIZE);
        var readChunks = 0;
        using var fs = File.OpenRead(path);
        var semaphore = new Semaphore(MaxConcurrentTasks, MaxConcurrentTasks);

        for (var i = 0; i < MaxConcurrentTasks; ++i)
        {
            long offset = readChunks * Globals.BUFFER_SIZE;
            tasks.Add(ReadChunkAsync(fs, offset));
            readChunks += 1;
        }

        while (tasks.Count > 0)
        {
            Task<(int, byte[])> finishedTask = await Task.WhenAny(tasks);
            tasks.Remove(finishedTask);
            var (ReadBytes, Content) = await finishedTask;

            if (ReadBytes == 0)
            {
                break;
            }

            var thread = new Thread(() =>
            {
                semaphore.WaitOne();
                Action?.Invoke(Content[..ReadBytes]);
                semaphore.Release();
            });

            threads.Add(thread);
            thread.Start();

            long offset = readChunks * Globals.BUFFER_SIZE;
            tasks.Add(ReadChunkAsync(fs, offset));
            readChunks += 1;
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }
    }

    async public Task ReadFileChunksMultithreadedThreadPoolAsync(string path)
    {
        var tasks = new List<Task<(int, byte[])>>();
        var threads = new List<Thread>();
        var fileSize = new FileInfo(path).Length;
        var chunkCount = Math.Ceiling((double)fileSize / Globals.BUFFER_SIZE);
        var readChunks = 0;
        using var fs = File.OpenRead(path);
        var semaphore = new Semaphore(MaxConcurrentTasks, MaxConcurrentTasks);

        for (var i = 0; i < MaxConcurrentTasks; ++i)
        {
            long offset = readChunks * Globals.BUFFER_SIZE;
            tasks.Add(ReadChunkAsync(fs, offset));
            readChunks += 1;
        }

        using var countdown = new CountdownEvent(1);

        while (tasks.Count > 0)
        {
            Task<(int, byte[])> finishedTask = await Task.WhenAny(tasks);
            tasks.Remove(finishedTask);
            var (ReadBytes, Content) = await finishedTask;

            if (ReadBytes == 0)
            {
                break;
            }

            countdown.AddCount();
            ThreadPool.QueueUserWorkItem((_) =>
            {
                semaphore.WaitOne();
                Action?.Invoke(Content[..ReadBytes]);
                countdown.Signal();
                semaphore.Release();
            });

            long offset = readChunks * Globals.BUFFER_SIZE;
            tasks.Add(ReadChunkAsync(fs, offset));
            readChunks += 1;
        }

        countdown.Signal();
        countdown.Wait();
    }

    static async private Task<(int, byte[])> ReadChunkAsync(FileStream fs, long offset)
    {
        var buffer = new byte[Globals.BUFFER_SIZE];
        fs.Seek(offset, SeekOrigin.Begin);
        var readBytes = await fs.ReadAsync(buffer, 0, Globals.BUFFER_SIZE);
        return (readBytes, buffer);
    }
}
