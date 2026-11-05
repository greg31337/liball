namespace fake;

internal static class GitData
{
    internal const string Source = "Project-authored fictional Git fixtures and reviewed SPDX identifiers";
    internal const string License = "project-license";

    internal static readonly string[] Branches = new string[]
    {
        "chore/update-fixtures",
        "docs/api-guide",
        "feature/add-search",
        "feature/sample-export",
        "fix/cache-timeout",
        "fix/example-validation",
        "refactor/order-service",
        "release/1.2.3"
    };

    internal static readonly string[] CommitMessages = new string[]
    {
        "Add sample search support",
        "Document example API usage",
        "Fix synthetic cache timeout",
        "Prepare synthetic release",
        "Refactor sample order flow",
        "Remove obsolete fixture alias",
        "Update reviewed fixtures",
        "Validate example configuration"
    };

    internal static readonly string[] AuthorNames = new string[]
    {
        "Alex Example",
        "Casey Sample",
        "Jordan Fixture",
        "Morgan Test",
        "Riley Demo",
        "Taylor Mock"
    };

    internal static readonly string[] AuthorEmails = new string[]
    {
        "alex@example.com",
        "casey@example.com",
        "jordan@example.com",
        "morgan@example.com",
        "riley@example.com",
        "taylor@example.com"
    };

    internal static readonly string[] RepositoryOwners = new string[]
    {
        "demo-lab",
        "example-org",
        "fixture-team",
        "sample-group",
        "synthetic-code"
    };

    internal static readonly string[] RepositoryNames = new string[]
    {
        "api-fixtures",
        "example-library",
        "sample-client",
        "sample-service",
        "test-toolkit"
    };

    internal static readonly string[] PackageNames = new string[]
    {
        "example-core",
        "example-parser",
        "fixture-tools",
        "sample-client",
        "sample-runtime",
        "synthetic-data",
        "test-support"
    };

    internal static readonly string[] ProgrammingLanguages = new string[]
    {
        "C#",
        "Go",
        "Java",
        "JavaScript",
        "Kotlin",
        "Python",
        "Ruby",
        "Rust",
        "TypeScript"
    };

    internal static readonly string[] LicenseIdentifiers = new string[]
    {
        "Apache-2.0",
        "BSD-2-Clause",
        "BSD-3-Clause",
        "ISC",
        "MIT",
        "MPL-2.0",
        "Unlicense"
    };
}
