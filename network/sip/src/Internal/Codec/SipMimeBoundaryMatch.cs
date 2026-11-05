namespace sip;

internal class SipMimeBoundaryMatch
{
    internal bool Found;
    internal bool Closing;
    internal int Start = -1;
    internal int AfterLine = -1;
}
