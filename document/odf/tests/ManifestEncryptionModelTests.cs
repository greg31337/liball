using System.Text;

namespace odf.Tests;

public class ManifestEncryptionModelTests
{
    [Test]
    public void ParsesPasswordEncryptionChecksumAlgorithmDerivationAndSizeModels()
    {
        string xml = ManifestStart("1.4")
            + "<m:file-entry m:full-path='secret.bin' m:media-type='application/octet-stream' m:size='4096'>"
            + "<m:encryption-data m:checksum-type='"
            + OdfAlgorithmIdentifiers.Sha256OneKilobyte
            + "' m:checksum='AQIDBA=='>"
            + "<m:algorithm m:algorithm-name='"
            + OdfAlgorithmIdentifiers.Aes256Cbc
            + "' m:initialisation-vector='EBESExQVFhcYGRobHB0eHw=='/>"
            + "<m:start-key-generation m:start-key-generation-name='"
            + OdfAlgorithmIdentifiers.Sha256Odf
            + "' m:key-size='32'/>"
            + "<m:key-derivation m:key-derivation-name='"
            + OdfAlgorithmIdentifiers.Pbkdf2
            + "' m:salt='c2FsdC12YWx1ZQ==' m:iteration-count='100000' m:key-size='32'/>"
            + "</m:encryption-data></m:file-entry></m:manifest>";

        OdfManifest manifest = ManifestParser.ParseText(xml);
        OdfManifestEntry entry = manifest.Entries[0];
        OdfEncryptionInfo encryption = entry.Encryption ?? new OdfEncryptionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(manifest.IsValid, Is.True);
            Assert.That(encryption.IsValid, Is.True);
            Assert.That(encryption.Method, Is.EqualTo(OdfEncryptionMethod.Aes256Cbc));
            Assert.That(encryption.ChecksumType, Is.EqualTo(OdfAlgorithmIdentifiers.Sha256OneKilobyte));
            Assert.That(encryption.Checksum, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(encryption.ChecksumLexical, Is.EqualTo("AQIDBA=="));
            Assert.That(encryption.ChecksumIsValid, Is.True);
            Assert.That(encryption.HasSize, Is.True);
            Assert.That(encryption.Size, Is.EqualTo(4096));
            Assert.That(encryption.SizeLexical, Is.EqualTo("4096"));
            Assert.That(encryption.SizeIsValid, Is.True);
            Assert.That(encryption.Algorithm.Name, Is.EqualTo(OdfAlgorithmIdentifiers.Aes256Cbc));
            Assert.That(encryption.Algorithm.InitializationVector, Has.Length.EqualTo(16));
            Assert.That(encryption.Algorithm.InitializationVectorIsValid, Is.True);
            Assert.That(encryption.HasStartKeyGeneration, Is.True);
            Assert.That(encryption.StartKeyGeneration.Name, Is.EqualTo(OdfAlgorithmIdentifiers.Sha256Odf));
            Assert.That(encryption.StartKeyGeneration.KeySize, Is.EqualTo(32));
            Assert.That(encryption.KeyDerivation.Name, Is.EqualTo(OdfAlgorithmIdentifiers.Pbkdf2));
            Assert.That(encryption.KeyDerivation.Salt, Is.EqualTo(Encoding.UTF8.GetBytes("salt-value")));
            Assert.That(encryption.KeyDerivation.IterationCount, Is.EqualTo(100000));
            Assert.That(encryption.KeyDerivation.KeySize, Is.EqualTo(32));
            Assert.That(encryption.Node, Is.SameAs(entry.EncryptionData));
        });
    }

    [TestCase(OdfAlgorithmIdentifiers.BlowfishCfbName, OdfEncryptionMethod.BlowfishCfb)]
    [TestCase(OdfAlgorithmIdentifiers.BlowfishCfb, OdfEncryptionMethod.BlowfishCfb)]
    [TestCase(OdfAlgorithmIdentifiers.Aes128Cbc, OdfEncryptionMethod.Aes128Cbc)]
    [TestCase(OdfAlgorithmIdentifiers.Aes192Cbc, OdfEncryptionMethod.Aes192Cbc)]
    [TestCase("urn:example:future-cipher", OdfEncryptionMethod.Unsupported)]
    public void MapsStandardAndUnsupportedEncryptionAlgorithms(
        string algorithm,
        OdfEncryptionMethod expected)
    {
        string xml = ManifestStart("1.4")
            + "<m:file-entry m:full-path='secret.bin' m:media-type='application/octet-stream'>"
            + "<m:encryption-data m:checksum-type='SHA1/1K' m:checksum='Y2hlY2s='>"
            + "<m:algorithm m:algorithm-name='"
            + algorithm
            + "' m:initialisation-vector='aXY='/>"
            + "<m:key-derivation m:key-derivation-name='PBKDF2' m:salt='c2FsdA=='"
            + " m:iteration-count='1024'/></m:encryption-data></m:file-entry></m:manifest>";

        OdfManifest manifest = ManifestParser.ParseText(xml);
        OdfEncryptionInfo encryption = manifest.Entries[0].Encryption
            ?? new OdfEncryptionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(encryption.Method, Is.EqualTo(expected));
            Assert.That(encryption.Algorithm.Method, Is.EqualTo(expected));
            Assert.That(encryption.IsValid, Is.True);
        });
    }

    [Test]
    public void ParsesPgpDerivationEncryptedKeyRecipientAndCipherModels()
    {
        string xml = ManifestStart("1.4")
            + "<m:encrypted-key>"
            + "<m:encryption-method m:PGPAlgorithm='urn:example:openpgp'/>"
            + "<m:keyinfo><m:PGPData><m:PGPKeyID>AQIDBAUGBwg=</m:PGPKeyID>"
            + "<m:PGPKeyPacket>cGFja2V0</m:PGPKeyPacket>"
            + "</m:PGPData></m:keyinfo>"
            + "<m:CipherData><m:CipherValue>ZW5jcnlwdGVkLWtleQ==</m:CipherValue></m:CipherData>"
            + "</m:encrypted-key>"
            + "<m:file-entry m:full-path='secret.bin' m:media-type='application/octet-stream'>"
            + "<m:encryption-data m:checksum-type='SHA1/1K' m:checksum='Y2hlY2s='>"
            + "<m:algorithm m:algorithm-name='Blowfish CFB' m:initialisation-vector='aXY='/>"
            + "<m:key-derivation m:key-derivation-name='PGP'/>"
            + "</m:encryption-data></m:file-entry></m:manifest>";

        OdfManifest manifest = ManifestParser.ParseText(xml);
        OdfEncryptedKey encryptedKey = manifest.EncryptedKeyModels[0];
        OdfEncryptionInfo encryption = manifest.Entries[0].Encryption
            ?? new OdfEncryptionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(manifest.EncryptedKeys, Has.Length.EqualTo(1));
            Assert.That(manifest.EncryptedKeyModels, Has.Length.EqualTo(1));
            Assert.That(encryptedKey.IsValid, Is.True);
            Assert.That(encryptedKey.HasPgpAlgorithm, Is.True);
            Assert.That(encryptedKey.PgpAlgorithm, Is.EqualTo("urn:example:openpgp"));
            Assert.That(encryptedKey.EncryptionMethodNode, Is.SameAs(encryptedKey.Node.Children[0]));
            Assert.That(encryptedKey.KeyInfoNode, Is.SameAs(encryptedKey.Node.Children[1]));
            Assert.That(encryptedKey.PgpData.Node, Is.SameAs(encryptedKey.KeyInfoNode?.Children[0]));
            Assert.That(encryptedKey.PgpData.KeyIdNode, Is.SameAs(encryptedKey.PgpData.Node.Children[0]));
            Assert.That(encryptedKey.PgpData.KeyId, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
            Assert.That(encryptedKey.PgpData.HasKeyPacket, Is.True);
            Assert.That(encryptedKey.PgpData.KeyPacketNode, Is.SameAs(encryptedKey.PgpData.Node.Children[1]));
            Assert.That(Encoding.UTF8.GetString(encryptedKey.PgpData.KeyPacket), Is.EqualTo("packet"));
            Assert.That(encryptedKey.CipherData.Node, Is.SameAs(encryptedKey.Node.Children[2]));
            Assert.That(encryptedKey.CipherData.ValueNode, Is.SameAs(encryptedKey.CipherData.Node.Children[0]));
            Assert.That(Encoding.UTF8.GetString(encryptedKey.CipherData.Value), Is.EqualTo("encrypted-key"));
            Assert.That(encryptedKey.Node, Is.SameAs(manifest.EncryptedKeys[0]));
            Assert.That(encryption.KeyDerivation.IsPgp, Is.True);
            Assert.That(encryption.KeyDerivation.IsValid, Is.True);
            Assert.That(encryption.IsValid, Is.True);
        });
    }

    [Test]
    public void PreservesOpenPgpMetadataAndForeignContentDuringStructuredWriting()
    {
        string xml = ManifestStart("1.4")
            + "<m:encrypted-key xmlns:ext='urn:extension' ext:recipient='primary'>"
            + "<m:encryption-method m:PGPAlgorithm='urn:example:openpgp' ext:mode='keep'/>"
            + "<m:keyinfo><m:PGPData><m:PGPKeyID>AQIDBAUGBwg=</m:PGPKeyID>"
            + "<!--recipient note--><m:PGPKeyPacket>cGFja2V0</m:PGPKeyPacket>"
            + "<ext:recipient-data>opaque</ext:recipient-data>"
            + "</m:PGPData></m:keyinfo>"
            + "<m:CipherData><m:CipherValue>ZW5jcnlwdGVkLWtleQ==</m:CipherValue></m:CipherData>"
            + "</m:encrypted-key>"
            + "<m:file-entry m:full-path='/' m:media-type='application/vnd.oasis.opendocument.text'/>"
            + "</m:manifest>";
        OdfManifest manifest = ManifestParser.ParseText(xml);
        OdfSaveOptions options = new OdfSaveOptions();
        options.IndentXml = true;

        string written = ManifestWriter.WriteText(manifest, options);
        OdfManifest reparsed = ManifestParser.ParseText(written);
        OdfEncryptedKey encryptedKey = reparsed.EncryptedKeyModels[0];

        Assert.Multiple(() =>
        {
            Assert.That(written, Does.Contain("ext:recipient=\"primary\""));
            Assert.That(written, Does.Contain("ext:mode=\"keep\""));
            Assert.That(written, Does.Contain("<!--recipient note-->"));
            Assert.That(written, Does.Contain("<ext:recipient-data>opaque</ext:recipient-data>"));
            Assert.That(encryptedKey.PgpAlgorithm, Is.EqualTo("urn:example:openpgp"));
            Assert.That(encryptedKey.PgpData.KeyIdLexical, Is.EqualTo("AQIDBAUGBwg="));
            Assert.That(encryptedKey.PgpData.KeyPacketLexical, Is.EqualTo("cGFja2V0"));
            Assert.That(encryptedKey.CipherData.ValueLexical, Is.EqualTo("ZW5jcnlwdGVkLWtleQ=="));
        });
    }

    [Test]
    public void RejectsOpenPgpMetadataBeforeOdfThirteenButPreservesIt()
    {
        string xml = ManifestStart("1.2")
            + "<m:encrypted-key><m:keyinfo><m:PGPData>"
            + "<m:PGPKeyID>AQIDBAUGBwg=</m:PGPKeyID>"
            + "</m:PGPData></m:keyinfo><m:CipherData>"
            + "<m:CipherValue>ZW5jcnlwdGVkLWtleQ==</m:CipherValue>"
            + "</m:CipherData></m:encrypted-key>"
            + "<m:file-entry m:full-path='/' m:media-type='application/vnd.oasis.opendocument.text'/>"
            + "</m:manifest>";

        OdfManifest manifest = ManifestParser.ParseText(xml);

        Assert.Multiple(() =>
        {
            Assert.That(manifest.IsValid, Is.False);
            Assert.That(manifest.EncryptedKeys, Has.Length.EqualTo(1));
            Assert.That(manifest.EncryptedKeyModels[0].IsValid, Is.True);
            Assert.That(HasDiagnostic(manifest, OdfDiagnosticCodes.UnexpectedElement), Is.True);
        });
    }

    [Test]
    public void RetainsInvalidLexicalValuesAndReportsTypedModelValidity()
    {
        string xml = ManifestStart("1.4")
            + "<m:encrypted-key><m:keyinfo><m:PGPData>"
            + "<m:PGPKeyID>not-base64</m:PGPKeyID>"
            + "</m:PGPData></m:keyinfo><m:CipherData>"
            + "<m:CipherValue>also-invalid</m:CipherValue>"
            + "</m:CipherData></m:encrypted-key>"
            + "<m:file-entry m:full-path='secret.bin' m:media-type='application/octet-stream' m:size='9'>"
            + "<m:encryption-data m:checksum-type='SHA1/1K' m:checksum='bad'>"
            + "<m:algorithm m:algorithm-name='Blowfish CFB' m:initialisation-vector='bad'/>"
            + "<m:start-key-generation m:start-key-generation-name='SHA1' m:key-size='huge'/>"
            + "<m:key-derivation m:key-derivation-name='PBKDF2' m:salt='bad'"
            + " m:iteration-count='999999999999999999999' m:key-size='-1'/>"
            + "</m:encryption-data></m:file-entry></m:manifest>";

        OdfManifest manifest = ManifestParser.ParseText(xml);
        OdfEncryptionInfo encryption = manifest.Entries[0].Encryption
            ?? new OdfEncryptionInfo();
        OdfEncryptedKey encryptedKey = manifest.EncryptedKeyModels[0];

        Assert.Multiple(() =>
        {
            Assert.That(encryption.IsValid, Is.False);
            Assert.That(encryption.ChecksumLexical, Is.EqualTo("bad"));
            Assert.That(encryption.ChecksumIsValid, Is.False);
            Assert.That(encryption.Algorithm.InitializationVectorLexical, Is.EqualTo("bad"));
            Assert.That(encryption.Algorithm.InitializationVectorIsValid, Is.False);
            Assert.That(encryption.StartKeyGeneration.KeySizeLexical, Is.EqualTo("huge"));
            Assert.That(encryption.StartKeyGeneration.KeySizeIsValid, Is.False);
            Assert.That(encryption.KeyDerivation.SaltLexical, Is.EqualTo("bad"));
            Assert.That(encryption.KeyDerivation.IterationCountIsValid, Is.False);
            Assert.That(encryption.KeyDerivation.KeySizeIsValid, Is.False);
            Assert.That(encryptedKey.IsValid, Is.False);
            Assert.That(encryptedKey.PgpData.KeyIdLexical, Is.EqualTo("not-base64"));
            Assert.That(encryptedKey.CipherData.ValueLexical, Is.EqualTo("also-invalid"));
            Assert.That(HasDiagnostic(manifest, OdfDiagnosticCodes.InvalidDatatype), Is.True);
        });
    }

    static string ManifestStart(string version)
    {
        return "<m:manifest xmlns:m='"
            + OdfNamespaces.Manifest
            + "' m:version='"
            + version
            + "'>";
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
}
