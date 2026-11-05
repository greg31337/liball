using System;
using System.Collections.Generic;
using System.Numerics;

namespace xml;

internal class XmlSchemaFacetCompiler
{
    internal XmlSchemaSimpleTypeCompiler Compiler;

    internal XmlSchemaFacetCompiler(XmlSchemaSimpleTypeCompiler compiler)
    {
        Guard.NotNull(compiler, "compiler");
        Compiler = compiler;
    }

    internal CompiledXmlSchemaFacet[] CompileDeclaredXmlSchemaFacets(CompiledXmlSchemaSimpleType definition, XmlNode restriction, bool allowComplexContentChildren)
    {
        List<CompiledXmlSchemaFacet> facets = new List<CompiledXmlSchemaFacet>();
        XmlNode[] children = restriction.Children == null ? [] : restriction.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (Compiler.IsXmlSchemaElement(child, "annotation") || Compiler.IsXmlSchemaElement(child, "simpleType"))
            {
                index = index + 1;
                continue;
            }

            if (IsPotentialXmlSchemaFacet(child))
            {
                int facetValue = ClassifyXmlSchemaFacet(child);
                if (facetValue < 0)
                {
                    bool complexChild = Compiler.IsXmlSchemaElement(child, "attribute") || Compiler.IsXmlSchemaElement(child, "attributeGroup") || Compiler.IsXmlSchemaElement(child, "anyAttribute") || Compiler.IsXmlSchemaElement(child, "assert");
                    if (allowComplexContentChildren && complexChild)
                    {
                        index = index + 1;
                        continue;
                    }

                    throw Compiler.CreateXmlSchemaSimpleTypeError(child, "The restriction contains an unsupported facet element.");
                }

                XmlSchemaFacetKind kind = (XmlSchemaFacetKind)facetValue;
                EnsureXmlSchemaFacetApplicable(definition, kind, child);
                CompiledXmlSchemaFacet facet = ParseCompiledXmlSchemaFacet(definition, child, kind);
                EnsureXmlSchemaFacetNotRepeated(facets, facet);
                facets.Add(facet);
            }

            index = index + 1;
        }

        ValidateDeclaredXmlSchemaFacetConsistency(definition, facets);
        return facets.ToArray();
    }

    internal CompiledXmlSchemaFacet[] MergeEffectiveXmlSchemaFacets(CompiledXmlSchemaSimpleType definition, CompiledXmlSchemaFacet[] inherited, CompiledXmlSchemaFacet[] declared)
    {
        List<CompiledXmlSchemaFacet> effective = new List<CompiledXmlSchemaFacet>();
        int inheritedIndex = 0;
        while (inheritedIndex < inherited.Length)
        {
            CompiledXmlSchemaFacet inheritedFacet = inherited[inheritedIndex];
            CompiledXmlSchemaFacet? replacement = FindReplacingXmlSchemaFacet(declared, inheritedFacet.Kind);
            if (replacement == null)
            {
                effective.Add(inheritedFacet);
            }
            else if (inheritedFacet.Fixed && inheritedFacet.Value != replacement.Value)
            {
                throw Compiler.CreateXmlSchemaSimpleTypeError(replacement.Node, "A fixed inherited facet cannot be changed by a derived simple type.");
            }

            inheritedIndex = inheritedIndex + 1;
        }

        int declaredIndex = 0;
        while (declaredIndex < declared.Length)
        {
            effective.Add(declared[declaredIndex]);
            declaredIndex = declaredIndex + 1;
        }

        ValidateEffectiveXmlSchemaFacetConsistency(definition, effective);
        return effective.ToArray();
    }

    internal CompiledXmlSchemaFacet ParseCompiledXmlSchemaFacet(CompiledXmlSchemaSimpleType definition, XmlNode node, XmlSchemaFacetKind kind)
    {
        string attributeName = kind == XmlSchemaFacetKind.Assertion ? "test" : "value";
        string? optionalValue = Compiler.FindXmlSchemaSimpleTypeAttribute(node, attributeName, "");
        if (optionalValue == null)
        {
            throw Compiler.CreateXmlSchemaSimpleTypeError(node, "The xs:" + XmlSchemaFacetLocalName(kind) + " facet requires a " + attributeName + " attribute.");
        }

        string value = optionalValue;
        if (kind == XmlSchemaFacetKind.Assertion && new XdmStringDatatype().CollapseXdmStringWhitespace(value).Length == 0)
        {
            throw Compiler.CreateXmlSchemaSimpleTypeError(node, "An assertion facet requires a non-empty test expression.");
        }

        if (kind == XmlSchemaFacetKind.Pattern)
        {
            XmlSchemaRegexCompilationResult compilation = new XmlSchemaRegexCompiler().CompileXmlSchemaRegex(value, "", false);
            if (!compilation.Success)
            {
                string message = compilation.ErrorMessage == null ? "The pattern facet is not a valid XML Schema regular expression." : compilation.ErrorMessage;
                throw Compiler.CreateXmlSchemaSimpleTypeError(node, message);
            }
        }

        bool fixedValue = ParseXmlSchemaFacetFixed(node, kind);
        if (kind == XmlSchemaFacetKind.Length || kind == XmlSchemaFacetKind.MinLength || kind == XmlSchemaFacetKind.MaxLength || kind == XmlSchemaFacetKind.FractionDigits)
        {
            value = NormalizeXmlSchemaFacetInteger(node, value, false);
        }
        else if (kind == XmlSchemaFacetKind.TotalDigits)
        {
            value = NormalizeXmlSchemaFacetInteger(node, value, true);
        }
        else if (kind == XmlSchemaFacetKind.WhiteSpace)
        {
            if (value != "preserve" && value != "replace" && value != "collapse")
            {
                throw Compiler.CreateXmlSchemaSimpleTypeError(node, "A whiteSpace facet must be preserve, replace, or collapse.");
            }
        }
        else if (kind == XmlSchemaFacetKind.ExplicitTimezone)
        {
            if (value != "optional" && value != "required" && value != "prohibited")
            {
                throw Compiler.CreateXmlSchemaSimpleTypeError(node, "An explicitTimezone facet must be optional, required, or prohibited.");
            }
        }
        else if (kind == XmlSchemaFacetKind.MaxScale || kind == XmlSchemaFacetKind.MinScale)
        {
            value = NormalizeXmlSchemaFacetScale(node, value);
        }
        else if (IsXmlSchemaValueFacet(kind))
        {
            value = NormalizeXmlSchemaFacetValue(definition, node, value);
        }

        return new CompiledXmlSchemaFacet(kind, value, fixedValue, false, node);
    }

    internal bool ParseXmlSchemaFacetFixed(XmlNode node, XmlSchemaFacetKind kind)
    {
        string? fixedValue = Compiler.FindXmlSchemaSimpleTypeAttribute(node, "fixed", "");
        if (fixedValue == null)
        {
            return false;
        }

        if (kind == XmlSchemaFacetKind.Pattern || kind == XmlSchemaFacetKind.Enumeration || kind == XmlSchemaFacetKind.Assertion)
        {
            throw Compiler.CreateXmlSchemaSimpleTypeError(node, "This facet does not permit a fixed attribute.");
        }

        XdmNumericParseResult parsed = new XdmNumericDatatype().TryParseXdmBoolean(fixedValue);
        if (!parsed.Success || parsed.Value == null)
        {
            throw Compiler.CreateXmlSchemaSimpleTypeError(node, "A facet fixed attribute must be a valid xs:boolean value.");
        }

        return parsed.Value.BooleanValue;
    }

    internal string NormalizeXmlSchemaFacetInteger(XmlNode node, string lexical, bool positive)
    {
        XdmAtomicType type = positive ? XdmAtomicType.PositiveInteger : XdmAtomicType.NonNegativeInteger;
        XdmNumericParseResult parsed = new XdmNumericDatatype().TryParseXdmInteger(lexical, type);
        if (!parsed.Success || parsed.Value == null || parsed.Value.CanonicalValue == null)
        {
            throw Compiler.CreateXmlSchemaSimpleTypeError(node, "The facet value must be a valid " + (positive ? "positive" : "non-negative") + " integer.");
        }

        return parsed.Value.CanonicalValue;
    }

    internal string NormalizeXmlSchemaFacetScale(XmlNode node, string lexical)
    {
        XdmNumericParseResult parsed = new XdmNumericDatatype().TryParseXdmInteger(lexical, XdmAtomicType.Integer);
        if (!parsed.Success || parsed.Value == null || parsed.Value.CanonicalValue == null)
        {
            throw Compiler.CreateXmlSchemaSimpleTypeError(node, "A scale facet requires a valid integer value.");
        }

        return parsed.Value.CanonicalValue;
    }

    internal string NormalizeXmlSchemaFacetValue(CompiledXmlSchemaSimpleType definition, XmlNode node, string lexical)
    {
        if (!definition.HasAtomicType || definition.AtomicType == XdmAtomicType.AnyAtomicType || definition.AtomicType == XdmAtomicType.Notation)
        {
            return lexical;
        }

        XPathStaticContext context = Compiler.CreateXmlSchemaQNameContext(node);
        XdmConversionResult parsed = new XdmTypeConverter().TryParseXdmAtomicLexical(lexical, definition.AtomicType, context);
        if (!parsed.Success || parsed.Value == null)
        {
            throw Compiler.CreateXmlSchemaSimpleTypeError(node, "The facet value is not valid in the base type's value space.");
        }

        string? canonical = parsed.Value.CanonicalValue;
        return canonical == null ? lexical : canonical;
    }

    internal void EnsureXmlSchemaFacetApplicable(CompiledXmlSchemaSimpleType definition, XmlSchemaFacetKind kind, XmlNode node)
    {
        if (kind == XmlSchemaFacetKind.Pattern || kind == XmlSchemaFacetKind.Enumeration || kind == XmlSchemaFacetKind.Assertion)
        {
            return;
        }

        if (kind == XmlSchemaFacetKind.WhiteSpace)
        {
            if (definition.Variety == XmlSchemaSimpleVariety.Atomic || definition.Variety == XmlSchemaSimpleVariety.List)
            {
                return;
            }

            throw Compiler.CreateXmlSchemaSimpleTypeError(node, "A whiteSpace facet is not applicable to a union type.");
        }

        if (kind == XmlSchemaFacetKind.Length || kind == XmlSchemaFacetKind.MinLength || kind == XmlSchemaFacetKind.MaxLength)
        {
            bool list = definition.Variety == XmlSchemaSimpleVariety.List;
            bool atomicLength = definition.HasAtomicType && (Compiler.XdmTypes.IsXdmStringDerivedAtomicType(definition.AtomicType) || definition.AtomicType == XdmAtomicType.HexBinary || definition.AtomicType == XdmAtomicType.Base64Binary || definition.AtomicType == XdmAtomicType.AnyUri);
            if (list || atomicLength)
            {
                return;
            }

            throw Compiler.CreateXmlSchemaSimpleTypeError(node, "This length facet is not applicable to the restriction base type.");
        }

        if (kind == XmlSchemaFacetKind.TotalDigits || kind == XmlSchemaFacetKind.FractionDigits || kind == XmlSchemaFacetKind.MaxScale || kind == XmlSchemaFacetKind.MinScale)
        {
            if (definition.HasAtomicType && Compiler.XdmTypes.IsXdmAtomicSubtypeOf(definition.AtomicType, XdmAtomicType.Decimal))
            {
                return;
            }

            throw Compiler.CreateXmlSchemaSimpleTypeError(node, "This digit or scale facet requires a decimal-derived base type.");
        }

        if (kind == XmlSchemaFacetKind.ExplicitTimezone)
        {
            if (definition.HasAtomicType && definition.AtomicType >= XdmAtomicType.DateTime && definition.AtomicType <= XdmAtomicType.GDay)
            {
                return;
            }

            throw Compiler.CreateXmlSchemaSimpleTypeError(node, "An explicitTimezone facet requires a date or time base type.");
        }

        if (IsXmlSchemaBoundFacet(kind))
        {
            bool numeric = definition.HasAtomicType && Compiler.XdmTypes.IsXdmNumericAtomicType(definition.AtomicType);
            bool temporal = definition.HasAtomicType && (Compiler.XdmTypes.IsXdmDurationDerivedAtomicType(definition.AtomicType) || definition.AtomicType >= XdmAtomicType.DateTime && definition.AtomicType <= XdmAtomicType.GDay);
            if (numeric || temporal)
            {
                return;
            }

            throw Compiler.CreateXmlSchemaSimpleTypeError(node, "A bound facet requires an ordered numeric, duration, date, or time base type.");
        }
    }

    internal void EnsureXmlSchemaFacetNotRepeated(List<CompiledXmlSchemaFacet> facets, CompiledXmlSchemaFacet candidate)
    {
        if (candidate.Kind == XmlSchemaFacetKind.Pattern || candidate.Kind == XmlSchemaFacetKind.Enumeration || candidate.Kind == XmlSchemaFacetKind.Assertion)
        {
            return;
        }

        int index = 0;
        while (index < facets.Count)
        {
            if (facets[index].Kind == candidate.Kind)
            {
                throw Compiler.CreateXmlSchemaSimpleTypeError(candidate.Node, "A single restriction cannot repeat this facet.");
            }

            index = index + 1;
        }
    }

    internal void ValidateDeclaredXmlSchemaFacetConsistency(CompiledXmlSchemaSimpleType definition, List<CompiledXmlSchemaFacet> facets)
    {
        CompiledXmlSchemaFacet[] values = facets.ToArray();
        if (FindXmlSchemaFacet(values, XmlSchemaFacetKind.MinInclusive) != null && FindXmlSchemaFacet(values, XmlSchemaFacetKind.MinExclusive) != null)
        {
            throw Compiler.CreateXmlSchemaSimpleTypeError(definition.Node, "A restriction cannot specify both minInclusive and minExclusive.");
        }

        if (FindXmlSchemaFacet(values, XmlSchemaFacetKind.MaxInclusive) != null && FindXmlSchemaFacet(values, XmlSchemaFacetKind.MaxExclusive) != null)
        {
            throw Compiler.CreateXmlSchemaSimpleTypeError(definition.Node, "A restriction cannot specify both maxInclusive and maxExclusive.");
        }
    }

    internal void ValidateEffectiveXmlSchemaFacetConsistency(CompiledXmlSchemaSimpleType definition, List<CompiledXmlSchemaFacet> facets)
    {
        CompiledXmlSchemaFacet[] values = facets.ToArray();
        CompiledXmlSchemaFacet? length = FindXmlSchemaFacet(values, XmlSchemaFacetKind.Length);
        CompiledXmlSchemaFacet? minimumLength = FindXmlSchemaFacet(values, XmlSchemaFacetKind.MinLength);
        CompiledXmlSchemaFacet? maximumLength = FindXmlSchemaFacet(values, XmlSchemaFacetKind.MaxLength);
        if (length != null && minimumLength != null && ParseXmlSchemaFacetBigInteger(length) < ParseXmlSchemaFacetBigInteger(minimumLength) || length != null && maximumLength != null && ParseXmlSchemaFacetBigInteger(length) > ParseXmlSchemaFacetBigInteger(maximumLength) || minimumLength != null && maximumLength != null && ParseXmlSchemaFacetBigInteger(minimumLength) > ParseXmlSchemaFacetBigInteger(maximumLength))
        {
            throw Compiler.CreateXmlSchemaSimpleTypeError(definition.Node, "The effective length facets are contradictory.");
        }

        CompiledXmlSchemaFacet? totalDigits = FindXmlSchemaFacet(values, XmlSchemaFacetKind.TotalDigits);
        CompiledXmlSchemaFacet? fractionDigits = FindXmlSchemaFacet(values, XmlSchemaFacetKind.FractionDigits);
        if (totalDigits != null && fractionDigits != null && ParseXmlSchemaFacetBigInteger(fractionDigits) > ParseXmlSchemaFacetBigInteger(totalDigits))
        {
            throw Compiler.CreateXmlSchemaSimpleTypeError(definition.Node, "fractionDigits cannot exceed totalDigits.");
        }

        ValidateXmlSchemaWhitespaceNarrowing(definition, values);
        ValidateNumericXmlSchemaBounds(definition, values);
    }

    internal void ValidateXmlSchemaWhitespaceNarrowing(CompiledXmlSchemaSimpleType definition, CompiledXmlSchemaFacet[] facets)
    {
        CompiledXmlSchemaFacet? declared = FindXmlSchemaFacet(definition.DeclaredFacets, XmlSchemaFacetKind.WhiteSpace);
        CompiledXmlSchemaSimpleType? baseType = definition.BaseType;
        CompiledXmlSchemaFacet? inherited = baseType == null ? null : FindXmlSchemaFacet(baseType.EffectiveFacets, XmlSchemaFacetKind.WhiteSpace);
        if (declared == null || inherited == null)
        {
            return;
        }

        int declaredStrength = XmlSchemaWhitespaceStrength(declared.Value);
        int inheritedStrength = XmlSchemaWhitespaceStrength(inherited.Value);
        if (declaredStrength < inheritedStrength)
        {
            throw Compiler.CreateXmlSchemaSimpleTypeError(declared.Node, "A derived whiteSpace facet cannot weaken the base type's whitespace handling.");
        }
    }

    internal void ValidateNumericXmlSchemaBounds(CompiledXmlSchemaSimpleType definition, CompiledXmlSchemaFacet[] facets)
    {
        if (!definition.HasAtomicType || !Compiler.XdmTypes.IsXdmNumericAtomicType(definition.AtomicType))
        {
            return;
        }

        CompiledXmlSchemaFacet? minimum = FindXmlSchemaFacet(facets, XmlSchemaFacetKind.MinInclusive);
        if (minimum == null)
        {
            minimum = FindXmlSchemaFacet(facets, XmlSchemaFacetKind.MinExclusive);
        }

        CompiledXmlSchemaFacet? maximum = FindXmlSchemaFacet(facets, XmlSchemaFacetKind.MaxInclusive);
        if (maximum == null)
        {
            maximum = FindXmlSchemaFacet(facets, XmlSchemaFacetKind.MaxExclusive);
        }

        if (minimum == null || maximum == null)
        {
            return;
        }

        XmlNode? optionalContextNode = definition.Node == null ? minimum.Node : definition.Node;
        if (optionalContextNode == null)
        {
            return;
        }

        XPathStaticContext context = Compiler.CreateXmlSchemaQNameContext(optionalContextNode);
        XdmConversionResult minimumValue = new XdmTypeConverter().TryParseXdmAtomicLexical(minimum.Value, definition.AtomicType, context);
        XdmConversionResult maximumValue = new XdmTypeConverter().TryParseXdmAtomicLexical(maximum.Value, definition.AtomicType, context);
        if (minimumValue.Success && maximumValue.Success && minimumValue.Value != null && maximumValue.Value != null && new XdmNumericDatatype().CompareXdmNumeric(minimumValue.Value, maximumValue.Value) > 0)
        {
            throw Compiler.CreateXmlSchemaSimpleTypeError(definition.Node, "The effective minimum bound exceeds the maximum bound.");
        }
    }

    internal CompiledXmlSchemaFacet? FindReplacingXmlSchemaFacet(CompiledXmlSchemaFacet[] facets, XmlSchemaFacetKind inheritedKind)
    {
        if (inheritedKind == XmlSchemaFacetKind.Pattern || inheritedKind == XmlSchemaFacetKind.Assertion)
        {
            return null;
        }

        int index = 0;
        while (index < facets.Length)
        {
            XmlSchemaFacetKind candidate = facets[index].Kind;
            bool same = candidate == inheritedKind;
            bool minimumPair = (candidate == XmlSchemaFacetKind.MinInclusive || candidate == XmlSchemaFacetKind.MinExclusive) && (inheritedKind == XmlSchemaFacetKind.MinInclusive || inheritedKind == XmlSchemaFacetKind.MinExclusive);
            bool maximumPair = (candidate == XmlSchemaFacetKind.MaxInclusive || candidate == XmlSchemaFacetKind.MaxExclusive) && (inheritedKind == XmlSchemaFacetKind.MaxInclusive || inheritedKind == XmlSchemaFacetKind.MaxExclusive);
            if (same || minimumPair || maximumPair)
            {
                return facets[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal CompiledXmlSchemaFacet? FindXmlSchemaFacet(CompiledXmlSchemaFacet[] facets, XmlSchemaFacetKind kind)
    {
        int index = facets.Length - 1;
        while (index >= 0)
        {
            if (facets[index].Kind == kind)
            {
                return facets[index];
            }

            index = index - 1;
        }

        return null;
    }

    internal BigInteger ParseXmlSchemaFacetBigInteger(CompiledXmlSchemaFacet facet)
    {
        return BigInteger.Parse(facet.Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    internal int XmlSchemaWhitespaceStrength(string value)
    {
        if (value == "preserve")
        {
            return 0;
        }

        if (value == "replace")
        {
            return 1;
        }

        return 2;
    }

    internal bool IsPotentialXmlSchemaFacet(XmlNode node)
    {
        XmlName? name = node.Name;
        string namespaceUri = name == null || name.NamespaceUri == null ? "" : name.NamespaceUri;
        return node.Kind == XmlNodeKind.Element && namespaceUri == XmlNamespaces.XmlSchema;
    }

    internal int ClassifyXmlSchemaFacet(XmlNode node)
    {
        string localName = Compiler.XmlSchemaSimpleNodeLocalName(node);
        if (localName == "length") return (int)XmlSchemaFacetKind.Length;
        if (localName == "minLength") return (int)XmlSchemaFacetKind.MinLength;
        if (localName == "maxLength") return (int)XmlSchemaFacetKind.MaxLength;
        if (localName == "pattern") return (int)XmlSchemaFacetKind.Pattern;
        if (localName == "enumeration") return (int)XmlSchemaFacetKind.Enumeration;
        if (localName == "whiteSpace") return (int)XmlSchemaFacetKind.WhiteSpace;
        if (localName == "maxInclusive") return (int)XmlSchemaFacetKind.MaxInclusive;
        if (localName == "maxExclusive") return (int)XmlSchemaFacetKind.MaxExclusive;
        if (localName == "minExclusive") return (int)XmlSchemaFacetKind.MinExclusive;
        if (localName == "minInclusive") return (int)XmlSchemaFacetKind.MinInclusive;
        if (localName == "totalDigits") return (int)XmlSchemaFacetKind.TotalDigits;
        if (localName == "fractionDigits") return (int)XmlSchemaFacetKind.FractionDigits;
        if (localName == "assertion") return (int)XmlSchemaFacetKind.Assertion;
        if (localName == "explicitTimezone") return (int)XmlSchemaFacetKind.ExplicitTimezone;
        if (localName == "maxScale") return (int)XmlSchemaFacetKind.MaxScale;
        if (localName == "minScale") return (int)XmlSchemaFacetKind.MinScale;
        return -1;
    }

    internal bool IsXmlSchemaBoundFacet(XmlSchemaFacetKind kind)
    {
        return kind == XmlSchemaFacetKind.MaxInclusive || kind == XmlSchemaFacetKind.MaxExclusive || kind == XmlSchemaFacetKind.MinInclusive || kind == XmlSchemaFacetKind.MinExclusive;
    }

    internal bool IsXmlSchemaValueFacet(XmlSchemaFacetKind kind)
    {
        return kind == XmlSchemaFacetKind.Enumeration || IsXmlSchemaBoundFacet(kind);
    }

    internal string XmlSchemaFacetLocalName(XmlSchemaFacetKind kind)
    {
        return kind.ToString();
    }
}
