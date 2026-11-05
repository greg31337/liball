namespace email;

internal class EmailCharsetDecodedText
{
    string text;
    byte[] undecodableBytes;
    bool usedReplacement;

    internal EmailCharsetDecodedText(string text, byte[] undecodableBytes, bool usedReplacement)
    {
        this.text = text;
        this.undecodableBytes = undecodableBytes;
        this.usedReplacement = usedReplacement;
    }

    internal string GetText()
    {
        return text;
    }

    internal byte[] GetUndecodableBytes()
    {
        return undecodableBytes;
    }

    internal bool UsedReplacement()
    {
        return usedReplacement;
    }
}
