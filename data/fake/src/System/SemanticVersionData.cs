namespace fake;

internal static class SemanticVersionData
{
    internal const string SpecificationVersion = "2.0.0";
    internal const string Source = "Semantic Versioning 2.0.0";
    internal const string License = "Creative Commons Attribution 3.0";

    internal static readonly string[] PreReleases = new string[]
    {
        "0", "alpha", "alpha.1", "beta", "beta.2", "rc.1",
        "x.7.z.92", "x-y-z.--"
    };

    internal static readonly string[] BuildMetadata = new string[]
    {
        "001", "build.7", "exp.sha.5114f85", "linux-x64",
        "20130313144700", "21AF26D3----117B344092BD"
    };

    internal static readonly string[] InvalidVersions = new string[]
    {
        "01.2.3", "1.2", "1.2.3-alpha..1", "1.2.3-alpha_beta",
        "1.2.3-01", "1.2.3.4"
    };
}
