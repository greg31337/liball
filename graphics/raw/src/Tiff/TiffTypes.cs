namespace raw;

internal enum TiffFieldType
{
    Byte = 1,
    Ascii = 2,
    Short = 3,
    Long = 4,
    Rational = 5,
    SignedByte = 6,
    Undefined = 7,
    SignedShort = 8,
    SignedLong = 9,
    SignedRational = 10,
    Float = 11,
    Double = 12,
    Ifd = 13,
    Long8 = 16,
    SignedLong8 = 17,
    Ifd8 = 18,
    Utf8 = 129
}

internal static class TiffFieldTypes
{
    internal static TiffFieldType FromCode(ushort code, long offset)
    {
        if (code >= 1 && code <= 13)
        {
            return (TiffFieldType)code;
        }
        if (code >= 16 && code <= 18)
        {
            return (TiffFieldType)code;
        }
        if (code == 129)
        {
            return TiffFieldType.Utf8;
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The TIFF field type is not defined.", offset);
    }

    internal static int GetElementSize(TiffFieldType fieldType)
    {
        switch (fieldType)
        {
            case TiffFieldType.Byte:
            case TiffFieldType.Ascii:
            case TiffFieldType.SignedByte:
            case TiffFieldType.Undefined:
            case TiffFieldType.Utf8:
                return 1;
            case TiffFieldType.Short:
            case TiffFieldType.SignedShort:
                return 2;
            case TiffFieldType.Long:
            case TiffFieldType.SignedLong:
            case TiffFieldType.Float:
            case TiffFieldType.Ifd:
                return 4;
            case TiffFieldType.Rational:
            case TiffFieldType.SignedRational:
            case TiffFieldType.Double:
            case TiffFieldType.Long8:
            case TiffFieldType.SignedLong8:
            case TiffFieldType.Ifd8:
                return 8;
            default:
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The TIFF field type is not defined.", -1);
        }
    }
}
