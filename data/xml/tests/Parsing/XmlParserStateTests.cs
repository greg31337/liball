using System;
using System.Collections.Generic;
using xml;

namespace xml.Tests;

[TestFixture]
public class XmlParserStateTests
{
    [Test]
    public void FollowsTheSuccessfulParserLifecycle()
    {
        XmlParserState state = CreateState(null);

        Assert.That(state.CurrentXmlParserStatus(), Is.EqualTo(XmlParserStatus.Ready));
        state.BeginXmlParsing();
        Assert.That(state.CurrentXmlParserStatus(), Is.EqualTo(XmlParserStatus.Running));
        state.NeedXmlParserInput();
        Assert.That(state.CurrentXmlParserStatus(), Is.EqualTo(XmlParserStatus.NeedInput));
        state.ResumeXmlParsing();
        state.PauseXmlParsing();
        Assert.That(state.CurrentXmlParserStatus(), Is.EqualTo(XmlParserStatus.Paused));
        state.ResumeXmlParsing();
        state.CompleteXmlParsing();

        Assert.That(state.CurrentXmlParserStatus(), Is.EqualTo(XmlParserStatus.Completed));
        Assert.Throws<InvalidOperationException>(() => state.EnsureXmlParsingCanContinue());
    }

    [Test]
    public void FatalFailureMapsToAWellFormednessExceptionAndStopsParsing()
    {
        RecordingReporter reporter = new RecordingReporter();
        XmlParserState state = CreateState(reporter);
        state.BeginXmlParsing();
        SourceLocation location = CreateLocation();

        XmlWellFormednessException failure = state.FailXmlParsing(new FormatException("A start tag is incomplete"), location);

        Assert.That(state.CurrentXmlParserStatus(), Is.EqualTo(XmlParserStatus.Error));
        Assert.That(failure.Message, Is.EqualTo("A start tag is incomplete"));
        Assert.That(failure.Location, Is.SameAs(location));
        Assert.That(failure.Diagnostic, Is.Not.Null);
        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.MalformedMarkup));
        Assert.That(failure.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Xml));
        Assert.That(failure.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
        Assert.That(failure.Diagnostic.Range!.Start, Is.SameAs(location));
        Assert.That(failure.Diagnostic.Range.End, Is.SameAs(location));
        Assert.That(reporter.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(reporter.Diagnostics[0], Is.SameAs(failure.Diagnostic));
    }

    [Test]
    public void RetainsEarlierDiagnosticsButDiscardsUndeliveredEventsOnFatalFailure()
    {
        RecordingReporter reporter = new RecordingReporter();
        XmlParserState state = CreateState(reporter);
        XmlDiagnostic warning = CreateWarning();
        state.RecordXmlParserDiagnostic(warning);
        state.EventQueue!.EnqueueXmlTokenEvent(CreateComment(), 0, false);
        Assert.That(state.EventQueue.CountQueuedXmlEvents(), Is.EqualTo(1));

        XmlWellFormednessException failure = state.FailXmlParsing(new FormatException("Malformed markup"), null);
        XmlDiagnostic[] diagnostics = state.SnapshotXmlParserDiagnostics();

        Assert.That(state.EventQueue.CountQueuedXmlEvents(), Is.Zero);
        Assert.That(diagnostics, Has.Length.EqualTo(2));
        Assert.That(diagnostics[0], Is.SameAs(warning));
        Assert.That(diagnostics[1], Is.SameAs(failure.Diagnostic));
        Assert.That(reporter.Diagnostics, Is.EqualTo(diagnostics));
    }

    [Test]
    public void StrictModeRethrowsTheFirstFatalFailureUntilReset()
    {
        XmlParserState state = CreateState(null);
        state.BeginXmlParsing();
        XmlWellFormednessException first = state.FailXmlParsing(new FormatException("first"), null);

        Assert.That(Assert.Throws<XmlWellFormednessException>(() => state.EnsureXmlParsingCanContinue()), Is.SameAs(first));
        Assert.That(Assert.Throws<XmlWellFormednessException>(() => state.BeginXmlParsing()), Is.SameAs(first));
        Assert.That(Assert.Throws<XmlWellFormednessException>(() => state.CompleteXmlParsing()), Is.SameAs(first));

        XmlWellFormednessException repeated = state.FailXmlParsing(new FormatException("second"), null);
        Assert.That(repeated, Is.SameAs(first));
        Assert.That(state.SnapshotXmlParserDiagnostics(), Has.Length.EqualTo(1));
    }

    [Test]
    public void ResetDeterministicallyRestoresScannerQueueDiagnosticsAndStatus()
    {
        XmlParserState state = CreateState(null);
        state.BeginXmlParsing();
        state.Scanner!.ScanNextXmlToken();
        state.EventQueue!.EnqueueXmlTokenEvent(CreateComment(), 0, false);
        state.RecordXmlParserDiagnostic(CreateWarning());
        state.FailXmlParsing(new FormatException("bad"), null);

        state.ResetXmlParser();

        Assert.That(state.CurrentXmlParserStatus(), Is.EqualTo(XmlParserStatus.Ready));
        Assert.That(state.Scanner.CurrentXmlScannerOffset(), Is.Zero);
        Assert.That(state.EventQueue.CountQueuedXmlEvents(), Is.Zero);
        Assert.That(state.SnapshotXmlParserDiagnostics(), Is.Empty);
        Assert.That(state.FatalError, Is.Null);
        Assert.DoesNotThrow(() => state.BeginXmlParsing());
    }

    [Test]
    public void AbortDiscardsPendingEventsAndRequiresResetBeforeReuse()
    {
        XmlParserState state = CreateState(null);
        state.BeginXmlParsing();
        state.EventQueue!.EnqueueXmlTokenEvent(CreateComment(), 0, false);

        state.AbortXmlParsing();

        Assert.That(state.CurrentXmlParserStatus(), Is.EqualTo(XmlParserStatus.Aborted));
        Assert.That(state.EventQueue.CountQueuedXmlEvents(), Is.Zero);
        Assert.Throws<InvalidOperationException>(() => state.BeginXmlParsing());
        state.ResetXmlParser();
        Assert.DoesNotThrow(() => state.BeginXmlParsing());
    }

    [Test]
    public void CloseIsIdempotentTerminalAndRetainsDiagnosticsForInspection()
    {
        XmlParserState state = CreateState(null);
        XmlDiagnostic warning = CreateWarning();
        state.RecordXmlParserDiagnostic(warning);
        state.BeginXmlParsing();
        ParserEventQueue queue = state.EventQueue!;
        queue.EnqueueXmlTokenEvent(CreateComment(), 0, false);

        state.CloseXmlParser();
        state.CloseXmlParser();

        Assert.That(state.CurrentXmlParserStatus(), Is.EqualTo(XmlParserStatus.Closed));
        Assert.That(queue.CountQueuedXmlEvents(), Is.Zero);
        Assert.That(state.Scanner, Is.Null);
        Assert.That(state.EventQueue, Is.Null);
        Assert.That(state.ErrorReporter, Is.Null);
        Assert.That(state.SnapshotXmlParserDiagnostics(), Is.EqualTo(new[] { warning }));
        Assert.Throws<InvalidOperationException>(() => state.BeginXmlParsing());
        Assert.Throws<InvalidOperationException>(() => state.ResetXmlParser());
    }

    [Test]
    public void FatalStateIsCommittedEvenWhenTheReporterThrows()
    {
        XmlParserState state = CreateState(new ThrowingReporter());
        state.BeginXmlParsing();

        Assert.Throws<InvalidOperationException>(() => state.FailXmlParsing(new FormatException("bad"), null));

        Assert.That(state.CurrentXmlParserStatus(), Is.EqualTo(XmlParserStatus.Error));
        Assert.That(state.SnapshotXmlParserDiagnostics(), Has.Length.EqualTo(1));
        Assert.That(state.FatalError, Is.Not.Null);
        Assert.That(Assert.Throws<XmlWellFormednessException>(() => state.EnsureXmlParsingCanContinue()), Is.SameAs(state.FatalError));
    }

    [Test]
    public void CloseSupersedesAnErrorWithTheClosedTerminalState()
    {
        XmlParserState state = CreateState(null);
        state.BeginXmlParsing();
        state.FailXmlParsing(new FormatException("bad"), null);

        state.CloseXmlParser();

        Assert.That(state.CurrentXmlParserStatus(), Is.EqualTo(XmlParserStatus.Closed));
        Assert.Throws<InvalidOperationException>(() => state.BeginXmlParsing());
        Assert.Throws<InvalidOperationException>(() => state.FailXmlParsing(new FormatException("later"), null));
        Assert.That(state.SnapshotXmlParserDiagnostics(), Has.Length.EqualTo(1));
    }

    private static XmlParserState CreateState(IXmlErrorReporter? reporter)
    {
        XmlScanner scanner = new XmlScanner("<root/>".AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);
        XmlParserOptions options = new XmlParserOptions();
        options.PreserveComments = true;
        ParserEventQueue queue = new ParserEventQueue(options);
        return new XmlParserState(scanner, queue, reporter);
    }

    private static XmlDiagnostic CreateWarning()
    {
        return new XmlDiagnostic
        {
            Code = XmlErrorCodes.GeneralFailure,
            Category = XmlErrorCategory.General,
            Severity = XmlDiagnosticSeverity.Warning,
            Message = "warning"
        };
    }

    private static SourceLocation CreateLocation()
    {
        return new SourceLocation
        {
            SourceUri = "memory.xml",
            CharacterOffset = 4,
            ByteOffset = 4,
            Line = 1,
            Column = 5
        };
    }

    private static XmlToken CreateComment()
    {
        return new XmlToken(XmlTokenKind.Comment, null, "note", "<!--note-->", 0, 11, 0, false, '\0', false, null);
    }

    private sealed class RecordingReporter : IXmlErrorReporter
    {
        internal List<XmlDiagnostic> Diagnostics = new List<XmlDiagnostic>();

        public void ReportXmlDiagnostic(XmlDiagnostic diagnostic)
        {
            Diagnostics.Add(diagnostic);
        }
    }

    private sealed class ThrowingReporter : IXmlErrorReporter
    {
        public void ReportXmlDiagnostic(XmlDiagnostic diagnostic)
        {
            throw new InvalidOperationException("reporter failed");
        }
    }
}
