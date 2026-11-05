namespace svg;

/// <summary>Configures profile-aware bounded SVG structural validation.</summary>
public class SvgValidationOptions
{
    /// <summary>The conformance profile applied to recognized and legacy content.</summary>
    public SvgValidationProfile Profile;
    /// <summary>The positive maximum nodes visited.</summary>
    public int MaximumNodes;
    /// <summary>The positive maximum conformance issues retained.</summary>
    public int MaximumIssues;
    /// <summary>The current SVG element catalog.</summary>
    public SvgElementCatalog Elements;
    /// <summary>The SVG 1.1 compatibility catalog.</summary>
    public SvgLegacyCatalog Legacy;
    /// <summary>The typed-value catalogs and parser limits.</summary>
    public SvgTypedAttributeOptions TypedValues;
    /// <summary>The positive maximum timing or interpolation items in one list.</summary>
    public int MaximumTimingItems;
    /// <summary>The CSS catalogs, parser bounds, selector environment, and computed-style options.</summary>
    public SvgStyleResolverOptions Css;
    /// <summary>The URI and base-depth bounds used for authored references.</summary>
    public SvgReferenceResolveOptions References;
    /// <summary>The href-chain, use-expansion, and effect-recursion bounds.</summary>
    public SvgReferenceLimits ReferenceLimits;
    /// <summary>Optional already-acquired resources whose declared and actual media metadata is validated.</summary>
    public System.Collections.Generic.List<SvgResourceAcquisitionResult> Resources;
    /// <summary>The policy applied to authored external imports, images, fonts, and embedded resources.</summary>
    public SvgResourcePolicy ResourcePolicy;
    /// <summary>The positive maximum authored CSS and presentation declaration count.</summary>
    public int MaximumAuthoredDeclarations;
    /// <summary>The bounds used while resolving accessible names and descriptions.</summary>
    public SvgAccessibilityOptions Accessibility;

    /// <summary>Creates strict SVG 2 validation defaults.</summary>
    public SvgValidationOptions()
    {
        Profile = SvgValidationProfile.Svg2;
        MaximumNodes = 1000000;
        MaximumIssues = 100000;
        Elements = new SvgCatalogFactory().CreateElementCatalog();
        Legacy = new SvgCatalogFactory().CreateLegacyCatalog();
        TypedValues = new SvgTypedAttributeOptions();
        MaximumTimingItems = 1000000;
        Css = new SvgStyleResolverOptions();
        References = new SvgReferenceResolveOptions();
        ReferenceLimits = new SvgReferenceLimits();
        Resources = new System.Collections.Generic.List<SvgResourceAcquisitionResult>();
        ResourcePolicy = new SvgResourcePolicy();
        MaximumAuthoredDeclarations = Css.Limits.MaxDeclarations;
        Accessibility = new SvgAccessibilityOptions();
    }
}

/// <summary>Contains one located structural conformance issue.</summary>
public class SvgValidationIssue
{
    /// <summary>The violated structural rule.</summary>
    public SvgValidationRule Rule;
    /// <summary>The containing or offending source node.</summary>
    public SvgNode? Node;
    /// <summary>The offending attribute, or <c>null</c>.</summary>
    public SvgAttribute? Attribute;
    /// <summary>The selected validation profile.</summary>
    public SvgValidationProfile Profile;
    /// <summary>The located stable diagnostic.</summary>
    public SvgDiagnostic Diagnostic;
    /// <summary>The deterministic compatible-mode recovery, or an empty string when no recovery applies.</summary>
    public string RecoveryDescription;

    internal SvgValidationIssue(SvgValidationRule rule, SvgNode? node, SvgAttribute? attribute, SvgValidationProfile profile, SvgDiagnostic diagnostic)
    {
        Rule = rule;
        Node = node;
        Attribute = attribute;
        Profile = profile;
        Diagnostic = diagnostic;
        RecoveryDescription = "";
    }
}

/// <summary>Contains deterministic profile-aware structural validation results.</summary>
public class SvgValidationResult
{
    /// <summary>The validated source document.</summary>
    public SvgDocument? Document;
    /// <summary>The source document version captured during validation.</summary>
    public long SourceVersion;
    /// <summary>The selected validation profile.</summary>
    public SvgValidationProfile Profile;
    /// <summary>The conformance issues in document and attribute order.</summary>
    public System.Collections.Generic.List<SvgValidationIssue> Issues;
    /// <summary>The issue diagnostics in the same order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The number of nodes inspected.</summary>
    public int VisitedNodeCount;
    /// <summary>The number of informational issues.</summary>
    public int InformationCount;
    /// <summary>The number of warning issues.</summary>
    public int WarningCount;
    /// <summary>The number of error issues.</summary>
    public int ErrorCount;
    /// <summary>The stable operational failure.</summary>
    public SvgValidationError Error;
    /// <summary>Whether bounded validation completed.</summary>
    public bool Success;
    /// <summary>Whether validation completed without conformance errors.</summary>
    public bool Valid;

    internal SvgValidationResult(SvgDocument? document, SvgValidationProfile profile)
    {
        Document = document;
        SourceVersion = document == null ? 0 : document.Version;
        Profile = profile;
        Issues = new System.Collections.Generic.List<SvgValidationIssue>();
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        VisitedNodeCount = 0;
        InformationCount = 0;
        WarningCount = 0;
        ErrorCount = 0;
        Error = SvgValidationError.None;
        Success = false;
        Valid = false;
    }

    /// <summary>Returns issues matching one structural rule.</summary>
    /// <param name="rule">The exact validation rule.</param>
    /// <returns>A new issue list in deterministic source order.</returns>
    internal System.Collections.Generic.List<SvgValidationIssue> FindIssues(SvgValidationRule rule)
    {
        System.Collections.Generic.List<SvgValidationIssue> result = new System.Collections.Generic.List<SvgValidationIssue>();
        int index = 0;
        while (index < Issues.Count)
        {
            if (Issues[index].Rule == rule) result.Add(Issues[index]);
            index = index + 1;
        }
        return result;
    }

    /// <summary>Checks whether the validated document retains its captured version.</summary>
    /// <returns><c>true</c> when this validation snapshot remains current.</returns>
    internal bool IsCurrent()
    {
        return Document != null && Document.Version == SourceVersion;
    }
}
