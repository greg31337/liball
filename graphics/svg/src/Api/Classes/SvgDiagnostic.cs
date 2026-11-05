namespace svg;

/// <summary>Maps diagnostic enum values to stable external code text without reflection.</summary>
public class SvgDiagnosticCodeNames : ISvgDiagnosticCodeNames
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgDiagnosticCodeNames()
    {
    }

    /// <summary>Returns stable external text for a diagnostic code.</summary>
    /// <param name="code">The diagnostic code.</param>
    /// <returns>The stable <c>SVG</c>-prefixed code text.</returns>
    public string Get(SvgDiagnosticCode code)
    {
        switch (code)
        {
            case SvgDiagnosticCode.None:
                return "SVG0000";
            case SvgDiagnosticCode.DiagnosticsTruncated:
                return "SVG0001";
            case SvgDiagnosticCode.InvalidArgument:
                return "SVG0002";
            case SvgDiagnosticCode.InputLimitExceeded:
                return "SVG0003";
            case SvgDiagnosticCode.MalformedXml:
                return "SVG0100";
            case SvgDiagnosticCode.XmlNamespace:
                return "SVG0101";
            case SvgDiagnosticCode.InvalidSvgRoot:
                return "SVG0102";
            case SvgDiagnosticCode.InvalidContainer:
                return "SVG0103";
            case SvgDiagnosticCode.UnsupportedEncoding:
                return "SVG0104";
            case SvgDiagnosticCode.DuplicateId:
                return "SVG0105";
            case SvgDiagnosticCode.InvalidValue:
                return "SVG0200";
            case SvgDiagnosticCode.CompatibilityRecovery:
                return "SVG0201";
            case SvgDiagnosticCode.InvalidCss:
                return "SVG0300";
            case SvgDiagnosticCode.InvalidReference:
                return "SVG0400";
            case SvgDiagnosticCode.ReferenceCycle:
                return "SVG0401";
            case SvgDiagnosticCode.ResourceDenied:
                return "SVG0500";
            case SvgDiagnosticCode.ResourceFailed:
                return "SVG0501";
            case SvgDiagnosticCode.UnsupportedFeature:
                return "SVG0600";
            case SvgDiagnosticCode.PreservedFeature:
                return "SVG0601";
            case SvgDiagnosticCode.CallbackRequired:
                return "SVG0602";
            case SvgDiagnosticCode.InvalidFont:
                return "SVG0603";
            case SvgDiagnosticCode.LayoutFailed:
                return "SVG0700";
            case SvgDiagnosticCode.RenderingFailed:
                return "SVG0800";
            case SvgDiagnosticCode.LossyChange:
                return "SVG0900";
            case SvgDiagnosticCode.PatchConflict:
                return "SVG0902";
            case SvgDiagnosticCode.Cancelled:
                return "SVG0901";
            case SvgDiagnosticCode.InternalInvariant:
                return "SVG9999";
            default:
                return "SVG9999";
        }
    }
}
