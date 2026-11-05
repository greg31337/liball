using System;

namespace raw;

internal static class XmpCodec
{
    internal const string RdfNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    internal const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
    internal const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

    internal static RawXmpPacket Parse(byte[] rawBytes, RawLimits limits, long offset, string? ifdPath)
    {
        ValidateInput(rawBytes, limits);
        RawXmpEncoding encoding = DetectEncoding(rawBytes);
        int preambleLength = GetPreambleLength(rawBytes, encoding);
        if (!IsValidEncodedText(rawBytes, preambleLength, encoding))
        {
            throw Invalid("The XMP packet contains invalid encoded text.", offset);
        }
        System.Text.Encoding textEncoding = GetEncoding(encoding);
        string decoded = textEncoding.GetString(rawBytes, preambleLength, rawBytes.Length - preambleLength);
        int rootStart = FindRootStart(decoded);
        int rootEnd = FindRootEnd(decoded, rootStart);
        string prefixText = decoded.Substring(0, rootStart);
        if (prefixText.IndexOf("<!DOCTYPE", System.StringComparison.OrdinalIgnoreCase) >= 0 || prefixText.IndexOf("<!ENTITY", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw Invalid("XMP document type and entity declarations are prohibited.", offset);
        }
        string xmlText = decoded.Substring(rootStart, rootEnd - rootStart);
        string tailText = decoded.Substring(rootEnd);
        int paddingCharacters = CountPadding(tailText);
        string paddingText = tailText.Substring(0, paddingCharacters);
        string suffixText = tailText.Substring(paddingCharacters);

        int prefixLength = preambleLength + textEncoding.GetByteCount(prefixText);
        int xmlLength = textEncoding.GetByteCount(xmlText);
        int paddingLength = textEncoding.GetByteCount(paddingText);
        int suffixLength = rawBytes.Length - prefixLength - xmlLength - paddingLength;
        if (prefixLength < 0 || xmlLength <= 0 || paddingLength < 0 || suffixLength < 0)
        {
            throw Invalid("The XMP packet byte boundaries are inconsistent with its encoding.", offset);
        }

        byte[] prefixBytes = rawBytes.AsSpan(0, prefixLength).ToArray();
        byte[] xmlBytes = rawBytes.AsSpan(prefixLength, xmlLength).ToArray();
        byte[] paddingBytes = rawBytes.AsSpan(prefixLength + xmlLength, paddingLength).ToArray();
        byte[] suffixBytes = rawBytes.AsSpan(prefixLength + xmlLength + paddingLength, suffixLength).ToArray();
        RawXmpField[] fields = ParseFields(xmlText, limits, offset);
        bool readOnly = IsReadOnlySuffix(suffixText);
        bool hasBom = preambleLength > 0;
        return new RawXmpPacket(RawTextValue.Copy(rawBytes), encoding, hasBom, xmlText, prefixBytes, xmlBytes, paddingBytes, suffixBytes, readOnly, offset, ifdPath == null ? string.Empty : ifdPath, fields);
    }

    internal static RawXmpPacket[] Discover(TiffIfdGraph graph, RawLimits? limits)
    {
        if (graph == null)
        {
            return System.Array.Empty<RawXmpPacket>();
        }
        System.Collections.Generic.List<RawXmpPacket> packets = new System.Collections.Generic.List<RawXmpPacket>();
        long total = 0;
        int ifdIndex = 0;
        while (ifdIndex < graph.Ifds.Length)
        {
            TiffIfd ifd = graph.Ifds[ifdIndex];
            int entryIndex = 0;
            while (entryIndex < ifd.Entries.Length)
            {
                TiffEntry entry = ifd.Entries[entryIndex];
                if (entry.Tag == 700 && (entry.FieldType == TiffFieldType.Byte || entry.FieldType == TiffFieldType.Undefined))
                {
                    total = CheckedMath.Add(total, entry.RawBytes.Length);
                    if (total > (limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxMetadataBytes)
                    {
                        throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "Combined XMP packets exceed the configured metadata limit.", entry.EntryOffset);
                    }
                    long offset = GetValueOffset(graph, entry);
                    packets.Add(Parse(entry.RawBytes, limits, offset, ifd.Path));
                }
                entryIndex++;
            }
            ifdIndex++;
        }
        return packets.ToArray();
    }

    internal static RawXmpPacket Replace(RawXmpPacket packet, string xml, RawLimits limits, bool fixedSize)
    {
        if (packet == null || packet.RawBytes == null || packet.PrefixBytes == null || packet.PaddingBytes == null || packet.SuffixBytes == null)
        {
            throw Invalid("A complete source XMP packet is required.", -1);
        }
        ValidateReplacementXml(xml, limits, packet.Offset);
        System.Text.Encoding encoding = GetEncoding(packet.Encoding);
        byte[] xmlBytes = encoding.GetBytes(xml);
        byte[] padding;
        if (fixedSize)
        {
            int available = packet.RawBytes.Length - packet.PrefixBytes.Length - packet.SuffixBytes.Length - xmlBytes.Length;
            if (available < 0)
            {
                throw Invalid("The replacement XMP XML does not fit in the fixed packet length.", packet.Offset);
            }
            padding = ResizePadding(packet.PaddingBytes, available, packet.Encoding, packet.Offset);
        }
        else
        {
            padding = RawTextValue.Copy(packet.PaddingBytes);
        }
        byte[] result = Combine(packet.PrefixBytes, xmlBytes, padding, packet.SuffixBytes, limits, packet.Offset);
        return Parse(result, limits, packet.Offset, packet.IfdPath);
    }

    internal static RawXmpField[] ParseFields(string xml, RawLimits limits, long offset)
    {
        CheckedMath.RequireLimits(limits);
        CheckedMath.RequirePositiveLimit(limits.MaxXmpBytes, "The configured XMP limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(limits.MaxTagCount, "The configured tag-count limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(limits.MaxRecursionDepth, "The configured recursion limit must be greater than zero.");
        System.Xml.XmlReaderSettings settings = new System.Xml.XmlReaderSettings();
        settings.DtdProcessing = System.Xml.DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        settings.MaxCharactersFromEntities = limits.MaxXmpBytes;
        settings.MaxCharactersInDocument = limits.MaxXmpBytes;
        settings.CheckCharacters = true;
        settings.ConformanceLevel = System.Xml.ConformanceLevel.Document;
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = false;
        settings.IgnoreWhitespace = false;

        int capacity = CheckedMath.ToInt32(CheckedMath.Add(limits.MaxRecursionDepth, 2));
        CheckedMath.AllocationSize(capacity, System.Environment.Is64BitProcess ? 8 : 4, limits);
        string[] namespaces = new string[capacity];
        string[] prefixes = new string[capacity];
        string[] names = new string[capacity];
        string[] paths = new string[capacity];
        string[] languages = new string[capacity];
        System.Collections.Generic.List<RawXmpField> fields = new System.Collections.Generic.List<RawXmpField>();
        bool foundRdf = false;
        using (System.IO.StringReader input = new System.IO.StringReader(xml))
        using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(input, settings))
        {
            while (reader.Read())
            {
                int depth = reader.Depth;
                if (depth > limits.MaxRecursionDepth || depth >= capacity)
                {
                    throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The XMP XML depth exceeds the configured recursion limit.", offset);
                }
                if (reader.NodeType == System.Xml.XmlNodeType.DocumentType)
                {
                    throw Invalid("XMP document type declarations are prohibited.", offset);
                }
                if (reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    string namespaceUri = reader.NamespaceURI == null ? string.Empty : reader.NamespaceURI;
                    string prefix = reader.Prefix == null ? string.Empty : reader.Prefix;
                    string name = reader.LocalName == null ? string.Empty : reader.LocalName;
                    string parentPath = depth == 0 ? string.Empty : paths[depth - 1];
                    string qualified = prefix.Length == 0 ? name : prefix + ":" + name;
                    namespaces[depth] = namespaceUri;
                    prefixes[depth] = prefix;
                    names[depth] = name;
                    paths[depth] = parentPath.Length == 0 ? qualified : parentPath + "/" + qualified;
                    string? language = reader.GetAttribute("lang", XmlNamespace);
                    if (language == null && depth > 0)
                    {
                        language = languages[depth - 1];
                    }
                    languages[depth] = language == null ? string.Empty : language;
                    if (namespaceUri == RdfNamespace && name == "RDF")
                    {
                        foundRdf = true;
                    }
                    AddAttributes(reader, fields, namespaceUri, prefix, name, paths[depth], languages[depth]);
                }
                else if (reader.NodeType == System.Xml.XmlNodeType.Text || reader.NodeType == System.Xml.XmlNodeType.CDATA || reader.NodeType == System.Xml.XmlNodeType.SignificantWhitespace)
                {
                    if (!string.IsNullOrWhiteSpace(reader.Value))
                    {
                        AddTextField(fields, namespaces, prefixes, names, paths, languages, depth, reader.Value);
                    }
                }
                if (fields.Count > limits.MaxTagCount)
                {
                    throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The XMP field count exceeds the configured tag-count limit.", offset);
                }
            }
        }
        if (!foundRdf)
        {
            throw Invalid("An XMP packet must contain an RDF root element.", offset);
        }
        return fields.ToArray();
    }

    internal static void AddAttributes(System.Xml.XmlReader reader, System.Collections.Generic.List<RawXmpField> fields, string elementNamespace, string elementPrefix, string elementName, string path, string language)
    {
        if (!reader.HasAttributes)
        {
            return;
        }
        bool more = reader.MoveToFirstAttribute();
        while (more)
        {
            string namespaceUri = reader.NamespaceURI == null ? string.Empty : reader.NamespaceURI;
            string name = reader.LocalName == null ? string.Empty : reader.LocalName;
            string prefix = reader.Prefix == null ? string.Empty : reader.Prefix;
            if (namespaceUri == RdfNamespace && name == "resource" && elementNamespace != RdfNamespace)
            {
                fields.Add(new RawXmpField(elementNamespace, elementPrefix, elementName, reader.Value, language, true, path));
            }
            else if (namespaceUri != XmlnsNamespace && namespaceUri != XmlNamespace && namespaceUri != RdfNamespace && namespaceUri.Length != 0)
            {
                string qualified = prefix.Length == 0 ? name : prefix + ":" + name;
                fields.Add(new RawXmpField(namespaceUri, prefix, name, reader.Value, language, true, path + "/@" + qualified));
            }
            more = reader.MoveToNextAttribute();
        }
        reader.MoveToElement();
    }

    internal static void AddTextField(System.Collections.Generic.List<RawXmpField> fields, string[] namespaces, string[] prefixes, string[] names, string[] paths, string[] languages, int depth, string value)
    {
        string language = string.Empty;
        int languageIndex = depth;
        while (languageIndex >= 0)
        {
            if (languages[languageIndex] != null)
            {
                language = languages[languageIndex];
                break;
            }
            languageIndex--;
        }
        int target = depth;
        while (target >= 0)
        {
            string namespaceUri = namespaces[target];
            string name = names[target];
            if (namespaceUri != null && namespaceUri.Length != 0 && namespaceUri != RdfNamespace && name != "xmpmeta")
            {
                fields.Add(new RawXmpField(namespaceUri, prefixes[target], name, value, language, false, paths[depth - 1]));
                return;
            }
            target--;
        }
    }

    internal static void ValidateReplacementXml(string xml, RawLimits limits, long offset)
    {
        CheckedMath.RequireLimits(limits);
        if (xml == null || xml.Length == 0 || xml[0] != '<' || (xml.Length > 1 && (xml[1] == '?' || xml[1] == '!')) || xml.IndexOf("<?xpacket", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw Invalid("Replacement XMP must be one complete root XML element without a packet wrapper or declaration.", offset);
        }
        if (xml.Length > limits.MaxXmpBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "Replacement XMP exceeds the configured packet limit.", offset);
        }
        int rootStart = FindRootStart(xml);
        int rootEnd = FindRootEnd(xml, rootStart);
        if (rootStart != 0 || rootEnd != xml.Length)
        {
            throw Invalid("Replacement XMP must contain exactly one root XML element.", offset);
        }
        ParseFields(xml, limits, offset);
    }

    internal static void ValidateInput(byte[] rawBytes, RawLimits limits)
    {
        CheckedMath.RequireLimits(limits);
        CheckedMath.RequirePositiveLimit(limits.MaxXmpBytes, "The configured XMP limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(limits.MaxMetadataBytes, "The configured metadata limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(limits.MaxTagCount, "The configured tag-count limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(limits.MaxRecursionDepth, "The configured recursion limit must be greater than zero.");
        if (limits.MaxXmpBytes > limits.MaxMetadataBytes)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The XMP limit cannot exceed the combined metadata limit.", -1);
        }
        if (rawBytes == null || rawBytes.Length == 0)
        {
            throw Invalid("A non-empty XMP packet is required.", -1);
        }
        if (rawBytes.Length > limits.MaxXmpBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The XMP packet exceeds the configured size limit.", -1);
        }
        CheckedMath.AllocationSize(rawBytes.Length, 1, limits);
        CheckedMath.AllocationSize(rawBytes.Length, 2, limits);
    }

    internal static RawXmpEncoding DetectEncoding(byte[] bytes)
    {
        if (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 254 && bytes[3] == 255)
        {
            return RawXmpEncoding.Utf32BigEndian;
        }
        if (bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 254 && bytes[2] == 0 && bytes[3] == 0)
        {
            return RawXmpEncoding.Utf32LittleEndian;
        }
        if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
        {
            return RawXmpEncoding.Utf8;
        }
        if (bytes.Length >= 2 && bytes[0] == 254 && bytes[1] == 255)
        {
            return RawXmpEncoding.Utf16BigEndian;
        }
        if (bytes.Length >= 2 && bytes[0] == 255 && bytes[1] == 254)
        {
            return RawXmpEncoding.Utf16LittleEndian;
        }
        if (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 60)
        {
            return RawXmpEncoding.Utf32BigEndian;
        }
        if (bytes.Length >= 4 && bytes[0] == 60 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 0)
        {
            return RawXmpEncoding.Utf32LittleEndian;
        }
        if (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 60 && bytes[2] == 0)
        {
            return RawXmpEncoding.Utf16BigEndian;
        }
        if (bytes.Length >= 4 && bytes[0] == 60 && bytes[1] == 0 && bytes[3] == 0)
        {
            return RawXmpEncoding.Utf16LittleEndian;
        }
        return RawXmpEncoding.Utf8;
    }

    internal static System.Text.Encoding GetEncoding(RawXmpEncoding encoding)
    {
        switch (encoding)
        {
            case RawXmpEncoding.Utf8:
                return new System.Text.UTF8Encoding(false, true);
            case RawXmpEncoding.Utf16BigEndian:
                return new System.Text.UnicodeEncoding(true, false, true);
            case RawXmpEncoding.Utf16LittleEndian:
                return new System.Text.UnicodeEncoding(false, false, true);
            case RawXmpEncoding.Utf32BigEndian:
                return new System.Text.UTF32Encoding(true, false, true);
            case RawXmpEncoding.Utf32LittleEndian:
                return new System.Text.UTF32Encoding(false, false, true);
            default:
                throw Invalid("The XMP character encoding is not defined.", -1);
        }
    }

    internal static int GetPreambleLength(byte[] bytes, RawXmpEncoding encoding)
    {
        if (encoding == RawXmpEncoding.Utf8 && bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
        {
            return 3;
        }
        if ((encoding == RawXmpEncoding.Utf16BigEndian || encoding == RawXmpEncoding.Utf16LittleEndian) && bytes.Length >= 2 && ((bytes[0] == 254 && bytes[1] == 255) || (bytes[0] == 255 && bytes[1] == 254)))
        {
            return 2;
        }
        if ((encoding == RawXmpEncoding.Utf32BigEndian || encoding == RawXmpEncoding.Utf32LittleEndian) && bytes.Length >= 4 && ((bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 254 && bytes[3] == 255) || (bytes[0] == 255 && bytes[1] == 254 && bytes[2] == 0 && bytes[3] == 0)))
        {
            return 4;
        }
        return 0;
    }

    internal static bool IsValidEncodedText(byte[] bytes, int offset, RawXmpEncoding encoding)
    {
        if (encoding == RawXmpEncoding.Utf8)
        {
            return System.Text.Unicode.Utf8.IsValid(bytes.AsSpan(offset));
        }
        int length = bytes.Length - offset;
        if (encoding == RawXmpEncoding.Utf16BigEndian || encoding == RawXmpEncoding.Utf16LittleEndian)
        {
            if (length % 2 != 0)
            {
                return false;
            }
            bool bigEndian = encoding == RawXmpEncoding.Utf16BigEndian;
            int index = offset;
            while (index < bytes.Length)
            {
                int value = bigEndian ? bytes[index] << 8 | bytes[index + 1] : bytes[index] | bytes[index + 1] << 8;
                if (value >= 55296 && value <= 56319)
                {
                    if (index + 3 >= bytes.Length)
                    {
                        return false;
                    }
                    int following = bigEndian ? bytes[index + 2] << 8 | bytes[index + 3] : bytes[index + 2] | bytes[index + 3] << 8;
                    if (following < 56320 || following > 57343)
                    {
                        return false;
                    }
                    index += 4;
                }
                else
                {
                    if (value >= 56320 && value <= 57343)
                    {
                        return false;
                    }
                    index += 2;
                }
            }
            return true;
        }
        if (length % 4 != 0)
        {
            return false;
        }
        bool bigEndian32 = encoding == RawXmpEncoding.Utf32BigEndian;
        int position = offset;
        while (position < bytes.Length)
        {
            uint value;
            if (bigEndian32)
            {
                value = (uint)bytes[position] << 24 | (uint)bytes[position + 1] << 16 | (uint)bytes[position + 2] << 8 | bytes[position + 3];
            }
            else
            {
                value = bytes[position] | (uint)bytes[position + 1] << 8 | (uint)bytes[position + 2] << 16 | (uint)bytes[position + 3] << 24;
            }
            if (value > 1114111 || value >= 55296 && value <= 57343)
            {
                return false;
            }
            position += 4;
        }
        return true;
    }

    internal static int FindRootStart(string text)
    {
        int search = 0;
        while (search < text.Length)
        {
            int opening = text.IndexOf('<', search);
            if (opening < 0 || opening + 1 >= text.Length)
            {
                throw Invalid("The XMP packet does not contain a root XML element.", -1);
            }
            char next = text[opening + 1];
            if (next != '?' && next != '!')
            {
                return opening;
            }
            int closing = text.IndexOf('>', opening + 2);
            if (closing < 0)
            {
                throw Invalid("The XMP packet wrapper is truncated.", -1);
            }
            search = closing + 1;
        }
        throw Invalid("The XMP packet does not contain a root XML element.", -1);
    }

    internal static int FindRootEnd(string text, int rootStart)
    {
        int nameEnd = rootStart + 1;
        while (nameEnd < text.Length && !char.IsWhiteSpace(text[nameEnd]) && text[nameEnd] != '>' && text[nameEnd] != '/')
        {
            nameEnd++;
        }
        if (nameEnd <= rootStart + 1)
        {
            throw Invalid("The XMP root element name is missing.", -1);
        }
        string name = text.Substring(rootStart + 1, nameEnd - rootStart - 1);
        int openingEnd = text.IndexOf('>', nameEnd);
        if (openingEnd < 0)
        {
            throw Invalid("The XMP root element is truncated.", -1);
        }
        if (openingEnd > rootStart && text[openingEnd - 1] == '/')
        {
            return openingEnd + 1;
        }
        string closingToken = "</" + name;
        int closingStart = text.LastIndexOf(closingToken, System.StringComparison.Ordinal);
        if (closingStart < openingEnd)
        {
            throw Invalid("The XMP root element closing tag is missing.", -1);
        }
        int closingEnd = text.IndexOf('>', closingStart + closingToken.Length);
        if (closingEnd < 0)
        {
            throw Invalid("The XMP root element closing tag is truncated.", -1);
        }
        return closingEnd + 1;
    }

    internal static int CountPadding(string tail)
    {
        int index = 0;
        while (index < tail.Length && char.IsWhiteSpace(tail[index]))
        {
            index++;
        }
        return index;
    }

    internal static bool IsReadOnlySuffix(string suffix)
    {
        int packet = suffix.IndexOf("<?xpacket", System.StringComparison.OrdinalIgnoreCase);
        if (packet < 0)
        {
            return false;
        }
        int end = suffix.IndexOf("end", packet, System.StringComparison.OrdinalIgnoreCase);
        if (end < 0)
        {
            return false;
        }
        int index = end + 3;
        while (index < suffix.Length && char.IsWhiteSpace(suffix[index]))
        {
            index++;
        }
        if (index >= suffix.Length || suffix[index] != '=')
        {
            return false;
        }
        index++;
        while (index < suffix.Length && char.IsWhiteSpace(suffix[index]))
        {
            index++;
        }
        if (index >= suffix.Length || (suffix[index] != '\'' && suffix[index] != '\"'))
        {
            return false;
        }
        index++;
        return index < suffix.Length && (suffix[index] == 'r' || suffix[index] == 'R');
    }

    internal static byte[] ResizePadding(byte[] original, int length, RawXmpEncoding encoding, long offset)
    {
        System.Text.Encoding textEncoding = GetEncoding(encoding);
        byte[] space = textEncoding.GetBytes(" ");
        if (space.Length == 0 || length % space.Length != 0)
        {
            throw Invalid("The fixed XMP packet length is not aligned for its character encoding.", offset);
        }
        byte[] result = new byte[length];
        int preserved = original.Length < length ? original.Length : length;
        preserved -= preserved % space.Length;
        if (preserved > 0)
        {
            original.AsSpan(0, preserved).CopyTo(result);
        }
        int index = preserved;
        while (index < result.Length)
        {
            space.AsSpan().CopyTo(result.AsSpan(index));
            index += space.Length;
        }
        return result;
    }

    internal static byte[] Combine(byte[] prefix, byte[] xml, byte[] padding, byte[] suffix, RawLimits limits, long offset)
    {
        long length = CheckedMath.Add(prefix.Length, xml.Length);
        length = CheckedMath.Add(length, padding.Length);
        length = CheckedMath.Add(length, suffix.Length);
        if (length > limits.MaxXmpBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The replacement XMP packet exceeds the configured size limit.", offset);
        }
        CheckedMath.AllocationSize(length, 1, limits);
        byte[] result = new byte[CheckedMath.ToInt32(length)];
        int position = 0;
        prefix.AsSpan().CopyTo(result.AsSpan(position));
        position += prefix.Length;
        xml.AsSpan().CopyTo(result.AsSpan(position));
        position += xml.Length;
        padding.AsSpan().CopyTo(result.AsSpan(position));
        position += padding.Length;
        suffix.AsSpan().CopyTo(result.AsSpan(position));
        return result;
    }

    internal static long GetValueOffset(TiffIfdGraph graph, TiffEntry entry)
    {
        if (entry.Inline)
        {
            int slotOffset = graph.Header.BigTiff ? 12 : 8;
            return CheckedMath.Add(entry.EntryOffset, slotOffset);
        }
        return CheckedMath.Add(graph.Header.BaseOffset, CheckedMath.ToInt64(entry.ValueOffset));
    }

    internal static RawFormatException Invalid(string message, long offset)
    {
        return new RawFormatException(RawDiagnosticCode.InvalidMetadata, message, offset);
    }
}
