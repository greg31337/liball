using System;
using System.Collections.Generic;

namespace xml;

internal class NamespaceElementResolution
{
    internal XmlName Name;
    internal NamespaceContext Context;
    internal XmlNamespaceBinding[] Declarations;
    internal XmlAttributeData[] Attributes;
    internal XmlAttributeData[] OrdinaryAttributes;

    internal NamespaceElementResolution(XmlName name, NamespaceContext context, XmlNamespaceBinding[] declarations, XmlAttributeData[] attributes, XmlAttributeData[] ordinaryAttributes)
    {
        Name = name;
        Context = context;
        Declarations = declarations;
        Attributes = attributes;
        OrdinaryAttributes = ordinaryAttributes;
    }
}

internal class NamespaceScopeProcessor
{
    internal XmlNamespaceHandling Handling;
    internal NamespaceDeclarationProcessor DeclarationProcessor;
    internal QualifiedNameParser NameParser;
    internal XmlNamespaceBinding[] InitialBindings;
    internal NamespaceContext Context;
    internal List<XmlName> ElementNames;
    internal List<XmlNamespaceBinding[]> ElementDeclarations;

    internal NamespaceScopeProcessor(XmlNamespaceHandling handling, IXmlErrorReporter? errorReporter, XmlNamespaceBinding[] inheritedBindings)
    {
        Guard.NotNull(inheritedBindings, "inheritedBindings");
        if (handling != XmlNamespaceHandling.Namespaces10 && handling != XmlNamespaceHandling.Namespaces11)
        {
            throw new ArgumentOutOfRangeException("handling");
        }

        Handling = handling;
        DeclarationProcessor = new NamespaceDeclarationProcessor(handling, errorReporter);
        NameParser = new QualifiedNameParser(handling, errorReporter);
        InitialBindings = CopyAndValidateInheritedXmlNamespaceBindings(inheritedBindings);
        Context = new NamespaceContext(null, InitialBindings);
        ElementNames = new List<XmlName>();
        ElementDeclarations = new List<XmlNamespaceBinding[]>();
    }

    internal NamespaceElementResolution ProcessXmlNamespaceStart(XmlName elementName, XmlAttributeData[] attributes, SourceRange? range)
    {
        Guard.NotNull(elementName, "elementName");
        Guard.NotNull(attributes, "attributes");
        NamespaceDeclarationResult declarations = DeclarationProcessor.ProcessXmlNamespaceDeclarations(Context, attributes);
        XmlName resolvedName = NameParser.ResolveXmlElementName(NameParser.XmlQualifiedNameText(elementName), declarations.Context, range);
        NameParser.ResolveXmlAttributeNames(attributes, declarations.Context);
        Context = declarations.Context;
        ElementNames.Add(resolvedName);
        ElementDeclarations.Add(declarations.Declarations);
        return new NamespaceElementResolution(resolvedName, declarations.Context, declarations.Declarations, attributes, declarations.OrdinaryAttributes);
    }

    internal XmlName ResolveXmlNamespaceEnd(XmlName elementName, SourceRange? range)
    {
        Guard.NotNull(elementName, "elementName");
        if (ElementNames.Count == 0)
        {
            throw new InvalidOperationException("There is no open namespace scope to close.");
        }

        XmlName resolvedName = NameParser.ResolveXmlElementName(NameParser.XmlQualifiedNameText(elementName), Context, range);
        XmlName expectedName = ElementNames[ElementNames.Count - 1];
        if (!NameParser.SameExpandedXmlName(expectedName, resolvedName))
        {
            throw NameParser.CreateXmlQualifiedNameError("The end element expanded name does not match the open element", XmlErrorCodes.NamespaceUnboundPrefix, range);
        }

        return resolvedName;
    }

    internal XmlNamespaceBinding[] CurrentXmlNamespaceDeclarations()
    {
        if (ElementDeclarations.Count == 0)
        {
            throw new InvalidOperationException("There is no open namespace scope.");
        }

        return ElementDeclarations[ElementDeclarations.Count - 1];
    }

    internal void PopXmlNamespaceScope()
    {
        if (ElementNames.Count == 0 || ElementDeclarations.Count == 0)
        {
            throw new InvalidOperationException("There is no open namespace scope to close.");
        }

        Context = Context.PopXmlNamespaceFrame();
        ElementNames.RemoveAt(ElementNames.Count - 1);
        ElementDeclarations.RemoveAt(ElementDeclarations.Count - 1);
    }

    internal void ResetXmlNamespaceScopes()
    {
        Context = new NamespaceContext(null, InitialBindings);
        ElementNames.Clear();
        ElementDeclarations.Clear();
    }

    internal XmlNamespaceBinding[] CopyAndValidateInheritedXmlNamespaceBindings(XmlNamespaceBinding[] inheritedBindings)
    {
        HashSet<string> prefixes = new HashSet<string>(StringComparer.Ordinal);
        XmlNamespaceBinding[] copies = new XmlNamespaceBinding[inheritedBindings.Length];
        int index = 0;
        while (index < inheritedBindings.Length)
        {
            XmlNamespaceBinding? optionalBinding = inheritedBindings[index];
            if (optionalBinding == null || optionalBinding.Prefix == null || optionalBinding.NamespaceUri == null)
            {
                throw new ArgumentException("An inherited namespace binding must be complete.", "inheritedBindings");
            }

            string prefix = optionalBinding.Prefix;
            string namespaceUri = optionalBinding.IsUndeclaration ? "" : optionalBinding.NamespaceUri;
            if (!prefixes.Add(prefix))
            {
                throw new ArgumentException("An inherited namespace prefix cannot occur more than once.", "inheritedBindings");
            }

            DeclarationProcessor.ValidateReservedXmlNamespaceBinding(prefix, namespaceUri, optionalBinding.Range);
            DeclarationProcessor.ValidateXmlNamespaceUndeclaration(prefix, namespaceUri, optionalBinding.Range);
            XmlNamespaceBinding copy = new XmlNamespaceBinding();
            copy.Prefix = prefix;
            copy.NamespaceUri = namespaceUri;
            copy.IsUndeclaration = namespaceUri.Length == 0;
            copy.Range = optionalBinding.Range;
            copies[index] = copy;
            index = index + 1;
        }

        return copies;
    }
}
