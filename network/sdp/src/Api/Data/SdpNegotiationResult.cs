using System.Collections.Generic;

namespace sdp;

/// <summary>Contains the bounded result of an offer-and-answer operation.</summary>
public class SdpNegotiationResult
{
    /// <summary>Indicates whether negotiation completed successfully.</summary>
    public bool Success = false;
    /// <summary>Contains the generated offer or answer when available.</summary>
    public SdpSessionDescription? Description = null;
    /// <summary>Contains the options applied to negotiation.</summary>
    public SdpNegotiationOptions AppliedOptions = new SdpNegotiationOptions();
    /// <summary>Contains per-media outcomes in media order.</summary>
    public List<SdpMediaNegotiation> Media = new List<SdpMediaNegotiation>();
    /// <summary>Contains rejected media outcomes in media order.</summary>
    public List<SdpRejection> Rejections = new List<SdpRejection>();
    /// <summary>Contains negotiated BUNDLE groups.</summary>
    public List<SdpBundleGroup> BundleGroups = new List<SdpBundleGroup>();
    /// <summary>Contains resolved BUNDLE transports in group order.</summary>
    public List<SdpBundleTransport> BundleTransports = new List<SdpBundleTransport>();
    /// <summary>Contains retained diagnostics in deterministic order.</summary>
    public List<SdpDiagnostic> Diagnostics = new List<SdpDiagnostic>();
    /// <summary>Indicates that the configured diagnostic capacity was exhausted.</summary>
    public bool DiagnosticLimitReached = false;
}
