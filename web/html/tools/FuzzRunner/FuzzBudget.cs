using System.Diagnostics;
using System.Text;

namespace UniversalHtml.FuzzRunner;

internal sealed class FuzzBudget
{
    readonly Stopwatch stopwatch;
    readonly long maximumWork;
    readonly TimeSpan maximumTime;
    long usedWork;

    public FuzzBudget(long maximumWorkValue, TimeSpan maximumTimeValue)
    {
        maximumWork = maximumWorkValue;
        maximumTime = maximumTimeValue;
        stopwatch = Stopwatch.StartNew();
    }

    public long GetUsedWork()
    {
        return usedWork;
    }

    public TimeSpan GetElapsed()
    {
        return stopwatch.Elapsed;
    }

    public bool TrySpend(long amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (stopwatch.Elapsed >= maximumTime
            || amount > maximumWork - usedWork)
        {
            return false;
        }

        usedWork += amount;
        return true;
    }

    public bool IsAvailable()
    {
        return usedWork < maximumWork && stopwatch.Elapsed < maximumTime;
    }
}

internal sealed class GrammarWriter
{
    readonly StringBuilder output;
    readonly FuzzBudget budget;
    readonly int maximumLength;

    public GrammarWriter(FuzzBudget budgetValue, int maximumLengthValue)
    {
        budget = budgetValue;
        maximumLength = maximumLengthValue;
        output = new StringBuilder(Math.Min(maximumLengthValue, 1024));
    }

    public int GetLength()
    {
        return output.Length;
    }

    public bool CanContinue()
    {
        return output.Length < maximumLength && budget.IsAvailable();
    }

    public bool Append(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (!budget.TrySpend(1) || output.Length >= maximumLength)
        {
            return false;
        }

        int available = maximumLength - output.Length;
        output.Append(value, 0, Math.Min(value.Length, available));
        return value.Length <= available;
    }

    public bool Append(char value)
    {
        if (!budget.TrySpend(1) || output.Length >= maximumLength)
        {
            return false;
        }

        output.Append(value);
        return true;
    }

    public override string ToString()
    {
        return output.ToString();
    }
}
