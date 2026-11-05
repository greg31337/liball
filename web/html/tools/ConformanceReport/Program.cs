using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace UniversalHtml.ConformanceReport;

internal class CountSummary
{
    public int Cases { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
}

internal sealed class ReportGroup : CountSummary
{
    public string Id { get; set; }
}

internal sealed class SkipDetail
{
    public string ReasonCode { get; set; }
    public string Reason { get; set; }
    public int Count { get; set; }
    public string[] CaseIds { get; set; }
}

internal sealed class SuiteReport : CountSummary
{
    public string Id { get; set; }
    public string Fixture { get; set; }
    public string FixtureRevision { get; set; }
    public string Scope { get; set; }
    public string[] TestEntryPoints { get; set; }
    public string Status { get; set; }
    public ReportGroup[] Groups { get; set; }
    public SkipDetail[] SkipDetails { get; set; }
}

internal sealed class ReportDocument
{
    public int SchemaVersion { get; set; }
    public string ReportDate { get; set; }
    public string LibraryVersion { get; set; }
    public string TargetFramework { get; set; }
    public string FixtureManifest { get; set; }
    public string VerificationCommand { get; set; }
    public string TestCommand { get; set; }
    public string Status { get; set; }
    public CountSummary Totals { get; set; }
    public SuiteReport[] Suites { get; set; }
}

internal sealed class FixtureInventory
{
    public FixtureInventory(int count, IEnumerable<string> skipCaseIds)
    {
        Count = count;
        SkipCaseIds = new HashSet<string>(skipCaseIds,
            StringComparer.Ordinal);
    }

    public int Count { get; }
    public HashSet<string> SkipCaseIds { get; }
}

internal sealed class SuiteExpectation
{
    public SuiteExpectation(string id, string fixture, bool manifestPinned,
        Func<string, FixtureInventory> inventory)
    {
        Id = id;
        Fixture = fixture;
        ManifestPinned = manifestPinned;
        Inventory = inventory;
    }

    public string Id { get; }
    public string Fixture { get; }
    public bool ManifestPinned { get; }
    public Func<string, FixtureInventory> Inventory { get; }
}

internal static class Program
{
    static readonly SuiteExpectation[] Expectations =
    [
        new SuiteExpectation("html5lib-tokenizer",
            "tests/data/html5lib/tokenizer", true, InventoryTokenizer),
        new SuiteExpectation("html5lib-tree-construction",
            "tests/data/html5lib/tree-construction", true, InventoryTrees),
        new SuiteExpectation("whatwg-html-entities",
            "tests/data/entities/named-character-references.tsv", true,
            InventoryLines),
        new SuiteExpectation("wpt-selectors",
            "tests/data/selectors/wpt-selector-subset.tsv", true,
            InventoryLines),
        new SuiteExpectation("commonmark-0.31.2-generation",
            "tests/data/commonmark/generation.json", true,
            InventoryJsonArray),
        new SuiteExpectation("gfm-0.29.0-generation",
            "tests/data/gfm/generation.json", true, InventoryJsonArray),
        new SuiteExpectation("sanitizer-adversarial",
            "tests/data/sanitization/adversarial.json", false,
            InventorySanitizer)
    ];

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length > 1 || args.Length == 1 && args[0] != "--verify")
            {
                throw new ArgumentException(
                    "Usage: ConformanceReport [--verify]");
            }

            string repositoryRoot = FindRepositoryRoot();
            string reportPath = Path.Combine(repositoryRoot,
                "conformance-report.json");
            ReportDocument report = JsonSerializer.Deserialize<ReportDocument>(
                File.ReadAllText(reportPath), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            ValidateReport(repositoryRoot, report);
            foreach (SuiteReport suite in report.Suites)
            {
                Console.WriteLine(suite.Id + ": cases=" + suite.Cases
                    + ", passed=" + suite.Passed + ", failed=" + suite.Failed
                    + ", skipped=" + suite.Skipped);
            }

            Console.WriteLine("total: cases=" + report.Totals.Cases
                + ", passed=" + report.Totals.Passed + ", failed="
                + report.Totals.Failed + ", skipped="
                + report.Totals.Skipped);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("ConformanceReport: "
                + exception.Message);
            return 1;
        }
    }

    static void ValidateReport(string repositoryRoot, ReportDocument report)
    {
        if (report == null || report.SchemaVersion != 1
            || report.Totals == null || report.Suites == null)
        {
            throw new InvalidDataException(
                "The conformance report is missing or unsupported.");
        }

        if (!DateOnly.TryParseExact(report.ReportDate, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            || string.IsNullOrWhiteSpace(report.LibraryVersion)
            || string.IsNullOrWhiteSpace(report.TargetFramework)
            || string.IsNullOrWhiteSpace(report.VerificationCommand)
            || string.IsNullOrWhiteSpace(report.TestCommand))
        {
            throw new InvalidDataException(
                "The conformance report metadata is incomplete.");
        }

        if (report.FixtureManifest != "tests/data/fixture-manifest.json")
        {
            throw new InvalidDataException(
                "The fixture manifest path is not canonical.");
        }

        Dictionary<string, string> revisions = ReadManifestRevisions(
            repositoryRoot, report.FixtureManifest);
        Dictionary<string, SuiteReport> suites = new Dictionary<string, SuiteReport>(
            StringComparer.Ordinal);
        foreach (SuiteReport suite in report.Suites)
        {
            if (suite == null || string.IsNullOrWhiteSpace(suite.Id)
                || !suites.TryAdd(suite.Id, suite))
            {
                throw new InvalidDataException(
                    "The report has a missing or duplicate suite id.");
            }
        }

        if (suites.Count != Expectations.Length)
        {
            throw new InvalidDataException(
                "The report must contain exactly the required suites.");
        }

        CountSummary calculated = new CountSummary();
        foreach (SuiteExpectation expectation in Expectations)
        {
            if (!suites.TryGetValue(expectation.Id, out SuiteReport suite))
            {
                throw new InvalidDataException(
                    "Missing conformance suite: " + expectation.Id);
            }

            ValidateSuite(repositoryRoot, suite, expectation, revisions);
            calculated.Cases += suite.Cases;
            calculated.Passed += suite.Passed;
            calculated.Failed += suite.Failed;
            calculated.Skipped += suite.Skipped;
        }

        ValidateSameCounts("report totals", report.Totals, calculated);
        ValidateStatus("report", report.Status, report.Totals);
    }

    static void ValidateSuite(string repositoryRoot, SuiteReport suite,
        SuiteExpectation expectation, Dictionary<string, string> revisions)
    {
        if (suite.Fixture != expectation.Fixture
            || string.IsNullOrWhiteSpace(suite.FixtureRevision)
            || string.IsNullOrWhiteSpace(suite.Scope)
            || suite.TestEntryPoints == null
            || suite.TestEntryPoints.Length == 0
            || suite.TestEntryPoints.Any(string.IsNullOrWhiteSpace)
            || suite.Groups == null || suite.SkipDetails == null)
        {
            throw new InvalidDataException(
                "Suite metadata is incomplete for " + suite.Id + ".");
        }

        string fixturePath = ResolveWithinRoot(repositoryRoot, suite.Fixture);
        FixtureInventory inventory = expectation.Inventory(fixturePath);
        if (suite.Cases != inventory.Count)
        {
            throw new InvalidDataException("Fixture case count mismatch for "
                + suite.Id + ": expected " + suite.Cases + ", found "
                + inventory.Count + ".");
        }

        ValidateCounts(suite.Id, suite);
        ValidateStatus(suite.Id, suite.Status, suite);
        ValidateGroups(suite);
        ValidateSkips(suite, inventory.SkipCaseIds);

        if (expectation.ManifestPinned)
        {
            if (!revisions.TryGetValue(suite.Id, out string revision)
                || revision != suite.FixtureRevision)
            {
                throw new InvalidDataException(
                    "Fixture revision mismatch for " + suite.Id + ".");
            }
        }
        else
        {
            string prefix = "sha256:";
            string actual = Convert.ToHexStringLower(
                SHA256.HashData(File.ReadAllBytes(fixturePath)));
            if (!suite.FixtureRevision.StartsWith(prefix,
                StringComparison.Ordinal)
                || suite.FixtureRevision.Substring(prefix.Length) != actual)
            {
                throw new InvalidDataException(
                    "Local fixture checksum mismatch for " + suite.Id + ".");
            }
        }
    }

    static void ValidateGroups(SuiteReport suite)
    {
        if (suite.Groups.Length == 0)
        {
            return;
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        CountSummary calculated = new CountSummary();
        foreach (ReportGroup group in suite.Groups)
        {
            if (group == null || string.IsNullOrWhiteSpace(group.Id)
                || !ids.Add(group.Id))
            {
                throw new InvalidDataException(
                    "Missing or duplicate group in " + suite.Id + ".");
            }

            ValidateCounts(suite.Id + "/" + group.Id, group);
            calculated.Cases += group.Cases;
            calculated.Passed += group.Passed;
            calculated.Failed += group.Failed;
            calculated.Skipped += group.Skipped;
        }

        ValidateSameCounts("groups for " + suite.Id, suite, calculated);
    }

    static void ValidateSkips(SuiteReport suite,
        HashSet<string> eligibleSkipIds)
    {
        HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);
        int count = 0;
        foreach (SkipDetail detail in suite.SkipDetails)
        {
            if (detail == null || string.IsNullOrWhiteSpace(detail.ReasonCode)
                || string.IsNullOrWhiteSpace(detail.Reason)
                || detail.Count < 1 || detail.CaseIds == null
                || detail.CaseIds.Length != detail.Count)
            {
                throw new InvalidDataException(
                    "Incomplete skip detail in " + suite.Id + ".");
            }

            foreach (string caseId in detail.CaseIds)
            {
                if (string.IsNullOrWhiteSpace(caseId)
                    || !reported.Add(caseId))
                {
                    throw new InvalidDataException(
                        "Missing or duplicate skipped case in " + suite.Id
                        + ".");
                }
            }

            count += detail.Count;
        }

        if (count != suite.Skipped || !reported.SetEquals(eligibleSkipIds))
        {
            throw new InvalidDataException(
                "Skipped cases are not fully accounted for in " + suite.Id
                + ".");
        }
    }

    static void ValidateCounts(string name, CountSummary summary)
    {
        if (summary.Cases < 0 || summary.Passed < 0 || summary.Failed < 0
            || summary.Skipped < 0
            || summary.Passed + summary.Failed + summary.Skipped
                != summary.Cases)
        {
            throw new InvalidDataException(
                "Invalid pass/fail/skip accounting for " + name + ".");
        }
    }

    static void ValidateSameCounts(string name, CountSummary expected,
        CountSummary actual)
    {
        if (expected.Cases != actual.Cases
            || expected.Passed != actual.Passed
            || expected.Failed != actual.Failed
            || expected.Skipped != actual.Skipped)
        {
            throw new InvalidDataException(
                "Count rollup mismatch for " + name + ".");
        }
    }

    static void ValidateStatus(string name, string status,
        CountSummary summary)
    {
        string expected = summary.Failed == 0 && summary.Skipped == 0
            ? "pass" : "partial";
        if (status != expected)
        {
            throw new InvalidDataException("Status for " + name
                + " must be " + expected + ".");
        }
    }

    static Dictionary<string, string> ReadManifestRevisions(
        string repositoryRoot, string relativePath)
    {
        string path = ResolveWithinRoot(repositoryRoot, relativePath);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        Dictionary<string, string> revisions = new Dictionary<string, string>(
            StringComparer.Ordinal);
        foreach (JsonElement subset in document.RootElement
            .GetProperty("subsets").EnumerateArray())
        {
            revisions.Add(subset.GetProperty("id").GetString(),
                subset.GetProperty("revision").GetString());
        }

        return revisions;
    }

    static FixtureInventory InventoryTokenizer(string path)
    {
        int count = 0;
        List<string> skipped = new List<string>();
        foreach (string file in Directory.GetFiles(path, "*.test")
            .OrderBy(value => value, StringComparer.Ordinal))
        {
            using JsonDocument document = JsonDocument.Parse(
                File.ReadAllText(file));
            JsonElement root = document.RootElement;
            bool xmlViolation = root.TryGetProperty("xmlViolationTests",
                out JsonElement tests);
            if (!xmlViolation)
            {
                tests = root.GetProperty("tests");
            }

            foreach (JsonElement test in tests.EnumerateArray())
            {
                string description = test.GetProperty("description")
                    .GetString();
                string[] states = test.TryGetProperty("initialStates",
                    out JsonElement stateValues)
                    ? stateValues.EnumerateArray().Select(value =>
                        value.GetString()).ToArray()
                    : ["Data state"];
                foreach (string state in states)
                {
                    count++;
                    if (xmlViolation)
                    {
                        skipped.Add(Path.GetFileName(file) + "#"
                            + description + "[" + state + "]");
                    }
                }
            }
        }

        return new FixtureInventory(count, skipped);
    }

    static FixtureInventory InventoryTrees(string path)
    {
        int count = 0;
        List<string> skipped = new List<string>();
        foreach (string file in Directory.GetFiles(path, "*.dat",
            SearchOption.AllDirectories).OrderBy(value => value,
                StringComparer.Ordinal))
        {
            string relative = Path.GetRelativePath(path, file)
                .Replace('\\', '/');
            bool scripted = relative.StartsWith("scripted/",
                StringComparison.Ordinal);
            string content = File.ReadAllText(file);
            int start = content.IndexOf("#data\n", StringComparison.Ordinal);
            int caseNumber = 0;
            while (start >= 0)
            {
                int document = content.IndexOf("#document\n", start + 6,
                    StringComparison.Ordinal);
                if (document < 0)
                {
                    throw new InvalidDataException(
                        "Malformed tree fixture " + relative + ".");
                }

                int next = content.IndexOf("\n\n#data\n", document,
                    StringComparison.Ordinal);
                string metadata = content.Substring(start,
                    document - start);
                if (scripted)
                {
                    count++;
                    skipped.Add(relative + "#"
                        + caseNumber.ToString(CultureInfo.InvariantCulture)
                        + "[scripting=true]");
                }
                else
                {
                    bool fixedScripting = metadata.Contains("#script-on",
                        StringComparison.Ordinal)
                        || metadata.Contains("#script-off",
                            StringComparison.Ordinal);
                    count += fixedScripting ? 1 : 2;
                }

                caseNumber++;
                start = next < 0 ? -1 : next + 2;
            }
        }

        return new FixtureInventory(count, skipped);
    }

    static FixtureInventory InventoryLines(string path)
    {
        int count = File.ReadLines(path).Count(line =>
            !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'));
        return new FixtureInventory(count, Array.Empty<string>());
    }

    static FixtureInventory InventoryJsonArray(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("Expected a JSON array: " + path);
        }

        return new FixtureInventory(document.RootElement.GetArrayLength(),
            Array.Empty<string>());
    }

    static FixtureInventory InventorySanitizer(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        int count = document.RootElement.GetProperty("cases").GetArrayLength();
        return new FixtureInventory(count, Array.Empty<string>());
    }

    static string FindRepositoryRoot()
    {
        DirectoryInfo directory = new DirectoryInfo(
            Directory.GetCurrentDirectory());
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName,
                "conformance-report.json"))
                && File.Exists(Path.Combine(directory.FullName, "todo.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find the repository root.");
    }

    static string ResolveWithinRoot(string repositoryRoot, string relative)
    {
        string path = Path.GetFullPath(Path.Combine(repositoryRoot, relative));
        string prefix = repositoryRoot.EndsWith(Path.DirectorySeparatorChar)
            ? repositoryRoot : repositoryRoot + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.Ordinal)
            || !File.Exists(path) && !Directory.Exists(path))
        {
            throw new InvalidDataException(
                "Invalid repository path: " + relative);
        }

        return path;
    }
}
