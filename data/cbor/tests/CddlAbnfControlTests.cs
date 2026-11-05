using cbor;
using System.Text;

namespace cbor.Tests;

[TestFixture]
public class CddlAbnfControlTests
{
    [Test]
    public void AppliesRfc5234AndRfc7405StringCaseRules()
    {
        AssertValid("root = tstr .abnf \"\\\"abc\\\"\"", Text("AbC"));
        AssertValid(
            "root = tstr .abnf \"%i\\\"aBc\\\"\"",
            Text("ABC"));
        AssertValid(
            "root = tstr .abnf \"%s\\\"aBc\\\"\"",
            Text("aBc"));
        AssertInvalid(
            "root = tstr .abnf \"%s\\\"aBc\\\"\"",
            Text("abc"));
        AssertInvalid("root = tstr .abnf \"\\\"abc\\\"\"", Text("abcd"));
    }

    [Test]
    public void MatchesNumericBytesAndUnicodeScalarValues()
    {
        AssertValid(
            "root = tstr .abnf \"(%b1100001 %d98 %x63)\"",
            Text("abc"));
        AssertValid(
            "root = bstr .abnfb \"(%x00 %x41-5A)\"",
            Bytes(0x00, 0x4f));
        AssertInvalid(
            "root = bstr .abnfb \"(%x00 %x41-5A)\"",
            Bytes(0x00, 0x61));
        AssertValid(
            "root = tstr .abnfb \"%xF0.9F.98.80\"",
            Text("😀"));
        AssertValid(
            "root = tstr .abnf \"%x1F600\"",
            Text("😀"));
        AssertValid(
            "root = bstr .abnf \"%x1F600\"",
            Bytes(0xf0, 0x9f, 0x98, 0x80));
    }

    [Test]
    public void ResolvesExplicitRulesAlternativesRepetitionAndOptions()
    {
        string source =
            "root = tstr .abnf grammar\n"
            + "grammar = \"(word [\\\"!\\\"])\\n"
            + "word = 1*ALPHA\\n"
            + "ALPHA = %x41-5A / %x61-7A\\n\"";

        AssertValid(source, Text("hello"));
        AssertValid(source, Text("Hello!"));
        AssertInvalid(source, Text("hello!!"));
        AssertInvalid(source, Text("abc1"));
    }

    [Test]
    public void SupportsIncrementalAlternativesAndCaseInsensitiveRuleNames()
    {
        string source =
            "root = tstr .abnf \"token\\n"
            + "TOKEN = %s\\\"a\\\"\\n"
            + "token =/ %s\\\"b\\\"\\n\"";

        AssertValid(source, Text("a"));
        AssertValid(source, Text("b"));
        AssertInvalid(source, Text("B"));
        AssertInvalid(source, Text("c"));
    }

    [Test]
    public void AcceptsLfCrLfCommentsAndIndentedContinuations()
    {
        string lf =
            "root = tstr .abnf \"value\\n"
            + "value = %s\\\"a\\\" / ; first choice\\n"
            + "  %s\\\"b\\\"\\n\"";
        string crlf =
            "root = tstr .abnf \"value\\r\\n"
            + "; ignored\\r\\n"
            + "value = %s\\\"x\\\"\\r\\n\"";

        AssertValid(lf, Text("a"));
        AssertValid(lf, Text("b"));
        AssertValid(crlf, Text("x"));
    }

    [Test]
    public void AppliesExactBoundedAndUnboundedRepetition()
    {
        AssertValid("root = tstr .abnf \"(2*3%s\\\"a\\\")\"", Text("aa"));
        AssertValid("root = tstr .abnf \"(2*3%s\\\"a\\\")\"", Text("aaa"));
        AssertInvalid("root = tstr .abnf \"(2*3%s\\\"a\\\")\"", Text("a"));
        AssertInvalid("root = tstr .abnf \"(2*3%s\\\"a\\\")\"", Text("aaaa"));
        AssertValid("root = tstr .abnf \"(*%s\\\"a\\\")\"", Text(""));
        AssertValid("root = tstr .abnf \"(*%s\\\"a\\\")\"", Text("aaaa"));
        AssertValid("root = tstr .abnf \"(*2%s\\\"a\\\")\"", Text("a"));
        AssertInvalid("root = tstr .abnf \"(*2%s\\\"a\\\")\"", Text("aaa"));
        AssertValid("root = tstr .abnf \"[2%s\\\"a\\\"]\"", Text(""));
        AssertValid("root = tstr .abnf \"[2%s\\\"a\\\"]\"", Text("aa"));
    }

    [Test]
    public void AcceptsUtf8ByteControllersAndComputedGrammars()
    {
        AssertValid(
            "root = tstr .abnf '\"abc\"'",
            Text("ABC"));
        string computed =
            "root = tstr .abnf (\"word\" .cat rules)\n"
            + "rules = '\\nword = 1*DIGIT\\nDIGIT = %x30-39\\n'";
        AssertValid(computed, Text("12345"));
        AssertInvalid(computed, Text("12a"));
    }

    [Test]
    public void RejectsInvalidUtf8InputsAndControllers()
    {
        CddlValidationResult input = Validate(
            "root = bstr .abnf \"%x61\"",
            Bytes(0xff),
            new CddlValidationOptions());
        CddlValidationResult controller = Validate(
            "root = tstr .abnf h'ff'",
            Text("a"),
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(input.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ControlMismatch));
            Assert.That(controller.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidSchema));
        });
    }

    [TestCase("root = uint .abnf \"%x31\"",
        "CDDL_COMPILE_CONTROL_TARGET_INVALID")]
    [TestCase("root = tstr .abnfb 1",
        "CDDL_COMPILE_CONTROL_CONTROLLER_INVALID")]
    public void RejectsIncompatibleAbnfOperands(
        string source,
        string expectedCode)
    {
        CddlCompileResult result = Compile(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Does.Contain(expectedCode));
        });
    }

    [TestCase("root = tstr .abnf \"DIGIT\"", "not defined")]
    [TestCase("root = tstr .abnf \"<prose>\"", "prose")]
    [TestCase(
        "root = tstr .abnf \"rule\\nrule =/ \\\"a\\\"\\n\"",
        "no base definition")]
    [TestCase(
        "root = tstr .abnf \"rule\\nrule = \\\"a\\\"\\nrule = \\\"b\\\"\\n\"",
        "defined more than once")]
    [TestCase("root = tstr .abnf \"(%x5A-41)\"", "ordered endpoints")]
    [TestCase("root = tstr .abnf \"(3*2\\\"a\\\")\"", "minimum")]
    public void RejectsMalformedOrNonExecutableGrammars(
        string source,
        string messagePart)
    {
        CddlValidationResult result = Validate(
            source,
            Text("anything"),
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidSchema));
            Assert.That(result.Issues[0].Message,
                Does.Contain(messagePart).IgnoreCase);
        });
    }

    [Test]
    public void HandlesRightRecursionAndEnforcesPatternBudgets()
    {
        string source =
            "root = tstr .abnf \"many\\n"
            + "many = %s\\\"a\\\" [many]\\n\"";
        AssertValid(source, Text("aaaa"));
        AssertInvalid(source, Text("aaab"));

        CddlValidationResult limited = Validate(
            source,
            Text("aaaaaaaa"),
            new CddlValidationOptions { MaxPatternStates = 5 });
        CddlValidationResult alternatives = Validate(
            "root = tstr .abnf \"(%s\\\"a\\\" / %s\\\"b\\\" / %s\\\"c\\\")\"",
            Text("c"),
            new CddlValidationOptions { MaxAlternatives = 1 });
        Assert.Multiple(() =>
        {
            Assert.That(limited.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
            Assert.That(alternatives.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
        });
    }

    [Test]
    public void ReportsStructuredAbnfMismatches()
    {
        CddlValidationResult result = Validate(
            "root = tstr .abnf \"(%s\\\"ok\\\")\"",
            Text("no"),
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ControlMismatch));
            Assert.That(result.Issues[0].SchemaPath,
                Does.Contain("/control(.abnf)"));
            Assert.That(result.Issues[0].Expected,
                Does.Contain("Unicode scalar"));
        });
    }

    private static CddlCompileResult Compile(string source)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "abnf-control.cddl" });
        Assert.That(
            parse.Diagnostics,
            Is.Empty,
            parse.Diagnostics.Length == 0
                ? ""
                : parse.Diagnostics[0].Message);
        return new CddlCompiler().TryCompileSchema(
            parse.Document!,
            new CddlCompileOptions());
    }

    private static CddlValidationResult Validate(
        string source,
        CborValue value,
        CddlValidationOptions options)
    {
        CddlCompileResult compile = Compile(source);
        Assert.That(
            compile.IsSuccess,
            Is.True,
            compile.Diagnostics.Length == 0
                ? ""
                : compile.Diagnostics[0].Message);
        return new CddlValidator().ValidateCbor(
            compile.Schema!,
            value,
            options);
    }

    private static void AssertValid(string source, CborValue value)
    {
        CddlValidationResult result = Validate(
            source,
            value,
            new CddlValidationOptions());
        Assert.That(
            result.IsValid,
            Is.True,
            result.Issues.Length == 0 ? "" : result.Issues[0].Message);
    }

    private static void AssertInvalid(string source, CborValue value)
    {
        CddlValidationResult result = Validate(
            source,
            value,
            new CddlValidationOptions());
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues, Is.Not.Empty);
    }

    private static CborValue Text(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = value
        };
    }

    private static CborValue Bytes(params byte[] value)
    {
        return new CborValue
        {
            Kind = CborValueKind.ByteString,
            ByteStringValue = value
        };
    }
}
