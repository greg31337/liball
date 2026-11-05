namespace gif;

internal class GifConstants
{
    internal const string Signature = "GIF";
    internal const string Version87a = "87a";
    internal const string Version89a = "89a";
    internal const string Header87a = "GIF87a";
    internal const string Header89a = "GIF89a";

    internal const int HeaderSize = 6;
    internal const int SignatureSize = 3;
    internal const int VersionSize = 3;
    internal const int LogicalScreenDescriptorSize = 7;
    internal const int ImageDescriptorSize = 10;
    internal const int GraphicControlExtensionBlockSize = 4;
    internal const int PlainTextExtensionBlockSize = 12;
    internal const int ApplicationIdentifierSize = 8;
    internal const int ApplicationAuthenticationCodeSize = 3;
    internal const int NetscapeLoopExtensionPayloadSize = 3;

    internal const byte SignatureByteG = 71;
    internal const byte SignatureByteI = 73;
    internal const byte SignatureByteF = 70;
    internal const byte VersionByte8 = 56;
    internal const byte Version87Byte7 = 55;
    internal const byte Version89Byte9 = 57;
    internal const byte VersionByteA = 97;

    internal const byte ImageSeparator = 44;
    internal const byte ExtensionIntroducer = 33;
    internal const byte Trailer = 59;
    internal const byte BlockTerminator = 0;
    internal const int MaximumDataSubBlockSize = 255;

    internal const byte GraphicControlExtensionLabel = 249;
    internal const byte CommentExtensionLabel = 254;
    internal const byte PlainTextExtensionLabel = 1;
    internal const byte ApplicationExtensionLabel = 255;
    internal const byte NetscapeLoopExtensionSubBlockId = 1;

    internal const string NetscapeLoopApplicationIdentifier = "NETSCAPE";
    internal const string NetscapeLoopAuthenticationCode = "2.0";
    internal const string AnimextsLoopApplicationIdentifier = "ANIMEXTS";
    internal const string AnimextsLoopAuthenticationCode = "1.0";

    internal const byte LogicalScreenGlobalColorTableFlagMask = 128;
    internal const byte LogicalScreenColorResolutionMask = 112;
    internal const byte LogicalScreenSortFlagMask = 8;
    internal const byte LogicalScreenColorTableSizeMask = 7;

    internal const byte ImageLocalColorTableFlagMask = 128;
    internal const byte ImageInterlaceFlagMask = 64;
    internal const byte ImageSortFlagMask = 32;
    internal const byte ImageReservedFieldMask = 24;
    internal const byte ImageColorTableSizeMask = 7;

    internal const byte GraphicControlDisposalMethodMask = 28;
    internal const byte GraphicControlUserInputFlagMask = 2;
    internal const byte GraphicControlTransparentColorFlagMask = 1;
    internal const byte GraphicControlReservedFieldMask = 224;
    internal const int GraphicControlDisposalMethodShift = 2;

    internal const int MinimumColorTableSize = 2;
    internal const int MaximumColorTableSize = 256;
    internal const int ColorTableEntrySize = 3;

    internal const int MinimumLzwMinimumCodeSize = 2;
    internal const int MaximumLzwMinimumCodeSize = 8;
    internal const int MaximumLzwCodeSize = 12;
    internal const int MaximumLzwTableSize = 4096;

    internal const int DefaultMaxWidth = 32767;
    internal const int DefaultMaxHeight = 32767;
    internal const int DefaultMaxFrames = 10000;
    internal const int DefaultMaxBytes = 134217728;
    internal const int DefaultMaxCompressedBytes = 67108864;
    internal const int DefaultMaxSubBlocks = 1048576;
    internal const int DefaultMaxSubBlockBytes = 67108864;
    internal const int DefaultMaxDecodedPixels = 100000000;
    internal const int DefaultMaxMemoryBytes = 268435456;
}

internal class GifPackedFields
{
    internal bool HasFlag(int packedFields, int mask)
    {
        return (packedFields & mask) != 0;
    }

    internal int ExtractColorTableSizeCode(int packedFields, int mask)
    {
        return packedFields & mask;
    }

    internal int ComputeColorTableSizeFromCode(int sizeCode)
    {
        if (sizeCode < 0)
        {
            return -1;
        }

        if (sizeCode > 7)
        {
            return -1;
        }

        return 1 << (sizeCode + 1);
    }

    internal int ComputeColorTableSizeCode(int colorCount)
    {
        int size = GifConstants.MinimumColorTableSize;
        int code = 0;
        while (code <= 7)
        {
            if (colorCount == size)
            {
                return code;
            }

            size = size * 2;
            code = code + 1;
        }

        return -1;
    }

    internal bool HasLogicalScreenGlobalColorTable(int packedFields)
    {
        return HasFlag(packedFields, GifConstants.LogicalScreenGlobalColorTableFlagMask);
    }

    internal int ExtractLogicalScreenColorResolution(int packedFields)
    {
        return ((packedFields & GifConstants.LogicalScreenColorResolutionMask) >> 4) + 1;
    }

    internal bool HasLogicalScreenSortFlag(int packedFields)
    {
        return HasFlag(packedFields, GifConstants.LogicalScreenSortFlagMask);
    }

    internal int ExtractLogicalScreenColorTableSizeCode(int packedFields)
    {
        return ExtractColorTableSizeCode(packedFields, GifConstants.LogicalScreenColorTableSizeMask);
    }

    internal int ExtractLogicalScreenColorTableSize(int packedFields)
    {
        return ComputeColorTableSizeFromCode(ExtractLogicalScreenColorTableSizeCode(packedFields));
    }

    internal int CreateLogicalScreenPackedFields(bool hasGlobalColorTable, int colorResolution, bool sortFlag, int globalColorTableSize)
    {
        int colorResolutionCode = 0;
        if (colorResolution > 0)
        {
            if (colorResolution > 8)
            {
                return -1;
            }

            colorResolutionCode = colorResolution - 1;
        }

        int sizeCode = 0;
        if (hasGlobalColorTable)
        {
            sizeCode = ComputeColorTableSizeCode(globalColorTableSize);
            if (sizeCode < 0)
            {
                return -1;
            }
        }

        int packedFields = (colorResolutionCode << 4) | sizeCode;
        if (hasGlobalColorTable)
        {
            packedFields = packedFields | GifConstants.LogicalScreenGlobalColorTableFlagMask;
        }

        if (sortFlag)
        {
            packedFields = packedFields | GifConstants.LogicalScreenSortFlagMask;
        }

        return packedFields;
    }

    internal bool HasImageLocalColorTable(int packedFields)
    {
        return HasFlag(packedFields, GifConstants.ImageLocalColorTableFlagMask);
    }

    internal bool HasImageInterlaceFlag(int packedFields)
    {
        return HasFlag(packedFields, GifConstants.ImageInterlaceFlagMask);
    }

    internal bool HasImageSortFlag(int packedFields)
    {
        return HasFlag(packedFields, GifConstants.ImageSortFlagMask);
    }

    internal int ExtractImageColorTableSizeCode(int packedFields)
    {
        return ExtractColorTableSizeCode(packedFields, GifConstants.ImageColorTableSizeMask);
    }

    internal int ExtractImageColorTableSize(int packedFields)
    {
        return ComputeColorTableSizeFromCode(ExtractImageColorTableSizeCode(packedFields));
    }

    internal int CreateImagePackedFields(bool hasLocalColorTable, bool interlaceFlag, bool sortFlag, int localColorTableSize)
    {
        int sizeCode = 0;
        if (hasLocalColorTable)
        {
            sizeCode = ComputeColorTableSizeCode(localColorTableSize);
            if (sizeCode < 0)
            {
                return -1;
            }
        }

        int packedFields = sizeCode;
        if (hasLocalColorTable)
        {
            packedFields = packedFields | GifConstants.ImageLocalColorTableFlagMask;
        }

        if (interlaceFlag)
        {
            packedFields = packedFields | GifConstants.ImageInterlaceFlagMask;
        }

        if (sortFlag)
        {
            packedFields = packedFields | GifConstants.ImageSortFlagMask;
        }

        return packedFields;
    }

    internal int ExtractGraphicControlDisposalMethod(int packedFields)
    {
        return (packedFields & GifConstants.GraphicControlDisposalMethodMask) >> GifConstants.GraphicControlDisposalMethodShift;
    }

    internal bool HasGraphicControlUserInputFlag(int packedFields)
    {
        return HasFlag(packedFields, GifConstants.GraphicControlUserInputFlagMask);
    }

    internal bool HasGraphicControlTransparentColorFlag(int packedFields)
    {
        return HasFlag(packedFields, GifConstants.GraphicControlTransparentColorFlagMask);
    }

    internal int CreateGraphicControlPackedFields(int disposalMethod, bool userInputFlag, bool transparentColorFlag)
    {
        if (disposalMethod < 0)
        {
            return -1;
        }

        if (disposalMethod > 7)
        {
            return -1;
        }

        int packedFields = disposalMethod << GifConstants.GraphicControlDisposalMethodShift;
        if (userInputFlag)
        {
            packedFields = packedFields | GifConstants.GraphicControlUserInputFlagMask;
        }

        if (transparentColorFlag)
        {
            packedFields = packedFields | GifConstants.GraphicControlTransparentColorFlagMask;
        }

        return packedFields;
    }
}
