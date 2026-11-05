using System;
using System.Collections.Generic;

namespace htmlgui;

internal class NavigationRegistrationResult
{
    internal bool Success;
    internal bool Applied;
    internal string Code = string.Empty;
}

internal class NavigationGuardInvocation
{
    internal string HandlerId = string.Empty;
    internal int Generation;
    internal UiNavigationRequest Request = new UiNavigationRequest();
}

internal class NavigationObserverInvocation
{
    internal string HandlerId = string.Empty;
    internal string Stage = string.Empty;
    internal int Generation;
    internal string Code = string.Empty;
    internal UiNavigationRequest Request = new UiNavigationRequest();
    internal UiNavigationResult Navigation = new UiNavigationResult();
}

internal class NavigationFlowState
{
    internal bool Active;
    internal int Generation;
    internal int GuardIndex;
    internal string[] GuardIds = Array.Empty<string>();
    internal string[] ObserverIds = Array.Empty<string>();
    internal UiNavigationRequest Request = new UiNavigationRequest();
}

internal class NavigationFlowResult
{
    internal bool Success;
    internal bool Applied;
    internal bool IsPending;
    internal bool IsStale;
    internal string Code = string.Empty;
    internal int Generation;
    internal NavigationCoreResult? Navigation;
}

internal class PublicNavigationDispatchState
{
    internal NavigationFlowResult Flow = new NavigationFlowResult();
    internal List<UiDiagnostic> Diagnostics = new List<UiDiagnostic>();
    internal bool ObserverFailed;
}

internal class NavigationFlowCore
{
    internal const string BeforeStage = "before";
    internal const string PendingStage = "pending";
    internal const string AfterStage = "after";
    internal const string CancelStage = "cancel";
    internal const string ErrorStage = "error";

    internal NavigationCore Navigator = new NavigationCore();
    internal List<string> GuardIds = new List<string>();
    internal List<string> ObserverIds = new List<string>();
    internal List<NavigationGuardInvocation> PendingGuardInvocations = new List<NavigationGuardInvocation>();
    internal List<NavigationObserverInvocation> PendingObserverInvocations = new List<NavigationObserverInvocation>();
    internal NavigationFlowState Current = new NavigationFlowState();
    internal int Generation;

    internal NavigationRegistrationResult RegisterNavigationGuard(string? handlerId)
    {
        return RegisterNavigationHandler(GuardIds, handlerId, "navigation.invalid-guard", "navigation.duplicate-guard");
    }

    internal NavigationRegistrationResult UnregisterNavigationGuard(string? handlerId)
    {
        return UnregisterNavigationHandler(GuardIds, handlerId, "navigation.invalid-guard", "navigation.unknown-guard");
    }

    internal NavigationRegistrationResult RegisterNavigationObserver(string? handlerId)
    {
        return RegisterNavigationHandler(ObserverIds, handlerId, "navigation.invalid-observer", "navigation.duplicate-observer");
    }

    internal NavigationRegistrationResult UnregisterNavigationObserver(string? handlerId)
    {
        return UnregisterNavigationHandler(ObserverIds, handlerId, "navigation.invalid-observer", "navigation.unknown-observer");
    }

    internal NavigationFlowResult BeginNavigationFlow(UiNavigationRequest? request)
    {
        if (request == null)
        {
            return CreateNavigationFlowFailure("navigation.null-request", 0);
        }

        if (Generation == int.MaxValue)
        {
            return CreateNavigationFlowFailure("navigation.generation-exhausted", Generation);
        }

        if (Current.Active)
        {
            SupersedeCurrentNavigation();
        }

        Generation++;
        Current = new NavigationFlowState();
        Current.Active = true;
        Current.Generation = Generation;
        Current.GuardIds = GuardIds.ToArray();
        Current.ObserverIds = ObserverIds.ToArray();
        Current.Request = CopyNavigationRequest(request);
        Current.Request.Generation = Generation;
        QueueNavigationObservers(BeforeStage, Current, CreatePendingNavigationResult(Generation), string.Empty);
        QueueNavigationObservers(PendingStage, Current, CreatePendingNavigationResult(Generation), string.Empty);
        if (Current.GuardIds.Length == 0)
        {
            return ExecuteCurrentNavigation();
        }

        QueueCurrentNavigationGuard();
        return CreateNavigationFlowSuccess(true, false, Generation, null);
    }

    internal NavigationFlowResult CompleteNavigationGuard(string? handlerId, int generation, bool handlerSucceeded, bool allowNavigation, string? errorCode)
    {
        if (!Current.Active || generation != Current.Generation)
        {
            return CreateNavigationFlowStale(generation);
        }

        if (Current.GuardIndex < 0 || Current.GuardIndex >= Current.GuardIds.Length || !ValueComparer.TextEquals(Current.GuardIds[Current.GuardIndex], handlerId))
        {
            return CreateNavigationFlowFailure("navigation.unexpected-guard", generation);
        }

        RemoveNavigationGuardInvocation(handlerId, generation);
        if (!handlerSucceeded)
        {
            string code = string.IsNullOrWhiteSpace(errorCode) ? "navigation.guard-error" : errorCode;
            NavigationCoreResult failed = Navigator.CreateNavigationFailure(code, generation);
            return FinishCurrentNavigation(failed, ErrorStage, code);
        }

        if (!allowNavigation)
        {
            NavigationCoreResult cancelled = Navigator.SnapshotNavigationResult(Navigator.CreateNavigationCancellation("navigation.guard-cancelled", generation));
            return FinishCurrentNavigation(cancelled, CancelStage, "navigation.guard-cancelled");
        }

        Current.GuardIndex++;
        if (Current.GuardIndex < Current.GuardIds.Length)
        {
            QueueCurrentNavigationGuard();
            return CreateNavigationFlowSuccess(true, true, generation, null);
        }

        return ExecuteCurrentNavigation();
    }

    internal NavigationFlowResult CancelNavigationFlow(int generation)
    {
        if (!Current.Active || generation != Current.Generation)
        {
            return CreateNavigationFlowStale(generation);
        }

        RemoveNavigationGuardGenerationInvocations(generation);
        NavigationCoreResult cancelled = Navigator.SnapshotNavigationResult(Navigator.CreateNavigationCancellation("navigation.cancelled", generation));
        return FinishCurrentNavigation(cancelled, CancelStage, "navigation.cancelled");
    }

    internal NavigationGuardInvocation[] DrainNavigationGuardInvocations()
    {
        NavigationGuardInvocation[] result = new NavigationGuardInvocation[PendingGuardInvocations.Count];
        int index = 0;
        while (index < PendingGuardInvocations.Count)
        {
            result[index] = CopyNavigationGuardInvocation(PendingGuardInvocations[index]);
            index++;
        }

        PendingGuardInvocations.Clear();
        return result;
    }

    internal NavigationObserverInvocation[] DrainNavigationObserverInvocations()
    {
        NavigationObserverInvocation[] result = new NavigationObserverInvocation[PendingObserverInvocations.Count];
        int index = 0;
        while (index < PendingObserverInvocations.Count)
        {
            result[index] = CopyNavigationObserverInvocation(PendingObserverInvocations[index]);
            index++;
        }

        PendingObserverInvocations.Clear();
        return result;
    }

    internal NavigationFlowResult ExecuteCurrentNavigation()
    {
        NavigationCoreResult navigation = Navigator.ExecuteNavigation(Current.Request);
        if (!navigation.Success || navigation.Navigation.Status == UiNavigationStatus.Failed)
        {
            string code = navigation.Code.Length == 0 ? "navigation.failed" : navigation.Code;
            return FinishCurrentNavigation(navigation, ErrorStage, code);
        }

        if (navigation.Navigation.Status == UiNavigationStatus.Cancelled)
        {
            string code = navigation.Code.Length == 0 ? "navigation.cancelled" : navigation.Code;
            return FinishCurrentNavigation(navigation, CancelStage, code);
        }

        return FinishCurrentNavigation(navigation, AfterStage, string.Empty);
    }

    internal NavigationFlowResult FinishCurrentNavigation(NavigationCoreResult navigation, string stage, string code)
    {
        NavigationFlowState completed = Current;
        Current.Active = false;
        RemoveNavigationGuardGenerationInvocations(completed.Generation);
        QueueNavigationObservers(stage, completed, navigation.Navigation, code);
        NavigationFlowResult result = CreateNavigationFlowSuccess(false, true, completed.Generation, navigation);
        result.Code = code;
        result.Success = navigation.Success;
        return result;
    }

    internal void SupersedeCurrentNavigation()
    {
        NavigationFlowState superseded = Current;
        RemoveNavigationGuardGenerationInvocations(superseded.Generation);
        UiNavigationResult cancelled = new UiNavigationResult();
        cancelled.Status = UiNavigationStatus.Cancelled;
        cancelled.Generation = superseded.Generation;
        QueueNavigationObservers(CancelStage, superseded, cancelled, "navigation.superseded");
        Current.Active = false;
    }

    internal void QueueCurrentNavigationGuard()
    {
        NavigationGuardInvocation invocation = new NavigationGuardInvocation();
        invocation.HandlerId = Current.GuardIds[Current.GuardIndex];
        invocation.Generation = Current.Generation;
        invocation.Request = CopyNavigationRequest(Current.Request);
        PendingGuardInvocations.Add(invocation);
    }

    internal void QueueNavigationObservers(string stage, NavigationFlowState flow, UiNavigationResult navigation, string code)
    {
        int index = 0;
        while (index < flow.ObserverIds.Length)
        {
            NavigationObserverInvocation invocation = new NavigationObserverInvocation();
            invocation.HandlerId = flow.ObserverIds[index];
            invocation.Stage = stage;
            invocation.Generation = flow.Generation;
            invocation.Code = code;
            invocation.Request = CopyNavigationRequest(flow.Request);
            invocation.Navigation = CopyNavigationResult(navigation);
            PendingObserverInvocations.Add(invocation);
            index++;
        }
    }

    internal void RemoveNavigationGuardGenerationInvocations(int generation)
    {
        int guardIndex = PendingGuardInvocations.Count - 1;
        while (guardIndex >= 0)
        {
            if (PendingGuardInvocations[guardIndex].Generation == generation)
            {
                PendingGuardInvocations.RemoveAt(guardIndex);
            }

            guardIndex--;
        }
    }

    internal void RemoveNavigationGuardInvocation(string? handlerId, int generation)
    {
        int index = PendingGuardInvocations.Count - 1;
        while (index >= 0)
        {
            NavigationGuardInvocation invocation = PendingGuardInvocations[index];
            if (invocation.Generation == generation && ValueComparer.TextEquals(invocation.HandlerId, handlerId))
            {
                PendingGuardInvocations.RemoveAt(index);
            }

            index--;
        }
    }

    internal NavigationRegistrationResult RegisterNavigationHandler(List<string> handlers, string? handlerId, string invalidCode, string duplicateCode)
    {
        if (string.IsNullOrWhiteSpace(handlerId))
        {
            return CreateNavigationRegistrationFailure(invalidCode);
        }

        if (FindNavigationHandlerIndex(handlers, handlerId) >= 0)
        {
            return CreateNavigationRegistrationFailure(duplicateCode);
        }

        handlers.Add(handlerId);
        return CreateNavigationRegistrationSuccess();
    }

    internal NavigationRegistrationResult UnregisterNavigationHandler(List<string> handlers, string? handlerId, string invalidCode, string unknownCode)
    {
        if (string.IsNullOrWhiteSpace(handlerId))
        {
            return CreateNavigationRegistrationFailure(invalidCode);
        }

        int index = FindNavigationHandlerIndex(handlers, handlerId);
        if (index < 0)
        {
            return CreateNavigationRegistrationFailure(unknownCode);
        }

        handlers.RemoveAt(index);
        return CreateNavigationRegistrationSuccess();
    }

    internal int FindNavigationHandlerIndex(List<string> handlers, string? handlerId)
    {
        int index = 0;
        while (index < handlers.Count)
        {
            if (ValueComparer.TextEquals(handlers[index], handlerId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal NavigationRegistrationResult CreateNavigationRegistrationSuccess()
    {
        NavigationRegistrationResult result = new NavigationRegistrationResult();
        result.Success = true;
        result.Applied = true;
        return result;
    }

    internal NavigationRegistrationResult CreateNavigationRegistrationFailure(string code)
    {
        NavigationRegistrationResult result = new NavigationRegistrationResult();
        result.Code = code;
        return result;
    }

    internal NavigationFlowResult CreateNavigationFlowSuccess(bool pending, bool applied, int generation, NavigationCoreResult? navigation)
    {
        NavigationFlowResult result = new NavigationFlowResult();
        result.Success = true;
        result.Applied = applied;
        result.IsPending = pending;
        result.Generation = generation;
        result.Navigation = navigation;
        return result;
    }

    internal NavigationFlowResult CreateNavigationFlowFailure(string code, int generation)
    {
        NavigationFlowResult result = new NavigationFlowResult();
        result.Code = code;
        result.Generation = generation;
        return result;
    }

    internal NavigationFlowResult CreateNavigationFlowStale(int generation)
    {
        NavigationFlowResult result = new NavigationFlowResult();
        result.Success = true;
        result.IsStale = true;
        result.Code = "navigation.stale-generation";
        result.Generation = generation;
        return result;
    }

    internal UiNavigationResult CreatePendingNavigationResult(int generation)
    {
        UiNavigationResult result = new UiNavigationResult();
        result.Status = UiNavigationStatus.Pending;
        result.Generation = generation;
        return result;
    }

    internal NavigationGuardInvocation CopyNavigationGuardInvocation(NavigationGuardInvocation source)
    {
        NavigationGuardInvocation result = new NavigationGuardInvocation();
        result.HandlerId = source.HandlerId;
        result.Generation = source.Generation;
        result.Request = CopyNavigationRequest(source.Request);
        return result;
    }

    internal NavigationObserverInvocation CopyNavigationObserverInvocation(NavigationObserverInvocation source)
    {
        NavigationObserverInvocation result = new NavigationObserverInvocation();
        result.HandlerId = source.HandlerId;
        result.Stage = source.Stage;
        result.Generation = source.Generation;
        result.Code = source.Code;
        result.Request = CopyNavigationRequest(source.Request);
        result.Navigation = CopyNavigationResult(source.Navigation);
        return result;
    }

    internal UiNavigationRequest CopyNavigationRequest(UiNavigationRequest source)
    {
        object? copy = new ModelNormalizer().CopyValue(source);
        return copy == null ? new UiNavigationRequest() : (UiNavigationRequest)copy;
    }

    internal UiNavigationResult CopyNavigationResult(UiNavigationResult source)
    {
        object? copy = new ModelNormalizer().CopyValue(source);
        return copy == null ? new UiNavigationResult() : (UiNavigationResult)copy;
    }
}
