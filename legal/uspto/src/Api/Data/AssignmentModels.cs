namespace uspto;

/// <summary>
/// Describes a party transferring an interest in a recorded patent assignment.
/// </summary>
public class PatentAssignmentAssignor
{
    /// <summary>The assignor name as supplied by the source.</summary>
    public string? Name = null;

    /// <summary>The date on which the assignor executed the assignment.</summary>
    public global::System.DateTimeOffset ExecutionDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ExecutionDate"/> was supplied.</summary>
    public bool HasExecutionDate = false;

    /// <summary>Unmodeled assignor fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Describes a party receiving an interest in a recorded patent assignment.
/// </summary>
public class PatentAssignmentAssignee
{
    /// <summary>The assignee name as supplied by the source.</summary>
    public string? Name = null;

    /// <summary>The assignee's mailing address.</summary>
    public PatentAddress Address = new PatentAddress();

    /// <summary>Unmodeled assignee fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Describes a correspondent or domestic representative for a patent assignment.
/// </summary>
public class PatentAssignmentCorrespondent
{
    /// <summary>The correspondent or representative name.</summary>
    public string? Name = null;

    /// <summary>The correspondent or representative address.</summary>
    public PatentAddress Address = new PatentAddress();

    /// <summary>The source's last-ingestion time for the contact.</summary>
    public global::System.DateTimeOffset LastIngestedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastIngestedAt"/> was supplied.</summary>
    public bool HasLastIngestedAt = false;

    /// <summary>Unmodeled correspondent fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Describes one patent property identified by an assignment record.
/// </summary>
public class PatentAssignmentProperty
{
    /// <summary>The source identifier for the assigned property.</summary>
    public string? PropertyIdentifier = null;

    /// <summary>The source property type or category.</summary>
    public string? PropertyType = null;

    /// <summary>The related patent application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The related patent number.</summary>
    public string? PatentNumber = null;

    /// <summary>The related pre-grant publication number.</summary>
    public string? PublicationNumber = null;

    /// <summary>The title associated with the assigned property.</summary>
    public string? Title = null;

    /// <summary>Unmodeled assignment-property fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Describes one recorded transfer of an interest in a patent property.
/// </summary>
public class PatentAssignment
{
    /// <summary>The assignment reel identifier, preserving source zero padding.</summary>
    public string? ReelNumber = null;

    /// <summary>The assignment frame identifier, preserving source zero padding.</summary>
    public string? FrameNumber = null;

    /// <summary>The combined reel and frame identifier.</summary>
    public string? ReelAndFrameNumber = null;

    /// <summary>The assignment document's page count.</summary>
    public int PageCount = 0;

    /// <summary>Whether <see cref="PageCount"/> was supplied.</summary>
    public bool HasPageCount = false;

    /// <summary>Whether an assignment document image is available.</summary>
    public bool IsImageAvailable = false;

    /// <summary>Whether <see cref="IsImageAvailable"/> was supplied.</summary>
    public bool HasIsImageAvailable = false;

    /// <summary>The credential-free assignment document location.</summary>
    public global::System.Uri? DocumentUri = null;

    /// <summary>The date on which the assignment was received.</summary>
    public global::System.DateTimeOffset ReceivedDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ReceivedDate"/> was supplied.</summary>
    public bool HasReceivedDate = false;

    /// <summary>The date on which the assignment was recorded.</summary>
    public global::System.DateTimeOffset RecordedDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="RecordedDate"/> was supplied.</summary>
    public bool HasRecordedDate = false;

    /// <summary>The date on which assignment correspondence was mailed.</summary>
    public global::System.DateTimeOffset MailedDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="MailedDate"/> was supplied.</summary>
    public bool HasMailedDate = false;

    /// <summary>The description of the conveyed interest or transaction.</summary>
    public string? Conveyance = null;

    /// <summary>The attorney docket number associated with the assignment.</summary>
    public string? AttorneyDocketNumber = null;

    /// <summary>The parties transferring an interest.</summary>
    public PatentAssignmentAssignor[] Assignors = new PatentAssignmentAssignor[0];

    /// <summary>The parties receiving an interest.</summary>
    public PatentAssignmentAssignee[] Assignees = new PatentAssignmentAssignee[0];

    /// <summary>The assignment correspondence destinations.</summary>
    public PatentAssignmentCorrespondent[] Correspondents = new PatentAssignmentCorrespondent[0];

    /// <summary>The domestic representative, when supplied.</summary>
    public PatentAssignmentCorrespondent? DomesticRepresentative = null;

    /// <summary>The patent properties identified by the assignment.</summary>
    public PatentAssignmentProperty[] Properties = new PatentAssignmentProperty[0];

    /// <summary>Unmodeled assignment fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds the recorded patent assignments returned for one application.
/// </summary>
public class PatentAssignmentHistory
{
    /// <summary>The canonical requested patent application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The recorded assignments in source order.</summary>
    public PatentAssignment[] Assignments = new PatentAssignment[0];

    /// <summary>The number of returned assignments.</summary>
    public int Count = 0;

    /// <summary>The service request identifier associated with the response.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and authority of the assignment history.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>Unmodeled response and wrapper fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}
