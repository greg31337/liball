using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class PushPullApiCoverageTests
{
    [Test]
    public void PullApiExposesEveryEventKindAndReaderStateField()
    {
        string text = "<?pre data?><!DOCTYPE root [<!ELEMENT root (child)><!ATTLIST root state CDATA 'ready'><!ENTITY ent 'entity'><!NOTATION note PUBLIC 'note-public' 'urn:note'><!ELEMENT child (#PCDATA|leaf)*><!ELEMENT leaf EMPTY>]><root xmlns:p='urn:p' xml:base='parts/' xml:lang='en' xml:space='preserve' p:a='v'>  <child>text<![CDATA[cdata]]><!--comment--><?inside value?>&ent;<leaf/></child></root><?post done?>";
        XmlParserOptions options = FullEventOptions();
        XmlInput input = StringInput(text);
        input.BaseUri = "https://example.test/api/document.xml";
        List<XmlReaderState> states = ReadPull(input, options);

        XmlReaderState root = FindState(states, XmlEventKind.StartElement, "root");
        XmlReaderState child = FindState(states, XmlEventKind.StartElement, "child");
        XmlReaderState leaf = FindState(states, XmlEventKind.StartElement, "leaf");
        XmlReaderState whitespace = FindState(states, XmlEventKind.IgnorableWhitespace, null);
        XmlReaderState diagnostic = FindState(states, XmlEventKind.Diagnostic, null);
        XmlReaderState elementDeclaration = FindState(states, XmlEventKind.ElementDeclaration, null);
        XmlReaderState attributeDeclaration = FindState(states, XmlEventKind.AttributeDeclaration, null);
        XmlReaderState entityDeclaration = FindState(states, XmlEventKind.EntityDeclaration, null);
        XmlReaderState notationDeclaration = FindState(states, XmlEventKind.NotationDeclaration, null);

        Assert.That(states[0].Status, Is.EqualTo(XmlParserStatus.Running));
        Assert.That(states[^1].Status, Is.EqualTo(XmlParserStatus.Completed));
        Assert.That(root.Kind, Is.EqualTo(XmlEventKind.StartElement));
        Assert.That(root.NodeKind, Is.EqualTo(XmlNodeKind.Element));
        Assert.That(root.Name!.QualifiedName, Is.EqualTo("root"));
        Assert.That(FindState(states, XmlEventKind.Text, null).Value, Is.EqualTo("text"));
        Assert.That(root.Attributes, Has.Length.EqualTo(5));
        Assert.That(root.NamespaceBindings, Has.Length.EqualTo(1));
        Assert.That(child.Depth, Is.EqualTo(1));
        Assert.That(root.BaseUri, Is.EqualTo("https://example.test/api/parts/"));
        Assert.That(root.Language, Is.EqualTo("en"));
        Assert.That(root.SpaceMode, Is.EqualTo(XmlSpaceMode.Preserve));
        Assert.That(leaf.IsEmptyElement, Is.True);
        Assert.That(whitespace.IsIgnorableWhitespace, Is.True);
        Assert.That(root.Range!.Start!.CharacterOffset, Is.EqualTo(text.IndexOf("<root", StringComparison.Ordinal)));
        Assert.That(diagnostic.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.DtdValidity));
        Assert.That(elementDeclaration.ElementDeclaration!.Name!.QualifiedName, Is.EqualTo("root"));
        Assert.That(attributeDeclaration.AttributeDeclaration!.Name!.QualifiedName, Is.EqualTo("state"));
        Assert.That(entityDeclaration.EntityDeclaration!.Name, Is.EqualTo("ent"));
        Assert.That(notationDeclaration.NotationDeclaration!.Name, Is.EqualTo("note"));
        Assert.That(notationDeclaration.PublicIdentifier, Is.EqualTo("note-public"));
        Assert.That(notationDeclaration.SystemIdentifier, Is.EqualTo("urn:note"));
        Assert.That(root.Options, Is.SameAs(options));

        XmlParserOptions skippedOptions = BasicOptions();
        skippedOptions.DtdProcessing = XmlDtdProcessing.Ignore;
        List<XmlReaderState> skippedStates = ReadPull(StringInput("<!DOCTYPE root SYSTEM 'missing.dtd'><root>&missing;</root>"), skippedOptions);
        HashSet<XmlEventKind> observed = new HashSet<XmlEventKind>();
        AddKinds(observed, states);
        AddKinds(observed, skippedStates);
        XmlEventKind[] allKinds = Enum.GetValues<XmlEventKind>();
        int kindIndex = 0;
        while (kindIndex < allKinds.Length)
        {
            Assert.That(observed, Does.Contain(allKinds[kindIndex]), $"Missing pull event kind {allKinds[kindIndex]}");
            kindIndex = kindIndex + 1;
        }
    }

    [TestCase(XmlHandlerAction.Continue, XmlParserStatus.Completed)]
    [TestCase(XmlHandlerAction.Pause, XmlParserStatus.Paused)]
    [TestCase(XmlHandlerAction.Abort, XmlParserStatus.Aborted)]
    public void PushHandlerActionsProduceTheirDocumentedTransitions(XmlHandlerAction action, XmlParserStatus expectedStatus)
    {
        FirstActionHandler handler = new FirstActionHandler(action);
        XmlPushParser parser = new XmlPushParser();
        parser.BeginXmlPush(StringInput("<root/>"), BasicOptions(), handler, null, null, null);

        XmlEventBatch first = parser.FinishXmlPush();

        Assert.That(first.Status, Is.EqualTo(expectedStatus));
        Assert.That(parser.InspectXmlPushStatus(), Is.EqualTo(expectedStatus));
        Assert.That(handler.Events[0].Kind, Is.EqualTo(XmlEventKind.StartDocument));
        if (action == XmlHandlerAction.Pause)
        {
            Assert.That(parser.ResumeXmlPush(), Is.EqualTo(XmlParserStatus.Running));
            XmlEventBatch resumed = parser.FinishXmlPush();
            Assert.That(resumed.Status, Is.EqualTo(XmlParserStatus.Completed));
            Assert.That(handler.Events, Has.Count.EqualTo(4));
        }
        else if (action == XmlHandlerAction.Continue)
        {
            Assert.That(handler.Events, Has.Count.EqualTo(4));
        }
        else
        {
            Assert.That(handler.Events, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void PushAndPullExposeExactUtf8SourceLocations()
    {
        string text = "<root>é</root>";
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        XmlParserOptions options = BasicOptions();
        options.PreserveSourceLocations = true;
        XmlInput pullInput = ByteInput(bytes, false);
        List<XmlReaderState> pullStates = ReadPull(pullInput, options);
        XmlReaderState pullText = FindState(pullStates, XmlEventKind.Text, null);

        XmlPushParser parser = new XmlPushParser();
        parser.BeginXmlPush(ByteInput([], false), options, null, null, null, null);
        XmlEventBatch pushBatch = parser.FeedXmlPushBytes(bytes.AsMemory(), true);
        XmlEvent pushText = Array.Find(pushBatch.Events!, item => item.Kind == XmlEventKind.Text)!;

        AssertLocation(pullText.Range!, 6, 6, 7, 8);
        AssertLocation(pushText.Range!, 6, 6, 7, 8);
    }

    [Test]
    public void PullLimitFailureEntersErrorAndReleasesOwnedInput()
    {
        MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("<root/>"));
        XmlParserOptions options = BasicOptions();
        options.SecurityLimits = SecurityLimits(4);
        XmlPullReader reader = new XmlPullReader();
        reader.BeginXmlPull(ByteInput(stream, true), options, null, null, null);

        XmlLimitException failure = Assert.Throws<XmlLimitException>(() => reader.AdvanceXmlPull())!;

        Assert.That(failure.Message, Does.Contain("total input bytes"));
        Assert.That(reader.InspectXmlPull().Status, Is.EqualTo(XmlParserStatus.Error));
        Assert.That(stream.CanRead, Is.False);
        reader.CloseXmlPull();
    }

    [Test]
    public void PushAndPullRespectStreamAndTextReaderOwnership()
    {
        MemoryStream pushUnowned = new MemoryStream(Encoding.UTF8.GetBytes("<root/>"));
        XmlPushParser push = new XmlPushParser();
        push.BeginXmlPush(ByteInput(pushUnowned, false), BasicOptions(), null, null, null, null);
        Assert.That(push.FinishXmlPush().Status, Is.EqualTo(XmlParserStatus.Completed));
        push.CloseXmlPush();
        Assert.That(pushUnowned.CanRead, Is.True);

        StringReader pushOwned = new StringReader("<root/>");
        push = new XmlPushParser();
        push.BeginXmlPush(TextReaderInput(pushOwned, true), BasicOptions(), null, null, null, null);
        Assert.That(push.FinishXmlPush().Status, Is.EqualTo(XmlParserStatus.Completed));
        Assert.Throws<ObjectDisposedException>(() => pushOwned.Read());

        MemoryStream pullUnowned = new MemoryStream(Encoding.UTF8.GetBytes("<root/>"));
        XmlPullReader pull = new XmlPullReader();
        pull.BeginXmlPull(ByteInput(pullUnowned, false), BasicOptions(), null, null, null);
        ReadPullToCompletion(pull);
        pull.CloseXmlPull();
        Assert.That(pullUnowned.CanRead, Is.True);

        StringReader pullOwned = new StringReader("<root/>");
        pull = new XmlPullReader();
        pull.BeginXmlPull(TextReaderInput(pullOwned, true), BasicOptions(), null, null, null);
        ReadPullToCompletion(pull);
        Assert.Throws<ObjectDisposedException>(() => pullOwned.Read());
    }

    [Test]
    public void PullSkipHandlesEmptyElementAndRejectsTerminalState()
    {
        XmlPullReader reader = new XmlPullReader();
        reader.BeginXmlPull(StringInput("<root><empty/></root>"), BasicOptions(), null, null, null);
        XmlReaderState state = reader.AdvanceXmlPull();
        while (state.Kind != XmlEventKind.StartElement || state.Name!.LocalName != "empty")
        {
            state = reader.AdvanceXmlPull();
        }

        XmlReaderState after = reader.SkipXmlPullSubtree();
        Assert.That(after.Kind, Is.EqualTo(XmlEventKind.EndElement));
        Assert.That(after.Name!.LocalName, Is.EqualTo("root"));
        Assert.That(reader.AdvanceXmlPull().Status, Is.EqualTo(XmlParserStatus.Completed));
        Assert.Throws<InvalidOperationException>(() => reader.SkipXmlPullSubtree());
    }

    private static List<XmlReaderState> ReadPull(XmlInput input, XmlParserOptions options)
    {
        XmlPullReader reader = new XmlPullReader();
        reader.BeginXmlPull(input, options, null, null, null);
        return ReadPullToCompletion(reader);
    }

    private static List<XmlReaderState> ReadPullToCompletion(XmlPullReader reader)
    {
        List<XmlReaderState> states = new List<XmlReaderState>();
        XmlReaderState state = reader.AdvanceXmlPull();
        states.Add(state);
        while (state.Status != XmlParserStatus.Completed)
        {
            state = reader.AdvanceXmlPull();
            states.Add(state);
        }

        return states;
    }

    private static XmlReaderState FindState(List<XmlReaderState> states, XmlEventKind kind, string? localName)
    {
        XmlReaderState? found = states.Find(item => item.Kind == kind && (localName == null || item.Name != null && item.Name.LocalName == localName));
        Assert.That(found, Is.Not.Null, $"Missing state for {kind} {localName}");
        return found!;
    }

    private static void AddKinds(HashSet<XmlEventKind> target, List<XmlReaderState> states)
    {
        int index = 0;
        while (index < states.Count)
        {
            target.Add(states[index].Kind);
            index = index + 1;
        }
    }

    private static void AssertLocation(SourceRange range, long startCharacter, long startByte, long endCharacter, long endByte)
    {
        Assert.That(range.Start!.CharacterOffset, Is.EqualTo(startCharacter));
        Assert.That(range.Start.ByteOffset, Is.EqualTo(startByte));
        Assert.That(range.End!.CharacterOffset, Is.EqualTo(endCharacter));
        Assert.That(range.End.ByteOffset, Is.EqualTo(endByte));
    }

    private static XmlInput StringInput(string text)
    {
        return new XmlInput { Kind = XmlInputKind.String, Text = text };
    }

    private static XmlInput ByteInput(byte[] bytes, bool ownsResource)
    {
        return new XmlInput { Kind = XmlInputKind.ByteMemory, Bytes = bytes, OwnsResource = ownsResource };
    }

    private static XmlInput ByteInput(Stream stream, bool ownsResource)
    {
        return new XmlInput { Kind = XmlInputKind.Stream, ByteStream = stream, OwnsResource = ownsResource };
    }

    private static XmlInput TextReaderInput(TextReader reader, bool ownsResource)
    {
        return new XmlInput { Kind = XmlInputKind.TextReader, TextReader = reader, OwnsResource = ownsResource };
    }

    private static XmlParserOptions FullEventOptions()
    {
        XmlParserOptions options = BasicOptions();
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.ValidationMode = XmlValidationMode.Dtd;
        options.AccumulateValidationDiagnostics = true;
        options.PreserveCData = true;
        options.PreserveComments = true;
        options.PreserveProcessingInstructions = true;
        options.PreserveEntityReferences = true;
        options.PreserveIgnorableWhitespace = true;
        options.PreservePrefixes = true;
        options.PreserveSourceLocations = true;
        return options;
    }

    private static XmlParserOptions BasicOptions()
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

    private static XmlSecurityLimits SecurityLimits(long maximumInputBytes)
    {
        return new XmlSecurityLimits
        {
            MaximumDepth = 32,
            MaximumNodeCount = 1000,
            MaximumAttributesPerElement = 100,
            MaximumNameLength = 100,
            MaximumTextLength = 1000,
            MaximumTokenLength = 1000,
            MaximumInputCharacters = 1000,
            MaximumInputBytes = maximumInputBytes,
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

    private sealed class FirstActionHandler : IXmlEventHandler
    {
        private readonly XmlHandlerAction firstAction;
        private int callCount;

        internal List<XmlEvent> Events = new List<XmlEvent>();

        internal FirstActionHandler(XmlHandlerAction firstAction)
        {
            this.firstAction = firstAction;
        }

        public XmlHandlerAction HandleXmlEvent(XmlEvent value)
        {
            Events.Add(value);
            callCount = callCount + 1;
            return callCount == 1 ? firstAction : XmlHandlerAction.Continue;
        }
    }
}
