namespace svg;

/// <summary>Contains UTF-8 or SVGZ bytes and the underlying XML write result.</summary>
public class SvgWriteBytesResult
{
    /// <summary>Whether XML serialization and byte encoding succeeded.</summary>
    public bool Success;
    /// <summary>The underlying XML serialization result.</summary>
    public SvgWriteResult WriteResult;
    /// <summary>The complete encoded bytes, or an empty array after failure.</summary>
    public byte[] Bytes;
    /// <summary>The encoded byte count.</summary>
    public int ByteCount;
    /// <summary>Whether the bytes use an SVGZ gzip container.</summary>
    public bool Compressed;

    internal SvgWriteBytesResult(SvgWriteResult writeResult, byte[] bytes, bool compressed)
    {
        WriteResult = writeResult;
        Bytes = bytes;
        ByteCount = bytes.Length;
        Compressed = compressed;
        Success = writeResult != null && writeResult.Success;
    }
}

/// <summary>Reports an atomic write attempt into a caller-provided span.</summary>
public class SvgWriteBufferResult
{
    /// <summary>Whether the complete serialized value fit and was copied.</summary>
    public bool Success;
    /// <summary>The stable serialization or capacity failure.</summary>
    public SvgWriteError Error;
    /// <summary>The stable failure message.</summary>
    public string Message;
    /// <summary>The underlying XML serialization result.</summary>
    public SvgWriteResult WriteResult;
    /// <summary>The span element unit.</summary>
    public SvgWriteBufferKind Kind;
    /// <summary>The required span length.</summary>
    public int RequiredLength;
    /// <summary>The copied span length, zero after failure.</summary>
    public int WrittenLength;

    internal SvgWriteBufferResult(SvgWriteResult writeResult, SvgWriteBufferKind kind, int requiredLength, int writtenLength, SvgWriteError error, string message)
    {
        WriteResult = writeResult;
        Kind = kind;
        RequiredLength = requiredLength;
        WrittenLength = writtenLength;
        Error = error;
        Message = message;
        Success = error == SvgWriteError.None;
    }
}

/// <summary>Contains a serialized SVG data URL and its underlying XML write result.</summary>
public class SvgWriteDataUrlResult
{
    /// <summary>Whether serialization and data URL encoding succeeded.</summary>
    public bool Success;
    /// <summary>The stable write failure.</summary>
    public SvgWriteError Error;
    /// <summary>The stable failure message.</summary>
    public string Message;
    /// <summary>The underlying XML serialization result.</summary>
    public SvgWriteResult WriteResult;
    /// <summary>The complete data URL, or an empty string after failure.</summary>
    public string DataUrl;
    /// <summary>The selected payload encoding.</summary>
    public SvgDataUrlWriteEncoding Encoding;
    /// <summary>The decoded UTF-8 payload byte count.</summary>
    public int ByteCount;

    internal SvgWriteDataUrlResult(SvgWriteResult writeResult, string dataUrl, SvgDataUrlWriteEncoding encoding, int byteCount, SvgWriteError error, string message)
    {
        WriteResult = writeResult;
        DataUrl = dataUrl;
        Encoding = encoding;
        ByteCount = byteCount;
        Error = error;
        Message = message;
        Success = error == SvgWriteError.None;
    }
}
