namespace sip;

/// <summary>
/// Provides stable names for SIP transaction, dialog, and usage timers.
/// </summary>
public static class SipTimerNames
{
    /// <summary>The INVITE client request retransmission timer.</summary>
    public const string TimerA = "A";

    /// <summary>The INVITE client transaction timeout timer.</summary>
    public const string TimerB = "B";

    /// <summary>The proxy INVITE transaction timeout timer.</summary>
    public const string TimerC = "C";

    /// <summary>The INVITE client completed-state timer.</summary>
    public const string TimerD = "D";

    /// <summary>The non-INVITE client request retransmission timer.</summary>
    public const string TimerE = "E";

    /// <summary>The non-INVITE client transaction timeout timer.</summary>
    public const string TimerF = "F";

    /// <summary>The INVITE server response retransmission timer.</summary>
    public const string TimerG = "G";

    /// <summary>The INVITE server ACK wait timer.</summary>
    public const string TimerH = "H";

    /// <summary>The INVITE server confirmed-state timer.</summary>
    public const string TimerI = "I";

    /// <summary>The non-INVITE server completed-state timer.</summary>
    public const string TimerJ = "J";

    /// <summary>The non-INVITE client completed-state timer.</summary>
    public const string TimerK = "K";

    /// <summary>The accepted INVITE server transaction timer.</summary>
    public const string TimerL = "L";

    /// <summary>The accepted INVITE client transaction timer.</summary>
    public const string TimerM = "M";

    /// <summary>The reliable provisional response retransmission timer.</summary>
    public const string ReliableProvisional = "ReliableProvisional";

    /// <summary>The reliable provisional response timeout timer.</summary>
    public const string ReliableProvisionalTimeout = "ReliableProvisionalTimeout";

    /// <summary>The dialog session refresh timer.</summary>
    public const string SessionRefresh = "SessionRefresh";

    /// <summary>The dialog session expiry timer.</summary>
    public const string SessionExpires = "SessionExpires";

    /// <summary>The registration refresh timer.</summary>
    public const string RegistrationRefresh = "RegistrationRefresh";

    /// <summary>The subscription refresh timer.</summary>
    public const string SubscriptionRefresh = "SubscriptionRefresh";
}
