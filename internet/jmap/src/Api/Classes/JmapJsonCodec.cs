using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Converts the public JMAP protocol models to and from their UTF-8 JSON wire representations.
/// </summary>
public class JmapJsonCodec : IJmapCodec
{
    internal JmapJsonReader Reader;
    internal SessionJsonCodec Sessions;
    internal ProtocolJsonCodec Protocol;
    internal StandardMethodJsonCodec Standard;
    internal BlobPushJsonCodec Push;
    internal EmailJsonCodec Email;
    internal EmailSubmissionJsonCodec Submission;

    /// <summary>Creates a codec with bounded JSON nesting and document size.</summary>
    public JmapJsonCodec()
    {
        Reader = new JmapJsonReader(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, 64);
        Sessions = new SessionJsonCodec(Reader);
        Protocol = new ProtocolJsonCodec(Reader);
        Standard = new StandardMethodJsonCodec(Reader);
        Push = new BlobPushJsonCodec(Reader);
        Email = new EmailJsonCodec(Reader);
        Submission = new EmailSubmissionJsonCodec(Reader);
    }

    /// <summary>Decodes a Session from UTF-8 bytes.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded Session.</returns>
    public JmapSession DecodeSessionBytes(ReadOnlySpan<byte> utf8)
    {
        return Sessions.DecodeSession(Reader.ReadUtf8Span(utf8));
    }

    /// <summary>Decodes a Session from read-only UTF-8 memory.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded Session.</returns>
    public JmapSession DecodeSessionMemory(ReadOnlyMemory<byte> utf8)
    {
        return Sessions.DecodeSession(Reader.ReadUtf8Memory(utf8));
    }

    /// <summary>Decodes a Session from a UTF-8 stream.</summary>
    /// <param name="stream">The readable stream.</param>
    /// <param name="cancellationToken">A token that can cancel reading.</param>
    /// <returns>A task containing the decoded Session.</returns>
    public Task<JmapSession> DecodeSessionStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        return Task.Run(() => Sessions.DecodeSession(Reader.ReadUtf8Stream(stream, cancellationToken)), cancellationToken);
    }

    /// <summary>Encodes a Session as UTF-8 JSON bytes.</summary>
    /// <param name="session">The Session to encode.</param>
    /// <returns>The encoded bytes.</returns>
    public byte[] EncodeSessionBytes(JmapSession session)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Sessions.EncodeSession(session, writer);
        writer.DisposeWriter();
        return stream.ToArray();
    }

    /// <summary>Encodes a Session to a UTF-8 stream.</summary>
    /// <param name="session">The Session to encode.</param>
    /// <param name="stream">The writable stream.</param>
    /// <param name="cancellationToken">A token that can cancel writing.</param>
    /// <returns>A task representing the write operation.</returns>
    public Task EncodeSessionStreamAsync(JmapSession session, Stream stream, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            JmapJsonWriter writer = new JmapJsonWriter(stream);
            Sessions.EncodeSession(session, writer);
            writer.DisposeWriter();
            cancellationToken.ThrowIfCancellationRequested();
        }, cancellationToken);
    }

    /// <summary>Decodes a Request from UTF-8 bytes.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded Request.</returns>
    public JmapRequest DecodeRequestBytes(ReadOnlySpan<byte> utf8)
    {
        return Protocol.DecodeRequest(Reader.ReadUtf8Span(utf8));
    }

    /// <summary>Decodes a Request from read-only UTF-8 memory.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded Request.</returns>
    public JmapRequest DecodeRequestMemory(ReadOnlyMemory<byte> utf8)
    {
        return Protocol.DecodeRequest(Reader.ReadUtf8Memory(utf8));
    }

    /// <summary>Decodes a Request from a UTF-8 stream.</summary>
    /// <param name="stream">The readable stream.</param>
    /// <param name="cancellationToken">A token that can cancel reading.</param>
    /// <returns>A task containing the decoded Request.</returns>
    public Task<JmapRequest> DecodeRequestStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        return Task.Run(() => Protocol.DecodeRequest(Reader.ReadUtf8Stream(stream, cancellationToken)), cancellationToken);
    }

    /// <summary>Encodes a Request as UTF-8 JSON bytes.</summary>
    /// <param name="request">The Request to encode.</param>
    /// <returns>The encoded bytes.</returns>
    public byte[] EncodeRequestBytes(JmapRequest request)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Protocol.EncodeRequest(request, writer);
        writer.DisposeWriter();
        return stream.ToArray();
    }

    /// <summary>Encodes a Request to a UTF-8 stream.</summary>
    /// <param name="request">The Request to encode.</param>
    /// <param name="stream">The writable stream.</param>
    /// <param name="cancellationToken">A token that can cancel writing.</param>
    /// <returns>A task representing the write operation.</returns>
    public Task EncodeRequestStreamAsync(JmapRequest request, Stream stream, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            JmapJsonWriter writer = new JmapJsonWriter(stream);
            Protocol.EncodeRequest(request, writer);
            writer.DisposeWriter();
            cancellationToken.ThrowIfCancellationRequested();
        }, cancellationToken);
    }

    /// <summary>Decodes a Response from UTF-8 bytes.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded Response.</returns>
    public JmapResponse DecodeResponseBytes(ReadOnlySpan<byte> utf8)
    {
        return Protocol.DecodeResponse(Reader.ReadUtf8Span(utf8));
    }

    /// <summary>Decodes a Response from read-only UTF-8 memory.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded Response.</returns>
    public JmapResponse DecodeResponseMemory(ReadOnlyMemory<byte> utf8)
    {
        return Protocol.DecodeResponse(Reader.ReadUtf8Memory(utf8));
    }

    /// <summary>Decodes a Response from a UTF-8 stream.</summary>
    /// <param name="stream">The readable stream.</param>
    /// <param name="cancellationToken">A token that can cancel reading.</param>
    /// <returns>A task containing the decoded Response.</returns>
    public Task<JmapResponse> DecodeResponseStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        return Task.Run(() => Protocol.DecodeResponse(Reader.ReadUtf8Stream(stream, cancellationToken)), cancellationToken);
    }

    /// <summary>Encodes a Response as UTF-8 JSON bytes.</summary>
    /// <param name="response">The Response to encode.</param>
    /// <returns>The encoded bytes.</returns>
    public byte[] EncodeResponseBytes(JmapResponse response)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Protocol.EncodeResponse(response, writer);
        writer.DisposeWriter();
        return stream.ToArray();
    }

    /// <summary>Encodes a Response to a UTF-8 stream.</summary>
    /// <param name="response">The Response to encode.</param>
    /// <param name="stream">The writable stream.</param>
    /// <param name="cancellationToken">A token that can cancel writing.</param>
    /// <returns>A task representing the write operation.</returns>
    public Task EncodeResponseStreamAsync(JmapResponse response, Stream stream, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            JmapJsonWriter writer = new JmapJsonWriter(stream);
            Protocol.EncodeResponse(response, writer);
            writer.DisposeWriter();
            cancellationToken.ThrowIfCancellationRequested();
        }, cancellationToken);
    }

    /// <summary>Decodes problem details from UTF-8 bytes.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded problem details.</returns>
    public JmapProblem DecodeProblemBytes(ReadOnlySpan<byte> utf8)
    {
        return Protocol.DecodeProblem(Reader.ReadUtf8Span(utf8));
    }

    /// <summary>Decodes problem details from read-only UTF-8 memory.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded problem details.</returns>
    public JmapProblem DecodeProblemMemory(ReadOnlyMemory<byte> utf8)
    {
        return Protocol.DecodeProblem(Reader.ReadUtf8Memory(utf8));
    }

    /// <summary>Decodes problem details from a UTF-8 stream.</summary>
    /// <param name="stream">The readable stream.</param>
    /// <param name="cancellationToken">A token that can cancel reading.</param>
    /// <returns>A task containing the decoded problem details.</returns>
    public Task<JmapProblem> DecodeProblemStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        return Task.Run(() => Protocol.DecodeProblem(Reader.ReadUtf8Stream(stream, cancellationToken)), cancellationToken);
    }

    /// <summary>Encodes problem details as UTF-8 JSON bytes.</summary>
    /// <param name="problem">The problem details to encode.</param>
    /// <returns>The encoded bytes.</returns>
    public byte[] EncodeProblemBytes(JmapProblem problem)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Protocol.EncodeProblem(problem, writer);
        writer.DisposeWriter();
        return stream.ToArray();
    }

    /// <summary>Encodes problem details to a UTF-8 stream.</summary>
    /// <param name="problem">The problem details to encode.</param>
    /// <param name="stream">The writable stream.</param>
    /// <param name="cancellationToken">A token that can cancel writing.</param>
    /// <returns>A task representing the write operation.</returns>
    public Task EncodeProblemStreamAsync(JmapProblem problem, Stream stream, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            JmapJsonWriter writer = new JmapJsonWriter(stream);
            Protocol.EncodeProblem(problem, writer);
            writer.DisposeWriter();
            cancellationToken.ThrowIfCancellationRequested();
        }, cancellationToken);
    }

    /// <summary>Decodes a method error from UTF-8 bytes.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded method error.</returns>
    public JmapMethodError DecodeMethodErrorBytes(ReadOnlySpan<byte> utf8)
    {
        return Protocol.DecodeMethodError(Reader.ReadUtf8Span(utf8));
    }

    /// <summary>Encodes a method error as UTF-8 JSON bytes.</summary>
    /// <param name="error">The method error to encode.</param>
    /// <returns>The encoded bytes.</returns>
    public byte[] EncodeMethodErrorBytes(JmapMethodError error)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Protocol.EncodeMethodError(error, writer);
        writer.DisposeWriter();
        return stream.ToArray();
    }

    /// <summary>Decodes a SetError from UTF-8 bytes.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded SetError.</returns>
    public JmapSetError DecodeSetErrorBytes(ReadOnlySpan<byte> utf8)
    {
        return Protocol.DecodeSetError(Reader.ReadUtf8Span(utf8));
    }

    /// <summary>Encodes a SetError as UTF-8 JSON bytes.</summary>
    /// <param name="error">The SetError to encode.</param>
    /// <returns>The encoded bytes.</returns>
    public byte[] EncodeSetErrorBytes(JmapSetError error)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Protocol.EncodeSetError(error, writer);
        writer.DisposeWriter();
        return stream.ToArray();
    }

    /// <summary>Decodes a StateChange from UTF-8 bytes.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded StateChange.</returns>
    public StateChange DecodeStateChangeBytes(ReadOnlySpan<byte> utf8)
    {
        return Push.DecodeStateChange(Reader.ReadUtf8Span(utf8));
    }

    /// <summary>Decodes a StateChange from read-only UTF-8 memory.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded StateChange.</returns>
    public StateChange DecodeStateChangeMemory(ReadOnlyMemory<byte> utf8)
    {
        return Push.DecodeStateChange(Reader.ReadUtf8Memory(utf8));
    }

    /// <summary>Decodes a StateChange from a UTF-8 stream.</summary>
    /// <param name="stream">The readable stream.</param>
    /// <param name="cancellationToken">A token that can cancel reading.</param>
    /// <returns>A task containing the decoded StateChange.</returns>
    public Task<StateChange> DecodeStateChangeStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        return Task.Run(() => Push.DecodeStateChange(Reader.ReadUtf8Stream(stream, cancellationToken)), cancellationToken);
    }

    /// <summary>Encodes a StateChange as UTF-8 JSON bytes.</summary>
    /// <param name="stateChange">The StateChange to encode.</param>
    /// <returns>The encoded bytes.</returns>
    public byte[] EncodeStateChangeBytes(StateChange stateChange)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Push.EncodeStateChange(stateChange, writer);
        writer.DisposeWriter();
        return stream.ToArray();
    }

    /// <summary>Encodes a StateChange to a UTF-8 stream.</summary>
    /// <param name="stateChange">The StateChange to encode.</param>
    /// <param name="stream">The writable stream.</param>
    /// <param name="cancellationToken">A token that can cancel writing.</param>
    /// <returns>A task representing the write operation.</returns>
    public Task EncodeStateChangeStreamAsync(StateChange stateChange, Stream stream, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            JmapJsonWriter writer = new JmapJsonWriter(stream);
            Push.EncodeStateChange(stateChange, writer);
            writer.DisposeWriter();
            cancellationToken.ThrowIfCancellationRequested();
        }, cancellationToken);
    }

    /// <summary>Encodes common get arguments as a detached JSON object.</summary>
    /// <param name="request">The get arguments.</param>
    /// <returns>The encoded arguments.</returns>
    public JsonElement EncodeStandardGetRequest(JmapGetRequest request)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Standard.EncodeGetRequest(request, writer);
        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    /// <summary>Decodes common get result fields.</summary>
    /// <param name="arguments">The JSON result object.</param>
    /// <returns>The decoded result.</returns>
    public JmapGetResponse DecodeStandardGetResponse(JsonElement arguments)
    {
        return Standard.DecodeGetResponse(arguments);
    }

    /// <summary>Encodes common changes arguments as a detached JSON object.</summary>
    /// <param name="request">The changes arguments.</param>
    /// <returns>The encoded arguments.</returns>
    public JsonElement EncodeStandardChangesRequest(JmapChangesRequest request)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Standard.EncodeChangesRequest(request, writer);
        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    /// <summary>Decodes common changes result fields.</summary>
    /// <param name="arguments">The JSON result object.</param>
    /// <returns>The decoded result.</returns>
    public JmapChangesResponse DecodeStandardChangesResponse(JsonElement arguments)
    {
        return Standard.DecodeChangesResponse(arguments);
    }

    /// <summary>Encodes common set arguments as a detached JSON object.</summary>
    /// <param name="request">The set arguments.</param>
    /// <returns>The encoded arguments.</returns>
    public JsonElement EncodeStandardSetRequest(JmapSetRequest request)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Standard.EncodeSetRequest(request, writer);
        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    /// <summary>Decodes common set result fields.</summary>
    /// <param name="arguments">The JSON result object.</param>
    /// <returns>The decoded result.</returns>
    public JmapSetResponse DecodeStandardSetResponse(JsonElement arguments)
    {
        return Standard.DecodeSetResponse(arguments);
    }

    /// <summary>Encodes common copy arguments as a detached JSON object.</summary>
    /// <param name="request">The copy arguments.</param>
    /// <returns>The encoded arguments.</returns>
    public JsonElement EncodeStandardCopyRequest(JmapCopyRequest request)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Standard.EncodeCopyRequest(request, writer);
        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    /// <summary>Decodes common copy result fields.</summary>
    /// <param name="arguments">The JSON result object.</param>
    /// <returns>The decoded result.</returns>
    public JmapCopyResponse DecodeStandardCopyResponse(JsonElement arguments)
    {
        return Standard.DecodeCopyResponse(arguments);
    }

    /// <summary>Encodes common query arguments as a detached JSON object.</summary>
    /// <param name="request">The query arguments.</param>
    /// <returns>The encoded arguments.</returns>
    public JsonElement EncodeStandardQueryRequest(JmapQueryRequest request)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Standard.EncodeQueryRequest(request, writer);
        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    /// <summary>Decodes common query result fields.</summary>
    /// <param name="arguments">The JSON result object.</param>
    /// <returns>The decoded result.</returns>
    public JmapQueryResponse DecodeStandardQueryResponse(JsonElement arguments)
    {
        return Standard.DecodeQueryResponse(arguments);
    }

    /// <summary>Encodes common queryChanges arguments as a detached JSON object.</summary>
    /// <param name="request">The queryChanges arguments.</param>
    /// <returns>The encoded arguments.</returns>
    public JsonElement EncodeStandardQueryChangesRequest(JmapQueryChangesRequest request)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Standard.EncodeQueryChangesRequest(request, writer);
        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    /// <summary>Decodes common queryChanges result fields.</summary>
    /// <param name="arguments">The JSON result object.</param>
    /// <returns>The decoded result.</returns>
    public JmapQueryChangesResponse DecodeStandardQueryChangesResponse(JsonElement arguments)
    {
        return Standard.DecodeQueryChangesResponse(arguments);
    }

    /// <summary>Decodes a push verification callback from UTF-8 bytes.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded callback.</returns>
    public PushVerification DecodePushVerificationBytes(ReadOnlySpan<byte> utf8)
    {
        return Push.DecodePushVerification(Reader.ReadUtf8Span(utf8));
    }

    /// <summary>Encodes a push verification callback as UTF-8 JSON bytes.</summary>
    /// <param name="verification">The callback to encode.</param>
    /// <returns>The encoded bytes.</returns>
    public byte[] EncodePushVerificationBytes(PushVerification verification)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Push.EncodePushVerification(verification, writer);
        writer.DisposeWriter();
        return stream.ToArray();
    }

    /// <summary>Decodes an Email GroupedAddresses header value.</summary>
    /// <param name="value">The JSON header value.</param>
    /// <returns>The decoded address groups.</returns>
    public List<EmailAddressGroup> DecodeEmailAddressGroups(JsonElement value)
    {
        return Email.DecodeAddressGroups(value, "");
    }

    /// <summary>Classifies a request-level problem type URI.</summary>
    /// <param name="type">The wire type URI.</param>
    /// <returns>The problem classification.</returns>
    public JmapProblemKind ClassifyProblemType(string type)
    {
        return Protocol.ClassifyProblemType(type);
    }

    /// <summary>Classifies a method error token.</summary>
    /// <param name="type">The wire error token.</param>
    /// <returns>The method error classification.</returns>
    public JmapMethodErrorKind ClassifyMethodErrorType(string type)
    {
        return Protocol.ClassifyMethodErrorType(type);
    }

    /// <summary>Classifies a SetError token.</summary>
    /// <param name="type">The wire error token.</param>
    /// <returns>The SetError classification.</returns>
    public JmapSetErrorKind ClassifySetErrorType(string type)
    {
        return Protocol.ClassifySetErrorType(type);
    }

    /// <summary>Classifies a filter operator token.</summary>
    /// <param name="value">The wire operator token.</param>
    /// <returns>The filter operator classification.</returns>
    public JmapFilterOperator ClassifyFilterOperator(string value)
    {
        return Standard.DecodeFilterOperator(value, "");
    }

    /// <summary>Classifies an Email header-form token.</summary>
    /// <param name="value">The wire header-form token.</param>
    /// <returns>The header-form classification.</returns>
    public JmapHeaderForm ClassifyHeaderForm(string value)
    {
        return Email.ClassifyHeaderForm(value);
    }

    /// <summary>Classifies an EventSource event name.</summary>
    /// <param name="name">The wire event name.</param>
    /// <returns>The event classification.</returns>
    public JmapEventKind ClassifyEventName(string name)
    {
        return Push.ClassifyEventName(name);
    }

    /// <summary>Classifies an EmailSubmission undo-status token.</summary>
    /// <param name="value">The wire status token.</param>
    /// <returns>The undo-status classification.</returns>
    public JmapSubmissionUndoStatus ClassifySubmissionUndoStatus(string value)
    {
        return Submission.DecodeUndoStatus(value, "");
    }
}
