using System;
using xml;

namespace xml.Tests;

[TestFixture]
public class TreeBuilderTests
{
    [Test]
    public void BuildsRetainedNodeKindsMetadataAndOwnershipFromEvents()
    {
        XmlDocument metadata = Metadata(false);
        XmlElementDeclaration elementDeclaration = new XmlElementDeclaration { Name = Name("root"), ContentModel = "ANY" };
        XmlAttributeDeclaration attributeDeclaration = new XmlAttributeDeclaration { ElementName = Name("root"), Name = Name("a"), TypeName = "CDATA" };
        XmlEntityDeclaration entityDeclaration = new XmlEntityDeclaration { Name = "ent", ReplacementText = "entity" };
        XmlNotationDeclaration notationDeclaration = new XmlNotationDeclaration { Name = "note", SystemIdentifier = "urn:note" };
        XmlDiagnostic diagnostic = new XmlDiagnostic { Code = XmlErrorCodes.DtdValidity, Message = "diagnostic" };
        XmlNamespaceBinding binding = new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:p", Range = Range(28, 43) };
        XmlAttributeData firstAttribute = new XmlAttributeData { Name = Name("a"), Value = "one", Range = Range(50, 57) };
        XmlAttributeData secondAttribute = new XmlAttributeData { Name = Name("p:b"), Value = "two", Range = Range(58, 67) };
        XmlEvent[] events =
        [
            Event(XmlEventKind.StartDocument, XmlNodeKind.Document, null, null, Range(0, 0)),
            Event(XmlEventKind.ProcessingInstruction, XmlNodeKind.ProcessingInstruction, Name("pre"), "data", Range(0, 12)),
            DtdEvent(XmlEventKind.StartDtd, Name("root"), "public-id", "root.dtd", Range(12, 48)),
            new XmlEvent { Kind = XmlEventKind.ElementDeclaration, NodeKind = XmlNodeKind.DocumentType, ElementDeclaration = elementDeclaration },
            new XmlEvent { Kind = XmlEventKind.AttributeDeclaration, NodeKind = XmlNodeKind.DocumentType, AttributeDeclaration = attributeDeclaration },
            new XmlEvent { Kind = XmlEventKind.EntityDeclaration, NodeKind = XmlNodeKind.DocumentType, EntityDeclaration = entityDeclaration },
            new XmlEvent { Kind = XmlEventKind.NotationDeclaration, NodeKind = XmlNodeKind.DocumentType, NotationDeclaration = notationDeclaration },
            DtdEvent(XmlEventKind.EndDtd, Name("root"), "public-id", "root.dtd", Range(12, 48)),
            new XmlEvent { Kind = XmlEventKind.StartNamespace, NodeKind = XmlNodeKind.Namespace, NamespaceBindings = [binding], Value = "urn:p", Range = binding.Range },
            new XmlEvent { Kind = XmlEventKind.StartElement, NodeKind = XmlNodeKind.Element, Name = Name("root"), Attributes = [firstAttribute, secondAttribute], NamespaceBindings = [binding], Range = Range(48, 70) },
            Event(XmlEventKind.Text, XmlNodeKind.Text, null, "text", Range(70, 74)),
            Event(XmlEventKind.CData, XmlNodeKind.CData, null, "cdata", Range(74, 85)),
            Event(XmlEventKind.StartEntity, XmlNodeKind.EntityReference, Name("ent"), null, Range(85, 90)),
            Event(XmlEventKind.Text, XmlNodeKind.Text, null, "entity", Range(85, 90)),
            Event(XmlEventKind.EndEntity, XmlNodeKind.EntityReference, Name("ent"), null, Range(85, 90)),
            Event(XmlEventKind.Comment, XmlNodeKind.Comment, null, "note", Range(90, 101)),
            Event(XmlEventKind.ProcessingInstruction, XmlNodeKind.ProcessingInstruction, Name("inside"), "value", Range(101, 117)),
            new XmlEvent { Kind = XmlEventKind.StartElement, NodeKind = XmlNodeKind.Element, Name = Name("leaf"), IsEmptyElement = true, Attributes = [], NamespaceBindings = [], Depth = 1, Range = Range(117, 124) },
            new XmlEvent { Kind = XmlEventKind.EndElement, NodeKind = XmlNodeKind.Element, Name = Name("leaf"), IsEmptyElement = true, Depth = 1, Range = Range(117, 124) },
            new XmlEvent { Kind = XmlEventKind.EndElement, NodeKind = XmlNodeKind.Element, Name = Name("root"), Range = Range(124, 131) },
            new XmlEvent { Kind = XmlEventKind.EndNamespace, NodeKind = XmlNodeKind.Namespace, NamespaceBindings = [binding], Value = "urn:p", Range = binding.Range },
            new XmlEvent { Kind = XmlEventKind.Diagnostic, NodeKind = XmlNodeKind.Document, Diagnostic = diagnostic, Range = diagnostic.Range },
            Event(XmlEventKind.EndDocument, XmlNodeKind.Document, null, null, Range(131, 131))
        ];

        XmlDocument document = new TreeBuilder(metadata, false).BuildXmlEventTree(events);

        Assert.That(document.Version, Is.EqualTo(XmlVersion.Xml11));
        Assert.That(document.Standalone, Is.EqualTo(XmlStandalone.Yes));
        Assert.That(document.EncodingName, Is.EqualTo("UTF-8"));
        Assert.That(document.DocumentUri, Is.EqualTo("https://example.test/document.xml"));
        Assert.That(document.BaseUri, Is.EqualTo("https://example.test/"));
        Assert.That(document.Diagnostics, Is.EqualTo(new[] { diagnostic }));
        Assert.That(document.DocumentNode!.Kind, Is.EqualTo(XmlNodeKind.Document));
        Assert.That(document.DocumentNode.Parent, Is.Null);
        Assert.That(document.DocumentNode.OwnerDocument, Is.SameAs(document));
        Assert.That(document.DocumentNode.Range, Is.Not.SameAs(metadata.DocumentNode!.Range));
        Assert.That(document.DocumentNode.Children, Has.Length.EqualTo(3));
        Assert.That(document.DocumentNode.Children![0].Kind, Is.EqualTo(XmlNodeKind.ProcessingInstruction));
        Assert.That(document.DocumentNode.Children[1].Kind, Is.EqualTo(XmlNodeKind.DocumentType));

        XmlDocumentType documentType = document.DocumentType!;
        Assert.That(documentType.Name!.QualifiedName, Is.EqualTo("root"));
        Assert.That(documentType.PublicIdentifier, Is.EqualTo("public-id"));
        Assert.That(documentType.SystemIdentifier, Is.EqualTo("root.dtd"));
        Assert.That(documentType.InternalSubset, Is.EqualTo("<!ELEMENT root ANY>"));
        Assert.That(documentType.BaseUri, Is.EqualTo("https://example.test/document.xml"));
        Assert.That(documentType.ElementDeclarations, Is.EqualTo(new[] { elementDeclaration }));
        Assert.That(documentType.AttributeDeclarations, Is.EqualTo(new[] { attributeDeclaration }));
        Assert.That(documentType.EntityDeclarations, Is.EqualTo(new[] { entityDeclaration }));
        Assert.That(documentType.NotationDeclarations, Is.EqualTo(new[] { notationDeclaration }));

        XmlNode root = document.DocumentElement!;
        Assert.That(root, Is.SameAs(document.DocumentNode.Children[2]));
        Assert.That(root.Parent, Is.SameAs(document.DocumentNode));
        Assert.That(root.OwnerDocument, Is.SameAs(document));
        Assert.That(root.Attributes, Has.Length.EqualTo(2));
        Assert.That(root.Attributes![0].Name!.QualifiedName, Is.EqualTo("a"));
        Assert.That(root.Attributes[1].Name!.QualifiedName, Is.EqualTo("p:b"));
        Assert.That(root.Attributes[0].Parent, Is.SameAs(root));
        Assert.That(root.Attributes[0].OwnerDocument, Is.SameAs(document));
        Assert.That(root.NamespaceBindings, Has.Length.EqualTo(1));
        Assert.That(root.NamespaceBindings![0].Prefix, Is.EqualTo("p"));
        Assert.That(root.NamespaceBindings[0], Is.Not.SameAs(binding));
        Assert.That(root.Range!.Start!.CharacterOffset, Is.EqualTo(48));
        Assert.That(root.Range.End!.CharacterOffset, Is.EqualTo(131));
        Assert.That(root.Children, Has.Length.EqualTo(6));
        Assert.That(root.Children![0].Kind, Is.EqualTo(XmlNodeKind.Text));
        Assert.That(root.Children[1].Kind, Is.EqualTo(XmlNodeKind.CData));
        Assert.That(root.Children[2].Kind, Is.EqualTo(XmlNodeKind.EntityReference));
        Assert.That(root.Children[2].Children![0].Value, Is.EqualTo("entity"));
        Assert.That(root.Children[3].Kind, Is.EqualTo(XmlNodeKind.Comment));
        Assert.That(root.Children[4].Kind, Is.EqualTo(XmlNodeKind.ProcessingInstruction));
        Assert.That(root.Children[5].Kind, Is.EqualTo(XmlNodeKind.Element));
        Assert.That(root.Children[5].Children, Is.Empty);
        AssertOwnedTree(document.DocumentNode, document);
        AssertIncreasingIdentities(document.DocumentNode, 0);
    }

    [Test]
    public void BuildsFragmentsCoalescesTextAndRetainsSkippedEntities()
    {
        XmlDocument metadata = Metadata(true);
        XmlEvent[] events =
        [
            Event(XmlEventKind.Text, XmlNodeKind.Text, null, "a", Range(0, 1)),
            Event(XmlEventKind.IgnorableWhitespace, XmlNodeKind.Text, null, " ", Range(1, 2)),
            Event(XmlEventKind.Text, XmlNodeKind.Text, null, "b", Range(2, 3)),
            Event(XmlEventKind.SkippedEntity, XmlNodeKind.EntityReference, Name("missing"), null, Range(3, 12)),
            new XmlEvent { Kind = XmlEventKind.StartElement, NodeKind = XmlNodeKind.Element, Name = Name("item"), Attributes = [], Range = Range(12, 19), IsEmptyElement = true },
            new XmlEvent { Kind = XmlEventKind.EndElement, NodeKind = XmlNodeKind.Element, Name = Name("item"), Range = Range(12, 19), IsEmptyElement = true }
        ];

        XmlDocument document = new TreeBuilder(metadata, true).BuildXmlEventTree(events);

        Assert.That(document.DocumentNode!.Kind, Is.EqualTo(XmlNodeKind.DocumentFragment));
        Assert.That(document.DocumentElement, Is.Null);
        Assert.That(document.DocumentType, Is.Null);
        Assert.That(document.DocumentNode.Children, Has.Length.EqualTo(3));
        Assert.That(document.DocumentNode.Children![0].Kind, Is.EqualTo(XmlNodeKind.Text));
        Assert.That(document.DocumentNode.Children[0].Value, Is.EqualTo("a b"));
        Assert.That(document.DocumentNode.Children[0].Range!.End!.CharacterOffset, Is.EqualTo(3));
        Assert.That(document.DocumentNode.Children[1].Kind, Is.EqualTo(XmlNodeKind.EntityReference));
        Assert.That(document.DocumentNode.Children[1].Name!.QualifiedName, Is.EqualTo("missing"));
        Assert.That(document.DocumentNode.Children[2].Kind, Is.EqualTo(XmlNodeKind.Element));
        AssertOwnedTree(document.DocumentNode, document);
    }

    [Test]
    public void RejectsUnbalancedEventsAndBuilderReuse()
    {
        TreeBuilder unbalanced = new TreeBuilder(Metadata(false), false);
        XmlEvent start = new XmlEvent { Kind = XmlEventKind.StartElement, Name = Name("root") };
        Assert.Throws<ArgumentException>(() => unbalanced.BuildXmlEventTree([start]));
        Assert.Throws<InvalidOperationException>(() => unbalanced.BuildXmlEventTree([]));

        TreeBuilder mismatched = new TreeBuilder(Metadata(false), false);
        XmlEvent end = new XmlEvent { Kind = XmlEventKind.EndElement, Name = Name("other") };
        Assert.Throws<ArgumentException>(() => mismatched.BuildXmlEventTree([start, end]));
    }

    private static XmlDocument Metadata(bool fragment)
    {
        XmlDocument metadata = new XmlDocument();
        metadata.Version = XmlVersion.Xml11;
        metadata.Standalone = XmlStandalone.Yes;
        metadata.DocumentUri = "https://example.test/document.xml";
        metadata.BaseUri = "https://example.test/";
        metadata.EncodingName = "UTF-8";
        metadata.DocumentNode = new XmlNode { Kind = fragment ? XmlNodeKind.DocumentFragment : XmlNodeKind.Document, Range = Range(0, 131) };
        if (!fragment)
        {
            metadata.DocumentType = new XmlDocumentType
            {
                Name = Name("root"),
                InternalSubset = "<!ELEMENT root ANY>",
                BaseUri = "https://example.test/document.xml",
                Range = Range(12, 48)
            };
        }

        return metadata;
    }

    private static XmlEvent Event(XmlEventKind kind, XmlNodeKind nodeKind, XmlName? name, string? value, SourceRange range)
    {
        return new XmlEvent { Kind = kind, NodeKind = nodeKind, Name = name, Value = value, Range = range };
    }

    private static XmlEvent DtdEvent(XmlEventKind kind, XmlName name, string publicIdentifier, string systemIdentifier, SourceRange range)
    {
        return new XmlEvent { Kind = kind, NodeKind = XmlNodeKind.DocumentType, Name = name, PublicIdentifier = publicIdentifier, SystemIdentifier = systemIdentifier, Range = range };
    }

    private static XmlName Name(string qualifiedName)
    {
        int colon = qualifiedName.IndexOf(':');
        return new XmlName
        {
            Prefix = colon < 0 ? null : qualifiedName.Substring(0, colon),
            LocalName = colon < 0 ? qualifiedName : qualifiedName.Substring(colon + 1),
            NamespaceUri = colon < 0 ? null : "urn:p",
            QualifiedName = qualifiedName
        };
    }

    private static SourceRange Range(long start, long end)
    {
        return new SourceRange
        {
            Start = new SourceLocation { SourceUri = "memory.xml", CharacterOffset = start, ByteOffset = start, Line = 1, Column = (int)start + 1 },
            End = new SourceLocation { SourceUri = "memory.xml", CharacterOffset = end, ByteOffset = end, Line = 1, Column = (int)end + 1 }
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
            Assert.That(attributes[attributeIndex].OwnerDocument, Is.SameAs(document));
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

    private static long AssertIncreasingIdentities(XmlNode node, long preceding)
    {
        Assert.That(node.Identity, Is.GreaterThan(preceding));
        long current = node.Identity;
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int attributeIndex = 0;
        while (attributeIndex < attributes.Length)
        {
            current = AssertIncreasingIdentities(attributes[attributeIndex], current);
            attributeIndex = attributeIndex + 1;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            current = AssertIncreasingIdentities(children[childIndex], current);
            childIndex = childIndex + 1;
        }

        return current;
    }
}
