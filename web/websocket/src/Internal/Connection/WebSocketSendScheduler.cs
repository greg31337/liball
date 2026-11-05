using System;

namespace websocket;

internal class WebSocketScheduledSendFrame
{
    internal byte[] Bytes = [];
    internal int Offset;
    internal WebSocketScheduledSendFrame? Next;
}

internal class WebSocketSendScheduler
{
    int MaximumPendingByteCount;
    int PendingByteCount;
    int PendingDataByteCount;
    int PendingControlByteCount;
    int PendingCloseByteCount;
    WebSocketScheduledSendFrame? DataHead;
    WebSocketScheduledSendFrame? DataTail;
    WebSocketScheduledSendFrame? ControlHead;
    WebSocketScheduledSendFrame? ControlTail;
    WebSocketScheduledSendFrame? CloseFrame;
    WebSocketScheduledSendFrame? CurrentFrame;
    bool CurrentFrameIsControl;
    bool CurrentFrameIsClose;
    bool DataSendsStopped;
    bool CloseFrameQueued;
    bool CloseFrameCompleted;

    internal WebSocketSendScheduler(int maximumPendingByteCount)
    {
        if (maximumPendingByteCount <= 0)
        {
            throw new WebSocketLimitException();
        }

        MaximumPendingByteCount = maximumPendingByteCount;
    }

    internal bool TryQueueDataFrames(byte[][]? frames)
    {
        if (frames == null || frames.Length == 0)
        {
            throw new WebSocketStateException();
        }

        if (DataSendsStopped)
        {
            return false;
        }

        long requiredByteCount = 0;
        int frameIndex = 0;
        while (frameIndex < frames.Length)
        {
            byte[]? frame = frames[frameIndex];
            ValidateFrame(frame);
            if (frame == null)
            {
                throw new WebSocketStateException();
            }

            requiredByteCount = requiredByteCount + frame.Length;
            frameIndex = frameIndex + 1;
        }

        if (requiredByteCount >
            MaximumPendingByteCount - PendingByteCount)
        {
            return false;
        }

        WebSocketScheduledSendFrame? batchHead = null;
        WebSocketScheduledSendFrame? batchTail = null;
        frameIndex = 0;
        while (frameIndex < frames.Length)
        {
            byte[] frame = frames[frameIndex];
            WebSocketScheduledSendFrame scheduled = CopyFrame(frame);
            if (batchTail == null)
            {
                batchHead = scheduled;
            }
            else
            {
                batchTail.Next = scheduled;
            }

            batchTail = scheduled;
            frameIndex = frameIndex + 1;
        }

        AppendDataBatch(batchHead, batchTail);
        int acceptedByteCount = (int)requiredByteCount;
        PendingByteCount = PendingByteCount + acceptedByteCount;
        PendingDataByteCount = PendingDataByteCount + acceptedByteCount;
        return true;
    }

    internal bool TryQueueDataFrame(byte[]? frame)
    {
        ValidateFrame(frame);
        if (frame == null)
        {
            throw new WebSocketStateException();
        }

        if (DataSendsStopped)
        {
            return false;
        }

        if (frame.Length > MaximumPendingByteCount - PendingByteCount)
        {
            return false;
        }

        AppendDataFrame(CopyFrame(frame));
        PendingByteCount = PendingByteCount + frame.Length;
        PendingDataByteCount = PendingDataByteCount + frame.Length;
        return true;
    }

    internal bool TryQueueControlFrame(byte[]? frame)
    {
        ValidateFrame(frame);
        if (frame == null)
        {
            throw new WebSocketStateException();
        }

        if (frame.Length > MaximumPendingByteCount - PendingByteCount)
        {
            return false;
        }

        AppendControlFrame(CopyFrame(frame));
        PendingByteCount = PendingByteCount + frame.Length;
        PendingControlByteCount = PendingControlByteCount + frame.Length;
        return true;
    }

    internal bool TryQueueCloseFrame(byte[]? frame)
    {
        ValidateFrame(frame);
        if (frame == null)
        {
            throw new WebSocketStateException();
        }

        if (CloseFrameQueued || CloseFrameCompleted)
        {
            throw new WebSocketStateException();
        }

        if (frame.Length > MaximumPendingByteCount - PendingByteCount)
        {
            return false;
        }

        CloseFrame = CopyFrame(frame);
        CloseFrameQueued = true;
        PendingByteCount = PendingByteCount + frame.Length;
        PendingCloseByteCount = frame.Length;
        return true;
    }

    internal void StopDataFrames()
    {
        DataSendsStopped = true;
        int removedCount = GetFrameChainByteCount(DataHead);
        ClearFrameChain(DataHead);
        DataHead = null;
        DataTail = null;
        PendingByteCount = PendingByteCount - removedCount;
        PendingDataByteCount = PendingDataByteCount - removedCount;
    }

    internal int WritePendingBytes(Span<byte> destination)
    {
        int destinationOffset = 0;
        while (destinationOffset < destination.Length &&
            PendingByteCount != 0)
        {
            if (CurrentFrame == null)
            {
                SelectNextFrame();
            }

            WebSocketScheduledSendFrame? selected = CurrentFrame;
            if (selected == null)
            {
                throw new WebSocketStateException();
            }

            int writeCount = selected.Bytes.Length - selected.Offset;
            int destinationAvailable = destination.Length - destinationOffset;
            if (writeCount > destinationAvailable)
            {
                writeCount = destinationAvailable;
            }

            selected.Bytes.AsSpan(selected.Offset, writeCount).CopyTo(
                destination.Slice(destinationOffset, writeCount));
            selected.Offset = selected.Offset + writeCount;
            destinationOffset = destinationOffset + writeCount;
            PendingByteCount = PendingByteCount - writeCount;
            if (CurrentFrameIsClose)
            {
                PendingCloseByteCount =
                    PendingCloseByteCount - writeCount;
            }
            else if (CurrentFrameIsControl)
            {
                PendingControlByteCount =
                    PendingControlByteCount - writeCount;
            }
            else
            {
                PendingDataByteCount = PendingDataByteCount - writeCount;
            }

            if (selected.Offset == selected.Bytes.Length)
            {
                Array.Clear(selected.Bytes);
                CurrentFrame = null;
                CurrentFrameIsControl = false;
                if (CurrentFrameIsClose)
                {
                    CloseFrameQueued = false;
                    CloseFrameCompleted = true;
                }

                CurrentFrameIsClose = false;
            }
        }

        return destinationOffset;
    }

    internal int GetPendingByteCount()
    {
        return PendingByteCount;
    }

    internal int GetPendingDataByteCount()
    {
        return PendingDataByteCount;
    }

    internal int GetPendingControlByteCount()
    {
        return PendingControlByteCount;
    }

    internal int GetPendingCloseByteCount()
    {
        return PendingCloseByteCount;
    }

    internal bool GetDataSendsStopped()
    {
        return DataSendsStopped;
    }

    internal bool HasPendingCloseFrame()
    {
        return CloseFrameQueued;
    }

    internal bool HasCompletedCloseFrame()
    {
        return CloseFrameCompleted;
    }

    internal int GetPendingByteCapacity()
    {
        return MaximumPendingByteCount;
    }

    internal int GetPendingByteAvailableCapacity()
    {
        return MaximumPendingByteCount - PendingByteCount;
    }

    internal void ClearPendingBytes()
    {
        ClearFrame(CurrentFrame);
        ClearFrameChain(DataHead);
        ClearFrameChain(ControlHead);
        ClearFrame(CloseFrame);
        CurrentFrame = null;
        CurrentFrameIsControl = false;
        CurrentFrameIsClose = false;
        DataHead = null;
        DataTail = null;
        ControlHead = null;
        ControlTail = null;
        CloseFrame = null;
        PendingByteCount = 0;
        PendingDataByteCount = 0;
        PendingControlByteCount = 0;
        PendingCloseByteCount = 0;
        DataSendsStopped = false;
        CloseFrameQueued = false;
        CloseFrameCompleted = false;
    }

    void SelectNextFrame()
    {
        if (CloseFrame != null)
        {
            CurrentFrame = CloseFrame;
            CloseFrame = null;
            CurrentFrame.Next = null;
            CurrentFrameIsControl = false;
            CurrentFrameIsClose = true;
            return;
        }

        if (ControlHead != null)
        {
            CurrentFrame = ControlHead;
            ControlHead = ControlHead.Next;
            if (ControlHead == null)
            {
                ControlTail = null;
            }

            CurrentFrame.Next = null;
            CurrentFrameIsControl = true;
            CurrentFrameIsClose = false;
            return;
        }

        if (DataHead != null)
        {
            CurrentFrame = DataHead;
            DataHead = DataHead.Next;
            if (DataHead == null)
            {
                DataTail = null;
            }

            CurrentFrame.Next = null;
            CurrentFrameIsControl = false;
            CurrentFrameIsClose = false;
        }
    }

    void AppendDataBatch(
        WebSocketScheduledSendFrame? batchHead,
        WebSocketScheduledSendFrame? batchTail)
    {
        if (batchHead == null || batchTail == null)
        {
            throw new WebSocketStateException();
        }

        if (DataTail == null)
        {
            DataHead = batchHead;
        }
        else
        {
            DataTail.Next = batchHead;
        }

        DataTail = batchTail;
    }

    void AppendDataFrame(WebSocketScheduledSendFrame frame)
    {
        if (DataTail == null)
        {
            DataHead = frame;
        }
        else
        {
            DataTail.Next = frame;
        }

        DataTail = frame;
    }

    void AppendControlFrame(WebSocketScheduledSendFrame frame)
    {
        if (ControlTail == null)
        {
            ControlHead = frame;
        }
        else
        {
            ControlTail.Next = frame;
        }

        ControlTail = frame;
    }

    static WebSocketScheduledSendFrame CopyFrame(byte[] frame)
    {
        byte[] copy = new byte[frame.Length];
        frame.AsSpan().CopyTo(copy);
        WebSocketScheduledSendFrame scheduled =
            new WebSocketScheduledSendFrame();
        scheduled.Bytes = copy;
        return scheduled;
    }

    static void ValidateFrame(byte[]? frame)
    {
        if (frame == null || frame.Length == 0)
        {
            throw new WebSocketStateException();
        }
    }

    static void ClearFrame(WebSocketScheduledSendFrame? frame)
    {
        if (frame != null)
        {
            Array.Clear(frame.Bytes);
            frame.Next = null;
        }
    }

    static void ClearFrameChain(WebSocketScheduledSendFrame? frame)
    {
        WebSocketScheduledSendFrame? current = frame;
        while (current != null)
        {
            WebSocketScheduledSendFrame? next = current.Next;
            Array.Clear(current.Bytes);
            current.Next = null;
            current = next;
        }
    }

    static int GetFrameChainByteCount(
        WebSocketScheduledSendFrame? frame)
    {
        int byteCount = 0;
        WebSocketScheduledSendFrame? current = frame;
        while (current != null)
        {
            byteCount = byteCount +
                current.Bytes.Length - current.Offset;
            current = current.Next;
        }

        return byteCount;
    }
}
