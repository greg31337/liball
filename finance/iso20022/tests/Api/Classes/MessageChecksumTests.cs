using System.Text;
using iso20022;

namespace iso20022.Tests;

public class MessageChecksumTests
{
    [Test]
    public void ByteChecksumsPreserveExactInputAndVerifyExpectedValues()
    {
        IMessageChecksumCalculator service = new MessageChecksum();
        ChecksumRequest compactRequest = new ChecksumRequest
        {
            Xml = "<Document><Value>one</Value></Document>"u8.ToArray(),
            EqualityMode = MessageEqualityMode.Byte
        };
        ChecksumResult compact = service.CalculateChecksum(compactRequest);
        ChecksumResult spaced = service.CalculateChecksum(new ChecksumRequest
        {
            Xml = "<Document> <Value>one</Value></Document>"u8.ToArray(),
            EqualityMode = MessageEqualityMode.Byte
        });
        ChecksumResult matched = service.CalculateChecksum(new ChecksumRequest
        {
            Xml = compactRequest.Xml,
            EqualityMode = MessageEqualityMode.Byte,
            ExpectedChecksum = compact.Checksum.Value
        });
        ChecksumResult mismatched = service.CalculateChecksum(new ChecksumRequest
        {
            Xml = compactRequest.Xml,
            EqualityMode = MessageEqualityMode.Byte,
            ExpectedChecksum = new byte[32]
        });
        ChecksumResult sha512 = service.CalculateChecksum(new ChecksumRequest
        {
            Xml = compactRequest.Xml,
            EqualityMode = MessageEqualityMode.Byte,
            Algorithm = "sha512"
        });
        ChecksumResult unsafeAlgorithm = service.CalculateChecksum(
            new ChecksumRequest
            {
                Xml = compactRequest.Xml,
                Algorithm = "SHA-1"
            });

        Assert.Multiple(() =>
        {
            Assert.That(compact.Success, Is.True);
            Assert.That(compact.Status, Is.EqualTo(ChecksumStatus.Calculated));
            Assert.That(compact.Checksum.Value, Has.Length.EqualTo(32));
            Assert.That(compact.Checksum.Encoding,
                Is.EqualTo(BinaryEncodingKind.Hexadecimal));
            Assert.That(compact.Checksum.LexicalValue,
                Has.Length.EqualTo(64));
            Assert.That(spaced.Checksum.Value,
                Is.Not.EqualTo(compact.Checksum.Value));
            Assert.That(matched.Status, Is.EqualTo(ChecksumStatus.Matched));
            Assert.That(matched.HasExpectedChecksum, Is.True);
            Assert.That(matched.MatchesExpected, Is.True);
            Assert.That(mismatched.Status,
                Is.EqualTo(ChecksumStatus.Mismatched));
            Assert.That(mismatched.Success, Is.True);
            Assert.That(mismatched.MatchesExpected, Is.False);
            Assert.That(sha512.Algorithm, Is.EqualTo("SHA-512"));
            Assert.That(sha512.Checksum.Value, Has.Length.EqualTo(64));
            Assert.That(unsafeAlgorithm.Success, Is.False);
            Assert.That(unsafeAlgorithm.Status,
                Is.EqualTo(ChecksumStatus.UnsupportedAlgorithm));
        });
    }

    [Test]
    public void CanonicalChecksumsNormalizePrefixesAndCanIsolateMessageBody()
    {
        string firstXml =
            "<e:Envelope xmlns:e='urn:envelope' xmlns:d='urn:document'>" +
            "<e:Header>first</e:Header>" +
            "<d:Document b='2' a='1'><d:Value>same</d:Value></d:Document>" +
            "</e:Envelope>";
        string equivalentXml =
            "<wrapper:Envelope xmlns:body='urn:document' " +
            "xmlns:wrapper='urn:envelope'>" +
            "<wrapper:Header>first</wrapper:Header>" +
            "<body:Document a='1' b='2'><body:Value>same</body:Value>" +
            "</body:Document></wrapper:Envelope>";
        string changedHeaderXml = equivalentXml.Replace(
            ">first<",
            ">second<",
            StringComparison.Ordinal);
        IMessageChecksumCalculator service = new MessageChecksum();

        ChecksumResult firstWhole = service.CalculateChecksum(CanonicalRequest(
            firstXml,
            ChecksumScope.WholeMessage));
        ChecksumResult equivalentWhole = service.CalculateChecksum(
            CanonicalRequest(equivalentXml, ChecksumScope.WholeMessage));
        ChecksumResult changedWhole = service.CalculateChecksum(CanonicalRequest(
            changedHeaderXml,
            ChecksumScope.WholeMessage));
        ChecksumResult firstBody = service.CalculateChecksum(CanonicalRequest(
            firstXml,
            ChecksumScope.MessageBody));
        ChecksumResult changedHeaderBody = service.CalculateChecksum(
            CanonicalRequest(changedHeaderXml, ChecksumScope.MessageBody));

        Assert.Multiple(() =>
        {
            Assert.That(firstWhole.Success, Is.True, firstWhole.ErrorMessage);
            Assert.That(equivalentWhole.Checksum.Value,
                Is.EqualTo(firstWhole.Checksum.Value));
            Assert.That(changedWhole.Checksum.Value,
                Is.Not.EqualTo(firstWhole.Checksum.Value));
            Assert.That(firstBody.Success, Is.True, firstBody.ErrorMessage);
            Assert.That(firstBody.Scope, Is.EqualTo(ChecksumScope.MessageBody));
            Assert.That(changedHeaderBody.Checksum.Value,
                Is.EqualTo(firstBody.Checksum.Value));
            Assert.That(firstBody.Checksum.Value,
                Is.Not.EqualTo(firstWhole.Checksum.Value));
        });
    }

    [Test]
    public void SemanticChecksumsIgnoreXmlOnlyDetailsAndHashGeneratedModels()
    {
        string detailed =
            "<Document>\n<!--ignored--><?audit value?>" +
            "<Value><![CDATA[same]]></Value>\n</Document>";
        string semanticEquivalent =
            "<Document><Value>same</Value></Document>";
        IMessageChecksumCalculator service = new MessageChecksum();
        ChecksumResult firstSemantic = service.CalculateChecksum(
            SemanticRequest(detailed));
        ChecksumResult secondSemantic = service.CalculateChecksum(
            SemanticRequest(semanticEquivalent));
        MessageReadOptions readOptions = new MessageReadOptions
        {
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreserveWhitespace = true
        };
        MessageReadResult read = new XmlMessageReader().ReadGenericText(
            detailed,
            readOptions);
        ChecksumResult documentSemantic = service.CalculateChecksum(
            new ChecksumRequest
            {
                Document = read.Document,
                EqualityMode = MessageEqualityMode.SemanticModel
            });
        ChecksumResult firstCanonical = service.CalculateChecksum(
            CanonicalRequest(detailed, ChecksumScope.WholeMessage));
        ChecksumResult secondCanonical = service.CalculateChecksum(
            CanonicalRequest(semanticEquivalent, ChecksumScope.WholeMessage));
        ChecksumResult firstModel = service.CalculateChecksum(
            new ChecksumRequest
            {
                Model = new SemanticFixture { Identifier = "id-1", Count = 2 },
                EqualityMode = MessageEqualityMode.SemanticModel
            });
        ChecksumResult secondModel = service.CalculateChecksum(
            new ChecksumRequest
            {
                Model = new SemanticFixture { Identifier = "id-1", Count = 2 },
                EqualityMode = MessageEqualityMode.SemanticModel
            });
        ChecksumResult changedModel = service.CalculateChecksum(
            new ChecksumRequest
            {
                Model = new SemanticFixture { Identifier = "id-1", Count = 3 },
                EqualityMode = MessageEqualityMode.SemanticModel
            });

        Assert.Multiple(() =>
        {
            Assert.That(firstSemantic.Success, Is.True,
                firstSemantic.ErrorMessage);
            Assert.That(firstSemantic.Checksum.Value,
                Is.EqualTo(secondSemantic.Checksum.Value));
            Assert.That(documentSemantic.Checksum.Value,
                Is.EqualTo(firstSemantic.Checksum.Value));
            Assert.That(firstCanonical.Checksum.Value,
                Is.Not.EqualTo(secondCanonical.Checksum.Value));
            Assert.That(firstModel.Success, Is.True, firstModel.ErrorMessage);
            Assert.That(firstModel.Checksum.Value,
                Is.EqualTo(secondModel.Checksum.Value));
            Assert.That(changedModel.Checksum.Value,
                Is.Not.EqualTo(firstModel.Checksum.Value));
        });
    }

    [Test]
    public void DuplicateKeyPolicyRejectsOrReportsWithConfiguredRedaction()
    {
        byte[] xml = Encoding.UTF8.GetBytes(
            "<Document><Items>" +
            "<Item><Id>duplicate-id</Id></Item>" +
            "<Item><Id>duplicate-id</Id></Item>" +
            "</Items></Document>");
        XmlPathData keyPath = new XmlPathData
        {
            Value = "/Document/Items/Item/Id"
        };
        IMessageChecksumCalculator service = new MessageChecksum();
        ChecksumRequest rejectingRequest = new ChecksumRequest
        {
            Xml = xml,
            EqualityMode = MessageEqualityMode.SemanticModel
        };
        rejectingRequest.KeyPaths.Add(keyPath);
        ChecksumResult rejected = service.CalculateChecksum(rejectingRequest);
        ChecksumRequest reportingRequest = new ChecksumRequest
        {
            Xml = xml,
            EqualityMode = MessageEqualityMode.SemanticModel,
            RejectDuplicateKeys = false,
            RedactDuplicateKeys = false
        };
        reportingRequest.KeyPaths.Add(keyPath);
        ChecksumResult reported = service.CalculateChecksum(reportingRequest);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Status,
                Is.EqualTo(ChecksumStatus.DuplicateKey));
            Assert.That(rejected.Checksum.Value, Is.Empty);
            Assert.That(rejected.DuplicateKeyPaths.Single().Value,
                Is.EqualTo("/Document/Items/Item/Id"));
            Assert.That(rejected.DuplicateKeys, Is.Empty);
            Assert.That(rejected.DuplicateKeysRedacted, Is.True);
            Assert.That(rejected.Diagnostics.Single().OffendingValueRedacted,
                Is.True);
            Assert.That(rejected.Diagnostics.Single().Severity,
                Is.EqualTo(ValidationSeverity.Error));
            Assert.That(reported.Success, Is.True, reported.ErrorMessage);
            Assert.That(reported.Status,
                Is.EqualTo(ChecksumStatus.Calculated));
            Assert.That(reported.DuplicateKeys.Single(),
                Is.EqualTo("duplicate-id"));
            Assert.That(reported.Diagnostics.Single().OffendingValue,
                Is.EqualTo("duplicate-id"));
            Assert.That(reported.Diagnostics.Single().Severity,
                Is.EqualTo(ValidationSeverity.Warning));
        });
    }

    [Test]
    public void InvalidRepresentationsModesAndBodyScopesReturnStableFailures()
    {
        IMessageChecksumCalculator service = new MessageChecksum();
        ChecksumResult missing = service.CalculateChecksum(null);
        ChecksumResult ambiguous = service.CalculateChecksum(new ChecksumRequest
        {
            Xml = "<Document/>"u8.ToArray(),
            Document = new MessageDocument()
        });
        ChecksumResult unsupportedModelMode = service.CalculateChecksum(
            new ChecksumRequest
            {
                Model = new SemanticFixture(),
                EqualityMode = MessageEqualityMode.Byte
            });
        ChecksumResult ambiguousBody = service.CalculateChecksum(
            new ChecksumRequest
            {
                Xml = "<Envelope><Document/><Document/></Envelope>"u8.ToArray(),
                Scope = ChecksumScope.MessageBody
            });
        ChecksumResult unknownMode = service.CalculateChecksum(
            new ChecksumRequest
            {
                Xml = "<Document/>"u8.ToArray(),
                EqualityMode = MessageEqualityMode.Unknown
            });

        Assert.Multiple(() =>
        {
            Assert.That(missing.Status, Is.EqualTo(ChecksumStatus.InvalidInput));
            Assert.That(missing.ErrorCode, Is.EqualTo("checksum.request_missing"));
            Assert.That(ambiguous.Status,
                Is.EqualTo(ChecksumStatus.InvalidInput));
            Assert.That(ambiguous.ErrorCode,
                Is.EqualTo("checksum.input_ambiguous"));
            Assert.That(unsupportedModelMode.Status,
                Is.EqualTo(ChecksumStatus.UnsupportedMode));
            Assert.That(ambiguousBody.Status,
                Is.EqualTo(ChecksumStatus.InvalidInput));
            Assert.That(ambiguousBody.ErrorCode,
                Is.EqualTo("checksum.body_ambiguous"));
            Assert.That(unknownMode.Status,
                Is.EqualTo(ChecksumStatus.UnsupportedMode));
        });
    }

    [Test]
    public void InvalidPoliciesModelsAndPathHelpersReturnStableResults()
    {
        MessageChecksum checksum = new MessageChecksum();
        List<ChecksumRequest> invalid = new List<ChecksumRequest>
        {
            new ChecksumRequest { Xml = null! },
            new ChecksumRequest { ExpectedChecksum = null! },
            new ChecksumRequest { KeyPaths = null! },
            new ChecksumRequest { Limits = null! },
            new ChecksumRequest { Algorithm = null! },
            new ChecksumRequest { Scope = (ChecksumScope)99 },
            new ChecksumRequest { EqualityMode = (MessageEqualityMode)99 }
        };
        foreach (ChecksumRequest request in invalid)
        {
            Assert.That(checksum.CalculateChecksum(request).Success, Is.False);
        }

        ChecksumRequest tooManyPaths = new ChecksumRequest();
        tooManyPaths.Limits.MaxCollectionCount = 1;
        tooManyPaths.KeyPaths.Add(new XmlPathData { Value = "/x" });
        tooManyPaths.KeyPaths.Add(new XmlPathData { Value = "/y" });
        ChecksumRequest nullPath = new ChecksumRequest();
        nullPath.KeyPaths.Add(null!);
        ChecksumRequest emptyPath = new ChecksumRequest();
        emptyPath.KeyPaths.Add(new XmlPathData());
        ChecksumRequest tooManyBytes = new ChecksumRequest
        {
            Xml = "<x/>"u8.ToArray(),
            Limits = new ResourceLimits { MaxTotalBytes = 1 }
        };
        Assert.Multiple(() =>
        {
            Assert.That(checksum.CalculateChecksum(tooManyPaths).ErrorCode,
                Is.EqualTo("checksum.key_path_limit"));
            Assert.That(checksum.CalculateChecksum(nullPath).ErrorCode,
                Is.EqualTo("checksum.key_path_invalid"));
            Assert.That(checksum.CalculateChecksum(emptyPath).ErrorCode,
                Is.EqualTo("checksum.key_path_invalid"));
            Assert.That(checksum.CalculateChecksum(tooManyBytes).ErrorCode,
                Is.EqualTo("checksum.input_bytes_limit"));
            Assert.That(MessageChecksum.NormalizeAlgorithm("sha384"), Is.EqualTo("SHA-384"));
            Assert.That(MessageChecksum.NormalizeAlgorithm("sha512"), Is.EqualTo("SHA-512"));
            Assert.That(MessageChecksum.NormalizeAlgorithm("bad"), Is.Empty);
            Assert.That(MessageChecksum.CalculateHash([1], "SHA-384"), Has.Length.EqualTo(48));
            Assert.That(MessageChecksum.CalculateHash([1], "SHA-512"), Has.Length.EqualTo(64));
        });

        ChecksumRequest opaqueWrongMode = new ChecksumRequest
        {
            Model = new { Value = "x" },
            EqualityMode = MessageEqualityMode.Byte
        };
        ChecksumRequest opaquePaths = new ChecksumRequest
        {
            Model = new { Value = "x" },
            EqualityMode = MessageEqualityMode.SemanticModel
        };
        opaquePaths.KeyPaths.Add(new XmlPathData { Value = "/x" });
        ChecksumRequest opaqueLimit = new ChecksumRequest
        {
            Model = new { Value = "a long value" },
            EqualityMode = MessageEqualityMode.SemanticModel,
            Limits = new ResourceLimits { MaxTotalBytes = 1 }
        };
        Assert.Multiple(() =>
        {
            Assert.That(checksum.CalculateChecksum(opaqueWrongMode).ErrorCode,
                Is.EqualTo("checksum.model_mode_unsupported"));
            Assert.That(checksum.CalculateChecksum(opaquePaths).ErrorCode,
                Is.EqualTo("checksum.model_key_paths_unsupported"));
            Assert.That(checksum.CalculateChecksum(opaqueLimit).ErrorCode,
                Is.EqualTo("checksum.model_bytes_limit"));
        });

        DynamicXmlNode document = new DynamicXmlNode { Kind = XmlNodeKind.Document };
        DynamicXmlNode element = new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData { LocalName = "x", NamespaceUri = "urn:test" }
        };
        ChecksumRequest documentModel = new ChecksumRequest { Model = document };
        ChecksumRequest elementModel = new ChecksumRequest { Model = element };
        Assert.That(MessageChecksum.CreateInputTree(documentModel)?.Kind,
            Is.EqualTo(XmlNodeKind.Document));
        Assert.That(MessageChecksum.CreateInputTree(elementModel)?.Children, Has.Count.EqualTo(1));
        MessageDocument modelDocument = new MessageDocument();
        modelDocument.Payloads.Add(new MessagePayload { GenericContent = element });
        Assert.That(MessageChecksum.CreateInputTree(new ChecksumRequest
            { Model = modelDocument }), Is.Not.Null);
        Assert.That(MessageChecksum.CreateInputTree(new ChecksumRequest
            { Model = new object() }), Is.Null);
        Assert.That(MessageChecksum.CalculateSemanticModelChecksum(
            new ChecksumRequest
            {
                EqualityMode = MessageEqualityMode.SemanticModel,
                Model = null
            }, "SHA-256").ErrorCode, Is.EqualTo("checksum.model_missing"));

        ChecksumRequest byteDocument = new ChecksumRequest
        {
            Model = element,
            EqualityMode = MessageEqualityMode.Byte
        };
        Assert.That(MessageChecksum.CreateRepresentation(
            element, byteDocument), Is.Not.Empty);

        ChecksumRequest duplicateLimit = new ChecksumRequest
        {
            Model = new DynamicXmlNode
            {
                Kind = XmlNodeKind.Element,
                Name = new QualifiedNameData { LocalName = "Root" }
            }
        };
        DynamicXmlNode duplicateRoot = (DynamicXmlNode)duplicateLimit.Model;
        for (int duplicateIndex = 0; duplicateIndex < 2; duplicateIndex++)
        {
            DynamicXmlNode duplicate = new DynamicXmlNode
            {
                Kind = XmlNodeKind.Element,
                Name = new QualifiedNameData { LocalName = "Id" }
            };
            duplicate.Children.Add(new DynamicXmlNode
                { Kind = XmlNodeKind.Text, Value = "x" });
            duplicateRoot.Children.Add(duplicate);
        }
        duplicateLimit.KeyPaths.Add(new XmlPathData { Value = "/Root/Id" });
        duplicateLimit.Limits.MaxCollectionCount = 1;
        Assert.That(checksum.CalculateChecksum(duplicateLimit).ErrorCode,
            Is.EqualTo("checksum.duplicate_key_limit"));
        Assert.That(checksum.CalculateChecksum(new ChecksumRequest
        {
            Document = new MessageDocument()
        }).ErrorCode, Is.EqualTo("checksum.content_missing"));
        Assert.That(checksum.CalculateChecksum(new ChecksumRequest
        {
            Xml = "<x/>"u8.ToArray(),
            Limits = new ResourceLimits { MaxTotalBytes = 4 }
        }).ErrorCode, Is.EqualTo("checksum.representation_bytes_limit"));

        XmlPathData requested = new XmlPathData();
        requested.Segments.Add(new QualifiedNameData
            { LocalName = "x", NamespaceUri = "urn:test" });
        XmlPathData current = new XmlPathData();
        current.Segments.Add(new QualifiedNameData
            { LocalName = "y", NamespaceUri = "urn:test" });
        Assert.Multiple(() =>
        {
            Assert.That(MessageChecksum.ChecksumPathMatches(requested, new XmlPathData()), Is.False);
            Assert.That(MessageChecksum.ChecksumPathMatches(requested, current), Is.False);
            current.Segments[0].LocalName = "x";
            current.Segments[0].NamespaceUri = "urn:other";
            Assert.That(MessageChecksum.ChecksumPathMatches(requested, current), Is.False);
            current.Segments[0].NamespaceUri = "urn:test";
            Assert.That(MessageChecksum.ChecksumPathMatches(requested, current), Is.True);
            Assert.That(MessageChecksum.ChecksumPathMatches(
                new XmlPathData { Value = "/x" }, current), Is.True);
            Assert.That(MessageChecksum.ContainsValue(new List<string>(), "x"), Is.False);
            Assert.That(MessageChecksum.ContainsValue(new List<string> { "y" }, "x"), Is.False);
            Assert.That(MessageChecksum.ContainsValue(new List<string> { "x" }, "x"), Is.True);
        });

        ChecksumResult diagnostics = new ChecksumResult();
        ChecksumRequest noDiagnostics = new ChecksumRequest();
        noDiagnostics.Limits.MaxDiagnosticCount = 0;
        MessageChecksum.AddDuplicateDiagnostic(diagnostics, current,
            new SourceLocation(), "x", noDiagnostics);
        Assert.That(diagnostics.Diagnostics, Is.Empty);
    }

    private static ChecksumRequest CanonicalRequest(
        string xml,
        ChecksumScope scope)
    {
        return new ChecksumRequest
        {
            Xml = Encoding.UTF8.GetBytes(xml),
            Scope = scope,
            EqualityMode = MessageEqualityMode.CanonicalXml
        };
    }

    private static ChecksumRequest SemanticRequest(string xml)
    {
        return new ChecksumRequest
        {
            Xml = Encoding.UTF8.GetBytes(xml),
            EqualityMode = MessageEqualityMode.SemanticModel
        };
    }

    private class SemanticFixture
    {
        public string Identifier = string.Empty;
        public int Count;
    }
}
