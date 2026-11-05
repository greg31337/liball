using System.Collections.Generic;
using System.Xml;

namespace fix;

internal static class OrchestraVersion11Parser
{
    internal static OrchestraRepository ParseDocument(OrchestraXmlLoadResult source)
    {
        if (source.InterfacesDocument && source.Version == OrchestraVersion.Version11) return OrchestraInterfacesParser.ParseDocument(source);
        OrchestraRepository repository = OrchestraCommonParser.ParseDocument(source);
        XmlElement? root = source.Root;
        if (root == null || source.Version != OrchestraVersion.Version11) return repository;

        ParseExplicitScenarios(root, repository);
        OrchestraVersion10Parser.ParseDatatypes(root, repository);
        OrchestraVersion10Parser.ParseCodeSets(root, repository);
        OrchestraVersion10Parser.ParseFields(root, repository);
        OrchestraVersion10Parser.ParseComponents(root, repository);
        OrchestraVersion10Parser.ParseGroups(root, repository);
        OrchestraVersion10Parser.ParseMessages(root, repository);
        ParseEncodingStandards(root, repository);
        ParseDatatypeMappings(root, repository);
        ParseContainerEncodings(root, repository);
        ParseCategoryEncodings(root, repository);
        ParseSectionEncodings(root, repository);
        ParseCodeSetEncodings(root, repository);
        ParseFieldEncodings(root, repository);
        ParseComponentEncodings(root, repository);
        ParseGroupEncodings(root, repository);
        ParseMessageEncodings(root, repository);
        OrchestraBehaviorParser.ParseRepositoryBehavior(root, repository);
        return repository;
    }

    internal static void ParseContainerEncodings(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? codeSets = OrchestraCommonParser.FindDirectChild(root, "codeSets", root.NamespaceURI);
        if (codeSets != null) ParseEncodings(codeSets, repository.CodeSetEncodings, root.NamespaceURI);
        XmlElement? components = OrchestraCommonParser.FindDirectChild(root, "components", root.NamespaceURI);
        if (components != null) ParseEncodings(components, repository.ComponentEncodings, root.NamespaceURI);
        XmlElement? groups = OrchestraCommonParser.FindDirectChild(root, "groups", root.NamespaceURI);
        if (groups != null) ParseEncodings(groups, repository.GroupEncodings, root.NamespaceURI);
        XmlElement? messages = OrchestraCommonParser.FindDirectChild(root, "messages", root.NamespaceURI);
        if (messages != null) ParseEncodings(messages, repository.MessageEncodings, root.NamespaceURI);
    }

    internal static void ParseExplicitScenarios(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "scenarios", root.NamespaceURI);
        if (container == null) return;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (!OrchestraVersion10Parser.IsNativeElement(element, "scenario", root.NamespaceURI)) continue;
            if (element == null) continue;
            string name = element.GetAttribute("name").Trim();
            if (name.Length == 0) name = "base";
            OrchestraScenarioDefinition? scenario = FindScenario(repository, name);
            if (scenario == null)
            {
                scenario = new OrchestraScenarioDefinition();
                scenario.Name = name;
                repository.Scenarios.Add(scenario);
            }
            scenario.ParentName = element.GetAttribute("scenarioRef").Trim();
            scenario.Pedigree = OrchestraCommonParser.ParsePedigree(element);
            OrchestraCommonParser.ParseAnnotations(element, scenario.Annotations, root.NamespaceURI);
            OrchestraVersion10Parser.CopyAttributes(element, scenario.Attributes);
            OrchestraCommonParser.AddForeignExtensions(element, root.NamespaceURI, scenario.Extensions);
        }
    }

    internal static OrchestraScenarioDefinition? FindScenario(OrchestraRepository repository, string name)
    {
        for (int index = 0; index < repository.Scenarios.Count; index++)
        {
            if (repository.Scenarios[index].Name == name) return repository.Scenarios[index];
        }
        return null;
    }

    internal static void ParseEncodingStandards(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "encodingStandards", root.NamespaceURI);
        if (container == null) return;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (!OrchestraVersion10Parser.IsNativeElement(element, "encodingStandard", root.NamespaceURI)) continue;
            if (element == null) continue;
            OrchestraEncodingStandard standard = new OrchestraEncodingStandard();
            standard.Name = element.GetAttribute("name").Trim();
            standard.DisplayName = element.GetAttribute("displayName").Trim();
            XmlElement? encodingElement = OrchestraCommonParser.FindDirectChild(element, "encoding", root.NamespaceURI);
            if (encodingElement != null) standard.Encoding = ParseEncodingElement(encodingElement, root.NamespaceURI);
            OrchestraCommonParser.ParseAnnotations(element, standard.Annotations, root.NamespaceURI);
            OrchestraVersion10Parser.CopyAttributes(element, standard.Attributes);
            OrchestraCommonParser.AddForeignExtensions(element, root.NamespaceURI, standard.Extensions);
            repository.EncodingStandards.Add(standard);
        }
    }

    internal static void ParseDatatypeMappings(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "datatypes", root.NamespaceURI);
        if (container == null) return;
        int datatypeIndex = 0;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (!OrchestraVersion10Parser.IsNativeElement(element, "datatype", root.NamespaceURI)) continue;
            if (element == null || datatypeIndex >= repository.Datatypes.Count) continue;
            OrchestraDatatypeDefinition datatype = repository.Datatypes[datatypeIndex];
            for (int childIndex = 0; childIndex < element.ChildNodes.Count; childIndex++)
            {
                XmlElement? mappingElement = element.ChildNodes[childIndex] as XmlElement;
                if (!OrchestraVersion10Parser.IsNativeElement(mappingElement, "mappedDatatype", root.NamespaceURI)) continue;
                if (mappingElement == null) continue;
                datatype.Mappings.Add(ParseDatatypeMapping(mappingElement, root.NamespaceURI));
            }
            datatypeIndex++;
        }
    }

    internal static OrchestraDatatypeMapping ParseDatatypeMapping(XmlElement element, string namespaceUri)
    {
        OrchestraDatatypeMapping mapping = new OrchestraDatatypeMapping();
        mapping.Standard = element.GetAttribute("standard").Trim();
        mapping.BuiltinText = element.GetAttribute("builtin").Trim();
        mapping.HasBuiltin = mapping.BuiltinText.Length != 0;
        mapping.Builtin = mapping.BuiltinText == "true" || mapping.BuiltinText == "1";
        mapping.Base = element.GetAttribute("base").Trim();
        mapping.Pattern = element.GetAttribute("pattern");
        mapping.Element = element.GetAttribute("element").Trim();
        mapping.Size = element.GetAttribute("size").Trim();
        mapping.Parameter = element.GetAttribute("parameter");
        mapping.MinimumInclusive = element.GetAttribute("minInclusive");
        mapping.MaximumInclusive = element.GetAttribute("maxInclusive");
        XmlElement? encodingElement = OrchestraCommonParser.FindDirectChild(element, "encoding", namespaceUri);
        if (encodingElement != null) mapping.Encoding = ParseEncodingElement(encodingElement, namespaceUri);
        OrchestraCommonParser.ParseAnnotations(element, mapping.Annotations, namespaceUri);
        OrchestraVersion10Parser.CopyAttributes(element, mapping.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, mapping.Extensions);
        return mapping;
    }

    internal static OrchestraEncoding ParseEncodingElement(XmlElement element, string namespaceUri)
    {
        OrchestraEncoding encoding = new OrchestraEncoding();
        encoding.Standard = element.GetAttribute("standard").Trim();
        OrchestraCommonParser.ParseAnnotations(element, encoding.Annotations, namespaceUri);
        OrchestraVersion10Parser.CopyAttributes(element, encoding.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, encoding.Extensions);
        for (int index = 0; index < element.ChildNodes.Count; index++)
        {
            XmlElement? child = element.ChildNodes[index] as XmlElement;
            if (child != null) AddSchemaReferences(child, encoding.SchemaReferences);
        }
        return encoding;
    }

    internal static void AddSchemaReferences(XmlElement element, List<OrchestraExternalSchemaReference> destination)
    {
        string location = ReadAttributeByLocalName(element, "schemaLocation");
        if (location.Length == 0) location = ReadAttributeByLocalName(element, "location");
        if (location.Length == 0) location = ReadAttributeByLocalName(element, "href");
        if (location.Length == 0) location = ReadAttributeByLocalName(element, "uri");
        string loweredName = element.LocalName.ToLowerInvariant();
        bool schemaElement = loweredName.Contains("schema") || loweredName == "import" || loweredName == "include" || loweredName == "redefine";
        if (location.Length != 0 || schemaElement)
        {
            OrchestraExternalSchemaReference reference = new OrchestraExternalSchemaReference();
            reference.Name = element.LocalName;
            reference.NamespaceUri = ReadAttributeByLocalName(element, "namespace");
            if (reference.NamespaceUri.Length == 0) reference.NamespaceUri = ReadAttributeByLocalName(element, "targetNamespace");
            if (reference.NamespaceUri.Length == 0) reference.NamespaceUri = element.NamespaceURI;
            reference.Location = location;
            reference.Xml = element.OuterXml;
            destination.Add(reference);
        }
        for (int index = 0; index < element.ChildNodes.Count; index++)
        {
            XmlElement? child = element.ChildNodes[index] as XmlElement;
            if (child != null) AddSchemaReferences(child, destination);
        }
    }

    internal static string ReadAttributeByLocalName(XmlElement element, string name)
    {
        for (int index = 0; index < element.Attributes.Count; index++)
        {
            XmlAttribute? attribute = element.Attributes[index];
            if (attribute != null && attribute.LocalName == name) return attribute.Value.Trim();
        }
        return "";
    }

    internal static void ParseEncodings(XmlElement parent, List<OrchestraEncoding> destination, string namespaceUri)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(parent, "encodings", namespaceUri);
        if (container == null) return;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (OrchestraVersion10Parser.IsNativeElement(element, "encoding", namespaceUri) && element != null)
            {
                destination.Add(ParseEncodingElement(element, namespaceUri));
            }
        }
    }

    internal static void ParseCategoryEncodings(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "categories", root.NamespaceURI);
        if (container == null) return;
        int definitionIndex = 0;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (!OrchestraVersion10Parser.IsNativeElement(element, "category", root.NamespaceURI)) continue;
            if (element != null && definitionIndex < repository.Categories.Count) ParseEncodings(element, repository.Categories[definitionIndex].Encodings, root.NamespaceURI);
            definitionIndex++;
        }
    }

    internal static void ParseSectionEncodings(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "sections", root.NamespaceURI);
        if (container == null) return;
        int definitionIndex = 0;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (!OrchestraVersion10Parser.IsNativeElement(element, "section", root.NamespaceURI)) continue;
            if (element != null && definitionIndex < repository.Sections.Count) ParseEncodings(element, repository.Sections[definitionIndex].Encodings, root.NamespaceURI);
            definitionIndex++;
        }
    }

    internal static void ParseCodeSetEncodings(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "codeSets", root.NamespaceURI);
        if (container == null) return;
        int definitionIndex = 0;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (!OrchestraVersion10Parser.IsNativeElement(element, "codeSet", root.NamespaceURI)) continue;
            if (element != null && definitionIndex < repository.CodeSets.Count)
            {
                OrchestraCodeSetDefinition definition = repository.CodeSets[definitionIndex];
                definition.UnionDatatype = element.GetAttribute("unionDataType").Trim();
                ParseEncodings(element, definition.Encodings, root.NamespaceURI);
            }
            definitionIndex++;
        }
    }

    internal static void ParseFieldEncodings(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "fields", root.NamespaceURI);
        if (container == null) return;
        int definitionIndex = 0;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (!OrchestraVersion10Parser.IsNativeElement(element, "field", root.NamespaceURI)) continue;
            if (element != null && definitionIndex < repository.Fields.Count)
            {
                OrchestraFieldDefinition definition = repository.Fields[definitionIndex];
                definition.CodeSet = element.GetAttribute("codeSet").Trim();
                definition.UnionDatatype = element.GetAttribute("unionDataType").Trim();
                ParseEncodings(element, definition.Encodings, root.NamespaceURI);
            }
            definitionIndex++;
        }
    }

    internal static void ParseComponentEncodings(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = OrchestraCommonParser.FindDirectChild(root, "components", root.NamespaceURI);
        if (container == null) return;
        int definitionIndex = 0;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (!OrchestraVersion10Parser.IsNativeElement(element, "component", root.NamespaceURI)) continue;
            if (element != null && definitionIndex < repository.Components.Count)
            {
                OrchestraComponentDefinition definition = repository.Components[definitionIndex];
                ParseMemberEncodings(element, definition.Members, root.NamespaceURI);
                ParseEncodings(element, definition.Encodings, root.NamespaceURI);
            }
            definitionIndex++;
        }
    }

    internal static void ParseGroupEncodings(XmlElement root, OrchestraRepository repository)
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
                if (countElement != null && definition.CountMember != null) ParseEncodings(countElement, definition.CountMember.Encodings, root.NamespaceURI);
                ParseMemberEncodings(element, definition.Members, root.NamespaceURI);
                ParseEncodings(element, definition.Encodings, root.NamespaceURI);
            }
            definitionIndex++;
        }
    }

    internal static void ParseMessageEncodings(XmlElement root, OrchestraRepository repository)
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
                if (structure != null) ParseMemberEncodings(structure, definition.Members, root.NamespaceURI);
                ParseEncodings(element, definition.Encodings, root.NamespaceURI);
            }
            definitionIndex++;
        }
    }

    internal static void ParseMemberEncodings(XmlElement parent, List<OrchestraMemberDefinition> members, string namespaceUri)
    {
        int memberIndex = 0;
        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlElement? element = parent.ChildNodes[index] as XmlElement;
            if (element != null && element.NamespaceURI == namespaceUri && OrchestraVersion10Parser.IsMemberElement(element.LocalName))
            {
                if (memberIndex < members.Count) ParseEncodings(element, members[memberIndex].Encodings, namespaceUri);
                memberIndex++;
            }
        }
    }
}
