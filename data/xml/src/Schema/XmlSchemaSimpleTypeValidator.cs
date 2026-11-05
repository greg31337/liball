using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace xml;

internal class XmlSchemaSimpleTypeValidator
{
    internal XdmTypeSystem Types;

    internal XmlSchemaSimpleTypeValidator()
    {
        Types = new XdmTypeSystem();
    }

    internal XmlSchemaSimpleTypeValidationResult ValidateXmlSchemaSimpleType(CompiledXmlSchemaSimpleType type, string lexicalValue, XmlNode? contextNode)
    {
        Guard.NotNull(type, "type");
        Guard.NotNull(lexicalValue, "lexicalValue");
        XmlSchemaSimpleTypeValidationResult result = ParseXmlSchemaSimpleValue(type, lexicalValue, contextNode, true);
        if (!result.Success)
        {
            return result;
        }

        ApplyXmlSchemaValueFacets(type, result, contextNode);
        return result;
    }

    internal XmlSchemaSimpleTypeValidationResult ParseXmlSchemaSimpleValue(CompiledXmlSchemaSimpleType type, string lexicalValue, XmlNode? contextNode, bool validateMembers)
    {
        if (type.Variety == XmlSchemaSimpleVariety.List)
        {
            return ParseXmlSchemaListValue(type, lexicalValue, contextNode, validateMembers);
        }

        if (type.Variety == XmlSchemaSimpleVariety.Union)
        {
            return ParseXmlSchemaUnionValue(type, lexicalValue, contextNode, validateMembers);
        }

        return ParseXmlSchemaAtomicValue(type, lexicalValue, contextNode);
    }

    internal XmlSchemaSimpleTypeValidationResult ParseXmlSchemaAtomicValue(CompiledXmlSchemaSimpleType type, string lexicalValue, XmlNode? contextNode)
    {
        XmlSchemaSimpleTypeValidationResult result = new XmlSchemaSimpleTypeValidationResult();
        string normalized = NormalizeXmlSchemaWhitespace(type, lexicalValue);
        result.NormalizedValue = normalized;
        if (!type.HasAtomicType || type.AtomicType == XdmAtomicType.AnyAtomicType)
        {
            result.Success = type.Variety == XmlSchemaSimpleVariety.Universal;
            result.CanonicalValue = normalized;
            return result;
        }

        XdmConversionResult parsed;
        XPathStaticContext context = CreateXmlSchemaValueContext(contextNode);
        if (type.AtomicType == XdmAtomicType.Notation)
        {
            XdmNumericParseResult notation = new XdmQNameDatatype().TryParseXdmQNameWithNamespaces(normalized, XdmAtomicType.Notation, context.Namespaces == null ? [] : context.Namespaces, context.DefaultElementNamespace);
            parsed = new XdmConversionResult(notation.Success, false, notation.Value, notation.ErrorCode, notation.ErrorMessage);
        }
        else
        {
            parsed = new XdmTypeConverter().TryParseXdmAtomicLexical(normalized, type.AtomicType, context);
        }

        if (!parsed.Success || parsed.Value == null)
        {
            return result;
        }

        result.Success = true;
        result.AtomicValue = parsed.Value;
        result.AtomicValues = [parsed.Value];
        result.CanonicalValue = parsed.Value.CanonicalValue == null ? parsed.Value.StringValue : parsed.Value.CanonicalValue;
        if (result.CanonicalValue == null)
        {
            result.CanonicalValue = normalized;
        }

        return result;
    }

    internal XmlSchemaSimpleTypeValidationResult ParseXmlSchemaListValue(CompiledXmlSchemaSimpleType type, string lexicalValue, XmlNode? contextNode, bool validateMembers)
    {
        XmlSchemaSimpleTypeValidationResult result = new XmlSchemaSimpleTypeValidationResult();
        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(lexicalValue);
        result.NormalizedValue = normalized;
        string[] tokens = normalized.Length == 0 ? [] : normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        CompiledXmlSchemaSimpleType? itemType = type.ItemType;
        if (itemType == null)
        {
            return result;
        }

        List<XdmAtomicValue> values = new List<XdmAtomicValue>();
        StringBuilder canonical = new StringBuilder();
        int index = 0;
        while (index < tokens.Length)
        {
            XmlSchemaSimpleTypeValidationResult item = validateMembers ? ValidateXmlSchemaSimpleType(itemType, tokens[index], contextNode) : ParseXmlSchemaSimpleValue(itemType, tokens[index], contextNode, false);
            if (!item.Success || item.AtomicValues.Length == 0)
            {
                return result;
            }

            int valueIndex = 0;
            while (valueIndex < item.AtomicValues.Length)
            {
                values.Add(item.AtomicValues[valueIndex]);
                valueIndex = valueIndex + 1;
            }

            if (canonical.Length != 0)
            {
                canonical.Append(' ');
            }

            canonical.Append(item.CanonicalValue == null ? item.NormalizedValue : item.CanonicalValue);
            index = index + 1;
        }

        result.Success = true;
        result.AtomicValues = values.ToArray();
        result.CanonicalValue = canonical.ToString();
        return result;
    }

    internal XmlSchemaSimpleTypeValidationResult ParseXmlSchemaUnionValue(CompiledXmlSchemaSimpleType type, string lexicalValue, XmlNode? contextNode, bool validateMembers)
    {
        XmlSchemaSimpleTypeValidationResult failure = new XmlSchemaSimpleTypeValidationResult();
        failure.NormalizedValue = new XdmStringDatatype().CollapseXdmStringWhitespace(lexicalValue);
        int index = 0;
        while (index < type.MemberTypes.Length)
        {
            CompiledXmlSchemaSimpleType memberType = type.MemberTypes[index];
            XmlSchemaSimpleTypeValidationResult member = validateMembers ? ValidateXmlSchemaSimpleType(memberType, lexicalValue, contextNode) : ParseXmlSchemaSimpleValue(memberType, lexicalValue, contextNode, false);
            if (member.Success)
            {
                member.MemberType = memberType;
                return member;
            }

            index = index + 1;
        }

        return failure;
    }

    internal void ApplyXmlSchemaValueFacets(CompiledXmlSchemaSimpleType type, XmlSchemaSimpleTypeValidationResult result, XmlNode? contextNode)
    {
        CompiledXmlSchemaFacet[] facets = type.EffectiveFacets;
        int lengthIndex = 0;
        while (lengthIndex < facets.Length)
        {
            CompiledXmlSchemaFacet lengthFacet = facets[lengthIndex];
            if ((lengthFacet.Kind == XmlSchemaFacetKind.Length || lengthFacet.Kind == XmlSchemaFacetKind.MinLength || lengthFacet.Kind == XmlSchemaFacetKind.MaxLength) && !ValidateXmlSchemaLengthFacet(type, lengthFacet, result))
            {
                result.Success = false;
                result.FailedFacet = lengthFacet;
                return;
            }

            lengthIndex = lengthIndex + 1;
        }

        if (!ValidateXmlSchemaPatternFacets(type, facets, result.NormalizedValue))
        {
            result.Success = false;
            result.FailedFacet = FindFirstXmlSchemaFacet(facets, XmlSchemaFacetKind.Pattern);
            return;
        }

        if (!ValidateXmlSchemaEnumerationFacets(type, facets, result, contextNode))
        {
            result.Success = false;
            result.FailedFacet = FindFirstXmlSchemaFacet(facets, XmlSchemaFacetKind.Enumeration);
            return;
        }

        int index = 0;
        while (index < facets.Length && result.Success)
        {
            CompiledXmlSchemaFacet facet = facets[index];
            bool valid = true;
            if (facet.Kind == XmlSchemaFacetKind.MinInclusive || facet.Kind == XmlSchemaFacetKind.MinExclusive || facet.Kind == XmlSchemaFacetKind.MaxInclusive || facet.Kind == XmlSchemaFacetKind.MaxExclusive)
            {
                valid = ValidateXmlSchemaBoundFacet(type, facet, result, contextNode);
            }
            else if (facet.Kind == XmlSchemaFacetKind.TotalDigits || facet.Kind == XmlSchemaFacetKind.FractionDigits || facet.Kind == XmlSchemaFacetKind.MaxScale || facet.Kind == XmlSchemaFacetKind.MinScale)
            {
                valid = ValidateXmlSchemaDigitFacet(facet, result);
            }
            else if (facet.Kind == XmlSchemaFacetKind.ExplicitTimezone)
            {
                valid = ValidateXmlSchemaTimezoneFacet(facet, result);
            }
            else if (facet.Kind == XmlSchemaFacetKind.Assertion)
            {
                valid = new XmlSchemaAssertionEvaluator(this).EvaluateXmlSchemaAssertion(facet.Value, result);
            }

            if (!valid)
            {
                result.Success = false;
                result.FailedFacet = facet;
            }

            index = index + 1;
        }
    }

    internal bool ValidateXmlSchemaLengthFacet(CompiledXmlSchemaSimpleType type, CompiledXmlSchemaFacet facet, XmlSchemaSimpleTypeValidationResult result)
    {
        BigInteger length = new BigInteger(MeasureXmlSchemaValueLength(type, result));
        BigInteger limit = BigInteger.Parse(facet.Value, CultureInfo.InvariantCulture);
        if (facet.Kind == XmlSchemaFacetKind.Length)
        {
            return length == limit;
        }

        if (facet.Kind == XmlSchemaFacetKind.MinLength)
        {
            return length >= limit;
        }

        return length <= limit;
    }

    internal int MeasureXmlSchemaValueLength(CompiledXmlSchemaSimpleType type, XmlSchemaSimpleTypeValidationResult result)
    {
        if (type.Variety == XmlSchemaSimpleVariety.List)
        {
            return result.AtomicValues.Length;
        }

        XdmAtomicValue? value = result.AtomicValue;
        if (value != null && (value.Type == XdmAtomicType.HexBinary || value.Type == XdmAtomicType.Base64Binary))
        {
            return value.BinaryValue == null ? 0 : value.BinaryValue.Length;
        }

        return CountXmlSchemaCodepoints(result.NormalizedValue);
    }

    internal int CountXmlSchemaCodepoints(string value)
    {
        int count = 0;
        int index = 0;
        while (index < value.Length)
        {
            if (char.IsHighSurrogate(value[index]) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
            {
                index = index + 2;
            }
            else
            {
                index = index + 1;
            }

            count = count + 1;
        }

        return count;
    }

    internal bool ValidateXmlSchemaPatternFacets(CompiledXmlSchemaSimpleType type, CompiledXmlSchemaFacet[] facets, string value)
    {
        bool[] processed = new bool[facets.Length];
        int index = 0;
        while (index < facets.Length)
        {
            CompiledXmlSchemaFacet facet = facets[index];
            if (facet.Kind == XmlSchemaFacetKind.Pattern && !processed[index])
            {
                bool matched = IsXmlSchemaPatternMatch(type, value, facet.Value);
                processed[index] = true;
                XmlNode? parent = facet.Node == null ? null : facet.Node.Parent;
                if (parent != null)
                {
                    int alternativeIndex = index + 1;
                    while (alternativeIndex < facets.Length)
                    {
                        CompiledXmlSchemaFacet alternative = facets[alternativeIndex];
                        if (alternative.Kind == XmlSchemaFacetKind.Pattern && alternative.Node != null && alternative.Node.Parent == parent)
                        {
                            processed[alternativeIndex] = true;
                            matched = matched || IsXmlSchemaPatternMatch(type, value, alternative.Value);
                        }

                        alternativeIndex = alternativeIndex + 1;
                    }
                }

                if (!matched)
                {
                    return false;
                }
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsXmlSchemaPatternMatch(CompiledXmlSchemaSimpleType type, string value, string pattern)
    {
        XmlSchemaRegexCompilationResult compilation = new XmlSchemaRegexCompiler().CompileXmlSchemaRegex(pattern, "", false);
        if (!compilation.Success || compilation.Program == null)
        {
            return false;
        }

        return new XmlSchemaRegexMatcher().IsEntireXmlSchemaRegexMatch(compilation.Program, value);
    }

    internal bool ValidateXmlSchemaEnumerationFacets(CompiledXmlSchemaSimpleType type, CompiledXmlSchemaFacet[] facets, XmlSchemaSimpleTypeValidationResult result, XmlNode? contextNode)
    {
        bool found = false;
        int index = 0;
        while (index < facets.Length)
        {
            CompiledXmlSchemaFacet facet = facets[index];
            if (facet.Kind == XmlSchemaFacetKind.Enumeration)
            {
                found = true;
                XmlNode? facetNode = facet.Node == null ? contextNode : facet.Node;
                XmlSchemaSimpleTypeValidationResult candidate = ParseXmlSchemaSimpleValue(type, facet.Value, facetNode, false);
                if (candidate.Success && EqualXmlSchemaSimpleValues(result, candidate))
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return !found;
    }

    internal bool EqualXmlSchemaSimpleValues(XmlSchemaSimpleTypeValidationResult left, XmlSchemaSimpleTypeValidationResult right)
    {
        if (left.AtomicValues.Length != right.AtomicValues.Length)
        {
            return false;
        }

        if (left.AtomicValues.Length == 0)
        {
            return string.Equals(left.CanonicalValue, right.CanonicalValue, StringComparison.Ordinal);
        }

        int index = 0;
        while (index < left.AtomicValues.Length)
        {
            if (!EqualXmlSchemaAtomicValues(left.AtomicValues[index], right.AtomicValues[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool EqualXmlSchemaAtomicValues(XdmAtomicValue left, XdmAtomicValue right)
    {
        if (Types.IsXdmNumericAtomicType(left.Type) && Types.IsXdmNumericAtomicType(right.Type))
        {
            if ((left.Type == XdmAtomicType.Float || left.Type == XdmAtomicType.Double) && double.IsNaN(left.FloatingValue) && (right.Type == XdmAtomicType.Float || right.Type == XdmAtomicType.Double) && double.IsNaN(right.FloatingValue))
            {
                return true;
            }

            return new XdmNumericDatatype().CompareXdmNumeric(left, right) == 0;
        }

        if (left.Type == XdmAtomicType.Boolean && right.Type == XdmAtomicType.Boolean)
        {
            return left.BooleanValue == right.BooleanValue;
        }

        if (Types.IsXdmStringDerivedAtomicType(left.Type) && Types.IsXdmStringDerivedAtomicType(right.Type))
        {
            return string.Equals(left.StringValue, right.StringValue, StringComparison.Ordinal);
        }

        if (left.Type == XdmAtomicType.AnyUri && right.Type == XdmAtomicType.AnyUri)
        {
            return new XdmAnyUriDatatype().EqualXdmAnyUri(left, right);
        }

        if (Types.IsXdmDurationDerivedAtomicType(left.Type) && Types.IsXdmDurationDerivedAtomicType(right.Type))
        {
            return new XdmDurationDatatype().EqualXdmDuration(left, right);
        }

        if (left.Type >= XdmAtomicType.DateTime && left.Type <= XdmAtomicType.GDay && right.Type == left.Type)
        {
            return new XdmDateTimeDatatype().EqualXdmDateTime(left, right, 0);
        }

        if ((left.Type == XdmAtomicType.HexBinary || left.Type == XdmAtomicType.Base64Binary) && (right.Type == XdmAtomicType.HexBinary || right.Type == XdmAtomicType.Base64Binary))
        {
            return new XdmBinaryDatatype().EqualXdmBinary(left, right);
        }

        if (left.Type == XdmAtomicType.QName && right.Type == XdmAtomicType.QName)
        {
            return new XdmQNameDatatype().EqualXdmQName(left, right);
        }

        if (left.Type == XdmAtomicType.Notation && right.Type == XdmAtomicType.Notation)
        {
            return new XdmQNameDatatype().EqualXdmNotation(left, right);
        }

        return string.Equals(left.CanonicalValue, right.CanonicalValue, StringComparison.Ordinal);
    }

    internal bool ValidateXmlSchemaBoundFacet(CompiledXmlSchemaSimpleType type, CompiledXmlSchemaFacet facet, XmlSchemaSimpleTypeValidationResult result, XmlNode? contextNode)
    {
        XdmAtomicValue? actual = result.AtomicValue;
        if (actual == null)
        {
            return false;
        }

        XmlNode? facetNode = facet.Node == null ? contextNode : facet.Node;
        XmlSchemaSimpleTypeValidationResult limit = ParseXmlSchemaAtomicValue(type, facet.Value, facetNode);
        if (!limit.Success || limit.AtomicValue == null)
        {
            return false;
        }

        int comparison = CompareXmlSchemaAtomicValues(actual, limit.AtomicValue);
        if (comparison == XdmFloatingDatatype.UnorderedComparison)
        {
            return false;
        }

        if (facet.Kind == XmlSchemaFacetKind.MinInclusive)
        {
            return comparison >= 0;
        }

        if (facet.Kind == XmlSchemaFacetKind.MinExclusive)
        {
            return comparison > 0;
        }

        if (facet.Kind == XmlSchemaFacetKind.MaxInclusive)
        {
            return comparison <= 0;
        }

        return comparison < 0;
    }

    internal int CompareXmlSchemaAtomicValues(XdmAtomicValue left, XdmAtomicValue right)
    {
        if (Types.IsXdmNumericAtomicType(left.Type) && Types.IsXdmNumericAtomicType(right.Type))
        {
            return new XdmNumericDatatype().CompareXdmNumeric(left, right);
        }

        if (Types.IsXdmDurationDerivedAtomicType(left.Type) && Types.IsXdmDurationDerivedAtomicType(right.Type))
        {
            if (left.Type == XdmAtomicType.Duration || right.Type == XdmAtomicType.Duration)
            {
                return XdmFloatingDatatype.UnorderedComparison;
            }

            return new XdmDurationDatatype().CompareXdmDuration(left, right);
        }

        if (left.Type >= XdmAtomicType.DateTime && left.Type <= XdmAtomicType.GDay && right.Type == left.Type)
        {
            return new XdmDateTimeDatatype().CompareXdmDateTime(left, right, 0);
        }

        return XdmFloatingDatatype.UnorderedComparison;
    }

    internal bool ValidateXmlSchemaDigitFacet(CompiledXmlSchemaFacet facet, XmlSchemaSimpleTypeValidationResult result)
    {
        XdmAtomicValue? value = result.AtomicValue;
        if (value == null)
        {
            return false;
        }

        XdmDecimalStorage storage = new XdmNumericDatatype().ReadXdmDecimalStorage(value);
        BigInteger absolute = BigInteger.Abs(storage.Coefficient);
        int totalDigits = absolute == BigInteger.Zero ? 1 : absolute.ToString(CultureInfo.InvariantCulture).Length;
        int scale = storage.Scale;
        BigInteger limit = BigInteger.Parse(facet.Value, CultureInfo.InvariantCulture);
        if (facet.Kind == XmlSchemaFacetKind.TotalDigits)
        {
            return new BigInteger(totalDigits) <= limit;
        }

        if (facet.Kind == XmlSchemaFacetKind.FractionDigits || facet.Kind == XmlSchemaFacetKind.MaxScale)
        {
            return new BigInteger(scale) <= limit;
        }

        return new BigInteger(scale) >= limit;
    }

    internal bool ValidateXmlSchemaTimezoneFacet(CompiledXmlSchemaFacet facet, XmlSchemaSimpleTypeValidationResult result)
    {
        XdmAtomicValue? value = result.AtomicValue;
        object? extended = value == null ? null : value.ExtendedValue;
        if (extended == null)
        {
            return false;
        }

        XdmDateTimeStorage storage = (XdmDateTimeStorage)extended;
        if (facet.Value == "required")
        {
            return storage.HasTimezone;
        }

        if (facet.Value == "prohibited")
        {
            return !storage.HasTimezone;
        }

        return true;
    }

    internal string NormalizeXmlSchemaWhitespace(CompiledXmlSchemaSimpleType type, string value)
    {
        CompiledXmlSchemaFacet? facet = FindLastXmlSchemaFacet(type.EffectiveFacets, XmlSchemaFacetKind.WhiteSpace);
        if (facet == null || facet.Value == "preserve")
        {
            return value;
        }

        if (facet.Value == "replace")
        {
            return new XdmStringDatatype().ReplaceXdmStringWhitespace(value);
        }

        return new XdmStringDatatype().CollapseXdmStringWhitespace(value);
    }

    internal CompiledXmlSchemaFacet? FindFirstXmlSchemaFacet(CompiledXmlSchemaFacet[] facets, XmlSchemaFacetKind kind)
    {
        int index = 0;
        while (index < facets.Length)
        {
            if (facets[index].Kind == kind)
            {
                return facets[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal CompiledXmlSchemaFacet? FindLastXmlSchemaFacet(CompiledXmlSchemaFacet[] facets, XmlSchemaFacetKind kind)
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

    internal XPathStaticContext CreateXmlSchemaValueContext(XmlNode? node)
    {
        XPathStaticContext context = new XPathStaticContext();
        List<XmlNamespaceBinding> bindings = new List<XmlNamespaceBinding>();
        List<string> prefixes = new List<string>();
        XmlNode? current = node;
        while (current != null)
        {
            XmlNamespaceBinding[] declared = current.NamespaceBindings == null ? [] : current.NamespaceBindings;
            int index = declared.Length - 1;
            while (index >= 0)
            {
                string? optionalPrefix = declared[index].Prefix;
                string prefix = optionalPrefix == null ? "" : optionalPrefix;
                if (!prefixes.Contains(prefix))
                {
                    prefixes.Add(prefix);
                    bindings.Add(declared[index]);
                    if (prefix.Length == 0 && !declared[index].IsUndeclaration)
                    {
                        context.DefaultElementNamespace = declared[index].NamespaceUri;
                    }
                }

                index = index - 1;
            }

            current = current.Parent;
        }

        XmlNamespaceBinding xml = new XmlNamespaceBinding();
        xml.Prefix = "xml";
        xml.NamespaceUri = XmlNamespaces.Xml;
        if (!prefixes.Contains("xml"))
        {
            bindings.Add(xml);
        }

        context.Namespaces = bindings.ToArray();
        return context;
    }
}
