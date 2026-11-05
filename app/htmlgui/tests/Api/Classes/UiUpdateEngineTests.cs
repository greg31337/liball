using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class UiUpdateEngineTests
{
    [Test]
    public void CreateDomSnapshotCanonicalizesAndDetachesRoots()
    {
        DomNode text = new DomNode { Kind = DomNodeKind.Text, Text = "Initial" };
        DomNode root = new DomNode
        {
            Kind = DomNodeKind.Element,
            Name = HtmlElements.Division,
            Children = new DomNode[] { text }
        };
        IUiUpdateEngine engine = new UiUpdateEngine();

        DomSnapshot snapshot = engine.CreateDomSnapshot(new DomNode[] { root }, "session", "seed", 4);
        text.Text = "Mutated";
        root.Children = System.Array.Empty<DomNode>();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.SessionId, Is.EqualTo("session"));
            Assert.That(snapshot.RootSeed, Is.EqualTo("seed"));
            Assert.That(snapshot.Revision, Is.EqualTo(4));
            Assert.That(snapshot.ContentHash, Is.Not.Empty);
            Assert.That(snapshot.Roots[0].Id, Is.Not.Empty);
            Assert.That(snapshot.Roots[0].Key, Is.EqualTo("$element-0"));
            Assert.That(snapshot.Roots[0].Children[0].Text, Is.EqualTo("Initial"));
            Assert.That(snapshot.Roots[0].Children[0].Sequence, Is.EqualTo(1));
        });
    }

    [Test]
    public void ComputeAndAcceptCommitOneDetachedRevisionTransaction()
    {
        IUiUpdateEngine engine = new UiUpdateEngine();
        DomSnapshot before = Snapshot(engine, 7, "Before");
        DomSnapshot after = Snapshot(engine, 99, "After");
        UiPreservationState preservation = new UiPreservationState { FocusedNodeId = before.Roots[0].Id };

        UiUpdateBatch batch = engine.ComputeDomUpdates(before, after, UiUpdatePriority.UserInteraction, preservation);
        batch.Mutations[0].Value = "Changed by caller";
        UiUpdateBatch replay = engine.ComputeDomUpdates(before, after, UiUpdatePriority.Background, null);
        UiUpdateResult accepted = engine.AcceptDomUpdates(batch.Id, batch.Revision);
        UiUpdateResult repeated = engine.AcceptDomUpdates(batch.Id, batch.Revision);

        Assert.Multiple(() =>
        {
            Assert.That(batch.BaseRevision, Is.EqualTo(7));
            Assert.That(batch.Revision, Is.EqualTo(8));
            Assert.That(batch.Priority, Is.EqualTo(UiUpdatePriority.UserInteraction));
            Assert.That(replay.Id, Is.EqualTo(batch.Id));
            Assert.That(replay.Priority, Is.EqualTo(UiUpdatePriority.UserInteraction));
            Assert.That(replay.Mutations.Single().Kind, Is.EqualTo(UiMutationKind.SetText));
            Assert.That(replay.Mutations.Single().Value, Is.EqualTo("After"));
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(accepted.Revision, Is.EqualTo(8));
            Assert.That(repeated.Accepted, Is.True);
        });
    }

    [Test]
    public void RejectRetainsBaseAndReturnsDetachedRecoverySnapshot()
    {
        IUiUpdateEngine engine = new UiUpdateEngine();
        DomSnapshot before = Snapshot(engine, 2, "Before");
        DomSnapshot after = Snapshot(engine, 3, "Rejected");
        UiUpdateBatch batch = engine.ComputeDomUpdates(before, after, UiUpdatePriority.Immediate, null);

        UiUpdateResult rejected = engine.RejectDomUpdates(batch.Id, batch.Revision);
        rejected.Snapshot!.Roots[0].Children[0].Text = "Caller mutation";
        UiUpdateResult repeated = engine.RejectDomUpdates(batch.Id, batch.Revision);
        UiUpdateBatch replacement = engine.ComputeDomUpdates(before, Snapshot(engine, 8, "Replacement"), UiUpdatePriority.Normal, null);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.RequiresResync, Is.True);
            Assert.That(rejected.Revision, Is.EqualTo(2));
            Assert.That(rejected.Diagnostics.Single().Code, Is.EqualTo("update.batch-rejected"));
            Assert.That(repeated.Snapshot!.Roots[0].Children[0].Text, Is.EqualTo("Before"));
            Assert.That(replacement.BaseRevision, Is.EqualTo(2));
            Assert.That(replacement.Revision, Is.EqualTo(3));
        });
    }

    [Test]
    public void ComputeRejectsStaleBaseAndMismatchedSnapshotIdentity()
    {
        IUiUpdateEngine engine = new UiUpdateEngine();
        DomSnapshot before = Snapshot(engine, 0, "Before");
        UiUpdateBatch first = engine.ComputeDomUpdates(before, Snapshot(engine, 0, "After"), UiUpdatePriority.Normal, null);
        engine.AcceptDomUpdates(first.Id, first.Revision);

        Assert.Multiple(() =>
        {
            Assert.That(() => engine.ComputeDomUpdates(before, Snapshot(engine, 0, "Other"), UiUpdatePriority.Normal, null), Throws.ArgumentException.With.Property("ParamName").EqualTo("before"));
            Assert.That(() => engine.ComputeDomUpdates(Snapshot(engine, 1, "After"), engine.CreateDomSnapshot(ElementRoots("Next"), "other-session", "seed", 2), UiUpdatePriority.Normal, null), Throws.ArgumentException.With.Property("ParamName").EqualTo("after"));
            Assert.That(() => engine.ComputeDomUpdates(Snapshot(engine, 1, "After"), Snapshot(engine, 2, "Next"), (UiUpdatePriority)99, null), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void SnapshotAndAcknowledgementValidationArePredictable()
    {
        IUiUpdateEngine engine = new UiUpdateEngine();
        DomNode duplicate = new DomNode { Kind = DomNodeKind.Element, Key = "same", Name = HtmlElements.Division };
        DomNode other = new DomNode { Kind = DomNodeKind.Element, Key = "same", Name = HtmlElements.Division };

        UiUpdateResult unknownAcceptance = engine.AcceptDomUpdates(null, -1);
        UiUpdateResult unknownRejection = engine.RejectDomUpdates("missing", 1);

        Assert.Multiple(() =>
        {
            Assert.That(() => engine.CreateDomSnapshot(new DomNode[] { duplicate, other }, "session", "seed", 0), Throws.ArgumentException.With.Property("ParamName").EqualTo("roots"));
            Assert.That(() => engine.CreateDomSnapshot(ElementRoots("Text"), " ", "seed", 0), Throws.ArgumentException.With.Property("ParamName").EqualTo("sessionId"));
            Assert.That(() => engine.CreateDomSnapshot(ElementRoots("Text"), "session", "seed", -1), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(unknownAcceptance.Accepted, Is.False);
            Assert.That(unknownAcceptance.Diagnostics.Single().Code, Is.EqualTo("update.invalid-batch"));
            Assert.That(unknownRejection.Accepted, Is.False);
            Assert.That(unknownRejection.Diagnostics.Single().Code, Is.EqualTo("update.unknown-batch"));
        });
    }

    private static DomSnapshot Snapshot(IUiUpdateEngine engine, int revision, string text)
    {
        return engine.CreateDomSnapshot(ElementRoots(text), "session", "seed", revision);
    }

    private static DomNode[] ElementRoots(string text)
    {
        DomNode child = new DomNode { Kind = DomNodeKind.Text, Key = "text", Text = text };
        DomNode root = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "root",
            Name = HtmlElements.Division,
            Children = new DomNode[] { child }
        };
        return new DomNode[] { root };
    }
}
