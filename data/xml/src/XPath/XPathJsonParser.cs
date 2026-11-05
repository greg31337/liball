using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace xml;

internal class XPathJsonParser
{
    internal string Text;
    internal int Position;
    internal XPathJsonOptions Options;
    internal XPathDynamicContext DynamicContext;
    internal XPathStaticContext StaticContext;
    internal SourceRange Range;
    internal XdmFactory Factory;
    internal XdmSequenceOperations Sequences;
    internal XdmMapOperations Maps;
    internal XdmArrayOperations Arrays;
    internal XdmFunctionOperations Functions;
    internal XdmTypeConverter Converter;
    internal XdmTypeSystem Types;
    internal XmlDocument XmlDocument;
    internal TreeMutation XmlMutation;

    internal XPathJsonParser(string text, XPathJsonOptions options, XPathDynamicContext dynamicContext, XPathStaticContext staticContext, SourceRange range)
    {
        Text = text;
        Options = options;
        DynamicContext = dynamicContext;
        StaticContext = staticContext;
        Range = range;
        Factory = new XdmFactory();
        Sequences = new XdmSequenceOperations();
        Maps = new XdmMapOperations();
        Arrays = new XdmArrayOperations();
        Functions = new XdmFunctionOperations();
        Converter = new XdmTypeConverter();
        Types = new XdmTypeSystem();
        XmlDocument = new XmlDocument();
        XmlDocument.Version = XmlVersion.Xml10;
        XmlDocument.BaseUri = staticContext.BaseUri;
        XmlMutation = new TreeMutation();
        XmlNode container = XmlMutation.CreateXmlTreeNodeOperation(XmlDocument, XmlNodeKind.Document, null, null);
        XmlDocument.DocumentNode = container;
    }

    internal XdmSequence ParseXPathJsonText()
    {
        if (Position < Text.Length && Text[Position] == '\uFEFF')
        {
            Position = Position + 1;
        }

        SkipXPathJsonWhitespace();
        if (Position >= Text.Length)
        {
            throw CreateXPathJsonParseError("The JSON text is empty.", XPathErrorCodes.FOJS0001);
        }

        XdmSequence result = ParseXPathJsonValue(0);
        SkipXPathJsonWhitespace();
        if (Position != Text.Length)
        {
            throw CreateXPathJsonParseError("Unexpected content follows the JSON value.", XPathErrorCodes.FOJS0001);
        }

        if (Options.XmlOutput)
        {
            XmlNode? root = result.Nodes == null || result.Nodes.Length == 0 ? null : result.Nodes[0];
            if (root == null || XmlDocument.DocumentNode == null)
            {
                throw new InvalidOperationException("JSON-to-XML parsing did not create a root element.");
            }

            XmlNamespaceBinding binding = new XmlNamespaceBinding();
            binding.Prefix = "";
            binding.NamespaceUri = XPathNamespaces.Functions;
            binding.IsUndeclaration = false;
            root.NamespaceBindings = [binding];
            XmlMutation.AppendXmlTreeChildOperation(XmlDocument.DocumentNode, root);
            return Sequences.CreateXdmNodeSequence(XmlDocument.DocumentNode);
        }

        return result;
    }

    internal XdmSequence ParseXPathJsonValue(int depth)
    {
        if (depth > 512)
        {
            throw CreateXPathJsonParseError("The JSON value is nested too deeply.", XPathErrorCodes.FOJS0001);
        }

        SkipXPathJsonWhitespace();
        if (Position >= Text.Length)
        {
            throw CreateXPathJsonParseError("A JSON value was expected.", XPathErrorCodes.FOJS0001);
        }

        char current = Text[Position];
        if (current == '{')
        {
            return ParseXPathJsonObject(depth + 1);
        }

        if (current == '[')
        {
            return ParseXPathJsonArray(depth + 1);
        }

        if (current == '"' || Options.Liberal && current == '\'')
        {
            string value = ParseXPathJsonString();
            if (Options.XmlOutput)
            {
                return CreateXPathJsonXmlValue("string", value);
            }

            return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmStringAtomicValue(value, XdmAtomicType.String));
        }

        if (current == 't')
        {
            RequireXPathJsonLiteral("true");
            if (Options.XmlOutput)
            {
                return CreateXPathJsonXmlValue("boolean", "true");
            }

            return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmBooleanAtomicValue("true"));
        }

        if (current == 'f')
        {
            RequireXPathJsonLiteral("false");
            if (Options.XmlOutput)
            {
                return CreateXPathJsonXmlValue("boolean", "false");
            }

            return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmBooleanAtomicValue("false"));
        }

        if (current == 'n')
        {
            RequireXPathJsonLiteral("null");
            if (Options.XmlOutput)
            {
                return CreateXPathJsonXmlValue("null", "");
            }

            return Sequences.CreateEmptyXdmSequence();
        }

        if (current == '-' || current >= '0' && current <= '9')
        {
            return ParseXPathJsonNumber();
        }

        throw CreateXPathJsonParseError("A JSON value was expected.", XPathErrorCodes.FOJS0001);
    }

    internal XdmSequence ParseXPathJsonObject(int depth)
    {
        Position = Position + 1;
        SkipXPathJsonWhitespace();
        List<XdmMapEntry> entries = new List<XdmMapEntry>();
        List<XmlNode> xmlChildren = new List<XmlNode>();
        List<string> keys = new List<string>();
        if (ConsumeXPathJsonCharacter('}'))
        {
            if (Options.XmlOutput)
            {
                return CreateXPathJsonXmlContainer("map", xmlChildren);
            }

            return Sequences.CreateXdmMapSequence(Maps.CreateEmptyXdmMap());
        }

        bool complete = false;
        while (!complete)
        {
            SkipXPathJsonWhitespace();
            string key;
            if (Position < Text.Length && (Text[Position] == '"' || Options.Liberal && Text[Position] == '\''))
            {
                key = ParseXPathJsonString();
            }
            else if (Options.Liberal)
            {
                key = ParseXPathJsonBareKey();
            }
            else
            {
                throw CreateXPathJsonParseError("A JSON object key must be a quoted string.", XPathErrorCodes.FOJS0001);
            }

            SkipXPathJsonWhitespace();
            RequireXPathJsonCharacter(':');
            XdmSequence value = ParseXPathJsonValue(depth);
            int duplicate = FindXPathJsonStringKey(keys, key);
            if (duplicate >= 0)
            {
                if (Options.Duplicates == "reject")
                {
                    throw CreateXPathJsonParseError("The JSON object contains a duplicate key.", XPathErrorCodes.FOJS0003);
                }

                if (Options.Duplicates == "use-last")
                {
                    XdmMapEntry replacement = Maps.CreateXdmMapEntry(Factory.CreateXdmStringAtomicValue(key, XdmAtomicType.String), value);
                    entries[duplicate] = replacement;
                }
            }
            else
            {
                keys.Add(key);
            }

            if (Options.XmlOutput)
            {
                XmlNode child = ReadXPathJsonXmlNode(value);
                if (duplicate < 0 || Options.Duplicates == "retain")
                {
                    AttachXPathJsonXmlKey(child, key);
                    xmlChildren.Add(child);
                }
            }
            else if (duplicate < 0)
            {
                entries.Add(Maps.CreateXdmMapEntry(Factory.CreateXdmStringAtomicValue(key, XdmAtomicType.String), value));
            }

            SkipXPathJsonWhitespace();
            if (ConsumeXPathJsonCharacter('}'))
            {
                complete = true;
            }
            else
            {
                RequireXPathJsonCharacter(',');
                SkipXPathJsonWhitespace();
                if (Options.Liberal && ConsumeXPathJsonCharacter('}'))
                {
                    complete = true;
                }
            }
        }

        if (Options.XmlOutput)
        {
            return CreateXPathJsonXmlContainer("map", xmlChildren);
        }

        return Sequences.CreateXdmMapSequence(Maps.CreateXdmMap(entries.ToArray()));
    }

    internal XdmSequence ParseXPathJsonArray(int depth)
    {
        Position = Position + 1;
        SkipXPathJsonWhitespace();
        List<XdmSequence> members = new List<XdmSequence>();
        List<XmlNode> xmlChildren = new List<XmlNode>();
        if (ConsumeXPathJsonCharacter(']'))
        {
            if (Options.XmlOutput)
            {
                return CreateXPathJsonXmlContainer("array", xmlChildren);
            }

            return Sequences.CreateXdmArraySequence(Arrays.CreateEmptyXdmArray());
        }

        bool complete = false;
        while (!complete)
        {
            XdmSequence member = ParseXPathJsonValue(depth);
            if (Options.XmlOutput)
            {
                xmlChildren.Add(ReadXPathJsonXmlNode(member));
            }
            else
            {
                members.Add(member);
            }
            SkipXPathJsonWhitespace();
            if (ConsumeXPathJsonCharacter(']'))
            {
                complete = true;
            }
            else
            {
                RequireXPathJsonCharacter(',');
                SkipXPathJsonWhitespace();
                if (Options.Liberal && ConsumeXPathJsonCharacter(']'))
                {
                    complete = true;
                }
            }
        }

        if (Options.XmlOutput)
        {
            return CreateXPathJsonXmlContainer("array", xmlChildren);
        }

        return Sequences.CreateXdmArraySequence(Arrays.CreateXdmArray(members.ToArray()));
    }

    internal string ParseXPathJsonString()
    {
        char delimiter = Text[Position];
        Position = Position + 1;
        StringBuilder result = new StringBuilder();
        while (Position < Text.Length)
        {
            char current = Text[Position];
            Position = Position + 1;
            if (current == delimiter)
            {
                return result.ToString();
            }

            if (current == '\\')
            {
                AppendXPathJsonEscape(result);
            }
            else
            {
                if (current < 0x20 && !Options.Liberal)
                {
                    throw CreateXPathJsonParseError("A JSON string contains an unescaped control character.", XPathErrorCodes.FOJS0001);
                }

                AppendXPathJsonDirectCharacter(result, current);
            }
        }

        throw CreateXPathJsonParseError("A JSON string is not terminated.", XPathErrorCodes.FOJS0001);
    }

    internal void AppendXPathJsonEscape(StringBuilder result)
    {
        if (Position >= Text.Length)
        {
            throw CreateXPathJsonParseError("A JSON escape sequence is incomplete.", XPathErrorCodes.FOJS0001);
        }

        char escaped = Text[Position];
        Position = Position + 1;
        if (escaped == '"' || escaped == '/' || escaped == '\\' || Options.Liberal && escaped == '\'')
        {
            AppendXPathJsonCodePoint(result, escaped, "\\" + escaped);
            return;
        }

        if (escaped == 'b')
        {
            AppendXPathJsonCodePoint(result, 0x08, "\\b");
            return;
        }

        if (escaped == 'f')
        {
            AppendXPathJsonCodePoint(result, 0x0C, "\\f");
            return;
        }

        if (escaped == 'n')
        {
            AppendXPathJsonCodePoint(result, 0x0A, "\\n");
            return;
        }

        if (escaped == 'r')
        {
            AppendXPathJsonCodePoint(result, 0x0D, "\\r");
            return;
        }

        if (escaped == 't')
        {
            AppendXPathJsonCodePoint(result, 0x09, "\\t");
            return;
        }

        if (escaped != 'u')
        {
            throw CreateXPathJsonParseError("The JSON text contains an invalid escape sequence.", XPathErrorCodes.FOJS0001);
        }

        int first = ParseXPathJsonHexQuad();
        string firstEscape = FormatXPathJsonUnicodeEscape(first);
        if (first >= 0xD800 && first <= 0xDBFF)
        {
            int saved = Position;
            if (Position + 1 < Text.Length && Text[Position] == '\\' && Text[Position + 1] == 'u')
            {
                Position = Position + 2;
                int second = ParseXPathJsonHexQuad();
                if (second >= 0xDC00 && second <= 0xDFFF)
                {
                    int codePoint = 0x10000 + (first - 0xD800) * 0x400 + second - 0xDC00;
                    AppendXPathJsonCodePoint(result, codePoint, firstEscape + FormatXPathJsonUnicodeEscape(second));
                    return;
                }

                Position = saved;
            }
        }

        AppendXPathJsonCodePoint(result, first, firstEscape);
    }

    internal void AppendXPathJsonDirectCharacter(StringBuilder result, char current)
    {
        if (current >= 0xD800 && current <= 0xDBFF)
        {
            if (Position < Text.Length && Text[Position] >= 0xDC00 && Text[Position] <= 0xDFFF)
            {
                char second = Text[Position];
                Position = Position + 1;
                int codePoint = 0x10000 + (current - 0xD800) * 0x400 + second - 0xDC00;
                AppendXPathJsonCodePoint(result, codePoint, FormatXPathJsonUnicodeEscape(current) + FormatXPathJsonUnicodeEscape(second));
                return;
            }

            AppendXPathJsonCodePoint(result, current, FormatXPathJsonUnicodeEscape(current));
            return;
        }

        AppendXPathJsonCodePoint(result, current, FormatXPathJsonUnicodeEscape(current));
    }

    internal void AppendXPathJsonCodePoint(StringBuilder result, int codePoint, string escaped)
    {
        bool validXml = IsXPathJsonXmlCharacter(codePoint);
        bool special = codePoint <= 0x1F || codePoint >= 0x7F && codePoint <= 0x9F || codePoint == 0x5C || !validXml;
        if (Options.Escape && special)
        {
            result.Append(FormatXPathJsonSpecialEscape(codePoint, escaped));
            return;
        }

        if (!validXml)
        {
            result.Append(InvokeXPathJsonFallback(escaped));
            return;
        }

        result.Append(char.ConvertFromUtf32(codePoint));
    }

    internal string InvokeXPathJsonFallback(string escaped)
    {
        if (Options.Fallback == null)
        {
            return "\uFFFD";
        }

        XdmAtomicValue argument = Factory.CreateXdmStringAtomicValue(escaped, XdmAtomicType.String);
        XdmSequence supplied = Sequences.CreateXdmAtomicSequence(argument);
        XdmSequence returned = Functions.InvokeXdmFunctionItem(Options.Fallback, [supplied], DynamicContext, StaticContext);
        if (returned.Count != 1)
        {
            throw CreateXPathJsonTypeError("The JSON fallback function must return one string.");
        }

        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(returned, 0).AtomicValue;
        if (value == null || !Types.IsXdmStringDerivedAtomicType(value.Type))
        {
            throw CreateXPathJsonTypeError("The JSON fallback function must return one string.");
        }

        return Converter.ReadXdmAtomicStringValue(value);
    }

    internal XdmSequence ParseXPathJsonNumber()
    {
        int start = Position;
        if (Text[Position] == '-')
        {
            Position = Position + 1;
        }

        if (Position >= Text.Length || Text[Position] < '0' || Text[Position] > '9')
        {
            throw CreateXPathJsonParseError("A JSON number requires an integer part.", XPathErrorCodes.FOJS0001);
        }

        if (Text[Position] == '0')
        {
            Position = Position + 1;
            if (!Options.Liberal && Position < Text.Length && Text[Position] >= '0' && Text[Position] <= '9')
            {
                throw CreateXPathJsonParseError("A JSON number cannot contain a leading zero.", XPathErrorCodes.FOJS0001);
            }
        }

        while (Position < Text.Length && Text[Position] >= '0' && Text[Position] <= '9')
        {
            Position = Position + 1;
        }

        if (Position < Text.Length && Text[Position] == '.')
        {
            Position = Position + 1;
            int fractionStart = Position;
            while (Position < Text.Length && Text[Position] >= '0' && Text[Position] <= '9')
            {
                Position = Position + 1;
            }

            if (Position == fractionStart)
            {
                throw CreateXPathJsonParseError("A JSON number requires digits after its decimal point.", XPathErrorCodes.FOJS0001);
            }
        }

        if (Position < Text.Length && (Text[Position] == 'e' || Text[Position] == 'E'))
        {
            Position = Position + 1;
            if (Position < Text.Length && (Text[Position] == '+' || Text[Position] == '-'))
            {
                Position = Position + 1;
            }

            int exponentStart = Position;
            while (Position < Text.Length && Text[Position] >= '0' && Text[Position] <= '9')
            {
                Position = Position + 1;
            }

            if (Position == exponentStart)
            {
                throw CreateXPathJsonParseError("A JSON number requires exponent digits.", XPathErrorCodes.FOJS0001);
            }
        }

        string lexical = Text.Substring(start, Position - start);
        if (Options.XmlOutput)
        {
            return CreateXPathJsonXmlValue("number", lexical);
        }

        if (Options.NumberParser == null)
        {
            XdmAtomicValue number = Factory.CreateXdmFloatingAtomicValue(lexical, XdmAtomicType.Double);
            return Sequences.CreateXdmAtomicSequence(number);
        }

        XdmAtomicValue input = Factory.CreateXdmStringAtomicValue(lexical, XdmAtomicType.String);
        XdmSequence returned = Functions.InvokeXdmFunctionItem(Options.NumberParser, [Sequences.CreateXdmAtomicSequence(input)], DynamicContext, StaticContext);
        if (returned.Count != 1)
        {
            throw CreateXPathJsonTypeError("The JSON number-parser function must return one numeric value.");
        }

        XdmAtomicValue? parsed = Sequences.ReadXdmSequenceItem(returned, 0).AtomicValue;
        if (parsed == null || !Types.IsXdmNumericAtomicType(parsed.Type))
        {
            throw CreateXPathJsonTypeError("The JSON number-parser function must return one numeric value.");
        }

        return returned;
    }

    internal int ParseXPathJsonHexQuad()
    {
        if (Position + 4 > Text.Length)
        {
            throw CreateXPathJsonParseError("A JSON Unicode escape sequence is incomplete.", XPathErrorCodes.FOJS0001);
        }

        int value = 0;
        int count = 0;
        while (count < 4)
        {
            char current = Text[Position];
            int digit;
            if (current >= '0' && current <= '9')
            {
                digit = current - '0';
            }
            else if (current >= 'a' && current <= 'f')
            {
                digit = current - 'a' + 10;
            }
            else if (current >= 'A' && current <= 'F')
            {
                digit = current - 'A' + 10;
            }
            else
            {
                throw CreateXPathJsonParseError("A JSON Unicode escape sequence contains a non-hexadecimal digit.", XPathErrorCodes.FOJS0001);
            }

            value = value * 16 + digit;
            Position = Position + 1;
            count = count + 1;
        }

        return value;
    }

    internal string ParseXPathJsonBareKey()
    {
        int start = Position;
        while (Position < Text.Length)
        {
            char current = Text[Position];
            bool valid = current >= 'a' && current <= 'z' || current >= 'A' && current <= 'Z' || current >= '0' && current <= '9' || current == '_' || current == '-' || current == '$';
            if (!valid)
            {
                break;
            }

            Position = Position + 1;
        }

        if (Position == start)
        {
            throw CreateXPathJsonParseError("A JSON object key was expected.", XPathErrorCodes.FOJS0001);
        }

        return Text.Substring(start, Position - start);
    }

    internal XdmSequence CreateXPathJsonXmlValue(string localName, string value)
    {
        XmlNode element = CreateXPathJsonXmlElement(localName);
        if (value.Length > 0)
        {
            XmlNode text = XmlMutation.CreateXmlTreeNodeOperation(XmlDocument, XmlNodeKind.Text, null, value);
            XmlMutation.AppendXmlTreeChildOperation(element, text);
        }

        if (localName == "string" && Options.Escape && value.IndexOf('\\') >= 0)
        {
            SetXPathJsonXmlAttribute(element, "escaped", "true", XdmAtomicType.Boolean);
        }

        if (Options.Validate)
        {
            AnnotateXPathJsonXmlElement(element, localName + "Type", localName, value);
        }

        return Sequences.CreateXdmNodeSequence(element);
    }

    internal XdmSequence CreateXPathJsonXmlContainer(string localName, List<XmlNode> children)
    {
        XmlNode element = CreateXPathJsonXmlElement(localName);
        int index = 0;
        while (index < children.Count)
        {
            XmlMutation.AppendXmlTreeChildOperation(element, children[index]);
            index = index + 1;
        }

        if (Options.Validate)
        {
            AnnotateXPathJsonXmlElement(element, localName + "Type", localName, "");
        }

        return Sequences.CreateXdmNodeSequence(element);
    }

    internal XmlNode CreateXPathJsonXmlElement(string localName)
    {
        XmlName name = CreateXPathJsonXmlName(localName, XPathNamespaces.Functions);
        return XmlMutation.CreateXmlTreeNodeOperation(XmlDocument, XmlNodeKind.Element, name, null);
    }

    internal XmlNode ReadXPathJsonXmlNode(XdmSequence sequence)
    {
        XmlNode? result = sequence.Count == 1 && sequence.Nodes != null && sequence.Nodes.Length == 1 ? sequence.Nodes[0] : null;
        return result == null ? throw new InvalidOperationException("JSON-to-XML parsing produced an invalid node value.") : result;
    }

    internal void AttachXPathJsonXmlKey(XmlNode element, string key)
    {
        SetXPathJsonXmlAttribute(element, "key", key, XdmAtomicType.String);
        if (Options.Escape && key.IndexOf('\\') >= 0)
        {
            SetXPathJsonXmlAttribute(element, "escaped-key", "true", XdmAtomicType.Boolean);
        }

        if (Options.Validate && element.Name != null && element.Name.LocalName != null)
        {
            string localName = element.Name.LocalName;
            AnnotateXPathJsonXmlElement(element, localName + "WithinMapType", localName, new TreeNavigation().GetXmlNodeStringValueOperation(element));
        }
    }

    internal void SetXPathJsonXmlAttribute(XmlNode element, string localName, string value, XdmAtomicType atomicType)
    {
        XmlName name = CreateXPathJsonXmlName(localName, null);
        XmlNode attribute = XmlMutation.CreateXmlTreeNodeOperation(XmlDocument, XmlNodeKind.Attribute, name, value);
        if (Options.Validate)
        {
            XmlSchemaTypeInfo type = new XmlSchemaTypeInfo();
            type.TypeName = CreateXPathJsonXmlName(atomicType == XdmAtomicType.Boolean ? "boolean" : "string", XPathNamespaces.XmlSchema);
            type.AtomicType = atomicType;
            type.Validity = XmlSchemaValidity.Valid;
            type.NormalizedValue = value;
            type.ActualValue = atomicType == XdmAtomicType.Boolean ? Factory.CreateXdmBooleanAtomicValue(value) : Factory.CreateXdmStringAtomicValue(value, XdmAtomicType.String);
            type.ActualValues = [type.ActualValue];
            attribute.TypeInfo = type;
        }

        new TreeContentMutation(XmlMutation).SetXmlTreeAttributeOperation(element, attribute);
    }

    internal void AnnotateXPathJsonXmlElement(XmlNode element, string typeLocalName, string valueKind, string value)
    {
        XmlSchemaTypeInfo type = new XmlSchemaTypeInfo();
        type.TypeName = CreateXPathJsonXmlName(typeLocalName, XPathNamespaces.Functions);
        type.Validity = XmlSchemaValidity.Valid;
        type.NormalizedValue = value;
        if (valueKind == "string")
        {
            type.AtomicType = XdmAtomicType.String;
            type.ActualValue = Factory.CreateXdmStringAtomicValue(value, XdmAtomicType.String);
        }
        else if (valueKind == "boolean")
        {
            type.AtomicType = XdmAtomicType.Boolean;
            type.ActualValue = Factory.CreateXdmBooleanAtomicValue(value);
        }
        else if (valueKind == "number")
        {
            type.AtomicType = XdmAtomicType.Double;
            type.ActualValue = Factory.CreateXdmFloatingAtomicValue(value, XdmAtomicType.Double);
        }

        if (type.ActualValue != null)
        {
            type.ActualValues = [type.ActualValue];
        }

        element.TypeInfo = type;
    }

    internal XmlName CreateXPathJsonXmlName(string localName, string? namespaceUri)
    {
        XmlName result = new XmlName();
        result.Prefix = null;
        result.LocalName = localName;
        result.NamespaceUri = namespaceUri;
        result.QualifiedName = localName;
        return result;
    }

    internal int FindXPathJsonStringKey(List<string> keys, string key)
    {
        int index = 0;
        while (index < keys.Count)
        {
            if (string.Equals(keys[index], key, StringComparison.Ordinal))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void RequireXPathJsonLiteral(string literal)
    {
        if (Position + literal.Length > Text.Length || Text.Substring(Position, literal.Length) != literal)
        {
            throw CreateXPathJsonParseError("The JSON text contains an invalid literal.", XPathErrorCodes.FOJS0001);
        }

        Position = Position + literal.Length;
    }

    internal bool ConsumeXPathJsonCharacter(char expected)
    {
        if (Position < Text.Length && Text[Position] == expected)
        {
            Position = Position + 1;
            return true;
        }

        return false;
    }

    internal void RequireXPathJsonCharacter(char expected)
    {
        if (!ConsumeXPathJsonCharacter(expected))
        {
            throw CreateXPathJsonParseError("The JSON text is missing a required '" + expected + "' character.", XPathErrorCodes.FOJS0001);
        }
    }

    internal void SkipXPathJsonWhitespace()
    {
        bool scanning = true;
        while (scanning)
        {
            scanning = false;
            while (Position < Text.Length && (Text[Position] == ' ' || Text[Position] == '\t' || Text[Position] == '\r' || Text[Position] == '\n'))
            {
                Position = Position + 1;
            }

            if (Options.Liberal && Position + 1 < Text.Length && Text[Position] == '/' && Text[Position + 1] == '/')
            {
                Position = Position + 2;
                while (Position < Text.Length && Text[Position] != '\r' && Text[Position] != '\n')
                {
                    Position = Position + 1;
                }

                scanning = true;
            }
            else if (Options.Liberal && Position + 1 < Text.Length && Text[Position] == '/' && Text[Position + 1] == '*')
            {
                Position = Position + 2;
                while (Position + 1 < Text.Length && !(Text[Position] == '*' && Text[Position + 1] == '/'))
                {
                    Position = Position + 1;
                }

                if (Position + 1 >= Text.Length)
                {
                    throw CreateXPathJsonParseError("A JSON comment is not terminated.", XPathErrorCodes.FOJS0001);
                }

                Position = Position + 2;
                scanning = true;
            }
        }
    }

    internal bool IsXPathJsonXmlCharacter(int codePoint)
    {
        return codePoint == 0x09 || codePoint == 0x0A || codePoint == 0x0D || codePoint >= 0x20 && codePoint <= 0xD7FF || codePoint >= 0xE000 && codePoint <= 0xFFFD || codePoint >= 0x10000 && codePoint <= 0x10FFFF;
    }

    internal string FormatXPathJsonSpecialEscape(int codePoint, string original)
    {
        if (codePoint == 0x08)
        {
            return "\\b";
        }

        if (codePoint == 0x09)
        {
            return "\\t";
        }

        if (codePoint == 0x0A)
        {
            return "\\n";
        }

        if (codePoint == 0x0C)
        {
            return "\\f";
        }

        if (codePoint == 0x0D)
        {
            return "\\r";
        }

        if (codePoint == 0x5C)
        {
            return "\\\\";
        }

        if (codePoint <= 0xFFFF)
        {
            return FormatXPathJsonUnicodeEscape(codePoint);
        }

        return original;
    }

    internal string FormatXPathJsonUnicodeEscape(int codePoint)
    {
        return "\\u" + codePoint.ToString("X4", CultureInfo.InvariantCulture);
    }

    internal XPathDynamicException CreateXPathJsonParseError(string message, string code)
    {
        string detailed = message + " (character offset " + Position.ToString(CultureInfo.InvariantCulture) + ").";
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = detailed;
        diagnostic.Range = Range;
        return new XPathDynamicException(detailed, diagnostic, Range.Start);
    }

    internal XPathTypeException CreateXPathJsonTypeError(string message)
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
