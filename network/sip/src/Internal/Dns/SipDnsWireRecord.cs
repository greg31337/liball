namespace sip;

internal class SipDnsWireRecord
{
    internal string? Name;
    internal SipDnsRecordKind Kind;
    internal int Section;
    internal long TtlSeconds;
    internal string? Address;
    internal string? Target;
    internal int Priority = -1;
    internal int Weight = -1;
    internal int Port = -1;
    internal int NaptrOrder = -1;
    internal int NaptrPreference = -1;
    internal string? NaptrFlags;
    internal string? NaptrServices;
    internal string? NaptrRegexp;
    internal string? NaptrReplacement;
}
