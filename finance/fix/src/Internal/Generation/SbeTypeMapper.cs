using System;
using System.Collections.Generic;
using System.Globalization;

namespace fix;

internal static class SbeTypeMapper
{
    internal static SbeMappingPlan MapOrchestraRepository(OrchestraRepository repository, SbeGenerationOptions options)
    {
        SbeMappingPlan plan = CreateMappingPlan(options);
        if (repository == null)
        {
            AddTypeDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "missing-sbe-orchestra-repository", "The Orchestra repository to map is missing.", "/");
            return plan;
        }
        if (options == null)
        {
            AddTypeDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "missing-sbe-mapping-options", "The SBE generation options are missing.", "/options");
            return plan;
        }
        ResolveOrchestraByteOrder(repository, plan);
        MapOrchestraDatatypes(repository, options, plan);
        MapOrchestraCodeSets(repository, options, plan);
        MapOrchestraFields(repository, options, plan);
        MapOrchestraComponents(repository, options, plan);
        MapOrchestraGroups(repository, options, plan);
        MapOrchestraMessages(repository, options, plan);
        SortMappingPlan(plan);
        return plan;
    }

    internal static SbeMappingPlan MapDictionaryTypes(FixDictionary dictionary, SbeGenerationOptions options)
    {
        SbeMappingPlan plan = CreateMappingPlan(options);
        if (dictionary == null)
        {
            AddTypeDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "missing-sbe-dictionary", "The canonical dictionary to map is missing.", "/");
            return plan;
        }
        if (options == null)
        {
            AddTypeDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "missing-sbe-mapping-options", "The SBE generation options are missing.", "/options");
            return plan;
        }
        MapDictionaryDatatypes(dictionary, options, plan);
        MapDictionaryCodeSets(dictionary, options, plan);
        MapDictionaryFields(dictionary, options, plan);
        MapDictionaryComponents(dictionary, options, plan);
        MapDictionaryGroups(dictionary, options, plan);
        MapDictionaryMessages(dictionary, options, plan);
        SortMappingPlan(plan);
        return plan;
    }

    internal static SbeMappingPlan CreateMappingPlan(SbeGenerationOptions? options)
    {
        SbeMappingPlan plan = new SbeMappingPlan();
        if (options == null) return plan;
        plan.Package = options.Package == null ? "" : options.Package.Trim();
        plan.SchemaId = options.SchemaId;
        plan.Version = options.Version;
        plan.ByteOrder = options.LittleEndian ? "littleEndian" : "bigEndian";
        if (options.SchemaId < 0) AddTypeDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "invalid-sbe-schema-id", "The SBE schema identifier must not be negative.", "/options/schema-id");
        if (options.Version < 0) AddTypeDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "invalid-sbe-schema-version", "The SBE schema version must not be negative.", "/options/version");
        if (options.FirstTemplateId <= 0) AddTypeDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "invalid-sbe-template-id", "The first SBE template identifier must be positive.", "/options/first-template-id");
        return plan;
    }

    internal static void ResolveOrchestraByteOrder(OrchestraRepository repository, SbeMappingPlan plan)
    {
        for (int index = 0; index < repository.EncodingStandards.Count; index++)
        {
            OrchestraEncodingStandard standard = repository.EncodingStandards[index];
            if (!IsSbeStandard(standard.Name) || standard.Encoding == null) continue;
            string byteOrder = FindEncodingAttribute(standard.Encoding, "byteOrder");
            if (byteOrder.Length == 0) continue;
            if (string.Equals(byteOrder, "littleEndian", StringComparison.OrdinalIgnoreCase)) plan.ByteOrder = "littleEndian";
            else if (string.Equals(byteOrder, "bigEndian", StringComparison.OrdinalIgnoreCase)) plan.ByteOrder = "bigEndian";
            else AddTypeDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "invalid-sbe-byte-order", "The Orchestra SBE byte order must be littleEndian or bigEndian.", "/encoding-standards/" + standard.Name);
        }
    }

    internal static void MapOrchestraDatatypes(OrchestraRepository repository, SbeGenerationOptions options, SbeMappingPlan plan)
    {
        List<OrchestraDatatypeDefinition> definitions = new List<OrchestraDatatypeDefinition>(repository.Datatypes);
        definitions.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        for (int index = 0; index < definitions.Count; index++)
        {
            OrchestraDatatypeDefinition definition = definitions[index];
            OrchestraDatatypeMapping? explicitMapping = SelectOrchestraSbeMapping(definition, plan.Diagnostics);
            SbeTypeMapping mapping = CreateOrchestraDatatypeMapping(definition, explicitMapping, options, plan.Diagnostics);
            if (!options.EmitSemanticTypes) mapping.SemanticType = "";
            plan.Types.Add(mapping);
        }
    }

    internal static OrchestraDatatypeMapping? SelectOrchestraSbeMapping(OrchestraDatatypeDefinition definition, List<GenerationDiagnostic> diagnostics)
    {
        OrchestraDatatypeMapping? result = null;
        int count = 0;
        for (int index = 0; index < definition.Mappings.Count; index++)
        {
            if (IsSbeStandard(definition.Mappings[index].Standard))
            {
                if (result == null) result = definition.Mappings[index];
                count++;
            }
        }
        if (count > 1) AddTypeDiagnostic(diagnostics, ValidationSeverity.Error, "ambiguous-sbe-datatype-mapping", "A datatype has more than one SBE physical mapping.", "/datatypes/" + definition.Name);
        return result;
    }

    internal static SbeTypeMapping CreateOrchestraDatatypeMapping(OrchestraDatatypeDefinition definition, OrchestraDatatypeMapping? explicitMapping, SbeGenerationOptions options, List<GenerationDiagnostic> diagnostics)
    {
        SbeTypeMapping result = new SbeTypeMapping();
        result.Name = definition.Name;
        result.SemanticType = options.EmitSemanticTypes ? definition.Name : "";
        ApplyPedigreeToType(result, definition.Pedigree);
        string baseType = explicitMapping == null ? "" : explicitMapping.Base;
        if (explicitMapping != null)
        {
            string primitive = explicitMapping.Encoding == null ? "" : FindEncodingAttribute(explicitMapping.Encoding, "primitiveType");
            if (primitive.Length != 0) baseType = primitive;
            result.MinimumValue = explicitMapping.MinimumInclusive;
            result.MaximumValue = explicitMapping.MaximumInclusive;
            int size = ParseNonnegativeInteger(explicitMapping.Size);
            if (size >= 0 && explicitMapping.Size.Length != 0)
            {
                result.HasLength = true;
                result.Length = size;
            }
            else if (explicitMapping.Size.Length != 0) AddTypeDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-sbe-type-length", "An SBE datatype size must be a nonnegative integer.", "/datatypes/" + definition.Name + "/size");
            if (explicitMapping.Encoding != null) ApplyEncodingAttributesToType(result, explicitMapping.Encoding, diagnostics, "/datatypes/" + definition.Name);
        }

        FixFieldType logicalType = ResolveLogicalFieldType(definition.Name, definition.BaseType);
        if (IsDecimalFieldType(logicalType) || string.Equals(definition.BaseType, "Decimal", StringComparison.OrdinalIgnoreCase))
        {
            CreateDecimalComposite(result, baseType, explicitMapping == null ? "" : explicitMapping.Parameter);
        }
        else if (logicalType == FixFieldType.MonthYear)
        {
            CreateMonthYearComposite(result);
        }
        else
        {
            string primitive = IsSbePrimitive(baseType) ? NormalizePrimitiveName(baseType) : ResolvePrimitiveType(logicalType);
            if (primitive.Length != 0)
            {
                result.PrimitiveType = primitive;
                result.Kind = "type";
            }
            else if (baseType.Length != 0)
            {
                result.ReferenceType = baseType;
                result.Kind = "type";
            }
            else
            {
                result.PrimitiveType = "char";
                AddTypeDiagnostic(diagnostics, ValidationSeverity.Warning, "fallback-sbe-primitive", "A logical datatype used the portable char fallback because no SBE primitive mapping was supplied.", "/datatypes/" + definition.Name);
            }
            ApplyStringAndDataShape(result, logicalType);
        }
        return result;
    }

    internal static void MapOrchestraCodeSets(OrchestraRepository repository, SbeGenerationOptions options, SbeMappingPlan plan)
    {
        List<OrchestraCodeSetDefinition> definitions = new List<OrchestraCodeSetDefinition>(repository.CodeSets);
        definitions.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        for (int index = 0; index < definitions.Count; index++)
        {
            OrchestraCodeSetDefinition definition = definitions[index];
            SbeTypeMapping mapping = new SbeTypeMapping();
            mapping.Name = definition.Name;
            mapping.Kind = "enum";
            mapping.SemanticType = options.EmitSemanticTypes ? definition.Type : "";
            mapping.PrimitiveType = ResolveOrchestraCodePrimitive(repository, definition.Type);
            if (mapping.PrimitiveType.Length == 0) mapping.PrimitiveType = "char";
            ApplyPedigreeToType(mapping, definition.Pedigree);
            OrchestraEncoding? encoding = SelectSbeEncodingOrDefault(definition.Encodings, repository.CodeSetEncodings);
            if (encoding != null) ApplyEncodingAttributesToType(mapping, encoding, plan.Diagnostics, "/code-sets/" + definition.Name);
            if (!options.EmitSemanticTypes) mapping.SemanticType = "";
            List<OrchestraCodeDefinition> codes = new List<OrchestraCodeDefinition>(definition.Codes);
            codes.Sort((left, right) => CompareOrchestraCodes(left, right));
            for (int codeIndex = 0; codeIndex < codes.Count; codeIndex++)
            {
                SbeValueMapping value = new SbeValueMapping();
                value.Name = codes[codeIndex].Name;
                value.Value = codes[codeIndex].Value;
                value.SinceVersion = ResolveSinceVersion(codes[codeIndex].Pedigree);
                value.DeprecatedVersion = ResolveDeprecatedVersion(codes[codeIndex].Pedigree);
                mapping.Values.Add(value);
            }
            plan.Types.Add(mapping);
        }
    }

    internal static void MapOrchestraFields(OrchestraRepository repository, SbeGenerationOptions options, SbeMappingPlan plan)
    {
        List<OrchestraFieldDefinition> definitions = new List<OrchestraFieldDefinition>(repository.Fields);
        definitions.Sort((left, right) => CompareOrchestraFields(left, right));
        for (int index = 0; index < definitions.Count; index++)
        {
            OrchestraFieldDefinition definition = definitions[index];
            SbeFieldMapping field = new SbeFieldMapping();
            field.SourceId = ParseRequiredPositiveId(definition.Id, "field", definition.Name, plan.Diagnostics);
            field.Id = field.SourceId;
            field.Name = definition.Name;
            field.TypeName = definition.CodeSet.Length == 0 ? definition.Type : definition.CodeSet;
            field.SemanticType = options.EmitSemanticTypes ? definition.Type : "";
            field.Presence = ResolvePresence(definition.Presence, definition.Value);
            field.ConstantValue = field.Presence == "constant" ? definition.Value : "";
            field.SinceVersion = ResolveSinceVersion(definition.Pedigree);
            field.DeprecatedVersion = ResolveDeprecatedVersion(definition.Pedigree);
            OrchestraEncoding? encoding = SelectSbeEncoding(definition.Encodings);
            if (encoding != null) ApplyEncodingAttributesToField(field, encoding, plan.Diagnostics, "/fields/" + definition.Id);
            if (!options.EmitSemanticTypes) field.SemanticType = "";
            SbeTypeMapping? type = FindMappedType(plan.Types, field.TypeName);
            if (type != null)
            {
                field.Kind = type.VariableLength ? "data" : "field";
                if (field.NullValue.Length == 0) field.NullValue = type.NullValue;
            }
            else AddTypeDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "unresolved-sbe-field-type", "An Orchestra field references a datatype or code set that was not mapped.", "/fields/" + definition.Id);
            plan.Fields.Add(field);
        }
    }

    internal static void MapOrchestraComponents(OrchestraRepository repository, SbeGenerationOptions options, SbeMappingPlan plan)
    {
        List<OrchestraComponentDefinition> definitions = new List<OrchestraComponentDefinition>(repository.Components);
        definitions.Sort((left, right) => CompareOrchestraComponents(left, right));
        for (int index = 0; index < definitions.Count; index++)
        {
            OrchestraComponentDefinition definition = definitions[index];
            SbeComponentMapping component = new SbeComponentMapping();
            component.Id = ParseRequiredPositiveId(definition.Id, "component", definition.Name, plan.Diagnostics);
            component.Name = definition.Name;
            component.SinceVersion = ResolveSinceVersion(definition.Pedigree);
            component.DeprecatedVersion = ResolveDeprecatedVersion(definition.Pedigree);
            OrchestraEncoding? encoding = SelectSbeEncodingOrDefault(definition.Encodings, repository.ComponentEncodings);
            if (encoding != null) ApplyEncodingVersionsToComponent(component, encoding);
            MapOrchestraMembers(repository, definition.Members, options, plan, component.Members, "/components/" + definition.Id);
            plan.Components.Add(component);

            SbeTypeMapping composite = new SbeTypeMapping();
            composite.Name = definition.Name;
            composite.Kind = "composite";
            composite.SemanticType = options.EmitSemanticTypes ? definition.Name : "";
            composite.SinceVersion = component.SinceVersion;
            composite.DeprecatedVersion = component.DeprecatedVersion;
            CopyMappedMembers(component.Members, composite.Members);
            plan.Types.Add(composite);
        }
    }

    internal static void MapOrchestraGroups(OrchestraRepository repository, SbeGenerationOptions options, SbeMappingPlan plan)
    {
        List<OrchestraGroupDefinition> definitions = new List<OrchestraGroupDefinition>(repository.Groups);
        definitions.Sort((left, right) => CompareOrchestraGroups(left, right));
        for (int index = 0; index < definitions.Count; index++)
        {
            OrchestraGroupDefinition definition = definitions[index];
            SbeGroupMapping group = new SbeGroupMapping();
            group.Id = ParseRequiredPositiveId(definition.Id, "group", definition.Name, plan.Diagnostics);
            if (definition.CountMember != null)
            {
                int countId = ParseOptionalPositiveInteger(definition.CountMember.ReferenceId);
                if (countId > 0) group.Id = countId;
            }
            group.Name = definition.Name;
            group.SinceVersion = ResolveSinceVersion(definition.Pedigree);
            group.DeprecatedVersion = ResolveDeprecatedVersion(definition.Pedigree);
            OrchestraEncoding? encoding = SelectSbeEncodingOrDefault(definition.Encodings, repository.GroupEncodings);
            if (encoding != null)
            {
                string dimensionType = FindEncodingAttribute(encoding, "dimensionType");
                if (dimensionType.Length != 0) group.DimensionType = dimensionType;
                ApplyEncodingVersionsToGroup(group, encoding);
            }
            MapOrchestraMembers(repository, definition.Members, options, plan, group.Members, "/groups/" + definition.Id);
            plan.Groups.Add(group);
        }
    }

    internal static void MapOrchestraMessages(OrchestraRepository repository, SbeGenerationOptions options, SbeMappingPlan plan)
    {
        List<OrchestraMessageDefinition> definitions = SelectOrchestraMessages(repository, options.Scenario);
        for (int index = 0; index < definitions.Count; index++)
        {
            OrchestraMessageDefinition definition = definitions[index];
            SbeMessageMapping message = new SbeMessageMapping();
            message.SourceId = ParseOptionalPositiveInteger(definition.Id);
            message.TemplateId = options.FirstTemplateId + index;
            message.Name = definition.Name;
            message.MessageType = definition.MessageType;
            message.SemanticType = options.EmitSemanticTypes ? definition.MessageType : "";
            message.SinceVersion = ResolveSinceVersion(definition.Pedigree);
            message.DeprecatedVersion = ResolveDeprecatedVersion(definition.Pedigree);
            OrchestraEncoding? encoding = SelectSbeEncodingOrDefault(definition.Encodings, repository.MessageEncodings);
            if (encoding != null)
            {
                int explicitId = ParseNonnegativeInteger(FindEncodingAttribute(encoding, "id"));
                if (explicitId > 0) message.TemplateId = explicitId;
                else if (FindEncodingAttribute(encoding, "id").Length != 0) AddTypeDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "invalid-sbe-template-id", "An Orchestra SBE template identifier must be positive.", "/messages/" + definition.Id);
                ApplyEncodingVersionsToMessage(message, encoding);
            }
            MapOrchestraMembers(repository, definition.Members, options, plan, message.Members, "/messages/" + definition.Id);
            plan.Messages.Add(message);
        }
        AddDuplicateTemplateDiagnostics(plan);
    }

    internal static void MapOrchestraMembers(OrchestraRepository repository, List<OrchestraMemberDefinition> source, SbeGenerationOptions options, SbeMappingPlan plan, List<SbeMemberMapping> destination, string path)
    {
        List<OrchestraMemberDefinition> definitions = new List<OrchestraMemberDefinition>(source);
        definitions.Sort((left, right) => CompareOrchestraMembers(left, right));
        for (int index = 0; index < definitions.Count; index++)
        {
            OrchestraMemberDefinition definition = definitions[index];
            SbeMemberMapping member = new SbeMemberMapping();
            member.Position = definition.Position;
            member.Id = ParseRequiredPositiveId(definition.ReferenceId, "member", definition.InstanceName, plan.Diagnostics);
            member.Name = ResolveOrchestraMemberName(repository, definition);
            member.Kind = ResolveOrchestraMemberKind(definition.Kind);
            member.TypeName = ResolveOrchestraMemberType(repository, definition);
            member.SemanticType = options.EmitSemanticTypes ? ResolveOrchestraMemberSemanticType(repository, definition) : "";
            member.Presence = ResolvePresence(definition.Presence, definition.Value);
            member.Conditional = definition.Presence == FixPresence.Conditional || definition.Rules.Count != 0;
            member.ConstantValue = member.Presence == "constant" ? definition.Value : "";
            member.SinceVersion = ResolveSinceVersion(definition.Pedigree);
            member.DeprecatedVersion = ResolveDeprecatedVersion(definition.Pedigree);
            if (member.Kind == "field")
            {
                SbeFieldMapping? field = FindMappedFieldBySourceReference(plan.Fields, definition.ReferenceId);
                if (field != null)
                {
                    member.Id = field.Id;
                    member.Kind = field.Kind;
                    if (member.NullValue.Length == 0) member.NullValue = field.NullValue;
                }
            }
            else if (member.Kind == "group")
            {
                OrchestraGroupDefinition? group = FindOrchestraGroup(repository.Groups, definition.ReferenceId);
                if (group != null && group.CountMember != null)
                {
                    int countId = ParseOptionalPositiveInteger(group.CountMember.ReferenceId);
                    if (countId > 0) member.Id = countId;
                }
            }
            OrchestraEncoding? encoding = SelectSbeEncoding(definition.Encodings);
            if (encoding != null) ApplyEncodingAttributesToMember(member, encoding, plan.Diagnostics, path + "/members/" + index.ToString(CultureInfo.InvariantCulture));
            if (!options.EmitSemanticTypes) member.SemanticType = "";
            if (member.TypeName.Length == 0) AddTypeDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "unresolved-sbe-member", "An Orchestra structure member references a definition that was not mapped.", path + "/members/" + index.ToString(CultureInfo.InvariantCulture));
            destination.Add(member);
        }
    }

    internal static void MapDictionaryDatatypes(FixDictionary dictionary, SbeGenerationOptions options, SbeMappingPlan plan)
    {
        List<FixDatatypeDefinition> definitions = new List<FixDatatypeDefinition>(dictionary.Datatypes);
        definitions.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        for (int index = 0; index < definitions.Count; index++)
        {
            FixDatatypeDefinition definition = definitions[index];
            SbeTypeMapping mapping = new SbeTypeMapping();
            mapping.Name = definition.Name;
            mapping.SemanticType = options.EmitSemanticTypes ? definition.Name : "";
            if (IsDecimalFieldType(definition.FieldType)) CreateDecimalComposite(mapping, "int64", "");
            else if (definition.FieldType == FixFieldType.MonthYear) CreateMonthYearComposite(mapping);
            else
            {
                mapping.PrimitiveType = ResolvePrimitiveType(definition.FieldType);
                if (mapping.PrimitiveType.Length == 0 && definition.BaseTypeName.Length != 0) mapping.ReferenceType = definition.BaseTypeName;
                if (mapping.PrimitiveType.Length == 0 && mapping.ReferenceType.Length == 0) mapping.PrimitiveType = "char";
                ApplyStringAndDataShape(mapping, definition.FieldType);
            }
            plan.Types.Add(mapping);
        }
    }

    internal static void MapDictionaryCodeSets(FixDictionary dictionary, SbeGenerationOptions options, SbeMappingPlan plan)
    {
        List<FixCodeSetDefinition> definitions = new List<FixCodeSetDefinition>(dictionary.CodeSets);
        definitions.Sort((left, right) => CSharpNameResolver.CompareCodeSetDefinitions(left, right));
        for (int index = 0; index < definitions.Count; index++)
        {
            FixCodeSetDefinition definition = definitions[index];
            SbeTypeMapping mapping = new SbeTypeMapping();
            mapping.Name = definition.Name;
            mapping.Kind = "enum";
            mapping.PrimitiveType = ResolvePrimitiveType(definition.FieldType);
            if (mapping.PrimitiveType.Length == 0) mapping.PrimitiveType = "char";
            mapping.SemanticType = options.EmitSemanticTypes ? CanonicalSemanticType(definition.FieldType) : "";
            List<FixCodeDefinition> codes = new List<FixCodeDefinition>(definition.Codes);
            codes.Sort((left, right) => CSharpNameResolver.CompareCodeDefinitions(left, right));
            for (int codeIndex = 0; codeIndex < codes.Count; codeIndex++)
            {
                SbeValueMapping value = new SbeValueMapping();
                value.Name = codes[codeIndex].Name;
                value.Value = codes[codeIndex].Value;
                mapping.Values.Add(value);
            }
            plan.Types.Add(mapping);
        }
    }

    internal static void MapDictionaryFields(FixDictionary dictionary, SbeGenerationOptions options, SbeMappingPlan plan)
    {
        List<FixFieldDefinition> definitions = new List<FixFieldDefinition>(dictionary.Fields);
        definitions.Sort((left, right) => CSharpNameResolver.CompareFieldDefinitions(left, right));
        for (int index = 0; index < definitions.Count; index++)
        {
            FixFieldDefinition definition = definitions[index];
            SbeFieldMapping field = new SbeFieldMapping();
            field.SourceId = definition.Tag;
            field.Id = definition.Tag;
            field.Name = definition.Name;
            field.TypeName = definition.CodeSetName.Length == 0 ? ResolveDictionaryFieldTypeName(definition) : definition.CodeSetName;
            field.SemanticType = options.EmitSemanticTypes ? CanonicalSemanticType(definition.FieldType) : "";
            SbeTypeMapping? type = FindMappedType(plan.Types, field.TypeName);
            field.Kind = type != null && type.VariableLength ? "data" : "field";
            if (type == null) AddTypeDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "unresolved-sbe-field-type", "A canonical field references a datatype or code set that was not mapped.", "/fields/" + definition.Tag.ToString(CultureInfo.InvariantCulture));
            plan.Fields.Add(field);
        }
    }

    internal static void MapDictionaryComponents(FixDictionary dictionary, SbeGenerationOptions options, SbeMappingPlan plan)
    {
        List<FixComponentDefinition> definitions = new List<FixComponentDefinition>(dictionary.Components);
        definitions.Sort((left, right) => CSharpNameResolver.CompareComponentDefinitions(left, right));
        for (int index = 0; index < definitions.Count; index++)
        {
            SbeComponentMapping component = new SbeComponentMapping();
            component.Id = definitions[index].Id;
            component.Name = definitions[index].Name;
            MapDictionaryMembers(dictionary, definitions[index].Members, options, plan, component.Members, "/components/" + definitions[index].Id.ToString(CultureInfo.InvariantCulture));
            plan.Components.Add(component);
            SbeTypeMapping composite = new SbeTypeMapping();
            composite.Name = component.Name;
            composite.Kind = "composite";
            composite.SemanticType = options.EmitSemanticTypes ? component.Name : "";
            CopyMappedMembers(component.Members, composite.Members);
            plan.Types.Add(composite);
        }
    }

    internal static void MapDictionaryGroups(FixDictionary dictionary, SbeGenerationOptions options, SbeMappingPlan plan)
    {
        List<FixGroupDefinition> definitions = new List<FixGroupDefinition>(dictionary.Groups);
        definitions.Sort((left, right) => CSharpNameResolver.CompareGroupDefinitions(left, right));
        for (int index = 0; index < definitions.Count; index++)
        {
            SbeGroupMapping group = new SbeGroupMapping();
            group.Id = definitions[index].CountTag > 0 ? definitions[index].CountTag : definitions[index].Id;
            group.Name = definitions[index].Name;
            MapDictionaryMembers(dictionary, definitions[index].Members, options, plan, group.Members, "/groups/" + definitions[index].Id.ToString(CultureInfo.InvariantCulture));
            plan.Groups.Add(group);
        }
    }

    internal static void MapDictionaryMessages(FixDictionary dictionary, SbeGenerationOptions options, SbeMappingPlan plan)
    {
        List<FixMessageDefinition> definitions = SelectDictionaryMessages(dictionary, options.Scenario);
        for (int index = 0; index < definitions.Count; index++)
        {
            SbeMessageMapping message = new SbeMessageMapping();
            message.SourceId = definitions[index].Id;
            message.TemplateId = options.FirstTemplateId + index;
            message.Name = definitions[index].Name;
            message.MessageType = definitions[index].MsgType;
            message.SemanticType = options.EmitSemanticTypes ? definitions[index].MsgType : "";
            MapDictionaryMembers(dictionary, definitions[index].Members, options, plan, message.Members, "/messages/" + definitions[index].MsgType);
            plan.Messages.Add(message);
        }
    }

    internal static void MapDictionaryMembers(FixDictionary dictionary, List<FixMemberDefinition> source, SbeGenerationOptions options, SbeMappingPlan plan, List<SbeMemberMapping> destination, string path)
    {
        List<FixMemberDefinition> definitions = new List<FixMemberDefinition>(source);
        definitions.Sort((left, right) => CSharpEmitter.CompareMemberDefinitions(left, right));
        for (int index = 0; index < definitions.Count; index++)
        {
            FixMemberDefinition definition = definitions[index];
            SbeMemberMapping member = new SbeMemberMapping();
            member.Position = definition.Position;
            member.Id = definition.ReferenceId;
            member.Name = definition.Name;
            member.Presence = ResolvePresence(definition.Presence, "");
            member.Conditional = definition.Presence == FixPresence.Conditional || definition.PresenceRule.Length != 0;
            if (definition.Kind == FixMemberKind.Field)
            {
                SbeFieldMapping? field = FindMappedField(plan.Fields, definition.ReferenceId);
                if (field != null)
                {
                    member.Kind = field.Kind;
                    member.TypeName = field.TypeName;
                    member.SemanticType = options.EmitSemanticTypes ? field.SemanticType : "";
                }
            }
            else if (definition.Kind == FixMemberKind.Component)
            {
                member.Kind = "component";
                FixComponentDefinition? component = FindDictionaryComponent(dictionary.Components, definition.ReferenceId);
                if (component != null) member.TypeName = component.Name;
            }
            else if (definition.Kind == FixMemberKind.Group)
            {
                member.Kind = "group";
                FixGroupDefinition? group = FindDictionaryGroup(dictionary.Groups, definition.ReferenceId);
                if (group != null)
                {
                    member.TypeName = group.Name;
                    member.Id = group.CountTag > 0 ? group.CountTag : group.Id;
                }
            }
            if (member.TypeName.Length == 0) AddTypeDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "unresolved-sbe-member", "A canonical structure member references a definition that was not mapped.", path + "/members/" + index.ToString(CultureInfo.InvariantCulture));
            destination.Add(member);
        }
    }

    internal static void CreateDecimalComposite(SbeTypeMapping mapping, string primitive, string parameter)
    {
        mapping.Kind = "composite";
        mapping.PrimitiveType = "";
        SbeMemberMapping mantissa = new SbeMemberMapping();
        mantissa.Position = 0;
        mantissa.Name = "mantissa";
        mantissa.Kind = "type";
        mantissa.TypeName = IsSbePrimitive(primitive) ? NormalizePrimitiveName(primitive) : "int64";
        mapping.Members.Add(mantissa);
        SbeMemberMapping exponent = new SbeMemberMapping();
        exponent.Position = 1;
        exponent.Name = "exponent";
        exponent.Kind = "type";
        exponent.TypeName = "int8";
        exponent.Presence = "constant";
        exponent.ConstantValue = ResolveScaleExponent(parameter);
        mapping.Members.Add(exponent);
    }

    internal static void CreateMonthYearComposite(SbeTypeMapping mapping)
    {
        mapping.Kind = "composite";
        AddCompositePrimitiveMember(mapping.Members, "year", "uint16", 0);
        AddCompositePrimitiveMember(mapping.Members, "month", "uint8", 1);
        AddCompositePrimitiveMember(mapping.Members, "day", "uint8", 2);
        AddCompositePrimitiveMember(mapping.Members, "week", "uint8", 3);
    }

    internal static void AddCompositePrimitiveMember(List<SbeMemberMapping> members, string name, string primitive, int position)
    {
        SbeMemberMapping member = new SbeMemberMapping();
        member.Position = position;
        member.Name = name;
        member.Kind = "type";
        member.TypeName = primitive;
        members.Add(member);
    }

    internal static void ApplyStringAndDataShape(SbeTypeMapping mapping, FixFieldType type)
    {
        bool textual = type == FixFieldType.String || type == FixFieldType.MultipleCharValue || type == FixFieldType.MultipleStringValue || type == FixFieldType.Currency || type == FixFieldType.Country || type == FixFieldType.Exchange || type == FixFieldType.TzTimeOnly || type == FixFieldType.TzTimestamp;
        bool binary = type == FixFieldType.Data || type == FixFieldType.XmlData;
        if ((textual || binary) && !mapping.HasLength)
        {
            mapping.VariableLength = true;
            mapping.Length = 0;
            mapping.HasLength = true;
        }
        if (binary) mapping.PrimitiveType = "uint8";
        else if (textual) mapping.PrimitiveType = "char";
    }

    internal static void ApplyEncodingAttributesToType(SbeTypeMapping mapping, OrchestraEncoding encoding, List<GenerationDiagnostic> diagnostics, string path)
    {
        string nullValue = FindEncodingAttribute(encoding, "nullValue");
        if (nullValue.Length != 0) mapping.NullValue = nullValue;
        string semanticType = FindEncodingAttribute(encoding, "semanticType");
        if (semanticType.Length != 0) mapping.SemanticType = semanticType;
        string presence = FindEncodingAttribute(encoding, "presence");
        if (presence.Length != 0) mapping.Presence = NormalizeSbePresence(presence, diagnostics, path);
        string constant = FindEncodingAttribute(encoding, "value");
        if (constant.Length != 0)
        {
            mapping.Presence = "constant";
            mapping.ConstantValue = constant;
        }
        string variable = FindEncodingAttribute(encoding, "variableLength");
        if (variable.Length != 0) mapping.VariableLength = IsTrueText(variable);
        string length = FindEncodingAttribute(encoding, "length");
        if (length.Length != 0)
        {
            int parsed = ParseNonnegativeInteger(length);
            if (parsed < 0) AddTypeDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-sbe-type-length", "An SBE type length must be a nonnegative integer.", path);
            else
            {
                mapping.HasLength = true;
                mapping.Length = parsed;
                mapping.VariableLength = parsed == 0;
            }
        }
        int since = ParseNonnegativeInteger(FindEncodingAttribute(encoding, "sinceVersion"));
        if (since >= 0 && FindEncodingAttribute(encoding, "sinceVersion").Length != 0) mapping.SinceVersion = since;
        int deprecated = ParseNonnegativeInteger(FindEncodingAttribute(encoding, "deprecated"));
        if (deprecated >= 0 && FindEncodingAttribute(encoding, "deprecated").Length != 0) mapping.DeprecatedVersion = deprecated;
    }

    internal static void ApplyEncodingAttributesToField(SbeFieldMapping field, OrchestraEncoding encoding, List<GenerationDiagnostic> diagnostics, string path)
    {
        string id = FindEncodingAttribute(encoding, "id");
        if (id.Length != 0)
        {
            int parsed = ParseNonnegativeInteger(id);
            if (parsed <= 0) AddTypeDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-sbe-field-id", "An SBE field identifier must be positive.", path);
            else field.Id = parsed;
        }
        string nullValue = FindEncodingAttribute(encoding, "nullValue");
        if (nullValue.Length != 0) field.NullValue = nullValue;
        string semantic = FindEncodingAttribute(encoding, "semanticType");
        if (semantic.Length != 0) field.SemanticType = semantic;
        string presence = FindEncodingAttribute(encoding, "presence");
        if (presence.Length != 0) field.Presence = NormalizeSbePresence(presence, diagnostics, path);
        string constant = FindEncodingAttribute(encoding, "value");
        if (constant.Length != 0)
        {
            field.Presence = "constant";
            field.ConstantValue = constant;
        }
        ApplyEncodingVersionsToField(field, encoding);
    }

    internal static void ApplyEncodingAttributesToMember(SbeMemberMapping member, OrchestraEncoding encoding, List<GenerationDiagnostic> diagnostics, string path)
    {
        string id = FindEncodingAttribute(encoding, "id");
        if (id.Length != 0)
        {
            int parsed = ParseNonnegativeInteger(id);
            if (parsed <= 0) AddTypeDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-sbe-member-id", "An SBE member identifier must be positive.", path);
            else member.Id = parsed;
        }
        string nullValue = FindEncodingAttribute(encoding, "nullValue");
        if (nullValue.Length != 0) member.NullValue = nullValue;
        string semantic = FindEncodingAttribute(encoding, "semanticType");
        if (semantic.Length != 0) member.SemanticType = semantic;
        string presence = FindEncodingAttribute(encoding, "presence");
        if (presence.Length != 0) member.Presence = NormalizeSbePresence(presence, diagnostics, path);
        string constant = FindEncodingAttribute(encoding, "value");
        if (constant.Length != 0)
        {
            member.Presence = "constant";
            member.ConstantValue = constant;
        }
        int since = ParseNonnegativeInteger(FindEncodingAttribute(encoding, "sinceVersion"));
        if (since >= 0 && FindEncodingAttribute(encoding, "sinceVersion").Length != 0) member.SinceVersion = since;
        int deprecated = ParseNonnegativeInteger(FindEncodingAttribute(encoding, "deprecated"));
        if (deprecated >= 0 && FindEncodingAttribute(encoding, "deprecated").Length != 0) member.DeprecatedVersion = deprecated;
    }

    internal static void ApplyEncodingVersionsToField(SbeFieldMapping field, OrchestraEncoding encoding)
    {
        int since = ParseNonnegativeInteger(FindEncodingAttribute(encoding, "sinceVersion"));
        if (since >= 0 && FindEncodingAttribute(encoding, "sinceVersion").Length != 0) field.SinceVersion = since;
        int deprecated = ParseNonnegativeInteger(FindEncodingAttribute(encoding, "deprecated"));
        if (deprecated >= 0 && FindEncodingAttribute(encoding, "deprecated").Length != 0) field.DeprecatedVersion = deprecated;
    }

    internal static void ApplyEncodingVersionsToComponent(SbeComponentMapping component, OrchestraEncoding encoding)
    {
        int since = ParseNonnegativeInteger(FindEncodingAttribute(encoding, "sinceVersion"));
        if (since >= 0 && FindEncodingAttribute(encoding, "sinceVersion").Length != 0) component.SinceVersion = since;
        int deprecated = ParseNonnegativeInteger(FindEncodingAttribute(encoding, "deprecated"));
        if (deprecated >= 0 && FindEncodingAttribute(encoding, "deprecated").Length != 0) component.DeprecatedVersion = deprecated;
    }

    internal static void ApplyEncodingVersionsToGroup(SbeGroupMapping group, OrchestraEncoding encoding)
    {
        int since = ParseNonnegativeInteger(FindEncodingAttribute(encoding, "sinceVersion"));
        if (since >= 0 && FindEncodingAttribute(encoding, "sinceVersion").Length != 0) group.SinceVersion = since;
        int deprecated = ParseNonnegativeInteger(FindEncodingAttribute(encoding, "deprecated"));
        if (deprecated >= 0 && FindEncodingAttribute(encoding, "deprecated").Length != 0) group.DeprecatedVersion = deprecated;
    }

    internal static void ApplyEncodingVersionsToMessage(SbeMessageMapping message, OrchestraEncoding encoding)
    {
        int since = ParseNonnegativeInteger(FindEncodingAttribute(encoding, "sinceVersion"));
        if (since >= 0 && FindEncodingAttribute(encoding, "sinceVersion").Length != 0) message.SinceVersion = since;
        int deprecated = ParseNonnegativeInteger(FindEncodingAttribute(encoding, "deprecated"));
        if (deprecated >= 0 && FindEncodingAttribute(encoding, "deprecated").Length != 0) message.DeprecatedVersion = deprecated;
    }

    internal static string FindEncodingAttribute(OrchestraEncoding encoding, string name)
    {
        foreach (KeyValuePair<string, string> item in encoding.Attributes)
        {
            if (AttributeLocalName(item.Key) == name || string.Equals(AttributeLocalName(item.Key), name, StringComparison.OrdinalIgnoreCase)) return item.Value;
        }
        for (int index = 0; index < encoding.Extensions.Count; index++)
        {
            if (string.Equals(encoding.Extensions[index].Name, name, StringComparison.OrdinalIgnoreCase)) return ReadExtensionText(encoding.Extensions[index].Xml);
        }
        for (int annotationIndex = 0; annotationIndex < encoding.Annotations.Count; annotationIndex++)
        {
            for (int extensionIndex = 0; extensionIndex < encoding.Annotations[annotationIndex].Extensions.Count; extensionIndex++)
            {
                OrchestraExtension extension = encoding.Annotations[annotationIndex].Extensions[extensionIndex];
                if (string.Equals(extension.Name, name, StringComparison.OrdinalIgnoreCase)) return ReadExtensionText(extension.Xml);
            }
        }
        return "";
    }

    internal static string ReadExtensionText(string xml)
    {
        string value = xml == null ? "" : xml.Trim();
        int start = value.IndexOf('>');
        int end = value.LastIndexOf('<');
        if (start >= 0 && end > start) value = value.Substring(start + 1, end - start - 1).Trim();
        else
        {
            int equals = value.IndexOf('=');
            if (equals >= 0) value = value.Substring(equals + 1).Trim().Trim('"', '\'');
        }
        return value.Replace("&lt;", "<", StringComparison.Ordinal).Replace("&gt;", ">", StringComparison.Ordinal).Replace("&quot;", "\"", StringComparison.Ordinal).Replace("&apos;", "'", StringComparison.Ordinal).Replace("&amp;", "&", StringComparison.Ordinal);
    }

    internal static string AttributeLocalName(string name)
    {
        int brace = name.LastIndexOf('}');
        if (brace >= 0 && brace + 1 < name.Length) return name.Substring(brace + 1);
        int colon = name.LastIndexOf(':');
        return colon >= 0 && colon + 1 < name.Length ? name.Substring(colon + 1) : name;
    }

    internal static OrchestraEncoding? SelectSbeEncoding(List<OrchestraEncoding> encodings)
    {
        OrchestraEncoding? fallback = null;
        for (int index = 0; index < encodings.Count; index++)
        {
            if (IsSbeStandard(encodings[index].Standard)) return encodings[index];
            if (encodings[index].Standard.Length == 0 && fallback == null) fallback = encodings[index];
        }
        return fallback;
    }

    internal static OrchestraEncoding? SelectSbeEncodingOrDefault(List<OrchestraEncoding> encodings, List<OrchestraEncoding> defaults)
    {
        OrchestraEncoding? selected = SelectSbeEncoding(encodings);
        return selected == null ? SelectSbeEncoding(defaults) : selected;
    }

    internal static bool IsSbeStandard(string value)
    {
        string normalized = value == null ? "" : value.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        return string.Equals(normalized, "SBE", StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "SimpleBinaryEncoding", StringComparison.OrdinalIgnoreCase);
    }

    internal static string ResolveOrchestraCodePrimitive(OrchestraRepository repository, string typeName)
    {
        for (int index = 0; index < repository.Datatypes.Count; index++)
        {
            if (!string.Equals(repository.Datatypes[index].Name, typeName, StringComparison.OrdinalIgnoreCase)) continue;
            OrchestraDatatypeMapping? mapping = null;
            for (int mappingIndex = 0; mappingIndex < repository.Datatypes[index].Mappings.Count; mappingIndex++) if (IsSbeStandard(repository.Datatypes[index].Mappings[mappingIndex].Standard)) mapping = repository.Datatypes[index].Mappings[mappingIndex];
            if (mapping != null && IsSbePrimitive(mapping.Base)) return NormalizePrimitiveName(mapping.Base);
            return ResolvePrimitiveType(ResolveLogicalFieldType(repository.Datatypes[index].Name, repository.Datatypes[index].BaseType));
        }
        return ResolvePrimitiveType(ResolveLogicalFieldType(typeName, ""));
    }

    internal static FixFieldType ResolveLogicalFieldType(string name, string baseName)
    {
        string value = name.Length == 0 ? baseName : name;
        string normalized = value.Replace("_", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal).ToUpperInvariant();
        if (normalized == "INT") return FixFieldType.Int;
        if (normalized == "SEQNUM") return FixFieldType.SeqNum;
        if (normalized == "NUMINGROUP") return FixFieldType.NumInGroup;
        if (normalized == "LENGTH") return FixFieldType.Length;
        if (normalized == "TAGNUM") return FixFieldType.TagNum;
        if (normalized == "FLOAT") return FixFieldType.Float;
        if (normalized == "QTY") return FixFieldType.Qty;
        if (normalized == "PRICE") return FixFieldType.Price;
        if (normalized == "PRICEOFFSET") return FixFieldType.PriceOffset;
        if (normalized == "AMT") return FixFieldType.Amt;
        if (normalized == "PERCENTAGE") return FixFieldType.Percentage;
        if (normalized == "CHAR") return FixFieldType.Char;
        if (normalized == "BOOLEAN") return FixFieldType.Boolean;
        if (normalized == "STRING") return FixFieldType.String;
        if (normalized == "MULTIPLECHARVALUE") return FixFieldType.MultipleCharValue;
        if (normalized == "MULTIPLESTRINGVALUE") return FixFieldType.MultipleStringValue;
        if (normalized == "CURRENCY") return FixFieldType.Currency;
        if (normalized == "COUNTRY") return FixFieldType.Country;
        if (normalized == "EXCHANGE") return FixFieldType.Exchange;
        if (normalized == "MONTHYEAR") return FixFieldType.MonthYear;
        if (normalized == "DAYOFMONTH") return FixFieldType.DayOfMonth;
        if (normalized == "LOCALMKTDATE") return FixFieldType.LocalMktDate;
        if (normalized == "UTCDATEONLY") return FixFieldType.UtcDateOnly;
        if (normalized == "UTCTIMEONLY") return FixFieldType.UtcTimeOnly;
        if (normalized == "UTCTIMESTAMP") return FixFieldType.UtcTimestamp;
        if (normalized == "TZTIMEONLY") return FixFieldType.TzTimeOnly;
        if (normalized == "TZTIMESTAMP") return FixFieldType.TzTimestamp;
        if (normalized == "DATA") return FixFieldType.Data;
        if (normalized == "XMLDATA") return FixFieldType.XmlData;
        if (baseName.Length != 0 && !string.Equals(name, baseName, StringComparison.OrdinalIgnoreCase)) return ResolveLogicalFieldType(baseName, "");
        return FixFieldType.Unknown;
    }

    internal static string ResolvePrimitiveType(FixFieldType type)
    {
        if (type == FixFieldType.Int || type == FixFieldType.DayOfMonth) return "int32";
        if (type == FixFieldType.SeqNum || type == FixFieldType.Length || type == FixFieldType.TagNum || type == FixFieldType.LocalMktDate || type == FixFieldType.UtcDateOnly) return "uint32";
        if (type == FixFieldType.NumInGroup) return "uint16";
        if (type == FixFieldType.Char) return "char";
        if (type == FixFieldType.Boolean) return "uint8";
        if (type == FixFieldType.UtcTimeOnly || type == FixFieldType.UtcTimestamp) return "uint64";
        if (type == FixFieldType.Data || type == FixFieldType.XmlData) return "uint8";
        if (type == FixFieldType.String || type == FixFieldType.MultipleCharValue || type == FixFieldType.MultipleStringValue || type == FixFieldType.Currency || type == FixFieldType.Country || type == FixFieldType.Exchange || type == FixFieldType.TzTimeOnly || type == FixFieldType.TzTimestamp) return "char";
        return "";
    }

    internal static bool IsDecimalFieldType(FixFieldType type)
    {
        return type == FixFieldType.Float || type == FixFieldType.Qty || type == FixFieldType.Price || type == FixFieldType.PriceOffset || type == FixFieldType.Amt || type == FixFieldType.Percentage;
    }

    internal static bool IsSbePrimitive(string value)
    {
        string normalized = NormalizePrimitiveName(value);
        return normalized == "char" || normalized == "int8" || normalized == "int16" || normalized == "int32" || normalized == "int64" || normalized == "uint8" || normalized == "uint16" || normalized == "uint32" || normalized == "uint64" || normalized == "float" || normalized == "double";
    }

    internal static string NormalizePrimitiveName(string value)
    {
        return value == null ? "" : value.Trim().ToLowerInvariant();
    }

    internal static string CanonicalSemanticType(FixFieldType type)
    {
        return type == FixFieldType.Unknown ? "" : type.ToString();
    }

    internal static string ResolvePresence(FixPresence presence, string value)
    {
        if (presence == FixPresence.Constant) return "constant";
        if (presence == FixPresence.Optional || presence == FixPresence.Conditional) return "optional";
        return "required";
    }

    internal static string NormalizeSbePresence(string value, List<GenerationDiagnostic> diagnostics, string path)
    {
        if (string.Equals(value, "required", StringComparison.OrdinalIgnoreCase)) return "required";
        if (string.Equals(value, "optional", StringComparison.OrdinalIgnoreCase)) return "optional";
        if (string.Equals(value, "constant", StringComparison.OrdinalIgnoreCase)) return "constant";
        AddTypeDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-sbe-presence", "SBE presence must be required, optional, or constant.", path);
        return "required";
    }

    internal static string ResolveScaleExponent(string parameter)
    {
        string value = parameter == null ? "" : parameter.Trim();
        int equals = value.IndexOf('=');
        if (equals >= 0) value = value.Substring(equals + 1).Trim();
        int scale = ParseNonnegativeInteger(value);
        return scale < 0 ? "0" : "-" + scale.ToString(CultureInfo.InvariantCulture);
    }

    internal static int ResolveSinceVersion(OrchestraPedigree pedigree)
    {
        int extension = ParseNonnegativeInteger(pedigree.AddedExtensionPack);
        if (extension >= 0 && pedigree.AddedExtensionPack.Length != 0) return extension;
        int version = ParseNonnegativeInteger(pedigree.AddedVersion);
        return version < 0 ? 0 : version;
    }

    internal static int ResolveDeprecatedVersion(OrchestraPedigree pedigree)
    {
        int extension = ParseNonnegativeInteger(pedigree.DeprecatedExtensionPack);
        if (extension >= 0 && pedigree.DeprecatedExtensionPack.Length != 0) return extension;
        int version = ParseNonnegativeInteger(pedigree.DeprecatedVersion);
        return version < 0 ? 0 : version;
    }

    internal static void ApplyPedigreeToType(SbeTypeMapping mapping, OrchestraPedigree pedigree)
    {
        mapping.SinceVersion = ResolveSinceVersion(pedigree);
        mapping.DeprecatedVersion = ResolveDeprecatedVersion(pedigree);
    }

    internal static int ParseNonnegativeInteger(string value)
    {
        string source = value == null ? "" : value.Trim();
        if (source.Length == 0) return -1;
        int result = 0;
        for (int index = 0; index < source.Length; index++)
        {
            char character = source[index];
            if (character < '0' || character > '9') return -1;
            int digit = character - '0';
            if (result > (int.MaxValue - digit) / 10) return -1;
            result = result * 10 + digit;
        }
        return result;
    }

    internal static int ParseOptionalPositiveInteger(string value)
    {
        int parsed = ParseNonnegativeInteger(value);
        return parsed > 0 ? parsed : 0;
    }

    internal static int ParseRequiredPositiveId(string value, string kind, string name, List<GenerationDiagnostic> diagnostics)
    {
        int parsed = ParseNonnegativeInteger(value);
        if (parsed > 0) return parsed;
        AddTypeDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-sbe-" + kind + "-id", "An Orchestra " + kind + " identifier must be a positive integer.", "/" + kind + "s/" + name);
        return 0;
    }

    internal static bool IsTrueText(string value)
    {
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
    }

    internal static int CompareOrchestraCodes(OrchestraCodeDefinition left, OrchestraCodeDefinition right)
    {
        int leftSort = ParseNonnegativeInteger(left.Sort);
        int rightSort = ParseNonnegativeInteger(right.Sort);
        if (leftSort >= 0 && rightSort >= 0 && leftSort != rightSort) return leftSort.CompareTo(rightSort);
        int comparison = string.CompareOrdinal(left.Value, right.Value);
        return comparison != 0 ? comparison : string.CompareOrdinal(left.Name, right.Name);
    }

    internal static int CompareOrchestraFields(OrchestraFieldDefinition left, OrchestraFieldDefinition right)
    {
        int leftId = ParseNonnegativeInteger(left.Id);
        int rightId = ParseNonnegativeInteger(right.Id);
        int comparison = leftId.CompareTo(rightId);
        return comparison != 0 ? comparison : string.CompareOrdinal(left.Name, right.Name);
    }

    internal static int CompareOrchestraComponents(OrchestraComponentDefinition left, OrchestraComponentDefinition right)
    {
        int comparison = ParseNonnegativeInteger(left.Id).CompareTo(ParseNonnegativeInteger(right.Id));
        return comparison != 0 ? comparison : string.CompareOrdinal(left.Name, right.Name);
    }

    internal static int CompareOrchestraGroups(OrchestraGroupDefinition left, OrchestraGroupDefinition right)
    {
        int comparison = ParseNonnegativeInteger(left.Id).CompareTo(ParseNonnegativeInteger(right.Id));
        return comparison != 0 ? comparison : string.CompareOrdinal(left.Name, right.Name);
    }

    internal static int CompareOrchestraMembers(OrchestraMemberDefinition left, OrchestraMemberDefinition right)
    {
        int comparison = left.Position.CompareTo(right.Position);
        if (comparison != 0) return comparison;
        comparison = string.CompareOrdinal(left.Kind, right.Kind);
        return comparison != 0 ? comparison : string.CompareOrdinal(left.ReferenceId, right.ReferenceId);
    }

    internal static List<OrchestraMessageDefinition> SelectOrchestraMessages(OrchestraRepository repository, string scenario)
    {
        List<OrchestraMessageDefinition> sorted = new List<OrchestraMessageDefinition>(repository.Messages);
        sorted.Sort((left, right) => CompareOrchestraMessages(left, right));
        if (scenario == null || scenario.Trim().Length == 0) return sorted;
        List<OrchestraMessageDefinition> result = [];
        List<string> selectedTypes = [];
        string selected = NormalizeScenarioName(scenario);
        for (int pass = 0; pass < 2; pass++)
        {
            string candidate = pass == 0 ? selected : "BASE";
            for (int index = 0; index < sorted.Count; index++)
            {
                if (!ContainsExactText(selectedTypes, sorted[index].MessageType) && NormalizeScenarioName(sorted[index].Scenario) == candidate)
                {
                    selectedTypes.Add(sorted[index].MessageType);
                    result.Add(sorted[index]);
                }
            }
        }
        result.Sort((left, right) => CompareOrchestraMessages(left, right));
        return result;
    }

    internal static int CompareOrchestraMessages(OrchestraMessageDefinition left, OrchestraMessageDefinition right)
    {
        int comparison = string.CompareOrdinal(left.MessageType, right.MessageType);
        if (comparison != 0) return comparison;
        comparison = string.CompareOrdinal(NormalizeScenarioName(left.Scenario), NormalizeScenarioName(right.Scenario));
        return comparison != 0 ? comparison : string.CompareOrdinal(left.Name, right.Name);
    }

    internal static List<FixMessageDefinition> SelectDictionaryMessages(FixDictionary dictionary, string scenario)
    {
        List<FixMessageDefinition> sorted = new List<FixMessageDefinition>(dictionary.Messages);
        sorted.Sort((left, right) => CSharpNameResolver.CompareMessageDefinitions(left, right));
        if (scenario == null || scenario.Trim().Length == 0) return sorted;
        List<FixMessageDefinition> result = [];
        List<string> selectedTypes = [];
        string selected = NormalizeScenarioName(scenario);
        for (int pass = 0; pass < 2; pass++)
        {
            string candidate = pass == 0 ? selected : "BASE";
            for (int index = 0; index < sorted.Count; index++)
            {
                if (!ContainsExactText(selectedTypes, sorted[index].MsgType) && NormalizeScenarioName(sorted[index].Scenario) == candidate)
                {
                    selectedTypes.Add(sorted[index].MsgType);
                    result.Add(sorted[index]);
                }
            }
        }
        result.Sort((left, right) => CSharpNameResolver.CompareMessageDefinitions(left, right));
        return result;
    }

    internal static string NormalizeScenarioName(string value)
    {
        string result = value == null ? "" : value.Trim().ToUpperInvariant();
        return result.Length == 0 ? "BASE" : result;
    }

    internal static string ResolveOrchestraMemberName(OrchestraRepository repository, OrchestraMemberDefinition member)
    {
        if (member.InstanceName.Length != 0) return member.InstanceName;
        string kind = ResolveOrchestraMemberKind(member.Kind);
        if (kind == "field")
        {
            OrchestraFieldDefinition? field = FindOrchestraField(repository.Fields, member.ReferenceId);
            return field == null ? "" : field.Name;
        }
        if (kind == "component")
        {
            OrchestraComponentDefinition? component = FindOrchestraComponent(repository.Components, member.ReferenceId);
            return component == null ? "" : component.Name;
        }
        OrchestraGroupDefinition? group = FindOrchestraGroup(repository.Groups, member.ReferenceId);
        return group == null ? "" : group.Name;
    }

    internal static string ResolveOrchestraMemberType(OrchestraRepository repository, OrchestraMemberDefinition member)
    {
        string kind = ResolveOrchestraMemberKind(member.Kind);
        if (kind == "field")
        {
            OrchestraFieldDefinition? field = FindOrchestraField(repository.Fields, member.ReferenceId);
            return field == null ? "" : field.CodeSet.Length == 0 ? field.Type : field.CodeSet;
        }
        if (kind == "component")
        {
            OrchestraComponentDefinition? component = FindOrchestraComponent(repository.Components, member.ReferenceId);
            return component == null ? "" : component.Name;
        }
        OrchestraGroupDefinition? group = FindOrchestraGroup(repository.Groups, member.ReferenceId);
        return group == null ? "" : group.Name;
    }

    internal static string ResolveOrchestraMemberSemanticType(OrchestraRepository repository, OrchestraMemberDefinition member)
    {
        if (ResolveOrchestraMemberKind(member.Kind) != "field") return "";
        OrchestraFieldDefinition? field = FindOrchestraField(repository.Fields, member.ReferenceId);
        return field == null ? "" : field.Type;
    }

    internal static string ResolveOrchestraMemberKind(string kind)
    {
        if (kind.IndexOf("group", StringComparison.OrdinalIgnoreCase) >= 0) return "group";
        if (kind.IndexOf("component", StringComparison.OrdinalIgnoreCase) >= 0) return "component";
        return "field";
    }

    internal static OrchestraFieldDefinition? FindOrchestraField(List<OrchestraFieldDefinition> definitions, string id)
    {
        for (int index = 0; index < definitions.Count; index++) if (definitions[index].Id == id) return definitions[index];
        return null;
    }

    internal static OrchestraComponentDefinition? FindOrchestraComponent(List<OrchestraComponentDefinition> definitions, string id)
    {
        for (int index = 0; index < definitions.Count; index++) if (definitions[index].Id == id) return definitions[index];
        return null;
    }

    internal static OrchestraGroupDefinition? FindOrchestraGroup(List<OrchestraGroupDefinition> definitions, string id)
    {
        for (int index = 0; index < definitions.Count; index++) if (definitions[index].Id == id) return definitions[index];
        return null;
    }

    internal static FixComponentDefinition? FindDictionaryComponent(List<FixComponentDefinition> definitions, int id)
    {
        for (int index = 0; index < definitions.Count; index++) if (definitions[index].Id == id) return definitions[index];
        return null;
    }

    internal static FixGroupDefinition? FindDictionaryGroup(List<FixGroupDefinition> definitions, int id)
    {
        for (int index = 0; index < definitions.Count; index++) if (definitions[index].Id == id) return definitions[index];
        return null;
    }

    internal static SbeTypeMapping? FindMappedType(List<SbeTypeMapping> mappings, string name)
    {
        for (int index = 0; index < mappings.Count; index++) if (string.Equals(mappings[index].Name, name, StringComparison.OrdinalIgnoreCase)) return mappings[index];
        return null;
    }

    internal static SbeFieldMapping? FindMappedField(List<SbeFieldMapping> fields, int id)
    {
        for (int index = 0; index < fields.Count; index++) if (fields[index].Id == id) return fields[index];
        return null;
    }

    internal static SbeFieldMapping? FindMappedFieldBySourceReference(List<SbeFieldMapping> fields, string referenceId)
    {
        int id = ParseOptionalPositiveInteger(referenceId);
        for (int index = 0; index < fields.Count; index++)
        {
            if (fields[index].SourceId == id) return fields[index];
        }
        return null;
    }

    internal static string ResolveDictionaryFieldTypeName(FixFieldDefinition field)
    {
        if (field.DatatypeName.Length != 0) return field.DatatypeName;
        return CanonicalSemanticType(field.FieldType);
    }

    internal static void CopyMappedMembers(List<SbeMemberMapping> source, List<SbeMemberMapping> destination)
    {
        for (int index = 0; index < source.Count; index++) destination.Add(source[index]);
    }

    internal static void AddDuplicateTemplateDiagnostics(SbeMappingPlan plan)
    {
        for (int left = 0; left < plan.Messages.Count; left++)
        {
            for (int right = left + 1; right < plan.Messages.Count; right++)
            {
                if (plan.Messages[left].TemplateId == plan.Messages[right].TemplateId) AddTypeDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "duplicate-sbe-template-id", "Two mapped messages have the same SBE template identifier.", "/messages/" + plan.Messages[right].Name);
            }
        }
    }

    internal static void SortMappingPlan(SbeMappingPlan plan)
    {
        plan.Types.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        plan.Fields.Sort((left, right) => left.Id.CompareTo(right.Id));
        plan.Components.Sort((left, right) => left.Id.CompareTo(right.Id));
        plan.Groups.Sort((left, right) => left.Id.CompareTo(right.Id));
        plan.Messages.Sort((left, right) => left.TemplateId.CompareTo(right.TemplateId));
    }

    internal static bool ContainsExactText(List<string> values, string value)
    {
        for (int index = 0; index < values.Count; index++) if (values[index] == value) return true;
        return false;
    }

    internal static void AddTypeDiagnostic(List<GenerationDiagnostic> diagnostics, ValidationSeverity severity, string code, string message, string source)
    {
        GenerationDiagnostic diagnostic = new GenerationDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Source = source;
        diagnostics.Add(diagnostic);
    }
}
