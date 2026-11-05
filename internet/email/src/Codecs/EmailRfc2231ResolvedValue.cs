namespace email;

internal class EmailRfc2231ResolvedValue
{
    byte[] bytes;
    string? charset;
    bool charsetPresent;
    string? language;
    bool languagePresent;
    bool extended;
    bool continued;
    int segmentCount;
    bool fatal;

    internal EmailRfc2231ResolvedValue(byte[] bytes, string? charset, bool charsetPresent, string? language, bool languagePresent, bool extended, bool continued, int segmentCount, bool fatal)
    {
        this.bytes = bytes;
        this.charset = charset;
        this.charsetPresent = charsetPresent;
        this.language = language;
        this.languagePresent = languagePresent;
        this.extended = extended;
        this.continued = continued;
        this.segmentCount = segmentCount;
        this.fatal = fatal;
    }

    internal byte[] GetBytes()
    {
        return bytes;
    }

    internal string? GetCharset()
    {
        return charset;
    }

    internal bool HasCharset()
    {
        return charsetPresent;
    }

    internal string? GetLanguage()
    {
        return language;
    }

    internal bool HasLanguage()
    {
        return languagePresent;
    }

    internal bool IsExtended()
    {
        return extended;
    }

    internal bool IsContinued()
    {
        return continued;
    }

    internal int GetSegmentCount()
    {
        return segmentCount;
    }

    internal bool HasFatalError()
    {
        return fatal;
    }
}
