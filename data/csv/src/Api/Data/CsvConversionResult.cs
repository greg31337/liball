using System;

namespace csv;

/// <summary>
/// Holds an exception-less conversion outcome, typed values, and contextual diagnostics.
/// </summary>
public class CsvConversionResult
{
    /// <summary>
    /// The final conversion outcome.
    /// </summary>
    public CsvConversionStatus Status;
    /// <summary>
    /// The original input state before a configured default was considered.
    /// </summary>
    public CsvConversionStatus SourceStatus;
    /// <summary>
    /// The requested target type.
    /// </summary>
    public CsvTypeKind TypeKind;
    /// <summary>
    /// Whether one of the typed value fields contains a converted value.
    /// </summary>
    public bool HasValue;
    /// <summary>
    /// The source text, or the configured default when <see cref="Status"/> is <see cref="CsvConversionStatus.Defaulted"/>.
    /// </summary>
    public string TextValue;
    /// <summary>
    /// The converted Boolean value.
    /// </summary>
    public bool BooleanValue;
    /// <summary>
    /// The converted signed 32-bit integer value.
    /// </summary>
    public int Int32Value;
    /// <summary>
    /// The converted signed 64-bit integer value.
    /// </summary>
    public long Int64Value;
    /// <summary>
    /// The converted decimal value.
    /// </summary>
    public decimal DecimalValue;
    /// <summary>
    /// The converted double-precision value.
    /// </summary>
    public double DoubleValue;
    /// <summary>
    /// The converted date and time value.
    /// </summary>
    public DateTime DateTimeValue;
    /// <summary>
    /// The converted date value.
    /// </summary>
    public DateOnly DateOnlyValue;
    /// <summary>
    /// The converted time value.
    /// </summary>
    public TimeOnly TimeOnlyValue;
    /// <summary>
    /// The converted globally unique identifier value.
    /// </summary>
    public Guid GuidValue;
    /// <summary>
    /// The converted byte array value.
    /// </summary>
    public byte[] ByteArrayValue;
    /// <summary>
    /// Diagnostics describing non-success or defaulted outcomes.
    /// </summary>
    public CsvDiagnosticList Diagnostics;

    /// <summary>
    /// Initializes an empty conversion result.
    /// </summary>
    public CsvConversionResult()
    {
        Status = CsvConversionStatus.Invalid;
        SourceStatus = CsvConversionStatus.Invalid;
        TypeKind = CsvTypeKind.Unknown;
        HasValue = false;
        TextValue = "";
        BooleanValue = false;
        Int32Value = 0;
        Int64Value = 0;
        DecimalValue = 0m;
        DoubleValue = 0d;
        DateTimeValue = default(DateTime);
        DateOnlyValue = default(DateOnly);
        TimeOnlyValue = default(TimeOnly);
        GuidValue = Guid.Empty;
        ByteArrayValue = new byte[0];
        Diagnostics = new CsvDiagnosticList();
    }
}
