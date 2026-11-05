namespace uri.Tests;

public class WebLinkDataTests
{
    [Test]
    public void LinkModelsProvideSafeIndependentDefaults()
    {
        WebLink firstLink = new WebLink();
        WebLink secondLink = new WebLink();
        WebLinkParameter firstParameter = new WebLinkParameter();
        WebLinkParameter secondParameter = new WebLinkParameter();
        WebLinkSet firstSet = new WebLinkSet();
        WebLinkSet secondSet = new WebLinkSet();

        Assert.Multiple(() =>
        {
            Assert.That(firstLink.Target, Is.Empty);
            Assert.That(firstLink.HasAnchor, Is.False);
            Assert.That(firstLink.Anchor, Is.Empty);
            Assert.That(firstLink.Relations, Is.Empty);
            Assert.That(firstLink.RelationKinds, Is.Empty);
            Assert.That(firstLink.ReverseRelations, Is.Empty);
            Assert.That(firstLink.ReverseRelationKinds, Is.Empty);
            Assert.That(firstLink.Parameters, Is.Empty);
            Assert.That(firstLink.HrefLanguages, Is.Empty);
            Assert.That(firstLink.HasMedia, Is.False);
            Assert.That(firstLink.Media, Is.Empty);
            Assert.That(firstLink.HasTitle, Is.False);
            Assert.That(firstLink.Title, Is.Empty);
            Assert.That(firstLink.HasTitleExtended, Is.False);
            Assert.That(firstLink.TitleExtended, Is.Empty);
            Assert.That(firstLink.TitleExtendedCharacterSet, Is.Empty);
            Assert.That(firstLink.TitleExtendedLanguage, Is.Empty);
            Assert.That(firstLink.TitleExtendedRawValue, Is.Empty);
            Assert.That(firstLink.HasType, Is.False);
            Assert.That(firstLink.Type, Is.Empty);
            Assert.That(firstLink.OriginalText, Is.Empty);
            Assert.That(firstLink.Range.Offset, Is.Zero);
            Assert.That(firstLink.Range.Length, Is.Zero);
            Assert.That(firstLink.Relations,
                Is.Not.SameAs(secondLink.Relations));
            Assert.That(firstLink.Parameters,
                Is.Not.SameAs(secondLink.Parameters));
            Assert.That(firstLink.Range, Is.Not.SameAs(secondLink.Range));

            Assert.That(firstParameter.Kind,
                Is.EqualTo(WebLinkParameterKind.Extension));
            Assert.That(firstParameter.Name, Is.Empty);
            Assert.That(firstParameter.HasValue, Is.False);
            Assert.That(firstParameter.Value, Is.Empty);
            Assert.That(firstParameter.RawValue, Is.Empty);
            Assert.That(firstParameter.WasQuoted, Is.False);
            Assert.That(firstParameter.Extended, Is.False);
            Assert.That(firstParameter.CharacterSet, Is.Empty);
            Assert.That(firstParameter.Language, Is.Empty);
            Assert.That(firstParameter.Ignored, Is.False);
            Assert.That(firstParameter.OriginalText, Is.Empty);
            Assert.That(firstParameter.Range.Offset, Is.Zero);
            Assert.That(firstParameter.Range.Length, Is.Zero);
            Assert.That(firstParameter.Range,
                Is.Not.SameAs(secondParameter.Range));

            Assert.That(firstSet.Links, Is.Empty);
            Assert.That(firstSet.OriginalFieldValues, Is.Empty);
            Assert.That(firstSet.Diagnostics, Is.Empty);
            Assert.That(firstSet.Links, Is.Not.SameAs(secondSet.Links));
            Assert.That(firstSet.OriginalFieldValues,
                Is.Not.SameAs(secondSet.OriginalFieldValues));
            Assert.That(firstSet.Diagnostics,
                Is.Not.SameAs(secondSet.Diagnostics));
        });
    }

    [Test]
    public void ParameterModelPreservesRawDecodedExtendedAndIgnoredState()
    {
        WebLinkParameter parameter = new WebLinkParameter();
        parameter.Kind = WebLinkParameterKind.TitleExtended;
        parameter.Name = "Title*";
        parameter.HasValue = true;
        parameter.Value = "letztes Kapitel";
        parameter.RawValue = "UTF-8'de'letztes%20Kapitel";
        parameter.WasQuoted = true;
        parameter.Extended = true;
        parameter.CharacterSet = "UTF-8";
        parameter.Language = "de";
        parameter.Ignored = true;
        parameter.OriginalText =
            "; Title*=\"UTF-8'de'letztes%20Kapitel\"";
        parameter.Range = new TextRange(24, 43);

        Assert.Multiple(() =>
        {
            Assert.That(parameter.Kind,
                Is.EqualTo(WebLinkParameterKind.TitleExtended));
            Assert.That(parameter.Name, Is.EqualTo("Title*"));
            Assert.That(parameter.HasValue, Is.True);
            Assert.That(parameter.Value, Is.EqualTo("letztes Kapitel"));
            Assert.That(parameter.RawValue,
                Is.EqualTo("UTF-8'de'letztes%20Kapitel"));
            Assert.That(parameter.WasQuoted, Is.True);
            Assert.That(parameter.Extended, Is.True);
            Assert.That(parameter.CharacterSet, Is.EqualTo("UTF-8"));
            Assert.That(parameter.Language, Is.EqualTo("de"));
            Assert.That(parameter.Ignored, Is.True);
            Assert.That(parameter.OriginalText, Does.StartWith("; Title*="));
            Assert.That(parameter.Range.Offset, Is.EqualTo(24));
            Assert.That(parameter.Range.Length, Is.EqualTo(43));
        });
    }

    [Test]
    public void LinkModelPreservesRelationsAttributesOrderAndSourceState()
    {
        WebLinkParameter relation = new WebLinkParameter();
        relation.Kind = WebLinkParameterKind.Relation;
        relation.Name = "rel";
        relation.HasValue = true;
        relation.Value = "next https://relations.example/custom";
        WebLinkParameter language = new WebLinkParameter();
        language.Kind = WebLinkParameterKind.HrefLang;
        language.Name = "hreflang";
        language.HasValue = true;
        language.Value = "de";
        WebLink link = new WebLink();
        link.Target = "../kapitel/2";
        link.HasAnchor = true;
        link.Anchor = "#contents";
        link.Relations = new string[]
        {
            "next",
            "https://relations.example/custom"
        };
        link.RelationKinds = new WebLinkRelationKind[]
        {
            WebLinkRelationKind.Registered,
            WebLinkRelationKind.Extension
        };
        link.ReverseRelations = new string[] { "previous" };
        link.ReverseRelationKinds = new WebLinkRelationKind[]
        {
            WebLinkRelationKind.Registered
        };
        link.Parameters = new WebLinkParameter[] { relation, language };
        link.HrefLanguages = new string[] { "de", "en-GB" };
        link.HasMedia = true;
        link.Media = "screen and (color)";
        link.HasTitle = true;
        link.Title = "Chapter 2";
        link.HasTitleExtended = true;
        link.TitleExtended = "Kapitel 2";
        link.TitleExtendedCharacterSet = "UTF-8";
        link.TitleExtendedLanguage = "de";
        link.TitleExtendedRawValue = "UTF-8'de'Kapitel%202";
        link.HasType = true;
        link.Type = "text/html";
        link.OriginalText =
            "<../kapitel/2>; rel=next; hreflang=de";
        link.Range = new TextRange(3, 42);

        Assert.Multiple(() =>
        {
            Assert.That(link.Target, Is.EqualTo("../kapitel/2"));
            Assert.That(link.HasAnchor, Is.True);
            Assert.That(link.Anchor, Is.EqualTo("#contents"));
            Assert.That(link.Relations, Is.EqualTo(new string[]
            {
                "next",
                "https://relations.example/custom"
            }));
            Assert.That(link.RelationKinds, Is.EqualTo(
                new WebLinkRelationKind[]
                {
                    WebLinkRelationKind.Registered,
                    WebLinkRelationKind.Extension
                }));
            Assert.That(link.ReverseRelations,
                Is.EqualTo(new string[] { "previous" }));
            Assert.That(link.ReverseRelationKinds,
                Is.EqualTo(new WebLinkRelationKind[]
                {
                    WebLinkRelationKind.Registered
                }));
            Assert.That(link.Parameters,
                Is.EqualTo(new WebLinkParameter[] { relation, language }));
            Assert.That(link.HrefLanguages,
                Is.EqualTo(new string[] { "de", "en-GB" }));
            Assert.That(link.HasMedia, Is.True);
            Assert.That(link.Media, Is.EqualTo("screen and (color)"));
            Assert.That(link.HasTitle, Is.True);
            Assert.That(link.Title, Is.EqualTo("Chapter 2"));
            Assert.That(link.HasTitleExtended, Is.True);
            Assert.That(link.TitleExtended, Is.EqualTo("Kapitel 2"));
            Assert.That(link.TitleExtendedCharacterSet,
                Is.EqualTo("UTF-8"));
            Assert.That(link.TitleExtendedLanguage, Is.EqualTo("de"));
            Assert.That(link.TitleExtendedRawValue,
                Is.EqualTo("UTF-8'de'Kapitel%202"));
            Assert.That(link.HasType, Is.True);
            Assert.That(link.Type, Is.EqualTo("text/html"));
            Assert.That(link.OriginalText, Does.StartWith("<../kapitel/2>"));
            Assert.That(link.Range.Offset, Is.EqualTo(3));
            Assert.That(link.Range.Length, Is.EqualTo(42));
        });
    }

    [Test]
    public void ParseOptionsAndFiltersHaveIndependentExplicitPolicies()
    {
        WebLinkParseOptions firstOptions = new WebLinkParseOptions();
        WebLinkParseOptions secondOptions = new WebLinkParseOptions();
        WebLinkFilter filter = new WebLinkFilter();

        Assert.Multiple(() =>
        {
            Assert.That(firstOptions.Mode,
                Is.EqualTo(WebLinkParseMode.Strict));
            Assert.That(firstOptions.Limits,
                Is.Not.SameAs(secondOptions.Limits));
            Assert.That(filter.HasRelation, Is.False);
            Assert.That(filter.Relation, Is.Empty);
            Assert.That(filter.HasTarget, Is.False);
            Assert.That(filter.Target, Is.Empty);
            Assert.That(filter.HasContext, Is.False);
            Assert.That(filter.Context, Is.Empty);
            Assert.That(filter.HasMediaType, Is.False);
            Assert.That(filter.MediaType, Is.Empty);
            Assert.That(filter.HasLanguage, Is.False);
            Assert.That(filter.Language, Is.Empty);
            Assert.That(filter.HasExtensionAttribute, Is.False);
            Assert.That(filter.ExtensionAttributeName, Is.Empty);
            Assert.That(filter.ExtensionAttributeHasValue, Is.False);
            Assert.That(filter.ExtensionAttributeValue, Is.Empty);
        });

        firstOptions.Mode = WebLinkParseMode.Recovery;
        firstOptions.Limits.MaxWebLinks = 3;
        filter.HasRelation = true;
        filter.Relation = "alternate";
        filter.HasTarget = true;
        filter.Target = "https://example.test/de";
        filter.HasContext = true;
        filter.Context = "https://example.test/";
        filter.HasMediaType = true;
        filter.MediaType = "text/html";
        filter.HasLanguage = true;
        filter.Language = "de";
        filter.HasExtensionAttribute = true;
        filter.ExtensionAttributeName = "profile";
        filter.ExtensionAttributeHasValue = true;
        filter.ExtensionAttributeValue = "compact";

        Assert.Multiple(() =>
        {
            Assert.That(firstOptions.Mode,
                Is.EqualTo(WebLinkParseMode.Recovery));
            Assert.That(firstOptions.Limits.MaxWebLinks, Is.EqualTo(3));
            Assert.That(secondOptions.Mode,
                Is.EqualTo(WebLinkParseMode.Strict));
            Assert.That(secondOptions.Limits.MaxWebLinks, Is.EqualTo(4096));
            Assert.That(filter.HasRelation, Is.True);
            Assert.That(filter.Relation, Is.EqualTo("alternate"));
            Assert.That(filter.HasTarget, Is.True);
            Assert.That(filter.Target,
                Is.EqualTo("https://example.test/de"));
            Assert.That(filter.HasContext, Is.True);
            Assert.That(filter.Context,
                Is.EqualTo("https://example.test/"));
            Assert.That(filter.HasMediaType, Is.True);
            Assert.That(filter.MediaType, Is.EqualTo("text/html"));
            Assert.That(filter.HasLanguage, Is.True);
            Assert.That(filter.Language, Is.EqualTo("de"));
            Assert.That(filter.HasExtensionAttribute, Is.True);
            Assert.That(filter.ExtensionAttributeName,
                Is.EqualTo("profile"));
            Assert.That(filter.ExtensionAttributeHasValue, Is.True);
            Assert.That(filter.ExtensionAttributeValue,
                Is.EqualTo("compact"));
        });
    }

    [Test]
    public void ParseAndResolutionResultsRetainEveryOutcomeField()
    {
        WebLink link = new WebLink();
        link.Target = "chapter2";
        WebLinkSet set = new WebLinkSet();
        set.Links = new WebLink[] { link };
        set.OriginalFieldValues = new string[]
        {
            "<chapter2>; rel=next"
        };
        UriDiagnostic recoveryDiagnostic = CreateDiagnostic(
            UriDiagnosticCode.DuplicateParameter,
            UriComponent.LinkParameter,
            21,
            "The duplicate parameter was ignored.");
        set.Diagnostics = new UriDiagnostic[] { recoveryDiagnostic };
        WebLinkParseResult parse = new WebLinkParseResult(
            true,
            set,
            true,
            null,
            new UriDiagnostic[] { recoveryDiagnostic });

        UriDiagnostic resolutionDiagnostic = CreateDiagnostic(
            UriDiagnosticCode.UnhonoredAnchor,
            UriComponent.LinkContext,
            0,
            "The anchor could not be honored.");
        WebLinkResolutionResult resolution =
            new WebLinkResolutionResult(
                false,
                link,
                "https://example.test/chapter2",
                false,
                "",
                false,
                resolutionDiagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(parse.Success, Is.True);
            Assert.That(parse.LinkSet, Is.SameAs(set));
            Assert.That(parse.LinkSet.Links,
                Is.EqualTo(new WebLink[] { link }));
            Assert.That(parse.LinkSet.OriginalFieldValues,
                Is.EqualTo(new string[] { "<chapter2>; rel=next" }));
            Assert.That(parse.LinkSet.Diagnostics,
                Is.EqualTo(new UriDiagnostic[] { recoveryDiagnostic }));
            Assert.That(parse.Recovered, Is.True);
            Assert.That(parse.Diagnostic, Is.Null);
            Assert.That(parse.Diagnostics,
                Is.EqualTo(new UriDiagnostic[] { recoveryDiagnostic }));

            Assert.That(resolution.Success, Is.False);
            Assert.That(resolution.Link, Is.SameAs(link));
            Assert.That(resolution.Target,
                Is.EqualTo("https://example.test/chapter2"));
            Assert.That(resolution.HasContext, Is.False);
            Assert.That(resolution.Context, Is.Empty);
            Assert.That(resolution.AnchorHonored, Is.False);
            Assert.That(resolution.Diagnostic,
                Is.SameAs(resolutionDiagnostic));
        });
    }

    [Test]
    public void FailedParseResultCanCarryAnEmptySetAndFatalDiagnostic()
    {
        UriDiagnostic diagnostic = CreateDiagnostic(
            UriDiagnosticCode.InvalidWebLink,
            UriComponent.WebLink,
            0,
            "The Link field value is invalid.");
        WebLinkSet empty = new WebLinkSet();
        WebLinkParseResult result = new WebLinkParseResult(
            false,
            empty,
            false,
            diagnostic,
            new UriDiagnostic[] { diagnostic });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.LinkSet, Is.SameAs(empty));
            Assert.That(result.LinkSet.Links, Is.Empty);
            Assert.That(result.Recovered, Is.False);
            Assert.That(result.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(result.Diagnostics,
                Is.EqualTo(new UriDiagnostic[] { diagnostic }));
        });
    }

    [Test]
    public void WebLinkEnumsExposeEveryStablePolicyAndModelChoice()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Enum.GetValues<WebLinkParseMode>(), Is.EqualTo(
                new WebLinkParseMode[]
                {
                    WebLinkParseMode.Strict,
                    WebLinkParseMode.Recovery
                }));
            Assert.That((int)WebLinkParseMode.Strict, Is.EqualTo(0));
            Assert.That((int)WebLinkParseMode.Recovery, Is.EqualTo(1));

            Assert.That(Enum.GetValues<WebLinkParameterKind>(), Is.EqualTo(
                new WebLinkParameterKind[]
                {
                    WebLinkParameterKind.Extension,
                    WebLinkParameterKind.Relation,
                    WebLinkParameterKind.ReverseRelation,
                    WebLinkParameterKind.Anchor,
                    WebLinkParameterKind.HrefLang,
                    WebLinkParameterKind.Media,
                    WebLinkParameterKind.Title,
                    WebLinkParameterKind.TitleExtended,
                    WebLinkParameterKind.Type
                }));
            Assert.That((int)WebLinkParameterKind.Extension, Is.EqualTo(0));
            Assert.That((int)WebLinkParameterKind.Relation, Is.EqualTo(1));
            Assert.That((int)WebLinkParameterKind.ReverseRelation,
                Is.EqualTo(2));
            Assert.That((int)WebLinkParameterKind.Anchor, Is.EqualTo(3));
            Assert.That((int)WebLinkParameterKind.HrefLang, Is.EqualTo(4));
            Assert.That((int)WebLinkParameterKind.Media, Is.EqualTo(5));
            Assert.That((int)WebLinkParameterKind.Title, Is.EqualTo(6));
            Assert.That((int)WebLinkParameterKind.TitleExtended,
                Is.EqualTo(7));
            Assert.That((int)WebLinkParameterKind.Type, Is.EqualTo(8));

            Assert.That(Enum.GetValues<WebLinkRelationKind>(), Is.EqualTo(
                new WebLinkRelationKind[]
                {
                    WebLinkRelationKind.Registered,
                    WebLinkRelationKind.Extension
                }));
            Assert.That((int)WebLinkRelationKind.Registered, Is.EqualTo(0));
            Assert.That((int)WebLinkRelationKind.Extension, Is.EqualTo(1));
        });
    }

    [Test]
    public void WebLinkExceptionsRetainDiagnosticContextAndMessage()
    {
        UriDiagnostic syntaxDiagnostic = CreateDiagnostic(
            UriDiagnosticCode.InvalidWebLink,
            UriComponent.WebLink,
            4,
            "The Link field value is invalid.");
        UriDiagnostic resolutionDiagnostic = CreateDiagnostic(
            UriDiagnosticCode.UnhonoredAnchor,
            UriComponent.LinkContext,
            9,
            "The anchor could not be honored.");

        WebLinkSyntaxException syntax =
            new WebLinkSyntaxException(syntaxDiagnostic);
        WebLinkResolutionException resolution =
            new WebLinkResolutionException(resolutionDiagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(syntax.Diagnostic, Is.SameAs(syntaxDiagnostic));
            Assert.That(syntax.Message,
                Is.EqualTo(syntaxDiagnostic.Message));
            Assert.That(resolution.Diagnostic,
                Is.SameAs(resolutionDiagnostic));
            Assert.That(resolution.Message,
                Is.EqualTo(resolutionDiagnostic.Message));
        });
    }

    private static UriDiagnostic CreateDiagnostic(
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        string message)
    {
        return new UriDiagnostic(
            DiagnosticDomain.WebLink,
            code,
            DiagnosticSeverity.Error,
            component,
            new TextRange(offset, 1),
            message);
    }
}
