using System.Collections.Generic;
using System.Globalization;

namespace iso20022;

internal static class ProfileRebaseComparer
{
    internal static RepositoryDiff CompareProfiles(RepositoryComparisonRequest request)
    {
        RepositoryDiff result = new RepositoryDiff();
        ProfileDefinition? baselineValue = request.BaselineProfile;
        ProfileDefinition? localValue = request.LocalProfile;
        ProfileDefinition? targetValue = request.TargetProfile;
        if (baselineValue == null || localValue == null || targetValue == null)
        {
            return FailProfileComparison(
                result,
                "comparison.profile_three_way_required",
                "Baseline, local, and target profiles are required for a three-way comparison.");
        }

        ProfileDefinition baseline = baselineValue;
        ProfileDefinition local = localValue;
        ProfileDefinition target = targetValue;
        result.SourceEdition = local.Version;
        result.TargetEdition = target.Version;
        if (!ValidateProfileComparisonRequest(request, baseline, local, target, result))
        {
            return result;
        }

        List<ProfileComparisonEntry> baselineEntries = CreateProfileEntries(baseline);
        List<ProfileComparisonEntry> localEntries = CreateProfileEntries(local);
        List<ProfileComparisonEntry> targetEntries = CreateProfileEntries(target);
        List<string> keys = CollectEntryKeys(baselineEntries, localEntries, targetEntries);
        int keyIndex = 0;
        while (keyIndex < keys.Count)
        {
            if (request.CancellationToken.IsCancellationRequested)
            {
                return FailProfileComparison(
                    result,
                    "comparison.cancelled",
                    "Profile rebase comparison was cancelled.");
            }

            ProfileComparisonEntry? baselineEntry = FindProfileEntry(baselineEntries, keys[keyIndex]);
            ProfileComparisonEntry? localEntry = FindProfileEntry(localEntries, keys[keyIndex]);
            ProfileComparisonEntry? targetEntry = FindProfileEntry(targetEntries, keys[keyIndex]);
            AddThreeWayDifference(
                request,
                result,
                baselineEntry,
                localEntry,
                targetEntry);
            keyIndex++;
        }

        result.Success = true;
        result.Equivalent = !HasMaterialProfileDifference(result.Differences);
        return result;
    }

    internal static bool ValidateProfileComparisonRequest(
        RepositoryComparisonRequest request,
        ProfileDefinition baseline,
        ProfileDefinition local,
        ProfileDefinition target,
        RepositoryDiff result)
    {
        if (request.Limits == null || request.Limits.MaxCollectionCount < 1 ||
            request.Limits.MaxDiagnosticCount < 1)
        {
            FailProfileComparison(
                result,
                "comparison.resource_limit_invalid",
                "Profile comparison resource limits must be positive.");
            return false;
        }

        if (request.CancellationToken.IsCancellationRequested)
        {
            FailProfileComparison(result, "comparison.cancelled", "Profile rebase comparison was cancelled.");
            return false;
        }

        if (baseline.Identifier.Length == 0 || local.Identifier.Length == 0 ||
            target.Identifier.Length == 0 || baseline.Version.Length == 0 ||
            local.Version.Length == 0 || target.Version.Length == 0)
        {
            FailProfileComparison(
                result,
                "comparison.profile_identity_missing",
                "Every compared profile requires a stable identifier and exact version.");
            return false;
        }

        if (baseline.Identifier != local.Identifier || baseline.Identifier != target.Identifier)
        {
            FailProfileComparison(
                result,
                "comparison.profile_identity_mismatch",
                "Three-way comparison requires the same stable profile identifier.");
            return false;
        }

        if (!ValidateProfileCollections(request, baseline, result) ||
            !ValidateProfileCollections(request, local, result) ||
            !ValidateProfileCollections(request, target, result))
        {
            return false;
        }

        return true;
    }

    internal static bool ValidateProfileCollections(
        RepositoryComparisonRequest request,
        ProfileDefinition profile,
        RepositoryDiff result)
    {
        if (profile.Constraints == null || profile.Rules == null ||
            profile.MessageIdentifiers == null || profile.NamespaceUris == null ||
            profile.SupplementaryDataPolicies == null)
        {
            FailProfileComparison(
                result,
                "comparison.profile_collection_missing",
                "Profile comparison collections cannot be null.");
            return false;
        }

        if (profile.Constraints.Count > request.Limits.MaxCollectionCount ||
            profile.Rules.Count > request.Limits.MaxCollectionCount ||
            profile.MessageIdentifiers.Count > request.Limits.MaxCollectionCount ||
            profile.NamespaceUris.Count > request.Limits.MaxCollectionCount ||
            profile.SupplementaryDataPolicies.Count > request.Limits.MaxCollectionCount)
        {
            FailProfileComparison(
                result,
                "comparison.profile_collection_limit",
                "A profile comparison collection exceeds the configured limit.");
            return false;
        }

        List<string> identifiers = new List<string>();
        int index = 0;
        while (index < profile.Constraints.Count)
        {
            ProfileConstraint constraint = profile.Constraints[index];
            if (constraint == null || constraint.Identifier.Length == 0 ||
                constraint.Path == null || constraint.AllowedValues == null ||
                constraint.ProhibitedValues == null)
            {
                FailProfileComparison(
                    result,
                    "comparison.profile_constraint_invalid",
                    "Every compared profile constraint requires an identifier, path, and value lists.");
                return false;
            }

            if (ContainsProfileText(identifiers, constraint.Identifier))
            {
                FailProfileComparison(
                    result,
                    "comparison.profile_constraint_duplicate",
                    "Profile constraint identifiers must be unique within each profile version.");
                return false;
            }

            identifiers.Add(constraint.Identifier);
            index++;
        }

        identifiers.Clear();
        index = 0;
        while (index < profile.Rules.Count)
        {
            ProfileRuleReference rule = profile.Rules[index];
            if (rule == null || rule.RuleIdentifier.Length == 0)
            {
                FailProfileComparison(
                    result,
                    "comparison.profile_rule_invalid",
                    "Every compared profile rule requires a stable rule identifier.");
                return false;
            }

            if (ContainsProfileText(identifiers, rule.RuleIdentifier))
            {
                FailProfileComparison(
                    result,
                    "comparison.profile_rule_duplicate",
                    "Profile rule identifiers must be unique within each profile version.");
                return false;
            }

            identifiers.Add(rule.RuleIdentifier);
            index++;
        }

        identifiers.Clear();
        index = 0;
        while (index < profile.SupplementaryDataPolicies.Count)
        {
            SupplementaryDataPolicy policy = profile.SupplementaryDataPolicies[index];
            if (policy == null || policy.NamespaceUri.Length == 0)
            {
                FailProfileComparison(
                    result,
                    "comparison.profile_supplementary_policy_invalid",
                    "Every compared supplementary policy requires a namespace URI.");
                return false;
            }

            if (ContainsProfileText(identifiers, policy.NamespaceUri))
            {
                FailProfileComparison(
                    result,
                    "comparison.profile_supplementary_policy_duplicate",
                    "Supplementary policy namespaces must be unique within each profile version.");
                return false;
            }

            identifiers.Add(policy.NamespaceUri);
            index++;
        }

        return true;
    }

    internal static List<ProfileComparisonEntry> CreateProfileEntries(ProfileDefinition profile)
    {
        List<ProfileComparisonEntry> entries = new List<ProfileComparisonEntry>();
        AddProfileMetadataEntry(entries, profile.Identifier, "Name", profile.Name);
        AddProfileMetadataEntry(entries, profile.Identifier, "Definition", profile.Definition);
        AddProfileMetadataEntry(
            entries,
            profile.Identifier,
            "BaseProfileIdentifier",
            profile.BaseProfileIdentifier);
        AddProfileMetadataEntry(
            entries,
            profile.Identifier,
            "BaseVersion",
            CreateVersionSelectorValue(profile.BaseVersion));
        AddProfileMetadataEntry(
            entries,
            profile.Identifier,
            "MessageIdentifiers",
            CreateMessageIdentifierListValue(profile.MessageIdentifiers));
        AddProfileMetadataEntry(
            entries,
            profile.Identifier,
            "NamespaceUris",
            CreateSortedTextValue(profile.NamespaceUris));

        int index = 0;
        while (index < profile.Constraints.Count)
        {
            ProfileConstraint constraint = profile.Constraints[index];
            ProfileComparisonEntry entry = new ProfileComparisonEntry();
            entry.Key = "constraint:" + constraint.Identifier;
            entry.Identifier = constraint.Identifier;
            entry.Name = constraint.Name;
            entry.PropertyName = "ProfileConstraint";
            entry.Value = CreateConstraintValue(constraint);
            entry.Path = constraint.Path;
            entry.EntityKind = RepositoryEntityKind.Constraint;
            entries.Add(entry);
            index++;
        }

        index = 0;
        while (index < profile.Rules.Count)
        {
            ProfileRuleReference rule = profile.Rules[index];
            ProfileComparisonEntry entry = new ProfileComparisonEntry();
            entry.Key = "rule:" + rule.RuleIdentifier;
            entry.Identifier = rule.RuleIdentifier;
            entry.Name = rule.RuleIdentifier;
            entry.PropertyName = "SemanticRule";
            entry.Value = CreateRuleReferenceValue(rule);
            entry.EntityKind = RepositoryEntityKind.Constraint;
            entries.Add(entry);
            index++;
        }

        index = 0;
        while (index < profile.SupplementaryDataPolicies.Count)
        {
            SupplementaryDataPolicy policy = profile.SupplementaryDataPolicies[index];
            ProfileComparisonEntry entry = new ProfileComparisonEntry();
            entry.Key = "supplementary:" + policy.NamespaceUri;
            entry.Identifier = policy.NamespaceUri;
            entry.Name = policy.NamespaceUri;
            entry.PropertyName = "SupplementaryDataPolicy";
            entry.Value = CreateSupplementaryPolicyValue(policy);
            entry.EntityKind = RepositoryEntityKind.Constraint;
            entries.Add(entry);
            index++;
        }

        return entries;
    }

    internal static void AddProfileMetadataEntry(
        List<ProfileComparisonEntry> entries,
        string identifier,
        string propertyName,
        string value)
    {
        ProfileComparisonEntry entry = new ProfileComparisonEntry();
        entry.Key = "profile:" + propertyName;
        entry.Identifier = identifier;
        entry.Name = identifier;
        entry.PropertyName = propertyName;
        entry.Value = value;
        entry.EntityKind = RepositoryEntityKind.Repository;
        entries.Add(entry);
    }

    internal static List<string> CollectEntryKeys(
        List<ProfileComparisonEntry> baseline,
        List<ProfileComparisonEntry> local,
        List<ProfileComparisonEntry> target)
    {
        List<string> keys = new List<string>();
        AddEntryKeys(baseline, keys);
        AddEntryKeys(local, keys);
        AddEntryKeys(target, keys);
        keys.Sort(System.StringComparer.Ordinal);
        return keys;
    }

    internal static void AddEntryKeys(
        List<ProfileComparisonEntry> entries,
        List<string> keys)
    {
        int index = 0;
        while (index < entries.Count)
        {
            if (!ContainsProfileText(keys, entries[index].Key))
            {
                keys.Add(entries[index].Key);
            }

            index++;
        }
    }

    internal static ProfileComparisonEntry? FindProfileEntry(
        List<ProfileComparisonEntry> entries,
        string key)
    {
        int index = 0;
        while (index < entries.Count)
        {
            if (entries[index].Key == key)
            {
                return entries[index];
            }

            index++;
        }

        return null;
    }

    internal static void AddThreeWayDifference(
        RepositoryComparisonRequest request,
        RepositoryDiff result,
        ProfileComparisonEntry? baseline,
        ProfileComparisonEntry? local,
        ProfileComparisonEntry? target)
    {
        bool localChanged = !ProfileEntriesEqual(baseline, local);
        bool targetChanged = !ProfileEntriesEqual(baseline, target);
        bool branchesEqual = ProfileEntriesEqual(local, target);
        if ((!localChanged && !targetChanged) || branchesEqual)
        {
            if (request.IncludeUnchanged)
            {
                AddProfileDifference(
                    request,
                    result,
                    DifferenceKind.Unchanged,
                    local,
                    target,
                    "The local and target profile values converge.");
            }

            return;
        }

        if (localChanged && targetChanged)
        {
            AddProfileDifference(
                request,
                result,
                DifferenceKind.Conflict,
                local,
                target,
                "The local and target profiles changed the same baseline value incompatibly.");
            result.HasConflicts = true;
            AddProfileConflictDiagnostic(request, result, local, target);
            return;
        }

        DifferenceKind kind = DifferenceKind.Modified;
        if (local == null && target != null)
        {
            kind = DifferenceKind.Added;
        }
        else if (local != null && target == null)
        {
            kind = DifferenceKind.Removed;
        }

        string rationale = localChanged ?
            "The local profile changed this baseline value and the target profile retained it." :
            "The target profile changed this baseline value and the local profile retained it.";
        AddProfileDifference(request, result, kind, local, target, rationale);
    }

    internal static bool ProfileEntriesEqual(
        ProfileComparisonEntry? left,
        ProfileComparisonEntry? right)
    {
        if (left == null || right == null)
        {
            return left == null && right == null;
        }

        return left.Value == right.Value;
    }

    internal static void AddProfileDifference(
        RepositoryComparisonRequest request,
        RepositoryDiff result,
        DifferenceKind kind,
        ProfileComparisonEntry? local,
        ProfileComparisonEntry? target,
        string rationale)
    {
        if (result.Differences.Count >= request.Limits.MaxCollectionCount)
        {
            result.Truncated = true;
            return;
        }

        ProfileComparisonEntry? representative = local == null ? target : local;
        if (representative == null)
        {
            return;
        }

        DifferenceItem difference = new DifferenceItem();
        difference.Kind = kind;
        difference.EntityKind = representative.EntityKind;
        difference.SourceIdentifier = local == null ? string.Empty : local.Identifier;
        difference.TargetIdentifier = target == null ? string.Empty : target.Identifier;
        difference.SourceName = local == null ? string.Empty : local.Name;
        difference.TargetName = target == null ? string.Empty : target.Name;
        difference.SourcePath = local == null ? new XmlPathData() : local.Path;
        difference.TargetPath = target == null ? new XmlPathData() : target.Path;
        difference.PropertyName = representative.PropertyName;
        difference.SourceValue = local == null ? string.Empty : local.Value;
        difference.TargetValue = target == null ? string.Empty : target.Value;
        difference.MatchConfidence = 1m;
        difference.ExactIdentifierMatch = local != null && target != null;
        difference.BreakingChange = kind == DifferenceKind.Conflict || kind == DifferenceKind.Removed;
        difference.DataLossRisk = kind == DifferenceKind.Conflict || kind == DifferenceKind.Removed;
        difference.Rationale = rationale;
        result.Differences.Add(difference);
    }

    internal static void AddProfileConflictDiagnostic(
        RepositoryComparisonRequest request,
        RepositoryDiff result,
        ProfileComparisonEntry? local,
        ProfileComparisonEntry? target)
    {
        if (result.Diagnostics.Count >= request.Limits.MaxDiagnosticCount)
        {
            return;
        }

        ProfileComparisonEntry? representative = local == null ? target : local;
        if (representative == null)
        {
            return;
        }

        ValidationDiagnostic diagnostic = new ValidationDiagnostic();
        diagnostic.Code = "comparison.profile_rebase_conflict";
        diagnostic.RuleIdentifier = representative.Identifier;
        diagnostic.Severity = ValidationSeverity.Error;
        diagnostic.Stage = ValidationStage.Profile;
        diagnostic.Kind = DiagnosticKind.Conflict;
        diagnostic.Message = "Local and target profile changes conflict relative to the common baseline.";
        diagnostic.Path = representative.Path;
        diagnostic.SourceIdentifier = representative.Identifier;
        result.Diagnostics.Add(diagnostic);
    }

    internal static bool HasMaterialProfileDifference(List<DifferenceItem> differences)
    {
        int index = 0;
        while (index < differences.Count)
        {
            if (differences[index].Kind != DifferenceKind.Unchanged)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static string CreateConstraintValue(ProfileConstraint constraint)
    {
        return constraint.Name + "\n" + constraint.Definition + "\n" +
            constraint.Path.Value + "\n" + constraint.BaseConstraintIdentifier + "\n" +
            BoolValue(constraint.RemoveBaseConstraint) + BoolValue(constraint.RequirePath) +
            BoolValue(constraint.ProhibitPath) + BoolValue(constraint.HasMinimumOccurrences) +
            constraint.MinimumOccurrences.ToString(CultureInfo.InvariantCulture) + "\n" +
            BoolValue(constraint.HasMaximumOccurrences) +
            constraint.MaximumOccurrences.ToString(CultureInfo.InvariantCulture) + "\n" +
            CreateSortedTextValue(constraint.AllowedValues) + "\n" +
            CreateSortedTextValue(constraint.ProhibitedValues) + "\n" +
            ((int)constraint.Severity).ToString(CultureInfo.InvariantCulture);
    }

    internal static string CreateRuleReferenceValue(ProfileRuleReference rule)
    {
        return BoolValue(rule.Enabled) + BoolValue(rule.RemoveInheritedRule) +
            BoolValue(rule.HasSeverityOverride) +
            ((int)rule.Severity).ToString(CultureInfo.InvariantCulture) + "\n" +
            BoolValue(rule.HasOrderOverride) +
            rule.Order.ToString(CultureInfo.InvariantCulture) + "\n" +
            CreateRuleDescriptorValue(rule.Descriptor);
    }

    internal static string CreateRuleDescriptorValue(SemanticRuleDescriptor? descriptor)
    {
        if (descriptor == null)
        {
            return string.Empty;
        }

        List<string> paths = new List<string>();
        int pathIndex = 0;
        while (pathIndex < descriptor.Paths.Count)
        {
            paths.Add(descriptor.Paths[pathIndex].Value);
            pathIndex++;
        }

        return descriptor.Identifier + "\n" + descriptor.Name + "\n" +
            descriptor.Definition + "\n" +
            ((int)descriptor.Severity).ToString(CultureInfo.InvariantCulture) + "\n" +
            descriptor.Order.ToString(CultureInfo.InvariantCulture) + "\n" +
            BoolValue(descriptor.Enabled) +
            CreateMessageIdentifierListValue(descriptor.MessageIdentifiers) + "\n" +
            CreateVersionSelectorValue(descriptor.VersionSelector) + "\n" +
            CreateSortedTextValue(descriptor.ProfileIdentifiers) + "\n" +
            CreateSortedTextValue(paths) + "\n" + descriptor.SourceIdentifier;
    }

    internal static string CreateSupplementaryPolicyValue(SupplementaryDataPolicy policy)
    {
        string schemaIdentifier = policy.Schema == null ? string.Empty : policy.Schema.SourceName;
        string schemaNamespace = policy.Schema == null ? string.Empty : policy.Schema.NamespaceUri;
        return schemaIdentifier + "\n" + schemaNamespace + "\n" +
            BoolValue(policy.CanIgnore) + BoolValue(policy.MustUnderstand) +
            BoolValue(policy.ValidateSchema);
    }

    internal static string CreateVersionSelectorValue(VersionSelector? selector)
    {
        if (selector == null)
        {
            return string.Empty;
        }

        string baseline = selector.BaselineIdentifier == null ? string.Empty :
            MessageIdentifierLexical.FormatMessageIdentifier(selector.BaselineIdentifier) ?? string.Empty;
        return ((int)selector.Kind).ToString(CultureInfo.InvariantCulture) + "\n" +
            baseline + "\n" + selector.MinimumVersion + "\n" + selector.MaximumVersion;
    }

    internal static string CreateMessageIdentifierListValue(List<MessageIdentifier> identifiers)
    {
        List<string> values = new List<string>();
        int index = 0;
        while (index < identifiers.Count)
        {
            MessageIdentifier identifier = identifiers[index];
            string? formatted = MessageIdentifierLexical.FormatMessageIdentifier(identifier);
            values.Add(formatted == null ? string.Empty : formatted);
            index++;
        }

        return CreateSortedTextValue(values);
    }

    internal static string CreateSortedTextValue(List<string> values)
    {
        List<string> ordered = new List<string>();
        int index = 0;
        while (index < values.Count)
        {
            ordered.Add(values[index]);
            index++;
        }

        ordered.Sort(System.StringComparer.Ordinal);
        string result = string.Empty;
        index = 0;
        while (index < ordered.Count)
        {
            result += ordered[index].Length.ToString(CultureInfo.InvariantCulture) +
                ":" + ordered[index] + ";";
            index++;
        }

        return result;
    }

    internal static string BoolValue(bool value)
    {
        return value ? "1\n" : "0\n";
    }

    internal static bool ContainsProfileText(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static RepositoryDiff FailProfileComparison(
        RepositoryDiff result,
        string code,
        string message)
    {
        result.Success = false;
        result.Equivalent = false;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }
}
