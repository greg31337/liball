using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class FocusEngineTests
{
    [Test]
    public void FocusesTheFirstTargetInBrowserTabOrderAndRequestsReveal()
    {
        UiFocusScope scope = Scope("page", "page-owner", new string[] { "zero", "second", "first" });
        UiFocusTarget[] targets = new UiFocusTarget[]
        {
            Target("zero", "page", 0),
            Target("second", "page", 2),
            Target("first", "page", 1)
        };
        UiFocusRequest request = new UiFocusRequest { Operation = UiFocusOperation.Focus, ScopeId = "page", Reveal = true };

        UiFocusResult result = new FocusEngine().ResolveFocus(new UiFocusScope[] { scope }, targets, new UiFocusState(), request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.State.FocusedNodeId, Is.EqualTo("first"));
            Assert.That(result.State.ScopeId, Is.EqualTo("page"));
            Assert.That(result.RevealNodeId, Is.EqualTo("first"));
        });
    }

    [Test]
    public void RovingTabIndexWrapsInsideATrappedScopeWithoutMutatingInputs()
    {
        UiFocusScope scope = Scope("toolbar", "toolbar-owner", new string[] { "one", "two", "three" });
        scope.RovingTabIndex = true;
        scope.TrapFocus = true;
        UiFocusTarget[] targets = new UiFocusTarget[]
        {
            Target("one", "toolbar", -1),
            Target("two", "toolbar", -1),
            Target("three", "toolbar", 0)
        };
        UiFocusState state = new UiFocusState { FocusedNodeId = "three", ScopeId = "toolbar" };
        UiFocusRequest request = new UiFocusRequest { Operation = UiFocusOperation.Move, Direction = UiFocusDirection.Next };

        UiFocusResult result = new FocusEngine().ResolveFocus(new UiFocusScope[] { scope }, targets, state, request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.State.FocusedNodeId, Is.EqualTo("one"));
            Assert.That(result.State.IsTrapped, Is.True);
            Assert.That(result.Targets.Select(item => item.TabIndex), Is.EqualTo(new int[] { 0, -1, -1 }));
            Assert.That(targets.Select(item => item.TabIndex), Is.EqualTo(new int[] { -1, -1, 0 }));
            Assert.That(state.FocusedNodeId, Is.EqualTo("three"));
        });
    }

    [Test]
    public void MovesAnActiveDescendantWhileFocusRemainsOnTheCompositeOwner()
    {
        UiFocusScope scope = Scope("list", "list-owner", new string[] { "alpha", "beta", "gamma" });
        scope.UseActiveDescendant = true;
        UiFocusTarget[] targets = new UiFocusTarget[]
        {
            Target("alpha", "list", 0),
            Target("beta", "list", -1),
            Target("gamma", "list", -1)
        };
        UiFocusState state = new UiFocusState { FocusedNodeId = "list-owner", ActiveDescendantId = "beta", ScopeId = "list", IsFocusVisible = true };
        UiFocusRequest request = new UiFocusRequest { Operation = UiFocusOperation.Move, Direction = UiFocusDirection.Down, Reveal = true };

        UiFocusResult result = new FocusEngine().ResolveFocus(new UiFocusScope[] { scope }, targets, state, request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.State.FocusedNodeId, Is.EqualTo("list-owner"));
            Assert.That(result.State.ActiveDescendantId, Is.EqualTo("gamma"));
            Assert.That(result.State.IsFocusVisible, Is.True);
            Assert.That(result.RevealNodeId, Is.EqualTo("gamma"));
        });
    }

    [Test]
    public void SupportsInitialFocusBlurAndReturnFocusAcrossNestedScopes()
    {
        UiFocusScope page = Scope("page", "page-owner", new string[] { "trigger" });
        UiFocusScope dialog = Scope("dialog", "dialog-owner", new string[] { "cancel", "confirm" });
        dialog.ParentScopeId = "page";
        dialog.InitialFocusId = "cancel";
        dialog.ReturnFocusId = "trigger";
        dialog.TrapFocus = true;
        UiFocusScope[] scopes = new UiFocusScope[] { page, dialog };
        UiFocusTarget[] targets = new UiFocusTarget[]
        {
            Target("trigger", "page", 0),
            Target("cancel", "dialog", 0),
            Target("confirm", "dialog", 0)
        };
        FocusEngine engine = new FocusEngine();

        UiFocusResult entered = engine.ResolveFocus(scopes, targets, new UiFocusState(), new UiFocusRequest { Operation = UiFocusOperation.Focus, ScopeId = "dialog" });
        UiFocusResult blurred = engine.ResolveFocus(scopes, targets, entered.State, new UiFocusRequest { Operation = UiFocusOperation.Blur });
        UiFocusResult restored = engine.ResolveFocus(scopes, targets, entered.State, new UiFocusRequest { Operation = UiFocusOperation.Restore, ScopeId = "dialog", Reveal = true });

        Assert.Multiple(() =>
        {
            Assert.That(entered.State.FocusedNodeId, Is.EqualTo("cancel"));
            Assert.That(entered.State.IsTrapped, Is.True);
            Assert.That(blurred.Success, Is.True);
            Assert.That(blurred.State.FocusedNodeId, Is.Null);
            Assert.That(blurred.State.ScopeId, Is.Null);
            Assert.That(restored.Success, Is.True);
            Assert.That(restored.State.FocusedNodeId, Is.EqualTo("trigger"));
            Assert.That(restored.State.ScopeId, Is.EqualTo("page"));
            Assert.That(restored.RevealNodeId, Is.EqualTo("trigger"));
        });
    }

    [Test]
    public void TrapsExternalFocusAndReportsMalformedScopeStrategies()
    {
        UiFocusScope page = Scope("page", "page-owner", new string[] { "outside" });
        UiFocusScope dialog = Scope("dialog", "dialog-owner", new string[] { "inside" });
        dialog.TrapFocus = true;
        UiFocusTarget[] targets = new UiFocusTarget[]
        {
            Target("outside", "page", 0),
            Target("inside", "dialog", 0)
        };
        UiFocusState state = new UiFocusState { FocusedNodeId = "inside", ScopeId = "dialog", IsTrapped = true };
        UiFocusRequest request = new UiFocusRequest { Operation = UiFocusOperation.Focus, TargetId = "outside" };

        UiFocusResult trapped = new FocusEngine().ResolveFocus(new UiFocusScope[] { page, dialog }, targets, state, request);
        dialog.RovingTabIndex = true;
        dialog.UseActiveDescendant = true;
        UiFocusResult malformed = new FocusEngine().ResolveFocus(new UiFocusScope[] { page, dialog }, targets, state, request);

        Assert.Multiple(() =>
        {
            Assert.That(trapped.Success, Is.False);
            Assert.That(trapped.Diagnostics.Select(item => item.Code), Does.Contain("focus.trapped-target"));
            Assert.That(trapped.State.FocusedNodeId, Is.EqualTo("inside"));
            Assert.That(malformed.Success, Is.False);
            Assert.That(malformed.Diagnostics.Select(item => item.Code), Does.Contain("focus.conflicting-strategy"));
        });

        page.ParentScopeId = "dialog";
        dialog.ParentScopeId = "page";
        UiFocusResult cyclic = new FocusEngine().ResolveFocus(new UiFocusScope[] { page, dialog }, targets, state, request);
        Assert.That(cyclic.Diagnostics.Select(item => item.Code), Does.Contain("focus.scope-cycle"));
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
}
