using System.Collections.Generic;

namespace diameter;

/// <summary>
/// Contains the host identity, opaque state, and extension AVPs of a Proxy-Info value.
/// </summary>
public class DiameterProxyInformation
{
    /// <summary>The identity of the proxy that added the state.</summary>
    public DiameterIdentity ProxyHost = new DiameterIdentity();

    /// <summary>The opaque state returned to the proxy.</summary>
    public byte[] ProxyState = new byte[0];

    /// <summary>Additional AVPs carried after the required proxy values.</summary>
    public List<DiameterAvp> ExtensionAvps = new List<DiameterAvp>();
}

/// <summary>
/// Contains a vendor identifier and its vendor-assigned result code.
/// </summary>
public class DiameterExperimentalResult
{
    /// <summary>The vendor responsible for the result-code assignment.</summary>
    public uint VendorId;

    /// <summary>The vendor-assigned result code.</summary>
    public uint ResultCode;
}
