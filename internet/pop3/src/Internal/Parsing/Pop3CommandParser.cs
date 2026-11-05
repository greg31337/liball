namespace pop3;

internal class Pop3CommandParser
{
    internal Pop3Command ParseCommand(System.ReadOnlySpan<byte> line)
    {
        return ParseServerCommand(line, false);
    }

    internal Pop3Command ParseServerCommand(
        System.ReadOnlySpan<byte> line,
        bool allowUtf8Credentials)
    {
        if (line.Length == 0)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 command cannot be empty.");
        }
        if (line.Length > Pop3Defaults.MaximumCommandLineOctets - 2)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 command exceeds 255 octets including CRLF.");
        }

        int keywordLength = 0;
        while (keywordLength < line.Length && line[keywordLength] != 32)
        {
            ValidateVisibleAscii(line[keywordLength]);
            keywordLength++;
        }
        if (keywordLength < 3 || keywordLength > 4)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 command keyword must contain three or four visible ASCII characters.");
        }

        byte[] keywordBytes = new byte[keywordLength];
        int keywordIndex = 0;
        while (keywordIndex < keywordLength)
        {
            keywordBytes[keywordIndex] = ToUpperAscii(line[keywordIndex]);
            keywordIndex++;
        }
        string keyword = System.Text.Encoding.ASCII.GetString(keywordBytes);
        int argumentCount = CountServerArguments(
            line,
            keywordLength,
            keyword,
            allowUtf8Credentials);

        Pop3Command command = new Pop3Command();
        command.Keyword = keyword;
        command.Arguments = ReadServerArguments(
            line,
            keywordLength,
            argumentCount);
        SetCommandMetadata(command);
        return command;
    }

    internal bool HasValidServerEncoding(
        System.ReadOnlySpan<byte> line,
        int keywordLength,
        bool allowUtf8Credentials)
    {
        byte[] keywordBytes = new byte[keywordLength];
        int keywordIndex = 0;
        while (keywordIndex < keywordLength)
        {
            keywordBytes[keywordIndex] = ToUpperAscii(line[keywordIndex]);
            keywordIndex++;
        }
        string keyword = System.Text.Encoding.ASCII.GetString(keywordBytes);
        Pop3Utf8Validator validator = new Pop3Utf8Validator();
        int argumentIndex = 0;
        int index = keywordLength;
        while (index < line.Length)
        {
            index++;
            int start = index;
            bool hasNonAscii = false;
            while (index < line.Length && line[index] != 32)
            {
                if (line[index] > 127)
                {
                    hasNonAscii = true;
                }
                index++;
            }
            System.ReadOnlySpan<byte> argument =
                line.Slice(start, index - start);
            if (hasNonAscii &&
                !AllowsUtf8Argument(
                    keyword,
                    argumentIndex,
                    allowUtf8Credentials))
            {
                return false;
            }
            if (!validator.IsValidUtf8(argument))
            {
                return false;
            }
            argumentIndex++;
        }
        return true;
    }

    internal byte[] FormatCommand(Pop3Command command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (command.Keyword == null)
        {
            throw new System.IO.InvalidDataException("A POP3 command keyword cannot be null.");
        }
        if (command.Arguments == null)
        {
            throw new System.IO.InvalidDataException("POP3 command arguments cannot be null.");
        }
        if (command.Keyword.Length < 3 || command.Keyword.Length > 4)
        {
            throw new System.IO.InvalidDataException("A POP3 command keyword must contain three or four visible ASCII characters.");
        }

        int lineLength = command.Keyword.Length;
        int keywordIndex = 0;
        while (keywordIndex < command.Keyword.Length)
        {
            ValidateVisibleAsciiCharacter(command.Keyword[keywordIndex]);
            keywordIndex++;
        }

        int argumentIndex = 0;
        while (argumentIndex < command.Arguments.Length)
        {
            string argument = command.Arguments[argumentIndex];
            if (argument == null || argument.Length == 0)
            {
                throw new System.IO.InvalidDataException("A POP3 command argument cannot be null or empty.");
            }

            int characterIndex = 0;
            while (characterIndex < argument.Length)
            {
                ValidateVisibleAsciiCharacter(argument[characterIndex]);
                characterIndex++;
            }

            if (argument.Length > Pop3Defaults.MaximumCommandLineOctets - 2 - lineLength - 1)
            {
                throw new System.IO.InvalidDataException("A POP3 command exceeds 255 octets including CRLF.");
            }
            lineLength += argument.Length + 1;
            argumentIndex++;
        }

        byte[] line = new byte[lineLength];
        keywordIndex = 0;
        while (keywordIndex < command.Keyword.Length)
        {
            line[keywordIndex] = ToUpperAsciiCharacter(command.Keyword[keywordIndex]);
            keywordIndex++;
        }

        int writeIndex = command.Keyword.Length;
        argumentIndex = 0;
        while (argumentIndex < command.Arguments.Length)
        {
            string argument = command.Arguments[argumentIndex];
            line[writeIndex] = 32;
            writeIndex++;

            int characterIndex = 0;
            while (characterIndex < argument.Length)
            {
                line[writeIndex] = (byte)argument[characterIndex];
                writeIndex++;
                characterIndex++;
            }
            argumentIndex++;
        }

        return line;
    }

    int CountServerArguments(
        System.ReadOnlySpan<byte> line,
        int keywordLength,
        string keyword,
        bool allowUtf8Credentials)
    {
        int argumentCount = 0;
        int index = keywordLength;
        Pop3Utf8Validator validator = new Pop3Utf8Validator();
        while (index < line.Length)
        {
            if (line[index] != 32)
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 keyword and its arguments must be separated by one space.");
            }
            index++;
            if (index == line.Length || line[index] == 32)
            {
                throw new System.IO.InvalidDataException(
                    "POP3 command arguments must be nonempty and separated by one space.");
            }

            int start = index;
            bool hasNonAscii = false;
            while (index < line.Length && line[index] != 32)
            {
                byte value = line[index];
                if (value < 33 || value == 127)
                {
                    throw new System.IO.InvalidDataException(
                        "POP3 command arguments contain a control octet.");
                }
                if (value > 127)
                {
                    hasNonAscii = true;
                }
                index++;
            }
            System.ReadOnlySpan<byte> argument =
                line.Slice(start, index - start);
            if (hasNonAscii &&
                !AllowsUtf8Argument(
                    keyword,
                    argumentCount,
                    allowUtf8Credentials))
            {
                throw new System.IO.InvalidDataException(
                    "The POP3 command does not permit a UTF-8 argument.");
            }
            if (!validator.IsValidUtf8(argument))
            {
                throw new System.IO.InvalidDataException(
                    "The POP3 command contains malformed UTF-8.");
            }
            argumentCount++;
        }
        return argumentCount;
    }

    string[] ReadServerArguments(
        System.ReadOnlySpan<byte> line,
        int keywordLength,
        int argumentCount)
    {
        string[] arguments = new string[argumentCount];
        System.Text.UTF8Encoding encoding =
            new System.Text.UTF8Encoding(false, true);
        int argumentIndex = 0;
        int index = keywordLength;
        while (index < line.Length)
        {
            index++;
            int start = index;
            while (index < line.Length && line[index] != 32)
            {
                index++;
            }
            arguments[argumentIndex] = encoding.GetString(
                line.Slice(start, index - start));
            argumentIndex++;
        }
        return arguments;
    }

    bool AllowsUtf8Argument(
        string keyword,
        int argumentIndex,
        bool allowUtf8Credentials)
    {
        if (!allowUtf8Credentials || argumentIndex != 0)
        {
            return false;
        }
        return keyword == Pop3Commands.User ||
            keyword == Pop3Commands.Pass ||
            keyword == Pop3Commands.Apop;
    }

    void SetCommandMetadata(Pop3Command command)
    {
        string keyword = command.Keyword;
        command.ContainsSensitiveData = keyword == Pop3Commands.Pass || keyword == Pop3Commands.Apop || keyword == Pop3Commands.Auth;
        command.ExpectsMultilineResponse = keyword == Pop3Commands.Retr || keyword == Pop3Commands.Top;

        if ((keyword == Pop3Commands.List || keyword == Pop3Commands.Uidl || keyword == Pop3Commands.Lang) && command.Arguments.Length == 0)
        {
            command.ExpectsMultilineResponse = true;
        }
        if (keyword == Pop3Commands.Capa)
        {
            command.ExpectsMultilineResponse = true;
        }
    }

    void ValidateVisibleAscii(byte value)
    {
        if (value < 33 || value > 126)
        {
            throw new System.IO.InvalidDataException("POP3 command tokens must contain only visible ASCII characters.");
        }
    }

    void ValidateVisibleAsciiCharacter(char value)
    {
        if (value < 33 || value > 126)
        {
            throw new System.IO.InvalidDataException("POP3 command tokens must contain only visible ASCII characters.");
        }
    }

    byte ToUpperAscii(byte value)
    {
        if (value >= 97 && value <= 122)
        {
            return (byte)(value - 32);
        }
        return value;
    }

    byte ToUpperAsciiCharacter(char value)
    {
        if (value >= 97 && value <= 122)
        {
            return (byte)(value - 32);
        }
        return (byte)value;
    }
}
