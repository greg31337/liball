using System;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifGainMap"/>.</summary>
public interface IAvifGainMap
{
    /// <summary>Returns the association validation result.</summary>
    /// <returns>A stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns the caller-owned decoded gain-map image.</summary>
    /// <returns>The decoded gain-map samples.</returns>
    AvifImage? GetImage();

    /// <summary>Returns the parsed gain-map metadata.</summary>
    /// <returns>The immutable metadata object supplied at construction.</returns>
    AvifGainMapMetadata? GetMetadata();

    /// <summary>Returns a copy of the alternate-image color information.</summary>
    /// <returns>The alternate CICP information, or null when unavailable.</returns>
    AvifColorInfo? GetAlternateColorInfo();

    /// <summary>Sets the gain-map encoding quality override.</summary>
    /// <param name="quality">Zero through one hundred, or negative one to inherit.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetQuality(int quality);

    /// <summary>Returns the gain-map quality override.</summary>
    /// <returns>Zero through one hundred, or negative one to inherit.</returns>
    int GetQuality();
}
