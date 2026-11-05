namespace sip;

/// <summary>Holds nonce lifetime, binding, and key-rotation inputs.</summary>
public class SipNonceOptions
{
    /// <summary>The nonce lifetime in seconds.</summary>
    public int LifetimeSeconds = 300;
    /// <summary>The accepted clock skew in seconds.</summary>
    public int ClockSkewSeconds = 30;
    /// <summary>The realm bound to generated nonces.</summary>
    public string? Realm;
    /// <summary>The current nonce authentication key.</summary>
    public byte[] CurrentKey = new byte[0];
    /// <summary>The preceding nonce authentication key.</summary>
    public byte[] PreviousKey = new byte[0];
    /// <summary>The maximum replay entries.</summary>
    public int MaxReplayEntries = SipDefaults.MaxQueueCount;
}
