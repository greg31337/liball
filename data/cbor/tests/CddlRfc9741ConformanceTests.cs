using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlRfc9741ConformanceTests
{
    [TestCase(".b64u", "", "")]
    [TestCase(".b64u", "Zg", "66")]
    [TestCase(".b64u", "Zm8", "666f")]
    [TestCase(".b64u", "Zm9v", "666f6f")]
    [TestCase(".b64u", "Zm9vYg", "666f6f62")]
    [TestCase(".b64u", "Zm9vYmE", "666f6f6261")]
    [TestCase(".b64u", "Zm9vYmFy", "666f6f626172")]
    [TestCase(".b64u", "-_8", "fbff")]
    [TestCase(".b64c", "", "")]
    [TestCase(".b64c", "Zg==", "66")]
    [TestCase(".b64c", "Zm8=", "666f")]
    [TestCase(".b64c", "Zm9v", "666f6f")]
    [TestCase(".b64c", "Zm9vYg==", "666f6f62")]
    [TestCase(".b64c", "Zm9vYmE=", "666f6f6261")]
    [TestCase(".b64c", "Zm9vYmFy", "666f6f626172")]
    [TestCase(".b64c", "+/8=", "fbff")]
    [TestCase(".b32", "", "")]
    [TestCase(".b32", "MY", "66")]
    [TestCase(".b32", "MZXQ", "666f")]
    [TestCase(".b32", "MZXW6", "666f6f")]
    [TestCase(".b32", "MZXW6YQ", "666f6f62")]
    [TestCase(".b32", "MZXW6YTB", "666f6f6261")]
    [TestCase(".b32", "MZXW6YTBOI", "666f6f626172")]
    [TestCase(".h32", "", "")]
    [TestCase(".h32", "CO", "66")]
    [TestCase(".h32", "CPNG", "666f")]
    [TestCase(".h32", "CPNMU", "666f6f")]
    [TestCase(".h32", "CPNMUOG", "666f6f62")]
    [TestCase(".h32", "CPNMUOJ1", "666f6f6261")]
    [TestCase(".h32", "CPNMUOJ1E8", "666f6f626172")]
    public void MatchesRfc4648VectorsForEveryEncodingRemainder(
        string control,
        string encoded,
        string expectedHex)
    {
        AssertValid(
            "root = tstr " + control + " h'" + expectedHex + "'",
            Text(encoded));
    }

    [TestCase(".b64u", "Zh", false)]
    [TestCase(".b64u-sloppy", "Zh", true)]
    [TestCase(".b64u", "Zm9", false)]
    [TestCase(".b64u-sloppy", "Zm9", true)]
    [TestCase(".b64c", "Zh==", false)]
    [TestCase(".b64c-sloppy", "Zh==", true)]
    [TestCase(".b64c", "Zm9=", false)]
    [TestCase(".b64c-sloppy", "Zm9=", true)]
    public void SloppyBase64RelaxesOnlyTheUnusedBitRule(
        string control,
        string encoded,
        bool expected)
    {
        CddlValidationResult result = Validate(
            "root = tstr " + control + " (h'66' / h'666f')",
            Text(encoded),
            new CddlValidationOptions());

        Assert.That(result.IsValid, Is.EqualTo(expected));
    }

    [TestCase(".b64u", "Zg==")]
    [TestCase(".b64u-sloppy", "Zg==")]
    [TestCase(".b64u", "+w")]
    [TestCase(".b64u-sloppy", "/w")]
    [TestCase(".b64u", "A")]
    [TestCase(".b64u-sloppy", "A")]
    [TestCase(".b64c", "Zg")]
    [TestCase(".b64c-sloppy", "Zg")]
    [TestCase(".b64c", "-w==")]
    [TestCase(".b64c-sloppy", "_w==")]
    [TestCase(".b64c", "Zg===")]
    [TestCase(".b64c-sloppy", "Zg===")]
    [TestCase(".b64c", "=m9v")]
    public void StrictAndSloppyBase64EnforceAlphabetLengthAndPadding(
        string control,
        string encoded)
    {
        AssertInvalid(
            "root = tstr " + control + " bstr",
            Text(encoded));
    }

    [TestCase(".b32", "MZ")]
    [TestCase(".b32", "MZXR")]
    [TestCase(".b32", "MZXW7")]
    [TestCase(".b32", "MZXW6YR")]
    [TestCase(".h32", "CP")]
    [TestCase(".h32", "CPNH")]
    [TestCase(".h32", "CPNMV")]
    [TestCase(".h32", "CPNMUOH")]
    public void Base32RejectsNonzeroUnusedBitsAtEveryRemainder(
        string control,
        string encoded)
    {
        AssertInvalid(
            "root = tstr " + control + " bstr",
            Text(encoded));
    }

    [TestCase(".b32", "M")]
    [TestCase(".b32", "MZX")]
    [TestCase(".b32", "MZXW6Y")]
    [TestCase(".b32", "MY======")]
    [TestCase(".b32", "my")]
    [TestCase(".b32", "M0")]
    [TestCase(".h32", "C")]
    [TestCase(".h32", "CPN")]
    [TestCase(".h32", "CPNMUO")]
    [TestCase(".h32", "CO======")]
    [TestCase(".h32", "co")]
    [TestCase(".h32", "CW")]
    public void Base32EnforcesUnpaddedLengthAndUppercaseAlphabet(
        string control,
        string encoded)
    {
        AssertInvalid(
            "root = tstr " + control + " bstr",
            Text(encoded));
    }

    [TestCase("", "")]
    [TestCase("QED8WEX0", "6965746621")]
    [TestCase("UJCLQE7W581", "626173652d3435")]
    [TestCase("%69 VD92EX0", "48656c6c6f2121")]
    public void MatchesRfc9285Base45Vectors(
        string encoded,
        string expectedHex)
    {
        AssertValid(
            "root = tstr .b45 h'" + expectedHex + "'",
            Text(encoded));
    }

    [TestCase("A")]
    [TestCase("::")]
    [TestCase(":::")]
    [TestCase("abc")]
    [TestCase("00?")]
    public void Base45RejectsIncompleteOverflowAndWrongCaseForms(
        string encoded)
    {
        AssertInvalid("root = tstr .b45 bstr", Text(encoded));
    }

    [Test]
    public void HexControlsApplyTheirSpecifiedCaseRules()
    {
        Assert.Multiple(() =>
        {
            AssertValid("root = tstr .hex h'deadbeef'", Text("DeAdBeEf"));
            AssertValid("root = tstr .hexlc h'deadbeef'", Text("deadbeef"));
            AssertValid("root = tstr .hexuc h'deadbeef'", Text("DEADBEEF"));
            AssertInvalid("root = tstr .hexlc bstr", Text("deAd"));
            AssertInvalid("root = tstr .hexuc bstr", Text("DEaD"));
            AssertInvalid("root = tstr .hex bstr", Text("abc"));
            AssertInvalid("root = tstr .hex bstr", Text("0g"));
        });
    }

    [Test]
    public void Base10MatchesExactlyTheRfc9741CanonicalGrammar()
    {
        Assert.Multiple(() =>
        {
            AssertValid("root = tstr .base10 int", Text("0"));
            AssertValid("root = tstr .base10 int", Text("-1"));
            AssertValid(
                "root = tstr .base10 -18446744073709551617",
                Text("-18446744073709551617"));
            AssertValid("root = tstr .base10 (0..255)", Text("255"));
            AssertInvalid("root = tstr .base10 (0..255)", Text("256"));
            AssertInvalid("root = tstr .base10 int", Text(""));
            AssertInvalid("root = tstr .base10 int", Text("-0"));
            AssertInvalid("root = tstr .base10 int", Text("+1"));
            AssertInvalid("root = tstr .base10 int", Text("00"));
            AssertInvalid("root = tstr .base10 int", Text("01"));
            AssertInvalid("root = tstr .base10 int", Text("1.0"));
            AssertInvalid("root = tstr .base10 int", Text(" 1"));
        });
    }

    [TestCase("%d", "-42")]
    [TestCase("%i", "42")]
    [TestCase("%u", "42")]
    [TestCase("%o", "52")]
    [TestCase("%x", "2a")]
    [TestCase("%X", "2A")]
    [TestCase("%b", "101010")]
    [TestCase("%B", "101010")]
    public void PrintfSupportsEveryIntegerConversion(
        string format,
        string expected)
    {
        int argument = format == "%d" ? -42 : 42;
        AssertValid(
            "root = tstr .printf ([\"" + format + "\", "
                + argument + "])",
            Text(expected));
    }

    [TestCase("%+06d", "42", "+00042")]
    [TestCase("% 5d", "7", "    7")]
    [TestCase("%-05d", "12", "12   ")]
    [TestCase("%08.4d", "12", "    0012")]
    [TestCase("%.0d", "0", "")]
    [TestCase("%#.0o", "0", "0")]
    [TestCase("%#x", "255", "0xff")]
    [TestCase("%#X", "255", "0XFF")]
    [TestCase("%#b", "5", "0b101")]
    [TestCase("%#B", "5", "0B101")]
    public void PrintfAppliesFlagsWidthAndPrecisionLikeC(
        string format,
        string argument,
        string expected)
    {
        AssertValid(
            "root = tstr .printf ([\"" + format + "\", "
                + argument + "])",
            Text(expected));
    }

    [Test]
    public void PrintfInterpolatesUtf8TextScalarsAndLiteralPercent()
    {
        Assert.Multiple(() =>
        {
            AssertValid(
                "root = tstr .printf ([\"%.2s\", \"éx\"])",
                Text("é"));
            AssertValid(
                "root = tstr .printf ([\"%-5s\", \"é\"])",
                Text("é   "));
            AssertValid(
                "root = tstr .printf ([\"[%6c]\", 128512])",
                Text("[  😀]"));
            AssertValid(
                "root = tstr .printf ([\"%c\", 1114111])",
                Text(char.ConvertFromUtf32(1114111)));
            AssertValid(
                "root = tstr .printf ([\"%s: %d%%\", \"done\", 100])",
                Text("done: 100%"));
            AssertValid(
                "root = tstr .printf ([\"100%% complete\"])",
                Text("100% complete"));
        });
    }

    [Test]
    public void PrintfSearchesTypedArgumentRangesAndAmbiguousFields()
    {
        string source =
            "root = label<1..20>\n"
            + "label<K> = tstr .printf ([\"0x%04x\", K])";

        Assert.Multiple(() =>
        {
            AssertValid(source, Text("0x0001"));
            AssertValid(source, Text("0x0013"));
            AssertInvalid(source, Text("0x1234"));
            AssertValid(
                "root = tstr .printf ([\"%d%d\", 1..12, 0..99])",
                Text("123"));
            AssertInvalid(
                "root = tstr .printf ([\"%d%d\", 1..12, 0..2])",
                Text("123"));
        });
    }

    [TestCase("%f")]
    [TestCase("%a")]
    [TestCase("%p")]
    [TestCase("%n")]
    [TestCase("%ld")]
    [TestCase("%*d")]
    [TestCase("%.*d")]
    [TestCase("%0s")]
    [TestCase("%.1c")]
    [TestCase("%")]
    public void PrintfRejectsUnsupportedOrUndefinedDirectives(string format)
    {
        CddlValidationResult result = Validate(
            "root = tstr .printf ([\"" + format + "\", 1])",
            Text("anything"),
            new CddlValidationOptions());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues[0].Code,
            Is.EqualTo(CddlValidationCode.InvalidSchema));
    }

    [Test]
    public void JsonMapsEveryJsonDataModelKindAndIgnoresWhitespace()
    {
        string source =
            "root = tstr .json [true, false, null, tstr, uint, "
            + "float16, {\"x\": uint}]";
        string json =
            " \r\n [ true, false, null, \"A\\n😀\", 10, "
            + "1.5, { \"x\" : 2 } ] \t";

        AssertValid(source, Text(json));
        AssertInvalid("root = tstr .json bstr", Text("\"AQ\""));
    }

    [Test]
    public void JsonMapsIntegralFormsExactlyAndFractionalFormsToBinaryFloats()
    {
        Assert.Multiple(() =>
        {
            AssertValid("root = tstr .json 10", Text("10"));
            AssertValid("root = tstr .json 10", Text("10.0"));
            AssertValid("root = tstr .json 10", Text("1e1"));
            AssertValid("root = tstr .json 10", Text("100e-1"));
            AssertValid(
                "root = tstr .json 18446744073709551616",
                Text("184467440737095516160e-1"));
            AssertValid("root = tstr .json float16", Text("1.5"));
            AssertValid(
                "root = tstr .json float32",
                Text("1.00000011920928955078125"));
            AssertValid(
                "root = tstr .json float64",
                Text("1.00000011920928955078125"));
            AssertInvalid("root = tstr .json float16", Text("1.1"));
        });
    }

    [Test]
    public void JsonObjectsHaveTextKeysAndPreserveDuplicateMembers()
    {
        Assert.Multiple(() =>
        {
            AssertValid(
                "root = tstr .json {* tstr => uint}",
                Text("{\"x\":1,\"x\":2}"));
            AssertValid(
                "root = tstr .json {\"x\": 1, \"y\": 2}",
                Text("{\"y\":2,\"x\":1}"));
            AssertInvalid(
                "root = tstr .json {1: 1}",
                Text("{\"1\":1}"));
        });
    }

    [TestCase("")]
    [TestCase("1 2")]
    [TestCase("[1,]")]
    [TestCase("{\"x\":1,}")]
    [TestCase("01")]
    [TestCase("NaN")]
    [TestCase("/* comment */ 1")]
    [TestCase("\"\\uD800\"")]
    public void JsonRejectsAnythingOtherThanOneValidJsonValue(string json)
    {
        AssertInvalid("root = tstr .json any", Text(json));
    }

    [Test]
    public void JoinMatchesTheRfcIpv4ExampleAndComponentControllers()
    {
        string source =
            "root = tstr .join address-elements\n"
            + "address-elements = [bytetext, \".\", bytetext, \".\", "
            + "bytetext, \".\", bytetext]\n"
            + "bytetext = tstr .base10 byte\n"
            + "byte = 0..255";

        Assert.Multiple(() =>
        {
            AssertValid(source, Text("192.0.2.1"));
            AssertValid(source, Text("255.255.255.255"));
            AssertInvalid(source, Text("256.0.2.1"));
            AssertInvalid(source, Text("192.0.2"));
        });
    }

    [Test]
    public void JoinUsesTheFirstKindAndConcatenatesComponentBytes()
    {
        Assert.Multiple(() =>
        {
            AssertValid("root = tstr .join []", Text(""));
            AssertValid("root = bstr .join []", Bytes());
            AssertValid(
                "root = tstr .join [\"\", h'c3', h'a9']",
                Text("é"));
            AssertValid(
                "root = bstr .join [h'c3', \"é\"]",
                Bytes(0xc3, 0xc3, 0xa9));
            AssertInvalid(
                "root = tstr .join [h'c3', h'a9']",
                Text("é"));
            AssertInvalid(
                "root = bstr .join [\"\", h'c3a9']",
                Bytes(0xc3, 0xa9));
        });
    }

    [Test]
    public void JoinExploresAmbiguousChoicesOccurrencesAndSplits()
    {
        Assert.Multiple(() =>
        {
            AssertValid(
                "root = tstr .join [tstr .size 2, tstr .size 1]",
                Text("abc"));
            AssertValid(
                "root = tstr .join [1*3 tstr]",
                Text("abc"));
            AssertValid(
                "root = tstr .join ([\"a\", \"b\"] / [\"ab\"])",
                Text("ab"));
            AssertValid(
                "root = tstr .join [(\"a\", \"b\" // \"ab\",)]",
                Text("ab"));
            AssertInvalid(
                "root = tstr .join [tstr .size 2, tstr .size 2]",
                Text("abc"));
        });
    }

    [Test]
    public void EveryRfc9741MatcherHonorsSharedResourceLimits()
    {
        CddlValidationResult conversion = Validate(
            "root = tstr .hex bstr",
            Text("0001"),
            new CddlValidationOptions { MaxEmbeddedBytes = 1 });
        CddlValidationResult printfStates = Validate(
            "root = tstr .printf ([\"value=%d\", int])",
            Text("value=123"),
            new CddlValidationOptions { MaxPatternStates = 2 });
        CddlValidationResult printfAlternatives = Validate(
            "root = tstr .printf ([\"%d%d\", int, int])",
            Text("12345"),
            new CddlValidationOptions { MaxAlternatives = 1 });
        CddlValidationResult json = Validate(
            "root = tstr .json any",
            Text("[[1]]"),
            new CddlValidationOptions { MaxNestingDepth = 1 });
        CddlValidationResult join = Validate(
            "root = tstr .join [tstr, tstr]",
            Text("abcdef"),
            new CddlValidationOptions { MaxAlternatives = 1 });

        Assert.Multiple(() =>
        {
            AssertResourceLimit(conversion);
            AssertResourceLimit(printfStates);
            AssertResourceLimit(printfAlternatives);
            AssertResourceLimit(json);
            AssertResourceLimit(join);
        });
    }

    private static void AssertResourceLimit(CddlValidationResult result)
    {
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues.Select(item => item.Code),
            Does.Contain(CddlValidationCode.ResourceLimit));
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
    }

    private static CddlValidationResult Validate(
        string source,
        CborValue value,
        CddlValidationOptions options)
    {
        CddlDocument document = new CddlParser().ParseDocument(
            source,
            new CddlParseOptions { SourceName = "rfc9741.cddl" });
        CddlSchema schema = new CddlCompiler().CompileSchema(
            document,
            new CddlCompileOptions());
        return new CddlValidator().ValidateCbor(schema, value, options);
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
