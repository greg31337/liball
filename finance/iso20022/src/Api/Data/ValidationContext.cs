using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains resolved message identity and caller state shared with validation stages and semantic rules.</summary>
public class ValidationContext
{
    /// <summary>The resolved exact message identifier when known.</summary>
    public MessageIdentifier? MessageIdentifier;
    /// <summary>The resolved message namespace URI when known.</summary>
    public string NamespaceUri = string.Empty;
    /// <summary>The selected market-practice or caller profile identifier.</summary>
    public string ProfileIdentifier = string.Empty;
    /// <summary>The selected business-service identifier when supplied by the caller or header.</summary>
    public string BusinessService = string.Empty;
    /// <summary>The exact repository artifact edition used for model and constraint validation.</summary>
    public string RepositoryEdition = string.Empty;
    /// <summary>The effective time used for activity-date and temporal validation when supplied.</summary>
    public IsoDateTimeValue? EffectiveAt;
    /// <summary>Caller-owned state made available to semantic rules without using delegates.</summary>
    public Dictionary<string, object> CallerState = new Dictionary<string, object>();
}
