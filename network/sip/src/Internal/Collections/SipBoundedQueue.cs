namespace sip;

internal class SipBoundedQueue
{
    System.Collections.Generic.Queue<object> items;
    object synchronization;
    int capacity;

    internal SipBoundedQueue(int capacity)
    {
        if (capacity <= 0)
        {
            throw new System.ArgumentOutOfRangeException("capacity");
        }

        items = new System.Collections.Generic.Queue<object>();
        synchronization = new object();
        this.capacity = capacity;
    }

    internal bool TryEnqueue(object? item)
    {
        if (item == null)
        {
            return false;
        }

        lock (synchronization)
        {
            if (items.Count >= capacity)
            {
                return false;
            }

            items.Enqueue(item);
            return true;
        }
    }

    internal object? TryDequeue()
    {
        lock (synchronization)
        {
            if (items.Count == 0)
            {
                return null;
            }

            return items.Dequeue();
        }
    }

    internal object? TryPeek()
    {
        lock (synchronization)
        {
            if (items.Count == 0)
            {
                return null;
            }

            return items.Peek();
        }
    }

    internal object[] CopyItems()
    {
        lock (synchronization)
        {
            return items.ToArray();
        }
    }

    internal int GetCount()
    {
        lock (synchronization)
        {
            return items.Count;
        }
    }

    internal int GetCapacity()
    {
        return capacity;
    }

    internal int ClearItems()
    {
        lock (synchronization)
        {
            int removedCount = items.Count;
            items.Clear();
            return removedCount;
        }
    }
}
