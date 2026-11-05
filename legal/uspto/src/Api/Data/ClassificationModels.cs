namespace uspto;

/// <summary>
/// Holds one Cooperative Patent Classification assignment.
/// </summary>
public class PatentCpcClassification
{
    /// <summary>The complete CPC symbol.</summary>
    public string? Symbol = null;

    /// <summary>The CPC section.</summary>
    public string? Section = null;

    /// <summary>The CPC class.</summary>
    public string? Class = null;

    /// <summary>The CPC subclass.</summary>
    public string? Subclass = null;

    /// <summary>The CPC main group.</summary>
    public string? MainGroup = null;

    /// <summary>The CPC subgroup.</summary>
    public string? Subgroup = null;

    /// <summary>The classification version date or edition text.</summary>
    public string? Version = null;

    /// <summary>The assignment sequence supplied by the source.</summary>
    public int Sequence = 0;

    /// <summary>Whether <see cref="Sequence"/> was supplied.</summary>
    public bool HasSequence = false;

    /// <summary>Whether the classification is identified as inventive.</summary>
    public bool IsInventive = false;

    /// <summary>Whether <see cref="IsInventive"/> was supplied.</summary>
    public bool HasIsInventive = false;

    /// <summary>Whether this is identified as the first or primary classification.</summary>
    public bool IsPrimary = false;

    /// <summary>Whether <see cref="IsPrimary"/> was supplied.</summary>
    public bool HasIsPrimary = false;
}

/// <summary>
/// Holds one International Patent Classification assignment.
/// </summary>
public class PatentIpcClassification
{
    /// <summary>The complete IPC symbol.</summary>
    public string? Symbol = null;

    /// <summary>The IPC section.</summary>
    public string? Section = null;

    /// <summary>The IPC class.</summary>
    public string? Class = null;

    /// <summary>The IPC subclass.</summary>
    public string? Subclass = null;

    /// <summary>The IPC main group.</summary>
    public string? MainGroup = null;

    /// <summary>The IPC subgroup.</summary>
    public string? Subgroup = null;

    /// <summary>The IPC edition or version date text.</summary>
    public string? Version = null;

    /// <summary>The classification level or scope code.</summary>
    public string? ClassificationLevel = null;

    /// <summary>The classification position or source code.</summary>
    public string? PositionCode = null;

    /// <summary>The classification value or inventive-status code.</summary>
    public string? ValueCode = null;

    /// <summary>The assignment sequence supplied by the source.</summary>
    public int Sequence = 0;

    /// <summary>Whether <see cref="Sequence"/> was supplied.</summary>
    public bool HasSequence = false;
}

/// <summary>
/// Holds one United States Patent Classification assignment.
/// </summary>
public class PatentUspcClassification
{
    /// <summary>The complete USPC class and subclass symbol.</summary>
    public string? Symbol = null;

    /// <summary>The USPC class.</summary>
    public string? Class = null;

    /// <summary>The USPC subclass.</summary>
    public string? Subclass = null;

    /// <summary>The source category for the classification assignment.</summary>
    public string? ClassificationCategory = null;

    /// <summary>The assignment sequence supplied by the source.</summary>
    public int Sequence = 0;

    /// <summary>Whether <see cref="Sequence"/> was supplied.</summary>
    public bool HasSequence = false;

    /// <summary>Whether this is identified as the primary USPC classification.</summary>
    public bool IsPrimary = false;

    /// <summary>Whether <see cref="IsPrimary"/> was supplied.</summary>
    public bool HasIsPrimary = false;
}

/// <summary>
/// Holds one WIPO technology-field classification.
/// </summary>
public class PatentWipoClassification
{
    /// <summary>The WIPO technology field code.</summary>
    public string? FieldCode = null;

    /// <summary>The WIPO technology field name.</summary>
    public string? FieldName = null;

    /// <summary>The broader technology sector.</summary>
    public string? Sector = null;

    /// <summary>The assignment sequence supplied by the source.</summary>
    public int Sequence = 0;

    /// <summary>Whether <see cref="Sequence"/> was supplied.</summary>
    public bool HasSequence = false;
}

/// <summary>
/// Holds a government-interest statement and its award or agency identifiers.
/// </summary>
public class PatentGovernmentInterest
{
    /// <summary>The complete government-interest statement.</summary>
    public string? Statement = null;

    /// <summary>The government agency name.</summary>
    public string? AgencyName = null;

    /// <summary>The government agency code or source identifier.</summary>
    public string? AgencyIdentifier = null;

    /// <summary>The contract, grant, or award number.</summary>
    public string? AwardNumber = null;

    /// <summary>The contractor or recipient organization.</summary>
    public PatentOrganization? RecipientOrganization = null;

    /// <summary>The government's rights or interest category.</summary>
    public string? RightsCategory = null;

    /// <summary>Classifications explicitly associated with the government-interest record.</summary>
    public PatentClassificationData? RelatedClassifications = null;
}

/// <summary>
/// Collects patent classifications and government-interest statements without merging their schemes.
/// </summary>
public class PatentClassificationData
{
    /// <summary>The Cooperative Patent Classification assignments.</summary>
    public PatentCpcClassification[] Cpc = new PatentCpcClassification[0];

    /// <summary>The International Patent Classification assignments.</summary>
    public PatentIpcClassification[] Ipc = new PatentIpcClassification[0];

    /// <summary>The United States Patent Classification assignments.</summary>
    public PatentUspcClassification[] Uspc = new PatentUspcClassification[0];

    /// <summary>The WIPO technology-field assignments.</summary>
    public PatentWipoClassification[] Wipo = new PatentWipoClassification[0];

    /// <summary>The government-interest records associated with the invention.</summary>
    public PatentGovernmentInterest[] GovernmentInterests = new PatentGovernmentInterest[0];
}
