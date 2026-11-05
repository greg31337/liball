namespace xml;

internal class XPathPartialFunctionImplementation : IXPathFunction
{
    internal XdmFunctionItem Original;
    internal bool[] Placeholders;
    internal XdmSequence?[] FixedValues;
    internal XPathDynamicContext DynamicContext;
    internal XPathStaticContext StaticContext;

    internal XPathPartialFunctionImplementation(XdmFunctionItem original, bool[] placeholders, XdmSequence?[] fixedValues, XPathDynamicContext dynamicContext, XPathStaticContext staticContext)
    {
        Original = original;
        Placeholders = new bool[placeholders.Length];
        FixedValues = new XdmSequence?[fixedValues.Length];
        XdmSequenceOperations sequences = new XdmSequenceOperations();
        int index = 0;
        while (index < placeholders.Length)
        {
            Placeholders[index] = placeholders[index];
            XdmSequence? value = fixedValues[index];
            FixedValues[index] = value == null ? null : sequences.FreezeXdmSequenceStorage(value);
            index = index + 1;
        }

        DynamicContext = new XPathFunctionContextFactory().CopyXPathFunctionDynamicComponents(dynamicContext);
        StaticContext = staticContext;
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmSequence[] combined = new XdmSequence[Placeholders.Length];
        int argumentIndex = 0;
        int index = 0;
        while (index < Placeholders.Length)
        {
            if (Placeholders[index])
            {
                combined[index] = arguments[argumentIndex];
                argumentIndex = argumentIndex + 1;
            }
            else
            {
                XdmSequence? value = FixedValues[index];
                if (value == null)
                {
                    throw new System.InvalidOperationException("A fixed partial-function argument is absent.");
                }

                combined[index] = value;
            }

            index = index + 1;
        }

        return new XdmFunctionOperations().InvokeXdmFunctionItem(Original, combined, DynamicContext, StaticContext);
    }
}
