using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes one published value in an ISO 20022 external code set.</summary>
public class ExternalCodeValue
{
    /// <summary>The exact abbreviated code value.</summary>
    public string Value = string.Empty;
    /// <summary>The published non-abbreviated code name.</summary>
    public string Name = string.Empty;
    /// <summary>The published semantic definition.</summary>
    public string Definition = string.Empty;
    /// <summary>The organization or maintenance request that requested the value.</summary>
    public string Requester = string.Empty;
    /// <summary>The exact published registration-status value.</summary>
    public string RegistrationStatus = string.Empty;
    /// <summary>The most recent published update date when known.</summary>
    public IsoDateValue? LastUpdatedDate;
    /// <summary>The published creation date when known.</summary>
    public IsoDateValue? CreationDate;
    /// <summary>The date on which the code value becomes active when declared.</summary>
    public IsoDateValue? ActivationDate;
    /// <summary>The date on which the code value ceases to be active when declared.</summary>
    public IsoDateValue? DeactivationDate;
    /// <summary>The scheduled or effective removal date when known.</summary>
    public IsoDateValue? RemovalDate;
    /// <summary>The replacement code value when declared.</summary>
    public string ReplacedBy = string.Empty;
    /// <summary>The ordered external standards associated with mapped values.</summary>
    public List<string> MappingStandards = new List<string>();
    /// <summary>The ordered mapped values corresponding to <see cref="MappingStandards"/>.</summary>
    public List<string> MappingValues = new List<string>();
    /// <summary>Additional published information about the code value.</summary>
    public string AdditionalInformation = string.Empty;
}
