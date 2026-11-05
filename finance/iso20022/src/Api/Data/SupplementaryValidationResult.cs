using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains the exception-less supplementary discovery and validation outcome.</summary>
public class SupplementaryValidationResult
{
    /// <summary>Whether supplementary processing completed under the selected configuration and limits.</summary>
    public bool Success;
    /// <summary>Whether all discovered supplementary content passed applicable policy and schema checks.</summary>
    public bool Valid;
    /// <summary>The aggregate supplementary-data outcome.</summary>
    public SupplementaryValidationStatus Status;
    /// <summary>The discovered supplementary items in document order.</summary>
    public List<SupplementaryDataItem> Items = new List<SupplementaryDataItem>();
    /// <summary>The ordered located validation diagnostics.</summary>
    public List<ValidationDiagnostic> Diagnostics = new List<ValidationDiagnostic>();
    /// <summary>A stable operation error code when supplementary processing could not complete.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable operation error message when supplementary processing could not complete.</summary>
    public string ErrorMessage = string.Empty;
}
