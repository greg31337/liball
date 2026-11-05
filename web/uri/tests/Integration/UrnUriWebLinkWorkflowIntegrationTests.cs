namespace uri.Tests;

public class UrnUriWebLinkWorkflowIntegrationTests
{
    [Test]
    public void UrnParsesAsGenericUriAndRetainsUrnComponentBoundaries()
    {
        const string text =
            "URN:Example:catalog%2f/item?+resolver?=format=json#page-2";
        IUriParser uriParser = new UriParser();
        IUrnParser urnParser = new UrnParser();

        UriReference uri = uriParser.ParseReference(
            text,
            new UriParseOptions());
        Urn urn = urnParser.ParseUrn(text, new UrnParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(uri.Kind, Is.EqualTo(UriReferenceKind.Uri));
            Assert.That(uri.HasScheme, Is.True);
            Assert.That(uri.Scheme, Is.EqualTo("URN"));
            Assert.That(uri.HasAuthority, Is.False);
            Assert.That(uri.Path, Is.EqualTo("Example:catalog%2f/item"));
            Assert.That(uri.Query, Is.EqualTo(
                "+resolver?=format=json"));
            Assert.That(uri.Fragment, Is.EqualTo("page-2"));
            Assert.That(urn.Scheme, Is.EqualTo("URN"));
            Assert.That(urn.NamespaceIdentifier, Is.EqualTo("Example"));
            Assert.That(urn.NamespaceSpecificString, Is.EqualTo(
                "catalog%2f/item"));
            Assert.That(urn.AssignedName, Is.EqualTo(
                "URN:Example:catalog%2f/item"));
            Assert.That(urn.RComponent, Is.EqualTo("resolver"));
            Assert.That(urn.QComponent, Is.EqualTo("format=json"));
            Assert.That(urn.FComponent, Is.EqualTo("page-2"));
        });
    }

    [Test]
    public void AssignedNameEquivalenceIgnoresOptionalUrnComponents()
    {
        const string left =
            "URN:EXAMPLE:catalog%2f/item?+resolver?=format=json#page-2";
        const string right =
            "urn:example:catalog%2F/item?=different#section";
        IUrnComparer comparer = new UrnComparer();
        IUrnNormalizer normalizer = new UrnNormalizer();
        IUrnLocatorUtility locatorUtility = new UrnLocatorUtility();
        UrnParseOptions options = new UrnParseOptions();

        UrnEquivalenceResult exact = comparer.CompareUrnExact(
            left,
            right,
            options);
        UrnEquivalenceResult assigned = comparer.CompareUrnAssignedName(
            left,
            right,
            options);
        string leftAssigned = locatorUtility.SeparateAssignedName(
            left,
            options);
        string rightAssigned = locatorUtility.SeparateAssignedName(
            right,
            options);
        string normalizedLeft = normalizer.NormalizeAssignedName(
            leftAssigned,
            options);
        string normalizedRight = normalizer.NormalizeAssignedName(
            rightAssigned,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Equal, Is.False);
            Assert.That(exact.Level, Is.EqualTo(UrnEquivalenceLevel.Exact));
            Assert.That(assigned.Success, Is.True);
            Assert.That(assigned.Equal, Is.True);
            Assert.That(assigned.Level, Is.EqualTo(
                UrnEquivalenceLevel.AssignedName));
            Assert.That(assigned.DifferentComponent, Is.EqualTo(
                UriComponent.None));
            Assert.That(leftAssigned, Is.EqualTo(
                "URN:EXAMPLE:catalog%2f/item"));
            Assert.That(rightAssigned, Is.EqualTo(
                "urn:example:catalog%2F/item"));
            Assert.That(normalizedLeft, Is.EqualTo(
                "urn:example:catalog%2F/item"));
            Assert.That(normalizedRight, Is.EqualTo(normalizedLeft));
        });
    }

    [Test]
    public void LocatorTransferAppliesOnlyQAndFComponentsToTheResolvedUri()
    {
        const string urnText =
            "urn:example:weather?+resolver/service" +
            "?=op=map&lat=39.56#forecast";
        IUrnLocatorUtility locatorUtility = new UrnLocatorUtility();
        IUriParser uriParser = new UriParser();
        IUrnParser urnParser = new UrnParser();

        string locator = locatorUtility.ApplyUrnComponentsToLocator(
            urnText,
            "https://weather.example/report",
            new UrnLocatorMergeOptions());
        UriReference parsedLocator = uriParser.ParseReference(
            locator,
            new UriParseOptions());
        Urn source = urnParser.ParseUrn(urnText, new UrnParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(locator, Is.EqualTo(
                "https://weather.example/report" +
                "?op=map&lat=39.56#forecast"));
            Assert.That(locator, Does.Not.Contain("resolver/service"));
            Assert.That(parsedLocator.Query, Is.EqualTo(
                "op=map&lat=39.56"));
            Assert.That(parsedLocator.Fragment, Is.EqualTo("forecast"));
            Assert.That(source.RComponent, Is.EqualTo("resolver/service"));
            Assert.That(source.QComponent, Is.EqualTo(
                "op=map&lat=39.56"));
            Assert.That(source.FComponent, Is.EqualTo("forecast"));
        });
    }

    [Test]
    public void WebLinkResolutionPreservesUrnsAsTargetAndExplicitContext()
    {
        const string target =
            "urn:example:target%2Fitem?=view#part";
        const string context =
            "URN:Example:context%2Froot?=scope#section";
        IWebLinkParser linkParser = new WebLinkParser();
        IWebLinkResolver linkResolver = new WebLinkResolver();
        IUriParser uriParser = new UriParser();
        IUrnParser urnParser = new UrnParser();

        WebLinkSet links = linkParser.ParseLinkFieldValues(
            new string[]
            {
                "<" + target + ">; rel=item; anchor=\"" + context + "\""
            },
            new WebLinkParseOptions());
        WebLinkResolutionResult resolved = linkResolver.ResolveWebLink(
            links.Links[0],
            "https://representation.example/document",
            new OperationLimits());
        UriReference targetUri = uriParser.ParseReference(
            resolved.Target,
            new UriParseOptions());
        UriReference contextUri = uriParser.ParseReference(
            resolved.Context,
            new UriParseOptions());
        Urn targetUrn = urnParser.ParseUrn(
            resolved.Target,
            new UrnParseOptions());
        Urn contextUrn = urnParser.ParseUrn(
            resolved.Context,
            new UrnParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(links.Links, Has.Length.EqualTo(1));
            Assert.That(links.Links[0].Target, Is.EqualTo(target));
            Assert.That(links.Links[0].HasAnchor, Is.True);
            Assert.That(links.Links[0].Anchor, Is.EqualTo(context));
            Assert.That(resolved.Success, Is.True);
            Assert.That(resolved.Target, Is.EqualTo(target));
            Assert.That(resolved.HasContext, Is.True);
            Assert.That(resolved.Context, Is.EqualTo(context));
            Assert.That(resolved.AnchorHonored, Is.True);
            Assert.That(resolved.Diagnostic, Is.Null);
            Assert.That(targetUri.Scheme, Is.EqualTo("urn"));
            Assert.That(contextUri.Scheme, Is.EqualTo("URN"));
            Assert.That(targetUrn.NamespaceSpecificString, Is.EqualTo(
                "target%2Fitem"));
            Assert.That(targetUrn.QComponent, Is.EqualTo("view"));
            Assert.That(targetUrn.FComponent, Is.EqualTo("part"));
            Assert.That(contextUrn.NamespaceSpecificString, Is.EqualTo(
                "context%2Froot"));
            Assert.That(contextUrn.QComponent, Is.EqualTo("scope"));
            Assert.That(contextUrn.FComponent, Is.EqualTo("section"));
        });
    }
}
