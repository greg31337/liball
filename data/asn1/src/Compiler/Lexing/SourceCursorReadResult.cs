namespace asn1;

internal class SourceCursorReadResult
{
    internal SourceCursorStatus Status;
    internal int ScalarValue;
    internal int Utf16Length;
    internal bool IsLineBreak;
    internal string Text = "";
    internal AsnSourcePosition Start = new AsnSourcePosition();
    internal AsnSourcePosition End = new AsnSourcePosition();
}
