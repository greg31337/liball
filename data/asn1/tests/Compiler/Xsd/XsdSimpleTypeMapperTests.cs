using asn1;

namespace asn1.Tests;

public class XsdSimpleTypeMapperTests
{
    [Test]
    public void MapsLengthPatternWhitespaceAndDigitFacets()
    {
        XsdSimpleTypeMappingResult mapped = Map(
            "<xs:simpleType name=\"Code\">"
                + "<xs:restriction base=\"xs:string\">"
                + "<xs:minLength value=\"2\"/>"
                + "<xs:maxLength value=\"8\"/>"
                + "<xs:pattern value=\"[A-Z]+\"/>"
                + "<xs:whiteSpace value=\"collapse\"/>"
                + "</xs:restriction></xs:simpleType>"
                + "<xs:simpleType name=\"Amount\">"
                + "<xs:restriction base=\"xs:decimal\">"
                + "<xs:totalDigits value=\"5\"/>"
                + "<xs:fractionDigits value=\"2\"/>"
                + "</xs:restriction></xs:simpleType>");
        XsdSimpleTypeDefinition code = Find(mapped, "Code");
        XsdSimpleTypeDefinition amount = Find(mapped, "Amount");

        Assert.Multiple(() =>
        {
            Assert.That(mapped.Success, Is.True, Format(mapped.Diagnostics));
            Assert.That(code.AsnTypeDefinition,
                Does.Contain("(SIZE(2..8))"));
            Assert.That(code.AsnTypeDefinition,
                Does.Contain("XML representation of the XSD pattern"));
            Assert.That(code.Constraints.Select(value => value.Kind),
                Does.Contain(AsnConstraintKind.PermittedAlphabet));
            Assert.That(code.Constraints.Select(value => value.Kind),
                Does.Contain(AsnConstraintKind.Pattern));
            Assert.That(code.XerInstructions.Single().Kind,
                Is.EqualTo(AsnInstructionKind.Whitespace));
            Assert.That(code.XerInstructions.Single().Arguments,
                Is.EqualTo(new[] { "COLLAPSE" }));
            Assert.That(amount.Constraints.Count(value =>
                value.Kind == AsnConstraintKind.UserDefined),
                Is.EqualTo(2));
            Assert.That(amount.Constraints.All(value =>
                value.RequiresApplicationValidation), Is.True);
        });
    }

    [Test]
    public void MapsStringEnumerationInFilteredLexicalOrder()
    {
        XsdSimpleTypeMappingResult mapped = Map(
            "<xs:simpleType name=\"Color\">"
                + "<xs:restriction base=\"xs:string\">"
                + "<xs:minLength value=\"2\"/>"
                + "<xs:maxLength value=\"4\"/>"
                + "<xs:whiteSpace value=\"collapse\"/>"
                + "<xs:enumeration value=\" white \"/>"
                + "<xs:enumeration value=\"red\"/>"
                + "<xs:enumeration value=\"blue\"/>"
                + "<xs:enumeration value=\" blue \"/>"
                + "</xs:restriction></xs:simpleType>");
        XsdSimpleTypeDefinition color = Find(mapped, "Color");

        Assert.Multiple(() =>
        {
            Assert.That(mapped.Success, Is.True, Format(mapped.Diagnostics));
            Assert.That(color.Enumerated, Is.True);
            Assert.That(color.EnumerationItems.Select(value =>
                value.CanonicalValue), Is.EqualTo(new[] { "blue", "red" }));
            Assert.That(color.EnumerationItems.Select(value =>
                value.Identifier), Is.EqualTo(new[] { "blue", "red" }));
            Assert.That(color.AsnTypeDefinition,
                Is.EqualTo("ENUMERATED {blue, red}"));
        });
    }

    [Test]
    public void MapsIntegerEnumerationByValueSpaceAndBounds()
    {
        XsdSimpleTypeMappingResult mapped = Map(
            "<xs:simpleType name=\"Number\">"
                + "<xs:restriction base=\"xs:integer\">"
                + "<xs:minInclusive value=\"0\"/>"
                + "<xs:maxExclusive value=\"11\"/>"
                + "<xs:totalDigits value=\"2\"/>"
                + "<xs:enumeration value=\"10\"/>"
                + "<xs:enumeration value=\"+05\"/>"
                + "<xs:enumeration value=\"5\"/>"
                + "<xs:enumeration value=\"12\"/>"
                + "</xs:restriction></xs:simpleType>");
        XsdSimpleTypeDefinition number = Find(mapped, "Number");

        Assert.Multiple(() =>
        {
            Assert.That(mapped.Success, Is.True, Format(mapped.Diagnostics));
            Assert.That(number.EnumerationItems.Select(value =>
                value.CanonicalValue), Is.EqualTo(new[] { "5", "10" }));
            Assert.That(number.EnumerationItems.Select(value =>
                value.Value?.IntegerValue),
                Is.EqualTo(new System.Numerics.BigInteger[] { 5, 10 }));
            Assert.That(number.AsnTypeDefinition,
                Is.EqualTo("ENUMERATED {int5(5), int10(10)}"));
            Assert.That(number.XerInstructions.Single().Kind,
                Is.EqualTo(AsnInstructionKind.UseNumber));
        });
    }

    [Test]
    public void MapsOtherEnumerationsAndQNameValues()
    {
        XsdSimpleTypeMappingResult mapped = Map(
            "<xs:simpleType name=\"Flag\">"
                + "<xs:restriction base=\"xs:boolean\">"
                + "<xs:enumeration value=\"1\"/>"
                + "<xs:enumeration value=\"true\"/>"
                + "</xs:restriction></xs:simpleType>"
                + "<xs:simpleType name=\"Binary\">"
                + "<xs:restriction base=\"xs:hexBinary\">"
                + "<xs:enumeration value=\"0aFF\"/>"
                + "<xs:enumeration value=\"0AFF\"/>"
                + "</xs:restriction></xs:simpleType>"
                + "<xs:simpleType name=\"Qualified\">"
                + "<xs:restriction base=\"xs:QName\">"
                + "<xs:enumeration value=\"tns:item\"/>"
                + "</xs:restriction></xs:simpleType>");
        XsdSimpleTypeDefinition flag = Find(mapped, "Flag");
        XsdSimpleTypeDefinition binary = Find(mapped, "Binary");
        XsdSimpleTypeDefinition qualified = Find(mapped, "Qualified");

        Assert.Multiple(() =>
        {
            Assert.That(mapped.Success, Is.True, Format(mapped.Diagnostics));
            Assert.That(flag.EnumerationItems, Has.Length.EqualTo(1));
            Assert.That(flag.EnumerationItems[0].CanonicalValue,
                Is.EqualTo("true"));
            Assert.That(flag.Constraints.Single().Kind,
                Is.EqualTo(AsnConstraintKind.SingleValue));
            Assert.That(binary.EnumerationItems, Has.Length.EqualTo(1));
            Assert.That(binary.EnumerationItems[0].Value?.Octets,
                Is.EqualTo(new byte[] { 10, 255 }));
            Assert.That(qualified.EnumerationItems[0].CanonicalValue,
                Is.EqualTo("{urn:test}item"));
            Assert.That(qualified.EnumerationItems[0].Value?.NamedValues
                .Select(value => value.Name),
                Is.EqualTo(new[] { "uri", "name" }));
        });
    }

    [Test]
    public void MapsExactSizeOpenRangesDateBoundsAndEnumerationPatterns()
    {
        XsdSimpleTypeMappingResult mapped = Map(
            "<xs:simpleType name=\"Sized\">"
                + "<xs:restriction base=\"xs:hexBinary\">"
                + "<xs:length value=\"2\"/>"
                + "</xs:restriction></xs:simpleType>"
                + "<xs:simpleType name=\"Window\">"
                + "<xs:restriction base=\"xs:decimal\">"
                + "<xs:minExclusive value=\"1.0\"/>"
                + "<xs:maxExclusive value=\"10.00\"/>"
                + "</xs:restriction></xs:simpleType>"
                + "<xs:simpleType name=\"Moment\">"
                + "<xs:restriction base=\"xs:dateTime\">"
                + "<xs:minInclusive value=\"2020-01-01T00:00:00Z\"/>"
                + "</xs:restriction></xs:simpleType>"
                + "<xs:simpleType name=\"Upper\">"
                + "<xs:restriction base=\"xs:string\">"
                + "<xs:pattern value=\"[A-Z]+\"/>"
                + "<xs:enumeration value=\"AA\"/>"
                + "<xs:enumeration value=\"bb\"/>"
                + "</xs:restriction></xs:simpleType>");
        XsdSimpleTypeDefinition sized = Find(mapped, "Sized");
        XsdSimpleTypeDefinition window = Find(mapped, "Window");
        XsdSimpleTypeDefinition moment = Find(mapped, "Moment");
        XsdSimpleTypeDefinition upper = Find(mapped, "Upper");
        AsnConstraintElement range = window.Constraints.Single(value =>
            value.Kind == AsnConstraintKind.ValueRange).Elements.Single();

        Assert.Multiple(() =>
        {
            Assert.That(mapped.Success, Is.True, Format(mapped.Diagnostics));
            Assert.That(sized.AsnTypeDefinition,
                Is.EqualTo("OCTET STRING (SIZE(2))"));
            Assert.That(range.LowerEndpointIncluded, Is.False);
            Assert.That(range.UpperEndpointIncluded, Is.False);
            Assert.That(range.LowerValue?.RealValue?.DecimalCoefficient,
                Is.EqualTo(new System.Numerics.BigInteger(1)));
            Assert.That(range.UpperValue?.RealValue?.DecimalCoefficient,
                Is.EqualTo(new System.Numerics.BigInteger(1)));
            Assert.That(range.UpperValue?.RealValue?.DecimalExponent,
                Is.EqualTo(new System.Numerics.BigInteger(1)));
            Assert.That(moment.Constraints.Single().Kind,
                Is.EqualTo(AsnConstraintKind.UserDefined));
            Assert.That(moment.Constraints.Single()
                .RequiresApplicationValidation, Is.True);
            Assert.That(upper.EnumerationItems.Select(value =>
                value.CanonicalValue), Is.EqualTo(new[] { "AA" }));
            Assert.That(upper.XerInstructions.Single().Kind,
                Is.EqualTo(AsnInstructionKind.Text));
        });
    }

    [Test]
    public void ResolvesNamedRestrictionChainsAndInheritedFacets()
    {
        XsdSimpleTypeMappingResult mapped = Map(
            "<xs:simpleType name=\"BaseNumber\">"
                + "<xs:restriction base=\"xs:integer\">"
                + "<xs:minInclusive value=\"1\"/>"
                + "</xs:restriction></xs:simpleType>"
                + "<xs:simpleType name=\"SmallNumber\">"
                + "<xs:restriction base=\"tns:BaseNumber\">"
                + "<xs:maxInclusive value=\"9\"/>"
                + "<xs:enumeration value=\"0\"/>"
                + "<xs:enumeration value=\"5\"/>"
                + "</xs:restriction></xs:simpleType>");
        XsdSimpleTypeDefinition small = Find(mapped, "SmallNumber");

        Assert.Multiple(() =>
        {
            Assert.That(mapped.Success, Is.True, Format(mapped.Diagnostics));
            Assert.That(small.BaseTypeName, Is.EqualTo("BaseNumber"));
            Assert.That(small.BuiltInBaseName, Is.EqualTo("integer"));
            Assert.That(small.EnumerationItems.Select(value =>
                value.CanonicalValue), Is.EqualTo(new[] { "5" }));
            Assert.That(small.EffectiveFacets.Select(value => value.Name),
                Does.Contain("minInclusive"));
        });
    }

    [Test]
    public void DiagnosesUnknownBasesInvalidFacetsAndContradictions()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:simpleType name=\"BadBase\">"
                + "<xs:restriction base=\"tns:Missing\"/>"
                + "</xs:simpleType>"
                + "<xs:simpleType name=\"BadFacets\">"
                + "<xs:restriction base=\"xs:string\">"
                + "<xs:minLength value=\"8\"/>"
                + "<xs:maxLength value=\"2\"/>"
                + "<xs:whiteSpace value=\"invalid\"/>"
                + "</xs:restriction></xs:simpleType>");

        Assert.Multiple(() =>
        {
            Assert.That(importer.LastSimpleTypeResult.Success, Is.False);
            Assert.That(importer.LastSimpleTypeResult.Diagnostics.Select(
                value => value.Code), Does.Contain("ASNXSD0017"));
            Assert.That(importer.LastSimpleTypeResult.Diagnostics.Select(
                value => value.Code), Does.Contain("ASNXSD0018"));
            Assert.That(importer.LastSimpleTypeResult.Diagnostics.Select(
                value => value.Code), Does.Contain("ASNXSD0019"));
            Assert.That(importer.LastSimpleTypeResult.Diagnostics.All(value =>
                value.Span?.Start.SourceName == "types.xsd"), Is.True);
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
                Name = "types.xsd",
                HasBaseUri = true,
                BaseUri = "urn:types-document",
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

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(System.Environment.NewLine, diagnostics.Select(
            value => value.Code + ": " + value.Message));
    }
}
