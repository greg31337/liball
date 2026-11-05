
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlDiagnosticTests
{
    [Test]
    public void DiagnosticExposesStableCodeSeverityMessageAndSpan()
    {
        HtmlSourceSpan span = HtmlSourceSpan.Create(
            HtmlSourcePosition.Create(2, 1, 3),
            HtmlSourcePosition.Create(3, 1, 4));
        HtmlDiagnostic diagnostic = HtmlDiagnostic.Create(
            HtmlDiagnosticCode.TokenizerDuplicateAttribute,
            HtmlDiagnosticSeverity.Warning,
            "Duplicate attribute.",
            span);

        Assert.That((int)HtmlDiagnosticCode.TokenizerDuplicateAttribute, Is.EqualTo(3002));
        Assert.That((int)HtmlDiagnosticSeverity.Warning, Is.EqualTo(1));
        Assert.That(diagnostic.GetCode(), Is.EqualTo(HtmlDiagnosticCode.TokenizerDuplicateAttribute));
        Assert.That(diagnostic.GetIdentifier(), Is.EqualTo("UH03002"));
        Assert.That(diagnostic.GetSeverity(), Is.EqualTo(HtmlDiagnosticSeverity.Warning));
        Assert.That(diagnostic.GetMessage(), Is.EqualTo("Duplicate attribute."));
        Assert.That(diagnostic.GetSpan(), Is.SameAs(span));
    }

    [Test]
    public void DiagnosticFactoryValidatesRequiredValues()
    {
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();

        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlDiagnostic.Create(HtmlDiagnosticCode.None, HtmlDiagnosticSeverity.Error, "message", span));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlDiagnostic.Create(HtmlDiagnosticCode.InputNullCharacter, (HtmlDiagnosticSeverity)99, "message", span));
        Assert.Throws<ArgumentNullException>(() => HtmlDiagnostic.Create(HtmlDiagnosticCode.InputNullCharacter, HtmlDiagnosticSeverity.Error, null, span));
        Assert.Throws<ArgumentNullException>(() => HtmlDiagnostic.Create(HtmlDiagnosticCode.InputNullCharacter, HtmlDiagnosticSeverity.Error, "message", null));
    }

    [Test]
    public void CollectorBoundsStoredDiagnosticsAndReturnsSnapshots()
    {
        HtmlLimits limits = HtmlLimits.CreateUntrusted();
        limits.SetMaximumDiagnostics(2);
        HtmlBudget budget = new HtmlBudget(limits, CancellationToken.None);
        HtmlDiagnosticCollector collector = new HtmlDiagnosticCollector(budget);
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();

        Assert.That(collector.AddDetails(HtmlDiagnosticCode.InputNullCharacter, HtmlDiagnosticSeverity.Warning, "first", span), Is.True);
        Assert.That(collector.AddDetails(HtmlDiagnosticCode.TokenizerUnexpectedCharacter, HtmlDiagnosticSeverity.Error, "second", span), Is.True);
        Assert.That(collector.AddDetails(HtmlDiagnosticCode.TreeBuilderUnexpectedToken, HtmlDiagnosticSeverity.Error, "third", span), Is.False);

        Assert.That(collector.GetCount(), Is.EqualTo(2));
        Assert.That(collector.GetTotalCount(), Is.EqualTo(3));
        Assert.That(collector.GetDroppedCount(), Is.EqualTo(1));
        Assert.That(collector.WasTruncated(), Is.True);
        Assert.That(budget.GetFirstExceededLimit(), Is.EqualTo(HtmlLimitKind.Diagnostics));

        HtmlDiagnostic[] firstSnapshot = collector.GetDiagnostics();
        firstSnapshot[0] = firstSnapshot[1];
        HtmlDiagnostic[] secondSnapshot = collector.GetDiagnostics();
        Assert.That(secondSnapshot[0].GetMessage(), Is.EqualTo("first"));
    }

    [Test]
    public void CollectorsShareOneDiagnosticBudget()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumDiagnostics(2);
        HtmlBudget budget = new HtmlBudget(limits, CancellationToken.None);
        HtmlDiagnosticCollector tokenizer = new HtmlDiagnosticCollector(budget);
        HtmlDiagnosticCollector treeBuilder = new HtmlDiagnosticCollector(budget);
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();

        Assert.That(tokenizer.AddDetails(HtmlDiagnosticCode.TokenizerUnexpectedCharacter, HtmlDiagnosticSeverity.Error, "tokenizer", span), Is.True);
        Assert.That(treeBuilder.AddDetails(HtmlDiagnosticCode.TreeBuilderUnexpectedToken, HtmlDiagnosticSeverity.Error, "tree", span), Is.True);
        Assert.That(tokenizer.AddDetails(HtmlDiagnosticCode.TokenizerMalformedComment, HtmlDiagnosticSeverity.Warning, "extra", span), Is.False);
        Assert.That(tokenizer.GetCount(), Is.EqualTo(1));
        Assert.That(treeBuilder.GetCount(), Is.EqualTo(1));
    }

    [Test]
    public void CollectorChecksCancellationBeforeChangingCounts()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), source.Token);
        HtmlDiagnosticCollector collector = new HtmlDiagnosticCollector(budget);
        HtmlDiagnostic diagnostic = HtmlDiagnostic.Create(
            HtmlDiagnosticCode.OperationIncomplete,
            HtmlDiagnosticSeverity.Information,
            "Canceled.",
            HtmlSourceSpan.CreateUnknown());
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() => collector.Add(diagnostic));
        Assert.That(collector.GetCount(), Is.EqualTo(0));
        Assert.That(collector.GetTotalCount(), Is.EqualTo(0));
        Assert.That(collector.GetDroppedCount(), Is.EqualTo(0));
    }

    [Test]
    public void CollectorRejectsNullDiagnostics()
    {
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None);
        HtmlDiagnosticCollector collector = new HtmlDiagnosticCollector(budget);

        Assert.Throws<ArgumentNullException>(() => collector.Add((HtmlDiagnostic)null));
    }
}
