using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace jmap;

internal class ResultReferenceResolution
{
    internal bool IsSuccess;
    internal JsonElement Arguments;
    internal JmapMethodError? Error;
}

internal class ResultReferenceResolver
{
    internal ResultReferencePath Paths = new ResultReferencePath();
    internal JmapJsonReader Reader = new JmapJsonReader(int.MaxValue, 1000);

    internal ResultReferenceResolution ResolveArguments(JsonElement arguments, List<JmapInvocation>? previousResponses)
    {
        if (arguments.ValueKind != JsonValueKind.Object)
        {
            return InvalidArguments("JMAP method arguments must be a JSON object.");
        }

        Dictionary<string, JsonElement> resolvedValues = new Dictionary<string, JsonElement>();
        foreach (JsonProperty property in arguments.EnumerateObject())
        {
            if (CountArgumentMembers(arguments, property.Name) != 1)
            {
                return InvalidArguments("Argument names must be unique.");
            }

            if (property.Name.Length != 0 && property.Name[0] == '#')
            {
                string argumentName = property.Name.Substring(1);
                if (argumentName.Length == 0)
                {
                    return InvalidResultReference("A result-reference argument name is empty.");
                }

                if (CountArgumentMembers(arguments, argumentName) != 0)
                {
                    return InvalidArguments("An argument is present in both normal and result-reference form.");
                }

                if (!IsValidReference(property.Value))
                {
                    return InvalidResultReference("A result reference is missing a required string member.");
                }

                if (previousResponses == null)
                {
                    return InvalidResultReference("Previous method responses are unavailable.");
                }

                JmapResultReference reference = DecodeReference(property.Value);
                JmapInvocation? response = FindFirstResponse(previousResponses, reference.ResultOf);
                if (response == null || response.Name != reference.Name || response.Arguments.ValueKind != JsonValueKind.Object)
                {
                    return InvalidResultReference("A result reference does not match a previous method response.");
                }

                if (!Paths.CanEvaluatePath(response.Arguments, reference.Path))
                {
                    return InvalidResultReference("A result-reference path cannot be resolved.");
                }

                JsonElement resolvedValue = Paths.EvaluatePath(response.Arguments, reference.Path);
                resolvedValues.Add(argumentName, resolvedValue);
            }
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        writer.WriteObjectStart();
        foreach (JsonProperty property in arguments.EnumerateObject())
        {
            if (property.Name.Length != 0 && property.Name[0] == '#')
            {
                string argumentName = property.Name.Substring(1);
                writer.WriteElementMember(argumentName, resolvedValues[argumentName]);
            }
            else
            {
                writer.WriteElementMember(property.Name, property.Value);
            }
        }

        writer.WriteObjectEnd();
        writer.DisposeWriter();
        ResultReferenceResolution resolution = new ResultReferenceResolution();
        resolution.IsSuccess = true;
        resolution.Arguments = Reader.ReadUtf8Memory(stream.ToArray());
        return resolution;
    }

    internal bool IsValidReference(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        JsonElement resultOf = ReadUniqueMember(value, JmapProperties.ResultOf);
        JsonElement name = ReadUniqueMember(value, JmapProperties.Name);
        JsonElement path = ReadUniqueMember(value, JmapProperties.Path);
        return resultOf.ValueKind == JsonValueKind.String &&
            name.ValueKind == JsonValueKind.String &&
            path.ValueKind == JsonValueKind.String;
    }

    internal JmapResultReference DecodeReference(JsonElement value)
    {
        JmapResultReference reference = new JmapResultReference();
        reference.ResultOf = ReadUniqueMember(value, JmapProperties.ResultOf).GetString();
        reference.Name = ReadUniqueMember(value, JmapProperties.Name).GetString();
        reference.Path = ReadUniqueMember(value, JmapProperties.Path).GetString();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name != JmapProperties.ResultOf && property.Name != JmapProperties.Name && property.Name != JmapProperties.Path)
            {
                reference.UnknownProperties.Add(property.Name, property.Value.Clone());
            }
        }

        return reference;
    }

    internal JsonElement ReadUniqueMember(JsonElement value, string name)
    {
        JsonElement result = default(JsonElement);
        int matches = 0;
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name == name)
            {
                result = property.Value;
                matches = matches + 1;
            }
        }

        if (matches == 1)
        {
            return result;
        }

        return default(JsonElement);
    }

    internal int CountArgumentMembers(JsonElement arguments, string name)
    {
        int count = 0;
        foreach (JsonProperty property in arguments.EnumerateObject())
        {
            if (property.Name == name)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal JmapInvocation? FindFirstResponse(List<JmapInvocation> previousResponses, string? resultOf)
    {
        foreach (JmapInvocation response in previousResponses)
        {
            if (response != null && response.CallId == resultOf)
            {
                return response;
            }
        }

        return null;
    }

    internal ResultReferenceResolution InvalidResultReference(string description)
    {
        JmapMethodError error = new JmapMethodError();
        error.Type = JmapErrorCodes.InvalidResultReference;
        error.Kind = JmapMethodErrorKind.InvalidResultReference;
        error.Description = description;
        return ErrorResolution(error);
    }

    internal ResultReferenceResolution InvalidArguments(string description)
    {
        JmapMethodError error = new JmapMethodError();
        error.Type = JmapErrorCodes.InvalidArguments;
        error.Kind = JmapMethodErrorKind.InvalidArguments;
        error.Description = description;
        return ErrorResolution(error);
    }

    internal ResultReferenceResolution ErrorResolution(JmapMethodError error)
    {
        ResultReferenceResolution resolution = new ResultReferenceResolution();
        resolution.Error = error;
        return resolution;
    }
}
