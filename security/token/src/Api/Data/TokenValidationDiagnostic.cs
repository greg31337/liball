using System;
namespace token;

/// <summary>Describes one failure encountered by universal token validation.</summary>
public class TokenValidationDiagnostic
{
    /// <summary>Identifies the validation stage that produced the failure.</summary>
    public TokenValidationDiagnosticStage Stage;

    /// <summary>Identifies the recognized token representation, or <see cref="TokenKind.Unknown"/>.</summary>
    public TokenKind Kind;

    /// <summary>Identifies a signature or recipient entry, or <c>-1</c> when not applicable.</summary>
    public int ParticipantIndex;

    /// <summary>Contains stable failure details.</summary>
    public TokenError? Error;

    /// <summary>Initializes an empty validation-stage diagnostic.</summary>
    public TokenValidationDiagnostic()
    {
        Stage = TokenValidationDiagnosticStage.Validation;
        Kind = TokenKind.Unknown;
        ParticipantIndex = -1;
        Error = null;
    }
}
