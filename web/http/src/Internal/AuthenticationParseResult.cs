using System.Collections.Generic;

namespace http;

internal class AuthenticationParseResult
{
    internal HttpOperationStatus Status;
    internal int Consumed;
    internal int NextPosition;
    internal int ErrorPosition;
    internal int EmptyElements;
    internal HttpAuthenticationValue Value = new HttpAuthenticationValue();
    internal List<HttpAuthenticationValue> Values = new List<HttpAuthenticationValue>();
    internal HttpParameter Parameter = new HttpParameter();
    internal bool IsMalformed;
    internal bool IsLimitExceeded;
    internal bool IsDuplicateParameter;
}
