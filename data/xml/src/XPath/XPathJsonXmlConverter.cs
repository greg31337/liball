using System;
using System.Collections.Generic;
using System.Text;

namespace xml;

internal class XPathJsonXmlConverter
{
    internal SourceRange Range;
    internal XdmSequenceOperations Sequences;
    internal XdmFactory Factory;
    internal TreeNavigation Navigation;
    internal XdmStringDatatype Strings;

    internal XPathJsonXmlConverter(SourceRange range)
    {
        Range = range;
        Sequences = new XdmSequenceOperations();
        Factory = new XdmFactory();
        Navigation = new TreeNavigation();
        Strings = new XdmStringDatatype();
    }

    internal XdmSequence ConvertXPathXmlToJson(XdmSequence supplied, XPathJsonOptions options)
    {
        Sequences.ValidateXdmSequenceStorage(supplied);
        if (supplied.Count == 0)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        if (supplied.Count != 1)
        {
            throw CreateXPathJsonXmlTypeError("xml-to-json requires at most one node.");
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.Kind != XdmItemKind.Node || item.Node == null)
        {
            throw CreateXPathJsonXmlTypeError("xml-to-json requires a node.");
        }

        XmlNode root = SelectXPathJsonXmlRoot(item.Node);
        StringBuilder output = new StringBuilder();
        AppendXPathJsonFromXml(output, root, options, 0, false);
        XdmAtomicValue result = Factory.CreateXdmStringAtomicValue(output.ToString(), XdmAtomicType.String);
        return Sequences.CreateXdmAtomicSequence(result);
    }

    internal XmlNode SelectXPathJsonXmlRoot(XmlNode supplied)
    {
        if (supplied.Kind == XmlNodeKind.Element)
        {
            return supplied;
        }

        if (supplied.Kind != XmlNodeKind.Document)
        {
            throw CreateXPathJsonXmlError("The XML representation of JSON must be an element or document node.", XPathErrorCodes.FOJS0006);
        }

        XmlNode[] children = supplied.Children == null ? [] : supplied.Children;
        XmlNode? root = null;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element)
            {
                if (root != null)
                {
                    throw CreateXPathJsonXmlError("The JSON document must contain exactly one element.", XPathErrorCodes.FOJS0006);
                }

                root = child;
            }
            else if (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData)
            {
                throw CreateXPathJsonXmlError("The JSON document cannot contain text outside its document element.", XPathErrorCodes.FOJS0006);
            }
            else if (child.Kind != XmlNodeKind.Comment && child.Kind != XmlNodeKind.ProcessingInstruction)
            {
                throw CreateXPathJsonXmlError("The JSON document contains an unsupported child node.", XPathErrorCodes.FOJS0006);
            }

            index = index + 1;
        }

        return root == null ? throw CreateXPathJsonXmlError("The JSON document must contain exactly one element.", XPathErrorCodes.FOJS0006) : root;
    }

    internal void AppendXPathJsonFromXml(StringBuilder output, XmlNode element, XPathJsonOptions options, int depth, bool mapMember)
    {
        string localName = ReadXPathJsonXmlElementName(element);
        ValidateXPathJsonXmlAttributes(element, localName, mapMember, depth);
        if (localName == "null")
        {
            RequireXPathJsonXmlSimpleContent(element, false);
            output.Append("null");
            return;
        }

        if (localName == "boolean")
        {
            RequireXPathJsonXmlSimpleContent(element, true);
            string value = Navigation.GetXmlNodeStringValueOperation(element);
            XdmNumericParseResult parsed = new XdmNumericDatatype().TryParseXdmBoolean(value);
            if (!parsed.Success || parsed.Value == null)
            {
                throw CreateXPathJsonXmlError("A boolean element has an invalid value.", XPathErrorCodes.FOJS0006);
            }

            output.Append(parsed.Value.BooleanValue ? "true" : "false");
            return;
        }

        if (localName == "number")
        {
            RequireXPathJsonXmlSimpleContent(element, true);
            string value = Navigation.GetXmlNodeStringValueOperation(element);
            XdmNumericParseResult parsed = new XdmFloatingDatatype().TryParseXdmFloating(value, XdmAtomicType.Double);
            if (!parsed.Success || parsed.Value == null || double.IsInfinity(parsed.Value.FloatingValue) || double.IsNaN(parsed.Value.FloatingValue))
            {
                throw CreateXPathJsonXmlError("A number element must contain one finite xs:double value.", XPathErrorCodes.FOJS0006);
            }

            output.Append(parsed.Value.StringValue);
            return;
        }

        if (localName == "string")
        {
            RequireXPathJsonXmlSimpleContent(element, true);
            bool escaped = ReadXPathJsonXmlBooleanAttribute(element, "escaped", false);
            output.Append('"');
            AppendXPathJsonXmlString(output, Navigation.GetXmlNodeStringValueOperation(element), escaped);
            output.Append('"');
            return;
        }

        if (localName == "array")
        {
            AppendXPathJsonXmlArray(output, element, options, depth);
            return;
        }

        AppendXPathJsonXmlMap(output, element, options, depth);
    }

    internal void AppendXPathJsonXmlArray(StringBuilder output, XmlNode element, XPathJsonOptions options, int depth)
    {
        List<XmlNode> members = SelectXPathJsonXmlContainerChildren(element);
        output.Append('[');
        int index = 0;
        while (index < members.Count)
        {
            if (index > 0)
            {
                output.Append(',');
            }

            AppendXPathJsonXmlSeparator(output, options, depth + 1);
            AppendXPathJsonFromXml(output, members[index], options, depth + 1, false);
            index = index + 1;
        }

        if (members.Count > 0)
        {
            AppendXPathJsonXmlSeparator(output, options, depth);
        }

        output.Append(']');
    }

    internal void AppendXPathJsonXmlMap(StringBuilder output, XmlNode element, XPathJsonOptions options, int depth)
    {
        List<XmlNode> members = SelectXPathJsonXmlContainerChildren(element);
        List<string> normalizedKeys = new List<string>();
        output.Append('{');
        int index = 0;
        while (index < members.Count)
        {
            XmlNode child = members[index];
            XmlNode? keyAttribute = FindXPathJsonXmlAttribute(child, "key");
            if (keyAttribute == null)
            {
                throw CreateXPathJsonXmlError("Every map member must have a key attribute.", XPathErrorCodes.FOJS0006);
            }

            string key = keyAttribute.Value == null ? "" : keyAttribute.Value;
            bool escaped = ReadXPathJsonXmlBooleanAttribute(child, "escaped-key", false);
            string normalized = escaped ? DecodeXPathJsonXmlEscapedKey(key) : key;
            if (ContainsXPathJsonXmlKey(normalizedKeys, normalized))
            {
                throw CreateXPathJsonXmlError("A map contains duplicate normalized keys.", XPathErrorCodes.FOJS0006);
            }

            normalizedKeys.Add(normalized);
            if (index > 0)
            {
                output.Append(',');
            }

            AppendXPathJsonXmlSeparator(output, options, depth + 1);
            output.Append('"');
            AppendXPathJsonXmlString(output, key, escaped);
            output.Append(options.Indent ? "\": " : "\":");
            AppendXPathJsonFromXml(output, child, options, depth + 1, true);
            index = index + 1;
        }

        if (members.Count > 0)
        {
            AppendXPathJsonXmlSeparator(output, options, depth);
        }

        output.Append('}');
    }

    internal List<XmlNode> SelectXPathJsonXmlContainerChildren(XmlNode element)
    {
        List<XmlNode> result = new List<XmlNode>();
        XmlNode[] children = element.Children == null ? [] : element.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element)
            {
                ReadXPathJsonXmlElementName(child);
                result.Add(child);
            }
            else if (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData)
            {
                string value = child.Value == null ? "" : child.Value;
                if (Strings.CollapseXdmStringWhitespace(value).Length != 0)
                {
                    throw CreateXPathJsonXmlError("A map or array contains non-whitespace text.", XPathErrorCodes.FOJS0006);
                }
            }
            else if (child.Kind != XmlNodeKind.Comment && child.Kind != XmlNodeKind.ProcessingInstruction)
            {
                throw CreateXPathJsonXmlError("A map or array contains an unsupported child node.", XPathErrorCodes.FOJS0006);
            }

            index = index + 1;
        }

        return result;
    }

    internal string ReadXPathJsonXmlElementName(XmlNode element)
    {
        XmlName? name = element.Name;
        string localName = name == null || name.LocalName == null ? "" : name.LocalName;
        if (element.Kind != XmlNodeKind.Element || name == null || name.NamespaceUri != XPathNamespaces.Functions || localName != "map" && localName != "array" && localName != "string" && localName != "number" && localName != "boolean" && localName != "null")
        {
            throw CreateXPathJsonXmlError("The XML representation contains an unrecognized element.", XPathErrorCodes.FOJS0006);
        }

        return localName;
    }

    internal void ValidateXPathJsonXmlAttributes(XmlNode element, string localName, bool mapMember, int depth)
    {
        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        bool hasKey = false;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            XmlName? name = attribute.Name;
            string? namespaceUri = name == null ? null : name.NamespaceUri;
            string attributeName = name == null || name.LocalName == null ? "" : name.LocalName;
            if (namespaceUri == null || namespaceUri.Length == 0)
            {
                bool allowed = attributeName == "key" || attributeName == "escaped-key" || localName == "string" && attributeName == "escaped";
                if (!allowed)
                {
                    throw CreateXPathJsonXmlError("The XML representation contains an unsupported attribute.", XPathErrorCodes.FOJS0006);
                }

                if (attributeName == "key")
                {
                    hasKey = true;
                }

                if ((attributeName == "escaped" || attributeName == "escaped-key") && !IsXPathJsonXmlBooleanLexical(attribute.Value))
                {
                    throw CreateXPathJsonXmlError("A JSON escaping attribute has an invalid Boolean value.", XPathErrorCodes.FOJS0006);
                }
            }
            else if (namespaceUri == XPathNamespaces.Functions)
            {
                throw CreateXPathJsonXmlError("The XML representation contains an unsupported function-namespace attribute.", XPathErrorCodes.FOJS0006);
            }

            index = index + 1;
        }

        if (mapMember && !hasKey)
        {
            throw CreateXPathJsonXmlError("Every map member must have a key attribute.", XPathErrorCodes.FOJS0006);
        }

        if (!mapMember && hasKey && depth > 0)
        {
            throw CreateXPathJsonXmlError("An array member cannot have a key attribute.", XPathErrorCodes.FOJS0006);
        }
    }

    internal void RequireXPathJsonXmlSimpleContent(XmlNode element, bool allowText)
    {
        XmlNode[] children = element.Children == null ? [] : element.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNodeKind kind = children[index].Kind;
            if (kind == XmlNodeKind.Element || kind == XmlNodeKind.EntityReference || kind == XmlNodeKind.DocumentFragment || kind == XmlNodeKind.DocumentType)
            {
                throw CreateXPathJsonXmlError("A scalar JSON element contains structured content.", XPathErrorCodes.FOJS0006);
            }

            if (!allowText && (kind == XmlNodeKind.Text || kind == XmlNodeKind.CData))
            {
                throw CreateXPathJsonXmlError("A null element must have empty content.", XPathErrorCodes.FOJS0006);
            }

            index = index + 1;
        }
    }

    internal bool ReadXPathJsonXmlBooleanAttribute(XmlNode element, string localName, bool defaultValue)
    {
        XmlNode? attribute = FindXPathJsonXmlAttribute(element, localName);
        if (attribute == null)
        {
            return defaultValue;
        }

        XdmNumericParseResult parsed = new XdmNumericDatatype().TryParseXdmBoolean(attribute.Value == null ? "" : attribute.Value);
        if (!parsed.Success || parsed.Value == null)
        {
            throw CreateXPathJsonXmlError("A JSON escaping attribute has an invalid Boolean value.", XPathErrorCodes.FOJS0006);
        }

        return parsed.Value.BooleanValue;
    }

    internal XmlNode? FindXPathJsonXmlAttribute(XmlNode element, string localName)
    {
        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlName? name = attributes[index].Name;
            if (name != null && name.LocalName == localName && (name.NamespaceUri == null || name.NamespaceUri.Length == 0))
            {
                return attributes[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void AppendXPathJsonXmlString(StringBuilder output, string value, bool escaped)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (escaped && current == '\\')
            {
                int length = ReadXPathJsonXmlEscapeLength(value, index);
                output.Append(value, index, length);
                index = index + length;
                continue;
            }

            if (current == '"')
            {
                output.Append("\\\"");
            }
            else if (current == '\\')
            {
                output.Append("\\\\");
            }
            else if (current == '/')
            {
                output.Append("\\/");
            }
            else if (current == '\b')
            {
                output.Append("\\b");
            }
            else if (current == '\f')
            {
                output.Append("\\f");
            }
            else if (current == '\n')
            {
                output.Append("\\n");
            }
            else if (current == '\r')
            {
                output.Append("\\r");
            }
            else if (current == '\t')
            {
                output.Append("\\t");
            }
            else if (current >= 1 && current <= 31 || current >= 127 && current <= 159)
            {
                output.Append("\\u");
                output.Append(((int)current).ToString("X4"));
            }
            else
            {
                output.Append(current);
            }

            index = index + 1;
        }
    }

    internal int ReadXPathJsonXmlEscapeLength(string value, int index)
    {
        if (index + 1 >= value.Length)
        {
            throw CreateXPathJsonXmlError("An escaped JSON value ends with a backslash.", XPathErrorCodes.FOJS0007);
        }

        char next = value[index + 1];
        if (next == '"' || next == '\\' || next == '/' || next == 'b' || next == 'f' || next == 'n' || next == 'r' || next == 't')
        {
            return 2;
        }

        if (next != 'u' || index + 6 > value.Length)
        {
            throw CreateXPathJsonXmlError("An escaped JSON value contains an invalid escape sequence.", XPathErrorCodes.FOJS0007);
        }

        int offset = index + 2;
        while (offset < index + 6)
        {
            if (ReadXPathJsonXmlHexDigit(value[offset]) < 0)
            {
                throw CreateXPathJsonXmlError("A Unicode escape contains a non-hexadecimal digit.", XPathErrorCodes.FOJS0007);
            }

            offset = offset + 1;
        }

        return 6;
    }

    internal string DecodeXPathJsonXmlEscapedKey(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] != '\\')
            {
                result.Append(value[index]);
                index = index + 1;
                continue;
            }

            int length = ReadXPathJsonXmlEscapeLength(value, index);
            char next = value[index + 1];
            if (length == 6)
            {
                int code = 0;
                int offset = index + 2;
                while (offset < index + 6)
                {
                    code = code * 16 + ReadXPathJsonXmlHexDigit(value[offset]);
                    offset = offset + 1;
                }

                result.Append((char)code);
            }
            else if (next == 'b')
            {
                result.Append('\b');
            }
            else if (next == 'f')
            {
                result.Append('\f');
            }
            else if (next == 'n')
            {
                result.Append('\n');
            }
            else if (next == 'r')
            {
                result.Append('\r');
            }
            else if (next == 't')
            {
                result.Append('\t');
            }
            else
            {
                result.Append(next);
            }

            index = index + length;
        }

        return result.ToString();
    }

    internal int ReadXPathJsonXmlHexDigit(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        return -1;
    }

    internal bool IsXPathJsonXmlBooleanLexical(string? value)
    {
        XdmNumericParseResult parsed = new XdmNumericDatatype().TryParseXdmBoolean(value == null ? "" : value);
        return parsed.Success;
    }

    internal bool ContainsXPathJsonXmlKey(List<string> keys, string sought)
    {
        int index = 0;
        while (index < keys.Count)
        {
            if (string.Equals(keys[index], sought, StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AppendXPathJsonXmlSeparator(StringBuilder output, XPathJsonOptions options, int depth)
    {
        if (!options.Indent)
        {
            return;
        }

        output.Append('\n');
        int index = 0;
        while (index < depth)
        {
            output.Append("  ");
            index = index + 1;
        }
    }

    internal XPathDynamicException CreateXPathJsonXmlError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = Range;
        return new XPathDynamicException(message, diagnostic, Range.Start);
    }

    internal XPathTypeException CreateXPathJsonXmlTypeError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XPathErrorCodes.XPTY0004;
        diagnostic.Category = XmlErrorCategory.XPathType;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = Range;
        return new XPathTypeException(message, diagnostic, Range.Start);
    }
}
