using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxPackageRelationshipApiTests
{
    [Test]
    public void EnumerationLookupAndResolution_SupportPackageAndNormalizedPartSources()
    {
        global::docx.IDocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        global::docx.IDocxPackageRelationships relationships = package;

        List<global::docx.DocxRelationship> packageRelationships = relationships.GetRelationships("/");
        global::docx.DocxRelationship? mainDocument = relationships.FindRelationship("/", "rId1");

        packageRelationships.Clear();

        Assert.That(mainDocument, Is.Not.Null);
        Assert.That(mainDocument!.Type, Is.EqualTo(global::docx.DocxConstants.RelationshipTypeOfficeDocument));
        Assert.That(relationships.ResolveRelationshipTarget("/", "rId1"), Is.EqualTo("/word/document.xml"));
        Assert.That(relationships.GetRelationships("/"), Has.Count.EqualTo(1));
        Assert.That(relationships.GetRelationships("word\\document.xml"), Is.Empty);
        Assert.That(relationships.FindRelationship("word/document.xml", "missing"), Is.Null);
        Assert.That(relationships.ResolveRelationshipTarget("word/document.xml", "missing"), Is.EqualTo(""));
        Assert.That(package.FindRelationshipSet("/"), Is.SameAs(package.GetPackageRelationshipSet()));
    }

    [Test]
    public void AllocationAndCreation_CreateRootAndPartRelationshipFiles()
    {
        global::docx.IDocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        global::docx.IDocxPackageRelationships relationships = package;
        global::docx.IDocxPackageParts parts = package;

        Assert.That(relationships.AllocateRelationshipId("/"), Is.EqualTo("rId2"));
        Assert.That(relationships.AllocateRelationshipId("word/document.xml"), Is.EqualTo("rId1"));

        global::docx.DocxRelationship external = relationships.CreateRelationship(
            "/",
            "urn:test:external",
            "https://example.com/root?q=1#value",
            true);
        global::docx.DocxRelationship explicitRelationship = relationships.CreateRelationshipWithId(
            "/",
            "rId5",
            "urn:test:custom",
            "custom/item.bin",
            false);

        parts.AddBinaryPart("/word/media/image1.bin", "application/octet-stream", new byte[] { 1 });

        global::docx.DocxRelationship image = relationships.CreateRelationshipWithId(
            "word\\document.xml",
            "rId3",
            global::docx.DocxConstants.RelationshipTypeImage,
            "media/image1.bin",
            false);
        global::docx.DocxRelationship automatic = relationships.CreateRelationship(
            "/word/document.xml",
            "urn:test:second",
            "media/image1.bin",
            false);

        Assert.That(external.Id, Is.EqualTo("rId2"));
        Assert.That(external.Target, Is.EqualTo("https://example.com/root?q=1#value"));
        Assert.That(external.TargetMode, Is.EqualTo("External"));
        Assert.That(explicitRelationship.Id, Is.EqualTo("rId5"));
        Assert.That(relationships.AllocateRelationshipId("/"), Is.EqualTo("rId3"));
        Assert.That(image.Id, Is.EqualTo("rId3"));
        Assert.That(automatic.Id, Is.EqualTo("rId1"));
        Assert.That(relationships.ResolveRelationshipTarget("word/document.xml", "rId3"), Is.EqualTo("/word/media/image1.bin"));
        Assert.That(package.ContainsPart("/_rels/.rels"), Is.True);
        Assert.That(package.ContainsPart("/word/_rels/document.xml.rels"), Is.True);
        Assert.That(relationships.GetRelationships("/word/document.xml"), Has.Count.EqualTo(2));

        global::docx.DocxPackage emptyPackage = new global::docx.DocxPackage();
        global::docx.IDocxPackageRelationships emptyRelationships = emptyPackage;
        global::docx.DocxRelationship firstRootRelationship = emptyRelationships.CreateRelationship(
            "/",
            "urn:test:first",
            "custom/first.bin",
            false);

        Assert.That(firstRootRelationship.Id, Is.EqualTo("rId1"));
        Assert.That(emptyPackage.ContainsPart("/_rels/.rels"), Is.True);
        Assert.That(emptyRelationships.ResolveRelationshipTarget("/", "rId1"), Is.EqualTo("/custom/first.bin"));
    }

    [Test]
    public void UpdateAndRemove_SynchronizeRelationshipPartContent()
    {
        global::docx.IDocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        global::docx.IDocxPackageRelationships relationships = package;
        global::docx.IDocxPackageParts parts = package;

        global::docx.DocxRelationship relationship = relationships.CreateRelationship(
            "/word/document.xml",
            global::docx.DocxConstants.RelationshipTypeImage,
            "media/image1.png",
            false);

        Assert.That(relationships.UpdateRelationship(
            "word/document.xml",
            relationship.Id,
            global::docx.DocxConstants.RelationshipTypeHyperlink,
            "https://example.com/exact?x=1#fragment",
            true), Is.True);
        Assert.That(relationships.UpdateRelationship(
            "/word/document.xml",
            "missing",
            "urn:test:missing",
            "missing.bin",
            false), Is.False);

        string updatedXml = parts.GetPartXml("/word/_rels/document.xml.rels");

        Assert.That(relationship.Type, Is.EqualTo(global::docx.DocxConstants.RelationshipTypeHyperlink));
        Assert.That(relationship.Target, Is.EqualTo("https://example.com/exact?x=1#fragment"));
        Assert.That(relationship.IsExternal, Is.True);
        Assert.That(updatedXml, Does.Contain("https://example.com/exact?x=1#fragment"));
        Assert.That(updatedXml, Does.Contain("TargetMode=\"External\""));
        Assert.Throws<InvalidOperationException>(() => relationships.ResolveRelationshipTarget("/word/document.xml", relationship.Id));

        Assert.That(relationships.RemoveRelationship("word\\document.xml", relationship.Id), Is.True);
        Assert.That(relationships.RemoveRelationship("/word/document.xml", relationship.Id), Is.False);
        Assert.That(relationships.FindRelationship("/word/document.xml", relationship.Id), Is.Null);
        Assert.That(parts.GetPartXml("/word/_rels/document.xml.rels"), Does.Not.Contain(relationship.Id));
    }

    [Test]
    public void ExternalRelationshipTarget_RoundTripsWithoutNormalization()
    {
        global::docx.IDocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        global::docx.IDocxPackageRelationships relationships = package;
        global::docx.DocxSaveOptions saveOptions = new global::docx.DocxSaveOptions();
        global::docx.DocxLoadOptions loadOptions = new global::docx.DocxLoadOptions();
        MemoryStream stream = new MemoryStream();
        string target = "HTTPS://Example.com/A/../B?q=One%20Two#Exact";

        global::docx.DocxRelationship created = relationships.CreateRelationship(
            "/word/document.xml",
            global::docx.DocxConstants.RelationshipTypeHyperlink,
            target,
            true);

        saveOptions.Deterministic = true;
        loadOptions.LazyLoadXmlParts = false;
        loadOptions.LazyLoadBinaryParts = false;

        persistence.SavePackage(package, stream, saveOptions);
        stream.Position = 0;

        global::docx.DocxPackage reopenedPackage = persistence.OpenPackage(stream, loadOptions);
        global::docx.IDocxPackageRelationships reopened = reopenedPackage;
        global::docx.DocxRelationship? roundTripped = reopened.FindRelationship("/word/document.xml", created.Id);

        Assert.That(roundTripped, Is.Not.Null);
        Assert.That(roundTripped!.Target, Is.EqualTo(target));
        Assert.That(roundTripped.TargetMode, Is.EqualTo("External"));
        Assert.That(roundTripped.IsExternal, Is.True);
    }

    [Test]
    public void OrphanInspection_ReportsOnlyMissingInternalTargetsWithSourceContext()
    {
        global::docx.IDocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        global::docx.IDocxPackageRelationships relationships = package;
        global::docx.IDocxPackageParts parts = package;

        global::docx.DocxRelationship rootOrphan = relationships.CreateRelationship(
            "/",
            "urn:test:root-missing",
            "missing/root.xml",
            false);
        global::docx.DocxRelationship partOrphan = relationships.CreateRelationship(
            "/word/document.xml",
            global::docx.DocxConstants.RelationshipTypeImage,
            "media/missing.png",
            false);
        global::docx.DocxRelationship external = relationships.CreateRelationship(
            "/word/document.xml",
            global::docx.DocxConstants.RelationshipTypeHyperlink,
            "https://example.com/missing",
            true);

        List<global::docx.DocxOrphanRelationship> orphans = relationships.GetOrphanRelationships();

        Assert.That(orphans, Has.Count.EqualTo(2));
        Assert.That(orphans[0].SourcePartName, Is.EqualTo("/"));
        Assert.That(orphans[0].Relationship, Is.SameAs(rootOrphan));
        Assert.That(orphans[0].TargetPartName, Is.EqualTo("/missing/root.xml"));
        Assert.That(orphans[1].SourcePartName, Is.EqualTo("/word/document.xml"));
        Assert.That(orphans[1].Relationship, Is.SameAs(partOrphan));
        Assert.That(orphans[1].TargetPartName, Is.EqualTo("/word/media/missing.png"));
        Assert.That(relationships.IsOrphanRelationship("/", rootOrphan.Id), Is.True);
        Assert.That(relationships.IsOrphanRelationship("word/document.xml", partOrphan.Id), Is.True);
        Assert.That(relationships.IsOrphanRelationship("/word/document.xml", external.Id), Is.False);
        Assert.That(relationships.IsOrphanRelationship("/word/document.xml", "missing"), Is.False);

        orphans.Clear();

        Assert.That(relationships.GetOrphanRelationships(), Has.Count.EqualTo(2));

        parts.AddXmlPartWithContent("/missing/root.xml", global::docx.DocxConstants.ContentTypeXml, "<root />");
        parts.AddBinaryPart("/word/media/missing.png", global::docx.DocxConstants.ContentTypePng, new byte[] { 1 });

        Assert.That(relationships.GetOrphanRelationships(), Is.Empty);
    }

    [Test]
    public void RelationshipApi_RejectsInvalidArgumentsAndDuplicateIds()
    {
        global::docx.IDocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        global::docx.IDocxPackageRelationships relationships = package;

        Assert.Throws<ArgumentNullException>(() => relationships.GetRelationships(null!));
        Assert.Throws<ArgumentException>(() => relationships.GetRelationships("/word/../document.xml"));
        Assert.Throws<ArgumentNullException>(() => relationships.FindRelationship("/", null!));
        Assert.Throws<ArgumentNullException>(() => relationships.ResolveRelationshipTarget("/", null!));
        Assert.Throws<ArgumentNullException>(() => relationships.AllocateRelationshipId(null!));
        Assert.Throws<KeyNotFoundException>(() => relationships.AllocateRelationshipId("/word/missing.xml"));
        Assert.Throws<KeyNotFoundException>(() => relationships.CreateRelationship("/word/missing.xml", "urn:test", "target.bin", false));
        Assert.Throws<ArgumentNullException>(() => relationships.CreateRelationship("/", null!, "target.bin", false));
        Assert.Throws<ArgumentNullException>(() => relationships.CreateRelationship("/", "urn:test", null!, false));
        Assert.Throws<ArgumentException>(() => relationships.CreateRelationship("/", "", "target.bin", false));
        Assert.Throws<ArgumentException>(() => relationships.CreateRelationship("/", "urn:test", "", false));
        Assert.Throws<ArgumentException>(() => relationships.CreateRelationship("/word/document.xml", "urn:test", "../../outside.bin", false));
        Assert.Throws<ArgumentNullException>(() => relationships.CreateRelationshipWithId("/", null!, "urn:test", "target.bin", false));
        Assert.Throws<ArgumentException>(() => relationships.CreateRelationshipWithId("/", "", "urn:test", "target.bin", false));
        Assert.Throws<InvalidOperationException>(() => relationships.CreateRelationshipWithId(
            "/",
            "rId1",
            "urn:test:duplicate",
            "duplicate.bin",
            false));
        Assert.Throws<ArgumentNullException>(() => relationships.UpdateRelationship("/", null!, "urn:test", "target.bin", false));
        Assert.Throws<ArgumentNullException>(() => relationships.UpdateRelationship("/", "missing", null!, "target.bin", false));
        Assert.Throws<KeyNotFoundException>(() => relationships.UpdateRelationship("/word/missing.xml", "rId1", "urn:test", "target.bin", false));
        Assert.Throws<ArgumentNullException>(() => relationships.RemoveRelationship("/", null!));
        Assert.Throws<ArgumentNullException>(() => relationships.IsOrphanRelationship("/", null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxOrphanRelationship(null!, new global::docx.DocxRelationship("rId1", "urn:test", "target.bin", ""), "/target.bin"));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxOrphanRelationship("/", null!, "/target.bin"));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxOrphanRelationship("/", new global::docx.DocxRelationship("rId1", "urn:test", "target.bin", ""), null!));
    }
}
