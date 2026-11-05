namespace asn1;

internal class XerChoiceSelection
{
    internal bool Success;
    internal AsnFieldDescriptor? Descriptor;
    internal AsnRuntimeValue? Value;
    internal int Index = -1;
    internal string ErrorMessage = "";
}
