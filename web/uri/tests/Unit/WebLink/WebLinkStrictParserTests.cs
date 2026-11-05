namespace uri.Tests;

public class WebLinkStrictParserTests
{
    [Test]
    public void StrictParserPreservesTargetParameterOrderValuesAndRanges()
    {
        const string text =
            "  <https://example.test/a>; rel = \"next prev\"; flag; x=y; note=\"a,b;c\\\"d\"  ";
        int linkStart = text.IndexOf('<');
        int linkEnd = text.LastIndexOf('"') + 1;
        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.LinkSet.Links, Has.Length.EqualTo(1));
        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(link.Target,
                Is.EqualTo("https://example.test/a"));
            Assert.That(link.Parameters, Has.Length.EqualTo(4));
            Assert.That(link.OriginalText,
                Is.EqualTo(text.Substring(linkStart, linkEnd - linkStart)));
            Assert.That(link.Range.Offset, Is.EqualTo(linkStart));
            Assert.That(link.Range.Length, Is.EqualTo(linkEnd - linkStart));
            Assert.That(link.Relations,
                Is.EqualTo(new string[] { "next", "prev" }));
            Assert.That(link.RelationKinds,
                Is.EqualTo(new WebLinkRelationKind[]
                {
                    WebLinkRelationKind.Registered,
                    WebLinkRelationKind.Registered
                }));
            Assert.That(link.HrefLanguages, Is.Empty);
            Assert.That(result.Recovered, Is.False);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.LinkSet.OriginalFieldValues,
                Is.EqualTo(new string[] { text }));
        });

        AssertParameter(
            link.Parameters[0],
            "rel",
            true,
            "next prev",
            true,
            false,
            WebLinkParameterKind.Relation);
        AssertParameter(
            link.Parameters[1],
            "flag",
            false,
            "",
            false,
            false,
            WebLinkParameterKind.Extension);
        AssertParameter(
            link.Parameters[2],
            "x",
            true,
            "y",
            false,
            false,
            WebLinkParameterKind.Extension);
        AssertParameter(
            link.Parameters[3],
            "note",
            true,
            "a,b;c\"d",
            true,
            false,
            WebLinkParameterKind.Extension);
        Assert.That(link.Parameters[0].OriginalText,
            Is.EqualTo("; rel = \"next prev\""));
        Assert.That(link.Parameters[0].Range.Offset,
            Is.EqualTo(text.IndexOf("; rel")));
        Assert.That(link.Parameters[3].Range.Offset,
            Is.EqualTo(text.IndexOf("; note")));
    }

    [Test]
    public void StrictParserNeverSplitsCommasInsideTargetsOrQuotedValues()
    {
        const string text =
            "<https://example.test/a,b;c>; title=\"one,two;three\", <next>; x=\"a,b\"";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.LinkSet.Links, Has.Length.EqualTo(2));
            Assert.That(result.LinkSet.Links[0].Target,
                Is.EqualTo("https://example.test/a,b;c"));
            Assert.That(result.LinkSet.Links[0].Parameters[0].Value,
                Is.EqualTo("one,two;three"));
            Assert.That(result.LinkSet.Links[1].Target,
                Is.EqualTo("next"));
            Assert.That(result.LinkSet.Links[1].Parameters[0].Value,
                Is.EqualTo("a,b"));
            Assert.That(result.LinkSet.Links[0].Range.Offset, Is.Zero);
            Assert.That(result.LinkSet.Links[1].Range.Offset,
                Is.EqualTo(text.IndexOf("<next>")));
        });
    }

    [Test]
    public void StrictParserCombinesFieldsAndIgnoresHttpEmptyListMembers()
    {
        string[] fields = new string[]
        {
            ", , <a>,",
            "\t<b>;x=1 , , <c>\t"
        };

        WebLinkParseResult result = WebLinkParserCore.ParseStrictFieldValues(
            fields,
            new WebLinkParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.LinkSet.Links.Select(link => link.Target),
                Is.EqualTo(new string[] { "a", "b", "c" }));
            Assert.That(result.LinkSet.OriginalFieldValues,
                Is.EqualTo(fields));
            Assert.That(result.LinkSet.OriginalFieldValues,
                Is.Not.SameAs(fields));
            Assert.That(result.LinkSet.Links[0].Range.Offset,
                Is.EqualTo(fields[0].IndexOf("<a>")));
            Assert.That(result.LinkSet.Links[1].Range.Offset,
                Is.EqualTo(fields[1].IndexOf("<b>")));
            Assert.That(result.LinkSet.Links[2].Range.Offset,
                Is.EqualTo(fields[1].IndexOf("<c>")));
        });
    }

    [Test]
    public void StrictParserAcceptsEmptyListsAndEmptyFieldCollections()
    {
        WebLinkParseResult emptySpan =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                " \t , , ".AsSpan(),
                new WebLinkParseOptions());
        WebLinkParseResult emptyCollection =
            WebLinkParserCore.ParseStrictFieldValues(
                new string[0],
                new WebLinkParseOptions());
        WebLinkParseResult emptyFields =
            WebLinkParserCore.ParseStrictFieldValues(
                new string[] { "", " \t" },
                new WebLinkParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(emptySpan.Success, Is.True);
            Assert.That(emptySpan.LinkSet.Links, Is.Empty);
            Assert.That(emptyCollection.Success, Is.True);
            Assert.That(emptyCollection.LinkSet.Links, Is.Empty);
            Assert.That(emptyCollection.LinkSet.OriginalFieldValues,
                Is.Empty);
            Assert.That(emptyFields.Success, Is.True);
            Assert.That(emptyFields.LinkSet.Links, Is.Empty);
            Assert.That(emptyFields.LinkSet.OriginalFieldValues,
                Is.EqualTo(new string[] { "", " \t" }));
        });
    }

    [TestCase("<>", "")]
    [TestCase("<relative/path?x=1#f>", "relative/path?x=1#f")]
    [TestCase("<//example.test/path>", "//example.test/path")]
    [TestCase("<https://example.test/>", "https://example.test/")]
    [TestCase("<#fragment>", "#fragment")]
    public void StrictParserAcceptsEveryUriReferenceTargetForm(
        string text,
        string expectedTarget)
    {
        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.LinkSet.Links, Has.Length.EqualTo(1));
            Assert.That(result.LinkSet.Links[0].Target,
                Is.EqualTo(expectedTarget));
        });
    }

    [TestCase("<target", 7, 0)]
    [TestCase("<http://[bad]>", 12, 0)]
    [TestCase("<a b>", 2, 1)]
    [TestCase("<caf\u00e9>", 4, 1)]
    public void StrictParserReportsInvalidTargetSyntaxAtAbsoluteFieldOffsets(
        string text,
        int expectedOffset,
        int expectedLength)
    {
        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        AssertStrictFailure(
            result,
            UriDiagnosticCode.InvalidLinkTarget,
            UriComponent.LinkTarget,
            expectedOffset,
            expectedLength);
    }

    [TestCase("text", 0, 1)]
    [TestCase("<a> <b>", 4, 1)]
    [TestCase("<a>, text", 5, 1)]
    [TestCase("<a> @", 4, 1)]
    [TestCase("<a> =x", 4, 1)]
    public void StrictParserRejectsTextOutsideListAndLinkValueGrammar(
        string text,
        int expectedOffset,
        int expectedLength)
    {
        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        AssertStrictFailure(
            result,
            UriDiagnosticCode.InvalidWebLink,
            UriComponent.WebLink,
            expectedOffset,
            expectedLength);
    }

    [TestCase("<a>;", 4, 0)]
    [TestCase("<a>; =x", 5, 1)]
    [TestCase("<a>; x=", 7, 0)]
    [TestCase("<a>; x==y", 7, 1)]
    [TestCase("<a>; x=\"bad\rvalue\"", 11, 1)]
    [TestCase("<a>; x=\"unterminated", 20, 0)]
    public void StrictParserRejectsIncompleteOrInvalidParameters(
        string text,
        int expectedOffset,
        int expectedLength)
    {
        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        AssertStrictFailure(
            result,
            UriDiagnosticCode.InvalidLinkParameter,
            UriComponent.LinkParameter,
            expectedOffset,
            expectedLength);
    }

    [Test]
    public void StrictParserAcceptsHttpOwsAndBwsButRejectsOtherWhitespace()
    {
        WebLinkParseResult valid =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "\t<a>\t;\tname\t=\tvalue\t,\t<b>\t".AsSpan(),
                new WebLinkParseOptions());
        WebLinkParseResult carriageReturn =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "<a>\r,<b>".AsSpan(),
                new WebLinkParseOptions());
        WebLinkParseResult lineFeed =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "<a>\n;<b>".AsSpan(),
                new WebLinkParseOptions());

        Assert.That(valid.Success, Is.True);
        Assert.That(valid.LinkSet.Links, Has.Length.EqualTo(2));
        AssertStrictFailure(
            carriageReturn,
            UriDiagnosticCode.InvalidWebLink,
            UriComponent.WebLink,
            3,
            1);
        AssertStrictFailure(
            lineFeed,
            UriDiagnosticCode.InvalidWebLink,
            UriComponent.WebLink,
            3,
            1);
    }

    [Test]
    public void StrictParserMarksAsteriskParametersAndInterpretsRelations()
    {
        const string text =
            "<a>; Title*=UTF-8'en'hello%20world; rel=next";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        Assert.That(result.Success, Is.True);
        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(link.Parameters[0].Name, Is.EqualTo("Title*"));
            Assert.That(link.Parameters[0].Extended, Is.True);
            Assert.That(link.Parameters[0].RawValue,
                Is.EqualTo("UTF-8'en'hello%20world"));
            Assert.That(link.Parameters[0].Kind,
                Is.EqualTo(WebLinkParameterKind.TitleExtended));
            Assert.That(link.HasTitleExtended, Is.True);
            Assert.That(link.TitleExtended, Is.EqualTo("hello world"));
            Assert.That(link.TitleExtendedCharacterSet,
                Is.EqualTo("UTF-8"));
            Assert.That(link.TitleExtendedLanguage, Is.EqualTo("en"));
            Assert.That(link.TitleExtendedRawValue,
                Is.EqualTo("UTF-8'en'hello%20world"));
            Assert.That(link.Parameters[1].Name, Is.EqualTo("rel"));
            Assert.That(link.Parameters[1].Extended, Is.False);
            Assert.That(link.Parameters[1].Kind,
                Is.EqualTo(WebLinkParameterKind.Relation));
            Assert.That(link.Relations,
                Is.EqualTo(new string[] { "next" }));
            Assert.That(link.RelationKinds,
                Is.EqualTo(new WebLinkRelationKind[]
                {
                    WebLinkRelationKind.Registered
                }));
        });
    }

    [Test]
    public void StrictParserRejectsInvalidOptionsCollectionsAndMembers()
    {
        WebLinkParseOptions missingLimits = new WebLinkParseOptions();
        missingLimits.Limits = null!;
        WebLinkParseOptions recovery = new WebLinkParseOptions();
        recovery.Mode = WebLinkParseMode.Recovery;

        WebLinkParseResult nullOptions =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "<a>".AsSpan(),
                null);
        WebLinkParseResult nullLimits =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "<a>".AsSpan(),
                missingLimits);
        WebLinkParseResult wrongMode =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "<a>".AsSpan(),
                recovery);
        WebLinkParseResult nullCollection =
            WebLinkParserCore.ParseStrictFieldValues(
                null,
                new WebLinkParseOptions());
        WebLinkParseResult nullMember =
            WebLinkParserCore.ParseStrictFieldValues(
                new string[] { "<a>", null! },
                new WebLinkParseOptions());

        AssertInvalidInput(nullOptions);
        AssertInvalidInput(nullLimits);
        AssertInvalidInput(wrongMode);
        AssertInvalidInput(nullCollection);
        AssertInvalidInput(nullMember);
    }

    [Test]
    public void StrictParserAppliesExactAndExceededInputLimits()
    {
        const string text = "<a>,<b>";
        WebLinkParseOptions exactOptions = new WebLinkParseOptions();
        exactOptions.Limits.MaxInputCharacters = text.Length;
        WebLinkParseOptions shortOptions = new WebLinkParseOptions();
        shortOptions.Limits.MaxInputCharacters = text.Length - 1;
        WebLinkParseOptions combinedOptions = new WebLinkParseOptions();
        combinedOptions.Limits.MaxInputCharacters = 6;

        WebLinkParseResult exact =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                exactOptions);
        WebLinkParseResult shortInput =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                shortOptions);
        WebLinkParseResult combined =
            WebLinkParserCore.ParseStrictFieldValues(
                new string[] { "<a>", "<b>" },
                combinedOptions);
        WebLinkParseResult combinedExceeded =
            WebLinkParserCore.ParseStrictFieldValues(
                new string[] { "<a>", "<b>," },
                combinedOptions);

        Assert.That(exact.Success, Is.True);
        Assert.That(combined.Success, Is.True);
        AssertStrictFailure(
            shortInput,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Input,
            text.Length - 1,
            1);
        AssertStrictFailure(
            combinedExceeded,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Input,
            3,
            1);
    }

    [Test]
    public void StrictParserAppliesTargetParameterNameAndValueLimits()
    {
        WebLinkParseOptions targetOptions = new WebLinkParseOptions();
        targetOptions.Limits.MaxComponentCharacters = 2;
        WebLinkParseOptions nameOptions = new WebLinkParseOptions();
        nameOptions.Limits.MaxComponentCharacters = 2;
        WebLinkParseOptions valueOptions = new WebLinkParseOptions();
        valueOptions.Limits.MaxComponentCharacters = 2;

        WebLinkParseResult target =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "<abc>".AsSpan(),
                targetOptions);
        WebLinkParseResult name =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "<a>;abc=x".AsSpan(),
                nameOptions);
        WebLinkParseResult value =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "<a>;x=abc".AsSpan(),
                valueOptions);
        WebLinkParseResult boundary =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "<ab>;xy=zz".AsSpan(),
                targetOptions);

        AssertLimit(target, UriComponent.LinkTarget);
        AssertLimit(name, UriComponent.LinkParameter);
        AssertLimit(value, UriComponent.LinkParameter);
        Assert.That(boundary.Success, Is.True);
    }

    [Test]
    public void StrictParserAppliesTheLinkLimitAcrossCombinedFields()
    {
        WebLinkParseOptions options = new WebLinkParseOptions();
        options.Limits.MaxWebLinks = 1;

        WebLinkParseResult sameField =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "<a>,<b>".AsSpan(),
                options);
        WebLinkParseResult combined =
            WebLinkParserCore.ParseStrictFieldValues(
                new string[] { "<a>", "<b>" },
                options);
        WebLinkParseResult emptyMembers =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                ",,<a>,,".AsSpan(),
                options);

        AssertLimit(sameField, UriComponent.WebLink);
        Assert.That(sameField.Diagnostic!.Range.Offset, Is.EqualTo(4));
        AssertLimit(combined, UriComponent.WebLink);
        Assert.That(combined.Diagnostic!.Range.Offset, Is.Zero);
        Assert.That(emptyMembers.Success, Is.True);
        Assert.That(emptyMembers.LinkSet.Links, Has.Length.EqualTo(1));
    }

    [Test]
    public void StrictParserAppliesParameterLimitPerCombinedFieldValue()
    {
        WebLinkParseOptions options = new WebLinkParseOptions();
        options.Limits.MaxWebLinkParameters = 1;

        WebLinkParseResult sameLink =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "<a>;x=1;y=2".AsSpan(),
                options);
        WebLinkParseResult sameField =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "<a>;x=1,<b>;y=2".AsSpan(),
                options);
        WebLinkParseResult separateFields =
            WebLinkParserCore.ParseStrictFieldValues(
                new string[] { "<a>;x=1", "<b>;y=2" },
                options);

        AssertLimit(sameLink, UriComponent.LinkParameter);
        Assert.That(sameLink.Diagnostic!.Range.Offset,
            Is.EqualTo("<a>;x=1".Length));
        AssertLimit(sameField, UriComponent.LinkParameter);
        Assert.That(sameField.Diagnostic!.Range.Offset,
            Is.EqualTo("<a>;x=1,<b>".Length));
        Assert.That(separateFields.Success, Is.True);
        Assert.That(separateFields.LinkSet.Links, Has.Length.EqualTo(2));
    }

    [Test]
    public void StrictParserRejectsNegativeRelevantLimitsEvenForEmptyInput()
    {
        WebLinkParseOptions input = new WebLinkParseOptions();
        input.Limits.MaxInputCharacters = -1;
        WebLinkParseOptions component = new WebLinkParseOptions();
        component.Limits.MaxComponentCharacters = -1;
        WebLinkParseOptions links = new WebLinkParseOptions();
        links.Limits.MaxWebLinks = -1;
        WebLinkParseOptions parameters = new WebLinkParseOptions();
        parameters.Limits.MaxWebLinkParameters = -1;

        AssertLimit(
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "".AsSpan(),
                input),
            UriComponent.Input);
        AssertLimit(
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "".AsSpan(),
                component),
            UriComponent.LinkTarget);
        AssertLimit(
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "".AsSpan(),
                links),
            UriComponent.WebLink);
        AssertLimit(
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "".AsSpan(),
                parameters),
            UriComponent.LinkParameter);
    }

    [Test]
    public void StrictFailuresExposeTheSameFatalDiagnosticInBothFields()
    {
        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "<a>;x=".AsSpan(),
                new WebLinkParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.LinkSet.Links, Is.Empty);
            Assert.That(result.LinkSet.OriginalFieldValues, Is.Empty);
            Assert.That(result.Recovered, Is.False);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0],
                Is.SameAs(result.Diagnostic));
        });
    }

    private static void AssertParameter(
        WebLinkParameter parameter,
        string name,
        bool hasValue,
        string value,
        bool wasQuoted,
        bool extended,
        WebLinkParameterKind kind)
    {
        Assert.Multiple(() =>
        {
            Assert.That(parameter.Kind,
                Is.EqualTo(kind));
            Assert.That(parameter.Name, Is.EqualTo(name));
            Assert.That(parameter.HasValue, Is.EqualTo(hasValue));
            Assert.That(parameter.Value, Is.EqualTo(value));
            Assert.That(parameter.RawValue, Is.EqualTo(value));
            Assert.That(parameter.WasQuoted, Is.EqualTo(wasQuoted));
            Assert.That(parameter.Extended, Is.EqualTo(extended));
            Assert.That(parameter.CharacterSet, Is.Empty);
            Assert.That(parameter.Language, Is.Empty);
            Assert.That(parameter.Ignored, Is.False);
            Assert.That(parameter.Range.Length,
                Is.EqualTo(parameter.OriginalText.Length));
        });
    }

    private static void AssertStrictFailure(
        WebLinkParseResult result,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.LinkSet.Links, Is.Empty);
            Assert.That(result.Recovered, Is.False);
            Assert.That(result.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Severity,
                Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component,
                Is.EqualTo(component));
            Assert.That(result.Diagnostic.Range.Offset,
                Is.EqualTo(offset));
            Assert.That(result.Diagnostic.Range.Length,
                Is.EqualTo(length));
            Assert.That(result.Diagnostics,
                Is.EqualTo(new UriDiagnostic[] { result.Diagnostic }));
        });
    }

    private static void AssertInvalidInput(WebLinkParseResult result)
    {
        AssertStrictFailure(
            result,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input,
            0,
            0);
    }

    private static void AssertLimit(
        WebLinkParseResult result,
        UriComponent component)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(result.Diagnostic.Code,
                Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(result.Diagnostic.Component,
                Is.EqualTo(component));
        });
    }
}
