using System.Text;

namespace odf.Tests;

public class OdfChangeTrackerTests
{
    [Test]
    public void CoversEveryTextChangeKindProjectionAndBatchReview()
    {
        IOdfChangeTracker tracker = new OdfChangeTracker();
        OdfDocument selectedDocument = ReadText(AllTextChangesXml());
        OdfChange[] selectedChanges = tracker.GetTextChanges(selectedDocument);

        string current = tracker.ProjectVisibleText(
            selectedDocument, true, false);
        string original = tracker.ProjectVisibleText(
            selectedDocument, false, true);
        string combined = tracker.ProjectVisibleText(
            selectedDocument, true, true);
        int acceptedSelected = tracker.AcceptSelectedTextChanges(
            selectedDocument,
            [selectedChanges[0], selectedChanges[2]]);
        int rejectedRemaining = tracker.RejectAllTextChanges(selectedDocument);

        OdfDocument acceptedDocument = ReadText(AllTextChangesXml());
        int acceptedAll = tracker.AcceptAllTextChanges(acceptedDocument);
        OdfDocument rejectedDocument = ReadText(AllTextChangesXml());
        OdfChange[] rejectedChanges = tracker.GetTextChanges(rejectedDocument);
        int rejectedSelected = tracker.RejectSelectedTextChanges(
            rejectedDocument,
            [rejectedChanges[0], rejectedChanges[1], rejectedChanges[2]]);

        Assert.Multiple(() =>
        {
            Assert.That(selectedChanges.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    OdfChangeKind.Insertion,
                    OdfChangeKind.Deletion,
                    OdfChangeKind.FormatChange,
                    OdfChangeKind.Unknown
                }));
            Assert.That(current,
                Is.EqualTo("Before new words after \nstyled"));
            Assert.That(original,
                Is.EqualTo("Before  after removed\ncontent\nstyled"));
            Assert.That(combined,
                Is.EqualTo("Before new words after removed\ncontent\nstyled"));
            Assert.That(acceptedSelected, Is.EqualTo(2));
            Assert.That(rejectedRemaining, Is.EqualTo(1));
            Assert.That(tracker.GetTextChanges(selectedDocument),
                Has.Length.EqualTo(1));
            Assert.That(
                tracker.GetTextChanges(selectedDocument)[0].Kind,
                Is.EqualTo(OdfChangeKind.Unknown));
            Assert.That(acceptedAll, Is.EqualTo(3));
            Assert.That(tracker.GetTextChanges(acceptedDocument),
                Has.Length.EqualTo(1));
            Assert.That(rejectedSelected, Is.EqualTo(3));
            Assert.That(tracker.GetTextChanges(rejectedDocument),
                Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void ReportsAndRefusesABrokenTextChangeBoundary()
    {
        OdfDocument document = ReadText(BrokenTextChangeXml());
        IOdfChangeTracker tracker = new OdfChangeTracker();
        OdfChange change = tracker.GetTextChanges(document)[0];

        bool rejected = tracker.RejectTextChange(document, change);
        OdfDiagnostic[] diagnostics = new OdfValidator().ValidateDocument(
            document,
            new OdfValidationOptions
            {
                Mode = OdfValidationMode.SemanticOnly,
                Version = OdfVersion.Version14
            });

        Assert.Multiple(() =>
        {
            Assert.That(change.IsBoundaryResolved, Is.False);
            Assert.That(rejected, Is.False);
            Assert.That(diagnostics.Any(item =>
                    item.Code == OdfDiagnosticCodes.BrokenReference
                    && item.Message.Contains("boundaries")),
                Is.True);
        });
    }

    [Test]
    public void EnumeratesFiltersProjectsAndProcessesTextChanges()
    {
        OdfDocument document = ReadText(TextChangesXml());
        IOdfChangeTracker tracker = new OdfChangeTracker();

        OdfChange[] snapshot = tracker.GetTextChanges(document);
        OdfChange[] filtered = tracker.FilterTextChanges(
            document, "bob", "2026-09-02", "remove", false);
        string original = tracker.ProjectVisibleText(document, false, true);
        bool accepted = tracker.AcceptTextChange(document, snapshot[0]);
        OdfChange deletion = tracker.GetTextChanges(document)[0];
        bool rejected = tracker.RejectTextChange(document, deletion);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot, Has.Length.EqualTo(2));
            Assert.That(filtered, Has.Length.EqualTo(1));
            Assert.That(filtered[0].Kind, Is.EqualTo(OdfChangeKind.Deletion));
            Assert.That(original, Is.EqualTo("Before  after removed"));
            Assert.That(accepted, Is.True);
            Assert.That(rejected, Is.True);
            Assert.That(tracker.GetTextChanges(document), Is.Empty);
            Assert.That(tracker.ProjectVisibleText(document, true, false),
                Is.EqualTo("Before new after removed"));
        });
    }

    [Test]
    public void FiltersAndReviewsSpreadsheetChanges()
    {
        OdfDocument document = new OdfDocument();
        OdfSpreadsheetChange alice = SpreadsheetChange(
            "one", "Alice", "First value");
        OdfSpreadsheetChange bob = SpreadsheetChange(
            "two", "Bob", "Second value");
        document.SpreadsheetChanges = [alice, bob];
        IOdfChangeTracker tracker = new OdfChangeTracker();

        OdfSpreadsheetChange[] snapshot =
            tracker.GetSpreadsheetChanges(document);
        OdfSpreadsheetChange[] filtered = tracker.FilterSpreadsheetChanges(
            document, "alice", "2026-09", "first", false);
        int rejected = tracker.RejectSelectedSpreadsheetChanges(
            document, [bob], "review-1");
        int accepted = tracker.AcceptAllSpreadsheetChanges(document);
        snapshot[0] = new OdfSpreadsheetChange();

        Assert.Multiple(() =>
        {
            Assert.That(filtered, Has.Length.EqualTo(1));
            Assert.That(filtered[0], Is.SameAs(alice));
            Assert.That(rejected, Is.EqualTo(1));
            Assert.That(accepted, Is.EqualTo(2));
            Assert.That(document.SpreadsheetChanges[0], Is.SameAs(alice));
            Assert.That(alice.AcceptanceState, Is.EqualTo("accepted"));
            Assert.That(bob.AcceptanceState, Is.EqualTo("accepted"));
            Assert.That(bob.RejectingChangeId, Is.Empty);
        });
    }

    [Test]
    public void ReviewsEverySpreadsheetChangeKindThroughEveryScope()
    {
        IOdfChangeTracker tracker = new OdfChangeTracker();
        OdfDocument document = SpreadsheetChangeDocument();
        OdfSpreadsheetChange[] changes =
            tracker.GetSpreadsheetChanges(document);

        bool acceptedOne = tracker.AcceptSpreadsheetChange(
            document, changes[0]);
        bool rejectedOne = tracker.RejectSpreadsheetChange(
            document, changes[1], "review-one");
        int acceptedSelected = tracker.AcceptSelectedSpreadsheetChanges(
            document, [changes[2], changes[3]]);

        OdfDocument rejectedDocument = SpreadsheetChangeDocument();
        int rejectedAll = tracker.RejectAllSpreadsheetChanges(
            rejectedDocument, "review-all");

        Assert.Multiple(() =>
        {
            Assert.That(changes.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    "cell-content",
                    "insertion",
                    "deletion",
                    "movement"
                }));
            Assert.That(acceptedOne, Is.True);
            Assert.That(rejectedOne, Is.True);
            Assert.That(acceptedSelected, Is.EqualTo(2));
            Assert.That(changes[0].AcceptanceState, Is.EqualTo("accepted"));
            Assert.That(changes[1].AcceptanceState, Is.EqualTo("rejected"));
            Assert.That(changes[1].RejectingChangeId,
                Is.EqualTo("review-one"));
            Assert.That(changes[2].AcceptanceState, Is.EqualTo("accepted"));
            Assert.That(changes[3].AcceptanceState, Is.EqualTo("accepted"));
            Assert.That(rejectedAll, Is.EqualTo(4));
            Assert.That(rejectedDocument.SpreadsheetChanges.All(item =>
                    item.AcceptanceState == "rejected"
                    && item.RejectingChangeId == "review-all"),
                Is.True);
        });
    }

    [Test]
    public void RecordsExportsImportsReplaysAndInvertsOperations()
    {
        OdfDocument document = ReadText(DocumentXml("<text:p>A</text:p>"));
        IOdfChangeTracker tracker = new OdfChangeTracker();
        OdfOperation recorded = tracker.RecordSetTextOperation(
            document, document.TextBlocks[0].Node!, "B");
        recorded.Author = "Ada";
        recorded.SessionId = "session-1";
        recorded.Date = "2026-09-02T12:00:00Z";

        OdfOperation[] snapshot = tracker.GetOperations(document);
        byte[] exported = tracker.ExportOperationsBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        OdfOperation[] imported = tracker.ImportOperationsBytes(
            exported, new OdfLoadOptions());
        snapshot[0] = new OdfOperation();
        OdfEditResult replay = tracker.ReplayOperations(document, imported);
        OdfOperation[] inverse = tracker.InvertOperations(imported);
        OdfEditResult undo = tracker.ReplayOperations(document, inverse);

        Assert.Multiple(() =>
        {
            Assert.That(recorded.Sequence, Is.Zero);
            Assert.That(recorded.Id, Is.Not.Empty);
            Assert.That(tracker.GetOperations(document)[0],
                Is.SameAs(recorded));
            Assert.That(Encoding.UTF8.GetString(exported),
                Does.Contain("operation-log"));
            Assert.That(imported, Has.Length.EqualTo(1));
            Assert.That(imported[0].Author, Is.EqualTo("Ada"));
            Assert.That(replay.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(undo.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(document.TextBlocks[0].Text, Is.EqualTo("A"));
        });
    }

    [Test]
    public void ReplaysTheRecordedOperationCollection()
    {
        OdfDocument document = ReadText(DocumentXml("<text:p>A</text:p>"));
        IOdfChangeTracker tracker = new OdfChangeTracker();
        tracker.RecordSetTextOperation(
            document, document.TextBlocks[0].Node!, "changed");

        OdfEditResult result = tracker.ReplayRecordedOperations(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(result.AffectedCount, Is.EqualTo(1));
            Assert.That(document.TextBlocks[0].Text, Is.EqualTo("changed"));
        });
    }

    [Test]
    public void RecordsReplaysAndInvertsEveryOperationKind()
    {
        IOdfChangeTracker tracker = new OdfChangeTracker();

        OdfDocument insertDocument = ReadText(
            DocumentXml("<text:p>A</text:p>"));
        OdfNode insertParent = insertDocument.TextBlocks[0].Node!.Parent!;
        OdfNode payload = ReadText(
            DocumentXml("<text:p>Inserted</text:p>"))
            .TextBlocks[0].Node!;
        OdfOperation insert = tracker.RecordInsertNodeOperation(
            insertDocument,
            insertParent,
            insertParent.Children.Length,
            payload);
        AssertOperationRoundTrip(
            tracker, insertDocument, insert, OdfOperationKind.InsertNode,
            ["A", "Inserted"], ["A"]);

        OdfDocument removeDocument = ReadText(
            DocumentXml("<text:p>A</text:p><text:p>Removed</text:p>"));
        OdfOperation remove = tracker.RecordRemoveNodeOperation(
            removeDocument, removeDocument.TextBlocks[1].Node!);
        AssertOperationRoundTrip(
            tracker, removeDocument, remove, OdfOperationKind.RemoveNode,
            ["A"], ["A", "Removed"]);

        OdfDocument moveDocument = ReadText(
            DocumentXml("<text:p>A</text:p><text:p>Moved</text:p>"));
        OdfNode moveParent = moveDocument.TextBlocks[0].Node!.Parent!;
        OdfOperation move = tracker.RecordMoveNodeOperation(
            moveDocument,
            moveDocument.TextBlocks[1].Node!,
            moveParent,
            0);
        AssertOperationRoundTrip(
            tracker, moveDocument, move, OdfOperationKind.MoveNode,
            ["Moved", "A"], ["A", "Moved"]);

        OdfQualifiedName styleName = new OdfQualifiedName
        {
            NamespaceName = OdfNamespaces.Text,
            LocalName = "style-name",
            Prefix = "text"
        };
        OdfDocument setAttributeDocument = ReadText(
            DocumentXml("<text:p>A</text:p>"));
        OdfOperation setAttribute = tracker.RecordSetAttributeOperation(
            setAttributeDocument,
            setAttributeDocument.TextBlocks[0].Node!,
            styleName,
            "Emphasis");
        OdfEditResult setResult = tracker.ReplayOperations(
            setAttributeDocument, [setAttribute]);
        Assert.That(AttributeValue(
            setAttributeDocument.TextBlocks[0].Node!, styleName),
            Is.EqualTo("Emphasis"));
        OdfEditResult unsetResult = tracker.ReplayOperations(
            setAttributeDocument, tracker.InvertOperations([setAttribute]));

        OdfDocument removeAttributeDocument = ReadText(
            DocumentXml("<text:p text:style-name='Original'>A</text:p>"));
        OdfOperation removeAttribute = tracker.RecordRemoveAttributeOperation(
            removeAttributeDocument,
            removeAttributeDocument.TextBlocks[0].Node!,
            styleName);
        OdfEditResult removeResult = tracker.ReplayOperations(
            removeAttributeDocument, [removeAttribute]);
        Assert.That(AttributeValue(
            removeAttributeDocument.TextBlocks[0].Node!, styleName),
            Is.Empty);
        OdfEditResult restoreResult = tracker.ReplayOperations(
            removeAttributeDocument,
            tracker.InvertOperations([removeAttribute]));

        OdfDocument callerDocument = ReadText(
            DocumentXml("<text:p>Before</text:p>"));
        OdfOperation callerOperation = tracker.RecordSetTextOperation(
            callerDocument,
            callerDocument.TextBlocks[0].Node!,
            "After");
        callerDocument.Operations = [];
        callerOperation.Id = "";
        OdfOperation appended = tracker.RecordOperation(
            callerDocument, callerOperation);
        OdfEditResult callerReplay = tracker.ReplayRecordedOperations(
            callerDocument);
        OdfEditResult callerUndo = tracker.ReplayOperations(
            callerDocument, tracker.InvertOperations([appended]));

        Assert.Multiple(() =>
        {
            Assert.That(setAttribute.Kind,
                Is.EqualTo(OdfOperationKind.SetAttribute));
            Assert.That(setResult.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(unsetResult.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(AttributeValue(
                setAttributeDocument.TextBlocks[0].Node!, styleName),
                Is.Empty);
            Assert.That(removeAttribute.Kind,
                Is.EqualTo(OdfOperationKind.RemoveAttribute));
            Assert.That(removeResult.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(restoreResult.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(AttributeValue(
                removeAttributeDocument.TextBlocks[0].Node!, styleName),
                Is.EqualTo("Original"));
            Assert.That(appended.Kind, Is.EqualTo(OdfOperationKind.SetText));
            Assert.That(appended.Id, Is.Not.Empty);
            Assert.That(tracker.GetOperations(callerDocument),
                Has.Length.EqualTo(1));
            Assert.That(callerReplay.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(callerUndo.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(callerDocument.TextBlocks[0].Text,
                Is.EqualTo("Before"));
        });
    }

    static OdfSpreadsheetChange SpreadsheetChange(
        string id,
        string creator,
        string comment)
    {
        return new OdfSpreadsheetChange
        {
            Id = id,
            Creator = creator,
            Comment = comment,
            Date = "2026-09-02"
        };
    }

    static OdfDocument SpreadsheetChangeDocument()
    {
        OdfDocument result = new OdfDocument();
        result.SpreadsheetChanges =
        [
            SpreadsheetChangeOfKind("cell", "cell-content"),
            SpreadsheetChangeOfKind("insert", "insertion"),
            SpreadsheetChangeOfKind("delete", "deletion"),
            SpreadsheetChangeOfKind("move", "movement")
        ];
        return result;
    }

    static OdfSpreadsheetChange SpreadsheetChangeOfKind(
        string id,
        string kind)
    {
        OdfSpreadsheetChange result = SpreadsheetChange(
            id, "Ada", "Tracked " + kind);
        result.Kind = kind;
        return result;
    }

    static void AssertOperationRoundTrip(
        IOdfChangeTracker tracker,
        OdfDocument document,
        OdfOperation operation,
        OdfOperationKind expectedKind,
        string[] replayedText,
        string[] restoredText)
    {
        OdfEditResult replay = tracker.ReplayOperations(
            document, [operation]);
        string[] actualReplay = document.TextBlocks
            .Select(item => item.Text).ToArray();
        OdfOperation[] inverse = tracker.InvertOperations([operation]);
        OdfEditResult undo = tracker.ReplayOperations(document, inverse);
        string[] actualRestore = document.TextBlocks
            .Select(item => item.Text).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(operation.Kind, Is.EqualTo(expectedKind));
            Assert.That(replay.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(actualReplay, Is.EqualTo(replayedText));
            Assert.That(inverse, Has.Length.EqualTo(1));
            Assert.That(undo.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(actualRestore, Is.EqualTo(restoredText));
        });
    }

    static string AttributeValue(
        OdfNode node,
        OdfQualifiedName name)
    {
        OdfAttribute? result = node.Attributes.FirstOrDefault(item =>
            item.Name.NamespaceName == name.NamespaceName
            && item.Name.LocalName == name.LocalName);
        return result == null ? "" : result.Value;
    }

    static string AllTextChangesXml()
    {
        string info = ChangeInfo(
            "Alice", "2026-09-01T12:00:00Z", "Tracked change");
        string changes = "<text:tracked-changes>"
            + "<text:changed-region xml:id='insert'><text:insertion>"
            + info + "</text:insertion></text:changed-region>"
            + "<text:changed-region xml:id='delete'><text:deletion>"
            + info + "<text:p>removed</text:p><text:p>content</text:p>"
            + "</text:deletion></text:changed-region>"
            + "<text:changed-region xml:id='format'><text:format-change>"
            + info + "</text:format-change></text:changed-region>"
            + "<text:changed-region xml:id='unknown'><ext:replacement>"
            + info + "</ext:replacement></text:changed-region>"
            + "</text:tracked-changes>";
        string body = changes
            + "<text:p>Before <text:change-start text:change-id='insert'/>"
            + "new <text:span>words</text:span>"
            + "<text:change-end text:change-id='insert'/> after "
            + "<text:change text:change-id='delete'/></text:p>"
            + "<text:p><text:change-start text:change-id='format'/>"
            + "styled<text:change-end text:change-id='format'/></text:p>";
        return DocumentXml(body);
    }

    static string BrokenTextChangeXml()
    {
        string info = ChangeInfo(
            "Alice", "2026-09-01T12:00:00Z", "Broken range");
        string changes = "<text:tracked-changes>"
            + "<text:changed-region xml:id='broken'><text:insertion>"
            + info + "</text:insertion></text:changed-region>"
            + "</text:tracked-changes>";
        return DocumentXml(changes
            + "<text:p><text:change-start text:change-id='broken'/>"
            + "unclosed</text:p>");
    }

    static string TextChangesXml()
    {
        string alice = ChangeInfo(
            "Alice", "2026-09-01T12:00:00Z", "Inserted");
        string bob = ChangeInfo(
            "Bob", "2026-09-02T12:00:00Z", "Remove this");
        string changes = "<text:tracked-changes>"
            + "<text:changed-region xml:id='insert'><text:insertion>"
            + alice + "</text:insertion></text:changed-region>"
            + "<text:changed-region xml:id='delete'><text:deletion>"
            + bob + "<text:p>removed</text:p></text:deletion>"
            + "</text:changed-region></text:tracked-changes>";
        string body = changes
            + "<text:p>Before <text:change-start text:change-id='insert'/>"
            + "new<text:change-end text:change-id='insert'/> after "
            + "<text:change text:change-id='delete'/></text:p>";
        return DocumentXml(body);
    }

    static string ChangeInfo(
        string creator,
        string date,
        string comment)
    {
        return "<office:change-info><dc:creator>" + creator
            + "</dc:creator><dc:date>" + date + "</dc:date><text:p>"
            + comment + "</text:p></office:change-info>";
    }

    static string DocumentXml(string body)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' xmlns:text='" + OdfNamespaces.Text
            + "' xmlns:dc='" + OdfNamespaces.DublinCore
            + "' xmlns:ext='urn:change-extension"
            + "' office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Text + "'><office:body><office:text>"
            + body + "</office:text></office:body></office:document>";
    }

    static OdfDocument ReadText(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "tracker.fodt",
            new OdfLoadOptions());
    }
}
