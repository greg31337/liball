using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class UpdateBatchCoreTests
{
    [Test]
    public void AcknowledgementCommitsOneRevisionedDetachedTransaction()
    {
        UpdateBatchCore core = new UpdateBatchCore();
        DomSnapshot before = Snapshot(5, "before-hash", "Before");
        DomSnapshot after = Snapshot(91, "after-hash", "After");

        UiUpdateResult initialized = core.InitializeUpdateHistory(before, 4);
        UpdateBatchOperationResult created = core.BeginUpdateTransaction(after, UiUpdatePriority.UserInteraction, null);
        string batchId = created.Batch!.Id;
        after.Roots[0].Children[0].Text = "Mutated after begin";
        UiUpdateResult acknowledged = core.AcknowledgeUpdateTransaction(batchId, 6);
        UiUpdateResult repeated = core.AcknowledgeUpdateTransaction(batchId, 6);
        DomSnapshot committed = core.ReadCurrentUpdateSnapshot()!;

        Assert.Multiple(() =>
        {
            Assert.That(initialized.Accepted, Is.True);
            Assert.That(initialized.Revision, Is.EqualTo(5));
            Assert.That(created.Success, Is.True);
            Assert.That(created.IsReplay, Is.False);
            Assert.That(created.Batch.BaseRevision, Is.EqualTo(5));
            Assert.That(created.Batch.Revision, Is.EqualTo(6));
            Assert.That(created.Batch.Priority, Is.EqualTo(UiUpdatePriority.UserInteraction));
            Assert.That(created.Batch.Mutations.Single().Kind, Is.EqualTo(UiMutationKind.SetText));
            Assert.That(created.Batch.Mutations.Single().Value, Is.EqualTo("After"));
            Assert.That(acknowledged.Accepted, Is.True);
            Assert.That(acknowledged.Revision, Is.EqualTo(6));
            Assert.That(repeated.Accepted, Is.True);
            Assert.That(repeated.Revision, Is.EqualTo(6));
            Assert.That(committed.Revision, Is.EqualTo(6));
            Assert.That(committed.Roots[0].Children[0].Text, Is.EqualTo("After"));
            Assert.That(core.Pending, Is.Null);
            Assert.That(core.History, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void PendingTransactionsReplayIdempotentlyAndRejectNestedUpdates()
    {
        UpdateBatchCore core = InitializedCore(3);
        DomSnapshot after = Snapshot(0, "next-hash", "Next");

        UpdateBatchOperationResult first = core.BeginUpdateTransaction(after, UiUpdatePriority.Normal, null);
        UpdateBatchOperationResult replay = core.BeginUpdateTransaction(Snapshot(77, "next-hash", "Ignored equivalent content"), UiUpdatePriority.Background, null);
        UpdateBatchOperationResult nested = core.BeginUpdateTransaction(Snapshot(0, "other-hash", "Other"), UiUpdatePriority.Normal, null);
        UiUpdateResult wrongRevision = core.AcknowledgeUpdateTransaction(first.Batch!.Id, 99);
        UiUpdateResult accepted = core.AcknowledgeUpdateTransaction(first.Batch.Id, 4);

        Assert.Multiple(() =>
        {
            Assert.That(replay.Success, Is.True);
            Assert.That(replay.IsReplay, Is.True);
            Assert.That(replay.Batch!.Id, Is.EqualTo(first.Batch.Id));
            Assert.That(replay.Batch.Priority, Is.EqualTo(UiUpdatePriority.Normal));
            Assert.That(nested.Success, Is.False);
            Assert.That(nested.Code, Is.EqualTo("update.transaction-active"));
            Assert.That(nested.Result.RequiresResync, Is.False);
            Assert.That(wrongRevision.Accepted, Is.False);
            Assert.That(wrongRevision.Diagnostics.Single().Code, Is.EqualTo("update.revision-mismatch"));
            Assert.That(wrongRevision.RequiresResync, Is.True);
            Assert.That(wrongRevision.Snapshot!.Revision, Is.EqualTo(3));
            Assert.That(accepted.Accepted, Is.True);
        });
    }

    [Test]
    public void RejectionRollsBackAndReturnsDetachedResynchronizationSnapshots()
    {
        UpdateBatchCore core = InitializedCore(10);
        UpdateBatchOperationResult created = core.BeginUpdateTransaction(Snapshot(0, "rejected-hash", "Rejected"), UiUpdatePriority.Immediate, null);

        UiUpdateResult rejected = core.RejectUpdateTransaction(created.Batch!.Id, 11);
        rejected.Snapshot!.Roots[0].Children[0].Text = "Mutated recovery";
        UiUpdateResult repeated = core.RejectUpdateTransaction(created.Batch.Id, 11);
        DomSnapshot current = core.ReadCurrentUpdateSnapshot()!;
        UpdateBatchOperationResult next = core.BeginUpdateTransaction(Snapshot(0, "accepted-hash", "Accepted"), UiUpdatePriority.Normal, null);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.RequiresResync, Is.True);
            Assert.That(rejected.Revision, Is.EqualTo(10));
            Assert.That(rejected.Diagnostics.Single().Code, Is.EqualTo("update.batch-rejected"));
            Assert.That(rejected.Result.Success, Is.False);
            Assert.That(repeated.Diagnostics.Single().Code, Is.EqualTo("update.batch-rejected"));
            Assert.That(repeated.Snapshot!.Roots[0].Children[0].Text, Is.EqualTo("Initial"));
            Assert.That(current.Revision, Is.EqualTo(10));
            Assert.That(current.Roots[0].Children[0].Text, Is.EqualTo("Initial"));
            Assert.That(next.Batch!.BaseRevision, Is.EqualTo(10));
            Assert.That(next.Batch.Revision, Is.EqualTo(11));
            Assert.That(next.Batch.Id, Is.Not.EqualTo(created.Batch.Id));
        });
    }

    [Test]
    public void ReplayValidatesBaseRevisionAndBoundedHistoryEvictsOldBatches()
    {
        UpdateBatchCore core = new UpdateBatchCore();
        core.InitializeUpdateHistory(Snapshot(0, "initial-hash", "Initial"), 2);
        string[] ids = new string[3];
        int index = 0;
        while (index < ids.Length)
        {
            int revision = index + 1;
            UpdateBatchOperationResult created = core.BeginUpdateTransaction(Snapshot(0, "hash-" + revision, "Value " + revision), UiUpdatePriority.Normal, null);
            ids[index] = created.Batch!.Id;
            core.AcknowledgeUpdateTransaction(created.Batch.Id, revision);
            index++;
        }

        UpdateBatchOperationResult evicted = core.ReplayUpdateTransaction(ids[0], 0);
        UpdateBatchOperationResult stale = core.ReplayUpdateTransaction(ids[2], 1);
        UpdateBatchOperationResult replay = core.ReplayUpdateTransaction(ids[2], 2);
        replay.Batch!.Mutations[0].Value = "Mutated replay";
        UpdateBatchOperationResult repeated = core.ReplayUpdateTransaction(ids[2], 2);

        Assert.Multiple(() =>
        {
            Assert.That(core.History, Has.Count.EqualTo(2));
            Assert.That(evicted.Success, Is.False);
            Assert.That(evicted.Code, Is.EqualTo("update.unknown-batch"));
            Assert.That(evicted.Result.RequiresResync, Is.True);
            Assert.That(evicted.Result.Snapshot!.Revision, Is.EqualTo(3));
            Assert.That(stale.Code, Is.EqualTo("update.stale-base"));
            Assert.That(replay.Success, Is.True);
            Assert.That(replay.IsReplay, Is.True);
            Assert.That(repeated.Batch!.Mutations[0].Value, Is.EqualTo("Value 3"));
        });
    }

    [Test]
    public void AcceptedAndRejectedHistoryRemainIdempotentAndMutuallyExclusive()
    {
        UpdateBatchCore core = InitializedCore(0);
        UpdateBatchOperationResult acceptedBatch = core.BeginUpdateTransaction(Snapshot(0, "accepted-hash", "Accepted"), UiUpdatePriority.Normal, null);
        UiUpdateResult accepted = core.AcknowledgeUpdateTransaction(acceptedBatch.Batch!.Id, 1);
        UiUpdateResult acceptedAgain = core.AcknowledgeUpdateTransaction(acceptedBatch.Batch.Id, 1);
        UiUpdateResult rejectAccepted = core.RejectUpdateTransaction(acceptedBatch.Batch.Id, 1);
        UpdateBatchOperationResult acceptedReplay = core.ReplayUpdateTransaction(acceptedBatch.Batch.Id, 0);
        UpdateBatchOperationResult rejectedBatch = core.BeginUpdateTransaction(Snapshot(0, "rejected-hash", "Rejected"), UiUpdatePriority.Normal, null);
        UiUpdateResult rejected = core.RejectUpdateTransaction(rejectedBatch.Batch!.Id, 2);
        UiUpdateResult rejectedAgain = core.RejectUpdateTransaction(rejectedBatch.Batch.Id, 2);
        UiUpdateResult acceptRejected = core.AcknowledgeUpdateTransaction(rejectedBatch.Batch.Id, 2);
        UpdateBatchOperationResult rejectedReplay = core.ReplayUpdateTransaction(rejectedBatch.Batch.Id, 1);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(acceptedAgain.Accepted, Is.True);
            Assert.That(rejectAccepted.Diagnostics.Single().Code, Is.EqualTo("update.batch-acknowledged"));
            Assert.That(acceptedReplay.Success, Is.True);
            Assert.That(acceptedReplay.IsReplay, Is.True);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Diagnostics.Single().Code, Is.EqualTo("update.batch-rejected"));
            Assert.That(rejectedAgain.Diagnostics.Single().Code, Is.EqualTo("update.batch-rejected"));
            Assert.That(acceptRejected.Diagnostics.Single().Code, Is.EqualTo("update.batch-rejected"));
            Assert.That(rejectedReplay.Success, Is.False);
            Assert.That(rejectedReplay.Code, Is.EqualTo("update.batch-rejected"));
            Assert.That(core.ReadCurrentUpdateSnapshot()!.Revision, Is.EqualTo(1));
            Assert.That(core.ReadCurrentUpdateSnapshot()!.Roots[0].Children[0].Text, Is.EqualTo("Accepted"));
            Assert.That(core.History, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void StaleReplayReturnsDetachedCurrentRecoverySnapshots()
    {
        UpdateBatchCore core = InitializedCore(3);
        UpdateBatchOperationResult created = core.BeginUpdateTransaction(Snapshot(0, "accepted-hash", "Accepted"), UiUpdatePriority.Normal, null);
        core.AcknowledgeUpdateTransaction(created.Batch!.Id, 4);

        UpdateBatchOperationResult stale = core.ReplayUpdateTransaction(created.Batch.Id, 2);
        stale.Result.Snapshot!.Roots[0].Children[0].Text = "Mutated recovery";
        UpdateBatchOperationResult repeated = core.ReplayUpdateTransaction(created.Batch.Id, 2);
        UpdateBatchOperationResult unknown = core.ReplayUpdateTransaction("missing-batch", 4);

        Assert.Multiple(() =>
        {
            Assert.That(stale.Success, Is.False);
            Assert.That(stale.Code, Is.EqualTo("update.stale-base"));
            Assert.That(stale.Result.RequiresResync, Is.True);
            Assert.That(stale.Result.Revision, Is.EqualTo(4));
            Assert.That(repeated.Result.Snapshot!.Roots[0].Children[0].Text, Is.EqualTo("Accepted"));
            Assert.That(unknown.Code, Is.EqualTo("update.unknown-batch"));
            Assert.That(unknown.Result.RequiresResync, Is.True);
            Assert.That(unknown.Result.Snapshot!.Revision, Is.EqualTo(4));
            Assert.That(unknown.Result.Snapshot.Roots[0].Children[0].Text, Is.EqualTo("Accepted"));
        });
    }

    [Test]
    public void PreservationMetadataSurvivesPendingAndHistoricalReplayAsDetachedData()
    {
        UpdateBatchCore core = new UpdateBatchCore();
        core.InitializeUpdateHistory(PreservationSnapshot(6, "before-hash", "Before"), 4);
        UiPreservationState preservation = new UiPreservationState
        {
            FocusedNodeId = "field",
            HasSelection = true,
            SelectionStart = 2,
            SelectionEnd = 5,
            IsComposing = true,
            ScrollOffsets = new UiValueEntry[] { Entry("scroller", "scroll-value") },
            ScrollAnchorNodeId = "scroller",
            MediaPlaybackStates = new UiValueEntry[] { Entry("video", "media-value") },
            OpenOverlayIds = new string[] { "dialog" },
            UserEditedFieldValues = new UiValueEntry[] { Entry("field", "typed-value") }
        };

        UpdateBatchOperationResult created = core.BeginUpdateTransaction(PreservationSnapshot(99, "after-hash", "After"), UiUpdatePriority.UserInteraction, preservation);
        preservation.ScrollOffsets[0].Value.StringValue = "mutated source";
        created.Batch!.Preservation.MediaPlaybackStates[0].Value.StringValue = "mutated result";
        UpdateBatchOperationResult pendingReplay = core.ReplayUpdateTransaction(created.Batch.Id, 6);
        core.AcknowledgeUpdateTransaction(created.Batch.Id, 7);
        pendingReplay.Batch!.Preservation.UserEditedFieldValues[0].Value.StringValue = "mutated replay";
        UpdateBatchOperationResult historicalReplay = core.ReplayUpdateTransaction(created.Batch.Id, 6);
        UiPreservationState actual = historicalReplay.Batch!.Preservation;

        Assert.Multiple(() =>
        {
            Assert.That(actual.PreserveFocus, Is.True);
            Assert.That(actual.FocusedNodeId, Is.EqualTo("field"));
            Assert.That(actual.PreserveSelection, Is.True);
            Assert.That(actual.HasSelection, Is.True);
            Assert.That(actual.SelectionStart, Is.EqualTo(2));
            Assert.That(actual.SelectionEnd, Is.EqualTo(5));
            Assert.That(actual.PreserveComposition, Is.True);
            Assert.That(actual.IsComposing, Is.True);
            Assert.That(actual.PreserveScroll, Is.True);
            Assert.That(actual.ScrollOffsets.Single().Value.StringValue, Is.EqualTo("scroll-value"));
            Assert.That(actual.PreserveScrollAnchor, Is.True);
            Assert.That(actual.ScrollAnchorNodeId, Is.EqualTo("scroller"));
            Assert.That(actual.PreserveMediaPlayback, Is.True);
            Assert.That(actual.MediaPlaybackStates.Single().Value.StringValue, Is.EqualTo("media-value"));
            Assert.That(actual.PreserveOpenOverlays, Is.True);
            Assert.That(actual.OpenOverlayIds, Is.EqualTo(new string[] { "dialog" }));
            Assert.That(actual.PreserveUserEditedFields, Is.True);
            Assert.That(actual.UserEditedFieldValues.Single().Value.StringValue, Is.EqualTo("typed-value"));
            Assert.That(historicalReplay.IsReplay, Is.True);
        });
    }

    [Test]
    public void InitializationAndTransactionValidationLeaveStateUnchanged()
    {
        UpdateBatchCore core = new UpdateBatchCore();
        UiUpdateResult missing = core.InitializeUpdateHistory(null, 2);
        UiUpdateResult invalidLimit = core.InitializeUpdateHistory(Snapshot(1, "hash", "Initial"), 0);
        UiUpdateResult initialized = core.InitializeUpdateHistory(Snapshot(int.MaxValue, "hash", "Initial"), 2);
        UpdateBatchOperationResult invalidPriority = core.BeginUpdateTransaction(Snapshot(0, "next", "Next"), (UiUpdatePriority)99, null);
        UpdateBatchOperationResult wrongSession = core.BeginUpdateTransaction(Snapshot(0, "next", "Next", "other-session", "seed"), UiUpdatePriority.Normal, null);
        UpdateBatchOperationResult wrongSeed = core.BeginUpdateTransaction(Snapshot(0, "next", "Next", "session", "other-seed"), UiUpdatePriority.Normal, null);
        UpdateBatchOperationResult exhausted = core.BeginUpdateTransaction(Snapshot(0, "next", "Next"), UiUpdatePriority.Normal, null);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Diagnostics.Single().Code, Is.EqualTo("update.null-snapshot"));
            Assert.That(invalidLimit.Diagnostics.Single().Code, Is.EqualTo("update.invalid-history-limit"));
            Assert.That(initialized.Accepted, Is.True);
            Assert.That(invalidPriority.Code, Is.EqualTo("update.invalid-priority"));
            Assert.That(wrongSession.Code, Is.EqualTo("update.session-mismatch"));
            Assert.That(wrongSession.Result.RequiresResync, Is.True);
            Assert.That(wrongSeed.Code, Is.EqualTo("update.root-seed-mismatch"));
            Assert.That(exhausted.Code, Is.EqualTo("update.revision-exhausted"));
            Assert.That(core.Pending, Is.Null);
            Assert.That(core.ReadCurrentUpdateSnapshot()!.Revision, Is.EqualTo(int.MaxValue));
        });
    }

    [Test]
    public void EquivalentInputsCreateDeterministicBatchIdentifiersAndDetachedPreservation()
    {
        UpdateBatchCore firstCore = InitializedCore(1);
        UpdateBatchCore secondCore = InitializedCore(1);
        UiPreservationState preservation = new UiPreservationState
        {
            FocusedNodeId = "root",
            OpenOverlayIds = new string[] { "dialog" }
        };

        UpdateBatchOperationResult first = firstCore.BeginUpdateTransaction(Snapshot(0, "same-next", "Next"), UiUpdatePriority.Background, preservation);
        UpdateBatchOperationResult second = secondCore.BeginUpdateTransaction(Snapshot(0, "same-next", "Next"), UiUpdatePriority.Background, preservation);
        preservation.OpenOverlayIds[0] = "mutated";
        first.Batch!.Preservation.OpenOverlayIds[0] = "mutated batch";
        UpdateBatchOperationResult replay = firstCore.ReplayUpdateTransaction(first.Batch.Id, 1);

        Assert.Multiple(() =>
        {
            Assert.That(first.Batch.Id, Is.EqualTo(second.Batch!.Id));
            Assert.That(first.Batch.Id, Does.StartWith("update-"));
            Assert.That(first.Batch.Id, Has.Length.EqualTo(31));
            Assert.That(replay.Batch!.Preservation.OpenOverlayIds[0], Is.EqualTo("dialog"));
        });
    }

    private static UpdateBatchCore InitializedCore(int revision)
    {
        UpdateBatchCore core = new UpdateBatchCore();
        core.InitializeUpdateHistory(Snapshot(revision, "initial-hash", "Initial"), 4);
        return core;
    }

    private static DomSnapshot Snapshot(int revision, string hash, string text)
    {
        return Snapshot(revision, hash, text, "session", "seed");
    }

    private static DomSnapshot Snapshot(int revision, string hash, string text, string session, string rootSeed)
    {
        DomNode child = new DomNode
        {
            Kind = DomNodeKind.Text,
            Key = "text",
            Id = "text",
            Text = text,
            SubtreeHash = hash + "-text"
        };
        DomNode root = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "root",
            Id = "root",
            Name = HtmlElements.Division,
            Children = new DomNode[] { child },
            SubtreeHash = hash + "-root"
        };
        return new DomSnapshot
        {
            SessionId = session,
            RootSeed = rootSeed,
            Revision = revision,
            Roots = new DomNode[] { root },
            ContentHash = hash
        };
    }

    private static DomSnapshot PreservationSnapshot(int revision, string hash, string text)
    {
        DomNode field = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "field",
            Id = "field",
            Name = HtmlElements.Input,
            Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Text } },
            SubtreeHash = "field-hash"
        };
        DomNode scroller = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "scroller",
            Id = "scroller",
            Name = HtmlElements.Division,
            SubtreeHash = "scroller-hash"
        };
        DomNode video = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "video",
            Id = "video",
            Name = HtmlElements.Video,
            Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Source, Value = "video.mp4" } },
            SubtreeHash = "video-hash"
        };
        DomNode dialog = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "dialog",
            Id = "dialog",
            Name = HtmlElements.Dialog,
            SubtreeHash = "dialog-hash"
        };
        DomNode label = new DomNode
        {
            Kind = DomNodeKind.Text,
            Key = "label",
            Id = "label",
            Text = text,
            SubtreeHash = hash + "-label"
        };
        DomNode root = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "root",
            Id = "root",
            Name = HtmlElements.Division,
            Children = new DomNode[] { field, scroller, video, dialog, label },
            SubtreeHash = hash + "-root"
        };
        return new DomSnapshot
        {
            SessionId = "session",
            RootSeed = "seed",
            Revision = revision,
            Roots = new DomNode[] { root },
            ContentHash = hash
        };
    }

    private static UiValueEntry Entry(string key, string value)
    {
        return new UiValueEntry
        {
            Key = key,
            Value = new UiValue { Kind = UiValueKind.String, StringValue = value }
        };
    }
}
