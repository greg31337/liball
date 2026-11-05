namespace qrcode;

internal class Model2MaskTable
{
    internal static bool IsInverted(int mask, int row, int column)
    {
        if (mask < 0 || mask > 7)
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

        bool result = false;
        if (mask == 0)
        {
            result = (row + column) % 2 == 0;
        }
        else if (mask == 1)
        {
            result = row % 2 == 0;
        }
        else if (mask == 2)
        {
            result = column % 3 == 0;
        }
        else if (mask == 3)
        {
            result = (row + column) % 3 == 0;
        }
        else if (mask == 4)
        {
            result = (row / 2 + column / 3) % 2 == 0;
        }
        else if (mask == 5)
        {
            result = row * column % 2 + row * column % 3 == 0;
        }
        else if (mask == 6)
        {
            result = (row * column % 2 + row * column % 3) % 2 == 0;
        }
        else
        {
            result = ((row + column) % 2 + row * column % 3) % 2 == 0;
        }

        return result;
    }
}
