namespace ttf;

/// <summary>Creates configured public API objects without static public methods.</summary>
public class FontApiFactory : IFontApiFactory
{
    /// <summary>Creates a public API object factory.</summary>
    public FontApiFactory()
    {
    }

    /// <summary>Creates a font-container loader service.</summary>
    /// <returns>A new stateless loader.</returns>
    public FontLoader CreateFontLoader() { return new FontLoader(); }

    /// <summary>Creates an empty application-owned font set.</summary>
    /// <returns>A new font set.</returns>
    public FontSet CreateFontSet() { return new FontSet(); }

    /// <summary>Creates empty variable-font coordinates.</summary>
    /// <returns>A new coordinate collection.</returns>
    public FontCoordinates CreateCoordinates()
    {
        return new FontCoordinates();
    }

    /// <summary>Creates variable-font coordinates from paired arrays.</summary>
    /// <param name="axisTags">Big-endian OpenType axis tags.</param>
    /// <param name="values">Finite user-scale values.</param>
    /// <returns>A new coordinate collection.</returns>
    /// <exception cref="System.ArgumentNullException">An array is null.</exception>
    /// <exception cref="System.ArgumentException">Array lengths differ.</exception>
    public FontCoordinates CreateConfiguredCoordinates(
        int[] axisTags,
        double[] values)
    {
        if (axisTags == null)
        {
            throw new System.ArgumentNullException("axisTags");
        }

        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (axisTags.Length != values.Length)
        {
            throw new System.ArgumentException(
                "Coordinate tag and value arrays must have equal lengths.",
                "values");
        }

        FontCoordinates coordinates = new FontCoordinates();
        int index = 0;
        while (index < axisTags.Length)
        {
            coordinates.Set(axisTags[index], values[index]);
            index += 1;
        }

        return coordinates;
    }

    /// <summary>Creates a concrete family request with normal defaults.</summary>
    /// <param name="familyName">The requested localized family name.</param>
    /// <returns>A new request.</returns>
    public FontRequest CreateFamilyRequest(string familyName)
    {
        return new FontRequest(familyName);
    }

    /// <summary>Creates compatible general resource limits.</summary>
    /// <returns>The compatible limits.</returns>
    public FontLimits CreateCompatibleLimits()
    {
        return FontLimits.CreateCompatible();
    }

    /// <summary>Creates explicitly configured shared operation controls.</summary>
    /// <param name="mode">The strict, compatible, or inspect-only mode.</param>
    /// <param name="limits">The immutable hard limits.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A configured operation-control snapshot.</returns>
    public FontOperationOptions CreateConfiguredOperationOptions(
        FontLoadMode mode,
        FontLimits limits,
        System.Threading.CancellationToken cancellationToken)
    {
        FontOperationOptions options = new FontOperationOptions();
        options.SetMode(mode);
        options.SetLimits(limits);
        options.SetCancellationToken(cancellationToken);
        return options;
    }

    /// <summary>Creates strict shared operation controls.</summary>
    /// <returns>Strict mode with strict finite limits.</returns>
    public FontOperationOptions CreateStrictOperationOptions()
    {
        return CreateConfiguredOperationOptions(
            FontLoadMode.Strict,
            FontLimits.CreateStrict(),
            System.Threading.CancellationToken.None);
    }

    /// <summary>Creates compatible shared operation controls.</summary>
    /// <returns>Compatible mode with compatible finite limits.</returns>
    public FontOperationOptions CreateCompatibleOperationOptions()
    {
        return CreateConfiguredOperationOptions(
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible(),
            System.Threading.CancellationToken.None);
    }

    /// <summary>Creates inspect-only shared operation controls.</summary>
    /// <returns>Inspect-only mode with bounded inspection limits.</returns>
    public FontOperationOptions CreateInspectOnlyOperationOptions()
    {
        return CreateConfiguredOperationOptions(
            FontLoadMode.InspectOnly,
            FontLimits.CreateInspectOnly(),
            System.Threading.CancellationToken.None);
    }

    /// <summary>Creates compatible loading options without cancellation.</summary>
    /// <returns>The compatible load options.</returns>
    public FontLoadOptions CreateCompatibleLoadOptions()
    {
        return FontLoadOptions.CreateCompatible(
            System.Threading.CancellationToken.None);
    }

    /// <summary>Creates finite shaping limits with production defaults.</summary>
    /// <returns>New shaping limits.</returns>
    public TextShapingLimits CreateShapingLimits()
    {
        return new TextShapingLimits();
    }

    /// <summary>Creates shaping options with safe compatible defaults.</summary>
    /// <returns>New shaping options.</returns>
    public TextShapingOptions CreateShapingOptions()
    {
        return new TextShapingOptions();
    }

    /// <summary>Creates shaping options with explicit shared controls.</summary>
    /// <param name="operationOptions">Policy, limits, and cancellation.</param>
    /// <returns>New shaping options containing a snapshot.</returns>
    public TextShapingOptions CreateShapingOptionsWithOperation(
        FontOperationOptions operationOptions)
    {
        TextShapingOptions options = new TextShapingOptions();
        options.SetOperationOptions(operationOptions);
        return options;
    }

    /// <summary>Creates a reusable public text-layout engine.</summary>
    /// <returns>A new layout engine.</returns>
    public TextLayoutEngine CreateTextLayoutEngine()
    {
        return new TextLayoutEngine();
    }

    /// <summary>Creates text-layout options with safe defaults.</summary>
    /// <returns>New layout options.</returns>
    public TextLayoutOptions CreateTextLayoutOptions()
    {
        return new TextLayoutOptions();
    }

    /// <summary>Creates layout options with explicit shared controls.</summary>
    /// <param name="operationOptions">Policy, limits, and cancellation.</param>
    /// <returns>New layout options containing a snapshot.</returns>
    public TextLayoutOptions CreateTextLayoutOptionsWithOperation(
        FontOperationOptions operationOptions)
    {
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetOperationOptions(operationOptions);
        return options;
    }

    /// <summary>Creates point-based text style defaults.</summary>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <returns>A new text style.</returns>
    public TextStyle CreateTextStyle(double pointSize)
    {
        return new TextStyle(pointSize);
    }

    /// <summary>Creates one source-range layout style.</summary>
    /// <param name="utf16Start">The source UTF-16 start.</param>
    /// <param name="utf16Length">The positive source length.</param>
    /// <param name="request">The font request.</param>
    /// <param name="style">The measured text style.</param>
    /// <returns>A new layout style.</returns>
    public TextLayoutStyle CreateTextLayoutStyle(
        int utf16Start,
        int utf16Length,
        FontRequest request,
        TextStyle style)
    {
        return new TextLayoutStyle(
            utf16Start,
            utf16Length,
            request,
            style);
    }

    /// <summary>Creates a color-glyph resolver service.</summary>
    /// <returns>A new resolver.</returns>
    public ColorGlyphResolver CreateColorGlyphResolver()
    {
        return new ColorGlyphResolver();
    }

    /// <summary>Creates color resolution options with safe defaults.</summary>
    /// <returns>New color options.</returns>
    public ColorGlyphOptions CreateColorGlyphOptions()
    {
        return new ColorGlyphOptions();
    }

    /// <summary>Creates color options with explicit shared controls.</summary>
    /// <param name="operationOptions">Policy, limits, and cancellation.</param>
    /// <returns>New color options containing a snapshot.</returns>
    public ColorGlyphOptions CreateColorGlyphOptionsWithOperation(
        FontOperationOptions operationOptions)
    {
        ColorGlyphOptions options = new ColorGlyphOptions();
        options.SetOperationOptions(operationOptions);
        return options;
    }

    /// <summary>Creates compatible subset options.</summary>
    /// <returns>New compatible subset options.</returns>
    public FontSubsetOptions CreateCompatibleSubsetOptions()
    {
        return FontSubsetOptions.CreateCompatible();
    }

    /// <summary>Creates a subset request builder.</summary>
    /// <param name="instance">The immutable source instance.</param>
    /// <param name="options">The subset options to snapshot.</param>
    /// <returns>A new subset builder.</returns>
    public FontSubsetBuilder CreateSubsetBuilder(
        FontInstance instance,
        FontSubsetOptions options)
    {
        return new FontSubsetBuilder(instance, options);
    }

    /// <summary>Creates conservative default hyphenation limits.</summary>
    /// <returns>New hyphenation limits.</returns>
    public HyphenationLimits CreateHyphenationLimits()
    {
        return new HyphenationLimits();
    }

    /// <summary>Creates a bounded hyphenation dictionary loader.</summary>
    /// <returns>A new dictionary loader.</returns>
    public HyphenationDictionaryLoader CreateHyphenationDictionaryLoader()
    {
        return new HyphenationDictionaryLoader();
    }

    /// <summary>Creates compatible PDF font options.</summary>
    /// <returns>New compatible PDF options.</returns>
    public PdfFontOptions CreateCompatiblePdfOptions()
    {
        return PdfFontOptions.CreateCompatible();
    }

    /// <summary>Creates a single-instance PDF font builder.</summary>
    /// <param name="instance">The immutable font instance.</param>
    /// <param name="options">The PDF options to snapshot.</param>
    /// <returns>A new PDF font builder.</returns>
    public PdfFontBuilder CreatePdfFontBuilder(
        FontInstance instance,
        PdfFontOptions options)
    {
        return new PdfFontBuilder(instance, options);
    }

    /// <summary>Creates a fallback-aware PDF text usage builder.</summary>
    /// <param name="fontSet">The application-owned fallback set.</param>
    /// <param name="options">The PDF options to snapshot.</param>
    /// <returns>A new PDF text usage builder.</returns>
    public PdfTextUsageBuilder CreatePdfTextUsageBuilder(
        FontSet fontSet,
        PdfFontOptions options)
    {
        return new PdfTextUsageBuilder(fontSet, options);
    }

    /// <summary>Creates a document-local PDF resource collection.</summary>
    /// <returns>A new empty deduplication collection.</returns>
    public PdfFontResourceCollection CreatePdfFontResourceCollection()
    {
        return new PdfFontResourceCollection();
    }

    /// <summary>Creates a deterministic PDF ToUnicode CMap writer.</summary>
    /// <returns>A new writer.</returns>
    public PdfToUnicodeCMapWriter CreatePdfToUnicodeCMapWriter()
    {
        return new PdfToUnicodeCMapWriter();
    }

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
    public FontLimits CreateConfiguredLimits(
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
        int maxDiagnostics)
    {
        return FontLimits.Create(
            maxInputBytes,
            maxOutputBytes,
            maxFaces,
            maxTablesPerFace,
            maxGlyphsPerFace,
            maxPointsPerGlyph,
            maxContoursPerGlyph,
            maxComponentDepth,
            maxCffSubroutineDepth,
            maxLayoutNesting,
            maxLookupsPerTable,
            maxLayoutOperations,
            maxCoverageEntries,
            maxStringBytes,
            maxDecompressedBytes,
            maxDecompressionRatio,
            maxVariationRegions,
            maxPaintDepth,
            maxDiagnostics);
    }

    /// <summary>Creates strict general resource limits.</summary>
    /// <returns>The strict limits.</returns>
    public FontLimits CreateStrictLimits()
    {
        return FontLimits.CreateStrict();
    }

    /// <summary>Creates inspect-only general resource limits.</summary>
    /// <returns>The inspect-only limits.</returns>
    public FontLimits CreateInspectOnlyLimits()
    {
        return FontLimits.CreateInspectOnly();
    }

    /// <summary>Creates an explicitly configured load option set.</summary>
    /// <param name="mode">The structural mode.</param>
    /// <param name="inputOwnership">The memory ownership policy.</param>
    /// <param name="preserveRawTables">Whether raw tables remain available.</param>
    /// <param name="repairPolicy">The repair policy.</param>
    /// <param name="checksumPolicy">The checksum policy.</param>
    /// <param name="limits">The resource limits.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The configured load options.</returns>
    public FontLoadOptions CreateConfiguredLoadOptions(
        FontLoadMode mode,
        FontInputOwnership inputOwnership,
        bool preserveRawTables,
        FontRepairPolicy repairPolicy,
        FontChecksumPolicy checksumPolicy,
        FontLimits limits,
        System.Threading.CancellationToken cancellationToken)
    {
        return FontLoadOptions.Create(
            mode,
            inputOwnership,
            preserveRawTables,
            repairPolicy,
            checksumPolicy,
            limits,
            cancellationToken);
    }

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
    public FontLoadOptions CreateConfiguredLoadOptionsWithMediaTypeHint(
        FontLoadMode mode,
        FontInputOwnership inputOwnership,
        bool preserveRawTables,
        FontRepairPolicy repairPolicy,
        FontChecksumPolicy checksumPolicy,
        FontLimits limits,
        System.Threading.CancellationToken cancellationToken,
        string? mediaTypeHint)
    {
        return FontLoadOptions.CreateWithMediaTypeHint(
            mode,
            inputOwnership,
            preserveRawTables,
            repairPolicy,
            checksumPolicy,
            limits,
            cancellationToken,
            mediaTypeHint);
    }

    /// <summary>Creates strict load options.</summary>
    /// <returns>The strict load options.</returns>
    public FontLoadOptions CreateStrictLoadOptions()
    {
        return FontLoadOptions.CreateStrict();
    }

    /// <summary>Creates strict load options with cancellation.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The strict load options.</returns>
    public FontLoadOptions CreateStrictLoadOptionsWithCancellation(
        System.Threading.CancellationToken cancellationToken)
    {
        return FontLoadOptions.CreateStrict(cancellationToken);
    }

    /// <summary>Creates compatible load options with cancellation.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The compatible load options.</returns>
    public FontLoadOptions CreateCompatibleLoadOptionsWithCancellation(
        System.Threading.CancellationToken cancellationToken)
    {
        return FontLoadOptions.CreateCompatible(cancellationToken);
    }

    /// <summary>Creates inspect-only load options.</summary>
    /// <returns>The inspect-only load options.</returns>
    public FontLoadOptions CreateInspectOnlyLoadOptions()
    {
        return FontLoadOptions.CreateInspectOnly();
    }

    /// <summary>Creates inspect-only load options with cancellation.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The inspect-only load options.</returns>
    public FontLoadOptions CreateInspectOnlyLoadOptionsWithCancellation(
        System.Threading.CancellationToken cancellationToken)
    {
        return FontLoadOptions.CreateInspectOnly(cancellationToken);
    }

    /// <summary>Creates explicitly configured hyphenation limits.</summary>
    /// <param name="maxInputBytes">Maximum encoded bytes.</param>
    /// <param name="maxPatterns">Maximum patterns.</param>
    /// <param name="maxPatternLength">Maximum pattern length.</param>
    /// <param name="maxExceptions">Maximum exceptions.</param>
    /// <param name="maxTrieNodes">Maximum trie nodes.</param>
    /// <param name="maxWordLength">Maximum word length.</param>
    /// <returns>The configured limits.</returns>
    public HyphenationLimits CreateConfiguredHyphenationLimits(
        int maxInputBytes,
        int maxPatterns,
        int maxPatternLength,
        int maxExceptions,
        int maxTrieNodes,
        int maxWordLength)
    {
        return HyphenationLimits.Create(
            maxInputBytes,
            maxPatterns,
            maxPatternLength,
            maxExceptions,
            maxTrieNodes,
            maxWordLength);
    }

    /// <summary>Creates a generic-family request.</summary>
    /// <param name="genericFamily">The generic family.</param>
    /// <returns>The configured request.</returns>
    public FontRequest CreateGenericRequest(FontGenericFamily genericFamily)
    {
        return FontRequest.CreateGeneric(genericFamily);
    }

    /// <summary>Creates strict subset options.</summary>
    /// <returns>The strict subset options.</returns>
    public FontSubsetOptions CreateStrictSubsetOptions()
    {
        return FontSubsetOptions.CreateStrict();
    }

    /// <summary>Creates strict PDF font options.</summary>
    /// <returns>The strict PDF font options.</returns>
    public PdfFontOptions CreateStrictPdfOptions()
    {
        return PdfFontOptions.CreateStrict();
    }
}
