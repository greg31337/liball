using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace jmap;

/// <summary>
/// Builds isolated snapshots of ordered JMAP method calls and their request metadata.
/// </summary>
public class JmapRequestBuilder : IJmapRequestBuilder
{
    internal List<string> Capabilities = new List<string>();
    internal List<JmapInvocation> Calls = new List<JmapInvocation>();
    internal Dictionary<string, string>? CreatedIds;
    internal HashSet<string> CallIds = new HashSet<string>(StringComparer.Ordinal);
    internal long NextCallId;
    internal ValueValidator Values = new ValueValidator();
    internal JsonPointer Pointers = new JsonPointer();
    internal JmapJsonReader Reader = new JmapJsonReader(int.MaxValue, 1000);

    /// <summary>Creates an empty request builder.</summary>
    public JmapRequestBuilder()
    {
    }

    /// <summary>Adds a capability URI once while retaining first-add order.</summary>
    /// <param name="capability">The capability URI.</param>
    public void AddRequestCapability(string capability)
    {
        if (!Values.IsCapability(capability))
        {
            throw Validation("A valid capability URI is required.", "using");
        }

        AddCapabilityIfMissing(capability);
    }

    /// <summary>Adds a raw method call and returns its explicit or generated call identifier.</summary>
    /// <param name="call">The raw method call.</param>
    /// <returns>The call identifier assigned to the method call.</returns>
    public string AddRawMethodCall(JmapRawMethodCall call)
    {
        if (call == null)
        {
            throw Validation("A raw method call is required.", "methodCalls");
        }

        ValidateCallInputs(call.Name, call.Arguments, call.RequiredCapabilities, call.CallId);
        JsonElement arguments = MergeResultReferences(call.Arguments, call.ResultReferences);
        return AddPreparedCall(call.Name, arguments, call.RequiredCapabilities, call.CallId);
    }

    /// <summary>Adds a method call with pre-encoded typed arguments.</summary>
    /// <param name="methodName">The JMAP method name.</param>
    /// <param name="arguments">The encoded arguments object.</param>
    /// <param name="requiredCapabilities">The capabilities required by the method.</param>
    /// <param name="callId">An explicit call identifier, or null to generate one.</param>
    /// <returns>The call identifier assigned to the method call.</returns>
    public string AddTypedMethodCall(string methodName, JsonElement arguments, List<string> requiredCapabilities, string? callId)
    {
        ValidateCallInputs(methodName, arguments, requiredCapabilities, callId);
        return AddPreparedCall(methodName, CloneArguments(arguments), requiredCapabilities, callId);
    }

    /// <summary>Creates validated reference data for a previous method response.</summary>
    /// <param name="resultOf">The previous method call identifier.</param>
    /// <param name="responseName">The expected response name.</param>
    /// <param name="path">The result-reference JSON Pointer path.</param>
    /// <returns>The validated result reference.</returns>
    public JmapResultReference CreateResultReference(string resultOf, string responseName, string path)
    {
        if (!IsUsableCallId(resultOf))
        {
            throw Validation("A usable previous call identifier is required.", "resultOf");
        }

        if (!CallIds.Contains(resultOf))
        {
            throw Validation("The result reference must identify an earlier method call.", "resultOf");
        }

        if (!Values.IsMethodName(responseName))
        {
            throw Validation("A valid response method name is required.", "name");
        }

        if (!Pointers.IsPointer(path))
        {
            throw Validation("A valid result-reference path is required.", "path");
        }

        JmapResultReference reference = new JmapResultReference();
        reference.ResultOf = resultOf;
        reference.Name = responseName;
        reference.Path = path;
        return reference;
    }

    /// <summary>Adds or replaces a creation-id mapping carried into the request.</summary>
    /// <param name="creationId">The client creation identifier.</param>
    /// <param name="objectId">The corresponding server object identifier.</param>
    public void AddRequestCreatedId(string creationId, string objectId)
    {
        if (!Values.IsCreationId(creationId))
        {
            throw Validation("A valid creation identifier is required.", "createdIds");
        }

        if (!Values.IsId(objectId))
        {
            throw Validation("A valid object identifier is required.", "createdIds/" + creationId);
        }

        if (CreatedIds == null)
        {
            CreatedIds = new Dictionary<string, string>();
        }

        CreatedIds[creationId] = objectId;
    }

    /// <summary>Builds a detached snapshot of the current ordered request.</summary>
    /// <returns>The request snapshot.</returns>
    public JmapRequest BuildJmapRequest()
    {
        JmapRequest request = new JmapRequest();
        request.Using = new List<string>(Capabilities);
        foreach (JmapInvocation call in Calls)
        {
            request.MethodCalls.Add(CloneInvocation(call));
        }

        if (CreatedIds != null)
        {
            request.CreatedIds = new Dictionary<string, string>(CreatedIds);
        }

        return request;
    }

    /// <summary>Clears all accumulated request data and restarts generated identifiers.</summary>
    public void ResetJmapRequestBuilder()
    {
        Capabilities.Clear();
        Calls.Clear();
        CreatedIds = null;
        CallIds.Clear();
        NextCallId = 0L;
    }

    internal void ValidateCallInputs(string? methodName, JsonElement arguments, List<string>? requiredCapabilities, string? callId)
    {
        if (!Values.IsMethodName(methodName))
        {
            throw Validation("A valid JMAP method name is required.", "methodCalls/name");
        }

        if (arguments.ValueKind != JsonValueKind.Object)
        {
            throw Validation("JMAP method arguments must be an object.", "methodCalls/arguments");
        }

        if (requiredCapabilities == null)
        {
            throw Validation("A required-capability list is required.", "methodCalls/requiredCapabilities");
        }

        foreach (string capability in requiredCapabilities)
        {
            if (!Values.IsCapability(capability))
            {
                throw Validation("A method call contains an invalid capability URI.", "methodCalls/requiredCapabilities");
            }
        }

        if (callId != null && !IsUsableCallId(callId))
        {
            throw Validation("A usable explicit call identifier is required.", "methodCalls/callId");
        }

        if (callId != null && CallIds.Contains(callId))
        {
            throw Validation("A method call identifier must be unique within a request.", "methodCalls/callId");
        }
    }

    internal string AddPreparedCall(string? methodName, JsonElement arguments, List<string> requiredCapabilities, string? callId)
    {
        foreach (string capability in requiredCapabilities)
        {
            AddCapabilityIfMissing(capability);
        }

        string assignedCallId = callId == null ? GenerateCallId() : callId;
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = methodName;
        invocation.Arguments = arguments;
        invocation.CallId = assignedCallId;
        Calls.Add(invocation);
        CallIds.Add(assignedCallId);
        return assignedCallId;
    }

    internal void AddCapabilityIfMissing(string capability)
    {
        if (!Capabilities.Contains(capability))
        {
            Capabilities.Add(capability);
        }
    }

    internal string GenerateCallId()
    {
        string candidate = "s" + NextCallId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        while (CallIds.Contains(candidate))
        {
            NextCallId = NextCallId + 1L;
            candidate = "s" + NextCallId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        NextCallId = NextCallId + 1L;
        return candidate;
    }

    internal bool IsUsableCallId(string? callId)
    {
        return callId != null && callId.Length != 0 && Values.IsValidUnicode(callId);
    }

    internal JsonElement MergeResultReferences(JsonElement arguments, Dictionary<string, JmapResultReference>? references)
    {
        if (references == null)
        {
            throw Validation("Result-reference storage cannot be null.", "methodCalls/resultReferences");
        }

        if (references.Count > SecurityLimits.MaximumResultReferences)
        {
            throw Validation("A method call contains too many result references.", "methodCalls/resultReferences");
        }

        Dictionary<string, JsonElement> members = ReadArgumentMembers(arguments);
        foreach (KeyValuePair<string, JmapResultReference> item in references)
        {
            ValidateReferenceArgument(item.Key, item.Value, members);
            members.Add(item.Key, EncodeResultReference(item.Value));
        }

        return BuildArguments(members);
    }

    internal Dictionary<string, JsonElement> ReadArgumentMembers(JsonElement arguments)
    {
        Dictionary<string, JsonElement> members = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (JsonProperty property in arguments.EnumerateObject())
        {
            if (members.ContainsKey(property.Name))
            {
                throw Validation("Method arguments contain a duplicate member.", "methodCalls/arguments/" + property.Name);
            }

            members.Add(property.Name, property.Value.Clone());
        }

        return members;
    }

    internal void ValidateReferenceArgument(string argumentName, JmapResultReference? reference, Dictionary<string, JsonElement> arguments)
    {
        if (argumentName == null || argumentName.Length < 2 || argumentName[0] != '#')
        {
            throw Validation("A reference argument name must begin with '#' and contain a name.", "methodCalls/resultReferences");
        }

        if (arguments.ContainsKey(argumentName) || arguments.ContainsKey(argumentName.Substring(1)))
        {
            throw Validation("A reference argument conflicts with an existing argument.", "methodCalls/arguments/" + argumentName);
        }

        if (reference == null || !IsUsableCallId(reference.ResultOf) ||
            reference.ResultOf == null || !CallIds.Contains(reference.ResultOf) ||
            !Values.IsMethodName(reference.Name) || !Pointers.IsPointer(reference.Path))
        {
            throw Validation("A raw method call contains an invalid result reference.", "methodCalls/resultReferences/" + argumentName);
        }
    }

    internal JsonElement EncodeResultReference(JmapResultReference reference)
    {
        Dictionary<string, JsonElement> unknown = reference.UnknownProperties;
        if (unknown == null)
        {
            throw Validation("Result-reference unknown-property storage cannot be null.", "methodCalls/resultReferences");
        }

        if (unknown.ContainsKey(JmapProperties.ResultOf) || unknown.ContainsKey(JmapProperties.Name) || unknown.ContainsKey(JmapProperties.Path))
        {
            throw Validation("A result-reference unknown member conflicts with a standard member.", "methodCalls/resultReferences");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        writer.WriteObjectStart();
        writer.WriteStringMember(JmapProperties.ResultOf, reference.ResultOf, true);
        writer.WriteStringMember(JmapProperties.Name, reference.Name, true);
        writer.WriteStringMember(JmapProperties.Path, reference.Path, true);
        List<string> names = new List<string>(unknown.Keys);
        names.Sort(StringComparer.Ordinal);
        foreach (string name in names)
        {
            writer.WriteElementMember(name, unknown[name]);
        }

        writer.WriteObjectEnd();
        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    internal JsonElement BuildArguments(Dictionary<string, JsonElement> members)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        writer.WriteObjectStart();
        List<string> names = new List<string>(members.Keys);
        names.Sort(StringComparer.Ordinal);
        foreach (string name in names)
        {
            writer.WriteElementMember(name, members[name]);
        }

        writer.WriteObjectEnd();
        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    internal JsonElement CloneArguments(JsonElement arguments)
    {
        return BuildArguments(ReadArgumentMembers(arguments));
    }

    internal JmapInvocation CloneInvocation(JmapInvocation invocation)
    {
        JmapInvocation clone = new JmapInvocation();
        clone.Name = invocation.Name;
        clone.Arguments = invocation.Arguments.Clone();
        clone.CallId = invocation.CallId;
        return clone;
    }

    internal JmapValidationException Validation(string message, string path)
    {
        return new JmapValidationException(message, new List<string> { path }, default(JsonElement));
    }
}
