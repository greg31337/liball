namespace uri;

internal static class WebLinkParameterValueCore
{
    internal static int GetCharacterOffset(
        WebLinkParameter parameter,
        int valueIndex)
    {
        int equalsOffset = parameter.OriginalText.IndexOf('=');
        if (equalsOffset < 0)
        {
            return parameter.Range.Offset + parameter.Range.Length;
        }

        int offset = equalsOffset + 1;
        while (offset < parameter.OriginalText.Length &&
            IsOptionalWhitespace(parameter.OriginalText[offset]))
        {
            offset = offset + 1;
        }

        if (!parameter.WasQuoted ||
            offset >= parameter.OriginalText.Length ||
            parameter.OriginalText[offset] != '"')
        {
            return parameter.Range.Offset + offset + valueIndex;
        }

        offset = offset + 1;
        int decodedIndex = 0;
        while (offset < parameter.OriginalText.Length &&
            decodedIndex < valueIndex)
        {
            if (parameter.OriginalText[offset] == '\\' &&
                offset + 1 < parameter.OriginalText.Length)
            {
                offset = offset + 2;
            }
            else
            {
                offset = offset + 1;
            }

            decodedIndex = decodedIndex + 1;
        }

        if (offset < parameter.OriginalText.Length &&
            parameter.OriginalText[offset] == '\\' &&
            offset + 1 < parameter.OriginalText.Length)
        {
            offset = offset + 1;
        }

        return parameter.Range.Offset + offset;
    }

    internal static bool IsOptionalWhitespace(char value)
    {
        return value == ' ' || value == '\t';
    }
}
