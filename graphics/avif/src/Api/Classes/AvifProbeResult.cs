namespace avif;

/// <summary>Contains the bounded file-type probe result for a possible AVIF source.</summary>
public class AvifProbeResult : IAvifProbeResult, IAvifDiagnosticSource
{
    AvifResultCode code;
    AvifCapabilityReport capabilities;

    /// <summary>Creates a probe result without declared capabilities.</summary>
    /// <param name="code">The result code represented by the test or adapter result.</param>
    /// <remarks>This constructor is primarily useful for implementations of <see cref="IAvifCodec"/>.</remarks>
    public AvifProbeResult(AvifResultCode code)
    {
        this.code = code;
        capabilities = new AvifCapabilityReport(null, null);
    }

    internal static AvifProbeResult Create(
        AvifResultCode code,
        BrandCapabilityReport? capabilities)
    {
        AvifProbeResult result = new AvifProbeResult(code);
        result.capabilities = new AvifCapabilityReport(capabilities, null);
        return result;
    }

    /// <summary>Returns the probe outcome.</summary>
    /// <returns>Success, NeedMoreData, NotFound, or another stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Reports whether the source declares an AVIF brand.</summary>
    /// <returns><c>true</c> only for a successful AVIF probe.</returns>
    public bool IsAvif()
    {
        return code == AvifResultCode.Success && capabilities.IsAvif();
    }

    /// <summary>Returns the capabilities inferred from available file-type brands.</summary>
    /// <returns>The immutable capability report.</returns>
    public AvifCapabilityReport GetCapabilities()
    {
        return capabilities;
    }

    /// <summary>Returns diagnostics for the probe operation.</summary>
    /// <returns>The structured probe diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromCode(
            code,
            AvifDiagnosticCategory.Container,
            "The AVIF file-type probe did not complete successfully.");
    }
}
