namespace pop3;

internal class Pop3ExtensionDispatcher
{
    System.Collections.Generic.List<string> CommandNames =
        new System.Collections.Generic.List<string>();
    System.Collections.Generic.List<IPop3ServerExtension> CommandExtensions =
        new System.Collections.Generic.List<IPop3ServerExtension>();
    System.Collections.Generic.List<Pop3SessionState[]> CommandStates =
        new System.Collections.Generic.List<Pop3SessionState[]>();

    internal void ConfigureExtensions(IPop3ServerExtension[] extensions)
    {
        if (extensions == null)
        {
            throw new System.ArgumentException(
                "POP3 server extensions cannot be null.",
                "extensions");
        }

        CommandNames.Clear();
        CommandExtensions.Clear();
        CommandStates.Clear();
        int extensionIndex = 0;
        while (extensionIndex < extensions.Length)
        {
            IPop3ServerExtension extension = extensions[extensionIndex];
            if (extension == null)
            {
                throw new System.ArgumentException(
                    "A POP3 server extension cannot be null.",
                    "extensions");
            }
            string[] names = extension.GetCommandNames();
            if (names == null)
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 server extension returned null command names.");
            }

            int nameIndex = 0;
            while (nameIndex < names.Length)
            {
                string canonicalName = ValidateCommandName(names[nameIndex]);
                if (IsStandardCommand(canonicalName) ||
                    FindCommandIndex(canonicalName) >= 0)
                {
                    throw new System.IO.InvalidDataException(
                        "A POP3 server extension command conflicts with another command.");
                }
                Pop3SessionState[] states = ValidateAllowedStates(
                    extension.GetAllowedStates(canonicalName));
                CommandNames.Add(canonicalName);
                CommandExtensions.Add(extension);
                CommandStates.Add(states);
                nameIndex++;
            }
            extensionIndex++;
        }
    }

    internal bool HandlesCommand(string commandName)
    {
        if (commandName == null)
        {
            throw new System.ArgumentNullException("commandName");
        }
        return FindCommandIndex(commandName) >= 0;
    }

    internal bool AllowsCommand(
        string commandName,
        Pop3SessionState state)
    {
        int commandIndex = FindCommandIndex(commandName);
        if (commandIndex < 0)
        {
            return false;
        }
        Pop3SessionState[] states = CommandStates[commandIndex];
        int stateIndex = 0;
        while (stateIndex < states.Length)
        {
            if (states[stateIndex] == state)
            {
                return true;
            }
            stateIndex++;
        }
        return false;
    }

    internal Pop3ExtensionResult ExecuteRegisteredCommand(
        Pop3ExtensionContext context,
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        if (context == null)
        {
            throw new System.ArgumentNullException("context");
        }
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        int commandIndex = FindCommandIndex(command.Keyword);
        if (commandIndex < 0 ||
            !AllowsCommand(command.Keyword, context.State))
        {
            throw new Pop3StateException(
                "The POP3 extension command is not registered in the current state.",
                command.Keyword,
                context.State);
        }
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ExtensionResult supplied =
            CommandExtensions[commandIndex].ExecuteCommand(
                context,
                command,
                cancellationToken);
        return ValidateResult(supplied);
    }

    string ValidateCommandName(string commandName)
    {
        if (commandName == null)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 server extension command name cannot be null.");
        }
        Pop3Command command = new Pop3Command();
        command.Keyword = commandName;
        new Pop3CommandParser().FormatCommand(command);
        return commandName.ToUpperInvariant();
    }

    Pop3SessionState[] ValidateAllowedStates(Pop3SessionState[] states)
    {
        if (states == null || states.Length == 0)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 server extension command requires at least one legal state.");
        }
        Pop3SessionState[] copy = new Pop3SessionState[states.Length];
        int index = 0;
        while (index < states.Length)
        {
            if (states[index] != Pop3SessionState.Authorization &&
                states[index] != Pop3SessionState.Transaction)
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 server extension command has an invalid legal state.");
            }
            int previousIndex = 0;
            while (previousIndex < index)
            {
                if (states[previousIndex] == states[index])
                {
                    throw new System.IO.InvalidDataException(
                        "A POP3 server extension command repeats a legal state.");
                }
                previousIndex++;
            }
            copy[index] = states[index];
            index++;
        }
        return copy;
    }

    Pop3ExtensionResult ValidateResult(Pop3ExtensionResult supplied)
    {
        if (supplied == null || supplied.Response == null)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 server extension returned an invalid result.");
        }
        Pop3Response response = CopyResponse(supplied.Response);
        if (response.Kind != Pop3ResponseKind.Positive &&
            response.Kind != Pop3ResponseKind.Negative)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 server extension returned an invalid response kind.");
        }
        if (response.GreetingTimestamp != null)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 server extension response cannot contain a greeting timestamp.");
        }
        if (response.AdvisoryOctetCount < -1)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 server extension returned an invalid advisory size.");
        }

        System.IO.Stream? multilineData = supplied.MultilineData;
        if (multilineData != null &&
            (response.Kind != Pop3ResponseKind.Positive ||
                !multilineData.CanRead))
        {
            throw new System.IO.InvalidDataException(
                "A POP3 server extension returned invalid multiline data.");
        }
        if (multilineData == null && response.IsMultiline)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 server extension declared multiline data without a stream.");
        }
        response.IsMultiline = multilineData != null;
        new Pop3ResponseParser().FormatResponse(
            response,
            Pop3Defaults.MaximumResponseLineOctets);

        Pop3ExtensionResult result = new Pop3ExtensionResult();
        result.Response = response;
        result.MultilineData = multilineData;
        result.CloseSession = supplied.CloseSession;
        return result;
    }

    Pop3Response CopyResponse(Pop3Response source)
    {
        Pop3Response copy = new Pop3Response();
        copy.Kind = source.Kind;
        copy.Text = source.Text;
        if (source.ResponseCodeLevels == null)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 server extension returned null response-code levels.");
        }
        copy.ResponseCodeLevels = new string[
            source.ResponseCodeLevels.Length];
        System.Array.Copy(
            source.ResponseCodeLevels,
            copy.ResponseCodeLevels,
            source.ResponseCodeLevels.Length);
        copy.GreetingTimestamp = source.GreetingTimestamp;
        copy.IsMultiline = source.IsMultiline;
        copy.AdvisoryOctetCount = source.AdvisoryOctetCount;
        return copy;
    }

    int FindCommandIndex(string commandName)
    {
        int index = 0;
        while (index < CommandNames.Count)
        {
            if (string.Equals(
                CommandNames[index],
                commandName,
                System.StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    bool IsStandardCommand(string commandName)
    {
        return NameEquals(commandName, Pop3Commands.User) ||
            NameEquals(commandName, Pop3Commands.Pass) ||
            NameEquals(commandName, Pop3Commands.Apop) ||
            NameEquals(commandName, Pop3Commands.Stat) ||
            NameEquals(commandName, Pop3Commands.List) ||
            NameEquals(commandName, Pop3Commands.Retr) ||
            NameEquals(commandName, Pop3Commands.Dele) ||
            NameEquals(commandName, Pop3Commands.Noop) ||
            NameEquals(commandName, Pop3Commands.Rset) ||
            NameEquals(commandName, Pop3Commands.Quit) ||
            NameEquals(commandName, Pop3Commands.Top) ||
            NameEquals(commandName, Pop3Commands.Uidl) ||
            NameEquals(commandName, Pop3Commands.Capa) ||
            NameEquals(commandName, Pop3Commands.Stls) ||
            NameEquals(commandName, Pop3Commands.Auth) ||
            NameEquals(commandName, Pop3Commands.Utf8) ||
            NameEquals(commandName, Pop3Commands.Lang);
    }

    bool NameEquals(string first, string second)
    {
        return string.Equals(
            first,
            second,
            System.StringComparison.OrdinalIgnoreCase);
    }
}
