using System.IO;
using System.Linq;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class OrchestraReaderTests
{
    [Test]
    public void ReadsVersion11ContentAndPreservesSupportedExtensions()
    {
        string xml = """
            <fixr:repository xmlns:fixr="http://fixprotocol.io/2026/orchestra/repository"
                             xmlns:dcterms="http://purl.org/dc/terms/"
                             xmlns:venue="urn:venue:reader"
                             name="Venue" version="FIX.Latest">
              <fixr:metadata><dcterms:title>Venue repository</dcterms:title></fixr:metadata>
              <fixr:datatypes><fixr:datatype name="String"/></fixr:datatypes>
              <fixr:codeSets/>
              <fixr:fields/>
              <fixr:actors>
                <fixr:actor name="Buyer"/>
                <fixr:actor name="Seller"/>
                <fixr:flow name="Orders" source="Buyer" destination="Seller"/>
              </fixr:actors>
              <venue:settings mode="strict"/>
            </fixr:repository>
            """;

        OrchestraReadResult result = Read(xml, new OrchestraReadOptions { BaseUri = "venue.xml", ValidateSchema = false });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Codes(result));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Repository, Is.Not.Null);
            Assert.That(result.Repository!.Version, Is.EqualTo(OrchestraVersion.Version11));
            Assert.That(result.Repository.Metadata.Name, Is.EqualTo("Venue"));
            Assert.That(result.Repository.Actors.Select(item => item.Name), Is.EqualTo(new[] { "Buyer", "Seller" }));
            Assert.That(result.Repository.Flows.Single().Name, Is.EqualTo("Orders"));
            Assert.That(result.Repository.Extensions.Single().NamespaceUri, Is.EqualTo("urn:venue:reader"));
            Assert.That(result.Repository.Extensions.Single().Xml, Does.Contain("mode=\"strict\""));
        });
    }

    [Test]
    public void CombinesSchemaAndLocatedSemanticDiagnosticsWithPartialRepository()
    {
        string xml = """
            <repository xmlns="http://fixprotocol.io/2026/orchestra/repository"
                        xmlns:dcterms="http://purl.org/dc/terms/"
                        name="Example" version="1.1">
              <metadata><dcterms:title>Example repository</dcterms:title></metadata>
              <datatypes/>
              <codeSets/>
              <fields>
                <field id="1" name="OrderId" type="MissingType"/>
              </fields>
            </repository>
            """;

        OrchestraReadResult result = Read(xml, new OrchestraReadOptions { BaseUri = "invalid-reference.xml" });
        OrchestraDiagnostic semantic = result.Diagnostics.Single(item => item.Code == "unresolved-field-type");
        int schemaIndex = result.Diagnostics.FindIndex(item => item.Code == "orchestra-schema-validation-error");
        int semanticIndex = result.Diagnostics.FindIndex(item => item.Code == "unresolved-field-type");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Repository, Is.Not.Null);
            Assert.That(result.Repository!.Fields.Single().Name, Is.EqualTo("OrderId"));
            Assert.That(schemaIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(semanticIndex, Is.GreaterThan(schemaIndex));
            Assert.That(semantic.Source, Is.EqualTo("invalid-reference.xml"));
            Assert.That(semantic.Line, Is.EqualTo(8));
            Assert.That(semantic.Column, Is.GreaterThan(0));
            Assert.That(semantic.ElementPath, Is.EqualTo("/repository/fields/field"));
        });
    }

    [Test]
    public void SchemaValidationCanBeDisabledWithoutSuppressingSemanticValidation()
    {
        string xml = """
            <repository xmlns="http://fixprotocol.io/2026/orchestra/repository"
                        xmlns:dcterms="http://purl.org/dc/terms/"
                        name="Example" version="1.1">
              <metadata><dcterms:title>Example repository</dcterms:title></metadata>
              <datatypes/>
              <codeSets/>
              <fields><field id="1" name="OrderId" type="MissingType"/></fields>
            </repository>
            """;

        OrchestraReadResult result = Read(xml, new OrchestraReadOptions { BaseUri = "semantic-only.xml", ValidateSchema = false });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Repository, Is.Not.Null);
            Assert.That(result.Diagnostics.Any(item => item.Code == "orchestra-schema-validation-error"), Is.False);
            Assert.That(result.Diagnostics.Select(item => item.Code), Is.EqualTo(new[] { "unresolved-field-type" }));
        });
    }

    [Test]
    public void ContentSelectionOmitsBehaviorAndInterfacesBeforeValidation()
    {
        string repositoryXml = """
            <repository xmlns="http://fixprotocol.io/2026/orchestra/repository" name="Behavior" version="1.1">
              <datatypes/><codeSets/><fields/>
              <actors><actor name="Buyer"/><flow name="Broken" source="Buyer" destination="Missing"/></actors>
            </repository>
            """;
        string interfacesXml = """
            <interfaces xmlns="http://fixprotocol.io/2026/orchestra/interfaces">
              <interface name="Trading"><transport name="TCP" address="fix.example:9876"/></interface>
            </interfaces>
            """;
        OrchestraReadOptions repositoryOptions = new OrchestraReadOptions { BaseUri = "behavior.xml", ValidateSchema = false, IncludeBehavior = false };
        OrchestraReadOptions interfaceOptions = new OrchestraReadOptions { BaseUri = "interfaces.xml", ValidateSchema = false, IncludeInterfaces = false };

        OrchestraReadResult repository = Read(repositoryXml, repositoryOptions);
        OrchestraReadResult interfaces = Read(interfacesXml, interfaceOptions);

        Assert.Multiple(() =>
        {
            Assert.That(repository.Success, Is.True, Codes(repository));
            Assert.That(repository.Repository!.Actors, Is.Empty);
            Assert.That(repository.Repository.Flows, Is.Empty);
            Assert.That(repository.Diagnostics.Any(item => item.Code == "unresolved-flow-destination"), Is.False);
            Assert.That(interfaces.Success, Is.True, Codes(interfaces));
            Assert.That(interfaces.Repository!.Interfaces, Is.Empty);
            Assert.That(interfaces.Repository.RepositoryLinks, Is.Empty);
        });
    }

    [Test]
    public void UnrecognizedDocumentDoesNotProducePartialRepository()
    {
        string xml = "<repository xmlns=\"urn:unsupported\"/>";

        OrchestraReadResult result = Read(xml, new OrchestraReadOptions { BaseUri = "unsupported.xml" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Repository, Is.Null);
            Assert.That(result.Diagnostics.Select(item => item.Code), Is.EqualTo(new[] { "unsupported-orchestra-namespace" }));
            Assert.That(result.Diagnostics.Single().Source, Is.EqualTo("unsupported.xml"));
        });
    }

    private static OrchestraReadResult Read(string xml, OrchestraReadOptions options)
    {
        using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new OrchestraReader().ReadRepository(source, options);
    }

    private static string Codes(OrchestraReadResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Code + ":" + item.Message));
    }
}
