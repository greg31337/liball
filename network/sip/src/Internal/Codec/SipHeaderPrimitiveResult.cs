namespace sip;

internal class SipHeaderPrimitiveResult
{
    internal bool Success;
    internal string? Text;
    internal string[] TextValues = new string[0];
    internal string? Host;
    internal int Port = -1;
    internal int Number = -1;
    internal System.DateTime Date;
    internal SipUriParameter? Parameter;
    internal SipUriParameter[] Parameters = new SipUriParameter[0];
    internal SipNameAddress? NameAddress;
    internal SipMediaType? MediaType;
    internal SipWarningValue? WarningValue;
    internal int ErrorOffset = -1;
    internal string? ErrorMessage;
}
