namespace sip;

internal class SipMimeParseState
{
    internal bool Success;
    internal SipMimeEntity? Entity;
    internal int ErrorOffset = -1;
    internal string? ErrorMessage;
}
