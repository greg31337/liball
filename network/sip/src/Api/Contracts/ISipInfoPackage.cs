namespace sip;

/// <summary>Validates and handles one registered INFO package.</summary>
public interface ISipInfoPackage
{
    /// <summary>Processes one INFO package request.</summary>
    /// <param name="request">The INFO request.</param>
    /// <returns>The handling result.</returns>
    SipInfoResult ProcessInfo(SipInfoRequest request);
}
