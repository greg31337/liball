namespace sdp;

/// <summary>Validates SDP descriptions against a selected semantic profile.</summary>
public interface ISdpValidator
{
    /// <summary>Validates an SDP description and returns bounded structured issues.</summary>
    /// <param name="description">The SDP description to validate.</param>
    /// <param name="options">The validation options to apply.</param>
    /// <returns>A bounded validation result.</returns>
    SdpValidationResult ValidateDescription(SdpSessionDescription description, SdpValidationOptions options);
}
