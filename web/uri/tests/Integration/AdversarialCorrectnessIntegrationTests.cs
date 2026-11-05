namespace uri.Tests;

public class AdversarialCorrectnessIntegrationTests
{
    [Test]
    public void EncodedDelimitersNeverBecomeStructuralUriBoundaries()
    {
        const string text =
            "https://user%40label@example.test/a%2Fb" +
            "?next=%23admin#view%3Fmode";
        IUriParser parser = new UriParser();
        IUriPathUtility pathUtility = new UriPathUtility();
        IIriConverter iriConverter = new IriConverter();
        IUriComparer comparer = new UriComparer();

        UriReference reference = parser.ParseReference(
            text,
            new UriParseOptions());
        string[] segments = pathUtility.SplitPathSegments(
            reference.Path,
            new OperationLimits());
        string iriView = iriConverter.ConvertUriToIri(
            text,
            new IriConversionOptions());
        UriComparisonResult comparison = comparer.CompareSyntax(
            "x:/a%2Fb",
            "x:/a/b",
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reference.Authority.UserInfo, Is.EqualTo(
                "user%40label"));
            Assert.That(reference.Authority.Host, Is.EqualTo(
                "example.test"));
            Assert.That(reference.Path, Is.EqualTo("/a%2Fb"));
            Assert.That(reference.Query, Is.EqualTo("next=%23admin"));
            Assert.That(reference.Fragment, Is.EqualTo("view%3Fmode"));
            Assert.That(segments, Is.EqualTo(new string[]
            {
                "",
                "a%2Fb"
            }));
            Assert.That(iriView, Is.EqualTo(text));
            Assert.That(comparison.Success, Is.True);
            Assert.That(comparison.Equal, Is.False);
            Assert.That(comparison.DifferentComponent, Is.EqualTo(
                UriComponent.Path));
        });
    }

    [Test]
    public void LiteralAtSignAloneSelectsTheHostAfterUserInfo()
    {
        const string deceptive =
            "https://trusted.example%40security@attacker.example/login";
        const string extraDelimiter =
            "https://trusted.example@security@attacker.example/login";
        IUriParser parser = new UriParser();
        IIriConverter converter = new IriConverter();

        UriReference reference = parser.ParseReference(
            deceptive,
            new UriParseOptions());
        string display = converter.ConvertUriToIri(
            deceptive,
            new IriConversionOptions());
        UriParseResult rejected = parser.TryParseReference(
            extraDelimiter,
            new UriParseOptions());

        Assert.That(rejected.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(reference.Authority.HasUserInfo, Is.True);
            Assert.That(reference.Authority.UserInfo, Is.EqualTo(
                "trusted.example%40security"));
            Assert.That(reference.Authority.Host, Is.EqualTo(
                "attacker.example"));
            Assert.That(display, Is.EqualTo(deceptive));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Diagnostic!.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidUserInfo));
            Assert.That(rejected.Diagnostic.Component, Is.EqualTo(
                UriComponent.UserInfo));
        });
    }

    [Test]
    public void BidiFormattingControlsAreRejectedOrKeptPercentEncoded()
    {
        IIriConverter converter = new IriConverter();
        IriConversionOptions idnOptions = new IriConversionOptions();
        idnOptions.IdnMode = IriIdnMode.Platform;

        IriConversionException hostFailure = Assert.Throws<
            IriConversionException>(() => converter.ConvertHostToAscii(
                "safe\u202Eevil.test",
                idnOptions))!;
        string display = converter.ConvertUriToIri(
            "https://example.test/a%E2%80%AEb",
            new IriConversionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(hostFailure.Diagnostic.Domain, Is.EqualTo(
                DiagnosticDomain.Iri));
            Assert.That(hostFailure.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidBidiText));
            Assert.That(hostFailure.Diagnostic.Component, Is.EqualTo(
                UriComponent.Host));
            Assert.That(hostFailure.Diagnostic.Range.Offset, Is.EqualTo(4));
            Assert.That(display, Is.EqualTo(
                "https://example.test/a%E2%80%AEb"));
        });
    }

    [Test]
    public void MalformedUnicodeFailsAtTheSameSourceOffsetAcrossIriOperations()
    {
        const string prefix = "https://example.test/path";
        string malformed = prefix + new string('\ud800', 1) + "tail";
        IIriParser parser = new IriParser();
        IIriConverter converter = new IriConverter();

        IriParseResult parse = parser.TryParseIriReference(
            malformed,
            new IriParseOptions());
        UriTextResult conversion = converter.TryConvertIriToUri(
            malformed,
            new IriConversionOptions());

        Assert.That(parse.Diagnostic, Is.Not.Null);
        Assert.That(conversion.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(parse.Success, Is.False);
            Assert.That(parse.Diagnostic!.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidUnicode));
            Assert.That(parse.Diagnostic.Component, Is.EqualTo(
                UriComponent.Path));
            Assert.That(parse.Diagnostic.Range.Offset, Is.EqualTo(
                prefix.Length));
            Assert.That(conversion.Success, Is.False);
            Assert.That(conversion.Diagnostic!.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidUnicode));
            Assert.That(conversion.Diagnostic.Component, Is.EqualTo(
                UriComponent.Path));
            Assert.That(conversion.Diagnostic.Range.Offset, Is.EqualTo(
                prefix.Length));
        });
    }

    [Test]
    public void CompositeTemplateAmplificationStopsAtTheOutputLimit()
    {
        IUriTemplateParser parser = new UriTemplateParser();
        IUriTemplateExpander expander = new UriTemplateExpander();
        UriTemplate template = parser.ParseTemplate(
            "{?item*}",
            new UriTemplateParseOptions());
        string[] members = new string[128];
        int index = 0;
        while (index < members.Length)
        {
            members[index] = "abcdefgh";
            index = index + 1;
        }

        UriTemplateValue list = new UriTemplateValue();
        list.Kind = UriTemplateValueKind.List;
        list.List = members;
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            new UriTemplateBinding("item", list)
        };
        UriTemplateExpansionOptions roomy = new UriTemplateExpansionOptions();
        roomy.Limits.MaxTemplateCompositeMembers = members.Length;
        roomy.Limits.MaxOutputCharacters = 4096;
        UriTemplateExpansionOptions bounded =
            new UriTemplateExpansionOptions();
        bounded.Limits.MaxTemplateCompositeMembers = members.Length;
        bounded.Limits.MaxOutputCharacters = 64;

        string expanded = expander.ExpandTemplate(template, bindings, roomy);
        UriTemplateExpansionResult rejected = expander.TryExpandTemplate(
            template,
            bindings,
            bounded);

        Assert.That(rejected.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(expanded.Length, Is.GreaterThan(64));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Text, Is.Empty);
            Assert.That(rejected.Length, Is.Zero);
            Assert.That(rejected.Diagnostic!.Domain, Is.EqualTo(
                DiagnosticDomain.UriTemplate));
            Assert.That(rejected.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.LimitExceeded));
            Assert.That(rejected.Diagnostic.Component, Is.EqualTo(
                UriComponent.Output));
            Assert.That(rejected.Diagnostic.Range.Offset, Is.EqualTo(64));
        });
    }

    [Test]
    public void LinkQuotedPairsCannotInjectAdditionalLinksOrParameters()
    {
        const string escaped =
            "<https://safe.test>; " +
            "title=\"safe\\\", <https://evil.test>; rel=next\"";
        const string unescaped =
            "<https://safe.test>; " +
            "title=\"safe\", <https://evil.test>; rel=next\"";
        IWebLinkParser parser = new WebLinkParser();
        IWebLinkComposer composer = new WebLinkComposer();

        WebLinkSet safe = parser.ParseLinkFieldValues(
            new string[] { escaped },
            new WebLinkParseOptions());
        string serialized = composer.ComposeLinkFieldValue(
            safe,
            new OperationLimits());
        WebLinkSet roundTrip = parser.ParseLinkFieldValues(
            new string[] { serialized },
            new WebLinkParseOptions());
        WebLinkParseResult rejected = parser.TryParseLinkFieldValues(
            new string[] { unescaped },
            new WebLinkParseOptions());

        Assert.That(rejected.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(safe.Links, Has.Length.EqualTo(1));
            Assert.That(safe.Links[0].HasTitle, Is.True);
            Assert.That(safe.Links[0].Title, Is.EqualTo(
                "safe\", <https://evil.test>; rel=next"));
            Assert.That(safe.Links[0].Relations, Is.Empty);
            Assert.That(serialized, Is.EqualTo(escaped));
            Assert.That(roundTrip.Links, Has.Length.EqualTo(1));
            Assert.That(roundTrip.Links[0].Title, Is.EqualTo(
                safe.Links[0].Title));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.LinkSet.Links, Is.Empty);
            Assert.That(rejected.Diagnostic!.Domain, Is.EqualTo(
                DiagnosticDomain.WebLink));
            Assert.That(rejected.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidWebLink));
        });
    }
}
