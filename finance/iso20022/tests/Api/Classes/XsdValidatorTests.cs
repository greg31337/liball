using System.Text;
using System.Xml;
using iso20022;

namespace iso20022.Tests;

public class XsdValidatorTests
{
    [Test]
    public void RawXmlValidationReportsStableLinePathAndSchemaProvenance()
    {
        string root = CreateTemporaryRoot();
        try
        {
            SchemaRegistryOptions options = CreateOptions(root);
            SchemaArtifact schema = RegisterValidationSchema(root, options);
            ISchemaRegistry registry = CreateRegistry(schema, options);
            IXsdValidator validator = new XsdValidator(registry);
            SchemaArtifact selector = CreateSelector();
            string validXml = """
                <Document xmlns="urn:example:validation"
                  xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                  xsi:schemaLocation="urn:example:validation https://example.test/remote.xsd">
                  <Code>SHORT</Code>
                  <Count>2</Count>
                </Document>
                """;
            string invalidXml = """
                <Document xmlns="urn:example:validation">
                  <Code>TOO-LONG</Code>
                  <Count>0</Count>
                </Document>
                """;

            SchemaCompilationResult valid = validator.ValidateXmlStream(
                new MemoryStream(Encoding.UTF8.GetBytes(validXml)),
                selector,
                options);
            SchemaCompilationResult invalid = validator.ValidateXmlStream(
                new MemoryStream(Encoding.UTF8.GetBytes(invalidXml)),
                selector,
                options);

            Assert.Multiple(() =>
            {
                Assert.That(valid.Success, Is.True, valid.ErrorMessage);
                Assert.That(valid.Found, Is.True);
                Assert.That(valid.CompiledSchemas, Is.Not.Null);
                Assert.That(invalid.Success, Is.False);
                Assert.That(invalid.ErrorCode, Is.EqualTo("schema.validation.invalid"));
                Assert.That(invalid.DiagnosticCodes,
                    Has.Some.EqualTo("schema.validation.error"));
                Assert.That(invalid.DiagnosticPaths.Select(item => item.Value),
                    Does.Contain(
                        "/Q{urn:example:validation}Document/Q{urn:example:validation}Code"));
                Assert.That(invalid.DiagnosticPaths.Select(item => item.Value),
                    Does.Contain(
                        "/Q{urn:example:validation}Document/Q{urn:example:validation}Count"));
                Assert.That(invalid.DiagnosticLocations.All(item => item.HasLineInformation),
                    Is.True);
                Assert.That(invalid.DiagnosticLocations.Select(item => item.LineNumber),
                    Does.Contain(2));
                Assert.That(invalid.DiagnosticSchemaUris,
                    Is.All.EqualTo(schema.BaseUri));
                Assert.That(invalid.DiagnosticPaths.Count,
                    Is.EqualTo(invalid.DiagnosticCodes.Count));
                Assert.That(invalid.DiagnosticSchemaUris.Count,
                    Is.EqualTo(invalid.DiagnosticCodes.Count));
                Assert.That(invalid.ReusedCachedCompilation, Is.True);
            });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void MessageDocumentValidationUsesOriginalNodeLocations()
    {
        string root = CreateTemporaryRoot();
        try
        {
            SchemaRegistryOptions options = CreateOptions(root);
            SchemaArtifact schema = RegisterValidationSchema(root, options);
            ISchemaRegistry registry = CreateRegistry(schema, options);
            IXsdValidator validator = new XsdValidator(registry);
            string xml = """
                <Document xmlns="urn:example:validation">
                  <Code>TOO-LONG</Code>
                  <Count>1</Count>
                </Document>
                """;
            MessageReadResult read = new XmlMessageReader().ReadGenericText(
                xml,
                new MessageReadOptions());
            MessageDocument document = read.Document ??
                throw new AssertionException(read.ErrorMessage);
            DynamicXmlNode messageRoot = document.Payloads[0].GenericContent ??
                throw new AssertionException("The generic payload root is missing.");
            DynamicXmlNode code = messageRoot.Children.Single(
                item => item.Kind == XmlNodeKind.Element && item.Name.LocalName == "Code");
            code.Location.SourceName = "original-message.xml";
            code.Location.HasLineInformation = true;
            code.Location.LineNumber = 41;
            code.Location.ColumnNumber = 7;

            SchemaCompilationResult result = validator.ValidateMessageDocument(
                document,
                CreateSelector(),
                options);
            int diagnosticIndex = result.DiagnosticPaths.FindIndex(
                item => item.Value.EndsWith("}Code", StringComparison.Ordinal));

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo("schema.validation.invalid"));
                Assert.That(diagnosticIndex, Is.GreaterThanOrEqualTo(0));
                Assert.That(result.DiagnosticLocations[diagnosticIndex].SourceName,
                    Is.EqualTo("original-message.xml"));
                Assert.That(result.DiagnosticLocations[diagnosticIndex].LineNumber,
                    Is.EqualTo(41));
                Assert.That(result.DiagnosticLocations[diagnosticIndex].ColumnNumber,
                    Is.EqualTo(7));
                Assert.That(result.DiagnosticSchemaUris[diagnosticIndex],
                    Is.EqualTo(schema.BaseUri));
            });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void ValidationHonorsMissingSelectionDiagnosticAndXmlResourceBounds()
    {
        string root = CreateTemporaryRoot();
        try
        {
            SchemaRegistryOptions options = CreateOptions(root);
            SchemaArtifact schema = RegisterValidationSchema(root, options);
            ISchemaRegistry registry = CreateRegistry(schema, options);
            IXsdValidator validator = new XsdValidator(registry);
            string invalidXml = """
                <Document xmlns="urn:example:validation">
                  <Code>TOO-LONG</Code>
                  <Count>0</Count>
                </Document>
                """;
            SchemaArtifact missingSelector = CreateSelector();
            missingSelector.Profile = "missing";
            SchemaCompilationResult missing = validator.ValidateXmlStream(
                new MemoryStream(Encoding.UTF8.GetBytes(invalidXml)),
                missingSelector,
                options);

            SchemaRegistryOptions diagnosticOptions = CreateOptions(root);
            diagnosticOptions.Limits.MaxDiagnosticCount = 1;
            SchemaCompilationResult diagnosticLimit = validator.ValidateXmlStream(
                new MemoryStream(Encoding.UTF8.GetBytes(invalidXml)),
                CreateSelector(),
                diagnosticOptions);

            SchemaRegistryOptions resourceOptions = CreateOptions(root);
            resourceOptions.Limits.MaxCollectionCount = 3;
            string oversizedXml = """
                <Document xmlns="urn:example:validation">
                  <Code>SHORT</Code>
                  <Count>1</Count>
                  <Extra>unexpected</Extra>
                  <ExtraAgain>unexpected</ExtraAgain>
                </Document>
                """;
            SchemaCompilationResult resourceLimit = validator.ValidateXmlStream(
                new MemoryStream(Encoding.UTF8.GetBytes(oversizedXml)),
                CreateSelector(),
                resourceOptions);

            Assert.Multiple(() =>
            {
                Assert.That(missing.Success, Is.True);
                Assert.That(missing.Found, Is.False);
                Assert.That(diagnosticLimit.Success, Is.False);
                Assert.That(diagnosticLimit.ErrorCode,
                    Is.EqualTo("schema.validation.diagnostic_limit"));
                Assert.That(diagnosticLimit.DiagnosticCodes, Has.Count.EqualTo(1));
                Assert.That(resourceLimit.Success, Is.False);
                Assert.That(resourceLimit.ErrorCode,
                    Is.EqualTo("schema.validation.resource_limit"));
            });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void ValidationRejectsMissingInputsAndShortCircuitsCompilationFailures()
    {
        SchemaCompilationResult unavailable = new SchemaCompilationResult();
        StubSchemaRegistry registry = new StubSchemaRegistry(unavailable);
        IXsdValidator validator = new XsdValidator(registry);

        Assert.Multiple(() =>
        {
            Assert.That(validator.ValidateXmlStream(null, null, null).ErrorCode,
                Is.EqualTo("schema.validation.input_missing"));
            Assert.That(validator.ValidateMessageDocument(null, null, null).ErrorCode,
                Is.EqualTo("schema.validation.document_missing"));
            Assert.That(validator.ValidateXmlStream(new MemoryStream(), null, null),
                Is.SameAs(unavailable));
            Assert.That(validator.ValidateMessageDocument(new MessageDocument(), null, null),
                Is.SameAs(unavailable));
            Assert.Throws<ArgumentNullException>(() => new XsdValidator(null!));
        });
    }

    [Test]
    public void ValidationReportsInputDocumentAndCompiledSchemaBoundaryFailures()
    {
        SchemaCompilationResult compiled = CreateCompiledResult();
        StubSchemaRegistry registry = new StubSchemaRegistry(compiled);
        IXsdValidator validator = new XsdValidator(registry);
        SchemaRegistryOptions tiny = new SchemaRegistryOptions();
        tiny.Limits.MaxTotalBytes = 1;
        SchemaCompilationResult inputLimit = validator.ValidateXmlStream(
            new MemoryStream(Encoding.UTF8.GetBytes("<x/>")),
            new SchemaArtifact(),
            tiny);

        compiled = CreateCompiledResult();
        registry.Result = compiled;
        SchemaCompilationResult noContent = validator.ValidateMessageDocument(
            new MessageDocument(),
            new SchemaArtifact(),
            new SchemaRegistryOptions());

        SchemaCompilationResult missingRoot = CreateCompiledResult();
        missingRoot.RootSchema = null;
        SchemaCompilationResult missingSchemas = CreateCompiledResult();
        missingSchemas.CompiledSchemas = null;

        Assert.Multiple(() =>
        {
            Assert.That(inputLimit.ErrorCode, Is.EqualTo("schema.validation.input_limit"));
            Assert.That(noContent.ErrorCode,
                Is.EqualTo("schema.validation.document_content_missing"));
            Assert.That(XsdValidator.ValidateBytes(
                "<x/>"u8.ToArray(), null, new XsdDocumentLocationMap(), missingRoot,
                new SchemaRegistryOptions()).ErrorCode,
                Is.EqualTo("schema.validation.schema_missing"));
            Assert.That(XsdValidator.ValidateBytes(
                "<x/>"u8.ToArray(), null, new XsdDocumentLocationMap(), missingSchemas,
                new SchemaRegistryOptions()).ErrorCode,
                Is.EqualTo("schema.validation.schema_missing"));
        });

        compiled = CreateCompiledResult();
        registry.Result = compiled;
        MessageDocument document = new MessageDocument();
        document.Payloads.Add(new MessagePayload
        {
            GenericContent = new DynamicXmlNode
            {
                Kind = XmlNodeKind.Element,
                Name = new QualifiedNameData { LocalName = "x" }
            }
        });
        Assert.That(validator.ValidateMessageDocument(
            document, new SchemaArtifact(), tiny).ErrorCode,
            Is.EqualTo("schema.validation.input_limit"));
    }

    [Test]
    public void ReaderResourceChecksAndDiagnosticMetadataCoverAllValueKinds()
    {
        XsdValidationState noReader = CreateValidationState();
        XsdValidator.RecordReaderNode(noReader);
        SchemaCompilationResult metadata = new SchemaCompilationResult();
        metadata.DiagnosticCodes.Add("one");
        metadata.DiagnosticCodes.Add("two");
        XsdValidator.EnsureDiagnosticMetadata(metadata, "schema");

        Assert.Multiple(() =>
        {
            Assert.That(noReader.Continue, Is.True);
            Assert.That(metadata.DiagnosticPaths, Has.Count.EqualTo(2));
            Assert.That(metadata.DiagnosticSchemaUris,
                Is.EqualTo(new[] { "schema", "schema" }));
            Assert.That(ReadWithLimits("<r a='long'/>", 100, 1, 100).ErrorCode,
                Is.EqualTo("schema.validation.resource_limit"));
            Assert.That(ReadWithLimits("<r><a/><b/></r>", 100, 100, 1).ErrorCode,
                Is.EqualTo("schema.validation.resource_limit"));
            Assert.That(ReadWithLimits("<r>long</r>", 100, 1, 100).ErrorCode,
                Is.EqualTo("schema.validation.resource_limit"));
            Assert.That(ReadWithLimits("<r><![CDATA[long]]></r>", 100, 1, 100).ErrorCode,
                Is.EqualTo("schema.validation.resource_limit"));
            Assert.That(ReadWithLimits("<r>   </r>", 100, 1, 100).ErrorCode,
                Is.EqualTo("schema.validation.resource_limit"));
            Assert.That(ReadWithLimits("<r xml:space='preserve'>   </r>", 100, 1, 100).ErrorCode,
                Is.EqualTo("schema.validation.resource_limit"));
            Assert.That(ReadWithResourceLimits("<r><a/></r>", limits =>
                limits.MaxDepth = 1).ErrorCode,
                Is.EqualTo("schema.validation.resource_limit"));
            Assert.That(ReadWithResourceLimits("<r a='x'/>", limits =>
                limits.MaxAttributesPerElement = 0).ErrorCode,
                Is.EqualTo("schema.validation.resource_limit"));
            Assert.That(ReadWithResourceLimits("<r><a/></r>", limits =>
                limits.MaxElementCount = 1).ErrorCode,
                Is.EqualTo("schema.validation.resource_limit"));
            Assert.That(ReadWithLimits("<r>x</r>", 100, 1, 100).Success, Is.True);
            Assert.That(ReadWithLimits("<r>\n <a/>\n</r>", 100, 0, 100).ErrorCode,
                Is.EqualTo("schema.validation.resource_limit"));
            Assert.That(ReadWithLimits(
                "<r xml:space='preserve'>\n <a/>\n</r>", 100, 0, 100).ErrorCode,
                Is.EqualTo("schema.validation.resource_limit"));
            Assert.That(ReadPreservedWhitespace(
                "<r>\n <a/>\n</r>").ErrorCode,
                Is.EqualTo("schema.validation.resource_limit"));
            Assert.That(ReadPreservedWhitespace(
                "<r xml:space='preserve'>\n <a/>\n</r>").ErrorCode,
                Is.EqualTo("schema.validation.resource_limit"));
            Assert.That(ReadSignificantWhitespace(
                "<r xml:space='preserve'>\n <a/>\n</r>").ErrorCode,
                Is.EqualTo("schema.validation.resource_limit"));
        });
    }

    private static SchemaCompilationResult ReadWithResourceLimits(
        string xml,
        Action<ResourceLimits> configure)
    {
        XsdValidationState state = CreateValidationState();
        configure(state.Options.Limits);
        using XmlReader reader = XmlReader.Create(new StringReader(xml));
        state.Reader = reader;
        while (state.Continue && reader.Read())
        {
            XsdValidator.RecordReaderNode(state);
        }

        return state.Result;
    }

    private static SchemaCompilationResult ReadPreservedWhitespace(string xml)
    {
        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.PreserveWhitespace = true;
        document.LoadXml(xml);
        XsdValidationState state = CreateValidationState();
        state.Options.Limits.MaxTextLength = 0;
        using XmlNodeReader reader = new XmlNodeReader(document);
        state.Reader = reader;
        bool found = reader.Read();
        while (found && reader.NodeType != XmlNodeType.Whitespace &&
            reader.NodeType != XmlNodeType.SignificantWhitespace)
        {
            found = reader.Read();
        }

        if (found)
        {
            XsdValidator.RecordReaderNode(state);
        }

        return state.Result;
    }

    private static SchemaCompilationResult ReadSignificantWhitespace(string xml)
    {
        XsdValidationState state = CreateValidationState();
        state.Options.Limits.MaxTextLength = 0;
        using XmlTextReader reader = new XmlTextReader(new StringReader(xml));
        reader.WhitespaceHandling = WhitespaceHandling.Significant;
        state.Reader = reader;
        while (state.Continue && reader.Read())
        {
            XsdValidator.RecordReaderNode(state);
        }

        return state.Result;
    }

    private static SchemaCompilationResult ReadWithLimits(
        string xml,
        int maxElements,
        int maxText,
        int maxCollection)
    {
        XsdValidationState state = CreateValidationState();
        state.Options.Limits.MaxElementCount = maxElements;
        state.Options.Limits.MaxTextLength = maxText;
        state.Options.Limits.MaxCollectionCount = maxCollection;
        using XmlReader reader = XmlReader.Create(new StringReader(xml));
        state.Reader = reader;
        while (state.Continue && reader.Read())
        {
            XsdValidator.RecordReaderNode(state);
        }

        return state.Result;
    }

    private static XsdValidationState CreateValidationState()
    {
        return new XsdValidationState
        {
            Result = new SchemaCompilationResult { Success = true },
            Options = new SchemaRegistryOptions()
        };
    }

    private static SchemaCompilationResult CreateCompiledResult()
    {
        return new SchemaCompilationResult
        {
            Success = true,
            Found = true,
            RootSchema = new SchemaArtifact { BaseUri = "urn:test" },
            CompiledSchemas = new System.Xml.Schema.XmlSchemaSet()
        };
    }

    private static ISchemaRegistry CreateRegistry(
        SchemaArtifact schema,
        SchemaRegistryOptions options)
    {
        ISchemaRegistry registry = new SchemaRegistry();
        SchemaCompilationResult registered = registry.RegisterSchema(schema, options);
        Assert.That(registered.Success, Is.True, registered.ErrorMessage);
        return registry;
    }

    private static SchemaArtifact RegisterValidationSchema(
        string root,
        SchemaRegistryOptions options)
    {
        string path = Path.Combine(root, "validation.xsd");
        SchemaArtifact request = new SchemaArtifact
        {
            Artifact = new ArtifactManifest
            {
                Coordinate = new ArtifactCoordinate
                {
                    Kind = ArtifactKind.Schema,
                    Edition = "2026",
                    Identifier = "validation",
                    Version = "1"
                },
                OriginUri = new Uri(path).AbsoluteUri,
                IsApproved = true
            },
            SourceName = Path.GetFileName(path),
            BaseUri = new Uri(path).AbsoluteUri,
            NamespaceUri = "urn:example:validation",
            Version = "1",
            Profile = "base",
            IsEntryPoint = true
        };
        SchemaRegistryOptions importOptions = CreateOptions(root);
        importOptions.ResolveIncludes = false;
        importOptions.ResolveImports = false;
        SchemaCompilationResult imported = new SchemaImporter().ImportSchemaStream(
            new MemoryStream(Encoding.UTF8.GetBytes(CreateValidationSchema())),
            request,
            importOptions);
        Assert.That(imported.Success, Is.True, imported.ErrorMessage);
        return imported.RootSchema ?? throw new AssertionException(imported.ErrorMessage);
    }

    private static SchemaArtifact CreateSelector()
    {
        return new SchemaArtifact
        {
            NamespaceUri = "urn:example:validation",
            Version = "1",
            Profile = "base"
        };
    }

    private static SchemaRegistryOptions CreateOptions(string root)
    {
        return new SchemaRegistryOptions
        {
            LocalRootPath = root
        };
    }

    private static string CreateValidationSchema()
    {
        return """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
              targetNamespace="urn:example:validation"
              xmlns:tns="urn:example:validation"
              elementFormDefault="qualified">
              <xs:simpleType name="CodeText">
                <xs:restriction base="xs:string">
                  <xs:maxLength value="5"/>
                </xs:restriction>
              </xs:simpleType>
              <xs:element name="Document">
                <xs:complexType>
                  <xs:sequence>
                    <xs:element name="Code" type="tns:CodeText"/>
                    <xs:element name="Count" type="xs:positiveInteger"/>
                  </xs:sequence>
                </xs:complexType>
              </xs:element>
            </xs:schema>
            """;
    }

    private static string CreateTemporaryRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "iso20022-xsd-validator-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class StubSchemaRegistry : ISchemaRegistry
    {
        internal SchemaCompilationResult Result;

        internal StubSchemaRegistry(SchemaCompilationResult result)
        {
            Result = result;
        }

        public SchemaCompilationResult ImportRegisterSchemaStream(
            Stream? input, SchemaArtifact? schema, SchemaRegistryOptions? options)
        {
            return Result;
        }

        public SchemaCompilationResult ImportRegisterSchemaFile(
            string? path, SchemaArtifact? schema, SchemaRegistryOptions? options)
        {
            return Result;
        }

        public SchemaCompilationResult ImportRegisterSchemaArchive(
            Stream? input, SchemaArtifact? schema, SchemaRegistryOptions? options)
        {
            return Result;
        }

        public SchemaCompilationResult RegisterSchema(
            SchemaArtifact? schema, SchemaRegistryOptions? options)
        {
            return Result;
        }

        public SchemaCompilationResult LookupSchema(
            SchemaArtifact? selector, SchemaRegistryOptions? options)
        {
            return Result;
        }

        public SchemaCompilationResult CompileSchema(
            SchemaArtifact? selector, SchemaRegistryOptions? options)
        {
            return Result;
        }
    }
}
