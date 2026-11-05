using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes one normalized ISO 20022 external code set.</summary>
public class ExternalCodeSet
{
    /// <summary>The stable code-set identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The published code-set name.</summary>
    public string Name = string.Empty;
    /// <summary>The published semantic definition.</summary>
    public string Definition = string.Empty;
    /// <summary>The exact published registration-status value.</summary>
    public string RegistrationStatus = string.Empty;
    /// <summary>The code-set publication date when present.</summary>
    public IsoDateValue? PublicationDate;
    /// <summary>The date on which the code set becomes active when declared.</summary>
    public IsoDateValue? ActivationDate;
    /// <summary>The date on which the code set ceases to be active when declared.</summary>
    public IsoDateValue? DeactivationDate;
    /// <summary>Whether a minimum code length was declared.</summary>
    public bool HasMinimumLength;
    /// <summary>The declared minimum code length.</summary>
    public int MinimumLength;
    /// <summary>Whether a maximum code length was declared.</summary>
    public bool HasMaximumLength;
    /// <summary>The declared maximum code length.</summary>
    public int MaximumLength;
    /// <summary>The JSON Schema or XSD lexical pattern when declared.</summary>
    public string Pattern = string.Empty;
    /// <summary>The code values in deterministic source order.</summary>
    public List<ExternalCodeValue> Values = new List<ExternalCodeValue>();
}
