using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using iso20022;

namespace iso20022.Tests;

public class OfficialXsdGeneratorTests
{
    [Test]
    public void OfficialChoicesSequencesFacetsCodesWildcardsAndNamespacesKeepTheirShape()
    {
        XDocument officialPacs = XDocument.Load(GetOfficialSchemaPath("pacs.002.001.16"));
        XDocument generatedPacs = GenerateOfficialSchemaDocument("pacs.002.001.16");
        XDocument officialCards = XDocument.Load(GetOfficialSchemaPath("caad.007.001.05"));
        XDocument generatedCards = GenerateOfficialSchemaDocument("caad.007.001.05");
        XElement officialAmount = FindNamedSchemaType(
            officialPacs, "simpleType", "ActiveCurrencyAndAmount_SimpleType");
        XElement generatedAmount = FindNamedSchemaType(
            generatedPacs, "simpleType", "ActiveCurrencyAndAmount_SimpleType");
        XElement officialCodes = FindNamedSchemaType(
            officialPacs, "simpleType", "AddressType2Code");
        XElement generatedCodes = FindNamedSchemaType(
            generatedPacs, "simpleType", "AddressType2Code");
        XElement officialChoice = FindNamedSchemaType(
            officialPacs, "complexType", "AccountIdentification4Choice");
        XElement generatedChoice = FindNamedSchemaType(
            generatedPacs, "complexType", "AccountIdentification4Choice");
        XElement officialSequence = FindNamedSchemaType(
            officialPacs, "complexType", "Contact13");
        XElement generatedSequence = FindNamedSchemaType(
            generatedPacs, "complexType", "Contact13");
        XElement officialWildcard = FindNamedSchemaType(
            officialCards, "complexType", "ATICALaxProcessing");
        XElement generatedWildcard = FindNamedSchemaType(
            generatedCards, "complexType", "ATICALaxProcessing");

        Assert.Multiple(() =>
        {
            Assert.That(CreateFacetSignature(generatedAmount),
                Is.EqualTo(CreateFacetSignature(officialAmount)));
            Assert.That(CreateFacetSignature(generatedCodes),
                Is.EqualTo(CreateFacetSignature(officialCodes)));
            Assert.That(CreateChoiceSignature(generatedChoice),
                Is.EqualTo(CreateChoiceSignature(officialChoice)));
            Assert.That(CreateElementParticleSignature(generatedSequence, "Othr"),
                Is.EqualTo(CreateElementParticleSignature(officialSequence, "Othr")));
            Assert.That(CreateWildcardSignature(generatedWildcard),
                Is.EqualTo(CreateWildcardSignature(officialWildcard)));
            Assert.That(generatedPacs.Root?.Attribute("targetNamespace")?.Value,
                Is.EqualTo(officialPacs.Root?.Attribute("targetNamespace")?.Value));
            Assert.That(generatedPacs.Root?.Attribute("elementFormDefault")?.Value,
                Is.EqualTo("qualified"));
        });
    }

    [TestCase("pacs.002.001.16")]
    [TestCase("caad.007.001.05")]
    [TestCase("fxtr.008.001.08")]
    [TestCase("semt.001.001.04")]
    [TestCase("tsmt.001.001.03")]
    public void OfficialDomainSchemasRoundTripThroughNormalizedGeneration(string identifier)
    {
        string fixturePath = GetOfficialSchemaPath(identifier);
        SchemaArtifact official = ImportSchemaFile(fixturePath, identifier);
        GenerationPlan plan = new GenerationPlan();
        plan.Schemas.Add(official);
        plan.Options.EmitManifest = false;

        GenerationResult generated = new XsdGenerator().GenerateXsd(plan);

        Assert.That(generated.Success, Is.True, generated.ErrorMessage);
        GeneratedFile file = generated.Files.Single(value =>
            value.Kind == GenerationFileKind.Xsd);
        XmlSchemaSet compiled = CompileSchema(file.Content);
        SchemaArtifact regenerated = ImportSchemaBytes(file.Content, identifier);
        string[] expectedDataTypes = CreateDataTypeSignatures(official);
        string[] actualDataTypes = CreateDataTypeSignatures(regenerated);
        string[] expectedComponents = CreateComponentSignatures(official);
        string[] actualComponents = CreateComponentSignatures(regenerated);

        Assert.Multiple(() =>
        {
            Assert.That(compiled.IsCompiled, Is.True, identifier);
            Assert.That(regenerated.NamespaceUri, Is.EqualTo(official.NamespaceUri), identifier);
            Assert.That(regenerated.RootElement, Is.EqualTo(official.RootElement), identifier);
            Assert.That(actualDataTypes, Is.EqualTo(expectedDataTypes), identifier);
            Assert.That(actualComponents, Is.SupersetOf(expectedComponents), identifier);
            Assert.That(regenerated.MessageDefinitions.Select(value => value.RootElement),
                Is.EqualTo(official.MessageDefinitions.Select(value => value.RootElement)),
                identifier);
        });
    }

    [TestCase("Payments", "pacs.002.001.16")]
    [TestCase("Cards", "caad.007.001.05")]
    [TestCase("FX", "fxtr.008.001.08")]
    [TestCase("Securities", "semt.001.001.04")]
    [TestCase("Trade", "tsmt.001.001.03")]
    public void GeneratedSchemasMatchOfficialValidAndInvalidMessageBehavior(
        string domain,
        string identifier)
    {
        string fixturePath = GetOfficialSchemaPath(identifier);
        byte[] officialContent = File.ReadAllBytes(fixturePath);
        SchemaArtifact official = ImportSchemaFile(fixturePath, identifier);
        GenerationPlan plan = new GenerationPlan();
        plan.Schemas.Add(official);
        plan.Options.EmitManifest = false;
        GenerationResult generated = new XsdGenerator().GenerateXsd(plan);
        Assert.That(generated.Success, Is.True, generated.ErrorMessage);
        byte[] generatedContent = generated.Files.Single(value =>
            value.Kind == GenerationFileKind.Xsd).Content;
        string validXml = CreateValidMessage(domain, official.NamespaceUri);
        string invalidXml = "<Document xmlns=\"" + official.NamespaceUri + "\"/>";

        List<string> officialValid = ValidateXml(officialContent, validXml);
        List<string> generatedValid = ValidateXml(generatedContent, validXml);
        List<string> officialInvalid = ValidateXml(officialContent, invalidXml);
        List<string> generatedInvalid = ValidateXml(generatedContent, invalidXml);

        Assert.Multiple(() =>
        {
            Assert.That(officialValid, Is.Empty, identifier);
            Assert.That(generatedValid, Is.Empty, identifier);
            Assert.That(officialInvalid, Is.Not.Empty, identifier);
            Assert.That(generatedInvalid, Is.Not.Empty, identifier);
        });
    }

    private static SchemaArtifact ImportSchemaFile(string path, string identifier)
    {
        SchemaArtifact request = CreateRequest(identifier, path);
        SchemaRegistryOptions options = new SchemaRegistryOptions
        {
            ResolveIncludes = false,
            ResolveImports = false
        };
        SchemaCompilationResult result = new SchemaImporter().ImportSchemaFile(
            path,
            request,
            options);
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        return result.RootSchema ?? throw new AssertionException(result.ErrorMessage);
    }

    private static XDocument GenerateOfficialSchemaDocument(string identifier)
    {
        SchemaArtifact official = ImportSchemaFile(GetOfficialSchemaPath(identifier), identifier);
        GenerationPlan plan = new GenerationPlan();
        plan.Schemas.Add(official);
        plan.Options.EmitManifest = false;
        GenerationResult result = new XsdGenerator().GenerateXsd(plan);
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        byte[] content = result.Files.Single(value =>
            value.Kind == GenerationFileKind.Xsd).Content;
        return XDocument.Parse(Encoding.UTF8.GetString(content));
    }

    private static XElement FindNamedSchemaType(
        XDocument document,
        string localName,
        string name)
    {
        XNamespace schemaNamespace = Iso20022Namespaces.XmlSchema;
        return document.Root?.Elements(schemaNamespace + localName).Single(value =>
            value.Attribute("name")?.Value == name) ??
            throw new AssertionException("Missing schema type " + name);
    }

    private static string CreateFacetSignature(XElement type)
    {
        XNamespace schemaNamespace = Iso20022Namespaces.XmlSchema;
        XElement restriction = type.Element(schemaNamespace + "restriction") ??
            throw new AssertionException("Missing simple-type restriction.");
        return FindLocalName(restriction.Attribute("base")?.Value ?? string.Empty) + "|" +
            string.Join(",", restriction.Elements().Select(value =>
                    value.Name.LocalName + "=" + (value.Attribute("value")?.Value ?? string.Empty))
                .OrderBy(value => value, StringComparer.Ordinal));
    }

    private static string CreateChoiceSignature(XElement type)
    {
        XNamespace schemaNamespace = Iso20022Namespaces.XmlSchema;
        XElement choice = type.Descendants(schemaNamespace + "choice").SingleOrDefault() ??
            throw new AssertionException("Missing choice particle.");
        return CreateOccursSignature(choice) + "|" + string.Join(",",
            choice.Elements(schemaNamespace + "element").Select(value =>
                (value.Attribute("name")?.Value ?? string.Empty) + ":" +
                FindLocalName(value.Attribute("type")?.Value ?? string.Empty) + ":" +
                CreateOccursSignature(value)));
    }

    private static string CreateElementParticleSignature(XElement type, string elementName)
    {
        XNamespace schemaNamespace = Iso20022Namespaces.XmlSchema;
        XElement element = type.Descendants(schemaNamespace + "element").Single(value =>
            value.Attribute("name")?.Value == elementName);
        return elementName + ":" +
            FindLocalName(element.Attribute("type")?.Value ?? string.Empty) + ":" +
            CreateOccursSignature(element);
    }

    private static string CreateWildcardSignature(XElement type)
    {
        XNamespace schemaNamespace = Iso20022Namespaces.XmlSchema;
        XElement wildcard = type.Descendants(schemaNamespace + "any").Single();
        return (wildcard.Attribute("namespace")?.Value ?? "##any") + ":" +
            (wildcard.Attribute("processContents")?.Value ?? "strict") + ":" +
            CreateOccursSignature(wildcard);
    }

    private static string CreateOccursSignature(XElement particle)
    {
        return (particle.Attribute("minOccurs")?.Value ?? "1") + ":" +
            (particle.Attribute("maxOccurs")?.Value ?? "1");
    }

    private static SchemaArtifact ImportSchemaBytes(byte[] content, string identifier)
    {
        SchemaArtifact request = CreateRequest(identifier, identifier + ".generated.xsd");
        SchemaRegistryOptions options = new SchemaRegistryOptions
        {
            ResolveIncludes = false,
            ResolveImports = false
        };
        SchemaCompilationResult result = new SchemaImporter().ImportSchemaStream(
            new MemoryStream(content),
            request,
            options);
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        return result.RootSchema ?? throw new AssertionException(result.ErrorMessage);
    }

    private static SchemaArtifact CreateRequest(string identifier, string path)
    {
        string[] parts = identifier.Split('.');
        MessageIdentifier messageIdentifier = new MessageIdentifier
        {
            Value = identifier,
            BusinessArea = parts[0],
            MessageFunctionality = parts[1],
            Variant = parts[2],
            Version = parts[3]
        };
        string namespaceUri = Iso20022Namespaces.SchemaUrnPrefix + identifier;
        if (identifier == "semt.001.001.04")
        {
            namespaceUri = "urn:swift:xsd:" + identifier;
        }
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
                OriginUri = new Uri(Path.GetFullPath(path)).AbsoluteUri,
                IsApproved = true
            },
            SourceName = Path.GetFileName(path),
            BaseUri = new Uri(Path.GetFullPath(path)).AbsoluteUri,
            NamespaceUri = namespaceUri,
            MessageIdentifier = messageIdentifier,
            Version = messageIdentifier.Version,
            Profile = "iso",
            IsEntryPoint = true
        };
    }

    private static string[] CreateDataTypeSignatures(SchemaArtifact schema)
    {
        return schema.DataTypes.Select(type =>
                type.Name + "|" + FindLocalName(type.SchemaTypeName) + "|" +
                string.Join(",", type.Facets.SelectMany(facet =>
                        CreateFacetSignatures(facet))
                    .OrderBy(value => value, StringComparer.Ordinal)))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<string> CreateFacetSignatures(DataTypeFacet facet)
    {
        if (facet.Name == "enumeration")
        {
            return facet.EnumerationValues.Select(value => facet.Name + "=" + value);
        }
        return new[] { facet.Name + "=" + facet.Value };
    }

    private static string[] CreateComponentSignatures(SchemaArtifact schema)
    {
        return schema.MessageComponents.Select(component =>
                component.Name + "|" + string.Join(",", component.MessageElements.Select(
                    reference => CreateElementSignature(schema, reference))))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static string CreateElementSignature(
        SchemaArtifact schema,
        RepositoryReference reference)
    {
        MessageElementDefinition element = schema.MessageElements.Single(value =>
            value.Identifier == reference.TargetIdentifier);
        string maximum = element.Multiplicity.HasMaximumOccurs
            ? element.Multiplicity.MaximumOccurs.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            : "unbounded";
        return element.XmlTag + ":" + FindLocalName(element.MemberType.TargetIdentifier) + ":" +
            element.Multiplicity.MinimumOccurs.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + ":" + maximum;
    }

    private static string FindLocalName(string qualifiedName)
    {
        int index = qualifiedName.IndexOf(':');
        if (index >= 0)
        {
            return qualifiedName.Substring(index + 1);
        }
        return qualifiedName;
    }

    private static XmlSchemaSet CompileSchema(byte[] content)
    {
        XmlReaderSettings settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        XmlSchemaSet schemas = new XmlSchemaSet
        {
            XmlResolver = null
        };
        using MemoryStream input = new MemoryStream(content);
        using XmlReader reader = XmlReader.Create(input, settings);
        schemas.Add(null, reader);
        schemas.Compile();
        return schemas;
    }

    private static List<string> ValidateXml(byte[] schema, string xml)
    {
        XmlSchemaSet schemas = CompileSchema(schema);
        List<string> errors = new List<string>();
        XmlReaderSettings settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            ValidationType = ValidationType.Schema,
            Schemas = schemas
        };
        settings.ValidationEventHandler += (_, eventArgs) => errors.Add(eventArgs.Message);
        using StringReader text = new StringReader(xml);
        using XmlReader reader = XmlReader.Create(text, settings);
        while (reader.Read())
        {
        }
        return errors;
    }

    private static string GetOfficialSchemaPath(string identifier)
    {
        return Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../tests/Fixtures/OfficialSchemas",
            identifier + ".xsd"));
    }

    private static string CreateValidMessage(string domain, string namespaceUri)
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
}
