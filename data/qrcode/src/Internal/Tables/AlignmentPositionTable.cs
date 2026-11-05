namespace qrcode;

internal class AlignmentPositionTable
{
    static readonly IntegerLookupTable Positions = new IntegerLookupTable(new int[][]
    {
        new int[] { },
        new int[] { 6, 18 },
        new int[] { 6, 22 },
        new int[] { 6, 26 },
        new int[] { 6, 30 },
        new int[] { 6, 34 },
        new int[] { 6, 22, 38 },
        new int[] { 6, 24, 42 },
        new int[] { 6, 26, 46 },
        new int[] { 6, 28, 50 },
        new int[] { 6, 30, 54 },
        new int[] { 6, 32, 58 },
        new int[] { 6, 34, 62 },
        new int[] { 6, 26, 46, 66 },
        new int[] { 6, 26, 48, 70 },
        new int[] { 6, 26, 50, 74 },
        new int[] { 6, 30, 54, 78 },
        new int[] { 6, 30, 56, 82 },
        new int[] { 6, 30, 58, 86 },
        new int[] { 6, 34, 62, 90 },
        new int[] { 6, 28, 50, 72, 94 },
        new int[] { 6, 26, 50, 74, 98 },
        new int[] { 6, 30, 54, 78, 102 },
        new int[] { 6, 28, 54, 80, 106 },
        new int[] { 6, 32, 58, 84, 110 },
        new int[] { 6, 30, 58, 86, 114 },
        new int[] { 6, 34, 62, 90, 118 },
        new int[] { 6, 26, 50, 74, 98, 122 },
        new int[] { 6, 30, 54, 78, 102, 126 },
        new int[] { 6, 26, 52, 78, 104, 130 },
        new int[] { 6, 30, 56, 82, 108, 134 },
        new int[] { 6, 34, 60, 86, 112, 138 },
        new int[] { 6, 30, 58, 86, 114, 142 },
        new int[] { 6, 34, 62, 90, 118, 146 },
        new int[] { 6, 30, 54, 78, 102, 126, 150 },
        new int[] { 6, 24, 50, 76, 102, 128, 154 },
        new int[] { 6, 28, 54, 80, 106, 132, 158 },
        new int[] { 6, 32, 58, 84, 110, 136, 162 },
        new int[] { 6, 26, 54, 82, 110, 138, 166 },
        new int[] { 6, 30, 58, 86, 114, 142, 170 }
    });

    internal static int[] GetPositions(int version)
    {
        Model2VersionTable.ValidateVersion(version);
        return Positions.CreateRowCopy(version - 1);
    }
}
