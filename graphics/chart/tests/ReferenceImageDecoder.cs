namespace chart.Tests;

internal class ReferenceDecodedImage
{
    internal int Width;
    internal int Height;
    internal byte[] Pixels;

    internal ReferenceDecodedImage(int width, int height, byte[] pixels)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    internal ChartColor GetPixel(int x, int y)
    {
        int offset = (y * Width + x) * 4;
        return new ChartColor(Pixels[offset], Pixels[offset + 1], Pixels[offset + 2], Pixels[offset + 3]);
    }
}

internal class ReferenceBmpDecoder
{
    internal static ReferenceDecodedImage Decode(byte[] data)
    {
        if (data == null || data.Length < 54)
        {
            throw new System.IO.InvalidDataException("BMP data is truncated.");
        }
        if (data[0] != 66 || data[1] != 77)
        {
            throw new System.IO.InvalidDataException("BMP signature is invalid.");
        }

        int fileSize = ReadInt32(data, 2);
        int pixelOffset = ReadInt32(data, 10);
        int infoSize = ReadInt32(data, 14);
        int width = ReadInt32(data, 18);
        int storedHeight = ReadInt32(data, 22);
        int planes = ReadUInt16(data, 26);
        int bitsPerPixel = ReadUInt16(data, 28);
        int compression = ReadInt32(data, 30);
        if (fileSize != data.Length || infoSize != 40 || width <= 0 || storedHeight == 0 || planes != 1 || compression != 0)
        {
            throw new System.IO.InvalidDataException("BMP header is invalid.");
        }
        if (bitsPerPixel != 8 && bitsPerPixel != 24 && bitsPerPixel != 32)
        {
            throw new System.IO.InvalidDataException("BMP pixel depth is unsupported.");
        }

        int height = System.Math.Abs(storedHeight);
        int rowStride = (((width * bitsPerPixel) + 31) / 32) * 4;
        if (pixelOffset < 54 || pixelOffset + rowStride * height != data.Length)
        {
            throw new System.IO.InvalidDataException("BMP pixel data bounds are invalid.");
        }

        ChartColor[] palette = new ChartColor[0];
        if (bitsPerPixel == 8)
        {
            int paletteCount = ReadInt32(data, 46);
            if (paletteCount == 0)
            {
                paletteCount = 256;
            }
            if (paletteCount < 1 || paletteCount > 256 || 54 + paletteCount * 4 > pixelOffset)
            {
                throw new System.IO.InvalidDataException("BMP palette is invalid.");
            }

            palette = new ChartColor[paletteCount];
            int paletteIndex = 0;
            while (paletteIndex < palette.Length)
            {
                int paletteOffset = 54 + paletteIndex * 4;
                palette[paletteIndex] = new ChartColor(data[paletteOffset + 2], data[paletteOffset + 1], data[paletteOffset], 255);
                paletteIndex++;
            }
        }

        byte[] pixels = new byte[width * height * 4];
        int outputY = 0;
        while (outputY < height)
        {
            int storedY = outputY;
            if (storedHeight > 0)
            {
                storedY = height - outputY - 1;
            }

            int rowOffset = pixelOffset + storedY * rowStride;
            int x = 0;
            while (x < width)
            {
                ChartColor color;
                if (bitsPerPixel == 8)
                {
                    int paletteIndex = data[rowOffset + x];
                    if (paletteIndex >= palette.Length)
                    {
                        throw new System.IO.InvalidDataException("BMP palette index is invalid.");
                    }

                    color = palette[paletteIndex];
                }
                else
                {
                    int sourceOffset = rowOffset + x * (bitsPerPixel / 8);
                    byte alpha = 255;
                    if (bitsPerPixel == 32)
                    {
                        alpha = data[sourceOffset + 3];
                    }

                    color = new ChartColor(data[sourceOffset + 2], data[sourceOffset + 1], data[sourceOffset], alpha);
                }

                WritePixel(pixels, width, x, outputY, color);
                x++;
            }

            outputY++;
        }

        return new ReferenceDecodedImage(width, height, pixels);
    }

    private static int ReadUInt16(byte[] data, int offset)
    {
        return data[offset] | (data[offset + 1] << 8);
    }

    private static int ReadInt32(byte[] data, int offset)
    {
        return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
    }

    private static void WritePixel(byte[] pixels, int width, int x, int y, ChartColor color)
    {
        int offset = (y * width + x) * 4;
        pixels[offset] = color.R;
        pixels[offset + 1] = color.G;
        pixels[offset + 2] = color.B;
        pixels[offset + 3] = color.A;
    }
}

internal class ReferencePngDecoder
{
    internal static ReferenceDecodedImage Decode(byte[] data)
    {
        ValidateSignature(data);
        int width = 0;
        int height = 0;
        int colorType = -1;
        byte[] palette = new byte[0];
        byte[] transparency = new byte[0];
        System.IO.MemoryStream imageData = new System.IO.MemoryStream();
        string previousType = "";
        bool sawHeader = false;
        bool sawImageData = false;
        bool sawEnd = false;
        int offset = 8;
        while (offset < data.Length)
        {
            if (offset + 12 > data.Length)
            {
                throw new System.IO.InvalidDataException("PNG chunk is truncated.");
            }

            int length = ReadLength(data, offset);
            if (length < 0 || offset + 12 + length > data.Length)
            {
                throw new System.IO.InvalidDataException("PNG chunk length is invalid.");
            }

            string type = System.Text.Encoding.ASCII.GetString(data, offset + 4, 4);
            ValidateChunkCrc(data, offset, length);
            int chunkDataOffset = offset + 8;
            if (!sawHeader && type != "IHDR")
            {
                throw new System.IO.InvalidDataException("IHDR must be first.");
            }
            if (type == "IHDR")
            {
                if (sawHeader || length != 13)
                {
                    throw new System.IO.InvalidDataException("IHDR is invalid.");
                }

                width = ReadLength(data, chunkDataOffset);
                height = ReadLength(data, chunkDataOffset + 4);
                colorType = data[chunkDataOffset + 9];
                if (width <= 0 || height <= 0 || data[chunkDataOffset + 8] != 8 || data[chunkDataOffset + 10] != 0 || data[chunkDataOffset + 11] != 0 || data[chunkDataOffset + 12] != 0)
                {
                    throw new System.IO.InvalidDataException("IHDR fields are unsupported.");
                }

                BytesPerPixel(colorType);
                sawHeader = true;
            }
            else if (type == "PLTE")
            {
                palette = CopyRange(data, chunkDataOffset, length);
            }
            else if (type == "tRNS")
            {
                transparency = CopyRange(data, chunkDataOffset, length);
            }
            else if (type == "IDAT")
            {
                if (previousType != "" && sawImageData && previousType != "IDAT")
                {
                    throw new System.IO.InvalidDataException("IDAT chunks must be consecutive.");
                }

                imageData.Write(data, chunkDataOffset, length);
                sawImageData = true;
            }
            else if (type == "IEND")
            {
                if (length != 0 || !sawImageData || offset + 12 != data.Length)
                {
                    throw new System.IO.InvalidDataException("IEND is invalid.");
                }

                sawEnd = true;
            }

            previousType = type;
            offset = offset + length + 12;
        }

        if (!sawHeader || !sawImageData || !sawEnd)
        {
            throw new System.IO.InvalidDataException("PNG critical chunks are missing.");
        }
        if (colorType == 3 && (palette.Length == 0 || palette.Length % 3 != 0 || palette.Length > 768))
        {
            throw new System.IO.InvalidDataException("PNG palette is invalid.");
        }

        byte[] compressed = imageData.ToArray();
        byte[] filtered = Decompress(compressed);
        if (ReadUInt32(compressed, compressed.Length - 4) != Adler32(filtered))
        {
            throw new System.IO.InvalidDataException("PNG zlib Adler checksum is invalid.");
        }

        int bytesPerPixel = BytesPerPixel(colorType);
        byte[] samples = Unfilter(filtered, width, height, bytesPerPixel);
        byte[] pixels = ConvertSamples(samples, width, height, colorType, palette, transparency);
        return new ReferenceDecodedImage(width, height, pixels);
    }

    internal static uint Crc32(byte[] data)
    {
        uint crc = 4294967295;
        int index = 0;
        while (index < data.Length)
        {
            crc = crc ^ data[index];
            int bit = 0;
            while (bit < 8)
            {
                if ((crc & 1) != 0)
                {
                    crc = (crc >> 1) ^ 3988292384;
                }
                else
                {
                    crc = crc >> 1;
                }

                bit++;
            }

            index++;
        }

        return crc ^ 4294967295;
    }

    internal static uint Adler32(byte[] data)
    {
        uint first = 1;
        uint second = 0;
        int index = 0;
        while (index < data.Length)
        {
            first = (first + data[index]) % 65521;
            second = (second + first) % 65521;
            index++;
        }

        return (second << 16) | first;
    }

    private static void ValidateSignature(byte[] data)
    {
        byte[] signature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (data == null || data.Length < signature.Length)
        {
            throw new System.IO.InvalidDataException("PNG data is truncated.");
        }

        int index = 0;
        while (index < signature.Length)
        {
            if (data[index] != signature[index])
            {
                throw new System.IO.InvalidDataException("PNG signature is invalid.");
            }

            index++;
        }
    }

    private static void ValidateChunkCrc(byte[] data, int offset, int length)
    {
        byte[] crcInput = CopyRange(data, offset + 4, length + 4);
        uint expected = ReadUInt32(data, offset + length + 8);
        if (Crc32(crcInput) != expected)
        {
            throw new System.IO.InvalidDataException("PNG chunk CRC is invalid.");
        }
    }

    private static byte[] Decompress(byte[] compressed)
    {
        System.IO.MemoryStream input = new System.IO.MemoryStream(compressed);
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        using (System.IO.Compression.ZLibStream zlib = new System.IO.Compression.ZLibStream(input, System.IO.Compression.CompressionMode.Decompress))
        {
            zlib.CopyTo(output);
        }

        return output.ToArray();
    }

    private static byte[] Unfilter(byte[] filtered, int width, int height, int bytesPerPixel)
    {
        int rowLength = width * bytesPerPixel;
        if (filtered.Length != (rowLength + 1) * height)
        {
            throw new System.IO.InvalidDataException("PNG scanline length is invalid.");
        }

        byte[] samples = new byte[rowLength * height];
        int y = 0;
        while (y < height)
        {
            int filteredOffset = y * (rowLength + 1);
            int outputOffset = y * rowLength;
            int filter = filtered[filteredOffset];
            if (filter < 0 || filter > 4)
            {
                throw new System.IO.InvalidDataException("PNG filter is invalid.");
            }

            int index = 0;
            while (index < rowLength)
            {
                int left = 0;
                int up = 0;
                int upLeft = 0;
                if (index >= bytesPerPixel)
                {
                    left = samples[outputOffset + index - bytesPerPixel];
                    if (y > 0)
                    {
                        upLeft = samples[outputOffset + index - rowLength - bytesPerPixel];
                    }
                }
                if (y > 0)
                {
                    up = samples[outputOffset + index - rowLength];
                }

                int predictor = 0;
                if (filter == 1)
                {
                    predictor = left;
                }
                else if (filter == 2)
                {
                    predictor = up;
                }
                else if (filter == 3)
                {
                    predictor = (left + up) / 2;
                }
                else if (filter == 4)
                {
                    predictor = Paeth(left, up, upLeft);
                }

                samples[outputOffset + index] = (byte)((filtered[filteredOffset + index + 1] + predictor) & 255);
                index++;
            }

            y++;
        }

        return samples;
    }

    private static byte[] ConvertSamples(byte[] samples, int width, int height, int colorType, byte[] palette, byte[] transparency)
    {
        int bytesPerPixel = BytesPerPixel(colorType);
        byte[] pixels = new byte[width * height * 4];
        int pixelIndex = 0;
        while (pixelIndex < width * height)
        {
            int sourceOffset = pixelIndex * bytesPerPixel;
            int outputOffset = pixelIndex * 4;
            if (colorType == 0)
            {
                pixels[outputOffset] = samples[sourceOffset];
                pixels[outputOffset + 1] = samples[sourceOffset];
                pixels[outputOffset + 2] = samples[sourceOffset];
                pixels[outputOffset + 3] = 255;
            }
            else if (colorType == 2)
            {
                pixels[outputOffset] = samples[sourceOffset];
                pixels[outputOffset + 1] = samples[sourceOffset + 1];
                pixels[outputOffset + 2] = samples[sourceOffset + 2];
                pixels[outputOffset + 3] = 255;
            }
            else if (colorType == 3)
            {
                int paletteIndex = samples[sourceOffset];
                if (paletteIndex * 3 + 2 >= palette.Length)
                {
                    throw new System.IO.InvalidDataException("PNG palette index is invalid.");
                }

                pixels[outputOffset] = palette[paletteIndex * 3];
                pixels[outputOffset + 1] = palette[paletteIndex * 3 + 1];
                pixels[outputOffset + 2] = palette[paletteIndex * 3 + 2];
                pixels[outputOffset + 3] = 255;
                if (paletteIndex < transparency.Length)
                {
                    pixels[outputOffset + 3] = transparency[paletteIndex];
                }
            }
            else if (colorType == 4)
            {
                pixels[outputOffset] = samples[sourceOffset];
                pixels[outputOffset + 1] = samples[sourceOffset];
                pixels[outputOffset + 2] = samples[sourceOffset];
                pixels[outputOffset + 3] = samples[sourceOffset + 1];
            }
            else
            {
                pixels[outputOffset] = samples[sourceOffset];
                pixels[outputOffset + 1] = samples[sourceOffset + 1];
                pixels[outputOffset + 2] = samples[sourceOffset + 2];
                pixels[outputOffset + 3] = samples[sourceOffset + 3];
            }

            pixelIndex++;
        }

        return pixels;
    }

    private static int BytesPerPixel(int colorType)
    {
        if (colorType == 0 || colorType == 3)
        {
            return 1;
        }
        if (colorType == 4)
        {
            return 2;
        }
        if (colorType == 2)
        {
            return 3;
        }
        if (colorType == 6)
        {
            return 4;
        }

        throw new System.IO.InvalidDataException("PNG color type is unsupported.");
    }

    private static int Paeth(int left, int up, int upLeft)
    {
        int estimate = left + up - upLeft;
        int leftDistance = System.Math.Abs(estimate - left);
        int upDistance = System.Math.Abs(estimate - up);
        int upLeftDistance = System.Math.Abs(estimate - upLeft);
        if (leftDistance <= upDistance && leftDistance <= upLeftDistance)
        {
            return left;
        }
        if (upDistance <= upLeftDistance)
        {
            return up;
        }

        return upLeft;
    }

    private static byte[] CopyRange(byte[] data, int offset, int length)
    {
        byte[] copy = new byte[length];
        System.Array.Copy(data, offset, copy, 0, length);
        return copy;
    }

    private static int ReadLength(byte[] data, int offset)
    {
        uint value = ReadUInt32(data, offset);
        if (value > int.MaxValue)
        {
            throw new System.IO.InvalidDataException("PNG value is too large.");
        }

        return (int)value;
    }

    private static uint ReadUInt32(byte[] data, int offset)
    {
        if (offset < 0 || offset + 4 > data.Length)
        {
            throw new System.IO.InvalidDataException("PNG integer is truncated.");
        }

        return ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];
    }
}
