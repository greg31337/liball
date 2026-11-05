using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace fix;

internal static class OrchestraReaderCore
{
    internal static OrchestraReadResult ReadRepository(Stream source, OrchestraReadOptions options)
    {
        OrchestraReadResult result = new OrchestraReadResult();
        OrchestraXmlLoadResult loaded = OrchestraXmlLoader.LoadDocument(source, options);
        if (!CanParseDocument(loaded))
        {
            CopyDiagnostics(result.Diagnostics, loaded.Diagnostics);
            result.Success = false;
            return result;
        }

        if (options.ValidateSchema) OrchestraSchemaValidator.Validate(loaded);
        OrchestraRepository repository = ParseDocument(loaded);
        ApplyContentSelection(repository, options);
        OrchestraReadResult semantic = OrchestraSemanticValidator.ValidateRepository(repository);
        LocateSemanticDiagnostics(loaded, semantic.Diagnostics);

        result.Repository = repository;
        CopyDiagnostics(result.Diagnostics, loaded.Diagnostics);
        CopyDiagnostics(result.Diagnostics, semantic.Diagnostics);
        result.Success = !HasErrors(result.Diagnostics);
        return result;
    }

    internal static bool CanParseDocument(OrchestraXmlLoadResult loaded)
    {
        XmlElement? root = loaded.Root;
        if (root == null || loaded.Document == null) return false;
        string namespaceUri = root.NamespaceURI;
        bool repositoryNamespace = namespaceUri == OrchestraXmlLoader.RepositoryVersion10Namespace || namespaceUri == OrchestraXmlLoader.RepositoryVersion11Namespace;
        bool interfacesNamespace = namespaceUri == OrchestraXmlLoader.InterfacesVersion10Namespace || namespaceUri == OrchestraXmlLoader.InterfacesVersion11Namespace;
        if (repositoryNamespace) return root.LocalName == "repository";
        if (interfacesNamespace) return root.LocalName == "interfaces";
        return false;
    }

    internal static OrchestraRepository ParseDocument(OrchestraXmlLoadResult loaded)
    {
        if (loaded.Version == OrchestraVersion.Version10) return OrchestraVersion10Parser.ParseDocument(loaded);
        return OrchestraVersion11Parser.ParseDocument(loaded);
    }

    internal static void ApplyContentSelection(OrchestraRepository repository, OrchestraReadOptions options)
    {
        if (!options.IncludeBehavior) RemoveBehavior(repository);
        if (!options.IncludeInterfaces)
        {
            repository.Interfaces.Clear();
            repository.RepositoryLinks.Clear();
        }
    }

    internal static void RemoveBehavior(OrchestraRepository repository)
    {
        repository.Variables.Clear();
        repository.Timers.Clear();
        repository.Rules.Clear();
        repository.Actors.Clear();
        repository.Flows.Clear();
        repository.StateMachines.Clear();
        for (int index = 0; index < repository.Fields.Count; index++) repository.Fields[index].Rules.Clear();
        for (int index = 0; index < repository.Components.Count; index++) RemoveMemberRules(repository.Components[index].Members);
        for (int index = 0; index < repository.Groups.Count; index++)
        {
            OrchestraGroupDefinition group = repository.Groups[index];
            if (group.CountMember != null) group.CountMember.Rules.Clear();
            RemoveMemberRules(group.Members);
        }
        for (int index = 0; index < repository.Messages.Count; index++)
        {
            OrchestraMessageDefinition message = repository.Messages[index];
            message.Rules.Clear();
            message.Responses.Clear();
            RemoveMemberRules(message.Members);
        }
    }

    internal static void RemoveMemberRules(List<OrchestraMemberDefinition> members)
    {
        for (int index = 0; index < members.Count; index++) members[index].Rules.Clear();
    }

    internal static void LocateSemanticDiagnostics(OrchestraXmlLoadResult loaded, List<OrchestraDiagnostic> diagnostics)
    {
        XmlElement? root = loaded.Root;
        if (root == null) return;
        for (int index = 0; index < diagnostics.Count; index++)
        {
            OrchestraDiagnostic diagnostic = diagnostics[index];
            XmlElement element = FindDiagnosticElement(root, diagnostic.ElementPath);
            OrchestraXmlSourceLocation? location = loaded.GetLocation(element);
            if (location != null)
            {
                diagnostic.Source = location.Source;
                diagnostic.Line = location.Line;
                diagnostic.Column = location.Column;
                diagnostic.ElementPath = OrchestraXmlLoader.CreateElementPath(element);
            }
        }
    }

    internal static XmlElement FindDiagnosticElement(XmlElement root, string semanticPath)
    {
        string[] segments = semanticPath.Split('/');
        if (segments.Length < 2 || segments[1].Length == 0) return root;
        string category = segments[1];
        XmlElement? element = FindTopLevelElement(root, category, ReadSegment(segments, 2));
        if (element == null) return root;
        if (category == "codeSets" && ReadSegment(segments, 3) == "codes")
        {
            XmlElement? code = FindNamedChild(element, "code", ReadSegment(segments, 4));
            if (code != null) element = code;
        }
        if (ReadSegment(segments, 3) == "members")
        {
            XmlElement? member = FindMemberElement(element, OrchestraCommonParser.ParseNonnegativeInteger(ReadSegment(segments, 4)));
            if (member != null) element = member;
        }
        if (category == "interfaces") element = FindInterfaceDescendant(element, segments);
        if (category == "stateMachines") element = FindStateMachineDescendant(element, segments);
        return element;
    }

    internal static XmlElement? FindTopLevelElement(XmlElement root, string category, string name)
    {
        if (category == "repositoryLinks") return root;
        if (category == "actors" || category == "flows")
        {
            XmlElement? actors = OrchestraCommonParser.FindDirectChild(root, "actors", root.NamespaceURI);
            if (actors == null) return null;
            string actorElementName = category == "actors" ? "actor" : "flow";
            return FindNamedChild(actors, actorElementName, name);
        }
        if (category == "stateMachines") return FindNamedDescendant(root, "states", name);
        if (category == "interfaces") return FindNamedChild(root, "interface", name);
        if (category == "rules") return FindRuleElement(root, name);
        if (category == "timers") return FindNamedDescendant(root, "timer", name);
        string containerName = category;
        string elementName = ReadElementName(category);
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, containerName, root.NamespaceURI);
        if (container == null) return null;
        if (name.Length == 0) return container;
        XmlElement? named = FindNamedChild(container, elementName, name);
        if (named != null) return named;
        return container;
    }

    internal static string ReadElementName(string category)
    {
        if (category == "datatypes") return "datatype";
        if (category == "encodingStandards") return "encodingStandard";
        if (category == "scenarios") return "scenario";
        if (category == "codeSets") return "codeSet";
        if (category == "fields") return "field";
        if (category == "components") return "component";
        if (category == "groups") return "group";
        if (category == "messages") return "message";
        if (category == "categories") return "category";
        if (category == "sections") return "section";
        if (category == "concepts") return "concept";
        return "";
    }

    internal static XmlElement? FindNamedChild(XmlElement parent, string elementName, string name)
    {
        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlElement? child = parent.ChildNodes[index] as XmlElement;
            if (child != null && child.NamespaceURI == parent.NamespaceURI && child.LocalName == elementName)
            {
                string childName = child.GetAttribute("name").Trim();
                if (childName.Length == 0) childName = child.GetAttribute("id").Trim();
                if (name.Length == 0 || childName == name) return child;
            }
        }
        return null;
    }

    internal static XmlElement? FindNamedDescendant(XmlElement parent, string elementName, string name)
    {
        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlElement? child = parent.ChildNodes[index] as XmlElement;
            if (child != null)
            {
                if (child.NamespaceURI == parent.NamespaceURI && child.LocalName == elementName && (name.Length == 0 || child.GetAttribute("name").Trim() == name)) return child;
                XmlElement? nested = FindNamedDescendant(child, elementName, name);
                if (nested != null) return nested;
            }
        }
        return null;
    }

    internal static XmlElement? FindMemberElement(XmlElement owner, int position)
    {
        if (position < 0) return null;
        XmlElement? parent = owner;
        XmlElement? structure = OrchestraCommonParser.FindDirectChild(owner, "structure", owner.NamespaceURI);
        if (structure != null) parent = structure;
        if (parent == null) return null;
        int memberPosition = 0;
        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlElement? child = parent.ChildNodes[index] as XmlElement;
            if (child != null && child.NamespaceURI == parent.NamespaceURI && IsMemberElement(child.LocalName))
            {
                if (memberPosition == position) return child;
                memberPosition++;
            }
        }
        return null;
    }

    internal static bool IsMemberElement(string name)
    {
        return name == "fieldRef" || name == "componentRef" || name == "groupRef";
    }

    internal static XmlElement FindInterfaceDescendant(XmlElement element, string[] segments)
    {
        string section = ReadSegment(segments, 3);
        if (section == "sessions")
        {
            XmlElement? sessions = OrchestraCommonParser.FindDirectChild(element, "sessions", element.NamespaceURI);
            if (sessions == null) return element;
            XmlElement? session = FindNamedChild(sessions, "session", ReadSegment(segments, 4));
            if (session != null) return session;
            return sessions;
        }
        string elementName = section;
        if (section == "services") elementName = "service";
        else if (section == "encodings") elementName = "encoding";
        else if (section == "sessionProtocols") elementName = "sessionProtocol";
        else if (section == "protocols") elementName = "protocol";
        else if (section == "transports") elementName = "transport";
        XmlElement? child = FindNamedChild(element, elementName, ReadSegment(segments, 4));
        return child == null ? element : child;
    }

    internal static XmlElement FindStateMachineDescendant(XmlElement element, string[] segments)
    {
        if (ReadSegment(segments, 3) != "states") return element;
        XmlElement? state = FindNamedChild(element, "state", ReadSegment(segments, 4));
        if (state == null) state = FindNamedChild(element, "initial", ReadSegment(segments, 4));
        if (state == null) return element;
        if (ReadSegment(segments, 5) != "transitions") return state;
        XmlElement? transition = FindNamedChild(state, "transition", ReadSegment(segments, 6));
        return transition == null ? state : transition;
    }

    internal static XmlElement? FindRuleElement(XmlElement root, string name)
    {
        XmlElement? rule = FindNamedDescendant(root, "rule", name);
        if (rule == null) rule = FindNamedDescendant(root, "when", name);
        if (rule == null) rule = FindNamedDescendant(root, "assign", name);
        return rule;
    }

    internal static string ReadSegment(string[] segments, int index)
    {
        if (index >= 0 && index < segments.Length) return segments[index];
        return "";
    }

    internal static void CopyDiagnostics(List<OrchestraDiagnostic> destination, List<OrchestraDiagnostic> source)
    {
        for (int index = 0; index < source.Count; index++) destination.Add(source[index]);
    }

    internal static bool HasErrors(List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < diagnostics.Count; index++)
        {
            ValidationSeverity severity = diagnostics[index].Severity;
            if (severity == ValidationSeverity.Error || severity == ValidationSeverity.Fatal) return true;
        }
        return false;
    }
}
