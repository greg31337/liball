using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using fix;

namespace fix.Tests;

[TestFixture]
public class OrchestraAcceptanceTests
{
    [Test]
    public void DownloadedVersion11Rc3SchemasMatchTheBuiltInCatalog()
    {
        string directory = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../specs/orchestra/Orchestra-Schema-v1.1-RC3"));
        XmlSchema repository = ReadSchema(Path.Combine(directory, "repository.xsd"));
        XmlSchema repositoryTypes = ReadSchema(Path.Combine(directory, "repositorytypes.xsd"));
        XmlSchema interfaces = ReadSchema(Path.Combine(directory, "interfaces.xsd"));
        XmlSchemaSet repositoryCatalog = OrchestraSchemaCatalog.GetSchemas(OrchestraVersion.Version11, false);
        XmlSchemaSet interfacesCatalog = OrchestraSchemaCatalog.GetSchemas(OrchestraVersion.Version11, true);

        Assert.Multiple(() =>
        {
            Assert.That(repository.TargetNamespace, Is.EqualTo(OrchestraXmlLoader.RepositoryVersion11Namespace));
            Assert.That(repository.Version, Is.EqualTo("1.1-rc3"));
            Assert.That(repository.Includes.OfType<XmlSchemaInclude>().Single().SchemaLocation, Is.EqualTo("repositorytypes.xsd"));
            Assert.That(repositoryTypes.TargetNamespace, Is.EqualTo(OrchestraXmlLoader.RepositoryVersion11Namespace));
            Assert.That(interfaces.TargetNamespace, Is.EqualTo(OrchestraXmlLoader.InterfacesVersion11Namespace));
            Assert.That(interfaces.Version, Is.EqualTo("1.1-rc3"));
            Assert.That(repositoryCatalog.GlobalElements.Contains(new XmlQualifiedName("repository", repository.TargetNamespace)), Is.True);
            Assert.That(interfacesCatalog.GlobalElements.Contains(new XmlQualifiedName("interfaces", interfaces.TargetNamespace)), Is.True);
        });
    }

    [Test]
    public void OfficialLatestEp309ReadsAsVersion10Repository()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../specs/fix/FIXOrchestra_FIX.Latest_EP309/FIXOrchestra_FIX.Latest_EP309.xml"));
        using FileStream source = File.OpenRead(path);

        OrchestraReadResult result = new OrchestraReader().ReadRepository(source, new OrchestraReadOptions { BaseUri = path });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Codes(result));
            Assert.That(result.Repository, Is.Not.Null);
            Assert.That(result.Repository!.Version, Is.EqualTo(OrchestraVersion.Version10));
            Assert.That(result.Repository.Metadata.Name, Is.EqualTo("FIX.Latest"));
            Assert.That(result.Repository.Metadata.ExtensionPack, Is.EqualTo(309));
            Assert.That(result.Repository.Messages, Has.Count.EqualTo(181));
            Assert.That(result.Repository.Fields, Has.Count.EqualTo(6203));
        });
    }

    [Test]
    public void Version11Rc3RepositoryReadsThroughThePublicApi()
    {
        string xml = """
            <repository xmlns="http://fixprotocol.io/2026/orchestra/repository"
                        xmlns:dcterms="http://purl.org/dc/terms/"
                        name="Example" version="1.1">
              <metadata><dcterms:title>Example repository</dcterms:title></metadata>
              <datatypes><datatype name="String"/></datatypes>
              <codeSets/>
              <fields><field id="1" name="OrderId" type="String"/></fields>
            </repository>
            """;

        OrchestraReadResult result = Read(xml, new OrchestraReadOptions { BaseUri = "repository-v11-rc3.xml" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Codes(result));
            Assert.That(result.Repository, Is.Not.Null);
            Assert.That(result.Repository!.Version, Is.EqualTo(OrchestraVersion.Version11));
            Assert.That(result.Repository.Fields.Single().Name, Is.EqualTo("OrderId"));
        });
    }

    [TestCase(OrchestraXmlLoader.InterfacesVersion10Namespace, OrchestraVersion.Version10)]
    [TestCase(OrchestraXmlLoader.InterfacesVersion11Namespace, OrchestraVersion.Version11)]
    public void InterfacesFixturesReadThroughThePublicApi(string namespaceUri, OrchestraVersion version)
    {
        string xml = $"""
            <interfaces xmlns="{namespaceUri}" xmlns:dcterms="http://purl.org/dc/terms/">
              <metadata><dcterms:title>Venue services</dcterms:title></metadata>
              <interface name="Trading">
                <transport name="TCP" address="fix.example.test:9876"/>
              </interface>
            </interfaces>
            """;

        OrchestraReadResult result = Read(xml, new OrchestraReadOptions { BaseUri = "interfaces.xml" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Codes(result));
            Assert.That(result.Repository, Is.Not.Null);
            Assert.That(result.Repository!.Version, Is.EqualTo(version));
            Assert.That(result.Repository.Interfaces.Single().Name, Is.EqualTo("Trading"));
            Assert.That(result.Repository.Interfaces.Single().Transports.Single().Address, Is.EqualTo("fix.example.test:9876"));
        });
    }

    [Test]
    public void XIncludeRepositoryReadsIncludedDefinitions()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "fields.xml"), """
                <fields xmlns="http://fixprotocol.io/2026/orchestra/repository">
                  <field id="1" name="IncludedField" type="String"/>
                </fields>
                """);
            string xml = """
                <repository xmlns="http://fixprotocol.io/2026/orchestra/repository"
                            xmlns:dcterms="http://purl.org/dc/terms/"
                            xmlns:xi="http://www.w3.org/2001/XInclude"
                            name="Included" version="1.1">
                  <metadata><dcterms:title>Included repository</dcterms:title></metadata>
                  <datatypes><datatype name="String"/></datatypes>
                  <codeSets/>
                  <xi:include href="fields.xml"/>
                </repository>
                """;

            OrchestraReadResult result = Read(xml, new OrchestraReadOptions { BaseUri = directory, ResolveXInclude = true, MaxSourceSize = 8192 });

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True, Codes(result));
                Assert.That(result.Repository, Is.Not.Null);
                Assert.That(result.Repository!.Fields.Single().Name, Is.EqualTo("IncludedField"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestCase("<repository xmlns=\"http://fixprotocol.io/2026/orchestra/repository\"><fields></repository>", "invalid-orchestra-xml")]
    [TestCase("<!DOCTYPE repository [<!ENTITY secret SYSTEM 'file:///etc/passwd'>]><repository xmlns=\"http://fixprotocol.io/2026/orchestra/repository\">&secret;</repository>", "external-entities-prohibited")]
    public void InvalidAndUnsafeXmlReturnDiagnostics(string xml, string expectedCode)
    {
        OrchestraReadResult result = Read(xml, new OrchestraReadOptions { BaseUri = "unsafe-or-invalid.xml" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Repository, Is.Null);
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain(expectedCode));
            Assert.That(result.Diagnostics.Single(item => item.Code == expectedCode).Source, Is.EqualTo("unsafe-or-invalid.xml"));
        });
    }

    [Test]
    public void UnknownExtensionsSurviveAlongsideLocatedDiagnostics()
    {
        string xml = """
            <repository xmlns="http://fixprotocol.io/2026/orchestra/repository"
                        xmlns:venue="urn:venue:acceptance"
                        name="Venue" version="1.1">
              <datatypes/>
              <codeSets/>
              <fields>
                <field id="1" name="BrokenField" type="MissingType"/>
              </fields>
              <venue:settings mode="strict"/>
            </repository>
            """;

        OrchestraReadResult result = Read(xml, new OrchestraReadOptions { BaseUri = "located-extension.xml", ValidateSchema = false });
        OrchestraDiagnostic diagnostic = result.Diagnostics.Single(item => item.Code == "unresolved-field-type");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Repository, Is.Not.Null);
            Assert.That(result.Repository!.Extensions.Single().NamespaceUri, Is.EqualTo("urn:venue:acceptance"));
            Assert.That(result.Repository.Extensions.Single().Xml, Does.Contain("mode=\"strict\""));
            Assert.That(diagnostic.Source, Is.EqualTo("located-extension.xml"));
            Assert.That(diagnostic.Line, Is.EqualTo(7));
            Assert.That(diagnostic.Column, Is.GreaterThan(0));
            Assert.That(diagnostic.ElementPath, Is.EqualTo("/repository/fields/field"));
        });
    }

    private static XmlSchema ReadSchema(string path)
    {
        using XmlReader reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        return XmlSchema.Read(reader, null)!;
    }

    private static OrchestraReadResult Read(string xml, OrchestraReadOptions options)
    {
        using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new OrchestraReader().ReadRepository(source, options);
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "fix-orchestra-acceptance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string Codes(OrchestraReadResult result)
    {
        return string.Join(Environment.NewLine, result.Diagnostics.Select(item => item.Code + ": " + item.Message));
    }
}
