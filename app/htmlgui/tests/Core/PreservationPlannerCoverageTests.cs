using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class PreservationPlannerCoverageTests
{
    [TestCase(HtmlInputTypes.Text)]
    [TestCase(HtmlInputTypes.Search)]
    [TestCase(HtmlInputTypes.Email)]
    [TestCase(HtmlInputTypes.Url)]
    [TestCase(HtmlInputTypes.Telephone)]
    [TestCase(HtmlInputTypes.Password)]
    public void PreservesSelectionForEverySupportedTextInputType(string inputType)
    {
        DomSnapshot before = Snapshot(Input("field", inputType));
        DomSnapshot after = Snapshot(Input("field", inputType));

        UiPreservationState result = new PreservationPlanner().CreatePreservationPlan(
            before,
            after,
            Array.Empty<DomMutation>(),
            EditingState("field"));

        Assert.Multiple(() =>
        {
            Assert.That(result.PreserveFocus, Is.True);
            Assert.That(result.PreserveSelection, Is.True);
            Assert.That(result.PreserveComposition, Is.True);
        });
    }

    [Test]
    public void PreservesTextareaAndContentEditableSelections()
    {
        DomNode beforeArea = Element("area", HtmlElements.TextArea);
        DomNode afterArea = Element("area", HtmlElements.TextArea);
        DomNode beforeEditable = Element("editable", HtmlElements.Division);
        beforeEditable.Attributes = new DomAttribute[] { Attribute("contenteditable", "true") };
        DomNode afterEditable = Element("editable", HtmlElements.Division);
        afterEditable.Attributes = new DomAttribute[] { Attribute("contenteditable", "true") };
        PreservationPlanner planner = new PreservationPlanner();

        UiPreservationState area = planner.CreatePreservationPlan(Snapshot(beforeArea), Snapshot(afterArea), Array.Empty<DomMutation>(), EditingState("area"));
        UiPreservationState editable = planner.CreatePreservationPlan(Snapshot(beforeEditable), Snapshot(afterEditable), Array.Empty<DomMutation>(), EditingState("editable"));

        Assert.Multiple(() =>
        {
            Assert.That(area.PreserveSelection, Is.True);
            Assert.That(area.PreserveComposition, Is.True);
            Assert.That(editable.PreserveSelection, Is.True);
            Assert.That(editable.PreserveComposition, Is.True);
        });
    }

    [TestCase(HtmlInputTypes.Number)]
    [TestCase(HtmlInputTypes.Range)]
    [TestCase(HtmlInputTypes.Date)]
    [TestCase(HtmlInputTypes.Time)]
    [TestCase(HtmlInputTypes.Color)]
    public void DropsSelectionForUnsupportedInputTypes(string inputType)
    {
        UiPreservationState result = new PreservationPlanner().CreatePreservationPlan(
            Snapshot(Input("field", inputType)),
            Snapshot(Input("field", inputType)),
            Array.Empty<DomMutation>(),
            EditingState("field"));

        Assert.Multiple(() =>
        {
            Assert.That(result.PreserveFocus, Is.True);
            Assert.That(result.PreserveSelection, Is.False);
            Assert.That(result.PreserveComposition, Is.False);
            Assert.That(result.HasSelection, Is.False);
            Assert.That(result.SelectionStart, Is.Zero);
            Assert.That(result.SelectionEnd, Is.Zero);
        });
    }

    [Test]
    public void DropsSelectionWhenEditableHostsChangeTypeOrEditability()
    {
        DomNode beforeEditable = Element("editable", HtmlElements.Division);
        beforeEditable.Attributes = new DomAttribute[] { Attribute("contenteditable", "true") };
        DomNode afterEditable = Element("editable", HtmlElements.Division);
        PreservationPlanner planner = new PreservationPlanner();

        UiPreservationState changedType = planner.CreatePreservationPlan(
            Snapshot(Input("field", HtmlInputTypes.Text)),
            Snapshot(Input("field", HtmlInputTypes.Email)),
            Array.Empty<DomMutation>(),
            EditingState("field"));
        UiPreservationState disabledEditing = planner.CreatePreservationPlan(
            Snapshot(beforeEditable),
            Snapshot(afterEditable),
            Array.Empty<DomMutation>(),
            EditingState("editable"));

        Assert.Multiple(() =>
        {
            Assert.That(changedType.PreserveFocus, Is.True);
            Assert.That(changedType.PreserveSelection, Is.False);
            Assert.That(disabledEditing.PreserveFocus, Is.True);
            Assert.That(disabledEditing.PreserveSelection, Is.False);
        });
    }

    [TestCase(false, 1, 3)]
    [TestCase(true, -1, 3)]
    [TestCase(true, 4, 3)]
    public void RejectsAbsentOrInvalidSelectionRanges(bool hasSelection, int start, int end)
    {
        UiPreservationState state = EditingState("field");
        state.HasSelection = hasSelection;
        state.SelectionStart = start;
        state.SelectionEnd = end;

        UiPreservationState result = new PreservationPlanner().CreatePreservationPlan(
            Snapshot(Input("field", HtmlInputTypes.Text)),
            Snapshot(Input("field", HtmlInputTypes.Text)),
            Array.Empty<DomMutation>(),
            state);

        Assert.Multiple(() =>
        {
            Assert.That(result.PreserveFocus, Is.True);
            Assert.That(result.PreserveSelection, Is.False);
            Assert.That(result.PreserveComposition, Is.False);
            Assert.That(result.HasSelection, Is.False);
            Assert.That(result.SelectionStart, Is.Zero);
            Assert.That(result.SelectionEnd, Is.Zero);
        });
    }

    [Test]
    public void PreservesFocusedEditingAcrossEveryNonstructuralPatchKind()
    {
        UiMutationKind[] kinds = new UiMutationKind[]
        {
            UiMutationKind.SetText,
            UiMutationKind.SetAttribute,
            UiMutationKind.RemoveAttribute,
            UiMutationKind.SetProperty,
            UiMutationKind.RemoveProperty,
            UiMutationKind.AddClass,
            UiMutationKind.RemoveClass,
            UiMutationKind.SetStyle,
            UiMutationKind.RemoveStyle,
            UiMutationKind.SubscribeEvent,
            UiMutationKind.UnsubscribeEvent
        };
        DomSnapshot before = Snapshot(Input("field", HtmlInputTypes.Text));
        DomSnapshot after = Snapshot(Input("field", HtmlInputTypes.Text));
        PreservationPlanner planner = new PreservationPlanner();

        foreach (UiMutationKind kind in kinds)
        {
            UiPreservationState result = planner.CreatePreservationPlan(
                before,
                after,
                new DomMutation[] { new DomMutation { Kind = kind, TargetId = "field" } },
                EditingState("field"));

            Assert.Multiple(() =>
            {
                Assert.That(result.PreserveFocus, Is.True, kind.ToString());
                Assert.That(result.PreserveSelection, Is.True, kind.ToString());
                Assert.That(result.PreserveComposition, Is.True, kind.ToString());
            });
        }
    }

    [Test]
    public void RemovingAnUnrelatedSiblingPreservesFocusedEditing()
    {
        DomNode field = Input("field", HtmlInputTypes.Text);
        DomSnapshot before = Snapshot(Element("root", HtmlElements.Division, field, Element("sibling", HtmlElements.Span)));
        DomSnapshot after = Snapshot(Element("root", HtmlElements.Division, Input("field", HtmlInputTypes.Text)));

        UiPreservationState result = new PreservationPlanner().CreatePreservationPlan(
            before,
            after,
            new DomMutation[] { new DomMutation { Kind = UiMutationKind.Remove, TargetId = "sibling" } },
            EditingState("field"));

        Assert.Multiple(() =>
        {
            Assert.That(result.PreserveFocus, Is.True);
            Assert.That(result.PreserveSelection, Is.True);
            Assert.That(result.PreserveComposition, Is.True);
        });
    }

    [Test]
    public void ReplacingAnAncestorEndsCompositionButPreservesCompatibleEditingState()
    {
        DomSnapshot before = Snapshot(Element("wrapper", HtmlElements.Division, Input("field", HtmlInputTypes.Text)));
        DomNode replacement = Element("wrapper", HtmlElements.Division, Input("field", HtmlInputTypes.Text));
        DomSnapshot after = Snapshot(replacement);

        UiPreservationState result = new PreservationPlanner().CreatePreservationPlan(
            before,
            after,
            new DomMutation[] { new DomMutation { Kind = UiMutationKind.Replace, TargetId = "wrapper", Node = replacement } },
            EditingState("field"));

        Assert.Multiple(() =>
        {
            Assert.That(result.PreserveFocus, Is.True);
            Assert.That(result.PreserveSelection, Is.True);
            Assert.That(result.PreserveComposition, Is.False);
            Assert.That(result.IsComposing, Is.False);
        });
    }

    [Test]
    public void ClearsFocusWhenTheFocusedIdentifierIsMissingBeforeOrAfterThePatch()
    {
        PreservationPlanner planner = new PreservationPlanner();
        UiPreservationState missingBefore = planner.CreatePreservationPlan(
            Snapshot(Element("other", HtmlElements.Division)),
            Snapshot(Input("field", HtmlInputTypes.Text)),
            Array.Empty<DomMutation>(),
            EditingState("field"));
        UiPreservationState missingAfter = planner.CreatePreservationPlan(
            Snapshot(Input("field", HtmlInputTypes.Text)),
            Snapshot(Element("other", HtmlElements.Division)),
            Array.Empty<DomMutation>(),
            EditingState("field"));
        UiPreservationState noIdentifier = planner.CreatePreservationPlan(
            Snapshot(Input("field", HtmlInputTypes.Text)),
            Snapshot(Input("field", HtmlInputTypes.Text)),
            Array.Empty<DomMutation>(),
            new UiPreservationState());

        Assert.Multiple(() =>
        {
            Assert.That(missingBefore.PreserveFocus, Is.False);
            Assert.That(missingBefore.FocusedNodeId, Is.Null);
            Assert.That(missingAfter.PreserveFocus, Is.False);
            Assert.That(missingAfter.FocusedNodeId, Is.Null);
            Assert.That(noIdentifier.PreserveFocus, Is.False);
            Assert.That(noIdentifier.FocusedNodeId, Is.Null);
        });
    }

    [Test]
    public void FiltersNullAndRemovedScrollEntriesAndCopiesOverlayIdentifiers()
    {
        UiPreservationState state = EditingState("field");
        state.ScrollOffsets = new UiValueEntry[]
        {
            null!,
            Offset("kept", 10),
            Offset("removed", 20)
        };
        state.OpenOverlayIds = new string[] { "dialog", "popover" };
        DomSnapshot snapshot = Snapshot(Input("field", HtmlInputTypes.Text), Element("kept", HtmlElements.Division));

        UiPreservationState result = new PreservationPlanner().CreatePreservationPlan(
            snapshot,
            snapshot,
            Array.Empty<DomMutation>(),
            state);

        Assert.Multiple(() =>
        {
            Assert.That(result.PreserveScroll, Is.True);
            Assert.That(result.ScrollOffsets.Select(entry => entry.Key), Is.EqualTo(new string[] { "kept" }));
            Assert.That(result.OpenOverlayIds, Is.EqualTo(new string[] { "dialog", "popover" }));
            Assert.That(result.OpenOverlayIds, Is.Not.SameAs(state.OpenOverlayIds));
        });
    }

    [Test]
    public void RejectsNullMutationsInsideThePatchCollection()
    {
        DomSnapshot snapshot = Snapshot(Input("field", HtmlInputTypes.Text));

        Assert.That(
            () => new PreservationPlanner().CreatePreservationPlan(
                snapshot,
                snapshot,
                new DomMutation[] { null! },
                EditingState("field")),
            Throws.ArgumentException.With.Property("ParamName").EqualTo("mutations"));
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
        result.Attributes = new DomAttribute[] { Attribute(HtmlAttributes.Type, type) };
        return result;
    }

    private static DomAttribute Attribute(string name, string value)
    {
        return new DomAttribute { Name = name, Value = value };
    }

    private static UiPreservationState EditingState(string focusedId)
    {
        return new UiPreservationState
        {
            FocusedNodeId = focusedId,
            HasSelection = true,
            SelectionStart = 1,
            SelectionEnd = 3,
            IsComposing = true
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
