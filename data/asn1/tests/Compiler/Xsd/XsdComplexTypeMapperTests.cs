using asn1;

namespace asn1.Tests;

public class XsdComplexTypeMapperTests
{
    [Test]
    public void MapsSequencesChoicesAndNestedOccurrenceRanges()
    {
        XsdComplexTypeMappingResult mapped = Map(
            "<xs:complexType name=\"Basket\">"
                + "<xs:sequence>"
                + "<xs:element name=\"id\" type=\"xs:int\"/>"
                + "<xs:sequence>"
                + "<xs:element name=\"left\" type=\"xs:string\"/>"
                + "<xs:element name=\"right\" type=\"xs:string\"/>"
                + "</xs:sequence>"
                + "<xs:choice minOccurs=\"0\">"
                + "<xs:element name=\"small\" type=\"xs:boolean\"/>"
                + "<xs:element name=\"large\" type=\"xs:int\"/>"
                + "</xs:choice>"
                + "<xs:choice minOccurs=\"2\" maxOccurs=\"4\">"
                + "<xs:element name=\"red\" type=\"xs:string\"/>"
                + "<xs:element name=\"blue\" type=\"xs:string\"/>"
                + "</xs:choice>"
                + "</xs:sequence>"
                + "<xs:attribute name=\"version\" type=\"xs:int\" "
                + "use=\"required\"/>"
                + "</xs:complexType>");
        XsdComplexTypeDefinition basket = FindType(mapped, "Basket");
        XsdParticleDefinition root = basket.Particle
            ?? new XsdParticleDefinition();

        Assert.Multiple(() =>
        {
            Assert.That(mapped.Success, Is.True, Format(mapped.Diagnostics));
            Assert.That(root.Kind, Is.EqualTo("sequence"));
            Assert.That(root.Children[1].PointlessSequence, Is.True);
            Assert.That(root.Children[2].Optional, Is.True);
            Assert.That(root.Children[2].NamedTypeDefinition,
                Does.EndWith("OPTIONAL"));
            Assert.That(root.Children[3].Repeated, Is.True);
            Assert.That(root.Children[3].Constraints.Single().Kind,
                Is.EqualTo(AsnConstraintKind.Size));
            Assert.That(root.Children[3].NamedTypeDefinition,
                Does.Contain("SIZE(2..4)"));
            Assert.That(basket.AsnTypeDefinition, Does.Contain("id XSDv2.Int"));
            Assert.That(basket.AsnTypeDefinition,
                Does.Contain("left XSDv2.String, right XSDv2.String"));
            Assert.That(basket.AsnTypeDefinition,
                Does.Contain("choice CHOICE {small BOOLEAN, large XSDv2.Int} OPTIONAL"));
            Assert.That(basket.AsnTypeDefinition,
                Does.Contain("version XSDv2.Int"));
        });
    }

    [Test]
    public void MapsModelGroupReferencesAndOccurrenceNesting()
    {
        XsdComplexTypeMappingResult mapped = Map(
            "<xs:group name=\"Contact\"><xs:choice>"
                + "<xs:element name=\"email\" type=\"xs:string\"/>"
                + "<xs:element name=\"phone\" type=\"xs:string\"/>"
                + "</xs:choice></xs:group>"
                + "<xs:complexType name=\"Directory\"><xs:sequence>"
                + "<xs:group ref=\"tns:Contact\" minOccurs=\"0\" "
                + "maxOccurs=\"2\"/>"
                + "</xs:sequence></xs:complexType>");
        XsdModelGroupDefinition contact = mapped.ModelGroups.Single();
        XsdComplexTypeDefinition directory = FindType(mapped, "Directory");
        XsdParticleDefinition reference = directory.Particle?.Children.Single()
            ?? new XsdParticleDefinition();

        Assert.Multiple(() =>
        {
            Assert.That(contact.Kind, Is.EqualTo("choice"));
            Assert.That(contact.AsnTypeDefinition,
                Is.EqualTo("CHOICE {email XSDv2.String, phone XSDv2.String}"));
            Assert.That(contact.XerInstructions.Single().Kind,
                Is.EqualTo(AsnInstructionKind.Untagged));
            Assert.That(reference.ReferencedGroup, Is.SameAs(contact));
            Assert.That(reference.NamedTypeDefinition,
                Is.EqualTo("contact-list SEQUENCE (SIZE(0..2)) OF contact Contact"));
            Assert.That(directory.AsnTypeDefinition,
                Does.Contain(reference.NamedTypeDefinition));
        });
    }

    [Test]
    public void ExpandsNestedAttributeGroups()
    {
        XsdComplexTypeMappingResult mapped = Map(
            "<xs:attributeGroup name=\"Identity\">"
                + "<xs:attribute name=\"id\" type=\"xs:int\" "
                + "use=\"required\"/>"
                + "</xs:attributeGroup>"
                + "<xs:attributeGroup name=\"Metadata\">"
                + "<xs:attributeGroup ref=\"tns:Identity\"/>"
                + "<xs:attribute name=\"note\" type=\"xs:string\"/>"
                + "</xs:attributeGroup>"
                + "<xs:complexType name=\"Record\">"
                + "<xs:attributeGroup ref=\"tns:Metadata\"/>"
                + "<xs:attribute name=\"active\" type=\"xs:boolean\" "
                + "default=\"true\"/>"
                + "</xs:complexType>");
        XsdComplexTypeDefinition record = FindType(mapped, "Record");
        XsdModelGroupDefinition metadata = mapped.AttributeGroups.Single(
            value => value.Name == "Metadata");

        Assert.Multiple(() =>
        {
            Assert.That(metadata.AttributeUses.Select(value => value.Name),
                Is.EqualTo(new[] { "id", "note" }));
            Assert.That(record.AttributeUses.Select(value => value.Name),
                Is.EqualTo(new[] { "active", "id", "note" }));
            Assert.That(record.AsnTypeDefinition,
                Does.Contain("id XSDv2.Int"));
            Assert.That(record.AsnTypeDefinition,
                Does.Contain("note XSDv2.String OPTIONAL"));
            Assert.That(record.AsnTypeDefinition,
                Does.Contain("active BOOLEAN DEFAULT true"));
        });
    }

    [Test]
    public void MapsAllGroupsWithOrderAndUseOrder()
    {
        XsdComplexTypeMappingResult mapped = Map(
            "<xs:complexType name=\"Unordered\"><xs:all minOccurs=\"0\">"
                + "<xs:element name=\"first\" type=\"xs:string\"/>"
                + "<xs:element name=\"second\" type=\"xs:int\" "
                + "minOccurs=\"0\"/>"
                + "</xs:all></xs:complexType>");
        XsdComplexTypeDefinition unordered = FindType(mapped, "Unordered");

        Assert.Multiple(() =>
        {
            Assert.That(unordered.Particle?.Kind, Is.EqualTo("all"));
            Assert.That(unordered.Particle?.XerInstructions.Single().Kind,
                Is.EqualTo(AsnInstructionKind.UseOrder));
            Assert.That(unordered.XerInstructions.Single().Kind,
                Is.EqualTo(AsnInstructionKind.UseOrder));
            Assert.That(unordered.AsnTypeDefinition,
                Does.Contain("order SEQUENCE OF ENUMERATED {first, second}"));
            Assert.That(unordered.AsnTypeDefinition,
                Does.Contain("first XSDv2.String"));
            Assert.That(unordered.Particle?.Children[0].Optional, Is.True);
            Assert.That(unordered.AsnTypeDefinition,
                Does.Contain("first XSDv2.String OPTIONAL"));
            Assert.That(unordered.AsnTypeDefinition,
                Does.Contain("second XSDv2.Int OPTIONAL"));
            Assert.That(unordered.AsnTypeDefinition,
                Does.Not.Contain("all SEQUENCE"));
        });
    }

    [Test]
    public void MapsComplexExtensionAndRestrictionDerivations()
    {
        XsdComplexTypeMappingResult mapped = Map(
            "<xs:complexType name=\"Base\"><xs:sequence>"
                + "<xs:element name=\"baseValue\" type=\"xs:string\"/>"
                + "</xs:sequence>"
                + "<xs:attribute name=\"id\" type=\"xs:int\" "
                + "use=\"required\"/>"
                + "</xs:complexType>"
                + "<xs:complexType name=\"Extended\"><xs:complexContent>"
                + "<xs:extension base=\"tns:Base\"><xs:sequence>"
                + "<xs:element name=\"extra\" type=\"xs:boolean\"/>"
                + "</xs:sequence>"
                + "<xs:attribute name=\"code\" type=\"xs:string\"/>"
                + "</xs:extension></xs:complexContent></xs:complexType>"
                + "<xs:complexType name=\"Restricted\"><xs:complexContent>"
                + "<xs:restriction base=\"tns:Base\"><xs:sequence>"
                + "<xs:element name=\"baseValue\" type=\"xs:string\"/>"
                + "</xs:sequence>"
                + "<xs:attribute name=\"id\" use=\"prohibited\"/>"
                + "</xs:restriction></xs:complexContent></xs:complexType>"
                + "<xs:complexType name=\"FromAny\"><xs:complexContent>"
                + "<xs:extension base=\"xs:anyType\">"
                + "<xs:attribute name=\"tag\" type=\"xs:string\"/>"
                + "</xs:extension></xs:complexContent></xs:complexType>");
        XsdComplexTypeDefinition extended = FindType(mapped, "Extended");
        XsdComplexTypeDefinition restricted = FindType(mapped, "Restricted");
        XsdComplexTypeDefinition fromAny = FindType(mapped, "FromAny");

        Assert.Multiple(() =>
        {
            Assert.That(extended.Derivation, Is.EqualTo("extension"));
            Assert.That(extended.BaseComplexType?.Name, Is.EqualTo("Base"));
            Assert.That(extended.AttributeUses.Select(value => value.Name),
                Is.EqualTo(new[] { "code", "id" }));
            Assert.That(extended.AsnTypeDefinition,
                Does.Contain("baseValue XSDv2.String"));
            Assert.That(extended.AsnTypeDefinition,
                Does.Contain("extra BOOLEAN"));
            Assert.That(restricted.Derivation, Is.EqualTo("restriction"));
            Assert.That(restricted.AttributeUses, Is.Empty);
            Assert.That(restricted.AsnTypeDefinition,
                Does.Contain("baseValue XSDv2.String"));
            Assert.That(fromAny.BaseTypeName, Is.EqualTo("anyType"));
            Assert.That(fromAny.AsnTypeDefinition,
                Does.Contain("tag XSDv2.String OPTIONAL"));
        });
    }

    [Test]
    public void MapsSimpleContentExtensionAndRestrictionFacets()
    {
        XsdComplexTypeMappingResult mapped = Map(
            "<xs:simpleType name=\"Code\"><xs:restriction base=\"xs:string\"/>"
                + "</xs:simpleType>"
                + "<xs:complexType name=\"Label\"><xs:simpleContent>"
                + "<xs:extension base=\"tns:Code\">"
                + "<xs:attribute name=\"language\" type=\"xs:string\"/>"
                + "</xs:extension></xs:simpleContent></xs:complexType>"
                + "<xs:complexType name=\"ShortLabel\"><xs:simpleContent>"
                + "<xs:restriction base=\"tns:Code\">"
                + "<xs:minLength value=\"2\"/><xs:maxLength value=\"4\"/>"
                + "</xs:restriction></xs:simpleContent></xs:complexType>");
        XsdComplexTypeDefinition label = FindType(mapped, "Label");
        XsdComplexTypeDefinition shortLabel = FindType(mapped, "ShortLabel");

        Assert.Multiple(() =>
        {
            Assert.That(label.ContentKind, Is.EqualTo("simple"));
            Assert.That(label.AsnTypeDefinition,
                Is.EqualTo("SEQUENCE {language XSDv2.String OPTIONAL, base Code}"));
            Assert.That(shortLabel.SimpleContentType?.Anonymous, Is.True);
            Assert.That(shortLabel.SimpleContentType?.Definition?.Constraints
                .Single().Kind, Is.EqualTo(AsnConstraintKind.Size));
            Assert.That(shortLabel.AsnTypeDefinition,
                Does.Contain("base Code (SIZE(2..4))"));
        });
    }

    [Test]
    public void DiagnosesUnknownGroupsAndDerivationBases()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:complexType name=\"MissingGroup\"><xs:sequence>"
                + "<xs:group ref=\"tns:NoGroup\"/>"
                + "</xs:sequence></xs:complexType>"
                + "<xs:complexType name=\"MissingBase\"><xs:complexContent>"
                + "<xs:extension base=\"tns:NoBase\"/>"
                + "</xs:complexContent></xs:complexType>");

        Assert.Multiple(() =>
        {
            Assert.That(importer.LastComplexTypeResult.Success, Is.False);
            Assert.That(importer.LastComplexTypeResult.Diagnostics.Select(
                value => value.Code), Does.Contain("ASNXSD0028"));
            Assert.That(importer.LastComplexTypeResult.Diagnostics.Select(
                value => value.Code), Does.Contain("ASNXSD0030"));
        });
    }

    private static XsdComplexTypeMappingResult Map(string content)
    {
        XsdSchemaImporterImplementation importer = Import(content);
        Assert.That(importer.LastComplexTypeResult.Success,
            Is.True, Format(importer.LastComplexTypeResult.Diagnostics));
        return importer.LastComplexTypeResult;
    }

    private static XsdSchemaImporterImplementation Import(string content)
    {
        XsdSchemaImporterImplementation importer =
            new XsdSchemaImporterImplementation();
        importer.ImportSchemas(
            [new AsnXsdInput
            {
                Name = "complex-types.xsd",
                HasBaseUri = true,
                BaseUri = "urn:complex-types-document",
                Text = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" "
                    + "xmlns:tns=\"urn:test\" targetNamespace=\"urn:test\">"
                    + content + "</xs:schema>"
            }],
            new AsnXsdImportOptions());
        return importer;
    }

    private static XsdComplexTypeDefinition FindType(
        XsdComplexTypeMappingResult mapped,
        string name)
    {
        return mapped.Types.Single(value => value.Name == name);
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(System.Environment.NewLine, diagnostics.Select(
            value => value.Code + ": " + value.Message));
    }
}
