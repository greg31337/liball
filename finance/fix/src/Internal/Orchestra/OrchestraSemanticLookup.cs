using System.Collections.Generic;

namespace fix;

internal class OrchestraSemanticLookup
{
    internal Dictionary<string, OrchestraDatatypeDefinition> Datatypes = [];
    internal HashSet<string> EncodingStandards = [];
    internal Dictionary<string, OrchestraScenarioDefinition> Scenarios = [];
    internal HashSet<string> Sections = [];
    internal HashSet<string> Categories = [];
    internal HashSet<string> CodeSets = [];
    internal Dictionary<string, OrchestraFieldDefinition> Fields = [];
    internal HashSet<string> FieldIds = [];
    internal HashSet<string> Components = [];
    internal Dictionary<string, OrchestraComponentDefinition> ComponentDefinitions = [];
    internal HashSet<string> Groups = [];
    internal Dictionary<string, OrchestraGroupDefinition> GroupDefinitions = [];
    internal HashSet<string> MessageIdentifiers = [];
    internal HashSet<string> MessageNames = [];
    internal HashSet<string> Flows = [];
    internal HashSet<string> Actors = [];
    internal Dictionary<string, OrchestraStateMachine> StateMachines = [];
    internal HashSet<string> Timers = [];

    internal OrchestraSemanticLookup(OrchestraRepository repository)
    {
        for (int index = 0; index < repository.Datatypes.Count; index++) AddDatatype(repository.Datatypes[index]);
        for (int index = 0; index < repository.EncodingStandards.Count; index++) EncodingStandards.Add(repository.EncodingStandards[index].Name);
        for (int index = 0; index < repository.Scenarios.Count; index++) AddScenario(repository.Scenarios[index]);
        for (int index = 0; index < repository.Sections.Count; index++) Sections.Add(repository.Sections[index].Name);
        for (int index = 0; index < repository.Categories.Count; index++) Categories.Add(repository.Categories[index].Name);
        for (int index = 0; index < repository.CodeSets.Count; index++) CodeSets.Add(ScenarioKey(repository.CodeSets[index].Name, repository.CodeSets[index].Scenario));
        for (int index = 0; index < repository.Fields.Count; index++) AddField(repository.Fields[index]);
        for (int index = 0; index < repository.Components.Count; index++) AddComponent(repository.Components[index]);
        for (int index = 0; index < repository.Groups.Count; index++) AddGroup(repository.Groups[index]);
        for (int index = 0; index < repository.Messages.Count; index++) AddMessage(repository.Messages[index]);
        for (int index = 0; index < repository.Flows.Count; index++) Flows.Add(repository.Flows[index].Name);
        for (int index = 0; index < repository.Actors.Count; index++) Actors.Add(repository.Actors[index].Name);
        for (int index = 0; index < repository.StateMachines.Count; index++) AddStateMachine(repository.StateMachines[index]);
        for (int index = 0; index < repository.Timers.Count; index++) Timers.Add(PairKey(repository.Timers[index].Actor, repository.Timers[index].Name));
    }

    internal void AddDatatype(OrchestraDatatypeDefinition datatype)
    {
        if (!Datatypes.ContainsKey(datatype.Name)) Datatypes.Add(datatype.Name, datatype);
    }

    internal void AddScenario(OrchestraScenarioDefinition scenario)
    {
        if (!Scenarios.ContainsKey(scenario.Name)) Scenarios.Add(scenario.Name, scenario);
    }

    internal void AddField(OrchestraFieldDefinition field)
    {
        string key = ScenarioKey(field.Id, field.Scenario);
        if (!Fields.ContainsKey(key)) Fields.Add(key, field);
        FieldIds.Add(field.Id);
    }

    internal void AddMessage(OrchestraMessageDefinition message)
    {
        string scenario = message.Scenario;
        if (message.Id.Length != 0) MessageIdentifiers.Add(ScenarioKey("id:" + message.Id, scenario));
        if (message.MessageType.Length != 0) MessageIdentifiers.Add(ScenarioKey("type:" + message.MessageType, scenario));
        if (message.Name.Length != 0) MessageIdentifiers.Add(ScenarioKey("name:" + message.Name, scenario));
        MessageNames.Add(message.Name);
    }

    internal void AddComponent(OrchestraComponentDefinition component)
    {
        string key = ScenarioKey(component.Id, component.Scenario);
        Components.Add(key);
        if (!ComponentDefinitions.ContainsKey(key)) ComponentDefinitions.Add(key, component);
    }

    internal void AddGroup(OrchestraGroupDefinition group)
    {
        string key = ScenarioKey(group.Id, group.Scenario);
        Groups.Add(key);
        if (!GroupDefinitions.ContainsKey(key)) GroupDefinitions.Add(key, group);
    }

    internal void AddStateMachine(OrchestraStateMachine machine)
    {
        string key = PairKey(machine.Actor, machine.Name);
        if (!StateMachines.ContainsKey(key)) StateMachines.Add(key, machine);
    }

    internal OrchestraDatatypeDefinition? FindDatatype(string name)
    {
        if (Datatypes.ContainsKey(name)) return Datatypes[name];
        return null;
    }

    internal OrchestraScenarioDefinition? FindScenario(string name)
    {
        if (Scenarios.ContainsKey(name)) return Scenarios[name];
        return null;
    }

    internal OrchestraFieldDefinition? FindField(string id, string scenario)
    {
        string key = ScenarioKey(id, scenario);
        if (Fields.ContainsKey(key)) return Fields[key];
        return null;
    }

    internal OrchestraComponentDefinition? FindComponent(string id, string scenario)
    {
        string key = ScenarioKey(id, scenario);
        if (ComponentDefinitions.ContainsKey(key)) return ComponentDefinitions[key];
        return null;
    }

    internal OrchestraGroupDefinition? FindGroup(string id, string scenario)
    {
        string key = ScenarioKey(id, scenario);
        if (GroupDefinitions.ContainsKey(key)) return GroupDefinitions[key];
        return null;
    }

    internal bool HasMessage(string id, string scenario, string messageType, string name)
    {
        if (id.Length != 0 && MessageIdentifiers.Contains(ScenarioKey("id:" + id, scenario))) return true;
        if (messageType.Length != 0 && MessageIdentifiers.Contains(ScenarioKey("type:" + messageType, scenario))) return true;
        if (name.Length != 0 && MessageIdentifiers.Contains(ScenarioKey("name:" + name, scenario))) return true;
        return false;
    }

    internal OrchestraStateMachine? FindStateMachine(string actor, string name)
    {
        string key = PairKey(actor, name);
        if (StateMachines.ContainsKey(key)) return StateMachines[key];
        return null;
    }

    internal static string ScenarioKey(string value, string scenario)
    {
        string normalized = scenario.Length == 0 ? "base" : scenario;
        return value + "\u001f" + normalized;
    }

    internal static string PairKey(string left, string right)
    {
        return left + "\u001f" + right;
    }
}
