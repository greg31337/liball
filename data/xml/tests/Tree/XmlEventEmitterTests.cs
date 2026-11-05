using System;
using System.Linq;
using xml;

namespace xml.Tests;

[TestFixture]
public class XmlEventEmitterTests
{
    [Test]
    public void EmitsCompleteDocumentStreamThatRebuildsTheRetainedTree()
    {
        XmlParserOptions options = PreservationOptions();
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.PreserveEntityReferences = true;
        string text = "<?before data?><!DOCTYPE root [<!ELEMENT root ANY><!ATTLIST root a CDATA #IMPLIED><!ENTITY ent 'entity'><!NOTATION note SYSTEM 'urn:note'>]><root xmlns:p='urn:p' a='v'>text<![CDATA[cdata]]>&ent;<!--comment--><?inside data?><p:leaf/></root>";
        XmlDocument source = ParseDocument(text, options);

        XmlEventBatch batch = new XmlEventEmitter().EmitXmlNodeEvents(source.DocumentNode!, options);
        XmlEvent[] events = batch.Events!;

        Assert.That(batch.Offset, Is.Zero);
        Assert.That(batch.Count, Is.EqualTo(events.Length));
        Assert.That(batch.Status, Is.EqualTo(XmlParserStatus.Completed));
        Assert.That(batch.IsFinal, Is.True);
        Assert.That(batch.ReaderState, Is.Null);
        Assert.That(events[0].Kind, Is.EqualTo(XmlEventKind.StartDocument));
        Assert.That(events[^1].Kind, Is.EqualTo(XmlEventKind.EndDocument));
        Assert.That(events.Select(item => item.Kind), Does.Contain(XmlEventKind.StartDtd));
        Assert.That(events.Select(item => item.Kind), Does.Contain(XmlEventKind.ElementDeclaration));
        Assert.That(events.Select(item => item.Kind), Does.Contain(XmlEventKind.AttributeDeclaration));
        Assert.That(events.Select(item => item.Kind), Does.Contain(XmlEventKind.EntityDeclaration));
        Assert.That(events.Select(item => item.Kind), Does.Contain(XmlEventKind.NotationDeclaration));
        Assert.That(events.Select(item => item.Kind), Does.Contain(XmlEventKind.StartNamespace));
        Assert.That(events.Select(item => item.Kind), Does.Contain(XmlEventKind.CData));
        Assert.That(events.Select(item => item.Kind), Does.Contain(XmlEventKind.StartEntity));
        Assert.That(events.Select(item => item.Kind), Does.Contain(XmlEventKind.Comment));

        XmlDocument rebuilt = new TreeBuilder(source, false).BuildXmlEventTree(events);
        XmlNode sourceRoot = source.DocumentElement!;
        XmlNode rebuiltRoot = rebuilt.DocumentElement!;
        XmlNode[] rebuiltChildren = rebuiltRoot.Children!;
        XmlDocumentType rebuiltDocumentType = rebuilt.DocumentType!;

        Assert.That(rebuilt.DocumentNode!.Children!.Select(item => item.Kind), Is.EqualTo(source.DocumentNode!.Children!.Select(item => item.Kind)));
        Assert.That(rebuiltRoot.Name!.QualifiedName, Is.EqualTo("root"));
        Assert.That(rebuiltRoot.Attributes![0].Value, Is.EqualTo("v"));
        Assert.That(rebuiltRoot.NamespaceBindings![0].NamespaceUri, Is.EqualTo("urn:p"));
        Assert.That(rebuiltChildren.Select(item => item.Kind), Is.EqualTo(sourceRoot.Children!.Select(item => item.Kind)));
        Assert.That(rebuiltChildren[2].Children![0].Value, Is.EqualTo("entity"));
        Assert.That(rebuiltDocumentType.ElementDeclarations, Has.Length.EqualTo(1));
        Assert.That(rebuiltDocumentType.AttributeDeclarations, Has.Length.EqualTo(1));
        Assert.That(rebuiltDocumentType.EntityDeclarations, Has.Length.EqualTo(1));
        Assert.That(rebuiltDocumentType.NotationDeclarations, Has.Length.EqualTo(1));
        Assert.DoesNotThrow(() => new TreeInvariants().ValidateXmlDocumentTree(rebuilt));
    }

    [Test]
    public void EmitsSelfContainedSubtreeWithInheritedNamespaceContext()
    {
        XmlParserOptions options = PreservationOptions();
        XmlDocument source = ParseDocument("<root xmlns:p='urn:p'><p:child p:a='v'><p:leaf/></p:child></root>", options);
        XmlNode child = source.DocumentElement!.Children![0];

        XmlEventBatch batch = new XmlEventEmitter().EmitXmlNodeEvents(child, options);
        XmlEvent[] events = batch.Events!;
        XmlNamespaceBinding[] emittedBindings = events[0].NamespaceBindings!;

        Assert.That(events[0].Kind, Is.EqualTo(XmlEventKind.StartNamespace));
        Assert.That(emittedBindings[0].Prefix, Is.EqualTo("p"));
        Assert.That(emittedBindings[0].NamespaceUri, Is.EqualTo("urn:p"));
        XmlEvent childStart = events.Single(item => item.Kind == XmlEventKind.StartElement && item.Name!.LocalName == "child");
        XmlEvent leafStart = events.Single(item => item.Kind == XmlEventKind.StartElement && item.Name!.LocalName == "leaf");
        Assert.That(childStart.Depth, Is.Zero);
        Assert.That(leafStart.Depth, Is.EqualTo(1));
        Assert.That(childStart.Attributes![0].Name!.NamespaceUri, Is.EqualTo("urn:p"));

        XmlDocument rebuilt = new TreeBuilder(source, true).BuildXmlEventTree(events);

        XmlNode rebuiltChild = rebuilt.DocumentNode!.Children![0];
        Assert.That(rebuiltChild.Name!.QualifiedName, Is.EqualTo("p:child"));
        Assert.That(rebuiltChild.NamespaceBindings![0].Prefix, Is.EqualTo("p"));
        Assert.That(rebuiltChild.Attributes![0].Name!.NamespaceUri, Is.EqualTo("urn:p"));
        Assert.That(rebuiltChild.Children![0].Name!.NamespaceUri, Is.EqualTo("urn:p"));
    }

    [Test]
    public void AppliesLexicalPreservationOptionsWithoutChangingTextContent()
    {
        XmlParserOptions parseOptions = PreservationOptions();
        parseOptions.DtdProcessing = XmlDtdProcessing.Parse;
        parseOptions.PreserveEntityReferences = true;
        XmlDocument source = ParseDocument("<!DOCTYPE root [<!ENTITY ent 'entity'>]><root>a<![CDATA[b]]>&ent;<!--note--><?pi data?></root>", parseOptions);
        XmlParserOptions compact = BasicOptions();

        XmlEventBatch compactBatch = new XmlEventEmitter().EmitXmlNodeEvents(source.DocumentElement!, compact);
        XmlEvent[] compactEvents = compactBatch.Events!;

        Assert.That(compactEvents.Select(item => item.Kind), Does.Not.Contain(XmlEventKind.CData));
        Assert.That(compactEvents.Select(item => item.Kind), Does.Not.Contain(XmlEventKind.StartEntity));
        Assert.That(compactEvents.Select(item => item.Kind), Does.Not.Contain(XmlEventKind.Comment));
        Assert.That(compactEvents.Select(item => item.Kind), Does.Not.Contain(XmlEventKind.ProcessingInstruction));
        Assert.That(string.Concat(compactEvents.Where(item => item.Kind == XmlEventKind.Text).Select(item => item.Value)), Is.EqualTo("abentity"));

        XmlEventBatch preservedBatch = new XmlEventEmitter().EmitXmlNodeEvents(source.DocumentElement!, parseOptions);
        XmlEvent[] preservedEvents = preservedBatch.Events!;

        Assert.That(preservedEvents.Select(item => item.Kind), Does.Contain(XmlEventKind.CData));
        Assert.That(preservedEvents.Select(item => item.Kind), Does.Contain(XmlEventKind.StartEntity));
        Assert.That(preservedEvents.Select(item => item.Kind), Does.Contain(XmlEventKind.EndEntity));
        Assert.That(preservedEvents.Select(item => item.Kind), Does.Contain(XmlEventKind.Comment));
        Assert.That(preservedEvents.Select(item => item.Kind), Does.Contain(XmlEventKind.ProcessingInstruction));
    }

    [Test]
    public void EmitsOrderedSelectionsOnceAndRejectsInvalidatedLiveSelections()
    {
        XmlParserOptions options = BasicOptions();
        XmlDocument document = ParseDocument("<root><parent><child/></parent><sibling/></root>", options);
        XmlNode parent = document.DocumentElement!.Children![0];
        XmlNode child = parent.Children![0];
        XmlNodeSelection selection = new XmlNodeSelection
        {
            Document = document,
            Nodes = [parent, child, child],
            MutationVersion = document.MutationVersion,
            IsFrozen = false
        };
        XmlEventEmitter emitter = new XmlEventEmitter();

        XmlEventBatch batch = emitter.EmitXmlSelectionEvents(selection, options);
        XmlEvent[] events = batch.Events!;

        Assert.That(events.Count(item => item.Kind == XmlEventKind.StartElement && item.Name!.LocalName == "parent"), Is.EqualTo(1));
        Assert.That(events.Count(item => item.Kind == XmlEventKind.StartElement && item.Name!.LocalName == "child"), Is.EqualTo(1));
        new XmlTreeEditor().SetXmlTreeText(child, "changed");
        Assert.Throws<InvalidOperationException>(() => emitter.EmitXmlSelectionEvents(selection, options));

        selection.IsFrozen = true;
        XmlEventBatch snapshotBatch = emitter.EmitXmlSelectionEvents(selection, options);
        Assert.That(snapshotBatch.Status, Is.EqualTo(XmlParserStatus.Completed));
        Assert.That(snapshotBatch.Events!.Single(item => item.Kind == XmlEventKind.Text).Value, Is.EqualTo("changed"));
    }

    [Test]
    public void EmitsFragmentAndSiblingSelectionsAsBoundaryFreeSequences()
    {
        XmlParserOptions options = BasicOptions();
        XmlDocument fragment = ParseFragment("leading<a/>middle<b/>trailing", options);

        XmlEventBatch fragmentBatch = new XmlEventEmitter().EmitXmlNodeEvents(fragment.DocumentNode!, options);
        XmlEvent[] fragmentEvents = fragmentBatch.Events!;

        Assert.That(fragmentEvents.Select(item => item.Kind), Does.Not.Contain(XmlEventKind.StartDocument));
        Assert.That(fragmentEvents.Select(item => item.Kind), Does.Not.Contain(XmlEventKind.EndDocument));
        Assert.That(fragmentEvents.Where(item => item.Kind == XmlEventKind.StartElement).All(item => item.Depth == 0), Is.True);
        Assert.That(string.Concat(fragmentEvents.Where(item => item.Kind == XmlEventKind.Text).Select(item => item.Value)), Is.EqualTo("leadingmiddletrailing"));
        XmlNode fragmentRoot = fragment.DocumentNode!;
        XmlNode[] fragmentChildren = fragmentRoot.Children!;

        XmlNodeSelection siblings = new XmlNodeSelection
        {
            Document = fragment,
            Nodes = [fragmentChildren[1], fragmentChildren[3]],
            MutationVersion = fragment.MutationVersion
        };
        XmlEventBatch siblingBatch = new XmlEventEmitter().EmitXmlSelectionEvents(siblings, options);
        XmlEvent[] siblingEvents = siblingBatch.Events!;
        Assert.That(siblingEvents.Where(item => item.Kind == XmlEventKind.StartElement).Select(item => item.Name!.LocalName), Is.EqualTo(new[] { "a", "b" }));
        Assert.That(siblingEvents.Where(item => item.Kind == XmlEventKind.StartElement).All(item => item.Depth == 0), Is.True);
    }

    [Test]
    public void CopiesNamesLocationsAttributesAndNamespaceDeclarationsAccordingToOptions()
    {
        XmlParserOptions options = PreservationOptions();
        options.RetainNamespaceDeclarations = true;
        XmlDocument document = ParseDocument("<p:root xmlns:p='urn:p' p:a='v'/>", options);
        XmlNode root = document.DocumentElement!;

        XmlEventBatch preserved = new XmlEventEmitter().EmitXmlNodeEvents(root, options);
        XmlEvent start = preserved.Events!.Single(item => item.Kind == XmlEventKind.StartElement);

        Assert.That(start.Name!.QualifiedName, Is.EqualTo("p:root"));
        Assert.That(start.Name, Is.Not.SameAs(root.Name));
        Assert.That(start.Range, Is.Not.SameAs(root.Range));
        Assert.That(start.Attributes, Has.Length.EqualTo(2));
        Assert.That(start.Attributes!.Count(item => item.IsNamespaceDeclaration), Is.EqualTo(1));
        Assert.That(start.Attributes[0].Range, Is.Not.SameAs(root.Attributes![0].Range));

        XmlParserOptions normalized = BasicOptions();
        normalized.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        XmlEvent normalizedStart = new XmlEventEmitter().EmitXmlNodeEvents(root, normalized).Events!.Single(item => item.Kind == XmlEventKind.StartElement);
        Assert.That(normalizedStart.Name!.Prefix, Is.Null);
        Assert.That(normalizedStart.Name.QualifiedName, Is.EqualTo("root"));
        Assert.That(normalizedStart.Range, Is.Null);
        Assert.That(normalizedStart.Attributes, Has.Length.EqualTo(1));
        Assert.That(normalizedStart.Attributes![0].Name!.QualifiedName, Is.EqualTo("a"));
    }

    [Test]
    public void RejectsStandaloneAttributesNullArgumentsAndInvalidOptions()
    {
        XmlParserOptions options = BasicOptions();
        XmlDocument document = ParseDocument("<root a='v'/>", options);
        XmlEventEmitter emitter = new XmlEventEmitter();
        XmlNode attribute = document.DocumentElement!.Attributes![0];
        XmlNodeSelection selection = new XmlNodeSelection
        {
            Document = document,
            Nodes = [attribute],
            MutationVersion = document.MutationVersion
        };

        Assert.Throws<ArgumentException>(() => emitter.EmitXmlNodeEvents(attribute, options));
        Assert.Throws<ArgumentException>(() => emitter.EmitXmlSelectionEvents(selection, options));
        Assert.Throws<ArgumentNullException>(() => emitter.EmitXmlNodeEvents(null!, options));
        Assert.Throws<ArgumentNullException>(() => emitter.EmitXmlSelectionEvents(null!, options));
        Assert.Throws<ArgumentNullException>(() => emitter.EmitXmlNodeEvents(document.DocumentNode!, null!));

        options.NamespaceHandling = (XmlNamespaceHandling)100;
        Assert.Throws<ArgumentOutOfRangeException>(() => emitter.EmitXmlNodeEvents(document.DocumentNode!, options));
    }

    private static XmlDocument ParseDocument(string text, XmlParserOptions options)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = text, BaseUri = "memory.xml" };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
    }

    private static XmlDocument ParseFragment(string text, XmlParserOptions options)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = text, BaseUri = "memory.xml" };
        return new XmlParser().ParseXmlFragment(input, options, null, null, null);
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

    private static XmlParserOptions PreservationOptions()
    {
        XmlParserOptions options = BasicOptions();
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        options.PreserveCData = true;
        options.PreserveComments = true;
        options.PreserveProcessingInstructions = true;
        options.PreservePrefixes = true;
        options.PreserveSourceLocations = true;
        return options;
    }
}
