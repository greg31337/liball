using asn1;

namespace asn1.Tests;

public class XsdDocumentLoaderTests
{
    [Test]
    public void LoadsSchemaIdentityNamespacesBasesAndLocations()
    {
        AsnXsdInput main = Input(
            "main.xsd",
            "https://example.test/root/main.xsd",
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" "
                + "xmlns:tns=\"urn:main\" targetNamespace=\"urn:main\" "
                + "xml:base=\"schemas/\">"
                + "<xs:include xml:base=\"v1/\" schemaLocation=\"common.xsd\"/>"
                + "<xs:import namespace=\"urn:other\" schemaLocation=\"../other.xsd\"/>"
                + "<xs:redefine schemaLocation=\"rewrite.xsd\"/>"
                + "</xs:schema>");
        AsnXsdInput other = Input(
            "other.xsd",
            "https://example.test/root/other.xsd",
            "<schema xmlns=\"http://www.w3.org/2001/XMLSchema\" "
                + "targetNamespace=\"urn:other\"/>");

        XsdDocumentLoadResult result = new XsdDocumentLoader()
            .LoadDocuments([main, other], new AsnXsdImportOptions());
        XsdLoadedDocument document = result.Documents.Single(value =>
            value.Name == "main.xsd");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Format(result.Diagnostics));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Documents, Has.Length.EqualTo(2));
            Assert.That(result.ResolverInputs.Select(value =>
                value.ResolverUri), Is.EqualTo(new[]
                {
                    "https://example.test/root/main.xsd",
                    "https://example.test/root/other.xsd"
                }));
            Assert.That(document.EffectiveBaseUri,
                Is.EqualTo("https://example.test/root/schemas/"));
            Assert.That(document.TargetNamespace, Is.EqualTo("urn:main"));
            Assert.That(document.NamespaceBindings.Single(value =>
                value.Prefix == "xs").NamespaceUri,
                Is.EqualTo(XsdDocumentLoader.SchemaNamespace));
            Assert.That(document.NamespaceBindings.Single(value =>
                value.Prefix == "tns").NamespaceUri,
                Is.EqualTo("urn:main"));
            Assert.That(document.SchemaLocations.Select(value => value.Kind),
                Is.EqualTo(new[] { "include", "import", "redefine" }));
            Assert.That(document.SchemaLocations.Select(value =>
                value.ResolvedUri), Is.EqualTo(new[]
                {
                    "https://example.test/root/schemas/v1/common.xsd",
                    "https://example.test/root/other.xsd",
                    "https://example.test/root/schemas/rewrite.xsd"
                }));
            Assert.That(document.SchemaLocations[1].NamespaceUri,
                Is.EqualTo("urn:other"));
            Assert.That(document.SchemaLocations.All(value =>
                value.Span?.Start.SourceName == "main.xsd"), Is.True);
        });
    }

    [Test]
    public void RetainsUnprovidedLocationsAndRejectsAmbientResolution()
    {
        AsnXsdInput input = Input(
            "isolated.xsd",
            "https://schemas.test/isolated.xsd",
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">"
                + "<xs:import namespace=\"urn:remote\" "
                + "schemaLocation=\"https://unavailable.invalid/remote.xsd\"/>"
                + "</xs:schema>");
        XsdSchemaImporterImplementation importer =
            new XsdSchemaImporterImplementation();

        AsnXsdImportResult imported = importer.ImportSchemas(
            [input],
            new AsnXsdImportOptions());
        XsdSchemaLocation location = importer.LastLoadResult.Documents
            .Single().SchemaLocations.Single();

        Assert.Multiple(() =>
        {
            Assert.That(imported.Success, Is.False);
            Assert.That(imported.Diagnostics.Single().Code,
                Is.EqualTo("ASNXSD0009"));
            Assert.That(importer.LastLoadResult.ResolverInputs,
                Has.Length.EqualTo(1));
            Assert.That(importer.LastLoadResult.Documents,
                Has.Length.EqualTo(1));
            Assert.That(location.ResolvedUri,
                Is.EqualTo("https://unavailable.invalid/remote.xsd"));
        });
    }

    [Test]
    public void DiagnosesMalformedDocumentsRootsUrisAndResolverCollisions()
    {
        XsdDocumentLoadResult malformed = new XsdDocumentLoader()
            .LoadDocuments(
                [Input("broken.xsd", "urn:broken", "<xs:schema")],
                new AsnXsdImportOptions());
        XsdDocumentLoadResult wrongRoot = new XsdDocumentLoader()
            .LoadDocuments(
                [Input("wrong.xsd", "urn:wrong", "<schema/>")],
                new AsnXsdImportOptions());
        XsdDocumentLoadResult invalidBase = new XsdDocumentLoader()
            .LoadDocuments(
                [Input("base.xsd", "relative/base.xsd", Schema())],
                new AsnXsdImportOptions());
        XsdDocumentLoadResult duplicate = new XsdDocumentLoader()
            .LoadDocuments(
                [
                    Input("one.xsd", "urn:same", Schema()),
                    Input("two.xsd", "urn:same", Schema())
                ],
                new AsnXsdImportOptions());
        XsdDocumentLoadResult missingLocation = new XsdDocumentLoader()
            .LoadDocuments(
                [Input(
                    "include.xsd",
                    "urn:include",
                    "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">"
                        + "<xs:include/>"
                        + "</xs:schema>")],
                new AsnXsdImportOptions());

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Diagnostics.Single().Code,
                Is.EqualTo("ASNXSD0003"));
            Assert.That(malformed.Diagnostics.Single().Span?.Start.SourceName,
                Is.EqualTo("broken.xsd"));
            Assert.That(wrongRoot.Diagnostics.Single().Code,
                Is.EqualTo("ASNXSD0004"));
            Assert.That(invalidBase.Diagnostics.Single().Code,
                Is.EqualTo("ASNXSD0002"));
            Assert.That(duplicate.Diagnostics.Select(value => value.Code),
                Does.Contain("ASNXSD0007"));
            Assert.That(missingLocation.Diagnostics.Single().Code,
                Is.EqualTo("ASNXSD0006"));
        });
    }

    [Test]
    public void EnforcesConfiguredDocumentAndResolverBounds()
    {
        XsdDocumentLoadResult textLimit = new XsdDocumentLoader()
            .LoadDocuments(
                [Input("large.xsd", "urn:large", Schema())],
                new AsnXsdImportOptions
                {
                    ResourceLimits = new AsnResourceLimits
                    {
                        MaximumTextCharacters = 10
                    }
                });
        XsdDocumentLoadResult inputLimit = new XsdDocumentLoader()
            .LoadDocuments(
                [
                    Input("one.xsd", "urn:one", Schema()),
                    Input("two.xsd", "urn:two", Schema())
                ],
                new AsnXsdImportOptions
                {
                    ResourceLimits = new AsnResourceLimits
                    {
                        MaximumCollectionElements = 1
                    }
                });

        Assert.Multiple(() =>
        {
            Assert.That(textLimit.Success, Is.False);
            Assert.That(textLimit.Documents, Is.Empty);
            Assert.That(textLimit.Diagnostics.Single().Code,
                Is.EqualTo("ASNXSD0008"));
            Assert.That(inputLimit.Success, Is.False);
            Assert.That(inputLimit.Documents, Is.Empty);
            Assert.That(inputLimit.Diagnostics.Single().Code,
                Is.EqualTo("ASNXSD0008"));
        });
    }

    [Test]
    public void CompilerUsesTheBoundedLoaderForXsdInputsByDefault()
    {
        AsnCompilationResult result = new CompilerImplementation().Compile(
            new AsnCompilationInput
            {
                XsdInputs =
                [
                    Input(
                        "compiler.xsd",
                        "urn:compiler-schema",
                        Schema())
                ]
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Format(result.Diagnostics));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.XsdImportResults, Has.Length.EqualTo(1));
            Assert.That(result.XsdImportResults[0].Success, Is.True);
            Assert.That(result.XsdImportResults[0].Diagnostics, Is.Empty);
        });
    }

    private static AsnXsdInput Input(
        string name,
        string baseUri,
        string text)
    {
        return new AsnXsdInput
        {
            Name = name,
            Text = text,
            HasBaseUri = true,
            BaseUri = baseUri
        };
    }

    private static string Schema()
    {
        return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"/>";
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(value =>
            value.Code + ":" + value.Message));
    }
}
