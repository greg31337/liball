using System;
using System.Collections.Generic;

namespace diameter;

internal enum DictionaryRegistrationStatus
{
    Success = 0,
    Duplicate = 1,
    Invalid = 2,
    Conflict = 3
}

internal class DictionaryRegistrationResult
{
    internal DictionaryRegistrationStatus Status;
    internal string Code = string.Empty;
    internal string Message = string.Empty;
    internal DiameterDictionarySnapshot Snapshot = new DiameterDictionarySnapshot();
}

internal class DictionaryBuilder
{
    internal object BuilderLock = new object();
    internal List<DiameterDictionaryModule> Modules = new List<DiameterDictionaryModule>();
    internal DiameterDictionarySnapshot CurrentSnapshot = new DiameterDictionarySnapshot();
    internal DictionaryIndexes CurrentIndexes;

    internal DictionaryBuilder()
    {
        CurrentIndexes = new DictionaryIndexes(CurrentSnapshot);
    }

    internal DictionaryRegistrationResult RegisterDictionaryModule(
        DiameterDictionaryModule? module)
    {
        lock (BuilderLock)
        {
            DictionaryRegistrationResult validation = ValidateModule(module);
            if (validation.Status != DictionaryRegistrationStatus.Success || module == null)
            {
                validation.Snapshot = CloneSnapshot(CurrentSnapshot);
                return validation;
            }

            DiameterDictionaryModule candidate = CloneModule(module);
            int existingIndex = FindModuleIndex(Modules, candidate.Name);
            if (existingIndex >= 0)
            {
                DictionaryRegistrationResult duplicate = new DictionaryRegistrationResult();
                if (DictionaryDefinitionComparer.AreModulesEqual(
                    Modules[existingIndex],
                    candidate))
                {
                    duplicate.Status = DictionaryRegistrationStatus.Duplicate;
                    duplicate.Code = "duplicate-module";
                    duplicate.Message = "An equivalent dictionary module is already registered.";
                }
                else
                {
                    duplicate.Status = DictionaryRegistrationStatus.Conflict;
                    duplicate.Code = "module-name-conflict";
                    duplicate.Message = "A different dictionary module uses the same canonical name.";
                }

                duplicate.Snapshot = CloneSnapshot(CurrentSnapshot);
                return duplicate;
            }

            if (CurrentSnapshot.Revision == long.MaxValue)
            {
                return CreateRegistrationFailure(
                    DictionaryRegistrationStatus.Conflict,
                    "dictionary-revision-limit",
                    "The dictionary revision cannot be incremented.");
            }

            List<DiameterDictionaryModule> proposed = new List<DiameterDictionaryModule>();
            int moduleIndex = 0;
            while (moduleIndex < Modules.Count)
            {
                proposed.Add(Modules[moduleIndex]);
                moduleIndex = moduleIndex + 1;
            }

            proposed.Add(candidate);
            DictionaryRegistrationResult composition = ComposeModules(
                proposed,
                CurrentSnapshot.Revision + 1L);
            if (composition.Status != DictionaryRegistrationStatus.Success)
            {
                composition.Snapshot = CloneSnapshot(CurrentSnapshot);
                return composition;
            }

            CurrentSnapshot = composition.Snapshot;
            CurrentIndexes = new DictionaryIndexes(CurrentSnapshot);
            Modules = new List<DiameterDictionaryModule>();
            moduleIndex = 0;
            while (moduleIndex < CurrentSnapshot.Modules.Count)
            {
                Modules.Add(CurrentSnapshot.Modules[moduleIndex]);
                moduleIndex = moduleIndex + 1;
            }

            composition.Snapshot = CloneSnapshot(CurrentSnapshot);
            return composition;
        }
    }

    internal DiameterDictionarySnapshot ReadDictionarySnapshot()
    {
        lock (BuilderLock)
        {
            return CloneSnapshot(CurrentSnapshot);
        }
    }

    internal DictionaryRegistrationResult ComposeModules(
        List<DiameterDictionaryModule> modules,
        long revision)
    {
        DictionaryRegistrationResult ordering = OrderModules(modules);
        if (ordering.Status != DictionaryRegistrationStatus.Success)
        {
            return ordering;
        }

        List<DiameterDictionaryModule> ordered = ordering.Snapshot.Modules;
        DiameterDictionarySnapshot snapshot = new DiameterDictionarySnapshot();
        snapshot.Revision = revision;
        DictionaryIndexes indexes = new DictionaryIndexes(snapshot);
        int moduleIndex = 0;
        while (moduleIndex < ordered.Count)
        {
            DiameterDictionaryModule module = ordered[moduleIndex];
            snapshot.Modules.Add(module);
            DictionaryRegistrationResult merge = MergeModule(snapshot, indexes, module);
            if (merge.Status != DictionaryRegistrationStatus.Success)
            {
                return merge;
            }

            moduleIndex = moduleIndex + 1;
        }

        DictionaryRegistrationResult result = new DictionaryRegistrationResult();
        result.Status = DictionaryRegistrationStatus.Success;
        result.Code = "module-registered";
        result.Message = "The dictionary module was composed successfully.";
        result.Snapshot = snapshot;
        return result;
    }

    internal DictionaryRegistrationResult OrderModules(List<DiameterDictionaryModule> modules)
    {
        int moduleIndex = 0;
        while (moduleIndex < modules.Count)
        {
            DiameterDictionaryModule module = modules[moduleIndex];
            int dependencyIndex = 0;
            while (dependencyIndex < module.Dependencies.Count)
            {
                string dependency = module.Dependencies[dependencyIndex];
                if (FindModuleIndex(modules, dependency) < 0)
                {
                    return CreateRegistrationFailure(
                        DictionaryRegistrationStatus.Invalid,
                        "missing-module-dependency",
                        "A dictionary module dependency is not registered.");
                }

                dependencyIndex = dependencyIndex + 1;
            }

            moduleIndex = moduleIndex + 1;
        }

        List<DiameterDictionaryModule> ordered = new List<DiameterDictionaryModule>();
        while (ordered.Count < modules.Count)
        {
            DiameterDictionaryModule? selected = null;
            moduleIndex = 0;
            while (moduleIndex < modules.Count)
            {
                DiameterDictionaryModule candidate = modules[moduleIndex];
                if (FindModuleIndex(ordered, candidate.Name) < 0 &&
                    DependenciesAreOrdered(candidate, ordered))
                {
                    if (selected == null || string.Compare(
                        DictionaryIndexes.CreateCanonicalName(candidate.Name),
                        DictionaryIndexes.CreateCanonicalName(selected.Name),
                        StringComparison.Ordinal) < 0)
                    {
                        selected = candidate;
                    }
                }

                moduleIndex = moduleIndex + 1;
            }

            if (selected == null)
            {
                return CreateRegistrationFailure(
                    DictionaryRegistrationStatus.Invalid,
                    "circular-module-dependency",
                    "Dictionary module dependencies contain a cycle.");
            }

            ordered.Add(selected);
        }

        DictionaryRegistrationResult result = new DictionaryRegistrationResult();
        result.Status = DictionaryRegistrationStatus.Success;
        result.Snapshot.Modules = ordered;
        return result;
    }

    internal DictionaryRegistrationResult MergeModule(
        DiameterDictionarySnapshot snapshot,
        DictionaryIndexes indexes,
        DiameterDictionaryModule module)
    {
        DictionaryRegistrationResult result = MergeVendors(snapshot, indexes, module.Vendors);
        if (result.Status != DictionaryRegistrationStatus.Success)
        {
            return result;
        }

        result = MergeApplications(snapshot, indexes, module.Applications);
        if (result.Status != DictionaryRegistrationStatus.Success)
        {
            return result;
        }

        result = MergeTypes(snapshot, indexes, module.Types);
        if (result.Status != DictionaryRegistrationStatus.Success)
        {
            return result;
        }

        result = MergeEnumValues(snapshot, indexes, module.EnumValues);
        if (result.Status != DictionaryRegistrationStatus.Success)
        {
            return result;
        }

        result = MergeAvps(snapshot, indexes, module.Avps);
        if (result.Status != DictionaryRegistrationStatus.Success)
        {
            return result;
        }

        return MergeCommands(snapshot, indexes, module.Commands);
    }

    internal DictionaryRegistrationResult MergeVendors(
        DiameterDictionarySnapshot snapshot,
        DictionaryIndexes indexes,
        List<DiameterVendorDefinition> vendors)
    {
        int index = 0;
        while (index < vendors.Count)
        {
            DiameterVendorDefinition candidate = vendors[index];
            DiameterVendorDefinition? byId = indexes.FindVendorById(candidate.Id);
            DiameterVendorDefinition? byName = indexes.FindVendorByName(candidate.Name);
            if (byId != null || byName != null)
            {
                if (byId == null || byName == null || byId != byName ||
                    !DictionaryDefinitionComparer.AreVendorsEqual(byId, candidate))
                {
                    return CreateRegistrationFailure(
                        DictionaryRegistrationStatus.Conflict,
                        "vendor-definition-conflict",
                        "A vendor numeric identity or canonical name conflicts.");
                }
            }
            else
            {
                snapshot.Vendors.Add(candidate);
                List<DiameterVendorDefinition> added = new List<DiameterVendorDefinition>();
                added.Add(candidate);
                indexes.IndexVendors(added);
            }

            index = index + 1;
        }

        return CreateMergeSuccess();
    }

    internal DictionaryRegistrationResult MergeApplications(
        DiameterDictionarySnapshot snapshot,
        DictionaryIndexes indexes,
        List<DiameterApplicationDefinition> applications)
    {
        int index = 0;
        while (index < applications.Count)
        {
            DiameterApplicationDefinition candidate = applications[index];
            DiameterApplicationDefinition? byId = indexes.FindApplicationById(
                candidate.VendorId,
                candidate.Id);
            DiameterApplicationDefinition? byName = indexes.FindApplicationByName(
                candidate.VendorId,
                candidate.Name);
            if (byId != null || byName != null)
            {
                if (byId == null || byName == null || byId != byName ||
                    !DictionaryDefinitionComparer.AreApplicationsEqual(byId, candidate))
                {
                    return CreateRegistrationFailure(
                        DictionaryRegistrationStatus.Conflict,
                        "application-definition-conflict",
                        "An application numeric identity or canonical name conflicts.");
                }
            }
            else
            {
                snapshot.Applications.Add(candidate);
                List<DiameterApplicationDefinition> added =
                    new List<DiameterApplicationDefinition>();
                added.Add(candidate);
                indexes.IndexApplications(added);
            }

            index = index + 1;
        }

        return CreateMergeSuccess();
    }

    internal DictionaryRegistrationResult MergeTypes(
        DiameterDictionarySnapshot snapshot,
        DictionaryIndexes indexes,
        List<DiameterTypeDefinition> types)
    {
        int index = 0;
        while (index < types.Count)
        {
            DiameterTypeDefinition candidate = types[index];
            DiameterTypeDefinition? existing = indexes.FindTypeByName(
                candidate.VendorId,
                candidate.Name);
            if (existing != null)
            {
                if (!DictionaryDefinitionComparer.AreTypesEqual(existing, candidate))
                {
                    return CreateRegistrationFailure(
                        DictionaryRegistrationStatus.Conflict,
                        "type-definition-conflict",
                        "A canonical type name has conflicting definitions.");
                }
            }
            else
            {
                snapshot.Types.Add(candidate);
                List<DiameterTypeDefinition> added = new List<DiameterTypeDefinition>();
                added.Add(candidate);
                indexes.IndexTypes(added);
            }

            index = index + 1;
        }

        return CreateMergeSuccess();
    }

    internal DictionaryRegistrationResult MergeEnumValues(
        DiameterDictionarySnapshot snapshot,
        DictionaryIndexes indexes,
        List<DiameterEnumValueDefinition> enumValues)
    {
        int index = 0;
        while (index < enumValues.Count)
        {
            DiameterEnumValueDefinition candidate = enumValues[index];
            DiameterEnumValueDefinition? existing = FindExistingEnumValue(indexes, candidate);
            if (existing != null)
            {
                if (existing == candidate ||
                    !DictionaryDefinitionComparer.AreEnumValuesEqual(existing, candidate))
                {
                    return CreateRegistrationFailure(
                        DictionaryRegistrationStatus.Conflict,
                        "enum-value-definition-conflict",
                        "An Enumerated value name or number conflicts.");
                }
            }
            else
            {
                snapshot.EnumValues.Add(candidate);
                List<DiameterEnumValueDefinition> added =
                    new List<DiameterEnumValueDefinition>();
                added.Add(candidate);
                indexes.IndexEnumValues(added);
            }

            index = index + 1;
        }

        return CreateMergeSuccess();
    }

    internal DiameterEnumValueDefinition? FindExistingEnumValue(
        DictionaryIndexes indexes,
        DiameterEnumValueDefinition candidate)
    {
        DiameterEnumValueDefinition? byTypeValue = null;
        DiameterEnumValueDefinition? byTypeName = null;
        if (!string.IsNullOrWhiteSpace(candidate.TypeName))
        {
            byTypeValue = indexes.FindEnumValueByTypeValue(
                candidate.TypeVendorId,
                candidate.TypeName,
                candidate.Value);
            byTypeName = indexes.FindEnumValueByTypeName(
                candidate.TypeVendorId,
                candidate.TypeName,
                candidate.Name);
        }

        DiameterEnumValueDefinition? byAvpValue = null;
        DiameterEnumValueDefinition? byAvpName = null;
        if (candidate.AvpCode != 0U)
        {
            byAvpValue = indexes.FindEnumValueByAvpValue(
                candidate.AvpVendorId,
                candidate.AvpCode,
                candidate.Value);
            byAvpName = indexes.FindEnumValueByAvpName(
                candidate.AvpVendorId,
                candidate.AvpCode,
                candidate.Name);
        }

        DiameterEnumValueDefinition? existing = byTypeValue;
        if (existing == null)
        {
            existing = byTypeName;
        }

        if (existing == null)
        {
            existing = byAvpValue;
        }

        if (existing == null)
        {
            existing = byAvpName;
        }

        if ((byTypeValue != null && byTypeValue != existing) ||
            (byTypeName != null && byTypeName != existing) ||
            (byAvpValue != null && byAvpValue != existing) ||
            (byAvpName != null && byAvpName != existing))
        {
            return candidate;
        }

        return existing;
    }

    internal DictionaryRegistrationResult MergeAvps(
        DiameterDictionarySnapshot snapshot,
        DictionaryIndexes indexes,
        List<DiameterAvpDefinition> avps)
    {
        int index = 0;
        while (index < avps.Count)
        {
            DiameterAvpDefinition candidate = avps[index];
            DiameterAvpDefinition? byCode = indexes.FindAvpByCode(
                candidate.VendorId,
                candidate.Code);
            DiameterAvpDefinition? byName = indexes.FindAvpByName(
                candidate.VendorId,
                candidate.Name);
            if (byCode != null || byName != null)
            {
                if (byCode == null || byName == null || byCode != byName ||
                    !DictionaryDefinitionComparer.AreAvpsEqual(byCode, candidate))
                {
                    return CreateRegistrationFailure(
                        DictionaryRegistrationStatus.Conflict,
                        "avp-definition-conflict",
                        "An AVP numeric identity or canonical name conflicts.");
                }
            }
            else
            {
                snapshot.Avps.Add(candidate);
                List<DiameterAvpDefinition> added = new List<DiameterAvpDefinition>();
                added.Add(candidate);
                indexes.IndexAvps(added);
            }

            index = index + 1;
        }

        return CreateMergeSuccess();
    }

    internal DictionaryRegistrationResult MergeCommands(
        DiameterDictionarySnapshot snapshot,
        DictionaryIndexes indexes,
        List<DiameterCommandDefinition> commands)
    {
        int index = 0;
        while (index < commands.Count)
        {
            DiameterCommandDefinition candidate = commands[index];
            DiameterCommandDefinition? byCode = indexes.FindCommandByCode(
                candidate.ApplicationId,
                candidate.Code,
                candidate.IsRequest);
            DiameterCommandDefinition? byName = indexes.FindCommandByName(
                candidate.ApplicationId,
                candidate.Name,
                candidate.IsRequest);
            if (byCode != null || byName != null)
            {
                if (byCode == null || byName == null || byCode != byName ||
                    !DictionaryDefinitionComparer.AreCommandsEqual(byCode, candidate))
                {
                    return CreateRegistrationFailure(
                        DictionaryRegistrationStatus.Conflict,
                        "command-definition-conflict",
                        "A command numeric identity or canonical name conflicts.");
                }
            }
            else
            {
                snapshot.Commands.Add(candidate);
                List<DiameterCommandDefinition> added =
                    new List<DiameterCommandDefinition>();
                added.Add(candidate);
                indexes.IndexCommands(added);
            }

            index = index + 1;
        }

        return CreateMergeSuccess();
    }

    internal DictionaryRegistrationResult ValidateModule(DiameterDictionaryModule? module)
    {
        if (module == null)
        {
            return CreateRegistrationFailure(
                DictionaryRegistrationStatus.Invalid,
                "missing-dictionary-module",
                "A dictionary module is required.");
        }

        if (string.IsNullOrWhiteSpace(module.Name))
        {
            return CreateRegistrationFailure(
                DictionaryRegistrationStatus.Invalid,
                "missing-module-name",
                "A dictionary module requires a canonical name.");
        }

        if (module.Dependencies == null || module.Vendors == null ||
            module.Applications == null || module.Types == null ||
            module.EnumValues == null || module.Avps == null || module.Commands == null)
        {
            return CreateRegistrationFailure(
                DictionaryRegistrationStatus.Invalid,
                "missing-module-definitions",
                "Dictionary module definition collections cannot be null.");
        }

        if (!ValidateDefinitionEntries(module))
        {
            return CreateRegistrationFailure(
                DictionaryRegistrationStatus.Invalid,
                "invalid-module-definition",
                "Dictionary definitions require nonempty names and nonnull entries.");
        }

        return CreateMergeSuccess();
    }

    internal bool ValidateDefinitionEntries(DiameterDictionaryModule module)
    {
        int index = 0;
        while (index < module.Dependencies.Count)
        {
            string dependency = module.Dependencies[index];
            if (string.IsNullOrWhiteSpace(dependency) ||
                DictionaryDefinitionComparer.AreNamesEqual(dependency, module.Name))
            {
                return false;
            }

            index = index + 1;
        }

        index = 0;
        while (index < module.Vendors.Count)
        {
            DiameterVendorDefinition vendor = module.Vendors[index];
            if (vendor == null || string.IsNullOrWhiteSpace(vendor.Name))
            {
                return false;
            }

            index = index + 1;
        }

        index = 0;
        while (index < module.Applications.Count)
        {
            DiameterApplicationDefinition application = module.Applications[index];
            if (application == null || string.IsNullOrWhiteSpace(application.Name))
            {
                return false;
            }

            index = index + 1;
        }

        index = 0;
        while (index < module.Types.Count)
        {
            DiameterTypeDefinition type = module.Types[index];
            if (type == null || string.IsNullOrWhiteSpace(type.Name))
            {
                return false;
            }

            index = index + 1;
        }

        index = 0;
        while (index < module.EnumValues.Count)
        {
            DiameterEnumValueDefinition enumValue = module.EnumValues[index];
            if (enumValue == null || string.IsNullOrWhiteSpace(enumValue.Name) ||
                (string.IsNullOrWhiteSpace(enumValue.TypeName) && enumValue.AvpCode == 0U))
            {
                return false;
            }

            index = index + 1;
        }

        index = 0;
        while (index < module.Avps.Count)
        {
            DiameterAvpDefinition avp = module.Avps[index];
            if (avp == null || string.IsNullOrWhiteSpace(avp.Name) ||
                string.IsNullOrWhiteSpace(avp.TypeName) || avp.GroupedRules == null ||
                !ValidateRuleEntries(avp.GroupedRules))
            {
                return false;
            }

            index = index + 1;
        }

        index = 0;
        while (index < module.Commands.Count)
        {
            DiameterCommandDefinition command = module.Commands[index];
            if (command == null || string.IsNullOrWhiteSpace(command.Name) ||
                command.Rules == null || !ValidateRuleEntries(command.Rules))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool ValidateRuleEntries(List<DiameterAvpRuleDefinition> rules)
    {
        int index = 0;
        while (index < rules.Count)
        {
            DiameterAvpRuleDefinition rule = rules[index];
            if (rule == null || string.IsNullOrWhiteSpace(rule.AvpName))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool DependenciesAreOrdered(
        DiameterDictionaryModule module,
        List<DiameterDictionaryModule> ordered)
    {
        int dependencyIndex = 0;
        while (dependencyIndex < module.Dependencies.Count)
        {
            if (FindModuleIndex(ordered, module.Dependencies[dependencyIndex]) < 0)
            {
                return false;
            }

            dependencyIndex = dependencyIndex + 1;
        }

        return true;
    }

    internal static int FindModuleIndex(
        List<DiameterDictionaryModule> modules,
        string? name)
    {
        int index = 0;
        while (index < modules.Count)
        {
            if (DictionaryDefinitionComparer.AreNamesEqual(modules[index].Name, name))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static DictionaryRegistrationResult CreateMergeSuccess()
    {
        DictionaryRegistrationResult result = new DictionaryRegistrationResult();
        result.Status = DictionaryRegistrationStatus.Success;
        return result;
    }

    internal DictionaryRegistrationResult CreateRegistrationFailure(
        DictionaryRegistrationStatus status,
        string code,
        string message)
    {
        DictionaryRegistrationResult result = new DictionaryRegistrationResult();
        result.Status = status;
        result.Code = code;
        result.Message = message;
        result.Snapshot = CloneSnapshot(CurrentSnapshot);
        return result;
    }

    internal static DiameterDictionarySnapshot CloneSnapshot(
        DiameterDictionarySnapshot source)
    {
        DiameterDictionarySnapshot result = new DiameterDictionarySnapshot();
        result.Revision = source.Revision;
        int index = 0;
        while (index < source.Modules.Count)
        {
            result.Modules.Add(CloneModule(source.Modules[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.Vendors.Count)
        {
            result.Vendors.Add(DictionaryDefinitionComparer.CloneVendor(source.Vendors[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.Applications.Count)
        {
            result.Applications.Add(DictionaryDefinitionComparer.CloneApplication(
                source.Applications[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.Types.Count)
        {
            result.Types.Add(DictionaryDefinitionComparer.CloneType(source.Types[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.EnumValues.Count)
        {
            result.EnumValues.Add(DictionaryDefinitionComparer.CloneEnumValue(
                source.EnumValues[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.Avps.Count)
        {
            result.Avps.Add(DictionaryDefinitionComparer.CloneAvp(source.Avps[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.Commands.Count)
        {
            result.Commands.Add(DictionaryDefinitionComparer.CloneCommand(
                source.Commands[index]));
            index = index + 1;
        }

        return result;
    }

    internal static DiameterDictionaryModule CloneModule(DiameterDictionaryModule source)
    {
        DiameterDictionaryModule result = new DiameterDictionaryModule();
        result.Name = source.Name.Trim();
        result.Version = source.Version;
        int index = 0;
        while (index < source.Dependencies.Count)
        {
            result.Dependencies.Add(source.Dependencies[index].Trim());
            index = index + 1;
        }

        index = 0;
        while (index < source.Vendors.Count)
        {
            result.Vendors.Add(DictionaryDefinitionComparer.CloneVendor(source.Vendors[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.Applications.Count)
        {
            result.Applications.Add(DictionaryDefinitionComparer.CloneApplication(
                source.Applications[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.Types.Count)
        {
            result.Types.Add(DictionaryDefinitionComparer.CloneType(source.Types[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.EnumValues.Count)
        {
            result.EnumValues.Add(DictionaryDefinitionComparer.CloneEnumValue(
                source.EnumValues[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.Avps.Count)
        {
            result.Avps.Add(DictionaryDefinitionComparer.CloneAvp(source.Avps[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.Commands.Count)
        {
            result.Commands.Add(DictionaryDefinitionComparer.CloneCommand(
                source.Commands[index]));
            index = index + 1;
        }

        return result;
    }
}

internal static class DictionaryDefinitionComparer
{
    internal static bool AreNamesEqual(string? first, string? second)
    {
        return string.Equals(
            DictionaryIndexes.CreateCanonicalName(first),
            DictionaryIndexes.CreateCanonicalName(second),
            StringComparison.Ordinal);
    }

    internal static bool AreModulesEqual(
        DiameterDictionaryModule first,
        DiameterDictionaryModule second)
    {
        return AreNamesEqual(first.Name, second.Name) && first.Version == second.Version &&
            AreNameListsEqual(first.Dependencies, second.Dependencies) &&
            AreVendorListsEqual(first.Vendors, second.Vendors) &&
            AreApplicationListsEqual(first.Applications, second.Applications) &&
            AreTypeListsEqual(first.Types, second.Types) &&
            AreEnumValueListsEqual(first.EnumValues, second.EnumValues) &&
            AreAvpListsEqual(first.Avps, second.Avps) &&
            AreCommandListsEqual(first.Commands, second.Commands);
    }

    internal static bool AreVendorsEqual(
        DiameterVendorDefinition first,
        DiameterVendorDefinition second)
    {
        return first.Id == second.Id && AreNamesEqual(first.Name, second.Name);
    }

    internal static bool AreApplicationsEqual(
        DiameterApplicationDefinition first,
        DiameterApplicationDefinition second)
    {
        return first.Id == second.Id && first.VendorId == second.VendorId &&
            first.IsAccounting == second.IsAccounting && AreNamesEqual(first.Name, second.Name);
    }

    internal static bool AreTypesEqual(
        DiameterTypeDefinition first,
        DiameterTypeDefinition second)
    {
        return first.VendorId == second.VendorId && first.BaseType == second.BaseType &&
            first.ParentTypeVendorId == second.ParentTypeVendorId &&
            first.FixedDataLength == second.FixedDataLength &&
            AreNamesEqual(first.Name, second.Name) &&
            AreNamesEqual(first.ParentTypeName, second.ParentTypeName);
    }

    internal static bool AreEnumValuesEqual(
        DiameterEnumValueDefinition first,
        DiameterEnumValueDefinition second)
    {
        return first.Value == second.Value && first.TypeVendorId == second.TypeVendorId &&
            first.AvpCode == second.AvpCode && first.AvpVendorId == second.AvpVendorId &&
            AreNamesEqual(first.Name, second.Name) &&
            AreNamesEqual(first.TypeName, second.TypeName);
    }

    internal static bool AreRulesEqual(
        DiameterAvpRuleDefinition first,
        DiameterAvpRuleDefinition second)
    {
        return first.AvpCode == second.AvpCode &&
            first.AvpVendorId == second.AvpVendorId &&
            first.MinimumCount == second.MinimumCount &&
            first.MaximumCount == second.MaximumCount &&
            first.FixedHeadPosition == second.FixedHeadPosition &&
            first.OrderingGroup == second.OrderingGroup &&
            AreNamesEqual(first.AvpName, second.AvpName);
    }

    internal static bool AreAvpsEqual(
        DiameterAvpDefinition first,
        DiameterAvpDefinition second)
    {
        return first.Code == second.Code && first.VendorId == second.VendorId &&
            first.BaseType == second.BaseType && first.RequiredFlags == second.RequiredFlags &&
            first.ProhibitedFlags == second.ProhibitedFlags &&
            first.FixedDataLength == second.FixedDataLength &&
            first.AllowUnlistedAvps == second.AllowUnlistedAvps &&
            AreNamesEqual(first.Name, second.Name) &&
            AreNamesEqual(first.TypeName, second.TypeName) &&
            AreRuleListsEqual(first.GroupedRules, second.GroupedRules);
    }

    internal static bool AreCommandsEqual(
        DiameterCommandDefinition first,
        DiameterCommandDefinition second)
    {
        return first.Code == second.Code && first.ApplicationId == second.ApplicationId &&
            first.IsRequest == second.IsRequest &&
            first.RequiredFlags == second.RequiredFlags &&
            first.ProhibitedFlags == second.ProhibitedFlags &&
            first.AllowUnlistedAvps == second.AllowUnlistedAvps &&
            AreNamesEqual(first.Name, second.Name) &&
            AreRuleListsEqual(first.Rules, second.Rules);
    }

    internal static bool AreNameListsEqual(List<string> first, List<string> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        bool[] matched = new bool[second.Count];
        int firstIndex = 0;
        while (firstIndex < first.Count)
        {
            bool found = false;
            int secondIndex = 0;
            while (secondIndex < second.Count)
            {
                if (!matched[secondIndex] &&
                    AreNamesEqual(first[firstIndex], second[secondIndex]))
                {
                    matched[secondIndex] = true;
                    found = true;
                    break;
                }

                secondIndex = secondIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            firstIndex = firstIndex + 1;
        }

        return true;
    }

    internal static bool AreVendorListsEqual(
        List<DiameterVendorDefinition> first,
        List<DiameterVendorDefinition> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        bool[] matched = new bool[second.Count];
        int firstIndex = 0;
        while (firstIndex < first.Count)
        {
            bool found = false;
            int secondIndex = 0;
            while (secondIndex < second.Count)
            {
                if (!matched[secondIndex] &&
                    AreVendorsEqual(first[firstIndex], second[secondIndex]))
                {
                    matched[secondIndex] = true;
                    found = true;
                    break;
                }

                secondIndex = secondIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            firstIndex = firstIndex + 1;
        }

        return true;
    }

    internal static bool AreApplicationListsEqual(
        List<DiameterApplicationDefinition> first,
        List<DiameterApplicationDefinition> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        bool[] matched = new bool[second.Count];
        int firstIndex = 0;
        while (firstIndex < first.Count)
        {
            bool found = false;
            int secondIndex = 0;
            while (secondIndex < second.Count)
            {
                if (!matched[secondIndex] &&
                    AreApplicationsEqual(first[firstIndex], second[secondIndex]))
                {
                    matched[secondIndex] = true;
                    found = true;
                    break;
                }

                secondIndex = secondIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            firstIndex = firstIndex + 1;
        }

        return true;
    }

    internal static bool AreTypeListsEqual(
        List<DiameterTypeDefinition> first,
        List<DiameterTypeDefinition> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        bool[] matched = new bool[second.Count];
        int firstIndex = 0;
        while (firstIndex < first.Count)
        {
            bool found = false;
            int secondIndex = 0;
            while (secondIndex < second.Count)
            {
                if (!matched[secondIndex] &&
                    AreTypesEqual(first[firstIndex], second[secondIndex]))
                {
                    matched[secondIndex] = true;
                    found = true;
                    break;
                }

                secondIndex = secondIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            firstIndex = firstIndex + 1;
        }

        return true;
    }

    internal static bool AreEnumValueListsEqual(
        List<DiameterEnumValueDefinition> first,
        List<DiameterEnumValueDefinition> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        bool[] matched = new bool[second.Count];
        int firstIndex = 0;
        while (firstIndex < first.Count)
        {
            bool found = false;
            int secondIndex = 0;
            while (secondIndex < second.Count)
            {
                if (!matched[secondIndex] &&
                    AreEnumValuesEqual(first[firstIndex], second[secondIndex]))
                {
                    matched[secondIndex] = true;
                    found = true;
                    break;
                }

                secondIndex = secondIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            firstIndex = firstIndex + 1;
        }

        return true;
    }

    internal static bool AreAvpListsEqual(
        List<DiameterAvpDefinition> first,
        List<DiameterAvpDefinition> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        bool[] matched = new bool[second.Count];
        int firstIndex = 0;
        while (firstIndex < first.Count)
        {
            bool found = false;
            int secondIndex = 0;
            while (secondIndex < second.Count)
            {
                if (!matched[secondIndex] &&
                    AreAvpsEqual(first[firstIndex], second[secondIndex]))
                {
                    matched[secondIndex] = true;
                    found = true;
                    break;
                }

                secondIndex = secondIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            firstIndex = firstIndex + 1;
        }

        return true;
    }

    internal static bool AreCommandListsEqual(
        List<DiameterCommandDefinition> first,
        List<DiameterCommandDefinition> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        bool[] matched = new bool[second.Count];
        int firstIndex = 0;
        while (firstIndex < first.Count)
        {
            bool found = false;
            int secondIndex = 0;
            while (secondIndex < second.Count)
            {
                if (!matched[secondIndex] &&
                    AreCommandsEqual(first[firstIndex], second[secondIndex]))
                {
                    matched[secondIndex] = true;
                    found = true;
                    break;
                }

                secondIndex = secondIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            firstIndex = firstIndex + 1;
        }

        return true;
    }

    internal static bool AreRuleListsEqual(
        List<DiameterAvpRuleDefinition> first,
        List<DiameterAvpRuleDefinition> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        bool[] matched = new bool[second.Count];
        int firstIndex = 0;
        while (firstIndex < first.Count)
        {
            bool found = false;
            int secondIndex = 0;
            while (secondIndex < second.Count)
            {
                if (!matched[secondIndex] &&
                    AreRulesEqual(first[firstIndex], second[secondIndex]))
                {
                    matched[secondIndex] = true;
                    found = true;
                    break;
                }

                secondIndex = secondIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            firstIndex = firstIndex + 1;
        }

        return true;
    }

    internal static DiameterVendorDefinition CloneVendor(DiameterVendorDefinition source)
    {
        DiameterVendorDefinition result = new DiameterVendorDefinition();
        result.Id = source.Id;
        result.Name = source.Name;
        return result;
    }

    internal static DiameterApplicationDefinition CloneApplication(
        DiameterApplicationDefinition source)
    {
        DiameterApplicationDefinition result = new DiameterApplicationDefinition();
        result.Id = source.Id;
        result.VendorId = source.VendorId;
        result.Name = source.Name;
        result.IsAccounting = source.IsAccounting;
        return result;
    }

    internal static DiameterTypeDefinition CloneType(DiameterTypeDefinition source)
    {
        DiameterTypeDefinition result = new DiameterTypeDefinition();
        result.Name = source.Name;
        result.VendorId = source.VendorId;
        result.BaseType = source.BaseType;
        result.ParentTypeName = source.ParentTypeName;
        result.ParentTypeVendorId = source.ParentTypeVendorId;
        result.FixedDataLength = source.FixedDataLength;
        return result;
    }

    internal static DiameterEnumValueDefinition CloneEnumValue(
        DiameterEnumValueDefinition source)
    {
        DiameterEnumValueDefinition result = new DiameterEnumValueDefinition();
        result.Name = source.Name;
        result.Value = source.Value;
        result.TypeName = source.TypeName;
        result.TypeVendorId = source.TypeVendorId;
        result.AvpCode = source.AvpCode;
        result.AvpVendorId = source.AvpVendorId;
        return result;
    }

    internal static DiameterAvpRuleDefinition CloneRule(DiameterAvpRuleDefinition source)
    {
        DiameterAvpRuleDefinition result = new DiameterAvpRuleDefinition();
        result.AvpCode = source.AvpCode;
        result.AvpVendorId = source.AvpVendorId;
        result.AvpName = source.AvpName;
        result.MinimumCount = source.MinimumCount;
        result.MaximumCount = source.MaximumCount;
        result.FixedHeadPosition = source.FixedHeadPosition;
        result.OrderingGroup = source.OrderingGroup;
        return result;
    }

    internal static DiameterAvpDefinition CloneAvp(DiameterAvpDefinition source)
    {
        DiameterAvpDefinition result = new DiameterAvpDefinition();
        result.Code = source.Code;
        result.VendorId = source.VendorId;
        result.Name = source.Name;
        result.TypeName = source.TypeName;
        result.BaseType = source.BaseType;
        result.RequiredFlags = source.RequiredFlags;
        result.ProhibitedFlags = source.ProhibitedFlags;
        result.FixedDataLength = source.FixedDataLength;
        result.AllowUnlistedAvps = source.AllowUnlistedAvps;
        int index = 0;
        while (index < source.GroupedRules.Count)
        {
            result.GroupedRules.Add(CloneRule(source.GroupedRules[index]));
            index = index + 1;
        }

        return result;
    }

    internal static DiameterCommandDefinition CloneCommand(
        DiameterCommandDefinition source)
    {
        DiameterCommandDefinition result = new DiameterCommandDefinition();
        result.Code = source.Code;
        result.ApplicationId = source.ApplicationId;
        result.Name = source.Name;
        result.IsRequest = source.IsRequest;
        result.RequiredFlags = source.RequiredFlags;
        result.ProhibitedFlags = source.ProhibitedFlags;
        result.AllowUnlistedAvps = source.AllowUnlistedAvps;
        int index = 0;
        while (index < source.Rules.Count)
        {
            result.Rules.Add(CloneRule(source.Rules[index]));
            index = index + 1;
        }

        return result;
    }
}
