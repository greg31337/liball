using System.Text;
using iso20022;

namespace iso20022.Tests;

public class MessageProcessorTests
{
    [Test]
    public void ComposesSelectedSourceStagesInDeterministicOrderWithCompleteReports()
    {
        BuilderResult built = BuildPayment();
        ValidationRequest validation = new ValidationRequest
        {
            Policy = Policy(ValidationStage.EnvelopeAndHeader),
            Context = new ValidationContext()
        };
        ProcessingRequest request = new ProcessingRequest
        {
            Xml = built.Xml,
            ExpectedSourceMessageIdentifier = Identifier("pacs.008.001.10"),
            ExpectedSourceNamespaceUri = Namespace("pacs.008.001.10"),
            Validation = validation,
            WriteOptions = new MessageWriteOptions { WriteXmlDeclaration = false }
        };
        request.Stages.Add(ProcessingStage.Writing);
        request.Stages.Add(ProcessingStage.MetadataExtraction);
        request.Stages.Add(ProcessingStage.Detection);
        request.Stages.Add(ProcessingStage.Validation);
        request.Stages.Add(ProcessingStage.Reading);
        IMessageProcessor processor = new MessageProcessor();

        ProcessingResult result = processor.ProcessMessage(request);

        ProcessingStage[] expectedStages =
        {
            ProcessingStage.Detection,
            ProcessingStage.Reading,
            ProcessingStage.Validation,
            ProcessingStage.MetadataExtraction,
            ProcessingStage.Writing
        };
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.RequestedStages, Is.EqualTo(expectedStages));
            Assert.That(result.CompletedStages, Is.EqualTo(expectedStages));
            Assert.That(result.FailedStage, Is.EqualTo(ProcessingStage.Unknown));
            Assert.That(result.Detection?.Success, Is.True);
            Assert.That(result.Read?.Success, Is.True);
            Assert.That(result.Validation?.Valid, Is.True);
            Assert.That(result.Metadata?.Uetr,
                Is.EqualTo("123e4567-e89b-42d3-a456-426614174000"));
            Assert.That(result.Write?.Success, Is.True);
            Assert.That(result.ValidationReports.Single(),
                Is.SameAs(result.Validation));
            Assert.That(result.SourceMessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(result.SourceNamespaceUri,
                Is.EqualTo(Namespace("pacs.008.001.10")));
            Assert.That(Encoding.UTF8.GetString(result.Xml),
                Does.Contain("<EndToEndId>E2E-001</EndToEndId>"));
            Assert.That(request.Stages, Is.EqualTo(new[]
            {
                ProcessingStage.Writing,
                ProcessingStage.MetadataExtraction,
                ProcessingStage.Detection,
                ProcessingStage.Validation,
                ProcessingStage.Reading
            }));
            Assert.That(validation.Document, Is.Null);
            Assert.That(validation.Xml, Is.Empty);
        });
    }

    [Test]
    public void MaterializesOmittedPrerequisitesWithoutReportingUnselectedStages()
    {
        BuilderResult built = BuildPayment();
        ProcessingRequest request = new ProcessingRequest
        {
            Document = built.Document,
            ExpectedSourceMessageIdentifier = Identifier("pacs.008.001.10"),
            ExpectedSourceNamespaceUri = Namespace("pacs.008.001.10"),
            WriteOptions = new MessageWriteOptions { WriteXmlDeclaration = false }
        };
        request.Stages.Add(ProcessingStage.Writing);
        request.Stages.Add(ProcessingStage.MetadataExtraction);

        ProcessingResult result = new MessageProcessor().ProcessMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.RequestedStages, Is.EqualTo(new[]
            {
                ProcessingStage.MetadataExtraction,
                ProcessingStage.Writing
            }));
            Assert.That(result.CompletedStages, Is.EqualTo(result.RequestedStages));
            Assert.That(result.Detection, Is.Null);
            Assert.That(result.Read, Is.Null);
            Assert.That(result.Metadata?.PrimaryAmount?.Amount.Scale,
                Is.EqualTo(4));
            Assert.That(result.Write?.BytesWritten, Is.GreaterThan(0));
        });
    }

    [Test]
    public void MigratesThenWritesExactTargetAndRetainsMigrationReport()
    {
        MessageIdentifier source = Identifier("pacs.008.001.10");
        MessageIdentifier target = Identifier("pacs.008.001.11");
        MigrationStep step = new MigrationStep
        {
            Sequence = 0,
            Action = MigrationActionKind.Copy,
            Required = true
        };
        step.SourcePaths.Add(Path("/Document/Amount"));
        step.TargetPaths.Add(Path("/Document/Amount"));
        MigrationPlan plan = new MigrationPlan
        {
            Success = true,
            SourceMessageIdentifier = source,
            TargetMessageIdentifier = target
        };
        plan.Steps.Add(step);
        MigrationRequest migration = new MigrationRequest
        {
            SourceMessageIdentifier = source,
            TargetMessageIdentifier = target,
            Plan = plan,
            ValidateTarget = false
        };
        ProcessingRequest request = new ProcessingRequest
        {
            Xml = Encoding.UTF8.GetBytes(MessageXml(source.Value, "<Amount>10.5000</Amount>")),
            ExpectedSourceMessageIdentifier = source,
            ExpectedSourceNamespaceUri = Namespace(source.Value),
            TargetMessageIdentifier = target,
            TargetNamespaceUri = Namespace(target.Value),
            Migration = migration,
            WriteOptions = new MessageWriteOptions { WriteXmlDeclaration = false }
        };
        request.Stages.Add(ProcessingStage.Migration);
        request.Stages.Add(ProcessingStage.Writing);

        ProcessingResult result = new MessageProcessor().ProcessMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Migration?.Success, Is.True);
            Assert.That(result.Migration?.AppliedSteps.Single(), Is.SameAs(step));
            Assert.That(result.TargetMessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.11"));
            Assert.That(result.TargetNamespaceUri,
                Is.EqualTo(Namespace("pacs.008.001.11")));
            Assert.That(Encoding.UTF8.GetString(result.Xml),
                Does.Contain("pacs.008.001.11"));
            Assert.That(Encoding.UTF8.GetString(result.Xml),
                Does.Contain("<Amount>10.5000</Amount>"));
            Assert.That(migration.Xml, Is.Empty);
            Assert.That(migration.Document, Is.Null);
        });
    }

    [Test]
    public void MapsThenWritesAndAggregatesTargetValidationExactlyOnce()
    {
        MessageIdentifier source = Identifier("pacs.008.001.10");
        MessageIdentifier target = Identifier("pacs.008.001.11");
        MappingRuleDefinition rule = new MappingRuleDefinition
        {
            Identifier = "amount",
            Sequence = 0,
            Action = MappingActionKind.Rename,
            Required = true
        };
        rule.SourcePaths.Add(Path("/Document/Amount"));
        rule.TargetPaths.Add(Path("/Document/MappedAmount"));
        MappingDefinition definition = new MappingDefinition
        {
            Identifier = "processor-map",
            SourceMessageIdentifier = source,
            TargetMessageIdentifier = target
        };
        definition.Rules.Add(rule);
        ValidationDiagnostic diagnostic = new ValidationDiagnostic
        {
            Code = "target.checked",
            Severity = ValidationSeverity.Information,
            Stage = ValidationStage.Profile
        };
        ValidationReport targetValidation = new ValidationReport
        {
            Success = true,
            Valid = true,
            MessageIdentifier = target,
            NamespaceUri = Namespace(target.Value)
        };
        targetValidation.Diagnostics.Add(diagnostic);
        RecordingValidator validator = new RecordingValidator(targetValidation);
        MappingRequest mapping = new MappingRequest
        {
            Mapping = definition,
            ValidateTarget = true,
            Validator = validator
        };
        ProcessingRequest request = new ProcessingRequest
        {
            Xml = Encoding.UTF8.GetBytes(MessageXml(source.Value, "<Amount>42.250</Amount>")),
            ExpectedSourceMessageIdentifier = source,
            ExpectedSourceNamespaceUri = Namespace(source.Value),
            TargetMessageIdentifier = target,
            Mapping = mapping,
            WriteOptions = new MessageWriteOptions { WriteXmlDeclaration = false }
        };
        request.Stages.Add(ProcessingStage.Mapping);
        request.Stages.Add(ProcessingStage.Writing);

        ProcessingResult result = new MessageProcessor().ProcessMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Mapping?.AppliedRules.Single(), Is.SameAs(rule));
            Assert.That(result.ValidationReports.Single(),
                Is.SameAs(targetValidation));
            Assert.That(result.Diagnostics.Single(), Is.SameAs(diagnostic));
            Assert.That(result.TargetMessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.11"));
            Assert.That(Encoding.UTF8.GetString(result.Xml),
                Does.Contain("<MappedAmount>42.250</MappedAmount>"));
            Assert.That(validator.Request?.Document,
                Is.SameAs(result.Mapping?.Document));
            Assert.That(mapping.Xml, Is.Empty);
        });
    }

    [Test]
    public void StopsAtInvalidDirectValidationAndKeepsReachedReports()
    {
        BuilderResult built = BuildPayment();
        ValidationPolicy policy = Policy(ValidationStage.EnvelopeAndHeader);
        ProcessingRequest request = new ProcessingRequest
        {
            Xml = built.Xml,
            Validation = new ValidationRequest
            {
                Policy = policy,
                Context = new ValidationContext(),
                ExpectedNamespaceUri = Namespace("pacs.008.001.11")
            }
        };
        request.Stages.Add(ProcessingStage.Detection);
        request.Stages.Add(ProcessingStage.Validation);
        request.Stages.Add(ProcessingStage.Writing);

        ProcessingResult result = new MessageProcessor().ProcessMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.FailedStage, Is.EqualTo(ProcessingStage.Validation));
            Assert.That(result.CompletedStages,
                Is.EqualTo(new[] { ProcessingStage.Detection }));
            Assert.That(result.Validation, Is.Not.Null);
            Assert.That(result.Validation?.Valid, Is.False);
            Assert.That(result.ValidationReports.Single(),
                Is.SameAs(result.Validation));
            Assert.That(result.Diagnostics,
                Does.Contain(result.Validation!.Diagnostics.First()));
            Assert.That(result.Write, Is.Null);
            Assert.That(result.Document, Is.Not.Null);
            Assert.That(result.Read, Is.Null);
            Assert.That(result.Xml, Is.EqualTo(built.Xml));
        });
    }

    [Test]
    public void MaterializesGeneratedModelForReadingMetadataAndWriting()
    {
        ProjectionGeneratedTestModel model = new ProjectionGeneratedTestModel
        {
            MessageIdentifier = "GENERATED-MSG",
            PaymentIdentifier = "GENERATED-PAY"
        };
        ProcessingRequest request = new ProcessingRequest
        {
            Model = model,
            ExpectedSourceMessageIdentifier = Identifier("pain.001.001.11"),
            ExpectedSourceNamespaceUri = Namespace("pain.001.001.11"),
            WriteOptions = new MessageWriteOptions { WriteXmlDeclaration = false }
        };
        request.Stages.Add(ProcessingStage.Reading);
        request.Stages.Add(ProcessingStage.MetadataExtraction);
        request.Stages.Add(ProcessingStage.Writing);

        ProcessingResult result = new MessageProcessor().ProcessMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Read?.Document, Is.Not.Null);
            Assert.That(result.Metadata?.MessageIdentifier?.Value,
                Is.EqualTo("pain.001.001.11"));
            Assert.That(Encoding.UTF8.GetString(result.Xml),
                Does.Contain("GENERATED-PAY"));
            Assert.That(result.Model, Is.SameAs(model));
        });
    }

    [Test]
    public void RejectsInvalidStageSelectionsInputsAndCancellationDeterministically()
    {
        ProcessingRequest noStages = new ProcessingRequest
        {
            Xml = Encoding.UTF8.GetBytes(MessageXml("pacs.008.001.10", string.Empty))
        };
        ProcessingRequest duplicate = new ProcessingRequest
        {
            Xml = noStages.Xml
        };
        duplicate.Stages.Add(ProcessingStage.Reading);
        duplicate.Stages.Add(ProcessingStage.Reading);
        ProcessingRequest ambiguous = new ProcessingRequest
        {
            Xml = noStages.Xml,
            Document = new MessageDocument()
        };
        ambiguous.Stages.Add(ProcessingStage.Reading);
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        ProcessingRequest cancelled = new ProcessingRequest
        {
            Xml = noStages.Xml,
            CancellationToken = cancellation.Token
        };
        cancelled.Stages.Add(ProcessingStage.Detection);

        ProcessingResult missingResult = new MessageProcessor().ProcessMessage(noStages);
        ProcessingResult duplicateResult = new MessageProcessor().ProcessMessage(duplicate);
        ProcessingResult ambiguousResult = new MessageProcessor().ProcessMessage(ambiguous);
        ProcessingResult cancelledResult = new MessageProcessor().ProcessMessage(cancelled);

        Assert.Multiple(() =>
        {
            Assert.That(new MessageProcessor().ProcessMessage(null).ErrorCode,
                Is.EqualTo("processing.request_missing"));
            Assert.That(missingResult.ErrorCode,
                Is.EqualTo("processing.stages_missing"));
            Assert.That(duplicateResult.ErrorCode,
                Is.EqualTo("processing.stage_duplicate"));
            Assert.That(ambiguousResult.ErrorCode,
                Is.EqualTo("processing.input_invalid"));
            Assert.That(cancelledResult.ErrorCode,
                Is.EqualTo("processing.cancelled"));
            Assert.That(cancelledResult.FailedStage,
                Is.EqualTo(ProcessingStage.Detection));
            Assert.That(cancelledResult.CompletedStages, Is.Empty);
        });
    }

    [Test]
    public void CompletesEveryNonEmptyStageCombinationInCanonicalOrder()
    {
        int mask = 1;
        while (mask < 128)
        {
            ProcessingRequest request = CombinationRequest(mask);
            ProcessingResult result = new MessageProcessor().ProcessMessage(request);
            ProcessingStage[] expected = SelectedStages(mask);

            Assert.That(result.Success, Is.True,
                "Stage mask " + mask + " failed: " + result.ErrorCode + " " +
                result.ErrorMessage);
            Assert.That(result.RequestedStages, Is.EqualTo(expected),
                "Requested stages for mask " + mask);
            Assert.That(result.CompletedStages, Is.EqualTo(expected),
                "Completed stages for mask " + mask);
            Assert.That(result.FailedStage, Is.EqualTo(ProcessingStage.Unknown),
                "Failed stage for mask " + mask);
            AssertStageReports(result, mask);
            mask++;
        }
    }

    [Test]
    public void LocalizesEarlyFailureAndPreservesEarlierPartialResultsAtEveryStage()
    {
        ProcessingRequest detection = new ProcessingRequest
        {
            Xml = Encoding.UTF8.GetBytes("<Unknown xmlns='urn:unknown'/>")
        };
        detection.Stages.Add(ProcessingStage.Detection);
        detection.Stages.Add(ProcessingStage.Reading);

        ProcessingRequest reading = new ProcessingRequest
        {
            Xml = Encoding.UTF8.GetBytes("<Unknown xmlns='urn:unknown'/>")
        };
        reading.Stages.Add(ProcessingStage.Reading);
        reading.Stages.Add(ProcessingStage.MetadataExtraction);

        ProcessingRequest validation = BaseProcessingRequest();
        validation.Stages.Add(ProcessingStage.Reading);
        validation.Stages.Add(ProcessingStage.Validation);
        validation.Stages.Add(ProcessingStage.MetadataExtraction);

        ProcessingRequest metadata = BaseProcessingRequest();
        metadata.ExpectedSourceNamespaceUri = Namespace("pacs.008.001.11");
        metadata.Stages.Add(ProcessingStage.MetadataExtraction);
        metadata.Stages.Add(ProcessingStage.Writing);

        ProcessingRequest migration = BaseProcessingRequest();
        migration.Stages.Add(ProcessingStage.Reading);
        migration.Stages.Add(ProcessingStage.Migration);
        migration.Stages.Add(ProcessingStage.Writing);

        ProcessingRequest mapping = BaseProcessingRequest();
        mapping.Stages.Add(ProcessingStage.Reading);
        mapping.Stages.Add(ProcessingStage.Mapping);
        mapping.Stages.Add(ProcessingStage.Writing);

        MessageDocument emptyDocument = new MessageDocument();
        emptyDocument.Metadata.MessageIdentifier = Identifier("pacs.008.001.10");
        emptyDocument.Metadata.NamespaceUri = Namespace("pacs.008.001.10");
        ProcessingRequest writing = new ProcessingRequest
        {
            Document = emptyDocument,
            ExpectedSourceMessageIdentifier = Identifier("pacs.008.001.10"),
            ExpectedSourceNamespaceUri = Namespace("pacs.008.001.10")
        };
        writing.Stages.Add(ProcessingStage.MetadataExtraction);
        writing.Stages.Add(ProcessingStage.Writing);

        ProcessingRequest[] requests =
        [
            detection,
            reading,
            validation,
            metadata,
            migration,
            mapping,
            writing
        ];
        ProcessingStage[] failedStages =
        [
            ProcessingStage.Detection,
            ProcessingStage.Reading,
            ProcessingStage.Validation,
            ProcessingStage.MetadataExtraction,
            ProcessingStage.Migration,
            ProcessingStage.Mapping,
            ProcessingStage.Writing
        ];
        string[] errorCodes =
        [
            "detection.root",
            "detection.root",
            "processing.validation_request_missing",
            "processing.source_namespace_mismatch",
            "processing.migration_request_missing",
            "processing.mapping_request_missing",
            "writer.content_missing"
        ];
        int[] completedCounts = [0, 0, 1, 0, 1, 1, 1];

        int index = 0;
        while (index < requests.Length)
        {
            ProcessingResult result = new MessageProcessor().ProcessMessage(
                requests[index]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False,
                    "Failure case " + failedStages[index]);
                Assert.That(result.FailedStage, Is.EqualTo(failedStages[index]));
                Assert.That(result.ErrorCode, Is.EqualTo(errorCodes[index]));
                Assert.That(result.CompletedStages,
                    Has.Count.EqualTo(completedCounts[index]));
                Assert.That(result.CompletedStages,
                    Is.EqualTo(result.RequestedStages.Take(completedCounts[index])));
                Assert.That(result.Diagnostics, Is.Not.Empty);
                Assert.That(result.Diagnostics.Last().Code,
                    Is.EqualTo(errorCodes[index]));
            });
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(new MessageProcessor().ProcessMessage(detection).Detection,
                Is.Not.Null);
            Assert.That(new MessageProcessor().ProcessMessage(reading).Read,
                Is.Not.Null);
            Assert.That(new MessageProcessor().ProcessMessage(validation).Read?.Success,
                Is.True);
            Assert.That(new MessageProcessor().ProcessMessage(migration).Read?.Success,
                Is.True);
            Assert.That(new MessageProcessor().ProcessMessage(mapping).Read?.Success,
                Is.True);
            Assert.That(new MessageProcessor().ProcessMessage(writing).Metadata,
                Is.Not.Null);
        });
    }

    [Test]
    public void ChainsMigrationAndMappingPathsBeforeWriting()
    {
        MessageIdentifier source = Identifier("pacs.008.001.10");
        MessageIdentifier target = Identifier("pacs.008.001.11");
        ProcessingRequest request = BaseProcessingRequest();
        request.TargetMessageIdentifier = target;
        request.TargetNamespaceUri = Namespace(target.Value);
        request.Migration = Migration(source, target, PaymentAmountPath());
        request.Mapping = Mapping(target, target, "/Document/Amount");
        request.Stages.Add(ProcessingStage.Migration);
        request.Stages.Add(ProcessingStage.Mapping);
        request.Stages.Add(ProcessingStage.Writing);

        ProcessingResult result = new MessageProcessor().ProcessMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Migration?.AppliedSteps, Has.Count.EqualTo(1));
            Assert.That(result.Mapping?.AppliedRules, Has.Count.EqualTo(1));
            Assert.That(result.CompletedStages, Is.EqualTo(new[]
            {
                ProcessingStage.Migration,
                ProcessingStage.Mapping,
                ProcessingStage.Writing
            }));
            Assert.That(result.TargetMessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.11"));
            Assert.That(Encoding.UTF8.GetString(result.Xml),
                Does.Contain("<MappedAmount Ccy=\"USD\">99.9900</MappedAmount>"));
            Assert.That(Encoding.UTF8.GetString(result.Xml),
                Does.Not.Contain("<Amount>"));
        });
    }

    [Test]
    public void PropagatesTransformationDiagnosticsByReferenceWithoutDuplication()
    {
        MessageIdentifier source = Identifier("pacs.008.001.10");
        MessageIdentifier target = Identifier("pacs.008.001.11");
        ProcessingRequest migrationRequest = BaseProcessingRequest();
        migrationRequest.TargetMessageIdentifier = target;
        migrationRequest.Migration = Migration(source, target, "/Document/Missing");
        migrationRequest.Stages.Add(ProcessingStage.Migration);

        ProcessingRequest mappingRequest = BaseProcessingRequest();
        mappingRequest.TargetMessageIdentifier = target;
        mappingRequest.Mapping = Mapping(source, target, "/Document/Missing");
        mappingRequest.Stages.Add(ProcessingStage.Mapping);

        ProcessingResult migration = new MessageProcessor().ProcessMessage(
            migrationRequest);
        ProcessingResult mapping = new MessageProcessor().ProcessMessage(
            mappingRequest);

        Assert.Multiple(() =>
        {
            Assert.That(migration.Success, Is.False);
            Assert.That(migration.FailedStage, Is.EqualTo(ProcessingStage.Migration));
            Assert.That(migration.Migration?.FailedSteps, Has.Count.EqualTo(1));
            Assert.That(migration.Migration?.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(migration.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(migration.Diagnostics[0],
                Is.SameAs(migration.Migration?.Diagnostics[0]));
            Assert.That(migration.Diagnostics[0].Code,
                Is.EqualTo("migration.step_failed"));
            Assert.That(migration.Diagnostics[1].Code,
                Is.EqualTo("migration.required_step_failed"));
            Assert.That(mapping.Success, Is.False);
            Assert.That(mapping.FailedStage, Is.EqualTo(ProcessingStage.Mapping));
            Assert.That(mapping.Mapping?.FailedRules, Has.Count.EqualTo(1));
            Assert.That(mapping.Mapping?.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(mapping.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(mapping.Diagnostics[0],
                Is.SameAs(mapping.Mapping?.Diagnostics[0]));
            Assert.That(mapping.Diagnostics[0].Code,
                Is.EqualTo("mapping.rule_failed"));
            Assert.That(mapping.Diagnostics[1].Code,
                Is.EqualTo("mapping.required_rule_failed"));
        });
    }

    [Test]
    public void ConfigurationAndRepresentationBoundariesFailAtTheirOwningStage()
    {
        MessageProcessor processor = new MessageProcessor();
        ProcessingRequest noStages = BaseProcessingRequest();
        ProcessingRequest tooManyStages = BaseProcessingRequest();
        tooManyStages.Limits.MaxCollectionCount = 1;
        tooManyStages.Stages.Add(ProcessingStage.Detection);
        tooManyStages.Stages.Add(ProcessingStage.Reading);
        ProcessingRequest tooManyBytes = BaseProcessingRequest();
        tooManyBytes.Stages.Add(ProcessingStage.Detection);
        tooManyBytes.Limits.MaxTotalBytes = 1;
        ProcessingRequest ambiguous = BaseProcessingRequest();
        ambiguous.Stages.Add(ProcessingStage.Reading);
        ambiguous.Document = new MessageDocument();
        ProcessingRequest badSource = BaseProcessingRequest();
        badSource.Stages.Add(ProcessingStage.Reading);
        badSource.ExpectedSourceMessageIdentifier = new MessageIdentifier { Value = "bad" };
        ProcessingRequest badTarget = BaseProcessingRequest();
        badTarget.Stages.Add(ProcessingStage.Reading);
        badTarget.TargetMessageIdentifier = new MessageIdentifier { Value = "bad" };
        ProcessingRequest duplicate = BaseProcessingRequest();
        duplicate.Stages.Add(ProcessingStage.Reading);
        duplicate.Stages.Add(ProcessingStage.Reading);
        ProcessingRequest unknown = BaseProcessingRequest();
        unknown.Stages.Add((ProcessingStage)99);

        Assert.Multiple(() =>
        {
            Assert.That(processor.ProcessMessage(noStages).ErrorCode,
                Is.EqualTo("processing.stages_missing"));
            Assert.That(processor.ProcessMessage(tooManyStages).ErrorCode,
                Is.EqualTo("processing.stage_limit"));
            Assert.That(processor.ProcessMessage(tooManyBytes).ErrorCode,
                Is.EqualTo("processing.input_limit"));
            Assert.That(processor.ProcessMessage(ambiguous).ErrorCode,
                Is.EqualTo("processing.input_invalid"));
            Assert.That(processor.ProcessMessage(badSource).ErrorCode,
                Is.EqualTo("processing.source_identifier_invalid"));
            Assert.That(processor.ProcessMessage(badTarget).ErrorCode,
                Is.EqualTo("processing.target_identifier_invalid"));
            Assert.That(processor.ProcessMessage(duplicate).ErrorCode,
                Is.EqualTo("processing.stage_duplicate"));
            Assert.That(processor.ProcessMessage(unknown).ErrorCode,
                Is.EqualTo("processing.stage_invalid"));
            Assert.That(MessageProcessor.ProcessingStageIsKnown(ProcessingStage.Unknown), Is.False);
        });

        ProcessingExecutionState state = new ProcessingExecutionState();
        state.Request = BaseProcessingRequest();
        Assert.That(MessageProcessor.ExecuteProcessingStage(state,
            ProcessingStage.Unknown), Is.False);

        MigrationRequest migration = new MigrationRequest();
        MappingRequest mapping = new MappingRequest();
        state.Document = new MessageDocument();
        MessageProcessor.CopyCurrentProcessingRepresentation(state, migration, mapping);
        Assert.That(migration.Document, Is.SameAs(state.Document));
        state.Document = null;
        state.Model = new object();
        migration = new MigrationRequest();
        mapping = new MappingRequest();
        MessageProcessor.CopyCurrentProcessingRepresentation(state, migration, mapping);
        Assert.That(mapping.Model, Is.SameAs(state.Model));
        state.Model = null;
        state.Xml = [1, 2];
        migration = new MigrationRequest();
        mapping = new MappingRequest();
        MessageProcessor.CopyCurrentProcessingRepresentation(state, migration, mapping);
        Assert.That(migration.Xml, Is.EqualTo(new byte[] { 1, 2 }));

        MessageDocument namespaced = new MessageDocument();
        namespaced.Metadata.NamespaceUri = "urn:document";
        Assert.Multiple(() =>
        {
            Assert.That(MessageProcessor.ResolveProcessingTargetNamespace(
                "urn:requested", null, null), Is.EqualTo("urn:requested"));
            Assert.That(MessageProcessor.ResolveProcessingTargetNamespace(
                "", null, namespaced), Is.EqualTo("urn:document"));
            Assert.That(MessageProcessor.ResolveProcessingTargetNamespace(
                "", Identifier("pacs.008.001.10"), null),
                Is.EqualTo(Namespace("pacs.008.001.10")));
            Assert.That(MessageProcessor.ResolveProcessingTargetNamespace(
                "", null, null), Is.Empty);
        });

        ProcessingResult diagnostics = new ProcessingResult();
        MessageProcessor.AddProcessingFailureDiagnostic(diagnostics, "code", "message");
        MessageProcessor.AddProcessingFailureDiagnostic(diagnostics, "code", "message");
        Assert.That(diagnostics.Diagnostics, Has.Count.EqualTo(1));
    }

    private static BuilderResult BuildPayment()
    {
        BuilderRequest request = new BuilderRequest
        {
            TemplateIdentifier = "interbank-credit-transfer",
            InterbankTransfer = new InterbankTransferTemplate
            {
                MessageIdentifier = "MSG-001",
                InstructionIdentifier = "INSTR-001",
                EndToEndIdentifier = "E2E-001",
                TransactionIdentifier = "TX-001",
                Uetr = "123e4567-e89b-42d3-a456-426614174000",
                Debtor = "Debtor",
                Creditor = "Creditor",
                SettlementAmount = new IsoAmountValue
                {
                    Currency = "USD",
                    Amount = new IsoDecimalValue { Value = 99.9900m, Scale = 4 }
                },
                SettlementDate = new IsoDateValue
                {
                    Year = 2026,
                    Month = 9,
                    Day = 16
                }
            },
            TargetMessageIdentifier = Identifier("pacs.008.001.10"),
            TargetNamespaceUri = Namespace("pacs.008.001.10"),
            ValidateResult = false,
            WriteOptions = new MessageWriteOptions { WriteXmlDeclaration = false }
        };
        return new MessageBuilder().BuildMessage(request);
    }

    private static ProcessingRequest CombinationRequest(int mask)
    {
        MessageIdentifier source = Identifier("pacs.008.001.10");
        MessageIdentifier target = Identifier("pacs.008.001.11");
        ProcessingRequest request = BaseProcessingRequest();
        int stageValue = (int)ProcessingStage.Detection;
        while (stageValue <= (int)ProcessingStage.Writing)
        {
            ProcessingStage stage = (ProcessingStage)stageValue;
            if (StageSelected(mask, stage))
            {
                request.Stages.Add(stage);
            }
            stageValue++;
        }
        if (StageSelected(mask, ProcessingStage.Validation))
        {
            request.Validation = new ValidationRequest
            {
                Policy = Policy(ValidationStage.EnvelopeAndHeader),
                Context = new ValidationContext()
            };
        }
        if (StageSelected(mask, ProcessingStage.Migration))
        {
            request.TargetMessageIdentifier = target;
            request.TargetNamespaceUri = Namespace(target.Value);
            request.Migration = Migration(source, target, PaymentAmountPath());
        }
        if (StageSelected(mask, ProcessingStage.Mapping))
        {
            MessageIdentifier mappingSource =
                StageSelected(mask, ProcessingStage.Migration) ? target : source;
            request.TargetMessageIdentifier = target;
            request.TargetNamespaceUri = Namespace(target.Value);
            request.Mapping = Mapping(
                mappingSource,
                target,
                StageSelected(mask, ProcessingStage.Migration) ?
                    "/Document/Amount" : PaymentAmountPath());
        }
        return request;
    }

    private static ProcessingRequest BaseProcessingRequest()
    {
        return new ProcessingRequest
        {
            Xml = BuildPayment().Xml,
            ExpectedSourceMessageIdentifier = Identifier("pacs.008.001.10"),
            ExpectedSourceNamespaceUri = Namespace("pacs.008.001.10"),
            WriteOptions = new MessageWriteOptions { WriteXmlDeclaration = false }
        };
    }

    private static ProcessingStage[] SelectedStages(int mask)
    {
        List<ProcessingStage> stages = new List<ProcessingStage>();
        int stageValue = (int)ProcessingStage.Detection;
        while (stageValue <= (int)ProcessingStage.Writing)
        {
            ProcessingStage stage = (ProcessingStage)stageValue;
            if (StageSelected(mask, stage))
            {
                stages.Add(stage);
            }
            stageValue++;
        }
        return stages.ToArray();
    }

    private static bool StageSelected(int mask, ProcessingStage stage)
    {
        int bit = 1 << ((int)stage - 1);
        return (mask & bit) != 0;
    }

    private static string PaymentAmountPath()
    {
        return "/Document/FIToFICstmrCdtTrf/CdtTrfTxInf/IntrBkSttlmAmt";
    }

    private static void AssertStageReports(ProcessingResult result, int mask)
    {
        Assert.That(result.Detection != null,
            Is.EqualTo(StageSelected(mask, ProcessingStage.Detection)));
        Assert.That(result.Read != null,
            Is.EqualTo(StageSelected(mask, ProcessingStage.Reading)));
        Assert.That(result.Validation != null,
            Is.EqualTo(StageSelected(mask, ProcessingStage.Validation)));
        Assert.That(result.Metadata != null,
            Is.EqualTo(StageSelected(mask, ProcessingStage.MetadataExtraction)));
        Assert.That(result.Migration != null,
            Is.EqualTo(StageSelected(mask, ProcessingStage.Migration)));
        Assert.That(result.Mapping != null,
            Is.EqualTo(StageSelected(mask, ProcessingStage.Mapping)));
        Assert.That(result.Write != null,
            Is.EqualTo(StageSelected(mask, ProcessingStage.Writing)));
    }

    private static MigrationRequest Migration(
        MessageIdentifier source,
        MessageIdentifier target,
        string sourcePath)
    {
        MigrationStep step = new MigrationStep
        {
            Sequence = 0,
            Action = MigrationActionKind.Copy,
            Required = true
        };
        step.SourcePaths.Add(Path(sourcePath));
        step.TargetPaths.Add(Path("/Document/Amount"));
        MigrationPlan plan = new MigrationPlan
        {
            Success = true,
            SourceMessageIdentifier = source,
            TargetMessageIdentifier = target
        };
        plan.Steps.Add(step);
        return new MigrationRequest
        {
            SourceMessageIdentifier = source,
            TargetMessageIdentifier = target,
            Plan = plan,
            ValidateTarget = false
        };
    }

    private static MappingRequest Mapping(
        MessageIdentifier source,
        MessageIdentifier target,
        string sourcePath)
    {
        MappingRuleDefinition rule = new MappingRuleDefinition
        {
            Identifier = "processor.amount",
            Sequence = 0,
            Action = MappingActionKind.Rename,
            Required = true
        };
        rule.SourcePaths.Add(Path(sourcePath));
        rule.TargetPaths.Add(Path("/Document/MappedAmount"));
        MappingDefinition definition = new MappingDefinition
        {
            Identifier = "processor.mapping",
            SourceMessageIdentifier = source,
            TargetMessageIdentifier = target,
            AllowUnmappedSource = true
        };
        definition.Rules.Add(rule);
        return new MappingRequest
        {
            Mapping = definition,
            TargetMessageIdentifier = target,
            ValidateTarget = false
        };
    }

    private static ValidationPolicy Policy(ValidationStage stage)
    {
        ValidationPolicy policy = new ValidationPolicy();
        policy.Stages.Add(stage);
        return policy;
    }

    private static XmlPathData Path(string value)
    {
        return new XmlPathData { Value = value };
    }

    private static string MessageXml(string identifier, string content)
    {
        return "<Document xmlns=\"" + Namespace(identifier) + "\">" +
            content + "</Document>";
    }

    private static MessageIdentifier Identifier(string value)
    {
        string[] parts = value.Split('.');
        return new MessageIdentifier
        {
            BusinessArea = parts[0],
            MessageFunctionality = parts[1],
            Variant = parts[2],
            Version = parts[3],
            Value = value
        };
    }

    private static string Namespace(string identifier)
    {
        return Iso20022Namespaces.SchemaUrnPrefix + identifier;
    }

    private sealed class RecordingValidator : IMessageValidator
    {
        private readonly ValidationReport report;

        public RecordingValidator(ValidationReport report)
        {
            this.report = report;
        }

        public ValidationRequest? Request;

        public ValidationReport ValidateMessage(ValidationRequest? request)
        {
            Request = request;
            return report;
        }
    }
}
