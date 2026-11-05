using System;
using System.Collections.Generic;

namespace xml;

internal class NamespaceDeclarationResult
{
    internal NamespaceContext Context;
    internal XmlNamespaceBinding[] Declarations;
    internal XmlAttributeData[] OrdinaryAttributes;

    internal NamespaceDeclarationResult(NamespaceContext context, XmlNamespaceBinding[] declarations, XmlAttributeData[] ordinaryAttributes)
    {
        Context = context;
        Declarations = declarations;
        OrdinaryAttributes = ordinaryAttributes;
    }
}

internal class NamespaceDeclarationProcessor
{
    internal XmlNamespaceHandling Handling;
    internal IXmlErrorReporter? ErrorReporter;

    internal NamespaceDeclarationProcessor(XmlNamespaceHandling handling, IXmlErrorReporter? errorReporter)
    {
        if (handling != XmlNamespaceHandling.Namespaces10 && handling != XmlNamespaceHandling.Namespaces11)
        {
            throw new ArgumentOutOfRangeException("handling");
        }

        Handling = handling;
        ErrorReporter = errorReporter;
    }

    internal NamespaceDeclarationResult ProcessXmlNamespaceDeclarations(NamespaceContext parent, XmlAttributeData[] attributes)
    {
        Guard.NotNull(parent, "parent");
        Guard.NotNull(attributes, "attributes");
        List<XmlNamespaceBinding> declarations = new List<XmlNamespaceBinding>();
        List<XmlAttributeData> ordinaryAttributes = new List<XmlAttributeData>();
        List<XmlAttributeData> declarationAttributes = new List<XmlAttributeData>();
        HashSet<string> declaredPrefixes = new HashSet<string>(StringComparer.Ordinal);
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

            string? prefix = DeclaredXmlNamespacePrefix(optionalName, optionalAttribute.Range);
            if (prefix == null)
            {
                ordinaryAttributes.Add(optionalAttribute);
            }
            else
            {
                string? namespaceUri = optionalAttribute.Value;
                if (namespaceUri == null)
                {
                    throw new ArgumentException("A namespace declaration requires a normalized value.", "attributes");
                }

                if (!declaredPrefixes.Add(prefix))
                {
                    throw CreateXmlNamespaceDeclarationError("The namespace prefix '" + prefix + "' is declared more than once on the same element", XmlErrorCodes.NamespaceDuplicateAttribute, optionalAttribute.Range);
                }

                ValidateReservedXmlNamespaceBinding(prefix, namespaceUri, optionalAttribute.Range);
                ValidateXmlNamespaceUndeclaration(prefix, namespaceUri, optionalAttribute.Range);
                XmlNamespaceBinding binding = new XmlNamespaceBinding();
                binding.Prefix = prefix;
                binding.NamespaceUri = namespaceUri;
                binding.IsUndeclaration = namespaceUri.Length == 0;
                binding.Range = optionalAttribute.Range;
                declarations.Add(binding);
                declarationAttributes.Add(optionalAttribute);
            }

            index = index + 1;
        }

        index = 0;
        while (index < declarationAttributes.Count)
        {
            XmlAttributeData attribute = declarationAttributes[index];
            XmlNamespaceBinding binding = declarations[index];
            string prefix = binding.Prefix == null ? "" : binding.Prefix;
            attribute.IsNamespaceDeclaration = true;
            XmlName name = attribute.Name == null ? new XmlName() : attribute.Name;
            name.Prefix = prefix.Length == 0 ? null : "xmlns";
            name.LocalName = prefix.Length == 0 ? "xmlns" : prefix;
            name.NamespaceUri = XmlNamespaces.Xmlns;
            attribute.Name = name;
            index = index + 1;
        }

        XmlNamespaceBinding[] declarationArray = declarations.ToArray();
        NamespaceContext context = parent.PushXmlNamespaceFrame(declarationArray);
        return new NamespaceDeclarationResult(context, declarationArray, ordinaryAttributes.ToArray());
    }

    internal string? DeclaredXmlNamespacePrefix(XmlName name, SourceRange? range)
    {
        Guard.NotNull(name, "name");
        string? qualifiedName = name.QualifiedName;
        if (qualifiedName != null)
        {
            if (string.Equals(qualifiedName, "xmlns", StringComparison.Ordinal))
            {
                return "";
            }

            if (qualifiedName.StartsWith("xmlns:", StringComparison.Ordinal))
            {
                string prefix = qualifiedName.Substring(6);
                if (!IsValidXmlNamespacePrefix(prefix))
                {
                    throw CreateXmlNamespaceDeclarationError("A prefixed namespace declaration requires exactly one valid NCName after 'xmlns:'", XmlErrorCodes.NamespaceReservedBinding, range);
                }

                return prefix;
            }

            return null;
        }

        if (string.Equals(name.Prefix, "xmlns", StringComparison.Ordinal))
        {
            string? localName = name.LocalName;
            if (localName == null || !IsValidXmlNamespacePrefix(localName))
            {
                throw CreateXmlNamespaceDeclarationError("A prefixed namespace declaration requires a valid NCName local name", XmlErrorCodes.NamespaceReservedBinding, range);
            }

            return localName;
        }

        if (name.Prefix == null && string.Equals(name.LocalName, "xmlns", StringComparison.Ordinal))
        {
            return "";
        }

        return null;
    }

    internal bool IsValidXmlNamespacePrefix(string prefix)
    {
        Guard.NotNull(prefix, "prefix");
        if (prefix.Length == 0)
        {
            return false;
        }

        UnicodeScalarScanner scanner = new UnicodeScalarScanner(prefix.AsMemory());
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

    internal void ValidateReservedXmlNamespaceBinding(string prefix, string namespaceUri, SourceRange? range)
    {
        Guard.NotNull(prefix, "prefix");
        Guard.NotNull(namespaceUri, "namespaceUri");
        if (string.Equals(prefix, "xml", StringComparison.Ordinal))
        {
            if (!string.Equals(namespaceUri, XmlNamespaces.Xml, StringComparison.Ordinal))
            {
                throw CreateXmlNamespaceDeclarationError("The 'xml' prefix must be bound only to its reserved namespace name and cannot be undeclared", XmlErrorCodes.NamespaceReservedBinding, range);
            }

            return;
        }

        if (string.Equals(prefix, "xmlns", StringComparison.Ordinal))
        {
            throw CreateXmlNamespaceDeclarationError("The reserved 'xmlns' prefix cannot be declared or undeclared", XmlErrorCodes.NamespaceReservedBinding, range);
        }

        if (string.Equals(namespaceUri, XmlNamespaces.Xml, StringComparison.Ordinal))
        {
            throw CreateXmlNamespaceDeclarationError("Only the 'xml' prefix can be bound to the reserved XML namespace name", XmlErrorCodes.NamespaceReservedBinding, range);
        }

        if (string.Equals(namespaceUri, XmlNamespaces.Xmlns, StringComparison.Ordinal))
        {
            throw CreateXmlNamespaceDeclarationError("The reserved XML namespace-declaration name cannot be bound to a prefix or used as the default namespace", XmlErrorCodes.NamespaceReservedBinding, range);
        }
    }

    internal void ValidateXmlNamespaceUndeclaration(string prefix, string namespaceUri, SourceRange? range)
    {
        Guard.NotNull(prefix, "prefix");
        Guard.NotNull(namespaceUri, "namespaceUri");
        if (Handling == XmlNamespaceHandling.Namespaces10 && prefix.Length > 0 && namespaceUri.Length == 0)
        {
            throw CreateXmlNamespaceDeclarationError("Namespaces in XML 1.0 does not allow a prefix to be undeclared", XmlErrorCodes.NamespaceReservedBinding, range);
        }
    }

    internal XmlNamespaceException CreateXmlNamespaceDeclarationError(string message, string code, SourceRange? range)
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
