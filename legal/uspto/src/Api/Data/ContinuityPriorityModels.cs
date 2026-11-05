namespace uspto;

/// <summary>
/// Describes a parent application from which the requested application claims continuity.
/// </summary>
public class PatentParentContinuity
{
    /// <summary>The canonical parent application number.</summary>
    public string? ParentApplicationNumber = null;

    /// <summary>The canonical child application number.</summary>
    public string? ChildApplicationNumber = null;

    /// <summary>The patent number associated with the parent application.</summary>
    public string? ParentPatentNumber = null;

    /// <summary>The source code identifying the parentage relationship.</summary>
    public string? RelationshipCode = null;

    /// <summary>The human-readable parentage relationship.</summary>
    public string? RelationshipDescription = null;

    /// <summary>The parent application's status code.</summary>
    public int ParentApplicationStatusCode = 0;

    /// <summary>Whether <see cref="ParentApplicationStatusCode"/> was supplied.</summary>
    public bool HasParentApplicationStatusCode = false;

    /// <summary>The parent application's status description.</summary>
    public string? ParentApplicationStatusDescription = null;

    /// <summary>The parent application's filing date.</summary>
    public global::System.DateTimeOffset ParentApplicationFilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ParentApplicationFilingDate"/> was supplied.</summary>
    public bool HasParentApplicationFilingDate = false;

    /// <summary>Whether the relationship is governed by first-inventor-to-file rules.</summary>
    public bool IsFirstInventorToFile = false;

    /// <summary>Whether <see cref="IsFirstInventorToFile"/> was supplied.</summary>
    public bool HasIsFirstInventorToFile = false;

    /// <summary>Unmodeled parent-continuity fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Describes a child application claiming continuity from the requested application.
/// </summary>
public class PatentChildContinuity
{
    /// <summary>The canonical parent application number.</summary>
    public string? ParentApplicationNumber = null;

    /// <summary>The canonical child application number.</summary>
    public string? ChildApplicationNumber = null;

    /// <summary>The patent number associated with the child application.</summary>
    public string? ChildPatentNumber = null;

    /// <summary>The source code identifying the parentage relationship.</summary>
    public string? RelationshipCode = null;

    /// <summary>The human-readable parentage relationship.</summary>
    public string? RelationshipDescription = null;

    /// <summary>The child application's status code.</summary>
    public int ChildApplicationStatusCode = 0;

    /// <summary>Whether <see cref="ChildApplicationStatusCode"/> was supplied.</summary>
    public bool HasChildApplicationStatusCode = false;

    /// <summary>The child application's status description.</summary>
    public string? ChildApplicationStatusDescription = null;

    /// <summary>The child application's filing date.</summary>
    public global::System.DateTimeOffset ChildApplicationFilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ChildApplicationFilingDate"/> was supplied.</summary>
    public bool HasChildApplicationFilingDate = false;

    /// <summary>Whether the relationship is governed by first-inventor-to-file rules.</summary>
    public bool IsFirstInventorToFile = false;

    /// <summary>Whether <see cref="IsFirstInventorToFile"/> was supplied.</summary>
    public bool HasIsFirstInventorToFile = false;

    /// <summary>Unmodeled child-continuity fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds the parent and child continuity relationships returned for one patent application.
/// </summary>
public class PatentContinuity
{
    /// <summary>The canonical requested application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The applications from which the requested application claims continuity.</summary>
    public PatentParentContinuity[] Parents = new PatentParentContinuity[0];

    /// <summary>The applications claiming continuity from the requested application.</summary>
    public PatentChildContinuity[] Children = new PatentChildContinuity[0];

    /// <summary>The total number of returned parent and child relationships.</summary>
    public int Count = 0;

    /// <summary>The canonical graph projected from the returned relationships.</summary>
    public PatentContinuityGraph Graph = new PatentContinuityGraph();

    /// <summary>The service request identifier associated with the response.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and authority of the continuity data.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>Unmodeled response and wrapper fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Describes one application node in a patent continuity graph.
/// </summary>
public class PatentContinuityGraphNode
{
    /// <summary>The canonical patent application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The patent number associated with the application, when known.</summary>
    public string? PatentNumber = null;

    /// <summary>The application's status code, when known.</summary>
    public int ApplicationStatusCode = 0;

    /// <summary>Whether <see cref="ApplicationStatusCode"/> was supplied.</summary>
    public bool HasApplicationStatusCode = false;

    /// <summary>The application's status description, when known.</summary>
    public string? ApplicationStatusDescription = null;

    /// <summary>The application's filing date, when known.</summary>
    public global::System.DateTimeOffset FilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FilingDate"/> was supplied.</summary>
    public bool HasFilingDate = false;

    /// <summary>Whether this is the root application supplied first to graph construction.</summary>
    public bool IsRoot = false;

    /// <summary>Whether the application is referenced by an edge but lacks a supplied continuity record.</summary>
    public bool IsMissing = false;
}

/// <summary>
/// Describes one directed parent-to-child relationship in a patent continuity graph.
/// </summary>
public class PatentContinuityGraphEdge
{
    /// <summary>The canonical parent application number.</summary>
    public string? ParentApplicationNumber = null;

    /// <summary>The canonical child application number.</summary>
    public string? ChildApplicationNumber = null;

    /// <summary>The source code identifying the parentage relationship.</summary>
    public string? RelationshipCode = null;

    /// <summary>The human-readable parentage relationship.</summary>
    public string? RelationshipDescription = null;

    /// <summary>Whether this edge participates in a directed cycle.</summary>
    public bool IsCycle = false;
}

/// <summary>
/// Holds a canonical directed graph projected from patent continuity records.
/// </summary>
public class PatentContinuityGraph
{
    /// <summary>The first supplied canonical application number.</summary>
    public string? RootApplicationNumber = null;

    /// <summary>The graph nodes in deterministic discovery order.</summary>
    public PatentContinuityGraphNode[] Nodes = new PatentContinuityGraphNode[0];

    /// <summary>The unique parent-to-child edges in deterministic source order.</summary>
    public PatentContinuityGraphEdge[] Edges = new PatentContinuityGraphEdge[0];

    /// <summary>The number of referenced applications lacking supplied continuity records.</summary>
    public int MissingNodeCount = 0;

    /// <summary>The number of repeated source edges omitted from the graph.</summary>
    public int DuplicateEdgeCount = 0;

    /// <summary>Whether one or more directed cycles were detected.</summary>
    public bool HasCycle = false;

    /// <summary>The client-derived provenance and source labels of the graph.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();
}

/// <summary>
/// Describes one foreign priority claim associated with a patent application.
/// </summary>
public class PatentForeignPriorityClaim
{
    /// <summary>The intellectual-property office name reported by the source.</summary>
    public string? OfficeName = null;

    /// <summary>The country or intellectual-property office code, when supplied.</summary>
    public string? CountryCode = null;

    /// <summary>The foreign application number, preserved as an identifier string.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The foreign application's filing date.</summary>
    public global::System.DateTimeOffset FilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FilingDate"/> was supplied.</summary>
    public bool HasFilingDate = false;

    /// <summary>The priority claim's source sequence number.</summary>
    public int PrioritySequenceNumber = 0;

    /// <summary>Whether <see cref="PrioritySequenceNumber"/> was supplied.</summary>
    public bool HasPrioritySequenceNumber = false;

    /// <summary>Unmodeled foreign-priority fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds the foreign priority claims returned for one patent application.
/// </summary>
public class PatentForeignPriorityList
{
    /// <summary>The canonical requested application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The foreign priority claims in source order.</summary>
    public PatentForeignPriorityClaim[] Claims = new PatentForeignPriorityClaim[0];

    /// <summary>The number of returned foreign priority claims.</summary>
    public int Count = 0;

    /// <summary>The service request identifier associated with the response.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and authority of the foreign priority data.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>Unmodeled response and wrapper fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}
