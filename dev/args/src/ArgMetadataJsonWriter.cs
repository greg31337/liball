using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace args;

internal class ArgMetadataJsonWriter
{
    internal string WriteApplication(ArgApp app)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (Utf8JsonWriter writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteNumber("schemaVersion", 1);
                writer.WriteStartObject("application");
                writer.WriteString("name", app.Name);
                writer.WriteString("description", app.DescriptionText);
                writer.WriteString("version", app.GetVersionText());
                writer.WriteEndObject();
                writer.WritePropertyName("command");
                WriteCommand(writer, app.RootCommand);
                writer.WriteEndObject();
                writer.Flush();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    internal string WriteCommandDocument(ArgCommand command)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (Utf8JsonWriter writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteNumber("schemaVersion", 1);
                writer.WritePropertyName("command");
                WriteCommand(writer, command);
                writer.WriteEndObject();
                writer.Flush();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    internal void WriteCommand(Utf8JsonWriter writer, ArgCommand command)
    {
        writer.WriteStartObject();
        writer.WriteString("name", command.Name);
        writer.WritePropertyName("path");
        WriteCommandPath(writer, command);
        writer.WritePropertyName("aliases");
        WriteStringArray(writer, command.Aliases);
        writer.WriteString("summary", command.SummaryText);
        writer.WriteString("description", command.DescriptionText);
        writer.WriteString("usage", command.UsageText);
        writer.WriteString("version", command.VersionText);
        writer.WriteString("group", command.GroupName);
        if (command.DefaultCommandValue == null)
        {
            writer.WriteNull("defaultCommand");
        }
        else
        {
            writer.WriteString("defaultCommand", command.DefaultCommandValue.Name);
        }

        writer.WriteString("deprecated", command.DeprecatedMessage);
        writer.WriteBoolean("hidden", command.IsHidden);
        writer.WriteBoolean("requiresSubcommand", command.RequiresSubcommand);
        writer.WriteBoolean("branch", command.IsBranch);
        writer.WriteBoolean("hasHandler", command.HandlerFunc != null);
        writer.WriteBoolean("hasBeforeHook", command.BeforeFunc != null);
        writer.WriteBoolean("hasAfterHook", command.AfterFunc != null);
        writer.WriteStartArray("globalOptions");
        int index = 0;
        while (index < command.GlobalOptions.Count)
        {
            WriteOption(writer, command.GlobalOptions[index]);
            index = index + 1;
        }

        writer.WriteEndArray();
        writer.WriteStartArray("options");
        index = 0;
        while (index < command.Options.Count)
        {
            WriteOption(writer, command.Options[index]);
            index = index + 1;
        }

        writer.WriteEndArray();
        writer.WriteStartArray("arguments");
        index = 0;
        while (index < command.Arguments.Count)
        {
            WriteArgument(writer, command.Arguments[index], index);
            index = index + 1;
        }

        writer.WriteEndArray();
        writer.WriteStartArray("optionGroups");
        index = 0;
        while (index < command.OptionGroups.Count)
        {
            WriteOptionGroup(writer, command.OptionGroups[index]);
            index = index + 1;
        }

        writer.WriteEndArray();
        writer.WritePropertyName("examples");
        WriteStringArray(writer, command.Examples);
        writer.WriteStartArray("commands");
        index = 0;
        while (index < command.Commands.Count)
        {
            WriteCommand(writer, command.Commands[index]);
            index = index + 1;
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    internal void WriteOption(Utf8JsonWriter writer, ArgOption option)
    {
        writer.WriteStartObject();
        writer.WriteString("name", option.Name);
        writer.WritePropertyName("aliases");
        WriteStringArray(writer, option.Aliases);
        writer.WriteBoolean("global", option.IsGlobal);
        writer.WriteString("description", option.DescriptionText);
        writer.WriteString("valueName", option.ValueName);
        writer.WritePropertyName("arity");
        WriteArity(writer, option.ValueArity);
        writer.WriteBoolean("required", option.IsRequired);
        writer.WriteBoolean("hidden", option.IsHidden);
        writer.WriteBoolean("flag", option.IsFlag);
        writer.WriteBoolean("negatable", option.IsNegatable);
        writer.WriteBoolean("hasValue", option.HasValue);
        writer.WriteBoolean("optionalValue", option.ValueIsOptional);
        writer.WriteBoolean("repeatable", option.IsRepeatable);
        writer.WriteBoolean("count", option.IsCount);
        writer.WriteBoolean("hasDefault", option.HasDefault);
        writer.WriteString("default", option.DefaultValue);
        writer.WriteBoolean("hasFallback", option.HasFallback);
        writer.WriteString("fallback", option.FallbackValue);
        writer.WriteBoolean("hasEnvironment", option.HasEnv);
        writer.WriteString("environment", option.EnvName);
        writer.WritePropertyName("choices");
        WriteStringArray(writer, option.Choices);
        writer.WriteString("choiceValidationStage", ChoiceValidationStageText(option.ChoiceValidationStage));
        writer.WriteBoolean("hasNormalizer", option.ValueNormalizer != null);
        writer.WriteBoolean("hasChoiceConverter", option.ChoiceConverter != null);
        writer.WritePropertyName("completions");
        WriteStringArray(writer, option.Completions);
        writer.WritePropertyName("conflicts");
        WriteStringArray(writer, option.ConflictNames);
        writer.WritePropertyName("requires");
        WriteStringArray(writer, option.RequiredNames);
        writer.WriteStartArray("implications");
        int index = 0;
        while (index < option.Implications.Count)
        {
            WriteImplication(writer, option.Implications[index]);
            index = index + 1;
        }

        writer.WriteEndArray();
        writer.WriteString("deprecated", option.DeprecatedMessage);
        writer.WriteEndObject();
    }

    internal void WriteArgument(Utf8JsonWriter writer, ArgArgument argument, int position)
    {
        writer.WriteStartObject();
        writer.WriteString("name", argument.Name);
        writer.WriteNumber("position", position);
        writer.WriteString("description", argument.DescriptionText);
        writer.WritePropertyName("arity");
        WriteArity(writer, argument.ValueArity);
        writer.WriteBoolean("required", argument.IsRequired);
        writer.WriteBoolean("variadic", argument.IsVariadic);
        writer.WriteBoolean("hasDefault", argument.HasDefault);
        writer.WriteString("default", argument.DefaultValue);
        writer.WriteBoolean("hasEnvironment", argument.HasEnv);
        writer.WriteString("environment", argument.EnvName);
        writer.WritePropertyName("choices");
        WriteStringArray(writer, argument.Choices);
        writer.WriteString("choiceValidationStage", ChoiceValidationStageText(argument.ChoiceValidationStage));
        writer.WriteBoolean("hasNormalizer", argument.ValueNormalizer != null);
        writer.WriteBoolean("hasChoiceConverter", argument.ChoiceConverter != null);
        writer.WritePropertyName("completions");
        WriteStringArray(writer, argument.Completions);
        writer.WriteString("deprecated", argument.DeprecatedMessage);
        writer.WriteEndObject();
    }

    internal void WriteArity(Utf8JsonWriter writer, ArgArity arity)
    {
        writer.WriteStartObject();
        writer.WriteNumber("minimum", arity.Minimum);
        if (arity.Maximum == ArgArity.Unbounded)
        {
            writer.WriteNull("maximum");
            writer.WriteBoolean("unbounded", true);
        }
        else
        {
            writer.WriteNumber("maximum", arity.Maximum);
            writer.WriteBoolean("unbounded", false);
        }

        writer.WriteEndObject();
    }

    internal void WriteOptionGroup(Utf8JsonWriter writer, ArgOptionGroup group)
    {
        writer.WriteStartObject();
        writer.WriteString("name", group.Name);
        writer.WriteBoolean("mutuallyExclusive", group.IsMutuallyExclusive);
        writer.WriteBoolean("atLeastOneRequired", group.IsAtLeastOneRequired);
        writer.WritePropertyName("options");
        WriteStringArray(writer, group.OptionNames);
        writer.WriteEndObject();
    }

    internal void WriteImplication(Utf8JsonWriter writer, ArgOptionImplication implication)
    {
        writer.WriteStartObject();
        writer.WriteString("option", implication.OptionName);
        writer.WriteString("value", implication.Value);
        writer.WriteEndObject();
    }

    internal void WriteCommandPath(Utf8JsonWriter writer, ArgCommand command)
    {
        writer.WriteStartArray();
        WriteCommandPathItems(writer, command);
        writer.WriteEndArray();
    }

    internal void WriteCommandPathItems(Utf8JsonWriter writer, ArgCommand command)
    {
        if (command.Parent != null)
        {
            WriteCommandPathItems(writer, command.Parent);
        }

        writer.WriteStringValue(command.Name);
    }

    internal void WriteStringArray(Utf8JsonWriter writer, List<string> values)
    {
        writer.WriteStartArray();
        int index = 0;
        while (index < values.Count)
        {
            writer.WriteStringValue(values[index]);
            index = index + 1;
        }

        writer.WriteEndArray();
    }

    internal string ChoiceValidationStageText(ArgChoiceValidationStage stage)
    {
        if (stage == ArgChoiceValidationStage.Normalized)
        {
            return "normalized";
        }

        if (stage == ArgChoiceValidationStage.Converted)
        {
            return "converted";
        }

        return "raw";
    }
}
