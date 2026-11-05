using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlJsonValidatorTests
{
    [Test]
    public void ValidatesDirectJsonDocumentsAcrossTheAppendixEDataModel()
    {
        CddlSchema schema = Compile(
            "root = {"
            + "\"name\": tstr, \"active\": bool, \"nothing\": null, "
            + "\"items\": [uint, nint, any], \"nested\": {\"x\": tstr}"
            + "}");
        CddlValidationResult valid = Validate(
            schema,
            "{\"name\":\"Ada\",\"active\":true,\"nothing\":null,"
                + "\"items\":[10,-2,false],\"nested\":{\"x\":\"y\"}}");
        CddlValidationResult wrongType = Validate(
            schema,
            "{\"name\":\"Ada\",\"active\":1,\"nothing\":null,"
                + "\"items\":[10,-2,false],\"nested\":{\"x\":\"y\"}}");

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsValid, Is.True);
            Assert.That(valid.RuleName, Is.EqualTo("root"));
            Assert.That(valid.Issues, Is.Empty);
            Assert.That(wrongType.IsValid, Is.False);
            Assert.That(FlattenIssues(wrongType.Issues).Select(item =>
                item.InstancePath), Does.Contain("$[\"active\"]"));
        });
    }

    [Test]
    public void JsonObjectsExposeOnlyTextKeysAndPreserveMemberNames()
    {
        CddlValidationResult textKeys = Validate(
            Compile("root = {* tstr => uint}"),
            "{\"1\":1,\"two\":2}");
        CddlValidationResult integerKey = Validate(
            Compile("root = {1 => uint}"),
            "{\"1\":1}");
        CddlValidationResult bytes = Validate(
            Compile("root = bstr"),
            "\"AQ\"");
        CddlValidationResult undefined = Validate(
            Compile("root = undefined"),
            "null");

        Assert.Multiple(() =>
        {
            Assert.That(textKeys.IsValid, Is.True);
            Assert.That(integerKey.IsValid, Is.False);
            Assert.That(bytes.IsValid, Is.False);
            Assert.That(undefined.IsValid, Is.False);
            Assert.That(FlattenIssues(integerKey.Issues).Select(item =>
                item.InstancePath), Does.Contain("$[\"1\"]"));
        });
    }

    [TestCase("10")]
    [TestCase("10.0")]
    [TestCase("1e1")]
    [TestCase("1.0e1")]
    [TestCase("100e-1")]
    public void IntegralJsonSpellingsMatchIntegerTypes(string json)
    {
        CddlValidationResult result = Validate(Compile("root = uint"), json);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void EnforcesExactBinaryFloatingPointRepresentability()
    {
        CddlValidationResult half = Validate(Compile("root = float16"), "1.5");
        CddlValidationResult notHalf = Validate(
            Compile("root = float16"),
            "1.1");
        CddlValidationResult single = Validate(
            Compile("root = float32"),
            "16777216");
        CddlValidationResult notSingle = Validate(
            Compile("root = float32"),
            "16777217");
        CddlValidationResult doubleValue = Validate(
            Compile("root = float64"),
            "9007199254740992");
        CddlValidationResult notDouble = Validate(
            Compile("root = float64"),
            "9007199254740993");
        CddlValidationResult exactFraction = Validate(
            Compile("root = float64"),
            "0.5");
        CddlValidationResult inexactFraction = Validate(
            Compile("root = float64"),
            "0.1");
        CddlValidationResult unrestricted = Validate(
            Compile("root = any"),
            "0.1");

        Assert.Multiple(() =>
        {
            Assert.That(half.IsValid, Is.True);
            Assert.That(notHalf.IsValid, Is.False);
            Assert.That(single.IsValid, Is.True);
            Assert.That(notSingle.IsValid, Is.False);
            Assert.That(doubleValue.IsValid, Is.True);
            Assert.That(notDouble.IsValid, Is.False);
            Assert.That(exactFraction.IsValid, Is.True);
            Assert.That(inexactFraction.IsValid, Is.False);
            Assert.That(unrestricted.IsValid, Is.True);
        });
    }

    [Test]
    public void SupportsArbitraryPrecisionIntegralJsonNumbersAndRootSelection()
    {
        CddlSchema schema = Compile(
            "root = false\n"
            + "large = integer");
        CddlValidationResult result = new CddlJsonValidator().ValidateJson(
            schema,
            "184467440737095516160e-1",
            new CddlValidationOptions { RootRuleName = "large" });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.RuleName, Is.EqualTo("large"));
            Assert.That(result.Options.RootRuleName, Is.EqualTo("large"));
        });
    }

    [Test]
    public void ReportsJsonSyntaxRepresentationAndBudgetFailures()
    {
        CddlSchema schema = Compile("root = any");
        CddlValidationResult syntax = Validate(schema, "{\"x\":]}");
        CddlValidationResult range = Validate(schema, "1e-400");
        CddlValidationResult bytes = new CddlJsonValidator().ValidateJson(
            schema,
            "[1,2]",
            new CddlValidationOptions { MaxEmbeddedBytes = 4 });
        CddlValidationResult depth = new CddlJsonValidator().ValidateJson(
            schema,
            "[[1]]",
            new CddlValidationOptions { MaxNestingDepth = 1 });
        CddlValidationResult states = new CddlJsonValidator().ValidateJson(
            schema,
            "1e1000",
            new CddlValidationOptions { MaxPatternStates = 100 });

        Assert.Multiple(() =>
        {
            Assert.That(syntax.IsValid, Is.False);
            Assert.That(syntax.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidEncoding));
            Assert.That(syntax.Issues[0].ByteOffset, Is.GreaterThanOrEqualTo(0));
            Assert.That(range.IsValid, Is.False);
            Assert.That(range.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidEncoding));
            AssertResourceLimit(bytes);
            AssertResourceLimit(depth);
            AssertResourceLimit(states);
        });
    }

    [Test]
    public void MissingInputsReturnStructuredFailures()
    {
        CddlSchema schema = Compile("root = any");
        ICddlJsonValidator validator = new CddlJsonValidator();
        CddlValidationResult missingSchema = validator.ValidateJson(
            null!,
            "null",
            new CddlValidationOptions());
        CddlValidationResult missingJson = validator.ValidateJson(
            schema,
            null!,
            new CddlValidationOptions());
        CddlValidationResult missingOptions = validator.ValidateJson(
            schema,
            "null",
            null!);

        Assert.Multiple(() =>
        {
            Assert.That(missingSchema.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidSchema));
            Assert.That(missingJson.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidEncoding));
            Assert.That(missingOptions.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidSchema));
        });
    }

    private static void AssertResourceLimit(CddlValidationResult result)
    {
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues.Select(item => item.Code),
            Does.Contain(CddlValidationCode.ResourceLimit));
    }

    private static CddlValidationIssue[] FlattenIssues(
        CddlValidationIssue[] issues)
    {
        List<CddlValidationIssue> flattened = new List<CddlValidationIssue>();
        List<CddlValidationIssue> pending = new List<CddlValidationIssue>(issues);
        while (pending.Count > 0)
        {
            CddlValidationIssue issue = pending[0];
            pending.RemoveAt(0);
            flattened.Add(issue);
            pending.AddRange(issue.Details);
        }
        return flattened.ToArray();
    }

    private static CddlValidationResult Validate(
        CddlSchema schema,
        string json)
    {
        return new CddlJsonValidator().ValidateJson(
            schema,
            json,
            new CddlValidationOptions());
    }

    private static CddlSchema Compile(string source)
    {
        CddlDocument document = new CddlParser().ParseDocument(
            source,
            new CddlParseOptions { SourceName = "direct-json.cddl" });
        return new CddlCompiler().CompileSchema(
            document,
            new CddlCompileOptions());
    }
}
