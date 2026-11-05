using asn1;

namespace asn1.Tests;

public class XsdAdvancedMapperTests
{
    [Test]
    public void MapsMixedContentToEmbedValues()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:complexType name=\"Message\" mixed=\"true\">"
                + "<xs:sequence><xs:element name=\"code\" "
                + "type=\"xs:int\"/></xs:sequence>"
                + "</xs:complexType>");
        XsdComplexTypeDefinition message = ComplexType(importer, "Message");

        Assert.Multiple(() =>
        {
            Assert.That(importer.LastAdvancedMappingResult.Success,
                Is.True, Format(importer.LastAdvancedMappingResult.Diagnostics));
            Assert.That(message.Mixed, Is.True);
            Assert.That(message.AsnTypeDefinition,
                Does.StartWith("SEQUENCE {embed-values SEQUENCE OF XSDv2.String"));
            Assert.That(message.AsnTypeDefinition,
                Does.Contain("code XSDv2.Int"));
            Assert.That(message.XerInstructions.Select(value => value.Kind),
                Does.Contain(AsnInstructionKind.EmbedValues));
        });
    }

    [Test]
    public void MapsElementWildcardModesAndNamespaceConstraints()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:element name=\"alpha\" type=\"xs:string\"/>"
                + "<xs:element name=\"hidden\" type=\"xs:string\" "
                + "abstract=\"true\"/>"
                + "<xs:complexType name=\"Envelope\"><xs:sequence>"
                + "<xs:any namespace=\"##targetNamespace\" "
                + "processContents=\"strict\"/>"
                + "<xs:any processContents=\"lax\">"
                + "<xs:annotation a:wildcard-mapping=\"UTF-8\"/>"
                + "</xs:any>"
                + "<xs:any processContents=\"skip\"/>"
                + "</xs:sequence></xs:complexType>");
        XsdWildcardDefinition[] wildcards = importer.LastAdvancedMappingResult
            .Wildcards.Where(value => value.Kind == "element").ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(wildcards, Has.Length.EqualTo(3));
            Assert.That(wildcards[0].MappingMode, Is.EqualTo("CHOICE-FI"));
            Assert.That(wildcards[0].NamespaceTokens,
                Is.EqualTo(new[] { "##targetNamespace" }));
            Assert.That(wildcards[0].AllowedElements.Select(value => value.Name),
                Is.EqualTo(new[] { "alpha" }));
            Assert.That(wildcards[0].IncludesFallback, Is.True);
            Assert.That(wildcards[0].AsnTypeDefinition,
                Does.Contain("elem OCTET STRING"));
            Assert.That(wildcards[0].XerInstructions.Select(value => value.Kind),
                Is.EqualTo(new[]
                {
                    AsnInstructionKind.AnyElement,
                    AsnInstructionKind.Untagged
                }));
            Assert.That(wildcards[1].MappingMode, Is.EqualTo("UTF-8"));
            Assert.That(wildcards[1].AsnTypeDefinition,
                Is.EqualTo("UTF8String"));
            Assert.That(wildcards[2].MappingMode, Is.EqualTo("FI"));
            Assert.That(wildcards[2].AsnTypeDefinition,
                Is.EqualTo("OCTET STRING"));
        });
    }

    [Test]
    public void MapsAnyAttributeFromAttributeGroup()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:attributeGroup name=\"WildcardAttributes\">"
                + "<xs:anyAttribute namespace=\"##other\" "
                + "processContents=\"lax\"/>"
                + "</xs:attributeGroup>"
                + "<xs:attributeGroup name=\"OpenAttributes\">"
                + "<xs:attributeGroup ref=\"tns:WildcardAttributes\"/>"
                + "</xs:attributeGroup>"
                + "<xs:complexType name=\"Record\">"
                + "<xs:attributeGroup ref=\"tns:OpenAttributes\"/>"
                + "</xs:complexType>");
        XsdComplexTypeDefinition record = ComplexType(importer, "Record");
        XsdWildcardDefinition wildcard = record.AttributeWildcard
            ?? new XsdWildcardDefinition();

        Assert.Multiple(() =>
        {
            Assert.That(wildcard.Kind, Is.EqualTo("attribute"));
            Assert.That(wildcard.ProcessContents, Is.EqualTo("lax"));
            Assert.That(wildcard.NamespaceConstraint, Is.EqualTo("##other"));
            Assert.That(wildcard.AsnTypeDefinition,
                Is.EqualTo("SEQUENCE OF XSDv2.String"));
            Assert.That(wildcard.Constraints.Single().Kind,
                Is.EqualTo(AsnConstraintKind.UserDefined));
            Assert.That(wildcard.XerInstructions.Single().Kind,
                Is.EqualTo(AsnInstructionKind.AnyAttributes));
            Assert.That(record.AsnTypeDefinition,
                Does.Contain("attr SEQUENCE OF XSDv2.String"));
        });
    }

    [Test]
    public void MapsWildcardsInsideReferencedModelGroups()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:group name=\"OpenContent\"><xs:sequence>"
                + "<xs:any processContents=\"skip\"/>"
                + "</xs:sequence></xs:group>"
                + "<xs:complexType name=\"UsesGroup\"><xs:sequence>"
                + "<xs:group ref=\"tns:OpenContent\"/>"
                + "</xs:sequence></xs:complexType>");
        XsdModelGroupDefinition group = importer.LastComplexTypeResult
            .ModelGroups.Single(value => value.Name == "OpenContent");
        XsdParticleDefinition wildcardParticle = group.Particle?.Children.Single()
            ?? new XsdParticleDefinition();

        Assert.Multiple(() =>
        {
            Assert.That(wildcardParticle.PendingWildcardMapping, Is.False);
            Assert.That(wildcardParticle.Wildcard?.MappingMode,
                Is.EqualTo("FI"));
            Assert.That(group.AsnTypeDefinition,
                Does.Contain("elem OCTET STRING"));
            Assert.That(ComplexType(importer, "UsesGroup").AsnTypeDefinition,
                Does.Contain("OpenContent"));
        });
    }

    [Test]
    public void MapsTransitiveSubstitutionGroupsAndElementReferences()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:element name=\"Head\" type=\"xs:string\" "
                + "abstract=\"true\"/>"
                + "<xs:element name=\"Alpha\" type=\"xs:string\" "
                + "substitutionGroup=\"tns:Head\"/>"
                + "<xs:element name=\"Beta\" type=\"xs:string\" "
                + "substitutionGroup=\"tns:Alpha\"/>"
                + "<xs:element name=\"BlockedHead\" type=\"xs:string\" "
                + "block=\"substitution\"/>"
                + "<xs:element name=\"BlockedMember\" type=\"xs:string\" "
                + "substitutionGroup=\"tns:BlockedHead\"/>"
                + "<xs:complexType name=\"Holder\"><xs:sequence>"
                + "<xs:element ref=\"tns:Head\"/>"
                + "</xs:sequence></xs:complexType>");
        XsdSubstitutionGroupDefinition group = importer
            .LastAdvancedMappingResult.SubstitutionGroups.Single(value =>
                value.HeadName == "Head");
        XsdComplexTypeDefinition holder = ComplexType(importer, "Holder");
        XsdParticleDefinition particle = holder.Particle?.Children.Single()
            ?? new XsdParticleDefinition();

        Assert.Multiple(() =>
        {
            Assert.That(group.Members.Select(value => value.Name),
                Is.EqualTo(new[] { "Alpha", "Beta" }));
            Assert.That(importer.LastAdvancedMappingResult.SubstitutionGroups
                .Select(value => value.HeadName),
                Does.Not.Contain("BlockedHead"));
            Assert.That(group.AsnTypeDefinition,
                Does.StartWith("CHOICE {alpha "));
            Assert.That(group.AsnTypeDefinition, Does.Contain("beta "));
            Assert.That(group.XerInstructions.Single().Kind,
                Is.EqualTo(AsnInstructionKind.Untagged));
            Assert.That(particle.Declaration?.SubstitutionGroup,
                Is.SameAs(group));
            Assert.That(particle.NamedTypeDefinition,
                Does.Contain(group.AsnTypeDefinition));
            Assert.That(holder.AsnTypeDefinition,
                Does.Contain(group.AsnTypeDefinition));
        });
    }

    [Test]
    public void MapsXsiTypeToDerivedTypeChoice()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:complexType name=\"Base\"><xs:sequence>"
                + "<xs:element name=\"value\" type=\"xs:string\"/>"
                + "</xs:sequence></xs:complexType>"
                + "<xs:complexType name=\"Derived\"><xs:complexContent>"
                + "<xs:extension base=\"tns:Base\"><xs:attribute "
                + "name=\"flag\" type=\"xs:boolean\"/>"
                + "</xs:extension></xs:complexContent></xs:complexType>"
                + "<xs:element name=\"item\" type=\"tns:Base\"/>");
        XsdDeclarationDefinition item = GlobalElement(importer, "item");
        XsdTypeSubstitutionDefinition substitution = item.TypeSubstitution
            ?? new XsdTypeSubstitutionDefinition();

        Assert.Multiple(() =>
        {
            Assert.That(substitution.Alternatives.Select(value => value.Name),
                Is.EqualTo(new[] { "Base", "Derived" }));
            Assert.That(substitution.AsnTypeDefinition,
                Does.StartWith("CHOICE {base Base, derived Derived}"));
            Assert.That(substitution.XerInstructions.Single().Kind,
                Is.EqualTo(AsnInstructionKind.UseType));
            Assert.That(item.AsnTypeDefinition,
                Is.EqualTo(substitution.AsnTypeDefinition));
            Assert.That(item.XerInstructions.Select(value => value.Kind),
                Does.Contain(AsnInstructionKind.UseType));
        });
    }

    [Test]
    public void MapsLocalAbstractTypeUseAndHonorsElementBlock()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:complexType name=\"Base\" abstract=\"true\"/>"
                + "<xs:complexType name=\"Concrete\"><xs:complexContent>"
                + "<xs:extension base=\"tns:Base\"/>"
                + "</xs:complexContent></xs:complexType>"
                + "<xs:complexType name=\"Holder\"><xs:sequence>"
                + "<xs:element name=\"item\" type=\"tns:Base\"/>"
                + "</xs:sequence></xs:complexType>"
                + "<xs:element name=\"blocked\" type=\"tns:Base\" "
                + "block=\"extension\"/>");
        XsdDeclarationDefinition item = importer.LastDeclarationResult.Elements
            .Single(value => !value.Global && value.Name == "item");
        XsdDeclarationDefinition blocked = GlobalElement(importer, "blocked");
        XsdComplexTypeDefinition holder = ComplexType(importer, "Holder");

        Assert.Multiple(() =>
        {
            Assert.That(item.TypeSubstitution?.Alternatives.Select(
                value => value.Name), Is.EqualTo(new[] { "Concrete" }));
            Assert.That(item.XerInstructions.Select(value => value.Kind),
                Does.Contain(AsnInstructionKind.UseType));
            Assert.That(holder.AsnTypeDefinition,
                Does.Contain(item.TypeSubstitution?.AsnTypeDefinition));
            Assert.That(blocked.TypeSubstitution, Is.Null);
            Assert.That(importer.LastAdvancedMappingResult.Diagnostics.Select(
                value => value.Code), Does.Contain("ASNXSD0033"));
        });
    }

    [Test]
    public void MapsNillableComplexContentWithoutHidingAttributes()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:complexType name=\"Payload\"><xs:sequence>"
                + "<xs:element name=\"value\" type=\"xs:string\"/>"
                + "</xs:sequence><xs:attribute name=\"id\" "
                + "type=\"xs:int\" use=\"required\"/>"
                + "</xs:complexType>"
                + "<xs:element name=\"payload\" type=\"tns:Payload\" "
                + "nillable=\"true\"/>");
        XsdDeclarationDefinition payload = GlobalElement(importer, "payload");

        Assert.Multiple(() =>
        {
            Assert.That(payload.AsnTypeDefinition,
                Is.EqualTo("SEQUENCE {id XSDv2.Int, content SEQUENCE {value XSDv2.String} OPTIONAL}"));
            Assert.That(payload.XerInstructions.Select(value => value.Kind),
                Does.Contain(AsnInstructionKind.UseNil));
        });
    }

    [Test]
    public void DiagnosesInvalidWildcardAndSubstitutionHead()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:element name=\"orphan\" type=\"xs:string\" "
                + "substitutionGroup=\"tns:Missing\"/>"
                + "<xs:complexType name=\"Bad\"><xs:sequence>"
                + "<xs:any processContents=\"skip\">"
                + "<xs:annotation a:wildcard-mapping=\"CHOICE-FI\"/>"
                + "</xs:any></xs:sequence></xs:complexType>");
        string[] codes = importer.LastAdvancedMappingResult.Diagnostics
            .Select(value => value.Code).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(importer.LastAdvancedMappingResult.Success, Is.False);
            Assert.That(codes, Does.Contain("ASNXSD0031"));
            Assert.That(codes, Does.Contain("ASNXSD0032"));
        });
    }

    private static XsdSchemaImporterImplementation Import(string content)
    {
        XsdSchemaImporterImplementation importer =
            new XsdSchemaImporterImplementation();
        importer.ImportSchemas(
            [new AsnXsdInput
            {
                Name = "advanced.xsd",
                HasBaseUri = true,
                BaseUri = "urn:advanced-document",
                Text = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" "
                    + "xmlns:tns=\"urn:test\" targetNamespace=\"urn:test\" "
                    + "xmlns:a=\"urn:oid:2.1.5.2.0.1\">"
                    + content + "</xs:schema>"
            }],
            new AsnXsdImportOptions());
        return importer;
    }

    private static XsdComplexTypeDefinition ComplexType(
        XsdSchemaImporterImplementation importer,
        string name)
    {
        return importer.LastComplexTypeResult.Types.Single(value =>
            value.Name == name);
    }

    private static XsdDeclarationDefinition GlobalElement(
        XsdSchemaImporterImplementation importer,
        string name)
    {
        return importer.LastDeclarationResult.Elements.Single(value =>
            value.Global && value.Name == name);
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(System.Environment.NewLine, diagnostics.Select(
            value => value.Code + ": " + value.Message));
    }
}
