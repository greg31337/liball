namespace svg;

/// <summary>Contains one bounded staged input and explicit ownership metadata.</summary>
public class SvgLoadSource
{
    /// <summary>The staged representation kind.</summary>
    public SvgLoadSourceKind Kind;
    /// <summary>The staged character snapshot, or <c>null</c>.</summary>
    public string? Characters;
    /// <summary>The staged bytes, or <c>null</c>.</summary>
    public byte[]? Bytes;
    /// <summary>The retained caller-owned XML reader, or <c>null</c>.</summary>
    public System.Xml.XmlReader? XmlReader;
    /// <summary>The preserved source URI or path, or an empty string.</summary>
    public string SourceUri;
    /// <summary>Whether the caller retains ownership of the supplied source object.</summary>
    public bool CallerRetainsOwnership;
    /// <summary>Whether staged characters or bytes are independent from the supplied mutable source.</summary>
    public bool IsIndependentSnapshot;
    /// <summary>The staged byte or UTF-16 character count.</summary>
    public long Length;

    /// <summary>Creates ownership-explicit staged source data.</summary>
    /// <param name="kind">The source representation kind.</param>
    /// <param name="characters">The staged characters, or <c>null</c>.</param>
    /// <param name="bytes">The staged bytes, or <c>null</c>.</param>
    /// <param name="xmlReader">The retained XML reader, or <c>null</c>.</param>
    /// <param name="sourceUri">The preserved source URI.</param>
    /// <param name="callerRetainsOwnership">Whether caller ownership is retained.</param>
    /// <param name="isIndependentSnapshot">Whether staged data is independent.</param>
    public SvgLoadSource(SvgLoadSourceKind kind, string? characters, byte[]? bytes, System.Xml.XmlReader? xmlReader, string sourceUri, bool callerRetainsOwnership, bool isIndependentSnapshot)
    {
        if (sourceUri == null) throw new System.ArgumentNullException("sourceUri");
        bool validCharacters = kind == SvgLoadSourceKind.Characters && characters != null && bytes == null && xmlReader == null;
        bool validBytes = kind == SvgLoadSourceKind.Bytes && characters == null && bytes != null && xmlReader == null;
        bool validReader = kind == SvgLoadSourceKind.XmlReader && characters == null && bytes == null && xmlReader != null;
        if (!validCharacters && !validBytes && !validReader) throw new System.ArgumentOutOfRangeException("kind");
        Kind = kind;
        Characters = characters;
        Bytes = bytes;
        XmlReader = xmlReader;
        SourceUri = sourceUri;
        CallerRetainsOwnership = callerRetainsOwnership;
        IsIndependentSnapshot = isIndependentSnapshot;
        Length = characters != null ? characters.Length : bytes != null ? bytes.Length : 0;
    }
}

/// <summary>Summarizes root-resource acquisition performed while loading.</summary>
public class SvgResourceSummary
{
    /// <summary>The caller resource request count.</summary>
    public int RequestCount;
    /// <summary>The successfully acquired resource count.</summary>
    public int LoadedCount;
    /// <summary>The total staged encoded byte count.</summary>
    public long EncodedBytes;

    /// <summary>Creates an empty resource summary.</summary>
    public SvgResourceSummary()
    {
        RequestCount = 0;
        LoadedCount = 0;
        EncodedBytes = 0;
    }
}

/// <summary>Contains a bounded source-acquisition result for later container and XML stages.</summary>
public class SvgLoadResult
{
    /// <summary>The loaded document, or <c>null</c> until XML construction succeeds.</summary>
    public SvgDocument? Document;
    /// <summary>The staged input, or <c>null</c> after failure.</summary>
    public SvgLoadSource? Source;
    /// <summary>The ordered acquisition diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The root-resource acquisition summary.</summary>
    public SvgResourceSummary Resources;
    /// <summary>The detected media type, or an empty string before container detection.</summary>
    public string DetectedMediaType;
    /// <summary>The detected character encoding, or an empty string before container detection.</summary>
    public string DetectedEncoding;
    /// <summary>The detected container kind, or an empty string before container detection.</summary>
    public string DetectedContainer;
    /// <summary>The decoded XML characters, or <c>null</c> when decoding failed or an XML reader was supplied.</summary>
    public string? DecodedCharacters;
    /// <summary>The source-preserving XML lexical token snapshot.</summary>
    public System.Collections.Generic.List<SvgXmlToken> XmlTokens;
    /// <summary>Whether bounded source acquisition succeeded.</summary>
    public bool Success;

    /// <summary>Creates an empty failed loading result.</summary>
    public SvgLoadResult()
    {
        Document = null;
        Source = null;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Resources = new SvgResourceSummary();
        DetectedMediaType = "";
        DetectedEncoding = "";
        DetectedContainer = "";
        DecodedCharacters = null;
        XmlTokens = new System.Collections.Generic.List<SvgXmlToken>();
        Success = false;
    }
}
