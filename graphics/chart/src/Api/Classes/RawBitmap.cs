namespace chart;
/// <summary>
/// Represents a raw bitmap used by the chart model or rendering pipeline.
/// </summary>
/// <remarks>Rows begin at the top of the image. <c>Stride</c> may include unused bytes after the final pixel in each row.</remarks>
public class RawBitmap : IRawBitmap
{
    internal int Width;
    internal int Height;
    internal int Stride;
    internal ChartPixelFormat PixelFormat;
    internal byte[] Buffer;
    internal ChartColor[] Palette;
    internal bool HasCustomPalette;
    /// <summary>
    /// Initializes a raw bitmap from the supplied width, height, and pixel format.
    /// </summary>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "pixelFormat">The byte ordering and number of channels stored for each pixel.</param>
    public RawBitmap(int width, int height, ChartPixelFormat pixelFormat)
    {
        int stride = CalculateStride(width, pixelFormat);
        Width = width;
        Height = height;
        Stride = stride;
        PixelFormat = pixelFormat;
        Buffer = CreateBuffer(width, height, stride);
        Palette = CreateDefaultPalette(pixelFormat);
        HasCustomPalette = false;
    }

    internal static RawBitmap CreateWithStride(int width, int height, int stride, ChartPixelFormat pixelFormat)
    {
        ValidateDimensions(width, height);
        ValidateStride(width, stride, pixelFormat);
        RawBitmap bitmap = new RawBitmap(width, height, pixelFormat);
        if (bitmap.Stride != stride)
        {
            bitmap.Stride = stride;
            bitmap.Buffer = CreateBuffer(width, height, stride);
        }

        return bitmap;
    }

    internal static RawBitmap Wrap(int width, int height, int stride, ChartPixelFormat pixelFormat, byte[] buffer)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        ValidateDimensions(width, height);
        ValidateStride(width, stride, pixelFormat);
        ValidateBufferLength(height, stride, buffer.Length);
        RawBitmap bitmap = new RawBitmap(width, height, pixelFormat);
        bitmap.Stride = stride;
        bitmap.Buffer = buffer;
        return bitmap;
    }

    internal static RawBitmap WrapIndexed(int width, int height, int stride, byte[] buffer, ChartColor[] palette)
    {
        RawBitmap bitmap = Wrap(width, height, stride, ChartPixelFormat.Indexed8, buffer);
        bitmap.SetPalette(palette);
        return bitmap;
    }

    /// <summary>
    /// Applies the bytes per pixel operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting int.</returns>
    public int BytesPerPixel()
    {
        return BytesPerPixel(PixelFormat);
    }

    /// <summary>
    /// Returns the requested byte offset.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns>The resulting int.</returns>
    public int GetByteOffset(int x, int y)
    {
        ValidateCoordinates(x, y);
        return y * Stride + x * BytesPerPixel();
    }

    /// <summary>
    /// Determines whether this value contains pixel.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool ContainsPixel(int x, int y)
    {
        return x >= 0 && y >= 0 && x < Width && y < Height;
    }

    /// <summary>
    /// Returns the requested pixel.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns>The resulting color.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartColor GetPixel(int x, int y)
    {
        int offset = GetByteOffset(x, y);
        if (PixelFormat == ChartPixelFormat.Rgba32)
        {
            return new ChartColor(Buffer[offset], Buffer[offset + 1], Buffer[offset + 2], Buffer[offset + 3]);
        }

        if (PixelFormat == ChartPixelFormat.Bgra32)
        {
            return new ChartColor(Buffer[offset + 2], Buffer[offset + 1], Buffer[offset], Buffer[offset + 3]);
        }

        if (PixelFormat == ChartPixelFormat.Rgb24)
        {
            return new ChartColor(Buffer[offset], Buffer[offset + 1], Buffer[offset + 2], 255);
        }

        if (PixelFormat == ChartPixelFormat.Grayscale8)
        {
            byte gray = Buffer[offset];
            return new ChartColor(gray, gray, gray, 255);
        }

        if (PixelFormat == ChartPixelFormat.Alpha8)
        {
            return new ChartColor(0, 0, 0, Buffer[offset]);
        }

        if (PixelFormat == ChartPixelFormat.Indexed8)
        {
            byte index = Buffer[offset];
            if (index >= Palette.Length)
            {
                throw new System.InvalidOperationException("Indexed pixel refers to a missing palette entry.");
            }

            return Palette[index].Copy();
        }

        throw new System.ArgumentException("Unsupported pixel format.");
    }

    /// <summary>
    /// Sets the pixel configuration.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetPixel(int x, int y, ChartColor color)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        int offset = GetByteOffset(x, y);
        if (PixelFormat == ChartPixelFormat.Rgba32)
        {
            Buffer[offset] = color.R;
            Buffer[offset + 1] = color.G;
            Buffer[offset + 2] = color.B;
            Buffer[offset + 3] = color.A;
            return;
        }

        if (PixelFormat == ChartPixelFormat.Bgra32)
        {
            Buffer[offset] = color.B;
            Buffer[offset + 1] = color.G;
            Buffer[offset + 2] = color.R;
            Buffer[offset + 3] = color.A;
            return;
        }

        if (PixelFormat == ChartPixelFormat.Rgb24)
        {
            Buffer[offset] = color.R;
            Buffer[offset + 1] = color.G;
            Buffer[offset + 2] = color.B;
            return;
        }

        if (PixelFormat == ChartPixelFormat.Grayscale8)
        {
            Buffer[offset] = Luminance(color);
            return;
        }

        if (PixelFormat == ChartPixelFormat.Alpha8)
        {
            Buffer[offset] = color.A;
            return;
        }

        if (PixelFormat == ChartPixelFormat.Indexed8)
        {
            if (HasCustomPalette)
            {
                Buffer[offset] = (byte)FindNearestPaletteIndex(color, Palette);
            }
            else
            {
                Buffer[offset] = Luminance(color);
            }
            return;
        }

        throw new System.ArgumentException("Unsupported pixel format.");
    }

    /// <summary>
    /// Creates an independent copy of the buffer.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public byte[] CopyBuffer()
    {
        byte[] copy = new byte[Buffer.Length];
        System.Array.Copy(Buffer, copy, Buffer.Length);
        return copy;
    }

    /// <summary>Returns a mutable zero-copy view of the complete bitmap buffer, including row padding.</summary>
    /// <returns>A span backed by this bitmap's buffer.</returns>
    public System.Span<byte> GetBufferSpan()
    {
        return new System.Span<byte>(Buffer);
    }

    /// <summary>Returns a mutable zero-copy view of one complete bitmap row, including row padding.</summary>
    /// <param name = "row">The zero-based row index.</param>
    /// <returns>A span backed by the requested bitmap row.</returns>
    public System.Span<byte> GetRowSpan(int row)
    {
        if (row < 0 || row >= Height)
        {
            throw new System.ArgumentOutOfRangeException("row");
        }

        return new System.Span<byte>(Buffer, row * Stride, Stride);
    }

    /// <summary>Returns a read-only zero-copy view of the complete bitmap buffer.</summary>
    /// <returns>A read-only span backed by this bitmap's buffer.</returns>
    public System.ReadOnlySpan<byte> GetReadOnlyBufferSpan()
    {
        return new System.ReadOnlySpan<byte>(Buffer);
    }

    /// <summary>Returns a mutable zero-copy view of the pixel bytes in one row, excluding row padding.</summary>
    /// <param name = "row">The zero-based row index.</param>
    /// <returns>A span backed by the requested row's pixel bytes.</returns>
    public System.Span<byte> GetPixelRowSpan(int row)
    {
        ValidateRow(row);
        return new System.Span<byte>(Buffer, row * Stride, CalculateStride(Width, PixelFormat));
    }

    /// <summary>Returns a read-only zero-copy view of one complete bitmap row, including row padding.</summary>
    /// <param name = "row">The zero-based row index.</param>
    /// <returns>A read-only span backed by the requested bitmap row.</returns>
    public System.ReadOnlySpan<byte> GetReadOnlyRowSpan(int row)
    {
        ValidateRow(row);
        return new System.ReadOnlySpan<byte>(Buffer, row * Stride, Stride);
    }

    /// <summary>Returns a read-only zero-copy view of the pixel bytes in one row, excluding row padding.</summary>
    /// <param name = "row">The zero-based row index.</param>
    /// <returns>A read-only span backed by the requested row's pixel bytes.</returns>
    public System.ReadOnlySpan<byte> GetReadOnlyPixelRowSpan(int row)
    {
        ValidateRow(row);
        return new System.ReadOnlySpan<byte>(Buffer, row * Stride, CalculateStride(Width, PixelFormat));
    }

    /// <summary>Returns an independent copy of this bitmap, including stride, buffer, and indexed palette.</summary>
    /// <returns>An independently mutable raw bitmap.</returns>
    public RawBitmap Copy()
    {
        RawBitmap copy = Wrap(Width, Height, Stride, PixelFormat, CopyBuffer());
        copy.Palette = CopyPalette(Palette);
        copy.HasCustomPalette = HasCustomPalette;
        return copy;
    }

    /// <summary>Converts every pixel to the requested storage format.</summary>
    /// <param name = "pixelFormat">The destination pixel format.</param>
    /// <returns>A tightly packed converted bitmap.</returns>
    public RawBitmap ConvertTo(ChartPixelFormat pixelFormat)
    {
        BytesPerPixel(pixelFormat);
        if (pixelFormat == PixelFormat)
        {
            return CopyTightlyPacked();
        }

        if (pixelFormat == ChartPixelFormat.Indexed8)
        {
            return ConvertToIndexed(CreatePaletteForBitmap(this));
        }

        RawBitmap converted = new RawBitmap(Width, Height, pixelFormat);
        int y = 0;
        while (y < Height)
        {
            int x = 0;
            while (x < Width)
            {
                converted.SetPixel(x, y, GetPixel(x, y));
                x = x + 1;
            }

            y = y + 1;
        }

        return converted;
    }

    /// <summary>Converts every pixel to indexed storage using the supplied palette.</summary>
    /// <param name = "palette">One through 256 colors used for indexed pixels.</param>
    /// <returns>A tightly packed indexed bitmap.</returns>
    public RawBitmap ConvertToIndexed(ChartColor[] palette)
    {
        ValidatePalette(palette);
        RawBitmap converted = new RawBitmap(Width, Height, ChartPixelFormat.Indexed8);
        converted.SetPalette(palette);
        int y = 0;
        while (y < Height)
        {
            int x = 0;
            while (x < Width)
            {
                int index = FindNearestPaletteIndex(GetPixel(x, y), converted.Palette);
                converted.Buffer[y * converted.Stride + x] = (byte)index;
                x = x + 1;
            }

            y = y + 1;
        }

        return converted;
    }

    /// <summary>Replaces the palette used by an indexed bitmap.</summary>
    /// <param name = "palette">One through 256 colors copied into this bitmap.</param>
    public void SetPalette(ChartColor[] palette)
    {
        ValidateIndexedFormat();
        ValidatePalette(palette);
        ValidatePaletteBuffer(palette);
        Palette = CopyPalette(palette);
        HasCustomPalette = true;
    }

    /// <summary>Returns an independent copy of the indexed palette.</summary>
    /// <returns>The configured palette colors, or an empty array for a direct-color bitmap.</returns>
    public ChartColor[] GetPalette()
    {
        return CopyPalette(Palette);
    }

    /// <summary>Returns the number of colors in the indexed palette.</summary>
    /// <returns>The palette color count.</returns>
    public int GetPaletteCount()
    {
        return Palette.Length;
    }

    /// <summary>Returns an independent copy of one indexed palette color.</summary>
    /// <param name = "index">The zero-based palette index.</param>
    /// <returns>The requested palette color.</returns>
    public ChartColor GetPaletteColor(int index)
    {
        ValidatePaletteIndex(index);
        return Palette[index].Copy();
    }

    /// <summary>Replaces one indexed palette color.</summary>
    /// <param name = "index">The zero-based palette index.</param>
    /// <param name = "color">The replacement palette color.</param>
    public void SetPaletteColor(int index, ChartColor color)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        ValidatePaletteIndex(index);
        Palette[index] = color.Copy();
        HasCustomPalette = true;
    }

    /// <summary>Returns the stored palette index for one indexed pixel.</summary>
    /// <param name = "x">The pixel x coordinate.</param>
    /// <param name = "y">The pixel y coordinate.</param>
    /// <returns>The stored palette index.</returns>
    public int GetPaletteIndex(int x, int y)
    {
        ValidateIndexedFormat();
        return Buffer[GetByteOffset(x, y)];
    }

    /// <summary>Stores a palette index for one indexed pixel.</summary>
    /// <param name = "x">The pixel x coordinate.</param>
    /// <param name = "y">The pixel y coordinate.</param>
    /// <param name = "paletteIndex">The zero-based palette index.</param>
    public void SetPaletteIndex(int x, int y, int paletteIndex)
    {
        ValidatePaletteIndex(paletteIndex);
        Buffer[GetByteOffset(x, y)] = (byte)paletteIndex;
    }

    internal static int CalculateStride(int width, ChartPixelFormat pixelFormat)
    {
        if (width <= 0)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        long stride = (long)width * BytesPerPixel(pixelFormat);
        if (stride > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        return (int)stride;
    }

    internal static int BytesPerPixel(ChartPixelFormat pixelFormat)
    {
        if (pixelFormat == ChartPixelFormat.Rgba32)
        {
            return 4;
        }

        if (pixelFormat == ChartPixelFormat.Bgra32)
        {
            return 4;
        }

        if (pixelFormat == ChartPixelFormat.Rgb24)
        {
            return 3;
        }

        if (pixelFormat == ChartPixelFormat.Grayscale8)
        {
            return 1;
        }

        if (pixelFormat == ChartPixelFormat.Alpha8)
        {
            return 1;
        }

        if (pixelFormat == ChartPixelFormat.Indexed8)
        {
            return 1;
        }

        throw new System.ArgumentException("Unsupported pixel format.");
    }

    internal static byte Luminance(ChartColor color)
    {
        double value = color.R * 0.299 + color.G * 0.587 + color.B * 0.114;
        return ChartColor.RoundToByte(value);
    }

    internal static ChartColor[] CreateDefaultPalette(ChartPixelFormat pixelFormat)
    {
        if (pixelFormat != ChartPixelFormat.Indexed8)
        {
            return new ChartColor[0];
        }

        ChartColor[] palette = new ChartColor[256];
        int index = 0;
        while (index < palette.Length)
        {
            byte value = (byte)index;
            palette[index] = new ChartColor(value, value, value, 255);
            index = index + 1;
        }

        return palette;
    }

    internal static ChartColor[] CopyPalette(ChartColor[] palette)
    {
        if (palette == null)
        {
            throw new System.ArgumentNullException("palette");
        }

        ChartColor[] copy = new ChartColor[palette.Length];
        int index = 0;
        while (index < palette.Length)
        {
            if (palette[index] == null)
            {
                throw new System.ArgumentException("Palette cannot contain null colors.");
            }

            copy[index] = palette[index].Copy();
            index = index + 1;
        }

        return copy;
    }

    internal static void ValidatePalette(ChartColor[] palette)
    {
        if (palette == null)
        {
            throw new System.ArgumentNullException("palette");
        }
        if (palette.Length < 1 || palette.Length > 256)
        {
            throw new System.ArgumentOutOfRangeException("palette");
        }

        int index = 0;
        while (index < palette.Length)
        {
            if (palette[index] == null)
            {
                throw new System.ArgumentException("Palette cannot contain null colors.");
            }

            index = index + 1;
        }
    }

    internal static int FindNearestPaletteIndex(ChartColor color, ChartColor[] palette)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }
        ValidatePalette(palette);
        long bestDistance = long.MaxValue;
        int bestIndex = 0;
        int index = 0;
        while (index < palette.Length)
        {
            long red = color.R - palette[index].R;
            long green = color.G - palette[index].G;
            long blue = color.B - palette[index].B;
            long alpha = color.A - palette[index].A;
            long distance = red * red + green * green + blue * blue + alpha * alpha;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = index;
            }

            index = index + 1;
        }

        return bestIndex;
    }

    internal static ChartColor[] CreatePaletteForBitmap(RawBitmap bitmap)
    {
        if (bitmap == null)
        {
            throw new System.ArgumentNullException("bitmap");
        }

        ChartColor[] colors = new ChartColor[256];
        int count = 0;
        bool hasTransparency = BitmapHasTransparency(bitmap);
        int y = 0;
        while (y < bitmap.Height)
        {
            int x = 0;
            while (x < bitmap.Width)
            {
                ChartColor color = bitmap.GetPixel(x, y);
                if (!PaletteContains(colors, count, color))
                {
                    if (count >= 256)
                    {
                        return CreateQuantizedPalette(hasTransparency);
                    }

                    colors[count] = color.Copy();
                    count = count + 1;
                }

                x = x + 1;
            }

            y = y + 1;
        }

        ChartColor[] result = new ChartColor[count];
        System.Array.Copy(colors, result, count);
        return result;
    }

    internal static bool BitmapHasTransparency(RawBitmap bitmap)
    {
        int y = 0;
        while (y < bitmap.Height)
        {
            int x = 0;
            while (x < bitmap.Width)
            {
                if (bitmap.GetPixel(x, y).A < 255)
                {
                    return true;
                }

                x = x + 1;
            }

            y = y + 1;
        }

        return false;
    }

    internal static bool PaletteContains(ChartColor[] colors, int count, ChartColor color)
    {
        int index = 0;
        while (index < count)
        {
            if (colors[index].R == color.R && colors[index].G == color.G && colors[index].B == color.B && colors[index].A == color.A)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static ChartColor[] CreateQuantizedPalette(bool hasTransparency)
    {
        ChartColor[] palette = new ChartColor[256];
        int index = 0;
        while (index < palette.Length)
        {
            if (hasTransparency)
            {
                int alpha = (index >> 6) & 3;
                int red = (index >> 4) & 3;
                int green = (index >> 2) & 3;
                int blue = index & 3;
                palette[index] = new ChartColor(ScalePaletteLevel(red, 3), ScalePaletteLevel(green, 3), ScalePaletteLevel(blue, 3), ScalePaletteLevel(alpha, 3));
            }
            else
            {
                int red = (index >> 5) & 7;
                int green = (index >> 2) & 7;
                int blue = index & 3;
                palette[index] = new ChartColor(ScalePaletteLevel(red, 7), ScalePaletteLevel(green, 7), ScalePaletteLevel(blue, 3), 255);
            }

            index = index + 1;
        }

        return palette;
    }

    internal static byte ScalePaletteLevel(int level, int maximum)
    {
        return ChartColor.RoundToByte(level * 255.0 / maximum);
    }

    internal RawBitmap CopyTightlyPacked()
    {
        RawBitmap copy = new RawBitmap(Width, Height, PixelFormat);
        copy.Palette = CopyPalette(Palette);
        copy.HasCustomPalette = HasCustomPalette;
        int row = 0;
        while (row < Height)
        {
            System.Array.Copy(Buffer, row * Stride, copy.Buffer, row * copy.Stride, copy.Stride);
            row = row + 1;
        }

        return copy;
    }

    internal static void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new System.ArgumentOutOfRangeException("height");
        }
    }

    internal static void ValidateStride(int width, int stride, ChartPixelFormat pixelFormat)
    {
        int minimumStride = CalculateStride(width, pixelFormat);
        if (stride < minimumStride)
        {
            throw new System.ArgumentOutOfRangeException("stride");
        }
    }

    internal static void ValidateBufferLength(int height, int stride, int bufferLength)
    {
        long required = (long)height * stride;
        if (required > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("stride");
        }

        if (bufferLength < required)
        {
            throw new System.ArgumentException("Buffer is too small for bitmap dimensions.");
        }
    }

    internal static byte[] CreateBuffer(int width, int height, int stride)
    {
        ValidateDimensions(width, height);
        if (stride < 0)
        {
            throw new System.ArgumentOutOfRangeException("stride");
        }

        long length = (long)height * stride;
        if (length > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("stride");
        }

        return new byte[(int)length];
    }

    internal void ValidateCoordinates(int x, int y)
    {
        if (x < 0 || x >= Width)
        {
            throw new System.ArgumentOutOfRangeException("x");
        }

        if (y < 0 || y >= Height)
        {
            throw new System.ArgumentOutOfRangeException("y");
        }
    }

    internal void ValidateRow(int row)
    {
        if (row < 0 || row >= Height)
        {
            throw new System.ArgumentOutOfRangeException("row");
        }
    }

    internal void ValidateIndexedFormat()
    {
        if (PixelFormat != ChartPixelFormat.Indexed8)
        {
            throw new System.InvalidOperationException("Operation requires an indexed bitmap.");
        }
    }

    internal void ValidatePaletteIndex(int index)
    {
        ValidateIndexedFormat();
        if (index < 0 || index >= Palette.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }

    internal void ValidatePaletteBuffer(ChartColor[] palette)
    {
        int y = 0;
        while (y < Height)
        {
            int x = 0;
            while (x < Width)
            {
                int index = Buffer[y * Stride + x];
                if (index >= palette.Length)
                {
                    throw new System.ArgumentException("Palette does not contain every stored pixel index.");
                }

                x = x + 1;
            }

            y = y + 1;
        }
    }

    /// <summary>Returns the current width.</summary>
    /// <returns>The current field value.</returns>
    public int GetWidth()
    {
        return Width;
    }

    /// <summary>Replaces the current width.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetWidth(int value)
    {
        Width = value;
    }

    /// <summary>Returns the current height.</summary>
    /// <returns>The current field value.</returns>
    public int GetHeight()
    {
        return Height;
    }

    /// <summary>Replaces the current height.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHeight(int value)
    {
        Height = value;
    }

    /// <summary>Returns the current stride.</summary>
    /// <returns>The current field value.</returns>
    public int GetStride()
    {
        return Stride;
    }

    /// <summary>Replaces the current stride.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetStride(int value)
    {
        Stride = value;
    }

    /// <summary>Returns the current pixel format.</summary>
    /// <returns>The current field value.</returns>
    public ChartPixelFormat GetPixelFormat()
    {
        return PixelFormat;
    }

    /// <summary>Replaces the current pixel format.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetPixelFormat(ChartPixelFormat value)
    {
        PixelFormat = value;
    }

    /// <summary>Returns the current buffer.</summary>
    /// <returns>The current field value.</returns>
    public byte[] GetBuffer()
    {
        return Buffer;
    }

    /// <summary>Replaces the current buffer.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBuffer(byte[] value)
    {
        Buffer = value;
    }
}
