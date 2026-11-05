using System.Linq;
using System.Text.Json;
using htmlgui;

namespace htmlgui.Tests;

public class HydrationPlannerTests
{
    [Test]
    public void ManifestCollectsCanonicalDetachedMarkersForEveryHydrationCategory()
    {
        UiNode modelRoot = ModelRoot();
        UiPage page = new UiPage
        {
            Id = "page",
            ContentView = new UiView
            {
                Id = "view",
                ComponentContext = new UiComponentContext { ComponentId = "view-component", TypeId = "page-type", StateScopeId = "view-state" },
                ContentRoot = modelRoot
            }
        };
        UiApplication application = new UiApplication
        {
            State = new UiValueEntry[]
            {
                Entry("zeta", TextValue("last")),
                Entry("alpha", TextValue("first"))
            }
        };
        UiWindow window = new UiWindow
        {
            NavigationResult = new UiNavigationResult
            {
                CanonicalUrl = "/items",
                RouteData = new UiRouteData { RouteId = "items" },
                State = TextValue("history"),
                Generation = 3,
                HistoryIndex = 0,
                History = new UiHistoryEntry[] { new UiHistoryEntry { Id = "history-3" } }
            }
        };
        DomNode root = DomRoot();
        HydrationPlanner planner = new HydrationPlanner();

        UiHydrationManifest manifest = planner.CreateHydrationManifest(application, window, page, new DomNode[] { root }, "parity-hash");
        application.State[0].Value.StringValue = "mutated";
        modelRoot.EventBindings[0].HandlerId = "mutated";
        root.StyleScope = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(manifest.Hash, Is.EqualTo("parity-hash"));
            Assert.That(manifest.Nodes.Select(entry => entry.Key), Is.EqualTo(new string[] { "child", "root" }));
            Assert.That(ObjectField(manifest.Nodes[1].Value, "sequence").IntegerValue, Is.EqualTo(0));
            Assert.That(ObjectField(manifest.Nodes[1].Value, "marker").StringValue, Is.EqualTo("server-component"));
            Assert.That(manifest.Components.Select(entry => entry.Key), Is.EqualTo(new string[] { "nested-component", "root", "view-component" }));
            Assert.That(manifest.Events.Select(entry => entry.Key), Is.EqualTo(new string[] { "dom-event", "model-event" }));
            Assert.That(ObjectField(manifest.Events[1].Value, "handlerId").StringValue, Is.EqualTo("model-handler"));
            Assert.That(manifest.Bindings.Single().Key, Is.EqualTo("model-binding"));
            Assert.That(ObjectField(manifest.Bindings[0].Value, "targetNodeId").StringValue, Is.EqualTo("control"));
            Assert.That(manifest.State.Select(entry => entry.Key), Is.EqualTo(new string[] { "alpha", "zeta" }));
            Assert.That(manifest.State[1].Value.StringValue, Is.EqualTo("last"));
            Assert.That(manifest.Styles.Single().Key, Is.EqualTo("root"));
            Assert.That(manifest.Styles[0].Value.StringValue, Is.EqualTo("scope-a"));
            Assert.That(manifest.RouteUrl, Is.EqualTo("/items"));
            Assert.That(manifest.RouteData!.RouteId, Is.EqualTo("items"));
            Assert.That(manifest.HistoryEntryId, Is.EqualTo("history-3"));
            Assert.That(manifest.NavigationGeneration, Is.EqualTo(3));
        });
    }

    [Test]
    public void ManifestSerializationIncludesAllMarkerCategoriesInStableOrder()
    {
        UiHydrationManifest manifest = new HydrationPlanner().CreateHydrationManifest(
            new UiApplication(),
            new UiWindow(),
            new UiPage(),
            new DomNode[] { DomRoot() },
            "hash");

        string json = new HtmlOutputComposer().SerializeHydrationManifest(manifest);

        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            Assert.Multiple(() =>
            {
                Assert.That(json, Does.Contain("\"nodes\":").And.Contain("\"components\":").And.Contain("\"events\":").And.Contain("\"bindings\":").And.Contain("\"state\":").And.Contain("\"styles\":"));
                Assert.That(root.GetProperty("components").GetProperty("value").GetProperty("root").GetProperty("value").GetProperty("marker").GetProperty("value").GetString(), Is.EqualTo("server-component"));
                Assert.That(root.GetProperty("events").GetProperty("value").TryGetProperty("dom-event", out JsonElement _), Is.True);
                Assert.That(root.GetProperty("styles").GetProperty("value").GetProperty("root").GetProperty("value").GetString(), Is.EqualTo("scope-a"));
            });
        }
    }

    [Test]
    public void ExactAndMatchedModesDoNotScheduleMutations()
    {
        HydrationPlanner planner = new HydrationPlanner();
        DomMutation[] repair = new DomMutation[] { Mutation("repair") };
        UiDiagnostic[] diagnostics = new UiDiagnostic[] { Diagnostic("hydration.text") };

        UiHydrationResult matched = planner.PlanHydrationMode(UiHydrationMode.Repair, true, true, repair, repair, diagnostics);
        UiHydrationResult exact = planner.PlanHydrationMode(UiHydrationMode.Exact, false, true, repair, repair, diagnostics);

        Assert.Multiple(() =>
        {
            Assert.That(matched.Status, Is.EqualTo(UiHydrationStatus.Matched));
            Assert.That(matched.Mutations, Is.Empty);
            Assert.That(matched.Diagnostics.Single().Code, Is.EqualTo("hydration.text"));
            Assert.That(exact.Status, Is.EqualTo(UiHydrationStatus.Mismatch));
            Assert.That(exact.Mutations, Is.Empty);
            Assert.That(exact.Diagnostics.Single().Code, Is.EqualTo("hydration.text"));
        });
    }

    [Test]
    public void RepairAndReplaceModesReturnDetachedModeSpecificMutations()
    {
        HydrationPlanner planner = new HydrationPlanner();
        DomMutation repairMutation = Mutation("repair");
        DomMutation replacementMutation = Mutation("replace");
        UiDiagnostic diagnostic = Diagnostic("hydration.node");

        UiHydrationResult repaired = planner.PlanHydrationMode(UiHydrationMode.Repair, false, true, new DomMutation[] { repairMutation }, new DomMutation[] { replacementMutation }, new UiDiagnostic[] { diagnostic });
        UiHydrationResult replaced = planner.PlanHydrationMode(UiHydrationMode.Replace, false, false, new DomMutation[] { repairMutation }, new DomMutation[] { replacementMutation }, new UiDiagnostic[] { diagnostic });
        repairMutation.Value = "mutated";
        replacementMutation.Value = "mutated";
        diagnostic.Code = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(repaired.Mode, Is.EqualTo(UiHydrationMode.Repair));
            Assert.That(repaired.Status, Is.EqualTo(UiHydrationStatus.Repaired));
            Assert.That(repaired.Mutations.Single().Value, Is.EqualTo("repair"));
            Assert.That(repaired.Diagnostics.Single().Code, Is.EqualTo("hydration.node"));
            Assert.That(replaced.Mode, Is.EqualTo(UiHydrationMode.Replace));
            Assert.That(replaced.Status, Is.EqualTo(UiHydrationStatus.Replaced));
            Assert.That(replaced.Mutations.Single().Value, Is.EqualTo("replace"));
        });
    }

    [Test]
    public void UnrepairableAndInvalidModesFailWithStableDiagnostics()
    {
        HydrationPlanner planner = new HydrationPlanner();

        UiHydrationResult unrepairable = planner.PlanHydrationMode(UiHydrationMode.Repair, false, false, null, null, null);
        UiHydrationResult invalid = planner.PlanHydrationMode((UiHydrationMode)99, false, false, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(unrepairable.Status, Is.EqualTo(UiHydrationStatus.Failed));
            Assert.That(unrepairable.Diagnostics.Select(item => item.Code), Is.EqualTo(new string[] { "hydration.mismatch", "hydration.not-repairable" }));
            Assert.That(invalid.Status, Is.EqualTo(UiHydrationStatus.Failed));
            Assert.That(invalid.Diagnostics.Single().Code, Is.EqualTo("hydration.invalid-mode"));
            Assert.That(invalid.Mutations, Is.Empty);
        });
    }

    [Test]
    public void ManifestValidationRejectsMissingRootsHashesAndStableNodeIds()
    {
        HydrationPlanner planner = new HydrationPlanner();
        UiApplication application = new UiApplication();
        UiWindow window = new UiWindow();
        UiPage page = new UiPage();

        Assert.Multiple(() =>
        {
            Assert.That(() => planner.CreateHydrationManifest(application, window, page, null, "hash"), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("roots"));
            Assert.That(() => planner.CreateHydrationManifest(application, window, page, System.Array.Empty<DomNode>(), " "), Throws.ArgumentException.With.Property("ParamName").EqualTo("parityHash"));
            Assert.That(() => planner.CreateHydrationManifest(application, window, page, new DomNode[] { new DomNode() }, "hash"), Throws.ArgumentException.With.Property("ParamName").EqualTo("roots"));
        });
    }

    private static UiNode ModelRoot()
    {
        UiNode component = new UiNode
        {
            Kind = UiNodeKind.Component,
            Name = "nested-type",
            Identity = new UiNodeIdentity { StableId = "nested-component" }
        };
        return new UiNode
        {
            Kind = UiNodeKind.Element,
            Identity = new UiNodeIdentity { StableId = "control" },
            EventBindings = new UiEventBinding[]
            {
                new UiEventBinding { Id = "model-event", HandlerId = "model-handler", Kind = UiEventKind.Click }
            },
            Bindings = new UiBinding[]
            {
                new UiBinding
                {
                    Id = "model-binding",
                    StateId = "name-state",
                    Path = "/name",
                    Target = "value",
                    Mode = UiBindingMode.TwoWay,
                    Trigger = UiBindingTrigger.Input
                }
            },
            Children = new UiNode[] { component }
        };
    }

    private static DomNode DomRoot()
    {
        DomNode child = new DomNode { Sequence = 1, Kind = DomNodeKind.Text, Key = "child", Id = "child", Text = "Hello" };
        return new DomNode
        {
            Sequence = 0,
            Kind = DomNodeKind.Element,
            Key = "root",
            Id = "root",
            Name = HtmlElements.Division,
            HydrationMarker = "server-component",
            StyleScope = "scope-a",
            EventSubscriptions = new DomEventSubscription[]
            {
                new DomEventSubscription { Id = "dom-event", HandlerId = "dom-handler", Kind = UiEventKind.Input, EventName = DomEventNames.Input }
            },
            Children = new DomNode[] { child }
        };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    private static UiValue TextValue(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue ObjectField(UiValue value, string key)
    {
        return value.ObjectValue!.Single(entry => entry.Key == key).Value;
    }

    private static DomMutation Mutation(string value)
    {
        return new DomMutation { Kind = UiMutationKind.SetText, TargetId = "target", Value = value };
    }

    private static UiDiagnostic Diagnostic(string code)
    {
        return new UiDiagnostic { Severity = UiDiagnosticSeverity.Error, Code = code, Message = code, Location = "/node" };
    }
}
