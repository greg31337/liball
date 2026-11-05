namespace uri.Tests;

public class PublicWebLinkApiTests
{
    [Test]
    public void ParserRoutesStrictRecoveryArrayAndSpanOperations()
    {
        IWebLinkParser parser = new WebLinkParser();
        WebLinkParseOptions strict = new WebLinkParseOptions();
        string[] fields = new string[]
        {
            "<first>; rel=next",
            "<second>; type=\"text/plain\""
        };

        WebLinkSet parsed = parser.ParseLinkFieldValues(fields, strict);
        WebLinkParseResult tried = parser.TryParseLinkFieldValues(
            fields,
            strict);
        WebLinkParseResult span = parser.TryParseLinkFieldValueSpan(
            "<span>".AsSpan(),
            strict);

        WebLinkParseOptions recovery = new WebLinkParseOptions();
        recovery.Mode = WebLinkParseMode.Recovery;
        WebLinkParseResult recovered = parser.TryParseLinkFieldValueSpan(
            "<one> <two>".AsSpan(),
            recovery);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Links.Select(link => link.Target),
                Is.EqualTo(new string[] { "first", "second" }));
            Assert.That(parsed.OriginalFieldValues, Is.EqualTo(fields));
            Assert.That(tried.Success, Is.True);
            Assert.That(tried.LinkSet.Links, Has.Length.EqualTo(2));
            Assert.That(span.Success, Is.True);
            Assert.That(span.LinkSet.Links[0].Target, Is.EqualTo("span"));
            Assert.That(recovered.Success, Is.True);
            Assert.That(recovered.Recovered, Is.True);
            Assert.That(recovered.LinkSet.Links.Select(link => link.Target),
                Is.EqualTo(new string[] { "one", "two" }));
        });
    }

    [Test]
    public void ParserReturnsOrThrowsTheSameBoundedFailure()
    {
        IWebLinkParser parser = new WebLinkParser();
        WebLinkParseOptions options = new WebLinkParseOptions();
        options.Limits.MaxWebLinks = 0;

        WebLinkParseResult result = parser.TryParseLinkFieldValues(
            new string[] { "<target>" },
            options);
        WebLinkSyntaxException exception = Assert.Throws<WebLinkSyntaxException>(
            () => parser.ParseLinkFieldValues(
                new string[] { "<target>" },
                options))!;

        WebLinkParseOptions parameterOptions = new WebLinkParseOptions();
        parameterOptions.Limits.MaxWebLinkParameters = 0;
        WebLinkParseResult parameterFailure =
            parser.TryParseLinkFieldValues(
                new string[] { "<target>; rel=next" },
                parameterOptions);

        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(parameterFailure.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(result.Diagnostic.Component,
                Is.EqualTo(UriComponent.WebLink));
            Assert.That(exception.Diagnostic.Code,
                Is.EqualTo(result.Diagnostic.Code));
            Assert.That(exception.Diagnostic.Component,
                Is.EqualTo(result.Diagnostic.Component));
            Assert.That(parameterFailure.Success, Is.False);
            Assert.That(parameterFailure.Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(parameterFailure.Diagnostic.Component,
                Is.EqualTo(UriComponent.LinkParameter));
        });
    }

    [Test]
    public void ComposerExposesThrowingResultAndMeasurementOperations()
    {
        IWebLinkComposer composer = new WebLinkComposer();
        WebLinkSet linkSet = Parse(
            "<relative>; rel=\"next prev\"; flag, <https://example.test/b>");
        const string expected =
            "<relative>; rel=\"next prev\"; flag, <https://example.test/b>";
        OperationLimits limits = new OperationLimits();

        string text = composer.ComposeLinkFieldValue(linkSet, limits);
        UriTextResult result = composer.TryComposeLinkFieldValue(
            linkSet,
            limits);
        int length = composer.MeasureLinkFieldValue(linkSet, limits);

        OperationLimits shortLimits = new OperationLimits();
        shortLimits.MaxOutputCharacters = expected.Length - 1;
        UriTextResult failure = composer.TryComposeLinkFieldValue(
            linkSet,
            shortLimits);
        WebLinkSyntaxException composeException =
            Assert.Throws<WebLinkSyntaxException>(() =>
                composer.ComposeLinkFieldValue(linkSet, shortLimits))!;
        WebLinkSyntaxException measureException =
            Assert.Throws<WebLinkSyntaxException>(() =>
                composer.MeasureLinkFieldValue(linkSet, shortLimits))!;

        Assert.That(failure.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo(expected));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Length, Is.EqualTo(expected.Length));
            Assert.That(length, Is.EqualTo(expected.Length));
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(failure.Diagnostic.Component,
                Is.EqualTo(UriComponent.Output));
            Assert.That(composeException.Diagnostic.Code,
                Is.EqualTo(failure.Diagnostic.Code));
            Assert.That(measureException.Diagnostic.Code,
                Is.EqualTo(failure.Diagnostic.Code));
        });
    }

    [Test]
    public void ResolverExposesSingleBatchAndBoundedOutcomes()
    {
        IWebLinkResolver resolver = new WebLinkResolver();
        WebLinkSet linkSet = Parse(
            "<child>, <https://other.test/resource>");
        OperationLimits limits = new OperationLimits();

        WebLinkResolutionResult single = resolver.ResolveWebLink(
            linkSet.Links[0],
            "https://example.test/base/index",
            limits);
        WebLinkResolutionResult[] batch = resolver.ResolveWebLinks(
            linkSet,
            "https://example.test/base/index",
            limits);

        OperationLimits bounded = new OperationLimits();
        bounded.MaxWebLinks = 1;
        WebLinkResolutionResult[] limited = resolver.ResolveWebLinks(
            linkSet,
            "https://example.test/base/index",
            bounded);

        Assert.That(limited[1].Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(single.Success, Is.True);
            Assert.That(single.Target,
                Is.EqualTo("https://example.test/base/child"));
            Assert.That(batch, Has.Length.EqualTo(2));
            Assert.That(batch.Select(result => result.Target),
                Is.EqualTo(new string[]
                {
                    "https://example.test/base/child",
                    "https://other.test/resource"
                }));
            Assert.That(limited[0].Success, Is.True);
            Assert.That(limited[1].Success, Is.False);
            Assert.That(limited[1].Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(limited[1].Diagnostic.Component,
                Is.EqualTo(UriComponent.WebLink));
        });
    }

    [Test]
    public void QueryExposesCombinedAndSpecializedSelectionOperations()
    {
        IWebLinkQuery query = new WebLinkQuery();
        WebLinkSet linkSet = Parse(
            "<HTTP://EXAMPLE.TEST/%7Eone>; rel=NEXT; " +
            "anchor=\"HTTPS://CONTEXT.TEST/%7Ebase\"; " +
            "type=\"Application/JSON\", " +
            "<https://example.test/two>; rel=previous; " +
            "type=\"text/plain\"");

        WebLinkFilter filter = new WebLinkFilter();
        filter.HasRelation = true;
        filter.Relation = "next";
        filter.HasMediaType = true;
        filter.MediaType = "application/json";

        WebLinkSet filtered = query.FilterWebLinks(linkSet, filter);
        WebLinkSet relations = query.FindLinksByRelation(linkSet, "NEXT");
        WebLinkSet targets = query.FindLinksByTarget(
            linkSet,
            "http://example.test/~one");
        WebLinkSet contexts = query.FindLinksByContext(
            linkSet,
            "https://context.test/~base");

        Assert.Multiple(() =>
        {
            Assert.That(filtered.Links,
                Is.EqualTo(new WebLink[] { linkSet.Links[0] }));
            Assert.That(relations.Links,
                Is.EqualTo(new WebLink[] { linkSet.Links[0] }));
            Assert.That(targets.Links,
                Is.EqualTo(new WebLink[] { linkSet.Links[0] }));
            Assert.That(contexts.Links,
                Is.EqualTo(new WebLink[] { linkSet.Links[0] }));
            Assert.That(filtered.OriginalFieldValues,
                Is.EqualTo(linkSet.OriginalFieldValues));
        });
    }

    private static WebLinkSet Parse(string text)
    {
        IWebLinkParser parser = new WebLinkParser();
        return parser.ParseLinkFieldValues(
            new string[] { text },
            new WebLinkParseOptions());
    }
}
