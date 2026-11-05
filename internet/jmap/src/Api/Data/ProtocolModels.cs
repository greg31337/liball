using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

/// <summary>
/// Represents a JMAP method call or method response tuple.
/// </summary>
public class JmapInvocation
{
    /// <summary>The method or response name in the first tuple position.</summary>
    public string? Name;

    /// <summary>The arbitrary arguments object in the second tuple position.</summary>
    public JsonElement Arguments;

    /// <summary>The method call identifier in the third tuple position.</summary>
    public string? CallId;
}

/// <summary>
/// Represents a JMAP Request object with ordered method calls.
/// </summary>
public class JmapRequest
{
    /// <summary>The capability URIs enabled for this request.</summary>
    public List<string> Using = new List<string>();

    /// <summary>The method calls in execution order.</summary>
    public List<JmapInvocation> MethodCalls = new List<JmapInvocation>();

    /// <summary>The initial creation-id map, or null when the createdIds property is omitted.</summary>
    public Dictionary<string, string>? CreatedIds;

    /// <summary>Stores unrecognized Request properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Represents a JMAP Response object with ordered method responses.
/// </summary>
public class JmapResponse
{
    /// <summary>The method responses in processing order.</summary>
    public List<JmapInvocation> MethodResponses = new List<JmapInvocation>();

    /// <summary>The resulting creation-id map, or null when the createdIds property is omitted.</summary>
    public Dictionary<string, string>? CreatedIds;

    /// <summary>The current state of the Session resource.</summary>
    public string? SessionState;

    /// <summary>Stores unrecognized Response properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Describes a reference to a value in a previous method response.
/// </summary>
public class JmapResultReference
{
    /// <summary>The call identifier of the previous method response.</summary>
    public string? ResultOf;

    /// <summary>The expected name of the previous method response.</summary>
    public string? Name;

    /// <summary>The result-reference path evaluated against the response arguments.</summary>
    public string? Path;

    /// <summary>Stores unrecognized result-reference properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Represents an arbitrary standard or vendor JMAP method call before it is added to a request.
/// </summary>
public class JmapRawMethodCall
{
    /// <summary>The standard or vendor method name.</summary>
    public string? Name;

    /// <summary>The arbitrary method arguments object.</summary>
    public JsonElement Arguments;

    /// <summary>An explicit call identifier, or null for builder-generated identification.</summary>
    public string? CallId;

    /// <summary>The capability URIs required by this call.</summary>
    public List<string> RequiredCapabilities = new List<string>();

    /// <summary>Maps result-reference argument names, including their leading <c>#</c>, to reference data.</summary>
    public Dictionary<string, JmapResultReference> ResultReferences = new Dictionary<string, JmapResultReference>();
}

/// <summary>
/// Provides typed and raw information for one method response selected from a batch.
/// </summary>
public class JmapCallResult
{
    /// <summary>The response name.</summary>
    public string? Name;

    /// <summary>The originating method call identifier.</summary>
    public string? CallId;

    /// <summary>The raw response arguments object.</summary>
    public JsonElement Arguments;

    /// <summary>The classified method error, or null for a successful response.</summary>
    public JmapMethodError? Error;
}

/// <summary>
/// Represents request-level problem details returned by a JMAP endpoint.
/// </summary>
public class JmapProblem
{
    /// <summary>The complete problem type URI as received on the wire.</summary>
    public string? Type;

    /// <summary>The library classification of the problem type.</summary>
    public JmapProblemKind Kind;

    /// <summary>A short human-readable summary of the problem type.</summary>
    public string? Title;

    /// <summary>The HTTP status code generated for this occurrence.</summary>
    public int Status;

    /// <summary>Whether the status property was present.</summary>
    public bool HasStatus;

    /// <summary>A human-readable explanation specific to this occurrence.</summary>
    public string? Detail;

    /// <summary>A URI reference identifying this occurrence.</summary>
    public string? Instance;

    /// <summary>The name of the exceeded JMAP limit for a limit problem.</summary>
    public string? Limit;

    /// <summary>Stores unrecognized problem-detail properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Represents a JMAP method-level error while preserving its exact wire type.
/// </summary>
public class JmapMethodError
{
    /// <summary>The exact method error type received on the wire.</summary>
    public string? Type;

    /// <summary>The library classification of the method error.</summary>
    public JmapMethodErrorKind Kind;

    /// <summary>A non-localized diagnostic description.</summary>
    public string? Description;

    /// <summary>Stores unrecognized method error properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Represents a per-record JMAP SetError while preserving its exact wire type and structured details.
/// </summary>
public class JmapSetError
{
    /// <summary>The exact set error type received on the wire.</summary>
    public string? Type;

    /// <summary>The library classification of the set error.</summary>
    public JmapSetErrorKind Kind;

    /// <summary>A diagnostic or genuinely user-visible description, according to the error type.</summary>
    public string? Description;

    /// <summary>The invalid property paths supplied with an invalidProperties or invalidEmail error.</summary>
    public List<string>? Properties;

    /// <summary>The identifier of an equivalent record supplied with an alreadyExists error.</summary>
    public string? ExistingId;

    /// <summary>The maximum permitted message size supplied with a tooLarge submission error.</summary>
    public long MaxSize;

    /// <summary>Whether the maxSize property was present.</summary>
    public bool HasMaxSize;

    /// <summary>The maximum permitted recipient count supplied with a tooManyRecipients error.</summary>
    public long MaxRecipients;

    /// <summary>Whether the maxRecipients property was present.</summary>
    public bool HasMaxRecipients;

    /// <summary>The invalid recipient addresses supplied with an invalidRecipients error.</summary>
    public List<string>? InvalidRecipients;

    /// <summary>The missing blob identifiers supplied with a blobNotFound error.</summary>
    public List<string>? NotFound;

    /// <summary>Stores unrecognized SetError properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}
