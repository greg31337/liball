using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CddlRecursiveMatcherTests
{
    [Test]
    public void MatchesDirectMutualAndGenericProductiveRecursion()
    {
        string direct = "root = node\nnode = 0 / [node]";
        AssertValid(direct, Unsigned(0));
        AssertValid(direct, Array(Unsigned(0)));
        AssertValid(direct, Array(Array(Array(Unsigned(0)))));
        AssertInvalid(direct, Array(Unsigned(1)));

        string mutual =
            "root = first\n"
            + "first = 1 / [second]\n"
            + "second = 2 / [first]";
        AssertValid(mutual, Array(Unsigned(2)));
        AssertValid(mutual, Array(Array(Unsigned(1))));

        string generic =
            "root = chain<7>\n"
            + "chain<t> = t / [chain<t>]";
        AssertValid(generic, Array(Array(Unsigned(7))));
    }

    [Test]
    public void AllowsBaseAlternativesAfterAZeroProgressCycle()
    {
        AssertValid("root = root / 1", Unsigned(1));
        AssertValid(
            "root = first\nfirst = second / 1\nsecond = first",
            Unsigned(1));

        CddlValidationResult mismatch = Validate(
            "root = root / 1",
            Unsigned(2));

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.IsValid, Is.False);
            Assert.That(FlattenCodes(mismatch.Issues[0]),
                Does.Contain(CddlValidationCode.RecursionLimit));
            Assert.That(FlattenCodes(mismatch.Issues[0]),
                Does.Contain(CddlValidationCode.LiteralMismatch));
        });
    }

    [Test]
    public void MatchesProductiveRecursiveArrayGroups()
    {
        string source =
            "root = [items]\n"
            + "items = (1, ? items)";
        AssertValid(source, Array(Unsigned(1)));
        AssertValid(
            source,
            Array(Unsigned(1), Unsigned(1), Unsigned(1)));
        AssertInvalid(source, Array());
        AssertInvalid(source, Array(Unsigned(1), Unsigned(2)));
    }

    [Test]
    public void MatchesProductiveRecursiveMapGroups()
    {
        string source =
            "root = {members}\n"
            + "members = (value: 1, ? members)";
        AssertValid(source, Map(Entry("value", Unsigned(1))));
        AssertValid(
            source,
            Map(
                Entry("value", Unsigned(1)),
                Entry("value", Unsigned(1)),
                Entry("value", Unsigned(1))));
        AssertInvalid(source, Map());
        AssertInvalid(
            source,
            Map(Entry("value", Unsigned(1)), Entry("value", Unsigned(2))));
    }

    [Test]
    public void RejectsCyclicInstanceGraphsWithoutUnboundedRecursion()
    {
        CborValue cycle = new CborValue { Kind = CborValueKind.Array };
        cycle.ArrayItems = new List<CborValue> { cycle };

        CddlValidationResult result = Validate(
            "root = node\nnode = 0 / [node]",
            cycle);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(FlattenCodes(result.Issues[0]),
                Does.Contain(CddlValidationCode.RecursionLimit));
        });
    }

    [Test]
    public void ReusesCompletedMemoEntriesByRuleAndInstanceIdentity()
    {
        string source =
            "root = [leaf, leaf]\n"
            + "leaf = 1 / [leaf]";
        CborValue shared = Unsigned(1);
        CddlValidationOptions options = new CddlValidationOptions
        {
            MaxMemoEntries = 2
        };
        CddlValidationResult reused = Validate(
            source,
            Array(shared, shared),
            options);
        CddlValidationResult distinct = Validate(
            source,
            Array(Unsigned(1), Unsigned(1)),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(reused.IsValid, Is.True);
            Assert.That(reused.Issues, Is.Empty);
            Assert.That(distinct.IsValid, Is.False);
            Assert.That(distinct.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
        });
    }

    [Test]
    public void AppliesMemoStepAndDepthLimitsDuringRecursiveDescent()
    {
        string source = "root = node\nnode = 0 / [node]";
        CborValue deep = Array(Array(Array(Unsigned(0))));
        CddlValidationResult memo = Validate(
            source,
            deep,
            new CddlValidationOptions { MaxMemoEntries = 2 });
        CddlValidationResult steps = Validate(
            source,
            deep,
            new CddlValidationOptions { MaxSteps = 5 });
        CddlValidationResult depth = Validate(
            source,
            deep,
            new CddlValidationOptions { MaxNestingDepth = 4 });

        Assert.Multiple(() =>
        {
            Assert.That(memo.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
            Assert.That(steps.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
            Assert.That(depth.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
        });
    }

    [Test]
    public void PreservesNestedPathsThroughRecursiveRules()
    {
        CddlValidationResult result = Validate(
            "root = node\nnode = 0 / [node]",
            Array(Array(Unsigned(2))));
        CddlValidationIssue? literal = FindIssue(
            result.Issues[0],
            CddlValidationCode.LiteralMismatch,
            "$[0][0]");

        Assert.Multiple(() =>
        {
            Assert.That(literal, Is.Not.Null);
            Assert.That(literal!.RuleName, Is.EqualTo("node"));
            Assert.That(literal.SchemaPath, Does.Contain("/rule(node)"));
        });
    }

    private static CddlValidationIssue? FindIssue(
        CddlValidationIssue issue,
        CddlValidationCode code,
        string path)
    {
        if (issue.Code == code && issue.InstancePath == path)
        {
            return issue;
        }
        int index = 0;
        while (index < issue.Details.Length)
        {
            CddlValidationIssue? found = FindIssue(
                issue.Details[index],
                code,
                path);
            if (found != null)
            {
                return found;
            }
            index = index + 1;
        }
        return null;
    }

    private static CddlValidationCode[] FlattenCodes(
        CddlValidationIssue issue)
    {
        List<CddlValidationCode> codes = new List<CddlValidationCode>();
        AddCodes(issue, codes);
        return codes.ToArray();
    }

    private static void AddCodes(
        CddlValidationIssue issue,
        List<CddlValidationCode> codes)
    {
        codes.Add(issue.Code);
        int index = 0;
        while (index < issue.Details.Length)
        {
            AddCodes(issue.Details[index], codes);
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
            new CddlParseOptions { SourceName = "recursive.cddl" });
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

    private static CborValue Array(params CborValue[] values)
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = new List<CborValue>(values)
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
