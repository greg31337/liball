using System;
using System.Collections.Generic;
using System.Numerics;

namespace xml;

internal class XmlSchemaParticleCompiler
{
    internal XmlSchemaComplexTypeCompiler Compiler;

    internal XmlSchemaParticleCompiler(XmlSchemaComplexTypeCompiler compiler)
    {
        Guard.NotNull(compiler, "compiler");
        Compiler = compiler;
    }

    internal XmlNode? FindSingleXmlSchemaParticle(XmlNode parent, bool groupDefinition)
    {
        XmlNode? found = null;
        XmlNode[] children = parent.Children == null ? [] : parent.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            bool compositor = Compiler.IsXmlSchemaElement(child, "sequence") || Compiler.IsXmlSchemaElement(child, "choice") || Compiler.IsXmlSchemaElement(child, "all");
            bool particle = compositor || !groupDefinition && Compiler.IsXmlSchemaElement(child, "group");
            if (particle)
            {
                if (found != null)
                {
                    throw Compiler.CreateXmlSchemaComplexTypeError(child, "Only one top-level content particle is permitted.");
                }

                found = child;
            }

            index = index + 1;
        }

        return found;
    }

    internal CompiledXmlSchemaParticle CompileXmlSchemaParticle(XmlNode node, XmlSchemaModule? module, CompiledXmlSchemaGroup? currentGroup)
    {
        CompiledXmlSchemaParticle result;
        if (Compiler.IsXmlSchemaElement(node, "element"))
        {
            result = CompileXmlSchemaElementParticle(node, module);
        }
        else if (Compiler.IsXmlSchemaElement(node, "any"))
        {
            result = new CompiledXmlSchemaParticle(XmlSchemaParticleKind.Any, node);
            result.Wildcard = CompileXmlSchemaWildcard(node, module);
        }
        else if (Compiler.IsXmlSchemaElement(node, "group"))
        {
            result = CompileXmlSchemaGroupParticle(node, module, currentGroup);
        }
        else
        {
            result = CompileXmlSchemaCompositor(node, module, currentGroup);
        }

        CompileXmlSchemaOccurrence(result, node);
        return result;
    }

    internal CompiledXmlSchemaParticle CompileXmlSchemaElementParticle(XmlNode node, XmlSchemaModule? module)
    {
        string? nameValue = Compiler.FindXmlSchemaAttribute(node, "name", "");
        string? referenceValue = Compiler.FindXmlSchemaAttribute(node, "ref", "");
        if (nameValue == null && referenceValue == null || nameValue != null && referenceValue != null)
        {
            throw Compiler.CreateXmlSchemaComplexTypeError(node, "An element particle requires exactly one name or ref attribute.");
        }

        CompiledXmlSchemaParticle result = new CompiledXmlSchemaParticle(XmlSchemaParticleKind.Element, node);
        if (referenceValue != null)
        {
            result.Name = Compiler.ResolveXmlSchemaComponentName(referenceValue, node, module);
            result.IsReference = true;
        }
        else
        {
            string localName = nameValue == null ? "" : nameValue;
            if (!new XdmStringDatatype().IsValidXdmStringLexical(localName, XdmAtomicType.NcName))
            {
                throw Compiler.CreateXmlSchemaComplexTypeError(node, "A local element requires a valid NCName name.");
            }

            string? namespaceUri = module == null ? null : Compiler.ResolveXmlSchemaLocalDeclarationNamespace(node, module, "elementFormDefault");
            result.Name = Compiler.CreateXmlSchemaComponentName(localName, namespaceUri, null);
        }

        return result;
    }

    internal CompiledXmlSchemaParticle CompileXmlSchemaGroupParticle(XmlNode node, XmlSchemaModule? module, CompiledXmlSchemaGroup? currentGroup)
    {
        string? referenceValue = Compiler.FindXmlSchemaAttribute(node, "ref", "");
        if (referenceValue == null)
        {
            throw Compiler.CreateXmlSchemaComplexTypeError(node, "A local model-group particle requires a ref attribute.");
        }

        CompiledXmlSchemaGroup group = Compiler.ResolveXmlSchemaGroupReference(referenceValue, node, module, currentGroup);
        Compiler.CompileXmlSchemaGroup(group);
        CompiledXmlSchemaParticle result = new CompiledXmlSchemaParticle(XmlSchemaParticleKind.GroupReference, node);
        result.Name = group.Name;
        result.IsReference = true;
        result.GroupDefinition = group;
        if (group.Particle != null)
        {
            result.Children = [group.Particle];
        }

        return result;
    }

    internal CompiledXmlSchemaParticle CompileXmlSchemaCompositor(XmlNode node, XmlSchemaModule? module, CompiledXmlSchemaGroup? currentGroup)
    {
        XmlSchemaParticleKind kind;
        if (Compiler.IsXmlSchemaElement(node, "sequence"))
        {
            kind = XmlSchemaParticleKind.Sequence;
        }
        else if (Compiler.IsXmlSchemaElement(node, "choice"))
        {
            kind = XmlSchemaParticleKind.Choice;
        }
        else if (Compiler.IsXmlSchemaElement(node, "all"))
        {
            kind = XmlSchemaParticleKind.All;
        }
        else
        {
            throw Compiler.CreateXmlSchemaComplexTypeError(node, "An unsupported content particle was encountered.");
        }

        List<CompiledXmlSchemaParticle> particles = new List<CompiledXmlSchemaParticle>();
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            bool supported = Compiler.IsXmlSchemaElement(child, "element") || Compiler.IsXmlSchemaElement(child, "any") || Compiler.IsXmlSchemaElement(child, "group") || Compiler.IsXmlSchemaElement(child, "sequence") || Compiler.IsXmlSchemaElement(child, "choice") || Compiler.IsXmlSchemaElement(child, "all");
            if (supported)
            {
                CompiledXmlSchemaParticle particle = CompileXmlSchemaParticle(child, module, currentGroup);
                bool nestedCompositor = particle.Kind == XmlSchemaParticleKind.Sequence || particle.Kind == XmlSchemaParticleKind.Choice || particle.Kind == XmlSchemaParticleKind.All;
                if (kind == XmlSchemaParticleKind.All && nestedCompositor)
                {
                    throw Compiler.CreateXmlSchemaComplexTypeError(child, "An all particle cannot directly contain another compositor.");
                }

                if (kind != XmlSchemaParticleKind.All && particle.Kind == XmlSchemaParticleKind.All)
                {
                    throw Compiler.CreateXmlSchemaComplexTypeError(child, "An all particle cannot be nested directly in sequence or choice.");
                }

                if (kind == XmlSchemaParticleKind.All && particle.Kind == XmlSchemaParticleKind.Element && (particle.MaxOccursUnbounded || particle.MaxOccurs > BigInteger.One))
                {
                    throw Compiler.CreateXmlSchemaComplexTypeError(child, "Element children of an all particle cannot have maxOccurs greater than one.");
                }

                particles.Add(particle);
            }

            index = index + 1;
        }

        CompiledXmlSchemaParticle result = new CompiledXmlSchemaParticle(kind, node);
        result.Children = particles.ToArray();
        return result;
    }

    internal void CompileXmlSchemaOccurrence(CompiledXmlSchemaParticle particle, XmlNode node)
    {
        string? minimumValue = Compiler.FindXmlSchemaAttribute(node, "minOccurs", "");
        string? maximumValue = Compiler.FindXmlSchemaAttribute(node, "maxOccurs", "");
        if (minimumValue != null)
        {
            particle.MinOccurs = ParseXmlSchemaOccurrenceInteger(node, minimumValue, "minOccurs");
        }

        if (maximumValue == "unbounded")
        {
            particle.MaxOccursUnbounded = true;
        }
        else if (maximumValue != null)
        {
            particle.MaxOccurs = ParseXmlSchemaOccurrenceInteger(node, maximumValue, "maxOccurs");
        }

        if (!particle.MaxOccursUnbounded && particle.MinOccurs > particle.MaxOccurs)
        {
            throw Compiler.CreateXmlSchemaComplexTypeError(node, "minOccurs cannot exceed maxOccurs.");
        }

        if (particle.Kind == XmlSchemaParticleKind.All && (particle.MaxOccursUnbounded || particle.MaxOccurs > BigInteger.One))
        {
            throw Compiler.CreateXmlSchemaComplexTypeError(node, "An all particle cannot have maxOccurs greater than one.");
        }
    }

    internal BigInteger ParseXmlSchemaOccurrenceInteger(XmlNode node, string value, string attributeName)
    {
        XdmNumericParseResult parsed = new XdmNumericDatatype().TryParseXdmInteger(value, XdmAtomicType.NonNegativeInteger);
        if (!parsed.Success || parsed.Value == null)
        {
            throw Compiler.CreateXmlSchemaComplexTypeError(node, "The " + attributeName + " attribute requires a non-negative integer.");
        }

        return new XdmNumericDatatype().ReadXdmIntegerValue(parsed.Value);
    }

    internal CompiledXmlSchemaWildcard CompileXmlSchemaWildcard(XmlNode node, XmlSchemaModule? module)
    {
        CompiledXmlSchemaWildcard result = new CompiledXmlSchemaWildcard(node);
        string? namespaceValue = Compiler.FindXmlSchemaAttribute(node, "namespace", "");
        string? notNamespaceValue = Compiler.FindXmlSchemaAttribute(node, "notNamespace", "");
        if (namespaceValue != null && notNamespaceValue != null)
        {
            throw Compiler.CreateXmlSchemaComplexTypeError(node, "A wildcard cannot specify both namespace and notNamespace.");
        }

        if (notNamespaceValue != null)
        {
            result.Kind = XmlSchemaWildcardKind.Exclusion;
            result.ExcludedNamespaces = ParseXmlSchemaWildcardNamespaces(node, notNamespaceValue, module, false);
        }
        else if (namespaceValue == null || new XdmStringDatatype().CollapseXdmStringWhitespace(namespaceValue) == "##any")
        {
            result.Kind = XmlSchemaWildcardKind.Any;
        }
        else if (new XdmStringDatatype().CollapseXdmStringWhitespace(namespaceValue) == "##other")
        {
            result.Kind = XmlSchemaWildcardKind.Other;
            string targetNamespace = module == null || module.EffectiveTargetNamespace == null ? "" : module.EffectiveTargetNamespace;
            result.ExcludedNamespaces = [targetNamespace, ""];
        }
        else
        {
            result.Kind = XmlSchemaWildcardKind.Enumeration;
            result.Namespaces = ParseXmlSchemaWildcardNamespaces(node, namespaceValue, module, true);
        }

        string? processValue = Compiler.FindXmlSchemaAttribute(node, "processContents", "");
        if (processValue == null || processValue == "strict")
        {
            result.ProcessContents = XmlSchemaProcessContents.Strict;
        }
        else if (processValue == "lax")
        {
            result.ProcessContents = XmlSchemaProcessContents.Lax;
        }
        else if (processValue == "skip")
        {
            result.ProcessContents = XmlSchemaProcessContents.Skip;
        }
        else
        {
            throw Compiler.CreateXmlSchemaComplexTypeError(node, "processContents must be strict, lax, or skip.");
        }

        CompileXmlSchemaNotQName(result, node, module);
        return result;
    }

    internal string[] ParseXmlSchemaWildcardNamespaces(XmlNode node, string value, XmlSchemaModule? module, bool allowOther)
    {
        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(value);
        string[] tokens = normalized.Length == 0 ? [] : normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            throw Compiler.CreateXmlSchemaComplexTypeError(node, "A wildcard namespace list cannot be empty.");
        }

        List<string> namespaces = new List<string>();
        int index = 0;
        while (index < tokens.Length)
        {
            string token = tokens[index];
            string namespaceUri;
            if (token == "##targetNamespace")
            {
                namespaceUri = module == null || module.EffectiveTargetNamespace == null ? "" : module.EffectiveTargetNamespace;
            }
            else if (token == "##local")
            {
                namespaceUri = "";
            }
            else if (token == "##any" || token == "##other" || token.StartsWith("##", StringComparison.Ordinal))
            {
                if (allowOther && tokens.Length == 1 && token == "##other")
                {
                    namespaceUri = token;
                }
                else
                {
                    throw Compiler.CreateXmlSchemaComplexTypeError(node, "The wildcard namespace list contains an invalid token.");
                }
            }
            else if (!Uri.IsWellFormedUriString(token, UriKind.Absolute))
            {
                throw Compiler.CreateXmlSchemaComplexTypeError(node, "Wildcard namespace names must be absolute URI references.");
            }
            else
            {
                namespaceUri = token;
            }

            if (!namespaces.Contains(namespaceUri))
            {
                namespaces.Add(namespaceUri);
            }

            index = index + 1;
        }

        return namespaces.ToArray();
    }

    internal void CompileXmlSchemaNotQName(CompiledXmlSchemaWildcard wildcard, XmlNode node, XmlSchemaModule? module)
    {
        string? value = Compiler.FindXmlSchemaAttribute(node, "notQName", "");
        if (value == null)
        {
            return;
        }

        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(value);
        string[] tokens = normalized.Length == 0 ? [] : normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            throw Compiler.CreateXmlSchemaComplexTypeError(node, "notQName cannot be empty.");
        }

        List<XmlName> names = new List<XmlName>();
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index] == "##defined")
            {
                wildcard.ExcludesDefinedNames = true;
            }
            else if (tokens[index] == "##definedSibling")
            {
                if (Compiler.IsXmlSchemaElement(node, "anyAttribute"))
                {
                    throw Compiler.CreateXmlSchemaComplexTypeError(node, "##definedSibling is not permitted on an attribute wildcard.");
                }

                wildcard.ExcludesDefinedSiblingNames = true;
            }
            else if (tokens[index].StartsWith("##", StringComparison.Ordinal))
            {
                throw Compiler.CreateXmlSchemaComplexTypeError(node, "notQName contains an invalid special token.");
            }
            else
            {
                names.Add(Compiler.ResolveXmlSchemaComponentName(tokens[index], node, module));
            }

            index = index + 1;
        }

        wildcard.ExcludedNames = names.ToArray();
    }
}
