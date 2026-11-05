namespace qrcode;

internal class RectangularMicroAlignmentTable
{
    static readonly IntegerLookupTable CenterColumns = new IntegerLookupTable(new int[][]
    {
        new int[] { },
        new int[] { 21 },
        new int[] { 19, 39 },
        new int[] { 25, 51 },
        new int[] { 23, 49, 75 },
        new int[] { 27, 55, 83, 111 }
    });

    internal static int[] GetCenterColumns(int width)
    {
        int widthIndex = GetWidthIndex(width);
        return CenterColumns.CreateRowCopy(widthIndex);
    }

    static int GetWidthIndex(int width)
    {
        if (width == 27)
        {
            return 0;
        }

        if (width == 43)
        {
            return 1;
        }

        if (width == 59)
        {
            return 2;
        }

        if (width == 77)
        {
            return 3;
        }

        if (width == 99)
        {
            return 4;
        }

        if (width == 139)
        {
            return 5;
        }

        throw new global::System.ArgumentOutOfRangeException("width");
    }
}
