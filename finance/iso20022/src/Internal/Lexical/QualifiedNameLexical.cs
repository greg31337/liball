using System;

namespace iso20022;

internal static class QualifiedNameLexical
{
    internal static bool IsQualifiedName(ReadOnlySpan<char> text)
    {
        return FindQualifiedNameSeparator(text) != -2;
    }

    internal static QualifiedNameData? ParseQualifiedName(ReadOnlySpan<char> text, ReadOnlySpan<char> namespaceUri)
    {
        int separator = FindQualifiedNameSeparator(text);
        if (separator == -2)
        {
            return null;
        }

        string prefix = string.Empty;
        ReadOnlySpan<char> localName = text;
        if (separator >= 0)
        {
            prefix = text.Slice(0, separator).ToString();
            localName = text.Slice(separator + 1, text.Length - separator - 1);
            if (namespaceUri.Length == 0)
            {
                return null;
            }
        }

        if (namespaceUri.Length > 0 && !UriLexical.IsNamespaceUri(namespaceUri))
        {
            return null;
        }

        string namespaceValue = namespaceUri.ToString();
        if (prefix == "xmlns" || namespaceValue == Iso20022Namespaces.Xmlns)
        {
            return null;
        }

        if (prefix == "xml")
        {
            if (namespaceValue != Iso20022Namespaces.Xml)
            {
                return null;
            }
        }
        else if (namespaceValue == Iso20022Namespaces.Xml)
        {
            return null;
        }

        QualifiedNameData result = new QualifiedNameData();
        result.Prefix = prefix;
        result.LocalName = localName.ToString();
        result.NamespaceUri = namespaceValue;
        return result;
    }

    internal static int FindQualifiedNameSeparator(ReadOnlySpan<char> text)
    {
        if (text.Length == 0)
        {
            return -2;
        }

        int separator = -1;
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == ':')
            {
                if (separator >= 0)
                {
                    return -2;
                }

                separator = index;
            }

            index++;
        }

        if (separator < 0)
        {
            return XmlNameLexical.IsNcName(text) ? -1 : -2;
        }

        if (separator == 0 || separator == text.Length - 1)
        {
            return -2;
        }

        ReadOnlySpan<char> prefix = text.Slice(0, separator);
        ReadOnlySpan<char> localName = text.Slice(separator + 1, text.Length - separator - 1);
        if (!XmlNameLexical.IsNcName(prefix) || !XmlNameLexical.IsNcName(localName))
        {
            return -2;
        }

        return separator;
    }
}
