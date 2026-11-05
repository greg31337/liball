using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

/// <summary>
/// Builds ordered JMAP requests with capabilities, calls, references, and created identifiers.
/// </summary>
public interface IJmapRequestBuilder
{
    /// <summary>Adds a capability URI if it has not already been enabled.</summary>
    /// <param name="capability">The capability URI.</param>
    void AddRequestCapability(string capability);

    /// <summary>Adds an arbitrary standard or vendor call to the ordered request.</summary>
    /// <param name="call">The call definition and optional explicit call id.</param>
    /// <returns>The explicit or generated call id.</returns>
    string AddRawMethodCall(JmapRawMethodCall call);

    /// <summary>Adds a typed call whose arguments have already been encoded.</summary>
    /// <param name="methodName">The JMAP method name.</param>
    /// <param name="arguments">The encoded method arguments object.</param>
    /// <param name="requiredCapabilities">The capability URIs required by the call.</param>
    /// <param name="callId">An explicit call id, or null to generate one.</param>
    /// <returns>The explicit or generated call id.</returns>
    string AddTypedMethodCall(string methodName, JsonElement arguments, List<string> requiredCapabilities, string? callId);

    /// <summary>Creates a reference to data in a previous response.</summary>
    /// <param name="resultOf">The previous call id.</param>
    /// <param name="responseName">The expected response name.</param>
    /// <param name="path">The result-reference path.</param>
    /// <returns>The result-reference data.</returns>
    JmapResultReference CreateResultReference(string resultOf, string responseName, string path);

    /// <summary>Adds a carried-in creation-id mapping.</summary>
    /// <param name="creationId">The client creation id.</param>
    /// <param name="objectId">The resolved object id.</param>
    void AddRequestCreatedId(string creationId, string objectId);

    /// <summary>Builds a snapshot of the current ordered request.</summary>
    /// <returns>The built JMAP request.</returns>
    JmapRequest BuildJmapRequest();

    /// <summary>Clears all capabilities, calls, created ids, and generated-id state.</summary>
    void ResetJmapRequestBuilder();
}
