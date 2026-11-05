using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class XsltStreamingPipelineTests
{
    [Test]
    public void EventPipelineTransformsDropsAndResumesWithBackpressure()
    {
        RecordingEventSink sink = new RecordingEventSink();
        sink.PauseAfter = 1;
        XmlEventPipeline pipeline = new XmlEventPipeline([new UppercaseTextTransform(), new DropCommentTransform()], sink);
        XmlEvent[] events =
        [
            new XmlEvent { Kind = XmlEventKind.Text, Value = "one" },
            new XmlEvent { Kind = XmlEventKind.Comment, Value = "ignored" },
            new XmlEvent { Kind = XmlEventKind.Text, Value = "two" }
        ];

        XmlHandlerAction paused = pipeline.ProcessXmlEventBatch(new XmlEventBatch { Events = events, Offset = 0, Count = events.Length, IsFinal = true });
        sink.PauseAfter = 0;
        XmlHandlerAction completed = pipeline.ProcessXmlEventBatch(new XmlEventBatch { Events = events, Offset = 1, Count = 2, IsFinal = true });

        Assert.Multiple(() =>
        {
            Assert.That(paused, Is.EqualTo(XmlHandlerAction.Pause));
            Assert.That(completed, Is.EqualTo(XmlHandlerAction.Continue));
            Assert.That(sink.Values, Is.EqualTo(new string[] { "ONE", "TWO" }));
            Assert.That(sink.Completed, Is.True);
            Assert.That(pipeline.ReadEventCount, Is.EqualTo(3));
            Assert.That(pipeline.WrittenEventCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void XmlSerializationSinkWritesPartialBatchesWithBoundedBuffers()
    {
        string first = new string('x', 12000) + "&";
        string second = "<" + new string('y', 12000);
        RecordingByteSink bytes = new RecordingByteSink();
        XmlSerializationOptions options = new XmlSerializationOptions
        {
            Method = XmlSerializationMethod.Xml,
            OmitXmlDeclaration = true,
            InvalidCharacterPolicy = XmlInvalidCharacterPolicy.Error
        };
        XmlSerializationEventSink sink = new XmlSerializationEventSink(new XmlOutput { Kind = XmlOutputKind.ByteSink, ByteSink = bytes }, options);
        XmlEventPipeline pipeline = new XmlEventPipeline([], sink);
        pipeline.ProcessXmlEventBatch(new XmlEventBatch
        {
            Events =
            [
                new XmlEvent { Kind = XmlEventKind.StartDocument },
                new XmlEvent
                {
                    Kind = XmlEventKind.StartElement,
                    Name = new XmlName { Prefix = "p", LocalName = "root", QualifiedName = "p:root", NamespaceUri = "urn:root" },
                    NamespaceBindings = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:root" }],
                    Attributes = [new XmlAttributeData { Name = Name("a"), Value = "\"&" }]
                },
                new XmlEvent { Kind = XmlEventKind.Text, Value = first }
            ],
            Count = 3,
            IsFinal = false
        });
        int partialByteCount = bytes.Bytes.Count;
        pipeline.ProcessXmlEventBatch(new XmlEventBatch
        {
            Events =
            [
                new XmlEvent { Kind = XmlEventKind.Text, Value = second },
                new XmlEvent { Kind = XmlEventKind.EndElement, Name = new XmlName { Prefix = "p", LocalName = "root", QualifiedName = "p:root", NamespaceUri = "urn:root" } },
                new XmlEvent { Kind = XmlEventKind.EndDocument }
            ],
            Count = 3,
            IsFinal = true
        });

        string serialized = Encoding.UTF8.GetString(bytes.Bytes.ToArray());
        Assert.Multiple(() =>
        {
            Assert.That(partialByteCount, Is.GreaterThan(first.Length));
            Assert.That(serialized, Is.EqualTo("<p:root xmlns:p=\"urn:root\" a=\"&quot;&amp;\">" + new string('x', 12000) + "&amp;&lt;" + new string('y', 12000) + "</p:root>"));
            Assert.That(sink.PeakBufferedCharacterCount, Is.LessThanOrEqualTo(XmlSerializationEventSink.MaximumBufferedCharacterCount));
            Assert.That(bytes.BlockSizes, Has.All.LessThanOrEqualTo(4096));
        });
    }

    [TestCase(XmlCanonicalizationMethod.CanonicalXml11)]
    [TestCase(XmlCanonicalizationMethod.ExclusiveXml10)]
    [TestCase(XmlCanonicalizationMethod.CanonicalXml20Experimental)]
    public void CanonicalEventSinkMatchesGroundedOutputInBoundedBlocks(XmlCanonicalizationMethod method)
    {
        string source = "<?before value?><!--before--><p:root xmlns:p='urn:root' xmlns:q='urn:q' q:z='2' a='1'>" + new string('x', 12000) + "</p:root><!--after--><?after value?>";
        XmlDocument document = Parse(source);
        XmlParserOptions parserOptions = ParserOptions();
        XmlEventBatch events = new XmlEventEmitter().EmitXmlNodeEvents(document.DocumentNode!, parserOptions);
        XmlCanonicalizationOptions options = new XmlCanonicalizationOptions { Method = method, IncludeComments = true };
        StringWriter groundedWriter = new StringWriter();
        new XmlCanonicalizer().CanonicalizeXml(document, new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = groundedWriter }, options);
        RecordingByteSink bytes = new RecordingByteSink();

        new XmlCanonicalizer().CanonicalizeXmlEvents(events, new XmlOutput { Kind = XmlOutputKind.ByteSink, ByteSink = bytes }, options);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.UTF8.GetString(bytes.Bytes.ToArray()), Is.EqualTo(groundedWriter.ToString()));
            Assert.That(bytes.BlockSizes, Has.Count.GreaterThan(5));
            Assert.That(bytes.BlockSizes, Has.All.LessThanOrEqualTo(4096));
        });
    }

    private static XmlName Name(string localName)
    {
        return new XmlName { LocalName = localName, QualifiedName = localName };
    }

    private static XmlDocument Parse(string source)
    {
        return new XmlParser().ParseXmlDocument(new XmlInput { Kind = XmlInputKind.String, Text = source }, ParserOptions(), null, null, null);
    }

    private static XmlParserOptions ParserOptions()
    {
        return new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreservePrefixes = true,
            RetainNamespaceDeclarations = true
        };
    }

    private sealed class UppercaseTextTransform : IXmlEventTransform
    {
        public XmlEvent? TransformXmlEvent(XmlEvent value)
        {
            if (value.Kind != XmlEventKind.Text)
            {
                return value;
            }

            return new XmlEvent { Kind = value.Kind, Value = (value.Value == null ? "" : value.Value).ToUpperInvariant() };
        }
    }

    private sealed class DropCommentTransform : IXmlEventTransform
    {
        public XmlEvent? TransformXmlEvent(XmlEvent value)
        {
            return value.Kind == XmlEventKind.Comment ? null : value;
        }
    }

    private sealed class RecordingEventSink : IXmlEventHandler, IXmlEventCompletion
    {
        internal List<string> Values = [];
        internal int PauseAfter;
        internal bool Completed;

        public XmlHandlerAction HandleXmlEvent(XmlEvent value)
        {
            Values.Add(value.Value == null ? "" : value.Value);
            return PauseAfter > 0 && Values.Count == PauseAfter ? XmlHandlerAction.Pause : XmlHandlerAction.Continue;
        }

        public void CompleteXmlEventSink()
        {
            Completed = true;
        }
    }

    private sealed class RecordingByteSink : IXmlByteSink
    {
        internal List<byte> Bytes = [];
        internal List<int> BlockSizes = [];

        public void WriteXmlBytes(ReadOnlyMemory<byte> bytes)
        {
            Bytes.AddRange(bytes.ToArray());
            BlockSizes.Add(bytes.Length);
        }
    }
}
