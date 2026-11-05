using iso20022;

namespace iso20022.Tests;

public class HighLevelContractDataTests
{
    [Test]
    public void BuilderContractKeepsExactTargetAndValidationDirectlyVisible()
    {
        MessageIdentifier target = Identifier("pacs.008.001.10");
        ValidationReport validation = new ValidationReport
        {
            Success = true,
            Valid = true,
            MessageIdentifier = target,
            NamespaceUri = Namespace("pacs.008.001.10")
        };
        BuilderRequest request = new BuilderRequest
        {
            TemplateIdentifier = "interbank-credit-transfer",
            SemanticInput = new IsoAmountValue
            {
                Amount = new IsoDecimalValue { Value = 125.2500m, Scale = 4 },
                Currency = "USD"
            },
            TargetMessageIdentifier = target,
            TargetNamespaceUri = Namespace("pacs.008.001.10"),
            ValidateResult = true
        };
        BuilderResult expected = new BuilderResult
        {
            Success = true,
            TemplateIdentifier = request.TemplateIdentifier,
            TargetMessageIdentifier = target,
            TargetNamespaceUri = request.TargetNamespaceUri,
            Document = new MessageDocument(),
            Validation = validation
        };
        ContractStub stub = new ContractStub(expected, new ProjectionResult(),
            new ProcessingResult());
        IMessageBuilder builder = stub;

        BuilderResult actual = builder.BuildMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(stub.BuilderRequest, Is.SameAs(request));
            Assert.That(actual.TargetMessageIdentifier, Is.SameAs(target));
            Assert.That(actual.TargetMessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(actual.TargetNamespaceUri,
                Is.EqualTo(Namespace("pacs.008.001.10")));
            Assert.That(actual.Validation, Is.SameAs(validation));
            Assert.That(actual.Validation?.Valid, Is.True);
        });
    }

    [Test]
    public void ProjectionContractPreservesSourceIdentityPathsPrecisionAndUnmappedData()
    {
        MessageIdentifier source = Identifier("camt.053.001.13");
        XmlPathData sourcePath = new XmlPathData
        {
            Value = "/Document/BkToCstmrStmt/Stmt/Bal/Amt"
        };
        RawXmlFragment unmapped = new RawXmlFragment
        {
            Xml = "<Extension xmlns='urn:example:extension'>kept</Extension>"
        };
        IsoAmountValue view = new IsoAmountValue
        {
            Amount = new IsoDecimalValue
            {
                Value = 10.2300m,
                Scale = 4,
                LexicalValue = "10.2300"
            },
            Currency = "EUR"
        };
        ValidationReport validation = new ValidationReport
        {
            Success = true,
            Valid = true,
            MessageIdentifier = source
        };
        ProjectionRequest request = new ProjectionRequest
        {
            ProjectionIdentifier = "cash-statement",
            ExpectedSourceMessageIdentifier = source,
            ExpectedSourceNamespaceUri = Namespace("camt.053.001.13"),
            ValidateSource = true
        };
        ProjectionResult expected = new ProjectionResult
        {
            Success = true,
            ProjectionIdentifier = request.ProjectionIdentifier,
            SourceMessageIdentifier = source,
            SourceNamespaceUri = request.ExpectedSourceNamespaceUri,
            View = view,
            SourceValidation = validation
        };
        expected.SourcePaths.Add(sourcePath);
        expected.UnmappedContent.Add(unmapped);
        ContractStub stub = new ContractStub(new BuilderResult(), expected,
            new ProcessingResult());
        IMessageProjector projector = stub;

        ProjectionResult actual = projector.ProjectMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(stub.ProjectionRequest, Is.SameAs(request));
            Assert.That(actual.SourceMessageIdentifier, Is.SameAs(source));
            Assert.That(actual.SourceNamespaceUri,
                Is.EqualTo(Namespace("camt.053.001.13")));
            Assert.That(actual.SourcePaths.Single(), Is.SameAs(sourcePath));
            Assert.That(actual.UnmappedContent.Single(), Is.SameAs(unmapped));
            Assert.That(((IsoAmountValue?)actual.View)?.Amount.Scale, Is.EqualTo(4));
            Assert.That(((IsoAmountValue?)actual.View)?.Amount.LexicalValue,
                Is.EqualTo("10.2300"));
            Assert.That(actual.SourceValidation, Is.SameAs(validation));
        });
    }

    [Test]
    public void ProcessorContractRetainsEveryStageReportAndExactTargetIdentity()
    {
        MessageIdentifier source = Identifier("pacs.008.001.09");
        MessageIdentifier target = Identifier("pacs.008.001.10");
        ValidationReport sourceValidation = new ValidationReport
        {
            Success = true,
            Valid = true,
            MessageIdentifier = source
        };
        ValidationReport targetValidation = new ValidationReport
        {
            Success = true,
            Valid = true,
            MessageIdentifier = target
        };
        MigrationResult migration = new MigrationResult
        {
            Success = true,
            SourceMessageIdentifier = source,
            TargetMessageIdentifier = target,
            TargetValidation = targetValidation
        };
        ProcessingRequest request = new ProcessingRequest
        {
            ExpectedSourceMessageIdentifier = source,
            ExpectedSourceNamespaceUri = Namespace("pacs.008.001.09"),
            TargetMessageIdentifier = target,
            TargetNamespaceUri = Namespace("pacs.008.001.10")
        };
        request.Stages.Add(ProcessingStage.Detection);
        request.Stages.Add(ProcessingStage.Reading);
        request.Stages.Add(ProcessingStage.Validation);
        request.Stages.Add(ProcessingStage.Migration);
        request.Stages.Add(ProcessingStage.Writing);
        ProcessingResult expected = new ProcessingResult
        {
            Success = true,
            SourceMessageIdentifier = source,
            SourceNamespaceUri = request.ExpectedSourceNamespaceUri,
            TargetMessageIdentifier = target,
            TargetNamespaceUri = request.TargetNamespaceUri,
            Detection = new MessageDetectionResult { Success = true },
            Read = new MessageReadResult { Success = true },
            Validation = sourceValidation,
            Migration = migration,
            Write = new MessageWriteResult { Success = true }
        };
        expected.RequestedStages.AddRange(request.Stages);
        expected.CompletedStages.AddRange(request.Stages);
        expected.ValidationReports.Add(sourceValidation);
        expected.ValidationReports.Add(targetValidation);
        ContractStub stub = new ContractStub(new BuilderResult(),
            new ProjectionResult(), expected);
        IMessageProcessor processor = stub;

        ProcessingResult actual = processor.ProcessMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(stub.ProcessingRequest, Is.SameAs(request));
            Assert.That(actual.RequestedStages, Is.EqualTo(request.Stages));
            Assert.That(actual.CompletedStages, Is.EqualTo(request.Stages));
            Assert.That(actual.SourceMessageIdentifier, Is.SameAs(source));
            Assert.That(actual.TargetMessageIdentifier, Is.SameAs(target));
            Assert.That(actual.TargetNamespaceUri,
                Is.EqualTo(Namespace("pacs.008.001.10")));
            Assert.That(actual.Validation, Is.SameAs(sourceValidation));
            Assert.That(actual.Migration, Is.SameAs(migration));
            Assert.That(actual.Migration?.TargetValidation,
                Is.SameAs(targetValidation));
            Assert.That(actual.ValidationReports,
                Is.EqualTo(new[] { sourceValidation, targetValidation }));
            Assert.That(actual.Write?.Success, Is.True);
        });
    }

    [Test]
    public void MutableHighLevelDefaultsAreIndependent()
    {
        BuilderRequest firstBuilder = new BuilderRequest();
        BuilderRequest secondBuilder = new BuilderRequest();
        ProjectionResult firstProjection = new ProjectionResult();
        ProjectionResult secondProjection = new ProjectionResult();
        ProcessingRequest firstProcessing = new ProcessingRequest();
        ProcessingRequest secondProcessing = new ProcessingRequest();
        ProcessingResult firstResult = new ProcessingResult();
        ProcessingResult secondResult = new ProcessingResult();

        firstBuilder.ValidationPolicy.Stages.Add(ValidationStage.Xsd);
        firstProjection.SourcePaths.Add(new XmlPathData());
        firstProjection.Metadata.References.Add("REFERENCE-1");
        firstProcessing.Stages.Add(ProcessingStage.MetadataExtraction);
        firstProcessing.MetadataOptions.AdditionalReferencePaths.Add(new XmlPathData());
        firstResult.CompletedStages.Add(ProcessingStage.Reading);
        firstResult.ValidationReports.Add(new ValidationReport());

        Assert.Multiple(() =>
        {
            Assert.That(secondBuilder.ValidationPolicy.Stages, Is.Empty);
            Assert.That(secondProjection.SourcePaths, Is.Empty);
            Assert.That(secondProjection.Metadata.References, Is.Empty);
            Assert.That(secondProcessing.Stages, Is.Empty);
            Assert.That(secondProcessing.MetadataOptions.AdditionalReferencePaths, Is.Empty);
            Assert.That(secondResult.CompletedStages, Is.Empty);
            Assert.That(secondResult.ValidationReports, Is.Empty);
            Assert.That(firstBuilder.Limits, Is.Not.SameAs(secondBuilder.Limits));
            Assert.That(firstProjection.Metadata,
                Is.Not.SameAs(secondProjection.Metadata));
            Assert.That(firstProcessing.ReadOptions,
                Is.Not.SameAs(secondProcessing.ReadOptions));
        });
    }

    private static MessageIdentifier Identifier(string value)
    {
        MessageIdentifierParseResult result =
            new MessageIdentifierParser().ParseIdentifier(value);
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        return result.Identifier ?? throw new AssertionException("Expected an identifier.");
    }

    private static string Namespace(string identifier)
    {
        return Iso20022Namespaces.SchemaUrnPrefix + identifier;
    }

    private sealed class ContractStub : IMessageBuilder, IMessageProjector, IMessageProcessor
    {
        readonly BuilderResult builderResult;
        readonly ProjectionResult projectionResult;
        readonly ProcessingResult processingResult;

        internal ContractStub(
            BuilderResult builderResult,
            ProjectionResult projectionResult,
            ProcessingResult processingResult)
        {
            this.builderResult = builderResult;
            this.projectionResult = projectionResult;
            this.processingResult = processingResult;
        }

        internal BuilderRequest? BuilderRequest;

        internal ProjectionRequest? ProjectionRequest;

        internal ProcessingRequest? ProcessingRequest;

        public BuilderResult BuildMessage(BuilderRequest? request)
        {
            BuilderRequest = request;
            return builderResult;
        }

        public ProjectionResult ProjectMessage(ProjectionRequest? request)
        {
            ProjectionRequest = request;
            return projectionResult;
        }

        public ProcessingResult ProcessMessage(ProcessingRequest? request)
        {
            ProcessingRequest = request;
            return processingResult;
        }
    }
}
