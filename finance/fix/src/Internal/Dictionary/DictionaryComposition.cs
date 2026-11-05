using System;
using System.Collections.Generic;

namespace fix;

internal static class DictionaryComposition
{
    internal static DictionaryLoadResult ComposeGraphs(FixDictionary baseDictionary, FixDictionary overlayDictionary, FixDictionaryOptions options)
    {
        string baseOrigin = ResolveDictionaryOrigin(baseDictionary);
        string overlayOrigin = ResolveDictionaryOrigin(overlayDictionary);
        FixDictionary composed = CloneDictionaryGraph(baseDictionary, options, baseOrigin);
        MergeIdentity(composed.Identity, overlayDictionary.Identity);
        CopyOverlayDiagnostics(composed.Diagnostics, overlayDictionary.Diagnostics);
        MergeDatatypeDefinitions(composed, overlayDictionary, overlayOrigin, options);
        MergeCodeSetDefinitions(composed, overlayDictionary, overlayOrigin, options);
        MergeFieldDefinitions(composed, overlayDictionary, overlayOrigin, options);
        MergeComponentDefinitions(composed, overlayDictionary, overlayOrigin, options);
        MergeGroupDefinitions(composed, overlayDictionary, overlayOrigin, options);
        MergeMessageDefinitions(composed, overlayDictionary, overlayOrigin, options);
        MergeScenarioDefinitions(composed, overlayDictionary, overlayOrigin, options);
        if (overlayDictionary.Header.Count != 0)
        {
            MergeMemberDefinitions(composed.Header, overlayDictionary.Header, overlayOrigin, "StandardHeader", composed.Diagnostics, options);
        }
        if (overlayDictionary.Trailer.Count != 0)
        {
            MergeMemberDefinitions(composed.Trailer, overlayDictionary.Trailer, overlayOrigin, "StandardTrailer", composed.Diagnostics, options);
        }
        DictionaryNormalizer.NormalizeDictionary(composed, options);
        DictionaryLoadResult result = new DictionaryLoadResult();
        result.Dictionary = composed;
        result.Diagnostics = composed.Diagnostics;
        result.Success = !HasCompositionErrors(composed.Diagnostics);
        return result;
    }

    internal static FixDictionary CloneDictionaryGraph(FixDictionary source, FixDictionaryOptions options, string origin)
    {
        FixDictionary clone = new FixDictionary();
        clone.Identity = CloneIdentity(source.Identity);
        if (clone.Identity.Source.Length == 0)
        {
            clone.Identity.Source = origin;
        }
        clone.Options = CloneOptions(options);
        for (int index = 0; index < source.Datatypes.Count; index++)
        {
            if (!source.Datatypes[index].IsRemoved)
            {
                clone.Datatypes.Add(CloneDatatype(source.Datatypes[index], origin));
            }
        }
        for (int index = 0; index < source.CodeSets.Count; index++)
        {
            if (!source.CodeSets[index].IsRemoved)
            {
                clone.CodeSets.Add(CloneCodeSet(source.CodeSets[index], origin));
            }
        }
        for (int index = 0; index < source.Fields.Count; index++)
        {
            if (!source.Fields[index].IsRemoved)
            {
                clone.Fields.Add(CloneField(source.Fields[index], origin));
            }
        }
        CloneMemberList(source.Header, clone.Header, origin);
        CloneMemberList(source.Trailer, clone.Trailer, origin);
        for (int index = 0; index < source.Components.Count; index++)
        {
            if (!source.Components[index].IsRemoved)
            {
                clone.Components.Add(CloneComponent(source.Components[index], origin));
            }
        }
        for (int index = 0; index < source.Groups.Count; index++)
        {
            if (!source.Groups[index].IsRemoved)
            {
                clone.Groups.Add(CloneGroup(source.Groups[index], origin));
            }
        }
        for (int index = 0; index < source.Messages.Count; index++)
        {
            if (!source.Messages[index].IsRemoved)
            {
                clone.Messages.Add(CloneMessage(source.Messages[index], origin));
            }
        }
        for (int index = 0; index < source.Scenarios.Count; index++)
        {
            if (!source.Scenarios[index].IsRemoved)
            {
                clone.Scenarios.Add(CloneScenario(source.Scenarios[index], origin));
            }
        }
        CopyOverlayDiagnostics(clone.Diagnostics, source.Diagnostics);
        return clone;
    }

    internal static void MergeIdentity(FixDictionaryIdentity destination, FixDictionaryIdentity overlay)
    {
        if (overlay.Name.Length != 0) destination.Name = overlay.Name;
        if (overlay.BeginString.Length != 0) destination.BeginString = overlay.BeginString;
        if (overlay.ApplicationVersion.Length != 0) destination.ApplicationVersion = overlay.ApplicationVersion;
        if (overlay.HasExtensionPack)
        {
            destination.HasExtensionPack = true;
            destination.ExtensionPack = overlay.ExtensionPack;
        }
        if (overlay.CustomVersion.Length != 0) destination.CustomVersion = overlay.CustomVersion;
        if (overlay.Scenario.Length != 0) destination.Scenario = overlay.Scenario;
        if (overlay.Source.Length != 0) destination.Source = overlay.Source;
    }

    internal static void MergeDatatypeDefinitions(FixDictionary destination, FixDictionary overlay, string origin, FixDictionaryOptions options)
    {
        for (int overlayIndex = 0; overlayIndex < overlay.Datatypes.Count; overlayIndex++)
        {
            FixDatatypeDefinition definition = overlay.Datatypes[overlayIndex];
            int existingIndex = FindDatatypeIndex(destination.Datatypes, definition.Name, options.NormalizeNames);
            if (definition.IsRemoved)
            {
                if (existingIndex >= 0)
                {
                    destination.Datatypes.RemoveAt(existingIndex);
                }
                else
                {
                    AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Warning, "overlay-remove-missing-datatype", "The overlay requested removal of a datatype that is not present.", definition.Name, false, 0, ResolveItemOrigin(definition.Source, origin));
                }
            }
            else if (definition.Name.Length == 0)
            {
                AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Error, "invalid-overlay-datatype", "An overlay datatype must have a name.", "", false, 0, origin);
            }
            else if (existingIndex >= 0)
            {
                destination.Datatypes[existingIndex] = CloneDatatype(definition, origin);
            }
            else
            {
                destination.Datatypes.Add(CloneDatatype(definition, origin));
            }
        }
    }

    internal static void MergeCodeSetDefinitions(FixDictionary destination, FixDictionary overlay, string origin, FixDictionaryOptions options)
    {
        for (int overlayIndex = 0; overlayIndex < overlay.CodeSets.Count; overlayIndex++)
        {
            FixCodeSetDefinition definition = overlay.CodeSets[overlayIndex];
            int existingIndex = FindCodeSetIndex(destination.CodeSets, definition.Name, options.NormalizeNames);
            if (definition.IsRemoved)
            {
                if (existingIndex >= 0)
                {
                    destination.CodeSets.RemoveAt(existingIndex);
                }
                else
                {
                    AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Warning, "overlay-remove-missing-code-set", "The overlay requested removal of a code set that is not present.", definition.Name, false, 0, ResolveItemOrigin(definition.Source, origin));
                }
                continue;
            }
            if (definition.Name.Length == 0)
            {
                AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Error, "invalid-overlay-code-set", "An overlay code set must have a name.", "", false, 0, origin);
                continue;
            }
            int idCollision = FindCodeSetIdCollision(destination.CodeSets, definition.Id, existingIndex);
            if (idCollision >= 0)
            {
                AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Error, "code-set-id-collision", "The overlay code-set identifier is already used by another definition.", definition.Name, false, 0, ResolveItemOrigin(definition.Source, origin));
                continue;
            }
            if (existingIndex >= 0)
            {
                FixCodeSetDefinition replacement = CloneCodeSetMetadata(definition, origin);
                CloneCodeList(destination.CodeSets[existingIndex].Codes, replacement.Codes, destination.CodeSets[existingIndex].Source);
                MergeCodeDefinitions(replacement.Codes, definition.Codes, origin, definition.Name, destination.Diagnostics, options);
                destination.CodeSets[existingIndex] = replacement;
            }
            else
            {
                destination.CodeSets.Add(CloneCodeSet(definition, origin));
            }
        }
    }

    internal static void MergeFieldDefinitions(FixDictionary destination, FixDictionary overlay, string origin, FixDictionaryOptions options)
    {
        for (int overlayIndex = 0; overlayIndex < overlay.Fields.Count; overlayIndex++)
        {
            FixFieldDefinition definition = overlay.Fields[overlayIndex];
            int existingIndex = FindFieldIndex(destination.Fields, definition.Tag);
            if (definition.IsRemoved)
            {
                if (existingIndex >= 0)
                {
                    destination.Fields.RemoveAt(existingIndex);
                }
                else
                {
                    AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Warning, "overlay-remove-missing-field", "The overlay requested removal of a field that is not present.", definition.Name, true, definition.Tag, ResolveItemOrigin(definition.Source, origin));
                }
                continue;
            }
            if (definition.Tag <= 0 || definition.Name.Length == 0)
            {
                AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Error, "invalid-overlay-field", "An overlay field must have a positive tag and name.", definition.Name, true, definition.Tag, ResolveItemOrigin(definition.Source, origin));
                continue;
            }
            if (existingIndex < 0 && !overlay.Identity.HasExtensionPack && options.UserDefinedTagMinimum > 0 && definition.Tag < options.UserDefinedTagMinimum)
            {
                AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Error, "invalid-venue-extension-tag", "A venue field addition must use a user-defined tag or identify an official extension pack.", definition.Name, true, definition.Tag, ResolveItemOrigin(definition.Source, origin));
                continue;
            }
            int nameCollision = FindFieldNameCollision(destination.Fields, definition.Name, existingIndex, options.NormalizeNames);
            if (nameCollision >= 0)
            {
                AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Error, "field-name-collision", "The overlay field name is already used by another tag.", definition.Name, true, definition.Tag, ResolveItemOrigin(definition.Source, origin));
                continue;
            }
            if (existingIndex >= 0)
            {
                destination.Fields[existingIndex] = CloneField(definition, origin);
            }
            else
            {
                destination.Fields.Add(CloneField(definition, origin));
            }
        }
    }

    internal static void MergeComponentDefinitions(FixDictionary destination, FixDictionary overlay, string origin, FixDictionaryOptions options)
    {
        for (int overlayIndex = 0; overlayIndex < overlay.Components.Count; overlayIndex++)
        {
            FixComponentDefinition definition = overlay.Components[overlayIndex];
            int existingIndex = FindComponentIndex(destination.Components, definition.Id);
            if (definition.IsRemoved)
            {
                if (existingIndex >= 0)
                {
                    destination.Components.RemoveAt(existingIndex);
                }
                else
                {
                    AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Warning, "overlay-remove-missing-component", "The overlay requested removal of a component that is not present.", definition.Name, false, 0, ResolveItemOrigin(definition.Source, origin));
                }
                continue;
            }
            if (definition.Id <= 0 || definition.Name.Length == 0)
            {
                AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Error, "invalid-overlay-component", "An overlay component must have a positive identifier and name.", definition.Name, false, 0, ResolveItemOrigin(definition.Source, origin));
                continue;
            }
            int nameCollision = FindComponentNameCollision(destination.Components, definition.Name, existingIndex, options.NormalizeNames);
            if (nameCollision >= 0)
            {
                AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Error, "component-name-collision", "The overlay component name is already used by another identifier.", definition.Name, false, 0, ResolveItemOrigin(definition.Source, origin));
                continue;
            }
            if (existingIndex >= 0)
            {
                FixComponentDefinition replacement = CloneComponentMetadata(definition, origin);
                CloneMemberList(destination.Components[existingIndex].Members, replacement.Members, destination.Components[existingIndex].Source);
                MergeMemberDefinitions(replacement.Members, definition.Members, origin, definition.Name, destination.Diagnostics, options);
                destination.Components[existingIndex] = replacement;
            }
            else
            {
                destination.Components.Add(CloneComponent(definition, origin));
            }
        }
    }

    internal static void MergeGroupDefinitions(FixDictionary destination, FixDictionary overlay, string origin, FixDictionaryOptions options)
    {
        for (int overlayIndex = 0; overlayIndex < overlay.Groups.Count; overlayIndex++)
        {
            FixGroupDefinition definition = overlay.Groups[overlayIndex];
            int existingIndex = FindGroupIndex(destination.Groups, definition.Id);
            if (definition.IsRemoved)
            {
                if (existingIndex >= 0)
                {
                    destination.Groups.RemoveAt(existingIndex);
                }
                else
                {
                    AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Warning, "overlay-remove-missing-group", "The overlay requested removal of a group that is not present.", definition.Name, true, definition.CountTag, ResolveItemOrigin(definition.Source, origin));
                }
                continue;
            }
            if (definition.Id <= 0 || definition.Name.Length == 0 || definition.CountTag <= 0)
            {
                AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Error, "invalid-overlay-group", "An overlay group must have an identifier, name, and count tag.", definition.Name, true, definition.CountTag, ResolveItemOrigin(definition.Source, origin));
                continue;
            }
            int nameCollision = FindGroupNameCollision(destination.Groups, definition.Name, existingIndex, options.NormalizeNames);
            if (nameCollision >= 0)
            {
                AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Error, "group-name-collision", "The overlay group name is already used by another identifier.", definition.Name, true, definition.CountTag, ResolveItemOrigin(definition.Source, origin));
                continue;
            }
            if (existingIndex >= 0)
            {
                FixGroupDefinition replacement = CloneGroupMetadata(definition, origin);
                CloneMemberList(destination.Groups[existingIndex].Members, replacement.Members, destination.Groups[existingIndex].Source);
                MergeMemberDefinitions(replacement.Members, definition.Members, origin, definition.Name, destination.Diagnostics, options);
                destination.Groups[existingIndex] = replacement;
            }
            else
            {
                destination.Groups.Add(CloneGroup(definition, origin));
            }
        }
    }

    internal static void MergeMessageDefinitions(FixDictionary destination, FixDictionary overlay, string origin, FixDictionaryOptions options)
    {
        for (int overlayIndex = 0; overlayIndex < overlay.Messages.Count; overlayIndex++)
        {
            FixMessageDefinition definition = overlay.Messages[overlayIndex];
            int existingIndex = FindMessageIndex(destination.Messages, definition.MsgType, definition.Scenario, options.NormalizeNames);
            if (definition.IsRemoved)
            {
                if (existingIndex >= 0)
                {
                    destination.Messages.RemoveAt(existingIndex);
                }
                else
                {
                    AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Warning, "overlay-remove-missing-message", "The overlay requested removal of a message scenario that is not present.", definition.Name, false, 0, ResolveItemOrigin(definition.Source, origin));
                }
                continue;
            }
            if (definition.Name.Length == 0 || definition.MsgType.Length == 0)
            {
                AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Error, "invalid-overlay-message", "An overlay message must have a name and MsgType.", definition.Name, false, 0, ResolveItemOrigin(definition.Source, origin));
                continue;
            }
            int nameCollision = FindMessageNameCollision(destination.Messages, definition.Name, definition.Scenario, existingIndex, options.NormalizeNames);
            if (nameCollision >= 0)
            {
                AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Error, "message-name-collision", "The overlay message name and scenario are already used by another MsgType.", definition.Name, false, 0, ResolveItemOrigin(definition.Source, origin));
                continue;
            }
            if (existingIndex >= 0)
            {
                FixMessageDefinition replacement = CloneMessageMetadata(definition, origin);
                replacement.Scenario = destination.Messages[existingIndex].Scenario;
                CloneMemberList(destination.Messages[existingIndex].Members, replacement.Members, destination.Messages[existingIndex].Source);
                MergeMemberDefinitions(replacement.Members, definition.Members, origin, definition.Name, destination.Diagnostics, options);
                destination.Messages[existingIndex] = replacement;
            }
            else
            {
                destination.Messages.Add(CloneMessage(definition, origin));
            }
        }
    }

    internal static void MergeScenarioDefinitions(FixDictionary destination, FixDictionary overlay, string origin, FixDictionaryOptions options)
    {
        for (int overlayIndex = 0; overlayIndex < overlay.Scenarios.Count; overlayIndex++)
        {
            FixScenarioDefinition definition = overlay.Scenarios[overlayIndex];
            int existingIndex = FindScenarioIndex(destination.Scenarios, definition.Name, options.NormalizeNames);
            if (definition.IsRemoved)
            {
                if (existingIndex >= 0)
                {
                    destination.Scenarios.RemoveAt(existingIndex);
                }
                else
                {
                    AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Warning, "overlay-remove-missing-scenario", "The overlay requested removal of a scenario that is not present.", definition.Name, false, 0, ResolveItemOrigin(definition.Source, origin));
                }
            }
            else if (definition.Name.Length == 0)
            {
                AddCompositionDiagnostic(destination.Diagnostics, ValidationSeverity.Error, "invalid-overlay-scenario", "An overlay scenario must have a name.", "", false, 0, origin);
            }
            else if (existingIndex >= 0)
            {
                FixScenarioDefinition replacement = CloneScenarioMetadata(definition, origin);
                CloneMemberList(destination.Scenarios[existingIndex].Members, replacement.Members, destination.Scenarios[existingIndex].Source);
                MergeMemberDefinitions(replacement.Members, definition.Members, origin, definition.Name, destination.Diagnostics, options);
                if (definition.Rules.Count == 0)
                {
                    CloneRuleList(destination.Scenarios[existingIndex].Rules, replacement.Rules);
                }
                else
                {
                    CloneRuleList(definition.Rules, replacement.Rules);
                }
                destination.Scenarios[existingIndex] = replacement;
            }
            else
            {
                destination.Scenarios.Add(CloneScenario(definition, origin));
            }
        }
    }

    internal static void MergeCodeDefinitions(List<FixCodeDefinition> destination, List<FixCodeDefinition> overlay, string origin, string owner, List<DictionaryDiagnostic> diagnostics, FixDictionaryOptions options)
    {
        for (int overlayIndex = 0; overlayIndex < overlay.Count; overlayIndex++)
        {
            FixCodeDefinition code = overlay[overlayIndex];
            int existingIndex = FindCodeIndex(destination, code.Value);
            if (code.IsRemoved)
            {
                if (existingIndex >= 0)
                {
                    destination.RemoveAt(existingIndex);
                }
                else
                {
                    AddCompositionDiagnostic(diagnostics, ValidationSeverity.Warning, "overlay-remove-missing-code", "The overlay requested removal of a code value that is not present.", owner + ":" + code.Value, false, 0, ResolveItemOrigin(code.Source, origin));
                }
            }
            else if (code.Value.Length == 0)
            {
                AddCompositionDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-overlay-code", "An overlay code must have a lexical value.", owner, false, 0, ResolveItemOrigin(code.Source, origin));
            }
            else if (FindCodeNameCollision(destination, code.Name, existingIndex, options.NormalizeNames) >= 0)
            {
                AddCompositionDiagnostic(diagnostics, ValidationSeverity.Error, "code-name-collision", "The overlay code name is already used by another value.", owner + ":" + code.Name, false, 0, ResolveItemOrigin(code.Source, origin));
            }
            else if (existingIndex >= 0)
            {
                destination[existingIndex] = CloneCode(code, origin);
            }
            else
            {
                destination.Add(CloneCode(code, origin));
            }
        }
    }

    internal static void MergeMemberDefinitions(List<FixMemberDefinition> destination, List<FixMemberDefinition> overlay, string origin, string owner, List<DictionaryDiagnostic> diagnostics, FixDictionaryOptions options)
    {
        for (int overlayIndex = 0; overlayIndex < overlay.Count; overlayIndex++)
        {
            FixMemberDefinition member = overlay[overlayIndex];
            int existingIndex = FindMemberIndex(destination, member, options.NormalizeNames);
            if (member.IsRemoved)
            {
                if (existingIndex >= 0)
                {
                    destination.RemoveAt(existingIndex);
                }
                else
                {
                    AddCompositionDiagnostic(diagnostics, ValidationSeverity.Warning, "overlay-remove-missing-member", "The overlay requested removal of a member that is not present.", owner + ":" + member.Name, member.Kind == FixMemberKind.Field, member.ReferenceId, ResolveItemOrigin(member.Source, origin));
                }
            }
            else if (member.ReferenceId <= 0)
            {
                AddCompositionDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-overlay-member", "An overlay member must have a positive reference identifier.", owner + ":" + member.Name, member.Kind == FixMemberKind.Field, member.ReferenceId, ResolveItemOrigin(member.Source, origin));
            }
            else if (existingIndex >= 0)
            {
                destination[existingIndex] = CloneMember(member, origin);
            }
            else
            {
                destination.Add(CloneMember(member, origin));
            }
        }
    }

    internal static int FindDatatypeIndex(List<FixDatatypeDefinition> definitions, string name, bool normalizeNames)
    {
        for (int index = 0; index < definitions.Count; index++)
        {
            if (NamesEqual(definitions[index].Name, name, normalizeNames))
            {
                return index;
            }
        }
        return -1;
    }

    internal static int FindCodeSetIndex(List<FixCodeSetDefinition> definitions, string name, bool normalizeNames)
    {
        for (int index = 0; index < definitions.Count; index++)
        {
            if (NamesEqual(definitions[index].Name, name, normalizeNames))
            {
                return index;
            }
        }
        return -1;
    }

    internal static int FindCodeSetIdCollision(List<FixCodeSetDefinition> definitions, int id, int excludedIndex)
    {
        if (id == 0)
        {
            return -1;
        }
        for (int index = 0; index < definitions.Count; index++)
        {
            if (index != excludedIndex && definitions[index].Id == id)
            {
                return index;
            }
        }
        return -1;
    }

    internal static int FindFieldIndex(List<FixFieldDefinition> definitions, int tag)
    {
        for (int index = 0; index < definitions.Count; index++)
        {
            if (definitions[index].Tag == tag)
            {
                return index;
            }
        }
        return -1;
    }

    internal static int FindFieldNameCollision(List<FixFieldDefinition> definitions, string name, int excludedIndex, bool normalizeNames)
    {
        for (int index = 0; index < definitions.Count; index++)
        {
            if (index != excludedIndex && (NamesEqual(definitions[index].Name, name, normalizeNames) || NamesEqual(definitions[index].Abbreviation, name, normalizeNames)))
            {
                return index;
            }
        }
        return -1;
    }

    internal static int FindComponentIndex(List<FixComponentDefinition> definitions, int id)
    {
        for (int index = 0; index < definitions.Count; index++)
        {
            if (definitions[index].Id == id)
            {
                return index;
            }
        }
        return -1;
    }

    internal static int FindComponentNameCollision(List<FixComponentDefinition> definitions, string name, int excludedIndex, bool normalizeNames)
    {
        for (int index = 0; index < definitions.Count; index++)
        {
            if (index != excludedIndex && (NamesEqual(definitions[index].Name, name, normalizeNames) || NamesEqual(definitions[index].Abbreviation, name, normalizeNames)))
            {
                return index;
            }
        }
        return -1;
    }

    internal static int FindGroupIndex(List<FixGroupDefinition> definitions, int id)
    {
        for (int index = 0; index < definitions.Count; index++)
        {
            if (definitions[index].Id == id)
            {
                return index;
            }
        }
        return -1;
    }

    internal static int FindGroupNameCollision(List<FixGroupDefinition> definitions, string name, int excludedIndex, bool normalizeNames)
    {
        for (int index = 0; index < definitions.Count; index++)
        {
            if (index != excludedIndex && NamesEqual(definitions[index].Name, name, normalizeNames))
            {
                return index;
            }
        }
        return -1;
    }

    internal static int FindMessageIndex(List<FixMessageDefinition> definitions, string msgType, string scenario, bool normalizeNames)
    {
        for (int index = 0; index < definitions.Count; index++)
        {
            if (definitions[index].MsgType == msgType && ScenarioNamesEqual(definitions[index].Scenario, scenario, normalizeNames))
            {
                return index;
            }
        }
        return -1;
    }

    internal static int FindMessageNameCollision(List<FixMessageDefinition> definitions, string name, string scenario, int excludedIndex, bool normalizeNames)
    {
        for (int index = 0; index < definitions.Count; index++)
        {
            if (index != excludedIndex && NamesEqual(definitions[index].Name, name, normalizeNames) && ScenarioNamesEqual(definitions[index].Scenario, scenario, normalizeNames))
            {
                return index;
            }
        }
        return -1;
    }

    internal static bool ScenarioNamesEqual(string left, string right, bool normalizeNames)
    {
        string normalizedLeft = left.Length == 0 ? "base" : left;
        string normalizedRight = right.Length == 0 ? "base" : right;
        return NamesEqual(normalizedLeft, normalizedRight, normalizeNames);
    }

    internal static int FindScenarioIndex(List<FixScenarioDefinition> definitions, string name, bool normalizeNames)
    {
        for (int index = 0; index < definitions.Count; index++)
        {
            if (NamesEqual(definitions[index].Name, name, normalizeNames))
            {
                return index;
            }
        }
        return -1;
    }

    internal static int FindCodeIndex(List<FixCodeDefinition> definitions, string value)
    {
        for (int index = 0; index < definitions.Count; index++)
        {
            if (definitions[index].Value == value)
            {
                return index;
            }
        }
        return -1;
    }

    internal static int FindCodeNameCollision(List<FixCodeDefinition> definitions, string name, int excludedIndex, bool normalizeNames)
    {
        if (name.Length == 0)
        {
            return -1;
        }
        for (int index = 0; index < definitions.Count; index++)
        {
            if (index != excludedIndex && NamesEqual(definitions[index].Name, name, normalizeNames))
            {
                return index;
            }
        }
        return -1;
    }

    internal static int FindMemberIndex(List<FixMemberDefinition> definitions, FixMemberDefinition member, bool normalizeNames)
    {
        for (int index = 0; index < definitions.Count; index++)
        {
            FixMemberDefinition candidate = definitions[index];
            if (candidate.Kind == member.Kind && candidate.ReferenceId == member.ReferenceId && NamesEqual(candidate.Scenario, member.Scenario, normalizeNames))
            {
                return index;
            }
        }
        return -1;
    }

    internal static bool NamesEqual(string first, string second, bool normalizeNames)
    {
        if (normalizeNames)
        {
            return first.Trim().ToUpperInvariant() == second.Trim().ToUpperInvariant();
        }
        return first == second;
    }

    internal static FixDictionaryIdentity CloneIdentity(FixDictionaryIdentity source)
    {
        FixDictionaryIdentity clone = new FixDictionaryIdentity();
        clone.Name = source.Name;
        clone.BeginString = source.BeginString;
        clone.ApplicationVersion = source.ApplicationVersion;
        clone.HasExtensionPack = source.HasExtensionPack;
        clone.ExtensionPack = source.ExtensionPack;
        clone.CustomVersion = source.CustomVersion;
        clone.Scenario = source.Scenario;
        clone.Source = source.Source;
        return clone;
    }

    internal static FixDictionaryOptions CloneOptions(FixDictionaryOptions source)
    {
        FixDictionaryOptions clone = new FixDictionaryOptions();
        clone.MaxDefinitions = source.MaxDefinitions;
        clone.UserDefinedTagMinimum = source.UserDefinedTagMinimum;
        clone.AllowDuplicateDefinitions = source.AllowDuplicateDefinitions;
        clone.AllowUnknownDatatypes = source.AllowUnknownDatatypes;
        clone.RequireResolvedReferences = source.RequireResolvedReferences;
        clone.NormalizeNames = source.NormalizeNames;
        clone.PreserveSourceOrder = source.PreserveSourceOrder;
        return clone;
    }

    internal static FixDatatypeDefinition CloneDatatype(FixDatatypeDefinition source, string origin)
    {
        FixDatatypeDefinition clone = new FixDatatypeDefinition();
        clone.Name = source.Name;
        clone.FieldType = source.FieldType;
        clone.BaseTypeName = source.BaseTypeName;
        clone.Description = source.Description;
        clone.Source = ResolveItemOrigin(source.Source, origin);
        return clone;
    }

    internal static FixCodeSetDefinition CloneCodeSet(FixCodeSetDefinition source, string origin)
    {
        FixCodeSetDefinition clone = CloneCodeSetMetadata(source, origin);
        CloneCodeList(source.Codes, clone.Codes, clone.Source);
        return clone;
    }

    internal static FixCodeSetDefinition CloneCodeSetMetadata(FixCodeSetDefinition source, string origin)
    {
        FixCodeSetDefinition clone = new FixCodeSetDefinition();
        clone.Id = source.Id;
        clone.Name = source.Name;
        clone.FieldType = source.FieldType;
        clone.Description = source.Description;
        clone.Source = ResolveItemOrigin(source.Source, origin);
        return clone;
    }

    internal static FixCodeDefinition CloneCode(FixCodeDefinition source, string origin)
    {
        FixCodeDefinition clone = new FixCodeDefinition();
        clone.Name = source.Name;
        clone.Value = source.Value;
        clone.Id = source.Id;
        clone.SortOrder = source.SortOrder;
        clone.Description = source.Description;
        clone.Source = ResolveItemOrigin(source.Source, origin);
        return clone;
    }

    internal static void CloneCodeList(List<FixCodeDefinition> source, List<FixCodeDefinition> destination, string origin)
    {
        for (int index = 0; index < source.Count; index++)
        {
            if (!source[index].IsRemoved)
            {
                destination.Add(CloneCode(source[index], origin));
            }
        }
    }

    internal static FixFieldDefinition CloneField(FixFieldDefinition source, string origin)
    {
        FixFieldDefinition clone = new FixFieldDefinition();
        clone.Tag = source.Tag;
        clone.Name = source.Name;
        clone.Abbreviation = source.Abbreviation;
        clone.DatatypeName = source.DatatypeName;
        clone.FieldType = source.FieldType;
        clone.CodeSetName = source.CodeSetName;
        clone.HasDiscriminatorTag = source.HasDiscriminatorTag;
        clone.DiscriminatorTag = source.DiscriminatorTag;
        clone.HasLengthTag = source.HasLengthTag;
        clone.LengthTag = source.LengthTag;
        clone.HasDataTag = source.HasDataTag;
        clone.DataTag = source.DataTag;
        clone.Description = source.Description;
        clone.Source = ResolveItemOrigin(source.Source, origin);
        return clone;
    }

    internal static FixMemberDefinition CloneMember(FixMemberDefinition source, string origin)
    {
        FixMemberDefinition clone = new FixMemberDefinition();
        clone.Name = source.Name;
        clone.Kind = source.Kind;
        clone.ReferenceId = source.ReferenceId;
        clone.Position = source.Position;
        clone.Presence = source.Presence;
        clone.Scenario = source.Scenario;
        clone.PresenceRule = source.PresenceRule;
        clone.RulePresence = source.RulePresence;
        clone.Source = ResolveItemOrigin(source.Source, origin);
        return clone;
    }

    internal static void CloneMemberList(List<FixMemberDefinition> source, List<FixMemberDefinition> destination, string origin)
    {
        for (int index = 0; index < source.Count; index++)
        {
            if (!source[index].IsRemoved)
            {
                destination.Add(CloneMember(source[index], origin));
            }
        }
    }

    internal static FixComponentDefinition CloneComponent(FixComponentDefinition source, string origin)
    {
        FixComponentDefinition clone = CloneComponentMetadata(source, origin);
        CloneMemberList(source.Members, clone.Members, clone.Source);
        return clone;
    }

    internal static FixComponentDefinition CloneComponentMetadata(FixComponentDefinition source, string origin)
    {
        FixComponentDefinition clone = new FixComponentDefinition();
        clone.Id = source.Id;
        clone.Name = source.Name;
        clone.Abbreviation = source.Abbreviation;
        clone.Description = source.Description;
        clone.Source = ResolveItemOrigin(source.Source, origin);
        return clone;
    }

    internal static FixGroupDefinition CloneGroup(FixGroupDefinition source, string origin)
    {
        FixGroupDefinition clone = CloneGroupMetadata(source, origin);
        CloneMemberList(source.Members, clone.Members, clone.Source);
        return clone;
    }

    internal static FixGroupDefinition CloneGroupMetadata(FixGroupDefinition source, string origin)
    {
        FixGroupDefinition clone = new FixGroupDefinition();
        clone.Id = source.Id;
        clone.Name = source.Name;
        clone.CountTag = source.CountTag;
        clone.DelimiterTag = source.DelimiterTag;
        clone.Description = source.Description;
        clone.Source = ResolveItemOrigin(source.Source, origin);
        return clone;
    }

    internal static FixMessageDefinition CloneMessage(FixMessageDefinition source, string origin)
    {
        FixMessageDefinition clone = CloneMessageMetadata(source, origin);
        CloneMemberList(source.Members, clone.Members, clone.Source);
        return clone;
    }

    internal static FixMessageDefinition CloneMessageMetadata(FixMessageDefinition source, string origin)
    {
        FixMessageDefinition clone = new FixMessageDefinition();
        clone.Id = source.Id;
        clone.Name = source.Name;
        clone.MsgType = source.MsgType;
        clone.Category = source.Category;
        clone.Scenario = source.Scenario;
        clone.Description = source.Description;
        clone.Source = ResolveItemOrigin(source.Source, origin);
        return clone;
    }

    internal static FixScenarioDefinition CloneScenario(FixScenarioDefinition source, string origin)
    {
        FixScenarioDefinition clone = CloneScenarioMetadata(source, origin);
        CloneMemberList(source.Members, clone.Members, clone.Source);
        CloneRuleList(source.Rules, clone.Rules);
        return clone;
    }

    internal static FixScenarioDefinition CloneScenarioMetadata(FixScenarioDefinition source, string origin)
    {
        FixScenarioDefinition clone = new FixScenarioDefinition();
        clone.Name = source.Name;
        clone.ParentName = source.ParentName;
        clone.Description = source.Description;
        clone.Source = ResolveItemOrigin(source.Source, origin);
        return clone;
    }

    internal static void CloneRuleList(List<string> source, List<string> destination)
    {
        for (int index = 0; index < source.Count; index++)
        {
            destination.Add(source[index]);
        }
    }

    internal static void CopyOverlayDiagnostics(List<DictionaryDiagnostic> destination, List<DictionaryDiagnostic> source)
    {
        for (int index = 0; index < source.Count; index++)
        {
            DictionaryDiagnostic original = source[index];
            DictionaryDiagnostic clone = new DictionaryDiagnostic();
            clone.Severity = original.Severity;
            clone.Code = original.Code;
            clone.Message = original.Message;
            clone.Source = original.Source;
            clone.Line = original.Line;
            clone.Column = original.Column;
            clone.DefinitionName = original.DefinitionName;
            clone.HasTag = original.HasTag;
            clone.Tag = original.Tag;
            destination.Add(clone);
        }
    }

    internal static string ResolveDictionaryOrigin(FixDictionary dictionary)
    {
        return dictionary.Identity.Source.Length == 0 ? dictionary.Identity.Name : dictionary.Identity.Source;
    }

    internal static string ResolveItemOrigin(string source, string fallback)
    {
        return source.Length == 0 ? fallback : source;
    }

    internal static void AddCompositionDiagnostic(List<DictionaryDiagnostic> diagnostics, ValidationSeverity severity, string code, string message, string definitionName, bool hasTag, int tag, string source)
    {
        if (diagnostics.Count >= FixDefaults.MaxDiagnostics)
        {
            return;
        }
        DictionaryDiagnostic diagnostic = new DictionaryDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.DefinitionName = definitionName;
        diagnostic.HasTag = hasTag;
        diagnostic.Tag = tag;
        diagnostic.Source = source;
        diagnostics.Add(diagnostic);
    }

    internal static bool HasCompositionErrors(List<DictionaryDiagnostic> diagnostics)
    {
        for (int index = 0; index < diagnostics.Count; index++)
        {
            if (diagnostics[index].Severity == ValidationSeverity.Error || diagnostics[index].Severity == ValidationSeverity.Fatal)
            {
                return true;
            }
        }
        return false;
    }
}
