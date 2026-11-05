namespace barcode.Tests;

internal static class IndependentRenderParser
{
    internal static BarcodeImage Parse(BarcodeRenderedImage rendered)
    {
        if (rendered.Format == BarcodeRenderFormat.Png)
        {
            return ParsePng(rendered.Bytes);
        }

        if (rendered.Format == BarcodeRenderFormat.Bmp)
        {
            return ParseBmp(rendered.Bytes);
        }

        if (rendered.Format == BarcodeRenderFormat.Svg)
        {
            return ParseSvg(rendered.Text);
        }

        throw new global::System.IO.InvalidDataException(
            "The rendered format is not independently parseable.");
    }

    static BarcodeImage ParsePng(byte[]? source)
    {
        if (source == null || source.Length < 33)
        {
            throw new global::System.IO.InvalidDataException(
                "The PNG data is incomplete.");
        }

        byte[] signature = new byte[]
        {
            137, 80, 78, 71, 13, 10, 26, 10
        };
        int signatureIndex = 0;
        while (signatureIndex < signature.Length)
        {
            if (source[signatureIndex] != signature[signatureIndex])
            {
                throw new global::System.IO.InvalidDataException(
                    "The PNG signature is invalid.");
            }

            signatureIndex++;
        }

        int width = 0;
        int height = 0;
        int colorType = -1;
        bool foundHeader = false;
        bool foundEnd = false;
        global::System.Collections.Generic.List<byte> compressed =
            new global::System.Collections.Generic.List<byte>();
        int offset = 8;
        while (offset < source.Length)
        {
            RequireAvailable(source, offset, 12, "PNG chunk header");
            int length = ReadBigEndianInteger(source, offset);
            if (length < 0)
            {
                throw new global::System.IO.InvalidDataException(
                    "A PNG chunk has a negative length.");
            }

            RequireAvailable(source, offset + 8, length + 4, "PNG chunk");
            string type = global::System.Text.Encoding.ASCII.GetString(
                source,
                offset + 4,
                4);
            uint expectedCrc = ReadBigEndianUnsignedInteger(
                source,
                offset + 8 + length);
            uint actualCrc = CalculateChunkCrc(
                source,
                offset + 4,
                length + 4);
            if (expectedCrc != actualCrc)
            {
                throw new global::System.IO.InvalidDataException(
                    "A PNG chunk CRC is invalid.");
            }

            if (type == "IHDR")
            {
                if (foundHeader || offset != 8 || length != 13)
                {
                    throw new global::System.IO.InvalidDataException(
                        "The PNG header is invalid.");
                }

                width = ReadBigEndianInteger(source, offset + 8);
                height = ReadBigEndianInteger(source, offset + 12);
                int bitDepth = source[offset + 16];
                colorType = source[offset + 17];
                if (width <= 0 || height <= 0 || bitDepth != 8
                    || source[offset + 18] != 0
                    || source[offset + 19] != 0
                    || source[offset + 20] != 0)
                {
                    throw new global::System.IO.InvalidDataException(
                        "The PNG image header is unsupported.");
                }

                foundHeader = true;
            }
            else if (type == "IDAT")
            {
                int index = 0;
                while (index < length)
                {
                    compressed.Add(source[offset + 8 + index]);
                    index++;
                }
            }
            else if (type == "IEND")
            {
                if (length != 0)
                {
                    throw new global::System.IO.InvalidDataException(
                        "The PNG end chunk is invalid.");
                }

                foundEnd = true;
                offset += 12;
                break;
            }

            offset += length + 12;
        }

        if (!foundHeader || !foundEnd || offset != source.Length
            || compressed.Count == 0)
        {
            throw new global::System.IO.InvalidDataException(
                "The PNG chunk sequence is incomplete.");
        }

        int channels = ChannelCount(colorType);
        byte[] scanlines = Decompress(compressed.ToArray());
        byte[] samples = Unfilter(scanlines, width, height, channels);
        return CreateImage(
            width,
            height,
            ConvertSamplesToGray(samples, width, height, colorType));
    }

    static BarcodeImage ParseBmp(byte[]? source)
    {
        if (source == null || source.Length < 54
            || source[0] != (byte)'B' || source[1] != (byte)'M')
        {
            throw new global::System.IO.InvalidDataException(
                "The BMP data is incomplete or has an invalid signature.");
        }

        int fileSize = ReadLittleEndianInteger(source, 2);
        int pixelOffset = ReadLittleEndianInteger(source, 10);
        int headerSize = ReadLittleEndianInteger(source, 14);
        int width = ReadLittleEndianInteger(source, 18);
        int height = ReadLittleEndianInteger(source, 22);
        int planes = ReadLittleEndianShort(source, 26);
        int bitsPerPixel = ReadLittleEndianShort(source, 28);
        int compression = ReadLittleEndianInteger(source, 30);
        int imageSize = ReadLittleEndianInteger(source, 34);
        if (fileSize != source.Length || width <= 0 || height <= 0
            || planes != 1
            || (bitsPerPixel != 1
                && bitsPerPixel != 24
                && bitsPerPixel != 32)
            || (bitsPerPixel == 32 ? compression != 3 : compression != 0)
            || (headerSize != 40 && headerSize != 108))
        {
            throw new global::System.IO.InvalidDataException(
                "The BMP header is invalid or unsupported.");
        }

        int stride = ((width * bitsPerPixel + 31) / 32) * 4;
        if (imageSize != stride * height
            || pixelOffset < 14 + headerSize
            || pixelOffset + imageSize != source.Length)
        {
            throw new global::System.IO.InvalidDataException(
                "The BMP pixel bounds are invalid.");
        }

        if (bitsPerPixel == 32
            && (ReadLittleEndianUnsignedInteger(source, 54) != 0x00FF0000u
                || ReadLittleEndianUnsignedInteger(source, 58) != 0x0000FF00u
                || ReadLittleEndianUnsignedInteger(source, 62) != 0x000000FFu
                || ReadLittleEndianUnsignedInteger(source, 66) != 0xFF000000u))
        {
            throw new global::System.IO.InvalidDataException(
                "The BMP channel masks are unsupported.");
        }

        byte[] palette = bitsPerPixel == 1
            ? ReadBmpPalette(source, headerSize, pixelOffset)
            : new byte[0];
        byte[] pixels = new byte[width * height];
        int y = 0;
        while (y < height)
        {
            int sourceRow = height - y - 1;
            int rowOffset = pixelOffset + sourceRow * stride;
            int x = 0;
            while (x < width)
            {
                int red;
                int green;
                int blue;
                int alpha = 255;
                if (bitsPerPixel == 1)
                {
                    int paletteIndex = (source[rowOffset + x / 8]
                        >> (7 - x % 8)) & 1;
                    int paletteOffset = paletteIndex * 3;
                    red = palette[paletteOffset];
                    green = palette[paletteOffset + 1];
                    blue = palette[paletteOffset + 2];
                }
                else
                {
                    int bytesPerPixel = bitsPerPixel / 8;
                    int sampleOffset = rowOffset + x * bytesPerPixel;
                    blue = source[sampleOffset];
                    green = source[sampleOffset + 1];
                    red = source[sampleOffset + 2];
                    if (bitsPerPixel == 32)
                    {
                        alpha = source[sampleOffset + 3];
                    }
                }

                pixels[y * width + x] = ToGray(red, green, blue, alpha);
                x++;
            }

            y++;
        }

        return CreateImage(width, height, pixels);
    }

    static BarcodeImage ParseSvg(string? source)
    {
        if (source == null)
        {
            throw new global::System.IO.InvalidDataException(
                "The SVG text is missing.");
        }

        global::System.Xml.Linq.XDocument document =
            global::System.Xml.Linq.XDocument.Parse(
                source,
                global::System.Xml.Linq.LoadOptions.None);
        global::System.Xml.Linq.XElement? root = document.Root;
        if (root == null
            || root.Name.LocalName != "svg"
            || root.Name.NamespaceName != "http://www.w3.org/2000/svg")
        {
            throw new global::System.IO.InvalidDataException(
                "The SVG root element is invalid.");
        }

        int width = ReadSvgInteger(root, "width");
        int height = ReadSvgInteger(root, "height");
        string expectedViewBox = "0 0 " + width + " " + height;
        if ((string?)root.Attribute("viewBox") != expectedViewBox
            || (string?)root.Attribute("shape-rendering") != "crispEdges")
        {
            throw new global::System.IO.InvalidDataException(
                "The SVG viewport is invalid.");
        }

        byte[] pixels = new byte[width * height];
        global::System.Array.Fill(pixels, (byte)255);
        global::System.Xml.Linq.XNamespace svg =
            "http://www.w3.org/2000/svg";
        foreach (global::System.Xml.Linq.XElement rectangle
            in root.Elements(svg + "rect"))
        {
            int rectangleWidth = ReadSvgInteger(rectangle, "width");
            int rectangleHeight = ReadSvgInteger(rectangle, "height");
            FillRectangle(
                pixels,
                width,
                height,
                0,
                0,
                rectangleWidth,
                rectangleHeight,
                ReadSvgGray(rectangle));
        }

        global::System.Text.RegularExpressions.Regex pathPattern =
            new global::System.Text.RegularExpressions.Regex(
                @"M(?<x>\d+) (?<y>\d+)h(?<width>\d+)v(?<height>\d+)h-(?<close>\d+)Z",
                global::System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        foreach (global::System.Xml.Linq.XElement path
            in root.Elements(svg + "path"))
        {
            string? data = (string?)path.Attribute("d");
            if (data == null)
            {
                throw new global::System.IO.InvalidDataException(
                    "An SVG path has no geometry.");
            }

            global::System.Text.RegularExpressions.MatchCollection matches =
                pathPattern.Matches(data);
            string remaining = pathPattern.Replace(data, "").Replace(" ", "");
            if (matches.Count == 0 || remaining.Length != 0)
            {
                throw new global::System.IO.InvalidDataException(
                    "An SVG path command is unsupported.");
            }

            byte sample = ReadSvgGray(path);
            foreach (global::System.Text.RegularExpressions.Match match
                in matches)
            {
                int rectangleWidth = ParseInvariantInteger(
                    match.Groups["width"].Value);
                if (rectangleWidth != ParseInvariantInteger(
                    match.Groups["close"].Value))
                {
                    throw new global::System.IO.InvalidDataException(
                        "An SVG path does not close to its starting point.");
                }

                FillRectangle(
                    pixels,
                    width,
                    height,
                    ParseInvariantInteger(match.Groups["x"].Value),
                    ParseInvariantInteger(match.Groups["y"].Value),
                    rectangleWidth,
                    ParseInvariantInteger(match.Groups["height"].Value),
                    sample);
            }
        }

        return CreateImage(width, height, pixels);
    }

    static byte[] ReadBmpPalette(byte[] source, int headerSize, int pixelOffset)
    {
        int paletteOffset = 14 + headerSize;
        if (pixelOffset - paletteOffset != 8)
        {
            throw new global::System.IO.InvalidDataException(
                "The indexed BMP palette is invalid.");
        }

        byte[] palette = new byte[6];
        int index = 0;
        while (index < 2)
        {
            int sourceOffset = paletteOffset + index * 4;
            palette[index * 3] = source[sourceOffset + 2];
            palette[index * 3 + 1] = source[sourceOffset + 1];
            palette[index * 3 + 2] = source[sourceOffset];
            index++;
        }

        return palette;
    }

    static byte[] Decompress(byte[] source)
    {
        using (global::System.IO.MemoryStream input =
            new global::System.IO.MemoryStream(source))
        using (global::System.IO.Compression.ZLibStream decompressor =
            new global::System.IO.Compression.ZLibStream(
                input,
                global::System.IO.Compression.CompressionMode.Decompress))
        using (global::System.IO.MemoryStream output =
            new global::System.IO.MemoryStream())
        {
            decompressor.CopyTo(output);
            return output.ToArray();
        }
    }

    static byte[] Unfilter(
        byte[] source,
        int width,
        int height,
        int channels)
    {
        int rowBytes = width * channels;
        if (source.Length != (rowBytes + 1) * height)
        {
            throw new global::System.IO.InvalidDataException(
                "The PNG scanline size is invalid.");
        }

        byte[] result = new byte[rowBytes * height];
        int row = 0;
        while (row < height)
        {
            int inputOffset = row * (rowBytes + 1);
            int filter = source[inputOffset];
            if (filter < 0 || filter > 4)
            {
                throw new global::System.IO.InvalidDataException(
                    "The PNG filter type is invalid.");
            }

            int index = 0;
            while (index < rowBytes)
            {
                int left = index < channels
                    ? 0
                    : result[row * rowBytes + index - channels];
                int above = row == 0
                    ? 0
                    : result[(row - 1) * rowBytes + index];
                int upperLeft = row == 0 || index < channels
                    ? 0
                    : result[(row - 1) * rowBytes + index - channels];
                int predictor = PngPredictor(
                    filter,
                    left,
                    above,
                    upperLeft);
                result[row * rowBytes + index] = (byte)(
                    (source[inputOffset + index + 1] + predictor) & 255);
                index++;
            }

            row++;
        }

        return result;
    }

    static byte[] ConvertSamplesToGray(
        byte[] samples,
        int width,
        int height,
        int colorType)
    {
        int channels = ChannelCount(colorType);
        byte[] pixels = new byte[width * height];
        int pixel = 0;
        while (pixel < pixels.Length)
        {
            int offset = pixel * channels;
            int red;
            int green;
            int blue;
            int alpha = 255;
            if (colorType == 0 || colorType == 4)
            {
                red = samples[offset];
                green = red;
                blue = red;
                if (colorType == 4)
                {
                    alpha = samples[offset + 1];
                }
            }
            else
            {
                red = samples[offset];
                green = samples[offset + 1];
                blue = samples[offset + 2];
                if (colorType == 6)
                {
                    alpha = samples[offset + 3];
                }
            }

            pixels[pixel] = ToGray(red, green, blue, alpha);
            pixel++;
        }

        return pixels;
    }

    static int ChannelCount(int colorType)
    {
        switch (colorType)
        {
            case 0:
                return 1;
            case 2:
                return 3;
            case 4:
                return 2;
            case 6:
                return 4;
            default:
                throw new global::System.IO.InvalidDataException(
                    "The PNG color type is unsupported.");
        }
    }

    static int PngPredictor(
        int filter,
        int left,
        int above,
        int upperLeft)
    {
        switch (filter)
        {
            case 0:
                return 0;
            case 1:
                return left;
            case 2:
                return above;
            case 3:
                return (left + above) / 2;
            default:
                return Paeth(left, above, upperLeft);
        }
    }

    static int Paeth(int left, int above, int upperLeft)
    {
        int estimate = left + above - upperLeft;
        int leftDistance = global::System.Math.Abs(estimate - left);
        int aboveDistance = global::System.Math.Abs(estimate - above);
        int upperLeftDistance = global::System.Math.Abs(
            estimate - upperLeft);
        if (leftDistance <= aboveDistance
            && leftDistance <= upperLeftDistance)
        {
            return left;
        }

        return aboveDistance <= upperLeftDistance ? above : upperLeft;
    }

    static byte ReadSvgGray(global::System.Xml.Linq.XElement element)
    {
        string? fill = (string?)element.Attribute("fill");
        if (fill == null || fill.Length != 7 || fill[0] != '#')
        {
            throw new global::System.IO.InvalidDataException(
                "An SVG fill color is invalid.");
        }

        int red = ParseHexByte(fill, 1);
        int green = ParseHexByte(fill, 3);
        int blue = ParseHexByte(fill, 5);
        int alpha = 255;
        string? opacity = (string?)element.Attribute("fill-opacity");
        if (opacity != null)
        {
            double value = double.Parse(
                opacity,
                global::System.Globalization.CultureInfo.InvariantCulture);
            if (value < 0.0 || value > 1.0)
            {
                throw new global::System.IO.InvalidDataException(
                    "An SVG fill opacity is invalid.");
            }

            alpha = (int)global::System.Math.Round(value * 255.0);
        }

        return ToGray(red, green, blue, alpha);
    }

    static int ReadSvgInteger(
        global::System.Xml.Linq.XElement element,
        string name)
    {
        string? value = (string?)element.Attribute(name);
        if (value == null)
        {
            throw new global::System.IO.InvalidDataException(
                "An SVG dimension is missing.");
        }

        int parsed = ParseInvariantInteger(value);
        if (parsed <= 0)
        {
            throw new global::System.IO.InvalidDataException(
                "An SVG dimension is invalid.");
        }

        return parsed;
    }

    static int ParseInvariantInteger(string value)
    {
        return int.Parse(
            value,
            global::System.Globalization.NumberStyles.None,
            global::System.Globalization.CultureInfo.InvariantCulture);
    }

    static int ParseHexByte(string value, int offset)
    {
        return int.Parse(
            value.Substring(offset, 2),
            global::System.Globalization.NumberStyles.HexNumber,
            global::System.Globalization.CultureInfo.InvariantCulture);
    }

    static void FillRectangle(
        byte[] pixels,
        int imageWidth,
        int imageHeight,
        int x,
        int y,
        int width,
        int height,
        byte sample)
    {
        if (x < 0 || y < 0 || width <= 0 || height <= 0
            || x + width > imageWidth || y + height > imageHeight)
        {
            throw new global::System.IO.InvalidDataException(
                "An SVG rectangle is outside the viewport.");
        }

        int row = y;
        while (row < y + height)
        {
            int column = x;
            while (column < x + width)
            {
                pixels[row * imageWidth + column] = sample;
                column++;
            }

            row++;
        }
    }

    static byte ToGray(int red, int green, int blue, int alpha)
    {
        int compositedRed = CompositeOverWhite(red, alpha);
        int compositedGreen = CompositeOverWhite(green, alpha);
        int compositedBlue = CompositeOverWhite(blue, alpha);
        return (byte)((77 * compositedRed
            + 150 * compositedGreen
            + 29 * compositedBlue
            + 128) >> 8);
    }

    static int CompositeOverWhite(int value, int alpha)
    {
        return (value * alpha + 255 * (255 - alpha) + 127) / 255;
    }

    static BarcodeImage CreateImage(int width, int height, byte[] pixels)
    {
        BarcodeImage image = new BarcodeImage();
        image.Pixels = pixels;
        image.Width = width;
        image.Height = height;
        image.Stride = width;
        image.PixelFormat = BarcodePixelFormat.Gray8;
        return image;
    }

    static void RequireAvailable(
        byte[] source,
        int offset,
        int count,
        string description)
    {
        if (offset < 0 || count < 0 || offset > source.Length - count)
        {
            throw new global::System.IO.InvalidDataException(
                "The " + description + " exceeds the source bounds.");
        }
    }

    static int ReadBigEndianInteger(byte[] source, int offset)
    {
        return (source[offset] << 24)
            | (source[offset + 1] << 16)
            | (source[offset + 2] << 8)
            | source[offset + 3];
    }

    static uint ReadBigEndianUnsignedInteger(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24)
            | ((uint)source[offset + 1] << 16)
            | ((uint)source[offset + 2] << 8)
            | source[offset + 3];
    }

    static int ReadLittleEndianShort(byte[] source, int offset)
    {
        return source[offset] | source[offset + 1] << 8;
    }

    static int ReadLittleEndianInteger(byte[] source, int offset)
    {
        return source[offset]
            | source[offset + 1] << 8
            | source[offset + 2] << 16
            | source[offset + 3] << 24;
    }

    static uint ReadLittleEndianUnsignedInteger(byte[] source, int offset)
    {
        return source[offset]
            | (uint)source[offset + 1] << 8
            | (uint)source[offset + 2] << 16
            | (uint)source[offset + 3] << 24;
    }

    static uint CalculateChunkCrc(byte[] source, int offset, int count)
    {
        uint crc = 0xFFFFFFFFu;
        int index = 0;
        while (index < count)
        {
            crc ^= source[offset + index];
            int bit = 0;
            while (bit < 8)
            {
                crc = (crc & 1u) == 0u
                    ? crc >> 1
                    : crc >> 1 ^ 0xEDB88320u;
                bit++;
            }

            index++;
        }

        return crc ^ 0xFFFFFFFFu;
    }
}
