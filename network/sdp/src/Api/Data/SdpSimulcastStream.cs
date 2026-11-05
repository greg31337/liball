using System.Collections.Generic;

namespace sdp;

/// <summary>Represents an ordered set of simulcast alternatives.</summary>
public class SdpSimulcastStream
{
    /// <summary>Contains RID alternatives in preference order.</summary>
    public List<SdpSimulcastAlternative> Alternatives = new List<SdpSimulcastAlternative>();
}
