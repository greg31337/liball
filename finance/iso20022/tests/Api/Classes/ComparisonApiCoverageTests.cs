using iso20022;

namespace iso20022.Tests;

public class ComparisonApiCoverageTests
{
    [Test]
    public void PublicRepositoryComparisonProducesEveryCompatibilityOutcome()
    {
        RepositoryDiff compatible = Compare(Snapshot(), Snapshot());

        RepositorySnapshot warningSource = Snapshot();
        RepositorySnapshot warningTarget = Snapshot();
        warningTarget.BusinessComponents.Add(Component("_added", "Added", warningTarget));
        RepositoryDiff warning = Compare(warningSource, warningTarget);

        RepositorySnapshot conditionalSource = Snapshot();
        RepositorySnapshot conditionalTarget = Snapshot();
        conditionalSource.BusinessComponents.Add(Component("_meaning", "Meaning", conditionalSource, "old"));
        conditionalTarget.BusinessComponents.Add(Component("_meaning", "Meaning", conditionalTarget, "new"));
        RepositoryDiff conditional = Compare(conditionalSource, conditionalTarget);

        RepositorySnapshot incompatibleSource = Snapshot();
        RepositorySnapshot incompatibleTarget = Snapshot();
        incompatibleSource.BusinessComponents.Add(Component("_renamed", "OldName", incompatibleSource));
        incompatibleTarget.BusinessComponents.Add(Component("_renamed", "NewName", incompatibleTarget));
        RepositoryDiff incompatible = Compare(incompatibleSource, incompatibleTarget);

        RepositorySnapshot lossSource = Snapshot();
        RepositorySnapshot lossTarget = Snapshot();
        lossSource.BusinessComponents.Add(Component("_removed", "Removed", lossSource));
        RepositoryDiff loss = Compare(lossSource, lossTarget);

        Assert.Multiple(() =>
        {
            Assert.That(compatible.Compatibility.Single().Kind,
                Is.EqualTo(CompatibilityKind.Compatible));
            Assert.That(warning.Compatibility.Single().Kind,
                Is.EqualTo(CompatibilityKind.CompatibleWithWarnings));
            Assert.That(conditional.Compatibility.Single().Kind,
                Is.EqualTo(CompatibilityKind.ConditionallyCompatible));
            Assert.That(incompatible.Compatibility.Single().Kind,
                Is.EqualTo(CompatibilityKind.Incompatible));
            Assert.That(loss.Compatibility.Single().Kind,
                Is.EqualTo(CompatibilityKind.DataLoss));
            Assert.That(loss.Compatibility.Single().Lossless, Is.False);
        });
    }

    [Test]
    public void ExactSemanticAndAmbiguousIdentityMatchingAreDeterministic()
    {
        RepositorySnapshot source = Snapshot();
        RepositorySnapshot target = Snapshot();
        source.BusinessComponents.Add(Component("_exact", "Exact", source));
        target.BusinessComponents.Add(Component("_exact", "Exact", target));
        source.BusinessComponents.Add(Component("_semanticSource", "Semantic", source));
        target.BusinessComponents.Add(Component("_semanticTarget", "Semantic", target));
        source.BusinessComponents.Add(Component("_ambiguousSource", "Shared", source));
        target.BusinessComponents.Add(Component("_ambiguousB", "Shared", target));
        target.BusinessComponents.Add(Component("_ambiguousA", "Shared", target));
        RepositoryComparisonRequest request = new RepositoryComparisonRequest
        {
            Source = source,
            Target = target,
            IncludeUnchanged = true
        };

        RepositoryDiff first = new RepositoryComparer().CompareRepositories(request);
        RepositoryDiff second = new RepositoryComparer().CompareRepositories(request);
        DifferenceItem exact = first.Differences.Single(item =>
            item.SourceIdentifier == "_exact");
        DifferenceItem semantic = first.Differences.Single(item =>
            item.SourceIdentifier == "_semanticSource");
        DifferenceItem ambiguous = first.Differences.Single(item =>
            item.SourceIdentifier == "_ambiguousSource");

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(exact.Kind, Is.EqualTo(DifferenceKind.Unchanged));
            Assert.That(exact.ExactIdentifierMatch, Is.True);
            Assert.That(exact.SemanticFallbackMatch, Is.False);
            Assert.That(semantic.Kind, Is.EqualTo(DifferenceKind.Unchanged));
            Assert.That(semantic.ExactIdentifierMatch, Is.False);
            Assert.That(semantic.SemanticFallbackMatch, Is.True);
            Assert.That(semantic.MatchConfidence, Is.GreaterThanOrEqualTo(0.80m));
            Assert.That(ambiguous.Kind, Is.EqualTo(DifferenceKind.AmbiguousMatch));
            Assert.That(ambiguous.Ambiguous, Is.True);
            Assert.That(ambiguous.AlternativeTargetIdentifiers,
                Is.EqualTo(new[] { "_ambiguousA", "_ambiguousB" }));
            Assert.That(first.HasAmbiguousMatches, Is.True);
            Assert.That(DifferenceKeys(first), Is.EqualTo(DifferenceKeys(second)));
        });
    }

    [Test]
    public void MessageComparisonCompatibilityOutputIsStableAcrossRepeatedRuns()
    {
        RepositorySnapshot source = MessageSnapshot("10", 0, 5);
        RepositorySnapshot target = MessageSnapshot("11", 1, 3);
        RepositoryComparisonRequest request = new RepositoryComparisonRequest
        {
            Source = source,
            Target = target,
            SourceMessageIdentifier = Identifier("10"),
            TargetMessageIdentifier = Identifier("11")
        };

        RepositoryDiff first = new MessageVersionComparer().CompareMessageVersions(request);
        RepositoryDiff second = new MessageVersionComparer().CompareMessageVersions(request);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.CardinalityChanged));
            Assert.That(first.Compatibility.Single().Kind,
                Is.EqualTo(CompatibilityKind.DataLoss));
            Assert.That(DifferenceKeys(first), Is.EqualTo(DifferenceKeys(second)));
            Assert.That(CompatibilityKeys(first), Is.EqualTo(CompatibilityKeys(second)));
        });
    }

    private static RepositoryDiff Compare(
        RepositorySnapshot source,
        RepositorySnapshot target)
    {
        return new RepositoryComparer().CompareRepositories(new RepositoryComparisonRequest
        {
            Source = source,
            Target = target
        });
    }

    private static RepositorySnapshot Snapshot()
    {
        RepositorySnapshot snapshot = new RepositorySnapshot
        {
            Identifier = "_repository",
            Edition = "edition"
        };
        snapshot.DataDictionary = Reference(
            snapshot.Identifier,
            RepositoryEntityKind.Repository,
            "dataDictionary",
            "_dictionary",
            RepositoryEntityKind.DataDictionary,
            0);
        snapshot.References.Add(snapshot.DataDictionary);
        return snapshot;
    }

    private static BusinessComponentDefinition Component(
        string identifier,
        string name,
        RepositorySnapshot snapshot,
        string definition = "same")
    {
        return new BusinessComponentDefinition
        {
            Identifier = identifier,
            Name = name,
            Definition = definition,
            DataDictionary = Reference(
                identifier,
                RepositoryEntityKind.BusinessComponent,
                "dataDictionary",
                snapshot.DataDictionary.TargetIdentifier,
                RepositoryEntityKind.DataDictionary,
                0)
        };
    }

    private static RepositorySnapshot MessageSnapshot(
        string version,
        int minimum,
        int maximum)
    {
        RepositorySnapshot snapshot = Snapshot();
        MessageDefinitionMetadata message = new MessageDefinitionMetadata
        {
            Identifier = "_message",
            Name = "PaymentMessage",
            MessageIdentifier = Identifier(version),
            RootElement = "Document",
            NamespaceUri = "urn:iso:std:iso:20022:tech:xsd:pacs.008.001." + version
        };
        MessageBuildingBlockDefinition block = new MessageBuildingBlockDefinition
        {
            Identifier = "_block",
            Name = "Payment",
            MessageDefinition = Reference(
                "_block",
                RepositoryEntityKind.MessageBuildingBlock,
                "messageDefinition",
                message.Identifier,
                RepositoryEntityKind.MessageDefinition,
                0),
            MemberType = Reference(
                "_block",
                RepositoryEntityKind.MessageBuildingBlock,
                "memberType",
                "_component",
                RepositoryEntityKind.MessageComponent,
                0),
            Multiplicity = new MultiplicityDefinition
            {
                MinimumOccurs = minimum,
                HasMaximumOccurs = true,
                MaximumOccurs = maximum
            }
        };
        message.BuildingBlocks.Add(Reference(
            message.Identifier,
            RepositoryEntityKind.MessageDefinition,
            "buildingBlocks",
            block.Identifier,
            RepositoryEntityKind.MessageBuildingBlock,
            0));
        snapshot.MessageDefinitions.Add(message);
        snapshot.MessageBuildingBlocks.Add(block);
        snapshot.MessageComponents.Add(new MessageComponentDefinition
        {
            Identifier = "_component",
            Name = "Payment",
            DataDictionary = Reference(
                "_component",
                RepositoryEntityKind.MessageComponent,
                "dataDictionary",
                snapshot.DataDictionary.TargetIdentifier,
                RepositoryEntityKind.DataDictionary,
                0)
        });
        return snapshot;
    }

    private static MessageIdentifier Identifier(string version)
    {
        return new MessageIdentifier
        {
            BusinessArea = "pacs",
            MessageFunctionality = "008",
            Variant = "001",
            Version = version,
            Value = "pacs.008.001." + version
        };
    }

    private static RepositoryReference Reference(
        string sourceIdentifier,
        RepositoryEntityKind sourceKind,
        string propertyName,
        string targetIdentifier,
        RepositoryEntityKind targetKind,
        int position)
    {
        return new RepositoryReference
        {
            SourceIdentifier = sourceIdentifier,
            SourceKind = sourceKind,
            PropertyName = propertyName,
            TargetIdentifier = targetIdentifier,
            TargetKind = targetKind,
            Position = position,
            IsResolved = true
        };
    }

    private static IEnumerable<string> DifferenceKeys(RepositoryDiff result)
    {
        return result.Differences.Select(item =>
            $"{item.Kind}|{item.EntityKind}|{item.SourceIdentifier}|" +
            $"{item.TargetIdentifier}|{item.PropertyName}|{item.SourceValue}|{item.TargetValue}");
    }

    private static IEnumerable<string> CompatibilityKeys(RepositoryDiff result)
    {
        return result.Compatibility.Select(item =>
            $"{item.Kind}|{item.ReaderCompatible}|{item.WriterCompatible}|" +
            $"{item.SchemaCompatible}|{item.Lossless}|{item.SemanticallyCompatible}|" +
            $"{item.Confidence}|{string.Join(";", item.Rationale)}");
    }
}
