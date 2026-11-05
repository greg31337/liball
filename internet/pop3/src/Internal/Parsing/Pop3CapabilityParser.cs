namespace pop3;

internal class Pop3CapabilityParser
{
    internal Pop3Capability ParseCapability(System.ReadOnlySpan<byte> line)
    {
        if (line.Length == 0)
        {
            throw new System.IO.InvalidDataException("A POP3 capability line cannot be empty.");
        }
        if (line.Length > Pop3Defaults.MaximumResponseLineOctets - 2)
        {
            throw new System.IO.InvalidDataException("A POP3 capability line exceeds 512 octets including CRLF.");
        }

        int nameLength = 0;
        while (nameLength < line.Length && line[nameLength] != 32)
        {
            ValidateNameOctet(line[nameLength]);
            nameLength++;
        }
        if (nameLength == 0)
        {
            throw new System.IO.InvalidDataException("A POP3 capability must have a nonempty tag.");
        }

        int parameterCount = CountParameters(line, nameLength);
        Pop3Capability capability = new Pop3Capability();
        capability.Name = System.Text.Encoding.ASCII.GetString(line.Slice(0, nameLength));
        capability.Parameters = ReadParameters(line, nameLength, parameterCount);
        return capability;
    }

    internal byte[] FormatCapability(Pop3Capability capability, Pop3SessionState state)
    {
        ValidateState(state);
        ValidateCapability(capability, state);

        string name = GetCanonicalName(capability.Name);
        int lineLength = name.Length;
        if (lineLength > Pop3Defaults.MaximumResponseLineOctets - 2)
        {
            throw new System.IO.InvalidDataException("A POP3 capability line exceeds 512 octets including CRLF.");
        }
        int parameterIndex = 0;
        while (parameterIndex < capability.Parameters.Length)
        {
            if (capability.Parameters[parameterIndex].Length > Pop3Defaults.MaximumResponseLineOctets - 2 - lineLength - 1)
            {
                throw new System.IO.InvalidDataException("A POP3 capability line exceeds 512 octets including CRLF.");
            }
            lineLength += capability.Parameters[parameterIndex].Length + 1;
            parameterIndex++;
        }

        byte[] line = new byte[lineLength];
        WriteAscii(name, line, 0);
        int writeIndex = name.Length;
        parameterIndex = 0;
        while (parameterIndex < capability.Parameters.Length)
        {
            line[writeIndex] = 32;
            writeIndex++;
            string parameter = capability.Parameters[parameterIndex];
            WriteAscii(parameter, line, writeIndex);
            writeIndex += parameter.Length;
            parameterIndex++;
        }
        return line;
    }

    internal Pop3CapabilitySet ParseCapabilities(byte[][] lines, Pop3SessionState state)
    {
        if (lines == null)
        {
            throw new System.ArgumentNullException("lines");
        }
        ValidateState(state);

        Pop3Capability[] entries = new Pop3Capability[lines.Length];
        int index = 0;
        while (index < lines.Length)
        {
            if (lines[index] == null)
            {
                throw new System.IO.InvalidDataException("A POP3 capability line cannot be null.");
            }
            entries[index] = ParseCapability(lines[index]);
            index++;
        }
        return InterpretCapabilities(entries, state);
    }

    internal Pop3CapabilitySet InterpretCapabilities(Pop3Capability[] entries, Pop3SessionState state)
    {
        if (entries == null)
        {
            throw new System.ArgumentNullException("entries");
        }
        ValidateState(state);

        int saslMechanismCount = CountSaslMechanisms(entries, state);
        Pop3CapabilitySet result = new Pop3CapabilitySet();
        result.Entries = entries;
        result.SaslMechanisms = new string[saslMechanismCount];
        int saslIndex = 0;
        int entryIndex = 0;
        while (entryIndex < entries.Length)
        {
            Pop3Capability capability = entries[entryIndex];
            ValidateCapability(capability, state);
            if (NameEquals(capability.Name, Pop3Capabilities.Top))
            {
                result.SupportsTop = true;
            }
            else if (NameEquals(capability.Name, Pop3Capabilities.User))
            {
                result.SupportsUser = true;
            }
            else if (NameEquals(capability.Name, Pop3Capabilities.Sasl))
            {
                int parameterIndex = 0;
                while (parameterIndex < capability.Parameters.Length)
                {
                    result.SaslMechanisms[saslIndex] = capability.Parameters[parameterIndex];
                    saslIndex++;
                    parameterIndex++;
                }
            }
            else if (NameEquals(capability.Name, Pop3Capabilities.ResponseCodes))
            {
                result.SupportsResponseCodes = true;
            }
            else if (NameEquals(capability.Name, Pop3Capabilities.LoginDelay))
            {
                if (!result.HasLoginDelay)
                {
                    result.HasLoginDelay = true;
                    result.LoginDelaySeconds = ParseNonnegativeInt32(capability.Parameters[0]);
                    result.LoginDelayPerUser = capability.Parameters.Length == 2;
                }
            }
            else if (NameEquals(capability.Name, Pop3Capabilities.Pipelining))
            {
                result.SupportsPipelining = true;
            }
            else if (NameEquals(capability.Name, Pop3Capabilities.Expire))
            {
                if (!result.HasExpire)
                {
                    result.HasExpire = true;
                    result.ExpireNever = ParameterEquals(capability.Parameters[0], "NEVER");
                    if (!result.ExpireNever)
                    {
                        result.ExpireDays = ParseNonnegativeInt32(capability.Parameters[0]);
                    }
                    result.ExpirePerUser = capability.Parameters.Length == 2;
                }
            }
            else if (NameEquals(capability.Name, Pop3Capabilities.Uidl))
            {
                result.SupportsUidl = true;
            }
            else if (NameEquals(capability.Name, Pop3Capabilities.Implementation))
            {
                if (result.Implementation == null)
                {
                    result.Implementation = JoinParameters(capability.Parameters);
                }
            }
            else if (NameEquals(capability.Name, Pop3Capabilities.Stls))
            {
                result.SupportsStls = true;
            }
            else if (NameEquals(capability.Name, Pop3Capabilities.AuthenticationResponseCode))
            {
                result.SupportsAuthenticationResponseCode = true;
            }
            else if (NameEquals(capability.Name, Pop3Capabilities.Utf8))
            {
                result.SupportsUtf8 = true;
                if (capability.Parameters.Length == 1)
                {
                    result.SupportsUtf8User = true;
                }
            }
            else if (NameEquals(capability.Name, Pop3Capabilities.Lang))
            {
                result.SupportsLang = true;
            }
            entryIndex++;
        }
        return result;
    }

    internal Pop3Capability[] FindCapabilities(Pop3Capability[] entries, string name)
    {
        if (entries == null)
        {
            throw new System.ArgumentNullException("entries");
        }
        if (name == null)
        {
            throw new System.ArgumentNullException("name");
        }

        int matchCount = 0;
        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index] == null)
            {
                throw new System.IO.InvalidDataException("A POP3 capability entry cannot be null.");
            }
            if (NameEquals(entries[index].Name, name))
            {
                matchCount++;
            }
            index++;
        }

        Pop3Capability[] matches = new Pop3Capability[matchCount];
        int matchIndex = 0;
        index = 0;
        while (index < entries.Length)
        {
            if (NameEquals(entries[index].Name, name))
            {
                matches[matchIndex] = entries[index];
                matchIndex++;
            }
            index++;
        }
        return matches;
    }

    int CountParameters(System.ReadOnlySpan<byte> line, int nameLength)
    {
        int parameterCount = 0;
        int index = nameLength;
        while (index < line.Length)
        {
            if (line[index] != 32)
            {
                throw new System.IO.InvalidDataException("A POP3 capability tag and its parameters must be separated by one space.");
            }
            index++;
            if (index == line.Length || line[index] == 32)
            {
                throw new System.IO.InvalidDataException("POP3 capability parameters must be nonempty and separated by one space.");
            }
            while (index < line.Length && line[index] != 32)
            {
                ValidateParameterOctet(line[index]);
                index++;
            }
            parameterCount++;
        }
        return parameterCount;
    }

    string[] ReadParameters(System.ReadOnlySpan<byte> line, int nameLength, int parameterCount)
    {
        string[] parameters = new string[parameterCount];
        int parameterIndex = 0;
        int index = nameLength;
        while (index < line.Length)
        {
            index++;
            int start = index;
            while (index < line.Length && line[index] != 32)
            {
                index++;
            }
            parameters[parameterIndex] = System.Text.Encoding.ASCII.GetString(line.Slice(start, index - start));
            parameterIndex++;
        }
        return parameters;
    }

    int CountSaslMechanisms(Pop3Capability[] entries, Pop3SessionState state)
    {
        int count = 0;
        int index = 0;
        while (index < entries.Length)
        {
            Pop3Capability capability = entries[index];
            ValidateCapability(capability, state);
            if (NameEquals(capability.Name, Pop3Capabilities.Sasl))
            {
                if (count > int.MaxValue - capability.Parameters.Length)
                {
                    throw new System.IO.InvalidDataException("The POP3 SASL mechanism list exceeds the supported size.");
                }
                count += capability.Parameters.Length;
            }
            index++;
        }
        return count;
    }

    void ValidateCapability(Pop3Capability capability, Pop3SessionState state)
    {
        if (capability == null)
        {
            throw new System.IO.InvalidDataException("A POP3 capability entry cannot be null.");
        }
        if (capability.Name == null)
        {
            throw new System.IO.InvalidDataException("A POP3 capability tag cannot be null.");
        }
        if (capability.Parameters == null)
        {
            throw new System.IO.InvalidDataException("POP3 capability parameters cannot be null.");
        }
        ValidateName(capability.Name);

        int parameterIndex = 0;
        while (parameterIndex < capability.Parameters.Length)
        {
            ValidateParameter(capability.Parameters[parameterIndex]);
            parameterIndex++;
        }

        if (IsNoParameterCapability(capability.Name) && capability.Parameters.Length != 0)
        {
            throw new System.IO.InvalidDataException("The registered POP3 capability does not accept parameters.");
        }
        if (NameEquals(capability.Name, Pop3Capabilities.Sasl))
        {
            ValidateSaslCapability(capability);
        }
        else if (NameEquals(capability.Name, Pop3Capabilities.LoginDelay))
        {
            ValidatePolicyCapability(capability, state, false);
        }
        else if (NameEquals(capability.Name, Pop3Capabilities.Expire))
        {
            ValidatePolicyCapability(capability, state, true);
        }
        else if (NameEquals(capability.Name, Pop3Capabilities.Implementation) && capability.Parameters.Length == 0)
        {
            throw new System.IO.InvalidDataException("The IMPLEMENTATION capability requires descriptive text.");
        }
        else if (NameEquals(capability.Name, Pop3Capabilities.Utf8))
        {
            if (capability.Parameters.Length > 1 ||
                capability.Parameters.Length == 1 && !ParameterEquals(capability.Parameters[0], "USER"))
            {
                throw new System.IO.InvalidDataException("The UTF8 capability accepts only the optional USER parameter.");
            }
        }
        else if (NameEquals(capability.Name, Pop3Capabilities.Stls) && state != Pop3SessionState.Authorization)
        {
            throw new System.IO.InvalidDataException("The STLS capability is valid only in the POP3 authorization state.");
        }
    }

    void ValidateSaslCapability(Pop3Capability capability)
    {
        if (capability.Parameters.Length == 0)
        {
            throw new System.IO.InvalidDataException("The SASL capability requires at least one mechanism.");
        }

        int parameterIndex = 0;
        while (parameterIndex < capability.Parameters.Length)
        {
            string mechanism = capability.Parameters[parameterIndex];
            if (mechanism.Length > 20)
            {
                throw new System.IO.InvalidDataException("A SASL mechanism name cannot exceed twenty characters.");
            }
            int characterIndex = 0;
            while (characterIndex < mechanism.Length)
            {
                char character = mechanism[characterIndex];
                bool valid = character >= 'A' && character <= 'Z' ||
                    character >= 'a' && character <= 'z' ||
                    character >= '0' && character <= '9' ||
                    character == '-' || character == '_';
                if (!valid)
                {
                    throw new System.IO.InvalidDataException("A SASL mechanism name contains an invalid character.");
                }
                characterIndex++;
            }
            parameterIndex++;
        }
    }

    void ValidatePolicyCapability(Pop3Capability capability, Pop3SessionState state, bool allowNever)
    {
        if (capability.Parameters.Length < 1 || capability.Parameters.Length > 2)
        {
            throw new System.IO.InvalidDataException("The POP3 policy capability has an invalid parameter count.");
        }
        if (!(allowNever && ParameterEquals(capability.Parameters[0], "NEVER")))
        {
            ParseNonnegativeInt32(capability.Parameters[0]);
        }
        if (capability.Parameters.Length == 2)
        {
            if (!ParameterEquals(capability.Parameters[1], "USER"))
            {
                throw new System.IO.InvalidDataException("The POP3 policy capability accepts only USER as its scope parameter.");
            }
            if (state != Pop3SessionState.Authorization)
            {
                throw new System.IO.InvalidDataException("A user-scoped POP3 policy capability is valid only in the authorization state.");
            }
        }
    }

    bool IsNoParameterCapability(string name)
    {
        return NameEquals(name, Pop3Capabilities.Top) ||
            NameEquals(name, Pop3Capabilities.User) ||
            NameEquals(name, Pop3Capabilities.ResponseCodes) ||
            NameEquals(name, Pop3Capabilities.Pipelining) ||
            NameEquals(name, Pop3Capabilities.Uidl) ||
            NameEquals(name, Pop3Capabilities.Stls) ||
            NameEquals(name, Pop3Capabilities.AuthenticationResponseCode) ||
            NameEquals(name, Pop3Capabilities.Lang);
    }

    int ParseNonnegativeInt32(string value)
    {
        if (value.Length == 0)
        {
            throw new System.IO.InvalidDataException("A POP3 capability integer cannot be empty.");
        }

        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                throw new System.IO.InvalidDataException("A POP3 capability integer must contain only decimal digits.");
            }
            int digit = character - '0';
            if (result > (int.MaxValue - digit) / 10)
            {
                throw new System.IO.InvalidDataException("A POP3 capability integer exceeds Int32.");
            }
            result = result * 10 + digit;
            index++;
        }
        return result;
    }

    string JoinParameters(string[] parameters)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < parameters.Length)
        {
            if (index > 0)
            {
                builder.Append(' ');
            }
            builder.Append(parameters[index]);
            index++;
        }
        return builder.ToString();
    }

    string GetCanonicalName(string name)
    {
        if (NameEquals(name, Pop3Capabilities.Top))
        {
            return Pop3Capabilities.Top;
        }
        if (NameEquals(name, Pop3Capabilities.User))
        {
            return Pop3Capabilities.User;
        }
        if (NameEquals(name, Pop3Capabilities.Sasl))
        {
            return Pop3Capabilities.Sasl;
        }
        if (NameEquals(name, Pop3Capabilities.ResponseCodes))
        {
            return Pop3Capabilities.ResponseCodes;
        }
        if (NameEquals(name, Pop3Capabilities.LoginDelay))
        {
            return Pop3Capabilities.LoginDelay;
        }
        if (NameEquals(name, Pop3Capabilities.Pipelining))
        {
            return Pop3Capabilities.Pipelining;
        }
        if (NameEquals(name, Pop3Capabilities.Expire))
        {
            return Pop3Capabilities.Expire;
        }
        if (NameEquals(name, Pop3Capabilities.Uidl))
        {
            return Pop3Capabilities.Uidl;
        }
        if (NameEquals(name, Pop3Capabilities.Implementation))
        {
            return Pop3Capabilities.Implementation;
        }
        if (NameEquals(name, Pop3Capabilities.Stls))
        {
            return Pop3Capabilities.Stls;
        }
        if (NameEquals(name, Pop3Capabilities.AuthenticationResponseCode))
        {
            return Pop3Capabilities.AuthenticationResponseCode;
        }
        if (NameEquals(name, Pop3Capabilities.Utf8))
        {
            return Pop3Capabilities.Utf8;
        }
        if (NameEquals(name, Pop3Capabilities.Lang))
        {
            return Pop3Capabilities.Lang;
        }
        return name;
    }

    void ValidateName(string name)
    {
        if (name.Length == 0)
        {
            throw new System.IO.InvalidDataException("A POP3 capability must have a nonempty tag.");
        }
        int index = 0;
        while (index < name.Length)
        {
            char value = name[index];
            if (value < 33 || value > 126 || value == '.')
            {
                throw new System.IO.InvalidDataException("A POP3 capability tag contains an invalid character.");
            }
            index++;
        }
    }

    void ValidateParameter(string parameter)
    {
        if (parameter == null || parameter.Length == 0)
        {
            throw new System.IO.InvalidDataException("A POP3 capability parameter cannot be null or empty.");
        }
        int index = 0;
        while (index < parameter.Length)
        {
            char value = parameter[index];
            if (value < 33 || value > 126)
            {
                throw new System.IO.InvalidDataException("A POP3 capability parameter contains an invalid character.");
            }
            index++;
        }
    }

    void ValidateNameOctet(byte value)
    {
        if (value < 33 || value > 126 || value == 46)
        {
            throw new System.IO.InvalidDataException("A POP3 capability tag contains an invalid octet.");
        }
    }

    void ValidateParameterOctet(byte value)
    {
        if (value < 33 || value > 126)
        {
            throw new System.IO.InvalidDataException("A POP3 capability parameter contains an invalid octet.");
        }
    }

    void WriteAscii(string value, byte[] destination, int offset)
    {
        int index = 0;
        while (index < value.Length)
        {
            destination[offset + index] = (byte)value[index];
            index++;
        }
    }

    bool NameEquals(string first, string second)
    {
        return string.Equals(first, second, System.StringComparison.OrdinalIgnoreCase);
    }

    bool ParameterEquals(string first, string second)
    {
        return string.Equals(first, second, System.StringComparison.OrdinalIgnoreCase);
    }

    void ValidateState(Pop3SessionState state)
    {
        if (state != Pop3SessionState.Authorization && state != Pop3SessionState.Transaction)
        {
            throw new System.ArgumentOutOfRangeException("state");
        }
    }
}
