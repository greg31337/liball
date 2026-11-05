using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class HydrationComparerTests
{
    [Test]
    public void MatchingTreesProduceDetachedRuntimeHandoffAndLifecyclePlan()
    {
        DomSnapshot expected = Snapshot("Hello", "root", HtmlElements.Division, "ready");
        DomSnapshot actual = Snapshot("Hello", "root", HtmlElements.Division, "ready");
        UiHydrationManifest manifest = Manifest(expected);
        HydrationComparer comparer = new HydrationComparer();

        UiHydrationResult result = comparer.CompareHydration(expected, actual, manifest, UiHydrationMode.Exact);
        manifest.State[0].Value.StringValue = "mutated";
        manifest.Events[0].Value.ObjectValue![0].Value.StringValue = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(UiHydrationStatus.Matched));
            Assert.That(result.ParityMatched, Is.True);
            Assert.That(result.ServerHash, Has.Length.EqualTo(64));
            Assert.That(result.ClientHash, Is.EqualTo(result.ServerHash));
            Assert.That(result.Mutations, Is.Empty);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.State.Single().Value.StringValue, Is.EqualTo("server-state"));
            Assert.That(result.EventAttachments.Select(item => item.Key), Is.EqualTo(new string[] { "dom-event", "model-event" }));
            Assert.That(result.BindingAttachments.Single().Key, Is.EqualTo("model-binding"));
            Assert.That(result.LifecycleNotifications.Select(item => item.Stage), Is.EqualTo(new UiLifecycleStage[]
            {
                UiLifecycleStage.HydrationStart,
                UiLifecycleStage.HydrationStart,
                UiLifecycleStage.HydrationComplete,
                UiLifecycleStage.HydrationComplete
            }));
            Assert.That(result.LifecycleNotifications.Select(item => item.Generation), Is.All.EqualTo(6));
        });
    }

    [TestCase(UiHydrationMode.Exact, UiHydrationStatus.Mismatch)]
    [TestCase(UiHydrationMode.Repair, UiHydrationStatus.Repaired)]
    [TestCase(UiHydrationMode.Replace, UiHydrationStatus.Replaced)]
    public void EveryRepairableMismatchClassIsReportedInEveryMode(UiHydrationMode mode, UiHydrationStatus expectedStatus)
    {
        UiHydrationResult[] results = RepairableMismatchResults(mode);
        string[] actualCodes = results
            .SelectMany(result => result.Diagnostics)
            .Select(diagnostic => diagnostic.Code)
            .Distinct()
            .ToArray();
        string[] expectedCodes = new string[]
        {
            "hydration.server-hash",
            "hydration.root-seed",
            "hydration.parity-hash",
            "hydration.root-count",
            "hydration.node-missing",
            "hydration.node-extra",
            "hydration.node-id",
            "hydration.node-key",
            "hydration.node-kind",
            "hydration.namespace",
            "hydration.tag",
            "hydration.text",
            "hydration.child-count",
            "hydration.attribute-count",
            "hydration.attribute-missing",
            "hydration.attribute-value",
            "hydration.attribute-extra"
        };

        Assert.Multiple(() =>
        {
            Assert.That(results.Select(result => result.Status), Is.All.EqualTo(expectedStatus));
            Assert.That(actualCodes, Is.SupersetOf(expectedCodes));
            if (mode == UiHydrationMode.Exact)
            {
                Assert.That(results.SelectMany(result => result.Mutations), Is.Empty);
            }
        });
    }

    [TestCase(UiHydrationMode.Exact, UiHydrationStatus.Matched, true)]
    [TestCase(UiHydrationMode.Repair, UiHydrationStatus.Repaired, false)]
    [TestCase(UiHydrationMode.Replace, UiHydrationStatus.Replaced, false)]
    public void SuccessfulModesPublishDetachedHandoffAttachmentsLifecycleAndParity(UiHydrationMode mode, UiHydrationStatus expectedStatus, bool parityMatched)
    {
        DomSnapshot expected = Snapshot("Server", "root", HtmlElements.Division, "ready");
        DomSnapshot actual = mode == UiHydrationMode.Exact
            ? Snapshot("Server", "root", HtmlElements.Division, "ready")
            : Snapshot("Client", "root", HtmlElements.Division, "ready");
        UiHydrationManifest manifest = Manifest(expected);

        UiHydrationResult result = new HydrationComparer().CompareHydration(expected, actual, manifest, mode);
        manifest.State[0].Value.StringValue = "mutated state";
        manifest.Events[0].Value.ObjectValue![0].Value.StringValue = "mutated event";
        manifest.Bindings[0].Value.ObjectValue![0].Value.StringValue = "mutated binding";

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(expectedStatus));
            Assert.That(result.ParityMatched, Is.EqualTo(parityMatched));
            Assert.That(result.ServerHash, Has.Length.EqualTo(64));
            Assert.That(result.ClientHash, Has.Length.EqualTo(64));
            Assert.That(result.State.Single().Value.StringValue, Is.EqualTo("server-state"));
            Assert.That(result.EventAttachments.Select(item => item.Key), Is.EqualTo(new string[] { "dom-event", "model-event" }));
            Assert.That(result.EventAttachments.Select(item => ReadStringField(item, "nodeId")), Is.All.EqualTo("root"));
            Assert.That(result.BindingAttachments.Select(item => item.Key), Is.EqualTo(new string[] { "model-binding" }));
            Assert.That(ReadStringField(result.BindingAttachments.Single(), "targetNodeId"), Is.EqualTo("root"));
            Assert.That(result.LifecycleNotifications.Select(item => item.ComponentId), Is.EqualTo(new string[]
            {
                "page-component",
                "root",
                "page-component",
                "root"
            }));
            Assert.That(result.LifecycleNotifications.Select(item => item.Stage), Is.EqualTo(new UiLifecycleStage[]
            {
                UiLifecycleStage.HydrationStart,
                UiLifecycleStage.HydrationStart,
                UiLifecycleStage.HydrationComplete,
                UiLifecycleStage.HydrationComplete
            }));
            Assert.That(result.LifecycleNotifications.Select(item => item.Generation), Is.All.EqualTo(6));
        });
    }

    [Test]
    public void ExactModeReportsTextAndParityMismatchWithoutRuntimeAttachments()
    {
        DomSnapshot expected = Snapshot("Server", "root", HtmlElements.Division, "ready");
        DomSnapshot actual = Snapshot("Client", "root", HtmlElements.Division, "ready");

        UiHydrationResult result = new HydrationComparer().CompareHydration(expected, actual, Manifest(expected), UiHydrationMode.Exact);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(UiHydrationStatus.Mismatch));
            Assert.That(result.ParityMatched, Is.False);
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("hydration.parity-hash").And.Contain("hydration.text"));
            Assert.That(result.Diagnostics.Single(item => item.Code == "hydration.text").Location, Is.EqualTo("/roots/0/children/0/text"));
            Assert.That(result.Mutations, Is.Empty);
            Assert.That(result.State, Is.Empty);
            Assert.That(result.EventAttachments, Is.Empty);
            Assert.That(result.BindingAttachments, Is.Empty);
            Assert.That(result.LifecycleNotifications, Is.Empty);
        });
    }

    [Test]
    public void RepairModeUsesKeyedDiffAndPublishesAttachmentsAfterRepair()
    {
        DomSnapshot expected = Snapshot("Server", "root", HtmlElements.Division, "ready");
        DomSnapshot actual = Snapshot("Client", "root", HtmlElements.Division, "ready");

        UiHydrationResult result = new HydrationComparer().CompareHydration(expected, actual, Manifest(expected), UiHydrationMode.Repair);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(UiHydrationStatus.Repaired));
            Assert.That(result.Mutations.Single().Kind, Is.EqualTo(UiMutationKind.SetText));
            Assert.That(result.Mutations.Single().TargetId, Is.EqualTo("text"));
            Assert.That(result.Mutations.Single().Value, Is.EqualTo("Server"));
            Assert.That(result.State, Has.Length.EqualTo(1));
            Assert.That(result.EventAttachments, Has.Length.EqualTo(2));
            Assert.That(result.BindingAttachments, Has.Length.EqualTo(1));
            Assert.That(result.LifecycleNotifications, Has.Length.EqualTo(4));
        });
    }

    [Test]
    public void ReplaceModeCreatesOneDetachedRootReplacement()
    {
        DomSnapshot expected = Snapshot("Server", "root", HtmlElements.Section, "ready");
        DomSnapshot actual = Snapshot("Client", "root", HtmlElements.Division, "stale");

        UiHydrationResult result = new HydrationComparer().CompareHydration(expected, actual, Manifest(expected), UiHydrationMode.Replace);
        expected.Roots[0].Name = HtmlElements.Article;
        expected.Roots[0].Children[0].Text = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(UiHydrationStatus.Replaced));
            Assert.That(result.Mutations, Has.Length.EqualTo(1));
            Assert.That(result.Mutations[0].Kind, Is.EqualTo(UiMutationKind.Replace));
            Assert.That(result.Mutations[0].TargetId, Is.EqualTo("root"));
            Assert.That(result.Mutations[0].Node!.Name, Is.EqualTo(HtmlElements.Section));
            Assert.That(result.Mutations[0].Node!.Children[0].Text, Is.EqualTo("Server"));
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("hydration.tag").And.Contain("hydration.attribute-value"));
        });
    }

    [Test]
    public void ComparisonReportsIdentityTagAttributeAndChildMismatchesAtStablePaths()
    {
        DomSnapshot expected = Snapshot("Text", "root", HtmlElements.Division, "ready");
        DomSnapshot actual = Snapshot("Text", "other-key", HtmlElements.Section, "stale");
        actual.Roots[0].Id = "other-id";
        actual.Roots[0].Children = System.Array.Empty<DomNode>();

        UiHydrationResult result = new HydrationComparer().CompareHydration(expected, actual, Manifest(expected), UiHydrationMode.Exact);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("hydration.node-id"));
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("hydration.node-key"));
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("hydration.tag"));
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("hydration.attribute-value"));
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("hydration.child-count"));
            Assert.That(result.Status, Is.EqualTo(UiHydrationStatus.Mismatch));
        });
    }

    [Test]
    public void ManifestHashMismatchIsDistinctFromClientServerTreeParity()
    {
        DomSnapshot expected = Snapshot("Same", "root", HtmlElements.Division, "ready");
        DomSnapshot actual = Snapshot("Same", "root", HtmlElements.Division, "ready");
        UiHydrationManifest manifest = Manifest(expected);
        manifest.Hash = new string('0', 64);

        UiHydrationResult result = new HydrationComparer().CompareHydration(expected, actual, manifest, UiHydrationMode.Exact);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(UiHydrationStatus.Mismatch));
            Assert.That(result.ParityMatched, Is.True);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("hydration.server-hash"));
            Assert.That(result.State, Is.Empty);
        });
    }

    [Test]
    public void RootSeedMismatchPreventsParityEvenWhenTreesMatch()
    {
        DomSnapshot expected = Snapshot("Same", "root", HtmlElements.Division, "ready");
        DomSnapshot actual = Snapshot("Same", "root", HtmlElements.Division, "ready");
        actual.RootSeed = "other-seed";

        UiHydrationResult result = new HydrationComparer().CompareHydration(expected, actual, Manifest(expected), UiHydrationMode.Exact);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(UiHydrationStatus.Mismatch));
            Assert.That(result.ParityMatched, Is.False);
            Assert.That(result.ServerHash, Is.EqualTo(result.ClientHash));
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("hydration.root-seed"));
        });
    }

    [Test]
    public void InvalidClientTreeCannotUseRepairButCanBeReplaced()
    {
        DomSnapshot expected = SnapshotWithSiblingKeys("first", "second");
        DomSnapshot actual = SnapshotWithSiblingKeys("duplicate", "duplicate");
        UiHydrationManifest manifest = Manifest(expected);
        HydrationComparer comparer = new HydrationComparer();

        UiHydrationResult exact = comparer.CompareHydration(expected, actual, manifest, UiHydrationMode.Exact);
        UiHydrationResult repair = comparer.CompareHydration(expected, actual, manifest, UiHydrationMode.Repair);
        UiHydrationResult replace = comparer.CompareHydration(expected, actual, manifest, UiHydrationMode.Replace);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(UiHydrationStatus.Mismatch));
            Assert.That(exact.Diagnostics.Select(item => item.Code), Does.Contain("hydration.invalid-client-tree"));
            Assert.That(exact.Mutations, Is.Empty);
            Assert.That(repair.Status, Is.EqualTo(UiHydrationStatus.Failed));
            Assert.That(repair.Diagnostics.Select(item => item.Code), Does.Contain("hydration.invalid-client-tree").And.Contain("hydration.not-repairable"));
            Assert.That(repair.Mutations, Is.Empty);
            Assert.That(replace.Status, Is.EqualTo(UiHydrationStatus.Replaced));
            Assert.That(replace.Mutations.Single().Kind, Is.EqualTo(UiMutationKind.Replace));
        });
    }

    [Test]
    public void ComparisonRejectsMissingInputsAndInvalidManifestIdentity()
    {
        HydrationComparer comparer = new HydrationComparer();
        DomSnapshot snapshot = Snapshot("Text", "root", HtmlElements.Division, "ready");
        UiHydrationManifest manifest = Manifest(snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(() => comparer.CompareHydration(null, snapshot, manifest, UiHydrationMode.Exact), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("expected"));
            Assert.That(() => comparer.CompareHydration(snapshot, null, manifest, UiHydrationMode.Exact), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("actual"));
            Assert.That(() => comparer.CompareHydration(snapshot, snapshot, null, UiHydrationMode.Exact), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("manifest"));
            Assert.That(() => comparer.CompareHydration(snapshot, snapshot, new UiHydrationManifest(), UiHydrationMode.Exact), Throws.ArgumentException.With.Property("ParamName").EqualTo("manifest"));
        });
    }

    private static DomSnapshot Snapshot(string text, string rootKey, string rootName, string state)
    {
        DomNode child = new DomNode { Kind = DomNodeKind.Text, Key = "text", Id = "text", Text = text };
        DomNode root = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = rootKey,
            Id = "root",
            Name = rootName,
            HydrationMarker = "root-component",
            Attributes = new DomAttribute[] { new DomAttribute { Name = "data-state", Value = state } },
            EventSubscriptions = new DomEventSubscription[]
            {
                new DomEventSubscription { Id = "dom-event", HandlerId = "dom-handler", Kind = UiEventKind.Click, EventName = DomEventNames.Click }
            },
            Children = new DomNode[] { child }
        };
        RenderTreeAnnotationResult tree = new RenderTreeAnnotator().AnnotateTree(new DomNode[] { root }, "seed", 100);
        return new DomSnapshot { SessionId = "session", RootSeed = "seed", Roots = tree.Roots, ContentHash = tree.ContentHash };
    }

    private static DomSnapshot SnapshotWithSiblingKeys(string firstKey, string secondKey)
    {
        DomNode first = new DomNode { Kind = DomNodeKind.Text, Key = firstKey, Id = "first", Text = "first" };
        DomNode second = new DomNode { Kind = DomNodeKind.Text, Key = secondKey, Id = "second", Text = "second" };
        DomNode root = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "root",
            Id = "root",
            Name = HtmlElements.Division,
            Children = new DomNode[] { first, second }
        };
        RenderTreeAnnotationResult tree = new RenderTreeAnnotator().AnnotateTree(new DomNode[] { root }, "seed", 100);
        return new DomSnapshot { SessionId = "session", RootSeed = "seed", Roots = tree.Roots, ContentHash = tree.ContentHash };
    }

    private static UiHydrationResult[] RepairableMismatchResults(UiHydrationMode mode)
    {
        HydrationComparer comparer = new HydrationComparer();

        DomSnapshot missingExpected = SnapshotWithRoots("first", "second");
        DomSnapshot missingActual = SnapshotWithRoots("first");
        UiHydrationResult missing = comparer.CompareHydration(missingExpected, missingActual, Manifest(missingExpected), mode);

        DomSnapshot extraExpected = SnapshotWithRoots("first");
        DomSnapshot extraActual = SnapshotWithRoots("first", "second");
        UiHydrationResult extra = comparer.CompareHydration(extraExpected, extraActual, Manifest(extraExpected), mode);

        DomSnapshot identityExpected = Snapshot("Text", "root", HtmlElements.Division, "ready");
        DomSnapshot identityActual = Snapshot("Text", "other-key", HtmlElements.Section, "stale");
        identityActual.Roots[0].Id = "other-id";
        identityActual.Roots[0].NamespaceUri = "urn:client";
        identityActual.Roots[0].Children = System.Array.Empty<DomNode>();
        UiHydrationResult identity = comparer.CompareHydration(identityExpected, identityActual, Manifest(identityExpected), mode);

        DomSnapshot kindExpected = Snapshot("Text", "root", HtmlElements.Division, "ready");
        DomSnapshot kindActual = Snapshot("Text", "root", HtmlElements.Division, "ready");
        kindActual.Roots[0].Children[0].Kind = DomNodeKind.Comment;
        UiHydrationResult kind = comparer.CompareHydration(kindExpected, kindActual, Manifest(kindExpected), mode);

        DomSnapshot textExpected = Snapshot("Server", "root", HtmlElements.Division, "ready");
        DomSnapshot textActual = Snapshot("Client", "root", HtmlElements.Division, "ready");
        UiHydrationResult text = comparer.CompareHydration(textExpected, textActual, Manifest(textExpected), mode);

        DomSnapshot missingAttributeExpected = Snapshot("Text", "root", HtmlElements.Division, "ready");
        DomSnapshot missingAttributeActual = Snapshot("Text", "root", HtmlElements.Division, "ready");
        missingAttributeActual.Roots[0].Attributes = System.Array.Empty<DomAttribute>();
        UiHydrationResult missingAttribute = comparer.CompareHydration(missingAttributeExpected, missingAttributeActual, Manifest(missingAttributeExpected), mode);

        DomSnapshot extraAttributeExpected = Snapshot("Text", "root", HtmlElements.Division, "ready");
        DomSnapshot extraAttributeActual = Snapshot("Text", "root", HtmlElements.Division, "ready");
        extraAttributeActual.Roots[0].Attributes = new DomAttribute[]
        {
            new DomAttribute { Name = "data-state", Value = "ready" },
            new DomAttribute { Name = "data-extra", Value = "extra" }
        };
        UiHydrationResult extraAttribute = comparer.CompareHydration(extraAttributeExpected, extraAttributeActual, Manifest(extraAttributeExpected), mode);

        DomSnapshot rootSeedExpected = Snapshot("Text", "root", HtmlElements.Division, "ready");
        DomSnapshot rootSeedActual = Snapshot("Text", "root", HtmlElements.Division, "ready");
        rootSeedActual.RootSeed = "other-seed";
        UiHydrationResult rootSeed = comparer.CompareHydration(rootSeedExpected, rootSeedActual, Manifest(rootSeedExpected), mode);

        DomSnapshot serverHashExpected = Snapshot("Text", "root", HtmlElements.Division, "ready");
        UiHydrationManifest wrongManifest = Manifest(serverHashExpected);
        wrongManifest.Hash = new string('0', 64);
        UiHydrationResult serverHash = comparer.CompareHydration(serverHashExpected, Snapshot("Text", "root", HtmlElements.Division, "ready"), wrongManifest, mode);

        return new UiHydrationResult[]
        {
            missing,
            extra,
            identity,
            kind,
            text,
            missingAttribute,
            extraAttribute,
            rootSeed,
            serverHash
        };
    }

    private static DomSnapshot SnapshotWithRoots(params string[] keys)
    {
        DomNode[] roots = keys.Select(key => new DomNode
        {
            Kind = DomNodeKind.Text,
            Key = key,
            Id = key,
            Text = key
        }).ToArray();
        RenderTreeAnnotationResult tree = new RenderTreeAnnotator().AnnotateTree(roots, "seed", 100);
        return new DomSnapshot { SessionId = "session", RootSeed = "seed", Roots = tree.Roots, ContentHash = tree.ContentHash };
    }

    private static string? ReadStringField(UiValueEntry entry, string key)
    {
        return entry.Value.ObjectValue!.Single(field => field.Key == key).Value.StringValue;
    }

    private static UiHydrationManifest Manifest(DomSnapshot snapshot)
    {
        UiNode modelRoot = new UiNode
        {
            Kind = UiNodeKind.Element,
            Identity = new UiNodeIdentity { StableId = "root" },
            EventBindings = new UiEventBinding[]
            {
                new UiEventBinding { Id = "model-event", HandlerId = "model-handler", Kind = UiEventKind.Input }
            },
            Bindings = new UiBinding[]
            {
                new UiBinding { Id = "model-binding", StateId = "state", Target = "value", TargetNodeId = "root" }
            }
        };
        UiApplication application = new UiApplication
        {
            State = new UiValueEntry[]
            {
                new UiValueEntry { Key = "state", Value = new UiValue { Kind = UiValueKind.String, StringValue = "server-state" } }
            }
        };
        UiPage page = new UiPage
        {
            ContentView = new UiView
            {
                Id = "root",
                ComponentContext = new UiComponentContext { ComponentId = "page-component", TypeId = "page", StateScopeId = "state" },
                ContentRoot = modelRoot
            }
        };
        UiWindow window = new UiWindow
        {
            NavigationResult = new UiNavigationResult { Generation = 6 }
        };
        return new HydrationPlanner().CreateHydrationManifest(application, window, page, snapshot.Roots, snapshot.ContentHash);
    }
}
