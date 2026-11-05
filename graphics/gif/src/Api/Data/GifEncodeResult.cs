namespace gif;

/// <summary>Contains encoded bytes, the normalized document that produced them, and any fatal diagnostic.</summary>
public class GifEncodeResult
{
    /// <summary>Indicates whether encoding completed without a fatal error.</summary>
    public bool Success;
    /// <summary>Contains the complete GIF file bytes when <see cref="Success"/> is <c>true</c>.</summary>
    public byte[] Data;
    /// <summary>Contains the validated document created or used by the encoder.</summary>
    public GifDocument Document;
    /// <summary>Contains the failure diagnostic when <see cref="Success"/> is <c>false</c>.</summary>
    public GifErrorDiagnostic Error;

    /// <summary>Creates an unsuccessful result with empty data and a no-error diagnostic.</summary>
    public GifEncodeResult()
    {
        Success = false;
        Data = new byte[0];
        Document = new GifDocument();
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}
