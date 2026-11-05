using System.Text;
using System.Text.Json;

namespace jmap.Tests;

internal static class PublicApiTestHelper
{
    public static JmapRequestContext AuthenticatedContext()
    {
        return new JmapRequestContext
        {
            UserId = "user-1",
            CredentialId = "credential-1",
            IsSecure = true
        };
    }

    public static JmapSession CoreSession(string state)
    {
        JmapCoreCapabilities core = new JmapCoreCapabilities
        {
            MaxSizeUpload = JmapProtocolLimits.SuggestedMinimumMaxSizeUpload,
            MaxConcurrentUpload = JmapProtocolLimits.SuggestedMinimumMaxConcurrentUpload,
            MaxSizeRequest = JmapProtocolLimits.SuggestedMinimumMaxSizeRequest,
            MaxConcurrentRequests = JmapProtocolLimits.SuggestedMinimumMaxConcurrentRequests,
            MaxCallsInRequest = JmapProtocolLimits.SuggestedMinimumMaxCallsInRequest,
            MaxObjectsInGet = JmapProtocolLimits.SuggestedMinimumMaxObjectsInGet,
            MaxObjectsInSet = JmapProtocolLimits.SuggestedMinimumMaxObjectsInSet
        };
        JmapSession session = new JmapSession
        {
            Username = "user@example.test",
            ApiUrl = "https://jmap.example.test/api",
            DownloadUrl = "https://jmap.example.test/download/{accountId}/{blobId}/{name}?type={type}",
            UploadUrl = "https://jmap.example.test/upload/{accountId}",
            EventSourceUrl = "https://jmap.example.test/events?types={types}&closeafter={closeafter}&ping={ping}",
            State = state
        };
        session.Capabilities.Add(JmapCapabilities.Core, new JmapCapability { Core = core });
        session.Accounts.Add("account-1", new JmapAccount { Name = "Primary", IsPersonal = true });
        session.PrimaryAccounts.Add(JmapCapabilities.Core, "account-1");
        return session;
    }

    public static JmapRequest CoreEchoRequest(string callId, string argumentsJson)
    {
        JmapInvocation invocation = new JmapInvocation
        {
            Name = JmapMethods.CoreEcho,
            Arguments = Json(argumentsJson),
            CallId = callId
        };
        JmapRequest request = new JmapRequest();
        request.Using.Add(JmapCapabilities.Core);
        request.MethodCalls.Add(invocation);
        return request;
    }

    public static JmapEndpointRequest JsonEndpointRequest(string json)
    {
        byte[] body = Encoding.UTF8.GetBytes(json);
        return new JmapEndpointRequest
        {
            Context = AuthenticatedContext(),
            Method = "POST",
            ContentType = JmapMediaTypes.Json + "; charset=utf-8",
            Body = new MemoryStream(body, false),
            ContentLength = body.Length,
            HasContentLength = true
        };
    }

    public static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    public static JmapRequest DecodeRequestWithPublicApi(byte[] utf8)
    {
        IJmapCodec codec = new JmapJsonCodec();
        return codec.DecodeRequestMemory(utf8);
    }

    public static byte[] EncodeRequestWithPublicApi(JmapRequest request)
    {
        IJmapCodec codec = new JmapJsonCodec();
        return codec.EncodeRequestBytes(request);
    }
}
