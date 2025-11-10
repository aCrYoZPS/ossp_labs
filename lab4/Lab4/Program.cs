using Lab4;

ExecuteMutex();
ExecuteGlobalMutex();
ExecuteSemaphore();

void ExecuteMutex()
{
    Console.WriteLine("Forks as mutexes:");
    var threads = new List<Thread>();
    var mutexForks = new List<Mutex> { new(), new(), new(), new(), new() };
    var mutexPhilosophers = new List<Philosopher> {
        new (1, mutexForks[4], mutexForks[0]),
        new (2, mutexForks[0], mutexForks[1]),
        new (3, mutexForks[1], mutexForks[2]),
        new (4, mutexForks[2], mutexForks[3]),
        new (5, mutexForks[3], mutexForks[4]),
    };

    var waitTimes = new int[5];
    var thinkTimes = new int[5];
    var eatTimes = new int[5];

    var idx = 0;
    foreach (var p in mutexPhilosophers)
    {
        int currentIndex = idx;
        var thread = new Thread(() => p.Dine(ref waitTimes[currentIndex], ref thinkTimes[currentIndex], ref eatTimes[currentIndex]));
        thread.Start();
        threads.Add(thread);
        idx += 1;
    }

    foreach (var thread in threads)
    {
        thread.Join();
    }

    for (var i = 0; i < mutexPhilosophers.Count; ++i)
    {
        var totalTime = waitTimes[i] + thinkTimes[i] + eatTimes[i];
        Console.WriteLine($"Philosopher {i + 1} waited for {(float)waitTimes[i] / 1000}s => {(float)waitTimes[i] / totalTime * 100}%, " +
                $"thought for {(float)thinkTimes[i] / 1000}s => {(float)thinkTimes[i] / totalTime * 100}% and " +
                $"ate for {(float)eatTimes[i] / 1000}s => {(float)eatTimes[i] / totalTime * 100}%");
    }

}

void ExecuteGlobalMutex()
{
    Console.WriteLine("One global mutex:");
    var threads = new List<Thread>();
    var globalMutex = new Mutex();

    var globalMutexPhilosophers = new List<PhilosopherGlobalMutex> {
        new (1, globalMutex),
        new (2, globalMutex),
        new (3, globalMutex),
        new (4, globalMutex),
        new (5, globalMutex),
    };

    var waitTimes = new int[5];
    var thinkTimes = new int[5];
    var eatTimes = new int[5];

    var idx = 0;
    foreach (var p in globalMutexPhilosophers)
    {
        int currentIndex = idx;
        var thread = new Thread(() => p.Dine(ref waitTimes[currentIndex], ref thinkTimes[currentIndex], ref eatTimes[currentIndex]));
        thread.Start();
        threads.Add(thread);
        idx += 1;
    }

    foreach (var thread in threads)
    {
        thread.Join();
    }

    for (var i = 0; i < globalMutexPhilosophers.Count; ++i)
    {
        var totalTime = waitTimes[i] + thinkTimes[i] + eatTimes[i];
        Console.WriteLine($"Philosopher {i + 1} waited for {(float)waitTimes[i] / 1000}s => {(float)waitTimes[i] / totalTime * 100}%, " +
                $"thought for {(float)thinkTimes[i] / 1000}s  => {(float)thinkTimes[i] / totalTime * 100}% and " +
                $"ate for {(float)eatTimes[i] / 1000}s => {(float)eatTimes[i] / totalTime * 100}%");
    }
}

void ExecuteSemaphore()
{
    Console.WriteLine("Semaphore:");
    var threads = new List<Thread>();
    var semaphore = new Semaphore(2, 2);

    var semaphorePhilosophers = new List<PhilosopherSemaphore> {
        new (1, semaphore),
        new (2, semaphore),
        new (3, semaphore),
        new (4, semaphore),
        new (5, semaphore),
    };

    var waitTimes = new int[5];
    var thinkTimes = new int[5];
    var eatTimes = new int[5];

    var idx = 0;
    foreach (var p in semaphorePhilosophers)
    {
        int currentIndex = idx;
        var thread = new Thread(() => p.Dine(ref waitTimes[currentIndex], ref thinkTimes[currentIndex], ref eatTimes[currentIndex]));
        thread.Start();
        threads.Add(thread);
        idx += 1;
    }

    foreach (var thread in threads)
    {
        thread.Join();
    }

    for (var i = 0; i < semaphorePhilosophers.Count; ++i)
    {
        var totalTime = waitTimes[i] + thinkTimes[i] + eatTimes[i];
        Console.WriteLine($"Philosopher {i + 1} waited for {(float)waitTimes[i] / 1000}s => {(float)waitTimes[i] / totalTime * 100}%, " +
                $"thought for {(float)thinkTimes[i] / 1000}s  => {(float)thinkTimes[i] / totalTime * 100}% and " +
                $"ate for {(float)eatTimes[i] / 1000}s => {(float)eatTimes[i] / totalTime * 100}%");
    }
}
