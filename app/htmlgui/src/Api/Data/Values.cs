namespace htmlgui;

/// <summary>Represents a portable, explicitly tagged value used by UI models and messages.</summary>
/// <remarks>Only the field selected by <see cref="UiValue.Kind"/> may contain non-default data.</remarks>
public class UiValue
{
    /// <summary>The representation stored by this value.</summary>
    public UiValueKind Kind;
    /// <summary>The value used when <see cref="UiValue.Kind"/> is <see cref="UiValueKind.Boolean"/>.</summary>
    public bool BooleanValue;
    /// <summary>The value used when <see cref="UiValue.Kind"/> is <see cref="UiValueKind.Integer"/>.</summary>
    public long IntegerValue;
    /// <summary>The value used when <see cref="UiValue.Kind"/> is <see cref="UiValueKind.FloatingPoint"/>.</summary>
    public double FloatingPointValue;
    /// <summary>The value used when <see cref="UiValue.Kind"/> is <see cref="UiValueKind.Decimal"/>.</summary>
    public decimal DecimalValue;
    /// <summary>The value used when <see cref="UiValue.Kind"/> is <see cref="UiValueKind.String"/>.</summary>
    public string? StringValue;
    /// <summary>The invariant text used when <see cref="UiValue.Kind"/> is <see cref="UiValueKind.DateTime"/>.</summary>
    public string? DateTimeValue;
    /// <summary>The bytes used when <see cref="UiValue.Kind"/> is <see cref="UiValueKind.Binary"/>.</summary>
    public byte[]? BinaryValue;
    /// <summary>The ordered items used when <see cref="UiValue.Kind"/> is <see cref="UiValueKind.Array"/>.</summary>
    public UiValue[]? ArrayValue;
    /// <summary>The string-keyed entries used when <see cref="UiValue.Kind"/> is <see cref="UiValueKind.Object"/>.</summary>
    public UiValueEntry[]? ObjectValue;
}

/// <summary>Associates an ordinal string key with a portable UI value.</summary>
public class UiValueEntry
{
    /// <summary>The case-sensitive object key.</summary>
    public string Key = string.Empty;
    /// <summary>The value associated with <see cref="UiValueEntry.Key"/>.</summary>
    public UiValue Value = new UiValue();
}

/// <summary>Describes a public location-aware diagnostic.</summary>
public class UiDiagnostic
{
    /// <summary>The severity.</summary>
    public UiDiagnosticSeverity Severity;
    /// <summary>The stable diagnostic code.</summary>
    public string Code = string.Empty;
    /// <summary>The diagnostic message.</summary>
    public string Message = string.Empty;
    /// <summary>The optional model or protocol location.</summary>
    public string? Location;
}

/// <summary>Describes one named host or browser capability.</summary>
public class UiCapability
{
    /// <summary>The stable capability name.</summary>
    public string Name = string.Empty;
    /// <summary>Whether the capability is supported.</summary>
    public bool IsSupported;
    /// <summary>The optional capability version.</summary>
    public string? Version;
}

/// <summary>Describes an exceptionless portable operation result.</summary>
public class UiResult
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool Success;
    /// <summary>The optional portable result value.</summary>
    public UiValue? Value;
    /// <summary>The ordered diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}
