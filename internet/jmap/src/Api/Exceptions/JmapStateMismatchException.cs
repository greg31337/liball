using System.Text.Json;

namespace jmap;

/// <summary>
/// Represents a failed state precondition for a JMAP method call.
/// </summary>
public class JmapStateMismatchException : System.Exception
{
    /// <summary>The method name associated with the failed state precondition.</summary>
    public string? MethodName;

    /// <summary>The method call identifier associated with the failed state precondition.</summary>
    public string? CallId;

    /// <summary>The account identifier associated with the failed state precondition.</summary>
    public string? AccountId;

    /// <summary>The state value supplied by the caller.</summary>
    public string? ExpectedState;

    /// <summary>The current state value reported by the server, or null when unavailable.</summary>
    public string? ActualState;

    /// <summary>The decoded stateMismatch method error, or null when unavailable.</summary>
    public JmapMethodError? Error;

    /// <summary>The complete structured method error data returned by the server.</summary>
    public JsonElement StructuredData;

    /// <summary>Whether retrying after state reconciliation can be considered by the caller.</summary>
    public bool IsRetryable;

    /// <summary>Creates a state mismatch exception with call, state, error, and retry context.</summary>
    /// <param name="message">The diagnostic failure message.</param>
    /// <param name="methodName">The method name associated with the failure.</param>
    /// <param name="callId">The method call identifier associated with the failure.</param>
    /// <param name="accountId">The account identifier associated with the failure.</param>
    /// <param name="expectedState">The state value supplied by the caller.</param>
    /// <param name="actualState">The current state value reported by the server, or null.</param>
    /// <param name="error">The decoded stateMismatch method error, or null.</param>
    /// <param name="data">The complete structured method error data.</param>
    /// <param name="isRetryable">Whether retrying after reconciliation can be considered by the caller.</param>
    public JmapStateMismatchException(string message, string? methodName, string? callId, string? accountId, string? expectedState, string? actualState, JmapMethodError? error, JsonElement data, bool isRetryable) : base(message)
    {
        MethodName = methodName;
        CallId = callId;
        AccountId = accountId;
        ExpectedState = expectedState;
        ActualState = actualState;
        Error = error;
        StructuredData = data;
        IsRetryable = isRetryable;
    }
}
