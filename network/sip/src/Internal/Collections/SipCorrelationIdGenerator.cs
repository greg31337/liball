namespace sip;

internal class SipCorrelationIdGenerator
{
    object synchronization;
    SipRandomTokenGenerator tokenGenerator;

    internal SipCorrelationIdGenerator(ISipRandomSource randomSource)
    {
        synchronization = new object();
        tokenGenerator = new SipRandomTokenGenerator(randomSource);
    }

    internal string CreateCorrelationId(string? prefix)
    {
        if (prefix == null)
        {
            throw new System.ArgumentNullException("prefix");
        }

        if (prefix.Length > 0 && !SipAscii.IsToken(prefix))
        {
            throw new System.ArgumentException("The correlation identifier prefix must contain only SIP token characters.", "prefix");
        }

        lock (synchronization)
        {
            return prefix + tokenGenerator.CreateHexToken(16);
        }
    }
}
