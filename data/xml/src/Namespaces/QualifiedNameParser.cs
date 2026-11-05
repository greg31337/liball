using System;

namespace xml;

internal class QualifiedNameParts
{
    internal string? Prefix;
    internal string LocalName;
    internal string QualifiedName;

    internal QualifiedNameParts(string? prefix, string localName, string qualifiedName)
    {
        Prefix = prefix;
        LocalName = localName;
        QualifiedName = qualifiedName;
    }
}

internal class QualifiedNameParser
{
    internal XmlNamespaceHandling Handling;
    internal IXmlErrorReporter? ErrorReporter;

    internal QualifiedNameParser(XmlNamespaceHandling handling, IXmlErrorReporter? errorReporter)
    {
        if (handling != XmlNamespaceHandling.Namespaces10 && handling != XmlNamespaceHandling.Namespaces11)
        {
            throw new ArgumentOutOfRangeException("handling");
        }

        Handling = handling;
        ErrorReporter = errorReporter;
    }

    internal QualifiedNameParts ParseXmlQualifiedName(string qualifiedName, SourceRange? range)
    {
        Guard.NotNull(qualifiedName, "qualifiedName");
        if (qualifiedName.Length == 0)
        {
            throw CreateXmlQualifiedNameError("An XML qualified name cannot be empty", XmlErrorCodes.MalformedMarkup, range);
        }

        int colon = qualifiedName.IndexOf(':');
        if (colon < 0)
        {
            if (!IsValidXmlNcName(qualifiedName))
            {
                throw CreateXmlQualifiedNameError("An unprefixed XML name must be a valid NCName", XmlErrorCodes.MalformedMarkup, range);
            }

            return new QualifiedNameParts(null, qualifiedName, qualifiedName);
        }

        if (colon == 0 || colon == qualifiedName.Length - 1 || colon != qualifiedName.LastIndexOf(':'))
        {
            throw CreateXmlQualifiedNameError("An XML qualified name must contain one nonempty prefix and local name", XmlErrorCodes.MalformedMarkup, range);
        }

        string prefix = qualifiedName.Substring(0, colon);
        string localName = qualifiedName.Substring(colon + 1);
        if (!IsValidXmlNcName(prefix) || !IsValidXmlNcName(localName))
        {
            throw CreateXmlQualifiedNameError("The prefix and local part of an XML qualified name must be valid NCNames", XmlErrorCodes.MalformedMarkup, range);
        }

        return new QualifiedNameParts(prefix, localName, qualifiedName);
    }

    internal XmlName ResolveXmlElementName(string qualifiedName, NamespaceContext context, SourceRange? range)
    {
        Guard.NotNull(context, "context");
        QualifiedNameParts parts = ParseXmlQualifiedName(qualifiedName, range);
        string? prefix = parts.Prefix;
        if (string.Equals(prefix, "xmlns", StringComparison.Ordinal))
        {
            throw CreateXmlQualifiedNameError("The reserved 'xmlns' prefix cannot be used on an element name", XmlErrorCodes.NamespaceReservedBinding, range);
        }

        string? namespaceUri;
        if (prefix == null)
        {
            namespaceUri = context.DefaultXmlNamespaceUri();
        }
        else
        {
            namespaceUri = context.LookupXmlNamespaceUri(prefix);
            if (namespaceUri == null)
            {
                throw CreateXmlQualifiedNameError("The namespace prefix '" + prefix + "' is not bound in this element's context", XmlErrorCodes.NamespaceUnboundPrefix, range);
            }
        }

        return CreateResolvedXmlName(parts, namespaceUri);
    }

    internal XmlName ResolveXmlAttributeName(string qualifiedName, NamespaceContext context, SourceRange? range)
    {
        Guard.NotNull(context, "context");
        QualifiedNameParts parts = ParseXmlQualifiedName(qualifiedName, range);
        string? prefix = parts.Prefix;
        string? namespaceUri = null;
        if (prefix != null)
        {
            if (string.Equals(prefix, "xmlns", StringComparison.Ordinal))
            {
                namespaceUri = XmlNamespaces.Xmlns;
            }
            else
            {
                namespaceUri = context.LookupXmlNamespaceUri(prefix);
                if (namespaceUri == null)
                {
                    throw CreateXmlQualifiedNameError("The namespace prefix '" + prefix + "' is not bound in this attribute's context", XmlErrorCodes.NamespaceUnboundPrefix, range);
                }
            }
        }
        else if (string.Equals(parts.LocalName, "xmlns", StringComparison.Ordinal))
        {
            namespaceUri = XmlNamespaces.Xmlns;
        }

        return CreateResolvedXmlName(parts, namespaceUri);
    }

    internal void ResolveXmlAttributeNames(XmlAttributeData[] attributes, NamespaceContext context)
    {
        Guard.NotNull(attributes, "attributes");
        Guard.NotNull(context, "context");
        XmlName[] resolvedNames = new XmlName[attributes.Length];
        bool[] namespaceDeclarations = new bool[attributes.Length];
        int index = 0;
        while (index < attributes.Length)
        {
            XmlAttributeData? optionalAttribute = attributes[index];
            if (optionalAttribute == null)
            {
                throw new ArgumentException("An XML attribute cannot be null.", "attributes");
            }

            XmlName? optionalName = optionalAttribute.Name;
            if (optionalName == null)
            {
                throw new ArgumentException("An XML attribute requires a name.", "attributes");
            }

            string qualifiedName = XmlQualifiedNameText(optionalName);
            XmlName resolved = ResolveXmlAttributeName(qualifiedName, context, optionalAttribute.Range);
            bool namespaceDeclaration = optionalAttribute.IsNamespaceDeclaration || string.Equals(resolved.NamespaceUri, XmlNamespaces.Xmlns, StringComparison.Ordinal);
            resolvedNames[index] = resolved;
            namespaceDeclarations[index] = namespaceDeclaration;
            index = index + 1;
        }

        index = 0;
        while (index < attributes.Length)
        {
            if (!namespaceDeclarations[index])
            {
                int preceding = 0;
                while (preceding < index)
                {
                    if (!namespaceDeclarations[preceding] && SameExpandedXmlName(resolvedNames[preceding], resolvedNames[index]))
                    {
                        XmlAttributeData attribute = attributes[index];
                        throw CreateXmlQualifiedNameError("Two attributes on the same element have the expanded name '" + ExpandedXmlNameText(resolvedNames[index]) + "'", XmlErrorCodes.NamespaceDuplicateAttribute, attribute.Range);
                    }

                    preceding = preceding + 1;
                }
            }

            index = index + 1;
        }

        index = 0;
        while (index < attributes.Length)
        {
            attributes[index].Name = resolvedNames[index];
            index = index + 1;
        }
    }

    internal string XmlQualifiedNameText(XmlName name)
    {
        Guard.NotNull(name, "name");
        string? qualifiedName = name.QualifiedName;
        if (qualifiedName != null)
        {
            return qualifiedName;
        }

        string? localName = name.LocalName;
        if (localName == null)
        {
            throw new ArgumentException("An XML name requires a qualified or local name.", "name");
        }

        string? prefix = name.Prefix;
        return prefix == null ? localName : prefix + ":" + localName;
    }

    internal bool IsValidXmlNcName(string value)
    {
        Guard.NotNull(value, "value");
        if (value.Length == 0)
        {
            return false;
        }

        UnicodeScalarScanner scanner = new UnicodeScalarScanner(value.AsMemory());
        UnicodeScalar scalar = scanner.ReadUnicodeScalar();
        bool validStart = Handling == XmlNamespaceHandling.Namespaces10 ? Xml10CharacterRules.IsXml10NameStartCharacter(scalar.Value) : Xml11CharacterRules.IsXml11NameStartCharacter(scalar.Value);
        if (!validStart || scalar.Value == ':')
        {
            return false;
        }

        while (!scanner.IsUnicodeScalarEnd())
        {
            scalar = scanner.ReadUnicodeScalar();
            bool valid = Handling == XmlNamespaceHandling.Namespaces10 ? Xml10CharacterRules.IsXml10NameCharacter(scalar.Value) : Xml11CharacterRules.IsXml11NameCharacter(scalar.Value);
            if (!valid || scalar.Value == ':')
            {
                return false;
            }
        }

        return true;
    }

    internal bool SameExpandedXmlName(XmlName first, XmlName second)
    {
        Guard.NotNull(first, "first");
        Guard.NotNull(second, "second");
        return string.Equals(first.NamespaceUri, second.NamespaceUri, StringComparison.Ordinal) && string.Equals(first.LocalName, second.LocalName, StringComparison.Ordinal);
    }

    internal string ExpandedXmlNameText(XmlName name)
    {
        Guard.NotNull(name, "name");
        string namespaceUri = name.NamespaceUri == null ? "" : name.NamespaceUri;
        string localName = name.LocalName == null ? "" : name.LocalName;
        return "{" + namespaceUri + "}" + localName;
    }

    internal XmlName CreateResolvedXmlName(QualifiedNameParts parts, string? namespaceUri)
    {
        Guard.NotNull(parts, "parts");
        XmlName name = new XmlName();
        name.Prefix = parts.Prefix;
        name.LocalName = parts.LocalName;
        name.NamespaceUri = namespaceUri;
        name.QualifiedName = parts.QualifiedName;
        return name;
    }

    internal XmlNamespaceException CreateXmlQualifiedNameError(string message, string code, SourceRange? range)
    {
        SourceLocation? location = range == null ? null : range.Start;
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.Namespace;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = range;
        IXmlErrorReporter? reporter = ErrorReporter;
        if (reporter != null)
        {
            reporter.ReportXmlDiagnostic(diagnostic);
        }

        return new XmlNamespaceException(message, diagnostic, location);
    }
}
