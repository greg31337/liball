using asn1;

namespace asn1.Tests;

public class XsdSourceMappingTests
{
    [Test]
    public void MapsGeneratedAssignmentsToXsdDocumentsAndComponents()
    {
        XsdSchemaImporterImplementation importer = Import(
            "mapped.xsd",
            "<xs:simpleType name=\"Code\">\n"
                + "<xs:restriction base=\"xs:string\"/>\n"
                + "</xs:simpleType>\n"
                + "<xs:element name=\"item\" type=\"tns:Code\"/>");
        XsdGeneratedModule module = importer.LastModuleGenerationResult
            .Modules.Single();
        XsdGeneratedSourceMapEntry mapping = module.SourceMap.Single(value =>
            value.Kind == "assignment" && value.XsdComponentName == "item");
        string generatedText = module.SourceUnit.Text.Substring(
            mapping.GeneratedStartOffset,
            mapping.GeneratedEndOffset - mapping.GeneratedStartOffset);

        Assert.Multiple(() =>
        {
            Assert.That(importer.LastModuleGenerationResult.Success, Is.True);
            Assert.That(mapping.StableId, Does.StartWith("x694:source-map:"));
            Assert.That(mapping.GeneratedSymbol, Is.EqualTo("Item"));
            Assert.That(generatedText, Is.EqualTo("Item ::= Code"));
            Assert.That(mapping.GeneratedSpan.Start.SourceName,
                Is.EqualTo("Mapped.asn1"));
            Assert.That(mapping.GeneratedSpan.Start.Offset,
                Is.EqualTo(mapping.GeneratedStartOffset));
            Assert.That(mapping.GeneratedSpan.End.Offset,
                Is.EqualTo(mapping.GeneratedEndOffset));
            Assert.That(mapping.XsdDocumentId, Is.Not.Empty);
            Assert.That(mapping.XsdComponentId, Is.Not.Empty);
            Assert.That(mapping.XsdComponentKind, Is.EqualTo("element"));
            Assert.That(mapping.XsdSpan?.Start.SourceName,
                Is.EqualTo("mapped.xsd"));
            Assert.That(mapping.XsdSpan?.Start.Line, Is.EqualTo(6));
            Assert.That(importer.LastModuleGenerationResult.SourceMap,
                Is.EqualTo(module.SourceMap));
            Assert.That(module.XsdSourceMappings.Single(value =>
                value.GeneratedSpan?.Start.Offset
                    == mapping.GeneratedStartOffset).ComponentName,
                Is.EqualTo(mapping.XsdComponentId));
            Assert.That(module.XsdSourceMappings.Single(value =>
                value.GeneratedSpan?.Start.Offset
                    == mapping.GeneratedStartOffset).SourceSpan,
                Is.SameAs(mapping.XsdSpan));
            Assert.That(module.SourceMap.Any(value =>
                value.Kind == "assignment"
                    && value.XsdComponentKind == "simpleType"
                    && value.XsdComponentName == "Code"),
                Is.True);
        });
    }

    [Test]
    public void MapsEncodingInstructionsToTheirXsdSource()
    {
        XsdSchemaImporterImplementation importer = Import(
            "instructions.xsd",
            "<xs:element name=\"item\" type=\"xs:string\"/>\n");
        XsdGeneratedModule module = importer.LastModuleGenerationResult
            .Modules.Single();
        XsdGeneratedSourceMapEntry mapping = module.SourceMap.Single(value =>
            value.Kind == "instruction"
                && value.GeneratedSymbol == "Item"
                && module.SourceUnit.Text.Substring(
                    value.GeneratedStartOffset,
                    value.GeneratedEndOffset - value.GeneratedStartOffset)
                    .StartsWith("NAMESPACE Item", System.StringComparison.Ordinal));
        string generatedText = module.SourceUnit.Text.Substring(
            mapping.GeneratedStartOffset,
            mapping.GeneratedEndOffset - mapping.GeneratedStartOffset);

        Assert.Multiple(() =>
        {
            Assert.That(generatedText,
                Is.EqualTo("NAMESPACE Item AS \"urn:test\""));
            Assert.That(mapping.GeneratedSpan.Start.SourceName,
                Is.EqualTo("Mapped.asn1"));
            Assert.That(mapping.XsdSpan?.Start.SourceName,
                Is.EqualTo("instructions.xsd"));
            Assert.That(mapping.XsdSpan?.Start.Line, Is.EqualTo(3));
            Assert.That(mapping.XsdDocumentId, Is.Not.Empty);
            Assert.That(mapping.XsdComponentId, Is.Not.Empty);
            Assert.That(mapping.XsdComponentKind, Is.EqualTo("element"));
            Assert.That(mapping.XsdComponentName, Is.EqualTo("item"));
        });
    }

    [Test]
    public void DiagnosesUnmappableXsdConstructAtItsSourceSpan()
    {
        XsdSchemaImporterImplementation importer = Import(
            "assert.xsd",
            "<xs:complexType name=\"Checked\">\n"
                + "<xs:assert test=\"true()\"/>\n"
                + "</xs:complexType>");
        AsnDiagnostic diagnostic = importer.LastMappingValidationResult
            .Diagnostics.Single(value => value.Code == "ASNXSD0035");

        Assert.Multiple(() =>
        {
            Assert.That(importer.LastMappingValidationResult.Success, Is.False);
            Assert.That(importer.LastModuleGenerationResult.SourceUnits,
                Is.Empty);
            Assert.That(diagnostic.Message, Does.Contain("assert"));
            Assert.That(diagnostic.Span?.Start.SourceName,
                Is.EqualTo("assert.xsd"));
            Assert.That(diagnostic.Span?.Start.Line, Is.EqualTo(4));
            Assert.That(diagnostic.Span?.Start.Column, Is.EqualTo(1));
        });
    }

    [Test]
    public void DiagnosesUnmappableXsdIdentityConstraint()
    {
        XsdSchemaImporterImplementation importer = Import(
            "identity.xsd",
            "<xs:element name=\"items\">\n"
                + "<xs:complexType/>\n"
                + "<xs:key name=\"itemKey\">\n"
                + "<xs:selector xpath=\"item\"/>\n"
                + "<xs:field xpath=\"@id\"/>\n"
                + "</xs:key>\n"
                + "</xs:element>");
        AsnDiagnostic diagnostic = importer.LastMappingValidationResult
            .Diagnostics.Single(value => value.Code == "ASNXSD0035");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Message, Does.Contain("key"));
            Assert.That(diagnostic.Span?.Start.SourceName,
                Is.EqualTo("identity.xsd"));
            Assert.That(diagnostic.Span?.Start.Line, Is.EqualTo(5));
            Assert.That(importer.LastModuleGenerationResult.Modules, Is.Empty);
        });
    }

    [Test]
    public void DiagnosesCompetingDeclarationTypesWithRelatedLocation()
    {
        XsdSchemaImporterImplementation importer = Import(
            "competing.xsd",
            "<xs:element name=\"value\" type=\"xs:string\">\n"
                + "<xs:simpleType><xs:restriction base=\"xs:string\"/>"
                + "</xs:simpleType>\n"
                + "</xs:element>");
        AsnDiagnostic diagnostic = importer.LastMappingValidationResult
            .Diagnostics.Single(value => value.Code == "ASNXSD0036");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Message,
                Does.Contain("type attribute and an inline simpleType"));
            Assert.That(diagnostic.Span?.Start.SourceName,
                Is.EqualTo("competing.xsd"));
            Assert.That(diagnostic.Span?.Start.Line, Is.EqualTo(4));
            Assert.That(diagnostic.RelatedLocations, Has.Length.EqualTo(1));
            Assert.That(diagnostic.RelatedLocations[0].Span.Start.Line,
                Is.EqualTo(3));
        });
    }

    [Test]
    public void DiagnosesInvalidWildcardNamespaceConstraint()
    {
        XsdSchemaImporterImplementation importer = Import(
            "wildcard.xsd",
            "<xs:complexType name=\"Open\">\n"
                + "<xs:sequence>\n"
                + "<xs:any namespace=\"##any urn:other\"/>\n"
                + "</xs:sequence>\n"
                + "</xs:complexType>");
        AsnDiagnostic diagnostic = importer.LastMappingValidationResult
            .Diagnostics.Single(value => value.Code == "ASNXSD0037");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Message, Does.Contain("##any urn:other"));
            Assert.That(diagnostic.Span?.Start.SourceName,
                Is.EqualTo("wildcard.xsd"));
            Assert.That(diagnostic.Span?.Start.Line, Is.EqualTo(5));
            Assert.That(importer.LastModuleGenerationResult.Modules, Is.Empty);
        });
    }

    [Test]
    public void DiagnosesInvalidAllOccurrenceAtTheOffendingElement()
    {
        XsdSchemaImporterImplementation importer = Import(
            "all.xsd",
            "<xs:complexType name=\"Items\">\n"
                + "<xs:all>\n"
                + "<xs:element name=\"item\" type=\"xs:string\" "
                + "maxOccurs=\"2\"/>\n"
                + "</xs:all>\n"
                + "</xs:complexType>");
        AsnDiagnostic diagnostic = importer.LastMappingValidationResult
            .Diagnostics.Single(value => value.Code == "ASNXSD0036");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Message,
                Does.Contain("element inside an all model group"));
            Assert.That(diagnostic.Span?.Start.SourceName,
                Is.EqualTo("all.xsd"));
            Assert.That(diagnostic.Span?.Start.Line, Is.EqualTo(5));
        });
    }

    private static XsdSchemaImporterImplementation Import(
        string name,
        string content)
    {
        XsdSchemaImporterImplementation importer =
            new XsdSchemaImporterImplementation();
        importer.ImportSchemas(
            [
                new AsnXsdInput
                {
                    Name = name,
                    HasBaseUri = true,
                    BaseUri = "urn:" + name,
                    Text = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"\n"
                        + "xmlns:tns=\"urn:test\" targetNamespace=\"urn:test\">\n"
                        + content + "\n</xs:schema>"
                }
            ],
            new AsnXsdImportOptions
            {
                HasModuleName = true,
                ModuleName = "Mapped"
            });
        return importer;
    }
}
