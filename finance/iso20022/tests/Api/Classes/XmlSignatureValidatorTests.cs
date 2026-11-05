using System.Text;
using iso20022;

namespace iso20022.Tests;

public class XmlSignatureValidatorTests
{
    private const string SignatureNamespace =
        "http://www.w3.org/2000/09/xmldsig#";
    private const string ExclusiveCanonicalization =
        "http://www.w3.org/2001/10/xml-exc-c14n#";
    private const string EnvelopedSignature =
        "http://www.w3.org/2000/09/xmldsig#enveloped-signature";
    private const string Sha256 =
        "http://www.w3.org/2001/04/xmlenc#sha256";
    private const string RsaSha256 =
        "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
    private const string DigestValue =
        "ypwmXgxp4kH0laTR9Hd2RKTUUhNhA0yOMaVq8fIL7og=";
    private const string SignatureValue =
        "owcY937EWBX3aiiCJcPp3T7OkYkJ4ZhHf8eC4w6j7Zq4VmV9ih4IPisO95+YuEAg" +
        "6tjlepTx/QEypLfhNSvAqa7Lz2vP6lHZXFC727zdIL4oWryxnFiRWk0hN5/GSS1A" +
        "qwU0y3NbsssvVIffe8xYXxN49MF2ONz4up+Ir2DejJPNI3ltAuvyVSJ9ivzCK/G" +
        "WqgI9pLpAZcUlITHGQJJcUvOe5qXhslRaH7WdJmnjsedkH1PgNTrL6FJWrkYY9E" +
        "8KQBz9inJIty7K8epmIeOmmTgCy/VyofNClMAYLj6/zLCI98/RAd4ANLKuHSAD3K" +
        "hUSi6j/R5mDdl1xLjgvSVIlQ==";
    internal const string CertificateValue =
        "MIIDJzCCAg+gAwIBAgIUaKMcmgsp6dJYa2B4nCpSTsQNoXQwDQYJKoZIhvcNAQEL" +
        "BQAwIzEhMB8GA1UEAwwYSVNPIDIwMDIyIFNpZ25hdHVyZSBUZXN0MB4XDTI2MDkw" +
        "MTA4MjY0MVoXDTM2MDgyOTA4MjY0MVowIzEhMB8GA1UEAwwYSVNPIDIwMDIyIFNp" +
        "Z25hdHVyZSBUZXN0MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwaDj" +
        "We3zeCXsPFTaQOT10daihL/yVVgbsBC0yrgWPVLSLA2JfacdhCGPNBMRm/8HPRO4" +
        "+57MEA1jo/OrtU85yU4eGqSkiTGJD5k3sMh4nYpSlEJi7eBAAv9KEf2/iaPP7l8" +
        "oDUI/xnIt0I/U64kupxdfe+HMPB3iAoSKXH7uPIbH17BMhUS+r0eBTkMLqWyIe1X" +
        "wlFtxFC/ccLbL3TgkEaUou9goYdruvwAE+Ma6OAPX9UT+PV/aFykk/P7gFeHOUUP" +
        "yvrlCiim92gpgk1lXU8pVdpXgEwXvXAelXXK7NVHVfoxt0auZ8010vEg92NEYe9" +
        "jj+CQVuqO5wHC2tdOTQwIDAQABo1MwUTAdBgNVHQ4EFgQUpnSSyGfeFtmMesRvBs" +
        "eex200KmAwHwYDVR0jBBgwFoAUpnSSyGfeFtmMesRvBseex200KmAwDwYDVR0TAQH" +
        "/BAUwAwEB/zANBgkqhkiG9w0BAQsFAAOCAQEAYtmuIctTm6fD00ohZJiS/ig9rGUT" +
        "q1OhjdSAQDzlBbmtoHQF+vZP/0xdRgJxUpKs9I1H1UBCH3OjYEqYxN8uJONDAwNo" +
        "HfSQ8ZinzOMUBUaYJ/ESvPrmvZLmlwJIo9j3euyFAYXjqDTtLtYtj93AQkDsrG/I" +
        "wfmcSbDPmZz3r8Tuu1l5aoj8kiFKy7l6aGDr9oBk66tA63mNaoMSpTFEzK6DnJO" +
        "Zc/gHnC1D8Z79Z6yrlR+Z484tT3TJxFUon1VbbP70QBH0SkdS/9PrzDFTt8c7gb" +
        "Q2KCPBcWX1f2AMRzVGDsFR4T7vW5Tqwyt66C+Lr7EODv8bA33bwRVZRufGMA==";

    [Test]
    public void ValidatesContainedEnvelopedSignatureAndTrustedCertificate()
    {
        SignedFixture fixture = CreateSignedFixture();
        SignatureValidationRequest request = CreateTrustedRequest(fixture);
        request.RedactCertificateIdentifiers = false;
        IXmlSignatureValidator service = new XmlSignatureValidator();

        SignatureValidationResult result = service.ValidateSignatures(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Status, Is.EqualTo(SignatureValidationStatus.Valid));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Signatures, Has.Count.EqualTo(1));
            Assert.That(result.Signatures[0].Identifier, Is.EqualTo("signature-1"));
            Assert.That(result.Signatures[0].Path.Value,
                Does.EndWith("/Q{" + SignatureNamespace + "}Signature"));
            Assert.That(result.Signatures[0].CanonicalizationAlgorithm,
                Is.EqualTo(ExclusiveCanonicalization));
            Assert.That(result.Signatures[0].SignatureAlgorithm,
                Is.EqualTo(RsaSha256));
            Assert.That(result.Signatures[0].CertificateStatus,
                Is.EqualTo(CertificateTrustStatus.Trusted));
            Assert.That(result.Signatures[0].CertificateIdentifier,
                Does.StartWith("sha256:"));
            Assert.That(result.Signatures[0].References.Single().Status,
                Is.EqualTo(SignatureReferenceStatus.Valid));
            Assert.That(result.Signatures[0].References.Single().ContainedInMessage,
                Is.True);
            Assert.That(result.Signatures[0].References.Single().TargetPath.Value,
                Is.EqualTo("/Document"));
            Assert.That(result.Signatures[0].References.Single().ExpectedDigest.Value,
                Is.EqualTo(result.Signatures[0].References.Single().CalculatedDigest.Value));
        });
    }

    [Test]
    public void ReportsTamperingAndUntrustedCertificatesSeparately()
    {
        SignedFixture fixture = CreateSignedFixture();
        SignatureValidationRequest tamperedRequest = CreateTrustedRequest(fixture);
        tamperedRequest.Xml = Encoding.UTF8.GetBytes(
            fixture.Xml.Replace("original", "tampered"));
        SignatureValidationRequest untrustedRequest =
            new SignatureValidationRequest
            {
                Xml = Encoding.UTF8.GetBytes(fixture.Xml)
            };
        untrustedRequest.TrustPolicy.UseSystemTrustStore = false;
        untrustedRequest.TrustPolicy.CheckCertificateRevocation = false;
        IXmlSignatureValidator service = new XmlSignatureValidator();

        SignatureValidationResult tampered =
            service.ValidateSignatures(tamperedRequest);
        SignatureValidationResult untrusted =
            service.ValidateSignatures(untrustedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(tampered.Success, Is.True, tampered.ErrorMessage);
            Assert.That(tampered.Valid, Is.False);
            Assert.That(tampered.Status,
                Is.EqualTo(SignatureValidationStatus.ReferenceInvalid));
            Assert.That(tampered.Signatures.Single().References.Single().Status,
                Is.EqualTo(SignatureReferenceStatus.DigestMismatch));
            Assert.That(tampered.Diagnostics.Single().Code,
                Is.EqualTo("signature.reference_digest_mismatch"));
            Assert.That(untrusted.Success, Is.True, untrusted.ErrorMessage);
            Assert.That(untrusted.Valid, Is.False);
            Assert.That(untrusted.Status,
                Is.EqualTo(SignatureValidationStatus.CertificateUntrusted));
            Assert.That(untrusted.Signatures.Single().CertificateStatus,
                Is.EqualTo(CertificateTrustStatus.Untrusted));
            Assert.That(untrusted.Diagnostics.Single().Code,
                Is.EqualTo("signature.certificate_untrusted"));
        });
    }

    [Test]
    public void RejectsUnsafeAlgorithmsAndExternalReferencesWithoutExecution()
    {
        SignedFixture fixture = CreateSignedFixture();
        SignatureValidationRequest unsafeRequest = CreateTrustedRequest(fixture);
        unsafeRequest.Xml = Encoding.UTF8.GetBytes(
            fixture.Xml.Replace(
                RsaSha256,
                "http://www.w3.org/2000/09/xmldsig#rsa-sha1"));
        SignatureValidationRequest externalRequest = CreateTrustedRequest(fixture);
        externalRequest.Xml = Encoding.UTF8.GetBytes(
            fixture.Xml.Replace("URI=\"#message\"", "URI=\"https://example.test/key\""));
        IXmlSignatureValidator service = new XmlSignatureValidator();

        SignatureValidationResult unsafeResult =
            service.ValidateSignatures(unsafeRequest);
        SignatureValidationResult external =
            service.ValidateSignatures(externalRequest);

        Assert.Multiple(() =>
        {
            Assert.That(unsafeResult.Success, Is.True, unsafeResult.ErrorMessage);
            Assert.That(unsafeResult.Valid, Is.False);
            Assert.That(unsafeResult.Status,
                Is.EqualTo(SignatureValidationStatus.UnsafeAlgorithm));
            Assert.That(unsafeResult.Diagnostics.Single().Code,
                Is.EqualTo("signature.algorithm_unsafe"));
            Assert.That(external.Success, Is.True, external.ErrorMessage);
            Assert.That(external.Valid, Is.False);
            Assert.That(external.Signatures.Single().References.Single().Status,
                Is.EqualTo(SignatureReferenceStatus.ExternalReferenceRejected));
            Assert.That(external.Diagnostics.Single().Code,
                Is.EqualTo("signature.reference_external_rejected"));
        });
    }

    [Test]
    public void ReportsRequiredMissingMalformedAndAmbiguousTargets()
    {
        IXmlSignatureValidator service = new XmlSignatureValidator();
        SignatureValidationResult missing = service.ValidateSignatures(
            new SignatureValidationRequest
            {
                Xml = "<Document/>"u8.ToArray(),
                RequireSignature = true
            });
        SignatureValidationResult malformed = service.ValidateSignatures(
            new SignatureValidationRequest
            {
                Xml = Encoding.UTF8.GetBytes(
                    "<Document xmlns:ds=\"" + SignatureNamespace + "\">" +
                    "<ds:Signature/></Document>")
            });
        SignatureValidationRequest boundedRequest = new SignatureValidationRequest
        {
            Xml = "<Document/>"u8.ToArray()
        };
        boundedRequest.Limits.MaxTotalBytes = 1;
        SignatureValidationResult bounded =
            service.ValidateSignatures(boundedRequest);
        SignedFixture fixture = CreateSignedFixture();
        SignatureValidationRequest ambiguousRequest = CreateTrustedRequest(fixture);
        ambiguousRequest.Xml = Encoding.UTF8.GetBytes(
            fixture.Xml.Replace(
                "<Value>original</Value>",
                "<Value Id=\"message\">original</Value>"));
        SignatureValidationResult ambiguous =
            service.ValidateSignatures(ambiguousRequest);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.True, missing.ErrorMessage);
            Assert.That(missing.Valid, Is.False);
            Assert.That(missing.Status,
                Is.EqualTo(SignatureValidationStatus.NotPresent));
            Assert.That(missing.Diagnostics.Single().Code,
                Is.EqualTo("signature.required_missing"));
            Assert.That(malformed.Success, Is.True, malformed.ErrorMessage);
            Assert.That(malformed.Status,
                Is.EqualTo(SignatureValidationStatus.Malformed));
            Assert.That(malformed.Diagnostics.Single().Code,
                Is.EqualTo("signature.markup_malformed"));
            Assert.That(bounded.Success, Is.False);
            Assert.That(bounded.Status,
                Is.EqualTo(SignatureValidationStatus.ResourceLimitReached));
            Assert.That(bounded.ErrorCode,
                Is.EqualTo("signature.total_bytes_limit"));
            Assert.That(ambiguous.Success, Is.True, ambiguous.ErrorMessage);
            Assert.That(ambiguous.Valid, Is.False);
            Assert.That(ambiguous.Signatures.Single().References.Single().Status,
                Is.EqualTo(SignatureReferenceStatus.AmbiguousTarget));
            Assert.That(ambiguous.Diagnostics.Single().Code,
                Is.EqualTo("signature.reference_target_ambiguous"));
        });
    }

    internal static SignatureValidationRequest CreateTrustedRequest(
        SignedFixture fixture)
    {
        SignatureValidationRequest request = new SignatureValidationRequest
        {
            Xml = Encoding.UTF8.GetBytes(fixture.Xml)
        };
        request.TrustPolicy.TrustedCertificates.Add(fixture.Certificate);
        request.TrustPolicy.UseSystemTrustStore = false;
        request.TrustPolicy.CheckCertificateRevocation = false;
        return request;
    }

    [Test]
    public void RequestAlgorithmAndLookupHelpersCoverBoundaryPolicies()
    {
        XmlSignatureValidator validator = new XmlSignatureValidator();
        SignatureValidationRequest nullXml = new SignatureValidationRequest { Xml = null! };
        SignatureValidationRequest nullPolicy = new SignatureValidationRequest
            { TrustPolicy = null! };
        SignatureValidationRequest nullLimits = new SignatureValidationRequest
            { Limits = null! };
        SignatureValidationRequest nullAnchors = new SignatureValidationRequest();
        nullAnchors.TrustPolicy.TrustedCertificates = null!;
        SignatureValidationRequest tooLarge = new SignatureValidationRequest
        {
            Xml = "<x/>"u8.ToArray(),
            Limits = new ResourceLimits { MaxTotalBytes = 1 }
        };
        Assert.Multiple(() =>
        {
            Assert.That(validator.ValidateSignatures(nullXml).ErrorCode,
                Is.EqualTo("signature.xml_invalid"));
            Assert.That(validator.ValidateSignatures(nullPolicy).ErrorCode,
                Is.EqualTo("signature.policy_invalid"));
            Assert.That(validator.ValidateSignatures(nullLimits).ErrorCode,
                Is.EqualTo("signature.policy_invalid"));
            Assert.That(validator.ValidateSignatures(nullAnchors).ErrorCode,
                Is.EqualTo("signature.policy_invalid"));
            Assert.That(validator.ValidateSignatures(tooLarge).ErrorCode,
                Is.EqualTo("signature.total_bytes_limit"));
            Assert.That(XmlSignatureValidator.CanonicalizationPolicyStatus(
                "bad", []), Is.EqualTo(2));
            Assert.That(XmlSignatureValidator.CanonicalizationPolicyStatus(
                SignatureAlgorithmUris.ExclusiveCanonicalization, ["other"]),
                Is.EqualTo(1));
            Assert.That(XmlSignatureValidator.DigestPolicyStatus("bad", []), Is.EqualTo(2));
            Assert.That(XmlSignatureValidator.DigestPolicyStatus(
                SignatureAlgorithmUris.Sha1, []), Is.EqualTo(1));
            Assert.That(XmlSignatureValidator.DigestPolicyStatus(
                SignatureAlgorithmUris.Sha384, []), Is.Zero);
            Assert.That(XmlSignatureValidator.DigestPolicyStatus(
                SignatureAlgorithmUris.Sha512, [SignatureAlgorithmUris.Sha512]), Is.Zero);
            Assert.That(XmlSignatureValidator.SignaturePolicyStatus("bad", []), Is.EqualTo(2));
            Assert.That(XmlSignatureValidator.SignaturePolicyStatus(
                SignatureAlgorithmUris.RsaSha1, []), Is.EqualTo(1));
            Assert.That(XmlSignatureValidator.SignaturePolicyStatus(
                SignatureAlgorithmUris.RsaSha384, []), Is.Zero);
            Assert.That(XmlSignatureValidator.SignaturePolicyStatus(
                SignatureAlgorithmUris.RsaSha512, [SignatureAlgorithmUris.RsaSha512]), Is.Zero);
            Assert.That(XmlSignatureValidator.ContainsValue([], "x"), Is.False);
            Assert.That(XmlSignatureValidator.ContainsValue(["x"], "x"), Is.True);
            Assert.That(XmlSignatureValidator.FindSignatureChild(null, "x"), Is.Null);
            Assert.That(XmlSignatureValidator.FindAttributeValue(null, "x"), Is.Null);
            Assert.That(XmlSignatureValidator.EvaluateCertificateTrust(
                null, new SignatureTrustPolicy()),
                Is.EqualTo(CertificateTrustStatus.NotPresent));
        });

        Assert.Multiple(() =>
        {
            Assert.That(XmlSignatureValidator.CalculateDigest(
                [1], SignatureAlgorithmUris.Sha1), Has.Length.EqualTo(20));
            Assert.That(XmlSignatureValidator.CalculateDigest(
                [1], SignatureAlgorithmUris.Sha384), Has.Length.EqualTo(48));
            Assert.That(XmlSignatureValidator.CalculateDigest(
                [1], SignatureAlgorithmUris.Sha512), Has.Length.EqualTo(64));
            Assert.That(XmlSignatureValidator.GetSignatureHashName(
                SignatureAlgorithmUris.RsaSha1), Is.EqualTo(System.Security.Cryptography.HashAlgorithmName.SHA1));
            Assert.That(XmlSignatureValidator.GetSignatureHashName(
                SignatureAlgorithmUris.RsaSha384), Is.EqualTo(System.Security.Cryptography.HashAlgorithmName.SHA384));
            Assert.That(XmlSignatureValidator.GetSignatureHashName(
                SignatureAlgorithmUris.RsaSha512), Is.EqualTo(System.Security.Cryptography.HashAlgorithmName.SHA512));
        });

        DynamicXmlNode parent = new DynamicXmlNode();
        parent.Attributes.Add(new XmlAttributeData
        {
            Name = new QualifiedNameData { LocalName = "Id" },
            Value = "identifier"
        });
        DynamicXmlNode signature = new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData
                { LocalName = "Signature", NamespaceUri = Iso20022Namespaces.XmlSignature }
        };
        parent.Children.Add(new DynamicXmlNode { Kind = XmlNodeKind.Text });
        parent.Children.Add(signature);
        Assert.Multiple(() =>
        {
            Assert.That(XmlSignatureValidator.FindSignatureChild(parent, "Signature"),
                Is.SameAs(signature));
            Assert.That(XmlSignatureValidator.FindAttributeValue(parent, "Id"),
                Is.EqualTo("identifier"));
            Assert.That(XmlSignatureValidator.GetValidationTime(null).Success, Is.True);
            Assert.That(XmlSignatureValidator.GetValidationTime(
                new IsoDateTimeValue()).Success, Is.False);
        });
    }

    internal static SignedFixture CreateSignedFixture()
    {
        string signedInfo =
            "<ds:SignedInfo>" +
            "<ds:CanonicalizationMethod Algorithm=\"" +
            ExclusiveCanonicalization + "\"></ds:CanonicalizationMethod>" +
            "<ds:SignatureMethod Algorithm=\"" + RsaSha256 +
            "\"></ds:SignatureMethod>" +
            "<ds:Reference URI=\"#message\"><ds:Transforms>" +
            "<ds:Transform Algorithm=\"" + EnvelopedSignature +
            "\"></ds:Transform>" +
            "<ds:Transform Algorithm=\"" + ExclusiveCanonicalization +
            "\"></ds:Transform></ds:Transforms>" +
            "<ds:DigestMethod Algorithm=\"" + Sha256 +
            "\"></ds:DigestMethod><ds:DigestValue>" + DigestValue +
            "</ds:DigestValue></ds:Reference></ds:SignedInfo>";
        string xml =
            "<Document Id=\"message\"><Value>original</Value>" +
            "<ds:Signature xmlns:ds=\"" + SignatureNamespace +
            "\" Id=\"signature-1\">" + signedInfo +
            "<ds:SignatureValue>" + SignatureValue +
            "</ds:SignatureValue><ds:KeyInfo><ds:X509Data>" +
            "<ds:X509Certificate>" + CertificateValue +
            "</ds:X509Certificate></ds:X509Data></ds:KeyInfo>" +
            "</ds:Signature></Document>";
        return new SignedFixture
        {
            Xml = xml,
            Certificate = Convert.FromBase64String(CertificateValue)
        };
    }

    internal class SignedFixture
    {
        public string Xml = string.Empty;
        public byte[] Certificate = [];
    }
}
