namespace sip;

internal class SipSubscriptionClientContext
{
    internal SipSubscriptionRequest? Request;
    internal SipMessage? Message;
    internal SipMessage? Response;
    internal string? BaseId;
    internal string? SubscriptionId;
    internal string? CallId;
    internal string? LocalTag;
    internal string? RemoteTag;
    internal int CSeq;
    internal int LastNotifyCSeq = -1;
    internal long Version;
    internal SipSubscriptionState State;
    internal SipDialogId? DialogId;
    internal int ExpiresSeconds = -1;
    internal long RefreshAtMilliseconds = -1;
    internal long ExpiresAtMilliseconds = -1;
    internal long RetryAtMilliseconds = -1;
    internal int RetryAfterSeconds = -1;
    internal bool Forked;
    internal bool SubscribeFinal;
    internal bool NotificationFinal;
    internal bool Terminating;
    internal int AuthenticationAttempts;
}
