using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifItemProperty"/>.</summary>
public interface IAvifItemProperty
{
    /// <summary>Returns the one-based property-container index.</summary>
    /// <returns>The property index.</returns>
    int GetIndex();

    /// <summary>Returns the four-character property type.</summary>
    /// <returns>The property type.</returns>
    string GetTypeName();

    /// <summary>Reports whether the association marks the property essential.</summary>
    /// <returns><c>true</c> when the property is essential.</returns>
    bool IsEssential();

    /// <summary>Reports whether this property version has parsed semantics.</summary>
    /// <returns><c>true</c> when the property semantics are available.</returns>
    bool IsKnown();

    /// <summary>Returns a copy of the complete property box bytes.</summary>
    /// <returns>The retained property bytes.</returns>
    byte[] GetRawData();

    /// <summary>Returns a deterministic short semantic description.</summary>
    /// <returns>The property summary.</returns>
    string GetSummary();

    /// <summary>Returns the AV1 sequence profile.</summary>
    /// <returns>The profile, or minus one when unavailable.</returns>
    int GetAv1Profile();

    /// <summary>Returns the AV1 sequence level index.</summary>
    /// <returns>The level index, or minus one when unavailable.</returns>
    int GetAv1LevelIndex();

    /// <summary>Returns the AV1 sequence tier.</summary>
    /// <returns>The tier, or minus one when unavailable.</returns>
    int GetAv1Tier();

    /// <summary>Returns the configured AV1 bit depth.</summary>
    /// <returns>The bit depth, or minus one when unavailable.</returns>
    int GetBitDepth();

    /// <summary>Returns the configured AV1 channel count.</summary>
    /// <returns>The channel count, or minus one when unavailable.</returns>
    int GetChannelCount();

    /// <summary>Returns the parsed spatial width.</summary>
    /// <returns>The width, or zero when unavailable.</returns>
    uint GetImageWidth();

    /// <summary>Returns the parsed spatial height.</summary>
    /// <returns>The height, or zero when unavailable.</returns>
    uint GetImageHeight();

    /// <summary>Returns the number of pixel-information channels.</summary>
    /// <returns>The channel count.</returns>
    int GetPixelChannelCount();

    /// <summary>Returns one pixel-information channel bit depth.</summary>
    /// <param name="indexValue">The zero-based channel index.</param>
    /// <returns>The bit depth, or zero for an invalid index.</returns>
    int GetPixelChannelBitDepth(int indexValue);

    /// <summary>Returns the horizontal pixel-aspect spacing.</summary>
    /// <returns>The spacing, or zero when unavailable.</returns>
    uint GetHorizontalSpacing();

    /// <summary>Returns the vertical pixel-aspect spacing.</summary>
    /// <returns>The spacing, or zero when unavailable.</returns>
    uint GetVerticalSpacing();

    /// <summary>Returns the counter-clockwise rotation in degrees.</summary>
    /// <returns>The rotation, or minus one when unavailable.</returns>
    int GetRotationDegrees();

    /// <summary>Returns the mirror axis value.</summary>
    /// <returns>The axis, or minus one when unavailable.</returns>
    int GetMirrorAxis();

    /// <summary>Returns the auxiliary type identifier.</summary>
    /// <returns>The auxiliary type, or an empty string.</returns>
    string GetAuxiliaryType();

    /// <summary>Returns the selected AV1 operating-point index.</summary>
    /// <returns>The index, or minus one when unavailable.</returns>
    int GetOperatingPointIndex();

    /// <summary>Returns the selected layer identifier.</summary>
    /// <returns>The layer identifier, or minus one when unavailable.</returns>
    int GetLayerId();

    /// <summary>Returns the progressive layer count from an AV1 layered-image index.</summary>
    /// <returns>One through four, or zero when unavailable.</returns>
    int GetLayerCount();

    /// <summary>Reports whether the layered-image index uses 32-bit sizes.</summary>
    /// <returns><c>true</c> when indexed sizes use 32 bits.</returns>
    bool UsesLargeLayerSizes();

    /// <summary>Returns one indexed layer-segment size.</summary>
    /// <param name="index">The zero-based segment index before the final layer.</param>
    /// <returns>The segment size, or zero for an invalid index.</returns>
    uint GetLayerSize(int index);

    /// <summary>Returns the HDR and viewing metadata carried by this property.</summary>
    /// <returns>A metadata copy, which is empty for unrelated properties.</returns>
    AvifHdrMetadata GetHdrMetadata();

    /// <summary>Returns transform metadata carried by this property.</summary>
    /// <returns>A transform copy, which is an identity for unrelated properties.</returns>
    AvifTransform GetTransform();
}
