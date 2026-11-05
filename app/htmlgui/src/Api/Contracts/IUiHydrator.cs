namespace htmlgui;

/// <summary>Creates deterministic hydration manifests and compares server and client DOM snapshots.</summary>
public interface IUiHydrator
{
    /// <summary>Creates a detached hydration manifest for an application and server-parity snapshot.</summary>
    /// <param name="application">The application whose runtime markers and state are captured.</param>
    /// <param name="snapshot">The server-parity DOM snapshot.</param>
    /// <returns>The deterministic hydration manifest.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the snapshot identity is invalid.</exception>
    /// <exception cref="UiHydrationException">Thrown when the application or server tree cannot produce a manifest.</exception>
    UiHydrationManifest CreateHydrationManifest(UiApplication? application, DomSnapshot? snapshot);

    /// <summary>Plans hydration by comparing an expected server snapshot with the existing client snapshot.</summary>
    /// <param name="expected">The expected server DOM snapshot.</param>
    /// <param name="actual">The existing client DOM snapshot.</param>
    /// <param name="manifest">The server hydration manifest.</param>
    /// <param name="mode">The requested mismatch strategy.</param>
    /// <returns>The exceptionless match, repair, replacement, or failure plan.</returns>
    /// <exception cref="System.ArgumentException">Thrown when a snapshot or manifest is malformed.</exception>
    UiHydrationResult PlanHydration(DomSnapshot? expected, DomSnapshot? actual, UiHydrationManifest? manifest, UiHydrationMode mode);
}
