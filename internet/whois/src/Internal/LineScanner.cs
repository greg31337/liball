namespace whois;

internal static class LineScanner
{
    internal static string[] ScanLines(string text)
    {
        InputGuard.RequireReference(text, "text");
        System.ReadOnlySpan<char> characters = System.MemoryExtensions.AsSpan(text);
        System.Collections.Generic.List<string> lines = new System.Collections.Generic.List<string>();
        int lineStart = 0;
        int index = 0;
        while (index < characters.Length)
        {
            char current = characters[index];
            if (current == '\r' || current == '\n')
            {
                lines.Add(characters.Slice(lineStart, index - lineStart).ToString());
                if (current == '\r' && index + 1 < characters.Length && characters[index + 1] == '\n')
                {
                    index = index + 1;
                }
                index = index + 1;
                lineStart = index;
            }
            else
            {
                index = index + 1;
            }
        }
        if (lineStart < characters.Length)
        {
            lines.Add(characters.Slice(lineStart, characters.Length - lineStart).ToString());
        }
        return lines.ToArray();
    }

    internal static int CountLines(string text)
    {
        InputGuard.RequireReference(text, "text");
        System.ReadOnlySpan<char> characters = System.MemoryExtensions.AsSpan(text);
        int count = 0;
        int lineStart = 0;
        int index = 0;
        while (index < characters.Length)
        {
            char current = characters[index];
            if (current == '\r' || current == '\n')
            {
                count = count + 1;
                if (current == '\r' && index + 1 < characters.Length && characters[index + 1] == '\n')
                {
                    index = index + 1;
                }
                index = index + 1;
                lineStart = index;
            }
            else
            {
                index = index + 1;
            }
        }
        if (lineStart < characters.Length)
        {
            count = count + 1;
        }
        return count;
    }
}
