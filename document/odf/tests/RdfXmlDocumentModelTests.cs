using System.IO;
using System.Text;
using NUnit.Framework;

namespace odf.Tests;

public class RdfXmlDocumentModelTests
{
    [Test]
    public void ReadsAndQueriesPackageManifestAndRdfXmlTriples()
    {
        OdfDocument document = ReadRdfPackage();

        Assert.That(document.RdfTriples, Has.Length.EqualTo(5));
        OdfRdfTriple packagePart = FindTriple(
            document, OdfNamespaces.PackageMetadata + "hasPart",
            "content.xml");
        Assert.That(packagePart.SourceKind,
            Is.EqualTo(OdfRdfSourceKind.PackageMetadata));
        Assert.That(packagePart.PartPath,
            Is.EqualTo(OdfPartNames.MetadataManifest));
        Assert.That(packagePart.Subject,
            Is.EqualTo(OdfPartNames.MetadataManifest));
        Assert.That(packagePart.IsLiteral, Is.False);

        OdfRdfTriple title = FindTriple(
            document, OdfNamespaces.DublinCore + "title", "Graph title");
        Assert.That(title.SourceKind, Is.EqualTo(OdfRdfSourceKind.RdfXml));
        Assert.That(title.Subject, Is.EqualTo("urn:item"));
        Assert.That(title.Language, Is.EqualTo("en"));
        Assert.That(title.IsLiteral, Is.True);
        Assert.That(title.IsValid, Is.True);

        OdfRdfTriple relation = FindTriple(
            document, "urn:example:related", "urn:other");
        Assert.That(relation.IsLiteral, Is.False);
        OdfRdfTriple type = FindTriple(
            document, OdfNamespaces.Rdf + "type", "urn:example:Thing");
        Assert.That(type.Subject, Is.EqualTo("urn:typed"));

        IOdfRdfQuery query = new OdfRdfQuery();
        OdfRdfTriple[] queried = query.FindRdfTriples(
            document, "urn:item", OdfNamespaces.DublinCore + "title", null);
        Assert.That(queried, Has.Length.EqualTo(1));
        Assert.That(queried[0], Is.SameAs(title));
    }

    [Test]
    public void UpdatesRdfXmlAndPackageMetadataWhilePreservingForeignXml()
    {
        OdfDocument document = ReadRdfPackage();
        OdfRdfTriple title = FindTriple(
            document, OdfNamespaces.DublinCore + "title", "Graph title");
        title.Object = "Updated graph title";
        OdfRdfTriple relation = FindTriple(
            document, "urn:example:related", "urn:other");
        relation.Object = "urn:new-resource";
        OdfRdfTriple type = FindTriple(
            document, OdfNamespaces.Rdf + "type", "urn:example:Thing");
        OdfRdfTriple added = new OdfRdfTriple
        {
            SourceKind = OdfRdfSourceKind.PackageMetadata,
            PartPath = OdfPartNames.MetadataManifest,
            Subject = OdfPartNames.MetadataManifest,
            Predicate = OdfNamespaces.PackageMetadata + "hasPart",
            Object = "styles.xml",
            IsLiteral = false
        };
        document.RdfTriples = RemoveAndAppend(
            document.RdfTriples, type, added);

        OdfDocumentWriter writer = new OdfDocumentWriter();
        byte[] bytes = writer.WriteDocumentBytes(
            document, new OdfSaveOptions());
        OdfDocument roundTrip = new OdfDocumentReader().ReadDocumentBytes(
            bytes, "updated.odt", new OdfLoadOptions());

        Assert.That(FindTriple(roundTrip,
            OdfNamespaces.DublinCore + "title", "Updated graph title").Language,
            Is.EqualTo("en"));
        Assert.That(FindTriple(roundTrip,
            "urn:example:related", "urn:new-resource").IsLiteral, Is.False);
        Assert.That(FindTriple(roundTrip,
            OdfNamespaces.PackageMetadata + "hasPart", "styles.xml").SourceKind,
            Is.EqualTo(OdfRdfSourceKind.PackageMetadata));
        Assert.That(ContainsTriple(roundTrip,
            OdfNamespaces.Rdf + "type", "urn:example:Thing"), Is.False);

        string graphXml = ReadPackageText(
            roundTrip, "Metadata/graph.rdf");
        Assert.That(graphXml, Does.Contain("foreign:tag=\"preserved\""));
        Assert.That(graphXml, Does.Contain("foreign graph comment"));

        byte[] secondBytes = writer.WriteDocumentBytes(
            document, new OdfSaveOptions());
        OdfDocument second = new OdfDocumentReader().ReadDocumentBytes(
            secondBytes, "second.odt", new OdfLoadOptions());
        OdfRdfTriple[] addedParts = RdfXmlDocumentModel.QueryTriples(
            second,
            OdfPartNames.MetadataManifest,
            OdfNamespaces.PackageMetadata + "hasPart",
            "styles.xml");
        Assert.That(addedParts, Has.Length.EqualTo(1));
    }

    [Test]
    public void CreatesANewRdfXmlPartAndManifestEntry()
    {
        OdfDocument document = new OdfDocumentFactory().CreatePackageDocument(
            OdfDocumentKind.Text);
        OdfRdfTriple triple = new OdfRdfTriple
        {
            SourceKind = OdfRdfSourceKind.RdfXml,
            PartPath = "Metadata/new.rdf",
            Subject = "urn:new-item",
            Predicate = "urn:created:name",
            Object = "Created graph",
            IsLiteral = true
        };
        document.RdfTriples = new[] { triple };

        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document, new OdfSaveOptions());
        OdfDocument roundTrip = new OdfDocumentReader().ReadDocumentBytes(
            bytes, "created.odt", new OdfLoadOptions());

        OdfRdfTriple loaded = FindTriple(
            roundTrip, "urn:created:name", "Created graph");
        Assert.That(loaded.Subject, Is.EqualTo("urn:new-item"));
        Assert.That(loaded.PartPath, Is.EqualTo("Metadata/new.rdf"));
        Assert.That(FindManifestEntry(
            roundTrip.Manifest, "Metadata/new.rdf").MediaType,
            Is.EqualTo(OdfMediaTypes.RdfXml));
    }

    static OdfDocument ReadRdfPackage()
    {
        OdfDocument skeleton = new OdfDocumentFactory().CreatePackageDocument(
            OdfDocumentKind.Text);
        OdfPackage? package = skeleton.Package;
        Assert.That(package, Is.Not.Null);
        OdfPackageWriter writer = new OdfPackageWriter();
        OdfEditResult manifestEdit = writer.AddPackageEntry(
            package,
            OdfPartNames.MetadataManifest,
            Encoding.UTF8.GetBytes(PackageManifestRdf()),
            OdfCompressionMethod.Deflated);
        OdfEditResult graphEdit = writer.AddPackageEntry(
            package,
            "Metadata/graph.rdf",
            Encoding.UTF8.GetBytes(GraphRdf()),
            OdfCompressionMethod.Deflated);
        Assert.That(manifestEdit.Status, Is.EqualTo(OdfEditStatus.Success));
        Assert.That(graphEdit.Status, Is.EqualTo(OdfEditStatus.Success));
        byte[] bytes = writer.WritePackageBytes(package, new OdfSaveOptions());
        return new OdfDocumentReader().ReadDocumentBytes(
            bytes, "graph.odt", new OdfLoadOptions());
    }

    static OdfRdfTriple FindTriple(
        OdfDocument document,
        string predicate,
        string objectValue)
    {
        int index = 0;
        while (index < document.RdfTriples.Length)
        {
            OdfRdfTriple triple = document.RdfTriples[index];
            if (triple.Predicate == predicate && triple.Object == objectValue)
            {
                return triple;
            }
            index = index + 1;
        }
        Assert.Fail("The expected RDF triple was not found.");
        return new OdfRdfTriple();
    }

    static bool ContainsTriple(
        OdfDocument document,
        string predicate,
        string objectValue)
    {
        int index = 0;
        while (index < document.RdfTriples.Length)
        {
            if (document.RdfTriples[index].Predicate == predicate
                && document.RdfTriples[index].Object == objectValue)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfRdfTriple[] RemoveAndAppend(
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

    static string ReadPackageText(OdfDocument document, string path)
    {
        OdfPackage? package = document.Package;
        Assert.That(package, Is.Not.Null);
        using (Stream input = new OdfPackageReader().OpenPackageEntry(
            package, path))
        using (StreamReader reader = new StreamReader(input, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }

    static OdfManifestEntry FindManifestEntry(
        OdfManifest? manifest,
        string path)
    {
        Assert.That(manifest, Is.Not.Null);
        int index = 0;
        while (index < manifest.Entries.Length)
        {
            if (manifest.Entries[index].FullPath == path)
            {
                return manifest.Entries[index];
            }
            index = index + 1;
        }
        Assert.Fail("The RDF manifest entry was not found.");
        return new OdfManifestEntry();
    }

    static string PackageManifestRdf()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<rdf:RDF xmlns:rdf='" + OdfNamespaces.Rdf
            + "' xmlns:pkg='" + OdfNamespaces.PackageMetadata + "'>"
            + "<rdf:Description rdf:about=''>"
            + "<pkg:hasPart rdf:resource='content.xml'/>"
            + "</rdf:Description></rdf:RDF>";
    }

    static string GraphRdf()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<rdf:RDF xmlns:rdf='" + OdfNamespaces.Rdf
            + "' xmlns:dc='" + OdfNamespaces.DublinCore
            + "' xmlns:ex='urn:example:' xmlns:foreign='urn:foreign:'>"
            + "<!--foreign graph comment-->"
            + "<rdf:Description rdf:about='urn:item' "
            + "foreign:tag='preserved'>"
            + "<dc:title xml:lang='en'>Graph title</dc:title>"
            + "<ex:related rdf:resource='urn:other'/>"
            + "</rdf:Description>"
            + "<ex:Thing rdf:about='urn:typed'/>"
            + "</rdf:RDF>";
    }
}
