namespace jmap;

/// <summary>
/// Represents use of a capability that is missing or unavailable for an account or method.
/// </summary>
public class JmapCapabilityException : System.Exception
{
    /// <summary>The capability URI associated with the failure.</summary>
    public string? Capability;

    /// <summary>The method name associated with the failure, or null when not method-specific.</summary>
    public string? MethodName;

    /// <summary>The account identifier associated with the failure, or null when not account-specific.</summary>
    public string? AccountId;

    /// <summary>Creates a capability exception with capability, method, and account context.</summary>
    /// <param name="message">The diagnostic failure message.</param>
    /// <param name="capability">The capability URI associated with the failure.</param>
    /// <param name="methodName">The method name associated with the failure, or null.</param>
    /// <param name="accountId">The account identifier associated with the failure, or null.</param>
    public JmapCapabilityException(string message, string? capability, string? methodName, string? accountId) : base(message)
    {
        Capability = capability;
        MethodName = methodName;
        AccountId = accountId;
    }
}
