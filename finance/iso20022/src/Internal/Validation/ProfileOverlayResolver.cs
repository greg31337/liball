using System.Collections.Generic;

namespace iso20022;

internal static class ProfileOverlayResolver
{
    internal static ProfileOverlayResolution ResolveOverlay(
        ValidationRequest request,
        MessageIdentifier? actualIdentifier,
        string actualNamespace)
    {
        ProfileOverlayResolution result = new ProfileOverlayResolution();
        ProfileDefinition? selected = request.Profile;
        if (selected == null)
        {
            return result;
        }
        List<ProfileDefinition> derivedFirst = new List<ProfileDefinition>();
        List<string> visited = new List<string>();
        ProfileDefinition? current = selected;
        while (current != null)
        {
            if (current.Identifier == null || current.Identifier.Length == 0 ||
                current.Version == null || current.Version.Length == 0)
            {
                SetFailure(result,
                    "validation.profile.identity_missing",
                    "Every selected or inherited profile requires a stable identifier and exact version.",
                    DiagnosticKind.Configuration,
                    true);
                return result;
            }
            if (derivedFirst.Count >= request.Policy.Limits.MaxCollectionCount)
            {
                SetFailure(result,
                    "validation.profile.inheritance_limit",
                    "The profile inheritance chain exceeds the collection limit.",
                    DiagnosticKind.ResourceLimit,
                    true);
                return result;
            }
            string identity = current.Identifier + "\n" + current.Version;
            if (TextListContains(visited, identity) ||
                TextListContainsProfileIdentifier(derivedFirst, current.Identifier))
            {
                SetFailure(result,
                    "validation.profile.inheritance_cycle",
                    "The selected profile inheritance chain contains a cycle.",
                    DiagnosticKind.Conflict,
                    true);
                return result;
            }
            visited.Add(identity);
            derivedFirst.Add(current);
            if (current.BaseProfileIdentifier == null ||
                current.BaseProfileIdentifier.Length == 0)
            {
                current = null;
            }
            else if (request.ProfileProvider == null)
            {
                SetFailure(result,
                    "validation.profile.provider_missing",
                    "Resolving an inherited profile requires a profile provider.",
                    DiagnosticKind.Configuration,
                    true);
                return result;
            }
            else
            {
                current = request.ProfileProvider.FindProfile(
                    current.BaseProfileIdentifier,
                    string.Empty);
                if (current == null)
                {
                    SetFailure(result,
                        "validation.profile.base_missing",
                        "The selected profile base could not be resolved.",
                        DiagnosticKind.Configuration,
                        true);
                    return result;
                }
            }
        }
        int index = derivedFirst.Count - 1;
        while (index >= 0)
        {
            result.Profiles.Add(derivedFirst[index]);
            index--;
        }
        index = 0;
        while (index < result.Profiles.Count)
        {
            ProfileDefinition profile = result.Profiles[index];
            if (!ProfileTargetsMessage(profile, actualIdentifier, actualNamespace))
            {
                SetFailure(result,
                    "validation.profile.target_mismatch",
                    "The selected profile is not compatible with the message identity, version, or namespace.",
                    DiagnosticKind.Applicability,
                    false);
                return result;
            }
            if (!ValidateProfileLists(request, profile, result))
            {
                return result;
            }
            if (!ApplyProfileConstraints(profile, result))
            {
                return result;
            }
            index++;
        }
        return result;
    }

    internal static bool ValidateProfileLists(
        ValidationRequest request,
        ProfileDefinition profile,
        ProfileOverlayResolution result)
    {
        if (profile.Constraints == null || profile.Rules == null ||
            profile.MessageIdentifiers == null || profile.NamespaceUris == null)
        {
            SetFailure(result,
                "validation.profile.list_missing",
                "Profile constraint, rule, identity, and namespace lists cannot be null.",
                DiagnosticKind.Configuration,
                true);
            return false;
        }
        if (profile.Constraints.Count > request.Policy.Limits.MaxCollectionCount ||
            profile.Rules.Count > request.Policy.Limits.MaxCollectionCount ||
            profile.MessageIdentifiers.Count > request.Policy.Limits.MaxCollectionCount ||
            profile.NamespaceUris.Count > request.Policy.Limits.MaxCollectionCount)
        {
            SetFailure(result,
                "validation.profile.list_limit",
                "A profile collection exceeds the configured collection limit.",
                DiagnosticKind.ResourceLimit,
                true);
            return false;
        }
        return true;
    }

    internal static bool ProfileTargetsMessage(
        ProfileDefinition profile,
        MessageIdentifier? actualIdentifier,
        string actualNamespace)
    {
        if (profile.MessageIdentifiers != null && profile.MessageIdentifiers.Count > 0)
        {
            if (actualIdentifier == null)
            {
                return false;
            }
            bool matched = false;
            string actualText = MessageValidator.GetIdentifierText(actualIdentifier);
            int index = 0;
            while (index < profile.MessageIdentifiers.Count)
            {
                MessageIdentifier? expected = profile.MessageIdentifiers[index];
                if (expected != null &&
                    MessageValidator.GetIdentifierText(expected) == actualText)
                {
                    matched = true;
                }
                index++;
            }
            if (!matched)
            {
                return false;
            }
        }
        if (profile.NamespaceUris != null && profile.NamespaceUris.Count > 0 &&
            !TextListContains(profile.NamespaceUris, actualNamespace))
        {
            return false;
        }
        if (!VersionSelectorIsEmpty(profile.BaseVersion) &&
            !MessageValidator.SemanticVersionSelectorApplies(
                profile.BaseVersion,
                actualIdentifier))
        {
            return false;
        }
        return true;
    }

    internal static bool VersionSelectorIsEmpty(VersionSelector? selector)
    {
        return selector == null ||
            (selector.Kind == VersionSelectionKind.Exact &&
            selector.BaselineIdentifier == null &&
            selector.MinimumVersion.Length == 0 &&
            selector.MaximumVersion.Length == 0);
    }

    internal static bool ApplyProfileConstraints(
        ProfileDefinition profile,
        ProfileOverlayResolution result)
    {
        int index = 0;
        while (index < profile.Constraints.Count)
        {
            ProfileConstraint? constraint = profile.Constraints[index];
            if (constraint == null || constraint.Identifier == null ||
                constraint.Identifier.Length == 0 || constraint.Path == null ||
                constraint.AllowedValues == null || constraint.ProhibitedValues == null)
            {
                SetConstraintConflict(result,
                    "Every profile constraint requires an identifier, path, and value lists.");
                return false;
            }
            if (!ConstraintIsInternallyValid(constraint))
            {
                SetConstraintConflict(result,
                    "A profile constraint contains incompatible path, cardinality, or value restrictions.");
                return false;
            }
            string targetIdentifier = constraint.BaseConstraintIdentifier;
            if (targetIdentifier.Length == 0)
            {
                targetIdentifier = constraint.Identifier;
            }
            ProfileConstraintSelection? existing = FindConstraint(
                result.Constraints,
                targetIdentifier);
            if (constraint.RemoveBaseConstraint)
            {
                if (existing == null)
                {
                    SetConstraintConflict(result,
                        "A profile cannot remove an inherited constraint that does not exist.");
                    return false;
                }
                result.Constraints.Remove(existing);
            }
            else if (constraint.BaseConstraintIdentifier.Length > 0)
            {
                if (existing == null)
                {
                    ProfileConstraintSelection selection = new ProfileConstraintSelection();
                    selection.Constraint = CloneConstraint(constraint);
                    selection.Profile = profile;
                    result.Constraints.Add(selection);
                }
                else if (!MergeConstraint(existing, constraint, profile))
                {
                    SetConstraintConflict(result,
                        "An inherited profile constraint override conflicts with its base restriction.");
                    return false;
                }
            }
            else if (existing != null)
            {
                SetConstraintConflict(result,
                    "Profile constraint identifiers must be unique unless an inherited base is named.");
                return false;
            }
            else
            {
                ProfileConstraintSelection selection = new ProfileConstraintSelection();
                selection.Constraint = CloneConstraint(constraint);
                selection.Profile = profile;
                result.Constraints.Add(selection);
            }
            index++;
        }
        return true;
    }

    internal static bool ConstraintIsInternallyValid(ProfileConstraint constraint)
    {
        if (constraint.RequirePath && constraint.ProhibitPath)
        {
            return false;
        }
        if (constraint.HasMinimumOccurrences && constraint.MinimumOccurrences < 0)
        {
            return false;
        }
        if (constraint.HasMaximumOccurrences && constraint.MaximumOccurrences < 0)
        {
            return false;
        }
        if (constraint.HasMinimumOccurrences && constraint.HasMaximumOccurrences &&
            constraint.MinimumOccurrences > constraint.MaximumOccurrences)
        {
            return false;
        }
        if (constraint.RequirePath && constraint.HasMaximumOccurrences &&
            constraint.MaximumOccurrences < 1)
        {
            return false;
        }
        if (constraint.ProhibitPath && constraint.HasMinimumOccurrences &&
            constraint.MinimumOccurrences > 0)
        {
            return false;
        }
        int index = 0;
        while (index < constraint.AllowedValues.Count)
        {
            if (TextListContains(
                constraint.ProhibitedValues,
                constraint.AllowedValues[index]))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static ProfileConstraintSelection? FindConstraint(
        List<ProfileConstraintSelection> values,
        string identifier)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index].Constraint.Identifier == identifier)
            {
                return values[index];
            }
            index++;
        }
        return null;
    }

    internal static bool MergeConstraint(
        ProfileConstraintSelection existing,
        ProfileConstraint overlay,
        ProfileDefinition profile)
    {
        ProfileConstraint value = existing.Constraint;
        if (!PathsEqual(value.Path, overlay.Path))
        {
            return false;
        }
        if ((value.RequirePath && overlay.ProhibitPath) ||
            (value.ProhibitPath && overlay.RequirePath))
        {
            return false;
        }
        if (value.HasMinimumOccurrences && overlay.HasMinimumOccurrences &&
            overlay.MinimumOccurrences < value.MinimumOccurrences)
        {
            return false;
        }
        if (value.RequirePath && overlay.HasMinimumOccurrences &&
            overlay.MinimumOccurrences < 1)
        {
            return false;
        }
        if (value.HasMaximumOccurrences && overlay.HasMaximumOccurrences &&
            overlay.MaximumOccurrences > value.MaximumOccurrences)
        {
            return false;
        }
        if (value.AllowedValues.Count > 0 && overlay.AllowedValues.Count > 0 &&
            !IsSubset(overlay.AllowedValues, value.AllowedValues))
        {
            return false;
        }
        value.Identifier = overlay.Identifier;
        value.Name = overlay.Name;
        value.Definition = overlay.Definition;
        value.BaseConstraintIdentifier = overlay.BaseConstraintIdentifier;
        value.RequirePath = value.RequirePath || overlay.RequirePath;
        value.ProhibitPath = value.ProhibitPath || overlay.ProhibitPath;
        if (overlay.HasMinimumOccurrences)
        {
            value.HasMinimumOccurrences = true;
            value.MinimumOccurrences = overlay.MinimumOccurrences;
        }
        if (overlay.HasMaximumOccurrences)
        {
            value.HasMaximumOccurrences = true;
            value.MaximumOccurrences = overlay.MaximumOccurrences;
        }
        if (overlay.AllowedValues.Count > 0)
        {
            value.AllowedValues.Clear();
            value.AllowedValues.AddRange(overlay.AllowedValues);
        }
        bool hasAllowedRestriction = value.AllowedValues.Count > 0;
        AddMissingValues(value.ProhibitedValues, overlay.ProhibitedValues);
        if (hasAllowedRestriction)
        {
            RemoveProhibitedAllowedValues(
                value.AllowedValues,
                value.ProhibitedValues);
            if (value.AllowedValues.Count == 0)
            {
                return false;
            }
        }
        value.Severity = overlay.Severity;
        if (!ConstraintIsInternallyValid(value))
        {
            return false;
        }
        existing.Profile = profile;
        return true;
    }

    internal static ProfileConstraint CloneConstraint(ProfileConstraint source)
    {
        ProfileConstraint value = new ProfileConstraint();
        value.Identifier = source.Identifier;
        value.Name = source.Name;
        value.Definition = source.Definition;
        value.Path = source.Path;
        value.BaseConstraintIdentifier = source.BaseConstraintIdentifier;
        value.RemoveBaseConstraint = source.RemoveBaseConstraint;
        value.RequirePath = source.RequirePath;
        value.ProhibitPath = source.ProhibitPath;
        value.HasMinimumOccurrences = source.HasMinimumOccurrences;
        value.MinimumOccurrences = source.MinimumOccurrences;
        value.HasMaximumOccurrences = source.HasMaximumOccurrences;
        value.MaximumOccurrences = source.MaximumOccurrences;
        value.AllowedValues.AddRange(source.AllowedValues);
        value.ProhibitedValues.AddRange(source.ProhibitedValues);
        value.Severity = source.Severity;
        return value;
    }

    internal static bool PathsEqual(XmlPathData left, XmlPathData right)
    {
        if (left.Value.Length > 0 || right.Value.Length > 0)
        {
            return left.Value == right.Value;
        }
        if (left.Segments.Count != right.Segments.Count)
        {
            return false;
        }
        int index = 0;
        while (index < left.Segments.Count)
        {
            if (left.Segments[index].LocalName != right.Segments[index].LocalName ||
                left.Segments[index].NamespaceUri != right.Segments[index].NamespaceUri)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool IsSubset(List<string> values, List<string> candidates)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (!TextListContains(candidates, values[index]))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static void AddMissingValues(
        List<string> target,
        List<string> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (!TextListContains(target, values[index]))
            {
                target.Add(values[index]);
            }
            index++;
        }
    }

    internal static void RemoveProhibitedAllowedValues(
        List<string> allowed,
        List<string> prohibited)
    {
        int index = allowed.Count - 1;
        while (index >= 0)
        {
            if (TextListContains(prohibited, allowed[index]))
            {
                allowed.RemoveAt(index);
            }
            index--;
        }
    }

    internal static bool TextListContains(List<string> values, string value)
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

    internal static bool TextListContainsProfileIdentifier(
        List<ProfileDefinition> values,
        string identifier)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index].Identifier == identifier)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static void SetConstraintConflict(
        ProfileOverlayResolution result,
        string message)
    {
        SetFailure(result,
            "validation.profile.constraint_conflict",
            message,
            DiagnosticKind.Conflict,
            true);
    }

    internal static void SetFailure(
        ProfileOverlayResolution result,
        string code,
        string message,
        DiagnosticKind kind,
        bool operationFailure)
    {
        result.Success = false;
        result.OperationFailure = operationFailure;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        result.Kind = kind;
    }
}
