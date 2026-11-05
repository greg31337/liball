using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CddlOrderedGroupMatcherTests
{
    [Test]
    public void MatchesEmptyFixedAndNestedOrderedArrays()
    {
        AssertValid("root = []", Array());
        AssertValid(
            "root = [1, \"two\", [3]]",
            Array(Unsigned(1), Text("two"), Array(Unsigned(3))));
        CddlValidationResult wrong = Validate(
            "root = [1, \"two\", [3]]",
            Array(Unsigned(1), Text("wrong"), Array(Unsigned(3))));
        CddlValidationResult missing = Validate(
            "root = [1, 2]",
            Array(Unsigned(1)));
        CddlValidationResult extra = Validate(
            "root = [1]",
            Array(Unsigned(1), Unsigned(2)));

        Assert.Multiple(() =>
        {
            Assert.That(wrong.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.OccurrenceMismatch));
            Assert.That(FindIssue(wrong.Issues[0],
                    CddlValidationCode.LiteralMismatch)!.InstancePath,
                Is.EqualTo("$[1]"));
            Assert.That(missing.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.OccurrenceMismatch));
            Assert.That(FlattenPaths(missing.Issues[0]),
                Does.Contain("$[1]"));
            Assert.That(extra.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.OccurrenceMismatch));
        });
    }

    [Test]
    public void MatchesEveryOccurrenceFormAndEnforcesBounds()
    {
        string source = "root = [? 0, * (1), + 2, 2*3 3]";
        AssertValid(
            source,
            Array(
                Unsigned(0),
                Unsigned(1),
                Unsigned(1),
                Unsigned(2),
                Unsigned(3),
                Unsigned(3)));
        AssertValid(
            source,
            Array(Unsigned(2), Unsigned(3), Unsigned(3), Unsigned(3)));
        AssertInvalid(
            source,
            Array(Unsigned(3), Unsigned(3)),
            CddlValidationCode.OccurrenceMismatch);
        AssertInvalid(
            source,
            Array(
                Unsigned(2),
                Unsigned(3),
                Unsigned(3),
                Unsigned(3),
                Unsigned(3)),
            CddlValidationCode.OccurrenceMismatch);
    }

    [Test]
    public void BacktracksAcrossAmbiguousAdjacentOccurrences()
    {
        string source = "root = [* (1), + 1, 2]";
        AssertValid(
            source,
            Array(Unsigned(1), Unsigned(1), Unsigned(2)));
        AssertValid(source, Array(Unsigned(1), Unsigned(2)));
        AssertInvalid(
            source,
            Array(Unsigned(1), Unsigned(1)),
            CddlValidationCode.OccurrenceMismatch);
    }

    [Test]
    public void MatchesIncludedGroupsLabelsAndOptionalMembers()
    {
        string source =
            "root = [header, fields]\n"
            + "header = (version: 1,)\n"
            + "fields = (name: #3, ? count: #0,)";
        AssertValid(
            source,
            Array(Unsigned(1), Text("item"), Unsigned(4)));
        AssertValid(source, Array(Unsigned(1), Text("item")));
        CddlValidationResult wrongOrder = Validate(
            source,
            Array(Text("item"), Unsigned(1)));

        Assert.Multiple(() =>
        {
            Assert.That(wrongOrder.IsValid, Is.False);
            Assert.That(wrongOrder.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.OccurrenceMismatch));
            Assert.That(FlattenPaths(wrongOrder.Issues[0]),
                Does.Contain("$[0]"));
        });
    }

    [Test]
    public void ExploresGroupChoicesIncludingAnEmptyAlternative()
    {
        string source = "root = [1, 2 // 3 //]";
        AssertValid(source, Array(Unsigned(1), Unsigned(2)));
        AssertValid(source, Array(Unsigned(3)));
        AssertValid(source, Array());
        CddlValidationResult mismatch = Validate(
            source,
            Array(Unsigned(1)));

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.IsValid, Is.False);
            Assert.That(mismatch.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.OccurrenceMismatch));
            Assert.That(mismatch.Issues[0].Details, Is.Not.Empty);
        });
    }

    [Test]
    public void KeepsVariableLengthGroupAlternativesForLaterEntries()
    {
        string source =
            "root = [* part, 9]\n"
            + "part = (1 // 1, 2)";
        AssertValid(
            source,
            Array(Unsigned(1), Unsigned(2), Unsigned(9)));
        AssertValid(
            source,
            Array(
                Unsigned(1),
                Unsigned(1),
                Unsigned(2),
                Unsigned(9)));
        AssertInvalid(
            source,
            Array(Unsigned(1), Unsigned(2)),
            CddlValidationCode.OccurrenceMismatch);
    }

    [Test]
    public void MatchesNullableRepeatedGroupsWithoutEmptyLoops()
    {
        AssertValid(
            "root = [* empty, 1]\nempty = ()",
            Array(Unsigned(1)));
        AssertValid(
            "root = [+ empty]\nempty = ()",
            Array());
        AssertValid(
            "root = [2* empty]\nempty = ()",
            Array());
        AssertValid(
            "root = [2* optional]\noptional = (? 1,)",
            Array());

        CddlValidationOptions limitedOptions = new CddlValidationOptions
        {
            MaxAlternatives = 10
        };
        CddlValidationResult bounded = Validate(
            "root = [1000000* empty]\nempty = ()",
            Array(),
            limitedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(bounded.IsValid, Is.False);
            Assert.That(bounded.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
        });
    }

    [Test]
    public void MatchesUnwrappedArrayGroupsAndGenericGroups()
    {
        string unwrap =
            "root = [0, ~base, 3]\n"
            + "base = [1, ? 2]";
        AssertValid(
            unwrap,
            Array(Unsigned(0), Unsigned(1), Unsigned(2), Unsigned(3)));
        AssertValid(
            unwrap,
            Array(Unsigned(0), Unsigned(1), Unsigned(3)));
        AssertValid(
            "root = [pair<1>]\npair<t> = (t, t)",
            Array(Unsigned(1), Unsigned(1)));
    }

    [Test]
    public void PreservesNestedArrayPathsInMismatchDetails()
    {
        CddlValidationResult result = Validate(
            "root = [1, [2, 3]]",
            Array(Unsigned(1), Array(Unsigned(2), Unsigned(4))));
        CddlValidationIssue? literal = FindIssue(
            result.Issues[0],
            CddlValidationCode.LiteralMismatch);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(literal, Is.Not.Null);
            Assert.That(literal!.InstancePath, Is.EqualTo("$[1][1]"));
            Assert.That(literal.SchemaPath, Does.Contain("/node["));
        });
    }

    [Test]
    public void AppliesAlternativeStepAndNestingBudgetsToOrderedGroups()
    {
        CddlValidationResult exactAlternatives = Validate(
            "root = [+ 1]",
            Array(Unsigned(1), Unsigned(1)),
            new CddlValidationOptions { MaxAlternatives = 2 });
        CddlValidationResult alternatives = Validate(
            "root = [1 // 2]",
            Array(Unsigned(2)),
            new CddlValidationOptions { MaxAlternatives = 1 });
        CddlValidationResult steps = Validate(
            "root = [1]",
            Array(Unsigned(1)),
            new CddlValidationOptions { MaxSteps = 2 });
        CddlValidationResult nesting = Validate(
            "root = [[[1]]]",
            Array(Array(Array(Unsigned(1)))),
            new CddlValidationOptions { MaxNestingDepth = 2 });

        Assert.Multiple(() =>
        {
            Assert.That(exactAlternatives.IsValid, Is.True);
            Assert.That(alternatives.IsValid, Is.False);
            Assert.That(alternatives.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
            Assert.That(steps.IsValid, Is.False);
            Assert.That(steps.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
            Assert.That(nesting.IsValid, Is.False);
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

    private static void AssertInvalid(
        string source,
        CborValue value,
        CddlValidationCode code)
    {
        CddlValidationResult result = Validate(source, value);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
    }

    private static CddlValidationResult Validate(
        string source,
        CborValue value,
        CddlValidationOptions? options = null)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "ordered.cddl" });
        Assert.That(parse.IsSuccess, Is.True);
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

    private static CborValue Array(params CborValue[] items)
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = new List<CborValue>(items)
        };
    }
}
