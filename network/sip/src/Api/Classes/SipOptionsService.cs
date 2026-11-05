namespace sip;

/// <summary>Creates OPTIONS queries and answers capability requests.</summary>
public class SipOptionsService : ISipOptionsService
{
    /// <summary>Initializes an OPTIONS service.</summary>
    public SipOptionsService()
    {
    }

    /// <summary>Creates an OPTIONS capability query request.</summary>
    /// <param name="target">The capability target.</param>
    /// <param name="cancellation">The cancellation request.</param>
    /// <returns>The query result.</returns>
    public SipOptionsResult QueryOptions(
        SipUri target, SipCancellation cancellation)
    {
        return SipOptionsServiceCore.CreateQuery(target, cancellation);
    }

    /// <summary>Answers an incoming OPTIONS request.</summary>
    /// <param name="request">The OPTIONS request.</param>
    /// <param name="capabilities">The advertised capabilities.</param>
    /// <returns>The response result.</returns>
    public SipOptionsResult AnswerOptions(
        SipMessage request, SipCapabilitySet capabilities)
    {
        return SipOptionsServiceCore.AnswerRequest(
            request, capabilities);
    }
}
