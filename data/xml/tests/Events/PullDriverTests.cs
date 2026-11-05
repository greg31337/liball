using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class PullDriverTests
{
    [Test]
    public void AdvancesThroughNormalizedEventsAndCompletesAtEndDocument()
    {
        XmlPullReader reader = new XmlPullReader();
        XmlParserOptions options = Options();
        options.PreserveSourceLocations = true;
        XmlInput input = StringInput("<root a='value'>text<child/></root>");
        input.BaseUri = "urn:pull-document";

        XmlReaderState begun = reader.BeginXmlPull(input, options, null, null, null);

        Assert.That(begun.Status, Is.EqualTo(XmlParserStatus.Running));
        Assert.That(begun.Options, Is.SameAs(options));
        Assert.That(begun.BaseUri, Is.EqualTo("urn:pull-document"));
        Assert.That(reader.InspectXmlPull(), Is.SameAs(begun));

        List<XmlReaderState> states = ReadToCompletion(reader);

        Assert.That(states.ConvertAll(item => item.Kind), Is.EqualTo(new[]
        {
            XmlEventKind.StartDocument,
            XmlEventKind.StartElement,
            XmlEventKind.Text,
            XmlEventKind.StartElement,
            XmlEventKind.EndElement,
            XmlEventKind.EndElement,
            XmlEventKind.EndDocument
        }));
        Assert.That(states[1].Name!.QualifiedName, Is.EqualTo("root"));
        Assert.That(states[1].Attributes, Has.Length.EqualTo(1));
        Assert.That(states[1].Attributes![0].Value, Is.EqualTo("value"));
        Assert.That(states[1].Range!.Start!.CharacterOffset, Is.Zero);
        Assert.That(states[2].Value, Is.EqualTo("text"));
        Assert.That(states[3].Depth, Is.EqualTo(1));
        Assert.That(states[3].IsEmptyElement, Is.True);
        Assert.That(states[^1].Status, Is.EqualTo(XmlParserStatus.Completed));
        Assert.That(reader.AdvanceXmlPull(), Is.SameAs(states[^1]));
        Assert.Throws<InvalidOperationException>(() => reader.BeginXmlPull(input, options, null, null, null));
    }

    [Test]
    public void EmptyFragmentAdvancesDirectlyToCompletedEofState()
    {
        XmlPullReader reader = new XmlPullReader();
        XmlParserOptions options = Options();
        options.ParseFragment = true;
        reader.BeginXmlPull(StringInput(""), options, null, null, null);

        XmlReaderState completed = reader.AdvanceXmlPull();

        Assert.That(completed.Status, Is.EqualTo(XmlParserStatus.Completed));
        Assert.That(completed.Name, Is.Null);
        Assert.That(completed.Value, Is.Null);
        Assert.That(completed.Options, Is.SameAs(options));
        Assert.That(reader.AdvanceXmlPull(), Is.SameAs(completed));
    }

    [Test]
    public void SkipsNestedSubtreeAndItsNamespaceEndEvents()
    {
        XmlPullReader reader = new XmlPullReader();
        XmlParserOptions options = Options();
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        options.PreservePrefixes = true;
        reader.BeginXmlPull(StringInput("<root><skip xmlns:p='urn:p'><p:inner>hidden</p:inner></skip><after/></root>"), options, null, null, null);

        XmlReaderState state = reader.AdvanceXmlPull();
        Assert.Throws<InvalidOperationException>(() => reader.SkipXmlPullSubtree());
        while (state.Kind != XmlEventKind.StartElement || state.Name!.LocalName != "skip")
        {
            state = reader.AdvanceXmlPull();
        }

        XmlReaderState after = reader.SkipXmlPullSubtree();

        Assert.That(after.Kind, Is.EqualTo(XmlEventKind.StartElement));
        Assert.That(after.Name!.LocalName, Is.EqualTo("after"));
        Assert.That(after.Depth, Is.EqualTo(1));
        Assert.That(after.NamespaceBindings, Is.Empty);
        List<XmlReaderState> remainder = ReadToCompletion(reader);
        Assert.That(remainder.Exists(item => item.Value == "hidden"), Is.False);
    }

    [Test]
    public void ExposesAttributesNamespacesAndInheritedXmlContext()
    {
        string text = "<root xmlns:p='urn:p' xml:base='parts/' xml:lang='en' xml:space='preserve'><p:child xml:lang='fr'><leaf xml:space='default'/></p:child><tail/></root>";
        XmlInput input = StringInput(text);
        input.BaseUri = "https://example.test/base/document.xml";
        XmlParserOptions options = Options();
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        options.PreservePrefixes = true;
        XmlPullReader reader = new XmlPullReader();
        reader.BeginXmlPull(input, options, null, null, null);

        List<XmlReaderState> states = ReadToCompletion(reader);
        XmlReaderState root = FindStart(states, "root");
        XmlReaderState child = FindStart(states, "child");
        XmlReaderState leaf = FindStart(states, "leaf");
        XmlReaderState tail = FindStart(states, "tail");

        Assert.That(root.Attributes, Has.Length.EqualTo(3));
        Assert.That(root.NamespaceBindings, Has.Length.EqualTo(1));
        Assert.That(root.NamespaceBindings![0].Prefix, Is.EqualTo("p"));
        Assert.That(root.NamespaceBindings[0].NamespaceUri, Is.EqualTo("urn:p"));
        Assert.That(root.BaseUri, Is.EqualTo("https://example.test/base/parts/"));
        Assert.That(root.Language, Is.EqualTo("en"));
        Assert.That(root.SpaceMode, Is.EqualTo(XmlSpaceMode.Preserve));
        Assert.That(child.Name!.NamespaceUri, Is.EqualTo("urn:p"));
        Assert.That(child.BaseUri, Is.EqualTo(root.BaseUri));
        Assert.That(child.Language, Is.EqualTo("fr"));
        Assert.That(child.SpaceMode, Is.EqualTo(XmlSpaceMode.Preserve));
        Assert.That(leaf.Language, Is.EqualTo("fr"));
        Assert.That(leaf.SpaceMode, Is.EqualTo(XmlSpaceMode.Default));
        Assert.That(tail.Language, Is.EqualTo("en"));
        Assert.That(tail.SpaceMode, Is.EqualTo(XmlSpaceMode.Preserve));
    }

    [Test]
    public void ExposesDiagnosticEventsThroughCurrentState()
    {
        XmlParserOptions options = Options();
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.ValidationMode = XmlValidationMode.Dtd;
        options.AccumulateValidationDiagnostics = true;
        options.PreserveIgnorableWhitespace = true;
        XmlPullReader reader = new XmlPullReader();
        reader.BeginXmlPull(StringInput("<!DOCTYPE root [<!ELEMENT root (child)><!ELEMENT child EMPTY>]><root extra='value'>  <child/></root>"), options, null, null, null);

        List<XmlReaderState> states = ReadToCompletion(reader);
        XmlReaderState diagnostic = states.Find(item => item.Kind == XmlEventKind.Diagnostic)!;
        XmlReaderState declaration = states.Find(item => item.Kind == XmlEventKind.ElementDeclaration)!;
        XmlReaderState whitespace = states.Find(item => item.Kind == XmlEventKind.IgnorableWhitespace)!;

        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.DtdValidity));
        Assert.That(diagnostic.Diagnostic.Message, Does.Contain("DTD declaration"));
        Assert.That(declaration.ElementDeclaration!.Name!.QualifiedName, Is.EqualTo("root"));
        Assert.That(whitespace.IsIgnorableWhitespace, Is.True);
    }

    [Test]
    public void ParseFailureLeavesInspectableErrorState()
    {
        XmlPullReader reader = new XmlPullReader();
        reader.BeginXmlPull(StringInput("<root>"), Options(), null, null, null);

        Assert.Throws<XmlWellFormednessException>(() => reader.AdvanceXmlPull());

        Assert.That(reader.InspectXmlPull().Status, Is.EqualTo(XmlParserStatus.Error));
        Assert.Throws<InvalidOperationException>(() => reader.AdvanceXmlPull());
        Assert.Throws<InvalidOperationException>(() => reader.SkipXmlPullSubtree());
        reader.CloseXmlPull();
        Assert.That(reader.InspectXmlPull().Status, Is.EqualTo(XmlParserStatus.Closed));
    }

    [Test]
    public void CloseIsIdempotentAndReleasesOwnedInputBeforeOrAfterParsing()
    {
        MemoryStream unopenedStream = new MemoryStream(Encoding.UTF8.GetBytes("<root/>"));
        XmlPullReader unopened = new XmlPullReader();
        unopened.BeginXmlPull(StreamInput(unopenedStream), Options(), null, null, null);

        unopened.CloseXmlPull();
        unopened.CloseXmlPull();

        Assert.That(unopenedStream.CanRead, Is.False);
        Assert.That(unopened.InspectXmlPull().Status, Is.EqualTo(XmlParserStatus.Closed));
        Assert.Throws<InvalidOperationException>(() => unopened.AdvanceXmlPull());
        Assert.Throws<InvalidOperationException>(() => unopened.BeginXmlPull(StringInput("<root/>"), Options(), null, null, null));

        MemoryStream parsedStream = new MemoryStream(Encoding.UTF8.GetBytes("<root/>"));
        XmlPullReader parsed = new XmlPullReader();
        parsed.BeginXmlPull(StreamInput(parsedStream), Options(), null, null, null);
        parsed.AdvanceXmlPull();

        Assert.That(parsedStream.CanRead, Is.False);
        parsed.CloseXmlPull();
        Assert.That(parsed.InspectXmlPull().Status, Is.EqualTo(XmlParserStatus.Closed));
    }

    private static List<XmlReaderState> ReadToCompletion(XmlPullReader reader)
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

    private static XmlReaderState FindStart(List<XmlReaderState> states, string localName)
    {
        XmlReaderState? found = states.Find(item => item.Kind == XmlEventKind.StartElement && item.Name != null && item.Name.LocalName == localName);
        Assert.That(found, Is.Not.Null);
        return found!;
    }

    private static XmlInput StringInput(string text)
    {
        XmlInput input = new XmlInput();
        input.Text = text;
        return input;
    }

    private static XmlInput StreamInput(Stream stream)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.Stream;
        input.ByteStream = stream;
        input.OwnsResource = true;
        return input;
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
