namespace websocket;

internal class WebSocketEventQueue
{
    WebSocketEvent?[] buffer;
    int head;
    int count;

    internal WebSocketEventQueue(int capacity)
    {
        if (capacity <= 0)
        {
            throw new WebSocketLimitException();
        }

        this.buffer = new WebSocketEvent?[capacity];
        this.head = 0;
        this.count = 0;
    }

    internal int GetEventCount()
    {
        return this.count;
    }

    internal int GetEventCapacity()
    {
        return this.buffer.Length;
    }

    internal int GetEventAvailableCapacity()
    {
        return this.buffer.Length - this.count;
    }

    internal bool TryEnqueueEvent(WebSocketEvent? connectionEvent)
    {
        if (connectionEvent == null)
        {
            throw new WebSocketStateException();
        }

        if (this.count == this.buffer.Length)
        {
            return false;
        }

        int writeIndex;
        int tailLength = this.buffer.Length - this.head;
        if (this.count >= tailLength)
        {
            writeIndex = this.count - tailLength;
        }
        else
        {
            writeIndex = this.head + this.count;
        }

        this.buffer[writeIndex] = connectionEvent;
        this.count = this.count + 1;
        return true;
    }

    internal WebSocketEvent? PeekEvent()
    {
        if (this.count == 0)
        {
            return null;
        }

        return this.buffer[this.head];
    }

    internal WebSocketEvent? DequeueEvent()
    {
        if (this.count == 0)
        {
            return null;
        }

        WebSocketEvent? connectionEvent = this.buffer[this.head];
        this.buffer[this.head] = null;
        this.head = this.head + 1;
        if (this.head == this.buffer.Length)
        {
            this.head = 0;
        }

        this.count = this.count - 1;
        if (this.count == 0)
        {
            this.head = 0;
        }

        return connectionEvent;
    }

    internal void ClearEvents()
    {
        int remainingCount = this.count;
        while (remainingCount > 0)
        {
            this.buffer[this.head] = null;
            this.head = this.head + 1;
            if (this.head == this.buffer.Length)
            {
                this.head = 0;
            }

            remainingCount = remainingCount - 1;
        }

        this.head = 0;
        this.count = 0;
    }
}
