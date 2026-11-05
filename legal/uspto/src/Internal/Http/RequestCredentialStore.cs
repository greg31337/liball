namespace uspto;

internal class RequestCredentialValue
{
    internal string Value = string.Empty;
}

internal static class RequestCredentialStore
{
    internal static readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.Net.Http.HttpRequestMessage, RequestCredentialValue> Values = new global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.Net.Http.HttpRequestMessage, RequestCredentialValue>();

    internal static void Remember(global::System.Net.Http.HttpRequestMessage request, string? credential)
    {
        RequestCredentialValue value = Values.GetOrCreateValue(request);
        value.Value = credential ?? string.Empty;
    }

    internal static string[] MergeResponseSecrets(global::System.Net.Http.HttpResponseMessage response, string[]? configuredSecrets)
    {
        global::System.Collections.Generic.List<string> result = new global::System.Collections.Generic.List<string>();
        if (configuredSecrets != null)
        {
            int configuredIndex;
            for (configuredIndex = 0; configuredIndex < configuredSecrets.Length; configuredIndex += 1)
            {
                AddDistinct(result, configuredSecrets[configuredIndex]);
            }
        }

        if (response.RequestMessage != null)
        {
            RequestCredentialValue value = Values.GetOrCreateValue(response.RequestMessage);
            AddDistinct(result, value.Value);
        }

        return result.ToArray();
    }

    internal static void AddDistinct(global::System.Collections.Generic.List<string> values, string? candidate)
    {
        if (global::System.String.IsNullOrEmpty(candidate))
        {
            return;
        }

        int index;
        for (index = 0; index < values.Count; index += 1)
        {
            if (values[index] == candidate)
            {
                return;
            }
        }

        values.Add(candidate);
    }
}
