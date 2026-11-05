namespace http;

/// <summary>
/// Represents the result of evaluating HTTP request preconditions.
/// </summary>
public class HttpPreconditionDecision
{
    /// <summary>
    /// The precondition outcome.
    /// </summary>
    public HttpPreconditionOutcome Outcome;

    /// <summary>
    /// The evaluation context, or <c>null</c> when no evaluation was performed.
    /// </summary>
    public HttpPreconditionContext? Context;

    /// <summary>
    /// The field name of the condition that determined the outcome, or <c>null</c> when none did.
    /// </summary>
    public string? DecidingCondition;

    /// <summary>
    /// Indicates that an HTTP status code is associated with the outcome.
    /// </summary>
    public bool HasStatusCode;

    /// <summary>
    /// The HTTP status code associated with the outcome.
    /// </summary>
    public int StatusCode;

    /// <summary>
    /// Indicates that Range processing remains permitted.
    /// </summary>
    public bool ApplyRange;
}
