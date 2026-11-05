using System.Threading.Tasks;

namespace htmlgui;

/// <summary>Receives copied component lifecycle notifications without public delegates.</summary>
public interface IUiLifecycleHandler
{
    /// <summary>Handles one ordered component lifecycle notification.</summary>
    /// <param name="notification">The copied lifecycle notification.</param>
    /// <returns>A task containing the exceptionless callback result.</returns>
    Task<UiLifecycleHandlerResult> HandleUiComponentLifecycle(UiLifecycleNotification notification);
}
