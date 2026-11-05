using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace odf.Tests;

public class XmlSignatureWriterTests
{
    [TestCase(OdfAlgorithmIdentifiers.RsaSha1)]
    [TestCase(OdfAlgorithmIdentifiers.RsaSha256)]
    [TestCase(OdfAlgorithmIdentifiers.RsaSha384)]
    [TestCase(OdfAlgorithmIdentifiers.RsaSha512)]
    public void CreatesVerifiableRsaSignatureContainers(string signatureMethod)
    {
        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(XmlSignatureVerifierTests.RsaPrivateKeyPem);
        byte[] certificateBytes = Convert.FromBase64String(
            XmlSignatureVerifierTests.RsaCertificateBase64);
        using X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(
            certificateBytes);
        byte[] storedBytes = Encoding.UTF8.GetBytes("stored encrypted bytes");
        ZipEntryIndex package = XmlSignatureVerifierTests.CreatePackageIndex(
            "content.xml",
            storedBytes);
        package.Manifest = new OdfManifest
        {
            Entries =
            [
                new OdfManifestEntry
                {
                    FullPath = "content.xml",
                    Encryption = new OdfEncryptionInfo()
                }
            ]
        };

        OdfSignatureContainer container = XmlSignatureWriter.CreateContainer(
            package,
            OdfPartNames.DocumentSignatures,
            "1.4",
            ["content.xml"],
            rsa,
            certificate,
            signatureMethod,
            XmlSignatureVerifierTests.CreateLimits());
        OdfSignatureContainer reparsed = XmlSignatureReader.ParseBytes(
            container.OriginalBytes,
            OdfPartNames.DocumentSignatures,
            XmlSignatureVerifierTests.CreateLimits());
        XmlSignatureVerifier.VerifyContainer(
            reparsed,
            package,
            XmlSignatureVerifierTests.CreateLimits());
        byte[] ownershipProbe = rsa.SignData(
            [1, 2, 3],
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        Assert.Multiple(() =>
        {
            Assert.That(container.Version, Is.EqualTo("1.4"));
            Assert.That(container.PartPath, Is.EqualTo(OdfPartNames.DocumentSignatures));
            Assert.That(container.IsValid, Is.True);
            Assert.That(container.Signatures, Has.Length.EqualTo(1));
            Assert.That(container.Signatures[0].Status, Is.EqualTo(OdfSignatureStatus.Valid));
            Assert.That(container.Signatures[0].SignatureMethod, Is.EqualTo(signatureMethod));
            Assert.That(container.Signatures[0].References[0].Uri, Is.EqualTo("content.xml"));
            Assert.That(container.Signatures[0].References[0].UsesEncryptedPackageBytes, Is.True);
            Assert.That(container.Signatures[0].X509Data[0].Certificates[0], Is.EqualTo(certificateBytes));
            Assert.That(container.Signatures[0].X509Data[0].Digests, Has.Length.EqualTo(1));
            Assert.That(Encoding.UTF8.GetString(container.OriginalBytes), Does.StartWith("<?xml"));
            Assert.That(reparsed.Signatures[0].Status, Is.EqualTo(OdfSignatureStatus.Valid));
            Assert.That(certificate.Subject, Is.EqualTo("CN=ODF Signature Test"));
            Assert.That(ownershipProbe, Is.Not.Empty);
        });
    }

    [TestCase(OdfPartNames.DocumentSignatures, "1.2")]
    [TestCase(OdfPartNames.MacroSignatures, "1.3")]
    [TestCase(OdfPartNames.PackageSignatures, "1.4")]
    public void CreatesEveryStandardOdfSignatureFile(
        string partPath,
        string version)
    {
        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(XmlSignatureVerifierTests.RsaPrivateKeyPem);
        using X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(
            Convert.FromBase64String(
                XmlSignatureVerifierTests.RsaCertificateBase64));
        ZipEntryIndex package = XmlSignatureVerifierTests.CreatePackageIndex(
            "content.xml",
            Encoding.UTF8.GetBytes("content"));

        OdfSignatureContainer container = XmlSignatureWriter.CreateContainer(
            package,
            partPath,
            version,
            ["content.xml"],
            rsa,
            certificate,
            OdfAlgorithmIdentifiers.RsaSha256,
            XmlSignatureVerifierTests.CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(container.PartPath, Is.EqualTo(partPath));
            Assert.That(container.Version, Is.EqualTo(version));
            Assert.That(container.Signatures[0].Status, Is.EqualTo(OdfSignatureStatus.Valid));
        });
    }

    [Test]
    public void CreatesVerifiableEcdsaSignatureContainer()
    {
        using ECDsa ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(XmlSignatureVerifierTests.EcdsaPrivateKeyPem);
        using X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(
            Convert.FromBase64String(
                XmlSignatureVerifierTests.EcdsaCertificateBase64));
        ZipEntryIndex package = XmlSignatureVerifierTests.CreatePackageIndex(
            "Pictures/image.bin",
            [1, 3, 5, 7, 9]);

        OdfSignatureContainer container = XmlSignatureWriter.CreateContainer(
            package,
            OdfPartNames.DocumentSignatures,
            "1.4",
            ["Pictures/image.bin"],
            ecdsa,
            certificate,
            OdfAlgorithmIdentifiers.EcdsaSha256,
            XmlSignatureVerifierTests.CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(container.Signatures[0].Status, Is.EqualTo(OdfSignatureStatus.Valid));
            Assert.That(container.Signatures[0].SignatureValueMatches, Is.True);
            Assert.That(container.Signatures[0].SignatureMethod, Is.EqualTo(OdfAlgorithmIdentifiers.EcdsaSha256));
            Assert.That(container.Signatures[0].References[0].ResolvedPackagePath, Is.EqualTo("Pictures/image.bin"));
        });
    }

    [Test]
    public void RejectsUnsupportedMismatchedAndInvalidSigningInputs()
    {
        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(XmlSignatureVerifierTests.RsaPrivateKeyPem);
        using RSA mismatchedRsa = RSA.Create(2048);
        using ECDsa ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(XmlSignatureVerifierTests.EcdsaPrivateKeyPem);
        using X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(
            Convert.FromBase64String(
                XmlSignatureVerifierTests.RsaCertificateBase64));
        ZipEntryIndex package = XmlSignatureVerifierTests.CreatePackageIndex(
            "content.xml",
            Encoding.UTF8.GetBytes("content"));
        ZipEntryIndex selfReferencingPackage = XmlSignatureVerifierTests.CreatePackageIndex(
            OdfPartNames.DocumentSignatures,
            Encoding.UTF8.GetBytes("old signature"));
        OdfSecurityLimits smallLimits = XmlSignatureVerifierTests.CreateLimits();
        smallLimits.MaximumSingleEntryBytes = 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<OdfSecurityException>(() => CreateWith(
                package,
                ["content.xml"],
                rsa,
                certificate,
                "urn:unsupported:signature",
                "1.4",
                OdfPartNames.DocumentSignatures,
                XmlSignatureVerifierTests.CreateLimits()));
            Assert.Throws<OdfSecurityException>(() => CreateWith(
                package,
                ["content.xml"],
                rsa,
                certificate,
                OdfAlgorithmIdentifiers.HmacSha1,
                "1.4",
                OdfPartNames.DocumentSignatures,
                XmlSignatureVerifierTests.CreateLimits()));
            Assert.Throws<OdfSecurityException>(() => CreateWith(
                package,
                ["content.xml"],
                ecdsa,
                certificate,
                OdfAlgorithmIdentifiers.RsaSha256,
                "1.4",
                OdfPartNames.DocumentSignatures,
                XmlSignatureVerifierTests.CreateLimits()));
            Assert.Throws<OdfSecurityException>(() => CreateWith(
                package,
                ["content.xml"],
                mismatchedRsa,
                certificate,
                OdfAlgorithmIdentifiers.RsaSha256,
                "1.4",
                OdfPartNames.DocumentSignatures,
                XmlSignatureVerifierTests.CreateLimits()));
            Assert.Throws<OdfSecurityException>(() => CreateWith(
                package,
                ["missing.xml"],
                rsa,
                certificate,
                OdfAlgorithmIdentifiers.RsaSha256,
                "1.4",
                OdfPartNames.DocumentSignatures,
                XmlSignatureVerifierTests.CreateLimits()));
            Assert.Throws<OdfSecurityException>(() => CreateWith(
                package,
                ["content.xml", "content.xml"],
                rsa,
                certificate,
                OdfAlgorithmIdentifiers.RsaSha256,
                "1.4",
                OdfPartNames.DocumentSignatures,
                XmlSignatureVerifierTests.CreateLimits()));
            Assert.Throws<OdfSecurityException>(() => CreateWith(
                selfReferencingPackage,
                [OdfPartNames.DocumentSignatures],
                rsa,
                certificate,
                OdfAlgorithmIdentifiers.RsaSha256,
                "1.4",
                OdfPartNames.DocumentSignatures,
                XmlSignatureVerifierTests.CreateLimits()));
            Assert.Throws<OdfSecurityException>(() => CreateWith(
                package,
                [],
                rsa,
                certificate,
                OdfAlgorithmIdentifiers.RsaSha256,
                "1.4",
                OdfPartNames.DocumentSignatures,
                XmlSignatureVerifierTests.CreateLimits()));
            Assert.Throws<OdfSecurityException>(() => CreateWith(
                package,
                ["content.xml"],
                rsa,
                certificate,
                OdfAlgorithmIdentifiers.RsaSha256,
                "1.1",
                OdfPartNames.DocumentSignatures,
                XmlSignatureVerifierTests.CreateLimits()));
            Assert.Throws<OdfSecurityException>(() => CreateWith(
                package,
                ["content.xml"],
                rsa,
                certificate,
                OdfAlgorithmIdentifiers.RsaSha256,
                "1.4",
                "signatures.xml",
                XmlSignatureVerifierTests.CreateLimits()));
            Assert.Throws<OdfSecurityException>(() => CreateWith(
                package,
                ["content.xml"],
                rsa,
                certificate,
                OdfAlgorithmIdentifiers.RsaSha256,
                "1.4",
                OdfPartNames.DocumentSignatures,
                smallLimits));
        });
    }

    static OdfSignatureContainer CreateWith(
        ZipEntryIndex package,
        string[] paths,
        AsymmetricAlgorithm key,
        X509Certificate2 certificate,
        string signatureMethod,
        string version,
        string partPath,
        OdfSecurityLimits limits)
    {
        return XmlSignatureWriter.CreateContainer(
            package,
            partPath,
            version,
            paths,
            key,
            certificate,
            signatureMethod,
            limits);
    }
}
