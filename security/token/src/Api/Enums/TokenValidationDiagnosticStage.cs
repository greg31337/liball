using System;
namespace token;

/// <summary>Identifies the representation-neutral stage that produced a validation diagnostic.</summary>
public enum TokenValidationDiagnosticStage
{
    /// <summary>The complete representation-specific validator reported the failure.</summary>
    Validation = 0,

    /// <summary>The validation policy was incomplete or contradictory.</summary>
    Policy = 1,

    /// <summary>The token representation could not be safely identified or inspected.</summary>
    Inspection = 2,

    /// <summary>A recognized token representation could not be parsed.</summary>
    Parsing = 3,

    /// <summary>A cryptographic key could not be resolved.</summary>
    KeyResolution = 4,

    /// <summary>A signature, MAC, or authenticated-encryption operation failed.</summary>
    Authentication = 5,

    /// <summary>Authenticated claims failed decoding or policy validation.</summary>
    Claims = 6,

    /// <summary>The recognized token feature is not supported by the universal facade.</summary>
    Unsupported = 7
}
