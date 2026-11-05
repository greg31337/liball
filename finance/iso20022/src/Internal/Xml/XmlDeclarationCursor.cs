using System;

namespace iso20022;

internal class XmlDeclarationCursor
{
    string value;
    int index;

    internal XmlDeclarationCursor(string value)
    {
        this.value = value;
    }

    internal bool SkipWhitespace()
    {
        int start = index;
        while (index < value.Length && XmlInputRepairProcessor.IsXmlWhitespace(value[index]))
        {
            index++;
        }

        return index > start;
    }

    internal bool StartsWithName(string name)
    {
        if (index + name.Length > value.Length)
        {
            return false;
        }

        return string.CompareOrdinal(value, index, name, 0, name.Length) == 0;
    }

    internal string? ReadAttributeValue(string name)
    {
        if (!StartsWithName(name))
        {
            return null;
        }

        index += name.Length;
        SkipWhitespace();
        if (index >= value.Length || value[index] != '=')
        {
            return null;
        }

        index++;
        SkipWhitespace();
        if (index >= value.Length || (value[index] != '\'' && value[index] != '"'))
        {
            return null;
        }

        char quote = value[index];
        index++;
        int start = index;
        while (index < value.Length && value[index] != quote)
        {
            index++;
        }

        if (index >= value.Length)
        {
            return null;
        }

        string attributeValue = value.Substring(start, index - start);
        index++;
        return attributeValue;
    }

    internal bool IsAtEnd()
    {
        return index == value.Length;
    }
}
