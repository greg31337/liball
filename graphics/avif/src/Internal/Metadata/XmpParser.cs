using System.Collections.Generic;
using System.Text;

namespace avif;

internal class XmpParserWorker
{
    const string RdfNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
    const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

    string text;
    int position;
    AvifSecurityLimits limits;
    XmpNode? root;
    XmpNode? current;
    bool rootClosed;
    bool xmlDeclaration;
    int objectCount;
    AvifResultCode code;

    internal XmpParserWorker(string text, AvifSecurityLimits limits)
    {
        this.text = text;
        this.limits = limits;
        position = text.Length != 0 && text[0] == '\ufeff' ? 1 : 0;
        root = null;
        current = null;
        rootClosed = false;
        xmlDeclaration = false;
        objectCount = 0;
        code = AvifResultCode.Success;
    }

    internal XmpParseResult Parse(byte[] data)
    {
        XmpParseResult result = new XmpParseResult(
            AvifResultCode.InvalidMetadata);
        while (code == AvifResultCode.Success && position < text.Length)
        {
            if (text[position] == '<')
            {
                ParseMarkup();
            }
            else
            {
                ParseText();
            }
        }
        if (code != AvifResultCode.Success)
        {
            result.Code = code;
            return result;
        }
        if (root == null || current != null || !rootClosed)
        {
            return result;
        }
        XmpParsedData parsed = new XmpParsedData();
        parsed.Data = (byte[])data.Clone();
        if (!ExtractFields(root, parsed.Fields))
        {
            return result;
        }
        result.Code = AvifResultCode.Success;
        result.Parsed = parsed;
        return result;
    }

    void ParseMarkup()
    {
        if (StartsWith("<!--"))
        {
            ParseComment();
        }
        else if (StartsWith("<![CDATA["))
        {
            ParseCdata();
        }
        else if (StartsWith("<?"))
        {
            ParseProcessingInstruction();
        }
        else if (StartsWith("</"))
        {
            ParseEndTag();
        }
        else if (StartsWith("<!"))
        {
            code = AvifResultCode.InvalidMetadata;
        }
        else
        {
            ParseStartTag();
        }
    }

    void ParseStartTag()
    {
        if (rootClosed)
        {
            code = AvifResultCode.InvalidMetadata;
            return;
        }
        position++;
        string rawName = ReadName();
        XmpQualifiedName name = SplitName(rawName);
        if (!name.Valid)
        {
            code = AvifResultCode.InvalidMetadata;
            return;
        }
        XmpNode node = new XmpNode(rawName, current);
        bool selfClosing = false;
        while (code == AvifResultCode.Success)
        {
            SkipWhitespace();
            if (position >= text.Length)
            {
                code = AvifResultCode.InvalidMetadata;
                return;
            }
            if (text[position] == '>')
            {
                position++;
                break;
            }
            if (text[position] == '/' && position + 1 < text.Length
                && text[position + 1] == '>')
            {
                position += 2;
                selfClosing = true;
                break;
            }
            string attributeName = ReadName();
            XmpQualifiedName attributeParts = SplitName(attributeName);
            if (!attributeParts.Valid || HasRawAttribute(node, attributeName))
            {
                code = AvifResultCode.InvalidMetadata;
                return;
            }
            SkipWhitespace();
            if (position >= text.Length || text[position] != '=')
            {
                code = AvifResultCode.InvalidMetadata;
                return;
            }
            position++;
            SkipWhitespace();
            if (position >= text.Length
                || (text[position] != '\'' && text[position] != '"'))
            {
                code = AvifResultCode.InvalidMetadata;
                return;
            }
            char quote = text[position];
            position++;
            int valueStart = position;
            while (position < text.Length && text[position] != quote)
            {
                if (text[position] == '<')
                {
                    code = AvifResultCode.InvalidMetadata;
                    return;
                }
                position++;
            }
            if (position >= text.Length)
            {
                code = AvifResultCode.InvalidMetadata;
                return;
            }
            XmpStringResult decoded = DecodeEntities(
                text.Substring(valueStart, position - valueStart));
            position++;
            if (!decoded.Valid)
            {
                code = AvifResultCode.InvalidMetadata;
                return;
            }
            decoded.Value = NormalizeAttributeValue(decoded.Value);
            if (attributeName == "xmlns"
                || attributeParts.Prefix == "xmlns")
            {
                string prefix = attributeName == "xmlns"
                    ? string.Empty : attributeParts.LocalName;
                if (!AddNamespace(node, prefix, decoded.Value))
                {
                    code = AvifResultCode.InvalidMetadata;
                    return;
                }
            }
            else
            {
                node.Attributes.Add(new XmpAttribute(
                    attributeName, decoded.Value));
                if (!ReserveObject())
                {
                    return;
                }
            }
        }

        if (!ResolveNode(node, name))
        {
            code = AvifResultCode.InvalidMetadata;
            return;
        }
        if (!ReserveObject())
        {
            return;
        }
        if (current != null)
        {
            current.Children.Add(node);
        }
        else if (root == null)
        {
            root = node;
        }
        else
        {
            code = AvifResultCode.InvalidMetadata;
            return;
        }
        if (!selfClosing)
        {
            current = node;
            if (GetDepth(node) > limits.MaxRecursionDepth)
            {
                code = AvifResultCode.LimitExceeded;
            }
        }
        else if (node.Parent == null)
        {
            rootClosed = true;
        }
    }

    void ParseEndTag()
    {
        position += 2;
        string name = ReadName();
        SkipWhitespace();
        if (current == null || name != current.RawName
            || position >= text.Length || text[position] != '>')
        {
            code = AvifResultCode.InvalidMetadata;
            return;
        }
        position++;
        XmpNode closed = current;
        current = current.Parent;
        if (closed.Parent == null)
        {
            rootClosed = true;
        }
    }

    void ParseText()
    {
        int start = position;
        while (position < text.Length && text[position] != '<')
        {
            position++;
        }
        string raw = text.Substring(start, position - start);
        if (raw.IndexOf("]]>", System.StringComparison.Ordinal) >= 0)
        {
            code = AvifResultCode.InvalidMetadata;
            return;
        }
        XmpStringResult decoded = DecodeEntities(raw);
        if (!decoded.Valid)
        {
            code = AvifResultCode.InvalidMetadata;
            return;
        }
        if (current == null)
        {
            if (!IsWhitespaceText(decoded.Value))
            {
                code = AvifResultCode.InvalidMetadata;
            }
            return;
        }
        current.Text.Append(NormalizeLineEndings(decoded.Value));
    }

    void ParseComment()
    {
        int end = text.IndexOf("-->", position + 4,
            System.StringComparison.Ordinal);
        if (end < 0)
        {
            code = AvifResultCode.InvalidMetadata;
            return;
        }
        int forbidden = text.IndexOf("--", position + 4,
            System.StringComparison.Ordinal);
        if (forbidden >= 0 && forbidden < end)
        {
            code = AvifResultCode.InvalidMetadata;
            return;
        }
        position = end + 3;
    }

    void ParseCdata()
    {
        if (current == null)
        {
            code = AvifResultCode.InvalidMetadata;
            return;
        }
        int start = position + 9;
        int end = text.IndexOf("]]>", start,
            System.StringComparison.Ordinal);
        if (end < 0)
        {
            code = AvifResultCode.InvalidMetadata;
            return;
        }
        current.Text.Append(NormalizeLineEndings(
            text.Substring(start, end - start)));
        position = end + 3;
    }

    void ParseProcessingInstruction()
    {
        int start = position + 2;
        int end = text.IndexOf("?>", start,
            System.StringComparison.Ordinal);
        if (end < 0)
        {
            code = AvifResultCode.InvalidMetadata;
            return;
        }
        int saved = position;
        position = start;
        string target = ReadName();
        position = saved;
        if (target.Length == 0)
        {
            code = AvifResultCode.InvalidMetadata;
            return;
        }
        if (string.Equals(
            target, "xml", System.StringComparison.OrdinalIgnoreCase))
        {
            if (target != "xml" || xmlDeclaration || root != null
                || !OnlyWhitespaceBefore(saved))
            {
                code = AvifResultCode.InvalidMetadata;
                return;
            }
            string declaration = text.Substring(start, end - start);
            if (!ValidateXmlDeclaration(declaration))
            {
                code = AvifResultCode.InvalidMetadata;
                return;
            }
            xmlDeclaration = true;
        }
        position = end + 2;
    }

    bool ValidateXmlDeclaration(string declaration)
    {
        int index = 3;
        bool version = false;
        bool encoding = false;
        bool standalone = false;
        while (index < declaration.Length)
        {
            if (!IsWhitespace(declaration[index]))
            {
                return false;
            }
            while (index < declaration.Length
                && IsWhitespace(declaration[index]))
            {
                index++;
            }
            if (index == declaration.Length)
            {
                break;
            }
            int nameStart = index;
            while (index < declaration.Length
                && declaration[index] >= 'A'
                && declaration[index] <= 'Z'
                || index < declaration.Length
                && declaration[index] >= 'a'
                && declaration[index] <= 'z')
            {
                index++;
            }
            string name = declaration.Substring(
                nameStart, index - nameStart);
            while (index < declaration.Length
                && IsWhitespace(declaration[index]))
            {
                index++;
            }
            if (name.Length == 0 || index >= declaration.Length
                || declaration[index] != '=')
            {
                return false;
            }
            index++;
            while (index < declaration.Length
                && IsWhitespace(declaration[index]))
            {
                index++;
            }
            if (index >= declaration.Length
                || (declaration[index] != '\''
                    && declaration[index] != '"'))
            {
                return false;
            }
            char quote = declaration[index];
            index++;
            int valueStart = index;
            while (index < declaration.Length
                && declaration[index] != quote)
            {
                index++;
            }
            if (index >= declaration.Length)
            {
                return false;
            }
            string value = declaration.Substring(
                valueStart, index - valueStart);
            index++;
            if (name == "version" && !version
                && !encoding && !standalone && value == "1.0")
            {
                version = true;
            }
            else if (name == "encoding" && version
                && !encoding && !standalone
                && string.Equals(
                    value, "utf-8",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                encoding = true;
            }
            else if (name == "standalone" && version && !standalone
                && (value == "yes" || value == "no"))
            {
                standalone = true;
            }
            else
            {
                return false;
            }
        }
        return version;
    }

    bool ResolveNode(XmpNode node, XmpQualifiedName name)
    {
        node.Prefix = name.Prefix;
        node.LocalName = name.LocalName;
        node.NamespaceUri = ResolveNamespace(
            node, name.Prefix, true);
        if (name.Prefix.Length != 0 && node.NamespaceUri.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < node.Attributes.Count)
        {
            XmpAttribute attribute = node.Attributes[index];
            XmpQualifiedName parts = SplitName(attribute.RawName);
            attribute.Prefix = parts.Prefix;
            attribute.LocalName = parts.LocalName;
            attribute.NamespaceUri = ResolveNamespace(
                node, parts.Prefix, false);
            if (parts.Prefix.Length != 0
                && attribute.NamespaceUri.Length == 0)
            {
                return false;
            }
            int previous = 0;
            while (previous < index)
            {
                XmpAttribute other = node.Attributes[previous];
                if (other.NamespaceUri == attribute.NamespaceUri
                    && other.LocalName == attribute.LocalName)
                {
                    return false;
                }
                previous++;
            }
            index++;
        }
        return true;
    }

    bool AddNamespace(XmpNode node, string prefix, string uri)
    {
        if (prefix == "xmlns" || prefix.Length != 0 && uri.Length == 0
            || uri == XmlnsNamespace
            || prefix == "xml" && uri != XmlNamespace
            || prefix != "xml" && uri == XmlNamespace)
        {
            return false;
        }
        int index = 0;
        while (index < node.Namespaces.Count)
        {
            if (node.Namespaces[index].Prefix == prefix)
            {
                return false;
            }
            index++;
        }
        node.Namespaces.Add(new XmpNamespace(prefix, uri));
        return ReserveObject();
    }

    string ResolveNamespace(
        XmpNode node,
        string prefix,
        bool element)
    {
        if (prefix == "xml")
        {
            return XmlNamespace;
        }
        if (prefix.Length == 0 && !element)
        {
            return string.Empty;
        }
        XmpNode? currentNode = node;
        while (currentNode != null)
        {
            int index = 0;
            while (index < currentNode.Namespaces.Count)
            {
                if (currentNode.Namespaces[index].Prefix == prefix)
                {
                    return currentNode.Namespaces[index].Uri;
                }
                index++;
            }
            currentNode = currentNode.Parent;
        }
        return string.Empty;
    }

    bool ExtractFields(XmpNode rootNode, List<XmpField> fields)
    {
        List<XmpNode> rdfNodes = new List<XmpNode>();
        CollectNamedNodes(rootNode, RdfNamespace, "RDF", rdfNodes);
        if (rdfNodes.Count != 1)
        {
            return false;
        }
        List<XmpNode> descriptions = new List<XmpNode>();
        CollectDescriptions(rdfNodes[0], descriptions);
        if (descriptions.Count == 0)
        {
            return false;
        }
        int descriptionIndex = 0;
        while (descriptionIndex < descriptions.Count)
        {
            XmpNode description = descriptions[descriptionIndex];
            int attributeIndex = 0;
            while (attributeIndex < description.Attributes.Count)
            {
                XmpAttribute attribute = description.Attributes[
                    attributeIndex];
                if (attribute.NamespaceUri.Length != 0
                    && attribute.NamespaceUri != RdfNamespace
                    && attribute.NamespaceUri != XmlNamespace)
                {
                    AddField(
                        fields,
                        attribute.NamespaceUri,
                        attribute.LocalName,
                        attribute.Value);
                }
                attributeIndex++;
            }
            int childIndex = 0;
            while (childIndex < description.Children.Count)
            {
                XmpNode child = description.Children[childIndex];
                if (child.NamespaceUri.Length != 0
                    && child.NamespaceUri != RdfNamespace)
                {
                    string value = GetPropertyValue(child);
                    AddField(fields, child.NamespaceUri,
                        child.LocalName, value);
                }
                childIndex++;
            }
            descriptionIndex++;
        }
        return true;
    }

    void CollectNamedNodes(
        XmpNode node,
        string namespaceUri,
        string name,
        List<XmpNode> matches)
    {
        if (node.NamespaceUri == namespaceUri
            && node.LocalName == name)
        {
            matches.Add(node);
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            CollectNamedNodes(
                node.Children[index], namespaceUri, name, matches);
            index++;
        }
    }

    void CollectDescriptions(
        XmpNode node,
        List<XmpNode> descriptions)
    {
        if (node.NamespaceUri == RdfNamespace
            && node.LocalName == "Description")
        {
            descriptions.Add(node);
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            CollectDescriptions(node.Children[index], descriptions);
            index++;
        }
    }

    string GetPropertyValue(XmpNode node)
    {
        XmpAttribute? resource = FindAttribute(
            node, RdfNamespace, "resource");
        if (resource != null)
        {
            return resource.Value;
        }
        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            XmpNode container = node.Children[childIndex];
            if (container.NamespaceUri == RdfNamespace
                && (container.LocalName == "Alt"
                    || container.LocalName == "Seq"
                    || container.LocalName == "Bag"))
            {
                XmpNode? selected = null;
                int itemIndex = 0;
                while (itemIndex < container.Children.Count)
                {
                    XmpNode item = container.Children[itemIndex];
                    if (item.NamespaceUri == RdfNamespace
                        && item.LocalName == "li")
                    {
                        if (selected == null)
                        {
                            selected = item;
                        }
                        XmpAttribute? language = FindAttribute(
                            item, XmlNamespace, "lang");
                        if (container.LocalName == "Alt"
                            && language != null
                            && language.Value == "x-default")
                        {
                            selected = item;
                            break;
                        }
                    }
                    itemIndex++;
                }
                return selected == null
                    ? string.Empty : selected.Text.ToString().Trim();
            }
            childIndex++;
        }
        return node.Text.ToString().Trim();
    }

    void AddField(
        List<XmpField> fields,
        string namespaceUri,
        string name,
        string value)
    {
        fields.Add(new XmpField(namespaceUri, name, value));
    }

    XmpAttribute? FindAttribute(
        XmpNode node,
        string namespaceUri,
        string name)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            XmpAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri == namespaceUri
                && attribute.LocalName == name)
            {
                return attribute;
            }
            index++;
        }
        return null;
    }

    string ReadName()
    {
        int start = position;
        while (position < text.Length && IsNameCharacter(text[position]))
        {
            position++;
        }
        return text.Substring(start, position - start);
    }

    XmpQualifiedName SplitName(string value)
    {
        XmpQualifiedName result = new XmpQualifiedName();
        if (value.Length == 0)
        {
            return result;
        }
        int colon = value.IndexOf(':');
        if (colon >= 0 && value.IndexOf(':', colon + 1) >= 0)
        {
            return result;
        }
        string prefix = colon < 0 ? string.Empty : value.Substring(0, colon);
        string local = colon < 0 ? value : value.Substring(colon + 1);
        if (!IsNamePart(prefix, true) || !IsNamePart(local, false))
        {
            return result;
        }
        result.Valid = true;
        result.Prefix = prefix;
        result.LocalName = local;
        return result;
    }

    bool IsNamePart(string value, bool allowEmpty)
    {
        if (value.Length == 0)
        {
            return allowEmpty;
        }
        if (!IsNameStart(value[0]))
        {
            return false;
        }
        int index = 1;
        while (index < value.Length)
        {
            if (!IsNameCharacter(value[index]) || value[index] == ':')
            {
                return false;
            }
            index++;
        }
        return true;
    }

    bool IsNameStart(char value)
    {
        return value == '_' || value >= 'A' && value <= 'Z'
            || value >= 'a' && value <= 'z'
            || value >= 128 && char.IsLetter(value);
    }

    bool IsNameCharacter(char value)
    {
        return IsNameStart(value) || value >= '0' && value <= '9'
            || value == '-' || value == '.' || value == ':'
            || value >= 128 && char.IsLetterOrDigit(value);
    }

    XmpStringResult DecodeEntities(string value)
    {
        XmpStringResult result = new XmpStringResult();
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] != '&')
            {
                builder.Append(value[index]);
                index++;
                continue;
            }
            int semicolon = value.IndexOf(';', index + 1);
            if (semicolon < 0)
            {
                return result;
            }
            string entity = value.Substring(
                index + 1, semicolon - index - 1);
            if (entity == "amp")
            {
                builder.Append('&');
            }
            else if (entity == "lt")
            {
                builder.Append('<');
            }
            else if (entity == "gt")
            {
                builder.Append('>');
            }
            else if (entity == "apos")
            {
                builder.Append('\'');
            }
            else if (entity == "quot")
            {
                builder.Append('"');
            }
            else
            {
                int scalar = ParseCharacterReference(entity);
                if (!IsXmlScalar(scalar))
                {
                    return result;
                }
                builder.Append(char.ConvertFromUtf32(scalar));
            }
            index = semicolon + 1;
        }
        result.Valid = true;
        result.Value = builder.ToString();
        return result;
    }

    int ParseCharacterReference(string entity)
    {
        if (entity.Length < 2 || entity[0] != '#')
        {
            return -1;
        }
        bool hexadecimal = entity.Length >= 3
            && (entity[1] == 'x' || entity[1] == 'X');
        int index = hexadecimal ? 2 : 1;
        if (index >= entity.Length)
        {
            return -1;
        }
        int value = 0;
        while (index < entity.Length)
        {
            int digit = GetDigit(entity[index], hexadecimal);
            int radix = hexadecimal ? 16 : 10;
            if (digit < 0 || value > (1114111 - digit) / radix)
            {
                return -1;
            }
            value = value * radix + digit;
            index++;
        }
        return value;
    }

    int GetDigit(char value, bool hexadecimal)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (hexadecimal && value >= 'a' && value <= 'f')
        {
            return 10 + value - 'a';
        }
        if (hexadecimal && value >= 'A' && value <= 'F')
        {
            return 10 + value - 'A';
        }
        return -1;
    }

    bool ReserveObject()
    {
        objectCount++;
        long estimate = (long)text.Length * 2
            + (long)objectCount * 128;
        if (estimate > limits.MaxTemporaryBytes)
        {
            code = AvifResultCode.LimitExceeded;
            return false;
        }
        return true;
    }

    int GetDepth(XmpNode node)
    {
        int depth = 1;
        XmpNode? parent = node.Parent;
        while (parent != null)
        {
            depth++;
            parent = parent.Parent;
        }
        return depth;
    }

    bool HasRawAttribute(XmpNode node, string rawName)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            if (node.Attributes[index].RawName == rawName)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    void SkipWhitespace()
    {
        while (position < text.Length && IsWhitespace(text[position]))
        {
            position++;
        }
    }

    bool OnlyWhitespaceBefore(int end)
    {
        int index = text.Length != 0 && text[0] == '\ufeff' ? 1 : 0;
        while (index < end)
        {
            if (!IsWhitespace(text[index]))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    bool IsWhitespaceText(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!IsWhitespace(value[index]))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    bool IsWhitespace(char value)
    {
        return value == ' ' || value == '\t'
            || value == '\r' || value == '\n';
    }

    bool StartsWith(string value)
    {
        return position <= text.Length - value.Length
            && string.CompareOrdinal(
                text, position, value, 0, value.Length) == 0;
    }

    string NormalizeAttributeValue(string value)
    {
        StringBuilder builder = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            builder.Append(character == '\t'
                    || character == '\r' || character == '\n'
                ? ' ' : character);
            index++;
        }
        return builder.ToString();
    }

    string NormalizeLineEndings(string value)
    {
        StringBuilder builder = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\r')
            {
                builder.Append('\n');
                if (index + 1 < value.Length && value[index + 1] == '\n')
                {
                    index++;
                }
            }
            else
            {
                builder.Append(value[index]);
            }
            index++;
        }
        return builder.ToString();
    }

    internal static bool IsValidXmlText(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            int scalar;
            char first = value[index];
            if (char.IsHighSurrogate(first))
            {
                if (index + 1 >= value.Length
                    || !char.IsLowSurrogate(value[index + 1]))
                {
                    return false;
                }
                scalar = char.ConvertToUtf32(first, value[index + 1]);
                index += 2;
            }
            else
            {
                if (char.IsLowSurrogate(first))
                {
                    return false;
                }
                scalar = first;
                index++;
            }
            if (!IsXmlScalar(scalar))
            {
                return false;
            }
        }
        return true;
    }

    static bool IsXmlScalar(int value)
    {
        if (value >= 0 && value <= 65535)
        {
            return System.Xml.XmlConvert.IsXmlChar((char)value);
        }
        return value >= 65536 && value <= 1114111;
    }
}

internal static class XmpParser
{
    internal static XmpParseResult Parse(
        byte[] data,
        AvifSecurityLimits limits)
    {
        XmpParseResult result = new XmpParseResult(
            AvifResultCode.InvalidMetadata);
        if (data == null || limits == null)
        {
            result.Code = AvifResultCode.InvalidArgument;
            return result;
        }
        if (data.LongLength > limits.MaxXmpBytes
            || data.LongLength > limits.MaxMetadataBytes)
        {
            result.Code = AvifResultCode.LimitExceeded;
            return result;
        }
        if (data.Length == 0 || !Utf8Utility.IsValid(data))
        {
            return result;
        }
        string text = Encoding.UTF8.GetString(data);
        if (!XmpParserWorker.IsValidXmlText(text))
        {
            return result;
        }
        long initialEstimate = (long)text.Length * 2;
        if (initialEstimate > limits.MaxTemporaryBytes)
        {
            result.Code = AvifResultCode.LimitExceeded;
            return result;
        }
        return new XmpParserWorker(text, limits).Parse(data);
    }
}
