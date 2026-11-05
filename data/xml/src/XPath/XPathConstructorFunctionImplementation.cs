namespace xml;

internal class XPathConstructorFunctionImplementation : IXPathFunction
{
    internal XdmAtomicType TargetType;
    internal CompiledXmlSchemaSimpleType? SchemaType;
    internal XPathStaticContext StaticContext;
    internal XPathAtomicTypeCaster Caster;
    internal XdmSequenceOperations Sequences;
    internal bool ListType;

    internal XPathConstructorFunctionImplementation(XdmAtomicType targetType, CompiledXmlSchemaSimpleType? schemaType, XPathStaticContext staticContext, bool listType)
    {
        TargetType = targetType;
        SchemaType = schemaType;
        StaticContext = staticContext;
        Caster = new XPathAtomicTypeCaster();
        Sequences = new XdmSequenceOperations();
        ListType = listType;
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmSequence argument = arguments[0];
        if (argument.Count == 0)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(argument, 0);
        if (item.AtomicValue == null)
        {
            throw new System.InvalidOperationException("A constructor function argument was not converted to an atomic value.");
        }

        if (ListType)
        {
            string lexical = new XdmStringDatatype().CollapseXdmStringWhitespace(new XdmTypeConverter().ReadXdmAtomicStringValue(item.AtomicValue));
            if (lexical.Length == 0)
            {
                throw CreateXPathListConstructorError();
            }

            string[] tokens = lexical.Split(' ');
            XdmSequenceBuilder builder = new XdmSequenceBuilder();
            int index = 0;
            while (index < tokens.Length)
            {
                XdmAtomicValue source = new XdmStringDatatype().ParseXdmStringAtomic(tokens[index], XdmAtomicType.String);
                builder.AddXdmAtomicValue(Caster.RequireXPathAtomicCastResult(Caster.TryCastXPathBuiltInAtomic(source, TargetType, StaticContext)));
                index = index + 1;
            }

            return builder.BuildImmutableXdmSequence();
        }

        XdmConversionResult converted = SchemaType == null ? Caster.TryCastXPathBuiltInAtomic(item.AtomicValue, TargetType, StaticContext) : Caster.TryCastXPathSchemaAtomic(item.AtomicValue, SchemaType, StaticContext);
        return Sequences.CreateXdmAtomicSequence(Caster.RequireXPathAtomicCastResult(converted));
    }

    internal XPathDynamicException CreateXPathListConstructorError()
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XPathErrorCodes.FORG0001;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Error;
        diagnostic.Message = "A list constructor requires at least one valid list item.";
        return new XPathDynamicException(diagnostic.Message, diagnostic, null);
    }
}
