using System.Text;
using iso20022;

namespace iso20022.Tests;

public class SchemaDependencyResolverTests
{
    [Test]
    public void RegistryFileImportReturnsAndCompilesTheResolvedOfflineClosure()
    {
        string root = CreateTemporaryRoot();
        string mainPath = Path.Combine(root, "main.xsd");
        string commonPath = Path.Combine(root, "common.xsd");
        string dependencyPath = Path.Combine(root, "dependency.xsd");
        File.WriteAllText(mainPath, CreateMainSchema(
            """
              <xs:include schemaLocation="common.xsd"/>
              <xs:import namespace="urn:example:dependency" schemaLocation="dependency.xsd"/>
            """));
        File.WriteAllText(commonPath, CreateSimpleSchema(string.Empty, "CommonText"));
        File.WriteAllText(dependencyPath, CreateSimpleSchema(
            "urn:example:dependency",
            "DependencyText"));
        try
        {
            ISchemaRegistry registry = new SchemaRegistry();
            SchemaRegistryOptions options = new SchemaRegistryOptions
            {
                LocalRootPath = root,
                CompileOnRegistration = true
            };

            SchemaCompilationResult registered = registry.ImportRegisterSchemaFile(
                mainPath,
                CreateSchemaRequest("urn:example:main", mainPath),
                options);
            options.CompileOnRegistration = false;
            SchemaCompilationResult cached = registry.CompileSchema(
                CreateSelector("urn:example:main", "1", "base"),
                options);

            Assert.Multiple(() =>
            {
                Assert.That(registered.Success, Is.True, registered.ErrorMessage);
                Assert.That(registered.Registered, Is.True);
                Assert.That(registered.CompiledSchemas?.IsCompiled, Is.True);
                Assert.That(registered.Schemas.Select(item => item.NamespaceUri),
                    Is.EqualTo(new[]
                    {
                        "urn:example:main",
                        "urn:example:main",
                        "urn:example:dependency"
                    }));
                Assert.That(registered.Schemas.Select(item => item.SourceName),
                    Is.EqualTo(new[] { "main.xsd", "common.xsd", "dependency.xsd" }));
                Assert.That(cached.Success, Is.True, cached.ErrorMessage);
                Assert.That(cached.ReusedCachedCompilation, Is.True);
                Assert.That(cached.CompiledSchemas, Is.SameAs(registered.CompiledSchemas));
            });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void FileImportNormalizesAndReusesLocalIncludeImportClosure()
    {
        string root = CreateTemporaryRoot();
        string mainPath = Path.Combine(root, "main.xsd");
        string commonPath = Path.Combine(root, "common.xsd");
        string importDirectory = Path.Combine(root, "imports");
        string importedPath = Path.Combine(importDirectory, "dependency.xsd");
        Directory.CreateDirectory(importDirectory);
        File.WriteAllText(mainPath, CreateMainSchema(
            """
              <xs:include schemaLocation="parts/../common.xsd"/>
              <xs:include schemaLocation="./common.xsd"/>
              <xs:import namespace="urn:example:dependency" schemaLocation="imports/dependency.xsd"/>
            """));
        File.WriteAllText(commonPath, CreateSimpleSchema(string.Empty, "CommonText"));
        File.WriteAllText(importedPath, CreateSimpleSchema(
            "urn:example:dependency",
            "DependencyText"));
        try
        {
            SchemaRegistryOptions options = new SchemaRegistryOptions
            {
                LocalRootPath = root
            };
            ISchemaImporter importer = new SchemaImporter();

            SchemaCompilationResult result = importer.ImportSchemaFile(
                mainPath,
                CreateSchemaRequest("urn:example:main", mainPath),
                options);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True, result.ErrorMessage);
                Assert.That(result.Schemas, Has.Count.EqualTo(3));
                Assert.That(result.Schemas.Select(item => item.NamespaceUri),
                    Is.EqualTo(new[]
                    {
                        "urn:example:main",
                        "urn:example:main",
                        "urn:example:dependency"
                    }));
                Assert.That(result.Schemas[1].DataTypes.Single().Name,
                    Is.EqualTo("CommonText"));
                Assert.That(result.Schemas[1].BaseUri,
                    Is.EqualTo(new Uri(commonPath).AbsoluteUri));
                Assert.That(result.Schemas[2].DataTypes.Single().Name,
                    Is.EqualTo("DependencyText"));
                Assert.That(result.Schemas[2].BaseUri,
                    Is.EqualTo(new Uri(importedPath).AbsoluteUri));
                Assert.That(result.RootSchema?.IncludeLocations,
                    Is.EqualTo(new[] { "parts/../common.xsd", "./common.xsd" }));
            });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void ResolutionRejectsTraversalRemoteLocationsAndCycles()
    {
        string parent = CreateTemporaryRoot();
        string root = Path.Combine(parent, "schemas");
        Directory.CreateDirectory(root);
        string outsidePath = Path.Combine(parent, "outside.xsd");
        File.WriteAllText(outsidePath, CreateSimpleSchema("urn:example:main", "Outside"));
        SchemaRegistryOptions options = new SchemaRegistryOptions
        {
            LocalRootPath = root
        };
        ISchemaImporter importer = new SchemaImporter();
        SchemaArtifact streamRequest = CreateSchemaRequest(
            "urn:example:main",
            Path.Combine(root, "main.xsd"));

        SchemaCompilationResult traversal = importer.ImportSchemaStream(
            new MemoryStream(Encoding.UTF8.GetBytes(CreateMainSchema(
                "<xs:include schemaLocation=\"../outside.xsd\"/>"))),
            streamRequest,
            options);
        SchemaCompilationResult remote = importer.ImportSchemaStream(
            new MemoryStream(Encoding.UTF8.GetBytes(CreateMainSchema(
                "<xs:import namespace=\"urn:example:remote\" schemaLocation=\"https://example.test/remote.xsd\"/>"))),
            streamRequest,
            options);

        string firstPath = Path.Combine(root, "first.xsd");
        string secondPath = Path.Combine(root, "second.xsd");
        File.WriteAllText(firstPath, CreateMainSchema(
            "<xs:include schemaLocation=\"second.xsd\"/>"));
        File.WriteAllText(secondPath, CreateMainSchema(
            "<xs:include schemaLocation=\"first.xsd\"/>"));
        SchemaCompilationResult cycle = importer.ImportSchemaFile(
            firstPath,
            CreateSchemaRequest("urn:example:main", firstPath),
            options);

        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(traversal.Success, Is.False);
                Assert.That(traversal.ErrorCode, Is.EqualTo("schema.resolve.traversal"));
                Assert.That(remote.Success, Is.False);
                Assert.That(remote.ErrorCode, Is.EqualTo("schema.resolve.local_only"));
                Assert.That(cycle.Success, Is.False);
                Assert.That(cycle.ErrorCode, Is.EqualTo("schema.resolve.cycle"));
                Assert.That(cycle.Schemas, Has.Count.EqualTo(2));
            });
        }
        finally
        {
            Directory.Delete(parent, true);
        }
    }

    [Test]
    public void NamespaceOnlyImportsUseUniqueHighestPrecedenceAndReportTies()
    {
        string root = CreateTemporaryRoot();
        try
        {
            SchemaArtifact low = CreateLocalSchema(
                root,
                "low.xsd",
                "urn:example:dependency",
                "LowType",
                10);
            SchemaArtifact high = CreateLocalSchema(
                root,
                "high.xsd",
                "urn:example:dependency",
                "HighType",
                20);
            SchemaRegistryOptions options = new SchemaRegistryOptions
            {
                LocalRootPath = root
            };
            options.LocalSchemas.Add(low);
            options.LocalSchemas.Add(high);
            ISchemaImporter importer = new SchemaImporter();
            byte[] main = Encoding.UTF8.GetBytes(CreateMainSchema(
                "<xs:import namespace=\"urn:example:dependency\"/>"));
            SchemaArtifact request = CreateSchemaRequest(
                "urn:example:main",
                Path.Combine(root, "main.xsd"));

            SchemaCompilationResult selected = importer.ImportSchemaStream(
                new MemoryStream(main),
                request,
                options);

            SchemaArtifact tie = CreateLocalSchema(
                root,
                "tie.xsd",
                "urn:example:dependency",
                "TieType",
                20);
            SchemaRegistryOptions tieOptions = new SchemaRegistryOptions
            {
                LocalRootPath = root
            };
            tieOptions.LocalSchemas.Add(high);
            tieOptions.LocalSchemas.Add(tie);
            SchemaCompilationResult collision = importer.ImportSchemaStream(
                new MemoryStream(main),
                request,
                tieOptions);

            Assert.Multiple(() =>
            {
                Assert.That(selected.Success, Is.True, selected.ErrorMessage);
                Assert.That(selected.Schemas, Has.Count.EqualTo(2));
                Assert.That(selected.Schemas[1].DataTypes.Single().Name,
                    Is.EqualTo("HighType"));
                Assert.That(selected.Schemas[1].Precedence, Is.EqualTo(20));
                Assert.That(selected.DiagnosticCodes,
                    Does.Contain("schema.resolve.precedence_selected"));
                Assert.That(collision.Success, Is.False);
                Assert.That(collision.ErrorCode,
                    Is.EqualTo("schema.resolve.precedence_collision"));
                Assert.That(collision.Schemas, Has.Count.EqualTo(1));
            });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static SchemaArtifact CreateLocalSchema(
        string root,
        string fileName,
        string namespaceUri,
        string typeName,
        int precedence)
    {
        SchemaArtifact schema = CreateSchemaRequest(namespaceUri, Path.Combine(root, fileName));
        schema.Content = Encoding.UTF8.GetBytes(CreateSimpleSchema(namespaceUri, typeName));
        schema.Precedence = precedence;
        return schema;
    }

    private static SchemaArtifact CreateSchemaRequest(string namespaceUri, string path)
    {
        return new SchemaArtifact
        {
            Artifact = new ArtifactManifest
            {
                Coordinate = new ArtifactCoordinate
                {
                    Kind = ArtifactKind.Schema,
                    Edition = "2026",
                    Identifier = Path.GetFileNameWithoutExtension(path),
                    Version = "1"
                },
                OriginUri = new Uri(path).AbsoluteUri,
                IsApproved = true
            },
            SourceName = Path.GetFileName(path),
            BaseUri = new Uri(path).AbsoluteUri,
            NamespaceUri = namespaceUri,
            Version = "1",
            Profile = "base",
            IsEntryPoint = true
        };
    }

    private static SchemaArtifact CreateSelector(
        string namespaceUri,
        string version,
        string profile)
    {
        return new SchemaArtifact
        {
            NamespaceUri = namespaceUri,
            Version = version,
            Profile = profile
        };
    }

    private static string CreateMainSchema(string dependencies)
    {
        return """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
              targetNamespace="urn:example:main">
            """ + dependencies + """
              <xs:element name="Document" type="xs:string"/>
            </xs:schema>
            """;
    }

    private static string CreateSimpleSchema(string namespaceUri, string typeName)
    {
        string targetNamespace = namespaceUri.Length == 0
            ? string.Empty
            : " targetNamespace=\"" + namespaceUri + "\"";
        return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"" +
            targetNamespace + "><xs:simpleType name=\"" + typeName +
            "\"><xs:restriction base=\"xs:string\"/></xs:simpleType></xs:schema>";
    }

    private static string CreateTemporaryRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "iso20022-schema-resolution-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
