namespace sdp;

internal class SdpUtf8DecodeResult
{
    internal bool Success = false;
    internal bool HasByteOrderMark = false;
    internal bool ByteOrderMarkNotAllowed = false;
    internal bool InvalidUtf8 = false;
    internal bool NulCharacterNotAllowed = false;
    internal bool ControlCharacterNotAllowed = false;
    internal int ByteOffset = -1;
    internal int CharacterOffset = -1;
    internal int BytesConsumed = 0;
    internal int CharacterCount = 0;
    internal string Text = string.Empty;
    internal string DiagnosticCode = string.Empty;
}
