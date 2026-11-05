using System;
using System.Collections.Generic;

namespace xml;

internal class XmlSchemaDeclarationCompiler
{
    internal XmlSchemaModule[] Modules;
    internal XmlSchemaSimpleTypeCompiler SimpleCompiler;
    internal XmlSchemaComplexTypeCompiler ComplexCompiler;
    internal List<CompiledXmlSchemaElementDeclaration> Elements;
    internal List<CompiledXmlSchemaAttributeDeclaration> Attributes;
    internal List<CompiledXmlSchemaIdentityConstraint> IdentityConstraints;
    internal List<CompiledXmlSchemaNotation> Notations;
    internal Dictionary<string, CompiledXmlSchemaElementDeclaration> ElementsByName;
    internal Dictionary<string, CompiledXmlSchemaAttributeDeclaration> AttributesByName;
    internal Dictionary<string, CompiledXmlSchemaIdentityConstraint> IdentityConstraintsByName;
    internal Dictionary<string, CompiledXmlSchemaNotation> NotationsByName;

    internal XmlSchemaDeclarationCompiler(XmlSchemaModule[] modules, XmlSchemaSimpleTypeCompiler simpleCompiler, XmlSchemaComplexTypeCompiler complexCompiler)
    {
        Guard.NotNull(modules, "modules");
        Guard.NotNull(simpleCompiler, "simpleCompiler");
        Guard.NotNull(complexCompiler, "complexCompiler");
        Modules = modules;
        SimpleCompiler = simpleCompiler;
        ComplexCompiler = complexCompiler;
        Elements = new List<CompiledXmlSchemaElementDeclaration>();
        Attributes = new List<CompiledXmlSchemaAttributeDeclaration>();
        IdentityConstraints = new List<CompiledXmlSchemaIdentityConstraint>();
        Notations = new List<CompiledXmlSchemaNotation>();
        ElementsByName = new Dictionary<string, CompiledXmlSchemaElementDeclaration>(StringComparer.Ordinal);
        AttributesByName = new Dictionary<string, CompiledXmlSchemaAttributeDeclaration>(StringComparer.Ordinal);
        IdentityConstraintsByName = new Dictionary<string, CompiledXmlSchemaIdentityConstraint>(StringComparer.Ordinal);
        NotationsByName = new Dictionary<string, CompiledXmlSchemaNotation>(StringComparer.Ordinal);
    }

    internal void CompileXmlSchemaDeclarations()
    {
        ValidateXmlSchemaDeclarationDefaults();
        CollectGlobalXmlSchemaDeclarations();
        CollectReplacedXmlSchemaDeclarations();

        int attributeIndex = 0;
        while (attributeIndex < Attributes.Count)
        {
            CompileGlobalXmlSchemaAttribute(Attributes[attributeIndex]);
            attributeIndex = attributeIndex + 1;
        }

        int elementIndex = 0;
        while (elementIndex < Elements.Count)
        {
            CompileGlobalXmlSchemaElement(Elements[elementIndex]);
            elementIndex = elementIndex + 1;
        }

        BindAllXmlSchemaLocalDeclarations();
        ResolveXmlSchemaSubstitutionGroups();
        ResolveXmlSchemaIdentityConstraintReferences();
    }

    internal void ValidateXmlSchemaDeclarationDefaults()
    {
        int index = 0;
        while (index < Modules.Length)
        {
            XmlNode? root = Modules[index].Document.DocumentElement;
            if (root != null)
            {
                ValidateXmlSchemaFormValue(root, "elementFormDefault");
                ValidateXmlSchemaFormValue(root, "attributeFormDefault");
            }

            index = index + 1;
        }
    }

    internal void ValidateXmlSchemaFormValue(XmlNode root, string localName)
    {
        string? value = ComplexCompiler.FindXmlSchemaAttribute(root, localName, "");
        if (value != null && value != "qualified" && value != "unqualified")
        {
            throw CreateXmlSchemaDeclarationError(root, "Schema form values must be qualified or unqualified.");
        }
    }

    internal void CollectGlobalXmlSchemaDeclarations()
    {
        int moduleIndex = 0;
        while (moduleIndex < Modules.Length)
        {
            XmlSchemaModule module = Modules[moduleIndex];
            XmlNode? root = module.Document.DocumentElement;
            XmlNode[] children = root == null || root.Children == null ? [] : root.Children;
            int childIndex = 0;
            while (childIndex < children.Length)
            {
                CollectOneGlobalXmlSchemaDeclaration(children[childIndex], module, false);
                childIndex = childIndex + 1;
            }

            moduleIndex = moduleIndex + 1;
        }
    }

    internal void CollectReplacedXmlSchemaDeclarations()
    {
        int moduleIndex = 0;
        while (moduleIndex < Modules.Length)
        {
            XmlSchemaModule module = Modules[moduleIndex];
            int directiveIndex = 0;
            while (directiveIndex < module.Directives.Length)
            {
                XmlSchemaDirective directive = module.Directives[directiveIndex];
                if (directive.Kind == XmlSchemaDirectiveKind.Override)
                {
                    XmlNode[] children = directive.Node.Children == null ? [] : directive.Node.Children;
                    int childIndex = 0;
                    while (childIndex < children.Length)
                    {
                        CollectOneGlobalXmlSchemaDeclaration(children[childIndex], module, true);
                        childIndex = childIndex + 1;
                    }
                }

                directiveIndex = directiveIndex + 1;
            }

            moduleIndex = moduleIndex + 1;
        }
    }

    internal void CollectOneGlobalXmlSchemaDeclaration(XmlNode node, XmlSchemaModule module, bool replacement)
    {
        if (ComplexCompiler.IsXmlSchemaElement(node, "element"))
        {
            AddGlobalXmlSchemaElement(node, module, replacement);
        }
        else if (ComplexCompiler.IsXmlSchemaElement(node, "attribute"))
        {
            AddGlobalXmlSchemaAttribute(node, module, replacement);
        }
        else if (ComplexCompiler.IsXmlSchemaElement(node, "notation"))
        {
            AddGlobalXmlSchemaNotation(node, module, replacement);
        }
    }

    internal void AddGlobalXmlSchemaElement(XmlNode node, XmlSchemaModule module, bool replacement)
    {
        if (ComplexCompiler.FindXmlSchemaAttribute(node, "ref", "") != null)
        {
            throw CreateXmlSchemaDeclarationError(node, "A global element cannot have a ref attribute.");
        }

        XmlName name = ComplexCompiler.RequireGlobalXmlSchemaComponentName(node, module, "element");
        string key = ComplexCompiler.CreateXmlSchemaComponentKey(name);
        CompiledXmlSchemaElementDeclaration? previous = ElementsByName.ContainsKey(key) ? ElementsByName[key] : null;
        EnsureXmlSchemaReplacement(node, replacement, previous != null, "element");
        CompiledXmlSchemaElementDeclaration declaration = new CompiledXmlSchemaElementDeclaration(name, true, node, module);
        declaration.PreviousDefinition = previous;
        if (replacement)
        {
            ElementsByName[key] = declaration;
        }
        else
        {
            ElementsByName.Add(key, declaration);
        }

        Elements.Add(declaration);
    }

    internal void AddGlobalXmlSchemaAttribute(XmlNode node, XmlSchemaModule module, bool replacement)
    {
        if (ComplexCompiler.FindXmlSchemaAttribute(node, "ref", "") != null)
        {
            throw CreateXmlSchemaDeclarationError(node, "A global attribute cannot have a ref attribute.");
        }

        XmlName name = ComplexCompiler.RequireGlobalXmlSchemaComponentName(node, module, "attribute");
        string key = ComplexCompiler.CreateXmlSchemaComponentKey(name);
        CompiledXmlSchemaAttributeDeclaration? previous = AttributesByName.ContainsKey(key) ? AttributesByName[key] : null;
        EnsureXmlSchemaReplacement(node, replacement, previous != null, "attribute");
        CompiledXmlSchemaAttributeDeclaration declaration = new CompiledXmlSchemaAttributeDeclaration(name, true, node, module);
        declaration.PreviousDefinition = previous;
        if (replacement)
        {
            AttributesByName[key] = declaration;
        }
        else
        {
            AttributesByName.Add(key, declaration);
        }

        Attributes.Add(declaration);
    }

    internal void AddGlobalXmlSchemaNotation(XmlNode node, XmlSchemaModule module, bool replacement)
    {
        XmlName name = ComplexCompiler.RequireGlobalXmlSchemaComponentName(node, module, "notation");
        string key = ComplexCompiler.CreateXmlSchemaComponentKey(name);
        CompiledXmlSchemaNotation? previous = NotationsByName.ContainsKey(key) ? NotationsByName[key] : null;
        EnsureXmlSchemaReplacement(node, replacement, previous != null, "notation");
        CompiledXmlSchemaNotation notation = new CompiledXmlSchemaNotation(name, node, module);
        notation.PreviousDefinition = previous;
        notation.PublicIdentifier = NormalizeOptionalXmlSchemaToken(ComplexCompiler.FindXmlSchemaAttribute(node, "public", ""));
        notation.SystemIdentifier = NormalizeOptionalXmlSchemaToken(ComplexCompiler.FindXmlSchemaAttribute(node, "system", ""));
        if (notation.PublicIdentifier == null && notation.SystemIdentifier == null)
        {
            throw CreateXmlSchemaDeclarationError(node, "A notation requires a public or system identifier.");
        }

        if (replacement)
        {
            NotationsByName[key] = notation;
        }
        else
        {
            NotationsByName.Add(key, notation);
        }

        Notations.Add(notation);
    }

    internal void EnsureXmlSchemaReplacement(XmlNode node, bool replacement, bool existing, string description)
    {
        if (!replacement && existing)
        {
            throw CreateXmlSchemaDeclarationError(node, "The global " + description + " is declared more than once.");
        }

        if (replacement && !existing)
        {
            throw CreateXmlSchemaDeclarationError(node, "An overridden " + description + " must match a referenced definition.");
        }
    }

    internal void CompileGlobalXmlSchemaElement(CompiledXmlSchemaElementDeclaration declaration)
    {
        CompileXmlSchemaElementDeclaration(declaration);
    }

    internal void CompileXmlSchemaElementDeclaration(CompiledXmlSchemaElementDeclaration declaration)
    {
        if (declaration.IsCompiled)
        {
            return;
        }

        if (declaration.IsCompiling)
        {
            throw CreateXmlSchemaDeclarationError(declaration.Node, "A circular element declaration was detected.");
        }

        declaration.IsCompiling = true;
        XmlNode node = declaration.Node;
        ValidateXmlSchemaElementLocationAttributes(declaration);
        declaration.IsAbstract = ComplexCompiler.ParseOptionalXmlSchemaBoolean(node, "abstract", false);
        declaration.IsNillable = ComplexCompiler.ParseOptionalXmlSchemaBoolean(node, "nillable", false);
        declaration.DefaultValue = ComplexCompiler.FindXmlSchemaAttribute(node, "default", "");
        declaration.FixedValue = ComplexCompiler.FindXmlSchemaAttribute(node, "fixed", "");
        if (declaration.DefaultValue != null && declaration.FixedValue != null)
        {
            throw CreateXmlSchemaDeclarationError(node, "An element cannot specify both default and fixed values.");
        }

        declaration.Block = ComplexCompiler.ParseXmlSchemaComplexDerivationSet(node, "block", "blockDefault", true);
        declaration.Final = ComplexCompiler.ParseXmlSchemaComplexDerivationSet(node, "final", "finalDefault", false);
        string? substitutionValue = ComplexCompiler.FindXmlSchemaAttribute(node, "substitutionGroup", "");
        if (substitutionValue != null)
        {
            if (!declaration.IsGlobal)
            {
                throw CreateXmlSchemaDeclarationError(node, "A local element cannot specify a substitution group.");
            }

            declaration.SubstitutionGroupName = ComplexCompiler.ResolveXmlSchemaComponentName(substitutionValue, node, declaration.Module);
        }

        CompileXmlSchemaElementType(declaration);
        EnsureXmlSchemaElementValueConstraintAllowed(declaration);
        declaration.TypeAlternatives = CompileXmlSchemaTypeAlternatives(declaration);
        declaration.IdentityConstraints = CompileXmlSchemaIdentityConstraints(declaration);
        declaration.IsCompiling = false;
        declaration.IsCompiled = true;
    }

    internal void EnsureXmlSchemaElementValueConstraintAllowed(CompiledXmlSchemaElementDeclaration declaration)
    {
        if (declaration.DefaultValue == null && declaration.FixedValue == null || declaration.ComplexType == null)
        {
            return;
        }

        CompiledXmlSchemaComplexType type = declaration.ComplexType;
        if (type.ContentKind != XmlSchemaContentKind.Mixed || !ComplexCompiler.IsXmlSchemaParticleEmptiable(type.Particle))
        {
            throw CreateXmlSchemaDeclarationError(declaration.Node, "An element value constraint requires a simple type or an emptiable mixed-content type.");
        }
    }

    internal void ValidateXmlSchemaElementLocationAttributes(CompiledXmlSchemaElementDeclaration declaration)
    {
        XmlNode node = declaration.Node;
        if (declaration.IsGlobal)
        {
            if (ComplexCompiler.FindXmlSchemaAttribute(node, "minOccurs", "") != null || ComplexCompiler.FindXmlSchemaAttribute(node, "maxOccurs", "") != null || ComplexCompiler.FindXmlSchemaAttribute(node, "form", "") != null || ComplexCompiler.FindXmlSchemaAttribute(node, "targetNamespace", "") != null)
            {
                throw CreateXmlSchemaDeclarationError(node, "A global element cannot specify occurrence, form, or local target-namespace attributes.");
            }
        }
        else if (ComplexCompiler.FindXmlSchemaAttribute(node, "abstract", "") != null || ComplexCompiler.FindXmlSchemaAttribute(node, "final", "") != null || ComplexCompiler.FindXmlSchemaAttribute(node, "substitutionGroup", "") != null)
        {
            throw CreateXmlSchemaDeclarationError(node, "A local element cannot specify abstract, final, or substitutionGroup.");
        }
    }

    internal void CompileXmlSchemaElementType(CompiledXmlSchemaElementDeclaration declaration)
    {
        XmlNode node = declaration.Node;
        string? typeValue = ComplexCompiler.FindXmlSchemaAttribute(node, "type", "");
        XmlNode? simpleNode = ComplexCompiler.FindSingleXmlSchemaChild(node, "simpleType");
        XmlNode? complexNode = ComplexCompiler.FindSingleXmlSchemaChild(node, "complexType");
        int count = typeValue == null ? 0 : 1;
        count = simpleNode == null ? count : count + 1;
        count = complexNode == null ? count : count + 1;
        if (count > 1)
        {
            throw CreateXmlSchemaDeclarationError(node, "An element cannot combine a type attribute with an inline type or contain multiple inline types.");
        }

        if (simpleNode != null)
        {
            declaration.SimpleType = SimpleCompiler.CompileAnonymousXmlSchemaSimpleType(simpleNode, declaration.Module);
        }
        else if (complexNode != null)
        {
            declaration.ComplexType = ComplexCompiler.CompileAnonymousXmlSchemaComplexType(complexNode, declaration.Module);
        }
        else if (typeValue != null)
        {
            XmlName typeName = ComplexCompiler.ResolveXmlSchemaComponentName(typeValue, node, declaration.Module);
            CompiledXmlSchemaSimpleType? simpleType = ComplexCompiler.FindXmlSchemaSimpleType(typeName);
            if (simpleType != null)
            {
                SimpleCompiler.CompileUserXmlSchemaSimpleType(simpleType);
                declaration.SimpleType = simpleType;
            }
            else
            {
                string key = ComplexCompiler.CreateXmlSchemaComponentKey(typeName);
                if (!ComplexCompiler.TypesByName.ContainsKey(key))
                {
                    throw CreateXmlSchemaDeclarationError(node, "The element type does not name a declared simple or complex type.");
                }

                CompiledXmlSchemaComplexType complexType = ComplexCompiler.TypesByName[key];
                ComplexCompiler.CompileUserXmlSchemaComplexType(complexType);
                declaration.ComplexType = complexType;
            }
        }
        else
        {
            declaration.ComplexType = ComplexCompiler.AnyType;
        }
    }

    internal CompiledXmlSchemaTypeAlternative[] CompileXmlSchemaTypeAlternatives(CompiledXmlSchemaElementDeclaration declaration)
    {
        List<CompiledXmlSchemaTypeAlternative> alternatives = new List<CompiledXmlSchemaTypeAlternative>();
        XmlNode[] children = declaration.Node.Children == null ? [] : declaration.Node.Children;
        bool foundDefault = false;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (ComplexCompiler.IsXmlSchemaElement(child, "alternative"))
            {
                if (foundDefault)
                {
                    throw CreateXmlSchemaDeclarationError(child, "The testless type alternative must be last.");
                }

                CompiledXmlSchemaTypeAlternative alternative = CompileXmlSchemaTypeAlternative(child, declaration);
                foundDefault = alternative.Test == null;
                alternatives.Add(alternative);
            }

            index = index + 1;
        }

        return alternatives.ToArray();
    }

    internal CompiledXmlSchemaTypeAlternative CompileXmlSchemaTypeAlternative(XmlNode node, CompiledXmlSchemaElementDeclaration declaration)
    {
        CompiledXmlSchemaTypeAlternative result = new CompiledXmlSchemaTypeAlternative(node);
        string? testValue = ComplexCompiler.FindXmlSchemaAttribute(node, "test", "");
        if (testValue != null)
        {
            string test = new XdmStringDatatype().CollapseXdmStringWhitespace(testValue);
            if (test.Length == 0)
            {
                throw CreateXmlSchemaDeclarationError(node, "A type-alternative test cannot be empty.");
            }

            result.Test = test;
        }

        string? typeValue = ComplexCompiler.FindXmlSchemaAttribute(node, "type", "");
        XmlNode? simpleNode = ComplexCompiler.FindSingleXmlSchemaChild(node, "simpleType");
        XmlNode? complexNode = ComplexCompiler.FindSingleXmlSchemaChild(node, "complexType");
        int count = typeValue == null ? 0 : 1;
        count = simpleNode == null ? count : count + 1;
        count = complexNode == null ? count : count + 1;
        if (count != 1)
        {
            throw CreateXmlSchemaDeclarationError(node, "A type alternative requires exactly one named or inline type.");
        }

        if (simpleNode != null)
        {
            result.SimpleType = SimpleCompiler.CompileAnonymousXmlSchemaSimpleType(simpleNode, declaration.Module);
        }
        else if (complexNode != null)
        {
            result.ComplexType = ComplexCompiler.CompileAnonymousXmlSchemaComplexType(complexNode, declaration.Module);
        }
        else if (typeValue != null)
        {
            XmlName typeName = ComplexCompiler.ResolveXmlSchemaComponentName(typeValue, node, declaration.Module);
            CompiledXmlSchemaSimpleType? simpleType = ComplexCompiler.FindXmlSchemaSimpleType(typeName);
            if (simpleType != null)
            {
                SimpleCompiler.CompileUserXmlSchemaSimpleType(simpleType);
                result.SimpleType = simpleType;
            }
            else
            {
                string key = ComplexCompiler.CreateXmlSchemaComponentKey(typeName);
                if (!ComplexCompiler.TypesByName.ContainsKey(key))
                {
                    throw CreateXmlSchemaDeclarationError(node, "The type alternative does not name a declared type.");
                }

                CompiledXmlSchemaComplexType complexType = ComplexCompiler.TypesByName[key];
                ComplexCompiler.CompileUserXmlSchemaComplexType(complexType);
                result.ComplexType = complexType;
            }
        }

        return result;
    }

    internal CompiledXmlSchemaIdentityConstraint[] CompileXmlSchemaIdentityConstraints(CompiledXmlSchemaElementDeclaration declaration)
    {
        List<CompiledXmlSchemaIdentityConstraint> constraints = new List<CompiledXmlSchemaIdentityConstraint>();
        XmlNode[] children = declaration.Node.Children == null ? [] : declaration.Node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            bool supported = ComplexCompiler.IsXmlSchemaElement(child, "key") || ComplexCompiler.IsXmlSchemaElement(child, "unique") || ComplexCompiler.IsXmlSchemaElement(child, "keyref");
            if (supported)
            {
                XmlSchemaIdentityConstraintKind kind = ClassifyXmlSchemaIdentityConstraint(child);
                CompiledXmlSchemaIdentityConstraint constraint = CompileXmlSchemaIdentityConstraint(child, declaration, kind);
                constraints.Add(constraint);
                IdentityConstraints.Add(constraint);
                string key = ComplexCompiler.CreateXmlSchemaComponentKey(constraint.Name);
                if (IdentityConstraintsByName.ContainsKey(key))
                {
                    throw CreateXmlSchemaDeclarationError(child, "The identity constraint is declared more than once.");
                }

                IdentityConstraintsByName.Add(key, constraint);
            }

            index = index + 1;
        }

        return constraints.ToArray();
    }

    internal XmlSchemaIdentityConstraintKind ClassifyXmlSchemaIdentityConstraint(XmlNode node)
    {
        if (ComplexCompiler.IsXmlSchemaElement(node, "key"))
        {
            return XmlSchemaIdentityConstraintKind.Key;
        }

        if (ComplexCompiler.IsXmlSchemaElement(node, "unique"))
        {
            return XmlSchemaIdentityConstraintKind.Unique;
        }

        return XmlSchemaIdentityConstraintKind.KeyReference;
    }

    internal CompiledXmlSchemaIdentityConstraint CompileXmlSchemaIdentityConstraint(XmlNode node, CompiledXmlSchemaElementDeclaration declaration, XmlSchemaIdentityConstraintKind kind)
    {
        XmlName name = ComplexCompiler.RequireGlobalXmlSchemaComponentName(node, declaration.Module, "identity constraint");
        CompiledXmlSchemaIdentityConstraint result = new CompiledXmlSchemaIdentityConstraint(name, kind, node, declaration.Module);
        result.OwnerElement = declaration;
        result.XPathDefaultNamespace = NormalizeOptionalXmlSchemaToken(ComplexCompiler.FindXmlSchemaAttribute(node, "xpathDefaultNamespace", ""));
        XmlNode? selector = ComplexCompiler.FindSingleXmlSchemaChild(node, "selector");
        if (selector == null)
        {
            throw CreateXmlSchemaDeclarationError(node, "An identity constraint requires one selector.");
        }

        string? selectorValue = ComplexCompiler.FindXmlSchemaAttribute(selector, "xpath", "");
        result.Selector = RequireXmlSchemaXPath(selector, selectorValue, "selector");
        List<string> fields = new List<string>();
        List<XmlNode> fieldNodes = new List<XmlNode>();
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            if (ComplexCompiler.IsXmlSchemaElement(children[index], "field"))
            {
                string? fieldValue = ComplexCompiler.FindXmlSchemaAttribute(children[index], "xpath", "");
                fields.Add(RequireXmlSchemaXPath(children[index], fieldValue, "field"));
                fieldNodes.Add(children[index]);
            }

            index = index + 1;
        }

        if (fields.Count == 0)
        {
            throw CreateXmlSchemaDeclarationError(node, "An identity constraint requires at least one field.");
        }

        result.Fields = fields.ToArray();
        XmlSchemaIdentityXPathCompiler xpathCompiler = new XmlSchemaIdentityXPathCompiler(this);
        result.CompiledSelector = xpathCompiler.CompileXmlSchemaIdentityXPath(result.Selector, selector, declaration.Module, result.XPathDefaultNamespace, false);
        List<XmlSchemaIdentityXPathExpression> compiledFields = new List<XmlSchemaIdentityXPathExpression>();
        int fieldIndex = 0;
        while (fieldIndex < result.Fields.Length)
        {
            compiledFields.Add(xpathCompiler.CompileXmlSchemaIdentityXPath(result.Fields[fieldIndex], fieldNodes[fieldIndex], declaration.Module, result.XPathDefaultNamespace, true));
            fieldIndex = fieldIndex + 1;
        }

        result.CompiledFields = compiledFields.ToArray();
        string? referValue = ComplexCompiler.FindXmlSchemaAttribute(node, "refer", "");
        if (kind == XmlSchemaIdentityConstraintKind.KeyReference)
        {
            if (referValue == null)
            {
                throw CreateXmlSchemaDeclarationError(node, "A keyref requires a refer attribute.");
            }

            result.ReferName = ComplexCompiler.ResolveXmlSchemaComponentName(referValue, node, declaration.Module);
        }
        else if (referValue != null)
        {
            throw CreateXmlSchemaDeclarationError(node, "Only keyref can specify a refer attribute.");
        }

        return result;
    }

    internal string RequireXmlSchemaXPath(XmlNode node, string? value, string description)
    {
        string normalized = value == null ? "" : new XdmStringDatatype().CollapseXdmStringWhitespace(value);
        if (normalized.Length == 0)
        {
            throw CreateXmlSchemaDeclarationError(node, "An identity-constraint " + description + " requires a non-empty xpath attribute.");
        }

        return normalized;
    }

    internal void CompileGlobalXmlSchemaAttribute(CompiledXmlSchemaAttributeDeclaration declaration)
    {
        CompileXmlSchemaAttributeDeclaration(declaration);
    }

    internal void CompileXmlSchemaAttributeDeclaration(CompiledXmlSchemaAttributeDeclaration declaration)
    {
        if (declaration.IsCompiled)
        {
            return;
        }

        if (declaration.IsCompiling)
        {
            throw CreateXmlSchemaDeclarationError(declaration.Node, "A circular attribute declaration was detected.");
        }

        declaration.IsCompiling = true;
        XmlNode node = declaration.Node;
        if (declaration.IsGlobal && (ComplexCompiler.FindXmlSchemaAttribute(node, "use", "") != null || ComplexCompiler.FindXmlSchemaAttribute(node, "form", "") != null || ComplexCompiler.FindXmlSchemaAttribute(node, "targetNamespace", "") != null))
        {
            throw CreateXmlSchemaDeclarationError(node, "A global attribute cannot specify use, form, or a local target namespace.");
        }

        declaration.DefaultValue = ComplexCompiler.FindXmlSchemaAttribute(node, "default", "");
        declaration.FixedValue = ComplexCompiler.FindXmlSchemaAttribute(node, "fixed", "");
        if (declaration.DefaultValue != null && declaration.FixedValue != null)
        {
            throw CreateXmlSchemaDeclarationError(node, "An attribute cannot specify both default and fixed values.");
        }

        declaration.Inheritable = ComplexCompiler.ParseOptionalXmlSchemaBoolean(node, "inheritable", false);
        string? typeValue = ComplexCompiler.FindXmlSchemaAttribute(node, "type", "");
        XmlNode? inlineNode = ComplexCompiler.FindSingleXmlSchemaChild(node, "simpleType");
        if (typeValue != null && inlineNode != null)
        {
            throw CreateXmlSchemaDeclarationError(node, "An attribute cannot specify both type and inline simpleType.");
        }

        if (inlineNode != null)
        {
            declaration.Type = SimpleCompiler.CompileAnonymousXmlSchemaSimpleType(inlineNode, declaration.Module);
        }
        else if (typeValue != null)
        {
            XmlName typeName = ComplexCompiler.ResolveXmlSchemaComponentName(typeValue, node, declaration.Module);
            declaration.Type = ComplexCompiler.FindXmlSchemaSimpleType(typeName);
            if (declaration.Type == null)
            {
                throw CreateXmlSchemaDeclarationError(node, "The attribute type does not name a declared simple type.");
            }

            SimpleCompiler.CompileUserXmlSchemaSimpleType(declaration.Type);
        }
        else
        {
            XmlName stringName = ComplexCompiler.CreateXmlSchemaComponentName("string", XmlNamespaces.XmlSchema, "xs");
            declaration.Type = ComplexCompiler.FindXmlSchemaSimpleType(stringName);
        }

        declaration.IsCompiling = false;
        declaration.IsCompiled = true;
    }

    internal void BindAllXmlSchemaLocalDeclarations()
    {
        int groupIndex = 0;
        while (groupIndex < ComplexCompiler.Groups.Count)
        {
            BindXmlSchemaParticle(ComplexCompiler.Groups[groupIndex].Particle, ComplexCompiler.Groups[groupIndex].Module);
            groupIndex = groupIndex + 1;
        }

        int attributeGroupIndex = 0;
        while (attributeGroupIndex < ComplexCompiler.AttributeGroups.Count)
        {
            CompiledXmlSchemaAttributeGroup group = ComplexCompiler.AttributeGroups[attributeGroupIndex];
            BindXmlSchemaAttributeUses(group.Attributes, group.Module);
            attributeGroupIndex = attributeGroupIndex + 1;
        }

        int typeIndex = 0;
        while (typeIndex < ComplexCompiler.Types.Count)
        {
            CompiledXmlSchemaComplexType type = ComplexCompiler.Types[typeIndex];
            BindXmlSchemaParticle(type.Particle, type.Module);
            BindXmlSchemaAttributeUses(type.Attributes, type.Module);
            typeIndex = typeIndex + 1;
        }
    }

    internal void BindXmlSchemaParticle(CompiledXmlSchemaParticle? particle, XmlSchemaModule? module)
    {
        if (particle == null)
        {
            return;
        }

        if (particle.Kind == XmlSchemaParticleKind.Element && particle.ElementDeclaration == null)
        {
            XmlName? name = particle.Name;
            if (name == null)
            {
                throw CreateXmlSchemaDeclarationError(particle.Node, "An element particle has no name.");
            }

            if (particle.IsReference)
            {
                ValidateXmlSchemaElementReference(particle.Node);
                string key = ComplexCompiler.CreateXmlSchemaComponentKey(name);
                if (!ElementsByName.ContainsKey(key))
                {
                    throw CreateXmlSchemaDeclarationError(particle.Node, "The referenced global element is not declared.");
                }

                CompiledXmlSchemaElementDeclaration referenced = ElementsByName[key];
                CompileXmlSchemaElementDeclaration(referenced);
                particle.ElementDeclaration = referenced;
            }
            else
            {
                if (particle.Node == null || module == null)
                {
                    throw CreateXmlSchemaDeclarationError(particle.Node, "A local element declaration requires a schema module.");
                }

                ApplyXmlSchemaLocalElementForm(name, particle.Node, module);
                CompiledXmlSchemaElementDeclaration local = new CompiledXmlSchemaElementDeclaration(name, false, particle.Node, module);
                local.IsQualified = name.NamespaceUri != null && name.NamespaceUri.Length != 0;
                CompileXmlSchemaElementDeclaration(local);
                particle.ElementDeclaration = local;
                Elements.Add(local);
            }
        }

        int index = 0;
        while (index < particle.Children.Length)
        {
            BindXmlSchemaParticle(particle.Children[index], module);
            index = index + 1;
        }
    }

    internal void ValidateXmlSchemaElementReference(XmlNode? node)
    {
        if (node == null)
        {
            return;
        }

        if (ComplexCompiler.FindXmlSchemaAttribute(node, "type", "") != null || ComplexCompiler.FindXmlSchemaAttribute(node, "default", "") != null || ComplexCompiler.FindXmlSchemaAttribute(node, "fixed", "") != null || ComplexCompiler.FindXmlSchemaAttribute(node, "form", "") != null || ComplexCompiler.FindXmlSchemaAttribute(node, "targetNamespace", "") != null || ComplexCompiler.FindXmlSchemaAttribute(node, "nillable", "") != null || ComplexCompiler.FindXmlSchemaAttribute(node, "block", "") != null)
        {
            throw CreateXmlSchemaDeclarationError(node, "An element reference cannot redeclare element properties.");
        }

        XmlNode? simpleNode = ComplexCompiler.FindSingleXmlSchemaChild(node, "simpleType");
        XmlNode? complexNode = ComplexCompiler.FindSingleXmlSchemaChild(node, "complexType");
        if (simpleNode != null || complexNode != null)
        {
            throw CreateXmlSchemaDeclarationError(node, "An element reference cannot contain a local type.");
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (ComplexCompiler.IsXmlSchemaElement(child, "alternative") || ComplexCompiler.IsXmlSchemaElement(child, "key") || ComplexCompiler.IsXmlSchemaElement(child, "unique") || ComplexCompiler.IsXmlSchemaElement(child, "keyref"))
            {
                throw CreateXmlSchemaDeclarationError(node, "An element reference cannot contain alternatives or identity constraints.");
            }

            index = index + 1;
        }
    }

    internal void BindXmlSchemaAttributeUses(CompiledXmlSchemaAttributeUse[] uses, XmlSchemaModule? module)
    {
        int index = 0;
        while (index < uses.Length)
        {
            CompiledXmlSchemaAttributeUse use = uses[index];
            if (use.Declaration == null)
            {
                if (use.IsReference)
                {
                    if (ComplexCompiler.FindXmlSchemaAttribute(use.Node, "form", "") != null || ComplexCompiler.FindXmlSchemaAttribute(use.Node, "targetNamespace", "") != null)
                    {
                        throw CreateXmlSchemaDeclarationError(use.Node, "An attribute reference cannot specify form or targetNamespace.");
                    }

                    string key = ComplexCompiler.CreateXmlSchemaComponentKey(use.Name);
                    if (!AttributesByName.ContainsKey(key))
                    {
                        throw CreateXmlSchemaDeclarationError(use.Node, "The referenced global attribute is not declared.");
                    }

                    CompiledXmlSchemaAttributeDeclaration referenced = AttributesByName[key];
                    CompileXmlSchemaAttributeDeclaration(referenced);
                    use.Declaration = referenced;
                    use.Type = referenced.Type;
                    if (referenced.FixedValue != null && use.DefaultValue != null)
                    {
                        throw CreateXmlSchemaDeclarationError(use.Node, "An attribute reference cannot replace a fixed value with a default value.");
                    }

                    if (referenced.FixedValue != null && use.FixedValue != null && !string.Equals(referenced.FixedValue, use.FixedValue, StringComparison.Ordinal))
                    {
                        throw CreateXmlSchemaDeclarationError(use.Node, "An attribute reference cannot change the referenced fixed value.");
                    }

                    if (use.DefaultValue == null && use.FixedValue == null)
                    {
                        use.DefaultValue = referenced.DefaultValue;
                        use.FixedValue = referenced.FixedValue;
                    }
                }
                else
                {
                    if (module == null)
                    {
                        throw CreateXmlSchemaDeclarationError(use.Node, "A local attribute declaration requires a schema module.");
                    }

                    ApplyXmlSchemaLocalAttributeForm(use.Name, use.Node, module);
                    CompiledXmlSchemaAttributeDeclaration local = new CompiledXmlSchemaAttributeDeclaration(use.Name, false, use.Node, module);
                    local.IsQualified = use.Name.NamespaceUri != null && use.Name.NamespaceUri.Length != 0;
                    local.Type = use.Type;
                    local.DefaultValue = use.DefaultValue;
                    local.FixedValue = use.FixedValue;
                    local.Inheritable = ComplexCompiler.ParseOptionalXmlSchemaBoolean(use.Node, "inheritable", false);
                    local.IsCompiled = true;
                    use.Declaration = local;
                    Attributes.Add(local);
                }
            }

            index = index + 1;
        }
    }

    internal void ApplyXmlSchemaLocalElementForm(XmlName name, XmlNode node, XmlSchemaModule module)
    {
        ApplyXmlSchemaLocalForm(name, node, module, "elementFormDefault");
    }

    internal void ApplyXmlSchemaLocalAttributeForm(XmlName name, XmlNode node, XmlSchemaModule module)
    {
        ApplyXmlSchemaLocalForm(name, node, module, "attributeFormDefault");
    }

    internal void ApplyXmlSchemaLocalForm(XmlName name, XmlNode node, XmlSchemaModule module, string defaultName)
    {
        name.NamespaceUri = ComplexCompiler.ResolveXmlSchemaLocalDeclarationNamespace(node, module, defaultName);
    }

    internal void ResolveXmlSchemaSubstitutionGroups()
    {
        int index = 0;
        while (index < Elements.Count)
        {
            CompiledXmlSchemaElementDeclaration declaration = Elements[index];
            if (declaration.IsGlobal && declaration.SubstitutionGroupName != null)
            {
                string key = ComplexCompiler.CreateXmlSchemaComponentKey(declaration.SubstitutionGroupName);
                if (!ElementsByName.ContainsKey(key))
                {
                    throw CreateXmlSchemaDeclarationError(declaration.Node, "The substitution-group head is not declared.");
                }

                CompiledXmlSchemaElementDeclaration head = ElementsByName[key];
                if (head == declaration)
                {
                    throw CreateXmlSchemaDeclarationError(declaration.Node, "An element cannot substitute for itself.");
                }

                EnsureNoXmlSchemaSubstitutionCycle(declaration, head);
                EnsureXmlSchemaSubstitutionTypeAllowed(declaration, head);
                declaration.SubstitutionGroupHead = head;
                List<CompiledXmlSchemaElementDeclaration> members = new List<CompiledXmlSchemaElementDeclaration>();
                int memberIndex = 0;
                while (memberIndex < head.SubstitutionGroupMembers.Length)
                {
                    members.Add(head.SubstitutionGroupMembers[memberIndex]);
                    memberIndex = memberIndex + 1;
                }

                members.Add(declaration);
                head.SubstitutionGroupMembers = members.ToArray();
            }

            index = index + 1;
        }
    }

    internal void EnsureXmlSchemaSubstitutionTypeAllowed(CompiledXmlSchemaElementDeclaration declaration, CompiledXmlSchemaElementDeclaration head)
    {
        if (head.ComplexType == ComplexCompiler.AnyType)
        {
            return;
        }

        XmlSchemaDerivationMethod methods = XmlSchemaDerivationMethod.None;
        if (head.ComplexType != null && declaration.ComplexType != null)
        {
            CompiledXmlSchemaComplexType? currentComplex = declaration.ComplexType;
            while (currentComplex != null)
            {
                if (currentComplex == head.ComplexType)
                {
                    if (((int)head.Final & (int)methods) != 0)
                    {
                        throw CreateXmlSchemaDeclarationError(declaration.Node, "The substitution-group head prohibits the member type derivation.");
                    }

                    return;
                }

                methods = methods | currentComplex.DerivationMethod;
                CompiledXmlSchemaComplexType? next = currentComplex.BaseType;
                currentComplex = next == currentComplex ? null : next;
            }
        }
        else if (head.SimpleType != null && declaration.SimpleType != null)
        {
            CompiledXmlSchemaSimpleType? currentSimple = declaration.SimpleType;
            while (currentSimple != null)
            {
                if (currentSimple == head.SimpleType)
                {
                    if (((int)head.Final & (int)methods) != 0)
                    {
                        throw CreateXmlSchemaDeclarationError(declaration.Node, "The substitution-group head prohibits the member type derivation.");
                    }

                    return;
                }

                methods = methods | currentSimple.DerivationMethod;
                currentSimple = currentSimple.BaseType;
            }
        }

        throw CreateXmlSchemaDeclarationError(declaration.Node, "A substitution-group member type must be validly derived from the head type.");
    }

    internal void EnsureNoXmlSchemaSubstitutionCycle(CompiledXmlSchemaElementDeclaration declaration, CompiledXmlSchemaElementDeclaration head)
    {
        CompiledXmlSchemaElementDeclaration? current = head;
        int remaining = Elements.Count + 1;
        while (current != null && remaining > 0)
        {
            if (current == declaration)
            {
                throw CreateXmlSchemaDeclarationError(declaration.Node, "A circular substitution group was detected.");
            }

            if (current.SubstitutionGroupName == null)
            {
                return;
            }

            string key = ComplexCompiler.CreateXmlSchemaComponentKey(current.SubstitutionGroupName);
            current = ElementsByName.ContainsKey(key) ? ElementsByName[key] : null;
            remaining = remaining - 1;
        }
    }

    internal void ResolveXmlSchemaIdentityConstraintReferences()
    {
        int index = 0;
        while (index < IdentityConstraints.Count)
        {
            CompiledXmlSchemaIdentityConstraint constraint = IdentityConstraints[index];
            if (constraint.Kind == XmlSchemaIdentityConstraintKind.KeyReference)
            {
                XmlName? referName = constraint.ReferName;
                string key = referName == null ? "" : ComplexCompiler.CreateXmlSchemaComponentKey(referName);
                if (referName == null || !IdentityConstraintsByName.ContainsKey(key))
                {
                    throw CreateXmlSchemaDeclarationError(constraint.Node, "The keyref target is not declared.");
                }

                CompiledXmlSchemaIdentityConstraint referenced = IdentityConstraintsByName[key];
                if (referenced.Kind == XmlSchemaIdentityConstraintKind.KeyReference)
                {
                    throw CreateXmlSchemaDeclarationError(constraint.Node, "A keyref must refer to a key or unique constraint.");
                }

                if (referenced.Fields.Length != constraint.Fields.Length)
                {
                    throw CreateXmlSchemaDeclarationError(constraint.Node, "A keyref must have the same number of fields as its target.");
                }

                constraint.ReferencedConstraint = referenced;
            }

            index = index + 1;
        }
    }

    internal string? NormalizeOptionalXmlSchemaToken(string? value)
    {
        if (value == null)
        {
            return null;
        }

        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(value);
        return normalized.Length == 0 ? null : normalized;
    }

    internal XmlSchemaException CreateXmlSchemaDeclarationError(XmlNode? node, string message)
    {
        return ComplexCompiler.CreateXmlSchemaComplexTypeError(node, message);
    }
}
