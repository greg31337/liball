using System;
namespace token;

/// <summary>Contains the outcome of reading one authenticated nested JOSE layer.</summary>
public class NestedJoseResult
{
    /// <summary>Indicates whether the nested layer was read successfully.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Contains the parsed inner JOSE layer, or <c>null</c> on failure.</summary>
    public NestedJoseToken? Token;

    /// <summary>Initializes an unsuccessful nested JOSE result.</summary>
    public NestedJoseResult()
    {
        Success = false;
        Error = null;
        Token = null;
    }
}
