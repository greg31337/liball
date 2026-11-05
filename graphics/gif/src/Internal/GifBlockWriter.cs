namespace gif;

internal class GifHeaderWriteResult
{
    internal bool Success;
    internal GifVersion Version;
    internal string Header;
    internal byte[] EncodedData;
    internal int BytesWritten;
    internal GifErrorDiagnostic Error;

    internal GifHeaderWriteResult()
    {
        Success = false;
        Version = GifVersion.Unknown;
        Header = string.Empty;
        EncodedData = new byte[0];
        BytesWritten = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifHeaderWriter
{
    internal GifHeaderWriteResult Write(IGifByteWriter writer, GifVersion version)
    {
        GifHeaderWriteResult result = new GifHeaderWriteResult();
        result.Version = version;

        byte[] header = CreateHeaderBytes(version);
        if (header.Length != GifConstants.HeaderSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnsupportedVersion, "GIF header version is unsupported.", new GifDiagnosticPosition(-1, -1, "Header"));
            return result;
        }

        result.Header = CreateHeaderText(version);

        if (writer == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Missing header writer.", new GifDiagnosticPosition(-1, -1, "Header"));
            return result;
        }

        if (!writer.Write(header))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DestinationWriteFailed, "Failed to write GIF header.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "Header"));
            return result;
        }

        result.Success = true;
        result.BytesWritten = GifConstants.HeaderSize;
        return result;
    }

    internal GifHeaderWriteResult Create(GifVersion version)
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        IGifByteWriter writer = new GifStreamByteWriter(stream);
        GifHeaderWriteResult result = Write(writer, version);
        if (result.Success)
        {
            result.EncodedData = stream.ToArray();
        }

        return result;
    }

    internal byte[] CreateHeaderBytes(GifVersion version)
    {
        if (version == GifVersion.Gif87a)
        {
            return new byte[] { GifConstants.SignatureByteG, GifConstants.SignatureByteI, GifConstants.SignatureByteF, GifConstants.VersionByte8, GifConstants.Version87Byte7, GifConstants.VersionByteA };
        }

        if (version == GifVersion.Gif89a)
        {
            return new byte[] { GifConstants.SignatureByteG, GifConstants.SignatureByteI, GifConstants.SignatureByteF, GifConstants.VersionByte8, GifConstants.Version89Byte9, GifConstants.VersionByteA };
        }

        return new byte[0];
    }

    internal string CreateHeaderText(GifVersion version)
    {
        if (version == GifVersion.Gif87a)
        {
            return GifConstants.Header87a;
        }

        if (version == GifVersion.Gif89a)
        {
            return GifConstants.Header89a;
        }

        return string.Empty;
    }
}

internal class GifTrailerWriteResult
{
    internal bool Success;
    internal byte[] EncodedData;
    internal int BytesWritten;
    internal GifErrorDiagnostic Error;

    internal GifTrailerWriteResult()
    {
        Success = false;
        EncodedData = new byte[0];
        BytesWritten = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifTrailerWriter
{
    internal GifTrailerWriteResult Write(IGifByteWriter writer)
    {
        GifTrailerWriteResult result = new GifTrailerWriteResult();
        if (writer == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Missing trailer writer.", new GifDiagnosticPosition(-1, -1, "Trailer"));
            return result;
        }

        if (!writer.WriteByte(GifConstants.Trailer))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DestinationWriteFailed, "Failed to write GIF trailer.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "Trailer"));
            return result;
        }

        result.Success = true;
        result.BytesWritten = 1;
        return result;
    }

    internal GifTrailerWriteResult Create()
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        IGifByteWriter writer = new GifStreamByteWriter(stream);
        GifTrailerWriteResult result = Write(writer);
        if (result.Success)
        {
            result.EncodedData = stream.ToArray();
        }

        return result;
    }
}

internal class GifLogicalScreenDescriptorWriteResult
{
    internal bool Success;
    internal byte[] EncodedData;
    internal byte PackedFields;
    internal int BytesWritten;
    internal GifErrorDiagnostic Error;

    internal GifLogicalScreenDescriptorWriteResult()
    {
        Success = false;
        EncodedData = new byte[0];
        PackedFields = 0;
        BytesWritten = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifLogicalScreenDescriptorWriter
{
    internal GifLogicalScreenDescriptorWriteResult Write(IGifByteWriter writer, GifLogicalScreen logicalScreen)
    {
        GifLogicalScreenDescriptorWriteResult result = new GifLogicalScreenDescriptorWriteResult();
        if (writer == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Missing logical screen descriptor writer.", new GifDiagnosticPosition(-1, -1, "LogicalScreenDescriptor"));
            return result;
        }

        byte[] data = CreateDescriptorBytes(logicalScreen);
        if (data.Length != GifConstants.LogicalScreenDescriptorSize)
        {
            result.Error = CreateValidationError(logicalScreen, writer.GetByteOffset());
            return result;
        }

        if (!writer.Write(data))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DestinationWriteFailed, "Failed to write logical screen descriptor.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "LogicalScreenDescriptor"));
            return result;
        }

        result.Success = true;
        result.EncodedData = data;
        result.PackedFields = data[4];
        result.BytesWritten = GifConstants.LogicalScreenDescriptorSize;
        return result;
    }

    internal GifLogicalScreenDescriptorWriteResult Create(GifLogicalScreen logicalScreen)
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        IGifByteWriter writer = new GifStreamByteWriter(stream);
        GifLogicalScreenDescriptorWriteResult result = Write(writer, logicalScreen);
        if (result.Success)
        {
            result.EncodedData = stream.ToArray();
        }

        return result;
    }

    internal byte[] CreateDescriptorBytes(GifLogicalScreen logicalScreen)
    {
        if (!CanEncode(logicalScreen))
        {
            return new byte[0];
        }

        byte[] data = new byte[GifConstants.LogicalScreenDescriptorSize];
        GifBinaryWriter.WriteUInt16LittleEndian(data, 0, logicalScreen.Width);
        GifBinaryWriter.WriteUInt16LittleEndian(data, 2, logicalScreen.Height);
        data[4] = (byte)CreatePackedFields(logicalScreen);
        data[5] = (byte)logicalScreen.BackgroundColorIndex;
        data[6] = logicalScreen.PixelAspectRatio;
        return data;
    }

    internal int CreatePackedFields(GifLogicalScreen logicalScreen)
    {
        if (logicalScreen == null)
        {
            return -1;
        }

        int colorResolutionCode = 0;
        if (logicalScreen.ColorResolution > 0)
        {
            if (logicalScreen.ColorResolution > 8)
            {
                return -1;
            }

            colorResolutionCode = logicalScreen.ColorResolution - 1;
        }

        int sizeCode = 0;
        if (logicalScreen.HasGlobalColorTable)
        {
            sizeCode = ComputeGlobalColorTableSizeCode(logicalScreen.GlobalColorTableSize);
            if (sizeCode < 0)
            {
                return -1;
            }
        }

        int packed = (colorResolutionCode << 4) | sizeCode;
        if (logicalScreen.HasGlobalColorTable)
        {
            packed = packed | GifConstants.LogicalScreenGlobalColorTableFlagMask;
        }

        if (logicalScreen.SortFlag)
        {
            packed = packed | GifConstants.LogicalScreenSortFlagMask;
        }

        return packed;
    }

    internal int ComputeGlobalColorTableSizeCode(int colorCount)
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

    internal bool CanEncode(GifLogicalScreen logicalScreen)
    {
        if (logicalScreen == null)
        {
            return false;
        }

        if (logicalScreen.Width < 0 || logicalScreen.Width > 65535)
        {
            return false;
        }

        if (logicalScreen.Height < 0 || logicalScreen.Height > 65535)
        {
            return false;
        }

        if (logicalScreen.BackgroundColorIndex < 0 || logicalScreen.BackgroundColorIndex > 255)
        {
            return false;
        }

        if (logicalScreen.ColorResolution < 0 || logicalScreen.ColorResolution > 8)
        {
            return false;
        }

        if (logicalScreen.HasGlobalColorTable && ComputeGlobalColorTableSizeCode(logicalScreen.GlobalColorTableSize) < 0)
        {
            return false;
        }

        return true;
    }

    internal GifErrorDiagnostic CreateValidationError(GifLogicalScreen logicalScreen, long byteOffset)
    {
        if (logicalScreen == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.MissingLogicalScreenDescriptor, "Logical screen descriptor is missing.", new GifDiagnosticPosition(byteOffset, -1, "LogicalScreenDescriptor"));
        }

        if (logicalScreen.HasGlobalColorTable && ComputeGlobalColorTableSizeCode(logicalScreen.GlobalColorTableSize) < 0)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Global color table size is not a valid GIF size.", new GifDiagnosticPosition(byteOffset, -1, "LogicalScreenDescriptor"));
        }

        return new GifErrorDiagnostic(GifErrorCode.DimensionLimitExceeded, "Logical screen descriptor contains an unencodable value.", new GifDiagnosticPosition(byteOffset, -1, "LogicalScreenDescriptor"));
    }
}

internal class GifColorTableWriteResult
{
    internal bool Success;
    internal byte[] EncodedData;
    internal int ColorCount;
    internal int BytesWritten;
    internal GifErrorDiagnostic Error;

    internal GifColorTableWriteResult()
    {
        Success = false;
        EncodedData = new byte[0];
        ColorCount = 0;
        BytesWritten = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifColorTableWriter
{
    internal GifColorTableWriteResult Write(IGifByteWriter writer, GifPalette palette)
    {
        GifColorTableWriteResult result = new GifColorTableWriteResult();
        if (writer == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Missing color table writer.", new GifDiagnosticPosition(-1, -1, "ColorTable"));
            return result;
        }

        byte[] data = CreateColorTableBytes(palette);
        if (data.Length == 0)
        {
            result.Error = CreateValidationError(palette, writer.GetByteOffset());
            return result;
        }

        if (!writer.Write(data))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DestinationWriteFailed, "Failed to write color table.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "ColorTable"));
            return result;
        }

        result.Success = true;
        result.EncodedData = data;
        result.ColorCount = palette.Count;
        result.BytesWritten = data.Length;
        return result;
    }

    internal GifColorTableWriteResult Create(GifPalette palette)
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        IGifByteWriter writer = new GifStreamByteWriter(stream);
        GifColorTableWriteResult result = Write(writer, palette);
        if (result.Success)
        {
            result.EncodedData = stream.ToArray();
        }

        return result;
    }

    internal byte[] CreateColorTableBytes(GifPalette palette)
    {
        if (!CanEncode(palette))
        {
            return new byte[0];
        }

        byte[] data = new byte[palette.Count * GifConstants.ColorTableEntrySize];
        int colorIndex = 0;
        int byteIndex = 0;
        while (colorIndex < palette.Count)
        {
            GifColor color = palette.Colors[colorIndex];
            data[byteIndex] = color.Red;
            data[byteIndex + 1] = color.Green;
            data[byteIndex + 2] = color.Blue;
            colorIndex = colorIndex + 1;
            byteIndex = byteIndex + GifConstants.ColorTableEntrySize;
        }

        return data;
    }

    internal bool CanEncode(GifPalette palette)
    {
        GifColorTableValidator validator = new GifColorTableValidator();
        return validator.IsValidPalette(palette);
    }

    internal GifErrorDiagnostic CreateValidationError(GifPalette palette, long byteOffset)
    {
        if (palette == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Color table is missing.", new GifDiagnosticPosition(byteOffset, -1, "ColorTable"));
        }

        return new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Color table cannot be encoded.", new GifDiagnosticPosition(byteOffset, -1, "ColorTable"));
    }
}

internal class GifImageDescriptorWriteResult
{
    internal bool Success;
    internal byte[] EncodedData;
    internal byte PackedFields;
    internal int BytesWritten;
    internal GifErrorDiagnostic Error;

    internal GifImageDescriptorWriteResult()
    {
        Success = false;
        EncodedData = new byte[0];
        PackedFields = 0;
        BytesWritten = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifImageDescriptorWriter
{
    internal GifImageDescriptorWriteResult Write(IGifByteWriter writer, GifFrame frame)
    {
        GifImageDescriptorWriteResult result = new GifImageDescriptorWriteResult();
        if (writer == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Missing image descriptor writer.", new GifDiagnosticPosition(-1, -1, "ImageDescriptor"));
            return result;
        }

        byte[] data = CreateDescriptorBytes(frame);
        if (data.Length != GifConstants.ImageDescriptorSize)
        {
            result.Error = CreateValidationError(frame, writer.GetByteOffset());
            return result;
        }

        if (!writer.Write(data))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DestinationWriteFailed, "Failed to write image descriptor.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "ImageDescriptor"));
            return result;
        }

        result.Success = true;
        result.EncodedData = data;
        result.PackedFields = data[9];
        result.BytesWritten = GifConstants.ImageDescriptorSize;
        return result;
    }

    internal GifImageDescriptorWriteResult Create(GifFrame frame)
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        IGifByteWriter writer = new GifStreamByteWriter(stream);
        GifImageDescriptorWriteResult result = Write(writer, frame);
        if (result.Success)
        {
            result.EncodedData = stream.ToArray();
        }

        return result;
    }

    internal byte[] CreateDescriptorBytes(GifFrame frame)
    {
        if (!CanEncode(frame))
        {
            return new byte[0];
        }

        byte[] data = new byte[GifConstants.ImageDescriptorSize];
        data[0] = GifConstants.ImageSeparator;
        GifBinaryWriter.WriteUInt16LittleEndian(data, 1, frame.Left);
        GifBinaryWriter.WriteUInt16LittleEndian(data, 3, frame.Top);
        GifBinaryWriter.WriteUInt16LittleEndian(data, 5, frame.Width);
        GifBinaryWriter.WriteUInt16LittleEndian(data, 7, frame.Height);
        data[9] = (byte)CreatePackedFields(frame);
        return data;
    }

    internal int CreatePackedFields(GifFrame frame)
    {
        if (frame == null)
        {
            return -1;
        }

        GifPackedFields packedFields = new GifPackedFields();
        return packedFields.CreateImagePackedFields(frame.HasLocalColorTable, frame.Interlace, frame.LocalColorTableSortFlag, frame.LocalColorTableSize);
    }

    internal bool CanEncode(GifFrame frame)
    {
        if (frame == null)
        {
            return false;
        }

        if (frame.Left < 0 || frame.Left > 65535)
        {
            return false;
        }

        if (frame.Top < 0 || frame.Top > 65535)
        {
            return false;
        }

        if (frame.Width < 0 || frame.Width > 65535)
        {
            return false;
        }

        if (frame.Height < 0 || frame.Height > 65535)
        {
            return false;
        }

        if (CreatePackedFields(frame) < 0)
        {
            return false;
        }

        return true;
    }

    internal GifErrorDiagnostic CreateValidationError(GifFrame frame, long byteOffset)
    {
        if (frame == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Image descriptor frame is missing.", new GifDiagnosticPosition(byteOffset, -1, "ImageDescriptor"));
        }

        if (frame.HasLocalColorTable && CreatePackedFields(frame) < 0)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Local color table size is not a valid GIF size.", new GifDiagnosticPosition(byteOffset, -1, "ImageDescriptor"));
        }

        return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Image descriptor contains an unencodable rectangle value.", new GifDiagnosticPosition(byteOffset, -1, "ImageDescriptor"));
    }
}

internal class GifGraphicControlExtensionWriteResult
{
    internal bool Success;
    internal byte[] EncodedData;
    internal byte PackedFields;
    internal int BytesWritten;
    internal GifErrorDiagnostic Error;

    internal GifGraphicControlExtensionWriteResult()
    {
        Success = false;
        EncodedData = new byte[0];
        PackedFields = 0;
        BytesWritten = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifGraphicControlExtensionWriter
{
    internal GifGraphicControlExtensionWriteResult Write(IGifByteWriter writer, GifGraphicControl graphicControl)
    {
        GifGraphicControlExtensionWriteResult result = new GifGraphicControlExtensionWriteResult();
        if (writer == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Missing graphic control extension writer.", new GifDiagnosticPosition(-1, -1, "GraphicControlExtension"));
            return result;
        }

        byte[] data = CreateExtensionBytes(graphicControl);
        if (data.Length != GifConstants.GraphicControlExtensionBlockSize + 4)
        {
            result.Error = CreateValidationError(graphicControl, writer.GetByteOffset());
            return result;
        }

        if (!writer.Write(data))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DestinationWriteFailed, "Failed to write graphic control extension.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "GraphicControlExtension"));
            return result;
        }

        result.Success = true;
        result.EncodedData = data;
        result.PackedFields = data[3];
        result.BytesWritten = data.Length;
        return result;
    }

    internal GifGraphicControlExtensionWriteResult Create(GifGraphicControl graphicControl)
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        IGifByteWriter writer = new GifStreamByteWriter(stream);
        GifGraphicControlExtensionWriteResult result = Write(writer, graphicControl);
        if (result.Success)
        {
            result.EncodedData = stream.ToArray();
        }

        return result;
    }

    internal byte[] CreateExtensionBytes(GifGraphicControl graphicControl)
    {
        if (!CanEncode(graphicControl))
        {
            return new byte[0];
        }

        byte[] data = new byte[GifConstants.GraphicControlExtensionBlockSize + 4];
        data[0] = GifConstants.ExtensionIntroducer;
        data[1] = GifConstants.GraphicControlExtensionLabel;
        data[2] = GifConstants.GraphicControlExtensionBlockSize;
        data[3] = (byte)CreatePackedFields(graphicControl);
        GifBinaryWriter.WriteUInt16LittleEndian(data, 4, graphicControl.DelayHundredths);
        data[6] = (byte)NormalizeTransparentColorIndex(graphicControl);
        data[7] = GifConstants.BlockTerminator;
        return data;
    }

    internal int CreatePackedFields(GifGraphicControl graphicControl)
    {
        if (graphicControl == null)
        {
            return -1;
        }

        GifPackedFields packedFields = new GifPackedFields();
        return packedFields.CreateGraphicControlPackedFields((int)graphicControl.DisposalMethod, graphicControl.UserInputFlag, graphicControl.HasTransparency);
    }

    internal int NormalizeTransparentColorIndex(GifGraphicControl graphicControl)
    {
        if (graphicControl == null)
        {
            return -1;
        }

        if (graphicControl.TransparentColorIndex < 0)
        {
            return 0;
        }

        return graphicControl.TransparentColorIndex;
    }

    internal bool CanEncode(GifGraphicControl graphicControl)
    {
        if (graphicControl == null)
        {
            return false;
        }

        if (CreatePackedFields(graphicControl) < 0)
        {
            return false;
        }

        if (graphicControl.DelayHundredths < 0 || graphicControl.DelayHundredths > 65535)
        {
            return false;
        }

        if (graphicControl.TransparentColorIndex < -1 || graphicControl.TransparentColorIndex > 255)
        {
            return false;
        }

        if (graphicControl.HasTransparency && graphicControl.TransparentColorIndex < 0)
        {
            return false;
        }

        return true;
    }

    internal GifErrorDiagnostic CreateValidationError(GifGraphicControl graphicControl, long byteOffset)
    {
        if (graphicControl == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Graphic control extension is missing.", new GifDiagnosticPosition(byteOffset, -1, "GraphicControlExtension"));
        }

        return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Graphic control extension contains an unencodable value.", new GifDiagnosticPosition(byteOffset, -1, "GraphicControlExtension"));
    }
}

internal class GifCommentExtensionWriteResult
{
    internal bool Success;
    internal byte[] EncodedData;
    internal int BlockCount;
    internal int TotalBytes;
    internal int BytesWritten;
    internal GifErrorDiagnostic Error;

    internal GifCommentExtensionWriteResult()
    {
        Success = false;
        EncodedData = new byte[0];
        BlockCount = 0;
        TotalBytes = 0;
        BytesWritten = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifCommentExtensionWriter
{
    internal GifCommentExtensionWriteResult Write(IGifByteWriter writer, GifCommentExtension comment)
    {
        GifCommentExtensionWriteResult result = new GifCommentExtensionWriteResult();
        if (writer == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Missing comment extension writer.", new GifDiagnosticPosition(-1, -1, "CommentExtension"));
            return result;
        }

        if (comment == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Comment extension is missing.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "CommentExtension"));
            return result;
        }

        byte[] payload = CreatePayloadBytes(comment);
        long startOffset = writer.GetByteOffset();
        if (!writer.WriteByte(GifConstants.ExtensionIntroducer))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DestinationWriteFailed, "Failed to write comment extension introducer.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "CommentExtension"));
            return result;
        }

        if (!writer.WriteByte(GifConstants.CommentExtensionLabel))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DestinationWriteFailed, "Failed to write comment extension label.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "CommentExtension"));
            return result;
        }

        GifSubBlockWriter subBlockWriter = new GifSubBlockWriter(GifConstants.DefaultMaxSubBlockBytes);
        GifSubBlockWriteResult subBlocks = subBlockWriter.Write(writer, payload);
        result.BlockCount = subBlocks.BlockCount;
        result.TotalBytes = subBlocks.TotalBytes;
        if (!subBlocks.Success)
        {
            result.Error = subBlocks.Error;
            return result;
        }

        result.Success = true;
        result.BytesWritten = (int)(writer.GetByteOffset() - startOffset);
        return result;
    }

    internal GifCommentExtensionWriteResult Create(GifCommentExtension comment)
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        IGifByteWriter writer = new GifStreamByteWriter(stream);
        GifCommentExtensionWriteResult result = Write(writer, comment);
        if (result.Success)
        {
            result.EncodedData = stream.ToArray();
        }

        return result;
    }

    internal byte[] CreatePayloadBytes(GifCommentExtension comment)
    {
        if (comment == null)
        {
            return new byte[0];
        }

        if (comment.Data != null)
        {
            if (comment.Data.Length > 0)
            {
                return comment.Data;
            }
        }

        return EncodeText(comment.Text);
    }

    internal byte[] EncodeText(string text)
    {
        if (text == null)
        {
            return new byte[0];
        }

        byte[] data = new byte[text.Length];
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            if (value > 255)
            {
                data[index] = 63;
            }
            else
            {
                data[index] = (byte)value;
            }

            index = index + 1;
        }

        return data;
    }
}

internal class GifPlainTextExtensionWriteResult
{
    internal bool Success;
    internal byte[] EncodedData;
    internal byte BlockSize;
    internal int BlockCount;
    internal int TotalBytes;
    internal int BytesWritten;
    internal GifErrorDiagnostic Error;

    internal GifPlainTextExtensionWriteResult()
    {
        Success = false;
        EncodedData = new byte[0];
        BlockSize = 0;
        BlockCount = 0;
        TotalBytes = 0;
        BytesWritten = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifPlainTextExtensionWriter
{
    internal GifPlainTextExtensionWriteResult Write(IGifByteWriter writer, GifPlainTextExtension plainText)
    {
        GifPlainTextExtensionWriteResult result = new GifPlainTextExtensionWriteResult();
        if (writer == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Missing plain text extension writer.", new GifDiagnosticPosition(-1, -1, "PlainTextExtension"));
            return result;
        }

        byte[] header = CreateHeaderBytes(plainText);
        if (header.Length == 0)
        {
            result.Error = CreateValidationError(plainText, writer.GetByteOffset());
            return result;
        }

        long startOffset = writer.GetByteOffset();
        if (!writer.Write(header))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DestinationWriteFailed, "Failed to write plain text extension header.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "PlainTextExtension"));
            return result;
        }

        byte[] payload = CreatePayloadBytes(plainText);
        GifSubBlockWriter subBlockWriter = new GifSubBlockWriter(GifConstants.DefaultMaxSubBlockBytes);
        GifSubBlockWriteResult subBlocks = subBlockWriter.Write(writer, payload);
        result.BlockCount = subBlocks.BlockCount;
        result.TotalBytes = subBlocks.TotalBytes;
        if (!subBlocks.Success)
        {
            result.Error = subBlocks.Error;
            return result;
        }

        result.Success = true;
        result.BlockSize = header[2];
        result.BytesWritten = (int)(writer.GetByteOffset() - startOffset);
        return result;
    }

    internal GifPlainTextExtensionWriteResult Create(GifPlainTextExtension plainText)
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        IGifByteWriter writer = new GifStreamByteWriter(stream);
        GifPlainTextExtensionWriteResult result = Write(writer, plainText);
        if (result.Success)
        {
            result.EncodedData = stream.ToArray();
        }

        return result;
    }

    internal byte[] CreateHeaderBytes(GifPlainTextExtension plainText)
    {
        if (!CanEncode(plainText))
        {
            return new byte[0];
        }

        byte[] data = new byte[GifConstants.PlainTextExtensionBlockSize + 3];
        data[0] = GifConstants.ExtensionIntroducer;
        data[1] = GifConstants.PlainTextExtensionLabel;
        data[2] = GifConstants.PlainTextExtensionBlockSize;
        GifBinaryWriter.WriteUInt16LittleEndian(data, 3, plainText.TextGridLeft);
        GifBinaryWriter.WriteUInt16LittleEndian(data, 5, plainText.TextGridTop);
        GifBinaryWriter.WriteUInt16LittleEndian(data, 7, plainText.TextGridWidth);
        GifBinaryWriter.WriteUInt16LittleEndian(data, 9, plainText.TextGridHeight);
        data[11] = plainText.CharacterCellWidth;
        data[12] = plainText.CharacterCellHeight;
        data[13] = plainText.ForegroundColorIndex;
        data[14] = plainText.BackgroundColorIndex;
        return data;
    }

    internal byte[] CreatePayloadBytes(GifPlainTextExtension plainText)
    {
        if (plainText == null)
        {
            return new byte[0];
        }

        if (plainText.TextData != null)
        {
            if (plainText.TextData.Length > 0)
            {
                return plainText.TextData;
            }
        }

        return EncodeText(plainText.Text);
    }

    internal byte[] EncodeText(string text)
    {
        if (text == null)
        {
            return new byte[0];
        }

        byte[] data = new byte[text.Length];
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            if (value > 255)
            {
                data[index] = 63;
            }
            else
            {
                data[index] = (byte)value;
            }

            index = index + 1;
        }

        return data;
    }

    internal bool CanEncode(GifPlainTextExtension plainText)
    {
        if (plainText == null)
        {
            return false;
        }

        if (!CanEncodeUInt16(plainText.TextGridLeft))
        {
            return false;
        }

        if (!CanEncodeUInt16(plainText.TextGridTop))
        {
            return false;
        }

        if (!CanEncodeUInt16(plainText.TextGridWidth))
        {
            return false;
        }

        if (!CanEncodeUInt16(plainText.TextGridHeight))
        {
            return false;
        }

        return true;
    }

    internal bool CanEncodeUInt16(int value)
    {
        if (value < 0)
        {
            return false;
        }

        if (value > 65535)
        {
            return false;
        }

        return true;
    }

    internal GifErrorDiagnostic CreateValidationError(GifPlainTextExtension plainText, long byteOffset)
    {
        if (plainText == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Plain text extension is missing.", new GifDiagnosticPosition(byteOffset, -1, "PlainTextExtension"));
        }

        return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Plain text extension contains an unencodable value.", new GifDiagnosticPosition(byteOffset, -1, "PlainTextExtension"));
    }
}

internal class GifApplicationExtensionWriteResult
{
    internal bool Success;
    internal byte[] EncodedData;
    internal byte BlockSize;
    internal int BlockCount;
    internal int TotalBytes;
    internal int BytesWritten;
    internal GifErrorDiagnostic Error;

    internal GifApplicationExtensionWriteResult()
    {
        Success = false;
        EncodedData = new byte[0];
        BlockSize = 0;
        BlockCount = 0;
        TotalBytes = 0;
        BytesWritten = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifApplicationExtensionWriter
{
    internal GifApplicationExtensionWriteResult Write(IGifByteWriter writer, GifApplicationExtension application)
    {
        GifApplicationExtensionWriteResult result = new GifApplicationExtensionWriteResult();
        if (writer == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Missing application extension writer.", new GifDiagnosticPosition(-1, -1, "ApplicationExtension"));
            return result;
        }

        byte[] header = CreateHeaderBytes(application);
        if (header.Length == 0)
        {
            result.Error = CreateValidationError(application, writer.GetByteOffset());
            return result;
        }

        long startOffset = writer.GetByteOffset();
        if (!writer.Write(header))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DestinationWriteFailed, "Failed to write application extension header.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "ApplicationExtension"));
            return result;
        }

        byte[] payload = CreatePayloadBytes(application);
        GifSubBlockWriter subBlockWriter = new GifSubBlockWriter(GifConstants.DefaultMaxSubBlockBytes);
        GifSubBlockWriteResult subBlocks = subBlockWriter.Write(writer, payload);
        result.BlockCount = subBlocks.BlockCount;
        result.TotalBytes = subBlocks.TotalBytes;
        if (!subBlocks.Success)
        {
            result.Error = subBlocks.Error;
            return result;
        }

        result.Success = true;
        result.BlockSize = header[2];
        result.BytesWritten = (int)(writer.GetByteOffset() - startOffset);
        return result;
    }

    internal GifApplicationExtensionWriteResult WriteNetscapeLoopExtension(IGifByteWriter writer, int loopCount)
    {
        GifApplicationExtensionWriteResult result = new GifApplicationExtensionWriteResult();
        if (writer == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Missing application extension writer.", new GifDiagnosticPosition(-1, -1, "ApplicationExtension"));
            return result;
        }

        if (!CanEncodeLoopCount(loopCount))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Netscape loop extension contains an unencodable loop count.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "ApplicationExtension"));
            return result;
        }

        GifApplicationExtension application = CreateNetscapeLoopApplication(loopCount);
        return Write(writer, application);
    }

    internal GifApplicationExtensionWriteResult Create(GifApplicationExtension application)
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        IGifByteWriter writer = new GifStreamByteWriter(stream);
        GifApplicationExtensionWriteResult result = Write(writer, application);
        if (result.Success)
        {
            result.EncodedData = stream.ToArray();
        }

        return result;
    }

    internal GifApplicationExtensionWriteResult CreateNetscapeLoopExtension(int loopCount)
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        IGifByteWriter writer = new GifStreamByteWriter(stream);
        GifApplicationExtensionWriteResult result = WriteNetscapeLoopExtension(writer, loopCount);
        if (result.Success)
        {
            result.EncodedData = stream.ToArray();
        }

        return result;
    }

    internal byte[] CreateHeaderBytes(GifApplicationExtension application)
    {
        if (!CanEncode(application))
        {
            return new byte[0];
        }

        int blockSize = GifConstants.ApplicationIdentifierSize + GifConstants.ApplicationAuthenticationCodeSize;
        byte[] data = new byte[blockSize + 3];
        data[0] = GifConstants.ExtensionIntroducer;
        data[1] = GifConstants.ApplicationExtensionLabel;
        data[2] = (byte)blockSize;
        if (IsNetscapeLoopApplication(application))
        {
            WriteFixedText(data, 3, GifConstants.ApplicationIdentifierSize, GifConstants.NetscapeLoopApplicationIdentifier);
            WriteFixedText(data, 3 + GifConstants.ApplicationIdentifierSize, GifConstants.ApplicationAuthenticationCodeSize, GifConstants.NetscapeLoopAuthenticationCode);
        }
        else
        {
            WriteFixedText(data, 3, GifConstants.ApplicationIdentifierSize, application.Identifier);
            WriteFixedText(data, 3 + GifConstants.ApplicationIdentifierSize, GifConstants.ApplicationAuthenticationCodeSize, application.AuthenticationCode);
        }

        return data;
    }

    internal byte[] CreatePayloadBytes(GifApplicationExtension application)
    {
        if (application == null)
        {
            return new byte[0];
        }

        if (IsNetscapeLoopApplication(application))
        {
            return CreateNetscapeLoopPayload(application.LoopCount);
        }

        if (application.Data == null)
        {
            return new byte[0];
        }

        return application.Data;
    }

    internal bool CanEncode(GifApplicationExtension application)
    {
        if (application == null)
        {
            return false;
        }

        if (IsNetscapeLoopApplication(application))
        {
            return CanEncodeLoopCount(application.LoopCount);
        }

        if (!CanEncodeFixedText(application.Identifier, GifConstants.ApplicationIdentifierSize))
        {
            return false;
        }

        if (!CanEncodeFixedText(application.AuthenticationCode, GifConstants.ApplicationAuthenticationCodeSize))
        {
            return false;
        }

        return true;
    }

    internal GifApplicationExtension CreateNetscapeLoopApplication(int loopCount)
    {
        GifApplicationExtension application = new GifApplicationExtension();
        if (!CanEncodeLoopCount(loopCount))
        {
            return application;
        }

        application.Identifier = GifConstants.NetscapeLoopApplicationIdentifier;
        application.AuthenticationCode = GifConstants.NetscapeLoopAuthenticationCode;
        application.Data = CreateNetscapeLoopPayload(loopCount);
        application.IsLoopExtension = true;
        application.LoopCount = loopCount;
        return application;
    }

    internal byte[] CreateNetscapeLoopPayload(int loopCount)
    {
        if (!CanEncodeLoopCount(loopCount))
        {
            return new byte[0];
        }

        byte[] data = new byte[GifConstants.NetscapeLoopExtensionPayloadSize];
        data[0] = GifConstants.NetscapeLoopExtensionSubBlockId;
        GifBinaryWriter.WriteUInt16LittleEndian(data, 1, loopCount);
        return data;
    }

    internal bool IsNetscapeLoopApplication(GifApplicationExtension application)
    {
        if (application == null)
        {
            return false;
        }

        if (application.IsLoopExtension)
        {
            return true;
        }

        if (application.LoopCount >= 0)
        {
            return true;
        }

        return false;
    }

    internal bool CanEncodeLoopCount(int loopCount)
    {
        if (loopCount < 0)
        {
            return false;
        }

        if (loopCount > 65535)
        {
            return false;
        }

        return true;
    }

    internal bool CanEncodeFixedText(string text, int maximumLength)
    {
        if (maximumLength < 0)
        {
            return false;
        }

        if (text == null)
        {
            return true;
        }

        if (text.Length > maximumLength)
        {
            return false;
        }

        return true;
    }

    internal bool WriteFixedText(byte[] destination, int offset, int count, string text)
    {
        if (destination == null)
        {
            return false;
        }

        if (offset < 0 || count < 0)
        {
            return false;
        }

        if (offset > destination.Length)
        {
            return false;
        }

        if (count > destination.Length - offset)
        {
            return false;
        }

        int index = 0;
        while (index < count)
        {
            byte value = 0;
            if (text != null)
            {
                if (index < text.Length)
                {
                    char character = text[index];
                    if (character > 255)
                    {
                        value = 63;
                    }
                    else
                    {
                        value = (byte)character;
                    }
                }
            }

            destination[offset + index] = value;
            index = index + 1;
        }

        return true;
    }

    internal GifErrorDiagnostic CreateValidationError(GifApplicationExtension application, long byteOffset)
    {
        if (application == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Application extension is missing.", new GifDiagnosticPosition(byteOffset, -1, "ApplicationExtension"));
        }

        return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Application extension contains an unencodable value.", new GifDiagnosticPosition(byteOffset, -1, "ApplicationExtension"));
    }
}

internal class GifUnknownExtensionWriteResult
{
    internal bool Success;
    internal byte[] EncodedData;
    internal int BlockCount;
    internal int TotalBytes;
    internal int BytesWritten;
    internal GifErrorDiagnostic Error;

    internal GifUnknownExtensionWriteResult()
    {
        Success = false;
        EncodedData = new byte[0];
        BlockCount = 0;
        TotalBytes = 0;
        BytesWritten = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifUnknownExtensionWriter
{
    internal GifUnknownExtensionWriteResult Write(IGifByteWriter writer, GifUnknownExtension unknownExtension)
    {
        GifUnknownExtensionWriteResult result = new GifUnknownExtensionWriteResult();
        if (writer == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Missing unknown extension writer.", new GifDiagnosticPosition(-1, -1, "UnknownExtension"));
            return result;
        }

        if (!CanEncode(unknownExtension))
        {
            result.Error = CreateValidationError(unknownExtension, writer.GetByteOffset());
            return result;
        }

        long startOffset = writer.GetByteOffset();
        if (!writer.WriteByte(GifConstants.ExtensionIntroducer))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DestinationWriteFailed, "Failed to write unknown extension introducer.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "UnknownExtension"));
            return result;
        }

        if (!writer.WriteByte(unknownExtension.Label))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DestinationWriteFailed, "Failed to write unknown extension label.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "UnknownExtension"));
            return result;
        }

        GifSubBlockWriter subBlockWriter = new GifSubBlockWriter(GifConstants.DefaultMaxSubBlockBytes);
        GifSubBlockWriteResult subBlocks = subBlockWriter.Write(writer, CreatePayloadBytes(unknownExtension));
        result.BlockCount = subBlocks.BlockCount;
        result.TotalBytes = subBlocks.TotalBytes;
        if (!subBlocks.Success)
        {
            result.Error = subBlocks.Error;
            return result;
        }

        result.Success = true;
        result.BytesWritten = (int)(writer.GetByteOffset() - startOffset);
        return result;
    }

    internal GifUnknownExtensionWriteResult Create(GifUnknownExtension unknownExtension)
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        IGifByteWriter writer = new GifStreamByteWriter(stream);
        GifUnknownExtensionWriteResult result = Write(writer, unknownExtension);
        if (result.Success)
        {
            result.EncodedData = stream.ToArray();
        }

        return result;
    }

    internal byte[] CreatePayloadBytes(GifUnknownExtension unknownExtension)
    {
        if (unknownExtension == null)
        {
            return new byte[0];
        }

        if (unknownExtension.Data == null)
        {
            return new byte[0];
        }

        return unknownExtension.Data;
    }

    internal bool CanEncode(GifUnknownExtension unknownExtension)
    {
        if (unknownExtension == null)
        {
            return false;
        }

        GifUnknownExtensionParser parser = new GifUnknownExtensionParser();
        return parser.IsUnknownExtensionLabel(unknownExtension.Label);
    }

    internal GifErrorDiagnostic CreateValidationError(GifUnknownExtension unknownExtension, long byteOffset)
    {
        if (unknownExtension == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Unknown extension is missing.", new GifDiagnosticPosition(byteOffset, -1, "UnknownExtension"));
        }

        return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Unknown extension contains a known extension label.", new GifDiagnosticPosition(byteOffset, -1, "UnknownExtension"));
    }
}
