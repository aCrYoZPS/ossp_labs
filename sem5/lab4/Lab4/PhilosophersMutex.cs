using System.Diagnostics;

namespace Lab4;

class Philosopher
{
    private readonly int id;
    private readonly Mutex leftFork;
    private readonly Mutex rightFork;
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

    public Philosopher(int id, Mutex left, Mutex right)
    {
        this.id = id;
        leftFork = left;
        rightFork = right;
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
        bool leftAcquired = leftFork.WaitOne(timeout);
        waitTotal += (int)stopwatch.ElapsedMilliseconds;
        if (!leftAcquired) return false;

        stopwatch = Stopwatch.StartNew();
        bool rightAcquired = rightFork.WaitOne(timeout);
        waitTotal += (int)stopwatch.ElapsedMilliseconds;
        if (!rightAcquired)
        {
            leftFork.ReleaseMutex();
            return false;
        }
        return true;
    }

    private void Eat()
    {
        var eatTime = rnd.Next(eatTimeMin, eatTimeMax);
        Thread.Sleep(rnd.Next(eatTimeMin, eatTimeMax));
        eatTotal += eatTime;
    }

    private void PutDownForks()
    {
        leftFork.ReleaseMutex();
        rightFork.ReleaseMutex();
    }
}
