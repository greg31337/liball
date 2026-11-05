using System.Text;
using NUnit.Framework;

namespace odf.Tests;

public class RdfaDocumentModelTests
{
    [Test]
    public void ExtractsRdfaCurieLiteralsResourcesAndScopedPrefixes()
    {
        OdfDocument document = ReadFlat(RdfaXml());

        Assert.That(document.RdfTriples, Has.Length.EqualTo(5));
        OdfRdfTriple title = FindPredicate(
            document, OdfNamespaces.DublinCore + "title");
        Assert.That(title.SourceKind, Is.EqualTo(OdfRdfSourceKind.Rdfa));
        Assert.That(title.Subject, Is.EqualTo("urn:example:document"));
        Assert.That(title.SubjectLexical, Is.EqualTo("[ex:document]"));
        Assert.That(title.PredicateLexical, Is.EqualTo("dc:title"));
        Assert.That(title.Object, Is.EqualTo("Plan"));
        Assert.That(title.Datatype,
            Is.EqualTo("http://www.w3.org/2001/XMLSchema#string"));
        Assert.That(title.Language, Is.EqualTo("en"));
        Assert.That(title.IsLiteral, Is.True);
        Assert.That(title.IsValid, Is.True);

        OdfRdfTriple score = FindPredicate(document, "urn:example:score");
        Assert.That(score.Subject, Is.EqualTo(title.Subject));
        Assert.That(score.Object, Is.EqualTo(title.Object));

        OdfRdfTriple related = FindPredicate(document, "urn:example:related");
        Assert.That(related.Subject, Is.EqualTo("#document"));
        Assert.That(related.Object, Is.EqualTo("urn:example:asset"));
        Assert.That(related.ObjectLexical, Is.EqualTo("[ex:asset]"));
        Assert.That(related.IsLiteral, Is.False);
        Assert.That(related.IsValid, Is.True);

        OdfRdfTriple label = FindPredicate(document, "urn:example:label");
        Assert.That(label.Object, Is.EqualTo("Nested literal"));
        Assert.That(label.ObjectLexical, Is.EqualTo("Nested literal"));

        OdfRdfTriple shadow = FindPredicate(document, "urn:shadow:name");
        Assert.That(shadow.Subject, Is.EqualTo("urn:shadow:item"));
        Assert.That(shadow.IsValid, Is.True);
    }

    [Test]
    public void UpdatesAndRemovesRdfaAndAddsItToAnExistingContentNode()
    {
        OdfDocument document = ReadFlat(RdfaXml());
        OdfRdfTriple label = FindPredicate(document, "urn:example:label");
        label.Subject = "urn:example:updated";
        label.Predicate = OdfNamespaces.DublinCore + "description";
        label.Object = "Updated literal";
        label.Datatype = "http://www.w3.org/2001/XMLSchema#string";
        label.Language = "fr";

        OdfRdfTriple related = FindPredicate(document, "urn:example:related");
        related.ObjectLexical = "[ex:newAsset]";

        OdfRdfTriple shadow = FindPredicate(document, "urn:shadow:name");
        OdfNode target = FindStyleNode(document.Body, "RdfaTarget");
        OdfRdfTriple added = new OdfRdfTriple
        {
            SourceKind = OdfRdfSourceKind.Rdfa,
            SubjectLexical = "[ex:new]",
            PredicateLexical = "ex:note",
            Object = "Added literal",
            ObjectLexical = "Added literal",
            IsLiteral = true,
            Node = target
        };
        document.RdfTriples = ReplaceShadowWithAdded(
            document.RdfTriples, shadow, added);

        OdfDocumentWriter writer = new OdfDocumentWriter();
        byte[] firstBytes = writer.WriteDocumentBytes(
            document, new OdfSaveOptions());
        OdfDocument first = new OdfDocumentReader().ReadDocumentBytes(
            firstBytes, "first.fodt", new OdfLoadOptions());

        OdfRdfTriple updated = FindPredicate(
            first, OdfNamespaces.DublinCore + "description");
        Assert.That(updated.Subject, Is.EqualTo("urn:example:updated"));
        Assert.That(updated.SubjectLexical, Is.EqualTo("urn:example:updated"));
        Assert.That(updated.PredicateLexical, Is.EqualTo("dc:description"));
        Assert.That(updated.Object, Is.EqualTo("Updated literal"));
        Assert.That(updated.DatatypeLexical, Is.EqualTo("xsd:string"));
        Assert.That(updated.Language, Is.EqualTo("fr"));
        Assert.That(FindPredicate(first, "urn:example:related").Object,
            Is.EqualTo("urn:example:newAsset"));
        Assert.That(FindPredicate(first, "urn:example:note").Object,
            Is.EqualTo("Added literal"));
        Assert.That(ContainsPredicate(first, "urn:shadow:name"), Is.False);
        Assert.That(Encoding.UTF8.GetString(firstBytes),
            Does.Contain("foreign:flag=\"kept\""));

        byte[] secondBytes = writer.WriteDocumentBytes(
            first, new OdfSaveOptions());
        OdfDocument second = new OdfDocumentReader().ReadDocumentBytes(
            secondBytes, "second.fodt", new OdfLoadOptions());
        Assert.That(FindPredicate(
            second, OdfNamespaces.DublinCore + "description").Object,
            Is.EqualTo("Updated literal"));
        Assert.That(FindPredicate(second, "urn:example:note").Object,
            Is.EqualTo("Added literal"));
    }

    [Test]
    public void PreservesMalformedRdfaAndReportsItAsInvalid()
    {
        string xml = RdfaXml().Replace(
            "<text:span text:style-name='RdfaTarget'>target</text:span>",
            "<text:span text:style-name='RdfaTarget' "
                + "xhtml:about='[missing:item]' "
                + "xhtml:property='missing:name' "
                + "xhtml:datatype='missing:type'>target</text:span>");
        OdfDocument document = ReadFlat(xml);
        OdfRdfTriple invalid = document.RdfTriples[
            document.RdfTriples.Length - 1];

        Assert.That(invalid.Subject, Is.Empty);
        Assert.That(invalid.Predicate, Is.Empty);
        Assert.That(invalid.IsValid, Is.False);

        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document, new OdfSaveOptions());
        string output = Encoding.UTF8.GetString(bytes);
        Assert.That(output, Does.Contain("xhtml:about=\"[missing:item]\""));
        Assert.That(output, Does.Contain("xhtml:property=\"missing:name\""));
        Assert.That(output, Does.Contain("xhtml:datatype=\"missing:type\""));
    }

    static OdfDocument ReadFlat(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "rdfa.fodt",
            new OdfLoadOptions());
    }

    static OdfRdfTriple FindPredicate(OdfDocument document, string predicate)
    {
        int index = 0;
        while (index < document.RdfTriples.Length)
        {
            if (document.RdfTriples[index].Predicate == predicate)
            {
                return document.RdfTriples[index];
            }
            index = index + 1;
        }
        Assert.Fail("The expected RDF predicate was not found: " + predicate);
        return new OdfRdfTriple();
    }

    static bool ContainsPredicate(OdfDocument document, string predicate)
    {
        int index = 0;
        while (index < document.RdfTriples.Length)
        {
            if (document.RdfTriples[index].Predicate == predicate)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfRdfTriple[] ReplaceShadowWithAdded(
        OdfRdfTriple[] triples,
        OdfRdfTriple removed,
        OdfRdfTriple added)
    {
        OdfRdfTriple[] result = new OdfRdfTriple[triples.Length];
        int source = 0;
        int destination = 0;
        while (source < triples.Length)
        {
            if (triples[source] != removed)
            {
                result[destination] = triples[source];
                destination = destination + 1;
            }
            source = source + 1;
        }
        result[destination] = added;
        return result;
    }

    static OdfNode FindStyleNode(OdfNode? node, string styleName)
    {
        if (node == null)
        {
            Assert.Fail("The document body was not found.");
            return new OdfNode();
        }
        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[attributeIndex];
            if (attribute.Name.NamespaceName == OdfNamespaces.Text
                && attribute.Name.LocalName == "style-name"
                && attribute.Value == styleName)
            {
                return node;
            }
            attributeIndex = attributeIndex + 1;
        }
        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            OdfNode? found = FindStyleNodeOrNull(
                node.Children[childIndex], styleName);
            if (found != null)
            {
                return found;
            }
            childIndex = childIndex + 1;
        }
        Assert.Fail("The target content node was not found.");
        return new OdfNode();
    }

    static OdfNode? FindStyleNodeOrNull(OdfNode node, string styleName)
    {
        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[attributeIndex];
            if (attribute.Name.NamespaceName == OdfNamespaces.Text
                && attribute.Name.LocalName == "style-name"
                && attribute.Value == styleName)
            {
                return node;
            }
            attributeIndex = attributeIndex + 1;
        }
        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            OdfNode? found = FindStyleNodeOrNull(
                node.Children[childIndex], styleName);
            if (found != null)
            {
                return found;
            }
            childIndex = childIndex + 1;
        }
        return null;
    }

    static string RdfaXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' xmlns:text='" + OdfNamespaces.Text
            + "' xmlns:xhtml='" + OdfNamespaces.Xhtml
            + "' xmlns:dc='" + OdfNamespaces.DublinCore
            + "' xmlns:ex='urn:example:'"
            + " xmlns:xsd='http://www.w3.org/2001/XMLSchema#'"
            + " xmlns:foreign='urn:foreign' office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + "<office:body><office:text><text:p xml:lang='en'>"
            + "<text:span xhtml:about='[ex:document]' "
            + "xhtml:property='dc:title ex:score' xhtml:content='Plan' "
            + "xhtml:datatype='xsd:string' foreign:flag='kept'>visible</text:span>"
            + "<text:span xhtml:about='#document' xhtml:property='ex:related' "
            + "xhtml:resource='[ex:asset]'/>"
            + "<text:span xhtml:about='[ex:text]' xhtml:property='ex:label'>"
            + "Nested <text:span>literal</text:span></text:span>"
            + "<text:span xmlns:ex='urn:shadow:' xhtml:about='[ex:item]' "
            + "xhtml:property='ex:name'>Shadow</text:span>"
            + "<text:span text:style-name='RdfaTarget'>target</text:span>"
            + "</text:p></office:text></office:body></office:document>";
    }
}
