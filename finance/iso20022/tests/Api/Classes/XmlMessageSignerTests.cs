using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using iso20022;

namespace iso20022.Tests;

public class XmlMessageSignerTests
{
    private const string PrivateKeyValue =
        "MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQDBoONZ7fN4Jew8" +
        "VNpA5PXR1qKEv/JVWBuwELTKuBY9UtIsDYl9px2EIY80ExGb/wc9E7j7nswQDWO" +
        "j86u1TznJTh4apKSJMYkPmTewyHidilKUQmLt4EAC/0oR/b+Jo8/uXygNQj/Gci3" +
        "Qj9TriS6nF1974cw8HeIChIpcfu48hsfXsEyFRL6vR4FOQwupbIh7VfCUW3EUL9" +
        "xwtsvdOCQRpSi72Chh2u6/AAT4xro4A9f1RP49X9oXKST8/uAV4c5RQ/K+uUKKK" +
        "b3aCmCTWVdTylV2leATBe9cB6Vdcrs1UdV+jG3Rq5nzTXS8SD3Y0Rh72OP4JBW6" +
        "o7nAcLa105NDAgMBAAECggEAVPZY7HwKCl1xlGOTRUhySPpKY7+qASLhO1eJTS6L" +
        "q6rsyCHEg9tRi0oCgVIgtven4HgIAnkCh8SoRSfF34/9BkT91qkJ/Z07zBlqI2tD" +
        "6bTpWV7r8/8jd203vj8+zZGQysL8Wx0HyPcO/WYvgo62ZuFNp5lDudlO89XetImJ" +
        "1gp2i4EUzPhOJn4KK4egq4aHdUg80ZAoTvTCmsvp9p7PWjhiUMA/EU4FLJ74j9gH" +
        "vpJo9VWFyW0viwUnYHnArYZ7LqLwGARL0zc+svYZImI9URo9ttEZQX0ppOuzjpYj" +
        "B6qeBCCz1OSNSqJvVE4YresMO91FsRS8LjiqqtBtIeqgkQKBgQDpKs0JW5eEYoVl" +
        "2cBc2JscHno/XyT3x+QUOMDTPfmU0pL2f9WMhvb6hBFYp34KKyGA0iC3aiWe0SWJ" +
        "LrCDwNvpbNy23bUrEoEfzLla6eBG2FnxqxhAXNLWUQr+j/MLkQQ0IkrQUBZraB+H" +
        "mcp9pEy/RFX8vR3rlK+NNWD/JG67iQKBgQDUlucWa4QpMQ/Vzuw+PwsHic7YcQkg" +
        "k/CuXJj7LBZ5KOgw45Rv4dwtm6KqJ+tMcD+4MzSzOjePp8oY1NbSIdfNHQmxM4JO" +
        "5ZB5tiOlmCMUtSopgwUNVjr5nhMW97c0GV679l1NKKD1L21T9Qz6ZgiJHskVRP2g" +
        "CARix6hfQ6BpawKBgQC2OvFMYv0kRiuj0m95IQD5FQjOBnpJ/TvQYhqx/jnVik25" +
        "wWcREVptqFI2S5FCOTtSBufy8U69tQ/1CzyBfHVjFeXaNaSmKSwpwkMLPsY+GFET" +
        "g3jMtL9DKMvC8JYvcAMtC7AU5sLe5t3diOCtGX+x7KKXiOade/NIFXPddIfmoQKB" +
        "gCqA7Tz3TBosO/1EfMlJM3xTWi6+EtQXoARMcS2ut+OLgTjdF31zC+F2f4RDrPDA" +
        "dIzwd/XpithG9MoYK8chixLwXlK2eLQrg8JB30ipbUIjooVgM69h8TKdHzX1WwSo" +
        "md88t5TIrb23uMUTdn/NzKEVraB6BbdsQuw4/Pnm/Hh3AoGAMu5Ko5J6e8tBvca" +
        "E2DyW27kjB9LCiLGtPhU3PyfcQLx8q4wGmw6o0L328XFOIhQ17xSmHRtgpSSmwmf" +
        "wutuuvGFw6gbMJ+ZZjEcPoPmsVmhzYCFqtP6NrVVX68+0bM8CgI9cTdAVGFbkg2V" +
        "6+UL1tQKCEcLHoQw++fRD42VFfyo=";

    [Test]
    public void SignsWholeDocumentDeterministicallyAndVerifiesGeneratedKeyValue()
    {
        using RSA key = RSA.Create(2048);
        MessageSigningRequest request = new MessageSigningRequest
        {
            Xml = Encoding.UTF8.GetBytes(
                "<Document><Body Id=\"body\"><Value>one</Value></Body></Document>"),
            SigningKey = key,
            InsertionPath = new XmlPathData { Value = "/Document" },
            KeyName = "generated-key"
        };
        IXmlMessageSigner service = new XmlMessageSigner();

        MessageSigningResult first = service.SignMessage(request);
        MessageSigningResult second = service.SignMessage(request);
        SignatureValidationRequest verificationRequest =
            new SignatureValidationRequest
            {
                Xml = first.Xml
            };
        verificationRequest.TrustPolicy.RequireTrustedCertificate = false;
        SignatureValidationResult verification =
            new XmlSignatureValidator().ValidateSignatures(verificationRequest);
        string signedText = Encoding.UTF8.GetString(first.Xml);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, first.ErrorMessage);
            Assert.That(first.Status, Is.EqualTo(MessageSigningStatus.Signed));
            Assert.That(first.Xml, Is.EqualTo(second.Xml));
            Assert.That(first.Signature, Is.Not.Null);
            Assert.That(first.Signature!.Status,
                Is.EqualTo(SignatureValidationStatus.Valid));
            Assert.That(first.Signature.References.Single().Uri, Is.Empty);
            Assert.That(first.Signature.References.Single().TargetPath.Value,
                Is.EqualTo("/Document"));
            Assert.That(signedText, Does.Contain("<ds:Signature"));
            Assert.That(signedText, Does.Contain("<ds:KeyName>generated-key"));
            Assert.That(signedText, Does.Contain("<ds:RSAKeyValue>"));
            Assert.That(verification.Success, Is.True, verification.ErrorMessage);
            Assert.That(verification.Valid, Is.True);
        });
    }

    [Test]
    public void SignsExplicitReferenceAndEmbedsMatchingCertificate()
    {
        using RSA key = RSA.Create();
        key.ImportPkcs8PrivateKey(
            Convert.FromBase64String(PrivateKeyValue),
            out int bytesRead);
        using X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(
            Convert.FromBase64String(XmlSignatureValidatorTests.CertificateValue));
        MessageSigningRequest request = new MessageSigningRequest
        {
            Xml = Encoding.UTF8.GetBytes(
                "<Document><Body Id=\"body\"><Value>signed</Value></Body></Document>"),
            SigningKey = key,
            Certificate = certificate,
            InsertionPath = new XmlPathData { Value = "/Document" },
            KeyName = "certificate-key"
        };
        request.ReferencePaths.Add(new XmlPathData
        {
            Value = "/Document/Body"
        });

        MessageSigningResult result = new XmlMessageSigner().SignMessage(request);
        string signedText = Encoding.UTF8.GetString(result.Xml);

        Assert.Multiple(() =>
        {
            Assert.That(bytesRead,
                Is.EqualTo(Convert.FromBase64String(PrivateKeyValue).Length));
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Status, Is.EqualTo(MessageSigningStatus.Signed));
            Assert.That(result.Signature, Is.Not.Null);
            Assert.That(result.Signature!.CertificateStatus,
                Is.EqualTo(CertificateTrustStatus.Trusted));
            Assert.That(result.Signature.References.Single().Uri,
                Is.EqualTo("#body"));
            Assert.That(result.Signature.References.Single().TargetPath.Value,
                Is.EqualTo("/Document/Body"));
            Assert.That(signedText, Does.Contain("<ds:X509Certificate>"));
            Assert.That(signedText, Does.Not.Contain("<ds:RSAKeyValue>"));
        });
    }

    [Test]
    public void RejectsMissingKeysUnsafeAlgorithmsAndInvalidLocations()
    {
        IXmlMessageSigner service = new XmlMessageSigner();
        MessageSigningResult missingKey = service.SignMessage(
            new MessageSigningRequest
            {
                Xml = "<Document/>"u8.ToArray(),
                InsertionPath = new XmlPathData { Value = "/Document" }
            });
        using RSA unsafeKey = RSA.Create(2048);
        MessageSigningResult unsafeAlgorithm = service.SignMessage(
            new MessageSigningRequest
            {
                Xml = "<Document/>"u8.ToArray(),
                SigningKey = unsafeKey,
                InsertionPath = new XmlPathData { Value = "/Document" },
                DigestAlgorithm = "http://www.w3.org/2000/09/xmldsig#sha1"
            });
        MessageSigningResult missingInsertion = service.SignMessage(
            new MessageSigningRequest
            {
                Xml = "<Document/>"u8.ToArray(),
                SigningKey = unsafeKey,
                InsertionPath = new XmlPathData { Value = "/Absent" }
            });
        MessageSigningRequest invalidReferenceRequest = new MessageSigningRequest
        {
            Xml = "<Document><Body/></Document>"u8.ToArray(),
            SigningKey = unsafeKey,
            InsertionPath = new XmlPathData { Value = "/Document" }
        };
        invalidReferenceRequest.ReferencePaths.Add(
            new XmlPathData { Value = "/Document/Body" });
        MessageSigningResult invalidReference =
            service.SignMessage(invalidReferenceRequest);

        Assert.Multiple(() =>
        {
            Assert.That(missingKey.Success, Is.False);
            Assert.That(missingKey.Status,
                Is.EqualTo(MessageSigningStatus.KeyUnavailable));
            Assert.That(missingKey.ErrorCode,
                Is.EqualTo("signing.key_unavailable"));
            Assert.That(unsafeAlgorithm.Success, Is.False);
            Assert.That(unsafeAlgorithm.Status,
                Is.EqualTo(MessageSigningStatus.UnsafeAlgorithm));
            Assert.That(unsafeAlgorithm.ErrorCode,
                Is.EqualTo("signing.algorithm_unsafe"));
            Assert.That(missingInsertion.Success, Is.False);
            Assert.That(missingInsertion.Status,
                Is.EqualTo(MessageSigningStatus.InsertionLocationNotFound));
            Assert.That(missingInsertion.Diagnostics.Single().Path.Value,
                Is.EqualTo("/Absent"));
            Assert.That(invalidReference.Success, Is.False);
            Assert.That(invalidReference.Status,
                Is.EqualTo(MessageSigningStatus.InvalidRequest));
            Assert.That(invalidReference.ErrorCode,
                Is.EqualTo("signing.reference_invalid"));
        });
    }

    [Test]
    public void SigningRequestAndPathHelperBoundariesReturnSpecificFailures()
    {
        XmlMessageSigner signer = new XmlMessageSigner();
        List<MessageSigningRequest> invalid = new List<MessageSigningRequest>
        {
            new MessageSigningRequest { Xml = null! },
            new MessageSigningRequest { Limits = null! },
            new MessageSigningRequest { InsertionPath = null! },
            new MessageSigningRequest { ReferencePaths = null! }
        };
        foreach (MessageSigningRequest request in invalid)
        {
            Assert.That(signer.SignMessage(request).ErrorCode,
                Is.EqualTo("signing.request_invalid"));
        }

        MessageSigningRequest tooLarge = new MessageSigningRequest
        {
            Xml = "<x/>"u8.ToArray(),
            Limits = new ResourceLimits { MaxTotalBytes = 1 },
            InsertionPath = new XmlPathData { Value = "/x" }
        };
        MessageSigningRequest missingPath = new MessageSigningRequest
            { Xml = "<x/>"u8.ToArray() };
        MessageSigningRequest tooManyReferences = new MessageSigningRequest
        {
            Xml = "<x/>"u8.ToArray(),
            InsertionPath = new XmlPathData { Value = "/x" }
        };
        tooManyReferences.Limits.MaxCollectionCount = 1;
        tooManyReferences.ReferencePaths.Add(new XmlPathData { Value = "/x" });
        tooManyReferences.ReferencePaths.Add(new XmlPathData { Value = "/y" });
        using System.Security.Cryptography.RSA weak =
            System.Security.Cryptography.RSA.Create(1024);
        MessageSigningRequest weakKey = new MessageSigningRequest
        {
            Xml = "<x/>"u8.ToArray(),
            InsertionPath = new XmlPathData { Value = "/x" },
            SigningKey = weak
        };
        Assert.Multiple(() =>
        {
            Assert.That(signer.SignMessage(tooLarge).ErrorCode,
                Is.EqualTo("signing.total_bytes_limit"));
            Assert.That(signer.SignMessage(missingPath).ErrorCode,
                Is.EqualTo("signing.insertion_path_missing"));
            Assert.That(signer.SignMessage(tooManyReferences).ErrorCode,
                Is.EqualTo("signing.reference_limit"));
            Assert.That(signer.SignMessage(weakKey).ErrorCode,
                Is.EqualTo("signing.key_unsafe"));
            Assert.That(XmlMessageSigner.ValidateAlgorithms(
                "urn:unsupported", SignatureAlgorithmUris.Sha256,
                SignatureAlgorithmUris.RsaSha256)?.ErrorCode,
                Is.EqualTo("signing.algorithm_unsupported"));
        });

        XmlPathData expected = new XmlPathData();
        expected.Segments.Add(new QualifiedNameData
            { LocalName = "x", NamespaceUri = "urn:test" });
        XmlPathData actual = new XmlPathData();
        Assert.That(XmlMessageSigner.SigningPathMatches(expected, actual), Is.False);
        actual.Segments.Add(new QualifiedNameData
            { LocalName = "y", NamespaceUri = "urn:test" });
        Assert.That(XmlMessageSigner.SigningPathMatches(expected, actual), Is.False);
        actual.Segments[0].LocalName = "x";
        actual.Segments[0].NamespaceUri = "urn:other";
        Assert.That(XmlMessageSigner.SigningPathMatches(expected, actual), Is.False);

        DynamicXmlNode identifiers = new DynamicXmlNode();
        identifiers.Attributes.Add(new XmlAttributeData
        {
            Name = new QualifiedNameData { LocalName = "other" },
            Value = "no"
        });
        identifiers.Attributes.Add(new XmlAttributeData
        {
            Name = new QualifiedNameData
                { LocalName = "id", NamespaceUri = Iso20022Namespaces.Xml },
            Value = "yes"
        });
        Assert.That(XmlMessageSigner.FindIdentifier(identifiers), Is.EqualTo("yes"));
        Assert.That(XmlMessageSigner.FindIdentifier(new DynamicXmlNode()), Is.Null);
        Assert.That(XmlMessageSigner.CreateLocationFailure(
            2, expected, "code", "message").Diagnostics[0].Kind,
            Is.EqualTo(DiagnosticKind.Cardinality));
    }
}
