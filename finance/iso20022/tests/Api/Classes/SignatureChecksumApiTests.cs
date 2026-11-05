using System.Text;
using iso20022;

namespace iso20022.Tests;

public class SignatureChecksumApiTests
{
    private const string Sha256 =
        "http://www.w3.org/2001/04/xmlenc#sha256";
    private const string Sha1 =
        "http://www.w3.org/2000/09/xmldsig#sha1";

    [Test]
    public void SignatureApiDistinguishesValidTamperedMissingUnsafeAndUntrusted()
    {
        XmlSignatureValidatorTests.SignedFixture fixture =
            XmlSignatureValidatorTests.CreateSignedFixture();
        IXmlSignatureValidator service = new XmlSignatureValidator();
        SignatureValidationResult valid = service.ValidateSignatures(
            XmlSignatureValidatorTests.CreateTrustedRequest(fixture));
        SignatureValidationRequest tamperedRequest =
            XmlSignatureValidatorTests.CreateTrustedRequest(fixture);
        tamperedRequest.Xml = Encoding.UTF8.GetBytes(
            fixture.Xml.Replace(
                "<Value>original</Value>",
                "<Value>tampered</Value>",
                StringComparison.Ordinal));
        SignatureValidationResult tampered =
            service.ValidateSignatures(tamperedRequest);
        SignatureValidationRequest missingRequest =
            XmlSignatureValidatorTests.CreateTrustedRequest(fixture);
        missingRequest.Xml = Encoding.UTF8.GetBytes(
            fixture.Xml.Replace(
                "URI=\"#message\"",
                "URI=\"#missing\"",
                StringComparison.Ordinal));
        SignatureValidationResult missing =
            service.ValidateSignatures(missingRequest);
        SignatureValidationRequest unsafeRequest =
            XmlSignatureValidatorTests.CreateTrustedRequest(fixture);
        unsafeRequest.Xml = Encoding.UTF8.GetBytes(
            fixture.Xml.Replace(Sha256, Sha1, StringComparison.Ordinal));
        SignatureValidationResult unsafeAlgorithm =
            service.ValidateSignatures(unsafeRequest);
        SignatureValidationRequest untrustedRequest =
            new SignatureValidationRequest
            {
                Xml = Encoding.UTF8.GetBytes(fixture.Xml)
            };
        untrustedRequest.TrustPolicy.UseSystemTrustStore = false;
        untrustedRequest.TrustPolicy.CheckCertificateRevocation = false;
        SignatureValidationResult untrusted =
            service.ValidateSignatures(untrustedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True, valid.ErrorMessage);
            Assert.That(valid.Valid, Is.True);
            Assert.That(valid.Status,
                Is.EqualTo(SignatureValidationStatus.Valid));
            Assert.That(valid.Signatures.Single().References.Single().Status,
                Is.EqualTo(SignatureReferenceStatus.Valid));
            Assert.That(tampered.Success, Is.True, tampered.ErrorMessage);
            Assert.That(tampered.Valid, Is.False);
            Assert.That(tampered.Status,
                Is.EqualTo(SignatureValidationStatus.ReferenceInvalid));
            Assert.That(tampered.Signatures.Single().References.Single().Status,
                Is.EqualTo(SignatureReferenceStatus.DigestMismatch));
            Assert.That(missing.Success, Is.True, missing.ErrorMessage);
            Assert.That(missing.Valid, Is.False);
            Assert.That(missing.Signatures.Single().References.Single().Status,
                Is.EqualTo(SignatureReferenceStatus.MissingTarget));
            Assert.That(missing.Diagnostics.Single().Code,
                Is.EqualTo("signature.reference_target_missing"));
            Assert.That(unsafeAlgorithm.Success, Is.True,
                unsafeAlgorithm.ErrorMessage);
            Assert.That(unsafeAlgorithm.Valid, Is.False);
            Assert.That(unsafeAlgorithm.Status,
                Is.EqualTo(SignatureValidationStatus.UnsafeAlgorithm));
            Assert.That(unsafeAlgorithm.Signatures.Single().References.Single()
                    .Status,
                Is.EqualTo(SignatureReferenceStatus.UnsafeAlgorithm));
            Assert.That(untrusted.Success, Is.True, untrusted.ErrorMessage);
            Assert.That(untrusted.Valid, Is.False);
            Assert.That(untrusted.Status,
                Is.EqualTo(SignatureValidationStatus.CertificateUntrusted));
            Assert.That(untrusted.Signatures.Single().CertificateStatus,
                Is.EqualTo(CertificateTrustStatus.Untrusted));
        });
    }

    [Test]
    public void CanonicalChecksumComparisonIgnoresPrefixesButByteComparisonDoesNot()
    {
        string firstXml =
            "<first:Document xmlns:first='urn:message' " +
            "xmlns:meta='urn:metadata' meta:identifier='id-1' plain='value'>" +
            "<first:Body><first:Amount Currency='USD'>10.00" +
            "</first:Amount></first:Body></first:Document>";
        string secondXml =
            "<message:Document plain=\"value\" information:identifier=\"id-1\" " +
            "xmlns:information=\"urn:metadata\" " +
            "xmlns:message=\"urn:message\">" +
            "<message:Body><message:Amount Currency=\"USD\">10.00" +
            "</message:Amount></message:Body></message:Document>";
        IMessageChecksumCalculator service = new MessageChecksum();
        ChecksumResult firstCanonical = service.CalculateChecksum(
            CreateRequest(firstXml, MessageEqualityMode.CanonicalXml));
        ChecksumRequest canonicalComparison = CreateRequest(
            secondXml,
            MessageEqualityMode.CanonicalXml);
        canonicalComparison.ExpectedChecksum = firstCanonical.Checksum.Value;
        ChecksumResult equivalentCanonical =
            service.CalculateChecksum(canonicalComparison);
        ChecksumResult firstByte = service.CalculateChecksum(
            CreateRequest(firstXml, MessageEqualityMode.Byte));
        ChecksumRequest byteComparison = CreateRequest(
            secondXml,
            MessageEqualityMode.Byte);
        byteComparison.ExpectedChecksum = firstByte.Checksum.Value;
        ChecksumResult differentBytes =
            service.CalculateChecksum(byteComparison);

        Assert.Multiple(() =>
        {
            Assert.That(firstCanonical.Success, Is.True,
                firstCanonical.ErrorMessage);
            Assert.That(equivalentCanonical.Success, Is.True,
                equivalentCanonical.ErrorMessage);
            Assert.That(equivalentCanonical.HasExpectedChecksum, Is.True);
            Assert.That(equivalentCanonical.MatchesExpected, Is.True);
            Assert.That(equivalentCanonical.Status,
                Is.EqualTo(ChecksumStatus.Matched));
            Assert.That(equivalentCanonical.Checksum.Value,
                Is.EqualTo(firstCanonical.Checksum.Value));
            Assert.That(differentBytes.Success, Is.True,
                differentBytes.ErrorMessage);
            Assert.That(differentBytes.HasExpectedChecksum, Is.True);
            Assert.That(differentBytes.MatchesExpected, Is.False);
            Assert.That(differentBytes.Status,
                Is.EqualTo(ChecksumStatus.Mismatched));
            Assert.That(differentBytes.Checksum.Value,
                Is.Not.EqualTo(firstByte.Checksum.Value));
        });
    }

    private static ChecksumRequest CreateRequest(
        string xml,
        MessageEqualityMode mode)
    {
        return new ChecksumRequest
        {
            Xml = Encoding.UTF8.GetBytes(xml),
            EqualityMode = mode
        };
    }
}
