namespace dns;

/// <summary>Creates and verifies cryptographically protected DNS Cookies.</summary>
public interface IDnsCookieService
{
    /// <summary>Creates a new unpredictable eight-octet Client Cookie.</summary>
    /// <returns>A request-form DNS Cookie containing only a Client Cookie.</returns>
    DnsEdnsCookie CreateClientCookie();

    /// <summary>Creates an interoperable version-one Server Cookie.</summary>
    /// <param name="requestCookie">The request Cookie containing an eight-octet Client Cookie.</param>
    /// <param name="clientAddress">The four-octet IPv4 or sixteen-octet IPv6 client address.</param>
    /// <param name="serverSecret">The sixteen-octet SipHash server secret.</param>
    /// <param name="timestamp">The unsigned Unix timestamp placed in the Server Cookie.</param>
    /// <returns>A response-form DNS Cookie containing the copied Client Cookie and new Server Cookie.</returns>
    DnsEdnsCookie CreateServerCookie(DnsEdnsCookie requestCookie, byte[] clientAddress, byte[] serverSecret, uint timestamp);

    /// <summary>Verifies an interoperable version-one Server Cookie and its timestamp.</summary>
    /// <param name="cookie">The Cookie containing the Client Cookie and Server Cookie.</param>
    /// <param name="clientAddress">The four-octet IPv4 or sixteen-octet IPv6 client address.</param>
    /// <param name="serverSecret">The sixteen-octet SipHash server secret.</param>
    /// <param name="currentTimestamp">The current unsigned Unix timestamp.</param>
    /// <param name="maximumPastAgeSeconds">The accepted age in seconds.</param>
    /// <param name="maximumFutureSkewSeconds">The accepted future clock skew in seconds.</param>
    /// <returns><c>true</c> when the cookie MAC and timestamp are valid; otherwise, <c>false</c>.</returns>
    bool VerifyServerCookie(DnsEdnsCookie cookie, byte[] clientAddress, byte[] serverSecret, uint currentTimestamp, int maximumPastAgeSeconds, int maximumFutureSkewSeconds);
}
