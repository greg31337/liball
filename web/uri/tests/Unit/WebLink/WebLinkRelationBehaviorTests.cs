namespace uri.Tests;

public class WebLinkRelationBehaviorTests
{
    [Test]
    public void MultipleRelationsPreserveOrderKindAndCaseRules()
    {
        WebLink link = ParseStrict(
            "<target>; rel=\"NEXT Prev A1 A.B-C " +
            "HTTPS://Relations.Example/Custom?X=Y\"; " +
            "rev=\"UP urn:example:Reverse\"").Links[0];

        Assert.Multiple(() =>
        {
            Assert.That(link.Relations,
                Is.EqualTo(new string[]
                {
                    "next",
                    "prev",
                    "a1",
                    "a.b-c",
                    "HTTPS://Relations.Example/Custom?X=Y"
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
            Assert.That(link.ReverseRelations,
                Is.EqualTo(new string[]
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
        });
    }

    [TestCase("../relative")]
    [TestCase("/absolute-path")]
    [TestCase("//example.test/network-path")]
    [TestCase("?query")]
    [TestCase("#fragment")]
    public void StrictModeRejectsRelativeExtensionRelations(
        string relation)
    {
        IWebLinkParser parser = new WebLinkParser();
        WebLinkParseResult result = parser.TryParseLinkFieldValueSpan(
            ("<target>; rel=\"" + relation + "\"").AsSpan(),
            new WebLinkParseOptions());

        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidRelationType));
            Assert.That(result.Diagnostic.Component,
                Is.EqualTo(UriComponent.LinkRelation));
            Assert.That(result.Diagnostic.Severity,
                Is.EqualTo(DiagnosticSeverity.Error));
        });
    }

    [Test]
    public void RecoveryDropsInvalidMembersFromRelAndRevLists()
    {
        WebLinkParseOptions options = new WebLinkParseOptions();
        options.Mode = WebLinkParseMode.Recovery;
        IWebLinkParser parser = new WebLinkParser();
        WebLinkParseResult result = parser.TryParseLinkFieldValueSpan(
            ("<target>; rel=\"NEXT ../relative " +
             "https://relations.test/valid LAST\"; " +
             "rev=\"UP /invalid DOWN\"").AsSpan(),
            options);

        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovered, Is.True);
            Assert.That(link.Relations,
                Is.EqualTo(new string[]
                {
                    "next",
                    "https://relations.test/valid",
                    "last"
                }));
            Assert.That(link.ReverseRelations,
                Is.EqualTo(new string[] { "up", "down" }));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(2));
            Assert.That(result.Diagnostics.All(diagnostic =>
                diagnostic.Code == UriDiagnosticCode.InvalidRelationType),
                Is.True);
            Assert.That(result.Diagnostics.All(diagnostic =>
                diagnostic.Message ==
                    WebLinkRelationCore.IgnoredRelationTypeMessage),
                Is.True);
        });
    }

    [Test]
    public void FilteringUsesForwardRelationsAndRfcCaseComparison()
    {
        WebLinkSet linkSet = ParseStrict(
            "<one>; rel=\"NEXT HTTPS://REL.EXAMPLE/TYPE?X=Y\"; " +
            "rev=previous, " +
            "<two>; rel=next, " +
            "<three>; rev=next");
        IWebLinkQuery query = new WebLinkQuery();

        WebLinkSet registered = query.FindLinksByRelation(
            linkSet,
            "nExT");
        WebLinkSet extension = query.FindLinksByRelation(
            linkSet,
            "https://rel.example/type?x=y");
        WebLinkSet reverse = query.FindLinksByRelation(
            linkSet,
            "previous");
        WebLinkSet characterDifferent = query.FindLinksByRelation(
            linkSet,
            "https://rel.example/%54ype?x=y");

        Assert.Multiple(() =>
        {
            Assert.That(registered.Links,
                Is.EqualTo(new WebLink[]
                {
                    linkSet.Links[0],
                    linkSet.Links[1]
                }));
            Assert.That(extension.Links,
                Is.EqualTo(new WebLink[] { linkSet.Links[0] }));
            Assert.That(reverse.Links, Is.Empty);
            Assert.That(characterDifferent.Links, Is.Empty);
        });
    }

    [Test]
    public void DuplicateRelAndRevKeepOnlyTheirFirstSemanticValues()
    {
        const string fieldValue =
            "<one>; rel=next; REL=previous; " +
            "rev=up; REV=down, " +
            "<two>; rel=previous";
        IWebLinkParser parser = new WebLinkParser();
        WebLinkParseResult result = parser.TryParseLinkFieldValueSpan(
            fieldValue.AsSpan(),
            new WebLinkParseOptions());
        IWebLinkQuery query = new WebLinkQuery();

        WebLink first = result.LinkSet.Links[0];
        WebLinkSet next = query.FindLinksByRelation(result.LinkSet, "next");
        WebLinkSet previous = query.FindLinksByRelation(
            result.LinkSet,
            "previous");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(2));
            Assert.That(result.Diagnostics.All(diagnostic =>
                diagnostic.Code == UriDiagnosticCode.DuplicateParameter),
                Is.True);
            Assert.That(first.Relations,
                Is.EqualTo(new string[] { "next" }));
            Assert.That(first.ReverseRelations,
                Is.EqualTo(new string[] { "up" }));
            Assert.That(first.Parameters.Select(parameter =>
                parameter.Ignored),
                Is.EqualTo(new bool[] { false, true, false, true }));
            Assert.That(next.Links,
                Is.EqualTo(new WebLink[] { first }));
            Assert.That(previous.Links,
                Is.EqualTo(new WebLink[] { result.LinkSet.Links[1] }));
        });
    }

    private static WebLinkSet ParseStrict(string fieldValue)
    {
        IWebLinkParser parser = new WebLinkParser();
        return parser.ParseLinkFieldValues(
            new string[] { fieldValue },
            new WebLinkParseOptions());
    }
}
