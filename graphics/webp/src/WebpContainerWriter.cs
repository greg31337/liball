using System;
using System.Collections.Generic;

namespace webp;

internal static class WebpContainerWriter
{
    internal static byte[] WriteBasicStill(byte[] encodedBitstream, WebpBitstreamKind bitstreamKind)
    {
        ArgumentNullException.ThrowIfNull(encodedBitstream);
        ValidateStillBitstreamKind(bitstreamKind, "bitstreamKind");

        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk(GetImageFourCc(bitstreamKind), encodedBitstream));
        byte[] data = WebpRiffWriter.Write(WebpConstants.FourCcWebpString, chunks);
        RequireValidBasicStill(data);
        return data;
    }

    internal static byte[] WriteBasicStill(WebpFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ValidateBasicFrame(frame);
        if (frame.EncodedBitstream == null)
        {
            throw new ArgumentException("basic still-image output requires an encoded bitstream", "frame");
        }

        return WriteBasicStill(frame.EncodedBitstream, frame.BitstreamKind);
    }

    internal static byte[] WriteBasicStill(WebpImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        ValidateBasicImage(image);

        WebpFrame frame = image.Frames[0];
        byte[] data = WriteBasicStill(frame);
        ValidateImageDimensions(image, frame, data);
        return data;
    }

    internal static byte[] WriteExtendedStill(byte[] encodedBitstream, WebpBitstreamKind bitstreamKind, WebpMetadata metadata, byte[]? alphaData)
    {
        ArgumentNullException.ThrowIfNull(encodedBitstream);
        ArgumentNullException.ThrowIfNull(metadata);
        ValidateStillBitstreamKind(bitstreamKind, "bitstreamKind");

        WebpBasicContainerParseResult parsed = ParseEncodedStillPayload(encodedBitstream, bitstreamKind);
        return WriteExtendedStill(encodedBitstream, bitstreamKind, parsed.Width, parsed.Height, metadata, alphaData);
    }

    internal static byte[] WriteExtendedStill(byte[] encodedBitstream, WebpBitstreamKind bitstreamKind, int width, int height, WebpMetadata metadata, byte[]? alphaData)
    {
        ArgumentNullException.ThrowIfNull(encodedBitstream);
        ArgumentNullException.ThrowIfNull(metadata);
        ValidateStillBitstreamKind(bitstreamKind, "bitstreamKind");
        ValidateExtendedCanvasDimensions(width, height);
        ValidateMetadata(metadata);
        ValidateExtendedAlpha(bitstreamKind, alphaData);

        WebpBasicContainerParseResult parsed = ParseEncodedStillPayload(encodedBitstream, bitstreamKind);
        if (parsed.Width != width || parsed.Height != height)
        {
            throw new ArgumentException("extended still-image canvas dimensions must match the encoded bitstream", "encodedBitstream");
        }

        bool hasAlpha = alphaData != null || parsed.HasAlpha;
        byte[] data = WriteExtendedStillChunks(encodedBitstream, bitstreamKind, width, height, metadata, alphaData, hasAlpha);
        RequireValidExtendedStill(data);
        return data;
    }

    internal static byte[] WriteExtendedStill(WebpFrame frame, WebpMetadata metadata, int canvasWidth, int canvasHeight)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(metadata);
        ValidateExtendedFrame(frame);
        if (frame.EncodedBitstream == null)
        {
            throw new ArgumentException("extended still-image output requires an encoded bitstream", "frame");
        }

        byte[] data = WriteExtendedStill(frame.EncodedBitstream, frame.BitstreamKind, canvasWidth, canvasHeight, metadata, frame.AlphaData);
        ValidateFrameDimensions(frame, data);
        return data;
    }

    internal static byte[] WriteExtendedStill(WebpImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        ValidateExtendedImage(image);

        WebpFrame frame = image.Frames[0];
        int canvasWidth = GetImageCanvasWidth(image, frame);
        int canvasHeight = GetImageCanvasHeight(image, frame);
        byte[] data = WriteExtendedStill(frame, image.Metadata, canvasWidth, canvasHeight);
        AddUnknownChunkValidation(image);
        if (image.UnknownChunks.Count == 0)
        {
            return data;
        }

        return WriteExtendedStillWithUnknownChunks(data, image);
    }

    internal static byte[] WriteAnimated(List<WebpFrame> frames, int canvasWidth, int canvasHeight, ushort loopCount, uint backgroundColorBgra)
    {
        WebpMetadata metadata = new WebpMetadata();
        return WriteAnimated(frames, canvasWidth, canvasHeight, loopCount, backgroundColorBgra, metadata);
    }

    internal static byte[] WriteAnimated(List<WebpFrame> frames, int canvasWidth, int canvasHeight, ushort loopCount, uint backgroundColorBgra, WebpMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(metadata);
        ValidateAnimatedInput(frames, canvasWidth, canvasHeight, metadata);

        bool hasAlpha = HasAnimationAlpha(frames);
        byte[] data = WriteAnimatedChunks(frames, canvasWidth, canvasHeight, loopCount, backgroundColorBgra, metadata, hasAlpha, null);
        RequireValidAnimation(data);
        return data;
    }

    internal static byte[] WriteAnimated(WebpImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        ValidateAnimatedImage(image);

        bool hasAlpha = image.HasAlpha || HasAnimationAlpha(image.Frames);
        byte[] data = WriteAnimatedChunks(image.Frames, image.CanvasWidth, image.CanvasHeight, image.LoopCount, image.BackgroundColorBgra, image.Metadata, hasAlpha, image.UnknownChunks);
        RequireValidAnimation(data);
        return data;
    }

    internal static void ValidateBasicImage(WebpImage image)
    {
        ArgumentNullException.ThrowIfNull(image.Metadata);
        ArgumentNullException.ThrowIfNull(image.Frames);
        ArgumentNullException.ThrowIfNull(image.UnknownChunks);

        if (image.HasAnimation || image.Frames.Count != 1)
        {
            throw new ArgumentException("basic still-image output requires exactly one non-animated frame", "image");
        }

        if (image.Metadata.IccProfile != null || image.Metadata.Exif != null || image.Metadata.Xmp != null)
        {
            throw new ArgumentException("basic still-image output cannot contain metadata chunks", "image");
        }

        if (image.UnknownChunks.Count != 0)
        {
            throw new ArgumentException("basic still-image output cannot contain unknown chunks", "image");
        }

        if (image.Frames[0] == null)
        {
            throw new ArgumentException("basic still-image output requires a frame", "image");
        }

        if (image.HasAlpha && image.Frames[0].BitstreamKind == WebpBitstreamKind.Lossy)
        {
            throw new ArgumentException("basic lossy still-image output cannot contain alpha", "image");
        }
    }

    internal static void ValidateBasicFrame(WebpFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame.UnknownChunks);
        ValidateStillBitstreamKind(frame.BitstreamKind, "frame");

        if (frame.X != 0 || frame.Y != 0)
        {
            throw new ArgumentException("basic still-image output requires a frame at the canvas origin", "frame");
        }

        if (frame.AlphaData != null)
        {
            throw new ArgumentException("basic still-image output cannot contain an ALPH side chunk", "frame");
        }

        if (frame.UnknownChunks.Count != 0)
        {
            throw new ArgumentException("basic still-image output cannot contain frame unknown chunks", "frame");
        }
    }

    internal static void ValidateImageDimensions(WebpImage image, WebpFrame frame, byte[] data)
    {
        WebpBasicContainerParseResult parsed = WebpBasicContainerParser.Parse(data, WebpStrictness.Strict);
        if (image.CanvasWidth > 0 && image.CanvasWidth != parsed.Width)
        {
            throw new ArgumentException("image canvas width does not match the encoded bitstream", "image");
        }

        if (image.CanvasHeight > 0 && image.CanvasHeight != parsed.Height)
        {
            throw new ArgumentException("image canvas height does not match the encoded bitstream", "image");
        }

        if (frame.Width > 0 && frame.Width != parsed.Width)
        {
            throw new ArgumentException("frame width does not match the encoded bitstream", "image");
        }

        if (frame.Height > 0 && frame.Height != parsed.Height)
        {
            throw new ArgumentException("frame height does not match the encoded bitstream", "image");
        }
    }

    internal static void ValidateExtendedFrame(WebpFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame.UnknownChunks);
        ValidateStillBitstreamKind(frame.BitstreamKind, "frame");

        if (frame.X != 0 || frame.Y != 0)
        {
            throw new ArgumentException("extended still-image output requires a frame at the canvas origin", "frame");
        }

        if (frame.UnknownChunks.Count != 0)
        {
            throw new ArgumentException("extended still-image output cannot contain frame unknown chunks", "frame");
        }
    }

    internal static void ValidateExtendedImage(WebpImage image)
    {
        ArgumentNullException.ThrowIfNull(image.Metadata);
        ArgumentNullException.ThrowIfNull(image.Frames);
        ArgumentNullException.ThrowIfNull(image.UnknownChunks);
        ValidateMetadata(image.Metadata);

        if (image.HasAnimation || image.Frames.Count != 1)
        {
            throw new ArgumentException("extended still-image output requires exactly one non-animated frame", "image");
        }

        if (image.Frames[0] == null)
        {
            throw new ArgumentException("extended still-image output requires a frame", "image");
        }

        if (image.HasAlpha && image.Frames[0].BitstreamKind == WebpBitstreamKind.Lossy && image.Frames[0].AlphaData == null)
        {
            throw new ArgumentException("extended lossy still-image output with alpha requires frame alpha data", "image");
        }
    }

    internal static void ValidateStillBitstreamKind(WebpBitstreamKind bitstreamKind, string parameterName)
    {
        if (bitstreamKind != WebpBitstreamKind.Lossy && bitstreamKind != WebpBitstreamKind.Lossless)
        {
            throw new ArgumentException("still-image output requires a lossy or lossless bitstream kind", parameterName);
        }
    }

    internal static void ValidateMetadata(WebpMetadata metadata)
    {
        if (metadata.IccProfile != null && metadata.IccProfile.Length == 0)
        {
            throw new ArgumentException("ICC profile metadata must not be empty", "metadata");
        }

        if (metadata.Exif != null && metadata.Exif.Length == 0)
        {
            throw new ArgumentException("Exif metadata must not be empty", "metadata");
        }

        if (metadata.Xmp != null && metadata.Xmp.Length == 0)
        {
            throw new ArgumentException("XMP metadata must not be empty", "metadata");
        }
    }

    internal static void ValidateExtendedAlpha(WebpBitstreamKind bitstreamKind, byte[]? alphaData)
    {
        if (alphaData == null)
        {
            return;
        }

        if (alphaData.Length == 0)
        {
            throw new ArgumentException("ALPH payload must not be empty", "alphaData");
        }

        if (bitstreamKind == WebpBitstreamKind.Lossless)
        {
            throw new ArgumentException("ALPH side chunks are not allowed with VP8L lossless images", "alphaData");
        }
    }

    internal static void ValidateExtendedCanvasDimensions(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException("extended still-image canvas dimensions must be non-zero", "width");
        }

        if (width > WebpConstants.Vp8XMaxCanvasDimension || height > WebpConstants.Vp8XMaxCanvasDimension)
        {
            throw new ArgumentException("extended still-image canvas dimensions exceed the maximum", "width");
        }

        long pixels = (long)width * (long)height;
        if (pixels > WebpConstants.Vp8XMaxCanvasPixels)
        {
            throw new ArgumentException("extended still-image canvas dimensions exceed the maximum pixel count", "width");
        }
    }

    internal static int GetImageCanvasWidth(WebpImage image, WebpFrame frame)
    {
        if (image.CanvasWidth > 0)
        {
            return image.CanvasWidth;
        }

        return frame.Width;
    }

    internal static int GetImageCanvasHeight(WebpImage image, WebpFrame frame)
    {
        if (image.CanvasHeight > 0)
        {
            return image.CanvasHeight;
        }

        return frame.Height;
    }

    internal static void ValidateFrameDimensions(WebpFrame frame, byte[] data)
    {
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);
        if (frame.Width > 0 && frame.Width != parsed.Vp8X.CanvasWidth)
        {
            throw new ArgumentException("frame width does not match the encoded bitstream", "frame");
        }

        if (frame.Height > 0 && frame.Height != parsed.Vp8X.CanvasHeight)
        {
            throw new ArgumentException("frame height does not match the encoded bitstream", "frame");
        }
    }

    internal static WebpBasicContainerParseResult ParseEncodedStillPayload(byte[] encodedBitstream, WebpBitstreamKind bitstreamKind)
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk(GetImageFourCc(bitstreamKind), encodedBitstream));
        byte[] data = WebpRiffWriter.Write(WebpConstants.FourCcWebpString, chunks);
        WebpBasicContainerParseResult parsed = WebpBasicContainerParser.Parse(data, WebpStrictness.Strict);
        if (parsed.HasErrors())
        {
            throw new ArgumentException("encoded bitstream is not a valid still-image payload", "encodedBitstream");
        }

        return parsed;
    }

    internal static byte[] WriteExtendedStillChunks(byte[] encodedBitstream, WebpBitstreamKind bitstreamKind, int width, int height, WebpMetadata metadata, byte[]? alphaData, bool hasAlpha)
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcVp8XString, CreateVp8XPayload(width, height, metadata, hasAlpha, false)));
        AddMetadataChunksBeforeImage(chunks, metadata);
        if (alphaData != null)
        {
            chunks.Add(new WebpRiffChunk(WebpConstants.FourCcAlphaString, alphaData));
        }

        chunks.Add(new WebpRiffChunk(GetImageFourCc(bitstreamKind), encodedBitstream));
        AddMetadataChunksAfterImage(chunks, metadata);
        return WebpRiffWriter.Write(WebpConstants.FourCcWebpString, chunks);
    }

    internal static byte[] CreateVp8XPayload(int width, int height, WebpMetadata metadata, bool hasAlpha, bool hasAnimation)
    {
        byte[] payload = new byte[WebpConstants.Vp8XChunkPayloadSize];
        byte flags = 0;
        if (metadata.IccProfile != null)
        {
            flags = (byte)(flags | WebpConstants.Vp8XFeatureIccProfile);
        }

        if (hasAlpha)
        {
            flags = (byte)(flags | WebpConstants.Vp8XFeatureAlpha);
        }

        if (metadata.Exif != null)
        {
            flags = (byte)(flags | WebpConstants.Vp8XFeatureExif);
        }

        if (metadata.Xmp != null)
        {
            flags = (byte)(flags | WebpConstants.Vp8XFeatureXmp);
        }

        if (hasAnimation)
        {
            flags = (byte)(flags | WebpConstants.Vp8XFeatureAnimation);
        }

        payload[0] = flags;
        WebpEndianWriter.WriteUInt24(payload, 4, (uint)(width - 1));
        WebpEndianWriter.WriteUInt24(payload, 7, (uint)(height - 1));
        return payload;
    }

    internal static void AddMetadataChunksBeforeImage(List<WebpRiffChunk> chunks, WebpMetadata metadata)
    {
        if (metadata.IccProfile != null)
        {
            chunks.Add(new WebpRiffChunk(WebpConstants.FourCcIccProfileString, metadata.IccProfile));
        }
    }

    internal static void AddMetadataChunksAfterImage(List<WebpRiffChunk> chunks, WebpMetadata metadata)
    {
        if (metadata.Exif != null)
        {
            chunks.Add(new WebpRiffChunk(WebpConstants.FourCcExifString, metadata.Exif));
        }

        if (metadata.Xmp != null)
        {
            chunks.Add(new WebpRiffChunk(WebpConstants.FourCcXmpString, metadata.Xmp));
        }
    }

    internal static void AddUnknownChunkValidation(WebpImage image)
    {
        int index = 0;
        while (index < image.UnknownChunks.Count)
        {
            WebpUnknownChunk unknown = image.UnknownChunks[index];
            if (unknown == null)
            {
                throw new ArgumentException("unknown chunk list contains a null chunk", "image");
            }

            if (IsKnownChunk(unknown.FourCc))
            {
                throw new ArgumentException("image unknown chunks must not use known WebP FourCC values", "image");
            }

            index = index + 1;
        }
    }

    internal static byte[] WriteExtendedStillWithUnknownChunks(byte[] data, WebpImage image)
    {
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            ReadOnlySpan<byte> payload = WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength);
            chunks.Add(new WebpRiffChunk(chunk.FourCc, payload.ToArray()));
            index = index + 1;
        }

        index = 0;
        while (index < image.UnknownChunks.Count)
        {
            WebpUnknownChunk unknown = image.UnknownChunks[index];
            chunks.Add(new WebpRiffChunk(unknown.FourCc, unknown.Data));
            index = index + 1;
        }

        return WebpRiffWriter.Write(WebpConstants.FourCcWebpString, chunks);
    }

    internal static bool IsKnownChunk(string fourCc)
    {
        return fourCc == WebpConstants.FourCcVp8String
            || fourCc == WebpConstants.FourCcVp8LString
            || fourCc == WebpConstants.FourCcVp8XString
            || fourCc == WebpConstants.FourCcAlphaString
            || fourCc == WebpConstants.FourCcAnimString
            || fourCc == WebpConstants.FourCcAnimationFrameString
            || fourCc == WebpConstants.FourCcIccProfileString
            || fourCc == WebpConstants.FourCcExifString
            || fourCc == WebpConstants.FourCcXmpString;
    }

    internal static void ValidateAnimatedImage(WebpImage image)
    {
        ArgumentNullException.ThrowIfNull(image.Metadata);
        ArgumentNullException.ThrowIfNull(image.Frames);
        ArgumentNullException.ThrowIfNull(image.UnknownChunks);
        ValidateMetadata(image.Metadata);
        AddUnknownChunkValidation(image);
        ValidateAnimatedInput(image.Frames, image.CanvasWidth, image.CanvasHeight, image.Metadata);
    }

    internal static void ValidateAnimatedInput(List<WebpFrame> frames, int canvasWidth, int canvasHeight, WebpMetadata metadata)
    {
        ValidateMetadata(metadata);
        ValidateExtendedCanvasDimensions(canvasWidth, canvasHeight);

        if (frames.Count == 0)
        {
            throw new ArgumentException("animated WebP output requires at least one frame", "frames");
        }

        int index = 0;
        while (index < frames.Count)
        {
            ValidateAnimationFrame(frames[index], canvasWidth, canvasHeight, index);
            index = index + 1;
        }
    }

    internal static void ValidateAnimationFrame(WebpFrame frame, int canvasWidth, int canvasHeight, int frameIndex)
    {
        if (frame == null)
        {
            throw new ArgumentException("animated WebP output frame list contains a null frame", "frames");
        }

        ArgumentNullException.ThrowIfNull(frame.UnknownChunks);
        ValidateStillBitstreamKind(frame.BitstreamKind, "frames");

        if (frame.EncodedBitstream == null)
        {
            throw new ArgumentException("animated WebP output requires each frame to have an encoded bitstream", "frames");
        }

        if (frame.X < 0 || frame.Y < 0)
        {
            throw new ArgumentException("animated WebP frame position is invalid", "frames");
        }

        if ((frame.X & 1) != 0 || (frame.Y & 1) != 0)
        {
            throw new ArgumentException("animated WebP frame positions must be even", "frames");
        }

        if (frame.DurationMilliseconds < 0 || frame.DurationMilliseconds > 16777215)
        {
            throw new ArgumentException("animated WebP frame duration exceeds the 24-bit ANMF field", "frames");
        }

        if (frame.AlphaData != null && frame.AlphaData.Length == 0)
        {
            throw new ArgumentException("animated WebP frame ALPH payload must not be empty", "frames");
        }

        if (frame.AlphaData != null && frame.BitstreamKind == WebpBitstreamKind.Lossless)
        {
            throw new ArgumentException("animated WebP VP8L frames cannot contain ALPH side chunks", "frames");
        }

        WebpBasicContainerParseResult parsed = ParseEncodedStillPayload(frame.EncodedBitstream, frame.BitstreamKind);
        ValidateAnimationFrameDimensions(frame, parsed, canvasWidth, canvasHeight, frameIndex);
        ValidateFrameUnknownChunks(frame);
    }

    internal static void ValidateAnimationFrameDimensions(WebpFrame frame, WebpBasicContainerParseResult parsed, int canvasWidth, int canvasHeight, int frameIndex)
    {
        int width = GetFrameWidth(frame, parsed);
        int height = GetFrameHeight(frame, parsed);

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException("animated WebP frame dimensions must be non-zero", "frames");
        }

        if (frame.Width > 0 && frame.Width != parsed.Width)
        {
            throw new ArgumentException("animated WebP frame width does not match the encoded bitstream", "frames");
        }

        if (frame.Height > 0 && frame.Height != parsed.Height)
        {
            throw new ArgumentException("animated WebP frame height does not match the encoded bitstream", "frames");
        }

        long right = (long)frame.X + (long)width;
        long bottom = (long)frame.Y + (long)height;
        if (right > canvasWidth || bottom > canvasHeight)
        {
            throw new ArgumentException("animated WebP frame rectangle exceeds the canvas", "frames");
        }
    }

    internal static void ValidateFrameUnknownChunks(WebpFrame frame)
    {
        int index = 0;
        while (index < frame.UnknownChunks.Count)
        {
            WebpUnknownChunk unknown = frame.UnknownChunks[index];
            if (unknown == null)
            {
                throw new ArgumentException("frame unknown chunk list contains a null chunk", "frames");
            }

            if (IsKnownChunk(unknown.FourCc))
            {
                throw new ArgumentException("frame unknown chunks must not use known WebP FourCC values", "frames");
            }

            index = index + 1;
        }
    }

    internal static bool HasAnimationAlpha(List<WebpFrame> frames)
    {
        int index = 0;
        while (index < frames.Count)
        {
            WebpFrame frame = frames[index];
            if (frame.AlphaData != null)
            {
                return true;
            }

            if (frame.EncodedBitstream != null && frame.BitstreamKind == WebpBitstreamKind.Lossless)
            {
                WebpBasicContainerParseResult parsed = ParseEncodedStillPayload(frame.EncodedBitstream, frame.BitstreamKind);
                if (parsed.HasAlpha)
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return false;
    }

    internal static byte[] WriteAnimatedChunks(List<WebpFrame> frames, int canvasWidth, int canvasHeight, ushort loopCount, uint backgroundColorBgra, WebpMetadata metadata, bool hasAlpha, List<WebpUnknownChunk>? unknownChunks)
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcVp8XString, CreateVp8XPayload(canvasWidth, canvasHeight, metadata, hasAlpha, true)));
        AddMetadataChunksBeforeImage(chunks, metadata);
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcAnimString, CreateAnimPayload(backgroundColorBgra, loopCount)));

        int index = 0;
        while (index < frames.Count)
        {
            chunks.Add(new WebpRiffChunk(WebpConstants.FourCcAnimationFrameString, CreateAnmfPayload(frames[index])));
            index = index + 1;
        }

        AddMetadataChunksAfterImage(chunks, metadata);
        AddTopLevelUnknownChunks(chunks, unknownChunks);
        return WebpRiffWriter.Write(WebpConstants.FourCcWebpString, chunks);
    }

    internal static byte[] CreateAnimPayload(uint backgroundColorBgra, ushort loopCount)
    {
        byte[] payload = new byte[WebpConstants.AnimChunkPayloadSize];
        WebpEndianWriter.WriteUInt32(payload, 0, backgroundColorBgra);
        WebpEndianWriter.WriteUInt16(payload, 4, loopCount);
        return payload;
    }

    internal static byte[] CreateAnmfPayload(WebpFrame frame)
    {
        if (frame.EncodedBitstream == null)
        {
            throw new ArgumentException("animated WebP output requires each frame to have an encoded bitstream", "frames");
        }

        WebpBasicContainerParseResult parsed = ParseEncodedStillPayload(frame.EncodedBitstream, frame.BitstreamKind);
        List<byte> payload = new List<byte>();
        WebpEndianWriter.AppendUInt24(payload, (uint)(frame.X / 2));
        WebpEndianWriter.AppendUInt24(payload, (uint)(frame.Y / 2));
        WebpEndianWriter.AppendUInt24(payload, (uint)(GetFrameWidth(frame, parsed) - 1));
        WebpEndianWriter.AppendUInt24(payload, (uint)(GetFrameHeight(frame, parsed) - 1));
        WebpEndianWriter.AppendUInt24(payload, (uint)frame.DurationMilliseconds);
        payload.Add(GetAnimationFrameFlags(frame));

        if (frame.AlphaData != null)
        {
            AppendSubchunk(payload, WebpConstants.FourCcAlphaString, frame.AlphaData);
        }

        AppendFrameUnknownChunks(payload, frame);
        AppendSubchunk(payload, GetImageFourCc(frame.BitstreamKind), frame.EncodedBitstream);
        return payload.ToArray();
    }

    internal static int GetFrameWidth(WebpFrame frame, WebpBasicContainerParseResult parsed)
    {
        if (frame.Width > 0)
        {
            return frame.Width;
        }

        return parsed.Width;
    }

    internal static int GetFrameHeight(WebpFrame frame, WebpBasicContainerParseResult parsed)
    {
        if (frame.Height > 0)
        {
            return frame.Height;
        }

        return parsed.Height;
    }

    internal static byte GetAnimationFrameFlags(WebpFrame frame)
    {
        byte flags = 0;
        if (frame.DisposeMode == WebpFrameDisposeMode.DisposeToBackground)
        {
            flags = (byte)(flags | WebpConstants.AnimFrameDisposeBackgroundFlag);
        }

        if (frame.BlendMode == WebpFrameBlendMode.Replace)
        {
            flags = (byte)(flags | WebpConstants.AnimFrameNoBlendFlag);
        }

        return flags;
    }

    internal static void AppendFrameUnknownChunks(List<byte> payload, WebpFrame frame)
    {
        int index = 0;
        while (index < frame.UnknownChunks.Count)
        {
            WebpUnknownChunk unknown = frame.UnknownChunks[index];
            AppendSubchunk(payload, unknown.FourCc, unknown.Data);
            index = index + 1;
        }
    }

    internal static void AppendSubchunk(List<byte> data, string fourCc, byte[] payload)
    {
        data.Add((byte)fourCc[0]);
        data.Add((byte)fourCc[1]);
        data.Add((byte)fourCc[2]);
        data.Add((byte)fourCc[3]);
        WebpEndianWriter.AppendUInt32(data, (uint)payload.Length);
        WebpEndianWriter.AppendChunkPayload(data, payload);
    }

    internal static void AddTopLevelUnknownChunks(List<WebpRiffChunk> chunks, List<WebpUnknownChunk>? unknownChunks)
    {
        if (unknownChunks == null)
        {
            return;
        }

        int index = 0;
        while (index < unknownChunks.Count)
        {
            WebpUnknownChunk unknown = unknownChunks[index];
            chunks.Add(new WebpRiffChunk(unknown.FourCc, unknown.Data));
            index = index + 1;
        }
    }

    internal static string GetImageFourCc(WebpBitstreamKind bitstreamKind)
    {
        if (bitstreamKind == WebpBitstreamKind.Lossy)
        {
            return WebpConstants.FourCcVp8String;
        }

        return WebpConstants.FourCcVp8LString;
    }

    internal static void RequireValidBasicStill(byte[] data)
    {
        WebpBasicContainerParseResult parsed = WebpBasicContainerParser.Parse(data, WebpStrictness.Strict);
        if (parsed.HasErrors())
        {
            throw new ArgumentException("encoded bitstream is not a valid basic still-image payload", "encodedBitstream");
        }
    }

    internal static void RequireValidExtendedStill(byte[] data)
    {
        WebpValidationResult validation = WebpValidator.Validate(data);
        if (validation.HasErrors())
        {
            throw new InvalidOperationException("extended still-image output failed validation");
        }
    }

    internal static void RequireValidAnimation(byte[] data)
    {
        WebpValidationResult validation = WebpValidator.Validate(data);
        if (validation.HasErrors())
        {
            throw new InvalidOperationException("animated WebP output failed validation");
        }
    }
}
