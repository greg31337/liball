using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace xml;

internal class XPathParsingSerializationFunctionImplementation : IXPathFunction
{
    internal string LocalName;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmMapOperations Maps;
    internal XdmFactory Factory;

    internal XPathParsingSerializationFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext)
    {
        LocalName = localName;
        Range = range;
        StaticContext = staticContext;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Maps = new XdmMapOperations();
        Factory = new XdmFactory();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        if (LocalName == "serialize")
        {
            return SerializeXPathSequence(arguments);
        }

        return ParseXPathXml(arguments[0], context);
    }

    internal XdmSequence ParseXPathXml(XdmSequence supplied, XPathDynamicContext context)
    {
        string? source = ReadXPathOptionalString(supplied);
        if (source == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = source;
        input.BaseUri = StaticContext.BaseUri;
        XmlParserOptions options = CreateXPathParserOptions(context);
        bool fragment = LocalName == "parse-xml-fragment";
        Task<XmlDocument> pending = Task.Run(() => fragment ? new XmlParser().ParseXmlFragment(input, options, context.ResourceResolver, null, context.Cancellation) : new XmlParser().ParseXmlDocument(input, options, context.ResourceResolver, null, context.Cancellation));
        ((System.IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion || pending.Result.DocumentNode == null)
        {
            throw CreateXPathParsingError("The supplied string is not well-formed XML.", XPathErrorCodes.FODC0006);
        }

        pending.Result.DocumentUri = null;
        pending.Result.BaseUri = StaticContext.BaseUri;
        return Sequences.CreateXdmNodeSequence(pending.Result.DocumentNode);
    }

    internal XmlParserOptions CreateXPathParserOptions(XPathDynamicContext context)
    {
        XmlParserOptions result = new XmlParserOptions();
        result.Version = XmlVersion.Xml10;
        result.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        result.DtdProcessing = XmlDtdProcessing.Parse;
        result.EntityHandling = XmlEntityHandling.Expand;
        result.ValidationMode = XmlValidationMode.None;
        result.PreserveCData = false;
        result.PreserveEntityReferences = false;
        result.PreserveComments = true;
        result.PreserveProcessingInstructions = true;
        result.PreservePrefixes = true;
        result.PreserveIgnorableWhitespace = true;
        result.RetainNamespaceDeclarations = true;
        result.SecurityLimits = context.SecurityLimits;
        result.ResolverPolicy = context.ResolverPolicy;
        return result;
    }

    internal XdmSequence SerializeXPathSequence(XdmSequence[] arguments)
    {
        XmlSerializationOptions options = CreateXPathSerializationDefaults();
        if (arguments.Length == 2 && arguments[1].Count != 0)
        {
            ApplyXPathSerializationParameters(options, arguments[1]);
        }

        MemoryStream stream = new MemoryStream();
        XmlOutput output = new XmlOutput();
        output.Kind = XmlOutputKind.Stream;
        output.ByteStream = stream;
        output.EncodingName = options.Encoding;
        new XmlSerializer().SerializeXdmSequence(arguments[0], output, options);
        Encoding encoding = EncodingDetector.ResolveXmlEncodingName(options.Encoding == null ? "utf-8" : options.Encoding, EncodingPolicyKind.BuiltInText);
        byte[] bytes = stream.ToArray();
        int preambleLength = EncodingDetector.GetByteOrderMarkLength(EncodingDetector.DetectByteOrderMark(bytes));
        string serialized = encoding.GetString(bytes, preambleLength, bytes.Length - preambleLength);
        XdmAtomicValue value = Factory.CreateXdmStringAtomicValue(serialized, XdmAtomicType.String);
        return Sequences.CreateXdmAtomicSequence(value);
    }

    internal XmlSerializationOptions CreateXPathSerializationDefaults()
    {
        XmlSerializationOptions result = new XmlSerializationOptions();
        result.Method = XmlSerializationMethod.Xml;
        result.AllowDuplicateNames = false;
        result.ByteOrderMark = false;
        result.Encoding = "utf-8";
        result.EscapeUriAttributes = true;
        result.HtmlVersion = "5";
        result.IncludeContentType = true;
        result.Indent = false;
        result.JsonNodeOutputMethod = XmlSerializationMethod.Xml;
        result.NormalizationForm = "none";
        result.OmitXmlDeclaration = true;
        result.Standalone = XmlStandalone.Unspecified;
        result.UndeclarePrefixes = false;
        result.Version = "1.0";
        result.InvalidCharacterPolicy = XmlInvalidCharacterPolicy.Error;
        return result;
    }

    internal void ApplyXPathSerializationParameters(XmlSerializationOptions options, XdmSequence supplied)
    {
        if (supplied.Count != 1)
        {
            throw CreateXPathSerializationTypeError("Serialization parameters must be one map or serialization-parameters element.");
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.Kind == XdmItemKind.Map && item.Map != null)
        {
            ApplyXPathSerializationMap(options, item.Map);
            return;
        }

        if (item.Kind == XdmItemKind.Node && item.Node != null)
        {
            if (!IsXPathSerializationParameterElement(item.Node))
            {
                throw CreateXPathSerializationTypeError("The serialization parameter element has an invalid name.");
            }

            ApplyXPathSerializationElement(options, item.Node);
            return;
        }

        throw CreateXPathSerializationTypeError("Serialization parameters must be one map or serialization-parameters element.");
    }

    internal void ApplyXPathSerializationMap(XmlSerializationOptions options, XdmMapValue map)
    {
        Maps.ValidateXdmMapStorage(map);
        XdmMapEntry[] entries = map.Entries == null ? [] : map.Entries;
        int index = 0;
        while (index < entries.Length)
        {
            XdmAtomicValue? key = entries[index].Key;
            XdmSequence? value = entries[index].Value;
            if (key == null || value == null)
            {
                throw CreateXPathSerializationTypeError("A serialization parameter map entry is incomplete.");
            }

            if (Maps.IsXdmStringMapKey(key.Type))
            {
                ApplyXPathSerializationMapEntry(options, Converter.ReadXdmAtomicStringValue(key), value);
            }
            else if (key.Type == XdmAtomicType.QName && key.NameValue != null)
            {
            }
            else
            {
                throw CreateXPathSerializationTypeError("Serialization parameter keys must be strings or namespace-qualified QNames.");
            }

            index = index + 1;
        }
    }

    internal void ApplyXPathSerializationMapEntry(XmlSerializationOptions options, string name, XdmSequence value)
    {
        if (name == "method")
        {
            SetXPathSerializationMethod(options, value, false);
        }
        else if (name == "json-node-output-method")
        {
            SetXPathSerializationMethod(options, value, true);
        }
        else if (name == "allow-duplicate-names")
        {
            options.AllowDuplicateNames = ReadXPathSerializationBoolean(value, false);
        }
        else if (name == "byte-order-mark")
        {
            options.ByteOrderMark = ReadXPathSerializationBoolean(value, false);
        }
        else if (name == "escape-uri-attributes")
        {
            options.EscapeUriAttributes = ReadXPathSerializationBoolean(value, true);
        }
        else if (name == "include-content-type")
        {
            options.IncludeContentType = ReadXPathSerializationBoolean(value, true);
        }
        else if (name == "indent")
        {
            options.Indent = ReadXPathSerializationBoolean(value, false);
        }
        else if (name == "omit-xml-declaration")
        {
            options.OmitXmlDeclaration = ReadXPathSerializationBoolean(value, true);
        }
        else if (name == "undeclare-prefixes")
        {
            options.UndeclarePrefixes = ReadXPathSerializationBoolean(value, false);
        }
        else if (name == "standalone")
        {
            options.Standalone = value.Count == 0 ? XmlStandalone.Unspecified : ReadXPathSerializationBoolean(value, false) ? XmlStandalone.Yes : XmlStandalone.No;
        }
        else if (name == "cdata-section-elements")
        {
            options.CDataSectionElements = ReadXPathSerializationQNames(value);
        }
        else if (name == "suppress-indentation")
        {
            options.SuppressIndentation = ReadXPathSerializationQNames(value);
        }
        else if (name == "use-character-maps")
        {
            options.UseCharacterMaps = ReadXPathSerializationCharacterMap(value);
        }
        else if (name == "doctype-public")
        {
            options.DoctypePublic = ReadXPathSerializationString(value, null);
        }
        else if (name == "doctype-system")
        {
            options.DoctypeSystem = ReadXPathSerializationString(value, null);
        }
        else if (name == "encoding")
        {
            options.Encoding = ReadXPathSerializationString(value, "utf-8");
        }
        else if (name == "html-version")
        {
            options.HtmlVersion = ReadXPathSerializationDecimal(value, "5");
        }
        else if (name == "item-separator")
        {
            options.ItemSeparator = ReadXPathSerializationString(value, null);
        }
        else if (name == "media-type")
        {
            options.MediaType = ReadXPathSerializationString(value, null);
        }
        else if (name == "normalization-form")
        {
            options.NormalizationForm = ReadXPathSerializationString(value, "none");
        }
        else if (name == "version")
        {
            options.Version = ReadXPathSerializationString(value, "1.0");
        }
    }

    internal bool IsXPathSerializationParameterElement(XmlNode node)
    {
        XmlName? name = node.Name;
        return node.Kind == XmlNodeKind.Element && name != null && name.LocalName == "serialization-parameters" && name.NamespaceUri == "http://www.w3.org/2010/xslt-xquery-serialization";
    }

    internal void ApplyXPathSerializationElement(XmlSerializationOptions options, XmlNode element)
    {
        RequireXPathSerializationAttributes(element, []);
        XmlNode[] children = element.Children == null ? [] : element.Children;
        List<string> names = new List<string>();
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Text && (child.Value == null || child.Value.Trim().Length == 0))
            {
                index = index + 1;
                continue;
            }

            if (child.Kind != XmlNodeKind.Element || child.Name == null || child.Name.LocalName == null)
            {
                throw CreateXPathParsingError("A serialization parameter element contains invalid content.", XmlErrorCodes.SEPM0017);
            }

            string namespaceUri = child.Name.NamespaceUri == null ? "" : child.Name.NamespaceUri;
            string expandedName = namespaceUri + "|" + child.Name.LocalName;
            if (names.Contains(expandedName))
            {
                throw CreateXPathParsingError("A serialization parameter occurs more than once.", XmlErrorCodes.SEPM0019);
            }

            names.Add(expandedName);
            if (namespaceUri != "http://www.w3.org/2010/xslt-xquery-serialization")
            {
                if (namespaceUri.Length == 0)
                {
                    throw CreateXPathParsingError("A serialization parameter is in no namespace.", XmlErrorCodes.SEPM0017);
                }

                index = index + 1;
                continue;
            }

            if (child.Name.LocalName == "use-character-maps")
            {
                options.UseCharacterMaps = ReadXPathSerializationElementCharacterMap(child);
            }
            else
            {
                if (!IsXPathStandardSerializationParameterName(child.Name.LocalName))
                {
                    throw CreateXPathParsingError("A serialization parameter name is not recognized.", XmlErrorCodes.SEPM0017);
                }

                RequireXPathSerializationAttributes(child, ["value"]);
                string? lexical = ReadXPathSerializationValueAttribute(child);
                if (lexical == null)
                {
                    throw CreateXPathParsingError("A serialization parameter element requires a value attribute.", XmlErrorCodes.SEPM0017);
                }

                ApplyXPathSerializationLexicalEntry(options, child.Name.LocalName, lexical);
            }

            index = index + 1;
        }
    }

    internal bool IsXPathStandardSerializationParameterName(string name)
    {
        return name == "method" || name == "json-node-output-method" || name == "allow-duplicate-names" || name == "byte-order-mark" || name == "escape-uri-attributes" || name == "include-content-type" || name == "indent" || name == "omit-xml-declaration" || name == "undeclare-prefixes" || name == "standalone" || name == "cdata-section-elements" || name == "suppress-indentation" || name == "doctype-public" || name == "doctype-system" || name == "encoding" || name == "html-version" || name == "item-separator" || name == "media-type" || name == "normalization-form" || name == "version";
    }

    internal void RequireXPathSerializationAttributes(XmlNode element, string[] allowed)
    {
        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlName? name = attributes[index].Name;
            string namespaceUri = name == null || name.NamespaceUri == null ? "" : name.NamespaceUri;
            string localName = name == null || name.LocalName == null ? "" : name.LocalName;
            if (namespaceUri != XmlNamespaces.Xmlns && (namespaceUri.Length != 0 || !ContainsXPathSerializationName(allowed, localName)))
            {
                throw CreateXPathParsingError("A serialization parameter element has an invalid attribute.", XmlErrorCodes.SEPM0017);
            }

            index = index + 1;
        }
    }

    internal bool ContainsXPathSerializationName(string[] names, string name)
    {
        int index = 0;
        while (index < names.Length)
        {
            if (names[index] == name)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XmlCharacterMapEntry[] ReadXPathSerializationElementCharacterMap(XmlNode element)
    {
        RequireXPathSerializationAttributes(element, []);
        List<XmlCharacterMapEntry> result = new List<XmlCharacterMapEntry>();
        XmlNode[] children = element.Children == null ? [] : element.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Text && (child.Value == null || child.Value.Trim().Length == 0))
            {
                index = index + 1;
                continue;
            }

            if (child.Kind != XmlNodeKind.Element || child.Name == null || child.Name.LocalName != "character-map" || child.Name.NamespaceUri != "http://www.w3.org/2010/xslt-xquery-serialization")
            {
                throw CreateXPathParsingError("A character map contains an invalid child.", XmlErrorCodes.SEPM0017);
            }

            RequireXPathSerializationAttributes(child, ["character", "map-string"]);
            string? character = ReadXPathSerializationNamedAttribute(child, "character");
            string? replacement = ReadXPathSerializationNamedAttribute(child, "map-string");
            if (character == null || replacement == null)
            {
                throw CreateXPathParsingError("A character map entry requires character and map-string attributes.", XmlErrorCodes.SEPM0017);
            }

            int codepoint = character.Length == 1 ? character[0] : character.Length == 2 && char.IsSurrogatePair(character, 0) ? char.ConvertToUtf32(character, 0) : -1;
            if (codepoint < 0)
            {
                throw CreateXPathParsingError("A character map key must contain one Unicode character.", XmlErrorCodes.SEPM0017);
            }

            int duplicateIndex = 0;
            while (duplicateIndex < result.Count && result[duplicateIndex].CodePoint != (uint)codepoint)
            {
                duplicateIndex = duplicateIndex + 1;
            }

            if (duplicateIndex < result.Count)
            {
                throw CreateXPathParsingError("A character map key occurs more than once.", XmlErrorCodes.SEPM0018);
            }

            XmlCharacterMapEntry entry = new XmlCharacterMapEntry();
            entry.CodePoint = (uint)codepoint;
            entry.Replacement = replacement;
            result.Add(entry);
            index = index + 1;
        }

        return result.ToArray();
    }

    internal string? ReadXPathSerializationNamedAttribute(XmlNode element, string requested)
    {
        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlName? name = attributes[index].Name;
            if (name != null && name.LocalName == requested && (name.NamespaceUri == null || name.NamespaceUri.Length == 0))
            {
                return attributes[index].Value;
            }

            index = index + 1;
        }

        return null;
    }

    internal string? ReadXPathSerializationValueAttribute(XmlNode element)
    {
        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlName? name = attributes[index].Name;
            if (name != null && name.LocalName == "value" && (name.NamespaceUri == null || name.NamespaceUri.Length == 0))
            {
                return attributes[index].Value;
            }

            index = index + 1;
        }

        return null;
    }

    internal void ApplyXPathSerializationLexicalEntry(XmlSerializationOptions options, string name, string value)
    {
        if (name == "method")
        {
            options.Method = ReadXPathSerializationMethodName(value);
        }
        else if (name == "json-node-output-method")
        {
            options.JsonNodeOutputMethod = ReadXPathSerializationMethodName(value);
        }
        else if (name == "allow-duplicate-names")
        {
            options.AllowDuplicateNames = ReadXPathSerializationElementYesNo(value);
        }
        else if (name == "byte-order-mark")
        {
            options.ByteOrderMark = ReadXPathSerializationElementYesNo(value);
        }
        else if (name == "escape-uri-attributes")
        {
            options.EscapeUriAttributes = ReadXPathSerializationElementYesNo(value);
        }
        else if (name == "include-content-type")
        {
            options.IncludeContentType = ReadXPathSerializationElementYesNo(value);
        }
        else if (name == "indent")
        {
            options.Indent = ReadXPathSerializationElementYesNo(value);
        }
        else if (name == "omit-xml-declaration")
        {
            options.OmitXmlDeclaration = ReadXPathSerializationElementYesNo(value);
        }
        else if (name == "undeclare-prefixes")
        {
            options.UndeclarePrefixes = ReadXPathSerializationElementYesNo(value);
        }
        else if (name == "standalone")
        {
            string standalone = value.Trim();
            options.Standalone = standalone == "omit" ? XmlStandalone.Unspecified : ReadXPathSerializationElementYesNo(standalone) ? XmlStandalone.Yes : XmlStandalone.No;
        }
        else if (name == "doctype-public")
        {
            options.DoctypePublic = value.Length == 0 ? null : value;
        }
        else if (name == "doctype-system")
        {
            options.DoctypeSystem = value.Length == 0 ? null : value;
        }
        else if (name == "encoding")
        {
            options.Encoding = value;
        }
        else if (name == "html-version")
        {
            options.HtmlVersion = value;
        }
        else if (name == "item-separator")
        {
            options.ItemSeparator = value;
        }
        else if (name == "media-type")
        {
            options.MediaType = value;
        }
        else if (name == "normalization-form")
        {
            options.NormalizationForm = value;
        }
        else if (name == "version")
        {
            options.Version = value;
        }
    }

    internal void SetXPathSerializationMethod(XmlSerializationOptions options, XdmSequence supplied, bool jsonNodeMethod)
    {
        XmlSerializationMethod method = XmlSerializationMethod.Xml;
        XmlName? extension = null;
        if (supplied.Count != 0)
        {
            if (supplied.Count != 1)
            {
                throw CreateXPathSerializationTypeError("A serialization method must be one string or QName.");
            }

            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
            XdmAtomicValue? value = item.AtomicValue;
            if (item.Kind != XdmItemKind.Atomic || value == null)
            {
                throw CreateXPathSerializationTypeError("A serialization method must be one string or QName.");
            }

            if (Maps.IsXdmStringMapKey(value.Type))
            {
                method = ReadXPathSerializationMethodName(Converter.ReadXdmAtomicStringValue(value));
            }
            else if (value.Type == XdmAtomicType.QName && value.NameValue != null)
            {
                XmlName name = value.NameValue;
                if (name.NamespaceUri == null || name.NamespaceUri.Length == 0)
                {
                    method = ReadXPathSerializationMethodName(name.LocalName == null ? "" : name.LocalName);
                }
                else
                {
                    method = XmlSerializationMethod.Extension;
                    extension = name;
                }
            }
            else
            {
                throw CreateXPathSerializationTypeError("A serialization method must be one string or QName.");
            }
        }

        if (jsonNodeMethod)
        {
            options.JsonNodeOutputMethod = method;
        }
        else
        {
            options.Method = method;
            options.ExtensionMethodName = extension;
        }
    }

    internal XmlSerializationMethod ReadXPathSerializationMethodName(string value)
    {
        if (value == "xml")
        {
            return XmlSerializationMethod.Xml;
        }

        if (value == "xhtml")
        {
            return XmlSerializationMethod.Xhtml;
        }

        if (value == "html")
        {
            return XmlSerializationMethod.Html;
        }

        if (value == "text")
        {
            return XmlSerializationMethod.Text;
        }

        if (value == "json")
        {
            return XmlSerializationMethod.Json;
        }

        if (value == "adaptive")
        {
            return XmlSerializationMethod.Adaptive;
        }

        throw CreateXPathParsingError("The serialization method is not recognized.", XmlErrorCodes.SEPM0016);
    }

    internal bool ReadXPathSerializationBoolean(XdmSequence supplied, bool defaultValue)
    {
        if (supplied.Count == 0)
        {
            return defaultValue;
        }

        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.Boolean;
        expected.Occurrence = XdmOccurrence.ExactlyOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
        return value != null && value.BooleanValue;
    }

    internal string? ReadXPathSerializationString(XdmSequence supplied, string? defaultValue)
    {
        if (supplied.Count == 0)
        {
            return defaultValue;
        }

        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.String;
        expected.Occurrence = XdmOccurrence.ExactlyOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
        return value == null ? defaultValue : Converter.ReadXdmAtomicStringValue(value);
    }

    internal string ReadXPathSerializationDecimal(XdmSequence supplied, string defaultValue)
    {
        if (supplied.Count == 0)
        {
            return defaultValue;
        }

        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.Decimal;
        expected.Occurrence = XdmOccurrence.ExactlyOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
        return value == null || value.StringValue == null ? defaultValue : value.StringValue;
    }

    internal XmlName[] ReadXPathSerializationQNames(XdmSequence supplied)
    {
        XmlName[] result = new XmlName[supplied.Count];
        int index = 0;
        while (index < supplied.Count)
        {
            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, index);
            if (item.Kind != XdmItemKind.Atomic || item.AtomicValue == null || item.AtomicValue.Type != XdmAtomicType.QName || item.AtomicValue.NameValue == null)
            {
                throw CreateXPathSerializationTypeError("A serialization QName list contains a non-QName value.");
            }

            result[index] = item.AtomicValue.NameValue;
            index = index + 1;
        }

        return result;
    }

    internal XmlCharacterMapEntry[] ReadXPathSerializationCharacterMap(XdmSequence supplied)
    {
        if (supplied.Count == 0)
        {
            return [];
        }

        if (supplied.Count != 1)
        {
            throw CreateXPathSerializationTypeError("The use-character-maps parameter must contain one map.");
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.Kind != XdmItemKind.Map || item.Map == null)
        {
            throw CreateXPathSerializationTypeError("The use-character-maps parameter must contain one map.");
        }

        Maps.ValidateXdmMapStorage(item.Map);
        XdmMapEntry[] entries = item.Map.Entries == null ? [] : item.Map.Entries;
        XmlCharacterMapEntry[] result = new XmlCharacterMapEntry[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            XdmAtomicValue? key = entries[index].Key;
            XdmSequence? value = entries[index].Value;
            if (key == null || value == null || !Maps.IsXdmStringMapKey(key.Type))
            {
                throw CreateXPathSerializationTypeError("Character map keys must be strings.");
            }

            string character = Converter.ReadXdmAtomicStringValue(key);
            int codePoint = character.Length == 1 ? character[0] : character.Length == 2 && char.IsSurrogatePair(character, 0) ? char.ConvertToUtf32(character, 0) : -1;
            if (codePoint < 0)
            {
                throw CreateXPathSerializationTypeError("Each character map key must contain one Unicode character.");
            }

            XmlCharacterMapEntry entry = new XmlCharacterMapEntry();
            entry.CodePoint = (uint)codePoint;
            if (value.Count != 1 || Sequences.ReadXdmSequenceItem(value, 0).Kind != XdmItemKind.Atomic)
            {
                throw CreateXPathSerializationTypeError("Character map values must be atomic strings.");
            }

            entry.Replacement = ReadXPathSerializationString(value, "");
            result[index] = entry;
            index = index + 1;
        }

        return result;
    }

    internal bool ReadXPathSerializationYesNo(string value)
    {
        if (value == "yes" || value == "true" || value == "1")
        {
            return true;
        }

        if (value == "no" || value == "false" || value == "0")
        {
            return false;
        }

        throw CreateXPathParsingError("A serialization Boolean parameter has an invalid value.", XmlErrorCodes.SEPM0016);
    }

    internal bool ReadXPathSerializationElementYesNo(string value)
    {
        string normalized = value.Trim();
        if (normalized == "yes" || normalized == "true" || normalized == "1")
        {
            return true;
        }

        if (normalized == "no" || normalized == "false" || normalized == "0")
        {
            return false;
        }

        throw CreateXPathParsingError("A serialization parameter element has an invalid Boolean value.", XmlErrorCodes.SEPM0017);
    }

    internal string? ReadXPathOptionalString(XdmSequence supplied)
    {
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.String;
        expected.Occurrence = XdmOccurrence.ZeroOrOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        if (converted.Count == 0)
        {
            return null;
        }

        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
        return value == null ? null : Converter.ReadXdmAtomicStringValue(value);
    }

    internal XPathDynamicException CreateXPathParsingError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = Range;
        return new XPathDynamicException(message, diagnostic, Range.Start);
    }

    internal XPathTypeException CreateXPathSerializationTypeError(string message)
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
