namespace gif;

/// <summary>References one block in the ordered content region of a raw GIF document.</summary>
/// <remarks><see cref="Index"/> selects an item from the collection identified by <see cref="Kind"/>. Trailer entries use <c>-1</c>. Scope data identifies graphic-control targets and extensions in the trailer-adjacent region.</remarks>
public class GifRawBlock
{
    /// <summary>Identifies the referenced block type.</summary>
    public GifRawBlockKind Kind;
    /// <summary>Contains the zero-based index in the corresponding document collection, or <c>-1</c> for the trailer.</summary>
    public int Index;
    /// <summary>Identifies the block's document, frame, plain-text, or trailer-adjacent scope.</summary>
    public GifRawBlockScope Scope;
    /// <summary>Contains the frame or plain-text index for scoped blocks, or <c>-1</c> when no indexed target applies.</summary>
    public int ScopeIndex;

    /// <summary>Creates a document-scoped entry with no referenced collection item.</summary>
    public GifRawBlock()
    {
        Kind = GifRawBlockKind.GraphicControl;
        Index = -1;
        Scope = GifRawBlockScope.Document;
        ScopeIndex = -1;
    }
}

/// <summary>Represents a decoded or ready-to-encode GIF document in its raw block-oriented form.</summary>
/// <remarks>Frames retain indexed pixels, compressed payloads, rectangles, and local palettes. Use <see cref="Gif.Coalesce(GifDocument)"/> when full-screen RGBA frames are needed.</remarks>
public class GifDocument
{
    /// <summary>Identifies the decoded version or the version selected for encoding.</summary>
    public GifVersion Version;
    /// <summary>Contains logical-screen dimensions and global display settings.</summary>
    public GifLogicalScreen LogicalScreen;
    /// <summary>Contains the palette shared by frames that do not provide a local color table.</summary>
    public GifPalette GlobalColorTable;
    /// <summary>Contains raw image frames in stream order.</summary>
    public GifFrame[] Frames;
    /// <summary>Contains every decoded Graphic Control Extension in stream order, including controls that are not consumed by an image.</summary>
    public GifGraphicControl[] GraphicControls;
    /// <summary>Contains ordered references to extensions, images, and the trailer after the logical screen and global color table.</summary>
    /// <remarks>When nonempty, encoding follows this sequence. Empty order data uses the grouped metadata and frame collections for compatibility with newly constructed documents.</remarks>
    public GifRawBlock[] BlockOrder;
    /// <summary>Contains a grouped view of document-level extension metadata.</summary>
    public GifMetadata Metadata;
    /// <summary>Contains document comment extensions in stream order.</summary>
    public GifCommentExtension[] Comments;
    /// <summary>Contains application extensions, including recognized loop extensions.</summary>
    public GifApplicationExtension[] ApplicationExtensions;
    /// <summary>Contains decoded plain-text extensions.</summary>
    public GifPlainTextExtension[] PlainTextExtensions;
    /// <summary>Contains structured plain-text rendering blocks when decoding uses <see cref="GifPlainTextExtensionPolicy.Expose"/>; the library does not rasterize them.</summary>
    public GifPlainTextExtension[] PlainTextGraphics;
    /// <summary>Contains preserved extension labels and payloads not otherwise recognized.</summary>
    public GifUnknownExtension[] UnknownExtensions;
    /// <summary>Contains the repeat count: <c>-1</c> means no loop extension, <c>0</c> means forever, and positive values are finite repeats.</summary>
    public int LoopCount;
    /// <summary>Contains non-fatal conditions encountered while decoding or transforming the document.</summary>
    public GifWarningDiagnostic[] Warnings;
    /// <summary>Contains general counters available for application-level aggregation.</summary>
    public GifStatistics Statistics;
    /// <summary>Contains byte, frame, extension, and pixel counts from decoding.</summary>
    public GifDecodeStatistics DecodeStatistics;
    /// <summary>Contains byte, frame, extension, and pixel counts from the latest encoding operation.</summary>
    public GifEncodeStatistics EncodeStatistics;

    /// <summary>Creates an empty document with initialized arrays, metadata, diagnostics, and statistics.</summary>
    public GifDocument()
    {
        Version = GifVersion.Unknown;
        LogicalScreen = new GifLogicalScreen(0, 0);
        GlobalColorTable = new GifPalette(null);
        Frames = new GifFrame[0];
        GraphicControls = new GifGraphicControl[0];
        BlockOrder = new GifRawBlock[0];
        Metadata = new GifMetadata();
        Comments = new GifCommentExtension[0];
        ApplicationExtensions = new GifApplicationExtension[0];
        PlainTextExtensions = new GifPlainTextExtension[0];
        PlainTextGraphics = new GifPlainTextExtension[0];
        UnknownExtensions = new GifUnknownExtension[0];
        LoopCount = -1;
        Warnings = new GifWarningDiagnostic[0];
        Statistics = new GifStatistics();
        DecodeStatistics = new GifDecodeStatistics();
        EncodeStatistics = new GifEncodeStatistics();
    }
}
