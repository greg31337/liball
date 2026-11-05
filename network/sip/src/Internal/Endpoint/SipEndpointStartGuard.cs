namespace sip;

internal class SipEndpointStartGuard : System.IDisposable
{
    SipEndpoint endpoint;
    bool completed;

    internal SipEndpointStartGuard(SipEndpoint endpoint)
    {
        this.endpoint = endpoint;
    }

    internal void CompleteEndpointStart()
    {
        completed = true;
    }

    public void Dispose()
    {
        if (!completed)
        {
            endpoint.FailEndpointStart();
        }
    }
}
