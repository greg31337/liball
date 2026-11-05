using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace htmlgui;

internal class BridgeCodec
{
    internal const string RequestMember = "request";
    internal const string ResponseMember = "response";
    internal const string OperationMember = "operation";
    internal const string ArgumentsMember = "arguments";
    internal const string TimeoutMillisecondsMember = "timeoutMilliseconds";
    internal const string ValueMember = "value";
    internal const string RetryableMember = "retryable";

    internal string SerializeBridgeEnvelope(UiBridgeEnvelope? envelope, int maximumMessageLength)
    {
        BridgeValidationResult validation = new BridgeValidator().ValidateBridgePayload(envelope, maximumMessageLength);
        if (!validation.Success)
        {
            throw new ArgumentException(validation.Message, "envelope");
        }

        UiBridgeEnvelope source = envelope ?? new UiBridgeEnvelope();
        using (MemoryStream stream = new MemoryStream())
        {
            using (Utf8JsonWriter writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteNumber(BridgeProtocolNames.Version, source.Version);
                writer.WriteString(BridgeProtocolNames.Session, source.Session);
                writer.WriteNumber(BridgeProtocolNames.Sequence, source.Sequence);
                if (source.CorrelationId != null)
                {
                    writer.WriteString(BridgeProtocolNames.CorrelationId, source.CorrelationId);
                }

                writer.WriteString(BridgeProtocolNames.Origin, source.Origin);
                writer.WriteString(BridgeProtocolNames.Kind, GetBridgeKindToken(source.Kind));
                if (source.Payload != null)
                {
                    writer.WritePropertyName(BridgeProtocolNames.Payload);
                    ValueJsonCodec.WriteNode(writer, source.Payload);
                }

                if (source.Request != null)
                {
                    WriteBridgeRequest(writer, source.Request);
                }

                if (source.Response != null)
                {
                    WriteBridgeResponse(writer, source.Response);
                }

                if (source.Error != null)
                {
                    writer.WritePropertyName(BridgeProtocolNames.Error);
                    WriteBridgeError(writer, source.Error);
                }

                writer.WriteEndObject();
            }

            byte[] bytes = stream.ToArray();
            if (bytes.Length > maximumMessageLength)
            {
                throw new ArgumentException("The encoded bridge message exceeds the maximum length.", "envelope");
            }

            return Encoding.UTF8.GetString(bytes);
        }
    }

    internal UiBridgeEnvelope DeserializeBridgeEnvelope(string? json, int maximumMessageLength)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("A bridge JSON message is required.", "json");
        }

        if (maximumMessageLength < 1)
        {
            throw new ArgumentOutOfRangeException("maximumMessageLength", "The maximum bridge message length must be positive.");
        }

        string source = json ?? string.Empty;
        if (Encoding.UTF8.GetByteCount(source) > maximumMessageLength)
        {
            throw new ArgumentException("The encoded bridge message exceeds the maximum length.", "json");
        }

        JsonDocumentOptions documentOptions = new JsonDocumentOptions();
        documentOptions.MaxDepth = 72;
        using (JsonDocument document = JsonDocument.Parse(source, documentOptions))
        {
            JsonElement root = document.RootElement;
            RequireBridgeObject(root, "A bridge envelope must be a JSON object.");
            UiBridgeEnvelope envelope = new UiBridgeEnvelope();
            bool hasVersion = false;
            bool hasSession = false;
            bool hasSequence = false;
            bool hasCorrelation = false;
            bool hasOrigin = false;
            bool hasKind = false;
            bool hasPayload = false;
            bool hasRequest = false;
            bool hasResponse = false;
            bool hasError = false;
            BridgeValueValidationState state = new BridgeValueValidationState(maximumMessageLength);
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (property.Name == BridgeProtocolNames.Version)
                {
                    if (hasVersion || property.Value.ValueKind != JsonValueKind.Number)
                    {
                        throw new JsonException("The bridge version must be one integer.");
                    }

                    envelope.Version = property.Value.GetInt32();
                    hasVersion = true;
                }
                else if (property.Name == BridgeProtocolNames.Session)
                {
                    if (hasSession)
                    {
                        throw new JsonException("The bridge session cannot be repeated.");
                    }

                    envelope.Session = ReadRequiredBridgeString(property.Value, state.MaximumStringLength, "The bridge session must be a bounded string.");
                    hasSession = true;
                }
                else if (property.Name == BridgeProtocolNames.Sequence)
                {
                    if (hasSequence || property.Value.ValueKind != JsonValueKind.Number)
                    {
                        throw new JsonException("The bridge sequence must be one integer.");
                    }

                    envelope.Sequence = property.Value.GetInt32();
                    hasSequence = true;
                }
                else if (property.Name == BridgeProtocolNames.CorrelationId)
                {
                    if (hasCorrelation)
                    {
                        throw new JsonException("The bridge correlation identifier cannot be repeated.");
                    }

                    envelope.CorrelationId = ReadOptionalBridgeString(property.Value, state.MaximumStringLength, "The bridge correlation identifier must be a bounded string or null.");
                    hasCorrelation = true;
                }
                else if (property.Name == BridgeProtocolNames.Origin)
                {
                    if (hasOrigin)
                    {
                        throw new JsonException("The bridge origin cannot be repeated.");
                    }

                    envelope.Origin = ReadRequiredBridgeString(property.Value, state.MaximumStringLength, "The bridge origin must be a bounded string.");
                    hasOrigin = true;
                }
                else if (property.Name == BridgeProtocolNames.Kind)
                {
                    if (hasKind || property.Value.ValueKind != JsonValueKind.String)
                    {
                        throw new JsonException("The bridge kind must be one string.");
                    }

                    envelope.Kind = ParseBridgeKindToken(property.Value.GetString());
                    hasKind = true;
                }
                else if (property.Name == BridgeProtocolNames.Payload)
                {
                    if (hasPayload)
                    {
                        throw new JsonException("The bridge payload cannot be repeated.");
                    }

                    envelope.Payload = ReadBridgeValue(property.Value, state);
                    hasPayload = true;
                }
                else if (property.Name == RequestMember)
                {
                    if (hasRequest)
                    {
                        throw new JsonException("The bridge request cannot be repeated.");
                    }

                    envelope.Request = ReadBridgeRequest(property.Value, state);
                    hasRequest = true;
                }
                else if (property.Name == ResponseMember)
                {
                    if (hasResponse)
                    {
                        throw new JsonException("The bridge response cannot be repeated.");
                    }

                    envelope.Response = ReadBridgeResponse(property.Value, state);
                    hasResponse = true;
                }
                else if (property.Name == BridgeProtocolNames.Error)
                {
                    if (hasError)
                    {
                        throw new JsonException("The bridge error cannot be repeated.");
                    }

                    envelope.Error = ReadBridgeError(property.Value, state.MaximumStringLength);
                    hasError = true;
                }
                else
                {
                    throw new JsonException("The bridge envelope contains an unknown member.");
                }
            }

            if (!hasVersion || !hasSession || !hasSequence || !hasOrigin || !hasKind)
            {
                throw new JsonException("The bridge envelope is missing a required member.");
            }

            BridgeValidationResult validation = new BridgeValidator().ValidateBridgePayload(envelope, maximumMessageLength);
            if (!validation.Success)
            {
                throw new JsonException(validation.Message);
            }

            return envelope;
        }
    }

    internal void WriteBridgeRequest(Utf8JsonWriter writer, UiBridgeRequest request)
    {
        writer.WriteStartObject(RequestMember);
        writer.WriteString(BridgeProtocolNames.CorrelationId, request.CorrelationId);
        writer.WriteString(OperationMember, request.Operation);
        WriteBridgeArguments(writer, request.Arguments);
        writer.WriteNumber(TimeoutMillisecondsMember, request.TimeoutMilliseconds);
        writer.WriteEndObject();
    }

    internal void WriteBridgeArguments(Utf8JsonWriter writer, UiValueEntry[] arguments)
    {
        writer.WriteStartObject(ArgumentsMember);
        UiValueEntry[] sorted = ValueJsonCodec.SortEntries(arguments);
        int index = 0;
        while (index < sorted.Length)
        {
            writer.WritePropertyName(sorted[index].Key);
            ValueJsonCodec.WriteNode(writer, sorted[index].Value);
            index++;
        }

        writer.WriteEndObject();
    }

    internal void WriteBridgeResponse(Utf8JsonWriter writer, UiBridgeResponse response)
    {
        writer.WriteStartObject(ResponseMember);
        writer.WriteString(BridgeProtocolNames.CorrelationId, response.CorrelationId);
        writer.WriteBoolean(BridgeProtocolNames.Success, response.Success);
        if (response.Value != null)
        {
            writer.WritePropertyName(ValueMember);
            ValueJsonCodec.WriteNode(writer, response.Value);
        }

        if (response.Error != null)
        {
            writer.WritePropertyName(BridgeProtocolNames.Error);
            WriteBridgeError(writer, response.Error);
        }

        writer.WriteEndObject();
    }

    internal void WriteBridgeError(Utf8JsonWriter writer, UiBridgeError error)
    {
        writer.WriteStartObject();
        writer.WriteString(BridgeProtocolNames.ErrorCode, error.Code);
        writer.WriteString(BridgeProtocolNames.ErrorMessage, error.Message);
        if (error.CorrelationId != null)
        {
            writer.WriteString(BridgeProtocolNames.CorrelationId, error.CorrelationId);
        }

        writer.WriteBoolean(RetryableMember, error.IsRetryable);
        writer.WriteEndObject();
    }

    internal UiBridgeRequest ReadBridgeRequest(JsonElement element, BridgeValueValidationState state)
    {
        RequireBridgeObject(element, "A bridge request must be a JSON object.");
        UiBridgeRequest request = new UiBridgeRequest();
        bool hasCorrelation = false;
        bool hasOperation = false;
        bool hasArguments = false;
        bool hasTimeout = false;
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.Name == BridgeProtocolNames.CorrelationId)
            {
                if (hasCorrelation)
                {
                    throw new JsonException("The request correlation identifier cannot be repeated.");
                }

                request.CorrelationId = ReadRequiredBridgeString(property.Value, state.MaximumStringLength, "The request correlation identifier must be a bounded string.");
                hasCorrelation = true;
            }
            else if (property.Name == OperationMember)
            {
                if (hasOperation)
                {
                    throw new JsonException("The request operation cannot be repeated.");
                }

                request.Operation = ReadRequiredBridgeString(property.Value, state.MaximumStringLength, "The request operation must be a bounded string.");
                hasOperation = true;
            }
            else if (property.Name == ArgumentsMember)
            {
                if (hasArguments)
                {
                    throw new JsonException("The request arguments cannot be repeated.");
                }

                request.Arguments = ReadBridgeArguments(property.Value, state);
                hasArguments = true;
            }
            else if (property.Name == TimeoutMillisecondsMember)
            {
                if (hasTimeout || property.Value.ValueKind != JsonValueKind.Number)
                {
                    throw new JsonException("The request timeout must be one integer.");
                }

                request.TimeoutMilliseconds = property.Value.GetInt32();
                hasTimeout = true;
            }
            else
            {
                throw new JsonException("The bridge request contains an unknown member.");
            }
        }

        if (!hasCorrelation || !hasOperation || !hasArguments || !hasTimeout)
        {
            throw new JsonException("The bridge request is missing a required member.");
        }

        return request;
    }

    internal UiValueEntry[] ReadBridgeArguments(JsonElement element, BridgeValueValidationState state)
    {
        RequireBridgeObject(element, "Bridge request arguments must be a JSON object.");
        List<UiValueEntry> arguments = new List<UiValueEntry>();
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.Name.Length > state.MaximumStringLength)
            {
                throw new JsonException("A bridge argument key exceeds the maximum length.");
            }

            int index = 0;
            while (index < arguments.Count)
            {
                if (arguments[index].Key == property.Name)
                {
                    throw new JsonException("Bridge request argument keys must be unique.");
                }

                index++;
            }

            if (!new BridgeValidator().AddBridgeCollectionCount(1, state))
            {
                throw new JsonException("Bridge request arguments exceed the maximum count.");
            }

            ValueJsonCodec.AddCollectionItemsForJson(1, state.JsonState);

            arguments.Add(new UiValueEntry { Key = property.Name, Value = ReadBridgeValue(property.Value, state) });
        }

        return arguments.ToArray();
    }

    internal UiBridgeResponse ReadBridgeResponse(JsonElement element, BridgeValueValidationState state)
    {
        RequireBridgeObject(element, "A bridge response must be a JSON object.");
        UiBridgeResponse response = new UiBridgeResponse();
        bool hasCorrelation = false;
        bool hasSuccess = false;
        bool hasValue = false;
        bool hasError = false;
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.Name == BridgeProtocolNames.CorrelationId)
            {
                if (hasCorrelation)
                {
                    throw new JsonException("The response correlation identifier cannot be repeated.");
                }

                response.CorrelationId = ReadRequiredBridgeString(property.Value, state.MaximumStringLength, "The response correlation identifier must be a bounded string.");
                hasCorrelation = true;
            }
            else if (property.Name == BridgeProtocolNames.Success)
            {
                if (hasSuccess || (property.Value.ValueKind != JsonValueKind.True && property.Value.ValueKind != JsonValueKind.False))
                {
                    throw new JsonException("The response success member must be one Boolean.");
                }

                response.Success = property.Value.GetBoolean();
                hasSuccess = true;
            }
            else if (property.Name == ValueMember)
            {
                if (hasValue)
                {
                    throw new JsonException("The response value cannot be repeated.");
                }

                response.Value = ReadBridgeValue(property.Value, state);
                hasValue = true;
            }
            else if (property.Name == BridgeProtocolNames.Error)
            {
                if (hasError)
                {
                    throw new JsonException("The response error cannot be repeated.");
                }

                response.Error = ReadBridgeError(property.Value, state.MaximumStringLength);
                hasError = true;
            }
            else
            {
                throw new JsonException("The bridge response contains an unknown member.");
            }
        }

        if (!hasCorrelation || !hasSuccess)
        {
            throw new JsonException("The bridge response is missing a required member.");
        }

        return response;
    }

    internal UiBridgeError ReadBridgeError(JsonElement element, int maximumStringLength)
    {
        RequireBridgeObject(element, "A bridge error must be a JSON object.");
        UiBridgeError error = new UiBridgeError();
        bool hasCode = false;
        bool hasMessage = false;
        bool hasCorrelation = false;
        bool hasRetryable = false;
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.Name == BridgeProtocolNames.ErrorCode)
            {
                if (hasCode)
                {
                    throw new JsonException("The bridge error code cannot be repeated.");
                }

                error.Code = ReadRequiredBridgeString(property.Value, maximumStringLength, "The bridge error code must be a bounded string.");
                hasCode = true;
            }
            else if (property.Name == BridgeProtocolNames.ErrorMessage)
            {
                if (hasMessage)
                {
                    throw new JsonException("The bridge error message cannot be repeated.");
                }

                error.Message = ReadRequiredBridgeString(property.Value, maximumStringLength, "The bridge error message must be a bounded string.");
                hasMessage = true;
            }
            else if (property.Name == BridgeProtocolNames.CorrelationId)
            {
                if (hasCorrelation)
                {
                    throw new JsonException("The bridge error correlation identifier cannot be repeated.");
                }

                error.CorrelationId = ReadOptionalBridgeString(property.Value, maximumStringLength, "The bridge error correlation identifier must be a bounded string or null.");
                hasCorrelation = true;
            }
            else if (property.Name == RetryableMember)
            {
                if (hasRetryable || (property.Value.ValueKind != JsonValueKind.True && property.Value.ValueKind != JsonValueKind.False))
                {
                    throw new JsonException("The bridge retryable member must be one Boolean.");
                }

                error.IsRetryable = property.Value.GetBoolean();
                hasRetryable = true;
            }
            else
            {
                throw new JsonException("The bridge error contains an unknown member.");
            }
        }

        if (!hasCode || !hasMessage || !hasRetryable)
        {
            throw new JsonException("The bridge error is missing a required member.");
        }

        return error;
    }

    internal UiValue ReadBridgeValue(JsonElement element, BridgeValueValidationState state)
    {
        return ValueJsonCodec.ReadNode(element, 0, state.JsonState);
    }

    internal string ReadRequiredBridgeString(JsonElement element, int maximumLength, string message)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new JsonException(message);
        }

        string? value = element.GetString();
        string result = value ?? string.Empty;
        if (result.Length > maximumLength)
        {
            throw new JsonException(message);
        }

        return result;
    }

    internal string? ReadOptionalBridgeString(JsonElement element, int maximumLength, string message)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return ReadRequiredBridgeString(element, maximumLength, message);
    }

    internal void RequireBridgeObject(JsonElement element, string message)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException(message);
        }
    }

    internal string GetBridgeKindToken(UiBridgeMessageKind kind)
    {
        switch (kind)
        {
            case UiBridgeMessageKind.Initialize:
                return "initialize";
            case UiBridgeMessageKind.Ready:
                return "ready";
            case UiBridgeMessageKind.RenderSnapshot:
                return "renderSnapshot";
            case UiBridgeMessageKind.DomPatch:
                return "domPatch";
            case UiBridgeMessageKind.Event:
                return "event";
            case UiBridgeMessageKind.Command:
                return "command";
            case UiBridgeMessageKind.StateMutation:
                return "stateMutation";
            case UiBridgeMessageKind.Validation:
                return "validation";
            case UiBridgeMessageKind.Navigation:
                return "navigation";
            case UiBridgeMessageKind.Focus:
                return "focus";
            case UiBridgeMessageKind.Measurement:
                return "measurement";
            case UiBridgeMessageKind.VirtualizationRange:
                return "virtualizationRange";
            case UiBridgeMessageKind.VirtualizationResult:
                return "virtualizationResult";
            case UiBridgeMessageKind.Lifecycle:
                return "lifecycle";
            case UiBridgeMessageKind.Log:
                return "log";
            case UiBridgeMessageKind.Diagnostic:
                return "diagnostic";
            case UiBridgeMessageKind.HostCall:
                return "hostCall";
            case UiBridgeMessageKind.HostResult:
                return "hostResult";
            case UiBridgeMessageKind.Request:
                return "request";
            case UiBridgeMessageKind.Response:
                return "response";
            case UiBridgeMessageKind.Error:
                return "error";
            case UiBridgeMessageKind.Cancel:
                return "cancel";
            case UiBridgeMessageKind.Acknowledgement:
                return "acknowledgement";
            case UiBridgeMessageKind.Heartbeat:
                return "heartbeat";
            case UiBridgeMessageKind.Shutdown:
                return "shutdown";
            case UiBridgeMessageKind.Chunk:
                return "chunk";
            default:
                throw new ArgumentOutOfRangeException("kind", "The bridge message kind is invalid.");
        }
    }

    internal UiBridgeMessageKind ParseBridgeKindToken(string? token)
    {
        switch (token)
        {
            case "initialize":
                return UiBridgeMessageKind.Initialize;
            case "ready":
                return UiBridgeMessageKind.Ready;
            case "renderSnapshot":
                return UiBridgeMessageKind.RenderSnapshot;
            case "domPatch":
                return UiBridgeMessageKind.DomPatch;
            case "event":
                return UiBridgeMessageKind.Event;
            case "command":
                return UiBridgeMessageKind.Command;
            case "stateMutation":
                return UiBridgeMessageKind.StateMutation;
            case "validation":
                return UiBridgeMessageKind.Validation;
            case "navigation":
                return UiBridgeMessageKind.Navigation;
            case "focus":
                return UiBridgeMessageKind.Focus;
            case "measurement":
                return UiBridgeMessageKind.Measurement;
            case "virtualizationRange":
                return UiBridgeMessageKind.VirtualizationRange;
            case "virtualizationResult":
                return UiBridgeMessageKind.VirtualizationResult;
            case "lifecycle":
                return UiBridgeMessageKind.Lifecycle;
            case "log":
                return UiBridgeMessageKind.Log;
            case "diagnostic":
                return UiBridgeMessageKind.Diagnostic;
            case "hostCall":
                return UiBridgeMessageKind.HostCall;
            case "hostResult":
                return UiBridgeMessageKind.HostResult;
            case "request":
                return UiBridgeMessageKind.Request;
            case "response":
                return UiBridgeMessageKind.Response;
            case "error":
                return UiBridgeMessageKind.Error;
            case "cancel":
                return UiBridgeMessageKind.Cancel;
            case "acknowledgement":
                return UiBridgeMessageKind.Acknowledgement;
            case "heartbeat":
                return UiBridgeMessageKind.Heartbeat;
            case "shutdown":
                return UiBridgeMessageKind.Shutdown;
            case "chunk":
                return UiBridgeMessageKind.Chunk;
            default:
                throw new JsonException("The bridge kind token is invalid.");
        }
    }
}
