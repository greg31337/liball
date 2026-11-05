using System.IO;

namespace png;

internal class PngPixelFormatConverter
{
    internal static PngPixelBuffer Convert(PngPixelBuffer source, PngPixelConversionOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(source.Data);

        if (IsReorderedFormat(source.Format))
        {
            return Convert(PngEncoder.NormalizeChannelOrder(source), options);
        }

        if (IsReorderedFormat(options.TargetFormat))
        {
            PngPixelConversionOptions nativeOptions = CreateNativeOrderOptions(options);
            PngPixelBuffer nativeResult = Convert(source, nativeOptions);
            return ApplyTargetChannelOrder(nativeResult, options.TargetFormat);
        }

        ValidateBuffer(source);
        PngSizeCalculator.GetPixelFormatBitsPerPixel(options.TargetFormat);
        if (source.Format == options.TargetFormat)
        {
            return CopyBuffer(source);
        }

        if (PngDecoderPixelConverter.IsIndexedFormat(options.TargetFormat))
        {
            if (PngDecoderPixelConverter.IsIndexedFormat(source.Format))
            {
                return PngDecoderPixelConverter.ConvertIndexed(source, options.TargetFormat);
            }

            return ConvertToIndexed(source, options);
        }

        PngPalette palette = options.Palette ?? new PngPalette();
        if (PngDecoderPixelConverter.IsIndexedFormat(source.Format))
        {
            ValidatePalette(palette, options.IndexedTransparency);
        }

        PngChunk[] chunks = CreateTransparencyChunks(source.Format, options);
        return PngDecoderPixelConverter.Convert(source, options.TargetFormat, palette, chunks);
    }

    internal static bool IsReorderedFormat(PngPixelFormat format)
    {
        return format == PngPixelFormat.Bgr8
            || format == PngPixelFormat.Bgr16
            || format == PngPixelFormat.Bgra8
            || format == PngPixelFormat.Bgra16
            || format == PngPixelFormat.Argb8
            || format == PngPixelFormat.Argb16;
    }

    internal static PngPixelConversionOptions CreateNativeOrderOptions(PngPixelConversionOptions options)
    {
        PngPixelConversionOptions nativeOptions = new PngPixelConversionOptions();
        nativeOptions.TargetFormat = GetNativeOrderFormat(options.TargetFormat);
        nativeOptions.Palette = options.Palette;
        nativeOptions.IndexedTransparency = options.IndexedTransparency;
        nativeOptions.GrayscaleTransparency = options.GrayscaleTransparency;
        nativeOptions.TruecolorTransparency = options.TruecolorTransparency;
        return nativeOptions;
    }

    internal static PngPixelFormat GetNativeOrderFormat(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Bgr8:
                return PngPixelFormat.Rgb8;
            case PngPixelFormat.Bgr16:
                return PngPixelFormat.Rgb16;
            case PngPixelFormat.Bgra8:
            case PngPixelFormat.Argb8:
                return PngPixelFormat.Rgba8;
            case PngPixelFormat.Bgra16:
            case PngPixelFormat.Argb16:
                return PngPixelFormat.Rgba16;
            default:
                return format;
        }
    }

    internal static PngPixelBuffer ApplyTargetChannelOrder(PngPixelBuffer buffer, PngPixelFormat targetFormat)
    {
        if (targetFormat == PngPixelFormat.Bgr8
            || targetFormat == PngPixelFormat.Bgr16
            || targetFormat == PngPixelFormat.Bgra8
            || targetFormat == PngPixelFormat.Bgra16)
        {
            return PngChannelTransformer.SwapRgbBgr(buffer);
        }

        if (targetFormat == PngPixelFormat.Argb8 || targetFormat == PngPixelFormat.Argb16)
        {
            return PngChannelTransformer.SwapRgbaArgb(buffer);
        }

        throw new ArgumentOutOfRangeException("TargetFormat");
    }

    internal static PngPixelBuffer ConvertToIndexed(PngPixelBuffer source, PngPixelConversionOptions options)
    {
        PngPalette? palette = options.Palette;
        if (palette == null)
        {
            throw new ArgumentNullException("Palette");
        }

        ValidatePalette(palette, options.IndexedTransparency);
        int bits = PngDecoderPixelConverter.GetIndexedBits(options.TargetFormat);
        int stride = PngDecoderPixelConverter.CalculateStride(source.Width, options.TargetFormat, 1, bits);
        byte[] data = PngDecoderPixelConverter.CreateData(stride, source.Height);
        PngChunk[] chunks = CreateTransparencyChunks(source.Format, options);
        PngChunk? transparency = PngDecoderPixelConverter.FindChunk(chunks, PngConstants.ChunkTransparency);
        int y = 0;
        while (y < source.Height)
        {
            int x = 0;
            while (x < source.Width)
            {
                PngDecoderPixel pixel = PngDecoderPixelConverter.ReadPixel(source, x, y, palette, transparency);
                int paletteIndex = FindPaletteIndex(pixel, palette, options.IndexedTransparency, bits);
                PngDecoderPixelConverter.WritePackedSample(data, y * stride, x, bits, paletteIndex);
                x += 1;
            }

            y += 1;
        }

        return PngDecoderPixelConverter.CreateBuffer(options.TargetFormat, source.Width, source.Height, stride, data);
    }

    internal static int FindPaletteIndex(PngDecoderPixel pixel, PngPalette palette, PngIndexedTransparency? transparency, int bits)
    {
        int red = PngDecoderPixelConverter.ScaleFrom16(pixel.Red, 255);
        int green = PngDecoderPixelConverter.ScaleFrom16(pixel.Green, 255);
        int blue = PngDecoderPixelConverter.ScaleFrom16(pixel.Blue, 255);
        int alpha = PngDecoderPixelConverter.ScaleFrom16(pixel.Alpha, 255);
        int maximumIndex = (1 << bits) - 1;
        int count = palette.Entries.Length;
        if (count > maximumIndex + 1)
        {
            count = maximumIndex + 1;
        }

        int index = 0;
        while (index < count)
        {
            PngPaletteEntry entry = palette.Entries[index];
            if (entry.Red == red && entry.Green == green && entry.Blue == blue && GetPaletteAlpha(transparency, index) == alpha)
            {
                return index;
            }

            index += 1;
        }

        throw new InvalidOperationException("A source pixel does not have an exact entry in the destination palette.");
    }

    internal static int GetPaletteAlpha(PngIndexedTransparency? transparency, int index)
    {
        if (transparency != null && index < transparency.Alpha.Length)
        {
            return transparency.Alpha[index];
        }

        return 255;
    }

    internal static PngChunk[] CreateTransparencyChunks(PngPixelFormat sourceFormat, PngPixelConversionOptions options)
    {
        if (PngDecoderPixelConverter.IsIndexedFormat(sourceFormat) && options.IndexedTransparency != null)
        {
            ArgumentNullException.ThrowIfNull(options.IndexedTransparency.Alpha);
            PngChunk chunk = new PngChunk();
            chunk.Type = PngConstants.ChunkTransparency;
            chunk.Data = options.IndexedTransparency.Alpha;
            return new PngChunk[] { chunk };
        }

        if ((sourceFormat == PngPixelFormat.Grayscale1
            || sourceFormat == PngPixelFormat.Grayscale2
            || sourceFormat == PngPixelFormat.Grayscale4
            || sourceFormat == PngPixelFormat.Grayscale8
            || sourceFormat == PngPixelFormat.Grayscale16)
            && options.GrayscaleTransparency != null)
        {
            byte[] data = new byte[2];
            PngBinaryWriter.WriteUInt16(data, 0, options.GrayscaleTransparency.Gray);
            PngChunk chunk = new PngChunk();
            chunk.Type = PngConstants.ChunkTransparency;
            chunk.Data = data;
            return new PngChunk[] { chunk };
        }

        if ((sourceFormat == PngPixelFormat.Rgb8 || sourceFormat == PngPixelFormat.Rgb16)
            && options.TruecolorTransparency != null)
        {
            byte[] data = new byte[6];
            PngBinaryWriter.WriteUInt16(data, 0, options.TruecolorTransparency.Red);
            PngBinaryWriter.WriteUInt16(data, 2, options.TruecolorTransparency.Green);
            PngBinaryWriter.WriteUInt16(data, 4, options.TruecolorTransparency.Blue);
            PngChunk chunk = new PngChunk();
            chunk.Type = PngConstants.ChunkTransparency;
            chunk.Data = data;
            return new PngChunk[] { chunk };
        }

        return new PngChunk[0];
    }

    internal static void ValidatePalette(PngPalette palette, PngIndexedTransparency? transparency)
    {
        ArgumentNullException.ThrowIfNull(palette.Entries);
        if (palette.Entries.Length == 0 || palette.Entries.Length > 256)
        {
            throw new ArgumentOutOfRangeException("Palette");
        }

        int index = 0;
        while (index < palette.Entries.Length)
        {
            ArgumentNullException.ThrowIfNull(palette.Entries[index]);
            index += 1;
        }

        if (transparency != null)
        {
            ArgumentNullException.ThrowIfNull(transparency.Alpha);
            if (transparency.Alpha.Length > palette.Entries.Length)
            {
                throw new ArgumentOutOfRangeException("IndexedTransparency");
            }
        }
    }

    internal static void ValidateBuffer(PngPixelBuffer buffer)
    {
        if (buffer.Width <= 0)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (buffer.Height <= 0)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        long rowBytes = PngSizeCalculator.CalculateFrameRowBytes(buffer.Width, buffer.Format);
        if (rowBytes > int.MaxValue || buffer.Stride < rowBytes)
        {
            throw new ArgumentOutOfRangeException("Stride");
        }

        long requiredBytes = rowBytes;
        if (buffer.Height > 1)
        {
            requiredBytes = (long)buffer.Stride * (buffer.Height - 1) + rowBytes;
        }

        if (requiredBytes > buffer.Data.Length)
        {
            throw new ArgumentOutOfRangeException("Data");
        }
    }

    internal static PngPixelBuffer CopyBuffer(PngPixelBuffer source)
    {
        int rowBytes = (int)PngSizeCalculator.CalculateFrameRowBytes(source.Width, source.Format);
        byte[] data = PngDecoderPixelConverter.CreateData(rowBytes, source.Height);
        int y = 0;
        while (y < source.Height)
        {
            CopyBytes(source.Data, y * source.Stride, data, y * rowBytes, rowBytes);
            y += 1;
        }

        return PngDecoderPixelConverter.CreateBuffer(source.Format, source.Width, source.Height, rowBytes, data);
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
