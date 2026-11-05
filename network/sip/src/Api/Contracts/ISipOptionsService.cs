namespace sip;

/// <summary>Creates OPTIONS requests and answers capability queries.</summary>
public interface ISipOptionsService
{
    /// <summary>Creates an OPTIONS capability query request.</summary>
    /// <param name="target">The capability target.</param>
    /// <param name="cancellation">The cancellation request.</param>
    /// <returns>The query result.</returns>
    SipOptionsResult QueryOptions(SipUri target, SipCancellation cancellation);
    /// <summary>Answers an incoming OPTIONS request.</summary>
    /// <param name="request">The OPTIONS request.</param>
    /// <param name="capabilities">The advertised capabilities.</param>
    /// <returns>The response result.</returns>
    SipOptionsResult AnswerOptions(SipMessage request, SipCapabilitySet capabilities);
}
