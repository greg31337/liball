using System;
using System.Linq;
using xml;

namespace xml.Tests;

[TestFixture]
public class NormalizedPushEventTests
{
    [Test]
    public void EmitsDocumentDtdNamespaceContentAndEntityEventsInSourceOrder()
    {
        string text = "<?pre data?><!DOCTYPE root [<!ELEMENT root (#PCDATA|child)*><!ATTLIST root state CDATA 'ready'><!ENTITY ent 'entity'><!NOTATION note SYSTEM 'urn:note'><!ELEMENT child EMPTY>]><root xmlns='urn:r' xmlns:p='urn:p' p:a='v'>text<![CDATA[cdata]]><!--comment--><?inside value?>&ent;<child/></root><?post done?>";
        XmlParserOptions options = EventOptions();
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        options.PreserveCData = true;
        options.PreserveComments = true;
        options.PreserveProcessingInstructions = true;
        options.PreserveEntityReferences = true;
        options.PreservePrefixes = true;
        options.PreserveSourceLocations = true;
        XmlPushParser parser = BeginStringParser(text, options);

        XmlEventBatch batch = parser.FinishXmlPush();
        XmlEvent[] events = batch.Events!;

        Assert.That(events.Select(item => item.Kind), Is.EqualTo(new[]
        {
            XmlEventKind.StartDocument,
            XmlEventKind.ProcessingInstruction,
            XmlEventKind.StartDtd,
            XmlEventKind.ElementDeclaration,
            XmlEventKind.AttributeDeclaration,
            XmlEventKind.EntityDeclaration,
            XmlEventKind.NotationDeclaration,
            XmlEventKind.ElementDeclaration,
            XmlEventKind.EndDtd,
            XmlEventKind.StartNamespace,
            XmlEventKind.StartNamespace,
            XmlEventKind.StartElement,
            XmlEventKind.Text,
            XmlEventKind.CData,
            XmlEventKind.Comment,
            XmlEventKind.ProcessingInstruction,
            XmlEventKind.StartEntity,
            XmlEventKind.Text,
            XmlEventKind.EndEntity,
            XmlEventKind.StartElement,
            XmlEventKind.EndElement,
            XmlEventKind.EndElement,
            XmlEventKind.EndNamespace,
            XmlEventKind.EndNamespace,
            XmlEventKind.ProcessingInstruction,
            XmlEventKind.EndDocument
        }));
        Assert.That(batch.Status, Is.EqualTo(XmlParserStatus.Completed));
        Assert.That(batch.IsFinal, Is.True);

        XmlEvent dtdStart = events[2];
        Assert.That(dtdStart.Name!.QualifiedName, Is.EqualTo("root"));
        Assert.That(dtdStart.Range, Is.Not.Null);
        Assert.That(events[3].ElementDeclaration!.ContentModel, Is.EqualTo("(#PCDATA|child)*"));
        Assert.That(events[4].AttributeDeclaration!.Name!.QualifiedName, Is.EqualTo("state"));
        Assert.That(events[5].EntityDeclaration!.ReplacementText, Is.EqualTo("entity"));
        Assert.That(events[6].NotationDeclaration!.SystemIdentifier, Is.EqualTo("urn:note"));

        Assert.That(events[9].NamespaceBindings![0].Prefix, Is.Empty);
        Assert.That(events[9].Value, Is.EqualTo("urn:r"));
        Assert.That(events[10].NamespaceBindings![0].Prefix, Is.EqualTo("p"));
        Assert.That(events[10].Value, Is.EqualTo("urn:p"));

        XmlEvent root = events[11];
        Assert.That(root.Name!.NamespaceUri, Is.EqualTo("urn:r"));
        Assert.That(root.Attributes, Has.Length.EqualTo(2));
        Assert.That(root.Attributes!.Single(item => item.Name!.QualifiedName == "p:a").Name!.NamespaceUri, Is.EqualTo("urn:p"));
        XmlAttributeData defaulted = root.Attributes.Single(item => item.Name!.QualifiedName == "state");
        Assert.That(defaulted.Value, Is.EqualTo("ready"));
        Assert.That(defaulted.IsDefaulted, Is.True);
        Assert.That(defaulted.IsSpecified, Is.False);
        Assert.That(root.Range!.Start!.CharacterOffset, Is.EqualTo(text.IndexOf("<root", StringComparison.Ordinal)));
        Assert.That(events[12].Value, Is.EqualTo("text"));
        Assert.That(events[13].Value, Is.EqualTo("cdata"));
        Assert.That(events[14].Value, Is.EqualTo("comment"));
        Assert.That(events[15].Name!.QualifiedName, Is.EqualTo("inside"));
        Assert.That(events[16].Name!.QualifiedName, Is.EqualTo("ent"));
        Assert.That(events[17].Value, Is.EqualTo("entity"));
        Assert.That(events[19].Depth, Is.EqualTo(1));
        Assert.That(events[21].Depth, Is.Zero);
        Assert.That(events[22].NamespaceBindings![0].Prefix, Is.EqualTo("p"));
        Assert.That(events[23].NamespaceBindings![0].Prefix, Is.Empty);
        Assert.That(events[24].Name!.QualifiedName, Is.EqualTo("post"));
        Assert.That(events[25].Range!.Start!.CharacterOffset, Is.EqualTo(text.Length));
    }

    [Test]
    public void EmitsIgnorableWhitespaceAndAccumulatedValidationDiagnostics()
    {
        string text = "<!DOCTYPE root [<!ELEMENT root (child)><!ELEMENT child EMPTY>]><root bad='x'>  <child/></root>";
        XmlParserOptions options = EventOptions();
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.ValidationMode = XmlValidationMode.Dtd;
        options.AccumulateValidationDiagnostics = true;
        options.PreserveIgnorableWhitespace = true;
        XmlPushParser parser = BeginStringParser(text, options);

        XmlEvent[] events = parser.FinishXmlPush().Events!;

        XmlEvent whitespace = events.Single(item => item.Kind == XmlEventKind.IgnorableWhitespace);
        Assert.That(whitespace.Value, Is.EqualTo("  "));
        Assert.That(whitespace.IsIgnorableWhitespace, Is.True);
        XmlEvent diagnostic = events.Single(item => item.Kind == XmlEventKind.Diagnostic);
        Assert.That(diagnostic.Diagnostic, Is.SameAs(parser.Driver.ParsedDocument!.Diagnostics![0]));
        Assert.That(diagnostic.Diagnostic!.Message, Does.Contain("DTD declaration"));
        Assert.That(Array.IndexOf(events, diagnostic), Is.EqualTo(events.Length - 2));
        Assert.That(events[^1].Kind, Is.EqualTo(XmlEventKind.EndDocument));
    }

    [Test]
    public void EmitsSkippedEntityWhenAnExternalSubsetWasNotLoaded()
    {
        XmlParserOptions options = EventOptions();
        options.DtdProcessing = XmlDtdProcessing.Ignore;
        XmlPushParser parser = BeginStringParser("<!DOCTYPE root SYSTEM 'missing.dtd'><root>&missing;</root>", options);

        XmlEvent[] events = parser.FinishXmlPush().Events!;

        Assert.That(events.Select(item => item.Kind), Is.EqualTo(new[]
        {
            XmlEventKind.StartDocument,
            XmlEventKind.StartDtd,
            XmlEventKind.EndDtd,
            XmlEventKind.StartElement,
            XmlEventKind.SkippedEntity,
            XmlEventKind.EndElement,
            XmlEventKind.EndDocument
        }));
        Assert.That(events[4].Name!.QualifiedName, Is.EqualTo("missing"));
        Assert.That(events[4].NodeKind, Is.EqualTo(XmlNodeKind.EntityReference));
    }

    [Test]
    public void AppliesPreservationOptionsToLexicalAndEntityEvents()
    {
        XmlPushParser parser = BeginStringParser("<r>a<![CDATA[b]]><!--drop--><?drop value?>&amp;</r>", EventOptions());

        XmlEvent[] events = parser.FinishXmlPush().Events!;

        Assert.That(events.Select(item => item.Kind), Is.EqualTo(new[]
        {
            XmlEventKind.StartDocument,
            XmlEventKind.StartElement,
            XmlEventKind.Text,
            XmlEventKind.Text,
            XmlEventKind.Text,
            XmlEventKind.EndElement,
            XmlEventKind.EndDocument
        }));
        Assert.That(events.Where(item => item.Kind == XmlEventKind.Text).Select(item => item.Value), Is.EqualTo(new[] { "a", "b", "&" }));
    }

    private static XmlPushParser BeginStringParser(string text, XmlParserOptions options)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = text;
        input.BaseUri = "urn:event-test";
        XmlPushParser parser = new XmlPushParser();
        parser.BeginXmlPush(input, options, null, null, null, null);
        return parser;
    }

    private static XmlParserOptions EventOptions()
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
