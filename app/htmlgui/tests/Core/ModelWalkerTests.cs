using System;
using htmlgui;

namespace htmlgui.Tests;

public class ModelWalkerTests
{
    [Test]
    public void ValidHierarchyAndNestedNodesProduceNoDiagnostics()
    {
        UiApplication application = CreateValidApplication();
        UiNode root = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        IUiFactory factory = new UiFactory();
        UiNode child = factory.CreateTextNode(application.RootSeed, root.Identity.StableId, "message", "Hello");
        root.Children = new UiNode[] { child };
        ModelWalker walker = new ModelWalker(100);

        DiagnosticEntry[] diagnostics = walker.ValidateApplication(application);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void DetectsCyclesAndSharedStructuralReferences()
    {
        UiApplication cyclicApplication = CreateValidApplication();
        UiNode cyclicRoot = cyclicApplication.Windows[0].Pages[0].ContentView!.ContentRoot!;
        cyclicRoot.Children = new UiNode[] { cyclicRoot };
        ModelWalker cyclicWalker = new ModelWalker(100);

        DiagnosticEntry[] cycleDiagnostics = cyclicWalker.ValidateApplication(cyclicApplication);

        UiApplication sharedApplication = CreateValidApplication();
        UiNode sharedRoot = sharedApplication.Windows[0].Pages[0].ContentView!.ContentRoot!;
        IUiFactory factory = new UiFactory();
        UiNode sharedChild = factory.CreateElementNode(sharedApplication.RootSeed, sharedRoot.Identity.StableId, "shared", null, "span");
        sharedRoot.Children = new UiNode[] { sharedChild };
        sharedRoot.Layout = new UiLayout { Children = new UiNode[] { sharedChild } };
        ModelWalker sharedWalker = new ModelWalker(100);

        DiagnosticEntry[] sharedDiagnostics = sharedWalker.ValidateApplication(sharedApplication);

        Assert.Multiple(() =>
        {
            Assert.That(HasCode(cycleDiagnostics, "model.cycle"), Is.True);
            Assert.That(HasLocation(cycleDiagnostics, "/application/windows/0/pages/0/contentView/contentRoot/children/0"), Is.True);
            Assert.That(HasCode(sharedDiagnostics, "model.shared-reference"), Is.True);
            Assert.That(HasCode(sharedDiagnostics, "model.duplicate-id"), Is.True);
        });
    }

    [Test]
    public void DetectsDuplicateIdentitiesAndSiblingKeys()
    {
        UiApplication application = CreateValidApplication();
        UiNode root = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        IUiFactory factory = new UiFactory();
        UiNode first = factory.CreateElementNode(application.RootSeed, root.Identity.StableId, "duplicate", null, "div");
        UiNode second = factory.CreateElementNode(application.RootSeed, root.Identity.StableId, "other", null, "div");
        second.Identity.AuthorKey = "duplicate";
        second.Identity.StableId = first.Identity.StableId;
        root.Children = new UiNode[] { first, second };
        ModelWalker walker = new ModelWalker(100);

        DiagnosticEntry[] diagnostics = walker.ValidateApplication(application);

        Assert.Multiple(() =>
        {
            Assert.That(HasCode(diagnostics, "model.duplicate-id"), Is.True);
            Assert.That(HasCode(diagnostics, "model.duplicate-key"), Is.True);
            Assert.That(HasLocation(diagnostics, "/children/1/identity/stableId"), Is.True);
            Assert.That(HasLocation(diagnostics, "/children/1/identity/authorKey"), Is.True);
        });
    }

    [Test]
    public void DetectsIncorrectParentLinksAndIllegalLeafChildren()
    {
        UiApplication application = CreateValidApplication();
        UiNode root = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        IUiFactory factory = new UiFactory();
        UiNode leaf = factory.CreateTextNode(application.RootSeed, root.Identity.StableId, "leaf", "Leaf");
        UiNode illegalChild = factory.CreateElementNode(application.RootSeed, leaf.Identity.StableId, "illegal", null, "span");
        leaf.Children = new UiNode[] { illegalChild };
        leaf.Identity.ParentStableId = "wrong-parent";
        root.Children = new UiNode[] { leaf };
        root.Tree = new UiTree
        {
            Items = new UiTreeItem[]
            {
                new UiTreeItem
                {
                    Key = "parent",
                    Children = new UiTreeItem[]
                    {
                        new UiTreeItem { Key = "child", Relationship = new UiTreeRelationship { ParentKey = "wrong-item" } }
                    }
                }
            }
        };
        ModelWalker walker = new ModelWalker(100);

        DiagnosticEntry[] diagnostics = walker.ValidateApplication(application);

        Assert.Multiple(() =>
        {
            Assert.That(CountCode(diagnostics, "model.parent-mismatch"), Is.EqualTo(2));
            Assert.That(HasCode(diagnostics, "model.illegal-child"), Is.True);
            Assert.That(HasLocation(diagnostics, "/children/0/identity/parentStableId"), Is.True);
            Assert.That(HasLocation(diagnostics, "/tree/items/0/children/0/relationship/parentKey"), Is.True);
        });
    }

    [Test]
    public void WalksKeyedControlFamiliesAndReportsDuplicateKeysAtTheirLocations()
    {
        UiApplication application = CreateValidApplication();
        UiNode root = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        root.Collection = new UiCollection { Items = new UiCollectionItem[] { new UiCollectionItem { Key = "item" }, new UiCollectionItem { Key = "item" } } };
        root.Table = new UiTable { Columns = new UiTableColumn[] { new UiTableColumn { Key = "column" }, new UiTableColumn { Key = "column" } } };
        root.Tree = new UiTree
        {
            Items = new UiTreeItem[]
            {
                new UiTreeItem
                {
                    Key = "tree-item",
                    Children = new UiTreeItem[] { new UiTreeItem { Key = "tree-item", Relationship = new UiTreeRelationship { ParentKey = "tree-item" } } }
                }
            }
        };
        root.Menu = new UiMenu { Items = new UiMenuItem[] { new UiMenuItem { Key = "menu-item" }, new UiMenuItem { Key = "menu-item" } } };
        root.Tabs = new UiTabs { Tabs = new UiTab[] { new UiTab { Key = "tab" }, new UiTab { Key = "tab" } } };
        root.Toolbar = new UiToolbar { Tools = new UiTool[] { new UiTool { Key = "tool" }, new UiTool { Key = "tool" } } };
        root.Breadcrumb = new UiBreadcrumb { Items = new UiChoice[] { new UiChoice { Key = "crumb" }, new UiChoice { Key = "crumb" } } };
        root.Form = new UiForm
        {
            Fields = new UiField[]
            {
                new UiField
                {
                    Key = "field",
                    Name = "name",
                    Choices = new UiChoice[] { new UiChoice { Key = "choice" }, new UiChoice { Key = "choice" } },
                    Rules = new UiValidationRule[] { new UiValidationRule { Key = "rule" }, new UiValidationRule { Key = "rule" } }
                },
                new UiField { Key = "field", Name = "name" }
            }
        };
        ModelWalker walker = new ModelWalker(100);

        DiagnosticEntry[] diagnostics = walker.ValidateApplication(application);

        Assert.Multiple(() =>
        {
            Assert.That(CountCode(diagnostics, "model.duplicate-key"), Is.EqualTo(11));
            Assert.That(HasLocation(diagnostics, "/collection/items/1/key"), Is.True);
            Assert.That(HasLocation(diagnostics, "/table/columns/1/key"), Is.True);
            Assert.That(HasLocation(diagnostics, "/tree/items/0/children/0/key"), Is.True);
            Assert.That(HasLocation(diagnostics, "/menu/items/1/key"), Is.True);
            Assert.That(HasLocation(diagnostics, "/tabs/tabs/1/key"), Is.True);
            Assert.That(HasLocation(diagnostics, "/toolbar/tools/1/key"), Is.True);
            Assert.That(HasLocation(diagnostics, "/breadcrumb/items/1/key"), Is.True);
            Assert.That(HasLocation(diagnostics, "/form/fields/0/choices/1/key"), Is.True);
            Assert.That(HasLocation(diagnostics, "/form/fields/0/rules/1/key"), Is.True);
        });
    }

    [Test]
    public void ValidationDoesNotMutateTheInputGraphAndHonorsDiagnosticLimit()
    {
        UiApplication application = CreateValidApplication();
        UiWindow[] windows = application.Windows;
        UiPage[] pages = windows[0].Pages;
        UiView view = pages[0].ContentView!;
        UiNode root = view.ContentRoot!;
        UiNodeIdentity identity = root.Identity;
        root.Children = new UiNode[] { root, root, root };
        UiNode[] children = root.Children;
        string stableId = identity.StableId;
        ModelWalker walker = new ModelWalker(2);

        DiagnosticEntry[] diagnostics = walker.ValidateApplication(application);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Has.Length.EqualTo(2));
            Assert.That(walker.Diagnostics.WasTruncated(), Is.True);
            Assert.That(application.Windows, Is.SameAs(windows));
            Assert.That(windows[0].Pages, Is.SameAs(pages));
            Assert.That(pages[0].ContentView, Is.SameAs(view));
            Assert.That(view.ContentRoot, Is.SameAs(root));
            Assert.That(root.Identity, Is.SameAs(identity));
            Assert.That(root.Identity.StableId, Is.EqualTo(stableId));
            Assert.That(root.Children, Is.SameAs(children));
            Assert.That(root.Children[0], Is.SameAs(root));
        });
    }

    [Test]
    public void NullRootAndMalformedCollectionsProduceDiagnosticsInsteadOfThrowing()
    {
        ModelWalker walker = new ModelWalker(100);
        DiagnosticEntry[] nullRootDiagnostics = walker.ValidateApplication(null);
        UiApplication application = CreateValidApplication();
        application.Windows[0].Pages = null!;

        DiagnosticEntry[] malformedDiagnostics = walker.ValidateApplication(application);

        Assert.Multiple(() =>
        {
            Assert.That(HasCode(nullRootDiagnostics, "model.null-root"), Is.True);
            Assert.That(HasCode(malformedDiagnostics, "model.null-collection"), Is.True);
            Assert.That(HasLocation(malformedDiagnostics, "/application/windows/0/pages"), Is.True);
        });
    }

    [Test]
    public void MissingStructuralReferencesAreReportedAtTheirExactLocations()
    {
        UiApplication application = CreateValidApplication();
        UiWindow window = application.Windows[0];
        UiNode root = window.Pages[0].ContentView!.ContentRoot!;
        application.Id = string.Empty;
        window.Key = string.Empty;
        root.Identity.StableId = string.Empty;
        root.Children = new UiNode[] { null! };
        ModelWalker walker = new ModelWalker(100);

        DiagnosticEntry[] diagnostics = walker.ValidateApplication(application);

        Assert.Multiple(() =>
        {
            Assert.That(HasDiagnostic(diagnostics, "model.missing-id", "/application/id"), Is.True);
            Assert.That(HasDiagnostic(diagnostics, "model.missing-key", "/application/windows/0/key"), Is.True);
            Assert.That(HasDiagnostic(diagnostics, "model.missing-id", "/application/windows/0/pages/0/contentView/contentRoot/identity/stableId"), Is.True);
            Assert.That(HasDiagnostic(diagnostics, "model.null-child", "/application/windows/0/pages/0/contentView/contentRoot/children/0"), Is.True);
        });
    }

    [Test]
    public void RepeatedValidationProducesTheSameDiagnosticOrder()
    {
        UiApplication application = CreateValidApplication();
        UiNode root = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        IUiFactory factory = new UiFactory();
        UiNode first = factory.CreateTextNode(application.RootSeed, root.Identity.StableId, "duplicate", "First");
        UiNode second = factory.CreateTextNode(application.RootSeed, root.Identity.StableId, "second", "Second");
        second.Identity.AuthorKey = first.Identity.AuthorKey;
        second.Identity.StableId = first.Identity.StableId;
        second.Children = new UiNode[] { second };
        root.Children = new UiNode[] { first, second };
        ModelWalker walker = new ModelWalker(100);

        DiagnosticEntry[] firstPass = walker.ValidateApplication(application);
        DiagnosticEntry[] secondPass = walker.ValidateApplication(application);

        Assert.Multiple(() =>
        {
            Assert.That(Array.ConvertAll(secondPass, entry => entry.Code), Is.EqualTo(Array.ConvertAll(firstPass, entry => entry.Code)));
            Assert.That(Array.ConvertAll(secondPass, entry => entry.Location), Is.EqualTo(Array.ConvertAll(firstPass, entry => entry.Location)));
            Assert.That(Array.ConvertAll(secondPass, entry => entry.Message), Is.EqualTo(Array.ConvertAll(firstPass, entry => entry.Message)));
        });
    }

    private static UiApplication CreateValidApplication()
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("model-walker-seed", "application");
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, "window");
        UiPage page = factory.CreatePage(application.RootSeed, window.Id, "page");
        UiView view = factory.CreateView(application.RootSeed, page.Id, "view");
        UiNode root = factory.CreateElementNode(application.RootSeed, view.Id, "root", null, "main");
        view.ContentRoot = root;
        page.ContentView = view;
        window.Pages = new UiPage[] { page };
        application.Windows = new UiWindow[] { window };
        return application;
    }

    private static bool HasCode(DiagnosticEntry[] diagnostics, string code)
    {
        return Array.Exists(diagnostics, entry => entry.Code == code);
    }

    private static bool HasLocation(DiagnosticEntry[] diagnostics, string locationSuffix)
    {
        return Array.Exists(diagnostics, entry => entry.Location.EndsWith(locationSuffix, StringComparison.Ordinal));
    }

    private static bool HasDiagnostic(DiagnosticEntry[] diagnostics, string code, string location)
    {
        return Array.Exists(diagnostics, entry => entry.Code == code && entry.Location == location);
    }

    private static int CountCode(DiagnosticEntry[] diagnostics, string code)
    {
        int count = 0;
        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index].Code == code)
            {
                count++;
            }

            index++;
        }

        return count;
    }
}
