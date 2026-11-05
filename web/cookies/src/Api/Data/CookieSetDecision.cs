namespace cookies;

/// <summary>
/// Describes a cookie acceptance decision and its normalized candidate.
/// </summary>
public class CookieSetDecision
{
    /// <summary>
    /// The storage disposition selected by the policy.
    /// </summary>
    public CookieSetStatus Status;

    /// <summary>
    /// The primary stable reason for the decision.
    /// </summary>
    public CookieErrorCode ErrorCode;

    /// <summary>
    /// The normalized storage candidate, when one was produced.
    /// </summary>
    public StoredCookie? NormalizedCookie;

    /// <summary>
    /// The diagnostics produced while making the decision.
    /// </summary>
    public CookieIssue[] Issues = [];
}
