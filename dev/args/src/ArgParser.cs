using System.Collections.Generic;

namespace args;

internal class ArgParser
{
    internal ArgCommand Command;
    internal ArgCommand CurrentCommand;
    internal ArgParserSettings Settings;
    internal ArgTokenizer Tokenizer;
    internal bool StopParsing;
    internal ArgOption DefaultHelpOption;
    internal ArgOption DefaultVersionOption;
    internal string ResolvedVersionText;

    internal ArgParser(ArgCommand command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }

        Command = command;
        CurrentCommand = command;
        Settings = new ArgParserSettings();
        Tokenizer = ArgTokenizer.WithSettings(Settings);
        StopParsing = false;
        DefaultHelpOption = CreateDefaultHelpOption();
        DefaultVersionOption = CreateDefaultVersionOption();
        ResolvedVersionText = ArgVersionText.ResolveCommand(command);
    }

    internal static ArgParser WithSettings(ArgCommand command, ArgParserSettings settings)
    {
        if (settings == null)
        {
            throw new System.ArgumentNullException("settings");
        }

        ArgParser parser = new ArgParser(command);
        parser.Settings = settings;
        parser.Tokenizer = ArgTokenizer.WithSettings(settings);
        parser.DefaultHelpOption = parser.CreateDefaultHelpOption();
        parser.DefaultVersionOption = parser.CreateDefaultVersionOption();
        return parser;
    }

    internal ArgOption CreateDefaultHelpOption()
    {
        ArgOption option = ArgOption.Empty();
        if (Settings.HelpAliases.Count > 0)
        {
            string alias = Settings.HelpAliases[0];
            ArgName.ValidateOptionAlias(alias);
            option.Name = alias;
            option.Aliases.Add(alias);
            int index = 1;
            while (index < Settings.HelpAliases.Count)
            {
                option.Alias(Settings.HelpAliases[index]);
                index = index + 1;
            }
        }

        option.Flag();
        option.Description(ArgMessageLookup.Get(Settings, ArgMessageKeys.HelpOptionDescription, new string[] { }));
        return option;
    }

    internal ArgOption CreateDefaultVersionOption()
    {
        ArgOption option = new ArgOption("--version");
        option.Alias("-V");
        option.Flag();
        option.Description(ArgMessageLookup.Get(Settings, ArgMessageKeys.VersionOptionDescription, new string[] { }));
        return option;
    }

    internal ArgParseResult Parse(string[] argv)
    {
        List<ArgToken> tokens = Tokenizer.Tokenize(argv);
        ArgParseResult result = new ArgParseResult(Command, tokens);
        result.Settings = Settings;
        result.VersionText = ResolvedVersionText;
        AddCommandDeprecation(result, Command, -1);
        ResolveCommandPath(result, tokens);
        CurrentCommand = result.Command;
        StopParsing = false;
        int index = 0;
        while (index < tokens.Count && !StopParsing)
        {
            ArgToken token = tokens[index];
            if (token.Consumed)
            {
                index = index + 1;
                continue;
            }

            if (token.Kind == ArgTokenKind.LongOption)
            {
                index = ParseLongOption(result, tokens, index);
            }
            else if (token.Kind == ArgTokenKind.ShortOption)
            {
                index = ParseShortOption(result, tokens, index);
            }
            else if (token.Kind == ArgTokenKind.SlashOption)
            {
                index = ParseSlashOption(result, tokens, index);
            }
            else if (token.Kind == ArgTokenKind.EndOfOptions)
            {
                token.MarkConsumed();
                index = index + 1;
            }
            else
            {
                if (Settings.OptionsStopAtFirstPositional())
                {
                    AddRemainingPositionals(result, tokens, index);
                    index = tokens.Count;
                }
                else
                {
                    result.Positionals.Add(token);
                    index = index + 1;
                }
            }
        }

        AssignPositionalArguments(result);
        ApplyOptionImplications(result);
        ApplyEnvironmentValues(result);
        ApplyDefaultValues(result);
        AddOccurrenceDeprecations(result);
        return result;
    }

    internal void AddCommandDeprecation(ArgParseResult result, ArgCommand command, int tokenIndex)
    {
        if (command.DeprecatedMessage.Length == 0)
        {
            return;
        }

        result.AddDiagnostic("deprecated_command", ArgSymbolKind.Command, command.DeprecatedMessage, tokenIndex, command.Name);
    }

    internal void AddOccurrenceDeprecations(ArgParseResult result)
    {
        int index = 0;
        while (index < result.Occurrences.Count)
        {
            ArgOccurrence occurrence = result.Occurrences[index];
            if (occurrence.Option != null && OptionOccurrenceUsesDeprecatedSymbol(occurrence))
            {
                AddOptionDeprecation(result, occurrence);
            }
            else if (occurrence.Argument != null && occurrence.TokenIndex >= 0)
            {
                AddArgumentDeprecation(result, occurrence);
            }

            index = index + 1;
        }
    }

    internal bool OptionOccurrenceUsesDeprecatedSymbol(ArgOccurrence occurrence)
    {
        if (occurrence.TokenIndex >= 0)
        {
            return true;
        }

        return occurrence.Source == ArgValueSource.Implied;
    }

    internal void AddOptionDeprecation(ArgParseResult result, ArgOccurrence occurrence)
    {
        if (occurrence.Option == null || occurrence.Option.DeprecatedMessage.Length == 0)
        {
            return;
        }

        if (HasDeprecationDiagnostic(result, ArgSymbolKind.Option, occurrence.Option.Name))
        {
            return;
        }

        result.AddDiagnostic("deprecated_option", ArgSymbolKind.Option, occurrence.Option.DeprecatedMessage, occurrence.TokenIndex, occurrence.Option.Name);
    }

    internal void AddArgumentDeprecation(ArgParseResult result, ArgOccurrence occurrence)
    {
        if (occurrence.Argument == null || occurrence.Argument.DeprecatedMessage.Length == 0)
        {
            return;
        }

        if (HasDeprecationDiagnostic(result, ArgSymbolKind.Argument, occurrence.Argument.Name))
        {
            return;
        }

        result.AddDiagnostic("deprecated_argument", ArgSymbolKind.Argument, occurrence.Argument.DeprecatedMessage, occurrence.TokenIndex, occurrence.Argument.Name);
    }

    internal bool HasDeprecationDiagnostic(ArgParseResult result, ArgSymbolKind symbolKind, string symbolName)
    {
        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            ArgDiagnostic diagnostic = result.Diagnostics[index];
            if (diagnostic.SymbolKind == symbolKind && diagnostic.SymbolName == symbolName)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void ApplyOptionImplications(ArgParseResult result)
    {
        int occurrenceIndex = 0;
        while (occurrenceIndex < result.Occurrences.Count)
        {
            ArgOccurrence occurrence = result.Occurrences[occurrenceIndex];
            if (occurrence.Option != null && OccurrenceCanImply(occurrence))
            {
                ApplyOptionImplicationsFrom(result, occurrence.Option);
            }

            occurrenceIndex = occurrenceIndex + 1;
        }
    }

    internal bool OccurrenceCanImply(ArgOccurrence occurrence)
    {
        if (occurrence.Source == ArgValueSource.CommandLine)
        {
            return true;
        }

        if (occurrence.Source == ArgValueSource.Fallback)
        {
            return true;
        }

        return occurrence.Source == ArgValueSource.Implied;
    }

    internal void ApplyOptionImplicationsFrom(ArgParseResult result, ArgOption source)
    {
        int index = 0;
        while (index < source.Implications.Count)
        {
            ArgOptionImplication implication = source.Implications[index];
            ArgOption? target = FindConfiguredOption(implication.OptionName);
            if (target != null && !HasOptionOccurrence(result, target))
            {
                result.AddImpliedOption(target, implication.Value);
            }

            index = index + 1;
        }
    }

    internal ArgOption? FindConfiguredOption(string name)
    {
        ArgOption? local = FindLocalOption(CurrentCommand, name);
        if (local != null)
        {
            return local;
        }

        return FindInheritedGlobalOption(CurrentCommand, name);
    }

    internal void AddRemainingPositionals(ArgParseResult result, List<ArgToken> tokens, int startIndex)
    {
        int index = startIndex;
        while (index < tokens.Count)
        {
            ArgToken token = tokens[index];
            if (!token.Consumed)
            {
                result.Positionals.Add(token);
            }

            index = index + 1;
        }
    }

    internal void ApplyEnvironmentValues(ArgParseResult result)
    {
        ApplyOptionEnvironmentValues(result, result.Command);
        ApplyInheritedGlobalOptionEnvironmentValues(result, result.Command);
        ApplyArgumentEnvironmentValues(result);
    }

    internal void ApplyOptionEnvironmentValues(ArgParseResult result, ArgCommand command)
    {
        int index = 0;
        while (index < command.Options.Count)
        {
            ArgOption option = command.Options[index];
            ApplyOptionEnvironmentValue(result, option);
            index = index + 1;
        }
    }

    internal void ApplyInheritedGlobalOptionEnvironmentValues(ArgParseResult result, ArgCommand command)
    {
        ArgCommand? current = command;
        while (current != null)
        {
            int index = 0;
            while (index < current.GlobalOptions.Count)
            {
                ArgOption option = current.GlobalOptions[index];
                ApplyOptionEnvironmentValue(result, option);
                index = index + 1;
            }

            current = current.Parent;
        }
    }

    internal void ApplyOptionEnvironmentValue(ArgParseResult result, ArgOption option)
    {
        if (!option.HasEnv)
        {
            return;
        }

        if (HasOptionOccurrence(result, option))
        {
            return;
        }

        string? value = Settings.Environment.GetEnvironmentVariable(option.EnvName);
        if (value == null)
        {
            return;
        }

        ArgOccurrence occurrence = new ArgOccurrence();
        occurrence.SymbolKind = ArgSymbolKind.Option;
        occurrence.SymbolName = option.Name;
        occurrence.Alias = "";
        occurrence.TokenIndex = -1;
        occurrence.Option = option;
        occurrence.Source = ArgValueSource.Environment;
        occurrence.Values.Add(value);
        result.Occurrences.Add(occurrence);
    }

    internal void ApplyArgumentEnvironmentValues(ArgParseResult result)
    {
        int index = 0;
        while (index < result.Command.Arguments.Count)
        {
            ArgArgument argument = result.Command.Arguments[index];
            if (argument.HasEnv && !HasArgumentOccurrence(result, argument))
            {
                string? value = Settings.Environment.GetEnvironmentVariable(argument.EnvName);
                if (value != null)
                {
                    ArgOccurrence occurrence = new ArgOccurrence();
                    occurrence.SymbolKind = ArgSymbolKind.Argument;
                    occurrence.SymbolName = argument.Name;
                    occurrence.Alias = "";
                    occurrence.TokenIndex = -1;
                    occurrence.Argument = argument;
                    occurrence.Source = ArgValueSource.Environment;
                    occurrence.Values.Add(value);
                    result.Occurrences.Add(occurrence);
                }
            }

            index = index + 1;
        }
    }

    internal void ApplyDefaultValues(ArgParseResult result)
    {
        ApplyOptionDefaults(result, result.Command);
        ApplyInheritedGlobalOptionDefaults(result, result.Command);
        ApplyArgumentDefaults(result);
    }

    internal void ApplyOptionDefaults(ArgParseResult result, ArgCommand command)
    {
        int index = 0;
        while (index < command.Options.Count)
        {
            ArgOption option = command.Options[index];
            ApplyOptionDefault(result, option);
            index = index + 1;
        }
    }

    internal void ApplyInheritedGlobalOptionDefaults(ArgParseResult result, ArgCommand command)
    {
        ArgCommand? current = command;
        while (current != null)
        {
            int index = 0;
            while (index < current.GlobalOptions.Count)
            {
                ArgOption option = current.GlobalOptions[index];
                ApplyOptionDefault(result, option);
                index = index + 1;
            }

            current = current.Parent;
        }
    }

    internal void ApplyOptionDefault(ArgParseResult result, ArgOption option)
    {
        if (!option.HasDefault)
        {
            return;
        }

        if (HasOptionOccurrence(result, option))
        {
            return;
        }

        ArgOccurrence occurrence = new ArgOccurrence();
        occurrence.SymbolKind = ArgSymbolKind.Option;
        occurrence.SymbolName = option.Name;
        occurrence.Alias = "";
        occurrence.TokenIndex = -1;
        occurrence.Option = option;
        occurrence.Source = ArgValueSource.Default;
        occurrence.Values.Add(option.DefaultValue);
        ApplyFallbackValue(occurrence, option);
        result.Occurrences.Add(occurrence);
    }

    internal void ApplyArgumentDefaults(ArgParseResult result)
    {
        int index = 0;
        while (index < result.Command.Arguments.Count)
        {
            ArgArgument argument = result.Command.Arguments[index];
            if (argument.HasDefault && !HasArgumentOccurrence(result, argument))
            {
                ArgOccurrence occurrence = new ArgOccurrence();
                occurrence.SymbolKind = ArgSymbolKind.Argument;
                occurrence.SymbolName = argument.Name;
                occurrence.Alias = "";
                occurrence.TokenIndex = -1;
                occurrence.Argument = argument;
                occurrence.Source = ArgValueSource.Default;
                occurrence.Values.Add(argument.DefaultValue);
                result.Occurrences.Add(occurrence);
            }

            index = index + 1;
        }
    }

    internal bool HasOptionOccurrence(ArgParseResult result, ArgOption option)
    {
        int index = 0;
        while (index < result.Occurrences.Count)
        {
            ArgOccurrence occurrence = result.Occurrences[index];
            if (occurrence.Option == option)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasArgumentOccurrence(ArgParseResult result, ArgArgument argument)
    {
        int index = 0;
        while (index < result.Occurrences.Count)
        {
            ArgOccurrence occurrence = result.Occurrences[index];
            if (occurrence.Argument == argument)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AssignPositionalArguments(ArgParseResult result)
    {
        List<ArgToken> remaining = new List<ArgToken>();
        int argumentIndex = 0;
        int positionalIndex = 0;
        while (positionalIndex < result.Positionals.Count)
        {
            ArgToken token = result.Positionals[positionalIndex];
            if (argumentIndex < result.Command.Arguments.Count)
            {
                ArgArgument argument = result.Command.Arguments[argumentIndex];
                ArgOccurrence occurrence = new ArgOccurrence();
                occurrence.SymbolKind = ArgSymbolKind.Argument;
                occurrence.SymbolName = argument.Name;
                occurrence.Alias = "";
                occurrence.TokenIndex = token.Index;
                occurrence.Argument = argument;
                occurrence.Source = ArgValueSource.CommandLine;
                if (ArgumentIsVariadic(argument))
                {
                    while (positionalIndex < result.Positionals.Count)
                    {
                        ArgToken variadicToken = result.Positionals[positionalIndex];
                        occurrence.Values.Add(variadicToken.RawText);
                        variadicToken.MarkConsumed();
                        positionalIndex = positionalIndex + 1;
                    }

                    result.Occurrences.Add(occurrence);
                    argumentIndex = argumentIndex + 1;
                    continue;
                }

                occurrence.Values.Add(token.RawText);
                result.Occurrences.Add(occurrence);
                token.MarkConsumed();
                argumentIndex = argumentIndex + 1;
            }
            else
            {
                remaining.Add(token);
            }

            positionalIndex = positionalIndex + 1;
        }

        result.Positionals = remaining;
    }

    internal bool ArgumentIsVariadic(ArgArgument argument)
    {
        if (argument.IsVariadic)
        {
            return true;
        }

        if (argument.ValueArity.Maximum == ArgArity.Unbounded)
        {
            return true;
        }

        return false;
    }

    internal void ResolveCommandPath(ArgParseResult result, List<ArgToken> tokens)
    {
        if (TryResolveHelpCommandPath(result, tokens))
        {
            return;
        }

        ArgCommand current = Command;
        int index = 0;
        while (index < tokens.Count)
        {
            ArgToken token = tokens[index];
            if (token.Kind != ArgTokenKind.Positional)
            {
                break;
            }

            ArgCommand? next = FindSubcommand(current, token.RawText);
            if (next == null)
            {
                break;
            }

            token.MarkConsumed();
            current = next;
            result.Command = current;
            result.CommandPath.Add(current);
            AddCommandDeprecation(result, current, token.Index);
            index = index + 1;
        }

        ResolveDefaultCommandPath(result, current);
    }

    internal void ResolveDefaultCommandPath(ArgParseResult result, ArgCommand command)
    {
        ArgCommand current = command;
        while (current.DefaultCommandValue != null)
        {
            current = current.DefaultCommandValue;
            result.Command = current;
            result.CommandPath.Add(current);
            AddCommandDeprecation(result, current, -1);
        }
    }

    internal bool TryResolveHelpCommandPath(ArgParseResult result, List<ArgToken> tokens)
    {
        if (tokens.Count == 0)
        {
            return false;
        }

        ArgToken token = tokens[0];
        if (token.Kind != ArgTokenKind.Positional)
        {
            return false;
        }

        if (!IsHelpCommandName(token.RawText))
        {
            return false;
        }

        if (FindSubcommand(Command, token.RawText) != null)
        {
            return false;
        }

        token.MarkConsumed();
        result.HelpCommandRequested = true;
        result.HelpCommandTokenIndex = token.Index;
        ResolveHelpCommandTargetPath(result, tokens, 1);
        return true;
    }

    internal void ResolveHelpCommandTargetPath(ArgParseResult result, List<ArgToken> tokens, int startIndex)
    {
        ArgCommand current = Command;
        int index = startIndex;
        while (index < tokens.Count)
        {
            ArgToken token = tokens[index];
            if (token.Kind != ArgTokenKind.Positional)
            {
                return;
            }

            ArgCommand? next = FindSubcommand(current, token.RawText);
            if (next == null)
            {
                return;
            }

            token.MarkConsumed();
            current = next;
            result.Command = current;
            result.CommandPath.Add(current);
            AddCommandDeprecation(result, current, token.Index);
            index = index + 1;
        }
    }

    internal bool IsHelpCommandName(string name)
    {
        return NamesEqual(ArgMessageLookup.Get(Settings, ArgMessageKeys.HelpCommandName, new string[] { }), name, Settings.CommandMatchingCaseInsensitive());
    }

    internal ArgCommand? FindSubcommand(ArgCommand command, string name)
    {
        int index = 0;
        while (index < command.Commands.Count)
        {
            ArgCommand child = command.Commands[index];
            if (CommandMatchesNameOrAlias(child, name))
            {
                return child;
            }

            index = index + 1;
        }

        return null;
    }

    internal bool CommandMatchesNameOrAlias(ArgCommand command, string name)
    {
        if (NamesEqual(command.Name, name, Settings.CommandMatchingCaseInsensitive()))
        {
            return true;
        }

        int index = 0;
        while (index < command.Aliases.Count)
        {
            if (NamesEqual(command.Aliases[index], name, Settings.CommandMatchingCaseInsensitive()))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool NamesEqual(string left, string right, bool ignoreCase)
    {
        if (ignoreCase)
        {
            return string.Equals(left, right, System.StringComparison.OrdinalIgnoreCase);
        }

        return left == right;
    }

    internal int ParseLongOption(ArgParseResult result, List<ArgToken> tokens, int index)
    {
        ArgToken token = tokens[index];
        string alias = token.RawText;
        string inlineValue = "";
        bool hasInlineValue = false;
        int equalsIndex = token.RawText.IndexOf('=');
        if (equalsIndex >= 0)
        {
            alias = token.RawText.Substring(0, equalsIndex);
            inlineValue = token.RawText.Substring(equalsIndex + 1);
            hasInlineValue = true;
        }

        token.MarkConsumed();
        ArgOption? option = FindOption(alias);
        if (option == null)
        {
            AddUnknownOption(result, tokens, index, token, alias);
            return index + 1;
        }

        ArgOccurrence occurrence = new ArgOccurrence();
        occurrence.SymbolKind = ArgSymbolKind.Option;
        occurrence.SymbolName = option.Name;
        occurrence.Alias = alias;
        occurrence.TokenIndex = token.Index;
        occurrence.Option = option;
        occurrence.Source = ArgValueSource.CommandLine;
        ApplyOptionCount(occurrence, option);

        if (IsNegatedOptionAlias(option, alias))
        {
            if (hasInlineValue)
            {
                result.AddOptionError("unexpected_value", ArgMessageLookup.Get(Settings, ArgMessageKeys.UnexpectedOptionValue, new string[] { alias }), token.Index, alias);
            }
            else
            {
                occurrence.Values.Add("false");
            }
        }
        else if (OptionAcceptsValue(option))
        {
            if (hasInlineValue)
            {
                occurrence.Values.Add(inlineValue);
                AddSeparateOptionValues(result, occurrence, tokens, index);
            }
            else
            {
                AddSeparateOptionValues(result, occurrence, tokens, index);
            }
        }
        else
        {
            if (hasInlineValue)
            {
                result.AddOptionError("unexpected_value", ArgMessageLookup.Get(Settings, ArgMessageKeys.UnexpectedOptionValue, new string[] { alias }), token.Index, alias);
            }
        }

        ApplyFallbackValue(occurrence, option);
        result.Occurrences.Add(occurrence);
        return index + 1;
    }

    internal int ParseShortOption(ArgParseResult result, List<ArgToken> tokens, int index)
    {
        ArgToken token = tokens[index];
        string alias = token.RawText;
        string attachedValue = "";
        bool hasAttachedValue = false;
        if (token.RawText.Length > 2)
        {
            if (Settings.ShortClustersEnabled())
            {
                return ParseShortCluster(result, tokens, index);
            }

            if (Settings.AttachedShortValuesEnabled())
            {
                alias = token.RawText.Substring(0, 2);
                attachedValue = token.RawText.Substring(2);
                hasAttachedValue = true;
            }
            else
            {
                token.MarkConsumed();
                AddUnknownOption(result, tokens, index, token, alias);
                return index + 1;
            }
        }

        token.MarkConsumed();
        ArgOption? option = FindOption(alias);
        if (option == null)
        {
            AddUnknownOption(result, tokens, index, token, alias);
            return index + 1;
        }

        ArgOccurrence occurrence = new ArgOccurrence();
        occurrence.SymbolKind = ArgSymbolKind.Option;
        occurrence.SymbolName = option.Name;
        occurrence.Alias = alias;
        occurrence.TokenIndex = token.Index;
        occurrence.Option = option;
        occurrence.Source = ArgValueSource.CommandLine;
        ApplyOptionCount(occurrence, option);

        if (OptionAcceptsValue(option))
        {
            if (hasAttachedValue)
            {
                occurrence.Values.Add(attachedValue);
                AddSeparateOptionValues(result, occurrence, tokens, index);
            }
            else
            {
                AddSeparateOptionValues(result, occurrence, tokens, index);
            }
        }
        else
        {
            if (hasAttachedValue)
            {
                result.AddOptionError("unexpected_value", ArgMessageLookup.Get(Settings, ArgMessageKeys.UnexpectedOptionValue, new string[] { alias }), token.Index, alias);
            }
        }

        ApplyFallbackValue(occurrence, option);
        result.Occurrences.Add(occurrence);
        return index + 1;
    }

    internal int ParseSlashOption(ArgParseResult result, List<ArgToken> tokens, int index)
    {
        ArgToken token = tokens[index];
        string alias = token.RawText;
        string inlineValue = "";
        bool hasInlineValue = false;
        int colonIndex = token.RawText.IndexOf(':');
        if (colonIndex >= 0)
        {
            alias = token.RawText.Substring(0, colonIndex);
            inlineValue = token.RawText.Substring(colonIndex + 1);
            hasInlineValue = true;
        }

        token.MarkConsumed();
        ArgOption? option = FindOption(alias);
        if (option == null)
        {
            AddUnknownOption(result, tokens, index, token, alias);
            return index + 1;
        }

        ArgOccurrence occurrence = new ArgOccurrence();
        occurrence.SymbolKind = ArgSymbolKind.Option;
        occurrence.SymbolName = option.Name;
        occurrence.Alias = alias;
        occurrence.TokenIndex = token.Index;
        occurrence.Option = option;
        occurrence.Source = ArgValueSource.CommandLine;
        ApplyOptionCount(occurrence, option);

        if (OptionAcceptsValue(option))
        {
            if (hasInlineValue)
            {
                occurrence.Values.Add(inlineValue);
                AddSeparateOptionValues(result, occurrence, tokens, index);
            }
            else
            {
                AddSeparateOptionValues(result, occurrence, tokens, index);
            }
        }
        else
        {
            if (hasInlineValue)
            {
                result.AddOptionError("unexpected_value", ArgMessageLookup.Get(Settings, ArgMessageKeys.UnexpectedOptionValue, new string[] { alias }), token.Index, alias);
            }
        }

        ApplyFallbackValue(occurrence, option);
        result.Occurrences.Add(occurrence);
        return index + 1;
    }

    internal int ParseShortCluster(ArgParseResult result, List<ArgToken> tokens, int index)
    {
        ArgToken token = tokens[index];
        token.MarkConsumed();
        int characterIndex = 1;
        while (characterIndex < token.RawText.Length)
        {
            string alias = "-" + token.RawText[characterIndex].ToString();
            ArgOption? option = FindOption(alias);
            if (option == null)
            {
                AddUnknownOption(result, tokens, index, token, alias);
                if (StopParsing)
                {
                    return index + 1;
                }

                characterIndex = characterIndex + 1;
                continue;
            }

            ArgOccurrence occurrence = new ArgOccurrence();
            occurrence.SymbolKind = ArgSymbolKind.Option;
            occurrence.SymbolName = option.Name;
            occurrence.Alias = alias;
            occurrence.TokenIndex = token.Index;
            occurrence.Option = option;
            occurrence.Source = ArgValueSource.CommandLine;
            ApplyOptionCount(occurrence, option);

            if (OptionAcceptsValue(option))
            {
                if (characterIndex + 1 < token.RawText.Length)
                {
                    occurrence.Values.Add(token.RawText.Substring(characterIndex + 1));
                    AddSeparateOptionValues(result, occurrence, tokens, index);
                }
                else
                {
                    AddSeparateOptionValues(result, occurrence, tokens, index);
                }

                ApplyFallbackValue(occurrence, option);
                result.Occurrences.Add(occurrence);
                return index + 1;
            }

            ApplyFallbackValue(occurrence, option);
            result.Occurrences.Add(occurrence);
            characterIndex = characterIndex + 1;
        }

        return index + 1;
    }

    internal void AddSeparateOptionValues(ArgParseResult result, ArgOccurrence occurrence, List<ArgToken> tokens, int optionIndex)
    {
        int valueIndex = optionIndex + 1;
        int initialValueCount = occurrence.Values.Count;
        while (valueIndex < tokens.Count && OptionCanAcceptAnotherValue(occurrence))
        {
            ArgToken valueToken = tokens[valueIndex];
            if (valueToken.Consumed)
            {
                break;
            }

            if (TokenStopsOptionValues(valueToken))
            {
                break;
            }

            if (FindSubcommand(CurrentCommand, valueToken.RawText) != null)
            {
                break;
            }

            int positionalReserve = RequiredPositionalValueReserve(result);
            int remainingPositionals = CountRemainingPositionalTokens(tokens, valueIndex);
            if (remainingPositionals <= positionalReserve)
            {
                break;
            }

            occurrence.Values.Add(valueToken.RawText);
            valueToken.MarkConsumed();
            valueIndex = valueIndex + 1;
        }

        if (initialValueCount == 0 && occurrence.Values.Count == 0 && occurrence.Option != null && occurrence.Option.ValueArity.Minimum > 0)
        {
            result.AddOptionError("missing_value", ArgMessageLookup.Get(Settings, ArgMessageKeys.MissingOptionValue, new string[] { occurrence.Alias }), occurrence.TokenIndex, occurrence.Alias);
        }
    }

    internal bool OptionCanAcceptAnotherValue(ArgOccurrence occurrence)
    {
        if (occurrence.Option == null)
        {
            return false;
        }

        if (occurrence.Option.ValueArity.Maximum == ArgArity.Unbounded)
        {
            return true;
        }

        return occurrence.Values.Count < occurrence.Option.ValueArity.Maximum;
    }

    internal bool TokenStopsOptionValues(ArgToken token)
    {
        if (token.Kind == ArgTokenKind.LongOption)
        {
            return true;
        }

        if (token.Kind == ArgTokenKind.ShortOption)
        {
            return true;
        }

        if (token.Kind == ArgTokenKind.SlashOption)
        {
            return true;
        }

        return token.Kind == ArgTokenKind.EndOfOptions;
    }

    internal int RequiredPositionalValueReserve(ArgParseResult result)
    {
        int reserve = 0;
        int index = 0;
        while (index < result.Command.Arguments.Count)
        {
            reserve = reserve + result.Command.Arguments[index].ValueArity.Minimum;
            index = index + 1;
        }

        reserve = reserve - result.Positionals.Count;
        if (reserve < 0)
        {
            return 0;
        }

        return reserve;
    }

    internal int CountRemainingPositionalTokens(List<ArgToken> tokens, int startIndex)
    {
        int count = 0;
        int index = startIndex;
        while (index < tokens.Count)
        {
            ArgToken token = tokens[index];
            if (!token.Consumed && token.Kind == ArgTokenKind.Positional)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal void AddUnknownOption(ArgParseResult result, List<ArgToken> tokens, int index, ArgToken token, string alias)
    {
        if (Settings.StopAtFirstUnmatched)
        {
            AddRemainingUnmatched(result, tokens, index);
            StopParsing = true;
            return;
        }

        if (Settings.CollectUnmatched)
        {
            result.AddUnmatchedToken(token);
        }
        else
        {
            result.AddOptionError("unknown_option", ArgMessageLookup.Get(Settings, ArgMessageKeys.UnknownOption, new string[] { alias }), token.Index, alias);
        }
    }

    internal void AddRemainingUnmatched(ArgParseResult result, List<ArgToken> tokens, int startIndex)
    {
        int index = startIndex;
        while (index < tokens.Count)
        {
            result.AddUnmatchedToken(tokens[index]);
            index = index + 1;
        }
    }

    internal bool OptionAcceptsValue(ArgOption option)
    {
        if (option.HasValue)
        {
            return true;
        }

        if (option.ValueArity.Maximum != 0)
        {
            return true;
        }

        return false;
    }

    internal void ApplyOptionCount(ArgOccurrence occurrence, ArgOption option)
    {
        if (option.IsCount)
        {
            occurrence.Count = 1;
        }
        else
        {
            occurrence.Count = 0;
        }
    }

    internal void ApplyFallbackValue(ArgOccurrence occurrence, ArgOption option)
    {
        if (!option.HasFallback)
        {
            return;
        }

        if (!option.ValueIsOptional)
        {
            return;
        }

        if (occurrence.Values.Count > 0)
        {
            return;
        }

        occurrence.Values.Add(option.FallbackValue);
        occurrence.Source = ArgValueSource.Fallback;
    }

    internal ArgOption? FindOption(string alias)
    {
        ArgOption? local = FindLocalOption(CurrentCommand, alias);
        if (local != null)
        {
            return local;
        }

        ArgOption? global = FindInheritedGlobalOption(CurrentCommand, alias);
        if (global != null)
        {
            return global;
        }

        ArgOption? help = FindDefaultHelpOption(alias);
        if (help != null)
        {
            return help;
        }

        return FindDefaultVersionOption(alias);
    }

    internal ArgOption? FindDefaultHelpOption(string alias)
    {
        if (OptionMatchesAlias(DefaultHelpOption, alias))
        {
            return DefaultHelpOption;
        }

        return null;
    }

    internal ArgOption? FindDefaultVersionOption(string alias)
    {
        if (!HasVersionText())
        {
            return null;
        }

        if (OptionMatchesAlias(DefaultVersionOption, alias))
        {
            return DefaultVersionOption;
        }

        return null;
    }

    internal bool HasVersionText()
    {
        return ResolvedVersionText.Length > 0;
    }

    internal ArgCommand CommandRoot(ArgCommand command)
    {
        ArgCommand current = command;
        while (current.Parent != null)
        {
            current = current.Parent;
        }

        return current;
    }

    internal ArgOption? FindLocalOption(ArgCommand command, string alias)
    {
        int localIndex = 0;
        while (localIndex < command.Options.Count)
        {
            ArgOption option = command.Options[localIndex];
            if (OptionMatchesAlias(option, alias))
            {
                return option;
            }

            localIndex = localIndex + 1;
        }

        localIndex = 0;
        while (localIndex < command.Options.Count)
        {
            ArgOption option = command.Options[localIndex];
            if (IsNegatedOptionAlias(option, alias))
            {
                return option;
            }

            localIndex = localIndex + 1;
        }

        return null;
    }

    internal ArgOption? FindGlobalOption(ArgCommand command, string alias)
    {
        int globalIndex = 0;
        while (globalIndex < command.GlobalOptions.Count)
        {
            ArgOption option = command.GlobalOptions[globalIndex];
            if (OptionMatchesAlias(option, alias))
            {
                return option;
            }

            globalIndex = globalIndex + 1;
        }

        globalIndex = 0;
        while (globalIndex < command.GlobalOptions.Count)
        {
            ArgOption option = command.GlobalOptions[globalIndex];
            if (IsNegatedOptionAlias(option, alias))
            {
                return option;
            }

            globalIndex = globalIndex + 1;
        }

        return null;
    }

    internal ArgOption? FindInheritedGlobalOption(ArgCommand command, string alias)
    {
        ArgCommand? current = command;
        while (current != null)
        {
            ArgOption? option = FindGlobalOption(current, alias);
            if (option != null)
            {
                return option;
            }

            current = current.Parent;
        }

        return null;
    }

    internal bool OptionMatchesAlias(ArgOption option, string alias)
    {
        int index = 0;
        while (index < option.Aliases.Count)
        {
            if (NamesEqual(option.Aliases[index], alias, Settings.OptionMatchingCaseInsensitive()))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsNegatedOptionAlias(ArgOption option, string alias)
    {
        if (!option.IsNegatable)
        {
            return false;
        }

        if (alias.Length <= 5)
        {
            return false;
        }

        if (!NamesEqual(alias.Substring(0, 5), "--no-", Settings.OptionMatchingCaseInsensitive()))
        {
            return false;
        }

        string positiveAlias = "--" + alias.Substring(5);
        return OptionMatchesAlias(option, positiveAlias);
    }
}
