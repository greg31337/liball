namespace ttf;

internal enum ContainerSignatureKind
{
    Unknown = 0,
    SfntVersion1 = 1,
    SfntOpenType = 2,
    SfntAppleTrueType = 3,
    SfntType1 = 4,
    Collection = 5,
    Woff1 = 6,
    Woff2 = 7,
    UnsupportedType1Pfb = 100,
    UnsupportedType1Pfa = 101,
    UnsupportedEmbeddedOpenType = 102,
    UnsupportedBitmapPcf = 103,
    UnsupportedBitmapBdf = 104
}

internal static class ContainerDetector
{
    internal static ContainerSignatureKind Detect(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 4)
        {
            uint signature = ReadUInt32BigEndian(data, 0);

            switch (signature)
            {
                case 0x00010000U:
                    return ContainerSignatureKind.SfntVersion1;
                case 0x4F54544FU:
                    return ContainerSignatureKind.SfntOpenType;
                case 0x74727565U:
                    return ContainerSignatureKind.SfntAppleTrueType;
                case 0x74797031U:
                    return ContainerSignatureKind.SfntType1;
                case 0x74746366U:
                    return ContainerSignatureKind.Collection;
                case 0x774F4646U:
                    return ContainerSignatureKind.Woff1;
                case 0x774F4632U:
                    return ContainerSignatureKind.Woff2;
                case 0x01666370U:
                    return ContainerSignatureKind.UnsupportedBitmapPcf;
                default:
                    break;
            }
        }

        if (data.Length >= 2 && data[0] == 0x80 &&
            (data[1] == 0x01 || data[1] == 0x02))
        {
            return ContainerSignatureKind.UnsupportedType1Pfb;
        }

        if (MatchesAscii(data, "%!PS-AdobeFont-1") || MatchesAscii(data, "%!FontType1"))
        {
            return ContainerSignatureKind.UnsupportedType1Pfa;
        }

        if (MatchesAscii(data, "STARTFONT"))
        {
            return ContainerSignatureKind.UnsupportedBitmapBdf;
        }

        if (IsEmbeddedOpenType(data))
        {
            return ContainerSignatureKind.UnsupportedEmbeddedOpenType;
        }

        return ContainerSignatureKind.Unknown;
    }

    internal static bool IsSupported(ContainerSignatureKind kind)
    {
        return kind == ContainerSignatureKind.SfntVersion1 ||
            kind == ContainerSignatureKind.SfntOpenType ||
            kind == ContainerSignatureKind.SfntAppleTrueType ||
            kind == ContainerSignatureKind.SfntType1 ||
            kind == ContainerSignatureKind.Collection ||
            kind == ContainerSignatureKind.Woff1 ||
            kind == ContainerSignatureKind.Woff2;
    }

    internal static FontContainerKind GetContainerKind(ContainerSignatureKind kind)
    {
        switch (kind)
        {
            case ContainerSignatureKind.SfntVersion1:
            case ContainerSignatureKind.SfntOpenType:
            case ContainerSignatureKind.SfntAppleTrueType:
            case ContainerSignatureKind.SfntType1:
                return FontContainerKind.Sfnt;
            case ContainerSignatureKind.Collection:
                return FontContainerKind.Collection;
            case ContainerSignatureKind.Woff1:
                return FontContainerKind.Woff1;
            case ContainerSignatureKind.Woff2:
                return FontContainerKind.Woff2;
            default:
                return FontContainerKind.Unknown;
        }
    }

    static bool IsEmbeddedOpenType(ReadOnlySpan<byte> data)
    {
        if (data.Length < 36)
        {
            return false;
        }

        uint eotSize = ReadUInt32LittleEndian(data, 0);
        uint fontDataSize = ReadUInt32LittleEndian(data, 4);
        uint version = ReadUInt32LittleEndian(data, 8);
        ushort magic = ReadUInt16LittleEndian(data, 34);
        bool knownVersion = version == 0x00010000U ||
            version == 0x00020001U ||
            version == 0x00020002U;

        return magic == 0x504C &&
            knownVersion &&
            eotSize >= 36 &&
            fontDataSize <= eotSize;
    }

    static bool MatchesAscii(ReadOnlySpan<byte> data, string value)
    {
        if (data.Length < value.Length)
        {
            return false;
        }

        int index = 0;

        while (index < value.Length)
        {
            if (data[index] != value[index])
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    static uint ReadUInt32BigEndian(ReadOnlySpan<byte> data, int offset)
    {
        return ((uint)data[offset] << 24) |
            ((uint)data[offset + 1] << 16) |
            ((uint)data[offset + 2] << 8) |
            data[offset + 3];
    }

    static uint ReadUInt32LittleEndian(ReadOnlySpan<byte> data, int offset)
    {
        return data[offset] |
            ((uint)data[offset + 1] << 8) |
            ((uint)data[offset + 2] << 16) |
            ((uint)data[offset + 3] << 24);
    }

    static ushort ReadUInt16LittleEndian(ReadOnlySpan<byte> data, int offset)
    {
        return (ushort)(data[offset] | data[offset + 1] << 8);
    }
}
