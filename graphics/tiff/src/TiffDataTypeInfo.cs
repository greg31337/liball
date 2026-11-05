using System;

namespace tiff;

internal class TiffDataTypeInfo
{
    internal const int ClassicInlineValueBytes = 4;
    internal const int BigTiffInlineValueBytes = 8;

    internal static bool IsKnown(TiffDataType dataType)
    {
        switch (dataType)
        {
            case TiffDataType.Byte:
            case TiffDataType.Ascii:
            case TiffDataType.Short:
            case TiffDataType.Long:
            case TiffDataType.Rational:
            case TiffDataType.SByte:
            case TiffDataType.Undefined:
            case TiffDataType.SShort:
            case TiffDataType.SLong:
            case TiffDataType.SRational:
            case TiffDataType.Float:
            case TiffDataType.Double:
            case TiffDataType.Ifd:
            case TiffDataType.Long8:
            case TiffDataType.SLong8:
            case TiffDataType.Ifd8:
                return true;
            default:
                return false;
        }
    }

    internal static bool IsBigTiffOnly(TiffDataType dataType)
    {
        switch (dataType)
        {
            case TiffDataType.Long8:
            case TiffDataType.SLong8:
            case TiffDataType.Ifd8:
                return true;
            default:
                return false;
        }
    }

    internal static bool IsOffsetDataType(TiffDataType dataType)
    {
        switch (dataType)
        {
            case TiffDataType.Ifd:
            case TiffDataType.Ifd8:
                return true;
            default:
                return false;
        }
    }

    internal static int GetElementSize(TiffDataType dataType)
    {
        switch (dataType)
        {
            case TiffDataType.Byte:
            case TiffDataType.Ascii:
            case TiffDataType.SByte:
            case TiffDataType.Undefined:
                return 1;
            case TiffDataType.Short:
            case TiffDataType.SShort:
                return 2;
            case TiffDataType.Long:
            case TiffDataType.SLong:
            case TiffDataType.Float:
            case TiffDataType.Ifd:
                return 4;
            case TiffDataType.Rational:
            case TiffDataType.SRational:
            case TiffDataType.Double:
            case TiffDataType.Long8:
            case TiffDataType.SLong8:
            case TiffDataType.Ifd8:
                return 8;
            default:
                throw new ArgumentOutOfRangeException("dataType");
        }
    }

    internal static int GetElementBitCount(TiffDataType dataType)
    {
        return GetElementSize(dataType) * 8;
    }

    internal static long CalculateValueByteCount(TiffDataType dataType, long valueCount)
    {
        int elementSize = GetElementSize(dataType);
        return TiffMath.CalculateValueByteCount(valueCount, elementSize);
    }

    internal static bool FitsClassicInline(TiffDataType dataType, long valueCount)
    {
        long byteCount = CalculateValueByteCount(dataType, valueCount);
        return byteCount <= ClassicInlineValueBytes;
    }

    internal static bool FitsBigTiffInline(TiffDataType dataType, long valueCount)
    {
        long byteCount = CalculateValueByteCount(dataType, valueCount);
        return byteCount <= BigTiffInlineValueBytes;
    }
}
