using asn1;

namespace asn1.Tests;

public class XsdBuiltInTypeCatalogTests
{
    [Test]
    public void MapsEveryXsdBuiltInTypeFromX694TableTwo()
    {
        Dictionary<string, string> expected = new Dictionary<string, string>
        {
            ["anyURI"] = "XSDv2.AnyURI",
            ["anySimpleType"] = "XSDv2.AnySimpleType",
            ["anyType"] = "XSDv2.AnyType",
            ["base64Binary"] = "[BASE64] OCTET STRING",
            ["boolean"] = "BOOLEAN",
            ["byte"] = "INTEGER (-128..127)",
            ["date"] = "XSDv2.Date",
            ["dateTime"] = "XSDv2.DateTime",
            ["decimal"] = "XSDv2.Decimal",
            ["double"] = "XSDv2.Double",
            ["duration"] = "XSDv2.Duration",
            ["ENTITIES"] = "XSDv2.ENTITIES",
            ["ENTITY"] = "XSDv2.ENTITY",
            ["float"] = "XSDv2.Float",
            ["gDay"] = "XSDv2.GDay",
            ["gMonth"] = "XSDv2.GMonth",
            ["gMonthDay"] = "XSDv2.GMonthDay",
            ["gYear"] = "XSDv2.GYear",
            ["gYearMonth"] = "XSDv2.GYearMonth",
            ["hexBinary"] = "OCTET STRING",
            ["ID"] = "XSDv2.ID",
            ["IDREF"] = "XSDv2.IDREF",
            ["IDREFS"] = "XSDv2.IDREFS",
            ["int"] = "XSDv2.Int",
            ["integer"] = "INTEGER",
            ["language"] = "XSDv2.Language",
            ["long"] = "XSDv2.Long",
            ["Name"] = "XSDv2.Name",
            ["NCName"] = "XSDv2.NCName",
            ["negativeInteger"] = "INTEGER (MIN..-1)",
            ["NMTOKEN"] = "XSDv2.NMTOKEN",
            ["NMTOKENS"] = "XSDv2.NMTOKENS",
            ["nonNegativeInteger"] = "INTEGER (0..MAX)",
            ["nonPositiveInteger"] = "INTEGER (MIN..0)",
            ["normalizedString"] = "XSDv2.NormalizedString",
            ["NOTATION"] = "XSDv2.NOTATION",
            ["positiveInteger"] = "INTEGER (1..MAX)",
            ["QName"] = "XSDv2.QName",
            ["short"] = "XSDv2.Short",
            ["string"] = "XSDv2.String",
            ["time"] = "XSDv2.Time",
            ["token"] = "XSDv2.Token",
            ["unsignedByte"] = "INTEGER (0..255)",
            ["unsignedInt"] = "XSDv2.UnsignedInt",
            ["unsignedLong"] = "XSDv2.UnsignedLong",
            ["unsignedShort"] = "XSDv2.UnsignedShort"
        };

        XsdBuiltInTypeDefinition[] actual =
            XsdBuiltInTypeCatalog.GetTypes();

        Assert.That(actual, Has.Length.EqualTo(expected.Count));
        Assert.Multiple(() =>
        {
            foreach (KeyValuePair<string, string> pair in expected)
            {
                XsdBuiltInTypeDefinition? definition =
                    XsdBuiltInTypeCatalog.FindType(
                        XsdBuiltInTypeCatalog.SchemaNamespace,
                        pair.Key);
                Assert.That(definition, Is.Not.Null, pair.Key);
                Assert.That(definition?.AsnTypeDefinition,
                    Is.EqualTo(pair.Value), pair.Key);
                Assert.That(definition?.StableId,
                    Is.EqualTo("x694:built-in:" + pair.Key), pair.Key);
            }
        });
    }

    [Test]
    public void AppliesNamespaceAndNillableAnyTypeRules()
    {
        string schema = XsdBuiltInTypeCatalog.SchemaNamespace;

        Assert.Multiple(() =>
        {
            Assert.That(XsdBuiltInTypeCatalog.MapType(
                schema, "anyType", false), Is.EqualTo("XSDv2.AnyType"));
            Assert.That(XsdBuiltInTypeCatalog.MapType(
                schema, "anyType", true),
                Is.EqualTo("XSDv2.AnyType-nillable"));
            Assert.That(XsdBuiltInTypeCatalog.MapType(
                schema, "string", true), Is.EqualTo("XSDv2.String"));
            Assert.That(XsdBuiltInTypeCatalog.FindType(
                "urn:not-xml-schema", "string"), Is.Null);
            Assert.That(XsdBuiltInTypeCatalog.MapType(
                schema, "unknown", false), Is.Empty);
            Assert.That(XsdBuiltInTypeCatalog.FindType(
                schema, "Int"), Is.Null);
        });
    }

    [Test]
    public void PreservesFamiliesAndEffectiveXerInstructions()
    {
        XsdBuiltInTypeDefinition[] definitions =
            XsdBuiltInTypeCatalog.GetTypes();

        Assert.Multiple(() =>
        {
            Assert.That(definitions.Select(value => value.Family).Distinct(),
                Is.EquivalentTo(Enum.GetValues<XsdBuiltInTypeFamily>()));
            Assert.That(Find(definitions, "base64Binary").XerInstructions,
                Is.EqualTo(new[] { "BASE64" }));
            Assert.That(Find(definitions, "QName").XerInstructions,
                Is.EqualTo(new[] { "USE-QNAME" }));
            Assert.That(Find(definitions, "NMTOKENS").XerInstructions,
                Is.EqualTo(new[] { "LIST" }));
            Assert.That(Find(definitions, "normalizedString").XerInstructions,
                Is.EqualTo(new[] { "WHITESPACE REPLACE" }));
            Assert.That(Find(definitions, "duration").XerInstructions,
                Is.EqualTo(new[] { "USE-UNION" }));
            Assert.That(Find(definitions, "boolean").SupportTypeName,
                Is.Empty);
        });
    }

    [Test]
    public void ProvidesStableVersionTwoSupportModule()
    {
        XsdV2SupportModule module = XsdV2SupportModuleCatalog.GetModule();
        XsdBuiltInTypeDefinition[] mappings =
            XsdBuiltInTypeCatalog.GetTypes();
        string[] requiredSupportTypes = mappings
            .Where(value => value.SupportTypeName.Length > 0)
            .Select(value => value.SupportTypeName)
            .Distinct()
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(module.StableId,
                Is.EqualTo("x694:module:xsd:version2"));
            Assert.That(module.ModuleName, Is.EqualTo("XSDv2"));
            Assert.That(module.ObjectIdentifier,
                Is.EqualTo(new[] { 2, 1, 0, 0, 2, 2 }));
            Assert.That(module.InternationalizedResourceIdentifier,
                Is.EqualTo(
                    "/ASN.1/Specification/Modules/XSD-Module/Version2"));
            Assert.That(module.Types, Has.Length.EqualTo(45));
            Assert.That(module.Types.Select(value => value.StableId)
                .Distinct().ToArray(),
                Has.Length.EqualTo(module.Types.Length));
            Assert.That(requiredSupportTypes.Except(module.Types.Select(
                value => value.Name)), Is.Empty);
            Assert.That(module.Types.Single(value => value.Name == "QName")
                .Definition,
                Is.EqualTo(
                    "SEQUENCE { uri AnyURI OPTIONAL, name NCName }"));
            Assert.That(module.Types.Single(value => value.Name == "DateTime")
                .Definition, Does.StartWith("TIME"));
            Assert.That(module.XerEncodingInstructions,
                Does.Contain("USE-QNAME QName"));
            Assert.That(module.XerEncodingInstructions,
                Does.Contain("USE-UNION GenericTimeTypeChoice"));
        });
    }

    [Test]
    public void ImporterInitializesBuiltInMappingsForFutureSchemaMapping()
    {
        XsdSchemaImporterImplementation importer =
            new XsdSchemaImporterImplementation();
        AsnXsdImportResult result = importer.ImportSchemas(
            [new AsnXsdInput
            {
                Name = "built-ins.xsd",
                HasBaseUri = true,
                BaseUri = "urn:built-ins",
                Text = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"/>"
            }],
            new AsnXsdImportOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(importer.LastBuiltInTypes, Has.Length.EqualTo(46));
            Assert.That(importer.LastSupportModule.ModuleName,
                Is.EqualTo("XSDv2"));
            Assert.That(importer.LastBuiltInTypes.Single(value =>
                value.XsdName == "boolean").AsnTypeDefinition,
                Is.EqualTo("BOOLEAN"));
        });
    }

    private static XsdBuiltInTypeDefinition Find(
        XsdBuiltInTypeDefinition[] definitions,
        string name)
    {
        return definitions.Single(value => value.XsdName == name);
    }
}
