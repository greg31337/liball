namespace pop3;

/// <summary>Describes one step in a POP3 SASL exchange.</summary>
public class Pop3SaslStep
{
    /// <summary>The outcome of the exchange step.</summary>
    public Pop3SaslStepStatus Status;

    /// <summary>The decoded challenge or response bytes for the exchange step.</summary>
    public byte[] Data = new byte[0];

    /// <summary>Indicates whether the step carries data, including an explicitly present empty value.</summary>
    public bool HasData;

    /// <summary>The authenticated identity, or <c>null</c> until the exchange succeeds.</summary>
    public string? AuthenticatedIdentity;
}
