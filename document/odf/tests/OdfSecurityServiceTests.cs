using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace odf.Tests;

public class OdfSecurityServiceTests
{
    [TestCase(OdfEncryptionMethod.BlowfishCfb, OdfVersion.Version12)]
    [TestCase(OdfEncryptionMethod.Aes128Cbc, OdfVersion.Version12)]
    [TestCase(OdfEncryptionMethod.Aes192Cbc, OdfVersion.Version13)]
    [TestCase(OdfEncryptionMethod.Aes256Cbc, OdfVersion.Version14)]
    public void EncryptsAndDecryptsOneEntryThroughPublicService(
        OdfEncryptionMethod method,
        OdfVersion version)
    {
        byte[] original = Encoding.UTF8.GetBytes(
            "The public security service round-trips this package entry.");
        OdfPackage package = CreatePackage("content.xml", original);
        IOdfSecurityService service = new OdfSecurityService();

        OdfEditResult encrypted = service.EncryptPackageEntry(
            package,
            "content.xml",
            "correct horse battery staple",
            method,
            version,
            XmlSignatureVerifierTests.CreateLimits());
        byte[] ciphertext = ReadEntry(package, "content.xml");
        OdfManifestEntry encryptedManifestEntry = FindManifestEntry(
            package,
            "content.xml");
        OdfEncryptionInfo encryptedInfo = encryptedManifestEntry.Encryption!;
        string encryptedManifestXml = Encoding.UTF8.GetString(
            ReadEntry(package, OdfPartNames.Manifest));

        OdfEditResult decrypted = service.DecryptPackageEntry(
            package,
            "content.xml",
            "correct horse battery staple",
            version,
            XmlSignatureVerifierTests.CreateLimits());
        byte[] roundTripped = ReadEntry(package, "content.xml");
        OdfManifestEntry decryptedManifestEntry = FindManifestEntry(
            package,
            "content.xml");
        string decryptedManifestXml = Encoding.UTF8.GetString(
            ReadEntry(package, OdfPartNames.Manifest));

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(encrypted.Changed, Is.True);
            Assert.That(ciphertext, Is.Not.EqualTo(original));
            Assert.That(encryptedInfo, Is.Not.Null);
            Assert.That(encryptedInfo.Method, Is.EqualTo(method));
            Assert.That(encryptedInfo.IsValid, Is.True);
            Assert.That(encryptedInfo.KeyDerivation.IterationCount, Is.EqualTo(100000));
            Assert.That(encryptedManifestXml, Does.Contain("encryption-data"));
            Assert.That(decrypted.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(roundTripped, Is.EqualTo(original));
            Assert.That(decryptedManifestEntry.Encryption, Is.Null);
            Assert.That(decryptedManifestEntry.EncryptionData, Is.Null);
            Assert.That(decryptedManifestXml, Does.Not.Contain("encryption-data"));
        });
    }

    [Test]
    public void DecryptsPublishedOdfToolkitAes256Fixture()
    {
        // Apache ODF Toolkit validator fixture, password "hello":
        // validator/src/test/resources/encrypted-with-pwd_hello.odt
        byte[] fixture = ReadOdfToolkitFixture();
        IOdfPackageReader reader = new OdfPackageReader();
        OdfPackage package = reader.ReadPackageBytes(
            fixture,
            new OdfLoadOptions());
        OdfEncryptionInfo encryption = FindManifestEntry(
            package,
            "content.xml").Encryption!;
        IOdfSecurityService service = new OdfSecurityService();

        OdfEditResult result = service.DecryptPackageDocument(
            package,
            "hello",
            OdfVersion.Version12,
            XmlSignatureVerifierTests.CreateLimits());
        byte[] plaintext = ReadEntry(package, "content.xml");

        OdfPackage wrongPasswordPackage = reader.ReadPackageBytes(
            fixture,
            new OdfLoadOptions());
        byte[] encryptedSnapshot = ReadEntry(
            wrongPasswordPackage,
            "content.xml");

        Assert.Multiple(() =>
        {
            Assert.That(
                Convert.ToHexString(SHA256.HashData(fixture)),
                Is.EqualTo("0A8C4A1B470DA72F2B8186199BCEAF6D7152B1BE28C551B55CF1F5B782A7F588"));
            Assert.That(package.IsValid, Is.True);
            Assert.That(encryption.Method, Is.EqualTo(OdfEncryptionMethod.Aes256Cbc));
            Assert.That(encryption.StartKeyGeneration.Name,
                Is.EqualTo(OdfAlgorithmIdentifiers.Sha256Odf));
            Assert.That(encryption.KeyDerivation.IterationCount, Is.EqualTo(1024));
            Assert.That(encryption.KeyDerivation.KeySize, Is.EqualTo(32));
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(result.AffectedCount, Is.EqualTo(6));
            Assert.That(plaintext, Has.Length.EqualTo(4078));
            Assert.That(
                Convert.ToHexString(SHA256.HashData(plaintext)),
                Is.EqualTo("1E7E5C34BF0FC2029CDA9CE6A65FBF16456371BE2C8DF72C31A4AE82BF8310BB"));
            Assert.That(Encoding.UTF8.GetString(plaintext),
                Does.StartWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>"));
            Assert.That(FindManifestEntry(package, "content.xml").Encryption,
                Is.Null);
            Assert.That(FindManifestEntry(package, "styles.xml").Encryption,
                Is.Null);
            Assert.That(
                () => service.DecryptPackageDocument(
                    wrongPasswordPackage,
                    "wrong",
                    OdfVersion.Version12,
                    XmlSignatureVerifierTests.CreateLimits()),
                Throws.TypeOf<OdfSecurityException>());
            Assert.That(ReadEntry(wrongPasswordPackage, "content.xml"),
                Is.EqualTo(encryptedSnapshot));
        });
    }

    [Test]
    public void WrongPasswordDoesNotMutateEncryptedEntry()
    {
        OdfPackage package = CreatePackage(
            "content.xml",
            Encoding.UTF8.GetBytes("password protected content"));
        IOdfSecurityService service = new OdfSecurityService();
        service.EncryptPackageEntry(
            package,
            "content.xml",
            "right password",
            OdfEncryptionMethod.Aes256Cbc,
            OdfVersion.Version14,
            XmlSignatureVerifierTests.CreateLimits());
        byte[] before = ReadEntry(package, "content.xml");

        Assert.That(
            () => service.DecryptPackageEntry(
                package,
                "content.xml",
                "wrong password",
                OdfVersion.Version14,
                XmlSignatureVerifierTests.CreateLimits()),
            Throws.Exception);

        Assert.Multiple(() =>
        {
            Assert.That(ReadEntry(package, "content.xml"), Is.EqualTo(before));
            Assert.That(FindManifestEntry(package, "content.xml").Encryption, Is.Not.Null);
        });
    }

    [Test]
    public void DocumentOperationsAreTransactionalAndUseDistinctEntrySalts()
    {
        byte[] content = Encoding.UTF8.GetBytes("document content");
        byte[] styles = Encoding.UTF8.GetBytes("document styles");
        OdfPackage package = CreatePackage("content.xml", content);
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        index.AddEntry("styles.xml", styles, OdfCompressionMethod.Deflated);
        PackageApiModel.RefreshPackage(package);
        IOdfSecurityService service = new OdfSecurityService();

        OdfEditResult encrypted = service.EncryptPackageDocument(
            package,
            "document password",
            OdfEncryptionMethod.Aes128Cbc,
            OdfVersion.Version14,
            XmlSignatureVerifierTests.CreateLimits());
        OdfEncryptionInfo contentEncryption = FindManifestEntry(
            package,
            "content.xml").Encryption!;
        OdfEncryptionInfo stylesEncryption = FindManifestEntry(
            package,
            "styles.xml").Encryption!;

        OdfEditResult decrypted = service.DecryptPackageDocument(
            package,
            "document password",
            OdfVersion.Version14,
            XmlSignatureVerifierTests.CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(encrypted.AffectedCount, Is.EqualTo(2));
            Assert.That(contentEncryption.KeyDerivation.Salt, Is.Not.EqualTo(stylesEncryption.KeyDerivation.Salt));
            Assert.That(decrypted.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(decrypted.AffectedCount, Is.EqualTo(2));
            Assert.That(ReadEntry(package, "content.xml"), Is.EqualTo(content));
            Assert.That(ReadEntry(package, "styles.xml"), Is.EqualTo(styles));
            Assert.That(FindManifestEntry(package, "content.xml").Encryption, Is.Null);
            Assert.That(FindManifestEntry(package, "styles.xml").Encryption, Is.Null);
        });
    }

    [Test]
    public void DecryptsOpenPgpTransportWithCallerSuppliedKeyMaterial()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("OpenPGP transported entry key");
        byte[] suppliedKey = new byte[16];
        int keyIndex = 0;
        while (keyIndex < suppliedKey.Length)
        {
            suppliedKey[keyIndex] = (byte)(keyIndex + 1);
            keyIndex++;
        }
        byte[] operationKey = (byte[])suppliedKey.Clone();
        PackageEncryptionResult encrypted;
        using (SecurityBuffer key = new SecurityBuffer(operationKey))
        {
            encrypted = PackageEncryption.EncryptAesEntry(
                plaintext,
                key,
                OdfEncryptionMethod.Aes128Cbc,
                OdfAlgorithmIdentifiers.Sha256OneKilobyte,
                OdfVersion.Version14);
        }
        SecurityBuffer.ClearBytes(operationKey);
        OdfPackage package = CreatePackage("content.xml", plaintext);
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        index.ReplaceEntry("content.xml", encrypted.EncryptedBytes);
        PackageApiModel.RefreshPackage(package);
        OdfManifestEntry manifestEntry = FindManifestEntry(
            package,
            "content.xml");
        manifestEntry.Encryption = new OdfEncryptionInfo
        {
            Method = OdfEncryptionMethod.Aes128Cbc,
            ChecksumType = OdfAlgorithmIdentifiers.Sha256OneKilobyte,
            Checksum = encrypted.Checksum,
            ChecksumIsValid = true,
            Size = plaintext.Length,
            HasSize = true,
            SizeIsValid = true,
            Algorithm = new OdfEncryptionAlgorithm
            {
                Name = OdfAlgorithmIdentifiers.Aes128Cbc,
                Method = OdfEncryptionMethod.Aes128Cbc,
                InitializationVector = encrypted.InitializationVector,
                InitializationVectorIsValid = true,
                IsValid = true
            },
            KeyDerivation = new OdfKeyDerivation
            {
                Name = "PGP",
                IsPgp = true,
                SaltIsValid = true,
                IterationCountIsValid = true,
                KeySizeIsValid = true,
                IsValid = true
            },
            IsValid = true
        };
        OdfEncryptedKey encryptedKey = new OdfEncryptedKey
        {
            IsValid = true
        };
        SecurityTestOpenPgpProvider provider = new SecurityTestOpenPgpProvider(
            suppliedKey);
        IOdfSecurityService service = new OdfSecurityService();

        OdfEditResult result = service.DecryptPackageEntryWithOpenPgp(
            package,
            "content.xml",
            provider,
            encryptedKey,
            OdfVersion.Version14,
            XmlSignatureVerifierTests.CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(ReadEntry(package, "content.xml"), Is.EqualTo(plaintext));
            Assert.That(provider.CallCount, Is.EqualTo(1));
            Assert.That(provider.RequestedKey, Is.SameAs(encryptedKey));
            Assert.That(suppliedKey, Is.All.Zero);
            Assert.That(FindManifestEntry(package, "content.xml").Encryption, Is.Null);
        });
    }

    [Test]
    public void SignsVerifiesAndReportsTamperedPackageContent()
    {
        OdfPackage package = CreatePackage(
            "content.xml",
            Encoding.UTF8.GetBytes("signed content"));
        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(XmlSignatureVerifierTests.RsaPrivateKeyPem);
        using X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(
            Convert.FromBase64String(
                XmlSignatureVerifierTests.RsaCertificateBase64));
        IOdfSecurityService service = new OdfSecurityService();

        OdfSignatureContainer created = service.SignPackageEntries(
            package,
            OdfPartNames.DocumentSignatures,
            "1.4",
            ["content.xml"],
            rsa,
            certificate,
            OdfAlgorithmIdentifiers.RsaSha256,
            XmlSignatureVerifierTests.CreateLimits());
        OdfSignatureContainer[] verified = service.VerifyPackageSignatures(
            package,
            XmlSignatureVerifierTests.CreateLimits());
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        index.ReplaceEntry(
            "content.xml",
            Encoding.UTF8.GetBytes("tampered content"));
        PackageApiModel.RefreshPackage(package);
        OdfSignatureContainer[] tampered = service.VerifyPackageSignatures(
            package,
            XmlSignatureVerifierTests.CreateLimits());
        OdfDiagnostic[] diagnostics = service.InspectPackageIntegrity(
            package,
            XmlSignatureVerifierTests.CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(created.Signatures[0].Status, Is.EqualTo(OdfSignatureStatus.Valid));
            Assert.That(verified, Has.Length.EqualTo(1));
            Assert.That(verified[0].Signatures[0].Status, Is.EqualTo(OdfSignatureStatus.Valid));
            Assert.That(PackageHasEntry(package, OdfPartNames.DocumentSignatures), Is.True);
            Assert.That(tampered[0].Signatures[0].Status, Is.EqualTo(OdfSignatureStatus.DigestMismatch));
            Assert.That(DiagnosticCodes(diagnostics), Does.Contain(OdfDiagnosticCodes.SignatureInvalid));
        });
    }

    [Test]
    public void IntegrityInspectionReportsContainerAndManifestProblems()
    {
        OdfPackage package = CreatePackage(
            "content.xml",
            Encoding.UTF8.GetBytes("content"));
        OdfPackageEntry contentEntry = FindPackageEntry(package, "content.xml");
        contentEntry.HasValidCrc = false;
        package.Manifest!.Entries =
        [
            .. package.Manifest.Entries,
            new OdfManifestEntry
            {
                FullPath = "missing.bin",
                MediaType = "application/octet-stream"
            }
        ];
        IOdfSecurityService service = new OdfSecurityService();

        OdfDiagnostic[] diagnostics = service.InspectPackageIntegrity(
            package,
            XmlSignatureVerifierTests.CreateLimits());
        string[] codes = DiagnosticCodes(diagnostics);

        Assert.Multiple(() =>
        {
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.CrcMismatch));
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.ManifestEntryMissing));
        });
    }

    [Test]
    public void ReturnsExpectedResultsForRoutineInvalidRequests()
    {
        OdfPackage package = CreatePackage(
            "content.xml",
            Encoding.UTF8.GetBytes("content"));
        IOdfSecurityService service = new OdfSecurityService();

        OdfEditResult reserved = service.EncryptPackageEntry(
            package,
            OdfPartNames.Mimetype,
            "password",
            OdfEncryptionMethod.Aes128Cbc,
            OdfVersion.Version14,
            XmlSignatureVerifierTests.CreateLimits());
        OdfEditResult missing = service.EncryptPackageEntry(
            package,
            "missing.xml",
            "password",
            OdfEncryptionMethod.Aes128Cbc,
            OdfVersion.Version14,
            XmlSignatureVerifierTests.CreateLimits());
        OdfEditResult unencrypted = service.DecryptPackageEntry(
            package,
            "content.xml",
            "password",
            OdfVersion.Version14,
            XmlSignatureVerifierTests.CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(reserved.Status, Is.EqualTo(OdfEditStatus.ValidationFailed));
            Assert.That(missing.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(unencrypted.Status, Is.EqualTo(OdfEditStatus.NoChange));
            Assert.Throws<OdfSecurityException>(() => service.EncryptPackageEntry(
                package,
                "content.xml",
                "password",
                OdfEncryptionMethod.Unsupported,
                OdfVersion.Version14,
                XmlSignatureVerifierTests.CreateLimits()));
        });
    }

    [Test]
    public void EncryptionMethodsExposeEveryRoutineEditStatus()
    {
        IOdfSecurityService service = new OdfSecurityService();
        OdfSecurityLimits limits = XmlSignatureVerifierTests.CreateLimits();
        OdfPackage entryPackage = CreatePackage(
            "content.xml",
            Encoding.UTF8.GetBytes("entry status content"));

        OdfEditResult entryConflict = service.EncryptPackageEntry(
            entryPackage,
            "missing.xml",
            "password",
            OdfEncryptionMethod.Aes128Cbc,
            OdfVersion.Version14,
            limits);
        OdfEditResult entryValidation = service.EncryptPackageEntry(
            entryPackage,
            OdfPartNames.Manifest,
            "password",
            OdfEncryptionMethod.Aes128Cbc,
            OdfVersion.Version14,
            limits);
        OdfEditResult entrySuccess = service.EncryptPackageEntry(
            entryPackage,
            "content.xml",
            "password",
            OdfEncryptionMethod.Aes128Cbc,
            OdfVersion.Version14,
            limits);
        OdfEditResult entryNoChange = service.EncryptPackageEntry(
            entryPackage,
            "content.xml",
            "password",
            OdfEncryptionMethod.Aes128Cbc,
            OdfVersion.Version14,
            limits);
        OdfEditResult openPgpValidation =
            service.DecryptPackageEntryWithOpenPgp(
                entryPackage,
                "content.xml",
                new SecurityTestOpenPgpProvider(new byte[16]),
                new OdfEncryptedKey(),
                OdfVersion.Version14,
                limits);
        OdfEditResult decryptSuccess = service.DecryptPackageEntry(
            entryPackage,
            "content.xml",
            "password",
            OdfVersion.Version14,
            limits);
        OdfEditResult decryptNoChange = service.DecryptPackageEntry(
            entryPackage,
            "content.xml",
            "password",
            OdfVersion.Version14,
            limits);
        OdfEditResult decryptConflict = service.DecryptPackageEntry(
            entryPackage,
            "missing.xml",
            "password",
            OdfVersion.Version14,
            limits);
        OdfEditResult openPgpNoChange =
            service.DecryptPackageEntryWithOpenPgp(
                entryPackage,
                "content.xml",
                new SecurityTestOpenPgpProvider(new byte[16]),
                new OdfEncryptedKey(),
                OdfVersion.Version14,
                limits);
        OdfEditResult openPgpConflict =
            service.DecryptPackageEntryWithOpenPgp(
                entryPackage,
                "missing.xml",
                new SecurityTestOpenPgpProvider(new byte[16]),
                new OdfEncryptedKey(),
                OdfVersion.Version14,
                limits);

        OdfPackage documentPackage = CreatePackage(
            "content.xml",
            Encoding.UTF8.GetBytes("document status content"));
        OdfEditResult documentEncryptSuccess = service.EncryptPackageDocument(
            documentPackage,
            "password",
            OdfEncryptionMethod.BlowfishCfb,
            OdfVersion.Version12,
            limits);
        OdfEditResult documentEncryptNoChange = service.EncryptPackageDocument(
            documentPackage,
            "password",
            OdfEncryptionMethod.BlowfishCfb,
            OdfVersion.Version12,
            limits);
        OdfEditResult documentDecryptSuccess = service.DecryptPackageDocument(
            documentPackage,
            "password",
            OdfVersion.Version12,
            limits);
        OdfEditResult documentDecryptNoChange = service.DecryptPackageDocument(
            documentPackage,
            "password",
            OdfVersion.Version12,
            limits);

        OdfPackage missingManifest = CreatePackage(
            "content.xml",
            Encoding.UTF8.GetBytes("missing manifest"));
        missingManifest.Manifest = null;
        OdfEditResult documentEncryptValidation = service.EncryptPackageDocument(
            missingManifest,
            "password",
            OdfEncryptionMethod.Aes128Cbc,
            OdfVersion.Version14,
            limits);
        OdfEditResult documentDecryptValidation = service.DecryptPackageDocument(
            missingManifest,
            "password",
            OdfVersion.Version14,
            limits);

        Assert.Multiple(() =>
        {
            Assert.That(entrySuccess.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(entryNoChange.Status, Is.EqualTo(OdfEditStatus.NoChange));
            Assert.That(entryConflict.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(entryValidation.Status,
                Is.EqualTo(OdfEditStatus.ValidationFailed));
            Assert.That(decryptSuccess.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(decryptNoChange.Status, Is.EqualTo(OdfEditStatus.NoChange));
            Assert.That(decryptConflict.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(openPgpValidation.Status,
                Is.EqualTo(OdfEditStatus.ValidationFailed));
            Assert.That(openPgpNoChange.Status, Is.EqualTo(OdfEditStatus.NoChange));
            Assert.That(openPgpConflict.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(documentEncryptSuccess.Status,
                Is.EqualTo(OdfEditStatus.Success));
            Assert.That(documentEncryptNoChange.Status,
                Is.EqualTo(OdfEditStatus.NoChange));
            Assert.That(documentDecryptSuccess.Status,
                Is.EqualTo(OdfEditStatus.Success));
            Assert.That(documentDecryptNoChange.Status,
                Is.EqualTo(OdfEditStatus.NoChange));
            Assert.That(documentEncryptValidation.Status,
                Is.EqualTo(OdfEditStatus.ValidationFailed));
            Assert.That(documentDecryptValidation.Status,
                Is.EqualTo(OdfEditStatus.ValidationFailed));
        });
    }

    static OdfPackage CreatePackage(string path, byte[] data)
    {
        ZipEntryIndex index = XmlSignatureVerifierTests.CreatePackageIndex(
            path,
            data);
        OdfPackage result = new OdfPackage();
        result.EntryIndex = index;
        result.Entries = index.EnumerateEntries();
        result.Manifest = index.Manifest;
        result.IsValid = true;
        return result;
    }

    static byte[] ReadEntry(OdfPackage package, string path)
    {
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        using Stream input = index.OpenEntry(path);
        using MemoryStream output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }

    static OdfManifestEntry FindManifestEntry(
        OdfPackage package,
        string path)
    {
        Assert.That(package.Manifest, Is.Not.Null);
        int index = 0;
        while (index < package.Manifest!.Entries.Length)
        {
            if (package.Manifest.Entries[index].FullPath == path)
            {
                return package.Manifest.Entries[index];
            }
            index++;
        }
        Assert.Fail($"Manifest entry '{path}' was not found.");
        return new OdfManifestEntry();
    }

    static OdfPackageEntry FindPackageEntry(
        OdfPackage package,
        string path)
    {
        int index = 0;
        while (index < package.Entries.Length)
        {
            if (package.Entries[index].Path == path)
            {
                return package.Entries[index];
            }
            index++;
        }
        Assert.Fail($"Package entry '{path}' was not found.");
        return new OdfPackageEntry();
    }

    static bool PackageHasEntry(OdfPackage package, string path)
    {
        int index = 0;
        while (index < package.Entries.Length)
        {
            if (package.Entries[index].Path == path)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static string[] DiagnosticCodes(OdfDiagnostic[] diagnostics)
    {
        string[] result = new string[diagnostics.Length];
        int index = 0;
        while (index < diagnostics.Length)
        {
            result[index] = diagnostics[index].Code;
            index++;
        }
        return result;
    }

    static byte[] ReadOdfToolkitFixture()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "Fixtures",
            "encrypted-with-pwd_hello.odt.base64"));
        return Convert.FromBase64String(File.ReadAllText(path));
    }
}

public class SecurityTestOpenPgpProvider : IOdfOpenPgpKeyProvider
{
    byte[] key;

    public int CallCount;
    public OdfEncryptedKey? RequestedKey;

    public SecurityTestOpenPgpProvider(byte[] keyValue)
    {
        key = keyValue;
    }

    public byte[] GetDecryptedKey(OdfEncryptedKey encryptedKey)
    {
        CallCount++;
        RequestedKey = encryptedKey;
        return key;
    }
}
