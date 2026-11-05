namespace svg;

/// <summary>Configures complete document resolution from the editable tree through current semantic stages.</summary>
public class SvgResolveOptions
{
    /// <summary>The fallback host viewport width in CSS pixels.</summary>
    public double ViewportWidth;
    /// <summary>The fallback host viewport height in CSS pixels.</summary>
    public double ViewportHeight;
    /// <summary>The optional SVG fragment identifier, with or without a leading number sign.</summary>
    public string FragmentIdentifier;
    /// <summary>The ordered host language preferences, most preferred first.</summary>
    public System.Collections.Generic.List<string> LanguagePreferences;
    /// <summary>The exact SVG 1.1 feature strings supported by the host.</summary>
    public SvgCapabilitySet Capabilities;
    /// <summary>The exact extension IRIs supported by the host.</summary>
    public SvgCapabilitySet Extensions;
    /// <summary>The caller-provided stylesheets appended after embedded stylesheets.</summary>
    public System.Collections.Generic.List<SvgStyleSheet> CallerStyleSheets;
    /// <summary>The style environment and computed-style cache configuration.</summary>
    public SvgStyleResolverOptions StyleOptions;
    /// <summary>The policy applied before caller-mediated resource acquisition.</summary>
    public SvgResourcePolicy ResourcePolicy;
    /// <summary>The optional synchronous external resource provider.</summary>
    public ISvgResourceProvider? ResourceProvider;
    /// <summary>The animation sample time in seconds reserved for animation stages.</summary>
    public double AnimationTime;
    /// <summary>The complete processing security limits.</summary>
    public SvgSecurityLimits Limits;
    /// <summary>The diagnostic stop behavior used by resolution stages.</summary>
    public SvgDiagnosticBehavior DiagnosticBehavior;
    /// <summary>The maximum retained resolved-document diagnostic count.</summary>
    public int MaxDiagnostics;
    /// <summary>The maximum resolved documents retained by one resolver.</summary>
    public int MaxCacheEntries;
    /// <summary>Whether decoded image payloads are available to embedded-content resolution.</summary>
    public bool ImageContentAvailable;
    /// <summary>Whether the host can process foreignObject content.</summary>
    public bool ForeignContentCallbackAvailable;
    /// <summary>The optional synchronous bounded embedded host-content callback.</summary>
    public System.Func<SvgHostContentCallbackRequest, SvgHostContentCallbackResult>? HostContentCallback;
    /// <summary>Optional image intrinsic dimensions shared by the current resolution request.</summary>
    public SvgIntrinsicSize? ImageIntrinsicSize;
    /// <summary>An optional caller-loaded document used for external references.</summary>
    public SvgDocument? ExternalDocument;
    /// <summary>The synchronous cancellation token.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates secure deterministic document-resolution defaults.</summary>
    public SvgResolveOptions()
    {
        ViewportWidth = 300;
        ViewportHeight = 150;
        FragmentIdentifier = "";
        LanguagePreferences = new System.Collections.Generic.List<string>();
        Capabilities = new SvgCapabilitySet();
        Extensions = new SvgCapabilitySet();
        CallerStyleSheets = new System.Collections.Generic.List<SvgStyleSheet>();
        StyleOptions = new SvgStyleResolverOptions();
        StyleOptions.StyleEnvironment.ViewportWidth = ViewportWidth;
        StyleOptions.StyleEnvironment.ViewportHeight = ViewportHeight;
        ResourcePolicy = new SvgResourcePolicy();
        ResourceProvider = null;
        AnimationTime = 0;
        Limits = new SvgSecurityLimits();
        DiagnosticBehavior = SvgDiagnosticBehavior.Compatible;
        MaxDiagnostics = 10000;
        MaxCacheEntries = 8;
        ImageContentAvailable = false;
        ForeignContentCallbackAvailable = false;
        HostContentCallback = null;
        ImageIntrinsicSize = null;
        ExternalDocument = null;
        CancellationToken = System.Threading.CancellationToken.None;
    }
}
