using System.Globalization;

namespace csv;

/// <summary>
/// Controls null recognition and conversion of CSV text to built-in .NET values.
/// </summary>
public class CsvConversionOptions
{
    /// <summary>
    /// The culture used for numeric and date conversion.
    /// </summary>
    public CultureInfo Culture;
    /// <summary>
    /// Exact date or time formats tried before built-in formats.
    /// </summary>
    public string[] DateTimeFormats;
    /// <summary>
    /// The decimal separator recognized in numeric text.
    /// </summary>
    public string DecimalSeparator;
    /// <summary>
    /// The grouping separator removed from numeric text.
    /// </summary>
    public string ThousandsSeparator;
    /// <summary>
    /// The text token representing a null field.
    /// </summary>
    public string NullString;
    /// <summary>
    /// Whether null-token matching is active.
    /// </summary>
    public bool HasNullString;
    /// <summary>
    /// Whether empty text is treated as null during conversion.
    /// </summary>
    public bool EmptyStringIsNull;
    /// <summary>
    /// Fallback text used for null or invalid input.
    /// </summary>
    public string DefaultValue;
    /// <summary>
    /// Whether null input uses the configured fallback.
    /// </summary>
    public bool UseDefaultForNull;
    /// <summary>
    /// Whether invalid input uses the configured fallback.
    /// </summary>
    public bool UseDefaultForInvalid;

    /// <summary>
    /// Initializes a new CsvConversionOptions with library defaults.
    /// </summary>
    public CsvConversionOptions()
    {
        Culture = CultureInfo.InvariantCulture;
        DateTimeFormats = new string[0];
        DecimalSeparator = ".";
        ThousandsSeparator = ",";
        NullString = "";
        HasNullString = false;
        EmptyStringIsNull = false;
        DefaultValue = "";
        UseDefaultForNull = false;
        UseDefaultForInvalid = false;
    }

    /// <summary>
    /// Creates an instance populated with library defaults.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvConversionOptions CreateDefault()
    {
        CsvConversionOptions options = new CsvConversionOptions();
        return options;
    }

    /// <summary>
    /// Creates an independent copy so later changes do not affect the original.
    /// </summary>
    /// <returns>An independent copy.</returns>
    internal CsvConversionOptions Clone()
    {
        CsvConversionOptions options = new CsvConversionOptions();
        options.Culture = Culture;
        options.DateTimeFormats = new string[DateTimeFormats.Length];
        int index = 0;
        while (index < DateTimeFormats.Length)
        {
            options.DateTimeFormats[index] = DateTimeFormats[index];
            index = index + 1;
        }
        options.DecimalSeparator = DecimalSeparator;
        options.ThousandsSeparator = ThousandsSeparator;
        options.NullString = NullString;
        options.HasNullString = HasNullString;
        options.EmptyStringIsNull = EmptyStringIsNull;
        options.DefaultValue = DefaultValue;
        options.UseDefaultForNull = UseDefaultForNull;
        options.UseDefaultForInvalid = UseDefaultForInvalid;
        return options;
    }

    /// <summary>
    /// Copies the culture decimal and grouping separators into the explicit separator fields.
    /// </summary>
    internal void UseCultureSeparators()
    {
        DecimalSeparator = Culture.NumberFormat.NumberDecimalSeparator;
        ThousandsSeparator = Culture.NumberFormat.NumberGroupSeparator;
    }

    internal bool HasDateTimeFormats()
    {
        if (DateTimeFormats.Length > 0)
        {
            return true;
        }

        return false;
    }

    internal bool IsNullValue(string? value)
    {
        if (value == null)
        {
            return true;
        }

        if (EmptyStringIsNull && value.Length == 0)
        {
            return true;
        }

        if (HasNullString && value == NullString)
        {
            return true;
        }

        return false;
    }

    internal string? ResolveNullValue(string? value)
    {
        if (IsNullValue(value) && UseDefaultForNull)
        {
            return DefaultValue;
        }

        return value;
    }

    internal string ResolveInvalidValue(string value)
    {
        if (UseDefaultForInvalid)
        {
            return DefaultValue;
        }

        return value;
    }

    internal string NormalizeNumberString(string value)
    {
        string normalized = value;
        if (ThousandsSeparator.Length > 0)
        {
            normalized = normalized.Replace(ThousandsSeparator, "");
        }

        if (DecimalSeparator.Length > 0 && DecimalSeparator != ".")
        {
            normalized = normalized.Replace(DecimalSeparator, ".");
        }

        return normalized;
    }
}
