namespace fake;

internal static class DeveloperCompletionData
{
    internal static readonly string[] PackageNames =
    {
        "example-core",
        "example-parser",
        "fixture-tools",
        "sample-client",
        "sample-runtime",
        "synthetic-data",
        "test-support"
    };

    internal static readonly string[] FrameworkNames =
    {
        ".NET",
        "Java",
        "Node.js",
        "Python",
        "Ruby"
    };

    internal static readonly string[][] FrameworkVersions =
    {
        new string[] { "6.0.0", "7.0.0", "8.0.0" },
        new string[] { "17.0.0", "21.0.0", "22.0.0" },
        new string[] { "18.0.0", "20.0.0", "22.0.0" },
        new string[] { "3.10.0", "3.11.0", "3.12.0" },
        new string[] { "3.1.0", "3.2.0", "3.3.0" }
    };

    internal static readonly string[][] TargetFrameworks =
    {
        new string[] { "net6.0", "net7.0", "net8.0" },
        new string[] { "java17", "java21", "java22" },
        new string[] { "node18", "node20", "node22" },
        new string[] { "py310", "py311", "py312" },
        new string[] { "ruby31", "ruby32", "ruby33" }
    };

    internal static readonly string[] PackageManagers =
    {
        "NuGet",
        "Maven",
        "npm",
        "PyPI",
        "RubyGems"
    };
}
