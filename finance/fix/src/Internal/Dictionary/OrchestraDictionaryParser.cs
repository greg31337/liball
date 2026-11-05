using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace fix;

internal static class OrchestraDictionaryParser
{
    internal const string Version10Namespace = "http://fixprotocol.io/2020/orchestra/repository";
    internal const string Version11Namespace = "http://fixprotocol.io/2026/orchestra/repository";

    internal static DictionaryLoadResult ParseRepositoryDictionary(Stream source, FixDictionaryOptions options)
    {
        DictionaryLoadResult result = new DictionaryLoadResult();
        FixDictionary dictionary = new FixDictionary();
        dictionary.Options = options;
        result.Dictionary = dictionary;
        if (!source.CanRead)
        {
            AddParserDiagnostic(dictionary, ValidationSeverity.Error, "unreadable-source", "The Orchestra repository source is not readable.", "", false, 0);
            CompleteParseResult(result, dictionary, false);
            return result;
        }

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        settings.IgnoreComments = true;
        settings.IgnoreWhitespace = true;
        settings.CloseInput = false;
        DictionaryXmlLoadResult loaded = DictionaryXmlLoader.LoadDocument(source, settings);
        if (loaded.Error != null || loaded.Document == null)
        {
            string message = loaded.Error == null ? "The Orchestra XML document could not be loaded." : loaded.Error.Message;
            AddParserDiagnostic(dictionary, ValidationSeverity.Error, "invalid-orchestra-xml", message, "", false, 0);
            DictionaryDiagnostic diagnostic = dictionary.Diagnostics[dictionary.Diagnostics.Count - 1];
            diagnostic.Line = loaded.Line;
            diagnostic.Column = loaded.Column;
            CompleteParseResult(result, dictionary, false);
            return result;
        }
        XmlDocument document = loaded.Document;

        XmlElement? possibleRoot = document.DocumentElement;
        if (possibleRoot == null || possibleRoot.LocalName != "repository")
        {
            AddParserDiagnostic(dictionary, ValidationSeverity.Error, "invalid-orchestra-root", "The document root must be an Orchestra repository element.", "", false, 0);
            CompleteParseResult(result, dictionary, false);
            return result;
        }
        XmlElement root = possibleRoot;
        if (!IsSupportedNamespace(root.NamespaceURI))
        {
            AddParserDiagnostic(dictionary, ValidationSeverity.Error, "unsupported-orchestra-namespace", "The repository namespace is not a supported Orchestra 1.0 or 1.1 namespace.", root.NamespaceURI, false, 0);
            CompleteParseResult(result, dictionary, false);
            return result;
        }

        OrchestraDictionaryParseContext context = new OrchestraDictionaryParseContext(dictionary, options);
        context.NamespaceName = root.NamespaceURI;
        ParseIdentity(root, context);
        ParseScenarios(root, context);
        ParseDatatypes(root, context);
        ResolveDatatypeDefinitions(context);
        ParseCodeSets(root, context);
        ParseFields(root, context);
        RegisterComponents(root, context);
        RegisterGroups(root, context);
        ParseComponentMembers(root, context);
        ParseGroupMembers(root, context);
        ExtractEnvelopeComponents(context);
        ParseMessages(root, context);
        if (context.HasDeferredScenarioDefinitions)
        {
            AddParserDiagnostic(dictionary, ValidationSeverity.Warning, "orchestra-scenario-definitions-deferred", "Scenario-specialized field, code-set, component, or group definitions were deferred; base definitions remain authoritative in the canonical dictionary.", dictionary.Identity.Name, false, 0);
        }
        if (ContainsDeferredBehavior(root))
        {
            AddParserDiagnostic(dictionary, ValidationSeverity.Warning, "orchestra-behavior-deferred", "Orchestra behavioral rules were retained as raw member conditions where possible and deferred for Orchestra rule processing.", dictionary.Identity.Name, false, 0);
        }

        DictionaryNormalizer.NormalizeDictionary(dictionary, options);
        DowngradeStructuralAmbiguities(dictionary);
        CompleteParseResult(result, dictionary, !HasParserErrors(dictionary.Diagnostics));
        return result;
    }

    internal static bool IsSupportedNamespace(string namespaceName)
    {
        return namespaceName == Version10Namespace || namespaceName == Version11Namespace;
    }

    internal static void CompleteParseResult(DictionaryLoadResult result, FixDictionary dictionary, bool success)
    {
        result.Success = success;
        result.Diagnostics = dictionary.Diagnostics;
    }

    internal static void ParseIdentity(XmlElement root, OrchestraDictionaryParseContext context)
    {
        string name = root.GetAttribute("name").Trim();
        string version = root.GetAttribute("version").Trim();
        context.Dictionary.Identity.Name = name.Length == 0 ? version : name;
        context.Dictionary.Identity.Scenario = root.GetAttribute("scenario").Trim();
        string applicationVersion = version;
        int marker = version.LastIndexOf("_EP", StringComparison.OrdinalIgnoreCase);
        if (marker >= 0)
        {
            applicationVersion = version.Substring(0, marker);
            int extensionPack = ParseNonnegativeInteger(version.Substring(marker + 3), int.MaxValue);
            if (extensionPack >= 0)
            {
                context.Dictionary.Identity.HasExtensionPack = true;
                context.Dictionary.Identity.ExtensionPack = extensionPack;
            }
            else
            {
                AddParserDiagnostic(context.Dictionary, ValidationSeverity.Error, "invalid-extension-pack", "The Orchestra repository version has an invalid extension-pack suffix.", version, false, 0);
            }
        }
        context.Dictionary.Identity.ApplicationVersion = applicationVersion;
        if (IsClassicBeginString(applicationVersion) || applicationVersion.StartsWith("FIXT.", StringComparison.OrdinalIgnoreCase))
        {
            context.Dictionary.Identity.BeginString = applicationVersion;
        }
        if (context.Dictionary.Identity.Name.Length == 0)
        {
            AddParserDiagnostic(context.Dictionary, ValidationSeverity.Error, "missing-repository-name", "The Orchestra repository has no name or version.", "", false, 0);
        }
    }

    internal static bool IsClassicBeginString(string version)
    {
        return version == "FIX.2.7" || version == "FIX.3.0" || version == FixBeginStrings.Fix40 || version == FixBeginStrings.Fix41 || version == FixBeginStrings.Fix42 || version == FixBeginStrings.Fix43 || version == FixBeginStrings.Fix44;
    }

    internal static void ParseScenarios(XmlElement root, OrchestraDictionaryParseContext context)
    {
        XmlElement? section = FindDirectChild(root, "scenarios", context.NamespaceName);
        if (section == null)
        {
            return;
        }
        foreach (XmlNode node in section.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)node;
            if (element.LocalName != "scenario" || element.NamespaceURI != context.NamespaceName)
            {
                continue;
            }
            string name = element.GetAttribute("name").Trim();
            if (name.Length == 0)
            {
                name = "base";
            }
            FixScenarioDefinition scenario = new FixScenarioDefinition();
            scenario.Name = name;
            scenario.ParentName = element.GetAttribute("scenarioRef").Trim();
            if (scenario.ParentName.Length == 0)
            {
                scenario.ParentName = element.GetAttribute("parent").Trim();
            }
            scenario.Description = GetDescription(element, context.NamespaceName);
            context.Dictionary.Scenarios.Add(scenario);
            string key = CreateNameKey(name, context.Options.NormalizeNames);
            if (!context.ScenariosByName.ContainsKey(key))
            {
                context.ScenariosByName.Add(key, scenario);
            }
        }
    }

    internal static void ParseDatatypes(XmlElement root, OrchestraDictionaryParseContext context)
    {
        XmlElement? section = FindDirectChild(root, "datatypes", context.NamespaceName);
        if (section == null)
        {
            AddParserDiagnostic(context.Dictionary, ValidationSeverity.Warning, "missing-datatypes-section", "The Orchestra repository has no datatypes section.", context.Dictionary.Identity.Name, false, 0);
            return;
        }
        foreach (XmlNode node in section.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)node;
            if (element.LocalName != "datatype" || element.NamespaceURI != context.NamespaceName)
            {
                continue;
            }
            string name = element.GetAttribute("name").Trim();
            if (name.Length == 0)
            {
                AddParserDiagnostic(context.Dictionary, ValidationSeverity.Error, "missing-datatype-name", "An Orchestra datatype has no name.", "", false, 0);
                continue;
            }
            FixDatatypeDefinition datatype = new FixDatatypeDefinition();
            datatype.Name = name;
            datatype.BaseTypeName = element.GetAttribute("baseType").Trim();
            datatype.FieldType = ResolveOrchestraBuiltinType(name);
            datatype.Description = GetDescription(element, context.NamespaceName);
            context.Dictionary.Datatypes.Add(datatype);
            string key = CreateNameKey(name, context.Options.NormalizeNames);
            if (!context.DatatypesByName.ContainsKey(key))
            {
                context.DatatypesByName.Add(key, datatype);
            }
        }
    }

    internal static void ResolveDatatypeDefinitions(OrchestraDictionaryParseContext context)
    {
        for (int index = 0; index < context.Dictionary.Datatypes.Count; index++)
        {
            FixDatatypeDefinition datatype = context.Dictionary.Datatypes[index];
            if (datatype.FieldType == FixFieldType.Unknown)
            {
                datatype.FieldType = ResolveDatatypeName(datatype.Name, context, context.Dictionary.Datatypes.Count);
            }
        }
    }

    internal static void ParseCodeSets(XmlElement root, OrchestraDictionaryParseContext context)
    {
        XmlElement? section = FindDirectChild(root, "codeSets", context.NamespaceName);
        if (section == null)
        {
            return;
        }
        foreach (XmlNode node in section.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)node;
            if (element.LocalName != "codeSet" || element.NamespaceURI != context.NamespaceName)
            {
                continue;
            }
            if (IsDeferredScenarioDefinition(element, context))
            {
                continue;
            }
            int id = ParseDefinitionId(element, "code set", context, false);
            string name = element.GetAttribute("name").Trim();
            if (id < 0 || name.Length == 0)
            {
                if (name.Length == 0)
                {
                    AddParserDiagnostic(context.Dictionary, ValidationSeverity.Error, "missing-code-set-name", "An Orchestra code set has no name.", "", false, 0);
                }
                continue;
            }
            FixCodeSetDefinition codeSet = new FixCodeSetDefinition();
            codeSet.Id = id;
            codeSet.Name = name;
            codeSet.FieldType = ResolveDatatypeName(element.GetAttribute("type").Trim(), context, context.Dictionary.Datatypes.Count);
            codeSet.Description = GetDescription(element, context.NamespaceName);
            ParseCodes(element, codeSet, context);
            context.Dictionary.CodeSets.Add(codeSet);
            string key = CreateNameKey(name, context.Options.NormalizeNames);
            if (!context.CodeSetsByName.ContainsKey(key))
            {
                context.CodeSetsByName.Add(key, codeSet);
            }
        }
    }

    internal static void ParseCodes(XmlElement codeSetElement, FixCodeSetDefinition codeSet, OrchestraDictionaryParseContext context)
    {
        int fallbackSort = 0;
        foreach (XmlNode node in codeSetElement.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)node;
            if (element.LocalName != "code" || element.NamespaceURI != context.NamespaceName)
            {
                continue;
            }
            if (IsDeferredScenarioDefinition(element, context))
            {
                continue;
            }
            string value = element.GetAttribute("value");
            if (value.Length == 0)
            {
                AddParserDiagnostic(context.Dictionary, ValidationSeverity.Error, "missing-code-value", "An Orchestra code has no lexical value.", codeSet.Name, false, 0);
                continue;
            }
            FixCodeDefinition code = new FixCodeDefinition();
            code.Id = ParseOptionalInteger(element.GetAttribute("id"), int.MaxValue, 0);
            code.Name = element.GetAttribute("name").Trim();
            if (code.Name.Length == 0)
            {
                code.Name = value;
            }
            code.Value = value;
            code.SortOrder = ParseOptionalInteger(element.GetAttribute("sort"), int.MaxValue, fallbackSort);
            code.Description = GetDescription(element, context.NamespaceName);
            codeSet.Codes.Add(code);
            fallbackSort++;
        }
    }

    internal static void ParseFields(XmlElement root, OrchestraDictionaryParseContext context)
    {
        XmlElement? section = FindDirectChild(root, "fields", context.NamespaceName);
        if (section == null)
        {
            AddParserDiagnostic(context.Dictionary, ValidationSeverity.Error, "missing-fields-section", "The Orchestra repository has no fields section.", context.Dictionary.Identity.Name, false, 0);
            return;
        }
        foreach (XmlNode node in section.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)node;
            if (element.LocalName != "field" || element.NamespaceURI != context.NamespaceName)
            {
                continue;
            }
            if (IsDeferredScenarioDefinition(element, context))
            {
                continue;
            }
            int id = ParseDefinitionId(element, "field", context, true);
            string name = element.GetAttribute("name").Trim();
            if (id <= 0 || name.Length == 0)
            {
                if (name.Length == 0)
                {
                    AddParserDiagnostic(context.Dictionary, ValidationSeverity.Error, "missing-field-name", "An Orchestra field has no name.", "", id > 0, id > 0 ? id : 0);
                }
                continue;
            }
            FixFieldDefinition field = new FixFieldDefinition();
            field.Tag = id;
            field.Name = name;
            field.Abbreviation = element.GetAttribute("abbrName").Trim();
            string datatypeName = element.GetAttribute("type").Trim();
            string codeSetName = element.GetAttribute("codeSet").Trim();
            if (codeSetName.Length == 0)
            {
                string typeKey = CreateNameKey(datatypeName, context.Options.NormalizeNames);
                if (context.CodeSetsByName.ContainsKey(typeKey))
                {
                    codeSetName = context.CodeSetsByName[typeKey].Name;
                    datatypeName = ResolveCodeSetDatatypeName(context.CodeSetsByName[typeKey], element.GetAttribute("type").Trim());
                }
            }
            else
            {
                string codeSetKey = CreateNameKey(codeSetName, context.Options.NormalizeNames);
                if (context.CodeSetsByName.ContainsKey(codeSetKey))
                {
                    datatypeName = ResolveCodeSetDatatypeName(context.CodeSetsByName[codeSetKey], datatypeName);
                }
            }
            field.DatatypeName = datatypeName;
            field.CodeSetName = codeSetName;
            field.FieldType = ResolveDatatypeName(datatypeName, context, context.Dictionary.Datatypes.Count);
            int discriminatorId = ParseOptionalInteger(element.GetAttribute("discriminatorId"), FixDefaults.MaxTagNumber, 0);
            if (discriminatorId > 0)
            {
                field.HasDiscriminatorTag = true;
                field.DiscriminatorTag = discriminatorId;
            }
            int lengthId = ParseOptionalInteger(element.GetAttribute("lengthId"), FixDefaults.MaxTagNumber, 0);
            if (lengthId > 0)
            {
                field.HasLengthTag = true;
                field.LengthTag = lengthId;
            }
            field.Description = GetDescription(element, context.NamespaceName);
            context.Dictionary.Fields.Add(field);
            if (!context.FieldsById.ContainsKey(id))
            {
                context.FieldsById.Add(id, field);
            }
        }
    }

    internal static string ResolveCodeSetDatatypeName(FixCodeSetDefinition codeSet, string fallback)
    {
        string resolved = ResolveCanonicalTypeName(codeSet.FieldType);
        if (resolved.Length != 0)
        {
            return resolved;
        }
        return fallback;
    }

    internal static void RegisterComponents(XmlElement root, OrchestraDictionaryParseContext context)
    {
        XmlElement? section = FindDirectChild(root, "components", context.NamespaceName);
        if (section == null)
        {
            return;
        }
        foreach (XmlNode node in section.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)node;
            if (element.LocalName != "component" || element.NamespaceURI != context.NamespaceName)
            {
                continue;
            }
            if (IsDeferredScenarioDefinition(element, context))
            {
                continue;
            }
            int id = ParseDefinitionId(element, "component", context, false);
            string name = element.GetAttribute("name").Trim();
            if (id < 0 || name.Length == 0)
            {
                if (name.Length == 0)
                {
                    AddParserDiagnostic(context.Dictionary, ValidationSeverity.Error, "missing-component-name", "An Orchestra component has no name.", "", false, 0);
                }
                continue;
            }
            FixComponentDefinition component = new FixComponentDefinition();
            component.Id = id;
            component.Name = name;
            component.Abbreviation = element.GetAttribute("abbrName").Trim();
            component.Description = GetDescription(element, context.NamespaceName);
            context.Dictionary.Components.Add(component);
            if (!context.ComponentsById.ContainsKey(id))
            {
                context.ComponentsById.Add(id, component);
            }
        }
    }

    internal static void RegisterGroups(XmlElement root, OrchestraDictionaryParseContext context)
    {
        XmlElement? section = FindDirectChild(root, "groups", context.NamespaceName);
        if (section == null)
        {
            return;
        }
        foreach (XmlNode node in section.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)node;
            if (element.LocalName != "group" || element.NamespaceURI != context.NamespaceName)
            {
                continue;
            }
            if (IsDeferredScenarioDefinition(element, context))
            {
                continue;
            }
            int id = ParseDefinitionId(element, "group", context, false);
            string name = element.GetAttribute("name").Trim();
            if (id < 0 || name.Length == 0)
            {
                if (name.Length == 0)
                {
                    AddParserDiagnostic(context.Dictionary, ValidationSeverity.Error, "missing-group-name", "An Orchestra group has no name.", "", false, 0);
                }
                continue;
            }
            FixGroupDefinition group = new FixGroupDefinition();
            group.Id = id;
            group.Name = name;
            group.Description = GetDescription(element, context.NamespaceName);
            XmlElement? count = FindDirectChild(element, "numInGroup", context.NamespaceName);
            if (count != null)
            {
                group.CountTag = ParseReferenceId(count, "group count", context);
            }
            else
            {
                AddParserDiagnostic(context.Dictionary, ValidationSeverity.Error, "missing-group-count-field", "An Orchestra group has no numInGroup reference.", name, false, 0);
            }
            context.Dictionary.Groups.Add(group);
            if (!context.GroupsById.ContainsKey(id))
            {
                context.GroupsById.Add(id, group);
            }
        }
    }

    internal static void ParseComponentMembers(XmlElement root, OrchestraDictionaryParseContext context)
    {
        XmlElement? section = FindDirectChild(root, "components", context.NamespaceName);
        if (section == null)
        {
            return;
        }
        foreach (XmlNode node in section.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)node;
            if (element.LocalName != "component" || element.NamespaceURI != context.NamespaceName)
            {
                continue;
            }
            if (IsDeferredScenarioDefinition(element, context))
            {
                continue;
            }
            int id = ParseOptionalInteger(element.GetAttribute("id"), int.MaxValue, -1);
            if (context.ComponentsById.ContainsKey(id))
            {
                ParseMemberElements(element, context.ComponentsById[id].Members, context, false);
            }
        }
    }

    internal static void ParseGroupMembers(XmlElement root, OrchestraDictionaryParseContext context)
    {
        XmlElement? section = FindDirectChild(root, "groups", context.NamespaceName);
        if (section == null)
        {
            return;
        }
        foreach (XmlNode node in section.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)node;
            if (element.LocalName != "group" || element.NamespaceURI != context.NamespaceName)
            {
                continue;
            }
            if (IsDeferredScenarioDefinition(element, context))
            {
                continue;
            }
            int id = ParseOptionalInteger(element.GetAttribute("id"), int.MaxValue, -1);
            if (context.GroupsById.ContainsKey(id))
            {
                FixGroupDefinition group = context.GroupsById[id];
                ParseMemberElements(element, group.Members, context, false);
                group.DelimiterTag = FindFirstFieldTag(group.Members, context, 0);
                if (group.DelimiterTag == 0)
                {
                    AddParserDiagnostic(context.Dictionary, ValidationSeverity.Error, "missing-group-delimiter", "An Orchestra group has no field delimiter member.", group.Name, true, group.CountTag);
                }
            }
        }
    }

    internal static void ExtractEnvelopeComponents(OrchestraDictionaryParseContext context)
    {
        int index = 0;
        while (index < context.Dictionary.Components.Count)
        {
            FixComponentDefinition component = context.Dictionary.Components[index];
            if (string.Equals(component.Name, "StandardHeader", StringComparison.OrdinalIgnoreCase))
            {
                context.Dictionary.Header = component.Members;
                context.EnvelopeComponentIds.Add(component.Id);
                context.Dictionary.Components.RemoveAt(index);
            }
            else if (string.Equals(component.Name, "StandardTrailer", StringComparison.OrdinalIgnoreCase))
            {
                context.Dictionary.Trailer = component.Members;
                context.EnvelopeComponentIds.Add(component.Id);
                context.Dictionary.Components.RemoveAt(index);
            }
            else
            {
                index++;
            }
        }
    }

    internal static void ParseMessages(XmlElement root, OrchestraDictionaryParseContext context)
    {
        XmlElement? section = FindDirectChild(root, "messages", context.NamespaceName);
        if (section == null)
        {
            return;
        }
        foreach (XmlNode node in section.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)node;
            if (element.LocalName != "message" || element.NamespaceURI != context.NamespaceName)
            {
                continue;
            }
            int id = ParseDefinitionId(element, "message", context, false);
            string name = element.GetAttribute("name").Trim();
            string msgType = element.GetAttribute("msgType");
            if (id < 0 || name.Length == 0 || msgType.Length == 0)
            {
                AddParserDiagnostic(context.Dictionary, ValidationSeverity.Error, "invalid-message-definition", "An Orchestra message must have an id, name, and MsgType.", name, false, 0);
                continue;
            }
            FixMessageDefinition message = new FixMessageDefinition();
            message.Id = id;
            message.Name = name;
            message.MsgType = msgType;
            message.Category = element.GetAttribute("category").Trim();
            message.Scenario = element.GetAttribute("scenario").Trim();
            if (message.Scenario.Length == 0)
            {
                message.Scenario = "base";
            }
            message.Description = GetDescription(element, context.NamespaceName);
            XmlElement? structure = FindDirectChild(element, "structure", context.NamespaceName);
            if (structure != null)
            {
                ParseMemberElements(structure, message.Members, context, true);
            }
            context.Dictionary.Messages.Add(message);
            string parent = element.GetAttribute("scenarioRef").Trim();
            if (parent.Length != 0)
            {
                EnsureScenarioParent(message.Scenario, parent, context);
            }
        }
    }

    internal static void EnsureScenarioParent(string name, string parent, OrchestraDictionaryParseContext context)
    {
        string key = CreateNameKey(name, context.Options.NormalizeNames);
        if (context.ScenariosByName.ContainsKey(key))
        {
            if (context.ScenariosByName[key].ParentName.Length == 0)
            {
                context.ScenariosByName[key].ParentName = parent;
            }
            return;
        }
        FixScenarioDefinition scenario = new FixScenarioDefinition();
        scenario.Name = name;
        scenario.ParentName = parent;
        context.Dictionary.Scenarios.Add(scenario);
        context.ScenariosByName.Add(key, scenario);
    }

    internal static void ParseMemberElements(XmlElement owner, List<FixMemberDefinition> destination, OrchestraDictionaryParseContext context, bool skipEnvelope)
    {
        foreach (XmlNode node in owner.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)node;
            if (element.NamespaceURI != context.NamespaceName)
            {
                continue;
            }
            FixMemberDefinition? member = null;
            if (element.LocalName == "fieldRef")
            {
                member = CreateFieldMember(element, context);
            }
            else if (element.LocalName == "componentRef")
            {
                int componentId = ParseOptionalInteger(element.GetAttribute("id"), int.MaxValue, -1);
                if (!skipEnvelope || !context.EnvelopeComponentIds.Contains(componentId))
                {
                    member = CreateComponentMember(element, context);
                }
            }
            else if (element.LocalName == "groupRef")
            {
                member = CreateGroupMember(element, context);
            }
            if (member != null)
            {
                member.Position = destination.Count;
                destination.Add(member);
            }
        }
    }

    internal static FixMemberDefinition? CreateFieldMember(XmlElement element, OrchestraDictionaryParseContext context)
    {
        int id = ParseReferenceId(element, "field", context);
        if (id <= 0)
        {
            return null;
        }
        FixMemberDefinition member = new FixMemberDefinition();
        member.Kind = FixMemberKind.Field;
        member.ReferenceId = id;
        member.Name = ResolveMemberName(element, id, context.FieldsById.ContainsKey(id) ? context.FieldsById[id].Name : "");
        PopulateMemberBehavior(element, member, context);
        return member;
    }

    internal static FixMemberDefinition? CreateComponentMember(XmlElement element, OrchestraDictionaryParseContext context)
    {
        int id = ParseReferenceId(element, "component", context);
        if (id <= 0)
        {
            return null;
        }
        FixMemberDefinition member = new FixMemberDefinition();
        member.Kind = FixMemberKind.Component;
        member.ReferenceId = id;
        member.Name = ResolveMemberName(element, id, context.ComponentsById.ContainsKey(id) ? context.ComponentsById[id].Name : "");
        PopulateMemberBehavior(element, member, context);
        return member;
    }

    internal static FixMemberDefinition? CreateGroupMember(XmlElement element, OrchestraDictionaryParseContext context)
    {
        int id = ParseReferenceId(element, "group", context);
        if (id <= 0)
        {
            return null;
        }
        FixMemberDefinition member = new FixMemberDefinition();
        member.Kind = FixMemberKind.Group;
        member.ReferenceId = id;
        member.Name = ResolveMemberName(element, id, context.GroupsById.ContainsKey(id) ? context.GroupsById[id].Name : "");
        PopulateMemberBehavior(element, member, context);
        return member;
    }

    internal static string ResolveMemberName(XmlElement element, int id, string definitionName)
    {
        string instanceName = element.GetAttribute("instanceName").Trim();
        if (instanceName.Length != 0)
        {
            return instanceName;
        }
        string name = element.GetAttribute("name").Trim();
        if (name.Length != 0)
        {
            return name;
        }
        if (definitionName.Length != 0)
        {
            return definitionName;
        }
        return id.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static void PopulateMemberBehavior(XmlElement element, FixMemberDefinition member, OrchestraDictionaryParseContext context)
    {
        member.Presence = ParsePresence(element.GetAttribute("presence"), element, context);
        if (element.HasAttribute("scenario"))
        {
            member.Scenario = element.GetAttribute("scenario").Trim();
        }
        member.PresenceRule = FindFirstRuleExpression(element, context.NamespaceName);
        if (member.PresenceRule.Length != 0 && member.Presence == FixPresence.Optional)
        {
            member.Presence = FixPresence.Conditional;
        }
        if (member.PresenceRule.Length != 0)
        {
            member.RulePresence = FindFirstRulePresence(element, context.NamespaceName, context);
        }
    }

    internal static FixPresence ParsePresence(string value, XmlElement owner, OrchestraDictionaryParseContext context)
    {
        string normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length == 0 || normalized == "optional") return FixPresence.Optional;
        if (normalized == "required") return FixPresence.Required;
        if (normalized == "forbidden") return FixPresence.Forbidden;
        if (normalized == "ignored") return FixPresence.Ignored;
        if (normalized == "constant") return FixPresence.Constant;
        AddParserDiagnostic(context.Dictionary, ValidationSeverity.Warning, "unknown-member-presence", "An Orchestra member presence value was treated as optional.", owner.LocalName, false, 0);
        return FixPresence.Optional;
    }

    internal static string FindFirstRuleExpression(XmlElement element, string namespaceName)
    {
        foreach (XmlNode node in element.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement child = (XmlElement)node;
            if (child.NamespaceURI == namespaceName && child.LocalName == "rule")
            {
                XmlElement? when = FindDirectChild(child, "when", namespaceName);
                if (when != null)
                {
                    return NormalizeText(when.InnerText);
                }
            }
        }
        return "";
    }

    internal static FixPresence FindFirstRulePresence(XmlElement element, string namespaceName, OrchestraDictionaryParseContext context)
    {
        foreach (XmlNode node in element.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement child = (XmlElement)node;
            if (child.NamespaceURI == namespaceName && child.LocalName == "rule")
            {
                return ParsePresence(child.GetAttribute("presence"), child, context);
            }
        }
        return FixPresence.Optional;
    }

    internal static int FindFirstFieldTag(List<FixMemberDefinition> members, OrchestraDictionaryParseContext context, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            return 0;
        }
        for (int index = 0; index < members.Count; index++)
        {
            FixMemberDefinition member = members[index];
            if (member.Kind == FixMemberKind.Field)
            {
                return member.ReferenceId;
            }
            if (member.Kind == FixMemberKind.Component && context.ComponentsById.ContainsKey(member.ReferenceId))
            {
                int fieldTag = FindFirstFieldTag(context.ComponentsById[member.ReferenceId].Members, context, depth + 1);
                if (fieldTag > 0)
                {
                    return fieldTag;
                }
            }
            if (member.Kind == FixMemberKind.Group && context.GroupsById.ContainsKey(member.ReferenceId))
            {
                return context.GroupsById[member.ReferenceId].CountTag;
            }
        }
        return 0;
    }

    internal static void DowngradeStructuralAmbiguities(FixDictionary dictionary)
    {
        for (int index = 0; index < dictionary.Diagnostics.Count; index++)
        {
            DictionaryDiagnostic diagnostic = dictionary.Diagnostics[index];
            bool downgrade = diagnostic.Code == "duplicate-code-name" || diagnostic.Code == "duplicate-group-count-tag";
            if (diagnostic.Code == "duplicate-field-name" && !HasRepeatedFieldName(dictionary, diagnostic.DefinitionName))
            {
                downgrade = true;
            }
            if (diagnostic.Code == "duplicate-component-name" && !HasRepeatedComponentName(dictionary, diagnostic.DefinitionName))
            {
                downgrade = true;
            }
            if (diagnostic.Code == "duplicate-message-id" && HasMessageIdAcrossScenarios(dictionary, diagnostic.DefinitionName))
            {
                downgrade = true;
            }
            if (downgrade)
            {
                diagnostic.Severity = ValidationSeverity.Warning;
            }
        }
    }

    internal static bool HasRepeatedFieldName(FixDictionary dictionary, string name)
    {
        string key = CreateNameKey(name, dictionary.Options.NormalizeNames);
        int count = 0;
        for (int index = 0; index < dictionary.Fields.Count; index++)
        {
            if (CreateNameKey(dictionary.Fields[index].Name, dictionary.Options.NormalizeNames) == key)
            {
                count++;
            }
        }
        return count > 1;
    }

    internal static bool HasRepeatedComponentName(FixDictionary dictionary, string name)
    {
        string key = CreateNameKey(name, dictionary.Options.NormalizeNames);
        int count = 0;
        for (int index = 0; index < dictionary.Components.Count; index++)
        {
            if (CreateNameKey(dictionary.Components[index].Name, dictionary.Options.NormalizeNames) == key)
            {
                count++;
            }
        }
        return count > 1;
    }

    internal static bool HasMessageIdAcrossScenarios(FixDictionary dictionary, string name)
    {
        string nameKey = CreateNameKey(name, dictionary.Options.NormalizeNames);
        for (int messageIndex = 0; messageIndex < dictionary.Messages.Count; messageIndex++)
        {
            FixMessageDefinition message = dictionary.Messages[messageIndex];
            if (CreateNameKey(message.Name, dictionary.Options.NormalizeNames) != nameKey)
            {
                continue;
            }
            string scenarioKey = CreateNameKey(message.Scenario, dictionary.Options.NormalizeNames);
            for (int otherIndex = 0; otherIndex < dictionary.Messages.Count; otherIndex++)
            {
                FixMessageDefinition other = dictionary.Messages[otherIndex];
                if (other.Id == message.Id && CreateNameKey(other.Scenario, dictionary.Options.NormalizeNames) != scenarioKey)
                {
                    return true;
                }
            }
        }
        return false;
    }

    internal static bool IsDeferredScenarioDefinition(XmlElement element, OrchestraDictionaryParseContext context)
    {
        if (!element.HasAttribute("scenario"))
        {
            return false;
        }
        string scenario = element.GetAttribute("scenario").Trim();
        if (scenario.Length == 0 || string.Equals(scenario, "base", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        context.HasDeferredScenarioDefinitions = true;
        return true;
    }

    internal static bool ContainsDeferredBehavior(XmlElement root)
    {
        List<XmlElement> pending = [root];
        int index = 0;
        while (index < pending.Count)
        {
            XmlElement current = pending[index];
            index++;
            if (current.LocalName == "rule" || current.LocalName == "assign" || current.LocalName == "when" || current.LocalName == "responses" || current.LocalName == "actors" || current.LocalName == "flows")
            {
                return true;
            }
            foreach (XmlNode node in current.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    pending.Add((XmlElement)node);
                }
            }
        }
        return false;
    }

    internal static string GetDescription(XmlElement owner, string namespaceName)
    {
        XmlElement? annotation = FindDirectChild(owner, "annotation", namespaceName);
        if (annotation == null)
        {
            return "";
        }
        string fallback = "";
        foreach (XmlNode node in annotation.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)node;
            if (element.LocalName != "documentation" || element.NamespaceURI != namespaceName)
            {
                continue;
            }
            string text = NormalizeText(element.InnerText);
            if (text.Length == 0)
            {
                continue;
            }
            if (fallback.Length == 0)
            {
                fallback = text;
            }
            if (element.GetAttribute("purpose").Trim().ToUpperInvariant() == "SYNOPSIS")
            {
                return text;
            }
        }
        return fallback;
    }

    internal static string NormalizeText(string value)
    {
        StringBuilder builder = new StringBuilder();
        bool whitespace = false;
        for (int index = 0; index < value.Length; index++)
        {
            char current = value[index];
            if (char.IsWhiteSpace(current))
            {
                whitespace = builder.Length != 0;
            }
            else
            {
                if (whitespace)
                {
                    builder.Append(' ');
                    whitespace = false;
                }
                builder.Append(current);
            }
        }
        return builder.ToString();
    }

    internal static XmlElement? FindDirectChild(XmlElement parent, string localName, string namespaceName)
    {
        foreach (XmlNode node in parent.ChildNodes)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                XmlElement element = (XmlElement)node;
                if (element.LocalName == localName && element.NamespaceURI == namespaceName)
                {
                    return element;
                }
            }
        }
        return null;
    }

    internal static int ParseDefinitionId(XmlElement element, string kind, OrchestraDictionaryParseContext context, bool fieldTag)
    {
        int maximum = fieldTag ? FixDefaults.MaxTagNumber : int.MaxValue;
        int value = ParseNonnegativeInteger(element.GetAttribute("id"), maximum);
        if (value <= 0)
        {
            AddParserDiagnostic(context.Dictionary, ValidationSeverity.Error, "invalid-orchestra-id", "An Orchestra " + kind + " has a missing or invalid identifier.", element.GetAttribute("name").Trim(), fieldTag && value > 0, value > 0 ? value : 0);
            return -1;
        }
        return value;
    }

    internal static int ParseReferenceId(XmlElement element, string kind, OrchestraDictionaryParseContext context)
    {
        int value = ParseNonnegativeInteger(element.GetAttribute("id"), int.MaxValue);
        if (value <= 0)
        {
            AddParserDiagnostic(context.Dictionary, ValidationSeverity.Error, "invalid-orchestra-reference", "An Orchestra " + kind + " reference has a missing or invalid identifier.", element.GetAttribute("name").Trim(), false, 0);
            return -1;
        }
        return value;
    }

    internal static int ParseOptionalInteger(string value, int maximum, int fallback)
    {
        if (value.Length == 0)
        {
            return fallback;
        }
        int parsed = ParseNonnegativeInteger(value, maximum);
        return parsed < 0 ? fallback : parsed;
    }

    internal static int ParseNonnegativeInteger(string value, int maximum)
    {
        if (value.Length == 0)
        {
            return -1;
        }
        AsciiIntegerResult parsed = AsciiDigits.ParseUnsignedInt(Encoding.ASCII.GetBytes(value.Trim()), maximum);
        return parsed.Success ? parsed.Value : -1;
    }

    internal static string CreateNameKey(string value, bool normalizeNames)
    {
        return DictionaryNormalizer.CreateNameKey(value, normalizeNames);
    }

    internal static FixFieldType ResolveDatatypeName(string name, OrchestraDictionaryParseContext context, int maximumDepth)
    {
        FixFieldType builtin = ResolveOrchestraBuiltinType(name);
        if (builtin != FixFieldType.Unknown)
        {
            return builtin;
        }
        string key = CreateNameKey(name, context.Options.NormalizeNames);
        int depth = 0;
        while (key.Length != 0 && context.DatatypesByName.ContainsKey(key) && depth <= maximumDepth)
        {
            FixDatatypeDefinition datatype = context.DatatypesByName[key];
            if (datatype.FieldType != FixFieldType.Unknown)
            {
                return datatype.FieldType;
            }
            builtin = ResolveOrchestraBuiltinType(datatype.BaseTypeName);
            if (builtin != FixFieldType.Unknown)
            {
                return builtin;
            }
            key = CreateNameKey(datatype.BaseTypeName, context.Options.NormalizeNames);
            depth++;
        }
        return FixFieldType.Unknown;
    }

    internal static FixFieldType ResolveOrchestraBuiltinType(string name)
    {
        FixFieldType fieldType = DictionaryNormalizer.ResolveBuiltinFieldType(name);
        if (fieldType != FixFieldType.Unknown)
        {
            return fieldType;
        }
        string key = name.Trim().ToUpperInvariant();
        if (key == "PATTERN" || key == "TENOR" || key == "RESERVED100PLUS" || key == "RESERVED1000PLUS" || key == "RESERVED4000PLUS" || key == "LANGUAGE" || key == "XID" || key == "XIDREF") return FixFieldType.String;
        if (key == "LOCALMKTTIME") return FixFieldType.UtcTimeOnly;
        return FixFieldType.Unknown;
    }

    internal static string ResolveCanonicalTypeName(FixFieldType fieldType)
    {
        if (fieldType == FixFieldType.Int) return "int";
        if (fieldType == FixFieldType.SeqNum) return "SeqNum";
        if (fieldType == FixFieldType.NumInGroup) return "NumInGroup";
        if (fieldType == FixFieldType.Length) return "Length";
        if (fieldType == FixFieldType.TagNum) return "TagNum";
        if (fieldType == FixFieldType.Float) return "float";
        if (fieldType == FixFieldType.Qty) return "Qty";
        if (fieldType == FixFieldType.Price) return "Price";
        if (fieldType == FixFieldType.PriceOffset) return "PriceOffset";
        if (fieldType == FixFieldType.Amt) return "Amt";
        if (fieldType == FixFieldType.Percentage) return "Percentage";
        if (fieldType == FixFieldType.Char) return "char";
        if (fieldType == FixFieldType.Boolean) return "Boolean";
        if (fieldType == FixFieldType.String) return "String";
        if (fieldType == FixFieldType.MultipleCharValue) return "MultipleCharValue";
        if (fieldType == FixFieldType.MultipleStringValue) return "MultipleStringValue";
        if (fieldType == FixFieldType.Currency) return "Currency";
        if (fieldType == FixFieldType.Country) return "Country";
        if (fieldType == FixFieldType.Exchange) return "Exchange";
        if (fieldType == FixFieldType.MonthYear) return "MonthYear";
        if (fieldType == FixFieldType.DayOfMonth) return "DayOfMonth";
        if (fieldType == FixFieldType.LocalMktDate) return "LocalMktDate";
        if (fieldType == FixFieldType.UtcDateOnly) return "UTCDateOnly";
        if (fieldType == FixFieldType.UtcTimeOnly) return "UTCTimeOnly";
        if (fieldType == FixFieldType.UtcTimestamp) return "UTCTimestamp";
        if (fieldType == FixFieldType.TzTimeOnly) return "TZTimeOnly";
        if (fieldType == FixFieldType.TzTimestamp) return "TZTimestamp";
        if (fieldType == FixFieldType.Data) return "data";
        if (fieldType == FixFieldType.XmlData) return "XMLData";
        return "";
    }

    internal static bool HasParserErrors(List<DictionaryDiagnostic> diagnostics)
    {
        for (int index = 0; index < diagnostics.Count; index++)
        {
            if (diagnostics[index].Severity == ValidationSeverity.Error || diagnostics[index].Severity == ValidationSeverity.Fatal)
            {
                return true;
            }
        }
        return false;
    }

    internal static void AddParserDiagnostic(FixDictionary dictionary, ValidationSeverity severity, string code, string message, string definitionName, bool hasTag, int tag)
    {
        if (dictionary.Diagnostics.Count >= FixDefaults.MaxDiagnostics)
        {
            return;
        }
        DictionaryDiagnostic diagnostic = new DictionaryDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.DefinitionName = definitionName;
        diagnostic.HasTag = hasTag;
        diagnostic.Tag = tag;
        dictionary.Diagnostics.Add(diagnostic);
    }
}
