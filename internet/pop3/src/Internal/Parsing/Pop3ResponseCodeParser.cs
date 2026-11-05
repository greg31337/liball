namespace pop3;

internal class Pop3ResponseCodeParser
{
    internal string[] ParseResponseCode(System.ReadOnlySpan<byte> text)
    {
        if (text.Length == 0 || text[0] != 91)
        {
            return new string[0];
        }

        int end = 1;
        while (end < text.Length && text[end] != 93)
        {
            end++;
        }
        if (end == text.Length)
        {
            throw new System.IO.InvalidDataException("A POP3 response code is missing its closing bracket.");
        }
        if (end == 1)
        {
            throw new System.IO.InvalidDataException("A POP3 response code must contain at least one level.");
        }

        int levelCount = 1;
        int index = 1;
        while (index < end)
        {
            if (text[index] == 47)
            {
                levelCount++;
            }
            else
            {
                ValidateResponseCodeOctet(text[index]);
            }
            index++;
        }

        string[] levels = new string[levelCount];
        int levelStart = 1;
        int levelIndex = 0;
        index = 1;
        while (index <= end)
        {
            if (index == end || text[index] == 47)
            {
                if (index == levelStart)
                {
                    throw new System.IO.InvalidDataException("A POP3 response code cannot contain an empty level.");
                }
                levels[levelIndex] = DecodeLevel(text.Slice(levelStart, index - levelStart));
                levelIndex++;
                levelStart = index + 1;
            }
            index++;
        }
        return levels;
    }

    internal byte[] FormatResponseCode(string[] levels)
    {
        if (levels == null)
        {
            throw new System.ArgumentNullException("levels");
        }
        if (levels.Length == 0)
        {
            return new byte[0];
        }

        int index = 0;
        while (index < levels.Length)
        {
            ValidateResponseCodeLevel(levels[index]);
            index++;
        }

        string registeredCode = GetRegisteredResponseCode(levels);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append('[');
        index = 0;
        while (index < levels.Length)
        {
            if (index > 0)
            {
                builder.Append('/');
            }
            builder.Append(GetFormattedLevel(levels, index, registeredCode));
            index++;
        }
        builder.Append(']');
        return System.Text.Encoding.ASCII.GetBytes(builder.ToString());
    }

    internal string GetRegisteredResponseCode(string[] levels)
    {
        if (levels == null)
        {
            throw new System.ArgumentNullException("levels");
        }
        if (MatchesLevel(levels, 0, Pop3ResponseCodes.LoginDelay))
        {
            return Pop3ResponseCodes.LoginDelay;
        }
        if (MatchesLevel(levels, 0, Pop3ResponseCodes.InUse))
        {
            return Pop3ResponseCodes.InUse;
        }
        if (MatchesLevel(levels, 0, "SYS") && MatchesLevel(levels, 1, "PERM"))
        {
            return Pop3ResponseCodes.SystemPermanent;
        }
        if (MatchesLevel(levels, 0, "SYS") && MatchesLevel(levels, 1, "TEMP"))
        {
            return Pop3ResponseCodes.SystemTemporary;
        }
        if (MatchesLevel(levels, 0, Pop3ResponseCodes.Authentication))
        {
            return Pop3ResponseCodes.Authentication;
        }
        if (MatchesLevel(levels, 0, Pop3ResponseCodes.Utf8))
        {
            return Pop3ResponseCodes.Utf8;
        }
        return "";
    }

    internal Pop3AuthenticationStatus ClassifyAuthenticationStatus(string[] levels)
    {
        string registeredCode = GetRegisteredResponseCode(levels);
        if (registeredCode == Pop3ResponseCodes.LoginDelay)
        {
            return Pop3AuthenticationStatus.LoginDelay;
        }
        if (registeredCode == Pop3ResponseCodes.InUse)
        {
            return Pop3AuthenticationStatus.InUse;
        }
        if (registeredCode == Pop3ResponseCodes.SystemTemporary)
        {
            return Pop3AuthenticationStatus.TemporaryFailure;
        }
        if (registeredCode == Pop3ResponseCodes.SystemPermanent)
        {
            return Pop3AuthenticationStatus.PermanentFailure;
        }
        return Pop3AuthenticationStatus.Rejected;
    }

    internal bool ResponseCodesEqual(string[] first, string[] second)
    {
        if (first == null)
        {
            throw new System.ArgumentNullException("first");
        }
        if (second == null)
        {
            throw new System.ArgumentNullException("second");
        }
        if (first.Length != second.Length)
        {
            return false;
        }

        int index = 0;
        while (index < first.Length)
        {
            if (!string.Equals(first[index], second[index], System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    void ValidateResponseCodeOctet(byte value)
    {
        if (value < 33 || value > 126 || value == 47)
        {
            throw new System.IO.InvalidDataException("A POP3 response code contains an invalid octet.");
        }
    }

    void ValidateResponseCodeLevel(string level)
    {
        if (level == null)
        {
            throw new System.IO.InvalidDataException("A POP3 response code level cannot be null.");
        }
        if (level.Length == 0)
        {
            throw new System.IO.InvalidDataException("A POP3 response code cannot contain an empty level.");
        }

        int index = 0;
        while (index < level.Length)
        {
            char value = level[index];
            if (value < 33 || value > 126 || value == '/' || value == ']')
            {
                throw new System.IO.InvalidDataException("A POP3 response code level contains an invalid character.");
            }
            index++;
        }
    }

    string DecodeLevel(System.ReadOnlySpan<byte> level)
    {
        return System.Text.Encoding.ASCII.GetString(level);
    }

    bool MatchesLevel(string[] levels, int index, string expected)
    {
        if (index >= levels.Length)
        {
            return false;
        }
        return string.Equals(levels[index], expected, System.StringComparison.OrdinalIgnoreCase);
    }

    string GetFormattedLevel(string[] levels, int index, string registeredCode)
    {
        if (registeredCode == Pop3ResponseCodes.SystemPermanent)
        {
            return index == 0 ? "SYS" : index == 1 ? "PERM" : levels[index];
        }
        if (registeredCode == Pop3ResponseCodes.SystemTemporary)
        {
            return index == 0 ? "SYS" : index == 1 ? "TEMP" : levels[index];
        }
        if (registeredCode.Length > 0 && index == 0)
        {
            return registeredCode;
        }
        return levels[index];
    }
}
