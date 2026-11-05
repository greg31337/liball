using System.Collections.Generic;

namespace sdp;

/// <summary>
/// Configures bounded semantic validation and diagnostic severity policy.
/// </summary>
public class SdpValidationOptions
{
    /// <summary>Specifies the validation rule set.</summary>
    public SdpValidationProfile Profile = SdpValidationProfile.Rfc8866;

    /// <summary>Identifies whether the validated description is an offer or answer when negotiation-specific rules are required.</summary>
    public SdpNegotiationRole NegotiationRole = SdpNegotiationRole.Unknown;

    /// <summary>Contains the related offer when validating answer-specific continuity rules.</summary>
    public SdpSessionDescription? OfferDescription = null;

    /// <summary>Specifies the lowest severity retained in validation results.</summary>
    public SdpValidationSeverity MinimumSeverity = SdpValidationSeverity.Information;

    /// <summary>Contains diagnostic codes that are explicitly suppressed.</summary>
    public List<string> SuppressedDiagnosticCodes = new List<string>();

    /// <summary>Contains diagnostic codes whose warnings are promoted to errors.</summary>
    public List<string> PromotedDiagnosticCodes = new List<string>();

    /// <summary>Contains the maximum number of issues retained by one validation.</summary>
    public int MaximumDiagnosticCount = SdpLimits.DefaultMaximumDiagnosticCount;
}
