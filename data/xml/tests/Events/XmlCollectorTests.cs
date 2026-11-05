using System;
using xml;

namespace xml.Tests;

[TestFixture]
public class XmlCollectorTests
{
    [Test]
    public void EventCollectorRetainsOrderedSnapshotsAndClearsWithoutChangingCapacity()
    {
        XmlEventCollector collector = new XmlEventCollector();
        collector.ConfigureXmlEventCollection(2);
        XmlEvent first = Event(XmlEventKind.StartDocument);
        XmlEvent second = Event(XmlEventKind.EndDocument);

        Assert.That(collector.HandleXmlEvent(first), Is.EqualTo(XmlHandlerAction.Continue));
        Assert.That(collector.HandleXmlEvent(second), Is.EqualTo(XmlHandlerAction.Pause));
        Assert.That(collector.CountCollectedXmlEvents(), Is.EqualTo(2));
        XmlEvent[] snapshot = collector.SnapshotCollectedXmlEvents();
        Assert.That(snapshot, Is.EqualTo(new[] { first, second }));

        snapshot[0] = new XmlEvent();
        Assert.That(collector.SnapshotCollectedXmlEvents()[0], Is.SameAs(first));
        Assert.Throws<InvalidOperationException>(() => collector.HandleXmlEvent(new XmlEvent()));

        collector.ClearCollectedXmlEvents();
        Assert.That(collector.CountCollectedXmlEvents(), Is.Zero);
        Assert.That(collector.HandleXmlEvent(first), Is.EqualTo(XmlHandlerAction.Continue));
        Assert.That(collector.HandleXmlEvent(second), Is.EqualTo(XmlHandlerAction.Pause));

        collector.ResetXmlEventCollection();
        Assert.That(collector.CountCollectedXmlEvents(), Is.Zero);
        Assert.That(collector.HandleXmlEvent(first), Is.EqualTo(XmlHandlerAction.Continue));
        Assert.That(collector.HandleXmlEvent(second), Is.EqualTo(XmlHandlerAction.Continue));
        Assert.Throws<ArgumentOutOfRangeException>(() => collector.ConfigureXmlEventCollection(0));
        Assert.Throws<ArgumentNullException>(() => collector.HandleXmlEvent(null!));
    }

    [Test]
    public void EventCollectorBackpressureCanDrainACompletedPushStreamInBatches()
    {
        XmlEventCollector collector = new XmlEventCollector();
        collector.ConfigureXmlEventCollection(3);
        XmlPushParser parser = new XmlPushParser();
        XmlInput input = new XmlInput();
        input.Text = "<root/>";
        parser.BeginXmlPush(input, Options(), collector, null, null, null);

        XmlEventBatch paused = parser.FinishXmlPush();

        Assert.That(paused.Status, Is.EqualTo(XmlParserStatus.Paused));
        Assert.That(paused.Count, Is.EqualTo(3));
        Assert.That(collector.SnapshotCollectedXmlEvents(), Is.EqualTo(paused.Events));
        Assert.That(collector.SnapshotCollectedXmlEvents()[2].Kind, Is.EqualTo(XmlEventKind.EndElement));

        collector.ClearCollectedXmlEvents();
        Assert.That(parser.ResumeXmlPush(), Is.EqualTo(XmlParserStatus.Running));
        XmlEventBatch completed = parser.FinishXmlPush();

        Assert.That(completed.Status, Is.EqualTo(XmlParserStatus.Completed));
        Assert.That(completed.IsFinal, Is.True);
        Assert.That(completed.Count, Is.EqualTo(1));
        Assert.That(collector.CountCollectedXmlEvents(), Is.EqualTo(1));
        Assert.That(collector.SnapshotCollectedXmlEvents()[0].Kind, Is.EqualTo(XmlEventKind.EndDocument));
    }

    [Test]
    public void DiagnosticCollectorRetainsOrderedSnapshotsAndSupportsClearAndReset()
    {
        XmlDiagnosticCollector collector = new XmlDiagnosticCollector();
        collector.ConfigureXmlDiagnosticCollection(2);
        XmlDiagnostic first = Diagnostic("first");
        XmlDiagnostic second = Diagnostic("second");

        collector.ReportXmlDiagnostic(first);
        collector.ReportXmlDiagnostic(second);

        Assert.That(collector.CountCollectedXmlDiagnostics(), Is.EqualTo(2));
        XmlDiagnostic[] snapshot = collector.SnapshotCollectedXmlDiagnostics();
        Assert.That(snapshot, Is.EqualTo(new[] { first, second }));
        snapshot[0] = new XmlDiagnostic();
        Assert.That(collector.SnapshotCollectedXmlDiagnostics()[0], Is.SameAs(first));

        collector.ClearCollectedXmlDiagnostics();
        Assert.That(collector.CountCollectedXmlDiagnostics(), Is.Zero);
        collector.ReportXmlDiagnostic(first);
        collector.ReportXmlDiagnostic(second);

        collector.ResetXmlDiagnosticCollection();
        Assert.That(collector.CountCollectedXmlDiagnostics(), Is.Zero);
        collector.ReportXmlDiagnostic(first);
        collector.ReportXmlDiagnostic(second);
        collector.ReportXmlDiagnostic(Diagnostic("third"));
        Assert.That(collector.CountCollectedXmlDiagnostics(), Is.EqualTo(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => collector.ConfigureXmlDiagnosticCollection(-1));
        Assert.Throws<ArgumentNullException>(() => collector.ReportXmlDiagnostic(null!));
    }

    [Test]
    public void DiagnosticCollectorRaisesTypedFailureWithoutDiscardingRetainedItems()
    {
        XmlDiagnosticCollector collector = new XmlDiagnosticCollector();
        collector.ConfigureXmlDiagnosticCollection(1);
        XmlDiagnostic retained = Diagnostic("retained");
        XmlDiagnostic rejected = Diagnostic("rejected");
        SourceLocation location = new SourceLocation();
        SourceRange range = new SourceRange();
        range.Start = location;
        rejected.Range = range;
        collector.ReportXmlDiagnostic(retained);

        XmlLimitException failure = Assert.Throws<XmlLimitException>(() => collector.ReportXmlDiagnostic(rejected))!;

        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
        Assert.That(failure.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Limit));
        Assert.That(failure.Location, Is.SameAs(location));
        Assert.That(collector.CountCollectedXmlDiagnostics(), Is.EqualTo(1));
        Assert.That(collector.SnapshotCollectedXmlDiagnostics()[0], Is.SameAs(retained));
    }

    [Test]
    public void DiagnosticCollectorIntegratesWithAccumulatingParserValidation()
    {
        XmlDiagnosticCollector collector = new XmlDiagnosticCollector();
        XmlParserOptions options = Options();
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.ValidationMode = XmlValidationMode.Dtd;
        options.AccumulateValidationDiagnostics = true;
        XmlInput input = new XmlInput();
        input.Text = "<!DOCTYPE root [<!ELEMENT root EMPTY>]><root unknown='value'/>";

        XmlDocument document = new XmlParser().ParseXmlDocument(input, options, null, collector, null);

        Assert.That(collector.CountCollectedXmlDiagnostics(), Is.EqualTo(1));
        Assert.That(collector.SnapshotCollectedXmlDiagnostics()[0], Is.SameAs(document.Diagnostics![0]));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo(XmlErrorCodes.DtdValidity));
    }

    private static XmlEvent Event(XmlEventKind kind)
    {
        XmlEvent item = new XmlEvent();
        item.Kind = kind;
        return item;
    }

    private static XmlDiagnostic Diagnostic(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Message = message;
        return diagnostic;
    }

    private static XmlParserOptions Options()
    {
        return new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Disabled,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None
        };
    }
}
