using System;

namespace xml;

internal class XdmQNameDatatype
{
    internal XdmAtomicValue ParseXdmQNameWithNamespaces(string lexicalValue, XdmAtomicType targetType, XmlNamespaceBinding[] namespaces, string? defaultNamespace)
    {
        XdmNumericParseResult result = TryParseXdmQNameWithNamespaces(lexicalValue, targetType, namespaces, defaultNamespace);
        return new XdmNumericDatatype().RequireXdmNumericParseResult(result);
    }

    internal XdmNumericParseResult TryParseXdmQNameWithNamespaces(string lexicalValue, XdmAtomicType targetType, XmlNamespaceBinding[] namespaces, string? defaultNamespace)
    {
        Guard.NotNull(lexicalValue, "lexicalValue");
        Guard.NotNull(namespaces, "namespaces");
        RequireXdmNamespaceSensitiveType(targetType);
        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(lexicalValue);
        XmlName? name = ParseXdmQNameLexical(normalized);
        if (name == null)
        {
            return InvalidXdmQNameLexical();
        }

        string? prefix = name.Prefix;
        string? namespaceUri;
        if (prefix == null)
        {
            namespaceUri = NormalizeXdmAbsentNamespace(defaultNamespace);
        }
        else if (string.Equals(prefix, "xml", StringComparison.Ordinal))
        {
            namespaceUri = XmlNamespaces.Xml;
        }
        else
        {
            XmlNamespaceBinding? binding = FindXdmNamespaceBinding(namespaces, prefix);
            if (binding == null || binding.IsUndeclaration || binding.NamespaceUri == null || binding.NamespaceUri.Length == 0)
            {
                return UnboundXdmQNamePrefix(prefix);
            }

            namespaceUri = binding.NamespaceUri;
        }

        if (namespaceUri != null && !new XdmAnyUriDatatype().IsValidXdmAnyUriValue(namespaceUri))
        {
            return new XdmNumericParseResult(false, null, XPathErrorCodes.FOCA0002, "The resolved QName namespace is not a valid xs:anyURI value.");
        }

        name.NamespaceUri = namespaceUri;
        return new XdmNumericParseResult(true, CreateXdmNamespaceSensitiveValue(name, targetType, lexicalValue), null, null);
    }

    internal XdmAtomicValue ResolveXdmQNameStatic(string lexicalValue, XPathStaticContext context)
    {
        Guard.NotNull(context, "context");
        XmlNamespaceBinding[] namespaces = context.Namespaces == null ? [] : context.Namespaces;
        return ParseXdmQNameWithNamespaces(lexicalValue, XdmAtomicType.QName, namespaces, context.DefaultElementNamespace);
    }

    internal XdmAtomicValue ResolveXdmQNameDynamic(string lexicalValue, XmlNode element)
    {
        Guard.NotNull(lexicalValue, "lexicalValue");
        Guard.NotNull(element, "element");
        if (element.Kind != XmlNodeKind.Element)
        {
            throw new ArgumentException("Dynamic QName resolution requires an element node.", "element");
        }

        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(lexicalValue);
        if (ParseXdmQNameLexical(normalized) == null)
        {
            throw CreateXdmQNameDynamicError("The lexical value is not valid for xs:QName.", XPathErrorCodes.FOCA0002);
        }

        XmlNamespaceBinding[] namespaces = new TreeNavigation().SelectXmlNamespacesOperation(element);
        XmlNamespaceBinding? defaultBinding = FindXdmNamespaceBinding(namespaces, "");
        string? defaultNamespace = defaultBinding == null || defaultBinding.IsUndeclaration ? null : defaultBinding.NamespaceUri;
        return ParseXdmQNameWithNamespaces(lexicalValue, XdmAtomicType.QName, namespaces, defaultNamespace);
    }

    internal XdmAtomicValue ResolveXdmDeclaredNotation(string lexicalValue, XmlNamespaceBinding[] namespaces, string? defaultNamespace, XmlName[] permittedNames)
    {
        Guard.NotNull(permittedNames, "permittedNames");
        XdmAtomicValue value = ParseXdmQNameWithNamespaces(lexicalValue, XdmAtomicType.Notation, namespaces, defaultNamespace);
        XmlName actual = ReadXdmExpandedName(value, XdmAtomicType.Notation);
        int index = 0;
        while (index < permittedNames.Length)
        {
            XmlName? permitted = permittedNames[index];
            if (permitted != null && IsEqualXdmExpandedName(actual, permitted))
            {
                return value;
            }

            index = index + 1;
        }

        throw CreateXdmQNameDynamicError("The QName does not identify a permitted xs:NOTATION value.", XPathErrorCodes.FORG0001);
    }

    internal XdmAtomicValue CreateXdmQNameValue(string? namespaceUri, string lexicalQName)
    {
        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(lexicalQName);
        XmlName? name = ParseXdmQNameLexical(normalized);
        if (name == null)
        {
            throw CreateXdmQNameDynamicError("The lexical value is not valid for xs:QName.", XPathErrorCodes.FOCA0002);
        }

        string? normalizedNamespace = NormalizeXdmAbsentNamespace(namespaceUri);
        if (name.Prefix != null && normalizedNamespace == null)
        {
            throw CreateXdmQNameDynamicError("A prefixed QName must have a namespace URI.", XPathErrorCodes.FOCA0002);
        }

        if (normalizedNamespace != null && !new XdmAnyUriDatatype().IsValidXdmAnyUriValue(normalizedNamespace))
        {
            throw CreateXdmQNameDynamicError("The QName namespace URI is not a valid xs:anyURI value.", XPathErrorCodes.FOCA0002);
        }

        name.NamespaceUri = normalizedNamespace;
        return CreateXdmNamespaceSensitiveValue(name, XdmAtomicType.QName, lexicalQName);
    }

    internal XdmAtomicValue CreateXdmNotationValue(string? namespaceUri, string lexicalQName, XmlName[] permittedNames)
    {
        Guard.NotNull(permittedNames, "permittedNames");
        XdmAtomicValue qname = CreateXdmQNameValue(namespaceUri, lexicalQName);
        XmlName actual = ReadXdmExpandedName(qname, XdmAtomicType.QName);
        int index = 0;
        while (index < permittedNames.Length)
        {
            XmlName? permitted = permittedNames[index];
            if (permitted != null && IsEqualXdmExpandedName(actual, permitted))
            {
                return CreateXdmNamespaceSensitiveValue(CopyXdmExpandedName(actual), XdmAtomicType.Notation, lexicalQName);
            }

            index = index + 1;
        }

        throw CreateXdmQNameDynamicError("The QName does not identify a permitted xs:NOTATION value.", XPathErrorCodes.FORG0001);
    }

    internal XdmAtomicValue CastXdmAtomicToQName(XdmAtomicValue source, XPathStaticContext context)
    {
        Guard.NotNull(source, "source");
        Guard.NotNull(context, "context");
        if (source.Type == XdmAtomicType.QName || source.Type == XdmAtomicType.Notation)
        {
            XmlName name = ReadXdmExpandedName(source, source.Type);
            string lexical = name.QualifiedName == null ? BuildXdmQNameLexical(name) : name.QualifiedName;
            return CreateXdmNamespaceSensitiveValue(CopyXdmExpandedName(name), XdmAtomicType.QName, lexical);
        }

        bool stringType = new XdmTypeSystem().IsXdmStringDerivedAtomicType(source.Type);
        if (!stringType && source.Type != XdmAtomicType.UntypedAtomic)
        {
            throw CreateXdmQNameTypeError("Only xs:string, xs:untypedAtomic, or xs:QName can be cast to xs:QName.");
        }

        string? lexicalValue = source.StringValue;
        if (lexicalValue == null)
        {
            lexicalValue = source.CanonicalValue == null ? source.LexicalValue : source.CanonicalValue;
        }

        if (lexicalValue == null)
        {
            throw CreateXdmQNameTypeError("The source value does not provide a lexical representation.");
        }

        return ResolveXdmQNameStatic(lexicalValue, context);
    }

    internal void EnsureXdmNamespaceSensitiveCastTarget(XdmAtomicType targetType)
    {
        if (targetType == XdmAtomicType.Notation)
        {
            throw CreateXdmQNameStaticError("xs:NOTATION cannot be the target type of cast or castable.", XPathErrorCodes.XPST0080);
        }

        if (targetType != XdmAtomicType.QName)
        {
            throw new ArgumentException("The requested cast target is not namespace-sensitive.", "targetType");
        }
    }

    internal bool EqualXdmQName(XdmAtomicValue left, XdmAtomicValue right)
    {
        XmlName leftName = ReadXdmExpandedName(left, XdmAtomicType.QName);
        XmlName rightName = ReadXdmExpandedName(right, XdmAtomicType.QName);
        return IsEqualXdmExpandedName(leftName, rightName);
    }

    internal bool EqualXdmNotation(XdmAtomicValue left, XdmAtomicValue right)
    {
        XmlName leftName = ReadXdmExpandedName(left, XdmAtomicType.Notation);
        XmlName rightName = ReadXdmExpandedName(right, XdmAtomicType.Notation);
        return IsEqualXdmExpandedName(leftName, rightName);
    }

    internal bool IsEqualXdmExpandedName(XmlName left, XmlName right)
    {
        Guard.NotNull(left, "left");
        Guard.NotNull(right, "right");
        string leftNamespace = left.NamespaceUri == null ? "" : left.NamespaceUri;
        string rightNamespace = right.NamespaceUri == null ? "" : right.NamespaceUri;
        return string.Equals(leftNamespace, rightNamespace, StringComparison.Ordinal) && string.Equals(left.LocalName, right.LocalName, StringComparison.Ordinal);
    }

    internal XmlName? ParseXdmQNameLexical(string lexicalValue)
    {
        Guard.NotNull(lexicalValue, "lexicalValue");
        int colon = lexicalValue.IndexOf(':');
        string? prefix = null;
        string localName = lexicalValue;
        if (colon >= 0)
        {
            if (colon != lexicalValue.LastIndexOf(':'))
            {
                return null;
            }

            prefix = lexicalValue.Substring(0, colon);
            localName = lexicalValue.Substring(colon + 1);
        }

        XdmStringDatatype strings = new XdmStringDatatype();
        if (prefix != null && !strings.IsValidXdmName(prefix, true) || !strings.IsValidXdmName(localName, true))
        {
            return null;
        }

        XmlName result = new XmlName();
        result.Prefix = prefix;
        result.LocalName = localName;
        result.NamespaceUri = null;
        result.QualifiedName = lexicalValue;
        return result;
    }

    internal XmlNamespaceBinding? FindXdmNamespaceBinding(XmlNamespaceBinding[] namespaces, string prefix)
    {
        int index = 0;
        while (index < namespaces.Length)
        {
            XmlNamespaceBinding? binding = namespaces[index];
            if (binding != null && string.Equals(binding.Prefix == null ? "" : binding.Prefix, prefix, StringComparison.Ordinal))
            {
                return binding;
            }

            index = index + 1;
        }

        return null;
    }

    internal XdmAtomicValue CreateXdmNamespaceSensitiveValue(XmlName name, XdmAtomicType type, string lexicalValue)
    {
        RequireXdmNamespaceSensitiveType(type);
        string qualifiedName = BuildXdmQNameLexical(name);
        XdmAtomicValue result = new XdmAtomicValue();
        result.Type = type;
        result.LexicalValue = lexicalValue;
        result.CanonicalValue = qualifiedName;
        result.StringValue = qualifiedName;
        result.NameValue = CopyXdmExpandedName(name);
        result.IsImmutable = true;
        return result;
    }

    internal XmlName ReadXdmExpandedName(XdmAtomicValue value, XdmAtomicType expectedType)
    {
        Guard.NotNull(value, "value");
        RequireXdmNamespaceSensitiveType(expectedType);
        if (value.Type != expectedType || value.NameValue == null)
        {
            throw new ArgumentException("The atomic value does not have the expected namespace-sensitive type and storage.", "value");
        }

        XmlName name = value.NameValue;
        if (!IsValidXdmExpandedName(name))
        {
            throw new ArgumentException("The atomic value contains an invalid expanded QName.", "value");
        }

        return name;
    }

    internal bool IsValidXdmExpandedName(XmlName name)
    {
        Guard.NotNull(name, "name");
        if (name.LocalName == null || !new XdmStringDatatype().IsValidXdmName(name.LocalName, true))
        {
            return false;
        }

        if (name.Prefix != null && !new XdmStringDatatype().IsValidXdmName(name.Prefix, true))
        {
            return false;
        }

        if (name.Prefix != null && (name.NamespaceUri == null || name.NamespaceUri.Length == 0))
        {
            return false;
        }

        if (name.NamespaceUri != null && name.NamespaceUri.Length > 0 && !new XdmAnyUriDatatype().IsValidXdmAnyUriValue(name.NamespaceUri))
        {
            return false;
        }

        return name.QualifiedName == null || string.Equals(name.QualifiedName, BuildXdmQNameLexical(name), StringComparison.Ordinal);
    }

    internal XmlName CopyXdmExpandedName(XmlName source)
    {
        XmlName result = new XmlName();
        result.Prefix = source.Prefix;
        result.LocalName = source.LocalName;
        result.NamespaceUri = NormalizeXdmAbsentNamespace(source.NamespaceUri);
        result.QualifiedName = BuildXdmQNameLexical(source);
        return result;
    }

    internal string BuildXdmQNameLexical(XmlName name)
    {
        string localName = name.LocalName == null ? "" : name.LocalName;
        return name.Prefix == null || name.Prefix.Length == 0 ? localName : name.Prefix + ":" + localName;
    }

    internal string? NormalizeXdmAbsentNamespace(string? namespaceUri)
    {
        return namespaceUri == null || namespaceUri.Length == 0 ? null : namespaceUri;
    }

    internal void RequireXdmNamespaceSensitiveType(XdmAtomicType type)
    {
        if (type != XdmAtomicType.QName && type != XdmAtomicType.Notation)
        {
            throw new ArgumentException("The requested atomic type is not namespace-sensitive.", "type");
        }
    }

    internal XdmNumericParseResult InvalidXdmQNameLexical()
    {
        return new XdmNumericParseResult(false, null, XPathErrorCodes.FORG0001, "The lexical value is not valid for xs:QName.");
    }

    internal XdmNumericParseResult UnboundXdmQNamePrefix(string prefix)
    {
        return new XdmNumericParseResult(false, null, XPathErrorCodes.FONS0004, "The QName prefix '" + prefix + "' has no namespace binding.");
    }

    internal XPathDynamicException CreateXdmQNameDynamicError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathDynamicException(message, diagnostic, null);
    }

    internal XPathStaticException CreateXdmQNameStaticError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathStatic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathStaticException(message, diagnostic, null);
    }

    internal XPathTypeException CreateXdmQNameTypeError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XPathErrorCodes.XPTY0004;
        diagnostic.Category = XmlErrorCategory.XPathType;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathTypeException(message, diagnostic, null);
    }
}
