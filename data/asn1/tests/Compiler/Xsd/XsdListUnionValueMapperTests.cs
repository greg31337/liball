using asn1;

namespace asn1.Tests;

public class XsdListUnionValueMapperTests
{
    [Test]
    public void MapsNamedAndAnonymousListItemTypes()
    {
        XsdSimpleTypeMappingResult mapped = Map(
            "<xs:simpleType name=\"Number\">"
                + "<xs:restriction base=\"xs:int\">"
                + "<xs:minInclusive value=\"1\"/>"
                + "</xs:restriction></xs:simpleType>"
                + "<xs:simpleType name=\"Numbers\">"
                + "<xs:list itemType=\"tns:Number\"/>"
                + "</xs:simpleType>"
                + "<xs:simpleType name=\"Words\">"
                + "<xs:list><xs:simpleType>"
                + "<xs:restriction base=\"xs:string\">"
                + "<xs:minLength value=\"2\"/>"
                + "</xs:restriction>"
                + "</xs:simpleType></xs:list>"
                + "</xs:simpleType>");
        XsdSimpleTypeDefinition numbers = Find(mapped, "Numbers");
        XsdSimpleTypeDefinition words = Find(mapped, "Words");

        Assert.Multiple(() =>
        {
            Assert.That(numbers.Variety, Is.EqualTo(XsdSimpleTypeVariety.List));
            Assert.That(numbers.ItemType?.Definition?.Name,
                Is.EqualTo("Number"));
            Assert.That(numbers.AsnTypeDefinition,
                Is.EqualTo("SEQUENCE OF Number"));
            Assert.That(numbers.XerInstructions.Single().Kind,
                Is.EqualTo(AsnInstructionKind.List));
            Assert.That(words.ItemType?.Anonymous, Is.True);
            Assert.That(words.ItemType?.Definition?.Constraints.Single().Kind,
                Is.EqualTo(AsnConstraintKind.Size));
            Assert.That(words.AsnTypeDefinition,
                Does.Contain("FROM ({0,0,0,33}..{0,16,255,253})"));
        });
    }

    [Test]
    public void MapsUnionMembersInOrderWithRequiredInstructions()
    {
        XsdSimpleTypeMappingResult mapped = Map(
            "<xs:simpleType name=\"Code\">"
                + "<xs:restriction base=\"xs:string\"/>"
                + "</xs:simpleType>"
                + "<xs:simpleType name=\"ChoiceValue\">"
                + "<xs:union memberTypes=\"tns:Code xs:QName\">"
                + "<xs:simpleType><xs:restriction base=\"xs:integer\"/>"
                + "</xs:simpleType>"
                + "<xs:simpleType><xs:restriction base=\"xs:boolean\"/>"
                + "</xs:simpleType>"
                + "</xs:union></xs:simpleType>");
        XsdSimpleTypeDefinition choice = Find(mapped, "ChoiceValue");

        Assert.Multiple(() =>
        {
            Assert.That(choice.Variety,
                Is.EqualTo(XsdSimpleTypeVariety.Union));
            Assert.That(choice.UnionAlternatives.Select(value =>
                value.Identifier),
                Is.EqualTo(new[] { "Code", "QName", "alt", "alt-1" }));
            Assert.That(choice.UnionAlternatives[1].TypeUse?.XerInstructions
                .Single().Kind, Is.EqualTo(AsnInstructionKind.UseQName));
            Assert.That(choice.UnionAlternatives[2].XerInstructions
                .Single().Arguments, Is.EqualTo(new[] { "AS", "" }));
            Assert.That(choice.AsnTypeDefinition,
                Does.Contain("alt [NAME AS \"\"] INTEGER"));
            Assert.That(choice.XerInstructions.Single().Kind,
                Is.EqualTo(AsnInstructionKind.UseUnion));
        });
    }

    [Test]
    public void MapsCanonicalDefaultsAndFixedValueConstraints()
    {
        XsdSimpleTypeMappingResult mapped = Map(
            "<xs:simpleType name=\"Numbers\">"
                + "<xs:list itemType=\"xs:integer\"/>"
                + "</xs:simpleType>"
                + "<xs:element name=\"count\" type=\"xs:integer\" "
                + "default=\"+05\"/>"
                + "<xs:attribute name=\"enabled\" type=\"xs:boolean\" "
                + "fixed=\"1\"/>"
                + "<xs:attribute name=\"numbers\" type=\"tns:Numbers\" "
                + "default=\" 01  2 \"/>");
        XsdValueConstraintDefinition count = Constraint(mapped, "count");
        XsdValueConstraintDefinition enabled = Constraint(mapped, "enabled");
        XsdValueConstraintDefinition numbers = Constraint(mapped, "numbers");

        Assert.Multiple(() =>
        {
            Assert.That(count.CanonicalValue, Is.EqualTo("5"));
            Assert.That(count.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(5)));
            Assert.That(count.DefaultForEmptyInstruction?.Kind,
                Is.EqualTo(AsnInstructionKind.DefaultForEmpty));
            Assert.That(count.FixedConstraint, Is.Null);
            Assert.That(enabled.CanonicalValue, Is.EqualTo("true"));
            Assert.That(enabled.FixedConstraint?.Kind,
                Is.EqualTo(AsnConstraintKind.SingleValue));
            Assert.That(enabled.FixedConstraint?.Elements.Single().Value
                ?.TextValue, Is.EqualTo("true"));
            Assert.That(numbers.CanonicalValue, Is.EqualTo("1 2"));
            Assert.That(numbers.Value?.Kind,
                Is.EqualTo(AsnValueKind.SequenceOf));
            Assert.That(numbers.Value?.Items.Select(value =>
                value.IntegerValue), Is.EqualTo(
                    new System.Numerics.BigInteger[] { 1, 2 }));
            Assert.That(numbers.TypeInstructions.Single().Kind,
                Is.EqualTo(AsnInstructionKind.List));
        });
    }

    [Test]
    public void AppliesQNameInstructionsAndIgnoresElementValueConstraints()
    {
        XsdSimpleTypeMappingResult mapped = Map(
            "<xs:element name=\"qualifiedElement\" type=\"xs:QName\" "
                + "default=\"tns:item\"/>"
                + "<xs:attribute name=\"qualifiedAttribute\" "
                + "type=\"xs:QName\" fixed=\"tns:item\"/>");
        XsdValueConstraintDefinition element = Constraint(
            mapped,
            "qualifiedElement");
        XsdValueConstraintDefinition attribute = Constraint(
            mapped,
            "qualifiedAttribute");

        Assert.Multiple(() =>
        {
            Assert.That(element.IgnoredForQName, Is.True);
            Assert.That(element.Value, Is.Null);
            Assert.That(element.DefaultForEmptyInstruction, Is.Null);
            Assert.That(element.TypeInstructions.Single().Kind,
                Is.EqualTo(AsnInstructionKind.UseQName));
            Assert.That(attribute.IgnoredForQName, Is.False);
            Assert.That(attribute.CanonicalValue,
                Is.EqualTo("{urn:test}item"));
            Assert.That(attribute.Value?.NamedValues.Select(value =>
                value.Name), Is.EqualTo(new[] { "uri", "name" }));
            Assert.That(attribute.FixedConstraint, Is.Not.Null);
            Assert.That(attribute.TypeInstructions.Single().Kind,
                Is.EqualTo(AsnInstructionKind.UseQName));
        });
    }

    [Test]
    public void ParsesUnionValuesUsingTheFirstMatchingMember()
    {
        XsdSimpleTypeMappingResult mapped = Map(
            "<xs:simpleType name=\"Numberish\">"
                + "<xs:union memberTypes=\"xs:integer xs:string\"/>"
                + "</xs:simpleType>"
                + "<xs:attribute name=\"choice\" type=\"tns:Numberish\" "
                + "fixed=\"+7\"/>");
        XsdValueConstraintDefinition choice = Constraint(mapped, "choice");

        Assert.Multiple(() =>
        {
            Assert.That(choice.CanonicalValue, Is.EqualTo("7"));
            Assert.That(choice.Value?.Kind, Is.EqualTo(AsnValueKind.Choice));
            Assert.That(choice.Value?.NamedValues.Single().Name,
                Is.EqualTo("integer"));
            Assert.That(choice.Value?.NamedValues.Single().Value
                ?.IntegerValue, Is.EqualTo(new System.Numerics.BigInteger(7)));
            Assert.That(choice.TypeInstructions.Single().Kind,
                Is.EqualTo(AsnInstructionKind.UseUnion));
        });
    }

    [Test]
    public void DiagnosesInvalidListUnionAndValueConstraints()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:simpleType name=\"InnerList\">"
                + "<xs:list itemType=\"xs:string\"/>"
                + "</xs:simpleType>"
                + "<xs:simpleType name=\"OuterList\">"
                + "<xs:list itemType=\"tns:InnerList\"/>"
                + "</xs:simpleType>"
                + "<xs:simpleType name=\"BadUnion\">"
                + "<xs:union memberTypes=\"tns:Missing\"/>"
                + "</xs:simpleType>"
                + "<xs:element name=\"badValue\" type=\"xs:integer\" "
                + "default=\"not-an-integer\"/>");

        Assert.Multiple(() =>
        {
            Assert.That(importer.LastSimpleTypeResult.Success, Is.False);
            Assert.That(importer.LastSimpleTypeResult.Diagnostics.Select(
                value => value.Code), Does.Contain("ASNXSD0020"));
            Assert.That(importer.LastSimpleTypeResult.Diagnostics.Select(
                value => value.Code), Does.Contain("ASNXSD0021"));
            Assert.That(importer.LastSimpleTypeResult.Diagnostics.Select(
                value => value.Code), Does.Contain("ASNXSD0022"));
        });
    }

    private static XsdSimpleTypeMappingResult Map(string content)
    {
        XsdSchemaImporterImplementation importer = Import(content);
        Assert.That(importer.LastSimpleTypeResult.Success,
            Is.True, Format(importer.LastSimpleTypeResult.Diagnostics));
        return importer.LastSimpleTypeResult;
    }

    private static XsdSchemaImporterImplementation Import(string content)
    {
        XsdSchemaImporterImplementation importer =
            new XsdSchemaImporterImplementation();
        importer.ImportSchemas(
            [new AsnXsdInput
            {
                Name = "lists-and-unions.xsd",
                HasBaseUri = true,
                BaseUri = "urn:lists-and-unions-document",
                Text = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" "
                    + "xmlns:tns=\"urn:test\" targetNamespace=\"urn:test\">"
                    + content + "</xs:schema>"
            }],
            new AsnXsdImportOptions());
        return importer;
    }

    private static XsdSimpleTypeDefinition Find(
        XsdSimpleTypeMappingResult mapped,
        string name)
    {
        return mapped.Types.Single(value => value.Name == name);
    }

    private static XsdValueConstraintDefinition Constraint(
        XsdSimpleTypeMappingResult mapped,
        string componentName)
    {
        return mapped.ValueConstraints.Single(value =>
            value.ComponentName == componentName);
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(System.Environment.NewLine, diagnostics.Select(
            value => value.Code + ": " + value.Message));
    }
}
