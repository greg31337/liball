namespace unicode;

/// <summary>
/// Checks PDF text strings and font mappings for Unicode and extraction problems.
/// </summary>
public class UnicodePdfDiagnostics : IUnicodePdfDiagnostics
{
    internal static readonly UnicodePdfDiagnostics Instance = new UnicodePdfDiagnostics();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodePdfDiagnostics()
    {
    }
    /// <summary>
    /// The stable diagnostic rule identifier for invalid Text Scalar.
    /// </summary>
    internal const string InvalidTextScalarRule = "Pdf.Text.InvalidScalar";
    /// <summary>
    /// The stable diagnostic rule identifier for needs Actual Text Preparation.
    /// </summary>
    internal const string NeedsActualTextPreparationRule = "Pdf.Text.NeedsActualTextPreparation";
    /// <summary>
    /// The stable diagnostic rule identifier for requires UTF-16 Be.
    /// </summary>
    internal const string RequiresUtf16BeRule = "Pdf.Text.RequiresUtf16Be";
    /// <summary>
    /// The stable diagnostic rule identifier for missing Unicode Map.
    /// </summary>
    internal const string MissingUnicodeMapRule = "Pdf.Font.MissingToUnicodeMap";
    /// <summary>
    /// The stable diagnostic rule identifier for invalid Unicode Map.
    /// </summary>
    internal const string InvalidUnicodeMapRule = "Pdf.Font.InvalidToUnicodeMap";
    /// <summary>
    /// The stable diagnostic rule identifier for accessibility text preparation.
    /// </summary>
    internal const string NeedsAccessibilityTextPreparationRule = "Pdf.Text.NeedsAccessibilityPreparation";
    /// <summary>
    /// The stable diagnostic rule identifier for prohibited accessibility scalars.
    /// </summary>
    internal const string ProhibitedTextScalarRule = "Pdf.Text.ProhibitedScalar";
    /// <summary>
    /// The stable diagnostic rule identifier for explicit replacement scalars.
    /// </summary>
    internal const string ReplacementTextScalarRule = "Pdf.Text.ReplacementScalar";

    /// <summary>
    /// Checks whether text is valid, accessibility-ready, and encodable as a PDF text string.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>An object containing the result described in the summary.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodePdfDiagnosticResult CheckTextString(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics = new System.Collections.Generic.List<UnicodePdfDiagnostic>();
        AddTextStringDiagnostics(text, diagnostics);
        return CreateDiagnosticResult(diagnostics);
    }

    /// <summary>
    /// Checks whether text is valid, prepared, and encodable for a PDF accessibility context.
    /// </summary>
    /// <param name="text">The UTF-16 text to inspect.</param>
    /// <param name="context">The PDF text context.</param>
    /// <returns>The combined accessibility-text diagnostics.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>context</c> is invalid.</exception>
    public UnicodePdfDiagnosticResult CheckAccessibilityText(string text, UnicodePdfTextContext context)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        UnicodePdfText.ValidateTextContext(context);

        System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics = new System.Collections.Generic.List<UnicodePdfDiagnostic>();
        AddAccessibilityTextDiagnostics(text, context, diagnostics);
        return CreateDiagnosticResult(diagnostics);
    }

    /// <summary>
    /// Checks PDF/A or PDF/UA text preparation, text-string encoding, and ToUnicode requirements.
    /// </summary>
    /// <param name="text">The UTF-16 text to inspect.</param>
    /// <param name="context">The PDF text context.</param>
    /// <param name="conformance">The conformance profile.</param>
    /// <param name="unicodeMapRequired">Whether the font text requires a ToUnicode map.</param>
    /// <param name="glyphMap">The glyph-map snapshot, or <c>null</c> when none is present.</param>
    /// <returns>The aggregated conformance diagnostics.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an enum value is invalid.</exception>
    public UnicodePdfDiagnosticResult CheckPdfConformance(
        string text,
        UnicodePdfTextContext context,
        UnicodePdfConformance conformance,
        bool unicodeMapRequired,
        IUnicodePdfGlyphMap? glyphMap)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        UnicodePdfText.ValidateTextContext(context);
        ValidateConformance(conformance);

        System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics = new System.Collections.Generic.List<UnicodePdfDiagnostic>();
        AddAccessibilityTextDiagnostics(text, context, diagnostics);
        AddConformanceMappingDiagnostics(conformance, unicodeMapRequired, glyphMap, diagnostics);
        return CreateDiagnosticResult(diagnostics);
    }

    /// <summary>
    /// Checks a supplied font glyph-map snapshot for missing or invalid ToUnicode mappings.
    /// </summary>
    /// <param name="glyphMap">The PDF glyph-map source to inspect, or <c>null</c> when no map is present.</param>
    /// <returns>An object containing the result described in the summary.</returns>
    public UnicodePdfDiagnosticResult CheckFontUnicodeMapping(IUnicodePdfGlyphMap? glyphMap)
    {
        System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics = new System.Collections.Generic.List<UnicodePdfDiagnostic>();
        AddFontUnicodeMappingDiagnostics(glyphMap, diagnostics);
        return CreateDiagnosticResult(diagnostics);
    }

    internal static void AddTextStringDiagnostics(
        string text,
        System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        bool hasInvalidScalar = AddInvalidTextScalarDiagnostics(scalars, diagnostics);
        if (!hasInvalidScalar)
        {
            AddActualTextPreparationDiagnostic(text, diagnostics);
            AddTextStringEncodingDiagnostic(scalars, diagnostics);
        }
    }

    internal static void AddAccessibilityTextDiagnostics(
        string text,
        UnicodePdfTextContext context,
        System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(diagnostics);
        UnicodePdfText.ValidateTextContext(context);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        bool hasInvalidScalar = AddInvalidTextScalarDiagnostics(scalars, diagnostics);
        if (hasInvalidScalar)
        {
            return;
        }

        AddAccessibilityScalarDiagnostics(scalars, diagnostics);
        AddAccessibilityTextPreparationDiagnostic(text, context, diagnostics);
        AddTextStringEncodingDiagnostic(scalars, diagnostics);
    }

    internal static void AddAccessibilityScalarDiagnostics(
        UnicodeScalarRecord[] scalars,
        System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        int index = 0;
        while (index < scalars.Length)
        {
            if (scalars[index] == null)
            {
                throw new System.ArgumentNullException("scalars");
            }

            if (scalars[index].CodePoint == UnicodeScalar.ReplacementCharacter)
            {
                diagnostics.Add(new UnicodePdfDiagnostic(
                    scalars[index].Utf16Index,
                    scalars[index].Utf16Length,
                    scalars[index].CodePoint,
                    -1,
                    -1,
                    UnicodePdfDiagnosticStatus.ReplacementTextScalar,
                    ReplacementTextScalarRule,
                    "PDF accessibility text contains U+FFFD and may no longer represent the original source text."));
            }
            else if (UnicodePdfText.IsProhibitedAccessibilityScalar(scalars[index].CodePoint))
            {
                diagnostics.Add(new UnicodePdfDiagnostic(
                    scalars[index].Utf16Index,
                    scalars[index].Utf16Length,
                    scalars[index].CodePoint,
                    -1,
                    -1,
                    UnicodePdfDiagnosticStatus.ProhibitedTextScalar,
                    ProhibitedTextScalarRule,
                    "PDF accessibility text contains a prohibited C0 control scalar."));
            }

            index = index + 1;
        }
    }

    internal static void AddAccessibilityTextPreparationDiagnostic(
        string text,
        UnicodePdfTextContext context,
        System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(diagnostics);
        UnicodePdfText.ValidateTextContext(context);

        if (!UnicodePdfText.Instance.IsAccessibilityTextPrepared(text, context))
        {
            diagnostics.Add(new UnicodePdfDiagnostic(
                0,
                text.Length,
                -1,
                -1,
                -1,
                UnicodePdfDiagnosticStatus.NeedsAccessibilityTextPreparation,
                NeedsAccessibilityTextPreparationRule,
                GetTextContextName(context) + " text should contain permitted scalars and be normalized to NFC."));
        }
    }

    internal static bool AddInvalidTextScalarDiagnostics(
        UnicodeScalarRecord[] scalars,
        System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        bool hasInvalidScalar = false;
        int index = 0;
        while (index < scalars.Length)
        {
            if (scalars[index].IsReplacement)
            {
                diagnostics.Add(new UnicodePdfDiagnostic(
                    scalars[index].Utf16Index,
                    scalars[index].Utf16Length,
                    scalars[index].CodePoint,
                    -1,
                    -1,
                    UnicodePdfDiagnosticStatus.InvalidTextScalar,
                    InvalidTextScalarRule,
                    "PDF text strings must be built from well-formed Unicode scalar input."));
                hasInvalidScalar = true;
            }

            index = index + 1;
        }

        return hasInvalidScalar;
    }

    internal static void AddActualTextPreparationDiagnostic(
        string text,
        System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        if (!UnicodePdfText.Instance.IsActualTextPrepared(text))
        {
            diagnostics.Add(new UnicodePdfDiagnostic(
                0,
                text.Length,
                -1,
                -1,
                -1,
                UnicodePdfDiagnosticStatus.NeedsActualTextPreparation,
                NeedsActualTextPreparationRule,
                "PDF accessibility text should be valid scalar text normalized with UnicodePdfText.Instance.PrepareActualText."));
        }
    }

    internal static void AddTextStringEncodingDiagnostic(
        UnicodeScalarRecord[] scalars,
        System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        int index = FindFirstScalarRequiringUtf16Be(scalars);
        if (index >= 0)
        {
            diagnostics.Add(new UnicodePdfDiagnostic(
                scalars[index].Utf16Index,
                scalars[index].Utf16Length,
                scalars[index].CodePoint,
                -1,
                -1,
                UnicodePdfDiagnosticStatus.RequiresUtf16Be,
                RequiresUtf16BeRule,
                "PDF text string contains a scalar that requires UTF-16BE encoding with a byte-order mark."));
        }
    }

    internal static int FindFirstScalarRequiringUtf16Be(UnicodeScalarRecord[] scalars)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        int index = 0;
        while (index < scalars.Length)
        {
            if (scalars[index].IsReplacement)
            {
                return index;
            }

            if (UnicodePdfEncoding.FindPdfDocEncodingByte(scalars[index].CodePoint) < 0)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static void AddFontUnicodeMappingDiagnostics(
        IUnicodePdfGlyphMap? glyphMap,
        System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        if (glyphMap == null)
        {
            diagnostics.Add(new UnicodePdfDiagnostic(
                -1,
                -1,
                -1,
                -1,
                -1,
                UnicodePdfDiagnosticStatus.MissingUnicodeMap,
                MissingUnicodeMapRule,
                "PDF/UA text extraction requires a ToUnicode map for embedded font text."));
            return;
        }

        UnicodePdfCMapValidationResult validationResult = UnicodePdfCMap.Instance.ValidateToUnicodeMap(glyphMap);
        int index = 0;
        while (index < validationResult.Diagnostics.Length)
        {
            AddCMapValidationDiagnostic(validationResult.Diagnostics[index], diagnostics);
            index = index + 1;
        }
    }

    internal static void AddConformanceMappingDiagnostics(
        UnicodePdfConformance conformance,
        bool unicodeMapRequired,
        IUnicodePdfGlyphMap? glyphMap,
        System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostics);
        ValidateConformance(conformance);

        if (glyphMap == null)
        {
            if (unicodeMapRequired)
            {
                diagnostics.Add(new UnicodePdfDiagnostic(
                    -1,
                    -1,
                    -1,
                    -1,
                    -1,
                    UnicodePdfDiagnosticStatus.MissingUnicodeMap,
                    MissingUnicodeMapRule,
                    GetConformanceName(conformance) + " conformance requires ToUnicode data for this font text."));
            }

            return;
        }

        AddFontUnicodeMappingDiagnostics(glyphMap, diagnostics);
    }

    internal static void ValidateConformance(UnicodePdfConformance conformance)
    {
        if (conformance < UnicodePdfConformance.PdfA || conformance > UnicodePdfConformance.PdfAAndPdfUa)
        {
            throw new System.ArgumentOutOfRangeException("conformance");
        }
    }

    internal static string GetConformanceName(UnicodePdfConformance conformance)
    {
        ValidateConformance(conformance);

        if (conformance == UnicodePdfConformance.PdfA)
        {
            return "PDF/A";
        }

        if (conformance == UnicodePdfConformance.PdfUa)
        {
            return "PDF/UA";
        }

        return "PDF/A and PDF/UA";
    }

    internal static string GetTextContextName(UnicodePdfTextContext context)
    {
        UnicodePdfText.ValidateTextContext(context);

        if (context == UnicodePdfTextContext.ActualText)
        {
            return "ActualText";
        }

        if (context == UnicodePdfTextContext.AltText)
        {
            return "Alt";
        }

        if (context == UnicodePdfTextContext.Bookmark)
        {
            return "Bookmark";
        }

        if (context == UnicodePdfTextContext.Outline)
        {
            return "Outline";
        }

        if (context == UnicodePdfTextContext.StructureElement)
        {
            return "Structure element";
        }

        return "Metadata";
    }

    internal static void AddCMapValidationDiagnostic(
        UnicodePdfCMapDiagnostic cmapDiagnostic,
        System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(cmapDiagnostic);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        UnicodePdfDiagnosticStatus status = UnicodePdfDiagnosticStatus.InvalidUnicodeMap;
        string rule = InvalidUnicodeMapRule;
        string message = "ToUnicode map is present but does not provide valid Unicode mappings for PDF text extraction.";
        if (cmapDiagnostic.Status == UnicodePdfCMapStatus.EmptyMap)
        {
            status = UnicodePdfDiagnosticStatus.MissingUnicodeMap;
            rule = MissingUnicodeMapRule;
            message = "PDF/UA text extraction requires a non-empty ToUnicode map for embedded font text.";
        }

        diagnostics.Add(new UnicodePdfDiagnostic(
            -1,
            -1,
            cmapDiagnostic.CodePoint,
            cmapDiagnostic.MappingIndex,
            cmapDiagnostic.CharacterCode,
            status,
            rule,
            message));
    }

    internal static UnicodePdfDiagnosticResult CreateDiagnosticResult(System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        return new UnicodePdfDiagnosticResult(
            !HasHardFailures(diagnostics),
            GetDiagnosticStatus(diagnostics),
            diagnostics.ToArray());
    }

    internal static UnicodePdfDiagnosticStatus GetDiagnosticStatus(System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        UnicodePdfDiagnosticStatus status = UnicodePdfDiagnosticStatus.Valid;
        int priority = 0;
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index] == null)
            {
                throw new System.ArgumentNullException("diagnostics");
            }

            int diagnosticPriority = GetDiagnosticPriority(diagnostics[index].Status);
            if (diagnosticPriority > priority)
            {
                status = diagnostics[index].Status;
                priority = diagnosticPriority;
            }

            index = index + 1;
        }

        return status;
    }

    internal static int GetDiagnosticPriority(UnicodePdfDiagnosticStatus status)
    {
        if (status == UnicodePdfDiagnosticStatus.InvalidTextScalar)
        {
            return 80;
        }

        if (status == UnicodePdfDiagnosticStatus.ProhibitedTextScalar)
        {
            return 70;
        }

        if (status == UnicodePdfDiagnosticStatus.ReplacementTextScalar)
        {
            return 70;
        }

        if (status == UnicodePdfDiagnosticStatus.InvalidUnicodeMap)
        {
            return 60;
        }

        if (status == UnicodePdfDiagnosticStatus.MissingUnicodeMap)
        {
            return 50;
        }

        if (status == UnicodePdfDiagnosticStatus.NeedsActualTextPreparation)
        {
            return 40;
        }

        if (status == UnicodePdfDiagnosticStatus.NeedsAccessibilityTextPreparation)
        {
            return 40;
        }

        if (status == UnicodePdfDiagnosticStatus.RequiresUtf16Be)
        {
            return 10;
        }

        return 0;
    }

    internal static bool HasHardFailures(System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index] == null)
            {
                throw new System.ArgumentNullException("diagnostics");
            }

            if (IsHardFailure(diagnostics[index].Status))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsHardFailure(UnicodePdfDiagnosticStatus status)
    {
        if (status == UnicodePdfDiagnosticStatus.InvalidTextScalar)
        {
            return true;
        }

        if (status == UnicodePdfDiagnosticStatus.NeedsActualTextPreparation)
        {
            return true;
        }

        if (status == UnicodePdfDiagnosticStatus.NeedsAccessibilityTextPreparation)
        {
            return true;
        }

        if (status == UnicodePdfDiagnosticStatus.ProhibitedTextScalar)
        {
            return true;
        }

        if (status == UnicodePdfDiagnosticStatus.ReplacementTextScalar)
        {
            return true;
        }

        if (status == UnicodePdfDiagnosticStatus.MissingUnicodeMap)
        {
            return true;
        }

        return status == UnicodePdfDiagnosticStatus.InvalidUnicodeMap;
    }
}
