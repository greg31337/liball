namespace qrcode;

internal class LegacyModel1Placement
{
    // Each eight-entry group maps MSB-first codeword bits to row-major cells.
    // The first four entries are -1: the first symbol character has only four bits.
    internal static int[] CreateOffsets(int version)
    {
        int dimension = LegacyModel1Table.GetDimension(version);
        int[] offsets = new int[LegacyModel1Table.GetTotalCodewordCount(version) * 8];
        int columns = dimension / 4 + 3;
        int word = 0;
        for (int group = 0; group < columns; group++)
        {
            bool vertical = LegacyModel1Table.IsVerticalSymbolCharacterColumn(group, columns);
            int rows = group < 2 ? (dimension - 8) / 4
                : vertical ? (dimension - 16) / 4 : dimension / 2;
            for (int rowGroup = 0; rowGroup < rows; rowGroup++)
            {
                int x;
                int y;
                if (group < 2)
                {
                    if (group == 0 && rowGroup % 2 == 0 && rowGroup > 0 && rowGroup < rows - 1)
                    {
                        continue;
                    }

                    x = dimension - 1 - group * 2;
                    y = dimension - 1 - rowGroup * 4;
                }
                else if (vertical)
                {
                    x = (columns - group - 1) * 2 + 1;
                    if (columns - group == 4) { x++; }
                    y = dimension - 9 - rowGroup * 4;
                }
                else
                {
                    if (group == 2 && rowGroup >= rows - 4
                        || rowGroup == 0 && group % 2 == 1 && group + 1 != columns - 4)
                    {
                        continue;
                    }

                    x = dimension - 5 - (group - 2) * 4;
                    y = dimension - 1 - rowGroup * 2;
                    if (rowGroup >= rows - 3) { y--; }
                }

                for (int bit = 0; bit < 8; bit++)
                {
                    int row = y - LegacyModel1Table.GetSymbolCharacterBitRow(bit, vertical, false);
                    int column = x - LegacyModel1Table.GetSymbolCharacterBitColumn(bit, vertical, false);
                    offsets[word * 8 + bit] = word == 0 && bit < 4 ? -1 : row * dimension + column;
                }

                word++;
            }
        }

        return offsets;
    }
}
