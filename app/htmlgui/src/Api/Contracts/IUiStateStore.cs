namespace htmlgui;

/// <summary>Stores scoped observable state with deterministic transactions and callbacks.</summary>
public interface IUiStateStore
{
    /// <summary>Creates one state entry.</summary>
    /// <param name="scopeId">The optional state scope.</param>
    /// <param name="stateId">The stable state identifier.</param>
    /// <param name="value">The initial portable value.</param>
    /// <param name="origin">The optional mutation origin.</param>
    /// <returns>The exceptionless state result.</returns>
    UiStateResult CreateState(string? scopeId, string? stateId, UiValue? value, string? origin);

    /// <summary>Reads one state entry.</summary>
    /// <param name="scopeId">The optional state scope.</param>
    /// <param name="stateId">The stable state identifier.</param>
    /// <returns>The exceptionless state result.</returns>
    UiStateResult ReadState(string? scopeId, string? stateId);

    /// <summary>Writes one state entry.</summary>
    /// <param name="scopeId">The optional state scope.</param>
    /// <param name="stateId">The stable state identifier.</param>
    /// <param name="value">The new portable value.</param>
    /// <param name="origin">The optional mutation origin.</param>
    /// <returns>The exceptionless state result.</returns>
    UiStateResult WriteState(string? scopeId, string? stateId, UiValue? value, string? origin);

    /// <summary>Removes one state entry.</summary>
    /// <param name="scopeId">The optional state scope.</param>
    /// <param name="stateId">The stable state identifier.</param>
    /// <param name="origin">The optional mutation origin.</param>
    /// <returns>The exceptionless state result.</returns>
    UiStateResult RemoveState(string? scopeId, string? stateId, string? origin);

    /// <summary>Writes one state entry only when its version matches.</summary>
    /// <param name="scopeId">The optional state scope.</param>
    /// <param name="stateId">The stable state identifier.</param>
    /// <param name="expectedVersion">The required current version.</param>
    /// <param name="value">The new portable value.</param>
    /// <param name="origin">The optional mutation origin.</param>
    /// <returns>The exceptionless state result.</returns>
    UiStateResult CompareAndSetState(string? scopeId, string? stateId, int expectedVersion, UiValue? value, string? origin);

    /// <summary>Applies bounded patch operations to one state entry.</summary>
    /// <param name="scopeId">The optional state scope.</param>
    /// <param name="stateId">The stable state identifier.</param>
    /// <param name="patches">The ordered patch operations.</param>
    /// <param name="origin">The optional mutation origin.</param>
    /// <returns>The exceptionless state result.</returns>
    UiStateResult PatchState(string? scopeId, string? stateId, UiStatePatch[]? patches, string? origin);

    /// <summary>Begins one atomic state batch.</summary>
    /// <param name="batchId">The stable batch identifier.</param>
    /// <returns>The exceptionless batch result.</returns>
    UiStateBatchResult BeginStateBatch(string? batchId);

    /// <summary>Commits the active state batch and delivers its observers.</summary>
    /// <returns>The exceptionless batch result.</returns>
    UiStateBatchResult CommitStateBatch();

    /// <summary>Rolls back the active state batch.</summary>
    /// <returns>The exceptionless batch result.</returns>
    UiStateBatchResult RollbackStateBatch();

    /// <summary>Creates one named state snapshot.</summary>
    /// <param name="snapshotId">The stable snapshot identifier.</param>
    /// <returns>The exceptionless snapshot result.</returns>
    UiStateSnapshotResult CreateStateSnapshot(string? snapshotId);

    /// <summary>Restores one named state snapshot.</summary>
    /// <param name="snapshotId">The stable snapshot identifier.</param>
    /// <param name="origin">The optional mutation origin.</param>
    /// <returns>The exceptionless snapshot result.</returns>
    UiStateSnapshotResult RestoreStateSnapshot(string? snapshotId, string? origin);

    /// <summary>Registers one derived-state definition.</summary>
    /// <param name="scopeId">The optional state scope.</param>
    /// <param name="derived">The derived-state definition.</param>
    /// <returns>The exceptionless derived-state result.</returns>
    UiDerivedStateResult RegisterDerivedState(string? scopeId, UiDerivedState? derived);

    /// <summary>Reads one derived-state definition.</summary>
    /// <param name="scopeId">The optional state scope.</param>
    /// <param name="derivedId">The stable derived-state identifier.</param>
    /// <returns>The exceptionless derived-state result.</returns>
    UiDerivedStateResult ReadDerivedState(string? scopeId, string? derivedId);

    /// <summary>Updates one derived state's explicit dependencies.</summary>
    /// <param name="scopeId">The optional state scope.</param>
    /// <param name="derivedId">The stable derived-state identifier.</param>
    /// <param name="dependencyIds">The dependency state identifiers.</param>
    /// <returns>The exceptionless derived-state result.</returns>
    UiDerivedStateResult UpdateDerivedStateDependencies(string? scopeId, string? derivedId, string[]? dependencyIds);

    /// <summary>Accepts a computed value for one dirty derived state.</summary>
    /// <param name="scopeId">The optional state scope.</param>
    /// <param name="derivedId">The stable derived-state identifier.</param>
    /// <param name="value">The computed portable value.</param>
    /// <returns>The exceptionless derived-state result.</returns>
    UiDerivedStateResult AcceptDerivedStateValue(string? scopeId, string? derivedId, UiValue? value);

    /// <summary>Registers one post-commit state effect.</summary>
    /// <param name="scopeId">The optional state scope.</param>
    /// <param name="effect">The effect definition.</param>
    /// <returns>The exceptionless effect result.</returns>
    UiStateEffectResult RegisterStateEffect(string? scopeId, UiEffect? effect);

    /// <summary>Reads one post-commit state effect.</summary>
    /// <param name="scopeId">The optional state scope.</param>
    /// <param name="effectId">The stable effect identifier.</param>
    /// <returns>The exceptionless effect result.</returns>
    UiStateEffectResult ReadStateEffect(string? scopeId, string? effectId);

    /// <summary>Cancels one post-commit state effect.</summary>
    /// <param name="scopeId">The optional state scope.</param>
    /// <param name="effectId">The stable effect identifier.</param>
    /// <returns>The exceptionless effect result.</returns>
    UiStateEffectResult CancelStateEffect(string? scopeId, string? effectId);

    /// <summary>Registers one explicit observer lifetime scope.</summary>
    /// <param name="lifetimeId">The stable lifetime identifier.</param>
    /// <returns>The exceptionless subscription result.</returns>
    UiStateSubscriptionResult RegisterStateSubscriptionLifetime(string? lifetimeId);

    /// <summary>Disposes an observer lifetime scope and its attached subscriptions.</summary>
    /// <param name="lifetimeId">The stable lifetime identifier.</param>
    /// <returns>The exceptionless subscription result.</returns>
    UiStateSubscriptionResult DisposeStateSubscriptionLifetime(string? lifetimeId);

    /// <summary>Registers one state subscription and its observer callback.</summary>
    /// <param name="stateScopeId">The optional state scope.</param>
    /// <param name="subscription">The subscription definition.</param>
    /// <param name="observer">The observer callback.</param>
    /// <returns>The exceptionless subscription result.</returns>
    UiStateSubscriptionResult RegisterStateSubscription(string? stateScopeId, UiSubscription? subscription, IUiStateObserver? observer);

    /// <summary>Removes one state subscription and its observer callback.</summary>
    /// <param name="stateScopeId">The optional state scope.</param>
    /// <param name="subscriptionId">The stable subscription identifier.</param>
    /// <returns>The exceptionless subscription result.</returns>
    UiStateSubscriptionResult UnsubscribeState(string? stateScopeId, string? subscriptionId);
}
