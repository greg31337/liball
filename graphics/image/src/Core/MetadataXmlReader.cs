using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace image;

internal class MetadataXmlReader
{
    internal static XmlReaderSettings CreateSafeSettings(long maxCharacters, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Ignore;
        settings.XmlResolver = null;
        settings.MaxCharactersFromEntities = 0;

        if (maxCharacters < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "xml.limit.negative", "XML character limit was negative.", blockType, 0, maxCharacters);
            settings.MaxCharactersInDocument = 1;
            return settings;
        }

        settings.MaxCharactersInDocument = maxCharacters;
        return settings;
    }

    internal static string ReadRootNameFromBytes(byte[] bytes, long maxBytes, long maxCharacters, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        string xmlText = DecodeXmlBytes(bytes, maxBytes, diagnostics, blockType);
        return ReadRootName(xmlText, maxCharacters, diagnostics, blockType);
    }

    internal static string ReadRootName(string? xmlText, long maxCharacters, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        MetadataXmlRoot root = ReadRoot(xmlText, maxCharacters, diagnostics, blockType);
        return root.LocalName;
    }

    internal static MetadataXmlRoot ReadRoot(string? xmlText, long maxCharacters, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        MetadataXmlRoot root = new MetadataXmlRoot();
        if (!ValidateXmlText(xmlText, maxCharacters, diagnostics, blockType))
        {
            return root;
        }

        if (xmlText == null)
        {
            return root;
        }

        if (!HasBalancedElements(xmlText))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "xml.malformed", "XML element markup was malformed or unbalanced.", blockType, 0, xmlText.Length);
            return root;
        }

        AddDtdDiagnosticIfPresent(xmlText, diagnostics, blockType);
        XmlReaderSettings settings = CreateSafeSettings(maxCharacters, diagnostics, blockType);

        using (StringReader stringReader = new StringReader(xmlText))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, settings))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        root.LocalName = reader.LocalName;
                        root.NamespaceUri = reader.NamespaceURI;
                        root.Prefix = reader.Prefix;
                        root.Found = true;
                        return root;
                    }
                }
            }
        }

        return root;
    }

    internal static List<string> ReadElementTextValues(string xmlText, string elementName, long maxCharacters, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        List<string> values = new List<string>();

        if (!ValidateXmlText(xmlText, maxCharacters, diagnostics, blockType))
        {
            return values;
        }

        if (elementName == null || elementName.Length == 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "xml.element.empty", "XML element name was empty.", blockType, 0, 0);
            return values;
        }

        AddDtdDiagnosticIfPresent(xmlText, diagnostics, blockType);
        XmlReaderSettings settings = CreateSafeSettings(maxCharacters, diagnostics, blockType);

        using (StringReader stringReader = new StringReader(xmlText))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, settings))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == elementName)
                    {
                        values.Add(reader.ReadElementContentAsString());
                    }
                }
            }
        }

        return values;
    }

    internal static string DecodeXmlBytes(byte[] bytes, long maxBytes, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        if (bytes == null)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "xml.bytes.null", "XML byte array was null.", blockType, 0, 0);
            return "";
        }

        if (maxBytes < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "xml.bytes.limit.negative", "XML byte limit was negative.", blockType, 0, maxBytes);
            return "";
        }

        if (maxBytes > 0 && bytes.Length > maxBytes)
        {
            MetadataDiagnostic.AddLimitExceeded(diagnostics, "xml.bytes.limit", "XML byte limit was exceeded.", blockType, 0, bytes.Length);
            return "";
        }

        if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
        {
            return MetadataStringReader.DecodeUtf8RangeWithLatin1Fallback(bytes, 3, bytes.Length - 3, diagnostics, blockType);
        }

        if (bytes.Length >= 2 && bytes[0] == 255 && bytes[1] == 254)
        {
            return MetadataStringReader.DecodeUtf16LittleEndianRange(bytes, 2, bytes.Length - 2, diagnostics, blockType);
        }

        if (bytes.Length >= 2 && bytes[0] == 254 && bytes[1] == 255)
        {
            return MetadataStringReader.DecodeUtf16BigEndianRange(bytes, 2, bytes.Length - 2, diagnostics, blockType);
        }

        return MetadataStringReader.DecodeUtf8WithLatin1Fallback(bytes, diagnostics, blockType);
    }

    internal static bool ValidateXmlText(string? xmlText, long maxCharacters, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        if (xmlText == null)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "xml.text.null", "XML text was null.", blockType, 0, 0);
            return false;
        }

        if (xmlText.Length == 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "xml.text.empty", "XML text was empty.", blockType, 0, 0);
            return false;
        }

        if (maxCharacters < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "xml.limit.negative", "XML character limit was negative.", blockType, 0, maxCharacters);
            return false;
        }

        if (maxCharacters > 0 && xmlText.Length > maxCharacters)
        {
            MetadataDiagnostic.AddLimitExceeded(diagnostics, "xml.character.limit", "XML character limit was exceeded.", blockType, 0, xmlText.Length);
            return false;
        }

        return true;
    }

    internal static void AddDtdDiagnosticIfPresent(string xmlText, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        if (ContainsDtd(xmlText))
        {
            MetadataDiagnostic.AddUnsupported(diagnostics, "xml.dtd.ignored", "XML DTD was ignored.", blockType);
        }
    }

    internal static bool ContainsDtd(string? xmlText)
    {
        if (xmlText == null)
        {
            return false;
        }

        return xmlText.IndexOf("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    internal static bool HasBalancedElements(string xmlText)
    {
        if (xmlText == null || xmlText.Length == 0)
        {
            return false;
        }

        List<string> stack = new List<string>();
        int rootCount = 0;
        int offset = 0;
        while (offset < xmlText.Length)
        {
            int start = xmlText.IndexOf('<', offset);
            if (start < 0)
            {
                break;
            }

            if (StartsWithAt(xmlText, start, "<!--"))
            {
                int endComment = xmlText.IndexOf("-->", start + 4, StringComparison.Ordinal);
                if (endComment < 0)
                {
                    return false;
                }

                offset = endComment + 3;
                continue;
            }

            if (StartsWithAt(xmlText, start, "<![CDATA["))
            {
                if (stack.Count == 0)
                {
                    return false;
                }

                int endCdata = xmlText.IndexOf("]]>", start + 9, StringComparison.Ordinal);
                if (endCdata < 0)
                {
                    return false;
                }

                offset = endCdata + 3;
                continue;
            }

            if (StartsWithAt(xmlText, start, "<?"))
            {
                int endInstruction = xmlText.IndexOf("?>", start + 2, StringComparison.Ordinal);
                if (endInstruction < 0)
                {
                    return false;
                }

                offset = endInstruction + 2;
                continue;
            }

            if (StartsWithIgnoreCaseAt(xmlText, start, "<!DOCTYPE"))
            {
                int endDoctype = FindDoctypeEnd(xmlText, start + 9);
                if (endDoctype < 0)
                {
                    return false;
                }

                offset = endDoctype + 1;
                continue;
            }

            int end = FindTagEnd(xmlText, start + 1);
            if (end < 0)
            {
                return false;
            }

            if (start + 1 < xmlText.Length && xmlText[start + 1] == '!')
            {
                offset = end + 1;
                continue;
            }

            bool closing = start + 1 < xmlText.Length && xmlText[start + 1] == '/';
            int nameOffset = closing ? start + 2 : start + 1;
            string name = ReadMarkupName(xmlText, nameOffset, end);
            if (name.Length == 0)
            {
                return false;
            }

            if (closing)
            {
                if (stack.Count == 0 || stack[stack.Count - 1] != name)
                {
                    return false;
                }

                stack.RemoveAt(stack.Count - 1);
            }
            else if (!IsSelfClosing(xmlText, start, end))
            {
                if (stack.Count == 0)
                {
                    rootCount = rootCount + 1;
                }

                stack.Add(name);
            }
            else if (stack.Count == 0)
            {
                rootCount = rootCount + 1;
            }

            if (rootCount > 1)
            {
                return false;
            }

            offset = end + 1;
        }

        return rootCount == 1 && stack.Count == 0;
    }

    internal static int FindTagEnd(string xmlText, int offset)
    {
        int quote = 0;
        while (offset < xmlText.Length)
        {
            int value = xmlText[offset];
            if (quote == 0 && (value == 34 || value == 39))
            {
                quote = value;
            }
            else if (quote == value)
            {
                quote = 0;
            }
            else if (quote == 0 && value == 62)
            {
                return offset;
            }
            else if (quote == 0 && value == 60)
            {
                return -1;
            }

            offset = offset + 1;
        }

        return -1;
    }

    internal static int FindDoctypeEnd(string xmlText, int offset)
    {
        int quote = 0;
        int bracketDepth = 0;
        while (offset < xmlText.Length)
        {
            int value = xmlText[offset];
            if (quote == 0 && (value == 34 || value == 39))
            {
                quote = value;
            }
            else if (quote == value)
            {
                quote = 0;
            }
            else if (quote == 0 && value == 91)
            {
                bracketDepth = bracketDepth + 1;
            }
            else if (quote == 0 && value == 93 && bracketDepth > 0)
            {
                bracketDepth = bracketDepth - 1;
            }
            else if (quote == 0 && bracketDepth == 0 && value == 62)
            {
                return offset;
            }

            offset = offset + 1;
        }

        return -1;
    }

    internal static string ReadMarkupName(string xmlText, int offset, int end)
    {
        while (offset < end && char.IsWhiteSpace(xmlText[offset]))
        {
            offset = offset + 1;
        }

        int start = offset;
        while (offset < end && !char.IsWhiteSpace(xmlText[offset]) && xmlText[offset] != '/' && xmlText[offset] != '>')
        {
            offset = offset + 1;
        }

        return xmlText.Substring(start, offset - start);
    }

    internal static bool IsSelfClosing(string xmlText, int start, int end)
    {
        int offset = end - 1;
        while (offset > start && char.IsWhiteSpace(xmlText[offset]))
        {
            offset = offset - 1;
        }

        return xmlText[offset] == '/';
    }

    internal static bool StartsWithAt(string text, int offset, string value)
    {
        if (offset < 0 || value == null || text.Length - offset < value.Length)
        {
            return false;
        }

        return string.CompareOrdinal(text, offset, value, 0, value.Length) == 0;
    }

    internal static bool StartsWithIgnoreCaseAt(string text, int offset, string value)
    {
        if (offset < 0 || value == null || text.Length - offset < value.Length)
        {
            return false;
        }

        return string.Compare(text, offset, value, 0, value.Length, StringComparison.OrdinalIgnoreCase) == 0;
    }
}
