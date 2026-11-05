using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class UiFocusControllerTests
{
    [Test]
    public void RegistersIndependentScopesAndAppliesFocusMovementAndRestore()
    {
        UiFocusScope page = Scope("page", "page-owner", new string[] { "trigger" });
        UiFocusScope dialog = Scope("dialog", "dialog-owner", new string[] { "cancel", "confirm" });
        dialog.ParentScopeId = "page";
        dialog.InitialFocusId = "cancel";
        dialog.ReturnFocusId = "trigger";
        dialog.RovingTabIndex = true;
        dialog.Wrap = true;
        UiFocusTarget trigger = Target("trigger", "page", 0);
        UiFocusTarget cancel = Target("cancel", "dialog", 0);
        UiFocusTarget confirm = Target("confirm", "dialog", -1);
        IUiFocusController controller = new UiFocusController();

        UiResult registration = controller.RegisterFocusScopes(
            new UiFocusScope[] { page, dialog },
            new UiFocusTarget[] { trigger, cancel, confirm });
        dialog.InitialFocusId = "confirm";
        cancel.TabIndex = -1;
        UiFocusResult entered = controller.ApplyFocusRequest(
            new UiFocusState(),
            new UiFocusRequest { Operation = UiFocusOperation.Focus, ScopeId = "dialog" });
        UiFocusResult moved = controller.MoveFocus(entered.State, UiFocusDirection.Next, true);
        UiFocusResult restored = controller.RestoreFocus(moved.State, "dialog", true);

        Assert.Multiple(() =>
        {
            Assert.That(registration.Success, Is.True);
            Assert.That(registration.Diagnostics, Is.Empty);
            Assert.That(entered.Success, Is.True);
            Assert.That(entered.State.FocusedNodeId, Is.EqualTo("cancel"));
            Assert.That(entered.Targets.Single(item => item.Id == "cancel").TabIndex, Is.Zero);
            Assert.That(moved.State.FocusedNodeId, Is.EqualTo("confirm"));
            Assert.That(moved.RevealNodeId, Is.EqualTo("confirm"));
            Assert.That(moved.Targets.Select(item => item.TabIndex), Is.EqualTo(new int[] { 0, -1, 0 }));
            Assert.That(restored.Success, Is.True);
            Assert.That(restored.State.FocusedNodeId, Is.EqualTo("trigger"));
            Assert.That(restored.State.ScopeId, Is.EqualTo("page"));
            Assert.That(restored.RevealNodeId, Is.EqualTo("trigger"));
        });
    }

    [Test]
    public void UpdatesTrapStateAndKeepsTheLastValidRegistration()
    {
        UiFocusScope page = Scope("page", "page-owner", new string[] { "outside" });
        UiFocusScope dialog = Scope("dialog", "dialog-owner", new string[] { "inside" });
        UiFocusTarget[] targets = new UiFocusTarget[]
        {
            Target("outside", "page", 0),
            Target("inside", "dialog", 0)
        };
        IUiFocusController controller = new UiFocusController();
        controller.RegisterFocusScopes(new UiFocusScope[] { page, dialog }, targets);

        UiResult enabled = controller.SetFocusTrap("dialog", true);
        UiFocusState inside = new UiFocusState { FocusedNodeId = "inside", ScopeId = "dialog", IsTrapped = true };
        UiFocusRequest outside = new UiFocusRequest { Operation = UiFocusOperation.Focus, TargetId = "outside" };
        UiFocusResult trapped = controller.ApplyFocusRequest(inside, outside);
        UiResult invalid = controller.RegisterFocusScopes(
            new UiFocusScope[] { Scope("duplicate", "one", Array.Empty<string>()), Scope("duplicate", "two", Array.Empty<string>()) },
            Array.Empty<UiFocusTarget>());
        UiResult disabled = controller.SetFocusTrap("dialog", false);
        UiFocusResult released = controller.ApplyFocusRequest(inside, outside);
        UiResult unknown = controller.SetFocusTrap("missing", true);

        Assert.Multiple(() =>
        {
            Assert.That(enabled.Success, Is.True);
            Assert.That(trapped.Success, Is.False);
            Assert.That(trapped.Diagnostics.Select(item => item.Code), Does.Contain("focus.trapped-target"));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Diagnostics.Select(item => item.Code), Does.Contain("focus.duplicate-scope"));
            Assert.That(disabled.Success, Is.True);
            Assert.That(released.Success, Is.True);
            Assert.That(released.State.FocusedNodeId, Is.EqualTo("outside"));
            Assert.That(unknown.Success, Is.False);
            Assert.That(unknown.Diagnostics.Single().Code, Is.EqualTo("focus.unknown-scope"));
        });
    }

    [Test]
    public void ReportsNullRegistrationAndRequestInputs()
    {
        IUiFocusController controller = new UiFocusController();

        UiResult registration = controller.RegisterFocusScopes(null, null);
        UiFocusResult request = controller.ApplyFocusRequest(null, null);

        Assert.Multiple(() =>
        {
            Assert.That(registration.Success, Is.False);
            Assert.That(registration.Diagnostics.Select(item => item.Code), Is.EqualTo(new string[] { "focus.null-scopes", "focus.null-targets" }));
            Assert.That(request.Success, Is.False);
            Assert.That(request.Diagnostics.Select(item => item.Code), Does.Contain("focus.null-state"));
            Assert.That(request.Diagnostics.Select(item => item.Code), Does.Contain("focus.null-request"));
        });
    }

    [Test]
    public void PlansBrowserStatePreservationThroughThePublicContract()
    {
        DomNode inputBefore = Input("field");
        DomNode inputAfter = Input("field");
        DomNode scrollerAfter = new DomNode { Kind = DomNodeKind.Element, Id = "scroll", Name = "div" };
        DomSnapshot before = new DomSnapshot { Roots = new DomNode[] { inputBefore } };
        DomSnapshot after = new DomSnapshot { Roots = new DomNode[] { inputAfter, scrollerAfter } };
        UiPreservationState current = new UiPreservationState
        {
            FocusedNodeId = "field",
            HasSelection = true,
            SelectionStart = 1,
            SelectionEnd = 3,
            IsComposing = true,
            ScrollOffsets = new UiValueEntry[]
            {
                new UiValueEntry { Key = "scroll", Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = 12 } },
                new UiValueEntry { Key = "removed", Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = 20 } }
            }
        };
        IUiFocusController controller = new UiFocusController();

        UiPreservationState result = controller.PlanFocusPreservation(before, after, Array.Empty<DomMutation>(), current);

        Assert.Multiple(() =>
        {
            Assert.That(result.PreserveFocus, Is.True);
            Assert.That(result.PreserveSelection, Is.True);
            Assert.That(result.PreserveComposition, Is.True);
            Assert.That(result.PreserveScroll, Is.True);
            Assert.That(result.ScrollOffsets.Select(item => item.Key), Is.EqualTo(new string[] { "scroll" }));
            Assert.That(result, Is.Not.SameAs(current));
        });
    }

    private static UiFocusScope Scope(string id, string ownerId, string[] targetIds)
    {
        return new UiFocusScope { Id = id, OwnerNodeId = ownerId, TargetIds = targetIds };
    }

    private static UiFocusTarget Target(string id, string scopeId, int tabIndex)
    {
        return new UiFocusTarget
        {
            Id = id,
            ScopeId = scopeId,
            HasTabIndex = true,
            TabIndex = tabIndex
        };
    }

    private static DomNode Input(string id)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Id = id,
            Name = HtmlElements.Input,
            Attributes = new DomAttribute[]
            {
                new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Text }
            }
        };
    }
}
