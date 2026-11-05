using System;

namespace sdp;

/// <summary>Validates SDP descriptions against supported semantic profiles.</summary>
public class SdpValidator : ISdpValidator
{
    /// <summary>Initializes an SDP validator.</summary>
    public SdpValidator()
    {
    }

    /// <summary>Validates an SDP description and returns bounded structured issues.</summary>
    /// <param name="description">The SDP description to validate.</param>
    /// <param name="options">The validation options to apply.</param>
    /// <returns>A bounded validation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or options argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the validation options contain an unsupported value.</exception>
    public SdpValidationResult ValidateDescription(SdpSessionDescription description, SdpValidationOptions options)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
        return SdpValidationCore.ValidateDescription(description, options);
    }
}
