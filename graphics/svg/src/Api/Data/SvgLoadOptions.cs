namespace svg;

/// <summary>Configures synchronous SVG source acquisition and later parsing stages.</summary>
public class SvgLoadOptions
{
    /// <summary>The requested processing mode.</summary>
    public SvgProcessingMode Mode;
    /// <summary>The caller-provided base URI, or an empty string.</summary>
    public string BaseUri;
    /// <summary>The caller-provided content type, or an empty string.</summary>
    public string ContentType;
    /// <summary>The complete security limits.</summary>
    public SvgSecurityLimits Limits;
    /// <summary>The explicit root and nested resource policy.</summary>
    public SvgResourcePolicy ResourcePolicy;
    /// <summary>The optional synchronous external resource provider.</summary>
    public ISvgResourceProvider? ResourceProvider;
    /// <summary>The synchronous cancellation token.</summary>
    public System.Threading.CancellationToken CancellationToken;
    /// <summary>The diagnostic stop behavior used by later parsing stages.</summary>
    public SvgDiagnosticBehavior DiagnosticBehavior;
    /// <summary>Whether source text and source locations should be retained.</summary>
    public bool PreserveSource;
    /// <summary>Whether unknown well-formed content should be retained.</summary>
    public bool PreserveUnknown;
    /// <summary>Whether caller byte arrays and callback byte arrays are copied before staging.</summary>
    public bool CopyInputData;
    /// <summary>The document type declaration policy.</summary>
    public SvgDtdPolicy DtdPolicy;

    /// <summary>Creates secure, source-preserving defaults.</summary>
    public SvgLoadOptions()
    {
        Mode = SvgProcessingMode.SecureStatic;
        BaseUri = "";
        ContentType = "";
        Limits = new SvgSecurityLimits();
        ResourcePolicy = new SvgResourcePolicy();
        ResourceProvider = null;
        CancellationToken = System.Threading.CancellationToken.None;
        DiagnosticBehavior = SvgDiagnosticBehavior.Strict;
        PreserveSource = true;
        PreserveUnknown = true;
        CopyInputData = true;
        DtdPolicy = SvgDtdPolicy.Prohibit;
    }
}
