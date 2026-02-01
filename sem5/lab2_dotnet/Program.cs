using lab2_dotnet;
using System.Diagnostics;


var fileSize = new FileInfo(Globals.BINARY_FILE_PATH).Length;
Console.WriteLine($"File size: {fileSize} bytes");
var bm = new Benchmark();
bm.RunAll();

public class Benchmark
{
    private readonly FileReader fileReader = new FileReader() { Action = QuickSort };
    private readonly AsyncFileReader asyncFileReader = new AsyncFileReader() { Action = QuickSort };
    private readonly AsyncMultithreadedFileReader asyncMultithreadedFileReader = new AsyncMultithreadedFileReader { Action = QuickSort };

    public void ChunkFileRead()
    {
        var sw = Stopwatch.StartNew();
        fileReader.ReadFileChunks(Globals.BINARY_FILE_PATH);
        sw.Stop();
        Console.WriteLine($"ChunkFileRead elapsed time: {sw.Elapsed:mm\\:ss\\.fff}");
    }

    public void FullFileRead()
    {
        var sw = Stopwatch.StartNew();
        fileReader.ReadFileFull(Globals.BINARY_FILE_PATH);
        sw.Stop();
        Console.WriteLine($"FullFileRead elapsed time: {sw.Elapsed:mm\\:ss\\.fff}");
    }

    public void AsyncChunkFileRead()
    {
        var sw = Stopwatch.StartNew();
        asyncFileReader.ReadFileChunksAsync(Globals.BINARY_FILE_PATH).Wait();
        sw.Stop();
        Console.WriteLine($"AsyncChunkFileRead elapsed time: {sw.Elapsed:mm\\:ss\\.fff}");
    }

    public void AsyncMultithreadedChunkFileRead()
    {
        var sw = Stopwatch.StartNew();
        asyncMultithreadedFileReader.ReadFileChunksMultithreadedAsync(Globals.BINARY_FILE_PATH).Wait();
        sw.Stop();
        Console.WriteLine($"AsyncMultithreadedChunkFileRead elapsed time: {sw.Elapsed:mm\\:ss\\.fff}");
    }

    public void AsyncMultithreadedThreadPoolChunkFileRead()
    {
        var sw = Stopwatch.StartNew();
        asyncMultithreadedFileReader.ReadFileChunksMultithreadedThreadPoolAsync(Globals.BINARY_FILE_PATH).Wait();
        sw.Stop();
        Console.WriteLine($"AsyncMultithreadedChunkFileRead elapsed time: {sw.Elapsed:mm\\:ss\\.fff}");
    }

    public void RunAll()
    {
        ChunkFileRead();
        // FullFileRead();
        AsyncChunkFileRead();
        AsyncMultithreadedChunkFileRead();
        AsyncMultithreadedThreadPoolChunkFileRead();
    }

    public static void LinearAction(byte[] bytes)
    {
        int n = bytes.Length;
        int aCount = 0;
        for (int i = 0; i < n; ++i)
        {
            if (bytes[i] == 'a')
            {
                aCount += 1;
            }
        }
    }

    public static void SortByteBuffer(byte[] bytes)
    {
        int n = bytes.Length;
        bool swapped;

        for (int i = 0; i < n - 1; i++)
        {
            swapped = false;
            for (int j = 0; j < n - 1 - i; j++)
            {
                if (bytes[j] > bytes[j + 1])
                {
                    (bytes[j + 1], bytes[j]) = (bytes[j], bytes[j + 1]);
                    swapped = true;
                }
            }

            if (!swapped)
            {
                break;
            }
        }
    }

    static void QuickSort(byte[] array)
    {
        QuickSortInternal(array, 0, array.Length - 1);
    }

    static void QuickSortInternal(byte[] array, int low, int high)
    {
        if (low < high)
        {
            int pivotIndex = Partition(array, low, high);
            QuickSortInternal(array, low, pivotIndex - 1);
            QuickSortInternal(array, pivotIndex + 1, high);
        }
    }

    static int Partition(byte[] array, int low, int high)
    {
        byte pivot = array[high];
        int i = low - 1;

        for (int j = low; j < high; j++)
        {
            if (array[j] <= pivot)
            {
                i++;
                Swap(array, i, j);
            }
        }

        Swap(array, i + 1, high);
        return i + 1;
    }

    static void Swap(byte[] array, int i, int j)
    {
        byte temp = array[i];
        array[i] = array[j];
        array[j] = temp;

    }
}

