using System;
using System.IO;
using System.Text;
using System.Xml;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class OrchestraXmlLoaderTests
{
    [Test]
    public void OfficialVersion10RepositoryLoadsWithinDefaultSecurityLimits()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../specs/fix/FIXOrchestra_FIX.Latest_EP309/FIXOrchestra_FIX.Latest_EP309.xml"));
        using FileStream source = File.OpenRead(path);

        OrchestraXmlLoadResult result = OrchestraXmlLoader.LoadDocument(source, new OrchestraReadOptions { BaseUri = path });
        OrchestraXmlSourceLocation? location = result.GetLocation(result.Root!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Codes(result));
            Assert.That(result.Version, Is.EqualTo(OrchestraVersion.Version10));
            Assert.That(result.InterfacesDocument, Is.False);
            Assert.That(result.Root!.GetAttribute("name"), Is.EqualTo("FIX.Latest"));
            Assert.That(location, Is.Not.Null);
            Assert.That(location!.Source, Is.EqualTo(path));
            Assert.That(location.Line, Is.EqualTo(2));
        });
    }

    [TestCase("repository", OrchestraXmlLoader.RepositoryVersion10Namespace, OrchestraVersion.Version10, false)]
    [TestCase("repository", OrchestraXmlLoader.RepositoryVersion11Namespace, OrchestraVersion.Version11, false)]
    [TestCase("interfaces", OrchestraXmlLoader.InterfacesVersion10Namespace, OrchestraVersion.Version10, true)]
    [TestCase("interfaces", OrchestraXmlLoader.InterfacesVersion11Namespace, OrchestraVersion.Version11, true)]
    public void SupportedNamespacesDispatchDocumentKindAndVersion(string root, string namespaceUri, OrchestraVersion version, bool interfacesDocument)
    {
        string xml = $"<{root} xmlns=\"{namespaceUri}\"><metadata/></{root}>";

        OrchestraXmlLoadResult result = Load(xml, new OrchestraReadOptions { BaseUri = "memory.xml" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Codes(result));
            Assert.That(result.Root, Is.Not.Null);
            Assert.That(result.Root!.LocalName, Is.EqualTo(root));
            Assert.That(result.NamespaceUri, Is.EqualTo(namespaceUri));
            Assert.That(result.Version, Is.EqualTo(version));
            Assert.That(result.InterfacesDocument, Is.EqualTo(interfacesDocument));
            Assert.That(result.Document, Is.Not.Null);
        });
    }

    [Test]
    public void UnsupportedNamespaceAndMismatchedRootHaveLocatedDiagnostics()
    {
        OrchestraXmlLoadResult unsupported = Load("\n<repository xmlns=\"urn:unsupported\"/>", new OrchestraReadOptions { BaseUri = "unsupported.xml" });
        OrchestraXmlLoadResult mismatched = Load("\n\n<interfaces xmlns=\"http://fixprotocol.io/2026/orchestra/repository\"/>", new OrchestraReadOptions { BaseUri = "wrong-root.xml" });

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Success, Is.False);
            AssertDiagnostic(unsupported.Diagnostics[0], "unsupported-orchestra-namespace", "unsupported.xml", 2, Is.EqualTo(2), "/repository");
            Assert.That(mismatched.Success, Is.False);
            AssertDiagnostic(mismatched.Diagnostics[0], "invalid-orchestra-root", "wrong-root.xml", 3, Is.EqualTo(2), "/interfaces");
        });
    }

    [Test]
    public void DtdEntitiesAndExternalEntityOptInAreRejectedBeforeParsing()
    {
        string entityXml = "<!DOCTYPE repository [<!ENTITY secret SYSTEM 'file:///etc/passwd'>]><repository xmlns=\"http://fixprotocol.io/2020/orchestra/repository\">&secret;</repository>";
        OrchestraXmlLoadResult dtd = Load(entityXml, new OrchestraReadOptions());
        OrchestraXmlLoadResult optedIn = Load("<repository xmlns=\"http://fixprotocol.io/2020/orchestra/repository\"/>", new OrchestraReadOptions { AllowExternalEntities = true });

        Assert.Multiple(() =>
        {
            Assert.That(dtd.Success, Is.False);
            Assert.That(dtd.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(dtd.Diagnostics[0].Code, Is.EqualTo("external-entities-prohibited"));
            Assert.That(optedIn.Success, Is.False);
            Assert.That(optedIn.Diagnostics.Exists(item => item.Code == "external-entities-prohibited"), Is.True);
        });
    }

    [Test]
    public void ByteAndElementDepthLimitsFailWithStableDiagnostics()
    {
        string xml = "<repository xmlns=\"http://fixprotocol.io/2020/orchestra/repository\"><a><b/></a></repository>";
        OrchestraXmlLoadResult bytes = Load(xml, new OrchestraReadOptions { MaxSourceSize = 20 });
        OrchestraXmlLoadResult depth = Load(xml, new OrchestraReadOptions { MaxSourceSize = 1024, MaxXmlDepth = 2, BaseUri = "deep.xml" });
        OrchestraXmlLoadResult badOptions = Load(xml, new OrchestraReadOptions { MaxSourceSize = 0, MaxXmlDepth = 0 });

        Assert.Multiple(() =>
        {
            Assert.That(bytes.Success, Is.False);
            Assert.That(bytes.Diagnostics[0].Code, Is.EqualTo("orchestra-source-size-exceeded"));
            Assert.That(depth.Success, Is.False);
            AssertDiagnostic(depth.Diagnostics[0], "orchestra-xml-depth-exceeded", "deep.xml", 1, Is.GreaterThan(1), "");
            Assert.That(badOptions.Success, Is.False);
            Assert.That(badOptions.Diagnostics.ConvertAll(item => item.Code), Is.EqualTo(new[] { "invalid-orchestra-source-limit", "invalid-orchestra-depth-limit" }));
        });
    }

    [Test]
    public void ElementLocationsAndCanonicalPathsAreRetained()
    {
        string xml = """
            <repository xmlns="http://fixprotocol.io/2026/orchestra/repository">
              <fields>
                <field id="1"/>
                <field id="2"/>
              </fields>
            </repository>
            """;
        OrchestraXmlLoadResult result = Load(xml, new OrchestraReadOptions { BaseUri = "locations.xml" });
        XmlElement second = (XmlElement)result.Root!.SelectNodes("*[local-name()='fields']/*[local-name()='field']")![1]!;
        OrchestraXmlSourceLocation? location = result.GetLocation(second);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Codes(result));
            Assert.That(location, Is.Not.Null);
            Assert.That(location!.Source, Is.EqualTo("locations.xml"));
            Assert.That(location.Line, Is.EqualTo(4));
            Assert.That(location.Column, Is.GreaterThan(1));
            Assert.That(OrchestraXmlLoader.CreateElementPath(second), Is.EqualTo("/repository/fields/field[2]"));
        });
    }

    [Test]
    public void XIncludeIsDisabledByDefaultAndResolvesLocalXmlWhenExplicitlyEnabled()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            string includePath = Path.Combine(directory, "fields.xml");
            File.WriteAllText(includePath, """
                <fields xmlns="http://fixprotocol.io/2026/orchestra/repository">
                  <field id="11"/>
                </fields>
                """);
            string xml = """
                <repository xmlns="http://fixprotocol.io/2026/orchestra/repository" xmlns:xi="http://www.w3.org/2001/XInclude">
                  <xi:include href="fields.xml"/>
                </repository>
                """;
            OrchestraXmlLoadResult disabled = Load(xml, new OrchestraReadOptions { BaseUri = directory });
            OrchestraXmlLoadResult enabled = Load(xml, new OrchestraReadOptions { ResolveXInclude = true, BaseUri = directory, MaxSourceSize = 4096 });
            XmlElement included = (XmlElement)enabled.Root!.SelectSingleNode("*[local-name()='fields']")!;
            XmlElement field = (XmlElement)included.SelectSingleNode("*[local-name()='field']")!;
            OrchestraXmlSourceLocation? includedLocation = enabled.GetLocation(field);

            Assert.Multiple(() =>
            {
                Assert.That(disabled.Success, Is.True, Codes(disabled));
                Assert.That(((XmlElement)disabled.Root!.SelectSingleNode("*[local-name()='include']")!).NamespaceURI, Is.EqualTo(OrchestraXmlLoader.XIncludeNamespace));
                Assert.That(enabled.Success, Is.True, Codes(enabled));
                Assert.That(included.LocalName, Is.EqualTo("fields"));
                Assert.That(field.LocalName, Is.EqualTo("field"));
                Assert.That(field.GetAttribute("id"), Is.EqualTo("11"));
                Assert.That(includedLocation, Is.Not.Null);
                Assert.That(includedLocation!.Source, Is.EqualTo(includePath));
                Assert.That(includedLocation.Line, Is.EqualTo(2));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestCase("../outside.xml", "xinclude-outside-base-uri")]
    [TestCase("https://example.com/fields.xml", "invalid-xinclude-reference")]
    [TestCase("fields.xml#part", "invalid-xinclude-reference")]
    [TestCase("missing.xml", "xinclude-source-not-found")]
    public void XIncludeRejectsUncontrolledReferences(string href, string expectedCode)
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            string xml = $"<repository xmlns=\"http://fixprotocol.io/2026/orchestra/repository\" xmlns:xi=\"http://www.w3.org/2001/XInclude\"><xi:include href=\"{href}\"/></repository>";

            OrchestraXmlLoadResult result = Load(xml, new OrchestraReadOptions { ResolveXInclude = true, BaseUri = directory, MaxSourceSize = 2048 });

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(expectedCode));
                Assert.That(result.Diagnostics[0].ElementPath, Is.EqualTo("/repository/include"));
                Assert.That(result.Root!.FirstChild!.NamespaceURI, Is.EqualTo(OrchestraXmlLoader.XIncludeNamespace));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void IncludesShareTheAggregateByteAndCombinedDepthLimits()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "large.xml"), $"<fields xmlns=\"{OrchestraXmlLoader.RepositoryVersion11Namespace}\">{new string('x', 300)}</fields>");
            File.WriteAllText(Path.Combine(directory, "deep.xml"), $"<one xmlns=\"{OrchestraXmlLoader.RepositoryVersion11Namespace}\"><two><three/></two></one>");
            string largeRoot = $"<repository xmlns=\"{OrchestraXmlLoader.RepositoryVersion11Namespace}\" xmlns:xi=\"{OrchestraXmlLoader.XIncludeNamespace}\"><xi:include href=\"large.xml\"/></repository>";
            string deepRoot = $"<repository xmlns=\"{OrchestraXmlLoader.RepositoryVersion11Namespace}\" xmlns:xi=\"{OrchestraXmlLoader.XIncludeNamespace}\"><container><xi:include href=\"deep.xml\"/></container></repository>";

            OrchestraXmlLoadResult size = Load(largeRoot, new OrchestraReadOptions { ResolveXInclude = true, BaseUri = directory, MaxSourceSize = Encoding.UTF8.GetByteCount(largeRoot) + 100 });
            OrchestraXmlLoadResult depth = Load(deepRoot, new OrchestraReadOptions { ResolveXInclude = true, BaseUri = directory, MaxSourceSize = 2048, MaxXmlDepth = 4 });

            Assert.Multiple(() =>
            {
                Assert.That(size.Success, Is.False);
                Assert.That(size.Diagnostics.Exists(item => item.Code == "orchestra-source-size-exceeded"), Is.True);
                Assert.That(depth.Success, Is.False);
                Assert.That(depth.Diagnostics.Exists(item => item.Code == "orchestra-xml-depth-exceeded"), Is.True);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static OrchestraXmlLoadResult Load(string xml, OrchestraReadOptions options)
    {
        using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return OrchestraXmlLoader.LoadDocument(source, options);
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "fix-orchestra-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string Codes(OrchestraXmlLoadResult result)
    {
        return string.Join(",", result.Diagnostics.ConvertAll(item => item.Code + ":" + item.Message));
    }

    private static void AssertDiagnostic(OrchestraDiagnostic diagnostic, string code, string source, int line, NUnit.Framework.Constraints.IResolveConstraint column, string path)
    {
        Assert.That(diagnostic.Code, Is.EqualTo(code));
        Assert.That(diagnostic.Source, Is.EqualTo(source));
        Assert.That(diagnostic.Line, Is.EqualTo(line));
        Assert.That(diagnostic.Column, column);
        Assert.That(diagnostic.ElementPath, Is.EqualTo(path));
    }
}
