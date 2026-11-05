namespace qrcode;

internal class RectangularMicroMaskTable
{
    internal static bool IsInverted(int row, int column)
    {
        if (row < 0)
        {
            throw new global::System.ArgumentOutOfRangeException("row");
        }

        if (column < 0)
        {
            throw new global::System.ArgumentOutOfRangeException("column");
        }

        return (row / 2 + column / 3) % 2 == 0;
    }
}
