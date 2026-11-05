using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class BlobCopyMethod
{
    internal ValueValidator Values = new ValueValidator();
    internal JmapJsonReader Reader = new JmapJsonReader(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, 64);
    internal BlobPushJsonCodec Codec;
    internal ProtocolJsonCodec Protocol;
    internal ResponseMapper Responses = new ResponseMapper();
    internal ErrorFactory Errors = new ErrorFactory(null);

    internal BlobCopyMethod()
    {
        Codec = new BlobPushJsonCodec(Reader);
        Protocol = new ProtocolJsonCodec(Reader);
    }

    internal Task<JmapInvocation> ProcessInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IBlobService blobService,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessInvocation(invocation, session, blobService, context, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IBlobService blobService,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        BlobCopyRequest? request = DecodeRequest(invocation.Arguments);
        if (request == null || !IsValidRequest(request))
        {
            return ErrorInvocation(
                JmapErrorCodes.InvalidArguments,
                JmapMethodErrorKind.InvalidArguments,
                "The Blob/copy arguments are invalid.",
                callId);
        }

        if (session == null || session.Accounts == null || session.Capabilities == null)
        {
            throw ProtocolFailure("A valid Session is required to dispatch Blob/copy.", "session", invocation.Arguments);
        }

        if (blobService == null)
        {
            throw ProtocolFailure("A blob backend is required to dispatch Blob/copy.", "blobService", invocation.Arguments);
        }

        if (context == null)
        {
            throw ProtocolFailure("A request context is required to dispatch Blob/copy.", "context", invocation.Arguments);
        }

        string fromAccountId = request.FromAccountId == null ? "" : request.FromAccountId;
        string accountId = request.AccountId == null ? "" : request.AccountId;
        if (!session.Accounts.ContainsKey(fromAccountId))
        {
            return ErrorInvocation(
                JmapErrorCodes.FromAccountNotFound,
                JmapMethodErrorKind.FromAccountNotFound,
                "The source account was not found or is not accessible.",
                callId);
        }

        if (!session.Accounts.ContainsKey(accountId))
        {
            return ErrorInvocation(
                JmapErrorCodes.AccountNotFound,
                JmapMethodErrorKind.AccountNotFound,
                "The destination account was not found or is not accessible.",
                callId);
        }

        if (session.Accounts[fromAccountId] == null)
        {
            throw ProtocolFailure("The Session contains a null source account.", "session/accounts/" + fromAccountId, invocation.Arguments);
        }

        if (!session.Capabilities.ContainsKey(JmapCapabilities.Core) ||
            session.Capabilities[JmapCapabilities.Core] == null ||
            session.Capabilities[JmapCapabilities.Core].Core == null)
        {
            return ErrorInvocation(
                JmapErrorCodes.AccountNotSupportedByMethod,
                JmapMethodErrorKind.AccountNotSupportedByMethod,
                "The Core capability required by Blob/copy is unavailable.",
                callId);
        }

        JmapAccount target = session.Accounts[accountId];
        if (target == null)
        {
            throw ProtocolFailure("The Session contains a null destination account.", "session/accounts/" + accountId, invocation.Arguments);
        }

        if (target.IsReadOnly)
        {
            return ErrorInvocation(
                JmapErrorCodes.AccountReadOnly,
                JmapMethodErrorKind.AccountReadOnly,
                "The destination account is read-only.",
                callId);
        }

        cancellationToken.ThrowIfCancellationRequested();
        BlobCopyResponse response = blobService.CopyStoredBlobsAsync(
            request,
            context,
            cancellationToken).GetAwaiter().GetResult();
        ValidateResponse(response, request);
        return SuccessInvocation(response, callId);
    }

    internal BlobCopyResponse MapResponse(
        JmapResponse response,
        string callId,
        BlobCopyRequest request)
    {
        if (request == null || !IsValidRequest(request))
        {
            throw ValidationFailure("Valid original Blob/copy arguments are required for response mapping.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.BlobCopy);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.BlobCopy);
        BlobCopyResponse decoded = Codec.DecodeBlobCopyResponse(arguments);
        ValidateResponse(decoded, request);
        return decoded;
    }

    internal JmapInvocation CreateInvocation(BlobCopyRequest request, string callId)
    {
        if (request == null || !IsValidRequest(request))
        {
            throw ValidationFailure("Valid Blob/copy arguments are required.", "request");
        }

        if (callId == null || callId.Length == 0 || !Values.IsValidUnicode(callId))
        {
            throw ValidationFailure("A usable Blob/copy call identifier is required.", "callId");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeBlobCopyRequest(request, writer);
        writer.DisposeWriter();
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = JmapMethods.BlobCopy;
        invocation.Arguments = Reader.ReadUtf8Memory(stream.ToArray());
        invocation.CallId = callId;
        return invocation;
    }

    internal void ValidateInvocation(JmapInvocation invocation)
    {
        if (invocation == null)
        {
            throw ValidationFailure("A Blob/copy invocation is required.", "methodCalls/0");
        }

        if (invocation.Name != JmapMethods.BlobCopy)
        {
            throw ValidationFailure("The invocation is not a Blob/copy call.", "methodCalls/0/0");
        }

        if (invocation.Arguments.ValueKind != JsonValueKind.Object)
        {
            throw ValidationFailure("Blob/copy arguments must be a JSON object.", "methodCalls/0/1");
        }

        if (invocation.CallId == null || invocation.CallId.Length == 0 || !Values.IsValidUnicode(invocation.CallId))
        {
            throw ValidationFailure("A usable Blob/copy call identifier is required.", "methodCalls/0/2");
        }
    }

    internal BlobCopyRequest? DecodeRequest(JsonElement arguments)
    {
        JsonElement fromAccountId = Codec.Values.GetMember(arguments, JmapProperties.FromAccountId);
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement blobIds = Codec.Values.GetMember(arguments, JmapProperties.BlobIds);
        if (fromAccountId.ValueKind != JsonValueKind.String ||
            accountId.ValueKind != JsonValueKind.String ||
            blobIds.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (JsonElement blobId in blobIds.EnumerateArray())
        {
            if (blobId.ValueKind != JsonValueKind.String)
            {
                return null;
            }
        }

        return Codec.DecodeBlobCopyRequest(arguments);
    }

    internal bool IsValidRequest(BlobCopyRequest request)
    {
        if (!Values.IsId(request.FromAccountId) || !Values.IsId(request.AccountId) ||
            request.FromAccountId == request.AccountId || request.BlobIds == null)
        {
            return false;
        }

        HashSet<string> identifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (string blobId in request.BlobIds)
        {
            if (!Values.IsId(blobId) || !identifiers.Add(blobId))
            {
                return false;
            }
        }

        return true;
    }

    internal void ValidateResponse(BlobCopyResponse response, BlobCopyRequest request)
    {
        if (response == null || response.FromAccountId != request.FromAccountId ||
            response.AccountId != request.AccountId ||
            (request.BlobIds.Count != 0 && response.Copied == null && response.NotCopied == null) ||
            (response.Copied != null && response.Copied.Count == 0) ||
            (response.NotCopied != null && response.NotCopied.Count == 0))
        {
            throw ProtocolFailure("The Blob/copy response has invalid account or partition metadata.", "methodResponses/1", default(JsonElement));
        }

        HashSet<string> requested = new HashSet<string>(request.BlobIds, StringComparer.Ordinal);
        HashSet<string> partitioned = new HashSet<string>(StringComparer.Ordinal);
        if (response.Copied != null)
        {
            foreach (KeyValuePair<string, string> item in response.Copied)
            {
                if (!requested.Contains(item.Key) || !partitioned.Add(item.Key) || !Values.IsId(item.Value))
                {
                    throw ProtocolFailure("The Blob/copy copied map is invalid.", "copied", default(JsonElement));
                }
            }
        }

        if (response.NotCopied != null)
        {
            foreach (KeyValuePair<string, JmapSetError> item in response.NotCopied)
            {
                if (!requested.Contains(item.Key) || !partitioned.Add(item.Key))
                {
                    throw ProtocolFailure("The Blob/copy notCopied map is invalid.", "notCopied", default(JsonElement));
                }

                ValidateCopyError(item.Value, item.Key);
            }
        }

        if (partitioned.Count != requested.Count)
        {
            throw ProtocolFailure("The Blob/copy response does not account for every requested blob.", "methodResponses/1", default(JsonElement));
        }
    }

    internal void ValidateCopyError(JmapSetError error, string blobId)
    {
        if (error == null || error.Type == null || error.Type.Length == 0 || !Values.IsValidUnicode(error.Type))
        {
            throw ProtocolFailure("A Blob/copy failure contains an invalid SetError.", "notCopied/" + blobId, default(JsonElement));
        }

        JmapSetError normalized = Errors.NormalizeSetError(error);
        bool allowed = normalized.Kind == JmapSetErrorKind.Forbidden ||
            normalized.Kind == JmapSetErrorKind.OverQuota ||
            normalized.Kind == JmapSetErrorKind.TooLarge ||
            normalized.Kind == JmapSetErrorKind.RateLimit ||
            normalized.Kind == JmapSetErrorKind.InvalidProperties ||
            normalized.Kind == JmapSetErrorKind.Singleton ||
            normalized.Kind == JmapSetErrorKind.NotFound ||
            normalized.Kind == JmapSetErrorKind.Unknown;
        if (!allowed)
        {
            throw ProtocolFailure("Blob/copy returned a SetError that is not valid for creation.", "notCopied/" + blobId + "/type", default(JsonElement));
        }

        error.Kind = normalized.Kind;
    }

    internal JmapInvocation SuccessInvocation(BlobCopyResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeBlobCopyResponse(response, writer);
        writer.DisposeWriter();
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = JmapMethods.BlobCopy;
        invocation.Arguments = Reader.ReadUtf8Memory(stream.ToArray());
        invocation.CallId = callId;
        return invocation;
    }

    internal JmapInvocation ErrorInvocation(
        string type,
        JmapMethodErrorKind kind,
        string description,
        string callId)
    {
        JmapMethodError error = Errors.CreateMethodError(type, description, false);
        error.Kind = kind;
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Protocol.EncodeMethodError(error, writer);
        writer.DisposeWriter();
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = ResponseMapper.ErrorResponseName;
        invocation.Arguments = Reader.ReadUtf8Memory(stream.ToArray());
        invocation.CallId = callId;
        return invocation;
    }

    internal JmapValidationException ValidationFailure(string message, string path)
    {
        return new JmapValidationException(
            message,
            new List<string> { path },
            default(JsonElement));
    }

    internal JmapProtocolException ProtocolFailure(string message, string path, JsonElement data)
    {
        JsonElement context = data.ValueKind == JsonValueKind.Undefined ? default(JsonElement) : data.Clone();
        return new JmapProtocolException(message, path, context);
    }
}
