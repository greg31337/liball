namespace dns;

/// <summary>Signs and verifies DNS messages using caller-supplied TSIG keys.</summary>
public class DnsTsigService : IDnsTsigService
{
    /// <summary>Creates a stateless TSIG service.</summary>
    public DnsTsigService()
    {
    }

    /// <summary>Creates a TSIG message authentication code.</summary>
    /// <param name="message">The message to authenticate.</param>
    /// <param name="key">The shared key and TSIG algorithm.</param>
    /// <param name="priorMac">The preceding MAC for a response or transfer chain.</param>
    /// <param name="now">The signing time.</param>
    /// <returns>The TSIG data, MAC, chain state, and diagnostics.</returns>
    public DnsTsigResult SignTsig(
        DnsMessage message,
        DnsTsigKey key,
        byte[] priorMac,
        DnsClockValue now)
    {
        return TsigOperations.Sign(message, key, priorMac, now);
    }

    /// <summary>Verifies a TSIG message authentication code.</summary>
    /// <param name="message">The authenticated message.</param>
    /// <param name="key">The expected shared key and TSIG algorithm.</param>
    /// <param name="priorMac">The preceding MAC for a response or transfer chain.</param>
    /// <param name="now">The verification time.</param>
    /// <returns>The authentication state, error, chain state, and diagnostics.</returns>
    public DnsTsigResult VerifyTsig(
        DnsMessage message,
        DnsTsigKey key,
        byte[] priorMac,
        DnsClockValue now)
    {
        return TsigOperations.Verify(message, key, priorMac, now);
    }
}
