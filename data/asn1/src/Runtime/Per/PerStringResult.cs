namespace asn1;

internal class PerStringResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal string Text = "";
    internal byte[] Data = [];
    internal int CharacterCount;
    internal int BitsPerCharacter;
    internal int BitCount;
    internal int PaddingBitCount;
    internal bool CompactedAlphabet;
    internal bool ExtensionValue;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
