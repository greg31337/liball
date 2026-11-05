namespace svg;

/// <summary>Represents one parsed supports condition node.</summary>
public class SvgSupportsCondition
{
    /// <summary>The condition kind.</summary>
    public SvgSupportsConditionKind Kind;
    /// <summary>The ordered child conditions.</summary>
    public System.Collections.Generic.List<SvgSupportsCondition> Children;
    /// <summary>The normalized declaration property name, or an empty string.</summary>
    public string PropertyName;
    /// <summary>The exact declaration value text, or an empty string.</summary>
    public string ValueText;
    /// <summary>The exact selector text, or an empty string.</summary>
    public string SelectorText;
    /// <summary>The parsed selector, or <c>null</c>.</summary>
    public SvgSelector? Selector;
    /// <summary>The exact authored condition text.</summary>
    public string RawText;
    /// <summary>The source span, or <c>null</c>.</summary>
    public SvgSourceSpan? Source;
    /// <summary>Whether the condition grammar is valid.</summary>
    public bool IsValid;

    /// <summary>Creates an empty invalid condition.</summary>
    public SvgSupportsCondition()
    {
        Kind = SvgSupportsConditionKind.Unknown;
        Children = new System.Collections.Generic.List<SvgSupportsCondition>();
        PropertyName = "";
        ValueText = "";
        SelectorText = "";
        Selector = null;
        RawText = "";
        Source = null;
        IsValid = false;
    }
}

/// <summary>Contains a complete parsed CSS supports query.</summary>
public class SvgSupportsQuery
{
    /// <summary>The root condition, or <c>null</c>.</summary>
    public SvgSupportsCondition? Condition;
    /// <summary>The complete authored query text.</summary>
    public string SourceText;
    /// <summary>The ordered parse diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether the complete query is valid.</summary>
    public bool IsValid;

    /// <summary>Creates a supports query and copies its diagnostics.</summary>
    /// <param name="condition">The root condition, or <c>null</c>.</param>
    /// <param name="sourceText">The complete source text.</param>
    /// <param name="diagnostics">The ordered diagnostics.</param>
    public SvgSupportsQuery(SvgSupportsCondition? condition, string sourceText, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        if (sourceText == null) throw new System.ArgumentNullException("sourceText");
        if (diagnostics == null) throw new System.ArgumentNullException("diagnostics");
        Condition = condition;
        SourceText = sourceText;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>(diagnostics);
        IsValid = condition != null && condition.IsValid && !HasErrors(Diagnostics);
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

/// <summary>Contains a bounded supports-query parse result.</summary>
public class SvgSupportsParseResult
{
    /// <summary>Whether tokenization and bounded parsing completed.</summary>
    public bool Success;
    /// <summary>The parsed query, or <c>null</c> after a fatal limit failure.</summary>
    public SvgSupportsQuery? Query;
    /// <summary>The ordered parse diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The exact fatal failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a supports-query parse result.</summary>
    /// <param name="success">Whether bounded parsing completed.</param>
    /// <param name="query">The parsed query, or <c>null</c>.</param>
    /// <param name="diagnostics">The ordered diagnostics.</param>
    /// <param name="errorOffset">The fatal failure offset.</param>
    public SvgSupportsParseResult(bool success, SvgSupportsQuery? query, System.Collections.Generic.List<SvgDiagnostic> diagnostics, int errorOffset)
    {
        Success = success;
        Query = query;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>(diagnostics);
        ErrorOffset = errorOffset;
    }
}

/// <summary>Provides property and capability catalogs for supports-query evaluation.</summary>
public class SvgSupportsEnvironment
{
    /// <summary>The active property support catalog.</summary>
    public SvgPropertyCatalog Properties;
    /// <summary>The active capability identifiers.</summary>
    public SvgCapabilitySet Capabilities;

    /// <summary>Creates a default environment for the currently implemented CSS parser and selectors.</summary>
    public SvgSupportsEnvironment()
    {
        Properties = SvgPropertyCatalog.CreateDefault();
        Capabilities = new SvgCapabilitySet();
        Capabilities.Add(SvgFeatureIdentifiers.CssParsing);
        Capabilities.Add(SvgFeatureIdentifiers.CssSelectors);
    }

    /// <summary>Parses and evaluates supports-query text.</summary>
    /// <param name="queryText">The complete supports condition.</param>
    /// <param name="limits">The CSS parsing limits.</param>
    /// <returns><c>true</c> when the valid condition evaluates true.</returns>
    internal bool Matches(string queryText, SvgCssLimits limits)
    {
        SvgSupportsParseResult parsed = new SvgSupportsQueryParser().ParseWithValueAndLimits(queryText, limits);
        return parsed.Success && parsed.Query != null && new SvgSupportsQueryEvaluator().Matches(parsed.Query, this, limits);
    }
}
