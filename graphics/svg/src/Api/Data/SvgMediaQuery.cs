namespace svg;

/// <summary>Represents one parsed media feature expression.</summary>
public class SvgMediaFeature
{
    /// <summary>The normalized authored feature name without a min or max prefix.</summary>
    public string Name;
    /// <summary>The recognized feature kind.</summary>
    public SvgMediaFeatureKind Kind;
    /// <summary>The comparison applied during evaluation.</summary>
    public SvgMediaComparison Comparison;
    /// <summary>The exact authored value text, or an empty string for Boolean context.</summary>
    public string ValueText;
    /// <summary>The parsed numeric component, or zero for keyword and Boolean features.</summary>
    public double NumericValue;
    /// <summary>The normalized numeric unit, or an empty string.</summary>
    public string Unit;
    /// <summary>The normalized keyword value, or an empty string.</summary>
    public string Keyword;
    /// <summary>The source span, or <c>null</c>.</summary>
    public SvgSourceSpan? Source;
    /// <summary>Whether the feature syntax and value are valid.</summary>
    public bool IsValid;

    /// <summary>Creates an empty unknown media feature.</summary>
    public SvgMediaFeature()
    {
        Name = "";
        Kind = SvgMediaFeatureKind.Unknown;
        Comparison = SvgMediaComparison.Boolean;
        ValueText = "";
        NumericValue = 0;
        Unit = "";
        Keyword = "";
        Source = null;
        IsValid = false;
    }
}

/// <summary>Represents one conjunction in a comma-separated media query list.</summary>
public class SvgMediaQuery
{
    /// <summary>The optional query modifier.</summary>
    public SvgMediaQueryModifier Modifier;
    /// <summary>The normalized media type, using all when omitted.</summary>
    public string MediaType;
    /// <summary>The ordered and-connected media features.</summary>
    public System.Collections.Generic.List<SvgMediaFeature> Features;
    /// <summary>The exact authored query text.</summary>
    public string RawText;
    /// <summary>The source span, or <c>null</c>.</summary>
    public SvgSourceSpan? Source;
    /// <summary>Whether the complete query is valid.</summary>
    public bool IsValid;

    /// <summary>Creates an empty invalid media query.</summary>
    public SvgMediaQuery()
    {
        Modifier = SvgMediaQueryModifier.None;
        MediaType = "all";
        Features = new System.Collections.Generic.List<SvgMediaFeature>();
        RawText = "";
        Source = null;
        IsValid = false;
    }
}

/// <summary>Contains a comma-separated media query list.</summary>
public class SvgMediaQueryList
{
    /// <summary>The ordered queries, which are evaluated as alternatives.</summary>
    public System.Collections.Generic.List<SvgMediaQuery> Queries;
    /// <summary>The complete authored query-list text.</summary>
    public string SourceText;
    /// <summary>The ordered parse diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether every query in the list is valid.</summary>
    public bool IsValid;

    /// <summary>Creates a media query list and copies its collections.</summary>
    /// <param name="queries">The ordered queries.</param>
    /// <param name="sourceText">The complete source text.</param>
    /// <param name="diagnostics">The ordered diagnostics.</param>
    public SvgMediaQueryList(System.Collections.Generic.List<SvgMediaQuery> queries, string sourceText, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        if (queries == null) throw new System.ArgumentNullException("queries");
        if (sourceText == null) throw new System.ArgumentNullException("sourceText");
        if (diagnostics == null) throw new System.ArgumentNullException("diagnostics");
        Queries = new System.Collections.Generic.List<SvgMediaQuery>(queries);
        SourceText = sourceText;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>(diagnostics);
        IsValid = AllValid(Queries) && !HasErrors(Diagnostics);
    }

    internal static bool AllValid(System.Collections.Generic.List<SvgMediaQuery> queries)
    {
        if (queries.Count == 0) return false;
        int index = 0;
        while (index < queries.Count)
        {
            if (queries[index] == null || !queries[index].IsValid) return false;
            index = index + 1;
        }
        return true;
    }

    internal static bool HasErrors(System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index] != null && diagnostics[index].Severity == SvgDiagnosticSeverity.Error) return true;
            index = index + 1;
        }
        return false;
    }
}

/// <summary>Contains a bounded media-query parse result.</summary>
public class SvgMediaQueryParseResult
{
    /// <summary>Whether tokenization and bounded parsing completed.</summary>
    public bool Success;
    /// <summary>The parsed query list, or <c>null</c> after a fatal tokenizer failure.</summary>
    public SvgMediaQueryList? QueryList;
    /// <summary>The ordered parse diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The exact fatal failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a media-query parse result.</summary>
    /// <param name="success">Whether bounded parsing completed.</param>
    /// <param name="queryList">The parsed query list, or <c>null</c>.</param>
    /// <param name="diagnostics">The ordered diagnostics.</param>
    /// <param name="errorOffset">The fatal failure offset.</param>
    public SvgMediaQueryParseResult(bool success, SvgMediaQueryList? queryList, System.Collections.Generic.List<SvgDiagnostic> diagnostics, int errorOffset)
    {
        Success = success;
        QueryList = queryList;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>(diagnostics);
        ErrorOffset = errorOffset;
    }
}

/// <summary>Describes the media values used to evaluate stylesheets and media rules.</summary>
public class SvgStyleEnvironment
{
    /// <summary>The caller-managed environment revision.</summary>
    public long Version;
    /// <summary>The active media type, commonly screen or print.</summary>
    public string MediaType;
    /// <summary>The viewport width in CSS pixels.</summary>
    public double ViewportWidth;
    /// <summary>The viewport height in CSS pixels.</summary>
    public double ViewportHeight;
    /// <summary>The output resolution in dots per CSS inch.</summary>
    public double ResolutionDpi;
    /// <summary>The number of bits per color component.</summary>
    public int ColorBits;
    /// <summary>The number of monochrome bits per pixel.</summary>
    public int MonochromeBits;
    /// <summary>Whether the user requests reduced motion.</summary>
    public bool ReducedMotion;
    /// <summary>The root font size in CSS pixels for em and rem media values.</summary>
    public double RootFontSize;

    /// <summary>Creates a conventional screen style environment.</summary>
    public SvgStyleEnvironment()
    {
        Version = 0;
        MediaType = "screen";
        ViewportWidth = 0;
        ViewportHeight = 0;
        ResolutionDpi = 96;
        ColorBits = 8;
        MonochromeBits = 0;
        ReducedMotion = false;
        RootFontSize = 16;
    }

    /// <summary>Marks caller-managed media or viewport state changed.</summary>
    /// <returns>The incremented environment version.</returns>
    /// <exception cref="System.OverflowException">Thrown when the version cannot be incremented.</exception>
    internal long MarkChanged()
    {
        Version = Version + 1;
        return Version;
    }

    /// <summary>Tests media query text against this environment.</summary>
    /// <param name="queryText">The complete media query list.</param>
    /// <param name="limits">The CSS token and character limits.</param>
    /// <returns><c>true</c> when a valid query matches.</returns>
    internal bool Matches(string queryText, SvgCssLimits limits)
    {
        SvgMediaQueryParseResult parsed = new SvgMediaQueryParser().ParseWithValueAndLimits(queryText, limits);
        return parsed.Success && parsed.QueryList != null && new SvgMediaQueryEvaluator().Matches(parsed.QueryList, this);
    }
}
