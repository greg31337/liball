using System;

namespace htmlgui;

/// <summary>Creates hydration metadata and deterministic client/server hydration plans.</summary>
public class UiHydrator : IUiHydrator
{
    internal HydrationPlanner Planner;
    internal HydrationComparer Comparer;
    internal RendererPipeline Pipeline;

    /// <summary>Initializes a hydrator with deterministic manifest and comparison services.</summary>
    public UiHydrator()
    {
        Planner = new HydrationPlanner();
        Comparer = new HydrationComparer();
        Pipeline = new RendererPipeline();
    }

    /// <summary>Creates a detached hydration manifest for an application and server-parity snapshot.</summary>
    public UiHydrationManifest CreateHydrationManifest(UiApplication? application, DomSnapshot? snapshot)
    {
        if (application == null)
        {
            throw new ArgumentNullException("application");
        }

        if (snapshot == null)
        {
            throw new ArgumentNullException("snapshot");
        }

        RendererPreparation preparation = Pipeline.PrepareApplication(application);
        if (!preparation.Success || preparation.Application == null || preparation.Window == null || preparation.Page == null)
        {
            throw CreateHydrationPreparationException(application, preparation.Diagnostics);
        }

        string rootSeed = Guard.ArgumentNotWhiteSpace(snapshot.RootSeed, "snapshot");
        if (!string.Equals(preparation.Application.RootSeed, rootSeed, StringComparison.Ordinal))
        {
            throw new ArgumentException("The hydration snapshot root seed does not match the application root seed.", "snapshot");
        }

        if (snapshot.Roots == null)
        {
            throw new ArgumentException("The hydration snapshot roots are required.", "snapshot");
        }

        RenderTreeAnnotationResult tree = new RenderTreeAnnotator().AnnotateTree(snapshot.Roots, rootSeed, preparation.MaximumDiagnostics);
        if (!tree.Success)
        {
            UiDiagnostic diagnostic = tree.Diagnostics[0];
            throw new UiHydrationException(diagnostic.Message, ResolveHydrationNodeId(snapshot), UiHydrationMode.Exact, UiHydrationStatus.Failed);
        }

        return Planner.CreateHydrationManifest(preparation.Application, preparation.Window, preparation.Page, tree.Roots, tree.ContentHash);
    }

    /// <summary>Plans hydration by comparing an expected server snapshot with the existing client snapshot.</summary>
    public UiHydrationResult PlanHydration(DomSnapshot? expected, DomSnapshot? actual, UiHydrationManifest? manifest, UiHydrationMode mode)
    {
        return Comparer.CompareHydration(expected, actual, manifest, mode);
    }

    internal UiHydrationException CreateHydrationPreparationException(UiApplication application, UiDiagnostic[] diagnostics)
    {
        string message = "The application cannot produce a hydration manifest.";
        if (diagnostics.Length > 0 && !string.IsNullOrWhiteSpace(diagnostics[0].Message))
        {
            message = diagnostics[0].Message;
        }

        string nodeId = string.IsNullOrWhiteSpace(application.Id) ? "$application" : application.Id;
        return new UiHydrationException(message, nodeId, UiHydrationMode.Exact, UiHydrationStatus.Failed);
    }

    internal string ResolveHydrationNodeId(DomSnapshot snapshot)
    {
        DomNode[] roots = snapshot.Roots ?? Array.Empty<DomNode>();
        if (roots.Length > 0 && roots[0] != null && !string.IsNullOrWhiteSpace(roots[0].Id))
        {
            return roots[0].Id;
        }

        return "$root";
    }
}
