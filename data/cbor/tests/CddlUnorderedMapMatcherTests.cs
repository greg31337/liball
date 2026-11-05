using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CddlUnorderedMapMatcherTests
{
    [Test]
    public void MatchesStructMembersWithoutDependingOnMapOrder()
    {
        string source = "root = {name: #3, count: #0}";
        AssertValid(
            source,
            Map(Entry("count", Unsigned(2)), Entry("name", Text("item"))));
        AssertValid(source, Map(Entry("name", Text("item")),
            Entry("count", Unsigned(2))));

        CddlValidationResult wrong = Validate(
            source,
            Map(Entry("name", Unsigned(1)), Entry("count", Unsigned(2))));

        Assert.Multiple(() =>
        {
            Assert.That(wrong.IsValid, Is.False);
            Assert.That(FlattenPaths(wrong.Issues[0]),
                Does.Contain("$[\"name\"]"));
            Assert.That(FindIssue(wrong.Issues[0],
                    CddlValidationCode.TypeMismatch), Is.Not.Null);
        });
    }

    [Test]
    public void ReportsMissingUnknownAndDuplicateStructMembers()
    {
        CddlValidationResult missing = Validate(
            "root = {name: #3}",
            Map());
        CddlValidationResult unknown = Validate(
            "root = {name: #3}",
            Map(Entry("name", Text("item")), Entry("extra", Unsigned(1))));
        CddlValidationResult duplicate = Validate(
            "root = {name: #3}",
            Map(Entry("name", Text("first")), Entry("name", Text("second"))));

        Assert.Multiple(() =>
        {
            Assert.That(missing.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.MissingMember));
            Assert.That(FlattenPaths(missing.Issues[0]),
                Does.Contain("$[\"name\"]"));
            Assert.That(unknown.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.UnknownMember));
            Assert.That(unknown.Issues[0].InstancePath,
                Is.EqualTo("$[\"extra\"]"));
            Assert.That(duplicate.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.DuplicateMember));
            Assert.That(duplicate.Issues[0].InstancePath,
                Is.EqualTo("$[\"name\"]"));
        });
    }

    [Test]
    public void MatchesArbitraryKeysDuplicatesAndOccurrenceBounds()
    {
        string source = "root = {2*3 #3 => #0}";
        AssertValid(
            source,
            Map(Entry("a", Unsigned(1)), Entry("b", Unsigned(2))));
        AssertValid(
            source,
            Map(
                Entry("a", Unsigned(1)),
                Entry("a", Unsigned(2)),
                Entry("b", Unsigned(3))));
        AssertInvalid(source, Map(Entry("a", Unsigned(1))));
        AssertInvalid(
            source,
            Map(
                Entry("a", Unsigned(1)),
                Entry("b", Unsigned(2)),
                Entry("c", Unsigned(3)),
                Entry("d", Unsigned(4))));
    }

    [Test]
    public void BacktracksAcrossUnorderedEntryAssignments()
    {
        string source = "root = {#3 => 1, \"x\" => 1}";
        AssertValid(
            source,
            Map(Entry("x", Unsigned(1)), Entry("y", Unsigned(1))));

        string choices =
            "root = {#3 => 1, \"x\" => 2 // \"x\" => 1, #3 => 2}";
        AssertValid(
            choices,
            Map(Entry("x", Unsigned(1)), Entry("y", Unsigned(2))));
    }

    [Test]
    public void MatchesIncludedTablesOptionalMembersAndGroupChoices()
    {
        string source =
            "root = {common, details}\n"
            + "common = (id: #0, ? note: #3)\n"
            + "details = (left: 1 // right: 2)";
        AssertValid(
            source,
            Map(Entry("right", Unsigned(2)), Entry("id", Unsigned(7))));
        AssertValid(
            source,
            Map(
                Entry("note", Text("ok")),
                Entry("left", Unsigned(1)),
                Entry("id", Unsigned(7))));
        AssertInvalid(
            source,
            Map(Entry("left", Unsigned(2)), Entry("id", Unsigned(7))));
    }

    [Test]
    public void CommitsAGroupChoiceOnlyWhenACutKeyMatches()
    {
        CborValue value = Map(Entry("x", Text("value")));
        AssertValid("root = {#3 => #0 // \"x\" => #3}", value);
        AssertInvalid("root = {#3 ^ => #0 // \"x\" => #3}", value);
        AssertValid(
            "root = {#0 ^ => #0 // \"x\" => #3}",
            value);
    }

    [Test]
    public void SupportsRepeatedNullableGroupsWithoutInfiniteLoops()
    {
        AssertValid(
            "root = {* empty, value: 1}\nempty = ()",
            Map(Entry("value", Unsigned(1))));
        AssertValid("root = {+ empty}\nempty = ()", Map());
        AssertValid("root = {2* empty}\nempty = ()", Map());

        CddlValidationResult bounded = Validate(
            "root = {1000000* empty}\nempty = ()",
            Map(),
            new CddlValidationOptions { MaxAlternatives = 10 });

        Assert.Multiple(() =>
        {
            Assert.That(bounded.IsValid, Is.False);
            Assert.That(bounded.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
        });
    }

    [Test]
    public void MatchesGenericMapGroups()
    {
        AssertValid(
            "root = {pair<1, 2>}\npair<a, b> = (first: a, second: b)",
            Map(Entry("second", Unsigned(2)), Entry("first", Unsigned(1))));
    }

    [Test]
    public void PreservesNestedMapPathsAndStableDiagnostics()
    {
        string source = "root = {outer: {inner: 1}}";
        CborValue value = Map(Entry("outer", Map(Entry("inner", Unsigned(2)))));
        CddlValidationResult first = Validate(source, value);
        CddlValidationResult second = Validate(source, value);
        CddlValidationIssue? literal = FindIssue(
            first.Issues[0],
            CddlValidationCode.LiteralMismatch);

        Assert.Multiple(() =>
        {
            Assert.That(literal, Is.Not.Null);
            Assert.That(literal!.InstancePath,
                Is.EqualTo("$[\"outer\"][\"inner\"]"));
            Assert.That(second.Issues[0].Code,
                Is.EqualTo(first.Issues[0].Code));
            Assert.That(FlattenPaths(second.Issues[0]),
                Is.EqualTo(FlattenPaths(first.Issues[0])));
        });
    }

    [Test]
    public void AppliesAlternativeStepAndNestingBudgetsToMaps()
    {
        CddlValidationResult alternatives = Validate(
            "root = {1 => 1 // 2 => 2}",
            Map(IntegerEntry(2, Unsigned(2))),
            new CddlValidationOptions { MaxAlternatives = 1 });
        CddlValidationResult steps = Validate(
            "root = {name: 1}",
            Map(Entry("name", Unsigned(1))),
            new CddlValidationOptions { MaxSteps = 2 });
        CddlValidationResult nesting = Validate(
            "root = {one: {two: {three: 1}}}",
            Map(Entry("one", Map(Entry("two", Map(
                Entry("three", Unsigned(1))))))),
            new CddlValidationOptions { MaxNestingDepth = 2 });

        Assert.Multiple(() =>
        {
            Assert.That(alternatives.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
            Assert.That(steps.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
            Assert.That(nesting.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
        });
    }

    private static CddlValidationIssue? FindIssue(
        CddlValidationIssue issue,
        CddlValidationCode code)
    {
        if (issue.Code == code)
        {
            return issue;
        }
        int index = 0;
        while (index < issue.Details.Length)
        {
            CddlValidationIssue? found = FindIssue(issue.Details[index], code);
            if (found != null)
            {
                return found;
            }
            index = index + 1;
        }
        return null;
    }

    private static string[] FlattenPaths(CddlValidationIssue issue)
    {
        List<string> paths = new List<string>();
        AddPaths(issue, paths);
        return paths.ToArray();
    }

    private static void AddPaths(
        CddlValidationIssue issue,
        List<string> paths)
    {
        paths.Add(issue.InstancePath);
        int index = 0;
        while (index < issue.Details.Length)
        {
            AddPaths(issue.Details[index], paths);
            index = index + 1;
        }
    }

    private static void AssertValid(string source, CborValue value)
    {
        CddlValidationResult result = Validate(source, value);
        Assert.That(result.IsValid, Is.True,
            result.Issues.Length == 0 ? "" : result.Issues[0].Message);
        Assert.That(result.Issues, Is.Empty);
    }

    private static void AssertInvalid(string source, CborValue value)
    {
        CddlValidationResult result = Validate(source, value);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues, Is.Not.Empty);
    }

    private static CddlValidationResult Validate(
        string source,
        CborValue value,
        CddlValidationOptions? options = null)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "unordered.cddl" });
        Assert.That(parse.IsSuccess, Is.True,
            parse.Diagnostics.Length == 0 ? "" : parse.Diagnostics[0].Message);
        CddlCompileResult compile = new CddlCompiler().TryCompileSchema(
            parse.Document!,
            new CddlCompileOptions { IncludeStandardPrelude = false });
        Assert.That(compile.IsSuccess, Is.True,
            compile.Diagnostics.Length == 0
                ? ""
                : compile.Diagnostics[0].Message);
        return new CddlValidator().ValidateCbor(
            compile.Schema!,
            value,
            options ?? new CddlValidationOptions());
    }

    private static CborMapEntry Entry(string key, CborValue value)
    {
        return new CborMapEntry { Key = Text(key), Value = value };
    }

    private static CborMapEntry IntegerEntry(int key, CborValue value)
    {
        return new CborMapEntry { Key = Unsigned(key), Value = value };
    }

    private static CborValue Unsigned(int value)
    {
        return new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private static CborValue Text(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = value
        };
    }

    private static CborValue Map(params CborMapEntry[] entries)
    {
        return new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = new List<CborMapEntry>(entries)
        };
    }
}
