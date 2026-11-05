namespace xml;

internal class XPathEnvironmentFunctionImplementation : IXPathFunction
{
    internal string LocalName;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmFactory Factory;

    internal XPathEnvironmentFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext)
    {
        LocalName = localName;
        Range = range;
        StaticContext = staticContext;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Factory = new XdmFactory();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        string[] names = context.EnvironmentNames == null ? [] : context.EnvironmentNames;
        string[] values = context.EnvironmentValues == null ? [] : context.EnvironmentValues;
        if (names.Length != values.Length)
        {
            throw new System.InvalidOperationException("The XPath environment snapshot is inconsistent.");
        }

        if (LocalName == "available-environment-variables")
        {
            XdmSequenceBuilder builder = new XdmSequenceBuilder();
            int index = 0;
            while (index < names.Length)
            {
                builder.AddXdmAtomicValue(Factory.CreateXdmStringAtomicValue(names[index], XdmAtomicType.String));
                index = index + 1;
            }

            return builder.BuildImmutableXdmSequence();
        }

        string requested = ReadXPathEnvironmentName(arguments[0]);
        int position = 0;
        while (position < names.Length)
        {
            if (names[position] == requested)
            {
                XdmAtomicValue value = Factory.CreateXdmStringAtomicValue(values[position], XdmAtomicType.String);
                return Sequences.CreateXdmAtomicSequence(value);
            }

            position = position + 1;
        }

        return Sequences.CreateEmptyXdmSequence();
    }

    internal string ReadXPathEnvironmentName(XdmSequence supplied)
    {
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.String;
        expected.Occurrence = XdmOccurrence.ExactlyOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(converted, 0);
        XdmAtomicValue? value = item.AtomicValue;
        if (value == null)
        {
            throw new System.InvalidOperationException("A converted environment variable name is not atomic.");
        }

        return Converter.ReadXdmAtomicStringValue(value);
    }
}
