using System;
using System.Collections.Generic;

namespace xml;

internal class XmlSchemaComplexTypeCompiler
{
    internal XmlSchemaModule[] Modules;
    internal XmlSchemaSimpleTypeCompiler SimpleCompiler;
    internal List<CompiledXmlSchemaComplexType> Types;
    internal List<CompiledXmlSchemaComplexType> UserTypes;
    internal List<CompiledXmlSchemaGroup> Groups;
    internal List<CompiledXmlSchemaAttributeGroup> AttributeGroups;
    internal Dictionary<string, CompiledXmlSchemaComplexType> TypesByName;
    internal Dictionary<string, CompiledXmlSchemaGroup> GroupsByName;
    internal Dictionary<string, CompiledXmlSchemaAttributeGroup> AttributeGroupsByName;
    internal CompiledXmlSchemaComplexType AnyType;

    internal XmlSchemaComplexTypeCompiler(XmlSchemaModule[] modules, XmlSchemaSimpleTypeCompiler simpleCompiler)
    {
        Guard.NotNull(modules, "modules");
        Guard.NotNull(simpleCompiler, "simpleCompiler");
        Modules = modules;
        SimpleCompiler = simpleCompiler;
        Types = new List<CompiledXmlSchemaComplexType>();
        UserTypes = new List<CompiledXmlSchemaComplexType>();
        Groups = new List<CompiledXmlSchemaGroup>();
        AttributeGroups = new List<CompiledXmlSchemaAttributeGroup>();
        TypesByName = new Dictionary<string, CompiledXmlSchemaComplexType>(StringComparer.Ordinal);
        GroupsByName = new Dictionary<string, CompiledXmlSchemaGroup>(StringComparer.Ordinal);
        AttributeGroupsByName = new Dictionary<string, CompiledXmlSchemaAttributeGroup>(StringComparer.Ordinal);
        XmlName anyTypeName = CreateXmlSchemaComponentName("anyType", XmlNamespaces.XmlSchema, "xs");
        AnyType = new CompiledXmlSchemaComplexType(anyTypeName, true, false, null, null);
    }

    internal CompiledXmlSchemaComplexType[] CompileXmlSchemaComplexTypes()
    {
        CreateBuiltInXmlSchemaAnyType();
        CollectGlobalXmlSchemaComplexComponents();
        CollectReplacedXmlSchemaComplexComponents();
        ValidateDefaultXmlSchemaOpenContentDeclarations();

        int groupIndex = 0;
        while (groupIndex < Groups.Count)
        {
            CompileXmlSchemaGroup(Groups[groupIndex]);
            groupIndex = groupIndex + 1;
        }

        int attributeGroupIndex = 0;
        while (attributeGroupIndex < AttributeGroups.Count)
        {
            CompileXmlSchemaAttributeGroup(AttributeGroups[attributeGroupIndex]);
            attributeGroupIndex = attributeGroupIndex + 1;
        }

        int typeIndex = 0;
        while (typeIndex < UserTypes.Count)
        {
            CompileUserXmlSchemaComplexType(UserTypes[typeIndex]);
            typeIndex = typeIndex + 1;
        }

        return Types.ToArray();
    }

    internal void ValidateDefaultXmlSchemaOpenContentDeclarations()
    {
        int index = 0;
        while (index < Modules.Length)
        {
            XmlSchemaModule module = Modules[index];
            XmlNode? root = module.Document.DocumentElement;
            if (root != null)
            {
                XmlNode? declaration = FindSingleXmlSchemaChild(root, "defaultOpenContent");
                if (declaration != null)
                {
                    CompileXmlSchemaOpenContent(declaration, module, true);
                }
            }

            index = index + 1;
        }
    }

    internal void CreateBuiltInXmlSchemaAnyType()
    {
        AnyType.BaseType = AnyType;
        AnyType.ContentKind = XmlSchemaContentKind.Mixed;
        AnyType.Mixed = true;
        AnyType.DerivationMethod = XmlSchemaDerivationMethod.Restriction;

        CompiledXmlSchemaWildcard elementWildcard = new CompiledXmlSchemaWildcard(null);
        elementWildcard.ProcessContents = XmlSchemaProcessContents.Lax;
        CompiledXmlSchemaParticle any = new CompiledXmlSchemaParticle(XmlSchemaParticleKind.Any, null);
        any.MinOccurs = System.Numerics.BigInteger.Zero;
        any.MaxOccursUnbounded = true;
        any.Wildcard = elementWildcard;
        CompiledXmlSchemaParticle sequence = new CompiledXmlSchemaParticle(XmlSchemaParticleKind.Sequence, null);
        sequence.Children = [any];
        AnyType.Particle = sequence;

        CompiledXmlSchemaWildcard attributeWildcard = new CompiledXmlSchemaWildcard(null);
        attributeWildcard.ProcessContents = XmlSchemaProcessContents.Lax;
        AnyType.AttributeWildcard = attributeWildcard;
        AddNamedXmlSchemaComplexType(AnyType, false, null);
    }

    internal void CollectGlobalXmlSchemaComplexComponents()
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
                CollectOneGlobalXmlSchemaComplexComponent(children[childIndex], module, false);
                childIndex = childIndex + 1;
            }

            moduleIndex = moduleIndex + 1;
        }
    }

    internal void CollectReplacedXmlSchemaComplexComponents()
    {
        int moduleIndex = 0;
        while (moduleIndex < Modules.Length)
        {
            XmlSchemaModule module = Modules[moduleIndex];
            int directiveIndex = 0;
            while (directiveIndex < module.Directives.Length)
            {
                XmlSchemaDirective directive = module.Directives[directiveIndex];
                if (directive.Kind == XmlSchemaDirectiveKind.Redefine || directive.Kind == XmlSchemaDirectiveKind.Override)
                {
                    XmlNode[] children = directive.Node.Children == null ? [] : directive.Node.Children;
                    int childIndex = 0;
                    while (childIndex < children.Length)
                    {
                        CollectOneGlobalXmlSchemaComplexComponent(children[childIndex], module, true);
                        childIndex = childIndex + 1;
                    }
                }

                directiveIndex = directiveIndex + 1;
            }

            moduleIndex = moduleIndex + 1;
        }
    }

    internal void CollectOneGlobalXmlSchemaComplexComponent(XmlNode node, XmlSchemaModule module, bool replacement)
    {
        if (IsXmlSchemaElement(node, "complexType"))
        {
            AddGlobalXmlSchemaComplexType(node, module, replacement);
        }
        else if (IsXmlSchemaElement(node, "group"))
        {
            AddGlobalXmlSchemaGroup(node, module, replacement);
        }
        else if (IsXmlSchemaElement(node, "attributeGroup"))
        {
            AddGlobalXmlSchemaAttributeGroup(node, module, replacement);
        }
    }

    internal void AddGlobalXmlSchemaComplexType(XmlNode node, XmlSchemaModule module, bool replacement)
    {
        XmlName name = RequireGlobalXmlSchemaComponentName(node, module, "complex type");
        if (FindXmlSchemaSimpleType(name) != null)
        {
            throw CreateXmlSchemaComplexTypeError(node, "A simple and complex type cannot share the same expanded name.");
        }

        string key = CreateXmlSchemaComponentKey(name);
        CompiledXmlSchemaComplexType? previous = TypesByName.ContainsKey(key) ? TypesByName[key] : null;
        if (!replacement && previous != null)
        {
            throw CreateXmlSchemaComplexTypeError(node, "The complex type is declared more than once.");
        }

        if (replacement && previous == null)
        {
            throw CreateXmlSchemaComplexTypeError(node, "A redefined or overridden complex type must match a referenced definition.");
        }

        CompiledXmlSchemaComplexType definition = new CompiledXmlSchemaComplexType(name, false, false, node, module);
        definition.PreviousDefinition = previous;
        AddNamedXmlSchemaComplexType(definition, replacement, node);
        UserTypes.Add(definition);
    }

    internal void AddNamedXmlSchemaComplexType(CompiledXmlSchemaComplexType definition, bool replacement, XmlNode? node)
    {
        XmlName? name = definition.Name;
        if (name == null)
        {
            throw CreateXmlSchemaComplexTypeError(node, "A named complex type requires a name.");
        }

        string key = CreateXmlSchemaComponentKey(name);
        if (TypesByName.ContainsKey(key))
        {
            if (!replacement)
            {
                throw CreateXmlSchemaComplexTypeError(node, "The complex type is already declared.");
            }

            TypesByName[key] = definition;
        }
        else
        {
            TypesByName.Add(key, definition);
        }

        Types.Add(definition);
    }

    internal void AddGlobalXmlSchemaGroup(XmlNode node, XmlSchemaModule module, bool replacement)
    {
        if (FindXmlSchemaAttribute(node, "ref", "") != null)
        {
            throw CreateXmlSchemaComplexTypeError(node, "A global model group cannot have a ref attribute.");
        }

        XmlName name = RequireGlobalXmlSchemaComponentName(node, module, "model group");
        string key = CreateXmlSchemaComponentKey(name);
        CompiledXmlSchemaGroup? previous = GroupsByName.ContainsKey(key) ? GroupsByName[key] : null;
        if (!replacement && previous != null)
        {
            throw CreateXmlSchemaComplexTypeError(node, "The model group is declared more than once.");
        }

        if (replacement && previous == null)
        {
            throw CreateXmlSchemaComplexTypeError(node, "A redefined or overridden model group must match a referenced definition.");
        }

        CompiledXmlSchemaGroup group = new CompiledXmlSchemaGroup(name, node, module);
        group.PreviousDefinition = previous;
        if (replacement)
        {
            GroupsByName[key] = group;
        }
        else
        {
            GroupsByName.Add(key, group);
        }

        Groups.Add(group);
    }

    internal void AddGlobalXmlSchemaAttributeGroup(XmlNode node, XmlSchemaModule module, bool replacement)
    {
        if (FindXmlSchemaAttribute(node, "ref", "") != null)
        {
            throw CreateXmlSchemaComplexTypeError(node, "A global attribute group cannot have a ref attribute.");
        }

        XmlName name = RequireGlobalXmlSchemaComponentName(node, module, "attribute group");
        string key = CreateXmlSchemaComponentKey(name);
        CompiledXmlSchemaAttributeGroup? previous = AttributeGroupsByName.ContainsKey(key) ? AttributeGroupsByName[key] : null;
        if (!replacement && previous != null)
        {
            throw CreateXmlSchemaComplexTypeError(node, "The attribute group is declared more than once.");
        }

        if (replacement && previous == null)
        {
            throw CreateXmlSchemaComplexTypeError(node, "A redefined or overridden attribute group must match a referenced definition.");
        }

        CompiledXmlSchemaAttributeGroup group = new CompiledXmlSchemaAttributeGroup(name, node, module);
        group.PreviousDefinition = previous;
        if (replacement)
        {
            AttributeGroupsByName[key] = group;
        }
        else
        {
            AttributeGroupsByName.Add(key, group);
        }

        AttributeGroups.Add(group);
    }

    internal void CompileXmlSchemaGroup(CompiledXmlSchemaGroup group)
    {
        if (group.IsCompiled)
        {
            return;
        }

        if (group.IsCompiling)
        {
            throw CreateXmlSchemaComplexTypeError(group.Node, "A circular model-group reference was detected.");
        }

        group.IsCompiling = true;
        if (FindXmlSchemaAttribute(group.Node, "minOccurs", "") != null || FindXmlSchemaAttribute(group.Node, "maxOccurs", "") != null)
        {
            throw CreateXmlSchemaComplexTypeError(group.Node, "A global model group cannot specify occurrence constraints.");
        }

        XmlNode? particleNode = new XmlSchemaParticleCompiler(this).FindSingleXmlSchemaParticle(group.Node, true);
        if (particleNode == null)
        {
            throw CreateXmlSchemaComplexTypeError(group.Node, "A model group requires one sequence, choice, or all particle.");
        }

        group.Particle = new XmlSchemaParticleCompiler(this).CompileXmlSchemaParticle(particleNode, group.Module, group);
        group.IsCompiling = false;
        group.IsCompiled = true;
    }

    internal void CompileXmlSchemaAttributeGroup(CompiledXmlSchemaAttributeGroup group)
    {
        if (group.IsCompiled)
        {
            return;
        }

        if (group.IsCompiling)
        {
            throw CreateXmlSchemaComplexTypeError(group.Node, "A circular attribute-group reference was detected.");
        }

        group.IsCompiling = true;
        CompiledXmlSchemaAttributeSet set = new XmlSchemaAttributeCompiler(this).CompileXmlSchemaAttributeSet(group.Node, group.Module, group);
        group.Attributes = RemoveProhibitedXmlSchemaAttributes(set.Attributes);
        group.Wildcard = set.Wildcard;
        group.IsCompiling = false;
        group.IsCompiled = true;
    }

    internal void CompileUserXmlSchemaComplexType(CompiledXmlSchemaComplexType definition)
    {
        if (definition.IsCompiled)
        {
            return;
        }

        XmlNode? node = definition.Node;
        if (node == null)
        {
            throw CreateXmlSchemaComplexTypeError(null, "A user complex type requires a declaration node.");
        }

        if (definition.IsCompiling)
        {
            throw CreateXmlSchemaComplexTypeError(node, "A circular complex-type derivation was detected.");
        }

        definition.IsCompiling = true;
        definition.Final = ParseXmlSchemaComplexDerivationSet(node, "final", "finalDefault", false);
        definition.Block = ParseXmlSchemaComplexDerivationSet(node, "block", "blockDefault", false);
        definition.Abstract = ParseOptionalXmlSchemaBoolean(node, "abstract", false);
        definition.Mixed = ParseOptionalXmlSchemaBoolean(node, "mixed", false);

        XmlNode? simpleContent = FindSingleXmlSchemaChild(node, "simpleContent");
        XmlNode? complexContent = FindSingleXmlSchemaChild(node, "complexContent");
        if (simpleContent != null && complexContent != null)
        {
            throw CreateXmlSchemaComplexTypeError(node, "A complex type cannot contain both simpleContent and complexContent.");
        }

        XmlNode? directParticle = new XmlSchemaParticleCompiler(this).FindSingleXmlSchemaParticle(node, false);
        if ((simpleContent != null || complexContent != null) && directParticle != null)
        {
            throw CreateXmlSchemaComplexTypeError(node, "A content wrapper cannot be combined with a direct content particle.");
        }

        if (simpleContent != null)
        {
            if (definition.Mixed)
            {
                throw CreateXmlSchemaComplexTypeError(node, "A complex type with simple content cannot have mixed content.");
            }

            CompileXmlSchemaSimpleContent(definition, simpleContent);
        }
        else if (complexContent != null)
        {
            CompileXmlSchemaComplexContent(definition, complexContent);
        }
        else
        {
            CompileDirectXmlSchemaComplexContent(definition, node);
        }

        definition.IsCompiling = false;
        definition.IsCompiled = true;
    }

    internal CompiledXmlSchemaComplexType CompileAnonymousXmlSchemaComplexType(XmlNode node, XmlSchemaModule module)
    {
        if (FindXmlSchemaAttribute(node, "name", "") != null)
        {
            throw CreateXmlSchemaComplexTypeError(node, "An inline complex type cannot have a name.");
        }

        CompiledXmlSchemaComplexType definition = new CompiledXmlSchemaComplexType(null, false, true, node, module);
        Types.Add(definition);
        UserTypes.Add(definition);
        CompileUserXmlSchemaComplexType(definition);
        return definition;
    }

    internal void CompileDirectXmlSchemaComplexContent(CompiledXmlSchemaComplexType definition, XmlNode node)
    {
        definition.BaseType = AnyType;
        definition.DerivationMethod = XmlSchemaDerivationMethod.Restriction;
        XmlNode? particleNode = new XmlSchemaParticleCompiler(this).FindSingleXmlSchemaParticle(node, false);
        if (particleNode != null)
        {
            CompiledXmlSchemaParticle compiledParticle = new XmlSchemaParticleCompiler(this).CompileXmlSchemaParticle(particleNode, definition.Module, null);
            definition.Particle = NormalizeXmlSchemaExplicitParticle(compiledParticle);
        }

        CompiledXmlSchemaAttributeSet attributes = new XmlSchemaAttributeCompiler(this).CompileXmlSchemaAttributeSet(node, definition.Module, null);
        definition.Attributes = RemoveProhibitedXmlSchemaAttributes(attributes.Attributes);
        definition.AttributeWildcard = attributes.Wildcard;
        definition.Assertions = CompileXmlSchemaComplexAssertions(node);
        EnsureXmlSchemaMixedContentParticle(definition);
        definition.ContentKind = definition.Mixed ? XmlSchemaContentKind.Mixed : definition.Particle == null ? XmlSchemaContentKind.Empty : XmlSchemaContentKind.ElementOnly;
        definition.OpenContent = CompileEffectiveXmlSchemaOpenContent(node, definition.Module, definition.ContentKind == XmlSchemaContentKind.Empty);
        ApplyXmlSchemaOpenContentToEmptyType(definition);
    }

    internal void CompileXmlSchemaComplexContent(CompiledXmlSchemaComplexType definition, XmlNode content)
    {
        XmlNode derivation = RequireXmlSchemaContentDerivation(content);
        string? baseValue = FindXmlSchemaAttribute(derivation, "base", "");
        if (baseValue == null)
        {
            throw CreateXmlSchemaComplexTypeError(derivation, "Complex content derivation requires a base attribute.");
        }

        CompiledXmlSchemaComplexType baseType = ResolveXmlSchemaComplexTypeReference(baseValue, derivation, definition.Module, definition);
        CompileUserXmlSchemaComplexType(baseType);
        if (baseType.ContentKind == XmlSchemaContentKind.Simple)
        {
            throw CreateXmlSchemaComplexTypeError(derivation, "Complex content cannot be derived from a complex type with simple content.");
        }

        XmlSchemaDerivationMethod method = IsXmlSchemaElement(derivation, "extension") ? XmlSchemaDerivationMethod.Extension : XmlSchemaDerivationMethod.Restriction;
        EnsureXmlSchemaComplexDerivationAllowed(baseType, method, derivation);
        definition.BaseType = baseType;
        definition.DerivationMethod = method;

        string? mixedValue = FindXmlSchemaAttribute(content, "mixed", "");
        if (mixedValue != null)
        {
            bool contentMixed = ParseXmlSchemaBooleanValue(content, mixedValue, "mixed");
            string? typeMixedValue = definition.Node == null ? null : FindXmlSchemaAttribute(definition.Node, "mixed", "");
            if (typeMixedValue != null && definition.Mixed != contentMixed)
            {
                throw CreateXmlSchemaComplexTypeError(content, "The mixed attributes on complexType and complexContent must agree.");
            }

            definition.Mixed = contentMixed;
        }

        XmlNode? localParticleNode = new XmlSchemaParticleCompiler(this).FindSingleXmlSchemaParticle(derivation, false);
        CompiledXmlSchemaParticle? compiledLocalParticle = localParticleNode == null ? null : new XmlSchemaParticleCompiler(this).CompileXmlSchemaParticle(localParticleNode, definition.Module, null);
        CompiledXmlSchemaParticle? localParticle = compiledLocalParticle == null ? null : NormalizeXmlSchemaExplicitParticle(compiledLocalParticle);
        CompiledXmlSchemaAttributeSet localAttributes = new XmlSchemaAttributeCompiler(this).CompileXmlSchemaAttributeSet(derivation, definition.Module, null);
        if (method == XmlSchemaDerivationMethod.Extension)
        {
            definition.Particle = CombineExtendedXmlSchemaParticles(baseType.Particle, localParticle);
            definition.Attributes = MergeExtendedXmlSchemaAttributes(baseType.Attributes, localAttributes.Attributes, derivation);
            definition.AttributeWildcard = new XmlSchemaAttributeCompiler(this).CombineXmlSchemaAttributeWildcards(baseType.AttributeWildcard, localAttributes.Wildcard, XmlSchemaWildcardCombination.Union);
            definition.Assertions = CombineXmlSchemaAssertions(baseType.Assertions, CompileXmlSchemaComplexAssertions(derivation));
        }
        else
        {
            definition.Particle = localParticle;
            definition.Attributes = MergeRestrictedXmlSchemaAttributes(baseType.Attributes, localAttributes.Attributes);
            definition.AttributeWildcard = localAttributes.Wildcard;
            definition.Assertions = CompileXmlSchemaComplexAssertions(derivation);
        }

        if (method == XmlSchemaDerivationMethod.Extension && localParticle == null && (baseType.ContentKind == XmlSchemaContentKind.ElementOnly || baseType.ContentKind == XmlSchemaContentKind.Mixed))
        {
            definition.ContentKind = baseType.ContentKind;
            definition.Mixed = baseType.Mixed;
        }
        else
        {
            EnsureXmlSchemaMixedContentParticle(definition);
            definition.ContentKind = definition.Mixed ? XmlSchemaContentKind.Mixed : definition.Particle == null ? XmlSchemaContentKind.Empty : XmlSchemaContentKind.ElementOnly;
        }

        definition.OpenContent = CompileEffectiveXmlSchemaOpenContent(derivation, definition.Module, definition.ContentKind == XmlSchemaContentKind.Empty);
        if (definition.OpenContent == null)
        {
            definition.OpenContent = baseType.OpenContent;
        }

        ApplyXmlSchemaOpenContentToEmptyType(definition);
    }

    internal void CompileXmlSchemaSimpleContent(CompiledXmlSchemaComplexType definition, XmlNode content)
    {
        XmlNode derivation = RequireXmlSchemaContentDerivation(content);
        string? baseValue = FindXmlSchemaAttribute(derivation, "base", "");
        if (baseValue == null)
        {
            throw CreateXmlSchemaComplexTypeError(derivation, "Simple content derivation requires a base attribute.");
        }

        XmlSchemaDerivationMethod method = IsXmlSchemaElement(derivation, "extension") ? XmlSchemaDerivationMethod.Extension : XmlSchemaDerivationMethod.Restriction;
        CompiledXmlSchemaSimpleType baseSimple = ResolveXmlSchemaSimpleContentBase(baseValue, derivation, definition, method);
        if (definition.BaseType != null)
        {
            EnsureXmlSchemaComplexDerivationAllowed(definition.BaseType, method, derivation);
        }
        else
        {
            SimpleCompiler.EnsureXmlSchemaSimpleTypeDerivationAllowed(baseSimple, method, derivation);
        }

        definition.DerivationMethod = method;
        if (method == XmlSchemaDerivationMethod.Restriction)
        {
            definition.SimpleContentType = CompileRestrictedXmlSchemaSimpleContentType(baseSimple, derivation, definition.Module);
        }
        else
        {
            if (FindSingleXmlSchemaChild(derivation, "simpleType") != null)
            {
                throw CreateXmlSchemaComplexTypeError(derivation, "Simple-content extension cannot contain an inline simple type.");
            }

            definition.SimpleContentType = baseSimple;
        }

        CompiledXmlSchemaAttributeSet localAttributes = new XmlSchemaAttributeCompiler(this).CompileXmlSchemaAttributeSet(derivation, definition.Module, null);
        if (method == XmlSchemaDerivationMethod.Extension && definition.BaseType != null)
        {
            definition.Attributes = MergeExtendedXmlSchemaAttributes(definition.BaseType.Attributes, localAttributes.Attributes, derivation);
            definition.AttributeWildcard = new XmlSchemaAttributeCompiler(this).CombineXmlSchemaAttributeWildcards(definition.BaseType.AttributeWildcard, localAttributes.Wildcard, XmlSchemaWildcardCombination.Union);
            definition.Assertions = CombineXmlSchemaAssertions(definition.BaseType.Assertions, CompileXmlSchemaComplexAssertions(derivation));
        }
        else
        {
            definition.Attributes = definition.BaseType == null ? RemoveProhibitedXmlSchemaAttributes(localAttributes.Attributes) : MergeRestrictedXmlSchemaAttributes(definition.BaseType.Attributes, localAttributes.Attributes);
            definition.AttributeWildcard = localAttributes.Wildcard;
            definition.Assertions = CompileXmlSchemaComplexAssertions(derivation);
        }

        definition.ContentKind = XmlSchemaContentKind.Simple;
        definition.Mixed = false;
        definition.Particle = null;
    }

    internal CompiledXmlSchemaSimpleType ResolveXmlSchemaSimpleContentBase(string lexicalName, XmlNode node, CompiledXmlSchemaComplexType definition, XmlSchemaDerivationMethod method)
    {
        XmlName name = ResolveXmlSchemaComponentName(lexicalName, node, definition.Module);
        CompiledXmlSchemaSimpleType? simple = FindXmlSchemaSimpleType(name);
        if (simple != null)
        {
            SimpleCompiler.CompileUserXmlSchemaSimpleType(simple);
            definition.BaseSimpleType = simple;
            return simple;
        }

        CompiledXmlSchemaComplexType complex = ResolveXmlSchemaComplexTypeName(name, node, definition);
        CompileUserXmlSchemaComplexType(complex);
        if (complex.ContentKind == XmlSchemaContentKind.Simple && complex.SimpleContentType != null)
        {
            definition.BaseType = complex;
            return complex.SimpleContentType;
        }

        if (method == XmlSchemaDerivationMethod.Restriction && complex.ContentKind == XmlSchemaContentKind.Mixed && IsXmlSchemaParticleEmptiable(complex.Particle))
        {
            definition.BaseType = complex;
            return SimpleCompiler.AnySimpleType;
        }

        if (complex.ContentKind != XmlSchemaContentKind.Simple || complex.SimpleContentType == null)
        {
            throw CreateXmlSchemaComplexTypeError(node, "The base of simple content must be a simple type or a complex type with simple content.");
        }

        throw CreateXmlSchemaComplexTypeError(node, "The simple-content base is invalid.");
    }

    internal CompiledXmlSchemaSimpleType CompileRestrictedXmlSchemaSimpleContentType(CompiledXmlSchemaSimpleType baseType, XmlNode restriction, XmlSchemaModule? module)
    {
        XmlNode? inlineNode = FindSingleXmlSchemaChild(restriction, "simpleType");
        CompiledXmlSchemaSimpleType effectiveBase = inlineNode == null ? baseType : SimpleCompiler.CompileAnonymousXmlSchemaSimpleType(inlineNode, module);
        if (inlineNode != null && !IsXmlSchemaSimpleTypeDerivedFrom(effectiveBase, baseType))
        {
            throw CreateXmlSchemaComplexTypeError(inlineNode, "The inline simple type is not validly derived from the simple-content base type.");
        }

        if (effectiveBase.Variety == XmlSchemaSimpleVariety.Universal || effectiveBase.HasAtomicType && effectiveBase.AtomicType == XdmAtomicType.AnyAtomicType)
        {
            throw CreateXmlSchemaComplexTypeError(restriction, "Universal simple types cannot be restricted as simple content.");
        }

        CompiledXmlSchemaSimpleType result = new CompiledXmlSchemaSimpleType(null, false, true, restriction, module);
        result.Variety = effectiveBase.Variety;
        result.BaseType = effectiveBase;
        result.ItemType = effectiveBase.ItemType;
        result.MemberTypes = effectiveBase.MemberTypes;
        result.DerivationMethod = XmlSchemaDerivationMethod.Restriction;
        result.AtomicType = effectiveBase.AtomicType;
        result.HasAtomicType = effectiveBase.HasAtomicType;
        XmlSchemaFacetCompiler facets = new XmlSchemaFacetCompiler(SimpleCompiler);
        result.DeclaredFacets = facets.CompileDeclaredXmlSchemaFacets(result, restriction, true);
        result.EffectiveFacets = facets.MergeEffectiveXmlSchemaFacets(result, effectiveBase.EffectiveFacets, result.DeclaredFacets);
        result.FundamentalFacets = SimpleCompiler.CalculateUserXmlSchemaFundamentalFacets(result);
        result.IsCompiled = true;
        SimpleCompiler.Types.Add(result);
        SimpleCompiler.UserTypes.Add(result);
        return result;
    }

    internal bool IsXmlSchemaSimpleTypeDerivedFrom(CompiledXmlSchemaSimpleType candidate, CompiledXmlSchemaSimpleType baseType)
    {
        CompiledXmlSchemaSimpleType? current = candidate;
        while (current != null)
        {
            if (current == baseType)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    internal XmlNode RequireXmlSchemaContentDerivation(XmlNode content)
    {
        XmlNode? extension = FindSingleXmlSchemaChild(content, "extension");
        XmlNode? restriction = FindSingleXmlSchemaChild(content, "restriction");
        if (extension == null && restriction == null || extension != null && restriction != null)
        {
            throw CreateXmlSchemaComplexTypeError(content, "Content requires exactly one extension or restriction child.");
        }

        if (extension != null)
        {
            return extension;
        }

        if (restriction == null)
        {
            throw CreateXmlSchemaComplexTypeError(content, "Content derivation is missing.");
        }

        return restriction;
    }

    internal CompiledXmlSchemaParticle? CombineExtendedXmlSchemaParticles(CompiledXmlSchemaParticle? baseParticle, CompiledXmlSchemaParticle? localParticle)
    {
        if (baseParticle == null)
        {
            return localParticle;
        }

        if (localParticle == null)
        {
            return baseParticle;
        }

        if (baseParticle.Kind == XmlSchemaParticleKind.All && localParticle.Kind == XmlSchemaParticleKind.All)
        {
            CompiledXmlSchemaParticle all = new CompiledXmlSchemaParticle(XmlSchemaParticleKind.All, null);
            all.MinOccurs = localParticle.MinOccurs;
            all.Children = new CompiledXmlSchemaParticle[baseParticle.Children.Length + localParticle.Children.Length];
            if (baseParticle.Children.Length > 0)
            {
                Array.Copy(baseParticle.Children, all.Children, baseParticle.Children.Length);
            }

            if (localParticle.Children.Length > 0)
            {
                Array.Copy(localParticle.Children, 0, all.Children, baseParticle.Children.Length, localParticle.Children.Length);
            }

            return all;
        }

        CompiledXmlSchemaParticle sequence = new CompiledXmlSchemaParticle(XmlSchemaParticleKind.Sequence, null);
        sequence.Children = [baseParticle, localParticle];
        return sequence;
    }

    internal CompiledXmlSchemaParticle? NormalizeXmlSchemaExplicitParticle(CompiledXmlSchemaParticle particle)
    {
        if (particle.MaxOccurs == System.Numerics.BigInteger.Zero && !particle.MaxOccursUnbounded)
        {
            return null;
        }

        if ((particle.Kind == XmlSchemaParticleKind.Sequence || particle.Kind == XmlSchemaParticleKind.All) && particle.Children.Length == 0)
        {
            return null;
        }

        if (particle.Kind == XmlSchemaParticleKind.Choice && particle.Children.Length == 0 && particle.MinOccurs == System.Numerics.BigInteger.Zero)
        {
            return null;
        }

        if (particle.Kind == XmlSchemaParticleKind.GroupReference && particle.Children.Length == 1 && NormalizeXmlSchemaExplicitParticle(particle.Children[0]) == null)
        {
            return null;
        }

        return particle;
    }

    internal void EnsureXmlSchemaMixedContentParticle(CompiledXmlSchemaComplexType definition)
    {
        if (definition.Mixed && definition.Particle == null)
        {
            definition.Particle = new CompiledXmlSchemaParticle(XmlSchemaParticleKind.Sequence, null);
        }
    }

    internal CompiledXmlSchemaAttributeUse[] MergeExtendedXmlSchemaAttributes(CompiledXmlSchemaAttributeUse[] inherited, CompiledXmlSchemaAttributeUse[] local, XmlNode node)
    {
        List<CompiledXmlSchemaAttributeUse> result = new List<CompiledXmlSchemaAttributeUse>();
        int inheritedIndex = 0;
        while (inheritedIndex < inherited.Length)
        {
            result.Add(inherited[inheritedIndex]);
            inheritedIndex = inheritedIndex + 1;
        }

        int localIndex = 0;
        while (localIndex < local.Length)
        {
            CompiledXmlSchemaAttributeUse candidate = local[localIndex];
            if (FindXmlSchemaAttributeUse(result, candidate.Name) != null)
            {
                throw CreateXmlSchemaComplexTypeError(node, "Complex-content extension cannot redeclare an inherited attribute use.");
            }

            if (candidate.Use != XmlSchemaAttributeUseKind.Prohibited)
            {
                result.Add(candidate);
            }

            localIndex = localIndex + 1;
        }

        return result.ToArray();
    }

    internal CompiledXmlSchemaAttributeUse[] RemoveProhibitedXmlSchemaAttributes(CompiledXmlSchemaAttributeUse[] attributes)
    {
        List<CompiledXmlSchemaAttributeUse> result = new List<CompiledXmlSchemaAttributeUse>();
        int index = 0;
        while (index < attributes.Length)
        {
            if (attributes[index].Use != XmlSchemaAttributeUseKind.Prohibited)
            {
                result.Add(attributes[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal CompiledXmlSchemaAttributeUse[] MergeRestrictedXmlSchemaAttributes(CompiledXmlSchemaAttributeUse[] inherited, CompiledXmlSchemaAttributeUse[] local)
    {
        List<CompiledXmlSchemaAttributeUse> result = new List<CompiledXmlSchemaAttributeUse>();
        int localIndex = 0;
        while (localIndex < local.Length)
        {
            if (local[localIndex].Use != XmlSchemaAttributeUseKind.Prohibited)
            {
                result.Add(local[localIndex]);
            }

            localIndex = localIndex + 1;
        }

        int inheritedIndex = 0;
        while (inheritedIndex < inherited.Length)
        {
            if (!HasLocalXmlSchemaAttributeUse(local, inherited[inheritedIndex].Name))
            {
                result.Add(inherited[inheritedIndex]);
            }

            inheritedIndex = inheritedIndex + 1;
        }

        return result.ToArray();
    }

    internal bool HasLocalXmlSchemaAttributeUse(CompiledXmlSchemaAttributeUse[] attributes, XmlName name)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            if (SameXmlSchemaComponentName(attributes[index].Name, name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal CompiledXmlSchemaAttributeUse? FindXmlSchemaAttributeUse(List<CompiledXmlSchemaAttributeUse> attributes, XmlName name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (SameXmlSchemaComponentName(attributes[index].Name, name))
            {
                return attributes[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal string[] CompileXmlSchemaComplexAssertions(XmlNode parent)
    {
        List<string> assertions = new List<string>();
        XmlNode[] children = parent.Children == null ? [] : parent.Children;
        int index = 0;
        while (index < children.Length)
        {
            if (IsXmlSchemaElement(children[index], "assert"))
            {
                string? test = FindXmlSchemaAttribute(children[index], "test", "");
                if (test == null || new XdmStringDatatype().CollapseXdmStringWhitespace(test).Length == 0)
                {
                    throw CreateXmlSchemaComplexTypeError(children[index], "A complex-type assertion requires a non-empty test attribute.");
                }

                assertions.Add(test);
            }

            index = index + 1;
        }

        return assertions.ToArray();
    }

    internal string[] CombineXmlSchemaAssertions(string[] inherited, string[] local)
    {
        string[] result = new string[inherited.Length + local.Length];
        if (inherited.Length > 0)
        {
            Array.Copy(inherited, result, inherited.Length);
        }

        if (local.Length > 0)
        {
            Array.Copy(local, 0, result, inherited.Length, local.Length);
        }

        return result;
    }

    internal CompiledXmlSchemaOpenContent? CompileEffectiveXmlSchemaOpenContent(XmlNode parent, XmlSchemaModule? module, bool emptyContent)
    {
        XmlNode? explicitNode = FindSingleXmlSchemaChild(parent, "openContent");
        if (explicitNode != null)
        {
            return CompileXmlSchemaOpenContent(explicitNode, module, false);
        }

        if (module == null)
        {
            return null;
        }

        XmlNode? root = module.Document.DocumentElement;
        if (root == null)
        {
            return null;
        }

        XmlNode? defaultNode = FindSingleXmlSchemaChild(root, "defaultOpenContent");
        if (defaultNode == null)
        {
            return null;
        }

        bool appliesToEmpty = ParseOptionalXmlSchemaBoolean(defaultNode, "appliesToEmpty", false);
        if (emptyContent && !appliesToEmpty)
        {
            return null;
        }

        return CompileXmlSchemaOpenContent(defaultNode, module, true);
    }

    internal CompiledXmlSchemaOpenContent CompileXmlSchemaOpenContent(XmlNode node, XmlSchemaModule? module, bool defaultValue)
    {
        CompiledXmlSchemaOpenContent result = new CompiledXmlSchemaOpenContent(node);
        result.IsDefault = defaultValue;
        string? mode = FindXmlSchemaAttribute(node, "mode", "");
        if (mode == "none")
        {
            if (defaultValue)
            {
                throw CreateXmlSchemaComplexTypeError(node, "defaultOpenContent cannot use mode none.");
            }

            result.Mode = XmlSchemaOpenContentMode.None;
        }
        else if (mode == null || mode == "interleave")
        {
            result.Mode = XmlSchemaOpenContentMode.Interleave;
        }
        else if (mode == "suffix")
        {
            result.Mode = XmlSchemaOpenContentMode.Suffix;
        }
        else
        {
            throw CreateXmlSchemaComplexTypeError(node, "Open-content mode must be none, interleave, or suffix.");
        }

        XmlNode? any = FindSingleXmlSchemaChild(node, "any");
        if (any != null && (FindXmlSchemaAttribute(any, "minOccurs", "") != null || FindXmlSchemaAttribute(any, "maxOccurs", "") != null))
        {
            throw CreateXmlSchemaComplexTypeError(any, "An open-content wildcard cannot specify occurrence constraints.");
        }

        if (result.Mode == XmlSchemaOpenContentMode.None)
        {
            if (any != null)
            {
                throw CreateXmlSchemaComplexTypeError(node, "Open content with mode none cannot contain a wildcard.");
            }
        }
        else
        {
            if (any == null)
            {
                throw CreateXmlSchemaComplexTypeError(node, "Open content requires an xs:any wildcard.");
            }

            result.Wildcard = new XmlSchemaParticleCompiler(this).CompileXmlSchemaWildcard(any, module);
        }

        return result;
    }

    internal void ApplyXmlSchemaOpenContentToEmptyType(CompiledXmlSchemaComplexType definition)
    {
        if (definition.ContentKind != XmlSchemaContentKind.Empty || definition.OpenContent == null || definition.OpenContent.Mode == XmlSchemaOpenContentMode.None)
        {
            return;
        }

        definition.ContentKind = XmlSchemaContentKind.ElementOnly;
        definition.Particle = new CompiledXmlSchemaParticle(XmlSchemaParticleKind.Sequence, null);
    }

    internal bool IsXmlSchemaParticleEmptiable(CompiledXmlSchemaParticle? particle)
    {
        if (particle == null || particle.MinOccurs == System.Numerics.BigInteger.Zero)
        {
            return true;
        }

        if (particle.Kind == XmlSchemaParticleKind.Element || particle.Kind == XmlSchemaParticleKind.Any)
        {
            return false;
        }

        if (particle.Kind == XmlSchemaParticleKind.Choice)
        {
            int choiceIndex = 0;
            while (choiceIndex < particle.Children.Length)
            {
                if (IsXmlSchemaParticleEmptiable(particle.Children[choiceIndex]))
                {
                    return true;
                }

                choiceIndex = choiceIndex + 1;
            }

            return false;
        }

        int index = 0;
        while (index < particle.Children.Length)
        {
            if (!IsXmlSchemaParticleEmptiable(particle.Children[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal XmlSchemaDerivationMethod ParseXmlSchemaComplexDerivationSet(XmlNode node, string localName, string defaultName, bool allowSubstitution)
    {
        string? value = FindXmlSchemaAttribute(node, localName, "");
        bool inherited = false;
        if (value == null)
        {
            XmlNode? ancestor = node.Parent;
            while (ancestor != null && !IsXmlSchemaElement(ancestor, "schema"))
            {
                ancestor = ancestor.Parent;
            }

            if (ancestor != null)
            {
                value = FindXmlSchemaAttribute(ancestor, defaultName, "");
                inherited = value != null;
            }
        }

        if (value == null || value.Length == 0)
        {
            return XmlSchemaDerivationMethod.None;
        }

        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(value);
        if (normalized == "#all")
        {
            XmlSchemaDerivationMethod all = XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction;
            return allowSubstitution ? all | XmlSchemaDerivationMethod.Substitution : all;
        }

        XmlSchemaDerivationMethod result = XmlSchemaDerivationMethod.None;
        string[] tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index] == "extension")
            {
                result = result | XmlSchemaDerivationMethod.Extension;
            }
            else if (tokens[index] == "restriction")
            {
                result = result | XmlSchemaDerivationMethod.Restriction;
            }
            else if (allowSubstitution && tokens[index] == "substitution")
            {
                result = result | XmlSchemaDerivationMethod.Substitution;
            }
            else if (inherited && (tokens[index] == "list" || tokens[index] == "union" || tokens[index] == "substitution"))
            {
                result = result | XmlSchemaDerivationMethod.None;
            }
            else
            {
                throw CreateXmlSchemaComplexTypeError(node, "The derivation-control attribute contains an invalid token.");
            }

            index = index + 1;
        }

        return result;
    }

    internal bool ParseOptionalXmlSchemaBoolean(XmlNode node, string localName, bool defaultValue)
    {
        string? value = FindXmlSchemaAttribute(node, localName, "");
        return value == null ? defaultValue : ParseXmlSchemaBooleanValue(node, value, localName);
    }

    internal bool ParseXmlSchemaBooleanValue(XmlNode node, string value, string localName)
    {
        XdmNumericParseResult parsed = new XdmNumericDatatype().TryParseXdmBoolean(value);
        if (!parsed.Success || parsed.Value == null)
        {
            throw CreateXmlSchemaComplexTypeError(node, "The " + localName + " attribute must be a valid xs:boolean.");
        }

        return parsed.Value.BooleanValue;
    }

    internal void EnsureXmlSchemaComplexDerivationAllowed(CompiledXmlSchemaComplexType baseType, XmlSchemaDerivationMethod method, XmlNode node)
    {
        if (((int)baseType.Final & (int)method) != 0)
        {
            throw CreateXmlSchemaComplexTypeError(node, "The base complex type prohibits the requested derivation method.");
        }
    }

    internal CompiledXmlSchemaComplexType ResolveXmlSchemaComplexTypeReference(string lexicalName, XmlNode node, XmlSchemaModule? module, CompiledXmlSchemaComplexType current)
    {
        XmlName name = ResolveXmlSchemaComponentName(lexicalName, node, module);
        return ResolveXmlSchemaComplexTypeName(name, node, current);
    }

    internal CompiledXmlSchemaComplexType ResolveXmlSchemaComplexTypeName(XmlName name, XmlNode node, CompiledXmlSchemaComplexType current)
    {
        string key = CreateXmlSchemaComponentKey(name);
        XmlName? currentName = current.Name;
        if (current.PreviousDefinition != null && currentName != null && key == CreateXmlSchemaComponentKey(currentName))
        {
            return current.PreviousDefinition;
        }

        if (!TypesByName.ContainsKey(key))
        {
            throw CreateXmlSchemaComplexTypeError(node, "The referenced complex type is not declared.");
        }

        return TypesByName[key];
    }

    internal CompiledXmlSchemaGroup ResolveXmlSchemaGroupReference(string lexicalName, XmlNode node, XmlSchemaModule? module, CompiledXmlSchemaGroup? current)
    {
        XmlName name = ResolveXmlSchemaComponentName(lexicalName, node, module);
        string key = CreateXmlSchemaComponentKey(name);
        if (current != null && current.PreviousDefinition != null && key == CreateXmlSchemaComponentKey(current.Name))
        {
            return current.PreviousDefinition;
        }

        if (!GroupsByName.ContainsKey(key))
        {
            throw CreateXmlSchemaComplexTypeError(node, "The referenced model group is not declared.");
        }

        return GroupsByName[key];
    }

    internal CompiledXmlSchemaAttributeGroup ResolveXmlSchemaAttributeGroupReference(string lexicalName, XmlNode node, XmlSchemaModule? module, CompiledXmlSchemaAttributeGroup? current)
    {
        XmlName name = ResolveXmlSchemaComponentName(lexicalName, node, module);
        string key = CreateXmlSchemaComponentKey(name);
        if (current != null && current.PreviousDefinition != null && key == CreateXmlSchemaComponentKey(current.Name))
        {
            return current.PreviousDefinition;
        }

        if (!AttributeGroupsByName.ContainsKey(key))
        {
            throw CreateXmlSchemaComplexTypeError(node, "The referenced attribute group is not declared.");
        }

        return AttributeGroupsByName[key];
    }

    internal CompiledXmlSchemaSimpleType? FindXmlSchemaSimpleType(XmlName name)
    {
        string key = CreateXmlSchemaComponentKey(name);
        int index = SimpleCompiler.Types.Count - 1;
        while (index >= 0)
        {
            XmlName? candidate = SimpleCompiler.Types[index].Name;
            if (candidate != null && CreateXmlSchemaComponentKey(candidate) == key)
            {
                return SimpleCompiler.Types[index];
            }

            index = index - 1;
        }

        return null;
    }

    internal XmlName ResolveXmlSchemaComponentName(string lexicalName, XmlNode node, XmlSchemaModule? module)
    {
        XPathStaticContext context = SimpleCompiler.CreateXmlSchemaQNameContext(node);
        XdmNumericParseResult parsed = new XdmQNameDatatype().TryParseXdmQNameWithNamespaces(lexicalName, XdmAtomicType.QName, context.Namespaces == null ? [] : context.Namespaces, context.DefaultElementNamespace);
        if (!parsed.Success || parsed.Value == null || parsed.Value.NameValue == null)
        {
            throw CreateXmlSchemaComplexTypeError(node, "The component reference is not a resolvable QName.");
        }

        XmlName name = parsed.Value.NameValue;
        if (name.Prefix == null && (name.NamespaceUri == null || name.NamespaceUri.Length == 0) && module != null && module.IsChameleon)
        {
            name.NamespaceUri = module.EffectiveTargetNamespace;
        }

        return name;
    }

    internal XmlName RequireGlobalXmlSchemaComponentName(XmlNode node, XmlSchemaModule module, string description)
    {
        string? value = FindXmlSchemaAttribute(node, "name", "");
        if (value == null || !new XdmStringDatatype().IsValidXdmStringLexical(value, XdmAtomicType.NcName))
        {
            throw CreateXmlSchemaComplexTypeError(node, "A global " + description + " requires a valid NCName name.");
        }

        return CreateXmlSchemaComponentName(value, module.EffectiveTargetNamespace, null);
    }

    internal XmlName CreateXmlSchemaComponentName(string localName, string? namespaceUri, string? prefix)
    {
        XmlName name = new XmlName();
        name.LocalName = localName;
        name.NamespaceUri = namespaceUri;
        name.Prefix = prefix;
        return name;
    }

    internal string? ResolveXmlSchemaLocalDeclarationNamespace(XmlNode node, XmlSchemaModule module, string defaultName)
    {
        string? formValue = FindXmlSchemaAttribute(node, "form", "");
        string? targetValue = FindXmlSchemaAttribute(node, "targetNamespace", "");
        if (formValue != null && targetValue != null)
        {
            throw CreateXmlSchemaComplexTypeError(node, "A local declaration cannot specify both form and targetNamespace.");
        }

        if (targetValue != null)
        {
            string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(targetValue);
            if (normalized.Length == 0 || !Uri.IsWellFormedUriString(normalized, UriKind.Absolute))
            {
                throw CreateXmlSchemaComplexTypeError(node, "A local declaration targetNamespace must be an absolute URI.");
            }

            return normalized;
        }

        if (formValue == null)
        {
            XmlNode? root = module.Document.DocumentElement;
            formValue = root == null ? null : FindXmlSchemaAttribute(root, defaultName, "");
        }

        if (formValue == null || formValue == "unqualified")
        {
            return null;
        }

        if (formValue == "qualified")
        {
            return module.EffectiveTargetNamespace;
        }

        throw CreateXmlSchemaComplexTypeError(node, "Schema form values must be qualified or unqualified.");
    }

    internal string CreateXmlSchemaComponentKey(XmlName name)
    {
        string namespaceUri = name.NamespaceUri == null ? "" : name.NamespaceUri;
        string localName = name.LocalName == null ? "" : name.LocalName;
        return namespaceUri + "\u001f" + localName;
    }

    internal bool SameXmlSchemaComponentName(XmlName left, XmlName right)
    {
        return string.Equals(left.NamespaceUri, right.NamespaceUri, StringComparison.Ordinal) && string.Equals(left.LocalName, right.LocalName, StringComparison.Ordinal);
    }

    internal XmlNode? FindSingleXmlSchemaChild(XmlNode parent, string localName)
    {
        XmlNode? found = null;
        XmlNode[] children = parent.Children == null ? [] : parent.Children;
        int index = 0;
        while (index < children.Length)
        {
            if (IsXmlSchemaElement(children[index], localName))
            {
                if (found != null)
                {
                    throw CreateXmlSchemaComplexTypeError(children[index], "This XML Schema child may occur only once.");
                }

                found = children[index];
            }

            index = index + 1;
        }

        return found;
    }

    internal bool IsXmlSchemaElement(XmlNode node, string localName)
    {
        return SimpleCompiler.IsXmlSchemaElement(node, localName);
    }

    internal string? FindXmlSchemaAttribute(XmlNode node, string localName, string namespaceUri)
    {
        return SimpleCompiler.FindXmlSchemaSimpleTypeAttribute(node, localName, namespaceUri);
    }

    internal XmlSchemaException CreateXmlSchemaComplexTypeError(XmlNode? node, string message)
    {
        return SimpleCompiler.CreateXmlSchemaSimpleTypeError(node, message);
    }
}
