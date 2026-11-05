using System.Collections.Generic;

namespace iso20022;

internal static class MigrationPlanBuilder
{
    internal static MigrationPlan BuildPlan(MigrationRequest request)
    {
        MigrationPlan plan = new MigrationPlan();
        plan.SourceMessageIdentifier = request.SourceMessageIdentifier;
        plan.TargetMessageIdentifier = request.TargetMessageIdentifier;
        plan.SupplementaryFallbackAuthorized = request.AllowSupplementaryFallback;
        if (request.CancellationToken.IsCancellationRequested)
        {
            return FailPlan(
                plan,
                "migration.cancelled",
                "Migration planning was cancelled.");
        }

        if (request.Limits == null || request.Limits.MaxCollectionCount < 1)
        {
            return FailPlan(
                plan,
                "migration.resource_limit_invalid",
                "The migration step collection limit must be positive.");
        }

        if (request.SourceMessageIdentifier == null ||
            MessageIdentifierLexical.FormatMessageIdentifier(
                request.SourceMessageIdentifier) == null)
        {
            return FailPlan(
                plan,
                "migration.source_message_identifier_invalid",
                "A valid exact source message identifier is required.");
        }

        if (request.TargetMessageIdentifier == null ||
            MessageIdentifierLexical.FormatMessageIdentifier(
                request.TargetMessageIdentifier) == null)
        {
            return FailPlan(
                plan,
                "migration.target_message_identifier_invalid",
                "A valid exact target message identifier is required.");
        }

        RepositoryDiff? difference = ResolveDifference(request);
        if (difference == null)
        {
            return FailPlan(
                plan,
                "migration.difference_required",
                "A successful difference or both exact repository snapshots are required.");
        }

        if (!difference.Success)
        {
            return FailPlan(
                plan,
                "migration.difference_failed",
                "The source-to-target difference did not complete successfully.");
        }

        plan.Difference = difference;
        if (difference.Compatibility.Count > 0)
        {
            plan.Compatibility = difference.Compatibility[0];
        }

        List<MappingRuleDefinition> rules = new List<MappingRuleDefinition>();
        if (!ValidateAndOrderRules(request, plan, rules))
        {
            return plan;
        }

        int ruleIndex = 0;
        while (ruleIndex < rules.Count)
        {
            MappingRuleDefinition rule = rules[ruleIndex];
            if (rule.Action != MappingActionKind.Unknown)
            {
                MigrationStep? explicitStep = CreateExplicitStep(rule, plan);
                if (explicitStep == null)
                {
                    return plan;
                }

                if (!AppendStep(request, plan, explicitStep))
                {
                    return plan;
                }
            }

            ruleIndex++;
        }

        int differenceIndex = 0;
        while (differenceIndex < difference.Differences.Count)
        {
            if (request.CancellationToken.IsCancellationRequested)
            {
                return FailPlan(
                    plan,
                    "migration.cancelled",
                    "Migration planning was cancelled.");
            }

            DifferenceItem item = difference.Differences[differenceIndex];
            if (!DifferenceCoveredByRules(item, rules))
            {
                MigrationStep? automaticStep = CreateAutomaticStep(request, item);
                if (automaticStep != null && !AppendStep(request, plan, automaticStep))
                {
                    return plan;
                }
            }

            differenceIndex++;
        }

        plan.Success = true;
        return plan;
    }

    internal static RepositoryDiff? ResolveDifference(MigrationRequest request)
    {
        if (request.Difference != null)
        {
            return request.Difference;
        }

        if (request.SourceRepository == null || request.TargetRepository == null ||
            request.SourceMessageIdentifier == null ||
            request.TargetMessageIdentifier == null)
        {
            return null;
        }

        RepositoryComparisonRequest comparison = new RepositoryComparisonRequest();
        comparison.Source = request.SourceRepository;
        comparison.Target = request.TargetRepository;
        comparison.SourceMessageIdentifier = request.SourceMessageIdentifier;
        comparison.TargetMessageIdentifier = request.TargetMessageIdentifier;
        comparison.MatchOverrides = request.Mapping;
        comparison.Limits = request.Limits;
        comparison.CancellationToken = request.CancellationToken;
        return new MessageVersionComparer().CompareMessageVersions(comparison);
    }

    internal static bool ValidateAndOrderRules(
        MigrationRequest request,
        MigrationPlan plan,
        List<MappingRuleDefinition> ordered)
    {
        if (request.Mapping == null)
        {
            return true;
        }

        if (!MappingIdentifierMatches(
            request.Mapping.SourceMessageIdentifier,
            request.SourceMessageIdentifier) ||
            !MappingIdentifierMatches(
                request.Mapping.TargetMessageIdentifier,
                request.TargetMessageIdentifier))
        {
            FailPlan(
                plan,
                "migration.mapping_message_mismatch",
                "The mapping definition does not select the requested exact message versions.");
            return false;
        }

        int ruleIndex = 0;
        while (ruleIndex < request.Mapping.Rules.Count)
        {
            MappingRuleDefinition rule = request.Mapping.Rules[ruleIndex];
            string validationError = ValidateExplicitRule(rule);
            if (validationError.Length > 0)
            {
                FailPlan(plan, "migration.mapping_rule_invalid", validationError);
                return false;
            }

            if (rule.Sequence < 0)
            {
                FailPlan(
                    plan,
                    "migration.mapping_sequence_invalid",
                    "Mapping rule sequences must be non-negative.");
                return false;
            }

            int existingIndex = 0;
            while (existingIndex < ordered.Count)
            {
                if (ordered[existingIndex].Sequence == rule.Sequence)
                {
                    FailPlan(
                        plan,
                        "migration.mapping_sequence_duplicate",
                        "Mapping rule sequences must be unique.");
                    return false;
                }

                existingIndex++;
            }

            InsertOrderedRule(ordered, rule);
            ruleIndex++;
        }

        return true;
    }

    internal static bool MappingIdentifierMatches(
        MessageIdentifier? mappingIdentifier,
        MessageIdentifier? requestIdentifier)
    {
        if (mappingIdentifier == null)
        {
            return true;
        }

        if (requestIdentifier == null)
        {
            return false;
        }

        string? mappingValue = MessageIdentifierLexical.FormatMessageIdentifier(mappingIdentifier);
        string? requestValue = MessageIdentifierLexical.FormatMessageIdentifier(requestIdentifier);
        return mappingValue != null && mappingValue == requestValue;
    }

    internal static void InsertOrderedRule(
        List<MappingRuleDefinition> ordered,
        MappingRuleDefinition rule)
    {
        int index = 0;
        while (index < ordered.Count && ordered[index].Sequence < rule.Sequence)
        {
            index++;
        }

        ordered.Insert(index, rule);
    }

    internal static MigrationStep? CreateExplicitStep(
        MappingRuleDefinition rule,
        MigrationPlan plan)
    {
        string validationError = ValidateExplicitRule(rule);
        if (validationError.Length > 0)
        {
            FailPlan(plan, "migration.mapping_rule_invalid", validationError);
            return null;
        }

        MigrationStep step = new MigrationStep();
        step.Action = ResolveMigrationAction(rule.Action);
        step.SourceIdentifier = rule.SourceIdentifier;
        step.TargetIdentifier = rule.TargetIdentifier;
        CopyRulePaths(rule.SourcePaths, step.SourcePaths);
        CopyRulePaths(rule.TargetPaths, step.TargetPaths);
        if (step.SourcePaths.Count == 0 && rule.SourceMember.Length > 0)
        {
            step.SourcePaths.Add(CreateMemberPath(rule.SourceMember));
        }

        if (step.TargetPaths.Count == 0 && rule.TargetMember.Length > 0)
        {
            step.TargetPaths.Add(CreateMemberPath(rule.TargetMember));
        }

        step.DefaultValue = rule.ConstantValue;
        step.ConverterIdentifier = rule.ConverterIdentifier;
        CopyCodeValues(rule.SourceCodeValues, step.SourceCodeValues);
        CopyCodeValues(rule.TargetCodeValues, step.TargetCodeValues);
        step.Required = rule.Required;
        step.Lossy = rule.Lossy || rule.Action == MappingActionKind.Drop;
        step.MappingRule = rule;
        step.Rationale = rule.Rationale.Length > 0 ? rule.Rationale :
            "The caller supplied the explicit mapping rule " + rule.Identifier + ".";
        return step;
    }

    internal static string ValidateExplicitRule(MappingRuleDefinition rule)
    {
        if ((int)rule.Action < (int)MappingActionKind.Unknown ||
            (int)rule.Action > (int)MappingActionKind.Custom)
        {
            return "The mapping rule action is not recognized.";
        }

        if (rule.Action == MappingActionKind.Unknown)
        {
            if (rule.SourceIdentifier.Length > 0 && rule.TargetIdentifier.Length > 0)
            {
                return string.Empty;
            }

            return "A mapping rule action is required.";
        }

        bool hasSource = RuleHasSource(rule);
        bool hasTarget = RuleHasTarget(rule);
        if (rule.Action == MappingActionKind.Copy ||
            rule.Action == MappingActionKind.Rename ||
            rule.Action == MappingActionKind.Move)
        {
            if (!hasSource || !hasTarget)
            {
                return "Copy, rename, and move rules require source and target locations.";
            }
        }

        if (rule.Action == MappingActionKind.Convert)
        {
            if (!hasSource || !hasTarget || rule.ConverterIdentifier.Length == 0)
            {
                return "Conversion rules require source, target, and converter identifiers.";
            }
        }

        if (rule.Action == MappingActionKind.Constant && !hasTarget)
        {
            return "Constant rules require a target location.";
        }

        if (rule.Action == MappingActionKind.Split)
        {
            if (!hasSource || rule.TargetPaths.Count < 2)
            {
                return "Split rules require a source and at least two target paths.";
            }
        }

        if (rule.Action == MappingActionKind.Merge)
        {
            if (rule.SourcePaths.Count < 2 || !hasTarget)
            {
                return "Merge rules require at least two source paths and a target.";
            }
        }

        if (rule.Action == MappingActionKind.CodeMap)
        {
            if (!hasSource || !hasTarget ||
                rule.SourceCodeValues.Count == 0 ||
                rule.SourceCodeValues.Count != rule.TargetCodeValues.Count)
            {
                return "Code-map rules require aligned non-empty source and target code values.";
            }
        }

        if (rule.Action == MappingActionKind.Drop && !hasSource)
        {
            return "Drop rules require a source location.";
        }

        if (rule.Action == MappingActionKind.Custom)
        {
            if (!hasSource && !hasTarget)
            {
                return "Custom rules require a source or target location.";
            }

            if (rule.ConverterIdentifier.Length == 0 && rule.Identifier.Length == 0)
            {
                return "Custom rules require a stable rule or converter identifier.";
            }
        }

        return string.Empty;
    }

    internal static bool RuleHasSource(MappingRuleDefinition rule)
    {
        return rule.SourcePaths.Count > 0 ||
            rule.SourceMember.Length > 0 ||
            rule.SourceIdentifier.Length > 0;
    }

    internal static bool RuleHasTarget(MappingRuleDefinition rule)
    {
        return rule.TargetPaths.Count > 0 ||
            rule.TargetMember.Length > 0 ||
            rule.TargetIdentifier.Length > 0;
    }

    internal static MigrationActionKind ResolveMigrationAction(MappingActionKind action)
    {
        if (action == MappingActionKind.Copy)
        {
            return MigrationActionKind.Copy;
        }

        if (action == MappingActionKind.Rename)
        {
            return MigrationActionKind.Rename;
        }

        if (action == MappingActionKind.Move)
        {
            return MigrationActionKind.Move;
        }

        if (action == MappingActionKind.Convert)
        {
            return MigrationActionKind.Convert;
        }

        if (action == MappingActionKind.Constant)
        {
            return MigrationActionKind.DefaultValue;
        }

        if (action == MappingActionKind.Split)
        {
            return MigrationActionKind.Split;
        }

        if (action == MappingActionKind.Merge)
        {
            return MigrationActionKind.Merge;
        }

        if (action == MappingActionKind.CodeMap)
        {
            return MigrationActionKind.CodeMap;
        }

        if (action == MappingActionKind.Drop)
        {
            return MigrationActionKind.Drop;
        }

        if (action == MappingActionKind.Custom)
        {
            return MigrationActionKind.Custom;
        }

        return MigrationActionKind.Unknown;
    }

    internal static void CopyRulePaths(
        List<XmlPathData> source,
        List<XmlPathData> target)
    {
        int pathIndex = 0;
        while (pathIndex < source.Count)
        {
            target.Add(source[pathIndex]);
            pathIndex++;
        }
    }

    internal static void CopyCodeValues(
        List<string> source,
        List<string> target)
    {
        int valueIndex = 0;
        while (valueIndex < source.Count)
        {
            target.Add(source[valueIndex]);
            valueIndex++;
        }
    }

    internal static XmlPathData CreateMemberPath(string member)
    {
        XmlPathData path = new XmlPathData();
        path.Value = "/" + member;
        return path;
    }

    internal static bool DifferenceCoveredByRules(
        DifferenceItem difference,
        List<MappingRuleDefinition> rules)
    {
        int ruleIndex = 0;
        while (ruleIndex < rules.Count)
        {
            if (RuleCoversDifference(rules[ruleIndex], difference))
            {
                return true;
            }

            ruleIndex++;
        }

        return false;
    }

    internal static bool RuleCoversDifference(
        MappingRuleDefinition rule,
        DifferenceItem difference)
    {
        if (rule.Action == MappingActionKind.Unknown)
        {
            return false;
        }

        bool sourceMatches = rule.SourceIdentifier.Length > 0 &&
            rule.SourceIdentifier == difference.SourceIdentifier;
        bool targetMatches = rule.TargetIdentifier.Length > 0 &&
            rule.TargetIdentifier == difference.TargetIdentifier;
        if (sourceMatches || targetMatches)
        {
            return true;
        }

        int pathIndex = 0;
        while (pathIndex < rule.SourcePaths.Count)
        {
            if (rule.SourcePaths[pathIndex].Value == difference.SourcePath.Value &&
                difference.SourcePath.Value.Length > 0)
            {
                return true;
            }

            pathIndex++;
        }

        pathIndex = 0;
        while (pathIndex < rule.TargetPaths.Count)
        {
            if (rule.TargetPaths[pathIndex].Value == difference.TargetPath.Value &&
                difference.TargetPath.Value.Length > 0)
            {
                return true;
            }

            pathIndex++;
        }

        return false;
    }

    internal static MigrationStep? CreateAutomaticStep(
        MigrationRequest request,
        DifferenceItem difference)
    {
        MigrationActionKind action = ResolveAutomaticAction(difference.Kind);
        if (action == MigrationActionKind.Unknown)
        {
            return null;
        }

        MigrationStep step = new MigrationStep();
        step.Action = action;
        step.SourceIdentifier = difference.SourceIdentifier;
        step.TargetIdentifier = difference.TargetIdentifier;
        if (difference.SourcePath.Value.Length > 0)
        {
            step.SourcePaths.Add(difference.SourcePath);
        }

        if (difference.TargetPath.Value.Length > 0)
        {
            step.TargetPaths.Add(difference.TargetPath);
        }

        step.SourceType = difference.SourceValue;
        step.TargetType = difference.TargetValue;
        step.Difference = difference;
        step.Required = difference.Kind == DifferenceKind.Added &&
            TargetContentIsRequired(request, difference.TargetIdentifier);
        step.Lossy = difference.DataLossRisk ||
            action == MigrationActionKind.Drop ||
            difference.Kind == DifferenceKind.TypeChanged ||
            difference.Kind == DifferenceKind.FacetChanged ||
            difference.Kind == DifferenceKind.CodeRemoved ||
            difference.Kind == DifferenceKind.CodeChanged;
        if (action == MigrationActionKind.Convert)
        {
            step.ConverterIdentifier = ResolveAutomaticConverter(difference.Kind);
        }

        if (action == MigrationActionKind.Custom)
        {
            step.ConverterIdentifier =
                "iso20022.migration.custom." + ((int)difference.Kind).ToString();
        }

        step.Rationale = AutomaticRationale(difference, action);
        return step;
    }

    internal static MigrationActionKind ResolveAutomaticAction(DifferenceKind kind)
    {
        if (kind == DifferenceKind.Unchanged)
        {
            return MigrationActionKind.Copy;
        }

        if (kind == DifferenceKind.Renamed)
        {
            return MigrationActionKind.Rename;
        }

        if (kind == DifferenceKind.Moved)
        {
            return MigrationActionKind.Move;
        }

        if (kind == DifferenceKind.TypeChanged ||
            kind == DifferenceKind.CardinalityChanged ||
            kind == DifferenceKind.FacetChanged)
        {
            return MigrationActionKind.Convert;
        }

        if (kind == DifferenceKind.Added)
        {
            return MigrationActionKind.DefaultValue;
        }

        if (kind == DifferenceKind.Removed)
        {
            return MigrationActionKind.Drop;
        }

        if (kind == DifferenceKind.CodeRemoved ||
            kind == DifferenceKind.CodeChanged ||
            kind == DifferenceKind.ConstraintAdded ||
            kind == DifferenceKind.ConstraintChanged ||
            kind == DifferenceKind.Modified ||
            kind == DifferenceKind.RelationshipAdded ||
            kind == DifferenceKind.RelationshipRemoved ||
            kind == DifferenceKind.RelationshipChanged ||
            kind == DifferenceKind.SyntaxChanged ||
            kind == DifferenceKind.AmbiguousMatch ||
            kind == DifferenceKind.Conflict)
        {
            return MigrationActionKind.Custom;
        }

        return MigrationActionKind.Unknown;
    }

    internal static string ResolveAutomaticConverter(DifferenceKind kind)
    {
        if (kind == DifferenceKind.TypeChanged)
        {
            return "iso20022.convert.type";
        }

        if (kind == DifferenceKind.CardinalityChanged)
        {
            return "iso20022.convert.cardinality";
        }

        return "iso20022.convert.facet";
    }

    internal static string AutomaticRationale(
        DifferenceItem difference,
        MigrationActionKind action)
    {
        if (action == MigrationActionKind.Copy)
        {
            return "The matched content is copied without semantic change.";
        }

        if (action == MigrationActionKind.Rename)
        {
            return "The matched content is copied to its renamed target path.";
        }

        if (action == MigrationActionKind.Move)
        {
            return "The matched content is copied to its moved target location.";
        }

        if (action == MigrationActionKind.Convert)
        {
            return "The " + difference.PropertyName +
                " change requires the identified deterministic conversion.";
        }

        if (action == MigrationActionKind.DefaultValue)
        {
            return "The added target content requires an explicit default or omission decision.";
        }

        if (action == MigrationActionKind.Drop)
        {
            return "The removed source content is explicitly dropped with acknowledged data loss.";
        }

        return "The difference requires an explicit caller-implemented migration rule.";
    }

    internal static bool TargetContentIsRequired(
        MigrationRequest request,
        string targetIdentifier)
    {
        if (request.TargetRepository == null || targetIdentifier.Length == 0)
        {
            return false;
        }

        RepositorySnapshotIndex index = RepositorySnapshotIndexer.BuildIndex(
            request.TargetRepository);
        OccurrenceInfo occurrence = CompatibilityClassifier.ResolveOccurrence(
            index,
            targetIdentifier);
        return occurrence.Available && occurrence.MinimumOccurs > 0;
    }

    internal static bool AppendStep(
        MigrationRequest request,
        MigrationPlan plan,
        MigrationStep step)
    {
        if (plan.Steps.Count >= request.Limits.MaxCollectionCount)
        {
            FailPlan(
                plan,
                "migration.step_limit",
                "The migration plan exceeds the configured step collection limit.");
            return false;
        }

        step.Sequence = plan.Steps.Count;
        plan.Steps.Add(step);
        if (step.Lossy)
        {
            plan.HasDataLoss = true;
        }

        if (step.Action == MigrationActionKind.Custom ||
            (step.Action == MigrationActionKind.DefaultValue &&
                step.Required && step.MappingRule == null))
        {
            plan.RequiresCustomRules = true;
        }

        return true;
    }

    internal static MigrationPlan FailPlan(
        MigrationPlan plan,
        string code,
        string message)
    {
        plan.Success = false;
        plan.ErrorCode = code;
        plan.ErrorMessage = message;
        return plan;
    }
}
