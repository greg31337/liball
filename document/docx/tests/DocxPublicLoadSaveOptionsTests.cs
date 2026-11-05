using System.IO.Compression;
using System.Text;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxPublicLoadSaveOptionsTests
{
    [Test]
    public void LoadOptions_CanNormalizeWhitespaceAndDiscardUnsupportedMarkup()
    {
        global::docx.DocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        string xml = "<w:document xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\" xmlns:x=\"urn:example:future\">\n  <w:body>\n    <x:future />\n    <w:p><w:r><w:t>kept</w:t></w:r></w:p>\n  </w:body>\n</w:document>";
        package.ReplaceXmlPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, xml);
        MemoryStream stream = SavePackage(persistence, package, new global::docx.DocxSaveOptions());
        global::docx.DocxLoadOptions options = new global::docx.DocxLoadOptions();
        options.PreserveXmlWhitespace = false;
        options.PreserveUnknownMarkup = false;
        options.LazyLoadXmlParts = false;
        options.LazyLoadBinaryParts = false;

        global::docx.DocxPackage opened = persistence.OpenPackage(stream, options);
        string openedXml = opened.GetPartXml("/word/document.xml");

        Assert.That(openedXml, Does.Contain("kept"));
        Assert.That(openedXml, Does.Not.Contain("future"));
        Assert.That(openedXml, Does.Not.Contain("\n  "));
    }

    [Test]
    public void LoadOptions_CanDiscardUnsupportedPartsAndRelationships()
    {
        global::docx.DocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        package.AddBinaryPart("/custom/payload.bin", "application/x-example", new byte[] { 1, 2, 3 });
        global::docx.DocxRelationship relationship = package.CreateRelationship(
            "/",
            "urn:example:relationship",
            "custom/payload.bin",
            false);
        MemoryStream stream = SavePackage(persistence, package, new global::docx.DocxSaveOptions());
        global::docx.DocxLoadOptions options = new global::docx.DocxLoadOptions();
        options.PreserveUnknownParts = false;
        options.PreserveUnknownRelationships = false;
        options.LazyLoadXmlParts = false;
        options.LazyLoadBinaryParts = false;

        global::docx.DocxPackage opened = persistence.OpenPackage(stream, options);

        Assert.That(opened.ContainsPart("/custom/payload.bin"), Is.False);
        Assert.That(opened.FindRelationship("/", relationship.Id), Is.Null);
    }

    [Test]
    public void SaveOptions_CanDiscardUnsupportedPackageDataWithoutMutatingSource()
    {
        global::docx.DocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        package.AddBinaryPart("/custom/payload.bin", "application/x-example", new byte[] { 4, 5, 6 });
        package.CreateRelationship("/", "urn:example:relationship", "custom/payload.bin", false);
        global::docx.DocxSaveOptions saveOptions = new global::docx.DocxSaveOptions();
        saveOptions.PreserveUnknownParts = false;
        saveOptions.PreserveUnknownRelationships = false;
        MemoryStream stream = SavePackage(persistence, package, saveOptions);

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            Assert.That(archive.GetEntry("custom/payload.bin"), Is.Null);
            string relationshipsXml = ReadEntryText(archive, "_rels/.rels");
            string contentTypesXml = ReadEntryText(archive, "[Content_Types].xml");
            Assert.That(relationshipsXml, Does.Not.Contain("urn:example:relationship"));
            Assert.That(contentTypesXml, Does.Not.Contain("/custom/payload.bin"));
        }

        Assert.That(package.ContainsPart("/custom/payload.bin"), Is.True);
    }

    [Test]
    public void SaveOptions_CanNormalizeWhitespaceAndDiscardUnsupportedMarkup()
    {
        global::docx.DocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        string xml = "<w:document xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\" xmlns:x=\"urn:example:future\">\n  <w:body><x:future /><w:p /></w:body>\n</w:document>";
        package.ReplaceXmlPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, xml);
        global::docx.DocxSaveOptions options = new global::docx.DocxSaveOptions();
        options.PreserveXmlWhitespace = false;
        options.PreserveUnknownMarkup = false;
        MemoryStream stream = SavePackage(persistence, package, options);

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            string savedXml = ReadEntryText(archive, "word/document.xml");
            Assert.That(savedXml, Does.Not.Contain("future"));
            Assert.That(savedXml, Does.Not.Contain("\n  "));
        }

        Assert.That(package.GetPartXml("/word/document.xml"), Does.Contain("x:future"));
    }

    [Test]
    public void LoadOptions_ApplyRepairBeforeRequestedValidation()
    {
        global::docx.DocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage incomplete = new global::docx.DocxPackage();
        MemoryStream stream = SavePackage(persistence, incomplete, new global::docx.DocxSaveOptions());
        global::docx.DocxLoadOptions options = new global::docx.DocxLoadOptions();
        options.RepairMode = global::docx.DocxRepairMode.Safe;
        options.ValidationMode = global::docx.DocxValidationMode.Full;
        options.LazyLoadXmlParts = false;
        options.LazyLoadBinaryParts = false;

        global::docx.DocxPackage opened = persistence.OpenPackage(stream, options);

        Assert.That(options.RepairReport, Is.Not.Null);
        Assert.That(options.RepairReport!.Errors, Is.Not.Empty);
        Assert.That(options.ValidationReport, Is.Not.Null);
        Assert.That(opened.ContainsPart("/word/document.xml"), Is.True);
        Assert.That(opened.GetPackageRelationshipSet(), Is.Not.Null);
    }

    [Test]
    public void SaveOptions_CanEmitTemplateAndDocumentMainPartTypes()
    {
        global::docx.DocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        global::docx.DocxSaveOptions templateOptions = new global::docx.DocxSaveOptions();
        templateOptions.OutputType = global::docx.DocxOutputType.Template;
        MemoryStream templateStream = SavePackage(persistence, package, templateOptions);
        global::docx.DocxPackage template = persistence.OpenPackage(templateStream, new global::docx.DocxLoadOptions());

        Assert.That(
            template.ResolveContentType("/word/document.xml"),
            Is.EqualTo(global::docx.DocxConstants.ContentTypeTemplateMainDocument));

        global::docx.DocxSaveOptions documentOptions = new global::docx.DocxSaveOptions();
        documentOptions.OutputType = global::docx.DocxOutputType.Document;
        MemoryStream documentStream = SavePackage(persistence, template, documentOptions);
        global::docx.DocxPackage document = persistence.OpenPackage(documentStream, new global::docx.DocxLoadOptions());

        Assert.That(
            document.ResolveContentType("/word/document.xml"),
            Is.EqualTo(global::docx.DocxConstants.ContentTypeMainDocument));
    }

    [Test]
    public void LoadOptions_RejectInvalidEnumsBeforeReadingTheInputStream()
    {
        global::docx.DocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        MemoryStream stream = new MemoryStream();
        global::docx.DocxLoadOptions validationOptions = new global::docx.DocxLoadOptions();
        validationOptions.ValidationMode = (global::docx.DocxValidationMode)99;

        ArgumentOutOfRangeException validationError = Assert.Throws<ArgumentOutOfRangeException>(
            () => persistence.OpenPackage(stream, validationOptions))!;

        Assert.That(validationError.ParamName, Is.EqualTo("ValidationMode"));
        Assert.That(stream.Position, Is.EqualTo(0));

        global::docx.DocxLoadOptions repairOptions = new global::docx.DocxLoadOptions();
        repairOptions.RepairMode = (global::docx.DocxRepairMode)99;
        ArgumentOutOfRangeException repairError = Assert.Throws<ArgumentOutOfRangeException>(
            () => persistence.OpenPackage(stream, repairOptions))!;

        Assert.That(repairError.ParamName, Is.EqualTo("RepairMode"));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void SaveOptions_RejectInvalidEnumsBeforeWritingTheOutputStream()
    {
        global::docx.DocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        global::docx.DocxSaveOptions modeOptions = new global::docx.DocxSaveOptions();
        modeOptions.Mode = (global::docx.DocxSaveMode)99;
        MemoryStream modeStream = new MemoryStream();

        ArgumentOutOfRangeException modeError = Assert.Throws<ArgumentOutOfRangeException>(
            () => persistence.SavePackage(package, modeStream, modeOptions))!;

        Assert.That(modeError.ParamName, Is.EqualTo("Mode"));
        Assert.That(modeStream.Length, Is.EqualTo(0));

        global::docx.DocxSaveOptions compressionOptions = new global::docx.DocxSaveOptions();
        compressionOptions.CompressionMode = (global::docx.DocxCompressionMode)99;
        MemoryStream compressionStream = new MemoryStream();
        ArgumentOutOfRangeException compressionError = Assert.Throws<ArgumentOutOfRangeException>(
            () => persistence.SavePackage(package, compressionStream, compressionOptions))!;

        Assert.That(compressionError.ParamName, Is.EqualTo("CompressionMode"));
        Assert.That(compressionStream.Length, Is.EqualTo(0));

        global::docx.DocxSaveOptions outputOptions = new global::docx.DocxSaveOptions();
        outputOptions.OutputType = (global::docx.DocxOutputType)99;
        MemoryStream outputStream = new MemoryStream();
        ArgumentOutOfRangeException outputError = Assert.Throws<ArgumentOutOfRangeException>(
            () => persistence.SavePackage(package, outputStream, outputOptions))!;

        Assert.That(outputError.ParamName, Is.EqualTo("OutputType"));
        Assert.That(outputStream.Length, Is.EqualTo(0));
    }

    [Test]
    public void PackagePersistence_AcceptsNonSeekableAndReadOnlyInputStreams()
    {
        global::docx.DocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        MemoryStream saved = SavePackage(persistence, persistence.CreatePackage(), new global::docx.DocxSaveOptions());
        byte[] bytes = saved.ToArray();
        NonSeekableMemoryStream nonSeekable = new NonSeekableMemoryStream(bytes);
        MemoryStream readOnly = new MemoryStream(bytes, false);

        global::docx.DocxPackage nonSeekablePackage = persistence.OpenPackage(nonSeekable, new global::docx.DocxLoadOptions());
        global::docx.DocxPackage readOnlyPackage = persistence.OpenPackage(readOnly, new global::docx.DocxLoadOptions());

        Assert.That(nonSeekablePackage.ContainsPart("/word/document.xml"), Is.True);
        Assert.That(readOnlyPackage.ContainsPart("/word/document.xml"), Is.True);
    }

    [Test]
    public void PackagePersistence_ReportsReadOnlyAndDisposedStreamFailures()
    {
        global::docx.DocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        MemoryStream saved = SavePackage(persistence, package, new global::docx.DocxSaveOptions());
        MemoryStream readOnlyOutput = new MemoryStream(new byte[0], false);
        MemoryStream disposedInput = new MemoryStream(saved.ToArray());
        MemoryStream disposedOutput = new MemoryStream();
        disposedInput.Dispose();
        disposedOutput.Dispose();

        Assert.Throws<NotSupportedException>(
            () => persistence.SavePackage(package, readOnlyOutput, new global::docx.DocxSaveOptions()));
        Assert.Throws<ObjectDisposedException>(
            () => persistence.OpenPackage(disposedInput, new global::docx.DocxLoadOptions()));
        Assert.Throws<ObjectDisposedException>(
            () => persistence.SavePackage(package, disposedOutput, new global::docx.DocxSaveOptions()));
    }

    static MemoryStream SavePackage(
        global::docx.DocxPackagePersistence persistence,
        global::docx.DocxPackage package,
        global::docx.DocxSaveOptions options)
    {
        MemoryStream stream = new MemoryStream();
        persistence.SavePackage(package, stream, options);
        stream.Position = 0;
        return stream;
    }

    static string ReadEntryText(ZipArchive archive, string entryName)
    {
        ZipArchiveEntry entry = archive.GetEntry(entryName)!;

        using (Stream stream = entry.Open())
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }

    class NonSeekableMemoryStream : MemoryStream
    {
        public NonSeekableMemoryStream(byte[] bytes)
            : base(bytes)
        {
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }
    }
}
