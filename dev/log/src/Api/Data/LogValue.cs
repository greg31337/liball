using System;
using System.Collections.Generic;

namespace log;

/// <summary>
/// Stores one normalized structured value together with capture-limit metadata.
/// </summary>
/// <remarks>
/// Inspect Kind before reading TextValue, IntegerValue, ArrayValue, or another kind-specific field. Collection and text factories copy or truncate inputs to bounded sizes.
/// </remarks>
public class LogValue
{
    internal const int DefaultMaximumStringLength = 4096;
    internal const int DefaultMaximumCollectionLength = 64;
    internal const int DefaultMaximumDepth = 8;

    /// <summary>
    /// The kind that determines which value field is active.
    /// </summary>
    public LogValueKind Kind;
    /// <summary>
    /// The normalized text for string, exception, or object-text values.
    /// </summary>
    public string TextValue;
    /// <summary>
    /// The Boolean payload when Kind is Bool.
    /// </summary>
    public bool BoolValue;
    /// <summary>
    /// The signed 64-bit payload when Kind is Integer.
    /// </summary>
    public long IntegerValue;
    /// <summary>
    /// The decimal payload when Kind is Decimal.
    /// </summary>
    public decimal DecimalValue;
    /// <summary>
    /// The double-precision payload when Kind is FloatingPoint.
    /// </summary>
    public double FloatingPointValue;
    /// <summary>
    /// The timestamp payload when Kind is DateTime.
    /// </summary>
    public DateTimeOffset DateTimeValue;
    /// <summary>
    /// The time-span payload when Kind is Duration.
    /// </summary>
    public TimeSpan DurationValue;
    /// <summary>
    /// The exception attached to the event, or null when none was supplied.
    /// </summary>
    public Exception? ExceptionValue;
    /// <summary>
    /// A copied array payload when Kind is Array.
    /// </summary>
    public LogValue[] ArrayValue;
    /// <summary>
    /// A copied string-keyed payload when Kind is Dictionary.
    /// </summary>
    public Dictionary<string, LogValue> DictionaryValue;
    /// <summary>
    /// The unquoted JSON-compatible text when Kind is Raw.
    /// </summary>
    public string RawValue;
    /// <summary>
    /// Whether capture limits removed part of the original value.
    /// </summary>
    public bool IsTruncated;
    /// <summary>
    /// The input length before capture limits were applied.
    /// </summary>
    public int OriginalLength;
    /// <summary>
    /// The stored length after capture limits were applied.
    /// </summary>
    public int CapturedLength;
    /// <summary>
    /// The capture limit used for this value.
    /// </summary>
    public int MaximumLength;
    /// <summary>
    /// The deepest collection level retained while normalizing the value.
    /// </summary>
    public int CapturedDepth;

    /// <summary>
    /// Creates a structured value from its complete field representation.
    /// </summary>
    /// <param name="kind">The value category.</param>
    /// <param name="textValue">The text representation.</param>
    /// <param name="boolValue">The Boolean representation.</param>
    /// <param name="integerValue">The integer representation.</param>
    /// <param name="decimalValue">The decimal representation.</param>
    /// <param name="floatingPointValue">The floating-point representation.</param>
    /// <param name="dateTimeValue">The date and time representation.</param>
    /// <param name="durationValue">The duration representation.</param>
    /// <param name="exceptionValue">The exception representation.</param>
    /// <param name="arrayValue">The array representation.</param>
    /// <param name="dictionaryValue">The dictionary representation.</param>
    /// <param name="rawValue">The raw representation.</param>
    public LogValue(LogValueKind kind, string textValue, bool boolValue, long integerValue, decimal decimalValue, double floatingPointValue, DateTimeOffset dateTimeValue, TimeSpan durationValue, Exception? exceptionValue, LogValue[] arrayValue, Dictionary<string, LogValue> dictionaryValue, string rawValue)
    {
        string normalizedText = LogResult.NormalizeText(textValue);
        string normalizedRaw = LogResult.NormalizeText(rawValue);
        Kind = kind;
        TextValue = LogValue.TruncateText(normalizedText, LogValue.DefaultMaximumStringLength);
        BoolValue = boolValue;
        IntegerValue = integerValue;
        DecimalValue = decimalValue;
        FloatingPointValue = floatingPointValue;
        DateTimeValue = dateTimeValue;
        DurationValue = durationValue;
        ExceptionValue = exceptionValue;
        ArrayValue = LogValue.CopyArray(arrayValue);
        DictionaryValue = LogValue.CopyDictionary(dictionaryValue);
        RawValue = LogValue.TruncateText(normalizedRaw, LogValue.DefaultMaximumStringLength);
        IsTruncated = TextValue.Length < normalizedText.Length || RawValue.Length < normalizedRaw.Length;
        OriginalLength = LogValue.SelectOriginalLength(kind, normalizedText.Length, normalizedRaw.Length, arrayValue, dictionaryValue);
        CapturedLength = LogValue.SelectCapturedLength(kind, TextValue.Length, RawValue.Length, ArrayValue, DictionaryValue);
        MaximumLength = LogValue.SelectMaximumLength(kind);
        CapturedDepth = 0;
    }

    /// <summary>
    /// Creates a normalized null structured value.
    /// </summary>
    /// <returns>The resulting log value.</returns>
    internal static LogValue Null()
    {
        return new LogValue(LogValueKind.Null, "", false, 0, 0m, 0d, DateTimeOffset.MinValue, TimeSpan.Zero, null, new LogValue[0], new Dictionary<string, LogValue>(), "");
    }

    /// <summary>
    /// Creates a normalized string structured value.
    /// </summary>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue String(string? value)
    {
        return LogValue.StringLimit(value, LogValue.DefaultMaximumStringLength);
    }

    /// <summary>
    /// Creates a normalized string structured value using the supplied capture limit.
    /// </summary>
    /// <param name="value">The value to normalize or compare.</param>
    /// <param name="maximumLength">The maximum text or collection length to capture.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue StringLimit(string? value, int maximumLength)
    {
        string normalized = LogResult.NormalizeText(value);
        int limit = LogValue.NormalizeMaximumLength(maximumLength);
        string captured = LogValue.TruncateText(normalized, limit);
        LogValue result = new LogValue(LogValueKind.String, captured, false, 0, 0m, 0d, DateTimeOffset.MinValue, TimeSpan.Zero, null, new LogValue[0], new Dictionary<string, LogValue>(), "");
        result.SetLimitMetadata(captured.Length < normalized.Length, normalized.Length, captured.Length, limit, 0);
        return result;
    }

    /// <summary>
    /// Creates a normalized bool structured value.
    /// </summary>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue Bool(bool value)
    {
        return new LogValue(LogValueKind.Bool, "", value, 0, 0m, 0d, DateTimeOffset.MinValue, TimeSpan.Zero, null, new LogValue[0], new Dictionary<string, LogValue>(), "");
    }

    /// <summary>
    /// Creates a normalized integer structured value.
    /// </summary>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue Integer(long value)
    {
        return new LogValue(LogValueKind.Integer, "", false, value, 0m, 0d, DateTimeOffset.MinValue, TimeSpan.Zero, null, new LogValue[0], new Dictionary<string, LogValue>(), "");
    }

    /// <summary>
    /// Creates a normalized decimal structured value.
    /// </summary>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue Decimal(decimal value)
    {
        return new LogValue(LogValueKind.Decimal, "", false, 0, value, 0d, DateTimeOffset.MinValue, TimeSpan.Zero, null, new LogValue[0], new Dictionary<string, LogValue>(), "");
    }

    /// <summary>
    /// Creates a normalized floating point structured value.
    /// </summary>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue FloatingPoint(double value)
    {
        return new LogValue(LogValueKind.FloatingPoint, "", false, 0, 0m, value, DateTimeOffset.MinValue, TimeSpan.Zero, null, new LogValue[0], new Dictionary<string, LogValue>(), "");
    }

    /// <summary>
    /// Creates a normalized date time structured value.
    /// </summary>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue DateTime(DateTimeOffset value)
    {
        return new LogValue(LogValueKind.DateTime, "", false, 0, 0m, 0d, value, TimeSpan.Zero, null, new LogValue[0], new Dictionary<string, LogValue>(), "");
    }

    /// <summary>
    /// Creates a normalized duration structured value.
    /// </summary>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue Duration(TimeSpan value)
    {
        return new LogValue(LogValueKind.Duration, "", false, 0, 0m, 0d, DateTimeOffset.MinValue, value, null, new LogValue[0], new Dictionary<string, LogValue>(), "");
    }

    /// <summary>
    /// Creates a normalized exception structured value.
    /// </summary>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue Exception(Exception? value)
    {
        if (value == null)
        {
            return LogValue.Null();
        }

        return LogValue.ExceptionLimit(value, LogValue.DefaultMaximumStringLength);
    }

    /// <summary>
    /// Creates a normalized exception structured value using the supplied capture limit.
    /// </summary>
    /// <param name="value">The value to normalize or compare.</param>
    /// <param name="maximumLength">The maximum text or collection length to capture.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue ExceptionLimit(Exception? value, int maximumLength)
    {
        if (value == null)
        {
            return LogValue.Null();
        }

        string normalized = LogResult.NormalizeText(value.ToString());
        int limit = LogValue.NormalizeMaximumLength(maximumLength);
        string captured = LogValue.TruncateText(normalized, limit);
        LogValue result = new LogValue(LogValueKind.Exception, captured, false, 0, 0m, 0d, DateTimeOffset.MinValue, TimeSpan.Zero, value, new LogValue[0], new Dictionary<string, LogValue>(), "");
        result.SetLimitMetadata(captured.Length < normalized.Length, normalized.Length, captured.Length, limit, 0);
        return result;
    }

    /// <summary>
    /// Creates a normalized object text structured value.
    /// </summary>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue ObjectText(object? value)
    {
        if (value == null)
        {
            return LogValue.Null();
        }

        return LogValue.ObjectTextLimit(value, LogValue.DefaultMaximumStringLength);
    }

    /// <summary>
    /// Creates a normalized object text structured value using the supplied capture limit.
    /// </summary>
    /// <param name="value">The value to normalize or compare.</param>
    /// <param name="maximumLength">The maximum text or collection length to capture.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue ObjectTextLimit(object? value, int maximumLength)
    {
        if (value == null)
        {
            return LogValue.Null();
        }

        string? text = value.ToString();
        if (text == null)
        {
            text = "";
        }

        int limit = LogValue.NormalizeMaximumLength(maximumLength);
        string captured = LogValue.TruncateText(text, limit);
        LogValue result = new LogValue(LogValueKind.ObjectText, captured, false, 0, 0m, 0d, DateTimeOffset.MinValue, TimeSpan.Zero, null, new LogValue[0], new Dictionary<string, LogValue>(), "");
        result.SetLimitMetadata(captured.Length < text.Length, text.Length, captured.Length, limit, 0);
        return result;
    }

    /// <summary>
    /// Creates a normalized array structured value.
    /// </summary>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue Array(LogValue?[]? values)
    {
        if (values == null)
        {
            return LogValue.Null();
        }

        return LogValue.ArrayLimit(values, LogValue.DefaultMaximumCollectionLength);
    }

    /// <summary>
    /// Creates a normalized array structured value using the supplied capture limit.
    /// </summary>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <param name="maximumLength">The maximum text or collection length to capture.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue ArrayLimit(LogValue?[]? values, int maximumLength)
    {
        if (values == null)
        {
            return LogValue.Null();
        }

        int limit = LogValue.NormalizeMaximumLength(maximumLength);
        LogValue[] captured = LogValue.CopyArrayLimit(values, limit);
        LogValue result = new LogValue(LogValueKind.Array, "", false, 0, 0m, 0d, DateTimeOffset.MinValue, TimeSpan.Zero, null, captured, new Dictionary<string, LogValue>(), "");
        result.SetLimitMetadata(captured.Length < values.Length, values.Length, captured.Length, limit, 0);
        return result;
    }

    /// <summary>
    /// Creates a normalized dictionary structured value.
    /// </summary>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue Dictionary(Dictionary<string, LogValue>? values)
    {
        if (values == null)
        {
            return LogValue.Null();
        }

        return LogValue.DictionaryLimit(values, LogValue.DefaultMaximumCollectionLength);
    }

    /// <summary>
    /// Creates a normalized dictionary structured value using the supplied capture limit.
    /// </summary>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <param name="maximumLength">The maximum text or collection length to capture.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue DictionaryLimit(Dictionary<string, LogValue>? values, int maximumLength)
    {
        if (values == null)
        {
            return LogValue.Null();
        }

        int limit = LogValue.NormalizeMaximumLength(maximumLength);
        Dictionary<string, LogValue> captured = LogValue.CopyDictionaryLimit(values, limit);
        LogValue result = new LogValue(LogValueKind.Dictionary, "", false, 0, 0m, 0d, DateTimeOffset.MinValue, TimeSpan.Zero, null, new LogValue[0], captured, "");
        result.SetLimitMetadata(captured.Count < values.Count, values.Count, captured.Count, limit, 0);
        return result;
    }

    /// <summary>
    /// Creates a normalized raw structured value.
    /// </summary>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue Raw(string? value)
    {
        return LogValue.RawLimit(value, LogValue.DefaultMaximumStringLength);
    }

    /// <summary>
    /// Creates a normalized raw structured value using the supplied capture limit.
    /// </summary>
    /// <param name="value">The value to normalize or compare.</param>
    /// <param name="maximumLength">The maximum text or collection length to capture.</param>
    /// <returns>The resulting log value.</returns>
    internal static LogValue RawLimit(string? value, int maximumLength)
    {
        string normalized = LogResult.NormalizeText(value);
        int limit = LogValue.NormalizeMaximumLength(maximumLength);
        string captured = LogValue.TruncateText(normalized, limit);
        LogValue result = new LogValue(LogValueKind.Raw, "", false, 0, 0m, 0d, DateTimeOffset.MinValue, TimeSpan.Zero, null, new LogValue[0], new Dictionary<string, LogValue>(), captured);
        result.SetLimitMetadata(captured.Length < normalized.Length, normalized.Length, captured.Length, limit, 0);
        return result;
    }

    internal static LogValue[] CopyArray(LogValue?[]? values)
    {
        return LogValue.CopyArrayLimit(values, LogValue.DefaultMaximumCollectionLength);
    }

    internal static LogValue[] CopyArrayLimit(LogValue?[]? values, int maximumLength)
    {
        if (values == null)
        {
            return new LogValue[0];
        }

        int limit = LogValue.NormalizeMaximumLength(maximumLength);
        int length = values.Length;
        if (length > limit)
        {
            length = limit;
        }

        LogValue[] copy = new LogValue[length];
        int index = 0;
        while (index < length)
        {
            LogValue? value = values[index];
            if (value == null)
            {
                copy[index] = LogValue.Null();
            }
            else
            {
                copy[index] = value;
            }

            index = index + 1;
        }

        return copy;
    }

    internal static Dictionary<string, LogValue> CopyDictionary(Dictionary<string, LogValue>? values)
    {
        return LogValue.CopyDictionaryLimit(values, LogValue.DefaultMaximumCollectionLength);
    }

    internal static Dictionary<string, LogValue> CopyDictionaryLimit(Dictionary<string, LogValue>? values, int maximumLength)
    {
        Dictionary<string, LogValue> copy = new Dictionary<string, LogValue>();

        if (values == null)
        {
            return copy;
        }

        int limit = LogValue.NormalizeMaximumLength(maximumLength);
        int count = 0;
        foreach (KeyValuePair<string, LogValue> entry in values)
        {
            if (count >= limit)
            {
                break;
            }

            if (entry.Key == null)
            {
                continue;
            }

            if (entry.Value == null)
            {
                copy[entry.Key] = LogValue.Null();
            }
            else
            {
                copy[entry.Key] = entry.Value;
            }

            count = count + 1;
        }

        return copy;
    }

    internal void SetLimitMetadata(bool isTruncated, int originalLength, int capturedLength, int maximumLength, int capturedDepth)
    {
        IsTruncated = isTruncated;
        OriginalLength = LogEvent.NormalizeNonNegative(originalLength);
        CapturedLength = LogEvent.NormalizeNonNegative(capturedLength);
        MaximumLength = LogValue.NormalizeMaximumLength(maximumLength);
        CapturedDepth = LogEvent.NormalizeNonNegative(capturedDepth);
    }

    internal static string TruncateText(string? value, int maximumLength)
    {
        string normalized = LogResult.NormalizeText(value);
        int limit = LogValue.NormalizeMaximumLength(maximumLength);
        if (normalized.Length <= limit)
        {
            return normalized;
        }

        return normalized.Substring(0, limit);
    }

    internal static int NormalizeMaximumLength(int maximumLength)
    {
        if (maximumLength < 0)
        {
            return 0;
        }

        return maximumLength;
    }

    internal static int SelectOriginalLength(LogValueKind kind, int textLength, int rawLength, LogValue[] arrayValue, Dictionary<string, LogValue> dictionaryValue)
    {
        if (kind == LogValueKind.String || kind == LogValueKind.Exception || kind == LogValueKind.ObjectText)
        {
            return textLength;
        }

        if (kind == LogValueKind.Raw)
        {
            return rawLength;
        }

        if (kind == LogValueKind.Array && arrayValue != null)
        {
            return arrayValue.Length;
        }

        if (kind == LogValueKind.Dictionary && dictionaryValue != null)
        {
            return dictionaryValue.Count;
        }

        return 0;
    }

    internal static int SelectCapturedLength(LogValueKind kind, int textLength, int rawLength, LogValue[] arrayValue, Dictionary<string, LogValue> dictionaryValue)
    {
        if (kind == LogValueKind.String || kind == LogValueKind.Exception || kind == LogValueKind.ObjectText)
        {
            return textLength;
        }

        if (kind == LogValueKind.Raw)
        {
            return rawLength;
        }

        if (kind == LogValueKind.Array && arrayValue != null)
        {
            return arrayValue.Length;
        }

        if (kind == LogValueKind.Dictionary && dictionaryValue != null)
        {
            return dictionaryValue.Count;
        }

        return 0;
    }

    internal static int SelectMaximumLength(LogValueKind kind)
    {
        if (kind == LogValueKind.String || kind == LogValueKind.Exception || kind == LogValueKind.ObjectText || kind == LogValueKind.Raw)
        {
            return LogValue.DefaultMaximumStringLength;
        }

        if (kind == LogValueKind.Array || kind == LogValueKind.Dictionary)
        {
            return LogValue.DefaultMaximumCollectionLength;
        }

        return 0;
    }
}
