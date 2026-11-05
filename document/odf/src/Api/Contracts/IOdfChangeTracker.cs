namespace odf;

/// <summary>
/// Enumerates, filters, accepts, rejects, records, replays, and exports changes.
/// </summary>
public interface IOdfChangeTracker
{
    /// <summary>Gets tracked text changes in document order.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>An independent array of live change models.</returns>
    public OdfChange[] GetTextChanges(OdfDocument document);
    /// <summary>Gets tracked spreadsheet changes in document order.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>An independent array of live spreadsheet-change models.</returns>
    public OdfSpreadsheetChange[] GetSpreadsheetChanges(
        OdfDocument document);
    /// <summary>Filters text changes by creator, date, and comment fragments.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="creator">The creator fragment, or an empty string.</param>
    /// <param name="date">The date fragment, or an empty string.</param>
    /// <param name="comment">The comment fragment, or an empty string.</param>
    /// <param name="matchCase">Whether comparisons are case-sensitive.</param>
    /// <returns>The matching live text changes.</returns>
    public OdfChange[] FilterTextChanges(
        OdfDocument document,
        string creator,
        string date,
        string comment,
        bool matchCase);
    /// <summary>Filters spreadsheet changes by creator, date, and comment fragments.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="creator">The creator fragment, or an empty string.</param>
    /// <param name="date">The date fragment, or an empty string.</param>
    /// <param name="comment">The comment fragment, or an empty string.</param>
    /// <param name="matchCase">Whether comparisons are case-sensitive.</param>
    /// <returns>The matching live spreadsheet changes.</returns>
    public OdfSpreadsheetChange[] FilterSpreadsheetChanges(
        OdfDocument document,
        string creator,
        string date,
        string comment,
        bool matchCase);
    /// <summary>Projects text while independently showing insertions and deletions.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="showInsertions">Whether inserted content is visible.</param>
    /// <param name="showDeletions">Whether deleted content is visible.</param>
    /// <returns>The projected plain text.</returns>
    public string ProjectVisibleText(
        OdfDocument document,
        bool showInsertions,
        bool showDeletions);
    /// <summary>Accepts one tracked text change.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="change">The change to accept.</param>
    /// <returns>Whether the change could be applied.</returns>
    public bool AcceptTextChange(
        OdfDocument document,
        OdfChange change);
    /// <summary>Rejects one tracked text change.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="change">The change to reject.</param>
    /// <returns>Whether the change could be applied.</returns>
    public bool RejectTextChange(
        OdfDocument document,
        OdfChange change);
    /// <summary>Accepts every resolvable tracked text change.</summary>
    /// <param name="document">The document to modify.</param>
    /// <returns>The number of accepted changes.</returns>
    public int AcceptAllTextChanges(OdfDocument document);
    /// <summary>Rejects every resolvable tracked text change.</summary>
    /// <param name="document">The document to modify.</param>
    /// <returns>The number of rejected changes.</returns>
    public int RejectAllTextChanges(OdfDocument document);
    /// <summary>Accepts selected tracked text changes.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="changes">The selected live changes.</param>
    /// <returns>The number of accepted changes.</returns>
    public int AcceptSelectedTextChanges(
        OdfDocument document,
        OdfChange[] changes);
    /// <summary>Rejects selected tracked text changes.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="changes">The selected live changes.</param>
    /// <returns>The number of rejected changes.</returns>
    public int RejectSelectedTextChanges(
        OdfDocument document,
        OdfChange[] changes);
    /// <summary>Marks one spreadsheet change accepted.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="change">The change to accept.</param>
    /// <returns>Whether the state was assigned.</returns>
    public bool AcceptSpreadsheetChange(
        OdfDocument document,
        OdfSpreadsheetChange change);
    /// <summary>Marks one spreadsheet change rejected.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="change">The change to reject.</param>
    /// <param name="rejectingChangeId">The rejecting change identifier.</param>
    /// <returns>Whether the state was assigned.</returns>
    public bool RejectSpreadsheetChange(
        OdfDocument document,
        OdfSpreadsheetChange change,
        string rejectingChangeId);
    /// <summary>Marks every spreadsheet change accepted.</summary>
    /// <param name="document">The document to modify.</param>
    /// <returns>The number of changes updated.</returns>
    public int AcceptAllSpreadsheetChanges(OdfDocument document);
    /// <summary>Marks every spreadsheet change rejected.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="rejectingChangeId">The rejecting change identifier.</param>
    /// <returns>The number of changes updated.</returns>
    public int RejectAllSpreadsheetChanges(
        OdfDocument document,
        string rejectingChangeId);
    /// <summary>Marks selected spreadsheet changes accepted.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="changes">The selected live changes.</param>
    /// <returns>The number of changes updated.</returns>
    public int AcceptSelectedSpreadsheetChanges(
        OdfDocument document,
        OdfSpreadsheetChange[] changes);
    /// <summary>Marks selected spreadsheet changes rejected.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="changes">The selected live changes.</param>
    /// <param name="rejectingChangeId">The rejecting change identifier.</param>
    /// <returns>The number of changes updated.</returns>
    public int RejectSelectedSpreadsheetChanges(
        OdfDocument document,
        OdfSpreadsheetChange[] changes,
        string rejectingChangeId);
    /// <summary>Gets recorded SDK collaboration operations in recorded order.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>An independent array of live operation records.</returns>
    public OdfOperation[] GetOperations(OdfDocument document);
    /// <summary>Records a future node insertion without applying it.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="parent">The insertion parent.</param>
    /// <param name="childIndex">The insertion index.</param>
    /// <param name="payload">The subtree to capture.</param>
    /// <returns>The recorded operation.</returns>
    public OdfOperation RecordInsertNodeOperation(
        OdfDocument document,
        OdfNode parent,
        int childIndex,
        OdfNode payload);
    /// <summary>Records a future node removal without applying it.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="target">The node to capture and remove during replay.</param>
    /// <returns>The recorded operation.</returns>
    public OdfOperation RecordRemoveNodeOperation(
        OdfDocument document,
        OdfNode target);
    /// <summary>Records a future node move without applying it.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="target">The node to move.</param>
    /// <param name="destinationParent">The destination parent.</param>
    /// <param name="destinationIndex">The destination index.</param>
    /// <returns>The recorded operation.</returns>
    public OdfOperation RecordMoveNodeOperation(
        OdfDocument document,
        OdfNode target,
        OdfNode destinationParent,
        int destinationIndex);
    /// <summary>Records a future attribute assignment without applying it.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="target">The target element.</param>
    /// <param name="attributeName">The qualified attribute name.</param>
    /// <param name="value">The future value.</param>
    /// <returns>The recorded operation.</returns>
    public OdfOperation RecordSetAttributeOperation(
        OdfDocument document,
        OdfNode target,
        OdfQualifiedName attributeName,
        string value);
    /// <summary>Records a future attribute removal without applying it.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="target">The target element.</param>
    /// <param name="attributeName">The qualified attribute name.</param>
    /// <returns>The recorded operation.</returns>
    public OdfOperation RecordRemoveAttributeOperation(
        OdfDocument document,
        OdfNode target,
        OdfQualifiedName attributeName);
    /// <summary>Records a future text replacement without applying it.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="target">The target node.</param>
    /// <param name="value">The future text content.</param>
    /// <returns>The recorded operation.</returns>
    public OdfOperation RecordSetTextOperation(
        OdfDocument document,
        OdfNode target,
        string value);
    /// <summary>Appends a caller-created operation record.</summary>
    /// <param name="document">The document operation log.</param>
    /// <param name="operation">The operation to append.</param>
    /// <returns>The appended operation.</returns>
    public OdfOperation RecordOperation(
        OdfDocument document,
        OdfOperation operation);
    /// <summary>Replays supplied operations in stable sequence order.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="operations">The operations to replay.</param>
    /// <returns>The atomic replay result.</returns>
    public OdfEditResult ReplayOperations(
        OdfDocument document,
        OdfOperation[] operations);
    /// <summary>Replays the document's recorded operation collection.</summary>
    /// <param name="document">The document to modify.</param>
    /// <returns>The atomic replay result.</returns>
    public OdfEditResult ReplayRecordedOperations(OdfDocument document);
    /// <summary>Creates a reverse-order inverse operation collection.</summary>
    /// <param name="operations">The operations to invert.</param>
    /// <returns>The inverse operation collection.</returns>
    public OdfOperation[] InvertOperations(OdfOperation[] operations);
    /// <summary>Exports recorded operations as non-standard extension XML.</summary>
    /// <param name="document">The document operation log.</param>
    /// <param name="options">The XML save options.</param>
    /// <returns>The complete extension XML bytes.</returns>
    public byte[] ExportOperationsBytes(
        OdfDocument document,
        OdfSaveOptions options);
    /// <summary>Imports non-standard operation-extension XML.</summary>
    /// <param name="data">The complete extension XML bytes.</param>
    /// <param name="options">The XML load options and security limits.</param>
    /// <returns>The imported operations.</returns>
    public OdfOperation[] ImportOperationsBytes(
        byte[] data,
        OdfLoadOptions options);
}
