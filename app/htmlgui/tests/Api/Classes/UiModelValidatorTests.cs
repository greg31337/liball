using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class UiModelValidatorTests
{
    [Test]
    public void ValidModelSucceedsThroughBothPublicPaths()
    {
        UiApplication application = CreateValidApplication();
        IUiModelValidator validator = new UiModelValidator();

        UiDiagnostic[] diagnostics = validator.ValidateModel(application);
        UiResult result = validator.TryValidateModel(application);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Is.Empty);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Value, Is.Null);
        });
    }

    [Test]
    public void ExceptionlessPathReturnsOrderedLocationAwareDiagnostics()
    {
        UiApplication application = CreateValidApplication();
        UiNode root = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        IUiFactory factory = new UiFactory();
        UiNode first = factory.CreateElementNode(application.RootSeed, root.Identity.StableId, "duplicate", null, "div");
        UiNode second = factory.CreateElementNode(application.RootSeed, root.Identity.StableId, "other", null, "span");
        second.Identity.AuthorKey = "duplicate";
        second.Identity.StableId = first.Identity.StableId;
        root.Children = new UiNode[] { first, second };
        IUiModelValidator validator = new UiModelValidator();

        UiResult result = validator.TryValidateModel(application);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(2));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(UiDiagnosticSeverity.Error));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo("model.duplicate-key"));
            Assert.That(result.Diagnostics[0].Message, Does.Contain("already used"));
            Assert.That(result.Diagnostics[0].Location, Is.EqualTo("/application/windows/0/pages/0/contentView/contentRoot/children/1/identity/authorKey"));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo("model.duplicate-id"));
            Assert.That(result.Diagnostics[1].Location, Is.EqualTo("/application/windows/0/pages/0/contentView/contentRoot/children/1/identity/stableId"));
        });
    }

    [Test]
    public void ThrowingPathUsesFirstErrorAsModelExceptionContext()
    {
        UiApplication application = CreateValidApplication();
        UiNode root = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        IUiFactory factory = new UiFactory();
        UiNode child = factory.CreateTextNode(application.RootSeed, root.Identity.StableId, "child", "Child");
        child.Identity.ParentStableId = "wrong-parent";
        root.Children = new UiNode[] { child };
        IUiModelValidator validator = new UiModelValidator();

        UiModelException? exception = Assert.Throws<UiModelException>(() => validator.ValidateModel(application));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Code, Is.EqualTo("model.parent-mismatch"));
            Assert.That(exception.Location, Is.EqualTo("/application/windows/0/pages/0/contentView/contentRoot/children/0/identity/parentStableId"));
            Assert.That(exception.Message, Does.Contain("model code 'model.parent-mismatch'"));
            Assert.That(exception.Message, Does.Contain("/children/0/identity/parentStableId"));
        });
    }

    [Test]
    public void NullRootIsExceptionlessForTryAndThrowsModelExceptionForValidate()
    {
        IUiModelValidator validator = new UiModelValidator();

        UiResult result = validator.TryValidateModel(null);
        UiModelException? exception = Assert.Throws<UiModelException>(() => validator.ValidateModel(null!));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo("model.null-root"));
            Assert.That(result.Diagnostics[0].Location, Is.EqualTo("/"));
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Code, Is.EqualTo("model.null-root"));
            Assert.That(exception.Location, Is.EqualTo("/"));
        });
    }

    [Test]
    public void ExceptionlessPathHonorsPositiveDiagnosticLimitAndUsesSafeFallback()
    {
        UiApplication limitedApplication = CreateApplicationWithRepeatedRoot();
        limitedApplication.RenderOptions.MaximumDiagnostics = 2;
        UiApplication fallbackApplication = CreateApplicationWithRepeatedRoot();
        fallbackApplication.RenderOptions.MaximumDiagnostics = 0;
        IUiModelValidator validator = new UiModelValidator();

        UiResult limited = validator.TryValidateModel(limitedApplication);
        UiResult fallback = validator.TryValidateModel(fallbackApplication);

        Assert.Multiple(() =>
        {
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Diagnostics, Has.Length.EqualTo(2));
            Assert.That(fallback.Success, Is.False);
            Assert.That(fallback.Diagnostics.Length, Is.GreaterThan(2));
        });
    }

    [Test]
    public void ValidationDoesNotMutateInvalidInput()
    {
        UiApplication application = CreateValidApplication();
        UiWindow[] windows = application.Windows;
        UiPage[] pages = windows[0].Pages;
        UiNode root = pages[0].ContentView!.ContentRoot!;
        UiNode child = new UiNode
        {
            Identity = new UiNodeIdentity
            {
                AuthorKey = null!,
                SiblingIdentity = null!,
                StableId = "child-id",
                ParentStableId = root.Identity.StableId
            }
        };
        root.Children = new UiNode[] { child };
        UiNode[] children = root.Children;
        IUiModelValidator validator = new UiModelValidator();

        UiResult result = validator.TryValidateModel(application);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(Array.Exists(result.Diagnostics, diagnostic => diagnostic.Code == "model.missing-key"), Is.True);
            Assert.That(application.Windows, Is.SameAs(windows));
            Assert.That(application.Windows[0].Pages, Is.SameAs(pages));
            Assert.That(root.Children, Is.SameAs(children));
            Assert.That(root.Children[0], Is.SameAs(child));
            Assert.That(child.Identity.AuthorKey, Is.Null);
            Assert.That(child.Identity.SiblingIdentity, Is.Null);
        });
    }

    [Test]
    public void RejectsNestedFormsWithoutRejectingSiblingForms()
    {
        UiApplication application = CreateValidApplication();
        UiNode root = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        IUiFactory factory = new UiFactory();
        UiNode outer = factory.CreateElementNode(application.RootSeed, root.Identity.StableId, "outer", null, "form");
        outer.Form = new UiForm { Key = "outer" };
        UiNode nested = factory.CreateElementNode(application.RootSeed, outer.Identity.StableId, "nested", null, "form");
        nested.Form = new UiForm { Key = "nested" };
        outer.Children = new UiNode[] { nested };
        UiNode sibling = factory.CreateElementNode(application.RootSeed, root.Identity.StableId, "sibling", null, "form");
        sibling.Form = new UiForm { Key = "sibling" };
        root.Children = new UiNode[] { outer, sibling };
        IUiModelValidator validator = new UiModelValidator();

        UiResult result = validator.TryValidateModel(application);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Count(item => item.Code == "model.nested-form"), Is.EqualTo(1));
            Assert.That(result.Diagnostics.Single(item => item.Code == "model.nested-form").Location, Is.EqualTo("/application/windows/0/pages/0/contentView/contentRoot/children/0/children/0/form"));
        });
    }

    [Test]
    public void RejectsUndefinedNodeKindsBeforeRendering()
    {
        UiApplication application = CreateValidApplication();
        application.Windows[0].Pages[0].ContentView!.ContentRoot!.Kind = (UiNodeKind)int.MaxValue;
        IUiModelValidator validator = new UiModelValidator();

        UiResult validation = validator.TryValidateModel(application);
        UiRenderResult rendering = new UiRenderer().RenderDocument(application);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.False);
            Assert.That(validation.Diagnostics.Single(item => item.Code == "model.invalid-node-kind").Location, Is.EqualTo("/application/windows/0/pages/0/contentView/contentRoot/kind"));
            Assert.That(rendering.Snapshot, Is.Null);
            Assert.That(rendering.Diagnostics.Select(item => item.Code), Does.Contain("model.invalid-node-kind"));
        });
    }

    private static UiApplication CreateApplicationWithRepeatedRoot()
    {
        UiApplication application = CreateValidApplication();
        UiNode root = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        root.Children = new UiNode[] { root, root, root };
        return application;
    }

    private static UiApplication CreateValidApplication()
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("validator-seed", "application");
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, "window");
        UiPage page = factory.CreatePage(application.RootSeed, window.Id, "page");
        UiView view = factory.CreateView(application.RootSeed, page.Id, "view");
        view.ContentRoot = factory.CreateElementNode(application.RootSeed, view.Id, "root", null, "main");
        page.ContentView = view;
        window.Pages = new UiPage[] { page };
        application.Windows = new UiWindow[] { window };
        return application;
    }
}
