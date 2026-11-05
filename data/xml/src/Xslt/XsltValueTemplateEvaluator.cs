using System.Text;

namespace xml;

internal class XsltValueTemplateEvaluator
{
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;

    internal XsltValueTemplateEvaluator()
    {
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
    }

    internal string EvaluateXsltValueTemplate(XsltValueTemplate template, XPathDynamicContext context)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < template.Parts.Length)
        {
            XsltValueTemplatePart part = template.Parts[index];
            if (part.Kind == XsltValueTemplatePartKind.Fixed)
            {
                result.Append(part.Value);
            }
            else if (part.ExpressionPlan != null)
            {
                XdmSequence value = new XPathRuntime(part.ExpressionPlan, context).EvaluateXPathPlan();
                AppendXsltValueTemplateSequence(result, value, IsXsltBackwardsCompatibleAttribute(template));
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal void AppendXsltValueTemplateSequence(StringBuilder result, XdmSequence value, bool firstOnly)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(value);
        int index = 0;
        while (index < atomized.Count)
        {
            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(atomized, index);
            if (item.AtomicValue != null)
            {
                if (index > 0)
                {
                    result.Append(' ');
                }

                result.Append(Converter.ReadXdmAtomicStringValue(item.AtomicValue));
                if (firstOnly)
                {
                    return;
                }
            }

            index = index + 1;
        }
    }

    internal bool IsXsltBackwardsCompatibleAttribute(XsltValueTemplate template)
    {
        return template.Kind == XsltValueTemplateKind.Attribute && template.StaticContext != null && template.StaticContext.XPathCompatibilityMode == XPathCompatibilityMode.BackwardsCompatible;
    }
}
