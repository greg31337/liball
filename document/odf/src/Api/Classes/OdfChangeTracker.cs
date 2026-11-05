namespace odf;

/// <summary>
/// Enumerates, filters, accepts, rejects, records, replays, and exports changes.
/// </summary>
public class OdfChangeTracker : IOdfChangeTracker
{
    /// <summary>Initializes a change tracker.</summary>
    public OdfChangeTracker()
    {
    }

    /// <summary>Gets tracked text changes in document order.</summary>
    public OdfChange[] GetTextChanges(OdfDocument document)
    {
        return ChangeTrackerModel.GetTextChanges(document);
    }

    /// <summary>Gets tracked spreadsheet changes in document order.</summary>
    public OdfSpreadsheetChange[] GetSpreadsheetChanges(
        OdfDocument document)
    {
        return ChangeTrackerModel.GetSpreadsheetChanges(document);
    }

    /// <summary>Filters text changes by creator, date, and comment fragments.</summary>
    public OdfChange[] FilterTextChanges(
        OdfDocument document,
        string creator,
        string date,
        string comment,
        bool matchCase)
    {
        return ChangeProcessingModel.FilterTextChanges(
            document, creator, date, comment, matchCase);
    }

    /// <summary>Filters spreadsheet changes by creator, date, and comment fragments.</summary>
    public OdfSpreadsheetChange[] FilterSpreadsheetChanges(
        OdfDocument document,
        string creator,
        string date,
        string comment,
        bool matchCase)
    {
        return ChangeProcessingModel.FilterSpreadsheetChanges(
            document, creator, date, comment, matchCase);
    }

    /// <summary>Projects text while independently showing insertions and deletions.</summary>
    public string ProjectVisibleText(
        OdfDocument document,
        bool showInsertions,
        bool showDeletions)
    {
        return ChangeProcessingModel.ProjectVisibleText(
            document, showInsertions, showDeletions);
    }

    /// <summary>Accepts one tracked text change.</summary>
    public bool AcceptTextChange(
        OdfDocument document,
        OdfChange change)
    {
        return ChangeProcessingModel.AcceptTextChange(document, change);
    }

    /// <summary>Rejects one tracked text change.</summary>
    public bool RejectTextChange(
        OdfDocument document,
        OdfChange change)
    {
        return ChangeProcessingModel.RejectTextChange(document, change);
    }

    /// <summary>Accepts every resolvable tracked text change.</summary>
    public int AcceptAllTextChanges(OdfDocument document)
    {
        return ChangeProcessingModel.AcceptAllTextChanges(document);
    }

    /// <summary>Rejects every resolvable tracked text change.</summary>
    public int RejectAllTextChanges(OdfDocument document)
    {
        return ChangeProcessingModel.RejectAllTextChanges(document);
    }

    /// <summary>Accepts selected tracked text changes.</summary>
    public int AcceptSelectedTextChanges(
        OdfDocument document,
        OdfChange[] changes)
    {
        return ChangeProcessingModel.AcceptSelectedTextChanges(
            document, changes);
    }

    /// <summary>Rejects selected tracked text changes.</summary>
    public int RejectSelectedTextChanges(
        OdfDocument document,
        OdfChange[] changes)
    {
        return ChangeProcessingModel.RejectSelectedTextChanges(
            document, changes);
    }

    /// <summary>Marks one spreadsheet change accepted.</summary>
    public bool AcceptSpreadsheetChange(
        OdfDocument document,
        OdfSpreadsheetChange change)
    {
        return ChangeProcessingModel.AcceptSpreadsheetChange(
            document, change);
    }

    /// <summary>Marks one spreadsheet change rejected.</summary>
    public bool RejectSpreadsheetChange(
        OdfDocument document,
        OdfSpreadsheetChange change,
        string rejectingChangeId)
    {
        return ChangeProcessingModel.RejectSpreadsheetChange(
            document, change, rejectingChangeId);
    }

    /// <summary>Marks every spreadsheet change accepted.</summary>
    public int AcceptAllSpreadsheetChanges(OdfDocument document)
    {
        return ChangeProcessingModel.AcceptAllSpreadsheetChanges(document);
    }

    /// <summary>Marks every spreadsheet change rejected.</summary>
    public int RejectAllSpreadsheetChanges(
        OdfDocument document,
        string rejectingChangeId)
    {
        return ChangeProcessingModel.RejectAllSpreadsheetChanges(
            document, rejectingChangeId);
    }

    /// <summary>Marks selected spreadsheet changes accepted.</summary>
    public int AcceptSelectedSpreadsheetChanges(
        OdfDocument document,
        OdfSpreadsheetChange[] changes)
    {
        return ChangeProcessingModel.AcceptSelectedSpreadsheetChanges(
            document, changes);
    }

    /// <summary>Marks selected spreadsheet changes rejected.</summary>
    public int RejectSelectedSpreadsheetChanges(
        OdfDocument document,
        OdfSpreadsheetChange[] changes,
        string rejectingChangeId)
    {
        return ChangeProcessingModel.RejectSelectedSpreadsheetChanges(
            document, changes, rejectingChangeId);
    }

    /// <summary>Gets recorded SDK collaboration operations in recorded order.</summary>
    public OdfOperation[] GetOperations(OdfDocument document)
    {
        return ChangeTrackerModel.GetOperations(document);
    }

    /// <summary>Records a future node insertion without applying it.</summary>
    public OdfOperation RecordInsertNodeOperation(
        OdfDocument document,
        OdfNode parent,
        int childIndex,
        OdfNode payload)
    {
        return ChangeTrackerModel.RecordInsertNode(
            document, parent, childIndex, payload);
    }

    /// <summary>Records a future node removal without applying it.</summary>
    public OdfOperation RecordRemoveNodeOperation(
        OdfDocument document,
        OdfNode target)
    {
        return ChangeTrackerModel.RecordRemoveNode(document, target);
    }

    /// <summary>Records a future node move without applying it.</summary>
    public OdfOperation RecordMoveNodeOperation(
        OdfDocument document,
        OdfNode target,
        OdfNode destinationParent,
        int destinationIndex)
    {
        return ChangeTrackerModel.RecordMoveNode(
            document, target, destinationParent, destinationIndex);
    }

    /// <summary>Records a future attribute assignment without applying it.</summary>
    public OdfOperation RecordSetAttributeOperation(
        OdfDocument document,
        OdfNode target,
        OdfQualifiedName attributeName,
        string value)
    {
        return ChangeTrackerModel.RecordSetAttribute(
            document, target, attributeName, value);
    }

    /// <summary>Records a future attribute removal without applying it.</summary>
    public OdfOperation RecordRemoveAttributeOperation(
        OdfDocument document,
        OdfNode target,
        OdfQualifiedName attributeName)
    {
        return ChangeTrackerModel.RecordRemoveAttribute(
            document, target, attributeName);
    }

    /// <summary>Records a future text replacement without applying it.</summary>
    public OdfOperation RecordSetTextOperation(
        OdfDocument document,
        OdfNode target,
        string value)
    {
        return ChangeTrackerModel.RecordSetText(document, target, value);
    }

    /// <summary>Appends a caller-created operation record.</summary>
    public OdfOperation RecordOperation(
        OdfDocument document,
        OdfOperation operation)
    {
        return ChangeTrackerModel.RecordOperation(document, operation);
    }

    /// <summary>Replays supplied operations in stable sequence order.</summary>
    public OdfEditResult ReplayOperations(
        OdfDocument document,
        OdfOperation[] operations)
    {
        return ChangeTrackerModel.ReplayOperations(document, operations);
    }

    /// <summary>Replays the document's recorded operation collection.</summary>
    public OdfEditResult ReplayRecordedOperations(OdfDocument document)
    {
        return ChangeTrackerModel.ReplayRecordedOperations(document);
    }

    /// <summary>Creates a reverse-order inverse operation collection.</summary>
    public OdfOperation[] InvertOperations(OdfOperation[] operations)
    {
        return OperationLogModel.InvertOperations(operations);
    }

    /// <summary>Exports recorded operations as non-standard extension XML.</summary>
    public byte[] ExportOperationsBytes(
        OdfDocument document,
        OdfSaveOptions options)
    {
        return ChangeTrackerModel.ExportOperations(document, options);
    }

    /// <summary>Imports non-standard operation-extension XML.</summary>
    public OdfOperation[] ImportOperationsBytes(
        byte[] data,
        OdfLoadOptions options)
    {
        return ChangeTrackerModel.ImportOperationsBytes(data, options);
    }
}
