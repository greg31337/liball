using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace xml;

internal class XdmTypeConverter
{
    internal XdmSequenceOperations Sequences;
    internal XdmTypeSystem Types;
    internal XdmNodeAccessor Nodes;

    internal XdmTypeConverter()
    {
        Sequences = new XdmSequenceOperations();
        Types = new XdmTypeSystem();
        Nodes = new XdmNodeAccessor();
    }

    internal XdmSequence AtomizeXdmSequence(XdmSequence sequence)
    {
        Guard.NotNull(sequence, "sequence");
        Sequences.ValidateXdmSequenceStorage(sequence);
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        int position = 0;
        while (position < sequence.Count)
        {
            AppendAtomizedXdmItem(builder, Sequences.ReadValidatedXdmSequenceItem(sequence, position));
            position = position + 1;
        }

        return builder.BuildImmutableXdmSequence();
    }

    internal void AppendAtomizedXdmItem(XdmSequenceBuilder builder, XdmSequenceItem item)
    {
        if (item.Kind == XdmItemKind.Atomic && item.AtomicValue != null)
        {
            builder.AddXdmAtomicValue(item.AtomicValue);
            return;
        }

        if (item.Kind == XdmItemKind.Node && item.Node != null)
        {
            builder.AppendXdmSequence(DeriveXdmNodeTypedValue(item.Node));
            return;
        }

        if (item.Kind == XdmItemKind.Array && item.Array != null)
        {
            AppendAtomizedXdmArray(builder, item.Array);
            return;
        }

        throw CreateXdmConversionDynamicError("A function or map item cannot be atomized.", XPathErrorCodes.FOTY0013);
    }

    internal void AppendAtomizedXdmArray(XdmSequenceBuilder builder, XdmArrayValue array)
    {
        XdmSequence[] members = array.Members == null ? [] : array.Members;
        if (array.Count < 0 || array.Count != members.Length)
        {
            throw new ArgumentException("The XDM array member storage is inconsistent.", "array");
        }

        int index = 0;
        while (index < members.Length)
        {
            XdmSequence? member = members[index];
            if (member == null)
            {
                throw new ArgumentException("An XDM array contains a null member.", "array");
            }

            builder.AppendXdmSequence(AtomizeXdmSequence(member));
            index = index + 1;
        }
    }

    internal XdmSequence DeriveXdmNodeTypedValue(XmlNode node)
    {
        Guard.NotNull(node, "node");
        XdmNode wrapped = Nodes.WrapXmlNode(node);
        XmlSchemaTypeInfo? annotation = Nodes.GetXdmNodeTypeAnnotation(wrapped);
        if (wrapped.Kind == XdmNodeKind.Element && annotation != null && annotation.IsNilled)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        if (annotation != null && annotation.ActualValue != null && (wrapped.Kind == XdmNodeKind.Element || wrapped.Kind == XdmNodeKind.Attribute))
        {
            return Sequences.CreateXdmAtomicSequence(annotation.ActualValue);
        }

        if (annotation != null && annotation.ActualValues != null && (wrapped.Kind == XdmNodeKind.Element || wrapped.Kind == XdmNodeKind.Attribute))
        {
            XdmSequenceBuilder builder = new XdmSequenceBuilder();
            int index = 0;
            while (index < annotation.ActualValues.Length)
            {
                builder.AddXdmAtomicValue(annotation.ActualValues[index]);
                index = index + 1;
            }

            return builder.BuildImmutableXdmSequence();
        }

        string stringValue = Nodes.GetXdmNodeStringValue(wrapped);
        if (annotation != null && annotation.AtomicType != XdmAtomicType.AnyAtomicType && (wrapped.Kind == XdmNodeKind.Element || wrapped.Kind == XdmNodeKind.Attribute))
        {
            XdmAtomicValue typed = ParseXdmAnnotatedAtomicValue(stringValue, annotation.AtomicType, node);
            typed.TypeInfo = annotation;
            return Sequences.CreateXdmAtomicSequence(typed);
        }

        if (annotation != null && annotation.TypeName != null && !Types.IsXdmUntypedNodeTypeName(annotation.TypeName) && (wrapped.Kind == XdmNodeKind.Element || wrapped.Kind == XdmNodeKind.Attribute))
        {
            throw CreateXdmConversionDynamicError("The schema type annotation does not provide an atomic typed value.", XPathErrorCodes.FOTY0012);
        }

        XdmAtomicType type = XdmAtomicType.UntypedAtomic;
        if (wrapped.Kind == XdmNodeKind.Namespace || wrapped.Kind == XdmNodeKind.ProcessingInstruction || wrapped.Kind == XdmNodeKind.Comment)
        {
            type = XdmAtomicType.String;
        }

        XdmAtomicValue atomic = type == XdmAtomicType.String ? new XdmStringDatatype().ParseXdmStringAtomic(stringValue, type) : CreateXdmUntypedAtomicValue(stringValue);
        atomic.TypeInfo = annotation;
        return Sequences.CreateXdmAtomicSequence(atomic);
    }

    internal XdmAtomicValue ParseXdmAnnotatedAtomicValue(string lexicalValue, XdmAtomicType type, XmlNode node)
    {
        if (type == XdmAtomicType.QName || type == XdmAtomicType.Notation)
        {
            XmlNamespaceBinding[] namespaces = new TreeNavigation().SelectXmlNamespacesOperation(node);
            XmlNamespaceBinding? defaultBinding = new XdmQNameDatatype().FindXdmNamespaceBinding(namespaces, "");
            string? defaultNamespace = defaultBinding == null ? null : defaultBinding.NamespaceUri;
            return new XdmQNameDatatype().ParseXdmQNameWithNamespaces(lexicalValue, type, namespaces, defaultNamespace);
        }

        XdmConversionResult result = TryParseXdmAtomicLexical(lexicalValue, type, new XPathStaticContext());
        return RequireXdmConversionResult(result);
    }

    internal XdmAtomicValue CreateXdmUntypedAtomicValue(string value)
    {
        Guard.NotNull(value, "value");
        if (!new XdmStringDatatype().IsWellFormedXdmUtf16(value))
        {
            throw CreateXdmConversionDynamicError("The xs:untypedAtomic value contains a character outside the XML character set.", XPathErrorCodes.FORG0001);
        }

        XdmAtomicValue result = new XdmAtomicValue();
        result.Type = XdmAtomicType.UntypedAtomic;
        result.LexicalValue = value;
        result.CanonicalValue = value;
        result.StringValue = value;
        result.IsImmutable = true;
        return result;
    }

    internal bool EffectiveBooleanValueXdmSequence(XdmSequence sequence)
    {
        Guard.NotNull(sequence, "sequence");
        Sequences.ValidateXdmSequenceStorage(sequence);
        if (sequence.Count == 0)
        {
            return false;
        }

        XdmSequenceItem first = Sequences.ReadValidatedXdmSequenceItem(sequence, 0);
        if (first.Kind == XdmItemKind.Node)
        {
            return true;
        }

        if (sequence.Count != 1 || first.Kind != XdmItemKind.Atomic || first.AtomicValue == null)
        {
            throw InvalidXdmEffectiveBooleanValue();
        }

        XdmAtomicValue value = first.AtomicValue;
        if (value.Type == XdmAtomicType.Boolean)
        {
            return value.BooleanValue;
        }

        if (value.Type == XdmAtomicType.UntypedAtomic || value.Type == XdmAtomicType.AnyUri || Types.IsXdmStringDerivedAtomicType(value.Type))
        {
            return ReadXdmAtomicStringValue(value).Length != 0;
        }

        if (Types.IsXdmNumericAtomicType(value.Type))
        {
            if (value.Type == XdmAtomicType.Float || value.Type == XdmAtomicType.Double)
            {
                double number = value.Type == XdmAtomicType.Float ? (float)value.FloatingValue : value.FloatingValue;
                return number != 0 && !double.IsNaN(number);
            }

            return new XdmNumericDatatype().ReadXdmDecimalStorage(value).Coefficient != BigInteger.Zero;
        }

        throw InvalidXdmEffectiveBooleanValue();
    }

    internal bool MatchesXdmSequenceType(XdmSequence sequence, XdmSequenceType expected)
    {
        Guard.NotNull(sequence, "sequence");
        Guard.NotNull(expected, "expected");
        ValidateXdmSequenceType(expected);
        if (!Sequences.MatchesXdmSequenceCardinality(sequence, expected.Occurrence))
        {
            return false;
        }

        int position = 0;
        while (position < sequence.Count)
        {
            if (!MatchesXdmSequenceItemType(Sequences.ReadValidatedXdmSequenceItem(sequence, position), expected))
            {
                return false;
            }

            position = position + 1;
        }

        return true;
    }

    internal bool MatchesXdmSequenceItemType(XdmSequenceItem item, XdmSequenceType expected)
    {
        if (expected.AnyItem)
        {
            return true;
        }

        if (expected.AnyFunction)
        {
            return item.Kind == XdmItemKind.Function || item.Kind == XdmItemKind.Map || item.Kind == XdmItemKind.Array;
        }

        if (item.Kind != expected.ItemKind)
        {
            return false;
        }

        if (item.Kind == XdmItemKind.Atomic)
        {
            return item.AtomicValue != null && (expected.AtomicType == XdmAtomicType.AnyAtomicType || Types.IsXdmAtomicSubtypeOf(item.AtomicValue.Type, expected.AtomicType));
        }

        if (item.Kind == XdmItemKind.Node && !expected.AnyNode)
        {
            return item.Node != null && Nodes.WrapXmlNode(item.Node).Kind == expected.NodeKind;
        }

        return true;
    }

    internal void ValidateXdmSequenceType(XdmSequenceType expected)
    {
        Sequences.ValidateXdmSequenceOccurrence(expected.Occurrence);
        if (expected.AnyItem || expected.AnyFunction)
        {
            return;
        }

        if (expected.ItemKind < XdmItemKind.Node || expected.ItemKind > XdmItemKind.Array)
        {
            throw new ArgumentOutOfRangeException("expected");
        }

        if (expected.ItemKind == XdmItemKind.Atomic)
        {
            Types.GetXdmAtomicTypeDefinition(expected.AtomicType);
        }

        if (expected.ItemKind == XdmItemKind.Node && !expected.AnyNode && (expected.NodeKind < XdmNodeKind.Document || expected.NodeKind > XdmNodeKind.Text))
        {
            throw new ArgumentOutOfRangeException("expected");
        }
    }

    internal XdmSequence ApplyXdmFunctionConversion(XdmSequence supplied, XdmSequenceType expected, XPathStaticContext context)
    {
        Guard.NotNull(supplied, "supplied");
        Guard.NotNull(expected, "expected");
        Guard.NotNull(context, "context");
        ValidateXdmSequenceType(expected);
        XdmSequence converted = supplied;
        if (!expected.AnyItem && expected.ItemKind == XdmItemKind.Atomic)
        {
            XdmSequence atomized = AtomizeXdmSequence(supplied);
            XdmSequenceBuilder builder = new XdmSequenceBuilder();
            int position = 0;
            while (position < atomized.Count)
            {
                XdmSequenceItem item = Sequences.ReadValidatedXdmSequenceItem(atomized, position);
                if (item.AtomicValue == null)
                {
                    throw CreateXdmConversionTypeError("Function conversion did not produce an atomic value.", XPathErrorCodes.XPTY0004);
                }

                XdmAtomicValue value = item.AtomicValue;
                if (expected.AtomicType != XdmAtomicType.AnyAtomicType && value.Type == XdmAtomicType.UntypedAtomic)
                {
                    if (Types.IsXdmNamespaceSensitiveAtomicType(expected.AtomicType))
                    {
                        throw CreateXdmConversionTypeError("Function conversion cannot convert xs:untypedAtomic to a namespace-sensitive type.", XPathErrorCodes.XPTY0117);
                    }

                    value = CastXdmAtomic(value, expected.AtomicType, context);
                }
                else if (expected.AtomicType == XdmAtomicType.String && value.Type == XdmAtomicType.AnyUri)
                {
                    value = PromoteXdmAtomic(value, expected.AtomicType);
                }
                else if (expected.AtomicType != XdmAtomicType.AnyAtomicType && !Types.IsXdmAtomicSubtypeOf(value.Type, expected.AtomicType) && Types.IsXdmNumericAtomicType(value.Type))
                {
                    value = PromoteXdmAtomic(value, expected.AtomicType);
                }

                builder.AddXdmAtomicValue(value);
                position = position + 1;
            }

            converted = builder.BuildImmutableXdmSequence();
        }

        if (!MatchesXdmSequenceType(converted, expected))
        {
            throw CreateXdmConversionTypeError("The converted value does not match the required sequence type.", XPathErrorCodes.XPTY0004);
        }

        return converted;
    }

    internal XdmAtomicValue PromoteXdmAtomic(XdmAtomicValue value, XdmAtomicType targetType)
    {
        Guard.NotNull(value, "value");
        Types.GetXdmAtomicTypeDefinition(targetType);
        if (Types.IsXdmAtomicSubtypeOf(value.Type, targetType))
        {
            return value;
        }

        if (value.Type == XdmAtomicType.AnyUri && targetType == XdmAtomicType.String)
        {
            return new XdmStringDatatype().ParseXdmStringAtomic(ReadXdmAtomicStringValue(value), XdmAtomicType.String);
        }

        if (Types.IsXdmNumericAtomicType(value.Type))
        {
            if (targetType == XdmAtomicType.Float && value.Type != XdmAtomicType.Double && value.Type != XdmAtomicType.Float)
            {
                return new XdmNumericDatatype().PromoteXdmNumeric(value, targetType);
            }

            if (targetType == XdmAtomicType.Double && value.Type != XdmAtomicType.Double)
            {
                return new XdmNumericDatatype().PromoteXdmNumeric(value, targetType);
            }
        }

        throw CreateXdmConversionTypeError("The atomic value cannot be promoted to the requested type.", XPathErrorCodes.XPTY0004);
    }

    internal XdmAtomicValue CastXdmAtomic(XdmAtomicValue source, XdmAtomicType targetType, XPathStaticContext context)
    {
        Guard.NotNull(source, "source");
        Guard.NotNull(context, "context");
        EnsureXdmCastTargetAllowed(targetType);
        XdmConversionResult result = TryCastXdmAtomic(source, targetType, context);
        return RequireXdmConversionResult(result);
    }

    internal bool CastableXdmAtomic(XdmAtomicValue source, XdmAtomicType targetType, XPathStaticContext context)
    {
        Guard.NotNull(source, "source");
        Guard.NotNull(context, "context");
        EnsureXdmCastTargetAllowed(targetType);
        return TryCastXdmAtomic(source, targetType, context).Success;
    }

    internal XdmConversionResult TryCastXdmAtomic(XdmAtomicValue source, XdmAtomicType targetType, XPathStaticContext context)
    {
        Guard.NotNull(source, "source");
        Guard.NotNull(context, "context");
        Types.GetXdmAtomicTypeDefinition(source.Type);
        Types.GetXdmAtomicTypeDefinition(targetType);
        if (source.Type == targetType)
        {
            return SuccessfulXdmConversion(source);
        }

        if (targetType == XdmAtomicType.String || targetType == XdmAtomicType.UntypedAtomic)
        {
            string text = ReadXdmAtomicStringValue(source);
            XdmAtomicValue converted = targetType == XdmAtomicType.String ? new XdmStringDatatype().ParseXdmStringAtomic(text, targetType) : CreateXdmUntypedAtomicValue(text);
            return SuccessfulXdmConversion(converted);
        }

        if (Types.IsXdmStringDerivedAtomicType(targetType))
        {
            return ConvertXdmStringParseResult(new XdmStringDatatype().TryParseXdmStringAtomic(ReadXdmAtomicStringValue(source), targetType));
        }

        if (source.Type == XdmAtomicType.UntypedAtomic || Types.IsXdmStringDerivedAtomicType(source.Type))
        {
            return TryParseXdmAtomicLexical(ReadXdmAtomicStringValue(source), targetType, context);
        }

        if (source.Type == XdmAtomicType.AnyUri)
        {
            return UnsupportedXdmCast(source.Type, targetType);
        }

        if (source.Type == XdmAtomicType.QName || source.Type == XdmAtomicType.Notation)
        {
            if (targetType == XdmAtomicType.QName)
            {
                XmlName name = new XdmQNameDatatype().ReadXdmExpandedName(source, source.Type);
                string lexical = new XdmQNameDatatype().BuildXdmQNameLexical(name);
                return SuccessfulXdmConversion(new XdmQNameDatatype().CreateXdmNamespaceSensitiveValue(new XdmQNameDatatype().CopyXdmExpandedName(name), XdmAtomicType.QName, lexical));
            }

            return UnsupportedXdmCast(source.Type, targetType);
        }

        if (source.Type == XdmAtomicType.HexBinary || source.Type == XdmAtomicType.Base64Binary)
        {
            if (targetType == XdmAtomicType.HexBinary || targetType == XdmAtomicType.Base64Binary)
            {
                byte[] bytes = new XdmBinaryDatatype().ReadXdmBinaryOperand(source);
                return SuccessfulXdmConversion(new XdmBinaryDatatype().CreateXdmBinaryValue(bytes, targetType, null));
            }

            return UnsupportedXdmCast(source.Type, targetType);
        }

        if (source.Type == XdmAtomicType.Boolean || Types.IsXdmNumericAtomicType(source.Type))
        {
            return TryCastXdmNumericOrBoolean(source, targetType);
        }

        if (Types.IsXdmDurationDerivedAtomicType(source.Type))
        {
            return TryCastXdmDuration(source, targetType);
        }

        if (source.Type >= XdmAtomicType.DateTime && source.Type <= XdmAtomicType.GDay)
        {
            return TryCastXdmDateTime(source, targetType);
        }

        return UnsupportedXdmCast(source.Type, targetType);
    }

    internal XdmConversionResult TryParseXdmAtomicLexical(string lexicalValue, XdmAtomicType targetType, XPathStaticContext context)
    {
        if (targetType == XdmAtomicType.UntypedAtomic)
        {
            return SuccessfulXdmConversion(CreateXdmUntypedAtomicValue(lexicalValue));
        }

        if (Types.IsXdmStringDerivedAtomicType(targetType))
        {
            XdmStringParseResult parsedString = new XdmStringDatatype().TryParseXdmStringAtomic(lexicalValue, targetType);
            return ConvertXdmStringParseResult(parsedString);
        }

        if (targetType == XdmAtomicType.Boolean)
        {
            return ConvertXdmNumericParseResult(new XdmNumericDatatype().TryParseXdmBoolean(lexicalValue));
        }

        if (targetType == XdmAtomicType.Decimal)
        {
            return ConvertXdmNumericParseResult(new XdmNumericDatatype().TryParseXdmDecimal(lexicalValue));
        }

        if (Types.IsXdmAtomicSubtypeOf(targetType, XdmAtomicType.Integer))
        {
            return ConvertXdmLexicalParseResult(new XdmNumericDatatype().TryParseXdmInteger(lexicalValue, targetType));
        }

        if (targetType == XdmAtomicType.Float || targetType == XdmAtomicType.Double)
        {
            return ConvertXdmNumericParseResult(new XdmFloatingDatatype().TryParseXdmFloating(lexicalValue, targetType));
        }

        if (Types.IsXdmDurationDerivedAtomicType(targetType))
        {
            return ConvertXdmNumericParseResult(new XdmDurationDatatype().TryParseXdmDuration(lexicalValue, targetType));
        }

        if (targetType >= XdmAtomicType.DateTime && targetType <= XdmAtomicType.GDay)
        {
            return ConvertXdmNumericParseResult(new XdmDateTimeDatatype().TryParseXdmDateTime(lexicalValue, targetType));
        }

        if (targetType == XdmAtomicType.HexBinary || targetType == XdmAtomicType.Base64Binary)
        {
            return ConvertXdmNumericParseResult(new XdmBinaryDatatype().TryParseXdmBinary(lexicalValue, targetType));
        }

        if (targetType == XdmAtomicType.AnyUri)
        {
            return ConvertXdmStringParseResult(new XdmAnyUriDatatype().TryParseXdmAnyUri(lexicalValue));
        }

        if (targetType == XdmAtomicType.QName)
        {
            XmlNamespaceBinding[] namespaces = context.Namespaces == null ? [] : context.Namespaces;
            return ConvertXdmNumericParseResult(new XdmQNameDatatype().TryParseXdmQNameWithNamespaces(lexicalValue, targetType, namespaces, context.DefaultElementNamespace));
        }

        return UnsupportedXdmCast(XdmAtomicType.String, targetType);
    }

    internal XdmConversionResult TryCastXdmNumericOrBoolean(XdmAtomicValue source, XdmAtomicType targetType)
    {
        if (targetType == XdmAtomicType.Boolean)
        {
            bool boolean;
            if (source.Type == XdmAtomicType.Boolean)
            {
                boolean = source.BooleanValue;
            }
            else if (source.Type == XdmAtomicType.Float || source.Type == XdmAtomicType.Double)
            {
                double number = source.Type == XdmAtomicType.Float ? (float)source.FloatingValue : source.FloatingValue;
                boolean = number != 0 && !double.IsNaN(number);
            }
            else
            {
                boolean = new XdmNumericDatatype().ReadXdmDecimalStorage(source).Coefficient != BigInteger.Zero;
            }

            return SuccessfulXdmConversion(new XdmNumericDatatype().ParseXdmBoolean(boolean ? "true" : "false"));
        }

        bool numericTarget = targetType == XdmAtomicType.Decimal || targetType == XdmAtomicType.Float || targetType == XdmAtomicType.Double || Types.IsXdmAtomicSubtypeOf(targetType, XdmAtomicType.Integer);
        if (!numericTarget)
        {
            return UnsupportedXdmCast(source.Type, targetType);
        }

        if (source.Type == XdmAtomicType.Boolean)
        {
            return TryParseXdmAtomicLexical(source.BooleanValue ? "1" : "0", targetType, new XPathStaticContext());
        }

        if (targetType == XdmAtomicType.Float || targetType == XdmAtomicType.Double)
        {
            if (source.Type == XdmAtomicType.Float && targetType == XdmAtomicType.Double)
            {
                return SuccessfulXdmConversion(new XdmFloatingDatatype().PromoteXdmFloating(source, targetType));
            }

            return ConvertXdmNumericParseResult(new XdmFloatingDatatype().TryParseXdmFloating(ReadXdmAtomicStringValue(source), targetType));
        }

        if (targetType == XdmAtomicType.Decimal)
        {
            if (source.Type == XdmAtomicType.Decimal)
            {
                return SuccessfulXdmConversion(source);
            }

            if (Types.IsXdmAtomicSubtypeOf(source.Type, XdmAtomicType.Integer))
            {
                BigInteger integer = new XdmNumericDatatype().ReadXdmIntegerValue(source);
                return ConvertXdmNumericParseResult(new XdmNumericDatatype().TryParseXdmDecimal(integer.ToString(CultureInfo.InvariantCulture)));
            }

            double floating = source.Type == XdmAtomicType.Float ? (float)source.FloatingValue : source.FloatingValue;
            if (double.IsNaN(floating) || double.IsInfinity(floating))
            {
                return InvalidXdmCast("A non-finite floating-point value cannot be cast to xs:decimal.", XPathErrorCodes.FOCA0002);
            }

            return ConvertXdmNumericParseResult(new XdmNumericDatatype().TryParseXdmDecimal(FloatingToXdmDecimalLexical(floating, source.Type)));
        }

        BigInteger integral;
        if (source.Type == XdmAtomicType.Float || source.Type == XdmAtomicType.Double)
        {
            double floating = source.Type == XdmAtomicType.Float ? (float)source.FloatingValue : source.FloatingValue;
            if (double.IsNaN(floating) || double.IsInfinity(floating))
            {
                return InvalidXdmCast("A non-finite floating-point value cannot be cast to an integer type.", XPathErrorCodes.FOCA0002);
            }

            integral = new BigInteger(Math.Truncate(floating));
        }
        else
        {
            XdmDecimalStorage decimalValue = new XdmNumericDatatype().ReadXdmDecimalStorage(source);
            integral = decimalValue.Coefficient / new XdmNumericDatatype().PowerOfTen(decimalValue.Scale);
        }

        return ConvertXdmNumericParseResult(new XdmNumericDatatype().TryParseXdmInteger(integral.ToString(CultureInfo.InvariantCulture), targetType));
    }

    internal string FloatingToXdmDecimalLexical(double number, XdmAtomicType sourceType)
    {
        string roundTrip = sourceType == XdmAtomicType.Float ? ((float)number).ToString("R", CultureInfo.InvariantCulture) : number.ToString("R", CultureInfo.InvariantCulture);
        int exponentMarker = roundTrip.IndexOf('E');
        if (exponentMarker < 0)
        {
            exponentMarker = roundTrip.IndexOf('e');
        }

        if (exponentMarker < 0)
        {
            return roundTrip;
        }

        string mantissa = roundTrip.Substring(0, exponentMarker);
        int exponent = int.Parse(roundTrip.Substring(exponentMarker + 1), CultureInfo.InvariantCulture);
        bool negative = mantissa.StartsWith("-", StringComparison.Ordinal);
        if (negative || mantissa.StartsWith("+", StringComparison.Ordinal))
        {
            mantissa = mantissa.Substring(1);
        }

        int point = mantissa.IndexOf('.');
        int fractionLength = point < 0 ? 0 : mantissa.Length - point - 1;
        string digits = point < 0 ? mantissa : mantissa.Substring(0, point) + mantissa.Substring(point + 1);
        int scale = fractionLength - exponent;
        StringBuilder result = new StringBuilder();
        if (negative)
        {
            result.Append('-');
        }

        if (scale <= 0)
        {
            result.Append(digits);
            result.Append('0', -scale);
            return result.ToString();
        }

        if (scale >= digits.Length)
        {
            result.Append("0.");
            result.Append('0', scale - digits.Length);
            result.Append(digits);
            return result.ToString();
        }

        result.Append(digits.Substring(0, digits.Length - scale));
        result.Append('.');
        result.Append(digits.Substring(digits.Length - scale));
        return result.ToString();
    }

    internal XdmConversionResult TryCastXdmDuration(XdmAtomicValue source, XdmAtomicType targetType)
    {
        if (!Types.IsXdmDurationDerivedAtomicType(targetType))
        {
            return UnsupportedXdmCast(source.Type, targetType);
        }

        XdmDurationStorage storage = new XdmDurationDatatype().ReadXdmDurationStorage(source);
        if (targetType == XdmAtomicType.YearMonthDuration)
        {
            storage = new XdmDurationStorage(storage.Months, BigInteger.Zero, 0);
        }
        else if (targetType == XdmAtomicType.DayTimeDuration)
        {
            storage = new XdmDurationStorage(BigInteger.Zero, storage.SecondsCoefficient, storage.SecondsScale);
        }

        return SuccessfulXdmConversion(new XdmDurationDatatype().CreateXdmDurationValue(storage, targetType, null));
    }

    internal XdmConversionResult TryCastXdmDateTime(XdmAtomicValue source, XdmAtomicType targetType)
    {
        bool allowed = source.Type == targetType;
        if (source.Type == XdmAtomicType.DateTime || source.Type == XdmAtomicType.DateTimeStamp)
        {
            allowed = targetType == XdmAtomicType.DateTime || targetType == XdmAtomicType.DateTimeStamp || targetType == XdmAtomicType.Date || targetType == XdmAtomicType.Time || targetType == XdmAtomicType.GYearMonth || targetType == XdmAtomicType.GYear || targetType == XdmAtomicType.GMonthDay || targetType == XdmAtomicType.GMonth || targetType == XdmAtomicType.GDay;
        }
        else if (source.Type == XdmAtomicType.Date)
        {
            allowed = targetType == XdmAtomicType.Date || targetType == XdmAtomicType.DateTime || targetType == XdmAtomicType.DateTimeStamp || targetType == XdmAtomicType.GYearMonth || targetType == XdmAtomicType.GYear || targetType == XdmAtomicType.GMonthDay || targetType == XdmAtomicType.GMonth || targetType == XdmAtomicType.GDay;
        }

        if (!allowed)
        {
            return UnsupportedXdmCast(source.Type, targetType);
        }

        XdmDateTimeStorage storage = new XdmDateTimeDatatype().ReadXdmDateTimeStorage(source);
        if (targetType == XdmAtomicType.DateTimeStamp && !storage.HasTimezone)
        {
            return InvalidXdmCast("Casting to xs:dateTimeStamp requires a timezone.", XPathErrorCodes.FORG0001);
        }

        if (targetType == XdmAtomicType.Date || targetType == XdmAtomicType.GYearMonth || targetType == XdmAtomicType.GYear || targetType == XdmAtomicType.GMonthDay || targetType == XdmAtomicType.GMonth || targetType == XdmAtomicType.GDay)
        {
            storage.Hour = 0;
            storage.Minute = 0;
            storage.SecondCoefficient = BigInteger.Zero;
            storage.SecondScale = 0;
        }

        return SuccessfulXdmConversion(new XdmDateTimeDatatype().CreateXdmDateTimeValue(storage, targetType, null));
    }

    internal bool InstanceOfXdmSequence(XdmSequence value, XdmSequenceType requiredType)
    {
        return MatchesXdmSequenceType(value, requiredType);
    }

    internal XdmSequence TreatXdmSequence(XdmSequence value, XdmSequenceType requiredType)
    {
        if (!MatchesXdmSequenceType(value, requiredType))
        {
            throw CreateXdmConversionDynamicError("The value does not match the type required by treat as.", XPathErrorCodes.XPDY0050);
        }

        return value;
    }

    internal string ReadXdmAtomicStringValue(XdmAtomicValue value)
    {
        Guard.NotNull(value, "value");
        if (value.Type == XdmAtomicType.QName || value.Type == XdmAtomicType.Notation)
        {
            XmlName name = new XdmQNameDatatype().ReadXdmExpandedName(value, value.Type);
            return new XdmQNameDatatype().BuildXdmQNameLexical(name);
        }

        if (value.StringValue != null)
        {
            return value.StringValue;
        }

        if (value.CanonicalValue != null)
        {
            return value.CanonicalValue;
        }

        if (value.LexicalValue != null)
        {
            return value.LexicalValue;
        }

        return new SequenceNormalizer().CastXdmAtomicToSerializationString(value);
    }

    internal void EnsureXdmCastTargetAllowed(XdmAtomicType targetType)
    {
        XdmAtomicTypeDefinition definition = Types.GetXdmAtomicTypeDefinition(targetType);
        if (targetType == XdmAtomicType.AnyAtomicType || targetType == XdmAtomicType.Notation || definition.IsAbstract)
        {
            XmlDiagnostic diagnostic = new XmlDiagnostic();
            diagnostic.Code = XPathErrorCodes.XPST0080;
            diagnostic.Category = XmlErrorCategory.XPathStatic;
            diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
            diagnostic.Message = "The requested type cannot be the target of cast or castable.";
            throw new XPathStaticException(diagnostic.Message, diagnostic, null);
        }
    }

    internal XdmAtomicValue RequireXdmConversionResult(XdmConversionResult result)
    {
        if (result.Success && result.Value != null)
        {
            return result.Value;
        }

        string code = result.ErrorCode == null ? XPathErrorCodes.FORG0001 : result.ErrorCode;
        string message = result.ErrorMessage == null ? "The requested atomic conversion failed." : result.ErrorMessage;
        if (result.TypeError)
        {
            throw CreateXdmConversionTypeError(message, code);
        }

        throw CreateXdmConversionDynamicError(message, code);
    }

    internal XdmConversionResult ConvertXdmNumericParseResult(XdmNumericParseResult result)
    {
        return new XdmConversionResult(result.Success, false, result.Value, result.ErrorCode, result.ErrorMessage);
    }

    internal XdmConversionResult ConvertXdmLexicalParseResult(XdmNumericParseResult result)
    {
        return new XdmConversionResult(result.Success, false, result.Value, result.Success ? null : XPathErrorCodes.FORG0001, result.ErrorMessage);
    }

    internal XdmConversionResult ConvertXdmStringParseResult(XdmStringParseResult result)
    {
        return new XdmConversionResult(result.Success, false, result.Value, result.Success ? null : XPathErrorCodes.FORG0001, result.ErrorMessage);
    }

    internal XdmConversionResult SuccessfulXdmConversion(XdmAtomicValue value)
    {
        return new XdmConversionResult(true, false, value, null, null);
    }

    internal XdmConversionResult InvalidXdmCast(string message, string code)
    {
        return new XdmConversionResult(false, false, null, code, message);
    }

    internal XdmConversionResult UnsupportedXdmCast(XdmAtomicType sourceType, XdmAtomicType targetType)
    {
        return new XdmConversionResult(false, true, null, XPathErrorCodes.XPTY0004, "Casting from " + sourceType.ToString() + " to " + targetType.ToString() + " is not supported.");
    }

    internal XPathDynamicException InvalidXdmEffectiveBooleanValue()
    {
        return CreateXdmConversionDynamicError("The sequence has no effective Boolean value.", XPathErrorCodes.FORG0006);
    }

    internal XPathDynamicException CreateXdmConversionDynamicError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathDynamicException(message, diagnostic, null);
    }

    internal XPathTypeException CreateXdmConversionTypeError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathType;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathTypeException(message, diagnostic, null);
    }
}
