using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

[NonParallelizable]
public class UiRendererDeterminismTests
{
    [Test]
    public void RenderingIsByteIdenticalAcrossCulturesAndTimeZones()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        string? originalTimeZone = Environment.GetEnvironmentVariable("TZ");
        try
        {
            UiRenderResult? baseline = null;
            string[] cultures = new string[] { "en-US", "fr-FR", "tr-TR", "ar-SA" };
            string[] timeZones = new string[] { "UTC", "America/Los_Angeles", "Asia/Tokyo", "Europe/Paris" };
            int index = 0;
            while (index < cultures.Length)
            {
                CultureInfo.CurrentCulture = new CultureInfo(cultures[index]);
                CultureInfo.CurrentUICulture = new CultureInfo(cultures[index]);
                Environment.SetEnvironmentVariable("TZ", timeZones[index]);
                TimeZoneInfo.ClearCachedData();

                UiRenderResult current = new UiRenderer().RenderDocument(Application(17, "\n", "determinism-seed", "Content"));
                Assert.That(current.Diagnostics, Is.Empty, cultures[index] + "/" + timeZones[index]);
                if (baseline == null)
                {
                    baseline = current;
                }
                else
                {
                    AssertEquivalentResults(baseline, current, cultures[index] + "/" + timeZones[index]);
                }

                index++;
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
            Environment.SetEnvironmentVariable("TZ", originalTimeZone);
            TimeZoneInfo.ClearCachedData();
        }
    }

    [Test]
    public void RandomizedMapInsertionOrderProducesByteIdenticalResults()
    {
        UiRenderResult baseline = new UiRenderer().RenderDocument(Application(0, "\n", "determinism-seed", "Content"));

        int seed = 1;
        while (seed <= 24)
        {
            UiRenderResult current = new UiRenderer().RenderDocument(Application(seed, "\n", "determinism-seed", "Content"));
            AssertEquivalentResults(baseline, current, "shuffle seed " + seed.ToString(CultureInfo.InvariantCulture));
            seed++;
        }
    }

    [Test]
    public void RepeatedAndEquivalentModelsPreserveStableIdsAndHashes()
    {
        UiApplication firstModel = Application(8, "\n", "determinism-seed", "Content");
        UiApplication equivalentModel = Application(19, "\n", "determinism-seed", "Content");
        IUiRenderer renderer = new UiRenderer();

        UiRenderResult first = renderer.RenderDocument(firstModel);
        UiRenderResult repeated = renderer.RenderDocument(firstModel);
        UiRenderResult equivalent = new UiRenderer().RenderDocument(equivalentModel);

        Assert.Multiple(() =>
        {
            Assert.That(first.Diagnostics, Is.Empty);
            Assert.That(repeated.Snapshot, Is.Not.SameAs(first.Snapshot));
            Assert.That(equivalent.Snapshot, Is.Not.SameAs(first.Snapshot));
            AssertEquivalentResults(first, repeated, "repeated model");
            AssertEquivalentResults(first, equivalent, "equivalent model");
        });
    }

    [Test]
    public void LineEndingVariantsNormalizeToByteIdenticalResults()
    {
        UiRenderResult lineFeed = new UiRenderer().RenderDocument(Application(3, "\n", "determinism-seed", "Content"));
        UiRenderResult carriageReturnLineFeed = new UiRenderer().RenderDocument(Application(3, "\r\n", "determinism-seed", "Content"));
        UiRenderResult carriageReturn = new UiRenderer().RenderDocument(Application(3, "\r", "determinism-seed", "Content"));

        Assert.Multiple(() =>
        {
            Assert.That(lineFeed.Html, Does.Contain("First line\nSecond line"));
            Assert.That(lineFeed.Html, Does.Not.Contain("\r"));
            AssertEquivalentResults(lineFeed, carriageReturnLineFeed, "CRLF");
            AssertEquivalentResults(lineFeed, carriageReturn, "CR");
        });
    }

    [Test]
    public void StableIdsAndHashesChangeOnlyWithIdentityOrContent()
    {
        UiRenderResult baseline = new UiRenderer().RenderDocument(Application(4, "\n", "determinism-seed", "Content"));
        UiRenderResult changedContent = new UiRenderer().RenderDocument(Application(4, "\n", "determinism-seed", "Changed"));
        UiRenderResult changedIdentity = new UiRenderer().RenderDocument(Application(4, "\n", "other-seed", "Content"));
        string[] baselineIds = SnapshotIdentities(baseline);
        string[] contentIds = SnapshotIdentities(changedContent);
        string[] identityIds = SnapshotIdentities(changedIdentity);

        Assert.Multiple(() =>
        {
            Assert.That(baseline.Diagnostics, Is.Empty);
            Assert.That(changedContent.Diagnostics, Is.Empty);
            Assert.That(changedIdentity.Diagnostics, Is.Empty);
            Assert.That(contentIds, Is.EqualTo(baselineIds));
            Assert.That(identityIds, Is.Not.EqualTo(baselineIds));
            Assert.That(changedContent.Snapshot!.ContentHash, Is.Not.EqualTo(baseline.Snapshot!.ContentHash));
            Assert.That(changedContent.Hash, Is.Not.EqualTo(baseline.Hash));
            Assert.That(changedIdentity.Snapshot!.ContentHash, Is.Not.EqualTo(baseline.Snapshot.ContentHash));
            Assert.That(changedIdentity.Hash, Is.Not.EqualTo(baseline.Hash));
        });
    }

    private static UiApplication Application(int orderSeed, string lineEnding, string rootSeed, string content)
    {
        Random random = new Random(orderSeed);
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication(rootSeed, "application");
        UiWindow activeWindow = factory.CreateWindow(rootSeed, application.Id, "window-z");
        UiWindow inactiveWindow = factory.CreateWindow(rootSeed, application.Id, "window-a");
        inactiveWindow.IsActive = false;
        UiPage page = factory.CreatePage(rootSeed, activeWindow.Id, "page-z");
        UiPage sparePage = factory.CreatePage(rootSeed, activeWindow.Id, "page-a");
        UiView view = factory.CreateView(rootSeed, page.Id, "view");
        UiNode root = factory.CreateElementNode(rootSeed, view.Id, "root", null, HtmlElements.Main);
        UiNode text = factory.CreateTextNode(rootSeed, root.Identity.StableId, "message", "First line" + lineEnding + "Second line: " + content);

        root.Children = new UiNode[] { text };
        root.Attributes = Shuffled(random, new DomAttribute[]
        {
            new DomAttribute { Name = "title", Value = "Deterministic " + content },
            new DomAttribute { Name = "aria-label", Value = "Content" }
        });
        root.TokenLists = new UiTokenList[]
        {
            new UiTokenList { Name = HtmlAttributes.Class, Tokens = Shuffled(random, new string[] { "zeta", "alpha", "alpha" }) }
        };
        root.DataAttributes = Shuffled(random, new UiValueEntry[]
        {
            Entry("zeta", Text("last")),
            Entry("alpha", Decimal(1234.5m))
        });
        root.Properties = Shuffled(random, new DomProperty[]
        {
            new DomProperty { Name = "zetaState", Value = ObjectValue(random) },
            new DomProperty { Name = "alphaState", Value = Text("ready" + lineEnding + "now") }
        });
        root.Style = new UiStyle
        {
            Key = "root-style",
            Declarations = Shuffled(random, new UiCssDeclaration[]
            {
                Declaration(CssProperties.Display, "grid"),
                Declaration(CssProperties.Border, "1px solid navy")
            })
        };
        view.Parameters = Shuffled(random, new UiValueEntry[]
        {
            Entry("zeta", Integer(9)),
            Entry("alpha", Text("parameter"))
        });
        view.ContentRoot = root;
        page.Title = "Deterministic" + lineEnding + "Page";
        page.Metadata.Description = "First" + lineEnding + "Second";
        page.Metadata.Values = Shuffled(random, new UiValueEntry[]
        {
            Entry("zeta-meta", Text("zeta")),
            Entry("alpha-meta", Decimal(4.25m))
        });
        page.Resources = Shuffled(random, new UiResource[]
        {
            new UiResource
            {
                Key = "zeta-resource",
                Url = "/zeta.css",
                Relationship = "stylesheet",
                Attributes = Shuffled(random, new UiValueEntry[]
                {
                    Entry("title", Text("first" + lineEnding + "second")),
                    Entry("fetchpriority", Text("high"))
                })
            },
            new UiResource { Key = "alpha-resource", Url = "/alpha.css", Relationship = "stylesheet" }
        });
        page.Styles = Shuffled(random, new UiStyle[]
        {
            Style("zeta-style", CssProperties.BackgroundColor, "white"),
            Style("alpha-style", CssProperties.Color, "navy")
        });
        page.ContentView = view;
        sparePage.Title = "Spare";
        activeWindow.Pages = Shuffled(random, new UiPage[] { page, sparePage });
        activeWindow.CurrentPageKey = page.Key;
        application.Windows = Shuffled(random, new UiWindow[] { activeWindow, inactiveWindow });
        application.State = Shuffled(random, new UiValueEntry[]
        {
            Entry("zeta-state", Text("state" + lineEnding + "value")),
            Entry("alpha-state", Decimal(9876.5m))
        });
        application.Theme = new UiTheme
        {
            Key = "theme",
            Tokens = Shuffled(random, new UiValueEntry[]
            {
                Entry("zeta-token", Decimal(12.5m)),
                Entry("alpha-token", Text("navy"))
            })
        };
        application.RenderOptions = new UiRenderOptions
        {
            FullDocument = true,
            IncludeHydration = true,
            IncludeBootstrap = true,
            ContentSecurityPolicyNonce = "determinism-nonce",
            MaximumDiagnostics = 100
        };
        return application;
    }

    private static UiStyle Style(string key, string property, string value)
    {
        return new UiStyle
        {
            Key = key,
            Declarations = new UiCssDeclaration[] { Declaration(property, value) }
        };
    }

    private static UiCssDeclaration Declaration(string property, string value)
    {
        return new UiCssDeclaration { Property = property, Value = value, IsTrusted = true };
    }

    private static UiValue ObjectValue(Random random)
    {
        return new UiValue
        {
            Kind = UiValueKind.Object,
            ObjectValue = Shuffled(random, new UiValueEntry[]
            {
                Entry("zeta", Integer(2)),
                Entry("alpha", Decimal(1.5m))
            })
        };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue Integer(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }

    private static UiValue Decimal(decimal value)
    {
        return new UiValue { Kind = UiValueKind.Decimal, DecimalValue = value };
    }

    private static T[] Shuffled<T>(Random random, T[] values)
    {
        int index = values.Length - 1;
        while (index > 0)
        {
            int replacement = random.Next(index + 1);
            T current = values[index];
            values[index] = values[replacement];
            values[replacement] = current;
            index--;
        }

        return values;
    }

    private static void AssertEquivalentResults(UiRenderResult expected, UiRenderResult actual, string message)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Diagnostics, Is.Empty, message + " diagnostics");
            Assert.That(actual.Html, Is.EqualTo(expected.Html), message + " HTML");
            Assert.That(actual.Css, Is.EqualTo(expected.Css), message + " CSS");
            Assert.That(actual.Bootstrap, Is.EqualTo(expected.Bootstrap), message + " bootstrap");
            Assert.That(actual.Hash, Is.EqualTo(expected.Hash), message + " output hash");
            Assert.That(actual.Snapshot!.SessionId, Is.EqualTo(expected.Snapshot!.SessionId), message + " session ID");
            Assert.That(actual.Snapshot.RootSeed, Is.EqualTo(expected.Snapshot.RootSeed), message + " root seed");
            Assert.That(actual.Snapshot.ContentHash, Is.EqualTo(expected.Snapshot.ContentHash), message + " snapshot hash");
            Assert.That(SnapshotAnnotations(actual), Is.EqualTo(SnapshotAnnotations(expected)), message + " node annotations");
            Assert.That(actual.Hydration!.Hash, Is.EqualTo(expected.Hydration!.Hash), message + " hydration hash");
            Assert.That(HydrationEntries(actual.Hydration.Nodes), Is.EqualTo(HydrationEntries(expected.Hydration.Nodes)), message + " hydration nodes");
            Assert.That(HydrationEntries(actual.Hydration.State), Is.EqualTo(HydrationEntries(expected.Hydration.State)), message + " hydration state");
            Assert.That(actual.Resources.Select(resource => resource.Key), Is.EqualTo(expected.Resources.Select(resource => resource.Key)), message + " resources");
        });
    }

    private static string[] SnapshotIdentities(UiRenderResult result)
    {
        List<string> values = new List<string>();
        DomNode[] roots = result.Snapshot!.Roots;
        int index = 0;
        while (index < roots.Length)
        {
            AddIdentity(values, roots[index]);
            index++;
        }

        return values.ToArray();
    }

    private static string[] SnapshotAnnotations(UiRenderResult result)
    {
        List<string> values = new List<string>();
        DomNode[] roots = result.Snapshot!.Roots;
        int index = 0;
        while (index < roots.Length)
        {
            AddAnnotation(values, roots[index]);
            index++;
        }

        return values.ToArray();
    }

    private static void AddIdentity(List<string> values, DomNode node)
    {
        values.Add(node.Key + "|" + node.Id);
        int index = 0;
        while (index < node.Children.Length)
        {
            AddIdentity(values, node.Children[index]);
            index++;
        }
    }

    private static void AddAnnotation(List<string> values, DomNode node)
    {
        values.Add(node.Sequence.ToString(CultureInfo.InvariantCulture) + "|" + node.Key + "|" + node.Id + "|" + node.SubtreeHash);
        int index = 0;
        while (index < node.Children.Length)
        {
            AddAnnotation(values, node.Children[index]);
            index++;
        }
    }

    private static string[] HydrationEntries(UiValueEntry[] entries)
    {
        return Array.ConvertAll(entries, entry => entry.Key + "|" + ValueJsonCodec.SerializeValue(entry.Value, 64, 10000, 1048576, 1048576));
    }
}
