namespace sip;

internal class SipMessageLineResult
{
    internal bool Complete;
    internal bool Error;
    internal int LineEnd = -1;
    internal int NextOffset = -1;
    internal int ErrorOffset = -1;
}
