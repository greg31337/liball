namespace uri.Tests;

public class WebLinkPublicApiCompleteTests
{
    [Test]
    public void ParserMethodsExposeStrictRecoveryAndBoundaryResults()
    {
        IWebLinkParser parser = new WebLinkParser();
        WebLinkParseResult empty = parser.TryParseLinkFieldValues(
            new string[0],
            new WebLinkParseOptions());

        WebLinkParseOptions recoveryOptions = new WebLinkParseOptions();
        recoveryOptions.Mode = WebLinkParseMode.Recovery;
        WebLinkParseResult recovery = parser.TryParseLinkFieldValues(
            new string[] { "<a><b>", "garbage" },
            recoveryOptions);
        WebLinkParseResult span = parser.TryParseLinkFieldValueSpan(
            "<span>; rel=next".AsSpan(),
            new WebLinkParseOptions());
        WebLinkParseResult nullFields = parser.TryParseLinkFieldValues(
            null!,
            new WebLinkParseOptions());
        WebLinkParseResult nullOptions = parser.TryParseLinkFieldValueSpan(
            "<a>".AsSpan(),
            null!);

        WebLinkSyntaxException exception =
            Assert.Throws<WebLinkSyntaxException>(() =>
                parser.ParseLinkFieldValues(
                    null!,
                    new WebLinkParseOptions()))!;

        Assert.That(nullFields.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.LinkSet.Links, Is.Empty);
            Assert.That(empty.LinkSet.OriginalFieldValues, Is.Empty);
            Assert.That(recovery.Success, Is.True);
            Assert.That(recovery.Recovered, Is.True);
            Assert.That(recovery.LinkSet.Links.Select(link => link.Target),
                Is.EqualTo(new string[] { "a", "b" }));
            Assert.That(recovery.Diagnostics, Has.Length.EqualTo(2));
            Assert.That(recovery.LinkSet.Diagnostics,
                Is.SameAs(recovery.Diagnostics));
            Assert.That(span.Success, Is.True);
            Assert.That(span.LinkSet.Links[0].Target,
                Is.EqualTo("span"));
            Assert.That(nullFields.Success, Is.False);
            Assert.That(nullFields.Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidInput));
            Assert.That(nullOptions.Success, Is.False);
            Assert.That(nullOptions.Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidInput));
            Assert.That(exception.Diagnostic.Code,
                Is.EqualTo(nullFields.Diagnostic.Code));
            Assert.That(exception.Message,
                Is.EqualTo(exception.Diagnostic.Message));
        });
    }

    [Test]
    public void PublicParserPopulatesEveryParameterKindAndModelField()
    {
        const string fieldValue =
            "  <relative/path>; " +
            "rel=\"NEXT https://relations.test/custom\"; rev=UP; " +
            "anchor=\"#context\"; hreflang=en; " +
            "media=\"screen, print\"; title=\"plain title\"; " +
            "title*=UTF-8'de'bevorzugt; " +
            "type=\"application/problem+json\"; " +
            "custom=\"raw,value\"  ";
        IWebLinkParser parser = new WebLinkParser();

        WebLinkParseResult result = parser.TryParseLinkFieldValues(
            new string[] { fieldValue },
            new WebLinkParseOptions());

        WebLink link = result.LinkSet.Links[0];
        WebLinkParameter extended = link.Parameters[6];
        WebLinkParameter custom = link.Parameters[8];
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovered, Is.False);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.LinkSet.Diagnostics,
                Is.SameAs(result.Diagnostics));
            Assert.That(result.LinkSet.OriginalFieldValues,
                Is.EqualTo(new string[] { fieldValue }));
            Assert.That(link.Target, Is.EqualTo("relative/path"));
            Assert.That(link.Relations,
                Is.EqualTo(new string[]
                {
                    "next",
                    "https://relations.test/custom"
                }));
            Assert.That(link.ReverseRelations,
                Is.EqualTo(new string[] { "up" }));
            Assert.That(link.HasAnchor, Is.True);
            Assert.That(link.Anchor, Is.EqualTo("#context"));
            Assert.That(link.HrefLanguages,
                Is.EqualTo(new string[] { "en" }));
            Assert.That(link.HasMedia, Is.True);
            Assert.That(link.Media, Is.EqualTo("screen, print"));
            Assert.That(link.HasTitle, Is.True);
            Assert.That(link.Title, Is.EqualTo("plain title"));
            Assert.That(link.HasTitleExtended, Is.True);
            Assert.That(link.TitleExtended, Is.EqualTo("bevorzugt"));
            Assert.That(link.TitleExtendedCharacterSet,
                Is.EqualTo("UTF-8"));
            Assert.That(link.TitleExtendedLanguage, Is.EqualTo("de"));
            Assert.That(link.TitleExtendedRawValue,
                Is.EqualTo("UTF-8'de'bevorzugt"));
            Assert.That(link.HasType, Is.True);
            Assert.That(link.Type,
                Is.EqualTo("application/problem+json"));
            Assert.That(link.Parameters.Select(parameter => parameter.Kind),
                Is.EqualTo(new WebLinkParameterKind[]
                {
                    WebLinkParameterKind.Relation,
                    WebLinkParameterKind.ReverseRelation,
                    WebLinkParameterKind.Anchor,
                    WebLinkParameterKind.HrefLang,
                    WebLinkParameterKind.Media,
                    WebLinkParameterKind.Title,
                    WebLinkParameterKind.TitleExtended,
                    WebLinkParameterKind.Type,
                    WebLinkParameterKind.Extension
                }));
            Assert.That(link.Range.Offset, Is.EqualTo(2));
            Assert.That(link.OriginalText,
                Is.EqualTo(fieldValue.Trim()));
            Assert.That(extended.Extended, Is.True);
            Assert.That(extended.Value, Is.EqualTo("bevorzugt"));
            Assert.That(extended.RawValue,
                Is.EqualTo("UTF-8'de'bevorzugt"));
            Assert.That(extended.CharacterSet, Is.EqualTo("UTF-8"));
            Assert.That(extended.Language, Is.EqualTo("de"));
            Assert.That(custom.HasValue, Is.True);
            Assert.That(custom.Value, Is.EqualTo("raw,value"));
            Assert.That(custom.RawValue, Is.EqualTo("raw,value"));
            Assert.That(custom.WasQuoted, Is.True);
            Assert.That(custom.Extended, Is.False);
            Assert.That(custom.Ignored, Is.False);
        });
    }

    [Test]
    public void ComposerMethodsShareFailureDiagnosticsAndNoPartialOutput()
    {
        IWebLinkComposer composer = new WebLinkComposer();
        WebLinkSet invalid = new WebLinkSet();
        WebLink link = new WebLink();
        link.Target = "bad target";
        invalid.Links = new WebLink[] { link };

        UriTextResult result = composer.TryComposeLinkFieldValue(
            invalid,
            new OperationLimits());
        UriTextResult nullResult = composer.TryComposeLinkFieldValue(
            null!,
            new OperationLimits());
        WebLinkSyntaxException composeException =
            Assert.Throws<WebLinkSyntaxException>(() =>
                composer.ComposeLinkFieldValue(
                    invalid,
                    new OperationLimits()))!;
        WebLinkSyntaxException measureException =
            Assert.Throws<WebLinkSyntaxException>(() =>
                composer.MeasureLinkFieldValue(
                    invalid,
                    new OperationLimits()))!;

        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(nullResult.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Length, Is.Zero);
            Assert.That(result.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(result.Diagnostic.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidLinkTarget));
            Assert.That(result.Diagnostic.Component,
                Is.EqualTo(UriComponent.LinkTarget));
            Assert.That(nullResult.Success, Is.False);
            Assert.That(nullResult.Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidInput));
            Assert.That(composeException.Diagnostic.Code,
                Is.EqualTo(result.Diagnostic.Code));
            Assert.That(measureException.Diagnostic.Code,
                Is.EqualTo(result.Diagnostic.Code));
        });
    }

    [Test]
    public void ResolverMethodsPopulateSuccessFailureAndBatchResultFields()
    {
        IWebLinkResolver resolver = new WebLinkResolver();
        WebLinkSet linkSet = Parse(
            "<relative>, <https://target.test/absolute?x#f>");

        WebLinkResolutionResult failure = resolver.ResolveWebLink(
            linkSet.Links[0],
            null,
            new OperationLimits());
        WebLinkResolutionResult success = resolver.ResolveWebLink(
            linkSet.Links[0],
            "https://context.test/base/index",
            new OperationLimits());
        WebLinkResolutionResult[] batch = resolver.ResolveWebLinks(
            linkSet,
            null,
            new OperationLimits());

        Assert.That(failure.Diagnostic, Is.Not.Null);
        WebLinkResolutionException exception =
            new WebLinkResolutionException(failure.Diagnostic!);
        Assert.Multiple(() =>
        {
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.Link, Is.SameAs(linkSet.Links[0]));
            Assert.That(failure.Target, Is.Empty);
            Assert.That(failure.HasContext, Is.False);
            Assert.That(failure.Context, Is.Empty);
            Assert.That(failure.AnchorHonored, Is.True);
            Assert.That(failure.Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.UnresolvableReference));
            Assert.That(success.Success, Is.True);
            Assert.That(success.Target,
                Is.EqualTo("https://context.test/base/relative"));
            Assert.That(success.HasContext, Is.True);
            Assert.That(success.Context,
                Is.EqualTo("https://context.test/base/index"));
            Assert.That(success.AnchorHonored, Is.True);
            Assert.That(success.Diagnostic, Is.Null);
            Assert.That(batch, Has.Length.EqualTo(2));
            Assert.That(batch[0].Success, Is.False);
            Assert.That(batch[1].Success, Is.True);
            Assert.That(batch[1].Target,
                Is.EqualTo("https://target.test/absolute?x#f"));
            Assert.That(exception.Diagnostic,
                Is.SameAs(failure.Diagnostic));
            Assert.That(exception.Message,
                Is.EqualTo(failure.Diagnostic.Message));
        });
    }

    [Test]
    public void QueryMethodsApplyEveryFilterFieldAndRejectInvalidInputs()
    {
        IWebLinkQuery query = new WebLinkQuery();
        WebLinkSet linkSet = Parse(
            "<HTTP://EXAMPLE.TEST/%7Eitem>; rel=NEXT; " +
            "anchor=\"HTTPS://CONTEXT.TEST/%7Ebase\"; " +
            "hreflang=EN-us; type=\"Application/JSON\"; " +
            "profile=Compact, " +
            "<https://example.test/other>; rel=next; " +
            "hreflang=de; type=\"text/plain\"; profile=Other");
        WebLinkFilter filter = new WebLinkFilter();
        filter.HasRelation = true;
        filter.Relation = "next";
        filter.HasTarget = true;
        filter.Target = "http://example.test/~item";
        filter.HasContext = true;
        filter.Context = "https://context.test/~base";
        filter.HasMediaType = true;
        filter.MediaType = "application/json";
        filter.HasLanguage = true;
        filter.Language = "en-US";
        filter.HasExtensionAttribute = true;
        filter.ExtensionAttributeName = "PROFILE";
        filter.ExtensionAttributeHasValue = true;
        filter.ExtensionAttributeValue = "Compact";

        WebLinkSet filtered = query.FilterWebLinks(linkSet, filter);
        WebLinkSet relations = query.FindLinksByRelation(linkSet, "NEXT");
        WebLinkSet targets = query.FindLinksByTarget(
            linkSet,
            "http://example.test/~item");
        WebLinkSet contexts = query.FindLinksByContext(
            linkSet,
            "https://context.test/~base");

        Assert.Multiple(() =>
        {
            Assert.That(filtered.Links,
                Is.EqualTo(new WebLink[] { linkSet.Links[0] }));
            Assert.That(relations.Links,
                Is.EqualTo(new WebLink[]
                {
                    linkSet.Links[0],
                    linkSet.Links[1]
                }));
            Assert.That(targets.Links,
                Is.EqualTo(new WebLink[] { linkSet.Links[0] }));
            Assert.That(contexts.Links,
                Is.EqualTo(new WebLink[] { linkSet.Links[0] }));
            Assert.That(filtered.Links, Is.Not.SameAs(linkSet.Links));
            Assert.That(filtered.OriginalFieldValues,
                Is.EqualTo(linkSet.OriginalFieldValues));
            Assert.Throws<ArgumentException>(() =>
                query.FilterWebLinks(null!, new WebLinkFilter()));
            Assert.Throws<ArgumentException>(() =>
                query.FindLinksByRelation(linkSet, null!));
            Assert.Throws<ArgumentException>(() =>
                query.FindLinksByTarget(linkSet, null!));
            Assert.Throws<ArgumentException>(() =>
                query.FindLinksByContext(linkSet, null!));
        });
    }

    [Test]
    public void PublicOperationsReportEveryRelevantProcessingLimit()
    {
        IWebLinkParser parser = new WebLinkParser();
        WebLinkParseOptions input = new WebLinkParseOptions();
        input.Limits.MaxInputCharacters = 2;
        WebLinkParseOptions component = new WebLinkParseOptions();
        component.Limits.MaxComponentCharacters = 0;
        WebLinkParseOptions links = new WebLinkParseOptions();
        links.Limits.MaxWebLinks = 0;
        WebLinkParseOptions parameters = new WebLinkParseOptions();
        parameters.Limits.MaxWebLinkParameters = 0;

        WebLinkParseResult inputFailure = parser.TryParseLinkFieldValueSpan(
            "<a>".AsSpan(), input);
        WebLinkParseResult componentFailure =
            parser.TryParseLinkFieldValueSpan("<a>".AsSpan(), component);
        WebLinkParseResult linkFailure = parser.TryParseLinkFieldValueSpan(
            "<a>".AsSpan(), links);
        WebLinkParseResult parameterFailure =
            parser.TryParseLinkFieldValueSpan(
                "<a>; rel=next".AsSpan(),
                parameters);

        WebLinkSet linkSet = Parse("<relative>");
        IWebLinkComposer composer = new WebLinkComposer();
        OperationLimits output = new OperationLimits();
        output.MaxOutputCharacters = "<relative>".Length - 1;
        UriTextResult outputFailure = composer.TryComposeLinkFieldValue(
            linkSet,
            output);

        IWebLinkResolver resolver = new WebLinkResolver();
        OperationLimits resolvedOutput = new OperationLimits();
        resolvedOutput.MaxOutputCharacters =
            "https://context.test/base/relative".Length - 1;
        WebLinkResolutionResult resolutionFailure =
            resolver.ResolveWebLink(
                linkSet.Links[0],
                "https://context.test/base/index",
                resolvedOutput);

        AssertLimit(inputFailure.Diagnostic, UriComponent.Input);
        AssertLimit(componentFailure.Diagnostic, UriComponent.LinkTarget);
        AssertLimit(linkFailure.Diagnostic, UriComponent.WebLink);
        AssertLimit(parameterFailure.Diagnostic, UriComponent.LinkParameter);
        AssertLimit(outputFailure.Diagnostic, UriComponent.Output);
        AssertLimit(resolutionFailure.Diagnostic, UriComponent.Output);
    }

    private static WebLinkSet Parse(string fieldValue)
    {
        IWebLinkParser parser = new WebLinkParser();
        return parser.ParseLinkFieldValues(
            new string[] { fieldValue },
            new WebLinkParseOptions());
    }

    private static void AssertLimit(
        UriDiagnostic? diagnostic,
        UriComponent component)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(diagnostic.Code,
                Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(diagnostic.Severity,
                Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.Component, Is.EqualTo(component));
        });
    }
}
