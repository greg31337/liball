namespace fix;

/// <summary>Defines standard FIX administrative message type identifiers.</summary>
public static class FixAdminMessageTypes
{
    /// <summary>Heartbeat message.</summary>
    public const string Heartbeat = "0";
    /// <summary>Test request message.</summary>
    public const string TestRequest = "1";
    /// <summary>Resend request message.</summary>
    public const string ResendRequest = "2";
    /// <summary>Session reject message.</summary>
    public const string Reject = "3";
    /// <summary>Sequence reset message.</summary>
    public const string SequenceReset = "4";
    /// <summary>Logout message.</summary>
    public const string Logout = "5";
    /// <summary>Logon message.</summary>
    public const string Logon = "A";
    /// <summary>Business message reject.</summary>
    public const string BusinessMessageReject = "j";
}
