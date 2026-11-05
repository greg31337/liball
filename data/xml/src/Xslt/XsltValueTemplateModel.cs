namespace xml;

internal enum XsltValueTemplateKind
{
    Attribute = 0,
    Text = 1,
    Shadow = 2
}

internal enum XsltValueTemplatePartKind
{
    Fixed = 0,
    Expression = 1
}

internal class XsltValueTemplatePart
{
    internal XsltValueTemplatePartKind Kind;
    internal string Value;
    internal int TemplateOffset;
    internal int TemplateLength;
    internal SourceRange Range;
    internal XPathPlan? ExpressionPlan;

    internal XsltValueTemplatePart(XsltValueTemplatePartKind kind, string value, int templateOffset, int templateLength, SourceRange range)
    {
        Kind = kind;
        Value = value;
        TemplateOffset = templateOffset;
        TemplateLength = templateLength;
        Range = range;
        ExpressionPlan = null;
    }
}

internal class XsltValueTemplate
{
    internal XmlNode Node;
    internal XsltValueTemplateKind Kind;
    internal string Source;
    internal XsltValueTemplatePart[] Parts;
    internal XsltStaticContext? StaticContext;
    internal SourceRange? Range;

    internal XsltValueTemplate(XmlNode node, XsltValueTemplateKind kind, string source, XsltValueTemplatePart[] parts)
    {
        Node = node;
        Kind = kind;
        Source = source;
        Parts = parts;
        StaticContext = null;
        Range = node.Range;
    }
}
