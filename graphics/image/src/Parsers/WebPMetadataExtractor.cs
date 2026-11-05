using System.Collections.Generic;
using System.Text;

namespace image;

internal class WebPMetadataExtractor
{
    internal static void Extract(ImageMetadataDocument document, List<RiffChunk> chunks, ImageMetadataReaderOptions options)
    {
        if (document == null || chunks == null)
        {
            return;
        }

        WebPParserState state = new WebPParserState();
        foreach (RiffChunk chunk in chunks)
        {
            if (chunk == null || chunk.DataLimited)
            {
                continue;
            }

            ExtractChunk(document, chunk, options, state);
        }

        FinishFrames(document, state, options);
        ValidateFeatures(document, chunks, state);
    }

    internal static void ExtractChunk(ImageMetadataDocument document, RiffChunk chunk, ImageMetadataReaderOptions options, WebPParserState state)
    {
        switch (chunk.FourCc)
        {
            case "VP8X":
                ExtractVp8x(document, chunk, state);
                return;
            case "VP8 ":
                ExtractVp8(document, chunk, state, options);
                return;
            case "VP8L":
                ExtractVp8l(document, chunk, state, options);
                return;
            case "ALPH":
                ExtractAlpha(document, chunk, state, null);
                return;
            case "ICCP":
                ExtractIcc(document, chunk, options);
                return;
            case "EXIF":
                ExtractExif(document, chunk, options);
                return;
            case "XMP ":
                ExtractXmp(document, chunk, options);
                return;
            case "ANIM":
                ExtractAnimationControl(document, chunk, state);
                return;
            case "ANMF":
                ExtractAnimationFrame(document, chunk, state, options);
                return;
            default:
                return;
        }
    }

    internal static void ExtractVp8x(ImageMetadataDocument document, RiffChunk chunk, WebPParserState state)
    {
        if (!HasLength(document, chunk, 10, "webp.vp8x.length.invalid"))
        {
            return;
        }

        byte[] data = chunk.Data;
        int flags = data[0];
        bool hasIcc = (flags & 32) != 0;
        bool hasAlpha = (flags & 16) != 0;
        bool hasExif = (flags & 8) != 0;
        bool hasXmp = (flags & 4) != 0;
        bool animated = (flags & 2) != 0;
        int width = ReadUnsigned24(data, 4) + 1;
        int height = ReadUnsigned24(data, 7) + 1;
        MetadataDirectory directory = FindDirectory(document, chunk);
        AddTag(directory, "WebP.VP8X.HasIcc", "ICC Profile Present", MetadataValue.FromBoolean(hasIcc), chunk, 0, 1, "");
        AddTag(directory, "WebP.VP8X.HasAlpha", "Alpha Present", MetadataValue.FromBoolean(hasAlpha), chunk, 0, 1, "");
        AddTag(directory, "WebP.VP8X.HasExif", "EXIF Present", MetadataValue.FromBoolean(hasExif), chunk, 0, 1, "");
        AddTag(directory, "WebP.VP8X.HasXmp", "XMP Present", MetadataValue.FromBoolean(hasXmp), chunk, 0, 1, "");
        AddTag(directory, "WebP.VP8X.IsAnimated", "Animated", MetadataValue.FromBoolean(animated), chunk, 0, 1, "");
        AddTag(directory, "WebP.VP8X.CanvasWidth", "Canvas Width", MetadataValue.FromInteger(width), chunk, 4, 3, "");
        AddTag(directory, "WebP.VP8X.CanvasHeight", "Canvas Height", MetadataValue.FromInteger(height), chunk, 7, 3, "");

        if ((flags & 193) != 0 || data[1] != 0 || data[2] != 0 || data[3] != 0)
        {
            AddInvalidField(document, chunk, "webp.vp8x.reserved.invalid", "WebP VP8X reserved bits were not zero.", 0, 4);
        }

        if ((long)width * height > 4294967295)
        {
            AddInvalidField(document, chunk, "webp.vp8x.canvas.invalid", "WebP VP8X canvas pixel count exceeded the defined maximum.", 4, 6);
        }

        state.HasVp8x = true;
        state.CanvasWidth = width;
        state.CanvasHeight = height;
        state.HasIccFlag = hasIcc;
        state.HasAlphaFlag = hasAlpha;
        state.HasExifFlag = hasExif;
        state.HasXmpFlag = hasXmp;
        state.AnimationFlag = animated;
    }

    internal static void ExtractVp8(ImageMetadataDocument document, RiffChunk chunk, WebPParserState state, ImageMetadataReaderOptions options)
    {
        if (chunk.Data == null || chunk.Data.Length < 10)
        {
            AddInvalidLength(document, chunk, "webp.vp8.header.truncated", "WebP VP8 frame header was truncated.", 10);
            return;
        }

        byte[] data = chunk.Data;
        if ((data[0] & 1) != 0 || data[3] != 157 || data[4] != 1 || data[5] != 42)
        {
            AddInvalidField(document, chunk, "webp.vp8.header.invalid", "WebP VP8 payload did not begin with a valid key-frame header.", 0, 10);
            return;
        }

        int widthField = ReadUnsigned16(data, 6);
        int heightField = ReadUnsigned16(data, 8);
        int width = widthField & 16383;
        int height = heightField & 16383;
        int horizontalScale = widthField >> 14;
        int verticalScale = heightField >> 14;
        MetadataDirectory directory = FindDirectory(document, chunk);
        AddTag(directory, "WebP.VP8.Width", "Width", MetadataValue.FromInteger(width), chunk, 6, 2, "");
        AddTag(directory, "WebP.VP8.Height", "Height", MetadataValue.FromInteger(height), chunk, 8, 2, "");
        AddTag(directory, "WebP.VP8.HorizontalScale", "Horizontal Scale", MetadataValue.FromInteger(horizontalScale), chunk, 6, 2, "");
        AddTag(directory, "WebP.VP8.VerticalScale", "Vertical Scale", MetadataValue.FromInteger(verticalScale), chunk, 8, 2, "");
        if (width == 0 || height == 0)
        {
            AddInvalidField(document, chunk, "webp.vp8.dimensions.invalid", "WebP VP8 dimensions must be greater than zero.", 6, 4);
        }

        if (!state.AnimationFlag)
        {
            AddStillFrame(document, state, options, width, height, false, ImageColorSpaceKind.YCbCr);
        }
    }

    internal static void ExtractVp8l(ImageMetadataDocument document, RiffChunk chunk, WebPParserState state, ImageMetadataReaderOptions options)
    {
        if (chunk.Data == null || chunk.Data.Length < 5)
        {
            AddInvalidLength(document, chunk, "webp.vp8l.header.truncated", "WebP VP8L frame header was truncated.", 5);
            return;
        }

        byte[] data = chunk.Data;
        if (data[0] != 47)
        {
            AddInvalidField(document, chunk, "webp.vp8l.signature.invalid", "WebP VP8L signature byte was invalid.", 0, 1);
            return;
        }

        int width = 1 + data[1] + ((data[2] & 63) << 8);
        int height = 1 + ((data[2] >> 6) & 3) + (data[3] << 2) + ((data[4] & 15) << 10);
        bool hasAlpha = (data[4] & 16) != 0;
        int version = data[4] >> 5;
        MetadataDirectory directory = FindDirectory(document, chunk);
        AddTag(directory, "WebP.VP8L.Signature", "Signature", MetadataValue.FromInteger(data[0]), chunk, 0, 1, "0x2F");
        AddTag(directory, "WebP.VP8L.Width", "Width", MetadataValue.FromInteger(width), chunk, 1, 4, "");
        AddTag(directory, "WebP.VP8L.Height", "Height", MetadataValue.FromInteger(height), chunk, 1, 4, "");
        AddTag(directory, "WebP.VP8L.HasAlpha", "Alpha Used", MetadataValue.FromBoolean(hasAlpha), chunk, 4, 1, "");
        AddTag(directory, "WebP.VP8L.Version", "Version", MetadataValue.FromInteger(version), chunk, 4, 1, "");
        if (version != 0)
        {
            AddInvalidField(document, chunk, "webp.vp8l.version.invalid", "WebP VP8L version field was not zero.", 4, 1);
        }

        if (!state.AnimationFlag)
        {
            AddStillFrame(document, state, options, width, height, hasAlpha, ImageColorSpaceKind.Rgb);
        }
    }

    internal static void ExtractAlpha(ImageMetadataDocument document, RiffChunk chunk, WebPParserState state, ImageMetadataFrame? frame)
    {
        ExtractAlphaData(document, FindDirectory(document, chunk), chunk.Data, chunk.DataOffset, chunk.FourCc, frame);
        if (state != null)
        {
            state.TopLevelAlpha = true;
            if (state.StillFrame != null)
            {
                state.StillFrame.HasAlpha = true;
                state.StillFrame.ChannelCount = 4;
            }
        }
    }

    internal static void ExtractAlphaData(ImageMetadataDocument document, MetadataDirectory directory, byte[] data, long offset, string blockType, ImageMetadataFrame? frame)
    {
        if (data == null || data.Length < 1)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "webp.alph.header.truncated", "WebP ALPH header was truncated.", blockType, offset, 1);
            return;
        }

        int flags = data[0];
        int compression = flags & 3;
        int filtering = (flags >> 2) & 3;
        int preprocessing = (flags >> 4) & 3;
        AddDirectTag(directory, "WebP.ALPH.CompressionMethod", "Compression Method", MetadataValue.FromInteger(compression), offset, 1, AlphaCompressionName(compression));
        AddDirectTag(directory, "WebP.ALPH.FilteringMethod", "Filtering Method", MetadataValue.FromInteger(filtering), offset, 1, AlphaFilterName(filtering));
        AddDirectTag(directory, "WebP.ALPH.Preprocessing", "Preprocessing", MetadataValue.FromInteger(preprocessing), offset, 1, AlphaPreprocessingName(preprocessing));
        AddDirectTag(directory, "WebP.ALPH.DataByteLength", "Alpha Data Byte Length", MetadataValue.FromInteger(data.Length - 1), offset + 1, data.Length - 1, "");
        if ((flags & 192) != 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "webp.alph.reserved.invalid", "WebP ALPH reserved bits were not zero.", blockType, offset, 1);
        }

        if (compression > 1)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "webp.alph.compression.invalid", "WebP ALPH compression method was reserved.", blockType, offset, 1);
        }

        if (preprocessing > 1)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "webp.alph.preprocessing.invalid", "WebP ALPH preprocessing method was reserved.", blockType, offset, 1);
        }

        if (frame != null)
        {
            frame.HasAlpha = true;
            frame.ChannelCount = 4;
        }
    }

    internal static void ExtractIcc(ImageMetadataDocument document, RiffChunk chunk, ImageMetadataReaderOptions options)
    {
        MetadataDirectory directory = FindDirectory(document, chunk);
        AddTag(directory, "WebP.ICCP.ProfileByteLength", "ICC Profile Byte Length", MetadataValue.FromInteger(chunk.Data.Length), chunk, 0, chunk.Data.Length, "");
        if (!ShouldParseIcc(options))
        {
            return;
        }

        MetadataProfile profile = CreateProfile(MetadataProfileKind.Icc, MetadataFamily.Icc, "Icc.WebP.ICCP", "ICC Profile", "application/vnd.iccprofile", chunk);
        PreserveProfile(document, profile, chunk.Data, GetMaxProfileBytes(options), "webp.icc.profile.limit", "ICCP");
        IccProfileParser.ParseProfile(document, profile, options, "WebP/ICCP/ICC", "ICCP");
    }

    internal static void ExtractExif(ImageMetadataDocument document, RiffChunk chunk, ImageMetadataReaderOptions options)
    {
        int tiffOffset = HasExifPrefix(chunk.Data) ? 6 : 0;
        int tiffLength = chunk.Data.Length - tiffOffset;
        byte[] tiff = CopyRange(chunk.Data, tiffOffset, tiffLength);
        MetadataDirectory directory = FindDirectory(document, chunk);
        AddTag(directory, "WebP.EXIF.ByteLength", "EXIF Byte Length", MetadataValue.FromInteger(chunk.Data.Length), chunk, 0, chunk.Data.Length, "");
        AddTag(directory, "WebP.EXIF.HasIdentifier", "Exif Identifier Present", MetadataValue.FromBoolean(tiffOffset == 6), chunk, 0, tiffOffset, "");
        AddTag(directory, "WebP.EXIF.ByteOrder", "TIFF Byte Order", MetadataValue.FromString(JpegExifExtractor.TiffByteOrderName(tiff)), chunk, tiffOffset, PrefixLength(tiff, 2), JpegExifExtractor.TiffByteOrderName(tiff));
        if (!JpegExifExtractor.HasTiffHeader(tiff))
        {
            AddInvalidField(document, chunk, "webp.exif.tiff.header.invalid", "WebP EXIF payload did not contain a recognized TIFF header.", tiffOffset, tiffLength);
        }

        MetadataProfile profile = CreateProfile(MetadataProfileKind.Exif, MetadataFamily.Exif, "Exif.WebP.EXIF", "EXIF", "image/tiff", chunk);
        profile.Offset = chunk.DataOffset + tiffOffset;
        profile.Length = tiffLength;
        PreserveProfile(document, profile, tiff, GetMaxProfileBytes(options), "webp.exif.profile.limit", "EXIF");
    }

    internal static void ExtractXmp(ImageMetadataDocument document, RiffChunk chunk, ImageMetadataReaderOptions options)
    {
        MetadataDirectory directory = FindDirectory(document, chunk);
        AddTag(directory, "WebP.XMP.PacketByteLength", "XMP Packet Byte Length", MetadataValue.FromInteger(chunk.Data.Length), chunk, 0, chunk.Data.Length, "");
        if (!ShouldParseXmp(options))
        {
            return;
        }

        MetadataProfile profile = CreateProfile(MetadataProfileKind.Xmp, MetadataFamily.Xmp, "Xmp.WebP.XMP", "XMP", "application/rdf+xml", chunk);
        if (chunk.Data.Length <= GetMaxXmlBytes(options))
        {
            profile.Data = CopyRange(chunk.Data, 0, chunk.Data.Length);
            profile.Text = Encoding.UTF8.GetString(chunk.Data);
            if (!MetadataStringReader.IsValidUtf8(chunk.Data))
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "webp.xmp.utf8.invalid", "WebP XMP packet was not valid UTF-8.", "XMP ", chunk.DataOffset, chunk.Data.Length);
            }
        }
        else
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "webp.xmp.profile.limit", "WebP XMP packet exceeded the configured XML limit.", "XMP ", chunk.DataOffset, chunk.Data.Length);
        }

        document.AddProfile(profile);
        XmpPacketParser.ParseProfile(document, profile, options, "WebP/XMP");
    }

    internal static void ExtractAnimationControl(ImageMetadataDocument document, RiffChunk chunk, WebPParserState state)
    {
        if (!HasLength(document, chunk, 6, "webp.anim.length.invalid"))
        {
            return;
        }

        byte[] data = chunk.Data;
        int blue = data[0];
        int green = data[1];
        int red = data[2];
        int alpha = data[3];
        uint background = ReadUnsigned32(data, 0);
        int loopCount = ReadUnsigned16(data, 4);
        MetadataDirectory directory = FindDirectory(document, chunk);
        AddTag(directory, "WebP.ANIM.BackgroundColor", "Background Color", MetadataValue.FromUnsignedInteger(background), chunk, 0, 4, "BGRA(" + blue.ToString() + ", " + green.ToString() + ", " + red.ToString() + ", " + alpha.ToString() + ")");
        AddTag(directory, "WebP.ANIM.BackgroundBlue", "Background Blue", MetadataValue.FromInteger(blue), chunk, 0, 1, "");
        AddTag(directory, "WebP.ANIM.BackgroundGreen", "Background Green", MetadataValue.FromInteger(green), chunk, 1, 1, "");
        AddTag(directory, "WebP.ANIM.BackgroundRed", "Background Red", MetadataValue.FromInteger(red), chunk, 2, 1, "");
        AddTag(directory, "WebP.ANIM.BackgroundAlpha", "Background Alpha", MetadataValue.FromInteger(alpha), chunk, 3, 1, "");
        AddTag(directory, "WebP.ANIM.LoopCount", "Loop Count", MetadataValue.FromInteger(loopCount), chunk, 4, 2, loopCount == 0 ? "Infinite" : loopCount.ToString());
        state.HasAnim = true;
        state.LoopCount = loopCount;
    }

    internal static void ExtractAnimationFrame(ImageMetadataDocument document, RiffChunk chunk, WebPParserState state, ImageMetadataReaderOptions options)
    {
        if (chunk.Data == null || chunk.Data.Length < 16)
        {
            AddInvalidLength(document, chunk, "webp.anmf.header.truncated", "WebP ANMF frame header was truncated.", 16);
            return;
        }

        if (document.Frames.Count >= GetMaxFrameCount(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "webp.frame.limit", "WebP animation frame count exceeded the configured frame limit.", "ANMF", chunk.Offset, chunk.Length + 8);
            return;
        }

        byte[] data = chunk.Data;
        int x = ReadUnsigned24(data, 0) * 2;
        int y = ReadUnsigned24(data, 3) * 2;
        int width = ReadUnsigned24(data, 6) + 1;
        int height = ReadUnsigned24(data, 9) + 1;
        int duration = ReadUnsigned24(data, 12);
        int flags = data[15];
        bool noBlend = (flags & 2) != 0;
        bool disposeBackground = (flags & 1) != 0;
        ImageMetadataFrame frame = new ImageMetadataFrame();
        frame.Width = width;
        frame.Height = height;
        frame.CanvasWidth = state.CanvasWidth;
        frame.CanvasHeight = state.CanvasHeight;
        frame.CanvasX = x;
        frame.CanvasY = y;
        frame.BitDepth = 8;
        frame.ChannelCount = 3;
        frame.IsAnimated = true;
        frame.DurationMilliseconds = duration;
        frame.Compression = ImageCompressionKind.WebP;
        frame.ColorSpace = ImageColorSpaceKind.Rgb;
        document.AddFrame(frame);

        MetadataDirectory directory = FindDirectory(document, chunk);
        directory.FrameIndex = frame.Index;
        AddTag(directory, "WebP.ANMF.X", "Frame X", MetadataValue.FromInteger(x), chunk, 0, 3, "");
        AddTag(directory, "WebP.ANMF.Y", "Frame Y", MetadataValue.FromInteger(y), chunk, 3, 3, "");
        AddTag(directory, "WebP.ANMF.Width", "Frame Width", MetadataValue.FromInteger(width), chunk, 6, 3, "");
        AddTag(directory, "WebP.ANMF.Height", "Frame Height", MetadataValue.FromInteger(height), chunk, 9, 3, "");
        AddTag(directory, "WebP.ANMF.DurationMilliseconds", "Frame Duration", MetadataValue.FromInteger(duration), chunk, 12, 3, duration.ToString() + " ms");
        AddTag(directory, "WebP.ANMF.NoBlend", "Do Not Blend", MetadataValue.FromBoolean(noBlend), chunk, 15, 1, "");
        AddTag(directory, "WebP.ANMF.DisposeToBackground", "Dispose To Background", MetadataValue.FromBoolean(disposeBackground), chunk, 15, 1, "");
        if ((flags & 252) != 0)
        {
            AddInvalidField(document, chunk, "webp.anmf.reserved.invalid", "WebP ANMF reserved bits were not zero.", 15, 1);
        }

        if ((long)x + width > state.CanvasWidth || (long)y + height > state.CanvasHeight)
        {
            AddInvalidField(document, chunk, "webp.anmf.bounds.invalid", "WebP animation frame exceeded the VP8X canvas.", 0, 12);
        }

        ParseFrameData(document, chunk, frame, directory);
        state.HasAnmf = true;
    }

    internal static void ParseFrameData(ImageMetadataDocument document, RiffChunk chunk, ImageMetadataFrame frame, MetadataDirectory directory)
    {
        byte[] data = chunk.Data;
        int offset = 16;
        int count = 0;
        bool hasBitstream = false;
        bool hasAlpha = false;
        while (offset < data.Length)
        {
            if (data.Length - offset < 8)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "webp.anmf.chunk.header.truncated", "WebP ANMF nested chunk header was truncated.", "ANMF", chunk.DataOffset + offset, 8);
                return;
            }

            string type = MetadataStringReader.DecodeAsciiRange(data, offset, 4, null, "ANMF");
            uint length = ReadUnsigned32(data, offset + 4);
            long storedLength = length + ((length & 1) != 0 ? 1 : 0);
            if (storedLength > data.Length - offset - 8)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "webp.anmf.chunk.truncated", "WebP ANMF nested chunk payload or padding was truncated.", type, chunk.DataOffset + offset, 8 + storedLength);
                return;
            }

            if (length > 2147483647)
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "webp.anmf.chunk.large", "WebP ANMF nested chunk was too large to inspect.", type, chunk.DataOffset + offset, length);
                return;
            }

            byte[] payload = CopyRange(data, offset + 8, (int)length);
            if (type == "ALPH")
            {
                if (hasAlpha)
                {
                    MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "webp.anmf.alpha.duplicate", "WebP ANMF contained more than one ALPH chunk.", "ANMF", chunk.DataOffset + offset, 8 + storedLength);
                }

                hasAlpha = true;
                ExtractAlphaData(document, directory, payload, chunk.DataOffset + offset + 8, "ANMF/ALPH", frame);
            }
            else if (type == "VP8 " || type == "VP8L")
            {
                if (hasBitstream)
                {
                    MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "webp.anmf.bitstream.duplicate", "WebP ANMF contained more than one image bitstream chunk.", "ANMF", chunk.DataOffset + offset, 8 + storedLength);
                }

                hasBitstream = true;
                ValidateFrameBitstream(document, directory, frame, type, payload, chunk.DataOffset + offset + 8);
            }

            if ((length & 1) != 0 && data[offset + 8 + (int)length] != 0)
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "webp.anmf.padding.invalid", "WebP ANMF nested chunk padding byte was not zero.", type, chunk.DataOffset + offset + 8 + length, 1);
            }

            offset = offset + 8 + (int)storedLength;
            count = count + 1;
        }

        AddTag(directory, "WebP.ANMF.SubChunkCount", "Frame Subchunk Count", MetadataValue.FromInteger(count), chunk, 16, chunk.Data.Length - 16, "");
        AddTag(directory, "WebP.ANMF.HasAlphaChunk", "Alpha Chunk Present", MetadataValue.FromBoolean(hasAlpha), chunk, 16, chunk.Data.Length - 16, "");
        AddTag(directory, "WebP.ANMF.HasBitstream", "Bitstream Present", MetadataValue.FromBoolean(hasBitstream), chunk, 16, chunk.Data.Length - 16, "");
        if (!hasBitstream)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "webp.anmf.bitstream.missing", "WebP ANMF did not contain a VP8 or VP8L bitstream chunk.", "ANMF", chunk.DataOffset + 16, chunk.Data.Length - 16);
        }
    }

    internal static void ValidateFrameBitstream(ImageMetadataDocument document, MetadataDirectory directory, ImageMetadataFrame frame, string type, byte[]? payload, long offset)
    {
        int width = 0;
        int height = 0;
        bool valid = false;
        if (type == "VP8L")
        {
            frame.ColorSpace = ImageColorSpaceKind.Rgb;
            if (payload != null && payload.Length >= 5 && payload[0] == 47 && (payload[4] >> 5) == 0)
            {
                width = 1 + payload[1] + ((payload[2] & 63) << 8);
                height = 1 + ((payload[2] >> 6) & 3) + (payload[3] << 2) + ((payload[4] & 15) << 10);
                valid = true;
                if ((payload[4] & 16) != 0)
                {
                    frame.HasAlpha = true;
                    frame.ChannelCount = 4;
                }
            }
        }
        else
        {
            frame.ColorSpace = ImageColorSpaceKind.YCbCr;
            if (payload != null && payload.Length >= 10 && (payload[0] & 1) == 0 && payload[3] == 157 && payload[4] == 1 && payload[5] == 42)
            {
                width = ReadUnsigned16(payload, 6) & 16383;
                height = ReadUnsigned16(payload, 8) & 16383;
                valid = true;
            }
        }

        AddDirectTag(directory, "WebP.ANMF.BitstreamType", "Bitstream Type", MetadataValue.FromString(type), offset - 8, 4, type);
        if (!valid)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "webp.anmf.bitstream.invalid", "WebP ANMF nested image bitstream header was invalid.", type, offset, payload == null ? 0 : payload.Length);
            return;
        }

        if (payload == null)
        {
            return;
        }

        AddDirectTag(directory, "WebP.ANMF.BitstreamWidth", "Bitstream Width", MetadataValue.FromInteger(width), offset, payload.Length, "");
        AddDirectTag(directory, "WebP.ANMF.BitstreamHeight", "Bitstream Height", MetadataValue.FromInteger(height), offset, payload.Length, "");
        if (width != frame.Width || height != frame.Height)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "webp.anmf.bitstream.dimensions.mismatch", "WebP ANMF frame dimensions did not match its nested bitstream.", type, offset, payload.Length);
        }
    }

    internal static void AddStillFrame(ImageMetadataDocument document, WebPParserState state, ImageMetadataReaderOptions options, int width, int height, bool hasAlpha, ImageColorSpaceKind colorSpace)
    {
        if (state.StillFrame != null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "webp.bitstream.duplicate", "WebP contained more than one top-level image bitstream.");
            return;
        }

        if (document.Frames.Count >= GetMaxFrameCount(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "webp.frame.limit", "WebP frame count exceeded the configured frame limit.", "WebP", 0, document.Frames.Count);
            return;
        }

        ImageMetadataFrame frame = new ImageMetadataFrame();
        frame.Width = width;
        frame.Height = height;
        frame.CanvasWidth = state.HasVp8x ? state.CanvasWidth : width;
        frame.CanvasHeight = state.HasVp8x ? state.CanvasHeight : height;
        frame.BitDepth = 8;
        frame.ChannelCount = hasAlpha || state.TopLevelAlpha ? 4 : 3;
        frame.HasAlpha = hasAlpha || state.TopLevelAlpha;
        frame.Compression = ImageCompressionKind.WebP;
        frame.ColorSpace = colorSpace;
        document.AddFrame(frame);
        state.StillFrame = frame;
    }

    internal static void FinishFrames(ImageMetadataDocument document, WebPParserState state, ImageMetadataReaderOptions options)
    {
        if (document.Frames.Count == 0 && state.HasVp8x && !state.AnimationFlag)
        {
            AddStillFrame(document, state, options, state.CanvasWidth, state.CanvasHeight, false, ImageColorSpaceKind.Rgb);
        }

        foreach (ImageMetadataFrame frame in document.Frames)
        {
            if (frame != null && state.AnimationFlag)
            {
                frame.IsAnimated = true;
            }
        }
    }

    internal static void ValidateFeatures(ImageMetadataDocument document, List<RiffChunk> chunks, WebPParserState state)
    {
        if (!state.HasVp8x)
        {
            return;
        }

        ValidateFeature(document, state.HasIccFlag, HasChunk(chunks, "ICCP"), "ICCP");
        ValidateFeature(document, state.HasAlphaFlag, HasAlpha(document, state), "alpha data");
        ValidateFeature(document, state.HasExifFlag, HasChunk(chunks, "EXIF"), "EXIF");
        ValidateFeature(document, state.HasXmpFlag, HasChunk(chunks, "XMP "), "XMP ");
        ValidateFeature(document, state.AnimationFlag, state.HasAnim || state.HasAnmf, "ANIM/ANMF");

        if (state.AnimationFlag && (!state.HasAnim || !state.HasAnmf))
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "webp.animation.structure.invalid", "Animated WebP required both ANIM and ANMF chunks.");
        }
    }

    internal static bool HasAlpha(ImageMetadataDocument document, WebPParserState state)
    {
        if (state != null && state.TopLevelAlpha)
        {
            return true;
        }

        if (document == null)
        {
            return false;
        }

        foreach (ImageMetadataFrame frame in document.Frames)
        {
            if (frame != null && frame.HasAlpha)
            {
                return true;
            }
        }

        return false;
    }

    internal static void ValidateFeature(ImageMetadataDocument document, bool flag, bool present, string feature)
    {
        if (flag != present)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "webp.vp8x.feature.mismatch", "WebP VP8X feature flag did not match the presence of " + feature + ".");
        }
    }

    internal static bool HasChunk(List<RiffChunk> chunks, string fourCc)
    {
        foreach (RiffChunk chunk in chunks)
        {
            if (chunk != null && chunk.FourCc == fourCc)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasLength(ImageMetadataDocument document, RiffChunk chunk, int expected, string code)
    {
        if (chunk.Data != null && chunk.Data.Length == expected)
        {
            return true;
        }

        AddInvalidLength(document, chunk, code, "WebP chunk length did not match its defined size.", expected);
        return false;
    }

    internal static void AddInvalidLength(ImageMetadataDocument document, RiffChunk chunk, string code, string message, int expected)
    {
        MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, code, message, chunk.FourCc, chunk.Offset, expected + 8);
    }

    internal static void AddInvalidField(ImageMetadataDocument document, RiffChunk chunk, string code, string message, int relativeOffset, long length)
    {
        MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, code, message, chunk.FourCc, chunk.DataOffset + relativeOffset, length);
    }

    internal static MetadataDirectory FindDirectory(ImageMetadataDocument document, RiffChunk chunk)
    {
        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory != null && directory.Offset == chunk.Offset && directory.Family == MetadataFamily.WebP)
            {
                return directory;
            }
        }

        return RiffChunkTraverser.AddChunkDirectory(document, chunk);
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string key, string name, MetadataValue value, RiffChunk chunk, int relativeOffset, long length, string displayValue)
    {
        return AddDirectTag(directory, key, name, value, chunk.DataOffset + relativeOffset, length, displayValue);
    }

    internal static MetadataTag AddDirectTag(MetadataDirectory directory, string key, string name, MetadataValue value, long offset, long length, string displayValue)
    {
        if (directory == null)
        {
            return new MetadataTag();
        }

        MetadataTag tag = directory.AddTagWithValue(key, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = displayValue;
        return tag;
    }

    internal static MetadataProfile CreateProfile(MetadataProfileKind kind, MetadataFamily family, string key, string name, string mimeType, RiffChunk chunk)
    {
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = kind;
        profile.Family = family;
        profile.Key = key;
        profile.Name = name;
        profile.MimeType = mimeType;
        profile.Offset = chunk.DataOffset;
        profile.Length = chunk.Data.Length;
        return profile;
    }

    internal static void PreserveProfile(ImageMetadataDocument document, MetadataProfile profile, byte[] data, long limit, string code, string blockType)
    {
        if (profile.Length <= limit)
        {
            profile.Data = CopyRange(data, 0, data.Length);
        }
        else
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, code, "WebP embedded profile exceeded the configured profile limit.", blockType, profile.Offset, profile.Length);
        }

        document.AddProfile(profile);
    }

    internal static bool HasExifPrefix(byte[] data)
    {
        return data != null && data.Length >= 6
            && data[0] == 69 && data[1] == 120 && data[2] == 105 && data[3] == 102
            && data[4] == 0 && data[5] == 0;
    }

    internal static int PrefixLength(byte[] data, int maximum)
    {
        if (data == null)
        {
            return 0;
        }

        return data.Length < maximum ? data.Length : maximum;
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

    internal static int ReadUnsigned16(byte[] data, int offset)
    {
        if (data == null || offset < 0 || offset + 2 > data.Length)
        {
            return 0;
        }

        return data[offset] | (data[offset + 1] << 8);
    }

    internal static int ReadUnsigned24(byte[] data, int offset)
    {
        if (data == null || offset < 0 || offset + 3 > data.Length)
        {
            return 0;
        }

        return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16);
    }

    internal static uint ReadUnsigned32(byte[] data, int offset)
    {
        if (data == null || offset < 0 || offset + 4 > data.Length)
        {
            return 0;
        }

        return (uint)data[offset]
            | ((uint)data[offset + 1] << 8)
            | ((uint)data[offset + 2] << 16)
            | ((uint)data[offset + 3] << 24);
    }

    internal static string AlphaCompressionName(int value)
    {
        if (value == 0)
        {
            return "None";
        }

        if (value == 1)
        {
            return "WebP lossless";
        }

        return "Reserved";
    }

    internal static string AlphaFilterName(int value)
    {
        switch (value)
        {
            case 0:
                return "None";
            case 1:
                return "Horizontal";
            case 2:
                return "Vertical";
            default:
                return "Gradient";
        }
    }

    internal static string AlphaPreprocessingName(int value)
    {
        if (value == 0)
        {
            return "None";
        }

        if (value == 1)
        {
            return "Level reduction";
        }

        return "Reserved";
    }

    internal static bool ShouldParseXmp(ImageMetadataReaderOptions options)
    {
        return options == null || options.ParseXmp;
    }

    internal static bool ShouldParseIcc(ImageMetadataReaderOptions options)
    {
        return options == null || options.ParseIcc;
    }

    internal static long GetMaxProfileBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxProfileBytes : options.MaxProfileBytes;
    }

    internal static long GetMaxXmlBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxXmlBytes : options.MaxXmlBytes;
    }

    internal static int GetMaxFrameCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxFrameCount : options.MaxFrameCount;
    }
}

internal class WebPParserState
{
    internal bool HasVp8x;
    internal int CanvasWidth;
    internal int CanvasHeight;
    internal bool HasIccFlag;
    internal bool HasAlphaFlag;
    internal bool HasExifFlag;
    internal bool HasXmpFlag;
    internal bool AnimationFlag;
    internal bool TopLevelAlpha;
    internal bool HasAnim;
    internal bool HasAnmf;
    internal int LoopCount;
    internal ImageMetadataFrame? StillFrame;

    internal WebPParserState()
    {
        HasVp8x = false;
        CanvasWidth = 0;
        CanvasHeight = 0;
        HasIccFlag = false;
        HasAlphaFlag = false;
        HasExifFlag = false;
        HasXmpFlag = false;
        AnimationFlag = false;
        TopLevelAlpha = false;
        HasAnim = false;
        HasAnmf = false;
        LoopCount = 0;
        StillFrame = null;
    }
}
