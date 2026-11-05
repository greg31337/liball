namespace gif;

/// <summary>Represents a fatal GIF processing error with source, frame, and block context.</summary>
public class GifException : System.Exception
{
    /// <summary>Contains the machine-readable failure code.</summary>
    public GifErrorCode ErrorCode;
    /// <summary>Contains the zero-based source byte offset, or <c>-1</c> when unknown.</summary>
    public long ByteOffset;
    /// <summary>Contains the zero-based frame index, or <c>-1</c> when not applicable.</summary>
    public int FrameIndex;
    /// <summary>Contains the block or operation name associated with the failure.</summary>
    public string BlockName;

    /// <summary>Creates an exception with source, frame, and block context.</summary>
    /// <param name="errorCode">The machine-readable error code.</param>
    /// <param name="message">The human-readable explanation; <c>null</c> is normalized to an empty string.</param>
    /// <param name="byteOffset">The zero-based source offset, or <c>-1</c> when unknown.</param>
    /// <param name="frameIndex">The zero-based frame index, or <c>-1</c> when not applicable.</param>
    /// <param name="blockName">The block or operation name; <c>null</c> is normalized to an empty string.</param>
    public GifException(GifErrorCode errorCode, string? message, long byteOffset, int frameIndex, string? blockName)
        : base(message == null ? string.Empty : message)
    {
        ErrorCode = errorCode;
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

    internal static GifException FromDiagnostic(GifErrorDiagnostic? diagnostic)
    {
        if (diagnostic == null)
        {
            return new GifException(GifErrorCode.None, null, -1, -1, null);
        }

        return new GifException(
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.Position.ByteOffset,
            diagnostic.Position.FrameIndex,
            diagnostic.Position.BlockName);
    }

    /// <summary>Determines whether a source byte offset is available.</summary>
    /// <returns><c>true</c> when <see cref="ByteOffset"/> is non-negative.</returns>
    public bool HasByteOffset()
    {
        return ByteOffset >= 0;
    }

    /// <summary>Determines whether a frame index is available.</summary>
    /// <returns><c>true</c> when <see cref="FrameIndex"/> is non-negative.</returns>
    public bool HasFrameIndex()
    {
        return FrameIndex >= 0;
    }

    /// <summary>Determines whether a block or operation name is available.</summary>
    /// <returns><c>true</c> when <see cref="BlockName"/> is non-empty.</returns>
    public bool HasBlockName()
    {
        return BlockName.Length > 0;
    }

    /// <summary>Creates a structured position from the flattened location fields.</summary>
    /// <returns>A new diagnostic position.</returns>
    public GifDiagnosticPosition CreatePosition()
    {
        return new GifDiagnosticPosition(ByteOffset, FrameIndex, BlockName);
    }

    /// <summary>Creates a structured error diagnostic with the same code, message, and location.</summary>
    /// <returns>A new error diagnostic.</returns>
    public GifErrorDiagnostic ToDiagnostic()
    {
        return new GifErrorDiagnostic(ErrorCode, Message, CreatePosition());
    }

    /// <summary>Formats the message with any available block, frame, and byte context.</summary>
    /// <returns>A human-readable diagnostic string.</returns>
    public string FormatMessage()
    {
        string result = Message;
        if (result.Length == 0)
        {
            result = ErrorCode.ToString();
        }

        if (HasBlockName())
        {
            result = result + " in " + BlockName;
        }

        if (HasFrameIndex())
        {
            result = result + " at frame " + FrameIndex.ToString();
        }

        if (HasByteOffset())
        {
            result = result + " at byte " + ByteOffset.ToString();
        }

        return result;
    }
}
