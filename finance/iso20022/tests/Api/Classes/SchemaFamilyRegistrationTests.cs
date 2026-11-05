using System.IO.Compression;
using System.Text;
using iso20022;

namespace iso20022.Tests;

public class SchemaFamilyRegistrationTests
{
    [Test]
    public void CallerSuppliedFamiliesUseTheSameStreamRegistryPath()
    {
        string root = CreateTemporaryRoot();
        try
        {
            string[] identifiers =
            {
                "head.001.001.01",
                "head.001.001.02",
                "head.001.001.03",
                "head.001.001.04",
                "supl.001.001.01",
                "pacs.008.002.10",
                "camt.053.001.13"
            };
            string[] rootElements =
            {
                "AppHdr",
                "AppHdr",
                "AppHdr",
                "AppHdr",
                "Document",
                "Document",
                "Document"
            };
            ISchemaRegistry registry = new SchemaRegistry();
            SchemaRegistryOptions options = CreateOptions(root);

            int index = 0;
            while (index < identifiers.Length)
            {
                string identifier = identifiers[index];
                string namespaceUri = Iso20022Namespaces.SchemaUrnPrefix + identifier;
                SchemaArtifact request = CreateRequest(
                    identifier,
                    root,
                    identifier + ".xsd",
                    new Uri(Path.Combine(root, identifier + ".xsd")).AbsoluteUri);
                string xsd = CreateSchema(namespaceUri, rootElements[index]);

                SchemaCompilationResult registered = registry.ImportRegisterSchemaStream(
                    new MemoryStream(Encoding.UTF8.GetBytes(xsd)),
                    request,
                    options);
                SchemaCompilationResult found = registry.LookupSchema(
                    CreateSelector(identifier),
                    options);

                Assert.Multiple(() =>
                {
                    Assert.That(registered.Success, Is.True,
                        identifier + ": " + registered.ErrorMessage);
                    Assert.That(registered.Registered, Is.True, identifier);
                    Assert.That(registered.Found, Is.True, identifier);
                    Assert.That(registered.RootSchema?.NamespaceUri,
                        Is.EqualTo(namespaceUri), identifier);
                    Assert.That(registered.RootSchema?.MessageIdentifier?.Value,
                        Is.EqualTo(identifier), identifier);
                    Assert.That(registered.RootSchema?.RootElement,
                        Is.EqualTo(rootElements[index]), identifier);
                    Assert.That(registered.RootSchema?.Artifact.Hash.Value,
                        Has.Length.EqualTo(32), identifier);
                    Assert.That(registered.RootSchema?.Artifact.MediaType,
                        Is.EqualTo(Iso20022MediaTypes.XmlSchema), identifier);
                    Assert.That(found.Found, Is.True, identifier);
                    Assert.That(found.RootSchema, Is.SameAs(registered.RootSchema), identifier);
                });

                index++;
            }
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void BundledCurrentBmeAndBfeArchivesUseTheSameRegistryPath()
    {
        string specificationRoot = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../specs/iso20022"));
        string root = CreateTemporaryRoot();
        try
        {
            string[] identifiers =
            {
                "nvlp.001.001.02",
                "nvlp.002.001.02"
            };
            string[] archiveNames =
            {
                "ISO20022_Business_Message_Envelope_V02.zip",
                "ISO20022_Business_File_Envelope_V02.zip"
            };
            string[] rootElements =
            {
                "BizMsgEnvlp",
                "BizFileEnvlp"
            };
            ISchemaRegistry registry = new SchemaRegistry();
            SchemaRegistryOptions options = CreateOptions(root);

            int index = 0;
            while (index < identifiers.Length)
            {
                string identifier = identifiers[index];
                string archivePath = Path.Combine(specificationRoot, archiveNames[index]);
                string entryName = identifier + ".xsd";
                SchemaArtifact request = CreateRequest(
                    identifier,
                    root,
                    entryName,
                    new Uri(archivePath).AbsoluteUri);
                SchemaCompilationResult registered;
                using (Stream input = File.OpenRead(archivePath))
                {
                    registered = registry.ImportRegisterSchemaArchive(
                        input,
                        request,
                        options);
                }

                SchemaCompilationResult found = registry.LookupSchema(
                    CreateSelector(identifier),
                    options);

                Assert.Multiple(() =>
                {
                    Assert.That(registered.Success, Is.True,
                        identifier + ": " + registered.ErrorMessage);
                    Assert.That(registered.Registered, Is.True, identifier);
                    Assert.That(registered.RootSchema?.NamespaceUri,
                        Is.EqualTo(Iso20022Namespaces.SchemaUrnPrefix + identifier), identifier);
                    Assert.That(registered.RootSchema?.MessageIdentifier?.Value,
                        Is.EqualTo(identifier), identifier);
                    Assert.That(registered.RootSchema?.RootElement,
                        Is.EqualTo(rootElements[index]), identifier);
                    Assert.That(registered.RootSchema?.SourceName,
                        Is.EqualTo(entryName), identifier);
                    Assert.That(registered.RootSchema?.Artifact.OriginUri,
                        Is.EqualTo(new Uri(archivePath).AbsoluteUri), identifier);
                    Assert.That(registered.RootSchema?.Artifact.Hash.Value,
                        Has.Length.EqualTo(32), identifier);
                    Assert.That(found.RootSchema, Is.SameAs(registered.RootSchema), identifier);
                });

                index++;
            }
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void FileRegistrationImportsBeforeApplyingRegistryPolicy()
    {
        string root = CreateTemporaryRoot();
        string identifier = "admi.004.001.02";
        string path = Path.Combine(root, identifier + ".xsd");
        File.WriteAllText(
            path,
            CreateSchema(Iso20022Namespaces.SchemaUrnPrefix + identifier, "Document"));
        try
        {
            ISchemaRegistry registry = new SchemaRegistry();
            SchemaRegistryOptions options = CreateOptions(root);
            SchemaArtifact request = CreateRequest(
                identifier,
                root,
                string.Empty,
                new Uri(path).AbsoluteUri);
            request.BaseUri = string.Empty;

            SchemaCompilationResult registered = registry.ImportRegisterSchemaFile(
                path,
                request,
                options);
            SchemaCompilationResult found = registry.LookupSchema(
                CreateSelector(identifier),
                options);

            Assert.Multiple(() =>
            {
                Assert.That(registered.Success, Is.True, registered.ErrorMessage);
                Assert.That(registered.Registered, Is.True);
                Assert.That(registered.RootSchema?.SourceName, Is.EqualTo(identifier + ".xsd"));
                Assert.That(registered.RootSchema?.BaseUri, Is.EqualTo(new Uri(path).AbsoluteUri));
                Assert.That(found.RootSchema, Is.SameAs(registered.RootSchema));
            });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void ArchiveRegistrationRequiresAnExactSafeXsdEntry()
    {
        string root = CreateTemporaryRoot();
        try
        {
            const string identifier = "pacs.008.001.10";
            ISchemaRegistry registry = new SchemaRegistry();
            SchemaRegistryOptions options = CreateOptions(root);
            SchemaArtifact missingRequest = CreateRequest(
                identifier,
                root,
                "missing.xsd",
                "https://example.test/schemas.zip");
            SchemaCompilationResult missing = registry.ImportRegisterSchemaArchive(
                CreateArchive("safe.xsd", CreateSchema(
                    Iso20022Namespaces.SchemaUrnPrefix + identifier,
                    "Document")),
                missingRequest,
                options);

            SchemaArtifact unsafeRequest = CreateRequest(
                identifier,
                root,
                "safe.xsd",
                "https://example.test/unsafe.zip");
            MemoryStream unsafeArchive = CreateArchive(
                "../unsafe.xsd",
                CreateSchema(Iso20022Namespaces.SchemaUrnPrefix + identifier, "Document"));
            SchemaCompilationResult unsafeResult = registry.ImportRegisterSchemaArchive(
                unsafeArchive,
                unsafeRequest,
                options);

            Assert.Multiple(() =>
            {
                Assert.That(missing.Success, Is.False);
                Assert.That(missing.ErrorCode,
                    Is.EqualTo("artifact.archive.entry_not_found"));
                Assert.That(unsafeResult.Success, Is.False);
                Assert.That(unsafeResult.ErrorCode,
                    Is.EqualTo("artifact.archive.path"));
            });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static SchemaArtifact CreateRequest(
        string identifier,
        string root,
        string sourceName,
        string originUri)
    {
        MessageIdentifier messageIdentifier = ParseIdentifier(identifier);
        return new SchemaArtifact
        {
            Artifact = new ArtifactManifest
            {
                Coordinate = new ArtifactCoordinate
                {
                    Kind = ArtifactKind.Schema,
                    Edition = "2026",
                    Identifier = identifier,
                    Version = messageIdentifier.Version
                },
                OriginUri = originUri,
                IsApproved = true
            },
            SourceName = sourceName,
            BaseUri = new Uri(Path.Combine(root, identifier + ".xsd")).AbsoluteUri,
            NamespaceUri = Iso20022Namespaces.SchemaUrnPrefix + identifier,
            MessageIdentifier = messageIdentifier,
            Version = messageIdentifier.Version,
            Profile = "iso",
            Precedence = 100,
            IsEntryPoint = true
        };
    }

    private static SchemaArtifact CreateSelector(string identifier)
    {
        MessageIdentifier messageIdentifier = ParseIdentifier(identifier);
        return new SchemaArtifact
        {
            NamespaceUri = Iso20022Namespaces.SchemaUrnPrefix + identifier,
            MessageIdentifier = messageIdentifier,
            Version = messageIdentifier.Version,
            Profile = "iso"
        };
    }

    private static MessageIdentifier ParseIdentifier(string identifier)
    {
        MessageIdentifierParseResult result =
            new MessageIdentifierParser().ParseIdentifier(identifier);
        return result.Identifier ?? throw new AssertionException(result.ErrorMessage);
    }

    private static SchemaRegistryOptions CreateOptions(string root)
    {
        return new SchemaRegistryOptions
        {
            LocalRootPath = root,
            ResolveIncludes = false,
            ResolveImports = false
        };
    }

    private static string CreateSchema(string namespaceUri, string rootElement)
    {
        return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"" +
            " targetNamespace=\"" + namespaceUri + "\"" +
            " elementFormDefault=\"qualified\">" +
            "<xs:element name=\"" + rootElement + "\" type=\"xs:string\"/>" +
            "</xs:schema>";
    }

    private static MemoryStream CreateArchive(string entryName, string content)
    {
        MemoryStream result = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(result, ZipArchiveMode.Create, true))
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName);
            using (StreamWriter writer = new StreamWriter(entry.Open(), Encoding.UTF8))
            {
                writer.Write(content);
            }
        }

        result.Position = 0;
        return result;
    }

    private static string CreateTemporaryRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "iso20022-schema-family-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
