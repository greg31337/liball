namespace ftp;

internal static class FtpProtectionNegotiation
{
    internal static int InspectBufferSize(string? argument, bool tlsMechanism)
    {
        if (string.IsNullOrEmpty(argument))
        {
            return FtpReplyCodes.SyntaxErrorInParameters;
        }

        const long maximumBufferSize = 4294967295L;
        long value = 0;
        int index = 0;
        while (index < argument.Length)
        {
            char character = argument[index];
            if (character < '0' || character > '9')
            {
                return FtpReplyCodes.SyntaxErrorInParameters;
            }

            int digit = character - '0';
            if (value > (maximumBufferSize - digit) / 10)
            {
                return FtpReplyCodes.SyntaxErrorInParameters;
            }

            value = (value * 10) + digit;
            index++;
        }

        if (tlsMechanism && value != 0)
        {
            return FtpReplyCodes.SyntaxErrorInParameters;
        }
        if (!tlsMechanism && value == 0)
        {
            return FtpReplyCodes.SyntaxErrorInParameters;
        }
        return 0;
    }

    internal static int InspectProtectionLevel(string? argument, bool tlsMechanism)
    {
        if (argument == null || argument.Length != 1)
        {
            return FtpReplyCodes.CommandParameterNotImplemented;
        }

        char level = argument[0];
        bool clear = level == 'C' || level == 'c';
        bool safe = level == 'S' || level == 's';
        bool confidential = level == 'E' || level == 'e';
        bool privateLevel = level == 'P' || level == 'p';
        if (!clear && !safe && !confidential && !privateLevel)
        {
            return FtpReplyCodes.CommandParameterNotImplemented;
        }
        if (tlsMechanism && (safe || confidential))
        {
            return FtpReplyCodes.ProtectionLevelNotSupported;
        }
        return 0;
    }

    internal static bool IsClearLevel(string? argument)
    {
        return string.Equals(argument, "C", System.StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsPrivateLevel(string? argument)
    {
        return string.Equals(argument, "P", System.StringComparison.OrdinalIgnoreCase);
    }
}
