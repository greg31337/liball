using System.Threading.Tasks;

namespace htmlgui;

/// <summary>Provides copied logical item ranges without exposing public delegates.</summary>
public interface IUiRangeProvider
{
    /// <summary>Provides one requested logical item range.</summary>
    /// <param name="request">The copied range and query context.</param>
    /// <returns>A task containing the provider result.</returns>
    Task<UiRangeResult> ProvideUiRange(UiRangeRequest request);
}
