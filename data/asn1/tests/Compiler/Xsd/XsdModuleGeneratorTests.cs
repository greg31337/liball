using asn1;

namespace asn1.Tests;

public class XsdModuleGeneratorTests
{
    [Test]
    public void GeneratesOrderedModuleImportsConstraintsAndControlSection()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:element name=\"z\" type=\"xs:string\"/>"
                + "<xs:element name=\"a\" type=\"xs:int\"/>"
                + "<xs:attribute name=\"b\" type=\"xs:string\"/>"
                + "<xs:simpleType name=\"Count\"><xs:restriction "
                + "base=\"xs:int\"><xs:minInclusive value=\"1\"/>"
                + "<xs:maxInclusive value=\"9\"/>"
                + "</xs:restriction></xs:simpleType>"
                + "<xs:complexType name=\"Box\"><xs:sequence>"
                + "<xs:element name=\"value\" type=\"xs:string\"/>"
                + "<xs:any processContents=\"skip\"/>"
                + "</xs:sequence><xs:anyAttribute/>"
                + "</xs:complexType>"
                + "<xs:group name=\"Pair\"><xs:sequence>"
                + "<xs:element name=\"left\" type=\"xs:int\"/>"
                + "</xs:sequence></xs:group>",
            "Mapped.Schema");
        XsdGeneratedModule module = importer.LastModuleGenerationResult
            .Modules.Single();
        string source = module.SourceUnit.Text;

        Assert.Multiple(() =>
        {
            Assert.That(module.ModuleName, Is.EqualTo("Mapped-Schema"));
            Assert.That(module.ModuleIdentifier, Is.EqualTo("Mapped-Schema"));
            Assert.That(module.AutomaticTags, Is.True);
            Assert.That(module.Assignments.Where(value => !value.Special)
                .Select(value => value.OriginalName),
                Is.EqualTo(new[] { "a", "z", "b", "Box", "Count", "Pair" }));
            Assert.That(module.Imports.Single().ModuleName,
                Is.EqualTo("XSDv2"));
            Assert.That(module.Imports.Single().Symbols,
                Is.EqualTo(new[] { "Int", "String" }));
            Assert.That(module.Assignments.Single(value =>
                value.OriginalName == "Count").Definition,
                Does.Contain("(1..9)"));
            Assert.That(module.Assignments.Single(value =>
                value.OriginalName == "Box").Definition,
                Does.Contain("CONSTRAINED BY"));
            Assert.That(source,
                Does.StartWith("Mapped-Schema DEFINITIONS XER INSTRUCTIONS AUTOMATIC TAGS ::=\nBEGIN"));
            Assert.That(source, Does.Contain("FROM XSDv2 { joint-iso-itu-t"));
            Assert.That(source,
                Does.Contain("ENCODING-CONTROL XER\nGLOBAL-DEFAULTS MODIFIED-ENCODINGS"));
            Assert.That(source,
                Does.Contain("GLOBAL-DEFAULTS CONTROL-NAMESPACE \"http://www.w3.org/2001/XMLSchema-instance\" PREFIX \"xsi\""));
            Assert.That(source, Does.Not.Contain("EXPORTS"));
            Assert.That(importer.LastModuleGenerationResult.SourceUnits.Single(),
                Is.SameAs(module.SourceUnit));
        });
    }

    [Test]
    public void ResolvesGlobalTypeNameCollisionsAndReservedNames()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:element name=\"dup\" type=\"xs:string\"/>"
                + "<xs:attribute name=\"dup\" type=\"xs:string\"/>"
                + "<xs:simpleType name=\"String\"><xs:restriction "
                + "base=\"xs:string\"/></xs:simpleType>"
                + "<xs:simpleType name=\"a.b\"><xs:restriction "
                + "base=\"xs:string\"/></xs:simpleType>"
                + "<xs:simpleType name=\"a_b\"><xs:restriction "
                + "base=\"xs:string\"/></xs:simpleType>"
                + "<xs:simpleType name=\"GeneralizedTime\"><xs:restriction "
                + "base=\"xs:string\"/></xs:simpleType>"
                + "<xs:complexType name=\"COMPONENTS\"/>",
            "Names");
        XsdGeneratedAssignment[] assignments = importer
            .LastModuleGenerationResult.Modules.Single().Assignments;

        Assert.Multiple(() =>
        {
            Assert.That(assignments.Single(value =>
                value.Kind == "element").Name, Is.EqualTo("Dup"));
            Assert.That(assignments.Single(value =>
                value.Kind == "attribute").Name, Is.EqualTo("Dup-1"));
            Assert.That(assignments.Single(value =>
                value.OriginalName == "String").Name, Is.EqualTo("String-1"));
            Assert.That(assignments.Single(value =>
                value.OriginalName == "COMPONENTS").Name,
                Is.EqualTo("COMPONENTS-1"));
            Assert.That(assignments.Single(value =>
                value.OriginalName == "GeneralizedTime").Name,
                Is.EqualTo("GeneralizedTime-1"));
            Assert.That(assignments.Where(value =>
                value.OriginalName == "a.b" || value.OriginalName == "a_b")
                .Select(value => value.Name),
                Is.EqualTo(new[] { "A-b", "A-b-1" }));
            Assert.That(importer.LastModuleGenerationResult.Modules.Single()
                .XerInstructions.Where(value =>
                    value.Instruction?.Kind == AsnInstructionKind.Name)
                .Select(value => value.Target),
                Does.Contain("COMPONENTS-1"));
        });
    }

    [Test]
    public void ResolvesComponentAndEnumerationIdentifierCollisions()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:simpleType name=\"Words\"><xs:restriction "
                + "base=\"xs:string\"><xs:enumeration value=\"A\"/>"
                + "<xs:enumeration value=\"a\"/>"
                + "</xs:restriction></xs:simpleType>"
                + "<xs:complexType name=\"Container\"><xs:sequence>"
                + "<xs:element name=\"Elem\" type=\"xs:string\"/>"
                + "<xs:element name=\"elem\" type=\"xs:string\"/>"
                + "<xs:element name=\"Elem-1\" type=\"xs:string\"/>"
                + "<xs:element name=\"elem-1\" type=\"xs:string\"/>"
                + "</xs:sequence></xs:complexType>",
            "Identifiers");
        XsdGeneratedAssignment container = importer.LastModuleGenerationResult
            .Modules.Single().Assignments.Single(value =>
                value.OriginalName == "Container");
        XsdGeneratedAssignment words = importer.LastModuleGenerationResult
            .Modules.Single().Assignments.Single(value =>
                value.OriginalName == "Words");

        Assert.Multiple(() =>
        {
            Assert.That(container.Definition,
                Does.Contain("elem XSDv2.String, elem-1 XSDv2.String, elem-1-1 XSDv2.String, elem-1-2 XSDv2.String"));
            Assert.That(words.Definition,
                Is.EqualTo("ENUMERATED {a, a-1}"));
        });
    }

    [Test]
    public void GeneratesSpecialAssignmentsAfterDirectAssignments()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:complexType name=\"Base\"/>"
                + "<xs:complexType name=\"Derived\"><xs:complexContent>"
                + "<xs:extension base=\"tns:Base\"/>"
                + "</xs:complexContent></xs:complexType>"
                + "<xs:element name=\"item\" type=\"tns:Base\"/>"
                + "<xs:element name=\"head\" type=\"xs:string\" "
                + "abstract=\"true\"/>"
                + "<xs:element name=\"member\" type=\"xs:string\" "
                + "substitutionGroup=\"tns:head\"/>",
            "Specials");
        XsdGeneratedAssignment[] assignments = importer
            .LastModuleGenerationResult.Modules.Single().Assignments;
        int firstSpecial = System.Array.FindIndex(assignments,
            value => value.Special);

        Assert.Multiple(() =>
        {
            Assert.That(firstSpecial, Is.GreaterThan(0));
            Assert.That(assignments.Take(firstSpecial).All(value =>
                !value.Special), Is.True);
            Assert.That(assignments.Skip(firstSpecial).All(value =>
                value.Special), Is.True);
            Assert.That(assignments.Select(value => value.Name),
                Does.Contain("Base-derivations"));
            Assert.That(assignments.Select(value => value.Name),
                Does.Contain("Head-group"));
            Assert.That(assignments.Single(value =>
                value.OriginalName == "item").Definition,
                Is.EqualTo("Base-derivations"));
            Assert.That(assignments.Single(value =>
                value.Name == "Head-group").Definition,
                Does.Contain("member Member"));
        });
    }

    [Test]
    public void IsDeterministicAcrossInputOrder()
    {
        AsnXsdInput first = SchemaInput(
            "first.xsd",
            "urn:first",
            "<xs:element name=\"same\" type=\"xs:string\"/>");
        AsnXsdInput second = SchemaInput(
            "second.xsd",
            "urn:second",
            "<xs:element name=\"same\" type=\"xs:int\"/>");
        string forward = ImportInputs([first, second], "Combined")
            .LastModuleGenerationResult.SourceUnits.Single().Text;
        string reverse = ImportInputs([second, first], "Combined")
            .LastModuleGenerationResult.SourceUnits.Single().Text;

        Assert.That(reverse, Is.EqualTo(forward));
    }

    [Test]
    public void DiagnosesEmptyExplicitModuleName()
    {
        XsdSchemaImporterImplementation importer = Import(
            "<xs:element name=\"value\" type=\"xs:string\"/>",
            "");

        Assert.Multiple(() =>
        {
            Assert.That(importer.LastModuleGenerationResult.Success, Is.False);
            Assert.That(importer.LastModuleGenerationResult.SourceUnits,
                Is.Empty);
            Assert.That(importer.LastModuleGenerationResult.Diagnostics
                .Single().Code, Is.EqualTo("ASNXSD0034"));
        });
    }

    [Test]
    public void DerivesDeterministicDefaultModuleName()
    {
        XsdSchemaImporterImplementation importer =
            new XsdSchemaImporterImplementation();
        AsnXsdImportResult result = importer.ImportSchemas(
            [SchemaInput(
                "default.xsd",
                "urn:default-name",
                "<xs:element name=\"value\" type=\"xs:string\"/>")],
            new AsnXsdImportOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(importer.LastModuleGenerationResult.Modules.Single()
                .ModuleName, Is.EqualTo("XSD-urndefault-name"));
            Assert.That(result.SourceUnits.Single().Name,
                Is.EqualTo("XSD-urndefault-name.asn1"));
        });
    }

    private static XsdSchemaImporterImplementation Import(
        string content,
        string moduleName)
    {
        return ImportInputs(
            [SchemaInput("module.xsd", "urn:test", content)],
            moduleName);
    }

    private static XsdSchemaImporterImplementation ImportInputs(
        AsnXsdInput[] inputs,
        string moduleName)
    {
        XsdSchemaImporterImplementation importer =
            new XsdSchemaImporterImplementation();
        importer.ImportSchemas(
            inputs,
            new AsnXsdImportOptions
            {
                HasModuleName = true,
                ModuleName = moduleName
            });
        return importer;
    }

    private static AsnXsdInput SchemaInput(
        string name,
        string targetNamespace,
        string content)
    {
        return new AsnXsdInput
        {
            Name = name,
            HasBaseUri = true,
            BaseUri = "urn:" + name,
            Text = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" "
                + "xmlns:tns=\"" + targetNamespace + "\" "
                + "targetNamespace=\"" + targetNamespace + "\">"
                + content + "</xs:schema>"
        };
    }
}
