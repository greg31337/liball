namespace raw;

/// <summary>Represents one simple or RDF-container XMP field discovered during safe streaming parsing.</summary>
public class RawXmpField
{
    /// <summary>The property namespace URI.</summary>
    public string NamespaceUri;
    /// <summary>The source namespace prefix, or an empty string.</summary>
    public string Prefix;
    /// <summary>The local property name.</summary>
    public string Name;
    /// <summary>The decoded property value.</summary>
    public string Value;
    /// <summary>The inherited <c>xml:lang</c> value, or an empty string.</summary>
    public string Language;
    /// <summary>Whether the value was serialized as an RDF description attribute.</summary>
    public bool Attribute;
    /// <summary>A stable slash-separated XML element path.</summary>
    public string Path;

    /// <summary>Creates an XMP field description.</summary>
    /// <param name="namespaceUri">The namespace URI.</param>
    /// <param name="prefix">The namespace prefix.</param>
    /// <param name="name">The local name.</param>
    /// <param name="value">The decoded value.</param>
    /// <param name="language">The language, or an empty string.</param>
    /// <param name="attribute">Whether the field came from an attribute.</param>
    /// <param name="path">The stable element path.</param>
    public RawXmpField(string namespaceUri, string prefix, string name, string value, string language, bool attribute, string path)
    {
        NamespaceUri = namespaceUri == null ? string.Empty : namespaceUri;
        Prefix = prefix == null ? string.Empty : prefix;
        Name = name == null ? string.Empty : name;
        Value = value == null ? string.Empty : value;
        Language = language == null ? string.Empty : language;
        Attribute = attribute;
        Path = path == null ? string.Empty : path;
    }
}

/// <summary>Retains one exact XMP packet, its encoding and wrapper padding, and safely extracted RDF fields.</summary>
public class RawXmpPacket
{
    /// <summary>The exact encoded packet bytes.</summary>
    public byte[]? RawBytes;
    /// <summary>The packet character encoding.</summary>
    public RawXmpEncoding Encoding;
    /// <summary>Whether the packet begins with a matching byte-order mark.</summary>
    public bool HasByteOrderMark;
    /// <summary>The decoded root XML element without packet wrapper or padding.</summary>
    public string? XmlText;
    /// <summary>Exact bytes before the root XML element, including a BOM and opening packet wrapper.</summary>
    public byte[]? PrefixBytes;
    /// <summary>Exact bytes occupied by the root XML element.</summary>
    public byte[]? XmlBytes;
    /// <summary>Exact whitespace bytes after the root element and before a closing packet wrapper.</summary>
    public byte[]? PaddingBytes;
    /// <summary>Exact non-padding bytes after the root element.</summary>
    public byte[]? SuffixBytes;
    /// <summary>Whether the closing packet wrapper declares the packet read-only.</summary>
    public bool ReadOnly;
    /// <summary>The absolute packet offset in the containing source, or <c>-1</c>.</summary>
    public long Offset;
    /// <summary>The containing stable IFD path, or an empty string.</summary>
    public string? IfdPath;
    /// <summary>Safely extracted fields in XML encounter order.</summary>
    public RawXmpField[]? Fields;

    /// <summary>Parses a bounded standalone XMP packet.</summary>
    /// <param name="rawBytes">The exact packet bytes.</param>
    /// <param name="limits">The resource limits.</param>
    /// <exception cref="RawFormatException">The packet, encoding, wrapper, or limits are invalid.</exception>
    /// <exception cref="System.Xml.XmlException">The XML is malformed, contains a prohibited DTD, or exceeds XML reader constraints.</exception>
    /// <returns>The parsed packet.</returns>
    internal static RawXmpPacket Parse(byte[] rawBytes, RawLimits limits)
    {
        return XmpCodec.Parse(rawBytes, limits, -1, string.Empty);
    }

    internal RawXmpPacket(byte[] rawBytes, RawXmpEncoding encoding, bool hasByteOrderMark, string xmlText, byte[] prefixBytes, byte[] xmlBytes, byte[] paddingBytes, byte[] suffixBytes, bool readOnly, long offset, string ifdPath, RawXmpField[] fields)
    {
        RawBytes = rawBytes;
        Encoding = encoding;
        HasByteOrderMark = hasByteOrderMark;
        XmlText = xmlText;
        PrefixBytes = prefixBytes;
        XmlBytes = xmlBytes;
        PaddingBytes = paddingBytes;
        SuffixBytes = suffixBytes;
        ReadOnly = readOnly;
        Offset = offset;
        IfdPath = ifdPath;
        Fields = fields;
    }

    /// <summary>Returns an independent copy of the complete encoded packet.</summary>
    /// <returns>The copied packet.</returns>
    internal byte[] CopyBytes()
    {
        return RawTextValue.Copy(RawBytes);
    }

    /// <summary>Creates a TIFF BYTE value suitable for XMP tag 700.</summary>
    /// <returns>An exact independent typed value.</returns>
    internal RawTagValue ToTagValue()
    {
        return RawTagValue.CreateByte(CopyBytes());
    }

    /// <summary>Finds the first field matching a namespace URI, local name, and optional language.</summary>
    /// <param name="namespaceUri">The exact namespace URI.</param>
    /// <param name="name">The exact local name.</param>
    /// <param name="language">The exact language, or <c>null</c> to accept any language.</param>
    /// <returns>The first matching field, or <c>null</c>.</returns>
    internal RawXmpField? GetField(string namespaceUri, string name, string? language)
    {
        if (namespaceUri == null || name == null)
        {
            return null;
        }
        int index = 0;
        while (index < (Fields ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            RawXmpField field = Fields[index];
            if (field != null && field.NamespaceUri == namespaceUri && field.Name == name && (language == null || field.Language == language))
            {
                return field;
            }
            index++;
        }
        return null;
    }

    /// <summary>Returns every field matching a namespace URI and local name.</summary>
    /// <param name="namespaceUri">The exact namespace URI.</param>
    /// <param name="name">The exact local name.</param>
    /// <returns>Matching fields in encounter order.</returns>
    internal RawXmpField[] GetFields(string namespaceUri, string name)
    {
        System.Collections.Generic.List<RawXmpField> matches = new System.Collections.Generic.List<RawXmpField>();
        if (namespaceUri == null || name == null)
        {
            return matches.ToArray();
        }
        int index = 0;
        while (index < (Fields ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            RawXmpField field = Fields[index];
            if (field != null && field.NamespaceUri == namespaceUri && field.Name == name)
            {
                matches.Add(field);
            }
            index++;
        }
        return matches.ToArray();
    }

    /// <summary>Replaces the root XML while retaining the original encoding, packet wrapper, and padding bytes.</summary>
    /// <param name="xml">A complete replacement root element without an XMP packet wrapper.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The parsed replacement packet.</returns>
    /// <exception cref="RawFormatException">The replacement is invalid or exceeds the configured limit.</exception>
    /// <exception cref="System.Xml.XmlException">The replacement XML is malformed or unsafe.</exception>
    internal RawXmpPacket ReplaceXml(string xml, RawLimits limits)
    {
        return XmpCodec.Replace(this, xml, limits, false);
    }

    /// <summary>Replaces the root XML while retaining the exact total packet length by consuming or extending whitespace padding.</summary>
    /// <param name="xml">A complete replacement root element without an XMP packet wrapper.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The parsed fixed-length replacement packet.</returns>
    /// <exception cref="RawFormatException">The replacement cannot fit, is invalid, or exceeds the configured limit.</exception>
    /// <exception cref="System.Xml.XmlException">The replacement XML is malformed or unsafe.</exception>
    internal RawXmpPacket ReplaceXmlFixedSize(string xml, RawLimits limits)
    {
        return XmpCodec.Replace(this, xml, limits, true);
    }

}
