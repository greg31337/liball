using iso20022;

namespace iso20022.Tests;

public class ProfileRebaseComparerTests
{
    [Test]
    public void ReportsConcurrentConstraintConflictWithLocatedDiagnostic()
    {
        ProfileDefinition baseline = Profile("base");
        baseline.Constraints.Add(MaximumConstraint("amount", 10));
        ProfileDefinition local = Profile("local");
        local.Constraints.Add(MaximumConstraint("amount", 8));
        ProfileDefinition target = Profile("target");
        target.Constraints.Add(MaximumConstraint("amount", 6));

        RepositoryDiff result = Compare(baseline, local, target);
        DifferenceItem conflict = result.Differences.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Equivalent, Is.False);
            Assert.That(result.HasConflicts, Is.True);
            Assert.That(result.SourceEdition, Is.EqualTo("local"));
            Assert.That(result.TargetEdition, Is.EqualTo("target"));
            Assert.That(conflict.Kind, Is.EqualTo(DifferenceKind.Conflict));
            Assert.That(conflict.EntityKind, Is.EqualTo(RepositoryEntityKind.Constraint));
            Assert.That(conflict.SourceIdentifier, Is.EqualTo("amount"));
            Assert.That(conflict.TargetIdentifier, Is.EqualTo("amount"));
            Assert.That(conflict.SourcePath.Value, Is.EqualTo("/Document/amount"));
            Assert.That(conflict.BreakingChange, Is.True);
            Assert.That(conflict.DataLossRisk, Is.True);
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("comparison.profile_rebase_conflict"));
            Assert.That(result.Diagnostics.Single().Kind, Is.EqualTo(DiagnosticKind.Conflict));
            Assert.That(result.Diagnostics.Single().Stage, Is.EqualTo(ValidationStage.Profile));
        });
    }

    [Test]
    public void DistinguishesOneSidedAddsRemovalsAndModificationsDeterministically()
    {
        ProfileDefinition baseline = Profile("base");
        baseline.Constraints.Add(MaximumConstraint("existing", 10));
        ProfileDefinition local = Profile("local");
        local.Constraints.Add(MaximumConstraint("existing", 9));
        local.Constraints.Add(MaximumConstraint("local-addition", 4));
        ProfileDefinition target = Profile("target");
        target.Constraints.Add(MaximumConstraint("existing", 10));
        target.Constraints.Add(MaximumConstraint("target-addition", 5));

        RepositoryDiff first = Compare(baseline, local, target);
        RepositoryDiff second = Compare(baseline, local, target);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.HasConflicts, Is.False);
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.Modified && item.SourceIdentifier == "existing"));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.Removed && item.SourceIdentifier == "local-addition"));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.Added && item.TargetIdentifier == "target-addition"));
            Assert.That(DifferenceKeys(first), Is.EqualTo(DifferenceKeys(second)));
        });
    }

    [Test]
    public void ConvergentChangesAndReorderedValueSetsRemainEquivalent()
    {
        ProfileDefinition baseline = Profile("base");
        baseline.Name = "Original";
        baseline.Constraints.Add(ValueConstraint("status", "A", "B", "C"));
        ProfileDefinition local = Profile("local");
        local.Name = "Converged";
        local.Constraints.Add(ValueConstraint("status", "A", "C"));
        ProfileDefinition target = Profile("target");
        target.Name = "Converged";
        target.Constraints.Add(ValueConstraint("status", "C", "A"));
        RepositoryComparisonRequest request = Request(baseline, local, target);
        request.IncludeUnchanged = true;

        RepositoryDiff result = new RepositoryComparer().CompareRepositories(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Equivalent, Is.True);
            Assert.That(result.HasConflicts, Is.False);
            Assert.That(result.Differences, Is.Not.Empty);
            Assert.That(result.Differences.All(item => item.Kind == DifferenceKind.Unchanged),
                Is.True);
        });
    }

    [Test]
    public void DetectsSemanticRuleAndSupplementaryPolicyConflicts()
    {
        ProfileDefinition baseline = Profile("base");
        baseline.Rules.Add(Rule("control-sum", ValidationSeverity.Warning));
        baseline.SupplementaryDataPolicies.Add(Policy("urn:example:ext", true, false));
        ProfileDefinition local = Profile("local");
        local.Rules.Add(Rule("control-sum", ValidationSeverity.Information));
        local.SupplementaryDataPolicies.Add(Policy("urn:example:ext", false, true));
        ProfileDefinition target = Profile("target");
        target.Rules.Add(Rule("control-sum", ValidationSeverity.Error));
        target.SupplementaryDataPolicies.Add(Policy("urn:example:ext", true, true));

        RepositoryDiff result = Compare(baseline, local, target);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasConflicts, Is.True);
            Assert.That(result.Differences.Count(item => item.Kind == DifferenceKind.Conflict),
                Is.EqualTo(2));
            Assert.That(result.Differences.Select(item => item.PropertyName),
                Is.EquivalentTo(new[] { "SemanticRule", "SupplementaryDataPolicy" }));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics.All(item => item.Kind == DiagnosticKind.Conflict),
                Is.True);
        });
    }

    [Test]
    public void BoundsOutputAndReportsStableConfigurationFailures()
    {
        RepositoryComparer comparer = new RepositoryComparer();
        RepositoryComparisonRequest incomplete = new RepositoryComparisonRequest
        {
            BaselineProfile = Profile("base")
        };
        ProfileDefinition wrongIdentity = Profile("target");
        wrongIdentity.Identifier = "other";
        RepositoryComparisonRequest mismatch = Request(
            Profile("base"),
            Profile("local"),
            wrongIdentity);
        ProfileDefinition baseline = Profile("base");
        ProfileDefinition local = Profile("local");
        ProfileDefinition target = Profile("target");
        local.Name = "local name";
        local.Definition = "local definition";
        local.BaseProfileIdentifier = "local-base";
        RepositoryComparisonRequest bounded = Request(baseline, local, target);
        bounded.Limits.MaxCollectionCount = 2;

        RepositoryDiff incompleteResult = comparer.CompareRepositories(incomplete);
        RepositoryDiff mismatchResult = comparer.CompareRepositories(mismatch);
        RepositoryDiff boundedResult = comparer.CompareRepositories(bounded);

        Assert.Multiple(() =>
        {
            Assert.That(incompleteResult.ErrorCode,
                Is.EqualTo("comparison.profile_three_way_required"));
            Assert.That(mismatchResult.ErrorCode,
                Is.EqualTo("comparison.profile_identity_mismatch"));
            Assert.That(boundedResult.Success, Is.True);
            Assert.That(boundedResult.Truncated, Is.True);
            Assert.That(boundedResult.Differences, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void HonorsCancellationBeforeProfileComparison()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RepositoryComparisonRequest request = Request(
            Profile("base"),
            Profile("local"),
            Profile("target"));
        request.CancellationToken = cancellation.Token;

        RepositoryDiff result = new RepositoryComparer().CompareRepositories(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("comparison.cancelled"));
        });
    }

    private static RepositoryDiff Compare(
        ProfileDefinition baseline,
        ProfileDefinition local,
        ProfileDefinition target)
    {
        return new RepositoryComparer().CompareRepositories(Request(baseline, local, target));
    }

    private static RepositoryComparisonRequest Request(
        ProfileDefinition baseline,
        ProfileDefinition local,
        ProfileDefinition target)
    {
        return new RepositoryComparisonRequest
        {
            BaselineProfile = baseline,
            LocalProfile = local,
            TargetProfile = target
        };
    }

    private static ProfileDefinition Profile(string version)
    {
        return new ProfileDefinition
        {
            Identifier = "payments-profile",
            Version = version
        };
    }

    private static ProfileConstraint MaximumConstraint(string identifier, int maximum)
    {
        return new ProfileConstraint
        {
            Identifier = identifier,
            Name = identifier,
            Path = new XmlPathData { Value = "/Document/" + identifier.Replace("-", string.Empty) },
            HasMaximumOccurrences = true,
            MaximumOccurrences = maximum
        };
    }

    private static ProfileConstraint ValueConstraint(string identifier, params string[] values)
    {
        ProfileConstraint constraint = new ProfileConstraint
        {
            Identifier = identifier,
            Name = identifier,
            Path = new XmlPathData { Value = "/Document/Status" }
        };
        constraint.AllowedValues.AddRange(values);
        return constraint;
    }

    private static ProfileRuleReference Rule(string identifier, ValidationSeverity severity)
    {
        return new ProfileRuleReference
        {
            RuleIdentifier = identifier,
            HasSeverityOverride = true,
            Severity = severity
        };
    }

    private static SupplementaryDataPolicy Policy(
        string namespaceUri,
        bool canIgnore,
        bool mustUnderstand)
    {
        return new SupplementaryDataPolicy
        {
            NamespaceUri = namespaceUri,
            CanIgnore = canIgnore,
            MustUnderstand = mustUnderstand
        };
    }

    private static IEnumerable<string> DifferenceKeys(RepositoryDiff result)
    {
        return result.Differences.Select(item =>
            $"{item.Kind}|{item.PropertyName}|{item.SourceIdentifier}|{item.TargetIdentifier}");
    }
}
