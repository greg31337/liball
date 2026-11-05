using System.Text;
using iso20022;

namespace iso20022.Tests;

public class SchemaCompilerTests
{
    [Test]
    public void ResolvedIncludeImportClosureCompilesAndReusesExactCacheEntry()
    {
        string root = CreateTemporaryRoot();
        string mainPath = Path.Combine(root, "main.xsd");
        string commonPath = Path.Combine(root, "common.xsd");
        string dependencyPath = Path.Combine(root, "dependency.xsd");
        File.WriteAllText(mainPath, CreateClosureRootSchema());
        File.WriteAllText(commonPath, CreateCommonSchema());
        File.WriteAllText(dependencyPath, CreateDependencySchema());
        try
        {
            SchemaRegistryOptions options = new SchemaRegistryOptions
            {
                LocalRootPath = root
            };
            ISchemaImporter importer = new SchemaImporter();
            SchemaCompiler compiler = new SchemaCompiler();
            SchemaCompilationResult firstImport = importer.ImportSchemaFile(
                mainPath,
                CreateRequest("urn:example:main", "1", "base", mainPath),
                options);
            SchemaCompilationResult secondImport = importer.ImportSchemaFile(
                mainPath,
                CreateRequest("urn:example:main", "1", "base", mainPath),
                options);

            SchemaCompilationResult first = compiler.CompileClosure(firstImport, options);
            SchemaCompilationResult cached = compiler.CompileClosure(secondImport, options);

            Assert.Multiple(() =>
            {
                Assert.That(first.Success, Is.True, first.ErrorMessage);
                Assert.That(first.CompiledSchemas, Is.Not.Null);
                Assert.That(first.CompiledSchemas?.IsCompiled, Is.True);
                Assert.That(first.Schemas, Has.Count.EqualTo(3));
                Assert.That(first.ReusedCachedCompilation, Is.False);
                Assert.That(cached.Success, Is.True, cached.ErrorMessage);
                Assert.That(cached.ReusedCachedCompilation, Is.True);
                Assert.That(cached.CompiledSchemas, Is.SameAs(first.CompiledSchemas));
            });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void CacheKeysKeepNamespaceVersionAndProfileExact()
    {
        SchemaCompiler compiler = new SchemaCompiler();
        SchemaRegistryOptions options = new SchemaRegistryOptions
        {
            ResolveIncludes = false,
            ResolveImports = false
        };
        SchemaCompilationResult first = compiler.CompileClosure(
            ImportSimpleSchema("urn:example:one", "1", "base", options),
            options);
        SchemaCompilationResult same = compiler.CompileClosure(
            ImportSimpleSchema("urn:example:one", "1", "base", options),
            options);
        SchemaCompilationResult version = compiler.CompileClosure(
            ImportSimpleSchema("urn:example:one", "2", "base", options),
            options);
        SchemaCompilationResult profile = compiler.CompileClosure(
            ImportSimpleSchema("urn:example:one", "1", "market", options),
            options);
        SchemaCompilationResult schemaNamespace = compiler.CompileClosure(
            ImportSimpleSchema("urn:example:two", "1", "base", options),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, first.ErrorMessage);
            Assert.That(same.ReusedCachedCompilation, Is.True);
            Assert.That(same.CompiledSchemas, Is.SameAs(first.CompiledSchemas));
            Assert.That(version.ReusedCachedCompilation, Is.False);
            Assert.That(version.CompiledSchemas, Is.Not.SameAs(first.CompiledSchemas));
            Assert.That(profile.ReusedCachedCompilation, Is.False);
            Assert.That(profile.CompiledSchemas, Is.Not.SameAs(first.CompiledSchemas));
            Assert.That(schemaNamespace.ReusedCachedCompilation, Is.False);
            Assert.That(schemaNamespace.CompiledSchemas, Is.Not.SameAs(first.CompiledSchemas));
        });
    }

    [Test]
    public void CompilationEnforcesClosureBoundsAndReportsXsdErrors()
    {
        SchemaRegistryOptions importOptions = new SchemaRegistryOptions
        {
            ResolveIncludes = false,
            ResolveImports = false
        };
        SchemaCompilationResult imported = ImportSimpleSchema(
            "urn:example:bounded",
            "1",
            "base",
            importOptions);
        SchemaCompilationResult tooMany = SchemaCompiler.CopyImportResult(imported);
        tooMany.Schemas.Add(imported.Schemas[0]);
        SchemaRegistryOptions countOptions = new SchemaRegistryOptions();
        countOptions.Limits.MaxSchemaCount = 1;
        SchemaRegistryOptions byteOptions = new SchemaRegistryOptions();
        byteOptions.Limits.MaxTotalBytes = imported.Schemas[0].Content.Length - 1;
        SchemaCompiler compiler = new SchemaCompiler();

        SchemaCompilationResult countFailure = compiler.CompileClosure(
            tooMany,
            countOptions);
        SchemaCompilationResult byteFailure = compiler.CompileClosure(
            imported,
            byteOptions);
        SchemaCompilationResult invalidImport = ImportSchemaText(
            "urn:example:invalid",
            "1",
            "base",
            """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
              xmlns:tns="urn:example:invalid"
              targetNamespace="urn:example:invalid">
              <xs:element name="Document" type="tns:MissingType"/>
            </xs:schema>
            """,
            importOptions);
        SchemaCompilationResult invalid = compiler.CompileClosure(
            invalidImport,
            importOptions);

        Assert.Multiple(() =>
        {
            Assert.That(countFailure.Success, Is.False);
            Assert.That(countFailure.ErrorCode, Is.EqualTo("schema.compile.schema_limit"));
            Assert.That(byteFailure.Success, Is.False);
            Assert.That(byteFailure.ErrorCode, Is.EqualTo("schema.compile.byte_limit"));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.ErrorCode, Is.EqualTo("schema.compile.invalid"));
            Assert.That(invalid.DiagnosticCodes, Does.Contain("schema.compile.error"));
            Assert.That(invalid.DiagnosticLocations.Any(item => item.HasLineInformation),
                Is.True);
            Assert.That(invalid.CompiledSchemas, Is.Null);
        });
    }

    private static SchemaCompilationResult ImportSimpleSchema(
        string namespaceUri,
        string version,
        string profile,
        SchemaRegistryOptions options)
    {
        string schema = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"" +
            " targetNamespace=\"" + namespaceUri + "\">" +
            "<xs:element name=\"Document\" type=\"xs:string\"/>" +
            "</xs:schema>";
        return ImportSchemaText(namespaceUri, version, profile, schema, options);
    }

    private static SchemaCompilationResult ImportSchemaText(
        string namespaceUri,
        string version,
        string profile,
        string schema,
        SchemaRegistryOptions options)
    {
        string path = Path.Combine(Path.GetTempPath(), "schema-compiler.xsd");
        SchemaArtifact request = CreateRequest(namespaceUri, version, profile, path);
        ISchemaImporter importer = new SchemaImporter();
        return importer.ImportSchemaStream(
            new MemoryStream(Encoding.UTF8.GetBytes(schema)),
            request,
            options);
    }

    private static SchemaArtifact CreateRequest(
        string namespaceUri,
        string version,
        string profile,
        string path)
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
                    Version = version
                },
                OriginUri = new Uri(path).AbsoluteUri,
                IsApproved = true
            },
            SourceName = Path.GetFileName(path),
            BaseUri = new Uri(path).AbsoluteUri,
            NamespaceUri = namespaceUri,
            Version = version,
            Profile = profile,
            IsEntryPoint = true
        };
    }

    private static string CreateClosureRootSchema()
    {
        return """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
              xmlns:tns="urn:example:main"
              xmlns:dep="urn:example:dependency"
              targetNamespace="urn:example:main">
              <xs:include schemaLocation="common.xsd"/>
              <xs:import namespace="urn:example:dependency" schemaLocation="dependency.xsd"/>
              <xs:element name="Document">
                <xs:complexType>
                  <xs:sequence>
                    <xs:element name="Common" type="tns:CommonText"/>
                    <xs:element name="Dependency" type="dep:DependencyText"/>
                  </xs:sequence>
                </xs:complexType>
              </xs:element>
            </xs:schema>
            """;
    }

    private static string CreateCommonSchema()
    {
        return """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:simpleType name="CommonText">
                <xs:restriction base="xs:string"/>
              </xs:simpleType>
            </xs:schema>
            """;
    }

    private static string CreateDependencySchema()
    {
        return """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
              targetNamespace="urn:example:dependency">
              <xs:simpleType name="DependencyText">
                <xs:restriction base="xs:string"/>
              </xs:simpleType>
            </xs:schema>
            """;
    }

    private static string CreateTemporaryRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "iso20022-schema-compiler-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
