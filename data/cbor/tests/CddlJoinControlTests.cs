using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlJoinControlTests
{
    [Test]
    public void MatchesTheRfc9741Ipv4Example()
    {
        string source =
            "root = tstr .join address-elements\n"
            + "address-elements = [bytetext, \".\", bytetext, \".\", "
            + "bytetext, \".\", bytetext]\n"
            + "bytetext = tstr .base10 byte\n"
            + "byte = 0..255";

        AssertValid(source, Text("192.0.2.1"));
        AssertValid(source, Text("255.255.255.255"));
        AssertInvalid(source, Text("256.0.2.1"));
        AssertInvalid(source, Text("192.0.2"));
    }

    [Test]
    public void EmptyControllerMatchesEitherEmptyStringKind()
    {
        AssertValid("root = tstr .join []", Text(""));
        AssertValid("root = bstr .join []", Bytes());
        AssertInvalid("root = tstr .join []", Text("x"));
    }

    [Test]
    public void EnforcesThatTheFirstElementDeterminesTheResultKind()
    {
        AssertValid(
            "root = tstr .join [\"\", h'c3a9']",
            Text("é"));
        AssertValid(
            "root = bstr .join [h'c3', h'a9']",
            Bytes(0xc3, 0xa9));
        AssertInvalid(
            "root = tstr .join [h'c3', h'a9']",
            Text("é"));
        AssertInvalid(
            "root = bstr .join [\"\", h'c3a9']",
            Bytes(0xc3, 0xa9));
    }

    [Test]
    public void AllowsByteComponentsToSplitAValidUtf8Result()
    {
        AssertValid(
            "root = tstr .join [\"\", h'c3', h'a9']",
            Text("é"));
        AssertInvalid(
            "root = tstr .join [\"\", h'c3', h'a8']",
            Text("é"));
    }

    [Test]
    public void ExploresAmbiguousAdjacentVariableElements()
    {
        AssertValid(
            "root = tstr .join [tstr, tstr, \"!\"]",
            Text("ambiguous!"));
        AssertValid(
            "root = tstr .join [1*3 tstr]",
            Text("abc"));
    }

    [Test]
    public void SupportsGroupChoicesAndReferencedControllerArrays()
    {
        string source =
            "root = tstr .join pieces\n"
            + "pieces = [choice]\n"
            + "choice = (\"a\", \"b\" // \"ab\",)";

        AssertValid(source, Text("ab"));
        AssertInvalid(source, Text("ac"));
        AssertValid(
            "root = tstr .join ([\"a\"] / [\"b\"])",
            Text("b"));
    }

    [Test]
    public void RejectsNonStringControllerElementsAndInvalidTargetText()
    {
        AssertInvalid("root = tstr .join [1]", Text("1"));
        CddlValidationResult invalidText = Validate(
            "root = tstr .join [tstr]",
            Text("\ud800"),
            new CddlValidationOptions());

        Assert.That(invalidText.IsValid, Is.False);
        Assert.That(invalidText.Issues[0].Code,
            Is.EqualTo(CddlValidationCode.ControlMismatch));
    }

    [Test]
    public void AppliesAlternativeAndNestingBudgets()
    {
        CddlValidationResult alternatives = Validate(
            "root = tstr .join [tstr, tstr]",
            Text("abcdef"),
            new CddlValidationOptions { MaxAlternatives = 1 });
        CddlValidationResult nesting = Validate(
            "root = tstr .join [(((tstr, tstr)))]",
            Text("ab"),
            new CddlValidationOptions { MaxNestingDepth = 2 });

        AssertResourceLimit(alternatives);
        AssertResourceLimit(nesting);
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
        Assert.That(result.Issues, Is.Not.Empty);
    }

    private static CddlValidationResult Validate(
        string source,
        CborValue value,
        CddlValidationOptions options)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "join-control.cddl" });
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
            value,
            options);
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
