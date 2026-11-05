namespace gif;

/// <summary>Identifies where a diagnostic occurred in a GIF stream or processing operation.</summary>
public class GifDiagnosticPosition
{
    /// <summary>Contains the zero-based source byte offset, or <c>-1</c> when unknown.</summary>
    public long ByteOffset;
    /// <summary>Contains the zero-based frame index, or <c>-1</c> when not applicable.</summary>
    public int FrameIndex;
    /// <summary>Contains the block or operation name.</summary>
    public string BlockName;

    /// <summary>Creates a diagnostic position with stream, frame, and block context.</summary>
    /// <param name="byteOffset">The zero-based source offset, or <c>-1</c> when unknown.</param>
    /// <param name="frameIndex">The zero-based frame index, or <c>-1</c> when not applicable.</param>
    /// <param name="blockName">The block or operation name; <c>null</c> is normalized to an empty string.</param>
    public GifDiagnosticPosition(long byteOffset, int frameIndex, string? blockName)
    {
        ByteOffset = byteOffset;
        FrameIndex = frameIndex;
        if (blockName == null)
        {
            BlockName = string.Empty;
        }
        else
        {
            BlockName = blockName;
        }
    }

    internal bool HasByteOffset()
    {
        return ByteOffset >= 0;
    }

    internal bool HasFrameIndex()
    {
        return FrameIndex >= 0;
    }

    internal bool HasBlockName()
    {
        return BlockName.Length > 0;
    }
}

/// <summary>Describes a fatal validation, decoding, or encoding failure without throwing.</summary>
public class GifErrorDiagnostic
{
    /// <summary>Contains the machine-readable failure code; <see cref="GifErrorCode.None"/> means success.</summary>
    public GifErrorCode Code;
    /// <summary>Contains a plain-English explanation of the failure.</summary>
    public string Message;
    /// <summary>Contains the source, frame, and block context for the failure.</summary>
    public GifDiagnosticPosition Position;

    /// <summary>Creates an error diagnostic.</summary>
    /// <param name="code">The machine-readable failure code.</param>
    /// <param name="message">The human-readable explanation; <c>null</c> is normalized to an empty string.</param>
    /// <param name="position">The source, frame, and block context; <c>null</c> is normalized to an unknown position.</param>
    public GifErrorDiagnostic(GifErrorCode code, string? message, GifDiagnosticPosition? position)
    {
        Code = code;
        if (message == null)
        {
            Message = string.Empty;
        }
        else
        {
            Message = message;
        }

        if (position == null)
        {
            Position = new GifDiagnosticPosition(-1, -1, null);
        }
        else
        {
            Position = position;
        }
    }
}

/// <summary>Describes a recoverable condition encountered while processing a GIF.</summary>
public class GifWarningDiagnostic
{
    /// <summary>Contains the machine-readable warning code; <see cref="GifWarningCode.None"/> means no warning.</summary>
    public GifWarningCode Code;
    /// <summary>Contains a plain-English explanation of the condition.</summary>
    public string Message;
    /// <summary>Contains the source, frame, and block context for the condition.</summary>
    public GifDiagnosticPosition Position;

    /// <summary>Creates a warning diagnostic.</summary>
    /// <param name="code">The machine-readable warning code.</param>
    /// <param name="message">The human-readable explanation; <c>null</c> is normalized to an empty string.</param>
    /// <param name="position">The source, frame, and block context; <c>null</c> is normalized to an unknown position.</param>
    public GifWarningDiagnostic(GifWarningCode code, string? message, GifDiagnosticPosition? position)
    {
        Code = code;
        if (message == null)
        {
            Message = string.Empty;
        }
        else
        {
            Message = message;
        }

        if (position == null)
        {
            Position = new GifDiagnosticPosition(-1, -1, null);
        }
        else
        {
            Position = position;
        }
    }
}
