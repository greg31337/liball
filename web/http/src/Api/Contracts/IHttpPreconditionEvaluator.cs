namespace http;

/// <summary>
/// Evaluates HTTP conditional request fields.
/// </summary>
public interface IHttpPreconditionEvaluator
{
    /// <summary>Performs a strong entity-tag comparison.</summary>
    /// <param name="left">The first tag.</param>
    /// <param name="right">The second tag.</param>
    /// <returns><c>true</c> when the tags match strongly.</returns>
    bool CompareStrongTag(HttpEntityTag left, HttpEntityTag right);

    /// <summary>Performs a weak entity-tag comparison.</summary>
    /// <param name="left">The first tag.</param>
    /// <param name="right">The second tag.</param>
    /// <returns><c>true</c> when the tags match weakly.</returns>
    bool CompareWeakTag(HttpEntityTag left, HttpEntityTag right);

    /// <summary>
    /// Evaluates the preconditions carried by a request.
    /// </summary>
    /// <param name="request">The conditional request to evaluate.</param>
    /// <param name="context">The current representation and recipient context.</param>
    /// <returns>The precondition decision.</returns>
    HttpPreconditionDecision EvaluatePreconditions(HttpRequest request, HttpPreconditionContext context);

    /// <summary>Evaluates If-Range for a range request.</summary>
    /// <param name="request">The range request.</param>
    /// <param name="context">The current validator context.</param>
    /// <returns>The range precondition decision.</returns>
    HttpPreconditionDecision EvaluateIfRange(HttpRequest request, HttpPreconditionContext context);
}
