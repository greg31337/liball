using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace fix;

internal static class QuickFixXmlParser
{
    internal static DictionaryLoadResult ParseDictionary(Stream source, FixDictionaryOptions options)
    {
        DictionaryLoadResult result = new DictionaryLoadResult();
        FixDictionary dictionary = new FixDictionary();
        dictionary.Options = options;
        result.Dictionary = dictionary;
        if (!source.CanRead)
        {
            AddParserDiagnostic(dictionary, ValidationSeverity.Error, "unreadable-source", "The QuickFIX XML source is not readable.", "", false, 0);
            CompleteLoadResult(result, dictionary);
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
            string message = loaded.Error == null ? "The QuickFIX XML document could not be loaded." : loaded.Error.Message;
            AddParserDiagnostic(dictionary, ValidationSeverity.Error, "invalid-quickfix-xml", message, "", false, 0);
            DictionaryDiagnostic diagnostic = dictionary.Diagnostics[dictionary.Diagnostics.Count - 1];
            diagnostic.Line = loaded.Line;
            diagnostic.Column = loaded.Column;
            CompleteLoadResult(result, dictionary);
            return result;
        }
        XmlDocument document = loaded.Document;

        XmlElement? possibleRoot = document.DocumentElement;
        if (possibleRoot == null || possibleRoot.LocalName != "fix")
        {
            AddParserDiagnostic(dictionary, ValidationSeverity.Error, "invalid-quickfix-root", "The document root must be a QuickFIX fix element.", "", false, 0);
            CompleteLoadResult(result, dictionary);
            return result;
        }
        XmlElement root = possibleRoot;
        ParseDictionaryIdentity(root, dictionary);
        XmlElement? fieldsElement = FindDirectChild(root, "fields");
        if (fieldsElement == null)
        {
            AddParserDiagnostic(dictionary, ValidationSeverity.Error, "missing-fields-section", "The QuickFIX dictionary has no fields section.", dictionary.Identity.Name, false, 0);
        }
        else
        {
            ParseFieldDefinitions(fieldsElement, dictionary);
            InferLengthDataPairs(dictionary);
        }

        XmlElement? componentsElement = FindDirectChild(root, "components");
        if (componentsElement != null)
        {
            RegisterComponents(componentsElement, dictionary);
            ParseComponentDefinitions(componentsElement, dictionary, options);
        }
        ParseHeaderOrTrailer(root, "header", dictionary.Header, dictionary, options);
        ParseHeaderOrTrailer(root, "trailer", dictionary.Trailer, dictionary, options);
        ParseMessageDefinitions(root, dictionary, options);

        DictionaryIndex normalized = DictionaryNormalizer.NormalizeDictionary(dictionary, options);
        result.Success = normalized.Success && !HasErrorDiagnostics(dictionary.Diagnostics);
        result.Diagnostics = dictionary.Diagnostics;
        return result;
    }

    internal static void CompleteLoadResult(DictionaryLoadResult result, FixDictionary dictionary)
    {
        result.Success = false;
        result.Diagnostics = dictionary.Diagnostics;
    }

    internal static void ParseDictionaryIdentity(XmlElement root, FixDictionary dictionary)
    {
        string type = root.GetAttribute("type").Trim().ToUpperInvariant();
        string major = root.GetAttribute("major").Trim();
        string minor = root.GetAttribute("minor").Trim();
        string servicePack = root.GetAttribute("servicepack").Trim();
        if (servicePack.Length == 0)
        {
            servicePack = "0";
        }
        if (type != "FIX" && type != "FIXT")
        {
            AddParserDiagnostic(dictionary, ValidationSeverity.Error, "invalid-fix-type", "The QuickFIX root type must be FIX or FIXT.", type, false, 0);
        }
        if (major.Length == 0 || minor.Length == 0)
        {
            AddParserDiagnostic(dictionary, ValidationSeverity.Error, "missing-fix-version", "The QuickFIX root must declare major and minor versions.", type, false, 0);
        }
        string version = type + "." + major + "." + minor;
        dictionary.Identity.BeginString = version;
        dictionary.Identity.Name = type + major + minor;
        if (servicePack != "0")
        {
            dictionary.Identity.Name = dictionary.Identity.Name + "SP" + servicePack;
        }
        if (type == "FIX")
        {
            dictionary.Identity.ApplicationVersion = version;
            if (servicePack != "0")
            {
                dictionary.Identity.ApplicationVersion = version + "SP" + servicePack;
            }
        }
    }

    internal static void ParseFieldDefinitions(XmlElement fieldsElement, FixDictionary dictionary)
    {
        foreach (XmlNode node in fieldsElement.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)node;
            if (element.LocalName != "field")
            {
                AddParserDiagnostic(dictionary, ValidationSeverity.Warning, "unsupported-fields-element", "An unsupported element in the fields section was ignored.", element.LocalName, false, 0);
                continue;
            }
            string numberText = element.GetAttribute("number").Trim();
            int tag = ParsePositiveInteger(numberText, FixDefaults.MaxTagNumber);
            string name = element.GetAttribute("name").Trim();
            string typeName = element.GetAttribute("type").Trim();
            if (tag <= 0)
            {
                AddParserDiagnostic(dictionary, ValidationSeverity.Error, "invalid-field-number", "A QuickFIX field number is missing or invalid.", name, false, 0);
                continue;
            }
            if (name.Length == 0)
            {
                AddParserDiagnostic(dictionary, ValidationSeverity.Error, "missing-field-name", "A QuickFIX field name is missing.", numberText, true, tag);
                continue;
            }
            FixFieldType fieldType = ResolveQuickFixFieldType(typeName);
            EnsureDatatypeDefinition(dictionary, typeName, fieldType);
            FixFieldDefinition field = new FixFieldDefinition();
            field.Tag = tag;
            field.Name = name;
            field.DatatypeName = typeName;
            field.FieldType = fieldType;
            dictionary.Fields.Add(field);
            ParseCodeSet(element, field, dictionary);
        }
    }

    internal static void ParseCodeSet(XmlElement fieldElement, FixFieldDefinition field, FixDictionary dictionary)
    {
        List<FixCodeDefinition> codes = [];
        int sortOrder = 0;
        foreach (XmlNode node in fieldElement.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement valueElement = (XmlElement)node;
            if (valueElement.LocalName != "value")
            {
                AddParserDiagnostic(dictionary, ValidationSeverity.Warning, "unsupported-field-element", "An unsupported element in a field definition was ignored.", field.Name, true, field.Tag);
                continue;
            }
            string value = valueElement.GetAttribute("enum");
            string name = valueElement.GetAttribute("description").Trim();
            if (value.Length == 0)
            {
                AddParserDiagnostic(dictionary, ValidationSeverity.Error, "missing-code-value", "A QuickFIX enum value is missing.", field.Name, true, field.Tag);
                continue;
            }
            FixCodeDefinition code = new FixCodeDefinition();
            code.Name = name.Length == 0 ? value : name;
            code.Value = value;
            code.SortOrder = sortOrder;
            sortOrder++;
            codes.Add(code);
        }
        if (codes.Count == 0)
        {
            return;
        }
        FixCodeSetDefinition codeSet = new FixCodeSetDefinition();
        codeSet.Id = field.Tag;
        codeSet.Name = field.Name + "CodeSet";
        codeSet.FieldType = field.FieldType;
        codeSet.Codes = codes;
        dictionary.CodeSets.Add(codeSet);
        field.CodeSetName = codeSet.Name;
    }

    internal static void EnsureDatatypeDefinition(FixDictionary dictionary, string typeName, FixFieldType fieldType)
    {
        if (typeName.Length == 0)
        {
            return;
        }
        string key = typeName.Trim().ToUpperInvariant();
        for (int index = 0; index < dictionary.Datatypes.Count; index++)
        {
            if (dictionary.Datatypes[index].Name.Trim().ToUpperInvariant() == key)
            {
                return;
            }
        }
        FixDatatypeDefinition datatype = new FixDatatypeDefinition();
        datatype.Name = typeName;
        datatype.FieldType = fieldType;
        dictionary.Datatypes.Add(datatype);
    }

    internal static void InferLengthDataPairs(FixDictionary dictionary)
    {
        for (int index = 0; index + 1 < dictionary.Fields.Count; index++)
        {
            FixFieldDefinition length = dictionary.Fields[index];
            FixFieldDefinition data = dictionary.Fields[index + 1];
            if (length.FieldType == FixFieldType.Length && (data.FieldType == FixFieldType.Data || data.FieldType == FixFieldType.XmlData) && IsLengthNameForData(length.Name, data.Name))
            {
                length.HasDataTag = true;
                length.DataTag = data.Tag;
                data.HasLengthTag = true;
                data.LengthTag = length.Tag;
            }
        }
    }

    internal static bool IsLengthNameForData(string lengthName, string dataName)
    {
        if (lengthName.EndsWith("Length", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(lengthName.Substring(0, lengthName.Length - 6), dataName, StringComparison.OrdinalIgnoreCase);
        }
        if (lengthName.EndsWith("Len", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(lengthName.Substring(0, lengthName.Length - 3), dataName, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    internal static void RegisterComponents(XmlElement componentsElement, FixDictionary dictionary)
    {
        foreach (XmlNode node in componentsElement.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)node;
            if (element.LocalName != "component")
            {
                AddParserDiagnostic(dictionary, ValidationSeverity.Warning, "unsupported-components-element", "An unsupported element in the components section was ignored.", element.LocalName, false, 0);
                continue;
            }
            FixComponentDefinition component = new FixComponentDefinition();
            component.Id = dictionary.Components.Count + 1;
            component.Name = element.GetAttribute("name").Trim();
            dictionary.Components.Add(component);
        }
    }

    internal static void ParseComponentDefinitions(XmlElement componentsElement, FixDictionary dictionary, FixDictionaryOptions options)
    {
        int componentIndex = 0;
        foreach (XmlNode node in componentsElement.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)node;
            if (element.LocalName != "component")
            {
                continue;
            }
            if (componentIndex < dictionary.Components.Count)
            {
                ParseMemberElements(element, dictionary.Components[componentIndex].Members, dictionary.Components[componentIndex].Name, dictionary, options);
            }
            componentIndex++;
        }
    }

    internal static void ParseHeaderOrTrailer(XmlElement root, string elementName, List<FixMemberDefinition> destination, FixDictionary dictionary, FixDictionaryOptions options)
    {
        XmlElement? element = FindDirectChild(root, elementName);
        if (element == null)
        {
            AddParserDiagnostic(dictionary, ValidationSeverity.Error, "missing-" + elementName + "-section", "The QuickFIX dictionary is missing a required section.", elementName, false, 0);
            return;
        }
        ParseMemberElements(element, destination, elementName, dictionary, options);
    }

    internal static void ParseMessageDefinitions(XmlElement root, FixDictionary dictionary, FixDictionaryOptions options)
    {
        XmlElement? messages = FindDirectChild(root, "messages");
        if (messages == null)
        {
            AddParserDiagnostic(dictionary, ValidationSeverity.Error, "missing-messages-section", "The QuickFIX dictionary has no messages section.", dictionary.Identity.Name, false, 0);
            return;
        }
        foreach (XmlNode node in messages.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)node;
            if (element.LocalName != "message")
            {
                AddParserDiagnostic(dictionary, ValidationSeverity.Warning, "unsupported-messages-element", "An unsupported element in the messages section was ignored.", element.LocalName, false, 0);
                continue;
            }
            FixMessageDefinition message = new FixMessageDefinition();
            message.Id = dictionary.Messages.Count + 1;
            message.Name = element.GetAttribute("name").Trim();
            message.MsgType = element.GetAttribute("msgtype");
            message.Category = element.GetAttribute("msgcat").Trim();
            if (message.Name.Length == 0 || message.MsgType.Length == 0)
            {
                AddParserDiagnostic(dictionary, ValidationSeverity.Error, "invalid-message-definition", "A QuickFIX message must have a name and MsgType.", message.Name, false, 0);
            }
            ParseMemberElements(element, message.Members, message.Name, dictionary, options);
            dictionary.Messages.Add(message);
        }
    }

    internal static void ParseMemberElements(XmlElement parent, List<FixMemberDefinition> destination, string owner, FixDictionary dictionary, FixDictionaryOptions options)
    {
        foreach (XmlNode node in parent.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)node;
            FixMemberDefinition? member = null;
            if (element.LocalName == "field")
            {
                member = CreateFieldMember(element, owner, dictionary, options);
            }
            else if (element.LocalName == "component")
            {
                member = CreateComponentMember(element, owner, dictionary, options);
            }
            else if (element.LocalName == "group")
            {
                member = CreateGroupMember(element, owner, dictionary, options);
            }
            else
            {
                AddParserDiagnostic(dictionary, ValidationSeverity.Warning, "unsupported-member-element", "An unsupported member element was ignored.", owner, false, 0);
            }
            if (member != null)
            {
                member.Position = destination.Count;
                destination.Add(member);
            }
        }
    }

    internal static FixMemberDefinition CreateFieldMember(XmlElement element, string owner, FixDictionary dictionary, FixDictionaryOptions options)
    {
        string name = element.GetAttribute("name").Trim();
        FixFieldDefinition? field = FindFieldByName(dictionary, name, options.NormalizeNames);
        FixMemberDefinition member = new FixMemberDefinition();
        member.Name = name;
        member.Kind = FixMemberKind.Field;
        member.ReferenceId = field == null ? 0 : field.Tag;
        member.Presence = ParsePresence(element.GetAttribute("required"), owner, dictionary);
        if (field == null)
        {
            AddParserDiagnostic(dictionary, ReferenceSeverity(options), "unknown-field-reference", "A QuickFIX member references an unknown field.", owner + ":" + name, false, 0);
        }
        return member;
    }

    internal static FixMemberDefinition CreateComponentMember(XmlElement element, string owner, FixDictionary dictionary, FixDictionaryOptions options)
    {
        string name = element.GetAttribute("name").Trim();
        FixComponentDefinition? component = FindComponentByName(dictionary, name, options.NormalizeNames);
        FixMemberDefinition member = new FixMemberDefinition();
        member.Name = name;
        member.Kind = FixMemberKind.Component;
        member.ReferenceId = component == null ? 0 : component.Id;
        member.Presence = ParsePresence(element.GetAttribute("required"), owner, dictionary);
        if (component == null)
        {
            AddParserDiagnostic(dictionary, ReferenceSeverity(options), "unknown-component-reference", "A QuickFIX member references an unknown component.", owner + ":" + name, false, 0);
        }
        return member;
    }

    internal static FixMemberDefinition CreateGroupMember(XmlElement element, string owner, FixDictionary dictionary, FixDictionaryOptions options)
    {
        string name = element.GetAttribute("name").Trim();
        FixGroupDefinition? group = FindGroupByName(dictionary, name, options.NormalizeNames);
        if (group == null)
        {
            group = new FixGroupDefinition();
            group.Id = dictionary.Groups.Count + 1;
            group.Name = name;
            FixFieldDefinition? countField = FindFieldByName(dictionary, name, options.NormalizeNames);
            group.CountTag = countField == null ? 0 : countField.Tag;
            if (countField == null)
            {
                AddParserDiagnostic(dictionary, ReferenceSeverity(options), "unknown-group-count-field", "A QuickFIX group name does not resolve to a count field.", owner + ":" + name, false, 0);
            }
            dictionary.Groups.Add(group);
            ParseMemberElements(element, group.Members, name, dictionary, options);
            group.DelimiterTag = FindFirstFieldMemberTag(group.Members, dictionary, 0);
        }
        FixMemberDefinition member = new FixMemberDefinition();
        member.Name = name;
        member.Kind = FixMemberKind.Group;
        member.ReferenceId = group.Id;
        member.Presence = ParsePresence(element.GetAttribute("required"), owner, dictionary);
        return member;
    }

    internal static int FindFirstFieldMemberTag(List<FixMemberDefinition> members, FixDictionary dictionary, int depth)
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
            if (member.Kind == FixMemberKind.Component)
            {
                FixComponentDefinition? component = FindComponentById(dictionary, member.ReferenceId);
                if (component != null)
                {
                    int tag = FindFirstFieldMemberTag(component.Members, dictionary, depth + 1);
                    if (tag > 0)
                    {
                        return tag;
                    }
                }
            }
        }
        return 0;
    }

    internal static FixPresence ParsePresence(string required, string owner, FixDictionary dictionary)
    {
        string value = required.Trim().ToUpperInvariant();
        if (value == "Y")
        {
            return FixPresence.Required;
        }
        if (value.Length == 0 || value == "N")
        {
            return FixPresence.Optional;
        }
        AddParserDiagnostic(dictionary, ValidationSeverity.Warning, "invalid-required-flag", "A QuickFIX required flag is neither Y nor N and was treated as optional.", owner, false, 0);
        return FixPresence.Optional;
    }

    internal static FixFieldDefinition? FindFieldByName(FixDictionary dictionary, string name, bool normalizeNames)
    {
        string key = DictionaryNormalizer.CreateNameKey(name, normalizeNames);
        for (int index = 0; index < dictionary.Fields.Count; index++)
        {
            if (DictionaryNormalizer.CreateNameKey(dictionary.Fields[index].Name, normalizeNames) == key)
            {
                return dictionary.Fields[index];
            }
        }
        return null;
    }

    internal static FixComponentDefinition? FindComponentByName(FixDictionary dictionary, string name, bool normalizeNames)
    {
        string key = DictionaryNormalizer.CreateNameKey(name, normalizeNames);
        for (int index = 0; index < dictionary.Components.Count; index++)
        {
            if (DictionaryNormalizer.CreateNameKey(dictionary.Components[index].Name, normalizeNames) == key)
            {
                return dictionary.Components[index];
            }
        }
        return null;
    }

    internal static FixComponentDefinition? FindComponentById(FixDictionary dictionary, int id)
    {
        for (int index = 0; index < dictionary.Components.Count; index++)
        {
            if (dictionary.Components[index].Id == id)
            {
                return dictionary.Components[index];
            }
        }
        return null;
    }

    internal static FixGroupDefinition? FindGroupByName(FixDictionary dictionary, string name, bool normalizeNames)
    {
        string key = DictionaryNormalizer.CreateNameKey(name, normalizeNames);
        for (int index = 0; index < dictionary.Groups.Count; index++)
        {
            if (DictionaryNormalizer.CreateNameKey(dictionary.Groups[index].Name, normalizeNames) == key)
            {
                return dictionary.Groups[index];
            }
        }
        return null;
    }

    internal static XmlElement? FindDirectChild(XmlElement parent, string name)
    {
        foreach (XmlNode node in parent.ChildNodes)
        {
            if (node.NodeType == XmlNodeType.Element && node.LocalName == name)
            {
                return (XmlElement)node;
            }
        }
        return null;
    }

    internal static int ParsePositiveInteger(string value, int maximum)
    {
        if (value.Length == 0)
        {
            return -1;
        }
        AsciiIntegerResult parsed = AsciiDigits.ParseUnsignedInt(Encoding.ASCII.GetBytes(value), maximum);
        return parsed.Success && parsed.Value > 0 ? parsed.Value : -1;
    }

    internal static FixFieldType ResolveQuickFixFieldType(string name)
    {
        FixFieldType standard = DictionaryNormalizer.ResolveBuiltinFieldType(name);
        if (standard != FixFieldType.Unknown)
        {
            return standard;
        }
        string key = name.Trim().ToUpperInvariant();
        if (key == "DATE" || key == "UTCDATE") return FixFieldType.UtcDateOnly;
        if (key == "TIME" || key == "LOCALMKTTIME") return FixFieldType.UtcTimeOnly;
        if (key == "LANGUAGE" || key == "XID" || key == "XIDREF") return FixFieldType.String;
        if (key == "MULTIPLEVALUESTRING") return FixFieldType.MultipleStringValue;
        return FixFieldType.Unknown;
    }

    internal static ValidationSeverity ReferenceSeverity(FixDictionaryOptions options)
    {
        return options.RequireResolvedReferences ? ValidationSeverity.Error : ValidationSeverity.Warning;
    }

    internal static bool HasErrorDiagnostics(List<DictionaryDiagnostic> diagnostics)
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
