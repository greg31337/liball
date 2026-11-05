using System;
using System.IO;
using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class XmlParserTests
{
    [Test]
    public void ParsesDocumentMetadataAndAnOwnedTree()
    {
        XmlInput input = StringInput("<?xml version='1.1' standalone='yes'?><root a='x&amp;&#65;'>before<![CDATA[cdata]]><!--note--><?pi data?><child/>after</root>");
        input.BaseUri = "https://example.test/document.xml";
        XmlParserOptions options = DefaultOptions();
        options.PreserveCData = true;
        options.PreserveComments = true;
        options.PreserveProcessingInstructions = true;
        options.PreserveSourceLocations = true;

        XmlDocument document = new XmlParser().ParseXmlDocument(input, options, null, null, null);

        Assert.That(document.Version, Is.EqualTo(XmlVersion.Xml11));
        Assert.That(document.Standalone, Is.EqualTo(XmlStandalone.Yes));
        Assert.That(document.DocumentUri, Is.EqualTo(input.BaseUri));
        Assert.That(document.BaseUri, Is.EqualTo(input.BaseUri));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.DocumentNode!.Kind, Is.EqualTo(XmlNodeKind.Document));
        Assert.That(document.DocumentElement, Is.SameAs(document.DocumentNode.Children![0]));

        XmlNode root = document.DocumentElement!;
        Assert.That(root.Name!.QualifiedName, Is.EqualTo("root"));
        Assert.That(root.OwnerDocument, Is.SameAs(document));
        Assert.That(root.Parent, Is.SameAs(document.DocumentNode));
        Assert.That(root.Attributes, Has.Length.EqualTo(1));
        Assert.That(root.Attributes![0].Name!.QualifiedName, Is.EqualTo("a"));
        Assert.That(root.Attributes[0].Value, Is.EqualTo("x&A"));
        Assert.That(root.Attributes[0].Parent, Is.SameAs(root));
        Assert.That(root.Children, Has.Length.EqualTo(6));
        Assert.That(root.Children![0].Value, Is.EqualTo("before"));
        Assert.That(root.Children[1].Kind, Is.EqualTo(XmlNodeKind.CData));
        Assert.That(root.Children[1].Value, Is.EqualTo("cdata"));
        Assert.That(root.Children[2].Kind, Is.EqualTo(XmlNodeKind.Comment));
        Assert.That(root.Children[3].Kind, Is.EqualTo(XmlNodeKind.ProcessingInstruction));
        Assert.That(root.Children[3].Name!.QualifiedName, Is.EqualTo("pi"));
        Assert.That(root.Children[4].Kind, Is.EqualTo(XmlNodeKind.Element));
        Assert.That(root.Children[5].Value, Is.EqualTo("after"));
        Assert.That(root.Range!.Start!.CharacterOffset, Is.EqualTo(38));
        Assert.That(root.Range.End!.CharacterOffset, Is.EqualTo(input.Text!.Length));
    }

    [Test]
    public void AppliesDefaultLexicalPreservationChoices()
    {
        XmlDocument document = new XmlParser().ParseXmlDocument(StringInput("<r>a<![CDATA[b]]>c<!--note--><?pi data?></r>"), DefaultOptions(), null, null, null);

        Assert.That(document.DocumentElement!.Children, Has.Length.EqualTo(1));
        Assert.That(document.DocumentElement.Children![0].Kind, Is.EqualTo(XmlNodeKind.Text));
        Assert.That(document.DocumentElement.Children[0].Value, Is.EqualTo("abc"));
        Assert.That(document.DocumentElement.Range, Is.Null);
    }

    [Test]
    public void ParsesEveryXmlInputRepresentation()
    {
        string xmlText = "<root>value</root>";
        XmlInput[] inputs =
        [
            StringInput(xmlText),
            CharacterInput(xmlText),
            ByteInput(Encoding.UTF8.GetBytes(xmlText)),
            StreamInput(new MemoryStream(Encoding.UTF8.GetBytes(xmlText)), false),
            ReaderInput(new StringReader(xmlText), false)
        ];

        int index = 0;
        while (index < inputs.Length)
        {
            XmlDocument document = new XmlParser().ParseXmlDocument(inputs[index], DefaultOptions(), null, null, null);
            Assert.That(document.DocumentElement!.Name!.LocalName, Is.EqualTo("root"), "input index " + index);
            Assert.That(document.DocumentElement.Children![0].Value, Is.EqualTo("value"), "input index " + index);
            index = index + 1;
        }
    }

    [Test]
    public void RetainsDetectedByteEncodingAndClosesOwnedInputs()
    {
        TrackingMemoryStream stream = new TrackingMemoryStream(Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("<root/>" )).ToArray());
        XmlInput streamInput = StreamInput(stream, true);
        TrackingStringReader reader = new TrackingStringReader("<root/>");
        XmlInput readerInput = ReaderInput(reader, true);

        XmlDocument byteDocument = new XmlParser().ParseXmlDocument(streamInput, DefaultOptions(), null, null, null);
        XmlDocument characterDocument = new XmlParser().ParseXmlDocument(readerInput, DefaultOptions(), null, null, null);

        Assert.That(byteDocument.EncodingName, Is.EqualTo("utf-16"));
        Assert.That(characterDocument.EncodingName, Is.Null);
        Assert.That(stream.Disposed, Is.True);
        Assert.That(reader.Disposed, Is.True);
    }

    [Test]
    public void ParsesBalancedFragmentsIntoAFragmentContainer()
    {
        XmlInput input = StringInput("leading<a/>middle<b><c/></b>trailing");
        input.BaseUri = "urn:fragment";

        XmlDocument document = new XmlParser().ParseXmlFragment(input, DefaultOptions(), null, null, null);

        Assert.That(document.DocumentNode!.Kind, Is.EqualTo(XmlNodeKind.DocumentFragment));
        Assert.That(document.DocumentElement, Is.Null);
        Assert.That(document.DocumentNode.Children, Has.Length.EqualTo(5));
        Assert.That(document.DocumentNode.Children![0].Value, Is.EqualTo("leading"));
        Assert.That(document.DocumentNode.Children[1].Name!.LocalName, Is.EqualTo("a"));
        Assert.That(document.DocumentNode.Children[2].Value, Is.EqualTo("middle"));
        Assert.That(document.DocumentNode.Children[3].Children![0].Name!.LocalName, Is.EqualTo("c"));
        Assert.That(document.DocumentNode.Children[4].Value, Is.EqualTo("trailing"));
        Assert.That(document.BaseUri, Is.EqualTo("urn:fragment"));
    }

    [Test]
    public void AppliesDocumentTypePolicyAndReportsProhibition()
    {
        XmlInput input = StringInput("<!DOCTYPE root><root/>");
        RecordingReporter reporter = new RecordingReporter();
        XmlParserOptions prohibited = DefaultOptions();

        XmlWellFormednessException failure = Assert.Throws<XmlWellFormednessException>(() => new XmlParser().ParseXmlDocument(input, prohibited, null, reporter, null))!;
        Assert.That(failure.Diagnostic, Is.SameAs(reporter.Diagnostic));

        XmlParserOptions ignored = DefaultOptions();
        ignored.DtdProcessing = XmlDtdProcessing.Ignore;
        XmlDocument ignoredDocument = new XmlParser().ParseXmlDocument(input, ignored, null, null, null);
        Assert.That(ignoredDocument.DocumentType, Is.Null);
        Assert.That(ignoredDocument.DocumentNode!.Children, Has.Length.EqualTo(1));

        XmlParserOptions parsed = DefaultOptions();
        parsed.DtdProcessing = XmlDtdProcessing.Parse;
        XmlDocument parsedDocument = new XmlParser().ParseXmlDocument(input, parsed, null, null, null);
        Assert.That(parsedDocument.DocumentType!.Name!.QualifiedName, Is.EqualTo("root"));
        Assert.That(parsedDocument.DocumentNode!.Children![0].Kind, Is.EqualTo(XmlNodeKind.DocumentType));
    }

    [Test]
    public void EnforcesConfiguredCancellationAndInputLimits()
    {
        XmlParserOptions options = DefaultOptions();
        options.SecurityLimits = LimitsForInput(4);
        RecordingReporter reporter = new RecordingReporter();
        TrackingMemoryStream canceledStream = new TrackingMemoryStream(Encoding.UTF8.GetBytes("<root/>"));

        XmlLimitException limit = Assert.Throws<XmlLimitException>(() => new XmlParser().ParseXmlDocument(StringInput("<root/>"), options, null, reporter, null))!;
        Assert.That(reporter.Diagnostic, Is.SameAs(limit.Diagnostic));
        Assert.Throws<OperationCanceledException>(() => new XmlParser().ParseXmlDocument(StreamInput(canceledStream, true), DefaultOptions(), null, null, new AlwaysCanceled()));
        Assert.That(canceledStream.ReadCount, Is.Zero);
        Assert.That(canceledStream.Disposed, Is.True);
    }

    [Test]
    public void ValidatesRequiredArgumentsAndInputPayloads()
    {
        XmlParser parser = new XmlParser();

        Assert.Throws<ArgumentNullException>(() => parser.ParseXmlDocument(null!, DefaultOptions(), null, null, null));
        Assert.Throws<ArgumentNullException>(() => parser.ParseXmlDocument(StringInput("<r/>"), null!, null, null, null));
        Assert.Throws<ArgumentException>(() => parser.ParseXmlDocument(new XmlInput(), DefaultOptions(), null, null, null));
    }

    [Test]
    public void MapsStructuralFailuresAndReportsTheSameFatalDiagnostic()
    {
        XmlInput input = StringInput("<root>\n<child></root>");
        input.BaseUri = "memory.xml";
        RecordingReporter reporter = new RecordingReporter();

        XmlWellFormednessException failure = Assert.Throws<XmlWellFormednessException>(() => new XmlParser().ParseXmlDocument(input, DefaultOptions(), null, reporter, null))!;

        Assert.That(failure.Diagnostic, Is.SameAs(reporter.Diagnostic));
        Assert.That(failure.Diagnostic!.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
        Assert.That(failure.Diagnostic.Code, Is.EqualTo(XmlErrorCodes.MalformedMarkup));
        Assert.That(failure.Location!.SourceUri, Is.EqualTo("memory.xml"));
        Assert.That(failure.Location.Line, Is.EqualTo(2));
        Assert.That(failure.Location.Column, Is.EqualTo(10));
    }

    [TestCase("<root>bad]]>text</root>")]
    [TestCase("<root><!--bad--comment--></root>")]
    [TestCase("<root><?target/data?></root>")]
    [TestCase("<root value='&missing'>text</root>")]
    public void MapsForbiddenMarkupSequencesThroughThePublicStrictErrorPath(string text)
    {
        RecordingReporter reporter = new RecordingReporter();

        XmlWellFormednessException failure = Assert.Throws<XmlWellFormednessException>(() => new XmlParser().ParseXmlDocument(StringInput(text), DefaultOptions(), null, reporter, null))!;

        Assert.That(failure.Diagnostic, Is.SameAs(reporter.Diagnostic));
        Assert.That(failure.Diagnostic!.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
    }

    [Test]
    public void TreatsDoctypeRootMismatchAsAValidityConstraint()
    {
        XmlParserOptions options = DefaultOptions();
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.ValidationMode = XmlValidationMode.Dtd;
        RecordingReporter reporter = new RecordingReporter();

        XmlValidationException failure = Assert.Throws<XmlValidationException>(() => new XmlParser().ParseXmlDocument(StringInput("<!DOCTYPE expected><actual/>"), options, null, reporter, null))!;

        Assert.That(failure.Message, Does.Contain("does not match"));
        Assert.That(failure.Diagnostic, Is.SameAs(reporter.Diagnostic));
    }

    private static XmlParserOptions DefaultOptions()
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

    private static XmlSecurityLimits LimitsForInput(long maximumCharacters)
    {
        return new XmlSecurityLimits
        {
            MaximumDepth = 16,
            MaximumNodeCount = 100,
            MaximumAttributesPerElement = 16,
            MaximumNameLength = 64,
            MaximumTextLength = 1024,
            MaximumTokenLength = 1024,
            MaximumInputCharacters = maximumCharacters,
            MaximumInputBytes = 1024,
            CancellationPollingInterval = 1
        };
    }

    private static XmlInput StringInput(string text)
    {
        return new XmlInput { Kind = XmlInputKind.String, Text = text };
    }

    private static XmlInput CharacterInput(string text)
    {
        return new XmlInput { Kind = XmlInputKind.CharacterMemory, Characters = text.AsMemory() };
    }

    private static XmlInput ByteInput(byte[] bytes)
    {
        return new XmlInput { Kind = XmlInputKind.ByteMemory, Bytes = bytes };
    }

    private static XmlInput StreamInput(Stream stream, bool ownsResource)
    {
        return new XmlInput { Kind = XmlInputKind.Stream, ByteStream = stream, OwnsResource = ownsResource };
    }

    private static XmlInput ReaderInput(TextReader reader, bool ownsResource)
    {
        return new XmlInput { Kind = XmlInputKind.TextReader, TextReader = reader, OwnsResource = ownsResource };
    }

    private sealed class RecordingReporter : IXmlErrorReporter
    {
        internal XmlDiagnostic? Diagnostic;

        public void ReportXmlDiagnostic(XmlDiagnostic diagnostic)
        {
            Diagnostic = diagnostic;
        }
    }

    private sealed class AlwaysCanceled : IXmlCancellation
    {
        public bool IsXmlCancellationRequested()
        {
            return true;
        }
    }

    private sealed class TrackingMemoryStream : MemoryStream
    {
        internal bool Disposed;
        internal int ReadCount;

        internal TrackingMemoryStream(byte[] bytes) : base(bytes)
        {
        }

        public override int Read(Span<byte> buffer)
        {
            ReadCount = ReadCount + 1;
            return base.Read(buffer);
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class TrackingStringReader : StringReader
    {
        internal bool Disposed;

        internal TrackingStringReader(string text) : base(text)
        {
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
