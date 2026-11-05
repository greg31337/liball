using System.Text;

namespace pop3.Tests;

internal sealed class FragmentingReadStream : Stream
{
    readonly Stream Inner;
    readonly int MaximumReadSize;
    readonly bool LeaveOpen;

    internal int ReadCallCount;

    internal FragmentingReadStream(Stream inner, int maximumReadSize, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumReadSize);
        Inner = inner;
        MaximumReadSize = maximumReadSize;
        LeaveOpen = leaveOpen;
    }

    public override bool CanRead => Inner.CanRead;
    public override bool CanSeek => Inner.CanSeek;
    public override bool CanTimeout => Inner.CanTimeout;
    public override bool CanWrite => Inner.CanWrite;
    public override long Length => Inner.Length;

    public override long Position
    {
        get => Inner.Position;
        set => Inner.Position = value;
    }

    public override int ReadTimeout
    {
        get => Inner.ReadTimeout;
        set => Inner.ReadTimeout = value;
    }

    public override int WriteTimeout
    {
        get => Inner.WriteTimeout;
        set => Inner.WriteTimeout = value;
    }

    public override void Flush()
    {
        Inner.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ReadCallCount++;
        return Inner.Read(buffer, offset, Math.Min(count, MaximumReadSize));
    }

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        ReadCallCount++;
        return Inner.ReadAsync(buffer, offset, Math.Min(count, MaximumReadSize), cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return Inner.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        Inner.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Inner.Write(buffer, offset, count);
    }

    public override Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        return Inner.WriteAsync(buffer, offset, count, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !LeaveOpen)
        {
            Inner.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal sealed class ShortWriteStream : Stream
{
    readonly MemoryStream Destination = new MemoryStream();
    readonly int MaximumWriteSize;

    internal int FlushCallCount;
    internal int WriteCallCount;

    internal ShortWriteStream(int maximumWriteSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumWriteSize);
        MaximumWriteSize = maximumWriteSize;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => Destination.Length;

    public override long Position
    {
        get => Destination.Position;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        FlushCallCount++;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (count > MaximumWriteSize)
        {
            throw new InvalidOperationException(
                $"Write size {count} exceeded the configured maximum of {MaximumWriteSize} bytes.");
        }

        WriteCallCount++;
        Destination.Write(buffer, offset, count);
    }

    internal byte[] ToArray()
    {
        return Destination.ToArray();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Destination.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal sealed class InjectedEofStream : Stream
{
    readonly Stream Inner;
    readonly int EofAfterBytes;
    readonly bool LeaveOpen;
    int BytesRead;

    internal int ReadCallCount;

    internal InjectedEofStream(Stream inner, int eofAfterBytes, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfNegative(eofAfterBytes);
        Inner = inner;
        EofAfterBytes = eofAfterBytes;
        LeaveOpen = leaveOpen;
    }

    public override bool CanRead => Inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanTimeout => Inner.CanTimeout;
    public override bool CanWrite => Inner.CanWrite;
    public override long Length => EofAfterBytes;

    public override long Position
    {
        get => BytesRead;
        set => throw new NotSupportedException();
    }

    public override int ReadTimeout
    {
        get => Inner.ReadTimeout;
        set => Inner.ReadTimeout = value;
    }

    public override int WriteTimeout
    {
        get => Inner.WriteTimeout;
        set => Inner.WriteTimeout = value;
    }

    public override void Flush()
    {
        Inner.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ReadCallCount++;
        int allowed = GetAllowedCount(count);
        if (allowed == 0)
        {
            return 0;
        }

        int result = Inner.Read(buffer, offset, allowed);
        BytesRead += result;
        return result;
    }

    public override async Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        ReadCallCount++;
        int allowed = GetAllowedCount(count);
        if (allowed == 0)
        {
            return 0;
        }

        int result = await Inner.ReadAsync(buffer, offset, allowed, cancellationToken);
        BytesRead += result;
        return result;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        Inner.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Inner.Write(buffer, offset, count);
    }

    public override Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        return Inner.WriteAsync(buffer, offset, count, cancellationToken);
    }

    int GetAllowedCount(int requestedCount)
    {
        return Math.Min(requestedCount, EofAfterBytes - BytesRead);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !LeaveOpen)
        {
            Inner.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal sealed class CancellationInjectingStream : Stream
{
    readonly Stream Inner;
    readonly CancellationTokenSource CancellationSource;
    readonly int CancelOnReadCall;
    readonly int CancelOnWriteCall;
    readonly bool LeaveOpen;

    internal int ReadCallCount;
    internal int WriteCallCount;

    internal CancellationInjectingStream(
        Stream inner,
        CancellationTokenSource cancellationSource,
        int cancelOnReadCall,
        int cancelOnWriteCall,
        bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(cancellationSource);
        if (cancelOnReadCall == 0 || cancelOnReadCall < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(cancelOnReadCall));
        }
        if (cancelOnWriteCall == 0 || cancelOnWriteCall < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(cancelOnWriteCall));
        }

        Inner = inner;
        CancellationSource = cancellationSource;
        CancelOnReadCall = cancelOnReadCall;
        CancelOnWriteCall = cancelOnWriteCall;
        LeaveOpen = leaveOpen;
    }

    public override bool CanRead => Inner.CanRead;
    public override bool CanSeek => Inner.CanSeek;
    public override bool CanTimeout => Inner.CanTimeout;
    public override bool CanWrite => Inner.CanWrite;
    public override long Length => Inner.Length;

    public override long Position
    {
        get => Inner.Position;
        set => Inner.Position = value;
    }

    public override void Flush()
    {
        Inner.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        InjectReadCancellation(CancellationSource.Token);
        return Inner.Read(buffer, offset, count);
    }

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        InjectReadCancellation(cancellationToken);
        return Inner.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return Inner.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        Inner.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        InjectWriteCancellation(CancellationSource.Token);
        Inner.Write(buffer, offset, count);
    }

    public override Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        InjectWriteCancellation(cancellationToken);
        return Inner.WriteAsync(buffer, offset, count, cancellationToken);
    }

    void InjectReadCancellation(CancellationToken operationToken)
    {
        ReadCallCount++;
        if (ReadCallCount == CancelOnReadCall)
        {
            CancellationSource.Cancel();
        }
        operationToken.ThrowIfCancellationRequested();
    }

    void InjectWriteCancellation(CancellationToken operationToken)
    {
        WriteCallCount++;
        if (WriteCallCount == CancelOnWriteCall)
        {
            CancellationSource.Cancel();
        }
        operationToken.ThrowIfCancellationRequested();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !LeaveOpen)
        {
            Inner.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal sealed class TimeoutStream : Stream
{
    readonly bool BlockReads;
    readonly bool BlockWrites;

    internal bool IsDisposed;
    internal int ReadCallCount;
    internal int WriteCallCount;

    internal TimeoutStream(bool blockReads, bool blockWrites)
    {
        BlockReads = blockReads;
        BlockWrites = blockWrites;
    }

    public override bool CanRead => !IsDisposed;
    public override bool CanSeek => false;
    public override bool CanWrite => !IsDisposed;
    public override long Length => 0;

    public override long Position
    {
        get => 0;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Use the cancellable asynchronous read path.");
    }

    public override async Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        ReadCallCount++;
        if (BlockReads)
        {
            await Task.Delay(System.Threading.Timeout.Infinite, cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested();
        return 0;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Use the cancellable asynchronous write path.");
    }

    public override async Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        WriteCallCount++;
        if (BlockWrites)
        {
            await Task.Delay(System.Threading.Timeout.Infinite, cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
}

internal sealed class TlsBoundaryStream : Stream
{
    readonly byte[] PlaintextData;
    readonly int PlaintextBoundary;
    readonly byte[] ProtectedData;
    readonly int MaximumReadSize;
    readonly MemoryStream Written = new MemoryStream();
    int PlaintextPosition;
    int ProtectedPosition;

    internal bool IsDisposed;
    internal bool TlsActive;
    internal int ReadCallCount;

    internal TlsBoundaryStream(
        byte[] plaintextData,
        int plaintextBoundary,
        byte[] protectedData,
        int maximumReadSize = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(plaintextData);
        ArgumentNullException.ThrowIfNull(protectedData);
        if (plaintextBoundary < 0 || plaintextBoundary > plaintextData.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(plaintextBoundary));
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumReadSize);

        PlaintextData = plaintextData;
        PlaintextBoundary = plaintextBoundary;
        ProtectedData = protectedData;
        MaximumReadSize = maximumReadSize;
    }

    public override bool CanRead => !IsDisposed;
    public override bool CanSeek => false;
    public override bool CanWrite => !IsDisposed;
    public override long Length => 0;

    public override long Position
    {
        get => 0;
        set => throw new NotSupportedException();
    }

    internal int InjectedPlaintextReadCount => Math.Max(0, PlaintextPosition - PlaintextBoundary);
    internal bool PlaintextBoundaryReached => PlaintextPosition >= PlaintextBoundary;

    internal void ActivateTls()
    {
        TlsActive = true;
    }

    internal byte[] WrittenBytes()
    {
        return Written.ToArray();
    }

    internal string WrittenAscii()
    {
        return Encoding.ASCII.GetString(Written.ToArray());
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ReadCallCount++;
        return ReadCore(buffer, offset, count);
    }

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadCallCount++;
        return Task.FromResult(ReadCore(buffer, offset, count));
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Written.Write(buffer, offset, count);
    }

    int ReadCore(byte[] buffer, int offset, int count)
    {
        byte[] source = TlsActive ? ProtectedData : PlaintextData;
        int position = TlsActive ? ProtectedPosition : PlaintextPosition;
        int readCount = Math.Min(Math.Min(count, MaximumReadSize), source.Length - position);
        Array.Copy(source, position, buffer, offset, readCount);
        if (TlsActive)
        {
            ProtectedPosition += readCount;
        }
        else
        {
            PlaintextPosition += readCount;
        }
        return readCount;
    }

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        if (disposing)
        {
            Written.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal enum ScriptedStreamOperation
{
    ReturnBytes,
    ExpectWrite
}

internal sealed class ScriptedStreamEvent
{
    internal readonly ScriptedStreamOperation Operation;
    internal readonly byte[] Data;

    ScriptedStreamEvent(ScriptedStreamOperation operation, byte[] data)
    {
        Operation = operation;
        Data = data;
    }

    internal static ScriptedStreamEvent Return(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new ScriptedStreamEvent(ScriptedStreamOperation.ReturnBytes, data);
    }

    internal static ScriptedStreamEvent ReturnAscii(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Return(Encoding.ASCII.GetBytes(text));
    }

    internal static ScriptedStreamEvent Expect(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new ScriptedStreamEvent(ScriptedStreamOperation.ExpectWrite, data);
    }

    internal static ScriptedStreamEvent ExpectAscii(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Expect(Encoding.ASCII.GetBytes(text));
    }
}

internal sealed class ScriptedDuplexStream : Stream
{
    readonly ScriptedStreamEvent[] Events;
    readonly int MaximumReadSize;
    readonly MemoryStream Written = new MemoryStream();
    int EventIndex;
    int EventOffset;

    internal bool IsDisposed;
    internal int FlushCallCount;
    internal int ReadCallCount;
    internal int WriteCallCount;

    internal ScriptedDuplexStream(
        ScriptedStreamEvent[] events,
        int maximumReadSize = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumReadSize);
        Events = events;
        MaximumReadSize = maximumReadSize;
        AdvancePastEmptyEvents();
    }

    public override bool CanRead => !IsDisposed;
    public override bool CanSeek => false;
    public override bool CanWrite => !IsDisposed;
    public override long Length => 0;

    public override long Position
    {
        get => 0;
        set => throw new NotSupportedException();
    }

    internal bool IsComplete => EventIndex == Events.Length;
    internal int RemainingEventCount => Events.Length - EventIndex;

    internal byte[] WrittenBytes()
    {
        return Written.ToArray();
    }

    internal string WrittenAscii()
    {
        return Encoding.ASCII.GetString(Written.ToArray());
    }

    public override void Flush()
    {
        FlushCallCount++;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ReadCallCount++;
        if (EventIndex == Events.Length)
        {
            return 0;
        }
        ScriptedStreamEvent current = Events[EventIndex];
        if (current.Operation != ScriptedStreamOperation.ReturnBytes)
        {
            throw new InvalidOperationException(
                $"Unexpected read at script event {EventIndex}; a write was expected.");
        }

        int remaining = current.Data.Length - EventOffset;
        int readCount = Math.Min(Math.Min(count, MaximumReadSize), remaining);
        Array.Copy(current.Data, EventOffset, buffer, offset, readCount);
        EventOffset += readCount;
        AdvanceCompletedEvent();
        return readCount;
    }

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Read(buffer, offset, count));
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        WriteCallCount++;
        int sourceOffset = offset;
        int remaining = count;
        while (remaining > 0)
        {
            if (EventIndex == Events.Length)
            {
                throw new InvalidOperationException("Unexpected write after the script completed.");
            }
            ScriptedStreamEvent current = Events[EventIndex];
            if (current.Operation != ScriptedStreamOperation.ExpectWrite)
            {
                throw new InvalidOperationException(
                    $"Unexpected write at script event {EventIndex}; a read was expected.");
            }

            int expectedRemaining = current.Data.Length - EventOffset;
            int compareCount = Math.Min(remaining, expectedRemaining);
            for (int index = 0; index < compareCount; index++)
            {
                byte actual = buffer[sourceOffset + index];
                byte expected = current.Data[EventOffset + index];
                if (actual != expected)
                {
                    throw new InvalidOperationException(
                        $"Unexpected byte at script event {EventIndex}, offset {EventOffset + index}: " +
                        $"expected 0x{expected:X2}, received 0x{actual:X2}.");
                }
            }

            Written.Write(buffer, sourceOffset, compareCount);
            sourceOffset += compareCount;
            remaining -= compareCount;
            EventOffset += compareCount;
            AdvanceCompletedEvent();
        }
    }

    public override Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Write(buffer, offset, count);
        return Task.CompletedTask;
    }

    void AdvanceCompletedEvent()
    {
        if (EventOffset == Events[EventIndex].Data.Length)
        {
            EventIndex++;
            EventOffset = 0;
            AdvancePastEmptyEvents();
        }
    }

    void AdvancePastEmptyEvents()
    {
        while (EventIndex < Events.Length && Events[EventIndex].Data.Length == 0)
        {
            EventIndex++;
        }
    }

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        if (disposing)
        {
            Written.Dispose();
        }
        base.Dispose(disposing);
    }
}
