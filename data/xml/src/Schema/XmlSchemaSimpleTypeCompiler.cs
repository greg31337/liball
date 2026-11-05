using System;
using System.Collections.Generic;

namespace xml;

internal class XmlSchemaSimpleTypeCompiler
{
    internal XmlSchemaModule[] Modules;
    internal List<CompiledXmlSchemaSimpleType> Types;
    internal List<CompiledXmlSchemaSimpleType> UserTypes;
    internal Dictionary<string, CompiledXmlSchemaSimpleType> TypesByName;
    internal Dictionary<XdmAtomicType, CompiledXmlSchemaSimpleType> AtomicBuiltIns;
    internal XdmTypeSystem XdmTypes;
    internal CompiledXmlSchemaSimpleType AnySimpleType;

    internal XmlSchemaSimpleTypeCompiler(XmlSchemaModule[] modules)
    {
        Guard.NotNull(modules, "modules");
        Modules = modules;
        Types = new List<CompiledXmlSchemaSimpleType>();
        UserTypes = new List<CompiledXmlSchemaSimpleType>();
        TypesByName = new Dictionary<string, CompiledXmlSchemaSimpleType>(StringComparer.Ordinal);
        AtomicBuiltIns = new Dictionary<XdmAtomicType, CompiledXmlSchemaSimpleType>();
        XdmTypes = new XdmTypeSystem();
        XmlName anySimpleName = CreateXmlSchemaTypeName("anySimpleType", XmlNamespaces.XmlSchema, "xs");
        AnySimpleType = new CompiledXmlSchemaSimpleType(anySimpleName, true, false, null, null);
    }

    internal CompiledXmlSchemaSimpleType[] CompileXmlSchemaSimpleTypes()
    {
        CreateBuiltInXmlSchemaSimpleTypes();
        CollectGlobalXmlSchemaSimpleTypes();
        CollectRedefinedAndOverriddenXmlSchemaSimpleTypes();
        int index = 0;
        while (index < UserTypes.Count)
        {
            CompileUserXmlSchemaSimpleType(UserTypes[index]);
            index = index + 1;
        }

        return Types.ToArray();
    }

    internal void CreateBuiltInXmlSchemaSimpleTypes()
    {
        AnySimpleType.Variety = XmlSchemaSimpleVariety.Universal;
        AnySimpleType.IsCompiled = true;
        AnySimpleType.FundamentalFacets = new XmlSchemaFundamentalFacets(XmlSchemaOrderKind.None, false, false, false);
        AddNamedXmlSchemaSimpleType(AnySimpleType, false, null);

        XdmAtomicTypeDefinition[] atomicDefinitions = XdmTypes.GetAllXdmAtomicTypeDefinitions();
        int index = 0;
        while (index < atomicDefinitions.Length)
        {
            XdmAtomicTypeDefinition atomicDefinition = atomicDefinitions[index];
            XmlName name = CreateXmlSchemaTypeName(XmlSchemaTypeLocalName(atomicDefinition), XmlNamespaces.XmlSchema, "xs");
            CompiledXmlSchemaSimpleType definition = new CompiledXmlSchemaSimpleType(name, true, false, null, null);
            definition.Variety = XmlSchemaSimpleVariety.Atomic;
            definition.AtomicType = atomicDefinition.Type;
            definition.HasAtomicType = true;
            definition.DerivationMethod = XmlSchemaDerivationMethod.Restriction;
            AtomicBuiltIns.Add(atomicDefinition.Type, definition);
            AddNamedXmlSchemaSimpleType(definition, false, null);
            index = index + 1;
        }

        ConnectBuiltInXmlSchemaAtomicBases(atomicDefinitions);
        CreateBuiltInXmlSchemaListTypes();
        ApplyBuiltInXmlSchemaFacets();
        CalculateBuiltInXmlSchemaFundamentalFacets();
    }

    internal void ConnectBuiltInXmlSchemaAtomicBases(XdmAtomicTypeDefinition[] atomicDefinitions)
    {
        int index = 0;
        while (index < atomicDefinitions.Length)
        {
            XdmAtomicTypeDefinition atomicDefinition = atomicDefinitions[index];
            CompiledXmlSchemaSimpleType definition = AtomicBuiltIns[atomicDefinition.Type];
            if (atomicDefinition.Type == XdmAtomicType.AnyAtomicType)
            {
                definition.BaseType = AnySimpleType;
            }
            else
            {
                definition.BaseType = AtomicBuiltIns[atomicDefinition.BaseType];
            }

            index = index + 1;
        }
    }

    internal void CreateBuiltInXmlSchemaListTypes()
    {
        CreateOneBuiltInXmlSchemaListType("NMTOKENS", XdmAtomicType.Nmtoken);
        CreateOneBuiltInXmlSchemaListType("IDREFS", XdmAtomicType.Idref);
        CreateOneBuiltInXmlSchemaListType("ENTITIES", XdmAtomicType.Entity);
    }

    internal void CreateOneBuiltInXmlSchemaListType(string localName, XdmAtomicType itemAtomicType)
    {
        CompiledXmlSchemaSimpleType itemType = AtomicBuiltIns[itemAtomicType];
        CompiledXmlSchemaSimpleType listBase = new CompiledXmlSchemaSimpleType(null, true, true, null, null);
        listBase.Variety = XmlSchemaSimpleVariety.List;
        listBase.BaseType = AnySimpleType;
        listBase.ItemType = itemType;
        listBase.DerivationMethod = XmlSchemaDerivationMethod.List;
        Types.Add(listBase);

        XmlName name = CreateXmlSchemaTypeName(localName, XmlNamespaces.XmlSchema, "xs");
        CompiledXmlSchemaSimpleType definition = new CompiledXmlSchemaSimpleType(name, true, false, null, null);
        definition.Variety = XmlSchemaSimpleVariety.List;
        definition.BaseType = listBase;
        definition.ItemType = itemType;
        definition.DerivationMethod = XmlSchemaDerivationMethod.Restriction;
        AddNamedXmlSchemaSimpleType(definition, false, null);
    }

    internal void ApplyBuiltInXmlSchemaFacets()
    {
        AddBuiltInXmlSchemaFacet(XdmAtomicType.String, XmlSchemaFacetKind.WhiteSpace, "preserve", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.NormalizedString, XmlSchemaFacetKind.WhiteSpace, "replace", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Token, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Language, XmlSchemaFacetKind.Pattern, "[a-zA-Z]{1,8}(-[a-zA-Z0-9]{1,8})*", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Name, XmlSchemaFacetKind.Pattern, "\\i\\c*", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.NcName, XmlSchemaFacetKind.Pattern, "[\\i-[:]][\\c-[:]]*", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Nmtoken, XmlSchemaFacetKind.Pattern, "\\c+", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Boolean, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Decimal, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Integer, XmlSchemaFacetKind.Pattern, "[-+]?[0-9]+", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Integer, XmlSchemaFacetKind.FractionDigits, "0", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Float, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Double, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Duration, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.DateTime, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.DateTimeStamp, XmlSchemaFacetKind.ExplicitTimezone, "required", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Date, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Time, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.GYearMonth, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.GYear, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.GMonthDay, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.GMonth, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.GDay, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.HexBinary, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Base64Binary, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.AnyUri, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.QName, XmlSchemaFacetKind.WhiteSpace, "collapse", true);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Notation, XmlSchemaFacetKind.WhiteSpace, "collapse", true);

        AddBuiltInXmlSchemaFacet(XdmAtomicType.NonPositiveInteger, XmlSchemaFacetKind.MaxInclusive, "0", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.NegativeInteger, XmlSchemaFacetKind.MaxInclusive, "-1", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Long, XmlSchemaFacetKind.MinInclusive, "-9223372036854775808", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Long, XmlSchemaFacetKind.MaxInclusive, "9223372036854775807", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Int, XmlSchemaFacetKind.MinInclusive, "-2147483648", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Int, XmlSchemaFacetKind.MaxInclusive, "2147483647", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Short, XmlSchemaFacetKind.MinInclusive, "-32768", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Short, XmlSchemaFacetKind.MaxInclusive, "32767", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Byte, XmlSchemaFacetKind.MinInclusive, "-128", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.Byte, XmlSchemaFacetKind.MaxInclusive, "127", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.NonNegativeInteger, XmlSchemaFacetKind.MinInclusive, "0", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.UnsignedLong, XmlSchemaFacetKind.MaxInclusive, "18446744073709551615", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.UnsignedInt, XmlSchemaFacetKind.MaxInclusive, "4294967295", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.UnsignedShort, XmlSchemaFacetKind.MaxInclusive, "65535", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.UnsignedByte, XmlSchemaFacetKind.MaxInclusive, "255", false);
        AddBuiltInXmlSchemaFacet(XdmAtomicType.PositiveInteger, XmlSchemaFacetKind.MinInclusive, "1", false);

        int index = 0;
        while (index < Types.Count)
        {
            CompiledXmlSchemaSimpleType definition = Types[index];
            if (definition.Variety == XmlSchemaSimpleVariety.List)
            {
                if (definition.IsAnonymous)
                {
                    definition.DeclaredFacets = [new CompiledXmlSchemaFacet(XmlSchemaFacetKind.WhiteSpace, "collapse", true, true, null)];
                }
                else
                {
                    definition.DeclaredFacets = [new CompiledXmlSchemaFacet(XmlSchemaFacetKind.MinLength, "1", false, true, null)];
                }
            }

            definition.EffectiveFacets = MergeBuiltInXmlSchemaFacets(definition.BaseType, definition.DeclaredFacets);
            index = index + 1;
        }
    }

    internal void AddBuiltInXmlSchemaFacet(XdmAtomicType atomicType, XmlSchemaFacetKind kind, string value, bool fixedValue)
    {
        CompiledXmlSchemaSimpleType definition = AtomicBuiltIns[atomicType];
        CompiledXmlSchemaFacet[] current = definition.DeclaredFacets;
        CompiledXmlSchemaFacet[] expanded = new CompiledXmlSchemaFacet[current.Length + 1];
        if (current.Length > 0)
        {
            Array.Copy(current, expanded, current.Length);
        }

        expanded[current.Length] = new CompiledXmlSchemaFacet(kind, value, fixedValue, true, null);
        definition.DeclaredFacets = expanded;
    }

    internal CompiledXmlSchemaFacet[] MergeBuiltInXmlSchemaFacets(CompiledXmlSchemaSimpleType? baseType, CompiledXmlSchemaFacet[] declared)
    {
        List<CompiledXmlSchemaFacet> merged = new List<CompiledXmlSchemaFacet>();
        if (baseType != null)
        {
            int inheritedIndex = 0;
            while (inheritedIndex < baseType.EffectiveFacets.Length)
            {
                CompiledXmlSchemaFacet inherited = baseType.EffectiveFacets[inheritedIndex];
                if (!HasReplacingBuiltInXmlSchemaFacet(declared, inherited.Kind))
                {
                    merged.Add(inherited);
                }

                inheritedIndex = inheritedIndex + 1;
            }
        }

        int declaredIndex = 0;
        while (declaredIndex < declared.Length)
        {
            merged.Add(declared[declaredIndex]);
            declaredIndex = declaredIndex + 1;
        }

        return merged.ToArray();
    }

    internal bool HasReplacingBuiltInXmlSchemaFacet(CompiledXmlSchemaFacet[] facets, XmlSchemaFacetKind kind)
    {
        if (kind == XmlSchemaFacetKind.Pattern || kind == XmlSchemaFacetKind.Enumeration || kind == XmlSchemaFacetKind.Assertion)
        {
            return false;
        }

        int index = 0;
        while (index < facets.Length)
        {
            if (facets[index].Kind == kind)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void CalculateBuiltInXmlSchemaFundamentalFacets()
    {
        int index = 0;
        while (index < Types.Count)
        {
            CompiledXmlSchemaSimpleType definition = Types[index];
            if (definition.Variety == XmlSchemaSimpleVariety.Atomic && definition.HasAtomicType)
            {
                if (XdmTypes.IsXdmPrimitiveAtomicType(definition.AtomicType))
                {
                    definition.FundamentalFacets = CreateAtomicXmlSchemaFundamentalFacets(definition.AtomicType);
                }
                else
                {
                    definition.FundamentalFacets = CalculateUserXmlSchemaFundamentalFacets(definition);
                }
            }
            else if (definition.Variety == XmlSchemaSimpleVariety.List)
            {
                definition.FundamentalFacets = CalculateUserXmlSchemaFundamentalFacets(definition);
            }

            index = index + 1;
        }
    }

    internal XmlSchemaFundamentalFacets CreateAtomicXmlSchemaFundamentalFacets(XdmAtomicType atomicType)
    {
        bool numeric = XdmTypes.IsXdmNumericAtomicType(atomicType);
        XmlSchemaOrderKind ordered = XmlSchemaOrderKind.None;
        if (atomicType == XdmAtomicType.Float || atomicType == XdmAtomicType.Double || XdmTypes.IsXdmDurationDerivedAtomicType(atomicType) || atomicType >= XdmAtomicType.DateTime && atomicType <= XdmAtomicType.GDay)
        {
            ordered = XmlSchemaOrderKind.Partial;
        }
        else if (XdmTypes.IsXdmAtomicSubtypeOf(atomicType, XdmAtomicType.Decimal))
        {
            ordered = XmlSchemaOrderKind.Total;
        }

        bool bounded = atomicType == XdmAtomicType.Float || atomicType == XdmAtomicType.Double || IsBoundedBuiltInXmlSchemaInteger(atomicType);
        bool finite = atomicType == XdmAtomicType.Boolean || atomicType == XdmAtomicType.Float || atomicType == XdmAtomicType.Double || IsBoundedBuiltInXmlSchemaInteger(atomicType);
        return new XmlSchemaFundamentalFacets(ordered, bounded, finite, numeric);
    }

    internal bool IsBoundedBuiltInXmlSchemaInteger(XdmAtomicType atomicType)
    {
        return atomicType == XdmAtomicType.Long || atomicType == XdmAtomicType.Int || atomicType == XdmAtomicType.Short || atomicType == XdmAtomicType.Byte || atomicType == XdmAtomicType.UnsignedLong || atomicType == XdmAtomicType.UnsignedInt || atomicType == XdmAtomicType.UnsignedShort || atomicType == XdmAtomicType.UnsignedByte;
    }

    internal void CollectGlobalXmlSchemaSimpleTypes()
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
                XmlNode child = children[childIndex];
                if (IsXmlSchemaElement(child, "simpleType"))
                {
                    AddGlobalXmlSchemaSimpleType(child, module, false, null);
                }

                childIndex = childIndex + 1;
            }

            moduleIndex = moduleIndex + 1;
        }
    }

    internal void CollectRedefinedAndOverriddenXmlSchemaSimpleTypes()
    {
        int moduleIndex = 0;
        while (moduleIndex < Modules.Length)
        {
            XmlSchemaModule module = Modules[moduleIndex];
            int directiveIndex = 0;
            while (directiveIndex < module.Directives.Length)
            {
                XmlSchemaDirective directive = module.Directives[directiveIndex];
                bool replacement = directive.Kind == XmlSchemaDirectiveKind.Redefine || directive.Kind == XmlSchemaDirectiveKind.Override;
                if (replacement)
                {
                    XmlNode[] children = directive.Node.Children == null ? [] : directive.Node.Children;
                    int childIndex = 0;
                    while (childIndex < children.Length)
                    {
                        XmlNode child = children[childIndex];
                        if (IsXmlSchemaElement(child, "simpleType"))
                        {
                            AddGlobalXmlSchemaSimpleType(child, module, true, directive);
                        }

                        childIndex = childIndex + 1;
                    }
                }

                directiveIndex = directiveIndex + 1;
            }

            moduleIndex = moduleIndex + 1;
        }
    }

    internal CompiledXmlSchemaSimpleType AddGlobalXmlSchemaSimpleType(XmlNode node, XmlSchemaModule module, bool replacement, XmlSchemaDirective? directive)
    {
        string? nameValue = FindXmlSchemaSimpleTypeAttribute(node, "name", "");
        if (nameValue == null || !new XdmStringDatatype().IsValidXdmStringLexical(nameValue, XdmAtomicType.NcName))
        {
            throw CreateXmlSchemaSimpleTypeError(node, "A global xs:simpleType requires a valid NCName name attribute.");
        }

        XmlName name = CreateXmlSchemaTypeName(nameValue, module.EffectiveTargetNamespace, null);
        string key = CreateXmlSchemaSimpleTypeKey(name);
        CompiledXmlSchemaSimpleType? previous = TypesByName.ContainsKey(key) ? TypesByName[key] : null;
        if (!replacement && previous != null)
        {
            throw CreateXmlSchemaSimpleTypeError(node, "The simple type '" + nameValue + "' is declared more than once in namespace '" + (module.EffectiveTargetNamespace == null ? "" : module.EffectiveTargetNamespace) + "'.");
        }

        if (replacement && previous == null)
        {
            throw CreateXmlSchemaSimpleTypeError(node, "A redefined or overridden simple type must match a type in the referenced schema.");
        }

        CompiledXmlSchemaSimpleType definition = new CompiledXmlSchemaSimpleType(name, false, false, node, module);
        definition.PreviousDefinition = previous;
        AddNamedXmlSchemaSimpleType(definition, replacement, node);
        UserTypes.Add(definition);
        return definition;
    }

    internal void AddNamedXmlSchemaSimpleType(CompiledXmlSchemaSimpleType definition, bool replace, XmlNode? node)
    {
        XmlName? optionalName = definition.Name;
        if (optionalName == null)
        {
            throw CreateXmlSchemaSimpleTypeError(node, "A named simple type requires an expanded name.");
        }

        string key = CreateXmlSchemaSimpleTypeKey(optionalName);
        if (TypesByName.ContainsKey(key))
        {
            if (!replace)
            {
                throw CreateXmlSchemaSimpleTypeError(node, "The simple type is already declared.");
            }

            TypesByName[key] = definition;
        }
        else
        {
            TypesByName.Add(key, definition);
        }

        Types.Add(definition);
    }

    internal void CompileUserXmlSchemaSimpleType(CompiledXmlSchemaSimpleType definition)
    {
        if (definition.IsCompiled)
        {
            return;
        }

        XmlNode? optionalNode = definition.Node;
        if (optionalNode == null)
        {
            throw CreateXmlSchemaSimpleTypeError(null, "A user simple type requires a declaration node.");
        }

        if (definition.IsCompiling)
        {
            throw CreateXmlSchemaSimpleTypeError(optionalNode, "A circular simple type derivation was detected.");
        }

        definition.IsCompiling = true;
        definition.Final = ParseXmlSchemaSimpleTypeFinal(optionalNode);
        XmlNode varietyNode = RequireXmlSchemaSimpleTypeVariety(optionalNode);
        string varietyName = XmlSchemaSimpleNodeLocalName(varietyNode);
        if (varietyName == "restriction")
        {
            CompileRestrictedXmlSchemaSimpleType(definition, varietyNode);
        }
        else if (varietyName == "list")
        {
            CompileListXmlSchemaSimpleType(definition, varietyNode);
        }
        else
        {
            CompileUnionXmlSchemaSimpleType(definition, varietyNode);
        }

        definition.FundamentalFacets = CalculateUserXmlSchemaFundamentalFacets(definition);
        definition.IsCompiling = false;
        definition.IsCompiled = true;
    }

    internal XmlNode RequireXmlSchemaSimpleTypeVariety(XmlNode node)
    {
        XmlNode[] children = node.Children == null ? [] : node.Children;
        XmlNode? variety = null;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (IsXmlSchemaElement(child, "restriction") || IsXmlSchemaElement(child, "list") || IsXmlSchemaElement(child, "union"))
            {
                if (variety != null)
                {
                    throw CreateXmlSchemaSimpleTypeError(child, "An xs:simpleType must contain exactly one restriction, list, or union child.");
                }

                variety = child;
            }

            index = index + 1;
        }

        if (variety == null)
        {
            throw CreateXmlSchemaSimpleTypeError(node, "An xs:simpleType must contain a restriction, list, or union child.");
        }

        return variety;
    }

    internal void CompileRestrictedXmlSchemaSimpleType(CompiledXmlSchemaSimpleType definition, XmlNode restriction)
    {
        string? baseValue = FindXmlSchemaSimpleTypeAttribute(restriction, "base", "");
        XmlNode? inlineTypeNode = FindInlineXmlSchemaSimpleType(restriction);
        if (baseValue == null && inlineTypeNode == null || baseValue != null && inlineTypeNode != null)
        {
            throw CreateXmlSchemaSimpleTypeError(restriction, "An xs:restriction requires exactly one base attribute or inline simpleType.");
        }

        CompiledXmlSchemaSimpleType baseType;
        if (baseValue != null)
        {
            baseType = ResolveXmlSchemaSimpleTypeReference(baseValue, restriction, definition.Module, definition);
        }
        else
        {
            baseType = CompileAnonymousXmlSchemaSimpleType(inlineTypeNode, definition.Module);
        }

        CompileUserXmlSchemaSimpleType(baseType);
        if (baseType.Variety == XmlSchemaSimpleVariety.Universal || baseType.HasAtomicType && baseType.AtomicType == XdmAtomicType.AnyAtomicType)
        {
            throw CreateXmlSchemaSimpleTypeError(restriction, "xs:anySimpleType and xs:anyAtomicType cannot be used as restriction base types.");
        }

        EnsureXmlSchemaSimpleTypeDerivationAllowed(baseType, XmlSchemaDerivationMethod.Restriction, restriction);
        definition.Variety = baseType.Variety;
        definition.BaseType = baseType;
        definition.ItemType = baseType.ItemType;
        definition.MemberTypes = baseType.MemberTypes;
        definition.DerivationMethod = XmlSchemaDerivationMethod.Restriction;
        definition.AtomicType = baseType.AtomicType;
        definition.HasAtomicType = baseType.HasAtomicType;
        definition.DeclaredFacets = new XmlSchemaFacetCompiler(this).CompileDeclaredXmlSchemaFacets(definition, restriction, false);
        definition.EffectiveFacets = new XmlSchemaFacetCompiler(this).MergeEffectiveXmlSchemaFacets(definition, baseType.EffectiveFacets, definition.DeclaredFacets);
    }

    internal void CompileListXmlSchemaSimpleType(CompiledXmlSchemaSimpleType definition, XmlNode list)
    {
        string? itemValue = FindXmlSchemaSimpleTypeAttribute(list, "itemType", "");
        XmlNode? inlineTypeNode = FindInlineXmlSchemaSimpleType(list);
        if (itemValue == null && inlineTypeNode == null || itemValue != null && inlineTypeNode != null)
        {
            throw CreateXmlSchemaSimpleTypeError(list, "An xs:list requires exactly one itemType attribute or inline simpleType.");
        }

        CompiledXmlSchemaSimpleType itemType = itemValue == null ? CompileAnonymousXmlSchemaSimpleType(inlineTypeNode, definition.Module) : ResolveXmlSchemaSimpleTypeReference(itemValue, list, definition.Module, definition);
        CompileUserXmlSchemaSimpleType(itemType);
        EnsureXmlSchemaSimpleTypeDerivationAllowed(itemType, XmlSchemaDerivationMethod.List, list);
        if (itemType.Variety == XmlSchemaSimpleVariety.List || ContainsListXmlSchemaUnionMember(itemType))
        {
            throw CreateXmlSchemaSimpleTypeError(list, "The item type of an xs:list cannot itself be a list or a union containing a list.");
        }

        definition.Variety = XmlSchemaSimpleVariety.List;
        definition.BaseType = AnySimpleType;
        definition.ItemType = itemType;
        definition.DerivationMethod = XmlSchemaDerivationMethod.List;
        definition.DeclaredFacets = [new CompiledXmlSchemaFacet(XmlSchemaFacetKind.WhiteSpace, "collapse", true, true, list)];
        definition.EffectiveFacets = definition.DeclaredFacets;
    }

    internal void CompileUnionXmlSchemaSimpleType(CompiledXmlSchemaSimpleType definition, XmlNode union)
    {
        List<CompiledXmlSchemaSimpleType> members = new List<CompiledXmlSchemaSimpleType>();
        string? memberValues = FindXmlSchemaSimpleTypeAttribute(union, "memberTypes", "");
        if (memberValues != null)
        {
            string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(memberValues);
            string[] lexicalMembers = normalized.Length == 0 ? [] : normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int memberIndex = 0;
            while (memberIndex < lexicalMembers.Length)
            {
                CompiledXmlSchemaSimpleType member = ResolveXmlSchemaSimpleTypeReference(lexicalMembers[memberIndex], union, definition.Module, definition);
                CompileUserXmlSchemaSimpleType(member);
                EnsureXmlSchemaSimpleTypeDerivationAllowed(member, XmlSchemaDerivationMethod.Union, union);
                AddFlattenedXmlSchemaUnionMember(members, member);
                memberIndex = memberIndex + 1;
            }
        }

        XmlNode[] children = union.Children == null ? [] : union.Children;
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            XmlNode child = children[childIndex];
            if (IsXmlSchemaElement(child, "simpleType"))
            {
                CompiledXmlSchemaSimpleType member = CompileAnonymousXmlSchemaSimpleType(child, definition.Module);
                EnsureXmlSchemaSimpleTypeDerivationAllowed(member, XmlSchemaDerivationMethod.Union, union);
                AddFlattenedXmlSchemaUnionMember(members, member);
            }

            childIndex = childIndex + 1;
        }

        if (members.Count == 0)
        {
            throw CreateXmlSchemaSimpleTypeError(union, "An xs:union requires at least one member type.");
        }

        definition.Variety = XmlSchemaSimpleVariety.Union;
        definition.BaseType = AnySimpleType;
        definition.MemberTypes = members.ToArray();
        definition.DerivationMethod = XmlSchemaDerivationMethod.Union;
        definition.DeclaredFacets = [];
        definition.EffectiveFacets = definition.DeclaredFacets;
    }

    internal CompiledXmlSchemaSimpleType CompileAnonymousXmlSchemaSimpleType(XmlNode? node, XmlSchemaModule? module)
    {
        if (node == null)
        {
            throw CreateXmlSchemaSimpleTypeError(null, "An inline simple type is missing.");
        }

        if (FindXmlSchemaSimpleTypeAttribute(node, "name", "") != null)
        {
            throw CreateXmlSchemaSimpleTypeError(node, "An inline xs:simpleType cannot have a name attribute.");
        }

        CompiledXmlSchemaSimpleType definition = new CompiledXmlSchemaSimpleType(null, false, true, node, module);
        Types.Add(definition);
        UserTypes.Add(definition);
        CompileUserXmlSchemaSimpleType(definition);
        return definition;
    }

    internal XmlNode? FindInlineXmlSchemaSimpleType(XmlNode parent)
    {
        XmlNode? found = null;
        XmlNode[] children = parent.Children == null ? [] : parent.Children;
        int index = 0;
        while (index < children.Length)
        {
            if (IsXmlSchemaElement(children[index], "simpleType"))
            {
                if (found != null)
                {
                    throw CreateXmlSchemaSimpleTypeError(children[index], "Only one inline xs:simpleType is permitted here.");
                }

                found = children[index];
            }

            index = index + 1;
        }

        return found;
    }

    internal CompiledXmlSchemaSimpleType ResolveXmlSchemaSimpleTypeReference(string lexicalName, XmlNode node, XmlSchemaModule? module, CompiledXmlSchemaSimpleType current)
    {
        XPathStaticContext context = CreateXmlSchemaQNameContext(node);
        XdmNumericParseResult parsed = new XdmQNameDatatype().TryParseXdmQNameWithNamespaces(lexicalName, XdmAtomicType.QName, context.Namespaces == null ? [] : context.Namespaces, context.DefaultElementNamespace);
        if (!parsed.Success || parsed.Value == null || parsed.Value.NameValue == null)
        {
            throw CreateXmlSchemaSimpleTypeError(node, "The type reference '" + lexicalName + "' is not a resolvable QName.");
        }

        XmlName resolved = parsed.Value.NameValue;
        if (resolved.Prefix == null && (resolved.NamespaceUri == null || resolved.NamespaceUri.Length == 0) && module != null && module.IsChameleon)
        {
            resolved.NamespaceUri = module.EffectiveTargetNamespace;
        }

        string key = CreateXmlSchemaSimpleTypeKey(resolved);
        XmlName? currentName = current.Name;
        if (current.PreviousDefinition != null && currentName != null && key == CreateXmlSchemaSimpleTypeKey(currentName))
        {
            return current.PreviousDefinition;
        }

        if (!TypesByName.ContainsKey(key))
        {
            throw CreateXmlSchemaSimpleTypeError(node, "The simple type '" + lexicalName + "' is not declared.");
        }

        return TypesByName[key];
    }

    internal XPathStaticContext CreateXmlSchemaQNameContext(XmlNode node)
    {
        XmlNamespaceBinding[] bindings = new TreeNavigation().SelectXmlNamespacesOperation(node);
        string? defaultNamespace = null;
        int index = 0;
        while (index < bindings.Length)
        {
            string? optionalPrefix = bindings[index].Prefix;
            string prefix = optionalPrefix == null ? "" : optionalPrefix;
            if (prefix.Length == 0 && !bindings[index].IsUndeclaration)
            {
                defaultNamespace = bindings[index].NamespaceUri;
            }

            index = index + 1;
        }

        XPathStaticContext context = new XPathStaticContext();
        context.Namespaces = bindings;
        context.DefaultElementNamespace = defaultNamespace;
        return context;
    }

    internal void EnsureXmlSchemaSimpleTypeDerivationAllowed(CompiledXmlSchemaSimpleType baseType, XmlSchemaDerivationMethod method, XmlNode node)
    {
        int prohibited = (int)baseType.Final & (int)method;
        if (prohibited != 0)
        {
            throw CreateXmlSchemaSimpleTypeError(node, "The base simple type prohibits the requested derivation method.");
        }
    }

    internal XmlSchemaDerivationMethod ParseXmlSchemaSimpleTypeFinal(XmlNode node)
    {
        string? value = FindXmlSchemaSimpleTypeAttribute(node, "final", "");
        if (value == null)
        {
            XmlNode? ancestor = node.Parent;
            while (ancestor != null && !IsXmlSchemaElement(ancestor, "schema"))
            {
                ancestor = ancestor.Parent;
            }

            if (ancestor != null)
            {
                value = FindXmlSchemaSimpleTypeAttribute(ancestor, "finalDefault", "");
            }
        }

        if (value == null || value.Length == 0)
        {
            return XmlSchemaDerivationMethod.None;
        }

        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(value);
        if (normalized == "#all")
        {
            return XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.List | XmlSchemaDerivationMethod.Union;
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
            else if (tokens[index] == "list")
            {
                result = result | XmlSchemaDerivationMethod.List;
            }
            else if (tokens[index] == "union")
            {
                result = result | XmlSchemaDerivationMethod.Union;
            }
            else
            {
                throw CreateXmlSchemaSimpleTypeError(node, "The simple type final attribute contains an invalid derivation method.");
            }

            index = index + 1;
        }

        return result;
    }

    internal bool ContainsListXmlSchemaUnionMember(CompiledXmlSchemaSimpleType definition)
    {
        if (definition.Variety != XmlSchemaSimpleVariety.Union)
        {
            return false;
        }

        int index = 0;
        while (index < definition.MemberTypes.Length)
        {
            if (definition.MemberTypes[index].Variety == XmlSchemaSimpleVariety.List || ContainsListXmlSchemaUnionMember(definition.MemberTypes[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddFlattenedXmlSchemaUnionMember(List<CompiledXmlSchemaSimpleType> members, CompiledXmlSchemaSimpleType member)
    {
        if (member.Variety == XmlSchemaSimpleVariety.Union)
        {
            int index = 0;
            while (index < member.MemberTypes.Length)
            {
                members.Add(member.MemberTypes[index]);
                index = index + 1;
            }
        }
        else
        {
            members.Add(member);
        }
    }

    internal XmlSchemaFundamentalFacets CalculateUserXmlSchemaFundamentalFacets(CompiledXmlSchemaSimpleType definition)
    {
        if (definition.Variety == XmlSchemaSimpleVariety.Atomic && definition.BaseType != null)
        {
            XmlSchemaFundamentalFacets inherited = definition.BaseType.FundamentalFacets;
            bool bounded = HasBothXmlSchemaBoundFacets(definition.EffectiveFacets);
            bool finite = inherited.CardinalityFinite || HasXmlSchemaFacet(definition.EffectiveFacets, XmlSchemaFacetKind.Length) || HasXmlSchemaFacet(definition.EffectiveFacets, XmlSchemaFacetKind.MaxLength) || HasXmlSchemaFacet(definition.EffectiveFacets, XmlSchemaFacetKind.TotalDigits);
            if (!finite && bounded)
            {
                finite = HasXmlSchemaFacet(definition.EffectiveFacets, XmlSchemaFacetKind.FractionDigits) || HasFiniteXmlSchemaCalendarPrimitive(definition);
            }

            return new XmlSchemaFundamentalFacets(inherited.Ordered, bounded, finite, inherited.Numeric);
        }

        if (definition.Variety == XmlSchemaSimpleVariety.List)
        {
            bool fixedLength = HasXmlSchemaFacet(definition.EffectiveFacets, XmlSchemaFacetKind.Length);
            bool lengthRange = HasXmlSchemaFacet(definition.EffectiveFacets, XmlSchemaFacetKind.MinLength) && HasXmlSchemaFacet(definition.EffectiveFacets, XmlSchemaFacetKind.MaxLength);
            bool itemFinite = definition.ItemType != null && definition.ItemType.FundamentalFacets.CardinalityFinite;
            return new XmlSchemaFundamentalFacets(XmlSchemaOrderKind.None, false, (fixedLength || lengthRange) && itemFinite, false);
        }

        if (definition.Variety == XmlSchemaSimpleVariety.Union)
        {
            bool finite = true;
            bool allUnordered = true;
            bool numeric = true;
            bool bounded = true;
            int index = 0;
            while (index < definition.MemberTypes.Length)
            {
                XmlSchemaFundamentalFacets memberFacets = definition.MemberTypes[index].FundamentalFacets;
                if (!memberFacets.CardinalityFinite)
                {
                    finite = false;
                }

                if (memberFacets.Ordered != XmlSchemaOrderKind.None)
                {
                    allUnordered = false;
                }

                if (!memberFacets.Numeric)
                {
                    numeric = false;
                }

                if (!memberFacets.Bounded)
                {
                    bounded = false;
                }

                index = index + 1;
            }

            bool samePrimitive = XmlSchemaUnionMembersHaveSamePrimitive(definition);
            XmlSchemaOrderKind ordered = XmlSchemaOrderKind.Partial;
            if (samePrimitive)
            {
                ordered = XmlSchemaUnionPrimitiveOrder(definition);
            }
            else if (allUnordered)
            {
                ordered = XmlSchemaOrderKind.None;
            }

            return new XmlSchemaFundamentalFacets(ordered, samePrimitive && bounded, finite, numeric);
        }

        return new XmlSchemaFundamentalFacets(XmlSchemaOrderKind.None, false, false, false);
    }

    internal bool HasFiniteXmlSchemaCalendarPrimitive(CompiledXmlSchemaSimpleType definition)
    {
        if (!definition.HasAtomicType)
        {
            return false;
        }

        XdmAtomicTypeDefinition atomic = XdmTypes.GetXdmAtomicTypeDefinition(definition.AtomicType);
        XdmAtomicType primitive = atomic.HasPrimitiveType ? atomic.PrimitiveType : atomic.Type;
        return primitive == XdmAtomicType.Date || primitive == XdmAtomicType.GYearMonth || primitive == XdmAtomicType.GYear || primitive == XdmAtomicType.GMonthDay || primitive == XdmAtomicType.GDay || primitive == XdmAtomicType.GMonth;
    }

    internal bool XmlSchemaUnionMembersHaveSamePrimitive(CompiledXmlSchemaSimpleType definition)
    {
        XdmAtomicType primitive = XdmAtomicType.AnyAtomicType;
        int index = 0;
        while (index < definition.MemberTypes.Length)
        {
            CompiledXmlSchemaSimpleType member = definition.MemberTypes[index];
            if (member.Variety != XmlSchemaSimpleVariety.Atomic || !member.HasAtomicType)
            {
                return false;
            }

            XdmAtomicTypeDefinition atomic = XdmTypes.GetXdmAtomicTypeDefinition(member.AtomicType);
            if (!atomic.HasPrimitiveType)
            {
                return false;
            }

            if (index == 0)
            {
                primitive = atomic.PrimitiveType;
            }
            else if (primitive != atomic.PrimitiveType)
            {
                return false;
            }

            index = index + 1;
        }

        return definition.MemberTypes.Length > 0;
    }

    internal XmlSchemaOrderKind XmlSchemaUnionPrimitiveOrder(CompiledXmlSchemaSimpleType definition)
    {
        CompiledXmlSchemaSimpleType member = definition.MemberTypes[0];
        XdmAtomicTypeDefinition atomic = XdmTypes.GetXdmAtomicTypeDefinition(member.AtomicType);
        return AtomicBuiltIns[atomic.PrimitiveType].FundamentalFacets.Ordered;
    }

    internal bool HasBothXmlSchemaBoundFacets(CompiledXmlSchemaFacet[] facets)
    {
        bool minimum = HasXmlSchemaFacet(facets, XmlSchemaFacetKind.MinInclusive) || HasXmlSchemaFacet(facets, XmlSchemaFacetKind.MinExclusive);
        bool maximum = HasXmlSchemaFacet(facets, XmlSchemaFacetKind.MaxInclusive) || HasXmlSchemaFacet(facets, XmlSchemaFacetKind.MaxExclusive);
        return minimum && maximum;
    }

    internal bool HasXmlSchemaFacet(CompiledXmlSchemaFacet[] facets, XmlSchemaFacetKind kind)
    {
        int index = 0;
        while (index < facets.Length)
        {
            if (facets[index].Kind == kind)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsXmlSchemaElement(XmlNode node, string localName)
    {
        XmlName? name = node.Name;
        string nodeLocal = name == null || name.LocalName == null ? "" : name.LocalName;
        string nodeNamespace = name == null || name.NamespaceUri == null ? "" : name.NamespaceUri;
        return node.Kind == XmlNodeKind.Element && nodeLocal == localName && nodeNamespace == XmlNamespaces.XmlSchema;
    }

    internal string? FindXmlSchemaSimpleTypeAttribute(XmlNode node, string localName, string namespaceUri)
    {
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            XmlName? name = attribute.Name;
            string attributeLocal = name == null || name.LocalName == null ? "" : name.LocalName;
            string attributeNamespace = name == null || name.NamespaceUri == null ? "" : name.NamespaceUri;
            if (attribute.Kind == XmlNodeKind.Attribute && attributeLocal == localName && attributeNamespace == namespaceUri)
            {
                return attribute.Value == null ? "" : attribute.Value;
            }

            index = index + 1;
        }

        return null;
    }

    internal string XmlSchemaSimpleNodeLocalName(XmlNode node)
    {
        XmlName? name = node.Name;
        return name == null || name.LocalName == null ? "" : name.LocalName;
    }

    internal XmlName CreateXmlSchemaTypeName(string localName, string? namespaceUri, string? prefix)
    {
        XmlName name = new XmlName();
        name.Prefix = prefix;
        name.LocalName = localName;
        name.NamespaceUri = namespaceUri;
        name.QualifiedName = prefix == null || prefix.Length == 0 ? localName : prefix + ":" + localName;
        return name;
    }

    internal string CreateXmlSchemaSimpleTypeKey(XmlName name)
    {
        string namespaceUri = name.NamespaceUri == null ? "" : name.NamespaceUri;
        string localName = name.LocalName == null ? "" : name.LocalName;
        return "{" + namespaceUri + "}" + localName;
    }

    internal string XmlSchemaTypeLocalName(XdmAtomicTypeDefinition definition)
    {
        string? localName = definition.Name.LocalName;
        return localName == null ? definition.Type.ToString() : localName;
    }

    internal CompiledXmlSchemaSimpleType? FindCompiledXmlSchemaSimpleType(string namespaceUri, string localName)
    {
        XmlName name = CreateXmlSchemaTypeName(localName, namespaceUri, null);
        string key = CreateXmlSchemaSimpleTypeKey(name);
        return TypesByName.ContainsKey(key) ? TypesByName[key] : null;
    }

    internal XmlSchemaException CreateXmlSchemaSimpleTypeError(XmlNode? node, string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.SchemaFailure;
        diagnostic.Category = XmlErrorCategory.Schema;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = node == null ? null : node.Range;
        SourceLocation? location = diagnostic.Range == null ? null : diagnostic.Range.Start;
        return new XmlSchemaException(message, diagnostic, location);
    }
}
