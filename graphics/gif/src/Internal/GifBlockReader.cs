namespace gif;

internal class GifHeaderReadResult
{
    internal bool Success;
    internal GifVersion Version;
    internal string Header;
    internal GifErrorDiagnostic Error;

    internal GifHeaderReadResult()
    {
        Success = false;
        Version = GifVersion.Unknown;
        Header = string.Empty;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifHeaderParser
{
    internal GifHeaderReadResult Read(IGifByteReader reader)
    {
        GifHeaderReadResult result = new GifHeaderReadResult();
        if (reader == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Missing header reader.", new GifDiagnosticPosition(-1, -1, "Header"));
            return result;
        }

        byte[] header = new byte[GifConstants.HeaderSize];
        int bytesRead = reader.Read(header, 0, GifConstants.HeaderSize);
        if (bytesRead < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "GIF header is shorter than 6 bytes.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "Header"));
            return result;
        }

        return ParseHeader(header, reader.GetByteOffset());
    }


    internal GifHeaderReadResult Parse(byte[] data)
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(data);
        return Read(reader);
    }

    internal GifHeaderReadResult ParseHeader(byte[] header, long byteOffset)
    {
        GifHeaderReadResult result = new GifHeaderReadResult();
        if (header == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "GIF header is missing.", new GifDiagnosticPosition(byteOffset, -1, "Header"));
            return result;
        }

        if (header.Length != GifConstants.HeaderSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "GIF header is shorter than 6 bytes.", new GifDiagnosticPosition(byteOffset, -1, "Header"));
            return result;
        }

        result.Header = new string(new char[] { (char)header[0], (char)header[1], (char)header[2], (char)header[3], (char)header[4], (char)header[5] });

        if (header[0] != GifConstants.SignatureByteG || header[1] != GifConstants.SignatureByteI || header[2] != GifConstants.SignatureByteF)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidSignature, "GIF signature is invalid.", new GifDiagnosticPosition(byteOffset, -1, "Header"));
            return result;
        }

        if (header[3] == GifConstants.VersionByte8 && header[4] == GifConstants.Version87Byte7 && header[5] == GifConstants.VersionByteA)
        {
            result.Success = true;
            result.Version = GifVersion.Gif87a;
            return result;
        }

        if (header[3] == GifConstants.VersionByte8 && header[4] == GifConstants.Version89Byte9 && header[5] == GifConstants.VersionByteA)
        {
            result.Success = true;
            result.Version = GifVersion.Gif89a;
            return result;
        }

        result.Error = new GifErrorDiagnostic(GifErrorCode.UnsupportedVersion, "GIF version is unsupported.", new GifDiagnosticPosition(byteOffset, -1, "Header"));
        return result;
    }
}

internal class GifLogicalScreenDescriptorReadResult
{
    internal bool Success;
    internal GifLogicalScreen LogicalScreen;
    internal byte PackedFields;
    internal int GlobalColorTableSizeCode;
    internal GifErrorDiagnostic Error;

    internal GifLogicalScreenDescriptorReadResult()
    {
        Success = false;
        LogicalScreen = new GifLogicalScreen(0, 0);
        PackedFields = 0;
        GlobalColorTableSizeCode = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifLogicalScreenDescriptorParser
{
    internal GifLogicalScreenDescriptorReadResult Read(IGifByteReader reader)
    {
        GifLogicalScreenDescriptorReadResult result = new GifLogicalScreenDescriptorReadResult();
        if (reader == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Missing logical screen descriptor reader.", new GifDiagnosticPosition(-1, -1, "LogicalScreenDescriptor"));
            return result;
        }

        byte[] data = new byte[GifConstants.LogicalScreenDescriptorSize];
        int bytesRead = reader.Read(data, 0, GifConstants.LogicalScreenDescriptorSize);
        if (bytesRead < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Logical screen descriptor is shorter than 7 bytes.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "LogicalScreenDescriptor"));
            return result;
        }

        return ParseDescriptor(data, reader.GetByteOffset());
    }


    internal GifLogicalScreenDescriptorReadResult Parse(byte[] data)
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(data);
        return Read(reader);
    }

    internal GifLogicalScreenDescriptorReadResult ParseDescriptor(byte[] data, long byteOffset)
    {
        GifLogicalScreenDescriptorReadResult result = new GifLogicalScreenDescriptorReadResult();
        if (data == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Logical screen descriptor is missing.", new GifDiagnosticPosition(byteOffset, -1, "LogicalScreenDescriptor"));
            return result;
        }

        if (data.Length != GifConstants.LogicalScreenDescriptorSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Logical screen descriptor is shorter than 7 bytes.", new GifDiagnosticPosition(byteOffset, -1, "LogicalScreenDescriptor"));
            return result;
        }

        GifLogicalScreen logicalScreen = new GifLogicalScreen(0, 0);
        byte packed = data[4];
        int sizeCode = packed & GifConstants.LogicalScreenColorTableSizeMask;

        logicalScreen.Width = GifBinaryReader.ReadUInt16LittleEndian(data, 0);
        logicalScreen.Height = GifBinaryReader.ReadUInt16LittleEndian(data, 2);
        logicalScreen.HasGlobalColorTable = (packed & GifConstants.LogicalScreenGlobalColorTableFlagMask) != 0;
        logicalScreen.ColorResolution = ((packed & GifConstants.LogicalScreenColorResolutionMask) >> 4) + 1;
        logicalScreen.SortFlag = (packed & GifConstants.LogicalScreenSortFlagMask) != 0;
        logicalScreen.GlobalColorTableSize = 1 << (sizeCode + 1);
        logicalScreen.BackgroundColorIndex = data[5];
        logicalScreen.PixelAspectRatio = data[6];

        result.Success = true;
        result.LogicalScreen = logicalScreen;
        result.PackedFields = packed;
        result.GlobalColorTableSizeCode = sizeCode;
        return result;
    }
}

internal class GifColorTableReadResult
{
    internal bool Success;
    internal GifPalette Palette;
    internal int ColorCount;
    internal int BytesRead;
    internal GifErrorDiagnostic Error;

    internal GifColorTableReadResult()
    {
        Success = false;
        Palette = new GifPalette(null);
        ColorCount = 0;
        BytesRead = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifColorTableReader
{
    internal GifColorTableReadResult Read(IGifByteReader reader, int colorCount)
    {
        GifColorTableReadResult result = new GifColorTableReadResult();
        if (!ValidateColorCount(colorCount))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Color table size is invalid.", new GifDiagnosticPosition(-1, -1, "ColorTable"));
            return result;
        }

        if (reader == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Missing color table reader.", new GifDiagnosticPosition(-1, -1, "ColorTable"));
            return result;
        }

        byte[] data = new byte[colorCount * GifConstants.ColorTableEntrySize];
        int bytesRead = reader.Read(data, 0, data.Length);
        if (bytesRead < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Color table ended before all RGB entries were read.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "ColorTable"));
            return result;
        }

        return ParseColorTable(data, colorCount, reader.GetByteOffset());
    }


    internal GifColorTableReadResult ParseColorTable(byte[] data, int colorCount, long byteOffset)
    {
        GifColorTableReadResult result = new GifColorTableReadResult();
        if (!ValidateColorCount(colorCount))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Color table size is invalid.", new GifDiagnosticPosition(byteOffset, -1, "ColorTable"));
            return result;
        }

        if (data == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Color table data is missing.", new GifDiagnosticPosition(byteOffset, -1, "ColorTable"));
            return result;
        }

        int expectedBytes = colorCount * GifConstants.ColorTableEntrySize;
        if (data.Length != expectedBytes)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Color table byte count does not match color count.", new GifDiagnosticPosition(byteOffset, -1, "ColorTable"));
            return result;
        }

        GifColor[] colors = new GifColor[colorCount];
        int colorIndex = 0;
        int byteIndex = 0;
        while (colorIndex < colorCount)
        {
            colors[colorIndex] = new GifColor(data[byteIndex], data[byteIndex + 1], data[byteIndex + 2]);
            colorIndex = colorIndex + 1;
            byteIndex = byteIndex + GifConstants.ColorTableEntrySize;
        }

        result.Success = true;
        result.ColorCount = colorCount;
        result.BytesRead = expectedBytes;
        result.Palette = new GifPalette(colors);
        return result;
    }

    internal bool ValidateColorCount(int colorCount)
    {
        GifColorTableValidator validator = new GifColorTableValidator();
        return validator.IsValidColorCount(colorCount);
    }
}

internal class GifImageDescriptorReadResult
{
    internal bool Success;
    internal GifFrame Frame;
    internal byte PackedFields;
    internal int LocalColorTableSizeCode;
    internal GifErrorDiagnostic Error;

    internal GifImageDescriptorReadResult()
    {
        Success = false;
        Frame = new GifFrame(0, 0, 0, 0);
        PackedFields = 0;
        LocalColorTableSizeCode = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifImageDescriptorParser
{
    internal GifImageDescriptorReadResult Read(IGifByteReader reader)
    {
        GifImageDescriptorReadResult result = new GifImageDescriptorReadResult();
        if (reader == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Missing image descriptor reader.", new GifDiagnosticPosition(-1, -1, "ImageDescriptor"));
            return result;
        }

        byte[] data = new byte[GifConstants.ImageDescriptorSize];
        int bytesRead = reader.Read(data, 0, GifConstants.ImageDescriptorSize);
        if (bytesRead < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Image descriptor is shorter than 10 bytes.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "ImageDescriptor"));
            return result;
        }

        return ParseDescriptor(data, reader.GetByteOffset());
    }


    internal GifImageDescriptorReadResult Parse(byte[] data)
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(data);
        return Read(reader);
    }

    internal GifImageDescriptorReadResult ParseDescriptor(byte[] data, long byteOffset)
    {
        GifImageDescriptorReadResult result = new GifImageDescriptorReadResult();
        if (data == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Image descriptor is missing.", new GifDiagnosticPosition(byteOffset, -1, "ImageDescriptor"));
            return result;
        }

        if (data.Length != GifConstants.ImageDescriptorSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Image descriptor is shorter than 10 bytes.", new GifDiagnosticPosition(byteOffset, -1, "ImageDescriptor"));
            return result;
        }

        if (data[0] != GifConstants.ImageSeparator)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnknownBlockType, "Image descriptor does not start with the image separator.", new GifDiagnosticPosition(byteOffset, -1, "ImageDescriptor"));
            return result;
        }

        GifPackedFields packedFields = new GifPackedFields();
        GifFrame frame = new GifFrame(0, 0, 0, 0);
        byte packed = data[9];
        int sizeCode = packedFields.ExtractImageColorTableSizeCode(packed);

        frame.Left = GifBinaryReader.ReadUInt16LittleEndian(data, 1);
        frame.Top = GifBinaryReader.ReadUInt16LittleEndian(data, 3);
        frame.Width = GifBinaryReader.ReadUInt16LittleEndian(data, 5);
        frame.Height = GifBinaryReader.ReadUInt16LittleEndian(data, 7);
        frame.HasLocalColorTable = packedFields.HasImageLocalColorTable(packed);
        frame.Interlace = packedFields.HasImageInterlaceFlag(packed);
        frame.LocalColorTableSortFlag = packedFields.HasImageSortFlag(packed);
        frame.LocalColorTableSizeCode = sizeCode;
        frame.LocalColorTableSize = packedFields.ComputeColorTableSizeFromCode(sizeCode);

        result.Success = true;
        result.Frame = frame;
        result.PackedFields = packed;
        result.LocalColorTableSizeCode = sizeCode;
        return result;
    }
}

internal class GifGraphicControlExtensionReadResult
{
    internal bool Success;
    internal GifGraphicControl GraphicControl;
    internal byte PackedFields;
    internal GifErrorDiagnostic Error;

    internal GifGraphicControlExtensionReadResult()
    {
        Success = false;
        GraphicControl = new GifGraphicControl();
        PackedFields = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifGraphicControlExtensionParser
{
    internal GifGraphicControlExtensionReadResult Read(IGifByteReader reader)
    {
        GifGraphicControlExtensionReadResult result = new GifGraphicControlExtensionReadResult();
        if (reader == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Missing graphic control extension reader.", new GifDiagnosticPosition(-1, -1, "GraphicControlExtension"));
            return result;
        }

        byte[] data = new byte[GifConstants.GraphicControlExtensionBlockSize + 4];
        int bytesRead = reader.Read(data, 0, data.Length);
        if (bytesRead < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Graphic control extension is shorter than 8 bytes.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "GraphicControlExtension"));
            return result;
        }

        return ParseExtension(data, reader.GetByteOffset());
    }


    internal GifGraphicControlExtensionReadResult Parse(byte[] data)
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(data);
        return Read(reader);
    }

    internal GifGraphicControlExtensionReadResult ParseExtension(byte[] data, long byteOffset)
    {
        GifGraphicControlExtensionReadResult result = new GifGraphicControlExtensionReadResult();
        if (data == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Graphic control extension is missing.", new GifDiagnosticPosition(byteOffset, -1, "GraphicControlExtension"));
            return result;
        }

        if (data.Length != GifConstants.GraphicControlExtensionBlockSize + 4)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Graphic control extension is shorter than 8 bytes.", new GifDiagnosticPosition(byteOffset, -1, "GraphicControlExtension"));
            return result;
        }

        if (data[0] != GifConstants.ExtensionIntroducer || data[1] != GifConstants.GraphicControlExtensionLabel)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnknownBlockType, "Graphic control extension has an invalid introducer or label.", new GifDiagnosticPosition(byteOffset, -1, "GraphicControlExtension"));
            return result;
        }

        if (data[2] != GifConstants.GraphicControlExtensionBlockSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Graphic control extension block size must be 4.", new GifDiagnosticPosition(byteOffset, -1, "GraphicControlExtension"));
            return result;
        }

        if (data[7] != GifConstants.BlockTerminator)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Graphic control extension is missing its terminator.", new GifDiagnosticPosition(byteOffset, -1, "GraphicControlExtension"));
            return result;
        }

        GifPackedFields packedFields = new GifPackedFields();
        GifGraphicControl control = new GifGraphicControl();
        byte packed = data[3];
        int disposalMethod = packedFields.ExtractGraphicControlDisposalMethod(packed);

        control.DisposalMethod = (GifDisposalMethod)disposalMethod;
        control.UserInputFlag = packedFields.HasGraphicControlUserInputFlag(packed);
        control.HasTransparency = packedFields.HasGraphicControlTransparentColorFlag(packed);
        control.DelayHundredths = GifBinaryReader.ReadUInt16LittleEndian(data, 4);
        control.TransparentColorIndex = data[6];

        result.Success = true;
        result.GraphicControl = control;
        result.PackedFields = packed;
        return result;
    }
}

internal class GifCommentExtensionReadResult
{
    internal bool Success;
    internal GifCommentExtension Comment;
    internal int BlockCount;
    internal int TotalBytes;
    internal GifErrorDiagnostic Error;

    internal GifCommentExtensionReadResult()
    {
        Success = false;
        Comment = new GifCommentExtension(null);
        BlockCount = 0;
        TotalBytes = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifCommentExtensionParser
{
    internal GifCommentExtensionReadResult Read(IGifByteReader reader)
    {
        GifCommentExtensionReadResult result = new GifCommentExtensionReadResult();
        if (reader == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Missing comment extension reader.", new GifDiagnosticPosition(-1, -1, "CommentExtension"));
            return result;
        }

        long headerOffset = reader.GetByteOffset();
        byte[] header = new byte[2];
        int bytesRead = reader.Read(header, 0, header.Length);
        if (bytesRead < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Comment extension is missing its introducer or label.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "CommentExtension"));
            return result;
        }

        if (!IsCommentExtensionHeader(header))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnknownBlockType, "Comment extension has an invalid introducer or label.", new GifDiagnosticPosition(headerOffset, -1, "CommentExtension"));
            return result;
        }

        GifSubBlockReader subBlockReader = new GifSubBlockReader(GifConstants.DefaultMaxSubBlocks, GifConstants.DefaultMaxSubBlockBytes);
        GifSubBlockReadResult subBlocks = subBlockReader.Read(reader);
        return CreateResultFromSubBlocks(subBlocks);
    }


    internal GifCommentExtensionReadResult Parse(byte[] data)
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(data);
        return Read(reader);
    }

    internal bool IsCommentExtensionHeader(byte[] header)
    {
        if (header == null)
        {
            return false;
        }

        if (header.Length != 2)
        {
            return false;
        }

        return header[0] == GifConstants.ExtensionIntroducer && header[1] == GifConstants.CommentExtensionLabel;
    }

    internal string DecodeText(byte[] data)
    {
        if (data == null)
        {
            return string.Empty;
        }

        char[] chars = new char[data.Length];
        int index = 0;
        while (index < data.Length)
        {
            chars[index] = (char)data[index];
            index = index + 1;
        }

        return new string(chars);
    }

    internal GifCommentExtensionReadResult CreateResultFromSubBlocks(GifSubBlockReadResult subBlocks)
    {
        GifCommentExtensionReadResult result = new GifCommentExtensionReadResult();
        if (subBlocks == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnterminatedSubBlocks, "Comment extension data sub-blocks are missing.", new GifDiagnosticPosition(-1, -1, "CommentExtension"));
            return result;
        }

        result.Comment.Data = subBlocks.Data;
        result.Comment.Text = DecodeText(subBlocks.Data);
        result.BlockCount = subBlocks.BlockCount;
        result.TotalBytes = subBlocks.TotalBytes;

        if (!subBlocks.Success)
        {
            result.Error = subBlocks.Error;
            return result;
        }

        result.Success = true;
        return result;
    }
}

internal class GifPlainTextExtensionReadResult
{
    internal bool Success;
    internal GifPlainTextExtension PlainText;
    internal byte BlockSize;
    internal int BlockCount;
    internal int TotalBytes;
    internal GifErrorDiagnostic Error;

    internal GifPlainTextExtensionReadResult()
    {
        Success = false;
        PlainText = new GifPlainTextExtension();
        BlockSize = 0;
        BlockCount = 0;
        TotalBytes = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifPlainTextExtensionParser
{
    internal GifPlainTextExtensionReadResult Read(IGifByteReader reader)
    {
        GifPlainTextExtensionReadResult result = new GifPlainTextExtensionReadResult();
        if (reader == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Missing plain text extension reader.", new GifDiagnosticPosition(-1, -1, "PlainTextExtension"));
            return result;
        }

        long headerOffset = reader.GetByteOffset();
        byte[] header = new byte[GifConstants.PlainTextExtensionBlockSize + 3];
        int bytesRead = reader.Read(header, 0, header.Length);
        if (bytesRead < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Plain text extension header is shorter than 15 bytes.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "PlainTextExtension"));
            return result;
        }

        result = ParseHeader(header, headerOffset);
        if (!result.Success)
        {
            return result;
        }

        GifSubBlockReader subBlockReader = new GifSubBlockReader(GifConstants.DefaultMaxSubBlocks, GifConstants.DefaultMaxSubBlockBytes);
        GifSubBlockReadResult subBlocks = subBlockReader.Read(reader);
        return CreateResultFromSubBlocks(result.PlainText, result.BlockSize, subBlocks);
    }


    internal GifPlainTextExtensionReadResult Parse(byte[] data)
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(data);
        return Read(reader);
    }

    internal GifPlainTextExtensionReadResult ParseHeader(byte[] header, long byteOffset)
    {
        GifPlainTextExtensionReadResult result = new GifPlainTextExtensionReadResult();
        int headerSize = GifConstants.PlainTextExtensionBlockSize + 3;
        if (header == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Plain text extension header is missing.", new GifDiagnosticPosition(byteOffset, -1, "PlainTextExtension"));
            return result;
        }

        if (header.Length != headerSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Plain text extension header is shorter than 15 bytes.", new GifDiagnosticPosition(byteOffset, -1, "PlainTextExtension"));
            return result;
        }

        if (header[0] != GifConstants.ExtensionIntroducer || header[1] != GifConstants.PlainTextExtensionLabel)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnknownBlockType, "Plain text extension has an invalid introducer or label.", new GifDiagnosticPosition(byteOffset, -1, "PlainTextExtension"));
            return result;
        }

        if (header[2] != GifConstants.PlainTextExtensionBlockSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Plain text extension block size must be 12.", new GifDiagnosticPosition(byteOffset, -1, "PlainTextExtension"));
            return result;
        }

        GifPlainTextExtension plainText = new GifPlainTextExtension();
        plainText.TextGridLeft = GifBinaryReader.ReadUInt16LittleEndian(header, 3);
        plainText.TextGridTop = GifBinaryReader.ReadUInt16LittleEndian(header, 5);
        plainText.TextGridWidth = GifBinaryReader.ReadUInt16LittleEndian(header, 7);
        plainText.TextGridHeight = GifBinaryReader.ReadUInt16LittleEndian(header, 9);
        plainText.CharacterCellWidth = header[11];
        plainText.CharacterCellHeight = header[12];
        plainText.ForegroundColorIndex = header[13];
        plainText.BackgroundColorIndex = header[14];

        result.Success = true;
        result.PlainText = plainText;
        result.BlockSize = header[2];
        return result;
    }

    internal GifPlainTextExtensionReadResult CreateResultFromSubBlocks(GifPlainTextExtension plainText, byte blockSize, GifSubBlockReadResult subBlocks)
    {
        GifPlainTextExtensionReadResult result = new GifPlainTextExtensionReadResult();
        if (plainText != null)
        {
            result.PlainText = plainText;
        }

        result.BlockSize = blockSize;

        if (subBlocks == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnterminatedSubBlocks, "Plain text extension data sub-blocks are missing.", new GifDiagnosticPosition(-1, -1, "PlainTextExtension"));
            return result;
        }

        result.PlainText.TextData = subBlocks.Data;
        result.PlainText.Text = DecodeText(subBlocks.Data);
        result.BlockCount = subBlocks.BlockCount;
        result.TotalBytes = subBlocks.TotalBytes;

        if (!subBlocks.Success)
        {
            result.Error = subBlocks.Error;
            return result;
        }

        result.Success = true;
        return result;
    }

    internal string DecodeText(byte[] data)
    {
        if (data == null)
        {
            return string.Empty;
        }

        char[] chars = new char[data.Length];
        int index = 0;
        while (index < data.Length)
        {
            chars[index] = (char)data[index];
            index = index + 1;
        }

        return new string(chars);
    }
}

internal class GifUnknownExtensionReadResult
{
    internal bool Success;
    internal GifUnknownExtension UnknownExtension;
    internal int BlockCount;
    internal int TotalBytes;
    internal GifErrorDiagnostic Error;

    internal GifUnknownExtensionReadResult()
    {
        Success = false;
        UnknownExtension = new GifUnknownExtension(0, null, null);
        BlockCount = 0;
        TotalBytes = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifUnknownExtensionParser
{
    internal GifUnknownExtensionReadResult Read(IGifByteReader reader)
    {
        GifUnknownExtensionReadResult result = new GifUnknownExtensionReadResult();
        if (reader == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Missing unknown extension reader.", new GifDiagnosticPosition(-1, -1, "UnknownExtension"));
            return result;
        }

        long headerOffset = reader.GetByteOffset();
        byte[] header = new byte[2];
        int bytesRead = reader.Read(header, 0, header.Length);
        if (bytesRead < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Unknown extension is missing its introducer or label.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "UnknownExtension"));
            return result;
        }

        result = ParseHeader(header, headerOffset);
        if (!result.Success)
        {
            return result;
        }

        GifSubBlockReader subBlockReader = new GifSubBlockReader(GifConstants.DefaultMaxSubBlocks, GifConstants.DefaultMaxSubBlockBytes);
        GifSubBlockReadResult subBlocks = subBlockReader.Read(reader);
        return CreateResultFromSubBlocks(result.UnknownExtension, subBlocks);
    }


    internal GifUnknownExtensionReadResult Parse(byte[] data)
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(data);
        return Read(reader);
    }

    internal GifUnknownExtensionReadResult ParseHeader(byte[] header, long byteOffset)
    {
        GifUnknownExtensionReadResult result = new GifUnknownExtensionReadResult();
        if (header == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Unknown extension header is missing.", new GifDiagnosticPosition(byteOffset, -1, "UnknownExtension"));
            return result;
        }

        if (header.Length != 2)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Unknown extension header is shorter than 2 bytes.", new GifDiagnosticPosition(byteOffset, -1, "UnknownExtension"));
            return result;
        }

        if (header[0] != GifConstants.ExtensionIntroducer)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnknownBlockType, "Unknown extension has an invalid introducer.", new GifDiagnosticPosition(byteOffset, -1, "UnknownExtension"));
            return result;
        }

        if (!IsUnknownExtensionLabel(header[1]))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnknownBlockType, "Extension label is handled by a known parser.", new GifDiagnosticPosition(byteOffset, -1, "UnknownExtension"));
            return result;
        }

        GifUnknownExtension unknownExtension = new GifUnknownExtension(0, null, null);
        unknownExtension.Label = header[1];
        unknownExtension.BlockName = CreateBlockName(header[1]);

        result.Success = true;
        result.UnknownExtension = unknownExtension;
        return result;
    }

    internal GifUnknownExtensionReadResult CreateResultFromSubBlocks(GifUnknownExtension unknownExtension, GifSubBlockReadResult subBlocks)
    {
        GifUnknownExtensionReadResult result = new GifUnknownExtensionReadResult();
        if (unknownExtension != null)
        {
            result.UnknownExtension = unknownExtension;
        }

        if (subBlocks == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnterminatedSubBlocks, "Unknown extension data sub-blocks are missing.", new GifDiagnosticPosition(-1, -1, "UnknownExtension"));
            return result;
        }

        result.UnknownExtension.Data = subBlocks.Data;
        result.BlockCount = subBlocks.BlockCount;
        result.TotalBytes = subBlocks.TotalBytes;

        if (!subBlocks.Success)
        {
            result.Error = subBlocks.Error;
            return result;
        }

        result.Success = true;
        return result;
    }

    internal bool IsUnknownExtensionLabel(byte label)
    {
        if (label == GifConstants.GraphicControlExtensionLabel)
        {
            return false;
        }

        if (label == GifConstants.CommentExtensionLabel)
        {
            return false;
        }

        if (label == GifConstants.PlainTextExtensionLabel)
        {
            return false;
        }

        if (label == GifConstants.ApplicationExtensionLabel)
        {
            return false;
        }

        return true;
    }

    internal string CreateBlockName(byte label)
    {
        return "UnknownExtension0x" + ((int)label).ToString("X2");
    }
}

internal class GifApplicationExtensionReadResult
{
    internal bool Success;
    internal GifApplicationExtension Application;
    internal byte BlockSize;
    internal int BlockCount;
    internal int TotalBytes;
    internal GifErrorDiagnostic Error;

    internal GifApplicationExtensionReadResult()
    {
        Success = false;
        Application = new GifApplicationExtension();
        BlockSize = 0;
        BlockCount = 0;
        TotalBytes = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifApplicationExtensionParser
{
    internal GifApplicationExtensionReadResult Read(IGifByteReader reader)
    {
        GifApplicationExtensionReadResult result = new GifApplicationExtensionReadResult();
        if (reader == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Missing application extension reader.", new GifDiagnosticPosition(-1, -1, "ApplicationExtension"));
            return result;
        }

        long headerOffset = reader.GetByteOffset();
        byte[] header = new byte[GifConstants.ApplicationIdentifierSize + GifConstants.ApplicationAuthenticationCodeSize + 3];
        int bytesRead = reader.Read(header, 0, header.Length);
        if (bytesRead < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Application extension header is shorter than 14 bytes.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "ApplicationExtension"));
            return result;
        }

        result = ParseHeader(header, headerOffset);
        if (!result.Success)
        {
            return result;
        }

        GifSubBlockReader subBlockReader = new GifSubBlockReader(GifConstants.DefaultMaxSubBlocks, GifConstants.DefaultMaxSubBlockBytes);
        GifSubBlockReadResult subBlocks = subBlockReader.Read(reader);
        return CreateResultFromSubBlocks(result.Application, result.BlockSize, subBlocks);
    }


    internal GifApplicationExtensionReadResult Parse(byte[] data)
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(data);
        return Read(reader);
    }

    internal GifApplicationExtensionReadResult ParseHeader(byte[] header, long byteOffset)
    {
        GifApplicationExtensionReadResult result = new GifApplicationExtensionReadResult();
        int headerSize = GifConstants.ApplicationIdentifierSize + GifConstants.ApplicationAuthenticationCodeSize + 3;
        if (header == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Application extension header is missing.", new GifDiagnosticPosition(byteOffset, -1, "ApplicationExtension"));
            return result;
        }

        if (header.Length != headerSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Application extension header is shorter than 14 bytes.", new GifDiagnosticPosition(byteOffset, -1, "ApplicationExtension"));
            return result;
        }

        if (header[0] != GifConstants.ExtensionIntroducer || header[1] != GifConstants.ApplicationExtensionLabel)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnknownBlockType, "Application extension has an invalid introducer or label.", new GifDiagnosticPosition(byteOffset, -1, "ApplicationExtension"));
            return result;
        }

        int expectedBlockSize = GifConstants.ApplicationIdentifierSize + GifConstants.ApplicationAuthenticationCodeSize;
        if (header[2] != expectedBlockSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Application extension block size must be 11.", new GifDiagnosticPosition(byteOffset, -1, "ApplicationExtension"));
            return result;
        }

        GifApplicationExtension application = new GifApplicationExtension();
        application.Identifier = DecodeFixedText(header, 3, GifConstants.ApplicationIdentifierSize);
        application.AuthenticationCode = DecodeFixedText(header, 3 + GifConstants.ApplicationIdentifierSize, GifConstants.ApplicationAuthenticationCodeSize);

        result.Success = true;
        result.Application = application;
        result.BlockSize = header[2];
        return result;
    }

    internal string DecodeFixedText(byte[] data, int offset, int count)
    {
        if (data == null)
        {
            return string.Empty;
        }

        if (offset < 0 || count < 0)
        {
            return string.Empty;
        }

        if (offset > data.Length)
        {
            return string.Empty;
        }

        if (count > data.Length - offset)
        {
            return string.Empty;
        }

        char[] chars = new char[count];
        int index = 0;
        while (index < count)
        {
            chars[index] = (char)data[offset + index];
            index = index + 1;
        }

        return new string(chars);
    }

    internal GifApplicationExtensionReadResult CreateResultFromSubBlocks(GifApplicationExtension application, byte blockSize, GifSubBlockReadResult subBlocks)
    {
        GifApplicationExtensionReadResult result = new GifApplicationExtensionReadResult();
        if (application != null)
        {
            result.Application = application;
        }

        result.BlockSize = blockSize;

        if (subBlocks == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnterminatedSubBlocks, "Application extension data sub-blocks are missing.", new GifDiagnosticPosition(-1, -1, "ApplicationExtension"));
            return result;
        }

        result.Application.Data = subBlocks.Data;
        result.BlockCount = subBlocks.BlockCount;
        result.TotalBytes = subBlocks.TotalBytes;

        if (!subBlocks.Success)
        {
            result.Error = subBlocks.Error;
            return result;
        }

        if (IsLoopExtensionHeader(result.Application))
        {
            if (!PopulateLoopExtension(result.Application))
            {
                result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Application loop extension payload is malformed.", new GifDiagnosticPosition(-1, -1, "ApplicationExtension"));
                return result;
            }
        }

        result.Success = true;
        return result;
    }

    internal bool IsNetscapeLoopHeader(GifApplicationExtension application)
    {
        if (application == null)
        {
            return false;
        }

        if (application.Identifier != GifConstants.NetscapeLoopApplicationIdentifier)
        {
            return false;
        }

        if (application.AuthenticationCode != GifConstants.NetscapeLoopAuthenticationCode)
        {
            return false;
        }

        return true;
    }

    internal bool IsAnimextsLoopHeader(GifApplicationExtension application)
    {
        if (application == null)
        {
            return false;
        }

        if (application.Identifier != GifConstants.AnimextsLoopApplicationIdentifier)
        {
            return false;
        }

        if (application.AuthenticationCode != GifConstants.AnimextsLoopAuthenticationCode)
        {
            return false;
        }

        return true;
    }

    internal bool IsLoopExtensionHeader(GifApplicationExtension application)
    {
        if (IsNetscapeLoopHeader(application))
        {
            return true;
        }

        if (IsAnimextsLoopHeader(application))
        {
            return true;
        }

        return false;
    }

    internal bool PopulateNetscapeLoop(GifApplicationExtension application)
    {
        return PopulateLoopExtension(application);
    }

    internal bool PopulateLoopExtension(GifApplicationExtension application)
    {
        if (application == null)
        {
            return false;
        }

        if (application.Data == null)
        {
            return false;
        }

        if (application.Data.Length != GifConstants.NetscapeLoopExtensionPayloadSize)
        {
            return false;
        }

        if (application.Data[0] != GifConstants.NetscapeLoopExtensionSubBlockId)
        {
            return false;
        }

        int loopCount = GifBinaryReader.ReadUInt16LittleEndian(application.Data, 1);
        if (loopCount < 0)
        {
            return false;
        }

        application.IsLoopExtension = true;
        application.LoopCount = loopCount;
        return true;
    }
}

internal class GifImageDataReadResult
{
    internal bool Success;
    internal int LzwMinimumCodeSize;
    internal byte[] CompressedData;
    internal int BlockCount;
    internal int TotalBytes;
    internal GifErrorDiagnostic Error;

    internal GifImageDataReadResult()
    {
        Success = false;
        LzwMinimumCodeSize = 0;
        CompressedData = new byte[0];
        BlockCount = 0;
        TotalBytes = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifImageDataReader
{
    internal int MaxSubBlocks;
    internal int MaxBytes;
    internal int MaxCompressedBytes;
    internal int MaxMemoryBytes;
    internal long MaxInputBytes;

    internal GifImageDataReader()
    {
        MaxSubBlocks = GifConstants.DefaultMaxSubBlocks;
        MaxBytes = GifConstants.DefaultMaxSubBlockBytes;
        MaxCompressedBytes = GifConstants.DefaultMaxCompressedBytes;
        MaxMemoryBytes = GifConstants.DefaultMaxMemoryBytes;
        MaxInputBytes = long.MaxValue;
    }

    internal GifImageDataReadResult Read(IGifByteReader reader)
    {
        GifImageDataReadResult result = new GifImageDataReadResult();
        if (reader == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Missing image data reader.", new GifDiagnosticPosition(-1, -1, "ImageData"));
            return result;
        }

        if (reader.GetByteOffset() >= MaxInputBytes)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InputByteLimitExceeded, "GIF input byte limit was reached before image data.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "ImageData"));
            return result;
        }

        int minimumCodeSize = reader.ReadByte();
        if (minimumCodeSize < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Image data is missing its LZW minimum code size.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "ImageData"));
            return result;
        }

        return ReadSubBlocks(reader, minimumCodeSize);
    }


    internal GifImageDataReadResult ReadMetadataOnly(IGifByteReader reader)
    {
        GifImageDataReadResult result = new GifImageDataReadResult();
        if (reader == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Missing image data reader.", new GifDiagnosticPosition(-1, -1, "ImageData"));
            return result;
        }

        if (reader.GetByteOffset() >= MaxInputBytes)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InputByteLimitExceeded, "GIF input byte limit was reached before image data.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "ImageData"));
            return result;
        }

        int minimumCodeSize = reader.ReadByte();
        if (minimumCodeSize < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Image data is missing its LZW minimum code size.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "ImageData"));
            return result;
        }

        return SkipSubBlocks(reader, minimumCodeSize);
    }


    internal GifImageDataReadResult ReadSubBlocks(IGifByteReader reader, int minimumCodeSize)
    {
        GifImageDataReadResult result = ValidateMinimumCodeSize(minimumCodeSize);
        if (!result.Success)
        {
            return result;
        }

        GifSubBlockReader subBlockReader = new GifSubBlockReader(MaxSubBlocks, MaxBytes);
        subBlockReader.MaxAdditionalBytes = MaxCompressedBytes;
        subBlockReader.AdditionalByteLimitErrorCode = GifErrorCode.CompressedByteLimitExceeded;
        subBlockReader.MaxMemoryBytes = MaxMemoryBytes;
        subBlockReader.MaxInputBytes = MaxInputBytes;
        GifSubBlockReadResult subBlocks = subBlockReader.Read(reader);
        return CreateResultFromSubBlocks(minimumCodeSize, subBlocks);
    }


    internal GifImageDataReadResult SkipSubBlocks(IGifByteReader reader, int minimumCodeSize)
    {
        GifImageDataReadResult result = ValidateMinimumCodeSize(minimumCodeSize);
        if (!result.Success)
        {
            return result;
        }

        if (reader == null)
        {
            result.Success = false;
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnterminatedSubBlocks, "Missing sub-block reader.", new GifDiagnosticPosition(-1, -1, "DataSubBlocks"));
            return result;
        }

        while (true)
        {
            if (reader.GetByteOffset() >= MaxInputBytes)
            {
                result.Success = false;
                result.Error = new GifErrorDiagnostic(GifErrorCode.InputByteLimitExceeded, "GIF input byte limit was reached before the data sub-block chain ended.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "DataSubBlocks"));
                return result;
            }

            int size = reader.ReadByte();
            if (size < 0)
            {
                result.Success = false;
                result.Error = new GifErrorDiagnostic(GifErrorCode.UnterminatedSubBlocks, "Data sub-block chain is missing its terminator.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "DataSubBlocks"));
                return result;
            }

            if (size == 0)
            {
                result.Success = true;
                result.CompressedData = new byte[0];
                return result;
            }

            if (result.BlockCount >= MaxSubBlocks)
            {
                result.Success = false;
                result.Error = new GifErrorDiagnostic(GifErrorCode.SubBlockCountLimitExceeded, "Data sub-block count limit exceeded.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "DataSubBlocks"));
                return result;
            }

            if (size > MaxBytes - result.TotalBytes)
            {
                result.Success = false;
                result.Error = new GifErrorDiagnostic(GifErrorCode.SubBlockByteLimitExceeded, "Data sub-block byte limit exceeded.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "DataSubBlocks"));
                return result;
            }

            if (size > MaxCompressedBytes - result.TotalBytes)
            {
                result.Success = false;
                result.Error = new GifErrorDiagnostic(GifErrorCode.CompressedByteLimitExceeded, "Compressed image byte limit exceeded.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "DataSubBlocks"));
                return result;
            }

            if ((long)size > MaxInputBytes - reader.GetByteOffset())
            {
                result.Success = false;
                result.Error = new GifErrorDiagnostic(GifErrorCode.InputByteLimitExceeded, "Data sub-block payload exceeds the remaining GIF input byte limit.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "DataSubBlocks"));
                return result;
            }

            if (!ConsumeBytes(reader, size))
            {
                result.Success = false;
                result.Error = new GifErrorDiagnostic(GifErrorCode.UnterminatedSubBlocks, "Data sub-block ended before its declared size.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "DataSubBlocks"));
                return result;
            }

            result.BlockCount = result.BlockCount + 1;
            result.TotalBytes = result.TotalBytes + size;
        }
    }


    internal bool ConsumeBytes(IGifByteReader reader, int count)
    {
        if (reader == null)
        {
            return false;
        }

        return reader.Advance(count);
    }


    internal GifImageDataReadResult ValidateMinimumCodeSize(int minimumCodeSize)
    {
        GifImageDataReadResult result = new GifImageDataReadResult();
        result.LzwMinimumCodeSize = minimumCodeSize;
        if (minimumCodeSize < GifConstants.MinimumLzwMinimumCodeSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwMinimumCodeSize, "Image data LZW minimum code size is too small.", new GifDiagnosticPosition(-1, -1, "ImageData"));
            return result;
        }

        if (minimumCodeSize > GifConstants.MaximumLzwMinimumCodeSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwMinimumCodeSize, "Image data LZW minimum code size is too large.", new GifDiagnosticPosition(-1, -1, "ImageData"));
            return result;
        }

        result.Success = true;
        return result;
    }

    internal GifImageDataReadResult CreateResultFromSubBlocks(int minimumCodeSize, GifSubBlockReadResult subBlocks)
    {
        GifImageDataReadResult result = new GifImageDataReadResult();
        result.LzwMinimumCodeSize = minimumCodeSize;

        if (subBlocks == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnterminatedSubBlocks, "Image data sub-blocks are missing.", new GifDiagnosticPosition(-1, -1, "ImageData"));
            return result;
        }

        result.CompressedData = subBlocks.Data;
        result.BlockCount = subBlocks.BlockCount;
        result.TotalBytes = subBlocks.TotalBytes;

        if (!subBlocks.Success)
        {
            result.Error = subBlocks.Error;
            return result;
        }

        result.Success = true;
        return result;
    }
}

internal class GifTrailerReadResult
{
    internal bool Success;
    internal bool HasTrailer;
    internal bool HasExtraData;
    internal int ExtraDataLength;
    internal byte TrailerByte;
    internal GifWarningDiagnostic Warning;
    internal GifErrorDiagnostic Error;

    internal GifTrailerReadResult()
    {
        Success = false;
        HasTrailer = false;
        HasExtraData = false;
        ExtraDataLength = 0;
        TrailerByte = 0;
        Warning = new GifWarningDiagnostic(GifWarningCode.None, string.Empty, null);
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifTrailerValidator
{
    internal GifTrailerReadResult Read(IGifByteReader reader)
    {
        GifTrailerReadResult result = new GifTrailerReadResult();
        if (reader == null)
        {
            result.Error = CreateMissingTrailerError(-1);
            result.Warning = CreateMissingTrailerWarning(-1);
            return result;
        }

        long byteOffset = reader.GetByteOffset();
        int value = reader.ReadByte();
        if (value < 0)
        {
            result.Error = CreateMissingTrailerError(reader.GetByteOffset());
            result.Warning = CreateMissingTrailerWarning(reader.GetByteOffset());
            return result;
        }

        result = ValidateTrailerByte(value, byteOffset);
        if (!result.Success)
        {
            return result;
        }

        AddExtraDataWarning(result, byteOffset + 1, reader.RemainingCount());
        return result;
    }


    internal GifTrailerReadResult Parse(byte[] data)
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(data);
        return Read(reader);
    }

    internal GifTrailerReadResult ValidateTrailerByte(int value, long byteOffset)
    {
        GifTrailerReadResult result = new GifTrailerReadResult();
        if (value < 0)
        {
            result.Error = CreateMissingTrailerError(byteOffset);
            result.Warning = CreateMissingTrailerWarning(byteOffset);
            return result;
        }

        if (value > 255)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnknownBlockType, "GIF trailer byte is invalid.", new GifDiagnosticPosition(byteOffset, -1, "Trailer"));
            return result;
        }

        if (value != GifConstants.Trailer)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnknownBlockType, "GIF trailer byte is invalid.", new GifDiagnosticPosition(byteOffset, -1, "Trailer"));
            return result;
        }

        result.Success = true;
        result.HasTrailer = true;
        result.TrailerByte = (byte)value;
        return result;
    }

    internal void AddExtraDataWarning(GifTrailerReadResult result, long byteOffset, int extraDataLength)
    {
        if (result == null)
        {
            return;
        }

        if (extraDataLength <= 0)
        {
            return;
        }

        result.HasExtraData = true;
        result.ExtraDataLength = extraDataLength;
        result.Warning = new GifWarningDiagnostic(GifWarningCode.ExtraDataAfterTrailer, "GIF stream has extra data after the trailer.", new GifDiagnosticPosition(byteOffset, -1, "Trailer"));
    }

    internal GifErrorDiagnostic CreateMissingTrailerError(long byteOffset)
    {
        return new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "GIF stream ended before the trailer.", new GifDiagnosticPosition(byteOffset, -1, "Trailer"));
    }

    internal GifErrorDiagnostic CreateMissingTrailerErrorForStrictness(GifStrictness strictness, long byteOffset)
    {
        if (strictness == GifStrictness.Strict)
        {
            return new GifErrorDiagnostic(GifErrorCode.MissingTrailer, "Strict GIF decoding requires a trailer marker.", new GifDiagnosticPosition(byteOffset, -1, "Trailer"));
        }

        return CreateMissingTrailerError(byteOffset);
    }

    internal GifWarningDiagnostic CreateMissingTrailerWarning(long byteOffset)
    {
        return new GifWarningDiagnostic(GifWarningCode.MissingTrailer, "GIF stream is missing its trailer.", new GifDiagnosticPosition(byteOffset, -1, "Trailer"));
    }
}

internal class GifBlockReadResult
{
    internal bool Success;
    internal GifDocument Document;
    internal bool HasTrailer;
    internal int FrameCount;
    internal int ExtensionCount;
    internal int ImageDataBlockCount;
    internal int ImageDataBytes;
    internal int SubBlockCount;
    internal int SubBlockBytes;
    internal int BufferedSubBlockBytes;
    internal long DecodedPixelCount;
    internal long BytesRead;
    internal bool HasPendingGraphicControl;
    internal GifGraphicControl PendingGraphicControl;
    internal int PendingGraphicControlBlockIndex;
    internal GifErrorDiagnostic Error;

    internal GifBlockReadResult()
    {
        Success = false;
        Document = new GifDocument();
        HasTrailer = false;
        FrameCount = 0;
        ExtensionCount = 0;
        ImageDataBlockCount = 0;
        ImageDataBytes = 0;
        SubBlockCount = 0;
        SubBlockBytes = 0;
        BufferedSubBlockBytes = 0;
        DecodedPixelCount = 0;
        BytesRead = 0;
        HasPendingGraphicControl = false;
        PendingGraphicControl = new GifGraphicControl();
        PendingGraphicControlBlockIndex = -1;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifBlockReader
{
    internal GifBlockReadResult Read(byte[] data)
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(data);
        return ReadFromReader(reader, new GifDecodeOptions());
    }

    internal GifBlockReadResult ReadBytesWithOptions(byte[] data, GifDecodeOptions? options)
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(data);
        return ReadFromReader(reader, options);
    }

    internal GifBlockReadResult ReadMemoryWithOptions(System.ReadOnlyMemory<byte> data, GifDecodeOptions? options)
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(data);
        return ReadFromReader(reader, options);
    }

    internal GifBlockReadResult ReadStream(System.IO.Stream stream)
    {
        GifStreamByteReader reader = new GifStreamByteReader(stream);
        return ReadFromReader(reader, new GifDecodeOptions());
    }

    internal GifBlockReadResult ReadStreamWithOptions(System.IO.Stream stream, GifDecodeOptions? options)
    {
        GifStreamByteReader reader = new GifStreamByteReader(stream);
        return ReadFromReader(reader, options);
    }

    internal GifBlockReadResult ReadFrameRangeBytesWithOptions(byte[] data, GifDecodeOptions? options, int firstFrameIndex, int lastFrameIndex)
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(data);
        return ReadFrameRangeFromReader(reader, options, firstFrameIndex, lastFrameIndex);
    }

    internal GifBlockReadResult ReadFrameRangeMemoryWithOptions(System.ReadOnlyMemory<byte> data, GifDecodeOptions? options, int firstFrameIndex, int lastFrameIndex)
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(data);
        return ReadFrameRangeFromReader(reader, options, firstFrameIndex, lastFrameIndex);
    }

    internal GifBlockReadResult ReadFrameRangeStreamWithOptions(System.IO.Stream stream, GifDecodeOptions? options, int firstFrameIndex, int lastFrameIndex)
    {
        GifStreamByteReader reader = new GifStreamByteReader(stream);
        return ReadFrameRangeFromReader(reader, options, firstFrameIndex, lastFrameIndex);
    }

    internal GifBlockReadResult ReadFromReader(IGifByteReader reader, GifDecodeOptions? options)
    {
        return ReadFrameRangeFromReader(reader, options, -1, -1);
    }

    internal GifBlockReadResult ReadFrameRangeFromReader(IGifByteReader reader, GifDecodeOptions? options, int firstFrameIndex, int lastFrameIndex)
    {
        GifBlockReadResult result = new GifBlockReadResult();
        if (firstFrameIndex < -1 || lastFrameIndex < -1 || firstFrameIndex > lastFrameIndex)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF frame selection range is invalid.", new GifDiagnosticPosition(-1, firstFrameIndex, "FrameSelection"));
            return result;
        }

        if (reader == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Missing GIF block reader.", new GifDiagnosticPosition(-1, -1, "BlockReader"));
            return result;
        }

        GifDecodeOptions activeOptions = NormalizeOptions(options);

        GifHeaderParser headerParser = new GifHeaderParser();
        GifHeaderReadResult header = headerParser.Read(reader);
        if (!header.Success)
        {
            result.Error = header.Error;
            return FinishFailure(result, reader.GetByteOffset());
        }

        result.Document.Version = header.Version;
        if (!CheckByteLimit(activeOptions, result, reader.GetByteOffset(), "Header"))
        {
            return FinishFailure(result, reader.GetByteOffset());
        }

        GifLogicalScreenDescriptorParser logicalScreenParser = new GifLogicalScreenDescriptorParser();
        GifLogicalScreenDescriptorReadResult logicalScreen = logicalScreenParser.Read(reader);
        if (!logicalScreen.Success)
        {
            result.Error = logicalScreen.Error;
            return FinishFailure(result, reader.GetByteOffset());
        }

        result.Document.LogicalScreen = logicalScreen.LogicalScreen;
        if (activeOptions.Strictness == GifStrictness.Strict && !logicalScreen.LogicalScreen.HasGlobalColorTable && (logicalScreen.PackedFields & (GifConstants.LogicalScreenSortFlagMask | GifConstants.LogicalScreenColorTableSizeMask)) != 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedPackedField, "Logical-screen packed fields specify table metadata without a global color table.", new GifDiagnosticPosition(reader.GetByteOffset() - GifConstants.LogicalScreenDescriptorSize, -1, "LogicalScreenDescriptor"));
            return FinishFailure(result, reader.GetByteOffset());
        }

        if (!CheckLogicalScreenLimits(activeOptions, result, reader.GetByteOffset()))
        {
            return FinishFailure(result, reader.GetByteOffset());
        }

        if (!CheckByteLimit(activeOptions, result, reader.GetByteOffset(), "LogicalScreenDescriptor"))
        {
            return FinishFailure(result, reader.GetByteOffset());
        }

        if (logicalScreen.LogicalScreen.HasGlobalColorTable)
        {
            GifColorTableReader colorTableReader = new GifColorTableReader();
            GifColorTableReadResult colorTable = colorTableReader.Read(reader, logicalScreen.LogicalScreen.GlobalColorTableSize);
            if (!colorTable.Success)
            {
                result.Error = colorTable.Error;
                return FinishFailure(result, reader.GetByteOffset());
            }

            result.Document.GlobalColorTable = colorTable.Palette;
            if (!CheckByteLimit(activeOptions, result, reader.GetByteOffset(), "ColorTable"))
            {
                return FinishFailure(result, reader.GetByteOffset());
            }
        }
        else
        {
            if (!ApplyDefaultGlobalColorTable(activeOptions, result))
            {
                return FinishFailure(result, reader.GetByteOffset());
            }
        }

        while (true)
        {
            long blockOffset = reader.GetByteOffset();
            int blockType = reader.ReadByte();
            if (blockType < 0)
            {
                GifTrailerValidator trailerValidator = new GifTrailerValidator();
                result.Error = trailerValidator.CreateMissingTrailerErrorForStrictness(activeOptions.Strictness, reader.GetByteOffset());
                AddWarning(result, trailerValidator.CreateMissingTrailerWarning(reader.GetByteOffset()));
                return FinishFailure(result, reader.GetByteOffset());
            }

            if (!CheckByteLimit(activeOptions, result, reader.GetByteOffset(), "BlockReader"))
            {
                return FinishFailure(result, reader.GetByteOffset());
            }

            if (blockType == GifConstants.Trailer)
            {
                if (!HandleTrailer(reader, result, blockType, blockOffset))
                {
                    return FinishFailure(result, reader.GetByteOffset());
                }

                if (!CheckByteLimit(activeOptions, result, reader.GetByteOffset(), "Trailer"))
                {
                    return FinishFailure(result, reader.GetByteOffset());
                }

                return FinishSuccess(result, reader.GetByteOffset());
            }

            if (blockType == GifConstants.ExtensionIntroducer)
            {
                if (!ReadExtension(reader, activeOptions, result, blockOffset))
                {
                    return FinishFailure(result, reader.GetByteOffset());
                }

                if (!CheckByteLimit(activeOptions, result, reader.GetByteOffset(), "Extension"))
                {
                    return FinishFailure(result, reader.GetByteOffset());
                }

                continue;
            }

            if (blockType == GifConstants.ImageSeparator)
            {
                if (!CheckFrameLimit(activeOptions, result, blockOffset))
                {
                    return FinishFailure(result, reader.GetByteOffset());
                }

                if (!ReadImageBlockForFrameRange(reader, activeOptions, result, blockOffset, firstFrameIndex, lastFrameIndex))
                {
                    return FinishFailure(result, reader.GetByteOffset());
                }

                if (!CheckByteLimit(activeOptions, result, reader.GetByteOffset(), "ImageData"))
                {
                    return FinishFailure(result, reader.GetByteOffset());
                }

                if (lastFrameIndex >= 0 && result.FrameCount > lastFrameIndex)
                {
                    return FinishSuccess(result, reader.GetByteOffset());
                }

                if (lastFrameIndex < 0 && ShouldStopAfterFirstFrame(activeOptions, result))
                {
                    return FinishSuccess(result, reader.GetByteOffset());
                }

                continue;
            }

            result.Error = new GifErrorDiagnostic(GifErrorCode.UnknownBlockType, "GIF block type is unknown.", new GifDiagnosticPosition(blockOffset, result.FrameCount, "BlockReader"));
            return FinishFailure(result, reader.GetByteOffset());
        }
    }


    internal bool ReadExtension(IGifByteReader reader, GifDecodeOptions options, GifBlockReadResult result, long blockOffset)
    {
        int label = reader.ReadByte();
        if (label < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Extension block is missing its label.", new GifDiagnosticPosition(reader.GetByteOffset(), result.FrameCount, "Extension"));
            return false;
        }

        return ReadExtensionPayload(reader, options, result, blockOffset, (byte)label);
    }


    internal bool HandleTrailer(IGifByteReader reader, GifBlockReadResult result, int trailerByte, long blockOffset)
    {
        GifTrailerValidator validator = new GifTrailerValidator();
        GifTrailerReadResult trailer = validator.ValidateTrailerByte(trailerByte, blockOffset);
        if (!trailer.Success)
        {
            result.Error = trailer.Error;
            return false;
        }

        result.HasTrailer = true;
        MarkTrailerAdjacentExtensions(result.Document);
        RecordRawBlock(result.Document, GifRawBlockKind.Trailer, -1, GifRawBlockScope.Trailer, -1);
        validator.AddExtraDataWarning(trailer, blockOffset + 1, reader.RemainingCount());
        if (trailer.HasExtraData)
        {
            AddWarning(result, trailer.Warning);
        }

        return true;
    }


    internal bool ReadExtensionPayload(IGifByteReader reader, GifDecodeOptions options, GifBlockReadResult result, long blockOffset, byte label)
    {
        if (label == GifConstants.GraphicControlExtensionLabel)
        {
            return ReadGraphicControlExtension(reader, options, result, blockOffset);
        }

        if (label == GifConstants.CommentExtensionLabel)
        {
            return ReadCommentExtensionWithOptions(reader, options, result);
        }

        if (label == GifConstants.PlainTextExtensionLabel)
        {
            return ReadPlainTextExtensionWithOptions(reader, options, result, blockOffset);
        }

        if (label == GifConstants.ApplicationExtensionLabel)
        {
            return ReadApplicationExtensionWithOptions(reader, options, result, blockOffset);
        }

        return ReadUnknownExtension(reader, options, result, blockOffset, label);
    }


    internal bool ReadGraphicControlExtension(IGifByteReader reader, GifDecodeOptions options, GifBlockReadResult result, long blockOffset)
    {
        byte[] data = new byte[GifConstants.GraphicControlExtensionBlockSize + 4];
        data[0] = GifConstants.ExtensionIntroducer;
        data[1] = GifConstants.GraphicControlExtensionLabel;
        int bytesRead = reader.Read(data, 2, data.Length - 2);
        if (bytesRead < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Graphic control extension is shorter than 8 bytes.", new GifDiagnosticPosition(reader.GetByteOffset(), result.FrameCount, "GraphicControlExtension"));
            return false;
        }

        GifGraphicControlExtensionParser parser = new GifGraphicControlExtensionParser();
        GifGraphicControlExtensionReadResult control = parser.ParseExtension(data, blockOffset);
        if (!control.Success)
        {
            result.Error = control.Error;
            return false;
        }

        if (options.Strictness == GifStrictness.Strict && (control.PackedFields & GifConstants.GraphicControlReservedFieldMask) != 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.ReservedFieldValue, "Graphic Control Extension reserved bits must be zero in strict mode.", new GifDiagnosticPosition(blockOffset, result.FrameCount, "GraphicControlExtension"));
            return false;
        }

        int controlIndex = result.Document.GraphicControls.Length;
        result.Document.GraphicControls = AppendGraphicControl(result.Document.GraphicControls, control.GraphicControl);
        RecordRawBlock(result.Document, GifRawBlockKind.GraphicControl, controlIndex, GifRawBlockScope.Document, -1);
        SetPendingGraphicControl(result, control.GraphicControl, result.Document.BlockOrder.Length - 1);
        result.ExtensionCount = result.ExtensionCount + 1;
        return true;
    }


    internal bool ReadCommentExtension(IGifByteReader reader, GifBlockReadResult result)
    {
        return ReadCommentExtensionWithOptions(reader, new GifDecodeOptions(), result);
    }

    internal bool ReadCommentExtensionWithOptions(IGifByteReader reader, GifDecodeOptions options, GifBlockReadResult result)
    {
        GifDecodeOptions activeOptions = NormalizeOptions(options);
        GifSubBlockReader subBlockReader = CreateSubBlockReader(activeOptions, result);
        GifSubBlockReadResult subBlocks = subBlockReader.Read(reader);
        GifCommentExtensionParser parser = new GifCommentExtensionParser();
        GifCommentExtensionReadResult comment = parser.CreateResultFromSubBlocks(subBlocks);
        if (!comment.Success)
        {
            result.Error = comment.Error;
            return false;
        }

        RecordSubBlocks(result, subBlocks.BlockCount, subBlocks.TotalBytes);
        RecordBufferedSubBlockBytes(result, subBlocks.TotalBytes);

        result.Document.Comments = AppendComment(result.Document.Comments, comment.Comment);
        result.Document.Metadata.Comments = AppendComment(result.Document.Metadata.Comments, comment.Comment);
        RecordRawBlock(result.Document, GifRawBlockKind.Comment, result.Document.Comments.Length - 1, GifRawBlockScope.Document, -1);
        result.ExtensionCount = result.ExtensionCount + 1;
        return true;
    }



    internal bool ReadPlainTextExtension(IGifByteReader reader, GifBlockReadResult result, long blockOffset)
    {
        return ReadPlainTextExtensionWithOptions(reader, new GifDecodeOptions(), result, blockOffset);
    }

    internal bool ReadPlainTextExtensionWithOptions(IGifByteReader reader, GifDecodeOptions options, GifBlockReadResult result, long blockOffset)
    {
        byte[] header = new byte[GifConstants.PlainTextExtensionBlockSize + 3];
        header[0] = GifConstants.ExtensionIntroducer;
        header[1] = GifConstants.PlainTextExtensionLabel;
        int bytesRead = reader.Read(header, 2, header.Length - 2);
        if (bytesRead < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Plain text extension header is shorter than 15 bytes.", new GifDiagnosticPosition(reader.GetByteOffset(), result.FrameCount, "PlainTextExtension"));
            return false;
        }

        GifPlainTextExtensionParser parser = new GifPlainTextExtensionParser();
        GifPlainTextExtensionReadResult plainText = parser.ParseHeader(header, blockOffset);
        if (!plainText.Success)
        {
            result.Error = plainText.Error;
            return false;
        }

        GifDecodeOptions activeOptions = NormalizeOptions(options);
        GifSubBlockReader subBlockReader = CreateSubBlockReader(activeOptions, result);
        GifSubBlockReadResult subBlocks = subBlockReader.Read(reader);
        plainText = parser.CreateResultFromSubBlocks(plainText.PlainText, plainText.BlockSize, subBlocks);
        if (!plainText.Success)
        {
            result.Error = plainText.Error;
            return false;
        }

        RecordSubBlocks(result, subBlocks.BlockCount, subBlocks.TotalBytes);

        AttachPendingGraphicControlToPlainText(plainText.PlainText, result);
        if (activeOptions.PlainTextExtensionPolicy == GifPlainTextExtensionPolicy.Ignore)
        {
            GifWarningDiagnostic warning = new GifWarningDiagnostic(GifWarningCode.IgnoredPlainTextExtension, "Plain text extension was ignored by the selected decode policy.", new GifDiagnosticPosition(blockOffset, result.FrameCount, "PlainTextExtension"));
            AddWarning(result, warning);
            result.ExtensionCount = result.ExtensionCount + 1;
            return true;
        }

        RecordBufferedSubBlockBytes(result, subBlocks.TotalBytes);

        result.Document.PlainTextExtensions = AppendPlainText(result.Document.PlainTextExtensions, plainText.PlainText);
        result.Document.Metadata.PlainTextExtensions = AppendPlainText(result.Document.Metadata.PlainTextExtensions, plainText.PlainText);
        RecordRawBlock(result.Document, GifRawBlockKind.PlainText, result.Document.PlainTextExtensions.Length - 1, GifRawBlockScope.PlainText, result.Document.PlainTextExtensions.Length - 1);
        if (activeOptions.PlainTextExtensionPolicy == GifPlainTextExtensionPolicy.Expose)
        {
            result.Document.PlainTextGraphics = AppendPlainText(result.Document.PlainTextGraphics, plainText.PlainText);
        }

        result.ExtensionCount = result.ExtensionCount + 1;
        return true;
    }



    internal bool ReadApplicationExtension(IGifByteReader reader, GifBlockReadResult result, long blockOffset)
    {
        return ReadApplicationExtensionWithOptions(reader, new GifDecodeOptions(), result, blockOffset);
    }

    internal bool ReadApplicationExtensionWithOptions(IGifByteReader reader, GifDecodeOptions options, GifBlockReadResult result, long blockOffset)
    {
        byte[] header = new byte[GifConstants.ApplicationIdentifierSize + GifConstants.ApplicationAuthenticationCodeSize + 3];
        header[0] = GifConstants.ExtensionIntroducer;
        header[1] = GifConstants.ApplicationExtensionLabel;
        int bytesRead = reader.Read(header, 2, header.Length - 2);
        if (bytesRead < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Application extension header is shorter than 14 bytes.", new GifDiagnosticPosition(reader.GetByteOffset(), result.FrameCount, "ApplicationExtension"));
            return false;
        }

        GifApplicationExtensionParser parser = new GifApplicationExtensionParser();
        GifApplicationExtensionReadResult application = parser.ParseHeader(header, blockOffset);
        if (!application.Success)
        {
            result.Error = application.Error;
            return false;
        }

        GifDecodeOptions activeOptions = NormalizeOptions(options);
        GifSubBlockReader subBlockReader = CreateSubBlockReader(activeOptions, result);
        GifSubBlockReadResult subBlocks = subBlockReader.Read(reader);
        application = parser.CreateResultFromSubBlocks(application.Application, application.BlockSize, subBlocks);
        if (!application.Success)
        {
            result.Error = application.Error;
            return false;
        }

        RecordSubBlocks(result, subBlocks.BlockCount, subBlocks.TotalBytes);
        RecordBufferedSubBlockBytes(result, subBlocks.TotalBytes);

        AppendApplicationToDocument(result.Document, application.Application);
        RecordRawBlock(result.Document, GifRawBlockKind.Application, result.Document.ApplicationExtensions.Length - 1, GifRawBlockScope.Document, -1);
        result.ExtensionCount = result.ExtensionCount + 1;
        return true;
    }



    internal bool ReadUnknownExtension(IGifByteReader reader, GifDecodeOptions options, GifBlockReadResult result, long blockOffset, byte label)
    {
        GifUnknownExtensionParser parser = new GifUnknownExtensionParser();
        GifUnknownExtensionReadResult unknown = parser.ParseHeader(new byte[] { GifConstants.ExtensionIntroducer, label }, blockOffset);
        if (!unknown.Success)
        {
            result.Error = unknown.Error;
            return false;
        }

        GifDecodeOptions activeOptions = NormalizeOptions(options);
        GifSubBlockReader subBlockReader = CreateSubBlockReader(activeOptions, result);
        GifSubBlockReadResult subBlocks = subBlockReader.Read(reader);
        unknown = parser.CreateResultFromSubBlocks(unknown.UnknownExtension, subBlocks);
        if (!unknown.Success)
        {
            result.Error = unknown.Error;
            return false;
        }

        RecordSubBlocks(result, subBlocks.BlockCount, subBlocks.TotalBytes);

        if (activeOptions.PreserveUnknownExtensions)
        {
            RecordBufferedSubBlockBytes(result, subBlocks.TotalBytes);
            result.Document.UnknownExtensions = AppendUnknown(result.Document.UnknownExtensions, unknown.UnknownExtension);
            result.Document.Metadata.UnknownExtensions = AppendUnknown(result.Document.Metadata.UnknownExtensions, unknown.UnknownExtension);
            RecordRawBlock(result.Document, GifRawBlockKind.UnknownExtension, result.Document.UnknownExtensions.Length - 1, GifRawBlockScope.Document, -1);
        }

        result.ExtensionCount = result.ExtensionCount + 1;
        return true;
    }


    internal bool ReadImageBlock(IGifByteReader reader, GifBlockReadResult result, long blockOffset)
    {
        return ReadImageBlockWithOptions(reader, new GifDecodeOptions(), result, blockOffset);
    }

    internal bool ReadImageBlockWithOptions(IGifByteReader reader, GifDecodeOptions options, GifBlockReadResult result, long blockOffset)
    {
        return ReadImageBlockForFrameRange(reader, options, result, blockOffset, -1, -1);
    }

    internal bool ReadImageBlockForFrameRange(IGifByteReader reader, GifDecodeOptions options, GifBlockReadResult result, long blockOffset, int firstFrameIndex, int lastFrameIndex)
    {
        byte[] data = new byte[GifConstants.ImageDescriptorSize];
        data[0] = GifConstants.ImageSeparator;
        int bytesRead = reader.Read(data, 1, data.Length - 1);
        if (bytesRead < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Image descriptor is shorter than 10 bytes.", new GifDiagnosticPosition(reader.GetByteOffset(), result.FrameCount, "ImageDescriptor"));
            return false;
        }

        GifImageDescriptorParser parser = new GifImageDescriptorParser();
        GifImageDescriptorReadResult imageDescriptor = parser.ParseDescriptor(data, blockOffset);
        if (!imageDescriptor.Success)
        {
            result.Error = AddFrameContextToError(imageDescriptor.Error, result.FrameCount);
            return false;
        }

        GifFrame frame = imageDescriptor.Frame;
        GifDecodeOptions activeOptions = NormalizeOptions(options);
        if (activeOptions.Strictness == GifStrictness.Strict && (imageDescriptor.PackedFields & GifConstants.ImageReservedFieldMask) != 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.ReservedFieldValue, "Image descriptor reserved bits must be zero in strict mode.", new GifDiagnosticPosition(blockOffset, result.FrameCount, "ImageDescriptor"));
            return false;
        }

        if (activeOptions.Strictness == GifStrictness.Strict && !frame.HasLocalColorTable && (imageDescriptor.PackedFields & (GifConstants.ImageSortFlagMask | GifConstants.ImageColorTableSizeMask)) != 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedPackedField, "Image packed fields specify table metadata without a local color table.", new GifDiagnosticPosition(blockOffset, result.FrameCount, "ImageDescriptor"));
            return false;
        }
        if (!CheckFrameDimensionLimits(activeOptions, result, frame, blockOffset))
        {
            return false;
        }

        if (!CheckDecodedPixelLimit(activeOptions, result, frame, blockOffset))
        {
            return false;
        }

        GifImageRectangleValidator validator = new GifImageRectangleValidator();
        GifImageRectangleValidationResult validation = validator.ValidateAtFrameIndex(frame, result.Document.LogicalScreen, result.FrameCount);
        if (!validation.Success)
        {
            result.Error = validation.Error;
            return false;
        }

        if (frame.HasLocalColorTable)
        {
            GifColorTableReader colorTableReader = new GifColorTableReader();
            GifColorTableReadResult colorTable = colorTableReader.Read(reader, frame.LocalColorTableSize);
            if (!colorTable.Success)
            {
                result.Error = AddFrameContextToError(colorTable.Error, result.FrameCount);
                return false;
            }

            frame.LocalColorTable = colorTable.Palette;
        }

        GifImageDataReader imageDataReader = new GifImageDataReader();
        imageDataReader.MaxSubBlocks = activeOptions.MaxSubBlocks - result.SubBlockCount;
        imageDataReader.MaxBytes = activeOptions.MaxSubBlockBytes - result.SubBlockBytes;
        imageDataReader.MaxCompressedBytes = activeOptions.MaxCompressedBytes - result.ImageDataBytes;
        imageDataReader.MaxMemoryBytes = activeOptions.MaxMemoryBytes - result.BufferedSubBlockBytes;
        imageDataReader.MaxInputBytes = activeOptions.MaxBytes;
        GifImageDataReadResult imageData = new GifImageDataReadResult();
        bool retainImageData = !activeOptions.MetadataOnly;
        if (firstFrameIndex >= 0 && result.FrameCount < firstFrameIndex)
        {
            retainImageData = false;
        }

        if (lastFrameIndex >= 0 && result.FrameCount > lastFrameIndex)
        {
            retainImageData = false;
        }

        if (!retainImageData)
        {
            imageData = imageDataReader.ReadMetadataOnly(reader);
        }
        else
        {
            imageData = imageDataReader.Read(reader);
        }

        if (!imageData.Success)
        {
            result.Error = AddFrameContextToError(imageData.Error, result.FrameCount);
            return false;
        }

        frame.LzwMinimumCodeSize = imageData.LzwMinimumCodeSize;
        frame.CompressedImageData = imageData.CompressedData;
        AttachPendingGraphicControlToFrame(frame, result);
        result.ImageDataBlockCount = result.ImageDataBlockCount + imageData.BlockCount;
        result.ImageDataBytes = result.ImageDataBytes + imageData.TotalBytes;
        RecordSubBlocks(result, imageData.BlockCount, imageData.TotalBytes);
        if (retainImageData)
        {
            RecordBufferedSubBlockBytes(result, imageData.TotalBytes);
        }
        result.DecodedPixelCount = result.DecodedPixelCount + CalculateFramePixelCount(frame);
        result.Document.Frames = AppendFrame(result.Document.Frames, frame);
        RecordRawBlock(result.Document, GifRawBlockKind.Image, result.FrameCount, GifRawBlockScope.Frame, result.FrameCount);
        result.FrameCount = result.FrameCount + 1;
        return true;
    }

    internal GifErrorDiagnostic AddFrameContextToError(GifErrorDiagnostic error, int frameIndex)
    {
        if (error == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, new GifDiagnosticPosition(-1, frameIndex, "ImageData"));
        }

        int resolvedFrameIndex = error.Position.FrameIndex;
        if (resolvedFrameIndex < 0)
        {
            resolvedFrameIndex = frameIndex;
        }

        return new GifErrorDiagnostic(error.Code, error.Message, new GifDiagnosticPosition(error.Position.ByteOffset, resolvedFrameIndex, error.Position.BlockName));
    }



    internal void SetPendingGraphicControl(GifBlockReadResult result, GifGraphicControl control, int blockOrderIndex)
    {
        if (result == null)
        {
            return;
        }

        result.PendingGraphicControl = CopyGraphicControl(control);
        result.HasPendingGraphicControl = true;
        result.PendingGraphicControlBlockIndex = blockOrderIndex;
    }

    internal void AttachPendingGraphicControlToFrame(GifFrame frame, GifBlockReadResult result)
    {
        if (frame == null)
        {
            return;
        }

        if (result == null)
        {
            return;
        }

        if (!result.HasPendingGraphicControl)
        {
            return;
        }

        GifGraphicControl control = CopyGraphicControl(result.PendingGraphicControl);
        frame.GraphicControl = control;
        frame.DelayHundredths = control.DelayHundredths;
        frame.DisposalMethod = control.DisposalMethod;
        frame.HasTransparency = control.HasTransparency;
        frame.TransparentColorIndex = control.TransparentColorIndex;
        frame.UserInputFlag = control.UserInputFlag;
        SetRawBlockScope(result, GifRawBlockScope.Frame, result.FrameCount);
        ClearPendingGraphicControl(result);
    }

    internal void AttachPendingGraphicControlToPlainText(GifPlainTextExtension plainText, GifBlockReadResult result)
    {
        if (plainText == null)
        {
            return;
        }

        if (result == null)
        {
            return;
        }

        if (!result.HasPendingGraphicControl)
        {
            return;
        }

        plainText.GraphicControl = CopyGraphicControl(result.PendingGraphicControl);
        SetRawBlockScope(result, GifRawBlockScope.PlainText, result.Document.PlainTextExtensions.Length);
        ClearPendingGraphicControl(result);
    }

    internal void ClearPendingGraphicControl(GifBlockReadResult result)
    {
        if (result == null)
        {
            return;
        }

        result.HasPendingGraphicControl = false;
        result.PendingGraphicControl = new GifGraphicControl();
        result.PendingGraphicControlBlockIndex = -1;
    }

    internal void SetRawBlockScope(GifBlockReadResult result, GifRawBlockScope scope, int scopeIndex)
    {
        if (result == null || result.Document == null)
        {
            return;
        }

        int blockIndex = result.PendingGraphicControlBlockIndex;
        if (blockIndex < 0 || blockIndex >= result.Document.BlockOrder.Length)
        {
            return;
        }

        GifRawBlock block = result.Document.BlockOrder[blockIndex];
        if (block == null)
        {
            return;
        }

        block.Scope = scope;
        block.ScopeIndex = scopeIndex;
    }

    internal void MarkTrailerAdjacentExtensions(GifDocument document)
    {
        if (document == null || document.BlockOrder == null)
        {
            return;
        }

        int blockIndex = document.BlockOrder.Length - 1;
        while (blockIndex >= 0)
        {
            GifRawBlock block = document.BlockOrder[blockIndex];
            if (block != null)
            {
                if (block.Kind == GifRawBlockKind.Image || block.Kind == GifRawBlockKind.PlainText)
                {
                    return;
                }

                block.Scope = GifRawBlockScope.Trailer;
                block.ScopeIndex = -1;
            }

            blockIndex = blockIndex - 1;
        }
    }

    internal void RecordRawBlock(GifDocument document, GifRawBlockKind kind, int index, GifRawBlockScope scope, int scopeIndex)
    {
        if (document == null)
        {
            return;
        }

        GifRawBlock block = new GifRawBlock();
        block.Kind = kind;
        block.Index = index;
        block.Scope = scope;
        block.ScopeIndex = scopeIndex;
        document.BlockOrder = AppendRawBlock(document.BlockOrder, block);
    }

    internal GifGraphicControl CopyGraphicControl(GifGraphicControl control)
    {
        GifGraphicControl copy = new GifGraphicControl();
        if (control == null)
        {
            return copy;
        }

        copy.DisposalMethod = control.DisposalMethod;
        copy.UserInputFlag = control.UserInputFlag;
        copy.HasTransparency = control.HasTransparency;
        copy.DelayHundredths = control.DelayHundredths;
        copy.TransparentColorIndex = control.TransparentColorIndex;
        return copy;
    }

    internal bool CheckLogicalScreenLimits(GifDecodeOptions options, GifBlockReadResult result, long byteOffset)
    {
        if (result == null)
        {
            return false;
        }

        if (result.Document == null)
        {
            return true;
        }

        return CheckDimensionLimits(options, result, result.Document.LogicalScreen.Width, result.Document.LogicalScreen.Height, byteOffset, -1, "LogicalScreenDescriptor");
    }

    internal bool CheckFrameDimensionLimits(GifDecodeOptions options, GifBlockReadResult result, GifFrame frame, long byteOffset)
    {
        if (result == null)
        {
            return false;
        }

        if (frame == null)
        {
            return true;
        }

        int frameIndex = result.FrameCount;
        return CheckDimensionLimits(options, result, frame.Width, frame.Height, byteOffset, frameIndex, "ImageDescriptor");
    }

    internal bool CheckDimensionLimits(GifDecodeOptions options, GifBlockReadResult result, int width, int height, long byteOffset, int frameIndex, string blockName)
    {
        if (options == null)
        {
            return true;
        }

        if (width > options.MaxWidth)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DimensionLimitExceeded, "GIF width exceeds the configured decode limit.", new GifDiagnosticPosition(byteOffset, frameIndex, blockName));
            return false;
        }

        if (height > options.MaxHeight)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DimensionLimitExceeded, "GIF height exceeds the configured decode limit.", new GifDiagnosticPosition(byteOffset, frameIndex, blockName));
            return false;
        }

        return true;
    }

    internal bool CheckFrameLimit(GifDecodeOptions options, GifBlockReadResult result, long byteOffset)
    {
        if (options == null)
        {
            return true;
        }

        if (result == null)
        {
            return false;
        }

        if (result.FrameCount >= options.MaxFrames)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.FrameLimitExceeded, "GIF frame count exceeds the configured decode limit.", new GifDiagnosticPosition(byteOffset, result.FrameCount, "ImageDescriptor"));
            return false;
        }

        return true;
    }

    internal bool CheckDecodedPixelLimit(GifDecodeOptions options, GifBlockReadResult result, GifFrame frame, long byteOffset)
    {
        if (options == null)
        {
            return true;
        }

        if (result == null)
        {
            return false;
        }

        long framePixels = CalculateFramePixelCount(frame);
        if (framePixels > (long)options.MaxDecodedPixels - result.DecodedPixelCount)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelLimitExceeded, "GIF decoded pixel count exceeds the configured decode limit.", new GifDiagnosticPosition(byteOffset, result.FrameCount, "ImageData"));
            return false;
        }

        return true;
    }

    internal long CalculateFramePixelCount(GifFrame frame)
    {
        if (frame == null)
        {
            return 0;
        }

        return (long)frame.Width * (long)frame.Height;
    }

    internal bool CheckByteLimit(GifDecodeOptions options, GifBlockReadResult result, long byteOffset, string blockName)
    {
        if (options == null)
        {
            return true;
        }

        if (result == null)
        {
            return false;
        }

        if (byteOffset > options.MaxBytes)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InputByteLimitExceeded, "GIF byte count exceeds the configured decode input limit.", new GifDiagnosticPosition(byteOffset, result.FrameCount, blockName));
            return false;
        }

        return true;
    }

    internal GifSubBlockReader CreateSubBlockReader(GifDecodeOptions options, GifBlockReadResult result)
    {
        int remainingBlocks = options.MaxSubBlocks - result.SubBlockCount;
        int remainingBytes = options.MaxSubBlockBytes - result.SubBlockBytes;
        GifSubBlockReader reader = new GifSubBlockReader(remainingBlocks, remainingBytes);
        reader.MaxMemoryBytes = options.MaxMemoryBytes - result.BufferedSubBlockBytes;
        reader.MaxInputBytes = options.MaxBytes;
        return reader;
    }

    internal void RecordSubBlocks(GifBlockReadResult result, int blockCount, int byteCount)
    {
        result.SubBlockCount = result.SubBlockCount + blockCount;
        result.SubBlockBytes = result.SubBlockBytes + byteCount;
    }

    internal void RecordBufferedSubBlockBytes(GifBlockReadResult result, int byteCount)
    {
        result.BufferedSubBlockBytes = result.BufferedSubBlockBytes + byteCount;
    }

    internal GifDecodeOptions NormalizeOptions(GifDecodeOptions? options)
    {
        if (options == null)
        {
            return new GifDecodeOptions();
        }

        return options;
    }

    internal bool ApplyDefaultGlobalColorTable(GifDecodeOptions options, GifBlockReadResult result)
    {
        if (options == null)
        {
            return true;
        }

        if (options.DefaultGlobalColorTable == null || options.DefaultGlobalColorTable.Length == 0)
        {
            return true;
        }

        GifActiveColorTableResolver resolver = new GifActiveColorTableResolver();
        GifActiveColorTableResult active = resolver.ResolveDefaultGlobalColorTable(options, -1);
        if (!active.Success)
        {
            return true;
        }

        result.Document.GlobalColorTable = active.Palette;
        return true;
    }

    internal bool ShouldStopAfterFirstFrame(GifDecodeOptions options, GifBlockReadResult result)
    {
        if (options == null)
        {
            return false;
        }

        if (!options.FirstFrameOnly)
        {
            return false;
        }

        if (result == null)
        {
            return false;
        }

        return result.FrameCount > 0;
    }

    internal GifBlockReadResult FinishSuccess(GifBlockReadResult result, long byteOffset)
    {
        result.Success = true;
        result.BytesRead = byteOffset;
        UpdateStatistics(result);
        return result;
    }

    internal GifBlockReadResult FinishFailure(GifBlockReadResult result, long byteOffset)
    {
        result.BytesRead = byteOffset;
        UpdateStatistics(result);
        return result;
    }

    internal void UpdateStatistics(GifBlockReadResult result)
    {
        result.Document.DecodeStatistics.BytesRead = result.BytesRead;
        result.Document.DecodeStatistics.FramesRead = result.FrameCount;
        result.Document.DecodeStatistics.ExtensionCount = result.ExtensionCount;
        result.Document.DecodeStatistics.CompressedBytesRead = result.ImageDataBytes;
        result.Document.DecodeStatistics.DecodedPixelCount = result.DecodedPixelCount;
        result.Document.DecodeStatistics.WarningCount = result.Document.Warnings.Length;
    }

    internal void AddWarning(GifBlockReadResult result, GifWarningDiagnostic warning)
    {
        if (result == null)
        {
            return;
        }

        if (warning == null)
        {
            return;
        }

        if (warning.Code == GifWarningCode.None)
        {
            return;
        }

        result.Document.Warnings = AppendWarning(result.Document.Warnings, warning);
    }

    internal void AppendApplicationToDocument(GifDocument document, GifApplicationExtension application)
    {
        document.ApplicationExtensions = AppendApplication(document.ApplicationExtensions, application);
        document.Metadata.ApplicationExtensions = AppendApplication(document.Metadata.ApplicationExtensions, application);
        if (application != null)
        {
            if (application.IsLoopExtension)
            {
                document.LoopCount = application.LoopCount;
                document.Metadata.LoopCount = application.LoopCount;
            }
        }
    }

    internal GifFrame[] AppendFrame(GifFrame[] items, GifFrame item)
    {
        if (items == null)
        {
            items = new GifFrame[0];
        }

        GifFrame[] result = new GifFrame[items.Length + 1];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index];
            index = index + 1;
        }

        result[items.Length] = item;
        return result;
    }

    internal GifCommentExtension[] AppendComment(GifCommentExtension[] items, GifCommentExtension item)
    {
        if (items == null)
        {
            items = new GifCommentExtension[0];
        }

        GifCommentExtension[] result = new GifCommentExtension[items.Length + 1];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index];
            index = index + 1;
        }

        result[items.Length] = item;
        return result;
    }

    internal GifApplicationExtension[] AppendApplication(GifApplicationExtension[] items, GifApplicationExtension item)
    {
        if (items == null)
        {
            items = new GifApplicationExtension[0];
        }

        GifApplicationExtension[] result = new GifApplicationExtension[items.Length + 1];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index];
            index = index + 1;
        }

        result[items.Length] = item;
        return result;
    }

    internal GifGraphicControl[] AppendGraphicControl(GifGraphicControl[] items, GifGraphicControl item)
    {
        if (items == null)
        {
            items = new GifGraphicControl[0];
        }

        GifGraphicControl[] result = new GifGraphicControl[items.Length + 1];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index];
            index = index + 1;
        }

        result[items.Length] = item;
        return result;
    }

    internal GifRawBlock[] AppendRawBlock(GifRawBlock[] items, GifRawBlock item)
    {
        if (items == null)
        {
            items = new GifRawBlock[0];
        }

        GifRawBlock[] result = new GifRawBlock[items.Length + 1];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index];
            index = index + 1;
        }

        result[items.Length] = item;
        return result;
    }

    internal GifPlainTextExtension[] AppendPlainText(GifPlainTextExtension[] items, GifPlainTextExtension item)
    {
        if (items == null)
        {
            items = new GifPlainTextExtension[0];
        }

        GifPlainTextExtension[] result = new GifPlainTextExtension[items.Length + 1];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index];
            index = index + 1;
        }

        result[items.Length] = item;
        return result;
    }

    internal GifUnknownExtension[] AppendUnknown(GifUnknownExtension[] items, GifUnknownExtension item)
    {
        if (items == null)
        {
            items = new GifUnknownExtension[0];
        }

        GifUnknownExtension[] result = new GifUnknownExtension[items.Length + 1];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index];
            index = index + 1;
        }

        result[items.Length] = item;
        return result;
    }

    internal GifWarningDiagnostic[] AppendWarning(GifWarningDiagnostic[] items, GifWarningDiagnostic item)
    {
        if (items == null)
        {
            items = new GifWarningDiagnostic[0];
        }

        GifWarningDiagnostic[] result = new GifWarningDiagnostic[items.Length + 1];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index];
            index = index + 1;
        }

        result[items.Length] = item;
        return result;
    }
}
