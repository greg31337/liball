using System.Text;
using iso20022;

namespace iso20022.Tests;

public class OfficialXsdValidatorTests
{
    [TestCase("Payments", "pacs.002.001.16")]
    [TestCase("Cards", "caad.007.001.05")]
    [TestCase("FX", "fxtr.008.001.08")]
    [TestCase("Securities", "semt.001.001.04")]
    [TestCase("Trade Finance", "tsmt.001.001.03")]
    public void OfficialBusinessDomainSchemasAcceptValidAndRejectInvalidMessages(
        string domain,
        string identifier)
    {
        string fixtureRoot = GetOfficialSchemaFixtureRoot();
        string namespaceUri = GetNamespace(identifier);
        SchemaRegistryOptions options = CreateOptions(fixtureRoot);
        ISchemaRegistry registry = new SchemaRegistry();
        SchemaArtifact schema = RegisterOfficialFileSchema(
            registry,
            fixtureRoot,
            identifier,
            namespaceUri,
            options);
        IXsdValidator validator = new XsdValidator(registry);

        SchemaCompilationResult valid = Validate(
            validator,
            CreateSelector(identifier, namespaceUri),
            options,
            CreateValidDomainMessage(domain, namespaceUri));
        SchemaCompilationResult invalid = Validate(
            validator,
            CreateSelector(identifier, namespaceUri),
            options,
            "<Document xmlns=\"" + namespaceUri + "\"/>");

        AssertValidAndInvalid(domain, schema, valid, invalid);
    }

    [TestCase("head.001.001.01")]
    [TestCase("head.001.001.02")]
    [TestCase("head.001.001.03")]
    [TestCase("head.001.001.04")]
    public void OfficialBahSchemasAcceptValidAndRejectInvalidHeaders(string identifier)
    {
        string fixtureRoot = GetOfficialSchemaFixtureRoot();
        string namespaceUri = GetNamespace(identifier);
        SchemaRegistryOptions options = CreateOptions(fixtureRoot);
        ISchemaRegistry registry = new SchemaRegistry();
        SchemaArtifact schema = RegisterOfficialFileSchema(
            registry,
            fixtureRoot,
            identifier,
            namespaceUri,
            options);
        IXsdValidator validator = new XsdValidator(registry);

        SchemaCompilationResult valid = Validate(
            validator,
            CreateSelector(identifier, namespaceUri),
            options,
            CreateValidHeader(namespaceUri));
        SchemaCompilationResult invalid = Validate(
            validator,
            CreateSelector(identifier, namespaceUri),
            options,
            "<AppHdr xmlns=\"" + namespaceUri + "\"><Fr><OrgId/></Fr></AppHdr>");

        AssertValidAndInvalid(identifier, schema, valid, invalid);
    }

    [TestCase(
        "nvlp.001.001.02",
        "ISO20022_Business_Message_Envelope_V02.zip",
        "BizMsgEnvlp")]
    [TestCase(
        "nvlp.002.001.02",
        "ISO20022_Business_File_Envelope_V02.zip",
        "BizFileEnvlp")]
    public void BundledOfficialEnvelopeSchemasAcceptValidAndRejectInvalidEnvelopes(
        string identifier,
        string archiveName,
        string rootElement)
    {
        string specificationRoot = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../specs/iso20022"));
        string archivePath = Path.Combine(specificationRoot, archiveName);
        string namespaceUri = GetNamespace(identifier);
        SchemaRegistryOptions options = CreateOptions(specificationRoot);
        ISchemaRegistry registry = new SchemaRegistry();
        SchemaArtifact request = CreateRequest(
            identifier,
            namespaceUri,
            Path.Combine(specificationRoot, identifier + ".xsd"),
            new Uri(archivePath).AbsoluteUri);
        SchemaCompilationResult registered;
        using (Stream input = File.OpenRead(archivePath))
        {
            registered = registry.ImportRegisterSchemaArchive(input, request, options);
        }

        SchemaArtifact schema = registered.RootSchema ??
            throw new AssertionException(registered.ErrorMessage);
        IXsdValidator validator = new XsdValidator(registry);
        SchemaCompilationResult valid = Validate(
            validator,
            CreateSelector(identifier, namespaceUri),
            options,
            CreateValidEnvelope(identifier, namespaceUri));
        SchemaCompilationResult invalid = Validate(
            validator,
            CreateSelector(identifier, namespaceUri),
            options,
            "<" + rootElement + " xmlns=\"" + namespaceUri + "\"/>");

        Assert.Multiple(() =>
        {
            Assert.That(registered.Success, Is.True, registered.ErrorMessage);
            Assert.That(registered.Registered, Is.True);
        });
        AssertValidAndInvalid(identifier, schema, valid, invalid);
    }

    private static SchemaArtifact RegisterOfficialFileSchema(
        ISchemaRegistry registry,
        string fixtureRoot,
        string identifier,
        string namespaceUri,
        SchemaRegistryOptions options)
    {
        string path = Path.Combine(fixtureRoot, identifier + ".xsd");
        SchemaArtifact request = CreateRequest(
            identifier,
            namespaceUri,
            path,
            "https://www.iso20022.org/sites/default/files/documents/messages/" +
            identifier.Substring(0, 4) + "/schemas/" + identifier + ".xsd");
        SchemaCompilationResult registered = registry.ImportRegisterSchemaFile(
            path,
            request,
            options);
        Assert.That(registered.Success, Is.True, registered.ErrorMessage);
        Assert.That(registered.Registered, Is.True);
        return registered.RootSchema ??
            throw new AssertionException(registered.ErrorMessage);
    }

    private static SchemaArtifact CreateRequest(
        string identifier,
        string namespaceUri,
        string path,
        string originUri)
    {
        MessageIdentifier messageIdentifier = ParseIdentifier(identifier);
        return new SchemaArtifact
        {
            Artifact = new ArtifactManifest
            {
                Coordinate = new ArtifactCoordinate
                {
                    Kind = ArtifactKind.Schema,
                    Edition = "2026",
                    Identifier = identifier,
                    Version = messageIdentifier.Version
                },
                OriginUri = originUri,
                IsApproved = true
            },
            SourceName = Path.GetFileName(path),
            BaseUri = new Uri(path).AbsoluteUri,
            NamespaceUri = namespaceUri,
            MessageIdentifier = messageIdentifier,
            Version = messageIdentifier.Version,
            Profile = "iso",
            Precedence = 100,
            IsEntryPoint = true
        };
    }

    private static SchemaArtifact CreateSelector(string identifier, string namespaceUri)
    {
        MessageIdentifier messageIdentifier = ParseIdentifier(identifier);
        return new SchemaArtifact
        {
            NamespaceUri = namespaceUri,
            MessageIdentifier = messageIdentifier,
            Version = messageIdentifier.Version,
            Profile = "iso"
        };
    }

    private static MessageIdentifier ParseIdentifier(string identifier)
    {
        MessageIdentifierParseResult parsed =
            new MessageIdentifierParser().ParseIdentifier(identifier);
        return parsed.Identifier ?? throw new AssertionException(parsed.ErrorMessage);
    }

    private static SchemaRegistryOptions CreateOptions(string root)
    {
        return new SchemaRegistryOptions
        {
            LocalRootPath = root
        };
    }

    private static SchemaCompilationResult Validate(
        IXsdValidator validator,
        SchemaArtifact selector,
        SchemaRegistryOptions options,
        string xml)
    {
        return validator.ValidateXmlStream(
            new MemoryStream(Encoding.UTF8.GetBytes(xml)),
            selector,
            options);
    }

    private static void AssertValidAndInvalid(
        string caseName,
        SchemaArtifact schema,
        SchemaCompilationResult valid,
        SchemaCompilationResult invalid)
    {
        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True,
                caseName + ": " + valid.ErrorMessage + " " +
                string.Join(" | ", valid.DiagnosticMessages));
            Assert.That(valid.Found, Is.True, caseName);
            Assert.That(valid.DiagnosticCodes,
                Has.None.EqualTo("schema.validation.error"), caseName);
            Assert.That(invalid.Success, Is.False, caseName);
            Assert.That(invalid.ErrorCode,
                Is.EqualTo("schema.validation.invalid"), caseName);
            Assert.That(invalid.DiagnosticCodes,
                Has.Some.EqualTo("schema.validation.error"), caseName);
            Assert.That(invalid.DiagnosticSchemaUris,
                Is.All.EqualTo(schema.BaseUri), caseName);
            Assert.That(invalid.ReusedCachedCompilation, Is.True, caseName);
        });
    }

    private static string GetOfficialSchemaFixtureRoot()
    {
        return Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../tests/Fixtures/OfficialSchemas"));
    }

    private static string GetNamespace(string identifier)
    {
        if (identifier == "semt.001.001.04")
        {
            return "urn:swift:xsd:" + identifier;
        }

        return Iso20022Namespaces.SchemaUrnPrefix + identifier;
    }

    private static string CreateValidDomainMessage(string domain, string namespaceUri)
    {
        if (domain == "Payments")
        {
            return "<Document xmlns=\"" + namespaceUri + "\">" +
                "<FIToFIPmtStsRpt><GrpHdr><MsgId>PAYMENT-1</MsgId>" +
                "<CreDtTm>2026-08-31T12:00:00Z</CreDtTm></GrpHdr>" +
                "</FIToFIPmtStsRpt></Document>";
        }

        if (domain == "Cards")
        {
            return "<Document xmlns=\"" + namespaceUri + "\"><Err>" +
                "<Hdr><MsgFctn>ERRO</MsgFctn></Hdr>" +
                "<SysTracAudtNb>1</SysTracAudtNb>" +
                "<TrnsmssnDtTm>2026-08-31T12:00:00Z</TrnsmssnDtTm>" +
                "</Err></Document>";
        }

        if (domain == "FX")
        {
            return "<Document xmlns=\"" + namespaceUri + "\">" +
                "<FXTradStsNtfctn><TradData><MsgId>FX-1</MsgId>" +
                "<MtchgSysUnqRef>MATCH-1</MtchgSysUnqRef>" +
                "<CurSts><StsCd><Cd>INVA</Cd></StsCd></CurSts>" +
                "</TradData></FXTradStsNtfctn></Document>";
        }

        if (domain == "Securities")
        {
            return "<Document xmlns=\"" + namespaceUri + "\">" +
                "<SctiesMsgRjctn><RltdRef><Ref>SEC-1</Ref>" +
                "<MsgNb><ShrtNb>001</ShrtNb></MsgNb></RltdRef>" +
                "<Rsn><Rsn>NALO</Rsn></Rsn>" +
                "</SctiesMsgRjctn></Document>";
        }

        return "<Document xmlns=\"" + namespaceUri + "\"><Ack>" +
            "<AckId><Id>TRADE-ACK-1</Id>" +
            "<CreDtTm>2026-08-31T12:00:00Z</CreDtTm></AckId>" +
            "<AckdMsgRef><Id>TRADE-1</Id>" +
            "<CreDtTm>2026-08-31T11:59:00Z</CreDtTm></AckdMsgRef>" +
            "</Ack></Document>";
    }

    private static string CreateValidHeader(string namespaceUri)
    {
        return "<AppHdr xmlns=\"" + namespaceUri + "\">" +
            "<Fr><OrgId/></Fr><To><OrgId/></To>" +
            "<BizMsgIdr>BUSINESS-MESSAGE-1</BizMsgIdr>" +
            "<MsgDefIdr>pacs.002.001.16</MsgDefIdr>" +
            "<CreDt>2026-08-31T12:00:00Z</CreDt>" +
            "</AppHdr>";
    }

    private static string CreateValidEnvelope(string identifier, string namespaceUri)
    {
        if (identifier == "nvlp.001.001.02")
        {
            return "<BizMsgEnvlp xmlns=\"" + namespaceUri + "\"><Doc>" +
                "<Document xmlns=\"urn:iso:std:iso:20022:tech:xsd:pacs.002.001.16\"/>" +
                "</Doc></BizMsgEnvlp>";
        }

        return "<BizFileEnvlp xmlns=\"" + namespaceUri + "\">" +
            "<PyldData><BizFileIdr>FILE-1</BizFileIdr>" +
            "<CreDt>2026-08-31T12:00:00Z</CreDt></PyldData>" +
            "<Pyld><BizCntt>" +
            "<Document xmlns=\"urn:iso:std:iso:20022:tech:xsd:pacs.002.001.16\"/>" +
            "</BizCntt></Pyld></BizFileEnvlp>";
    }
}
