using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlTextConversionControlTests
{
    [TestCase(".b64u", "Zm8", "666f")]
    [TestCase(".b64u", "_w", "ff")]
    [TestCase(".b64c", "Zm8=", "666f")]
    [TestCase(".b64c", "/w==", "ff")]
    [TestCase(".b32", "MZXQ", "666f")]
    [TestCase(".h32", "CPNG", "666f")]
    [TestCase(".b45", "BB8", "4142")]
    [TestCase(".b45", "QED8WEX0", "6965746621")]
    [TestCase(".hex", "DeAd", "dead")]
    [TestCase(".hexlc", "dead", "dead")]
    [TestCase(".hexuc", "DEAD", "dead")]
    [TestCase(".b64u", "", "")]
    [TestCase(".b64c", "", "")]
    [TestCase(".b32", "", "")]
    [TestCase(".h32", "", "")]
    [TestCase(".b45", "", "")]
    [TestCase(".hex", "", "")]
    public void MatchesRfcEncodingVectors(
        string control,
        string encoded,
        string expectedHex)
    {
        string source = "root = tstr " + control
            + " h'" + expectedHex + "'";

        AssertValid(source, encoded);
    }

    [TestCase(".b64u", "Zh", false)]
    [TestCase(".b64u-sloppy", "Zh", true)]
    [TestCase(".b64c", "Zh==", false)]
    [TestCase(".b64c-sloppy", "Zh==", true)]
    public void SloppyBase64OnlyRelaxesTrailingBitValidation(
        string control,
        string encoded,
        bool expected)
    {
        CddlValidationResult result = Validate(
            "root = tstr " + control + " h'66'",
            encoded,
            new CddlValidationOptions());

        Assert.That(result.IsValid, Is.EqualTo(expected));
    }

    [TestCase(".b64u", "Zg==")]
    [TestCase(".b64u", "+w")]
    [TestCase(".b64u", "A")]
    [TestCase(".b64c", "Zg")]
    [TestCase(".b64c", "-w==")]
    [TestCase(".b64c", "Zg===")]
    [TestCase(".b32", "mzxq")]
    [TestCase(".b32", "MZ")]
    [TestCase(".h32", "cpng")]
    [TestCase(".b45", "A")]
    [TestCase(".b45", "::")]
    [TestCase(".b45", ":::")]
    [TestCase(".b45", "abc")]
    public void RejectsInvalidOrNonCanonicalEncodings(
        string control,
        string encoded)
    {
        CddlValidationResult result = Validate(
            "root = tstr " + control + " bstr",
            encoded,
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ControlMismatch));
        });
    }

    [TestCase(".hexlc", "DEAD")]
    [TestCase(".hexuc", "dead")]
    [TestCase(".hex", "abc")]
    [TestCase(".hex", "0g")]
    public void EnforcesHexadecimalCaseAndShape(
        string control,
        string encoded)
    {
        CddlValidationResult result = Validate(
            "root = tstr " + control + " bstr",
            encoded,
            new CddlValidationOptions());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues[0].Code,
            Is.EqualTo(CddlValidationCode.ControlMismatch));
    }

    [TestCase("0", "0")]
    [TestCase("-1", "-1")]
    [TestCase("255", "0..255")]
    [TestCase("18446744073709551616", "18446744073709551616")]
    public void MatchesCanonicalBase10Integers(
        string encoded,
        string controller)
    {
        AssertValid(
            "root = tstr .base10 (" + controller + ")",
            encoded);
    }

    [TestCase("")]
    [TestCase("-0")]
    [TestCase("+1")]
    [TestCase("00")]
    [TestCase("01")]
    [TestCase(" 1")]
    [TestCase("1.0")]
    public void RejectsNonCanonicalBase10Text(string encoded)
    {
        CddlValidationResult result = Validate(
            "root = tstr .base10 int",
            encoded,
            new CddlValidationOptions());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues[0].Code,
            Is.EqualTo(CddlValidationCode.ControlMismatch));
    }

    [Test]
    public void AppliesTheControllerToTheConvertedValue()
    {
        CddlValidationResult bytes = Validate(
            "root = tstr .hex h'00'",
            "01",
            new CddlValidationOptions());
        CddlValidationResult integer = Validate(
            "root = tstr .base10 (0..255)",
            "256",
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(bytes.IsValid, Is.False);
            Assert.That(bytes.Issues[0].Details, Is.Not.Empty);
            Assert.That(integer.IsValid, Is.False);
            Assert.That(integer.Issues[0].Details, Is.Not.Empty);
        });
    }

    [Test]
    public void ChargesDecodedBytesToTheSharedBudget()
    {
        CddlValidationResult result = Validate(
            "root = tstr .hex bstr",
            "0001",
            new CddlValidationOptions { MaxEmbeddedBytes = 1 });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues.Select(item => item.Code),
            Does.Contain(CddlValidationCode.ResourceLimit));
    }

    private static void AssertValid(string source, string encoded)
    {
        CddlValidationResult result = Validate(
            source,
            encoded,
            new CddlValidationOptions());
        Assert.That(
            result.IsValid,
            Is.True,
            result.Issues.Length == 0 ? "" : result.Issues[0].Message);
    }

    private static CddlValidationResult Validate(
        string source,
        string encoded,
        CddlValidationOptions options)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions
            {
                SourceName = "text-conversion-control.cddl"
            });
        Assert.That(parse.Diagnostics, Is.Empty);
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
                TextStringValue = encoded
            },
            options);
    }
}
