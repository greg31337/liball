namespace uspto;

/// <summary>
/// Holds a PatentsView CPC class, subclass, group, or document assignment.
/// </summary>
public class PatentsViewCpcClassification
{
    /// <summary>The identifier of the represented CPC level or assignment.</summary>
    public string? ClassificationIdentifier = null;

    /// <summary>The class, subclass, group, or assignment level.</summary>
    public string? Level = null;

    /// <summary>The title of the represented CPC level.</summary>
    public string? Title = null;

    /// <summary>The CPC section symbol.</summary>
    public string? Section = null;

    /// <summary>The CPC class symbol.</summary>
    public string? Class = null;

    /// <summary>The CPC class identifier.</summary>
    public string? ClassIdentifier = null;

    /// <summary>The CPC subclass symbol.</summary>
    public string? Subclass = null;

    /// <summary>The CPC subclass identifier.</summary>
    public string? SubclassIdentifier = null;

    /// <summary>The CPC group symbol.</summary>
    public string? Group = null;

    /// <summary>The CPC group identifier.</summary>
    public string? GroupIdentifier = null;

    /// <summary>The inventional or additional assignment category.</summary>
    public string? ClassificationType = null;

    /// <summary>The assignment order within a patent or publication.</summary>
    public int Sequence = 0;

    /// <summary>Whether <see cref="Sequence"/> was supplied.</summary>
    public bool HasSequence = false;

    /// <summary>The first patent date on which this classification was recorded.</summary>
    public global::System.DateTimeOffset FirstSeenDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FirstSeenDate"/> was supplied.</summary>
    public bool HasFirstSeenDate = false;

    /// <summary>The most recent patent date on which this classification was recorded.</summary>
    public global::System.DateTimeOffset LastSeenDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastSeenDate"/> was supplied.</summary>
    public bool HasLastSeenDate = false;

    /// <summary>The number of active calendar years.</summary>
    public int YearsActive = 0;

    /// <summary>Whether <see cref="YearsActive"/> was supplied.</summary>
    public bool HasYearsActive = false;

    /// <summary>The number of associated patents.</summary>
    public int PatentCount = 0;

    /// <summary>Whether <see cref="PatentCount"/> was supplied.</summary>
    public bool HasPatentCount = false;

    /// <summary>The number of associated assignees.</summary>
    public int AssigneeCount = 0;

    /// <summary>Whether <see cref="AssigneeCount"/> was supplied.</summary>
    public bool HasAssigneeCount = false;

    /// <summary>The number of associated inventors.</summary>
    public int InventorCount = 0;

    /// <summary>Whether <see cref="InventorCount"/> was supplied.</summary>
    public bool HasInventorCount = false;

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled CPC fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one PatentsView International Patent Classification record or document assignment.
/// </summary>
public class PatentsViewIpcClassification
{
    /// <summary>The persistent PatentsView IPC entry identifier.</summary>
    public string? ClassificationIdentifier = null;

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

    /// <summary>The source classification-data category.</summary>
    public string? DataSource = null;

    /// <summary>The source classification value.</summary>
    public string? ClassificationValue = null;

    /// <summary>The source symbol-position code.</summary>
    public string? SymbolPosition = null;

    /// <summary>The source classification level.</summary>
    public string? ClassificationLevel = null;

    /// <summary>The source classification status.</summary>
    public string? ClassificationStatus = null;

    /// <summary>The classification action date.</summary>
    public global::System.DateTimeOffset ActionDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ActionDate"/> was supplied.</summary>
    public bool HasActionDate = false;

    /// <summary>The assignment order within a patent or publication.</summary>
    public int Sequence = 0;

    /// <summary>Whether <see cref="Sequence"/> was supplied.</summary>
    public bool HasSequence = false;

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled IPC fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds a PatentsView USPC main class, subclass, or document assignment.
/// </summary>
public class PatentsViewUspcClassification
{
    /// <summary>The identifier of the represented USPC level or assignment.</summary>
    public string? ClassificationIdentifier = null;

    /// <summary>The main-class, subclass, or assignment level.</summary>
    public string? Level = null;

    /// <summary>The title of the represented USPC level.</summary>
    public string? Title = null;

    /// <summary>The USPC main-class symbol.</summary>
    public string? MainClass = null;

    /// <summary>The USPC main-class identifier.</summary>
    public string? MainClassIdentifier = null;

    /// <summary>The USPC subclass symbol.</summary>
    public string? Subclass = null;

    /// <summary>The USPC subclass identifier.</summary>
    public string? SubclassIdentifier = null;

    /// <summary>The assignment order within a patent or publication.</summary>
    public int Sequence = 0;

    /// <summary>Whether <see cref="Sequence"/> was supplied.</summary>
    public bool HasSequence = false;

    /// <summary>The first patent date on which this classification was recorded.</summary>
    public global::System.DateTimeOffset FirstSeenDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FirstSeenDate"/> was supplied.</summary>
    public bool HasFirstSeenDate = false;

    /// <summary>The most recent patent date on which this classification was recorded.</summary>
    public global::System.DateTimeOffset LastSeenDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastSeenDate"/> was supplied.</summary>
    public bool HasLastSeenDate = false;

    /// <summary>The number of active calendar years.</summary>
    public int YearsActive = 0;

    /// <summary>Whether <see cref="YearsActive"/> was supplied.</summary>
    public bool HasYearsActive = false;

    /// <summary>The number of associated patents.</summary>
    public int PatentCount = 0;

    /// <summary>Whether <see cref="PatentCount"/> was supplied.</summary>
    public bool HasPatentCount = false;

    /// <summary>The number of associated assignees.</summary>
    public int AssigneeCount = 0;

    /// <summary>Whether <see cref="AssigneeCount"/> was supplied.</summary>
    public bool HasAssigneeCount = false;

    /// <summary>The number of associated inventors.</summary>
    public int InventorCount = 0;

    /// <summary>Whether <see cref="InventorCount"/> was supplied.</summary>
    public bool HasInventorCount = false;

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled USPC fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one PatentsView WIPO technology sector and field classification.
/// </summary>
public class PatentsViewWipoClassification
{
    /// <summary>The persistent PatentsView WIPO entry identifier.</summary>
    public string? ClassificationIdentifier = null;

    /// <summary>The technology sector title.</summary>
    public string? SectorTitle = null;

    /// <summary>The technology field title.</summary>
    public string? FieldTitle = null;

    /// <summary>The assignment order within a patent or publication.</summary>
    public int Sequence = 0;

    /// <summary>Whether <see cref="Sequence"/> was supplied.</summary>
    public bool HasSequence = false;

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled WIPO fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one page or bounded aggregate of PatentsView CPC classifications.
/// </summary>
public class PatentsViewCpcClassificationResponse
{
    /// <summary>The returned CPC records in stable query order.</summary>
    public PatentsViewCpcClassification[] Classifications = new PatentsViewCpcClassification[0];

    /// <summary>The query, counts, cursor, raw response, and research provenance.</summary>
    public PatentsViewResponseMetadata Metadata = new PatentsViewResponseMetadata();
}

/// <summary>
/// Holds one page or bounded aggregate of PatentsView IPC classifications.
/// </summary>
public class PatentsViewIpcClassificationResponse
{
    /// <summary>The returned IPC records in stable query order.</summary>
    public PatentsViewIpcClassification[] Classifications = new PatentsViewIpcClassification[0];

    /// <summary>The query, counts, cursor, raw response, and research provenance.</summary>
    public PatentsViewResponseMetadata Metadata = new PatentsViewResponseMetadata();
}

/// <summary>
/// Holds one page or bounded aggregate of PatentsView USPC classifications.
/// </summary>
public class PatentsViewUspcClassificationResponse
{
    /// <summary>The returned USPC records in stable query order.</summary>
    public PatentsViewUspcClassification[] Classifications = new PatentsViewUspcClassification[0];

    /// <summary>The query, counts, cursor, raw response, and research provenance.</summary>
    public PatentsViewResponseMetadata Metadata = new PatentsViewResponseMetadata();
}

/// <summary>
/// Holds one page or bounded aggregate of PatentsView WIPO classifications.
/// </summary>
public class PatentsViewWipoClassificationResponse
{
    /// <summary>The returned WIPO records in stable query order.</summary>
    public PatentsViewWipoClassification[] Classifications = new PatentsViewWipoClassification[0];

    /// <summary>The query, counts, cursor, raw response, and research provenance.</summary>
    public PatentsViewResponseMetadata Metadata = new PatentsViewResponseMetadata();
}
