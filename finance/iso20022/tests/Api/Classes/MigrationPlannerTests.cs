using iso20022;

namespace iso20022.Tests;

public class MigrationPlannerTests
{
    [Test]
    public void ExplicitRulesProduceEveryActionInDeterministicSequence()
    {
        MigrationRequest request = RequestWithDifference(new RepositoryDiff { Success = true });
        request.AllowSupplementaryFallback = true;
        request.Mapping = CompleteMapping();

        MigrationPlan plan = new MigrationPlanner().PlanMigration(request);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Success, Is.True);
            Assert.That(plan.Steps.Select(step => step.Sequence),
                Is.EqualTo(Enumerable.Range(0, 10)));
            Assert.That(plan.Steps.Select(step => step.Action), Is.EqualTo(new[]
            {
                MigrationActionKind.Copy,
                MigrationActionKind.Rename,
                MigrationActionKind.Move,
                MigrationActionKind.Convert,
                MigrationActionKind.DefaultValue,
                MigrationActionKind.Split,
                MigrationActionKind.Merge,
                MigrationActionKind.CodeMap,
                MigrationActionKind.Drop,
                MigrationActionKind.Custom
            }));
            Assert.That(plan.Steps[3].ConverterIdentifier, Is.EqualTo("decimal-to-text"));
            Assert.That(plan.Steps[4].DefaultValue, Is.EqualTo("DEFAULT"));
            Assert.That(plan.Steps[5].TargetPaths, Has.Count.EqualTo(2));
            Assert.That(plan.Steps[6].SourcePaths, Has.Count.EqualTo(2));
            Assert.That(plan.Steps[7].SourceCodeValues,
                Is.EqualTo(new[] { "A", "B" }));
            Assert.That(plan.Steps[7].TargetCodeValues,
                Is.EqualTo(new[] { "X", "Y" }));
            Assert.That(plan.Steps.All(step => step.MappingRule != null), Is.True);
            Assert.That(plan.HasDataLoss, Is.True);
            Assert.That(plan.RequiresCustomRules, Is.True);
            Assert.That(plan.SupplementaryFallbackAuthorized, Is.True);
        });
    }

    [Test]
    public void AutomaticPlanningCoversDifferencesAndRetainsCompatibility()
    {
        RepositoryDiff difference = new RepositoryDiff { Success = true };
        CompatibilityAssessment compatibility = new CompatibilityAssessment
        {
            Kind = CompatibilityKind.DataLoss,
            Lossless = false
        };
        difference.Compatibility.Add(compatibility);
        difference.Differences.Add(Difference(DifferenceKind.Unchanged, "_copy", "_copy"));
        difference.Differences.Add(Difference(DifferenceKind.Renamed, "_rename", "_renameTarget"));
        difference.Differences.Add(Difference(DifferenceKind.Moved, "_move", "_moveTarget"));
        difference.Differences.Add(Difference(DifferenceKind.TypeChanged, "_type", "_typeTarget"));
        difference.Differences.Add(Difference(DifferenceKind.Added, string.Empty, "_required"));
        difference.Differences.Add(Difference(DifferenceKind.Removed, "_removed", string.Empty));
        difference.Differences.Add(Difference(
            DifferenceKind.CardinalityChanged,
            "_cardinality",
            "_cardinalityTarget"));
        difference.Differences.Add(Difference(DifferenceKind.FacetChanged, "_facet", "_facetTarget"));
        difference.Differences.Add(Difference(DifferenceKind.CodeChanged, "_code", "_codeTarget"));
        difference.Differences.Add(Difference(
            DifferenceKind.ConstraintAdded,
            string.Empty,
            "_constraint"));
        MigrationRequest request = RequestWithDifference(difference);
        request.TargetRepository = new RepositorySnapshot();
        request.TargetRepository.MessageElements.Add(new MessageElementDefinition
        {
            Identifier = "_required",
            Name = "Required",
            Multiplicity = new MultiplicityDefinition
            {
                MinimumOccurs = 1,
                HasMaximumOccurs = true,
                MaximumOccurs = 1
            }
        });

        MigrationPlan first = new MigrationPlanner().PlanMigration(request);
        MigrationPlan second = new MigrationPlanner().PlanMigration(request);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Steps.Select(step => step.Action), Is.EqualTo(new[]
            {
                MigrationActionKind.Copy,
                MigrationActionKind.Rename,
                MigrationActionKind.Move,
                MigrationActionKind.Convert,
                MigrationActionKind.DefaultValue,
                MigrationActionKind.Drop,
                MigrationActionKind.Convert,
                MigrationActionKind.Convert,
                MigrationActionKind.Custom,
                MigrationActionKind.Custom
            }));
            Assert.That(first.Steps[3].ConverterIdentifier,
                Is.EqualTo("iso20022.convert.type"));
            Assert.That(first.Steps[4].Required, Is.True);
            Assert.That(first.Steps[4].Difference?.TargetIdentifier,
                Is.EqualTo("_required"));
            Assert.That(first.HasDataLoss, Is.True);
            Assert.That(first.RequiresCustomRules, Is.True);
            Assert.That(first.Compatibility, Is.SameAs(compatibility));
            Assert.That(StepKeys(first), Is.EqualTo(StepKeys(second)));
        });
    }

    [Test]
    public void ExplicitRuleCoversDifferenceButIdentityOverrideDoesNot()
    {
        RepositoryDiff difference = new RepositoryDiff { Success = true };
        difference.Differences.Add(Difference(
            DifferenceKind.Renamed,
            "_source",
            "_target"));
        MigrationRequest request = RequestWithDifference(difference);
        request.Mapping = new MappingDefinition
        {
            SourceMessageIdentifier = Identifier("10"),
            TargetMessageIdentifier = Identifier("11")
        };
        request.Mapping.Rules.Add(new MappingRuleDefinition
        {
            Sequence = 0,
            SourceIdentifier = "_source",
            TargetIdentifier = "_target",
            Action = MappingActionKind.Unknown
        });
        request.Mapping.Rules.Add(new MappingRuleDefinition
        {
            Identifier = "rename-override",
            Sequence = 1,
            Action = MappingActionKind.Rename,
            SourceIdentifier = "_source",
            TargetIdentifier = "_target"
        });

        MigrationPlan covered = new MigrationPlanner().PlanMigration(request);
        request.Mapping.Rules.RemoveAt(1);
        MigrationPlan identityOnly = new MigrationPlanner().PlanMigration(request);

        Assert.Multiple(() =>
        {
            Assert.That(covered.Success, Is.True);
            Assert.That(covered.Steps, Has.Count.EqualTo(1));
            Assert.That(covered.Steps[0].MappingRule?.Identifier,
                Is.EqualTo("rename-override"));
            Assert.That(identityOnly.Success, Is.True);
            Assert.That(identityOnly.Steps, Has.Count.EqualTo(1));
            Assert.That(identityOnly.Steps[0].MappingRule, Is.Null);
            Assert.That(identityOnly.Steps[0].Action, Is.EqualTo(MigrationActionKind.Rename));
        });
    }

    [Test]
    public void PlannerComputesExactMessageDifferenceWhenNotSupplied()
    {
        RepositorySnapshot source = SnapshotWithMessage("10");
        RepositorySnapshot target = SnapshotWithMessage("11");
        MigrationRequest request = new MigrationRequest
        {
            SourceRepository = source,
            TargetRepository = target,
            SourceMessageIdentifier = Identifier("10"),
            TargetMessageIdentifier = Identifier("11")
        };

        MigrationPlan plan = new MigrationPlanner().PlanMigration(request);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Success, Is.True);
            Assert.That(plan.Difference, Is.Not.Null);
            Assert.That(plan.Difference?.Success, Is.True);
            Assert.That(plan.Compatibility, Is.Not.Null);
            Assert.That(plan.Steps, Is.Not.Empty);
        });
    }

    [Test]
    public void PlannerRejectsInvalidRequestsMappingsAndStepLimits()
    {
        MigrationPlanner planner = new MigrationPlanner();
        MigrationPlan missing = planner.PlanMigration(null);
        MigrationPlan invalidIdentifier = planner.PlanMigration(new MigrationRequest());

        MigrationRequest duplicateSequence = RequestWithDifference(
            new RepositoryDiff { Success = true });
        duplicateSequence.Mapping = new MappingDefinition();
        duplicateSequence.Mapping.Rules.Add(CopyRule(0, "first"));
        duplicateSequence.Mapping.Rules.Add(CopyRule(0, "second"));
        MigrationPlan duplicate = planner.PlanMigration(duplicateSequence);

        MigrationRequest invalidConvert = RequestWithDifference(
            new RepositoryDiff { Success = true });
        invalidConvert.Mapping = new MappingDefinition();
        invalidConvert.Mapping.Rules.Add(new MappingRuleDefinition
        {
            Sequence = 0,
            Action = MappingActionKind.Convert,
            SourcePaths = new List<XmlPathData> { Path("/Source") },
            TargetPaths = new List<XmlPathData> { Path("/Target") }
        });
        MigrationPlan badRule = planner.PlanMigration(invalidConvert);

        RepositoryDiff twoDifferences = new RepositoryDiff { Success = true };
        twoDifferences.Differences.Add(Difference(DifferenceKind.Renamed, "_one", "_oneT"));
        twoDifferences.Differences.Add(Difference(DifferenceKind.Moved, "_two", "_twoT"));
        MigrationRequest limitedRequest = RequestWithDifference(twoDifferences);
        limitedRequest.Limits.MaxCollectionCount = 1;
        MigrationPlan limited = planner.PlanMigration(limitedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(missing.ErrorCode, Is.EqualTo("migration.request_missing"));
            Assert.That(invalidIdentifier.ErrorCode,
                Is.EqualTo("migration.source_message_identifier_invalid"));
            Assert.That(duplicate.ErrorCode,
                Is.EqualTo("migration.mapping_sequence_duplicate"));
            Assert.That(badRule.ErrorCode, Is.EqualTo("migration.mapping_rule_invalid"));
            Assert.That(limited.ErrorCode, Is.EqualTo("migration.step_limit"));
            Assert.That(limited.Success, Is.False);
        });
    }

    [Test]
    public void PlannerHonorsCancellationAndMappingMessageScope()
    {
        MigrationRequest cancelledRequest = RequestWithDifference(
            new RepositoryDiff { Success = true });
        cancelledRequest.CancellationToken = new CancellationToken(true);
        MigrationPlan cancelled = new MigrationPlanner().PlanMigration(cancelledRequest);

        MigrationRequest mismatchRequest = RequestWithDifference(
            new RepositoryDiff { Success = true });
        mismatchRequest.Mapping = new MappingDefinition
        {
            SourceMessageIdentifier = Identifier("09"),
            TargetMessageIdentifier = Identifier("11")
        };
        MigrationPlan mismatch = new MigrationPlanner().PlanMigration(mismatchRequest);

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.ErrorCode, Is.EqualTo("migration.cancelled"));
            Assert.That(mismatch.ErrorCode,
                Is.EqualTo("migration.mapping_message_mismatch"));
        });
    }

    private static MappingDefinition CompleteMapping()
    {
        MappingDefinition mapping = new MappingDefinition
        {
            SourceMessageIdentifier = Identifier("10"),
            TargetMessageIdentifier = Identifier("11")
        };
        List<MappingRuleDefinition> rules = new List<MappingRuleDefinition>();
        rules.Add(Rule(0, MappingActionKind.Copy, "copy"));
        rules.Add(Rule(1, MappingActionKind.Rename, "rename"));
        rules.Add(Rule(2, MappingActionKind.Move, "move"));
        MappingRuleDefinition convert = Rule(3, MappingActionKind.Convert, "convert");
        convert.ConverterIdentifier = "decimal-to-text";
        rules.Add(convert);
        MappingRuleDefinition constant = Rule(4, MappingActionKind.Constant, "constant");
        constant.SourcePaths.Clear();
        constant.ConstantValue = "DEFAULT";
        rules.Add(constant);
        MappingRuleDefinition split = Rule(5, MappingActionKind.Split, "split");
        split.TargetPaths.Add(Path("/Target/splitTwo"));
        rules.Add(split);
        MappingRuleDefinition merge = Rule(6, MappingActionKind.Merge, "merge");
        merge.SourcePaths.Add(Path("/Source/mergeTwo"));
        rules.Add(merge);
        MappingRuleDefinition codeMap = Rule(7, MappingActionKind.CodeMap, "code-map");
        codeMap.SourceCodeValues.Add("A");
        codeMap.SourceCodeValues.Add("B");
        codeMap.TargetCodeValues.Add("X");
        codeMap.TargetCodeValues.Add("Y");
        rules.Add(codeMap);
        MappingRuleDefinition drop = Rule(8, MappingActionKind.Drop, "drop");
        drop.TargetPaths.Clear();
        rules.Add(drop);
        MappingRuleDefinition custom = Rule(9, MappingActionKind.Custom, "custom");
        custom.ConverterIdentifier = "caller.custom";
        rules.Add(custom);
        int index = rules.Count - 1;
        while (index >= 0)
        {
            mapping.Rules.Add(rules[index]);
            index--;
        }

        return mapping;
    }

    private static MappingRuleDefinition Rule(
        int sequence,
        MappingActionKind action,
        string identifier)
    {
        return new MappingRuleDefinition
        {
            Identifier = identifier,
            Sequence = sequence,
            Action = action,
            SourcePaths = new List<XmlPathData> { Path("/Source/" + identifier) },
            TargetPaths = new List<XmlPathData> { Path("/Target/" + identifier) },
            Required = true,
            Rationale = "Rule " + identifier
        };
    }

    private static MappingRuleDefinition CopyRule(int sequence, string identifier)
    {
        return Rule(sequence, MappingActionKind.Copy, identifier);
    }

    private static MigrationRequest RequestWithDifference(RepositoryDiff difference)
    {
        return new MigrationRequest
        {
            SourceMessageIdentifier = Identifier("10"),
            TargetMessageIdentifier = Identifier("11"),
            Difference = difference
        };
    }

    private static DifferenceItem Difference(
        DifferenceKind kind,
        string sourceIdentifier,
        string targetIdentifier)
    {
        DifferenceItem difference = new DifferenceItem
        {
            Kind = kind,
            EntityKind = RepositoryEntityKind.MessageElement,
            SourceIdentifier = sourceIdentifier,
            TargetIdentifier = targetIdentifier,
            SourcePath = Path("/Source/" + sourceIdentifier),
            TargetPath = Path("/Target/" + targetIdentifier),
            PropertyName = "Value"
        };
        if (kind == DifferenceKind.TypeChanged)
        {
            difference.SourceValue = "SourceType";
            difference.TargetValue = "TargetType";
        }

        if (kind == DifferenceKind.CardinalityChanged)
        {
            difference.SourceValue = "0|true|5";
            difference.TargetValue = "1|true|3";
            difference.DataLossRisk = true;
        }

        if (kind == DifferenceKind.FacetChanged)
        {
            difference.SourceValue = "maxLength=35";
            difference.TargetValue = "maxLength=20";
        }

        return difference;
    }

    private static XmlPathData Path(string value)
    {
        return new XmlPathData { Value = value };
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

    private static RepositorySnapshot SnapshotWithMessage(string version)
    {
        RepositorySnapshot snapshot = new RepositorySnapshot
        {
            Identifier = "_repository" + version,
            Edition = version
        };
        snapshot.MessageDefinitions.Add(new MessageDefinitionMetadata
        {
            Identifier = "_message" + version,
            Name = "Payment",
            XmlName = "Payment",
            MessageIdentifier = Identifier(version)
        });
        return snapshot;
    }

    private static string[] StepKeys(MigrationPlan plan)
    {
        return plan.Steps.Select(step =>
            step.Sequence + "|" + ((int)step.Action) + "|" +
            step.SourceIdentifier + "|" + step.TargetIdentifier + "|" +
            step.ConverterIdentifier + "|" + step.Required + "|" + step.Lossy).ToArray();
    }
}
