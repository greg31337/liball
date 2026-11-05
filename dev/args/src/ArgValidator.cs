namespace args;

internal class ArgValidator
{
    internal ArgValidationResult Validate(ArgParseResult parseResult)
    {
        ArgValidationResult result = new ArgValidationResult(parseResult);
        ApplyHelpAndVersionShortCircuit(result);
        if (result.ShortCircuited)
        {
            return result;
        }

        ValidateRequiredOptions(result, parseResult.Command);
        ValidateRequiredInheritedGlobalOptions(result, parseResult.Command);
        ValidateRequiredArguments(result, parseResult.Command);
        ValidateRequiredSubcommand(result, parseResult.Command);
        ValidateArity(result);
        ValidateDuplicateOptions(result);
        ValidateUnknownOptions(result);
        ValidateUnknownCommand(result);
        ValidateChoices(result);
        ValidateOptionConflicts(result);
        ValidateOptionDependencies(result);
        ValidateOptionGroups(result, parseResult.Command);
        ValidateCustomCallbacks(result);
        return result;
    }

    internal void ValidateCustomCallbacks(ArgValidationResult result)
    {
        ArgContext context = new ArgContext(result.ParseResult);
        int commandIndex = 0;
        while (commandIndex < result.ParseResult.CommandPath.Count)
        {
            ArgCommand command = result.ParseResult.CommandPath[commandIndex];
            int validatorIndex = 0;
            while (validatorIndex < command.CommandValidators.Count)
            {
                ArgParseError? error = command.CommandValidators[validatorIndex](context);
                if (error != null)
                {
                    result.Errors.Add(error);
                }

                validatorIndex = validatorIndex + 1;
            }

            validatorIndex = 0;
            while (validatorIndex < command.CrossOptionValidators.Count)
            {
                ArgParseError? error = command.CrossOptionValidators[validatorIndex](context.Values);
                if (error != null)
                {
                    result.Errors.Add(error);
                }

                validatorIndex = validatorIndex + 1;
            }

            commandIndex = commandIndex + 1;
        }
    }

    internal void ApplyHelpAndVersionShortCircuit(ArgValidationResult result)
    {
        if (result.ParseResult.HelpCommandRequested)
        {
            result.MarkHelpRequested();
        }

        int index = 0;
        while (index < result.ParseResult.Occurrences.Count)
        {
            ArgOccurrence occurrence = result.ParseResult.Occurrences[index];
            if (occurrence.Option != null)
            {
                if (IsHelpOptionWithSettings(occurrence.Option, result.ParseResult.Settings))
                {
                    result.MarkHelpRequested();
                }

                if (IsVersionOption(occurrence.Option))
                {
                    result.MarkVersionRequestedWithText(result.ParseResult.VersionText);
                }
            }

            index = index + 1;
        }
    }

    internal bool IsHelpOption(ArgOption option)
    {
        return IsHelpOptionWithSettings(option, new ArgParserSettings());
    }

    internal bool IsHelpOptionWithSettings(ArgOption option, ArgParserSettings settings)
    {
        int index = 0;
        while (index < settings.HelpAliases.Count)
        {
            if (OptionMatchesName(option, settings.HelpAliases[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsVersionOption(ArgOption option)
    {
        if (OptionMatchesName(option, "--version"))
        {
            return true;
        }

        if (OptionMatchesName(option, "-V"))
        {
            return true;
        }

        if (OptionMatchesName(option, "/version"))
        {
            return true;
        }

        return false;
    }

    internal string VersionText(ArgCommand command)
    {
        return ArgVersionText.ResolveCommand(command);
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

    internal void ValidateRequiredOptions(ArgValidationResult result, ArgCommand command)
    {
        int index = 0;
        while (index < command.Options.Count)
        {
            ArgOption option = command.Options[index];
            ValidateRequiredOption(result, option);
            index = index + 1;
        }
    }

    internal void ValidateRequiredInheritedGlobalOptions(ArgValidationResult result, ArgCommand command)
    {
        ArgCommand? current = command;
        while (current != null)
        {
            int index = 0;
            while (index < current.GlobalOptions.Count)
            {
                ArgOption option = current.GlobalOptions[index];
                ValidateRequiredOption(result, option);
                index = index + 1;
            }

            current = current.Parent;
        }
    }

    internal void ValidateRequiredOption(ArgValidationResult result, ArgOption option)
    {
        if (!option.IsRequired)
        {
            return;
        }

        if (HasOptionOccurrence(result.ParseResult, option))
        {
            return;
        }

        result.AddOptionError("missing_required_option", Message(result, ArgMessageKeys.MissingRequiredOption, new string[] { option.Name }), option.Name);
    }

    internal void ValidateRequiredArguments(ArgValidationResult result, ArgCommand command)
    {
        int index = 0;
        while (index < command.Arguments.Count)
        {
            ArgArgument argument = command.Arguments[index];
            ValidateRequiredArgument(result, argument);
            index = index + 1;
        }
    }

    internal void ValidateRequiredArgument(ArgValidationResult result, ArgArgument argument)
    {
        if (!argument.IsRequired)
        {
            return;
        }

        if (HasArgumentOccurrence(result.ParseResult, argument))
        {
            return;
        }

        result.AddArgumentError("missing_required_argument", Message(result, ArgMessageKeys.MissingRequiredArgument, new string[] { argument.Name }), argument.Name);
    }

    internal void ValidateRequiredSubcommand(ArgValidationResult result, ArgCommand command)
    {
        if (!command.RequiresSubcommand)
        {
            return;
        }

        result.AddCommandError("missing_required_subcommand", Message(result, ArgMessageKeys.MissingRequiredSubcommand, new string[] { command.Name }), command.Name);
    }

    internal void ValidateArity(ArgValidationResult result)
    {
        int index = 0;
        while (index < result.ParseResult.Occurrences.Count)
        {
            ArgOccurrence occurrence = result.ParseResult.Occurrences[index];
            if (occurrence.Option != null)
            {
                ValidateOptionArity(result, occurrence, occurrence.Option);
            }
            else if (occurrence.Argument != null)
            {
                ValidateArgumentArity(result, occurrence, occurrence.Argument);
            }

            index = index + 1;
        }
    }

    internal void ValidateOptionArity(ArgValidationResult result, ArgOccurrence occurrence, ArgOption option)
    {
        if (occurrence.Source == ArgValueSource.Implied && option.IsFlag)
        {
            return;
        }

        int count = occurrence.Values.Count;
        if (count < option.ValueArity.Minimum)
        {
            string message = Message(result, ArgMessageKeys.OptionTooFewValues, new string[] { occurrence.SymbolName, ValueCountText(result, option.ValueArity.Minimum) });
            result.AddOptionErrorAtToken("too_few_values", message, occurrence.TokenIndex, occurrence.SymbolName);
            return;
        }

        if (option.ValueArity.Maximum != ArgArity.Unbounded && count > option.ValueArity.Maximum)
        {
            string message = Message(result, ArgMessageKeys.OptionTooManyValues, new string[] { occurrence.SymbolName, ValueCountText(result, option.ValueArity.Maximum) });
            result.AddOptionErrorAtToken("too_many_values", message, occurrence.TokenIndex, occurrence.SymbolName);
        }
    }

    internal void ValidateArgumentArity(ArgValidationResult result, ArgOccurrence occurrence, ArgArgument argument)
    {
        int count = occurrence.Values.Count;
        if (count < argument.ValueArity.Minimum)
        {
            string message = Message(result, ArgMessageKeys.ArgumentTooFewValues, new string[] { occurrence.SymbolName, ValueCountText(result, argument.ValueArity.Minimum) });
            result.AddArgumentErrorAtToken("too_few_values", message, occurrence.TokenIndex, occurrence.SymbolName);
            return;
        }

        if (argument.ValueArity.Maximum != ArgArity.Unbounded && count > argument.ValueArity.Maximum)
        {
            string message = Message(result, ArgMessageKeys.ArgumentTooManyValues, new string[] { occurrence.SymbolName, ValueCountText(result, argument.ValueArity.Maximum) });
            result.AddArgumentErrorAtToken("too_many_values", message, occurrence.TokenIndex, occurrence.SymbolName);
        }
    }

    internal string ValueCountText(ArgValidationResult result, int count)
    {
        string invariantCount = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (count == 1)
        {
            return Message(result, ArgMessageKeys.OneValue, new string[] { invariantCount });
        }

        return Message(result, ArgMessageKeys.MultipleValues, new string[] { invariantCount });
    }

    internal void ValidateDuplicateOptions(ArgValidationResult result)
    {
        int index = 0;
        while (index < result.ParseResult.Occurrences.Count)
        {
            ArgOccurrence occurrence = result.ParseResult.Occurrences[index];
            if (occurrence.Option != null)
            {
                ValidateDuplicateOption(result, occurrence, occurrence.Option, index);
            }

            index = index + 1;
        }
    }

    internal void ValidateDuplicateOption(ArgValidationResult result, ArgOccurrence occurrence, ArgOption option, int occurrenceIndex)
    {
        if (option.IsRepeatable)
        {
            return;
        }

        if (option.IsCount)
        {
            return;
        }

        if (!HasEarlierOptionOccurrence(result.ParseResult, option, occurrenceIndex))
        {
            return;
        }

        string message = Message(result, ArgMessageKeys.DuplicateOption, new string[] { option.Name });
        result.AddOptionErrorAtToken("duplicate_option", message, occurrence.TokenIndex, option.Name);
    }

    internal bool HasEarlierOptionOccurrence(ArgParseResult parseResult, ArgOption option, int beforeIndex)
    {
        int index = 0;
        while (index < beforeIndex)
        {
            ArgOccurrence occurrence = parseResult.Occurrences[index];
            if (occurrence.Option == option)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void ValidateUnknownOptions(ArgValidationResult result)
    {
        int index = 0;
        while (index < result.ParseResult.UnmatchedTokens.Count)
        {
            ArgToken token = result.ParseResult.UnmatchedTokens[index];
            if (IsOptionToken(token))
            {
                string alias = UnknownOptionAlias(token);
                if (!IsKnownOption(result.ParseResult, alias))
                {
                    result.AddOptionErrorAtToken("unknown_option", Message(result, ArgMessageKeys.UnknownOption, new string[] { alias }), token.Index, alias);
                }
            }

            index = index + 1;
        }
    }

    internal bool IsOptionToken(ArgToken token)
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

        return false;
    }

    internal string UnknownOptionAlias(ArgToken token)
    {
        if (token.Kind == ArgTokenKind.LongOption)
        {
            int equalsIndex = token.RawText.IndexOf('=');
            if (equalsIndex >= 0)
            {
                return token.RawText.Substring(0, equalsIndex);
            }
        }

        if (token.Kind == ArgTokenKind.SlashOption)
        {
            int colonIndex = token.RawText.IndexOf(':');
            if (colonIndex >= 0)
            {
                return token.RawText.Substring(0, colonIndex);
            }
        }

        return token.RawText;
    }

    internal bool IsKnownOption(ArgParseResult parseResult, string alias)
    {
        if (FindLocalOption(parseResult.Command, alias) != null)
        {
            return true;
        }

        ArgCommand? current = parseResult.Command;
        while (current != null)
        {
            if (FindGlobalOption(current, alias) != null)
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    internal ArgOption? FindLocalOption(ArgCommand command, string alias)
    {
        int index = 0;
        while (index < command.Options.Count)
        {
            ArgOption option = command.Options[index];
            if (option.HasAlias(alias))
            {
                return option;
            }

            index = index + 1;
        }

        return null;
    }

    internal ArgOption? FindGlobalOption(ArgCommand command, string alias)
    {
        int index = 0;
        while (index < command.GlobalOptions.Count)
        {
            ArgOption option = command.GlobalOptions[index];
            if (option.HasAlias(alias))
            {
                return option;
            }

            index = index + 1;
        }

        return null;
    }

    internal void ValidateUnknownCommand(ArgValidationResult result)
    {
        ArgCommand command = result.ParseResult.Command;
        if (command.Commands.Count == 0)
        {
            return;
        }

        if (command.Arguments.Count > 0)
        {
            return;
        }

        ArgToken? token = FirstRemainingPositional(result.ParseResult);
        if (token == null)
        {
            return;
        }

        if (CommandMatchesNameOrAlias(command, token.RawText))
        {
            return;
        }

        result.AddCommandErrorAtToken("unknown_command", Message(result, ArgMessageKeys.UnknownCommand, new string[] { token.RawText }), token.Index, token.RawText);
    }

    internal ArgToken? FirstRemainingPositional(ArgParseResult parseResult)
    {
        int index = 0;
        while (index < parseResult.Positionals.Count)
        {
            ArgToken token = parseResult.Positionals[index];
            if (token.Kind == ArgTokenKind.Positional)
            {
                return token;
            }

            index = index + 1;
        }

        return null;
    }

    internal bool CommandMatchesNameOrAlias(ArgCommand command, string name)
    {
        int index = 0;
        while (index < command.Commands.Count)
        {
            ArgCommand child = command.Commands[index];
            if (child.MatchesNameOrAlias(name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void ValidateChoices(ArgValidationResult result)
    {
        int index = 0;
        while (index < result.ParseResult.Occurrences.Count)
        {
            ArgOccurrence occurrence = result.ParseResult.Occurrences[index];
            if (occurrence.Option != null)
            {
                ValidateOptionChoices(result, occurrence, occurrence.Option);
            }
            else if (occurrence.Argument != null)
            {
                ValidateArgumentChoices(result, occurrence, occurrence.Argument);
            }

            index = index + 1;
        }
    }

    internal void ValidateOptionChoices(ArgValidationResult result, ArgOccurrence occurrence, ArgOption option)
    {
        if (option.Choices.Count == 0)
        {
            return;
        }

        int index = 0;
        while (index < occurrence.Values.Count)
        {
            string value = occurrence.Values[index];
            if (!ArgValuePipeline.ContainsChoice(option.Choices, value, option.ChoiceValidationStage, option.ValueNormalizer, option.ChoiceConverter))
            {
                string message = Message(result, ArgMessageKeys.InvalidOptionChoice, new string[] { occurrence.SymbolName, value });
                result.AddOptionErrorAtToken("invalid_choice", message, occurrence.TokenIndex, occurrence.SymbolName);
            }

            index = index + 1;
        }
    }

    internal void ValidateArgumentChoices(ArgValidationResult result, ArgOccurrence occurrence, ArgArgument argument)
    {
        if (argument.Choices.Count == 0)
        {
            return;
        }

        int index = 0;
        while (index < occurrence.Values.Count)
        {
            string value = occurrence.Values[index];
            if (!ArgValuePipeline.ContainsChoice(argument.Choices, value, argument.ChoiceValidationStage, argument.ValueNormalizer, argument.ChoiceConverter))
            {
                string message = Message(result, ArgMessageKeys.InvalidArgumentChoice, new string[] { occurrence.SymbolName, value });
                result.AddArgumentErrorAtToken("invalid_choice", message, occurrence.TokenIndex, occurrence.SymbolName);
            }

            index = index + 1;
        }
    }

    internal void ValidateOptionConflicts(ArgValidationResult result)
    {
        int occurrenceIndex = 0;
        while (occurrenceIndex < result.ParseResult.Occurrences.Count)
        {
            ArgOccurrence occurrence = result.ParseResult.Occurrences[occurrenceIndex];
            if (occurrence.Option != null)
            {
                ValidateOptionConflictNames(result, occurrence, occurrence.Option, occurrenceIndex);
            }

            occurrenceIndex = occurrenceIndex + 1;
        }
    }

    internal void ValidateOptionConflictNames(ArgValidationResult result, ArgOccurrence occurrence, ArgOption option, int occurrenceIndex)
    {
        int conflictIndex = 0;
        while (conflictIndex < option.ConflictNames.Count)
        {
            string conflictName = option.ConflictNames[conflictIndex];
            ArgOccurrence? conflictOccurrence = FindFirstOptionOccurrence(result.ParseResult, conflictName);
            if (conflictOccurrence != null && conflictOccurrence.Option != null && conflictOccurrence.Option != option)
            {
                if (!HasEarlierConflict(result.ParseResult, option, conflictOccurrence.Option, occurrenceIndex))
                {
                    string message = Message(result, ArgMessageKeys.ConflictingOption, new string[] { option.Name, conflictOccurrence.Option.Name });
                    result.AddOptionErrorAtToken("conflicting_option", message, occurrence.TokenIndex, option.Name);
                }
            }

            conflictIndex = conflictIndex + 1;
        }
    }

    internal ArgOccurrence? FindFirstOptionOccurrence(ArgParseResult parseResult, string name)
    {
        int index = 0;
        while (index < parseResult.Occurrences.Count)
        {
            ArgOccurrence occurrence = parseResult.Occurrences[index];
            if (occurrence.Option != null && OptionMatchesName(occurrence.Option, name))
            {
                return occurrence;
            }

            index = index + 1;
        }

        return null;
    }

    internal bool HasEarlierConflict(ArgParseResult parseResult, ArgOption option, ArgOption conflictOption, int beforeIndex)
    {
        int index = 0;
        while (index < beforeIndex)
        {
            ArgOccurrence occurrence = parseResult.Occurrences[index];
            if (occurrence.Option != null)
            {
                if (occurrence.Option == option && OptionDeclaresConflictWith(occurrence.Option, conflictOption))
                {
                    return true;
                }

                if (occurrence.Option == conflictOption && OptionDeclaresConflictWith(occurrence.Option, option))
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return false;
    }

    internal bool OptionDeclaresConflictWith(ArgOption option, ArgOption conflictOption)
    {
        int index = 0;
        while (index < option.ConflictNames.Count)
        {
            if (OptionMatchesName(conflictOption, option.ConflictNames[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool OptionMatchesName(ArgOption option, string name)
    {
        if (option.Name == name)
        {
            return true;
        }

        return option.HasAlias(name);
    }

    internal void ValidateOptionDependencies(ArgValidationResult result)
    {
        int occurrenceIndex = 0;
        while (occurrenceIndex < result.ParseResult.Occurrences.Count)
        {
            ArgOccurrence occurrence = result.ParseResult.Occurrences[occurrenceIndex];
            if (occurrence.Option != null)
            {
                ValidateOptionRequiredNames(result, occurrence, occurrence.Option, occurrenceIndex);
            }

            occurrenceIndex = occurrenceIndex + 1;
        }
    }

    internal void ValidateOptionRequiredNames(ArgValidationResult result, ArgOccurrence occurrence, ArgOption option, int occurrenceIndex)
    {
        int requiredIndex = 0;
        while (requiredIndex < option.RequiredNames.Count)
        {
            string requiredName = option.RequiredNames[requiredIndex];
            if (FindFirstOptionOccurrence(result.ParseResult, requiredName) == null)
            {
                if (!HasEarlierRequirement(result.ParseResult, option, requiredName, occurrenceIndex))
                {
                    string displayName = RequiredOptionDisplayName(result.ParseResult, requiredName);
                    string message = Message(result, ArgMessageKeys.MissingDependentOption, new string[] { option.Name, displayName });
                    result.AddOptionErrorAtToken("missing_dependent_option", message, occurrence.TokenIndex, option.Name);
                }
            }

            requiredIndex = requiredIndex + 1;
        }
    }

    internal bool HasEarlierRequirement(ArgParseResult parseResult, ArgOption option, string requiredName, int beforeIndex)
    {
        int index = 0;
        while (index < beforeIndex)
        {
            ArgOccurrence occurrence = parseResult.Occurrences[index];
            if (occurrence.Option == option && OptionRequiresName(option, requiredName))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool OptionRequiresName(ArgOption option, string requiredName)
    {
        int index = 0;
        while (index < option.RequiredNames.Count)
        {
            if (option.RequiredNames[index] == requiredName)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string RequiredOptionDisplayName(ArgParseResult parseResult, string requiredName)
    {
        ArgOption? option = FindVisibleOption(parseResult, requiredName);
        if (option != null)
        {
            return option.Name;
        }

        return requiredName;
    }

    internal ArgOption? FindVisibleOption(ArgParseResult parseResult, string name)
    {
        ArgOption? local = FindLocalOption(parseResult.Command, name);
        if (local != null)
        {
            return local;
        }

        ArgCommand? current = parseResult.Command;
        while (current != null)
        {
            ArgOption? global = FindGlobalOption(current, name);
            if (global != null)
            {
                return global;
            }

            current = current.Parent;
        }

        return null;
    }

    internal bool HasOptionOccurrence(ArgParseResult parseResult, ArgOption option)
    {
        int index = 0;
        while (index < parseResult.Occurrences.Count)
        {
            ArgOccurrence occurrence = parseResult.Occurrences[index];
            if (occurrence.Option == option)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasArgumentOccurrence(ArgParseResult parseResult, ArgArgument argument)
    {
        int index = 0;
        while (index < parseResult.Occurrences.Count)
        {
            ArgOccurrence occurrence = parseResult.Occurrences[index];
            if (occurrence.Argument == argument)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void ValidateOptionGroups(ArgValidationResult result, ArgCommand command)
    {
        int index = 0;
        while (index < command.OptionGroups.Count)
        {
            ValidateOptionGroup(result, command.OptionGroups[index]);
            index = index + 1;
        }
    }

    internal void ValidateOptionGroup(ArgValidationResult result, ArgOptionGroup group)
    {
        int suppliedCount = SuppliedOptionGroupCount(result.ParseResult, group);
        if (group.IsMutuallyExclusive && suppliedCount > 1)
        {
            string message = Message(result, ArgMessageKeys.MutuallyExclusiveOptionGroup, new string[] { group.Name });
            result.AddOptionError("mutually_exclusive_group", message, group.Name);
        }

        if (group.IsAtLeastOneRequired && suppliedCount == 0)
        {
            string message = Message(result, ArgMessageKeys.MissingRequiredOptionGroup, new string[] { group.Name });
            result.AddOptionError("missing_required_option_group", message, group.Name);
        }
    }

    internal int SuppliedOptionGroupCount(ArgParseResult parseResult, ArgOptionGroup group)
    {
        int count = 0;
        int index = 0;
        while (index < group.OptionNames.Count)
        {
            ArgOption? option = FindVisibleOption(parseResult, group.OptionNames[index]);
            if (option != null && HasOptionOccurrence(parseResult, option) && !HasEarlierGroupOption(parseResult, group, option, index))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal bool HasEarlierGroupOption(ArgParseResult parseResult, ArgOptionGroup group, ArgOption option, int beforeIndex)
    {
        int index = 0;
        while (index < beforeIndex)
        {
            ArgOption? earlier = FindVisibleOption(parseResult, group.OptionNames[index]);
            if (earlier == option)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string Message(ArgValidationResult result, string key, string[] values)
    {
        return ArgMessageLookup.Get(result.ParseResult.Settings, key, values);
    }
}
