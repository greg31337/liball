namespace email;

internal class EmailEncodingAnalysis
{
    long sourceByteCount;
    long textByteCount;
    long highBitByteCount;
    long nullByteCount;
    long controlByteCount;
    long maximumLineByteCount;
    bool canonicalLineBreaks;
    bool textualMediaType;
    bool mostlyText;

    internal EmailEncodingAnalysis(long sourceByteCount, long textByteCount, long highBitByteCount, long nullByteCount, long controlByteCount, long maximumLineByteCount, bool canonicalLineBreaks, bool textualMediaType, bool mostlyText)
    {
        this.sourceByteCount = sourceByteCount;
        this.textByteCount = textByteCount;
        this.highBitByteCount = highBitByteCount;
        this.nullByteCount = nullByteCount;
        this.controlByteCount = controlByteCount;
        this.maximumLineByteCount = maximumLineByteCount;
        this.canonicalLineBreaks = canonicalLineBreaks;
        this.textualMediaType = textualMediaType;
        this.mostlyText = mostlyText;
    }

    internal long GetSourceByteCount()
    {
        return sourceByteCount;
    }

    internal long GetTextByteCount()
    {
        return textByteCount;
    }

    internal long GetHighBitByteCount()
    {
        return highBitByteCount;
    }

    internal long GetNullByteCount()
    {
        return nullByteCount;
    }

    internal long GetControlByteCount()
    {
        return controlByteCount;
    }

    internal long GetMaximumLineByteCount()
    {
        return maximumLineByteCount;
    }

    internal bool HasCanonicalLineBreaks()
    {
        return canonicalLineBreaks;
    }

    internal bool IsTextualMediaType()
    {
        return textualMediaType;
    }

    internal bool IsMostlyText()
    {
        return mostlyText;
    }
}
