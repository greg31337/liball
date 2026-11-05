namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifProbeResult"/>.</summary>
public interface IAvifProbeResult
{
    /// <summary>Returns the probe outcome.</summary>
    /// <returns>Success, NeedMoreData, NotFound, or another stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Reports whether the source declares an AVIF brand.</summary>
    /// <returns><c>true</c> only for a successful AVIF probe.</returns>
    bool IsAvif();

    /// <summary>Returns the capabilities inferred from available file-type brands.</summary>
    /// <returns>The immutable capability report.</returns>
    AvifCapabilityReport GetCapabilities();
}
