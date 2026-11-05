using System;
using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class RequestValidationResult
{
    internal bool IsValid;
    internal JmapProblemKind ProblemKind;
    internal string? Limit;
    internal string? Detail;
    internal List<string> InvalidPaths = new List<string>();
    internal Dictionary<int, JmapMethodError> MethodErrors = new Dictionary<int, JmapMethodError>();
}

internal class RequestValidator
{
    internal JmapServerOptions Options;
    internal HashSet<string> SupportedCapabilities = new HashSet<string>(StringComparer.Ordinal);
    internal Dictionary<string, List<string>> MethodCapabilities = new Dictionary<string, List<string>>(StringComparer.Ordinal);
    internal ValueValidator Values = new ValueValidator();
    internal ResultReferenceResolver References = new ResultReferenceResolver();

    internal RequestValidator(JmapServerOptions options, List<string> supportedCapabilities, Dictionary<string, List<string>> methodCapabilities)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (supportedCapabilities == null)
        {
            throw new ArgumentNullException("supportedCapabilities");
        }

        if (methodCapabilities == null)
        {
            throw new ArgumentNullException("methodCapabilities");
        }

        Options = options;
        foreach (string capability in supportedCapabilities)
        {
            SupportedCapabilities.Add(capability);
        }

        foreach (KeyValuePair<string, List<string>> item in methodCapabilities)
        {
            if (item.Value == null)
            {
                throw new ArgumentException("A method capability list cannot be null.", "methodCapabilities");
            }

            MethodCapabilities.Add(item.Key, new List<string>(item.Value));
        }
    }

    internal RequestValidationResult ValidateRequest(JmapRequest? request, long requestBytes)
    {
        if (requestBytes < 0L)
        {
            return RequestFailure(JmapProblemKind.NotRequest, null, "The request size is not valid.", "");
        }

        if (requestBytes > Options.MaxRequestBytes)
        {
            return RequestFailure(JmapProblemKind.Limit, JmapProperties.MaxSizeRequest, "The request exceeds the configured byte limit.", "");
        }

        if (request == null)
        {
            return RequestFailure(JmapProblemKind.NotRequest, null, "A JMAP Request object is required.", "");
        }

        if (request.Using == null)
        {
            return RequestFailure(JmapProblemKind.NotRequest, null, "The using property must be an array.", "using");
        }

        if (request.MethodCalls == null)
        {
            return RequestFailure(JmapProblemKind.NotRequest, null, "The methodCalls property must be an array.", "methodCalls");
        }

        if (request.MethodCalls.Count > Options.MaxCallsInRequest)
        {
            return RequestFailure(JmapProblemKind.Limit, JmapProperties.MaxCallsInRequest, "The request contains too many method calls.", "methodCalls");
        }

        RequestValidationResult capabilityResult = ValidateCapabilities(request.Using);
        if (!capabilityResult.IsValid)
        {
            return capabilityResult;
        }

        RequestValidationResult createdIdsResult = ValidateCreatedIds(request.CreatedIds);
        if (!createdIdsResult.IsValid)
        {
            return createdIdsResult;
        }

        RequestValidationResult result = new RequestValidationResult();
        result.IsValid = true;
        HashSet<string> callIds = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        foreach (JmapInvocation invocation in request.MethodCalls)
        {
            string invocationPath = "methodCalls/" + index;
            string? invalidPath = ValidateInvocation(invocation, invocationPath, callIds);
            if (invalidPath != null)
            {
                return RequestFailure(JmapProblemKind.NotRequest, null, "A method invocation is not valid.", invalidPath);
            }

            AddMissingCapabilityError(invocation, request.Using, index, result);
            if (!result.MethodErrors.ContainsKey(index))
            {
                AddSequentialReferenceError(invocation, callIds, index, result);
            }

            if (invocation.CallId != null)
            {
                callIds.Add(invocation.CallId);
            }

            index = index + 1;
        }

        return result;
    }

    internal RequestValidationResult ValidateCapabilities(List<string> capabilities)
    {
        int index = 0;
        foreach (string capability in capabilities)
        {
            string path = "using/" + index;
            if (!Values.IsCapability(capability))
            {
                return RequestFailure(JmapProblemKind.NotRequest, null, "The using property contains an invalid capability.", path);
            }

            if (!SupportedCapabilities.Contains(capability))
            {
                return RequestFailure(JmapProblemKind.UnknownCapability, null, "The request uses an unsupported capability: " + capability, path);
            }

            index = index + 1;
        }

        return ValidResult();
    }

    internal RequestValidationResult ValidateCreatedIds(Dictionary<string, string>? createdIds)
    {
        if (createdIds != null)
        {
            foreach (KeyValuePair<string, string> item in createdIds)
            {
                string path = "createdIds/" + item.Key;
                if (!Values.IsCreationId(item.Key) || !Values.IsId(item.Value))
                {
                    return RequestFailure(JmapProblemKind.NotRequest, null, "The createdIds property contains an invalid mapping.", path);
                }
            }
        }

        return ValidResult();
    }

    internal string? ValidateInvocation(JmapInvocation? invocation, string path, HashSet<string> callIds)
    {
        if (invocation == null)
        {
            return path;
        }

        if (!Values.IsMethodName(invocation.Name))
        {
            return path + "/0";
        }

        if (invocation.Arguments.ValueKind != JsonValueKind.Object || HasDuplicateArgumentMembers(invocation.Arguments))
        {
            return path + "/1";
        }

        if (HasExcessiveResultReferences(invocation.Arguments))
        {
            return path + "/1";
        }

        if (!IsUsableCallId(invocation.CallId) || invocation.CallId == null || callIds.Contains(invocation.CallId))
        {
            return path + "/2";
        }

        return null;
    }

    internal bool HasExcessiveResultReferences(JsonElement arguments)
    {
        int references = 0;
        foreach (JsonProperty property in arguments.EnumerateObject())
        {
            if (property.Name.Length != 0 && property.Name[0] == '#')
            {
                references = references + 1;
                if (references > SecurityLimits.MaximumResultReferences)
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal bool HasDuplicateArgumentMembers(JsonElement arguments)
    {
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonProperty property in arguments.EnumerateObject())
        {
            if (!Values.IsValidUnicode(property.Name) || !names.Add(property.Name))
            {
                return true;
            }
        }

        return false;
    }

    internal bool IsUsableCallId(string? callId)
    {
        return callId != null && callId.Length != 0 && Values.IsValidUnicode(callId);
    }

    internal void AddMissingCapabilityError(JmapInvocation invocation, List<string> enabledCapabilities, int index, RequestValidationResult result)
    {
        if (invocation.Name == null || !MethodCapabilities.ContainsKey(invocation.Name))
        {
            return;
        }

        foreach (string capability in MethodCapabilities[invocation.Name])
        {
            if (!enabledCapabilities.Contains(capability))
            {
                JmapMethodError error = new JmapMethodError();
                error.Type = JmapErrorCodes.UnknownMethod;
                error.Kind = JmapMethodErrorKind.UnknownMethod;
                error.Description = "The method requires a capability that was not enabled: " + capability;
                result.MethodErrors.Add(index, error);
                return;
            }
        }
    }

    internal void AddSequentialReferenceError(JmapInvocation invocation, HashSet<string> previousCallIds, int index, RequestValidationResult result)
    {
        foreach (JsonProperty property in invocation.Arguments.EnumerateObject())
        {
            if (property.Name.Length != 0 && property.Name[0] == '#')
            {
                string argumentName = property.Name.Substring(1);
                if (argumentName.Length == 0)
                {
                    AddInvalidReferenceError(index, result);
                    return;
                }

                if (References.CountArgumentMembers(invocation.Arguments, argumentName) != 0)
                {
                    JmapMethodError conflictError = new JmapMethodError();
                    conflictError.Type = JmapErrorCodes.InvalidArguments;
                    conflictError.Kind = JmapMethodErrorKind.InvalidArguments;
                    conflictError.Description = "An argument is present in normal and result-reference form.";
                    result.MethodErrors.Add(index, conflictError);
                    return;
                }

                if (!References.IsValidReference(property.Value))
                {
                    AddInvalidReferenceError(index, result);
                    return;
                }

                JmapResultReference reference = References.DecodeReference(property.Value);
                if (reference.ResultOf == null || !previousCallIds.Contains(reference.ResultOf))
                {
                    AddInvalidReferenceError(index, result);
                    return;
                }
            }
        }
    }

    internal void AddInvalidReferenceError(int index, RequestValidationResult result)
    {
        JmapMethodError error = new JmapMethodError();
        error.Type = JmapErrorCodes.InvalidResultReference;
        error.Kind = JmapMethodErrorKind.InvalidResultReference;
        error.Description = "A result reference does not identify an earlier method call.";
        result.MethodErrors.Add(index, error);
    }

    internal RequestValidationResult ValidResult()
    {
        RequestValidationResult result = new RequestValidationResult();
        result.IsValid = true;
        return result;
    }

    internal RequestValidationResult RequestFailure(JmapProblemKind kind, string? limit, string detail, string path)
    {
        RequestValidationResult result = new RequestValidationResult();
        result.ProblemKind = kind;
        result.Limit = limit;
        result.Detail = detail;
        result.InvalidPaths.Add(path);
        return result;
    }
}
