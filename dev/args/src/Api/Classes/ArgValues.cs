using System.Collections.Generic;

namespace args;

/// <summary>
/// Looks up parsed values by symbol name and converts them to common .NET types.
/// </summary>
public class ArgValues : IArgValues
{
    /// <summary>
    /// The parse result associated with this operation.
    /// </summary>
    internal ArgParseResult? ParseResult;
    internal ArgValueConverter Converter;
    /// <summary>
    /// The most recent conversion result produced by this value facade.
    /// </summary>
    internal ArgConvertResult LastConversionResult;
    /// <summary>
    /// All failed conversions recorded since construction or the last clear operation.
    /// </summary>
    internal List<ArgConvertResult> ConversionErrors;

    /// <summary>
    /// Creates a lookup and conversion facade over the supplied parse result.
    /// </summary>
    /// <param name="parseResult">The parse result.</param>
    /// <exception cref="System.ArgumentNullException">The parse result is null.</exception>
    public ArgValues(ArgParseResult parseResult)
    {
        if (parseResult == null)
        {
            throw new System.ArgumentNullException("parseResult");
        }

        ParseResult = parseResult;
        Converter = new ArgValueConverter();
        Converter.UseCulture(parseResult.Settings.ConversionCulture);
        LastConversionResult = ArgConvertResult.Empty();
        ConversionErrors = new List<ArgConvertResult>();
    }

    /// <summary>
    /// Returns the parse result associated with this facade.
    /// </summary>
    /// <returns>The retained parse result.</returns>
    public ArgParseResult GetParseResult()
    {
        if (ParseResult == null)
        {
            throw new System.InvalidOperationException("The values facade has no parse result.");
        }

        return ParseResult;
    }

    /// <summary>
    /// Returns the most recent conversion result.
    /// </summary>
    /// <returns>The most recent conversion result.</returns>
    public ArgConvertResult GetLastConversionResult()
    {
        return LastConversionResult;
    }

    /// <summary>
    /// Returns all recorded conversion failures.
    /// </summary>
    /// <returns>The retained conversion-error list.</returns>
    public List<ArgConvertResult> GetConversionErrors()
    {
        return ConversionErrors;
    }

    /// <summary>
    /// Returns whether one or more value conversions have failed since construction or the last clear operation.
    /// </summary>
    /// <returns>True when recorded conversion failures exist; otherwise false.</returns>
    public bool HasConversionErrors()
    {
        return ConversionErrors.Count > 0;
    }

    internal static ArgValues Empty()
    {
        ArgCommand command = new ArgCommand("command");
        ArgParseResult parseResult = new ArgParseResult(command, new List<ArgToken>());
        return new ArgValues(parseResult);
    }

    /// <summary>
    /// Returns whether the named symbol has a resolved occurrence.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    public bool Has(string name)
    {
        return Occurrence(name) != null;
    }

    /// <summary>
    /// Returns whether the named value came from an explicit command-line token.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    public bool IsSupplied(string name)
    {
        int index = 0;
        while (ParseResult != null && index < ParseResult.Occurrences.Count)
        {
            ArgOccurrence occurrence = ParseResult.Occurrences[index];
            if (OccurrenceMatchesName(occurrence, name) && occurrence.TokenIndex >= 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Returns whether the named value came from a configured default.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    public bool IsDefaulted(string name)
    {
        int index = 0;
        while (ParseResult != null && index < ParseResult.Occurrences.Count)
        {
            ArgOccurrence occurrence = ParseResult.Occurrences[index];
            if (OccurrenceMatchesName(occurrence, name) && occurrence.Source == ArgValueSource.Default)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Returns the source of the named value, or None when the value is absent.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The resolved value source, or ArgValueSource.None when absent.</returns>
    public ArgValueSource Source(string name)
    {
        ArgOccurrence? occurrence = Occurrence(name);
        if (occurrence == null)
        {
            return ArgValueSource.None;
        }

        return occurrence.Source;
    }

    /// <summary>
    /// Returns the accumulated count for a named count option.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The total count, or zero when the count option is absent.</returns>
    public int Count(string name)
    {
        int total = 0;
        int index = 0;
        while (ParseResult != null && index < ParseResult.Occurrences.Count)
        {
            ArgOccurrence occurrence = ParseResult.Occurrences[index];
            if (OccurrenceMatchesName(occurrence, name) && occurrence.Option != null && occurrence.Option.IsCount)
            {
                total = total + occurrence.Count;
            }

            index = index + 1;
        }

        return total;
    }

    /// <summary>
    /// Returns the first normalized text value for the named symbol, or an empty string when absent or valueless.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The first text value, or an empty string when no value is available.</returns>
    public string String(string name)
    {
        ArgOccurrence? occurrence = Occurrence(name);
        if (occurrence == null)
        {
            return "";
        }

        if (occurrence.Values.Count == 0)
        {
            return NormalizeOccurrenceValue(occurrence, "");
        }

        return NormalizeOccurrenceValue(occurrence, occurrence.Values[0]);
    }

    /// <summary>
    /// Returns every normalized text value for the named symbol in encounter order.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>A new array containing all matching text values.</returns>
    public string[] Strings(string name)
    {
        List<string> values = new List<string>();
        int occurrenceIndex = 0;
        while (ParseResult != null && occurrenceIndex < ParseResult.Occurrences.Count)
        {
            ArgOccurrence occurrence = ParseResult.Occurrences[occurrenceIndex];
            if (OccurrenceMatchesName(occurrence, name))
            {
                int valueIndex = 0;
                while (valueIndex < occurrence.Values.Count)
                {
                    values.Add(NormalizeOccurrenceValue(occurrence, occurrence.Values[valueIndex]));
                    valueIndex = valueIndex + 1;
                }
            }

            occurrenceIndex = occurrenceIndex + 1;
        }

        return values.ToArray();
    }

    internal string NormalizeOccurrenceValue(ArgOccurrence occurrence, string value)
    {
        if (occurrence.Option != null)
        {
            return ArgValuePipeline.Normalize(value, occurrence.Option.ValueNormalizer);
        }

        if (occurrence.Argument != null)
        {
            return ArgValuePipeline.Normalize(value, occurrence.Argument.ValueNormalizer);
        }

        return value;
    }

    /// <summary>
    /// Converts the named value to Boolean and records conversion details.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The converted bool value, or its zero value after a failed conversion.</returns>
    public bool Bool(string name)
    {
        ArgOccurrence? occurrence = Occurrence(name);
        if (occurrence == null)
        {
            RecordConversion(Converter.ConvertBool(""));
            return false;
        }

        if (occurrence.Values.Count == 0)
        {
            if (occurrence.Option != null && occurrence.Option.IsFlag)
            {
                RecordConversion(ArgConvertResult.Detailed(true, true, "", "bool", ""));
                return true;
            }

            if (occurrence.Option != null && occurrence.Option.IsCount)
            {
                RecordConversion(ArgConvertResult.Detailed(true, occurrence.Count > 0, "", "bool", ""));
                return occurrence.Count > 0;
            }

            RecordConversion(Converter.ConvertBool(""));
            return false;
        }

        ArgConvertResult conversion = Converter.ConvertBool(NormalizeOccurrenceValue(occurrence, occurrence.Values[0]));
        RecordConversion(conversion);
        if (conversion.Success && conversion.Value != null)
        {
            return (bool)conversion.Value;
        }

        return false;
    }

    /// <summary>
    /// Converts the named value to a 32-bit integer and records conversion details.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The converted int value, or its zero value after a failed conversion.</returns>
    public int Int(string name)
    {
        ArgConvertResult conversion = Converter.ConvertInt(String(name));
        RecordConversion(conversion);
        if (conversion.Success && conversion.Value != null)
        {
            return (int)conversion.Value;
        }

        return 0;
    }

    /// <summary>
    /// Converts the named value to a 64-bit integer and records conversion details.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The converted long value, or its zero value after a failed conversion.</returns>
    public long Long(string name)
    {
        ArgConvertResult conversion = Converter.ConvertLong(String(name));
        RecordConversion(conversion);
        if (conversion.Success && conversion.Value != null)
        {
            return (long)conversion.Value;
        }

        return 0;
    }

    /// <summary>
    /// Converts the named value to a double-precision number using the configured conversion culture.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The converted double value, or its zero value after a failed conversion.</returns>
    public double Double(string name)
    {
        ArgConvertResult conversion = Converter.ConvertDouble(String(name));
        RecordConversion(conversion);
        if (conversion.Success && conversion.Value != null)
        {
            return (double)conversion.Value;
        }

        return 0;
    }

    /// <summary>
    /// Converts the named value to a decimal number using the configured conversion culture.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The converted decimal value, or its zero value after a failed conversion.</returns>
    public decimal Decimal(string name)
    {
        ArgConvertResult conversion = Converter.ConvertDecimal(String(name));
        RecordConversion(conversion);
        if (conversion.Success && conversion.Value != null)
        {
            return (decimal)conversion.Value;
        }

        return 0;
    }

    /// <summary>
    /// Converts the named value to a date and time using the configured conversion culture.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The converted <c>System.DateTime</c>, or its default value after a failed conversion.</returns>
    public System.DateTime DateTime(string name)
    {
        ArgConvertResult conversion = Converter.ConvertDateTime(String(name));
        RecordConversion(conversion);
        if (conversion.Success && conversion.Value != null)
        {
            return (System.DateTime)conversion.Value;
        }

        return new System.DateTime();
    }

    /// <summary>
    /// Converts the named value to a time interval using the configured conversion culture.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The converted <c>System.TimeSpan</c>, or its default value after a failed conversion.</returns>
    public System.TimeSpan TimeSpan(string name)
    {
        ArgConvertResult conversion = Converter.ConvertTimeSpan(String(name));
        RecordConversion(conversion);
        if (conversion.Success && conversion.Value != null)
        {
            return (System.TimeSpan)conversion.Value;
        }

        return new System.TimeSpan();
    }

    /// <summary>
    /// Creates file information from the named text value.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>A System.IO.FileInfo created from the parsed text.</returns>
    /// <remarks>This conversion does not check whether the path exists. An absent or empty value produces a descriptor for the current directory path.</remarks>
    public System.IO.FileInfo FileInfo(string name)
    {
        ArgConvertResult conversion = Converter.ConvertFileInfo(String(name));
        RecordConversion(conversion);
        if (conversion.Value != null)
        {
            return (System.IO.FileInfo)conversion.Value;
        }

        return new System.IO.FileInfo(".");
    }

    /// <summary>
    /// Creates directory information from the named text value.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>A System.IO.DirectoryInfo created from the parsed text.</returns>
    /// <remarks>This conversion does not check whether the path exists. An absent or empty value produces a descriptor for the current directory.</remarks>
    public System.IO.DirectoryInfo DirectoryInfo(string name)
    {
        ArgConvertResult conversion = Converter.ConvertDirectoryInfo(String(name));
        RecordConversion(conversion);
        if (conversion.Value != null)
        {
            return (System.IO.DirectoryInfo)conversion.Value;
        }

        return new System.IO.DirectoryInfo(".");
    }

    /// <summary>
    /// Parses the named value as a member of the supplied enum type without regard to case and records conversion details.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <param name="enumType">A System.Type that represents an enum.</param>
    /// <returns>The resolved value when available and valid; otherwise null.</returns>
    /// <exception cref="System.ArgumentNullException">The enum type is null.</exception>
    /// <exception cref="System.ArgumentException">The supplied type is not an enum.</exception>
    public object? EnumValue(string name, System.Type enumType)
    {
        ArgConvertResult conversion = Converter.ConvertEnumValue(String(name), enumType);
        RecordConversion(conversion);
        return conversion.Value;
    }

    /// <summary>
    /// Converts every matching text value to Boolean in encounter order.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>An array containing one converted value per encountered text value and one true value per valueless flag occurrence.</returns>
    /// <remarks>Failed conversions contribute false at their original positions and are recorded individually.</remarks>
    public bool[] Bools(string name)
    {
        List<bool> values = new List<bool>();
        int occurrenceIndex = 0;
        while (ParseResult != null && occurrenceIndex < ParseResult.Occurrences.Count)
        {
            ArgOccurrence occurrence = ParseResult.Occurrences[occurrenceIndex];
            if (OccurrenceMatchesName(occurrence, name))
            {
                if (occurrence.Values.Count == 0 && occurrence.Option != null && occurrence.Option.IsFlag)
                {
                    ArgConvertResult flagConversion = ArgConvertResult.Detailed(true, true, "", "bool", "");
                    RecordConversion(flagConversion);
                    values.Add(true);
                }

                int valueIndex = 0;
                while (valueIndex < occurrence.Values.Count)
                {
                    string text = NormalizeOccurrenceValue(occurrence, occurrence.Values[valueIndex]);
                    ArgConvertResult conversion = Converter.ConvertBool(text);
                    RecordConversion(conversion);
                    if (conversion.Success && conversion.Value != null)
                    {
                        values.Add((bool)conversion.Value);
                    }
                    else
                    {
                        values.Add(false);
                    }

                    valueIndex = valueIndex + 1;
                }
            }

            occurrenceIndex = occurrenceIndex + 1;
        }

        return values.ToArray();
    }

    /// <summary>
    /// Converts every matching text value to a 32-bit integer in encounter order.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>An array containing one converted value per encountered text value.</returns>
    /// <remarks>Failed conversions contribute zero at their original positions and are recorded individually.</remarks>
    public int[] Ints(string name)
    {
        string[] texts = Strings(name);
        List<int> values = new List<int>();
        int index = 0;
        while (index < texts.Length)
        {
            ArgConvertResult conversion = Converter.ConvertInt(texts[index]);
            RecordConversion(conversion);
            if (conversion.Success && conversion.Value != null)
            {
                values.Add((int)conversion.Value);
            }
            else
            {
                values.Add(0);
            }

            index = index + 1;
        }

        return values.ToArray();
    }

    /// <summary>
    /// Converts every matching text value to a 64-bit integer in encounter order.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>An array containing one converted value per encountered text value.</returns>
    /// <remarks>Failed conversions contribute zero at their original positions and are recorded individually.</remarks>
    public long[] Longs(string name)
    {
        string[] texts = Strings(name);
        List<long> values = new List<long>();
        int index = 0;
        while (index < texts.Length)
        {
            ArgConvertResult conversion = Converter.ConvertLong(texts[index]);
            RecordConversion(conversion);
            if (conversion.Success && conversion.Value != null)
            {
                values.Add((long)conversion.Value);
            }
            else
            {
                values.Add(0);
            }

            index = index + 1;
        }

        return values.ToArray();
    }

    /// <summary>
    /// Converts every matching text value to a double-precision number in encounter order.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>An array containing one converted value per encountered text value.</returns>
    /// <remarks>Failed conversions contribute zero at their original positions and are recorded individually.</remarks>
    public double[] Doubles(string name)
    {
        string[] texts = Strings(name);
        List<double> values = new List<double>();
        int index = 0;
        while (index < texts.Length)
        {
            ArgConvertResult conversion = Converter.ConvertDouble(texts[index]);
            RecordConversion(conversion);
            if (conversion.Success && conversion.Value != null)
            {
                values.Add((double)conversion.Value);
            }
            else
            {
                values.Add(0);
            }

            index = index + 1;
        }

        return values.ToArray();
    }

    /// <summary>
    /// Converts every matching text value to a decimal number in encounter order.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>An array containing one converted value per encountered text value.</returns>
    /// <remarks>Failed conversions contribute zero at their original positions and are recorded individually.</remarks>
    public decimal[] Decimals(string name)
    {
        string[] texts = Strings(name);
        List<decimal> values = new List<decimal>();
        int index = 0;
        while (index < texts.Length)
        {
            ArgConvertResult conversion = Converter.ConvertDecimal(texts[index]);
            RecordConversion(conversion);
            if (conversion.Success && conversion.Value != null)
            {
                values.Add((decimal)conversion.Value);
            }
            else
            {
                values.Add(0);
            }

            index = index + 1;
        }

        return values.ToArray();
    }

    /// <summary>
    /// Converts every matching text value to a date and time in encounter order.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>An array containing one converted value per encountered text value.</returns>
    /// <remarks>Failed conversions contribute the default date and time at their original positions and are recorded individually.</remarks>
    public System.DateTime[] DateTimes(string name)
    {
        string[] texts = Strings(name);
        List<System.DateTime> values = new List<System.DateTime>();
        int index = 0;
        while (index < texts.Length)
        {
            ArgConvertResult conversion = Converter.ConvertDateTime(texts[index]);
            RecordConversion(conversion);
            if (conversion.Success && conversion.Value != null)
            {
                values.Add((System.DateTime)conversion.Value);
            }
            else
            {
                values.Add(new System.DateTime());
            }

            index = index + 1;
        }

        return values.ToArray();
    }

    /// <summary>
    /// Converts every matching text value to a time interval in encounter order.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>An array containing one converted value per encountered text value.</returns>
    /// <remarks>Failed conversions contribute the default time interval at their original positions and are recorded individually.</remarks>
    public System.TimeSpan[] TimeSpans(string name)
    {
        string[] texts = Strings(name);
        List<System.TimeSpan> values = new List<System.TimeSpan>();
        int index = 0;
        while (index < texts.Length)
        {
            ArgConvertResult conversion = Converter.ConvertTimeSpan(texts[index]);
            RecordConversion(conversion);
            if (conversion.Success && conversion.Value != null)
            {
                values.Add((System.TimeSpan)conversion.Value);
            }
            else
            {
                values.Add(new System.TimeSpan());
            }

            index = index + 1;
        }

        return values.ToArray();
    }

    /// <summary>
    /// Creates file information from every matching text value in encounter order.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>An array containing one file descriptor per encountered text value.</returns>
    public System.IO.FileInfo[] FileInfos(string name)
    {
        string[] texts = Strings(name);
        List<System.IO.FileInfo> values = new List<System.IO.FileInfo>();
        int index = 0;
        while (index < texts.Length)
        {
            ArgConvertResult conversion = Converter.ConvertFileInfo(texts[index]);
            RecordConversion(conversion);
            if (conversion.Value != null)
            {
                values.Add((System.IO.FileInfo)conversion.Value);
            }

            index = index + 1;
        }

        return values.ToArray();
    }

    /// <summary>
    /// Creates directory information from every matching text value in encounter order.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>An array containing one directory descriptor per encountered text value.</returns>
    public System.IO.DirectoryInfo[] DirectoryInfos(string name)
    {
        string[] texts = Strings(name);
        List<System.IO.DirectoryInfo> values = new List<System.IO.DirectoryInfo>();
        int index = 0;
        while (index < texts.Length)
        {
            ArgConvertResult conversion = Converter.ConvertDirectoryInfo(texts[index]);
            RecordConversion(conversion);
            if (conversion.Value != null)
            {
                values.Add((System.IO.DirectoryInfo)conversion.Value);
            }

            index = index + 1;
        }

        return values.ToArray();
    }

    /// <summary>
    /// Converts every matching text value to a member of the supplied enum type in encounter order.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <param name="enumType">A System.Type that represents an enum.</param>
    /// <returns>An array containing one converted value per encountered text value.</returns>
    /// <remarks>Failed conversions contribute null at their original positions and are recorded individually.</remarks>
    /// <exception cref="System.ArgumentNullException">The enum type is null.</exception>
    /// <exception cref="System.ArgumentException">The supplied type is not an enum.</exception>
    public object?[] EnumValues(string name, System.Type enumType)
    {
        if (enumType == null)
        {
            throw new System.ArgumentNullException("enumType");
        }

        System.Enum.GetNames(enumType);
        string[] texts = Strings(name);
        List<object?> values = new List<object?>();
        int index = 0;
        while (index < texts.Length)
        {
            ArgConvertResult conversion = Converter.ConvertEnumValue(texts[index], enumType);
            RecordConversion(conversion);
            values.Add(conversion.Value);
            index = index + 1;
        }

        return values.ToArray();
    }

    internal void RecordConversion(ArgConvertResult conversion)
    {
        LastConversionResult = conversion;
        if (!conversion.Success)
        {
            ConversionErrors.Add(conversion);
        }
    }

    /// <summary>
    /// Clears recorded conversion failures without changing the most recent conversion result.
    /// </summary>
    public void ClearConversionErrors()
    {
        ConversionErrors.Clear();
    }

    /// <summary>
    /// Registers or replaces a custom converter under a logical type name.
    /// </summary>
    /// <param name="typeName">The logical type name used to register or select the converter.</param>
    /// <param name="converter">The function that converts one string into a detailed conversion result. A null result is normalized to a conversion failure.</param>
    /// <returns>The same ArgValues instance so additional configuration calls can be chained.</returns>
    /// <exception cref="System.ArgumentNullException">The type name or converter is null.</exception>
    public ArgValues RegisterConverter(string typeName, System.Func<string, ArgConvertResult?> converter)
    {
        Converter.Register(typeName, converter);
        return this;
    }

    /// <summary>
    /// Runs a registered custom converter for the named parsed value and returns its detailed result.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <param name="typeName">The logical type name used to register or select the converter.</param>
    /// <returns>The detailed custom conversion result.</returns>
    public ArgConvertResult CustomResult(string name, string typeName)
    {
        ArgConvertResult conversion = Converter.ConvertCustom(typeName, String(name));
        RecordConversion(conversion);
        return conversion;
    }

    /// <summary>
    /// Runs a registered custom converter and returns its value, or null when conversion fails.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <param name="typeName">The logical type name used to register or select the converter.</param>
    /// <returns>The resolved value when available and valid; otherwise null.</returns>
    public object? Custom(string name, string typeName)
    {
        ArgConvertResult conversion = CustomResult(name, typeName);
        return conversion.Value;
    }

    /// <summary>
    /// Returns the first occurrence matching a canonical name or alias, or null when absent.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The resolved value when available and valid; otherwise null.</returns>
    public ArgOccurrence? Occurrence(string name)
    {
        int index = 0;
        while (ParseResult != null && index < ParseResult.Occurrences.Count)
        {
            ArgOccurrence occurrence = ParseResult.Occurrences[index];
            if (OccurrenceMatchesName(occurrence, name))
            {
                return occurrence;
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>
    /// Returns every occurrence matching a canonical name or alias.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>A new list containing the matching occurrences.</returns>
    public List<ArgOccurrence> Occurrences(string name)
    {
        List<ArgOccurrence> matches = new List<ArgOccurrence>();
        int index = 0;
        while (ParseResult != null && index < ParseResult.Occurrences.Count)
        {
            ArgOccurrence occurrence = ParseResult.Occurrences[index];
            if (OccurrenceMatchesName(occurrence, name))
            {
                matches.Add(occurrence);
            }

            index = index + 1;
        }

        return matches;
    }

    internal bool OccurrenceMatchesName(ArgOccurrence occurrence, string name)
    {
        if (occurrence.SymbolName == name)
        {
            return true;
        }

        if (occurrence.Option != null && occurrence.Option.HasAlias(name))
        {
            return true;
        }

        if (occurrence.Argument != null && occurrence.Argument.Name == name)
        {
            return true;
        }

        return false;
    }
}
