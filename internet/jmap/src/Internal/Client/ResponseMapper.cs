using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class ResponseMapper
{
    internal const string ErrorResponseName = "error";
    internal JmapJsonReader Reader = new JmapJsonReader(int.MaxValue, 1000);
    internal ProtocolJsonCodec Protocol;

    internal ResponseMapper()
    {
        Protocol = new ProtocolJsonCodec(Reader);
    }

    internal List<JmapCallResult> FindAllResults(JmapResponse? response, string? callId)
    {
        ValidateSearch(response, callId);
        List<JmapCallResult> results = new List<JmapCallResult>();
        if (response == null)
        {
            return results;
        }

        foreach (JmapInvocation invocation in response.MethodResponses)
        {
            if (invocation != null && invocation.CallId == callId)
            {
                results.Add(MapInvocation(invocation));
            }
        }

        return results;
    }

    internal JmapCallResult FindRawResult(JmapResponse? response, string? callId)
    {
        List<JmapCallResult> results = FindAllResults(response, callId);
        if (results.Count == 0)
        {
            throw ProtocolFailure("No method response has the requested call identifier.", "methodResponses", default(JsonElement));
        }

        return results[0];
    }

    internal JmapCallResult FindTypedResult(JmapResponse? response, string? callId, string? responseName)
    {
        if (responseName == null || responseName.Length == 0)
        {
            throw ProtocolFailure("An expected response name is required.", "methodResponses", default(JsonElement));
        }

        List<JmapCallResult> results = FindAllResults(response, callId);
        JmapCallResult? errorResult = null;
        foreach (JmapCallResult result in results)
        {
            if (result.Name == responseName)
            {
                return result;
            }

            if (errorResult == null && result.Error != null)
            {
                errorResult = result;
            }
        }

        if (errorResult != null)
        {
            return errorResult;
        }

        throw ProtocolFailure("No correlated response has the expected response name.", "methodResponses", default(JsonElement));
    }

    internal JmapCallResult MapInvocation(JmapInvocation? invocation)
    {
        if (invocation == null)
        {
            throw ProtocolFailure("A method response invocation cannot be null.", "methodResponses", default(JsonElement));
        }

        if (invocation.Name == null || invocation.Name.Length == 0)
        {
            throw ProtocolFailure("A method response name is required.", "methodResponses/0", invocation.Arguments);
        }

        if (invocation.Arguments.ValueKind != JsonValueKind.Object)
        {
            throw ProtocolFailure("Method response arguments must be an object.", "methodResponses/1", invocation.Arguments);
        }

        if (invocation.CallId == null || invocation.CallId.Length == 0)
        {
            throw ProtocolFailure("A method response call identifier is required.", "methodResponses/2", invocation.Arguments);
        }

        JmapCallResult result = new JmapCallResult();
        result.Name = invocation.Name;
        result.CallId = invocation.CallId;
        result.Arguments = invocation.Arguments.Clone();
        if (invocation.Name == ErrorResponseName)
        {
            result.Error = Protocol.DecodeMethodError(result.Arguments);
        }

        return result;
    }

    internal JsonElement RequireSuccessfulArguments(JmapCallResult? result, string? expectedResponseName)
    {
        if (result == null)
        {
            throw ProtocolFailure("A call result is required.", "methodResponses", default(JsonElement));
        }

        if (result.Error != null)
        {
            throw ConvertMethodError(result, expectedResponseName);
        }

        if (expectedResponseName == null || result.Name != expectedResponseName)
        {
            throw ProtocolFailure("The correlated response name does not match the expected response type.", "methodResponses/0", result.Arguments);
        }

        if (result.Arguments.ValueKind != JsonValueKind.Object)
        {
            throw ProtocolFailure("The correlated response arguments are not an object.", "methodResponses/1", result.Arguments);
        }

        return result.Arguments.Clone();
    }

    internal JsonElement RequireSuccessfulStateArguments(
        JmapCallResult? result,
        string? expectedResponseName,
        string? accountId,
        string? expectedState)
    {
        if (result != null && result.Error != null &&
            result.Error.Kind == JmapMethodErrorKind.StateMismatch)
        {
            string token = result.Error.Type == null ? "unknown" : result.Error.Type;
            string message = "The JMAP method failed with error '" + token + "'.";
            throw new JmapStateMismatchException(
                message,
                expectedResponseName,
                result.CallId,
                accountId,
                expectedState,
                ReadActualState(result.Arguments),
                result.Error,
                result.Arguments.Clone(),
                true);
        }

        return RequireSuccessfulArguments(result, expectedResponseName);
    }

    internal string? ReadActualState(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (JsonProperty property in data.EnumerateObject())
        {
            if ((property.Name == "newState" || property.Name == "actualState") &&
                property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString();
            }
        }

        return null;
    }

    internal JmapMethodException ConvertMethodError(JmapCallResult? result, string? methodName)
    {
        if (result == null || result.Error == null)
        {
            JsonElement data = result == null ? default(JsonElement) : result.Arguments;
            throw ProtocolFailure("A method error result is required.", "methodResponses", data);
        }

        string token = result.Error.Type == null ? "unknown" : result.Error.Type;
        string message = "The JMAP method failed with error '" + token + "'.";
        bool isRetryable = result.Error.Kind == JmapMethodErrorKind.ServerUnavailable;
        return new JmapMethodException(
            message,
            methodName,
            result.CallId,
            result.Error.Type,
            result.Error,
            result.Arguments.Clone(),
            isRetryable);
    }

    internal void ValidateSearch(JmapResponse? response, string? callId)
    {
        if (response == null)
        {
            throw ProtocolFailure("A JMAP response is required.", "", default(JsonElement));
        }

        if (response.MethodResponses == null)
        {
            throw ProtocolFailure("The methodResponses collection cannot be null.", "methodResponses", default(JsonElement));
        }

        if (callId == null || callId.Length == 0)
        {
            throw ProtocolFailure("A call identifier is required for response correlation.", "methodResponses/2", default(JsonElement));
        }
    }

    internal JmapProtocolException ProtocolFailure(string message, string path, JsonElement data)
    {
        JsonElement context = data.ValueKind == JsonValueKind.Undefined ? default(JsonElement) : data.Clone();
        return new JmapProtocolException(message, path, context);
    }
}
