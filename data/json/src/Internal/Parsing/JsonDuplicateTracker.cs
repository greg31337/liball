namespace json;

internal enum JsonDuplicateDecisionKind
{
    Append = 0,
    Skip = 1,
    Replace = 2
}

internal class JsonDuplicateDecision
{
    internal JsonDuplicateDecisionKind Kind;
    internal bool IsDuplicate;
    internal int MemberIndex;

    internal JsonDuplicateDecision(JsonDuplicateDecisionKind kind, bool isDuplicate, int memberIndex)
    {
        Kind = kind;
        IsDuplicate = isDuplicate;
        MemberIndex = memberIndex;
    }
}

internal class JsonDuplicateScope
{
    internal global::System.Collections.Generic.Dictionary<string, int> MemberIndexes;
    internal int RetainedMemberCount;

    internal JsonDuplicateScope()
    {
        MemberIndexes = new global::System.Collections.Generic.Dictionary<string, int>(global::System.StringComparer.Ordinal);
        RetainedMemberCount = 0;
    }
}

internal class JsonDuplicateTracker
{
    JsonDuplicateKeyPolicy policy;
    global::System.Collections.Generic.List<JsonDuplicateScope> scopes;

    internal JsonDuplicateTracker(JsonDuplicateKeyPolicy policy)
    {
        if (policy != JsonDuplicateKeyPolicy.Preserve &&
            policy != JsonDuplicateKeyPolicy.Reject &&
            policy != JsonDuplicateKeyPolicy.KeepFirst &&
            policy != JsonDuplicateKeyPolicy.KeepLast)
        {
            throw CreatePolicyFailure();
        }

        this.policy = policy;
        scopes = new global::System.Collections.Generic.List<JsonDuplicateScope>();
    }

    internal JsonDuplicateKeyPolicy GetPolicy()
    {
        return policy;
    }

    internal bool ContainsPropertyName(string decodedName)
    {
        if (decodedName == null)
        {
            throw new global::System.ArgumentNullException("decodedName");
        }

        if (scopes.Count == 0)
        {
            throw CreateStateFailure("There is no active object scope.");
        }

        return scopes[scopes.Count - 1].MemberIndexes.ContainsKey(decodedName);
    }

    internal int GetObjectDepth()
    {
        return scopes.Count;
    }

    internal int GetCurrentRetainedMemberCount()
    {
        if (scopes.Count == 0)
        {
            throw CreateStateFailure("There is no active object scope.");
        }

        return scopes[scopes.Count - 1].RetainedMemberCount;
    }

    internal void EnterObject()
    {
        scopes.Add(new JsonDuplicateScope());
    }

    internal int ExitObject()
    {
        if (scopes.Count == 0)
        {
            throw CreateStateFailure("There is no active object scope to exit.");
        }

        JsonDuplicateScope scope = scopes[scopes.Count - 1];
        scopes.RemoveAt(scopes.Count - 1);
        return scope.RetainedMemberCount;
    }

    internal JsonDuplicateDecision RegisterPropertyName(string decodedName)
    {
        if (decodedName == null)
        {
            throw new global::System.ArgumentNullException("decodedName");
        }

        if (scopes.Count == 0)
        {
            throw CreateStateFailure("A property name requires an active object scope.");
        }

        JsonDuplicateScope scope = scopes[scopes.Count - 1];
        if (!scope.MemberIndexes.ContainsKey(decodedName))
        {
            int memberIndex = scope.RetainedMemberCount;
            scope.MemberIndexes.Add(decodedName, memberIndex);
            scope.RetainedMemberCount++;
            return new JsonDuplicateDecision(JsonDuplicateDecisionKind.Append, false, memberIndex);
        }

        int existingMemberIndex = scope.MemberIndexes[decodedName];
        if (policy == JsonDuplicateKeyPolicy.Reject)
        {
            throw CreateDuplicateFailure(decodedName, scopes.Count);
        }

        if (policy == JsonDuplicateKeyPolicy.KeepFirst)
        {
            return new JsonDuplicateDecision(JsonDuplicateDecisionKind.Skip, true, existingMemberIndex);
        }

        if (policy == JsonDuplicateKeyPolicy.KeepLast)
        {
            return new JsonDuplicateDecision(JsonDuplicateDecisionKind.Replace, true, existingMemberIndex);
        }

        int appendedMemberIndex = scope.RetainedMemberCount;
        scope.RetainedMemberCount++;
        return new JsonDuplicateDecision(JsonDuplicateDecisionKind.Append, true, appendedMemberIndex);
    }

    internal void ResetDuplicates()
    {
        scopes.Clear();
    }

    static JsonParseException CreatePolicyFailure()
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.InvalidOptions;
        diagnostic.Message = "DuplicateKeyPolicy has an undefined value.";
        return new JsonParseException(diagnostic);
    }

    static JsonParseException CreateDuplicateFailure(string decodedName, int depth)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.DuplicatePropertyName;
        diagnostic.Message = "The object contains a duplicate decoded property name.";
        diagnostic.Depth = depth;
        diagnostic.ActualToken = decodedName;
        return new JsonParseException(diagnostic);
    }

    static global::System.InvalidOperationException CreateStateFailure(string message)
    {
        return new global::System.InvalidOperationException(message);
    }
}
