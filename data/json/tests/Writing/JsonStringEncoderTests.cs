namespace json.Tests;

public class JsonStringEncoderTests
{
    [Test]
    public void MinimalModeEscapesOnlyRequiredJsonCharacters()
    {
        JsonStringEncoder encoder = CreateEncoder(
            JsonStringEscapeMode.Minimal,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false,
            null);
        string value = "\"\\/\b\f\n\r\t\0\u001f\u007f€\u2028e\u0301";

        string encoded = Text(encoder.EncodeString(value));

        Assert.That(
            encoded,
            Is.EqualTo("\"\\\"\\\\/\\b\\f\\n\\r\\t\\u0000\\u001f\u007f€\u2028e\u0301\""));
    }

    [Test]
    public void AsciiOnlyModeUsesConfiguredHexadecimalCaseAcrossBoundaries()
    {
        JsonStringEncoder uppercase = CreateEncoder(
            JsonStringEscapeMode.AsciiOnly,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            true,
            null);
        JsonStringEncoder lowercase = CreateEncoder(
            JsonStringEscapeMode.AsciiOnly,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false,
            null);
        string value = "\u007f\u0080é😀";

        Assert.Multiple(() =>
        {
            Assert.That(
                Text(uppercase.EncodeString(value)),
                Is.EqualTo("\"\u007f\\u0080\\u00E9\\uD83D\\uDE00\""));
            Assert.That(
                Text(lowercase.EncodeString(value)),
                Is.EqualTo("\"\u007f\\u0080\\u00e9\\ud83d\\ude00\""));
        });
    }

    [Test]
    public void HtmlSafeAndSolidusModesEscapeEmbeddingSensitiveCharacters()
    {
        JsonStringEncoder encoder = CreateEncoder(
            JsonStringEscapeMode.HtmlSafe,
            JsonUnicodePolicy.UnicodeScalar,
            true,
            false,
            null);

        string encoded = Text(encoder.EncodeString("<>&='/\"\u2028\u2029é"));

        Assert.That(
            encoded,
            Is.EqualTo("\"\\u003c\\u003e\\u0026\\u003d\\u0027\\/\\\"\\u2028\\u2029é\""));
    }

    [Test]
    public void SurrogatePolicyEscapesLoneUnitsOrRejectsThem()
    {
        JsonStringEncoder compatible = CreateEncoder(
            JsonStringEscapeMode.Minimal,
            JsonUnicodePolicy.SyntaxCompatible,
            false,
            true,
            null);
        JsonStringEncoder scalar = CreateEncoder(
            JsonStringEscapeMode.Minimal,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false,
            null);
        string loneUnits = new string(new char[]
        {
            '\uD800',
            'x',
            '\uDC00'
        });

        JsonWriteException highFailure = Assert.Throws<JsonWriteException>(() =>
            scalar.EncodeString(new string(new char[] { '\uD800' })))
            ?? throw new InvalidOperationException("Expected JsonWriteException.");
        JsonWriteException lowFailure = Assert.Throws<JsonWriteException>(() =>
            scalar.EncodeString(new string(new char[] { '\uDC00' })))
            ?? throw new InvalidOperationException("Expected JsonWriteException.");

        Assert.Multiple(() =>
        {
            Assert.That(
                Text(compatible.EncodeString(loneUnits)),
                Is.EqualTo("\"\\uD800x\\uDC00\""));
            Assert.That(
                Text(compatible.EncodeString("😀")),
                Is.EqualTo("\"😀\""));
            Assert.That(highFailure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidSurrogatePair));
            Assert.That(lowFailure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidSurrogatePair));
            Assert.That(Text(scalar.EncodeString("valid")),
                Is.EqualTo("\"valid\""));
        });
    }

    [Test]
    public void JcsRulesOverrideOrdinaryEscapeOptionsAndRejectLoneSurrogates()
    {
        JsonStringEncoder encoder = CreateEncoder(
            JsonStringEscapeMode.AsciiOnly,
            JsonUnicodePolicy.SyntaxCompatible,
            true,
            true,
            null);
        string value = "</é\u000f\n😀";

        string encoded = Text(encoder.EncodeJcsString(value));
        JsonWriteException failure = Assert.Throws<JsonWriteException>(() =>
            encoder.EncodeJcsString(new string(new char[] { '\uDEAD' })))
            ?? throw new InvalidOperationException("Expected JsonWriteException.");

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo("\"</é\\u000f\\n😀\""));
            Assert.That(failure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidSurrogatePair));
        });
    }

    [Test]
    public void DecodedStringLimitsUseUtf16UnitsAndUtf8ScalarBytes()
    {
        JsonLimits exactLimits = new JsonLimits();
        exactLimits.MaximumDecodedStringCodeUnits = 3;
        exactLimits.MaximumDecodedStringBytes = 5;
        JsonStringEncoder exact = CreateEncoder(
            JsonStringEscapeMode.Minimal,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false,
            exactLimits);
        Assert.That(Text(exact.EncodeString("A😀")), Is.EqualTo("\"A😀\""));

        JsonLimits unitLimits = new JsonLimits();
        unitLimits.MaximumDecodedStringCodeUnits = 2;
        JsonStringEncoder units = CreateEncoder(
            JsonStringEscapeMode.Minimal,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false,
            unitLimits);
        JsonLimits byteLimits = new JsonLimits();
        byteLimits.MaximumDecodedStringBytes = 4;
        JsonStringEncoder bytes = CreateEncoder(
            JsonStringEscapeMode.Minimal,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false,
            byteLimits);
        JsonLimits outputLimits = new JsonLimits();
        outputLimits.MaximumOutputBytes = 3;
        JsonStringEncoder output = CreateEncoder(
            JsonStringEscapeMode.Minimal,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false,
            outputLimits);

        JsonLimitException unitFailure = Assert.Throws<JsonLimitException>(() =>
            units.EncodeString("A😀"))
            ?? throw new InvalidOperationException("Expected JsonLimitException.");
        JsonLimitException byteFailure = Assert.Throws<JsonLimitException>(() =>
            bytes.EncodeString("A😀"))
            ?? throw new InvalidOperationException("Expected JsonLimitException.");
        JsonLimitException outputFailure = Assert.Throws<JsonLimitException>(() =>
            output.EncodeString("é"))
            ?? throw new InvalidOperationException("Expected JsonLimitException.");

        Assert.Multiple(() =>
        {
            Assert.That(unitFailure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.DecodedStringCodeUnitLimitExceeded));
            Assert.That(byteFailure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.DecodedStringByteLimitExceeded));
            Assert.That(outputFailure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.OutputByteLimitExceeded));
        });
    }

    static JsonStringEncoder CreateEncoder(
        JsonStringEscapeMode escapeMode,
        JsonUnicodePolicy unicodePolicy,
        bool escapeSolidus,
        bool uppercase,
        JsonLimits? limits)
    {
        JsonWriterOptions options = new JsonWriterOptions();
        options.StringEscapeMode = escapeMode;
        options.UnicodePolicy = unicodePolicy;
        options.EscapeSolidus = escapeSolidus;
        options.UseUppercaseHexadecimalEscapes = uppercase;
        options.Limits = limits;
        return new JsonStringEncoder(options);
    }

    static string Text(byte[] bytes)
    {
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
