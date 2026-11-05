using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class RouteResolverCoreTests
{
    [Test]
    public void ResolvesNestedAndIndexRoutesWithInheritedParametersAndLayoutContent()
    {
        RouteResolverCore core = new RouteResolverCore();
        UiRoute account = Route("account", "/accounts/{accountId:int}", LayoutView("account-layout", "main"));
        UiRoute index = Route("account-index", "/", ContentView("overview", "Overview"));
        index.ParentId = account.Id;
        index.OutletName = "main";
        index.IsIndex = true;
        UiRoute settings = Route("settings", "/settings/{section=profile}", ContentView("settings", "Settings"));
        settings.ParentId = account.Id;
        settings.OutletName = "main";
        UiRoute[] routes = new UiRoute[] { settings, index, account };

        RouteResolutionResult indexResult = core.ResolveRouteTree(routes, System.Array.Empty<UiPage>(), null, "/app/accounts/42", "/app/");
        RouteResolutionResult settingsResult = core.ResolveRouteTree(routes, System.Array.Empty<UiPage>(), null, "/app/accounts/42/settings?mode=advanced#top", "/app/");

        Assert.Multiple(() =>
        {
            Assert.That(indexResult.Success, Is.True);
            Assert.That(indexResult.Status, Is.EqualTo(UiNavigationStatus.Resolved));
            Assert.That(indexResult.RouteData!.RouteId, Is.EqualTo("account-index"));
            Assert.That(indexResult.RouteData.Parameters.Single().Key, Is.EqualTo("accountId"));
            Assert.That(indexResult.RouteData.Parameters.Single().Value.IntegerValue, Is.EqualTo(42));
            Assert.That(indexResult.RouteChain.Select(item => item.Id), Is.EqualTo(new string[] { "account", "account-index" }));
            Assert.That(indexResult.View!.Key, Is.EqualTo("account-layout"));
            Assert.That(indexResult.View.ContentRoot!.Children[1].Text, Is.EqualTo("Overview"));
            Assert.That(account.View!.ContentRoot!.Children[1].Kind, Is.EqualTo(UiNodeKind.Slot));
            Assert.That(settingsResult.RouteData!.RouteId, Is.EqualTo("settings"));
            Assert.That(settingsResult.RouteData.Parameters.Select(item => item.Key), Is.EqualTo(new string[] { "accountId", "section" }));
            Assert.That(settingsResult.RouteData.Parameters[1].Value.StringValue, Is.EqualTo("profile"));
            Assert.That(settingsResult.RouteData.Query.Single().Value.StringValue, Is.EqualTo("advanced"));
            Assert.That(settingsResult.RouteData.Fragment, Is.EqualTo("top"));
            Assert.That(settingsResult.View!.ContentRoot!.Children[1].Text, Is.EqualTo("Settings"));
        });
    }

    [Test]
    public void ComposesMultipleNamedLayoutOutletsFromLeafToRoot()
    {
        RouteResolverCore core = new RouteResolverCore();
        UiRoute root = Route("root", "/workspace", LayoutView("workspace-layout", "body"));
        UiRoute section = Route("section", "/reports", LayoutView("reports-layout", "detail"));
        section.ParentId = root.Id;
        section.OutletName = "body";
        UiRoute report = Route("report", "/{reportId:int}", ContentView("report", "Quarterly report"));
        report.ParentId = section.Id;
        report.OutletName = "detail";

        RouteResolutionResult result = core.ResolveRouteTree(new UiRoute[] { report, root, section }, null, null, "/workspace/reports/7", "/");
        UiNode reportsLayout = result.View!.ContentRoot!.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.RouteChain.Select(item => item.Id), Is.EqualTo(new string[] { "root", "section", "report" }));
            Assert.That(result.RouteData!.Parameters.Single().Value.IntegerValue, Is.EqualTo(7));
            Assert.That(reportsLayout.Name, Is.EqualTo("reports-layout-root"));
            Assert.That(reportsLayout.Children[1].Text, Is.EqualTo("Quarterly report"));
            Assert.That(section.View!.ContentRoot!.Children[1].Kind, Is.EqualTo(UiNodeKind.Slot));
        });
    }

    [Test]
    public void UsesTheDirectChildOutletAcrossAViewlessGroupingRoute()
    {
        RouteResolverCore core = new RouteResolverCore();
        UiRoute root = Route("root", "/root", LayoutView("root-layout", "group-content"));
        UiRoute group = Route("group", "/group", null);
        group.ParentId = root.Id;
        group.OutletName = "group-content";
        UiRoute leaf = Route("leaf", "/leaf", ContentView("leaf", "Leaf content"));
        leaf.ParentId = group.Id;
        leaf.OutletName = "unused-on-viewless-parent";

        RouteResolutionResult result = core.ResolveRouteTree(new UiRoute[] { leaf, group, root }, null, null, "/root/group/leaf", "/");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.RouteChain.Select(item => item.Id), Is.EqualTo(new string[] { "root", "group", "leaf" }));
            Assert.That(result.View!.ContentRoot!.Children[1].Text, Is.EqualTo("Leaf content"));
        });
    }

    [Test]
    public void SelectsTheDeepestNotFoundRouteAndInheritsParentRouteData()
    {
        RouteResolverCore core = new RouteResolverCore();
        UiRoute docs = Route("docs", "/docs/{version}", LayoutView("docs-layout", "outlet"));
        UiRoute nestedMissing = Route("docs-missing", "/", ContentView("docs-missing", "Missing documentation"));
        nestedMissing.ParentId = docs.Id;
        nestedMissing.IsNotFound = true;
        UiRoute globalMissing = Route("global-missing", "/", ContentView("global-missing", "Missing page"));
        globalMissing.IsNotFound = true;
        UiRoute[] routes = new UiRoute[] { globalMissing, nestedMissing, docs };

        RouteResolutionResult nested = core.ResolveRouteTree(routes, null, null, "/docs/v2/unknown/page?from=search", "/");
        RouteResolutionResult global = core.ResolveRouteTree(routes, null, null, "/elsewhere", "/");

        Assert.Multiple(() =>
        {
            Assert.That(nested.Success, Is.True);
            Assert.That(nested.Status, Is.EqualTo(UiNavigationStatus.NotFound));
            Assert.That(nested.RouteData!.RouteId, Is.EqualTo("docs-missing"));
            Assert.That(nested.RouteData.Parameters.Single().Key, Is.EqualTo("version"));
            Assert.That(nested.RouteData.Parameters.Single().Value.StringValue, Is.EqualTo("v2"));
            Assert.That(nested.RouteData.Query.Single().Value.StringValue, Is.EqualTo("search"));
            Assert.That(nested.RouteChain.Select(item => item.Id), Is.EqualTo(new string[] { "docs", "docs-missing" }));
            Assert.That(nested.View!.ContentRoot!.Children[1].Text, Is.EqualTo("Missing documentation"));
            Assert.That(global.Status, Is.EqualTo(UiNavigationStatus.NotFound));
            Assert.That(global.RouteData!.RouteId, Is.EqualTo("global-missing"));
            Assert.That(global.View!.ContentRoot!.Text, Is.EqualTo("Missing page"));
        });
    }

    [Test]
    public void ResolvesCanonicalRedirectsAndRejectsUnsafeDestinations()
    {
        RouteResolverCore core = new RouteResolverCore();
        UiRoute legacy = Route("legacy", "/legacy/{id:int}", null);
        legacy.RedirectUrl = "new-home?from=legacy";
        UiRoute unsafeRoute = Route("unsafe", "/unsafe", null);
        unsafeRoute.RedirectUrl = "javascript:alert(1)";

        RouteResolutionResult redirected = core.ResolveRouteTree(new UiRoute[] { legacy }, null, null, "/app/legacy/9", "/app/");
        RouteResolutionResult unsafeResult = core.ResolveRouteTree(new UiRoute[] { unsafeRoute }, null, null, "/unsafe", "/");

        Assert.Multiple(() =>
        {
            Assert.That(redirected.Success, Is.True);
            Assert.That(redirected.Status, Is.EqualTo(UiNavigationStatus.Redirected));
            Assert.That(redirected.RedirectUrl, Is.EqualTo("/app/new-home?from=legacy"));
            Assert.That(redirected.RouteData!.Parameters.Single().Value.IntegerValue, Is.EqualTo(9));
            Assert.That(redirected.View, Is.Null);
            Assert.That(unsafeResult.Success, Is.False);
            Assert.That(unsafeResult.Status, Is.EqualTo(UiNavigationStatus.Failed));
            Assert.That(unsafeResult.Code, Is.EqualTo("route.invalid-redirect"));
        });
    }

    [Test]
    public void SelectsNearestRoutePageAndAppliesDocumentMetadataInheritance()
    {
        RouteResolverCore core = new RouteResolverCore();
        UiDocumentMetadata document = new UiDocumentMetadata
        {
            Title = "Application",
            Language = "en-US",
            Description = "Application description",
            ApplicationName = "Example",
            Values = new UiValueEntry[] { Entry("theme", "window"), Entry("region", "west") }
        };
        UiRoute root = Route("root", "/account", LayoutView("layout", "outlet"));
        UiRoute index = Route("index", "/", ContentView("index", "Index"));
        index.ParentId = root.Id;
        index.IsIndex = true;
        UiRoute settings = Route("settings", "/settings", ContentView("settings", "Settings"));
        settings.ParentId = root.Id;
        UiPage rootPage = new UiPage { Key = "root-page", RouteId = "root", Title = "Account" };
        UiPage settingsPage = new UiPage
        {
            Key = "settings-page",
            RouteId = "settings",
            Metadata = new UiDocumentMetadata
            {
                Title = "Settings metadata",
                Description = "Settings description",
                Values = new UiValueEntry[] { Entry("theme", "page") }
            }
        };
        UiPage[] pages = new UiPage[] { rootPage, settingsPage };
        UiRoute[] routes = new UiRoute[] { settings, index, root };

        RouteResolutionResult inheritedPage = core.ResolveRouteTree(routes, pages, document, "/account", "/");
        RouteResolutionResult exactPage = core.ResolveRouteTree(routes, pages, document, "/account/settings", "/");

        Assert.Multiple(() =>
        {
            Assert.That(inheritedPage.Page!.Key, Is.EqualTo("root-page"));
            Assert.That(inheritedPage.Title, Is.EqualTo("Account"));
            Assert.That(inheritedPage.Metadata!.Language, Is.EqualTo("en-US"));
            Assert.That(inheritedPage.Metadata.Description, Is.EqualTo("Application description"));
            Assert.That(exactPage.Page!.Key, Is.EqualTo("settings-page"));
            Assert.That(exactPage.Title, Is.EqualTo("Settings metadata"));
            Assert.That(exactPage.Metadata!.Description, Is.EqualTo("Settings description"));
            Assert.That(exactPage.Metadata.ApplicationName, Is.EqualTo("Example"));
            Assert.That(exactPage.Metadata.Values.Select(item => item.Key), Is.EqualTo(new string[] { "theme", "region" }));
            Assert.That(exactPage.Metadata.Values.Single(item => item.Key == "theme").Value.StringValue, Is.EqualTo("page"));
            Assert.That(settingsPage.Metadata.ApplicationName, Is.Null);
            Assert.That(settingsPage.Metadata.Values, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void ReportsMissingAndDuplicateNamedOutlets()
    {
        RouteResolverCore core = new RouteResolverCore();
        UiRoute missingLayout = Route("missing-layout", "/missing", LayoutView("missing", "other"));
        UiRoute missingChild = Route("missing-child", "/child", ContentView("child", "Child"));
        missingChild.ParentId = missingLayout.Id;
        UiRoute duplicateLayout = Route("duplicate-layout", "/duplicate", DuplicateOutletView());
        UiRoute duplicateChild = Route("duplicate-child", "/child", ContentView("child", "Child"));
        duplicateChild.ParentId = duplicateLayout.Id;

        RouteResolutionResult missing = core.ResolveRouteTree(new UiRoute[] { missingLayout, missingChild }, null, null, "/missing/child", "/");
        RouteResolutionResult duplicate = core.ResolveRouteTree(new UiRoute[] { duplicateLayout, duplicateChild }, null, null, "/duplicate/child", "/");

        Assert.Multiple(() =>
        {
            Assert.That(missing.Code, Is.EqualTo("route.outlet-not-found"));
            Assert.That(duplicate.Code, Is.EqualTo("route.duplicate-outlet"));
        });
    }

    [Test]
    public void ValidatesNestedRouteRelationshipsKindsAndDestinations()
    {
        RouteResolverCore core = new RouteResolverCore();
        UiRoute unknown = Route("child", "/child", null);
        unknown.ParentId = "missing";
        UiRoute firstCycle = Route("first", "/first", null);
        UiRoute secondCycle = Route("second", "/second", null);
        firstCycle.ParentId = secondCycle.Id;
        secondCycle.ParentId = firstCycle.Id;
        UiRoute badIndex = Route("index", "/child", null);
        badIndex.ParentId = "parent";
        badIndex.IsIndex = true;
        UiRoute parent = Route("parent", "/parent", null);
        UiRoute badNotFound = Route("missing", "/child", null);
        badNotFound.IsNotFound = true;
        UiRoute duplicateParameter = Route("nested", "/{id}", null);
        duplicateParameter.ParentId = parent.Id;
        parent.Template = "/{id}";
        UiRoute multipleDestinations = Route("multiple", "/multiple", ContentView("content", "Content"));
        multipleDestinations.RedirectUrl = "/elsewhere";

        Assert.Multiple(() =>
        {
            Assert.That(core.PrepareRouteDefinitions(new UiRoute[] { unknown }).Code, Is.EqualTo("route.unknown-parent"));
            Assert.That(core.PrepareRouteDefinitions(new UiRoute[] { firstCycle, secondCycle }).Code, Is.EqualTo("route.parent-cycle"));
            Assert.That(core.PrepareRouteDefinitions(new UiRoute[] { parent, badIndex }).Code, Is.EqualTo("route.index-has-template"));
            Assert.That(core.PrepareRouteDefinitions(new UiRoute[] { badNotFound }).Code, Is.EqualTo("route.not-found-has-template"));
            Assert.That(core.PrepareRouteDefinitions(new UiRoute[] { parent, duplicateParameter }).Code, Is.EqualTo("route.duplicate-parameter"));
            Assert.That(core.PrepareRouteDefinitions(new UiRoute[] { multipleDestinations }).Code, Is.EqualTo("route.multiple-destinations"));
        });
    }

    [Test]
    public void ReturnsNotFoundWithoutADestinationAndRecognizesExternalUrls()
    {
        RouteResolverCore core = new RouteResolverCore();
        UiDocumentMetadata document = new UiDocumentMetadata { Title = "Application" };
        RouteResolutionResult missing = core.ResolveRouteTree(System.Array.Empty<UiRoute>(), null, document, "/missing", "/");
        RouteResolutionResult external = core.ResolveRouteTree(System.Array.Empty<UiRoute>(), null, document, "https://example.test/page", "/");

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.True);
            Assert.That(missing.Status, Is.EqualTo(UiNavigationStatus.NotFound));
            Assert.That(missing.RouteData, Is.Null);
            Assert.That(missing.Title, Is.EqualTo("Application"));
            Assert.That(external.Success, Is.True);
            Assert.That(external.Status, Is.EqualTo(UiNavigationStatus.External));
            Assert.That(external.CanonicalUrl, Is.EqualTo("https://example.test/page"));
        });
    }

    private static UiRoute Route(string id, string template, UiView? view)
    {
        return new UiRoute { Id = id, Template = template, View = view };
    }

    private static UiView LayoutView(string key, string outletName)
    {
        UiNode root = new UiNode { Kind = UiNodeKind.Fragment, Name = key + "-root" };
        root.Children = new UiNode[]
        {
            new UiNode { Kind = UiNodeKind.Text, Text = key + " shell" },
            new UiNode { Kind = UiNodeKind.Slot, Name = outletName }
        };
        return new UiView { Key = key, ContentRoot = root };
    }

    private static UiView DuplicateOutletView()
    {
        UiNode root = new UiNode { Kind = UiNodeKind.Fragment };
        root.Children = new UiNode[]
        {
            new UiNode { Kind = UiNodeKind.Slot, Name = RouteResolverCore.DefaultRouteOutletName },
            new UiNode { Kind = UiNodeKind.Slot, Name = RouteResolverCore.DefaultRouteOutletName }
        };
        return new UiView { Key = "duplicate", ContentRoot = root };
    }

    private static UiView ContentView(string key, string text)
    {
        return new UiView { Key = key, ContentRoot = new UiNode { Kind = UiNodeKind.Text, Text = text } };
    }

    private static UiValueEntry Entry(string key, string value)
    {
        return new UiValueEntry { Key = key, Value = new UiValue { Kind = UiValueKind.String, StringValue = value } };
    }
}
