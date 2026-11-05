namespace fix;

/// <summary>Controls layered FIX message validation.</summary>
public class ValidationOptions
{
    /// <summary>The selected validation layers.</summary>
    public ValidationLayer Layers = ValidationLayer.All;
    /// <summary>The maximum complete message size accepted by validation.</summary>
    public int MaxMessageSize = FixDefaults.MaxMessageSize;
    /// <summary>The maximum field value size accepted by validation.</summary>
    public int MaxFieldSize = FixDefaults.MaxFieldSize;
    /// <summary>The maximum number of flat fields accepted by validation.</summary>
    public int MaxFieldsPerMessage = FixDefaults.MaxFieldsPerMessage;
    /// <summary>The maximum number of issues retained from one validation pass.</summary>
    public int MaxIssues = FixDefaults.MaxDiagnostics;
    /// <summary>The minimum issue severity retained in the validation result.</summary>
    public ValidationSeverity MinimumSeverity = ValidationSeverity.Information;
    /// <summary>Whether empty field values produce validation errors.</summary>
    public bool RejectEmptyValues = true;
    /// <summary>Whether duplicate fields produce validation errors.</summary>
    public bool RejectDuplicateFields = true;
    /// <summary>Whether unknown fields produce validation errors.</summary>
    public bool RejectUnknownFields = true;
    /// <summary>Whether tags at or above the configured user range are accepted.</summary>
    public bool AllowUserDefinedFields;
    /// <summary>Whether standard field and member order is enforced.</summary>
    public bool CheckFieldOrder = true;
    /// <summary>Whether required dictionary members are enforced.</summary>
    public bool CheckRequiredFields = true;
    /// <summary>Whether repeating-group counts, delimiters, and order are enforced.</summary>
    public bool CheckGroups = true;
    /// <summary>Whether conditional presence rules are evaluated.</summary>
    public bool CheckConditionalRules = true;
}
