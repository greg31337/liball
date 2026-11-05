using System;

namespace avif;

/// <summary>Writes nested ISO boxes into bounded private storage.</summary>
public class AvifBoxWriter : IAvifBoxWriter
{
    IsoBoxWriter? writer;
    byte[]? finishedData;
    AvifResultCode code;
    bool finished;
    bool closed;

    /// <summary>Creates a writer using the default source-byte limit.</summary>
    /// <returns>The initialized box writer.</returns>
    internal static AvifBoxWriter Create()
    {
        return new AvifBoxWriter(AvifSecurityLimits.DefaultMaxSourceBytes);
    }

    /// <summary>Creates a writer with an explicit maximum output size.</summary>
    /// <param name="maximumOutputBytes">The positive maximum output size.</param>
    public AvifBoxWriter(long maximumOutputBytes)
    {
        Initialize(maximumOutputBytes);
    }

    /// <summary>Creates a writer with an explicit maximum output size.</summary>
    /// <returns>The initialized box writer.</returns>
    internal static AvifBoxWriter Create(long maximumOutputBytes)
    {
        return new AvifBoxWriter(maximumOutputBytes);
    }

    /// <summary>Returns the outcome of the most recent writer operation.</summary>
    /// <returns>The stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return closed ? AvifResultCode.Closed : code;
    }

    /// <summary>Begins a box with a four-character type.</summary>
    /// <param name="type">The four-character box type.</param>
    /// <returns>The operation result.</returns>
    public AvifResultCode BeginBox(string type)
    {
        AvifResultCode state = CheckBuildingState();
        if (state == AvifResultCode.Success)
        {
            code = (writer ?? throw new InvalidOperationException("Invalid internal state.")).BeginBox(type);
        }
        return code;
    }

    /// <summary>Begins a FullBox with version and 24-bit flags.</summary>
    /// <param name="type">The four-character box type.</param>
    /// <param name="version">The version from zero through 255.</param>
    /// <param name="flags">The flags from zero through 16777215.</param>
    /// <returns>The operation result.</returns>
    public AvifResultCode BeginFullBox(string type, int version, int flags)
    {
        AvifResultCode state = CheckBuildingState();
        if (state == AvifResultCode.Success)
        {
            code = (writer ?? throw new InvalidOperationException("Invalid internal state.")).BeginFullBox(type, version, flags);
        }
        return code;
    }

    /// <summary>Writes bytes into the current box.</summary>
    /// <param name="source">The bytes to copy.</param>
    /// <returns>The operation result.</returns>
    public AvifResultCode Write(ReadOnlySpan<byte> source)
    {
        AvifResultCode state = CheckBuildingState();
        if (state == AvifResultCode.Success)
        {
            code = (writer ?? throw new InvalidOperationException("Invalid internal state.")).Write(source);
        }
        return code;
    }

    /// <summary>Ends the innermost open box and patches its declared size.</summary>
    /// <returns>The operation result.</returns>
    public AvifResultCode EndBox()
    {
        AvifResultCode state = CheckBuildingState();
        if (state == AvifResultCode.Success)
        {
            code = (writer ?? throw new InvalidOperationException("Invalid internal state.")).EndBox();
        }
        return code;
    }

    /// <summary>Finalizes the box stream and returns an independent byte snapshot.</summary>
    /// <returns>The encoded result.</returns>
    public AvifEncodeResult Finish()
    {
        if (closed)
        {
            return new AvifEncodeResult(
                AvifResultCode.Closed,
                System.Array.Empty<byte>());
        }
        if (finished)
        {
            return new AvifEncodeResult(AvifResultCode.Success, finishedData);
        }
        if (code != AvifResultCode.Success)
        {
            return new AvifEncodeResult(code, System.Array.Empty<byte>());
        }

        IsoBoxWriteResult result = (writer ?? throw new InvalidOperationException("Invalid internal state.")).Finish();
        code = result.Code;
        if (code == AvifResultCode.Success)
        {
            finishedData = (byte[])result.Data.Clone();
            finished = true;
        }
        return new AvifEncodeResult(code, result.Data);
    }

    /// <summary>Releases retained output and construction state.</summary>
    /// <returns><c>Success</c>; repeated calls are harmless.</returns>
    public AvifResultCode Close()
    {
        if (!closed)
        {
            (writer ?? throw new InvalidOperationException("Invalid internal state.")).Close();
            finishedData = System.Array.Empty<byte>();
            finished = false;
            closed = true;
            code = AvifResultCode.Closed;
        }
        return AvifResultCode.Success;
    }

    void Initialize(long maximumOutputBytes)
    {
        int initialCapacity = maximumOutputBytes > 0 && maximumOutputBytes < 256
            ? (int)maximumOutputBytes : 256;
        writer = new IsoBoxWriter(initialCapacity, maximumOutputBytes);
        finishedData = System.Array.Empty<byte>();
        code = maximumOutputBytes > 0
            ? writer.Code : AvifResultCode.InvalidArgument;
        finished = false;
        closed = false;
    }

    AvifResultCode CheckBuildingState()
    {
        if (closed)
        {
            code = AvifResultCode.Closed;
        }
        else if (finished)
        {
            code = AvifResultCode.InvalidState;
        }
        return code;
    }
}
