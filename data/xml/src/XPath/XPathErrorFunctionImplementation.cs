namespace xml;

internal class XPathErrorFunctionImplementation : IXPathFunction
{
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;

    internal XPathErrorFunctionImplementation(SourceRange range, XPathStaticContext staticContext)
    {
        Range = range;
        StaticContext = staticContext;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        string code = XPathErrorCodes.FOER0000;
        XmlName errorName = new XmlName { Prefix = "err", LocalName = code, QualifiedName = "err:" + code, NamespaceUri = XPathNamespaces.Errors };
        if (arguments.Length > 0)
        {
            XdmSequenceType codeType = new XdmSequenceType();
            codeType.ItemKind = XdmItemKind.Atomic;
            codeType.AtomicType = XdmAtomicType.QName;
            codeType.Occurrence = XdmOccurrence.ZeroOrOne;
            XdmSequence convertedCode = Converter.ApplyXdmFunctionConversion(arguments[0], codeType, StaticContext);
            if (convertedCode.Count == 1)
            {
                XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(convertedCode, 0).AtomicValue;
                if (value != null && value.NameValue != null && value.NameValue.LocalName != null)
                {
                    code = value.NameValue.LocalName;
                    errorName = new XdmQNameDatatype().CopyXdmExpandedName(value.NameValue);
                }
            }
        }

        string message = "An error was raised by fn:error.";
        if (arguments.Length > 1)
        {
            XdmSequenceType descriptionType = new XdmSequenceType();
            descriptionType.ItemKind = XdmItemKind.Atomic;
            descriptionType.AtomicType = XdmAtomicType.String;
            descriptionType.Occurrence = XdmOccurrence.ExactlyOne;
            XdmSequence convertedDescription = Converter.ApplyXdmFunctionConversion(arguments[1], descriptionType, StaticContext);
            XdmAtomicValue? description = Sequences.ReadXdmSequenceItem(convertedDescription, 0).AtomicValue;
            if (description != null)
            {
                message = Converter.ReadXdmAtomicStringValue(description);
            }
        }

        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = Range;
        XPathDynamicException failure = new XPathDynamicException(message, diagnostic, Range.Start);
        failure.ErrorName = errorName;
        failure.ErrorValue = arguments.Length > 2 ? arguments[2] : Sequences.CreateEmptyXdmSequence();
        throw failure;
    }
}
