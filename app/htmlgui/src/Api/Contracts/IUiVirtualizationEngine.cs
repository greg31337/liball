using System.Threading.Tasks;

namespace htmlgui;

/// <summary>Calculates virtual ranges and coordinates asynchronous range providers.</summary>
public interface IUiVirtualizationEngine
{
    /// <summary>Calculates a fixed- or variable-extent viewport range.</summary>
    /// <param name="virtualization">The virtualization configuration.</param>
    /// <param name="scrollX">The horizontal scroll offset.</param>
    /// <param name="scrollY">The vertical scroll offset.</param>
    /// <param name="viewportWidth">The viewport width.</param>
    /// <param name="viewportHeight">The viewport height.</param>
    /// <returns>A copied configuration containing the calculated request and measurements.</returns>
    UiVirtualization CalculateUiViewport(UiVirtualization? virtualization, double scrollX, double scrollY, double viewportWidth, double viewportHeight);

    /// <summary>Applies one variable item measurement.</summary>
    /// <param name="virtualization">The variable-extent virtualization configuration.</param>
    /// <param name="measurement">The measured item.</param>
    /// <param name="anchorIndex">The logical anchor index used for correction.</param>
    /// <returns>A copied configuration containing the accepted measurement snapshot.</returns>
    UiVirtualization ApplyUiMeasurement(UiVirtualization? virtualization, UiMeasuredItem? measurement, int anchorIndex);

    /// <summary>Calculates a fixed- or variable-extent scroll target.</summary>
    /// <param name="virtualization">The virtualization configuration.</param>
    /// <param name="target">The requested logical target.</param>
    /// <param name="scrollX">The horizontal scroll offset.</param>
    /// <param name="scrollY">The vertical scroll offset.</param>
    /// <param name="viewportWidth">The viewport width.</param>
    /// <param name="viewportHeight">The viewport height.</param>
    /// <returns>The normalized calculated target.</returns>
    UiScrollTarget CalculateUiScrollTarget(UiVirtualization? virtualization, UiScrollTarget? target, double scrollX, double scrollY, double viewportWidth, double viewportHeight);

    /// <summary>Registers one stable asynchronous range provider.</summary>
    /// <param name="providerId">The stable provider identifier.</param>
    /// <param name="provider">The provider callback.</param>
    /// <returns>The exceptionless registration result.</returns>
    UiResult RegisterUiRangeProvider(string? providerId, IUiRangeProvider? provider);

    /// <summary>Unregisters one asynchronous range provider.</summary>
    /// <param name="providerId">The stable provider identifier.</param>
    /// <returns>The exceptionless unregistration result.</returns>
    UiResult UnregisterUiRangeProvider(string? providerId);

    /// <summary>Generates and dispatches one range-provider request.</summary>
    /// <param name="providerId">The registered provider identifier.</param>
    /// <param name="start">The zero-based logical start.</param>
    /// <param name="count">The requested item count.</param>
    /// <param name="sorts">The active sort context.</param>
    /// <param name="filters">The active filter context.</param>
    /// <param name="groups">The active group context.</param>
    /// <param name="parentKey">The optional parent tree key.</param>
    /// <returns>A task containing the accepted range, status rows, or diagnostic.</returns>
    Task<UiRangeResult> RequestUiRange(string? providerId, int start, int count, UiSort[]? sorts, UiFilter[]? filters, UiGroup[]? groups, string? parentKey);

    /// <summary>Retries the most recent failed range request.</summary>
    /// <param name="providerId">The registered provider identifier.</param>
    /// <returns>A task containing the accepted range, status rows, or diagnostic.</returns>
    Task<UiRangeResult> RetryUiRange(string? providerId);

    /// <summary>Cancels the active range generation.</summary>
    /// <param name="generation">The active request generation.</param>
    /// <returns>The exceptionless cancellation result.</returns>
    UiResult CancelUiRangeRequest(int generation);

    /// <summary>Reads a detached copy of the most recently accepted provider payload.</summary>
    /// <returns>The accepted provider payload, or null before successful acceptance.</returns>
    UiRangeResult? ReadUiRangeResult();
}
