namespace svg;

/// <summary>Describes one exact source-associated sanitizer output change.</summary>
public class SvgSanitizationChange
{
    /// <summary>The stable change kind.</summary>
    public SvgSanitizationChangeKind Kind;
    /// <summary>The affected node qualified name, or an empty string.</summary>
    public string NodeName;
    /// <summary>The affected attribute qualified name, or an empty string.</summary>
    public string AttributeName;
    /// <summary>The exact value or text before the change, or an empty string.</summary>
    public string OldValue;
    /// <summary>The exact value or text after the change, or an empty string.</summary>
    public string NewValue;
    /// <summary>The stable reason for the change.</summary>
    public string Reason;
    /// <summary>The authored source span associated with the change, or <c>null</c> for generated content.</summary>
    public SvgSourceSpan? Source;

    internal SvgSanitizationChange(SvgSanitizationChangeKind kind, string nodeName, string attributeName, string oldValue, string newValue, string reason, SvgSourceSpan? source)
    {
        Kind = kind;
        NodeName = nodeName;
        AttributeName = attributeName;
        OldValue = oldValue;
        NewValue = newValue;
        Reason = reason;
        Source = source;
    }
}

/// <summary>Defines optional allow-list restrictions layered over secure-static sanitization.</summary>
public class SvgSanitizationAllowList
{
    /// <summary>Allowed case-sensitive element local names; an empty list accepts every otherwise-safe element.</summary>
    public System.Collections.Generic.List<string> Elements;
    /// <summary>Allowed case-sensitive attribute local names; an empty list accepts every otherwise-safe attribute.</summary>
    public System.Collections.Generic.List<string> Attributes;
    /// <summary>Allowed exact element and attribute namespace URIs; an empty list adds no namespace restriction.</summary>
    public System.Collections.Generic.List<string> Namespaces;
    /// <summary>Allowed ASCII-case-insensitive CSS property names; an empty list accepts every otherwise-safe property.</summary>
    public System.Collections.Generic.List<string> CssProperties;
    /// <summary>Allowed ASCII-case-insensitive CSS value function names; an empty list accepts every otherwise-safe function.</summary>
    public System.Collections.Generic.List<string> CssFunctions;
    /// <summary>Allowed ASCII-case-insensitive normalized URI schemes; an empty list adds no scheme restriction.</summary>
    public System.Collections.Generic.List<string> UriSchemes;
    /// <summary>Allowed ASCII-case-insensitive normalized HTTP origins; an empty list adds no origin restriction.</summary>
    public System.Collections.Generic.List<string> Origins;
    /// <summary>Allowed ASCII-case-insensitive resource media types; an empty list adds no media-type restriction.</summary>
    public System.Collections.Generic.List<string> MediaTypes;
    /// <summary>Whether non-script navigation link targets may remain.</summary>
    public bool Links;
    /// <summary>Whether SVG metadata elements may remain.</summary>
    public bool Metadata;
    /// <summary>Whether otherwise-safe data URLs may remain.</summary>
    public bool DataUrls;

    /// <summary>Creates allow-list defaults that preserve all content accepted by secure-static sanitization.</summary>
    public SvgSanitizationAllowList()
    {
        Elements = new System.Collections.Generic.List<string>();
        Attributes = new System.Collections.Generic.List<string>();
        Namespaces = new System.Collections.Generic.List<string>();
        CssProperties = new System.Collections.Generic.List<string>();
        CssFunctions = new System.Collections.Generic.List<string>();
        UriSchemes = new System.Collections.Generic.List<string>();
        Origins = new System.Collections.Generic.List<string>();
        MediaTypes = new System.Collections.Generic.List<string>();
        Links = true;
        Metadata = true;
        DataUrls = true;
    }
}

/// <summary>Configures bounded secure-static SVG sanitization.</summary>
public class SvgSanitizationOptions
{
    /// <summary>The positive maximum source nodes inspected.</summary>
    public int MaximumNodes;
    /// <summary>The positive maximum retained sanitization diagnostics.</summary>
    public int MaximumDiagnostics;
    /// <summary>The CSS parser limits used before retaining authored declarations and rules.</summary>
    public SvgCssLimits CssLimits;
    /// <summary>The URI normalization limits used for authored references.</summary>
    public SvgReferenceResolveOptions References;
    /// <summary>The resource policy applied to images, fonts, stylesheets, and referenced documents.</summary>
    public SvgResourcePolicy ResourcePolicy;
    /// <summary>Whether normalized non-script external navigation links may remain.</summary>
    public bool AllowExternalLinks;
    /// <summary>Optional content restrictions layered over the secure-static defaults.</summary>
    public SvgSanitizationAllowList AllowList;

    /// <summary>Creates secure-static defaults with no external acquisition or navigation.</summary>
    public SvgSanitizationOptions()
    {
        MaximumNodes = 1000000;
        MaximumDiagnostics = 100000;
        CssLimits = new SvgCssLimits();
        References = new SvgReferenceResolveOptions();
        ResourcePolicy = SecureResourcePolicy();
        AllowExternalLinks = false;
        AllowList = new SvgSanitizationAllowList();
    }

    internal static SvgResourcePolicy SecureResourcePolicy()
    {
        SvgResourcePolicy result = new SvgResourcePolicy();
        result.AllowImportedStyleSheets = false;
        result.AllowRelativeImportUris = false;
        result.AllowDataImportUris = false;
        result.AllowFileImportUris = false;
        result.AllowCrossOriginImports = false;
        result.AllowRelativeUris = false;
        result.AllowFileUris = false;
        result.AllowRemoteFileUris = false;
        result.AllowHttpUris = false;
        result.AllowHttpsUris = false;
        result.AllowCrossOriginUris = false;
        result.AllowRedirects = false;
        result.AllowCrossOriginRedirects = false;
        result.AllowCustomSchemes = false;
        return result;
    }
}

/// <summary>Contains an independent secure-static document and bounded removal summary.</summary>
public class SvgSanitizationResult
{
    /// <summary>The source document that was inspected without mutation.</summary>
    public SvgDocument? SourceDocument;
    /// <summary>The source version captured before sanitization.</summary>
    public long SourceVersion;
    /// <summary>The independent sanitized document, or <c>null</c> after invalid arguments.</summary>
    public SvgDocument? Document;
    /// <summary>The number of removed element subtrees.</summary>
    public int RemovedNodes;
    /// <summary>The number of removed XML attributes.</summary>
    public int RemovedAttributes;
    /// <summary>The number of removed top-level CSS rules.</summary>
    public int RemovedCssRules;
    /// <summary>The number of removed inline CSS declarations or recovered fragments.</summary>
    public int RemovedCssDeclarations;
    /// <summary>Whether the sanitized output differs semantically from the source.</summary>
    public bool Changed;
    /// <summary>The stable operational failure.</summary>
    public SvgSanitizationError Error;
    /// <summary>Bounded source-located removal and failure diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The exact deterministic source-associated output changes in mutation order.</summary>
    public System.Collections.Generic.List<SvgSanitizationChange> Changes;
    /// <summary>Whether bounded sanitization completed.</summary>
    public bool Success;

    internal SvgSanitizationResult(SvgDocument? source)
    {
        SourceDocument = source;
        SourceVersion = source == null ? 0 : source.Version;
        Document = null;
        RemovedNodes = 0;
        RemovedAttributes = 0;
        RemovedCssRules = 0;
        RemovedCssDeclarations = 0;
        Changed = false;
        Error = SvgSanitizationError.None;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Changes = new System.Collections.Generic.List<SvgSanitizationChange>();
        Success = false;
    }

    /// <summary>Checks whether the source document retains its captured version.</summary>
    /// <returns><c>true</c> when the sanitization input snapshot remains current.</returns>
    internal bool IsCurrent()
    {
        return SourceDocument != null && SourceDocument.Version == SourceVersion;
    }
}
