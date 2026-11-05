namespace asn1;

internal class OerTimeCodec
{
    internal AsnResourceLimits Limits;

    internal OerTimeCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal OerPrimitiveResult EncodeTime(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        bool canonical)
    {
        if (value.TimeValue == null)
        {
            return this.CreateTimeError(
                "ASNO0040",
                "The runtime value does not contain a time value.",
                false);
        }

        BerPrimitiveValueResult validated = new BerTimeCodec(this.Limits)
            .EncodeTime(
                type.Kind,
                value.TimeValue,
                canonical ? AsnEncodingRules.Der : AsnEncodingRules.Ber);
        if (!validated.Success)
        {
            return this.CopyBerFailure(validated);
        }

        if (type.Kind == AsnTypeKind.Date)
        {
            return this.EncodeDate(value);
        }

        if (type.Kind == AsnTypeKind.TimeOfDay)
        {
            return this.EncodeTimeOfDay(type, value);
        }

        if (type.Kind == AsnTypeKind.DateTime)
        {
            return this.EncodeDateTime(type, value);
        }

        if (type.Kind == AsnTypeKind.Duration)
        {
            return this.EncodeDuration(value);
        }

        if (type.Kind == AsnTypeKind.Time)
        {
            TimeBasicSettingKind basic = this.GetBasicTimeSetting(type);
            if (basic == TimeBasicSettingKind.Date)
            {
                return this.EncodeDate(value);
            }

            if (basic == TimeBasicSettingKind.Time)
            {
                return this.EncodeTimeOfDay(type, value);
            }

            if (basic == TimeBasicSettingKind.DateTime)
            {
                return this.EncodeDateTime(type, value);
            }

            if (basic == TimeBasicSettingKind.Interval)
            {
                return this.EncodeDuration(value);
            }
        }

        AsnEncodingRules rules = type.Kind == AsnTypeKind.Time
            ? AsnEncodingRules.Der
            : canonical ? AsnEncodingRules.Der : AsnEncodingRules.Ber;
        BerPrimitiveValueResult content = new BerTimeCodec(this.Limits)
            .EncodeTime(type.Kind, value.TimeValue, rules);
        if (!content.Success)
        {
            return this.CopyBerFailure(content);
        }

        return this.CopyEncodedContent(
            new OerContentCodec(this.Limits).EncodeVariableContent(
                content.Content),
            value);
    }

    internal OerPrimitiveResult DecodeTime(
        AsnTypeDescriptor type,
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        if (type.Kind == AsnTypeKind.Date)
        {
            return this.DecodeDate(data, offset, count, canonical);
        }

        if (type.Kind == AsnTypeKind.TimeOfDay)
        {
            return this.DecodeTimeOfDay(type, data, offset, count, canonical);
        }

        if (type.Kind == AsnTypeKind.DateTime)
        {
            return this.DecodeDateTime(type, data, offset, count, canonical);
        }

        if (type.Kind == AsnTypeKind.Duration)
        {
            return this.DecodeDuration(data, offset, count, canonical);
        }

        if (type.Kind == AsnTypeKind.Time)
        {
            TimeBasicSettingKind basic = this.GetBasicTimeSetting(type);
            OerPrimitiveResult optimized;
            if (basic == TimeBasicSettingKind.Date)
            {
                optimized = this.DecodeDate(data, offset, count, canonical);
                return this.RetagTimeResult(optimized);
            }

            if (basic == TimeBasicSettingKind.Time)
            {
                optimized = this.DecodeTimeOfDay(
                    type,
                    data,
                    offset,
                    count,
                    canonical);
                return this.RetagTimeResult(optimized);
            }

            if (basic == TimeBasicSettingKind.DateTime)
            {
                optimized = this.DecodeDateTime(
                    type,
                    data,
                    offset,
                    count,
                    canonical);
                return this.RetagTimeResult(optimized);
            }

            if (basic == TimeBasicSettingKind.Interval)
            {
                optimized = this.DecodeDuration(data, offset, count, canonical);
                return this.RetagTimeResult(optimized);
            }
        }

        OerContentResult content = new OerContentCodec(this.Limits)
            .DecodeVariableContent(data, offset, count, canonical);
        if (!content.Success)
        {
            return this.CopyContentFailure(content);
        }

        BerPrimitiveValueResult decoded = new BerTimeCodec(this.Limits)
            .DecodeTime(type.Kind, content.Content);
        if (!decoded.Success || decoded.Value == null
            || decoded.Value.TimeValue == null)
        {
            return this.CopyBerFailure(decoded);
        }

        if (type.Kind == AsnTypeKind.Time || canonical)
        {
            BerPrimitiveValueResult normalized = new BerTimeCodec(this.Limits)
                .EncodeTime(
                    type.Kind,
                    decoded.Value.TimeValue,
                    AsnEncodingRules.Der);
            if (!normalized.Success
                || !this.BytesEqual(normalized.Content, content.Content))
            {
                return this.CreateTimeError(
                    "ASNO0041",
                    "The OER time content does not use its required DER form.",
                    false);
            }
        }

        return this.CreateDecodedTime(
            decoded.Value.TimeValue,
            content.Data,
            content.ConsumedCount,
            content.Minimal);
    }

    internal OerPrimitiveResult EncodeDate(AsnRuntimeValue value)
    {
        string text = value.TimeValue == null ? "" : value.TimeValue.Text;
        int year = this.ParseNumber(text, 0, 4);
        int month = this.ParseNumber(text, 5, 2);
        int day = this.ParseNumber(text, 8, 2);
        ByteAccumulator output = new ByteAccumulator(5);
        OerIntegerResult encodedYear = new OerIntegerCodec(this.Limits)
            .EncodeInteger(this.CreateUnboundedIntegerType(), year);
        if (!encodedYear.Success)
        {
            return this.CopyIntegerFailure(encodedYear);
        }

        output.AppendBytes(encodedYear.Data);
        output.AppendByte((byte)month);
        output.AppendByte((byte)day);
        return this.CreateEncodedTime(value, output.ToArray());
    }

    internal OerPrimitiveResult DecodeDate(
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        OerIntegerResult year = new OerIntegerCodec(this.Limits).DecodeInteger(
            this.CreateUnboundedIntegerType(),
            data,
            offset,
            count,
            canonical);
        if (!year.Success)
        {
            return this.CopyIntegerFailure(year);
        }

        if (year.Value < System.Numerics.BigInteger.Zero
            || year.Value > new System.Numerics.BigInteger(9999))
        {
            return this.CreateTimeError(
                "ASNO0042",
                "The decoded DATE year is outside the supported lexical range.",
                false);
        }

        if (count - year.ConsumedCount < 2)
        {
            return new OerPrimitiveResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = count,
                HasViolationOffset = true,
                ViolationOffset = count
            };
        }

        int month = data[offset + year.ConsumedCount];
        int day = data[offset + year.ConsumedCount + 1];
        string text = this.FormatNumber((int)year.Value, 4)
            + "-" + this.FormatNumber(month, 2)
            + "-" + this.FormatNumber(day, 2);
        AsnTimeValue time = new AsnTimeValue
        {
            Kind = AsnTimeKind.Date,
            Text = text
        };
        TimeValidationResult validation = new TimeValueValidator()
            .ValidateTime(time);
        if (!validation.Success)
        {
            return this.CreateTimeError(
                "ASNO0043",
                validation.ErrorMessage,
                false);
        }

        int consumed = year.ConsumedCount + 2;
        return this.CreateDecodedTime(
            time,
            this.CopySegment(data, offset, consumed),
            consumed,
            year.Minimal);
    }

    internal OerPrimitiveResult EncodeTimeOfDay(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        AsnTimeValue time = value.TimeValue
            ?? throw new System.InvalidOperationException();
        int fractionalDigits = this.GetFractionalDigits(type);
        bool fraction = fractionalDigits > 0;
        TimeZoneSettingKind zoneSetting = this.GetZoneSetting(type);
        bool difference = zoneSetting == TimeZoneSettingKind.LocalDifference;
        if ((time.HasFractionalDigits && !fraction)
            || (this.HasExplicitDifference(time.Text) && !difference))
        {
            return this.CreateTimeError(
                "ASNO0044",
                "The time value needs matching OER property settings for fractions or UTC differences.",
                false);
        }

        string body = this.RemoveZone(time.Text);
        int separator = this.FindFraction(body);
        string integral = separator < 0 ? body : body.Substring(0, separator);
        ByteAccumulator output = new ByteAccumulator(8);
        output.AppendByte((byte)this.ParseNumber(integral, 0, 2));
        output.AppendByte((byte)this.ParseNumber(integral, 3, 2));
        output.AppendByte((byte)this.ParseNumber(integral, 6, 2));
        if (fraction)
        {
            string digits = separator < 0 ? "0" : body.Substring(separator + 1);
            OerIntegerResult encodedFraction = new OerIntegerCodec(this.Limits)
                .EncodeInteger(
                    this.CreateNonnegativeIntegerType(),
                    System.Numerics.BigInteger.Parse(
                        digits,
                        System.Globalization.CultureInfo.InvariantCulture));
            if (!encodedFraction.Success)
            {
                return this.CopyIntegerFailure(encodedFraction);
            }

            output.AppendBytes(encodedFraction.Data);
        }

        if (difference)
        {
            OerIntegerResult encodedDifference = new OerIntegerCodec(this.Limits)
                .EncodeInteger(
                    this.CreateBoundedIntegerType(-900, 900),
                    time.UtcOffsetMinutes);
            if (!encodedDifference.Success)
            {
                return this.CopyIntegerFailure(encodedDifference);
            }

            output.AppendBytes(encodedDifference.Data);
        }

        return this.CreateEncodedTime(value, output.ToArray());
    }

    internal OerPrimitiveResult DecodeTimeOfDay(
        AsnTypeDescriptor type,
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        if (count < 3)
        {
            return new OerPrimitiveResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = count,
                HasViolationOffset = true,
                ViolationOffset = count
            };
        }

        int cursor = 3;
        int fractionalDigits = this.GetFractionalDigits(type);
        bool fraction = fractionalDigits > 0;
        TimeZoneSettingKind zoneSetting = this.GetZoneSetting(type);
        bool difference = zoneSetting == TimeZoneSettingKind.LocalDifference;
        string text = this.FormatNumber(data[offset], 2)
            + ":" + this.FormatNumber(data[offset + 1], 2)
            + ":" + this.FormatNumber(data[offset + 2], 2);
        bool minimal = true;
        if (fraction)
        {
            OerIntegerResult decodedFraction = new OerIntegerCodec(this.Limits)
                .DecodeInteger(
                    this.CreateNonnegativeIntegerType(),
                    data,
                    offset + cursor,
                    count - cursor,
                    canonical);
            if (!decodedFraction.Success)
            {
                return this.CopyIntegerFailure(decodedFraction);
            }

            string digits = decodedFraction.Value.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            if (digits.Length < fractionalDigits)
            {
                digits = new string(
                    '0',
                    fractionalDigits - digits.Length) + digits;
            }

            text = text + "." + digits;
            cursor = cursor + decodedFraction.ConsumedCount;
            minimal = decodedFraction.Minimal;
        }

        AsnTimeValue time = new AsnTimeValue
        {
            Kind = AsnTimeKind.TimeOfDay,
            Text = text,
            HasFractionalDigits = fraction,
            FractionalDigits = fractionalDigits
        };
        if (difference)
        {
            OerIntegerResult decodedDifference = new OerIntegerCodec(this.Limits)
                .DecodeInteger(
                    this.CreateBoundedIntegerType(-900, 900),
                    data,
                    offset + cursor,
                    count - cursor,
                    canonical);
            if (!decodedDifference.Success)
            {
                return this.CopyIntegerFailure(decodedDifference);
            }

            int minutes = (int)decodedDifference.Value;
            text = text + this.FormatDifference(minutes);
            time.Text = text;
            time.HasUtcOffsetMinutes = true;
            time.UtcOffsetMinutes = minutes;
            cursor = cursor + decodedDifference.ConsumedCount;
            minimal = minimal && decodedDifference.Minimal;
        }
        else if (zoneSetting == TimeZoneSettingKind.Utc)
        {
            time.Text = time.Text + "Z";
            time.HasUtcOffsetMinutes = true;
        }

        TimeValidationResult validation = new TimeValueValidator()
            .ValidateTime(time);
        if (!validation.Success)
        {
            return this.CreateTimeError(
                "ASNO0045",
                validation.ErrorMessage,
                false);
        }

        return this.CreateDecodedTime(
            time,
            this.CopySegment(data, offset, cursor),
            cursor,
            minimal);
    }

    internal OerPrimitiveResult EncodeDateTime(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        AsnTimeValue time = value.TimeValue
            ?? throw new System.InvalidOperationException();
        string[] parts = time.Text.Split('T');
        AsnRuntimeValue dateValue = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Time,
            TimeValue = new AsnTimeValue
            {
                Kind = AsnTimeKind.Date,
                Text = parts[0]
            }
        };
        AsnRuntimeValue clockValue = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Time,
            TimeValue = new AsnTimeValue
            {
                Kind = AsnTimeKind.TimeOfDay,
                Text = parts[1],
                HasFractionalDigits = time.HasFractionalDigits,
                FractionalDigits = time.FractionalDigits,
                HasUtcOffsetMinutes = time.HasUtcOffsetMinutes,
                UtcOffsetMinutes = time.UtcOffsetMinutes
            }
        };
        OerPrimitiveResult date = this.EncodeDate(dateValue);
        OerPrimitiveResult clock = this.EncodeTimeOfDay(type, clockValue);
        if (!date.Success)
        {
            return date;
        }

        if (!clock.Success)
        {
            return clock;
        }

        byte[] data = new byte[date.Data.Length + clock.Data.Length];
        System.Buffer.BlockCopy(date.Data, 0, data, 0, date.Data.Length);
        System.Buffer.BlockCopy(
            clock.Data,
            0,
            data,
            date.Data.Length,
            clock.Data.Length);
        return this.CreateEncodedTime(value, data);
    }

    internal OerPrimitiveResult DecodeDateTime(
        AsnTypeDescriptor type,
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        OerPrimitiveResult date = this.DecodeDate(
            data,
            offset,
            count,
            canonical);
        if (!date.Success || date.Value == null || date.Value.TimeValue == null)
        {
            return date;
        }

        OerPrimitiveResult clock = this.DecodeTimeOfDay(
            type,
            data,
            offset + date.ConsumedCount,
            count - date.ConsumedCount,
            canonical);
        if (!clock.Success || clock.Value == null || clock.Value.TimeValue == null)
        {
            return clock;
        }

        AsnTimeValue clockTime = clock.Value.TimeValue;
        AsnTimeValue time = new AsnTimeValue
        {
            Kind = AsnTimeKind.DateTime,
            Text = date.Value.TimeValue.Text + "T" + clockTime.Text,
            HasFractionalDigits = clockTime.HasFractionalDigits,
            FractionalDigits = clockTime.FractionalDigits,
            HasUtcOffsetMinutes = clockTime.HasUtcOffsetMinutes,
            UtcOffsetMinutes = clockTime.UtcOffsetMinutes
        };
        int consumed = date.ConsumedCount + clock.ConsumedCount;
        return this.CreateDecodedTime(
            time,
            this.CopySegment(data, offset, consumed),
            consumed,
            date.Minimal && clock.Minimal);
    }

    internal OerPrimitiveResult EncodeDuration(AsnRuntimeValue value)
    {
        string text = value.TimeValue == null ? "" : value.TimeValue.Text;
        OerDurationParts parts = this.ParseDuration(text);
        if (!parts.Success)
        {
            return this.CreateTimeError(
                "ASNO0046",
                parts.ErrorMessage,
                false);
        }

        int bitmap = parts.FractionPresent ? 1 : 0;
        int index = 0;
        while (index < parts.Present.Length)
        {
            if (parts.Present[index])
            {
                bitmap = bitmap | 1 << (7 - index);
            }

            index = index + 1;
        }

        ByteAccumulator output = new ByteAccumulator(16);
        output.AppendByte((byte)bitmap);
        index = 0;
        while (index < parts.Present.Length)
        {
            if (parts.Present[index])
            {
                OerIntegerResult encoded = new OerIntegerCodec(this.Limits)
                    .EncodeInteger(
                        this.CreateNonnegativeIntegerType(),
                        parts.Values[index]);
                if (!encoded.Success)
                {
                    return this.CopyIntegerFailure(encoded);
                }

                output.AppendBytes(encoded.Data);
            }

            index = index + 1;
        }

        if (parts.FractionPresent)
        {
            OerIntegerResult digits = new OerIntegerCodec(this.Limits)
                .EncodeInteger(
                    this.CreateNonnegativeIntegerType(),
                    parts.FractionDigits);
            OerIntegerResult fraction = new OerIntegerCodec(this.Limits)
                .EncodeInteger(
                    this.CreateNonnegativeIntegerType(),
                    parts.FractionValue);
            if (!digits.Success)
            {
                return this.CopyIntegerFailure(digits);
            }

            if (!fraction.Success)
            {
                return this.CopyIntegerFailure(fraction);
            }

            output.AppendBytes(digits.Data);
            output.AppendBytes(fraction.Data);
        }

        return this.CreateEncodedTime(value, output.ToArray());
    }

    internal OerPrimitiveResult DecodeDuration(
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        if (count == 0)
        {
            return new OerPrimitiveResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                HasViolationOffset = true,
                ViolationOffset = 0
            };
        }

        int bitmap = data[offset];
        int cursor = 1;
        OerDurationParts parts = new OerDurationParts { Success = true };
        bool minimal = true;
        int index = 0;
        while (index < parts.Present.Length)
        {
            parts.Present[index] = (bitmap & 1 << (7 - index)) != 0;
            if (parts.Present[index])
            {
                OerIntegerResult decoded = new OerIntegerCodec(this.Limits)
                    .DecodeInteger(
                        this.CreateNonnegativeIntegerType(),
                        data,
                        offset + cursor,
                        count - cursor,
                        canonical);
                if (!decoded.Success)
                {
                    return this.CopyIntegerFailure(decoded);
                }

                parts.Values[index] = decoded.Value;
                cursor = cursor + decoded.ConsumedCount;
                minimal = minimal && decoded.Minimal;
            }

            index = index + 1;
        }

        parts.FractionPresent = (bitmap & 1) != 0;
        if (parts.FractionPresent)
        {
            OerIntegerResult digits = new OerIntegerCodec(this.Limits)
                .DecodeInteger(
                    this.CreateNonnegativeIntegerType(),
                    data,
                    offset + cursor,
                    count - cursor,
                    canonical);
            if (!digits.Success)
            {
                return this.CopyIntegerFailure(digits);
            }

            cursor = cursor + digits.ConsumedCount;
            OerIntegerResult fraction = new OerIntegerCodec(this.Limits)
                .DecodeInteger(
                    this.CreateNonnegativeIntegerType(),
                    data,
                    offset + cursor,
                    count - cursor,
                    canonical);
            if (!fraction.Success)
            {
                return this.CopyIntegerFailure(fraction);
            }

            if (digits.Value <= System.Numerics.BigInteger.Zero
                || digits.Value > new System.Numerics.BigInteger(
                    this.Limits.MaximumTextCharacters))
            {
                return this.CreateTimeError(
                    "ASNO0047",
                    "The DURATION fractional precision is invalid or too large.",
                    false);
            }

            parts.FractionDigits = (int)digits.Value;
            parts.FractionValue = fraction.Value;
            string fractionText = parts.FractionValue.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            if (fractionText.Length > parts.FractionDigits)
            {
                return this.CreateTimeError(
                    "ASNO0047",
                    "The DURATION fractional value exceeds its declared precision.",
                    false);
            }

            cursor = cursor + fraction.ConsumedCount;
            minimal = minimal && digits.Minimal && fraction.Minimal;
        }

        string text = this.FormatDuration(parts);
        AsnTimeValue time = new AsnTimeValue
        {
            Kind = AsnTimeKind.Duration,
            Text = text,
            HasFractionalDigits = parts.FractionPresent,
            FractionalDigits = parts.FractionDigits
        };
        TimeValidationResult validation = new TimeValueValidator()
            .ValidateTime(time);
        if (!validation.Success)
        {
            return this.CreateTimeError(
                "ASNO0048",
                validation.ErrorMessage,
                false);
        }

        return this.CreateDecodedTime(
            time,
            this.CopySegment(data, offset, cursor),
            cursor,
            minimal);
    }

    internal OerDurationParts ParseDuration(string text)
    {
        OerDurationParts result = new OerDurationParts();
        if (text.StartsWith("-", System.StringComparison.Ordinal))
        {
            result.ErrorMessage = "Negative DURATION values are not supported by the optimized OER interval form.";
            return result;
        }

        int index = 1;
        bool time = false;
        while (index < text.Length)
        {
            if (text[index] == 'T')
            {
                time = true;
                index = index + 1;
                continue;
            }

            int start = index;
            while (index < text.Length
                && text[index] >= '0'
                && text[index] <= '9')
            {
                index = index + 1;
            }

            int separator = -1;
            if (index < text.Length
                && (text[index] == '.' || text[index] == ','))
            {
                separator = index;
                index = index + 1;
                while (index < text.Length
                    && text[index] >= '0'
                    && text[index] <= '9')
                {
                    index = index + 1;
                }
            }

            char designator = text[index];
            int part = this.GetDurationPart(designator, time);
            string whole = separator < 0
                ? text.Substring(start, index - start)
                : text.Substring(start, separator - start);
            result.Present[part] = true;
            result.Values[part] = System.Numerics.BigInteger.Parse(
                whole,
                System.Globalization.CultureInfo.InvariantCulture);
            if (separator >= 0)
            {
                string fraction = text.Substring(
                    separator + 1,
                    index - separator - 1);
                result.FractionPresent = true;
                result.FractionDigits = fraction.Length;
                result.FractionValue = System.Numerics.BigInteger.Parse(
                    fraction,
                    System.Globalization.CultureInfo.InvariantCulture);
            }

            index = index + 1;
        }

        result.Success = true;
        return result;
    }

    internal string FormatDuration(OerDurationParts parts)
    {
        string result = "P";
        bool time = false;
        int last = -1;
        int index = 0;
        while (index < parts.Present.Length)
        {
            if (parts.Present[index])
            {
                last = index;
            }

            index = index + 1;
        }

        index = 0;
        while (index < parts.Present.Length)
        {
            if (parts.Present[index])
            {
                if (index >= 4 && !time)
                {
                    result = result + "T";
                    time = true;
                }

                string number = parts.Values[index].ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
                if (parts.FractionPresent && index == last)
                {
                    string fraction = parts.FractionValue.ToString(
                        System.Globalization.CultureInfo.InvariantCulture);
                    if (fraction.Length < parts.FractionDigits)
                    {
                        fraction = new string(
                            '0',
                            parts.FractionDigits - fraction.Length) + fraction;
                    }

                    number = number + "." + fraction;
                }

                result = result + number + this.GetDurationDesignator(index);
            }

            index = index + 1;
        }

        return result;
    }

    internal int GetDurationPart(char designator, bool time)
    {
        if (designator == 'Y')
        {
            return 0;
        }

        if (designator == 'M')
        {
            return time ? 5 : 1;
        }

        if (designator == 'W')
        {
            return 2;
        }

        if (designator == 'D')
        {
            return 3;
        }

        if (designator == 'H')
        {
            return 4;
        }

        return 6;
    }

    internal string GetDurationDesignator(int part)
    {
        if (part == 0)
        {
            return "Y";
        }

        if (part == 1 || part == 5)
        {
            return "M";
        }

        if (part == 2)
        {
            return "W";
        }

        if (part == 3)
        {
            return "D";
        }

        return part == 4 ? "H" : "S";
    }

    internal int GetFractionalDigits(AsnTypeDescriptor type)
    {
        if (type.Resolution == null)
        {
            return 0;
        }

        TimePropertySettings[] settings = type.Resolution
            .EncodingConstraints.Oer.TimeSettings;
        int index = settings.Length - 1;
        while (index >= 0)
        {
            if (settings[index].FractionalDigits > 0)
            {
                return settings[index].FractionalDigits;
            }

            index = index - 1;
        }

        return 0;
    }

    internal TimeZoneSettingKind GetZoneSetting(AsnTypeDescriptor type)
    {
        if (type.Resolution == null)
        {
            return TimeZoneSettingKind.Unspecified;
        }

        TimePropertySettings[] settings = type.Resolution
            .EncodingConstraints.Oer.TimeSettings;
        int index = settings.Length - 1;
        while (index >= 0)
        {
            if (settings[index].Zone != TimeZoneSettingKind.Unspecified)
            {
                return settings[index].Zone;
            }

            index = index - 1;
        }

        return TimeZoneSettingKind.Unspecified;
    }

    internal TimeBasicSettingKind GetBasicTimeSetting(AsnTypeDescriptor type)
    {
        if (type.Resolution == null)
        {
            return TimeBasicSettingKind.Unspecified;
        }

        TimePropertySettings[] settings = type.Resolution
            .EncodingConstraints.Oer.TimeSettings;
        int index = settings.Length - 1;
        while (index >= 0)
        {
            if (settings[index].Basic != TimeBasicSettingKind.Unspecified)
            {
                return settings[index].Basic;
            }

            index = index - 1;
        }

        return TimeBasicSettingKind.Unspecified;
    }

    internal OerPrimitiveResult RetagTimeResult(OerPrimitiveResult result)
    {
        if (result.Success
            && result.Value != null
            && result.Value.TimeValue != null)
        {
            result.Value.TimeValue.Kind = AsnTimeKind.Time;
        }

        return result;
    }

    internal AsnTypeDescriptor CreateUnboundedIntegerType()
    {
        return new AsnTypeDescriptor { Kind = AsnTypeKind.Integer };
    }

    internal AsnTypeDescriptor CreateNonnegativeIntegerType()
    {
        return new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Integer,
            Constraints = [new AsnConstraintDescriptor
            {
                DomainKind = AsnTypeKind.Integer,
                RootIntervals = [new AsnIntervalDescriptor
                {
                    HasLowerBound = true,
                    LowerBound = System.Numerics.BigInteger.Zero
                }]
            }]
        };
    }

    internal AsnTypeDescriptor CreateBoundedIntegerType(int lower, int upper)
    {
        return new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Integer,
            Constraints = [new AsnConstraintDescriptor
            {
                DomainKind = AsnTypeKind.Integer,
                RootIntervals = [new AsnIntervalDescriptor
                {
                    HasLowerBound = true,
                    LowerBound = new System.Numerics.BigInteger(lower),
                    HasUpperBound = true,
                    UpperBound = new System.Numerics.BigInteger(upper)
                }]
            }]
        };
    }

    internal bool HasExplicitDifference(string text)
    {
        int plus = text.LastIndexOf('+');
        int minus = text.LastIndexOf('-');
        return plus >= 8 || minus >= 8;
    }

    internal string RemoveZone(string text)
    {
        if (text.EndsWith("Z", System.StringComparison.Ordinal))
        {
            return text.Substring(0, text.Length - 1);
        }

        int plus = text.LastIndexOf('+');
        int minus = text.LastIndexOf('-');
        int zone = plus > minus ? plus : minus;
        return zone >= 8 ? text.Substring(0, zone) : text;
    }

    internal int FindFraction(string text)
    {
        int separator = text.IndexOf('.');
        return separator < 0 ? text.IndexOf(',') : separator;
    }

    internal string FormatDifference(int minutes)
    {
        string sign = minutes < 0 ? "-" : "+";
        int absolute = minutes < 0 ? -minutes : minutes;
        return sign + this.FormatNumber(absolute / 60, 2)
            + ":" + this.FormatNumber(absolute % 60, 2);
    }

    internal int ParseNumber(string text, int start, int count)
    {
        return int.Parse(
            text.Substring(start, count),
            System.Globalization.CultureInfo.InvariantCulture);
    }

    internal string FormatNumber(int value, int digits)
    {
        return value.ToString(
            "D" + digits.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            System.Globalization.CultureInfo.InvariantCulture);
    }

    internal byte[] CopySegment(byte[] data, int offset, int count)
    {
        byte[] result = new byte[count];
        System.Buffer.BlockCopy(data, offset, result, 0, count);
        return result;
    }

    internal bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal OerPrimitiveResult CopyEncodedContent(
        OerContentResult content,
        AsnRuntimeValue value)
    {
        if (!content.Success)
        {
            return this.CopyContentFailure(content);
        }

        return new OerPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Data = content.Data,
            ConsumedCount = content.Data.Length,
            Minimal = content.Minimal
        };
    }

    internal OerPrimitiveResult CopyContentFailure(OerContentResult content)
    {
        return new OerPrimitiveResult
        {
            Status = content.Status,
            ConsumedCount = content.ConsumedCount,
            Minimal = content.Minimal,
            HasViolationOffset = content.HasViolationOffset,
            ViolationOffset = content.ViolationOffset,
            LimitExceeded = content.LimitExceeded,
            ErrorCode = content.ErrorCode,
            ErrorMessage = content.ErrorMessage
        };
    }

    internal OerPrimitiveResult CopyIntegerFailure(OerIntegerResult integer)
    {
        return new OerPrimitiveResult
        {
            Status = integer.Status,
            ConsumedCount = integer.ConsumedCount,
            Minimal = integer.Minimal,
            HasViolationOffset = integer.HasViolationOffset,
            ViolationOffset = integer.ViolationOffset,
            LimitExceeded = integer.LimitExceeded,
            ErrorCode = integer.ErrorCode,
            ErrorMessage = integer.ErrorMessage
        };
    }

    internal OerPrimitiveResult CopyBerFailure(BerPrimitiveValueResult result)
    {
        return this.CreateTimeError(
            result.ErrorCode,
            result.ErrorMessage,
            result.LimitExceeded);
    }

    internal OerPrimitiveResult CreateEncodedTime(
        AsnRuntimeValue value,
        byte[] data)
    {
        return new OerPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Data = data,
            ConsumedCount = data.Length
        };
    }

    internal OerPrimitiveResult CreateDecodedTime(
        AsnTimeValue time,
        byte[] data,
        int consumed,
        bool minimal)
    {
        return new OerPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Time,
                TimeValue = time
            },
            Data = data,
            ConsumedCount = consumed,
            Minimal = minimal
        };
    }

    internal OerPrimitiveResult CreateTimeError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new OerPrimitiveResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
