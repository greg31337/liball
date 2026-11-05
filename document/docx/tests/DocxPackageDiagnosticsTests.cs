using System.IO.Compression;
using System.Text;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxPackageDiagnosticsTests
{
    [Test]
    public void PackageDiagnostics_ExposeCleanPackageCompatibilityState()
    {
        global::docx.IDocxValidator validator = new global::docx.DocxValidator();
        global::docx.DocxPackage package = new global::docx.DocxPackagePersistence().CreatePackage();

        global::docx.DocxPackageDiagnostics diagnostics = validator.GetPackageDiagnostics(package);

        Assert.That(diagnostics.IsReadablePackage, Is.True);
        Assert.That(diagnostics.HasCorruptInput, Is.False);
        Assert.That(diagnostics.HasDuplicateZipEntries, Is.False);
        Assert.That(diagnostics.HasMissingRequiredItems, Is.False);
        Assert.That(diagnostics.HasMacroEnabledContent, Is.False);
        Assert.That(diagnostics.NamespaceConformance, Is.EqualTo(global::docx.DocxNamespaceConformance.Transitional));
        Assert.That(diagnostics.HasRecoverableDefects, Is.False);
        Assert.That(diagnostics.Report.Errors, Is.Empty);
        Assert.That(diagnostics.RecoverableDefects.Errors, Is.Empty);
    }

    [Test]
    public void StreamDiagnostics_ReportDuplicateEntriesAndMissingRequiredItems()
    {
        global::docx.IDocxValidator validator = new global::docx.DocxValidator();
        MemoryStream stream = CreateIncompleteZipWithDuplicateEntries();

        global::docx.DocxPackageDiagnostics diagnostics = validator.GetStreamDiagnostics(stream);

        Assert.That(diagnostics.IsReadablePackage, Is.True);
        Assert.That(diagnostics.HasDuplicateZipEntries, Is.True);
        Assert.That(diagnostics.HasMissingRequiredItems, Is.True);
        Assert.That(diagnostics.HasCorruptInput, Is.False);
        Assert.That(diagnostics.Report.Errors.Any(error => error.Code == "opc.duplicate-zip-entry"), Is.True);
        Assert.That(diagnostics.Report.Errors.Any(error => error.Code == "opc.missing-content-types"), Is.True);
        Assert.That(diagnostics.Report.Errors.Any(error => error.Code == "opc.missing-package-relationships"), Is.True);
    }

    [Test]
    public void StreamDiagnostics_ReturnCorruptInputInsteadOfThrowing()
    {
        global::docx.IDocxValidator validator = new global::docx.DocxValidator();
        MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("not a zip package"));

        global::docx.DocxPackageDiagnostics diagnostics = validator.GetStreamDiagnostics(stream);

        Assert.That(diagnostics.IsReadablePackage, Is.False);
        Assert.That(diagnostics.HasCorruptInput, Is.True);
        Assert.That(diagnostics.Report.Errors.Any(
            error => error.Code == global::docx.DocxDiagnosticCodes.CorruptInput), Is.True);
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void StreamDiagnostics_ReportCorruptCatalogInsideReadableZip()
    {
        global::docx.IDocxValidator validator = new global::docx.DocxValidator();
        MemoryStream stream = new MemoryStream();

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddTextEntry(archive, "[Content_Types].xml", "<Types>");
            AddTextEntry(
                archive,
                "_rels/.rels",
                "<Relationships xmlns=\"" + global::docx.DocxConstants.NamespacePackageRelationships + "\" />");
        }

        stream.Position = 0;

        global::docx.DocxPackageDiagnostics diagnostics = validator.GetStreamDiagnostics(stream);

        Assert.That(diagnostics.IsReadablePackage, Is.False);
        Assert.That(diagnostics.HasCorruptInput, Is.True);
        Assert.That(diagnostics.Report.Errors.Any(
            error => error.Code == global::docx.DocxDiagnosticCodes.CorruptInput), Is.True);
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void PackageDiagnostics_DetectMacroContentWithoutChangingUnknownOrLazyParts()
    {
        global::docx.DocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage source = persistence.CreatePackage();
        source.ReplaceXmlPart(
            "/word/document.xml",
            global::docx.DocxConstants.ContentTypeMacroEnabledMainDocument,
            global::docx.DocxPackage.CreateMinimalDocumentXml());
        source.AddBinaryPart(
            "/word/vbaProject.bin",
            global::docx.DocxConstants.ContentTypeVbaProject,
            new byte[] { 7, 8, 9 });
        source.CreateRelationship(
            "/word/document.xml",
            global::docx.DocxConstants.RelationshipTypeVbaProject,
            "vbaProject.bin",
            false);
        source.AddBinaryPart("/custom/unknown.bin", "application/x-unknown", new byte[] { 1, 3, 5 });
        MemoryStream stream = new MemoryStream();
        persistence.SavePackage(source, stream, new global::docx.DocxSaveOptions());
        stream.Position = 0;
        global::docx.DocxLoadOptions loadOptions = new global::docx.DocxLoadOptions();
        global::docx.DocxPackage package = persistence.OpenPackage(stream, loadOptions);
        global::docx.DocxPart mainPart = package.FindPart("/word/document.xml")!;
        global::docx.DocxPart unknownPart = package.FindPart("/custom/unknown.bin")!;
        int partCount = package.PartCount();

        global::docx.DocxPackageDiagnostics diagnostics = new global::docx.DocxValidator().GetPackageDiagnostics(package);

        Assert.That(diagnostics.HasMacroEnabledContent, Is.True);
        Assert.That(diagnostics.NamespaceConformance, Is.EqualTo(global::docx.DocxNamespaceConformance.Transitional));
        Assert.That(package.PartCount(), Is.EqualTo(partCount));
        Assert.That(package.ContainsPart("/custom/unknown.bin"), Is.True);
        Assert.That(mainPart.IsLazy, Is.True);
        Assert.That(unknownPart.IsLazy, Is.True);
        Assert.That(package.GetPartBytes("/custom/unknown.bin"), Is.EqualTo(new byte[] { 1, 3, 5 }));
    }

    [Test]
    public void PackageDiagnostics_DescribeRecoverableDefectsWithoutRepairingOriginal()
    {
        global::docx.IDocxValidator validator = new global::docx.DocxValidator();
        global::docx.DocxPackage package = new global::docx.DocxPackage();

        global::docx.DocxPackageDiagnostics diagnostics = validator.GetPackageDiagnostics(package);

        Assert.That(diagnostics.HasMissingRequiredItems, Is.True);
        Assert.That(diagnostics.HasRecoverableDefects, Is.True);
        Assert.That(diagnostics.RecoverableDefects.Errors, Is.Not.Empty);
        Assert.That(package.PartCount(), Is.EqualTo(0));
        Assert.That(package.RelationshipSetCount(), Is.EqualTo(0));
        Assert.That(package.GetContentTypeDefaults(), Is.Empty);
        Assert.That(package.GetContentTypeOverrides(), Is.Empty);
    }

    [Test]
    public void PackageDiagnostics_ValidateMixedCaseXmlMediaTypeSuffixes()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackagePersistence().CreatePackage();
        package.AddXmlPartWithContent(
            "/custom/malformed.data",
            "APPLICATION/VND.EXAMPLE+XML",
            "<malformed>");

        global::docx.DocxPackageDiagnostics diagnostics = new global::docx.DocxValidator().GetPackageDiagnostics(package);

        Assert.That(diagnostics.Report.Errors.Any(
            error => error.Code == "xml.invalid-part" && error.Location == "/custom/malformed.data"), Is.True);
    }

    [Test]
    public void StreamDiagnostics_PreserveSeekPositionAndUnknownEntryBytes()
    {
        global::docx.DocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        package.AddBinaryPart("/custom/unknown.bin", "application/x-unknown", new byte[] { 2, 4, 6, 8 });
        MemoryStream stream = new MemoryStream();
        persistence.SavePackage(package, stream, new global::docx.DocxSaveOptions());
        stream.Position = 0;

        global::docx.DocxPackageDiagnostics diagnostics = new global::docx.DocxValidator().GetStreamDiagnostics(stream);

        Assert.That(diagnostics.IsReadablePackage, Is.True);
        Assert.That(stream.Position, Is.EqualTo(0));

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            ZipArchiveEntry entry = archive.GetEntry("custom/unknown.bin")!;

            using (Stream entryStream = entry.Open())
            {
                using (MemoryStream bytes = new MemoryStream())
                {
                    entryStream.CopyTo(bytes);
                    Assert.That(bytes.ToArray(), Is.EqualTo(new byte[] { 2, 4, 6, 8 }));
                }
            }
        }
    }

    [Test]
    public void Diagnostics_RejectNullInputs()
    {
        global::docx.IDocxValidator validator = new global::docx.DocxValidator();

        Assert.Throws<ArgumentNullException>(() => validator.GetStreamDiagnostics(null!));
        Assert.Throws<ArgumentNullException>(() => validator.GetPackageDiagnostics(null!));
    }

    static MemoryStream CreateIncompleteZipWithDuplicateEntries()
    {
        MemoryStream stream = new MemoryStream();

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddTextEntry(archive, "word/document.xml", "first");
            AddTextEntry(archive, "word/document.xml", "second");
        }

        stream.Position = 0;
        return stream;
    }

    static void AddTextEntry(ZipArchive archive, string name, string value)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name);

        using (Stream stream = entry.Open())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
