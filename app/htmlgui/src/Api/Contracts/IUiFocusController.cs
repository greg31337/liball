namespace htmlgui;

/// <summary>Registers focus scopes and applies deterministic focus and preservation operations.</summary>
public interface IUiFocusController
{
    /// <summary>Replaces the registered focus scopes and targets after exceptionless validation.</summary>
    /// <param name="scopes">The optional complete focus-scope collection.</param>
    /// <param name="targets">The optional complete focus-target collection.</param>
    /// <returns>The registration result.</returns>
    UiResult RegisterFocusScopes(UiFocusScope[]? scopes, UiFocusTarget[]? targets);

    /// <summary>Applies a focus, blur, move, restore, or active-descendant request.</summary>
    /// <param name="state">The optional current focus state.</param>
    /// <param name="request">The optional focus request.</param>
    /// <returns>The deterministic focus result.</returns>
    UiFocusResult ApplyFocusRequest(UiFocusState? state, UiFocusRequest? request);

    /// <summary>Moves focus in a logical direction within the active scope.</summary>
    /// <param name="state">The optional current focus state.</param>
    /// <param name="direction">The logical movement direction.</param>
    /// <param name="reveal">Whether the resulting target should be revealed.</param>
    /// <returns>The deterministic focus result.</returns>
    UiFocusResult MoveFocus(UiFocusState? state, UiFocusDirection direction, bool reveal);

    /// <summary>Restores focus to a scope's registered return target.</summary>
    /// <param name="state">The optional current focus state.</param>
    /// <param name="scopeId">The optional scope identifier, falling back to the state's active scope.</param>
    /// <param name="reveal">Whether the restored target should be revealed.</param>
    /// <returns>The deterministic focus result.</returns>
    UiFocusResult RestoreFocus(UiFocusState? state, string? scopeId, bool reveal);

    /// <summary>Enables or disables trapping for a registered focus scope.</summary>
    /// <param name="scopeId">The optional registered scope identifier.</param>
    /// <param name="enabled">Whether trapping is enabled.</param>
    /// <returns>The exceptionless trap-update result.</returns>
    UiResult SetFocusTrap(string? scopeId, bool enabled);

    /// <summary>Plans focus, selection, composition, scroll, and overlay preservation across a DOM patch.</summary>
    /// <param name="before">The snapshot before the patch.</param>
    /// <param name="after">The snapshot after the patch.</param>
    /// <param name="mutations">The ordered patch mutations.</param>
    /// <param name="current">The current browser preservation state.</param>
    /// <returns>The preservation plan.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when a mutation is malformed.</exception>
    UiPreservationState PlanFocusPreservation(DomSnapshot before, DomSnapshot after, DomMutation[] mutations, UiPreservationState current);
}
