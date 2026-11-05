using System;
using System.Collections.Generic;
using System.Globalization;

namespace fix;

internal static class SbeSchemaValidation
{
    internal static void ValidateSchemaPlan(SbeMappingPlan plan)
    {
        ValidateSchemaIdentifiers(plan);
        ValidateSchemaTypeMappings(plan);
        ValidateSchemaFieldMappings(plan);
        ValidateSchemaStructures(plan);
        ValidateSchemaRecursion(plan);
        ValidateSchemaPrecision(plan);
    }

    internal static void ValidateSchemaIdentifiers(SbeMappingPlan plan)
    {
        if (plan.SchemaId < 0 || plan.SchemaId > ushort.MaxValue) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "invalid-sbe-schema-id", "The SBE schema identifier must fit an unsigned 16-bit integer.", "/schema/id");
        if (plan.Version < 0) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "invalid-sbe-schema-version", "The SBE schema version must not be negative.", "/schema/version");
        if (plan.Messages.Count == 0) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "empty-sbe-message-set", "An SBE schema must contain at least one message template.", "/messages");
        for (int index = 0; index < plan.Messages.Count; index++)
        {
            if (plan.Messages[index].TemplateId <= 0 || plan.Messages[index].TemplateId > ushort.MaxValue) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "invalid-sbe-template-id", "An SBE template identifier must fit a positive unsigned 16-bit integer.", "/messages/" + plan.Messages[index].Name);
            for (int prior = 0; prior < index; prior++)
            {
                if (plan.Messages[prior].TemplateId == plan.Messages[index].TemplateId) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "ambiguous-sbe-template-id", "Two messages map to the same SBE template identifier.", "/messages/" + plan.Messages[index].Name);
            }
        }
    }

    internal static void ValidateSchemaTypeMappings(SbeMappingPlan plan)
    {
        List<string> emittedNames = ["messageHeader", "groupSizeEncoding"];
        for (int index = 0; index < plan.Types.Count; index++)
        {
            SbeTypeMapping mapping = plan.Types[index];
            string emittedName = SbeSchemaEmitter.ResolveSchemaName(mapping.Name);
            if (ContainsSchemaText(emittedNames, emittedName)) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "ambiguous-sbe-type-name", "Two mapped encodings normalize to the same SBE type name.", "/types/" + mapping.Name);
            else emittedNames.Add(emittedName);
            if (mapping.Name.Length == 0) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "missing-sbe-type-name", "An SBE encoding has no symbolic name.", "/types/" + index.ToString(CultureInfo.InvariantCulture));
            if (mapping.Kind == "enum" && mapping.Values.Count == 0) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "empty-sbe-enum", "An SBE enum must contain at least one valid value.", "/types/" + mapping.Name);
            if (mapping.Kind == "type" && mapping.PrimitiveType.Length == 0 && mapping.ReferenceType.Length == 0) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "missing-sbe-primitive", "An SBE scalar mapping has no primitive or referenced encoding.", "/types/" + mapping.Name);
            if (mapping.Kind == "type" && mapping.PrimitiveType.Length != 0 && !SbeTypeMapper.IsSbePrimitive(mapping.PrimitiveType)) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "unsupported-sbe-primitive", "An SBE scalar mapping uses an unsupported primitive type.", "/types/" + mapping.Name);
            if (mapping.Kind != "type" && mapping.Kind != "enum" && mapping.Kind != "composite") AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "unsupported-sbe-type-kind", "An encoding mapping uses an unsupported SBE type kind.", "/types/" + mapping.Name);
            ValidateSchemaVersionRange(mapping.SinceVersion, mapping.DeprecatedVersion, plan.Version, plan.Diagnostics, "/types/" + mapping.Name);
        }
    }

    internal static void ValidateSchemaFieldMappings(SbeMappingPlan plan)
    {
        for (int index = 0; index < plan.Fields.Count; index++)
        {
            SbeFieldMapping field = plan.Fields[index];
            if (field.Id <= 0 || field.Id > ushort.MaxValue) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "invalid-sbe-field-id", "An SBE field identifier must fit a positive unsigned 16-bit integer.", "/fields/" + field.Name);
            if (FindSchemaType(plan.Types, field.TypeName) == null) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "undefined-sbe-field-type", "A mapped field references an encoding that is not defined.", "/fields/" + field.Name);
            for (int prior = 0; prior < index; prior++)
            {
                if (plan.Fields[prior].Id == field.Id) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "ambiguous-sbe-field-id", "Two canonical fields map to the same SBE field identifier.", "/fields/" + field.Name);
            }
            ValidateSchemaVersionRange(field.SinceVersion, field.DeprecatedVersion, plan.Version, plan.Diagnostics, "/fields/" + field.Name);
        }
    }

    internal static void ValidateSchemaStructures(SbeMappingPlan plan)
    {
        for (int index = 0; index < plan.Components.Count; index++)
        {
            ValidateSchemaMembers(plan, plan.Components[index].Members, "/components/" + plan.Components[index].Name);
            ValidateSchemaVersionRange(plan.Components[index].SinceVersion, plan.Components[index].DeprecatedVersion, plan.Version, plan.Diagnostics, "/components/" + plan.Components[index].Name);
        }
        for (int index = 0; index < plan.Groups.Count; index++)
        {
            if (plan.Groups[index].Id <= 0 || plan.Groups[index].Id > ushort.MaxValue) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "invalid-sbe-group-id", "An SBE group identifier must fit a positive unsigned 16-bit integer.", "/groups/" + plan.Groups[index].Name);
            if (plan.Groups[index].DimensionType != "groupSizeEncoding" && FindSchemaType(plan.Types, plan.Groups[index].DimensionType) == null) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "undefined-sbe-dimension-type", "A repeating group references an undefined dimension encoding.", "/groups/" + plan.Groups[index].Name);
            ValidateSchemaMembers(plan, plan.Groups[index].Members, "/groups/" + plan.Groups[index].Name);
            ValidateSchemaVersionRange(plan.Groups[index].SinceVersion, plan.Groups[index].DeprecatedVersion, plan.Version, plan.Diagnostics, "/groups/" + plan.Groups[index].Name);
        }
        for (int index = 0; index < plan.Messages.Count; index++)
        {
            ValidateSchemaMembers(plan, plan.Messages[index].Members, "/messages/" + plan.Messages[index].Name);
            ValidateSchemaVersionRange(plan.Messages[index].SinceVersion, plan.Messages[index].DeprecatedVersion, plan.Version, plan.Diagnostics, "/messages/" + plan.Messages[index].Name);
        }
    }

    internal static void ValidateSchemaMembers(SbeMappingPlan plan, List<SbeMemberMapping> members, string path)
    {
        List<int> ids = [];
        for (int index = 0; index < members.Count; index++)
        {
            SbeMemberMapping member = members[index];
            if (member.Conditional) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "unsupported-sbe-conditional-layout", "SBE generation cannot represent a conditionally present layout member.", path + "/" + member.Name);
            if (member.Id <= 0 || member.Id > ushort.MaxValue) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "invalid-sbe-member-id", "An SBE member identifier must fit a positive unsigned 16-bit integer.", path + "/" + member.Name);
            if (ContainsSchemaInteger(ids, member.Id)) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "ambiguous-sbe-member-id", "A structure contains duplicate SBE member identifiers.", path + "/" + member.Name);
            else ids.Add(member.Id);
            if (member.Kind == "field" || member.Kind == "data")
            {
                if (FindSchemaType(plan.Types, member.TypeName) == null) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "undefined-sbe-member-type", "A structure member references an undefined SBE encoding.", path + "/" + member.Name);
            }
            else if (member.Kind == "component")
            {
                if (FindSchemaComponent(plan.Components, member.TypeName) == null) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "undefined-sbe-component", "A structure member references an undefined component.", path + "/" + member.Name);
            }
            else if (member.Kind == "group")
            {
                if (FindSchemaGroup(plan.Groups, member.TypeName) == null) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "undefined-sbe-group", "A structure member references an undefined repeating group.", path + "/" + member.Name);
            }
            else AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "unsupported-sbe-member-kind", "A structure contains an unsupported SBE member kind.", path + "/" + member.Name);
            ValidateSchemaVersionRange(member.SinceVersion, member.DeprecatedVersion, plan.Version, plan.Diagnostics, path + "/" + member.Name);
        }
    }

    internal static void ValidateSchemaRecursion(SbeMappingPlan plan)
    {
        List<string> complete = [];
        List<string> active = [];
        for (int index = 0; index < plan.Components.Count; index++) WalkSchemaComponent(plan, plan.Components[index], complete, active);
        for (int index = 0; index < plan.Groups.Count; index++) WalkSchemaGroup(plan, plan.Groups[index], complete, active);
        List<string> completeTypes = [];
        List<string> activeTypes = [];
        for (int index = 0; index < plan.Types.Count; index++) WalkSchemaType(plan, plan.Types[index], completeTypes, activeTypes);
    }

    internal static void WalkSchemaComponent(SbeMappingPlan plan, SbeComponentMapping component, List<string> complete, List<string> active)
    {
        string key = "C:" + component.Name;
        if (ContainsSchemaText(complete, key)) return;
        if (ContainsSchemaText(active, key))
        {
            AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "recursive-sbe-layout", "A component or group reference cycle cannot be represented by SBE.", "/components/" + component.Name);
            return;
        }
        active.Add(key);
        WalkSchemaMemberReferences(plan, component.Members, complete, active);
        RemoveSchemaText(active, key);
        if (!ContainsSchemaText(complete, key)) complete.Add(key);
    }

    internal static void WalkSchemaGroup(SbeMappingPlan plan, SbeGroupMapping group, List<string> complete, List<string> active)
    {
        string key = "G:" + group.Name;
        if (ContainsSchemaText(complete, key)) return;
        if (ContainsSchemaText(active, key))
        {
            AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "recursive-sbe-layout", "A component or group reference cycle cannot be represented by SBE.", "/groups/" + group.Name);
            return;
        }
        active.Add(key);
        WalkSchemaMemberReferences(plan, group.Members, complete, active);
        RemoveSchemaText(active, key);
        if (!ContainsSchemaText(complete, key)) complete.Add(key);
    }

    internal static void WalkSchemaMemberReferences(SbeMappingPlan plan, List<SbeMemberMapping> members, List<string> complete, List<string> active)
    {
        for (int index = 0; index < members.Count; index++)
        {
            if (members[index].Kind == "component")
            {
                SbeComponentMapping? component = FindSchemaComponent(plan.Components, members[index].TypeName);
                if (component != null) WalkSchemaComponent(plan, component, complete, active);
            }
            else if (members[index].Kind == "group")
            {
                SbeGroupMapping? group = FindSchemaGroup(plan.Groups, members[index].TypeName);
                if (group != null) WalkSchemaGroup(plan, group, complete, active);
            }
        }
    }

    internal static void WalkSchemaType(SbeMappingPlan plan, SbeTypeMapping mapping, List<string> complete, List<string> active)
    {
        if (mapping.Kind != "composite") return;
        if (ContainsSchemaText(complete, mapping.Name)) return;
        if (ContainsSchemaText(active, mapping.Name))
        {
            AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "recursive-sbe-type", "A composite encoding reference cycle cannot be represented by SBE.", "/types/" + mapping.Name);
            return;
        }
        active.Add(mapping.Name);
        for (int index = 0; index < mapping.Members.Count; index++)
        {
            SbeTypeMapping? referenced = FindSchemaType(plan.Types, mapping.Members[index].TypeName);
            if (referenced != null && referenced.Kind == "composite") WalkSchemaType(plan, referenced, complete, active);
        }
        RemoveSchemaText(active, mapping.Name);
        if (!ContainsSchemaText(complete, mapping.Name)) complete.Add(mapping.Name);
    }

    internal static void ValidateSchemaPrecision(SbeMappingPlan plan)
    {
        for (int typeIndex = 0; typeIndex < plan.Types.Count; typeIndex++)
        {
            SbeTypeMapping type = plan.Types[typeIndex];
            if (type.Kind != "composite") continue;
            for (int memberIndex = 0; memberIndex < type.Members.Count; memberIndex++)
            {
                SbeMemberMapping member = type.Members[memberIndex];
                if (!string.Equals(member.Name, "exponent", StringComparison.OrdinalIgnoreCase) || member.ConstantValue.Length == 0) continue;
                int exponent = ParseSchemaSignedInteger(member.ConstantValue);
                if (exponent < sbyte.MinValue || exponent > sbyte.MaxValue) AddSchemaDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "unsupported-sbe-precision", "A decimal scale requires an exponent outside the SBE int8 range.", "/types/" + type.Name + "/exponent");
            }
        }
    }

    internal static int ParseSchemaSignedInteger(string value)
    {
        string source = value == null ? "" : value.Trim();
        if (source.Length == 0) return int.MaxValue;
        bool negative = source[0] == '-';
        int start = negative ? 1 : 0;
        if (start == source.Length) return int.MaxValue;
        int result = 0;
        for (int index = start; index < source.Length; index++)
        {
            char character = source[index];
            if (character < '0' || character > '9') return int.MaxValue;
            int digit = character - '0';
            if (result > (int.MaxValue - digit) / 10) return int.MaxValue;
            result = result * 10 + digit;
        }
        return negative ? -result : result;
    }

    internal static void ValidateSchemaVersionRange(int sinceVersion, int deprecatedVersion, int schemaVersion, List<GenerationDiagnostic> diagnostics, string path)
    {
        if (sinceVersion < 0 || sinceVersion > schemaVersion) AddSchemaDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-sbe-since-version", "An element sinceVersion must be between zero and the schema version.", path);
        if (deprecatedVersion < 0 || deprecatedVersion > schemaVersion) AddSchemaDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-sbe-deprecated-version", "An element deprecated version must be between zero and the schema version.", path);
        if (deprecatedVersion != 0 && deprecatedVersion < sinceVersion) AddSchemaDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-sbe-version-range", "An element cannot be deprecated before it was introduced.", path);
    }

    internal static SbeTypeMapping? FindSchemaType(List<SbeTypeMapping> types, string name)
    {
        for (int index = 0; index < types.Count; index++) if (string.Equals(types[index].Name, name, StringComparison.OrdinalIgnoreCase)) return types[index];
        return null;
    }

    internal static SbeComponentMapping? FindSchemaComponent(List<SbeComponentMapping> components, string name)
    {
        for (int index = 0; index < components.Count; index++) if (string.Equals(components[index].Name, name, StringComparison.OrdinalIgnoreCase)) return components[index];
        return null;
    }

    internal static SbeGroupMapping? FindSchemaGroup(List<SbeGroupMapping> groups, string name)
    {
        for (int index = 0; index < groups.Count; index++) if (string.Equals(groups[index].Name, name, StringComparison.OrdinalIgnoreCase)) return groups[index];
        return null;
    }

    internal static bool ContainsSchemaText(List<string> values, string value)
    {
        for (int index = 0; index < values.Count; index++) if (string.Equals(values[index], value, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    internal static void RemoveSchemaText(List<string> values, string value)
    {
        for (int index = values.Count - 1; index >= 0; index--) if (string.Equals(values[index], value, StringComparison.OrdinalIgnoreCase)) values.RemoveAt(index);
    }

    internal static bool ContainsSchemaInteger(List<int> values, int value)
    {
        for (int index = 0; index < values.Count; index++) if (values[index] == value) return true;
        return false;
    }

    internal static void AddSchemaDiagnostic(List<GenerationDiagnostic> diagnostics, ValidationSeverity severity, string code, string message, string source)
    {
        GenerationDiagnostic diagnostic = new GenerationDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Source = source;
        diagnostics.Add(diagnostic);
    }
}
