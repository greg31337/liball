using System.Globalization;
using System.Numerics;

namespace xml;

internal class XPathFunctionItemFunctionImplementation : IXPathFunction
{
    internal string LocalName;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XPathFunctionEvaluator Evaluator;
    internal XdmFunctionOperations Functions;
    internal XdmSequenceOperations Sequences;
    internal XdmFactory Factory;

    internal XPathFunctionItemFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext, XPathFunctionEvaluator evaluator)
    {
        LocalName = localName;
        Range = CopyXPathFunctionItemRange(range);
        StaticContext = staticContext;
        Evaluator = evaluator;
        Functions = new XdmFunctionOperations();
        Sequences = new XdmSequenceOperations();
        Factory = new XdmFactory();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        if (LocalName == "function-lookup")
        {
            XmlName name = ReadXPathFunctionLookupName(arguments[0]);
            BigInteger suppliedArity = ReadXPathFunctionItemInteger(arguments[1]);
            if (suppliedArity < BigInteger.Zero || suppliedArity > new BigInteger(int.MaxValue))
            {
                return Sequences.CreateEmptyXdmSequence();
            }

            XdmFunctionItem? found = Evaluator.LookupXPathNamedFunctionItem(name, (int)suppliedArity, Range);
            if (found == null)
            {
                return Sequences.CreateEmptyXdmSequence();
            }

            XPathDynamicContext captured = Functions.CreateXPathClosureContext(context, []);
            captured.XsltCurrentGroupAvailable = false;
            captured.XsltCurrentGroup = null;
            captured.XsltCurrentGroupingKeyAvailable = false;
            captured.XsltCurrentGroupingKey = null;
            captured.XsltCurrentMergeGroupAvailable = false;
            captured.XsltCurrentMergeGroup = null;
            captured.XsltCurrentMergeSourceNames = null;
            captured.XsltCurrentMergeSourceGroups = null;
            captured.XsltCurrentMergeKeyAvailable = false;
            captured.XsltCurrentMergeKey = null;
            captured.XsltKeys = null;
            captured.XsltRegexGroups = null;
            IXPathFunction implementation = new XPathContextBoundFunctionImplementation(found, captured, StaticContext);
            XPathFunctionBinding foundBinding = Functions.CreateXPathFunctionBinding(found);
            XdmFunctionItem result = Functions.CreateXdmFunctionItem(foundBinding, implementation, []);
            return Sequences.CreateXdmFunctionSequence(result);
        }

        XdmFunctionItem function = ReadXPathFunctionItem(arguments[0]);
        if (LocalName == "function-name")
        {
            XmlName? name = function.Name;
            if (name == null || name.LocalName == null)
            {
                return Sequences.CreateEmptyXdmSequence();
            }

            string lexical = name.Prefix == null || name.Prefix.Length == 0 ? name.LocalName : name.Prefix + ":" + name.LocalName;
            XdmAtomicValue value = new XdmQNameDatatype().CreateXdmQNameValue(name.NamespaceUri, lexical);
            return Sequences.CreateXdmAtomicSequence(value);
        }

        if (LocalName == "function-arity")
        {
            string lexical = function.Arity.ToString(CultureInfo.InvariantCulture);
            return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmIntegerAtomicValue(lexical, XdmAtomicType.Integer));
        }

        XdmArrayValue array = ReadXPathFunctionItemArray(arguments[1]);
        XdmSequence[] appliedArguments = array.Members == null ? [] : array.Members;
        if (function.Arity != appliedArguments.Length)
        {
            throw CreateXPathFunctionItemError("The supplied array size does not match the function arity.", XPathErrorCodes.FOAP0001);
        }

        return Functions.InvokeXdmFunctionItem(function, appliedArguments, context, StaticContext);
    }

    internal XdmFunctionItem ReadXPathFunctionItem(XdmSequence sequence)
    {
        Sequences.ValidateXdmSequenceStorage(sequence);
        if (sequence.Count != 1)
        {
            throw CreateXPathFunctionItemError("A function argument must contain exactly one function item.", XPathErrorCodes.XPTY0004);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(sequence, 0);
        if (item.Kind == XdmItemKind.Function && item.Function != null)
        {
            return item.Function;
        }

        if (item.Kind == XdmItemKind.Map && item.Map != null)
        {
            return Functions.CreateXdmMapFunctionItem(item.Map);
        }

        if (item.Kind == XdmItemKind.Array && item.Array != null)
        {
            return Functions.CreateXdmArrayFunctionItem(item.Array);
        }

        throw CreateXPathFunctionItemError("The supplied argument must be a function, map, or array.", XPathErrorCodes.XPTY0004);
    }

    internal XmlName ReadXPathFunctionLookupName(XdmSequence sequence)
    {
        Sequences.ValidateXdmSequenceStorage(sequence);
        if (sequence.Count != 1)
        {
            throw CreateXPathFunctionItemError("The function name must contain exactly one QName.", XPathErrorCodes.XPTY0004);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(sequence, 0);
        XdmAtomicValue? value = item.AtomicValue;
        if (item.Kind == XdmItemKind.Atomic && value != null && value.Type == XdmAtomicType.UntypedAtomic)
        {
            throw CreateXPathFunctionItemError("Function conversion cannot convert xs:untypedAtomic to xs:QName.", XPathErrorCodes.XPTY0117);
        }

        if (item.Kind != XdmItemKind.Atomic || value == null || value.Type != XdmAtomicType.QName || value.NameValue == null)
        {
            throw CreateXPathFunctionItemError("The function name must be an xs:QName.", XPathErrorCodes.XPTY0004);
        }

        return value.NameValue;
    }

    internal BigInteger ReadXPathFunctionItemInteger(XdmSequence sequence)
    {
        Sequences.ValidateXdmSequenceStorage(sequence);
        if (sequence.Count != 1)
        {
            throw CreateXPathFunctionItemError("The function arity must contain exactly one integer.", XPathErrorCodes.XPTY0004);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(sequence, 0);
        XdmAtomicValue? value = item.AtomicValue;
        if (item.Kind != XdmItemKind.Atomic || value == null || !new XdmTypeSystem().IsXdmAtomicSubtypeOf(value.Type, XdmAtomicType.Integer))
        {
            throw CreateXPathFunctionItemError("The function arity must be an xs:integer.", XPathErrorCodes.XPTY0004);
        }

        return new XdmNumericDatatype().ReadXdmIntegerValue(value);
    }

    internal XdmArrayValue ReadXPathFunctionItemArray(XdmSequence sequence)
    {
        Sequences.ValidateXdmSequenceStorage(sequence);
        if (sequence.Count != 1)
        {
            throw CreateXPathFunctionItemError("The argument list must contain exactly one array.", XPathErrorCodes.XPTY0004);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(sequence, 0);
        if (item.Kind != XdmItemKind.Array || item.Array == null)
        {
            throw CreateXPathFunctionItemError("The argument list must be an array.", XPathErrorCodes.XPTY0004);
        }

        return item.Array;
    }

    internal XPathDynamicException CreateXPathFunctionItemError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = CopyXPathFunctionItemRange(Range);
        return new XPathDynamicException(message, diagnostic, diagnostic.Range.Start);
    }

    internal SourceRange CopyXPathFunctionItemRange(SourceRange source)
    {
        SourceRange result = new SourceRange();
        result.Start = CopyXPathFunctionItemLocation(source.Start);
        result.End = CopyXPathFunctionItemLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXPathFunctionItemLocation(SourceLocation? source)
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

internal class XPathContextBoundFunctionImplementation : IXPathFunction
{
    internal XdmFunctionItem Function;
    internal XPathDynamicContext DynamicContext;
    internal XPathStaticContext StaticContext;
    internal XdmFunctionOperations Functions;

    internal XPathContextBoundFunctionImplementation(XdmFunctionItem function, XPathDynamicContext dynamicContext, XPathStaticContext staticContext)
    {
        Function = function;
        DynamicContext = dynamicContext;
        StaticContext = staticContext;
        Functions = new XdmFunctionOperations();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        return Functions.InvokeXdmFunctionItem(Function, arguments, DynamicContext, StaticContext);
    }
}
