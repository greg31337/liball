using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class FocusEngineCoverageTests
{
    [TestCase(UiFocusDirection.First, "one")]
    [TestCase(UiFocusDirection.Last, "three")]
    [TestCase(UiFocusDirection.Next, "three")]
    [TestCase(UiFocusDirection.Down, "three")]
    [TestCase(UiFocusDirection.Right, "three")]
    [TestCase(UiFocusDirection.Previous, "one")]
    [TestCase(UiFocusDirection.Up, "one")]
    [TestCase(UiFocusDirection.Left, "one")]
    public void AppliesEveryLogicalFocusDirection(UiFocusDirection direction, string expected)
    {
        UiFocusScope scope = Scope("scope", "owner", new string[] { "one", "two", "three" });
        UiFocusTarget[] targets = Targets("scope", "one", "two", "three");
        UiFocusState state = new UiFocusState { FocusedNodeId = "two", ScopeId = "scope" };

        UiFocusResult result = new FocusEngine().ResolveFocus(
            new UiFocusScope[] { scope },
            targets,
            state,
            new UiFocusRequest { Operation = UiFocusOperation.Move, Direction = direction });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.State.FocusedNodeId, Is.EqualTo(expected));
        });
    }

    [Test]
    public void OrdersPositiveTabIndexesBeforeSourceOrderAndSkipsIneligibleTargets()
    {
        UiFocusScope scope = Scope("scope", "owner", new string[] { "zero", "negative", "disabled", "hidden", "not-focusable", "later", "first" });
        UiFocusTarget zero = Target("zero", "scope", 0);
        UiFocusTarget negative = Target("negative", "scope", -1);
        UiFocusTarget disabled = Target("disabled", "scope", 1);
        disabled.IsEnabled = false;
        UiFocusTarget hidden = Target("hidden", "scope", 1);
        hidden.IsVisible = false;
        UiFocusTarget notFocusable = Target("not-focusable", "scope", 1);
        notFocusable.IsFocusable = false;
        UiFocusTarget later = Target("later", "scope", 3);
        UiFocusTarget first = Target("first", "scope", 1);
        FocusEngine engine = new FocusEngine();
        UiFocusTarget[] targets = new UiFocusTarget[] { zero, negative, disabled, hidden, notFocusable, later, first };

        UiFocusResult initial = engine.ResolveFocus(
            new UiFocusScope[] { scope },
            targets,
            new UiFocusState(),
            new UiFocusRequest { Operation = UiFocusOperation.Focus, ScopeId = "scope" });
        UiFocusResult next = engine.ResolveFocus(
            new UiFocusScope[] { scope },
            targets,
            initial.State,
            new UiFocusRequest { Operation = UiFocusOperation.Move, Direction = UiFocusDirection.Next });
        UiFocusResult last = engine.ResolveFocus(
            new UiFocusScope[] { scope },
            targets,
            next.State,
            new UiFocusRequest { Operation = UiFocusOperation.Move, Direction = UiFocusDirection.Last });

        Assert.Multiple(() =>
        {
            Assert.That(initial.State.FocusedNodeId, Is.EqualTo("first"));
            Assert.That(next.State.FocusedNodeId, Is.EqualTo("later"));
            Assert.That(last.State.FocusedNodeId, Is.EqualTo("zero"));
            Assert.That(targets.Select(target => target.TabIndex), Is.EqualTo(new int[] { 0, -1, 1, 1, 1, 3, 1 }));
        });
    }

    [Test]
    public void ClampsUnwrappedBoundariesAndWrapsTrappedBoundaries()
    {
        UiFocusScope ordinary = Scope("ordinary", "ordinary-owner", new string[] { "first", "last" });
        UiFocusTarget[] ordinaryTargets = Targets("ordinary", "first", "last");
        UiFocusState first = new UiFocusState { FocusedNodeId = "first", ScopeId = "ordinary" };
        UiFocusState last = new UiFocusState { FocusedNodeId = "last", ScopeId = "ordinary" };
        FocusEngine engine = new FocusEngine();

        UiFocusResult clampedPrevious = Move(engine, ordinary, ordinaryTargets, first, UiFocusDirection.Previous);
        UiFocusResult clampedNext = Move(engine, ordinary, ordinaryTargets, last, UiFocusDirection.Next);
        ordinary.TrapFocus = true;
        UiFocusResult wrappedPrevious = Move(engine, ordinary, ordinaryTargets, first, UiFocusDirection.Previous);
        UiFocusResult wrappedNext = Move(engine, ordinary, ordinaryTargets, last, UiFocusDirection.Next);

        Assert.Multiple(() =>
        {
            Assert.That(clampedPrevious.State.FocusedNodeId, Is.EqualTo("first"));
            Assert.That(clampedNext.State.FocusedNodeId, Is.EqualTo("last"));
            Assert.That(wrappedPrevious.State.FocusedNodeId, Is.EqualTo("last"));
            Assert.That(wrappedPrevious.State.IsTrapped, Is.True);
            Assert.That(wrappedNext.State.FocusedNodeId, Is.EqualTo("first"));
            Assert.That(wrappedNext.State.IsTrapped, Is.True);
        });
    }

    [Test]
    public void SelectsNestedScopesFromExplicitTargetsAndRejectsAmbiguousRequests()
    {
        UiFocusScope page = Scope("page", "page-owner", new string[] { "page-target" });
        UiFocusScope dialog = Scope("dialog", "dialog-owner", new string[] { "dialog-target" });
        dialog.ParentScopeId = "page";
        UiFocusScope[] scopes = new UiFocusScope[] { page, dialog };
        UiFocusTarget[] targets = new UiFocusTarget[]
        {
            Target("page-target", "page", 0),
            Target("dialog-target", "dialog", 0)
        };
        FocusEngine engine = new FocusEngine();

        UiFocusResult explicitTarget = engine.ResolveFocus(
            scopes,
            targets,
            new UiFocusState(),
            new UiFocusRequest { Operation = UiFocusOperation.Focus, TargetId = "dialog-target" });
        UiFocusResult ambiguous = engine.ResolveFocus(
            scopes,
            targets,
            new UiFocusState(),
            new UiFocusRequest { Operation = UiFocusOperation.Focus });

        Assert.Multiple(() =>
        {
            Assert.That(explicitTarget.Success, Is.True);
            Assert.That(explicitTarget.State.ScopeId, Is.EqualTo("dialog"));
            Assert.That(explicitTarget.State.FocusedNodeId, Is.EqualTo("dialog-target"));
            Assert.That(ambiguous.Success, Is.False);
            Assert.That(ambiguous.Diagnostics.Single().Code, Is.EqualTo("focus.unknown-scope"));
        });
    }

    [Test]
    public void AppliesAndValidatesExplicitActiveDescendantRequests()
    {
        UiFocusScope composite = Scope("list", "list-owner", new string[] { "alpha", "beta" });
        composite.UseActiveDescendant = true;
        UiFocusScope ordinary = Scope("ordinary", "ordinary-owner", new string[] { "ordinary-target" });
        UiFocusTarget[] targets = new UiFocusTarget[]
        {
            Target("alpha", "list", -1),
            Target("beta", "list", -1),
            Target("ordinary-target", "ordinary", 0)
        };
        FocusEngine engine = new FocusEngine();

        UiFocusResult applied = engine.ResolveFocus(
            new UiFocusScope[] { composite, ordinary },
            targets,
            new UiFocusState { FocusedNodeId = "list-owner", ActiveDescendantId = "alpha", ScopeId = "list" },
            new UiFocusRequest { Operation = UiFocusOperation.SetActiveDescendant, TargetId = "beta", Reveal = true });
        UiFocusResult wrongStrategy = engine.ResolveFocus(
            new UiFocusScope[] { composite, ordinary },
            targets,
            new UiFocusState { FocusedNodeId = "ordinary-target", ScopeId = "ordinary" },
            new UiFocusRequest { Operation = UiFocusOperation.SetActiveDescendant, TargetId = "ordinary-target" });
        UiFocusResult missingTarget = engine.ResolveFocus(
            new UiFocusScope[] { composite, ordinary },
            targets,
            new UiFocusState { FocusedNodeId = "list-owner", ScopeId = "list" },
            new UiFocusRequest { Operation = UiFocusOperation.SetActiveDescendant });

        Assert.Multiple(() =>
        {
            Assert.That(applied.Success, Is.True);
            Assert.That(applied.State.FocusedNodeId, Is.EqualTo("list-owner"));
            Assert.That(applied.State.ActiveDescendantId, Is.EqualTo("beta"));
            Assert.That(applied.RevealNodeId, Is.EqualTo("beta"));
            Assert.That(wrongStrategy.Diagnostics.Single().Code, Is.EqualTo("focus.invalid-active-descendant"));
            Assert.That(missingTarget.Diagnostics.Single().Code, Is.EqualTo("focus.unknown-target"));
        });
    }

    [TestCase(false, "one")]
    [TestCase(true, "one")]
    public void FallsBackToTheFirstEligibleTargetAfterTheCurrentTargetIsRemoved(bool activeDescendant, string expected)
    {
        UiFocusScope scope = Scope("scope", "owner", new string[] { "one", "two" });
        scope.UseActiveDescendant = activeDescendant;
        UiFocusTarget[] remaining = Targets("scope", "one", "two");
        UiFocusState state = new UiFocusState
        {
            FocusedNodeId = activeDescendant ? "owner" : "removed",
            ActiveDescendantId = activeDescendant ? "removed" : null,
            ScopeId = "scope"
        };

        UiFocusResult result = new FocusEngine().ResolveFocus(
            new UiFocusScope[] { scope },
            remaining,
            state,
            new UiFocusRequest { Operation = UiFocusOperation.Move, Direction = UiFocusDirection.Next });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(activeDescendant ? result.State.ActiveDescendantId : result.State.FocusedNodeId, Is.EqualTo(expected));
            Assert.That(result.State.FocusedNodeId, Is.EqualTo(activeDescendant ? "owner" : "one"));
        });
    }

    [Test]
    public void UsesActiveTargetThenSourceOrderWhenNoInitialTargetIsConfigured()
    {
        UiFocusScope active = Scope("active", "active-owner", new string[] { "one", "two" });
        active.ActiveTargetId = "two";
        UiFocusScope source = Scope("source", "source-owner", new string[] { "source-one", "source-two" });
        FocusEngine engine = new FocusEngine();

        UiFocusResult activeResult = engine.ResolveFocus(
            new UiFocusScope[] { active },
            Targets("active", "one", "two"),
            new UiFocusState(),
            new UiFocusRequest { Operation = UiFocusOperation.Focus });
        UiFocusResult sourceResult = engine.ResolveFocus(
            new UiFocusScope[] { source },
            Targets("source", "source-one", "source-two"),
            new UiFocusState(),
            new UiFocusRequest { Operation = UiFocusOperation.Focus });

        Assert.Multiple(() =>
        {
            Assert.That(activeResult.State.FocusedNodeId, Is.EqualTo("two"));
            Assert.That(sourceResult.State.FocusedNodeId, Is.EqualTo("source-one"));
        });
    }

    private static UiFocusResult Move(FocusEngine engine, UiFocusScope scope, UiFocusTarget[] targets, UiFocusState state, UiFocusDirection direction)
    {
        return engine.ResolveFocus(
            new UiFocusScope[] { scope },
            targets,
            state,
            new UiFocusRequest { Operation = UiFocusOperation.Move, Direction = direction });
    }

    private static UiFocusScope Scope(string id, string ownerId, string[] targetIds)
    {
        return new UiFocusScope { Id = id, OwnerNodeId = ownerId, TargetIds = targetIds };
    }

    private static UiFocusTarget[] Targets(string scopeId, params string[] ids)
    {
        UiFocusTarget[] result = new UiFocusTarget[ids.Length];
        int index = 0;
        while (index < ids.Length)
        {
            result[index] = Target(ids[index], scopeId, 0);
            index++;
        }

        return result;
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
