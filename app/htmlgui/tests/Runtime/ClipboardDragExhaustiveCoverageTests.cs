using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class ClipboardDragExhaustiveCoverageTests
{
    [Test]
    public void AcceptsEveryPortableValueKindAndReturnsAnIndependentPayload()
    {
        ClipboardDragCore core = new ClipboardDragCore();
        TransferPolicy policy = Policy();
        policy.AllowedMimeTypes = new string[] { "*/*" };
        Assert.That(core.ConfigureTransferPolicy(policy).Success, Is.True);
        byte[] bytes = new byte[] { 1, 2, 3 };
        UiValue nestedText = Text("nested");
        UiValueEntry[] data = new UiValueEntry[]
        {
            Entry("application/x-null", new UiValue { Kind = UiValueKind.Null }),
            Entry("application/x-boolean", new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true }),
            Entry("application/x-integer", new UiValue { Kind = UiValueKind.Integer, IntegerValue = -42 }),
            Entry("application/x-floating", new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = 1.25 }),
            Entry("application/x-decimal", new UiValue { Kind = UiValueKind.Decimal, DecimalValue = 2.5m }),
            Entry("application/x-string", Text("text")),
            Entry("application/x-date", new UiValue { Kind = UiValueKind.DateTime, DateTimeValue = "2026-09-14T12:00:00Z" }),
            Entry("application/x-binary", new UiValue { Kind = UiValueKind.Binary, BinaryValue = bytes }),
            Entry("application/x-array", new UiValue { Kind = UiValueKind.Array, ArrayValue = new UiValue[] { nestedText } }),
            Entry("application/x-object", new UiValue
            {
                Kind = UiValueKind.Object,
                ObjectValue = new UiValueEntry[] { Entry("answer", new UiValue { Kind = UiValueKind.Integer, IntegerValue = 42 }) }
            })
        };

        TransferOperationResult result = core.ProcessClipboardTransfer(UiEventKind.Paste, Clipboard(data), true, true, null);
        bytes[0] = 9;
        nestedText.StringValue = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Allowed, Is.True);
            Assert.That(result.Clipboard!.Data, Has.Length.EqualTo(10));
            Assert.That(result.Clipboard.Data.Select(item => item.Key), Is.Ordered);
            Assert.That(result.Clipboard.Data.Single(item => item.Key == "application/x-binary").Value.BinaryValue, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.Clipboard.Data.Single(item => item.Key == "application/x-array").Value.ArrayValue![0].StringValue, Is.EqualTo("nested"));
            Assert.That(result.Clipboard.Data.Single(item => item.Key == "application/x-object").Value.ObjectValue![0].Value.IntegerValue, Is.EqualTo(42));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("text")]
    [TestCase("/plain")]
    [TestCase("text/")]
    [TestCase("text/plain/extra")]
    [TestCase("text/plain; charset=utf-8")]
    [TestCase("text/*")]
    [TestCase("*/*")]
    [TestCase("te xt/plain")]
    public void RejectsMalformedPayloadMimeTypes(string? mimeType)
    {
        ClipboardDragCore core = new ClipboardDragCore();
        UiValueEntry entry = new UiValueEntry { Key = mimeType!, Value = Text("value") };

        TransferOperationResult result = core.ProcessClipboardTransfer(UiEventKind.Copy, Clipboard(entry), true, true, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo("transfer.invalid-mime"));
        });
    }

    [Test]
    public void RejectsNullAndMalformedPortablePayloadShapes()
    {
        ClipboardDragCore core = new ClipboardDragCore();
        UiClipboardData nullEntries = new UiClipboardData { Data = new UiValueEntry[] { null! } };
        UiClipboardData nullValue = Clipboard(new UiValueEntry { Key = "text/plain", Value = null! });
        UiValue invalidKind = new UiValue { Kind = (UiValueKind)999 };
        UiValue invalidBoolean = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true, IntegerValue = 1 };
        UiValue invalidFloat = new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = double.NaN };
        UiValue invalidArray = new UiValue { Kind = UiValueKind.Array, ArrayValue = new UiValue[] { null! } };
        UiValue invalidObjectEntry = new UiValue { Kind = UiValueKind.Object, ObjectValue = new UiValueEntry[] { null! } };
        UiValue duplicateObjectKey = new UiValue
        {
            Kind = UiValueKind.Object,
            ObjectValue = new UiValueEntry[] { Entry("same", Text("one")), Entry("same", Text("two")) }
        };

        TransferOperationResult nullClipboard = core.ProcessClipboardTransfer(UiEventKind.Copy, null, true, true, null);
        TransferOperationResult empty = core.ProcessClipboardTransfer(
            UiEventKind.Copy, new UiClipboardData { Data = null! }, true, true, null);
        TransferOperationResult nullEntry = core.ProcessClipboardTransfer(UiEventKind.Copy, nullEntries, true, true, null);
        TransferOperationResult nullEntryValue = core.ProcessClipboardTransfer(UiEventKind.Copy, nullValue, true, true, null);
        TransferOperationResult unknownKind = ClipboardResult(core, invalidKind);
        TransferOperationResult mixedShape = ClipboardResult(core, invalidBoolean);
        TransferOperationResult nonFinite = ClipboardResult(core, invalidFloat);
        TransferOperationResult nullArrayItem = ClipboardResult(core, invalidArray);
        TransferOperationResult nullObjectItem = ClipboardResult(core, invalidObjectEntry);
        TransferOperationResult duplicateKey = ClipboardResult(core, duplicateObjectKey);

        Assert.Multiple(() =>
        {
            Assert.That(nullClipboard.Code, Is.EqualTo("clipboard.null-data"));
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Clipboard!.Data, Is.Empty);
            Assert.That(nullEntry.Code, Is.EqualTo("transfer.null-entry"));
            Assert.That(nullEntryValue.Code, Is.EqualTo("transfer.invalid-value"));
            Assert.That(unknownKind.Code, Is.EqualTo("transfer.invalid-value"));
            Assert.That(mixedShape.Code, Is.EqualTo("transfer.invalid-value"));
            Assert.That(nonFinite.Code, Is.EqualTo("transfer.invalid-value"));
            Assert.That(nullArrayItem.Code, Is.EqualTo("transfer.invalid-value"));
            Assert.That(nullObjectItem.Code, Is.EqualTo("transfer.invalid-value"));
            Assert.That(duplicateKey.Code, Is.EqualTo("transfer.invalid-value"));
        });
    }

    [TestCase(UiEventKind.Copy)]
    [TestCase(UiEventKind.Cut)]
    [TestCase(UiEventKind.Paste)]
    public void DeniesEachDisabledClipboardOperation(UiEventKind kind)
    {
        ClipboardDragCore core = new ClipboardDragCore();
        TransferPolicy policy = Policy();
        policy.AllowCopy = kind != UiEventKind.Copy;
        policy.AllowCut = kind != UiEventKind.Cut;
        policy.AllowPaste = kind != UiEventKind.Paste;
        core.ConfigureTransferPolicy(policy);

        TransferOperationResult result = core.ProcessClipboardTransfer(kind, Clipboard(Entry("text/plain", Text("value"))), true, true, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Allowed, Is.False);
            Assert.That(result.Code, Is.EqualTo("clipboard.operation-denied"));
            Assert.That(result.Clipboard!.Data, Is.Empty);
        });
    }

    [Test]
    public void EnforcesAndCanRelaxTrustFrameAndOriginPolicyForClipboardAndDrag()
    {
        ClipboardDragCore guarded = new ClipboardDragCore();
        TransferPolicy guardedPolicy = Policy();
        guardedPolicy.ExpectedOrigin = "https://app.example";
        guarded.ConfigureTransferPolicy(guardedPolicy);
        UiClipboardData clipboard = Clipboard(Entry("text/plain", Text("value")));

        TransferOperationResult untrustedClipboard = guarded.ProcessClipboardTransfer(
            UiEventKind.Paste, clipboard, false, true, "https://app.example");
        TransferOperationResult crossOriginClipboard = guarded.ProcessClipboardTransfer(
            UiEventKind.Paste, clipboard, true, true, "https://other.example");
        TransferOperationResult untrustedDrag = guarded.ProcessDragTransfer(
            UiEventKind.DragStart, Drag("source", null, "none", "copy", false), false, true, "https://app.example", false);
        TransferOperationResult crossOriginKeyboard = guarded.BeginKeyboardDrag(
            "source", Array.Empty<UiValueEntry>(), "copy", true, true, "https://other.example");

        ClipboardDragCore relaxed = new ClipboardDragCore();
        TransferPolicy relaxedPolicy = Policy();
        relaxedPolicy.RequireTrusted = false;
        relaxedPolicy.RequireMainFrame = false;
        relaxed.ConfigureTransferPolicy(relaxedPolicy);
        TransferOperationResult relaxedClipboard = relaxed.ProcessClipboardTransfer(
            UiEventKind.Paste, clipboard, false, false, "https://other.example");
        TransferOperationResult relaxedDrag = relaxed.BeginKeyboardDrag(
            "source", Array.Empty<UiValueEntry>(), "copy", false, false, "https://other.example");

        Assert.Multiple(() =>
        {
            Assert.That(untrustedClipboard.Code, Is.EqualTo("transfer.untrusted"));
            Assert.That(crossOriginClipboard.Code, Is.EqualTo("transfer.origin-mismatch"));
            Assert.That(untrustedDrag.Code, Is.EqualTo("transfer.untrusted"));
            Assert.That(crossOriginKeyboard.Code, Is.EqualTo("transfer.origin-mismatch"));
            Assert.That(guarded.DragState.Active, Is.False);
            Assert.That(relaxedClipboard.Success, Is.True);
            Assert.That(relaxedDrag.Success, Is.True);
            Assert.That(relaxedDrag.Drag!.IsKeyboard, Is.True);
        });
    }

    [TestCase("none", "none", "none", false)]
    [TestCase("none", "copy", "none", false)]
    [TestCase("copy", "copy", "copy", true)]
    [TestCase("copy", "link", "copy", false)]
    [TestCase(" COPYLINK ", "copy", "copyLink", true)]
    [TestCase("copyLink", "link", "copyLink", true)]
    [TestCase("copyLink", "move", "copyLink", false)]
    [TestCase("copyMove", "copy", "copyMove", true)]
    [TestCase("copyMove", "move", "copyMove", true)]
    [TestCase("copyMove", "link", "copyMove", false)]
    [TestCase("link", "link", "link", true)]
    [TestCase("link", "copy", "link", false)]
    [TestCase("linkMove", "link", "linkMove", true)]
    [TestCase("linkMove", "move", "linkMove", true)]
    [TestCase("linkMove", "copy", "linkMove", false)]
    [TestCase("move", "move", "move", true)]
    [TestCase("all", "copy", "all", true)]
    [TestCase("all", "link", "all", true)]
    [TestCase("all", "move", "all", true)]
    [TestCase("uninitialized", "copy", "all", true)]
    public void AppliesEveryAllowedDragEffectCombination(string allowedEffect, string effect, string normalizedAllowedEffect, bool expectedAllowed)
    {
        ClipboardDragCore core = new ClipboardDragCore();
        TransferOperationResult started = core.ProcessDragTransfer(
            UiEventKind.DragStart, Drag("source", null, "none", allowedEffect, false), true, true, null, false);
        TransferOperationResult entered = core.ProcessDragTransfer(
            UiEventKind.DragEnter, Drag("source", "target", effect, string.Empty, false), true, true, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(started.Success, Is.True);
            Assert.That(started.Drag!.AllowedEffect, Is.EqualTo(normalizedAllowedEffect));
            Assert.That(entered.Success, Is.True);
            Assert.That(entered.Allowed, Is.EqualTo(expectedAllowed));
            Assert.That(entered.Drag!.Effect, Is.EqualTo(expectedAllowed ? effect : "none"));
            Assert.That(entered.Code, Is.EqualTo(expectedAllowed || effect == "none" ? string.Empty : "drag.effect-not-allowed"));
        });
    }

    [Test]
    public void RejectsMalformedDragStartsAndRequestedEffects()
    {
        ClipboardDragCore core = new ClipboardDragCore();
        TransferOperationResult invalidKind = core.ProcessDragTransfer(UiEventKind.Click, null, true, true, null, false);
        TransferOperationResult nullDrag = core.ProcessDragTransfer(UiEventKind.DragStart, null, true, true, null, false);
        TransferOperationResult nullSource = core.ProcessDragTransfer(
            UiEventKind.DragStart, Drag(null, null, "none", "copy", false), true, true, null, false);
        TransferOperationResult blankSource = core.ProcessDragTransfer(
            UiEventKind.DragStart, Drag(" ", null, "none", "copy", false), true, true, null, false);
        TransferOperationResult invalidAllowedEffect = core.ProcessDragTransfer(
            UiEventKind.DragStart, Drag("source", null, "none", "copy-and-move", false), true, true, null, false);
        TransferOperationResult started = core.ProcessDragTransfer(
            UiEventKind.DragStart, Drag("source", null, "none", "all", false), true, true, null, false);
        TransferOperationResult invalidEffect = core.ProcessDragTransfer(
            UiEventKind.DragOver, Drag("source", "target", "copy-and-move", string.Empty, false), true, true, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(invalidKind.Code, Is.EqualTo("drag.invalid-operation"));
            Assert.That(nullDrag.Code, Is.EqualTo("drag.null-data"));
            Assert.That(nullSource.Code, Is.EqualTo("drag.invalid-source"));
            Assert.That(blankSource.Code, Is.EqualTo("drag.invalid-source"));
            Assert.That(invalidAllowedEffect.Code, Is.EqualTo("drag.invalid-allowed-effect"));
            Assert.That(started.Success, Is.True);
            Assert.That(invalidEffect.Code, Is.EqualTo("drag.invalid-effect"));
        });
    }

    [Test]
    public void ReportsExplicitAndImplicitCancellationAndResetsStateAfterEnd()
    {
        ClipboardDragCore inactive = new ClipboardDragCore();
        TransferOperationResult inactiveCancellation = inactive.ProcessDragTransfer(
            UiEventKind.DragEnd, null, true, true, null, true);

        ClipboardDragCore implicitCore = new ClipboardDragCore();
        implicitCore.ProcessDragTransfer(
            UiEventKind.DragStart, Drag("pointer", null, "none", "move", false), true, true, null, false);
        TransferOperationResult implicitEnd = implicitCore.ProcessDragTransfer(
            UiEventKind.DragEnd, Drag("pointer", null, "none", string.Empty, false), true, true, null, false);

        ClipboardDragCore explicitCore = new ClipboardDragCore();
        explicitCore.BeginKeyboardDrag("keyboard", Array.Empty<UiValueEntry>(), "copy", true, true, null);
        TransferOperationResult explicitCancellation = explicitCore.ProcessDragTransfer(
            UiEventKind.DragLeave, null, true, true, null, true);
        TransferOperationResult afterCancellation = explicitCore.ProcessDragTransfer(
            UiEventKind.Drag, Drag("keyboard", null, "none", string.Empty, true), true, true, null, false);
        TransferOperationResult explicitEnd = explicitCore.ProcessDragTransfer(
            UiEventKind.DragEnd, Drag("keyboard", null, "none", string.Empty, true), true, true, null, false);
        TransferOperationResult secondEnd = explicitCore.ProcessDragTransfer(
            UiEventKind.DragEnd, null, true, true, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(inactiveCancellation.Code, Is.EqualTo("drag.not-active"));
            Assert.That(implicitEnd.Success, Is.True);
            Assert.That(implicitEnd.Allowed, Is.False);
            Assert.That(implicitEnd.Canceled, Is.True);
            Assert.That(implicitEnd.DropCompleted, Is.False);
            Assert.That(implicitCore.DragState.SourceId, Is.Empty);
            Assert.That(explicitCancellation.Canceled, Is.True);
            Assert.That(explicitCancellation.Drag!.IsKeyboard, Is.True);
            Assert.That(afterCancellation.Code, Is.EqualTo("drag.not-active"));
            Assert.That(explicitEnd.Success, Is.True);
            Assert.That(explicitEnd.Canceled, Is.True);
            Assert.That(secondEnd.Code, Is.EqualTo("drag.not-active"));
            Assert.That(explicitCore.DragState.SourceId, Is.Empty);
        });
    }

    [Test]
    public void RejectsNullPolicyOriginWithoutThrowing()
    {
        ClipboardDragCore core = new ClipboardDragCore();
        TransferPolicy policy = Policy();
        policy.ExpectedOrigin = null!;

        TransferOperationResult result = core.ConfigureTransferPolicy(policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo("transfer.invalid-origin"));
        });
    }

    private static TransferOperationResult ClipboardResult(ClipboardDragCore core, UiValue value)
    {
        return core.ProcessClipboardTransfer(UiEventKind.Paste, Clipboard(Entry("text/plain", value)), true, true, null);
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
