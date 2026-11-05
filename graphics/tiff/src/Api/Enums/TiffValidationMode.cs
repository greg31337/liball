namespace tiff;

/// <summary>Specifies whether recoverable TIFF conformance issues are errors or warnings.</summary>
public enum TiffValidationMode
{
    /// <summary>Specifies that recoverable conformance issues are reported as errors.</summary>
    Strict = 0,
    /// <summary>Specifies that recoverable conformance issues are reported as warnings.</summary>
    Permissive = 1
}
