using System;

namespace csv;

/// <summary>
/// Converts CSV field text to and from common .NET values.
/// </summary>
public interface ICsvConverter
{
    /// <summary>Converts a value to CSV field text.</summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="column">Column metadata.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted field text.</returns>
    string ToStringValue(object? value, CsvColumn column, CsvConversionOptions options);

    /// <summary>Converts field text to a Boolean value.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    bool ToBoolean(string? value, CsvConversionOptions options);

    /// <summary>Converts field text to a 32-bit integer.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    int ToInt32(string? value, CsvConversionOptions options);

    /// <summary>Converts field text to a 64-bit integer.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    long ToInt64(string? value, CsvConversionOptions options);

    /// <summary>Converts field text to a decimal value.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    decimal ToDecimal(string? value, CsvConversionOptions options);

    /// <summary>Converts field text to a double-precision value.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    double ToDouble(string? value, CsvConversionOptions options);

    /// <summary>Converts field text to a date and time.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    DateTime ToDateTime(string? value, CsvConversionOptions options);

    /// <summary>Converts field text to a date.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    DateOnly ToDateOnly(string? value, CsvConversionOptions options);

    /// <summary>Converts field text to a time.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    TimeOnly ToTimeOnly(string? value, CsvConversionOptions options);

    /// <summary>Converts field text to a globally unique identifier.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    Guid ToGuid(string? value, CsvConversionOptions options);

    /// <summary>Converts hexadecimal field text to bytes.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted bytes.</returns>
    byte[] ToByteArray(string? value, CsvConversionOptions options);

    /// <summary>Converts a row field to text and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    CsvConversionResult ConvertString(CsvRow row, int fieldIndex, CsvConversionOptions options);

    /// <summary>Converts a row field to a Boolean and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    CsvConversionResult ConvertBoolean(CsvRow row, int fieldIndex, CsvConversionOptions options);

    /// <summary>Converts a row field to a signed 32-bit integer and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    CsvConversionResult ConvertInt32(CsvRow row, int fieldIndex, CsvConversionOptions options);

    /// <summary>Converts a row field to a signed 64-bit integer and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    CsvConversionResult ConvertInt64(CsvRow row, int fieldIndex, CsvConversionOptions options);

    /// <summary>Converts a row field to a decimal and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    CsvConversionResult ConvertDecimal(CsvRow row, int fieldIndex, CsvConversionOptions options);

    /// <summary>Converts a row field to a double-precision value and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    CsvConversionResult ConvertDouble(CsvRow row, int fieldIndex, CsvConversionOptions options);

    /// <summary>Converts a row field to a date and time and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    CsvConversionResult ConvertDateTime(CsvRow row, int fieldIndex, CsvConversionOptions options);

    /// <summary>Converts a row field to a date and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    CsvConversionResult ConvertDateOnly(CsvRow row, int fieldIndex, CsvConversionOptions options);

    /// <summary>Converts a row field to a time and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    CsvConversionResult ConvertTimeOnly(CsvRow row, int fieldIndex, CsvConversionOptions options);

    /// <summary>Converts a row field to a globally unique identifier and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    CsvConversionResult ConvertGuid(CsvRow row, int fieldIndex, CsvConversionOptions options);

    /// <summary>Converts a row field to bytes and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    CsvConversionResult ConvertByteArray(CsvRow row, int fieldIndex, CsvConversionOptions options);
}
