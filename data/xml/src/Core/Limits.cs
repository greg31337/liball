using System;

namespace xml;

internal class Limits
{
    internal long Maximum;
    internal long Used;

    internal Limits(long maximum)
    {
        Guard.NonNegative(maximum, "maximum");
        Maximum = maximum;
        Used = 0;
    }

    internal long Remaining()
    {
        return Maximum - Used;
    }

    internal bool TryConsume(long amount)
    {
        Guard.NonNegative(amount, "amount");
        if (amount > Maximum - Used)
        {
            return false;
        }

        Used = Used + amount;
        return true;
    }

    internal void Consume(long amount)
    {
        if (!TryConsume(amount))
        {
            throw new InvalidOperationException("The configured limit was exceeded.");
        }
    }

    internal void Release(long amount)
    {
        Guard.NonNegative(amount, "amount");
        if (amount > Used)
        {
            throw new ArgumentOutOfRangeException("amount");
        }

        Used = Used - amount;
    }

    internal void Reset()
    {
        Used = 0;
    }
}
