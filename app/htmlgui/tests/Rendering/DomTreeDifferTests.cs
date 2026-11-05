using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class DomTreeDifferTests
{
    [Test]
    public void EquivalentKeyedTreesProduceNoMutations()
    {
        DomSnapshot before = Snapshot(Element("root", "root", Text("label", "label", "Hello")));
        DomSnapshot after = Snapshot(Element("root", "root", Text("label", "label", "Hello")));

        DomMutation[] mutations = new DomTreeDiffer().CreateMutations(before, after);

        Assert.That(mutations, Is.Empty);
    }

    [TestCase(DomNodeKind.Text)]
    [TestCase(DomNodeKind.Comment)]
    [TestCase(DomNodeKind.RawContent)]
    public void TextBearingNodesProduceSetTextMutations(DomNodeKind kind)
    {
        DomNode beforeNode = Text("content", "content", "Before");
        DomNode afterNode = Text("content", "content", "After");
        beforeNode.Kind = kind;
        afterNode.Kind = kind;
        if (kind == DomNodeKind.RawContent)
        {
            beforeNode.RawContentPolicyId = "approved";
            afterNode.RawContentPolicyId = "approved";
        }

        DomMutation[] mutations = new DomTreeDiffer().CreateMutations(Snapshot(beforeNode), Snapshot(afterNode));

        Assert.Multiple(() =>
        {
            Assert.That(mutations, Has.Length.EqualTo(1));
            Assert.That(mutations[0].Kind, Is.EqualTo(UiMutationKind.SetText));
            Assert.That(mutations[0].Sequence, Is.Zero);
            Assert.That(mutations[0].TargetId, Is.EqualTo("content"));
            Assert.That(mutations[0].Value, Is.EqualTo("After"));
            Assert.That(mutations[0].Node, Is.Null);
        });
    }

    [Test]
    public void EveryMutationKindIsProducedByAFunctionalDiffScenario()
    {
        DomTreeDiffer differ = new DomTreeDiffer();
        DomNode structuralBefore = Element(
            "root",
            "root",
            Element("a", "a"),
            Element("b", "b"),
            Element("removed", "removed"));
        DomNode structuralAfter = Element(
            "root",
            "root",
            Element("b", "b"),
            Element("inserted", "inserted"),
            Element("a", "a"));
        DomNode replacementBefore = Element("replacement", "replacement");
        DomNode replacementAfter = Element("replacement", "replacement");
        replacementAfter.Name = HtmlElements.Section;
        DomNode textBefore = Text("text", "text", "before");
        DomNode textAfter = Text("text", "text", "after");
        DomNode valuesBefore = Element("values", "values");
        valuesBefore.Attributes = new DomAttribute[]
        {
            new DomAttribute { Name = "data-remove", Value = "old" },
            new DomAttribute { Name = HtmlAttributes.Class, Value = "remove" },
            new DomAttribute { Name = HtmlAttributes.Style, Value = "old-property:old-value" }
        };
        valuesBefore.Properties = new DomProperty[]
        {
            Property("remove", new UiValue { Kind = UiValueKind.String, StringValue = "old" })
        };
        valuesBefore.EventSubscriptions = new DomEventSubscription[]
        {
            Subscription("remove", "old-handler", DomEventNames.Click)
        };
        DomNode valuesAfter = Element("values", "values");
        valuesAfter.Attributes = new DomAttribute[]
        {
            new DomAttribute { Name = "data-add", Value = "new" },
            new DomAttribute { Name = HtmlAttributes.Class, Value = "add" },
            new DomAttribute { Name = HtmlAttributes.Style, Value = "new-property:new-value" }
        };
        valuesAfter.Properties = new DomProperty[]
        {
            Property("add", new UiValue { Kind = UiValueKind.String, StringValue = "new" })
        };
        valuesAfter.EventSubscriptions = new DomEventSubscription[]
        {
            Subscription("add", "new-handler", DomEventNames.Input)
        };

        UiMutationKind[] actual = differ.CreateMutations(Snapshot(structuralBefore), Snapshot(structuralAfter))
            .Concat(differ.CreateMutations(Snapshot(replacementBefore), Snapshot(replacementAfter)))
            .Concat(differ.CreateMutations(Snapshot(textBefore), Snapshot(textAfter)))
            .Concat(differ.CreateMutations(Snapshot(valuesBefore), Snapshot(valuesAfter)))
            .Select(mutation => mutation.Kind)
            .Distinct()
            .OrderBy(kind => (int)kind)
            .ToArray();

        Assert.That(actual, Is.EqualTo(Enum.GetValues<UiMutationKind>()));
    }

    [Test]
    public void MixedSiblingChangesUseDeterministicIndexSafeOrdering()
    {
        DomNode beforeRoot = Element(
            "root",
            "root",
            Element("a", "a"),
            Element("b", "b"),
            Element("c", "c"),
            Element("d", "d"));
        DomNode inserted = Element("e", "e", Text("e-label", "e-label", "Inserted"));
        DomNode afterRoot = Element(
            "root",
            "root",
            Element("d", "d"),
            Element("b", "b"),
            inserted,
            Element("a", "a"));

        DomMutation[] mutations = new DomTreeDiffer().CreateMutations(Snapshot(beforeRoot), Snapshot(afterRoot));

        Assert.Multiple(() =>
        {
            Assert.That(mutations.Select(mutation => mutation.Kind), Is.EqualTo(new UiMutationKind[]
            {
                UiMutationKind.Remove,
                UiMutationKind.Move,
                UiMutationKind.Move,
                UiMutationKind.Insert
            }));
            Assert.That(mutations.Select(mutation => mutation.Sequence), Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(mutations.Select(mutation => mutation.TargetId), Is.EqualTo(new string[] { "c", "d", "b", "e" }));
            Assert.That(mutations[0].ParentId, Is.EqualTo("root"));
            Assert.That(mutations[0].HasFromIndex, Is.True);
            Assert.That(mutations[0].FromIndex, Is.EqualTo(2));
            Assert.That(mutations[1].FromIndex, Is.EqualTo(2));
            Assert.That(mutations[1].ToIndex, Is.Zero);
            Assert.That(mutations[1].ReferenceId, Is.EqualTo("a"));
            Assert.That(mutations[2].FromIndex, Is.EqualTo(2));
            Assert.That(mutations[2].ToIndex, Is.EqualTo(1));
            Assert.That(mutations[2].ReferenceId, Is.EqualTo("a"));
            Assert.That(mutations[3].Index, Is.EqualTo(2));
            Assert.That(mutations[3].ReferenceId, Is.EqualTo("a"));
            Assert.That(mutations[3].Node!.Children[0].Text, Is.EqualTo("Inserted"));
        });
    }

    [Test]
    public void NestedKeyedMovesRetainTheirImmediateParentsAndFollowAncestorMoves()
    {
        DomNode before = Element(
            "root",
            "root",
            Element("group-a", "group-a", Element("a-one", "a-one"), Element("a-two", "a-two")),
            Element("group-b", "group-b", Element("b-one", "b-one"), Element("b-two", "b-two")));
        DomNode after = Element(
            "root",
            "root",
            Element("group-b", "group-b", Element("b-two", "b-two"), Element("b-one", "b-one")),
            Element("group-a", "group-a", Element("a-two", "a-two"), Element("a-one", "a-one")));

        DomMutation[] mutations = new DomTreeDiffer().CreateMutations(Snapshot(before), Snapshot(after));

        Assert.Multiple(() =>
        {
            Assert.That(mutations.Select(mutation => mutation.Kind), Is.All.EqualTo(UiMutationKind.Move));
            Assert.That(mutations.Select(mutation => mutation.Sequence), Is.EqualTo(new int[] { 0, 1, 2 }));
            Assert.That(mutations.Select(mutation => mutation.TargetId), Is.EqualTo(new string[] { "group-b", "b-two", "a-two" }));
            Assert.That(mutations.Select(mutation => mutation.ParentId), Is.EqualTo(new string[] { "root", "group-b", "group-a" }));
            Assert.That(mutations.Select(mutation => mutation.ReferenceId), Is.EqualTo(new string[] { "group-a", "b-one", "a-one" }));
            Assert.That(mutations.Select(mutation => mutation.FromIndex), Is.EqualTo(new int[] { 1, 1, 1 }));
            Assert.That(mutations.Select(mutation => mutation.ToIndex), Is.EqualTo(new int[] { 0, 0, 0 }));
        });
    }

    [Test]
    public void MultipleRemovalsRunRightToLeftBeforeDestinationEdits()
    {
        DomNode before = Element(
            "root",
            "root",
            Element("a", "a"),
            Element("b", "b"),
            Element("c", "c"),
            Element("d", "d"));
        DomNode after = Element("root", "root", Element("b", "b"));

        DomMutation[] mutations = new DomTreeDiffer().CreateMutations(Snapshot(before), Snapshot(after));

        Assert.Multiple(() =>
        {
            Assert.That(mutations.Select(mutation => mutation.TargetId), Is.EqualTo(new string[] { "d", "c", "a" }));
            Assert.That(mutations.Select(mutation => mutation.FromIndex), Is.EqualTo(new int[] { 3, 2, 0 }));
            Assert.That(mutations.All(mutation => mutation.Kind == UiMutationKind.Remove), Is.True);
        });
    }

    [Test]
    public void AncestorStructuralChangesPrecedeMatchedDescendantChanges()
    {
        DomNode beforeKeep = Element(
            "keep",
            "keep",
            Element("drop", "drop"),
            Text("label", "label", "Before"));
        DomNode beforeRoot = Element("root", "root", beforeKeep, Element("gone", "gone", Element("nested", "nested")));
        DomNode afterKeep = Element(
            "keep",
            "keep",
            Text("label", "label", "After"),
            Element("new-child", "new-child"));
        DomNode afterRoot = Element("root", "root", Element("added", "added", Element("added-child", "added-child")), afterKeep);

        DomMutation[] mutations = new DomTreeDiffer().CreateMutations(Snapshot(beforeRoot), Snapshot(afterRoot));

        Assert.Multiple(() =>
        {
            Assert.That(mutations.Select(mutation => mutation.Kind), Is.EqualTo(new UiMutationKind[]
            {
                UiMutationKind.Remove,
                UiMutationKind.Insert,
                UiMutationKind.Remove,
                UiMutationKind.Insert,
                UiMutationKind.SetText
            }));
            Assert.That(mutations.Select(mutation => mutation.TargetId), Is.EqualTo(new string[]
            {
                "gone",
                "added",
                "drop",
                "new-child",
                "label"
            }));
            Assert.That(mutations[0].ParentId, Is.EqualTo("root"));
            Assert.That(mutations[1].ParentId, Is.EqualTo("root"));
            Assert.That(mutations[2].ParentId, Is.EqualTo("keep"));
            Assert.That(mutations[3].ParentId, Is.EqualTo("keep"));
            Assert.That(mutations[4].Value, Is.EqualTo("After"));
            Assert.That(mutations.Select(mutation => mutation.Sequence), Is.EqualTo(new int[] { 0, 1, 2, 3, 4 }));
        });
    }

    [Test]
    public void StructuralIdentityChangesReplaceTheWholeMatchedSubtree()
    {
        DomNode before = Element("item", "item", Text("old", "old", "Old"));
        DomNode after = Element("item", "item", Text("new", "new", "New"));
        after.Name = HtmlElements.Section;
        after.NamespaceUri = "urn:replacement";

        DomMutation[] mutations = new DomTreeDiffer().CreateMutations(Snapshot(before), Snapshot(after));

        Assert.Multiple(() =>
        {
            Assert.That(mutations, Has.Length.EqualTo(1));
            Assert.That(mutations[0].Kind, Is.EqualTo(UiMutationKind.Replace));
            Assert.That(mutations[0].TargetId, Is.EqualTo("item"));
            Assert.That(mutations[0].ParentId, Is.Null);
            Assert.That(mutations[0].HasIndex, Is.True);
            Assert.That(mutations[0].Index, Is.Zero);
            Assert.That(mutations[0].Node!.Name, Is.EqualTo(HtmlElements.Section));
            Assert.That(mutations[0].Node!.NamespaceUri, Is.EqualTo("urn:replacement"));
            Assert.That(mutations[0].Node!.Children[0].Text, Is.EqualTo("New"));
        });
    }

    [Test]
    public void ChangedStableIdentifierAndRawPolicyRequireReplacement()
    {
        DomNode beforeIdentity = Element("item", "before-id");
        DomNode afterIdentity = Element("item", "after-id");
        DomNode beforeRaw = Text("raw", "raw", "<b>same</b>");
        beforeRaw.Kind = DomNodeKind.RawContent;
        beforeRaw.RawContentPolicyId = "first-policy";
        DomNode afterRaw = Text("raw", "raw", "<b>same</b>");
        afterRaw.Kind = DomNodeKind.RawContent;
        afterRaw.RawContentPolicyId = "second-policy";
        DomTreeDiffer differ = new DomTreeDiffer();

        DomMutation[] identity = differ.CreateMutations(Snapshot(beforeIdentity), Snapshot(afterIdentity));
        DomMutation[] policy = differ.CreateMutations(Snapshot(beforeRaw), Snapshot(afterRaw));

        Assert.Multiple(() =>
        {
            Assert.That(identity.Single().Kind, Is.EqualTo(UiMutationKind.Replace));
            Assert.That(identity.Single().TargetId, Is.EqualTo("before-id"));
            Assert.That(identity.Single().Node!.Id, Is.EqualTo("after-id"));
            Assert.That(policy.Single().Kind, Is.EqualTo(UiMutationKind.Replace));
            Assert.That(policy.Single().Node!.RawContentPolicyId, Is.EqualTo("second-policy"));
        });
    }

    [Test]
    public void InsertAndReplacePayloadsAreDetachedDeepCopies()
    {
        DomNode inserted = RichElement("inserted", "inserted");
        DomNode replacement = RichElement("replace", "replace");
        replacement.Name = HtmlElements.Section;
        DomSnapshot before = Snapshot(Element("root", "root", Element("replace", "replace")));
        DomSnapshot after = Snapshot(Element("root", "root", inserted, replacement));

        DomMutation[] mutations = new DomTreeDiffer().CreateMutations(before, after);
        DomNode insertedPayload = mutations.Single(mutation => mutation.Kind == UiMutationKind.Insert).Node!;
        DomNode replacementPayload = mutations.Single(mutation => mutation.Kind == UiMutationKind.Replace).Node!;
        inserted.Attributes[0].Value = "changed";
        inserted.Properties[0].Value.StringValue = "changed";
        inserted.EventSubscriptions[0].HandlerId = "changed";
        inserted.Children[0].Text = "changed";
        replacement.Children[0].Text = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(insertedPayload, Is.Not.SameAs(inserted));
            Assert.That(insertedPayload.Attributes, Is.Not.SameAs(inserted.Attributes));
            Assert.That(insertedPayload.Attributes[0].Value, Is.EqualTo("ready"));
            Assert.That(insertedPayload.Properties[0].Value.StringValue, Is.EqualTo("value"));
            Assert.That(insertedPayload.EventSubscriptions[0].HandlerId, Is.EqualTo("handler"));
            Assert.That(insertedPayload.Children[0].Text, Is.EqualTo("child"));
            Assert.That(replacementPayload.Children[0].Text, Is.EqualTo("child"));
        });
    }

    [Test]
    public void RootInsertAndRemoveMutationsHaveNoParentAndAreRepeatable()
    {
        DomSnapshot before = Snapshot(Element("old", "old"), Element("keep", "keep"));
        DomSnapshot after = Snapshot(Element("keep", "keep"), Element("new", "new"));
        DomTreeDiffer differ = new DomTreeDiffer();

        DomMutation[] first = differ.CreateMutations(before, after);
        DomMutation[] repeated = differ.CreateMutations(before, after);

        Assert.Multiple(() =>
        {
            Assert.That(first.Select(mutation => mutation.Kind), Is.EqualTo(new UiMutationKind[] { UiMutationKind.Remove, UiMutationKind.Insert }));
            Assert.That(first.Select(mutation => mutation.TargetId), Is.EqualTo(new string[] { "old", "new" }));
            Assert.That(first.All(mutation => mutation.ParentId == null), Is.True);
            Assert.That(first[1].Index, Is.EqualTo(1));
            Assert.That(repeated.Select(MutationDescription), Is.EqualTo(first.Select(MutationDescription)));
        });
    }

    [Test]
    public void RejectsMissingSnapshotsAndNullNodes()
    {
        DomTreeDiffer differ = new DomTreeDiffer();
        DomSnapshot valid = Snapshot(Element("root", "root"));
        DomSnapshot malformed = new DomSnapshot { Roots = new DomNode[] { null! } };

        Assert.Multiple(() =>
        {
            Assert.That(() => differ.CreateMutations(null, valid), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("before"));
            Assert.That(() => differ.CreateMutations(valid, null), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("after"));
            Assert.That(() => differ.CreateMutations(malformed, valid), Throws.ArgumentException.With.Property("ParamName").EqualTo("before"));
            Assert.That(() => differ.CreateMutations(valid, malformed), Throws.ArgumentException.With.Property("ParamName").EqualTo("nodes"));
        });
    }

    [Test]
    public void AttributeDiffsAreOrderedAndCarryDetachedNamespacePayloads()
    {
        DomNode before = Element("item", "item");
        before.Attributes = new DomAttribute[]
        {
            new DomAttribute { Name = "data-change", Value = "before" },
            new DomAttribute { NamespaceUri = "urn:icons", Prefix = "old", Name = "label", Value = "same" },
            new DomAttribute { Name = "data-remove", Value = "gone" }
        };
        DomNode after = Element("item", "item");
        after.Attributes = new DomAttribute[]
        {
            new DomAttribute { NamespaceUri = "urn:icons", Prefix = "new", Name = "label", Value = "same" },
            new DomAttribute { Name = "data-change", Value = "after" },
            new DomAttribute { Name = "data-add", Value = "added" }
        };

        DomMutation[] mutations = new DomTreeDiffer().CreateMutations(Snapshot(before), Snapshot(after));
        after.Attributes[0].Prefix = "modified";
        after.Attributes[1].Value = "modified";

        Assert.Multiple(() =>
        {
            Assert.That(mutations.Select(mutation => mutation.Kind), Is.EqualTo(new UiMutationKind[]
            {
                UiMutationKind.RemoveAttribute,
                UiMutationKind.SetAttribute,
                UiMutationKind.SetAttribute,
                UiMutationKind.SetAttribute
            }));
            Assert.That(mutations.Select(mutation => mutation.Name), Is.EqualTo(new string[] { "data-remove", "data-add", "data-change", "label" }));
            Assert.That(mutations.Select(mutation => mutation.Sequence), Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(mutations[0].Attribute!.Value, Is.EqualTo("gone"));
            Assert.That(mutations[2].Attribute!.Value, Is.EqualTo("after"));
            Assert.That(mutations[2].Value, Is.EqualTo("after"));
            Assert.That(mutations[3].Attribute!.NamespaceUri, Is.EqualTo("urn:icons"));
            Assert.That(mutations[3].Attribute!.Prefix, Is.EqualTo("new"));
        });
    }

    [Test]
    public void ClassAndInlineStyleDiffsUseTokenAndDeclarationSemantics()
    {
        DomNode before = Element("item", "item");
        before.Attributes = new DomAttribute[]
        {
            new DomAttribute { Name = HtmlAttributes.Style, Value = "z-index:1;color:red;background:url(\"data:x;y\");" },
            new DomAttribute { Name = HtmlAttributes.Class, Value = "z keep remove keep" }
        };
        DomNode after = Element("item", "item");
        after.Attributes = new DomAttribute[]
        {
            new DomAttribute { Name = HtmlAttributes.Class, Value = "add\tkeep z" },
            new DomAttribute { Name = HtmlAttributes.Style, Value = "opacity:0.5;background:url(\"data:x;y\");color:blue;" }
        };

        DomMutation[] mutations = new DomTreeDiffer().CreateMutations(Snapshot(before), Snapshot(after));

        Assert.Multiple(() =>
        {
            Assert.That(mutations.Select(mutation => mutation.Kind), Is.EqualTo(new UiMutationKind[]
            {
                UiMutationKind.RemoveClass,
                UiMutationKind.AddClass,
                UiMutationKind.RemoveStyle,
                UiMutationKind.SetStyle,
                UiMutationKind.SetStyle
            }));
            Assert.That(mutations.Select(mutation => mutation.Name), Is.EqualTo(new string[] { "remove", "add", "z-index", "color", "opacity" }));
            Assert.That(mutations.Select(mutation => mutation.Value), Is.EqualTo(new string?[] { "remove", "add", null, "blue", "0.5" }));
        });
    }

    [Test]
    public void PropertyDiffsAreOrderedSerializedAndDetached()
    {
        DomNode before = Element("item", "item");
        before.Properties = new DomProperty[]
        {
            Property("remove", new UiValue { Kind = UiValueKind.String, StringValue = "gone" }),
            Property("count", new UiValue { Kind = UiValueKind.Integer, IntegerValue = 1 })
        };
        UiValue changedValue = new UiValue
        {
            Kind = UiValueKind.Array,
            ArrayValue = new UiValue[] { new UiValue { Kind = UiValueKind.Integer, IntegerValue = 2 } }
        };
        DomNode after = Element("item", "item");
        after.Properties = new DomProperty[]
        {
            Property("count", changedValue),
            Property("active", new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true })
        };

        DomMutation[] mutations = new DomTreeDiffer().CreateMutations(Snapshot(before), Snapshot(after));
        changedValue.ArrayValue[0].IntegerValue = 99;

        Assert.Multiple(() =>
        {
            Assert.That(mutations.Select(mutation => mutation.Kind), Is.EqualTo(new UiMutationKind[]
            {
                UiMutationKind.RemoveProperty,
                UiMutationKind.SetProperty,
                UiMutationKind.SetProperty
            }));
            Assert.That(mutations.Select(mutation => mutation.Name), Is.EqualTo(new string[] { "remove", "active", "count" }));
            Assert.That(mutations[1].Value, Is.EqualTo("{\"kind\":\"boolean\",\"value\":true}"));
            Assert.That(mutations[2].Value, Is.EqualTo("{\"kind\":\"array\",\"value\":[{\"kind\":\"integer\",\"value\":2}]}"));
            Assert.That(mutations[2].Property!.Value.ArrayValue![0].IntegerValue, Is.EqualTo(2));
            Assert.That(mutations[2].Property, Is.Not.SameAs(after.Properties[0]));
        });
    }

    [Test]
    public void ChangedAndReplacedSubscriptionsUnsubscribeBeforeSubscribing()
    {
        DomNode before = Element("item", "item");
        before.EventSubscriptions = new DomEventSubscription[]
        {
            Subscription("z-removed", "removed", DomEventNames.Input),
            Subscription("same", "before-handler", DomEventNames.Click)
        };
        DomNode after = Element("item", "item");
        after.EventSubscriptions = new DomEventSubscription[]
        {
            Subscription("same", "after-handler", DomEventNames.Click),
            Subscription("a-added", "added", DomEventNames.Change)
        };
        after.EventSubscriptions[0].PreventDefault = true;

        DomMutation[] mutations = new DomTreeDiffer().CreateMutations(Snapshot(before), Snapshot(after));
        after.EventSubscriptions[0].HandlerId = "modified";

        Assert.Multiple(() =>
        {
            Assert.That(mutations.Select(mutation => mutation.Kind), Is.EqualTo(new UiMutationKind[]
            {
                UiMutationKind.UnsubscribeEvent,
                UiMutationKind.UnsubscribeEvent,
                UiMutationKind.SubscribeEvent,
                UiMutationKind.SubscribeEvent
            }));
            Assert.That(mutations.Select(mutation => mutation.Name), Is.EqualTo(new string[] { "same", "z-removed", "a-added", "same" }));
            Assert.That(mutations[0].EventSubscription!.HandlerId, Is.EqualTo("before-handler"));
            Assert.That(mutations[3].EventSubscription!.HandlerId, Is.EqualTo("after-handler"));
            Assert.That(mutations[3].EventSubscription!.PreventDefault, Is.True);
        });
    }

    [Test]
    public void ControlledInputAndSelectionStateUsePropertyMutationsOnly()
    {
        DomNode beforeInput = Element("input", "input");
        beforeInput.Name = HtmlElements.Input;
        beforeInput.Attributes = new DomAttribute[]
        {
            new DomAttribute { Name = HtmlAttributes.Value, Value = "before" },
            new DomAttribute { Name = HtmlAttributes.Checked, IsBoolean = true }
        };
        beforeInput.Properties = new DomProperty[]
        {
            Property(HtmlAttributes.Value, new UiValue { Kind = UiValueKind.String, StringValue = "before" }),
            Property(HtmlAttributes.Checked, new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true }),
            Property("selectionStart", new UiValue { Kind = UiValueKind.Integer, IntegerValue = 1 })
        };
        DomNode afterInput = Element("input", "input");
        afterInput.Name = HtmlElements.Input;
        afterInput.Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Value, Value = "after" } };
        afterInput.Properties = new DomProperty[]
        {
            Property(HtmlAttributes.Value, new UiValue { Kind = UiValueKind.String, StringValue = "after" }),
            Property(HtmlAttributes.Checked, new UiValue { Kind = UiValueKind.Boolean, BooleanValue = false }),
            Property("selectionStart", new UiValue { Kind = UiValueKind.Integer, IntegerValue = 4 })
        };
        DomNode beforeOption = Element("option", "option");
        beforeOption.Name = HtmlElements.Option;
        DomNode afterOption = Element("option", "option");
        afterOption.Name = HtmlElements.Option;
        afterOption.Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Selected, IsBoolean = true } };

        DomMutation[] inputMutations = new DomTreeDiffer().CreateMutations(Snapshot(beforeInput), Snapshot(afterInput));
        DomMutation[] optionMutations = new DomTreeDiffer().CreateMutations(Snapshot(beforeOption), Snapshot(afterOption));

        Assert.Multiple(() =>
        {
            Assert.That(inputMutations.Select(mutation => mutation.Kind), Is.All.EqualTo(UiMutationKind.SetProperty));
            Assert.That(inputMutations.Select(mutation => mutation.Name), Is.EqualTo(new string[] { "checked", "selectionStart", "value" }));
            Assert.That(inputMutations[0].Property!.Value.BooleanValue, Is.False);
            Assert.That(inputMutations[1].Property!.Value.IntegerValue, Is.EqualTo(4));
            Assert.That(inputMutations[2].Property!.Value.StringValue, Is.EqualTo("after"));
            Assert.That(optionMutations.Single().Kind, Is.EqualTo(UiMutationKind.SetProperty));
            Assert.That(optionMutations.Single().Name, Is.EqualTo("selected"));
            Assert.That(optionMutations.Single().Property!.Value.BooleanValue, Is.True);
        });
    }

    [Test]
    public void OpenAndMediaStateUseEffectiveBooleanAndExplicitStateProperties()
    {
        DomNode beforeDetails = Element("details", "details");
        beforeDetails.Name = HtmlElements.Details;
        beforeDetails.Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Open, IsBoolean = true } };
        DomNode afterDetails = Element("details", "details");
        afterDetails.Name = HtmlElements.Details;
        DomNode beforeVideo = Element("video", "video");
        beforeVideo.Name = HtmlElements.Video;
        beforeVideo.Attributes = new DomAttribute[] { new DomAttribute { Name = "autoplay", IsBoolean = true } };
        beforeVideo.Properties = new DomProperty[]
        {
            Property("currentTime", new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = 2.5 }),
            Property("paused", new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true })
        };
        DomNode afterVideo = Element("video", "video");
        afterVideo.Name = HtmlElements.Video;
        afterVideo.Attributes = new DomAttribute[] { new DomAttribute { Name = "muted", IsBoolean = true } };
        afterVideo.Properties = new DomProperty[]
        {
            Property("currentTime", new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = 9.25 }),
            Property("paused", new UiValue { Kind = UiValueKind.Boolean, BooleanValue = false })
        };

        DomMutation[] detailsMutations = new DomTreeDiffer().CreateMutations(Snapshot(beforeDetails), Snapshot(afterDetails));
        DomMutation[] mediaMutations = new DomTreeDiffer().CreateMutations(Snapshot(beforeVideo), Snapshot(afterVideo));

        Assert.Multiple(() =>
        {
            Assert.That(detailsMutations.Single().Kind, Is.EqualTo(UiMutationKind.SetProperty));
            Assert.That(detailsMutations.Single().Name, Is.EqualTo("open"));
            Assert.That(detailsMutations.Single().Property!.Value.BooleanValue, Is.False);
            Assert.That(mediaMutations.Select(mutation => mutation.Kind), Is.All.EqualTo(UiMutationKind.SetProperty));
            Assert.That(mediaMutations.Select(mutation => mutation.Name), Is.EqualTo(new string[] { "autoplay", "currentTime", "muted", "paused" }));
            Assert.That(mediaMutations[0].Property!.Value.BooleanValue, Is.False);
            Assert.That(mediaMutations[1].Property!.Value.FloatingPointValue, Is.EqualTo(9.25));
            Assert.That(mediaMutations[2].Property!.Value.BooleanValue, Is.True);
            Assert.That(mediaMutations[3].Property!.Value.BooleanValue, Is.False);
        });
    }

    [Test]
    public void MatchingSubtreeHashesBailOutAndHashMismatchesOverrideVersions()
    {
        DomNode before = Element("root", "root", Text("label", "label", "Before"));
        before.Version = 3;
        before.SubtreeHash = "same-hash";
        DomNode after = Element("root", "root", Text("label", "label", "After"));
        after.Version = 4;
        after.SubtreeHash = "same-hash";
        DomNode mismatched = Element("root", "root", Text("label", "label", "After"));
        mismatched.Version = 3;
        mismatched.SubtreeHash = "different-hash";
        DomSnapshot beforeDocument = Snapshot(Element("duplicate", "first"), Element("duplicate", "second"));
        beforeDocument.ContentHash = "document-hash";
        DomSnapshot afterDocument = Snapshot(Element("duplicate", "changed-first"), Element("duplicate", "changed-second"));
        afterDocument.ContentHash = "document-hash";

        DomMutation[] bailedOut = new DomTreeDiffer().CreateMutations(Snapshot(before), Snapshot(after));
        DomMutation[] compared = new DomTreeDiffer().CreateMutations(Snapshot(before), Snapshot(mismatched));
        DomMutation[] documentBailout = new DomTreeDiffer().CreateMutations(beforeDocument, afterDocument);

        Assert.Multiple(() =>
        {
            Assert.That(bailedOut, Is.Empty);
            Assert.That(documentBailout, Is.Empty);
            Assert.That(compared.Single().Kind, Is.EqualTo(UiMutationKind.SetText));
            Assert.That(compared.Single().Value, Is.EqualTo("After"));
        });
    }

    [Test]
    public void PositiveMatchingVersionsBailOutWhenHashesAreUnavailable()
    {
        DomNode before = Element("root", "root", Text("label", "label", "Before"));
        before.Version = 8;
        DomNode after = Element("root", "root", Text("label", "label", "After"));
        after.Version = 8;
        DomNode defaultBefore = Element("default", "default", Text("text", "text", "Before"));
        DomNode defaultAfter = Element("default", "default", Text("text", "text", "After"));

        DomMutation[] versionBailout = new DomTreeDiffer().CreateMutations(Snapshot(before), Snapshot(after));
        DomMutation[] defaultVersionDiff = new DomTreeDiffer().CreateMutations(Snapshot(defaultBefore), Snapshot(defaultAfter));

        Assert.Multiple(() =>
        {
            Assert.That(versionBailout, Is.Empty);
            Assert.That(defaultVersionDiff.Single().Kind, Is.EqualTo(UiMutationKind.SetText));
        });
    }

    [Test]
    public void DuplicateOrUnstableChildKeysReplaceTheNearestSafeSubtree()
    {
        DomNode beforeDuplicate = Element("root", "root", Element("first", "first"), Element("second", "second"));
        DomNode afterDuplicate = Element("root", "root", Element("duplicate", "first"), Element("duplicate", "second"));
        DomNode beforeUnstable = Element("root", "root", Element("old-key", "stable-id"));
        DomNode afterUnstable = Element("root", "root", Element("new-key", "stable-id"));
        DomNode beforePositional = Element("root", "root", Element("$element-0", "generated-id"));
        DomNode afterPositional = Element("root", "root", Element("$element-0", "generated-id", Text("label", "label", "changed")));

        DomMutation[] duplicateFallback = new DomTreeDiffer().CreateMutations(Snapshot(beforeDuplicate), Snapshot(afterDuplicate));
        DomMutation[] unstableFallback = new DomTreeDiffer().CreateMutations(Snapshot(beforeUnstable), Snapshot(afterUnstable));
        DomMutation[] positionalFallback = new DomTreeDiffer().CreateMutations(Snapshot(beforePositional), Snapshot(afterPositional));
        afterDuplicate.Children[0].Name = HtmlElements.Section;

        Assert.Multiple(() =>
        {
            Assert.That(duplicateFallback, Has.Length.EqualTo(1));
            Assert.That(duplicateFallback[0].Kind, Is.EqualTo(UiMutationKind.Replace));
            Assert.That(duplicateFallback[0].TargetId, Is.EqualTo("root"));
            Assert.That(duplicateFallback[0].Node!.Children.Select(child => child.Key), Is.EqualTo(new string[] { "duplicate", "duplicate" }));
            Assert.That(duplicateFallback[0].Node!.Children[0].Name, Is.EqualTo(HtmlElements.Division));
            Assert.That(unstableFallback.Single().Kind, Is.EqualTo(UiMutationKind.Replace));
            Assert.That(unstableFallback.Single().TargetId, Is.EqualTo("root"));
            Assert.That(positionalFallback.Single().Kind, Is.EqualTo(UiMutationKind.Replace));
            Assert.That(positionalFallback.Single().TargetId, Is.EqualTo("root"));
        });
    }

    [Test]
    public void DuplicateAndUnstableRootKeysUseFullDocumentFallback()
    {
        DomSnapshot duplicateBefore = Snapshot(Element("duplicate", "old-first"), Element("duplicate", "old-second"));
        DomSnapshot duplicateAfter = Snapshot(Element("first", "new-first"), Element("second", "new-second"));
        DomSnapshot unstableBefore = Snapshot(Element("old-key", "stable-root"));
        DomSnapshot unstableAfter = Snapshot(Element("new-key", "stable-root"));
        DomSnapshot positionalBefore = Snapshot(Element("$element-0", "generated-root"));
        DomSnapshot positionalAfter = Snapshot(Element("$element-0", "generated-root", Text("label", "label", "changed")));

        DomMutation[] duplicateFallback = new DomTreeDiffer().CreateMutations(duplicateBefore, duplicateAfter);
        DomMutation[] unstableFallback = new DomTreeDiffer().CreateMutations(unstableBefore, unstableAfter);
        DomMutation[] positionalFallback = new DomTreeDiffer().CreateMutations(positionalBefore, positionalAfter);

        Assert.Multiple(() =>
        {
            Assert.That(duplicateFallback.Select(mutation => mutation.Kind), Is.EqualTo(new UiMutationKind[]
            {
                UiMutationKind.Remove,
                UiMutationKind.Remove,
                UiMutationKind.Insert,
                UiMutationKind.Insert
            }));
            Assert.That(duplicateFallback.Select(mutation => mutation.TargetId), Is.EqualTo(new string[] { "old-second", "old-first", "new-first", "new-second" }));
            Assert.That(duplicateFallback.Select(mutation => mutation.Sequence), Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(duplicateFallback.Take(2).Select(mutation => mutation.FromIndex), Is.EqualTo(new int[] { 1, 0 }));
            Assert.That(duplicateFallback.Skip(2).Select(mutation => mutation.Index), Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(duplicateFallback.All(mutation => mutation.ParentId == null), Is.True);
            Assert.That(unstableFallback.Select(mutation => mutation.Kind), Is.EqualTo(new UiMutationKind[] { UiMutationKind.Remove, UiMutationKind.Insert }));
            Assert.That(unstableFallback.Select(mutation => mutation.TargetId), Is.EqualTo(new string[] { "stable-root", "stable-root" }));
            Assert.That(positionalFallback.Select(mutation => mutation.Kind), Is.EqualTo(new UiMutationKind[] { UiMutationKind.Remove, UiMutationKind.Insert }));
            Assert.That(positionalFallback.Select(mutation => mutation.TargetId), Is.EqualTo(new string[] { "generated-root", "generated-root" }));
        });
    }

    [Test]
    public void RepeatedWritesToTheSameMutationSlotsAreCoalescedToTheirFinalValues()
    {
        DomNode before = Element("item", "item");
        DomNode after = Element("item", "item");
        after.Attributes = new DomAttribute[]
        {
            new DomAttribute { Name = "data-state", Value = "first" },
            new DomAttribute { Name = "data-state", Value = "final" }
        };
        after.Properties = new DomProperty[]
        {
            Property("count", new UiValue { Kind = UiValueKind.Integer, IntegerValue = 1 }),
            Property("count", new UiValue { Kind = UiValueKind.Integer, IntegerValue = 2 })
        };
        after.EventSubscriptions = new DomEventSubscription[]
        {
            Subscription("event", "first-handler", DomEventNames.Click),
            Subscription("event", "final-handler", DomEventNames.Click)
        };

        DomMutation[] mutations = new DomTreeDiffer().CreateMutations(Snapshot(before), Snapshot(after));

        Assert.Multiple(() =>
        {
            Assert.That(mutations.Select(mutation => mutation.Kind), Is.EqualTo(new UiMutationKind[]
            {
                UiMutationKind.SetAttribute,
                UiMutationKind.SetProperty,
                UiMutationKind.SubscribeEvent
            }));
            Assert.That(mutations.Select(mutation => mutation.Sequence), Is.EqualTo(new int[] { 0, 1, 2 }));
            Assert.That(mutations[0].Value, Is.EqualTo("final"));
            Assert.That(mutations[1].Property!.Value.IntegerValue, Is.EqualTo(2));
            Assert.That(mutations[2].EventSubscription!.HandlerId, Is.EqualTo("final-handler"));
        });
    }

    private static DomSnapshot Snapshot(params DomNode[] roots)
    {
        return new DomSnapshot { Roots = roots };
    }

    private static DomNode Element(string key, string id, params DomNode[] children)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = key,
            Id = id,
            Name = HtmlElements.Division,
            Children = children
        };
    }

    private static DomNode Text(string key, string id, string value)
    {
        return new DomNode { Kind = DomNodeKind.Text, Key = key, Id = id, Text = value };
    }

    private static DomNode RichElement(string key, string id)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = key,
            Id = id,
            Name = HtmlElements.Division,
            Attributes = new DomAttribute[] { new DomAttribute { Name = "data-state", Value = "ready" } },
            Properties = new DomProperty[]
            {
                new DomProperty { Name = "value", Value = new UiValue { Kind = UiValueKind.String, StringValue = "value" } }
            },
            EventSubscriptions = new DomEventSubscription[]
            {
                new DomEventSubscription { Id = "event", HandlerId = "handler", EventName = DomEventNames.Click }
            },
            Children = new DomNode[] { Text("child", id + "-child", "child") }
        };
    }

    private static DomProperty Property(string name, UiValue value)
    {
        return new DomProperty { Name = name, Value = value };
    }

    private static DomEventSubscription Subscription(string id, string handlerId, string eventName)
    {
        return new DomEventSubscription { Id = id, HandlerId = handlerId, EventName = eventName };
    }

    private static string MutationDescription(DomMutation mutation)
    {
        return mutation.Sequence + "|" + mutation.Kind + "|" + mutation.TargetId + "|" + mutation.ParentId + "|" + mutation.ReferenceId + "|" + mutation.FromIndex + "|" + mutation.ToIndex + "|" + mutation.Index;
    }
}
