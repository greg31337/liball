namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifGainMapMetadata"/>.</summary>
public interface IAvifGainMapMetadata
{
    /// <summary>Returns the metadata parsing and validation result.</summary>
    /// <returns>A stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns a copy of the complete versioned <c>tmap</c> metadata payload.</summary>
    /// <returns>The validated payload, or an empty array when parsing failed.</returns>
    byte[] GetData();

    /// <summary>Returns the syntax version.</summary>
    /// <returns>The parsed version, currently zero.</returns>
    int GetVersion();

    /// <summary>Returns the minimum reader version required by the payload.</summary>
    /// <returns>The minimum version.</returns>
    int GetMinimumVersion();

    /// <summary>Returns the writer metadata version.</summary>
    /// <returns>The writer version.</returns>
    int GetWriterVersion();

    /// <summary>Reports whether distinct parameters are present for three color channels.</summary>
    /// <returns><c>true</c> for multichannel metadata.</returns>
    bool IsMultichannel();

    /// <summary>Reports whether gain-map math uses the base image color space.</summary>
    /// <returns><c>true</c> for the base color space; otherwise the alternate color space.</returns>
    bool UsesBaseColorSpace();

    /// <summary>Returns the base-image log2 HDR headroom.</summary>
    /// <returns>The log2-encoded headroom.</returns>
    double GetBaseHdrHeadroom();

    /// <summary>Returns the alternate-image log2 HDR headroom.</summary>
    /// <returns>The log2-encoded headroom.</returns>
    double GetAlternateHdrHeadroom();

    /// <summary>Returns the base-image linear HDR headroom ratio.</summary>
    /// <returns>Two raised to the log2 headroom.</returns>
    double GetBaseHdrHeadroomRatio();

    /// <summary>Returns the alternate-image linear HDR headroom ratio.</summary>
    /// <returns>Two raised to the log2 headroom.</returns>
    double GetAlternateHdrHeadroomRatio();

    /// <summary>Returns one channel's minimum log2 gain.</summary>
    /// <param name="channel">The zero-based red, green, or blue channel.</param>
    /// <returns>The minimum gain, or zero for an invalid channel.</returns>
    double GetGainMapMinimum(int channel);

    /// <summary>Returns one channel's maximum log2 gain.</summary>
    /// <param name="channel">The zero-based red, green, or blue channel.</param>
    /// <returns>The maximum gain, or zero for an invalid channel.</returns>
    double GetGainMapMaximum(int channel);

    /// <summary>Returns one channel's positive encoding gamma.</summary>
    /// <param name="channel">The zero-based red, green, or blue channel.</param>
    /// <returns>The gamma, or zero for an invalid channel.</returns>
    double GetGamma(int channel);

    /// <summary>Returns one channel's base-image linear offset.</summary>
    /// <param name="channel">The zero-based red, green, or blue channel.</param>
    /// <returns>The base offset, or zero for an invalid channel.</returns>
    double GetBaseOffset(int channel);

    /// <summary>Returns one channel's alternate-image linear offset.</summary>
    /// <param name="channel">The zero-based red, green, or blue channel.</param>
    /// <returns>The alternate offset, or zero for an invalid channel.</returns>
    double GetAlternateOffset(int channel);

    /// <summary>Computes the signed gain-map weight for a display headroom ratio.</summary>
    /// <param name="displayHeadroom">The positive ratio relative to reference white.</param>
    /// <returns>A value from minus one through one, or zero for invalid metadata or headroom.</returns>
    double GetWeight(double displayHeadroom);
}
