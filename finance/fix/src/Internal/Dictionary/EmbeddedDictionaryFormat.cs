using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace fix;

internal static class EmbeddedDictionaryFormat
{
    internal static void WriteResource(Stream destination, FixDictionary dictionary)
    {
        using (GZipStream compression = new GZipStream(destination, CompressionLevel.SmallestSize, true))
        using (BinaryWriter writer = new BinaryWriter(compression, Encoding.UTF8, true))
        {
            writer.Write("FIXDICT");
            writer.Write(3);
            WriteIdentity(writer, dictionary.Identity);
            WriteOptions(writer, dictionary.Options);
            WriteDatatypes(writer, dictionary.Datatypes);
            WriteCodeSets(writer, dictionary.CodeSets);
            WriteFields(writer, dictionary.Fields);
            WriteMembers(writer, dictionary.Header);
            WriteMembers(writer, dictionary.Trailer);
            WriteComponents(writer, dictionary.Components);
            WriteGroups(writer, dictionary.Groups);
            WriteMessages(writer, dictionary.Messages);
            WriteScenarios(writer, dictionary.Scenarios);
        }
    }

    internal static FixDictionary ReadResource(Stream source)
    {
        using (GZipStream compression = new GZipStream(source, CompressionMode.Decompress, true))
        using (BinaryReader reader = new BinaryReader(compression, Encoding.UTF8, true))
        {
            string magic = reader.ReadString();
            int version = reader.ReadInt32();
            if (magic != "FIXDICT" || version != 1 && version != 3)
            {
                throw new FixDictionaryException("The embedded dictionary resource has an unsupported format.");
            }
            FixDictionary dictionary = new FixDictionary();
            dictionary.Identity = ReadIdentity(reader);
            dictionary.Options = ReadOptions(reader);
            dictionary.Datatypes = ReadDatatypes(reader);
            dictionary.CodeSets = ReadCodeSets(reader);
            bool hasConditionalMetadata = version >= 3;
            dictionary.Fields = ReadFields(reader, hasConditionalMetadata);
            dictionary.Header = ReadMembers(reader, hasConditionalMetadata);
            dictionary.Trailer = ReadMembers(reader, hasConditionalMetadata);
            dictionary.Components = ReadComponents(reader, hasConditionalMetadata);
            dictionary.Groups = ReadGroups(reader, hasConditionalMetadata);
            dictionary.Messages = ReadMessages(reader, hasConditionalMetadata);
            dictionary.Scenarios = ReadScenarios(reader, hasConditionalMetadata);
            return dictionary;
        }
    }

    internal static void WriteIdentity(BinaryWriter writer, FixDictionaryIdentity identity)
    {
        writer.Write(identity.Name);
        writer.Write(identity.BeginString);
        writer.Write(identity.ApplicationVersion);
        writer.Write(identity.HasExtensionPack);
        writer.Write(identity.ExtensionPack);
        writer.Write(identity.CustomVersion);
        writer.Write(identity.Scenario);
        writer.Write(identity.Source);
    }

    internal static FixDictionaryIdentity ReadIdentity(BinaryReader reader)
    {
        FixDictionaryIdentity identity = new FixDictionaryIdentity();
        identity.Name = reader.ReadString();
        identity.BeginString = reader.ReadString();
        identity.ApplicationVersion = reader.ReadString();
        identity.HasExtensionPack = reader.ReadBoolean();
        identity.ExtensionPack = reader.ReadInt32();
        identity.CustomVersion = reader.ReadString();
        identity.Scenario = reader.ReadString();
        identity.Source = reader.ReadString();
        return identity;
    }

    internal static void WriteOptions(BinaryWriter writer, FixDictionaryOptions options)
    {
        writer.Write(options.MaxDefinitions);
        writer.Write(options.UserDefinedTagMinimum);
        writer.Write(options.AllowDuplicateDefinitions);
        writer.Write(options.AllowUnknownDatatypes);
        writer.Write(options.RequireResolvedReferences);
        writer.Write(options.NormalizeNames);
        writer.Write(options.PreserveSourceOrder);
    }

    internal static FixDictionaryOptions ReadOptions(BinaryReader reader)
    {
        FixDictionaryOptions options = new FixDictionaryOptions();
        options.MaxDefinitions = reader.ReadInt32();
        options.UserDefinedTagMinimum = reader.ReadInt32();
        options.AllowDuplicateDefinitions = reader.ReadBoolean();
        options.AllowUnknownDatatypes = reader.ReadBoolean();
        options.RequireResolvedReferences = reader.ReadBoolean();
        options.NormalizeNames = reader.ReadBoolean();
        options.PreserveSourceOrder = reader.ReadBoolean();
        return options;
    }

    internal static void WriteDatatypes(BinaryWriter writer, List<FixDatatypeDefinition> definitions)
    {
        writer.Write(definitions.Count);
        for (int index = 0; index < definitions.Count; index++)
        {
            FixDatatypeDefinition definition = definitions[index];
            writer.Write(definition.Name);
            writer.Write((int)definition.FieldType);
            writer.Write(definition.BaseTypeName);
            writer.Write(definition.Description);
            writer.Write(definition.Source);
            writer.Write(definition.IsRemoved);
        }
    }

    internal static List<FixDatatypeDefinition> ReadDatatypes(BinaryReader reader)
    {
        int count = ReadDefinitionCount(reader);
        List<FixDatatypeDefinition> definitions = [];
        for (int index = 0; index < count; index++)
        {
            FixDatatypeDefinition definition = new FixDatatypeDefinition();
            definition.Name = reader.ReadString();
            definition.FieldType = (FixFieldType)reader.ReadInt32();
            definition.BaseTypeName = reader.ReadString();
            definition.Description = reader.ReadString();
            definition.Source = reader.ReadString();
            definition.IsRemoved = reader.ReadBoolean();
            definitions.Add(definition);
        }
        return definitions;
    }

    internal static void WriteCodeSets(BinaryWriter writer, List<FixCodeSetDefinition> definitions)
    {
        writer.Write(definitions.Count);
        for (int index = 0; index < definitions.Count; index++)
        {
            FixCodeSetDefinition definition = definitions[index];
            writer.Write(definition.Id);
            writer.Write(definition.Name);
            writer.Write((int)definition.FieldType);
            writer.Write(definition.Description);
            writer.Write(definition.Source);
            writer.Write(definition.IsRemoved);
            WriteCodes(writer, definition.Codes);
        }
    }

    internal static List<FixCodeSetDefinition> ReadCodeSets(BinaryReader reader)
    {
        int count = ReadDefinitionCount(reader);
        List<FixCodeSetDefinition> definitions = [];
        for (int index = 0; index < count; index++)
        {
            FixCodeSetDefinition definition = new FixCodeSetDefinition();
            definition.Id = reader.ReadInt32();
            definition.Name = reader.ReadString();
            definition.FieldType = (FixFieldType)reader.ReadInt32();
            definition.Description = reader.ReadString();
            definition.Source = reader.ReadString();
            definition.IsRemoved = reader.ReadBoolean();
            definition.Codes = ReadCodes(reader);
            definitions.Add(definition);
        }
        return definitions;
    }

    internal static void WriteCodes(BinaryWriter writer, List<FixCodeDefinition> definitions)
    {
        writer.Write(definitions.Count);
        for (int index = 0; index < definitions.Count; index++)
        {
            FixCodeDefinition definition = definitions[index];
            writer.Write(definition.Name);
            writer.Write(definition.Value);
            writer.Write(definition.Id);
            writer.Write(definition.SortOrder);
            writer.Write(definition.Description);
            writer.Write(definition.Source);
            writer.Write(definition.IsRemoved);
        }
    }

    internal static List<FixCodeDefinition> ReadCodes(BinaryReader reader)
    {
        int count = ReadDefinitionCount(reader);
        List<FixCodeDefinition> definitions = [];
        for (int index = 0; index < count; index++)
        {
            FixCodeDefinition definition = new FixCodeDefinition();
            definition.Name = reader.ReadString();
            definition.Value = reader.ReadString();
            definition.Id = reader.ReadInt32();
            definition.SortOrder = reader.ReadInt32();
            definition.Description = reader.ReadString();
            definition.Source = reader.ReadString();
            definition.IsRemoved = reader.ReadBoolean();
            definitions.Add(definition);
        }
        return definitions;
    }

    internal static void WriteFields(BinaryWriter writer, List<FixFieldDefinition> definitions)
    {
        writer.Write(definitions.Count);
        for (int index = 0; index < definitions.Count; index++)
        {
            FixFieldDefinition definition = definitions[index];
            writer.Write(definition.Tag);
            writer.Write(definition.Name);
            writer.Write(definition.Abbreviation);
            writer.Write(definition.DatatypeName);
            writer.Write((int)definition.FieldType);
            writer.Write(definition.CodeSetName);
            writer.Write(definition.HasDiscriminatorTag);
            writer.Write(definition.DiscriminatorTag);
            writer.Write(definition.HasLengthTag);
            writer.Write(definition.LengthTag);
            writer.Write(definition.HasDataTag);
            writer.Write(definition.DataTag);
            writer.Write(definition.Description);
            writer.Write(definition.Source);
            writer.Write(definition.IsRemoved);
        }
    }

    internal static List<FixFieldDefinition> ReadFields(BinaryReader reader, bool hasConditionalMetadata)
    {
        int count = ReadDefinitionCount(reader);
        List<FixFieldDefinition> definitions = [];
        for (int index = 0; index < count; index++)
        {
            FixFieldDefinition definition = new FixFieldDefinition();
            definition.Tag = reader.ReadInt32();
            definition.Name = reader.ReadString();
            definition.Abbreviation = reader.ReadString();
            definition.DatatypeName = reader.ReadString();
            definition.FieldType = (FixFieldType)reader.ReadInt32();
            definition.CodeSetName = reader.ReadString();
            if (hasConditionalMetadata)
            {
                definition.HasDiscriminatorTag = reader.ReadBoolean();
                definition.DiscriminatorTag = reader.ReadInt32();
            }
            definition.HasLengthTag = reader.ReadBoolean();
            definition.LengthTag = reader.ReadInt32();
            definition.HasDataTag = reader.ReadBoolean();
            definition.DataTag = reader.ReadInt32();
            definition.Description = reader.ReadString();
            definition.Source = reader.ReadString();
            definition.IsRemoved = reader.ReadBoolean();
            definitions.Add(definition);
        }
        return definitions;
    }

    internal static void WriteMembers(BinaryWriter writer, List<FixMemberDefinition> definitions)
    {
        writer.Write(definitions.Count);
        for (int index = 0; index < definitions.Count; index++)
        {
            FixMemberDefinition definition = definitions[index];
            writer.Write(definition.Name);
            writer.Write((int)definition.Kind);
            writer.Write(definition.ReferenceId);
            writer.Write(definition.Position);
            writer.Write((int)definition.Presence);
            writer.Write(definition.Scenario);
            writer.Write(definition.PresenceRule);
            writer.Write((int)definition.RulePresence);
            writer.Write(definition.Source);
            writer.Write(definition.IsRemoved);
        }
    }

    internal static List<FixMemberDefinition> ReadMembers(BinaryReader reader, bool hasConditionalMetadata)
    {
        int count = ReadDefinitionCount(reader);
        List<FixMemberDefinition> definitions = [];
        for (int index = 0; index < count; index++)
        {
            FixMemberDefinition definition = new FixMemberDefinition();
            definition.Name = reader.ReadString();
            definition.Kind = (FixMemberKind)reader.ReadInt32();
            definition.ReferenceId = reader.ReadInt32();
            definition.Position = reader.ReadInt32();
            definition.Presence = (FixPresence)reader.ReadInt32();
            definition.Scenario = reader.ReadString();
            definition.PresenceRule = reader.ReadString();
            if (hasConditionalMetadata)
            {
                definition.RulePresence = (FixPresence)reader.ReadInt32();
            }
            definition.Source = reader.ReadString();
            definition.IsRemoved = reader.ReadBoolean();
            definitions.Add(definition);
        }
        return definitions;
    }

    internal static void WriteComponents(BinaryWriter writer, List<FixComponentDefinition> definitions)
    {
        writer.Write(definitions.Count);
        for (int index = 0; index < definitions.Count; index++)
        {
            FixComponentDefinition definition = definitions[index];
            writer.Write(definition.Id);
            writer.Write(definition.Name);
            writer.Write(definition.Abbreviation);
            writer.Write(definition.Description);
            writer.Write(definition.Source);
            writer.Write(definition.IsRemoved);
            WriteMembers(writer, definition.Members);
        }
    }

    internal static List<FixComponentDefinition> ReadComponents(BinaryReader reader, bool hasConditionalMetadata)
    {
        int count = ReadDefinitionCount(reader);
        List<FixComponentDefinition> definitions = [];
        for (int index = 0; index < count; index++)
        {
            FixComponentDefinition definition = new FixComponentDefinition();
            definition.Id = reader.ReadInt32();
            definition.Name = reader.ReadString();
            definition.Abbreviation = reader.ReadString();
            definition.Description = reader.ReadString();
            definition.Source = reader.ReadString();
            definition.IsRemoved = reader.ReadBoolean();
            definition.Members = ReadMembers(reader, hasConditionalMetadata);
            definitions.Add(definition);
        }
        return definitions;
    }

    internal static void WriteGroups(BinaryWriter writer, List<FixGroupDefinition> definitions)
    {
        writer.Write(definitions.Count);
        for (int index = 0; index < definitions.Count; index++)
        {
            FixGroupDefinition definition = definitions[index];
            writer.Write(definition.Id);
            writer.Write(definition.Name);
            writer.Write(definition.CountTag);
            writer.Write(definition.DelimiterTag);
            writer.Write(definition.Description);
            writer.Write(definition.Source);
            writer.Write(definition.IsRemoved);
            WriteMembers(writer, definition.Members);
        }
    }

    internal static List<FixGroupDefinition> ReadGroups(BinaryReader reader, bool hasConditionalMetadata)
    {
        int count = ReadDefinitionCount(reader);
        List<FixGroupDefinition> definitions = [];
        for (int index = 0; index < count; index++)
        {
            FixGroupDefinition definition = new FixGroupDefinition();
            definition.Id = reader.ReadInt32();
            definition.Name = reader.ReadString();
            definition.CountTag = reader.ReadInt32();
            definition.DelimiterTag = reader.ReadInt32();
            definition.Description = reader.ReadString();
            definition.Source = reader.ReadString();
            definition.IsRemoved = reader.ReadBoolean();
            definition.Members = ReadMembers(reader, hasConditionalMetadata);
            definitions.Add(definition);
        }
        return definitions;
    }

    internal static void WriteMessages(BinaryWriter writer, List<FixMessageDefinition> definitions)
    {
        writer.Write(definitions.Count);
        for (int index = 0; index < definitions.Count; index++)
        {
            FixMessageDefinition definition = definitions[index];
            writer.Write(definition.Id);
            writer.Write(definition.Name);
            writer.Write(definition.MsgType);
            writer.Write(definition.Category);
            writer.Write(definition.Scenario);
            writer.Write(definition.Description);
            writer.Write(definition.Source);
            writer.Write(definition.IsRemoved);
            WriteMembers(writer, definition.Members);
        }
    }

    internal static List<FixMessageDefinition> ReadMessages(BinaryReader reader, bool hasConditionalMetadata)
    {
        int count = ReadDefinitionCount(reader);
        List<FixMessageDefinition> definitions = [];
        for (int index = 0; index < count; index++)
        {
            FixMessageDefinition definition = new FixMessageDefinition();
            definition.Id = reader.ReadInt32();
            definition.Name = reader.ReadString();
            definition.MsgType = reader.ReadString();
            definition.Category = reader.ReadString();
            definition.Scenario = reader.ReadString();
            definition.Description = reader.ReadString();
            definition.Source = reader.ReadString();
            definition.IsRemoved = reader.ReadBoolean();
            definition.Members = ReadMembers(reader, hasConditionalMetadata);
            definitions.Add(definition);
        }
        return definitions;
    }

    internal static void WriteScenarios(BinaryWriter writer, List<FixScenarioDefinition> definitions)
    {
        writer.Write(definitions.Count);
        for (int index = 0; index < definitions.Count; index++)
        {
            FixScenarioDefinition definition = definitions[index];
            writer.Write(definition.Name);
            writer.Write(definition.ParentName);
            writer.Write(definition.Description);
            writer.Write(definition.Source);
            writer.Write(definition.IsRemoved);
            WriteMembers(writer, definition.Members);
            WriteRules(writer, definition.Rules);
        }
    }

    internal static List<FixScenarioDefinition> ReadScenarios(BinaryReader reader, bool hasConditionalMetadata)
    {
        int count = ReadDefinitionCount(reader);
        List<FixScenarioDefinition> definitions = [];
        for (int index = 0; index < count; index++)
        {
            FixScenarioDefinition definition = new FixScenarioDefinition();
            definition.Name = reader.ReadString();
            definition.ParentName = reader.ReadString();
            definition.Description = reader.ReadString();
            definition.Source = reader.ReadString();
            definition.IsRemoved = reader.ReadBoolean();
            definition.Members = ReadMembers(reader, hasConditionalMetadata);
            definition.Rules = ReadRules(reader);
            definitions.Add(definition);
        }
        return definitions;
    }

    internal static void WriteRules(BinaryWriter writer, List<string> rules)
    {
        writer.Write(rules.Count);
        for (int index = 0; index < rules.Count; index++)
        {
            writer.Write(rules[index]);
        }
    }

    internal static List<string> ReadRules(BinaryReader reader)
    {
        int count = ReadDefinitionCount(reader);
        List<string> rules = [];
        for (int index = 0; index < count; index++)
        {
            rules.Add(reader.ReadString());
        }
        return rules;
    }

    internal static int ReadDefinitionCount(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        if (count < 0 || count > FixDefaults.MaxDictionaryDefinitions)
        {
            throw new FixDictionaryException("The embedded dictionary resource contains an invalid definition count.");
        }
        return count;
    }
}
