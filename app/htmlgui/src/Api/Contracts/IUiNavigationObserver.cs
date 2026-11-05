using System.Threading.Tasks;

namespace htmlgui;

/// <summary>Receives copied navigation guard and lifecycle notifications.</summary>
public interface IUiNavigationObserver
{
    /// <summary>Handles one navigation guard or lifecycle notification.</summary>
    /// <param name="notification">The copied navigation notification.</param>
    /// <returns>A task containing the exceptionless callback outcome.</returns>
    Task<UiNavigationObserverResult> HandleUiNavigation(UiNavigationNotification notification);
}
