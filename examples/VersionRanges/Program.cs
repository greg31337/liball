using version;

var versions = new NodeSemVerOperations();
string[] candidates =
{
    "2.0.0",
    "1.9.0",
    "1.4.0-beta.1",
    "1.3.2",
    "1.3.0",
    "1.2.9",
    "1.10.0"
};

// The caret range permits compatible 1.x releases; prereleases are excluded by default.
const string range = "^1.3.0";
Console.WriteLine($"Selecting a release for {range}");
Console.WriteLine("Sorted by version precedence (not by text):");
foreach (string candidate in versions.Sort(candidates))
{
    Console.WriteLine($"  {candidate,-16} {(versions.Satisfies(candidate, range) ? "allowed" : "excluded")}");
}

Console.WriteLine($"Best matching release: {versions.MaxSatisfying(candidates, range)}");
Console.WriteLine($"Next patch: {versions.IncrementPatch("1.10.0")}");
Console.WriteLine($"Next minor: {versions.IncrementMinor("1.10.0")}");
Console.WriteLine($"Next major: {versions.IncrementMajor("1.10.0")}");

// Build metadata identifies a build without affecting version precedence.
var semver = new SemVerOperations();
Console.WriteLine(
    $"Build metadata preserves precedence: {semver.ComparePrecedence("1.0.0+build.1", "1.0.0+build.2") == 0}");
Console.WriteLine($"Strict parser accepts '01.2.3': {semver.TryParseStrict("01.2.3").success}");
