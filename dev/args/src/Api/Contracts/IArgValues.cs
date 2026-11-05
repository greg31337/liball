using System.Collections.Generic;

namespace args;

/// <summary>
/// Defines parsed-value lookup, conversion, and conversion-diagnostic operations.
/// </summary>
public interface IArgValues
{
    /// <summary>Returns the associated parse result.</summary>
    ArgParseResult GetParseResult();
    /// <summary>Returns the most recent conversion result.</summary>
    ArgConvertResult GetLastConversionResult();
    /// <summary>Returns recorded conversion failures.</summary>
    List<ArgConvertResult> GetConversionErrors();
    /// <summary>Returns whether value conversion failures are recorded.</summary>
    bool HasConversionErrors();
    /// <summary>Returns whether a named value exists.</summary>
    bool Has(string name);
    /// <summary>Returns whether a named value was supplied explicitly.</summary>
    bool IsSupplied(string name);
    /// <summary>Returns whether a named value was defaulted.</summary>
    bool IsDefaulted(string name);
    /// <summary>Returns the source of a named value.</summary>
    ArgValueSource Source(string name);
    /// <summary>Returns the accumulated count for a named count option.</summary>
    int Count(string name);
    /// <summary>Returns the first text value.</summary>
    string String(string name);
    /// <summary>Returns all text values.</summary>
    string[] Strings(string name);
    /// <summary>Converts a named value to Boolean.</summary>
    bool Bool(string name);
    /// <summary>Converts a named value to a 32-bit integer.</summary>
    int Int(string name);
    /// <summary>Converts a named value to a 64-bit integer.</summary>
    long Long(string name);
    /// <summary>Converts a named value to a double-precision number.</summary>
    double Double(string name);
    /// <summary>Converts a named value to a decimal number.</summary>
    decimal Decimal(string name);
    /// <summary>Converts a named value to a date and time.</summary>
    System.DateTime DateTime(string name);
    /// <summary>Converts a named value to a time interval.</summary>
    System.TimeSpan TimeSpan(string name);
    /// <summary>Creates file information from a named value.</summary>
    System.IO.FileInfo FileInfo(string name);
    /// <summary>Creates directory information from a named value.</summary>
    System.IO.DirectoryInfo DirectoryInfo(string name);
    /// <summary>Converts a named value to an enum member.</summary>
    object? EnumValue(string name, System.Type enumType);
    /// <summary>Converts all matching values to Boolean.</summary>
    bool[] Bools(string name);
    /// <summary>Converts all matching values to 32-bit integers.</summary>
    int[] Ints(string name);
    /// <summary>Converts all matching values to 64-bit integers.</summary>
    long[] Longs(string name);
    /// <summary>Converts all matching values to double-precision numbers.</summary>
    double[] Doubles(string name);
    /// <summary>Converts all matching values to decimal numbers.</summary>
    decimal[] Decimals(string name);
    /// <summary>Converts all matching values to dates and times.</summary>
    System.DateTime[] DateTimes(string name);
    /// <summary>Converts all matching values to time intervals.</summary>
    System.TimeSpan[] TimeSpans(string name);
    /// <summary>Creates file information for all matching values.</summary>
    System.IO.FileInfo[] FileInfos(string name);
    /// <summary>Creates directory information for all matching values.</summary>
    System.IO.DirectoryInfo[] DirectoryInfos(string name);
    /// <summary>Converts all matching values to enum members.</summary>
    object?[] EnumValues(string name, System.Type enumType);
    /// <summary>Clears recorded conversion failures.</summary>
    void ClearConversionErrors();
    /// <summary>Registers a custom value converter.</summary>
    ArgValues RegisterConverter(string typeName, System.Func<string, ArgConvertResult?> converter);
    /// <summary>Runs a custom converter and returns its detailed result.</summary>
    ArgConvertResult CustomResult(string name, string typeName);
    /// <summary>Runs a custom converter and returns its value.</summary>
    object? Custom(string name, string typeName);
    /// <summary>Returns the first matching occurrence.</summary>
    ArgOccurrence? Occurrence(string name);
    /// <summary>Returns all matching occurrences.</summary>
    List<ArgOccurrence> Occurrences(string name);
}
