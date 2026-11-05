namespace sip;

/// <summary>Processes bodies for one custom SIP event package.</summary>
public interface ISipEventPackage
{
    /// <summary>Processes one validated custom-package request.</summary>
    /// <param name="request">The detached event-package request.</param>
    /// <returns>The package processing outcome.</returns>
    SipEventPackageResult ProcessEventBody(SipEventPackageRequest request);
}
