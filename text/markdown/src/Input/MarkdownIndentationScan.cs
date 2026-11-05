using System;

namespace markdown;

internal class MarkdownIndentationScan
{
    internal int CharacterIndex;
    internal int Column;
    internal bool IsBlank;

    internal MarkdownIndentationScan()
    {
        CharacterIndex = 0;
        Column = 0;
        IsBlank = true;
    }

    internal static MarkdownIndentationScan Create(int characterIndex, int column, bool isBlank)
    {
        if (characterIndex < 0)
        {
            throw new ArgumentOutOfRangeException("characterIndex");
        }

        if (column < 0)
        {
            throw new ArgumentOutOfRangeException("column");
        }

        MarkdownIndentationScan scan = new MarkdownIndentationScan();
        scan.CharacterIndex = characterIndex;
        scan.Column = column;
        scan.IsBlank = isBlank;
        return scan;
    }
}
