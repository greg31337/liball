using System;
using System.Collections.Generic;
using System.Text;

namespace fix;

internal static class GeneratedApiPlanner
{
    internal static GeneratedApiPlan PlanGeneration(FixDictionary dictionary, ModelGenerationOptions options)
    {
        GeneratedApiPlan plan = new GeneratedApiPlan();
        plan.Dictionary = DictionaryComposition.CloneDictionaryGraph(dictionary, dictionary.Options, DictionaryComposition.ResolveDictionaryOrigin(dictionary));
        if (!VersionMatchesSelection(plan.Dictionary.Identity, options.Version))
        {
            plan.CanEmit = false;
            AddPlanningDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "generation-version-mismatch", "The supplied dictionary does not match the requested generation version.", "/options/version");
            return plan;
        }

        List<string> messageSelections = NormalizeTextSelections(options.MessageNames);
        List<string> categorySelections = NormalizeTextSelections(options.CategoryNames);
        AddUnknownMessageSelections(plan.Dictionary, messageSelections, plan.Diagnostics);
        AddUnknownCategorySelections(plan.Dictionary, categorySelections, plan.Diagnostics);
        string scenario = ResolveGenerationScenario(plan.Dictionary, options.Scenario);
        AddUnknownScenarioSelection(plan.Dictionary, scenario, plan.Diagnostics);
        List<FixMessageDefinition> selectedMessages = SelectGenerationMessages(plan.Dictionary, messageSelections, categorySelections, scenario);
        plan.Dictionary.Messages = selectedMessages;

        List<int> fields = [];
        List<int> components = [];
        List<int> groups = [];
        for (int index = 0; index < options.FieldTags.Count; index++) AddPlanningInteger(fields, options.FieldTags[index]);
        for (int index = 0; index < selectedMessages.Count; index++) ScanGenerationMembers(selectedMessages[index].Members, fields, components, groups);
        ExpandStructuralDependencies(plan.Dictionary, fields, components, groups, plan.Diagnostics);
        ExpandFieldDependencies(plan.Dictionary, fields, plan.Diagnostics);
        FilterGenerationDefinitions(plan.Dictionary, fields, components, groups, plan.Diagnostics);
        FilterGenerationScenarios(plan.Dictionary, selectedMessages, scenario);
        if (selectedMessages.Count == 0 && (messageSelections.Count != 0 || categorySelections.Count != 0))
        {
            AddPlanningDiagnostic(plan.Diagnostics, ValidationSeverity.Warning, "empty-message-selection", "No message definitions matched the requested generation selection.", "/messages");
        }
        return plan;
    }

    internal static bool VersionMatchesSelection(FixDictionaryIdentity identity, string requestedVersion)
    {
        string requested = NormalizeVersionText(requestedVersion);
        if (requested.Length == 0) return true;
        if (requested == NormalizeVersionText(identity.Name)) return true;
        if (requested == NormalizeVersionText(identity.BeginString)) return true;
        if (requested == NormalizeVersionText(identity.ApplicationVersion)) return true;
        if (requested == NormalizeVersionText(CSharpNameResolver.StandardApplicationVersionName(identity.ApplicationVersion))) return true;
        if (requested == NormalizeVersionText(CSharpNameResolver.StandardApplicationVersionName(identity.BeginString))) return true;
        if (identity.HasExtensionPack)
        {
            string extension = identity.ExtensionPack.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (requested == NormalizeVersionText(identity.ApplicationVersion + "EP" + extension)) return true;
            if (requested == NormalizeVersionText(identity.Name + "EP" + extension)) return true;
            if (requested == NormalizeVersionText(CSharpNameResolver.StandardApplicationVersionName(identity.ApplicationVersion) + "EP" + extension)) return true;
        }
        return false;
    }

    internal static string NormalizeVersionText(string value)
    {
        string source = value == null ? "" : value.Trim();
        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < source.Length; index++)
        {
            char character = source[index];
            if (character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9') builder.Append(char.ToUpperInvariant(character));
        }
        return builder.ToString();
    }

    internal static List<string> NormalizeTextSelections(List<string> source)
    {
        List<string> result = [];
        for (int index = 0; index < source.Count; index++)
        {
            string value = source[index] == null ? "" : source[index].Trim();
            if (value.Length != 0 && !ContainsPlanningText(result, value)) result.Add(value);
        }
        result.Sort((left, right) => string.CompareOrdinal(left.ToUpperInvariant(), right.ToUpperInvariant()));
        return result;
    }

    internal static void AddUnknownMessageSelections(FixDictionary dictionary, List<string> selections, List<GenerationDiagnostic> diagnostics)
    {
        for (int selectionIndex = 0; selectionIndex < selections.Count; selectionIndex++)
        {
            bool found = false;
            for (int messageIndex = 0; messageIndex < dictionary.Messages.Count; messageIndex++)
            {
                FixMessageDefinition message = dictionary.Messages[messageIndex];
                if (GenerationNamesEqual(dictionary, message.Name, selections[selectionIndex]) || message.MsgType == selections[selectionIndex]) found = true;
            }
            if (!found) AddPlanningDiagnostic(diagnostics, ValidationSeverity.Warning, "unknown-generation-message", "No message name or MsgType matches the requested selection.", "/options/message-names/" + selections[selectionIndex]);
        }
    }

    internal static void AddUnknownCategorySelections(FixDictionary dictionary, List<string> selections, List<GenerationDiagnostic> diagnostics)
    {
        for (int selectionIndex = 0; selectionIndex < selections.Count; selectionIndex++)
        {
            bool found = false;
            for (int messageIndex = 0; messageIndex < dictionary.Messages.Count; messageIndex++)
            {
                if (GenerationNamesEqual(dictionary, dictionary.Messages[messageIndex].Category, selections[selectionIndex])) found = true;
            }
            if (!found) AddPlanningDiagnostic(diagnostics, ValidationSeverity.Warning, "unknown-generation-category", "No message category matches the requested selection.", "/options/category-names/" + selections[selectionIndex]);
        }
    }

    internal static string ResolveGenerationScenario(FixDictionary dictionary, string requestedScenario)
    {
        string scenario = requestedScenario == null ? "" : requestedScenario.Trim();
        if (scenario.Length == 0) scenario = dictionary.Identity.Scenario.Trim();
        return scenario;
    }

    internal static void AddUnknownScenarioSelection(FixDictionary dictionary, string scenario, List<GenerationDiagnostic> diagnostics)
    {
        if (scenario.Length == 0) return;
        bool found = false;
        for (int index = 0; index < dictionary.Scenarios.Count; index++) if (GenerationNamesEqual(dictionary, dictionary.Scenarios[index].Name, scenario)) found = true;
        for (int index = 0; index < dictionary.Messages.Count; index++) if (GenerationNamesEqual(dictionary, NormalizePlanningScenario(dictionary.Messages[index].Scenario), NormalizePlanningScenario(scenario))) found = true;
        if (!found) AddPlanningDiagnostic(diagnostics, ValidationSeverity.Warning, "unknown-generation-scenario", "The requested scenario is not declared; base message definitions are used when available.", "/options/scenario");
    }

    internal static List<FixMessageDefinition> SelectGenerationMessages(FixDictionary dictionary, List<string> messageSelections, List<string> categorySelections, string scenario)
    {
        List<FixMessageDefinition> candidates = new List<FixMessageDefinition>(dictionary.Messages);
        candidates.Sort((left, right) => CSharpNameResolver.CompareMessageDefinitions(left, right));
        List<FixMessageDefinition> scenarioMatches = [];
        if (scenario.Length == 0)
        {
            for (int index = 0; index < candidates.Count; index++) scenarioMatches.Add(candidates[index]);
        }
        else
        {
            List<string> scenarioCandidates = BuildGenerationScenarioCandidates(dictionary, scenario);
            List<string> selectedTypes = [];
            for (int scenarioIndex = 0; scenarioIndex < scenarioCandidates.Count; scenarioIndex++)
            {
                for (int messageIndex = 0; messageIndex < candidates.Count; messageIndex++)
                {
                    FixMessageDefinition message = candidates[messageIndex];
                    if (ContainsExactPlanningText(selectedTypes, message.MsgType)) continue;
                    if (GenerationNamesEqual(dictionary, NormalizePlanningScenario(message.Scenario), scenarioCandidates[scenarioIndex]))
                    {
                        scenarioMatches.Add(message);
                        selectedTypes.Add(message.MsgType);
                    }
                }
            }
        }

        List<FixMessageDefinition> result = [];
        for (int index = 0; index < scenarioMatches.Count; index++)
        {
            FixMessageDefinition message = scenarioMatches[index];
            if (MessagePassesGenerationSelection(dictionary, message, messageSelections, categorySelections)) result.Add(message);
        }
        result.Sort((left, right) => CSharpNameResolver.CompareMessageDefinitions(left, right));
        return result;
    }

    internal static bool MessagePassesGenerationSelection(FixDictionary dictionary, FixMessageDefinition message, List<string> messageSelections, List<string> categorySelections)
    {
        if (messageSelections.Count == 0 && categorySelections.Count == 0) return true;
        for (int index = 0; index < messageSelections.Count; index++)
        {
            if (GenerationNamesEqual(dictionary, message.Name, messageSelections[index]) || message.MsgType == messageSelections[index]) return true;
        }
        for (int index = 0; index < categorySelections.Count; index++) if (GenerationNamesEqual(dictionary, message.Category, categorySelections[index])) return true;
        return false;
    }

    internal static List<string> BuildGenerationScenarioCandidates(FixDictionary dictionary, string scenario)
    {
        List<string> result = [];
        string current = NormalizePlanningScenario(scenario);
        int remaining = dictionary.Scenarios.Count;
        while (current.Length != 0 && remaining >= 0)
        {
            AddPlanningText(result, current);
            string parent = FindGenerationScenarioParent(dictionary, current);
            if (parent.Length == 0 || ContainsPlanningText(result, parent)) break;
            current = NormalizePlanningScenario(parent);
            remaining--;
        }
        AddPlanningText(result, "BASE");
        return result;
    }

    internal static string FindGenerationScenarioParent(FixDictionary dictionary, string scenario)
    {
        for (int index = 0; index < dictionary.Scenarios.Count; index++)
        {
            if (GenerationNamesEqual(dictionary, NormalizePlanningScenario(dictionary.Scenarios[index].Name), scenario)) return dictionary.Scenarios[index].ParentName;
        }
        return "";
    }

    internal static void ScanGenerationMembers(List<FixMemberDefinition> members, List<int> fields, List<int> components, List<int> groups)
    {
        for (int index = 0; index < members.Count; index++)
        {
            FixMemberDefinition member = members[index];
            if (member.Kind == FixMemberKind.Field) AddPlanningInteger(fields, member.ReferenceId);
            else if (member.Kind == FixMemberKind.Component) AddPlanningInteger(components, member.ReferenceId);
            else if (member.Kind == FixMemberKind.Group) AddPlanningInteger(groups, member.ReferenceId);
        }
    }

    internal static void ExpandStructuralDependencies(FixDictionary dictionary, List<int> fields, List<int> components, List<int> groups, List<GenerationDiagnostic> diagnostics)
    {
        List<int> missingComponents = [];
        List<int> missingGroups = [];
        int componentIndex = 0;
        int groupIndex = 0;
        while (componentIndex < components.Count || groupIndex < groups.Count)
        {
            while (componentIndex < components.Count)
            {
                int id = components[componentIndex];
                componentIndex++;
                FixComponentDefinition? component = FindGenerationComponent(dictionary.Components, id);
                if (component == null)
                {
                    if (!ContainsPlanningInteger(missingComponents, id))
                    {
                        missingComponents.Add(id);
                        AddPlanningDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-generation-component", "A selected model references a missing component definition.", "/components/" + CSharpNameResolver.IntegerNameText(id));
                    }
                }
                else ScanGenerationMembers(component.Members, fields, components, groups);
            }
            while (groupIndex < groups.Count)
            {
                int id = groups[groupIndex];
                groupIndex++;
                FixGroupDefinition? group = FindGenerationGroup(dictionary.Groups, id);
                if (group == null)
                {
                    if (!ContainsPlanningInteger(missingGroups, id))
                    {
                        missingGroups.Add(id);
                        AddPlanningDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-generation-group", "A selected model references a missing group definition.", "/groups/" + CSharpNameResolver.IntegerNameText(id));
                    }
                }
                else
                {
                    AddPlanningInteger(fields, group.CountTag);
                    AddPlanningInteger(fields, group.DelimiterTag);
                    ScanGenerationMembers(group.Members, fields, components, groups);
                }
            }
        }
    }

    internal static void ExpandFieldDependencies(FixDictionary dictionary, List<int> fields, List<GenerationDiagnostic> diagnostics)
    {
        List<int> missing = [];
        int index = 0;
        while (index < fields.Count)
        {
            int tag = fields[index];
            index++;
            FixFieldDefinition? field = FindGenerationField(dictionary.Fields, tag);
            if (field == null)
            {
                if (!ContainsPlanningInteger(missing, tag))
                {
                    missing.Add(tag);
                    AddPlanningDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-generation-field", "A selected model references a missing field definition.", "/fields/" + CSharpNameResolver.IntegerNameText(tag));
                }
            }
            else
            {
                if (field.HasDiscriminatorTag) AddPlanningInteger(fields, field.DiscriminatorTag);
                if (field.HasLengthTag) AddPlanningInteger(fields, field.LengthTag);
                if (field.HasDataTag) AddPlanningInteger(fields, field.DataTag);
            }
        }
    }

    internal static void FilterGenerationDefinitions(FixDictionary dictionary, List<int> fields, List<int> components, List<int> groups, List<GenerationDiagnostic> diagnostics)
    {
        for (int index = dictionary.Fields.Count - 1; index >= 0; index--) if (!ContainsPlanningInteger(fields, dictionary.Fields[index].Tag)) dictionary.Fields.RemoveAt(index);
        for (int index = dictionary.Components.Count - 1; index >= 0; index--) if (!ContainsPlanningInteger(components, dictionary.Components[index].Id)) dictionary.Components.RemoveAt(index);
        for (int index = dictionary.Groups.Count - 1; index >= 0; index--) if (!ContainsPlanningInteger(groups, dictionary.Groups[index].Id)) dictionary.Groups.RemoveAt(index);

        List<string> codeSets = [];
        List<string> datatypes = [];
        for (int index = 0; index < dictionary.Fields.Count; index++)
        {
            AddPlanningText(codeSets, dictionary.Fields[index].CodeSetName);
            AddPlanningText(datatypes, dictionary.Fields[index].DatatypeName);
        }
        ExpandDatatypeDependencies(dictionary, datatypes);
        AddMissingDefinitionDiagnostics(dictionary, codeSets, datatypes, diagnostics);
        for (int index = dictionary.CodeSets.Count - 1; index >= 0; index--) if (!ContainsPlanningText(codeSets, dictionary.CodeSets[index].Name)) dictionary.CodeSets.RemoveAt(index);
        for (int index = dictionary.Datatypes.Count - 1; index >= 0; index--) if (!ContainsPlanningText(datatypes, dictionary.Datatypes[index].Name)) dictionary.Datatypes.RemoveAt(index);
    }

    internal static void AddMissingDefinitionDiagnostics(FixDictionary dictionary, List<string> codeSets, List<string> datatypes, List<GenerationDiagnostic> diagnostics)
    {
        for (int codeIndex = 0; codeIndex < codeSets.Count; codeIndex++)
        {
            bool found = false;
            for (int definitionIndex = 0; definitionIndex < dictionary.CodeSets.Count; definitionIndex++) if (string.Equals(dictionary.CodeSets[definitionIndex].Name, codeSets[codeIndex], StringComparison.OrdinalIgnoreCase)) found = true;
            if (!found) AddPlanningDiagnostic(diagnostics, ValidationSeverity.Error, "unresolved-generation-code-set", "A selected field references a missing code-set definition.", "/code-sets/" + codeSets[codeIndex]);
        }
        for (int datatypeIndex = 0; datatypeIndex < datatypes.Count; datatypeIndex++)
        {
            bool found = false;
            for (int definitionIndex = 0; definitionIndex < dictionary.Datatypes.Count; definitionIndex++) if (string.Equals(dictionary.Datatypes[definitionIndex].Name, datatypes[datatypeIndex], StringComparison.OrdinalIgnoreCase)) found = true;
            if (!found)
            {
                ValidationSeverity severity = dictionary.Options.AllowUnknownDatatypes ? ValidationSeverity.Warning : ValidationSeverity.Error;
                AddPlanningDiagnostic(diagnostics, severity, "unresolved-generation-datatype", "A selected field references a missing datatype definition.", "/datatypes/" + datatypes[datatypeIndex]);
            }
        }
    }

    internal static void ExpandDatatypeDependencies(FixDictionary dictionary, List<string> datatypes)
    {
        int index = 0;
        while (index < datatypes.Count)
        {
            string name = datatypes[index];
            index++;
            for (int definitionIndex = 0; definitionIndex < dictionary.Datatypes.Count; definitionIndex++)
            {
                if (string.Equals(dictionary.Datatypes[definitionIndex].Name, name, StringComparison.OrdinalIgnoreCase)) AddPlanningText(datatypes, dictionary.Datatypes[definitionIndex].BaseTypeName);
            }
        }
    }

    internal static void FilterGenerationScenarios(FixDictionary dictionary, List<FixMessageDefinition> messages, string selectedScenario)
    {
        List<string> needed = [];
        for (int index = 0; index < messages.Count; index++) AddPlanningText(needed, NormalizePlanningScenario(messages[index].Scenario));
        if (selectedScenario.Length != 0)
        {
            List<string> candidates = BuildGenerationScenarioCandidates(dictionary, selectedScenario);
            for (int index = 0; index < candidates.Count; index++) AddPlanningText(needed, candidates[index]);
        }
        int scenarioIndex = 0;
        while (scenarioIndex < needed.Count)
        {
            string parent = FindGenerationScenarioParent(dictionary, needed[scenarioIndex]);
            AddPlanningText(needed, NormalizePlanningScenario(parent));
            scenarioIndex++;
        }
        for (int index = dictionary.Scenarios.Count - 1; index >= 0; index--) if (!ContainsPlanningText(needed, NormalizePlanningScenario(dictionary.Scenarios[index].Name))) dictionary.Scenarios.RemoveAt(index);
    }

    internal static FixComponentDefinition? FindGenerationComponent(List<FixComponentDefinition> definitions, int id)
    {
        for (int index = 0; index < definitions.Count; index++) if (definitions[index].Id == id) return definitions[index];
        return null;
    }

    internal static FixGroupDefinition? FindGenerationGroup(List<FixGroupDefinition> definitions, int id)
    {
        for (int index = 0; index < definitions.Count; index++) if (definitions[index].Id == id) return definitions[index];
        return null;
    }

    internal static FixFieldDefinition? FindGenerationField(List<FixFieldDefinition> definitions, int tag)
    {
        for (int index = 0; index < definitions.Count; index++) if (definitions[index].Tag == tag) return definitions[index];
        return null;
    }

    internal static string NormalizePlanningScenario(string value)
    {
        string result = value == null ? "" : value.Trim().ToUpperInvariant();
        return result.Length == 0 ? "BASE" : result;
    }

    internal static bool GenerationNamesEqual(FixDictionary dictionary, string left, string right)
    {
        return dictionary.Options.NormalizeNames ? string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase) : left == right;
    }

    internal static void AddPlanningInteger(List<int> values, int value)
    {
        if (value > 0 && !ContainsPlanningInteger(values, value)) values.Add(value);
    }

    internal static bool ContainsPlanningInteger(List<int> values, int value)
    {
        for (int index = 0; index < values.Count; index++) if (values[index] == value) return true;
        return false;
    }

    internal static void AddPlanningText(List<string> values, string value)
    {
        string item = value == null ? "" : value.Trim();
        if (item.Length != 0 && !ContainsPlanningText(values, item)) values.Add(item);
    }

    internal static bool ContainsPlanningText(List<string> values, string value)
    {
        for (int index = 0; index < values.Count; index++) if (string.Equals(values[index], value, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    internal static bool ContainsExactPlanningText(List<string> values, string value)
    {
        for (int index = 0; index < values.Count; index++) if (values[index] == value) return true;
        return false;
    }

    internal static void AddPlanningDiagnostic(List<GenerationDiagnostic> diagnostics, ValidationSeverity severity, string code, string message, string source)
    {
        GenerationDiagnostic diagnostic = new GenerationDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Source = source;
        diagnostics.Add(diagnostic);
    }
}
