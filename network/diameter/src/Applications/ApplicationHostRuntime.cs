namespace diameter;

internal interface IApplicationDispatchCompletionHandler
{
    void CompleteDiameterApplicationDispatch(
        DiameterApplicationResult result);
}

internal class ApplicationHostDispatchResult
{
    internal long WorkIdentifier;
    internal bool Accepted;
    internal bool Started;
    internal bool Queued;
    internal bool Immediate;
    internal DiameterApplicationResult? Result;
}

internal class ApplicationHostRequestHandler : IDiameterRequestHandler
{
    internal DiameterApplicationHost Host;
    internal ApplicationDispatchPreparation Preparation;

    internal ApplicationHostRequestHandler(
        DiameterApplicationHost host,
        ApplicationDispatchPreparation preparation)
    {
        Host = host;
        Preparation = preparation;
    }

    public DiameterApplicationResult HandleDiameterApplicationRequest(
        DiameterRequestContext context)
    {
        return Host.ProcessDiameterApplicationWork(
            Preparation,
            context);
    }
}

internal class ApplicationHostWorkCompletionHandler :
    IApplicationWorkCompletionHandler
{
    internal DiameterApplicationHost Host;
    internal ApplicationDispatchPreparation Preparation;
    internal IApplicationDispatchCompletionHandler CompletionHandler;

    internal ApplicationHostWorkCompletionHandler(
        DiameterApplicationHost host,
        ApplicationDispatchPreparation preparation,
        IApplicationDispatchCompletionHandler completionHandler)
    {
        Host = host;
        Preparation = preparation;
        CompletionHandler = completionHandler;
    }

    public void CompleteApplicationWork(
        ApplicationWorkCompletion completion)
    {
        Host.CompleteDiameterApplicationWork(
            Preparation,
            completion,
            CompletionHandler);
    }
}
