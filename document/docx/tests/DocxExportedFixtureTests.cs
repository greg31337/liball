using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxExportedFixtureTests
{
    [TestCase("microsoft-word.docx.b64", "Microsoft Office Word", "Microsoft Word fixture")]
    [TestCase("libreoffice-writer.docx.b64", "LibreOffice", "LibreOffice Writer fixture")]
    [TestCase("google-docs.docx.b64", "Google", "Google Docs fixture")]
    [TestCase("apple-pages.docx.b64", "Pages", "Apple Pages fixture")]
    [TestCase("openxml-sdk.docx.b64", "Microsoft Open XML SDK", "Open XML SDK fixture")]
    [TestCase("docx4j.docx.b64", "docx4j", "docx4j fixture")]
    [TestCase("python-docx.docx.b64", "python-docx", "python-docx fixture")]
    public void ExportedFixtureDocuments_OpenValidateExtractAndRoundTrip(string fileName, string expectedApplication, string expectedText)
    {
        byte[] bytes = ReadFixtureBytes(fileName);
        global::docx.DocxValidator validator = new global::docx.DocxValidator();

        using (MemoryStream validationStream = new MemoryStream(bytes))
        {
            Assert.That(validator.Validate(validationStream).Count(), Is.EqualTo(0));
        }

        using (MemoryStream openStream = new MemoryStream(bytes))
        {
            global::docx.DocxPackage package = global::docx.DocxPackage.Open(openStream);
            global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

            Assert.That(validator.ValidatePackage(package).Count(), Is.EqualTo(0));
            Assert.That(document.HasCoreProperties(), Is.True);
            Assert.That(document.HasExtendedProperties(), Is.True);
            Assert.That(document.GetExtendedProperties().Application, Is.EqualTo(expectedApplication));
            Assert.That(document.GetText(), Is.EqualTo(expectedText + "\nGenerated package compatibility sample"));
            Assert.That(document.Body.BlockCount(), Is.EqualTo(2));
            Assert.That(document.Body.GetBlock(0).IsParagraph(), Is.True);
            Assert.That(document.Body.GetParagraph(0).GetText(), Is.EqualTo(expectedText));
            global::docx.DocxRun firstRun = document.Body.GetParagraph(0).GetRun(0);
            global::docx.IDocxTextProcessor textProcessor = new global::docx.DocxTextProcessor();

            Assert.That(textProcessor.ReplaceInRun(firstRun, expectedText, expectedText + " edited"), Is.EqualTo(1));
            Assert.That(document.GetText(), Is.EqualTo(expectedText + " edited\nGenerated package compatibility sample"));

            MemoryStream saved = new MemoryStream();
            global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
            persistence.SaveDocument(document, saved, new global::docx.DocxSaveOptions());
            saved.Position = 0;

            global::docx.DocxDocument reopened = global::docx.DocxDocument.OpenPackage(global::docx.DocxPackage.Open(saved));

            Assert.That(reopened.GetText(), Is.EqualTo(expectedText + " edited\nGenerated package compatibility sample"));
            Assert.That(reopened.GetExtendedProperties().Application, Is.EqualTo(expectedApplication));
            Assert.That(validator.ValidatePackage(reopened.Package).Count(), Is.EqualTo(0));
        }
    }

    [Test]
    public void ExportedFixtureDirectory_ContainsOfficeAndServerGeneratorDocuments()
    {
        string directory = GetFixtureDirectory();
        string[] files = Directory.GetFiles(directory, "*.docx.b64");

        Assert.That(files.Select(Path.GetFileName).ToArray(), Does.Contain("microsoft-word.docx.b64"));
        Assert.That(files.Select(Path.GetFileName).ToArray(), Does.Contain("libreoffice-writer.docx.b64"));
        Assert.That(files.Select(Path.GetFileName).ToArray(), Does.Contain("google-docs.docx.b64"));
        Assert.That(files.Select(Path.GetFileName).ToArray(), Does.Contain("apple-pages.docx.b64"));
        Assert.That(files.Select(Path.GetFileName).ToArray(), Does.Contain("openxml-sdk.docx.b64"));
        Assert.That(files.Select(Path.GetFileName).ToArray(), Does.Contain("docx4j.docx.b64"));
        Assert.That(files.Select(Path.GetFileName).ToArray(), Does.Contain("python-docx.docx.b64"));
    }

    static byte[] ReadFixtureBytes(string fileName)
    {
        string path = Path.Combine(GetFixtureDirectory(), fileName);
        string text = File.ReadAllText(path);

        return Convert.FromBase64String(text);
    }

    static string GetFixtureDirectory()
    {
        return Path.Combine(FindRepositoryRoot(), "tests", "Fixtures", "ExportedDocuments");
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "docx.slnx")) && Directory.Exists(Path.Combine(directory, "src")))
            {
                return directory;
            }

            DirectoryInfo? parent = Directory.GetParent(directory);
            if (parent == null)
            {
                break;
            }

            directory = parent.FullName;
        }

        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
    }
}
