using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes one profile overlay restriction on a qualified message path.</summary>
public class ProfileConstraint
{
    /// <summary>The stable profile constraint identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The human-readable constraint name.</summary>
    public string Name = string.Empty;
    /// <summary>The human-readable constraint definition.</summary>
    public string Definition = string.Empty;
    /// <summary>The qualified message path affected by the constraint.</summary>
    public XmlPathData Path = new XmlPathData();
    /// <summary>The identifier of the base ISO or inherited profile constraint being changed.</summary>
    public string BaseConstraintIdentifier = string.Empty;
    /// <summary>Whether the identified inherited constraint is removed by this overlay.</summary>
    public bool RemoveBaseConstraint;
    /// <summary>Whether the selected path must occur.</summary>
    public bool RequirePath;
    /// <summary>Whether the selected path is prohibited.</summary>
    public bool ProhibitPath;
    /// <summary>Whether a minimum occurrence restriction is supplied.</summary>
    public bool HasMinimumOccurrences;
    /// <summary>The inclusive minimum occurrence count when supplied.</summary>
    public int MinimumOccurrences;
    /// <summary>Whether a maximum occurrence restriction is supplied.</summary>
    public bool HasMaximumOccurrences;
    /// <summary>The inclusive maximum occurrence count when supplied.</summary>
    public int MaximumOccurrences;
    /// <summary>The complete allowed-value set when the profile restricts values.</summary>
    public List<string> AllowedValues = new List<string>();
    /// <summary>Values prohibited by the profile in addition to the base definition.</summary>
    public List<string> ProhibitedValues = new List<string>();
    /// <summary>The diagnostic severity used when this constraint fails.</summary>
    public ValidationSeverity Severity = ValidationSeverity.Error;
}
