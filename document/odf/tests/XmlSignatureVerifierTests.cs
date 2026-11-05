using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace odf.Tests;

public class XmlSignatureVerifierTests
{
    internal const string RsaPrivateKeyPem = "-----BEGIN PRIVATE KEY-----\n"
        + "MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQCeqxeN+AsxqRgx\n"
        + "FEiYkCr2eIhAthzXkDj6hcCJChCq2zSjnMJlPLyDvqENXo75u53Tkqo84E3AgwD+\n"
        + "fC4arAooYh72tDqVmZQwzNlNLivgNTJrdaS3ma1jCMYQwtyTp06dFdZ9b7qgXrCX\n"
        + "0l2iqSj3eEOUCoFa+SCIw+FWK4WXrDxOa28DK98SrRZEJkR/i+rxsMqF++PlySlF\n"
        + "HslPh5w9cU1JvarNseUFJCKQoz+ZA+JC2+oM0ekBC+eC5QMH5KSPXmWkG++QsDIv\n"
        + "mrvaQOXfwFbvzC3kqYBUTigEu0fugNoydoKNbKqz0LcfjeuQTKyUMgsZrsX1FjT+\n"
        + "1hcT3oXhAgMBAAECggEAEjG2Sr5FslUchAw6YtwtQQQDQ8BeQE22auvJby1rSdxH\n"
        + "Bffkxq0XRNgoKZtNM8/Swj8QLFjgMZIUHBprQv21UHon6Mwz55Grz4i95j7EIzqW\n"
        + "ZIIh+De0DSYlaCigGIJX3mVxqgKU2OzivRYPTKavHul4flJSEPd+RpCHl1T4RbL5\n"
        + "G4wJ7OhRKkYSJMnza7tsMyEu3DuRyTkzq5/HMqkN9EPv2rfTL4+MvPVhUHg13ssL\n"
        + "PUm8mFRQZCRezUmIxezqA4VcP96+iWOdyYkmJsDx0cp9u6Qs3qnYkmcmzlgOxtPJ\n"
        + "PTxmxYnLFvLXy+8FkdTbjSBPAkwqmApHPYT9wuhaHQKBgQDQpF3iajxfpLipZs5O\n"
        + "HKtv3BC3pjfr12OqsALzPyy3+9QoI24ZQYgpAPs6EjykA+L2wyitFVwAqaIX4JNE\n"
        + "2PNVqyec/gqEgNKvxZTr9C/2nOljp+PXD8y35N1/vHdI0r/LFhkLELNOkfzjkbXb\n"
        + "OLfuBL3/LHzmyoVlET2Kd7wYuwKBgQDCruKcZ84lPSGdlSf90sCZHHWYQl6f1Zw0\n"
        + "8H2Kj3pQGIjwvCywZb8ZpOjSfiT8kOhNbMt2ZfigEtkjWAuz6gFa3hqEefQtKTRe\n"
        + "o/hDyrVhw2Qh+tI2pTcDfvL7080jj+W1/tfCtImlsA1NlU96stx4//D+PulIUYpl\n"
        + "/gHnQscQEwKBgQCHQRaPsZjj08/WGYxCPPmyMTIxK7Ly05WL0ECJWfnuZ3VYc+dq\n"
        + "vf27vyQduDbRiKd4mnljAwa6cz1eRzqRFidg8OxkLJe64Bw00FxDPfBd6ciuzua3\n"
        + "w0lOKTKiHRYjAmRNDk07zB7BH4iWKh5/N9yckzxsEHYD02P4ad3ARvCMAwKBgHDF\n"
        + "s3kcXHqPxcgtnaPXkD9hD/FqCLwXlcvIv0AjSpvZCoJAzrZDygYF0+QStEQlgJtQ\n"
        + "+S8VdIbtRCDIIMdQNwa2/69boYkDhFWKMAa9BoKiF3/jdh+3Zn287O3ZS8xcz5i0\n"
        + "DbG4uL9kkuAtLIQL1dC1mZj9Qk7lBQF9uQaIVR1HAoGAFlndF7x3mwLYFc1B3F08\n"
        + "BELhUiSBWsVLgtQmGs1bUQRlLAjze4iKfUndIqBACpRcIx1WOlcAcjeblIpWh7g5\n"
        + "2YJ7Bzp7SpXweaO9sRXme/Dsut/qFZ8i6dFoaNHXCxvKYmm5CCBQdP4oOcuDy/tC\n"
        + "6BJ7oR1WUsvy946uW0TW+y8=\n"
        + "-----END PRIVATE KEY-----";
    internal const string RsaCertificateBase64 = "MIIDGzCCAgOgAwIBAgIUaBk7UvD8/gH58nsE72HHXpeA5ZEwDQYJKoZIhvcNAQELBQAwHTEbMBkGA1UEAwwST0RGIFNpZ25hdHVyZSBUZXN0MB4XDTI2MDkwMjAxMzgyNloXDTM2MDgzMDAxMzgyNlowHTEbMBkGA1UEAwwST0RGIFNpZ25hdHVyZSBUZXN0MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAnqsXjfgLMakYMRRImJAq9niIQLYc15A4+oXAiQoQqts0o5zCZTy8g76hDV6O+bud05KqPOBNwIMA/nwuGqwKKGIe9rQ6lZmUMMzZTS4r4DUya3Wkt5mtYwjGEMLck6dOnRXWfW+6oF6wl9Jdoqko93hDlAqBWvkgiMPhViuFl6w8TmtvAyvfEq0WRCZEf4vq8bDKhfvj5ckpRR7JT4ecPXFNSb2qzbHlBSQikKM/mQPiQtvqDNHpAQvnguUDB+Skj15lpBvvkLAyL5q72kDl38BW78wt5KmAVE4oBLtH7oDaMnaCjWyqs9C3H43rkEyslDILGa7F9RY0/tYXE96F4QIDAQABo1MwUTAfBgNVHSMEGDAWgBQJ0+A6v1CS3lPH7uDCJ8L2JAsk4zAPBgNVHRMBAf8EBTADAQH/MB0GA1UdDgQWBBQJ0+A6v1CS3lPH7uDCJ8L2JAsk4zANBgkqhkiG9w0BAQsFAAOCAQEAllQG9f4CFXhd0yqVUL+Zp2MWbSujdq5viz8vv6AtWErYzfUHwX/oN6Bc/dRsucRHWmkqyqKLU3j6rI536TV8DZDMOQjow87yvLReUiVGKCYykcO3VJd6zLtvzIBq8rSjO0dCuYlbApNEImLF6Pt1r5/U/KzHKM9O4VPGkVzAKJKzlWak/38NGLpYyL84vrrAG8TvudxajNipoXApm7cjZzVqehC1Q5bj8HzEJ5xeI/Mz5vtJ4BJFR7+MHKHGevwDZlYIGkPDN3eB+V6khGBs3yhOcRhGeKxGWULvw6pRwJ/ijdoH3ikUZg01NEI4lUyIHgKh08Qvz0cZc42ARu/yTA==";
    internal const string EcdsaPrivateKeyPem = "-----BEGIN PRIVATE KEY-----\n"
        + "MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgWtc1P0CcIWuBnlHv\n"
        + "QXlmUYQl4+Lv/n+dGd4CNSFpzOChRANCAARSW3MoReIGb/J5XUSGvIbA1tCYdT82\n"
        + "UOBVTe+Roua7XlJuRvydTCbpoeOEtYFDcY/RlZ9XCbyW6qYm/O94nASC\n"
        + "-----END PRIVATE KEY-----";
    internal const string EcdsaCertificateBase64 = "MIIBhzCCAS2gAwIBAgIUbjMth7VQ/i2xV1lBtiPGhAx6s1YwCgYIKoZIzj0EAwIwGTEXMBUGA1UEAwwOT0RGIEVDRFNBIFRlc3QwHhcNMjYwOTAyMDEzODI2WhcNMzYwODMwMDEzODI2WjAZMRcwFQYDVQQDDA5PREYgRUNEU0EgVGVzdDBZMBMGByqGSM49AgEGCCqGSM49AwEHA0IABFJbcyhF4gZv8nldRIa8hsDW0Jh1PzZQ4FVN75Gi5rteUm5G/J1MJumh44S1gUNxj9GVn1cJvJbqpib873icBIKjUzBRMB0GA1UdDgQWBBR4e9Hey3awm6ZeChyASaDn2LjewTAfBgNVHSMEGDAWgBR4e9Hey3awm6ZeChyASaDn2LjewTAPBgNVHRMBAf8EBTADAQH/MAoGCCqGSM49BAMCA0gAMEUCIQCobxNkLe/FEWTwtvYlgqJ7b81xMF3ojqiPq7QbmNu8MAIgSHvXk13JPnDrGoya988oOipAZHoypeAlzqjadi1iTF8=";

    [Test]
    public void VerifiesRsaCertificateSelectorsAndEncryptedStoredBytes()
    {
        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(RsaPrivateKeyPem);
        byte[] certificateBytes = Convert.FromBase64String(RsaCertificateBase64);
        using X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(
            certificateBytes);
        byte[] storedCiphertext = [191, 16, 32, 48, 64, 80, 96, 112];
        byte[] plaintext = Encoding.UTF8.GetBytes("unencrypted content");
        string keyInfo = BuildCompleteKeyInfo(
            certificate,
            certificateBytes,
            Convert.FromHexString("09D3E03ABF5092DE53C7EEE0C227C2F6240B24E3"));
        string ciphertextReference = BuildReference(
            "secret.bin",
            OdfAlgorithmIdentifiers.Sha256,
            SHA256.HashData(storedCiphertext),
            "");
        string validXml = SignRsaContainer(
            rsa,
            OdfAlgorithmIdentifiers.CanonicalXml10,
            OdfAlgorithmIdentifiers.RsaSha256,
            ciphertextReference,
            keyInfo,
            "");
        string plaintextReference = BuildReference(
            "secret.bin",
            OdfAlgorithmIdentifiers.Sha256,
            SHA256.HashData(plaintext),
            "");
        string wrongOrderXml = SignRsaContainer(
            rsa,
            OdfAlgorithmIdentifiers.CanonicalXml10,
            OdfAlgorithmIdentifiers.RsaSha256,
            plaintextReference,
            keyInfo,
            "");
        ZipEntryIndex package = CreatePackageIndex("secret.bin", storedCiphertext);
        package.Manifest = new OdfManifest
        {
            Entries =
            [
                new OdfManifestEntry
                {
                    FullPath = "secret.bin",
                    Encryption = new OdfEncryptionInfo()
                }
            ]
        };
        OdfSignatureContainer valid = ParseContainer(validXml);
        OdfSignatureContainer wrongOrder = ParseContainer(wrongOrderXml);
        valid.Signatures[0].ManifestReferences =
        [
            new OdfSignatureReference
            {
                Uri = "secret.bin",
                HasUri = true,
                IsPackageReference = true,
                DigestMethod = OdfAlgorithmIdentifiers.Sha256,
                DigestValue = SHA256.HashData(storedCiphertext),
                DigestValueIsValid = true,
                IsValid = true
            }
        ];

        XmlSignatureVerifier.VerifyContainer(valid, package, CreateLimits());
        XmlSignatureVerifier.VerifyContainer(wrongOrder, package, CreateLimits());

        OdfSignature signature = valid.Signatures[0];
        OdfSignatureReference reference = signature.References[0];
        Assert.Multiple(() =>
        {
            Assert.That(signature.Status, Is.EqualTo(OdfSignatureStatus.Valid));
            Assert.That(signature.ReferencesAreValid, Is.True);
            Assert.That(signature.CertificateMaterialIsValid, Is.True);
            Assert.That(signature.SignatureValueMatches, Is.True);
            Assert.That(signature.SigningCertificateSubject, Is.EqualTo(certificate.Subject));
            Assert.That(signature.SigningCertificateThumbprint, Is.EqualTo(certificate.Thumbprint));
            Assert.That(reference.Status, Is.EqualTo(OdfSignatureStatus.Valid));
            Assert.That(reference.IsVerified, Is.True);
            Assert.That(reference.DigestMatches, Is.True);
            Assert.That(reference.ResolvedPackagePath, Is.EqualTo("secret.bin"));
            Assert.That(reference.UsesEncryptedPackageBytes, Is.True);
            Assert.That(signature.ManifestReferences[0].Status, Is.EqualTo(OdfSignatureStatus.Valid));
            Assert.That(signature.ManifestReferences[0].UsesEncryptedPackageBytes, Is.True);
            Assert.That(wrongOrder.Signatures[0].Status, Is.EqualTo(OdfSignatureStatus.DigestMismatch));
            Assert.That(wrongOrder.Signatures[0].References[0].UsesEncryptedPackageBytes, Is.True);
        });
    }

    [Test]
    public void VerifiesSameDocumentXadesDigestAndEcdsaSignature()
    {
        using ECDsa ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(EcdsaPrivateKeyPem);
        byte[] certificateBytes = Convert.FromBase64String(EcdsaCertificateBase64);
        string objectXml = "<ds:Object><xades:QualifyingProperties Target='#sig'>"
            + "<xades:SignedProperties Id='signed-properties'>"
            + "<xades:SignedSignatureProperties/></xades:SignedProperties>"
            + "</xades:QualifyingProperties></ds:Object>";
        string placeholderReference = BuildReference(
            "#signed-properties",
            OdfAlgorithmIdentifiers.Sha256,
            [1],
            "<ds:Transforms><ds:Transform Algorithm='"
                + OdfAlgorithmIdentifiers.CanonicalXml10
                + "'/></ds:Transforms>");
        string placeholder = BuildContainer(
            OdfAlgorithmIdentifiers.CanonicalXml10,
            OdfAlgorithmIdentifiers.EcdsaSha256,
            placeholderReference,
            BuildCertificateKeyInfo(certificateBytes),
            objectXml,
            "AQ==");
        OdfSignatureContainer parsedPlaceholder = ParseContainer(placeholder);
        XmlElement signedProperties = FindElement(
            parsedPlaceholder.Signatures[0].SignatureElement,
            OdfNamespaces.Xades,
            "SignedProperties");
        byte[] canonicalProperties = XmlCanonicalizer.CanonicalizeElement(
            signedProperties,
            OdfAlgorithmIdentifiers.CanonicalXml10,
            "",
            CreateLimits());
        string reference = BuildReference(
            "#signed-properties",
            OdfAlgorithmIdentifiers.Sha256,
            SHA256.HashData(canonicalProperties),
            "<ds:Transforms><ds:Transform Algorithm='"
                + OdfAlgorithmIdentifiers.CanonicalXml10
                + "'/></ds:Transforms>");
        string xml = SignEcdsaContainer(
            ecdsa,
            reference,
            BuildCertificateKeyInfo(certificateBytes),
            objectXml);
        OdfSignatureContainer container = ParseContainer(xml);
        ZipEntryIndex package = CreatePackageIndex("unused.bin", [0]);

        XmlSignatureVerifier.VerifyContainer(container, package, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(container.Signatures[0].Status, Is.EqualTo(OdfSignatureStatus.Valid));
            Assert.That(container.Signatures[0].SignatureValueMatches, Is.True);
            Assert.That(container.Signatures[0].References[0].Status, Is.EqualTo(OdfSignatureStatus.Valid));
            Assert.That(container.Signatures[0].References[0].IsVerified, Is.True);
        });
    }

    [TestCase("canonicalization")]
    [TestCase("signature")]
    [TestCase("digest")]
    [TestCase("transform")]
    public void ReportsUnsupportedAlgorithmsExplicitly(string unsupportedPart)
    {
        byte[] data = Encoding.UTF8.GetBytes("content");
        string canonicalization = unsupportedPart == "canonicalization"
            ? "urn:unsupported:canonicalization"
            : OdfAlgorithmIdentifiers.CanonicalXml10;
        string signatureMethod = unsupportedPart == "signature"
            ? "urn:unsupported:signature"
            : OdfAlgorithmIdentifiers.RsaSha256;
        string digestMethod = unsupportedPart == "digest"
            ? "urn:unsupported:digest"
            : OdfAlgorithmIdentifiers.Sha256;
        string transforms = unsupportedPart == "transform"
            ? "<ds:Transforms><ds:Transform Algorithm='urn:unsupported:transform'/></ds:Transforms>"
            : "";
        string reference = BuildReference(
            "content.xml",
            digestMethod,
            SHA256.HashData(data),
            transforms);
        string xml = BuildContainer(
            canonicalization,
            signatureMethod,
            reference,
            "",
            "",
            "AQ==");
        OdfSignatureContainer container = ParseContainer(xml);
        ZipEntryIndex package = CreatePackageIndex("content.xml", data);

        XmlSignatureVerifier.VerifyContainer(container, package, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(container.Signatures[0].Status, Is.EqualTo(OdfSignatureStatus.UnsupportedAlgorithm));
            Assert.That(
                container.Signatures[0].Diagnostics[^1].Code,
                Is.EqualTo(OdfDiagnosticCodes.SignatureAlgorithmUnsupported));
        });
    }

    [TestCase("missing.xml", false, OdfSignatureStatus.MissingReference)]
    [TestCase("content.xml", true, OdfSignatureStatus.DigestMismatch)]
    public void ReportsMissingAndMismatchedReferences(
        string path,
        bool useWrongDigest,
        OdfSignatureStatus expectedStatus)
    {
        byte[] data = Encoding.UTF8.GetBytes("content");
        byte[] digest = useWrongDigest ? SHA256.HashData([9, 9, 9]) : SHA256.HashData(data);
        string reference = BuildReference(
            path,
            OdfAlgorithmIdentifiers.Sha256,
            digest,
            "");
        OdfSignatureContainer container = ParseContainer(BuildContainer(
            OdfAlgorithmIdentifiers.CanonicalXml10,
            OdfAlgorithmIdentifiers.RsaSha256,
            reference,
            "",
            "",
            "AQ=="));
        ZipEntryIndex package = CreatePackageIndex("content.xml", data);

        XmlSignatureVerifier.VerifyContainer(container, package, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(container.Signatures[0].Status, Is.EqualTo(expectedStatus));
            Assert.That(container.Signatures[0].References[0].Status, Is.EqualTo(expectedStatus));
        });
    }

    [Test]
    public void RejectsMalformedCertificateMaterialWithCertificateStatus()
    {
        byte[] data = Encoding.UTF8.GetBytes("content");
        string reference = BuildReference(
            "content.xml",
            OdfAlgorithmIdentifiers.Sha256,
            SHA256.HashData(data),
            "");
        string keyInfo = BuildCertificateKeyInfo(Encoding.UTF8.GetBytes("not a certificate"));
        OdfSignatureContainer container = ParseContainer(BuildContainer(
            OdfAlgorithmIdentifiers.CanonicalXml10,
            OdfAlgorithmIdentifiers.RsaSha256,
            reference,
            keyInfo,
            "",
            "AQ=="));
        ZipEntryIndex package = CreatePackageIndex("content.xml", data);

        XmlSignatureVerifier.VerifyContainer(container, package, CreateLimits());

        Assert.That(
            container.Signatures[0].Status,
            Is.EqualTo(OdfSignatureStatus.CertificateInvalid));
    }

    [Test]
    public void RejectsAmbiguousSameDocumentIdentifiers()
    {
        string reference = BuildReference(
            "#duplicate",
            OdfAlgorithmIdentifiers.Sha256,
            SHA256.HashData(Encoding.UTF8.GetBytes("unused")),
            "");
        string objects = "<ds:Object><item xmlns='urn:test' Id='duplicate'/></ds:Object>"
            + "<ds:Object><item xmlns='urn:test' Id='duplicate'/></ds:Object>";
        OdfSignatureContainer container = ParseContainer(BuildContainer(
            OdfAlgorithmIdentifiers.CanonicalXml10,
            OdfAlgorithmIdentifiers.RsaSha256,
            reference,
            "",
            objects,
            "AQ=="));
        ZipEntryIndex package = CreatePackageIndex("unused.bin", [0]);

        XmlSignatureVerifier.VerifyContainer(container, package, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(container.Signatures[0].Status, Is.EqualTo(OdfSignatureStatus.Invalid));
            Assert.That(container.Signatures[0].References[0].Status, Is.EqualTo(OdfSignatureStatus.Invalid));
        });
    }

    [Test]
    public void ReportsUnsupportedX509DigestAndInvalidSignatureValue()
    {
        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(RsaPrivateKeyPem);
        byte[] certificateBytes = Convert.FromBase64String(RsaCertificateBase64);
        byte[] data = Encoding.UTF8.GetBytes("content");
        string reference = BuildReference(
            "content.xml",
            OdfAlgorithmIdentifiers.Sha256,
            SHA256.HashData(data),
            "");
        string unsupportedKeyInfo = "<ds:KeyInfo><ds:X509Data>"
            + "<ds11:X509Digest Algorithm='urn:unsupported:certificate-digest'>AQ==</ds11:X509Digest>"
            + "<ds:X509Certificate>"
            + Convert.ToBase64String(certificateBytes)
            + "</ds:X509Certificate></ds:X509Data></ds:KeyInfo>";
        OdfSignatureContainer unsupported = ParseContainer(BuildContainer(
            OdfAlgorithmIdentifiers.CanonicalXml10,
            OdfAlgorithmIdentifiers.RsaSha256,
            reference,
            unsupportedKeyInfo,
            "",
            "AQ=="));
        OdfSignatureContainer invalidSignature = ParseContainer(BuildContainer(
            OdfAlgorithmIdentifiers.CanonicalXml10,
            OdfAlgorithmIdentifiers.RsaSha256,
            reference,
            BuildCertificateKeyInfo(certificateBytes),
            "",
            Convert.ToBase64String(new byte[256])));
        ZipEntryIndex package = CreatePackageIndex("content.xml", data);

        XmlSignatureVerifier.VerifyContainer(unsupported, package, CreateLimits());
        XmlSignatureVerifier.VerifyContainer(invalidSignature, package, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Signatures[0].Status, Is.EqualTo(OdfSignatureStatus.UnsupportedAlgorithm));
            Assert.That(invalidSignature.Signatures[0].Status, Is.EqualTo(OdfSignatureStatus.Invalid));
            Assert.That(invalidSignature.Signatures[0].CertificateMaterialIsValid, Is.True);
            Assert.That(invalidSignature.Signatures[0].SignatureValueMatches, Is.False);
        });
    }

    static string SignRsaContainer(
        RSA rsa,
        string canonicalization,
        string signatureMethod,
        string references,
        string keyInfo,
        string objectXml)
    {
        string placeholder = BuildContainer(
            canonicalization,
            signatureMethod,
            references,
            keyInfo,
            objectXml,
            "AQ==");
        OdfSignatureContainer parsed = ParseContainer(placeholder);
        byte[] signedInfo = XmlCanonicalizer.CanonicalizeElement(
            parsed.Signatures[0].SignedInfoElement!,
            canonicalization,
            "",
            CreateLimits());
        byte[] signature = rsa.SignData(
            signedInfo,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return BuildContainer(
            canonicalization,
            signatureMethod,
            references,
            keyInfo,
            objectXml,
            Convert.ToBase64String(signature));
    }

    static string SignEcdsaContainer(
        ECDsa ecdsa,
        string references,
        string keyInfo,
        string objectXml)
    {
        string placeholder = BuildContainer(
            OdfAlgorithmIdentifiers.CanonicalXml10,
            OdfAlgorithmIdentifiers.EcdsaSha256,
            references,
            keyInfo,
            objectXml,
            "AQ==");
        OdfSignatureContainer parsed = ParseContainer(placeholder);
        byte[] signedInfo = XmlCanonicalizer.CanonicalizeElement(
            parsed.Signatures[0].SignedInfoElement!,
            OdfAlgorithmIdentifiers.CanonicalXml10,
            "",
            CreateLimits());
        byte[] signature = ecdsa.SignData(
            signedInfo,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return BuildContainer(
            OdfAlgorithmIdentifiers.CanonicalXml10,
            OdfAlgorithmIdentifiers.EcdsaSha256,
            references,
            keyInfo,
            objectXml,
            Convert.ToBase64String(signature));
    }

    static string BuildContainer(
        string canonicalization,
        string signatureMethod,
        string references,
        string keyInfo,
        string objectXml,
        string signatureValue)
    {
        return "<dsig:document-signatures xmlns:dsig='"
            + OdfNamespaces.DigitalSignature
            + "' xmlns:ds='"
            + OdfNamespaces.XmlDigitalSignature
            + "' xmlns:ds11='http://www.w3.org/2009/xmldsig11#' xmlns:xades='"
            + OdfNamespaces.Xades
            + "' dsig:version='1.4'><ds:Signature Id='sig'><ds:SignedInfo>"
            + "<ds:CanonicalizationMethod Algorithm='"
            + canonicalization
            + "'/><ds:SignatureMethod Algorithm='"
            + signatureMethod
            + "'/>"
            + references
            + "</ds:SignedInfo><ds:SignatureValue>"
            + signatureValue
            + "</ds:SignatureValue>"
            + keyInfo
            + objectXml
            + "</ds:Signature></dsig:document-signatures>";
    }

    static string BuildReference(
        string uri,
        string digestMethod,
        byte[] digest,
        string transforms)
    {
        return "<ds:Reference URI='"
            + uri
            + "'>"
            + transforms
            + "<ds:DigestMethod Algorithm='"
            + digestMethod
            + "'/><ds:DigestValue>"
            + Convert.ToBase64String(digest)
            + "</ds:DigestValue></ds:Reference>";
    }

    static string BuildCertificateKeyInfo(byte[] certificate)
    {
        return "<ds:KeyInfo><ds:X509Data><ds:X509Certificate>"
            + Convert.ToBase64String(certificate)
            + "</ds:X509Certificate></ds:X509Data></ds:KeyInfo>";
    }

    static string BuildCompleteKeyInfo(
        X509Certificate2 certificate,
        byte[] certificateBytes,
        byte[] subjectKeyIdentifier)
    {
        byte[] certificateDigest = SHA256.HashData(certificateBytes);
        return "<ds:KeyInfo><ds:X509Data><ds:X509IssuerSerial>"
            + "<ds:X509IssuerName>"
            + certificate.Issuer
            + "</ds:X509IssuerName><ds:X509SerialNumber>"
            + CertificateSerial(certificate)
            + "</ds:X509SerialNumber></ds:X509IssuerSerial><ds:X509SubjectName>"
            + certificate.Subject
            + "</ds:X509SubjectName><ds:X509SKI>"
            + Convert.ToBase64String(subjectKeyIdentifier)
            + "</ds:X509SKI><ds11:X509Digest Algorithm='"
            + OdfAlgorithmIdentifiers.Sha256
            + "'>"
            + Convert.ToBase64String(certificateDigest)
            + "</ds11:X509Digest><ds:X509Certificate>"
            + Convert.ToBase64String(certificateBytes)
            + "</ds:X509Certificate></ds:X509Data></ds:KeyInfo>";
    }

    static string CertificateSerial(X509Certificate2 certificate)
    {
        BigInteger serial = new BigInteger(certificate.GetSerialNumber(), true, false);
        return serial.ToString(CultureInfo.InvariantCulture);
    }

    static OdfSignatureContainer ParseContainer(string xml)
    {
        return XmlSignatureReader.ParseBytes(
            Encoding.UTF8.GetBytes(xml),
            OdfPartNames.DocumentSignatures,
            CreateLimits());
    }

    static XmlElement FindElement(
        XmlElement? root,
        string namespaceName,
        string localName)
    {
        Assert.That(root, Is.Not.Null);
        XmlNodeList matches = root!.GetElementsByTagName(localName, namespaceName);
        Assert.That(matches.Count, Is.EqualTo(1));
        return (XmlElement)matches[0]!;
    }

    internal static ZipEntryIndex CreatePackageIndex(string path, byte[] data)
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(
            output,
            ZipArchiveMode.Create,
            true))
        {
            WriteArchiveEntry(
                archive,
                OdfPartNames.Mimetype,
                Encoding.UTF8.GetBytes(OdfMediaTypes.Text),
                CompressionLevel.NoCompression);
            string manifest = "<?xml version='1.0' encoding='UTF-8'?><manifest:manifest xmlns:manifest='"
                + OdfNamespaces.Manifest
                + "' manifest:version='1.4'><manifest:file-entry manifest:full-path='/' manifest:media-type='"
                + OdfMediaTypes.Text
                + "'/><manifest:file-entry manifest:full-path='"
                + path
                + "' manifest:media-type='application/octet-stream'/></manifest:manifest>";
            WriteArchiveEntry(
                archive,
                OdfPartNames.Manifest,
                Encoding.UTF8.GetBytes(manifest),
                CompressionLevel.Optimal);
            WriteArchiveEntry(archive, path, data, CompressionLevel.Optimal);
        }
        return ZipEntryIndex.CreateBytes(output.ToArray());
    }

    static void WriteArchiveEntry(
        ZipArchive archive,
        string path,
        byte[] data,
        CompressionLevel compression)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compression);
        using Stream stream = entry.Open();
        stream.Write(data, 0, data.Length);
    }

    internal static OdfSecurityLimits CreateLimits()
    {
        OdfSecurityLimits result = new OdfSecurityLimits();
        result.MaximumSingleEntryBytes = 1048576;
        result.MaximumXmlDepth = 64;
        result.MaximumXmlAttributesPerElement = 64;
        result.MaximumXmlTextLength = 1048576;
        return result;
    }
}
