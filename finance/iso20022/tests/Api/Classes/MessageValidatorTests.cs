using iso20022;

namespace iso20022.Tests;

public class MessageValidatorTests
{
    [Test]
    public void DefaultPipelineRunsEveryStageInDeterministicOrderAndResolvesIdentity()
    {
        MessageIdentifier identifier = Identifier("pacs", "008", "001", "10");
        string namespaceUri = "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10";
        ValidationRequest request = DocumentRequest(identifier, namespaceUri);
        IMessageValidator validator = new MessageValidator();

        ValidationReport report = validator.ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True, report.ErrorMessage);
            Assert.That(report.Valid, Is.True);
            Assert.That(report.Diagnostics, Is.Empty);
            Assert.That(report.CompletedStages, Is.EqualTo(new[]
            {
                ValidationStage.SecureXml,
                ValidationStage.EnvelopeAndHeader,
                ValidationStage.Xsd,
                ValidationStage.Structural,
                ValidationStage.PrimitiveFacet,
                ValidationStage.InternalCode,
                ValidationStage.ExternalCode,
                ValidationStage.RepositoryConstraint,
                ValidationStage.Profile,
                ValidationStage.SupplementaryData,
                ValidationStage.Signature,
                ValidationStage.Semantic
            }));
            Assert.That(report.MessageIdentifier, Is.SameAs(identifier));
            Assert.That(report.NamespaceUri, Is.EqualTo(namespaceUri));
            Assert.That(report.StoppedEarly, Is.False);
        });
    }

    [Test]
    public void ExplicitStageSelectionIsDeduplicatedAndRunsInPipelineOrder()
    {
        ValidationRequest request = DocumentRequest(
            Identifier("camt", "053", "001", "12"),
            "urn:iso:std:iso:20022:tech:xsd:camt.053.001.12");
        request.Policy.Stages.Add(ValidationStage.Semantic);
        request.Policy.Stages.Add(ValidationStage.Xsd);
        request.Policy.Stages.Add(ValidationStage.SecureXml);
        request.Policy.Stages.Add(ValidationStage.Xsd);

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True, report.ErrorMessage);
            Assert.That(report.Valid, Is.True);
            Assert.That(report.CompletedStages, Is.EqualTo(new[]
            {
                ValidationStage.SecureXml,
                ValidationStage.Xsd,
                ValidationStage.Semantic
            }));
        });
    }

    [Test]
    public void CollectModeReturnsEveryIdentityDiagnosticAndContinuesLaterStages()
    {
        ValidationRequest request = MismatchedIdentityRequest();
        request.Policy.Stages.Add(ValidationStage.Semantic);
        request.Policy.Stages.Add(ValidationStage.EnvelopeAndHeader);
        request.Policy.Stages.Add(ValidationStage.Xsd);

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True, report.ErrorMessage);
            Assert.That(report.Valid, Is.False);
            Assert.That(report.Diagnostics.Select(item => item.Code), Is.EqualTo(new[]
            {
                "validation.identifier_mismatch",
                "validation.namespace_mismatch"
            }));
            Assert.That(report.Diagnostics.All(item =>
                item.Stage == ValidationStage.EnvelopeAndHeader), Is.True);
            Assert.That(report.Diagnostics.All(item =>
                item.Severity == ValidationSeverity.Error), Is.True);
            Assert.That(report.Diagnostics.All(item =>
                item.OffendingValueRedacted), Is.True);
            Assert.That(report.ErrorCount, Is.EqualTo(2));
            Assert.That(report.CompletedStages, Is.EqualTo(new[]
            {
                ValidationStage.EnvelopeAndHeader,
                ValidationStage.Xsd,
                ValidationStage.Semantic
            }));
            Assert.That(report.StoppedEarly, Is.False);
        });
    }

    [Test]
    public void FailFastModeStopsAfterFirstInvalidDiagnostic()
    {
        ValidationRequest request = MismatchedIdentityRequest();
        request.Policy.FailFast = true;
        request.Policy.Stages.Add(ValidationStage.Semantic);
        request.Policy.Stages.Add(ValidationStage.EnvelopeAndHeader);
        request.Policy.Stages.Add(ValidationStage.Xsd);

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True, report.ErrorMessage);
            Assert.That(report.Valid, Is.False);
            Assert.That(report.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(report.Diagnostics[0].Code,
                Is.EqualTo("validation.identifier_mismatch"));
            Assert.That(report.CompletedStages,
                Is.EqualTo(new[] { ValidationStage.EnvelopeAndHeader }));
            Assert.That(report.ErrorCount, Is.EqualTo(1));
            Assert.That(report.StoppedEarly, Is.True);
        });
    }

    [Test]
    public void DiagnosticLimitStopsCollectionWithoutExceedingTheBound()
    {
        ValidationRequest request = MismatchedIdentityRequest();
        request.Policy.Limits.MaxDiagnosticCount = 1;
        request.Policy.Stages.Add(ValidationStage.EnvelopeAndHeader);
        request.Policy.Stages.Add(ValidationStage.Semantic);

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.False);
            Assert.That(report.Valid, Is.False);
            Assert.That(report.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(report.Diagnostics[0].Code,
                Is.EqualTo("validation.identifier_mismatch"));
            Assert.That(report.DiagnosticLimitReached, Is.True);
            Assert.That(report.StoppedEarly, Is.True);
            Assert.That(report.ErrorCode, Is.EqualTo("validation.diagnostic_limit"));
            Assert.That(report.CompletedStages,
                Is.EqualTo(new[] { ValidationStage.EnvelopeAndHeader }));
        });
    }

    [Test]
    public void RawInputLimitReturnsAnExceptionlessResourceFailure()
    {
        ValidationRequest request = new ValidationRequest
        {
            Xml = "<Document/>"u8.ToArray()
        };
        request.Policy.Limits.MaxTotalBytes = request.Xml.Length - 1;
        request.Policy.Stages.Add(ValidationStage.SecureXml);

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.False);
            Assert.That(report.Valid, Is.False);
            Assert.That(report.ErrorCode, Is.EqualTo("validation.input_limit"));
            Assert.That(report.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(report.Diagnostics[0].Kind,
                Is.EqualTo(DiagnosticKind.ResourceLimit));
            Assert.That(report.Diagnostics[0].Stage,
                Is.EqualTo(ValidationStage.SecureXml));
            Assert.That(report.CompletedStages, Is.Empty);
            Assert.That(report.StoppedEarly, Is.True);
        });
    }

    [Test]
    public void ConfigurationFailuresReturnReportsInsteadOfThrowing()
    {
        MessageValidator validator = new MessageValidator();
        ValidationReport missingRequest = validator.ValidateMessage(null);
        ValidationReport missingRepresentation = validator.ValidateMessage(
            new ValidationRequest());
        ValidationRequest ambiguousRequest = DocumentRequest(
            Identifier("pacs", "008", "001", "10"),
            "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10");
        ambiguousRequest.Xml = "<Document/>"u8.ToArray();
        ValidationReport ambiguousRepresentation =
            validator.ValidateMessage(ambiguousRequest);
        ValidationRequest unknownStageRequest = DocumentRequest(
            Identifier("pacs", "008", "001", "10"),
            "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10");
        unknownStageRequest.Policy.Stages.Add(ValidationStage.Unknown);
        ValidationReport unknownStage = validator.ValidateMessage(
            unknownStageRequest);
        ValidationRequest invalidLimitRequest = DocumentRequest(
            Identifier("pacs", "008", "001", "10"),
            "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10");
        invalidLimitRequest.Policy.Limits.MaxDepth = 0;
        ValidationReport invalidLimits = validator.ValidateMessage(
            invalidLimitRequest);

        Assert.Multiple(() =>
        {
            AssertConfigurationFailure(missingRequest, "validation.request_missing");
            AssertConfigurationFailure(
                missingRepresentation,
                "validation.representation_invalid");
            AssertConfigurationFailure(
                ambiguousRepresentation,
                "validation.representation_invalid");
            AssertConfigurationFailure(unknownStage, "validation.stage_invalid");
            AssertConfigurationFailure(invalidLimits, "validation.limits_invalid");
        });
    }

    [Test]
    public void ExternalCodeEditionPolicyIsValidatedOnlyWhenItsStageIsSelected()
    {
        ValidationRequest pinned = DocumentRequest(
            Identifier("pain", "001", "001", "11"),
            "urn:iso:std:iso:20022:tech:xsd:pain.001.001.11");
        pinned.Policy.Stages.Add(ValidationStage.ExternalCode);
        pinned.Policy.SelectLatestExternalCodeEdition = false;
        pinned.Policy.ExternalCodeEdition = "2026-05-v1";
        ValidationReport pinnedReport = new MessageValidator().ValidateMessage(pinned);

        ValidationRequest conflicting = DocumentRequest(
            Identifier("pain", "001", "001", "11"),
            "urn:iso:std:iso:20022:tech:xsd:pain.001.001.11");
        conflicting.Policy.Stages.Add(ValidationStage.ExternalCode);
        conflicting.Policy.ExternalCodeEdition = "2026-05-v1";
        ValidationReport conflictingReport =
            new MessageValidator().ValidateMessage(conflicting);

        ValidationRequest unselected = DocumentRequest(
            Identifier("pain", "001", "001", "11"),
            "urn:iso:std:iso:20022:tech:xsd:pain.001.001.11");
        unselected.Policy.Stages.Add(ValidationStage.Structural);
        unselected.Policy.ExternalCodeEdition = "2026-05-v1";
        ValidationReport unselectedReport =
            new MessageValidator().ValidateMessage(unselected);

        Assert.Multiple(() =>
        {
            Assert.That(pinnedReport.Success, Is.True, pinnedReport.ErrorMessage);
            Assert.That(pinnedReport.CompletedStages,
                Is.EqualTo(new[] { ValidationStage.ExternalCode }));
            AssertConfigurationFailure(
                conflictingReport,
                "validation.external_code_policy_invalid");
            Assert.That(unselectedReport.Success, Is.True,
                unselectedReport.ErrorMessage);
            Assert.That(unselectedReport.CompletedStages,
                Is.EqualTo(new[] { ValidationStage.Structural }));
        });
    }

    [Test]
    public void DraftArtifactPolicyRunsInDeterministicSchemaThenProfileOrder()
    {
        ValidationRequest request = DocumentRequest(
            Identifier("pacs", "008", "001", "10"),
            "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10");
        request.Schema = new SchemaArtifact
        {
            SourceName = "draft-message.xsd",
            Artifact = new ArtifactManifest { IsApproved = false }
        };
        request.Profile = new ProfileDefinition
        {
            Identifier = "draft-profile",
            Version = "1",
            Artifact = new ArtifactManifest { IsApproved = false }
        };
        request.Policy.Stages.Add(ValidationStage.Profile);
        request.Policy.Stages.Add(ValidationStage.Xsd);

        ValidationReport rejected = new MessageValidator().ValidateMessage(request);
        request.Policy.AllowDraftArtifacts = true;
        ValidationReport allowed = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Success, Is.True, rejected.ErrorMessage);
            Assert.That(rejected.Valid, Is.False);
            Assert.That(rejected.Diagnostics.Select(item => item.Code), Is.EqualTo(new[]
            {
                "validation.schema_draft_disallowed",
                "validation.profile_draft_disallowed"
            }));
            Assert.That(rejected.Diagnostics[0].SourceArtifact,
                Is.SameAs(request.Schema.Artifact));
            Assert.That(rejected.Diagnostics[1].SourceArtifact,
                Is.SameAs(request.Profile.Artifact));
            Assert.That(rejected.CompletedStages, Is.EqualTo(new[]
            {
                ValidationStage.Xsd,
                ValidationStage.Profile
            }));
            Assert.That(allowed.Success, Is.True, allowed.ErrorMessage);
            Assert.That(allowed.Valid, Is.True);
            Assert.That(allowed.Diagnostics, Is.Empty);
            Assert.That(allowed.CompletedStages, Is.EqualTo(new[]
            {
                ValidationStage.Xsd,
                ValidationStage.Profile
            }));
        });
    }

    [Test]
    public void SemanticRulesUseDeterministicApplicabilityPathsAndCallerContext()
    {
        MessageIdentifier identifier = Identifier("pacs", "008", "001", "10");
        ValidationRequest request = DocumentRequest(
            identifier,
            "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10");
        request.Policy.Stages.Add(ValidationStage.Semantic);
        request.Context.ProfileIdentifier = "profile-a";
        object callerValue = new object();
        request.Context.CallerState.Add("directory", callerValue);
        List<string> calls = new List<string>();

        SemanticRuleDescriptor zDescriptor = SemanticDescriptor(
            "rule.z", 20, ValidationSeverity.Warning);
        zDescriptor.VersionSelector = new VersionSelector
        {
            Kind = VersionSelectionKind.Range,
            MinimumVersion = "09",
            MaximumVersion = "11"
        };
        zDescriptor.ProfileIdentifiers.Add("profile-a");
        zDescriptor.Paths.Add(new XmlPathData { Value = "/Document/B" });
        zDescriptor.Paths.Add(new XmlPathData { Value = "/Document/A" });
        RecordingSemanticRule zRule = new RecordingSemanticRule(
            zDescriptor, calls, callerValue);

        SemanticRuleDescriptor aDescriptor = SemanticDescriptor(
            "rule.a", 20, ValidationSeverity.Information);
        aDescriptor.VersionSelector = new VersionSelector
        {
            Kind = VersionSelectionKind.LatestCompatible,
            BaselineIdentifier = Identifier("pacs", "008", "001", "09")
        };
        RecordingSemanticRule aRule = new RecordingSemanticRule(
            aDescriptor, calls, callerValue);

        SemanticRuleDescriptor firstDescriptor = SemanticDescriptor(
            "rule.first", 10, ValidationSeverity.Warning);
        firstDescriptor.VersionSelector = new VersionSelector
        {
            Kind = VersionSelectionKind.Current
        };
        RecordingSemanticRule firstRule = new RecordingSemanticRule(
            firstDescriptor, calls, callerValue);

        SemanticRuleDescriptor wrongMessageDescriptor = SemanticDescriptor(
            "rule.wrong-message", 1, ValidationSeverity.Error);
        wrongMessageDescriptor.MessageIdentifiers.Add(
            Identifier("pacs", "009", "001", "10"));
        RecordingSemanticRule wrongMessage = new RecordingSemanticRule(
            wrongMessageDescriptor, calls, callerValue);

        SemanticRuleDescriptor wrongVersionDescriptor = SemanticDescriptor(
            "rule.wrong-version", 2, ValidationSeverity.Error);
        wrongVersionDescriptor.VersionSelector = new VersionSelector
        {
            Kind = VersionSelectionKind.Exact,
            BaselineIdentifier = Identifier("pacs", "008", "001", "09")
        };
        RecordingSemanticRule wrongVersion = new RecordingSemanticRule(
            wrongVersionDescriptor, calls, callerValue);

        SemanticRuleDescriptor wrongProfileDescriptor = SemanticDescriptor(
            "rule.wrong-profile", 3, ValidationSeverity.Error);
        wrongProfileDescriptor.ProfileIdentifiers.Add("profile-b");
        RecordingSemanticRule wrongProfile = new RecordingSemanticRule(
            wrongProfileDescriptor, calls, callerValue);

        SemanticRuleDescriptor disabledDescriptor = SemanticDescriptor(
            "rule.disabled", 0, ValidationSeverity.Error);
        disabledDescriptor.Enabled = false;
        RecordingSemanticRule disabled = new RecordingSemanticRule(
            disabledDescriptor, calls, callerValue);

        request.SemanticRules.Add(zRule);
        request.SemanticRules.Add(wrongMessage);
        request.SemanticRules.Add(aRule);
        request.SemanticRules.Add(disabled);
        request.SemanticRules.Add(wrongVersion);
        request.SemanticRules.Add(firstRule);
        request.SemanticRules.Add(wrongProfile);

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True, report.ErrorMessage);
            Assert.That(report.Valid, Is.True);
            Assert.That(report.CompletedStages,
                Is.EqualTo(new[] { ValidationStage.Semantic }));
            Assert.That(calls, Is.EqualTo(new[]
            {
                "rule.first|",
                "rule.a|",
                "rule.z|/Document/B",
                "rule.z|/Document/A"
            }));
            Assert.That(report.Diagnostics.Select(item => item.RuleIdentifier),
                Is.EqualTo(new[] { "rule.first", "rule.a", "rule.z", "rule.z" }));
            Assert.That(report.Diagnostics.Select(item => item.Path.Value),
                Is.EqualTo(new[] { string.Empty, string.Empty, "/Document/B", "/Document/A" }));
            Assert.That(report.Diagnostics.Select(item => item.Kind),
                Has.All.EqualTo(DiagnosticKind.ValidationFailure));
            Assert.That(report.WarningCount, Is.EqualTo(3));
            Assert.That(report.InformationCount, Is.EqualTo(1));
            Assert.That(report.ErrorCount, Is.Zero);
            Assert.That(firstRule.CallerValueMatched, Is.True);
            Assert.That(aRule.CallerValueMatched, Is.True);
            Assert.That(zRule.CallerValueMatched, Is.True);
            Assert.That(firstRule.LastRequest, Is.SameAs(request));
            Assert.That(wrongMessage.InvocationCount, Is.Zero);
            Assert.That(wrongVersion.InvocationCount, Is.Zero);
            Assert.That(wrongProfile.InvocationCount, Is.Zero);
            Assert.That(disabled.InvocationCount, Is.Zero);
        });
    }

    [Test]
    public void SemanticRuleCompositionAppliesProfileRemovalOverridesAndMissingRules()
    {
        ValidationRequest request = DocumentRequest(
            Identifier("pacs", "008", "001", "10"),
            "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10");
        request.Policy.Stages.Add(ValidationStage.Semantic);
        List<string> calls = new List<string>();
        RecordingSemanticRule removed = new RecordingSemanticRule(
            SemanticDescriptor("base.remove", 1, ValidationSeverity.Error),
            calls,
            null);
        SemanticRuleDescriptor overrideDescriptor = SemanticDescriptor(
            "base.override", 50, ValidationSeverity.Warning);
        overrideDescriptor.Enabled = false;
        RecordingSemanticRule overridden = new RecordingSemanticRule(
            overrideDescriptor,
            calls,
            null);
        RecordingSemanticRule remaining = new RecordingSemanticRule(
            SemanticDescriptor("base.remain", 10, ValidationSeverity.Warning),
            calls,
            null);
        request.SemanticRules.Add(remaining);
        request.SemanticRules.Add(removed);
        request.SemanticRules.Add(overridden);

        ProfileDefinition profile = new ProfileDefinition
        {
            Identifier = "profile-a",
            Version = "2026",
            Artifact = new ArtifactManifest
            {
                OriginUri = "https://example.test/profile-a.json",
                IsApproved = true
            }
        };
        profile.Rules.Add(new ProfileRuleReference
        {
            RuleIdentifier = "base.remove",
            Enabled = true,
            RemoveInheritedRule = true
        });
        SemanticRuleDescriptor profileOverride = SemanticDescriptor(
            "base.override", 40, ValidationSeverity.Warning);
        profileOverride.Definition = "The profile override must pass.";
        profileOverride.SourceIdentifier = "profile-a-source";
        profileOverride.Paths.Add(new XmlPathData { Value = "/Document/Amount" });
        profile.Rules.Add(new ProfileRuleReference
        {
            RuleIdentifier = "base.override",
            Descriptor = profileOverride,
            Enabled = true,
            HasSeverityOverride = true,
            Severity = ValidationSeverity.Error,
            HasOrderOverride = true,
            Order = 5
        });
        SemanticRuleDescriptor inline = SemanticDescriptor(
            "profile.inline", 1, ValidationSeverity.Warning);
        inline.Definition = "The inline profile rule requires an implementation.";
        inline.SourceIdentifier = "profile-a-source";
        profile.Rules.Add(new ProfileRuleReference
        {
            RuleIdentifier = "profile.inline",
            Descriptor = inline,
            Enabled = true
        });
        request.Profile = profile;

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True, report.ErrorMessage);
            Assert.That(report.Valid, Is.False);
            Assert.That(report.Diagnostics.Select(item => item.RuleIdentifier),
                Is.EqualTo(new[] { "profile.inline", "base.override", "base.remain" }));
            Assert.That(report.Diagnostics.Select(item => item.Severity),
                Is.EqualTo(new[]
                {
                    ValidationSeverity.Warning,
                    ValidationSeverity.Error,
                    ValidationSeverity.Warning
                }));
            Assert.That(report.Diagnostics[0].Code,
                Is.EqualTo("validation.semantic_rule.implementation_missing"));
            Assert.That(report.Diagnostics[0].Kind,
                Is.EqualTo(DiagnosticKind.Applicability));
            Assert.That(report.Diagnostics[0].SourceArtifact,
                Is.SameAs(profile.Artifact));
            Assert.That(report.Diagnostics[1].Path.Value,
                Is.EqualTo("/Document/Amount"));
            Assert.That(report.Diagnostics.Select(item => item.ProfileIdentifier),
                Has.All.EqualTo("profile-a"));
            Assert.That(report.Diagnostics.Select(item => item.ProfileVersion),
                Has.All.EqualTo("2026"));
            Assert.That(calls, Is.EqualTo(new[]
            {
                "base.override|/Document/Amount",
                "base.remain|"
            }));
            Assert.That(removed.InvocationCount, Is.Zero);
            Assert.That(overridden.InvocationCount, Is.EqualTo(1));
            Assert.That(remaining.InvocationCount, Is.EqualTo(1));
            Assert.That(report.WarningCount, Is.EqualTo(2));
            Assert.That(report.ErrorCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void DuplicateSemanticRuleIdentifiersReturnStableOperationFailure()
    {
        ValidationRequest request = DocumentRequest(
            Identifier("pacs", "008", "001", "10"),
            "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10");
        request.Policy.Stages.Add(ValidationStage.Semantic);
        List<string> calls = new List<string>();
        RecordingSemanticRule first = new RecordingSemanticRule(
            SemanticDescriptor("duplicate.rule", 1, ValidationSeverity.Error),
            calls,
            null);
        RecordingSemanticRule second = new RecordingSemanticRule(
            SemanticDescriptor("duplicate.rule", 2, ValidationSeverity.Warning),
            calls,
            null);
        request.SemanticRules.Add(first);
        request.SemanticRules.Add(second);

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.False);
            Assert.That(report.Valid, Is.False);
            Assert.That(report.StoppedEarly, Is.True);
            Assert.That(report.ErrorCode,
                Is.EqualTo("validation.semantic_rule.identifier_duplicate"));
            Assert.That(report.Diagnostics.Single().Kind,
                Is.EqualTo(DiagnosticKind.Configuration));
            Assert.That(report.Diagnostics.Single().Stage,
                Is.EqualTo(ValidationStage.Semantic));
            Assert.That(report.CompletedStages,
                Is.EqualTo(new[] { ValidationStage.Semantic }));
            Assert.That(first.InvocationCount, Is.Zero);
            Assert.That(second.InvocationCount, Is.Zero);
        });
    }

    [Test]
    public void RequestConfigurationAndSemanticVersionBoundariesAreExplicit()
    {
        MessageValidator validator = new MessageValidator();
        List<ValidationRequest> invalid = new List<ValidationRequest>
        {
            new ValidationRequest { Policy = null! },
            new ValidationRequest { Context = null! },
            new ValidationRequest { Xml = null! },
            new ValidationRequest { ExpectedNamespaceUri = null! },
            new ValidationRequest(),
            new ValidationRequest { Xml = "<x/>"u8.ToArray(), Document = new MessageDocument() },
            new ValidationRequest
            {
                Xml = "<x/>"u8.ToArray(),
                Policy = new ValidationPolicy { Limits = null! }
            },
            new ValidationRequest
            {
                Xml = "<x/>"u8.ToArray(),
                Policy = new ValidationPolicy { Stages = null! }
            }
        };
        foreach (ValidationRequest request in invalid)
        {
            Assert.That(validator.ValidateMessage(request).Success, Is.False);
        }

        ValidationRequest stageLimit = ConfiguredDocumentRequest(new ValidationPolicy());
        stageLimit.Policy.Limits.MaxCollectionCount = 1;
        stageLimit.Policy.Stages.Add(ValidationStage.SecureXml);
        stageLimit.Policy.Stages.Add(ValidationStage.Semantic);
        ValidationRequest unknownStage = ConfiguredDocumentRequest(new ValidationPolicy());
        unknownStage.Policy.Stages.Add((ValidationStage)99);
        ValidationRequest signature = ConfiguredDocumentRequest(new ValidationPolicy
        {
            RequireValidSignature = true,
            ValidatePresentSignatures = false
        });
        ValidationPolicy externalPolicy = new ValidationPolicy
        {
            SelectLatestExternalCodeEdition = false
        };
        externalPolicy.Stages.Add(ValidationStage.ExternalCode);
        ValidationRequest external = ConfiguredDocumentRequest(externalPolicy);
        ValidationRequest badIdentifier = ConfiguredDocumentRequest(new ValidationPolicy());
        badIdentifier.ExpectedIdentifier = new MessageIdentifier { Value = "bad" };
        Assert.Multiple(() =>
        {
            Assert.That(validator.ValidateMessage(stageLimit).ErrorCode,
                Is.EqualTo("validation.stage_limit"));
            Assert.That(validator.ValidateMessage(unknownStage).ErrorCode,
                Is.EqualTo("validation.stage_invalid"));
            Assert.That(validator.ValidateMessage(signature).ErrorCode,
                Is.EqualTo("validation.signature_policy_invalid"));
            Assert.That(validator.ValidateMessage(external).ErrorCode,
                Is.EqualTo("validation.external_code_policy_invalid"));
            Assert.That(validator.ValidateMessage(badIdentifier).ErrorCode,
                Is.EqualTo("validation.expected_identifier_invalid"));
        });

        MessageIdentifier v1 = Identifier("pacs", "008", "001", "01");
        MessageIdentifier v2 = Identifier("pacs", "008", "001", "02");
        Assert.Multiple(() =>
        {
            Assert.That(MessageValidator.SemanticVersionSelectorApplies(null, null), Is.True);
            Assert.That(MessageValidator.SemanticVersionSelectorApplies(
                new VersionSelector(), null), Is.False);
            Assert.That(MessageValidator.SemanticVersionSelectorApplies(
                new VersionSelector { Kind = VersionSelectionKind.Current }, v1), Is.True);
            Assert.That(MessageValidator.SemanticVersionSelectorApplies(
                new VersionSelector { Kind = (VersionSelectionKind)99 }, v1), Is.False);
            Assert.That(MessageValidator.SemanticVersionIsInRange("01", "02", ""), Is.False);
            Assert.That(MessageValidator.SemanticVersionIsInRange("03", "", "02"), Is.False);
            Assert.That(MessageValidator.CompareSemanticVersions("001", "1"), Is.Zero);
            Assert.That(MessageValidator.CompareSemanticVersions("10", "2"), Is.GreaterThan(0));
            Assert.That(MessageValidator.SemanticVersionsAreCompatible(v1, v2), Is.True);
            Assert.That(MessageValidator.GetIdentifierText(new MessageIdentifier()), Is.Empty);
        });

        ValidationRequest contextRequest = ConfiguredDocumentRequest(new ValidationPolicy());
        contextRequest.Document!.Metadata.MessageIdentifier = null;
        contextRequest.Document.Metadata.NamespaceUri = string.Empty;
        contextRequest.Context.MessageIdentifier = v1;
        contextRequest.Context.NamespaceUri = "urn:context";
        Assert.That(MessageValidator.FindActualIdentifier(contextRequest), Is.SameAs(v1));
        Assert.That(MessageValidator.FindActualNamespace(contextRequest),
            Is.EqualTo("urn:context"));
    }

    private static ValidationRequest ConfiguredDocumentRequest(ValidationPolicy policy)
    {
        ValidationRequest request = DocumentRequest(
            Identifier("pacs", "008", "001", "10"),
            Iso20022Namespaces.SchemaUrnPrefix + "pacs.008.001.10");
        request.Policy = policy;
        request.Context = new ValidationContext();
        return request;
    }

    private static SemanticRuleDescriptor SemanticDescriptor(
        string identifier,
        int order,
        ValidationSeverity severity)
    {
        return new SemanticRuleDescriptor
        {
            Identifier = identifier,
            Name = identifier,
            Definition = identifier + " must pass.",
            Order = order,
            Severity = severity,
            Enabled = true,
            SourceIdentifier = "caller-rules"
        };
    }

    private static ValidationRequest DocumentRequest(
        MessageIdentifier identifier,
        string namespaceUri)
    {
        return new ValidationRequest
        {
            Document = new MessageDocument
            {
                Metadata = new MessageMetadata
                {
                    MessageIdentifier = identifier,
                    NamespaceUri = namespaceUri
                }
            }
        };
    }

    private static ValidationRequest MismatchedIdentityRequest()
    {
        ValidationRequest request = DocumentRequest(
            Identifier("pacs", "008", "001", "10"),
            "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10");
        request.ExpectedIdentifier = Identifier("pacs", "009", "001", "11");
        request.ExpectedNamespaceUri =
            "urn:iso:std:iso:20022:tech:xsd:pacs.009.001.11";
        return request;
    }

    private static MessageIdentifier Identifier(
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

    private static void AssertConfigurationFailure(
        ValidationReport report,
        string code)
    {
        Assert.That(report.Success, Is.False);
        Assert.That(report.Valid, Is.False);
        Assert.That(report.ErrorCode, Is.EqualTo(code));
        Assert.That(report.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(report.Diagnostics[0].Kind,
            Is.EqualTo(DiagnosticKind.Configuration));
        Assert.That(report.Diagnostics[0].Severity,
            Is.EqualTo(ValidationSeverity.Error));
        Assert.That(report.CompletedStages, Is.Empty);
    }

    private class RecordingSemanticRule : ISemanticRule
    {
        private SemanticRuleDescriptor descriptor;
        private List<string> calls;
        private object? expectedCallerValue;

        internal int InvocationCount;
        internal bool CallerValueMatched = true;
        internal ValidationRequest? LastRequest;

        internal RecordingSemanticRule(
            SemanticRuleDescriptor descriptorValue,
            List<string> callValues,
            object? callerValue)
        {
            descriptor = descriptorValue;
            calls = callValues;
            expectedCallerValue = callerValue;
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
            LastRequest = request;
            string pathValue = path == null ? "<null>" : path.Value;
            calls.Add(descriptor.Identifier + "|" + pathValue);
            if (expectedCallerValue != null)
            {
                CallerValueMatched = request != null &&
                    request.Context.CallerState.ContainsKey("directory") &&
                    ReferenceEquals(
                        request.Context.CallerState["directory"],
                        expectedCallerValue);
            }
            return new List<ValidationDiagnostic>
            {
                new ValidationDiagnostic
                {
                    RuleIdentifier = "unstable-rule-id",
                    Severity = ValidationSeverity.Unknown,
                    Kind = DiagnosticKind.Unknown
                }
            };
        }
    }
}
