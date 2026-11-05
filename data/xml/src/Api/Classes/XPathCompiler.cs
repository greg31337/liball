namespace xml;

/// <summary>Compiles reusable XPath expressions into immutable evaluation plans.</summary>
public class XPathCompiler : IXPathCompiler
{
    /// <summary>Creates an XPath compiler.</summary>
    public XPathCompiler()
    {
    }

    /// <summary>Compiles an XPath expression.</summary>
    public XPathCompiledExpression CompileXPath(string expression, XPathStaticContext context)
    {
        XPathPlanCompiler compiler = new XPathPlanCompiler(context);
        XPathPlan plan = compiler.CompileXPathSource(expression, context.BaseUri);
        XPathCompiledExpression result = new XPathCompiledExpression();
        result.Source = expression;
        result.StaticContext = context;
        result.Plan = plan;
        result.CompiledPlan = plan;
        result.Range = compiler.CopyXPathRange(plan.Range);
        result.Diagnostics = [];
        result.IsThreadSafe = true;
        return result;
    }
}
