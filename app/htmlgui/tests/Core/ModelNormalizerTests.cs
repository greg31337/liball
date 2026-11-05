using System;
using System.Globalization;
using htmlgui;

namespace htmlgui.Tests;

public class ModelNormalizerTests
{
    [Test]
    public void ProducesADeepIndependentCopyAndPreservesPresentationOrder()
    {
        UiApplication source = CreateApplicationWithContent();
        UiNode sourceRoot = source.Windows[0].Pages[0].ContentView!.ContentRoot!;
        UiNode firstVisualChild = sourceRoot.Children[0];
        UiNode secondVisualChild = sourceRoot.Children[1];
        ModelNormalizer normalizer = new ModelNormalizer();

        UiApplication normalized = normalizer.NormalizeApplication(source);
        UiNode normalizedRoot = normalized.Windows[0].Pages[0].ContentView!.ContentRoot!;

        sourceRoot.Text = "changed";
        sourceRoot.Values[0].Value.StringValue = "changed";
        sourceRoot.Children[0].Text = "changed";
        normalizedRoot.Children[1].Name = "changed-normalized";

        Assert.Multiple(() =>
        {
            Assert.That(normalized, Is.Not.SameAs(source));
            Assert.That(normalized.Windows, Is.Not.SameAs(source.Windows));
            Assert.That(normalized.Windows[0], Is.Not.SameAs(source.Windows[0]));
            Assert.That(normalizedRoot, Is.Not.SameAs(sourceRoot));
            Assert.That(normalizedRoot.Identity, Is.Not.SameAs(sourceRoot.Identity));
            Assert.That(normalizedRoot.State, Is.Not.SameAs(sourceRoot.State));
            Assert.That(normalizedRoot.Values, Is.Not.SameAs(sourceRoot.Values));
            Assert.That(normalizedRoot.Values[0].Value, Is.Not.SameAs(sourceRoot.Values[1].Value));
            Assert.That(normalizedRoot.Values[0].Key, Is.EqualTo("alpha"));
            Assert.That(normalizedRoot.Values[0].Value.StringValue, Is.EqualTo("first"));
            Assert.That(normalizedRoot.Text, Is.EqualTo("root\ntext"));
            Assert.That(normalizedRoot.Children[0].Identity.AuthorKey, Is.EqualTo(firstVisualChild.Identity.AuthorKey));
            Assert.That(normalizedRoot.Children[1].Identity.AuthorKey, Is.EqualTo(secondVisualChild.Identity.AuthorKey));
            Assert.That(normalizedRoot.Children[0].Text, Is.EqualTo("second\nline"));
            Assert.That(sourceRoot.Children[1].Name, Is.Not.EqualTo("changed-normalized"));
        });
    }

    [Test]
    public void AppliesWindowMetadataInheritanceAndLocalPageOverrides()
    {
        IUiFactory factory = new UiFactory();
        UiApplication source = factory.CreateApplication("seed", "application");
        UiWindow window = factory.CreateWindow(source.RootSeed, source.Id, "window");
        window.Document = new UiDocumentMetadata
        {
            Title = "Window title",
            Language = "en-GB",
            TextDirection = "rtl",
            CharacterSet = "utf-16",
            Viewport = "width=900",
            BaseUrl = "https://example.test/app/",
            Description = "Inherited description",
            ApplicationName = "Sample",
            ContentSecurityPolicyNonce = "nonce",
            Values = new UiValueEntry[] { Entry("theme", "dark"), Entry("region", "west") }
        };
        UiPage inheritedPage = factory.CreatePage(source.RootSeed, window.Id, "inherited");
        inheritedPage.Metadata.Description = "Local description";
        inheritedPage.Metadata.Values = new UiValueEntry[] { Entry("theme", "light") };
        UiPage overridingPage = factory.CreatePage(source.RootSeed, window.Id, "overriding");
        overridingPage.Title = "Page title";
        overridingPage.Language = "fr-FR";
        overridingPage.TextDirection = "ltr";
        window.Pages = new UiPage[] { overridingPage, inheritedPage };
        source.Windows = new UiWindow[] { window };
        ModelNormalizer normalizer = new ModelNormalizer();

        UiApplication normalized = normalizer.NormalizeApplication(source);
        UiPage normalizedInherited = normalized.Windows[0].Pages[0];
        UiPage normalizedOverriding = normalized.Windows[0].Pages[1];

        Assert.Multiple(() =>
        {
            Assert.That(normalizedInherited.Key, Is.EqualTo("inherited"));
            Assert.That(normalizedInherited.Title, Is.EqualTo("Window title"));
            Assert.That(normalizedInherited.Metadata.Title, Is.EqualTo("Window title"));
            Assert.That(normalizedInherited.Language, Is.EqualTo("en-GB"));
            Assert.That(normalizedInherited.Metadata.Language, Is.EqualTo("en-GB"));
            Assert.That(normalizedInherited.TextDirection, Is.EqualTo("rtl"));
            Assert.That(normalizedInherited.Metadata.TextDirection, Is.EqualTo("rtl"));
            Assert.That(normalizedInherited.Metadata.CharacterSet, Is.EqualTo("utf-16"));
            Assert.That(normalizedInherited.Metadata.Viewport, Is.EqualTo("width=900"));
            Assert.That(normalizedInherited.Metadata.BaseUrl, Is.EqualTo("https://example.test/app/"));
            Assert.That(normalizedInherited.Metadata.Description, Is.EqualTo("Local description"));
            Assert.That(normalizedInherited.Metadata.ApplicationName, Is.EqualTo("Sample"));
            Assert.That(normalizedInherited.Metadata.ContentSecurityPolicyNonce, Is.EqualTo("nonce"));
            Assert.That(normalizedInherited.Metadata.Values, Has.Length.EqualTo(2));
            Assert.That(normalizedInherited.Metadata.Values[0].Key, Is.EqualTo("region"));
            Assert.That(normalizedInherited.Metadata.Values[1].Key, Is.EqualTo("theme"));
            Assert.That(normalizedInherited.Metadata.Values[1].Value.StringValue, Is.EqualTo("light"));
            Assert.That(normalizedInherited.Metadata.Values[0], Is.Not.SameAs(normalized.Windows[0].Document.Values[0]));
            Assert.That(normalizedInherited.Metadata.Values[0].Value, Is.Not.SameAs(normalized.Windows[0].Document.Values[0].Value));
            Assert.That(normalizedOverriding.Key, Is.EqualTo("overriding"));
            Assert.That(normalizedOverriding.Title, Is.EqualTo("Page title"));
            Assert.That(normalizedOverriding.Language, Is.EqualTo("fr-FR"));
            Assert.That(normalizedOverriding.TextDirection, Is.EqualTo("ltr"));
            Assert.That(inheritedPage.Title, Is.Empty);
            Assert.That(inheritedPage.Language, Is.Null);
            Assert.That(inheritedPage.Metadata.Values, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void CanonicallyOrdersUnorderedRegistriesUsingOrdinalComparison()
    {
        IUiFactory factory = new UiFactory();
        UiApplication source = factory.CreateApplication("seed", "application");
        UiWindow upperWindow = factory.CreateWindow(source.RootSeed, source.Id, "I");
        UiWindow dotlessWindow = factory.CreateWindow(source.RootSeed, source.Id, "ı");
        UiWindow lowerWindow = factory.CreateWindow(source.RootSeed, source.Id, "i");
        upperWindow.HostCapabilities = new string[] { "zeta", "alpha" };
        source.Windows = new UiWindow[] { dotlessWindow, lowerWindow, upperWindow };
        source.RouteIds = new string[] { "z-route", "a-route" };
        source.CommandIds = new string[] { "z-command", "a-command" };
        source.Commands = new UiCommand[] { new UiCommand { Id = "z" }, new UiCommand { Id = "a" } };
        source.StateEntries = new UiStateEntry[] { new UiStateEntry { Id = "z" }, new UiStateEntry { Id = "a" } };
        source.Routes = new UiRoute[] { new UiRoute { Id = "z" }, new UiRoute { Id = "a" } };
        source.State = new UiValueEntry[] { Entry("z", "last"), Entry("a", "first") };
        ModelNormalizer normalizer = new ModelNormalizer();

        UiApplication normalized = normalizer.NormalizeApplication(source);

        Assert.Multiple(() =>
        {
            Assert.That(Array.ConvertAll(normalized.Windows, window => window.Key), Is.EqualTo(new string[] { "I", "i", "ı" }));
            Assert.That(normalized.Windows[0].HostCapabilities, Is.EqualTo(new string[] { "alpha", "zeta" }));
            Assert.That(normalized.RouteIds, Is.EqualTo(new string[] { "a-route", "z-route" }));
            Assert.That(normalized.CommandIds, Is.EqualTo(new string[] { "a-command", "z-command" }));
            Assert.That(Array.ConvertAll(normalized.Commands, command => command.Id), Is.EqualTo(new string[] { "a", "z" }));
            Assert.That(Array.ConvertAll(normalized.StateEntries, state => state.Id), Is.EqualTo(new string[] { "a", "z" }));
            Assert.That(Array.ConvertAll(normalized.Routes, route => route.Id), Is.EqualTo(new string[] { "a", "z" }));
            Assert.That(Array.ConvertAll(normalized.State, entry => entry.Key), Is.EqualTo(new string[] { "a", "z" }));
        });
    }

    [Test]
    public void EquivalentRegistryOrdersProduceTheSameCanonicalResultWithoutMutation()
    {
        UiApplication forward = CreateApplicationWithRegistries(false);
        UiApplication reverse = CreateApplicationWithRegistries(true);
        UiWindow[] forwardWindows = forward.Windows;
        UiWindow[] reverseWindows = reverse.Windows;
        ModelNormalizer normalizer = new ModelNormalizer();

        UiApplication normalizedForward = normalizer.NormalizeApplication(forward);
        UiApplication normalizedReverse = normalizer.NormalizeApplication(reverse);

        Assert.Multiple(() =>
        {
            Assert.That(Array.ConvertAll(normalizedForward.Windows, window => window.Key), Is.EqualTo(Array.ConvertAll(normalizedReverse.Windows, window => window.Key)));
            Assert.That(Array.ConvertAll(normalizedForward.Commands, command => command.Id), Is.EqualTo(Array.ConvertAll(normalizedReverse.Commands, command => command.Id)));
            Assert.That(Array.ConvertAll(normalizedForward.StateEntries, state => state.Id), Is.EqualTo(Array.ConvertAll(normalizedReverse.StateEntries, state => state.Id)));
            Assert.That(Array.ConvertAll(normalizedForward.Routes, route => route.Id), Is.EqualTo(Array.ConvertAll(normalizedReverse.Routes, route => route.Id)));
            Assert.That(forward.Windows, Is.SameAs(forwardWindows));
            Assert.That(reverse.Windows, Is.SameAs(reverseWindows));
            Assert.That(Array.ConvertAll(forward.Windows, window => window.Key), Is.EqualTo(new string[] { "alpha", "zeta" }));
            Assert.That(Array.ConvertAll(reverse.Windows, window => window.Key), Is.EqualTo(new string[] { "zeta", "alpha" }));
        });
    }

    [Test]
    public void NormalizationIsIndependentOfCurrentCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        UiApplication source = CreateApplicationWithContent();
        UiApplication turkish;
        UiApplication french;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
            turkish = new ModelNormalizer().NormalizeApplication(source);
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            french = new ModelNormalizer().NormalizeApplication(source);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }

        UiNode turkishRoot = turkish.Windows[0].Pages[0].ContentView!.ContentRoot!;
        UiNode frenchRoot = french.Windows[0].Pages[0].ContentView!.ContentRoot!;
        Assert.Multiple(() =>
        {
            Assert.That(Array.ConvertAll(turkishRoot.Values, entry => entry.Key), Is.EqualTo(Array.ConvertAll(frenchRoot.Values, entry => entry.Key)));
            Assert.That(turkishRoot.Text, Is.EqualTo(frenchRoot.Text));
            Assert.That(turkishRoot.Identity.StableId, Is.EqualTo(frenchRoot.Identity.StableId));
        });
    }

    [Test]
    public void RestoresSafeInitializedDefaultsForNullRequiredFields()
    {
        UiApplication source = new UiApplication
        {
            Id = null!,
            RouteIds = null!,
            Windows = null!,
            RenderOptions = null!
        };
        ModelNormalizer normalizer = new ModelNormalizer();

        UiApplication normalized = normalizer.NormalizeApplication(source);

        Assert.Multiple(() =>
        {
            Assert.That(normalized.Id, Is.Empty);
            Assert.That(normalized.RouteIds, Is.Empty);
            Assert.That(normalized.Windows, Is.Empty);
            Assert.That(normalized.RenderOptions, Is.Not.Null);
            Assert.That(normalized.RenderOptions.FullDocument, Is.True);
            Assert.That(normalized.RenderOptions.IncludeHydration, Is.True);
            Assert.That(normalized.RenderOptions.MaximumDiagnostics, Is.EqualTo(100));
            Assert.That(source.Id, Is.Null);
            Assert.That(source.RouteIds, Is.Null);
            Assert.That(source.Windows, Is.Null);
            Assert.That(source.RenderOptions, Is.Null);
        });
    }

    [Test]
    public void PreservesInactiveTaggedValuePayloadsAsNull()
    {
        UiApplication source = CreateApplicationWithContent();
        source.State = new UiValueEntry[]
        {
            new UiValueEntry { Key = "count", Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = 4 } },
            new UiValueEntry { Key = "label", Value = new UiValue { Kind = UiValueKind.String, StringValue = "ready" } }
        };

        UiApplication normalized = new ModelNormalizer().NormalizeApplication(source);

        Assert.Multiple(() =>
        {
            Assert.That(normalized.State[0].Value.ArrayValue, Is.Null);
            Assert.That(normalized.State[0].Value.ObjectValue, Is.Null);
            Assert.That(normalized.State[1].Value.BinaryValue, Is.Null);
            Assert.That(ValueJsonCodec.SerializeValue(normalized.State[0].Value, 8, 20, 100, 100), Does.Contain("\"value\":4"));
            Assert.That(ValueJsonCodec.SerializeValue(normalized.State[1].Value, 8, 20, 100, 100), Does.Contain("\"value\":\"ready\""));
        });
    }

    [Test]
    public void CopiesCyclesWithoutRecursingForeverOrChangingTheirShape()
    {
        UiApplication source = CreateApplicationWithContent();
        UiNode sourceRoot = source.Windows[0].Pages[0].ContentView!.ContentRoot!;
        sourceRoot.Children = new UiNode[] { sourceRoot };
        ModelNormalizer normalizer = new ModelNormalizer();

        UiApplication normalized = normalizer.NormalizeApplication(source);
        UiNode normalizedRoot = normalized.Windows[0].Pages[0].ContentView!.ContentRoot!;

        Assert.Multiple(() =>
        {
            Assert.That(normalizedRoot, Is.Not.SameAs(sourceRoot));
            Assert.That(normalizedRoot.Children, Is.Not.SameAs(sourceRoot.Children));
            Assert.That(normalizedRoot.Children[0], Is.SameAs(normalizedRoot));
            Assert.That(sourceRoot.Children[0], Is.SameAs(sourceRoot));
        });
    }

    [Test]
    public void RejectsNullApplication()
    {
        ModelNormalizer normalizer = new ModelNormalizer();

        Assert.Throws<ArgumentNullException>(() => normalizer.NormalizeApplication(null));
    }

    private static UiApplication CreateApplicationWithContent()
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("normalizer-seed", "application");
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, "window");
        UiPage page = factory.CreatePage(application.RootSeed, window.Id, "page");
        UiView view = factory.CreateView(application.RootSeed, page.Id, "view");
        UiNode root = factory.CreateElementNode(application.RootSeed, view.Id, "root", null, "main");
        UiNode second = factory.CreateTextNode(application.RootSeed, root.Identity.StableId, "second", "second\rline");
        UiNode first = factory.CreateElementNode(application.RootSeed, root.Identity.StableId, "first", null, "section");
        root.Text = "root\r\ntext";
        root.Values = new UiValueEntry[] { Entry("zeta", "last"), Entry("alpha", "first") };
        root.Children = new UiNode[] { second, first };
        view.ContentRoot = root;
        page.ContentView = view;
        window.Pages = new UiPage[] { page };
        application.Windows = new UiWindow[] { window };
        return application;
    }

    private static UiApplication CreateApplicationWithRegistries(bool reverse)
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("registry-seed", "application");
        UiWindow alphaWindow = factory.CreateWindow(application.RootSeed, application.Id, "alpha");
        UiWindow zetaWindow = factory.CreateWindow(application.RootSeed, application.Id, "zeta");
        UiCommand alphaCommand = new UiCommand { Id = "alpha" };
        UiCommand zetaCommand = new UiCommand { Id = "zeta" };
        UiStateEntry alphaState = new UiStateEntry { Id = "alpha" };
        UiStateEntry zetaState = new UiStateEntry { Id = "zeta" };
        UiRoute alphaRoute = new UiRoute { Id = "alpha" };
        UiRoute zetaRoute = new UiRoute { Id = "zeta" };
        if (reverse)
        {
            application.Windows = new UiWindow[] { zetaWindow, alphaWindow };
            application.Commands = new UiCommand[] { zetaCommand, alphaCommand };
            application.StateEntries = new UiStateEntry[] { zetaState, alphaState };
            application.Routes = new UiRoute[] { zetaRoute, alphaRoute };
        }
        else
        {
            application.Windows = new UiWindow[] { alphaWindow, zetaWindow };
            application.Commands = new UiCommand[] { alphaCommand, zetaCommand };
            application.StateEntries = new UiStateEntry[] { alphaState, zetaState };
            application.Routes = new UiRoute[] { alphaRoute, zetaRoute };
        }

        return application;
    }

    private static UiValueEntry Entry(string key, string value)
    {
        return new UiValueEntry
        {
            Key = key,
            Value = new UiValue { Kind = UiValueKind.String, StringValue = value }
        };
    }
}
