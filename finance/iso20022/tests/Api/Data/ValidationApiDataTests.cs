using iso20022;

namespace iso20022.Tests;

public class ValidationApiDataTests
{
    [Test]
    public void ValidationPolicyUsesSecureBoundedDefaultsAndIndependentSelections()
    {
        ValidationPolicy first = new ValidationPolicy();
        ValidationPolicy second = new ValidationPolicy();

        first.Stages.Add(ValidationStage.Xsd);
        first.Limits.MaxDiagnosticCount = 3;
        first.FailFast = true;
        first.ExternalCodeEdition = "2026-05-v1";
        first.SelectLatestExternalCodeEdition = false;

        Assert.Multiple(() =>
        {
            Assert.That(first.Stages, Is.EqualTo(new[] { ValidationStage.Xsd }));
            Assert.That(first.FailFast, Is.True);
            Assert.That(first.ExternalCodeEdition, Is.EqualTo("2026-05-v1"));
            Assert.That(first.SelectLatestExternalCodeEdition, Is.False);
            Assert.That(second.Stages, Is.Empty);
            Assert.That(second.Stages, Is.Not.SameAs(first.Stages));
            Assert.That(second.Limits, Is.Not.SameAs(first.Limits));
            Assert.That(second.Limits.MaxDiagnosticCount,
                Is.EqualTo(Iso20022Limits.MaxDiagnosticCount));
            Assert.That(second.FailFast, Is.False);
            Assert.That(second.TreatWarningsAsErrors, Is.False);
            Assert.That(second.AllowDraftArtifacts, Is.False);
            Assert.That(second.SelectLatestExternalCodeEdition, Is.True);
            Assert.That(second.IncludeInactiveExternalCodes, Is.False);
            Assert.That(second.UnknownContentMode, Is.EqualTo(UnknownContentMode.Reject));
            Assert.That(second.RequireTimezone, Is.False);
            Assert.That(second.RequireValidSignature, Is.False);
            Assert.That(second.ValidatePresentSignatures, Is.True);
            Assert.That(second.RedactOffendingValues, Is.True);
        });
    }

    [Test]
    public void ValidationRequestCarriesRawGenericTypedIdentityProfileAndCallerContext()
    {
        MessageIdentifier identifier = new MessageIdentifier
        {
            BusinessArea = "pacs",
            MessageFunctionality = "008",
            Variant = "001",
            Version = "10",
            Value = "pacs.008.001.10"
        };
        XmlPathData amountPath = new XmlPathData { Value = "/Document/CdtTrfTxInf/IntrBkSttlmAmt" };
        amountPath.Segments.Add(new QualifiedNameData { LocalName = "Document" });
        amountPath.Segments.Add(new QualifiedNameData { LocalName = "CdtTrfTxInf" });
        amountPath.Segments.Add(new QualifiedNameData { LocalName = "IntrBkSttlmAmt" });
        SemanticRuleDescriptor descriptor = new SemanticRuleDescriptor
        {
            Identifier = "profile.cbpr.currency",
            Name = "Settlement currency rule",
            Definition = "The settlement currency must be supported by the selected service.",
            Severity = ValidationSeverity.Warning,
            Order = 20,
            Enabled = true,
            VersionSelector = new VersionSelector
            {
                Kind = VersionSelectionKind.Exact,
                BaselineIdentifier = identifier
            },
            SourceIdentifier = "cbpr-plus-2026"
        };
        descriptor.MessageIdentifiers.Add(identifier);
        descriptor.ProfileIdentifiers.Add("cbpr-plus");
        descriptor.Paths.Add(amountPath);
        ProfileConstraint constraint = new ProfileConstraint
        {
            Identifier = "profile.cbpr.amount.required",
            Name = "Settlement amount required",
            Definition = "Exactly one settlement amount is required.",
            Path = amountPath,
            BaseConstraintIdentifier = "iso.pacs.008.amount",
            RequirePath = true,
            HasMinimumOccurrences = true,
            MinimumOccurrences = 1,
            HasMaximumOccurrences = true,
            MaximumOccurrences = 1,
            Severity = ValidationSeverity.Error
        };
        constraint.AllowedValues.Add("USD");
        constraint.AllowedValues.Add("EUR");
        constraint.ProhibitedValues.Add("XXX");
        ProfileRuleReference ruleReference = new ProfileRuleReference
        {
            RuleIdentifier = descriptor.Identifier,
            Descriptor = descriptor,
            Enabled = true,
            HasSeverityOverride = true,
            Severity = ValidationSeverity.Error,
            HasOrderOverride = true,
            Order = 5
        };
        ProfileDefinition profile = new ProfileDefinition
        {
            Identifier = "cbpr-plus",
            Name = "CBPR+",
            Version = "2026",
            Definition = "Representative caller-provided profile.",
            BaseVersion = new VersionSelector
            {
                Kind = VersionSelectionKind.Exact,
                BaselineIdentifier = identifier
            },
            Artifact = new ArtifactManifest
            {
                OriginUri = "https://example.test/profiles/cbpr-plus-2026.json",
                IsApproved = true
            }
        };
        profile.MessageIdentifiers.Add(identifier);
        profile.NamespaceUris.Add("urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10");
        profile.Constraints.Add(constraint);
        profile.Rules.Add(ruleReference);
        ValidationContext context = new ValidationContext
        {
            MessageIdentifier = identifier,
            NamespaceUri = profile.NamespaceUris[0],
            ProfileIdentifier = profile.Identifier,
            BusinessService = "swift.cbprplus.02",
            RepositoryEdition = "2026",
            EffectiveAt = new IsoDateTimeValue { LexicalValue = "2026-08-31T12:00:00Z" }
        };
        object callerValue = new object();
        context.CallerState.Add("directory", callerValue);
        MessageDocument document = new MessageDocument();
        object model = new object();
        SchemaArtifact schema = new SchemaArtifact
        {
            NamespaceUri = context.NamespaceUri,
            MessageIdentifier = identifier,
            Profile = profile.Identifier
        };
        ValidationRequest request = new ValidationRequest
        {
            Xml = "<Document/>"u8.ToArray(),
            Document = document,
            Model = model,
            ExpectedIdentifier = identifier,
            ExpectedNamespaceUri = context.NamespaceUri,
            Schema = schema,
            Context = context,
            Profile = profile
        };
        request.Policy.Stages.Add(ValidationStage.Structural);
        request.Policy.Stages.Add(ValidationStage.Semantic);

        Assert.Multiple(() =>
        {
            Assert.That(request.Xml, Is.EqualTo("<Document/>"u8.ToArray()));
            Assert.That(request.Document, Is.SameAs(document));
            Assert.That(request.Model, Is.SameAs(model));
            Assert.That(request.ExpectedIdentifier, Is.SameAs(identifier));
            Assert.That(request.ExpectedNamespaceUri, Is.EqualTo(context.NamespaceUri));
            Assert.That(request.Schema, Is.SameAs(schema));
            Assert.That(request.Context, Is.SameAs(context));
            Assert.That(request.Profile, Is.SameAs(profile));
            Assert.That(request.Policy.Stages,
                Is.EqualTo(new[] { ValidationStage.Structural, ValidationStage.Semantic }));
            Assert.That(context.CallerState["directory"], Is.SameAs(callerValue));
            Assert.That(profile.BaseVersion.BaselineIdentifier, Is.SameAs(identifier));
            Assert.That(profile.Constraints.Single(), Is.SameAs(constraint));
            Assert.That(profile.Rules.Single(), Is.SameAs(ruleReference));
            Assert.That(ruleReference.Descriptor, Is.SameAs(descriptor));
            Assert.That(descriptor.MessageIdentifiers.Single(), Is.SameAs(identifier));
            Assert.That(descriptor.ProfileIdentifiers.Single(), Is.EqualTo("cbpr-plus"));
            Assert.That(descriptor.Paths.Single(), Is.SameAs(amountPath));
            Assert.That(constraint.MinimumOccurrences, Is.EqualTo(1));
            Assert.That(constraint.MaximumOccurrences, Is.EqualTo(1));
            Assert.That(constraint.AllowedValues, Is.EqualTo(new[] { "USD", "EUR" }));
            Assert.That(constraint.ProhibitedValues, Is.EqualTo(new[] { "XXX" }));
            Assert.That(profile.Artifact.IsApproved, Is.True);
        });
    }

    [Test]
    public void ValidationReportRetainsOrderedLocatedRedactedDiagnosticsAndProvenance()
    {
        ArtifactManifest artifact = new ArtifactManifest
        {
            OriginUri = "file:///repository-2026.iso20022",
            IsApproved = true
        };
        ValidationDiagnostic diagnostic = new ValidationDiagnostic
        {
            Code = "validation.external-code.inactive",
            RuleIdentifier = "iso.external.PaymentPurpose1Code.active",
            Severity = ValidationSeverity.Error,
            Stage = ValidationStage.ExternalCode,
            Kind = DiagnosticKind.InvalidValue,
            Message = "The supplied external code is inactive.",
            Path = new XmlPathData { Value = "/Document/PmtInf/Purp/Cd" },
            Location = new SourceLocation
            {
                SourceName = "message.xml",
                HasLineInformation = true,
                LineNumber = 14,
                ColumnNumber = 27
            },
            OffendingValueRedacted = true,
            ExpectedConstraint = "An active PaymentPurpose1Code value.",
            SourceArtifact = artifact,
            SourceIdentifier = "PaymentPurpose1Code",
            ProfileIdentifier = "cbpr-plus",
            ProfileVersion = "2026",
            RepairHint = "Select a code active on the message value date."
        };
        ValidationReport report = new ValidationReport
        {
            Success = true,
            Valid = false,
            MessageIdentifier = new MessageIdentifier { Value = "pacs.008.001.10" },
            NamespaceUri = "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10",
            ProfileIdentifier = "cbpr-plus",
            ErrorCount = 1,
            StoppedEarly = true
        };
        report.CompletedStages.Add(ValidationStage.SecureXml);
        report.CompletedStages.Add(ValidationStage.ExternalCode);
        report.Diagnostics.Add(diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True);
            Assert.That(report.Valid, Is.False);
            Assert.That(report.CompletedStages,
                Is.EqualTo(new[] { ValidationStage.SecureXml, ValidationStage.ExternalCode }));
            Assert.That(report.Diagnostics.Single(), Is.SameAs(diagnostic));
            Assert.That(report.ErrorCount, Is.EqualTo(1));
            Assert.That(report.WarningCount, Is.Zero);
            Assert.That(report.InformationCount, Is.Zero);
            Assert.That(report.StoppedEarly, Is.True);
            Assert.That(report.DiagnosticLimitReached, Is.False);
            Assert.That(diagnostic.OffendingValue, Is.Empty);
            Assert.That(diagnostic.OffendingValueRedacted, Is.True);
            Assert.That(diagnostic.Path.Value, Is.EqualTo("/Document/PmtInf/Purp/Cd"));
            Assert.That(diagnostic.Location.LineNumber, Is.EqualTo(14));
            Assert.That(diagnostic.Location.ColumnNumber, Is.EqualTo(27));
            Assert.That(diagnostic.SourceArtifact, Is.SameAs(artifact));
            Assert.That(diagnostic.SourceIdentifier, Is.EqualTo("PaymentPurpose1Code"));
            Assert.That(diagnostic.ProfileVersion, Is.EqualTo("2026"));
        });
    }

    [Test]
    public void ValidationCollectionsAndNestedDefaultsAreIndependent()
    {
        ValidationRequest firstRequest = new ValidationRequest();
        ValidationRequest secondRequest = new ValidationRequest();
        ValidationReport firstReport = new ValidationReport();
        ValidationReport secondReport = new ValidationReport();
        ProfileDefinition firstProfile = new ProfileDefinition();
        ProfileDefinition secondProfile = new ProfileDefinition();
        SemanticRuleDescriptor firstDescriptor = new SemanticRuleDescriptor();
        SemanticRuleDescriptor secondDescriptor = new SemanticRuleDescriptor();
        ProfileConstraint firstConstraint = new ProfileConstraint();
        ProfileConstraint secondConstraint = new ProfileConstraint();

        firstRequest.Xml = new byte[] { 1 };
        firstRequest.Context.CallerState.Add("key", "value");
        firstRequest.Policy.Stages.Add(ValidationStage.Profile);
        firstReport.CompletedStages.Add(ValidationStage.Xsd);
        firstReport.Diagnostics.Add(new ValidationDiagnostic());
        firstProfile.Constraints.Add(firstConstraint);
        firstProfile.Rules.Add(new ProfileRuleReference());
        firstDescriptor.MessageIdentifiers.Add(new MessageIdentifier());
        firstDescriptor.ProfileIdentifiers.Add("profile");
        firstDescriptor.Paths.Add(new XmlPathData());
        firstConstraint.AllowedValues.Add("A");
        firstConstraint.ProhibitedValues.Add("B");

        Assert.Multiple(() =>
        {
            Assert.That(secondRequest.Xml, Is.Empty);
            Assert.That(secondRequest.Context, Is.Not.SameAs(firstRequest.Context));
            Assert.That(secondRequest.Context.CallerState, Is.Empty);
            Assert.That(secondRequest.Policy, Is.Not.SameAs(firstRequest.Policy));
            Assert.That(secondRequest.Policy.Stages, Is.Empty);
            Assert.That(secondReport.CompletedStages, Is.Empty);
            Assert.That(secondReport.Diagnostics, Is.Empty);
            Assert.That(secondReport.CompletedStages, Is.Not.SameAs(firstReport.CompletedStages));
            Assert.That(secondReport.Diagnostics, Is.Not.SameAs(firstReport.Diagnostics));
            Assert.That(secondProfile.BaseVersion, Is.Not.SameAs(firstProfile.BaseVersion));
            Assert.That(secondProfile.Artifact, Is.Not.SameAs(firstProfile.Artifact));
            Assert.That(secondProfile.MessageIdentifiers, Is.Empty);
            Assert.That(secondProfile.NamespaceUris, Is.Empty);
            Assert.That(secondProfile.Constraints, Is.Empty);
            Assert.That(secondProfile.Rules, Is.Empty);
            Assert.That(secondDescriptor.MessageIdentifiers, Is.Empty);
            Assert.That(secondDescriptor.ProfileIdentifiers, Is.Empty);
            Assert.That(secondDescriptor.Paths, Is.Empty);
            Assert.That(secondConstraint.Path, Is.Not.SameAs(firstConstraint.Path));
            Assert.That(secondConstraint.AllowedValues, Is.Empty);
            Assert.That(secondConstraint.ProhibitedValues, Is.Empty);
        });
    }

}
