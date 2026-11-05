namespace http;

/// <summary>
/// Evaluates HTTP request preconditions and range validators.
/// </summary>
public class HttpPreconditionEvaluator : IHttpPreconditionEvaluator
{
    /// <summary>Creates an HTTP precondition evaluator.</summary>
    public HttpPreconditionEvaluator()
    {
    }

    /// <summary>Performs a strong entity-tag comparison.</summary>
    /// <param name="left">The first tag.</param>
    /// <param name="right">The second tag.</param>
    /// <returns><c>true</c> when the tags match strongly.</returns>
    public bool CompareStrongTag(HttpEntityTag left, HttpEntityTag right)
    {
        return EntityTagComparer.CompareStrong(left, right);
    }

    /// <summary>Performs a weak entity-tag comparison.</summary>
    /// <param name="left">The first tag.</param>
    /// <param name="right">The second tag.</param>
    /// <returns><c>true</c> when the tags match weakly.</returns>
    public bool CompareWeakTag(HttpEntityTag left, HttpEntityTag right)
    {
        return EntityTagComparer.CompareWeak(left, right);
    }

    /// <summary>Evaluates all request preconditions in protocol order.</summary>
    /// <param name="request">The conditional request to evaluate.</param>
    /// <param name="context">The current representation and recipient context.</param>
    /// <returns>The precondition decision.</returns>
    public HttpPreconditionDecision EvaluatePreconditions(
        HttpRequest request,
        HttpPreconditionContext context)
    {
        return PreconditionSemantics.EvaluatePreconditions(request, context);
    }

    /// <summary>Evaluates If-Range for a range request.</summary>
    /// <param name="request">The range request.</param>
    /// <param name="context">The current validator context.</param>
    /// <returns>The range precondition decision.</returns>
    public HttpPreconditionDecision EvaluateIfRange(
        HttpRequest request,
        HttpPreconditionContext context)
    {
        return PreconditionSemantics.EvaluateIfRange(request, context);
    }
}
