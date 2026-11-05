namespace sip;

internal class SipLimitCounter
{
    object synchronization;
    int limit;
    int usedCount;

    internal SipLimitCounter(int limit)
    {
        if (limit < 0)
        {
            throw new System.ArgumentOutOfRangeException("limit");
        }

        synchronization = new object();
        this.limit = limit;
        usedCount = 0;
    }

    internal bool TryAcquire(int count)
    {
        if (count <= 0)
        {
            return false;
        }

        lock (synchronization)
        {
            if (count > limit - usedCount)
            {
                return false;
            }

            usedCount = usedCount + count;
            return true;
        }
    }

    internal void Release(int count)
    {
        if (count <= 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        lock (synchronization)
        {
            if (count > usedCount)
            {
                throw new System.InvalidOperationException("Cannot release more capacity than is currently acquired.");
            }

            usedCount = usedCount - count;
        }
    }

    internal int GetLimit()
    {
        return limit;
    }

    internal int GetUsedCount()
    {
        lock (synchronization)
        {
            return usedCount;
        }
    }

    internal int GetAvailableCount()
    {
        lock (synchronization)
        {
            return limit - usedCount;
        }
    }
}
