namespace cbor;

/// <summary>Holds the outcome of a custom control evaluation or generation attempt.</summary>
public class CddlControlResult
{
    /// <summary>The custom control outcome.</summary>
    public CddlControlStatus Status = CddlControlStatus.None;
    /// <summary>A value supplied by generation, or null when no value was supplied.</summary>
    public CborValue? Value;
    /// <summary>A stable failure code, or null when no failure occurred.</summary>
    public string? ErrorCode;
    /// <summary>An explanatory failure message, or null when no failure occurred.</summary>
    public string? ErrorMessage;
    /// <summary>The RFC 9165 feature name, or null for a non-feature control result.</summary>
    public string? FeatureName;
    /// <summary>The explicit or inferred RFC 9165 feature detail value.</summary>
    public CborValue? FeatureDetail;
    /// <summary>Additional bounded validation issues.</summary>
    public CddlValidationIssue[] Issues = [];
}
