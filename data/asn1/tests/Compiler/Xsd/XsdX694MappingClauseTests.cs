using asn1;

namespace asn1.Tests;

public class XsdX694MappingClauseTests
{
    [Test]
    public void MapsEveryX694BuiltInThroughAnXsdDocument()
    {
        XsdBuiltInTypeDefinition[] builtIns =
            XsdBuiltInTypeCatalog.GetTypes();
        System.Text.StringBuilder declarations =
            new System.Text.StringBuilder();
        int index = 0;
        while (index < builtIns.Length)
        {
            declarations.Append("<xs:element name=\"value-");
            declarations.Append(index);
            declarations.Append("\" type=\"xs:");
            declarations.Append(builtIns[index].XsdName);
            declarations.Append("\"/>");
            index = index + 1;
        }

        XsdSchemaImporterImplementation importer = Import(
            [Input(
                "built-ins.xsd",
                "https://x694.test/built-ins.xsd",
                Schema("urn:built-ins", declarations.ToString()))]);

        Assert.That(importer.LastDeclarationResult.Elements,
            Has.Length.EqualTo(builtIns.Length));
        Assert.Multiple(() =>
        {
            Assert.That(importer.LastDeclarationResult.Success,
                Is.True, Format(importer.LastDeclarationResult.Diagnostics));
            index = 0;
            while (index < builtIns.Length)
            {
                XsdDeclarationDefinition declaration = importer
                    .LastDeclarationResult.Elements.Single(value =>
                        value.Name == "value-" + index);
                Assert.That(declaration.TypeUse?.BuiltIn,
                    Is.True, builtIns[index].XsdName);
                Assert.That(declaration.TypeUse?.Name,
                    Is.EqualTo(builtIns[index].XsdName),
                    builtIns[index].XsdName);
                Assert.That(declaration.TypeUse?.AsnTypeDefinition,
                    Is.EqualTo(builtIns[index].AsnTypeDefinition),
                    builtIns[index].XsdName);
                index = index + 1;
            }
        });
    }

    [Test]
    public void MapsFacetsListsUnionsParticlesDerivationsAndDeclarations()
    {
        XsdSchemaImporterImplementation importer = Import(
            [Input(
                "mapping-families.xsd",
                "https://x694.test/mapping-families.xsd",
                Schema(
                    "urn:mapping-families",
                    "<xs:simpleType name=\"Code\"><xs:restriction "
                        + "base=\"xs:string\"><xs:minLength value=\"2\"/>"
                        + "<xs:maxLength value=\"8\"/>"
                        + "<xs:pattern value=\"[A-Z]+\"/>"
                        + "<xs:whiteSpace value=\"collapse\"/>"
                        + "<xs:enumeration value=\"AA\"/>"
                        + "</xs:restriction></xs:simpleType>"
                        + "<xs:simpleType name=\"Codes\"><xs:list "
                        + "itemType=\"tns:Code\"/></xs:simpleType>"
                        + "<xs:simpleType name=\"Scalar\"><xs:union "
                        + "memberTypes=\"xs:int xs:boolean\"/>"
                        + "</xs:simpleType>"
                        + "<xs:group name=\"PayloadGroup\"><xs:choice>"
                        + "<xs:element name=\"code\" type=\"tns:Code\"/>"
                        + "<xs:element name=\"count\" type=\"xs:int\"/>"
                        + "</xs:choice></xs:group>"
                        + "<xs:attributeGroup name=\"Metadata\">"
                        + "<xs:attribute name=\"id\" type=\"xs:ID\" "
                        + "use=\"required\"/><xs:anyAttribute "
                        + "namespace=\"##other\" processContents=\"lax\"/>"
                        + "</xs:attributeGroup>"
                        + "<xs:complexType name=\"Base\" mixed=\"true\">"
                        + "<xs:sequence><xs:group ref=\"tns:PayloadGroup\" "
                        + "minOccurs=\"0\" maxOccurs=\"2\"/>"
                        + "<xs:any namespace=\"##other\" "
                        + "processContents=\"skip\"/></xs:sequence>"
                        + "<xs:attributeGroup ref=\"tns:Metadata\"/>"
                        + "</xs:complexType>"
                        + "<xs:complexType name=\"Derived\">"
                        + "<xs:complexContent><xs:extension "
                        + "base=\"tns:Base\"><xs:sequence>"
                        + "<xs:element name=\"extra\" type=\"tns:Scalar\" "
                        + "minOccurs=\"0\"/></xs:sequence>"
                        + "</xs:extension></xs:complexContent>"
                        + "</xs:complexType>"
                        + "<xs:complexType name=\"Restricted\">"
                        + "<xs:complexContent><xs:restriction "
                        + "base=\"tns:Base\"><xs:sequence>"
                        + "<xs:element name=\"code\" type=\"tns:Code\"/>"
                        + "</xs:sequence></xs:restriction>"
                        + "</xs:complexContent></xs:complexType>"
                        + "<xs:complexType name=\"Unordered\"><xs:all>"
                        + "<xs:element name=\"left\" type=\"xs:string\" "
                        + "minOccurs=\"0\"/>"
                        + "<xs:element name=\"right\" type=\"xs:int\"/>"
                        + "</xs:all></xs:complexType>"
                        + "<xs:element name=\"Head\" type=\"tns:Base\" "
                        + "abstract=\"true\"/>"
                        + "<xs:element name=\"Member\" "
                        + "type=\"tns:Derived\" "
                        + "substitutionGroup=\"tns:Head\"/>"
                        + "<xs:element name=\"item\" type=\"tns:Base\" "
                        + "nillable=\"true\"/>"))]);
        XsdSimpleTypeDefinition code = SimpleType(importer, "Code");
        XsdSimpleTypeDefinition codes = SimpleType(importer, "Codes");
        XsdSimpleTypeDefinition scalar = SimpleType(importer, "Scalar");
        XsdComplexTypeDefinition baseType = ComplexType(importer, "Base");
        XsdComplexTypeDefinition derived = ComplexType(importer, "Derived");
        XsdComplexTypeDefinition restricted = ComplexType(
            importer,
            "Restricted");
        XsdComplexTypeDefinition unordered = ComplexType(
            importer,
            "Unordered");
        XsdDeclarationDefinition item = importer.LastDeclarationResult
            .Elements.Single(value => value.Global && value.Name == "item");

        Assert.Multiple(() =>
        {
            Assert.That(importer.LastAdvancedMappingResult.Success,
                Is.True, Format(importer.LastAdvancedMappingResult.Diagnostics));
            Assert.That(code.EffectiveFacets.Select(value => value.Name),
                Is.EquivalentTo(new[]
                {
                    "minLength", "maxLength", "pattern", "whiteSpace",
                    "enumeration"
                }));
            Assert.That(code.AsnTypeDefinition,
                Is.EqualTo("ENUMERATED {AA}"));
            Assert.That(codes.Variety, Is.EqualTo(XsdSimpleTypeVariety.List));
            Assert.That(codes.ItemType?.Name, Is.EqualTo("Code"));
            Assert.That(scalar.Variety,
                Is.EqualTo(XsdSimpleTypeVariety.Union));
            Assert.That(scalar.UnionAlternatives.Select(value =>
                value.TypeUse?.Name), Is.EqualTo(new[] { "int", "boolean" }));
            Assert.That(baseType.Mixed, Is.True);
            Assert.That(baseType.Particle?.Kind, Is.EqualTo("sequence"));
            Assert.That(baseType.Particle?.Children.Select(value => value.Kind),
                Is.EqualTo(new[] { "group", "any" }));
            Assert.That(baseType.AttributeUses.Single().Required, Is.True);
            Assert.That(baseType.AttributeWildcard?.Kind,
                Is.EqualTo("attribute"));
            Assert.That(baseType.Wildcards.Single().Kind,
                Is.EqualTo("element"));
            Assert.That(derived.Derivation, Is.EqualTo("extension"));
            Assert.That(derived.BaseComplexType, Is.SameAs(baseType));
            Assert.That(restricted.Derivation, Is.EqualTo("restriction"));
            Assert.That(unordered.Particle?.Kind, Is.EqualTo("all"));
            Assert.That(importer.LastAdvancedMappingResult.SubstitutionGroups
                .Single().Members.Single().Name, Is.EqualTo("Member"));
            Assert.That(item.Nillable, Is.True);
            Assert.That(item.TypeSubstitution?.Alternatives.Select(value =>
                value.Name), Does.Contain("Derived"));
            Assert.That(item.XerInstructions.Select(value => value.Kind),
                Does.Contain(AsnInstructionKind.UseNil));
            Assert.That(item.XerInstructions.Select(value => value.Kind),
                Does.Contain(AsnInstructionKind.UseType));
            Assert.That(item.NamespaceUri,
                Is.EqualTo("urn:mapping-families"));
            Assert.That(importer.LastModuleGenerationResult.Modules.Single()
                .SourceUnit.Text, Does.Contain("ENCODING-CONTROL XER"));
        });
    }

    [Test]
    public void MapsIncludeImportAndRedefineDocumentsThroughGeneration()
    {
        AsnXsdInput root = Input(
            "root.xsd",
            "https://x694.test/root.xsd",
            Schema(
                "urn:root",
                "<xs:include schemaLocation=\"common.xsd\"/>"
                    + "<xs:import namespace=\"urn:foreign\" "
                    + "schemaLocation=\"foreign.xsd\"/>"
                    + "<xs:redefine schemaLocation=\"base.xsd\">"
                    + "<xs:complexType name=\"Base\"><xs:complexContent>"
                    + "<xs:extension base=\"tns:Base\"><xs:attribute "
                    + "name=\"revision\" type=\"xs:int\"/>"
                    + "</xs:extension></xs:complexContent>"
                    + "</xs:complexType></xs:redefine>"
                    + "<xs:complexType name=\"Root\"><xs:sequence>"
                    + "<xs:element name=\"shared\" type=\"tns:Shared\"/>"
                    + "<xs:element ref=\"f:Foreign\"/>"
                    + "</xs:sequence></xs:complexType>"));
        AsnXsdInput common = Input(
            "common.xsd",
            "https://x694.test/common.xsd",
            Schema(
                "",
                "<xs:simpleType name=\"Shared\"><xs:restriction "
                    + "base=\"xs:string\"><xs:maxLength value=\"12\"/>"
                    + "</xs:restriction></xs:simpleType>"));
        AsnXsdInput foreign = Input(
            "foreign.xsd",
            "https://x694.test/foreign.xsd",
            Schema(
                "urn:foreign",
                "<xs:element name=\"Foreign\" type=\"xs:int\"/>"));
        AsnXsdInput baseSchema = Input(
            "base.xsd",
            "https://x694.test/base.xsd",
            Schema(
                "urn:root",
                "<xs:complexType name=\"Base\"><xs:sequence>"
                    + "<xs:element name=\"value\" type=\"xs:string\"/>"
                    + "</xs:sequence></xs:complexType>"));

        XsdSchemaImporterImplementation importer = Import(
            [root, common, foreign, baseSchema]);
        XsdComplexTypeDefinition originalBase = importer
            .LastComplexTypeResult.Types.Single(value =>
                value.Name == "Base"
                    && value.Component != null
                    && !value.Component.Redefinition);
        XsdComplexTypeDefinition redefinedBase = importer
            .LastComplexTypeResult.Types.Single(value =>
                value.Name == "Base"
                    && value.Component != null
                    && value.Component.Redefinition);

        Assert.Multiple(() =>
        {
            Assert.That(importer.LastGraphResult.Success,
                Is.True, Format(importer.LastGraphResult.Diagnostics));
            Assert.That(importer.LastGraphResult.Edges.Select(value =>
                value.Kind), Is.EqualTo(new[]
                {
                    "include", "import", "redefine"
                }));
            Assert.That(importer.LastGraphResult.Edges.Single(value =>
                value.Kind == "include").Chameleon, Is.True);
            Assert.That(importer.LastGraphResult.Components.Any(value =>
                value.Name == "Shared"
                    && value.NamespaceUri == "urn:root"), Is.True);
            Assert.That(importer.LastGraphResult.Components.Any(value =>
                value.Name == "Foreign"
                    && value.NamespaceUri == "urn:foreign"), Is.True);
            Assert.That(importer.LastGraphResult.Components.Count(value =>
                value.Name == "Base"), Is.EqualTo(2));
            Assert.That(redefinedBase.BaseComplexType,
                Is.SameAs(originalBase));
            Assert.That(redefinedBase.AsnTypeDefinition,
                Does.Contain("value XSDv2.String"));
            Assert.That(redefinedBase.AsnTypeDefinition,
                Does.Contain("revision XSDv2.Int"));
            Assert.That(importer.LastModuleGenerationResult.Success,
                Is.True, Format(
                    importer.LastModuleGenerationResult.Diagnostics));
            Assert.That(importer.LastModuleGenerationResult.Modules.Single()
                .Assignments.Select(value => value.OriginalName),
                Does.Contain("Shared"));
            Assert.That(importer.LastModuleGenerationResult.Modules.Single()
                .Assignments.Select(value => value.OriginalName),
                Does.Contain("Foreign"));
        });
    }

    [Test]
    public void ReportsEveryUnmappableXsdMappingFamilyAtItsSource()
    {
        string[] names =
        [
            "assert", "alternative", "openContent", "defaultOpenContent",
            "override", "notation", "unique", "key", "keyref",
            "notNamespace", "notQName", "xpathDefaultNamespace",
            "defaultAttributes", "inheritable"
        ];
        XsdSchemaImporterImplementation importer = Import(
            [Input(
                "unmappable.xsd",
                "https://x694.test/unmappable.xsd",
                Schema(
                    "urn:unmappable",
                    "<xs:defaultOpenContent/><xs:override/>"
                        + "<xs:notation name=\"format\"/>"
                        + "<xs:complexType name=\"Checked\" "
                        + "defaultAttributes=\"attrs\"><xs:openContent/>"
                        + "<xs:assert test=\"true()\"/>"
                        + "<xs:any notNamespace=\"urn:hidden\" "
                        + "notQName=\"tns:hidden\"/>"
                        + "<xs:attribute name=\"state\" "
                        + "inheritable=\"true\"/>"
                        + "</xs:complexType>"
                        + "<xs:element name=\"item\" "
                        + "xpathDefaultNamespace=\"##targetNamespace\">"
                        + "<xs:alternative type=\"xs:string\"/>"
                        + "<xs:unique name=\"uniqueItem\"/>"
                        + "<xs:key name=\"itemKey\"/>"
                        + "<xs:keyref name=\"itemRef\" refer=\"tns:itemKey\"/>"
                        + "</xs:element>"))]);
        AsnDiagnostic[] diagnostics = importer.LastMappingValidationResult
            .Diagnostics.Where(value => value.Code == "ASNXSD0035")
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(importer.LastMappingValidationResult.Success,
                Is.False);
            Assert.That(importer.LastModuleGenerationResult.SourceUnits,
                Is.Empty);
            Assert.That(diagnostics, Has.Length.EqualTo(names.Length));
            int index = 0;
            while (index < names.Length)
            {
                Assert.That(diagnostics.Any(value => value.Message.Contains(
                    "'" + names[index] + "'",
                    System.StringComparison.Ordinal)),
                    Is.True,
                    names[index]);
                index = index + 1;
            }
            Assert.That(diagnostics.All(value =>
                value.Span?.Start.SourceName == "unmappable.xsd"), Is.True);
        });
    }

    private static XsdSchemaImporterImplementation Import(
        AsnXsdInput[] inputs)
    {
        XsdSchemaImporterImplementation importer =
            new XsdSchemaImporterImplementation();
        AsnXsdImportResult result = importer.ImportSchemas(
            inputs,
            new AsnXsdImportOptions
            {
                HasModuleName = true,
                ModuleName = "X694-Clause-Coverage"
            });
        if (importer.LastMappingValidationResult.Success)
        {
            Assert.That(result.Success,
                Is.True, Format(result.Diagnostics));
        }
        return importer;
    }

    private static AsnXsdInput Input(
        string name,
        string baseUri,
        string text)
    {
        return new AsnXsdInput
        {
            Name = name,
            HasBaseUri = true,
            BaseUri = baseUri,
            Text = text
        };
    }

    private static string Schema(string targetNamespace, string content)
    {
        string target = targetNamespace.Length == 0
            ? ""
            : " targetNamespace=\"" + targetNamespace + "\""
                + " xmlns:tns=\"" + targetNamespace + "\"";
        return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\""
            + " xmlns:f=\"urn:foreign\"" + target + ">"
            + content + "</xs:schema>";
    }

    private static XsdSimpleTypeDefinition SimpleType(
        XsdSchemaImporterImplementation importer,
        string name)
    {
        return importer.LastSimpleTypeResult.Types.Single(value =>
            value.Name == name);
    }

    private static XsdComplexTypeDefinition ComplexType(
        XsdSchemaImporterImplementation importer,
        string name)
    {
        return importer.LastComplexTypeResult.Types.Single(value =>
            value.Name == name);
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(value =>
            value.Code + ":" + value.Message));
    }
}
