using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Defines low-level conversion between JMAP wire representations and public protocol classifications.
/// </summary>
public interface IJmapCodec
{
    /// <summary>Decodes a UTF-8 JMAP Session object.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded Session.</returns>
    JmapSession DecodeSessionBytes(ReadOnlySpan<byte> utf8);

    /// <summary>Decodes a JMAP Session from read-only UTF-8 memory.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded Session.</returns>
    JmapSession DecodeSessionMemory(ReadOnlyMemory<byte> utf8);

    /// <summary>Decodes a JMAP Session from a UTF-8 stream.</summary>
    /// <param name="stream">The readable UTF-8 JSON stream.</param>
    /// <param name="cancellationToken">A token that can cancel reading.</param>
    /// <returns>A task containing the decoded Session.</returns>
    Task<JmapSession> DecodeSessionStreamAsync(Stream stream, CancellationToken cancellationToken);

    /// <summary>Encodes a JMAP Session object as UTF-8 JSON.</summary>
    /// <param name="session">The Session to encode.</param>
    /// <returns>The encoded UTF-8 JSON.</returns>
    byte[] EncodeSessionBytes(JmapSession session);

    /// <summary>Encodes a JMAP Session to a UTF-8 stream.</summary>
    /// <param name="session">The Session to encode.</param>
    /// <param name="stream">The writable output stream.</param>
    /// <param name="cancellationToken">A token that can cancel writing.</param>
    /// <returns>A task representing the write operation.</returns>
    Task EncodeSessionStreamAsync(JmapSession session, Stream stream, CancellationToken cancellationToken);

    /// <summary>Decodes a UTF-8 JMAP Request object.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded request.</returns>
    JmapRequest DecodeRequestBytes(ReadOnlySpan<byte> utf8);

    /// <summary>Decodes a JMAP Request from read-only UTF-8 memory.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded request.</returns>
    JmapRequest DecodeRequestMemory(ReadOnlyMemory<byte> utf8);

    /// <summary>Decodes a JMAP Request from a UTF-8 stream.</summary>
    /// <param name="stream">The readable UTF-8 JSON stream.</param>
    /// <param name="cancellationToken">A token that can cancel reading.</param>
    /// <returns>A task containing the decoded request.</returns>
    Task<JmapRequest> DecodeRequestStreamAsync(Stream stream, CancellationToken cancellationToken);

    /// <summary>Encodes a JMAP Request object as UTF-8 JSON.</summary>
    /// <param name="request">The request to encode.</param>
    /// <returns>The encoded UTF-8 JSON.</returns>
    byte[] EncodeRequestBytes(JmapRequest request);

    /// <summary>Encodes a JMAP Request to a UTF-8 stream.</summary>
    /// <param name="request">The request to encode.</param>
    /// <param name="stream">The writable output stream.</param>
    /// <param name="cancellationToken">A token that can cancel writing.</param>
    /// <returns>A task representing the write operation.</returns>
    Task EncodeRequestStreamAsync(JmapRequest request, Stream stream, CancellationToken cancellationToken);

    /// <summary>Decodes a UTF-8 JMAP Response object.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded response.</returns>
    JmapResponse DecodeResponseBytes(ReadOnlySpan<byte> utf8);

    /// <summary>Decodes a JMAP Response from read-only UTF-8 memory.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded response.</returns>
    JmapResponse DecodeResponseMemory(ReadOnlyMemory<byte> utf8);

    /// <summary>Decodes a JMAP Response from a UTF-8 stream.</summary>
    /// <param name="stream">The readable UTF-8 JSON stream.</param>
    /// <param name="cancellationToken">A token that can cancel reading.</param>
    /// <returns>A task containing the decoded response.</returns>
    Task<JmapResponse> DecodeResponseStreamAsync(Stream stream, CancellationToken cancellationToken);

    /// <summary>Encodes a JMAP Response object as UTF-8 JSON.</summary>
    /// <param name="response">The response to encode.</param>
    /// <returns>The encoded UTF-8 JSON.</returns>
    byte[] EncodeResponseBytes(JmapResponse response);

    /// <summary>Encodes a JMAP Response to a UTF-8 stream.</summary>
    /// <param name="response">The response to encode.</param>
    /// <param name="stream">The writable output stream.</param>
    /// <param name="cancellationToken">A token that can cancel writing.</param>
    /// <returns>A task representing the write operation.</returns>
    Task EncodeResponseStreamAsync(JmapResponse response, Stream stream, CancellationToken cancellationToken);

    /// <summary>Decodes UTF-8 JMAP problem details.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded problem details.</returns>
    JmapProblem DecodeProblemBytes(ReadOnlySpan<byte> utf8);

    /// <summary>Decodes problem details from read-only UTF-8 memory.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded problem details.</returns>
    JmapProblem DecodeProblemMemory(ReadOnlyMemory<byte> utf8);

    /// <summary>Decodes problem details from a UTF-8 stream.</summary>
    /// <param name="stream">The readable UTF-8 JSON stream.</param>
    /// <param name="cancellationToken">A token that can cancel reading.</param>
    /// <returns>A task containing the decoded problem details.</returns>
    Task<JmapProblem> DecodeProblemStreamAsync(Stream stream, CancellationToken cancellationToken);

    /// <summary>Encodes JMAP problem details as UTF-8 JSON.</summary>
    /// <param name="problem">The problem details to encode.</param>
    /// <returns>The encoded UTF-8 JSON.</returns>
    byte[] EncodeProblemBytes(JmapProblem problem);

    /// <summary>Encodes problem details to a UTF-8 stream.</summary>
    /// <param name="problem">The problem details to encode.</param>
    /// <param name="stream">The writable output stream.</param>
    /// <param name="cancellationToken">A token that can cancel writing.</param>
    /// <returns>A task representing the write operation.</returns>
    Task EncodeProblemStreamAsync(JmapProblem problem, Stream stream, CancellationToken cancellationToken);

    /// <summary>Decodes a UTF-8 JMAP method error object.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded method error.</returns>
    JmapMethodError DecodeMethodErrorBytes(ReadOnlySpan<byte> utf8);

    /// <summary>Encodes a JMAP method error as UTF-8 JSON.</summary>
    /// <param name="error">The method error to encode.</param>
    /// <returns>The encoded UTF-8 JSON.</returns>
    byte[] EncodeMethodErrorBytes(JmapMethodError error);

    /// <summary>Decodes a UTF-8 JMAP SetError object.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded SetError.</returns>
    JmapSetError DecodeSetErrorBytes(ReadOnlySpan<byte> utf8);

    /// <summary>Encodes a JMAP SetError as UTF-8 JSON.</summary>
    /// <param name="error">The SetError to encode.</param>
    /// <returns>The encoded UTF-8 JSON.</returns>
    byte[] EncodeSetErrorBytes(JmapSetError error);

    /// <summary>Decodes a UTF-8 StateChange object.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded state change.</returns>
    StateChange DecodeStateChangeBytes(ReadOnlySpan<byte> utf8);

    /// <summary>Decodes a StateChange object from read-only UTF-8 memory.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded state change.</returns>
    StateChange DecodeStateChangeMemory(ReadOnlyMemory<byte> utf8);

    /// <summary>Decodes a StateChange object from a UTF-8 stream.</summary>
    /// <param name="stream">The readable UTF-8 JSON stream.</param>
    /// <param name="cancellationToken">A token that can cancel reading.</param>
    /// <returns>A task containing the decoded state change.</returns>
    Task<StateChange> DecodeStateChangeStreamAsync(Stream stream, CancellationToken cancellationToken);

    /// <summary>Encodes a StateChange object as UTF-8 JSON.</summary>
    /// <param name="stateChange">The state change to encode.</param>
    /// <returns>The encoded UTF-8 JSON.</returns>
    byte[] EncodeStateChangeBytes(StateChange stateChange);

    /// <summary>Encodes a StateChange object to a UTF-8 stream.</summary>
    /// <param name="stateChange">The state change to encode.</param>
    /// <param name="stream">The writable output stream.</param>
    /// <param name="cancellationToken">A token that can cancel writing.</param>
    /// <returns>A task representing the write operation.</returns>
    Task EncodeStateChangeStreamAsync(StateChange stateChange, Stream stream, CancellationToken cancellationToken);

    /// <summary>Encodes common get arguments as a JSON object.</summary>
    /// <param name="request">The common get arguments.</param>
    /// <returns>The encoded arguments object.</returns>
    JsonElement EncodeStandardGetRequest(JmapGetRequest request);

    /// <summary>Decodes common get result fields from a JSON object.</summary>
    /// <param name="arguments">The result arguments object.</param>
    /// <returns>The decoded common get result.</returns>
    JmapGetResponse DecodeStandardGetResponse(JsonElement arguments);

    /// <summary>Encodes common changes arguments as a JSON object.</summary>
    /// <param name="request">The common changes arguments.</param>
    /// <returns>The encoded arguments object.</returns>
    JsonElement EncodeStandardChangesRequest(JmapChangesRequest request);

    /// <summary>Decodes common changes result fields from a JSON object.</summary>
    /// <param name="arguments">The result arguments object.</param>
    /// <returns>The decoded common changes result.</returns>
    JmapChangesResponse DecodeStandardChangesResponse(JsonElement arguments);

    /// <summary>Encodes common set arguments as a JSON object.</summary>
    /// <param name="request">The common set arguments.</param>
    /// <returns>The encoded arguments object.</returns>
    JsonElement EncodeStandardSetRequest(JmapSetRequest request);

    /// <summary>Decodes common set result fields from a JSON object.</summary>
    /// <param name="arguments">The result arguments object.</param>
    /// <returns>The decoded common set result.</returns>
    JmapSetResponse DecodeStandardSetResponse(JsonElement arguments);

    /// <summary>Encodes common copy arguments as a JSON object.</summary>
    /// <param name="request">The common copy arguments.</param>
    /// <returns>The encoded arguments object.</returns>
    JsonElement EncodeStandardCopyRequest(JmapCopyRequest request);

    /// <summary>Decodes common copy result fields from a JSON object.</summary>
    /// <param name="arguments">The result arguments object.</param>
    /// <returns>The decoded common copy result.</returns>
    JmapCopyResponse DecodeStandardCopyResponse(JsonElement arguments);

    /// <summary>Encodes common query arguments as a JSON object.</summary>
    /// <param name="request">The common query arguments.</param>
    /// <returns>The encoded arguments object.</returns>
    JsonElement EncodeStandardQueryRequest(JmapQueryRequest request);

    /// <summary>Decodes common query result fields from a JSON object.</summary>
    /// <param name="arguments">The result arguments object.</param>
    /// <returns>The decoded common query result.</returns>
    JmapQueryResponse DecodeStandardQueryResponse(JsonElement arguments);

    /// <summary>Encodes common queryChanges arguments as a JSON object.</summary>
    /// <param name="request">The common queryChanges arguments.</param>
    /// <returns>The encoded arguments object.</returns>
    JsonElement EncodeStandardQueryChangesRequest(JmapQueryChangesRequest request);

    /// <summary>Decodes common queryChanges result fields from a JSON object.</summary>
    /// <param name="arguments">The result arguments object.</param>
    /// <returns>The decoded common queryChanges result.</returns>
    JmapQueryChangesResponse DecodeStandardQueryChangesResponse(JsonElement arguments);

    /// <summary>Decodes a UTF-8 push verification callback object.</summary>
    /// <param name="utf8">The UTF-8 JSON input.</param>
    /// <returns>The decoded push verification data.</returns>
    PushVerification DecodePushVerificationBytes(ReadOnlySpan<byte> utf8);

    /// <summary>Encodes a push verification callback object as UTF-8 JSON.</summary>
    /// <param name="verification">The verification object to encode.</param>
    /// <returns>The encoded UTF-8 JSON.</returns>
    byte[] EncodePushVerificationBytes(PushVerification verification);

    /// <summary>Decodes a GroupedAddresses header-form value.</summary>
    /// <param name="value">The dynamic header property value to decode.</param>
    /// <returns>The address groups in wire order.</returns>
    List<EmailAddressGroup> DecodeEmailAddressGroups(JsonElement value);

    /// <summary>Classifies a request-level problem type URI.</summary>
    /// <param name="type">The wire problem type URI.</param>
    /// <returns>The recognized problem classification, or <see cref="JmapProblemKind.Unknown"/>.</returns>
    JmapProblemKind ClassifyProblemType(string type);

    /// <summary>Classifies a method-level error type.</summary>
    /// <param name="type">The wire method error token.</param>
    /// <returns>The recognized method error classification, or <see cref="JmapMethodErrorKind.Unknown"/>.</returns>
    JmapMethodErrorKind ClassifyMethodErrorType(string type);

    /// <summary>Classifies a per-record set error type.</summary>
    /// <param name="type">The wire set error token.</param>
    /// <returns>The recognized set error classification, or <see cref="JmapSetErrorKind.Unknown"/>.</returns>
    JmapSetErrorKind ClassifySetErrorType(string type);

    /// <summary>Classifies a filter operator token.</summary>
    /// <param name="value">The wire filter operator token.</param>
    /// <returns>The filter operator classification.</returns>
    JmapFilterOperator ClassifyFilterOperator(string value);

    /// <summary>Classifies an Email header-form token.</summary>
    /// <param name="value">The wire header-form token.</param>
    /// <returns>The Email header-form classification.</returns>
    JmapHeaderForm ClassifyHeaderForm(string value);

    /// <summary>Classifies an EventSource event name.</summary>
    /// <param name="name">The EventSource event name.</param>
    /// <returns>The recognized event classification, or <see cref="JmapEventKind.Unknown"/>.</returns>
    JmapEventKind ClassifyEventName(string name);

    /// <summary>Classifies an EmailSubmission undo-status token.</summary>
    /// <param name="value">The wire undo-status token.</param>
    /// <returns>The submission undo-status classification.</returns>
    JmapSubmissionUndoStatus ClassifySubmissionUndoStatus(string value);
}
