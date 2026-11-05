namespace asn1;

internal class XerCharacterValueResult
{
    internal bool Success;
    internal AsnRuntimeValue? Value;
    internal string Text = "";
    internal string Prefix = "";
    internal string NamespaceUri = "";
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
    internal bool LimitExceeded;
}
