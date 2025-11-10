using System.Diagnostics;

namespace Lab4;

class PhilosopherSemaphore
{
    private readonly int id;
    private readonly Semaphore semaphore;
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
    private readonly List<int> currentEaters = [];
    private static readonly Lock lockObject = new();

    public PhilosopherSemaphore(int id, Semaphore semaphore)
    {
        this.id = id;
        this.semaphore = semaphore;
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
        bool acquired = semaphore.WaitOne(timeout);
        if (acquired)
        {
            lock (lockObject)
            {
                if (currentEaters.Count == 2)
                {
                    semaphore.Release();
                    return false;
                }
                else if (currentEaters.Count == 1 && Math.Abs(currentEaters[0] - id) == 1)
                {
                    semaphore.Release();
                    return false;
                }

                currentEaters.Add(id);
            }
        }
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
        currentEaters.Remove(id);
        semaphore.Release();
    }
}
