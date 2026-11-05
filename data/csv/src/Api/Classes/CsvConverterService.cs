using System;

namespace csv;

/// <summary>
/// Provides instance-based CSV value conversion.
/// </summary>
public class CsvConverterService : ICsvConverter
{
    /// <summary>Initializes a conversion service.</summary>
    public CsvConverterService()
    {
    }

    /// <summary>Converts a value to CSV field text.</summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="column">Column metadata.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted field text.</returns>
    public string ToStringValue(object? value, CsvColumn column, CsvConversionOptions options)
    {
        return CsvConverter.ToStringValue(value, column, options);
    }

    /// <summary>Converts field text to a Boolean value.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    public bool ToBoolean(string? value, CsvConversionOptions options)
    {
        return CsvConverter.ToBoolean(value, options);
    }

    /// <summary>Converts field text to a 32-bit integer.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    public int ToInt32(string? value, CsvConversionOptions options)
    {
        return CsvConverter.ToInt32(value, options);
    }

    /// <summary>Converts field text to a 64-bit integer.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    public long ToInt64(string? value, CsvConversionOptions options)
    {
        return CsvConverter.ToInt64(value, options);
    }

    /// <summary>Converts field text to a decimal value.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    public decimal ToDecimal(string? value, CsvConversionOptions options)
    {
        return CsvConverter.ToDecimal(value, options);
    }

    /// <summary>Converts field text to a double-precision value.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    public double ToDouble(string? value, CsvConversionOptions options)
    {
        return CsvConverter.ToDouble(value, options);
    }

    /// <summary>Converts field text to a date and time.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    public DateTime ToDateTime(string? value, CsvConversionOptions options)
    {
        return CsvConverter.ToDateTime(value, options);
    }

    /// <summary>Converts field text to a date.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    public DateOnly ToDateOnly(string? value, CsvConversionOptions options)
    {
        return CsvConverter.ToDateOnly(value, options);
    }

    /// <summary>Converts field text to a time.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    public TimeOnly ToTimeOnly(string? value, CsvConversionOptions options)
    {
        return CsvConverter.ToTimeOnly(value, options);
    }

    /// <summary>Converts field text to a globally unique identifier.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted value.</returns>
    public Guid ToGuid(string? value, CsvConversionOptions options)
    {
        return CsvConverter.ToGuid(value, options);
    }

    /// <summary>Converts hexadecimal field text to bytes.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The converted bytes.</returns>
    public byte[] ToByteArray(string? value, CsvConversionOptions options)
    {
        return CsvConverter.ToByteArray(value, options);
    }

    /// <summary>Converts a row field to text and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    public CsvConversionResult ConvertString(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        return CsvConversionResults.ConvertString(row, fieldIndex, options);
    }

    /// <summary>Converts a row field to a Boolean and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    public CsvConversionResult ConvertBoolean(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        return CsvConversionResults.ConvertBoolean(row, fieldIndex, options);
    }

    /// <summary>Converts a row field to a signed 32-bit integer and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    public CsvConversionResult ConvertInt32(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        return CsvConversionResults.ConvertInt32(row, fieldIndex, options);
    }

    /// <summary>Converts a row field to a signed 64-bit integer and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    public CsvConversionResult ConvertInt64(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        return CsvConversionResults.ConvertInt64(row, fieldIndex, options);
    }

    /// <summary>Converts a row field to a decimal and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    public CsvConversionResult ConvertDecimal(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        return CsvConversionResults.ConvertDecimal(row, fieldIndex, options);
    }

    /// <summary>Converts a row field to a double-precision value and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    public CsvConversionResult ConvertDouble(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        return CsvConversionResults.ConvertDouble(row, fieldIndex, options);
    }

    /// <summary>Converts a row field to a date and time and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    public CsvConversionResult ConvertDateTime(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        return CsvConversionResults.ConvertDateTime(row, fieldIndex, options);
    }

    /// <summary>Converts a row field to a date and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    public CsvConversionResult ConvertDateOnly(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        return CsvConversionResults.ConvertDateOnly(row, fieldIndex, options);
    }

    /// <summary>Converts a row field to a time and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    public CsvConversionResult ConvertTimeOnly(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        return CsvConversionResults.ConvertTimeOnly(row, fieldIndex, options);
    }

    /// <summary>Converts a row field to a globally unique identifier and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    public CsvConversionResult ConvertGuid(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        return CsvConversionResults.ConvertGuid(row, fieldIndex, options);
    }

    /// <summary>Converts a row field to bytes and reports its input state.</summary>
    /// <param name="row">The source row and diagnostic context.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    public CsvConversionResult ConvertByteArray(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        return CsvConversionResults.ConvertByteArray(row, fieldIndex, options);
    }
}
