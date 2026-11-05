using System.Collections.Generic;
using System.Xml;

namespace fix;

internal static class OrchestraVersion10Parser
{
    internal static OrchestraRepository ParseDocument(OrchestraXmlLoadResult source)
    {
        if (source.InterfacesDocument && source.Version == OrchestraVersion.Version10) return OrchestraInterfacesParser.ParseDocument(source);
        OrchestraRepository repository = OrchestraCommonParser.ParseDocument(source);
        XmlElement? root = source.Root;
        if (root == null || source.Version != OrchestraVersion.Version10) return repository;
        ParseDatatypes(root, repository);
        ParseCodeSets(root, repository);
        ParseFields(root, repository);
        ParseComponents(root, repository);
        ParseGroups(root, repository);
        ParseMessages(root, repository);
        return repository;
    }

    internal static void ParseDatatypes(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "datatypes", root.NamespaceURI);
        if (container == null) return;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (element != null && IsNativeElement(element, "datatype", root.NamespaceURI))
            {
                OrchestraDatatypeDefinition datatype = new OrchestraDatatypeDefinition();
                datatype.Name = element.GetAttribute("name").Trim();
                datatype.BaseType = element.GetAttribute("baseType").Trim();
                datatype.Pedigree = OrchestraCommonParser.ParsePedigree(element);
                OrchestraCommonParser.ParseAnnotations(element, datatype.Annotations, root.NamespaceURI);
                CopyAttributes(element, datatype.Attributes);
                OrchestraCommonParser.AddForeignExtensions(element, root.NamespaceURI, datatype.Extensions);
                repository.Datatypes.Add(datatype);
            }
        }
    }

    internal static void ParseCodeSets(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "codeSets", root.NamespaceURI);
        if (container == null) return;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (element != null && IsNativeElement(element, "codeSet", root.NamespaceURI))
            {
                OrchestraCodeSetDefinition codeSet = new OrchestraCodeSetDefinition();
                codeSet.Id = element.GetAttribute("id").Trim();
                codeSet.Name = element.GetAttribute("name").Trim();
                codeSet.Scenario = ReadScenario(element);
                codeSet.Type = element.GetAttribute("type").Trim();
                codeSet.DefaultValue = element.GetAttribute("default");
                codeSet.SpecificationUri = element.GetAttribute("specUrl").Trim();
                codeSet.Pedigree = OrchestraCommonParser.ParsePedigree(element);
                OrchestraCommonParser.ParseAnnotations(element, codeSet.Annotations, root.NamespaceURI);
                CopyAttributes(element, codeSet.Attributes);
                OrchestraCommonParser.AddForeignExtensions(element, root.NamespaceURI, codeSet.Extensions);
                RegisterScenario(repository, codeSet.Scenario);
                ParseCodes(element, codeSet, repository, root.NamespaceURI);
                repository.CodeSets.Add(codeSet);
            }
        }
    }

    internal static void ParseCodes(XmlElement parent, OrchestraCodeSetDefinition codeSet, OrchestraRepository repository, string namespaceUri)
    {
        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlElement? element = parent.ChildNodes[index] as XmlElement;
            if (element != null && IsNativeElement(element, "code", namespaceUri))
            {
                OrchestraCodeDefinition code = new OrchestraCodeDefinition();
                code.Id = element.GetAttribute("id").Trim();
                code.Name = element.GetAttribute("name").Trim();
                code.Scenario = ReadScenario(element);
                code.Value = element.GetAttribute("value");
                code.Sort = element.GetAttribute("sort").Trim();
                code.Group = element.GetAttribute("group").Trim();
                code.Pedigree = OrchestraCommonParser.ParsePedigree(element);
                OrchestraCommonParser.ParseAnnotations(element, code.Annotations, namespaceUri);
                CopyAttributes(element, code.Attributes);
                OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, code.Extensions);
                codeSet.Codes.Add(code);
                RegisterScenario(repository, code.Scenario);
            }
        }
    }

    internal static void ParseFields(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "fields", root.NamespaceURI);
        if (container == null) return;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (element != null && IsNativeElement(element, "field", root.NamespaceURI))
            {
                OrchestraFieldDefinition field = new OrchestraFieldDefinition();
                field.Id = element.GetAttribute("id").Trim();
                field.Name = element.GetAttribute("name").Trim();
                field.Abbreviation = element.GetAttribute("abbrName").Trim();
                field.Scenario = ReadScenario(element);
                field.Type = element.GetAttribute("type").Trim();
                if (IsCodeSetReference(repository, field.Type, field.Scenario))
                {
                    field.CodeSet = field.Type;
                    field.Type = "";
                }
                field.PresenceText = element.GetAttribute("presence").Trim();
                field.Presence = ParsePresence(field.PresenceText);
                field.Value = element.GetAttribute("value");
                field.LengthId = element.GetAttribute("lengthId").Trim();
                field.DiscriminatorId = element.GetAttribute("discriminatorId").Trim();
                field.Pedigree = OrchestraCommonParser.ParsePedigree(element);
                OrchestraCommonParser.ParseAnnotations(element, field.Annotations, root.NamespaceURI);
                CopyAttributes(element, field.Attributes);
                OrchestraCommonParser.AddForeignExtensions(element, root.NamespaceURI, field.Extensions);
                repository.Fields.Add(field);
                RegisterScenario(repository, field.Scenario);
            }
        }
    }

    internal static bool IsCodeSetReference(OrchestraRepository repository, string name, string scenario)
    {
        for (int index = 0; index < repository.CodeSets.Count; index++)
        {
            OrchestraCodeSetDefinition codeSet = repository.CodeSets[index];
            if (codeSet.Name == name && (codeSet.Scenario == scenario || codeSet.Scenario == "base" && (scenario.Length == 0 || scenario == "base"))) return true;
        }
        return false;
    }

    internal static void ParseComponents(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "components", root.NamespaceURI);
        if (container == null) return;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (element != null && IsNativeElement(element, "component", root.NamespaceURI))
            {
                OrchestraComponentDefinition component = new OrchestraComponentDefinition();
                component.Id = element.GetAttribute("id").Trim();
                component.Name = element.GetAttribute("name").Trim();
                component.Abbreviation = element.GetAttribute("abbrName").Trim();
                component.Scenario = ReadScenario(element);
                component.Category = element.GetAttribute("category").Trim();
                component.Rendering = element.GetAttribute("rendering").Trim();
                component.Selection = element.GetAttribute("which").Trim();
                component.Pedigree = OrchestraCommonParser.ParsePedigree(element);
                RegisterScenario(repository, component.Scenario);
                ParseMembers(element, component.Members, repository, root.NamespaceURI);
                OrchestraCommonParser.ParseAnnotations(element, component.Annotations, root.NamespaceURI);
                CopyAttributes(element, component.Attributes);
                OrchestraCommonParser.AddForeignExtensions(element, root.NamespaceURI, component.Extensions);
                repository.Components.Add(component);
            }
        }
    }

    internal static void ParseGroups(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "groups", root.NamespaceURI);
        if (container == null) return;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (element != null && IsNativeElement(element, "group", root.NamespaceURI))
            {
                OrchestraGroupDefinition group = new OrchestraGroupDefinition();
                group.Id = element.GetAttribute("id").Trim();
                group.Name = element.GetAttribute("name").Trim();
                group.Abbreviation = element.GetAttribute("abbrName").Trim();
                group.Scenario = ReadScenario(element);
                group.Category = element.GetAttribute("category").Trim();
                group.Rendering = element.GetAttribute("rendering").Trim();
                group.Selection = element.GetAttribute("which").Trim();
                group.Pedigree = OrchestraCommonParser.ParsePedigree(element);
                RegisterScenario(repository, group.Scenario);
                XmlElement? count = OrchestraCommonParser.FindDirectChild(element, "numInGroup", root.NamespaceURI);
                if (count != null) group.CountMember = ParseMember(count, 0, repository, root.NamespaceURI);
                ParseMembers(element, group.Members, repository, root.NamespaceURI);
                OrchestraCommonParser.ParseAnnotations(element, group.Annotations, root.NamespaceURI);
                CopyAttributes(element, group.Attributes);
                OrchestraCommonParser.AddForeignExtensions(element, root.NamespaceURI, group.Extensions);
                repository.Groups.Add(group);
            }
        }
    }

    internal static void ParseMessages(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "messages", root.NamespaceURI);
        if (container == null) return;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (element != null && IsNativeElement(element, "message", root.NamespaceURI))
            {
                OrchestraMessageDefinition message = new OrchestraMessageDefinition();
                message.Id = element.GetAttribute("id").Trim();
                message.Name = element.GetAttribute("name").Trim();
                message.Abbreviation = element.GetAttribute("abbrName").Trim();
                message.Scenario = ReadScenario(element);
                message.MessageType = element.GetAttribute("msgType");
                message.Category = element.GetAttribute("category").Trim();
                message.Flow = element.GetAttribute("flow").Trim();
                message.Pedigree = OrchestraCommonParser.ParsePedigree(element);
                RegisterScenario(repository, message.Scenario);
                XmlElement? structure = OrchestraCommonParser.FindDirectChild(element, "structure", root.NamespaceURI);
                if (structure != null)
                {
                    message.Selection = structure.GetAttribute("which").Trim();
                    ParseMembers(structure, message.Members, repository, root.NamespaceURI);
                }
                OrchestraCommonParser.ParseAnnotations(element, message.Annotations, root.NamespaceURI);
                CopyAttributes(element, message.Attributes);
                OrchestraCommonParser.AddForeignExtensions(element, root.NamespaceURI, message.Extensions);
                repository.Messages.Add(message);
            }
        }
    }

    internal static void ParseMembers(XmlElement parent, List<OrchestraMemberDefinition> destination, OrchestraRepository repository, string namespaceUri)
    {
        int position = 0;
        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlElement? element = parent.ChildNodes[index] as XmlElement;
            if (element != null && element.NamespaceURI == namespaceUri && IsMemberElement(element.LocalName))
            {
                destination.Add(ParseMember(element, position, repository, namespaceUri));
                position++;
            }
        }
    }

    internal static OrchestraMemberDefinition ParseMember(XmlElement element, int position, OrchestraRepository repository, string namespaceUri)
    {
        OrchestraMemberDefinition member = new OrchestraMemberDefinition();
        member.Kind = element.LocalName == "numInGroup" ? "fieldRef" : element.LocalName;
        member.ReferenceId = element.GetAttribute("id").Trim();
        member.Scenario = ReadScenario(element);
        member.InstanceName = element.GetAttribute("instanceName").Trim();
        member.Position = position;
        member.PresenceText = element.GetAttribute("presence").Trim();
        member.Presence = ParsePresence(member.PresenceText);
        member.Value = element.GetAttribute("value");
        member.ImplementationMinimum = element.GetAttribute("implMinOccurs").Trim();
        member.ImplementationMaximum = element.GetAttribute("implMaxOccurs").Trim();
        member.LengthId = element.GetAttribute("lengthId").Trim();
        member.Pedigree = OrchestraCommonParser.ParsePedigree(element);
        OrchestraCommonParser.ParseAnnotations(element, member.Annotations, namespaceUri);
        CopyAttributes(element, member.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, member.Extensions);
        RegisterScenario(repository, member.Scenario);
        return member;
    }

    internal static bool IsMemberElement(string name)
    {
        return name == "fieldRef" || name == "componentRef" || name == "groupRef";
    }

    internal static bool IsNativeElement(XmlElement? element, string name, string namespaceUri)
    {
        return element != null && element.LocalName == name && element.NamespaceURI == namespaceUri;
    }

    internal static string ReadScenario(XmlElement element)
    {
        string scenario = element.GetAttribute("scenario").Trim();
        return scenario.Length == 0 ? "base" : scenario;
    }

    internal static FixPresence ParsePresence(string value)
    {
        if (value == "required") return FixPresence.Required;
        if (value == "forbidden") return FixPresence.Forbidden;
        if (value == "ignored") return FixPresence.Ignored;
        if (value == "constant") return FixPresence.Constant;
        return FixPresence.Optional;
    }

    internal static void RegisterScenario(OrchestraRepository repository, string name)
    {
        for (int index = 0; index < repository.Scenarios.Count; index++)
        {
            if (repository.Scenarios[index].Name == name) return;
        }
        OrchestraScenarioDefinition scenario = new OrchestraScenarioDefinition();
        scenario.Name = name;
        repository.Scenarios.Add(scenario);
    }

    internal static void CopyAttributes(XmlElement element, Dictionary<string, string> destination)
    {
        for (int index = 0; index < element.Attributes.Count; index++)
        {
            XmlAttribute? attribute = element.Attributes[index];
            if (attribute != null && attribute.NamespaceURI != OrchestraCommonParser.XmlnsNamespace)
            {
                destination[attribute.Name] = attribute.Value;
            }
        }
    }
}
