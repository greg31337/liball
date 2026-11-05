namespace json.Tests;

public class JsonStandardsConformanceTests
{
    [Test]
    public void Ecma404AllowsExactlyTheFourWhitespaceCharactersAroundStructuralTokens()
    {
        string text = " \t\r\n{ \"a\"\t:\n[ null\r, true , false , -0.125E+2 , \"\\u005C\" ] }\r\n";

        JsonDocument document = Parse(text, JsonConformanceProfile.SyntaxCompatible);
        List<JsonValue> values = document.Root?.ObjectMembers?[0].Value?.ArrayItems
            ?? throw new InvalidOperationException("Expected the ECMA-404 array fixture.");

        Assert.Multiple(() =>
        {
            Assert.That(values.Select(value => value.Kind), Is.EqualTo(new[]
            {
                JsonValueKind.Null,
                JsonValueKind.Boolean,
                JsonValueKind.Boolean,
                JsonValueKind.Number,
                JsonValueKind.String
            }));
            Assert.That(values[1].BooleanValue, Is.True);
            Assert.That(values[2].BooleanValue, Is.False);
            Assert.That(values[3].Number?.Lexeme, Is.EqualTo("-0.125E+2"));
            Assert.That(values[4].StringValue, Is.EqualTo("\\"));
        });
    }

    [Test]
    public void Ecma404RejectsEveryOtherAsciiControlAsTokenWhitespace()
    {
        IJsonDocumentParser parser = new JsonDocumentParser();
        JsonReaderOptions options = new JsonReaderOptions();

        for (int value = 0; value < 0x20; value += 1)
        {
            if (value == 0x09 || value == 0x0A || value == 0x0D)
            {
                continue;
            }

            byte[] input = new byte[] { 0x5B, 0x30, (byte)value, 0x5D };
            JsonDocumentResult result = parser.TryParseUtf8(input, options);
            Assert.That(result.Success, Is.False, "control 0x" + value.ToString("X2"));
        }
    }

    [Test]
    public void Ecma404PreservesArrayOrderAndPermitsDuplicateObjectNameSyntax()
    {
        JsonDocument document = Parse(
            "[{\"x\":0,\"x\":1},\"second\",true,null]",
            JsonConformanceProfile.SyntaxCompatible);
        List<JsonValue> items = document.Root?.ArrayItems
            ?? throw new InvalidOperationException("Expected an array root.");
        List<JsonMember> members = items[0].ObjectMembers
            ?? throw new InvalidOperationException("Expected object members.");

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.Kind), Is.EqualTo(new[]
            {
                JsonValueKind.Object,
                JsonValueKind.String,
                JsonValueKind.Boolean,
                JsonValueKind.Null
            }));
            Assert.That(members.Select(member => member.Name), Is.EqualTo(new[] { "x", "x" }));
            Assert.That(members.Select(member => member.Value?.Number?.Lexeme),
                Is.EqualTo(new[] { "0", "1" }));
        });
    }

    [TestCase("null", JsonValueKind.Null)]
    [TestCase("true", JsonValueKind.Boolean)]
    [TestCase("0", JsonValueKind.Number)]
    [TestCase("\"text\"", JsonValueKind.String)]
    [TestCase("[]", JsonValueKind.Array)]
    [TestCase("{}", JsonValueKind.Object)]
    public void Rfc8259AndIJsonAllowAnyJsonValueAtTheTopLevel(
        string text,
        JsonValueKind expectedKind)
    {
        JsonDocument rfc8259 = Parse(text, JsonConformanceProfile.SyntaxCompatible);
        JsonDocument iJson = Parse(text, JsonConformanceProfile.IJson);

        Assert.Multiple(() =>
        {
            Assert.That(rfc8259.Root?.Kind, Is.EqualTo(expectedKind));
            Assert.That(iJson.Root?.Kind, Is.EqualTo(expectedKind));
        });
    }

    [Test]
    public void Rfc8259InitialBomHandlingIsAnExplicitParserPolicy()
    {
        byte[] input = new byte[] { 0xEF, 0xBB, 0xBF, 0x7B, 0x7D };
        JsonReaderOptions rejectOptions = new JsonReaderOptions();
        JsonReaderOptions allowOptions = new JsonReaderOptions();
        allowOptions.BomPolicy = JsonBomPolicy.AllowInitial;
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocumentResult rejected = parser.TryParseUtf8(input, rejectOptions);
        JsonDocument allowed = parser.ParseUtf8(input, allowOptions);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.BomNotAllowed));
            Assert.That(allowed.Root?.Kind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(allowed.SourceByteCount, Is.EqualTo(input.Length));
            Assert.That(allowed.BomPolicy, Is.EqualTo(JsonBomPolicy.AllowInitial));
        });
    }

    [Test]
    public void Rfc8259NameComparisonDecodesEscapesButDoesNotNormalizeUnicode()
    {
        JsonDocument compatible = Parse(
            "{\"a\\\\b\":0,\"a\\u005Cb\":1,\"é\":2,\"e\\u0301\":3}",
            JsonConformanceProfile.SyntaxCompatible);
        List<JsonMember> members = compatible.Root?.ObjectMembers
            ?? throw new InvalidOperationException("Expected object members.");
        JsonDocument distinctIJson = Parse(
            "{\"é\":0,\"e\\u0301\":1}",
            JsonConformanceProfile.IJson);
        JsonDocumentResult duplicateIJson = TryParse(
            "{\"a\\\\b\":0,\"a\\u005Cb\":1}",
            JsonConformanceProfile.IJson);

        Assert.Multiple(() =>
        {
            Assert.That(members.Select(member => member.Name), Is.EqualTo(new[]
            {
                "a\\b", "a\\b", "é", "e\u0301"
            }));
            Assert.That(distinctIJson.Root?.ObjectMembers, Has.Count.EqualTo(2));
            Assert.That(duplicateIJson.Success, Is.False);
            Assert.That(duplicateIJson.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
        });
    }

    [TestCase("1E400")]
    [TestCase("3.141592653589793238462643383279")]
    [TestCase("9007199254740992")]
    public void Rfc8259AndIJsonRetainInteroperabilitySensitiveNumbersExactly(string lexeme)
    {
        JsonDocument rfc8259 = Parse(lexeme, JsonConformanceProfile.SyntaxCompatible);
        JsonDocument iJson = Parse(lexeme, JsonConformanceProfile.IJson);

        Assert.Multiple(() =>
        {
            Assert.That(rfc8259.Root?.Number?.Lexeme, Is.EqualTo(lexeme));
            Assert.That(iJson.Root?.Number?.Lexeme, Is.EqualTo(lexeme));
        });
    }

    [Test]
    public void Rfc8259SyntaxCanRetainAnUnpairedEscapeWhileIJsonRejectsIt()
    {
        const string text = "[\"\\uDEAD\"]";
        JsonDocument compatible = Parse(text, JsonConformanceProfile.SyntaxCompatible);
        JsonDocumentResult iJson = TryParse(text, JsonConformanceProfile.IJson);

        Assert.Multiple(() =>
        {
            Assert.That(compatible.Root?.ArrayItems?[0].StringValue?[0], Is.EqualTo('\uDEAD'));
            Assert.That(iJson.Success, Is.False);
            Assert.That(iJson.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.InvalidSurrogatePair));
        });
    }

    [Test]
    public void IJsonAcceptsACompleteEscapedSurrogatePair()
    {
        JsonDocument document = Parse(
            "[\"\\uD800\\uDEAD\"]",
            JsonConformanceProfile.IJson);

        Assert.That(
            document.Root?.ArrayItems?[0].StringValue,
            Is.EqualTo(char.ConvertFromUtf32(0x102AD)));
    }

    [Test]
    public void IJsonAndJcsRejectEveryRawAndEscapedUnicodeNoncharacter()
    {
        foreach (int scalar in CreateUnicodeNoncharacters())
        {
            string raw = "[\"" + char.ConvertFromUtf32(scalar) + "\"]";
            string escaped = "[\"" + EscapeScalar(scalar) + "\"]";

            Assert.That(
                TryParse(raw, JsonConformanceProfile.SyntaxCompatible).Success,
                Is.True,
                "raw syntax-compatible U+" + scalar.ToString("X"));
            Assert.That(
                TryParse(escaped, JsonConformanceProfile.Portable).Success,
                Is.True,
                "escaped portable U+" + scalar.ToString("X"));

            AssertNoncharacterFailure(raw, JsonConformanceProfile.IJson, scalar, "raw");
            AssertNoncharacterFailure(escaped, JsonConformanceProfile.IJson, scalar, "escaped");
            AssertNoncharacterFailure(raw, JsonConformanceProfile.Jcs, scalar, "raw");
            AssertNoncharacterFailure(escaped, JsonConformanceProfile.Jcs, scalar, "escaped");
        }
    }

    [Test]
    public void IJsonNoncharacterRuleAppliesToDecodedObjectMemberNames()
    {
        JsonDocumentResult raw = TryParse(
            "{\"﷐\":0}",
            JsonConformanceProfile.IJson);
        JsonDocumentResult escaped = TryParse(
            "{\"\\uFFFF\":0}",
            JsonConformanceProfile.IJson);

        Assert.Multiple(() =>
        {
            Assert.That(raw.Success, Is.False);
            Assert.That(raw.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.InvalidStringCharacter));
            Assert.That(escaped.Success, Is.False);
            Assert.That(escaped.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidStringCharacter));
        });
    }

    [Test]
    public void IJsonAcceptsUnicodeScalarsAdjacentToNoncharacterRanges()
    {
        int[] scalars = { 0xFDCF, 0xFDF0, 0xFFFD, 0x10000, 0x1FFFD, 0x10FFFD };

        foreach (int scalar in scalars)
        {
            string raw = "[\"" + char.ConvertFromUtf32(scalar) + "\"]";
            string escaped = "[\"" + EscapeScalar(scalar) + "\"]";
            Assert.That(
                TryParse(raw, JsonConformanceProfile.IJson).Success,
                Is.True,
                "raw U+" + scalar.ToString("X"));
            Assert.That(
                TryParse(escaped, JsonConformanceProfile.IJson).Success,
                Is.True,
                "escaped U+" + scalar.ToString("X"));
        }
    }

    static JsonDocument Parse(string text, JsonConformanceProfile profile)
    {
        JsonDocumentResult result = TryParse(text, profile);
        return result.Document
            ?? throw new InvalidOperationException(
                "Expected success for " + profile + ": " + result.Diagnostic?.Message);
    }

    static JsonDocumentResult TryParse(string text, JsonConformanceProfile profile)
    {
        byte[] input = System.Text.Encoding.UTF8.GetBytes(text);
        IJsonDocumentParser parser = new JsonDocumentParser();
        return parser.TryParseUtf8(input, CreateProfileOptions(profile));
    }

    static JsonReaderOptions CreateProfileOptions(JsonConformanceProfile profile)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.ConformanceProfile = profile;
        if (profile != JsonConformanceProfile.SyntaxCompatible)
        {
            options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
            options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
            options.BomPolicy = JsonBomPolicy.Reject;
        }

        return options;
    }

    static List<int> CreateUnicodeNoncharacters()
    {
        List<int> scalars = new List<int>();
        for (int scalar = 0xFDD0; scalar <= 0xFDEF; scalar += 1)
        {
            scalars.Add(scalar);
        }

        for (int plane = 0; plane <= 16; plane += 1)
        {
            scalars.Add((plane << 16) | 0xFFFE);
            scalars.Add((plane << 16) | 0xFFFF);
        }

        return scalars;
    }

    static string EscapeScalar(int scalar)
    {
        if (scalar <= 0xFFFF)
        {
            return "\\u" + scalar.ToString("X4");
        }

        string utf16 = char.ConvertFromUtf32(scalar);
        return "\\u" + ((int)utf16[0]).ToString("X4") +
            "\\u" + ((int)utf16[1]).ToString("X4");
    }

    static void AssertNoncharacterFailure(
        string text,
        JsonConformanceProfile profile,
        int scalar,
        string encoding)
    {
        JsonDocumentResult result = TryParse(text, profile);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False,
                encoding + " " + profile + " U+" + scalar.ToString("X"));
            Assert.That(result.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidStringCharacter),
                encoding + " " + profile + " U+" + scalar.ToString("X"));
        });
    }
}
