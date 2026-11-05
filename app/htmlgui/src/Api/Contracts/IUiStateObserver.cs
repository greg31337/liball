namespace htmlgui;

/// <summary>Receives deterministic post-commit state change callbacks.</summary>
public interface IUiStateObserver
{
    /// <summary>Observes one state change projected through a subscription path.</summary>
    /// <param name="context">The copied observer callback context.</param>
    /// <returns>The exceptionless callback result.</returns>
    UiResult ObserveStateChange(UiStateObserverContext context);
}
