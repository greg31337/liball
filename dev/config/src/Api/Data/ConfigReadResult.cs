using System;

namespace config;

/// <summary>Contains the outcome, diagnostic, and converted value from an exception-less typed read.</summary>
/// <remarks>Inspect <see cref="Status"/> before reading the field associated with the requested operation.</remarks>
public class ConfigReadResult
{
    /// <summary>Identifies whether conversion succeeded or why it did not.</summary>
    public ConfigReadStatus Status;
    /// <summary>Describes a failed read with source location when available, or is empty after success.</summary>
    public ConfigError Diagnostic;
    /// <summary>Contains the result of a string read.</summary>
    public string StringValue;
    /// <summary>Contains the result of a Boolean read.</summary>
    public bool BooleanValue;
    /// <summary>Contains the result of a 32-bit integer read.</summary>
    public int IntValue;
    /// <summary>Contains the result of a 64-bit integer read.</summary>
    public long LongValue;
    /// <summary>Contains the result of a decimal read.</summary>
    public decimal DecimalValue;
    /// <summary>Contains the result of a double-precision read.</summary>
    public double DoubleValue;
    /// <summary>Contains the result of a duration read.</summary>
    public TimeSpan TimeSpanValue;
    /// <summary>Contains the result of a byte-size read.</summary>
    public long ByteSizeValue;
    /// <summary>Contains the result of a string-array read.</summary>
    public string[] StringArrayValue;
    /// <summary>Contains the result of a 32-bit integer-array read.</summary>
    public int[] IntArrayValue;
    /// <summary>Contains the result of a 64-bit integer-array read.</summary>
    public long[] LongArrayValue;
    /// <summary>Contains the result of a decimal-array read.</summary>
    public decimal[] DecimalArrayValue;
    /// <summary>Contains the result of a double-precision-array read.</summary>
    public double[] DoubleArrayValue;
    /// <summary>Contains the boxed result of an enum read, or null when the read failed.</summary>
    public object? EnumValue;

    internal ConfigReadResult()
    {
        Status = ConfigReadStatus.Success;
        Diagnostic = new ConfigError("", "", "", 0, 0);
        StringValue = "";
        BooleanValue = false;
        IntValue = 0;
        LongValue = 0L;
        DecimalValue = 0m;
        DoubleValue = 0d;
        TimeSpanValue = TimeSpan.Zero;
        ByteSizeValue = 0L;
        StringArrayValue = new string[0];
        IntArrayValue = new int[0];
        LongArrayValue = new long[0];
        DecimalArrayValue = new decimal[0];
        DoubleArrayValue = new double[0];
        EnumValue = null;
    }
}
