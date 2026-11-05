namespace htmlgui;

/// <summary>Coordinates binding attachment and deterministic state-to-target propagation.</summary>
public interface IUiBindingEngine
{
    /// <summary>Gets the state store owned by this binding engine.</summary>
    /// <returns>The coordinated observable state store.</returns>
    IUiStateStore GetBindingStateStore();

    /// <summary>Registers one binding value converter.</summary>
    /// <param name="converterId">The stable converter identifier.</param>
    /// <param name="converter">The bidirectional converter callback.</param>
    /// <returns>The exceptionless registration result.</returns>
    UiResult RegisterBindingConverter(string? converterId, IUiValueConverter? converter);

    /// <summary>Attaches one binding and evaluates its initial target value.</summary>
    /// <param name="scopeId">The optional state and binding scope.</param>
    /// <param name="binding">The binding definition.</param>
    /// <returns>The lifecycle and initial propagation result.</returns>
    UiBindingResult AttachBinding(string? scopeId, UiBinding? binding);

    /// <summary>Detaches one binding.</summary>
    /// <param name="scopeId">The optional binding scope.</param>
    /// <param name="bindingId">The stable binding identifier.</param>
    /// <returns>The exceptionless lifecycle result.</returns>
    UiBindingResult DetachBinding(string? scopeId, string? bindingId);

    /// <summary>Evaluates one binding from state to target.</summary>
    /// <param name="scopeId">The optional binding scope.</param>
    /// <param name="bindingId">The stable binding identifier.</param>
    /// <returns>The deterministic target update result.</returns>
    UiBindingResult EvaluateBinding(string? scopeId, string? bindingId);

    /// <summary>Evaluates every target-directed binding in one scope.</summary>
    /// <param name="scopeId">The optional binding scope.</param>
    /// <returns>The deterministic target updates.</returns>
    UiBindingResult EvaluateStateBindings(string? scopeId);

    /// <summary>Pushes one triggered target value into state and propagates resulting target updates.</summary>
    /// <param name="scopeId">The optional binding scope.</param>
    /// <param name="bindingId">The stable binding identifier.</param>
    /// <param name="value">The copied target value.</param>
    /// <param name="origin">The target update origin.</param>
    /// <param name="trigger">The trigger that requested propagation.</param>
    /// <returns>The deterministic source and target propagation result.</returns>
    UiBindingResult PushBindingValue(string? scopeId, string? bindingId, UiValue? value, string? origin, UiBindingTrigger trigger);

    /// <summary>Processes one normalized DOM input, change, blur, or focus-out event.</summary>
    /// <param name="scopeId">The optional binding scope.</param>
    /// <param name="eventData">The normalized DOM event.</param>
    /// <param name="origin">The event propagation origin.</param>
    /// <returns>The atomically committed source and ordered target propagation result.</returns>
    UiBindingResult ProcessBindingEvent(string? scopeId, UiEventData? eventData, string? origin);
}
