namespace xml;

/// <summary>Evaluates compiled XPath plans with an isolated dynamic runtime.</summary>
public class XPathEvaluator : IXPathEvaluator
{
    /// <summary>Creates an XPath evaluator.</summary>
    public XPathEvaluator()
    {
    }

    /// <summary>Evaluates a compiled XPath expression.</summary>
    public XPathEvaluationResult EvaluateXPath(XPathCompiledExpression expression, XPathDynamicContext context)
    {
        if (expression == null)
        {
            throw new System.ArgumentException("A compiled XPath plan is required.", "expression");
        }

        XPathPlan? plan = expression.CompiledPlan == null ? expression.Plan as XPathPlan : expression.CompiledPlan;
        if (plan == null)
        {
            throw new System.ArgumentException("A compiled XPath plan is required.", "expression");
        }

        XPathRuntime runtime = new XPathRuntime(plan, context);
        XPathEvaluationResult result = new XPathEvaluationResult();
        result.Expression = expression;
        result.DynamicContext = runtime.Context.Snapshot;
        result.Value = runtime.EvaluateXPathPlan();
        result.Diagnostics = [];
        XPathTraceCollector? collector = runtime.Context.Snapshot.TraceReceiver as XPathTraceCollector;
        result.TraceEvents = collector == null ? [] : collector.SnapshotXPathTraceEvents();
        result.ErrorCode = null;
        result.Succeeded = true;
        return result;
    }
}
