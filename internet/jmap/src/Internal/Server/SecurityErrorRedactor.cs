using System;

namespace jmap;

internal class SecurityErrorRedactor
{
    internal string RedactEndpointFailure(AggregateException? failure)
    {
        if (failure != null)
        {
            failure.GetBaseException();
        }

        return "The endpoint could not complete the request.";
    }

    internal string RedactMethodFailure(AggregateException? failure)
    {
        if (failure != null)
        {
            failure.GetBaseException();
        }

        return "The method could not be processed.";
    }
}
