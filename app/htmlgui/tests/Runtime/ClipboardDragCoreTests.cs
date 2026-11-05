using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class ClipboardDragCoreTests
{
    [TestCase(UiEventKind.Copy, "copy")]
    [TestCase(UiEventKind.Cut, "cut")]
    [TestCase(UiEventKind.Paste, "paste")]
    public void NormalizesAllowedClipboardOperationsAndPayloads(UiEventKind kind, string operation)
    {
        ClipboardDragCore core = new ClipboardDragCore();
        UiValueEntry[] data = new UiValueEntry[]
        {
            Entry(" Text/Plain ", Text("plain")),
            Entry("APPLICATION/JSON", Text("json"))
        };
        UiClipboardData source = new UiClipboardData { Operation = "spoofed", Data = data, IsAllowed = false };

        TransferOperationResult result = core.ProcessClipboardTransfer(kind, source, true, true, null);
        data[0].Value.StringValue = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Allowed, Is.True);
            Assert.That(result.Clipboard, Is.Not.Null.And.Not.SameAs(source));
            Assert.That(result.Clipboard!.Operation, Is.EqualTo(operation));
            Assert.That(result.Clipboard.IsAllowed, Is.True);
            Assert.That(result.Clipboard.Data.Select(item => item.Key), Is.EqualTo(new string[] { "application/json", "text/plain" }));
            Assert.That(result.Clipboard.Data[1].Value.StringValue, Is.EqualTo("plain"));
        });
    }

    [Test]
    public void AppliesClipboardOperationAndSecurityPolicy()
    {
        ClipboardDragCore core = new ClipboardDragCore();
        TransferPolicy policy = Policy();
        policy.AllowCut = false;
        policy.ExpectedOrigin = "https://app.example";
        Assert.That(core.ConfigureTransferPolicy(policy).Success, Is.True);
        UiClipboardData data = Clipboard(Entry("text/plain", Text("value")));

        TransferOperationResult denied = core.ProcessClipboardTransfer(UiEventKind.Cut, data, true, true, "https://app.example");
        TransferOperationResult untrusted = core.ProcessClipboardTransfer(UiEventKind.Copy, data, false, true, "https://app.example");
        TransferOperationResult subframe = core.ProcessClipboardTransfer(UiEventKind.Copy, data, true, false, "https://app.example");
        TransferOperationResult origin = core.ProcessClipboardTransfer(UiEventKind.Copy, data, true, true, "https://evil.example");
        TransferOperationResult invalidKind = core.ProcessClipboardTransfer(UiEventKind.Click, data, true, true, "https://app.example");

        Assert.Multiple(() =>
        {
            Assert.That(denied.Success, Is.True);
            Assert.That(denied.Allowed, Is.False);
            Assert.That(denied.Code, Is.EqualTo("clipboard.operation-denied"));
            Assert.That(denied.Clipboard!.Operation, Is.EqualTo("cut"));
            Assert.That(denied.Clipboard.IsAllowed, Is.False);
            Assert.That(untrusted.Code, Is.EqualTo("transfer.untrusted"));
            Assert.That(subframe.Code, Is.EqualTo("transfer.main-frame-required"));
            Assert.That(origin.Code, Is.EqualTo("transfer.origin-mismatch"));
            Assert.That(invalidKind.Code, Is.EqualTo("clipboard.invalid-operation"));
        });
    }

    [Test]
    public void ValidatesAndNormalizesTransferPolicy()
    {
        ClipboardDragCore core = new ClipboardDragCore();
        TransferPolicy policy = Policy();
        policy.AllowedMimeTypes = new string[] { "Text/*", "APPLICATION/JSON" };
        TransferOperationResult configured = core.ConfigureTransferPolicy(policy);
        TransferOperationResult wildcard = core.ProcessClipboardTransfer(
            UiEventKind.Paste,
            Clipboard(Entry("TEXT/URI-LIST", Text("https://example.test"))),
            true,
            true,
            null);
        TransferPolicy duplicate = Policy();
        duplicate.AllowedMimeTypes = new string[] { "text/plain", "TEXT/PLAIN" };
        TransferPolicy invalidLimit = Policy();
        invalidLimit.MaximumValueDepth = 129;
        TransferPolicy invalidOrigin = Policy();
        invalidOrigin.ExpectedOrigin = " ";

        Assert.Multiple(() =>
        {
            Assert.That(configured.Success, Is.True);
            Assert.That(core.Policy.AllowedMimeTypes, Is.EqualTo(new string[] { "application/json", "text/*" }));
            Assert.That(wildcard.Success, Is.True);
            Assert.That(wildcard.Clipboard!.Data.Single().Key, Is.EqualTo("text/uri-list"));
            Assert.That(core.ConfigureTransferPolicy(null).Code, Is.EqualTo("transfer.null-policy"));
            Assert.That(core.ConfigureTransferPolicy(duplicate).Code, Is.EqualTo("transfer.invalid-policy-mime"));
            Assert.That(core.ConfigureTransferPolicy(invalidLimit).Code, Is.EqualTo("transfer.invalid-limit"));
            Assert.That(core.ConfigureTransferPolicy(invalidOrigin).Code, Is.EqualTo("transfer.invalid-origin"));
        });
    }

    [Test]
    public void RejectsMalformedDisallowedDuplicateAndOversizedPayloads()
    {
        ClipboardDragCore core = new ClipboardDragCore();
        TransferPolicy policy = Policy();
        policy.AllowedMimeTypes = new string[] { "text/plain" };
        policy.MaximumEntryCount = 1;
        policy.MaximumTextLength = 4;
        core.ConfigureTransferPolicy(policy);

        TransferOperationResult tooMany = ClipboardResult(core,
            Entry("text/plain", Text("one")), Entry("application/json", Text("two")));
        TransferOperationResult disallowed = ClipboardResult(core, Entry("application/json", Text("one")));
        TransferOperationResult invalidMime = ClipboardResult(core, Entry("text/plain; charset=utf-8", Text("one")));
        TransferOperationResult duplicate = ClipboardResult(new ClipboardDragCore(),
            Entry("text/plain", Text("one")), Entry("TEXT/PLAIN", Text("two")));
        TransferOperationResult oversized = ClipboardResult(core, Entry("text/plain", Text("large")));
        TransferOperationResult malformed = ClipboardResult(core, Entry("text/plain", new UiValue { Kind = UiValueKind.String }));

        Assert.Multiple(() =>
        {
            Assert.That(tooMany.Code, Is.EqualTo("transfer.too-many-items"));
            Assert.That(disallowed.Code, Is.EqualTo("transfer.mime-not-allowed"));
            Assert.That(invalidMime.Code, Is.EqualTo("transfer.invalid-mime"));
            Assert.That(duplicate.Code, Is.EqualTo("transfer.duplicate-mime"));
            Assert.That(oversized.Code, Is.EqualTo("transfer.value-too-large"));
            Assert.That(malformed.Code, Is.EqualTo("transfer.invalid-value"));
        });
    }

    [Test]
    public void RejectsCyclicDeepAndAggregatePortableValuesWithoutThrowing()
    {
        ClipboardDragCore core = new ClipboardDragCore();
        TransferPolicy policy = Policy();
        policy.MaximumValueDepth = 1;
        policy.MaximumCollectionCount = 10;
        core.ConfigureTransferPolicy(policy);
        UiValue cycle = new UiValue { Kind = UiValueKind.Array, ArrayValue = Array.Empty<UiValue>() };
        cycle.ArrayValue = new UiValue[] { cycle };
        UiValue deep = new UiValue
        {
            Kind = UiValueKind.Array,
            ArrayValue = new UiValue[]
            {
                new UiValue
                {
                    Kind = UiValueKind.Array,
                    ArrayValue = new UiValue[] { Text("leaf") }
                }
            }
        };
        UiValue aggregate = new UiValue { Kind = UiValueKind.Array, ArrayValue = new UiValue[] { Text("a"), Text("b") } };
        ClipboardDragCore aggregateCore = new ClipboardDragCore();
        TransferPolicy aggregatePolicy = Policy();
        aggregatePolicy.MaximumCollectionCount = 1;
        aggregateCore.ConfigureTransferPolicy(aggregatePolicy);

        Assert.Multiple(() =>
        {
            Assert.That(ClipboardResult(core, Entry("text/plain", cycle)).Code, Is.EqualTo("transfer.value-cycle"));
            Assert.That(ClipboardResult(core, Entry("text/plain", deep)).Code, Is.EqualTo("transfer.value-too-deep"));
            Assert.That(ClipboardResult(aggregateCore, Entry("text/plain", aggregate)).Code, Is.EqualTo("transfer.value-too-large"));
        });
    }

    [Test]
    public void NegotiatesPointerDragEffectsAndCompletesTheLifecycle()
    {
        ClipboardDragCore core = new ClipboardDragCore();
        UiValueEntry payloadEntry = Entry("text/plain", Text("dragged"));
        TransferOperationResult started = core.ProcessDragTransfer(
            UiEventKind.DragStart,
            Drag("source", null, "none", "COPYMOVE", false, payloadEntry),
            true,
            true,
            null,
            false);
        payloadEntry.Value.StringValue = "changed";
        TransferOperationResult rejected = core.ProcessDragTransfer(
            UiEventKind.DragEnter, Drag("source", "target", "link", "", false), true, true, null, false);
        TransferOperationResult accepted = core.ProcessDragTransfer(
            UiEventKind.DragOver, Drag("source", "target", "move", "", false), true, true, null, false);
        TransferOperationResult dropped = core.ProcessDragTransfer(
            UiEventKind.Drop, Drag("source", "target", "move", "", false), true, true, null, false);
        TransferOperationResult ended = core.ProcessDragTransfer(
            UiEventKind.DragEnd, Drag("source", null, "none", "", false), true, true, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(started.Success, Is.True);
            Assert.That(started.Drag!.AllowedEffect, Is.EqualTo("copyMove"));
            Assert.That(started.Drag.Data.Single().Value.StringValue, Is.EqualTo("dragged"));
            Assert.That(rejected.Success, Is.True);
            Assert.That(rejected.Allowed, Is.False);
            Assert.That(rejected.Code, Is.EqualTo("drag.effect-not-allowed"));
            Assert.That(accepted.Allowed, Is.True);
            Assert.That(accepted.Drag!.Effect, Is.EqualTo("move"));
            Assert.That(dropped.DropCompleted, Is.True);
            Assert.That(dropped.Drag!.TargetId, Is.EqualTo("target"));
            Assert.That(ended.Success, Is.True);
            Assert.That(ended.DropCompleted, Is.True);
            Assert.That(core.DragState.Active, Is.False);
            Assert.That(core.DragState.DropCompleted, Is.False);
        });
    }

    [Test]
    public void TracksTargetLeaveAndDeniesDropsWithoutAnAcceptedEffect()
    {
        ClipboardDragCore core = new ClipboardDragCore();
        core.ProcessDragTransfer(UiEventKind.DragStart, Drag("source", null, "none", "copy", false), true, true, null, false);
        TransferOperationResult entered = core.ProcessDragTransfer(
            UiEventKind.DragEnter, Drag("source", "first", "copy", "", false), true, true, null, false);
        TransferOperationResult mismatch = core.ProcessDragTransfer(
            UiEventKind.DragLeave, Drag("source", "other", "none", "", false), true, true, null, false);
        TransferOperationResult left = core.ProcessDragTransfer(
            UiEventKind.DragLeave, Drag("source", "first", "none", "", false), true, true, null, false);
        core.ProcessDragTransfer(UiEventKind.DragEnter, Drag("source", "second", "none", "", false), true, true, null, false);
        TransferOperationResult denied = core.ProcessDragTransfer(
            UiEventKind.Drop, Drag("source", "second", "none", "", false), true, true, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(entered.Allowed, Is.True);
            Assert.That(mismatch.Code, Is.EqualTo("drag.target-mismatch"));
            Assert.That(left.Success, Is.True);
            Assert.That(left.Drag!.TargetId, Is.Null);
            Assert.That(denied.Success, Is.True);
            Assert.That(denied.Allowed, Is.False);
            Assert.That(denied.Code, Is.EqualTo("drag.drop-denied"));
            Assert.That(core.DragState.Active, Is.True);
        });
    }

    [Test]
    public void UsesTheSameStateMachineForKeyboardDragAndCancellation()
    {
        ClipboardDragCore core = new ClipboardDragCore();
        UiValueEntry entry = Entry("text/plain", Text("keyboard"));
        TransferOperationResult started = core.BeginKeyboardDrag("item", new UiValueEntry[] { entry }, "all", true, true, null);
        entry.Value.StringValue = "changed";
        TransferOperationResult target = core.ProcessDragTransfer(
            UiEventKind.DragOver, Drag("item", "destination", "link", "", true), true, true, null, false);
        TransferOperationResult canceled = core.ProcessDragTransfer(
            UiEventKind.Drop, Drag("item", "destination", "link", "", true), true, true, null, true);
        TransferOperationResult ended = core.ProcessDragTransfer(
            UiEventKind.DragEnd, Drag("item", null, "none", "", true), true, true, null, false);
        TransferOperationResult restarted = core.BeginKeyboardDrag("second", Array.Empty<UiValueEntry>(), "move", true, true, null);

        Assert.Multiple(() =>
        {
            Assert.That(started.Allowed, Is.True);
            Assert.That(started.Drag!.IsKeyboard, Is.True);
            Assert.That(started.Drag.Data.Single().Value.StringValue, Is.EqualTo("keyboard"));
            Assert.That(target.Allowed, Is.True);
            Assert.That(target.Drag!.Effect, Is.EqualTo("link"));
            Assert.That(canceled.Canceled, Is.True);
            Assert.That(canceled.DropCompleted, Is.False);
            Assert.That(ended.Success, Is.True);
            Assert.That(ended.Canceled, Is.True);
            Assert.That(restarted.Success, Is.True);
            Assert.That(restarted.Drag!.SourceId, Is.EqualTo("second"));
        });
    }

    [Test]
    public void EnforcesKeyboardPolicyAndDragIdentity()
    {
        ClipboardDragCore deniedCore = new ClipboardDragCore();
        TransferPolicy policy = Policy();
        policy.AllowKeyboardDrag = false;
        deniedCore.ConfigureTransferPolicy(policy);
        TransferOperationResult denied = deniedCore.BeginKeyboardDrag("item", Array.Empty<UiValueEntry>(), "copy", true, true, null);

        ClipboardDragCore core = new ClipboardDragCore();
        TransferOperationResult inactive = core.ProcessDragTransfer(
            UiEventKind.Drag, Drag("source", null, "none", "", false), true, true, null, false);
        core.ProcessDragTransfer(UiEventKind.DragStart, Drag("source", null, "none", "copy", false), true, true, null, false);
        TransferOperationResult duplicate = core.ProcessDragTransfer(
            UiEventKind.DragStart, Drag("source", null, "none", "copy", false), true, true, null, false);
        TransferOperationResult source = core.ProcessDragTransfer(
            UiEventKind.Drag, Drag("other", null, "none", "", false), true, true, null, false);
        TransferOperationResult target = core.ProcessDragTransfer(
            UiEventKind.DragEnter, Drag("source", " ", "copy", "", false), true, true, null, false);
        TransferOperationResult effect = core.ProcessDragTransfer(
            UiEventKind.DragEnter, Drag("source", "target", "invalid", "", false), true, true, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(denied.Success, Is.True);
            Assert.That(denied.Allowed, Is.False);
            Assert.That(denied.Code, Is.EqualTo("drag.keyboard-denied"));
            Assert.That(inactive.Code, Is.EqualTo("drag.not-active"));
            Assert.That(duplicate.Code, Is.EqualTo("drag.already-active"));
            Assert.That(source.Code, Is.EqualTo("drag.source-mismatch"));
            Assert.That(target.Code, Is.EqualTo("drag.invalid-target"));
            Assert.That(effect.Code, Is.EqualTo("drag.invalid-effect"));
        });
    }

    [Test]
    public void ResetsActiveDragWhenPolicyChanges()
    {
        ClipboardDragCore core = new ClipboardDragCore();
        core.ProcessDragTransfer(UiEventKind.DragStart, Drag("source", null, "none", "copy", false), true, true, null, false);

        TransferOperationResult configured = core.ConfigureTransferPolicy(Policy());

        Assert.Multiple(() =>
        {
            Assert.That(configured.Success, Is.True);
            Assert.That(core.DragState.Active, Is.False);
            Assert.That(core.DragState.Data, Is.Empty);
        });
    }

    private static TransferOperationResult ClipboardResult(ClipboardDragCore core, params UiValueEntry[] entries)
    {
        return core.ProcessClipboardTransfer(UiEventKind.Paste, Clipboard(entries), true, true, null);
    }

    private static TransferPolicy Policy()
    {
        return new TransferPolicy();
    }

    private static UiClipboardData Clipboard(params UiValueEntry[] entries)
    {
        return new UiClipboardData { Data = entries };
    }

    private static UiDragData Drag(string? sourceId, string? targetId, string effect, string allowedEffect, bool keyboard, params UiValueEntry[] entries)
    {
        return new UiDragData
        {
            SourceId = sourceId,
            TargetId = targetId,
            Effect = effect,
            AllowedEffect = allowedEffect,
            IsKeyboard = keyboard,
            Data = entries
        };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
