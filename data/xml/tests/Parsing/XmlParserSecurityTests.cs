using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class XmlParserSecurityTests
{
    [Test]
    public void StopsOwnedCharacterInputAtTheConfiguredCharacterLimit()
    {
        string text = "<root>" + new string('x', 200) + "</root>";
        TrackingTextReader reader = new TrackingTextReader(text);
        XmlSecurityLimits limits = CreateSecurityLimits();
        limits.MaximumInputCharacters = 8;
        limits.CancellationPollingInterval = 1;
        XmlParserOptions options = CreateParserOptions(limits);
        RecordingReporter reporter = new RecordingReporter();

        XmlLimitException failure = Assert.Throws<XmlLimitException>(() => new XmlParser().ParseXmlDocument(ReaderInput(reader, true), options, null, reporter, null))!;

        AssertLimitFailure(failure, reporter, "total input characters", 8, 9);
        Assert.That(reader.CharactersRead, Is.LessThan(text.Length));
        Assert.That(reader.Disposed, Is.True);
    }

    [Test]
    public void CountsTheByteOrderMarkTowardTheConfiguredByteLimit()
    {
        byte[] document = Encoding.UTF8.GetBytes("\uFEFF<r/>" + new string('x', 200));
        TrackingMemoryStream stream = new TrackingMemoryStream(document);
        XmlSecurityLimits limits = CreateSecurityLimits();
        limits.MaximumInputBytes = 6;
        limits.CancellationPollingInterval = 1;
        RecordingReporter reporter = new RecordingReporter();

        XmlLimitException failure = Assert.Throws<XmlLimitException>(() => new XmlParser().ParseXmlDocument(StreamInput(stream, true), CreateParserOptions(limits), null, reporter, null))!;

        AssertLimitFailure(failure, reporter, "total input bytes", 6, 7);
        Assert.That(stream.BytesRead, Is.LessThan(document.Length));
        Assert.That(stream.Disposed, Is.True);
    }

    [Test]
    public void RejectsDeepNestingAtTheConfiguredDepth()
    {
        StringBuilder text = new StringBuilder();
        int index = 0;
        while (index < 256)
        {
            text.Append("<n>");
            index = index + 1;
        }

        index = 0;
        while (index < 256)
        {
            text.Append("</n>");
            index = index + 1;
        }

        XmlSecurityLimits limits = CreateSecurityLimits();
        limits.MaximumDepth = 32;

        AssertPublicLimit(text.ToString(), limits, "element depth", 32, 33);
    }

    [Test]
    public void RejectsAParsedTreeBeyondTheConfiguredNodeCount()
    {
        XmlSecurityLimits limits = CreateSecurityLimits();
        limits.MaximumNodeCount = 2;

        AssertPublicLimit("<root><child/></root>", limits, "node count", 2, 3);
    }

    [Test]
    public void RejectsAnElementBeyondTheConfiguredAttributeCount()
    {
        XmlSecurityLimits limits = CreateSecurityLimits();
        limits.MaximumAttributesPerElement = 1;

        AssertPublicLimit("<r a='1' b='2'/>", limits, "attributes per element", 1, 2);
    }

    [Test]
    public void RejectsAnXmlNameBeyondTheConfiguredLength()
    {
        XmlSecurityLimits limits = CreateSecurityLimits();
        limits.MaximumNameLength = 4;

        AssertPublicLimit("<oversized/>", limits, "XML name length", 4, 9);
    }

    [Test]
    public void RejectsATextNodeBeyondTheConfiguredLength()
    {
        XmlSecurityLimits limits = CreateSecurityLimits();
        limits.MaximumTextLength = 4;

        AssertPublicLimit("<r>12345</r>", limits, "text node length", 4, 5);
    }

    [Test]
    public void RejectsAGiantAttributeTokenAtTheLexicalBoundary()
    {
        XmlSecurityLimits limits = CreateSecurityLimits();
        limits.MaximumTokenLength = 64;
        string text = "<r a='" + new string('x', 65) + "'/>";

        AssertPublicLimit(text, limits, "lexical token length", 64, 67);
    }

    [Test]
    public void PollsCancellationWhileReadingOwnedCharacterInput()
    {
        string text = "<root>" + new string('x', 200) + "</root>";
        TrackingTextReader reader = new TrackingTextReader(text);
        XmlSecurityLimits limits = CreateSecurityLimits();
        limits.CancellationPollingInterval = 1;
        CountingCancellation cancellation = new CountingCancellation(5);

        Assert.Throws<OperationCanceledException>(() => new XmlParser().ParseXmlDocument(ReaderInput(reader, true), CreateParserOptions(limits), null, null, cancellation));

        Assert.That(cancellation.PollCount, Is.EqualTo(5));
        Assert.That(reader.CharactersRead, Is.LessThan(text.Length));
        Assert.That(reader.Disposed, Is.True);
    }

    [Test]
    public void PollsCancellationWhileReadingOwnedByteInput()
    {
        byte[] text = Encoding.UTF8.GetBytes("<root>" + new string('x', 200) + "</root>");
        TrackingMemoryStream stream = new TrackingMemoryStream(text);
        XmlSecurityLimits limits = CreateSecurityLimits();
        limits.CancellationPollingInterval = 1;
        CountingCancellation cancellation = new CountingCancellation(5);

        Assert.Throws<OperationCanceledException>(() => new XmlParser().ParseXmlDocument(StreamInput(stream, true), CreateParserOptions(limits), null, null, cancellation));

        Assert.That(cancellation.PollCount, Is.EqualTo(5));
        Assert.That(stream.BytesRead, Is.LessThan(text.Length));
        Assert.That(stream.Disposed, Is.True);
    }

    [Test]
    public void PollsCancellationDuringGrammarTokenScanning()
    {
        XmlSecurityLimits limits = CreateSecurityLimits();
        limits.CancellationPollingInterval = 1;
        CountingCancellation cancellation = new CountingCancellation(7);

        Assert.Throws<OperationCanceledException>(() => new XmlParser().ParseXmlDocument(StringInput("<r/>"), CreateParserOptions(limits), null, null, cancellation));

        Assert.That(cancellation.PollCount, Is.EqualTo(7));
    }

    [Test]
    public void PollsCancellationDuringTreeAssembly()
    {
        XmlSecurityLimits limits = CreateSecurityLimits();
        limits.CancellationPollingInterval = 1;
        CountingCancellation cancellation = new CountingCancellation(12);

        Assert.Throws<OperationCanceledException>(() => new XmlParser().ParseXmlDocument(StringInput("<r/>"), CreateParserOptions(limits), null, null, cancellation));

        Assert.That(cancellation.PollCount, Is.EqualTo(12));
    }

    [TestCase("<root")]
    [TestCase("<root>")]
    [TestCase("<root><!--unterminated")]
    [TestCase("<root><![CDATA[unterminated")]
    [TestCase("<root><?pi data")]
    [TestCase("<root a='")]
    [TestCase("<root></root")]
    public void ClosesOwnedCharacterInputForMalformedEndOfFile(string text)
    {
        TrackingTextReader reader = new TrackingTextReader(text);
        RecordingReporter reporter = new RecordingReporter();

        XmlWellFormednessException failure = Assert.Throws<XmlWellFormednessException>(() => new XmlParser().ParseXmlDocument(ReaderInput(reader, true), CreateParserOptions(CreateSecurityLimits()), null, reporter, null))!;

        Assert.That(failure.Diagnostic, Is.SameAs(reporter.Diagnostic));
        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.MalformedMarkup));
        Assert.That(reader.Disposed, Is.True);
    }

    [Test]
    public void LeavesBorrowedInputOpenAfterAParseFailure()
    {
        TrackingTextReader reader = new TrackingTextReader("<root>");

        Assert.Throws<XmlWellFormednessException>(() => new XmlParser().ParseXmlDocument(ReaderInput(reader, false), CreateParserOptions(CreateSecurityLimits()), null, null, null));

        Assert.That(reader.Disposed, Is.False);
    }

    private static void AssertPublicLimit(string text, XmlSecurityLimits limits, string dimension, long maximum, long attempted)
    {
        RecordingReporter reporter = new RecordingReporter();

        XmlLimitException failure = Assert.Throws<XmlLimitException>(() => new XmlParser().ParseXmlDocument(StringInput(text), CreateParserOptions(limits), null, reporter, null))!;

        AssertLimitFailure(failure, reporter, dimension, maximum, attempted);
    }

    private static void AssertLimitFailure(XmlLimitException failure, RecordingReporter reporter, string dimension, long maximum, long attempted)
    {
        Assert.That(failure.Message, Does.Contain(dimension));
        Assert.That(failure.Message, Does.Contain(maximum.ToString()));
        Assert.That(failure.Message, Does.Contain(attempted.ToString()));
        Assert.That(failure.Diagnostic, Is.SameAs(reporter.Diagnostic));
        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
        Assert.That(failure.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Limit));
        Assert.That(failure.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
    }

    private static XmlParserOptions CreateParserOptions(XmlSecurityLimits limits)
    {
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = XmlNamespaceHandling.Disabled;
        options.DtdProcessing = XmlDtdProcessing.Prohibit;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.ValidationMode = XmlValidationMode.None;
        options.SecurityLimits = limits;
        return options;
    }

    private static XmlSecurityLimits CreateSecurityLimits()
    {
        XmlSecurityLimits limits = new XmlSecurityLimits();
        limits.MaximumDepth = 1024;
        limits.MaximumNodeCount = 100000;
        limits.MaximumAttributesPerElement = 1000;
        limits.MaximumNameLength = 1000;
        limits.MaximumTextLength = 100000;
        limits.MaximumTokenLength = 100000;
        limits.MaximumInputCharacters = 100000;
        limits.MaximumInputBytes = 100000;
        limits.MaximumDtdCharacters = 100000;
        limits.MaximumEntityDepth = 100;
        limits.MaximumEntityCount = 100000;
        limits.MaximumEntityCharacters = 100000;
        limits.MaximumEntityAmplification = 100;
        limits.MaximumExternalResources = 100;
        limits.MaximumResourceDepth = 100;
        limits.MaximumResourceBytes = 100000;
        limits.CancellationPollingInterval = 8;
        return limits;
    }

    private static XmlInput StringInput(string text)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = text;
        return input;
    }

    private static XmlInput ReaderInput(TextReader reader, bool ownsResource)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.TextReader;
        input.TextReader = reader;
        input.OwnsResource = ownsResource;
        return input;
    }

    private static XmlInput StreamInput(Stream stream, bool ownsResource)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.Stream;
        input.ByteStream = stream;
        input.OwnsResource = ownsResource;
        return input;
    }

    private sealed class RecordingReporter : IXmlErrorReporter
    {
        internal XmlDiagnostic? Diagnostic;

        public void ReportXmlDiagnostic(XmlDiagnostic diagnostic)
        {
            Diagnostic = diagnostic;
        }
    }

    private sealed class CountingCancellation : IXmlCancellation
    {
        internal int CancelAtPoll;
        internal int PollCount;

        internal CountingCancellation(int cancelAtPoll)
        {
            CancelAtPoll = cancelAtPoll;
            PollCount = 0;
        }

        public bool IsXmlCancellationRequested()
        {
            PollCount = PollCount + 1;
            return PollCount >= CancelAtPoll;
        }
    }

    private sealed class TrackingTextReader : TextReader
    {
        internal string SourceText;
        internal int CharactersRead;
        internal bool Disposed;

        internal TrackingTextReader(string text)
        {
            SourceText = text;
            CharactersRead = 0;
            Disposed = false;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            int remaining = SourceText.Length - CharactersRead;
            int copied = count < remaining ? count : remaining;
            if (copied > 0)
            {
                SourceText.CopyTo(CharactersRead, buffer, index, copied);
                CharactersRead = CharactersRead + copied;
            }

            return copied;
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class TrackingMemoryStream : MemoryStream
    {
        internal int BytesRead;
        internal bool Disposed;

        internal TrackingMemoryStream(byte[] bytes) : base(bytes)
        {
            BytesRead = 0;
            Disposed = false;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = base.Read(buffer, offset, count);
            BytesRead = BytesRead + read;
            return read;
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
