namespace gif;

/// <summary>Represents the Graphic Control Extension that supplies timing and rendering behavior for the next graphic block.</summary>
public class GifGraphicControl
{
    /// <summary>Controls how the rendered graphic is removed.</summary>
    public GifDisposalMethod DisposalMethod;
    /// <summary>Preserves the GIF user-input flag.</summary>
    public bool UserInputFlag;
    /// <summary>Indicates whether <see cref="TransparentColorIndex"/> is active.</summary>
    public bool HasTransparency;
    /// <summary>Contains the display delay in hundredths of a second.</summary>
    public int DelayHundredths;
    /// <summary>Contains the transparent palette index, or <c>-1</c> when transparency is inactive.</summary>
    public int TransparentColorIndex;

    /// <summary>Creates a control block with no delay, transparency, user input, or specified disposal.</summary>
    public GifGraphicControl()
    {
        DisposalMethod = GifDisposalMethod.Unspecified;
        UserInputFlag = false;
        HasTransparency = false;
        DelayHundredths = 0;
        TransparentColorIndex = -1;
    }
}

/// <summary>Represents a GIF comment as both its raw bytes and its decoded text.</summary>
/// <remarks>GIF comments are byte-oriented. Text conversion uses the library's GIF text encoding behavior; retain <see cref="Data"/> for exact payload access.</remarks>
public class GifCommentExtension
{
    /// <summary>Contains the concatenated comment sub-block payload bytes.</summary>
    public byte[] Data;
    /// <summary>Contains the comment interpreted as text.</summary>
    public string Text;

    /// <summary>Creates a comment and derives its byte payload from the supplied text.</summary>
    /// <param name="text">The comment text; <c>null</c> is normalized to an empty string.</param>
    public GifCommentExtension(string? text)
    {
        Data = new byte[0];
        if (text == null)
        {
            Text = string.Empty;
        }
        else
        {
            Text = text;
        }
    }
}

/// <summary>Represents a GIF application extension and its application-defined payload.</summary>
public class GifApplicationExtension
{
    /// <summary>Contains the eight-byte application identifier interpreted as text.</summary>
    public string Identifier;
    /// <summary>Contains the three-byte authentication code interpreted as text.</summary>
    public string AuthenticationCode;
    /// <summary>Contains the concatenated application-data sub-block payload.</summary>
    public byte[] Data;
    /// <summary>Indicates whether the extension is a recognized Netscape or ANIMEXTS loop block.</summary>
    public bool IsLoopExtension;
    /// <summary>Contains the recognized repeat count: <c>0</c> means forever.</summary>
    public int LoopCount;

    /// <summary>Creates an empty application extension.</summary>
    public GifApplicationExtension()
    {
        Identifier = string.Empty;
        AuthenticationCode = string.Empty;
        Data = new byte[0];
        IsLoopExtension = false;
        LoopCount = -1;
    }
}

/// <summary>Represents a GIF89a Plain Text Extension and its optional graphic control.</summary>
public class GifPlainTextExtension
{
    /// <summary>Contains the text-grid horizontal offset in pixels.</summary>
    public int TextGridLeft;
    /// <summary>Contains the text-grid vertical offset in pixels.</summary>
    public int TextGridTop;
    /// <summary>Contains the text-grid width in pixels.</summary>
    public int TextGridWidth;
    /// <summary>Contains the text-grid height in pixels.</summary>
    public int TextGridHeight;
    /// <summary>Contains the character-cell width in pixels.</summary>
    public byte CharacterCellWidth;
    /// <summary>Contains the character-cell height in pixels.</summary>
    public byte CharacterCellHeight;
    /// <summary>Contains the global palette index used for text.</summary>
    public byte ForegroundColorIndex;
    /// <summary>Contains the global palette index used behind text.</summary>
    public byte BackgroundColorIndex;
    /// <summary>Contains the concatenated text-data sub-block payload.</summary>
    public byte[] TextData;
    /// <summary>Contains the payload interpreted as text.</summary>
    public string Text;
    /// <summary>Contains the Graphic Control Extension scoped to this rendering block.</summary>
    public GifGraphicControl GraphicControl;

    /// <summary>Creates an empty plain-text extension.</summary>
    public GifPlainTextExtension()
    {
        TextGridLeft = 0;
        TextGridTop = 0;
        TextGridWidth = 0;
        TextGridHeight = 0;
        CharacterCellWidth = 0;
        CharacterCellHeight = 0;
        ForegroundColorIndex = 0;
        BackgroundColorIndex = 0;
        TextData = new byte[0];
        Text = string.Empty;
        GraphicControl = new GifGraphicControl();
    }
}

/// <summary>Preserves an extension label and payload that the library does not otherwise interpret.</summary>
public class GifUnknownExtension
{
    /// <summary>Contains the byte following the GIF extension introducer.</summary>
    public byte Label;
    /// <summary>Contains the concatenated extension-data sub-block payload.</summary>
    public byte[] Data;
    /// <summary>Contains a diagnostic-friendly name for the block.</summary>
    public string BlockName;

    /// <summary>Creates an unknown extension from its label, payload, and descriptive name.</summary>
    /// <param name="label">The extension label byte.</param>
    /// <param name="data">The concatenated sub-block payload; <c>null</c> is normalized to an empty array.</param>
    /// <param name="blockName">A descriptive block name used in diagnostics; <c>null</c> is normalized to an empty string.</param>
    public GifUnknownExtension(byte label, byte[]? data, string? blockName)
    {
        Label = label;
        if (data == null)
        {
            Data = new byte[0];
        }
        else
        {
            Data = data;
        }

        if (blockName == null)
        {
            BlockName = string.Empty;
        }
        else
        {
            BlockName = blockName;
        }
    }
}

/// <summary>Groups document-level extensions and normalized loop metadata.</summary>
/// <remarks><see cref="GifDocument"/> also exposes these arrays directly for convenience. Decoding keeps the grouped and direct views synchronized.</remarks>
public class GifMetadata
{
    /// <summary>Contains comment extensions in stream order.</summary>
    public GifCommentExtension[] Comments;
    /// <summary>Contains application extensions in stream order.</summary>
    public GifApplicationExtension[] ApplicationExtensions;
    /// <summary>Contains plain-text extensions in stream order.</summary>
    public GifPlainTextExtension[] PlainTextExtensions;
    /// <summary>Contains preserved unrecognized extensions in stream order.</summary>
    public GifUnknownExtension[] UnknownExtensions;
    /// <summary>Contains the normalized repeat count: <c>-1</c> means absent and <c>0</c> means forever.</summary>
    public int LoopCount;

    /// <summary>Creates empty metadata with no loop extension.</summary>
    public GifMetadata()
    {
        Comments = new GifCommentExtension[0];
        ApplicationExtensions = new GifApplicationExtension[0];
        PlainTextExtensions = new GifPlainTextExtension[0];
        UnknownExtensions = new GifUnknownExtension[0];
        LoopCount = -1;
    }
}

/// <summary>Contains general byte, frame, error, and warning counters for application-level aggregation.</summary>
public class GifStatistics
{
    /// <summary>Contains the number of source bytes consumed.</summary>
    public long BytesRead;
    /// <summary>Contains the number of encoded bytes produced.</summary>
    public long BytesWritten;
    /// <summary>Contains the number of frames decoded.</summary>
    public int FramesRead;
    /// <summary>Contains the number of frames encoded.</summary>
    public int FramesWritten;
    /// <summary>Contains the number of fatal diagnostics counted by the caller.</summary>
    public int ErrorCount;
    /// <summary>Contains the number of warning diagnostics counted by the caller.</summary>
    public int WarningCount;

    /// <summary>Creates a statistics object with every counter set to zero.</summary>
    public GifStatistics()
    {
        BytesRead = 0;
        BytesWritten = 0;
        FramesRead = 0;
        FramesWritten = 0;
        ErrorCount = 0;
        WarningCount = 0;
    }
}

/// <summary>Contains workload counters collected during structural decoding.</summary>
public class GifDecodeStatistics
{
    /// <summary>Contains the number of source bytes consumed.</summary>
    public long BytesRead;
    /// <summary>Contains the number of compressed image payload bytes retained.</summary>
    public long CompressedBytesRead;
    /// <summary>Contains the aggregate number of frame-rectangle pixels accounted for.</summary>
    public long DecodedPixelCount;
    /// <summary>Contains the number of image frames read.</summary>
    public int FramesRead;
    /// <summary>Contains the number of extension blocks read.</summary>
    public int ExtensionCount;
    /// <summary>Contains the number of warning diagnostics produced.</summary>
    public int WarningCount;

    /// <summary>Creates decode statistics with every counter set to zero.</summary>
    public GifDecodeStatistics()
    {
        BytesRead = 0;
        CompressedBytesRead = 0;
        DecodedPixelCount = 0;
        FramesRead = 0;
        ExtensionCount = 0;
        WarningCount = 0;
    }
}

/// <summary>Contains workload counters collected during encoding.</summary>
public class GifEncodeStatistics
{
    /// <summary>Contains the number of encoded bytes produced.</summary>
    public long BytesWritten;
    /// <summary>Contains the number of compressed image payload bytes produced.</summary>
    public long CompressedBytesWritten;
    /// <summary>Contains the aggregate number of indexed pixels encoded.</summary>
    public long IndexedPixelCount;
    /// <summary>Contains the number of image frames written.</summary>
    public int FramesWritten;
    /// <summary>Contains the number of extension blocks written.</summary>
    public int ExtensionCount;
    /// <summary>Contains the number of warning diagnostics associated with encoding.</summary>
    public int WarningCount;

    /// <summary>Creates encode statistics with every counter set to zero.</summary>
    public GifEncodeStatistics()
    {
        BytesWritten = 0;
        CompressedBytesWritten = 0;
        IndexedPixelCount = 0;
        FramesWritten = 0;
        ExtensionCount = 0;
        WarningCount = 0;
    }
}
