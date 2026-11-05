using System.IO;

namespace png;

internal class PngProgressiveDecoderCore
{
    internal static PngImage DecodeBytes(byte[] data, PngDecoderOptions options, IPngProgressiveSink sink)
    {
        return DecodeBytesAtOffset(data, 0, options, sink);
    }

    internal static PngImage DecodeBytesAtOffset(byte[] data, int offset, PngDecoderOptions options, IPngProgressiveSink sink)
    {
        ArgumentNullException.ThrowIfNull(data);
        ValidateArguments(options, sink);
        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        PngDecoder.ValidateSignature(data, offset, options);
        PngChunkListResult chunkList = PngChunkListReader.Read(data, offset + PngConstants.SignatureLength, options.Limits);
        return DecodeChunkList(chunkList, options, sink);
    }

    internal static PngImage DecodeSpan(ReadOnlySpan<byte> data, PngDecoderOptions options, IPngProgressiveSink sink)
    {
        ValidateArguments(options, sink);
        PngDecoder.ValidateSignatureSpan(data, options);
        PngChunkListResult chunkList = PngChunkListReader.ReadSpan(data, PngConstants.SignatureLength, options.Limits);
        return DecodeChunkList(chunkList, options, sink);
    }

    internal static PngImage DecodeStream(Stream stream, PngDecoderOptions options, IPngProgressiveSink sink)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ValidateArguments(options, sink);

        byte[] signature = PngDecoder.ReadSignature(stream);
        PngDecoder.ValidateSignature(signature, 0, options);
        PngChunkListResult chunkList = PngChunkListReader.Read(stream, options.Limits);
        return DecodeChunkList(chunkList, options, sink);
    }

    internal static void ValidateArguments(PngDecoderOptions options, IPngProgressiveSink sink)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);
        ArgumentNullException.ThrowIfNull(sink);
    }

    internal static PngImage DecodeChunkList(PngChunkListResult chunkList, PngDecoderOptions options, IPngProgressiveSink sink)
    {
        PngDecoderValidationState validationState = PngDecoder.ValidateChunkList(chunkList, options);
        PngMetadata metadata = PngDecoder.ReadMetadata(chunkList.Chunks, options);
        PngIhdr ihdr = PngDecoder.ReadIhdr(chunkList);
        PngDecoder.ValidateIhdr(ihdr, options.Limits);
        byte[] compressedImageData = PngIdatConcatenator.Concatenate(chunkList, options.Limits);
        byte[] filteredScanlines = PngZlibDecompressor.Decompress(compressedImageData, options.Limits);
        PngDecoder.ValidateFilteredScanlines(filteredScanlines, ihdr, options.Limits);

        EmitWarnings(validationState.Warnings, sink);
        bool wantsPreviews = sink.WantsPreviews();
        PngPixelBuffer nativePixels = DecodePixels(filteredScanlines, ihdr, metadata.Palette, chunkList.Chunks, options, sink, wantsPreviews);
        PngPixelBuffer pixels = PngDecoderPixelConverter.Convert(nativePixels, options.PreferredPixelFormat, metadata.Palette, chunkList.Chunks);
        PngSecurityLimitEnforcer.EnforcePixelBuffer(pixels, options.Limits);
        PngImage image = PngDecoder.CreateImage(ihdr, pixels, chunkList, metadata, validationState.Warnings);
        if (options.DecodeAnimation && image.Info.IsAnimated && PngDecoder.FindFirstChunk(chunkList.Chunks, PngConstants.ChunkFrameControl) >= 0)
        {
            image.Animation = PngDecoder.DecodeAnimation(ihdr, nativePixels, metadata.Palette, chunkList.Chunks, options);
            EmitFrames(image.Animation.Frames, sink);
        }

        return image;
    }

    internal static PngPixelBuffer DecodePixels(byte[] filteredScanlines, PngIhdr ihdr, PngPalette palette, PngChunk[] chunks, PngDecoderOptions options, IPngProgressiveSink sink, bool wantsPreviews)
    {
        if (ihdr.InterlaceMethod == PngInterlaceMethod.None)
        {
            return DecodeNonInterlaced(filteredScanlines, ihdr, palette, chunks, options, sink, wantsPreviews);
        }

        if (ihdr.InterlaceMethod == PngInterlaceMethod.Adam7)
        {
            return DecodeAdam7(filteredScanlines, ihdr, palette, chunks, options, sink, wantsPreviews);
        }

        throw new ArgumentOutOfRangeException("InterlaceMethod");
    }

    internal static PngPixelBuffer DecodeNonInterlaced(byte[] filteredScanlines, PngIhdr ihdr, PngPalette palette, PngChunk[] chunks, PngDecoderOptions options, IPngProgressiveSink sink, bool wantsPreviews)
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(ihdr);
        if (layout.FilteredImageBytes > int.MaxValue || layout.ImageDataBytes > int.MaxValue || layout.RowDataBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        if ((int)layout.FilteredImageBytes != filteredScanlines.Length)
        {
            throw new ArgumentOutOfRangeException("filteredScanlines");
        }

        byte[] imageData = new byte[(int)layout.ImageDataBytes];
        byte[] previousRow = new byte[0];
        bool hasPreviousRow = false;
        int sourceOffset = 0;
        int destinationOffset = 0;
        int rowIndex = 0;
        while (rowIndex < layout.Height)
        {
            byte[] rowData = PngNonInterlacedImageDecoder.DecodeRow(filteredScanlines, sourceOffset, (int)layout.RowDataBytes, layout.FilterBytesPerPixel, previousRow, hasPreviousRow);
            CopyBytes(rowData, 0, imageData, destinationOffset, rowData.Length);
            PngPixelBuffer nativeRow = CreateBuffer(ihdr, rowData, (int)layout.RowDataBytes, 1);
            PngProgressiveRow row = new PngProgressiveRow();
            row.RowIndex = rowIndex;
            row.Pixels = PngDecoderPixelConverter.Convert(nativeRow, options.PreferredPixelFormat, palette, chunks);
            PngSecurityLimitEnforcer.EnforcePixelBuffer(row.Pixels, options.Limits);
            sink.ReceiveRow(row);

            if (wantsPreviews)
            {
                PngPixelBuffer previewPixels = CreatePreviewPixels(ihdr, imageData, (int)layout.RowDataBytes, palette, chunks, options);
                EmitPreview(0, rowIndex + 1, previewPixels, sink);
            }

            previousRow = rowData;
            hasPreviousRow = true;
            sourceOffset += (int)layout.FilteredRowBytes;
            destinationOffset += (int)layout.RowDataBytes;
            rowIndex += 1;
        }

        return CreateBuffer(ihdr, imageData, (int)layout.RowDataBytes, ihdr.Height);
    }

    internal static PngPixelBuffer DecodeAdam7(byte[] filteredScanlines, PngIhdr ihdr, PngPalette palette, PngChunk[] chunks, PngDecoderOptions options, IPngProgressiveSink sink, bool wantsPreviews)
    {
        long expectedBytes = PngAdam7PassGeometryCalculator.CalculateFilteredImageBytes(ihdr);
        if (expectedBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        if ((int)expectedBytes != filteredScanlines.Length)
        {
            throw new ArgumentOutOfRangeException("filteredScanlines");
        }

        PngScanlineLayout finalLayout = PngScanlineLayoutCalculator.Calculate(ihdr);
        if (finalLayout.ImageDataBytes > int.MaxValue || finalLayout.RowDataBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        byte[] imageData = new byte[(int)finalLayout.ImageDataBytes];
        PngAdam7PassGeometry[] passes = PngAdam7PassGeometryCalculator.CalculatePasses(ihdr);
        int sourceOffset = 0;
        int previewIndex = 0;
        int passIndex = 0;
        while (passIndex < passes.Length)
        {
            PngAdam7PassGeometry pass = passes[passIndex];
            sourceOffset = PngAdam7Decoder.DecodePass(filteredScanlines, sourceOffset, pass, finalLayout, imageData);
            EmitPass(pass, sink);
            if (wantsPreviews && !pass.IsEmpty)
            {
                previewIndex += 1;
                PngPixelBuffer previewPixels = CreatePreviewPixels(ihdr, imageData, (int)finalLayout.RowDataBytes, palette, chunks, options);
                EmitPreview(pass.PassIndex, previewIndex, previewPixels, sink);
            }

            passIndex += 1;
        }

        return CreateBuffer(ihdr, imageData, (int)finalLayout.RowDataBytes, ihdr.Height);
    }

    internal static PngPixelBuffer CreatePreviewPixels(PngIhdr ihdr, byte[] imageData, int stride, PngPalette palette, PngChunk[] chunks, PngDecoderOptions options)
    {
        byte[] snapshot = new byte[imageData.Length];
        CopyBytes(imageData, 0, snapshot, 0, imageData.Length);
        PngPixelBuffer nativePreview = CreateBuffer(ihdr, snapshot, stride, ihdr.Height);
        PngPixelBuffer preview = PngDecoderPixelConverter.Convert(nativePreview, options.PreferredPixelFormat, palette, chunks);
        PngSecurityLimitEnforcer.EnforcePixelBuffer(preview, options.Limits);
        return preview;
    }

    internal static PngPixelBuffer CreateBuffer(PngIhdr ihdr, byte[] data, int stride, int height)
    {
        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = PngNonInterlacedImageDecoder.GetNativePixelFormat(ihdr.ColorType, ihdr.BitDepth);
        buffer.Width = ihdr.Width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Data = data;
        return buffer;
    }

    internal static void EmitPass(PngAdam7PassGeometry pass, IPngProgressiveSink sink)
    {
        PngAdam7PassUpdate update = new PngAdam7PassUpdate();
        update.PassIndex = pass.PassIndex;
        update.Width = pass.Width;
        update.Height = pass.Height;
        update.RowsDecoded = pass.Height;
        update.IsEmpty = pass.IsEmpty;
        sink.ReceivePass(update);
    }

    internal static void EmitPreview(int passIndex, int updateIndex, PngPixelBuffer pixels, IPngProgressiveSink sink)
    {
        PngProgressivePreview preview = new PngProgressivePreview();
        preview.PassIndex = passIndex;
        preview.UpdateIndex = updateIndex;
        preview.Pixels = pixels;
        sink.ReceivePreview(preview);
    }

    internal static void EmitWarnings(PngWarning[] warnings, IPngProgressiveSink sink)
    {
        int index = 0;
        while (index < warnings.Length)
        {
            sink.ReceiveWarning(warnings[index]);
            index += 1;
        }
    }

    internal static void EmitFrames(PngFrame[] frames, IPngProgressiveSink sink)
    {
        int index = 0;
        while (index < frames.Length)
        {
            sink.ReceiveFrame(frames[index]);
            index += 1;
        }
    }

    internal static void CopyBytes(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }
}
