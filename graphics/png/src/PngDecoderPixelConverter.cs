using System.IO;

namespace png;

internal class PngDecoderPixelConverter
{
    internal static PngPixelBuffer Convert(PngPixelBuffer source, PngPixelFormat targetFormat, PngPalette palette, PngChunk[] chunks)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(source.Data);
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(chunks);

        if (targetFormat == PngPixelFormat.Unknown || targetFormat == source.Format)
        {
            return source;
        }

        if (PngPixelFormatConverter.IsReorderedFormat(targetFormat))
        {
            PngPixelFormat nativeFormat = PngPixelFormatConverter.GetNativeOrderFormat(targetFormat);
            PngPixelBuffer nativeBuffer = Convert(source, nativeFormat, palette, chunks);
            return PngPixelFormatConverter.ApplyTargetChannelOrder(nativeBuffer, targetFormat);
        }

        if (IsIndexedFormat(targetFormat))
        {
            return ConvertIndexed(source, targetFormat);
        }

        int targetBits = GetTargetBits(targetFormat);
        int targetChannels = GetTargetChannels(targetFormat);
        int stride = CalculateStride(source.Width, targetFormat, targetChannels, targetBits);
        byte[] data = CreateData(stride, source.Height);
        PngChunk? transparency = FindChunk(chunks, PngConstants.ChunkTransparency);

        int y = 0;
        while (y < source.Height)
        {
            int x = 0;
            while (x < source.Width)
            {
                PngDecoderPixel pixel = ReadPixel(source, x, y, palette, transparency);
                WritePixel(data, stride, x, y, targetFormat, pixel);
                x += 1;
            }

            y += 1;
        }

        return CreateBuffer(targetFormat, source.Width, source.Height, stride, data);
    }

    internal static PngPixelBuffer ConvertIndexed(PngPixelBuffer source, PngPixelFormat targetFormat)
    {
        if (!IsIndexedFormat(source.Format))
        {
            throw new ArgumentOutOfRangeException("PreferredPixelFormat");
        }

        int bitDepth = GetIndexedBits(targetFormat);
        int maximumIndex = (1 << bitDepth) - 1;
        int stride = CalculateStride(source.Width, targetFormat, 1, bitDepth);
        byte[] data = CreateData(stride, source.Height);
        int y = 0;
        while (y < source.Height)
        {
            int x = 0;
            while (x < source.Width)
            {
                int value = ReadIndexedSample(source, x, y);
                if (value > maximumIndex)
                {
                    throw new ArgumentOutOfRangeException("PreferredPixelFormat");
                }

                WritePackedSample(data, y * stride, x, bitDepth, value);
                x += 1;
            }

            y += 1;
        }

        return CreateBuffer(targetFormat, source.Width, source.Height, stride, data);
    }

    internal static PngDecoderPixel ReadPixel(PngPixelBuffer source, int x, int y, PngPalette palette, PngChunk? transparency)
    {
        PngDecoderPixel pixel = new PngDecoderPixel();
        pixel.Alpha = 65535;
        int offset = y * source.Stride;

        switch (source.Format)
        {
            case PngPixelFormat.Grayscale1:
            case PngPixelFormat.Grayscale2:
            case PngPixelFormat.Grayscale4:
            case PngPixelFormat.Grayscale8:
                ReadGrayscale8Pixel(source, x, offset, transparency, pixel);
                return pixel;
            case PngPixelFormat.Grayscale16:
                ReadGrayscale16Pixel(source, x, offset, transparency, pixel);
                return pixel;
            case PngPixelFormat.Indexed1:
            case PngPixelFormat.Indexed2:
            case PngPixelFormat.Indexed4:
            case PngPixelFormat.Indexed8:
                ReadIndexedPixel(source, x, y, palette, transparency, pixel);
                return pixel;
            case PngPixelFormat.Rgb8:
                ReadRgb8Pixel(source, x, offset, transparency, pixel);
                return pixel;
            case PngPixelFormat.Rgb16:
                ReadRgb16Pixel(source, x, offset, transparency, pixel);
                return pixel;
            case PngPixelFormat.GrayscaleAlpha8:
                ReadGrayscaleAlpha8Pixel(source, x, offset, pixel);
                return pixel;
            case PngPixelFormat.GrayscaleAlpha16:
                ReadGrayscaleAlpha16Pixel(source, x, offset, pixel);
                return pixel;
            case PngPixelFormat.Rgba8:
                ReadRgba8Pixel(source, x, offset, pixel);
                return pixel;
            case PngPixelFormat.Rgba16:
                ReadRgba16Pixel(source, x, offset, pixel);
                return pixel;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static void ReadGrayscale8Pixel(PngPixelBuffer source, int x, int offset, PngChunk? transparency, PngDecoderPixel pixel)
    {
        int bits = GetGrayscaleBits(source.Format);
        int sample = bits == 8 ? source.Data[offset + x] : ReadPackedSample(source.Data, offset, x, bits);
        int maximum = (1 << bits) - 1;
        int value = ScaleTo16(sample, maximum);
        pixel.Red = value;
        pixel.Green = value;
        pixel.Blue = value;
        if (transparency != null && transparency.Data.Length == 2 && sample == PngBinaryReader.ReadUInt16(transparency.Data, 0))
        {
            pixel.Alpha = 0;
        }
    }

    internal static void ReadGrayscale16Pixel(PngPixelBuffer source, int x, int offset, PngChunk? transparency, PngDecoderPixel pixel)
    {
        int sample = PngBinaryReader.ReadUInt16(source.Data, offset + x * 2);
        pixel.Red = sample;
        pixel.Green = sample;
        pixel.Blue = sample;
        if (transparency != null && transparency.Data.Length == 2 && sample == PngBinaryReader.ReadUInt16(transparency.Data, 0))
        {
            pixel.Alpha = 0;
        }
    }

    internal static void ReadIndexedPixel(PngPixelBuffer source, int x, int y, PngPalette palette, PngChunk? transparency, PngDecoderPixel pixel)
    {
        int paletteIndex = ReadIndexedSample(source, x, y);
        if (paletteIndex >= palette.Entries.Length)
        {
            throw new InvalidDataException("PNG palette index is outside PLTE.");
        }

        PngPaletteEntry entry = palette.Entries[paletteIndex];
        pixel.Red = entry.Red * 257;
        pixel.Green = entry.Green * 257;
        pixel.Blue = entry.Blue * 257;
        if (transparency != null && paletteIndex < transparency.Data.Length)
        {
            pixel.Alpha = transparency.Data[paletteIndex] * 257;
        }
    }

    internal static void ReadRgb8Pixel(PngPixelBuffer source, int x, int offset, PngChunk? transparency, PngDecoderPixel pixel)
    {
        int pixelOffset = offset + x * 3;
        int red = source.Data[pixelOffset];
        int green = source.Data[pixelOffset + 1];
        int blue = source.Data[pixelOffset + 2];
        pixel.Red = red * 257;
        pixel.Green = green * 257;
        pixel.Blue = blue * 257;
        if (transparency != null && transparency.Data.Length == 6
            && red == PngBinaryReader.ReadUInt16(transparency.Data, 0)
            && green == PngBinaryReader.ReadUInt16(transparency.Data, 2)
            && blue == PngBinaryReader.ReadUInt16(transparency.Data, 4))
        {
            pixel.Alpha = 0;
        }
    }

    internal static void ReadRgb16Pixel(PngPixelBuffer source, int x, int offset, PngChunk? transparency, PngDecoderPixel pixel)
    {
        int pixelOffset = offset + x * 6;
        pixel.Red = PngBinaryReader.ReadUInt16(source.Data, pixelOffset);
        pixel.Green = PngBinaryReader.ReadUInt16(source.Data, pixelOffset + 2);
        pixel.Blue = PngBinaryReader.ReadUInt16(source.Data, pixelOffset + 4);
        if (transparency != null && transparency.Data.Length == 6
            && pixel.Red == PngBinaryReader.ReadUInt16(transparency.Data, 0)
            && pixel.Green == PngBinaryReader.ReadUInt16(transparency.Data, 2)
            && pixel.Blue == PngBinaryReader.ReadUInt16(transparency.Data, 4))
        {
            pixel.Alpha = 0;
        }
    }

    internal static void ReadGrayscaleAlpha8Pixel(PngPixelBuffer source, int x, int offset, PngDecoderPixel pixel)
    {
        int pixelOffset = offset + x * 2;
        int value = source.Data[pixelOffset] * 257;
        pixel.Red = value;
        pixel.Green = value;
        pixel.Blue = value;
        pixel.Alpha = source.Data[pixelOffset + 1] * 257;
    }

    internal static void ReadGrayscaleAlpha16Pixel(PngPixelBuffer source, int x, int offset, PngDecoderPixel pixel)
    {
        int pixelOffset = offset + x * 4;
        int value = PngBinaryReader.ReadUInt16(source.Data, pixelOffset);
        pixel.Red = value;
        pixel.Green = value;
        pixel.Blue = value;
        pixel.Alpha = PngBinaryReader.ReadUInt16(source.Data, pixelOffset + 2);
    }

    internal static void ReadRgba8Pixel(PngPixelBuffer source, int x, int offset, PngDecoderPixel pixel)
    {
        int pixelOffset = offset + x * 4;
        pixel.Red = source.Data[pixelOffset] * 257;
        pixel.Green = source.Data[pixelOffset + 1] * 257;
        pixel.Blue = source.Data[pixelOffset + 2] * 257;
        pixel.Alpha = source.Data[pixelOffset + 3] * 257;
    }

    internal static void ReadRgba16Pixel(PngPixelBuffer source, int x, int offset, PngDecoderPixel pixel)
    {
        int pixelOffset = offset + x * 8;
        pixel.Red = PngBinaryReader.ReadUInt16(source.Data, pixelOffset);
        pixel.Green = PngBinaryReader.ReadUInt16(source.Data, pixelOffset + 2);
        pixel.Blue = PngBinaryReader.ReadUInt16(source.Data, pixelOffset + 4);
        pixel.Alpha = PngBinaryReader.ReadUInt16(source.Data, pixelOffset + 6);
    }

    internal static void WritePixel(byte[] data, int stride, int x, int y, PngPixelFormat format, PngDecoderPixel pixel)
    {
        int offset = y * stride;
        int gray = ((pixel.Red * 299) + (pixel.Green * 587) + (pixel.Blue * 114) + 500) / 1000;
        switch (format)
        {
            case PngPixelFormat.Grayscale1:
            case PngPixelFormat.Grayscale2:
            case PngPixelFormat.Grayscale4:
                int bits = GetGrayscaleBits(format);
                WritePackedSample(data, offset, x, bits, ScaleFrom16(gray, (1 << bits) - 1));
                return;
            case PngPixelFormat.Grayscale8:
                data[offset + x] = (byte)ScaleFrom16(gray, 255);
                return;
            case PngPixelFormat.Grayscale16:
                WriteUInt16(data, offset + x * 2, gray);
                return;
            case PngPixelFormat.Rgb8:
                WriteRgb8(data, offset + x * 3, pixel);
                return;
            case PngPixelFormat.Rgb16:
                WriteRgb16(data, offset + x * 6, pixel);
                return;
            case PngPixelFormat.GrayscaleAlpha8:
                data[offset + x * 2] = (byte)ScaleFrom16(gray, 255);
                data[offset + x * 2 + 1] = (byte)ScaleFrom16(pixel.Alpha, 255);
                return;
            case PngPixelFormat.GrayscaleAlpha16:
                WriteUInt16(data, offset + x * 4, gray);
                WriteUInt16(data, offset + x * 4 + 2, pixel.Alpha);
                return;
            case PngPixelFormat.Rgba8:
                WriteRgb8(data, offset + x * 4, pixel);
                data[offset + x * 4 + 3] = (byte)ScaleFrom16(pixel.Alpha, 255);
                return;
            case PngPixelFormat.Rgba16:
                WriteRgb16(data, offset + x * 8, pixel);
                WriteUInt16(data, offset + x * 8 + 6, pixel.Alpha);
                return;
            default:
                throw new ArgumentOutOfRangeException("PreferredPixelFormat");
        }
    }

    internal static void WriteRgb8(byte[] data, int offset, PngDecoderPixel pixel)
    {
        data[offset] = (byte)ScaleFrom16(pixel.Red, 255);
        data[offset + 1] = (byte)ScaleFrom16(pixel.Green, 255);
        data[offset + 2] = (byte)ScaleFrom16(pixel.Blue, 255);
    }

    internal static void WriteRgb16(byte[] data, int offset, PngDecoderPixel pixel)
    {
        WriteUInt16(data, offset, pixel.Red);
        WriteUInt16(data, offset + 2, pixel.Green);
        WriteUInt16(data, offset + 4, pixel.Blue);
    }

    internal static int ReadIndexedSample(PngPixelBuffer source, int x, int y)
    {
        int bits = GetIndexedBits(source.Format);
        int offset = y * source.Stride;
        if (bits == 8)
        {
            return source.Data[offset + x];
        }

        return ReadPackedSample(source.Data, offset, x, bits);
    }

    internal static int ReadPackedSample(byte[] data, int rowOffset, int x, int bits)
    {
        int bitOffset = x * bits;
        int shift = 8 - bits - bitOffset % 8;
        return (data[rowOffset + bitOffset / 8] >> shift) & ((1 << bits) - 1);
    }

    internal static void WritePackedSample(byte[] data, int rowOffset, int x, int bits, int value)
    {
        int bitOffset = x * bits;
        int byteOffset = rowOffset + bitOffset / 8;
        int shift = 8 - bits - bitOffset % 8;
        data[byteOffset] = (byte)(data[byteOffset] | value << shift);
    }

    internal static PngChunk? FindChunk(PngChunk[] chunks, string type)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            if (chunk.Type == type)
            {
                return chunk;
            }

            index += 1;
        }

        return null;
    }

    internal static int GetTargetBits(PngPixelFormat format)
    {
        if (format == PngPixelFormat.Grayscale1 || format == PngPixelFormat.Indexed1) return 1;
        if (format == PngPixelFormat.Grayscale2 || format == PngPixelFormat.Indexed2) return 2;
        if (format == PngPixelFormat.Grayscale4 || format == PngPixelFormat.Indexed4) return 4;
        if (format == PngPixelFormat.Grayscale16 || format == PngPixelFormat.Rgb16 || format == PngPixelFormat.GrayscaleAlpha16 || format == PngPixelFormat.Rgba16) return 16;
        if ((int)format >= (int)PngPixelFormat.Grayscale8 && (int)format <= (int)PngPixelFormat.Rgba16) return 8;
        throw new ArgumentOutOfRangeException("PreferredPixelFormat");
    }

    internal static int GetTargetChannels(PngPixelFormat format)
    {
        if (format == PngPixelFormat.Rgb8 || format == PngPixelFormat.Rgb16) return 3;
        if (format == PngPixelFormat.GrayscaleAlpha8 || format == PngPixelFormat.GrayscaleAlpha16) return 2;
        if (format == PngPixelFormat.Rgba8 || format == PngPixelFormat.Rgba16) return 4;
        return 1;
    }

    internal static int GetGrayscaleBits(PngPixelFormat format)
    {
        if (format == PngPixelFormat.Grayscale1) return 1;
        if (format == PngPixelFormat.Grayscale2) return 2;
        if (format == PngPixelFormat.Grayscale4) return 4;
        if (format == PngPixelFormat.Grayscale8) return 8;
        throw new ArgumentOutOfRangeException("Format");
    }

    internal static int GetIndexedBits(PngPixelFormat format)
    {
        if (format == PngPixelFormat.Indexed1) return 1;
        if (format == PngPixelFormat.Indexed2) return 2;
        if (format == PngPixelFormat.Indexed4) return 4;
        if (format == PngPixelFormat.Indexed8) return 8;
        throw new ArgumentOutOfRangeException("Format");
    }

    internal static bool IsIndexedFormat(PngPixelFormat format)
    {
        return format == PngPixelFormat.Indexed1 || format == PngPixelFormat.Indexed2 || format == PngPixelFormat.Indexed4 || format == PngPixelFormat.Indexed8;
    }

    internal static int CalculateStride(int width, PngPixelFormat format, int channels, int bits)
    {
        long stride;
        if (bits < 8)
        {
            stride = ((long)width * bits + 7) / 8;
        }
        else
        {
            stride = (long)width * channels * (bits / 8);
        }

        if (stride > int.MaxValue)
        {
            throw new OverflowException();
        }

        return (int)stride;
    }

    internal static byte[] CreateData(int stride, int height)
    {
        long length = (long)stride * height;
        if (length > int.MaxValue)
        {
            throw new OverflowException();
        }

        return new byte[(int)length];
    }

    internal static PngPixelBuffer CreateBuffer(PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = format;
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Data = data;
        return buffer;
    }

    internal static int ScaleTo16(int value, int maximum)
    {
        return (int)(((long)value * 65535 + maximum / 2) / maximum);
    }

    internal static int ScaleFrom16(int value, int maximum)
    {
        return (int)(((long)value * maximum + 32767) / 65535);
    }

    internal static void WriteUInt16(byte[] data, int offset, int value)
    {
        data[offset] = (byte)((value >> 8) & 255);
        data[offset + 1] = (byte)(value & 255);
    }
}

internal class PngDecoderPixel
{
    internal int Red;
    internal int Green;
    internal int Blue;
    internal int Alpha;
}
