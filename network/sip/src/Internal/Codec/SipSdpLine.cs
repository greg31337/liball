namespace sip;

internal class SipSdpLine
{
    internal string Text;
    internal int Offset;

    internal SipSdpLine(string text, int offset)
    {
        Text = text;
        Offset = offset;
    }
}
