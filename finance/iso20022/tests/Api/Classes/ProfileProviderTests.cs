using iso20022;

namespace iso20022.Tests;

public class ProfileProviderTests
{
    private const string MessageNamespace =
        "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10";

    [Test]
    public void ProviderFindsExactAndLatestDottedVersionFromCopiedRegistrationList()
    {
        ProfileDefinition versionOne = CreateProfile("profile-a", "1.0");
        ProfileDefinition versionTwo = CreateProfile("profile-a", "1.2");
        ProfileDefinition versionTen = CreateProfile("profile-a", "1.10");
        List<ProfileDefinition> registrations =
            new List<ProfileDefinition> { versionTwo, versionOne, versionTen };
        ProfileProvider provider = new ProfileProvider(registrations);

        registrations.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(provider.FindProfile("profile-a", "1.2"),
                Is.SameAs(versionTwo));
            Assert.That(provider.FindProfile("profile-a", string.Empty),
                Is.SameAs(versionTen));
            Assert.That(provider.FindProfile("missing", string.Empty), Is.Null);
            Assert.That(provider.FindProfile("profile-a", null), Is.Null);
        });
    }

    [Test]
    public void ProviderRejectsDuplicateExactProfileVersions()
    {
        List<ProfileDefinition> registrations = new List<ProfileDefinition>
        {
            CreateProfile("profile-a", "1"),
            CreateProfile("profile-a", "1")
        };

        Assert.That(() => new ProfileProvider(registrations),
            Throws.ArgumentException);
    }

    [Test]
    public void ProviderRejectsEveryIncompleteRegistrationForm()
    {
        ProfileDefinition nullIdentifier = CreateProfile("id", "1");
        nullIdentifier.Identifier = null!;
        ProfileDefinition emptyIdentifier = CreateProfile(string.Empty, "1");
        ProfileDefinition nullVersion = CreateProfile("id", "1");
        nullVersion.Version = null!;
        ProfileDefinition emptyVersion = CreateProfile("id", string.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(() => new ProfileProvider(null), Throws.ArgumentNullException);
            Assert.That(() => new ProfileProvider(
                new List<ProfileDefinition> { null! }), Throws.ArgumentException);
            Assert.That(() => new ProfileProvider(
                new List<ProfileDefinition> { nullIdentifier }), Throws.ArgumentException);
            Assert.That(() => new ProfileProvider(
                new List<ProfileDefinition> { emptyIdentifier }), Throws.ArgumentException);
            Assert.That(() => new ProfileProvider(
                new List<ProfileDefinition> { nullVersion }), Throws.ArgumentException);
            Assert.That(() => new ProfileProvider(
                new List<ProfileDefinition> { emptyVersion }), Throws.ArgumentException);
        });
    }

    [Test]
    public void LookupAndVersionComparisonCoverAllBoundaryForms()
    {
        ProfileDefinition first = CreateProfile("profile-a", "1");
        ProfileDefinition second = CreateProfile("profile-b", "2");
        ProfileProvider provider = new ProfileProvider(
            new List<ProfileDefinition> { first, second });

        Assert.Multiple(() =>
        {
            Assert.That(provider.FindProfile(null, string.Empty), Is.Null);
            Assert.That(provider.FindProfile(string.Empty, string.Empty), Is.Null);
            Assert.That(provider.FindProfile("profile-a", null), Is.Null);
            Assert.That(ProfileProvider.FindExactRegisteredProfile(
                new List<ProfileDefinition> { first, second }, "profile-b", "1"), Is.Null);
            Assert.That(ProfileProvider.FindExactRegisteredProfile(
                new List<ProfileDefinition> { first, second }, "profile-b", "2"),
                Is.SameAs(second));

            Assert.That(ProfileProvider.CompareProfileVersions("1", "1.0"), Is.LessThan(0));
            Assert.That(ProfileProvider.CompareProfileVersions("1.0", "1"), Is.GreaterThan(0));
            Assert.That(ProfileProvider.CompareProfileVersions("1.0", "1.0"), Is.Zero);
            Assert.That(ProfileProvider.CompareProfileVersions("2", "1"), Is.GreaterThan(0));
            Assert.That(ProfileProvider.CompareProfileVersions("1", "2"), Is.LessThan(0));

            Assert.That(ProfileProvider.CompareProfileVersionPart("a", "1"), Is.GreaterThan(0));
            Assert.That(ProfileProvider.CompareProfileVersionPart("1", "a"), Is.LessThan(0));
            Assert.That(ProfileProvider.CompareProfileVersionPart("001", "1"), Is.Zero);
            Assert.That(ProfileProvider.CompareProfileVersionPart("1", "001"), Is.Zero);
            Assert.That(ProfileProvider.CompareProfileVersionPart("10", "2"), Is.GreaterThan(0));
            Assert.That(ProfileProvider.CompareProfileVersionPart("2", "10"), Is.LessThan(0));
            Assert.That(ProfileProvider.CompareProfileVersionPart("2", "3"), Is.LessThan(0));

            Assert.That(ProfileProvider.ProfileVersionPartIsNumeric(string.Empty), Is.False);
            Assert.That(ProfileProvider.ProfileVersionPartIsNumeric("/"), Is.False);
            Assert.That(ProfileProvider.ProfileVersionPartIsNumeric(":"), Is.False);
            Assert.That(ProfileProvider.ProfileVersionPartIsNumeric("0123"), Is.True);
        });
    }

    [Test]
    public void InheritedOverlayStrengthensCardinalityAndValuesWithoutMutation()
    {
        ProfileDefinition baseProfile = CreateProfile("base-profile", "1");
        ProfileConstraint baseConstraint = CreateStatusConstraint(
            "base.status",
            string.Empty,
            1,
            3,
            new[] { "A", "B" },
            new[] { "X" });
        baseProfile.Constraints.Add(baseConstraint);
        ProfileDefinition derived = CreateProfile("derived-profile", "1");
        derived.BaseProfileIdentifier = baseProfile.Identifier;
        ProfileConstraint derivedConstraint = CreateStatusConstraint(
            "derived.status",
            baseConstraint.Identifier,
            2,
            2,
            new[] { "A" },
            new[] { "Y" });
        derived.Constraints.Add(derivedConstraint);
        ProfileProvider provider = new ProfileProvider(
            new List<ProfileDefinition> { baseProfile });

        ValidationReport valid = ValidateProfile(
            "<Status>A</Status><Status>A</Status>",
            derived,
            provider);
        ValidationReport invalid = ValidateProfile(
            "<Status>B</Status>",
            derived,
            provider);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True, valid.ErrorMessage);
            Assert.That(valid.Valid, Is.True);
            Assert.That(valid.Diagnostics, Is.Empty);
            Assert.That(invalid.Success, Is.True, invalid.ErrorMessage);
            Assert.That(invalid.Valid, Is.False);
            Assert.That(invalid.Diagnostics.Select(item => item.Code), Is.EqualTo(new[]
            {
                "validation.profile.minimum_occurs",
                "validation.profile.value_not_allowed"
            }));
            Assert.That(invalid.Diagnostics.Select(item => item.RuleIdentifier),
                Has.All.EqualTo("derived.status"));
            Assert.That(invalid.Diagnostics.Select(item => item.ProfileIdentifier),
                Has.All.EqualTo("derived-profile"));
            Assert.That(invalid.Diagnostics.Select(item => item.SourceArtifact),
                Has.All.SameAs(derived.Artifact));
            Assert.That(baseConstraint.MinimumOccurrences, Is.EqualTo(1));
            Assert.That(baseConstraint.MaximumOccurrences, Is.EqualTo(3));
            Assert.That(baseConstraint.AllowedValues, Is.EqualTo(new[] { "A", "B" }));
            Assert.That(baseConstraint.ProhibitedValues, Is.EqualTo(new[] { "X" }));
        });
    }

    [Test]
    public void WeakeningAnInheritedConstraintReportsAConflict()
    {
        ProfileDefinition baseProfile = CreateProfile("base-profile", "1");
        baseProfile.Constraints.Add(CreateStatusConstraint(
            "base.status",
            string.Empty,
            1,
            2,
            new[] { "A" },
            Array.Empty<string>()));
        ProfileDefinition derived = CreateProfile("derived-profile", "1");
        derived.BaseProfileIdentifier = baseProfile.Identifier;
        derived.Constraints.Add(CreateStatusConstraint(
            "derived.status",
            "base.status",
            0,
            3,
            new[] { "A", "B" },
            Array.Empty<string>()));

        ValidationReport report = ValidateProfile(
            "<Status>A</Status>",
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
    public void IncompatibleBaseMessageVersionReportsApplicabilityFailure()
    {
        ProfileDefinition profile = CreateProfile("profile-a", "1");
        profile.BaseVersion = new VersionSelector
        {
            Kind = VersionSelectionKind.Exact,
            BaselineIdentifier = new MessageIdentifier
            {
                Value = "pacs.008.001.09"
            }
        };

        ValidationReport report = ValidateProfile(
            "<Status>A</Status>",
            profile,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True, report.ErrorMessage);
            Assert.That(report.Valid, Is.False);
            Assert.That(report.Diagnostics.Single().Code,
                Is.EqualTo("validation.profile.target_mismatch"));
            Assert.That(report.Diagnostics.Single().Kind,
                Is.EqualTo(DiagnosticKind.Applicability));
        });
    }

    [Test]
    public void InheritedRuleRemovalAndReenableComposeInBaseToDerivedOrder()
    {
        ProfileDefinition baseProfile = CreateProfile("base-profile", "1");
        baseProfile.Rules.Add(new ProfileRuleReference
        {
            RuleIdentifier = "iso20022.semantic.transaction_count",
            Enabled = true,
            RemoveInheritedRule = true
        });
        ProfileDefinition derived = CreateProfile("derived-profile", "1");
        derived.BaseProfileIdentifier = baseProfile.Identifier;
        ProfileProvider provider = new ProfileProvider(
            new List<ProfileDefinition> { baseProfile });
        ValidationRequest request = CreateRequest(
            "<FIToFICstmrCdtTrf><GrpHdr><NbOfTxs>2</NbOfTxs></GrpHdr>" +
            "<CdtTrfTxInf/></FIToFICstmrCdtTrf>",
            derived,
            provider,
            ValidationStage.Semantic);

        ValidationReport removed = new MessageValidator().ValidateMessage(request);
        derived.Rules.Add(new ProfileRuleReference
        {
            RuleIdentifier = "iso20022.semantic.transaction_count",
            Enabled = true,
            HasSeverityOverride = true,
            Severity = ValidationSeverity.Warning
        });
        ValidationReport reenabled = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(removed.Success, Is.True, removed.ErrorMessage);
            Assert.That(removed.Valid, Is.True);
            Assert.That(removed.Diagnostics, Is.Empty);
            Assert.That(reenabled.Success, Is.True, reenabled.ErrorMessage);
            Assert.That(reenabled.Valid, Is.True);
            Assert.That(reenabled.WarningCount, Is.EqualTo(1));
            Assert.That(reenabled.Diagnostics.Single().RuleIdentifier,
                Is.EqualTo("iso20022.semantic.transaction_count"));
        });
    }

    private static ValidationReport ValidateProfile(
        string statusXml,
        ProfileDefinition profile,
        IProfileProvider? provider)
    {
        ValidationRequest request = CreateRequest(
            "<Root>" + statusXml + "</Root>",
            profile,
            provider,
            ValidationStage.Profile);
        request.Policy.RedactOffendingValues = false;
        return new MessageValidator().ValidateMessage(request);
    }

    private static ValidationRequest CreateRequest(
        string bodyXml,
        ProfileDefinition profile,
        IProfileProvider? provider,
        ValidationStage stage)
    {
        string xml = "<Document xmlns='" + MessageNamespace + "'>" +
            bodyXml + "</Document>";
        MessageReadResult read = new XmlMessageReader().ReadAutoText(
            xml,
            new MessageReadOptions());
        Assert.That(read.Success, Is.True, read.ErrorMessage);
        Assert.That(read.Document, Is.Not.Null);
        ValidationRequest request = new ValidationRequest
        {
            Document = read.Document,
            ExpectedIdentifier = new MessageIdentifier
            {
                Value = "pacs.008.001.10"
            },
            ExpectedNamespaceUri = MessageNamespace,
            Profile = profile,
            ProfileProvider = provider
        };
        request.Policy.Stages.Add(stage);
        return request;
    }

    private static ProfileDefinition CreateProfile(
        string identifier,
        string version)
    {
        return new ProfileDefinition
        {
            Identifier = identifier,
            Version = version,
            Artifact = new ArtifactManifest
            {
                OriginUri = "https://example.test/profiles/" + identifier +
                    "/" + version,
                IsApproved = true
            }
        };
    }

    private static ProfileConstraint CreateStatusConstraint(
        string identifier,
        string baseIdentifier,
        int minimum,
        int maximum,
        IEnumerable<string> allowed,
        IEnumerable<string> prohibited)
    {
        ProfileConstraint constraint = new ProfileConstraint
        {
            Identifier = identifier,
            BaseConstraintIdentifier = baseIdentifier,
            Path = new XmlPathData
            {
                Value = "/Document/Root/Status"
            },
            RequirePath = true,
            HasMinimumOccurrences = true,
            MinimumOccurrences = minimum,
            HasMaximumOccurrences = true,
            MaximumOccurrences = maximum,
            Severity = ValidationSeverity.Error
        };
        constraint.AllowedValues.AddRange(allowed);
        constraint.ProhibitedValues.AddRange(prohibited);
        return constraint;
    }
}
