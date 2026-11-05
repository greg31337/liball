using System.Collections.Generic;

namespace xml;

internal class XmlSchemaAttributeCompiler
{
    internal XmlSchemaComplexTypeCompiler Compiler;

    internal XmlSchemaAttributeCompiler(XmlSchemaComplexTypeCompiler compiler)
    {
        Guard.NotNull(compiler, "compiler");
        Compiler = compiler;
    }

    internal CompiledXmlSchemaAttributeSet CompileXmlSchemaAttributeSet(XmlNode parent, XmlSchemaModule? module, CompiledXmlSchemaAttributeGroup? currentGroup)
    {
        List<CompiledXmlSchemaAttributeUse> attributes = new List<CompiledXmlSchemaAttributeUse>();
        CompiledXmlSchemaWildcard? wildcard = null;
        XmlNode[] children = parent.Children == null ? [] : parent.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (Compiler.IsXmlSchemaElement(child, "attribute"))
            {
                AddXmlSchemaAttributeUse(attributes, CompileXmlSchemaAttributeUse(child, module), child);
            }
            else if (Compiler.IsXmlSchemaElement(child, "attributeGroup"))
            {
                string? referenceValue = Compiler.FindXmlSchemaAttribute(child, "ref", "");
                if (referenceValue == null)
                {
                    throw Compiler.CreateXmlSchemaComplexTypeError(child, "A local attributeGroup requires a ref attribute.");
                }

                CompiledXmlSchemaAttributeGroup referenced = Compiler.ResolveXmlSchemaAttributeGroupReference(referenceValue, child, module, currentGroup);
                Compiler.CompileXmlSchemaAttributeGroup(referenced);
                int referencedIndex = 0;
                while (referencedIndex < referenced.Attributes.Length)
                {
                    AddXmlSchemaAttributeUse(attributes, referenced.Attributes[referencedIndex], child);
                    referencedIndex = referencedIndex + 1;
                }

                if (referenced.Wildcard != null)
                {
                    wildcard = CombineXmlSchemaAttributeWildcards(wildcard, referenced.Wildcard, XmlSchemaWildcardCombination.Intersection);
                }
            }
            else if (Compiler.IsXmlSchemaElement(child, "anyAttribute"))
            {
                CompiledXmlSchemaWildcard declared = new XmlSchemaParticleCompiler(Compiler).CompileXmlSchemaWildcard(child, module);
                wildcard = CombineXmlSchemaAttributeWildcards(wildcard, declared, XmlSchemaWildcardCombination.Intersection);
            }

            index = index + 1;
        }

        if (currentGroup == null)
        {
            CompiledXmlSchemaAttributeGroup? defaultGroup = ResolveDefaultXmlSchemaAttributeGroup(parent, module);
            if (defaultGroup != null)
            {
                Compiler.CompileXmlSchemaAttributeGroup(defaultGroup);
                int defaultIndex = 0;
                while (defaultIndex < defaultGroup.Attributes.Length)
                {
                    AddXmlSchemaAttributeUse(attributes, defaultGroup.Attributes[defaultIndex], parent);
                    defaultIndex = defaultIndex + 1;
                }

                if (defaultGroup.Wildcard != null)
                {
                    wildcard = CombineXmlSchemaAttributeWildcards(wildcard, defaultGroup.Wildcard, XmlSchemaWildcardCombination.Intersection);
                }
            }
        }

        CompiledXmlSchemaAttributeSet result = new CompiledXmlSchemaAttributeSet();
        result.Attributes = attributes.ToArray();
        result.Wildcard = wildcard;
        return result;
    }

    internal CompiledXmlSchemaAttributeGroup? ResolveDefaultXmlSchemaAttributeGroup(XmlNode parent, XmlSchemaModule? module)
    {
        if (module == null)
        {
            return null;
        }

        XmlNode? complexType = parent;
        while (complexType != null && !Compiler.IsXmlSchemaElement(complexType, "complexType"))
        {
            complexType = complexType.Parent;
        }

        if (complexType == null || !Compiler.ParseOptionalXmlSchemaBoolean(complexType, "defaultAttributesApply", true))
        {
            return null;
        }

        XmlNode? root = module.Document.DocumentElement;
        if (root == null)
        {
            return null;
        }

        string? referenceValue = Compiler.FindXmlSchemaAttribute(root, "defaultAttributes", "");
        if (referenceValue == null)
        {
            return null;
        }

        return Compiler.ResolveXmlSchemaAttributeGroupReference(referenceValue, root, module, null);
    }

    internal CompiledXmlSchemaWildcard? CombineXmlSchemaAttributeWildcards(CompiledXmlSchemaWildcard? left, CompiledXmlSchemaWildcard? right, XmlSchemaWildcardCombination combination)
    {
        if (left == null)
        {
            return right;
        }

        if (right == null)
        {
            return left;
        }

        CompiledXmlSchemaWildcard result = new CompiledXmlSchemaWildcard(null);
        result.Combination = combination;
        result.Operands = [left, right];
        result.ProcessContents = left.ProcessContents > right.ProcessContents ? left.ProcessContents : right.ProcessContents;
        return result;
    }

    internal CompiledXmlSchemaAttributeUse CompileXmlSchemaAttributeUse(XmlNode node, XmlSchemaModule? module)
    {
        string? nameValue = Compiler.FindXmlSchemaAttribute(node, "name", "");
        string? referenceValue = Compiler.FindXmlSchemaAttribute(node, "ref", "");
        if (nameValue == null && referenceValue == null || nameValue != null && referenceValue != null)
        {
            throw Compiler.CreateXmlSchemaComplexTypeError(node, "An attribute use requires exactly one name or ref attribute.");
        }

        XmlName name;
        bool reference = referenceValue != null;
        if (referenceValue != null)
        {
            name = Compiler.ResolveXmlSchemaComponentName(referenceValue, node, module);
        }
        else
        {
            string localName = nameValue == null ? "" : nameValue;
            if (!new XdmStringDatatype().IsValidXdmStringLexical(localName, XdmAtomicType.NcName))
            {
                throw Compiler.CreateXmlSchemaComplexTypeError(node, "A local attribute requires a valid NCName name.");
            }

            string? namespaceUri = module == null ? null : Compiler.ResolveXmlSchemaLocalDeclarationNamespace(node, module, "attributeFormDefault");
            name = Compiler.CreateXmlSchemaComponentName(localName, namespaceUri, null);
        }

        CompiledXmlSchemaAttributeUse result = new CompiledXmlSchemaAttributeUse(name, node);
        result.IsReference = reference;
        result.Use = ParseXmlSchemaAttributeUse(node);
        result.DefaultValue = Compiler.FindXmlSchemaAttribute(node, "default", "");
        result.FixedValue = Compiler.FindXmlSchemaAttribute(node, "fixed", "");
        if (result.DefaultValue != null && result.FixedValue != null)
        {
            throw Compiler.CreateXmlSchemaComplexTypeError(node, "An attribute use cannot specify both default and fixed values.");
        }

        if (result.Use == XmlSchemaAttributeUseKind.Required && result.DefaultValue != null || result.Use == XmlSchemaAttributeUseKind.Prohibited && (result.DefaultValue != null || result.FixedValue != null))
        {
            throw Compiler.CreateXmlSchemaComplexTypeError(node, "The attribute value constraint is incompatible with its use.");
        }

        string? typeValue = Compiler.FindXmlSchemaAttribute(node, "type", "");
        XmlNode? inlineTypeNode = Compiler.FindSingleXmlSchemaChild(node, "simpleType");
        if (reference && (typeValue != null || inlineTypeNode != null))
        {
            throw Compiler.CreateXmlSchemaComplexTypeError(node, "A referenced attribute cannot declare a local type.");
        }

        if (typeValue != null && inlineTypeNode != null)
        {
            throw Compiler.CreateXmlSchemaComplexTypeError(node, "An attribute cannot specify both type and inline simpleType.");
        }

        if (!reference)
        {
            if (inlineTypeNode != null)
            {
                result.Type = Compiler.SimpleCompiler.CompileAnonymousXmlSchemaSimpleType(inlineTypeNode, module);
            }
            else if (typeValue != null)
            {
                XmlName typeName = Compiler.ResolveXmlSchemaComponentName(typeValue, node, module);
                result.Type = Compiler.FindXmlSchemaSimpleType(typeName);
                if (result.Type == null)
                {
                    throw Compiler.CreateXmlSchemaComplexTypeError(node, "The attribute type does not name a declared simple type.");
                }

                Compiler.SimpleCompiler.CompileUserXmlSchemaSimpleType(result.Type);
            }
            else
            {
                XmlName stringName = Compiler.CreateXmlSchemaComponentName("string", XmlNamespaces.XmlSchema, "xs");
                result.Type = Compiler.FindXmlSchemaSimpleType(stringName);
            }
        }

        return result;
    }

    internal XmlSchemaAttributeUseKind ParseXmlSchemaAttributeUse(XmlNode node)
    {
        string? value = Compiler.FindXmlSchemaAttribute(node, "use", "");
        if (value == null || value == "optional")
        {
            return XmlSchemaAttributeUseKind.Optional;
        }

        if (value == "required")
        {
            return XmlSchemaAttributeUseKind.Required;
        }

        if (value == "prohibited")
        {
            return XmlSchemaAttributeUseKind.Prohibited;
        }

        throw Compiler.CreateXmlSchemaComplexTypeError(node, "Attribute use must be optional, required, or prohibited.");
    }

    internal void AddXmlSchemaAttributeUse(List<CompiledXmlSchemaAttributeUse> attributes, CompiledXmlSchemaAttributeUse candidate, XmlNode node)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (Compiler.SameXmlSchemaComponentName(attributes[index].Name, candidate.Name))
            {
                throw Compiler.CreateXmlSchemaComplexTypeError(node, "An attribute use is declared more than once in the same attribute set.");
            }

            index = index + 1;
        }

        attributes.Add(candidate);
    }
}
