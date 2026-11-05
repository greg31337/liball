using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace fix;

internal static class QuickFixXmlEmitter
{
    internal static DictionaryLoadResult WriteDictionaryXml(Stream destination, FixDictionary dictionary, FixDictionaryOptions options)
    {
        DictionaryLoadResult result = new DictionaryLoadResult();
        result.Dictionary = dictionary;
        if (!destination.CanWrite)
        {
            AddWriteDiagnostic(result, ValidationSeverity.Error, "unwritable-destination", "The QuickFIX XML destination is not writable.", "", false, 0);
            return result;
        }

        DictionaryIndex index = DictionaryNormalizer.CreateDictionaryIndex(dictionary, options);
        for (int diagnosticIndex = 0; diagnosticIndex < index.Diagnostics.Count; diagnosticIndex++)
        {
            result.Diagnostics.Add(index.Diagnostics[diagnosticIndex]);
        }
        AddDictionaryLossDiagnostics(dictionary, result);

        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Encoding = new UTF8Encoding(false);
        settings.Indent = true;
        settings.IndentChars = "  ";
        settings.NewLineChars = "\n";
        settings.NewLineHandling = NewLineHandling.None;
        settings.OmitXmlDeclaration = true;
        settings.CloseOutput = false;
        List<int> emittedGroups = [];
        List<int> groupPath = [];
        using (XmlWriter writer = XmlWriter.Create(destination, settings))
        {
            WriteRootStart(writer, dictionary, result);
            WriteMemberSection(writer, "header", dictionary.Header, dictionary, index, options, result, emittedGroups, groupPath);
            WriteMessages(writer, dictionary, index, options, result, emittedGroups, groupPath);
            WriteMemberSection(writer, "trailer", dictionary.Trailer, dictionary, index, options, result, emittedGroups, groupPath);
            WriteComponents(writer, dictionary, index, options, result, emittedGroups, groupPath);
            WriteFields(writer, dictionary, index, options, result);
            writer.WriteEndElement();
            writer.Flush();
        }
        AddUnreferencedGroupDiagnostics(dictionary, emittedGroups, result);
        result.Success = !HasWriteErrors(result.Diagnostics);
        return result;
    }

    internal static void WriteRootStart(XmlWriter writer, FixDictionary dictionary, DictionaryLoadResult result)
    {
        string version = ResolveSelectedVersion(dictionary.Identity);
        string type = version.StartsWith("FIXT.", StringComparison.OrdinalIgnoreCase) ? "FIXT" : "FIX";
        string major = ExtractMajorVersion(version);
        string minor = ExtractMinorVersion(version);
        string applicationVersion = ResolveApplicationVersionIdentifier(dictionary.Identity.ApplicationVersion);
        string servicePack = ExtractServicePack(version);
        if (type == "FIX" && SameVersionBase(version, applicationVersion))
        {
            servicePack = ExtractServicePack(applicationVersion);
        }
        if (servicePack.Length == 0)
        {
            servicePack = "0";
        }
        if (major.Length == 0 || minor.Length == 0)
        {
            AddWriteDiagnostic(result, ValidationSeverity.Error, "invalid-dictionary-version", "The dictionary identity does not contain a writable FIX version.", dictionary.Identity.Name, false, 0);
            major = major.Length == 0 ? "0" : major;
            minor = minor.Length == 0 ? "0" : minor;
        }
        writer.WriteStartElement("fix");
        writer.WriteAttributeString("type", type);
        writer.WriteAttributeString("major", major);
        writer.WriteAttributeString("minor", minor);
        writer.WriteAttributeString("servicepack", servicePack);
    }

    internal static string ExtractMajorVersion(string version)
    {
        int firstDot = version.IndexOf('.');
        if (firstDot < 0 || firstDot + 1 >= version.Length)
        {
            return "";
        }
        int secondDot = version.IndexOf('.', firstDot + 1);
        if (secondDot < 0)
        {
            return "";
        }
        return version.Substring(firstDot + 1, secondDot - firstDot - 1);
    }

    internal static string ExtractMinorVersion(string version)
    {
        int firstDot = version.IndexOf('.');
        if (firstDot < 0)
        {
            return "";
        }
        int secondDot = version.IndexOf('.', firstDot + 1);
        if (secondDot < 0 || secondDot + 1 >= version.Length)
        {
            return "";
        }
        int end = version.IndexOf("SP", secondDot + 1, StringComparison.OrdinalIgnoreCase);
        if (end < 0)
        {
            end = version.Length;
        }
        return version.Substring(secondDot + 1, end - secondDot - 1);
    }

    internal static string ExtractServicePack(string version)
    {
        int marker = version.IndexOf("SP", StringComparison.OrdinalIgnoreCase);
        if (marker < 0 || marker + 2 >= version.Length)
        {
            return "";
        }
        return version.Substring(marker + 2);
    }

    internal static string ResolveSelectedVersion(FixDictionaryIdentity identity)
    {
        if (identity.BeginString.Length != 0)
        {
            return identity.BeginString;
        }
        return ResolveApplicationVersionIdentifier(identity.ApplicationVersion);
    }

    internal static string ResolveApplicationVersionIdentifier(string applicationVersion)
    {
        if (applicationVersion.StartsWith("FIX.", StringComparison.OrdinalIgnoreCase)) return applicationVersion;
        if (applicationVersion == FixApplicationVersions.Fix27) return "FIX.2.7";
        if (applicationVersion == FixApplicationVersions.Fix30) return "FIX.3.0";
        if (applicationVersion == FixApplicationVersions.Fix40) return "FIX.4.0";
        if (applicationVersion == FixApplicationVersions.Fix41) return "FIX.4.1";
        if (applicationVersion == FixApplicationVersions.Fix42) return "FIX.4.2";
        if (applicationVersion == FixApplicationVersions.Fix43) return "FIX.4.3";
        if (applicationVersion == FixApplicationVersions.Fix44) return "FIX.4.4";
        if (applicationVersion == FixApplicationVersions.Fix50) return "FIX.5.0";
        if (applicationVersion == FixApplicationVersions.Fix50Sp1) return "FIX.5.0SP1";
        if (applicationVersion == FixApplicationVersions.Fix50Sp2) return "FIX.5.0SP2";
        return "";
    }

    internal static bool SameVersionBase(string first, string second)
    {
        if (second.Length == 0)
        {
            return false;
        }
        return ExtractMajorVersion(first) == ExtractMajorVersion(second) && ExtractMinorVersion(first) == ExtractMinorVersion(second);
    }

    internal static void WriteMemberSection(XmlWriter writer, string elementName, List<FixMemberDefinition> members, FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options, DictionaryLoadResult result, List<int> emittedGroups, List<int> groupPath)
    {
        writer.WriteStartElement(elementName);
        List<FixMemberDefinition> ordered = OrderMembers(members, options.PreserveSourceOrder);
        for (int memberIndex = 0; memberIndex < ordered.Count; memberIndex++)
        {
            WriteMember(writer, ordered[memberIndex], dictionary, index, options, result, emittedGroups, groupPath);
        }
        writer.WriteEndElement();
    }

    internal static void WriteMessages(XmlWriter writer, FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options, DictionaryLoadResult result, List<int> emittedGroups, List<int> groupPath)
    {
        writer.WriteStartElement("messages");
        List<FixMessageDefinition> messages = OrderMessages(dictionary.Messages, options.PreserveSourceOrder);
        for (int messageIndex = 0; messageIndex < messages.Count; messageIndex++)
        {
            FixMessageDefinition message = messages[messageIndex];
            writer.WriteStartElement("message");
            writer.WriteAttributeString("name", message.Name);
            writer.WriteAttributeString("msgtype", message.MsgType);
            writer.WriteAttributeString("msgcat", message.Category);
            AddMessageLossDiagnostics(message, result);
            List<FixMemberDefinition> members = OrderMembers(message.Members, options.PreserveSourceOrder);
            for (int memberIndex = 0; memberIndex < members.Count; memberIndex++)
            {
                WriteMember(writer, members[memberIndex], dictionary, index, options, result, emittedGroups, groupPath);
            }
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteComponents(XmlWriter writer, FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options, DictionaryLoadResult result, List<int> emittedGroups, List<int> groupPath)
    {
        writer.WriteStartElement("components");
        List<FixComponentDefinition> components = OrderComponents(dictionary.Components, options.PreserveSourceOrder);
        for (int componentIndex = 0; componentIndex < components.Count; componentIndex++)
        {
            FixComponentDefinition component = components[componentIndex];
            writer.WriteStartElement("component");
            writer.WriteAttributeString("name", component.Name);
            if (component.Abbreviation.Length != 0 || component.Description.Length != 0)
            {
                AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-component-metadata", "QuickFIX XML cannot represent component abbreviation or description metadata.", component.Name, false, 0);
            }
            List<FixMemberDefinition> members = OrderMembers(component.Members, options.PreserveSourceOrder);
            for (int memberIndex = 0; memberIndex < members.Count; memberIndex++)
            {
                WriteMember(writer, members[memberIndex], dictionary, index, options, result, emittedGroups, groupPath);
            }
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteFields(XmlWriter writer, FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options, DictionaryLoadResult result)
    {
        writer.WriteStartElement("fields");
        List<FixFieldDefinition> fields = OrderFields(dictionary.Fields, options.PreserveSourceOrder);
        for (int fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
        {
            FixFieldDefinition field = fields[fieldIndex];
            writer.WriteStartElement("field");
            writer.WriteAttributeString("number", FormatInteger(field.Tag));
            writer.WriteAttributeString("name", field.Name);
            writer.WriteAttributeString("type", ResolveFieldTypeName(field, index, result));
            FixCodeSetDefinition? codeSet = FindCodeSetForField(field, index);
            if (codeSet != null)
            {
                WriteCodeValues(writer, codeSet, options, result);
                if (codeSet.Name != field.Name + "CodeSet")
                {
                    AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-code-set-name", "QuickFIX XML cannot preserve a distinct canonical code-set name.", codeSet.Name, true, field.Tag);
                }
                if (codeSet.Description.Length != 0)
                {
                    AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-code-set-description", "QuickFIX XML cannot represent code-set description metadata.", codeSet.Name, true, field.Tag);
                }
            }
            else if (field.CodeSetName.Length != 0)
            {
                AddWriteDiagnostic(result, ValidationSeverity.Error, "unresolved-code-set-reference", "A field references a code set that cannot be written.", field.Name, true, field.Tag);
            }
            if (field.Abbreviation.Length != 0 || field.Description.Length != 0)
            {
                AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-field-metadata", "QuickFIX XML cannot represent field abbreviation or description metadata.", field.Name, true, field.Tag);
            }
            if (field.HasDataTag)
            {
                FixFieldDefinition? data = index.FindFieldByTag(field.DataTag);
                if (data == null || !QuickFixXmlParser.IsLengthNameForData(field.Name, data.Name))
                {
                    AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-length-association", "QuickFIX XML cannot explicitly represent this length/data association.", field.Name, true, field.Tag);
                }
            }
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteCodeValues(XmlWriter writer, FixCodeSetDefinition codeSet, FixDictionaryOptions options, DictionaryLoadResult result)
    {
        List<FixCodeDefinition> codes = OrderCodes(codeSet.Codes, options.PreserveSourceOrder);
        for (int codeIndex = 0; codeIndex < codes.Count; codeIndex++)
        {
            FixCodeDefinition code = codes[codeIndex];
            writer.WriteStartElement("value");
            writer.WriteAttributeString("enum", code.Value);
            writer.WriteAttributeString("description", code.Name);
            writer.WriteEndElement();
            if (code.Description.Length != 0)
            {
                AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-code-description", "QuickFIX XML cannot preserve a separate canonical code description.", codeSet.Name + ":" + code.Name, false, 0);
            }
        }
    }

    internal static void WriteMember(XmlWriter writer, FixMemberDefinition member, FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options, DictionaryLoadResult result, List<int> emittedGroups, List<int> groupPath)
    {
        if (member.Kind == FixMemberKind.Field)
        {
            FixFieldDefinition? field = index.FindFieldByTag(member.ReferenceId);
            writer.WriteStartElement("field");
            writer.WriteAttributeString("name", field == null ? member.Name : field.Name);
            WriteRequiredAttribute(writer, member, result);
            writer.WriteEndElement();
            if (field == null)
            {
                AddWriteDiagnostic(result, ValidationSeverity.Error, "unresolved-field-reference", "A member references a field that cannot be written.", member.Name, true, member.ReferenceId);
            }
        }
        else if (member.Kind == FixMemberKind.Component)
        {
            FixComponentDefinition? component = index.FindComponentById(member.ReferenceId);
            writer.WriteStartElement("component");
            writer.WriteAttributeString("name", component == null ? member.Name : component.Name);
            WriteRequiredAttribute(writer, member, result);
            writer.WriteEndElement();
            if (component == null)
            {
                AddWriteDiagnostic(result, ValidationSeverity.Error, "unresolved-component-reference", "A member references a component that cannot be written.", member.Name, false, 0);
            }
        }
        else if (member.Kind == FixMemberKind.Group)
        {
            FixGroupDefinition? group = index.FindGroupById(member.ReferenceId);
            if (group == null)
            {
                writer.WriteStartElement("group");
                writer.WriteAttributeString("name", member.Name);
                WriteRequiredAttribute(writer, member, result);
                writer.WriteEndElement();
                AddWriteDiagnostic(result, ValidationSeverity.Error, "unresolved-group-reference", "A member references a group that cannot be written.", member.Name, false, 0);
            }
            else
            {
                WriteGroup(writer, member, group, dictionary, index, options, result, emittedGroups, groupPath);
            }
        }
        else
        {
            AddWriteDiagnostic(result, ValidationSeverity.Error, "unsupported-member-kind", "The member kind cannot be represented in QuickFIX XML.", member.Name, false, 0);
        }
        AddMemberLossDiagnostics(member, result);
    }

    internal static void WriteGroup(XmlWriter writer, FixMemberDefinition member, FixGroupDefinition group, FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options, DictionaryLoadResult result, List<int> emittedGroups, List<int> groupPath)
    {
        FixFieldDefinition? countField = index.FindFieldByTag(group.CountTag);
        bool firstEmission = !emittedGroups.Contains(group.Id);
        writer.WriteStartElement("group");
        writer.WriteAttributeString("name", countField == null ? group.Name : countField.Name);
        WriteRequiredAttribute(writer, member, result);
        if (groupPath.Contains(group.Id))
        {
            AddWriteDiagnostic(result, ValidationSeverity.Error, "recursive-group-reference", "A recursive group cannot be expanded into QuickFIX XML.", group.Name, true, group.CountTag);
            writer.WriteEndElement();
            return;
        }
        groupPath.Add(group.Id);
        if (firstEmission)
        {
            emittedGroups.Add(group.Id);
            if (countField == null)
            {
                AddWriteDiagnostic(result, ValidationSeverity.Error, "unresolved-group-count-field", "A group count tag does not resolve to a field that can be written.", group.Name, true, group.CountTag);
            }
            else if (group.Name.Length != 0 && group.Name != countField.Name)
            {
                AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-group-name", "QuickFIX XML identifies a group by its count-field name and cannot preserve a distinct canonical group name.", group.Name, true, group.CountTag);
            }
            int inferredDelimiter = QuickFixXmlParser.FindFirstFieldMemberTag(group.Members, dictionary, 0);
            if (group.DelimiterTag != inferredDelimiter)
            {
                AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-group-delimiter", "QuickFIX XML infers the group delimiter from its first field and cannot preserve this delimiter tag.", group.Name, true, group.DelimiterTag);
            }
            if (group.Description.Length != 0)
            {
                AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-group-description", "QuickFIX XML cannot represent group description metadata.", group.Name, true, group.CountTag);
            }
        }
        List<FixMemberDefinition> members = OrderMembers(group.Members, options.PreserveSourceOrder);
        for (int memberIndex = 0; memberIndex < members.Count; memberIndex++)
        {
            WriteMember(writer, members[memberIndex], dictionary, index, options, result, emittedGroups, groupPath);
        }
        groupPath.RemoveAt(groupPath.Count - 1);
        writer.WriteEndElement();
    }

    internal static void WriteRequiredAttribute(XmlWriter writer, FixMemberDefinition member, DictionaryLoadResult result)
    {
        writer.WriteAttributeString("required", member.Presence == FixPresence.Required ? "Y" : "N");
        if (member.Presence != FixPresence.Required && member.Presence != FixPresence.Optional)
        {
            AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-presence", "QuickFIX XML can represent only required or optional presence.", member.Name, member.Kind == FixMemberKind.Field, member.ReferenceId);
        }
    }

    internal static void AddDictionaryLossDiagnostics(FixDictionary dictionary, DictionaryLoadResult result)
    {
        string selectedVersion = ResolveSelectedVersion(dictionary.Identity);
        string selectedType = selectedVersion.StartsWith("FIXT.", StringComparison.OrdinalIgnoreCase) ? "FIXT" : "FIX";
        string expectedName = selectedType + ExtractMajorVersion(selectedVersion) + ExtractMinorVersion(selectedVersion);
        string applicationVersion = ResolveApplicationVersionIdentifier(dictionary.Identity.ApplicationVersion);
        string selectedServicePack = ExtractServicePack(selectedVersion);
        if (selectedType == "FIX" && SameVersionBase(selectedVersion, applicationVersion))
        {
            selectedServicePack = ExtractServicePack(applicationVersion);
        }
        if (selectedServicePack.Length != 0 && selectedServicePack != "0")
        {
            expectedName = expectedName + "SP" + selectedServicePack;
        }
        if (dictionary.Identity.Name.Length != 0 && dictionary.Identity.Name != expectedName)
        {
            AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-dictionary-name", "QuickFIX XML cannot preserve a distinct canonical dictionary name.", dictionary.Identity.Name, false, 0);
        }
        if (selectedType == "FIXT" && dictionary.Identity.ApplicationVersion.Length != 0)
        {
            AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-application-version", "QuickFIX XML cannot represent a separate application version on a FIXT transport dictionary.", dictionary.Identity.ApplicationVersion, false, 0);
        }
        if (dictionary.Identity.HasExtensionPack)
        {
            AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-extension-pack", "QuickFIX XML cannot represent a FIX extension-pack identifier.", dictionary.Identity.Name, false, 0);
        }
        if (dictionary.Identity.CustomVersion.Length != 0)
        {
            AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-custom-version", "QuickFIX XML cannot represent a custom application version identifier.", dictionary.Identity.Name, false, 0);
        }
        if (dictionary.Identity.Scenario.Length != 0)
        {
            AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-dictionary-scenario", "QuickFIX XML cannot represent a selected dictionary scenario.", dictionary.Identity.Scenario, false, 0);
        }
        for (int datatypeIndex = 0; datatypeIndex < dictionary.Datatypes.Count; datatypeIndex++)
        {
            FixDatatypeDefinition datatype = dictionary.Datatypes[datatypeIndex];
            string canonicalName = ResolveCanonicalTypeName(datatype.FieldType);
            if (datatype.BaseTypeName.Length != 0 || datatype.Description.Length != 0 || (canonicalName.Length != 0 && datatype.Name.ToUpperInvariant() != canonicalName))
            {
                AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-datatype-metadata", "QuickFIX XML cannot declare canonical datatype alias or description metadata.", datatype.Name, false, 0);
            }
        }
        for (int scenarioIndex = 0; scenarioIndex < dictionary.Scenarios.Count; scenarioIndex++)
        {
            AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-scenario", "QuickFIX XML cannot represent canonical scenario definitions.", dictionary.Scenarios[scenarioIndex].Name, false, 0);
        }
    }

    internal static void AddMessageLossDiagnostics(FixMessageDefinition message, DictionaryLoadResult result)
    {
        if (message.Scenario.Length != 0)
        {
            AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-message-scenario", "QuickFIX XML cannot represent message scenarios.", message.Name, false, 0);
        }
        if (message.Description.Length != 0)
        {
            AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-message-description", "QuickFIX XML cannot represent message description metadata.", message.Name, false, 0);
        }
    }

    internal static void AddMemberLossDiagnostics(FixMemberDefinition member, DictionaryLoadResult result)
    {
        if (member.Scenario.Length != 0)
        {
            AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-member-scenario", "QuickFIX XML cannot represent member scenarios.", member.Name, member.Kind == FixMemberKind.Field, member.ReferenceId);
        }
        if (member.PresenceRule.Length != 0)
        {
            AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-presence-rule", "QuickFIX XML cannot represent conditional presence expressions.", member.Name, member.Kind == FixMemberKind.Field, member.ReferenceId);
        }
    }

    internal static void AddUnreferencedGroupDiagnostics(FixDictionary dictionary, List<int> emittedGroups, DictionaryLoadResult result)
    {
        for (int groupIndex = 0; groupIndex < dictionary.Groups.Count; groupIndex++)
        {
            FixGroupDefinition group = dictionary.Groups[groupIndex];
            if (!emittedGroups.Contains(group.Id))
            {
                AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-unreferenced-group", "QuickFIX XML cannot declare a group that is not referenced by a member.", group.Name, true, group.CountTag);
            }
        }
    }

    internal static FixCodeSetDefinition? FindCodeSetForField(FixFieldDefinition field, DictionaryIndex index)
    {
        if (field.CodeSetName.Length == 0)
        {
            return null;
        }
        return index.FindCodeSetByName(field.CodeSetName);
    }

    internal static string ResolveFieldTypeName(FixFieldDefinition field, DictionaryIndex index, DictionaryLoadResult result)
    {
        FixFieldType resolvedType = field.FieldType;
        if (resolvedType == FixFieldType.Unknown && field.DatatypeName.Length != 0)
        {
            resolvedType = DictionaryNormalizer.ResolveDatatypeFieldType(field.DatatypeName, index, index.DatatypesByName.Count);
        }
        string name = ResolveCanonicalTypeName(resolvedType);
        if (name.Length == 0)
        {
            AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-field-type", "The field type is unknown and was written as STRING.", field.Name, true, field.Tag);
            return "STRING";
        }
        if (field.DatatypeName.Length != 0 && QuickFixXmlParser.ResolveQuickFixFieldType(field.DatatypeName) != resolvedType)
        {
            AddWriteDiagnostic(result, ValidationSeverity.Warning, "quickfix-loss-field-datatype", "QuickFIX XML cannot preserve the field's canonical datatype name.", field.Name, true, field.Tag);
        }
        return name;
    }

    internal static string ResolveCanonicalTypeName(FixFieldType fieldType)
    {
        if (fieldType == FixFieldType.Int) return "INT";
        if (fieldType == FixFieldType.SeqNum) return "SEQNUM";
        if (fieldType == FixFieldType.NumInGroup) return "NUMINGROUP";
        if (fieldType == FixFieldType.Length) return "LENGTH";
        if (fieldType == FixFieldType.TagNum) return "TAGNUM";
        if (fieldType == FixFieldType.Float) return "FLOAT";
        if (fieldType == FixFieldType.Qty) return "QTY";
        if (fieldType == FixFieldType.Price) return "PRICE";
        if (fieldType == FixFieldType.PriceOffset) return "PRICEOFFSET";
        if (fieldType == FixFieldType.Amt) return "AMT";
        if (fieldType == FixFieldType.Percentage) return "PERCENTAGE";
        if (fieldType == FixFieldType.Char) return "CHAR";
        if (fieldType == FixFieldType.Boolean) return "BOOLEAN";
        if (fieldType == FixFieldType.String) return "STRING";
        if (fieldType == FixFieldType.MultipleCharValue) return "MULTIPLECHARVALUE";
        if (fieldType == FixFieldType.MultipleStringValue) return "MULTIPLESTRINGVALUE";
        if (fieldType == FixFieldType.Currency) return "CURRENCY";
        if (fieldType == FixFieldType.Country) return "COUNTRY";
        if (fieldType == FixFieldType.Exchange) return "EXCHANGE";
        if (fieldType == FixFieldType.MonthYear) return "MONTHYEAR";
        if (fieldType == FixFieldType.DayOfMonth) return "DAYOFMONTH";
        if (fieldType == FixFieldType.LocalMktDate) return "LOCALMKTDATE";
        if (fieldType == FixFieldType.UtcDateOnly) return "UTCDATEONLY";
        if (fieldType == FixFieldType.UtcTimeOnly) return "UTCTIMEONLY";
        if (fieldType == FixFieldType.UtcTimestamp) return "UTCTIMESTAMP";
        if (fieldType == FixFieldType.TzTimeOnly) return "TZTIMEONLY";
        if (fieldType == FixFieldType.TzTimestamp) return "TZTIMESTAMP";
        if (fieldType == FixFieldType.Data) return "DATA";
        if (fieldType == FixFieldType.XmlData) return "XMLDATA";
        return "";
    }

    internal static List<FixFieldDefinition> OrderFields(List<FixFieldDefinition> source, bool preserveSourceOrder)
    {
        List<FixFieldDefinition> ordered = new List<FixFieldDefinition>(source);
        if (!preserveSourceOrder)
        {
            for (int index = 1; index < ordered.Count; index++)
            {
                FixFieldDefinition value = ordered[index];
                int position = index;
                while (position > 0 && ordered[position - 1].Tag > value.Tag)
                {
                    ordered[position] = ordered[position - 1];
                    position--;
                }
                ordered[position] = value;
            }
        }
        return ordered;
    }

    internal static List<FixMessageDefinition> OrderMessages(List<FixMessageDefinition> source, bool preserveSourceOrder)
    {
        List<FixMessageDefinition> ordered = new List<FixMessageDefinition>(source);
        if (!preserveSourceOrder)
        {
            for (int index = 1; index < ordered.Count; index++)
            {
                FixMessageDefinition value = ordered[index];
                int position = index;
                while (position > 0 && CompareMessages(ordered[position - 1], value) > 0)
                {
                    ordered[position] = ordered[position - 1];
                    position--;
                }
                ordered[position] = value;
            }
        }
        return ordered;
    }

    internal static int CompareMessages(FixMessageDefinition first, FixMessageDefinition second)
    {
        int typeComparison = string.Compare(first.MsgType, second.MsgType, StringComparison.Ordinal);
        if (typeComparison != 0)
        {
            return typeComparison;
        }
        int scenarioComparison = string.Compare(first.Scenario, second.Scenario, StringComparison.Ordinal);
        return scenarioComparison != 0 ? scenarioComparison : string.Compare(first.Name, second.Name, StringComparison.Ordinal);
    }

    internal static List<FixComponentDefinition> OrderComponents(List<FixComponentDefinition> source, bool preserveSourceOrder)
    {
        List<FixComponentDefinition> ordered = new List<FixComponentDefinition>(source);
        if (!preserveSourceOrder)
        {
            for (int index = 1; index < ordered.Count; index++)
            {
                FixComponentDefinition value = ordered[index];
                int position = index;
                while (position > 0 && CompareComponents(ordered[position - 1], value) > 0)
                {
                    ordered[position] = ordered[position - 1];
                    position--;
                }
                ordered[position] = value;
            }
        }
        return ordered;
    }

    internal static int CompareComponents(FixComponentDefinition first, FixComponentDefinition second)
    {
        int nameComparison = string.Compare(first.Name, second.Name, StringComparison.Ordinal);
        if (nameComparison != 0)
        {
            return nameComparison;
        }
        if (first.Id < second.Id) return -1;
        if (first.Id > second.Id) return 1;
        return 0;
    }

    internal static List<FixCodeDefinition> OrderCodes(List<FixCodeDefinition> source, bool preserveSourceOrder)
    {
        List<FixCodeDefinition> ordered = new List<FixCodeDefinition>(source);
        if (!preserveSourceOrder)
        {
            for (int index = 1; index < ordered.Count; index++)
            {
                FixCodeDefinition value = ordered[index];
                int position = index;
                while (position > 0 && CompareCodes(ordered[position - 1], value) > 0)
                {
                    ordered[position] = ordered[position - 1];
                    position--;
                }
                ordered[position] = value;
            }
        }
        return ordered;
    }

    internal static int CompareCodes(FixCodeDefinition first, FixCodeDefinition second)
    {
        if (first.SortOrder < second.SortOrder) return -1;
        if (first.SortOrder > second.SortOrder) return 1;
        return string.Compare(first.Value, second.Value, StringComparison.Ordinal);
    }

    internal static List<FixMemberDefinition> OrderMembers(List<FixMemberDefinition> source, bool preserveSourceOrder)
    {
        List<FixMemberDefinition> ordered = new List<FixMemberDefinition>(source);
        if (!preserveSourceOrder)
        {
            for (int index = 1; index < ordered.Count; index++)
            {
                FixMemberDefinition value = ordered[index];
                int position = index;
                while (position > 0 && CompareMembers(ordered[position - 1], value) > 0)
                {
                    ordered[position] = ordered[position - 1];
                    position--;
                }
                ordered[position] = value;
            }
        }
        return ordered;
    }

    internal static int CompareMembers(FixMemberDefinition first, FixMemberDefinition second)
    {
        if (first.Position < second.Position) return -1;
        if (first.Position > second.Position) return 1;
        if ((int)first.Kind < (int)second.Kind) return -1;
        if ((int)first.Kind > (int)second.Kind) return 1;
        if (first.ReferenceId < second.ReferenceId) return -1;
        if (first.ReferenceId > second.ReferenceId) return 1;
        return string.Compare(first.Name, second.Name, StringComparison.Ordinal);
    }

    internal static string FormatInteger(int value)
    {
        return value.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static bool HasWriteErrors(List<DictionaryDiagnostic> diagnostics)
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

    internal static void AddWriteDiagnostic(DictionaryLoadResult result, ValidationSeverity severity, string code, string message, string definitionName, bool hasTag, int tag)
    {
        DictionaryDiagnostic diagnostic = new DictionaryDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.DefinitionName = definitionName;
        diagnostic.HasTag = hasTag;
        diagnostic.Tag = tag;
        result.Diagnostics.Add(diagnostic);
    }
}
