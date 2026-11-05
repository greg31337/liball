using System;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifToneMap"/>.</summary>
public interface IAvifToneMap
{
    /// <summary>Returns the decoded dependency association state.</summary>
    /// <returns>A stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns the caller-owned base image.</summary>
    /// <returns>The first derived-image input.</returns>
    AvifImage? GetBaseImage();

    /// <summary>Returns the caller-owned gain-map association.</summary>
    /// <returns>The second derived-image input and metadata.</returns>
    AvifGainMap? GetGainMap();

    /// <summary>Reports whether the tone-mapped alternate precedes its base alternative.</summary>
    /// <returns><c>true</c> when automatic rendering should prefer reconstruction.</returns>
    bool IsAlternateImagePreferred();

    /// <summary>Returns the associated tone-map item identifier.</summary>
    /// <returns>The identifier, or zero for a directly constructed derivation.</returns>
    uint GetToneMapItemId();

    /// <summary>Returns the first <c>dimg</c> item identifier.</summary>
    /// <returns>The base item identifier, or zero when item relationships were not supplied.</returns>
    uint GetBaseItemId();

    /// <summary>Returns the second <c>dimg</c> item identifier.</summary>
    /// <returns>The gain-map item identifier, or zero when item relationships were not supplied.</returns>
    uint GetGainMapItemId();

    /// <summary>Renders the selected base or reconstructed alternate image.</summary>
    /// <param name="options">The tone-map policy, display headroom, color, and limits.</param>
    /// <returns>An owning linear BT.709 half-float RGBA buffer.</returns>
    AvifPixelBuffer Render(AvifRenderOptions options);
}
