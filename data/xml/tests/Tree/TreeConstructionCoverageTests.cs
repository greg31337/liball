using System;
using System.Linq;
using xml;

namespace xml.Tests;

[TestFixture]
public class TreeConstructionCoverageTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void BuildsTheExpectedTreeForCompactAndPreservedParserModes(bool preserve)
    {
        XmlParserOptions options = Options();
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.PreserveCData = preserve;
        options.PreserveComments = preserve;
        options.PreserveProcessingInstructions = preserve;
        options.PreserveEntityReferences = preserve;
        options.PreservePrefixes = preserve;
        options.RetainNamespaceDeclarations = preserve;
        string text = "<!DOCTYPE p:root [<!ENTITY ent 'entity'>]><p:root xmlns:p='urn:p' p:a='v'>a<![CDATA[b]]>c&ent;<!--note--><?pi data?></p:root>";

        XmlDocument document = ParseDocument(text, options);
        XmlNode root = document.DocumentElement!;

        Assert.That(root.Name!.LocalName, Is.EqualTo("root"));
        Assert.That(root.Name.NamespaceUri, Is.EqualTo("urn:p"));
        Assert.That(root.Name.Prefix, Is.EqualTo(preserve ? "p" : null));
        Assert.That(root.Name.QualifiedName, Is.EqualTo(preserve ? "p:root" : "root"));
        Assert.That(root.NamespaceBindings, Has.Length.EqualTo(1));
        Assert.That(root.NamespaceBindings![0].NamespaceUri, Is.EqualTo("urn:p"));
        Assert.That(root.Attributes, Has.Length.EqualTo(preserve ? 2 : 1));
        Assert.That(root.Attributes!.Last().Name!.NamespaceUri, Is.EqualTo("urn:p"));
        Assert.That(root.Attributes.Last().Value, Is.EqualTo("v"));

        if (preserve)
        {
            XmlNode[] children = root.Children!;
            Assert.That(children.Select(item => item.Kind), Is.EqualTo(new[]
            {
                XmlNodeKind.Text,
                XmlNodeKind.CData,
                XmlNodeKind.Text,
                XmlNodeKind.EntityReference,
                XmlNodeKind.Comment,
                XmlNodeKind.ProcessingInstruction
            }));
            Assert.That(children[0].Value, Is.EqualTo("a"));
            Assert.That(children[1].Value, Is.EqualTo("b"));
            Assert.That(children[2].Value, Is.EqualTo("c"));
            Assert.That(children[3].Name!.QualifiedName, Is.EqualTo("ent"));
            Assert.That(children[3].Children![0].Value, Is.EqualTo("entity"));
            Assert.That(children[4].Value, Is.EqualTo("note"));
            Assert.That(children[5].Name!.QualifiedName, Is.EqualTo("pi"));
            Assert.That(children[5].Value, Is.EqualTo("data"));
        }
        else
        {
            Assert.That(root.Children, Has.Length.EqualTo(1));
            Assert.That(root.Children![0].Kind, Is.EqualTo(XmlNodeKind.Text));
            Assert.That(root.Children[0].Value, Is.EqualTo("abcentity"));
        }

        AssertOwnedTree(document.DocumentNode!, document);
    }

    [Test]
    public void RetainsCompleteDtdMetadataAndDefaultedTreeAttributes()
    {
        XmlParserOptions options = Options();
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.PreserveSourceLocations = true;
        string subset = "<!ELEMENT root (child)><!ELEMENT child EMPTY><!ATTLIST root state (on|off) 'on'><!ENTITY ent 'value'><!NOTATION note SYSTEM 'urn:note'>";
        XmlInput input = Input("<!DOCTYPE root [" + subset + "]><root><child/></root>");
        input.BaseUri = "https://example.test/document.xml";

        XmlDocument document = new XmlParser().ParseXmlDocument(input, options, null, null, null);
        XmlDocumentType documentType = document.DocumentType!;

        Assert.That(document.DocumentNode!.Children![0].Kind, Is.EqualTo(XmlNodeKind.DocumentType));
        Assert.That(document.DocumentNode.Children[0].Name!.QualifiedName, Is.EqualTo("root"));
        Assert.That(documentType.Name!.QualifiedName, Is.EqualTo("root"));
        Assert.That(documentType.InternalSubset, Is.EqualTo(subset));
        Assert.That(documentType.BaseUri, Is.EqualTo(input.BaseUri));
        Assert.That(documentType.Range, Is.Not.Null);
        Assert.That(documentType.ElementDeclarations, Has.Length.EqualTo(2));
        Assert.That(documentType.ElementDeclarations![0].ContentModel, Is.EqualTo("(child)"));
        Assert.That(documentType.ElementDeclarations[0].ReferencedNames, Is.EqualTo(new[] { "child" }));
        Assert.That(documentType.ElementDeclarations[1].IsEmpty, Is.True);
        Assert.That(documentType.AttributeDeclarations, Has.Length.EqualTo(1));
        Assert.That(documentType.AttributeDeclarations![0].AllowedValues, Is.EqualTo(new[] { "on", "off" }));
        Assert.That(documentType.AttributeDeclarations[0].DefaultValue, Is.EqualTo("on"));
        Assert.That(documentType.EntityDeclarations, Has.Length.EqualTo(1));
        Assert.That(documentType.EntityDeclarations![0].ReplacementText, Is.EqualTo("value"));
        Assert.That(documentType.EntityDeclarations[0].IsParsed, Is.True);
        Assert.That(documentType.NotationDeclarations, Has.Length.EqualTo(1));
        Assert.That(documentType.NotationDeclarations![0].SystemIdentifier, Is.EqualTo("urn:note"));
        Assert.That(document.DocumentElement!.Attributes, Has.Length.EqualTo(1));
        Assert.That(document.DocumentElement.Attributes![0].Name!.QualifiedName, Is.EqualTo("state"));
        Assert.That(document.DocumentElement.Attributes[0].Value, Is.EqualTo("on"));
    }

    [Test]
    public void CopiesEventLocationsIntoIndependentTreeRanges()
    {
        XmlParserOptions options = Options();
        options.PreserveProcessingInstructions = true;
        options.PreserveSourceLocations = true;
        string text = "<?pre data?>\n<root a='v'>one\n<child/>two</root>\n ";
        XmlInput input = Input(text);
        input.BaseUri = "memory.xml";
        XmlParserEngine engine = new XmlParserEngine(input, options, null, null, null, false);

        XmlDocument document = engine.ParseXmlInput();
        XmlEvent[] events = engine.SnapshotXmlParserEvents();
        XmlEvent rootStart = events.Single(item => item.Kind == XmlEventKind.StartElement && item.Name!.LocalName == "root");
        XmlEvent rootEnd = events.Single(item => item.Kind == XmlEventKind.EndElement && item.Name!.LocalName == "root");
        XmlNode root = document.DocumentElement!;
        XmlNode container = document.DocumentNode!;

        Assert.That(container.Children!.Select(item => item.Kind), Is.EqualTo(new[] { XmlNodeKind.ProcessingInstruction, XmlNodeKind.Element }));
        Assert.That(container.Range!.Start!.CharacterOffset, Is.Zero);
        Assert.That(container.Range.End!.CharacterOffset, Is.EqualTo(text.Length));
        Assert.That(root.Range, Is.Not.SameAs(rootStart.Range));
        Assert.That(root.Range!.Start, Is.Not.SameAs(rootStart.Range!.Start));
        Assert.That(root.Range.Start!.CharacterOffset, Is.EqualTo(rootStart.Range.Start!.CharacterOffset));
        Assert.That(root.Range.Start.Line, Is.EqualTo(2));
        Assert.That(root.Range.Start.Column, Is.EqualTo(1));
        Assert.That(root.Range.End!.CharacterOffset, Is.EqualTo(rootEnd.Range!.End!.CharacterOffset));
        Assert.That(root.Attributes![0].Range, Is.Not.SameAs(rootStart.Attributes![0].Range));
        Assert.That(root.Attributes[0].Range!.Start!.SourceUri, Is.EqualTo("memory.xml"));
        Assert.That(root.Children![0].Range!.Start!.Line, Is.EqualTo(2));
        Assert.That(root.Children[1].Range!.Start!.Line, Is.EqualTo(3));
        Assert.That(root.Children[2].Range!.End!.CharacterOffset, Is.EqualTo(text.IndexOf("</root>", StringComparison.Ordinal)));
    }

    [Test]
    public void RetainsNamespaceScopesUndeclarationsAndDeclarationAttributes()
    {
        XmlParserOptions options = Options();
        options.Version = XmlVersion.Xml11;
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces11;
        options.PreservePrefixes = true;
        options.RetainNamespaceDeclarations = true;
        XmlDocument document = ParseDocument("<root xmlns='urn:outer' xmlns:p='urn:p'><p:item p:a='v'/><branch xmlns='' xmlns:p=''><leaf/></branch></root>", options);
        XmlNode root = document.DocumentElement!;
        XmlNode item = root.Children![0];
        XmlNode branch = root.Children[1];

        Assert.That(root.Name!.NamespaceUri, Is.EqualTo("urn:outer"));
        Assert.That(root.NamespaceBindings, Has.Length.EqualTo(2));
        Assert.That(root.Attributes, Has.Length.EqualTo(2));
        Assert.That(root.Attributes!.All(attribute => attribute.Name!.NamespaceUri == XmlNamespaces.Xmlns), Is.True);
        Assert.That(item.Name!.NamespaceUri, Is.EqualTo("urn:p"));
        Assert.That(item.Attributes![0].Name!.NamespaceUri, Is.EqualTo("urn:p"));
        Assert.That(item.NamespaceBindings, Is.Empty);
        Assert.That(branch.Name!.NamespaceUri, Is.Null);
        Assert.That(branch.NamespaceBindings, Has.Length.EqualTo(2));
        Assert.That(branch.NamespaceBindings!.All(binding => binding.IsUndeclaration), Is.True);
        Assert.That(branch.Children![0].Name!.NamespaceUri, Is.Null);
        Assert.That(branch.Attributes, Has.Length.EqualTo(2));
    }

    [Test]
    public void BuildsNestedAndSkippedEntityReferenceNodesAtTheirEventBoundaries()
    {
        XmlParserOptions preserved = Options();
        preserved.DtdProcessing = XmlDtdProcessing.Parse;
        preserved.PreserveEntityReferences = true;
        XmlDocument nestedDocument = ParseDocument("<!DOCTYPE root [<!ENTITY outer 'A&inner;C'><!ENTITY inner 'B'>]><root>&outer;</root>", preserved);
        XmlNode outer = nestedDocument.DocumentElement!.Children![0];
        XmlNode inner = outer.Children![1];

        Assert.That(outer.Kind, Is.EqualTo(XmlNodeKind.EntityReference));
        Assert.That(outer.Name!.QualifiedName, Is.EqualTo("outer"));
        Assert.That(outer.Children!.Select(item => item.Kind), Is.EqualTo(new[] { XmlNodeKind.Text, XmlNodeKind.EntityReference, XmlNodeKind.Text }));
        Assert.That(inner.Parent, Is.SameAs(outer));
        Assert.That(inner.Name!.QualifiedName, Is.EqualTo("inner"));
        Assert.That(inner.Children![0].Value, Is.EqualTo("B"));

        XmlParserOptions skipped = Options();
        skipped.DtdProcessing = XmlDtdProcessing.Ignore;
        XmlDocument skippedDocument = ParseDocument("<!DOCTYPE root SYSTEM 'missing.dtd'><root>&missing;</root>", skipped);
        XmlNode missing = skippedDocument.DocumentElement!.Children![0];
        Assert.That(skippedDocument.DocumentType, Is.Null);
        Assert.That(missing.Kind, Is.EqualTo(XmlNodeKind.EntityReference));
        Assert.That(missing.Name!.QualifiedName, Is.EqualTo("missing"));
        Assert.That(missing.Children, Is.Empty);
    }

    [Test]
    public void BuildsFragmentContainersWithInheritedNamespacesAndEveryTopLevelContentKind()
    {
        XmlParserOptions options = Options();
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces11;
        options.PreservePrefixes = true;
        options.PreserveCData = true;
        options.PreserveComments = true;
        options.PreserveProcessingInstructions = true;
        options.FragmentNamespaceBindings = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:inherited" }];

        XmlDocument document = ParseFragment("<p:first/>text<![CDATA[cdata]]><!--note--><?pi data?><p:second/>", options);
        XmlNode container = document.DocumentNode!;
        XmlNode[] children = container.Children!;

        Assert.That(container.Kind, Is.EqualTo(XmlNodeKind.DocumentFragment));
        Assert.That(container.Parent, Is.Null);
        Assert.That(document.DocumentElement, Is.Null);
        Assert.That(document.DocumentType, Is.Null);
        Assert.That(children.Select(item => item.Kind), Is.EqualTo(new[]
        {
            XmlNodeKind.Element,
            XmlNodeKind.Text,
            XmlNodeKind.CData,
            XmlNodeKind.Comment,
            XmlNodeKind.ProcessingInstruction,
            XmlNodeKind.Element
        }));
        Assert.That(children[0].Name!.NamespaceUri, Is.EqualTo("urn:inherited"));
        Assert.That(children[0].NamespaceBindings, Is.Empty);
        Assert.That(children[5].Name!.QualifiedName, Is.EqualTo("p:second"));
        AssertOwnedTree(container, document);
    }

    private static XmlDocument ParseDocument(string text, XmlParserOptions options)
    {
        return new XmlParser().ParseXmlDocument(Input(text), options, null, null, null);
    }

    private static XmlDocument ParseFragment(string text, XmlParserOptions options)
    {
        return new XmlParser().ParseXmlFragment(Input(text), options, null, null, null);
    }

    private static XmlInput Input(string text)
    {
        return new XmlInput { Kind = XmlInputKind.String, Text = text, BaseUri = "urn:tree-construction" };
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

    private static void AssertOwnedTree(XmlNode node, XmlDocument document)
    {
        Assert.That(node.OwnerDocument, Is.SameAs(document));
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int attributeIndex = 0;
        while (attributeIndex < attributes.Length)
        {
            Assert.That(attributes[attributeIndex].Parent, Is.SameAs(node));
            AssertOwnedTree(attributes[attributeIndex], document);
            attributeIndex = attributeIndex + 1;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            Assert.That(children[childIndex].Parent, Is.SameAs(node));
            AssertOwnedTree(children[childIndex], document);
            childIndex = childIndex + 1;
        }
    }
}
