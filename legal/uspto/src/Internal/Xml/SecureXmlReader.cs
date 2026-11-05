namespace uspto;

internal class SecureXmlAttribute
{
    internal bool IsPresent = false;
    internal string Name = string.Empty;
    internal string LocalName = string.Empty;
    internal string NamespaceUri = string.Empty;
    internal string Prefix = string.Empty;
    internal string Value = string.Empty;
    internal string Path = "/";
    internal int LineNumber = 0;
    internal int LinePosition = 0;
    internal bool HasLineInfo = false;
}

internal class SecureXmlNode
{
    internal global::System.Xml.XmlNodeType NodeType = global::System.Xml.XmlNodeType.None;
    internal string Name = string.Empty;
    internal string LocalName = string.Empty;
    internal string NamespaceUri = string.Empty;
    internal string Prefix = string.Empty;
    internal string Value = string.Empty;
    internal string Path = "/";
    internal int Depth = 0;
    internal bool IsEmptyElement = false;
    internal int LineNumber = 0;
    internal int LinePosition = 0;
    internal bool HasLineInfo = false;
    internal SecureXmlAttribute[] Attributes = new SecureXmlAttribute[0];
}

internal class SecureXmlReader
{
    internal global::System.Xml.XmlReader Reader;
    internal string? SourceName;
    internal int MaximumDepth;
    internal int MaximumTextCharacters;
    internal SecureXmlNode Current = new SecureXmlNode();
    internal global::System.Collections.Generic.List<string> ElementPaths = new global::System.Collections.Generic.List<string>();
    internal global::System.Collections.Generic.List<global::System.Collections.Generic.Dictionary<string, int>> SiblingCounts = new global::System.Collections.Generic.List<global::System.Collections.Generic.Dictionary<string, int>>();
    internal int ConsecutiveTextCharacters = 0;
    internal global::System.Threading.CancellationToken CancellationToken = default(global::System.Threading.CancellationToken);

    internal SecureXmlReader(global::System.IO.Stream input, string? sourceName, UsptoLimits limits, bool ignoreDocumentType)
    {
        if (input == null)
        {
            throw new ValidationException("An XML input stream is required.", "input", "REQUIRED", null);
        }

        if (!input.CanRead)
        {
            throw new ValidationException("The XML input stream must be readable.", "input", "STREAM_NOT_READABLE", null);
        }

        if (limits == null)
        {
            throw new ValidationException("XML parsing limits are required.", "limits", "REQUIRED", null);
        }

        if (limits.MaximumXmlDepth <= 0)
        {
            throw new ValidationException("The maximum XML depth must be positive.", "MaximumXmlDepth", "INVALID_LIMIT", null);
        }

        if (limits.MaximumStringCharacters <= 0)
        {
            throw new ValidationException("The maximum XML text length must be positive.", "MaximumStringCharacters", "INVALID_LIMIT", null);
        }

        SourceName = sourceName;
        MaximumDepth = limits.MaximumXmlDepth;
        MaximumTextCharacters = limits.MaximumStringCharacters;

        global::System.Xml.XmlReaderSettings settings = new global::System.Xml.XmlReaderSettings();
        settings.Async = false;
        settings.CheckCharacters = true;
        settings.CloseInput = false;
        settings.ConformanceLevel = global::System.Xml.ConformanceLevel.Document;
        settings.DtdProcessing = ignoreDocumentType ? global::System.Xml.DtdProcessing.Ignore : global::System.Xml.DtdProcessing.Prohibit;
        settings.IgnoreComments = false;
        settings.IgnoreProcessingInstructions = false;
        settings.IgnoreWhitespace = false;
        settings.XmlResolver = null;
        Reader = global::System.Xml.XmlReader.Create(input, settings);
        SiblingCounts.Add(new global::System.Collections.Generic.Dictionary<string, int>(global::System.StringComparer.Ordinal));
    }

    internal bool Read()
    {
        CancellationToken.ThrowIfCancellationRequested();
        bool hasNode = Reader.Read();
        if (!hasNode)
        {
            Current = new SecureXmlNode();
            ConsecutiveTextCharacters = 0;
            return false;
        }

        SecureXmlNode node = new SecureXmlNode();
        node.NodeType = Reader.NodeType;
        node.Name = Reader.Name;
        node.LocalName = Reader.LocalName;
        node.NamespaceUri = Reader.NamespaceURI;
        node.Prefix = Reader.Prefix;
        node.Depth = Reader.Depth;
        node.IsEmptyElement = Reader.NodeType == global::System.Xml.XmlNodeType.Element && Reader.IsEmptyElement;
        CaptureLineInfo(node);
        node.Path = BuildCurrentPath(node);
        Current = node;

        if (node.NodeType == global::System.Xml.XmlNodeType.Element && node.Depth + 1 > MaximumDepth)
        {
            ThrowDepthLimit(node);
        }

        if (Reader.HasValue)
        {
            node.Value = ReadBoundedValue(node.Path, node.LineNumber, node.HasLineInfo);
        }

        bool isText = node.NodeType == global::System.Xml.XmlNodeType.Text || node.NodeType == global::System.Xml.XmlNodeType.CDATA || node.NodeType == global::System.Xml.XmlNodeType.Whitespace || node.NodeType == global::System.Xml.XmlNodeType.SignificantWhitespace;
        if (isText)
        {
            if (node.Value.Length > MaximumTextCharacters - ConsecutiveTextCharacters)
            {
                ThrowTextLimit(node.Path, node.LineNumber, node.HasLineInfo);
            }

            ConsecutiveTextCharacters += node.Value.Length;
        }
        else
        {
            ConsecutiveTextCharacters = 0;
        }

        if (node.NodeType == global::System.Xml.XmlNodeType.Element)
        {
            node.Attributes = CaptureAttributes(node.Path);
        }

        return true;
    }

    internal bool IsElement(string localName, string? namespaceUri)
    {
        if (localName == null)
        {
            throw new ValidationException("An XML element local name is required.", "localName", "REQUIRED", null);
        }

        if (Current.NodeType != global::System.Xml.XmlNodeType.Element || !Current.LocalName.Equals(localName, global::System.StringComparison.Ordinal))
        {
            return false;
        }

        return namespaceUri == null || Current.NamespaceUri.Equals(namespaceUri, global::System.StringComparison.Ordinal);
    }

    internal SecureXmlAttribute ReadAttribute(string localName, string? namespaceUri)
    {
        if (localName == null)
        {
            throw new ValidationException("An XML attribute local name is required.", "localName", "REQUIRED", null);
        }

        foreach (SecureXmlAttribute attribute in Current.Attributes)
        {
            bool namespaceMatches = namespaceUri == null || attribute.NamespaceUri.Equals(namespaceUri, global::System.StringComparison.Ordinal);
            if (namespaceMatches && attribute.LocalName.Equals(localName, global::System.StringComparison.Ordinal))
            {
                return attribute;
            }
        }

        SecureXmlAttribute missing = new SecureXmlAttribute();
        missing.Path = Current.Path + "/@" + localName;
        return missing;
    }

    internal void Close()
    {
        Reader.Close();
    }

    internal void CaptureLineInfo(SecureXmlNode node)
    {
        global::System.Xml.IXmlLineInfo lineInfo = (global::System.Xml.IXmlLineInfo)Reader;
        node.HasLineInfo = lineInfo.HasLineInfo();
        if (node.HasLineInfo)
        {
            node.LineNumber = lineInfo.LineNumber;
            node.LinePosition = lineInfo.LinePosition;
        }
    }

    internal string BuildCurrentPath(SecureXmlNode node)
    {
        if (node.NodeType == global::System.Xml.XmlNodeType.Element)
        {
            while (SiblingCounts.Count <= node.Depth)
            {
                SiblingCounts.Add(new global::System.Collections.Generic.Dictionary<string, int>(global::System.StringComparer.Ordinal));
            }

            global::System.Collections.Generic.Dictionary<string, int> counts = SiblingCounts[node.Depth];
            string key = node.NamespaceUri + "\u001f" + node.LocalName;
            int occurrence = 1;
            if (counts.ContainsKey(key))
            {
                occurrence = counts[key] + 1;
            }

            counts[key] = occurrence;
            string segment = node.Name;
            if (occurrence > 1)
            {
                segment += "[" + occurrence.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            }

            string path = "/" + segment;
            if (node.Depth > 0 && ElementPaths.Count >= node.Depth)
            {
                path = ElementPaths[node.Depth - 1] + "/" + segment;
            }

            while (ElementPaths.Count <= node.Depth)
            {
                ElementPaths.Add(string.Empty);
            }

            ElementPaths[node.Depth] = path;
            while (SiblingCounts.Count > node.Depth + 1)
            {
                SiblingCounts.RemoveAt(SiblingCounts.Count - 1);
            }

            SiblingCounts.Add(new global::System.Collections.Generic.Dictionary<string, int>(global::System.StringComparer.Ordinal));
            return path;
        }

        if (node.NodeType == global::System.Xml.XmlNodeType.EndElement && ElementPaths.Count > node.Depth)
        {
            return ElementPaths[node.Depth];
        }

        if (node.Depth > 0 && ElementPaths.Count >= node.Depth)
        {
            return ElementPaths[node.Depth - 1];
        }

        return "/";
    }

    internal string ReadBoundedValue(string path, int lineNumber, bool hasLineNumber)
    {
        global::System.Text.StringBuilder value = new global::System.Text.StringBuilder();
        char[] buffer = new char[4096];
        int total = 0;
        while (true)
        {
            CancellationToken.ThrowIfCancellationRequested();
            int read = Reader.ReadValueChunk(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            if (read > MaximumTextCharacters - total)
            {
                ThrowTextLimit(path, lineNumber, hasLineNumber);
            }

            value.Append(buffer, 0, read);
            total += read;
        }

        return value.ToString();
    }

    internal SecureXmlAttribute[] CaptureAttributes(string elementPath)
    {
        global::System.Collections.Generic.List<SecureXmlAttribute> attributes = new global::System.Collections.Generic.List<SecureXmlAttribute>();
        bool hasAttribute = Reader.MoveToFirstAttribute();
        while (hasAttribute)
        {
            SecureXmlAttribute attribute = new SecureXmlAttribute();
            attribute.IsPresent = true;
            attribute.Name = Reader.Name;
            attribute.LocalName = Reader.LocalName;
            attribute.NamespaceUri = Reader.NamespaceURI;
            attribute.Prefix = Reader.Prefix;
            attribute.Path = elementPath + "/@" + attribute.Name;
            global::System.Xml.IXmlLineInfo lineInfo = (global::System.Xml.IXmlLineInfo)Reader;
            attribute.HasLineInfo = lineInfo.HasLineInfo();
            if (attribute.HasLineInfo)
            {
                attribute.LineNumber = lineInfo.LineNumber;
                attribute.LinePosition = lineInfo.LinePosition;
            }

            attribute.Value = ReadBoundedValue(attribute.Path, attribute.LineNumber, attribute.HasLineInfo);
            attributes.Add(attribute);
            hasAttribute = Reader.MoveToNextAttribute();
        }

        Reader.MoveToElement();
        SecureXmlAttribute[] result = new SecureXmlAttribute[attributes.Count];
        attributes.CopyTo(result, 0);
        return result;
    }

    internal void ThrowDepthLimit(SecureXmlNode node)
    {
        throw new ParseException("The XML document exceeds the configured nesting depth.", DocumentFormat.Xml, SourceName, node.Path, 0L, false, node.LineNumber, node.HasLineInfo, null);
    }

    internal void ThrowTextLimit(string path, int lineNumber, bool hasLineNumber)
    {
        throw new ParseException("The XML value exceeds the configured text length.", DocumentFormat.Xml, SourceName, path, 0L, false, lineNumber, hasLineNumber, null);
    }
}
