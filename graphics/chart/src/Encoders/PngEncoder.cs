namespace chart;

internal class PngEncoder
{
    internal const byte FilterNone = 0;
    internal const byte FilterSub = 1;
    internal const byte FilterUp = 2;
    internal const byte FilterAverage = 3;
    internal const byte FilterPaeth = 4;

    internal static byte[] Signature()
    {
        return new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
    }

    internal static byte[] Encode(RawBitmap bitmap)
    {
        if (bitmap == null)
        {
            throw new System.ArgumentNullException("bitmap");
        }

        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        byte[] signature = Signature();
        stream.Write(signature, 0, signature.Length);
        WriteChunk(stream, "IHDR", CreateIhdrData(bitmap.Width, bitmap.Height));
        WriteChunk(stream, "IDAT", CompressZlib(CreateAdaptiveFilteredScanlines(bitmap)));
        WriteChunk(stream, "IEND", new byte[0]);
        return stream.ToArray();
    }

    internal static byte[] EncodeWithOptions(RawBitmap bitmap, ChartPngOptions options)
    {
        ValidateOptions(bitmap, options);
        ChartPngColorType colorType = ResolveColorType(bitmap, options.ColorType);
        RawBitmap source = bitmap;
        if (colorType == ChartPngColorType.Indexed && bitmap.PixelFormat != ChartPixelFormat.Indexed8)
        {
            source = bitmap.ConvertTo(ChartPixelFormat.Indexed8);
        }

        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        byte[] signature = Signature();
        stream.Write(signature, 0, signature.Length);
        WriteChunk(stream, "IHDR", CreateIhdrDataWithColorType(source.Width, source.Height, colorType));
        WriteMetadataChunks(stream, options.Metadata);
        if (colorType == ChartPngColorType.Indexed)
        {
            ChartColor[] palette = source.GetPalette();
            WriteChunk(stream, "PLTE", CreatePaletteData(palette));
            byte[] transparency = CreateTransparencyData(palette);
            if (transparency.Length > 0)
            {
                WriteChunk(stream, "tRNS", transparency);
            }
        }

        byte[] scanlines = CreateOptionScanlines(source, colorType, options.Filter);
        WriteChunk(stream, "IDAT", CompressZlibWithLevel(scanlines, options.Compression));
        WriteChunk(stream, "IEND", new byte[0]);
        return stream.ToArray();
    }

    internal static void WriteStreamWithOptions(RawBitmap bitmap, System.IO.Stream stream, ChartPngOptions options)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }
        if (!stream.CanWrite)
        {
            throw new System.ArgumentException("Stream must be writable.");
        }

        byte[] bytes = EncodeWithOptions(bitmap, options);
        stream.Write(bytes, 0, bytes.Length);
    }

    internal static void WriteFileWithOptions(RawBitmap bitmap, string path, ChartPngOptions options)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        System.IO.File.WriteAllBytes(path, EncodeWithOptions(bitmap, options));
    }

    internal static byte[] CreateIhdrData(int width, int height)
    {
        if (width <= 0)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new System.ArgumentOutOfRangeException("height");
        }

        byte[] data = new byte[13];
        WriteUInt32BigEndian(data, 0, (uint)width);
        WriteUInt32BigEndian(data, 4, (uint)height);
        data[8] = 8;
        data[9] = 6;
        data[10] = 0;
        data[11] = 0;
        data[12] = 0;
        return data;
    }

    internal static byte[] CreateIhdrDataWithColorType(int width, int height, ChartPngColorType colorType)
    {
        byte[] data = CreateIhdrData(width, height);
        data[9] = PngColorTypeValue(colorType);
        return data;
    }

    internal static byte PngColorTypeValue(ChartPngColorType colorType)
    {
        if (colorType == ChartPngColorType.Grayscale)
        {
            return 0;
        }
        if (colorType == ChartPngColorType.Truecolor)
        {
            return 2;
        }
        if (colorType == ChartPngColorType.Indexed)
        {
            return 3;
        }
        if (colorType == ChartPngColorType.GrayscaleAlpha)
        {
            return 4;
        }
        if (colorType == ChartPngColorType.TruecolorAlpha)
        {
            return 6;
        }

        throw new System.ArgumentOutOfRangeException("colorType");
    }

    internal static ChartPngColorType ResolveColorType(RawBitmap bitmap, ChartPngColorType colorType)
    {
        if (colorType != ChartPngColorType.Automatic)
        {
            PngColorTypeValue(colorType);
            return colorType;
        }
        if (bitmap.PixelFormat == ChartPixelFormat.Grayscale8)
        {
            return ChartPngColorType.Grayscale;
        }
        if (bitmap.PixelFormat == ChartPixelFormat.Rgb24)
        {
            return ChartPngColorType.Truecolor;
        }
        if (bitmap.PixelFormat == ChartPixelFormat.Indexed8)
        {
            return ChartPngColorType.Indexed;
        }
        if (bitmap.PixelFormat == ChartPixelFormat.Alpha8)
        {
            return ChartPngColorType.GrayscaleAlpha;
        }
        if (bitmap.PixelFormat == ChartPixelFormat.Rgba32 || bitmap.PixelFormat == ChartPixelFormat.Bgra32)
        {
            return ChartPngColorType.TruecolorAlpha;
        }

        throw new System.ArgumentException("Unsupported source pixel format.");
    }

    internal static byte[] CreatePaletteData(ChartColor[] palette)
    {
        RawBitmap.ValidatePalette(palette);
        byte[] data = new byte[palette.Length * 3];
        int index = 0;
        while (index < palette.Length)
        {
            int offset = index * 3;
            data[offset] = palette[index].R;
            data[offset + 1] = palette[index].G;
            data[offset + 2] = palette[index].B;
            index = index + 1;
        }

        return data;
    }

    internal static byte[] CreateTransparencyData(ChartColor[] palette)
    {
        RawBitmap.ValidatePalette(palette);
        int lastTransparent = -1;
        int index = 0;
        while (index < palette.Length)
        {
            if (palette[index].A < 255)
            {
                lastTransparent = index;
            }

            index = index + 1;
        }
        if (lastTransparent < 0)
        {
            return new byte[0];
        }

        byte[] data = new byte[lastTransparent + 1];
        index = 0;
        while (index < data.Length)
        {
            data[index] = palette[index].A;
            index = index + 1;
        }

        return data;
    }

    internal static void WriteMetadataChunks(System.IO.Stream stream, ChartPngMetadata metadata)
    {
        WriteMetadataValue(stream, "Title", metadata.Title);
        WriteMetadataValue(stream, "Author", metadata.Author);
        WriteMetadataValue(stream, "Description", metadata.Description);
        WriteMetadataValue(stream, "Copyright", metadata.Copyright);
        WriteMetadataValue(stream, "Software", metadata.Software);
    }

    internal static void WriteMetadataValue(System.IO.Stream stream, string keyword, string value)
    {
        ValidateMetadataValue(value);
        if (value.Length > 0)
        {
            WriteChunk(stream, "iTXt", CreateInternationalTextData(keyword, value));
        }
    }

    internal static byte[] CreateInternationalTextData(string keyword, string value)
    {
        ChartTextBlock.ValidateText(keyword, "keyword");
        ValidateMetadataValue(value);
        byte[] keywordBytes = System.Text.Encoding.ASCII.GetBytes(keyword);
        byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(value);
        byte[] data = new byte[keywordBytes.Length + textBytes.Length + 5];
        System.Array.Copy(keywordBytes, 0, data, 0, keywordBytes.Length);
        System.Array.Copy(textBytes, 0, data, keywordBytes.Length + 5, textBytes.Length);
        return data;
    }

    internal static void ValidateMetadataValue(string value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("metadata");
        }
        if (value.IndexOf('\0') >= 0)
        {
            throw new System.ArgumentException("PNG metadata cannot contain null characters.");
        }
    }

    internal static byte[] CreateRgbaScanlines(RawBitmap bitmap)
    {
        return CreateFilteredScanlines(bitmap, FilterNone);
    }

    internal static byte[] CreateFilteredScanlines(RawBitmap bitmap, byte filterType)
    {
        if (IsFilterType(filterType))
        {
            return CreateFilteredScanlinesWithFilter(bitmap, filterType);
        }

        throw new System.ArgumentException("Unsupported PNG filter type.");
    }

    internal static byte[] CreateFilteredScanlinesNone(RawBitmap bitmap)
    {
        return CreateFilteredScanlinesWithFilter(bitmap, FilterNone);
    }

    internal static byte[] CreateAdaptiveFilteredScanlines(RawBitmap bitmap)
    {
        if (bitmap == null)
        {
            throw new System.ArgumentNullException("bitmap");
        }

        int bytesPerPixel = 4;
        int sourceRowLength = CalculateRgbaRowLength(bitmap.Width);
        int outputRowLength = sourceRowLength + 1;
        int dataLength = CalculateScanlineDataLength(outputRowLength, bitmap.Height);
        byte[] data = new byte[dataLength];
        byte[] previousRow = new byte[sourceRowLength];
        byte[] currentRow = new byte[sourceRowLength];
        int y = 0;
        while (y < bitmap.Height)
        {
            CopyRgbaRow(bitmap, y, currentRow);

            byte filterType = SelectAdaptiveFilter(currentRow, previousRow, bytesPerPixel);
            int rowOffset = y * outputRowLength;
            data[rowOffset] = filterType;
            WriteFilteredRow(currentRow, previousRow, data, rowOffset + 1, filterType, bytesPerPixel);

            byte[] swap = previousRow;
            previousRow = currentRow;
            currentRow = swap;
            y = y + 1;
        }

        return data;
    }

    internal static byte[] CreateOptionScanlines(RawBitmap bitmap, ChartPngColorType colorType, ChartPngFilter filter)
    {
        int bytesPerPixel = PngBytesPerPixel(colorType);
        long rowLengthValue = (long)bitmap.Width * bytesPerPixel;
        if (rowLengthValue > int.MaxValue - 1)
        {
            throw new System.ArgumentOutOfRangeException("bitmap");
        }

        int sourceRowLength = (int)rowLengthValue;
        int outputRowLength = sourceRowLength + 1;
        int dataLength = CalculateScanlineDataLength(outputRowLength, bitmap.Height);
        byte[] data = new byte[dataLength];
        byte[] previousRow = new byte[sourceRowLength];
        byte[] currentRow = new byte[sourceRowLength];
        int y = 0;
        while (y < bitmap.Height)
        {
            CopyOptionRow(bitmap, y, currentRow, colorType);
            byte filterType = ResolveFilterType(filter);
            if (filter == ChartPngFilter.Adaptive)
            {
                filterType = SelectAdaptiveFilter(currentRow, previousRow, bytesPerPixel);
            }

            int rowOffset = y * outputRowLength;
            data[rowOffset] = filterType;
            WriteFilteredRow(currentRow, previousRow, data, rowOffset + 1, filterType, bytesPerPixel);
            byte[] swap = previousRow;
            previousRow = currentRow;
            currentRow = swap;
            y = y + 1;
        }

        return data;
    }

    internal static int PngBytesPerPixel(ChartPngColorType colorType)
    {
        if (colorType == ChartPngColorType.Grayscale || colorType == ChartPngColorType.Indexed)
        {
            return 1;
        }
        if (colorType == ChartPngColorType.GrayscaleAlpha)
        {
            return 2;
        }
        if (colorType == ChartPngColorType.Truecolor)
        {
            return 3;
        }
        if (colorType == ChartPngColorType.TruecolorAlpha)
        {
            return 4;
        }

        throw new System.ArgumentOutOfRangeException("colorType");
    }

    internal static byte ResolveFilterType(ChartPngFilter filter)
    {
        if (filter == ChartPngFilter.Adaptive || filter == ChartPngFilter.None)
        {
            return FilterNone;
        }
        if (filter == ChartPngFilter.Sub)
        {
            return FilterSub;
        }
        if (filter == ChartPngFilter.Up)
        {
            return FilterUp;
        }
        if (filter == ChartPngFilter.Average)
        {
            return FilterAverage;
        }
        if (filter == ChartPngFilter.Paeth)
        {
            return FilterPaeth;
        }

        throw new System.ArgumentOutOfRangeException("Filter");
    }

    internal static void CopyOptionRow(RawBitmap bitmap, int y, byte[] row, ChartPngColorType colorType)
    {
        int x = 0;
        while (x < bitmap.Width)
        {
            ChartColor color = bitmap.GetPixel(x, y);
            if (colorType == ChartPngColorType.Grayscale)
            {
                row[x] = RawBitmap.Luminance(color);
            }
            else if (colorType == ChartPngColorType.Indexed)
            {
                row[x] = (byte)bitmap.GetPaletteIndex(x, y);
            }
            else if (colorType == ChartPngColorType.GrayscaleAlpha)
            {
                int offset = x * 2;
                row[offset] = RawBitmap.Luminance(color);
                row[offset + 1] = color.A;
            }
            else if (colorType == ChartPngColorType.Truecolor)
            {
                int offset = x * 3;
                row[offset] = color.R;
                row[offset + 1] = color.G;
                row[offset + 2] = color.B;
            }
            else if (colorType == ChartPngColorType.TruecolorAlpha)
            {
                int offset = x * 4;
                row[offset] = color.R;
                row[offset + 1] = color.G;
                row[offset + 2] = color.B;
                row[offset + 3] = color.A;
            }
            else
            {
                throw new System.ArgumentOutOfRangeException("colorType");
            }

            x = x + 1;
        }
    }

    internal static byte SelectAdaptiveFilter(byte[] row, byte[] previousRow, int bytesPerPixel)
    {
        ValidateFilterRows(row, previousRow, bytesPerPixel);

        byte bestFilter = FilterNone;
        long bestScore = long.MaxValue;
        int filterIndex = 0;
        while (filterIndex <= FilterPaeth)
        {
            byte filterType = (byte)filterIndex;
            long score = ScoreFilteredRow(row, previousRow, filterType, bytesPerPixel);
            if (score < bestScore)
            {
                bestScore = score;
                bestFilter = filterType;
            }

            filterIndex = filterIndex + 1;
        }

        return bestFilter;
    }

    internal static byte[] CreateFilteredScanlinesWithFilter(RawBitmap bitmap, byte filterType)
    {
        if (bitmap == null)
        {
            throw new System.ArgumentNullException("bitmap");
        }

        if (!IsFilterType(filterType))
        {
            throw new System.ArgumentException("Unsupported PNG filter type.");
        }

        int bytesPerPixel = 4;
        int sourceRowLength = CalculateRgbaRowLength(bitmap.Width);
        int outputRowLength = sourceRowLength + 1;
        int dataLength = CalculateScanlineDataLength(outputRowLength, bitmap.Height);
        byte[] data = new byte[dataLength];
        byte[] previousRow = new byte[sourceRowLength];
        byte[] currentRow = new byte[sourceRowLength];
        int y = 0;
        while (y < bitmap.Height)
        {
            CopyRgbaRow(bitmap, y, currentRow);

            int rowOffset = y * outputRowLength;
            data[rowOffset] = filterType;
            WriteFilteredRow(currentRow, previousRow, data, rowOffset + 1, filterType, bytesPerPixel);

            byte[] swap = previousRow;
            previousRow = currentRow;
            currentRow = swap;
            y = y + 1;
        }

        return data;
    }

    internal static void CopyRgbaRow(RawBitmap bitmap, int y, byte[] row)
    {
        int x = 0;
        while (x < bitmap.Width)
        {
            ChartColor color = bitmap.GetPixel(x, y);
            int offset = x * 4;
            row[offset] = color.R;
            row[offset + 1] = color.G;
            row[offset + 2] = color.B;
            row[offset + 3] = color.A;
            x = x + 1;
        }
    }

    internal static void WriteFilteredRow(byte[] row, byte[] previousRow, byte[] output, int outputOffset, byte filterType, int bytesPerPixel)
    {
        ValidateFilterRows(row, previousRow, bytesPerPixel);

        if (output == null)
        {
            throw new System.ArgumentNullException("output");
        }

        int index = 0;
        while (index < row.Length)
        {
            output[outputOffset + index] = FilterByte(row, previousRow, index, filterType, bytesPerPixel);
            index = index + 1;
        }
    }

    internal static long ScoreFilteredRow(byte[] row, byte[] previousRow, byte filterType, int bytesPerPixel)
    {
        ValidateFilterRows(row, previousRow, bytesPerPixel);

        long score = 0;
        int index = 0;
        while (index < row.Length)
        {
            byte value = FilterByte(row, previousRow, index, filterType, bytesPerPixel);
            int amount = 256 - value;
            if (value < 128)
            {
                amount = value;
            }

            score = score + amount;
            index = index + 1;
        }

        return score;
    }

    internal static byte FilterByte(byte[] row, byte[] previousRow, int index, byte filterType, int bytesPerPixel)
    {
        if (!IsFilterType(filterType))
        {
            throw new System.ArgumentException("Unsupported PNG filter type.");
        }

        int raw = row[index];
        int left = 0;
        int upLeft = 0;
        if (index >= bytesPerPixel)
        {
            left = row[index - bytesPerPixel];
            upLeft = previousRow[index - bytesPerPixel];
        }

        int up = previousRow[index];
        int predictor = 0;
        if (filterType == FilterSub)
        {
            predictor = left;
        }
        else if (filterType == FilterUp)
        {
            predictor = up;
        }
        else if (filterType == FilterAverage)
        {
            predictor = (left + up) / 2;
        }
        else if (filterType == FilterPaeth)
        {
            predictor = PaethPredictor(left, up, upLeft);
        }

        return (byte)((raw - predictor) & 255);
    }

    internal static int PaethPredictor(int left, int up, int upLeft)
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

    internal static bool IsFilterType(byte filterType)
    {
        return filterType == FilterNone || filterType == FilterSub || filterType == FilterUp || filterType == FilterAverage || filterType == FilterPaeth;
    }

    internal static void ValidateFilterRows(byte[] row, byte[] previousRow, int bytesPerPixel)
    {
        if (row == null)
        {
            throw new System.ArgumentNullException("row");
        }

        if (previousRow == null)
        {
            throw new System.ArgumentNullException("previousRow");
        }

        if (bytesPerPixel <= 0)
        {
            throw new System.ArgumentOutOfRangeException("bytesPerPixel");
        }

        if (row.Length != previousRow.Length)
        {
            throw new System.ArgumentException("PNG filter rows must have the same length.");
        }
    }

    internal static byte[] CompressZlib(byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        System.IO.MemoryStream output = new System.IO.MemoryStream();
        output.WriteByte(120);
        output.WriteByte(156);

        using (System.IO.Compression.DeflateStream deflate = new System.IO.Compression.DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal, true))
        {
            deflate.Write(data, 0, data.Length);
        }

        uint adler = Adler32(data);
        WriteUInt32BigEndian(output, adler);
        return output.ToArray();
    }

    internal static byte[] CompressZlibWithLevel(byte[] data, ChartPngCompression compression)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        System.IO.Compression.CompressionLevel level = CompressionLevel(compression);
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        output.WriteByte(120);
        if (compression == ChartPngCompression.SmallestSize)
        {
            output.WriteByte(218);
        }
        else if (compression == ChartPngCompression.Optimal)
        {
            output.WriteByte(156);
        }
        else
        {
            output.WriteByte(1);
        }

        using (System.IO.Compression.DeflateStream deflate = new System.IO.Compression.DeflateStream(output, level, true))
        {
            deflate.Write(data, 0, data.Length);
        }

        WriteUInt32BigEndian(output, Adler32(data));
        return output.ToArray();
    }

    internal static System.IO.Compression.CompressionLevel CompressionLevel(ChartPngCompression compression)
    {
        if (compression == ChartPngCompression.Fastest)
        {
            return System.IO.Compression.CompressionLevel.Fastest;
        }
        if (compression == ChartPngCompression.Optimal)
        {
            return System.IO.Compression.CompressionLevel.Optimal;
        }
        if (compression == ChartPngCompression.SmallestSize)
        {
            return System.IO.Compression.CompressionLevel.SmallestSize;
        }
        if (compression == ChartPngCompression.NoCompression)
        {
            return System.IO.Compression.CompressionLevel.NoCompression;
        }

        throw new System.ArgumentOutOfRangeException("Compression");
    }

    internal static void ValidateOptions(RawBitmap bitmap, ChartPngOptions options)
    {
        if (bitmap == null)
        {
            throw new System.ArgumentNullException("bitmap");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (options.Metadata == null)
        {
            throw new System.ArgumentNullException("Metadata");
        }

        ResolveColorType(bitmap, options.ColorType);
        ResolveFilterType(options.Filter);
        CompressionLevel(options.Compression);
        ValidateMetadataValue(options.Metadata.Title);
        ValidateMetadataValue(options.Metadata.Author);
        ValidateMetadataValue(options.Metadata.Description);
        ValidateMetadataValue(options.Metadata.Copyright);
        ValidateMetadataValue(options.Metadata.Software);
    }

    internal static void WriteChunk(System.IO.Stream stream, string type, byte[] data)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }

        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }

        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (type.Length != 4)
        {
            throw new System.ArgumentException("PNG chunk type must be four characters.");
        }

        if (data.Length > int.MaxValue - type.Length)
        {
            throw new System.ArgumentOutOfRangeException("data");
        }

        WriteUInt32BigEndian(stream, (uint)data.Length);
        byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes, 0, typeBytes.Length);
        stream.Write(data, 0, data.Length);

        byte[] crcBytes = new byte[typeBytes.Length + data.Length];
        System.Array.Copy(typeBytes, 0, crcBytes, 0, typeBytes.Length);
        System.Array.Copy(data, 0, crcBytes, typeBytes.Length, data.Length);
        WriteUInt32BigEndian(stream, Crc32(crcBytes));
    }

    internal static uint Crc32(byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        uint crc = 4294967295;
        int index = 0;
        while (index < data.Length)
        {
            crc = crc ^ data[index];
            int bit = 0;
            while (bit < 8)
            {
                if ((crc & 1) == 1)
                {
                    crc = (crc >> 1) ^ 3988292384;
                }
                else
                {
                    crc = crc >> 1;
                }

                bit = bit + 1;
            }

            index = index + 1;
        }

        return crc ^ 4294967295;
    }

    internal static uint Adler32(byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        uint a = 1;
        uint b = 0;
        int index = 0;
        while (index < data.Length)
        {
            a = (a + data[index]) % 65521;
            b = (b + a) % 65521;
            index = index + 1;
        }

        return (b << 16) | a;
    }

    internal static void WriteUInt32BigEndian(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)((value >> 24) & 255);
        bytes[offset + 1] = (byte)((value >> 16) & 255);
        bytes[offset + 2] = (byte)((value >> 8) & 255);
        bytes[offset + 3] = (byte)(value & 255);
    }

    internal static void WriteUInt32BigEndian(System.IO.Stream stream, uint value)
    {
        stream.WriteByte((byte)((value >> 24) & 255));
        stream.WriteByte((byte)((value >> 16) & 255));
        stream.WriteByte((byte)((value >> 8) & 255));
        stream.WriteByte((byte)(value & 255));
    }

    internal static uint ReadUInt32BigEndian(byte[] bytes, int offset)
    {
        return ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];
    }

    internal static int CalculateRgbaRowLength(int width)
    {
        if (width <= 0 || width > (int.MaxValue - 1) / 4)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        return width * 4;
    }

    internal static int CalculateScanlineDataLength(int outputRowLength, int height)
    {
        if (outputRowLength <= 0)
        {
            throw new System.ArgumentOutOfRangeException("outputRowLength");
        }

        if (height <= 0)
        {
            throw new System.ArgumentOutOfRangeException("height");
        }

        long dataLength = (long)outputRowLength * height;
        if (dataLength > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("height");
        }

        return (int)dataLength;
    }
}
