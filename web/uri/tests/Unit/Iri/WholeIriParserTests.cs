namespace uri.Tests;

public class WholeIriParserTests
{
    [TestCase("a:道", UriReferenceKind.Uri)]
    [TestCase("?問\uE000", UriReferenceKind.RelativeReference)]
    [TestCase("//例え.テスト/道", UriReferenceKind.NetworkPathReference)]
    [TestCase("/道", UriReferenceKind.AbsolutePathReference)]
    [TestCase("道", UriReferenceKind.RelativePathReference)]
    [TestCase("", UriReferenceKind.EmptyReference)]
    [TestCase("#片", UriReferenceKind.SameDocumentReference)]
    public void IriReferenceUsesFirstMatchAndClassifiesEveryReferenceForm(
        string text,
        UriReferenceKind expectedKind)
    {
        IriParseResult result = IriParserCore.ParseReference(text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.Reference.Kind, Is.EqualTo(expectedKind), text);
            Assert.That(result.Reference.OriginalText, Is.EqualTo(text), text);
            Assert.That(result.Diagnostic, Is.Null, text);
        });
    }

    [Test]
    public void IriParsingPreservesEveryInternationalizedComponentLosslessly()
    {
        string text = "HTTP://利用者:密碼@例え.テスト:443/道/𐀀?問\uE000#片";

        IriParseResult result = IriParserCore.ParseIri(text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Reference.Kind, Is.EqualTo(UriReferenceKind.Uri));
            Assert.That(result.Reference.HasScheme, Is.True);
            Assert.That(result.Reference.Scheme, Is.EqualTo("HTTP"));
            Assert.That(result.Reference.HasAuthority, Is.True);
            Assert.That(result.Reference.Authority.HasUserInfo, Is.True);
            Assert.That(result.Reference.Authority.UserInfo, Is.EqualTo("利用者:密碼"));
            Assert.That(result.Reference.Authority.HostKind, Is.EqualTo(UriHostKind.RegisteredName));
            Assert.That(result.Reference.Authority.Host, Is.EqualTo("例え.テスト"));
            Assert.That(result.Reference.Authority.HasPort, Is.True);
            Assert.That(result.Reference.Authority.Port, Is.EqualTo("443"));
            Assert.That(result.Reference.Path, Is.EqualTo("/道/𐀀"));
            Assert.That(result.Reference.HasQuery, Is.True);
            Assert.That(result.Reference.Query, Is.EqualTo("問\uE000"));
            Assert.That(result.Reference.HasFragment, Is.True);
            Assert.That(result.Reference.Fragment, Is.EqualTo("片"));
            Assert.That(result.Reference.OriginalText, Is.EqualTo(text));
        });
    }

    [TestCase("i:", "", false, "", false, "")]
    [TestCase("i:道", "道", false, "", false, "")]
    [TestCase("i:/道", "/道", false, "", false, "")]
    [TestCase("i://例え/道", "/道", false, "", false, "")]
    [TestCase("i:道?", "道", true, "", false, "")]
    [TestCase("i:道#", "道", false, "", true, "")]
    [TestCase("i:道?問#片", "道", true, "問", true, "片")]
    public void IriProductionHandlesEveryHierarchicalAndOptionalComponentForm(
        string text,
        string path,
        bool hasQuery,
        string query,
        bool hasFragment,
        string fragment)
    {
        IriParseResult result = IriParserCore.ParseIri(text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.Reference.Path, Is.EqualTo(path), text);
            Assert.That(result.Reference.HasQuery, Is.EqualTo(hasQuery), text);
            Assert.That(result.Reference.Query, Is.EqualTo(query), text);
            Assert.That(result.Reference.HasFragment, Is.EqualTo(hasFragment), text);
            Assert.That(result.Reference.Fragment, Is.EqualTo(fragment), text);
        });
    }

    [TestCase("", UriReferenceKind.EmptyReference, "")]
    [TestCase("?問\uE000", UriReferenceKind.RelativeReference, "")]
    [TestCase("#片", UriReferenceKind.SameDocumentReference, "")]
    [TestCase("//例え/道", UriReferenceKind.NetworkPathReference, "/道")]
    [TestCase("/道", UriReferenceKind.AbsolutePathReference, "/道")]
    [TestCase("道/先:端", UriReferenceKind.RelativePathReference, "道/先:端")]
    [TestCase("道%3A先", UriReferenceKind.RelativePathReference, "道%3A先")]
    public void RelativeIriProductionHandlesAllRelativeForms(
        string text,
        UriReferenceKind kind,
        string path)
    {
        IriParseResult result = IriParserCore.ParseRelativeReference(text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.Reference.Kind, Is.EqualTo(kind), text);
            Assert.That(result.Reference.HasScheme, Is.False, text);
            Assert.That(result.Reference.Path, Is.EqualTo(path), text);
        });
    }

    [Test]
    public void AbsoluteIriRequiresSchemeAndForbidsFragment()
    {
        IriParseResult success = IriParserCore.ParseAbsoluteIri(
            "https://例え.テスト/道?問\uE000".AsSpan());
        IriParseResult missingScheme = IriParserCore.ParseAbsoluteIri(
            "/道".AsSpan());
        IriParseResult fragment = IriParserCore.ParseAbsoluteIri(
            "i:道#%".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(success.Success, Is.True);
            Assert.That(success.Reference.Kind, Is.EqualTo(UriReferenceKind.AbsoluteUri));
            Assert.That(success.Reference.HasFragment, Is.False);
            AssertFailure(missingScheme, UriDiagnosticCode.InvalidScheme, UriComponent.Scheme, 0);
            AssertFailure(fragment, UriDiagnosticCode.InvalidFragment, UriComponent.Fragment, 3);
        });
    }

    [Test]
    public void SpanEntryPointConsumesOnlyTheProvidedSlice()
    {
        string expected = "i://例え/道?問#片";
        string padded = "before" + expected + "after";

        IriParseResult result = IriParserCore.ParseReference(
            padded.AsSpan(6, expected.Length));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Reference.OriginalText, Is.EqualTo(expected));
            Assert.That(result.Reference.Authority.Host, Is.EqualTo("例え"));
            Assert.That(result.Reference.Path, Is.EqualTo("/道"));
            Assert.That(result.Reference.Query, Is.EqualTo("問"));
            Assert.That(result.Reference.Fragment, Is.EqualTo("片"));
        });
    }

    [TestCase("iri", "1abc:道", UriDiagnosticCode.InvalidScheme, UriComponent.Scheme, 0)]
    [TestCase("relative", "a:道", UriDiagnosticCode.InvalidPath, UriComponent.Path, 1)]
    [TestCase("reference", "x://a@b@例え", UriDiagnosticCode.InvalidUserInfo, UriComponent.UserInfo, 5)]
    [TestCase("reference", "x://例\uE000", UriDiagnosticCode.InvalidHost, UriComponent.Host, 5)]
    [TestCase("reference", "x://[例]", UriDiagnosticCode.InvalidIpv6Address, UriComponent.Host, 5)]
    [TestCase("reference", "x://例:x", UriDiagnosticCode.InvalidPort, UriComponent.Port, 6)]
    [TestCase("reference", "x:道\uE000", UriDiagnosticCode.InvalidPath, UriComponent.Path, 3)]
    [TestCase("reference", "x:道%GG", UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path, 4)]
    [TestCase("reference", "x:道#\uE000", UriDiagnosticCode.InvalidFragment, UriComponent.Fragment, 4)]
    [TestCase("reference", "x:道#片#先", UriDiagnosticCode.InvalidFragment, UriComponent.Fragment, 5)]
    public void ComponentFailuresPropagateWithWholeInputOffsetsAndIriDomain(
        string entryPoint,
        string text,
        UriDiagnosticCode code,
        UriComponent component,
        int offset)
    {
        IriParseResult result;
        if (entryPoint == "iri")
        {
            result = IriParserCore.ParseIri(text.AsSpan());
        }
        else if (entryPoint == "relative")
        {
            result = IriParserCore.ParseRelativeReference(text.AsSpan());
        }
        else
        {
            result = IriParserCore.ParseReference(text.AsSpan());
        }

        AssertFailure(result, code, component, offset);
    }

    [Test]
    public void WholeInputPreservesMalformedUtf16ForPreciseDiagnostics()
    {
        string text = "x:道?" + '\uD800';

        IriParseResult result = IriParserCore.ParseReference(text.AsSpan());

        AssertFailure(result, UriDiagnosticCode.InvalidUnicode, UriComponent.Query, 4);
    }

    [Test]
    public void IriAndRelativeProductionsRemainDistinct()
    {
        IriParseResult iriMissingScheme = IriParserCore.ParseIri("道".AsSpan());
        IriParseResult relativeWithScheme = IriParserCore.ParseRelativeReference("a:道".AsSpan());
        IriParseResult referenceWithScheme = IriParserCore.ParseReference("a:道".AsSpan());

        Assert.Multiple(() =>
        {
            AssertFailure(iriMissingScheme, UriDiagnosticCode.InvalidScheme, UriComponent.Scheme, 0);
            AssertFailure(relativeWithScheme, UriDiagnosticCode.InvalidPath, UriComponent.Path, 1);
            Assert.That(referenceWithScheme.Success, Is.True);
            Assert.That(referenceWithScheme.Reference.Kind, Is.EqualTo(UriReferenceKind.Uri));
        });
    }

    [Test]
    public void ThrowingEntryPointsReturnSuccessfulReferences()
    {
        IriReference reference = IriParserCore.ParseReferenceOrThrow("a:道#片".AsSpan());
        IriReference iri = IriParserCore.ParseIriOrThrow("a:道#片".AsSpan());
        IriReference absolute = IriParserCore.ParseAbsoluteIriOrThrow("a:道?問".AsSpan());
        IriReference relative = IriParserCore.ParseRelativeReferenceOrThrow("道#片".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(reference.OriginalText, Is.EqualTo("a:道#片"));
            Assert.That(iri.Kind, Is.EqualTo(UriReferenceKind.Uri));
            Assert.That(absolute.Kind, Is.EqualTo(UriReferenceKind.AbsoluteUri));
            Assert.That(relative.Kind, Is.EqualTo(UriReferenceKind.RelativePathReference));
        });
    }

    [Test]
    public void EveryThrowingEntryPointPreservesItsIriDiagnostic()
    {
        UriSyntaxException reference = Assert.Throws<UriSyntaxException>(ThrowInvalidReference)!;
        UriSyntaxException iri = Assert.Throws<UriSyntaxException>(ThrowInvalidIri)!;
        UriSyntaxException absolute = Assert.Throws<UriSyntaxException>(ThrowInvalidAbsoluteIri)!;
        UriSyntaxException relative = Assert.Throws<UriSyntaxException>(ThrowInvalidRelativeIri)!;

        Assert.Multiple(() =>
        {
            Assert.That(reference.Diagnostic.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(reference.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidPath));
            Assert.That(iri.Diagnostic.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(iri.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidScheme));
            Assert.That(absolute.Diagnostic.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(absolute.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidFragment));
            Assert.That(relative.Diagnostic.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(relative.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidPath));
        });
    }

    private static void ThrowInvalidReference()
    {
        IriParserCore.ParseReferenceOrThrow("道:先".AsSpan());
    }

    private static void ThrowInvalidIri()
    {
        IriParserCore.ParseIriOrThrow("道".AsSpan());
    }

    private static void ThrowInvalidAbsoluteIri()
    {
        IriParserCore.ParseAbsoluteIriOrThrow("a:道#片".AsSpan());
    }

    private static void ThrowInvalidRelativeIri()
    {
        IriParserCore.ParseRelativeReferenceOrThrow("a:道".AsSpan());
    }

    private static void AssertFailure(
        IriParseResult result,
        UriDiagnosticCode code,
        UriComponent component,
        int offset)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
            Assert.That(result.Reference.OriginalText, Is.Empty);
        });
    }
}
