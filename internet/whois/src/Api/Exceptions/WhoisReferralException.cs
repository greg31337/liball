namespace whois;

/// <summary>
/// Reports invalid, disallowed, looping, unsupported, or excessive referral traversal.
/// </summary>
public class WhoisReferralException : System.Exception
{
    /// <summary>
    /// Contains the referral that caused traversal failure, or <c>null</c> when none was selected.
    /// </summary>
    public WhoisReferral? Referral;

    /// <summary>
    /// Contains a stable reason describing why referral traversal stopped.
    /// </summary>
    public string Reason;

    /// <summary>
    /// Contains every lookup hop completed before referral failure.
    /// </summary>
    public WhoisLookupResult PartialResult;

    /// <summary>
    /// Initializes a referral exception with rejected referral and partial lookup context.
    /// </summary>
    /// <param name="message">The referral traversal failure message.</param>
    /// <param name="referral">The referral that caused failure, or <c>null</c>.</param>
    /// <param name="reason">The stable traversal failure reason.</param>
    /// <param name="partialResult">The partial ordered lookup result.</param>
    /// <param name="innerException">The underlying referral exception, or <c>null</c>.</param>
    public WhoisReferralException(string message, WhoisReferral? referral, string reason, WhoisLookupResult partialResult, System.Exception? innerException) : base(message, innerException)
    {
        Referral = referral;
        Reason = reason;
        PartialResult = partialResult;
    }
}
