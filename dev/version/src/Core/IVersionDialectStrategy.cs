namespace version;

internal interface IVersionDialectStrategy
{
    public VersionParseResult Parse(string text);

    public int Compare(UniversalVersion left, UniversalVersion right, VersionComparisonMode mode);

    public string FormatNormalized(UniversalVersion version);

    public string FormatCanonical(UniversalVersion version);
}
