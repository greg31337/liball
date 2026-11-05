using iso20022;

namespace iso20022.Tests;

public class RepositoryMatcherTests
{
    [Test]
    public void ExactXmiIdentityWinsEvenWhenSemanticNamesDiffer()
    {
        RepositorySnapshot source = CreateSnapshot("source");
        RepositorySnapshot target = CreateSnapshot("target");
        source.BusinessComponents.Add(Component("_shared", "OldName", source));
        target.BusinessComponents.Add(Component("_shared", "NewName", target));

        RepositoryMatchResult result = RepositoryMatcher.Match(Request(source, target));
        RepositoryEntityMatch match = FindMatch(result, "_shared");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(match.Target?.Identifier, Is.EqualTo("_shared"));
            Assert.That(match.Confidence, Is.EqualTo(1m));
            Assert.That(match.ExactIdentifierMatch, Is.True);
            Assert.That(match.SemanticFallbackMatch, Is.False);
            Assert.That(match.OverrideMatch, Is.False);
        });
    }

    [Test]
    public void StableQualifiedSemanticIdentityInfersRename()
    {
        RepositorySnapshot source = CreateSnapshot("source");
        RepositorySnapshot target = CreateSnapshot("target");
        AddCode(source, "_sourceSet", "_sourceCode", "Accepted", "ACCP");
        AddCode(target, "_targetSet", "_targetCode", "Approved", "ACCP");

        RepositoryMatchResult result = RepositoryMatcher.Match(Request(source, target));
        RepositoryEntityMatch match = FindMatch(result, "_sourceCode");

        Assert.Multiple(() =>
        {
            Assert.That(match.Target?.Identifier, Is.EqualTo("_targetCode"));
            Assert.That(match.Confidence, Is.EqualTo(0.92m));
            Assert.That(match.SemanticFallbackMatch, Is.True);
            Assert.That(match.Rationale, Does.Contain("name change"));
        });
    }

    [Test]
    public void QualifiedOwnerContextSelectsSameNamedNestedEntity()
    {
        RepositorySnapshot source = CreateSnapshot("source");
        RepositorySnapshot target = CreateSnapshot("target");
        AddComponentAndElement(source, "_sourceA", "Account", "_sourceElement", "Id");
        AddComponentAndElement(target, "_targetB", "Balance", "_targetElementB", "Id");
        AddComponentAndElement(target, "_targetA", "Account", "_targetElementA", "Id");

        RepositoryMatchResult result = RepositoryMatcher.Match(Request(source, target));
        RepositoryEntityMatch match = FindMatch(result, "_sourceElement");

        Assert.Multiple(() =>
        {
            Assert.That(match.Target?.Identifier, Is.EqualTo("_targetElementA"));
            Assert.That(match.Confidence, Is.EqualTo(0.95m));
            Assert.That(match.Ambiguous, Is.False);
        });
    }

    [Test]
    public void EqualBestSemanticCandidatesAreReportedAsDeterministicAmbiguity()
    {
        RepositorySnapshot source = CreateSnapshot("source");
        RepositorySnapshot target = CreateSnapshot("target");
        source.BusinessComponents.Add(Component("_sourceAccount", "Account", source));
        target.BusinessComponents.Add(Component("_targetFirst", "Account", target));
        target.BusinessComponents.Add(Component("_targetSecond", "Account", target));

        RepositoryMatchResult result = RepositoryMatcher.Match(Request(source, target));
        RepositoryEntityMatch match = FindMatch(result, "_sourceAccount");

        Assert.Multiple(() =>
        {
            Assert.That(match.Target, Is.Null);
            Assert.That(match.Ambiguous, Is.True);
            Assert.That(match.Confidence, Is.EqualTo(0.95m));
            Assert.That(match.AlternativeTargets.Select(item => item.Identifier),
                Is.EqualTo(new[] { "_targetFirst", "_targetSecond" }));
            Assert.That(result.UnmatchedTargets.Select(item => item.Identifier),
                Does.Contain("_targetFirst").And.Contain("_targetSecond"));
        });
    }

    [Test]
    public void CallerOverrideTakesPriorityOverAutomaticCandidates()
    {
        RepositorySnapshot source = CreateSnapshot("source");
        RepositorySnapshot target = CreateSnapshot("target");
        source.BusinessComponents.Add(Component("_sourceAccount", "Account", source));
        target.BusinessComponents.Add(Component("_automatic", "Account", target));
        target.BusinessComponents.Add(Component("_preferred", "CustomerAccount", target));
        RepositoryComparisonRequest request = Request(source, target);
        request.MatchOverrideRules.Add(new MappingRuleDefinition
        {
            SourceIdentifier = "_sourceAccount",
            TargetIdentifier = "_preferred",
            EntityKind = RepositoryEntityKind.BusinessComponent
        });

        RepositoryMatchResult result = RepositoryMatcher.Match(request);
        RepositoryEntityMatch match = FindMatch(result, "_sourceAccount");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(match.Target?.Identifier, Is.EqualTo("_preferred"));
            Assert.That(match.OverrideMatch, Is.True);
            Assert.That(match.Confidence, Is.EqualTo(1m));
            Assert.That(result.UnmatchedTargets.Select(item => item.Identifier),
                Does.Contain("_automatic"));
        });
    }

    [Test]
    public void ConfidenceThresholdRejectsWeakCrossOwnerMove()
    {
        RepositorySnapshot source = CreateSnapshot("source");
        RepositorySnapshot target = CreateSnapshot("target");
        AddComponentAndElement(source, "_sourceA", "Account", "_sourceElement", "Id");
        AddComponentAndElement(target, "_targetB", "Balance", "_targetElement", "Id");
        RepositoryComparisonRequest request = Request(source, target);
        request.MinimumMatchConfidence = 0.90m;

        RepositoryMatchResult result = RepositoryMatcher.Match(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Matches.Any(item => item.Source.Identifier == "_sourceElement"),
                Is.False);
            Assert.That(result.UnmatchedSources.Select(item => item.Identifier),
                Does.Contain("_sourceElement"));
            Assert.That(result.UnmatchedTargets.Select(item => item.Identifier),
                Does.Contain("_targetElement"));
        });
    }

    [Test]
    public void ConflictingCallerOverridesFailWithoutPartialMatches()
    {
        RepositorySnapshot source = CreateSnapshot("source");
        RepositorySnapshot target = CreateSnapshot("target");
        source.BusinessComponents.Add(Component("_sourceAccount", "Account", source));
        target.BusinessComponents.Add(Component("_targetOne", "One", target));
        target.BusinessComponents.Add(Component("_targetTwo", "Two", target));
        RepositoryComparisonRequest request = Request(source, target);
        request.MatchOverrideRules.Add(new MappingRuleDefinition
        {
            SourceIdentifier = "_sourceAccount",
            TargetIdentifier = "_targetOne"
        });
        request.MatchOverrideRules.Add(new MappingRuleDefinition
        {
            SourceIdentifier = "_sourceAccount",
            TargetIdentifier = "_targetTwo"
        });

        RepositoryMatchResult result = RepositoryMatcher.Match(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("comparison.override_conflict"));
            Assert.That(result.Matches, Is.Empty);
        });
    }

    [Test]
    public void MatchingHonorsCancellationAndEntityLimits()
    {
        RepositorySnapshot source = CreateSnapshot("source");
        RepositorySnapshot target = CreateSnapshot("target");
        RepositoryComparisonRequest cancelledRequest = Request(source, target);
        cancelledRequest.CancellationToken = new CancellationToken(true);
        RepositoryComparisonRequest limitedRequest = Request(source, target);
        limitedRequest.Limits.MaxElementCount = 2;

        RepositoryMatchResult cancelled = RepositoryMatcher.Match(cancelledRequest);
        RepositoryMatchResult limited = RepositoryMatcher.Match(limitedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.Success, Is.False);
            Assert.That(cancelled.ErrorCode, Is.EqualTo("comparison.cancelled"));
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.ErrorCode, Is.EqualTo("comparison.resource_limit"));
        });
    }

    private static RepositoryEntityMatch FindMatch(
        RepositoryMatchResult result,
        string sourceIdentifier)
    {
        return result.Matches.Single(item => item.Source.Identifier == sourceIdentifier);
    }

    private static RepositoryComparisonRequest Request(
        RepositorySnapshot source,
        RepositorySnapshot target)
    {
        return new RepositoryComparisonRequest
        {
            Source = source,
            Target = target
        };
    }

    private static RepositorySnapshot CreateSnapshot(string suffix)
    {
        RepositorySnapshot snapshot = new RepositorySnapshot
        {
            Identifier = "_repository" + suffix,
            Edition = suffix
        };
        snapshot.DataDictionary = Reference(
            snapshot.Identifier,
            RepositoryEntityKind.Repository,
            "_dictionary" + suffix,
            RepositoryEntityKind.DataDictionary);
        snapshot.BusinessProcessCatalogue = Reference(
            snapshot.Identifier,
            RepositoryEntityKind.Repository,
            "_catalogue" + suffix,
            RepositoryEntityKind.BusinessProcessCatalogue);
        return snapshot;
    }

    private static BusinessComponentDefinition Component(
        string identifier,
        string name,
        RepositorySnapshot snapshot)
    {
        return new BusinessComponentDefinition
        {
            Identifier = identifier,
            Name = name,
            DataDictionary = Reference(
                identifier,
                RepositoryEntityKind.BusinessComponent,
                snapshot.DataDictionary.TargetIdentifier,
                RepositoryEntityKind.DataDictionary)
        };
    }

    private static void AddComponentAndElement(
        RepositorySnapshot snapshot,
        string componentIdentifier,
        string componentName,
        string elementIdentifier,
        string elementName)
    {
        snapshot.MessageComponents.Add(new MessageComponentDefinition
        {
            Identifier = componentIdentifier,
            Name = componentName,
            DataDictionary = Reference(
                componentIdentifier,
                RepositoryEntityKind.MessageComponent,
                snapshot.DataDictionary.TargetIdentifier,
                RepositoryEntityKind.DataDictionary)
        });
        snapshot.MessageElements.Add(new MessageElementDefinition
        {
            Identifier = elementIdentifier,
            Name = elementName,
            ComponentContext = Reference(
                elementIdentifier,
                RepositoryEntityKind.MessageElement,
                componentIdentifier,
                RepositoryEntityKind.MessageComponent)
        });
    }

    private static void AddCode(
        RepositorySnapshot snapshot,
        string codeSetIdentifier,
        string codeIdentifier,
        string codeName,
        string codeValue)
    {
        CodeSetDefinition codeSet = new CodeSetDefinition
        {
            Identifier = codeSetIdentifier,
            Name = "Status",
            DataDictionary = Reference(
                codeSetIdentifier,
                RepositoryEntityKind.CodeSet,
                snapshot.DataDictionary.TargetIdentifier,
                RepositoryEntityKind.DataDictionary)
        };
        codeSet.Codes.Add(new CodeValueDefinition
        {
            Identifier = codeIdentifier,
            Name = codeName,
            Value = codeValue,
            Owner = Reference(
                codeIdentifier,
                RepositoryEntityKind.CodeValue,
                codeSetIdentifier,
                RepositoryEntityKind.CodeSet)
        });
        snapshot.CodeSets.Add(codeSet);
    }

    private static RepositoryReference Reference(
        string sourceIdentifier,
        RepositoryEntityKind sourceKind,
        string targetIdentifier,
        RepositoryEntityKind targetKind)
    {
        return new RepositoryReference
        {
            SourceIdentifier = sourceIdentifier,
            SourceKind = sourceKind,
            TargetIdentifier = targetIdentifier,
            TargetKind = targetKind,
            IsResolved = true
        };
    }
}
