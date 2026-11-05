using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace avif;

/// <summary>
/// Describes one item-property association and selected parsed semantic values.
/// </summary>
public class AvifItemProperty : IAvifItemProperty
{
    int index;
    string type;
    bool essential;
    bool known;
    byte[] rawData;
    string summary;
    int av1Profile;
    int av1Level;
    int av1Tier;
    int bitDepth;
    int channelCount;
    uint imageWidth;
    uint imageHeight;
    List<int> pixelBitDepths;
    uint horizontalSpacing;
    uint verticalSpacing;
    int rotationDegrees;
    int mirrorAxis;
    string auxiliaryType;
    int operatingPointIndex;
    int layerId;
    bool largeLayerSizes;
    uint[] layerSizes;
    int layerCount;
    AvifHdrMetadata hdrMetadata;
    AvifTransform transform;

    internal AvifItemProperty(ParsedPropertyAssociation? association)
    {
        ParsedItemProperty property = (association ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty();
        index = association.GetPropertyIndex();
        type = property.GetBoxType();
        essential = association.IsEssential();
        known = AvifContainerValidator.HasSupportedPropertySemantics(property);
        rawData = property.GetRawData();
        summary = string.Empty;
        av1Profile = -1;
        av1Level = -1;
        av1Tier = -1;
        bitDepth = -1;
        channelCount = -1;
        imageWidth = 0;
        imageHeight = 0;
        pixelBitDepths = new List<int>();
        horizontalSpacing = 0;
        verticalSpacing = 0;
        rotationDegrees = -1;
        mirrorAxis = -1;
        auxiliaryType = string.Empty;
        operatingPointIndex = -1;
        layerId = -1;
        largeLayerSizes = false;
        layerSizes = System.Array.Empty<uint>();
        layerCount = 0;
        hdrMetadata = HdrMetadataParser.ParseContainerProperty(property);
        transform = AvifTransform.FromProperty(property);
        ReadSemantics(property);
        summary = CreateSummary(property);
    }

    internal void ReadSemantics(ParsedItemProperty property)
    {
        ParsedAv1Configuration? configuration = property.GetAv1Configuration();
        if (configuration != null)
        {
            av1Profile = configuration.GetSequenceProfile();
            av1Level = configuration.GetSequenceLevelIndex();
            av1Tier = configuration.GetSequenceTier();
            bitDepth = configuration.GetBitDepth();
            channelCount = configuration.GetChannelCount();
        }

        ParsedImageSpatialExtents? extents = property.GetSpatialExtents();
        if (extents != null)
        {
            imageWidth = extents.GetWidth();
            imageHeight = extents.GetHeight();
        }

        ParsedPixelInformation? pixels = property.GetPixelInformation();
        if (pixels != null)
        {
            int pixelIndex = 0;
            while (pixelIndex < pixels.GetChannelCount())
            {
                pixelBitDepths.Add((pixels.GetChannel(pixelIndex) ?? throw new InvalidOperationException("Invalid internal state.")).GetBitDepth());
                pixelIndex++;
            }
        }

        ParsedPixelAspectRatio? ratio = property.GetPixelAspectRatio();
        if (ratio != null)
        {
            horizontalSpacing = ratio.GetHorizontalSpacing();
            verticalSpacing = ratio.GetVerticalSpacing();
        }

        ParsedImageRotation? rotation = property.GetImageRotation();
        if (rotation != null)
        {
            rotationDegrees = rotation.GetDegreesCounterClockwise();
        }

        ParsedImageMirror? mirror = property.GetImageMirror();
        if (mirror != null)
        {
            mirrorAxis = mirror.GetAxis();
        }

        ParsedAuxiliaryType? auxiliary = property.GetAuxiliaryType();
        if (auxiliary != null)
        {
            auxiliaryType = auxiliary.GetTypeValue();
        }

        ParsedOperatingPointSelector? operatingPoint = property.GetOperatingPointSelector();
        if (operatingPoint != null)
        {
            operatingPointIndex = operatingPoint.GetOperatingPointIndex();
        }

        ParsedLayerSelector? layer = property.GetLayerSelector();
        if (layer != null)
        {
            layerId = layer.GetLayerId();
        }


        ParsedAv1LayeredImageIndex? layered =
            property.GetLayeredImageIndex();
        if (layered != null)
        {
            largeLayerSizes = layered.UsesLargeSizes();
            layerCount = layered.GetLayerCount();
            layerSizes = new uint[layerCount - 1];
            int index = 0;
            while (index < layerSizes.Length)
            {
                layerSizes[index] = layered.GetLayerSize(index);
                index++;
            }
        }
    }

    internal string CreateSummary(ParsedItemProperty property)
    {
        StringBuilder builder = new StringBuilder();
        if (!known)
        {
            return "opaque";
        }

        if (av1Profile >= 0)
        {
            builder.Append("profile=");
            builder.Append(av1Profile.ToString(CultureInfo.InvariantCulture));
            builder.Append(" level=");
            builder.Append(av1Level.ToString(CultureInfo.InvariantCulture));
            builder.Append(" tier=");
            builder.Append(av1Tier.ToString(CultureInfo.InvariantCulture));
            builder.Append(" depth=");
            builder.Append(bitDepth.ToString(CultureInfo.InvariantCulture));
        }
        else if (imageWidth != 0)
        {
            builder.Append("width=");
            builder.Append(imageWidth.ToString(CultureInfo.InvariantCulture));
            builder.Append(" height=");
            builder.Append(imageHeight.ToString(CultureInfo.InvariantCulture));
        }
        else if (pixelBitDepths.Count != 0)
        {
            builder.Append("channels=");
            builder.Append(pixelBitDepths.Count.ToString(CultureInfo.InvariantCulture));
            builder.Append(" depths=");
            int indexValue = 0;
            while (indexValue < pixelBitDepths.Count)
            {
                if (indexValue != 0)
                {
                    builder.Append(',');
                }

                builder.Append(pixelBitDepths[indexValue].ToString(CultureInfo.InvariantCulture));
                indexValue++;
            }
        }
        else if (horizontalSpacing != 0)
        {
            builder.Append(horizontalSpacing.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(verticalSpacing.ToString(CultureInfo.InvariantCulture));
        }
        else if (rotationDegrees >= 0)
        {
            builder.Append("degrees=");
            builder.Append(rotationDegrees.ToString(CultureInfo.InvariantCulture));
        }
        else if (mirrorAxis >= 0)
        {
            builder.Append("axis=");
            builder.Append(mirrorAxis.ToString(CultureInfo.InvariantCulture));
        }
        else if (auxiliaryType.Length != 0)
        {
            builder.Append("type=");
            builder.Append(auxiliaryType);
        }
        else if (operatingPointIndex >= 0)
        {
            builder.Append("index=");
            builder.Append(operatingPointIndex.ToString(CultureInfo.InvariantCulture));
        }
        else if (layerId >= 0)
        {
            builder.Append("layer=");
            builder.Append(layerId.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            builder.Append(type);
        }

        return builder.ToString();
    }

    /// <summary>Returns the one-based property-container index.</summary>
    /// <returns>The property index.</returns>
    public int GetIndex()
    {
        return index;
    }

    /// <summary>Returns the four-character property type.</summary>
    /// <returns>The property type.</returns>
    public string GetTypeName()
    {
        return type;
    }

    /// <summary>Reports whether the association marks the property essential.</summary>
    /// <returns><c>true</c> when the property is essential.</returns>
    public bool IsEssential()
    {
        return essential;
    }

    /// <summary>Reports whether this property version has parsed semantics.</summary>
    /// <returns><c>true</c> when the property semantics are available.</returns>
    public bool IsKnown()
    {
        return known;
    }

    /// <summary>Returns a copy of the complete property box bytes.</summary>
    /// <returns>The retained property bytes.</returns>
    public byte[] GetRawData()
    {
        return (byte[])rawData.Clone();
    }

    /// <summary>Returns a deterministic short semantic description.</summary>
    /// <returns>The property summary.</returns>
    public string GetSummary()
    {
        return summary;
    }

    /// <summary>Returns the AV1 sequence profile.</summary>
    /// <returns>The profile, or minus one when unavailable.</returns>
    public int GetAv1Profile()
    {
        return av1Profile;
    }

    /// <summary>Returns the AV1 sequence level index.</summary>
    /// <returns>The level index, or minus one when unavailable.</returns>
    public int GetAv1LevelIndex()
    {
        return av1Level;
    }

    /// <summary>Returns the AV1 sequence tier.</summary>
    /// <returns>The tier, or minus one when unavailable.</returns>
    public int GetAv1Tier()
    {
        return av1Tier;
    }

    /// <summary>Returns the configured AV1 bit depth.</summary>
    /// <returns>The bit depth, or minus one when unavailable.</returns>
    public int GetBitDepth()
    {
        return bitDepth;
    }

    /// <summary>Returns the configured AV1 channel count.</summary>
    /// <returns>The channel count, or minus one when unavailable.</returns>
    public int GetChannelCount()
    {
        return channelCount;
    }

    /// <summary>Returns the parsed spatial width.</summary>
    /// <returns>The width, or zero when unavailable.</returns>
    public uint GetImageWidth()
    {
        return imageWidth;
    }

    /// <summary>Returns the parsed spatial height.</summary>
    /// <returns>The height, or zero when unavailable.</returns>
    public uint GetImageHeight()
    {
        return imageHeight;
    }

    /// <summary>Returns the number of pixel-information channels.</summary>
    /// <returns>The channel count.</returns>
    public int GetPixelChannelCount()
    {
        return pixelBitDepths.Count;
    }

    /// <summary>Returns one pixel-information channel bit depth.</summary>
    /// <param name="indexValue">The zero-based channel index.</param>
    /// <returns>The bit depth, or zero for an invalid index.</returns>
    public int GetPixelChannelBitDepth(int indexValue)
    {
        if (indexValue < 0 || indexValue >= pixelBitDepths.Count)
        {
            return 0;
        }

        return pixelBitDepths[indexValue];
    }

    /// <summary>Returns the horizontal pixel-aspect spacing.</summary>
    /// <returns>The spacing, or zero when unavailable.</returns>
    public uint GetHorizontalSpacing()
    {
        return horizontalSpacing;
    }

    /// <summary>Returns the vertical pixel-aspect spacing.</summary>
    /// <returns>The spacing, or zero when unavailable.</returns>
    public uint GetVerticalSpacing()
    {
        return verticalSpacing;
    }

    /// <summary>Returns the counter-clockwise rotation in degrees.</summary>
    /// <returns>The rotation, or minus one when unavailable.</returns>
    public int GetRotationDegrees()
    {
        return rotationDegrees;
    }

    /// <summary>Returns the mirror axis value.</summary>
    /// <returns>The axis, or minus one when unavailable.</returns>
    public int GetMirrorAxis()
    {
        return mirrorAxis;
    }

    /// <summary>Returns the auxiliary type identifier.</summary>
    /// <returns>The auxiliary type, or an empty string.</returns>
    public string GetAuxiliaryType()
    {
        return auxiliaryType;
    }

    /// <summary>Returns the selected AV1 operating-point index.</summary>
    /// <returns>The index, or minus one when unavailable.</returns>
    public int GetOperatingPointIndex()
    {
        return operatingPointIndex;
    }

    /// <summary>Returns the selected layer identifier.</summary>
    /// <returns>The layer identifier, or minus one when unavailable.</returns>
    public int GetLayerId()
    {
        return layerId;
    }

    /// <summary>Returns the progressive layer count from an AV1 layered-image index.</summary>
    /// <returns>One through four, or zero when unavailable.</returns>
    public int GetLayerCount()
    {
        return layerCount;
    }

    /// <summary>Reports whether the layered-image index uses 32-bit sizes.</summary>
    /// <returns><c>true</c> when indexed sizes use 32 bits.</returns>
    public bool UsesLargeLayerSizes()
    {
        return largeLayerSizes;
    }

    /// <summary>Returns one indexed layer-segment size.</summary>
    /// <param name="index">The zero-based segment index before the final layer.</param>
    /// <returns>The segment size, or zero for an invalid index.</returns>
    public uint GetLayerSize(int index)
    {
        if (index < 0 || index >= layerSizes.Length)
        {
            return 0;
        }
        return layerSizes[index];
    }

    /// <summary>Returns the HDR and viewing metadata carried by this property.</summary>
    /// <returns>A metadata copy, which is empty for unrelated properties.</returns>
    public AvifHdrMetadata GetHdrMetadata()
    {
        return AvifHdrMetadata.CreateCopy(hdrMetadata);
    }

    internal AvifHdrMetadata GetHdrMetadataValue()
    {
        return hdrMetadata;
    }

    /// <summary>Returns transform metadata carried by this property.</summary>
    /// <returns>A transform copy, which is an identity for unrelated properties.</returns>
    public AvifTransform GetTransform()
    {
        return AvifTransform.CreateCopy(transform);
    }

    internal AvifTransform GetTransformValue()
    {
        return transform;
    }
}
