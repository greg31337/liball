namespace json.Tests;

public class JsonPointerCodecTests
{
    [Test]
    public void StringCodecMatchesRfc6901Examples()
    {
        IJsonPointerCodec codec = new JsonPointerCodec();

        Assert.Multiple(() =>
        {
            AssertStringExample(codec, "");
            AssertStringExample(codec, "/foo", "foo");
            AssertStringExample(codec, "/foo/0", "foo", "0");
            AssertStringExample(codec, "/", "");
            AssertStringExample(codec, "/a~1b", "a/b");
            AssertStringExample(codec, "/c%d", "c%d");
            AssertStringExample(codec, "/e^f", "e^f");
            AssertStringExample(codec, "/g|h", "g|h");
            AssertStringExample(codec, "/i\\j", "i\\j");
            AssertStringExample(codec, "/k\"l", "k\"l");
            AssertStringExample(codec, "/ ", " ");
            AssertStringExample(codec, "/m~0n", "m~n");
        });
    }

    [Test]
    public void UriFragmentCodecMatchesRfc6901Examples()
    {
        IJsonPointerCodec codec = new JsonPointerCodec();

        Assert.Multiple(() =>
        {
            AssertFragmentExample(codec, "#");
            AssertFragmentExample(codec, "#/foo", "foo");
            AssertFragmentExample(codec, "#/foo/0", "foo", "0");
            AssertFragmentExample(codec, "#/", "");
            AssertFragmentExample(codec, "#/a~1b", "a/b");
            AssertFragmentExample(codec, "#/c%25d", "c%d");
            AssertFragmentExample(codec, "#/e%5Ef", "e^f");
            AssertFragmentExample(codec, "#/g%7Ch", "g|h");
            AssertFragmentExample(codec, "#/i%5Cj", "i\\j");
            AssertFragmentExample(codec, "#/k%22l", "k\"l");
            AssertFragmentExample(codec, "#/%20", " ");
            AssertFragmentExample(codec, "#/m~0n", "m~n");
        });
    }

    [Test]
    public void StringCodecHandlesRootEmptyAndEscapedReferenceTokens()
    {
        IJsonPointerCodec codec = new JsonPointerCodec();

        JsonPointer root = codec.ParseStringPointer("");
        JsonPointer emptyToken = codec.ParseStringPointer("/");
        JsonPointer pointer = codec.ParseStringPointer(
            "/foo/0/a~1b/m~0n/~01");

        Assert.Multiple(() =>
        {
            AssertPointer(root, JsonPointerEncoding.String, "");
            Assert.That(root.ReferenceTokens, Is.Empty);
            Assert.That(codec.FormatStringPointer(root), Is.EqualTo(""));
            AssertPointer(emptyToken, JsonPointerEncoding.String, "/", "");
            Assert.That(codec.FormatStringPointer(emptyToken), Is.EqualTo("/"));
            AssertPointer(
                pointer,
                JsonPointerEncoding.String,
                "/foo/0/a~1b/m~0n/~01",
                "foo",
                "0",
                "a/b",
                "m~n",
                "~1");
            Assert.That(codec.FormatStringPointer(pointer),
                Is.EqualTo("/foo/0/a~1b/m~0n/~01"));
        });
    }

    [Test]
    public void TildeEscapesAreDecodedOnceFromLeftToRight()
    {
        IJsonPointerCodec codec = new JsonPointerCodec();
        const string representation = "/~01/~10/~00";

        JsonPointer pointer = codec.ParseStringPointer(representation);

        Assert.Multiple(() =>
        {
            AssertPointer(
                pointer,
                JsonPointerEncoding.String,
                representation,
                "~1",
                "/0",
                "~0");
            Assert.That(codec.FormatStringPointer(pointer),
                Is.EqualTo(representation));
        });
    }

    [Test]
    public void UriFragmentCodecUsesStrictUtf8AndRfcFragmentCharacters()
    {
        IJsonPointerCodec codec = new JsonPointerCodec();
        const string fragment =
            "#/c%25d/e%5Ef/g%7Ch/i%5Cj/k%22l/%20/%E2%82%AC/:@?&=";

        JsonPointer pointer = codec.ParseUriFragment(fragment);
        string formatted = codec.FormatUriFragment(pointer);

        Assert.Multiple(() =>
        {
            AssertPointer(
                pointer,
                JsonPointerEncoding.UriFragment,
                fragment,
                "c%d",
                "e^f",
                "g|h",
                "i\\j",
                "k\"l",
                " ",
                "€",
                ":@?&=");
            Assert.That(formatted, Is.EqualTo(fragment));
            Assert.That(codec.ParseUriFragment(formatted).ReferenceTokens,
                Is.EqualTo(pointer.ReferenceTokens));
        });
    }

    [Test]
    public void UriFragmentCodecRoundTripsSupplementaryTextAndNul()
    {
        IJsonPointerCodec codec = new JsonPointerCodec();
        JsonPointer source = CreatePointer("𝄞", "\0", "a/b", "m~n");

        string formatted = codec.FormatUriFragment(source);
        JsonPointer parsed = codec.ParseUriFragment(formatted);

        Assert.Multiple(() =>
        {
            Assert.That(formatted,
                Is.EqualTo("#/%F0%9D%84%9E/%00/a~1b/m~0n"));
            Assert.That(parsed.Encoding, Is.EqualTo(JsonPointerEncoding.UriFragment));
            Assert.That(parsed.OriginalRepresentation, Is.EqualTo(formatted));
            Assert.That(parsed.ReferenceTokens, Is.EqualTo(source.ReferenceTokens));
            Assert.That(codec.FormatStringPointer(parsed),
                Is.EqualTo("/𝄞/\0/a~1b/m~0n"));
        });
    }

    [Test]
    public void ParsingReportsRepresentationSyntaxFailuresPrecisely()
    {
        IJsonPointerCodec codec = new JsonPointerCodec();

        JsonPointerException? stringSyntax = Assert.Throws<JsonPointerException>(() =>
            codec.ParseStringPointer("not-a-pointer"));
        JsonPointerException? fragmentSyntax = Assert.Throws<JsonPointerException>(() =>
            codec.ParseUriFragment("/missing-hash"));
        JsonPointerException? rawSpace = Assert.Throws<JsonPointerException>(() =>
            codec.ParseUriFragment("#/raw space"));
        JsonPointerException? rawUnicode = Assert.Throws<JsonPointerException>(() =>
            codec.ParseUriFragment("#/€"));

        Assert.Multiple(() =>
        {
            Assert.That(stringSyntax?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidPointerSyntax));
            Assert.That(fragmentSyntax?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidPointerSyntax));
            Assert.That(rawSpace?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidPointerPercentEncoding));
            Assert.That(rawUnicode?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidPointerPercentEncoding));
        });
    }

    [TestCase("/~")]
    [TestCase("/~2")]
    [TestCase("/~x")]
    [TestCase("/a/~")]
    [TestCase("/a~b")]
    public void ParsingRejectsMalformedTildeEscapes(string representation)
    {
        IJsonPointerCodec codec = new JsonPointerCodec();

        JsonPointerException? exception = Assert.Throws<JsonPointerException>(() =>
            codec.ParseStringPointer(representation));

        Assert.That(exception?.Diagnostic.Code,
            Is.EqualTo(JsonDiagnosticCode.InvalidPointerEscape));
    }

    [TestCase("#/%")]
    [TestCase("#/%0")]
    [TestCase("#/%GG")]
    [TestCase("#/%0G")]
    [TestCase("#/%G0")]
    public void ParsingRejectsMalformedPercentEscapes(string fragment)
    {
        IJsonPointerCodec codec = new JsonPointerCodec();

        JsonPointerException? exception = Assert.Throws<JsonPointerException>(() =>
            codec.ParseUriFragment(fragment));

        Assert.That(exception?.Diagnostic.Code,
            Is.EqualTo(JsonDiagnosticCode.InvalidPointerPercentEncoding));
    }

    [TestCase("#/%80")]
    [TestCase("#/%C0%AF")]
    [TestCase("#/%E2%82")]
    [TestCase("#/%ED%A0%80")]
    [TestCase("#/%F4%90%80%80")]
    [TestCase("#/%F5%80%80%80")]
    public void ParsingRejectsInvalidFragmentUtf8(string fragment)
    {
        IJsonPointerCodec codec = new JsonPointerCodec();

        JsonPointerException? exception = Assert.Throws<JsonPointerException>(() =>
            codec.ParseUriFragment(fragment));

        Assert.That(exception?.Diagnostic.Code,
            Is.EqualTo(JsonDiagnosticCode.InvalidPointerPercentEncoding));
    }

    [Test]
    public void StringCodecPreservesUnicodeWithoutNormalization()
    {
        IJsonPointerCodec codec = new JsonPointerCodec();
        const string representation = "/é/é/日本語/𝄞";

        JsonPointer pointer = codec.ParseStringPointer(representation);

        Assert.Multiple(() =>
        {
            AssertPointer(
                pointer,
                JsonPointerEncoding.String,
                representation,
                "é",
                "é",
                "日本語",
                "𝄞");
            Assert.That(pointer.ReferenceTokens?[0],
                Is.Not.EqualTo(pointer.ReferenceTokens?[1]));
            Assert.That(codec.FormatStringPointer(pointer),
                Is.EqualTo(representation));
            Assert.That(codec.FormatUriFragment(pointer),
                Is.EqualTo("#/%C3%A9/e%CC%81/%E6%97%A5%E6%9C%AC%E8%AA%9E/%F0%9D%84%9E"));
        });
    }

    [Test]
    public void FormattingRejectsMalformedPublicPointerModels()
    {
        IJsonPointerCodec codec = new JsonPointerCodec();
        JsonPointer missingTokens = new JsonPointer();
        JsonPointer nullToken = new JsonPointer();
        nullToken.ReferenceTokens = new List<string> { null! };
        JsonPointer loneSurrogate = CreatePointer(new string(new[] { '\ud800' }));

        JsonPointerException? nullPointer = Assert.Throws<JsonPointerException>(() =>
            codec.FormatStringPointer(null!));
        JsonPointerException? missing = Assert.Throws<JsonPointerException>(() =>
            codec.FormatUriFragment(missingTokens));
        JsonPointerException? nullMember = Assert.Throws<JsonPointerException>(() =>
            codec.FormatStringPointer(nullToken));
        JsonPointerException? invalidUnicode = Assert.Throws<JsonPointerException>(() =>
            codec.FormatUriFragment(loneSurrogate));

        Assert.Multiple(() =>
        {
            Assert.That(nullPointer?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidPointerSyntax));
            Assert.That(missing?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidPointerSyntax));
            Assert.That(nullMember?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidPointerSyntax));
            Assert.That(invalidUnicode?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidPointerSyntax));
        });
    }

    static JsonPointer CreatePointer(params string[] tokens)
    {
        JsonPointer pointer = new JsonPointer();
        pointer.Encoding = JsonPointerEncoding.String;
        pointer.ReferenceTokens = new List<string>(tokens);
        return pointer;
    }

    static void AssertStringExample(
        IJsonPointerCodec codec,
        string representation,
        params string[] tokens)
    {
        JsonPointer pointer = codec.ParseStringPointer(representation);
        AssertPointer(pointer, JsonPointerEncoding.String, representation, tokens);
        Assert.That(codec.FormatStringPointer(pointer), Is.EqualTo(representation));
    }

    static void AssertFragmentExample(
        IJsonPointerCodec codec,
        string representation,
        params string[] tokens)
    {
        JsonPointer pointer = codec.ParseUriFragment(representation);
        AssertPointer(pointer, JsonPointerEncoding.UriFragment, representation, tokens);
        Assert.That(codec.FormatUriFragment(pointer), Is.EqualTo(representation));
    }

    static void AssertPointer(
        JsonPointer pointer,
        JsonPointerEncoding encoding,
        string representation,
        params string[] tokens)
    {
        Assert.That(pointer.Encoding, Is.EqualTo(encoding));
        Assert.That(pointer.OriginalRepresentation, Is.EqualTo(representation));
        Assert.That(pointer.ReferenceTokens, Is.EqualTo(tokens));
    }
}
