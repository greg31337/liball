using iso20022;

namespace iso20022.Tests;

public class ValidationReportIntegrationTests
{
    [Test]
    public void XsdStageMapsDiagnosticsSeverityLocationAndProvenanceIntoPipelineReport()
    {
        SchemaArtifact schema = CreateSchema();
        SchemaCompilationResult xsdResult = new SchemaCompilationResult
        {
            Success = false,
            Found = true,
            RootSchema = schema,
            ErrorCode = "schema.validation.invalid",
            ErrorMessage = "The message does not satisfy the schema."
        };
        xsdResult.Schemas.Add(schema);
        xsdResult.DiagnosticCodes.Add("schema.validation.warning");
        xsdResult.DiagnosticMessages.Add("A schema warning.");
        xsdResult.DiagnosticLocations.Add(new SourceLocation
        {
            SourceName = "message.xml",
            HasLineInformation = true,
            LineNumber = 4,
            ColumnNumber = 3
        });
        xsdResult.DiagnosticPaths.Add(new XmlPathData
        {
            Value = "/Q{urn:example:validation}Document"
        });
        xsdResult.DiagnosticSchemaUris.Add(schema.BaseUri);
        xsdResult.DiagnosticCodes.Add("schema.validation.error");
        xsdResult.DiagnosticMessages.Add("A schema error.");
        xsdResult.DiagnosticLocations.Add(new SourceLocation
        {
            SourceName = "message.xml",
            HasLineInformation = true,
            LineNumber = 7,
            ColumnNumber = 9
        });
        xsdResult.DiagnosticPaths.Add(new XmlPathData
        {
            Value = "/Q{urn:example:validation}Document/Q{urn:example:validation}Code"
        });
        xsdResult.DiagnosticSchemaUris.Add(schema.BaseUri);
        StubXsdValidator xsdValidator = new StubXsdValidator(xsdResult);
        ValidationRequest request = new ValidationRequest
        {
            Document = new MessageDocument(),
            Schema = schema,
            XsdValidator = xsdValidator
        };
        request.SchemaOptions.LocalRootPath = "/caller/schemas";
        request.Policy.Limits.MaxDepth = 17;
        request.Policy.Stages.Add(ValidationStage.Xsd);

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True, report.ErrorMessage);
            Assert.That(report.Valid, Is.False);
            Assert.That(report.CompletedStages,
                Is.EqualTo(new[] { ValidationStage.Xsd }));
            Assert.That(report.Diagnostics.Select(item => item.Code), Is.EqualTo(new[]
            {
                "validation.xsd.warning",
                "validation.xsd.error"
            }));
            Assert.That(report.Diagnostics.Select(item => item.Severity), Is.EqualTo(new[]
            {
                ValidationSeverity.Warning,
                ValidationSeverity.Error
            }));
            Assert.That(report.Diagnostics.Select(item => item.Stage),
                Has.All.EqualTo(ValidationStage.Xsd));
            Assert.That(report.Diagnostics.Select(item => item.SourceArtifact),
                Has.All.SameAs(schema.Artifact));
            Assert.That(report.Diagnostics.Select(item => item.SourceIdentifier),
                Has.All.EqualTo(schema.BaseUri));
            Assert.That(report.Diagnostics[1].Path.Value,
                Does.EndWith("}Code"));
            Assert.That(report.Diagnostics[1].Location.LineNumber, Is.EqualTo(7));
            Assert.That(report.WarningCount, Is.EqualTo(1));
            Assert.That(report.ErrorCount, Is.EqualTo(1));
            Assert.That(xsdValidator.DocumentInvocationCount, Is.EqualTo(1));
            Assert.That(xsdValidator.StreamInvocationCount, Is.Zero);
            Assert.That(xsdValidator.LastSelector, Is.SameAs(schema));
            Assert.That(xsdValidator.LastOptions, Is.Not.SameAs(request.SchemaOptions));
            Assert.That(xsdValidator.LastOptions!.LocalRootPath,
                Is.EqualTo("/caller/schemas"));
            Assert.That(xsdValidator.LastOptions.Limits, Is.SameAs(request.Policy.Limits));
            Assert.That(xsdValidator.LastOptions.Limits.MaxDepth, Is.EqualTo(17));
        });
    }

    [Test]
    public void RawXsdResourceFailureStopsWithStableOperationDiagnostic()
    {
        SchemaArtifact schema = CreateSchema();
        SchemaCompilationResult xsdResult = new SchemaCompilationResult
        {
            Success = false,
            Found = true,
            RootSchema = schema,
            ErrorCode = "schema.validation.resource_limit",
            ErrorMessage = "The XML exceeds a configured XSD resource limit."
        };
        xsdResult.Schemas.Add(schema);
        StubXsdValidator xsdValidator = new StubXsdValidator(xsdResult);
        ValidationRequest request = new ValidationRequest
        {
            Xml = "<Document/>"u8.ToArray(),
            Schema = schema,
            XsdValidator = xsdValidator
        };
        request.Policy.Stages.Add(ValidationStage.Xsd);
        request.Policy.Stages.Add(ValidationStage.Semantic);

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.False);
            Assert.That(report.Valid, Is.False);
            Assert.That(report.StoppedEarly, Is.True);
            Assert.That(report.ErrorCode,
                Is.EqualTo("validation.xsd.resource_limit"));
            Assert.That(report.Diagnostics.Single().Code,
                Is.EqualTo("validation.xsd.resource_limit"));
            Assert.That(report.Diagnostics.Single().Kind,
                Is.EqualTo(DiagnosticKind.ResourceLimit));
            Assert.That(report.Diagnostics.Single().Stage,
                Is.EqualTo(ValidationStage.Xsd));
            Assert.That(report.Diagnostics.Single().SourceArtifact,
                Is.SameAs(schema.Artifact));
            Assert.That(report.CompletedStages,
                Is.EqualTo(new[] { ValidationStage.Xsd }));
            Assert.That(xsdValidator.StreamInvocationCount, Is.EqualTo(1));
            Assert.That(xsdValidator.DocumentInvocationCount, Is.Zero);
        });
    }

    [Test]
    public void StableReportIntegratorNormalizesDiagnosticsAndPropagatesChildOutcome()
    {
        ValidationRequest request = new ValidationRequest
        {
            Document = new MessageDocument()
        };
        ValidationReport target = new ValidationReport
        {
            Success = true,
            Valid = true
        };
        ValidationPipelineState state = new ValidationPipelineState(
            request,
            request.Policy,
            target);
        state.Stage = ValidationStage.SupplementaryData;
        ValidationReport child = new ValidationReport
        {
            Success = true,
            Valid = false,
            ExternalCodeEdition = "2026-Q2"
        };
        child.Diagnostics.Add(new ValidationDiagnostic
        {
            OffendingValue = "secret"
        });

        ValidationReportIntegrator.MergeReport(state, child);

        Assert.Multiple(() =>
        {
            Assert.That(target.Success, Is.True);
            Assert.That(target.Valid, Is.False);
            Assert.That(target.ExternalCodeEdition, Is.EqualTo("2026-Q2"));
            Assert.That(target.ErrorCount, Is.EqualTo(1));
            Assert.That(target.Diagnostics.Single().Code,
                Is.EqualTo("validation.stage.diagnostic"));
            Assert.That(target.Diagnostics.Single().RuleIdentifier,
                Is.EqualTo("iso20022.validation.stage"));
            Assert.That(target.Diagnostics.Single().Severity,
                Is.EqualTo(ValidationSeverity.Error));
            Assert.That(target.Diagnostics.Single().Kind,
                Is.EqualTo(DiagnosticKind.ValidationFailure));
            Assert.That(target.Diagnostics.Single().Stage,
                Is.EqualTo(ValidationStage.SupplementaryData));
            Assert.That(target.Diagnostics.Single().OffendingValue, Is.Empty);
            Assert.That(target.Diagnostics.Single().OffendingValueRedacted, Is.True);
            Assert.That(child.Diagnostics.Single().OffendingValue,
                Is.EqualTo("secret"));
            Assert.That(child.Diagnostics.Single().OffendingValueRedacted, Is.False);
            Assert.That(child.Diagnostics.Single().Stage,
                Is.EqualTo(ValidationStage.Unknown));
        });
    }

    private static SchemaArtifact CreateSchema()
    {
        return new SchemaArtifact
        {
            BaseUri = "file:///schemas/message.xsd",
            NamespaceUri = "urn:example:validation",
            Version = "1",
            Profile = "base",
            Artifact = new ArtifactManifest
            {
                Coordinate = new ArtifactCoordinate
                {
                    Kind = ArtifactKind.Schema,
                    Identifier = "message-schema",
                    Version = "1"
                },
                IsApproved = true
            }
        };
    }

    private class StubXsdValidator : IXsdValidator
    {
        internal SchemaCompilationResult Result;
        internal int StreamInvocationCount;
        internal int DocumentInvocationCount;
        internal SchemaArtifact? LastSelector;
        internal SchemaRegistryOptions? LastOptions;

        internal StubXsdValidator(SchemaCompilationResult result)
        {
            Result = result;
        }

        public SchemaCompilationResult ValidateXmlStream(
            Stream? input,
            SchemaArtifact? selector,
            SchemaRegistryOptions? options)
        {
            StreamInvocationCount++;
            LastSelector = selector;
            LastOptions = options;
            return Result;
        }

        public SchemaCompilationResult ValidateMessageDocument(
            MessageDocument? document,
            SchemaArtifact? selector,
            SchemaRegistryOptions? options)
        {
            DocumentInvocationCount++;
            LastSelector = selector;
            LastOptions = options;
            return Result;
        }
    }
}
