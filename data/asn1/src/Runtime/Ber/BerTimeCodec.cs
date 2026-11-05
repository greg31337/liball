namespace asn1;

internal class BerTimeCodec
{
    internal AsnResourceLimits Limits;

    internal BerTimeCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal BerPrimitiveValueResult EncodeTime(
        AsnTypeKind kind,
        AsnTimeValue value,
        AsnEncodingRules rules)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (!this.TimeKindMatches(kind, value.Kind))
        {
            return this.CreateTimeError(
                "ASNE0036",
                "The time value category does not match the requested ASN.1 type.",
                false);
        }

        if (value.Text == null)
        {
            return this.CreateTimeError(
                "ASNE0037",
                "A useful or time value cannot have null text.",
                false);
        }

        TimeValidationResult validation = new TimeValueValidator()
            .ValidateTime(value);
        if (!validation.Success)
        {
            return this.CreateTimeError(
                "ASNE0037",
                validation.ErrorMessage,
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsTextCharacters(
            value.Text.Length))
        {
            return this.CreateTimeError(
                "ASNR0009",
                "The time text exceeds the configured text limit.",
                true);
        }

        string normalizedText = value.Text;
        if (rules == AsnEncodingRules.Der
            || rules == AsnEncodingRules.Cer)
        {
            DerTimeNormalizationResult normalized = new DerTimeNormalizer()
                .NormalizeTime(kind, value.Text);
            if (!normalized.Success)
            {
                return this.CreateTimeError(
                    "ASNE0037",
                    normalized.ErrorMessage,
                    false);
            }

            normalizedText = normalized.Text;
        }

        string encodedText = this.RemoveTimeSeparators(kind, normalizedText);
        if (!new ResourceCounter(this.Limits).AllowsTextCharacters(
            encodedText.Length))
        {
            return this.CreateTimeError(
                "ASNR0009",
                "The normalized time text exceeds the configured text limit.",
                true);
        }

        return new BerPrimitiveValueResult
        {
            Success = true,
            Content = System.Text.Encoding.UTF8.GetBytes(encodedText)
        };
    }

    internal BerPrimitiveValueResult DecodeTime(
        AsnTypeKind kind,
        byte[] content)
    {
        if (content == null)
        {
            throw new System.ArgumentNullException("content");
        }

        BerPrimitiveValueResult decoded = new BerCharacterStringCodec(
            this.Limits).DecodeText(AsnTypeKind.Utf8String, content);
        if (!decoded.Success || decoded.Value == null)
        {
            return decoded;
        }

        string text = this.RestoreTimeSeparators(
            kind,
            decoded.Value.TextValue);
        if (text.Length == 0)
        {
            return this.CreateTimeError(
                "ASND0057",
                "The encoded time value does not have the required lexical form.",
                false);
        }

        AsnTimeValue value = new AsnTimeValue
        {
            Kind = this.GetTimeKind(kind),
            Text = text
        };
        this.PopulateTimeMetadata(value);
        TimeValidationResult validation = new TimeValueValidator()
            .ValidateTime(value);
        if (!validation.Success)
        {
            return this.CreateTimeError(
                "ASND0058",
                validation.ErrorMessage,
                false);
        }

        return new BerPrimitiveValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Time,
                TimeValue = value
            }
        };
    }

    internal string RemoveTimeSeparators(AsnTypeKind kind, string text)
    {
        if (kind == AsnTypeKind.UtcTime
            || kind == AsnTypeKind.GeneralizedTime
            || kind == AsnTypeKind.Time)
        {
            return text;
        }

        TextAccumulator result = new TextAccumulator(text.Length);
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            bool remove = kind == AsnTypeKind.Date
                    && (index == 4 || index == 7)
                || kind == AsnTypeKind.TimeOfDay && current == ':'
                || kind == AsnTypeKind.DateTime
                    && (index == 4
                        || index == 7
                        || index == 10
                        || current == ':')
                || kind == AsnTypeKind.Duration
                    && current == 'P'
                    && (index == 0 || index == 1);
            if (!remove)
            {
                result.AppendCharacter(current);
            }

            index = index + 1;
        }

        return result.ToText();
    }

    internal string RestoreTimeSeparators(AsnTypeKind kind, string text)
    {
        if (kind == AsnTypeKind.UtcTime
            || kind == AsnTypeKind.GeneralizedTime)
        {
            return text;
        }

        if (kind == AsnTypeKind.Time)
        {
            return this.ExpandIntegralHourDifference(text);
        }

        if (kind == AsnTypeKind.Date)
        {
            if (text.Length != 8 || !this.AreDigits(text, 0, 8))
            {
                return "";
            }

            return text.Substring(0, 4) + "-"
                + text.Substring(4, 2) + "-"
                + text.Substring(6, 2);
        }

        if (kind == AsnTypeKind.TimeOfDay)
        {
            if (text.Length < 6 || !this.AreDigits(text, 0, 6))
            {
                return "";
            }

            return this.ExpandIntegralHourDifference(
                text.Substring(0, 2) + ":"
                + text.Substring(2, 2) + ":"
                + text.Substring(4));
        }

        if (kind == AsnTypeKind.DateTime)
        {
            if (text.Length < 14 || !this.AreDigits(text, 0, 14))
            {
                return "";
            }

            return this.ExpandIntegralHourDifference(
                text.Substring(0, 4) + "-"
                + text.Substring(4, 2) + "-"
                + text.Substring(6, 2) + "T"
                + text.Substring(8, 2) + ":"
                + text.Substring(10, 2) + ":"
                + text.Substring(12));
        }

        if (kind == AsnTypeKind.Duration)
        {
            if (text.Length == 0)
            {
                return "";
            }

            return text[0] == '-'
                ? "-P" + text.Substring(1)
                : "P" + text;
        }

        return "";
    }

    internal string ExpandIntegralHourDifference(string text)
    {
        int plus = text.LastIndexOf('+');
        int minus = text.LastIndexOf('-');
        int zone = plus > minus ? plus : minus;
        if (zone >= 8 && text.Length - zone == 3)
        {
            return text + "00";
        }

        return text;
    }

    internal bool TimeKindMatches(AsnTypeKind typeKind, AsnTimeKind timeKind)
    {
        return this.GetTimeKind(typeKind) == timeKind
            && this.GetUniversalTagNumber(typeKind) >= 0;
    }

    internal AsnTimeKind GetTimeKind(AsnTypeKind kind)
    {
        if (kind == AsnTypeKind.UtcTime)
        {
            return AsnTimeKind.UtcTime;
        }

        if (kind == AsnTypeKind.GeneralizedTime)
        {
            return AsnTimeKind.GeneralizedTime;
        }

        if (kind == AsnTypeKind.Date)
        {
            return AsnTimeKind.Date;
        }

        if (kind == AsnTypeKind.TimeOfDay)
        {
            return AsnTimeKind.TimeOfDay;
        }

        if (kind == AsnTypeKind.DateTime)
        {
            return AsnTimeKind.DateTime;
        }

        if (kind == AsnTypeKind.Duration)
        {
            return AsnTimeKind.Duration;
        }

        return AsnTimeKind.Time;
    }

    internal int GetUniversalTagNumber(AsnTypeKind kind)
    {
        if (kind == AsnTypeKind.UtcTime)
        {
            return AsnUniversalTags.UtcTime;
        }

        if (kind == AsnTypeKind.GeneralizedTime)
        {
            return AsnUniversalTags.GeneralizedTime;
        }

        if (kind == AsnTypeKind.Time)
        {
            return AsnUniversalTags.Time;
        }

        if (kind == AsnTypeKind.Date)
        {
            return AsnUniversalTags.Date;
        }

        if (kind == AsnTypeKind.TimeOfDay)
        {
            return AsnUniversalTags.TimeOfDay;
        }

        if (kind == AsnTypeKind.DateTime)
        {
            return AsnUniversalTags.DateTime;
        }

        if (kind == AsnTypeKind.Duration)
        {
            return AsnUniversalTags.Duration;
        }

        return -1;
    }

    internal bool AreDigits(string text, int start, int count)
    {
        if (start < 0 || count < 0 || start > text.Length - count)
        {
            return false;
        }

        int index = start;
        while (index < start + count)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void PopulateTimeMetadata(AsnTimeValue time)
    {
        int separator = time.Text.IndexOf('.');
        if (separator < 0)
        {
            separator = time.Text.IndexOf(',');
        }

        if (separator >= 0)
        {
            int index = separator + 1;
            int digits = 0;
            while (index < time.Text.Length
                && time.Text[index] >= '0'
                && time.Text[index] <= '9')
            {
                digits = digits + 1;
                index = index + 1;
            }

            time.HasFractionalDigits = digits > 0;
            time.FractionalDigits = digits;
        }

        if (time.Kind == AsnTimeKind.Duration)
        {
            return;
        }

        if (time.Text.EndsWith("Z", System.StringComparison.Ordinal))
        {
            time.HasUtcOffsetMinutes = true;
            return;
        }

        int plus = time.Text.LastIndexOf('+');
        int minus = time.Text.LastIndexOf('-');
        int zone = plus > minus ? plus : minus;
        if (zone <= 7 || zone >= time.Text.Length - 2)
        {
            return;
        }

        string zoneText = time.Text.Substring(zone + 1).Replace(":", "");
        if (zoneText.Length != 4 || !this.AreDigits(zoneText, 0, 4))
        {
            return;
        }

        int hours = int.Parse(
            zoneText.Substring(0, 2),
            System.Globalization.CultureInfo.InvariantCulture);
        int minutes = int.Parse(
            zoneText.Substring(2, 2),
            System.Globalization.CultureInfo.InvariantCulture);
        int offset = hours * 60 + minutes;
        time.HasUtcOffsetMinutes = true;
        time.UtcOffsetMinutes = time.Text[zone] == '-' ? -offset : offset;
    }

    internal BerPrimitiveValueResult CreateTimeError(
        string code,
        string message,
        bool resource)
    {
        return new BerPrimitiveValueResult
        {
            LimitExceeded = resource,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
