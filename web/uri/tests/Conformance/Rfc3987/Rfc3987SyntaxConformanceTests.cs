namespace uri.Tests;

public class Rfc3987SyntaxConformanceTests
{
    private static readonly int[] UcsCharacterEndpoints = new int[]
    {
        0x00A0, 0xD7FF,
        0xF900, 0xFDCF,
        0xFDF0, 0xFFEF,
        0x10000, 0x1FFFD,
        0x20000, 0x2FFFD,
        0x30000, 0x3FFFD,
        0x40000, 0x4FFFD,
        0x50000, 0x5FFFD,
        0x60000, 0x6FFFD,
        0x70000, 0x7FFFD,
        0x80000, 0x8FFFD,
        0x90000, 0x9FFFD,
        0xA0000, 0xAFFFD,
        0xB0000, 0xBFFFD,
        0xC0000, 0xCFFFD,
        0xD0000, 0xDFFFD,
        0xE1000, 0xEFFFD
    };

    private static readonly int[] ExcludedScalarEndpoints = new int[]
    {
        0x0080, 0x009F,
        0xFDD0, 0xFDEF,
        0xFFF0, 0xFFFF,
        0x1FFFE, 0x1FFFF,
        0x2FFFE, 0x2FFFF,
        0x3FFFE, 0x3FFFF,
        0x4FFFE, 0x4FFFF,
        0x5FFFE, 0x5FFFF,
        0x6FFFE, 0x6FFFF,
        0x7FFFE, 0x7FFFF,
        0x8FFFE, 0x8FFFF,
        0x9FFFE, 0x9FFFF,
        0xAFFFE, 0xAFFFF,
        0xBFFFE, 0xBFFFF,
        0xCFFFE, 0xCFFFF,
        0xDFFFE, 0xDFFFF,
        0xE0000, 0xE0FFF,
        0xEFFFE, 0xEFFFF,
        0xFFFFE, 0xFFFFF,
        0x10FFFE, 0x10FFFF
    };

    private static readonly int[] PrivateCharacterEndpoints = new int[]
    {
        0xE000, 0xF8FF,
        0xF0000, 0xFFFFD,
        0x100000, 0x10FFFD
    };

    [TestCase("http://résumé.example.org")]
    [TestCase("http://validator.w3.org/check?uri=http%3A%2F%2Frésumé.example.org")]
    [TestCase("http://www.example.org/red%09rosé#red")]
    [TestCase("http://example.com/𐌀𐌁𐌂")]
    [TestCase("http://www.example.org/Dürst")]
    [TestCase("http://納豆.example.org/%E2%80%AE")]
    [TestCase("http://www.example.org/résumé.html")]
    public void PublishedRfc3987IriExamplesParseLosslessly(string text)
    {
        IIriParser parser = new IriParser();

        IriParseResult result = parser.TryParseIriReference(
            text,
            new IriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.Reference.OriginalText, Is.EqualTo(text));
            Assert.That(result.Reference.HasScheme, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [TestCase("//例え.テスト/道", UriReferenceKind.NetworkPathReference)]
    [TestCase("/道", UriReferenceKind.AbsolutePathReference)]
    [TestCase("道/先:端", UriReferenceKind.RelativePathReference)]
    [TestCase("?問\uE000", UriReferenceKind.RelativeReference)]
    [TestCase("#片", UriReferenceKind.SameDocumentReference)]
    [TestCase("", UriReferenceKind.EmptyReference)]
    public void Rfc3987RelativeReferenceAlternativesUseFirstMatch(
        string text,
        UriReferenceKind kind)
    {
        IIriParser parser = new IriParser();

        IriParseResult result = parser.TryParseIriReference(
            text,
            new IriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.Reference.Kind, Is.EqualTo(kind), text);
            Assert.That(result.Reference.HasScheme, Is.False, text);
            Assert.That(result.Reference.OriginalText, Is.EqualTo(text), text);
        });
    }

    [TestCase(0x00A0, 0xD7FF)]
    [TestCase(0xF900, 0xFDCF)]
    [TestCase(0xFDF0, 0xFFEF)]
    [TestCase(0x10000, 0x1FFFD)]
    [TestCase(0x20000, 0x2FFFD)]
    [TestCase(0x30000, 0x3FFFD)]
    [TestCase(0x40000, 0x4FFFD)]
    [TestCase(0x50000, 0x5FFFD)]
    [TestCase(0x60000, 0x6FFFD)]
    [TestCase(0x70000, 0x7FFFD)]
    [TestCase(0x80000, 0x8FFFD)]
    [TestCase(0x90000, 0x9FFFD)]
    [TestCase(0xA0000, 0xAFFFD)]
    [TestCase(0xB0000, 0xBFFFD)]
    [TestCase(0xC0000, 0xCFFFD)]
    [TestCase(0xD0000, 0xDFFFD)]
    [TestCase(0xE1000, 0xEFFFD)]
    public void EveryUcsCharacterAbnfRangeIncludesBothEndpoints(
        int first,
        int last)
    {
        Assert.Multiple(() =>
        {
            Assert.That(IriGrammar.IsUcsCharacter(first), Is.True);
            Assert.That(IriGrammar.IsUcsCharacter(last), Is.True);
        });
    }

    [TestCaseSource(nameof(UcsCharacterEndpoints))]
    public void EveryUcsCharacterBoundaryParsesInAllExtendedComponents(
        int scalarValue)
    {
        IIriParser parser = new IriParser();
        string value = char.ConvertFromUtf32(scalarValue);
        string text = "x://" + value + "@" + value + ".test/" + value +
            "?" + value + "#" + value;

        IriParseResult result = parser.TryParseIriReference(
            text,
            new IriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, "U+" + scalarValue.ToString("X"));
            Assert.That(result.Reference.Authority.UserInfo, Is.EqualTo(value));
            Assert.That(result.Reference.Authority.Host, Is.EqualTo(value + ".test"));
            Assert.That(result.Reference.Path, Is.EqualTo("/" + value));
            Assert.That(result.Reference.Query, Is.EqualTo(value));
            Assert.That(result.Reference.Fragment, Is.EqualTo(value));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [TestCase(0x0080, 0x009F)]
    [TestCase(0xFDD0, 0xFDEF)]
    [TestCase(0xFFF0, 0xFFFF)]
    [TestCase(0x1FFFE, 0x1FFFF)]
    [TestCase(0x2FFFE, 0x2FFFF)]
    [TestCase(0x3FFFE, 0x3FFFF)]
    [TestCase(0x4FFFE, 0x4FFFF)]
    [TestCase(0x5FFFE, 0x5FFFF)]
    [TestCase(0x6FFFE, 0x6FFFF)]
    [TestCase(0x7FFFE, 0x7FFFF)]
    [TestCase(0x8FFFE, 0x8FFFF)]
    [TestCase(0x9FFFE, 0x9FFFF)]
    [TestCase(0xAFFFE, 0xAFFFF)]
    [TestCase(0xBFFFE, 0xBFFFF)]
    [TestCase(0xCFFFE, 0xCFFFF)]
    [TestCase(0xDFFFE, 0xDFFFF)]
    [TestCase(0xE0000, 0xE0FFF)]
    [TestCase(0xEFFFE, 0xEFFFF)]
    [TestCase(0xFFFFE, 0xFFFFF)]
    [TestCase(0x10FFFE, 0x10FFFF)]
    public void EveryExcludedValidScalarRangeRejectsBothEndpoints(
        int first,
        int last)
    {
        Assert.Multiple(() =>
        {
            Assert.That(IriGrammar.IsUcsCharacter(first), Is.False);
            Assert.That(IriGrammar.IsPrivateCharacter(first), Is.False);
            Assert.That(IriGrammar.IsUcsCharacter(last), Is.False);
            Assert.That(IriGrammar.IsPrivateCharacter(last), Is.False);
        });
    }

    [TestCaseSource(nameof(ExcludedScalarEndpoints))]
    public void EveryExcludedScalarBoundaryIsRejectedFromEveryIriComponent(
        int scalarValue)
    {
        IIriParser parser = new IriParser();
        string value = char.ConvertFromUtf32(scalarValue);
        IriParseOptions options = new IriParseOptions();

        IriParseResult userInfo = parser.TryParseIriReference(
            "x://" + value + "@host/",
            options);
        IriParseResult host = parser.TryParseIriReference(
            "x://" + value + ".test/",
            options);
        IriParseResult path = parser.TryParseIriReference(
            "x:/" + value,
            options);
        IriParseResult query = parser.TryParseIriReference(
            "x:/?" + value,
            options);
        IriParseResult fragment = parser.TryParseIriReference(
            "x:/#" + value,
            options);

        Assert.Multiple(() =>
        {
            AssertFailure(userInfo, UriDiagnosticCode.InvalidUserInfo, UriComponent.UserInfo);
            AssertFailure(host, UriDiagnosticCode.InvalidHost, UriComponent.Host);
            AssertFailure(path, UriDiagnosticCode.InvalidPath, UriComponent.Path);
            AssertFailure(query, UriDiagnosticCode.InvalidQuery, UriComponent.Query);
            AssertFailure(fragment, UriDiagnosticCode.InvalidFragment, UriComponent.Fragment);
        });
    }

    [TestCase(0xE000, 0xF8FF)]
    [TestCase(0xF0000, 0xFFFFD)]
    [TestCase(0x100000, 0x10FFFD)]
    public void EveryPrivateCharacterAbnfRangeIncludesBothEndpoints(
        int first,
        int last)
    {
        Assert.Multiple(() =>
        {
            Assert.That(IriGrammar.IsPrivateCharacter(first), Is.True);
            Assert.That(IriGrammar.IsPrivateCharacter(last), Is.True);
            Assert.That(IriGrammar.IsUcsCharacter(first), Is.False);
            Assert.That(IriGrammar.IsUcsCharacter(last), Is.False);
        });
    }

    [TestCaseSource(nameof(PrivateCharacterEndpoints))]
    public void EveryPrivateCharacterBoundaryIsAcceptedOnlyInQuery(
        int scalarValue)
    {
        IIriParser parser = new IriParser();
        string value = char.ConvertFromUtf32(scalarValue);
        IriParseOptions options = new IriParseOptions();

        IriParseResult query = parser.TryParseIriReference(
            "x:/path?before" + value + "after#fragment",
            options);
        IriParseResult userInfo = parser.TryParseIriReference(
            "x://" + value + "@host/",
            options);
        IriParseResult host = parser.TryParseIriReference(
            "x://" + value + ".test/",
            options);
        IriParseResult path = parser.TryParseIriReference(
            "x:/" + value,
            options);
        IriParseResult fragment = parser.TryParseIriReference(
            "x:/#" + value,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(query.Success, Is.True, "U+" + scalarValue.ToString("X"));
            Assert.That(query.Reference.Query, Is.EqualTo("before" + value + "after"));
            AssertFailure(userInfo, UriDiagnosticCode.InvalidUserInfo, UriComponent.UserInfo);
            AssertFailure(host, UriDiagnosticCode.InvalidHost, UriComponent.Host);
            AssertFailure(path, UriDiagnosticCode.InvalidPath, UriComponent.Path);
            AssertFailure(fragment, UriDiagnosticCode.InvalidFragment, UriComponent.Fragment);
        });
    }

    [TestCase('\uD800')]
    [TestCase('\uDBFF')]
    [TestCase('\uDC00')]
    [TestCase('\uDFFF')]
    public void EverySurrogateSubrangeBoundaryIsInvalidUnicode(char value)
    {
        IIriParser parser = new IriParser();

        IriParseResult result = parser.TryParseIriReference(
            "x:/" + value,
            new IriParseOptions());

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidUnicode,
            UriComponent.Path);
    }

    private static void AssertFailure(
        IriParseResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
        });
    }
}
