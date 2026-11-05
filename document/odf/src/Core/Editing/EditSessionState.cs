using System.Collections.Generic;

namespace odf;

internal class EditSessionState
{
    internal OdfDocument Document;
    internal string SessionId;
    internal OdfValidationOptions ValidationOptions;
    internal int MaximumOperations;
    internal List<OdfOperation> Operations = new List<OdfOperation>();
    internal bool Finished;
    internal bool Discarded;

    internal EditSessionState(
        OdfDocument document,
        string sessionId,
        OdfValidationOptions validationOptions,
        int maximumOperations)
    {
        Document = document;
        SessionId = sessionId;
        ValidationOptions = validationOptions;
        MaximumOperations = maximumOperations;
    }
}
