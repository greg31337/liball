using System;
using System.Numerics;

namespace xml;

internal class XPathMathFunctionImplementation : IXPathFunction
{
    internal string LocalName;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmTypeSystem Types;
    internal XdmNumericDatatype Numerics;
    internal XdmFactory Factory;

    internal XPathMathFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext)
    {
        LocalName = localName;
        Range = range;
        StaticContext = staticContext;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Types = new XdmTypeSystem();
        Numerics = new XdmNumericDatatype();
        Factory = new XdmFactory();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        if (LocalName == "pi")
        {
            return CreateXPathMathResult(Math.PI);
        }

        if (LocalName == "pow")
        {
            return EvaluateXPathMathPower(arguments);
        }

        if (LocalName == "atan2")
        {
            double y = ReadXPathRequiredMathDouble(arguments[0]);
            double x = ReadXPathRequiredMathDouble(arguments[1]);
            return CreateXPathMathResult(Math.Atan2(y, x));
        }

        XdmAtomicValue? argument = ReadXPathOptionalMathDouble(arguments[0]);
        if (argument == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        double value = argument.FloatingValue;
        double result;
        if (LocalName == "exp")
        {
            result = Math.Exp(value);
        }
        else if (LocalName == "exp10")
        {
            result = Math.Pow(10d, value);
        }
        else if (LocalName == "log")
        {
            result = Math.Log(value);
        }
        else if (LocalName == "log10")
        {
            result = Math.Log10(value);
        }
        else if (LocalName == "sqrt")
        {
            result = Math.Sqrt(value);
        }
        else if (LocalName == "sin")
        {
            result = Math.Sin(value);
        }
        else if (LocalName == "cos")
        {
            result = Math.Cos(value);
        }
        else if (LocalName == "tan")
        {
            result = Math.Tan(value);
        }
        else if (LocalName == "asin")
        {
            result = Math.Asin(value);
        }
        else if (LocalName == "acos")
        {
            result = Math.Acos(value);
        }
        else
        {
            result = Math.Atan(value);
        }

        return CreateXPathMathResult(result);
    }

    internal XdmSequence EvaluateXPathMathPower(XdmSequence[] arguments)
    {
        XdmAtomicValue exponent = ReadXPathRequiredMathNumeric(arguments[1]);
        XdmAtomicValue? suppliedBase = ReadXPathOptionalMathDouble(arguments[0]);
        if (suppliedBase == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        double value = suppliedBase.FloatingValue;
        double result;
        if (Types.IsXdmAtomicSubtypeOf(exponent.Type, XdmAtomicType.Integer))
        {
            result = PowerXPathMathInteger(value, Numerics.ReadXdmIntegerValue(exponent));
        }
        else
        {
            double power = ConvertXPathMathNumericToDouble(exponent);
            result = PowerXPathMathFloating(value, power);
        }

        return CreateXPathMathResult(result);
    }

    internal double PowerXPathMathInteger(double value, BigInteger exponent)
    {
        bool odd = !exponent.IsEven;
        bool negativeBase = IsXPathMathNegative(value);
        if (exponent == BigInteger.Zero)
        {
            return 1d;
        }

        if (double.IsNaN(value))
        {
            return double.NaN;
        }

        double absolute = Math.Abs(value);
        if (absolute == 1d)
        {
            return value < 0 && odd ? -1d : 1d;
        }

        if (value == 0d)
        {
            if (exponent > BigInteger.Zero)
            {
                return negativeBase && odd ? -0d : 0d;
            }

            return negativeBase && odd ? double.NegativeInfinity : double.PositiveInfinity;
        }

        if (double.IsInfinity(value))
        {
            if (exponent > BigInteger.Zero)
            {
                return negativeBase && odd ? double.NegativeInfinity : double.PositiveInfinity;
            }

            return negativeBase && odd ? -0d : 0d;
        }

        if (exponent > new BigInteger(int.MaxValue) || exponent < new BigInteger(int.MinValue))
        {
            bool grows = absolute > 1d ? exponent > BigInteger.Zero : exponent < BigInteger.Zero;
            if (grows)
            {
                return value < 0 && odd ? double.NegativeInfinity : double.PositiveInfinity;
            }

            return value < 0 && odd ? -0d : 0d;
        }

        return Math.Pow(value, (double)exponent);
    }

    internal double PowerXPathMathFloating(double value, double exponent)
    {
        if ((value == 1d || value == -1d) && (double.IsNaN(exponent) || double.IsInfinity(exponent)))
        {
            return 1d;
        }

        return Math.Pow(value, exponent);
    }

    internal bool IsXPathMathNegative(double value)
    {
        return value < 0d || value == 0d && BitConverter.DoubleToInt64Bits(value) < 0;
    }

    internal XdmAtomicValue? ReadXPathOptionalMathDouble(XdmSequence supplied)
    {
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.Double;
        expected.Occurrence = XdmOccurrence.ZeroOrOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        return converted.Count == 0 ? null : Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
    }

    internal double ReadXPathRequiredMathDouble(XdmSequence supplied)
    {
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.Double;
        expected.Occurrence = XdmOccurrence.ExactlyOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        XdmAtomicValue? result = Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
        return result == null ? double.NaN : result.FloatingValue;
    }

    internal XdmAtomicValue ReadXPathRequiredMathNumeric(XdmSequence supplied)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count != 1)
        {
            throw CreateXPathMathError("A math numeric argument must contain exactly one value.", XPathErrorCodes.XPTY0004);
        }

        XdmAtomicValue? value = Sequences.ReadValidatedXdmSequenceItem(atomized, 0).AtomicValue;
        if (value == null)
        {
            throw new InvalidOperationException("Atomization did not produce an atomic value.");
        }

        if (value.Type == XdmAtomicType.UntypedAtomic)
        {
            XdmConversionResult conversion = Converter.TryCastXdmAtomic(value, XdmAtomicType.Double, StaticContext);
            if (!conversion.Success || conversion.Value == null)
            {
                throw CreateXPathMathError("The math argument cannot be converted to a number.", XPathErrorCodes.FORG0001);
            }

            value = conversion.Value;
        }

        if (!Types.IsXdmNumericAtomicType(value.Type))
        {
            throw CreateXPathMathError("A math argument must be numeric.", XPathErrorCodes.XPTY0004);
        }

        return value;
    }

    internal double ConvertXPathMathNumericToDouble(XdmAtomicValue value)
    {
        XdmConversionResult conversion = Converter.TryCastXdmAtomic(value, XdmAtomicType.Double, StaticContext);
        if (!conversion.Success || conversion.Value == null)
        {
            throw CreateXPathMathError("The math argument cannot be promoted to xs:double.", XPathErrorCodes.FORG0001);
        }

        return conversion.Value.FloatingValue;
    }

    internal XdmSequence CreateXPathMathResult(double value)
    {
        string lexical = new XdmFloatingDatatype().CanonicalizeXdmFloating(value, XdmAtomicType.Double);
        return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmFloatingAtomicValue(lexical, XdmAtomicType.Double));
    }

    internal XPathDynamicException CreateXPathMathError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = Range;
        return new XPathDynamicException(message, diagnostic, Range.Start);
    }
}
