using System.Collections.Generic;
using System.Xml;

namespace fix;

internal static class OrchestraBehaviorEmitter
{
    internal static void WriteActorsAndFlows(XmlWriter writer, OrchestraRepository repository, OrchestraWriteOptions options, string namespaceUri)
    {
        if (repository.Actors.Count == 0 && repository.Flows.Count == 0) return;
        writer.WriteStartElement("fixr", "actors", namespaceUri);
        List<OrchestraActor> actors = new List<OrchestraActor>(repository.Actors);
        List<OrchestraFlow> flows = new List<OrchestraFlow>(repository.Flows);
        if (options.CanonicalOrder)
        {
            actors.Sort((left, right) => OrchestraXmlEmitter.CompareNames(left.Name, right.Name));
            flows.Sort((left, right) => OrchestraXmlEmitter.CompareNames(left.Name, right.Name));
        }
        for (int index = 0; index < actors.Count; index++) WriteActor(writer, actors[index], repository, options, namespaceUri);
        for (int index = 0; index < flows.Count; index++) WriteFlow(writer, flows[index], namespaceUri);
        writer.WriteEndElement();
    }

    internal static void WriteActor(XmlWriter writer, OrchestraActor actor, OrchestraRepository repository, OrchestraWriteOptions options, string namespaceUri)
    {
        writer.WriteStartElement("fixr", "actor", namespaceUri);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "name", actor.Name);
        OrchestraXmlEmitter.WriteExtensionAttributes(writer, actor.Extensions);
        for (int index = 0; index < actor.Variables.Count; index++) WriteVariable(writer, actor.Variables[index], namespaceUri);
        for (int index = 0; index < actor.Timers.Count; index++) WriteTimerDeclaration(writer, actor.Timers[index], namespaceUri);
        List<OrchestraStateMachine> machines = FindActorMachines(repository, actor);
        if (options.CanonicalOrder) machines.Sort((left, right) => OrchestraXmlEmitter.CompareNames(left.Name, right.Name));
        for (int index = 0; index < machines.Count; index++) WriteStateMachine(writer, machines[index], options, namespaceUri);
        OrchestraXmlEmitter.WriteAnnotations(writer, actor.Annotations, namespaceUri);
        OrchestraXmlEmitter.WriteExtensions(writer, actor.Extensions);
        writer.WriteEndElement();
    }

    internal static List<OrchestraStateMachine> FindActorMachines(OrchestraRepository repository, OrchestraActor actor)
    {
        List<OrchestraStateMachine> result = [];
        for (int index = 0; index < repository.StateMachines.Count; index++)
        {
            OrchestraStateMachine machine = repository.StateMachines[index];
            bool selected = machine.Actor == actor.Name;
            if (!selected)
            {
                for (int nameIndex = 0; nameIndex < actor.StateMachines.Count; nameIndex++)
                {
                    if (actor.StateMachines[nameIndex] == machine.Name) selected = true;
                }
            }
            if (selected) result.Add(machine);
        }
        return result;
    }

    internal static void WriteVariable(XmlWriter writer, OrchestraVariable variable, string namespaceUri)
    {
        string kind = variable.Kind.Length == 0 ? "field" : variable.Kind;
        writer.WriteStartElement("fixr", kind, namespaceUri);
        string id = variable.ReferenceId.Length == 0 ? variable.Id : variable.ReferenceId;
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "id", id);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "name", variable.Name);
        OrchestraXmlEmitter.WriteScenarioAttribute(writer, variable.Scenario);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "type", variable.Datatype);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "value", variable.InitialValue);
        OrchestraXmlEmitter.WriteExtensionAttributes(writer, variable.Extensions);
        WriteRules(writer, variable.Rules, namespaceUri);
        OrchestraXmlEmitter.WriteAnnotations(writer, variable.Annotations, namespaceUri);
        OrchestraXmlEmitter.WriteExtensions(writer, variable.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteTimerDeclaration(XmlWriter writer, OrchestraTimer timer, string namespaceUri)
    {
        writer.WriteStartElement("fixr", "timer", namespaceUri);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "name", timer.Name);
        OrchestraXmlEmitter.WriteExtensionAttributes(writer, timer.Extensions);
        OrchestraXmlEmitter.WriteExtensions(writer, timer.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteFlow(XmlWriter writer, OrchestraFlow flow, string namespaceUri)
    {
        writer.WriteStartElement("fixr", "flow", namespaceUri);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "name", flow.Name);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "source", flow.SourceActor);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "destination", flow.DestinationActor);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "reliability", flow.Reliability);
        OrchestraXmlEmitter.WriteExtensionAttributes(writer, flow.Extensions);
        OrchestraXmlEmitter.WriteAnnotations(writer, flow.Annotations, namespaceUri);
        OrchestraXmlEmitter.WriteExtensions(writer, flow.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteStateMachine(XmlWriter writer, OrchestraStateMachine machine, OrchestraWriteOptions options, string namespaceUri)
    {
        writer.WriteStartElement("fixr", "states", namespaceUri);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "name", machine.Name);
        OrchestraXmlEmitter.WriteExtensionAttributes(writer, machine.Extensions);
        for (int variableIndex = 0; variableIndex < machine.Variables.Count; variableIndex++) WriteVariable(writer, machine.Variables[variableIndex], namespaceUri);
        OrchestraState? initial = FindState(machine, machine.InitialState);
        if (initial != null) WriteState(writer, initial, true, namespaceUri);
        List<OrchestraState> remaining = new List<OrchestraState>();
        for (int index = 0; index < machine.States.Count; index++)
        {
            if (machine.States[index] != initial) remaining.Add(machine.States[index]);
        }
        if (options.CanonicalOrder) remaining.Sort((left, right) => OrchestraXmlEmitter.CompareNames(left.Name, right.Name));
        for (int index = 0; index < remaining.Count; index++) WriteState(writer, remaining[index], false, namespaceUri);
        OrchestraXmlEmitter.WriteAnnotations(writer, machine.Annotations, namespaceUri);
        OrchestraXmlEmitter.WriteExtensions(writer, machine.Extensions);
        writer.WriteEndElement();
    }

    internal static OrchestraState? FindState(OrchestraStateMachine machine, string name)
    {
        for (int index = 0; index < machine.States.Count; index++)
        {
            if (machine.States[index].Name == name) return machine.States[index];
        }
        return null;
    }

    internal static void WriteState(XmlWriter writer, OrchestraState state, bool initial, string namespaceUri)
    {
        string elementName = initial ? "initial" : "state";
        writer.WriteStartElement("fixr", elementName, namespaceUri);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "name", state.Name);
        OrchestraXmlEmitter.WriteExtensionAttributes(writer, state.Extensions);
        for (int index = 0; index < state.Transitions.Count; index++) WriteTransition(writer, state.Transitions[index], namespaceUri);
        WriteActionBlock(writer, "onentry", state.EntryActions, namespaceUri);
        WriteActionBlock(writer, "activity", state.ActivityActions, namespaceUri);
        WriteActionBlock(writer, "onexit", state.ExitActions, namespaceUri);
        OrchestraXmlEmitter.WriteAnnotations(writer, state.Annotations, namespaceUri);
        OrchestraXmlEmitter.WriteExtensions(writer, state.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteTransition(XmlWriter writer, OrchestraTransition transition, string namespaceUri)
    {
        writer.WriteStartElement("fixr", "transition", namespaceUri);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "name", transition.Name);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "trigger", transition.Trigger);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "target", transition.TargetState);
        OrchestraXmlEmitter.WriteExtensionAttributes(writer, transition.Extensions);
        string condition = transition.Condition;
        if (condition.Length == 0 && transition.Rule != null) condition = transition.Rule.Expression;
        if (condition.Length != 0) writer.WriteElementString("fixr", "when", namespaceUri, condition);
        if (transition.Responses.Count != 0)
        {
            writer.WriteStartElement("fixr", "responses", namespaceUri);
            for (int index = 0; index < transition.Responses.Count; index++) WriteResponse(writer, transition.Responses[index], namespaceUri);
            writer.WriteEndElement();
        }
        OrchestraXmlEmitter.WriteAnnotations(writer, transition.Annotations, namespaceUri);
        OrchestraXmlEmitter.WriteExtensions(writer, transition.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteMessageBehavior(XmlWriter writer, OrchestraMessageDefinition message, string namespaceUri)
    {
        for (int index = 0; index < message.Rules.Count; index++)
        {
            OrchestraRule rule = message.Rules[index];
            if (rule.Kind == OrchestraRuleKind.Assignment)
            {
                writer.WriteElementString("fixr", "assign", namespaceUri, rule.Expression);
            }
            else if (rule.Expression.Length != 0)
            {
                writer.WriteElementString("fixr", "when", namespaceUri, rule.Expression);
                index = message.Rules.Count;
            }
        }
        if (message.Responses.Count != 0)
        {
            writer.WriteStartElement("fixr", "responses", namespaceUri);
            for (int index = 0; index < message.Responses.Count; index++) WriteResponse(writer, message.Responses[index], namespaceUri);
            writer.WriteEndElement();
        }
    }

    internal static void WriteResponse(XmlWriter writer, OrchestraResponse response, string namespaceUri)
    {
        writer.WriteStartElement("fixr", "response", namespaceUri);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "name", response.Name);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "sync", response.Synchronization);
        OrchestraXmlEmitter.WriteExtensionAttributes(writer, response.Extensions);
        string condition = response.Condition;
        if (condition.Length == 0 && response.Rule != null) condition = response.Rule.Expression;
        if (condition.Length != 0) writer.WriteElementString("fixr", "when", namespaceUri, condition);
        for (int index = 0; index < response.Actions.Count; index++) WriteAction(writer, response.Actions[index], namespaceUri);
        if (response.Actions.Count == 0 && response.MessageName.Length != 0)
        {
            OrchestraAction action = new OrchestraAction();
            action.Kind = "messageRef";
            action.Name = response.MessageName;
            action.Scenario = response.Scenario;
            WriteAction(writer, action, namespaceUri);
        }
        if (response.Actions.Count == 0)
        {
            for (int index = 0; index < response.Assignments.Count; index++) WriteRule(writer, response.Assignments[index], namespaceUri);
        }
        OrchestraXmlEmitter.WriteAnnotations(writer, response.Annotations, namespaceUri);
        OrchestraXmlEmitter.WriteExtensions(writer, response.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteActionBlock(XmlWriter writer, string name, List<OrchestraAction> actions, string namespaceUri)
    {
        if (actions.Count == 0) return;
        writer.WriteStartElement("fixr", name, namespaceUri);
        for (int index = 0; index < actions.Count; index++) WriteAction(writer, actions[index], namespaceUri);
        writer.WriteEndElement();
    }

    internal static void WriteAction(XmlWriter writer, OrchestraAction action, string namespaceUri)
    {
        string kind = action.Kind.Length == 0 ? "trigger" : action.Kind;
        if (kind == "assign")
        {
            writer.WriteElementString("fixr", "assign", namespaceUri, action.Expression);
            return;
        }
        writer.WriteStartElement("fixr", kind, namespaceUri);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "id", action.ReferenceId);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "name", action.Name);
        OrchestraXmlEmitter.WriteScenarioAttribute(writer, action.Scenario);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "msgType", action.MessageType);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "actor", action.Actor);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "stateMachine", action.StateMachine);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "operation", action.Operation);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "interval", action.Interval);
        OrchestraXmlEmitter.WriteExtensionAttributes(writer, action.Extensions);
        if (action.Actions.Count != 0) WriteActionBlock(writer, "activity", action.Actions, namespaceUri);
        OrchestraXmlEmitter.WriteExtensions(writer, action.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteRules(XmlWriter writer, List<OrchestraRule> rules, string namespaceUri)
    {
        for (int index = 0; index < rules.Count; index++) WriteRule(writer, rules[index], namespaceUri);
    }

    internal static void WriteRule(XmlWriter writer, OrchestraRule rule, string namespaceUri)
    {
        if (rule.Kind == OrchestraRuleKind.Assignment || rule.SourceKind == "assign")
        {
            writer.WriteElementString("fixr", "assign", namespaceUri, rule.Expression);
            return;
        }
        writer.WriteStartElement("fixr", "rule", namespaceUri);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "name", rule.Name);
        OrchestraXmlEmitter.WritePresenceAttribute(writer, rule.PresenceText, rule.Presence);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "type", rule.Type);
        OrchestraXmlEmitter.WriteExtensionAttributes(writer, rule.Extensions);
        if (rule.UniqueFieldIds.Count != 0)
        {
            writer.WriteStartElement("fixr", "unique", namespaceUri);
            for (int index = 0; index < rule.UniqueFieldIds.Count; index++)
            {
                writer.WriteStartElement("fixr", "fieldRef", namespaceUri);
                OrchestraXmlEmitter.WriteOptionalAttribute(writer, "id", rule.UniqueFieldIds[index]);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        for (int index = 0; index < rule.Assignments.Count; index++) WriteRule(writer, rule.Assignments[index], namespaceUri);
        if (rule.Expression.Length != 0) writer.WriteElementString("fixr", "when", namespaceUri, rule.Expression);
        OrchestraXmlEmitter.WriteAnnotations(writer, rule.Annotations, namespaceUri);
        OrchestraXmlEmitter.WriteExtensions(writer, rule.Extensions);
        writer.WriteEndElement();
    }
}
