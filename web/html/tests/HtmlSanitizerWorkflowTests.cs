using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSanitizerWorkflowTests
{
    [Test]
    public void NamedCloneWorkflowsPreserveInputsAndExposeTypedOutputs()
    {
        HtmlSanitizerApi api = new HtmlSanitizerApi();
        HtmlCompiledSanitizerPolicy policy = CreatePolicy();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        HtmlDocument document = ParseDocument(
            "<p onclick=secret>safe<script>secret</script></p>");
        HtmlNode fragment = ParseFragment(
            "<p onclick=secret>safe<script>secret</script></p>");
        HtmlNode subtree = ParseFragment(
            "<p onclick=secret>safe<script>secret</script></p>")
            .GetFirstChild();

        HtmlSanitizeResult documentResult = api.SanitizeDocument(document,
            policy, limits, CancellationToken.None);
        HtmlSanitizeResult fragmentResult = api.SanitizeFragment(fragment,
            policy, limits, CancellationToken.None);
        HtmlSanitizeResult subtreeResult = api.SanitizeSubtree(subtree,
            policy, limits, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(documentResult.IsSuccess(), Is.True);
            Assert.That(documentResult.GetDocument(), Is.Not.Null);
            Assert.That(documentResult.GetDocument(), Is.Not.SameAs(document));
            Assert.That(documentResult.GetFragment(), Is.Null);
            Assert.That(documentResult.GetSubtree(), Is.Null);
            Assert.That(documentResult.GetHtml(), Is.EqualTo(
                "<html><head></head><body><p>safe</p></body></html>"));
            Assert.That(HtmlSerializer.Serialize(document.GetRoot()),
                Does.Contain("onclick=\"secret\""));
            Assert.That(HtmlSerializer.Serialize(document.GetRoot()),
                Does.Contain("<script>secret</script>"));

            Assert.That(fragmentResult.IsSuccess(), Is.True);
            Assert.That(fragmentResult.GetFragment(),
                Is.Not.SameAs(fragment));
            Assert.That(fragmentResult.GetDocument(), Is.Null);
            Assert.That(fragmentResult.GetSubtree(), Is.Null);
            Assert.That(fragmentResult.GetHtml(), Is.EqualTo("<p>safe</p>"));
            Assert.That(HtmlSerializer.SerializeChildren(fragment),
                Is.EqualTo("<p onclick=\"secret\">safe"
                    + "<script>secret</script></p>"));

            Assert.That(subtreeResult.IsSuccess(), Is.True);
            Assert.That(subtreeResult.GetSubtree(), Is.Not.SameAs(subtree));
            Assert.That(subtreeResult.GetDocument(), Is.Null);
            Assert.That(subtreeResult.GetFragment(), Is.Null);
            Assert.That(subtreeResult.GetHtml(), Is.EqualTo("<p>safe</p>"));
            Assert.That(HtmlSerializer.Serialize(subtree), Is.EqualTo(
                "<p onclick=\"secret\">safe<script>secret</script></p>"));
        });
    }

    [Test]
    public void ClearlyNamedInPlaceWorkflowsMutateOnlySuppliedScope()
    {
        HtmlSanitizerApi api = new HtmlSanitizerApi();
        HtmlCompiledSanitizerPolicy policy = CreatePolicy();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        HtmlDocument document = ParseDocument(
            "<p onclick=secret>safe<script>secret</script></p>");
        HtmlNode fragment = ParseFragment(
            "<p onclick=secret>safe<script>secret</script></p>");
        HtmlNode subtree = ParseFragment(
            "<p onclick=secret>safe<script>secret</script></p>")
            .GetFirstChild();

        HtmlSanitizeResult documentResult = api.SanitizeDocumentInPlace(
            document, policy, limits, CancellationToken.None);
        HtmlSanitizeResult fragmentResult = api.SanitizeFragmentInPlace(
            fragment, policy, limits, CancellationToken.None);
        HtmlSanitizeResult subtreeResult = api.SanitizeSubtreeInPlace(
            subtree, policy, limits, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(documentResult.GetDocument(), Is.SameAs(document));
            Assert.That(documentResult.GetHtml(), Is.EqualTo(
                "<html><head></head><body><p>safe</p></body></html>"));
            Assert.That(HtmlSerializer.Serialize(document.GetRoot()),
                Is.EqualTo(documentResult.GetHtml()));
            Assert.That(fragmentResult.GetFragment(), Is.SameAs(fragment));
            Assert.That(HtmlSerializer.SerializeChildren(fragment),
                Is.EqualTo("<p>safe</p>"));
            Assert.That(subtreeResult.GetSubtree(), Is.SameAs(subtree));
            Assert.That(HtmlSerializer.Serialize(subtree),
                Is.EqualTo("<p>safe</p>"));
        });
    }

    [Test]
    public void FailedOutputLimitPreservesCompleteRedactedChangeCounts()
    {
        HtmlSanitizerApi api = new HtmlSanitizerApi();
        HtmlNode fragment = ParseFragment(
            "<p onclick=very-secret-value>safe</p>");
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumOutputCharacters(10);

        HtmlSanitizeResult result = api.SanitizeFragment(fragment,
            CreatePolicy(), limits, CancellationToken.None);
        HtmlSanitizerReport report = result.GetReport();
        HtmlSanitizerChange change = report.GetChanges()[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.GetNode(), Is.Null);
            Assert.That(result.GetHtml(), Is.Empty);
            Assert.That(result.GetFailureCode(),
                Is.EqualTo(HtmlDiagnosticCode.SanitizerLimitExceeded));
            Assert.That(report.WasLimitExceeded(), Is.True);
            Assert.That(report.GetTotalCount(), Is.EqualTo(1));
            Assert.That(report.GetRetainedCount(), Is.EqualTo(1));
            Assert.That(report.GetDroppedCount(), Is.Zero);
            Assert.That(report.GetReasonCount(
                HtmlSanitizerChangeReason.AttributeNotAllowed),
                Is.EqualTo(1));
            Assert.That(change.GetOriginalName(), Is.EqualTo("onclick"));
            Assert.That(change.GetOriginalValue(), Is.EqualTo("[redacted]"));
            Assert.That(change.WasValueRedacted(), Is.True);
            Assert.That(HtmlSerializer.SerializeChildren(fragment),
                Is.EqualTo(
                    "<p onclick=\"very-secret-value\">safe</p>"));
        });
    }

    [Test]
    public void NamedWorkflowsValidateScopeAndObserveCancellation()
    {
        HtmlSanitizerApi api = new HtmlSanitizerApi();
        HtmlNode fragment = ParseFragment("<p onclick=x>safe</p>");
        HtmlNode subtree = fragment.GetFirstChild();
        HtmlCompiledSanitizerPolicy policy = CreatePolicy();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(() => api.SanitizeFragment(subtree, policy, limits,
                CancellationToken.None), Throws.ArgumentException);
            Assert.That(() => api.SanitizeSubtree(fragment, policy, limits,
                CancellationToken.None), Throws.ArgumentException);
            Assert.That(() => api.SanitizeFragmentInPlace(fragment, policy,
                limits, source.Token),
                Throws.InstanceOf<OperationCanceledException>());
            Assert.That(HtmlSerializer.SerializeChildren(fragment),
                Is.EqualTo("<p onclick=\"x\">safe</p>"));
        });
    }

    static HtmlCompiledSanitizerPolicy CreatePolicy()
    {
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("html");
        builder.AllowElement("head");
        builder.AllowElement("body");
        builder.AllowElement("p");
        return builder.Compile();
    }

    static HtmlDocument ParseDocument(string html)
    {
        HtmlParseResult parsed = Html.Parse(html,
            HtmlParseOptions.CreateDefault(), CancellationToken.None);
        Assert.That(parsed.IsSuccess(), Is.True);
        return parsed.GetDocument();
    }

    static HtmlNode ParseFragment(string html)
    {
        HtmlParseResult parsed = Html.ParseFragment(html, "body");
        Assert.That(parsed.IsSuccess(), Is.True);
        return parsed.GetFragment();
    }
}
