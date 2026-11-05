using System.Text;

namespace ftp;

internal static class FtpCommandParser
{
    internal static FtpCommand ParseEncodedCommand(byte[] lineBytes, Encoding? argumentEncoding)
    {
        if (lineBytes == null)
        {
            throw new System.ArgumentNullException("lineBytes");
        }

        if (argumentEncoding == null)
        {
            throw new FtpConfigurationException("A negotiated command encoding is required.", null);
        }

        int codePage = argumentEncoding.CodePage;
        if (codePage == Encoding.ASCII.CodePage)
        {
            ValidateAsciiBytes(lineBytes);
        }
        else if (codePage == Encoding.UTF8.CodePage)
        {
            ValidateUtf8Bytes(lineBytes);
        }
        else
        {
            throw new FtpConfigurationException("FTP command decoding supports negotiated ASCII or UTF-8.", null);
        }

        string line = argumentEncoding.GetString(lineBytes);
        return ParseDecodedCommand(line);
    }

    internal static FtpCommand ParseDecodedCommand(string? line)
    {
        if (line == null)
        {
            throw new System.ArgumentNullException("line");
        }

        if (line.Length == 0)
        {
            throw new FtpProtocolException("An FTP command line cannot be empty.", null);
        }

        ValidateDecodedCharacters(line);
        int separator = line.IndexOf(' ');
        string originalVerb;
        string? argument;
        if (separator < 0)
        {
            originalVerb = line;
            argument = null;
        }
        else
        {
            originalVerb = line.Substring(0, separator);
            argument = line.Substring(separator + 1);
        }

        ValidateVerb(originalVerb);
        string verb = originalVerb.ToUpperInvariant();
        FtpCommand command = new FtpCommand();
        command.Kind = ClassifyVerb(verb);
        command.Verb = verb;
        command.Argument = argument;
        command.RawText = line;
        command.Sensitive = FtpCommandWriter.IsSensitiveCommand(command.Kind, verb);
        ValidateNumericCommandFields(command);
        return command;
    }

    internal static long ParseNonNegativeInt64(string? text, string fieldName)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new FtpProtocolException($"The FTP {fieldName} value is required.", null);
        }

        long value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                throw new FtpProtocolException($"The FTP {fieldName} value must contain only decimal digits.", null);
            }

            int digit = character - '0';
            if (value > (long.MaxValue - digit) / 10)
            {
                throw new FtpProtocolException($"The FTP {fieldName} value exceeds Int64 bounds.", null);
            }

            value = (value * 10) + digit;
            index++;
        }

        return value;
    }

    internal static int ParseInt32InRange(string? text, int minimum, int maximum, string fieldName)
    {
        if (minimum > maximum)
        {
            throw new FtpConfigurationException("A numeric parsing range must be ordered.", null);
        }

        long value = ParseNonNegativeInt64(text, fieldName);
        if (value < minimum || value > maximum)
        {
            throw new FtpProtocolException($"The FTP {fieldName} value is outside its permitted range.", null);
        }

        return (int)value;
    }

    internal static void ValidateNumericCommandFields(FtpCommand command)
    {
        if (command.Kind == FtpCommandKind.Rest)
        {
            FtpArgumentCodec.ValidateRestartMarker(command.Argument);
            return;
        }

        if (command.Kind == FtpCommandKind.Pbsz)
        {
            ParseNonNegativeInt64(command.Argument, "PBSZ size");
            return;
        }

        if (command.Kind == FtpCommandKind.Allo)
        {
            FtpArgumentCodec.ParseAllocateArgument(command.Argument);
            return;
        }

        if (command.Kind == FtpCommandKind.Host)
        {
            FtpHostCodec.ParseHostArgument(command.Argument);
            return;
        }

        if (command.Kind == FtpCommandKind.Lang)
        {
            if (command.Argument != null)
            {
                FtpArgumentCodec.ParseLanguageTag(command.Argument);
            }
            return;
        }

        if (command.Kind == FtpCommandKind.Type)
        {
            FtpTransferParameterCodec.ParseRepresentation(command.Argument);
            return;
        }

        if (command.Kind == FtpCommandKind.Stru)
        {
            FtpTransferParameterCodec.ParseStructure(command.Argument);
            return;
        }

        if (command.Kind == FtpCommandKind.Mode)
        {
            FtpTransferParameterCodec.ParseMode(command.Argument);
            return;
        }

        if (command.Kind == FtpCommandKind.Port)
        {
            FtpEndpointCodec.ParsePortArgument(command.Argument);
            return;
        }

        if (command.Kind == FtpCommandKind.Eprt)
        {
            FtpEndpointCodec.ParseExtendedPortRequest(command.Argument);
            return;
        }

        if (command.Kind == FtpCommandKind.Epsv)
        {
            FtpEndpointCodec.ParseExtendedPassiveArgument(command.Argument);
        }
    }

    internal static void ValidateAllocateArgument(string? argument)
    {
        FtpArgumentCodec.ParseAllocateArgument(argument);
    }

    internal static void ValidateDecodedCharacters(string line)
    {
        int index = 0;
        while (index < line.Length)
        {
            if (char.IsControl(line[index]))
            {
                throw new FtpProtocolException("FTP command text cannot contain control characters.", null);
            }

            index++;
        }
    }

    internal static void ValidateVerb(string verb)
    {
        if (verb.Length == 0)
        {
            throw new FtpProtocolException("An FTP command verb is required before its argument.", null);
        }

        int index = 0;
        while (index < verb.Length)
        {
            char character = verb[index];
            bool letter = (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z');
            bool digit = character >= '0' && character <= '9';
            if ((!letter && (index == 0 || (!digit && character != '-'))) || character > 127)
            {
                throw new FtpProtocolException("An FTP command verb must be an ASCII token beginning with a letter.", null);
            }

            index++;
        }
    }

    internal static void ValidateAsciiBytes(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] > 127)
            {
                throw new FtpProtocolException("The command contains bytes outside negotiated ASCII.", null);
            }

            index++;
        }
    }

    internal static void ValidateUtf8Bytes(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            byte first = bytes[index];
            if (first <= 127)
            {
                index++;
            }
            else if (first >= 194 && first <= 223)
            {
                RequireUtf8Continuation(bytes, index, 1);
                index = index + 2;
            }
            else if (first == 224)
            {
                RequireUtf8Range(bytes, index, 160, 191, 1);
                RequireUtf8Continuation(bytes, index, 2);
                index = index + 3;
            }
            else if ((first >= 225 && first <= 236) || (first >= 238 && first <= 239))
            {
                RequireUtf8Continuation(bytes, index, 1);
                RequireUtf8Continuation(bytes, index, 2);
                index = index + 3;
            }
            else if (first == 237)
            {
                RequireUtf8Range(bytes, index, 128, 159, 1);
                RequireUtf8Continuation(bytes, index, 2);
                index = index + 3;
            }
            else if (first == 240)
            {
                RequireUtf8Range(bytes, index, 144, 191, 1);
                RequireUtf8Continuation(bytes, index, 2);
                RequireUtf8Continuation(bytes, index, 3);
                index = index + 4;
            }
            else if (first >= 241 && first <= 243)
            {
                RequireUtf8Continuation(bytes, index, 1);
                RequireUtf8Continuation(bytes, index, 2);
                RequireUtf8Continuation(bytes, index, 3);
                index = index + 4;
            }
            else if (first == 244)
            {
                RequireUtf8Range(bytes, index, 128, 143, 1);
                RequireUtf8Continuation(bytes, index, 2);
                RequireUtf8Continuation(bytes, index, 3);
                index = index + 4;
            }
            else
            {
                throw new FtpProtocolException("The command contains invalid UTF-8.", null);
            }
        }
    }

    internal static void RequireUtf8Continuation(byte[] bytes, int sequenceStart, int distance)
    {
        int index = sequenceStart + distance;
        if (index >= bytes.Length || bytes[index] < 128 || bytes[index] > 191)
        {
            throw new FtpProtocolException("The command contains invalid UTF-8.", null);
        }
    }

    internal static void RequireUtf8Range(byte[] bytes, int sequenceStart, int minimum, int maximum, int distance)
    {
        int index = sequenceStart + distance;
        if (index >= bytes.Length || bytes[index] < minimum || bytes[index] > maximum)
        {
            throw new FtpProtocolException("The command contains invalid UTF-8.", null);
        }
    }

    internal static FtpCommandKind ClassifyVerb(string verb)
    {
        return FtpCommandCatalog.ResolveKind(verb);
    }
}
