
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSanitizerReportTests
{
    [Test]
    public void TraversalReportsElementAttributeUrlCssAndNodeChanges()
    {
        HtmlNode fragment = ParseFragment(
            "<div onclick=secret id=location style='color:BLUE;position:fixed'>"
            + "<script>secret</script><x-box title=x>text</x-box>"
            + "<!--secret--><a target=_blank href='javascript:secret'>x</a>"
            + "</div>");
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("div");
        builder.AllowElement("a");
        builder.AllowAttribute("id");
        builder.AllowAttribute("style");
        builder.AllowAttributeOnElement("target", "a");
        builder.AllowAttributeOnElement("href", "a");
        builder.AllowCssProperty("color");

        HtmlSanitizerReport report =
            HtmlSanitizerNestedContent.ApplyWithReport(fragment,
                builder.Compile());
        HtmlSanitizerChange[] changes = report.GetChanges();

        Assert.Multiple(() =>
        {
            Assert.That(HtmlSerializer.SerializeChildren(fragment),
                Is.EqualTo("<div id=\"user-content-location\" style=\"color:blue\">"
                    + "text<a target=\"_blank\" rel=\"noopener noreferrer\">x</a>"
                    + "</div>"));
            Assert.That(report.GetTotalCount(), Is.EqualTo(9));
            Assert.That(report.GetRetainedCount(), Is.EqualTo(9));
            Assert.That(report.GetDroppedCount(), Is.Zero);
            Assert.That(report.WasTruncated(), Is.False);
            Assert.That(changes.Select(value => value.GetReason()),
                Is.EqualTo(new[]
                {
                    HtmlSanitizerChangeReason.CssDeclarationRewritten,
                    HtmlSanitizerChangeReason.CssPropertyRemoved,
                    HtmlSanitizerChangeReason.AttributeNotAllowed,
                    HtmlSanitizerChangeReason.DomClobbering,
                    HtmlSanitizerChangeReason.SensitiveElementDropped,
                    HtmlSanitizerChangeReason.ElementNotAllowed,
                    HtmlSanitizerChangeReason.CommentNotAllowed,
                    HtmlSanitizerChangeReason.UnsafeUrl,
                    HtmlSanitizerChangeReason.LinkHardened
                }));
            Assert.That(report.GetReasonCount(
                HtmlSanitizerChangeReason.CssPropertyRemoved), Is.EqualTo(1));
            Assert.That(changes.All(value => value.GetSpan().IsKnown()
                || value.GetReason()
                    == HtmlSanitizerChangeReason.LinkHardened), Is.True);
        });
    }

    [Test]
    public void AttackerValuesAreFixedRedactionsAndLongNamesAreOmitted()
    {
        string longName = "data-" + new string('x', 80);
        string secret = new string('s', 10000);
        HtmlNode element = ParseFragment("<p " + longName + "='" + secret
            + "' title=short></p>");
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("p");

        HtmlSanitizerReport report =
            HtmlSanitizerNestedContent.ApplyWithReport(element,
                builder.Compile());
        HtmlSanitizerChange[] changes = report.GetChanges();

        Assert.Multiple(() =>
        {
            Assert.That(changes, Has.Length.EqualTo(2));
            Assert.That(changes[0].GetOriginalName(), Is.Empty);
            Assert.That(changes[0].WasNameRedacted(), Is.True);
            Assert.That(changes[0].GetOriginalValue(),
                Is.EqualTo("[redacted]"));
            Assert.That(changes[0].WasValueRedacted(), Is.True);
            Assert.That(changes[1].GetOriginalName(), Is.EqualTo("title"));
            Assert.That(changes[1].WasNameRedacted(), Is.False);
            Assert.That(changes[1].GetOriginalValue(),
                Is.EqualTo("[redacted]"));
            Assert.That(changes.All(value =>
                !value.GetOriginalValue().Contains(secret)), Is.True);
        });
    }

    [Test]
    public void RetentionLimitDoesNotChangeSanitizationDecisions()
    {
        const string Input = "<p a=1 b=2 c=3 d=4>safe</p>";
        HtmlNode reported = ParseFragment(Input);
        HtmlNode unreported = ParseFragment(Input);
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("p");
        HtmlCompiledSanitizerPolicy policy = builder.Compile();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumDiagnostics(2);

        HtmlSanitizerReport report =
            HtmlSanitizerNestedContent.ApplyWithReport(reported, policy,
                limits);
        HtmlSanitizerNestedContent.Apply(unreported, policy);

        Assert.Multiple(() =>
        {
            Assert.That(HtmlSerializer.SerializeChildren(reported),
                Is.EqualTo(HtmlSerializer.SerializeChildren(unreported)));
            Assert.That(HtmlSerializer.SerializeChildren(reported),
                Is.EqualTo("<p>safe</p>"));
            Assert.That(report.GetRetainedCount(), Is.EqualTo(2));
            Assert.That(report.GetTotalCount(), Is.EqualTo(4));
            Assert.That(report.GetDroppedCount(), Is.EqualTo(2));
            Assert.That(report.WasTruncated(), Is.True);
            Assert.That(report.GetReasonCount(
                HtmlSanitizerChangeReason.AttributeNotAllowed),
                Is.EqualTo(4));
        });
    }

    [Test]
    public void DuplicateIdentifiersAreCountedWithoutRetainingTheirValue()
    {
        HtmlNode fragment = ParseFragment(
            "<div id=same></div><p id=same></p>");
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("div");
        builder.AllowElement("p");
        builder.AllowAttribute("id");

        HtmlSanitizerReport report =
            HtmlSanitizerNestedContent.ApplyWithReport(fragment,
                builder.Compile());
        HtmlSanitizerChange duplicate = report.GetChanges().Single(value =>
            value.GetReason()
                == HtmlSanitizerChangeReason.DuplicateIdentifier);

        Assert.Multiple(() =>
        {
            Assert.That(report.GetReasonCount(
                HtmlSanitizerChangeReason.DomClobbering), Is.EqualTo(2));
            Assert.That(report.GetReasonCount(
                HtmlSanitizerChangeReason.DuplicateIdentifier), Is.EqualTo(1));
            Assert.That(duplicate.GetOriginalName(), Is.EqualTo("id"));
            Assert.That(duplicate.GetOriginalValue(),
                Is.EqualTo("[redacted]"));
            Assert.That(duplicate.GetSpan().IsKnown(), Is.False);
        });
    }

    [Test]
    public void ReasonsHaveStableIdentifiersAndOccurrenceCounts()
    {
        HtmlSanitizerReportCollector collector =
            new HtmlSanitizerReportCollector(2);
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        collector.Add(HtmlSanitizerChangeKind.Removed,
            HtmlSanitizerChangeReason.UnsafeUrl, "href", "javascript:x",
            span);
        collector.Add(HtmlSanitizerChangeKind.Replaced,
            HtmlSanitizerChangeReason.UrlRewritten, "src", "relative.png",
            span);
        collector.Add(HtmlSanitizerChangeKind.Removed,
            HtmlSanitizerChangeReason.UnsafeUrl, "action", "secret", span);
        collector.MarkLimitExceeded();
        collector.MarkCancelled();

        HtmlSanitizerReport report = collector.CreateReport();
        HtmlSanitizerChange[] changes = report.GetChanges();
        changes[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That((int)HtmlSanitizerChangeReason.UnsafeUrl,
                Is.EqualTo(2101));
            Assert.That(report.GetChanges()[0].GetReasonIdentifier(),
                Is.EqualTo("UHS2101"));
            Assert.That(report.GetReasonCount(
                HtmlSanitizerChangeReason.UnsafeUrl), Is.EqualTo(2));
            Assert.That(report.GetReasonCount(
                HtmlSanitizerChangeReason.UrlRewritten), Is.EqualTo(1));
            Assert.That(report.GetTotalCount(), Is.EqualTo(3));
            Assert.That(report.GetDroppedCount(), Is.EqualTo(1));
            Assert.That(report.WasTruncated(), Is.True);
            Assert.That(report.WasLimitExceeded(), Is.True);
            Assert.That(report.WasCancelled(), Is.True);
            Assert.That(report.GetChanges()[0].GetOriginalName(),
                Is.EqualTo("href"));
        });
    }

    [Test]
    public void TemplateNeutralizationReportsTextAndAttributeRewrites()
    {
        HtmlNode fragment = ParseFragment(
            "<p title='{{secret}}'>${secret}</p>");
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("p");
        builder.AllowAttribute("title");
        builder.SetNeutralizeServerTemplateMarkers(true);

        HtmlSanitizerReport report =
            HtmlSanitizerNestedContent.ApplyWithReport(fragment,
                builder.Compile());

        Assert.Multiple(() =>
        {
            Assert.That(report.GetReasonCount(
                HtmlSanitizerChangeReason.TemplateSyntaxNeutralized),
                Is.EqualTo(2));
            Assert.That(report.GetChanges().All(value =>
                value.GetOriginalValue() == "[redacted]"), Is.True);
            Assert.That(HtmlSerializer.SerializeChildren(fragment),
                Does.Contain("\u200B"));
        });
    }

    [Test]
    public void PublicReportObjectsValidateInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => new HtmlSanitizerChange(
                (HtmlSanitizerChangeKind)99,
                HtmlSanitizerChangeReason.AttributeNotAllowed, "name", "",
                HtmlSourceSpan.CreateUnknown()),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new HtmlSanitizerChange(
                HtmlSanitizerChangeKind.Removed,
                HtmlSanitizerChangeReason.None, "name", "",
                HtmlSourceSpan.CreateUnknown()),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new HtmlSanitizerReportCollector(0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => HtmlSanitizerNestedContent.ApplyWithReport(null,
                new HtmlSanitizerPolicy().Compile()),
                Throws.ArgumentNullException);
            Assert.That(() => HtmlSanitizerNestedContent.ApplyWithReport(
                ParseFragment(""), null), Throws.ArgumentNullException);
            Assert.That(() => HtmlSanitizerNestedContent.ApplyWithReport(
                ParseFragment(""), new HtmlSanitizerPolicy().Compile(), null),
                Throws.ArgumentNullException);
        });
    }

    static HtmlNode ParseFragment(string html)
    {
        HtmlParseResult parsed = Html.ParseFragment(html, "body");
        Assert.That(parsed.IsSuccess(), Is.True);
        return parsed.GetFragment();
    }
}
