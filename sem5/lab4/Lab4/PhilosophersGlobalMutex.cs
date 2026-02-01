using System.Diagnostics;

namespace Lab4;

class PhilosopherGlobalMutex
{
    private readonly int id;
    private readonly Mutex mutex;
    private readonly Random rnd;
    private readonly int thinkTimeMin = 500;
    private readonly int thinkTimeMax = 2000;
    private readonly int eatTimeMin = 1000;
    private readonly int eatTimeMax = 3000;
    private readonly int timeout = 1000;
    private int foodCount = 0;
    private int waitTotal = 0;
    private int thinkTotal = 0;
    private int eatTotal = 0;

    public PhilosopherGlobalMutex(int id, Mutex mutex)
    {
        this.id = id;
        this.mutex = mutex;
        rnd = new Random(id);
    }

    public void Dine(ref int totalWaitTime, ref int totalThinkTime, ref int totalEatTime)
    {
        while (true)
        {
            if (foodCount > 5)
            {
                totalWaitTime = waitTotal;
                totalThinkTime = thinkTotal;
                totalEatTime = eatTotal;
                return;
            }

            Think();
            if (TryPickUpForks())
            {
                Eat();
                PutDownForks();
                foodCount += 1;
            }
        }
    }

    private void Think()
    {
        var thinkTime = rnd.Next(thinkTimeMin, thinkTimeMax);
        thinkTotal += thinkTime;
        Thread.Sleep(thinkTime);
    }

    private bool TryPickUpForks()
    {
        var stopwatch = Stopwatch.StartNew();
        bool acquired = mutex.WaitOne(timeout);
        waitTotal += (int)stopwatch.ElapsedMilliseconds;
        stopwatch.Stop();
        return acquired;
    }

    private void Eat()
    {
        var eatTime = rnd.Next(eatTimeMin, eatTimeMax);
        Thread.Sleep(rnd.Next(eatTimeMin, eatTimeMax));
        eatTotal += eatTime;
    }

    private void PutDownForks()
    {
        mutex.ReleaseMutex();
    }
}
