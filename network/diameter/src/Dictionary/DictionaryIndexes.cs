using System;
using System.Collections.Generic;
using System.Globalization;

namespace diameter;

internal class DictionaryIndexes
{
    internal Dictionary<string, DiameterDictionaryModule> ModulesByName =
        new Dictionary<string, DiameterDictionaryModule>();
    internal Dictionary<uint, DiameterVendorDefinition> VendorsById =
        new Dictionary<uint, DiameterVendorDefinition>();
    internal Dictionary<string, DiameterVendorDefinition> VendorsByName =
        new Dictionary<string, DiameterVendorDefinition>();
    internal Dictionary<string, DiameterApplicationDefinition> ApplicationsById =
        new Dictionary<string, DiameterApplicationDefinition>();
    internal Dictionary<string, DiameterApplicationDefinition> ApplicationsByName =
        new Dictionary<string, DiameterApplicationDefinition>();
    internal Dictionary<string, DiameterTypeDefinition> TypesByName =
        new Dictionary<string, DiameterTypeDefinition>();
    internal Dictionary<string, DiameterEnumValueDefinition> EnumValuesByTypeValue =
        new Dictionary<string, DiameterEnumValueDefinition>();
    internal Dictionary<string, DiameterEnumValueDefinition> EnumValuesByTypeName =
        new Dictionary<string, DiameterEnumValueDefinition>();
    internal Dictionary<string, DiameterEnumValueDefinition> EnumValuesByAvpValue =
        new Dictionary<string, DiameterEnumValueDefinition>();
    internal Dictionary<string, DiameterEnumValueDefinition> EnumValuesByAvpName =
        new Dictionary<string, DiameterEnumValueDefinition>();
    internal Dictionary<string, DiameterAvpDefinition> AvpsByCode =
        new Dictionary<string, DiameterAvpDefinition>();
    internal Dictionary<string, DiameterAvpDefinition> AvpsByName =
        new Dictionary<string, DiameterAvpDefinition>();
    internal Dictionary<string, DiameterCommandDefinition> CommandsByCode =
        new Dictionary<string, DiameterCommandDefinition>();
    internal Dictionary<string, DiameterCommandDefinition> CommandsByName =
        new Dictionary<string, DiameterCommandDefinition>();
    internal Dictionary<string, DiameterAvpRuleDefinition> GroupedRulesByCode =
        new Dictionary<string, DiameterAvpRuleDefinition>();
    internal Dictionary<string, DiameterAvpRuleDefinition> GroupedRulesByName =
        new Dictionary<string, DiameterAvpRuleDefinition>();
    internal Dictionary<string, DiameterAvpRuleDefinition> CommandRulesByCode =
        new Dictionary<string, DiameterAvpRuleDefinition>();
    internal Dictionary<string, DiameterAvpRuleDefinition> CommandRulesByName =
        new Dictionary<string, DiameterAvpRuleDefinition>();

    internal DictionaryIndexes(DiameterDictionarySnapshot? snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        IndexModules(snapshot.Modules);
        IndexVendors(snapshot.Vendors);
        IndexApplications(snapshot.Applications);
        IndexTypes(snapshot.Types);
        IndexEnumValues(snapshot.EnumValues);
        IndexAvps(snapshot.Avps);
        IndexCommands(snapshot.Commands);
    }

    internal DiameterDictionaryModule? FindModuleByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return ReadModule(ModulesByName, CreateCanonicalName(name));
    }

    internal DiameterVendorDefinition? FindVendorById(uint vendorId)
    {
        if (VendorsById.ContainsKey(vendorId))
        {
            return VendorsById[vendorId];
        }

        return null;
    }

    internal DiameterVendorDefinition? FindVendorByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return ReadVendor(VendorsByName, CreateCanonicalName(name));
    }

    internal DiameterApplicationDefinition? FindApplicationById(uint vendorId, uint applicationId)
    {
        return ReadApplication(
            ApplicationsById,
            CreateUnsignedPairKey(vendorId, applicationId));
    }

    internal DiameterApplicationDefinition? FindApplicationByName(
        uint vendorId,
        string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return ReadApplication(
            ApplicationsByName,
            CreateScopedNameKey(vendorId, name));
    }

    internal DiameterTypeDefinition? FindTypeByName(uint vendorId, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return ReadType(TypesByName, CreateScopedNameKey(vendorId, name));
    }

    internal DiameterEnumValueDefinition? FindEnumValueByTypeValue(
        uint typeVendorId,
        string? typeName,
        int value)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        string key = CreateScopedNameKey(typeVendorId, typeName) +
            "#" + value.ToString(CultureInfo.InvariantCulture);
        return ReadEnumValue(EnumValuesByTypeValue, key);
    }

    internal DiameterEnumValueDefinition? FindEnumValueByTypeName(
        uint typeVendorId,
        string? typeName,
        string? valueName)
    {
        if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(valueName))
        {
            return null;
        }

        string key = CreateTwoNameKey(typeVendorId, typeName, valueName);
        return ReadEnumValue(EnumValuesByTypeName, key);
    }

    internal DiameterEnumValueDefinition? FindEnumValueByAvpValue(
        uint avpVendorId,
        uint avpCode,
        int value)
    {
        string key = CreateUnsignedPairKey(avpVendorId, avpCode) +
            "#" + value.ToString(CultureInfo.InvariantCulture);
        return ReadEnumValue(EnumValuesByAvpValue, key);
    }

    internal DiameterEnumValueDefinition? FindEnumValueByAvpName(
        uint avpVendorId,
        uint avpCode,
        string? valueName)
    {
        if (string.IsNullOrWhiteSpace(valueName))
        {
            return null;
        }

        string key = CreateNumericScopeNameKey(avpVendorId, avpCode, valueName);
        return ReadEnumValue(EnumValuesByAvpName, key);
    }

    internal DiameterAvpDefinition? FindAvpByCode(uint vendorId, uint avpCode)
    {
        return ReadAvp(AvpsByCode, CreateUnsignedPairKey(vendorId, avpCode));
    }

    internal DiameterAvpDefinition? FindAvpByName(uint vendorId, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return ReadAvp(AvpsByName, CreateScopedNameKey(vendorId, name));
    }

    internal DiameterCommandDefinition? FindCommandByCode(
        uint applicationId,
        uint commandCode,
        bool request)
    {
        return ReadCommand(
            CommandsByCode,
            CreateCommandKey(applicationId, commandCode, request));
    }

    internal DiameterCommandDefinition? FindCommandByName(
        uint applicationId,
        string? name,
        bool request)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return ReadCommand(
            CommandsByName,
            CreateCommandNameKey(applicationId, name, request));
    }

    internal DiameterAvpRuleDefinition? FindGroupedRuleByCode(
        uint groupedVendorId,
        uint groupedAvpCode,
        uint childVendorId,
        uint childAvpCode)
    {
        return ReadRule(
            GroupedRulesByCode,
            CreateRuleKey(
                groupedVendorId,
                groupedAvpCode,
                childVendorId,
                childAvpCode));
    }

    internal DiameterAvpRuleDefinition? FindGroupedRuleByName(
        uint groupedVendorId,
        uint groupedAvpCode,
        string? childName)
    {
        if (string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        return ReadRule(
            GroupedRulesByName,
            CreateNumericScopeNameKey(groupedVendorId, groupedAvpCode, childName));
    }

    internal DiameterAvpRuleDefinition? FindCommandRuleByCode(
        uint applicationId,
        uint commandCode,
        bool request,
        uint childVendorId,
        uint childAvpCode)
    {
        string key = CreateCommandKey(applicationId, commandCode, request) +
            "#" + CreateUnsignedPairKey(childVendorId, childAvpCode);
        return ReadRule(CommandRulesByCode, key);
    }

    internal DiameterAvpRuleDefinition? FindCommandRuleByName(
        uint applicationId,
        uint commandCode,
        bool request,
        string? childName)
    {
        if (string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        string key = CreateCommandKey(applicationId, commandCode, request) +
            "#" + CreateCanonicalLengthName(childName);
        return ReadRule(CommandRulesByName, key);
    }

    internal void IndexModules(List<DiameterDictionaryModule>? modules)
    {
        if (modules == null)
        {
            return;
        }

        int index = 0;
        while (index < modules.Count)
        {
            DiameterDictionaryModule module = modules[index];
            if (module != null)
            {
                ModulesByName[CreateCanonicalName(module.Name)] = module;
            }

            index = index + 1;
        }
    }

    internal void IndexVendors(List<DiameterVendorDefinition>? vendors)
    {
        if (vendors == null)
        {
            return;
        }

        int index = 0;
        while (index < vendors.Count)
        {
            DiameterVendorDefinition vendor = vendors[index];
            if (vendor != null)
            {
                VendorsById[vendor.Id] = vendor;
                VendorsByName[CreateCanonicalName(vendor.Name)] = vendor;
            }

            index = index + 1;
        }
    }

    internal void IndexApplications(List<DiameterApplicationDefinition>? applications)
    {
        if (applications == null)
        {
            return;
        }

        int index = 0;
        while (index < applications.Count)
        {
            DiameterApplicationDefinition application = applications[index];
            if (application != null)
            {
                ApplicationsById[CreateUnsignedPairKey(application.VendorId, application.Id)] =
                    application;
                ApplicationsByName[CreateScopedNameKey(
                    application.VendorId,
                    application.Name)] = application;
            }

            index = index + 1;
        }
    }

    internal void IndexTypes(List<DiameterTypeDefinition>? types)
    {
        if (types == null)
        {
            return;
        }

        int index = 0;
        while (index < types.Count)
        {
            DiameterTypeDefinition type = types[index];
            if (type != null)
            {
                TypesByName[CreateScopedNameKey(type.VendorId, type.Name)] = type;
            }

            index = index + 1;
        }
    }

    internal void IndexEnumValues(List<DiameterEnumValueDefinition>? enumValues)
    {
        if (enumValues == null)
        {
            return;
        }

        int index = 0;
        while (index < enumValues.Count)
        {
            DiameterEnumValueDefinition enumValue = enumValues[index];
            if (enumValue != null)
            {
                if (!string.IsNullOrWhiteSpace(enumValue.TypeName))
                {
                    string typeKey = CreateScopedNameKey(
                        enumValue.TypeVendorId,
                        enumValue.TypeName);
                    EnumValuesByTypeValue[typeKey + "#" +
                        enumValue.Value.ToString(CultureInfo.InvariantCulture)] = enumValue;
                    EnumValuesByTypeName[CreateTwoNameKey(
                        enumValue.TypeVendorId,
                        enumValue.TypeName,
                        enumValue.Name)] = enumValue;
                }

                if (enumValue.AvpCode != 0U)
                {
                    string avpKey = CreateUnsignedPairKey(
                        enumValue.AvpVendorId,
                        enumValue.AvpCode);
                    EnumValuesByAvpValue[avpKey + "#" +
                        enumValue.Value.ToString(CultureInfo.InvariantCulture)] = enumValue;
                    EnumValuesByAvpName[CreateNumericScopeNameKey(
                        enumValue.AvpVendorId,
                        enumValue.AvpCode,
                        enumValue.Name)] = enumValue;
                }
            }

            index = index + 1;
        }
    }

    internal void IndexAvps(List<DiameterAvpDefinition>? avps)
    {
        if (avps == null)
        {
            return;
        }

        int index = 0;
        while (index < avps.Count)
        {
            DiameterAvpDefinition avp = avps[index];
            if (avp != null)
            {
                AvpsByCode[CreateUnsignedPairKey(avp.VendorId, avp.Code)] = avp;
                AvpsByName[CreateScopedNameKey(avp.VendorId, avp.Name)] = avp;
                IndexGroupedRules(avp);
            }

            index = index + 1;
        }
    }

    internal void IndexCommands(List<DiameterCommandDefinition>? commands)
    {
        if (commands == null)
        {
            return;
        }

        int index = 0;
        while (index < commands.Count)
        {
            DiameterCommandDefinition command = commands[index];
            if (command != null)
            {
                CommandsByCode[CreateCommandKey(
                    command.ApplicationId,
                    command.Code,
                    command.IsRequest)] = command;
                CommandsByName[CreateCommandNameKey(
                    command.ApplicationId,
                    command.Name,
                    command.IsRequest)] = command;
                IndexCommandRules(command);
            }

            index = index + 1;
        }
    }

    internal void IndexGroupedRules(DiameterAvpDefinition avp)
    {
        if (avp.GroupedRules == null)
        {
            return;
        }

        int index = 0;
        while (index < avp.GroupedRules.Count)
        {
            DiameterAvpRuleDefinition rule = avp.GroupedRules[index];
            if (rule != null)
            {
                GroupedRulesByCode[CreateRuleKey(
                    avp.VendorId,
                    avp.Code,
                    rule.AvpVendorId,
                    rule.AvpCode)] = rule;
                GroupedRulesByName[CreateNumericScopeNameKey(
                    avp.VendorId,
                    avp.Code,
                    rule.AvpName)] = rule;
            }

            index = index + 1;
        }
    }

    internal void IndexCommandRules(DiameterCommandDefinition command)
    {
        if (command.Rules == null)
        {
            return;
        }

        string commandKey = CreateCommandKey(
            command.ApplicationId,
            command.Code,
            command.IsRequest);
        int index = 0;
        while (index < command.Rules.Count)
        {
            DiameterAvpRuleDefinition rule = command.Rules[index];
            if (rule != null)
            {
                CommandRulesByCode[commandKey + "#" + CreateUnsignedPairKey(
                    rule.AvpVendorId,
                    rule.AvpCode)] = rule;
                CommandRulesByName[commandKey + "#" +
                    CreateCanonicalLengthName(rule.AvpName)] = rule;
            }

            index = index + 1;
        }
    }

    internal static string CreateCanonicalName(string? name)
    {
        if (name == null)
        {
            return string.Empty;
        }

        return name.Trim().ToLowerInvariant();
    }

    internal static string CreateUnsignedPairKey(uint first, uint second)
    {
        return first.ToString(CultureInfo.InvariantCulture) + "#" +
            second.ToString(CultureInfo.InvariantCulture);
    }

    internal static string CreateScopedNameKey(uint scope, string? name)
    {
        return scope.ToString(CultureInfo.InvariantCulture) + "#" +
            CreateCanonicalLengthName(name);
    }

    internal static string CreateTwoNameKey(uint scope, string? first, string? second)
    {
        return scope.ToString(CultureInfo.InvariantCulture) + "#" +
            CreateCanonicalLengthName(first) + "#" +
            CreateCanonicalLengthName(second);
    }

    internal static string CreateNumericScopeNameKey(
        uint first,
        uint second,
        string? name)
    {
        return CreateUnsignedPairKey(first, second) + "#" +
            CreateCanonicalLengthName(name);
    }

    internal static string CreateCommandKey(uint applicationId, uint commandCode, bool request)
    {
        string direction = "0";
        if (request)
        {
            direction = "1";
        }

        return CreateUnsignedPairKey(applicationId, commandCode) + "#" + direction;
    }

    internal static string CreateCommandNameKey(
        uint applicationId,
        string? name,
        bool request)
    {
        string direction = "0";
        if (request)
        {
            direction = "1";
        }

        return applicationId.ToString(CultureInfo.InvariantCulture) + "#" + direction + "#" +
            CreateCanonicalLengthName(name);
    }

    internal static string CreateRuleKey(
        uint parentVendorId,
        uint parentAvpCode,
        uint childVendorId,
        uint childAvpCode)
    {
        return CreateUnsignedPairKey(parentVendorId, parentAvpCode) + "#" +
            CreateUnsignedPairKey(childVendorId, childAvpCode);
    }

    internal static string CreateCanonicalLengthName(string? name)
    {
        string canonical = CreateCanonicalName(name);
        return canonical.Length.ToString(CultureInfo.InvariantCulture) + "#" + canonical;
    }

    internal static DiameterDictionaryModule? ReadModule(
        Dictionary<string, DiameterDictionaryModule> index,
        string key)
    {
        if (index.ContainsKey(key))
        {
            return index[key];
        }

        return null;
    }

    internal static DiameterVendorDefinition? ReadVendor(
        Dictionary<string, DiameterVendorDefinition> index,
        string key)
    {
        if (index.ContainsKey(key))
        {
            return index[key];
        }

        return null;
    }

    internal static DiameterApplicationDefinition? ReadApplication(
        Dictionary<string, DiameterApplicationDefinition> index,
        string key)
    {
        if (index.ContainsKey(key))
        {
            return index[key];
        }

        return null;
    }

    internal static DiameterTypeDefinition? ReadType(
        Dictionary<string, DiameterTypeDefinition> index,
        string key)
    {
        if (index.ContainsKey(key))
        {
            return index[key];
        }

        return null;
    }

    internal static DiameterEnumValueDefinition? ReadEnumValue(
        Dictionary<string, DiameterEnumValueDefinition> index,
        string key)
    {
        if (index.ContainsKey(key))
        {
            return index[key];
        }

        return null;
    }

    internal static DiameterAvpDefinition? ReadAvp(
        Dictionary<string, DiameterAvpDefinition> index,
        string key)
    {
        if (index.ContainsKey(key))
        {
            return index[key];
        }

        return null;
    }

    internal static DiameterCommandDefinition? ReadCommand(
        Dictionary<string, DiameterCommandDefinition> index,
        string key)
    {
        if (index.ContainsKey(key))
        {
            return index[key];
        }

        return null;
    }

    internal static DiameterAvpRuleDefinition? ReadRule(
        Dictionary<string, DiameterAvpRuleDefinition> index,
        string key)
    {
        if (index.ContainsKey(key))
        {
            return index[key];
        }

        return null;
    }
}
