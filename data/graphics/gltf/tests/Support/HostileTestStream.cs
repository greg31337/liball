namespace gltf.Tests.Support;

internal sealed class HostileTestStream : Stream
{
    private readonly MemoryStream storage;
    private readonly bool readable;
    private readonly bool writable;
    private readonly bool seekable;
    private readonly int maximumReadSize;
    private readonly int maximumWriteSize;

    internal bool Disposed;
    internal bool SeekAttempted;
    internal int ReadByteLength;
    internal int WrittenByteLength;
    internal int FailReadsAfterByteLength = -1;
    internal int FailWritesAfterByteLength = -1;

    internal HostileTestStream(
        byte[] bytes,
        bool readable,
        bool writable,
        bool seekable,
        int maximumReadSize,
        int maximumWriteSize)
    {
        storage = new MemoryStream();
        storage.Write(bytes, 0, bytes.Length);
        storage.Position = 0;
        this.readable = readable;
        this.writable = writable;
        this.seekable = seekable;
        this.maximumReadSize = maximumReadSize;
        this.maximumWriteSize = maximumWriteSize;
    }

    public override bool CanRead => readable && !Disposed;

    public override bool CanSeek => seekable && !Disposed;

    public override bool CanWrite => writable && !Disposed;

    public override long Length
    {
        get
        {
            RequireSeek();
            return storage.Length;
        }
    }

    public override long Position
    {
        get
        {
            RequireSeek();
            return storage.Position;
        }

        set
        {
            RequireSeek();
            storage.Position = value;
        }
    }

    internal byte[] GetBytes()
    {
        return storage.ToArray();
    }

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }

    public override void Flush()
    {
        storage.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (!CanRead)
        {
            throw new NotSupportedException("The stream is not readable.");
        }

        if (FailReadsAfterByteLength >= 0
            && ReadByteLength >= FailReadsAfterByteLength)
        {
            throw new IOException("Configured hostile read failure.");
        }

        int boundedCount = Math.Min(count, maximumReadSize);
        if (FailReadsAfterByteLength >= 0)
        {
            boundedCount = Math.Min(
                boundedCount,
                FailReadsAfterByteLength - ReadByteLength);
        }

        int result = storage.Read(buffer, offset, boundedCount);
        ReadByteLength += result;
        return result;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        RequireSeek();
        return storage.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        RequireSeek();
        storage.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (!CanWrite)
        {
            throw new NotSupportedException("The stream is not writable.");
        }

        if (FailWritesAfterByteLength >= 0
            && WrittenByteLength >= FailWritesAfterByteLength)
        {
            throw new IOException("Configured hostile write failure.");
        }

        int boundedCount = Math.Min(count, maximumWriteSize);
        if (FailWritesAfterByteLength >= 0)
        {
            boundedCount = Math.Min(
                boundedCount,
                FailWritesAfterByteLength - WrittenByteLength);
        }

        storage.Write(buffer, offset, boundedCount);
        WrittenByteLength += boundedCount;
        if (boundedCount != count)
        {
            throw new IOException("Configured hostile partial write failure.");
        }
    }

    private void RequireSeek()
    {
        SeekAttempted = true;
        if (!CanSeek)
        {
            throw new NotSupportedException("The stream is not seekable.");
        }
    }
}
