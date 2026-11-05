using System;
using System.Text;

namespace xml;

internal class XPathUriQNameFunctionImplementation : IXPathFunction
{
    internal string LocalName;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmTypeSystem Types;
    internal XdmFactory Factory;
    internal XdmQNameDatatype QNames;
    internal XdmStringDatatype Strings;

    internal XPathUriQNameFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext)
    {
        LocalName = localName;
        Range = CopyXPathUriQNameRange(range);
        StaticContext = staticContext;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Types = new XdmTypeSystem();
        Factory = new XdmFactory();
        QNames = new XdmQNameDatatype();
        Strings = new XdmStringDatatype();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        if (LocalName == "resolve-uri")
        {
            return EvaluateXPathResolveUri(arguments);
        }

        if (LocalName == "encode-for-uri" || LocalName == "iri-to-uri" || LocalName == "escape-html-uri")
        {
            return EvaluateXPathUriEncoder(arguments[0]);
        }

        if (LocalName == "QName")
        {
            return EvaluateXPathQNameConstructor(arguments);
        }

        if (LocalName == "resolve-QName")
        {
            return EvaluateXPathResolveQName(arguments);
        }

        if (LocalName == "prefix-from-QName" || LocalName == "local-name-from-QName" || LocalName == "namespace-uri-from-QName")
        {
            return EvaluateXPathQNameComponent(arguments[0]);
        }

        if (LocalName == "namespace-uri-for-prefix")
        {
            return EvaluateXPathNamespaceUriForPrefix(arguments);
        }

        if (LocalName == "in-scope-prefixes")
        {
            return EvaluateXPathInScopePrefixes(arguments[0]);
        }

        throw new InvalidOperationException("The XPath URI or QName function is not implemented.");
    }

    internal XdmSequence EvaluateXPathResolveUri(XdmSequence[] arguments)
    {
        string? relative = ReadXPathUriQNameOptionalString(arguments[0]);
        string? suppliedBase = arguments.Length == 2 ? ReadXPathUriQNameRequiredString(arguments[1]) : null;
        if (relative == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        if (IsXPathAbsoluteIri(relative))
        {
            return CreateXPathUriResult(relative);
        }

        if (!IsXPathRelativeIri(relative))
        {
            throw CreateXPathUriQNameError("The relative URI argument is not a valid IRI reference.", XPathErrorCodes.FORG0002);
        }

        string? baseValue = arguments.Length == 2 ? suppliedBase : StaticContext.BaseUri;
        if (baseValue == null)
        {
            throw CreateXPathUriQNameError("The static base URI is absent.", XPathErrorCodes.FONS0005);
        }

        if (!IsXPathUsableBaseIri(baseValue))
        {
            throw CreateXPathUriQNameError("The base URI is not a valid hierarchical absolute IRI without a fragment.", XPathErrorCodes.FORG0002);
        }

        Uri baseUri = new Uri(baseValue, UriKind.Absolute);
        Uri resolved = new Uri(baseUri, relative);
        string result = relative.IndexOf('%') >= 0 || baseValue.IndexOf('%') >= 0 ? UriTools.CanonicalAbsoluteUri(resolved) : resolved.ToString();
        return CreateXPathUriResult(result);
    }

    internal bool IsXPathAbsoluteIri(string value)
    {
        string validation = CreateXPathUriValidationText(value);
        return IsXPathIriAsciiText(value) && Uri.IsWellFormedUriString(validation, UriKind.Absolute);
    }

    internal bool IsXPathRelativeIri(string value)
    {
        string validation = CreateXPathUriValidationText(value);
        return value.Length == 0 || IsXPathIriAsciiText(value) && Uri.IsWellFormedUriString(validation, UriKind.Relative);
    }

    internal bool IsXPathUsableBaseIri(string value)
    {
        if (!IsXPathAbsoluteIri(value) || value.IndexOf('#') >= 0)
        {
            return false;
        }

        int colon = value.IndexOf(':');
        return colon >= 1 && colon + 1 < value.Length && value[colon + 1] == '/';
    }

    internal bool IsXPathIriAsciiText(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            int codepoint = ReadXPathUriCodepoint(value, index);
            if (codepoint <= 32 || codepoint == 127 || codepoint == '<' || codepoint == '>' || codepoint == '"' || codepoint == '{' || codepoint == '}' || codepoint == '|' || codepoint == '\\' || codepoint == '^' || codepoint == '`')
            {
                return false;
            }

            index = index + (codepoint > 0xFFFF ? 2 : 1);
        }

        return true;
    }

    internal string CreateXPathUriValidationText(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            int codepoint = ReadXPathUriCodepoint(value, index);
            int codeUnits = codepoint > 0xFFFF ? 2 : 1;
            if (codepoint <= 127)
            {
                result.Append(value, index, codeUnits);
            }
            else
            {
                AppendXPathUriEncodedCodepoint(result, codepoint);
            }

            index = index + codeUnits;
        }

        return result.ToString();
    }

    internal XdmSequence EvaluateXPathUriEncoder(XdmSequence supplied)
    {
        string value = ReadXPathUriQNameOptionalString(supplied) ?? "";
        StringBuilder result = new StringBuilder();
        int offset = 0;
        while (offset < value.Length)
        {
            int codepoint = ReadXPathUriCodepoint(value, offset);
            int codeUnits = codepoint > 0xFFFF ? 2 : 1;
            bool preserve;
            if (LocalName == "encode-for-uri")
            {
                preserve = IsXPathUriUnreserved(codepoint);
            }
            else if (LocalName == "iri-to-uri")
            {
                preserve = IsXPathIriUriCharacter(codepoint);
            }
            else
            {
                preserve = codepoint >= 32 && codepoint <= 126;
            }

            if (preserve)
            {
                result.Append(value, offset, codeUnits);
            }
            else
            {
                AppendXPathUriEncodedCodepoint(result, codepoint);
            }

            offset = offset + codeUnits;
        }

        return CreateXPathUriStringResult(result.ToString());
    }

    internal int ReadXPathUriCodepoint(string value, int offset)
    {
        int first = value[offset];
        if (first >= 0xD800 && first <= 0xDBFF)
        {
            if (offset + 1 >= value.Length || value[offset + 1] < 0xDC00 || value[offset + 1] > 0xDFFF)
            {
                throw CreateXPathUriQNameError("The URI argument contains an unpaired UTF-16 surrogate.", XPathErrorCodes.FOCH0001);
            }

            int second = value[offset + 1];
            return 0x10000 + (first - 0xD800) * 0x400 + second - 0xDC00;
        }

        if (first >= 0xDC00 && first <= 0xDFFF)
        {
            throw CreateXPathUriQNameError("The URI argument contains an unpaired UTF-16 surrogate.", XPathErrorCodes.FOCH0001);
        }

        return first;
    }

    internal bool IsXPathUriUnreserved(int codepoint)
    {
        return codepoint >= 'A' && codepoint <= 'Z' || codepoint >= 'a' && codepoint <= 'z' || codepoint >= '0' && codepoint <= '9' || codepoint == '-' || codepoint == '_' || codepoint == '.' || codepoint == '~';
    }

    internal bool IsXPathIriUriCharacter(int codepoint)
    {
        if (codepoint < 33 || codepoint > 126)
        {
            return false;
        }

        return codepoint != '<' && codepoint != '>' && codepoint != '"' && codepoint != '{' && codepoint != '}' && codepoint != '|' && codepoint != '\\' && codepoint != '^' && codepoint != '`';
    }

    internal void AppendXPathUriEncodedCodepoint(StringBuilder result, int codepoint)
    {
        if (codepoint <= 0x7F)
        {
            AppendXPathUriEncodedByte(result, codepoint);
            return;
        }

        if (codepoint <= 0x7FF)
        {
            AppendXPathUriEncodedByte(result, 0xC0 | codepoint >> 6);
            AppendXPathUriEncodedByte(result, 0x80 | codepoint & 0x3F);
            return;
        }

        if (codepoint <= 0xFFFF)
        {
            AppendXPathUriEncodedByte(result, 0xE0 | codepoint >> 12);
            AppendXPathUriEncodedByte(result, 0x80 | codepoint >> 6 & 0x3F);
            AppendXPathUriEncodedByte(result, 0x80 | codepoint & 0x3F);
            return;
        }

        AppendXPathUriEncodedByte(result, 0xF0 | codepoint >> 18);
        AppendXPathUriEncodedByte(result, 0x80 | codepoint >> 12 & 0x3F);
        AppendXPathUriEncodedByte(result, 0x80 | codepoint >> 6 & 0x3F);
        AppendXPathUriEncodedByte(result, 0x80 | codepoint & 0x3F);
    }

    internal void AppendXPathUriEncodedByte(StringBuilder result, int value)
    {
        const string digits = "0123456789ABCDEF";
        result.Append('%');
        result.Append(digits[value >> 4 & 0xF]);
        result.Append(digits[value & 0xF]);
    }

    internal XdmSequence EvaluateXPathQNameConstructor(XdmSequence[] arguments)
    {
        string? namespaceUri = ReadXPathUriQNameOptionalString(arguments[0]);
        string lexicalQName = ReadXPathUriQNameRequiredString(arguments[1]);
        string normalized = Strings.CollapseXdmStringWhitespace(lexicalQName);
        XmlName? parsed = QNames.ParseXdmQNameLexical(normalized);
        if (parsed == null)
        {
            throw CreateXPathUriQNameError("The lexical QName is invalid.", XPathErrorCodes.FOCA0002);
        }

        if (parsed.Prefix != null && (namespaceUri == null || namespaceUri.Length == 0))
        {
            throw CreateXPathUriQNameError("A prefixed QName requires a namespace URI.", XPathErrorCodes.FOCA0002);
        }

        XdmAtomicValue result = QNames.CreateXdmQNameValue(namespaceUri, lexicalQName);
        return Sequences.CreateXdmAtomicSequence(result);
    }

    internal XdmSequence EvaluateXPathResolveQName(XdmSequence[] arguments)
    {
        string? lexicalQName = ReadXPathUriQNameOptionalString(arguments[0]);
        XmlNode element = ReadXPathUriQNameElement(arguments[1]);
        if (lexicalQName == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        string normalized = Strings.CollapseXdmStringWhitespace(lexicalQName);
        if (QNames.ParseXdmQNameLexical(normalized) == null)
        {
            throw CreateXPathUriQNameError("The lexical QName is invalid.", XPathErrorCodes.FOCA0002);
        }

        XmlNamespaceBinding[] namespaces = new TreeNavigation().SelectXmlNamespacesOperation(element);
        XmlNamespaceBinding? defaultBinding = QNames.FindXdmNamespaceBinding(namespaces, "");
        string? defaultNamespace = defaultBinding == null || defaultBinding.IsUndeclaration ? null : defaultBinding.NamespaceUri;
        XdmNumericParseResult parsed = QNames.TryParseXdmQNameWithNamespaces(lexicalQName, XdmAtomicType.QName, namespaces, defaultNamespace);
        if (!parsed.Success || parsed.Value == null)
        {
            string code = parsed.ErrorCode == null ? XPathErrorCodes.FORG0001 : parsed.ErrorCode;
            string message = parsed.ErrorMessage == null ? "The QName cannot be resolved." : parsed.ErrorMessage;
            throw CreateXPathUriQNameError(message, code);
        }

        return Sequences.CreateXdmAtomicSequence(parsed.Value);
    }

    internal XdmSequence EvaluateXPathQNameComponent(XdmSequence supplied)
    {
        XdmAtomicValue? value = ReadXPathUriQNameOptionalQName(supplied);
        if (value == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XmlName name = QNames.ReadXdmExpandedName(value, XdmAtomicType.QName);
        if (LocalName == "prefix-from-QName")
        {
            if (name.Prefix == null || name.Prefix.Length == 0)
            {
                return Sequences.CreateEmptyXdmSequence();
            }

            return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmStringAtomicValue(name.Prefix, XdmAtomicType.NcName));
        }

        if (LocalName == "local-name-from-QName")
        {
            string localName = name.LocalName == null ? "" : name.LocalName;
            return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmStringAtomicValue(localName, XdmAtomicType.NcName));
        }

        string namespaceUri = name.NamespaceUri == null ? "" : name.NamespaceUri;
        return CreateXPathUriResult(namespaceUri);
    }

    internal XdmSequence EvaluateXPathNamespaceUriForPrefix(XdmSequence[] arguments)
    {
        string prefix = ReadXPathUriQNameOptionalString(arguments[0]) ?? "";
        XmlNode element = ReadXPathUriQNameElement(arguments[1]);
        XmlNamespaceBinding[] namespaces = new TreeNavigation().SelectXmlNamespacesOperation(element);
        XmlNamespaceBinding? binding = QNames.FindXdmNamespaceBinding(namespaces, prefix);
        if (binding == null || binding.IsUndeclaration || binding.NamespaceUri == null || binding.NamespaceUri.Length == 0)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        return CreateXPathUriResult(binding.NamespaceUri);
    }

    internal XdmSequence EvaluateXPathInScopePrefixes(XdmSequence supplied)
    {
        XmlNode element = ReadXPathUriQNameElement(supplied);
        XmlNamespaceBinding[] namespaces = new TreeNavigation().SelectXmlNamespacesOperation(element);
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < namespaces.Length)
        {
            XmlNamespaceBinding namespaceBinding = namespaces[index];
            string prefix = namespaceBinding.Prefix == null ? "" : namespaceBinding.Prefix;
            result.AddXdmAtomicValue(Factory.CreateXdmStringAtomicValue(prefix, XdmAtomicType.String));
            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal string? ReadXPathUriQNameOptionalString(XdmSequence supplied)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count == 0)
        {
            return null;
        }

        if (atomized.Count != 1)
        {
            throw CreateXPathUriQNameError("A string argument must atomize to at most one value.", XPathErrorCodes.XPTY0004);
        }

        XdmAtomicValue? value = Sequences.ReadValidatedXdmSequenceItem(atomized, 0).AtomicValue;
        if (value == null)
        {
            throw new InvalidOperationException("Atomization did not produce an atomic value.");
        }

        bool supported = Types.IsXdmStringDerivedAtomicType(value.Type) || value.Type == XdmAtomicType.UntypedAtomic || value.Type == XdmAtomicType.AnyUri;
        if (!supported)
        {
            throw CreateXPathUriQNameError("The function argument must be a string.", XPathErrorCodes.XPTY0004);
        }

        return Converter.ReadXdmAtomicStringValue(value);
    }

    internal string ReadXPathUriQNameRequiredString(XdmSequence supplied)
    {
        string? value = ReadXPathUriQNameOptionalString(supplied);
        if (value == null)
        {
            throw CreateXPathUriQNameError("A required string argument is empty.", XPathErrorCodes.XPTY0004);
        }

        return value;
    }

    internal XdmAtomicValue? ReadXPathUriQNameOptionalQName(XdmSequence supplied)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count == 0)
        {
            return null;
        }

        if (atomized.Count != 1)
        {
            throw CreateXPathUriQNameError("A QName argument must contain at most one value.", XPathErrorCodes.XPTY0004);
        }

        XdmAtomicValue? value = Sequences.ReadValidatedXdmSequenceItem(atomized, 0).AtomicValue;
        if (value == null)
        {
            throw new InvalidOperationException("Atomization did not produce an atomic value.");
        }

        if (value.Type == XdmAtomicType.UntypedAtomic)
        {
            throw CreateXPathUriQNameError("Function conversion cannot convert xs:untypedAtomic to xs:QName.", XPathErrorCodes.XPTY0117);
        }

        if (value.Type != XdmAtomicType.QName)
        {
            throw CreateXPathUriQNameError("The function argument must be an xs:QName.", XPathErrorCodes.XPTY0004);
        }

        return value;
    }

    internal XmlNode ReadXPathUriQNameElement(XdmSequence supplied)
    {
        Sequences.ValidateXdmSequenceStorage(supplied);
        if (supplied.Count != 1)
        {
            throw CreateXPathUriQNameError("An element argument must contain exactly one node.", XPathErrorCodes.XPTY0004);
        }

        XdmSequenceItem item = Sequences.ReadValidatedXdmSequenceItem(supplied, 0);
        if (item.Kind != XdmItemKind.Node || item.Node == null || item.Node.Kind != XmlNodeKind.Element)
        {
            throw CreateXPathUriQNameError("The function argument must be an element node.", XPathErrorCodes.XPTY0004);
        }

        return item.Node;
    }

    internal XdmSequence CreateXPathUriResult(string value)
    {
        return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmAnyUriAtomicValue(value));
    }

    internal XdmSequence CreateXPathUriStringResult(string value)
    {
        return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmStringAtomicValue(value, XdmAtomicType.String));
    }

    internal XPathDynamicException CreateXPathUriQNameError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = CopyXPathUriQNameRange(Range);
        return new XPathDynamicException(message, diagnostic, diagnostic.Range.Start);
    }

    internal SourceRange CopyXPathUriQNameRange(SourceRange source)
    {
        SourceRange result = new SourceRange();
        result.Start = CopyXPathUriQNameLocation(source.Start);
        result.End = CopyXPathUriQNameLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXPathUriQNameLocation(SourceLocation? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceLocation result = new SourceLocation();
        result.SourceUri = source.SourceUri;
        result.CharacterOffset = source.CharacterOffset;
        result.ByteOffset = source.ByteOffset;
        result.Line = source.Line;
        result.Column = source.Column;
        return result;
    }
}
