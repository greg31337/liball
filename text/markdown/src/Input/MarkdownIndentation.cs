using System;

namespace markdown;

internal class MarkdownIndentation
{
    internal const int TabStopColumns = 4;

    internal static int GetColumnsToNextTabStop(int column)
    {
        if (column < 0)
        {
            throw new ArgumentOutOfRangeException("column");
        }

        return TabStopColumns - column % TabStopColumns;
    }

    internal static int AdvanceColumn(int column, char character)
    {
        if (column < 0)
        {
            throw new ArgumentOutOfRangeException("column");
        }

        if (character == '\t')
        {
            return column + GetColumnsToNextTabStop(column);
        }

        return column + 1;
    }

    internal static MarkdownIndentationScan ScanLeading(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int index = 0;
        int column = 0;

        while (index < text.Length)
        {
            char character = text[index];
            if (MarkdownChar.IsSpace(character))
            {
                column = column + 1;
                index = index + 1;
            }
            else if (MarkdownChar.IsTab(character))
            {
                column = AdvanceColumn(column, character);
                index = index + 1;
            }
            else
            {
                return MarkdownIndentationScan.Create(index, column, false);
            }
        }

        return MarkdownIndentationScan.Create(index, column, true);
    }

    internal static int CountLeadingColumns(string text)
    {
        MarkdownIndentationScan scan = ScanLeading(text);
        return scan.Column;
    }

    internal static int FindFirstNonSpaceOrTab(string text)
    {
        MarkdownIndentationScan scan = ScanLeading(text);
        return scan.CharacterIndex;
    }
}
