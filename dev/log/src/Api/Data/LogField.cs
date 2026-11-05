using System;
using System.Collections.Generic;

namespace log;

/// <summary>
/// Associates a validated field name with a normalized structured value.
/// </summary>
public class LogField
{
    /// <summary>
    /// The normalized name used to identify this object.
    /// </summary>
    public string Name;
    /// <summary>
    /// The normalized structured value stored by the field.
    /// </summary>
    public LogValue Value;
    /// <summary>
    /// Whether the object passed validation and can be used normally.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// The validation error text, or an empty string for a valid object.
    /// </summary>
    public string ErrorMessage;

    /// <summary>
    /// Creates a field from a name and structured value.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="value">The structured value, or null for a null value.</param>
    public LogField(string name, LogValue? value)
    {
        string validationError = LogField.ValidateName(name);
        if (validationError.Length > 0)
        {
            Name = "";
            Value = LogValue.Null();
            IsValid = false;
            ErrorMessage = validationError;
            return;
        }

        Name = name;
        if (value == null)
        {
            Value = LogValue.Null();
        }
        else
        {
            Value = value;
        }

        IsValid = true;
        ErrorMessage = "";
    }

    /// <summary>
    /// Creates a field from an already normalized structured value.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log field.</returns>
    internal static LogField ValueField(string name, LogValue? value)
    {
        return new LogField(name, value);
    }

    /// <summary>
    /// Creates a structured field containing a string value.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log field.</returns>
    internal static LogField String(string name, string? value)
    {
        return new LogField(name, LogValue.String(value));
    }

    /// <summary>
    /// Creates a structured field containing a bool value.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log field.</returns>
    internal static LogField Bool(string name, bool value)
    {
        return new LogField(name, LogValue.Bool(value));
    }

    /// <summary>
    /// Creates a structured field containing a int value.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log field.</returns>
    internal static LogField Int(string name, int value)
    {
        return new LogField(name, LogValue.Integer(value));
    }

    /// <summary>
    /// Creates a structured field containing a long value.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log field.</returns>
    internal static LogField Long(string name, long value)
    {
        return new LogField(name, LogValue.Integer(value));
    }

    /// <summary>
    /// Creates a structured field containing a decimal value.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log field.</returns>
    internal static LogField Decimal(string name, decimal value)
    {
        return new LogField(name, LogValue.Decimal(value));
    }

    /// <summary>
    /// Creates a structured field containing a double value.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log field.</returns>
    internal static LogField Double(string name, double value)
    {
        return new LogField(name, LogValue.FloatingPoint(value));
    }

    /// <summary>
    /// Creates a structured field containing a date time value.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log field.</returns>
    internal static LogField DateTime(string name, DateTimeOffset value)
    {
        return new LogField(name, LogValue.DateTime(value));
    }

    /// <summary>
    /// Creates a structured field containing a duration value.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log field.</returns>
    internal static LogField Duration(string name, TimeSpan value)
    {
        return new LogField(name, LogValue.Duration(value));
    }

    /// <summary>
    /// Creates a structured field containing a exception value.
    /// </summary>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log field.</returns>
    internal static LogField Exception(Exception? value)
    {
        return new LogField("exception", LogValue.Exception(value));
    }

    /// <summary>
    /// Creates a structured field containing a exception named value.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log field.</returns>
    internal static LogField ExceptionNamed(string name, Exception? value)
    {
        return new LogField(name, LogValue.Exception(value));
    }

    /// <summary>
    /// Creates a structured field containing a object text value.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log field.</returns>
    internal static LogField ObjectText(string name, object? value)
    {
        return new LogField(name, LogValue.ObjectText(value));
    }

    /// <summary>
    /// Creates a structured field containing a array value.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>The resulting log field.</returns>
    internal static LogField Array(string name, LogValue?[]? values)
    {
        return new LogField(name, LogValue.Array(values));
    }

    /// <summary>
    /// Creates a structured field containing a dictionary value.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>The resulting log field.</returns>
    internal static LogField Dictionary(string name, Dictionary<string, LogValue>? values)
    {
        return new LogField(name, LogValue.Dictionary(values));
    }

    /// <summary>
    /// Creates a structured field containing a raw value.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log field.</returns>
    internal static LogField Raw(string name, string? value)
    {
        return new LogField(name, LogValue.Raw(value));
    }

    /// <summary>
    /// Reports whether the supplied name follows the structured-field naming rules.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    internal static bool IsValidName(string name)
    {
        return LogName.IsValidFieldName(name);
    }

    /// <summary>
    /// Returns an empty string for a valid field name or a plain-English validation error.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <returns>The normalized or formatted text.</returns>
    internal static string ValidateName(string name)
    {
        return LogName.ValidateFieldName(name);
    }
}
