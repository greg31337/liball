namespace uri.Tests;

public class WebLinkRelationCoreTests
{
    [Test]
    public void StrictParserNormalizesRegisteredRelationsAndPreservesExtensionUris()
    {
        const string text =
            "<target>; REL=\"NEXT prev A1 A.B-C https://Example.test/Relation\"; ReV=\"UP urn:example:Reverse\"";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        Assert.That(result.Success, Is.True);
        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(link.Relations, Is.EqualTo(new string[]
            {
                "next",
                "prev",
                "a1",
                "a.b-c",
                "https://Example.test/Relation"
            }));
            Assert.That(link.RelationKinds,
                Is.EqualTo(new WebLinkRelationKind[]
                {
                    WebLinkRelationKind.Registered,
                    WebLinkRelationKind.Registered,
                    WebLinkRelationKind.Registered,
                    WebLinkRelationKind.Registered,
                    WebLinkRelationKind.Extension
                }));
            Assert.That(link.ReverseRelations, Is.EqualTo(new string[]
            {
                "up",
                "urn:example:Reverse"
            }));
            Assert.That(link.ReverseRelationKinds,
                Is.EqualTo(new WebLinkRelationKind[]
                {
                    WebLinkRelationKind.Registered,
                    WebLinkRelationKind.Extension
                }));
            Assert.That(link.Parameters[0].Kind,
                Is.EqualTo(WebLinkParameterKind.Relation));
            Assert.That(link.Parameters[1].Kind,
                Is.EqualTo(WebLinkParameterKind.ReverseRelation));
            Assert.That(link.Parameters[0].Name, Is.EqualTo("REL"));
            Assert.That(link.Parameters[0].Value,
                Is.EqualTo("NEXT prev A1 A.B-C https://Example.test/Relation"));
        });
    }

    [TestCase("<a>; rel", 8, 0)]
    [TestCase("<a>; rel=\"\"", 10, 0)]
    [TestCase("<a>; rel=\" next\"", 10, 1)]
    [TestCase("<a>; rel=\"next \"", 14, 1)]
    [TestCase("<a>; rel=\"next\tprev\"", 14, 1)]
    public void StrictParserRejectsEmptyOrImproperlySeparatedRelationLists(
        string text,
        int expectedOffset,
        int expectedLength)
    {
        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        AssertRelationFailure(result, expectedOffset, expectedLength);
        Assert.That(result.Diagnostic!.Message,
            Is.EqualTo(WebLinkRelationCore.InvalidRelationListMessage));
    }

    [TestCase("<a>; rel=\"1next\"", 10, 1)]
    [TestCase("<a>; rel=\"../next\"", 10, 1)]
    [TestCase("<a>; rel=\"next_name\"", 14, 1)]
    [TestCase("<a>; rel=\"https://example.test/#fragment\"", 31, 1)]
    public void StrictParserRejectsInvalidRegisteredAndExtensionRelations(
        string text,
        int expectedOffset,
        int expectedLength)
    {
        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        AssertRelationFailure(result, expectedOffset, expectedLength);
        Assert.That(result.Diagnostic!.Message,
            Is.EqualTo(WebLinkRelationCore.InvalidRelationTypeMessage));
    }

    [Test]
    public void StrictParserAcceptsOneOrMoreSpacesBetweenRelationTypes()
    {
        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "<a>; rel=\"next   previous\"".AsSpan(),
                new WebLinkParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.LinkSet.Links[0].Relations,
                Is.EqualTo(new string[] { "next", "previous" }));
        });
    }

    [Test]
    public void StrictFailureIsAtomicWhenALaterRelationTypeIsInvalid()
    {
        const string text =
            "<a>; rel=\"next https://example.test/relation\", <b>; rel=\"bad_name\"";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.LinkSet.Links, Is.Empty);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Range.Offset,
                Is.EqualTo(text.LastIndexOf('_')));
        });
    }

    [Test]
    public void RecoveryParserRepairsRelationWhitespaceAndRetainsMembers()
    {
        const string text =
            "<a>; REL=\" next\tPREVIOUS \"; rev=\" UP \"";

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        Assert.That(result.Success, Is.True);
        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(2));
            Assert.That(link.Relations,
                Is.EqualTo(new string[] { "next", "previous" }));
            Assert.That(link.RelationKinds,
                Is.EqualTo(new WebLinkRelationKind[]
                {
                    WebLinkRelationKind.Registered,
                    WebLinkRelationKind.Registered
                }));
            Assert.That(link.ReverseRelations,
                Is.EqualTo(new string[] { "up" }));
            Assert.That(link.Parameters[0].Kind,
                Is.EqualTo(WebLinkParameterKind.Relation));
            Assert.That(link.Parameters[1].Kind,
                Is.EqualTo(WebLinkParameterKind.ReverseRelation));
            Assert.That(result.Diagnostics[0].Range.Offset,
                Is.EqualTo(text.IndexOf(" next")));
            Assert.That(result.Diagnostics[1].Range.Offset,
                Is.EqualTo(text.LastIndexOf(" UP")));
            Assert.That(result.Diagnostics[0].Message,
                Is.EqualTo(WebLinkRelationCore.RecoveredRelationListMessage));
        });
    }

    [Test]
    public void RecoveryParserSkipsInvalidRelationsAndPreservesValidOrder()
    {
        const string text =
            "<a>; rel=\"NEXT 1bad https://example.test/relation ../bad LAST\"";

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovered, Is.True);
            Assert.That(link.Relations, Is.EqualTo(new string[]
            {
                "next",
                "https://example.test/relation",
                "last"
            }));
            Assert.That(link.RelationKinds,
                Is.EqualTo(new WebLinkRelationKind[]
                {
                    WebLinkRelationKind.Registered,
                    WebLinkRelationKind.Extension,
                    WebLinkRelationKind.Registered
                }));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(2));
            Assert.That(result.Diagnostics[0].Range.Offset,
                Is.EqualTo(text.IndexOf("1bad")));
            Assert.That(result.Diagnostics[1].Range.Offset,
                Is.EqualTo(text.IndexOf("../bad")));
            Assert.That(result.Diagnostics[0].Message,
                Is.EqualTo(WebLinkRelationCore.IgnoredRelationTypeMessage));
        });
    }

    [Test]
    public void RecoveryParserReportsMissingRelationValuesAndKeepsLinks()
    {
        const string text = "<a>; rel, <b>; rev=\"\"";

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.LinkSet.Links, Has.Length.EqualTo(2));
            Assert.That(result.LinkSet.Links[0].Relations, Is.Empty);
            Assert.That(result.LinkSet.Links[1].ReverseRelations, Is.Empty);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(2));
            Assert.That(result.Diagnostics[0].Range.Offset,
                Is.EqualTo(text.IndexOf(",")));
            Assert.That(result.Diagnostics[0].Range.Length, Is.Zero);
            Assert.That(result.Diagnostics[1].Range.Offset,
                Is.EqualTo(text.LastIndexOf('"')));
            Assert.That(result.Diagnostics[1].Range.Length, Is.Zero);
        });
    }

    [Test]
    public void RecoveryParserOrdersRelationWarningsWithParameterWarnings()
    {
        const string text = "<a>; rel=\"bad_name\"; na@me=x";

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(UriDiagnosticCode.InvalidRelationType));
            Assert.That(result.Diagnostics[0].Range.Offset,
                Is.EqualTo(text.IndexOf('_')));
            Assert.That(result.Diagnostics[1].Code,
                Is.EqualTo(UriDiagnosticCode.InvalidLinkParameter));
            Assert.That(result.Diagnostics[1].Range.Offset,
                Is.EqualTo(text.IndexOf('@')));
        });
    }

    private static WebLinkParseOptions CreateRecoveryOptions()
    {
        WebLinkParseOptions options = new WebLinkParseOptions();
        options.Mode = WebLinkParseMode.Recovery;
        return options;
    }

    private static void AssertRelationFailure(
        WebLinkParseResult result,
        int offset,
        int length)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.LinkSet.Links, Is.Empty);
            Assert.That(result.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(result.Diagnostic.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidRelationType));
            Assert.That(result.Diagnostic.Severity,
                Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component,
                Is.EqualTo(UriComponent.LinkRelation));
            Assert.That(result.Diagnostic.Range.Offset,
                Is.EqualTo(offset));
            Assert.That(result.Diagnostic.Range.Length,
                Is.EqualTo(length));
            Assert.That(result.Diagnostics,
                Is.EqualTo(new UriDiagnostic[] { result.Diagnostic }));
        });
    }
}
