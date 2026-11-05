using System;

namespace avif;

internal class AdditionalItemPropertyParseResult
{
    internal AvifResultCode Code;
    internal ParsedColourInformation? ColourInformation;
    internal ParsedPixelAspectRatio? PixelAspectRatio;
    internal ParsedCleanAperture? CleanAperture;
    internal ParsedImageRotation? ImageRotation;
    internal ParsedImageMirror? ImageMirror;
    internal ParsedAuxiliaryType? AuxiliaryType;
    internal ParsedOperatingPointSelector? OperatingPointSelector;
    internal ParsedLayerSelector? LayerSelector;
    internal ParsedAv1LayeredImageIndex? LayeredImageIndex;

    internal AdditionalItemPropertyParseResult(AvifResultCode code)
    {
        Code = code;
        ColourInformation = null;
        PixelAspectRatio = null;
        CleanAperture = null;
        ImageRotation = null;
        ImageMirror = null;
        AuxiliaryType = null;
        OperatingPointSelector = null;
        LayerSelector = null;
        LayeredImageIndex = null;
    }
}

internal class ParsedColourInformation
{
    string colourType;
    bool cicp;
    bool fullRangePresent;
    int colourPrimaries;
    int transferCharacteristics;
    int matrixCoefficients;
    bool fullRange;
    byte[] data;

    internal ParsedColourInformation(
        string colourType,
        bool cicp,
        bool fullRangePresent,
        int colourPrimaries,
        int transferCharacteristics,
        int matrixCoefficients,
        bool fullRange,
        byte[] data)
    {
        this.colourType = colourType;
        this.cicp = cicp;
        this.fullRangePresent = fullRangePresent;
        this.colourPrimaries = colourPrimaries;
        this.transferCharacteristics = transferCharacteristics;
        this.matrixCoefficients = matrixCoefficients;
        this.fullRange = fullRange;
        this.data = (byte[])data.Clone();
    }

    internal string GetColourType()
    {
        return colourType;
    }

    internal bool HasCicpValues()
    {
        return cicp;
    }

    internal bool HasFullRangeFlag()
    {
        return fullRangePresent;
    }

    internal int GetColourPrimaries()
    {
        return colourPrimaries;
    }

    internal int GetTransferCharacteristics()
    {
        return transferCharacteristics;
    }

    internal int GetMatrixCoefficients()
    {
        return matrixCoefficients;
    }

    internal bool IsFullRange()
    {
        return fullRange;
    }

    internal bool IsIccProfile()
    {
        return colourType == "rICC" || colourType == "prof";
    }

    internal bool IsRestrictedIccProfile()
    {
        return colourType == "rICC";
    }

    internal byte[] GetData()
    {
        return (byte[])data.Clone();
    }

    internal long GetRetainedByteCount()
    {
        return data.Length;
    }
}

internal class ParsedPixelAspectRatio
{
    uint horizontalSpacing;
    uint verticalSpacing;

    internal ParsedPixelAspectRatio(uint horizontalSpacing, uint verticalSpacing)
    {
        this.horizontalSpacing = horizontalSpacing;
        this.verticalSpacing = verticalSpacing;
    }

    internal uint GetHorizontalSpacing()
    {
        return horizontalSpacing;
    }

    internal uint GetVerticalSpacing()
    {
        return verticalSpacing;
    }
}

internal class ParsedCleanAperture
{
    uint widthNumerator;
    uint widthDenominator;
    uint heightNumerator;
    uint heightDenominator;
    int horizontalOffsetNumerator;
    uint horizontalOffsetDenominator;
    int verticalOffsetNumerator;
    uint verticalOffsetDenominator;

    internal ParsedCleanAperture(
        uint widthNumerator,
        uint widthDenominator,
        uint heightNumerator,
        uint heightDenominator,
        int horizontalOffsetNumerator,
        uint horizontalOffsetDenominator,
        int verticalOffsetNumerator,
        uint verticalOffsetDenominator)
    {
        this.widthNumerator = widthNumerator;
        this.widthDenominator = widthDenominator;
        this.heightNumerator = heightNumerator;
        this.heightDenominator = heightDenominator;
        this.horizontalOffsetNumerator = horizontalOffsetNumerator;
        this.horizontalOffsetDenominator = horizontalOffsetDenominator;
        this.verticalOffsetNumerator = verticalOffsetNumerator;
        this.verticalOffsetDenominator = verticalOffsetDenominator;
    }

    internal uint GetWidthNumerator()
    {
        return widthNumerator;
    }

    internal uint GetWidthDenominator()
    {
        return widthDenominator;
    }

    internal uint GetHeightNumerator()
    {
        return heightNumerator;
    }

    internal uint GetHeightDenominator()
    {
        return heightDenominator;
    }

    internal int GetHorizontalOffsetNumerator()
    {
        return horizontalOffsetNumerator;
    }

    internal uint GetHorizontalOffsetDenominator()
    {
        return horizontalOffsetDenominator;
    }

    internal int GetVerticalOffsetNumerator()
    {
        return verticalOffsetNumerator;
    }

    internal uint GetVerticalOffsetDenominator()
    {
        return verticalOffsetDenominator;
    }
}

internal class ParsedImageRotation
{
    int angle;

    internal ParsedImageRotation(int angle)
    {
        this.angle = angle;
    }

    internal int GetQuarterTurnsCounterClockwise()
    {
        return angle;
    }

    internal int GetDegreesCounterClockwise()
    {
        return angle * 90;
    }
}

internal class ParsedImageMirror
{
    int axis;

    internal ParsedImageMirror(int axis)
    {
        this.axis = axis;
    }

    internal int GetAxis()
    {
        return axis;
    }

    internal bool ExchangesLeftAndRight()
    {
        return axis == 1;
    }
}

internal class ParsedAuxiliaryType
{
    string type;
    byte[] subtypes;

    internal ParsedAuxiliaryType(string type, byte[] subtypes)
    {
        this.type = type;
        this.subtypes = (byte[])subtypes.Clone();
    }

    internal string GetTypeValue()
    {
        return type;
    }

    internal byte[] GetSubtypes()
    {
        return (byte[])subtypes.Clone();
    }

    internal long GetRetainedByteCount()
    {
        return subtypes.Length;
    }
}

internal class ParsedOperatingPointSelector
{
    int operatingPointIndex;

    internal ParsedOperatingPointSelector(int operatingPointIndex)
    {
        this.operatingPointIndex = operatingPointIndex;
    }

    internal int GetOperatingPointIndex()
    {
        return operatingPointIndex;
    }
}

internal class ParsedLayerSelector
{
    int layerId;

    internal ParsedLayerSelector(int layerId)
    {
        this.layerId = layerId;
    }

    internal int GetLayerId()
    {
        return layerId;
    }

    internal bool AllowsProgressiveRendering()
    {
        return layerId == 65535;
    }
}

internal class ParsedAv1LayeredImageIndex
{
    bool largeSizes;
    uint[] layerSizes;
    int indexedLayerCount;

    internal ParsedAv1LayeredImageIndex(
        bool largeSizes,
        uint[] layerSizes,
        int indexedLayerCount)
    {
        this.largeSizes = largeSizes;
        this.layerSizes = (uint[])layerSizes.Clone();
        this.indexedLayerCount = indexedLayerCount;
    }

    internal bool UsesLargeSizes()
    {
        return largeSizes;
    }

    internal uint GetLayerSize(int index)
    {
        if (index < 0 || index >= layerSizes.Length)
        {
            return 0;
        }

        return layerSizes[index];
    }

    internal int GetIndexedLayerCount()
    {
        return indexedLayerCount;
    }

    internal int GetLayerCount()
    {
        return indexedLayerCount + 1;
    }

    internal ulong GetIndexedByteCount()
    {
        ulong total = 0;
        int index = 0;
        while (index < indexedLayerCount)
        {
            total += layerSizes[index];
            index++;
        }

        return total;
    }
}

internal static class AdditionalItemPropertyBox
{
    internal static bool IsSupportedType(string type)
    {
        return type == "colr"
            || type == "pasp"
            || type == "clap"
            || type == "irot"
            || type == "imir"
            || type == "auxC"
            || type == "a1op"
            || type == "lsel"
            || type == "a1lx";
    }

    internal static AdditionalItemPropertyParseResult Parse(
        ReadOnlySpan<byte> source,
        IsoBox box,
        ResourceGuard resources)
    {
        if (box == null || resources == null || !IsSupportedType(box.Type))
        {
            return CreateResult(AvifResultCode.InvalidArgument);
        }

        if (box.Type == "auxC")
        {
            return ParseAuxiliaryType(source, box);
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            box.PayloadOffset,
            0,
            box.PayloadSize,
            source.Length);
        if (!range.Success)
        {
            return CreateResult(range.Code);
        }

        ReadOnlySpan<byte> payload = source.Slice(
            (int)box.PayloadOffset,
            (int)box.PayloadSize);
        if (box.Type == "colr")
        {
            return ParseColourInformation(payload, resources);
        }

        if (box.Type == "pasp")
        {
            return ParsePixelAspectRatio(payload);
        }

        if (box.Type == "clap")
        {
            return ParseCleanAperture(payload);
        }

        if (box.Type == "irot")
        {
            return ParseImageRotation(payload);
        }

        if (box.Type == "imir")
        {
            return ParseImageMirror(payload);
        }

        if (box.Type == "a1op")
        {
            return ParseOperatingPointSelector(payload);
        }

        if (box.Type == "lsel")
        {
            return ParseLayerSelector(payload);
        }

        return ParseLayeredImageIndex(payload);
    }

    internal static AdditionalItemPropertyParseResult ParseColourInformation(
        ReadOnlySpan<byte> payload,
        ResourceGuard resources)
    {
        if (payload.Length < 4)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        ByteReader reader = new ByteReader(payload.Length);
        FourCc? typeValue = reader.ReadFourCc(payload);
        if (reader.Code != AvifResultCode.Success || typeValue == null)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        string type = typeValue.Text;
        if (type == "nclx" || type == "nclc")
        {
            int requiredLength = type == "nclx" ? 11 : 10;
            if (payload.Length != requiredLength)
            {
                return CreateResult(AvifResultCode.InvalidContainer);
            }

            int primaries = reader.ReadUInt16BigEndian(payload);
            int transfer = reader.ReadUInt16BigEndian(payload);
            int matrix = reader.ReadUInt16BigEndian(payload);
            bool fullRange = false;
            if (type == "nclx")
            {
                byte rangeAndReserved = reader.ReadUInt8(payload);
                if ((rangeAndReserved & 127) != 0)
                {
                    return CreateResult(AvifResultCode.InvalidItem);
                }

                fullRange = (rangeAndReserved & 128) != 0;
            }

            AdditionalItemPropertyParseResult result = CreateResult(AvifResultCode.Success);
            result.ColourInformation = new ParsedColourInformation(
                type,
                true,
                type == "nclx",
                primaries,
                transfer,
                matrix,
                fullRange,
                System.Array.Empty<byte>());
            return result;
        }

        byte[] data = payload.Slice(4).ToArray();
        if (type == "rICC" || type == "prof")
        {
            if (data.Length == 0)
            {
                return CreateResult(AvifResultCode.InvalidItem);
            }

            AvifResultCode countCode = resources.CountIccBytes(data.Length);
            if (countCode != AvifResultCode.Success)
            {
                return CreateResult(countCode);
            }
        }

        AdditionalItemPropertyParseResult opaqueResult = CreateResult(AvifResultCode.Success);
        opaqueResult.ColourInformation = new ParsedColourInformation(
            type,
            false,
            false,
            0,
            0,
            0,
            false,
            data);
        return opaqueResult;
    }

    internal static AdditionalItemPropertyParseResult ParsePixelAspectRatio(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length != 8)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        ByteReader reader = new ByteReader(payload.Length);
        uint horizontal = reader.ReadUInt32BigEndian(payload);
        uint vertical = reader.ReadUInt32BigEndian(payload);
        if (reader.Code != AvifResultCode.Success)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        if (horizontal == 0 || vertical == 0)
        {
            return CreateResult(AvifResultCode.InvalidItem);
        }

        AdditionalItemPropertyParseResult result = CreateResult(AvifResultCode.Success);
        result.PixelAspectRatio = new ParsedPixelAspectRatio(horizontal, vertical);
        return result;
    }

    internal static AdditionalItemPropertyParseResult ParseCleanAperture(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length != 32)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        ByteReader reader = new ByteReader(payload.Length);
        uint widthNumerator = reader.ReadUInt32BigEndian(payload);
        uint widthDenominator = reader.ReadUInt32BigEndian(payload);
        uint heightNumerator = reader.ReadUInt32BigEndian(payload);
        uint heightDenominator = reader.ReadUInt32BigEndian(payload);
        int horizontalNumerator = reader.ReadInt32BigEndian(payload);
        uint horizontalDenominator = reader.ReadUInt32BigEndian(payload);
        int verticalNumerator = reader.ReadInt32BigEndian(payload);
        uint verticalDenominator = reader.ReadUInt32BigEndian(payload);
        if (reader.Code != AvifResultCode.Success)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        if (widthNumerator == 0
            || widthNumerator > int.MaxValue
            || widthDenominator == 0
            || widthDenominator > int.MaxValue
            || heightNumerator == 0
            || heightNumerator > int.MaxValue
            || heightDenominator == 0
            || heightDenominator > int.MaxValue
            || horizontalDenominator == 0
            || horizontalDenominator > int.MaxValue
            || verticalDenominator == 0
            || verticalDenominator > int.MaxValue)
        {
            return CreateResult(AvifResultCode.InvalidItem);
        }

        AdditionalItemPropertyParseResult result = CreateResult(AvifResultCode.Success);
        result.CleanAperture = new ParsedCleanAperture(
            widthNumerator,
            widthDenominator,
            heightNumerator,
            heightDenominator,
            horizontalNumerator,
            horizontalDenominator,
            verticalNumerator,
            verticalDenominator);
        return result;
    }

    internal static AdditionalItemPropertyParseResult ParseImageRotation(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length != 1)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        if ((payload[0] & 252) != 0)
        {
            return CreateResult(AvifResultCode.InvalidItem);
        }

        AdditionalItemPropertyParseResult result = CreateResult(AvifResultCode.Success);
        result.ImageRotation = new ParsedImageRotation(payload[0] & 3);
        return result;
    }

    internal static AdditionalItemPropertyParseResult ParseImageMirror(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length != 1)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        if ((payload[0] & 254) != 0)
        {
            return CreateResult(AvifResultCode.InvalidItem);
        }

        AdditionalItemPropertyParseResult result = CreateResult(AvifResultCode.Success);
        result.ImageMirror = new ParsedImageMirror(payload[0] & 1);
        return result;
    }

    internal static AdditionalItemPropertyParseResult ParseAuxiliaryType(
        ReadOnlySpan<byte> source,
        IsoBox box)
    {
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success)
        {
            return CreateResult(fullBox.Code);
        }

        if (fullBox.Version != 0)
        {
            return CreateResult(AvifResultCode.UnsupportedFeature);
        }

        if (fullBox.Flags != 0)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            fullBox.PayloadOffset,
            0,
            fullBox.PayloadSize,
            source.Length);
        if (!range.Success)
        {
            return CreateResult(range.Code);
        }

        ReadOnlySpan<byte> payload = source.Slice(
            (int)fullBox.PayloadOffset,
            (int)fullBox.PayloadSize);
        ByteReader reader = new ByteReader(payload.Length);
        string type = reader.ReadNullTerminatedUtf8(payload);
        if (reader.Code != AvifResultCode.Success)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        if (type.Length == 0)
        {
            return CreateResult(AvifResultCode.InvalidItem);
        }

        byte[] subtypes = payload.Slice(reader.Position).ToArray();
        AdditionalItemPropertyParseResult result = CreateResult(AvifResultCode.Success);
        result.AuxiliaryType = new ParsedAuxiliaryType(type, subtypes);
        return result;
    }

    internal static AdditionalItemPropertyParseResult ParseOperatingPointSelector(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length != 1)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        if (payload[0] > 31)
        {
            return CreateResult(AvifResultCode.InvalidItem);
        }

        AdditionalItemPropertyParseResult result = CreateResult(AvifResultCode.Success);
        result.OperatingPointSelector = new ParsedOperatingPointSelector(payload[0]);
        return result;
    }

    internal static AdditionalItemPropertyParseResult ParseLayerSelector(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length != 2)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        ByteReader reader = new ByteReader(payload.Length);
        int layerId = reader.ReadUInt16BigEndian(payload);
        if (reader.Code != AvifResultCode.Success)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        if (layerId > 3 && layerId != 65535)
        {
            return CreateResult(AvifResultCode.InvalidItem);
        }

        AdditionalItemPropertyParseResult result = CreateResult(AvifResultCode.Success);
        result.LayerSelector = new ParsedLayerSelector(layerId);
        return result;
    }

    internal static AdditionalItemPropertyParseResult ParseLayeredImageIndex(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length != 7 && payload.Length != 13)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        ByteReader reader = new ByteReader(payload.Length);
        byte largeSizeField = reader.ReadUInt8(payload);
        if ((largeSizeField & 254) != 0)
        {
            return CreateResult(AvifResultCode.InvalidItem);
        }

        bool largeSizes = (largeSizeField & 1) != 0;
        if ((largeSizes && payload.Length != 13)
            || (!largeSizes && payload.Length != 7))
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        uint[] sizes = new uint[3];
        bool zeroSeen = false;
        int indexedLayerCount = 0;
        int index = 0;
        while (index < sizes.Length)
        {
            uint size = largeSizes
                ? reader.ReadUInt32BigEndian(payload)
                : reader.ReadUInt16BigEndian(payload);
            if (reader.Code != AvifResultCode.Success)
            {
                return CreateResult(AvifResultCode.InvalidContainer);
            }

            if (size == 0)
            {
                zeroSeen = true;
            }
            else
            {
                if (zeroSeen)
                {
                    return CreateResult(AvifResultCode.InvalidItem);
                }

                indexedLayerCount++;
            }

            sizes[index] = size;
            index++;
        }

        AdditionalItemPropertyParseResult result = CreateResult(AvifResultCode.Success);
        result.LayeredImageIndex = new ParsedAv1LayeredImageIndex(
            largeSizes,
            sizes,
            indexedLayerCount);
        return result;
    }

    internal static AdditionalItemPropertyParseResult CreateResult(AvifResultCode code)
    {
        return new AdditionalItemPropertyParseResult(code);
    }
}
