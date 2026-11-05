namespace whois;

/// <summary>
/// Defines extraction of referral values from a generic parsed response.
/// </summary>
public interface IWhoisReferralParser
{
    /// <summary>
    /// Parses every recognized, unsupported, or malformed referral in source order.
    /// </summary>
    /// <param name="response">The generic parsed response.</param>
    /// <returns>All referral metadata in response order.</returns>
    WhoisReferral[] ParseReferrals(WhoisParsedResponse response);
}
