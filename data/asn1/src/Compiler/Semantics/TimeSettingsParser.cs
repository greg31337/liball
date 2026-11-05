namespace asn1;

internal class TimeSettingsParser
{
    internal string Source;
    internal int Position;
    internal System.Collections.Generic.List<string> PropertyNames =
        new System.Collections.Generic.List<string>();

    internal TimeSettingsParser(string source)
    {
        this.Source = source == null
            ? throw new System.ArgumentNullException("source")
            : source;
    }

    internal TimeSettingsParseResult ParseSettings()
    {
        TimePropertySettings settings = new TimePropertySettings
        {
            Source = this.Source
        };
        this.SkipWhitespace();
        if (this.Position == this.Source.Length)
        {
            return this.CreateFailure(
                ConstraintDiagnosticCodes.InvalidTimeSettings,
                "The property settings list is empty.",
                0);
        }

        while (this.Position < this.Source.Length)
        {
            int pairOffset = this.Position;
            string propertyName = this.ReadPropertyName();
            if (propertyName.Length == 0)
            {
                return this.CreateFailure(
                    ConstraintDiagnosticCodes.InvalidTimeSettings,
                    "Expected a time property name.",
                    pairOffset);
            }

            this.SkipWhitespace();
            if (this.Position == this.Source.Length
                || this.Source[this.Position] != '=')
            {
                return this.CreateFailure(
                    ConstraintDiagnosticCodes.InvalidTimeSettings,
                    "Expected '=' after time property '" + propertyName + "'.",
                    this.Position);
            }

            this.Position = this.Position + 1;
            this.SkipWhitespace();
            int settingOffset = this.Position;
            string settingName = this.ReadSettingName();
            if (settingName.Length == 0)
            {
                return this.CreateFailure(
                    ConstraintDiagnosticCodes.InvalidTimeSettings,
                    "Expected a setting for time property '" + propertyName + "'.",
                    settingOffset);
            }

            if (this.PropertyNames.Contains(propertyName))
            {
                return this.CreateFailure(
                    ConstraintDiagnosticCodes.InvalidTimeSettings,
                    "Time property '" + propertyName + "' appears more than once.",
                    pairOffset);
            }

            this.PropertyNames.Add(propertyName);
            string? error = this.ApplyProperty(settings, propertyName, settingName);
            if (error != null)
            {
                return this.CreateFailure(
                    ConstraintDiagnosticCodes.InvalidTimeSettings,
                    error,
                    settingOffset);
            }

            this.SkipWhitespace();
        }

        string? restrictionError = this.ValidateBasicRestrictions(settings);
        if (restrictionError != null)
        {
            return this.CreateFailure(
                ConstraintDiagnosticCodes.IncompatibleTimeSettings,
                restrictionError,
                0);
        }

        return new TimeSettingsParseResult
        {
            Settings = settings,
            Success = true
        };
    }

    internal string ReadPropertyName()
    {
        int start = this.Position;
        while (this.Position < this.Source.Length
            && !char.IsWhiteSpace(this.Source[this.Position])
            && this.Source[this.Position] != '=')
        {
            this.Position = this.Position + 1;
        }

        return this.Source.Substring(start, this.Position - start);
    }

    internal string ReadSettingName()
    {
        int start = this.Position;
        while (this.Position < this.Source.Length
            && !char.IsWhiteSpace(this.Source[this.Position]))
        {
            this.Position = this.Position + 1;
        }

        return this.Source.Substring(start, this.Position - start);
    }

    internal void SkipWhitespace()
    {
        while (this.Position < this.Source.Length
            && char.IsWhiteSpace(this.Source[this.Position]))
        {
            this.Position = this.Position + 1;
        }
    }

    internal string? ApplyProperty(
        TimePropertySettings settings,
        string propertyName,
        string settingName)
    {
        switch (propertyName)
        {
            case "Basic":
                return this.ApplyBasic(settings, settingName);
            case "Date":
                return this.ApplyDate(settings, settingName);
            case "Year":
                return this.ApplyYear(settings, settingName);
            case "Time":
                return this.ApplyTime(settings, settingName);
            case "Local-or-UTC":
                return this.ApplyZone(settings, settingName);
            case "Midnight":
                return this.ApplyMidnight(settings, settingName);
            case "Interval-type":
                return this.ApplyInterval(settings, settingName);
            case "SE-point":
                return this.ApplyEndpoint(settings, settingName);
            case "Recurrence":
                return this.ApplyRecurrence(settings, settingName);
            default:
                return "Unknown time property '" + propertyName + "'.";
        }
    }

    internal string? ApplyBasic(TimePropertySettings settings, string settingName)
    {
        switch (settingName)
        {
            case "Date":
                settings.Basic = TimeBasicSettingKind.Date;
                return null;
            case "Time":
                settings.Basic = TimeBasicSettingKind.Time;
                return null;
            case "Date-Time":
                settings.Basic = TimeBasicSettingKind.DateTime;
                return null;
            case "Interval":
                settings.Basic = TimeBasicSettingKind.Interval;
                return null;
            case "Rec-Interval":
                settings.Basic = TimeBasicSettingKind.RecurringInterval;
                return null;
            default:
                return this.InvalidSetting("Basic", settingName);
        }
    }

    internal string? ApplyDate(TimePropertySettings settings, string settingName)
    {
        switch (settingName)
        {
            case "C":
                settings.Date = TimeDateSettingKind.Century;
                return null;
            case "Y":
                settings.Date = TimeDateSettingKind.Year;
                return null;
            case "YM":
                settings.Date = TimeDateSettingKind.YearMonth;
                return null;
            case "YMD":
                settings.Date = TimeDateSettingKind.YearMonthDay;
                return null;
            case "YD":
                settings.Date = TimeDateSettingKind.YearDay;
                return null;
            case "YW":
                settings.Date = TimeDateSettingKind.YearWeek;
                return null;
            case "YWD":
                settings.Date = TimeDateSettingKind.YearWeekDay;
                return null;
            default:
                return this.InvalidSetting("Date", settingName);
        }
    }

    internal string? ApplyYear(TimePropertySettings settings, string settingName)
    {
        switch (settingName)
        {
            case "Basic":
                settings.Year = TimeYearSettingKind.Basic;
                return null;
            case "Proleptic":
                settings.Year = TimeYearSettingKind.Proleptic;
                return null;
            case "Negative":
                settings.Year = TimeYearSettingKind.Negative;
                return null;
        }

        if (settingName.StartsWith("L", System.StringComparison.Ordinal))
        {
            int digits = this.ParsePositiveSuffix(settingName, 1, 5);
            if (digits > 0)
            {
                settings.Year = TimeYearSettingKind.Large;
                settings.LargeYearDigits = digits;
                return null;
            }
        }

        return this.InvalidSetting("Year", settingName);
    }

    internal string? ApplyTime(TimePropertySettings settings, string settingName)
    {
        switch (settingName)
        {
            case "H":
                settings.Time = TimePrecisionSettingKind.Hour;
                return null;
            case "HM":
                settings.Time = TimePrecisionSettingKind.HourMinute;
                return null;
            case "HMS":
                settings.Time = TimePrecisionSettingKind.HourMinuteSecond;
                return null;
        }

        int prefixLength = 0;
        TimePrecisionSettingKind precision = TimePrecisionSettingKind.Unspecified;
        if (settingName.StartsWith("HMSF", System.StringComparison.Ordinal))
        {
            prefixLength = 4;
            precision = TimePrecisionSettingKind.HourMinuteSecond;
        }
        else if (settingName.StartsWith("HMF", System.StringComparison.Ordinal))
        {
            prefixLength = 3;
            precision = TimePrecisionSettingKind.HourMinute;
        }
        else if (settingName.StartsWith("HF", System.StringComparison.Ordinal))
        {
            prefixLength = 2;
            precision = TimePrecisionSettingKind.Hour;
        }

        int digits = this.ParsePositiveSuffix(settingName, prefixLength, 1);
        if (prefixLength > 0 && digits > 0)
        {
            settings.Time = precision;
            settings.FractionalDigits = digits;
            return null;
        }

        return this.InvalidSetting("Time", settingName);
    }

    internal string? ApplyZone(TimePropertySettings settings, string settingName)
    {
        switch (settingName)
        {
            case "L":
                settings.Zone = TimeZoneSettingKind.Local;
                return null;
            case "Z":
                settings.Zone = TimeZoneSettingKind.Utc;
                return null;
            case "LD":
                settings.Zone = TimeZoneSettingKind.LocalDifference;
                return null;
            default:
                return this.InvalidSetting("Local-or-UTC", settingName);
        }
    }

    internal string? ApplyMidnight(TimePropertySettings settings, string settingName)
    {
        switch (settingName)
        {
            case "Start":
                settings.Midnight = TimeMidnightSettingKind.Start;
                return null;
            case "End":
                settings.Midnight = TimeMidnightSettingKind.End;
                return null;
            default:
                return this.InvalidSetting("Midnight", settingName);
        }
    }

    internal string? ApplyInterval(TimePropertySettings settings, string settingName)
    {
        switch (settingName)
        {
            case "SE":
                settings.Interval = TimeIntervalSettingKind.StartEnd;
                return null;
            case "D":
                settings.Interval = TimeIntervalSettingKind.Duration;
                return null;
            case "SD":
                settings.Interval = TimeIntervalSettingKind.StartDuration;
                return null;
            case "DE":
                settings.Interval = TimeIntervalSettingKind.DurationEnd;
                return null;
            default:
                return this.InvalidSetting("Interval-type", settingName);
        }
    }

    internal string? ApplyEndpoint(TimePropertySettings settings, string settingName)
    {
        switch (settingName)
        {
            case "Date":
                settings.Endpoint = TimeEndpointSettingKind.Date;
                return null;
            case "Time":
                settings.Endpoint = TimeEndpointSettingKind.Time;
                return null;
            case "Date-Time":
                settings.Endpoint = TimeEndpointSettingKind.DateTime;
                return null;
            default:
                return this.InvalidSetting("SE-point", settingName);
        }
    }

    internal string? ApplyRecurrence(TimePropertySettings settings, string settingName)
    {
        if (settingName == "Unlimited")
        {
            settings.Recurrence = TimeRecurrenceSettingKind.Unlimited;
            return null;
        }

        if (settingName.StartsWith("R", System.StringComparison.Ordinal))
        {
            int digits = this.ParsePositiveSuffix(settingName, 1, 1);
            if (digits > 0)
            {
                settings.Recurrence = TimeRecurrenceSettingKind.DigitCount;
                settings.RecurrenceDigits = digits;
                return null;
            }
        }

        return this.InvalidSetting("Recurrence", settingName);
    }

    internal int ParsePositiveSuffix(string text, int start, int minimum)
    {
        if (start <= 0 || start >= text.Length)
        {
            return 0;
        }

        int value = 0;
        int index = start;
        while (index < text.Length)
        {
            char current = text[index];
            if (current < '0' || current > '9')
            {
                return 0;
            }

            int digit = current - '0';
            if (value > (int.MaxValue - digit) / 10)
            {
                return 0;
            }

            value = (value * 10) + digit;
            index = index + 1;
        }

        return value < minimum ? 0 : value;
    }

    internal string? ValidateBasicRestrictions(TimePropertySettings settings)
    {
        switch (settings.Basic)
        {
            case TimeBasicSettingKind.Date:
                if (settings.Time != TimePrecisionSettingKind.Unspecified
                    || settings.Zone != TimeZoneSettingKind.Unspecified
                    || settings.Midnight != TimeMidnightSettingKind.Unspecified
                    || settings.Interval != TimeIntervalSettingKind.Unspecified
                    || settings.Endpoint != TimeEndpointSettingKind.Unspecified
                    || settings.Recurrence != TimeRecurrenceSettingKind.Unspecified)
                {
                    return "Basic=Date is incompatible with one or more supplied time properties.";
                }

                break;
            case TimeBasicSettingKind.Time:
                if (settings.Date != TimeDateSettingKind.Unspecified
                    || settings.Year != TimeYearSettingKind.Unspecified
                    || settings.Interval != TimeIntervalSettingKind.Unspecified
                    || settings.Endpoint != TimeEndpointSettingKind.Unspecified
                    || settings.Recurrence != TimeRecurrenceSettingKind.Unspecified)
                {
                    return "Basic=Time is incompatible with one or more supplied time properties.";
                }

                break;
            case TimeBasicSettingKind.DateTime:
                if (settings.Interval != TimeIntervalSettingKind.Unspecified
                    || settings.Endpoint != TimeEndpointSettingKind.Unspecified
                    || settings.Recurrence != TimeRecurrenceSettingKind.Unspecified)
                {
                    return "Basic=Date-Time is incompatible with one or more supplied time properties.";
                }

                break;
            case TimeBasicSettingKind.Interval:
                if (settings.Recurrence != TimeRecurrenceSettingKind.Unspecified)
                {
                    return "Basic=Interval is incompatible with the Recurrence property.";
                }

                break;
        }

        return null;
    }

    internal string InvalidSetting(string propertyName, string settingName)
    {
        return "Unknown setting '" + settingName + "' for time property '"
            + propertyName + "'.";
    }

    internal TimeSettingsParseResult CreateFailure(
        int diagnosticCode,
        string message,
        int offset)
    {
        return new TimeSettingsParseResult
        {
            DiagnosticCode = diagnosticCode,
            ErrorMessage = message,
            ErrorOffset = offset,
            Success = false
        };
    }
}
