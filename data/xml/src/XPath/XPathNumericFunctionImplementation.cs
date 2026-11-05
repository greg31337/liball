using System;
using System.Numerics;

namespace xml;

internal class XPathNumericFunctionImplementation : IXPathFunction
{
    internal string LocalName;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmTypeSystem Types;
    internal XdmNumericDatatype Numerics;
    internal XdmDurationDatatype Durations;
    internal XdmDateTimeDatatype DateTimes;
    internal XdmFactory Factory;

    internal XPathNumericFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext)
    {
        LocalName = localName;
        Range = CopyXPathNumericRange(range);
        StaticContext = staticContext;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Types = new XdmTypeSystem();
        Numerics = new XdmNumericDatatype();
        Durations = new XdmDurationDatatype();
        DateTimes = new XdmDateTimeDatatype();
        Factory = new XdmFactory();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        if (LocalName == "number")
        {
            return EvaluateXPathNumberFunction(arguments, context);
        }

        if (LocalName == "abs" || LocalName == "ceiling" || LocalName == "floor" || LocalName == "round" || LocalName == "round-half-to-even")
        {
            return EvaluateXPathUnaryNumericFunction(arguments);
        }

        if (LocalName == "sum" || LocalName == "avg")
        {
            return EvaluateXPathAdditiveAggregate(arguments);
        }

        if (LocalName == "min" || LocalName == "max")
        {
            return EvaluateXPathOrderedAggregate(arguments, context);
        }

        throw new InvalidOperationException("The XPath numeric function is not implemented.");
    }

    internal XdmSequence EvaluateXPathNumberFunction(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmSequence supplied;
        if (arguments.Length == 0)
        {
            XdmSequence? contextItem = context.ContextItem;
            if (contextItem == null || contextItem.Count == 0)
            {
                throw CreateXPathNumericError("fn:number requires a context item when its argument is omitted.", XPathErrorCodes.XPDY0002);
            }

            supplied = contextItem;
        }
        else
        {
            supplied = arguments[0];
        }

        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count == 0)
        {
            return CreateXPathNumericAtomicResult(Factory.CreateXdmFloatingAtomicValue("NaN", XdmAtomicType.Double));
        }

        if (atomized.Count > 1)
        {
            throw CreateXPathNumericError("fn:number requires an argument that atomizes to at most one value.", XPathErrorCodes.XPTY0004);
        }

        XdmAtomicValue? value = Sequences.ReadValidatedXdmSequenceItem(atomized, 0).AtomicValue;
        if (value == null)
        {
            throw new InvalidOperationException("Atomization did not produce an atomic value.");
        }

        XdmConversionResult converted = Converter.TryCastXdmAtomic(value, XdmAtomicType.Double, StaticContext);
        XdmAtomicValue result = converted.Success && converted.Value != null ? converted.Value : Factory.CreateXdmFloatingAtomicValue("NaN", XdmAtomicType.Double);
        return CreateXPathNumericAtomicResult(result);
    }

    internal XdmSequence EvaluateXPathUnaryNumericFunction(XdmSequence[] arguments)
    {
        int precision = arguments.Length == 2 ? ReadXPathNumericPrecision(arguments[1]) : 0;
        XdmAtomicValue? value = ReadXPathOptionalNumeric(arguments[0]);
        if (value == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XdmAtomicValue result;
        if (LocalName == "abs")
        {
            result = Numerics.AbsoluteXdmNumeric(value);
        }
        else if (LocalName == "ceiling")
        {
            result = Numerics.CeilingXdmNumeric(value);
        }
        else if (LocalName == "floor")
        {
            result = Numerics.FloorXdmNumeric(value);
        }
        else if (LocalName == "round")
        {
            result = Numerics.RoundXdmNumeric(value, precision);
        }
        else
        {
            result = Numerics.RoundHalfToEvenXdmNumeric(value, precision);
        }

        return CreateXPathNumericAtomicResult(result);
    }

    internal XdmAtomicValue? ReadXPathOptionalNumeric(XdmSequence supplied)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count == 0)
        {
            return null;
        }

        if (atomized.Count > 1)
        {
            throw CreateXPathNumericError("A numeric function argument must atomize to at most one value.", XPathErrorCodes.XPTY0004);
        }

        XdmAtomicValue? value = Sequences.ReadValidatedXdmSequenceItem(atomized, 0).AtomicValue;
        if (value == null)
        {
            throw new InvalidOperationException("Atomization did not produce an atomic value.");
        }

        if (value.Type == XdmAtomicType.UntypedAtomic)
        {
            value = RequireXPathNumericConversion(Converter.TryCastXdmAtomic(value, XdmAtomicType.Double, StaticContext));
        }

        if (!Types.IsXdmNumericAtomicType(value.Type))
        {
            throw CreateXPathNumericError("The function argument must be numeric.", XPathErrorCodes.XPTY0004);
        }

        return value;
    }

    internal int ReadXPathNumericPrecision(XdmSequence supplied)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count != 1)
        {
            throw CreateXPathNumericError("A rounding precision must be exactly one integer.", XPathErrorCodes.XPTY0004);
        }

        XdmAtomicValue? value = Sequences.ReadValidatedXdmSequenceItem(atomized, 0).AtomicValue;
        if (value == null)
        {
            throw new InvalidOperationException("Atomization did not produce an atomic precision.");
        }

        if (value.Type == XdmAtomicType.UntypedAtomic)
        {
            value = RequireXPathNumericConversion(Converter.TryCastXdmAtomic(value, XdmAtomicType.Integer, StaticContext));
        }

        if (!Types.IsXdmAtomicSubtypeOf(value.Type, XdmAtomicType.Integer))
        {
            throw CreateXPathNumericError("A rounding precision must be an integer.", XPathErrorCodes.XPTY0004);
        }

        BigInteger precision = Numerics.ReadXdmIntegerValue(value);
        if (precision > new BigInteger(XdmNumericDatatype.MaximumNumericDigits))
        {
            return XdmNumericDatatype.MaximumNumericDigits;
        }

        if (precision < new BigInteger(-XdmNumericDatatype.MaximumNumericDigits - 1))
        {
            return -XdmNumericDatatype.MaximumNumericDigits - 1;
        }

        return (int)precision;
    }

    internal XdmSequence EvaluateXPathAdditiveAggregate(XdmSequence[] arguments)
    {
        XdmAtomicValue[] values = PrepareXPathNumericAggregate(arguments[0], false);
        XdmSequence? zero = arguments.Length == 2 ? ReadXPathSumZero(arguments[1]) : null;
        if (values.Length == 0)
        {
            if (LocalName == "avg")
            {
                return Sequences.CreateEmptyXdmSequence();
            }

            if (zero != null)
            {
                return zero;
            }

            return CreateXPathNumericAtomicResult(Factory.CreateXdmIntegerAtomicValue("0", XdmAtomicType.Integer));
        }

        XdmAtomicValue result = values[0];
        int index = 1;
        while (index < values.Length)
        {
            result = IsXPathOrderedDuration(result.Type) ? Durations.AddXdmDurations(result, values[index]) : Numerics.AddXdmNumeric(result, values[index]);
            index = index + 1;
        }

        if (LocalName == "avg")
        {
            XdmAtomicValue count = Factory.CreateXdmIntegerAtomicValue(values.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), XdmAtomicType.Integer);
            result = IsXPathOrderedDuration(result.Type) ? Durations.DivideXdmDuration(result, count) : Numerics.DivideXdmNumeric(result, count);
        }

        return CreateXPathNumericAtomicResult(result);
    }

    internal XdmSequence ReadXPathSumZero(XdmSequence supplied)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count > 1)
        {
            throw CreateXPathNumericError("The zero value supplied to fn:sum must contain at most one atomic value.", XPathErrorCodes.XPTY0004);
        }

        return Sequences.FreezeXdmSequenceStorage(atomized);
    }

    internal XdmSequence EvaluateXPathOrderedAggregate(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmAtomicValue[] values = PrepareXPathNumericAggregate(arguments[0], true);
        if (arguments.Length == 2)
        {
            ValidateXPathNumericCollationArgument(arguments[1]);
        }

        if (values.Length == 0)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        bool strings = IsXPathStringAggregateValue(values[0]);
        IXPathCollation? collation = strings ? ResolveXPathNumericCollation(arguments) : null;
        bool binary = values[0].Type == XdmAtomicType.HexBinary || values[0].Type == XdmAtomicType.Base64Binary;
        if (!strings && !binary && values[0].Type != XdmAtomicType.Boolean && !IsXPathOrderedDuration(values[0].Type))
        {
            XdmAtomicType commonType = FindXPathAggregateNumericType(values);
            if (RequiresXPathAggregatePromotion(values, commonType))
            {
                int promotionIndex = 0;
                while (promotionIndex < values.Length)
                {
                    values[promotionIndex] = Numerics.PromoteXdmNumeric(values[promotionIndex], commonType);
                    promotionIndex = promotionIndex + 1;
                }
            }
        }

        XdmAtomicValue result = values[0];
        int index = 0;
        while (index < values.Length)
        {
            XdmAtomicValue candidate = values[index];
            if (IsXPathNumericNaN(candidate))
            {
                return CreateXPathNumericAtomicResult(candidate);
            }

            int comparison;
            if (strings)
            {
                if (collation == null)
                {
                    throw new InvalidOperationException("A string aggregate has no collation.");
                }

                string candidateString = Converter.ReadXdmAtomicStringValue(candidate);
                string resultString = Converter.ReadXdmAtomicStringValue(result);
                comparison = collation.CompareXPathCollation(candidateString, resultString);
            }
            else if (result.Type == XdmAtomicType.Boolean)
            {
                comparison = Numerics.CompareXdmBoolean(candidate, result);
            }
            else if (result.Type == XdmAtomicType.Date || result.Type == XdmAtomicType.Time || result.Type == XdmAtomicType.DateTime || result.Type == XdmAtomicType.DateTimeStamp)
            {
                int timezone = context.ImplicitTimezoneMinutes;
                comparison = DateTimes.CompareXdmDateTime(candidate, result, timezone);
            }
            else if (binary)
            {
                comparison = new XdmBinaryDatatype().CompareXdmBinary(candidate, result);
            }
            else
            {
                comparison = IsXPathOrderedDuration(result.Type) ? Durations.CompareXdmDuration(candidate, result) : Numerics.CompareXdmNumeric(candidate, result);
            }
            bool replace = LocalName == "min" ? comparison < 0 : comparison > 0;
            if (replace)
            {
                result = candidate;
            }

            index = index + 1;
        }

        return CreateXPathNumericAtomicResult(result);
    }

    internal XdmAtomicValue[] PrepareXPathNumericAggregate(XdmSequence supplied, bool allowBoolean)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        XdmAtomicValue[] values = new XdmAtomicValue[atomized.Count];
        bool numeric = false;
        bool boolean = false;
        bool strings = false;
        bool anyUris = false;
        XdmAtomicType durationType = XdmAtomicType.Duration;
        XdmAtomicType calendarType = XdmAtomicType.AnyAtomicType;
        XdmAtomicType binaryType = XdmAtomicType.AnyAtomicType;
        int index = 0;
        while (index < atomized.Count)
        {
            XdmAtomicValue? value = Sequences.ReadValidatedXdmSequenceItem(atomized, index).AtomicValue;
            if (value == null)
            {
                throw new InvalidOperationException("Atomization did not produce an atomic aggregate value.");
            }

            if (value.Type == XdmAtomicType.UntypedAtomic)
            {
                value = RequireXPathNumericConversion(Converter.TryCastXdmAtomic(value, XdmAtomicType.Double, StaticContext));
            }

            if (Types.IsXdmNumericAtomicType(value.Type))
            {
                if (boolean || strings || anyUris || durationType != XdmAtomicType.Duration || binaryType != XdmAtomicType.AnyAtomicType)
                {
                    throw CreateXPathNumericError("An aggregate input contains incompatible atomic types.", XPathErrorCodes.FORG0006);
                }

                numeric = true;
            }
            else if (IsXPathOrderedDuration(value.Type))
            {
                if (boolean || strings || anyUris || numeric || binaryType != XdmAtomicType.AnyAtomicType || durationType != XdmAtomicType.Duration && durationType != value.Type)
                {
                    throw CreateXPathNumericError("An aggregate input contains incompatible atomic types.", XPathErrorCodes.FORG0006);
                }

                durationType = value.Type;
            }
            else if (allowBoolean && value.Type == XdmAtomicType.Boolean)
            {
                if (strings || anyUris || numeric || durationType != XdmAtomicType.Duration || binaryType != XdmAtomicType.AnyAtomicType)
                {
                    throw CreateXPathNumericError("An aggregate input contains incompatible atomic types.", XPathErrorCodes.FORG0006);
                }

                boolean = true;
            }
            else if (allowBoolean && Types.IsXdmStringDerivedAtomicType(value.Type))
            {
                if (boolean || numeric || durationType != XdmAtomicType.Duration || binaryType != XdmAtomicType.AnyAtomicType)
                {
                    throw CreateXPathNumericError("An aggregate input contains incompatible atomic types.", XPathErrorCodes.FORG0006);
                }

                strings = true;
            }
            else if (allowBoolean && value.Type == XdmAtomicType.AnyUri)
            {
                if (boolean || numeric || durationType != XdmAtomicType.Duration || binaryType != XdmAtomicType.AnyAtomicType)
                {
                    throw CreateXPathNumericError("An aggregate input contains incompatible atomic types.", XPathErrorCodes.FORG0006);
                }

                anyUris = true;
            }
            else if (allowBoolean && (value.Type == XdmAtomicType.Date || value.Type == XdmAtomicType.Time || value.Type == XdmAtomicType.DateTime || value.Type == XdmAtomicType.DateTimeStamp))
            {
                bool dateTimes = (calendarType == XdmAtomicType.DateTime || calendarType == XdmAtomicType.DateTimeStamp) && (value.Type == XdmAtomicType.DateTime || value.Type == XdmAtomicType.DateTimeStamp);
                if (boolean || strings || anyUris || numeric || durationType != XdmAtomicType.Duration || binaryType != XdmAtomicType.AnyAtomicType || calendarType != XdmAtomicType.AnyAtomicType && calendarType != value.Type && !dateTimes)
                {
                    throw CreateXPathNumericError("An aggregate input contains incompatible atomic types.", XPathErrorCodes.FORG0006);
                }

                calendarType = value.Type;
            }
            else if (allowBoolean && (value.Type == XdmAtomicType.HexBinary || value.Type == XdmAtomicType.Base64Binary))
            {
                if (boolean || strings || anyUris || numeric || durationType != XdmAtomicType.Duration || calendarType != XdmAtomicType.AnyAtomicType || binaryType != XdmAtomicType.AnyAtomicType && binaryType != value.Type)
                {
                    throw CreateXPathNumericError("An aggregate input contains incompatible atomic types.", XPathErrorCodes.FORG0006);
                }

                binaryType = value.Type;
            }
            else
            {
                throw CreateXPathNumericError("An aggregate input must contain compatible numeric or ordered duration values.", XPathErrorCodes.FORG0006);
            }

            values[index] = value;
            index = index + 1;
        }

        if (strings && anyUris)
        {
            int conversionIndex = 0;
            while (conversionIndex < values.Length)
            {
                XdmAtomicValue value = values[conversionIndex];
                if (value.Type == XdmAtomicType.AnyUri)
                {
                    values[conversionIndex] = Factory.CreateXdmStringAtomicValue(Converter.ReadXdmAtomicStringValue(value), XdmAtomicType.String);
                }

                conversionIndex = conversionIndex + 1;
            }
        }

        return values;
    }

    internal bool IsXPathStringAggregateValue(XdmAtomicValue value)
    {
        return Types.IsXdmStringDerivedAtomicType(value.Type) || value.Type == XdmAtomicType.AnyUri;
    }

    internal IXPathCollation ResolveXPathNumericCollation(XdmSequence[] arguments)
    {
        string uri = StaticContext.DefaultCollation == null ? XPathNamespaces.CodepointCollation : StaticContext.DefaultCollation;
        if (arguments.Length == 2)
        {
            XdmSequence atomized = Converter.AtomizeXdmSequence(arguments[1]);
            XdmAtomicValue value = ReadXPathNumericCollationValue(atomized);
            uri = Converter.ReadXdmAtomicStringValue(value);
        }

        uri = ResolveXPathNumericCollationUri(uri);
        XPathCollationBinding[] bindings = StaticContext.CollationBindings == null ? [] : StaticContext.CollationBindings;
        int index = 0;
        while (index < bindings.Length)
        {
            XPathCollationBinding? binding = bindings[index];
            if (binding != null && binding.Uri == uri && binding.Implementation != null)
            {
                return binding.Implementation;
            }

            index = index + 1;
        }

        if (uri == XPathNamespaces.CodepointCollation)
        {
            return new XPathCodepointCollation();
        }

        if (uri == XPathAsciiCaseInsensitiveCollation.Uri)
        {
            return new XPathAsciiCaseInsensitiveCollation();
        }

        throw CreateXPathNumericError("The requested XPath collation is not supported.", XPathErrorCodes.FOCH0002);
    }

    internal XdmAtomicValue ReadXPathNumericCollationValue(XdmSequence atomized)
    {
        if (atomized.Count != 1)
        {
            throw CreateXPathNumericError("A collation argument must atomize to exactly one string.", XPathErrorCodes.XPTY0004);
        }

        XdmAtomicValue? value = Sequences.ReadValidatedXdmSequenceItem(atomized, 0).AtomicValue;
        if (value == null)
        {
            throw new InvalidOperationException("Atomization did not produce an atomic collation name.");
        }

        bool stringValue = Types.IsXdmStringDerivedAtomicType(value.Type) || value.Type == XdmAtomicType.UntypedAtomic || value.Type == XdmAtomicType.AnyUri;
        if (!stringValue)
        {
            throw CreateXPathNumericError("A collation argument must be a string.", XPathErrorCodes.XPTY0004);
        }

        return value;
    }

    internal string ResolveXPathNumericCollationUri(string value)
    {
        if (Uri.IsWellFormedUriString(value, UriKind.Absolute))
        {
            return new Uri(value, UriKind.Absolute).AbsoluteUri;
        }

        string? baseUri = StaticContext.BaseUri;
        if (baseUri == null || !Uri.IsWellFormedUriString(value, UriKind.Relative))
        {
            throw CreateXPathNumericError("The collation URI cannot be resolved.", XPathErrorCodes.FOCH0002);
        }

        return new Uri(new Uri(baseUri, UriKind.Absolute), value).AbsoluteUri;
    }

    internal XdmAtomicType FindXPathAggregateNumericType(XdmAtomicValue[] values)
    {
        XdmAtomicType result = XdmAtomicType.Integer;
        int index = 0;
        while (index < values.Length)
        {
            XdmAtomicType type = values[index].Type;
            if (type == XdmAtomicType.Double)
            {
                return XdmAtomicType.Double;
            }

            if (type == XdmAtomicType.Float)
            {
                result = XdmAtomicType.Float;
            }
            else if (type == XdmAtomicType.Decimal && result == XdmAtomicType.Integer)
            {
                result = XdmAtomicType.Decimal;
            }

            index = index + 1;
        }

        return result;
    }

    internal bool RequiresXPathAggregatePromotion(XdmAtomicValue[] values, XdmAtomicType commonType)
    {
        if (commonType == XdmAtomicType.Integer)
        {
            return false;
        }

        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Type != commonType)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void ValidateXPathNumericCollationArgument(XdmSequence supplied)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count != 1)
        {
            throw CreateXPathNumericError("A collation argument must atomize to exactly one string.", XPathErrorCodes.XPTY0004);
        }

        XdmAtomicValue? value = Sequences.ReadValidatedXdmSequenceItem(atomized, 0).AtomicValue;
        if (value == null)
        {
            throw new InvalidOperationException("Atomization did not produce an atomic collation name.");
        }

        bool stringValue = Types.IsXdmStringDerivedAtomicType(value.Type) || value.Type == XdmAtomicType.UntypedAtomic || value.Type == XdmAtomicType.AnyUri;
        if (!stringValue)
        {
            throw CreateXPathNumericError("A collation argument must be a string.", XPathErrorCodes.XPTY0004);
        }
    }

    internal bool IsXPathOrderedDuration(XdmAtomicType type)
    {
        return type == XdmAtomicType.YearMonthDuration || type == XdmAtomicType.DayTimeDuration;
    }

    internal bool IsXPathNumericNaN(XdmAtomicValue value)
    {
        if (value.Type == XdmAtomicType.Float)
        {
            return float.IsNaN((float)value.FloatingValue);
        }

        return value.Type == XdmAtomicType.Double && double.IsNaN(value.FloatingValue);
    }

    internal XdmAtomicValue RequireXPathNumericConversion(XdmConversionResult conversion)
    {
        if (conversion.Success && conversion.Value != null)
        {
            return conversion.Value;
        }

        string code = conversion.ErrorCode == null ? XPathErrorCodes.FORG0001 : conversion.ErrorCode;
        string message = conversion.ErrorMessage == null ? "The value cannot be converted to the required numeric type." : conversion.ErrorMessage;
        throw CreateXPathNumericError(message, code);
    }

    internal XdmSequence CreateXPathNumericAtomicResult(XdmAtomicValue value)
    {
        return Sequences.CreateXdmAtomicSequence(value);
    }

    internal XPathDynamicException CreateXPathNumericError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = CopyXPathNumericRange(Range);
        return new XPathDynamicException(message, diagnostic, diagnostic.Range.Start);
    }

    internal SourceRange CopyXPathNumericRange(SourceRange source)
    {
        SourceRange result = new SourceRange();
        result.Start = CopyXPathNumericLocation(source.Start);
        result.End = CopyXPathNumericLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXPathNumericLocation(SourceLocation? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceLocation result = new SourceLocation();
        result.SourceUri = source.SourceUri;
        result.CharacterOffset = source.CharacterOffset;
        result.ByteOffset = source.ByteOffset;
        result.Line = source.Line;
        result.Column = source.Column;
        return result;
    }
}
