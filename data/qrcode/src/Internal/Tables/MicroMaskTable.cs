namespace qrcode;

internal class MicroMaskTable
{
    internal static bool IsInverted(int mask, int row, int column)
    {
        if (mask < 0 || mask > 3)
        {
            throw new global::System.ArgumentOutOfRangeException("mask");
        }

        if (row < 0)
        {
            throw new global::System.ArgumentOutOfRangeException("row");
        }

        if (column < 0)
        {
            throw new global::System.ArgumentOutOfRangeException("column");
        }

        if (mask == 0)
        {
            return row % 2 == 0;
        }

        if (mask == 1)
        {
            return (row / 2 + column / 3) % 2 == 0;
        }

        if (mask == 2)
        {
            int product = row * column;
            return (product % 2 + product % 3) % 2 == 0;
        }

        return ((row + column) % 2 + row * column % 3) % 2 == 0;
    }
}
