using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace xml;

internal class XPathDateTimePictureMarker
{
    internal char Component;
    internal string Presentation;
    internal bool Ordinal;
    internal char SecondModifier;
    internal int MinimumWidth;
    internal int MaximumWidth;

    internal XPathDateTimePictureMarker()
    {
        Presentation = "";
        SecondModifier = 'c';
        MinimumWidth = -1;
        MaximumWidth = -1;
    }
}

internal class XPathDateTimeFunctionImplementation : IXPathFunction
{
    internal string LocalName;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmDateTimeDatatype DateTimes;
    internal XdmDurationDatatype Durations;
    internal XdmNumericDatatype Numerics;
    internal XdmFactory Factory;

    internal XPathDateTimeFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext)
    {
        LocalName = localName;
        Range = range;
        StaticContext = staticContext;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        DateTimes = new XdmDateTimeDatatype();
        Durations = new XdmDurationDatatype();
        Numerics = new XdmNumericDatatype();
        Factory = new XdmFactory();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        if (LocalName == "parse-ietf-date")
        {
            return EvaluateXPathIetfDate(arguments[0]);
        }

        if (IsXPathDurationComponentFunction())
        {
            return EvaluateXPathDurationComponent(arguments[0]);
        }

        if (IsXPathDateTimeComponentFunction())
        {
            return EvaluateXPathDateTimeComponent(arguments[0]);
        }

        if (LocalName == "dateTime")
        {
            return EvaluateXPathDateTimeConstructor(arguments);
        }

        if (LocalName == "adjust-dateTime-to-timezone" || LocalName == "adjust-date-to-timezone" || LocalName == "adjust-time-to-timezone")
        {
            return EvaluateXPathTimezoneAdjustment(arguments, context);
        }

        if (LocalName == "format-dateTime" || LocalName == "format-date" || LocalName == "format-time")
        {
            return EvaluateXPathDateTimeFormatting(arguments, context);
        }

        throw new InvalidOperationException("The XPath date/time function is not implemented.");
    }

    internal XdmSequence EvaluateXPathIetfDate(XdmSequence supplied)
    {
        string? source = ReadXPathOptionalString(supplied);
        if (source == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        string lexical = CreateXPathIetfDateLexical(source);
        XdmNumericParseResult parsed = DateTimes.TryParseXdmDateTime(lexical, XdmAtomicType.DateTime);
        if (!parsed.Success || parsed.Value == null)
        {
            throw CreateXPathDateTimeError("The value is not a valid IETF date.", XPathErrorCodes.FORG0010);
        }

        return Sequences.CreateXdmAtomicSequence(parsed.Value);
    }

    internal string CreateXPathIetfDateLexical(string source)
    {
        string whitespace = "[\\x09\\x0A\\x0D\\x20]";
        string dayName = "(?:Mon|Tue|Wed|Thu|Fri|Sat|Sun|Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday)";
        string monthName = "(?<month>Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)";
        string prefix = "^" + whitespace + "*(?:" + dayName + ",?" + whitespace + "+)?";
        string separator = "(?:" + whitespace + "+|" + whitespace + "*-" + whitespace + "*)";
        string time = "(?<hour>[0-9]{1,2}):(?<minute>[0-9]{2})(?::(?<second>[0-9]{2})(?<fraction>\\.[0-9]+)?)?" + CreateXPathIetfTimezonePattern(whitespace);
        string suffix = whitespace + "*$";
        string ordinary = prefix + "(?<day>[0-9]{1,2})" + separator + monthName + separator + "(?<year>[0-9]{2}(?:[0-9]{2})?)" + whitespace + "+" + time + suffix;
        string asctime = prefix + monthName + separator + "(?<day>[0-9]{1,2})" + whitespace + "+" + time + whitespace + "+(?<year>[0-9]{2}(?:[0-9]{2})?)" + suffix;
        Match match = Regex.Match(source, ordinary, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            match = Regex.Match(source, asctime, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        if (!match.Success)
        {
            throw CreateXPathDateTimeError("The value does not match the IETF date grammar.", XPathErrorCodes.FORG0010);
        }

        int day = int.Parse(match.Groups["day"].Value, CultureInfo.InvariantCulture);
        int month = ReadXPathIetfMonth(match.Groups["month"].Value);
        string yearText = match.Groups["year"].Value;
        int year = int.Parse(yearText, CultureInfo.InvariantCulture);
        if (yearText.Length == 2)
        {
            year = year + 1900;
        }

        int hour = int.Parse(match.Groups["hour"].Value, CultureInfo.InvariantCulture);
        int minute = int.Parse(match.Groups["minute"].Value, CultureInfo.InvariantCulture);
        string second = match.Groups["second"].Success ? match.Groups["second"].Value : "00";
        string fraction = match.Groups["fraction"].Success ? match.Groups["fraction"].Value : "";
        int timezone = ReadXPathIetfTimezone(match);
        string sign = timezone < 0 ? "-" : "+";
        int absoluteTimezone = timezone < 0 ? -timezone : timezone;
        return year.ToString("D4", CultureInfo.InvariantCulture) + "-" + month.ToString("D2", CultureInfo.InvariantCulture) + "-" + day.ToString("D2", CultureInfo.InvariantCulture) + "T" + hour.ToString("D2", CultureInfo.InvariantCulture) + ":" + minute.ToString("D2", CultureInfo.InvariantCulture) + ":" + second + fraction + sign + (absoluteTimezone / 60).ToString("D2", CultureInfo.InvariantCulture) + ":" + (absoluteTimezone % 60).ToString("D2", CultureInfo.InvariantCulture);
    }

    internal string CreateXPathIetfTimezonePattern(string whitespace)
    {
        string names = "UT|UTC|GMT|EST|EDT|CST|CDT|MST|MDT|PST|PDT";
        string offset = "(?<tzsign>[+-])(?:(?<tzh>[0-9]{1,2}):(?<tzm>[0-9]{2})?|(?<tzcompact>[0-9]{1,4}))(?:" + whitespace + "*\\(" + whitespace + "*(?:" + names + ")" + whitespace + "*\\))?";
        return "(?:" + whitespace + "*(?:(?<tzname>" + names + ")|" + offset + "))?";
    }

    internal int ReadXPathIetfMonth(string value)
    {
        string normalized = value.ToUpperInvariant();
        if (normalized == "JAN")
        {
            return 1;
        }

        if (normalized == "FEB")
        {
            return 2;
        }

        if (normalized == "MAR")
        {
            return 3;
        }

        if (normalized == "APR")
        {
            return 4;
        }

        if (normalized == "MAY")
        {
            return 5;
        }

        if (normalized == "JUN")
        {
            return 6;
        }

        if (normalized == "JUL")
        {
            return 7;
        }

        if (normalized == "AUG")
        {
            return 8;
        }

        if (normalized == "SEP")
        {
            return 9;
        }

        if (normalized == "OCT")
        {
            return 10;
        }

        return normalized == "NOV" ? 11 : 12;
    }

    internal int ReadXPathIetfTimezone(Match match)
    {
        Group named = match.Groups["tzname"];
        if (named.Success)
        {
            string value = named.Value.ToUpperInvariant();
            if (value == "EST")
            {
                return -300;
            }

            if (value == "EDT" || value == "CDT")
            {
                return value == "EDT" ? -240 : -300;
            }

            if (value == "CST" || value == "MDT")
            {
                return value == "CST" ? -360 : -360;
            }

            if (value == "MST" || value == "PDT")
            {
                return value == "MST" ? -420 : -420;
            }

            if (value == "PST")
            {
                return -480;
            }

            return 0;
        }

        if (!match.Groups["tzsign"].Success)
        {
            return 0;
        }

        int hours;
        int minutes;
        if (match.Groups["tzh"].Success)
        {
            hours = int.Parse(match.Groups["tzh"].Value, CultureInfo.InvariantCulture);
            string minuteText = match.Groups["tzm"].Value;
            minutes = minuteText.Length == 0 ? 0 : int.Parse(minuteText, CultureInfo.InvariantCulture);
        }
        else
        {
            string compact = match.Groups["tzcompact"].Value;
            int hourLength = compact.Length <= 2 ? compact.Length : compact.Length - 2;
            hours = int.Parse(compact.Substring(0, hourLength), CultureInfo.InvariantCulture);
            minutes = compact.Length <= 2 ? 0 : int.Parse(compact.Substring(hourLength), CultureInfo.InvariantCulture);
        }

        if (minutes >= 60 || hours > 14 || hours == 14 && minutes != 0)
        {
            throw CreateXPathDateTimeError("The IETF date contains an invalid timezone offset.", XPathErrorCodes.FORG0010);
        }

        int result = hours * 60 + minutes;
        return match.Groups["tzsign"].Value == "-" ? -result : result;
    }

    internal bool IsXPathDurationComponentFunction()
    {
        return LocalName == "years-from-duration" || LocalName == "months-from-duration" || LocalName == "days-from-duration" || LocalName == "hours-from-duration" || LocalName == "minutes-from-duration" || LocalName == "seconds-from-duration";
    }

    internal bool IsXPathDateTimeComponentFunction()
    {
        return LocalName == "year-from-dateTime" || LocalName == "month-from-dateTime" || LocalName == "day-from-dateTime" || LocalName == "hours-from-dateTime" || LocalName == "minutes-from-dateTime" || LocalName == "seconds-from-dateTime" || LocalName == "timezone-from-dateTime" || LocalName == "year-from-date" || LocalName == "month-from-date" || LocalName == "day-from-date" || LocalName == "timezone-from-date" || LocalName == "hours-from-time" || LocalName == "minutes-from-time" || LocalName == "seconds-from-time" || LocalName == "timezone-from-time";
    }

    internal XdmSequence EvaluateXPathDurationComponent(XdmSequence supplied)
    {
        XdmAtomicValue? value = ReadXPathOptionalAtomic(supplied, XdmAtomicType.Duration);
        if (value == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XdmAtomicValue result;
        if (LocalName == "years-from-duration")
        {
            result = Durations.YearsFromXdmDuration(value);
        }
        else if (LocalName == "months-from-duration")
        {
            result = Durations.MonthsFromXdmDuration(value);
        }
        else if (LocalName == "days-from-duration")
        {
            result = Durations.DaysFromXdmDuration(value);
        }
        else if (LocalName == "hours-from-duration")
        {
            result = Durations.HoursFromXdmDuration(value);
        }
        else if (LocalName == "minutes-from-duration")
        {
            result = Durations.MinutesFromXdmDuration(value);
        }
        else
        {
            result = Durations.SecondsFromXdmDuration(value);
        }

        return Sequences.CreateXdmAtomicSequence(result);
    }

    internal XdmSequence EvaluateXPathDateTimeComponent(XdmSequence supplied)
    {
        XdmAtomicType expected = ReadXPathDateTimeFunctionArgumentType();
        XdmAtomicValue? value = ReadXPathOptionalAtomic(supplied, expected);
        if (value == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XdmAtomicValue? result;
        if (LocalName == "year-from-dateTime" || LocalName == "year-from-date")
        {
            result = DateTimes.YearFromXdmDateTime(value);
        }
        else if (LocalName == "month-from-dateTime" || LocalName == "month-from-date")
        {
            result = DateTimes.MonthFromXdmDateTime(value);
        }
        else if (LocalName == "day-from-dateTime" || LocalName == "day-from-date")
        {
            result = DateTimes.DayFromXdmDateTime(value);
        }
        else if (LocalName == "hours-from-dateTime" || LocalName == "hours-from-time")
        {
            result = DateTimes.HoursFromXdmDateTime(value);
        }
        else if (LocalName == "minutes-from-dateTime" || LocalName == "minutes-from-time")
        {
            result = DateTimes.MinutesFromXdmDateTime(value);
        }
        else if (LocalName == "seconds-from-dateTime" || LocalName == "seconds-from-time")
        {
            result = DateTimes.SecondsFromXdmDateTime(value);
        }
        else
        {
            result = DateTimes.TimezoneFromXdmDateTime(value);
        }

        return result == null ? Sequences.CreateEmptyXdmSequence() : Sequences.CreateXdmAtomicSequence(result);
    }

    internal XdmAtomicType ReadXPathDateTimeFunctionArgumentType()
    {
        if (LocalName.IndexOf("dateTime", StringComparison.Ordinal) >= 0)
        {
            return XdmAtomicType.DateTime;
        }

        if (LocalName.IndexOf("-date", StringComparison.Ordinal) >= 0 || LocalName == "format-date")
        {
            return XdmAtomicType.Date;
        }

        return XdmAtomicType.Time;
    }

    internal XdmSequence EvaluateXPathDateTimeConstructor(XdmSequence[] arguments)
    {
        XdmAtomicValue? date = ReadXPathOptionalAtomic(arguments[0], XdmAtomicType.Date);
        XdmAtomicValue? time = ReadXPathOptionalAtomic(arguments[1], XdmAtomicType.Time);
        if (date == null || time == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XdmDateTimeStorage dateStorage = DateTimes.ReadXdmDateTimeStorage(date);
        XdmDateTimeStorage timeStorage = DateTimes.ReadXdmDateTimeStorage(time);
        if (dateStorage.HasTimezone && timeStorage.HasTimezone && dateStorage.TimezoneMinutes != timeStorage.TimezoneMinutes)
        {
            throw CreateXPathDateTimeError("The date and time arguments have different timezones.", XPathErrorCodes.FORG0008);
        }

        dateStorage.Hour = timeStorage.Hour;
        dateStorage.Minute = timeStorage.Minute;
        dateStorage.SecondCoefficient = timeStorage.SecondCoefficient;
        dateStorage.SecondScale = timeStorage.SecondScale;
        if (timeStorage.HasTimezone)
        {
            dateStorage.HasTimezone = true;
            dateStorage.TimezoneMinutes = timeStorage.TimezoneMinutes;
        }

        return Sequences.CreateXdmAtomicSequence(DateTimes.CreateXdmDateTimeValue(dateStorage, XdmAtomicType.DateTime, null));
    }

    internal XdmSequence EvaluateXPathTimezoneAdjustment(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmAtomicType expected = ReadXPathDateTimeFunctionArgumentType();
        XdmAtomicValue? value = ReadXPathOptionalAtomic(arguments[0], expected);
        bool hasTimezone = true;
        int timezoneMinutes = context.ImplicitTimezoneMinutes;
        if (arguments.Length == 2)
        {
            XdmAtomicValue? timezone = ReadXPathOptionalAtomic(arguments[1], XdmAtomicType.DayTimeDuration);
            hasTimezone = timezone != null;
            if (timezone != null)
            {
                timezoneMinutes = ReadXPathTimezoneMinutes(timezone);
            }
        }

        if (value == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        if (value.Type == XdmAtomicType.DateTimeStamp && !hasTimezone)
        {
            XdmDateTimeStorage ordinaryStorage = DateTimes.ReadXdmDateTimeStorage(value);
            value = DateTimes.CreateXdmDateTimeValue(ordinaryStorage, XdmAtomicType.DateTime, null);
        }

        XdmAtomicValue adjusted = DateTimes.AdjustXdmDateTimeTimezone(value, hasTimezone, timezoneMinutes);
        return Sequences.CreateXdmAtomicSequence(adjusted);
    }

    internal int ReadXPathTimezoneMinutes(XdmAtomicValue timezone)
    {
        XdmDurationStorage storage = Durations.ReadXdmDurationStorage(timezone);
        BigInteger factor = Numerics.PowerOfTen(storage.SecondsScale);
        BigInteger minuteUnit = factor * 60;
        if (storage.Months != BigInteger.Zero || storage.SecondsCoefficient % minuteUnit != BigInteger.Zero)
        {
            throw CreateXPathDateTimeError("A timezone must be an integral number of minutes.", XPathErrorCodes.FODT0003);
        }

        BigInteger minutes = storage.SecondsCoefficient / minuteUnit;
        if (minutes < new BigInteger(-840) || minutes > new BigInteger(840))
        {
            throw CreateXPathDateTimeError("A timezone must be between -14:00 and +14:00.", XPathErrorCodes.FODT0003);
        }

        return (int)minutes;
    }

    internal XdmSequence EvaluateXPathDateTimeFormatting(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmAtomicType expected = ReadXPathDateTimeFunctionArgumentType();
        XdmAtomicValue? value = ReadXPathOptionalAtomic(arguments[0], expected);
        string picture = ReadXPathRequiredString(arguments[1]);
        string? language = arguments.Length == 5 ? ReadXPathOptionalString(arguments[2]) : null;
        if (language == null || language.Length == 0)
        {
            language = context.DefaultLanguage;
            if (language == null || language.Length == 0)
            {
                language = "en";
            }
        }

        string? calendar = arguments.Length == 5 ? ReadXPathOptionalString(arguments[3]) : null;
        string? place = arguments.Length == 5 ? ReadXPathOptionalString(arguments[4]) : null;
        if (value == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        CultureInfo culture = ResolveXPathDateTimeCulture(language);
        string prefix = CreateXPathDateTimeFallbackPrefix(language, calendar, culture);
        string selectedCalendar = ReadXPathEffectiveCalendar(calendar);
        XdmAtomicValue adjusted = AdjustXPathDateTimeForPlace(value, place, context);
        string formatted = FormatXPathDateTimePicture(adjusted, picture, culture, selectedCalendar);
        return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmStringAtomicValue(prefix + formatted, XdmAtomicType.String));
    }

    internal XdmAtomicValue AdjustXPathDateTimeForPlace(XdmAtomicValue value, string? place, XPathDynamicContext context)
    {
        if (place == null || place.Length == 0 || value.Type == XdmAtomicType.Time)
        {
            return value;
        }

        TimeZoneInfo? zone = FindXPathTimeZone(place);
        if (zone == null)
        {
            return value;
        }

        XdmDateTimeStorage storage = DateTimes.ReadXdmDateTimeStorage(value);
        if (storage.Year < new BigInteger(2) || storage.Year > new BigInteger(9998))
        {
            return value;
        }

        XdmAtomicValue zoned = storage.HasTimezone ? value : DateTimes.ApplyXdmImplicitTimezone(value, context.ImplicitTimezoneMinutes);
        storage = DateTimes.ReadXdmDateTimeStorage(zoned);
        int second = (int)(storage.SecondCoefficient / Numerics.PowerOfTen(storage.SecondScale));
        DateTimeOffset instant = new DateTimeOffset((int)storage.Year, storage.Month, storage.Day, storage.Hour, storage.Minute, second, TimeSpan.FromMinutes(storage.TimezoneMinutes));
        TimeSpan targetOffset = zone.GetUtcOffset(instant.UtcDateTime);
        return DateTimes.AdjustXdmDateTimeTimezone(zoned, true, (int)targetOffset.TotalMinutes);
    }

    internal TimeZoneInfo? FindXPathTimeZone(string place)
    {
        System.Collections.ObjectModel.ReadOnlyCollection<TimeZoneInfo> zones = TimeZoneInfo.GetSystemTimeZones();
        int index = 0;
        while (index < zones.Count)
        {
            if (string.Equals(zones[index].Id, place, StringComparison.OrdinalIgnoreCase))
            {
                return zones[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal CultureInfo ResolveXPathDateTimeCulture(string? language)
    {
        if (language == null || language.Length == 0)
        {
            return CultureInfo.GetCultureInfo("en");
        }

        CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures | CultureTypes.SpecificCultures);
        int index = 0;
        while (index < cultures.Length)
        {
            if (string.Equals(cultures[index].Name, language, StringComparison.OrdinalIgnoreCase))
            {
                return cultures[index];
            }

            index = index + 1;
        }

        return CultureInfo.GetCultureInfo("en");
    }

    internal string CreateXPathDateTimeFallbackPrefix(string? language, string? calendar, CultureInfo culture)
    {
        string prefix = "";
        if (language != null && language.Length > 0 && !string.Equals(language, culture.Name, StringComparison.OrdinalIgnoreCase))
        {
            prefix = prefix + "[Language: en]";
        }

        string selectedCalendar = calendar == null || calendar.Length == 0 ? "AD" : ValidateXPathCalendar(calendar);
        if (selectedCalendar != "AD" && selectedCalendar != "ISO" && selectedCalendar != "CE")
        {
            prefix = prefix + "[Calendar: AD]";
        }

        return prefix;
    }

    internal string ReadXPathEffectiveCalendar(string? calendar)
    {
        if (calendar == null || calendar.Length == 0)
        {
            return "AD";
        }

        string selected = ValidateXPathCalendar(calendar);
        return selected == "AD" || selected == "ISO" || selected == "CE" ? selected : "AD";
    }

    internal string ValidateXPathCalendar(string calendar)
    {
        if (calendar.StartsWith("Q{", StringComparison.Ordinal))
        {
            int closing = calendar.IndexOf('}');
            if (closing < 2 || closing == calendar.Length - 1 || !IsXPathNcName(calendar.Substring(closing + 1)))
            {
                throw CreateXPathDateTimeError("The calendar is not a valid EQName.", XPathErrorCodes.FOFD1340);
            }

            if (closing == 2)
            {
                string local = calendar.Substring(3);
                if (!IsXPathKnownCalendarDesignator(local))
                {
                    throw CreateXPathDateTimeError("The calendar designator is not recognized.", XPathErrorCodes.FOFD1340);
                }

                return local;
            }

            return calendar;
        }

        int colon = calendar.IndexOf(':');
        if (colon >= 0)
        {
            if (colon == 0 || colon == calendar.Length - 1 || calendar.IndexOf(':', colon + 1) >= 0 || !IsXPathNcName(calendar.Substring(0, colon)) || !IsXPathNcName(calendar.Substring(colon + 1)) || !HasXPathCalendarPrefix(calendar.Substring(0, colon)))
            {
                throw CreateXPathDateTimeError("The calendar is not a valid resolvable QName.", XPathErrorCodes.FOFD1340);
            }

            return calendar;
        }

        if (!IsXPathNcName(calendar) || !IsXPathKnownCalendarDesignator(calendar))
        {
            throw CreateXPathDateTimeError("The calendar designator is not recognized.", XPathErrorCodes.FOFD1340);
        }

        return calendar;
    }

    internal bool HasXPathCalendarPrefix(string prefix)
    {
        XmlNamespaceBinding[] bindings = StaticContext.Namespaces == null ? [] : StaticContext.Namespaces;
        int index = 0;
        while (index < bindings.Length)
        {
            XmlNamespaceBinding? binding = bindings[index];
            if (binding != null && binding.Prefix == prefix && binding.NamespaceUri != null && binding.NamespaceUri.Length > 0)
            {
                return true;
            }

            index = index + 1;
        }

        return prefix == "xml";
    }

    internal bool IsXPathKnownCalendarDesignator(string value)
    {
        string calendars = " AD AH AME AM AP AS BE CB CE CL CS EE FE ISO JE KE KY ME MS NS OS RS SE SH SS TE VE VS ";
        return calendars.IndexOf(" " + value + " ", StringComparison.Ordinal) >= 0;
    }

    internal bool IsXPathNcName(string value)
    {
        if (value.Length == 0 || !(char.IsLetter(value[0]) || value[0] == '_'))
        {
            return false;
        }

        int index = 1;
        while (index < value.Length)
        {
            char character = value[index];
            if (!char.IsLetterOrDigit(character) && character != '_' && character != '-' && character != '.')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal string FormatXPathDateTimePicture(XdmAtomicValue value, string picture, CultureInfo culture, string? calendar)
    {
        XdmDateTimeStorage storage = DateTimes.ReadXdmDateTimeStorage(value);
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < picture.Length)
        {
            char character = picture[index];
            if (character == '[')
            {
                if (index + 1 < picture.Length && picture[index + 1] == '[')
                {
                    result.Append('[');
                    index = index + 2;
                }
                else
                {
                    int closing = FindXPathDateTimeMarkerEnd(picture, index + 1);
                    if (closing < 0)
                    {
                        throw CreateXPathDateTimeError("The date/time picture contains an unmatched opening bracket.", XPathErrorCodes.FOFD1340);
                    }

                    XPathDateTimePictureMarker marker = ParseXPathDateTimeMarker(picture.Substring(index + 1, closing - index - 1));
                    ValidateXPathDateTimeComponent(marker.Component, value.Type);
                    result.Append(FormatXPathDateTimeComponent(storage, marker, culture, calendar));
                    index = closing + 1;
                }
            }
            else if (character == ']')
            {
                if (index + 1 >= picture.Length || picture[index + 1] != ']')
                {
                    throw CreateXPathDateTimeError("The date/time picture contains an unmatched closing bracket.", XPathErrorCodes.FOFD1340);
                }

                result.Append(']');
                index = index + 2;
            }
            else
            {
                result.Append(character);
                index = index + 1;
            }
        }

        return result.ToString();
    }

    internal int FindXPathDateTimeMarkerEnd(string picture, int start)
    {
        int index = start;
        while (index < picture.Length)
        {
            if (picture[index] == ']')
            {
                return index;
            }

            if (picture[index] == '[')
            {
                return -1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal XPathDateTimePictureMarker ParseXPathDateTimeMarker(string source)
    {
        StringBuilder compactBuilder = new StringBuilder();
        int sourceIndex = 0;
        while (sourceIndex < source.Length)
        {
            if (!char.IsWhiteSpace(source[sourceIndex]))
            {
                compactBuilder.Append(source[sourceIndex]);
            }

            sourceIndex = sourceIndex + 1;
        }

        string compact = compactBuilder.ToString();
        if (compact.Length == 0 || !IsXPathDateTimeComponentSpecifier(compact[0]))
        {
            throw CreateXPathDateTimeError("A date/time marker requires a valid component specifier.", XPathErrorCodes.FOFD1340);
        }

        XPathDateTimePictureMarker result = new XPathDateTimePictureMarker();
        result.Component = compact[0];
        int comma = FindXPathDateTimeWidthSeparator(compact);
        string modifiers = comma < 0 ? compact.Substring(1) : compact.Substring(1, comma - 1);
        if (modifiers.Length > 1 && IsXPathDateTimeSecondModifier(modifiers[modifiers.Length - 1]))
        {
            result.SecondModifier = modifiers[modifiers.Length - 1];
            result.Ordinal = result.SecondModifier == 'o';
            modifiers = modifiers.Substring(0, modifiers.Length - 1);
        }

        result.Presentation = modifiers.Length == 0 ? ReadXPathDefaultDateTimePresentation(result.Component) : modifiers;
        ValidateXPathDateTimePresentation(result.Presentation, result.Component);
        if (comma >= 0)
        {
            ParseXPathDateTimeWidth(compact.Substring(comma + 1), result);
        }

        return result;
    }

    internal int FindXPathDateTimeWidthSeparator(string compact)
    {
        int comma = compact.LastIndexOf(',');
        if (comma < 1 || comma + 1 >= compact.Length)
        {
            return -1;
        }

        string candidate = compact.Substring(comma + 1);
        bool widthShape = candidate == "*" || candidate.IndexOf('-') >= 0 || comma == 1 || compact.IndexOf(',', 1) != comma || candidate.Length != 3;
        if (!widthShape)
        {
            return -1;
        }

        int index = 0;
        while (index < candidate.Length)
        {
            char character = candidate[index];
            if ((character < '0' || character > '9') && character != '*' && character != '-')
            {
                return -1;
            }

            index = index + 1;
        }

        return comma;
    }

    internal bool IsXPathDateTimeComponentSpecifier(char component)
    {
        return component == 'Y' || component == 'M' || component == 'D' || component == 'd' || component == 'F' || component == 'W' || component == 'w' || component == 'H' || component == 'h' || component == 'P' || component == 'm' || component == 's' || component == 'f' || component == 'Z' || component == 'z' || component == 'C' || component == 'E';
    }

    internal bool IsXPathDateTimeSecondModifier(char modifier)
    {
        return modifier == 'a' || modifier == 't' || modifier == 'c' || modifier == 'o';
    }

    internal string ReadXPathDefaultDateTimePresentation(char component)
    {
        if (component == 'm' || component == 's')
        {
            return "01";
        }

        if (component == 'F' || component == 'P' || component == 'C' || component == 'E')
        {
            return "n";
        }

        if (component == 'Z' || component == 'z')
        {
            return "01:01";
        }

        return "1";
    }

    internal void ValidateXPathDateTimePresentation(string presentation, char component)
    {
        if (presentation.Length == 0)
        {
            throw CreateXPathDateTimeError("A date/time presentation modifier cannot be empty.", XPathErrorCodes.FOFD1340);
        }

        if (presentation == "a" || presentation == "A" || presentation == "i" || presentation == "I" || presentation == "w" || presentation == "W" || presentation == "Ww" || presentation == "n" || presentation == "N" || presentation == "Nn" || presentation == "Z" || presentation == "1" || presentation == "01:01" || presentation == "1:01" || presentation == "01")
        {
            return;
        }

        bool hasDigit = false;
        bool foundMandatory = false;
        int index = 0;
        while (index < presentation.Length)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(presentation, index);
            bool digit = category == UnicodeCategory.DecimalDigitNumber;
            char character = presentation[index];
            if (component != 'f' && character == '#' && foundMandatory)
            {
                throw CreateXPathDateTimeError("Optional digits must precede mandatory digits in a date/time picture.", XPathErrorCodes.FOFD1340);
            }

            hasDigit = hasDigit || digit;
            foundMandatory = foundMandatory || digit;
            index = index + (char.IsHighSurrogate(character) && index + 1 < presentation.Length && char.IsLowSurrogate(presentation[index + 1]) ? 2 : 1);
        }

        if (!hasDigit)
        {
            throw CreateXPathDateTimeError("The date/time presentation modifier is invalid.", XPathErrorCodes.FOFD1340);
        }
    }

    internal void ParseXPathDateTimeWidth(string width, XPathDateTimePictureMarker marker)
    {
        int hyphen = width.IndexOf('-');
        if (hyphen < 0)
        {
            if (width == "*")
            {
                marker.MinimumWidth = -1;
                marker.MaximumWidth = -1;
                return;
            }

            marker.MinimumWidth = ParseXPathDateTimeWidthValue(width);
            marker.MaximumWidth = marker.MinimumWidth;
            if (marker.MinimumWidth <= 0)
            {
                throw CreateXPathDateTimeError("A date/time width modifier must be a positive integer or minimum-maximum form.", XPathErrorCodes.FOFD1340);
            }

            return;
        }

        if (width.IndexOf('-', hyphen + 1) >= 0)
        {
            throw CreateXPathDateTimeError("A date/time width modifier must have minimum-maximum form.", XPathErrorCodes.FOFD1340);
        }

        marker.MinimumWidth = ParseXPathDateTimeWidthValue(width.Substring(0, hyphen));
        marker.MaximumWidth = ParseXPathDateTimeWidthValue(width.Substring(hyphen + 1));
        if (marker.MinimumWidth == 0 || marker.MaximumWidth == 0 || marker.MinimumWidth > 0 && marker.MaximumWidth > 0 && marker.MinimumWidth > marker.MaximumWidth)
        {
            throw CreateXPathDateTimeError("The date/time width bounds are invalid.", XPathErrorCodes.FOFD1340);
        }
    }

    internal int ParseXPathDateTimeWidthValue(string value)
    {
        if (value == "*")
        {
            return -1;
        }

        if (value.Length == 0)
        {
            return 0;
        }

        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9' || result > 100000)
            {
                return 0;
            }

            result = result * 10 + value[index] - '0';
            index = index + 1;
        }

        return result;
    }

    internal void ValidateXPathDateTimeComponent(char component, XdmAtomicType type)
    {
        bool hasDate = type == XdmAtomicType.DateTime || type == XdmAtomicType.DateTimeStamp || type == XdmAtomicType.Date;
        bool hasTime = type == XdmAtomicType.DateTime || type == XdmAtomicType.DateTimeStamp || type == XdmAtomicType.Time;
        bool dateComponent = component == 'Y' || component == 'M' || component == 'D' || component == 'd' || component == 'F' || component == 'W' || component == 'w' || component == 'C' || component == 'E';
        bool timeComponent = component == 'H' || component == 'h' || component == 'P' || component == 'm' || component == 's' || component == 'f';
        if (dateComponent && !hasDate || timeComponent && !hasTime)
        {
            throw CreateXPathDateTimeError("The requested component is not available in the supplied value.", XPathErrorCodes.FOFD1350);
        }
    }

    internal string FormatXPathDateTimeComponent(XdmDateTimeStorage storage, XPathDateTimePictureMarker marker, CultureInfo culture, string? calendar)
    {
        char component = marker.Component;
        if (component == 'Z' || component == 'z')
        {
            return FormatXPathDateTimeTimezone(storage, marker);
        }

        if (component == 'M' && IsXPathNamePresentation(marker.Presentation))
        {
            string month = culture.DateTimeFormat.GetMonthName(storage.Month);
            string abbreviatedMonth = culture.DateTimeFormat.GetAbbreviatedMonthName(storage.Month);
            if (marker.MaximumWidth > 0 && month.Length > marker.MaximumWidth && abbreviatedMonth.Length >= marker.MinimumWidth && abbreviatedMonth.Length <= marker.MaximumWidth)
            {
                month = abbreviatedMonth;
            }

            return ApplyXPathDateTimeNamePresentation(month, marker);
        }

        if (component == 'F' && IsXPathNamePresentation(marker.Presentation))
        {
            int dayOfWeek = ReadXPathDayOfWeek(storage);
            string day = culture.DateTimeFormat.GetDayName((DayOfWeek)dayOfWeek);
            string abbreviatedDay = culture.DateTimeFormat.GetAbbreviatedDayName((DayOfWeek)dayOfWeek);
            if (marker.MaximumWidth > 0 && day.Length > marker.MaximumWidth && abbreviatedDay.Length >= marker.MinimumWidth && abbreviatedDay.Length <= marker.MaximumWidth)
            {
                day = abbreviatedDay;
            }

            return ApplyXPathDateTimeNamePresentation(day, marker);
        }

        if (component == 'P')
        {
            string designator = storage.Hour < 12 ? culture.DateTimeFormat.AMDesignator : culture.DateTimeFormat.PMDesignator;
            return ApplyXPathDateTimeNamePresentation(designator, marker);
        }

        if (component == 'C')
        {
            string name = calendar == "ISO" ? "ISO" : "Gregorian";
            return ApplyXPathDateTimeNamePresentation(name, marker);
        }

        if (component == 'E')
        {
            string era = calendar == "ISO" ? storage.Year <= BigInteger.Zero ? "-" : "" : storage.Year <= BigInteger.Zero ? "BC" : "AD";
            return ApplyXPathDateTimeNamePresentation(era, marker);
        }

        if (component == 'f')
        {
            return FormatXPathFractionalSeconds(storage, marker);
        }

        BigInteger number = ReadXPathDateTimeComponentNumber(storage, component);
        if (component == 'Y')
        {
            int yearDigits = CountXPathDateTimePictureSigns(marker.Presentation);
            bool decimalPresentation = IsXPathDecimalDateTimePresentation(marker.Presentation);
            int maximum = marker.MaximumWidth > 0 ? marker.MaximumWidth : decimalPresentation && yearDigits >= 2 ? yearDigits : -1;
            if (maximum > 0)
            {
                number = number % Numerics.PowerOfTen(maximum);
            }
        }

        return FormatXPathDateTimeNumber(number, marker);
    }

    internal bool IsXPathNamePresentation(string presentation)
    {
        return presentation == "n" || presentation == "N" || presentation == "Nn";
    }

    internal string ApplyXPathDateTimeNamePresentation(string value, XPathDateTimePictureMarker marker)
    {
        string result = value;
        if (marker.Presentation == "N")
        {
            result = result.ToUpperInvariant();
        }
        else if (marker.Presentation == "n")
        {
            result = result.ToLowerInvariant();
        }
        else if (marker.Presentation == "Nn" && result.Length != 0)
        {
            result = char.ToUpperInvariant(result[0]) + result.Substring(1).ToLowerInvariant();
        }

        if (marker.MaximumWidth > 0 && result.Length > marker.MaximumWidth)
        {
            result = result.Substring(0, marker.MaximumWidth);
        }

        while (marker.MinimumWidth > 0 && result.Length < marker.MinimumWidth)
        {
            result = result + " ";
        }

        return result;
    }

    internal string FormatXPathDateTimeTimezone(XdmDateTimeStorage storage, XPathDateTimePictureMarker marker)
    {
        if (!storage.HasTimezone)
        {
            return marker.Presentation == "Z" ? "J" : "";
        }

        int absolute = storage.TimezoneMinutes < 0 ? -storage.TimezoneMinutes : storage.TimezoneMinutes;
        string sign = storage.TimezoneMinutes < 0 ? "-" : "+";
        string hour = (absolute / 60).ToString(CultureInfo.InvariantCulture);
        string minute = (absolute % 60).ToString("00", CultureInfo.InvariantCulture);
        string result;
        if (marker.Presentation == "Z")
        {
            result = FormatXPathMilitaryTimezone(storage.TimezoneMinutes);
        }
        else if (marker.Presentation == "N" || marker.Presentation == "n" || marker.Presentation == "Nn")
        {
            result = storage.TimezoneMinutes == 0 ? "GMT" : sign + hour.PadLeft(2, '0') + ":" + minute;
        }
        else if (marker.SecondModifier == 't' && storage.TimezoneMinutes == 0)
        {
            result = "Z";
        }
        else
        {
            result = FormatXPathNumericTimezone(sign, hour, minute, absolute, marker.Presentation);
        }

        if (marker.Component == 'z' && marker.Presentation != "N" && !result.StartsWith("GMT", StringComparison.Ordinal))
        {
            result = "GMT" + result;
        }

        while (marker.MinimumWidth > 0 && result.Length < marker.MinimumWidth)
        {
            result = "0" + result;
        }

        return result;
    }

    internal string FormatXPathNumericTimezone(string sign, string hour, string minute, int absolute, string presentation)
    {
        int separatorIndex = -1;
        int hourWidth = 0;
        int index = 0;
        while (index < presentation.Length)
        {
            char character = presentation[index];
            int width = char.IsHighSurrogate(character) && index + 1 < presentation.Length && char.IsLowSurrogate(presentation[index + 1]) ? 2 : 1;
            if (CharUnicodeInfo.GetUnicodeCategory(presentation, index) != UnicodeCategory.DecimalDigitNumber)
            {
                separatorIndex = index;
                break;
            }

            hourWidth = hourWidth + 1;
            index = index + width;
        }

        if (separatorIndex >= 0)
        {
            int separatorWidth = char.IsHighSurrogate(presentation[separatorIndex]) && separatorIndex + 1 < presentation.Length && char.IsLowSurrogate(presentation[separatorIndex + 1]) ? 2 : 1;
            string separator = presentation.Substring(separatorIndex, separatorWidth);
            return TranslateXPathDateTimeDigits(sign + hour.PadLeft(hourWidth, '0') + separator + minute, ReadXPathDateTimeZeroDigit(presentation));
        }

        int digitCount = CountXPathDateTimeDigits(presentation);
        if (digitCount >= 3)
        {
            hourWidth = digitCount - 2;
            return TranslateXPathDateTimeDigits(sign + hour.PadLeft(hourWidth, '0') + minute, ReadXPathDateTimeZeroDigit(presentation));
        }

        string formattedHour = hour.PadLeft(digitCount, '0');
        return TranslateXPathDateTimeDigits(sign + formattedHour + (absolute % 60 == 0 ? "" : ":" + minute), ReadXPathDateTimeZeroDigit(presentation));
    }

    internal string FormatXPathMilitaryTimezone(int timezoneMinutes)
    {
        if (timezoneMinutes % 60 != 0 || timezoneMinutes < -720 || timezoneMinutes > 720)
        {
            int absolute = timezoneMinutes < 0 ? -timezoneMinutes : timezoneMinutes;
            string sign = timezoneMinutes < 0 ? "-" : "+";
            return sign + (absolute / 60).ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + ":" + (absolute % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        int hours = timezoneMinutes / 60;
        if (hours == 0)
        {
            return "Z";
        }

        if (hours > 0)
        {
            int offset = hours - 1;
            return ((char)('A' + offset + (offset >= 9 ? 1 : 0))).ToString();
        }

        return ((char)('N' + -hours - 1)).ToString();
    }

    internal string FormatXPathFractionalSeconds(XdmDateTimeStorage storage, XPathDateTimePictureMarker marker)
    {
        BigInteger factor = Numerics.PowerOfTen(storage.SecondScale);
        BigInteger fraction = storage.SecondCoefficient % factor;
        string digits = fraction.ToString(CultureInfo.InvariantCulture).PadLeft(storage.SecondScale, '0');
        int minimum = CountXPathDateTimeDigits(marker.Presentation);
        int maximum = CountXPathDateTimePictureSigns(marker.Presentation);
        bool optional = false;
        int presentationIndex = 0;
        while (presentationIndex < marker.Presentation.Length)
        {
            char character = marker.Presentation[presentationIndex];
            if (character == '#')
            {
                optional = true;
            }
            else if (CharUnicodeInfo.GetUnicodeCategory(marker.Presentation, presentationIndex) == UnicodeCategory.DecimalDigitNumber && optional)
            {
                throw CreateXPathDateTimeError("An optional fractional digit cannot precede a mandatory digit.", XPathErrorCodes.FOFD1340);
            }

            presentationIndex = presentationIndex + (char.IsHighSurrogate(character) && presentationIndex + 1 < marker.Presentation.Length && char.IsLowSurrogate(marker.Presentation[presentationIndex + 1]) ? 2 : 1);
        }

        if (marker.Presentation == "1")
        {
            minimum = 1;
            maximum = storage.SecondScale;
        }

        if (marker.MinimumWidth > minimum)
        {
            minimum = marker.MinimumWidth;
        }

        if (marker.MaximumWidth > 0)
        {
            maximum = marker.MaximumWidth;
        }

        if (maximum < minimum)
        {
            maximum = minimum;
        }

        if (digits.Length < minimum)
        {
            digits = digits.PadRight(minimum, '0');
        }
        else if (maximum > 0 && digits.Length > maximum)
        {
            digits = digits.Substring(0, maximum);
        }

        while (digits.Length > minimum && digits.EndsWith("0", StringComparison.Ordinal))
        {
            digits = digits.Substring(0, digits.Length - 1);
        }

        StringBuilder grouped = new StringBuilder();
        int digitIndex = 0;
        presentationIndex = 0;
        int picturePosition = 0;
        while (presentationIndex < marker.Presentation.Length && digitIndex < digits.Length)
        {
            char character = marker.Presentation[presentationIndex];
            bool pictureDigit = character == '#' || CharUnicodeInfo.GetUnicodeCategory(marker.Presentation, presentationIndex) == UnicodeCategory.DecimalDigitNumber;
            if (pictureDigit)
            {
                if (picturePosition < digits.Length)
                {
                    grouped.Append(digits[picturePosition]);
                }

                picturePosition = picturePosition + 1;
                digitIndex = picturePosition;
            }
            else if (picturePosition > 0 && picturePosition < digits.Length)
            {
                grouped.Append(character);
            }

            presentationIndex = presentationIndex + (char.IsHighSurrogate(character) && presentationIndex + 1 < marker.Presentation.Length && char.IsLowSurrogate(marker.Presentation[presentationIndex + 1]) ? 2 : 1);
        }

        while (digitIndex < digits.Length)
        {
            grouped.Append(digits[digitIndex]);
            digitIndex = digitIndex + 1;
        }

        return TranslateXPathDateTimeDigits(grouped.ToString(), ReadXPathDateTimeZeroDigit(marker.Presentation));
    }

    internal BigInteger ReadXPathDateTimeComponentNumber(XdmDateTimeStorage storage, char component)
    {
        if (component == 'Y')
        {
            return BigInteger.Abs(storage.Year);
        }

        if (component == 'M')
        {
            return new BigInteger(storage.Month);
        }

        if (component == 'D')
        {
            return new BigInteger(storage.Day);
        }

        if (component == 'd')
        {
            BigInteger first = DateTimes.XdmCalendarDayNumber(storage.Year, 1, 1);
            return DateTimes.XdmCalendarDayNumber(storage.Year, storage.Month, storage.Day) - first + BigInteger.One;
        }

        if (component == 'F')
        {
            return new BigInteger(ReadXPathDayOfWeek(storage) == 0 ? 7 : ReadXPathDayOfWeek(storage));
        }

        if (component == 'W')
        {
            return new BigInteger(ReadXPathIsoWeek(storage));
        }

        if (component == 'w')
        {
            return new BigInteger(ReadXPathWeekInMonth(storage));
        }

        if (component == 'H')
        {
            return new BigInteger(storage.Hour);
        }

        if (component == 'h')
        {
            int hour = storage.Hour % 12;
            return new BigInteger(hour == 0 ? 12 : hour);
        }

        if (component == 'm')
        {
            return new BigInteger(storage.Minute);
        }

        BigInteger secondFactor = Numerics.PowerOfTen(storage.SecondScale);
        return storage.SecondCoefficient / secondFactor;
    }

    internal int ReadXPathDayOfWeek(XdmDateTimeStorage storage)
    {
        BigInteger day = DateTimes.XdmCalendarDayNumber(storage.Year, storage.Month, storage.Day);
        int result = (int)((day + 3) % 7);
        return result < 0 ? result + 7 : result;
    }

    internal int ReadXPathIsoWeek(XdmDateTimeStorage storage)
    {
        int sundayBased = ReadXPathDayOfWeek(storage);
        int mondayBased = sundayBased == 0 ? 7 : sundayBased;
        BigInteger day = DateTimes.XdmCalendarDayNumber(storage.Year, storage.Month, storage.Day);
        BigInteger thursday = day + 4 - mondayBased;
        XdmCalendarDate thursdayDate = DateTimes.XdmCalendarDateFromDayNumber(thursday);
        BigInteger januaryFourth = DateTimes.XdmCalendarDayNumber(thursdayDate.Year, 1, 4);
        XdmCalendarDate januaryFourthDate = DateTimes.XdmCalendarDateFromDayNumber(januaryFourth);
        int januarySunday = ReadXPathDayOfWeekStorage(januaryFourthDate);
        int januaryMonday = januarySunday == 0 ? 7 : januarySunday;
        BigInteger firstMonday = januaryFourth - (januaryMonday - 1);
        return (int)((thursday - firstMonday) / 7 + BigInteger.One);
    }

    internal int ReadXPathDayOfWeekStorage(XdmCalendarDate date)
    {
        XdmDateTimeStorage storage = new XdmDateTimeStorage();
        storage.Year = date.Year;
        storage.Month = date.Month;
        storage.Day = date.Day;
        return ReadXPathDayOfWeek(storage);
    }

    internal int ReadXPathWeekInMonth(XdmDateTimeStorage storage)
    {
        XdmDateTimeStorage first = new XdmDateTimeStorage();
        first.Year = storage.Year;
        first.Month = storage.Month;
        first.Day = 1;
        int sundayBased = ReadXPathDayOfWeek(first);
        int mondayBased = sundayBased == 0 ? 7 : sundayBased;
        int firstThursday = 1 + (4 - mondayBased + 7) % 7;
        int firstMonday = firstThursday - 3;
        if (storage.Day < firstMonday)
        {
            BigInteger previousDay = DateTimes.XdmCalendarDayNumber(storage.Year, storage.Month, 1) - BigInteger.One;
            XdmCalendarDate previousDate = DateTimes.XdmCalendarDateFromDayNumber(previousDay);
            XdmDateTimeStorage previous = new XdmDateTimeStorage();
            previous.Year = previousDate.Year;
            previous.Month = previousDate.Month;
            previous.Day = previousDate.Day;
            return ReadXPathWeekInMonth(previous);
        }

        return (storage.Day - firstMonday) / 7 + 1;
    }

    internal string FormatXPathDateTimeNumber(BigInteger number, XPathDateTimePictureMarker marker)
    {
        string presentation = marker.Presentation;
        string result;
        if (presentation == "i" || presentation == "I")
        {
            result = FormatXPathRomanNumber(number);
            if (presentation == "i")
            {
                result = result.ToLowerInvariant();
            }
        }
        else if (presentation == "a" || presentation == "A")
        {
            result = FormatXPathAlphabeticNumber(number);
            if (presentation == "a")
            {
                result = result.ToLowerInvariant();
            }
        }
        else if (presentation == "w" || presentation == "W" || presentation == "Ww")
        {
            result = marker.Ordinal ? FormatXPathOrdinalNumberAsWords(number) : FormatXPathNumberAsWords(number);
            if (presentation == "W")
            {
                result = result.ToUpperInvariant();
            }
            else if (presentation == "w")
            {
                result = result.ToLowerInvariant();
            }
        }
        else
        {
            string zero = ReadXPathDateTimeZeroDigit(presentation);
            if (zero.Length > 1)
            {
                result = number.ToString(CultureInfo.InvariantCulture).PadLeft(CountXPathDateTimeDigits(presentation), '0');
            }
            else
            {
                result = new XPathNumberFormattingFunctionImplementation("format-integer", Range, StaticContext).FormatXPathIntegerValue(number, presentation);
            }

            result = TranslateXPathDateTimeDigits(result, zero);
        }

        if (marker.Ordinal && presentation != "w" && presentation != "W" && presentation != "Ww")
        {
            result = result + ReadXPathEnglishOrdinalSuffix(number);
        }

        if (!IsXPathDecimalDateTimePresentation(presentation))
        {
            while (marker.MinimumWidth > 0 && result.Length < marker.MinimumWidth)
            {
                result = result + " ";
            }

            return result;
        }

        string padding = ReadXPathDateTimeZeroDigit(presentation);
        return ApplyXPathDateTimeWidth(result, marker, padding);
    }

    internal int CountXPathDateTimeDigits(string presentation)
    {
        int result = 0;
        int index = 0;
        while (index < presentation.Length)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(presentation, index);
            if (category == UnicodeCategory.DecimalDigitNumber)
            {
                result = result + 1;
            }

            index = index + (char.IsHighSurrogate(presentation[index]) && index + 1 < presentation.Length && char.IsLowSurrogate(presentation[index + 1]) ? 2 : 1);
        }

        return result;
    }

    internal int CountXPathDateTimePictureSigns(string presentation)
    {
        int result = 0;
        int index = 0;
        while (index < presentation.Length)
        {
            char character = presentation[index];
            if (character == '#' || CharUnicodeInfo.GetUnicodeCategory(presentation, index) == UnicodeCategory.DecimalDigitNumber)
            {
                result = result + 1;
            }

            index = index + (char.IsHighSurrogate(character) && index + 1 < presentation.Length && char.IsLowSurrogate(presentation[index + 1]) ? 2 : 1);
        }

        return result;
    }

    internal bool IsXPathDecimalDateTimePresentation(string presentation)
    {
        int index = 0;
        while (index < presentation.Length)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(presentation, index) == UnicodeCategory.DecimalDigitNumber)
            {
                return true;
            }

            index = index + (char.IsHighSurrogate(presentation[index]) && index + 1 < presentation.Length && char.IsLowSurrogate(presentation[index + 1]) ? 2 : 1);
        }

        return false;
    }

    internal string ReadXPathDateTimeZeroDigit(string presentation)
    {
        int index = 0;
        while (index < presentation.Length)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(presentation, index) == UnicodeCategory.DecimalDigitNumber)
            {
                int codepoint = char.ConvertToUtf32(presentation, index);
                int numeric = CharUnicodeInfo.GetDecimalDigitValue(presentation, index);
                return char.ConvertFromUtf32(codepoint - numeric);
            }

            index = index + (char.IsHighSurrogate(presentation[index]) && index + 1 < presentation.Length && char.IsLowSurrogate(presentation[index + 1]) ? 2 : 1);
        }

        return "0";
    }

    internal string TranslateXPathDateTimeDigits(string value, string zero)
    {
        if (zero == "0")
        {
            return value;
        }

        StringBuilder result = new StringBuilder();
        int zeroCodepoint = char.ConvertToUtf32(zero, 0);
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character >= '0' && character <= '9')
            {
                result.Append(char.ConvertFromUtf32(zeroCodepoint + character - '0'));
            }
            else
            {
                result.Append(character);
            }
            index = index + 1;
        }

        return result.ToString();
    }

    internal string ApplyXPathDateTimeWidth(string value, XPathDateTimePictureMarker marker, string padding)
    {
        string result = value;
        while (marker.MinimumWidth > 0 && result.Length < marker.MinimumWidth)
        {
            result = padding + result;
        }

        if (marker.MaximumWidth > 0 && result.Length > marker.MaximumWidth)
        {
            result = result.Substring(result.Length - marker.MaximumWidth);
        }

        return result;
    }

    internal string FormatXPathRomanNumber(BigInteger number)
    {
        if (number <= BigInteger.Zero || number > new BigInteger(3999))
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        int remaining = (int)number;
        int[] values = [1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1];
        string[] symbols = ["M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I"];
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            while (remaining >= values[index])
            {
                result.Append(symbols[index]);
                remaining = remaining - values[index];
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal string FormatXPathAlphabeticNumber(BigInteger number)
    {
        if (number <= BigInteger.Zero)
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        StringBuilder reversed = new StringBuilder();
        BigInteger remaining = number;
        while (remaining > BigInteger.Zero)
        {
            remaining = remaining - BigInteger.One;
            reversed.Append((char)('A' + (int)(remaining % 26)));
            remaining = remaining / 26;
        }

        StringBuilder result = new StringBuilder();
        int index = reversed.Length - 1;
        while (index >= 0)
        {
            result.Append(reversed[index]);
            index = index - 1;
        }

        return result.ToString();
    }

    internal string FormatXPathNumberAsWords(BigInteger number)
    {
        if (number < BigInteger.Zero || number > new BigInteger(999999))
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        int value = (int)number;
        if (value == 0)
        {
            return "Zero";
        }

        string result = "";
        if (value >= 1000)
        {
            result = FormatXPathNumberUnderThousand(value / 1000) + " Thousand";
            value = value % 1000;
        }

        if (value > 0)
        {
            if (result.Length > 0)
            {
                result = result + (value < 100 ? " and " : " ");
            }

            result = result + FormatXPathNumberUnderThousand(value);
        }

        return result;
    }

    internal string FormatXPathOrdinalNumberAsWords(BigInteger number)
    {
        string cardinal = FormatXPathNumberAsWords(number);
        if (number < BigInteger.Zero || number > new BigInteger(999999))
        {
            return cardinal + ReadXPathEnglishOrdinalSuffix(number);
        }

        string[] cardinals = ["One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety", "Hundred", "Thousand"];
        string[] ordinals = ["First", "Second", "Third", "Fourth", "Fifth", "Sixth", "Seventh", "Eighth", "Ninth", "Tenth", "Eleventh", "Twelfth", "Thirteenth", "Fourteenth", "Fifteenth", "Sixteenth", "Seventeenth", "Eighteenth", "Nineteenth", "Twentieth", "Thirtieth", "Fortieth", "Fiftieth", "Sixtieth", "Seventieth", "Eightieth", "Ninetieth", "Hundredth", "Thousandth"];
        int index = 0;
        while (index < cardinals.Length)
        {
            if (cardinal.EndsWith(cardinals[index], StringComparison.Ordinal))
            {
                return cardinal.Substring(0, cardinal.Length - cardinals[index].Length) + ordinals[index];
            }

            index = index + 1;
        }

        return cardinal + ReadXPathEnglishOrdinalSuffix(number);
    }

    internal string FormatXPathNumberUnderThousand(int value)
    {
        string[] small = ["Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"];
        string[] tens = ["", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"];
        string result = "";
        if (value >= 100)
        {
            result = small[value / 100] + " Hundred";
            value = value % 100;
            if (value > 0)
            {
                result = result + " and ";
            }
        }

        if (value >= 20)
        {
            result = result + tens[value / 10];
            if (value % 10 > 0)
            {
                result = result + "-" + small[value % 10];
            }
        }
        else if (value > 0)
        {
            result = result + small[value];
        }

        return result;
    }

    internal string ReadXPathEnglishOrdinalSuffix(BigInteger number)
    {
        BigInteger absolute = BigInteger.Abs(number);
        int lastTwo = (int)(absolute % 100);
        if (lastTwo >= 11 && lastTwo <= 13)
        {
            return "th";
        }

        int last = (int)(absolute % 10);
        if (last == 1)
        {
            return "st";
        }

        if (last == 2)
        {
            return "nd";
        }

        return last == 3 ? "rd" : "th";
    }

    internal XdmAtomicValue? ReadXPathOptionalAtomic(XdmSequence supplied, XdmAtomicType type)
    {
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = type;
        expected.Occurrence = XdmOccurrence.ZeroOrOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        if (converted.Count == 0)
        {
            return null;
        }

        return Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
    }

    internal string ReadXPathRequiredString(XdmSequence supplied)
    {
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.String;
        expected.Occurrence = XdmOccurrence.ExactlyOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
        return value == null ? "" : Converter.ReadXdmAtomicStringValue(value);
    }

    internal string? ReadXPathOptionalString(XdmSequence supplied)
    {
        XdmAtomicValue? value = ReadXPathOptionalAtomic(supplied, XdmAtomicType.String);
        return value == null ? null : Converter.ReadXdmAtomicStringValue(value);
    }

    internal XPathDynamicException CreateXPathDateTimeError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = Range;
        return new XPathDynamicException(message, diagnostic, Range.Start);
    }
}
