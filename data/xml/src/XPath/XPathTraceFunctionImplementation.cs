namespace xml;

internal class XPathTraceFunctionImplementation : IXPathFunction
{
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;

    internal XPathTraceFunctionImplementation(SourceRange range, XPathStaticContext staticContext)
    {
        Range = CopyXPathTraceFunctionRange(range);
        StaticContext = staticContext;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmSequence result = Sequences.FreezeXdmSequenceStorage(arguments[0]);
        if (arguments.Length == 1)
        {
            return result;
        }

        XdmSequenceType labelType = new XdmSequenceType();
        labelType.ItemKind = XdmItemKind.Atomic;
        labelType.AtomicType = XdmAtomicType.String;
        labelType.Occurrence = XdmOccurrence.ExactlyOne;
        XdmSequence labelValue = Converter.ApplyXdmFunctionConversion(arguments[1], labelType, StaticContext);
        XdmAtomicValue? labelAtomic = Sequences.ReadXdmSequenceItem(labelValue, 0).AtomicValue;
        if (labelAtomic == null)
        {
            throw new System.InvalidOperationException("The trace label did not convert to one string.");
        }

        IXPathTraceReceiver? receiver = context.TraceReceiver;
        if (receiver != null)
        {
            receiver.ReceiveXPathTrace(Converter.ReadXdmAtomicStringValue(labelAtomic), result, CopyXPathTraceFunctionRange(Range));
        }

        return result;
    }

    internal SourceRange CopyXPathTraceFunctionRange(SourceRange source)
    {
        SourceRange result = new SourceRange();
        result.Start = CopyXPathTraceFunctionLocation(source.Start);
        result.End = CopyXPathTraceFunctionLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXPathTraceFunctionLocation(SourceLocation? source)
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
