using iso20022;

namespace iso20022.Tests;

public class ValidationApiBehaviorTests
{
    [TestCase(ValidationStage.SecureXml)]
    [TestCase(ValidationStage.EnvelopeAndHeader)]
    [TestCase(ValidationStage.Xsd)]
    [TestCase(ValidationStage.Structural)]
    [TestCase(ValidationStage.PrimitiveFacet)]
    [TestCase(ValidationStage.InternalCode)]
    [TestCase(ValidationStage.ExternalCode)]
    [TestCase(ValidationStage.RepositoryConstraint)]
    [TestCase(ValidationStage.Profile)]
    [TestCase(ValidationStage.SupplementaryData)]
    [TestCase(ValidationStage.Signature)]
    [TestCase(ValidationStage.Semantic)]
    public void EveryValidationStageCanBeSelectedIndividually(
        ValidationStage stage)
    {
        ValidationRequest request = CreateRequest();
        request.Policy.Stages.Add(stage);

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True, report.ErrorMessage);
            Assert.That(report.Valid, Is.True);
            Assert.That(report.Diagnostics, Is.Empty);
            Assert.That(report.CompletedStages, Is.EqualTo(new[] { stage }));
            Assert.That(report.StoppedEarly, Is.False);
        });
    }

    [Test]
    public void InformationWarningAndErrorSeveritiesUseIndependentCounts()
    {
        ValidationRequest request = CreateRequest();
        request.Policy.Stages.Add(ValidationStage.Semantic);
        request.SemanticRules.Add(CreateRule(
            "severity.information",
            1,
            CreateDiagnostic(ValidationSeverity.Information)));
        request.SemanticRules.Add(CreateRule(
            "severity.warning",
            2,
            CreateDiagnostic(ValidationSeverity.Warning)));
        request.SemanticRules.Add(CreateRule(
            "severity.error",
            3,
            CreateDiagnostic(ValidationSeverity.Error)));

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True, report.ErrorMessage);
            Assert.That(report.Valid, Is.False);
            Assert.That(report.InformationCount, Is.EqualTo(1));
            Assert.That(report.WarningCount, Is.EqualTo(1));
            Assert.That(report.ErrorCount, Is.EqualTo(1));
            Assert.That(report.Diagnostics.Select(item => item.Severity), Is.EqualTo(new[]
            {
                ValidationSeverity.Information,
                ValidationSeverity.Warning,
                ValidationSeverity.Error
            }));
            Assert.That(report.Diagnostics.Select(item => item.Stage),
                Has.All.EqualTo(ValidationStage.Semantic));
            Assert.That(report.StoppedEarly, Is.False);
        });
    }

    [Test]
    public void WarningPolicySwitchesBetweenCollectAndStrictFailFastModes()
    {
        ReusableDiagnosticRule firstRule = CreateRule(
            "warning.first",
            1,
            CreateDiagnostic(ValidationSeverity.Warning));
        ReusableDiagnosticRule secondRule = CreateRule(
            "warning.second",
            2,
            CreateDiagnostic(ValidationSeverity.Warning));
        ValidationRequest collectRequest = CreateRequest();
        collectRequest.Policy.Stages.Add(ValidationStage.Semantic);
        collectRequest.SemanticRules.Add(firstRule);
        collectRequest.SemanticRules.Add(secondRule);
        ValidationRequest strictRequest = CreateRequest();
        strictRequest.Policy.Stages.Add(ValidationStage.Semantic);
        strictRequest.Policy.TreatWarningsAsErrors = true;
        strictRequest.Policy.FailFast = true;
        strictRequest.SemanticRules.Add(firstRule);
        strictRequest.SemanticRules.Add(secondRule);

        ValidationReport collected = new MessageValidator().ValidateMessage(
            collectRequest);
        ValidationReport strict = new MessageValidator().ValidateMessage(
            strictRequest);

        Assert.Multiple(() =>
        {
            Assert.That(collected.Success, Is.True, collected.ErrorMessage);
            Assert.That(collected.Valid, Is.True);
            Assert.That(collected.WarningCount, Is.EqualTo(2));
            Assert.That(collected.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(collected.StoppedEarly, Is.False);
            Assert.That(strict.Success, Is.True, strict.ErrorMessage);
            Assert.That(strict.Valid, Is.False);
            Assert.That(strict.WarningCount, Is.EqualTo(1));
            Assert.That(strict.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(strict.Diagnostics.Single().RuleIdentifier,
                Is.EqualTo("warning.first"));
            Assert.That(strict.StoppedEarly, Is.True);
            Assert.That(firstRule.InvocationCount, Is.EqualTo(2));
            Assert.That(secondRule.InvocationCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void RedactionModesPreserveCallerDiagnosticAndAllProvenanceFields()
    {
        ArtifactManifest artifact = new ArtifactManifest
        {
            Coordinate = new ArtifactCoordinate
            {
                Kind = ArtifactKind.Repository,
                Identifier = "repository",
                Edition = "2026"
            },
            IsApproved = true
        };
        ValidationDiagnostic callerDiagnostic = new ValidationDiagnostic
        {
            Code = "validation.caller.failed",
            Severity = ValidationSeverity.Error,
            Kind = DiagnosticKind.InvalidValue,
            Message = "The caller rule failed.",
            Path = new XmlPathData
            {
                Value = "/Document/Payment/Amount"
            },
            Location = new SourceLocation
            {
                SourceName = "payment.xml",
                HasLineInformation = true,
                LineNumber = 21,
                ColumnNumber = 8,
                HasCharacterOffset = true,
                CharacterOffset = 412
            },
            OffendingValue = "TOP-SECRET",
            ExpectedConstraint = "A permitted amount.",
            SourceArtifact = artifact,
            SourceIdentifier = "repository-rule",
            RepairHint = "Replace the amount."
        };
        callerDiagnostic.Path.Segments.Add(new QualifiedNameData
        {
            NamespaceUri = "urn:example:payment",
            LocalName = "Amount",
            Prefix = "p"
        });
        ReusableDiagnosticRule rule = CreateRule(
            "caller.provenance",
            1,
            callerDiagnostic);
        ValidationRequest redactedRequest = CreateRequest();
        redactedRequest.Policy.Stages.Add(ValidationStage.Semantic);
        redactedRequest.SemanticRules.Add(rule);
        ValidationRequest visibleRequest = CreateRequest();
        visibleRequest.Policy.Stages.Add(ValidationStage.Semantic);
        visibleRequest.Policy.RedactOffendingValues = false;
        visibleRequest.SemanticRules.Add(rule);

        ValidationReport redacted = new MessageValidator().ValidateMessage(
            redactedRequest);
        ValidationReport visible = new MessageValidator().ValidateMessage(
            visibleRequest);
        ValidationDiagnostic redactedValue = redacted.Diagnostics.Single();
        ValidationDiagnostic visibleValue = visible.Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(redactedValue.OffendingValue, Is.Empty);
            Assert.That(redactedValue.OffendingValueRedacted, Is.True);
            Assert.That(visibleValue.OffendingValue, Is.EqualTo("TOP-SECRET"));
            Assert.That(visibleValue.OffendingValueRedacted, Is.False);
            Assert.That(callerDiagnostic.OffendingValue, Is.EqualTo("TOP-SECRET"));
            Assert.That(callerDiagnostic.OffendingValueRedacted, Is.False);
            Assert.That(redactedValue, Is.Not.SameAs(callerDiagnostic));
            Assert.That(redactedValue.Path, Is.Not.SameAs(callerDiagnostic.Path));
            Assert.That(redactedValue.Location,
                Is.Not.SameAs(callerDiagnostic.Location));
            Assert.That(visibleValue.Path.Value,
                Is.EqualTo("/Document/Payment/Amount"));
            Assert.That(visibleValue.Path.Segments.Single().NamespaceUri,
                Is.EqualTo("urn:example:payment"));
            Assert.That(visibleValue.Location.SourceName,
                Is.EqualTo("payment.xml"));
            Assert.That(visibleValue.Location.LineNumber, Is.EqualTo(21));
            Assert.That(visibleValue.Location.ColumnNumber, Is.EqualTo(8));
            Assert.That(visibleValue.Location.CharacterOffset, Is.EqualTo(412));
            Assert.That(visibleValue.SourceArtifact, Is.SameAs(artifact));
            Assert.That(visibleValue.SourceIdentifier,
                Is.EqualTo("repository-rule"));
            Assert.That(visibleValue.RepairHint, Is.EqualTo("Replace the amount."));
        });
    }

    [Test]
    public void OversizedSemanticResultReturnsExceptionlessLimitFailure()
    {
        ValidationRequest request = CreateRequest();
        request.Policy.Stages.Add(ValidationStage.Semantic);
        request.Policy.Limits.MaxCollectionCount = 2;
        List<ValidationDiagnostic> diagnostics = new List<ValidationDiagnostic>
        {
            CreateDiagnostic(ValidationSeverity.Error),
            CreateDiagnostic(ValidationSeverity.Error),
            CreateDiagnostic(ValidationSeverity.Error)
        };
        request.SemanticRules.Add(new ReusableDiagnosticRule(
            CreateDescriptor("limit.rule", -1),
            diagnostics));

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.False);
            Assert.That(report.Valid, Is.False);
            Assert.That(report.StoppedEarly, Is.True);
            Assert.That(report.ErrorCode,
                Is.EqualTo("validation.semantic_rule.result_limit"));
            Assert.That(report.Diagnostics.Single().Kind,
                Is.EqualTo(DiagnosticKind.Configuration));
            Assert.That(report.Diagnostics.Single().Stage,
                Is.EqualTo(ValidationStage.Semantic));
            Assert.That(report.CompletedStages,
                Is.EqualTo(new[] { ValidationStage.Semantic }));
        });
    }

    [Test]
    public void MissingSemanticResultReturnsExceptionlessOperationFailure()
    {
        ValidationRequest request = CreateRequest();
        request.Policy.Stages.Add(ValidationStage.Semantic);
        request.SemanticRules.Add(new MissingResultRule());

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.False);
            Assert.That(report.Valid, Is.False);
            Assert.That(report.ErrorCode,
                Is.EqualTo("validation.semantic_rule.result_missing"));
            Assert.That(report.Diagnostics.Single().Code,
                Is.EqualTo("validation.semantic_rule.result_missing"));
            Assert.That(report.Diagnostics.Single().Severity,
                Is.EqualTo(ValidationSeverity.Error));
            Assert.That(report.StoppedEarly, Is.True);
        });
    }

    private static ValidationRequest CreateRequest()
    {
        return new ValidationRequest
        {
            Document = new MessageDocument()
        };
    }

    private static ReusableDiagnosticRule CreateRule(
        string identifier,
        int order,
        ValidationDiagnostic diagnostic)
    {
        return new ReusableDiagnosticRule(
            CreateDescriptor(identifier, order),
            new List<ValidationDiagnostic> { diagnostic });
    }

    private static SemanticRuleDescriptor CreateDescriptor(
        string identifier,
        int order)
    {
        return new SemanticRuleDescriptor
        {
            Identifier = identifier,
            Name = identifier,
            Definition = identifier + " must pass.",
            Severity = ValidationSeverity.Error,
            Order = order,
            Enabled = true,
            SourceIdentifier = "caller"
        };
    }

    private static ValidationDiagnostic CreateDiagnostic(
        ValidationSeverity severity)
    {
        return new ValidationDiagnostic
        {
            Code = "validation.caller.failed",
            Severity = severity,
            Kind = DiagnosticKind.ValidationFailure,
            Message = "The caller rule failed."
        };
    }

    private class ReusableDiagnosticRule : ISemanticRule
    {
        private SemanticRuleDescriptor descriptor;
        private List<ValidationDiagnostic> diagnostics;

        internal int InvocationCount;

        internal ReusableDiagnosticRule(
            SemanticRuleDescriptor descriptorValue,
            List<ValidationDiagnostic> diagnosticValues)
        {
            descriptor = descriptorValue;
            diagnostics = diagnosticValues;
        }

        public SemanticRuleDescriptor GetSemanticRuleDescriptor()
        {
            return descriptor;
        }

        public List<ValidationDiagnostic> ValidateSemanticRule(
            ValidationRequest? request,
            XmlPathData? path)
        {
            InvocationCount++;
            return diagnostics;
        }
    }

    private class MissingResultRule : ISemanticRule
    {
        public SemanticRuleDescriptor GetSemanticRuleDescriptor()
        {
            return CreateDescriptor("missing.result", -1);
        }

        public List<ValidationDiagnostic> ValidateSemanticRule(
            ValidationRequest? request,
            XmlPathData? path)
        {
            return null!;
        }
    }
}
