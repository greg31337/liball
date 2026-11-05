namespace sip;

internal class SipSubscriptionServerContext
{
    internal SipSubscriptionRequest? Request;
    internal SipSubscriptionServerOptions? Options;
    internal SipMessage? Subscribe;
    internal SipMessage? Response;
    internal SipMessage? OutstandingNotification;
    internal string? SubscriptionId;
    internal string? CallId;
    internal string? LocalTag;
    internal string? RemoteTag;
    internal string? EventPackage;
    internal string? EventId;
    internal SipDialogId? DialogId;
    internal SipSubscriptionState State;
    internal int LastSubscribeCSeq;
    internal int NotifyCSeq;
    internal long Version;
    internal int ExpiresSeconds;
    internal long ExpiresAtMilliseconds = -1;
    internal string? Reason;
    internal int RetryAfterSeconds = -1;
    internal bool Forked;
    internal bool PendingExpiry;
}
