namespace qrcode;

internal class BitMatrixBuffer
{
    int Width;
    int Height;
    int ModuleCount;
    byte[] Bits;

    internal BitMatrixBuffer(int width, int height)
    {
        CheckedDimensions.ValidateDimensionBounds(
            width,
            height,
            QrLimits.DefaultMaximumImageDimension,
            QrLimits.DefaultMaximumImageDimension,
            QrLimits.DefaultMaximumImagePixels);
        Width = width;
        Height = height;
        ModuleCount = CheckedDimensions.CalculateArea(
            width,
            height,
            QrLimits.DefaultMaximumImagePixels);
        int paddedBitCount = CheckedDimensions.CalculateSum(
            ModuleCount,
            7,
            int.MaxValue,
            "MatrixBitCount");
        Bits = new byte[paddedBitCount / 8];
    }

    internal int GetWidth()
    {
        return Width;
    }

    internal int GetHeight()
    {
        return Height;
    }

    internal int GetPackedByteLength()
    {
        return Bits.Length;
    }

    internal bool GetModule(int row, int column)
    {
        int moduleIndex = GetModuleIndex(row, column);
        int byteIndex = moduleIndex / 8;
        int bitOffset = 7 - moduleIndex % 8;
        return ((Bits[byteIndex] >> bitOffset) & 1) != 0;
    }

    internal void SetModule(int row, int column, bool value)
    {
        int moduleIndex = GetModuleIndex(row, column);
        int byteIndex = moduleIndex / 8;
        int bitOffset = 7 - moduleIndex % 8;
        int mask = 1 << bitOffset;
        if (value)
        {
            Bits[byteIndex] = (byte)(Bits[byteIndex] | mask);
        }
        else
        {
            Bits[byteIndex] = (byte)(Bits[byteIndex] & ~mask);
        }
    }

    internal void ToggleModule(int row, int column)
    {
        int moduleIndex = GetModuleIndex(row, column);
        int byteIndex = moduleIndex / 8;
        int bitOffset = 7 - moduleIndex % 8;
        Bits[byteIndex] = (byte)(Bits[byteIndex] ^ 1 << bitOffset);
    }

    internal void ClearModules(bool value)
    {
        global::System.Span<byte> bits = Bits;
        if (value)
        {
            bits.Fill(255);
            MaskUnusedBits();
        }
        else
        {
            bits.Clear();
        }
    }

    internal void LoadPackedBytes(global::System.ReadOnlySpan<byte> source)
    {
        if (source.Length < Bits.Length)
        {
            throw new QrInputException("The source is too short for the matrix.", "source");
        }

        global::System.Span<byte> destination = Bits;
        source.Slice(0, Bits.Length).CopyTo(destination);
        MaskUnusedBits();
    }

    internal void CopyPackedBytes(global::System.Span<byte> destination)
    {
        if (destination.Length < Bits.Length)
        {
            throw new QrInputException("The destination is too short for the matrix.", "destination");
        }

        global::System.ReadOnlySpan<byte> source = Bits;
        source.CopyTo(destination);
    }

    internal int GetModuleIndex(int row, int column)
    {
        if (row < 0 || row >= Height)
        {
            throw new QrInputException("The row is outside the matrix.", "row");
        }

        if (column < 0 || column >= Width)
        {
            throw new QrInputException("The column is outside the matrix.", "column");
        }

        int rowOffset = CheckedDimensions.CalculateProduct(
            row,
            Width,
            ModuleCount - 1,
            "MatrixIndex");
        return CheckedDimensions.CalculateSum(
            rowOffset,
            column,
            ModuleCount - 1,
            "MatrixIndex");
    }

    internal void MaskUnusedBits()
    {
        int usedBitsInFinalByte = ModuleCount % 8;
        if (usedBitsInFinalByte == 0)
        {
            return;
        }

        int unusedBitCount = 8 - usedBitsInFinalByte;
        int mask = 255 << unusedBitCount;
        Bits[Bits.Length - 1] = (byte)(Bits[Bits.Length - 1] & mask);
    }
}
