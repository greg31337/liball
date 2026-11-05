namespace uri.Tests;

public class WebLinkIriCoreTests
{
    [Test]
    public void HttpSerializationMapsIriTargetsAndAnchorsToUriReferences()
    {
        WebLinkParameter anchor = new WebLinkParameter();
        anchor.Name = "anchor";
        anchor.Kind = WebLinkParameterKind.Anchor;
        anchor.HasValue = true;
        anchor.Value = "https://例え.テスト/文書#節";

        WebLink link = new WebLink();
        link.Target = "https://例え.テスト/道?問#片";
        link.HasAnchor = true;
        link.Anchor = anchor.Value;
        link.Parameters = new WebLinkParameter[] { anchor };
        WebLinkSet linkSet = new WebLinkSet();
        linkSet.Links = new WebLink[] { link };

        UriTextResult result = WebLinkComposerCore.ComposeLinkFieldValue(
            linkSet,
            new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(
                "<https://%E4%BE%8B%E3%81%88.%E3%83%86%E3%82%B9%E3%83%88/" +
                "%E9%81%93?%E5%95%8F#%E7%89%87>; anchor=\"https://" +
                "%E4%BE%8B%E3%81%88.%E3%83%86%E3%82%B9%E3%83%88/" +
                "%E6%96%87%E6%9B%B8#%E7%AF%80\""));
            Assert.That(link.Target,
                Is.EqualTo("https://例え.テスト/道?問#片"));
            Assert.That(anchor.Value,
                Is.EqualTo("https://例え.テスト/文書#節"));
        });
    }

    [Test]
    public void HttpSerializationPreservesExistingPercentTriplets()
    {
        WebLink link = new WebLink();
        link.Target =
            "https://example.test/%E9%81%93?encoded=%2F#%E7%89%87";
        WebLinkSet linkSet = new WebLinkSet();
        linkSet.Links = new WebLink[] { link };

        UriTextResult result = WebLinkComposerCore.ComposeLinkFieldValue(
            linkSet,
            new OperationLimits());

        Assert.That(result.Text, Is.EqualTo(
            "<https://example.test/%E9%81%93?encoded=%2F#%E7%89%87>"));
    }

    [Test]
    public void ResolverAcceptsIriContextAnchorAndTargetButReturnsUris()
    {
        WebLink link = new WebLink();
        link.Target = "子?問#片";
        link.HasAnchor = true;
        link.Anchor = "../文脈/base#節";

        WebLinkResolutionResult result =
            WebLinkResolverCore.ResolveWebLink(
                link,
                "https://例え.テスト/基点/path/index",
                new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Target, Is.EqualTo(
                "https://%E4%BE%8B%E3%81%88.%E3%83%86%E3%82%B9%E3%83%88/" +
                "%E5%9F%BA%E7%82%B9/%E6%96%87%E8%84%88/" +
                "%E5%AD%90?%E5%95%8F#%E7%89%87"));
            Assert.That(result.Context, Is.EqualTo(
                "https://%E4%BE%8B%E3%81%88.%E3%83%86%E3%82%B9%E3%83%88/" +
                "%E5%9F%BA%E7%82%B9/%E6%96%87%E8%84%88/base#%E7%AF%80"));
            Assert.That(result.AnchorHonored, Is.True);
        });
    }

    [Test]
    public void ApplicationViewsDecodeSafeUtf8AndPreserveReservedDelimiters()
    {
        string targetUri =
            "https://%E4%BE%8B%E3%81%88.%E3%83%86%E3%82%B9%E3%83%88/" +
            "%E9%81%93%2Fpart?%E5%95%8F#%E7%89%87";
        string contextUri =
            "https://example.test/%E6%96%87%E6%9B%B8%3Fmode#%E7%AF%80";

        UriTextResult target =
            WebLinkIriCore.ConvertTargetUriToIriView(
                targetUri,
                new OperationLimits());
        UriTextResult context =
            WebLinkIriCore.ConvertContextUriToIriView(
                contextUri,
                new OperationLimits());

        Assert.Multiple(() =>
        {
            AssertSuccess(
                target,
                "https://例え.テスト/道%2Fpart?問#片");
            AssertSuccess(
                context,
                "https://example.test/文書%3Fmode#節");
        });
    }

    [Test]
    public void ConvertedLengthDrivesMeasurementAndOutputLimits()
    {
        WebLink link = new WebLink();
        link.Target = "x:é";
        WebLinkSet linkSet = new WebLinkSet();
        linkSet.Links = new WebLink[] { link };
        const string expected = "<x:%C3%A9>";

        OperationLimits exactLimits = new OperationLimits();
        exactLimits.MaxOutputCharacters = expected.Length;
        OperationLimits shortLimits = new OperationLimits();
        shortLimits.MaxOutputCharacters = expected.Length - 1;

        UriTextResult exact = WebLinkComposerCore.ComposeLinkFieldValue(
            linkSet,
            exactLimits);
        UriMeasureResult measured =
            WebLinkComposerCore.MeasureLinkFieldValue(
                linkSet,
                exactLimits);
        UriTextResult limited = WebLinkComposerCore.ComposeLinkFieldValue(
            linkSet,
            shortLimits);

        Assert.Multiple(() =>
        {
            AssertSuccess(exact, expected);
            Assert.That(measured.Success, Is.True);
            Assert.That(measured.Length, Is.EqualTo(expected.Length));
            AssertFailure(
                limited,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Output);
        });
    }

    [Test]
    public void InvalidUnicodeAndInvalidUriViewsHaveWebLinkDiagnostics()
    {
        UriTextResult invalidIri =
            WebLinkIriCore.ConvertTargetIriToHttpUri(
                "x:\ud800",
                new OperationLimits(),
                7);
        UriTextResult invalidUri =
            WebLinkIriCore.ConvertTargetUriToIriView(
                "x:bad space",
                new OperationLimits());

        Assert.Multiple(() =>
        {
            AssertFailure(
                invalidIri,
                UriDiagnosticCode.InvalidUnicode,
                UriComponent.LinkTarget);
            Assert.That(invalidIri.Diagnostic!.Range.Offset,
                Is.EqualTo(9));
            AssertFailure(
                invalidUri,
                UriDiagnosticCode.InvalidLinkTarget,
                UriComponent.LinkTarget);
        });
    }

    private static void AssertSuccess(UriTextResult result, string text)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(text));
            Assert.That(result.Length, Is.EqualTo(text.Length));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertFailure(
        UriTextResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Length, Is.Zero);
            Assert.That(result.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Component,
                Is.EqualTo(component));
        });
    }
}
