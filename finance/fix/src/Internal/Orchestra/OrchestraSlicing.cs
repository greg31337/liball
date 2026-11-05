using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace fix;

internal static class OrchestraSlicing
{
    internal static OrchestraReadResult SliceRepository(OrchestraRepository repository, string[] messageNames, string[] scenarios)
    {
        OrchestraReadResult result = new OrchestraReadResult();
        if (repository == null) AddDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-orchestra-repository", "The Orchestra repository is missing.", "/");
        if (messageNames == null) AddDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-message-selection", "The message and category selection is missing.", "/messages");
        if (scenarios == null) AddDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-scenario-selection", "The scenario selection is missing.", "/scenarios");
        if (repository == null || messageNames == null || scenarios == null) return result;

        List<string> messageSelection = NormalizeSelection(messageNames);
        List<string> scenarioSelection = NormalizeSelection(scenarios);
        AddUnknownSelectionDiagnostics(repository, messageSelection, scenarioSelection, result.Diagnostics);
        OrchestraRepository sliced = CloneRepository(repository);
        result.Repository = sliced;

        FilterSelectedMessages(sliced, messageSelection, scenarioSelection);
        List<string> neededFields = [];
        List<string> neededComponents = [];
        List<string> neededGroups = [];
        List<string> neededCodeSets = [];
        List<string> neededDatatypes = [];
        List<string> neededRules = [];
        List<string> neededScenarios = [];
        List<string> neededStandards = [];
        List<string> neededCategories = [];
        List<string> selectedMessageNames = [];
        InitializeMessageDependencies(sliced, neededFields, neededComponents, neededGroups, neededRules, neededScenarios, neededStandards, neededCategories, selectedMessageNames);
        for (int index = 0; index < scenarioSelection.Count; index++) AddText(neededScenarios, scenarioSelection[index]);
        SelectWorkflowSubset(sliced, selectedMessageNames, neededRules);
        ExpandStructuralClosure(sliced, neededFields, neededComponents, neededGroups, neededCodeSets, neededDatatypes, neededRules, neededScenarios, neededStandards, neededCategories);
        ExpandScenarioClosure(sliced, neededScenarios, scenarioSelection.Count == 0);
        FilterDefinitions(sliced, neededFields, neededComponents, neededGroups, neededCodeSets, neededDatatypes, neededRules, neededScenarios, neededStandards, neededCategories, scenarioSelection.Count == 0);
        FilterConcepts(sliced, neededFields, neededComponents, neededGroups);
        result.Success = !HasErrors(result.Diagnostics);
        return result;
    }

    internal static List<string> NormalizeSelection(string[] source)
    {
        List<string> result = [];
        for (int index = 0; index < source.Length; index++)
        {
            string item = source[index] == null ? "" : source[index].Trim();
            if (item.Length != 0 && !ContainsText(result, item)) result.Add(item);
        }
        return result;
    }

    internal static void AddUnknownSelectionDiagnostics(OrchestraRepository repository, List<string> messages, List<string> scenarios, List<OrchestraDiagnostic> diagnostics)
    {
        for (int selectionIndex = 0; selectionIndex < messages.Count; selectionIndex++)
        {
            bool found = false;
            for (int messageIndex = 0; messageIndex < repository.Messages.Count; messageIndex++)
            {
                OrchestraMessageDefinition message = repository.Messages[messageIndex];
                if (message.Name == messages[selectionIndex] || message.MessageType == messages[selectionIndex] || message.Category == messages[selectionIndex]) found = true;
            }
            if (!found) AddDiagnostic(diagnostics, ValidationSeverity.Warning, "unknown-message-or-category-selection", $"No message name, message type, or category matches '{messages[selectionIndex]}'.", "/messages/" + EscapePathSegment(messages[selectionIndex]));
        }
        for (int selectionIndex = 0; selectionIndex < scenarios.Count; selectionIndex++)
        {
            bool found = false;
            for (int scenarioIndex = 0; scenarioIndex < repository.Scenarios.Count; scenarioIndex++) if (repository.Scenarios[scenarioIndex].Name == scenarios[selectionIndex]) found = true;
            for (int messageIndex = 0; messageIndex < repository.Messages.Count; messageIndex++) if (NormalizeScenario(repository.Messages[messageIndex].Scenario) == scenarios[selectionIndex]) found = true;
            if (!found) AddDiagnostic(diagnostics, ValidationSeverity.Warning, "unknown-scenario-selection", $"No scenario matches '{scenarios[selectionIndex]}'.", "/scenarios/" + EscapePathSegment(scenarios[selectionIndex]));
        }
    }

    internal static void FilterSelectedMessages(OrchestraRepository repository, List<string> messages, List<string> scenarios)
    {
        for (int index = repository.Messages.Count - 1; index >= 0; index--)
        {
            OrchestraMessageDefinition item = repository.Messages[index];
            bool messageSelected = messages.Count == 0 || ContainsText(messages, item.Name) || ContainsText(messages, item.MessageType) || ContainsText(messages, item.Category);
            bool scenarioSelected = ScenarioIsSelected(repository.Messages, item, scenarios);
            if (!messageSelected || !scenarioSelected || !IsActive(item.Pedigree, repository.Metadata)) repository.Messages.RemoveAt(index);
            else FilterMembersByVersion(item.Members, repository.Metadata);
        }
    }

    internal static bool ScenarioIsSelected(List<OrchestraMessageDefinition> messages, OrchestraMessageDefinition item, List<string> scenarios)
    {
        if (scenarios.Count == 0) return true;
        string scenario = NormalizeScenario(item.Scenario);
        if (ContainsText(scenarios, scenario)) return true;
        if (scenario != "base") return false;
        for (int index = 0; index < messages.Count; index++)
        {
            OrchestraMessageDefinition candidate = messages[index];
            bool sameMessage = candidate.Name == item.Name || candidate.MessageType == item.MessageType;
            if (sameMessage && ContainsText(scenarios, NormalizeScenario(candidate.Scenario))) return false;
        }
        return true;
    }

    internal static void InitializeMessageDependencies(OrchestraRepository repository, List<string> fields, List<string> components, List<string> groups, List<string> rules, List<string> scenarios, List<string> standards, List<string> categories, List<string> selectedMessages)
    {
        for (int index = 0; index < repository.Messages.Count; index++)
        {
            OrchestraMessageDefinition message = repository.Messages[index];
            AddText(selectedMessages, message.Name);
            AddText(selectedMessages, message.MessageType);
            AddText(categories, message.Category);
            AddText(scenarios, NormalizeScenario(message.Scenario));
            ScanMembers(repository, message.Members, fields, components, groups, rules, scenarios, standards);
            ScanRules(message.Rules, fields, rules, [], 0);
            AddEncodingStandards(message.Encodings, standards);
            ScanResponses(message.Responses, fields, rules);
        }
    }

    internal static void ExpandStructuralClosure(OrchestraRepository repository, List<string> fields, List<string> components, List<string> groups, List<string> codeSets, List<string> datatypes, List<string> rules, List<string> scenarios, List<string> standards, List<string> categories)
    {
        int previous = -1;
        while (previous != fields.Count + components.Count + groups.Count + codeSets.Count + datatypes.Count + rules.Count + scenarios.Count + standards.Count + categories.Count)
        {
            previous = fields.Count + components.Count + groups.Count + codeSets.Count + datatypes.Count + rules.Count + scenarios.Count + standards.Count + categories.Count;
            for (int index = 0; index < repository.Components.Count; index++)
            {
                OrchestraComponentDefinition item = repository.Components[index];
                if (ContainsText(components, DefinitionKey(item.Id, item.Scenario)) && IsActive(item.Pedigree, repository.Metadata))
                {
                    FilterMembersByVersion(item.Members, repository.Metadata);
                    AddText(categories, item.Category);
                    AddText(scenarios, NormalizeScenario(item.Scenario));
                    ScanMembers(repository, item.Members, fields, components, groups, rules, scenarios, standards);
                    AddEncodingStandards(item.Encodings, standards);
                }
            }
            for (int index = 0; index < repository.Groups.Count; index++)
            {
                OrchestraGroupDefinition item = repository.Groups[index];
                if (ContainsText(groups, DefinitionKey(item.Id, item.Scenario)) && IsActive(item.Pedigree, repository.Metadata))
                {
                    FilterMembersByVersion(item.Members, repository.Metadata);
                    AddText(categories, item.Category);
                    AddText(scenarios, NormalizeScenario(item.Scenario));
                    if (item.CountMember != null && !IsActive(item.CountMember.Pedigree, repository.Metadata)) item.CountMember = null;
                    if (item.CountMember != null) ScanMember(repository, item.CountMember, fields, components, groups, rules, scenarios, standards);
                    ScanMembers(repository, item.Members, fields, components, groups, rules, scenarios, standards);
                    AddEncodingStandards(item.Encodings, standards);
                }
            }
            for (int index = 0; index < repository.Fields.Count; index++)
            {
                OrchestraFieldDefinition item = repository.Fields[index];
                if (ContainsText(fields, DefinitionKey(item.Id, item.Scenario)) && IsActive(item.Pedigree, repository.Metadata))
                {
                    AddText(scenarios, NormalizeScenario(item.Scenario));
                    AddCodeSetDependency(repository, item.CodeSet, item.Scenario, codeSets);
                    AddText(datatypes, item.Type);
                    AddText(datatypes, item.UnionDatatype);
                    AddFieldDependency(repository, item.LengthId, item.Scenario, fields);
                    AddFieldDependency(repository, item.DiscriminatorId, item.Scenario, fields);
                    ScanRules(item.Rules, fields, rules, datatypes, 0);
                    AddEncodingStandards(item.Encodings, standards);
                }
            }
            for (int index = 0; index < repository.CodeSets.Count; index++)
            {
                OrchestraCodeSetDefinition item = repository.CodeSets[index];
                if (ContainsText(codeSets, DefinitionKey(item.Name, item.Scenario)) && IsActive(item.Pedigree, repository.Metadata))
                {
                    AddText(scenarios, NormalizeScenario(item.Scenario));
                    AddText(datatypes, item.Type);
                    AddText(datatypes, item.UnionDatatype);
                    AddEncodingStandards(item.Encodings, standards);
                }
            }
            for (int index = 0; index < repository.Datatypes.Count; index++)
            {
                OrchestraDatatypeDefinition item = repository.Datatypes[index];
                if (ContainsText(datatypes, item.Name) && IsActive(item.Pedigree, repository.Metadata))
                {
                    AddText(datatypes, item.BaseType);
                    for (int mappingIndex = 0; mappingIndex < item.Mappings.Count; mappingIndex++)
                    {
                        OrchestraDatatypeMapping mapping = item.Mappings[mappingIndex];
                        AddText(standards, mapping.Standard);
                        if (mapping.Encoding != null) AddText(standards, mapping.Encoding.Standard);
                    }
                }
            }
            for (int index = 0; index < repository.Rules.Count; index++)
            {
                OrchestraRule item = repository.Rules[index];
                if (ContainsText(rules, RuleKey(item))) ScanRule(item, fields, rules, datatypes, 0);
            }
            for (int index = 0; index < repository.EncodingStandards.Count; index++)
            {
                OrchestraEncodingStandard item = repository.EncodingStandards[index];
                if (ContainsText(standards, item.Name) && item.Encoding != null) AddText(standards, item.Encoding.Standard);
            }
            for (int index = 0; index < repository.Categories.Count; index++)
            {
                OrchestraCategory item = repository.Categories[index];
                if (ContainsText(categories, item.Name))
                {
                    AddEncodingStandards(item.Encodings, standards);
                    for (int sectionIndex = 0; sectionIndex < repository.Sections.Count; sectionIndex++) if (repository.Sections[sectionIndex].Name == item.Section) AddEncodingStandards(repository.Sections[sectionIndex].Encodings, standards);
                }
            }
        }
    }

    internal static void ScanMembers(OrchestraRepository repository, List<OrchestraMemberDefinition> members, List<string> fields, List<string> components, List<string> groups, List<string> rules, List<string> scenarios, List<string> standards)
    {
        for (int index = 0; index < members.Count; index++) ScanMember(repository, members[index], fields, components, groups, rules, scenarios, standards);
    }

    internal static void ScanMember(OrchestraRepository repository, OrchestraMemberDefinition member, List<string> fields, List<string> components, List<string> groups, List<string> rules, List<string> scenarios, List<string> standards)
    {
        string scenario = NormalizeScenario(member.Scenario);
        AddText(scenarios, scenario);
        if (member.Kind == "fieldRef") AddFieldDependency(repository, member.ReferenceId, scenario, fields);
        else if (member.Kind == "componentRef") AddComponentDependency(repository, member.ReferenceId, scenario, components);
        else if (member.Kind == "groupRef") AddGroupDependency(repository, member.ReferenceId, scenario, groups);
        AddFieldDependency(repository, member.LengthId, scenario, fields);
        ScanRules(member.Rules, fields, rules, [], 0);
        AddEncodingStandards(member.Encodings, standards);
    }

    internal static void AddFieldDependency(OrchestraRepository repository, string id, string scenario, List<string> fields)
    {
        if (id.Length == 0) return;
        string selected = FindFieldScenario(repository, id, scenario);
        AddText(fields, DefinitionKey(id, selected));
    }

    internal static void AddComponentDependency(OrchestraRepository repository, string id, string scenario, List<string> components)
    {
        if (id.Length == 0) return;
        string selected = FindComponentScenario(repository, id, scenario);
        AddText(components, DefinitionKey(id, selected));
    }

    internal static void AddGroupDependency(OrchestraRepository repository, string id, string scenario, List<string> groups)
    {
        if (id.Length == 0) return;
        string selected = FindGroupScenario(repository, id, scenario);
        AddText(groups, DefinitionKey(id, selected));
    }

    internal static void AddCodeSetDependency(OrchestraRepository repository, string name, string scenario, List<string> codeSets)
    {
        if (name.Length == 0) return;
        string selected = FindCodeSetScenario(repository, name, scenario);
        AddText(codeSets, DefinitionKey(name, selected));
    }

    internal static string FindFieldScenario(OrchestraRepository repository, string id, string scenario)
    {
        for (int index = 0; index < repository.Fields.Count; index++) if (repository.Fields[index].Id == id && NormalizeScenario(repository.Fields[index].Scenario) == scenario) return scenario;
        return "base";
    }

    internal static string FindComponentScenario(OrchestraRepository repository, string id, string scenario)
    {
        for (int index = 0; index < repository.Components.Count; index++) if (repository.Components[index].Id == id && NormalizeScenario(repository.Components[index].Scenario) == scenario) return scenario;
        return "base";
    }

    internal static string FindGroupScenario(OrchestraRepository repository, string id, string scenario)
    {
        for (int index = 0; index < repository.Groups.Count; index++) if (repository.Groups[index].Id == id && NormalizeScenario(repository.Groups[index].Scenario) == scenario) return scenario;
        return "base";
    }

    internal static string FindCodeSetScenario(OrchestraRepository repository, string name, string scenario)
    {
        for (int index = 0; index < repository.CodeSets.Count; index++) if (repository.CodeSets[index].Name == name && NormalizeScenario(repository.CodeSets[index].Scenario) == scenario) return scenario;
        return "base";
    }

    internal static void ScanRules(List<OrchestraRule> source, List<string> fields, List<string> rules, List<string> datatypes, int depth)
    {
        for (int index = 0; index < source.Count; index++) ScanRule(source[index], fields, rules, datatypes, depth);
    }

    internal static void ScanRule(OrchestraRule rule, List<string> fields, List<string> rules, List<string> datatypes, int depth)
    {
        if (depth > 64) return;
        AddText(rules, RuleKey(rule));
        AddText(datatypes, rule.Type);
        for (int index = 0; index < rule.UniqueFieldIds.Count; index++) AddText(fields, DefinitionKey(rule.UniqueFieldIds[index], "base"));
        for (int index = 0; index < rule.Assignments.Count; index++) ScanRule(rule.Assignments[index], fields, rules, datatypes, depth + 1);
    }

    internal static void ScanResponses(List<OrchestraResponse> source, List<string> fields, List<string> rules)
    {
        for (int index = 0; index < source.Count; index++)
        {
            OrchestraResponse response = source[index];
            if (response.Rule != null) ScanRule(response.Rule, fields, rules, [], 0);
            ScanRules(response.Assignments, fields, rules, [], 0);
        }
    }

    internal static void AddEncodingStandards(List<OrchestraEncoding> source, List<string> standards)
    {
        for (int index = 0; index < source.Count; index++) AddText(standards, source[index].Standard);
    }

    internal static void ExpandScenarioClosure(OrchestraRepository repository, List<string> scenarios, bool preserveAll)
    {
        if (preserveAll)
        {
            for (int index = 0; index < repository.Scenarios.Count; index++) if (IsActive(repository.Scenarios[index].Pedigree, repository.Metadata)) AddText(scenarios, repository.Scenarios[index].Name);
        }
        int previous = -1;
        while (previous != scenarios.Count)
        {
            previous = scenarios.Count;
            for (int index = 0; index < repository.Scenarios.Count; index++)
            {
                OrchestraScenarioDefinition item = repository.Scenarios[index];
                if (ContainsText(scenarios, item.Name)) AddText(scenarios, item.ParentName);
            }
        }
        if (scenarios.Count != 0) AddText(scenarios, "base");
    }

    internal static void FilterDefinitions(OrchestraRepository repository, List<string> fields, List<string> components, List<string> groups, List<string> codeSets, List<string> datatypes, List<string> rules, List<string> scenarios, List<string> standards, List<string> categories, bool preserveScenarios)
    {
        for (int index = repository.Datatypes.Count - 1; index >= 0; index--) if (!ContainsText(datatypes, repository.Datatypes[index].Name) || !IsActive(repository.Datatypes[index].Pedigree, repository.Metadata)) repository.Datatypes.RemoveAt(index);
        for (int index = repository.CodeSets.Count - 1; index >= 0; index--)
        {
            OrchestraCodeSetDefinition item = repository.CodeSets[index];
            if (!ContainsText(codeSets, DefinitionKey(item.Name, item.Scenario)) || !IsActive(item.Pedigree, repository.Metadata)) repository.CodeSets.RemoveAt(index);
            else for (int codeIndex = item.Codes.Count - 1; codeIndex >= 0; codeIndex--) if (!IsActive(item.Codes[codeIndex].Pedigree, repository.Metadata)) item.Codes.RemoveAt(codeIndex);
        }
        for (int index = repository.Fields.Count - 1; index >= 0; index--)
        {
            OrchestraFieldDefinition item = repository.Fields[index];
            if (!ContainsText(fields, DefinitionKey(item.Id, item.Scenario)) || !IsActive(item.Pedigree, repository.Metadata)) repository.Fields.RemoveAt(index);
        }
        for (int index = repository.Components.Count - 1; index >= 0; index--)
        {
            OrchestraComponentDefinition item = repository.Components[index];
            if (!ContainsText(components, DefinitionKey(item.Id, item.Scenario)) || !IsActive(item.Pedigree, repository.Metadata)) repository.Components.RemoveAt(index);
        }
        for (int index = repository.Groups.Count - 1; index >= 0; index--)
        {
            OrchestraGroupDefinition item = repository.Groups[index];
            if (!ContainsText(groups, DefinitionKey(item.Id, item.Scenario)) || !IsActive(item.Pedigree, repository.Metadata)) repository.Groups.RemoveAt(index);
        }
        for (int index = repository.Rules.Count - 1; index >= 0; index--) if (!ContainsText(rules, RuleKey(repository.Rules[index]))) repository.Rules.RemoveAt(index);
        for (int index = repository.Scenarios.Count - 1; index >= 0; index--)
        {
            OrchestraScenarioDefinition item = repository.Scenarios[index];
            if ((!preserveScenarios && !ContainsText(scenarios, item.Name)) || !IsActive(item.Pedigree, repository.Metadata)) repository.Scenarios.RemoveAt(index);
        }
        FilterDefaultsAndStandards(repository, standards);
        FilterCategoriesAndSections(repository, categories);
    }

    internal static void FilterDefaultsAndStandards(OrchestraRepository repository, List<string> standards)
    {
        if (repository.CodeSets.Count == 0) repository.CodeSetEncodings.Clear();
        if (repository.Components.Count == 0) repository.ComponentEncodings.Clear();
        if (repository.Groups.Count == 0) repository.GroupEncodings.Clear();
        if (repository.Messages.Count == 0) repository.MessageEncodings.Clear();
        AddEncodingStandards(repository.CodeSetEncodings, standards);
        AddEncodingStandards(repository.ComponentEncodings, standards);
        AddEncodingStandards(repository.GroupEncodings, standards);
        AddEncodingStandards(repository.MessageEncodings, standards);
        for (int index = repository.EncodingStandards.Count - 1; index >= 0; index--) if (!ContainsText(standards, repository.EncodingStandards[index].Name)) repository.EncodingStandards.RemoveAt(index);
    }

    internal static void FilterCategoriesAndSections(OrchestraRepository repository, List<string> categories)
    {
        List<string> sections = [];
        for (int index = repository.Categories.Count - 1; index >= 0; index--)
        {
            OrchestraCategory item = repository.Categories[index];
            if (!ContainsText(categories, item.Name) || !IsActive(item.Pedigree, repository.Metadata)) repository.Categories.RemoveAt(index);
            else AddText(sections, item.Section);
        }
        for (int index = repository.Sections.Count - 1; index >= 0; index--)
        {
            OrchestraSection item = repository.Sections[index];
            if (!ContainsText(sections, item.Name) || !IsActive(item.Pedigree, repository.Metadata)) repository.Sections.RemoveAt(index);
        }
    }

    internal static void FilterConcepts(OrchestraRepository repository, List<string> fields, List<string> components, List<string> groups)
    {
        for (int conceptIndex = repository.Concepts.Count - 1; conceptIndex >= 0; conceptIndex--)
        {
            OrchestraConcept concept = repository.Concepts[conceptIndex];
            for (int memberIndex = concept.Members.Count - 1; memberIndex >= 0; memberIndex--)
            {
                OrchestraConceptMember member = concept.Members[memberIndex];
                string key = DefinitionKey(member.Id, member.Scenario);
                bool retained = member.Kind == "fieldRef" && ContainsText(fields, key);
                if (member.Kind == "componentRef" && ContainsText(components, key)) retained = true;
                if (member.Kind == "groupRef" && ContainsText(groups, key)) retained = true;
                if (!retained) concept.Members.RemoveAt(memberIndex);
            }
            if (concept.Members.Count == 0) repository.Concepts.RemoveAt(conceptIndex);
        }
    }

    internal static void SelectWorkflowSubset(OrchestraRepository repository, List<string> selectedMessages, List<string> rules)
    {
        List<string> actors = [];
        for (int flowIndex = repository.Flows.Count - 1; flowIndex >= 0; flowIndex--)
        {
            OrchestraFlow flow = repository.Flows[flowIndex];
            bool retained = false;
            for (int messageIndex = flow.Messages.Count - 1; messageIndex >= 0; messageIndex--)
            {
                if (!ContainsText(selectedMessages, flow.Messages[messageIndex])) flow.Messages.RemoveAt(messageIndex);
                else retained = true;
            }
            if (!retained) repository.Flows.RemoveAt(flowIndex);
            else
            {
                AddText(actors, flow.SourceActor);
                AddText(actors, flow.DestinationActor);
            }
        }
        for (int machineIndex = repository.StateMachines.Count - 1; machineIndex >= 0; machineIndex--)
        {
            OrchestraStateMachine machine = repository.StateMachines[machineIndex];
            if (!ContainsText(actors, machine.Actor)) repository.StateMachines.RemoveAt(machineIndex);
            else ScanStateMachineRules(machine, rules);
        }
        for (int actorIndex = repository.Actors.Count - 1; actorIndex >= 0; actorIndex--)
        {
            OrchestraActor actor = repository.Actors[actorIndex];
            if (!ContainsText(actors, actor.Name)) repository.Actors.RemoveAt(actorIndex);
            else
            {
                for (int variableIndex = 0; variableIndex < actor.Variables.Count; variableIndex++) ScanRules(actor.Variables[variableIndex].Rules, [], rules, [], 0);
            }
        }
        for (int index = repository.Variables.Count - 1; index >= 0; index--)
        {
            OrchestraVariable item = repository.Variables[index];
            if (item.Attributes.ContainsKey("actor") && !ContainsText(actors, item.Attributes["actor"])) repository.Variables.RemoveAt(index);
            else ScanRules(item.Rules, [], rules, [], 0);
        }
        for (int index = repository.Timers.Count - 1; index >= 0; index--) if (repository.Timers[index].Actor.Length != 0 && !ContainsText(actors, repository.Timers[index].Actor)) repository.Timers.RemoveAt(index);
    }

    internal static void ScanStateMachineRules(OrchestraStateMachine machine, List<string> rules)
    {
        for (int variableIndex = 0; variableIndex < machine.Variables.Count; variableIndex++) ScanRules(machine.Variables[variableIndex].Rules, [], rules, [], 0);
        for (int stateIndex = 0; stateIndex < machine.States.Count; stateIndex++)
        {
            OrchestraState state = machine.States[stateIndex];
            for (int transitionIndex = 0; transitionIndex < state.Transitions.Count; transitionIndex++)
            {
                OrchestraTransition transition = state.Transitions[transitionIndex];
                if (transition.Rule != null) ScanRule(transition.Rule, [], rules, [], 0);
                ScanResponses(transition.Responses, [], rules);
            }
        }
    }

    internal static void FilterMembersByVersion(List<OrchestraMemberDefinition> members, OrchestraMetadata metadata)
    {
        for (int index = members.Count - 1; index >= 0; index--) if (!IsActive(members[index].Pedigree, metadata)) members.RemoveAt(index);
    }

    internal static bool IsActive(OrchestraPedigree pedigree, OrchestraMetadata metadata)
    {
        string supported = pedigree.Supported.Trim().ToLowerInvariant();
        if (supported == "unsupported" || supported == "false" || supported == "no") return false;
        string target = metadata.FixVersion.Length == 0 ? metadata.Release : metadata.FixVersion;
        if (target.Length == 0) return true;
        if (pedigree.AddedVersion.Length != 0 && CompareVersions(target, pedigree.AddedVersion) < 0) return false;
        if (pedigree.ReplacedVersion.Length != 0 && CompareVersions(target, pedigree.ReplacedVersion) >= 0) return false;
        if (pedigree.AddedExtensionPack.Length != 0 && VersionNamesEqual(target, pedigree.AddedVersion) && metadata.ExtensionPack < ParseVersionNumber(pedigree.AddedExtensionPack)) return false;
        if (pedigree.ReplacedExtensionPack.Length != 0 && VersionNamesEqual(target, pedigree.ReplacedVersion) && metadata.ExtensionPack >= ParseVersionNumber(pedigree.ReplacedExtensionPack)) return false;
        return true;
    }

    internal static int CompareVersions(string left, string right)
    {
        string leftUpper = left.ToUpperInvariant();
        string rightUpper = right.ToUpperInvariant();
        bool leftLatest = leftUpper.Contains("LATEST", StringComparison.Ordinal);
        bool rightLatest = rightUpper.Contains("LATEST", StringComparison.Ordinal);
        if (leftLatest && !rightLatest) return 1;
        if (!leftLatest && rightLatest) return -1;
        List<int> leftParts = VersionParts(leftUpper);
        List<int> rightParts = VersionParts(rightUpper);
        int count = leftParts.Count > rightParts.Count ? leftParts.Count : rightParts.Count;
        for (int index = 0; index < count; index++)
        {
            int leftPart = index < leftParts.Count ? leftParts[index] : 0;
            int rightPart = index < rightParts.Count ? rightParts[index] : 0;
            if (leftPart < rightPart) return -1;
            if (leftPart > rightPart) return 1;
        }
        return string.CompareOrdinal(leftUpper, rightUpper);
    }

    internal static List<int> VersionParts(string value)
    {
        List<int> result = [];
        int current = -1;
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (character >= '0' && character <= '9')
            {
                if (current < 0) current = 0;
                current = current * 10 + character - '0';
            }
            else if (current >= 0)
            {
                result.Add(current);
                current = -1;
            }
        }
        if (current >= 0) result.Add(current);
        return result;
    }

    internal static bool VersionNamesEqual(string left, string right)
    {
        if (right.Length == 0) return true;
        return CompareVersions(left, right) == 0;
    }

    internal static int ParseVersionNumber(string value)
    {
        List<int> parts = VersionParts(value);
        return parts.Count == 0 ? 0 : parts[parts.Count - 1];
    }

    internal static OrchestraRepository CloneRepository(OrchestraRepository source)
    {
        Dictionary<object, object> known = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);
        object? cloned = CloneGraphValue(source, known, 0);
        OrchestraRepository? repository = cloned as OrchestraRepository;
        return repository == null ? new OrchestraRepository() : repository;
    }

    internal static object? CloneGraphValue(object? value, Dictionary<object, object> known, int depth)
    {
        if (value == null) return null;
        if (depth > 128) return value;
        Type type = value.GetType();
        if (type.FullName == "System.String" || type.IsValueType) return value;
        if (known.ContainsKey(value)) return known[value];
        object? created = Activator.CreateInstance(type);
        if (created == null) return value;
        known.Add(value, created);
        IDictionary? sourceDictionary = value as IDictionary;
        IDictionary? targetDictionary = created as IDictionary;
        if (sourceDictionary != null && targetDictionary != null)
        {
            foreach (DictionaryEntry entry in sourceDictionary)
            {
                object? key = CloneGraphValue(entry.Key, known, depth + 1);
                object? item = CloneGraphValue(entry.Value, known, depth + 1);
                if (key != null) targetDictionary.Add(key, item);
            }
            return created;
        }
        IList? sourceList = value as IList;
        IList? targetList = created as IList;
        if (sourceList != null && targetList != null)
        {
            for (int index = 0; index < sourceList.Count; index++) targetList.Add(CloneGraphValue(sourceList[index], known, depth + 1));
            return created;
        }
        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
        for (int index = 0; index < fields.Length; index++)
        {
            if (type.FullName == "fix.OrchestraRepository" && fields[index].Name == "Dictionary") continue;
            fields[index].SetValue(created, CloneGraphValue(fields[index].GetValue(value), known, depth + 1));
        }
        return created;
    }

    internal static string RuleKey(OrchestraRule rule)
    {
        if (rule.Id != 0) return rule.Id.ToString(CultureInfo.InvariantCulture);
        return rule.Name;
    }

    internal static string DefinitionKey(string id, string scenario)
    {
        return id + "\u001f" + NormalizeScenario(scenario);
    }

    internal static string NormalizeScenario(string scenario)
    {
        return scenario.Length == 0 ? "base" : scenario;
    }

    internal static void AddText(List<string> destination, string value)
    {
        if (value.Length != 0 && !ContainsText(destination, value)) destination.Add(value);
    }

    internal static bool ContainsText(List<string> source, string value)
    {
        for (int index = 0; index < source.Count; index++) if (source[index] == value) return true;
        return false;
    }

    internal static string EscapePathSegment(string value)
    {
        return value.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
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

    internal static bool HasErrors(List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < diagnostics.Count; index++) if (diagnostics[index].Severity == ValidationSeverity.Error || diagnostics[index].Severity == ValidationSeverity.Fatal) return true;
        return false;
    }
}
