using iso20022;

namespace iso20022.Tests;

public class SemanticProfileApiTests
{
    private const string MessageNamespace =
        "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10";

    [Test]
    public void InheritedProfileRuleApplicabilityUsesOrderOverrideAndCallerState()
    {
        List<string> calls = new List<string>();
        object callerState = new object();
        SemanticRuleDescriptor profileDescriptor = CreateDescriptor(
            "profile.rule",
            50);
        profileDescriptor.ProfileIdentifiers.Add("base-profile");
        RecordingRule profileRule = new RecordingRule(
            profileDescriptor,
            calls,
            callerState);
        RecordingRule otherRule = new RecordingRule(
            CreateDescriptor("other.rule", 20),
            calls,
            callerState);
        ProfileDefinition baseProfile = CreateProfile("base-profile");
        baseProfile.Rules.Add(new ProfileRuleReference
        {
            RuleIdentifier = profileDescriptor.Identifier,
            Enabled = true,
            HasOrderOverride = true,
            Order = 5
        });
        ProfileDefinition derived = CreateProfile("derived-profile");
        derived.BaseProfileIdentifier = baseProfile.Identifier;
        ValidationRequest request = CreateDocumentRequest();
        request.Profile = derived;
        request.ProfileProvider = new ProfileProvider(
            new List<ProfileDefinition> { baseProfile });
        request.Context.CallerState.Add("state", callerState);
        request.SemanticRules.Add(otherRule);
        request.SemanticRules.Add(profileRule);
        request.Policy.Stages.Add(ValidationStage.Semantic);

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True, report.ErrorMessage);
            Assert.That(report.Valid, Is.True);
            Assert.That(report.Diagnostics, Is.Empty);
            Assert.That(calls, Is.EqualTo(new[]
            {
                "profile.rule",
                "other.rule"
            }));
            Assert.That(profileRule.CallerStateMatched, Is.True);
            Assert.That(otherRule.CallerStateMatched, Is.True);
            Assert.That(profileRule.LastRequest, Is.SameAs(request));
            Assert.That(report.ProfileIdentifier, Is.EqualTo("derived-profile"));
        });
    }

    [Test]
    public void DerivedProhibitionStrengthensInheritedAllowedValuesWithoutMutation()
    {
        ProfileDefinition baseProfile = CreateProfile("base-profile");
        ProfileConstraint baseConstraint = CreateValueConstraint("base.value");
        baseConstraint.AllowedValues.Add("A");
        baseConstraint.AllowedValues.Add("B");
        baseProfile.Constraints.Add(baseConstraint);
        ProfileDefinition derived = CreateProfile("derived-profile");
        derived.BaseProfileIdentifier = baseProfile.Identifier;
        ProfileConstraint overlay = CreateValueConstraint("derived.value");
        overlay.BaseConstraintIdentifier = baseConstraint.Identifier;
        overlay.RequirePath = false;
        overlay.HasMinimumOccurrences = false;
        overlay.HasMaximumOccurrences = false;
        overlay.ProhibitedValues.Add("B");
        derived.Constraints.Add(overlay);
        IProfileProvider provider = new ProfileProvider(
            new List<ProfileDefinition> { baseProfile });

        ValidationReport allowed = ValidateGenericProfile("A", derived, provider);
        ValidationReport prohibited = ValidateGenericProfile("B", derived, provider);

        Assert.Multiple(() =>
        {
            Assert.That(allowed.Success, Is.True, allowed.ErrorMessage);
            Assert.That(allowed.Valid, Is.True);
            Assert.That(allowed.Diagnostics, Is.Empty);
            Assert.That(prohibited.Success, Is.True, prohibited.ErrorMessage);
            Assert.That(prohibited.Valid, Is.False);
            Assert.That(prohibited.Diagnostics.Single().Code,
                Is.EqualTo("validation.profile.value_prohibited"));
            Assert.That(prohibited.Diagnostics.Single().RuleIdentifier,
                Is.EqualTo("derived.value"));
            Assert.That(prohibited.Diagnostics.Single().ProfileIdentifier,
                Is.EqualTo("derived-profile"));
            Assert.That(baseConstraint.AllowedValues,
                Is.EqualTo(new[] { "A", "B" }));
            Assert.That(baseConstraint.ProhibitedValues, Is.Empty);
            Assert.That(overlay.ProhibitedValues, Is.EqualTo(new[] { "B" }));
        });
    }

    [Test]
    public void ProhibitingEveryInheritedAllowedValueReportsOverlayConflict()
    {
        ProfileDefinition baseProfile = CreateProfile("base-profile");
        ProfileConstraint baseConstraint = CreateValueConstraint("base.value");
        baseConstraint.AllowedValues.Add("A");
        baseProfile.Constraints.Add(baseConstraint);
        ProfileDefinition derived = CreateProfile("derived-profile");
        derived.BaseProfileIdentifier = baseProfile.Identifier;
        ProfileConstraint overlay = CreateValueConstraint("derived.value");
        overlay.BaseConstraintIdentifier = baseConstraint.Identifier;
        overlay.RequirePath = false;
        overlay.HasMinimumOccurrences = false;
        overlay.HasMaximumOccurrences = false;
        overlay.ProhibitedValues.Add("A");
        derived.Constraints.Add(overlay);

        ValidationReport report = ValidateGenericProfile(
            "A",
            derived,
            new ProfileProvider(new List<ProfileDefinition> { baseProfile }));

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.False);
            Assert.That(report.Valid, Is.False);
            Assert.That(report.StoppedEarly, Is.True);
            Assert.That(report.ErrorCode,
                Is.EqualTo("validation.profile.constraint_conflict"));
            Assert.That(report.Diagnostics.Single().Kind,
                Is.EqualTo(DiagnosticKind.Conflict));
        });
    }

    [Test]
    public void TypedProfileConstraintReportsUnsupportedConstraintApplicability()
    {
        ProfileDefinition profile = CreateProfile("typed-profile");
        profile.Constraints.Add(CreateValueConstraint("typed.value"));
        ValidationRequest request = new ValidationRequest
        {
            Model = new object(),
            ExpectedIdentifier = new MessageIdentifier
            {
                Value = "pacs.008.001.10"
            },
            ExpectedNamespaceUri = MessageNamespace,
            Profile = profile
        };
        request.Policy.Stages.Add(ValidationStage.Profile);

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True, report.ErrorMessage);
            Assert.That(report.Valid, Is.True);
            Assert.That(report.WarningCount, Is.EqualTo(1));
            Assert.That(report.Diagnostics.Single().Code,
                Is.EqualTo("validation.profile.representation_unsupported"));
            Assert.That(report.Diagnostics.Single().Kind,
                Is.EqualTo(DiagnosticKind.UnsupportedConstraint));
            Assert.That(report.Diagnostics.Single().Stage,
                Is.EqualTo(ValidationStage.Profile));
            Assert.That(report.Diagnostics.Single().SourceArtifact,
                Is.SameAs(profile.Artifact));
        });
    }

    [Test]
    public void TargetVersionMismatchIsReportedOnceAndDoesNotRemoveCallerRule()
    {
        List<string> calls = new List<string>();
        RecordingRule callerRule = new RecordingRule(
            CreateDescriptor("caller.rule", 1),
            calls,
            null);
        ProfileDefinition profile = CreateProfile("future-profile");
        profile.BaseVersion = new VersionSelector
        {
            Kind = VersionSelectionKind.Range,
            MinimumVersion = "11",
            MaximumVersion = "12"
        };
        profile.Rules.Add(new ProfileRuleReference
        {
            RuleIdentifier = "caller.rule",
            Enabled = true,
            RemoveInheritedRule = true
        });
        ValidationRequest request = CreateDocumentRequest();
        request.Profile = profile;
        request.SemanticRules.Add(callerRule);
        request.Policy.Stages.Add(ValidationStage.Profile);
        request.Policy.Stages.Add(ValidationStage.Semantic);

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True, report.ErrorMessage);
            Assert.That(report.Valid, Is.False);
            Assert.That(report.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(report.Diagnostics.Single().Code,
                Is.EqualTo("validation.profile.target_mismatch"));
            Assert.That(report.Diagnostics.Single().Kind,
                Is.EqualTo(DiagnosticKind.Applicability));
            Assert.That(report.Diagnostics.Single().Stage,
                Is.EqualTo(ValidationStage.Profile));
            Assert.That(callerRule.InvocationCount, Is.EqualTo(1));
            Assert.That(calls, Is.EqualTo(new[] { "caller.rule" }));
            Assert.That(report.CompletedStages, Is.EqualTo(new[]
            {
                ValidationStage.Profile,
                ValidationStage.Semantic
            }));
        });
    }

    private static ValidationReport ValidateGenericProfile(
        string value,
        ProfileDefinition profile,
        IProfileProvider provider)
    {
        string xml = "<Document xmlns='" + MessageNamespace +
            "'><Root><Value>" + value + "</Value></Root></Document>";
        MessageReadResult read = new XmlMessageReader().ReadAutoText(
            xml,
            new MessageReadOptions());
        Assert.That(read.Success, Is.True, read.ErrorMessage);
        Assert.That(read.Document, Is.Not.Null);
        ValidationRequest request = new ValidationRequest
        {
            Document = read.Document,
            Profile = profile,
            ProfileProvider = provider
        };
        request.Policy.RedactOffendingValues = false;
        request.Policy.Stages.Add(ValidationStage.Profile);
        return new MessageValidator().ValidateMessage(request);
    }

    private static ValidationRequest CreateDocumentRequest()
    {
        return new ValidationRequest
        {
            Document = new MessageDocument
            {
                Metadata = new MessageMetadata
                {
                    MessageIdentifier = new MessageIdentifier
                    {
                        BusinessArea = "pacs",
                        MessageFunctionality = "008",
                        Variant = "001",
                        Version = "10",
                        Value = "pacs.008.001.10"
                    },
                    NamespaceUri = MessageNamespace
                }
            }
        };
    }

    private static ProfileDefinition CreateProfile(string identifier)
    {
        return new ProfileDefinition
        {
            Identifier = identifier,
            Version = "1",
            Artifact = new ArtifactManifest
            {
                OriginUri = "https://example.test/profiles/" + identifier,
                IsApproved = true
            }
        };
    }

    private static ProfileConstraint CreateValueConstraint(string identifier)
    {
        return new ProfileConstraint
        {
            Identifier = identifier,
            Path = new XmlPathData
            {
                Value = "/Document/Root/Value"
            },
            RequirePath = true,
            HasMinimumOccurrences = true,
            MinimumOccurrences = 1,
            HasMaximumOccurrences = true,
            MaximumOccurrences = 1,
            Severity = ValidationSeverity.Error
        };
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

    private class RecordingRule : ISemanticRule
    {
        private SemanticRuleDescriptor descriptor;
        private List<string> calls;
        private object? expectedCallerState;

        internal int InvocationCount;
        internal bool CallerStateMatched = true;
        internal ValidationRequest? LastRequest;

        internal RecordingRule(
            SemanticRuleDescriptor descriptorValue,
            List<string> callValues,
            object? callerState)
        {
            descriptor = descriptorValue;
            calls = callValues;
            expectedCallerState = callerState;
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
            calls.Add(descriptor.Identifier);
            if (expectedCallerState != null)
            {
                CallerStateMatched = request != null &&
                    request.Context.CallerState.ContainsKey("state") &&
                    ReferenceEquals(
                        request.Context.CallerState["state"],
                        expectedCallerState);
            }
            return new List<ValidationDiagnostic>();
        }
    }
}
