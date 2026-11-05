using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class PreservationPlannerStateTests
{
    [Test]
    public void PreservesStableScrollAnchorsAndDeepCopiesCapturedOffsets()
    {
        DomSnapshot before = Snapshot(Element("root", HtmlElements.Division, Element("anchor", HtmlElements.Division)));
        DomSnapshot after = Snapshot(Element("root", HtmlElements.Division, Element("anchor", HtmlElements.Division)));
        UiValue offsetValue = ObjectValue("top", IntegerValue(20));
        UiPreservationState current = new UiPreservationState
        {
            ScrollAnchorNodeId = "anchor",
            ScrollOffsets = new UiValueEntry[] { Entry("anchor", offsetValue) }
        };

        UiPreservationState preserved = new PreservationPlanner().CreatePreservationPlan(before, after, Array.Empty<DomMutation>(), current);
        offsetValue.ObjectValue![0].Value.IntegerValue = 99;
        UiPreservationState removed = new PreservationPlanner().CreatePreservationPlan(before, Snapshot(Element("root", HtmlElements.Division)), Array.Empty<DomMutation>(), current);

        Assert.Multiple(() =>
        {
            Assert.That(preserved.PreserveScroll, Is.True);
            Assert.That(preserved.PreserveScrollAnchor, Is.True);
            Assert.That(preserved.ScrollAnchorNodeId, Is.EqualTo("anchor"));
            Assert.That(preserved.ScrollOffsets[0].Value.ObjectValue![0].Value.IntegerValue, Is.EqualTo(20));
            Assert.That(preserved.ScrollOffsets[0], Is.Not.SameAs(current.ScrollOffsets[0]));
            Assert.That(removed.PreserveScroll, Is.False);
            Assert.That(removed.PreserveScrollAnchor, Is.False);
            Assert.That(removed.ScrollAnchorNodeId, Is.Null);
        });
    }

    [Test]
    public void PreservesCompatibleMediaPlaybackAcrossUnrelatedAndReplacementPatches()
    {
        DomNode beforeVideo = Media("video", HtmlElements.Video, "/movie.mp4");
        DomNode afterVideo = Media("video", HtmlElements.Video, "/movie.mp4");
        UiValue playback = ObjectValue("currentTime", FloatingPointValue(12.5));
        UiPreservationState current = new UiPreservationState
        {
            MediaPlaybackStates = new UiValueEntry[] { Entry("video", playback) }
        };
        DomMutation unrelated = new DomMutation { Kind = UiMutationKind.AddClass, TargetId = "video", Name = "active" };
        DomMutation replacement = new DomMutation { Kind = UiMutationKind.Replace, TargetId = "video", Node = afterVideo };
        PreservationPlanner planner = new PreservationPlanner();

        UiPreservationState nonstructural = planner.CreatePreservationPlan(Snapshot(beforeVideo), Snapshot(afterVideo), new DomMutation[] { unrelated }, current);
        UiPreservationState replaced = planner.CreatePreservationPlan(Snapshot(beforeVideo), Snapshot(afterVideo), new DomMutation[] { replacement }, current);
        playback.ObjectValue![0].Value.FloatingPointValue = 30;

        Assert.Multiple(() =>
        {
            Assert.That(nonstructural.PreserveMediaPlayback, Is.True);
            Assert.That(nonstructural.MediaPlaybackStates.Single().Key, Is.EqualTo("video"));
            Assert.That(nonstructural.MediaPlaybackStates[0].Value.ObjectValue![0].Value.FloatingPointValue, Is.EqualTo(12.5));
            Assert.That(replaced.PreserveMediaPlayback, Is.True);
            Assert.That(replaced.MediaPlaybackStates.Single().Key, Is.EqualTo("video"));
        });
    }

    [Test]
    public void ModelControlledOrIncompatibleMediaStateWinsOverCapturedPlayback()
    {
        DomNode beforeVideo = Media("video", HtmlElements.Video, "/movie.mp4");
        DomNode sameVideo = Media("video", HtmlElements.Video, "/movie.mp4");
        DomNode stateChangedVideo = Media("video", HtmlElements.Video, "/movie.mp4");
        beforeVideo.Properties = new DomProperty[] { new DomProperty { Name = "currentTime", Value = FloatingPointValue(2) } };
        stateChangedVideo.Properties = new DomProperty[] { new DomProperty { Name = "currentTime", Value = FloatingPointValue(8) } };
        DomNode changedVideo = Media("video", HtmlElements.Video, "/other.mp4");
        UiPreservationState current = new UiPreservationState
        {
            MediaPlaybackStates = new UiValueEntry[] { Entry("video", FloatingPointValue(5)) }
        };
        DomMutation controlled = new DomMutation
        {
            Kind = UiMutationKind.SetProperty,
            TargetId = "video",
            Name = "currentTime",
            Property = new DomProperty { Name = "currentTime", Value = FloatingPointValue(8) }
        };
        PreservationPlanner planner = new PreservationPlanner();

        UiPreservationState modelWins = planner.CreatePreservationPlan(Snapshot(beforeVideo), Snapshot(sameVideo), new DomMutation[] { controlled }, current);
        UiPreservationState modelChangedInsideFallback = planner.CreatePreservationPlan(Snapshot(beforeVideo), Snapshot(stateChangedVideo), new DomMutation[] { new DomMutation { Kind = UiMutationKind.Replace, TargetId = "video", Node = stateChangedVideo } }, current);
        UiPreservationState sourceWins = planner.CreatePreservationPlan(Snapshot(beforeVideo), Snapshot(changedVideo), Array.Empty<DomMutation>(), current);
        UiPreservationState wrongHost = planner.CreatePreservationPlan(Snapshot(beforeVideo), Snapshot(Element("video", HtmlElements.Division)), Array.Empty<DomMutation>(), current);

        Assert.Multiple(() =>
        {
            Assert.That(modelWins.PreserveMediaPlayback, Is.False);
            Assert.That(modelWins.MediaPlaybackStates, Is.Empty);
            Assert.That(modelChangedInsideFallback.PreserveMediaPlayback, Is.False);
            Assert.That(sourceWins.PreserveMediaPlayback, Is.False);
            Assert.That(wrongHost.PreserveMediaPlayback, Is.False);
        });
    }

    [Test]
    public void FiltersRemovedControlledDuplicateAndInvalidOpenOverlayIdentifiers()
    {
        DomNode dialog = Element("dialog", HtmlElements.Dialog);
        DomNode removed = Element("removed", HtmlElements.Dialog);
        DomSnapshot before = Snapshot(Element("root", HtmlElements.Division, dialog, removed));
        DomSnapshot after = Snapshot(Element("root", HtmlElements.Division, Element("dialog", HtmlElements.Dialog)));
        UiPreservationState current = new UiPreservationState
        {
            OpenOverlayIds = new string[] { "dialog", "removed", "external-portal", "dialog", " " }
        };
        PreservationPlanner planner = new PreservationPlanner();

        UiPreservationState preserved = planner.CreatePreservationPlan(before, after, Array.Empty<DomMutation>(), current);
        DomMutation close = new DomMutation { Kind = UiMutationKind.SetProperty, TargetId = "dialog", Name = HtmlAttributes.Open };
        UiPreservationState controlled = planner.CreatePreservationPlan(before, after, new DomMutation[] { close }, current);
        DomNode openedDialog = Element("dialog", HtmlElements.Dialog);
        openedDialog.Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Open, IsBoolean = true } };
        UiPreservationState modelOpened = planner.CreatePreservationPlan(before, Snapshot(Element("root", HtmlElements.Division, openedDialog)), Array.Empty<DomMutation>(), current);

        Assert.Multiple(() =>
        {
            Assert.That(preserved.PreserveOpenOverlays, Is.True);
            Assert.That(preserved.OpenOverlayIds, Is.EqualTo(new string[] { "dialog", "external-portal" }));
            Assert.That(controlled.PreserveOpenOverlays, Is.True);
            Assert.That(controlled.OpenOverlayIds, Is.EqualTo(new string[] { "external-portal" }));
            Assert.That(modelOpened.OpenOverlayIds, Is.EqualTo(new string[] { "external-portal" }));
            Assert.That(current.OpenOverlayIds, Has.Length.EqualTo(5));
        });
    }

    [Test]
    public void PreservesUserEditedFieldsUntilTheModelControlsOrChangesTheHost()
    {
        DomNode beforeInput = Input("field", HtmlInputTypes.Text);
        DomNode sameInput = Input("field", HtmlInputTypes.Text);
        DomNode modelChangedInput = Input("field", HtmlInputTypes.Text);
        beforeInput.Properties = new DomProperty[] { new DomProperty { Name = HtmlAttributes.Value, Value = new UiValue { Kind = UiValueKind.String, StringValue = "model before" } } };
        sameInput.Properties = new DomProperty[] { new DomProperty { Name = HtmlAttributes.Value, Value = new UiValue { Kind = UiValueKind.String, StringValue = "model before" } } };
        modelChangedInput.Properties = new DomProperty[] { new DomProperty { Name = HtmlAttributes.Value, Value = new UiValue { Kind = UiValueKind.String, StringValue = "model after" } } };
        DomNode changedInput = Input("field", HtmlInputTypes.Email);
        UiValue typed = new UiValue { Kind = UiValueKind.String, StringValue = "typed locally" };
        UiPreservationState current = new UiPreservationState
        {
            UserEditedFieldValues = new UiValueEntry[] { Entry("field", typed), Entry("field", new UiValue()) }
        };
        DomMutation unrelated = new DomMutation { Kind = UiMutationKind.SetAttribute, TargetId = "field", Name = AriaAttributes.Invalid };
        DomMutation controlled = new DomMutation { Kind = UiMutationKind.SetProperty, TargetId = "field", Name = HtmlAttributes.Value };
        PreservationPlanner planner = new PreservationPlanner();

        UiPreservationState preserved = planner.CreatePreservationPlan(Snapshot(beforeInput), Snapshot(sameInput), new DomMutation[] { unrelated }, current);
        UiPreservationState modelWins = planner.CreatePreservationPlan(Snapshot(beforeInput), Snapshot(sameInput), new DomMutation[] { controlled }, current);
        UiPreservationState modelChangedInsideFallback = planner.CreatePreservationPlan(Snapshot(beforeInput), Snapshot(modelChangedInput), new DomMutation[] { new DomMutation { Kind = UiMutationKind.Replace, TargetId = "field", Node = modelChangedInput } }, current);
        UiPreservationState hostChanged = planner.CreatePreservationPlan(Snapshot(beforeInput), Snapshot(changedInput), Array.Empty<DomMutation>(), current);
        typed.StringValue = "mutated source";

        Assert.Multiple(() =>
        {
            Assert.That(preserved.PreserveUserEditedFields, Is.True);
            Assert.That(preserved.UserEditedFieldValues, Has.Length.EqualTo(1));
            Assert.That(preserved.UserEditedFieldValues[0].Value.StringValue, Is.EqualTo("typed locally"));
            Assert.That(modelWins.PreserveUserEditedFields, Is.False);
            Assert.That(modelChangedInsideFallback.PreserveUserEditedFields, Is.False);
            Assert.That(hostChanged.PreserveUserEditedFields, Is.False);
        });
    }

    [Test]
    public void ControlledFocusedWritesEndCompositionWithoutDiscardingValidSelection()
    {
        DomNode beforeInput = Input("field", HtmlInputTypes.Text);
        DomNode afterInput = Input("field", HtmlInputTypes.Text);
        UiPreservationState current = new UiPreservationState
        {
            FocusedNodeId = "field",
            HasSelection = true,
            SelectionStart = 2,
            SelectionEnd = 4,
            IsComposing = true
        };
        DomMutation controlled = new DomMutation { Kind = UiMutationKind.SetProperty, TargetId = "field", Name = HtmlAttributes.Value };

        UiPreservationState result = new PreservationPlanner().CreatePreservationPlan(Snapshot(beforeInput), Snapshot(afterInput), new DomMutation[] { controlled }, current);

        Assert.Multiple(() =>
        {
            Assert.That(result.PreserveFocus, Is.True);
            Assert.That(result.PreserveSelection, Is.True);
            Assert.That(result.SelectionStart, Is.EqualTo(2));
            Assert.That(result.SelectionEnd, Is.EqualTo(4));
            Assert.That(result.PreserveComposition, Is.False);
            Assert.That(result.IsComposing, Is.False);
        });
    }

    private static DomSnapshot Snapshot(params DomNode[] roots)
    {
        return new DomSnapshot { Roots = roots };
    }

    private static DomNode Element(string id, string name, params DomNode[] children)
    {
        return new DomNode { Kind = DomNodeKind.Element, Key = id, Id = id, Name = name, Children = children };
    }

    private static DomNode Input(string id, string type)
    {
        DomNode result = Element(id, HtmlElements.Input);
        result.Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Type, Value = type } };
        return result;
    }

    private static DomNode Media(string id, string name, string source)
    {
        DomNode result = Element(id, name);
        result.Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Source, Value = source } };
        return result;
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    private static UiValue ObjectValue(string key, UiValue value)
    {
        return new UiValue
        {
            Kind = UiValueKind.Object,
            ObjectValue = new UiValueEntry[] { Entry(key, value) }
        };
    }

    private static UiValue IntegerValue(int value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }

    private static UiValue FloatingPointValue(double value)
    {
        return new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = value };
    }
}
