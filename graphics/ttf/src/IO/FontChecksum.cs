namespace ttf;

internal static class FontChecksum
{
    internal const uint WholeFontChecksum = 0xB1B0AFBAU;

    internal static uint CalculateTable(ReadOnlySpan<byte> tableData)
    {
        return Calculate(tableData, -1, -1);
    }

    internal static uint CalculateHeadTable(ReadOnlySpan<byte> headData)
    {
        if (headData.Length < 12)
        {
            throw FontFormatException.Create("The head table is truncated before checksumAdjustment.");
        }

        return Calculate(headData, 8, 12);
    }

    internal static uint CalculateFont(ReadOnlySpan<byte> fontData)
    {
        return Calculate(fontData, -1, -1);
    }

    internal static uint CalculateFontWithZeroedHeadAdjustment(
        ReadOnlySpan<byte> fontData,
        int headOffset)
    {
        ValidateHeadAdjustmentRange(fontData.Length, headOffset);
        return Calculate(fontData, headOffset + 8, headOffset + 12);
    }

    internal static uint CalculateChecksumAdjustment(
        ReadOnlySpan<byte> fontData,
        int headOffset)
    {
        uint checksum = CalculateFontWithZeroedHeadAdjustment(fontData, headOffset);
        ulong difference = (ulong)WholeFontChecksum + 0x100000000UL - checksum;
        return (uint)(difference & 0xFFFFFFFFUL);
    }

    internal static bool VerifyTable(ReadOnlySpan<byte> tableData, uint expectedChecksum)
    {
        return CalculateTable(tableData) == expectedChecksum;
    }

    internal static bool VerifyHeadTable(ReadOnlySpan<byte> headData, uint expectedChecksum)
    {
        return CalculateHeadTable(headData) == expectedChecksum;
    }

    internal static bool VerifyFont(ReadOnlySpan<byte> fontData)
    {
        return CalculateFont(fontData) == WholeFontChecksum;
    }

    static uint Calculate(ReadOnlySpan<byte> data, int zeroStart, int zeroEnd)
    {
        ulong sum = 0;
        int index = 0;

        while (index < data.Length)
        {
            int remaining = data.Length - index;
            int byteCount = remaining < 4 ? remaining : 4;
            uint word = 0;
            int wordIndex = 0;

            while (wordIndex < byteCount)
            {
                int dataIndex = index + wordIndex;
                byte value = 0;

                if (dataIndex < zeroStart || dataIndex >= zeroEnd)
                {
                    value = data[dataIndex];
                }

                int shift = (3 - wordIndex) * 8;
                word |= (uint)value << shift;
                wordIndex += 1;
            }

            sum = (sum + word) & 0xFFFFFFFFUL;
            index += byteCount;
        }

        return (uint)sum;
    }

    static void ValidateHeadAdjustmentRange(int fontLength, int headOffset)
    {
        if (headOffset < 0 || headOffset > fontLength - 12)
        {
            throw FontFormatException.Create("The head checksumAdjustment field is outside the bounded font data.");
        }
    }
}
