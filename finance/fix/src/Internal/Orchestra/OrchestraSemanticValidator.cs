using System.Collections.Generic;

namespace fix;

internal static class OrchestraSemanticValidator
{
    internal static readonly System.Threading.AsyncLocal<OrchestraSemanticLookup?> CurrentLookup = new System.Threading.AsyncLocal<OrchestraSemanticLookup?>();

    internal static OrchestraReadResult ValidateRepository(OrchestraRepository repository)
    {
        OrchestraReadResult result = new OrchestraReadResult();
        result.Repository = repository;
        if (repository == null)
        {
            AddDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-orchestra-repository", "The Orchestra repository is missing.", "/");
            result.Success = false;
            return result;
        }
        CurrentLookup.Value = new OrchestraSemanticLookup(repository);
        ValidateUniqueDefinitions(repository, result.Diagnostics);
        ValidateScenarioInheritance(repository, result.Diagnostics);
        ValidateDefinitionReferences(repository, result.Diagnostics);
        ValidateMembersAndGroups(repository, result.Diagnostics);
        ValidateCodeTypes(repository, result.Diagnostics);
        ValidateRuleReferences(repository, result.Diagnostics);
        ValidateWorkflow(repository, result.Diagnostics);
        ValidateInterfaces(repository, result.Diagnostics);
        result.Success = !HasErrors(result.Diagnostics);
        CurrentLookup.Value = null;
        return result;
    }

    internal static void ValidateUniqueDefinitions(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < repository.Datatypes.Count; index++)
        {
            for (int previous = 0; previous < index; previous++)
            {
                if (repository.Datatypes[index].Name == repository.Datatypes[previous].Name) AddDuplicate(diagnostics, "duplicate-datatype-name", repository.Datatypes[index].Name, "/datatypes");
            }
        }
        for (int index = 0; index < repository.EncodingStandards.Count; index++)
        {
            for (int previous = 0; previous < index; previous++)
            {
                if (repository.EncodingStandards[index].Name == repository.EncodingStandards[previous].Name) AddDuplicate(diagnostics, "duplicate-encoding-standard-name", repository.EncodingStandards[index].Name, "/encodingStandards");
            }
        }
        for (int index = 0; index < repository.Scenarios.Count; index++)
        {
            for (int previous = 0; previous < index; previous++)
            {
                if (repository.Scenarios[index].Name == repository.Scenarios[previous].Name) AddDuplicate(diagnostics, "duplicate-scenario-name", repository.Scenarios[index].Name, "/scenarios");
            }
        }
        ValidateCodeSetUniqueness(repository, diagnostics);
        ValidateFieldUniqueness(repository, diagnostics);
        ValidateComponentUniqueness(repository, diagnostics);
        ValidateGroupUniqueness(repository, diagnostics);
        ValidateMessageUniqueness(repository, diagnostics);
        for (int index = 0; index < repository.Rules.Count; index++)
        {
            for (int previous = 0; previous < index; previous++)
            {
                if (repository.Rules[index].Id == repository.Rules[previous].Id) AddDuplicate(diagnostics, "duplicate-rule-id", repository.Rules[index].Id.ToString(), "/rules");
            }
        }
    }

    internal static void ValidateCodeSetUniqueness(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < repository.CodeSets.Count; index++)
        {
            OrchestraCodeSetDefinition current = repository.CodeSets[index];
            for (int previous = 0; previous < index; previous++)
            {
                OrchestraCodeSetDefinition other = repository.CodeSets[previous];
                if (current.Scenario == other.Scenario && current.Id.Length != 0 && current.Id == other.Id) AddDuplicate(diagnostics, "duplicate-code-set-id", current.Id, "/codeSets");
                if (current.Scenario == other.Scenario && current.Name == other.Name) AddDuplicate(diagnostics, "duplicate-code-set-name", current.Name, "/codeSets");
            }
            for (int codeIndex = 0; codeIndex < current.Codes.Count; codeIndex++)
            {
                OrchestraCodeDefinition code = current.Codes[codeIndex];
                for (int previousCode = 0; previousCode < codeIndex; previousCode++)
                {
                    OrchestraCodeDefinition otherCode = current.Codes[previousCode];
                    if (code.Id.Length != 0 && code.Id == otherCode.Id) AddDuplicate(diagnostics, "duplicate-code-id", code.Id, $"/codeSets/{current.Name}/codes");
                    if (code.Name == otherCode.Name) AddDuplicate(diagnostics, "duplicate-code-name", code.Name, $"/codeSets/{current.Name}/codes");
                    if (code.Value == otherCode.Value) AddDuplicate(diagnostics, "duplicate-code-value", code.Value, $"/codeSets/{current.Name}/codes");
                }
            }
        }
    }

    internal static void ValidateFieldUniqueness(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        Dictionary<string, int> identifierCounts = [];
        Dictionary<string, int> nameCounts = [];
        for (int index = 0; index < repository.Fields.Count; index++)
        {
            OrchestraFieldDefinition current = repository.Fields[index];
            string identifierKey = current.Id + "\u001f" + current.Scenario;
            string nameKey = current.Name + "\u001f" + current.Scenario;
            int identifierCount = identifierCounts.ContainsKey(identifierKey) ? identifierCounts[identifierKey] : 0;
            int nameCount = nameCounts.ContainsKey(nameKey) ? nameCounts[nameKey] : 0;
            for (int duplicate = 0; duplicate < identifierCount; duplicate++) AddDuplicate(diagnostics, "duplicate-field-id", current.Id, "/fields");
            for (int duplicate = 0; duplicate < nameCount; duplicate++) AddDuplicate(diagnostics, "duplicate-field-name", current.Name, "/fields");
            identifierCounts[identifierKey] = identifierCount + 1;
            nameCounts[nameKey] = nameCount + 1;
        }
    }

    internal static void ValidateComponentUniqueness(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < repository.Components.Count; index++)
        {
            OrchestraComponentDefinition current = repository.Components[index];
            for (int previous = 0; previous < index; previous++)
            {
                OrchestraComponentDefinition other = repository.Components[previous];
                if (current.Scenario == other.Scenario && current.Id == other.Id) AddDuplicate(diagnostics, "duplicate-component-id", current.Id, "/components");
                if (current.Scenario == other.Scenario && current.Name == other.Name) AddDuplicate(diagnostics, "duplicate-component-name", current.Name, "/components");
            }
        }
    }

    internal static void ValidateGroupUniqueness(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < repository.Groups.Count; index++)
        {
            OrchestraGroupDefinition current = repository.Groups[index];
            for (int previous = 0; previous < index; previous++)
            {
                OrchestraGroupDefinition other = repository.Groups[previous];
                if (current.Scenario == other.Scenario && current.Id == other.Id) AddDuplicate(diagnostics, "duplicate-group-id", current.Id, "/groups");
                if (current.Scenario == other.Scenario && current.Name == other.Name) AddDuplicate(diagnostics, "duplicate-group-name", current.Name, "/groups");
            }
        }
    }

    internal static void ValidateMessageUniqueness(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < repository.Messages.Count; index++)
        {
            OrchestraMessageDefinition current = repository.Messages[index];
            for (int previous = 0; previous < index; previous++)
            {
                OrchestraMessageDefinition other = repository.Messages[previous];
                if (current.Scenario == other.Scenario && current.Id == other.Id) AddDuplicate(diagnostics, "duplicate-message-id", current.Id, "/messages");
                if (current.Scenario == other.Scenario && current.Name == other.Name) AddDuplicate(diagnostics, "duplicate-message-name", current.Name, "/messages");
                if (current.Scenario == other.Scenario && current.MessageType.Length != 0 && current.MessageType == other.MessageType) AddDuplicate(diagnostics, "duplicate-message-type", current.MessageType, "/messages");
            }
        }
    }

    internal static void ValidateScenarioInheritance(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < repository.Scenarios.Count; index++)
        {
            OrchestraScenarioDefinition scenario = repository.Scenarios[index];
            if (scenario.ParentName.Length != 0 && !HasScenario(repository, scenario.ParentName))
            {
                AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-parent-scenario", $"Scenario '{scenario.Name}' inherits missing scenario '{scenario.ParentName}'.", $"/scenarios/{scenario.Name}");
            }
            if (scenario.ParentName.Length != 0 && HasScenarioCycle(repository, scenario.Name))
            {
                AddDiagnostic(diagnostics, ValidationSeverity.Error, "scenario-inheritance-cycle", $"Scenario '{scenario.Name}' participates in an inheritance cycle.", $"/scenarios/{scenario.Name}");
            }
        }
        ValidateDefinitionScenarioReferences(repository, diagnostics);
    }

    internal static bool HasScenarioCycle(OrchestraRepository repository, string start)
    {
        string current = start;
        for (int depth = 0; depth <= repository.Scenarios.Count; depth++)
        {
            OrchestraScenarioDefinition? scenario = FindScenario(repository, current);
            if (scenario == null || scenario.ParentName.Length == 0) return false;
            current = scenario.ParentName;
            if (current == start) return true;
        }
        return true;
    }

    internal static void ValidateDefinitionScenarioReferences(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < repository.CodeSets.Count; index++) ValidateScenarioReference(repository, repository.CodeSets[index].Scenario, repository.CodeSets[index].Attributes, diagnostics, $"/codeSets/{repository.CodeSets[index].Name}");
        for (int index = 0; index < repository.Fields.Count; index++) ValidateScenarioReference(repository, repository.Fields[index].Scenario, repository.Fields[index].Attributes, diagnostics, $"/fields/{repository.Fields[index].Name}");
        for (int index = 0; index < repository.Components.Count; index++) ValidateScenarioReference(repository, repository.Components[index].Scenario, repository.Components[index].Attributes, diagnostics, $"/components/{repository.Components[index].Name}");
        for (int index = 0; index < repository.Groups.Count; index++) ValidateScenarioReference(repository, repository.Groups[index].Scenario, repository.Groups[index].Attributes, diagnostics, $"/groups/{repository.Groups[index].Name}");
        for (int index = 0; index < repository.Messages.Count; index++) ValidateScenarioReference(repository, repository.Messages[index].Scenario, repository.Messages[index].Attributes, diagnostics, $"/messages/{repository.Messages[index].Name}");
    }

    internal static void ValidateScenarioReference(OrchestraRepository repository, string scenario, Dictionary<string, string> attributes, List<OrchestraDiagnostic> diagnostics, string path)
    {
        if (scenario.Length != 0 && !HasScenario(repository, scenario)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-scenario", $"Definition scenario '{scenario}' is not declared.", path);
        string parent = ReadAttribute(attributes, "scenarioRef");
        if (parent.Length != 0 && !HasScenario(repository, parent)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-parent-scenario", $"Inherited scenario '{parent}' is not declared.", path);
        if (parent.Length != 0 && parent == scenario) AddDiagnostic(diagnostics, ValidationSeverity.Error, "scenario-inheritance-cycle", "A definition cannot inherit its own scenario.", path);
    }

    internal static void ValidateDefinitionReferences(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < repository.Datatypes.Count; index++)
        {
            OrchestraDatatypeDefinition datatype = repository.Datatypes[index];
            if (datatype.BaseType.Length != 0 && !HasDatatype(repository, datatype.BaseType)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-datatype-base", $"Datatype '{datatype.Name}' references missing base type '{datatype.BaseType}'.", $"/datatypes/{datatype.Name}");
            for (int mappingIndex = 0; mappingIndex < datatype.Mappings.Count; mappingIndex++)
            {
                string standard = datatype.Mappings[mappingIndex].Standard;
                if (standard.Length != 0 && !HasEncodingStandard(repository, standard)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-encoding-standard", $"Datatype mapping references missing encoding standard '{standard}'.", $"/datatypes/{datatype.Name}/mappings");
            }
        }
        for (int index = 0; index < repository.Categories.Count; index++)
        {
            OrchestraCategory category = repository.Categories[index];
            if (category.Section.Length != 0 && !HasSection(repository, category.Section)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-section-reference", $"Category '{category.Name}' references missing section '{category.Section}'.", $"/categories/{category.Name}");
            ValidateEncodingReferences(repository, category.Encodings, diagnostics, $"/categories/{category.Name}/encodings");
        }
        for (int index = 0; index < repository.Sections.Count; index++) ValidateEncodingReferences(repository, repository.Sections[index].Encodings, diagnostics, $"/sections/{repository.Sections[index].Name}/encodings");
        ValidateCodeSetReferences(repository, diagnostics);
        ValidateFieldReferences(repository, diagnostics);
        ValidateDefinitionMemberReferences(repository, diagnostics);
        ValidateConceptReferences(repository, diagnostics);
    }

    internal static void ValidateCodeSetReferences(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        ValidateEncodingReferences(repository, repository.CodeSetEncodings, diagnostics, "/codeSets/encodings");
        for (int index = 0; index < repository.CodeSets.Count; index++)
        {
            OrchestraCodeSetDefinition codeSet = repository.CodeSets[index];
            if (!HasDatatype(repository, codeSet.Type)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-code-type", $"Code set '{codeSet.Name}' references missing datatype '{codeSet.Type}'.", $"/codeSets/{codeSet.Name}");
            ValidateEncodingReferences(repository, codeSet.Encodings, diagnostics, $"/codeSets/{codeSet.Name}/encodings");
        }
    }

    internal static void ValidateFieldReferences(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < repository.Fields.Count; index++)
        {
            OrchestraFieldDefinition field = repository.Fields[index];
            string path = $"/fields/{field.Name}";
            if (field.Type.Length == 0 && field.CodeSet.Length == 0) AddDiagnostic(diagnostics, ValidationSeverity.Error, "missing-field-type", $"Field '{field.Name}' has neither a datatype nor a code set.", path);
            if (field.Type.Length != 0 && field.CodeSet.Length != 0) AddDiagnostic(diagnostics, ValidationSeverity.Error, "ambiguous-field-type", $"Field '{field.Name}' has both a datatype and a code set.", path);
            if (field.Type.Length != 0 && !HasDatatype(repository, field.Type)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-field-type", $"Field '{field.Name}' references missing datatype '{field.Type}'.", path);
            if (field.CodeSet.Length != 0 && !HasCodeSet(repository, field.CodeSet, field.Scenario)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-code-set-reference", $"Field '{field.Name}' references missing code set '{field.CodeSet}'.", path);
            if (field.LengthId.Length != 0 && !HasField(repository, field.LengthId, field.Scenario)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-length-field", $"Field '{field.Name}' references missing length field '{field.LengthId}'.", path);
            if (field.DiscriminatorId.Length != 0 && !HasField(repository, field.DiscriminatorId, field.Scenario)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-field-discriminator", $"Field '{field.Name}' references missing discriminator field '{field.DiscriminatorId}'.", path);
            ValidateEncodingReferences(repository, field.Encodings, diagnostics, path + "/encodings");
        }
    }

    internal static void ValidateDefinitionMemberReferences(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        ValidateEncodingReferences(repository, repository.ComponentEncodings, diagnostics, "/components/encodings");
        for (int index = 0; index < repository.Components.Count; index++)
        {
            OrchestraComponentDefinition component = repository.Components[index];
            if (component.Category.Length != 0 && !HasCategory(repository, component.Category)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-category-reference", $"Component '{component.Name}' references missing category '{component.Category}'.", $"/components/{component.Name}");
            ValidateMemberReferences(repository, component.Members, diagnostics, $"/components/{component.Name}");
            ValidateEncodingReferences(repository, component.Encodings, diagnostics, $"/components/{component.Name}/encodings");
        }
        ValidateEncodingReferences(repository, repository.GroupEncodings, diagnostics, "/groups/encodings");
        for (int index = 0; index < repository.Groups.Count; index++)
        {
            OrchestraGroupDefinition group = repository.Groups[index];
            if (group.Category.Length != 0 && !HasCategory(repository, group.Category)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-category-reference", $"Group '{group.Name}' references missing category '{group.Category}'.", $"/groups/{group.Name}");
            ValidateMemberReferences(repository, group.Members, diagnostics, $"/groups/{group.Name}");
            ValidateEncodingReferences(repository, group.Encodings, diagnostics, $"/groups/{group.Name}/encodings");
        }
        ValidateEncodingReferences(repository, repository.MessageEncodings, diagnostics, "/messages/encodings");
        for (int index = 0; index < repository.Messages.Count; index++)
        {
            OrchestraMessageDefinition message = repository.Messages[index];
            if (message.Category.Length != 0 && !HasCategory(repository, message.Category)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-category-reference", $"Message '{message.Name}' references missing category '{message.Category}'.", $"/messages/{message.Name}");
            if (message.Flow.Length != 0 && !HasFlow(repository, message.Flow)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-flow-reference", $"Message '{message.Name}' references missing flow '{message.Flow}'.", $"/messages/{message.Name}");
            ValidateMemberReferences(repository, message.Members, diagnostics, $"/messages/{message.Name}");
            ValidateEncodingReferences(repository, message.Encodings, diagnostics, $"/messages/{message.Name}/encodings");
        }
    }

    internal static void ValidateMemberReferences(OrchestraRepository repository, List<OrchestraMemberDefinition> members, List<OrchestraDiagnostic> diagnostics, string ownerPath)
    {
        for (int index = 0; index < members.Count; index++)
        {
            OrchestraMemberDefinition member = members[index];
            string path = ownerPath + $"/members/{index}";
            bool resolved = false;
            if (member.Kind == "fieldRef") resolved = HasField(repository, member.ReferenceId, member.Scenario);
            else if (member.Kind == "componentRef") resolved = HasComponent(repository, member.ReferenceId, member.Scenario);
            else if (member.Kind == "groupRef") resolved = HasGroup(repository, member.ReferenceId, member.Scenario);
            else AddDiagnostic(diagnostics, ValidationSeverity.Error, "unknown-member-kind", $"Member kind '{member.Kind}' is not supported.", path);
            if (!resolved && (member.Kind == "fieldRef" || member.Kind == "componentRef" || member.Kind == "groupRef")) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-member-reference", $"Member '{member.Kind}' references missing identifier '{member.ReferenceId}' in scenario '{member.Scenario}'.", path);
            if (member.LengthId.Length != 0 && !HasField(repository, member.LengthId, member.Scenario)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-length-field", $"Member references missing length field '{member.LengthId}'.", path);
            ValidateEncodingReferences(repository, member.Encodings, diagnostics, path + "/encodings");
        }
    }

    internal static void ValidateConceptReferences(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int conceptIndex = 0; conceptIndex < repository.Concepts.Count; conceptIndex++)
        {
            OrchestraConcept concept = repository.Concepts[conceptIndex];
            for (int memberIndex = 0; memberIndex < concept.Members.Count; memberIndex++)
            {
                OrchestraConceptMember member = concept.Members[memberIndex];
                bool resolved = false;
                if (member.Kind == "fieldRef") resolved = HasField(repository, member.Id, member.Scenario);
                else if (member.Kind == "componentRef") resolved = HasComponent(repository, member.Id, member.Scenario);
                else if (member.Kind == "groupRef") resolved = HasGroup(repository, member.Id, member.Scenario);
                else if (member.Kind == "messageRef") resolved = HasMessage(repository, member.Id, member.Scenario, member.MessageType, member.Name);
                if (!resolved) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-concept-member", $"Concept '{concept.Name}' contains an unresolved '{member.Kind}' reference.", $"/concepts/{concept.Name}/members/{memberIndex}");
            }
        }
    }

    internal static void ValidateEncodingReferences(OrchestraRepository repository, List<OrchestraEncoding> encodings, List<OrchestraDiagnostic> diagnostics, string path)
    {
        for (int index = 0; index < encodings.Count; index++)
        {
            if (encodings[index].Standard.Length != 0 && !HasEncodingStandard(repository, encodings[index].Standard)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-encoding-standard", $"Encoding references missing standard '{encodings[index].Standard}'.", path + $"/{index}");
        }
    }

    internal static void ValidateMembersAndGroups(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < repository.Components.Count; index++) ValidateMemberCardinality(repository.Components[index].Members, diagnostics, $"/components/{repository.Components[index].Name}");
        for (int index = 0; index < repository.Messages.Count; index++) ValidateMemberCardinality(repository.Messages[index].Members, diagnostics, $"/messages/{repository.Messages[index].Name}");
        for (int index = 0; index < repository.Groups.Count; index++)
        {
            OrchestraGroupDefinition group = repository.Groups[index];
            string path = $"/groups/{group.Name}";
            ValidateMemberCardinality(group.Members, diagnostics, path);
            if (group.CountMember == null) AddDiagnostic(diagnostics, ValidationSeverity.Error, "missing-group-count-field", $"Group '{group.Name}' has no count-field reference.", path);
            else
            {
                if (!HasField(repository, group.CountMember.ReferenceId, group.CountMember.Scenario)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-group-count-field", $"Group '{group.Name}' count field '{group.CountMember.ReferenceId}' cannot be resolved.", path);
                OrchestraFieldDefinition? countField = FindField(repository, group.CountMember.ReferenceId, group.CountMember.Scenario);
                if (countField != null && !IsCountDatatype(repository, countField)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-group-count-type", $"Group '{group.Name}' count field is not a count datatype.", path);
            }
            if (group.Members.Count == 0) AddDiagnostic(diagnostics, ValidationSeverity.Error, "missing-group-delimiter", $"Group '{group.Name}' has no entry delimiter.", path);
            else if (!HasResolvableGroupDelimiter(repository, group.Members[0], 0)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-group-delimiter", $"Group '{group.Name}' must begin with a field delimiter directly or through a component.", path + "/members/0");
            ValidateGroupBounds(group, diagnostics, path);
        }
    }

    internal static bool HasResolvableGroupDelimiter(OrchestraRepository repository, OrchestraMemberDefinition member, int depth)
    {
        if (depth > repository.Components.Count + repository.Groups.Count) return false;
        if (member.Kind == "fieldRef") return HasField(repository, member.ReferenceId, member.Scenario);
        if (member.Kind == "componentRef")
        {
            OrchestraComponentDefinition? component = FindComponent(repository, member.ReferenceId, member.Scenario);
            if (component == null || component.Members.Count == 0) return false;
            return HasResolvableGroupDelimiter(repository, component.Members[0], depth + 1);
        }
        if (member.Kind == "groupRef")
        {
            OrchestraGroupDefinition? group = FindGroup(repository, member.ReferenceId, member.Scenario);
            if (group == null || group.Members.Count == 0) return false;
            return HasResolvableGroupDelimiter(repository, group.Members[0], depth + 1);
        }
        return false;
    }

    internal static void ValidateMemberCardinality(List<OrchestraMemberDefinition> members, List<OrchestraDiagnostic> diagnostics, string path)
    {
        for (int index = 0; index < members.Count; index++)
        {
            OrchestraMemberDefinition member = members[index];
            string memberPath = path + $"/members/{index}";
            if (member.Position != index) AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-member-position", $"Member position {member.Position} does not match source position {index}.", memberPath);
            int minimum = ParseCardinality(member.ImplementationMinimum, 0);
            int maximum = ParseMaximumCardinality(member.ImplementationMaximum);
            if (minimum < 0) AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-member-minimum", $"Member minimum '{member.ImplementationMinimum}' is invalid.", memberPath);
            if (maximum < 0) AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-member-maximum", $"Member maximum '{member.ImplementationMaximum}' is invalid.", memberPath);
            if (minimum >= 0 && maximum >= 0 && maximum != int.MaxValue && minimum > maximum) AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-member-cardinality", "Member minimum exceeds its maximum.", memberPath);
            if (member.Presence == FixPresence.Required && maximum == 0) AddDiagnostic(diagnostics, ValidationSeverity.Error, "contradictory-member-cardinality", "A required member cannot have a maximum of zero.", memberPath);
        }
    }

    internal static void ValidateGroupBounds(OrchestraGroupDefinition group, List<OrchestraDiagnostic> diagnostics, string path)
    {
        string minimumText = ReadAttribute(group.Attributes, "implMinOccurs");
        string maximumText = ReadAttribute(group.Attributes, "implMaxOccurs");
        int minimum = ParseCardinality(minimumText, 0);
        int maximum = ParseMaximumCardinality(maximumText);
        if (minimum < 0) AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-group-minimum", $"Group minimum '{minimumText}' is invalid.", path);
        if (maximum < 0) AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-group-maximum", $"Group maximum '{maximumText}' is invalid.", path);
        if (minimum >= 0 && maximum >= 0 && maximum != int.MaxValue && minimum > maximum) AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-group-cardinality", "Group minimum exceeds its maximum.", path);
    }

    internal static int ParseCardinality(string value, int defaultValue)
    {
        if (value.Length == 0) return defaultValue;
        int parsed = OrchestraCommonParser.ParseNonnegativeInteger(value);
        return parsed;
    }

    internal static int ParseMaximumCardinality(string value)
    {
        if (value.Length == 0 || value == "unbounded") return int.MaxValue;
        return OrchestraCommonParser.ParseNonnegativeInteger(value);
    }

    internal static void ValidateCodeTypes(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int setIndex = 0; setIndex < repository.CodeSets.Count; setIndex++)
        {
            OrchestraCodeSetDefinition codeSet = repository.CodeSets[setIndex];
            string rootType = ResolveRootDatatype(repository, codeSet.Type);
            for (int codeIndex = 0; codeIndex < codeSet.Codes.Count; codeIndex++)
            {
                OrchestraCodeDefinition code = codeSet.Codes[codeIndex];
                if (rootType.Length != 0 && !IsCompatibleCodeValue(code.Value, rootType)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-code-value", $"Code value '{code.Value}' is not valid for datatype '{rootType}'.", $"/codeSets/{codeSet.Name}/codes/{code.Name}");
            }
            if (codeSet.DefaultValue.Length != 0 && !HasCodeValue(codeSet, codeSet.DefaultValue)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-code-set-default", $"Code set default '{codeSet.DefaultValue}' is not a declared code value.", $"/codeSets/{codeSet.Name}");
        }
    }

    internal static string ResolveRootDatatype(OrchestraRepository repository, string name)
    {
        string current = name;
        for (int depth = 0; depth <= repository.Datatypes.Count; depth++)
        {
            OrchestraDatatypeDefinition? datatype = FindDatatype(repository, current);
            if (datatype == null || datatype.BaseType.Length == 0) return current;
            current = datatype.BaseType;
        }
        return current;
    }

    internal static bool IsCompatibleCodeValue(string value, string type)
    {
        string normalized = type.ToLowerInvariant();
        if (normalized == "boolean" || normalized == "bool") return value == "true" || value == "false" || value == "0" || value == "1";
        if (normalized == "char") return value.Length == 1;
        if (normalized.Contains("int") || normalized == "length" || normalized == "numingroup" || normalized == "seqnum" || normalized == "tagnum" || normalized == "dayofmonth") return IsSignedInteger(value);
        if (normalized == "float" || normalized == "decimal" || normalized == "price" || normalized == "qty" || normalized == "amt" || normalized == "amount" || normalized == "percentage" || normalized == "priceoffset") return IsDecimalNumber(value);
        return true;
    }

    internal static bool IsSignedInteger(string value)
    {
        if (value.Length == 0) return false;
        int index = value[0] == '-' || value[0] == '+' ? 1 : 0;
        if (index == value.Length) return false;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9') return false;
            index++;
        }
        return true;
    }

    internal static bool IsDecimalNumber(string value)
    {
        if (value.Length == 0) return false;
        int index = value[0] == '-' || value[0] == '+' ? 1 : 0;
        bool digit = false;
        bool point = false;
        while (index < value.Length)
        {
            char character = value[index];
            if (character >= '0' && character <= '9') digit = true;
            else if (character == '.' && !point) point = true;
            else return false;
            index++;
        }
        return digit;
    }

    internal static bool HasCodeValue(OrchestraCodeSetDefinition codeSet, string value)
    {
        for (int index = 0; index < codeSet.Codes.Count; index++) if (codeSet.Codes[index].Value == value) return true;
        return false;
    }

    internal static void ValidateRuleReferences(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < repository.Rules.Count; index++)
        {
            OrchestraRule rule = repository.Rules[index];
            string path = $"/rules/{rule.Id}";
            if ((rule.Kind == OrchestraRuleKind.Conditional || rule.Kind == OrchestraRuleKind.Presence) && rule.Expression.Trim().Length == 0) AddDiagnostic(diagnostics, ValidationSeverity.Error, "missing-rule-expression", $"Rule '{rule.Name}' has no condition expression.", path);
            if (rule.Kind == OrchestraRuleKind.Assignment && rule.AssignmentTarget.Length == 0) AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-assignment-target", "An assignment rule has no target.", path);
            if (rule.Type.Length != 0 && !HasDatatype(repository, rule.Type)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-rule-type", $"Rule '{rule.Name}' references missing datatype '{rule.Type}'.", path);
            for (int fieldIndex = 0; fieldIndex < rule.UniqueFieldIds.Count; fieldIndex++)
            {
                if (!HasFieldAnyScenario(repository, rule.UniqueFieldIds[fieldIndex])) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-rule-field", $"Rule '{rule.Name}' references missing field '{rule.UniqueFieldIds[fieldIndex]}'.", path);
            }
        }
        ValidateOwnedRules(repository, diagnostics);
    }

    internal static void ValidateOwnedRules(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < repository.Fields.Count; index++) ValidateRuleRegistration(repository, repository.Fields[index].Rules, diagnostics, $"/fields/{repository.Fields[index].Name}/rules");
        for (int index = 0; index < repository.Components.Count; index++) ValidateMemberRuleRegistration(repository, repository.Components[index].Members, diagnostics, $"/components/{repository.Components[index].Name}");
        for (int index = 0; index < repository.Groups.Count; index++) ValidateMemberRuleRegistration(repository, repository.Groups[index].Members, diagnostics, $"/groups/{repository.Groups[index].Name}");
        for (int index = 0; index < repository.Messages.Count; index++)
        {
            ValidateRuleRegistration(repository, repository.Messages[index].Rules, diagnostics, $"/messages/{repository.Messages[index].Name}/rules");
            ValidateMemberRuleRegistration(repository, repository.Messages[index].Members, diagnostics, $"/messages/{repository.Messages[index].Name}");
        }
    }

    internal static void ValidateMemberRuleRegistration(OrchestraRepository repository, List<OrchestraMemberDefinition> members, List<OrchestraDiagnostic> diagnostics, string path)
    {
        for (int index = 0; index < members.Count; index++) ValidateRuleRegistration(repository, members[index].Rules, diagnostics, path + $"/members/{index}/rules");
    }

    internal static void ValidateRuleRegistration(OrchestraRepository repository, List<OrchestraRule> rules, List<OrchestraDiagnostic> diagnostics, string path)
    {
        for (int index = 0; index < rules.Count; index++)
        {
            if (!ContainsRule(repository.Rules, rules[index])) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unregistered-rule", $"Rule '{rules[index].Name}' is not registered by the repository.", path + $"/{index}");
        }
    }

    internal static bool ContainsRule(List<OrchestraRule> rules, OrchestraRule target)
    {
        for (int index = 0; index < rules.Count; index++) if (rules[index] == target) return true;
        return false;
    }

    internal static void ValidateWorkflow(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        ValidateWorkflowUniqueness(repository, diagnostics);
        for (int index = 0; index < repository.Flows.Count; index++)
        {
            OrchestraFlow flow = repository.Flows[index];
            string path = $"/actors/flows/{flow.Name}";
            if (!HasActor(repository, flow.SourceActor)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-flow-source", $"Flow '{flow.Name}' references missing source actor '{flow.SourceActor}'.", path);
            if (!HasActor(repository, flow.DestinationActor)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-flow-destination", $"Flow '{flow.Name}' references missing destination actor '{flow.DestinationActor}'.", path);
            for (int messageIndex = 0; messageIndex < flow.Messages.Count; messageIndex++)
            {
                if (!HasMessageName(repository, flow.Messages[messageIndex])) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-flow-message", $"Flow '{flow.Name}' references missing message '{flow.Messages[messageIndex]}'.", path);
            }
        }
        for (int index = 0; index < repository.StateMachines.Count; index++) ValidateStateMachine(repository, repository.StateMachines[index], diagnostics);
        for (int index = 0; index < repository.Messages.Count; index++)
        {
            OrchestraMessageDefinition message = repository.Messages[index];
            for (int responseIndex = 0; responseIndex < message.Responses.Count; responseIndex++) ValidateResponse(repository, message.Responses[responseIndex], diagnostics, $"/messages/{message.Name}/responses/{responseIndex}");
        }
    }

    internal static void ValidateWorkflowUniqueness(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < repository.Actors.Count; index++)
        {
            for (int previous = 0; previous < index; previous++) if (repository.Actors[index].Name == repository.Actors[previous].Name) AddDuplicate(diagnostics, "duplicate-actor-name", repository.Actors[index].Name, "/actors");
            OrchestraActor actor = repository.Actors[index];
            for (int timerIndex = 0; timerIndex < actor.Timers.Count; timerIndex++)
            {
                for (int previousTimer = 0; previousTimer < timerIndex; previousTimer++) if (actor.Timers[timerIndex].Name == actor.Timers[previousTimer].Name) AddDuplicate(diagnostics, "duplicate-timer-name", actor.Timers[timerIndex].Name, $"/actors/{actor.Name}/timers");
            }
            for (int variableIndex = 0; variableIndex < actor.Variables.Count; variableIndex++)
            {
                for (int previousVariable = 0; previousVariable < variableIndex; previousVariable++) if (actor.Variables[variableIndex].Name.Length != 0 && actor.Variables[variableIndex].Name == actor.Variables[previousVariable].Name) AddDuplicate(diagnostics, "duplicate-variable-name", actor.Variables[variableIndex].Name, $"/actors/{actor.Name}/variables");
            }
        }
        for (int index = 0; index < repository.Flows.Count; index++)
        {
            for (int previous = 0; previous < index; previous++) if (repository.Flows[index].Name == repository.Flows[previous].Name) AddDuplicate(diagnostics, "duplicate-flow-name", repository.Flows[index].Name, "/actors/flows");
        }
        for (int index = 0; index < repository.StateMachines.Count; index++)
        {
            for (int previous = 0; previous < index; previous++) if (repository.StateMachines[index].Actor == repository.StateMachines[previous].Actor && repository.StateMachines[index].Name == repository.StateMachines[previous].Name) AddDuplicate(diagnostics, "duplicate-state-machine-name", repository.StateMachines[index].Name, $"/actors/{repository.StateMachines[index].Actor}/states");
        }
    }

    internal static void ValidateStateMachine(OrchestraRepository repository, OrchestraStateMachine machine, List<OrchestraDiagnostic> diagnostics)
    {
        string path = $"/actors/{machine.Actor}/states/{machine.Name}";
        if (!HasActor(repository, machine.Actor)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-state-machine-actor", $"State machine '{machine.Name}' references missing actor '{machine.Actor}'.", path);
        if (!HasState(machine, machine.InitialState)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-initial-state", $"State machine '{machine.Name}' references missing initial state '{machine.InitialState}'.", path);
        for (int stateIndex = 0; stateIndex < machine.States.Count; stateIndex++)
        {
            OrchestraState state = machine.States[stateIndex];
            for (int previous = 0; previous < stateIndex; previous++) if (state.Name == machine.States[previous].Name) AddDuplicate(diagnostics, "duplicate-state-name", state.Name, path);
            for (int transitionIndex = 0; transitionIndex < state.Transitions.Count; transitionIndex++)
            {
                OrchestraTransition transition = state.Transitions[transitionIndex];
                string transitionPath = path + $"/{state.Name}/transitions/{transition.Name}";
                for (int previousTransition = 0; previousTransition < transitionIndex; previousTransition++) if (transition.Name == state.Transitions[previousTransition].Name) AddDuplicate(diagnostics, "duplicate-transition-name", transition.Name, path + $"/{state.Name}/transitions");
                if (!HasState(machine, transition.TargetState)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-transition-target", $"Transition '{transition.Name}' references missing target state '{transition.TargetState}'.", transitionPath);
            }
            ValidateActions(repository, state.EntryActions, diagnostics, path + $"/{state.Name}/onentry");
            ValidateActions(repository, state.ActivityActions, diagnostics, path + $"/{state.Name}/activity");
            ValidateActions(repository, state.ExitActions, diagnostics, path + $"/{state.Name}/onexit");
        }
    }

    internal static void ValidateResponse(OrchestraRepository repository, OrchestraResponse response, List<OrchestraDiagnostic> diagnostics, string path)
    {
        if (response.MessageName.Length != 0 && !HasMessageName(repository, response.MessageName)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-response-message", $"Response '{response.Name}' references missing message '{response.MessageName}'.", path);
        ValidateActions(repository, response.Actions, diagnostics, path + "/actions");
    }

    internal static void ValidateActions(OrchestraRepository repository, List<OrchestraAction> actions, List<OrchestraDiagnostic> diagnostics, string path)
    {
        for (int index = 0; index < actions.Count; index++)
        {
            OrchestraAction action = actions[index];
            string actionPath = path + $"/{index}";
            if (action.Kind == "fieldRef" && !HasField(repository, action.ReferenceId, action.Scenario)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-action-field", $"Action references missing field '{action.ReferenceId}'.", actionPath);
            else if (action.Kind == "componentRef" && !HasComponent(repository, action.ReferenceId, action.Scenario)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-action-component", $"Action references missing component '{action.ReferenceId}'.", actionPath);
            else if (action.Kind == "groupRef" && !HasGroup(repository, action.ReferenceId, action.Scenario)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-action-group", $"Action references missing group '{action.ReferenceId}'.", actionPath);
            else if (action.Kind == "messageRef" && !HasMessage(repository, action.ReferenceId, action.Scenario, action.MessageType, action.Name)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-action-message", $"Action references missing message '{action.Name}'.", actionPath);
            else if (action.Kind == "trigger") ValidateTrigger(repository, action, diagnostics, actionPath);
            else if (action.Kind == "timerSchedule") ValidateTimerAction(repository, action, diagnostics, actionPath);
            ValidateActions(repository, action.Actions, diagnostics, actionPath + "/actions");
        }
    }

    internal static void ValidateTrigger(OrchestraRepository repository, OrchestraAction action, List<OrchestraDiagnostic> diagnostics, string path)
    {
        OrchestraStateMachine? machine = FindStateMachine(repository, action.Actor, action.StateMachine);
        if (machine == null)
        {
            AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-trigger-state-machine", $"Trigger references missing state machine '{action.StateMachine}' for actor '{action.Actor}'.", path);
            return;
        }
        if (!HasTransition(machine, action.Name)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-trigger-transition", $"Trigger references missing transition '{action.Name}'.", path);
    }

    internal static void ValidateTimerAction(OrchestraRepository repository, OrchestraAction action, List<OrchestraDiagnostic> diagnostics, string path)
    {
        if (!HasTimer(repository, action.Actor, action.Name)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-timer-reference", $"Timer action references missing timer '{action.Name}' for actor '{action.Actor}'.", path);
        if (action.Operation != "START" && action.Operation != "CANCEL" && action.Operation != "RESET") AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-timer-operation", $"Timer operation '{action.Operation}' is invalid.", path);
    }

    internal static void ValidateInterfaces(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < repository.Interfaces.Count; index++)
        {
            OrchestraInterface orchestraInterface = repository.Interfaces[index];
            string path = $"/interfaces/{orchestraInterface.Name}";
            for (int previous = 0; previous < index; previous++) if (orchestraInterface.Name == repository.Interfaces[previous].Name) AddDuplicate(diagnostics, "duplicate-interface-name", orchestraInterface.Name, "/interfaces");
            ValidateServices(orchestraInterface.Services, diagnostics, path + "/services");
            ValidateProtocolList(orchestraInterface.UserInterfaces, "application", diagnostics, path + "/userInterfaces");
            ValidateProtocolList(orchestraInterface.Encodings, "presentation", diagnostics, path + "/encodings");
            ValidateProtocolList(orchestraInterface.SessionProtocols, "session", diagnostics, path + "/sessionProtocols");
            ValidateProtocolList(orchestraInterface.Protocols, "", diagnostics, path + "/protocols");
            ValidateProtocolList(orchestraInterface.Transports, "transport", diagnostics, path + "/transports");
            ValidateTransportAddresses(orchestraInterface.Transports, diagnostics, path + "/transports");
            ValidateSessions(repository, orchestraInterface, diagnostics, path);
            ValidateInterfaceLinks(repository, orchestraInterface, diagnostics, path);
        }
        ValidateRepositoryLinkList(repository, diagnostics);
    }

    internal static void ValidateServices(List<OrchestraService> services, List<OrchestraDiagnostic> diagnostics, string path)
    {
        for (int index = 0; index < services.Count; index++)
        {
            for (int previous = 0; previous < index; previous++) if (services[index].Name == services[previous].Name) AddDuplicate(diagnostics, "duplicate-service-name", services[index].Name, path);
            if (services[index].Layer.Length != 0 && services[index].Layer != "application") AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-service-layer", $"Service '{services[index].Name}' must use the application layer.", path + $"/{services[index].Name}");
        }
    }

    internal static void ValidateProtocolList(List<OrchestraProtocol> protocols, string expectedLayer, List<OrchestraDiagnostic> diagnostics, string path)
    {
        for (int index = 0; index < protocols.Count; index++)
        {
            OrchestraProtocol protocol = protocols[index];
            for (int previous = 0; previous < index; previous++) if (protocol.Name == protocols[previous].Name) AddDuplicate(diagnostics, "duplicate-protocol-name", protocol.Name, path);
            if (expectedLayer.Length != 0 && protocol.Layer != expectedLayer) AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-protocol-layer", $"Protocol '{protocol.Name}' has layer '{protocol.Layer}' instead of '{expectedLayer}'.", path + $"/{protocol.Name}");
        }
    }

    internal static void ValidateTransportAddresses(List<OrchestraProtocol> transports, List<OrchestraDiagnostic> diagnostics, string path)
    {
        for (int index = 0; index < transports.Count; index++)
        {
            if (transports[index].Address.Trim().Length == 0) AddDiagnostic(diagnostics, ValidationSeverity.Error, "missing-transport-address", $"Transport '{transports[index].Name}' has no address.", path + $"/{transports[index].Name}");
        }
    }

    internal static void ValidateSessions(OrchestraRepository repository, OrchestraInterface orchestraInterface, List<OrchestraDiagnostic> diagnostics, string interfacePath)
    {
        for (int index = 0; index < orchestraInterface.Sessions.Count; index++)
        {
            OrchestraSession session = orchestraInterface.Sessions[index];
            string path = interfacePath + $"/sessions/{session.Name}";
            for (int previous = 0; previous < index; previous++) if (session.Name == orchestraInterface.Sessions[previous].Name) AddDuplicate(diagnostics, "duplicate-session-name", session.Name, interfacePath + "/sessions");
            for (int identifierIndex = 0; identifierIndex < session.IdentifierEntries.Count; identifierIndex++)
            {
                for (int previousIdentifier = 0; previousIdentifier < identifierIndex; previousIdentifier++) if (session.IdentifierEntries[identifierIndex].Name == session.IdentifierEntries[previousIdentifier].Name) AddDuplicate(diagnostics, "duplicate-session-identifier", session.IdentifierEntries[identifierIndex].Name, path + "/identifiers");
            }
            if (session.Role.Length != 0 && session.Role != "initiator" && session.Role != "acceptor" && session.Role != "client" && session.Role != "server") AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-session-role", $"Session role '{session.Role}' is invalid.", path);
            ValidateServiceOverrides(orchestraInterface, session, diagnostics, path);
            ValidateProtocolOverrides(orchestraInterface.UserInterfaces, session.UserInterfaceOverrides, diagnostics, path + "/userInterfaces");
            ValidateProtocolOverrides(orchestraInterface.Encodings, session.EncodingOverrides, diagnostics, path + "/encodings");
            ValidateProtocolOverrides(orchestraInterface.SessionProtocols, session.SessionProtocolOverrides, diagnostics, path + "/sessionProtocols");
            ValidateProtocolOverrides(orchestraInterface.Protocols, session.ProtocolOverrides, diagnostics, path + "/protocols");
            ValidateProtocolOverrides(orchestraInterface.Transports, session.TransportOverrides, diagnostics, path + "/transports");
            ValidateTransportAddresses(session.TransportOverrides, diagnostics, path + "/transports");
        }
    }

    internal static void ValidateServiceOverrides(OrchestraInterface orchestraInterface, OrchestraSession session, List<OrchestraDiagnostic> diagnostics, string path)
    {
        for (int index = 0; index < session.ServiceOverrides.Count; index++)
        {
            string name = session.ServiceOverrides[index].Name;
            bool resolved = false;
            for (int serviceIndex = 0; serviceIndex < orchestraInterface.Services.Count; serviceIndex++) if (orchestraInterface.Services[serviceIndex].Name == name) resolved = true;
            if (!resolved) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-service-override", $"Session service override '{name}' has no interface definition.", path + "/services");
        }
    }

    internal static void ValidateProtocolOverrides(List<OrchestraProtocol> definitions, List<OrchestraProtocol> overrides, List<OrchestraDiagnostic> diagnostics, string path)
    {
        for (int index = 0; index < overrides.Count; index++)
        {
            string name = overrides[index].Name;
            bool resolved = false;
            for (int definitionIndex = 0; definitionIndex < definitions.Count; definitionIndex++) if (definitions[definitionIndex].Name == name) resolved = true;
            if (!resolved) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-protocol-override", $"Session protocol override '{name}' has no interface definition.", path);
        }
    }

    internal static void ValidateInterfaceLinks(OrchestraRepository repository, OrchestraInterface orchestraInterface, List<OrchestraDiagnostic> diagnostics, string path)
    {
        for (int index = 0; index < orchestraInterface.Services.Count; index++) ValidateOneRepositoryLink(repository, orchestraInterface.Services[index].OrchestrationUri, diagnostics, path + $"/services/{orchestraInterface.Services[index].Name}");
        ValidateProtocolLinks(repository, orchestraInterface.UserInterfaces, diagnostics, path + "/userInterfaces");
        ValidateProtocolLinks(repository, orchestraInterface.Encodings, diagnostics, path + "/encodings");
        ValidateProtocolLinks(repository, orchestraInterface.SessionProtocols, diagnostics, path + "/sessionProtocols");
        ValidateProtocolLinks(repository, orchestraInterface.Protocols, diagnostics, path + "/protocols");
        ValidateProtocolLinks(repository, orchestraInterface.Transports, diagnostics, path + "/transports");
        for (int sessionIndex = 0; sessionIndex < orchestraInterface.Sessions.Count; sessionIndex++)
        {
            OrchestraSession session = orchestraInterface.Sessions[sessionIndex];
            string sessionPath = path + $"/sessions/{session.Name}";
            for (int serviceIndex = 0; serviceIndex < session.ServiceOverrides.Count; serviceIndex++) ValidateOneRepositoryLink(repository, session.ServiceOverrides[serviceIndex].OrchestrationUri, diagnostics, sessionPath + "/services");
            ValidateProtocolLinks(repository, session.UserInterfaceOverrides, diagnostics, sessionPath + "/userInterfaces");
            ValidateProtocolLinks(repository, session.EncodingOverrides, diagnostics, sessionPath + "/encodings");
            ValidateProtocolLinks(repository, session.SessionProtocolOverrides, diagnostics, sessionPath + "/sessionProtocols");
            ValidateProtocolLinks(repository, session.ProtocolOverrides, diagnostics, sessionPath + "/protocols");
            ValidateProtocolLinks(repository, session.TransportOverrides, diagnostics, sessionPath + "/transports");
        }
    }

    internal static void ValidateProtocolLinks(OrchestraRepository repository, List<OrchestraProtocol> protocols, List<OrchestraDiagnostic> diagnostics, string path)
    {
        for (int index = 0; index < protocols.Count; index++) ValidateOneRepositoryLink(repository, protocols[index].OrchestrationUri, diagnostics, path + $"/{protocols[index].Name}");
    }

    internal static void ValidateOneRepositoryLink(OrchestraRepository repository, string uri, List<OrchestraDiagnostic> diagnostics, string path)
    {
        if (uri.Length == 0) return;
        if (!IsLinkLexicallyValid(uri)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-repository-link", $"Repository link '{uri}' is not a valid URI reference.", path);
        bool registered = false;
        for (int index = 0; index < repository.RepositoryLinks.Count; index++) if (repository.RepositoryLinks[index] == uri) registered = true;
        if (!registered) AddDiagnostic(diagnostics, ValidationSeverity.Error, "unregistered-repository-link", $"Repository link '{uri}' is not registered by the repository.", path);
    }

    internal static void ValidateRepositoryLinkList(OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < repository.RepositoryLinks.Count; index++)
        {
            string link = repository.RepositoryLinks[index];
            if (!IsLinkLexicallyValid(link)) AddDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-repository-link", $"Repository link '{link}' is not a valid URI reference.", "/repositoryLinks");
            for (int previous = 0; previous < index; previous++) if (link == repository.RepositoryLinks[previous]) AddDuplicate(diagnostics, "duplicate-repository-link", link, "/repositoryLinks");
        }
    }

    internal static bool IsLinkLexicallyValid(string uri)
    {
        if (uri.Trim().Length == 0) return false;
        for (int index = 0; index < uri.Length; index++) if (char.IsWhiteSpace(uri[index]) || char.IsControl(uri[index])) return false;
        return true;
    }

    internal static bool HasErrors(List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < diagnostics.Count; index++) if (diagnostics[index].Severity == ValidationSeverity.Error) return true;
        return false;
    }

    internal static void AddDuplicate(List<OrchestraDiagnostic> diagnostics, string code, string value, string path)
    {
        AddDiagnostic(diagnostics, ValidationSeverity.Error, code, $"The value '{value}' is duplicated.", path);
    }

    internal static void AddDiagnostic(List<OrchestraDiagnostic> diagnostics, ValidationSeverity severity, string code, string message, string path)
    {
        OrchestraDiagnostic diagnostic = new OrchestraDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.ElementPath = path;
        diagnostics.Add(diagnostic);
    }

    internal static string ReadAttribute(Dictionary<string, string> attributes, string name)
    {
        if (attributes.ContainsKey(name)) return attributes[name];
        return "";
    }

    internal static bool HasDatatype(OrchestraRepository repository, string name)
    {
        return FindDatatype(repository, name) != null;
    }

    internal static OrchestraDatatypeDefinition? FindDatatype(OrchestraRepository repository, string name)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.FindDatatype(name);
        for (int index = 0; index < repository.Datatypes.Count; index++) if (repository.Datatypes[index].Name == name) return repository.Datatypes[index];
        return null;
    }

    internal static bool HasEncodingStandard(OrchestraRepository repository, string name)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.EncodingStandards.Contains(name);
        for (int index = 0; index < repository.EncodingStandards.Count; index++) if (repository.EncodingStandards[index].Name == name) return true;
        return false;
    }

    internal static bool HasScenario(OrchestraRepository repository, string name)
    {
        return FindScenario(repository, name) != null;
    }

    internal static OrchestraScenarioDefinition? FindScenario(OrchestraRepository repository, string name)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.FindScenario(name);
        for (int index = 0; index < repository.Scenarios.Count; index++) if (repository.Scenarios[index].Name == name) return repository.Scenarios[index];
        return null;
    }

    internal static bool HasSection(OrchestraRepository repository, string name)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.Sections.Contains(name);
        for (int index = 0; index < repository.Sections.Count; index++) if (repository.Sections[index].Name == name) return true;
        return false;
    }

    internal static bool HasCategory(OrchestraRepository repository, string name)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.Categories.Contains(name);
        for (int index = 0; index < repository.Categories.Count; index++) if (repository.Categories[index].Name == name) return true;
        return false;
    }

    internal static bool HasCodeSet(OrchestraRepository repository, string name, string scenario)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.CodeSets.Contains(OrchestraSemanticLookup.ScenarioKey(name, scenario));
        for (int index = 0; index < repository.CodeSets.Count; index++) if (repository.CodeSets[index].Name == name && ScenarioMatches(repository.CodeSets[index].Scenario, scenario)) return true;
        return false;
    }

    internal static bool HasField(OrchestraRepository repository, string id, string scenario)
    {
        return FindField(repository, id, scenario) != null;
    }

    internal static bool HasFieldAnyScenario(OrchestraRepository repository, string id)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.FieldIds.Contains(id);
        for (int index = 0; index < repository.Fields.Count; index++) if (repository.Fields[index].Id == id) return true;
        return false;
    }

    internal static OrchestraFieldDefinition? FindField(OrchestraRepository repository, string id, string scenario)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.FindField(id, scenario);
        for (int index = 0; index < repository.Fields.Count; index++) if (repository.Fields[index].Id == id && ScenarioMatches(repository.Fields[index].Scenario, scenario)) return repository.Fields[index];
        return null;
    }

    internal static bool HasComponent(OrchestraRepository repository, string id, string scenario)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.Components.Contains(OrchestraSemanticLookup.ScenarioKey(id, scenario));
        for (int index = 0; index < repository.Components.Count; index++) if (repository.Components[index].Id == id && ScenarioMatches(repository.Components[index].Scenario, scenario)) return true;
        return false;
    }

    internal static OrchestraComponentDefinition? FindComponent(OrchestraRepository repository, string id, string scenario)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.FindComponent(id, scenario);
        for (int index = 0; index < repository.Components.Count; index++) if (repository.Components[index].Id == id && ScenarioMatches(repository.Components[index].Scenario, scenario)) return repository.Components[index];
        return null;
    }

    internal static bool HasGroup(OrchestraRepository repository, string id, string scenario)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.Groups.Contains(OrchestraSemanticLookup.ScenarioKey(id, scenario));
        for (int index = 0; index < repository.Groups.Count; index++) if (repository.Groups[index].Id == id && ScenarioMatches(repository.Groups[index].Scenario, scenario)) return true;
        return false;
    }

    internal static OrchestraGroupDefinition? FindGroup(OrchestraRepository repository, string id, string scenario)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.FindGroup(id, scenario);
        for (int index = 0; index < repository.Groups.Count; index++) if (repository.Groups[index].Id == id && ScenarioMatches(repository.Groups[index].Scenario, scenario)) return repository.Groups[index];
        return null;
    }

    internal static bool HasMessage(OrchestraRepository repository, string id, string scenario, string messageType, string name)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.HasMessage(id, scenario, messageType, name);
        for (int index = 0; index < repository.Messages.Count; index++)
        {
            OrchestraMessageDefinition message = repository.Messages[index];
            bool identifierMatches = id.Length != 0 && message.Id == id;
            bool typeMatches = messageType.Length != 0 && message.MessageType == messageType;
            bool nameMatches = name.Length != 0 && message.Name == name;
            if ((identifierMatches || typeMatches || nameMatches) && ScenarioMatches(message.Scenario, scenario)) return true;
        }
        return false;
    }

    internal static bool HasMessageName(OrchestraRepository repository, string name)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.MessageNames.Contains(name);
        for (int index = 0; index < repository.Messages.Count; index++) if (repository.Messages[index].Name == name) return true;
        return false;
    }

    internal static bool HasFlow(OrchestraRepository repository, string name)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.Flows.Contains(name);
        for (int index = 0; index < repository.Flows.Count; index++) if (repository.Flows[index].Name == name) return true;
        return false;
    }

    internal static bool HasActor(OrchestraRepository repository, string name)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.Actors.Contains(name);
        for (int index = 0; index < repository.Actors.Count; index++) if (repository.Actors[index].Name == name) return true;
        return false;
    }

    internal static OrchestraStateMachine? FindStateMachine(OrchestraRepository repository, string actor, string name)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.FindStateMachine(actor, name);
        for (int index = 0; index < repository.StateMachines.Count; index++) if (repository.StateMachines[index].Actor == actor && repository.StateMachines[index].Name == name) return repository.StateMachines[index];
        return null;
    }

    internal static bool HasState(OrchestraStateMachine machine, string name)
    {
        for (int index = 0; index < machine.States.Count; index++) if (machine.States[index].Name == name) return true;
        return false;
    }

    internal static bool HasTransition(OrchestraStateMachine machine, string name)
    {
        for (int stateIndex = 0; stateIndex < machine.States.Count; stateIndex++)
        {
            for (int transitionIndex = 0; transitionIndex < machine.States[stateIndex].Transitions.Count; transitionIndex++) if (machine.States[stateIndex].Transitions[transitionIndex].Name == name) return true;
        }
        return false;
    }

    internal static bool HasTimer(OrchestraRepository repository, string actor, string name)
    {
        OrchestraSemanticLookup? lookup = CurrentLookup.Value;
        if (lookup != null) return lookup.Timers.Contains(OrchestraSemanticLookup.PairKey(actor, name));
        for (int index = 0; index < repository.Timers.Count; index++) if (repository.Timers[index].Actor == actor && repository.Timers[index].Name == name) return true;
        return false;
    }

    internal static bool ScenarioMatches(string definitionScenario, string referenceScenario)
    {
        if (definitionScenario == referenceScenario) return true;
        return definitionScenario == "base" && (referenceScenario.Length == 0 || referenceScenario == "base");
    }

    internal static bool IsCountDatatype(OrchestraRepository repository, OrchestraFieldDefinition field)
    {
        string type = field.Type;
        if (field.CodeSet.Length != 0)
        {
            for (int index = 0; index < repository.CodeSets.Count; index++) if (repository.CodeSets[index].Name == field.CodeSet) type = repository.CodeSets[index].Type;
        }
        string root = ResolveRootDatatype(repository, type).ToLowerInvariant();
        return root == "numingroup" || root == "length" || root.Contains("int");
    }
}
