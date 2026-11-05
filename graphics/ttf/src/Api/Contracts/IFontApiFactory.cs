namespace ttf;

/// <summary>Defines creation operations that replace prohibited static factories.</summary>
public interface IFontApiFactory
{
    /// <summary>Creates a font-container loader service.</summary>
    /// <returns>A new stateless loader.</returns>
    FontLoader CreateFontLoader();

    /// <summary>Creates an empty application-owned font set.</summary>
    /// <returns>A new font set.</returns>
    FontSet CreateFontSet();

    /// <summary>Creates empty variable-font coordinates.</summary>
    /// <returns>A new coordinate collection.</returns>
    FontCoordinates CreateCoordinates();

    /// <summary>Creates variable-font coordinates from paired arrays.</summary>
    /// <param name="axisTags">Big-endian OpenType axis tags.</param>
    /// <param name="values">Finite user-scale values.</param>
    /// <returns>A new coordinate collection.</returns>
    FontCoordinates CreateConfiguredCoordinates(
        int[] axisTags,
        double[] values);

    /// <summary>Creates a concrete family request with normal defaults.</summary>
    /// <param name="familyName">The requested localized family name.</param>
    /// <returns>A new request.</returns>
    FontRequest CreateFamilyRequest(string familyName);

    /// <summary>Creates compatible general resource limits.</summary>
    /// <returns>The compatible limits.</returns>
    FontLimits CreateCompatibleLimits();

    /// <summary>Creates explicitly configured shared operation controls.</summary>
    /// <param name="mode">The strict, compatible, or inspect-only mode.</param>
    /// <param name="limits">The immutable hard limits.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A configured operation-control snapshot.</returns>
    FontOperationOptions CreateConfiguredOperationOptions(
        FontLoadMode mode,
        FontLimits limits,
        System.Threading.CancellationToken cancellationToken);

    /// <summary>Creates strict shared operation controls.</summary>
    /// <returns>Strict mode with strict finite limits.</returns>
    FontOperationOptions CreateStrictOperationOptions();

    /// <summary>Creates compatible shared operation controls.</summary>
    /// <returns>Compatible mode with compatible finite limits.</returns>
    FontOperationOptions CreateCompatibleOperationOptions();

    /// <summary>Creates inspect-only shared operation controls.</summary>
    /// <returns>Inspect-only mode with bounded inspection limits.</returns>
    FontOperationOptions CreateInspectOnlyOperationOptions();

    /// <summary>Creates compatible loading options without cancellation.</summary>
    /// <returns>The compatible load options.</returns>
    FontLoadOptions CreateCompatibleLoadOptions();

    /// <summary>Creates finite shaping limits with production defaults.</summary>
    /// <returns>New shaping limits.</returns>
    TextShapingLimits CreateShapingLimits();

    /// <summary>Creates shaping options with safe compatible defaults.</summary>
    /// <returns>New shaping options.</returns>
    TextShapingOptions CreateShapingOptions();

    /// <summary>Creates shaping options with explicit shared controls.</summary>
    /// <param name="operationOptions">Policy, limits, and cancellation.</param>
    /// <returns>New shaping options containing a snapshot.</returns>
    TextShapingOptions CreateShapingOptionsWithOperation(
        FontOperationOptions operationOptions);

    /// <summary>Creates a reusable public text-layout engine.</summary>
    /// <returns>A new layout engine.</returns>
    TextLayoutEngine CreateTextLayoutEngine();

    /// <summary>Creates text-layout options with safe defaults.</summary>
    /// <returns>New layout options.</returns>
    TextLayoutOptions CreateTextLayoutOptions();

    /// <summary>Creates layout options with explicit shared controls.</summary>
    /// <param name="operationOptions">Policy, limits, and cancellation.</param>
    /// <returns>New layout options containing a snapshot.</returns>
    TextLayoutOptions CreateTextLayoutOptionsWithOperation(
        FontOperationOptions operationOptions);

    /// <summary>Creates point-based text style defaults.</summary>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <returns>A new text style.</returns>
    TextStyle CreateTextStyle(double pointSize);

    /// <summary>Creates one source-range layout style.</summary>
    /// <param name="utf16Start">The source UTF-16 start.</param>
    /// <param name="utf16Length">The positive source length.</param>
    /// <param name="request">The font request.</param>
    /// <param name="style">The measured text style.</param>
    /// <returns>A new layout style.</returns>
    TextLayoutStyle CreateTextLayoutStyle(
        int utf16Start,
        int utf16Length,
        FontRequest request,
        TextStyle style);

    /// <summary>Creates a color-glyph resolver service.</summary>
    /// <returns>A new resolver.</returns>
    ColorGlyphResolver CreateColorGlyphResolver();

    /// <summary>Creates color resolution options with safe defaults.</summary>
    /// <returns>New color options.</returns>
    ColorGlyphOptions CreateColorGlyphOptions();

    /// <summary>Creates color options with explicit shared controls.</summary>
    /// <param name="operationOptions">Policy, limits, and cancellation.</param>
    /// <returns>New color options containing a snapshot.</returns>
    ColorGlyphOptions CreateColorGlyphOptionsWithOperation(
        FontOperationOptions operationOptions);

    /// <summary>Creates compatible subset options.</summary>
    /// <returns>New compatible subset options.</returns>
    FontSubsetOptions CreateCompatibleSubsetOptions();

    /// <summary>Creates a subset request builder.</summary>
    /// <param name="instance">The immutable source instance.</param>
    /// <param name="options">The subset options to snapshot.</param>
    /// <returns>A new subset builder.</returns>
    FontSubsetBuilder CreateSubsetBuilder(
        FontInstance instance,
        FontSubsetOptions options);

    /// <summary>Creates conservative default hyphenation limits.</summary>
    /// <returns>New hyphenation limits.</returns>
    HyphenationLimits CreateHyphenationLimits();

    /// <summary>Creates a bounded hyphenation dictionary loader.</summary>
    /// <returns>A new dictionary loader.</returns>
    HyphenationDictionaryLoader CreateHyphenationDictionaryLoader();

    /// <summary>Creates compatible PDF font options.</summary>
    /// <returns>New compatible PDF options.</returns>
    PdfFontOptions CreateCompatiblePdfOptions();

    /// <summary>Creates a single-instance PDF font builder.</summary>
    /// <param name="instance">The immutable font instance.</param>
    /// <param name="options">The PDF options to snapshot.</param>
    /// <returns>A new PDF font builder.</returns>
    PdfFontBuilder CreatePdfFontBuilder(
        FontInstance instance,
        PdfFontOptions options);

    /// <summary>Creates a fallback-aware PDF text usage builder.</summary>
    /// <param name="fontSet">The application-owned fallback set.</param>
    /// <param name="options">The PDF options to snapshot.</param>
    /// <returns>A new PDF text usage builder.</returns>
    PdfTextUsageBuilder CreatePdfTextUsageBuilder(
        FontSet fontSet,
        PdfFontOptions options);

    /// <summary>Creates a document-local PDF resource collection.</summary>
    /// <returns>A new empty deduplication collection.</returns>
    PdfFontResourceCollection CreatePdfFontResourceCollection();

    /// <summary>Creates a deterministic PDF ToUnicode CMap writer.</summary>
    /// <returns>A new writer.</returns>
    PdfToUnicodeCMapWriter CreatePdfToUnicodeCMapWriter();

    /// <summary>Creates explicitly configured general resource limits.</summary>
    /// <param name="maxInputBytes">Maximum input bytes.</param>
    /// <param name="maxOutputBytes">Maximum output bytes.</param>
    /// <param name="maxFaces">Maximum faces.</param>
    /// <param name="maxTablesPerFace">Maximum tables per face.</param>
    /// <param name="maxGlyphsPerFace">Maximum glyphs per face.</param>
    /// <param name="maxPointsPerGlyph">Maximum points per glyph.</param>
    /// <param name="maxContoursPerGlyph">Maximum contours per glyph.</param>
    /// <param name="maxComponentDepth">Maximum component depth.</param>
    /// <param name="maxCffSubroutineDepth">Maximum CFF subroutine depth.</param>
    /// <param name="maxLayoutNesting">Maximum layout nesting.</param>
    /// <param name="maxLookupsPerTable">Maximum lookups per table.</param>
    /// <param name="maxLayoutOperations">Maximum layout operations.</param>
    /// <param name="maxCoverageEntries">Maximum coverage entries.</param>
    /// <param name="maxStringBytes">Maximum string bytes.</param>
    /// <param name="maxDecompressedBytes">Maximum decompressed bytes.</param>
    /// <param name="maxDecompressionRatio">Maximum decompression ratio.</param>
    /// <param name="maxVariationRegions">Maximum variation regions.</param>
    /// <param name="maxPaintDepth">Maximum paint depth.</param>
    /// <param name="maxDiagnostics">Maximum retained diagnostics.</param>
    /// <returns>The configured limits.</returns>
    FontLimits CreateConfiguredLimits(
        int maxInputBytes,
        int maxOutputBytes,
        int maxFaces,
        int maxTablesPerFace,
        int maxGlyphsPerFace,
        int maxPointsPerGlyph,
        int maxContoursPerGlyph,
        int maxComponentDepth,
        int maxCffSubroutineDepth,
        int maxLayoutNesting,
        int maxLookupsPerTable,
        int maxLayoutOperations,
        int maxCoverageEntries,
        int maxStringBytes,
        int maxDecompressedBytes,
        int maxDecompressionRatio,
        int maxVariationRegions,
        int maxPaintDepth,
        int maxDiagnostics);

    /// <summary>Creates strict general resource limits.</summary>
    /// <returns>The strict limits.</returns>
    FontLimits CreateStrictLimits();

    /// <summary>Creates inspect-only general resource limits.</summary>
    /// <returns>The inspect-only limits.</returns>
    FontLimits CreateInspectOnlyLimits();

    /// <summary>Creates an explicitly configured load option set.</summary>
    /// <param name="mode">The structural mode.</param>
    /// <param name="inputOwnership">The memory ownership policy.</param>
    /// <param name="preserveRawTables">Whether raw tables remain available.</param>
    /// <param name="repairPolicy">The repair policy.</param>
    /// <param name="checksumPolicy">The checksum policy.</param>
    /// <param name="limits">The resource limits.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The configured load options.</returns>
    FontLoadOptions CreateConfiguredLoadOptions(
        FontLoadMode mode,
        FontInputOwnership inputOwnership,
        bool preserveRawTables,
        FontRepairPolicy repairPolicy,
        FontChecksumPolicy checksumPolicy,
        FontLimits limits,
        System.Threading.CancellationToken cancellationToken);

    /// <summary>Creates explicitly configured load options with a media-type hint.</summary>
    /// <param name="mode">The structural mode.</param>
    /// <param name="inputOwnership">The memory ownership policy.</param>
    /// <param name="preserveRawTables">Whether raw tables remain available.</param>
    /// <param name="repairPolicy">The repair policy.</param>
    /// <param name="checksumPolicy">The checksum policy.</param>
    /// <param name="limits">The resource limits.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="mediaTypeHint">The caller-supplied media type, or null.</param>
    /// <returns>The configured load options.</returns>
    FontLoadOptions CreateConfiguredLoadOptionsWithMediaTypeHint(
        FontLoadMode mode,
        FontInputOwnership inputOwnership,
        bool preserveRawTables,
        FontRepairPolicy repairPolicy,
        FontChecksumPolicy checksumPolicy,
        FontLimits limits,
        System.Threading.CancellationToken cancellationToken,
        string? mediaTypeHint);

    /// <summary>Creates strict load options.</summary>
    /// <returns>The strict load options.</returns>
    FontLoadOptions CreateStrictLoadOptions();

    /// <summary>Creates strict load options with cancellation.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The strict load options.</returns>
    FontLoadOptions CreateStrictLoadOptionsWithCancellation(
        System.Threading.CancellationToken cancellationToken);

    /// <summary>Creates compatible load options with cancellation.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The compatible load options.</returns>
    FontLoadOptions CreateCompatibleLoadOptionsWithCancellation(
        System.Threading.CancellationToken cancellationToken);

    /// <summary>Creates inspect-only load options.</summary>
    /// <returns>The inspect-only load options.</returns>
    FontLoadOptions CreateInspectOnlyLoadOptions();

    /// <summary>Creates inspect-only load options with cancellation.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The inspect-only load options.</returns>
    FontLoadOptions CreateInspectOnlyLoadOptionsWithCancellation(
        System.Threading.CancellationToken cancellationToken);

    /// <summary>Creates explicitly configured hyphenation limits.</summary>
    /// <param name="maxInputBytes">Maximum encoded bytes.</param>
    /// <param name="maxPatterns">Maximum patterns.</param>
    /// <param name="maxPatternLength">Maximum pattern length.</param>
    /// <param name="maxExceptions">Maximum exceptions.</param>
    /// <param name="maxTrieNodes">Maximum trie nodes.</param>
    /// <param name="maxWordLength">Maximum word length.</param>
    /// <returns>The configured limits.</returns>
    HyphenationLimits CreateConfiguredHyphenationLimits(
        int maxInputBytes,
        int maxPatterns,
        int maxPatternLength,
        int maxExceptions,
        int maxTrieNodes,
        int maxWordLength);

    /// <summary>Creates a generic-family request.</summary>
    /// <param name="genericFamily">The generic family.</param>
    /// <returns>The configured request.</returns>
    FontRequest CreateGenericRequest(FontGenericFamily genericFamily);

    /// <summary>Creates strict subset options.</summary>
    /// <returns>The strict subset options.</returns>
    FontSubsetOptions CreateStrictSubsetOptions();

    /// <summary>Creates strict PDF font options.</summary>
    /// <returns>The strict PDF font options.</returns>
    PdfFontOptions CreateStrictPdfOptions();
}
