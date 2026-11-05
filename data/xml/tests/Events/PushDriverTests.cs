using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class PushDriverTests
{
    [Test]
    public void CharacterBlocksIncludeInitialContentAndCompleteAValidDocument()
    {
        XmlPushParser parser = new XmlPushParser();
        XmlInput input = CharacterInput("<ro");

        Assert.That(parser.BeginXmlPush(input, DefaultOptions(), null, null, null, null), Is.EqualTo(XmlParserStatus.Running));
        XmlEventBatch pending = parser.FeedXmlPushCharacters("ot>value".AsMemory(), false);

        Assert.That(pending.Count, Is.Zero);
        Assert.That(pending.IsFinal, Is.False);
        Assert.That(pending.Status, Is.EqualTo(XmlParserStatus.NeedInput));
        Assert.That(parser.PauseXmlPush(), Is.EqualTo(XmlParserStatus.Paused));
        Assert.That(parser.ResumeXmlPush(), Is.EqualTo(XmlParserStatus.Running));

        XmlEventBatch completed = parser.FeedXmlPushCharacters("</root>".AsMemory(), true);

        Assert.That(completed.Status, Is.EqualTo(XmlParserStatus.Completed));
        Assert.That(completed.IsFinal, Is.True);
        Assert.That(completed.Count, Is.EqualTo(5));
        Assert.That(completed.Events![0].Kind, Is.EqualTo(XmlEventKind.StartDocument));
        Assert.That(completed.Events[1].Kind, Is.EqualTo(XmlEventKind.StartElement));
        Assert.That(completed.Events[2].Kind, Is.EqualTo(XmlEventKind.Text));
        Assert.That(completed.Events[3].Kind, Is.EqualTo(XmlEventKind.EndElement));
        Assert.That(completed.Events[4].Kind, Is.EqualTo(XmlEventKind.EndDocument));
        Assert.That(parser.Driver.ParsedDocument!.DocumentElement!.Name!.LocalName, Is.EqualTo("root"));
        Assert.That(parser.Driver.ParsedDocument.DocumentElement.Children![0].Value, Is.EqualTo("value"));
    }

    [Test]
    public void ByteBlocksMaySplitOneUtf8ScalarAcrossFeeds()
    {
        XmlPushParser parser = new XmlPushParser();
        byte[] bytes = Encoding.UTF8.GetBytes("<root>\u00e9</root>");
        int scalarStart = Array.IndexOf(bytes, (byte)0xc3);

        parser.BeginXmlPush(ByteInput([]), DefaultOptions(), null, null, null, null);
        XmlEventBatch pending = parser.FeedXmlPushBytes(bytes.AsMemory(0, scalarStart + 1), false);
        XmlEventBatch completed = parser.FeedXmlPushBytes(bytes.AsMemory(scalarStart + 1), true);

        Assert.That(pending.Status, Is.EqualTo(XmlParserStatus.NeedInput));
        Assert.That(completed.Status, Is.EqualTo(XmlParserStatus.Completed));
        Assert.That(parser.Driver.ParsedDocument!.DocumentElement!.Children![0].Value, Is.EqualTo("\u00e9"));
    }

    [Test]
    public void FinalInputHonorsHandlerPauseAndResumesWithoutReparsing()
    {
        BackpressureHandler handler = new BackpressureHandler(XmlHandlerAction.Pause);
        XmlPushParser parser = new XmlPushParser();
        parser.BeginXmlPush(CharacterInput(""), DefaultOptions(), handler, null, null, null);

        XmlEventBatch paused = parser.FeedXmlPushCharacters("<root/>".AsMemory(), true);

        Assert.That(paused.Status, Is.EqualTo(XmlParserStatus.Paused));
        Assert.That(paused.IsFinal, Is.False);
        Assert.That(paused.Count, Is.EqualTo(1));
        Assert.That(paused.Events![0].Kind, Is.EqualTo(XmlEventKind.StartDocument));
        Assert.That(handler.Kinds, Is.EqualTo(new[] { XmlEventKind.StartDocument }));

        Assert.That(parser.ResumeXmlPush(), Is.EqualTo(XmlParserStatus.Running));
        XmlDocument parsedBeforeFinish = parser.Driver.ParsedDocument!;
        XmlEventBatch completed = parser.FinishXmlPush();

        Assert.That(completed.Status, Is.EqualTo(XmlParserStatus.Completed));
        Assert.That(completed.IsFinal, Is.True);
        Assert.That(completed.Count, Is.EqualTo(3));
        Assert.That(completed.Events![0].Kind, Is.EqualTo(XmlEventKind.StartElement));
        Assert.That(completed.Events[1].Kind, Is.EqualTo(XmlEventKind.EndElement));
        Assert.That(completed.Events[2].Kind, Is.EqualTo(XmlEventKind.EndDocument));
        Assert.That(handler.Kinds, Is.EqualTo(new[] { XmlEventKind.StartDocument, XmlEventKind.StartElement, XmlEventKind.EndElement, XmlEventKind.EndDocument }));
        Assert.That(parser.Driver.ParsedDocument, Is.SameAs(parsedBeforeFinish));
    }

    [Test]
    public void HandlerMayAbortFinalEventDispatch()
    {
        BackpressureHandler handler = new BackpressureHandler(XmlHandlerAction.Abort);
        XmlPushParser parser = new XmlPushParser();
        parser.BeginXmlPush(CharacterInput(""), DefaultOptions(), handler, null, null, null);

        XmlEventBatch aborted = parser.FeedXmlPushCharacters("<root/>".AsMemory(), true);

        Assert.That(aborted.Status, Is.EqualTo(XmlParserStatus.Aborted));
        Assert.That(aborted.IsFinal, Is.True);
        Assert.That(aborted.Count, Is.EqualTo(1));
        Assert.That(parser.InspectXmlPushStatus(), Is.EqualTo(XmlParserStatus.Aborted));
        Assert.Throws<InvalidOperationException>(() => parser.FinishXmlPush());
    }

    [Test]
    public void AbortResetAndCloseEnforceLifecycleTransitions()
    {
        XmlPushParser parser = new XmlPushParser();
        parser.BeginXmlPush(CharacterInput("<root"), DefaultOptions(), null, null, null, null);
        parser.FeedXmlPushCharacters("".AsMemory(), false);

        Assert.That(parser.AbortXmlPush(), Is.EqualTo(XmlParserStatus.Aborted));
        Assert.Throws<InvalidOperationException>(() => parser.FeedXmlPushCharacters("/>".AsMemory(), true));
        parser.ResetXmlPush();
        Assert.That(parser.InspectXmlPushStatus(), Is.EqualTo(XmlParserStatus.Ready));

        parser.BeginXmlPush(CharacterInput("<root/>"), DefaultOptions(), null, null, null, null);
        Assert.That(parser.FinishXmlPush().Status, Is.EqualTo(XmlParserStatus.Completed));
        parser.CloseXmlPush();
        parser.CloseXmlPush();
        Assert.That(parser.InspectXmlPushStatus(), Is.EqualTo(XmlParserStatus.Closed));
        Assert.Throws<InvalidOperationException>(() => parser.ResetXmlPush());
        Assert.Throws<InvalidOperationException>(() => parser.BeginXmlPush(CharacterInput("<root/>"), DefaultOptions(), null, null, null, null));
    }

    [Test]
    public void InputModesCannotBeMixedAndMalformedFinalInputEntersErrorState()
    {
        XmlPushParser parser = new XmlPushParser();
        parser.BeginXmlPush(CharacterInput(""), DefaultOptions(), null, null, null, null);

        Assert.Throws<InvalidOperationException>(() => parser.FeedXmlPushBytes(new byte[] { 1 }, false));
        Assert.That(parser.InspectXmlPushStatus(), Is.EqualTo(XmlParserStatus.Running));
        Assert.Throws<XmlWellFormednessException>(() => parser.FeedXmlPushCharacters("<root>".AsMemory(), true));
        Assert.That(parser.InspectXmlPushStatus(), Is.EqualTo(XmlParserStatus.Error));

        parser.ResetXmlPush();
        parser.BeginXmlPush(CharacterInput("<root/>"), DefaultOptions(), null, null, null, null);
        Assert.That(parser.FinishXmlPush().Status, Is.EqualTo(XmlParserStatus.Completed));
    }

    [Test]
    public void IncrementalFeedsEnforceCumulativeInputLimits()
    {
        XmlParserOptions options = DefaultOptions();
        options.SecurityLimits = SecurityLimits(6);
        XmlPushParser parser = new XmlPushParser();
        parser.BeginXmlPush(CharacterInput("<ro"), options, null, null, null, null);

        XmlLimitException failure = Assert.Throws<XmlLimitException>(() => parser.FeedXmlPushCharacters("ot/>".AsMemory(), false))!;

        Assert.That(failure.Message, Does.Contain("total input characters"));
        Assert.That(parser.InspectXmlPushStatus(), Is.EqualTo(XmlParserStatus.Error));
    }

    [Test]
    public void OwnedFixedStreamsAreReleasedOnCompletionAndAbort()
    {
        MemoryStream completedStream = new MemoryStream(Encoding.UTF8.GetBytes("<root/>"));
        XmlPushParser completedParser = new XmlPushParser();
        completedParser.BeginXmlPush(StreamInput(completedStream), DefaultOptions(), null, null, null, null);

        Assert.That(completedParser.FinishXmlPush().Status, Is.EqualTo(XmlParserStatus.Completed));
        Assert.That(completedStream.CanRead, Is.False);

        MemoryStream abortedStream = new MemoryStream(Encoding.UTF8.GetBytes("<root/>"));
        XmlPushParser abortedParser = new XmlPushParser();
        abortedParser.BeginXmlPush(StreamInput(abortedStream), DefaultOptions(), null, null, null, null);

        Assert.That(abortedParser.AbortXmlPush(), Is.EqualTo(XmlParserStatus.Aborted));
        Assert.That(abortedStream.CanRead, Is.False);
    }

    [Test]
    public void FragmentFinalizationProducesNoDocumentBoundaryEvents()
    {
        XmlParserOptions options = DefaultOptions();
        options.ParseFragment = true;
        XmlPushParser parser = new XmlPushParser();
        parser.BeginXmlPush(CharacterInput("<a/>"), options, null, null, null, null);

        XmlEventBatch completed = parser.FeedXmlPushCharacters("<b/>".AsMemory(), true);

        Assert.That(completed.Status, Is.EqualTo(XmlParserStatus.Completed));
        Assert.That(completed.Count, Is.EqualTo(4));
        Assert.That(completed.IsFinal, Is.True);
        Assert.That(completed.Events!.Select(item => item.Kind), Is.EqualTo(new[]
        {
            XmlEventKind.StartElement,
            XmlEventKind.EndElement,
            XmlEventKind.StartElement,
            XmlEventKind.EndElement
        }));
        Assert.That(parser.Driver.ParsedDocument!.DocumentNode!.Kind, Is.EqualTo(XmlNodeKind.DocumentFragment));
        Assert.That(parser.Driver.ParsedDocument.DocumentNode.Children, Has.Length.EqualTo(2));
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

    private static XmlSecurityLimits SecurityLimits(long maximumInputCharacters)
    {
        return new XmlSecurityLimits
        {
            MaximumDepth = 32,
            MaximumNodeCount = 1000,
            MaximumAttributesPerElement = 100,
            MaximumNameLength = 100,
            MaximumTextLength = 1000,
            MaximumTokenLength = 1000,
            MaximumInputCharacters = maximumInputCharacters,
            MaximumInputBytes = 1000,
            MaximumDtdCharacters = 1000,
            MaximumEntityDepth = 10,
            MaximumEntityCount = 100,
            MaximumEntityCharacters = 1000,
            MaximumEntityAmplification = 10,
            MaximumExternalResources = 10,
            MaximumResourceDepth = 10,
            MaximumResourceBytes = 1000,
            CancellationPollingInterval = 1
        };
    }

    private static XmlInput CharacterInput(string text)
    {
        return new XmlInput { Kind = XmlInputKind.CharacterMemory, Characters = text.AsMemory() };
    }

    private static XmlInput ByteInput(byte[] bytes)
    {
        return new XmlInput { Kind = XmlInputKind.ByteMemory, Bytes = bytes };
    }

    private static XmlInput StreamInput(Stream stream)
    {
        return new XmlInput { Kind = XmlInputKind.Stream, ByteStream = stream, OwnsResource = true };
    }

    private sealed class BackpressureHandler : IXmlEventHandler
    {
        private readonly XmlHandlerAction firstAction;
        private int callCount;

        internal List<XmlEventKind> Kinds = new List<XmlEventKind>();

        internal BackpressureHandler(XmlHandlerAction firstAction)
        {
            this.firstAction = firstAction;
        }

        public XmlHandlerAction HandleXmlEvent(XmlEvent value)
        {
            Kinds.Add(value.Kind);
            callCount = callCount + 1;
            return callCount == 1 ? firstAction : XmlHandlerAction.Continue;
        }
    }
}
