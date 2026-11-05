using System.Text;

namespace odf.Tests;

public class EditSessionModelTests
{
    [Test]
    public void CommitAppliesOrderedStagedOperationsAndReportsMutations()
    {
        OdfDocument document = ReadText(
            "<text:p>First</text:p><text:p>Second</text:p>");
        OdfNode first = document.TextBlocks[0].Node!;
        OdfNode second = document.TextBlocks[1].Node!;
        OdfOperation later = OperationLogModel.CreateSetTextOperation(
            document, second, "Second changed", 20);
        later.Id = "later";
        OdfOperation earlier = OperationLogModel.CreateSetTextOperation(
            document, first, "First changed", 10);
        earlier.Id = "earlier";
        EditSessionState session = EditSessionModel.BeginSession(
            document, "session-1", CommitOptions(), 10);

        OdfEditResult stagedLater = EditSessionModel.StageOperation(
            session, later);
        OdfEditResult stagedEarlier = EditSessionModel.StageOperation(
            session, earlier);
        later.Value = "caller mutation";
        OdfOperation[] snapshot = EditSessionModel.GetStagedOperations(session);
        snapshot[0].Value = "snapshot mutation";
        OdfEditResult committed = EditSessionModel.CommitSession(session);

        Assert.Multiple(() =>
        {
            Assert.That(stagedLater.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(stagedLater.Changed, Is.False);
            Assert.That(stagedEarlier.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(NodeText(document, first), Is.EqualTo("First changed"));
            Assert.That(NodeText(document, second), Is.EqualTo("Second changed"));
            Assert.That(committed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(committed.Changed, Is.True);
            Assert.That(committed.AffectedCount, Is.EqualTo(2));
            Assert.That(committed.Mutations, Has.Length.EqualTo(2));
            Assert.That(committed.Mutations[0].OperationId, Is.EqualTo("earlier"));
            Assert.That(committed.Mutations[0].BeforeValue, Is.EqualTo("First"));
            Assert.That(committed.Mutations[0].AfterValue,
                Is.EqualTo("First changed"));
            Assert.That(committed.Mutations[1].OperationId, Is.EqualTo("later"));
            Assert.That(committed.Mutations[1].Location.XmlId,
                Is.EqualTo(later.TargetNodeId));
            Assert.That(document.Operations, Has.Length.EqualTo(2));
            Assert.That(document.Operations[0].SessionId, Is.EqualTo("session-1"));
            Assert.That(document.Operations[1].SessionId, Is.EqualTo("session-1"));
        });
    }

    [Test]
    public void CommitReturnsConflictWithoutChangingConcurrentContent()
    {
        OdfDocument document = ReadText("<text:p>Original</text:p>");
        OdfNode paragraph = document.TextBlocks[0].Node!;
        OdfOperation operation = OperationLogModel.CreateSetTextOperation(
            document, paragraph, "Staged", 1);
        operation.Id = "conflicting-operation";
        EditSessionState session = EditSessionModel.BeginSession(
            document, "conflict-session", CommitOptions(), 10);
        EditSessionModel.StageOperation(session, operation);
        new OdfTreeEditor(document.Parts[0].Nodes)
            .SetTextContent(paragraph, "Concurrent");

        OdfEditResult committed = EditSessionModel.CommitSession(session);
        OdfEditResult discarded = EditSessionModel.DiscardSession(session);

        Assert.Multiple(() =>
        {
            Assert.That(committed.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(committed.Changed, Is.False);
            Assert.That(committed.AffectedCount, Is.Zero);
            Assert.That(committed.Mutations, Is.Empty);
            Assert.That(committed.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(committed.Diagnostics[0].Code,
                Is.EqualTo(OdfDiagnosticCodes.EditConflict));
            Assert.That(NodeText(document, paragraph), Is.EqualTo("Concurrent"));
            Assert.That(document.Operations, Is.Empty);
            Assert.That(discarded.Status, Is.EqualTo(OdfEditStatus.Success));
        });
    }

    [Test]
    public void ValidationFailureRollsBackAndLeavesSessionDiscardable()
    {
        OdfDocument document = ReadText("<text:p>Content</text:p>");
        OdfNode root = document.Parts[0].Root!;
        OdfQualifiedName versionName = OfficeName("version");
        OdfOperation operation = OperationLogModel.CreateSetAttributeOperation(
            document, root, versionName, "not-a-version", 1);
        operation.Id = "invalid-version";
        EditSessionState session = EditSessionModel.BeginSession(
            document, "validation-session", XmlValidationOptions(), 10);
        EditSessionModel.StageOperation(session, operation);

        OdfEditResult committed = EditSessionModel.CommitSession(session);
        OdfOperation[] remaining = EditSessionModel.GetStagedOperations(session);
        OdfEditResult discarded = EditSessionModel.DiscardSession(session);

        Assert.Multiple(() =>
        {
            Assert.That(committed.Status,
                Is.EqualTo(OdfEditStatus.ValidationFailed));
            Assert.That(committed.Changed, Is.False);
            Assert.That(committed.AffectedCount, Is.Zero);
            Assert.That(committed.Diagnostics, Is.Not.Empty);
            Assert.That(committed.Mutations, Is.Empty);
            Assert.That(ReadAttribute(root, versionName), Is.EqualTo("1.4"));
            Assert.That(document.Operations, Is.Empty);
            Assert.That(remaining, Has.Length.EqualTo(1));
            Assert.That(discarded.Status, Is.EqualTo(OdfEditStatus.Success));
        });
    }

    [Test]
    public void ExpectedSessionConflictsAndCancellationReturnStatuses()
    {
        OdfDocument document = ReadText("<text:p>A</text:p>");
        OdfNode paragraph = document.TextBlocks[0].Node!;
        OdfOperation first = OperationLogModel.CreateSetTextOperation(
            document, paragraph, "B", 1);
        first.Id = "first";
        OdfOperation second = OperationLogModel.CreateSetTextOperation(
            document, paragraph, "C", 2);
        second.Id = "second";
        EditSessionState limited = EditSessionModel.BeginSession(
            document, "limited", CommitOptions(), 1);

        OdfEditResult staged = EditSessionModel.StageOperation(limited, first);
        OdfEditResult limit = EditSessionModel.StageOperation(limited, second);
        OdfEditResult discard = EditSessionModel.DiscardSession(limited);
        OdfEditResult afterDiscard = EditSessionModel.CommitSession(limited);

        OdfValidationOptions cancelledOptions = CommitOptions();
        cancelledOptions.Cancellation.IsCancellationRequested = true;
        cancelledOptions.Cancellation.Reason = "caller stopped";
        EditSessionState cancelled = EditSessionModel.BeginSession(
            document, "cancelled", cancelledOptions, 10);
        OdfEditResult cancellation = EditSessionModel.StageOperation(
            cancelled, second);

        Assert.Multiple(() =>
        {
            Assert.That(staged.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(limit.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(discard.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(discard.AffectedCount, Is.EqualTo(1));
            Assert.That(afterDiscard.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(cancellation.Status, Is.EqualTo(OdfEditStatus.Cancelled));
            Assert.That(cancellation.Message, Does.Contain("caller stopped"));
            Assert.That(NodeText(document, paragraph), Is.EqualTo("A"));
        });
    }

    [Test]
    public void EmptySessionCommitsAsNoChange()
    {
        OdfDocument document = ReadText("<text:p>A</text:p>");
        EditSessionState session = EditSessionModel.BeginSession(
            document, "empty", CommitOptions(), 10);

        OdfEditResult result = EditSessionModel.CommitSession(session);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.NoChange));
            Assert.That(result.Changed, Is.False);
            Assert.That(result.AffectedCount, Is.Zero);
            Assert.That(result.Mutations, Is.Empty);
        });
    }

    static OdfValidationOptions CommitOptions()
    {
        return new OdfValidationOptions
        {
            Mode = OdfValidationMode.PackageOnly,
            ValidateXml = false,
            ValidateSemantics = false,
            ValidateFormulas = false,
            ValidateSecurity = false
        };
    }

    static OdfValidationOptions XmlValidationOptions()
    {
        return new OdfValidationOptions
        {
            Mode = OdfValidationMode.XmlOnly,
            ValidatePackage = false,
            ValidateSemantics = false,
            ValidateFormulas = false,
            ValidateSecurity = false
        };
    }

    static OdfDocument ReadText(string body)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' xmlns:text='" + OdfNamespaces.Text
            + "' office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Text + "'><office:body><office:text>"
            + body + "</office:text></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "edit-session.fodt",
            new OdfLoadOptions());
    }

    static string NodeText(OdfDocument document, OdfNode node)
    {
        return new OdfTreeEditor(document.Parts[0].Nodes)
            .GetTextContent(node);
    }

    static OdfQualifiedName OfficeName(string localName)
    {
        return new OdfQualifiedName
        {
            NamespaceName = OdfNamespaces.Office,
            LocalName = localName,
            Prefix = OdfNamespaces.OfficePrefix
        };
    }

    static string ReadAttribute(OdfNode node, OdfQualifiedName name)
    {
        OdfAttribute? attribute = new OdfTreeEditor([node])
            .FindAttribute(node, name);
        return attribute == null ? "" : attribute.Value;
    }
}
