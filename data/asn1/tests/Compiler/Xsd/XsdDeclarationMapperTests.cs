using asn1;

namespace asn1.Tests;

public class XsdDeclarationMapperTests
{
    [Test]
    public void MapsGlobalElementAndAttributeAssignments()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:element name=\"customer-id\" type=\"xs:int\" "
                + "nillable=\"true\" default=\"+05\" "
                + "block=\"#all\" final=\"restriction\"/>"
                + "<xs:attribute name=\"mode\" type=\"xs:string\" "
                + "fixed=\"safe\"/>"
                + "<xs:element name=\"hidden\" type=\"xs:string\" "
                + "abstract=\"true\"/>");
        XsdDeclarationDefinition element = GlobalElement(
            importer,
            "customer-id");
        XsdDeclarationDefinition attribute = importer.LastDeclarationResult
            .Attributes.Single(value => value.Global && value.Name == "mode");
        XsdDeclarationDefinition hidden = GlobalElement(importer, "hidden");

        Assert.Multiple(() =>
        {
            Assert.That(importer.LastDeclarationResult.Success,
                Is.True, Format(importer.LastDeclarationResult.Diagnostics));
            Assert.That(element.TypeReferenceName, Is.EqualTo("Customer-id"));
            Assert.That(element.Identifier, Is.EqualTo("customer-id"));
            Assert.That(element.Qualified, Is.True);
            Assert.That(element.NamespaceUri, Is.EqualTo("urn:test"));
            Assert.That(element.Nillable, Is.True);
            Assert.That(element.AsnTypeDefinition,
                Is.EqualTo("SEQUENCE {content XSDv2.Int OPTIONAL}"));
            Assert.That(element.XerInstructions.Select(value => value.Kind),
                Does.Contain(AsnInstructionKind.UseNil));
            Assert.That(element.XerInstructions.Select(value => value.Kind),
                Does.Contain(AsnInstructionKind.Namespace));
            Assert.That(element.ValueConstraint?.CanonicalValue,
                Is.EqualTo("5"));
            Assert.That(element.BlockValues,
                Is.EqualTo(new[] { "extension", "restriction", "substitution" }));
            Assert.That(element.FinalValues,
                Is.EqualTo(new[] { "restriction" }));
            Assert.That(attribute.Optional, Is.False);
            Assert.That(attribute.ValueConstraint?.FixedConstraint,
                Is.Not.Null);
            Assert.That(attribute.XerInstructions.Select(value => value.Kind),
                Does.Contain(AsnInstructionKind.Attribute));
            Assert.That(hidden.Ignored, Is.True);
        });
    }

    [Test]
    public void MapsLocalQualificationAndOccurrenceRanges()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:complexType name=\"Container\">"
                + "<xs:sequence>"
                + "<xs:element name=\"Item\" type=\"xs:string\" "
                + "minOccurs=\"0\" maxOccurs=\"3\"/>"
                + "<xs:element name=\"loose\" type=\"xs:boolean\" "
                + "form=\"unqualified\" minOccurs=\"0\"/>"
                + "<xs:element name=\"many\" type=\"xs:int\" "
                + "minOccurs=\"0\" maxOccurs=\"unbounded\"/>"
                + "</xs:sequence>"
                + "<xs:attribute name=\"code\" type=\"xs:int\" "
                + "use=\"required\"/>"
                + "<xs:attribute name=\"limit\" type=\"xs:int\" "
                + "default=\"+9\"/>"
                + "</xs:complexType>",
            "elementFormDefault=\"qualified\" "
                + "attributeFormDefault=\"unqualified\"");
        XsdDeclarationDefinition item = LocalElement(importer, "Item");
        XsdDeclarationDefinition loose = LocalElement(importer, "loose");
        XsdDeclarationDefinition many = LocalElement(importer, "many");
        XsdDeclarationDefinition code = importer.LastDeclarationResult
            .Attributes.Single(value => !value.Global && value.Name == "code");
        XsdDeclarationDefinition limit = importer.LastDeclarationResult
            .Attributes.Single(value => !value.Global && value.Name == "limit");

        Assert.Multiple(() =>
        {
            Assert.That(item.Qualified, Is.True);
            Assert.That(item.Identifier, Is.EqualTo("item"));
            Assert.That(item.MinimumOccurs,
                Is.EqualTo(System.Numerics.BigInteger.Zero));
            Assert.That(item.MaximumOccurs,
                Is.EqualTo(new System.Numerics.BigInteger(3)));
            Assert.That(item.Repeated, Is.True);
            Assert.That(item.OccurrenceTypeDefinition,
                Is.EqualTo("SEQUENCE (SIZE(0..3)) OF item XSDv2.String"));
            Assert.That(loose.Qualified, Is.False);
            Assert.That(loose.NamespaceUri, Is.Empty);
            Assert.That(loose.Optional, Is.True);
            Assert.That(loose.OccurrenceTypeDefinition,
                Is.EqualTo("BOOLEAN OPTIONAL"));
            Assert.That(many.MaximumOccursUnbounded, Is.True);
            Assert.That(many.OccurrenceTypeDefinition,
                Is.EqualTo("SEQUENCE OF many XSDv2.Int"));
            Assert.That(code.Qualified, Is.False);
            Assert.That(code.Required, Is.True);
            Assert.That(code.Optional, Is.False);
            Assert.That(limit.OccurrenceTypeDefinition,
                Is.EqualTo("XSDv2.Int DEFAULT 9"));
        });
    }

    [Test]
    public void MapsAnonymousSimpleAndComplexDeclarationTypes()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:element name=\"score\">"
                + "<xs:simpleType><xs:restriction base=\"xs:integer\">"
                + "<xs:minInclusive value=\"0\"/>"
                + "</xs:restriction></xs:simpleType>"
                + "</xs:element>"
                + "<xs:element name=\"emptyRecord\">"
                + "<xs:complexType/>"
                + "</xs:element>"
                + "<xs:complexType name=\"Holder\">"
                + "<xs:attribute name=\"level\" fixed=\"+2\">"
                + "<xs:simpleType><xs:restriction base=\"xs:int\"/>"
                + "</xs:simpleType>"
                + "</xs:attribute>"
                + "</xs:complexType>");
        XsdDeclarationDefinition score = GlobalElement(importer, "score");
        XsdDeclarationDefinition empty = GlobalElement(
            importer,
            "emptyRecord");
        XsdDeclarationDefinition level = importer.LastDeclarationResult
            .Attributes.Single(value => value.Name == "level");

        Assert.Multiple(() =>
        {
            Assert.That(score.TypeUse?.Anonymous, Is.True);
            Assert.That(score.TypeUse?.Kind, Is.EqualTo("simple"));
            Assert.That(score.TypeUse?.SimpleType?.Constraints.Single().Kind,
                Is.EqualTo(AsnConstraintKind.ValueRange));
            Assert.That(empty.TypeUse?.Anonymous, Is.True);
            Assert.That(empty.TypeUse?.Kind, Is.EqualTo("complex"));
            Assert.That(empty.TypeUse?.PendingComplexMapping, Is.False);
            Assert.That(empty.AsnTypeDefinition, Is.EqualTo("SEQUENCE {}"));
            Assert.That(level.TypeUse?.Anonymous, Is.True);
            Assert.That(level.ValueConstraint?.CanonicalValue, Is.EqualTo("2"));
            Assert.That(level.ValueConstraint?.FixedConstraint, Is.Not.Null);
        });
    }

    [Test]
    public void ResolvesLocalReferencesToGlobalDeclarations()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:element name=\"entry\" nillable=\"true\">"
                + "<xs:simpleType><xs:restriction base=\"xs:string\"/>"
                + "</xs:simpleType>"
                + "</xs:element>"
                + "<xs:complexType name=\"Entries\"><xs:sequence>"
                + "<xs:element ref=\"tns:entry\" minOccurs=\"1\" "
                + "maxOccurs=\"4\"/>"
                + "</xs:sequence></xs:complexType>");
        XsdDeclarationDefinition reference = importer.LastDeclarationResult
            .Elements.Single(value => value.Reference);

        Assert.Multiple(() =>
        {
            Assert.That(reference.Name, Is.EqualTo("entry"));
            Assert.That(reference.ReferenceNamespace, Is.EqualTo("urn:test"));
            Assert.That(reference.Qualified, Is.True);
            Assert.That(reference.Nillable, Is.True);
            Assert.That(reference.TypeUse?.Anonymous, Is.True);
            Assert.That(reference.OccurrenceTypeDefinition,
                Does.StartWith("SEQUENCE (SIZE(1..4)) OF entry "));
            Assert.That(reference.XerInstructions.Select(value => value.Kind),
                Does.Contain(AsnInstructionKind.UseNil));
        });
    }

    [Test]
    public void AppliesSchemaBlockAndFinalDefaults()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:element name=\"item\" type=\"xs:string\"/>",
            "blockDefault=\"substitution restriction\" "
                + "finalDefault=\"#all\"");
        XsdDeclarationDefinition item = GlobalElement(importer, "item");

        Assert.Multiple(() =>
        {
            Assert.That(item.BlockValues,
                Is.EqualTo(new[] { "substitution", "restriction" }));
            Assert.That(item.FinalValues,
                Is.EqualTo(new[] { "extension", "restriction" }));
        });
    }

    [Test]
    public void DiagnosesInvalidOccurrenceFormsAndFlags()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:complexType name=\"Bad\"><xs:sequence>"
                + "<xs:element name=\"count\" type=\"xs:int\" "
                + "minOccurs=\"4\" maxOccurs=\"2\"/>"
                + "<xs:element name=\"form\" type=\"xs:int\" "
                + "form=\"sometimes\"/>"
                + "</xs:sequence>"
                + "<xs:attribute name=\"state\" type=\"xs:string\" "
                + "use=\"sometimes\"/>"
                + "</xs:complexType>"
                + "<xs:element name=\"flag\" type=\"xs:string\" "
                + "nillable=\"sometimes\" block=\"invalid\"/>");

        Assert.Multiple(() =>
        {
            Assert.That(importer.LastDeclarationResult.Success, Is.False);
            Assert.That(importer.LastDeclarationResult.Diagnostics.Select(
                value => value.Code), Does.Contain("ASNXSD0025"));
            Assert.That(importer.LastDeclarationResult.Diagnostics.Select(
                value => value.Code), Does.Contain("ASNXSD0026"));
            Assert.That(importer.LastDeclarationResult.Diagnostics.All(value =>
                value.Span?.Start.SourceName == "declarations.xsd"), Is.True);
        });
    }

    private static XsdSchemaImporterImplementation Import(
        string content,
        string schemaAttributes = "")
    {
        XsdSchemaImporterImplementation importer =
            new XsdSchemaImporterImplementation();
        importer.ImportSchemas(
            [new AsnXsdInput
            {
                Name = "declarations.xsd",
                HasBaseUri = true,
                BaseUri = "urn:declarations-document",
                Text = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" "
                    + "xmlns:tns=\"urn:test\" targetNamespace=\"urn:test\" "
                    + schemaAttributes + ">" + content + "</xs:schema>"
            }],
            new AsnXsdImportOptions());
        return importer;
    }

    private static XsdDeclarationDefinition GlobalElement(
        XsdSchemaImporterImplementation importer,
        string name)
    {
        return importer.LastDeclarationResult.Elements.Single(value =>
            value.Global && value.Name == name);
    }

    private static XsdDeclarationDefinition LocalElement(
        XsdSchemaImporterImplementation importer,
        string name)
    {
        return importer.LastDeclarationResult.Elements.Single(value =>
            !value.Global && value.Name == name);
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(System.Environment.NewLine, diagnostics.Select(
            value => value.Code + ": " + value.Message));
    }
}
