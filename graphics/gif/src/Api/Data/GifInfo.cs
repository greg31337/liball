namespace gif;

/// <summary>Contains metadata collected without retaining or rendering image payloads.</summary>
public class GifInfo
{
    /// <summary>Indicates whether identification completed without a fatal error.</summary>
    public bool Success;
    /// <summary>Contains the detected GIF version.</summary>
    public GifVersion Version;
    /// <summary>Contains the logical-screen width in pixels.</summary>
    public int Width;
    /// <summary>Contains the logical-screen height in pixels.</summary>
    public int Height;
    /// <summary>Contains the number of image frames found.</summary>
    public int FrameCount;
    /// <summary>Indicates whether the document contains more than one frame.</summary>
    public bool HasAnimation;
    /// <summary>Indicates whether any frame declares a transparent palette index.</summary>
    public bool HasTransparency;
    /// <summary>Contains the repeat count: <c>-1</c> means absent, <c>0</c> means forever, and positive values are finite repeats.</summary>
    public int LoopCount;
    /// <summary>Indicates whether at least one comment extension was found.</summary>
    public bool HasComments;
    /// <summary>Contains the identifiers of application extensions found in the stream.</summary>
    public string[] ApplicationExtensionIdentifiers;
    /// <summary>Contains an estimate of the bytes needed to hold all frames as full RGBA canvases.</summary>
    public long EstimatedDecodedBytes;
    /// <summary>Contains recoverable conditions observed during identification.</summary>
    public GifWarningDiagnostic[] Warnings;
    /// <summary>Contains the fatal diagnostic when <see cref="Success"/> is <c>false</c>.</summary>
    public GifErrorDiagnostic Error;

    /// <summary>Creates an unsuccessful, empty identification result.</summary>
    public GifInfo()
    {
        Success = false;
        Version = GifVersion.Unknown;
        Width = 0;
        Height = 0;
        FrameCount = 0;
        HasAnimation = false;
        HasTransparency = false;
        LoopCount = -1;
        HasComments = false;
        ApplicationExtensionIdentifiers = new string[0];
        EstimatedDecodedBytes = 0;
        Warnings = new GifWarningDiagnostic[0];
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}
