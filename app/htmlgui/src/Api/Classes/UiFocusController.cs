namespace htmlgui;

/// <summary>Provides registered-scope focus control and DOM patch preservation planning.</summary>
public class UiFocusController : IUiFocusController
{
    internal FocusEngine Engine;
    internal PreservationPlanner Planner;
    internal UiFocusScope[] Scopes;
    internal UiFocusTarget[] Targets;

    /// <summary>Initializes an empty focus controller.</summary>
    public UiFocusController()
    {
        Engine = new FocusEngine();
        Planner = new PreservationPlanner();
        Scopes = System.Array.Empty<UiFocusScope>();
        Targets = System.Array.Empty<UiFocusTarget>();
    }

    /// <summary>Replaces the registered focus scopes and targets after exceptionless validation.</summary>
    public UiResult RegisterFocusScopes(UiFocusScope[]? scopes, UiFocusTarget[]? targets)
    {
        UiResult result = Engine.ValidateFocusRegistration(scopes, targets);
        if (result.Success)
        {
            Scopes = Engine.CopyFocusScopes(scopes);
            Targets = Engine.CopyFocusTargets(targets);
        }

        return result;
    }

    /// <summary>Applies a focus, blur, move, restore, or active-descendant request.</summary>
    public UiFocusResult ApplyFocusRequest(UiFocusState? state, UiFocusRequest? request)
    {
        return Engine.ResolveFocus(Scopes, Targets, state, request);
    }

    /// <summary>Moves focus in a logical direction within the active scope.</summary>
    public UiFocusResult MoveFocus(UiFocusState? state, UiFocusDirection direction, bool reveal)
    {
        return Engine.ResolveFocus(Scopes, Targets, state, new UiFocusRequest
        {
            Operation = UiFocusOperation.Move,
            Direction = direction,
            Reveal = reveal
        });
    }

    /// <summary>Restores focus to a scope's registered return target.</summary>
    public UiFocusResult RestoreFocus(UiFocusState? state, string? scopeId, bool reveal)
    {
        return Engine.ResolveFocus(Scopes, Targets, state, new UiFocusRequest
        {
            Operation = UiFocusOperation.Restore,
            ScopeId = scopeId,
            Reveal = reveal
        });
    }

    /// <summary>Enables or disables trapping for a registered focus scope.</summary>
    public UiResult SetFocusTrap(string? scopeId, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(scopeId))
        {
            return CreateFocusControllerFailure("focus.invalid-scope", "A focus scope identifier is required.", "/focus/scopeId");
        }

        UiFocusScope? scope = Engine.FindFocusScope(Scopes, scopeId);
        if (scope == null)
        {
            return CreateFocusControllerFailure("focus.unknown-scope", "The focus scope is not registered.", "/focus/scopeId");
        }

        scope.TrapFocus = enabled;
        return new UiResult { Success = true };
    }

    /// <summary>Plans focus, selection, composition, scroll, and overlay preservation across a DOM patch.</summary>
    public UiPreservationState PlanFocusPreservation(DomSnapshot before, DomSnapshot after, DomMutation[] mutations, UiPreservationState current)
    {
        return Planner.CreatePreservationPlan(before, after, mutations, current);
    }

    internal UiResult CreateFocusControllerFailure(string code, string message, string location)
    {
        return new UiResult
        {
            Success = false,
            Diagnostics = new UiDiagnostic[]
            {
                new UiDiagnostic
                {
                    Severity = UiDiagnosticSeverity.Error,
                    Code = code,
                    Message = message,
                    Location = location
                }
            }
        };
    }
}
