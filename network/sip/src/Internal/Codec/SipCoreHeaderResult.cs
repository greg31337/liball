namespace sip;

internal class SipCoreHeaderResult
{
    internal bool Success;
    internal int ContentLength = -1;
    internal SipMediaType? MediaType;
    internal SipMediaType[] MediaTypes = new SipMediaType[0];
    internal SipContentDisposition? ContentDisposition;
    internal string[] Methods = new string[0];
    internal string[] OptionTags = new string[0];
    internal System.DateTime Date;
    internal bool HasDate;
    internal int ExpiresSeconds = -1;
    internal int MinExpiresSeconds = -1;
    internal SipRetryAfterValue? RetryAfter;
    internal SipWarningValue[] WarningValues = new SipWarningValue[0];
    internal SipProductValue[] ProductValues = new SipProductValue[0];
    internal int ErrorOffset = -1;
    internal string? ErrorMessage;
}
