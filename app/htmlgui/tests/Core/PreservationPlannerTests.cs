using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class PreservationPlannerTests
{
    [Test]
    public void InsertPreservesFocusedEditingAndScrollStateWithoutMutatingInputs()
    {
        DomNode input = Input("input", HtmlInputTypes.Text);
        DomNode scroll = Element("scroll", HtmlElements.Division);
        DomSnapshot before = Snapshot(Element("root", HtmlElements.Division, input, scroll));
        DomNode inserted = Element("inserted", HtmlElements.Span);
        DomSnapshot after = Snapshot(Element("root", HtmlElements.Division, inserted, input, scroll));
        UiPreservationState current = EditingState("input", "scroll");
        DomMutation mutation = new DomMutation
        {
            Kind = UiMutationKind.Insert,
            TargetId = "inserted",
            ParentId = "root",
            Node = inserted
        };

        UiPreservationState result = new PreservationPlanner().CreatePreservationPlan(before, after, new DomMutation[] { mutation }, current);

        Assert.Multiple(() =>
        {
            Assert.That(result.PreserveFocus, Is.True);
            Assert.That(result.FocusedNodeId, Is.EqualTo("input"));
            Assert.That(result.PreserveSelection, Is.True);
            Assert.That(result.HasSelection, Is.True);
            Assert.That(result.SelectionStart, Is.EqualTo(1));
            Assert.That(result.SelectionEnd, Is.EqualTo(3));
            Assert.That(result.PreserveComposition, Is.True);
            Assert.That(result.IsComposing, Is.True);
            Assert.That(result.PreserveScroll, Is.True);
            Assert.That(result.ScrollOffsets.Select(item => item.Key), Is.EqualTo(new string[] { "scroll" }));
            Assert.That(result.OpenOverlayIds, Is.EqualTo(new string[] { "dialog" }));
            Assert.That(result.ScrollOffsets, Is.Not.SameAs(current.ScrollOffsets));
            Assert.That(result.OpenOverlayIds, Is.Not.SameAs(current.OpenOverlayIds));
            Assert.That(current.IsComposing, Is.True);
        });
    }

    [Test]
    public void MovePreservesStableNodeFocusSelectionCompositionAndScroll()
    {
        DomNode input = Input("input", HtmlInputTypes.Search);
        DomNode scroll = Element("scroll", HtmlElements.Division);
        DomSnapshot before = Snapshot(Element("root", HtmlElements.Division, input, scroll));
        DomSnapshot after = Snapshot(Element("root", HtmlElements.Division, scroll, input));
        DomMutation mutation = new DomMutation
        {
            Kind = UiMutationKind.Move,
            TargetId = "input",
            ParentId = "root",
            HasFromIndex = true,
            FromIndex = 0,
            HasToIndex = true,
            ToIndex = 1
        };

        UiPreservationState result = new PreservationPlanner().CreatePreservationPlan(before, after, new DomMutation[] { mutation }, EditingState("input", "scroll"));

        Assert.Multiple(() =>
        {
            Assert.That(result.PreserveFocus, Is.True);
            Assert.That(result.PreserveSelection, Is.True);
            Assert.That(result.PreserveComposition, Is.True);
            Assert.That(result.PreserveScroll, Is.True);
            Assert.That(result.FocusedNodeId, Is.EqualTo("input"));
        });
    }

    [Test]
    public void RemovingFocusedSubtreeClearsEditingStateAndRemovedScrollOffsets()
    {
        DomNode input = Input("input", HtmlInputTypes.Text);
        DomNode removedScroll = Element("removed-scroll", HtmlElements.Division);
        DomNode wrapper = Element("wrapper", HtmlElements.Division, input, removedScroll);
        DomNode survivingScroll = Element("surviving-scroll", HtmlElements.Division);
        DomSnapshot before = Snapshot(Element("root", HtmlElements.Division, wrapper, survivingScroll));
        DomSnapshot after = Snapshot(Element("root", HtmlElements.Division, survivingScroll));
        UiPreservationState current = EditingState("input", "removed-scroll");
        current.ScrollOffsets = new UiValueEntry[]
        {
            Offset("removed-scroll", 12),
            Offset("surviving-scroll", 24)
        };
        DomMutation mutation = new DomMutation { Kind = UiMutationKind.Remove, TargetId = "wrapper" };

        UiPreservationState result = new PreservationPlanner().CreatePreservationPlan(before, after, new DomMutation[] { mutation }, current);

        Assert.Multiple(() =>
        {
            Assert.That(result.PreserveFocus, Is.False);
            Assert.That(result.FocusedNodeId, Is.Null);
            Assert.That(result.PreserveSelection, Is.False);
            Assert.That(result.HasSelection, Is.False);
            Assert.That(result.SelectionStart, Is.Zero);
            Assert.That(result.SelectionEnd, Is.Zero);
            Assert.That(result.PreserveComposition, Is.False);
            Assert.That(result.IsComposing, Is.False);
            Assert.That(result.PreserveScroll, Is.True);
            Assert.That(result.ScrollOffsets.Select(item => item.Key), Is.EqualTo(new string[] { "surviving-scroll" }));
            Assert.That(result.ScrollOffsets[0].Value.IntegerValue, Is.EqualTo(24));
            Assert.That(current.ScrollOffsets, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void CompatibleReplacementRestoresFocusSelectionAndScrollButEndsComposition()
    {
        DomNode beforeInput = Input("input", HtmlInputTypes.Email);
        DomNode afterInput = Input("input", HtmlInputTypes.Email);
        DomSnapshot before = Snapshot(Element("root", HtmlElements.Division, beforeInput));
        DomSnapshot after = Snapshot(Element("root", HtmlElements.Division, afterInput));
        DomMutation mutation = new DomMutation { Kind = UiMutationKind.Replace, TargetId = "input", Node = afterInput };

        UiPreservationState result = new PreservationPlanner().CreatePreservationPlan(before, after, new DomMutation[] { mutation }, EditingState("input", "input"));

        Assert.Multiple(() =>
        {
            Assert.That(result.PreserveFocus, Is.True);
            Assert.That(result.FocusedNodeId, Is.EqualTo("input"));
            Assert.That(result.PreserveSelection, Is.True);
            Assert.That(result.HasSelection, Is.True);
            Assert.That(result.PreserveComposition, Is.False);
            Assert.That(result.IsComposing, Is.False);
            Assert.That(result.PreserveScroll, Is.True);
            Assert.That(result.ScrollOffsets.Single().Key, Is.EqualTo("input"));
        });
    }

    [Test]
    public void IncompatibleReplacementRestoresFocusButDropsSelectionAndComposition()
    {
        DomNode beforeInput = Input("input", HtmlInputTypes.Text);
        DomNode afterNode = Element("input", HtmlElements.Division);
        DomSnapshot before = Snapshot(Element("root", HtmlElements.Division, beforeInput));
        DomSnapshot after = Snapshot(Element("root", HtmlElements.Division, afterNode));
        DomMutation mutation = new DomMutation { Kind = UiMutationKind.Replace, TargetId = "input", Node = afterNode };

        UiPreservationState result = new PreservationPlanner().CreatePreservationPlan(before, after, new DomMutation[] { mutation }, EditingState("input", "input"));

        Assert.Multiple(() =>
        {
            Assert.That(result.PreserveFocus, Is.True);
            Assert.That(result.FocusedNodeId, Is.EqualTo("input"));
            Assert.That(result.PreserveSelection, Is.False);
            Assert.That(result.HasSelection, Is.False);
            Assert.That(result.SelectionStart, Is.Zero);
            Assert.That(result.SelectionEnd, Is.Zero);
            Assert.That(result.PreserveComposition, Is.False);
            Assert.That(result.IsComposing, Is.False);
        });
    }

    [Test]
    public void RejectsNullInputsUnknownMutationKindsAndMissingStructuralTargets()
    {
        PreservationPlanner planner = new PreservationPlanner();
        DomSnapshot snapshot = Snapshot(Element("root", HtmlElements.Division));
        UiPreservationState state = new UiPreservationState();

        Assert.Multiple(() =>
        {
            Assert.That(() => planner.CreatePreservationPlan(null, snapshot, Array.Empty<DomMutation>(), state), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("before"));
            Assert.That(() => planner.CreatePreservationPlan(snapshot, null, Array.Empty<DomMutation>(), state), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("after"));
            Assert.That(() => planner.CreatePreservationPlan(snapshot, snapshot, null, state), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("mutations"));
            Assert.That(() => planner.CreatePreservationPlan(snapshot, snapshot, Array.Empty<DomMutation>(), null), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("current"));
            Assert.That(() => planner.CreatePreservationPlan(snapshot, snapshot, new DomMutation[] { new DomMutation { Kind = (UiMutationKind)99 } }, state), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => planner.CreatePreservationPlan(snapshot, snapshot, new DomMutation[] { new DomMutation { Kind = UiMutationKind.Remove } }, state), Throws.ArgumentException);
        });
    }

    private static DomSnapshot Snapshot(params DomNode[] roots)
    {
        return new DomSnapshot { Roots = roots };
    }

    private static DomNode Element(string id, string name, params DomNode[] children)
    {
        return new DomNode { Kind = DomNodeKind.Element, Id = id, Name = name, Children = children };
    }

    private static DomNode Input(string id, string type)
    {
        DomNode result = Element(id, HtmlElements.Input);
        result.Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Type, Value = type } };
        return result;
    }

    private static UiPreservationState EditingState(string focusedId, string scrollId)
    {
        return new UiPreservationState
        {
            FocusedNodeId = focusedId,
            HasSelection = true,
            SelectionStart = 1,
            SelectionEnd = 3,
            IsComposing = true,
            ScrollOffsets = new UiValueEntry[] { Offset(scrollId, 12) },
            OpenOverlayIds = new string[] { "dialog" }
        };
    }

    private static UiValueEntry Offset(string id, int value)
    {
        return new UiValueEntry
        {
            Key = id,
            Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = value }
        };
    }
}
