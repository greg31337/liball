using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlPrintfControlTests
{
    [Test]
    public void MatchesTheRfc9741HexLabelExample()
    {
        AssertValid(
            "root = hexlabel<19>\n"
                + "hexlabel<K> = tstr .printf ([\"0x%04x\", K])",
            "0x0013");
    }

    [Test]
    public void MatchesAValueSelectedFromAnArgumentRange()
    {
        string source =
            "root = hexlabel<1..20>\n"
            + "hexlabel<K> = tstr .printf ([\"0x%04x\", K])";

        AssertValid(source, "0x0001");
        AssertValid(source, "0x0013");
        AssertInvalid(source, "0x1234");
    }

    [TestCase("%d", "-42", "-42")]
    [TestCase("%+06d", "42", "+00042")]
    [TestCase("% 5i", "7", "    7")]
    [TestCase("%-5d", "12", "12   ")]
    [TestCase("%.4u", "12", "0012")]
    [TestCase("%#x", "255", "0xff")]
    [TestCase("%#X", "255", "0XFF")]
    [TestCase("%#o", "8", "010")]
    [TestCase("%#b", "5", "0b101")]
    [TestCase("%#B", "5", "0B101")]
    public void FormatsIntegerDirectives(
        string format,
        string argument,
        string expected)
    {
        AssertValid(
            "root = tstr .printf ([\"" + format + "\", "
                + argument + "])",
            expected);
    }

    [Test]
    public void MatchesBroadIntegerControllerTypesByParsingTheOutput()
    {
        AssertValid(
            "root = tstr .printf ([\"id=%08X\", uint])",
            "id=0000BEEF");
        AssertInvalid(
            "root = tstr .printf ([\"id=%08X\", 0..255])",
            "id=0000BEEF");
    }

    [Test]
    public void FormatsUnicodeCharactersAndUtf8Text()
    {
        AssertValid(
            "root = tstr .printf ([\"[%6c]\", 128512])",
            "[  😀]");
        AssertValid(
            "root = tstr .printf ([\"%.2s\", \"éx\"])",
            "é");
        AssertValid(
            "root = tstr .printf ([\"%-5s\", \"é\"])",
            "é   ");
        AssertValid(
            "root = tstr .printf ([\"%c\", 0..127])",
            " ");
    }

    [Test]
    public void HandlesEscapedPercentAndMultipleArguments()
    {
        AssertValid(
            "root = tstr .printf ([\"%s: %d%%\", \"done\", 100])",
            "done: 100%");
        AssertValid(
            "root = tstr .printf ([\"100%% complete\"])",
            "100% complete");
    }

    [TestCase("%d", "\"text\"", "text")]
    [TestCase("%s", "1", "1")]
    [TestCase("%u", "-1", "-1")]
    [TestCase("%c", "-1", "x")]
    public void RejectsArgumentsWithIncompatibleDataTypes(
        string format,
        string argument,
        string value)
    {
        AssertInvalid(
            "root = tstr .printf ([\"" + format + "\", "
                + argument + "])",
            value);
    }

    [TestCase("%f")]
    [TestCase("%a")]
    [TestCase("%p")]
    [TestCase("%n")]
    [TestCase("%ld")]
    [TestCase("%*d")]
    [TestCase("%.*s")]
    [TestCase("%q")]
    [TestCase("%0s")]
    [TestCase("%")]
    public void ReportsUnsupportedOrInvalidDirectives(string format)
    {
        CddlValidationResult result = Validate(
            "root = tstr .printf ([\"" + format + "\", 1])",
            "anything",
            new CddlValidationOptions());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues[0].Code,
            Is.EqualTo(CddlValidationCode.InvalidSchema));
    }

    [Test]
    public void ReportsFormatArgumentCountMismatch()
    {
        CddlValidationResult missing = Validate(
            "root = tstr .printf ([\"%d %d\", 1])",
            "1 1",
            new CddlValidationOptions());
        CddlValidationResult extra = Validate(
            "root = tstr .printf ([\"%d\", 1, 2])",
            "1",
            new CddlValidationOptions());

        Assert.That(missing.Issues[0].Code,
            Is.EqualTo(CddlValidationCode.InvalidSchema));
        Assert.That(extra.Issues[0].Code,
            Is.EqualTo(CddlValidationCode.InvalidSchema));
    }

    [Test]
    public void AppliesPatternAndAlternativeBudgets()
    {
        CddlValidationResult pattern = Validate(
            "root = tstr .printf ([\"value=%d\", int])",
            "value=123",
            new CddlValidationOptions { MaxPatternStates = 2 });
        CddlValidationResult alternatives = Validate(
            "root = tstr .printf ([\"%d%d\", int, int])",
            "12345",
            new CddlValidationOptions { MaxAlternatives = 1 });

        Assert.That(pattern.Issues.Select(item => item.Code),
            Does.Contain(CddlValidationCode.ResourceLimit));
        Assert.That(alternatives.Issues.Select(item => item.Code),
            Does.Contain(CddlValidationCode.ResourceLimit));
    }

    private static void AssertValid(string source, string value)
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

    private static void AssertInvalid(string source, string value)
    {
        CddlValidationResult result = Validate(
            source,
            value,
            new CddlValidationOptions());
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues, Is.Not.Empty);
    }

    private static CddlValidationResult Validate(
        string source,
        string value,
        CddlValidationOptions options)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "printf-control.cddl" });
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
                TextStringValue = value
            },
            options);
    }
}
