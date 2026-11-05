using System.Collections.Generic;
using System.IO;
using System.Text;

namespace png;

internal class PngDecoder
{
    internal static PngImage Decode(byte[] data)
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.PreferredPixelFormat = PngPixelFormat.Unknown;
        return Decode(data, 0, options);
    }

    internal static PngImage Decode(byte[] data, int offset)
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.PreferredPixelFormat = PngPixelFormat.Unknown;
        return Decode(data, offset, options);
    }

    internal static PngImage Decode(byte[] data, int offset, PngDecoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);

        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        ValidateSignature(data, offset, options);
        PngChunkListResult chunkList = PngChunkListReader.Read(data, offset + PngConstants.SignatureLength, options.Limits);
        return DecodeChunkList(chunkList, options);
    }

    internal static PngImage Decode(Stream stream)
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.PreferredPixelFormat = PngPixelFormat.Unknown;
        return Decode(stream, options);
    }

    internal static PngImage Decode(Stream stream, PngDecoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);

        byte[] signature = ReadSignature(stream);
        ValidateSignature(signature, 0, options);
        PngChunkListResult chunkList = PngChunkListReader.Read(stream, options.Limits);
        return DecodeChunkList(chunkList, options);
    }

    internal static PngImage DecodeSpan(ReadOnlySpan<byte> data, PngDecoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);

        ValidateSignatureSpan(data, options);
        PngChunkListResult chunkList = PngChunkListReader.ReadSpan(data, PngConstants.SignatureLength, options.Limits);
        return DecodeChunkList(chunkList, options);
    }

    internal static PngImage DecodeChunkList(PngChunkListResult chunkList, PngDecoderOptions options)
    {
        PngDecoderValidationState validationState = ValidateChunkList(chunkList, options);
        PngMetadata metadata = ReadMetadata(chunkList.Chunks, options);
        PngIhdr ihdr = ReadIhdr(chunkList);
        ValidateIhdr(ihdr, options.Limits);
        byte[] compressedImageData = PngIdatConcatenator.Concatenate(chunkList, options.Limits);
        byte[] filteredScanlines = PngZlibDecompressor.Decompress(compressedImageData, options.Limits);
        ValidateFilteredScanlines(filteredScanlines, ihdr, options.Limits);
        PngPixelBuffer nativePixels = DecodePixels(filteredScanlines, ihdr);
        PngPixelBuffer pixels = PngDecoderPixelConverter.Convert(nativePixels, options.PreferredPixelFormat, metadata.Palette, chunkList.Chunks);
        PngSecurityLimitEnforcer.EnforcePixelBuffer(pixels, options.Limits);
        PngImage image = CreateImage(ihdr, pixels, chunkList, metadata, validationState.Warnings);
        if (options.DecodeAnimation && image.Info.IsAnimated && FindFirstChunk(chunkList.Chunks, PngConstants.ChunkFrameControl) >= 0)
        {
            image.Animation = DecodeAnimation(ihdr, nativePixels, metadata.Palette, chunkList.Chunks, options);
        }

        return image;
    }

    internal static void ValidateSignature(byte[] data, int offset, PngDecoderOptions options)
    {
        PngValidationResult signatureResult = PngSignature.ValidateSignature(data, offset, options.ErrorPolicy);
        if (!signatureResult.IsValid)
        {
            throw new InvalidDataException("PNG signature is invalid.");
        }
    }

    internal static void ValidateSignatureSpan(ReadOnlySpan<byte> data, PngDecoderOptions options)
    {
        PngValidationResult signatureResult = PngSignature.ValidateSignatureSpan(data, options.ErrorPolicy);
        if (!signatureResult.IsValid)
        {
            throw new InvalidDataException("PNG signature is invalid.");
        }
    }

    internal static byte[] ReadSignature(Stream stream)
    {
        byte[] signature = new byte[PngConstants.SignatureLength];
        int totalRead = 0;
        while (totalRead < signature.Length)
        {
            int read = stream.Read(signature, totalRead, signature.Length - totalRead);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return signature;
    }

    internal static PngDecoderValidationState ValidateChunkList(PngChunkListResult chunkList, PngDecoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(chunkList);
        ArgumentNullException.ThrowIfNull(chunkList.Chunks);

        ValidateOptions(options);

        if (!chunkList.FoundIend)
        {
            throw new InvalidDataException("PNG datastream must contain IEND.");
        }

        if (!options.IgnoreTrailingBytes)
        {
            ArgumentNullException.ThrowIfNull(chunkList.TrailingBytes);
            if (chunkList.TrailingBytes.Length > 0)
            {
                throw new InvalidDataException("PNG datastream contains trailing bytes.");
            }
        }

        PngChunk[] validationChunks = PngValidator.CreateValidationChunks(chunkList.Chunks, options);
        List<PngError> errors = new List<PngError>();
        List<PngWarning> warnings = new List<PngWarning>();
        PngValidator.AddPolicyErrors(errors, warnings, PngChunkOrderValidator.Validate(validationChunks).Errors, options);
        PngValidator.AddPolicyErrors(errors, warnings, PngChunkDuplicateValidator.Validate(validationChunks).Errors, options);
        PngValidator.AddPolicyErrors(errors, warnings, PngApngSequenceValidator.Validate(validationChunks).Errors, options);
        PngValidator.AddCrcFindings(errors, warnings, validationChunks, options);
        PngValidator.AddWarnings(warnings, chunkList.Warnings);
        if (errors.Count > 0)
        {
            throw new InvalidDataException(errors[0].Message);
        }

        PngDecoderValidationState state = new PngDecoderValidationState();
        state.Warnings = warnings.ToArray();
        return state;
    }

    internal static void ValidateOptions(PngDecoderOptions options)
    {
        int errorPolicy = (int)options.ErrorPolicy;
        if (errorPolicy < (int)PngErrorPolicy.Strict || errorPolicy > (int)PngErrorPolicy.BestEffortInspection)
        {
            throw new ArgumentOutOfRangeException("ErrorPolicy");
        }

        int pixelFormat = (int)options.PreferredPixelFormat;
        if (pixelFormat < (int)PngPixelFormat.Unknown || pixelFormat > (int)PngPixelFormat.Argb16)
        {
            throw new ArgumentOutOfRangeException("PreferredPixelFormat");
        }
    }

    internal static PngIhdr ReadIhdr(PngChunkListResult chunkList)
    {
        if (chunkList.Chunks.Length == 0)
        {
            throw new InvalidDataException("PNG datastream must contain IHDR.");
        }

        return PngIhdrParser.Parse(chunkList.Chunks[0]);
    }

    internal static void ValidateIhdr(PngIhdr ihdr, PngSecurityLimits limits)
    {
        PngValidationResult validationResult = PngIhdrValidator.Validate(ihdr, limits);
        if (!validationResult.IsValid)
        {
            throw new InvalidDataException("IHDR is invalid.");
        }

        PngSecurityLimitEnforcer.EnforceIhdr(ihdr, limits);
    }

    internal static void ValidateFilteredScanlines(byte[] filteredScanlines, PngIhdr ihdr, PngSecurityLimits limits)
    {
        if (ihdr.InterlaceMethod == PngInterlaceMethod.Adam7)
        {
            return;
        }

        PngValidationResult validationResult = PngRowFilterValidator.Validate(filteredScanlines, ihdr, limits);
        if (!validationResult.IsValid)
        {
            throw new InvalidDataException("PNG scanline data is invalid.");
        }
    }

    internal static PngPixelBuffer DecodePixels(byte[] filteredScanlines, PngIhdr ihdr)
    {
        if (ihdr.InterlaceMethod == PngInterlaceMethod.None)
        {
            return PngNonInterlacedImageDecoder.Decode(filteredScanlines, ihdr);
        }

        if (ihdr.InterlaceMethod == PngInterlaceMethod.Adam7)
        {
            return DecodeAdam7Pixels(filteredScanlines, ihdr);
        }

        throw new ArgumentOutOfRangeException("InterlaceMethod");
    }

    internal static PngPixelBuffer DecodeAdam7Pixels(byte[] filteredScanlines, PngIhdr ihdr)
    {
        byte[] imageData = PngAdam7Decoder.Decode(filteredScanlines, ihdr);
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(ihdr);
        if (layout.RowDataBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = PngNonInterlacedImageDecoder.GetNativePixelFormat(ihdr.ColorType, ihdr.BitDepth);
        buffer.Width = ihdr.Width;
        buffer.Height = ihdr.Height;
        buffer.Stride = (int)layout.RowDataBytes;
        buffer.Data = imageData;
        return buffer;
    }

    internal static PngImage CreateImage(PngIhdr ihdr, PngPixelBuffer pixels, PngChunkListResult chunkList, PngMetadata metadata, PngWarning[] warnings)
    {
        PngImage image = new PngImage();
        image.Info.Width = ihdr.Width;
        image.Info.Height = ihdr.Height;
        image.Info.BitDepth = ihdr.BitDepth;
        image.Info.ColorType = ihdr.ColorType;
        image.Info.CompressionMethod = ihdr.CompressionMethod;
        image.Info.FilterMethod = ihdr.FilterMethod;
        image.Info.InterlaceMethod = ihdr.InterlaceMethod;
        image.Info.PixelFormat = pixels.Format;
        PopulateAnimationInfo(image.Info, chunkList.Chunks);
        image.Pixels = pixels;
        image.Metadata = metadata;
        image.Warnings = warnings;
        return image;
    }

    internal static PngMetadata ReadMetadata(PngChunk[] chunks, PngDecoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);
        if (chunks.Length == 0)
        {
            throw new InvalidDataException("PNG datastream must contain IHDR.");
        }

        PngIhdr ihdr = PngIhdrParser.Parse(chunks[0]);
        PngMetadata metadata = PngMetadataReader.Read(chunks, chunks, ihdr.ColorType, options, false);
        PngSecurityLimitEnforcer.EnforceChunkAggregates(chunks, options.Limits);
        PngSecurityLimitEnforcer.EnforceAnimationChunks(chunks, ihdr, options.Limits);
        return metadata;
    }

    internal static long GetTextBytes(PngChunk chunk, PngSecurityLimits limits, long existingBytes)
    {
        PngSecurityLimits textLimits = CopyLimits(limits);
        if (limits.MaxTextBytes > 0)
        {
            if (existingBytes >= limits.MaxTextBytes)
            {
                textLimits.MaxDecompressedBytes = 1;
            }
            else
            {
                textLimits.MaxDecompressedBytes = limits.MaxTextBytes - existingBytes;
            }
        }

        if (chunk.Type == PngConstants.ChunkText)
        {
            PngText text = PngTextParser.Parse(chunk);
            return text.Text.Length;
        }

        if (chunk.Type == PngConstants.ChunkCompressedText)
        {
            PngCompressedText text = PngCompressedTextParser.Parse(chunk, textLimits);
            return text.Text.Length;
        }

        PngInternationalText internationalText = PngInternationalTextParser.Parse(chunk, textLimits);
        return Encoding.UTF8.GetByteCount(internationalText.Text);
    }

    internal static PngSecurityLimits CopyLimits(PngSecurityLimits source)
    {
        PngSecurityLimits result = new PngSecurityLimits();
        result.MaxWidth = source.MaxWidth;
        result.MaxHeight = source.MaxHeight;
        result.MaxPixelCount = source.MaxPixelCount;
        result.MaxRowBytes = source.MaxRowBytes;
        result.MaxChunkCount = source.MaxChunkCount;
        result.MaxMetadataBytes = source.MaxMetadataBytes;
        result.MaxTextBytes = source.MaxTextBytes;
        result.MaxTextEntries = source.MaxTextEntries;
        result.MaxIdatBytes = source.MaxIdatBytes;
        result.MaxDecompressedBytes = source.MaxDecompressedBytes;
        result.MaxAnimationFrames = source.MaxAnimationFrames;
        result.MaxFrameBytes = source.MaxFrameBytes;
        result.MaxAnimationBytes = source.MaxAnimationBytes;
        return result;
    }

    internal static long AddLimited(long current, long addition, long limit)
    {
        if (addition < 0 || current > long.MaxValue - addition)
        {
            throw new OverflowException();
        }

        long result = current + addition;
        if (limit > 0 && result > limit)
        {
            throw new OverflowException();
        }

        return result;
    }

    internal static void EnforceImageDataLimit(PngChunk[] chunks, PngSecurityLimits limits)
    {
        long totalBytes = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            if (chunk.Type == PngConstants.ChunkIDAT)
            {
                totalBytes = AddLimited(totalBytes, chunk.Data.Length, limits.MaxIdatBytes);
            }
            else if (chunk.Type == PngConstants.ChunkFrameData)
            {
                if (chunk.Data.Length < PngFrameDataParser.SequenceNumberLength)
                {
                    throw new InvalidDataException("fdAT data must contain a sequence number.");
                }

                totalBytes = AddLimited(totalBytes, chunk.Data.Length - PngFrameDataParser.SequenceNumberLength, limits.MaxIdatBytes);
            }

            index += 1;
        }
    }

    internal static bool IsAnimationDataChunk(string chunkType)
    {
        return chunkType == PngConstants.ChunkFrameData;
    }

    internal static PngAnimation DecodeAnimation(PngIhdr ihdr, PngPixelBuffer defaultNativePixels, PngPalette palette, PngChunk[] chunks, PngDecoderOptions options)
    {
        PngAnimationControl control = FindAnimationControl(chunks);
        PngApngAnimationLimitEnforcer.EnforceFrameCount(control.FrameCount, options.Limits);
        List<PngFrame> frames = new List<PngFrame>();
        List<PngFrame> compositionFrames = new List<PngFrame>();
        int firstIdatIndex = FindFirstChunk(chunks, PngConstants.ChunkIDAT);
        bool defaultImageIsFirstFrame = PngApngDefaultImageFrameHandler.UsesDefaultImageFrame(chunks);
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == PngConstants.ChunkFrameControl)
            {
                PngFrameControl frameControl = PngFrameControlParser.Parse(chunks[index]);
                bool usesDefaultImage = defaultImageIsFirstFrame && frames.Count == 0;
                if (index < firstIdatIndex && !usesDefaultImage)
                {
                    throw new InvalidDataException("Only one APNG frame control may appear before IDAT.");
                }

                ValidateFrameRectangle(frameControl, ihdr, usesDefaultImage);
                PngPixelBuffer nativePixels;
                if (usesDefaultImage)
                {
                    if (ReadFrameData(chunks, index + 1).Length > 0)
                    {
                        throw new InvalidDataException("The APNG default image frame must use IDAT image data.");
                    }

                    nativePixels = defaultNativePixels;
                }
                else
                {
                    PngFrameData[] frameData = ReadFrameData(chunks, index + 1);
                    if (frameData.Length == 0)
                    {
                        throw new InvalidDataException("APNG frame does not contain fdAT image data.");
                    }

                    nativePixels = PngApngFrameDecoder.Decode(frameControl, ihdr, frameData, options.Limits);
                }

                PngPixelBuffer pixels = PngDecoderPixelConverter.Convert(nativePixels, options.PreferredPixelFormat, palette, chunks);
                frames.Add(CreateFrame(frameControl, pixels, usesDefaultImage));
                PngPixelFormat compositionFormat = GetCompositionPixelFormat(ihdr);
                PngPixelBuffer compositionPixels = PngDecoderPixelConverter.Convert(nativePixels, compositionFormat, palette, chunks);
                compositionFrames.Add(CreateFrame(frameControl, compositionPixels, usesDefaultImage));
            }

            index += 1;
        }

        if (frames.Count != ConvertToInt(control.FrameCount, "FrameCount"))
        {
            throw new InvalidDataException("APNG frame count does not match acTL.");
        }

        PngAnimation animation = new PngAnimation();
        animation.FrameCount = frames.Count;
        animation.PlayCount = ConvertToInt(control.PlayCount, "PlayCount");
        animation.Frames = frames.ToArray();
        animation.ComposedFrames = ComposeAnimationFrames(ihdr, compositionFrames.ToArray());
        PngApngAnimationLimitEnforcer.Enforce(animation, ihdr, options.Limits);
        return animation;
    }

    internal static PngPixelFormat GetCompositionPixelFormat(PngIhdr ihdr)
    {
        if (ihdr.BitDepth == PngBitDepth.Sixteen)
        {
            return PngPixelFormat.Rgba16;
        }

        return PngPixelFormat.Rgba8;
    }

    internal static PngPixelBuffer[] ComposeAnimationFrames(PngIhdr ihdr, PngFrame[] frames)
    {
        PngPixelFormat format = GetCompositionPixelFormat(ihdr);
        int bytesPerPixel = format == PngPixelFormat.Rgba16 ? 8 : 4;
        if (ihdr.Width > int.MaxValue / bytesPerPixel)
        {
            throw new OverflowException();
        }

        int stride = ihdr.Width * bytesPerPixel;
        if (ihdr.Height > int.MaxValue / stride)
        {
            throw new OverflowException();
        }

        int byteCount = stride * ihdr.Height;
        PngPixelBuffer canvas = new PngPixelBuffer();
        canvas.Format = format;
        canvas.Width = ihdr.Width;
        canvas.Height = ihdr.Height;
        canvas.Stride = stride;
        canvas.Data = new byte[byteCount];

        PngPixelBuffer[] composedFrames = new PngPixelBuffer[frames.Length];
        int index = 0;
        while (index < frames.Length)
        {
            PngFrame frame = frames[index];
            PngPixelBuffer previousCanvas = PngApngFrameDisposer.ApplyNone(canvas);
            PngPixelBuffer composedCanvas = PngApngFrameCompositor.Compose(canvas, frame);
            composedFrames[index] = composedCanvas;
            if (index == 0 && frame.DisposeOperation == PngApngDisposeOperation.Previous)
            {
                canvas = PngApngFrameDisposer.ApplyBackground(composedCanvas, frame);
            }
            else
            {
                canvas = PngApngFrameDisposer.Apply(composedCanvas, frame, previousCanvas);
            }

            index += 1;
        }

        return composedFrames;
    }

    internal static PngFrameData[] ReadFrameData(PngChunk[] chunks, int offset)
    {
        List<PngFrameData> frameData = new List<PngFrameData>();
        int index = offset;
        while (index < chunks.Length && chunks[index].Type != PngConstants.ChunkFrameControl && chunks[index].Type != PngConstants.ChunkIEND)
        {
            if (chunks[index].Type == PngConstants.ChunkFrameData)
            {
                frameData.Add(PngFrameDataParser.Parse(chunks[index]));
            }

            index += 1;
        }

        return frameData.ToArray();
    }

    internal static void ValidateFrameRectangle(PngFrameControl frameControl, PngIhdr ihdr, bool usesDefaultImage)
    {
        PngValidationResult result;
        if (usesDefaultImage)
        {
            result = PngApngFrameRectangleValidator.ValidateDefaultImageFrame(frameControl, ihdr);
        }
        else
        {
            result = PngApngFrameRectangleValidator.Validate(frameControl, ihdr);
        }

        if (!result.IsValid)
        {
            throw new InvalidDataException(result.Errors[0].Message);
        }
    }

    internal static PngFrame CreateFrame(PngFrameControl control, PngPixelBuffer pixels, bool usesDefaultImage)
    {
        PngFrame frame = new PngFrame();
        frame.Width = ConvertToInt(control.Width, "Width");
        frame.Height = ConvertToInt(control.Height, "Height");
        frame.XOffset = ConvertToInt(control.XOffset, "XOffset");
        frame.YOffset = ConvertToInt(control.YOffset, "YOffset");
        frame.DelayNumerator = control.DelayNumerator;
        frame.DelayDenominator = PngApngFrameDelayNormalizer.NormalizeDenominator(control.DelayDenominator);
        frame.DisposeOperation = control.DisposeOperation;
        frame.BlendOperation = control.BlendOperation;
        frame.Pixels = pixels;
        frame.UsesDefaultImage = usesDefaultImage;
        return frame;
    }

    internal static PngAnimationControl FindAnimationControl(PngChunk[] chunks)
    {
        int index = FindFirstChunk(chunks, PngConstants.ChunkAnimationControl);
        if (index < 0)
        {
            throw new InvalidDataException("APNG datastream must contain acTL.");
        }

        return PngAnimationControlParser.Parse(chunks[index]);
    }

    internal static int FindFirstChunk(PngChunk[] chunks, string chunkType)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == chunkType)
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    internal static PngPalette ReadPalette(PngChunk[] chunks)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ArgumentNullException.ThrowIfNull(chunk);
            ArgumentNullException.ThrowIfNull(chunk.Type);
            if (chunk.Type == PngConstants.ChunkPLTE)
            {
                return PngPlteParser.Parse(chunk);
            }

            index += 1;
        }

        return new PngPalette();
    }

    internal static void PopulateAnimationInfo(PngImageInfo info, PngChunk[] chunks)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ArgumentNullException.ThrowIfNull(chunk);
            ArgumentNullException.ThrowIfNull(chunk.Type);
            if (chunk.Type == PngConstants.ChunkAnimationControl)
            {
                PngAnimationControl control = PngAnimationControlParser.Parse(chunk);
                info.IsAnimated = true;
                info.FrameCount = ConvertToInt(control.FrameCount, "FrameCount");
                info.PlayCount = ConvertToInt(control.PlayCount, "PlayCount");
                return;
            }

            index += 1;
        }
    }

    internal static int ConvertToInt(uint value, string name)
    {
        if (value > int.MaxValue)
        {
            throw new OverflowException();
        }

        return (int)value;
    }
}

internal class PngDecoderValidationState
{
    internal PngWarning[] Warnings = new PngWarning[0];
}
