using System.Text;
using iso20022;

namespace iso20022.Tests;

public class SchemaRegistryTests
{
    [Test]
    public void LookupRequiresEveryExactRegistryKeyComponent()
    {
        SchemaArtifact schema = ImportSchema(
            "urn:example:exact",
            "10",
            "base",
            10,
            "/tmp/exact.xsd",
            CreateSimpleSchema("urn:example:exact", "ExactText"),
            true);
        schema.MessageIdentifier = CreateMessageIdentifier(
            "pacs",
            "008",
            "001",
            "10");
        ISchemaRegistry registry = new SchemaRegistry();
        SchemaRegistryOptions options = CreateOptions(string.Empty);
        registry.RegisterSchema(schema, options);

        SchemaArtifact exact = CreateSelector("urn:example:exact", "10", "base");
        exact.MessageIdentifier = CreateMessageIdentifier("pacs", "008", "001", "10");
        SchemaArtifact namespaceMismatch = CreateSelector(
            "urn:example:different",
            "10",
            "base");
        namespaceMismatch.MessageIdentifier = exact.MessageIdentifier;
        SchemaArtifact identifierMismatch = CreateSelector(
            "urn:example:exact",
            "10",
            "base");
        identifierMismatch.MessageIdentifier = CreateMessageIdentifier(
            "pacs",
            "008",
            "002",
            "10");
        SchemaArtifact versionMismatch = CreateSelector(
            "urn:example:exact",
            "11",
            "base");
        versionMismatch.MessageIdentifier = exact.MessageIdentifier;
        SchemaArtifact profileMismatch = CreateSelector(
            "urn:example:exact",
            "10",
            "market");
        profileMismatch.MessageIdentifier = exact.MessageIdentifier;

        SchemaCompilationResult found = registry.LookupSchema(exact, options);
        SchemaCompilationResult wrongNamespace = registry.LookupSchema(
            namespaceMismatch,
            options);
        SchemaCompilationResult wrongIdentifier = registry.LookupSchema(
            identifierMismatch,
            options);
        SchemaCompilationResult wrongVersion = registry.LookupSchema(
            versionMismatch,
            options);
        SchemaCompilationResult wrongProfile = registry.LookupSchema(
            profileMismatch,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(found.Found, Is.True);
            Assert.That(found.RootSchema, Is.SameAs(schema));
            Assert.That(wrongNamespace.Found, Is.False);
            Assert.That(wrongIdentifier.Found, Is.False);
            Assert.That(wrongVersion.Found, Is.False);
            Assert.That(wrongProfile.Found, Is.False);
        });
    }

    [Test]
    public void RegistrationAndLookupUseExactKeysAndPreserveArtifactProvenance()
    {
        SchemaArtifact schema = ImportSchema(
            "urn:example:registered",
            "1",
            "base",
            10,
            "/tmp/registered.xsd",
            CreateSimpleSchema("urn:example:registered", "RegisteredText"),
            true);
        schema.MessageIdentifier = CreateMessageIdentifier(
            "pacs",
            "008",
            "001",
            "10");
        ISchemaRegistry registry = new SchemaRegistry();
        SchemaRegistryOptions options = CreateOptions(string.Empty);

        SchemaCompilationResult registered = registry.RegisterSchema(schema, options);
        SchemaCompilationResult repeated = registry.RegisterSchema(schema, options);
        SchemaArtifact exactSelector = CreateSelector(
            "urn:example:registered",
            "1",
            "base");
        exactSelector.MessageIdentifier = CreateMessageIdentifier(
            "pacs",
            "008",
            "001",
            "10");
        SchemaCompilationResult found = registry.LookupSchema(exactSelector, options);
        SchemaCompilationResult missing = registry.LookupSchema(
            CreateSelector("urn:example:registered", "1", "base"),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(registered.Success, Is.True, registered.ErrorMessage);
            Assert.That(registered.Registered, Is.True);
            Assert.That(registered.Found, Is.True);
            Assert.That(repeated.Success, Is.True);
            Assert.That(repeated.Registered, Is.False);
            Assert.That(found.Success, Is.True);
            Assert.That(found.Found, Is.True);
            Assert.That(found.RootSchema, Is.SameAs(schema));
            Assert.That(found.RootSchema?.Artifact.OriginUri,
                Is.EqualTo(new Uri("/tmp/registered.xsd").AbsoluteUri));
            Assert.That(found.RootSchema?.Artifact.Hash.Value, Has.Length.EqualTo(32));
            Assert.That(missing.Success, Is.True);
            Assert.That(missing.Found, Is.False);
            Assert.That(missing.RootSchema, Is.Null);
        });
    }

    [Test]
    public void RegistrationRequiresExplicitHigherPrecedenceReplacement()
    {
        SchemaArtifact first = ImportSchema(
            "urn:example:collision",
            "1",
            "base",
            10,
            "/tmp/first.xsd",
            CreateSimpleSchema("urn:example:collision", "FirstText"),
            true);
        SchemaArtifact tied = ImportSchema(
            "urn:example:collision",
            "1",
            "base",
            10,
            "/tmp/tied.xsd",
            CreateSimpleSchema("urn:example:collision", "TiedText"),
            true);
        SchemaArtifact higher = ImportSchema(
            "urn:example:collision",
            "1",
            "base",
            20,
            "/tmp/higher.xsd",
            CreateSimpleSchema("urn:example:collision", "HigherText"),
            true);
        SchemaArtifact lower = ImportSchema(
            "urn:example:collision",
            "1",
            "base",
            5,
            "/tmp/lower.xsd",
            CreateSimpleSchema("urn:example:collision", "LowerText"),
            true);
        ISchemaRegistry registry = new SchemaRegistry();
        SchemaRegistryOptions options = CreateOptions(string.Empty);
        registry.RegisterSchema(first, options);

        SchemaCompilationResult collision = registry.RegisterSchema(tied, options);
        SchemaCompilationResult disabled = registry.RegisterSchema(higher, options);
        options.AllowReplacement = true;
        SchemaCompilationResult replaced = registry.RegisterSchema(higher, options);
        SchemaCompilationResult rejected = registry.RegisterSchema(lower, options);
        SchemaCompilationResult selected = registry.LookupSchema(
            CreateSelector("urn:example:collision", "1", "base"),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(collision.ErrorCode,
                Is.EqualTo("schema.registry.precedence_collision"));
            Assert.That(disabled.ErrorCode,
                Is.EqualTo("schema.registry.replacement_disabled"));
            Assert.That(replaced.Success, Is.True, replaced.ErrorMessage);
            Assert.That(replaced.Registered, Is.True);
            Assert.That(replaced.ReplacedExistingSchema, Is.True);
            Assert.That(rejected.ErrorCode,
                Is.EqualTo("schema.registry.precedence_rejected"));
            Assert.That(selected.RootSchema, Is.SameAs(higher));
            Assert.That(selected.RootSchema?.Artifact.OriginUri,
                Is.EqualTo(new Uri("/tmp/higher.xsd").AbsoluteUri));
        });
    }

    [Test]
    public void DependencyReplacementInvalidatesCompiledRootClosure()
    {
        string root = CreateTemporaryRoot();
        try
        {
            string dependencyPath = Path.Combine(root, "dependency.xsd");
            string replacementPath = Path.Combine(root, "dependency-replacement.xsd");
            string mainPath = Path.Combine(root, "main.xsd");
            SchemaArtifact dependency = ImportSchema(
                "urn:example:dependency",
                "1",
                "base",
                10,
                dependencyPath,
                CreateSimpleSchema("urn:example:dependency", "DependencyText"),
                true);
            SchemaArtifact replacement = ImportSchema(
                "urn:example:dependency",
                "1",
                "base",
                20,
                replacementPath,
                CreateReplacementSchema(),
                true);
            SchemaArtifact main = ImportSchema(
                "urn:example:main",
                "1",
                "base",
                10,
                mainPath,
                CreateMainSchema(),
                true);
            ISchemaRegistry registry = new SchemaRegistry();
            SchemaRegistryOptions options = CreateOptions(root);
            registry.RegisterSchema(dependency, options);
            options.CompileOnRegistration = true;

            SchemaCompilationResult registration = registry.RegisterSchema(main, options);
            options.CompileOnRegistration = false;
            SchemaArtifact selector = CreateSelector("urn:example:main", "1", "base");
            SchemaCompilationResult cached = registry.CompileSchema(selector, options);
            options.AllowReplacement = true;
            SchemaCompilationResult replaced = registry.RegisterSchema(replacement, options);
            SchemaCompilationResult recompiled = registry.CompileSchema(selector, options);
            SchemaCompilationResult cachedAgain = registry.CompileSchema(selector, options);

            Assert.Multiple(() =>
            {
                Assert.That(registration.Success, Is.True, registration.ErrorMessage);
                Assert.That(registration.Registered, Is.True);
                Assert.That(registration.CompiledSchemas, Is.Not.Null);
                Assert.That(cached.Success, Is.True, cached.ErrorMessage);
                Assert.That(cached.ReusedCachedCompilation, Is.True);
                Assert.That(replaced.Success, Is.True, replaced.ErrorMessage);
                Assert.That(replaced.InvalidatedCachedCompilation, Is.True);
                Assert.That(recompiled.Success, Is.True, recompiled.ErrorMessage);
                Assert.That(recompiled.ReusedCachedCompilation, Is.False);
                Assert.That(recompiled.Schemas, Has.Count.EqualTo(2));
                Assert.That(recompiled.Schemas[1].Artifact.OriginUri,
                    Is.EqualTo(new Uri(replacementPath).AbsoluteUri));
                Assert.That(cachedAgain.ReusedCachedCompilation, Is.True);
                Assert.That(cachedAgain.CompiledSchemas,
                    Is.SameAs(recompiled.CompiledSchemas));
            });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void RegistrationEnforcesApprovalAndRegistryBounds()
    {
        ISchemaRegistry registry = new SchemaRegistry();
        SchemaRegistryOptions approvedOptions = CreateOptions(string.Empty);
        approvedOptions.RequireApprovedArtifacts = true;
        SchemaArtifact draft = ImportSchema(
            "urn:example:draft",
            "1",
            "base",
            1,
            "/tmp/draft.xsd",
            CreateSimpleSchema("urn:example:draft", "DraftText"),
            false);
        SchemaCompilationResult unapproved = registry.RegisterSchema(
            draft,
            approvedOptions);

        SchemaRegistryOptions boundedOptions = CreateOptions(string.Empty);
        boundedOptions.Limits.MaxSchemaCount = 1;
        SchemaArtifact first = ImportSchema(
            "urn:example:first",
            "1",
            "base",
            1,
            "/tmp/bounded-first.xsd",
            CreateSimpleSchema("urn:example:first", "FirstText"),
            true);
        SchemaArtifact second = ImportSchema(
            "urn:example:second",
            "1",
            "base",
            1,
            "/tmp/bounded-second.xsd",
            CreateSimpleSchema("urn:example:second", "SecondText"),
            true);
        SchemaCompilationResult accepted = registry.RegisterSchema(first, boundedOptions);
        SchemaCompilationResult limited = registry.RegisterSchema(second, boundedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(unapproved.Success, Is.False);
            Assert.That(unapproved.ErrorCode, Is.EqualTo("schema.registry.unapproved"));
            Assert.That(accepted.Success, Is.True);
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.ErrorCode, Is.EqualTo("schema.registry.limit"));
        });
    }

    [Test]
    public void RegistryValidationAndCompilationBoundariesReportExactCauses()
    {
        SchemaRegistryOptions options = new SchemaRegistryOptions();
        Assert.Multiple(() =>
        {
            Assert.That(SchemaRegistry.ValidateRegistration(null, options)?.ErrorCode,
                Is.EqualTo("schema.registry.schema_missing"));
            Assert.That(SchemaRegistry.ValidateRegistration(new SchemaArtifact(), null)?.ErrorCode,
                Is.EqualTo("schema.registry.options_missing"));
            Assert.That(SchemaRegistry.ValidateSelector(null, options)?.ErrorCode,
                Is.EqualTo("schema.registry.selector_missing"));
            Assert.That(SchemaRegistry.ValidateSelector(new SchemaArtifact(), null)?.ErrorCode,
                Is.EqualTo("schema.registry.options_missing"));
            Assert.That(SchemaRegistry.ValidateKey(new SchemaArtifact
                { NamespaceUri = null! })?.ErrorCode,
                Is.EqualTo("schema.registry.key_invalid"));
            Assert.That(SchemaRegistry.ValidateKey(new SchemaArtifact
                { NamespaceUri = "http://[bad" })?.ErrorCode,
                Is.EqualTo("schema.registry.key_invalid"));
            Assert.That(SchemaRegistry.ValidateKey(new SchemaArtifact
                { MessageIdentifier = new MessageIdentifier { Value = "bad" } })?.ErrorCode,
                Is.EqualTo("schema.registry.key_invalid"));
        });

        SchemaRegistryOptions badLimits = new SchemaRegistryOptions();
        badLimits.Limits.MaxSchemaCount = 0;
        Assert.That(SchemaRegistry.ValidateSelector(new SchemaArtifact(), badLimits)?.ErrorCode,
            Is.EqualTo("schema.registry.limits_invalid"));
        SchemaRegistryOptions tooMany = new SchemaRegistryOptions();
        tooMany.Limits.MaxCollectionCount = 1;
        tooMany.LocalSchemas.Add(new SchemaArtifact());
        tooMany.LocalSchemas.Add(new SchemaArtifact());
        Assert.That(SchemaRegistry.ValidateSelector(new SchemaArtifact(), tooMany)?.ErrorCode,
            Is.EqualTo("schema.registry.local_schema_limit"));

        SchemaArtifact invalidContent = ImportSchema(
            "urn:example:invalid", "1", "base", 1,
            "/tmp/invalid-content.xsd",
            CreateSimpleSchema("urn:example:invalid", "Text"), true);
        invalidContent.Content = "not xml"u8.ToArray();
        SchemaRegistry registry = new SchemaRegistry();
        SchemaRegistryOptions compile = new SchemaRegistryOptions
        {
            CompileOnRegistration = true,
            ResolveIncludes = false,
            ResolveImports = false
        };
        SchemaCompilationResult invalid = registry.RegisterSchema(invalidContent, compile);
        Assert.That(invalid.Success, Is.False);

        SchemaArtifact unapproved = ImportSchema(
            "urn:example:unapproved", "1", "base", 1,
            "/tmp/unapproved-compile.xsd",
            CreateSimpleSchema("urn:example:unapproved", "Text"), false);
        SchemaRegistryOptions requireApproved = new SchemaRegistryOptions
        {
            RequireApprovedArtifacts = true,
            CompileOnRegistration = true,
            ResolveIncludes = false,
            ResolveImports = false
        };
        Assert.That(new SchemaRegistry().RegisterSchema(unapproved, requireApproved).ErrorCode,
            Is.EqualTo("schema.registry.unapproved"));
    }

    private static SchemaArtifact ImportSchema(
        string namespaceUri,
        string version,
        string profile,
        int precedence,
        string path,
        string xsd,
        bool approved)
    {
        SchemaArtifact request = new SchemaArtifact
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
                IsApproved = approved
            },
            SourceName = Path.GetFileName(path),
            BaseUri = new Uri(path).AbsoluteUri,
            NamespaceUri = namespaceUri,
            Version = version,
            Profile = profile,
            Precedence = precedence,
            IsEntryPoint = true
        };
        SchemaRegistryOptions options = CreateOptions(string.Empty);
        options.ResolveIncludes = false;
        options.ResolveImports = false;
        SchemaCompilationResult imported = new SchemaImporter().ImportSchemaStream(
            new MemoryStream(Encoding.UTF8.GetBytes(xsd)),
            request,
            options);
        return imported.RootSchema ?? throw new AssertionException(imported.ErrorMessage);
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

    private static MessageIdentifier CreateMessageIdentifier(
        string area,
        string functionality,
        string variant,
        string version)
    {
        return new MessageIdentifier
        {
            BusinessArea = area,
            MessageFunctionality = functionality,
            Variant = variant,
            Version = version,
            Value = area + "." + functionality + "." + variant + "." + version
        };
    }

    private static SchemaRegistryOptions CreateOptions(string root)
    {
        return new SchemaRegistryOptions
        {
            LocalRootPath = root
        };
    }

    private static string CreateSimpleSchema(string namespaceUri, string typeName)
    {
        return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"" +
            " targetNamespace=\"" + namespaceUri + "\">" +
            "<xs:simpleType name=\"" + typeName + "\">" +
            "<xs:restriction base=\"xs:string\"/>" +
            "</xs:simpleType></xs:schema>";
    }

    private static string CreateReplacementSchema()
    {
        return """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
              targetNamespace="urn:example:dependency">
              <xs:simpleType name="DependencyText">
                <xs:restriction base="xs:string">
                  <xs:maxLength value="35"/>
                </xs:restriction>
              </xs:simpleType>
            </xs:schema>
            """;
    }

    private static string CreateMainSchema()
    {
        return """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
              xmlns:dep="urn:example:dependency"
              targetNamespace="urn:example:main">
              <xs:import namespace="urn:example:dependency"/>
              <xs:element name="Document" type="dep:DependencyText"/>
            </xs:schema>
            """;
    }

    private static string CreateTemporaryRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "iso20022-schema-registry-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
