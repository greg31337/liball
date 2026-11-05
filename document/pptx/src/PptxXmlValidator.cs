using System.Collections.Generic;
using System.Text;

namespace pptx;

internal static class PptxXmlValidator
{
    internal static void ValidatePart(PptxPart part, PptxResult result)
    {
        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        if (!part.IsXmlContentType())
        {
            return;
        }

        ValidateString(Encoding.UTF8.GetString(part.GetBytes()), part.PartName, result);
    }

    internal static void ValidateString(string xml, string partName, PptxResult result)
    {
        if (xml == null)
        {
            result.AddError("PKGV040", "XML part is empty.", partName, "", "");
            return;
        }

        if (xml.Length == 0)
        {
            result.AddError("PKGV040", "XML part is empty.", partName, "", "");
            return;
        }

        List<string> elementStack = new List<string>();
        int rootCount = 0;
        int index = 0;
        int textStart = 0;

        while (index < xml.Length)
        {
            if (xml[index] != '<')
            {
                if (elementStack.Count == 0 && !IsXmlWhitespace(xml[index]))
                {
                    result.AddError("PKGV047", "XML part contains content outside the document element.", partName, "", "");
                    return;
                }

                index = index + 1;
                continue;
            }

            if (!ValidateEntityReferences(xml, textStart, index))
            {
                result.AddError("PKGV046", "XML part contains an invalid entity reference.", partName, "", "");
                return;
            }

            if (StartsWith(xml, index, "<!--"))
            {
                int commentEnd = xml.IndexOf("-->", index + 4, System.StringComparison.Ordinal);
                if (commentEnd < 0)
                {
                    result.AddError("PKGV041", "XML part contains malformed markup.", partName, "", "");
                    return;
                }

                index = commentEnd + 3;
                textStart = index;
                continue;
            }

            if (StartsWith(xml, index, "<![CDATA["))
            {
                int cdataEnd = xml.IndexOf("]]>", index + 9, System.StringComparison.Ordinal);
                if (cdataEnd < 0)
                {
                    result.AddError("PKGV041", "XML part contains malformed markup.", partName, "", "");
                    return;
                }

                index = cdataEnd + 3;
                textStart = index;
                continue;
            }

            if (StartsWith(xml, index, "<?"))
            {
                int processingEnd = xml.IndexOf("?>", index + 2, System.StringComparison.Ordinal);
                if (processingEnd < 0)
                {
                    result.AddError("PKGV041", "XML part contains malformed markup.", partName, "", "");
                    return;
                }

                index = processingEnd + 2;
                textStart = index;
                continue;
            }

            if (StartsWith(xml, index, "<!DOCTYPE"))
            {
                result.AddError("PKGV048", "XML part contains a DTD.", partName, "", "");
                return;
            }

            if (StartsWith(xml, index, "</"))
            {
                int closeEnd = FindTagEnd(xml, index + 2);
                if (closeEnd < 0)
                {
                    result.AddError("PKGV041", "XML part contains malformed markup.", partName, "", "");
                    return;
                }

                string closeName = ReadTagName(xml, index + 2, closeEnd);
                if (closeName.Length == 0)
                {
                    result.AddError("PKGV041", "XML part contains malformed markup.", partName, "", "");
                    return;
                }

                if (!ValidateCloseTagSyntax(xml, index + 2, closeEnd))
                {
                    result.AddError("PKGV041", "XML part contains malformed markup.", partName, "", "");
                    return;
                }

                if (elementStack.Count == 0 || elementStack[elementStack.Count - 1] != closeName)
                {
                    result.AddError("PKGV042", "XML part contains a mismatched closing tag.", partName, "", "");
                    return;
                }

                elementStack.RemoveAt(elementStack.Count - 1);
                index = closeEnd + 1;
                textStart = index;
                continue;
            }

            int openEnd = FindTagEnd(xml, index + 1);
            if (openEnd < 0)
            {
                result.AddError("PKGV041", "XML part contains malformed markup.", partName, "", "");
                return;
            }

            string openName = ReadTagName(xml, index + 1, openEnd);
            if (openName.Length == 0)
            {
                result.AddError("PKGV041", "XML part contains malformed markup.", partName, "", "");
                return;
            }

            if (!ValidateStartTagSyntax(xml, index + 1, openEnd))
            {
                result.AddError("PKGV041", "XML part contains malformed markup.", partName, "", "");
                return;
            }

            if (elementStack.Count == 0)
            {
                rootCount = rootCount + 1;
                if (rootCount > 1)
                {
                    result.AddError("PKGV044", "XML part contains multiple document elements.", partName, "", "");
                    return;
                }
            }

            if (!IsSelfClosingTag(xml, openEnd))
            {
                elementStack.Add(openName);
            }

            index = openEnd + 1;
            textStart = index;
        }

        if (!ValidateEntityReferences(xml, textStart, xml.Length))
        {
            result.AddError("PKGV046", "XML part contains an invalid entity reference.", partName, "", "");
            return;
        }

        if (rootCount == 0)
        {
            result.AddError("PKGV040", "XML part is empty.", partName, "", "");
            return;
        }

        if (elementStack.Count > 0)
        {
            result.AddError("PKGV043", "XML part contains an unclosed element.", partName, "", "");
        }
    }

    internal static int FindTagEnd(string xml, int startIndex)
    {
        char quote = '\0';

        for (int index = startIndex; index < xml.Length; index++)
        {
            char value = xml[index];

            if (quote != '\0')
            {
                if (value == quote)
                {
                    quote = '\0';
                }

                continue;
            }

            if (value == '"' || value == '\'')
            {
                quote = value;
                continue;
            }

            if (value == '<')
            {
                return -1;
            }

            if (value == '>')
            {
                return index;
            }
        }

        return -1;
    }

    internal static string ReadTagName(string xml, int startIndex, int endIndex)
    {
        int index = SkipWhitespace(xml, startIndex, endIndex);
        if (index >= endIndex)
        {
            return "";
        }

        if (!IsXmlNameStart(xml[index]))
        {
            return "";
        }

        int nameEnd = index + 1;
        while (nameEnd < endIndex && IsXmlNameCharacter(xml[nameEnd]))
        {
            nameEnd = nameEnd + 1;
        }

        return xml.Substring(index, nameEnd - index);
    }

    internal static int ReadTagNameEnd(string xml, int startIndex, int endIndex)
    {
        int index = SkipWhitespace(xml, startIndex, endIndex);
        if (index >= endIndex)
        {
            return -1;
        }

        if (!IsXmlNameStart(xml[index]))
        {
            return -1;
        }

        int nameEnd = index + 1;
        while (nameEnd < endIndex && IsXmlNameCharacter(xml[nameEnd]))
        {
            nameEnd = nameEnd + 1;
        }

        return nameEnd;
    }

    internal static bool ValidateCloseTagSyntax(string xml, int startIndex, int endIndex)
    {
        int nameEnd = ReadTagNameEnd(xml, startIndex, endIndex);
        if (nameEnd < 0)
        {
            return false;
        }

        int index = SkipWhitespace(xml, nameEnd, endIndex);
        return index == endIndex;
    }

    internal static bool ValidateStartTagSyntax(string xml, int startIndex, int endIndex)
    {
        int nameEnd = ReadTagNameEnd(xml, startIndex, endIndex);
        if (nameEnd < 0)
        {
            return false;
        }

        int index = nameEnd;

        while (index < endIndex)
        {
            index = SkipWhitespace(xml, index, endIndex);
            if (index >= endIndex)
            {
                return true;
            }

            if (xml[index] == '/')
            {
                index = index + 1;
                index = SkipWhitespace(xml, index, endIndex);
                return index == endIndex;
            }

            if (!IsXmlNameStart(xml[index]))
            {
                return false;
            }

            index = index + 1;
            while (index < endIndex && IsXmlNameCharacter(xml[index]))
            {
                index = index + 1;
            }

            index = SkipWhitespace(xml, index, endIndex);
            if (index >= endIndex || xml[index] != '=')
            {
                return false;
            }

            index = index + 1;
            index = SkipWhitespace(xml, index, endIndex);
            if (index >= endIndex)
            {
                return false;
            }

            if (xml[index] != '"' && xml[index] != '\'')
            {
                return false;
            }

            char quote = xml[index];
            int valueStart = index + 1;
            index = valueStart;

            while (index < endIndex && xml[index] != quote)
            {
                index = index + 1;
            }

            if (index >= endIndex)
            {
                return false;
            }

            if (!ValidateEntityReferences(xml, valueStart, index))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsSelfClosingTag(string xml, int endIndex)
    {
        int index = endIndex - 1;

        while (index >= 0 && IsXmlWhitespace(xml[index]))
        {
            index = index - 1;
        }

        if (index >= 0 && xml[index] == '/')
        {
            return true;
        }

        return false;
    }

    internal static int SkipWhitespace(string text, int startIndex, int endIndex)
    {
        int index = startIndex;

        while (index < endIndex && IsXmlWhitespace(text[index]))
        {
            index = index + 1;
        }

        return index;
    }

    internal static bool IsXmlWhitespace(char value)
    {
        if (value == ' ')
        {
            return true;
        }

        if (value == '\t')
        {
            return true;
        }

        if (value == '\r')
        {
            return true;
        }

        if (value == '\n')
        {
            return true;
        }

        return false;
    }

    internal static bool IsXmlNameStart(char value)
    {
        if (PptxUriCore.IsAsciiLetter(value))
        {
            return true;
        }

        if (value == '_' || value == ':')
        {
            return true;
        }

        return false;
    }

    internal static bool IsXmlNameCharacter(char value)
    {
        if (IsXmlNameStart(value))
        {
            return true;
        }

        if (value >= '0' && value <= '9')
        {
            return true;
        }

        if (value == '-' || value == '.')
        {
            return true;
        }

        return false;
    }

    internal static bool ValidateEntityReferences(string xml, int startIndex, int endIndex)
    {
        for (int index = startIndex; index < endIndex; index++)
        {
            if (xml[index] != '&')
            {
                continue;
            }

            int semicolon = xml.IndexOf(';', index + 1);
            if (semicolon < 0 || semicolon >= endIndex)
            {
                return false;
            }

            string entityName = xml.Substring(index + 1, semicolon - index - 1);
            if (!IsValidEntityReferenceName(entityName))
            {
                return false;
            }

            index = semicolon;
        }

        return true;
    }

    internal static bool IsValidEntityReferenceName(string name)
    {
        if (name == "amp" || name == "lt" || name == "gt" || name == "apos" || name == "quot")
        {
            return true;
        }

        if (name.Length > 1 && name[0] == '#')
        {
            return IsValidCharacterReference(name);
        }

        return false;
    }

    internal static bool IsValidCharacterReference(string name)
    {
        if (name.Length > 2 && name[1] == 'x')
        {
            for (int index = 2; index < name.Length; index++)
            {
                if (!IsHexDigit(name[index]))
                {
                    return false;
                }
            }

            return true;
        }

        for (int index = 1; index < name.Length; index++)
        {
            if (name[index] < '0' || name[index] > '9')
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsHexDigit(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return true;
        }

        if (value >= 'a' && value <= 'f')
        {
            return true;
        }

        if (value >= 'A' && value <= 'F')
        {
            return true;
        }

        return false;
    }

    internal static bool StartsWith(string text, int index, string value)
    {
        if (index + value.Length > text.Length)
        {
            return false;
        }

        for (int offset = 0; offset < value.Length; offset++)
        {
            if (text[index + offset] != value[offset])
            {
                return false;
            }
        }

        return true;
    }
}
