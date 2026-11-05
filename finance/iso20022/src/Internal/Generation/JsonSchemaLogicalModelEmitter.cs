using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace iso20022;

internal static class JsonSchemaLogicalModelEmitter
{
    internal const string DraftIdentifier = "https://json-schema.org/draft/2020-12/schema";
    internal const string DurationPattern = "^-?P((([0-9]+Y([0-9]+M)?([0-9]+D)?|([0-9]+M)([0-9]+D)?|([0-9]+D))(T(([0-9]+H)([0-9]+M)?([0-9]+([.][0-9]+)?S)?|([0-9]+M)([0-9]+([.][0-9]+)?S)?|([0-9]+([.][0-9]+)?S)))?)|(T(([0-9]+H)([0-9]+M)?([0-9]+([.][0-9]+)?S)?|([0-9]+M)([0-9]+([.][0-9]+)?S)?|([0-9]+([.][0-9]+)?S))))$";
    internal const string DateTimePattern = "^-?([1-9][0-9]{3,}|0[0-9]{3})-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])T(([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]([.][0-9]+)?|(24:00:00([.]0+)?))(Z|[+-]((0[0-9]|1[0-3]):[0-5][0-9]|14:00))?$";
    internal const string NormalisedDateTimePattern = "^-?([1-9][0-9]{3,}|0[0-9]{3})-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])T(([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]([.][0-9]+)?|(24:00:00([.]0+)?))Z$";
    internal const string DatePattern = "^-?([1-9][0-9]{3,}|0[0-9]{3})-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])(Z|([+-]((0[0-9]|1[0-3]):[0-5][0-9]|14:00)))?$";
    internal const string TimePattern = "^(([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]([.][0-9]+)?|(24:00:00([.]0+)?))(Z|([+-]((0[0-9]|1[0-3]):[0-5][0-9]|14:00)))?$";
    internal const string YearMonthPattern = "^-?([1-9][0-9]{3,}|0[0-9]{3})-(0[1-9]|1[0-2])(Z|([+-]((0[0-9]|1[0-3]):[0-5][0-9]|14:00)))?$";
    internal const string MonthDayPattern = "^--(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])(Z|([+-]((0[0-9]|1[0-3]):[0-5][0-9]|14:00)))?$";
    internal const string YearPattern = "^-?([1-9][0-9]{3,}|0[0-9]{3})(Z|([+-]((0[0-9]|1[0-3]):[0-5][0-9]|14:00)))?$";
    internal const string MonthPattern = "^--(0[1-9]|1[0-2])(Z|([+-]((0[0-9]|1[0-3]):[0-5][0-9]|14:00)))?$";
    internal const string DayPattern = "^---(0[1-9]|[12][0-9]|3[01])(Z|([+-]((0[0-9]|1[0-3]):[0-5][0-9]|14:00)))?$";

    internal static byte[] EmitJsonSchema(XsdGenerationContext context)
    {
        JsonWriterOptions settings = new JsonWriterOptions();
        settings.Indented = true;
        byte[] raw;
        using (MemoryStream output = new MemoryStream())
        {
            using (Utf8JsonWriter writer = new Utf8JsonWriter(output, settings))
            {
                WriteRootSchema(writer, context);
                writer.Flush();
            }
            raw = output.ToArray();
        }
        return ApplyJsonTextStyle(raw, context.Options.Indentation, context.Options.NewLine);
    }

    internal static byte[] ApplyJsonTextStyle(
        byte[] raw,
        string indentation,
        string newLine)
    {
        string text = Encoding.UTF8.GetString(raw);
        string[] lines = text.Split('\n');
        StringBuilder styled = new StringBuilder();
        int lineIndex = 0;
        while (lineIndex < lines.Length)
        {
            string line = lines[lineIndex];
            int spaces = 0;
            while (spaces < line.Length && line[spaces] == ' ')
            {
                spaces++;
            }
            int level = spaces / 2;
            int levelIndex = 0;
            while (levelIndex < level)
            {
                styled.Append(indentation);
                levelIndex++;
            }
            styled.Append(line.Substring(spaces));
            if (lineIndex + 1 < lines.Length)
            {
                styled.Append(newLine);
            }
            lineIndex++;
        }
        return Encoding.UTF8.GetBytes(styled.ToString());
    }

    internal static void WriteRootSchema(Utf8JsonWriter writer, XsdGenerationContext context)
    {
        MessageDefinitionMetadata? primary = FindPrimaryMessage(context);
        string rootElement = FindRootElementName(primary);
        string rootAnchor = "_" + rootElement;
        writer.WriteStartObject();
        writer.WriteString("$schema", DraftIdentifier);
        writer.WriteString("$id", FindJsonSchemaIdentifier(context));
        writer.WriteString("description", FindRootDescription(context, primary));
        writer.WriteString("type", "object");
        writer.WriteBoolean("additionalProperties", false);
        writer.WriteStartArray("required");
        writer.WriteStringValue(rootElement);
        writer.WriteEndArray();
        writer.WriteStartObject("properties");
        writer.WriteStartObject(rootElement);
        writer.WriteString("$ref", "#" + rootAnchor);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteStartObject("$defs");
        WriteRootElementDefinition(writer, context, primary, rootElement, rootAnchor);
        WriteMessageDefinitions(writer, context);
        WriteNamedDefinitions(writer, context);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    internal static MessageDefinitionMetadata? FindPrimaryMessage(XsdGenerationContext context)
    {
        if (context.Unit.Message != null)
        {
            return context.Unit.Message;
        }
        if (context.Messages.Count > 0)
        {
            return context.Messages[0];
        }
        return null;
    }

    internal static string FindRootElementName(MessageDefinitionMetadata? message)
    {
        if (message != null && message.RootElement.Length > 0)
        {
            return XsdLogicalModelEmitter.SanitizeXsdName(
                message.RootElement,
                message.Identifier,
                GenerationConceptKind.MessageDefinition);
        }
        return "Document";
    }

    internal static string FindJsonSchemaIdentifier(XsdGenerationContext context)
    {
        MessageDefinitionMetadata? message = FindPrimaryMessage(context);
        if (message != null)
        {
            XsdGenerator helper = new XsdGenerator();
            string identifier = helper.FindIdentifierValue(message.MessageIdentifier);
            if (identifier != "...")
            {
                return "urn:iso:std:iso:20022:tech:json:" + identifier;
            }
        }
        if (context.Unit.Schema != null && context.Unit.Schema.MessageIdentifier != null)
        {
            XsdGenerator helper = new XsdGenerator();
            string identifier = helper.FindIdentifierValue(
                context.Unit.Schema.MessageIdentifier);
            if (identifier != "...")
            {
                return "urn:iso:std:iso:20022:tech:json:" + identifier;
            }
        }
        string source = context.Unit.NamespaceUri;
        if (source.Length > 0)
        {
            string converted = source.Replace(":tech:xsd:", ":tech:json:",
                StringComparison.Ordinal);
            if (converted != source)
            {
                return converted;
            }
        }
        return "urn:iso:std:iso:20022:tech:json:generated-" +
            GenerationStableHash.CreateHex(context.Unit.Identity).Substring(0, 24);
    }

    internal static string FindRootDescription(
        XsdGenerationContext context,
        MessageDefinitionMetadata? message)
    {
        string description = "Generated from ISO 20022";
        if (context.Options.ArtifactEdition.Length > 0)
        {
            description += " repository " + context.Options.ArtifactEdition;
        }
        description += " by iso20022.";
        if (context.Options.IncludeDocumentation && message != null &&
            message.Definition.Length > 0)
        {
            description += "\n\n" + message.Definition;
        }
        return description;
    }

    internal static void WriteRootElementDefinition(
        Utf8JsonWriter writer,
        XsdGenerationContext context,
        MessageDefinitionMetadata? message,
        string rootElement,
        string rootAnchor)
    {
        string messageName = "MessageDefinition";
        string abbreviated = "Message";
        if (message != null)
        {
            messageName = FindMessageDefinitionName(message);
            abbreviated = FindAbbreviatedMessageName(message);
        }
        writer.WriteStartObject(rootAnchor);
        writer.WriteString("type", "object");
        writer.WriteBoolean("additionalProperties", false);
        writer.WriteStartArray("required");
        writer.WriteStringValue(abbreviated);
        writer.WriteEndArray();
        if (context.Options.IncludeDocumentation)
        {
            writer.WriteString("description", "Root element for " + messageName + ".");
        }
        writer.WriteStartObject("properties");
        writer.WriteStartObject(abbreviated);
        writer.WriteString("$ref", "#" + messageName);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteString("$anchor", rootAnchor);
        writer.WriteEndObject();
    }

    internal static string FindMessageDefinitionName(MessageDefinitionMetadata message)
    {
        string candidate = message.Name;
        if (candidate.Length == 0)
        {
            candidate = message.XmlName;
        }
        return XsdLogicalModelEmitter.SanitizeXsdName(
            candidate,
            message.Identifier,
            GenerationConceptKind.MessageDefinition);
    }

    internal static string FindAbbreviatedMessageName(MessageDefinitionMetadata message)
    {
        if (message.XmlTag.Length > 0)
        {
            return XsdLogicalModelEmitter.SanitizeXsdName(
                message.XmlTag,
                message.Identifier,
                GenerationConceptKind.MessageDefinition);
        }
        string name = FindMessageDefinitionName(message);
        int marker = name.LastIndexOf('V');
        if (marker > 0 && marker + 1 < name.Length)
        {
            int index = marker + 1;
            bool digits = true;
            while (index < name.Length)
            {
                if (name[index] < '0' || name[index] > '9')
                {
                    digits = false;
                    break;
                }
                index++;
            }
            if (digits)
            {
                return name.Substring(0, marker);
            }
        }
        return name;
    }

    internal static void WriteMessageDefinitions(
        Utf8JsonWriter writer,
        XsdGenerationContext context)
    {
        int index = 0;
        while (index < context.Messages.Count)
        {
            MessageDefinitionMetadata message = context.Messages[index];
            string name = FindMessageDefinitionName(message);
            writer.WriteStartObject(name);
            WriteObjectSchemaHeader(writer, context, message.Definition);
            WriteRequiredBuildingBlocks(writer, context, message);
            writer.WriteStartObject("properties");
            List<RepositoryReference> buildingBlocks = CreateOrderedReferences(
                message.BuildingBlocks);
            int blockIndex = 0;
            while (blockIndex < buildingBlocks.Count)
            {
                MessageBuildingBlockDefinition? block = XsdLogicalModelEmitter.FindBuildingBlock(
                    context.BuildingBlocks,
                    buildingBlocks[blockIndex].TargetIdentifier);
                if (block != null)
                {
                    WriteBuildingBlockProperty(writer, context, block);
                }
                blockIndex++;
            }
            writer.WriteEndObject();
            WriteLegacyBuildingBlockConstraints(writer, context, message);
            writer.WriteString("$anchor", name);
            writer.WriteEndObject();
            index++;
        }
    }

    internal static void WriteObjectSchemaHeader(
        Utf8JsonWriter writer,
        XsdGenerationContext context,
        string definition)
    {
        if (context.Options.IncludeDocumentation && definition.Length > 0)
        {
            writer.WriteString("description", definition);
        }
        writer.WriteString("type", "object");
        writer.WriteBoolean("additionalProperties", false);
    }

    internal static void WriteRequiredBuildingBlocks(
        Utf8JsonWriter writer,
        XsdGenerationContext context,
        MessageDefinitionMetadata message)
    {
        List<string> required = new List<string>();
        List<RepositoryReference> buildingBlocks = CreateOrderedReferences(
            message.BuildingBlocks);
        int index = 0;
        while (index < buildingBlocks.Count)
        {
            MessageBuildingBlockDefinition? block = XsdLogicalModelEmitter.FindBuildingBlock(
                context.BuildingBlocks,
                buildingBlocks[index].TargetIdentifier);
            if (block != null && block.Multiplicity.MinimumOccurs > 0)
            {
                required.Add(FindMemberName(block.XmlTag, block.Name, block.Identifier));
            }
            index++;
        }
        WriteRequiredNames(writer, required);
    }

    internal static void WriteRequiredNames(Utf8JsonWriter writer, List<string> required)
    {
        if (required.Count == 0)
        {
            return;
        }
        writer.WriteStartArray("required");
        int index = 0;
        while (index < required.Count)
        {
            writer.WriteStringValue(required[index]);
            index++;
        }
        writer.WriteEndArray();
    }

    internal static void WriteBuildingBlockProperty(
        Utf8JsonWriter writer,
        XsdGenerationContext context,
        MessageBuildingBlockDefinition block)
    {
        string name = FindMemberName(block.XmlTag, block.Name, block.Identifier);
        writer.WriteStartObject(name);
        RepositoryReference reference = block.MemberType;
        if (block.SimpleType != null)
        {
            reference = block.SimpleType;
        }
        else if (block.ComplexType != null)
        {
            reference = block.ComplexType;
        }
        WriteOccurrenceSchema(writer, context, reference, false, block.Multiplicity,
            block.Identifier);
        WriteMemberDescription(writer, context, block.Name, block.Definition);
        writer.WriteEndObject();
    }

    internal static string FindMemberName(string xmlTag, string name, string identifier)
    {
        return XsdLogicalModelEmitter.FindElementName(xmlTag, name, identifier);
    }

    internal static void WriteMemberDescription(
        Utf8JsonWriter writer,
        XsdGenerationContext context,
        string name,
        string definition)
    {
        if (!context.Options.IncludeDocumentation)
        {
            return;
        }
        if (name.Length > 0 && definition.Length > 0)
        {
            writer.WriteString("description", name + " \n" + definition);
        }
        else if (definition.Length > 0)
        {
            writer.WriteString("description", definition);
        }
        else if (name.Length > 0)
        {
            writer.WriteString("description", name);
        }
    }

    internal static void WriteOccurrenceSchema(
        Utf8JsonWriter writer,
        XsdGenerationContext context,
        RepositoryReference reference,
        bool forceString,
        MultiplicityDefinition multiplicity,
        string ownerIdentifier)
    {
        if (multiplicity.HasMaximumOccurs && multiplicity.MaximumOccurs <= 1)
        {
            WriteReferenceSchema(writer, context, reference, forceString, ownerIdentifier);
            return;
        }
        writer.WriteStartArray("anyOf");
        writer.WriteStartObject();
        WriteReferenceSchema(writer, context, reference, forceString, ownerIdentifier);
        writer.WriteEndObject();
        writer.WriteStartObject();
        writer.WriteString("type", "array");
        int minimum = multiplicity.MinimumOccurs;
        if (minimum < 1)
        {
            minimum = 1;
        }
        writer.WriteNumber("minItems", minimum);
        if (multiplicity.HasMaximumOccurs)
        {
            writer.WriteNumber("maxItems", multiplicity.MaximumOccurs);
        }
        writer.WriteStartObject("items");
        WriteReferenceSchema(writer, context, reference, forceString, ownerIdentifier);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteEndArray();
    }

    internal static void WriteReferenceSchema(
        Utf8JsonWriter writer,
        XsdGenerationContext context,
        RepositoryReference reference,
        bool forceString,
        string ownerIdentifier)
    {
        if (forceString)
        {
            writer.WriteString("type", "string");
            return;
        }
        string target = reference.TargetIdentifier;
        if (target.Length == 0)
        {
            target = reference.RawValue;
        }
        string localName = XsdLogicalModelEmitter.FindQualifiedLocalName(target);
        string prefix = XsdLogicalModelEmitter.FindQualifiedPrefix(target);
        if (prefix == "xs" || prefix == "xsd" ||
            XsdLogicalModelEmitter.BuiltInSchemaTypeExists(localName))
        {
            WriteBuiltInSchema(writer, localName);
            return;
        }
        XsdGenerationName? name = XsdLogicalModelEmitter.FindTypeGenerationName(
            context.Names,
            target,
            localName,
            reference.TargetKind);
        if (name != null)
        {
            writer.WriteString("$ref", "#" + name.Name);
            return;
        }
        XsdLogicalModelEmitter.AddGenerationDiagnostic(
            context,
            "generation.json_schema.type_fallback",
            "A member type could not be resolved and was generated as a string.",
            ownerIdentifier);
        writer.WriteString("type", "string");
    }

    internal static void WriteBuiltInSchema(Utf8JsonWriter writer, string localName)
    {
        if (localName == "boolean")
        {
            WriteBooleanLexicalSchema(writer);
        }
        else if (localName == "decimal" || localName == "integer" ||
            localName == "positiveInteger" || localName == "nonNegativeInteger" ||
            localName == "negativeInteger" || localName == "nonPositiveInteger")
        {
            writer.WriteString("type", "string");
            writer.WriteString("pattern", "^[+-]?(([0-9]+([.][0-9]*)?)|([.][0-9]+))$");
        }
        else if (localName == "dateTime")
        {
            WriteStringPatternSchema(writer, DateTimePattern);
        }
        else if (localName == "date")
        {
            WriteStringPatternSchema(writer, DatePattern);
        }
        else if (localName == "time")
        {
            WriteStringPatternSchema(writer, TimePattern);
        }
        else if (localName == "duration")
        {
            WriteStringPatternSchema(writer, DurationPattern);
        }
        else if (localName == "gYear")
        {
            WriteStringPatternSchema(writer, YearPattern);
        }
        else if (localName == "gYearMonth")
        {
            WriteStringPatternSchema(writer, YearMonthPattern);
        }
        else if (localName == "gMonth")
        {
            WriteStringPatternSchema(writer, MonthPattern);
        }
        else if (localName == "gMonthDay")
        {
            WriteStringPatternSchema(writer, MonthDayPattern);
        }
        else if (localName == "gDay")
        {
            WriteStringPatternSchema(writer, DayPattern);
        }
        else
        {
            writer.WriteString("type", "string");
        }
    }

    internal static void WriteBooleanLexicalSchema(Utf8JsonWriter writer)
    {
        writer.WriteString("type", "string");
        writer.WriteStartArray("enum");
        writer.WriteStringValue("1");
        writer.WriteStringValue("0");
        writer.WriteStringValue("true");
        writer.WriteStringValue("false");
        writer.WriteEndArray();
    }

    internal static void WriteStringPatternSchema(Utf8JsonWriter writer, string pattern)
    {
        writer.WriteString("type", "string");
        writer.WriteString("pattern", pattern);
    }

    internal static void WriteNamedDefinitions(
        Utf8JsonWriter writer,
        XsdGenerationContext context)
    {
        int index = 0;
        while (index < context.Names.Count)
        {
            context.Options.CancellationToken.ThrowIfCancellationRequested();
            XsdGenerationName name = context.Names[index];
            if (name.Kind == GenerationConceptKind.MessageComponent)
            {
                MessageComponentDefinition? component =
                    XsdLogicalModelEmitter.FindMessageComponent(
                        context.Components,
                        name.Identifier);
                if (component != null)
                {
                    WriteComponentDefinition(writer, context, component, name.Name);
                }
            }
            else if (name.Kind == GenerationConceptKind.Choice)
            {
                ChoiceDefinition? choice = XsdLogicalModelEmitter.FindChoiceDefinition(
                    context.Choices,
                    name.Identifier);
                if (choice != null && choice.MessageElements.Count > 0)
                {
                    WriteChoiceDefinition(writer, context, choice, name.Name);
                }
            }
            else if (name.Kind == GenerationConceptKind.DataType)
            {
                DataTypeDefinition? dataType = XsdLogicalModelEmitter.FindDataType(
                    context.DataTypes,
                    name.Identifier);
                if (dataType != null)
                {
                    WriteDataTypeDefinition(writer, context, dataType, name.Name);
                }
            }
            else if (name.Kind == GenerationConceptKind.CodeSet)
            {
                CodeSetDefinition? codeSet = XsdLogicalModelEmitter.FindCodeSet(
                    context.CodeSets,
                    name.Identifier);
                if (codeSet != null)
                {
                    WriteCodeSetDefinition(writer, context, codeSet, name.Name);
                }
            }
            else if (name.Kind == GenerationConceptKind.ExternalSchema)
            {
                ExternalSchemaDefinition? external = XsdLogicalModelEmitter.FindExternalSchema(
                    context.ExternalSchemas,
                    name.Identifier);
                if (external != null)
                {
                    WriteExternalDefinition(writer, context, external, name.Name);
                }
            }
            index++;
        }
    }

    internal static void WriteComponentDefinition(
        Utf8JsonWriter writer,
        XsdGenerationContext context,
        MessageComponentDefinition component,
        string name)
    {
        writer.WriteStartObject(name);
        WriteObjectSchemaHeader(writer, context, component.Definition);
        List<RepositoryReference> messageElements = CreateOrderedReferences(
            component.MessageElements);
        List<string> required = CollectRequiredElementNames(context, messageElements);
        if (required.Count == 0)
        {
            writer.WriteNumber("minProperties", 1);
        }
        else
        {
            WriteRequiredNames(writer, required);
        }
        writer.WriteStartObject("properties");
        int index = 0;
        while (index < messageElements.Count)
        {
            MessageElementDefinition? element = XsdLogicalModelEmitter.FindMessageElement(
                context.Elements,
                messageElements[index].TargetIdentifier);
            if (element != null)
            {
                WriteMessageElementProperty(writer, context, element);
            }
            index++;
        }
        writer.WriteEndObject();
        WriteLegacyElementConstraints(writer, context, component);
        writer.WriteString("$anchor", name);
        writer.WriteEndObject();
    }

    internal static List<string> CollectRequiredElementNames(
        XsdGenerationContext context,
        List<RepositoryReference> references)
    {
        List<string> required = new List<string>();
        int index = 0;
        while (index < references.Count)
        {
            MessageElementDefinition? element = XsdLogicalModelEmitter.FindMessageElement(
                context.Elements,
                references[index].TargetIdentifier);
            if (element != null && element.Multiplicity.MinimumOccurs > 0)
            {
                required.Add(FindMemberName(element.XmlTag, element.Name, element.Identifier));
            }
            index++;
        }
        return required;
    }

    internal static void WriteMessageElementProperty(
        Utf8JsonWriter writer,
        XsdGenerationContext context,
        MessageElementDefinition element)
    {
        string name = FindMemberName(element.XmlTag, element.Name, element.Identifier);
        writer.WriteStartObject(name);
        RepositoryReference reference = element.MemberType;
        if (element.SimpleType != null)
        {
            reference = element.SimpleType;
        }
        else if (element.ComplexType != null)
        {
            reference = element.ComplexType;
        }
        bool forceString = element.Kind == MessageElementKind.AssociationEnd &&
            !element.IsComposite;
        WriteOccurrenceSchema(writer, context, reference, forceString,
            element.Multiplicity, element.Identifier);
        WriteMemberDescription(writer, context, element.Name, element.Definition);
        writer.WriteEndObject();
    }

    internal static void WriteChoiceDefinition(
        Utf8JsonWriter writer,
        XsdGenerationContext context,
        ChoiceDefinition choice,
        string name)
    {
        writer.WriteStartObject(name);
        if (context.Options.IncludeDocumentation && choice.Definition.Length > 0)
        {
            writer.WriteString("description", choice.Definition);
        }
        writer.WriteString("type", "object");
        writer.WriteBoolean("additionalProperties", false);
        writer.WriteNumber("minProperties", 1);
        writer.WriteNumber("maxProperties", 1);
        writer.WriteStartObject("properties");
        List<RepositoryReference> messageElements = CreateOrderedReferences(
            choice.MessageElements);
        int index = 0;
        while (index < messageElements.Count)
        {
            MessageElementDefinition? element = XsdLogicalModelEmitter.FindMessageElement(
                context.Elements,
                messageElements[index].TargetIdentifier);
            if (element != null)
            {
                WriteMessageElementProperty(writer, context, element);
            }
            index++;
        }
        writer.WriteEndObject();
        writer.WriteString("$anchor", name);
        writer.WriteEndObject();
    }

    internal static List<RepositoryReference> CreateOrderedReferences(
        List<RepositoryReference> source)
    {
        List<RepositoryReference> values = new List<RepositoryReference>();
        values.AddRange(source);
        values.Sort((left, right) =>
        {
            int comparison = left.Position.CompareTo(right.Position);
            if (comparison != 0)
            {
                return comparison;
            }
            comparison = string.Compare(
                left.TargetIdentifier,
                right.TargetIdentifier,
                StringComparison.Ordinal);
            if (comparison != 0)
            {
                return comparison;
            }
            comparison = string.Compare(
                left.RawValue,
                right.RawValue,
                StringComparison.Ordinal);
            if (comparison != 0)
            {
                return comparison;
            }
            return ((int)left.TargetKind).CompareTo((int)right.TargetKind);
        });
        return values;
    }

    internal static void WriteLegacyElementConstraints(
        Utf8JsonWriter writer,
        XsdGenerationContext context,
        MessageComponentDefinition component)
    {
        List<ChoiceDefinition> choices = new List<ChoiceDefinition>();
        int index = 0;
        while (index < component.Choices.Count)
        {
            ChoiceDefinition? choice = XsdLogicalModelEmitter.FindChoiceDefinition(
                context.Choices,
                component.Choices[index].TargetIdentifier);
            if (choice != null && choice.ImpactedMessageElements.Count > 1)
            {
                choices.Add(choice);
            }
            index++;
        }
        if (choices.Count == 0)
        {
            return;
        }
        writer.WriteStartArray("allOf");
        index = 0;
        while (index < choices.Count)
        {
            WriteAtMostOneElementConstraint(writer, context, choices[index]);
            index++;
        }
        writer.WriteEndArray();
    }

    internal static void WriteAtMostOneElementConstraint(
        Utf8JsonWriter writer,
        XsdGenerationContext context,
        ChoiceDefinition choice)
    {
        int left = 0;
        while (left < choice.ImpactedMessageElements.Count)
        {
            int right = left + 1;
            while (right < choice.ImpactedMessageElements.Count)
            {
                MessageElementDefinition? first = XsdLogicalModelEmitter.FindMessageElement(
                    context.Elements,
                    choice.ImpactedMessageElements[left].TargetIdentifier);
                MessageElementDefinition? second = XsdLogicalModelEmitter.FindMessageElement(
                    context.Elements,
                    choice.ImpactedMessageElements[right].TargetIdentifier);
                if (first != null && second != null)
                {
                    writer.WriteStartObject();
                    writer.WriteStartObject("not");
                    writer.WriteStartArray("required");
                    writer.WriteStringValue(FindMemberName(
                        first.XmlTag, first.Name, first.Identifier));
                    writer.WriteStringValue(FindMemberName(
                        second.XmlTag, second.Name, second.Identifier));
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }
                right++;
            }
            left++;
        }
    }

    internal static void WriteLegacyBuildingBlockConstraints(
        Utf8JsonWriter writer,
        XsdGenerationContext context,
        MessageDefinitionMetadata message)
    {
        List<ChoiceDefinition> choices = new List<ChoiceDefinition>();
        int index = 0;
        while (index < message.Choices.Count)
        {
            ChoiceDefinition? choice = XsdLogicalModelEmitter.FindChoiceDefinition(
                context.Choices,
                message.Choices[index].TargetIdentifier);
            if (choice != null && choice.ImpactedBuildingBlocks.Count > 1)
            {
                choices.Add(choice);
            }
            index++;
        }
        if (choices.Count == 0)
        {
            return;
        }
        writer.WriteStartArray("allOf");
        index = 0;
        while (index < choices.Count)
        {
            ChoiceDefinition choice = choices[index];
            int left = 0;
            while (left < choice.ImpactedBuildingBlocks.Count)
            {
                int right = left + 1;
                while (right < choice.ImpactedBuildingBlocks.Count)
                {
                    MessageBuildingBlockDefinition? first =
                        XsdLogicalModelEmitter.FindBuildingBlock(
                            context.BuildingBlocks,
                            choice.ImpactedBuildingBlocks[left].TargetIdentifier);
                    MessageBuildingBlockDefinition? second =
                        XsdLogicalModelEmitter.FindBuildingBlock(
                            context.BuildingBlocks,
                            choice.ImpactedBuildingBlocks[right].TargetIdentifier);
                    if (first != null && second != null)
                    {
                        writer.WriteStartObject();
                        writer.WriteStartObject("not");
                        writer.WriteStartArray("required");
                        writer.WriteStringValue(FindMemberName(
                            first.XmlTag, first.Name, first.Identifier));
                        writer.WriteStringValue(FindMemberName(
                            second.XmlTag, second.Name, second.Identifier));
                        writer.WriteEndArray();
                        writer.WriteEndObject();
                        writer.WriteEndObject();
                    }
                    right++;
                }
                left++;
            }
            index++;
        }
        writer.WriteEndArray();
    }

    internal static void WriteDataTypeDefinition(
        Utf8JsonWriter writer,
        XsdGenerationContext context,
        DataTypeDefinition dataType,
        string name)
    {
        writer.WriteStartObject(name);
        if (dataType.Kind == DataTypeKind.Boolean || dataType.Kind == DataTypeKind.Indicator)
        {
            WriteBooleanLexicalSchema(writer);
        }
        else if (dataType.Kind == DataTypeKind.Amount &&
            dataType.CurrencyIdentifierSet != null)
        {
            WriteCurrencyAmountSchema(writer, context, dataType);
        }
        else if (dataType.Kind == DataTypeKind.Decimal ||
            dataType.Kind == DataTypeKind.Quantity ||
            dataType.Kind == DataTypeKind.Rate ||
            dataType.Kind == DataTypeKind.Amount)
        {
            WriteStringPatternSchema(writer, FindDecimalPattern(dataType));
        }
        else if (IsTemporalKind(dataType.Kind))
        {
            WriteStringPatternSchema(writer, FindDataTypeTemporalPattern(dataType));
        }
        else if (dataType.Kind == DataTypeKind.Schema)
        {
            WriteBuiltInSchema(writer,
                XsdLogicalModelEmitter.FindQualifiedLocalName(dataType.SchemaTypeName));
            WriteStringFacets(writer, dataType.Facets, false);
        }
        else
        {
            writer.WriteString("type", "string");
            WriteStringFacets(writer, dataType.Facets, true);
        }
        if (context.Options.IncludeDocumentation && dataType.Definition.Length > 0)
        {
            writer.WriteString("description", dataType.Definition);
        }
        writer.WriteString("$anchor", name);
        writer.WriteEndObject();
    }

    internal static bool IsTemporalKind(DataTypeKind kind)
    {
        return kind == DataTypeKind.Duration || kind == DataTypeKind.DateTime ||
            kind == DataTypeKind.Date || kind == DataTypeKind.Time ||
            kind == DataTypeKind.Year || kind == DataTypeKind.YearMonth ||
            kind == DataTypeKind.Month || kind == DataTypeKind.MonthDay ||
            kind == DataTypeKind.Day;
    }

    internal static string FindTemporalPattern(DataTypeKind kind)
    {
        if (kind == DataTypeKind.Duration)
        {
            return DurationPattern;
        }
        if (kind == DataTypeKind.DateTime)
        {
            return DateTimePattern;
        }
        if (kind == DataTypeKind.Date)
        {
            return DatePattern;
        }
        if (kind == DataTypeKind.Time)
        {
            return TimePattern;
        }
        if (kind == DataTypeKind.Year)
        {
            return YearPattern;
        }
        if (kind == DataTypeKind.YearMonth)
        {
            return YearMonthPattern;
        }
        if (kind == DataTypeKind.Month)
        {
            return MonthPattern;
        }
        if (kind == DataTypeKind.MonthDay)
        {
            return MonthDayPattern;
        }
        return DayPattern;
    }

    internal static string FindDataTypeTemporalPattern(DataTypeDefinition dataType)
    {
        if (dataType.Kind == DataTypeKind.DateTime &&
            (dataType.SourceTypeName.Contains("Normalised", StringComparison.Ordinal) ||
            dataType.SourceTypeName.Contains("Normalized", StringComparison.Ordinal) ||
            dataType.Name.Contains("Normalised", StringComparison.Ordinal) ||
            dataType.Name.Contains("Normalized", StringComparison.Ordinal)))
        {
            return NormalisedDateTimePattern;
        }
        return FindTemporalPattern(dataType.Kind);
    }

    internal static void WriteCurrencyAmountSchema(
        Utf8JsonWriter writer,
        XsdGenerationContext context,
        DataTypeDefinition dataType)
    {
        writer.WriteString("type", "object");
        writer.WriteStartArray("required");
        writer.WriteStringValue("amt");
        writer.WriteStringValue("Ccy");
        writer.WriteEndArray();
        writer.WriteStartObject("properties");
        writer.WriteStartObject("amt");
        WriteStringPatternSchema(writer, FindDecimalPattern(dataType));
        writer.WriteEndObject();
        writer.WriteStartObject("Ccy");
        RepositoryReference currency = new RepositoryReference();
        if (dataType.CurrencyIdentifierSet != null)
        {
            currency = dataType.CurrencyIdentifierSet;
        }
        WriteReferenceSchema(writer, context, currency, false, dataType.Identifier);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteBoolean("additionalProperties", false);
    }

    internal static string FindDecimalPattern(DataTypeDefinition dataType)
    {
        string explicitPattern = FindFacetValue(dataType.Facets, "pattern");
        if (explicitPattern.Length > 0)
        {
            return explicitPattern;
        }
        int totalDigits = FindFacetInteger(dataType.Facets, "totalDigits");
        int fractionDigits = FindFacetInteger(dataType.Facets, "fractionDigits");
        string sign = FindDecimalSign(dataType.Facets);
        if (totalDigits > 0)
        {
            string integer = "(0|[1-9][0-9]{0," + (totalDigits - 1).ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "})([.]0*)?";
            if (fractionDigits > 0)
            {
                string fraction = "[.][0-9]{1," + fractionDigits.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) + "}0*";
                string real = "(?=[1-9.][0-9.]{1," + totalDigits.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) +
                    "}0*$)([1-9][0-9]*)?[.][0-9]{0," + fractionDigits.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) + "}0*";
                return "^" + sign + "0*((" + integer + ")|(" + fraction + ")|(" +
                    real + "))$";
            }
            if (fractionDigits == 0)
            {
                return "^" + sign + "0*((" + integer + ")|[.][0]+)$";
            }
            string anyFraction = "[.][0-9]{1," + totalDigits.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "}0*";
            string anyReal = "(?=[1-9.][0-9.]{1," + totalDigits.ToString(
                System.Globalization.CultureInfo.InvariantCulture) +
                "}0*$)([1-9][0-9]*)?[.][0-9]*0*";
            return "^" + sign + "0*((" + integer + ")|(" + anyFraction + ")|(" +
                anyReal + "))$";
        }
        if (fractionDigits > 0)
        {
            string limit = fractionDigits.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            return "^" + sign + "([0-9]+([.][0-9]{0," + limit +
                "}0*)?|0*[.][0-9]{1," + limit + "}0*)$";
        }
        if (fractionDigits == 0)
        {
            return "^" + sign + "0*((0|[1-9][0-9]*)([.]0*)?|[.][0]+)$";
        }
        return "^" + sign + "(([0-9]+[.]?)|(0*[.][0-9]+)|((?=[0-9.]{2,})[0-9]*[.][0-9]*))$";
    }

    internal static string FindDecimalSign(List<DataTypeFacet> facets)
    {
        string minimum = FindFacetValue(facets, "minInclusive");
        if (minimum.Length == 0)
        {
            minimum = FindFacetValue(facets, "minExclusive");
        }
        if (minimum.Length > 0 && minimum[0] != '-')
        {
            return "[+]?";
        }
        string maximum = FindFacetValue(facets, "maxInclusive");
        if (maximum.Length == 0)
        {
            maximum = FindFacetValue(facets, "maxExclusive");
        }
        if (maximum.Length > 0 && maximum[0] == '-')
        {
            return "-";
        }
        return "[+-]?";
    }

    internal static string FindFacetValue(List<DataTypeFacet> facets, string name)
    {
        int index = 0;
        while (index < facets.Count)
        {
            if (facets[index].Name == name)
            {
                return facets[index].Value;
            }
            index++;
        }
        return string.Empty;
    }

    internal static int FindFacetInteger(List<DataTypeFacet> facets, string name)
    {
        int index = 0;
        while (index < facets.Count)
        {
            DataTypeFacet facet = facets[index];
            if (facet.Name == name)
            {
                if (facet.HasIntegerValue)
                {
                    return facet.IntegerValue;
                }
                return ParseFacetInteger(facet.Value);
            }
            index++;
        }
        return -1;
    }

    internal static int ParseFacetInteger(string value)
    {
        if (value.Length == 0)
        {
            return -1;
        }
        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < '0' || current > '9')
            {
                return -1;
            }
            if (result > (int.MaxValue - (current - '0')) / 10)
            {
                return -1;
            }
            result = (result * 10) + current - '0';
            index++;
        }
        return result;
    }

    internal static void WriteStringFacets(
        Utf8JsonWriter writer,
        List<DataTypeFacet> source,
        bool includePattern)
    {
        List<DataTypeFacet> facets = new List<DataTypeFacet>();
        facets.AddRange(source);
        XsdLogicalModelEmitter.SortDataTypeFacets(facets);
        int index = 0;
        while (index < facets.Count)
        {
            DataTypeFacet facet = facets[index];
            if (facet.Name == "length")
            {
                int value = FindFacetInteger(facets, "length");
                if (value >= 0)
                {
                    writer.WriteNumber("minLength", value);
                    writer.WriteNumber("maxLength", value);
                }
            }
            else if (facet.Name == "minLength")
            {
                int value = FindFacetInteger(facets, "minLength");
                if (value >= 0)
                {
                    writer.WriteNumber("minLength", value);
                }
            }
            else if (facet.Name == "maxLength")
            {
                int value = FindFacetInteger(facets, "maxLength");
                if (value >= 0)
                {
                    writer.WriteNumber("maxLength", value);
                }
            }
            else if (facet.Name == "pattern" && includePattern)
            {
                writer.WriteString("pattern", facet.Value);
            }
            else if (facet.Name == "enumeration")
            {
                WriteFacetEnumeration(writer, facet);
            }
            index++;
        }
    }

    internal static void WriteFacetEnumeration(Utf8JsonWriter writer, DataTypeFacet facet)
    {
        List<string> values = new List<string>();
        values.AddRange(facet.EnumerationValues);
        XsdLogicalModelEmitter.SortFacetValues(values);
        writer.WriteStartArray("enum");
        int index = 0;
        while (index < values.Count)
        {
            writer.WriteStringValue(values[index]);
            index++;
        }
        writer.WriteEndArray();
    }

    internal static void WriteCodeSetDefinition(
        Utf8JsonWriter writer,
        XsdGenerationContext context,
        CodeSetDefinition codeSet,
        string name)
    {
        writer.WriteStartObject(name);
        writer.WriteString("type", "string");
        string pattern = FindFacetValue(codeSet.Facets, "pattern");
        if (pattern.Length > 0)
        {
            writer.WriteString("pattern", pattern);
        }
        if (codeSet.Codes.Count > 0)
        {
            List<CodeValueDefinition> codes = new List<CodeValueDefinition>();
            codes.AddRange(codeSet.Codes);
            XsdLogicalModelEmitter.SortCodeValues(codes);
            writer.WriteStartArray("enum");
            int index = 0;
            while (index < codes.Count)
            {
                writer.WriteStringValue(codes[index].Value);
                index++;
            }
            writer.WriteEndArray();
        }
        else
        {
            WriteStringFacets(writer, codeSet.Facets, false);
        }
        if (context.Options.IncludeDocumentation && codeSet.Definition.Length > 0)
        {
            writer.WriteString("description", codeSet.Definition);
        }
        writer.WriteString("$anchor", name);
        writer.WriteEndObject();
    }

    internal static void WriteExternalDefinition(
        Utf8JsonWriter writer,
        XsdGenerationContext context,
        ExternalSchemaDefinition external,
        string name)
    {
        writer.WriteStartObject(name);
        writer.WriteString("$anchor", name);
        if (context.Options.IncludeDocumentation && external.Definition.Length > 0)
        {
            writer.WriteString("description", external.Definition);
        }
        writer.WriteEndObject();
    }
}
