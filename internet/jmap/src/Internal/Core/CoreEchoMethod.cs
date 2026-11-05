using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class CoreEchoMethod
{
    internal ResponseMapper Responses = new ResponseMapper();
    internal ValueValidator Values = new ValueValidator();

    internal JmapInvocation ProcessInvocation(JmapInvocation? invocation)
    {
        if (invocation == null)
        {
            throw ValidationFailure(
                "A Core/echo invocation is required.",
                "methodCalls/0",
                default(JsonElement));
        }

        if (invocation.Name != JmapMethods.CoreEcho)
        {
            throw ValidationFailure(
                "The invocation is not a Core/echo call.",
                "methodCalls/0/0",
                invocation.Arguments);
        }

        if (invocation.Arguments.ValueKind != JsonValueKind.Object)
        {
            throw ValidationFailure(
                "Core/echo arguments must be a JSON object.",
                "methodCalls/0/1",
                invocation.Arguments);
        }

        if (invocation.CallId == null || invocation.CallId.Length == 0 || !Values.IsValidUnicode(invocation.CallId))
        {
            throw ValidationFailure(
                "A usable Core/echo method call identifier is required.",
                "methodCalls/0/2",
                invocation.Arguments);
        }

        JmapInvocation response = new JmapInvocation();
        response.Name = JmapMethods.CoreEcho;
        response.Arguments = invocation.Arguments.Clone();
        response.CallId = invocation.CallId;
        return response;
    }

    internal JsonElement MapResponse(JmapResponse? response, string? callId)
    {
        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.CoreEcho);
        return Responses.RequireSuccessfulArguments(result, JmapMethods.CoreEcho);
    }

    internal JmapValidationException ValidationFailure(string message, string path, JsonElement data)
    {
        JsonElement context = data.ValueKind == JsonValueKind.Undefined ? default(JsonElement) : data.Clone();
        return new JmapValidationException(message, new List<string> { path }, context);
    }
}
