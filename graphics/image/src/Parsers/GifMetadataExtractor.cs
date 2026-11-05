using System.Text;

namespace image;

internal class GifMetadataExtractor
{
    internal const string NetscapeIdentifier = "NETSCAPE";
    internal const string NetscapeAuthenticationCode = "2.0";
    internal const string XmpIdentifier = "XMP Data";
    internal const string XmpAuthenticationCode = "XMP";
    internal const string IccIdentifier = "ICCRGBG1";
    internal const string IccAuthenticationCode = "012";
    internal const int XmpMagicTrailerLength = 258;

    internal static void ExtractExtension(ImageMetadataDocument document, MetadataDirectory directory, int label, byte[] header, GifSubBlockResult result, ImageMetadataReaderOptions options, GifParserState state)
    {
        if (label == 249)
        {
            ExtractGraphicControl(document, directory, header, state);
        }
        else if (label == 254)
        {
            ExtractComment(document, directory, result);
        }
        else if (label == 255)
        {
            ExtractApplication(document, directory, header, result, options, state);
        }
        else if (label == 1 && state != null)
        {
            ClearPendingGraphicControl(state);
        }
    }

    internal static void ExtractGraphicControl(ImageMetadataDocument document, MetadataDirectory directory, byte[] header, GifParserState state)
    {
        if (header == null || header.Length < 4 || directory == null)
        {
            return;
        }

        int packed = header[0];
        int disposalMethod = (packed >> 2) & 7;
        bool userInput = (packed & 2) != 0;
        bool transparent = (packed & 1) != 0;
        int delay = header[1] | (header[2] << 8);
        int transparentIndex = header[3];
        long headerOffset = directory.Offset + 3;
        GifBlockParser.AddTag(directory, "Gif.GraphicControl.DisposalMethod", "Disposal Method", MetadataValue.FromInteger(disposalMethod), headerOffset, 1, DisposalMethodName(disposalMethod));
        GifBlockParser.AddTag(directory, "Gif.GraphicControl.UserInputFlag", "User Input Flag", MetadataValue.FromBoolean(userInput), headerOffset, 1, "");
        GifBlockParser.AddTag(directory, "Gif.GraphicControl.TransparencyFlag", "Transparency Flag", MetadataValue.FromBoolean(transparent), headerOffset, 1, "");
        GifBlockParser.AddTag(directory, "Gif.GraphicControl.DelayTime", "Delay Time", MetadataValue.FromInteger(delay), headerOffset + 1, 2, delay.ToString() + " cs");
        GifBlockParser.AddTag(directory, "Gif.GraphicControl.DelayMilliseconds", "Delay Milliseconds", MetadataValue.FromInteger(delay * 10), headerOffset + 1, 2, "");
        GifBlockParser.AddTag(directory, "Gif.GraphicControl.TransparentColorIndex", "Transparent Color Index", MetadataValue.FromInteger(transparentIndex), headerOffset + 3, 1, transparent ? transparentIndex.ToString() : "Not used");

        if ((packed & 224) != 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "gif.graphic-control.reserved.invalid", "GIF graphic control reserved bits were not zero.", "GraphicControlExtension", headerOffset, 1);
        }

        if (disposalMethod > 3)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "gif.graphic-control.disposal.invalid", "GIF graphic control disposal method was reserved.", "GraphicControlExtension", headerOffset, 1);
        }

        if (state != null)
        {
            state.PendingGraphicControl = true;
            state.PendingDelayHundredths = delay;
            state.PendingDisposalMethod = disposalMethod;
            state.PendingUserInput = userInput;
            state.PendingTransparency = transparent;
            state.PendingTransparentColorIndex = transparentIndex;
        }
    }

    internal static void ExtractComment(ImageMetadataDocument document, MetadataDirectory directory, GifSubBlockResult result)
    {
        if (directory == null || result == null || !result.DataAvailable)
        {
            return;
        }

        string text = MetadataStringReader.DecodeAscii(result.Data);
        MetadataTag tag = GifBlockParser.AddTag(directory, "Gif.Comment.Text", "Comment", MetadataValue.FromString(text), result.Offset + 1, result.EncodedLength, text);
        tag.RawValue = GifBlockParser.CopyBytes(result.Data);

        if (!IsAscii(result.Data))
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "gif.comment.ascii.invalid", "GIF comment contained bytes outside 7-bit ASCII.", "CommentExtension", result.Offset, result.EncodedLength);
        }
    }

    internal static void ExtractApplication(ImageMetadataDocument document, MetadataDirectory directory, byte[] header, GifSubBlockResult? result, ImageMetadataReaderOptions options, GifParserState state)
    {
        if (directory == null || header == null || header.Length < 11)
        {
            return;
        }

        string identifier = MetadataStringReader.DecodeAsciiRange(header, 0, 8, null, "ApplicationExtension");
        string authenticationCode = MetadataStringReader.DecodeAsciiRange(header, 8, 3, null, "ApplicationExtension");
        long headerOffset = directory.Offset + 3;
        MetadataTag identifierTag = GifBlockParser.AddTag(directory, "Gif.Application.Identifier", "Application Identifier", MetadataValue.FromString(identifier), headerOffset, 8, identifier);
        identifierTag.RawValue = CopyRange(header, 0, 8);
        MetadataTag authenticationTag = GifBlockParser.AddTag(directory, "Gif.Application.AuthenticationCode", "Authentication Code", MetadataValue.FromString(authenticationCode), headerOffset + 8, 3, authenticationCode);
        authenticationTag.RawValue = CopyRange(header, 8, 3);
        GifBlockParser.AddTag(directory, "Gif.Application.IdentifierAndAuthenticationCode", "Application Identifier and Authentication Code", MetadataValue.FromString(identifier + authenticationCode), headerOffset, 11, identifier + authenticationCode);

        if (!IsPrintableAsciiRange(header, 0, 8))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "gif.application.identifier.invalid", "GIF application identifier was not printable ASCII.", "ApplicationExtension", headerOffset, 8);
        }

        if (result != null && result.DataAvailable)
        {
            MetadataTag dataTag = GifBlockParser.AddTag(directory, "Gif.Application.Data", "Application Data", MetadataValue.FromBytes(result.Data), result.Offset, result.EncodedLength, "");
            dataTag.RawValue = GifBlockParser.CopyBytes(result.Data);
        }

        if (identifier == NetscapeIdentifier && authenticationCode == NetscapeAuthenticationCode)
        {
            ExtractNetscapeLoop(document, directory, result, state);
        }
        else if (identifier == XmpIdentifier && authenticationCode == XmpAuthenticationCode)
        {
            ExtractXmp(document, directory, result, options);
        }
        else if (identifier == IccIdentifier && authenticationCode == IccAuthenticationCode)
        {
            ExtractIcc(document, directory, result, options);
        }
    }

    internal static void ExtractNetscapeLoop(ImageMetadataDocument document, MetadataDirectory directory, GifSubBlockResult? result, GifParserState state)
    {
        if (result == null || !result.DataAvailable)
        {
            return;
        }

        if (result.Count != 1 || result.Data.Length != 3 || result.EncodedData.Length < 5 || result.EncodedData[0] != 3 || result.Data[0] != 1)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "gif.netscape.loop.invalid", "GIF Netscape loop application data was invalid.", "ApplicationExtension", result.Offset, result.EncodedLength);
            return;
        }

        int loopCount = result.Data[1] | (result.Data[2] << 8);
        GifBlockParser.AddTag(directory, "Gif.Application.LoopCount", "Loop Count", MetadataValue.FromInteger(loopCount), result.Offset + 2, 2, loopCount == 0 ? "Infinite" : loopCount.ToString());
        if (state != null)
        {
            state.LoopExtensionPresent = true;
            state.LoopCount = loopCount;
        }
    }

    internal static void ExtractXmp(ImageMetadataDocument document, MetadataDirectory directory, GifSubBlockResult? result, ImageMetadataReaderOptions options)
    {
        if (!ShouldParseXmp(options) || result == null || !result.DataAvailable)
        {
            return;
        }

        int packetLength = XmpPacketLength(result.EncodedData);
        if (packetLength < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "gif.xmp.trailer.invalid", "GIF XMP application data did not contain the required magic trailer.", "ApplicationExtension", result.Offset, result.EncodedLength);
            return;
        }

        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Xmp;
        profile.Family = MetadataFamily.Xmp;
        profile.Key = "Xmp.GIF.ApplicationExtension";
        profile.Name = "XMP";
        profile.MimeType = "application/rdf+xml";
        profile.Offset = result.Offset;
        profile.Length = packetLength;
        if (packetLength <= GetMaxXmlBytes(options))
        {
            byte[] packet = CopyRange(result.EncodedData, 0, packetLength);
            profile.Data = packet;
            profile.Text = Encoding.UTF8.GetString(packet);
            if (!MetadataStringReader.IsValidUtf8(packet))
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "gif.xmp.utf8.invalid", "GIF XMP packet was not valid UTF-8.", "ApplicationExtension", result.Offset, packetLength);
            }
        }
        else
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "gif.xmp.profile.limit", "GIF XMP packet exceeded the configured XML limit.", "ApplicationExtension", result.Offset, packetLength);
        }

        document.AddProfile(profile);
        XmpPacketParser.ParseProfile(document, profile, options, "GIF/ApplicationExtension/XMP");
        GifBlockParser.AddTag(directory, "Gif.Application.XmpPacketByteLength", "XMP Packet Byte Length", MetadataValue.FromInteger(packetLength), result.Offset, packetLength, "");
    }

    internal static void ExtractIcc(ImageMetadataDocument document, MetadataDirectory directory, GifSubBlockResult? result, ImageMetadataReaderOptions options)
    {
        if (!ShouldParseIcc(options) || result == null || !result.DataAvailable)
        {
            return;
        }

        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Icc;
        profile.Family = MetadataFamily.Icc;
        profile.Key = "Icc.GIF.ApplicationExtension";
        profile.Name = "ICC Profile";
        profile.MimeType = "application/vnd.iccprofile";
        profile.Offset = result.Offset + 1;
        profile.Length = result.Data.Length;
        if (profile.Length <= GetMaxProfileBytes(options))
        {
            profile.Data = GifBlockParser.CopyBytes(result.Data);
        }
        else
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "gif.icc.profile.limit", "GIF ICC profile exceeded the configured profile limit.", "ApplicationExtension", result.Offset, profile.Length);
        }

        document.AddProfile(profile);
        IccProfileParser.ParseProfile(document, profile, options, "GIF/ApplicationExtension/ICC", "ApplicationExtension");
        GifBlockParser.AddTag(directory, "Gif.Application.IccProfileByteLength", "ICC Profile Byte Length", MetadataValue.FromInteger(profile.Length), result.Offset, result.EncodedLength, "");
    }

    internal static void ApplyGraphicControl(ImageMetadataFrame frame, GifParserState state)
    {
        if (frame == null || state == null || !state.PendingGraphicControl)
        {
            return;
        }

        frame.DurationMilliseconds = state.PendingDelayHundredths * 10;
        frame.HasAlpha = state.PendingTransparency;
        ClearPendingGraphicControl(state);
    }

    internal static void FinishAnimation(ImageMetadataDocument document, GifParserState state)
    {
        if (document == null)
        {
            return;
        }

        bool animated = document.Frames.Count > 1 || (document.Frames.Count > 0 && state != null && state.LoopExtensionPresent);
        foreach (ImageMetadataFrame frame in document.Frames)
        {
            if (frame != null)
            {
                frame.IsAnimated = animated;
            }
        }

        MetadataDirectory? directory = FindLogicalScreenDirectory(document);
        if (directory != null)
        {
            GifBlockParser.AddTag(directory, "Gif.Animation.IsAnimated", "Animated", MetadataValue.FromBoolean(animated), directory.Offset, directory.Length, "");
            GifBlockParser.AddTag(directory, "Gif.Animation.FrameCount", "Frame Count", MetadataValue.FromInteger(document.Frames.Count), directory.Offset, directory.Length, "");
            if (state != null && state.LoopExtensionPresent)
            {
                GifBlockParser.AddTag(directory, "Gif.Animation.LoopCount", "Loop Count", MetadataValue.FromInteger(state.LoopCount), directory.Offset, directory.Length, state.LoopCount == 0 ? "Infinite" : state.LoopCount.ToString());
            }
        }
    }

    internal static void ClearPendingGraphicControl(GifParserState state)
    {
        if (state == null)
        {
            return;
        }

        state.PendingGraphicControl = false;
        state.PendingDelayHundredths = 0;
        state.PendingDisposalMethod = 0;
        state.PendingUserInput = false;
        state.PendingTransparency = false;
        state.PendingTransparentColorIndex = 0;
    }

    internal static int XmpPacketLength(byte[] data)
    {
        if (data == null || data.Length < XmpMagicTrailerLength)
        {
            return -1;
        }

        int packetLength = data.Length - XmpMagicTrailerLength;
        if (data[packetLength] != 1 || data[data.Length - 1] != 0)
        {
            return -1;
        }

        int index = 0;
        while (index < 256)
        {
            if (data[packetLength + 1 + index] != (byte)(255 - index))
            {
                return -1;
            }

            index = index + 1;
        }

        return packetLength;
    }

    internal static byte[] CopyRange(byte[] data, int offset, int length)
    {
        if (data == null || offset < 0 || length < 0 || offset + length > data.Length)
        {
            return new byte[0];
        }

        byte[] result = new byte[length];
        int index = 0;
        while (index < length)
        {
            result[index] = data[offset + index];
            index = index + 1;
        }

        return result;
    }

    internal static bool IsAscii(byte[] data)
    {
        if (data == null)
        {
            return false;
        }

        foreach (byte value in data)
        {
            if (value > 127)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsPrintableAsciiRange(byte[] data, int offset, int length)
    {
        if (data == null || offset < 0 || length < 0 || offset + length > data.Length)
        {
            return false;
        }

        int index = offset;
        int end = offset + length;
        while (index < end)
        {
            byte value = data[index];
            if (value < 32 || value > 126)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static string DisposalMethodName(int value)
    {
        switch (value)
        {
            case 0:
                return "Not specified";
            case 1:
                return "Do not dispose";
            case 2:
                return "Restore to background";
            case 3:
                return "Restore to previous";
            default:
                return "Reserved";
        }
    }

    internal static MetadataDirectory? FindLogicalScreenDirectory(ImageMetadataDocument document)
    {
        if (document == null)
        {
            return null;
        }

        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory != null && directory.Family == MetadataFamily.Gif && directory.Group == "LogicalScreen")
            {
                return directory;
            }
        }

        return null;
    }

    internal static bool ShouldParseXmp(ImageMetadataReaderOptions options)
    {
        return options == null || options.ParseXmp;
    }

    internal static bool ShouldParseIcc(ImageMetadataReaderOptions options)
    {
        return options == null || options.ParseIcc;
    }

    internal static long GetMaxXmlBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxXmlBytes : options.MaxXmlBytes;
    }

    internal static long GetMaxProfileBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxProfileBytes : options.MaxProfileBytes;
    }
}
