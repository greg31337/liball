namespace sip;

/// <summary>
/// Provides standard SIP option-tag tokens.
/// </summary>
public static class SipOptionTags
{
    /// <summary>The reliable provisional response option tag.</summary>
    public const string ReliableProvisional = "100rel";

    /// <summary>The early-dialog termination response option tag.</summary>
    public const string EarlyDialogTermination = "199";

    /// <summary>The answer-mode preference option tag.</summary>
    public const string AnswerMode = "answermode";

    /// <summary>The early-session content disposition option tag.</summary>
    public const string EarlySession = "early-session";

    /// <summary>The resource-list event option tag.</summary>
    public const string EventList = "eventlist";

    /// <summary>The connected-identity From update option tag.</summary>
    public const string FromChange = "from-change";

    /// <summary>The globally routable user-agent URI option tag.</summary>
    public const string Gruu = "gruu";

    /// <summary>The HTTP location acquisition profile option tag.</summary>
    public const string GeolocationHttp = "geolocation-http";

    /// <summary>The SIP location acquisition profile option tag.</summary>
    public const string GeolocationSip = "geolocation-sip";

    /// <summary>The History-Info option tag.</summary>
    public const string HistoryInfo = "histinfo";

    /// <summary>The Interactive Connectivity Establishment option tag.</summary>
    public const string Ice = "ice";

    /// <summary>The Join header option tag.</summary>
    public const string Join = "join";

    /// <summary>The no-implicit-subscription REFER option tag.</summary>
    public const string NoReferSub = "norefersub";

    /// <summary>The SIP Outbound option tag.</summary>
    public const string Outbound = "outbound";

    /// <summary>The Path extension option tag.</summary>
    public const string Path = "path";

    /// <summary>The precondition framework option tag.</summary>
    public const string Precondition = "precondition";

    /// <summary>The caller preferences option tag.</summary>
    public const string Preferences = "pref";

    /// <summary>The privacy option tag.</summary>
    public const string Privacy = "privacy";

    /// <summary>The recipient-list INVITE option tag.</summary>
    public const string RecipientListInvite = "recipient-list-invite";

    /// <summary>The recipient-list SUBSCRIBE option tag.</summary>
    public const string RecipientListSubscribe = "recipient-list-subscribe";

    /// <summary>The Replaces header option tag.</summary>
    public const string Replaces = "replaces";

    /// <summary>The resource priority option tag.</summary>
    public const string ResourcePriority = "resource-priority";

    /// <summary>The security agreement option tag.</summary>
    public const string SecurityAgreement = "sec-agree";

    /// <summary>The SIP recording option tag.</summary>
    public const string SipRecording = "siprec";

    /// <summary>The target-dialog option tag.</summary>
    public const string TargetDialog = "tdialog";

    /// <summary>The session timer option tag.</summary>
    public const string Timer = "timer";

    /// <summary>The trickle ICE option tag.</summary>
    public const string TrickleIce = "trickle-ice";
}
