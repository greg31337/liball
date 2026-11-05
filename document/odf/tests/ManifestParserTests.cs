using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class ManifestParserTests
{
    [TestCase("1.2")]
    [TestCase("1.3")]
    [TestCase("1.4")]
    public void ParsesManifestVersionsAndFileEntryAttributes(string version)
    {
        string xml = ManifestStart(version)
            + "<m:file-entry m:full-path='content.xml' m:media-type='text/xml'"
            + " m:size='42' m:preferred-view-mode='read-only' m:version='"
            + version
            + "'/>"
            + "</m:manifest>";

        OdfManifest manifest = ManifestParser.ParseText(xml);
        OdfManifestEntry entry = manifest.Entries[0];

        Assert.Multiple(() =>
        {
            Assert.That(manifest.IsValid, Is.True);
            Assert.That(manifest.Version, Is.EqualTo(version));
            Assert.That(manifest.Entries, Has.Length.EqualTo(1));
            Assert.That(entry.FullPath, Is.EqualTo("content.xml"));
            Assert.That(entry.MediaType, Is.EqualTo(OdfMediaTypes.Xml));
            Assert.That(entry.HasSize, Is.True);
            Assert.That(entry.SizeIsValid, Is.True);
            Assert.That(entry.Size, Is.EqualTo(42));
            Assert.That(entry.SizeLexical, Is.EqualTo("42"));
            Assert.That(entry.PreferredViewMode, Is.EqualTo("read-only"));
            Assert.That(entry.Version, Is.EqualTo(version));
            Assert.That(entry.Node.Name.NamespaceName, Is.EqualTo(OdfNamespaces.Manifest));
            Assert.That(entry.Node.Name.Prefix, Is.EqualTo("m"));
        });
    }

    [Test]
    public void ParsesEveryOdf12EncryptionDataElementAndAttribute()
    {
        string xml = ManifestStart("1.2")
            + "<m:file-entry m:full-path='content.xml' m:media-type='text/xml' m:size='128'>"
            + "<m:encryption-data m:checksum-type='SHA1/1K' m:checksum='Y2hlY2s='>"
            + "<m:algorithm m:algorithm-name='Blowfish CFB' m:initialisation-vector='aXY='>"
            + "<ext:parameters ext:mode='compat'>mixed<ext:value>one</ext:value></ext:parameters>"
            + "</m:algorithm>"
            + "<m:start-key-generation m:start-key-generation-name='SHA1' m:key-size='20'/>"
            + "<m:key-derivation m:key-derivation-name='PBKDF2' m:salt='c2FsdA=='"
            + " m:iteration-count='1024' m:key-size='16'/>"
            + "</m:encryption-data>"
            + "</m:file-entry>"
            + "</m:manifest>";

        OdfManifest manifest = ManifestParser.ParseText(xml);
        OdfManifestNode encryption = manifest.Entries[0].EncryptionData!;
        OdfManifestNode algorithm = FindChild(encryption, "algorithm");
        OdfManifestNode startKey = FindChild(encryption, "start-key-generation");
        OdfManifestNode derivation = FindChild(encryption, "key-derivation");
        OdfManifestNode extension = FindChild(algorithm, "parameters");

        Assert.Multiple(() =>
        {
            Assert.That(manifest.IsValid, Is.True);
            Assert.That(AttributeValue(encryption, OdfNamespaces.Manifest, "checksum-type"), Is.EqualTo("SHA1/1K"));
            Assert.That(AttributeValue(encryption, OdfNamespaces.Manifest, "checksum"), Is.EqualTo("Y2hlY2s="));
            Assert.That(AttributeValue(algorithm, OdfNamespaces.Manifest, "algorithm-name"), Is.EqualTo("Blowfish CFB"));
            Assert.That(AttributeValue(algorithm, OdfNamespaces.Manifest, "initialisation-vector"), Is.EqualTo("aXY="));
            Assert.That(AttributeValue(startKey, OdfNamespaces.Manifest, "start-key-generation-name"), Is.EqualTo("SHA1"));
            Assert.That(AttributeValue(startKey, OdfNamespaces.Manifest, "key-size"), Is.EqualTo("20"));
            Assert.That(AttributeValue(derivation, OdfNamespaces.Manifest, "key-derivation-name"), Is.EqualTo("PBKDF2"));
            Assert.That(AttributeValue(derivation, OdfNamespaces.Manifest, "salt"), Is.EqualTo("c2FsdA=="));
            Assert.That(AttributeValue(derivation, OdfNamespaces.Manifest, "iteration-count"), Is.EqualTo("1024"));
            Assert.That(AttributeValue(derivation, OdfNamespaces.Manifest, "key-size"), Is.EqualTo("16"));
            Assert.That(extension.IsManifestElement, Is.False);
            Assert.That(extension.TextContent, Is.EqualTo("mixedone"));
            Assert.That(AttributeValue(extension, "urn:example:manifest-extension", "mode"), Is.EqualTo("compat"));
            Assert.That(extension.Children[0].TextContent, Is.EqualTo("one"));
        });
    }

    [TestCase("1.3")]
    [TestCase("1.4")]
    public void ParsesEncryptedKeyPgpAndCipherSubtrees(string version)
    {
        string xml = ManifestStart(version)
            + "<m:encrypted-key>"
            + "<m:encryption-method m:PGPAlgorithm='urn:example:pgp'/>"
            + "<m:keyinfo><m:PGPData><m:PGPKeyID>a2V5LWlk</m:PGPKeyID>"
            + "<m:PGPKeyPacket>cGFja2V0</m:PGPKeyPacket>"
            + "</m:PGPData></m:keyinfo>"
            + "<m:CipherData><m:CipherValue>Y2lwaGVy</m:CipherValue></m:CipherData>"
            + "</m:encrypted-key>"
            + "<m:file-entry m:full-path='/' m:media-type='"
            + OdfMediaTypes.Text
            + "'/>"
            + "</m:manifest>";

        OdfManifest manifest = ManifestParser.ParseText(xml);
        OdfManifestNode encryptedKey = manifest.EncryptedKeys[0];
        OdfManifestNode method = FindDescendant(encryptedKey, "encryption-method");
        OdfManifestNode keyId = FindDescendant(encryptedKey, "PGPKeyID");
        OdfManifestNode keyPacket = FindDescendant(encryptedKey, "PGPKeyPacket");
        OdfManifestNode cipherValue = FindDescendant(encryptedKey, "CipherValue");

        Assert.Multiple(() =>
        {
            Assert.That(manifest.IsValid, Is.True);
            Assert.That(manifest.EncryptedKeys, Has.Length.EqualTo(1));
            Assert.That(AttributeValue(method, OdfNamespaces.Manifest, "PGPAlgorithm"), Is.EqualTo("urn:example:pgp"));
            Assert.That(keyId.TextContent, Is.EqualTo("a2V5LWlk"));
            Assert.That(keyPacket.TextContent, Is.EqualTo("cGFja2V0"));
            Assert.That(cipherValue.TextContent, Is.EqualTo("Y2lwaGVy"));
        });
    }

    [Test]
    public void PreservesForeignAttributesElementsPrefixesOrderAndOriginalXml()
    {
        string xml = "<?xml version='1.0'?>"
            + "<m:manifest xmlns:m='"
            + OdfNamespaces.Manifest
            + "' xmlns:ext='urn:example:manifest-extension' m:version='1.4' ext:root='keep'>"
            + "<ext:before ext:value='one'>alpha<ext:nested/>omega</ext:before>"
            + "<m:file-entry m:full-path='custom.bin' m:media-type='application/octet-stream' ext:flag='yes'>"
            + "<ext:payload ext:kind='binary'>AAEC</ext:payload>"
            + "</m:file-entry>"
            + "<ext:after/>"
            + "</m:manifest>";

        OdfManifest manifest = ManifestParser.ParseText(xml);
        OdfManifestEntry entry = manifest.Entries[0];

        Assert.Multiple(() =>
        {
            Assert.That(manifest.IsValid, Is.True);
            Assert.That(manifest.OriginalXml, Is.EqualTo(xml));
            Assert.That(manifest.Root.NamespaceDeclarations, Has.Length.EqualTo(2));
            Assert.That(manifest.Root.Children[0].Name.LocalName, Is.EqualTo("before"));
            Assert.That(manifest.Root.Children[1].Name.LocalName, Is.EqualTo("file-entry"));
            Assert.That(manifest.Root.Children[2].Name.LocalName, Is.EqualTo("after"));
            Assert.That(manifest.ForeignAttributes, Has.Length.EqualTo(1));
            Assert.That(manifest.ForeignAttributes[0].Name.Prefix, Is.EqualTo("ext"));
            Assert.That(manifest.ForeignAttributes[0].Value, Is.EqualTo("keep"));
            Assert.That(manifest.ForeignElements, Has.Length.EqualTo(2));
            Assert.That(manifest.ForeignElements[0].TextContent, Is.EqualTo("alphaomega"));
            Assert.That(manifest.ForeignElements[0].OriginalXml, Does.Contain("ext:nested"));
            Assert.That(entry.ForeignAttributes, Has.Length.EqualTo(1));
            Assert.That(entry.ForeignAttributes[0].Value, Is.EqualTo("yes"));
            Assert.That(entry.ForeignElements, Has.Length.EqualTo(1));
            Assert.That(entry.ForeignElements[0].Name.NamespaceName, Is.EqualTo("urn:example:manifest-extension"));
            Assert.That(entry.ForeignElements[0].TextContent, Is.EqualTo("AAEC"));
        });
    }

    [Test]
    public void InvalidOrOutOfRangeSizeIsPreservedLexicallyAndDiagnosed()
    {
        string lexical = "9223372036854775808";
        string xml = ManifestStart("1.4")
            + "<m:file-entry m:full-path='large.bin' m:media-type='application/octet-stream' m:size='"
            + lexical
            + "'/>"
            + "</m:manifest>";

        OdfManifest manifest = ManifestParser.ParseText(xml);
        OdfManifestEntry entry = manifest.Entries[0];

        Assert.Multiple(() =>
        {
            Assert.That(manifest.IsValid, Is.False);
            Assert.That(entry.HasSize, Is.True);
            Assert.That(entry.SizeIsValid, Is.False);
            Assert.That(entry.SizeLexical, Is.EqualTo(lexical));
            Assert.That(HasDiagnostic(manifest, OdfDiagnosticCodes.InvalidDatatype), Is.True);
        });
    }

    [Test]
    public void ZipInspectionRetainsParsedManifest()
    {
        string manifestXml = ManifestStart("1.4")
            + "<m:file-entry m:full-path='/' m:media-type='"
            + OdfMediaTypes.Text
            + "'/>"
            + "<m:file-entry m:full-path='content.xml' m:media-type='text/xml'/>"
            + "</m:manifest>";
        byte[] package = CreatePackage(manifestXml);

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(package);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.True);
            Assert.That(inspection.Manifest, Is.Not.Null);
            Assert.That(inspection.Manifest!.Version, Is.EqualTo("1.4"));
            Assert.That(inspection.Manifest.Entries, Has.Length.EqualTo(2));
            Assert.That(inspection.Manifest.Entries[1].FullPath, Is.EqualTo("content.xml"));
        });
    }

    static string ManifestStart(string version)
    {
        return "<m:manifest xmlns:m='"
            + OdfNamespaces.Manifest
            + "' xmlns:ext='urn:example:manifest-extension' m:version='"
            + version
            + "'>";
    }

    static OdfManifestNode FindChild(OdfManifestNode node, string localName)
    {
        foreach (OdfManifestNode child in node.Children)
        {
            if (child.Name.LocalName == localName)
            {
                return child;
            }
        }
        Assert.Fail($"Expected child {localName}.");
        return new OdfManifestNode();
    }

    static OdfManifestNode FindDescendant(OdfManifestNode node, string localName)
    {
        foreach (OdfManifestNode child in node.Children)
        {
            if (child.Name.LocalName == localName)
            {
                return child;
            }
            OdfManifestNode nested = FindDescendantOrEmpty(child, localName);
            if (nested.Name.LocalName.Length > 0)
            {
                return nested;
            }
        }
        Assert.Fail($"Expected descendant {localName}.");
        return new OdfManifestNode();
    }

    static OdfManifestNode FindDescendantOrEmpty(OdfManifestNode node, string localName)
    {
        foreach (OdfManifestNode child in node.Children)
        {
            if (child.Name.LocalName == localName)
            {
                return child;
            }
            OdfManifestNode nested = FindDescendantOrEmpty(child, localName);
            if (nested.Name.LocalName.Length > 0)
            {
                return nested;
            }
        }
        return new OdfManifestNode();
    }

    static string AttributeValue(
        OdfManifestNode node,
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
        Assert.Fail($"Expected attribute {localName}.");
        return "";
    }

    static bool HasDiagnostic(OdfManifest manifest, string code)
    {
        foreach (OdfDiagnostic diagnostic in manifest.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }
        return false;
    }

    static byte[] CreatePackage(string manifestXml)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                WriteEntry(archive, OdfPartNames.Mimetype, OdfMediaTypes.Text, CompressionLevel.NoCompression);
                WriteEntry(archive, OdfPartNames.Manifest, manifestXml, CompressionLevel.SmallestSize);
                WriteEntry(archive, OdfPartNames.Content, "content", CompressionLevel.SmallestSize);
            }
            return stream.ToArray();
        }
    }

    static void WriteEntry(
        ZipArchive archive,
        string path,
        string content,
        CompressionLevel compressionLevel)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compressionLevel);
        using (Stream output = entry.Open())
        using (StreamWriter writer = new StreamWriter(output, new UTF8Encoding(false)))
        {
            writer.Write(content);
        }
    }
}
