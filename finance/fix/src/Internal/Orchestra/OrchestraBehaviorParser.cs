using System.Collections.Generic;
using System.Xml;

namespace fix;

internal static class OrchestraBehaviorParser
{
    internal static void ParseRepositoryBehavior(XmlElement root, OrchestraRepository repository)
    {
        ParseActorsAndFlows(root, repository);
        ParseFieldRules(root, repository);
        ParseComponentMemberRules(root, repository);
        ParseGroupMemberRules(root, repository);
        ParseMessageBehavior(root, repository);
        PopulateFlowMessages(repository);
    }

    internal static void ParseActorsAndFlows(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "actors", root.NamespaceURI);
        if (container == null) return;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (OrchestraVersion10Parser.IsNativeElement(element, "actor", root.NamespaceURI) && element != null)
            {
                repository.Actors.Add(ParseActor(element, repository, root.NamespaceURI));
            }
            else if (OrchestraVersion10Parser.IsNativeElement(element, "flow", root.NamespaceURI) && element != null)
            {
                repository.Flows.Add(ParseFlow(element, root.NamespaceURI));
            }
        }
        OrchestraCommonParser.ParseAnnotations(container, repository.Annotations, root.NamespaceURI);
        OrchestraCommonParser.AddForeignExtensions(container, root.NamespaceURI, repository.Extensions);
    }

    internal static OrchestraActor ParseActor(XmlElement element, OrchestraRepository repository, string namespaceUri)
    {
        OrchestraActor actor = new OrchestraActor();
        actor.Name = element.GetAttribute("name").Trim();
        for (int index = 0; index < element.ChildNodes.Count; index++)
        {
            XmlElement? child = element.ChildNodes[index] as XmlElement;
            if (child == null || child.NamespaceURI != namespaceUri) continue;
            if (IsVariableElement(child.LocalName))
            {
                OrchestraVariable variable = ParseVariable(child, repository, namespaceUri);
                actor.Variables.Add(variable);
                repository.Variables.Add(variable);
            }
            else if (child.LocalName == "timer")
            {
                OrchestraTimer timer = ParseTimerDeclaration(child, actor.Name, namespaceUri);
                actor.Timers.Add(timer);
                repository.Timers.Add(timer);
            }
            else if (child.LocalName == "states")
            {
                OrchestraStateMachine machine = ParseStateMachine(child, repository, namespaceUri);
                machine.Actor = actor.Name;
                actor.StateMachines.Add(machine.Name);
                repository.StateMachines.Add(machine);
            }
        }
        OrchestraCommonParser.ParseAnnotations(element, actor.Annotations, namespaceUri);
        OrchestraVersion10Parser.CopyAttributes(element, actor.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, actor.Extensions);
        return actor;
    }

    internal static bool IsVariableElement(string name)
    {
        return name == "field" || name == "fieldRef" || name == "component" || name == "componentRef" || name == "group" || name == "groupRef";
    }

    internal static OrchestraVariable ParseVariable(XmlElement element, OrchestraRepository repository, string namespaceUri)
    {
        OrchestraVariable variable = new OrchestraVariable();
        variable.Kind = element.LocalName;
        variable.Id = element.GetAttribute("id").Trim();
        if (element.LocalName.EndsWith("Ref")) variable.ReferenceId = variable.Id;
        variable.Name = element.GetAttribute("name").Trim();
        variable.Scenario = OrchestraVersion10Parser.ReadScenario(element);
        variable.Datatype = element.GetAttribute("type").Trim();
        variable.InitialValue = element.GetAttribute("value");
        variable.IsExternal = variable.ReferenceId.Length != 0;
        ParseRulesFromOwner(element, variable.Rules, repository, namespaceUri);
        OrchestraCommonParser.ParseAnnotations(element, variable.Annotations, namespaceUri);
        OrchestraVersion10Parser.CopyAttributes(element, variable.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, variable.Extensions);
        variable.Xml = element.OuterXml;
        return variable;
    }

    internal static OrchestraTimer ParseTimerDeclaration(XmlElement element, string actorName, string namespaceUri)
    {
        OrchestraTimer timer = new OrchestraTimer();
        timer.Name = element.GetAttribute("name").Trim();
        timer.Actor = actorName;
        OrchestraVersion10Parser.CopyAttributes(element, timer.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, timer.Extensions);
        timer.Xml = element.OuterXml;
        return timer;
    }

    internal static OrchestraFlow ParseFlow(XmlElement element, string namespaceUri)
    {
        OrchestraFlow flow = new OrchestraFlow();
        flow.Name = element.GetAttribute("name").Trim();
        flow.SourceActor = element.GetAttribute("source").Trim();
        flow.DestinationActor = element.GetAttribute("destination").Trim();
        flow.Reliability = element.GetAttribute("reliability").Trim();
        OrchestraCommonParser.ParseAnnotations(element, flow.Annotations, namespaceUri);
        OrchestraVersion10Parser.CopyAttributes(element, flow.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, flow.Extensions);
        return flow;
    }

    internal static OrchestraStateMachine ParseStateMachine(XmlElement element, OrchestraRepository repository, string namespaceUri)
    {
        OrchestraStateMachine machine = new OrchestraStateMachine();
        machine.Name = element.GetAttribute("name").Trim();
        for (int index = 0; index < element.ChildNodes.Count; index++)
        {
            XmlElement? stateElement = element.ChildNodes[index] as XmlElement;
            if (stateElement == null || stateElement.NamespaceURI != namespaceUri) continue;
            if (IsVariableElement(stateElement.LocalName))
            {
                OrchestraVariable variable = ParseVariable(stateElement, repository, namespaceUri);
                machine.Variables.Add(variable);
                repository.Variables.Add(variable);
            }
            else if (stateElement.LocalName == "initial")
            {
                OrchestraState state = ParseState(stateElement, repository, namespaceUri);
                machine.InitialState = state.Name;
                machine.States.Add(state);
            }
            else if (stateElement.LocalName == "state")
            {
                machine.States.Add(ParseState(stateElement, repository, namespaceUri));
            }
        }
        OrchestraCommonParser.ParseAnnotations(element, machine.Annotations, namespaceUri);
        OrchestraVersion10Parser.CopyAttributes(element, machine.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, machine.Extensions);
        return machine;
    }

    internal static OrchestraState ParseState(XmlElement element, OrchestraRepository repository, string namespaceUri)
    {
        OrchestraState state = new OrchestraState();
        state.Name = element.GetAttribute("name").Trim();
        for (int index = 0; index < element.ChildNodes.Count; index++)
        {
            XmlElement? child = element.ChildNodes[index] as XmlElement;
            if (child == null || child.NamespaceURI != namespaceUri) continue;
            if (child.LocalName == "transition") state.Transitions.Add(ParseTransition(child, repository, namespaceUri));
            else if (child.LocalName == "onentry") ParseActionContainer(child, state.EntryActions, repository, namespaceUri);
            else if (child.LocalName == "activity") ParseActionContainer(child, state.ActivityActions, repository, namespaceUri);
            else if (child.LocalName == "onexit") ParseActionContainer(child, state.ExitActions, repository, namespaceUri);
        }
        OrchestraCommonParser.ParseAnnotations(element, state.Annotations, namespaceUri);
        OrchestraVersion10Parser.CopyAttributes(element, state.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, state.Extensions);
        return state;
    }

    internal static OrchestraTransition ParseTransition(XmlElement element, OrchestraRepository repository, string namespaceUri)
    {
        OrchestraTransition transition = new OrchestraTransition();
        transition.Name = element.GetAttribute("name").Trim();
        transition.Trigger = element.GetAttribute("trigger").Trim();
        transition.TargetState = element.GetAttribute("target").Trim();
        XmlElement? when = OrchestraCommonParser.FindDirectChild(element, "when", namespaceUri);
        if (when != null)
        {
            transition.Condition = when.InnerText;
            transition.Rule = ParseWhenRule(when, "transition", repository);
        }
        XmlElement? responses = OrchestraCommonParser.FindDirectChild(element, "responses", namespaceUri);
        if (responses != null) ParseTransitionResponses(responses, transition, repository, namespaceUri);
        OrchestraCommonParser.ParseAnnotations(element, transition.Annotations, namespaceUri);
        OrchestraVersion10Parser.CopyAttributes(element, transition.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, transition.Extensions);
        return transition;
    }

    internal static void ParseTransitionResponses(XmlElement container, OrchestraTransition transition, OrchestraRepository repository, string namespaceUri)
    {
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (OrchestraVersion10Parser.IsNativeElement(element, "response", namespaceUri) && element != null) transition.Responses.Add(ParseResponse(element, repository, namespaceUri));
        }
    }

    internal static void ParseActionContainer(XmlElement container, List<OrchestraAction> destination, OrchestraRepository repository, string namespaceUri)
    {
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (element == null || element.NamespaceURI != namespaceUri) continue;
            if (IsVariableElement(element.LocalName)) destination.Add(ParseParameterAction(element, namespaceUri));
            else if (element.LocalName == "messageRef") destination.Add(ParseMessageAction(element, namespaceUri));
            else if (element.LocalName == "assign") destination.Add(ParseAssignmentAction(element, repository, namespaceUri));
            else if (element.LocalName == "trigger") destination.Add(ParseTriggerAction(element, namespaceUri));
            else if (element.LocalName == "timerSchedule") destination.Add(ParseTimerAction(element, repository, namespaceUri));
        }
    }

    internal static OrchestraAction ParseParameterAction(XmlElement element, string namespaceUri)
    {
        OrchestraAction action = new OrchestraAction();
        action.Kind = element.LocalName;
        action.Name = element.GetAttribute("name").Trim();
        action.ReferenceId = element.GetAttribute("id").Trim();
        action.Scenario = OrchestraVersion10Parser.ReadScenario(element);
        OrchestraVersion10Parser.CopyAttributes(element, action.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, action.Extensions);
        action.Xml = element.OuterXml;
        return action;
    }

    internal static OrchestraAction ParseMessageAction(XmlElement element, string namespaceUri)
    {
        OrchestraAction action = new OrchestraAction();
        action.Kind = "messageRef";
        action.Name = element.GetAttribute("name").Trim();
        action.ReferenceId = element.GetAttribute("id").Trim();
        action.Scenario = OrchestraVersion10Parser.ReadScenario(element);
        action.MessageType = element.GetAttribute("msgType");
        OrchestraVersion10Parser.CopyAttributes(element, action.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, action.Extensions);
        action.Xml = element.OuterXml;
        return action;
    }

    internal static OrchestraAction ParseAssignmentAction(XmlElement element, OrchestraRepository repository, string namespaceUri)
    {
        OrchestraAction action = new OrchestraAction();
        action.Kind = "assign";
        action.Expression = element.InnerText;
        action.AssignmentTarget = ReadAssignmentTarget(action.Expression);
        action.Rule = ParseAssignmentRule(element, repository, namespaceUri);
        OrchestraVersion10Parser.CopyAttributes(element, action.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, action.Extensions);
        action.Xml = element.OuterXml;
        return action;
    }

    internal static OrchestraAction ParseTriggerAction(XmlElement element, string namespaceUri)
    {
        OrchestraAction action = new OrchestraAction();
        action.Kind = "trigger";
        action.Name = element.GetAttribute("name").Trim();
        action.Actor = element.GetAttribute("actor").Trim();
        action.StateMachine = element.GetAttribute("stateMachine").Trim();
        OrchestraVersion10Parser.CopyAttributes(element, action.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, action.Extensions);
        action.Xml = element.OuterXml;
        return action;
    }

    internal static OrchestraAction ParseTimerAction(XmlElement element, OrchestraRepository repository, string namespaceUri)
    {
        OrchestraAction action = new OrchestraAction();
        action.Kind = "timerSchedule";
        action.Name = element.GetAttribute("name").Trim();
        action.Actor = element.GetAttribute("actor").Trim();
        action.Operation = element.GetAttribute("operation").Trim();
        action.Interval = element.GetAttribute("interval").Trim();
        XmlElement? activity = OrchestraCommonParser.FindDirectChild(element, "activity", namespaceUri);
        if (activity != null) ParseActionContainer(activity, action.Actions, repository, namespaceUri);
        OrchestraTimer timer = new OrchestraTimer();
        timer.Name = action.Name;
        timer.Actor = action.Actor;
        timer.Operation = action.Operation;
        timer.Interval = action.Interval;
        timer.Actions = action.Actions;
        OrchestraVersion10Parser.CopyAttributes(element, timer.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, timer.Extensions);
        timer.Xml = element.OuterXml;
        action.Timer = timer;
        OrchestraVersion10Parser.CopyAttributes(element, action.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, action.Extensions);
        action.Xml = element.OuterXml;
        return action;
    }

    internal static void ParseFieldRules(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "fields", root.NamespaceURI);
        if (container == null) return;
        int definitionIndex = 0;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (!OrchestraVersion10Parser.IsNativeElement(element, "field", root.NamespaceURI)) continue;
            if (element != null && definitionIndex < repository.Fields.Count) ParseRulesFromOwner(element, repository.Fields[definitionIndex].Rules, repository, root.NamespaceURI);
            definitionIndex++;
        }
    }

    internal static void ParseComponentMemberRules(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "components", root.NamespaceURI);
        if (container == null) return;
        int definitionIndex = 0;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (!OrchestraVersion10Parser.IsNativeElement(element, "component", root.NamespaceURI)) continue;
            if (element != null && definitionIndex < repository.Components.Count) ParseMemberRules(element, repository.Components[definitionIndex].Members, repository, root.NamespaceURI);
            definitionIndex++;
        }
    }

    internal static void ParseGroupMemberRules(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "groups", root.NamespaceURI);
        if (container == null) return;
        int definitionIndex = 0;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (!OrchestraVersion10Parser.IsNativeElement(element, "group", root.NamespaceURI)) continue;
            if (element != null && definitionIndex < repository.Groups.Count)
            {
                OrchestraGroupDefinition definition = repository.Groups[definitionIndex];
                XmlElement? countElement = OrchestraCommonParser.FindDirectChild(element, "numInGroup", root.NamespaceURI);
                if (countElement != null && definition.CountMember != null) ParseRulesFromOwner(countElement, definition.CountMember.Rules, repository, root.NamespaceURI);
                ParseMemberRules(element, definition.Members, repository, root.NamespaceURI);
            }
            definitionIndex++;
        }
    }

    internal static void ParseMessageBehavior(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "messages", root.NamespaceURI);
        if (container == null) return;
        int definitionIndex = 0;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (!OrchestraVersion10Parser.IsNativeElement(element, "message", root.NamespaceURI)) continue;
            if (element != null && definitionIndex < repository.Messages.Count)
            {
                OrchestraMessageDefinition definition = repository.Messages[definitionIndex];
                XmlElement? structure = OrchestraCommonParser.FindDirectChild(element, "structure", root.NamespaceURI);
                if (structure != null) ParseMemberRules(structure, definition.Members, repository, root.NamespaceURI);
                XmlElement? when = OrchestraCommonParser.FindDirectChild(element, "when", root.NamespaceURI);
                if (when != null) definition.Rules.Add(ParseWhenRule(when, "message", repository));
                XmlElement? responses = OrchestraCommonParser.FindDirectChild(element, "responses", root.NamespaceURI);
                if (responses != null) ParseResponses(responses, definition, repository, root.NamespaceURI);
            }
            definitionIndex++;
        }
    }

    internal static void ParseMemberRules(XmlElement parent, List<OrchestraMemberDefinition> members, OrchestraRepository repository, string namespaceUri)
    {
        int memberIndex = 0;
        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlElement? element = parent.ChildNodes[index] as XmlElement;
            if (element != null && element.NamespaceURI == namespaceUri && OrchestraVersion10Parser.IsMemberElement(element.LocalName))
            {
                if (memberIndex < members.Count) ParseRulesFromOwner(element, members[memberIndex].Rules, repository, namespaceUri);
                memberIndex++;
            }
        }
    }

    internal static void ParseRulesFromOwner(XmlElement owner, List<OrchestraRule> destination, OrchestraRepository repository, string namespaceUri)
    {
        for (int index = 0; index < owner.ChildNodes.Count; index++)
        {
            XmlElement? element = owner.ChildNodes[index] as XmlElement;
            if (element == null || element.NamespaceURI != namespaceUri) continue;
            if (element.LocalName == "rule") destination.Add(ParseConditionalRule(element, repository, namespaceUri));
            else if (element.LocalName == "assign") destination.Add(ParseAssignmentRule(element, repository, namespaceUri));
        }
    }

    internal static OrchestraRule ParseConditionalRule(XmlElement element, OrchestraRepository repository, string namespaceUri)
    {
        OrchestraRule rule = new OrchestraRule();
        rule.Id = repository.Rules.Count + 1;
        rule.SourceKind = "rule";
        rule.Name = element.GetAttribute("name").Trim();
        rule.PresenceText = element.GetAttribute("presence").Trim();
        rule.Presence = OrchestraVersion10Parser.ParsePresence(rule.PresenceText);
        rule.Type = element.GetAttribute("type").Trim();
        rule.Kind = rule.PresenceText.Length == 0 ? OrchestraRuleKind.Conditional : OrchestraRuleKind.Presence;
        XmlElement? when = OrchestraCommonParser.FindDirectChild(element, "when", namespaceUri);
        if (when != null) rule.Expression = when.InnerText;
        XmlElement? unique = OrchestraCommonParser.FindDirectChild(element, "unique", namespaceUri);
        if (unique != null) ParseUniqueFields(unique, rule.UniqueFieldIds, namespaceUri);
        OrchestraVersion10Parser.CopyAttributes(element, rule.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, rule.Extensions);
        repository.Rules.Add(rule);
        for (int index = 0; index < element.ChildNodes.Count; index++)
        {
            XmlElement? child = element.ChildNodes[index] as XmlElement;
            if (OrchestraVersion10Parser.IsNativeElement(child, "assign", namespaceUri) && child != null)
            {
                rule.Assignments.Add(ParseAssignmentRule(child, repository, namespaceUri));
            }
        }
        return rule;
    }

    internal static void ParseUniqueFields(XmlElement unique, List<string> destination, string namespaceUri)
    {
        for (int index = 0; index < unique.ChildNodes.Count; index++)
        {
            XmlElement? element = unique.ChildNodes[index] as XmlElement;
            if (OrchestraVersion10Parser.IsNativeElement(element, "fieldRef", namespaceUri) && element != null)
            {
                destination.Add(element.GetAttribute("id").Trim());
            }
        }
    }

    internal static OrchestraRule ParseAssignmentRule(XmlElement element, OrchestraRepository repository, string namespaceUri)
    {
        OrchestraRule rule = new OrchestraRule();
        rule.Id = repository.Rules.Count + 1;
        rule.SourceKind = "assign";
        rule.Kind = OrchestraRuleKind.Assignment;
        rule.Expression = element.InnerText;
        rule.AssignmentTarget = ReadAssignmentTarget(rule.Expression);
        OrchestraCommonParser.ParseAnnotations(element, rule.Annotations, namespaceUri);
        OrchestraVersion10Parser.CopyAttributes(element, rule.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, rule.Extensions);
        repository.Rules.Add(rule);
        return rule;
    }

    internal static OrchestraRule ParseWhenRule(XmlElement element, string sourceKind, OrchestraRepository repository)
    {
        OrchestraRule rule = new OrchestraRule();
        rule.Id = repository.Rules.Count + 1;
        rule.SourceKind = sourceKind;
        rule.Kind = OrchestraRuleKind.Conditional;
        rule.Expression = element.InnerText;
        repository.Rules.Add(rule);
        return rule;
    }

    internal static string ReadAssignmentTarget(string expression)
    {
        int separator = expression.IndexOf('=');
        if (separator < 0) return "";
        return expression.Substring(0, separator).Trim();
    }

    internal static void ParseResponses(XmlElement container, OrchestraMessageDefinition message, OrchestraRepository repository, string namespaceUri)
    {
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (OrchestraVersion10Parser.IsNativeElement(element, "response", namespaceUri) && element != null)
            {
                message.Responses.Add(ParseResponse(element, repository, namespaceUri));
            }
        }
    }

    internal static OrchestraResponse ParseResponse(XmlElement element, OrchestraRepository repository, string namespaceUri)
    {
        OrchestraResponse response = new OrchestraResponse();
        response.Name = element.GetAttribute("name").Trim();
        response.Synchronization = element.GetAttribute("sync").Trim();
        ParseActionContainer(element, response.Actions, repository, namespaceUri);
        for (int index = 0; index < response.Actions.Count; index++)
        {
            OrchestraAction action = response.Actions[index];
            if (response.MessageName.Length == 0 && action.Kind == "messageRef")
            {
                response.MessageName = action.Name;
                response.Scenario = action.Scenario;
            }
            if (action.Rule != null) response.Assignments.Add(action.Rule);
        }
        XmlElement? when = OrchestraCommonParser.FindDirectChild(element, "when", namespaceUri);
        if (when != null)
        {
            response.Condition = when.InnerText;
            response.Rule = ParseWhenRule(when, "response", repository);
        }
        OrchestraCommonParser.ParseAnnotations(element, response.Annotations, namespaceUri);
        OrchestraVersion10Parser.CopyAttributes(element, response.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, response.Extensions);
        return response;
    }

    internal static void PopulateFlowMessages(OrchestraRepository repository)
    {
        for (int flowIndex = 0; flowIndex < repository.Flows.Count; flowIndex++)
        {
            OrchestraFlow flow = repository.Flows[flowIndex];
            for (int messageIndex = 0; messageIndex < repository.Messages.Count; messageIndex++)
            {
                OrchestraMessageDefinition message = repository.Messages[messageIndex];
                if (message.Flow == flow.Name) flow.Messages.Add(message.Name);
            }
        }
    }
}
