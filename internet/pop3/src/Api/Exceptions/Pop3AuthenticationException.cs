namespace pop3;

/// <summary>Represents a terminal classified POP3 authentication failure.</summary>
public class Pop3AuthenticationException : System.Exception
{
    /// <summary>The authentication method that produced the terminal failure.</summary>
    public Pop3AuthenticationMethod Method;

    /// <summary>The complete classified authentication result.</summary>
    public Pop3AuthenticationResult Result;

    /// <summary>The complete negative response returned by the POP3 peer.</summary>
    public Pop3Response Response;

    /// <summary>Initializes an authentication exception with its method, result, and peer response.</summary>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="method">The authentication method that failed.</param>
    /// <param name="result">The classified authentication result.</param>
    /// <param name="response">The complete negative response returned by the peer.</param>
    public Pop3AuthenticationException(
        string message,
        Pop3AuthenticationMethod method,
        Pop3AuthenticationResult result,
        Pop3Response response) : base(message)
    {
        Method = method;
        Result = result;
        Response = response;
    }
}
