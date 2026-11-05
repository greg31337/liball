using System.Text;
using System.Text.Json;
using htmlgui;

namespace htmlgui.Tests;

public class RandomizedRuntimePropertyTests
{
    [Test]
    public void RandomizedValidModelsRenderDeterministicallyWithoutMutation()
    {
        JsonSerializerOptions jsonOptions = new JsonSerializerOptions { IncludeFields = true };
        IUiRenderer renderer = new UiRenderer();

        for (int seed = 0; seed < 64; seed++)
        {
            UiApplication application = RandomApplication(seed);
            string original = JsonSerializer.Serialize(application, jsonOptions);

            UiRenderResult first = renderer.RenderDocument(application);
            UiRenderResult second = renderer.RenderDocument(application);

            Assert.Multiple(() =>
            {
                Assert.That(first.Diagnostics, Is.Empty, "seed " + seed);
                Assert.That(second.Diagnostics, Is.Empty, "seed " + seed);
                Assert.That(second.Html, Is.EqualTo(first.Html), "seed " + seed);
                Assert.That(second.Css, Is.EqualTo(first.Css), "seed " + seed);
                Assert.That(second.Hash, Is.EqualTo(first.Hash), "seed " + seed);
                Assert.That(second.Snapshot!.ContentHash, Is.EqualTo(first.Snapshot!.ContentHash), "seed " + seed);
                Assert.That(JsonSerializer.Serialize(application, jsonOptions), Is.EqualTo(original), "seed " + seed);
            });
        }
    }

    [Test]
    public void RandomizedStateAndEventScenariosReplayDeterministically()
    {
        for (int seed = 0; seed < 64; seed++)
        {
            string firstState = RunStateScenario(seed);
            string secondState = RunStateScenario(seed);
            string firstEvent = RunEventScenario(seed);
            string secondEvent = RunEventScenario(seed);

            Assert.Multiple(() =>
            {
                Assert.That(secondState, Is.EqualTo(firstState), "state seed " + seed);
                Assert.That(secondEvent, Is.EqualTo(firstEvent), "event seed " + seed);
            });
        }
    }

    [Test]
    public void RandomizedBridgePayloadsRoundTripCanonically()
    {
        BridgeCodec codec = new BridgeCodec();
        Random random = new Random(17401);

        for (int index = 0; index < 128; index++)
        {
            UiBridgeEnvelope envelope = new UiBridgeEnvelope
            {
                Version = BridgeProtocolVersions.Current,
                Session = "property-session",
                Sequence = index,
                Origin = BridgeProtocolNames.HostOrigin,
                Kind = UiBridgeMessageKind.Log,
                Payload = RandomValue(random, 0)
            };

            string first = codec.SerializeBridgeEnvelope(envelope, 65536);
            string repeated = codec.SerializeBridgeEnvelope(envelope, 65536);
            UiBridgeEnvelope decoded = codec.DeserializeBridgeEnvelope(first, 65536);
            string roundTrip = codec.SerializeBridgeEnvelope(decoded, 65536);

            Assert.Multiple(() =>
            {
                Assert.That(repeated, Is.EqualTo(first), "case " + index);
                Assert.That(roundTrip, Is.EqualTo(first), "case " + index);
                Assert.That(decoded.Sequence, Is.EqualTo(index), "case " + index);
                Assert.That(decoded.Kind, Is.EqualTo(UiBridgeMessageKind.Log), "case " + index);
            });
        }
    }

    [Test]
    public void RandomizedDomUpdatesAreDeterministicAndConvergeAfterAcceptance()
    {
        for (int seed = 0; seed < 64; seed++)
        {
            Random random = new Random(33000 + seed);
            DomNode beforeTree = RandomDomTree(random, false);
            DomNode afterTree = RandomDomTree(random, true);
            IUiUpdateEngine updates = new UiUpdateEngine();
            IUiUpdateEngine replayUpdates = new UiUpdateEngine();
            DomSnapshot before = updates.CreateDomSnapshot(new DomNode[] { beforeTree }, "session-" + seed, "dom-seed", 0);
            DomSnapshot after = updates.CreateDomSnapshot(new DomNode[] { afterTree }, "session-" + seed, "dom-seed", 0);
            DomSnapshot replayBefore = replayUpdates.CreateDomSnapshot(new DomNode[] { beforeTree }, "session-" + seed, "dom-seed", 0);
            DomSnapshot replayAfter = replayUpdates.CreateDomSnapshot(new DomNode[] { afterTree }, "session-" + seed, "dom-seed", 0);

            UiUpdateBatch batch = updates.ComputeDomUpdates(before, after, UiUpdatePriority.Normal, null);
            UiUpdateBatch replay = replayUpdates.ComputeDomUpdates(replayBefore, replayAfter, UiUpdatePriority.Normal, null);
            UiUpdateResult accepted = updates.AcceptDomUpdates(batch.Id, batch.Revision);
            DomSnapshot committed = updates.CreateDomSnapshot(after.Roots, after.SessionId, after.RootSeed, batch.Revision);
            UiUpdateBatch converged = updates.ComputeDomUpdates(committed, committed, UiUpdatePriority.Background, null);

            Assert.Multiple(() =>
            {
                Assert.That(batch.Id, Is.EqualTo(replay.Id), "seed " + seed);
                Assert.That(MutationSignature(batch.Mutations), Is.EqualTo(MutationSignature(replay.Mutations)), "seed " + seed);
                Assert.That(batch.Mutations, Is.Not.Empty, "seed " + seed);
                Assert.That(accepted.Accepted, Is.True, "seed " + seed);
                Assert.That(converged.Mutations, Is.Empty, "seed " + seed);
                Assert.That(converged.BaseRevision, Is.EqualTo(1), "seed " + seed);
            });
        }
    }

    [Test]
    public void BoundedHostileModelsEventsAndBridgeJsonFailWithoutUnexpectedExceptions()
    {
        IUiModelValidator validator = new UiModelValidator();
        IUiRenderer renderer = new UiRenderer();
        Random random = new Random(99174);

        for (int index = 0; index < 96; index++)
        {
            UiApplication application = HostileApplication(index);
            Assert.DoesNotThrow(() => validator.TryValidateModel(application), "model validation case " + index);
            AssertExpectedInputFailure(() => renderer.RenderDocument(application), "model rendering case " + index);

            IUiEventDispatcher events = new UiEventDispatcher();
            UiEventData hostileEvent = new UiEventData
            {
                Kind = index % 2 == 0 ? (UiEventKind)int.MaxValue : UiEventKind.Click,
                TargetId = index % 3 == 0 ? string.Empty : "target",
                Path = index % 5 == 0 ? new string[] { "other" } : new string[] { "target" }
            };
            AssertExpectedInputFailure(() => events.DispatchUiEvent(hostileEvent), "event case " + index);

            string hostileJson = RandomHostileJson(random, index % 129);
            AssertExpectedInputFailure(() => new BridgeCodec().DeserializeBridgeEnvelope(hostileJson, 1024), "bridge case " + index);
        }
    }

    private static UiApplication RandomApplication(int seed)
    {
        Random random = new Random(10000 + seed);
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("model-seed-" + seed, "application");
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, "window");
        UiPage page = factory.CreatePage(application.RootSeed, window.Id, "page");
        UiView view = factory.CreateView(application.RootSeed, page.Id, "view");
        UiNode root = factory.CreateElementNode(application.RootSeed, view.Id, "root", null, HtmlElements.Main);
        int childCount = random.Next(1, 9);
        UiNode[] children = new UiNode[childCount];
        string[] names = new string[] { HtmlElements.Division, HtmlElements.Span, HtmlElements.Paragraph, HtmlElements.Section };
        string[] colors = new string[] { "red", "blue", "#123456", "currentcolor" };
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            string key = "node-" + childIndex;
            UiNode child = factory.CreateElementNode(application.RootSeed, root.Identity.StableId, key, null, names[random.Next(names.Length)]);
            child.Attributes = new DomAttribute[]
            {
                new DomAttribute { Name = HtmlAttributes.Title, Value = RandomText(random, 20) }
            };
            child.Children = new UiNode[]
            {
                factory.CreateTextNode(application.RootSeed, child.Identity.StableId, key + "-text", RandomText(random, 40))
            };
            if (random.Next(2) == 0)
            {
                child.Style = new UiStyle
                {
                    Key = key + "-style",
                    Declarations = new UiCssDeclaration[]
                    {
                        new UiCssDeclaration { Property = CssProperties.Color, Value = colors[random.Next(colors.Length)] }
                    }
                };
            }

            child.State.IsVisible = random.Next(5) != 0;
            children[childIndex] = child;
            childIndex++;
        }

        root.Children = children;
        view.ContentRoot = root;
        page.ContentView = view;
        page.Title = RandomText(random, 24);
        page.Language = random.Next(2) == 0 ? "en" : "ar";
        page.TextDirection = page.Language == "ar" ? "rtl" : "ltr";
        page.Styles = new UiStyle[]
        {
            new UiStyle
            {
                Key = "page-style",
                ResponsiveRules = new UiResponsiveRule[]
                {
                    new UiResponsiveRule
                    {
                        Operator = UiResponsiveOperator.MinWidth,
                        Length = new UiLength { HasValue = true, Value = random.Next(20, 81), Unit = UiLengthUnit.Rem },
                        Declarations = new UiCssDeclaration[]
                        {
                            new UiCssDeclaration { Property = CssProperties.Gap, Value = random.Next(1, 5) + "rem" }
                        }
                    }
                }
            }
        };
        window.Pages = new UiPage[] { page };
        window.CurrentPageKey = page.Key;
        window.IsActive = true;
        application.Windows = new UiWindow[] { window };
        application.RenderOptions.IncludeHydration = false;
        application.RenderOptions.IncludeBootstrap = false;
        return application;
    }

    private static string RunStateScenario(int seed)
    {
        Random random = new Random(20000 + seed);
        IUiStateStore store = new UiStateStore();
        StringBuilder trace = new StringBuilder();
        int stateIndex = 0;
        while (stateIndex < 6)
        {
            store.CreateState("scope", "state-" + stateIndex, ObjectValue("value", Integer(stateIndex)), "setup");
            stateIndex++;
        }

        for (int operationIndex = 0; operationIndex < 96; operationIndex++)
        {
            string stateId = "state-" + random.Next(6);
            int value = random.Next(-10000, 10001);
            int operation = random.Next(6);
            UiStateResult result;
            if (operation == 0)
            {
                result = store.WriteState("scope", stateId, ObjectValue("value", Integer(value)), "write");
            }
            else if (operation == 1)
            {
                UiStateResult current = store.ReadState("scope", stateId);
                int expected = random.Next(3) == 0 ? current.Entry!.Version + 1 : current.Entry!.Version;
                result = store.CompareAndSetState("scope", stateId, expected, ObjectValue("value", Integer(value)), "compare");
            }
            else if (operation == 2)
            {
                result = store.PatchState("scope", stateId, new UiStatePatch[]
                {
                    new UiStatePatch { Operation = "replace", Path = "/value", Value = Integer(value) }
                }, "patch");
            }
            else if (operation == 3)
            {
                result = store.ReadState("scope", stateId);
            }
            else if (operation == 4)
            {
                store.BeginStateBatch("batch-" + operationIndex);
                store.WriteState("scope", stateId, ObjectValue("value", Integer(value)), "batch");
                if (random.Next(2) == 0)
                {
                    store.CommitStateBatch();
                }
                else
                {
                    store.RollbackStateBatch();
                }

                result = store.ReadState("scope", stateId);
            }
            else
            {
                string snapshotId = "snapshot-" + operationIndex;
                store.CreateStateSnapshot(snapshotId);
                store.WriteState("scope", stateId, ObjectValue("value", Integer(value)), "temporary");
                store.RestoreStateSnapshot(snapshotId, "restore");
                result = store.ReadState("scope", stateId);
            }

            trace.Append(result.Success ? '1' : '0');
            trace.Append(result.Applied ? '1' : '0');
            if (result.Entry != null)
            {
                trace.Append(':');
                trace.Append(result.Entry.Version);
                trace.Append(':');
                trace.Append(result.Entry.Value.ObjectValue![0].Value.IntegerValue);
            }

            trace.Append('|');
        }

        return trace.ToString();
    }

    private static string RunEventScenario(int seed)
    {
        Random random = new Random(25000 + seed);
        IUiEventDispatcher dispatcher = new UiEventDispatcher();
        ScenarioEventHandler capture = new ScenarioEventHandler(random.Next(2) == 0, false, false);
        ScenarioEventHandler target = new ScenarioEventHandler(true, random.Next(2) == 0, false);
        ScenarioEventHandler bubble = new ScenarioEventHandler(random.Next(2) == 0, false, random.Next(2) == 0);
        dispatcher.RegisterEventHandler("root", Binding("capture", UiEventPhase.Capture), capture);
        dispatcher.RegisterEventHandler("target", Binding("target", UiEventPhase.Target), target);
        dispatcher.RegisterEventHandler("parent", Binding("bubble", UiEventPhase.Bubble), bubble);

        UiEventDispatchResult result = dispatcher.DispatchUiEvent(new UiEventData
        {
            Kind = UiEventKind.Click,
            TargetId = "target",
            Path = new string[] { "target", "parent", "root" },
            IsTrusted = true,
            Pointer = new UiPointerData { PointerType = "mouse", PointerId = random.Next(1, 1000) }
        });

        return string.Join(",", result.InvokedBindingIds)
            + ":" + result.Handled
            + ":" + result.DefaultPrevented
            + ":" + result.PropagationStopped;
    }

    private static UiEventBinding Binding(string id, UiEventPhase phase)
    {
        return new UiEventBinding
        {
            Id = id,
            HandlerId = id + "-handler",
            Kind = UiEventKind.Click,
            Phase = phase
        };
    }

    private static UiValue RandomValue(Random random, int depth)
    {
        int kind = depth >= 3 ? random.Next(7) : random.Next(10);
        if (kind == 0)
        {
            return new UiValue();
        }

        if (kind == 1)
        {
            return new UiValue { Kind = UiValueKind.Boolean, BooleanValue = random.Next(2) == 0 };
        }

        if (kind == 2)
        {
            return Integer(random.NextInt64(-1000000, 1000001));
        }

        if (kind == 3)
        {
            return new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = random.NextDouble() * 2000 - 1000 };
        }

        if (kind == 4)
        {
            return new UiValue { Kind = UiValueKind.Decimal, DecimalValue = random.Next(-10000, 10001) / 100m };
        }

        if (kind == 5)
        {
            return Text(RandomText(random, 40));
        }

        if (kind == 6)
        {
            return new UiValue { Kind = UiValueKind.DateTime, DateTimeValue = "2026-09-15T12:34:56Z" };
        }

        if (kind == 7)
        {
            byte[] bytes = new byte[random.Next(0, 16)];
            random.NextBytes(bytes);
            return new UiValue { Kind = UiValueKind.Binary, BinaryValue = bytes };
        }

        if (kind == 8)
        {
            int count = random.Next(0, 5);
            UiValue[] values = new UiValue[count];
            int index = 0;
            while (index < count)
            {
                values[index] = RandomValue(random, depth + 1);
                index++;
            }

            return new UiValue { Kind = UiValueKind.Array, ArrayValue = values };
        }

        int entryCount = random.Next(0, 5);
        UiValueEntry[] entries = new UiValueEntry[entryCount];
        int entryIndex = 0;
        while (entryIndex < entryCount)
        {
            entries[entryIndex] = new UiValueEntry
            {
                Key = "key-" + entryIndex,
                Value = RandomValue(random, depth + 1)
            };
            entryIndex++;
        }

        Shuffle(entries, random);
        return new UiValue { Kind = UiValueKind.Object, ObjectValue = entries };
    }

    private static DomNode RandomDomTree(Random random, bool changed)
    {
        int count = random.Next(3, 8);
        List<int> keys = Enumerable.Range(0, count).ToList();
        Shuffle(keys, random);
        if (changed)
        {
            keys.RemoveAt(0);
            keys.Add(count);
            Shuffle(keys, random);
        }

        DomNode[] children = keys.Select(key => new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "item-" + key,
            Id = "item-id-" + key,
            Name = HtmlElements.Division,
            Attributes = new DomAttribute[]
            {
                new DomAttribute { Name = HtmlAttributes.Title, Value = changed && key % 2 == 0 ? "after-" + key : "value-" + key }
            },
            Children = new DomNode[]
            {
                new DomNode
                {
                    Kind = DomNodeKind.Text,
                    Key = "text-" + key,
                    Id = "text-id-" + key,
                    Text = changed && key % 3 == 0 ? "changed-" + key : "text-" + key
                }
            }
        }).ToArray();
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "root",
            Id = "root-id",
            Name = HtmlElements.Main,
            Children = children
        };
    }

    private static string MutationSignature(DomMutation[] mutations)
    {
        return string.Join("|", mutations.Select(mutation =>
            mutation.Kind + ":"
            + mutation.TargetId + ":"
            + mutation.ParentId + ":"
            + mutation.ReferenceId + ":"
            + mutation.Name + ":"
            + mutation.Value + ":"
            + mutation.Index + ":"
            + mutation.FromIndex + ":"
            + mutation.ToIndex));
    }

    private static UiApplication HostileApplication(int index)
    {
        UiApplication application = RandomApplication(50000 + index);
        UiNode root = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        int corruption = index % 6;
        if (corruption == 0)
        {
            application.Windows = new UiWindow[] { null! };
        }
        else if (corruption == 1)
        {
            root.Children = new UiNode[] { root.Children[0], root.Children[0] };
        }
        else if (corruption == 2)
        {
            root.Kind = (UiNodeKind)int.MaxValue;
        }
        else if (corruption == 3)
        {
            root.Children[0].Kind = UiNodeKind.RawContent;
            root.Children[0].Text = "<script>hostile()</script>";
            root.Children[0].RawContentPolicy = null;
        }
        else if (corruption == 4)
        {
            root.Children = new UiNode[] { null! };
        }
        else
        {
            root.Attributes = new DomAttribute[] { new DomAttribute { Name = "bad attribute", Value = "\0" } };
        }

        return application;
    }

    private static void AssertExpectedInputFailure(TestDelegate action, string message)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            Assert.That(
                exception,
                Is.InstanceOf<ArgumentException>().Or.InstanceOf<JsonException>().Or.InstanceOf<FormatException>().Or.InstanceOf<UiRenderException>().Or.InstanceOf<UiModelException>(),
                message);
        }
    }

    private static string RandomHostileJson(Random random, int length)
    {
        const string alphabet = "{}[],:\\\"nulltruefalse0123456789abcdefghijklmnopqrstuvwxyz <>\0";
        char[] characters = new char[length];
        int index = 0;
        while (index < characters.Length)
        {
            characters[index] = alphabet[random.Next(alphabet.Length)];
            index++;
        }

        return new string(characters);
    }

    private static string RandomText(Random random, int maximumLength)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyz ABCXYZ0123456789<>&\\\"'";
        int length = random.Next(maximumLength + 1);
        char[] characters = new char[length];
        int index = 0;
        while (index < characters.Length)
        {
            characters[index] = alphabet[random.Next(alphabet.Length)];
            index++;
        }

        return new string(characters);
    }

    private static UiValue ObjectValue(string key, UiValue value)
    {
        return new UiValue
        {
            Kind = UiValueKind.Object,
            ObjectValue = new UiValueEntry[] { new UiValueEntry { Key = key, Value = value } }
        };
    }

    private static UiValue Integer(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static void Shuffle<T>(IList<T> values, Random random)
    {
        int index = values.Count - 1;
        while (index > 0)
        {
            int other = random.Next(index + 1);
            T value = values[index];
            values[index] = values[other];
            values[other] = value;
            index--;
        }
    }

    private sealed class ScenarioEventHandler : IUiEventHandler
    {
        private readonly bool handled;
        private readonly bool preventDefault;
        private readonly bool stopPropagation;

        public ScenarioEventHandler(bool handled, bool preventDefault, bool stopPropagation)
        {
            this.handled = handled;
            this.preventDefault = preventDefault;
            this.stopPropagation = stopPropagation;
        }

        public UiEventHandlerResult HandleUiEvent(UiEventHandlerContext context)
        {
            return new UiEventHandlerResult
            {
                Success = true,
                Handled = handled,
                PreventDefault = preventDefault,
                StopPropagation = stopPropagation
            };
        }
    }
}
