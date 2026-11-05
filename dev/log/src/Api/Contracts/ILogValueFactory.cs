using System;
using System.Collections.Generic;

namespace log;

/// <summary>
/// Defines strongly typed structured value and field creation with configurable capture limits.
/// </summary>
public interface ILogValueFactory
{
    /// <summary>Normalizes an application value with string, collection, and nesting limits.</summary>
    LogValue CreateValueWithLimits(object? value, int maximumStringLength, int maximumCollectionLength, int maximumDepth);
    /// <summary>Creates a null structured value.</summary>
    LogValue CreateNullValue();
    /// <summary>Creates a string structured value.</summary>
    LogValue CreateStringValue(string? value);
    /// <summary>Creates a length-limited string structured value.</summary>
    LogValue CreateStringValueWithLimit(string? value, int maximumStringLength);
    /// <summary>Creates a Boolean structured value.</summary>
    LogValue CreateBoolValue(bool value);
    /// <summary>Creates a 32-bit integer structured value.</summary>
    LogValue CreateIntValue(int value);
    /// <summary>Creates a 64-bit integer structured value.</summary>
    LogValue CreateLongValue(long value);
    /// <summary>Creates a decimal structured value.</summary>
    LogValue CreateDecimalValue(decimal value);
    /// <summary>Creates a floating-point structured value.</summary>
    LogValue CreateDoubleValue(double value);
    /// <summary>Creates a date and time structured value.</summary>
    LogValue CreateDateTimeValue(DateTimeOffset value);
    /// <summary>Creates a duration structured value.</summary>
    LogValue CreateDurationValue(TimeSpan value);
    /// <summary>Creates an exception structured value.</summary>
    LogValue CreateExceptionValue(Exception? value);
    /// <summary>Creates a length-limited exception structured value.</summary>
    LogValue CreateExceptionValueWithLimit(Exception? value, int maximumStringLength);
    /// <summary>Creates an object-text structured value without reflection.</summary>
    LogValue CreateObjectTextValue(object? value);
    /// <summary>Creates a length-limited object-text structured value without reflection.</summary>
    LogValue CreateObjectTextValueWithLimit(object? value, int maximumStringLength);
    /// <summary>Creates an array structured value from application values.</summary>
    LogValue CreateArrayValue(object?[]? values);
    /// <summary>Creates an array structured value with string, collection, and nesting limits.</summary>
    LogValue CreateArrayValueWithLimits(object?[]? values, int maximumStringLength, int maximumCollectionLength, int maximumDepth);
    /// <summary>Creates a dictionary structured value from application values.</summary>
    LogValue CreateDictionaryValue(Dictionary<string, object>? values);
    /// <summary>Creates a dictionary structured value with string, collection, and nesting limits.</summary>
    LogValue CreateDictionaryValueWithLimits(Dictionary<string, object>? values, int maximumStringLength, int maximumCollectionLength, int maximumDepth);
    /// <summary>Creates a validated raw JSON structured value.</summary>
    LogValue CreateRawJsonValue(string? value);
    /// <summary>Creates a validated raw JSON structured value subject to string and nesting limits.</summary>
    LogValue CreateRawJsonValueWithLimits(string? value, int maximumStringLength, int maximumDepth);
    /// <summary>Creates a structured field from an application value with string, collection, and nesting limits.</summary>
    LogField CreateFieldWithLimits(string name, object? value, int maximumStringLength, int maximumCollectionLength, int maximumDepth);
    /// <summary>Creates a null structured field.</summary>
    LogField CreateNullField(string name);
    /// <summary>Creates a string structured field.</summary>
    LogField CreateStringField(string name, string? value);
    /// <summary>Creates a length-limited string structured field.</summary>
    LogField CreateStringFieldWithLimit(string name, string? value, int maximumStringLength);
    /// <summary>Creates a Boolean structured field.</summary>
    LogField CreateBoolField(string name, bool value);
    /// <summary>Creates a 32-bit integer structured field.</summary>
    LogField CreateIntField(string name, int value);
    /// <summary>Creates a 64-bit integer structured field.</summary>
    LogField CreateLongField(string name, long value);
    /// <summary>Creates a decimal structured field.</summary>
    LogField CreateDecimalField(string name, decimal value);
    /// <summary>Creates a floating-point structured field.</summary>
    LogField CreateDoubleField(string name, double value);
    /// <summary>Creates a date and time structured field.</summary>
    LogField CreateDateTimeField(string name, DateTimeOffset value);
    /// <summary>Creates a duration structured field.</summary>
    LogField CreateDurationField(string name, TimeSpan value);
    /// <summary>Creates an exception structured field.</summary>
    LogField CreateExceptionField(string name, Exception? value);
    /// <summary>Creates a length-limited exception structured field.</summary>
    LogField CreateExceptionFieldWithLimit(string name, Exception? value, int maximumStringLength);
    /// <summary>Creates an object-text structured field without reflection.</summary>
    LogField CreateObjectTextField(string name, object? value);
    /// <summary>Creates a length-limited object-text structured field without reflection.</summary>
    LogField CreateObjectTextFieldWithLimit(string name, object? value, int maximumStringLength);
    /// <summary>Creates an array structured field from application values.</summary>
    LogField CreateArrayField(string name, object?[]? values);
    /// <summary>Creates an array structured field with string, collection, and nesting limits.</summary>
    LogField CreateArrayFieldWithLimits(string name, object?[]? values, int maximumStringLength, int maximumCollectionLength, int maximumDepth);
    /// <summary>Creates a dictionary structured field from application values.</summary>
    LogField CreateDictionaryField(string name, Dictionary<string, object>? values);
    /// <summary>Creates a dictionary structured field with string, collection, and nesting limits.</summary>
    LogField CreateDictionaryFieldWithLimits(string name, Dictionary<string, object>? values, int maximumStringLength, int maximumCollectionLength, int maximumDepth);
    /// <summary>Creates a validated raw JSON structured field.</summary>
    LogField CreateRawJsonField(string name, string? value);
    /// <summary>Creates a validated raw JSON structured field subject to string and nesting limits.</summary>
    LogField CreateRawJsonFieldWithLimits(string name, string? value, int maximumStringLength, int maximumDepth);
}
