using System;

namespace email;

internal class EmailCryptographicWorkBudget
{
    long maximum;
    long used;

    internal EmailCryptographicWorkBudget(EmailCryptographicLimits limits)
    {
        if (limits == null) throw new ArgumentNullException("limits");
        maximum = limits.GetMaxOperationCount();
        used = 0;
    }

    internal long GetUsedOperationCount() { return used; }
    internal long GetRemainingOperationCount() { return maximum - used; }

    internal void Charge(long operations)
    {
        if (operations <= 0) throw new ArgumentOutOfRangeException("operations");
        long requested = operations > long.MaxValue - used ? long.MaxValue : used + operations;
        if (requested > maximum) throw new EmailLimitException("MaxCryptographicOperationCount", maximum, requested);
        used = requested;
    }
}
