using System;
using System.IO;
using System.IO.Compression;

namespace pdf;

internal static class PdfPngDecoder
{
    internal static PdfPngData Decode(PdfPngParseState state, byte[] encodedData)
    {
        int expectedLength = GetExpectedDecompressedLength(state);
        byte[] decompressed = Decompress(state.CompressedData.ToArray(), expectedLength);
        int[] samples = new int[state.Width * state.Height * state.ChannelCount];
        int position = 0;
        if (state.IsInterlaced)
        {
            int[] startX = new int[] { 0, 4, 0, 2, 0, 1, 0 };
            int[] startY = new int[] { 0, 0, 4, 0, 2, 0, 1 };
            int[] stepX = new int[] { 8, 8, 4, 4, 2, 2, 1 };
            int[] stepY = new int[] { 8, 8, 8, 4, 4, 2, 2 };
            for (int pass = 0; pass < 7; pass++)
            {
                int passWidth = GetPassSize(state.Width, startX[pass], stepX[pass]);
                int passHeight = GetPassSize(state.Height, startY[pass], stepY[pass]);
                if (passWidth > 0 && passHeight > 0)
                {
                    position = DecodePass(decompressed, position, samples, state, startX[pass], startY[pass], stepX[pass], stepY[pass], passWidth, passHeight);
                }
            }
        }
        else
        {
            position = DecodePass(decompressed, position, samples, state, 0, 0, 1, 1, state.Width, state.Height);
        }

        if (position != decompressed.Length)
        {
            throw new InvalidDataException("PNG decompressed image data has an invalid length.");
        }

        PdfPngColorSpace colorSpace;
        int outputBits = state.BitDepth;
        byte[] pixelData;
        byte[] alphaData = new byte[0];
        int alphaBits = 0;
        bool hasAlpha = false;
        if (state.ColorTypeValue == 0)
        {
            colorSpace = PdfPngColorSpace.Grayscale;
            pixelData = BuildSelectedSamples(samples, state.Width, state.Height, state.ChannelCount, new int[] { 0 }, state.BitDepth);
            if (state.HasTransparency)
            {
                alphaData = BuildTransparencyAlpha(samples, state);
                alphaBits = 8;
                hasAlpha = true;
            }
        }
        else if (state.ColorTypeValue == 2)
        {
            colorSpace = PdfPngColorSpace.Rgb;
            pixelData = BuildSelectedSamples(samples, state.Width, state.Height, state.ChannelCount, new int[] { 0, 1, 2 }, state.BitDepth);
            if (state.HasTransparency)
            {
                alphaData = BuildTransparencyAlpha(samples, state);
                alphaBits = 8;
                hasAlpha = true;
            }
        }
        else if (state.ColorTypeValue == 3)
        {
            colorSpace = PdfPngColorSpace.Rgb;
            outputBits = 8;
            pixelData = BuildPalettePixels(samples, state);
            if (state.HasTransparency)
            {
                alphaData = BuildTransparencyAlpha(samples, state);
                alphaBits = 8;
                hasAlpha = true;
            }
        }
        else if (state.ColorTypeValue == 4)
        {
            colorSpace = PdfPngColorSpace.Grayscale;
            pixelData = BuildSelectedSamples(samples, state.Width, state.Height, state.ChannelCount, new int[] { 0 }, state.BitDepth);
            alphaData = BuildSelectedSamples(samples, state.Width, state.Height, state.ChannelCount, new int[] { 1 }, state.BitDepth);
            alphaBits = state.BitDepth;
            hasAlpha = true;
        }
        else
        {
            colorSpace = PdfPngColorSpace.Rgb;
            pixelData = BuildSelectedSamples(samples, state.Width, state.Height, state.ChannelCount, new int[] { 0, 1, 2 }, state.BitDepth);
            alphaData = BuildSelectedSamples(samples, state.Width, state.Height, state.ChannelCount, new int[] { 3 }, state.BitDepth);
            alphaBits = state.BitDepth;
            hasAlpha = true;
        }

        byte[] compressedPixels = Compress(pixelData);
        byte[] compressedAlpha = new byte[0];
        if (hasAlpha)
        {
            compressedAlpha = Compress(alphaData);
        }

        return new PdfPngData(state.Width, state.Height, state.BitDepth, (PdfPngColorType)state.ColorTypeValue, colorSpace, hasAlpha, state.IsInterlaced, state.HasPhysicalDimensions, state.HorizontalDpi, state.VerticalDpi, outputBits, alphaBits, encodedData, compressedPixels, compressedAlpha);
    }

    internal static int GetExpectedDecompressedLength(PdfPngParseState state)
    {
        long expected = 0;
        if (state.IsInterlaced)
        {
            int[] startX = new int[] { 0, 4, 0, 2, 0, 1, 0 };
            int[] startY = new int[] { 0, 0, 4, 0, 2, 0, 1 };
            int[] stepX = new int[] { 8, 8, 4, 4, 2, 2, 1 };
            int[] stepY = new int[] { 8, 8, 8, 4, 4, 2, 2 };
            for (int pass = 0; pass < 7; pass++)
            {
                int passWidth = GetPassSize(state.Width, startX[pass], stepX[pass]);
                int passHeight = GetPassSize(state.Height, startY[pass], stepY[pass]);
                if (passWidth > 0 && passHeight > 0)
                {
                    long rowBytes = ((long)passWidth * state.ChannelCount * state.BitDepth + 7) / 8;
                    expected = expected + (rowBytes + 1) * passHeight;
                }
            }
        }
        else
        {
            long rowBytes = ((long)state.Width * state.ChannelCount * state.BitDepth + 7) / 8;
            expected = (rowBytes + 1) * state.Height;
        }

        if (expected <= 0 || expected > int.MaxValue)
        {
            throw new InvalidDataException("PNG decompressed image data exceeds the supported size.");
        }

        return (int)expected;
    }

    internal static int GetPassSize(int total, int start, int step)
    {
        if (total <= start)
        {
            return 0;
        }

        return (total - start + step - 1) / step;
    }

    internal static byte[] Decompress(byte[] compressedData, int expectedLength)
    {
        byte[] result = new byte[expectedLength];
        using (MemoryStream input = new MemoryStream(compressedData))
        {
            using (ZLibStream decompressed = new ZLibStream(input, CompressionMode.Decompress, true))
            {
                int position = 0;
                while (position < result.Length)
                {
                    int read = decompressed.Read(result, position, result.Length - position);
                    if (read == 0)
                    {
                        throw new InvalidDataException("PNG compressed image data is truncated.");
                    }

                    position = position + read;
                }

                byte[] extra = new byte[1];
                if (decompressed.Read(extra, 0, 1) != 0)
                {
                    throw new InvalidDataException("PNG compressed image data expands beyond the expected size.");
                }
            }
        }

        return result;
    }

    internal static int DecodePass(byte[] decompressed, int position, int[] samples, PdfPngParseState state, int startX, int startY, int stepX, int stepY, int passWidth, int passHeight)
    {
        int rowBytes = (int)(((long)passWidth * state.ChannelCount * state.BitDepth + 7) / 8);
        int bytesPerPixel = (state.ChannelCount * state.BitDepth + 7) / 8;
        if (bytesPerPixel < 1)
        {
            bytesPerPixel = 1;
        }

        byte[] previous = new byte[rowBytes];
        for (int passY = 0; passY < passHeight; passY++)
        {
            int filter = decompressed[position];
            position++;
            if (filter < 0 || filter > 4)
            {
                throw new InvalidDataException("PNG scanline uses an invalid filter type.");
            }

            byte[] row = new byte[rowBytes];
            for (int index = 0; index < rowBytes; index++)
            {
                int encoded = decompressed[position];
                position++;
                int left = index >= bytesPerPixel ? row[index - bytesPerPixel] : 0;
                int above = previous[index];
                int upperLeft = index >= bytesPerPixel ? previous[index - bytesPerPixel] : 0;
                int value = encoded;
                if (filter == 1)
                {
                    value = encoded + left;
                }
                else if (filter == 2)
                {
                    value = encoded + above;
                }
                else if (filter == 3)
                {
                    value = encoded + (left + above) / 2;
                }
                else if (filter == 4)
                {
                    value = encoded + Paeth(left, above, upperLeft);
                }

                row[index] = (byte)(value & 255);
            }

            int imageY = startY + passY * stepY;
            for (int passX = 0; passX < passWidth; passX++)
            {
                int imageX = startX + passX * stepX;
                for (int channel = 0; channel < state.ChannelCount; channel++)
                {
                    int passSample = passX * state.ChannelCount + channel;
                    int imageSample = (imageY * state.Width + imageX) * state.ChannelCount + channel;
                    samples[imageSample] = ReadSample(row, passSample, state.BitDepth);
                }
            }

            previous = row;
        }

        return position;
    }

    internal static int Paeth(int left, int above, int upperLeft)
    {
        int estimate = left + above - upperLeft;
        int leftDistance = Math.Abs(estimate - left);
        int aboveDistance = Math.Abs(estimate - above);
        int upperLeftDistance = Math.Abs(estimate - upperLeft);
        if (leftDistance <= aboveDistance && leftDistance <= upperLeftDistance)
        {
            return left;
        }

        if (aboveDistance <= upperLeftDistance)
        {
            return above;
        }

        return upperLeft;
    }

    internal static int ReadSample(byte[] row, int sampleIndex, int bitDepth)
    {
        if (bitDepth == 8)
        {
            return row[sampleIndex];
        }

        if (bitDepth == 16)
        {
            int offset = sampleIndex * 2;
            return row[offset] * 256 + row[offset + 1];
        }

        int bitOffset = sampleIndex * bitDepth;
        int shift = 8 - bitDepth - bitOffset % 8;
        int mask = (1 << bitDepth) - 1;
        return row[bitOffset / 8] >> shift & mask;
    }

    internal static byte[] BuildSelectedSamples(int[] samples, int width, int height, int sourceChannels, int[] selectedChannels, int bitDepth)
    {
        long rowBits = (long)width * selectedChannels.Length * bitDepth;
        int rowBytes = (int)((rowBits + 7) / 8);
        byte[] result = new byte[rowBytes * height];
        for (int y = 0; y < height; y++)
        {
            int rowOffset = y * rowBytes;
            for (int x = 0; x < width; x++)
            {
                for (int selected = 0; selected < selectedChannels.Length; selected++)
                {
                    int sourceIndex = (y * width + x) * sourceChannels + selectedChannels[selected];
                    int outputSample = x * selectedChannels.Length + selected;
                    WriteSample(result, rowOffset, outputSample, bitDepth, samples[sourceIndex]);
                }
            }
        }

        return result;
    }

    internal static void WriteSample(byte[] output, int rowOffset, int sampleIndex, int bitDepth, int value)
    {
        if (bitDepth == 8)
        {
            output[rowOffset + sampleIndex] = (byte)value;
            return;
        }

        if (bitDepth == 16)
        {
            int offset = rowOffset + sampleIndex * 2;
            output[offset] = (byte)(value >> 8);
            output[offset + 1] = (byte)(value & 255);
            return;
        }

        int bitOffset = sampleIndex * bitDepth;
        int shift = 8 - bitDepth - bitOffset % 8;
        output[rowOffset + bitOffset / 8] = (byte)(output[rowOffset + bitOffset / 8] | value << shift);
    }

    internal static byte[] BuildPalettePixels(int[] samples, PdfPngParseState state)
    {
        int pixelCount = state.Width * state.Height;
        byte[] result = new byte[pixelCount * 3];
        int entryCount = state.Palette.Length / 3;
        for (int pixel = 0; pixel < pixelCount; pixel++)
        {
            int paletteIndex = samples[pixel];
            if (paletteIndex >= entryCount)
            {
                throw new InvalidDataException("PNG image data references a missing palette entry.");
            }

            result[pixel * 3] = state.Palette[paletteIndex * 3];
            result[pixel * 3 + 1] = state.Palette[paletteIndex * 3 + 1];
            result[pixel * 3 + 2] = state.Palette[paletteIndex * 3 + 2];
        }

        return result;
    }

    internal static byte[] BuildTransparencyAlpha(int[] samples, PdfPngParseState state)
    {
        int pixelCount = state.Width * state.Height;
        byte[] result = new byte[pixelCount];
        for (int pixel = 0; pixel < pixelCount; pixel++)
        {
            int alpha = 255;
            int sampleOffset = pixel * state.ChannelCount;
            if (state.ColorTypeValue == 0)
            {
                if (samples[sampleOffset] == state.TransparentGray)
                {
                    alpha = 0;
                }
            }
            else if (state.ColorTypeValue == 2)
            {
                if (samples[sampleOffset] == state.TransparentRed && samples[sampleOffset + 1] == state.TransparentGreen && samples[sampleOffset + 2] == state.TransparentBlue)
                {
                    alpha = 0;
                }
            }
            else
            {
                int paletteIndex = samples[sampleOffset];
                if (paletteIndex < state.Transparency.Length)
                {
                    alpha = state.Transparency[paletteIndex];
                }
            }

            result[pixel] = (byte)alpha;
        }

        return result;
    }

    internal static byte[] Compress(byte[] data)
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (ZLibStream compressed = new ZLibStream(output, CompressionLevel.Optimal, true))
            {
                compressed.Write(data);
            }

            return output.ToArray();
        }
    }
}
