using System.Security.Cryptography;
using iso20022;

namespace iso20022.Tests;

public class SupplementarySignatureIntegrityApiTests
{
    [Test]
    public void SupplementaryValidationCarriesPoliciesPreservedContentAndLocatedResults()
    {
        SupplementaryValidationRequest first = new SupplementaryValidationRequest();
        SupplementaryValidationRequest second = new SupplementaryValidationRequest();
        SchemaArtifact schema = new SchemaArtifact
        {
            NamespaceUri = "urn:example:extension:payment-status"
        };
        SupplementaryDataPolicy policy = new SupplementaryDataPolicy
        {
            NamespaceUri = schema.NamespaceUri,
            Schema = schema,
            MustUnderstand = true,
            ValidateSchema = true
        };
        first.Xml = "<Document/>"u8.ToArray();
        first.Profile = new ProfileDefinition { Identifier = "market-profile" };
        first.Policies.Add(policy);
        first.Limits.MaxElementCount = 25;
        SupplementaryDataItem item = new SupplementaryDataItem
        {
            PlaceAndName = "/Document/PmtInf",
            LocationPath = new XmlPathData { Value = "/Document/PmtInf" },
            EnvelopeName = new QualifiedNameData
            {
                NamespaceUri = "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10",
                LocalName = "SplmtryData"
            },
            PayloadName = new QualifiedNameData
            {
                NamespaceUri = schema.NamespaceUri,
                LocalName = "PaymentStatusExtension"
            },
            NamespaceUri = schema.NamespaceUri,
            Envelope = new RawXmlFragment { Xml = "<SplmtryData/>" },
            Payload = new RawXmlFragment { Xml = "<ext:PaymentStatusExtension/>" },
            Schema = schema,
            Status = SupplementaryValidationStatus.Valid,
            OccurrenceIndex = 1,
            MustUnderstand = true
        };
        SupplementaryValidationResult expected = new SupplementaryValidationResult
        {
            Success = true,
            Valid = true,
            Status = SupplementaryValidationStatus.Valid
        };
        expected.Items.Add(item);
        StubSupplementaryDataValidator implementation =
            new StubSupplementaryDataValidator(expected);
        ISupplementaryDataValidator service = implementation;

        SupplementaryValidationResult actual =
            service.ValidateSupplementaryData(first);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.SameAs(expected));
            Assert.That(implementation.Request, Is.SameAs(first));
            Assert.That(first.Xml, Is.EqualTo("<Document/>"u8.ToArray()));
            Assert.That(first.Policies.Single(), Is.SameAs(policy));
            Assert.That(policy.Schema, Is.SameAs(schema));
            Assert.That(policy.CanIgnore, Is.False);
            Assert.That(policy.MustUnderstand, Is.True);
            Assert.That(policy.ValidateSchema, Is.True);
            Assert.That(first.ValidateSchemas, Is.True);
            Assert.That(first.PreserveContent, Is.True);
            Assert.That(first.Policies, Is.Not.SameAs(second.Policies));
            Assert.That(first.Limits, Is.Not.SameAs(second.Limits));
            Assert.That(second.Policies, Is.Empty);
            Assert.That(second.Xml, Is.Empty);
            Assert.That(item.PlaceAndName, Is.EqualTo("/Document/PmtInf"));
            Assert.That(item.LocationPath.Value, Is.EqualTo("/Document/PmtInf"));
            Assert.That(item.EnvelopeName.LocalName, Is.EqualTo("SplmtryData"));
            Assert.That(item.PayloadName.NamespaceUri, Is.EqualTo(schema.NamespaceUri));
            Assert.That(item.Envelope!.Xml, Is.EqualTo("<SplmtryData/>"));
            Assert.That(item.Payload!.Xml,
                Is.EqualTo("<ext:PaymentStatusExtension/>"));
            Assert.That(item.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(item.IsLegacyForm, Is.False);
            Assert.That(item.MustUnderstand, Is.True);
        });
    }

    [Test]
    public void SignatureValidationCarriesTrustReferencesAndSecureIndependentDefaults()
    {
        SignatureValidationRequest first = new SignatureValidationRequest();
        SignatureValidationRequest second = new SignatureValidationRequest();
        first.Xml = "<Document Id=\"message\"/>"u8.ToArray();
        first.RequireSignature = true;
        first.TrustPolicy.TrustedCertificates.Add(new byte[] { 1, 2, 3 });
        first.TrustPolicy.AllowedDigestAlgorithms.Add("urn:example:digest:sha256");
        first.TrustPolicy.ValidationTime = new IsoDateTimeValue
        {
            LexicalValue = "2026-09-01T12:00:00Z"
        };
        SignatureReferenceResult reference = new SignatureReferenceResult
        {
            Uri = "#message",
            TargetPath = new XmlPathData { Value = "/Document" },
            DigestAlgorithm = "urn:example:digest:sha256",
            ExpectedDigest = new IsoBinaryValue
            {
                Value = new byte[] { 10, 11 },
                Encoding = BinaryEncodingKind.Base64,
                LexicalValue = "Cgs="
            },
            CalculatedDigest = new IsoBinaryValue
            {
                Value = new byte[] { 10, 11 },
                Encoding = BinaryEncodingKind.Base64,
                LexicalValue = "Cgs="
            },
            Status = SignatureReferenceStatus.Valid,
            ContainedInMessage = true
        };
        XmlSignatureResult signature = new XmlSignatureResult
        {
            Identifier = "signature-1",
            Path = new XmlPathData { Value = "/Document/Signature" },
            CanonicalizationAlgorithm = "urn:example:c14n",
            SignatureAlgorithm = "urn:example:signature:rsa-sha256",
            Status = SignatureValidationStatus.Valid,
            CertificateStatus = CertificateTrustStatus.Trusted,
            CertificateIdentifier = "CN=Example"
        };
        signature.References.Add(reference);
        SignatureValidationResult expected = new SignatureValidationResult
        {
            Success = true,
            Valid = true,
            Status = SignatureValidationStatus.Valid
        };
        expected.Signatures.Add(signature);
        StubXmlSignatureValidator implementation =
            new StubXmlSignatureValidator(expected);
        IXmlSignatureValidator service = implementation;

        SignatureValidationResult actual = service.ValidateSignatures(first);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.SameAs(expected));
            Assert.That(implementation.Request, Is.SameAs(first));
            Assert.That(first.RequireSignature, Is.True);
            Assert.That(first.ValidateAllSignatures, Is.True);
            Assert.That(first.RedactCertificateIdentifiers, Is.True);
            Assert.That(first.TrustPolicy.UseSystemTrustStore, Is.True);
            Assert.That(first.TrustPolicy.RequireTrustedCertificate, Is.True);
            Assert.That(first.TrustPolicy.AllowEmbeddedCertificates, Is.True);
            Assert.That(first.TrustPolicy.CheckCertificateRevocation, Is.True);
            Assert.That(first.TrustPolicy.AllowExternalReferences, Is.False);
            Assert.That(first.TrustPolicy.TrustedCertificates,
                Is.Not.SameAs(second.TrustPolicy.TrustedCertificates));
            Assert.That(first.TrustPolicy.AllowedDigestAlgorithms,
                Is.Not.SameAs(second.TrustPolicy.AllowedDigestAlgorithms));
            Assert.That(second.TrustPolicy.TrustedCertificates, Is.Empty);
            Assert.That(first.Limits, Is.Not.SameAs(second.Limits));
            Assert.That(signature.References.Single(), Is.SameAs(reference));
            Assert.That(reference.ExpectedDigest.Value,
                Is.EqualTo(reference.CalculatedDigest.Value));
            Assert.That(reference.ContainedInMessage, Is.True);
            Assert.That(signature.CertificateStatus,
                Is.EqualTo(CertificateTrustStatus.Trusted));
        });
    }

    [Test]
    public void MessageSigningCarriesCallerKeyAlgorithmsReferencesAndSignedOutput()
    {
        using RSA key = RSA.Create();
        MessageSigningRequest first = new MessageSigningRequest
        {
            Xml = "<Document><Body Id=\"body\"/></Document>"u8.ToArray(),
            SigningKey = key,
            InsertionPath = new XmlPathData { Value = "/Document" },
            CanonicalizationAlgorithm = "urn:example:c14n",
            DigestAlgorithm = "urn:example:digest:sha256",
            SignatureAlgorithm = "urn:example:signature:rsa-sha256",
            KeyName = "signing-key"
        };
        first.ReferencePaths.Add(new XmlPathData { Value = "/Document/Body" });
        MessageSigningRequest second = new MessageSigningRequest();
        XmlSignatureResult signature = new XmlSignatureResult
        {
            Identifier = "generated-signature",
            Status = SignatureValidationStatus.Valid
        };
        MessageSigningResult expected = new MessageSigningResult
        {
            Success = true,
            Status = MessageSigningStatus.Signed,
            Xml = "<Document><Body/><Signature/></Document>"u8.ToArray(),
            Signature = signature
        };
        StubXmlMessageSigner implementation = new StubXmlMessageSigner(expected);
        IXmlMessageSigner service = implementation;

        MessageSigningResult actual = service.SignMessage(first);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.SameAs(expected));
            Assert.That(implementation.Request, Is.SameAs(first));
            Assert.That(first.SigningKey, Is.SameAs(key));
            Assert.That(first.InsertionPath.Value, Is.EqualTo("/Document"));
            Assert.That(first.ReferencePaths.Single().Value,
                Is.EqualTo("/Document/Body"));
            Assert.That(first.IncludeCertificate, Is.True);
            Assert.That(first.VerifyAfterSigning, Is.True);
            Assert.That(first.ReferencePaths, Is.Not.SameAs(second.ReferencePaths));
            Assert.That(first.Limits, Is.Not.SameAs(second.Limits));
            Assert.That(second.Xml, Is.Empty);
            Assert.That(actual.Xml, Is.Not.Empty);
            Assert.That(actual.Signature, Is.SameAs(signature));
            Assert.That(actual.Status, Is.EqualTo(MessageSigningStatus.Signed));
        });
    }

    [Test]
    public void ChecksumCarriesScopeModeExpectedValueDuplicateKeysAndContractResult()
    {
        ChecksumRequest first = new ChecksumRequest();
        ChecksumRequest second = new ChecksumRequest();
        first.Xml = "<Document><Body/></Document>"u8.ToArray();
        first.Scope = ChecksumScope.MessageBody;
        first.EqualityMode = MessageEqualityMode.SemanticModel;
        first.ExpectedChecksum = new byte[] { 1, 2, 3, 4 };
        first.KeyPaths.Add(new XmlPathData { Value = "/Document/Body/MessageId" });
        first.Limits.MaxTextLength = 128;
        ValidationDiagnostic diagnostic = new ValidationDiagnostic
        {
            Code = "checksum.duplicate_key",
            Stage = ValidationStage.Semantic,
            Kind = DiagnosticKind.Conflict,
            Path = first.KeyPaths[0]
        };
        ChecksumResult expected = new ChecksumResult
        {
            Success = true,
            Status = ChecksumStatus.Matched,
            Checksum = new IsoBinaryValue
            {
                Value = new byte[] { 1, 2, 3, 4 },
                Encoding = BinaryEncodingKind.Hexadecimal,
                LexicalValue = "01020304"
            },
            HasExpectedChecksum = true,
            MatchesExpected = true,
            Scope = ChecksumScope.MessageBody,
            EqualityMode = MessageEqualityMode.SemanticModel,
            Algorithm = "SHA-256"
        };
        expected.DuplicateKeyPaths.Add(first.KeyPaths[0]);
        expected.DuplicateKeys.Add("business-message-id");
        expected.Diagnostics.Add(diagnostic);
        StubMessageChecksumCalculator implementation =
            new StubMessageChecksumCalculator(expected);
        IMessageChecksumCalculator service = implementation;

        ChecksumResult actual = service.CalculateChecksum(first);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.SameAs(expected));
            Assert.That(implementation.Request, Is.SameAs(first));
            Assert.That(second.Scope, Is.EqualTo(ChecksumScope.WholeMessage));
            Assert.That(second.EqualityMode,
                Is.EqualTo(MessageEqualityMode.CanonicalXml));
            Assert.That(second.Algorithm, Is.EqualTo("SHA-256"));
            Assert.That(second.ExpectedChecksum, Is.Empty);
            Assert.That(second.RejectDuplicateKeys, Is.True);
            Assert.That(second.RedactDuplicateKeys, Is.True);
            Assert.That(first.KeyPaths, Is.Not.SameAs(second.KeyPaths));
            Assert.That(first.Limits, Is.Not.SameAs(second.Limits));
            Assert.That(actual.Checksum.Encoding,
                Is.EqualTo(BinaryEncodingKind.Hexadecimal));
            Assert.That(actual.Checksum.LexicalValue, Is.EqualTo("01020304"));
            Assert.That(actual.HasExpectedChecksum, Is.True);
            Assert.That(actual.MatchesExpected, Is.True);
            Assert.That(actual.DuplicateKeyPaths.Single(), Is.SameAs(first.KeyPaths[0]));
            Assert.That(actual.DuplicateKeys.Single(),
                Is.EqualTo("business-message-id"));
            Assert.That(actual.Diagnostics.Single(), Is.SameAs(diagnostic));
        });
    }

    private class StubSupplementaryDataValidator : ISupplementaryDataValidator
    {
        internal SupplementaryValidationRequest? Request;
        internal SupplementaryValidationResult Result;

        internal StubSupplementaryDataValidator(SupplementaryValidationResult result)
        {
            Result = result;
        }

        public SupplementaryValidationResult ValidateSupplementaryData(
            SupplementaryValidationRequest? request)
        {
            Request = request;
            return Result;
        }
    }

    private class StubXmlSignatureValidator : IXmlSignatureValidator
    {
        internal SignatureValidationRequest? Request;
        internal SignatureValidationResult Result;

        internal StubXmlSignatureValidator(SignatureValidationResult result)
        {
            Result = result;
        }

        public SignatureValidationResult ValidateSignatures(
            SignatureValidationRequest? request)
        {
            Request = request;
            return Result;
        }
    }

    private class StubXmlMessageSigner : IXmlMessageSigner
    {
        internal MessageSigningRequest? Request;
        internal MessageSigningResult Result;

        internal StubXmlMessageSigner(MessageSigningResult result)
        {
            Result = result;
        }

        public MessageSigningResult SignMessage(MessageSigningRequest? request)
        {
            Request = request;
            return Result;
        }
    }

    private class StubMessageChecksumCalculator : IMessageChecksumCalculator
    {
        internal ChecksumRequest? Request;
        internal ChecksumResult Result;

        internal StubMessageChecksumCalculator(ChecksumResult result)
        {
            Result = result;
        }

        public ChecksumResult CalculateChecksum(ChecksumRequest? request)
        {
            Request = request;
            return Result;
        }
    }
}
