using System.Collections.Generic;

namespace sdp;

/// <summary>
/// Contains the result of encoding typed SDP attribute data into generic form.
/// </summary>
public class SdpAttributeEncodeResult
{
    /// <summary>Indicates whether encoding produced a generic attribute.</summary>
    public bool Success = false;

    /// <summary>Contains the supplied typed attribute data for result correlation.</summary>
    public object? TypedAttribute = null;

    /// <summary>Contains the encoded generic attribute when encoding succeeds.</summary>
    public SdpAttribute? Attribute = null;

    /// <summary>Contains retained diagnostics in deterministic order.</summary>
    public List<SdpDiagnostic> Diagnostics = new List<SdpDiagnostic>();
}
