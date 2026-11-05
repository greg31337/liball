using System.Security.Cryptography;
using System.Text;

namespace odf.Tests;

public class InteroperabilityFixtureTests
{
    static object[][] ProducerFixtures =
    [
        ["libreoffice-1.0.fodt", "1.0", "LibreOffice/3.3", "http://openoffice.org/2009/office", "ooo", "LibreOffice 1.0"],
        ["microsoft-office-1.1.fodt", "1.1", "MicrosoftOffice/12.0 Microsoft Word", "urn:schemas-microsoft-com:office:office", "mso", "Microsoft Office 1.1"],
        ["odfpy-1.2.fodt", "1.2", "ODFPY/1.4.1", "urn:odfpy:fixture:1.0", "odfpy", "odfpy 1.2"],
        ["odfdom-1.3.fodt", "1.3", "Apache ODF Toolkit ODFDOM/0.12.0", "urn:odftoolkit:odfdom:fixture:1.0", "odfdom", "ODFDOM 1.3"],
        ["libreoffice-1.4.fodt", "1.4", "LibreOffice/25.2", OdfNamespaces.LibreOffice, "loext", "LibreOffice 1.4"]
    ];

    // Sources: LibreOffice core sw/qa/extras/odfexport/data/fdo38244.odt;
    // ODF Toolkit ODFDOM resources and CharacterParagraphFormat_MSO15.odt.
    // The odfpy fixture was generated with odfpy 1.4.1 for this corpus.
    static object[][] ProducerPackageFixtures =
    [
        ["libreoffice-fdo38244.odt.base64", "AFED3B64830E34BD1BBD8A32D24AC815910A2E15413534C0E2BC857B093F7B36", "1.2", "LibreOffice/3.7$Linux_X86_64 LibreOffice_project/a56114", "First comment.", OdfPartNames.Thumbnail],
        ["microsoft-office-15-character-format.odt.base64", "C268607D0EC61CEAB1B378EC38BB25E194C76438D87A750AB6CBD0B410B7C47F", "1.2", "MicrosoftOffice/15.0 MicrosoftWord", "A", ""],
        ["odfdom-0.8.8-text-template.odt.base64", "9E2CF4332458D2D3670BE9343E9D0FF1C9C98FCAD785D34BEC0E0E3C9FB6B4A5", "1.2", "ODFDOM/0.8.8-SNAPSHOT(2011-01-20T20:10:10)", "", "META-INF/"],
        ["odfpy-1.4.1-generated.odt.base64", "88111A90DBFCFD005A99072701A3A99E130036295DB8B22521A298BC7C949CDB", "1.2", "ODFPY/1.4.1", "Supported odfpy package", ""]
    ];

    [TestCaseSource(nameof(ProducerFixtures))]
    public void OpensAndResavesProducerFixtureWithoutContentLoss(
        string fixtureName,
        string version,
        string generator,
        string foreignNamespace,
        string foreignPrefix,
        string marker)
    {
        byte[] source = File.ReadAllBytes(FixturePath(fixtureName));
        IOdfDocumentReader reader = new OdfDocumentReader();
        IOdfDocumentWriter writer = new OdfDocumentWriter();
        OdfDocument document = reader.ReadDocumentBytes(
            source,
            fixtureName,
            new OdfLoadOptions());

        byte[] saved = writer.WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                PreserveLexicalValues = true,
                PreserveUnknownContent = true
            });
        OdfDocument reopened = reader.ReadDocumentBytes(
            saved,
            "resaved-" + fixtureName,
            new OdfLoadOptions());
        OdfNode root = reopened.Parts[0].Root!;
        OdfNode paragraph = FindElement(
            root,
            OdfNamespaces.Text,
            "p");
        OdfNode style = FindElement(
            root,
            OdfNamespaces.Style,
            "style");
        OdfNode foreign = FindElement(root, foreignNamespace, "payload");
        string savedXml = Encoding.UTF8.GetString(saved);

        Assert.Multiple(() =>
        {
            Assert.That(document.PackageKind, Is.EqualTo(OdfPackageKind.Flat));
            Assert.That(reopened.PackageKind, Is.EqualTo(OdfPackageKind.Flat));
            Assert.That(reopened.Kind, Is.EqualTo(OdfDocumentKind.Text));
            Assert.That(reopened.MediaType, Is.EqualTo(OdfMediaTypes.Text));
            Assert.That(reopened.VersionInfo?.LexicalValue, Is.EqualTo(version));
            Assert.That(reopened.Metadata.Generator, Is.EqualTo(generator));
            Assert.That(
                new OdfTextEditor().ExtractPlainText(reopened),
                Does.Contain("Supported " + marker));
            Assert.That(
                ReadAttribute(paragraph, OdfNamespaces.Text, "style-name"),
                Is.EqualTo("ProducerStyle"));
            Assert.That(
                ReadAttribute(style, OdfNamespaces.Style, "family"),
                Is.EqualTo("paragraph"));
            Assert.That(foreign.Name?.Prefix, Is.EqualTo(foreignPrefix));
            Assert.That(foreign.Children[0].Value, Is.EqualTo("Foreign " + marker));
            Assert.That(
                ReadAttribute(foreign, foreignNamespace, "token"),
                Is.EqualTo("+00042.5000"));
            Assert.That(
                ReadAttribute(root, foreignNamespace, "root-token"),
                Is.EqualTo("00007"));
            Assert.That(savedXml, Does.Contain("<" + foreignPrefix + ":payload"));
        });
    }

    [TestCaseSource(nameof(ProducerPackageFixtures))]
    public void OpensAndResavesApplicationProducedPackageFixture(
        string fixtureName,
        string sha256,
        string version,
        string generator,
        string expectedText,
        string preservedEntryPath)
    {
        byte[] source = ReadBase64PackageFixture(fixtureName);
        IOdfDocumentReader reader = new OdfDocumentReader();
        IOdfDocumentWriter writer = new OdfDocumentWriter();
        OdfDocument document = reader.ReadDocumentBytes(
            source,
            fixtureName,
            new OdfLoadOptions());

        byte[] saved = writer.WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                PreserveLexicalValues = true,
                PreserveUnknownContent = true
            });
        OdfDocument reopened = reader.ReadDocumentBytes(
            saved,
            "resaved-" + fixtureName,
            new OdfLoadOptions());
        IOdfPackageReader packageReader = new OdfPackageReader();
        OdfPackage sourcePackage = packageReader.ReadPackageBytes(
            source,
            new OdfLoadOptions());
        OdfPackage savedPackage = packageReader.ReadPackageBytes(
            saved,
            new OdfLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(
                Convert.ToHexString(SHA256.HashData(source)),
                Is.EqualTo(sha256));
            Assert.That(sourcePackage.Entries, Is.Not.Empty);
            Assert.That(savedPackage.Entries, Is.Not.Empty);
            Assert.That(reopened.PackageKind, Is.EqualTo(OdfPackageKind.Package));
            Assert.That(reopened.Kind, Is.EqualTo(OdfDocumentKind.Text));
            Assert.That(reopened.MediaType, Is.EqualTo(OdfMediaTypes.Text));
            Assert.That(reopened.VersionInfo?.LexicalValue, Is.EqualTo(version));
            Assert.That(reopened.Metadata.Generator, Is.EqualTo(generator));
            Assert.That(reopened.Body, Is.Not.Null);
            Assert.That(HasEntry(savedPackage, OdfPartNames.Content), Is.True);
            Assert.That(HasEntry(savedPackage, OdfPartNames.Styles), Is.True);
            Assert.That(HasEntry(savedPackage, OdfPartNames.Metadata), Is.True);
            Assert.That(HasEntry(savedPackage, OdfPartNames.Manifest), Is.True);
        });

        if (expectedText.Length > 0)
        {
            Assert.That(
                new OdfTextEditor().ExtractPlainText(reopened),
                Does.Contain(expectedText));
        }
        if (preservedEntryPath.Length > 0)
        {
            Assert.That(
                ReadEntry(packageReader, savedPackage, preservedEntryPath),
                Is.EqualTo(ReadEntry(
                    packageReader,
                    sourcePackage,
                    preservedEntryPath)));
        }
    }

    static string FixturePath(string fixtureName)
    {
        return Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "Fixtures",
            "Interoperability",
            fixtureName));
    }

    static byte[] ReadBase64PackageFixture(string fixtureName)
    {
        string path = Path.Combine(
            FixturePath("Packages"),
            fixtureName);
        return Convert.FromBase64String(File.ReadAllText(path));
    }

    static bool HasEntry(OdfPackage package, string path)
    {
        foreach (OdfPackageEntry entry in package.Entries)
        {
            if (entry.Path == path)
            {
                return true;
            }
        }
        return false;
    }

    static byte[] ReadEntry(
        IOdfPackageReader reader,
        OdfPackage package,
        string path)
    {
        using Stream input = reader.OpenPackageEntry(package, path);
        using MemoryStream output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }

    static OdfNode FindElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        OdfNode? result = FindOptionalElement(node, namespaceName, localName);
        Assert.That(result, Is.Not.Null);
        return result!;
    }

    static OdfNode? FindOptionalElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (node.Kind == OdfNodeKind.Element
            && node.Name?.NamespaceName == namespaceName
            && node.Name.LocalName == localName)
        {
            return node;
        }
        foreach (OdfNode child in node.Children)
        {
            OdfNode? result = FindOptionalElement(
                child,
                namespaceName,
                localName);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    static string ReadAttribute(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        foreach (OdfAttribute attribute in node.Attributes)
        {
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
        }
        return "";
    }
}
