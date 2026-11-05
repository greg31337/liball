namespace uri.Tests;

public class PathParserTests
{
    [TestCase("abempty", "", 0)]
    [TestCase("abempty", "/", 1)]
    [TestCase("abempty", "//", 2)]
    [TestCase("abempty", "/a", 1)]
    [TestCase("abempty", "/a/", 2)]
    [TestCase("abempty", "/a//b", 3)]
    [TestCase("abempty", "/a:b@c", 1)]
    [TestCase("abempty", "/%2F/%3A", 2)]
    [TestCase("absolute", "/", 0)]
    [TestCase("absolute", "/a", 1)]
    [TestCase("absolute", "/a/", 2)]
    [TestCase("absolute", "/a//b", 3)]
    [TestCase("absolute", "/:a", 1)]
    [TestCase("absolute", "/%2F", 1)]
    [TestCase("noscheme", "a", 1)]
    [TestCase("noscheme", "@", 1)]
    [TestCase("noscheme", "%3A", 1)]
    [TestCase("noscheme", "a%3Ab", 1)]
    [TestCase("noscheme", "a/b:c", 2)]
    [TestCase("noscheme", "a//", 3)]
    [TestCase("rootless", "a", 1)]
    [TestCase("rootless", ":a", 1)]
    [TestCase("rootless", "a:b", 1)]
    [TestCase("rootless", "a/b:c", 2)]
    [TestCase("rootless", "a//", 3)]
    [TestCase("empty", "", 0)]
    public void ValidProductionFormsReturnTheirGrammarSegmentCount(
        string production,
        string text,
        int expectedSegments)
    {
        PathParseResult result = Parse(production, text, 20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, production + ":" + text);
            Assert.That(result.SegmentCount, Is.EqualTo(expectedSegments));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void EveryRawPcharIsAcceptedInApplicableSegments()
    {
        string pchar =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~!$&'()*+,;=:@";
        string noColon =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~!$&'()*+,;=@";

        Assert.Multiple(() =>
        {
            Assert.That(PathParser.ParseAbEmpty(("/" + pchar).AsSpan(), 0).Success, Is.True);
            Assert.That(PathParser.ParseAbsolute(("/" + pchar).AsSpan(), 0).Success, Is.True);
            Assert.That(PathParser.ParseNoScheme((noColon + "/" + pchar).AsSpan(), 0).Success, Is.True);
            Assert.That(PathParser.ParseRootless(pchar.AsSpan(), 0).Success, Is.True);
        });
    }

    [Test]
    public void EveryPercentEncodedByteIsAcceptedWithoutCreatingDelimiters()
    {
        int value = 0;
        while (value <= 255)
        {
            string triplet = "%" + value.ToString("X2");

            PathParseResult abEmpty = PathParser.ParseAbEmpty(("/" + triplet).AsSpan(), 0);
            PathParseResult absolute = PathParser.ParseAbsolute(("/" + triplet).AsSpan(), 0);
            PathParseResult noScheme = PathParser.ParseNoScheme(triplet.AsSpan(), 0);
            PathParseResult rootless = PathParser.ParseRootless(triplet.AsSpan(), 0);

            Assert.Multiple(() =>
            {
                Assert.That(abEmpty.Success, Is.True, triplet);
                Assert.That(absolute.Success, Is.True, triplet);
                Assert.That(noScheme.Success, Is.True, triplet);
                Assert.That(rootless.Success, Is.True, triplet);
                Assert.That(abEmpty.SegmentCount, Is.EqualTo(1), triplet);
                Assert.That(noScheme.SegmentCount, Is.EqualTo(1), triplet);
            });
            value = value + 1;
        }
    }

    [TestCase("abempty", "a", 10, 1)]
    [TestCase("absolute", "", 10, 0)]
    [TestCase("absolute", "a", 10, 1)]
    [TestCase("absolute", "//host", 11, 1)]
    [TestCase("absolute", "///", 11, 1)]
    [TestCase("noscheme", "", 10, 0)]
    [TestCase("noscheme", "/a", 10, 1)]
    [TestCase("rootless", "", 10, 0)]
    [TestCase("rootless", "/a", 10, 1)]
    [TestCase("empty", "/", 10, 1)]
    [TestCase("empty", "a", 10, 1)]
    public void EachProductionEnforcesItsLeadingAndEmptyShape(
        string production,
        string text,
        int expectedOffset,
        int expectedLength)
    {
        PathParseResult result = Parse(production, text, 10);

        AssertInvalidPath(result, expectedOffset, expectedLength);
    }

    [TestCase(":a", 10)]
    [TestCase("a:b", 11)]
    [TestCase("abc:def/ghi", 13)]
    [TestCase("a%2Fb:c", 15)]
    public void PathNoSchemeRejectsRawColonOnlyInTheFirstSegment(
        string text,
        int expectedOffset)
    {
        PathParseResult result = PathParser.ParseNoScheme(text.AsSpan(), 10);

        AssertInvalidPath(result, expectedOffset, 1);
    }

    [TestCase("/%", 11)]
    [TestCase("/%0", 11)]
    [TestCase("/%G0", 12)]
    [TestCase("/%0G", 13)]
    [TestCase("/ok/%G0", 15)]
    public void MalformedAbsolutePercentTripletsReturnExactUriDiagnostics(
        string text,
        int expectedOffset)
    {
        PathParseResult result = PathParser.ParseAbsolute(text.AsSpan(), 10);

        AssertPercentFailure(result, expectedOffset);
    }

    [TestCase("%", 10)]
    [TestCase("%0", 10)]
    [TestCase("%G0", 11)]
    [TestCase("%0G", 12)]
    [TestCase("a/%0G", 14)]
    public void MalformedRelativePercentTripletsReturnExactUriDiagnostics(
        string text,
        int expectedOffset)
    {
        PathParseResult noScheme = PathParser.ParseNoScheme(text.AsSpan(), 10);
        PathParseResult rootless = PathParser.ParseRootless(text.AsSpan(), 10);

        AssertPercentFailure(noScheme, expectedOffset);
        AssertPercentFailure(rootless, expectedOffset);
    }

    [Test]
    public void NonPcharCharactersAreRejectedInEveryNonemptyProduction()
    {
        string invalid = "?#[] \\`\"<>\u0000\u007f\u0080\u00e9";
        int index = 0;
        while (index < invalid.Length)
        {
            char value = invalid[index];
            PathParseResult abEmpty = PathParser.ParseAbEmpty(("/a" + value).AsSpan(), 30);
            PathParseResult absolute = PathParser.ParseAbsolute(("/a" + value).AsSpan(), 30);
            PathParseResult noScheme = PathParser.ParseNoScheme(("a" + value).AsSpan(), 30);
            PathParseResult rootless = PathParser.ParseRootless(("a" + value).AsSpan(), 30);

            AssertInvalidPath(abEmpty, 32, 1);
            AssertInvalidPath(absolute, 32, 1);
            AssertInvalidPath(noScheme, 31, 1);
            AssertInvalidPath(rootless, 31, 1);
            index = index + 1;
        }
    }

    [Test]
    public void FirstErrorWinsAndOffsetsRemainStableAtBoundaries()
    {
        PathParseResult first = PathParser.ParseNoScheme("a:b?%".AsSpan(), 50);
        PathParseResult relative = PathParser.ParseRootless("a?b".AsSpan(), -1);
        PathParseResult saturated = PathParser.ParseRootless(
            "a?b".AsSpan(),
            int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(first.Diagnostic, Is.Not.Null);
            Assert.That(first.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidPath));
            Assert.That(first.Diagnostic.Range.Offset, Is.EqualTo(51));
            Assert.That(relative.Diagnostic, Is.Not.Null);
            Assert.That(relative.Diagnostic!.Range.Offset, Is.EqualTo(1));
            Assert.That(saturated.Diagnostic, Is.Not.Null);
            Assert.That(saturated.Diagnostic!.Range.Offset, Is.EqualTo(int.MaxValue));
        });
    }

    private static PathParseResult Parse(
        string production,
        string text,
        int baseOffset)
    {
        switch (production)
        {
            case "abempty":
                return PathParser.ParseAbEmpty(text.AsSpan(), baseOffset);
            case "absolute":
                return PathParser.ParseAbsolute(text.AsSpan(), baseOffset);
            case "noscheme":
                return PathParser.ParseNoScheme(text.AsSpan(), baseOffset);
            case "rootless":
                return PathParser.ParseRootless(text.AsSpan(), baseOffset);
            default:
                return PathParser.ParseEmpty(text.AsSpan(), baseOffset);
        }
    }

    private static void AssertInvalidPath(
        PathParseResult result,
        int expectedOffset,
        int expectedLength)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.SegmentCount, Is.Zero);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidPath));
            Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Path));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(expectedOffset));
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(expectedLength));
            Assert.That(result.Diagnostic.Message, Is.EqualTo(DiagnosticFactory.InvalidPathMessage));
        });
    }

    private static void AssertPercentFailure(PathParseResult result, int expectedOffset)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.SegmentCount, Is.Zero);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidPercentEncoding));
            Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Path));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(expectedOffset));
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(1));
            Assert.That(result.Diagnostic.Message, Is.EqualTo(DiagnosticFactory.InvalidPercentEncodingMessage));
        });
    }
}
