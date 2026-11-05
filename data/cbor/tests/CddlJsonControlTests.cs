using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlJsonControlTests
{
    [Test]
    public void ParsesAndValidatesOneEmbeddedJsonValue()
    {
        string source =
            "root = tstr .json claims\n"
            + "claims = {\"iss\": tstr, \"exp\": uint, "
            + "\"flags\": [true, false, null]}";

        AssertValid(
            source,
            "{\"iss\":\"joe\",\"exp\":1300819380,"
                + "\"flags\":[true,false,null]}");
        AssertInvalid(
            source,
            "{\"iss\":\"joe\",\"exp\":-1,"
                + "\"flags\":[true,false,null]}");
    }

    [TestCase("10")]
    [TestCase("10.0")]
    [TestCase("1e1")]
    [TestCase("1.0e1")]
    [TestCase("100e-1")]
    public void TreatsEveryIntegralJsonNumberFormAsAnInteger(string json)
    {
        AssertValid("root = tstr .json 10", json);
        AssertValid("root = tstr .json uint", json);
        AssertValid("root = tstr .json 10.0", json);
    }

    [Test]
    public void PreservesArbitraryPrecisionIntegralJsonNumbers()
    {
        AssertValid(
            "root = tstr .json 18446744073709551616",
            "184467440737095516160e-1");
        AssertValid(
            "root = tstr .json -18446744073709551616",
            "-18446744073709551616.0");
        AssertValid(
            "root = tstr .json integer",
            "1e400");
    }

    [Test]
    public void AppliesAppendixEFloatRepresentabilityRules()
    {
        AssertValid("root = tstr .json float16", "1.5");
        AssertValid("root = tstr .json float16", "10");
        AssertValid("root = tstr .json float32", "1.5");
        AssertValid("root = tstr .json float64", "1.5");
        AssertInvalid("root = tstr .json float16", "1.1");
        AssertValid("root = tstr .json (0.0..2.0)", "1");
    }

    [Test]
    public void TreatsJsonObjectsAsTextKeyedMapsWithoutWrapperConventions()
    {
        const string json =
            "{\"$cbor\":\"bytes\",\"encoding\":\"base64url\","
            + "\"value\":\"AQ\"}";
        string source =
            "root = tstr .json {\"$cbor\": \"bytes\", "
            + "\"encoding\": \"base64url\", \"value\": \"AQ\"}";

        AssertValid(source, json);
        AssertInvalid("root = tstr .json bstr", "\"AQ\"");
    }

    [Test]
    public void DecodesJsonStringsAndPreservesDuplicateMembers()
    {
        AssertValid(
            "root = tstr .json [\"A\\n😀\", 1, 2]",
            "[\"A\\n\\uD83D\\uDE00\",1,2]");
        AssertValid(
            "root = tstr .json {* tstr => uint}",
            "{\"x\":1,\"x\":2}");
    }

    [TestCase("")]
    [TestCase("1 2")]
    [TestCase("[1,]")]
    [TestCase("{\"x\":1,}")]
    [TestCase("01")]
    [TestCase("NaN")]
    [TestCase("/*x*/ 1")]
    [TestCase("1e-400")]
    public void RejectsMalformedOrUnrepresentableJson(string json)
    {
        CddlValidationResult result = Validate(
            "root = tstr .json any",
            json,
            new CddlValidationOptions());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues, Is.Not.Empty);
        Assert.That(result.Issues[0].Code,
            Is.EqualTo(CddlValidationCode.ControlMismatch));
    }

    [Test]
    public void AppliesSharedInputDepthStepAndNumericBudgets()
    {
        CddlValidationResult bytes = Validate(
            "root = tstr .json any",
            "[1,2]",
            new CddlValidationOptions { MaxEmbeddedBytes = 4 });
        CddlValidationResult depth = Validate(
            "root = tstr .json any",
            "[[1]]",
            new CddlValidationOptions { MaxNestingDepth = 1 });
        CddlValidationResult steps = Validate(
            "root = tstr .json any",
            "[1,2,3]",
            new CddlValidationOptions { MaxSteps = 4 });
        CddlValidationResult number = Validate(
            "root = tstr .json integer",
            "1e1000",
            new CddlValidationOptions { MaxPatternStates = 100 });

        AssertResourceLimit(bytes);
        AssertResourceLimit(depth);
        AssertResourceLimit(steps);
        AssertResourceLimit(number);
    }

    private static void AssertResourceLimit(CddlValidationResult result)
    {
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues.Select(item => item.Code),
            Does.Contain(CddlValidationCode.ResourceLimit));
    }

    private static void AssertValid(string source, string json)
    {
        CddlValidationResult result = Validate(
            source,
            json,
            new CddlValidationOptions());
        Assert.That(
            result.IsValid,
            Is.True,
            result.Issues.Length == 0 ? "" : result.Issues[0].Message);
    }

    private static void AssertInvalid(string source, string json)
    {
        CddlValidationResult result = Validate(
            source,
            json,
            new CddlValidationOptions());
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues, Is.Not.Empty);
    }

    private static CddlValidationResult Validate(
        string source,
        string json,
        CddlValidationOptions options)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "json-control.cddl" });
        Assert.That(
            parse.Diagnostics,
            Is.Empty,
            parse.Diagnostics.Length == 0
                ? ""
                : parse.Diagnostics[0].Message);
        CddlCompileResult compile = new CddlCompiler().TryCompileSchema(
            parse.Document!,
            new CddlCompileOptions());
        Assert.That(
            compile.IsSuccess,
            Is.True,
            compile.Diagnostics.Length == 0
                ? ""
                : compile.Diagnostics[0].Message);
        return new CddlValidator().ValidateCbor(
            compile.Schema!,
            new CborValue
            {
                Kind = CborValueKind.TextString,
                TextStringValue = json
            },
            options);
    }
}
