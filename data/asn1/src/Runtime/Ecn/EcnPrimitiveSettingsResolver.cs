namespace asn1;

internal class EcnPrimitiveSettingsResolver
{
    internal EcnPrimitiveSettingsResult ResolveSettings(
        EcnEncodingProperty[] properties)
    {
        if (properties == null)
        {
            throw new System.ArgumentNullException("properties");
        }

        EcnPrimitiveSettings settings = new EcnPrimitiveSettings();
        int index = 0;
        while (index < properties.Length)
        {
            EcnEncodingProperty property = properties[index];
            if (!property.Valid)
            {
                return this.CreateSettingsError(
                    "An invalid ECN property cannot be used by a primitive codec.");
            }

            if (property.Kind == EcnEncodingPropertyKind.Representation)
            {
                settings.Representation = property.Value.ToLowerInvariant();
            }
            else if (property.Kind == EcnEncodingPropertyKind.Signedness)
            {
                settings.Signedness = property.Value.ToLowerInvariant();
            }
            else if (property.Kind == EcnEncodingPropertyKind.Alphabet)
            {
                settings.HasAlphabet = true;
                settings.Alphabet = this.ReadAlphabet(property.Value);
            }
            else if (property.Kind == EcnEncodingPropertyKind.ByteOrder)
            {
                settings.ByteOrder = property.Value.ToLowerInvariant();
            }
            else if (property.Kind == EcnEncodingPropertyKind.BitOrder)
            {
                settings.BitOrder = property.Value.ToLowerInvariant();
            }
            else if (property.Kind == EcnEncodingPropertyKind.BitReversal)
            {
                settings.BitReversal = property.Value.ToLowerInvariant();
            }
            else if (property.Kind == EcnEncodingPropertyKind.Padding)
            {
                settings.Padding = property.Value.ToLowerInvariant();
            }
            else if (property.Kind == EcnEncodingPropertyKind.Justification)
            {
                settings.Justification = property.ValueTokens[0].Text
                    .ToLowerInvariant();
            }
            else if (property.Kind == EcnEncodingPropertyKind.ValueReversal)
            {
                settings.ValueReversal = property.Value == "TRUE"
                    || property.Value.ToLowerInvariant() == "true";
            }
            else if (property.Kind == EcnEncodingPropertyKind.UnusedBits)
            {
                if (!property.HasIntegerValue
                    || property.IntegerValue < 0
                    || property.IntegerValue > int.MaxValue)
                {
                    return this.CreateSettingsError(
                        "UNUSED BITS requires a supported non-negative integer.");
                }

                settings.HasUnusedBits = true;
                settings.UnusedBits = (int)property.IntegerValue;
            }
            else if (property.Kind == EcnEncodingPropertyKind.TruePattern)
            {
                EcnBitField? pattern = this.ParsePattern(property.Value);
                if (pattern == null || pattern.BitCount == 0)
                {
                    return this.CreateSettingsError(
                        "TRUE-PATTERN requires a non-empty bit pattern.");
                }

                settings.TruePattern = pattern;
            }
            else if (property.Kind == EcnEncodingPropertyKind.FalsePattern)
            {
                EcnBitField? pattern = this.ParsePattern(property.Value);
                if (pattern == null || pattern.BitCount == 0)
                {
                    return this.CreateSettingsError(
                        "FALSE-PATTERN requires a non-empty bit pattern.");
                }

                settings.FalsePattern = pattern;
            }
            else if (property.Kind == EcnEncodingPropertyKind.Pattern)
            {
                EcnBitField? pattern = this.ParsePattern(property.Value);
                if (pattern == null || pattern.BitCount == 0)
                {
                    return this.CreateSettingsError(
                        "PATTERN requires a non-empty bit pattern.");
                }

                settings.Pattern = pattern;
            }
            else if (property.Kind == EcnEncodingPropertyKind.Termination)
            {
                EcnBitField? terminator = this.ParseTermination(
                    property.Value);
                if (terminator == null || terminator.BitCount == 0)
                {
                    return this.CreateSettingsError(
                        "TERMINATION requires a supported non-empty pattern.");
                }

                settings.Terminator = terminator;
            }

            index = index + 1;
        }

        EcnPrimitiveSettingsResult range = this.ResolveRange(
            properties,
            settings);
        if (!range.Success)
        {
            return range;
        }

        EcnPrimitiveSettingsResult size = this.ResolveSize(
            properties,
            settings);
        if (!size.Success)
        {
            return size;
        }

        return new EcnPrimitiveSettingsResult
        {
            Success = true,
            Settings = settings
        };
    }

    internal EcnPrimitiveSettingsResult ResolveRange(
        EcnEncodingProperty[] properties,
        EcnPrimitiveSettings settings)
    {
        int index = 0;
        while (index < properties.Length)
        {
            EcnEncodingProperty property = properties[index];
            if (property.Kind == EcnEncodingPropertyKind.Range)
            {
                EcnBigIntegerParseResult lower = this.ParseInteger(
                    property.LowerBound);
                EcnBigIntegerParseResult upper = this.ParseInteger(
                    property.UpperBound);
                if (!lower.Success
                    || !upper.Success
                    || lower.Value > upper.Value)
                {
                    return this.CreateSettingsError(
                        "RANGE requires ordered integer bounds for primitive encoding.");
                }

                settings.HasRange = true;
                settings.LowerBound = lower.Value;
                settings.UpperBound = upper.Value;
                break;
            }

            index = index + 1;
        }

        return new EcnPrimitiveSettingsResult
        {
            Success = true,
            Settings = settings
        };
    }

    internal EcnPrimitiveSettingsResult ResolveSize(
        EcnEncodingProperty[] properties,
        EcnPrimitiveSettings settings)
    {
        EcnEncodingProperty? size = null;
        int index = 0;
        while (index < properties.Length)
        {
            EcnEncodingProperty property = properties[index];
            if (property.Kind == EcnEncodingPropertyKind.Size
                && (size == null
                    || property.Scope
                        == EcnEncodingPropertyScope.EncodingSpace))
            {
                size = property;
            }

            index = index + 1;
        }

        if (size == null || !size.HasIntegerValue || size.IntegerValue < 0)
        {
            return new EcnPrimitiveSettingsResult
            {
                Success = true,
                Settings = settings
            };
        }

        int unit = this.FindUnitBits(properties, size.Scope);
        if (unit <= 0
            || size.IntegerValue > int.MaxValue / unit)
        {
            return this.CreateSettingsError(
                "The ECN encoding-space size exceeds the supported bit range.");
        }

        settings.UnitBits = unit;
        settings.SizeBits = (int)size.IntegerValue * unit;
        if (settings.HasUnusedBits
            && settings.UnusedBits > settings.SizeBits)
        {
            return this.CreateSettingsError(
                "UNUSED BITS cannot exceed the encoding-space size.");
        }

        return new EcnPrimitiveSettingsResult
        {
            Success = true,
            Settings = settings
        };
    }

    internal int FindUnitBits(
        EcnEncodingProperty[] properties,
        EcnEncodingPropertyScope scope)
    {
        int index = 0;
        while (index < properties.Length)
        {
            EcnEncodingProperty property = properties[index];
            if (property.Kind == EcnEncodingPropertyKind.Multiple
                && property.Scope == scope)
            {
                return this.GetUnitBits(property.Unit);
            }

            index = index + 1;
        }

        return 1;
    }

    internal int GetUnitBits(string unit)
    {
        string normalized = unit.ToLowerInvariant();
        if (normalized == "bit" || normalized == "bits")
        {
            return 1;
        }

        if (normalized == "nibble")
        {
            return 4;
        }

        if (normalized == "octet")
        {
            return 8;
        }

        if (normalized == "word16")
        {
            return 16;
        }

        if (normalized == "dword32")
        {
            return 32;
        }

        EcnBigIntegerParseResult parsed = this.ParseInteger(normalized);
        if (parsed.Success
            && parsed.Value > System.Numerics.BigInteger.Zero
            && parsed.Value <= new System.Numerics.BigInteger(256))
        {
            return (int)parsed.Value;
        }

        return -1;
    }

    internal EcnBitField? ParseTermination(string value)
    {
        string normalized = value.ToLowerInvariant();
        if (normalized == "null-terminated"
            || normalized == "null"
            || normalized == "zero-octet")
        {
            return new EcnBitField
            {
                Data = [0],
                BitCount = 8
            };
        }

        return this.ParsePattern(value);
    }

    internal string ReadAlphabet(string value)
    {
        if (value.Length >= 2
            && value[0] == '"'
            && value[value.Length - 1] == '"')
        {
            System.Text.StringBuilder builder =
                new System.Text.StringBuilder();
            int index = 1;
            while (index < value.Length - 1)
            {
                if (value[index] == '"'
                    && index + 1 < value.Length - 1
                    && value[index + 1] == '"')
                {
                    builder.Append('"');
                    index = index + 2;
                }
                else
                {
                    builder.Append(value[index]);
                    index = index + 1;
                }
            }

            return builder.ToString();
        }

        return value;
    }

    internal EcnBitField? ParsePattern(string value)
    {
        int quote = value.IndexOf('\'');
        int close = value.LastIndexOf('\'');
        if (quote < 0 || close <= quote || close + 1 >= value.Length)
        {
            string named = value.ToLowerInvariant();
            if (named == "zero")
            {
                return new EcnBitField { Data = [0], BitCount = 1 };
            }

            if (named == "one")
            {
                return new EcnBitField { Data = [128], BitCount = 1 };
            }

            return null;
        }

        char suffix = value[close + 1];
        bool hexadecimal = suffix == 'H' || suffix == 'h';
        if (!hexadecimal && suffix != 'B' && suffix != 'b')
        {
            return null;
        }

        System.Collections.Generic.List<bool> bits =
            new System.Collections.Generic.List<bool>();
        int index = quote + 1;
        while (index < close)
        {
            char current = value[index];
            if (current == ' ' || current == '\t'
                || current == '\r' || current == '\n')
            {
                index = index + 1;
                continue;
            }

            if (hexadecimal)
            {
                int digit = this.GetHexDigit(current);
                if (digit < 0)
                {
                    return null;
                }

                bits.Add((digit & 8) != 0);
                bits.Add((digit & 4) != 0);
                bits.Add((digit & 2) != 0);
                bits.Add((digit & 1) != 0);
            }
            else if (current == '0' || current == '1')
            {
                bits.Add(current == '1');
            }
            else
            {
                return null;
            }

            index = index + 1;
        }

        return this.CreateField(bits);
    }

    internal EcnBitField CreateField(
        System.Collections.Generic.List<bool> bits)
    {
        byte[] data = new byte[(bits.Count + 7) / 8];
        int index = 0;
        while (index < bits.Count)
        {
            if (bits[index])
            {
                data[index / 8] = (byte)(data[index / 8]
                    | 1 << 7 - index % 8);
            }

            index = index + 1;
        }

        return new EcnBitField
        {
            Data = data,
            BitCount = bits.Count
        };
    }

    internal int GetHexDigit(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }

        return -1;
    }

    internal EcnBigIntegerParseResult ParseInteger(string text)
    {
        EcnBigIntegerParseResult result = new EcnBigIntegerParseResult();
        if (text.Length == 0)
        {
            return result;
        }

        bool negative = text[0] == '-';
        int index = negative || text[0] == '+' ? 1 : 0;
        if (index == text.Length)
        {
            return result;
        }

        System.Numerics.BigInteger value = System.Numerics.BigInteger.Zero;
        while (index < text.Length)
        {
            char current = text[index];
            if (current < '0' || current > '9')
            {
                return result;
            }

            value = value * 10 + current - '0';
            index = index + 1;
        }

        result.Success = true;
        result.Value = negative ? -value : value;
        return result;
    }

    internal EcnPrimitiveSettingsResult CreateSettingsError(string message)
    {
        return new EcnPrimitiveSettingsResult
        {
            ErrorMessage = message
        };
    }
}
