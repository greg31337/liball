using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class OrchestraSchemaValidatorTests
{
    [TestCase(OrchestraVersion.Version10, false, OrchestraXmlLoader.RepositoryVersion10Namespace, "repository")]
    [TestCase(OrchestraVersion.Version10, true, OrchestraXmlLoader.InterfacesVersion10Namespace, "interfaces")]
    [TestCase(OrchestraVersion.Version11, false, OrchestraXmlLoader.RepositoryVersion11Namespace, "repository")]
    [TestCase(OrchestraVersion.Version11, true, OrchestraXmlLoader.InterfacesVersion11Namespace, "interfaces")]
    public void BuiltInCatalogCompilesEachSupportedDocumentSchema(OrchestraVersion version, bool interfacesDocument, string namespaceUri, string rootName)
    {
        XmlSchemaSet schemas = OrchestraSchemaCatalog.GetSchemas(version, interfacesDocument);

        Assert.Multiple(() =>
        {
            Assert.That(schemas.IsCompiled, Is.True);
            Assert.That(schemas.GlobalElements.Contains(new XmlQualifiedName(rootName, namespaceUri)), Is.True);
        });
    }

    [Test]
    public void OfficialVersion10RepositoryValidatesAgainstBuiltInSchema()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../specs/fix/FIXOrchestra_FIX.Latest_EP309/FIXOrchestra_FIX.Latest_EP309.xml"));
        OrchestraXmlLoadResult result;
        using (FileStream source = File.OpenRead(path))
        {
            result = OrchestraXmlLoader.LoadDocument(source, new OrchestraReadOptions { BaseUri = path });
        }

        OrchestraSchemaValidator.Validate(result);

        Assert.That(result.Success, Is.True, Codes(result));
    }

    [TestCase(OrchestraXmlLoader.InterfacesVersion10Namespace, "interfaces-v10.xml")]
    [TestCase(OrchestraXmlLoader.InterfacesVersion11Namespace, "interfaces-v11.xml")]
    public void InterfacesVersionsValidateRequiredMetadata(string namespaceUri, string sourceName)
    {
        string xml = $"<interfaces xmlns=\"{namespaceUri}\" xmlns:dcterms=\"{OrchestraSchemaCatalog.DublinCoreTermsNamespace}\"><metadata><dcterms:title>Venue services</dcterms:title></metadata></interfaces>";

        OrchestraXmlLoadResult result = Validate(xml, sourceName);

        Assert.That(result.Success, Is.True, Codes(result));
    }

    [Test]
    public void Version11RepositoryValidatesMinimalDocument()
    {
        string xml = $"<repository xmlns=\"{OrchestraXmlLoader.RepositoryVersion11Namespace}\" xmlns:dcterms=\"{OrchestraSchemaCatalog.DublinCoreTermsNamespace}\" name=\"Example\" version=\"1.1\"><metadata><dcterms:title>Example repository</dcterms:title></metadata><datatypes/><codeSets/><fields/></repository>";

        OrchestraXmlLoadResult result = Validate(xml, "repository-v11.xml");

        Assert.That(result.Success, Is.True, Codes(result));
    }

    [TestCase(OrchestraXmlLoader.InterfacesVersion10Namespace)]
    [TestCase(OrchestraXmlLoader.InterfacesVersion11Namespace)]
    public void InvalidInterfacesContentProducesLocatedSchemaDiagnostic(string namespaceUri)
    {
        string xml = $"\n<interfaces xmlns=\"{namespaceUri}\"><unexpected/></interfaces>";

        OrchestraXmlLoadResult result = Validate(xml, "invalid-interfaces.xml");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Is.Not.Empty);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo("orchestra-schema-validation-error"));
            Assert.That(result.Diagnostics[0].Source, Is.EqualTo("invalid-interfaces.xml"));
            Assert.That(result.Diagnostics[0].Line, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].ElementPath, Does.StartWith("/interfaces"));
        });
    }

    [Test]
    public void Version11RepositoryRejectsMissingRequiredStructureAndAttributes()
    {
        string xml = $"<repository xmlns=\"{OrchestraXmlLoader.RepositoryVersion11Namespace}\"/>";

        OrchestraXmlLoadResult result = Validate(xml, "invalid-repository.xml");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(result.Diagnostics.TrueForAll(item => item.Code == "orchestra-schema-validation-error"), Is.True);
            Assert.That(result.Diagnostics.TrueForAll(item => item.Source == "invalid-repository.xml"), Is.True);
            Assert.That(result.Diagnostics.TrueForAll(item => item.ElementPath == "/repository"), Is.True);
        });
    }

    [Test]
    public void Version11RepositoryEnforcesRootScopedKeyReferences()
    {
        string xml = $"<repository xmlns=\"{OrchestraXmlLoader.RepositoryVersion11Namespace}\" xmlns:dcterms=\"{OrchestraSchemaCatalog.DublinCoreTermsNamespace}\" name=\"Example\" version=\"1.1\"><metadata><dcterms:title>Example repository</dcterms:title></metadata><datatypes/><codeSets/><fields><field id=\"1\" name=\"OrderId\" type=\"MissingType\"/></fields></repository>";

        OrchestraXmlLoadResult result = Validate(xml, "invalid-reference.xml");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Exists(item => item.Code == "orchestra-schema-validation-error"), Is.True);
            Assert.That(result.Diagnostics.Exists(item => item.Message.Contains("MissingType")), Is.True);
        });
    }

    private static OrchestraXmlLoadResult Validate(string xml, string sourceName)
    {
        using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        OrchestraXmlLoadResult result = OrchestraXmlLoader.LoadDocument(source, new OrchestraReadOptions { BaseUri = sourceName });
        return OrchestraSchemaValidator.Validate(result);
    }

    private static string Codes(OrchestraXmlLoadResult result)
    {
        return string.Join(" | ", result.Diagnostics.ConvertAll(item => item.Code + ":" + item.Message));
    }
}
