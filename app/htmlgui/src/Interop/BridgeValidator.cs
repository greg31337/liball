using System;
using System.Collections.Generic;

namespace htmlgui;

internal class BridgeValidationResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}

internal class BridgeValueValidationState
{
    internal int MaximumDepth;
    internal int MaximumCollectionCount;
    internal int MaximumStringLength;
    internal int MaximumBinaryLength;
    internal int CollectionCount;
    internal List<UiValue> Ancestors;
    internal UiValueTraversalState JsonState;

    internal BridgeValueValidationState(int maximumMessageLength)
    {
        MaximumDepth = 32;
        MaximumCollectionCount = 4096;
        MaximumStringLength = Math.Min(maximumMessageLength, 65536);
        MaximumBinaryLength = maximumMessageLength;
        Ancestors = new List<UiValue>();
        JsonState = new UiValueTraversalState(MaximumDepth, MaximumCollectionCount, MaximumStringLength, MaximumBinaryLength);
    }
}

internal class BridgeValidator
{
    internal BridgeValidationResult ValidateBridgeEnvelope(UiBridgeEnvelope? envelope, UiBridgeOptions? options, string? expectedSession, bool isMainFrame, int lastSequence)
    {
        BridgeValidationResult optionResult = ValidateBridgeOptions(options);
        if (!optionResult.Success)
        {
            return optionResult;
        }

        UiBridgeOptions checkedOptions = options ?? new UiBridgeOptions();
        BridgeValidationResult payloadResult = ValidateBridgePayload(envelope, checkedOptions.MaximumMessageLength);
        if (!payloadResult.Success)
        {
            return payloadResult;
        }

        UiBridgeEnvelope checkedEnvelope = envelope ?? new UiBridgeEnvelope();
        if (checkedEnvelope.Version != checkedOptions.Version)
        {
            return CreateBridgeValidationFailure("bridge.version-mismatch", "The bridge protocol version does not match the configured version.");
        }

        if (!string.IsNullOrWhiteSpace(expectedSession) && !ValueComparer.TextEquals(checkedEnvelope.Session, expectedSession))
        {
            return CreateBridgeValidationFailure("bridge.session-mismatch", "The bridge session does not match the active session.");
        }

        if (!string.IsNullOrWhiteSpace(checkedOptions.Origin) && !ValueComparer.TextEquals(checkedEnvelope.Origin, checkedOptions.Origin))
        {
            return CreateBridgeValidationFailure("bridge.origin-mismatch", "The bridge origin does not match the expected endpoint.");
        }

        if (checkedOptions.RequireMainFrame && !isMainFrame)
        {
            return CreateBridgeValidationFailure("bridge.frame-rejected", "The bridge message did not originate in the main frame.");
        }

        if (lastSequence < -1)
        {
            return CreateBridgeValidationFailure("bridge.invalid-last-sequence", "The last accepted bridge sequence is invalid.");
        }

        if (checkedEnvelope.Sequence <= lastSequence)
        {
            return CreateBridgeValidationFailure("bridge.sequence-rejected", "The bridge sequence is not newer than the last accepted sequence.");
        }

        return CreateBridgeValidationSuccess();
    }

    internal BridgeValidationResult ValidateBridgePayload(UiBridgeEnvelope? envelope, int maximumMessageLength)
    {
        if (envelope == null)
        {
            return CreateBridgeValidationFailure("bridge.null-envelope", "A bridge envelope is required.");
        }

        if (maximumMessageLength < 1)
        {
            return CreateBridgeValidationFailure("bridge.invalid-message-limit", "The maximum bridge message length must be positive.");
        }

        BridgeValueValidationState state = new BridgeValueValidationState(maximumMessageLength);
        if (envelope.Version < BridgeProtocolVersions.MinimumSupported || envelope.Version > BridgeProtocolVersions.Current)
        {
            return CreateBridgeValidationFailure("bridge.unsupported-version", "The bridge protocol version is not supported.");
        }

        if (string.IsNullOrWhiteSpace(envelope.Session) || envelope.Session.Length > state.MaximumStringLength)
        {
            return CreateBridgeValidationFailure("bridge.invalid-session", "The bridge session is missing or exceeds its bound.");
        }

        if (envelope.Sequence < 0)
        {
            return CreateBridgeValidationFailure("bridge.invalid-sequence", "The bridge sequence cannot be negative.");
        }

        if (!ValueComparer.TextEquals(envelope.Origin, BridgeProtocolNames.HostOrigin) && !ValueComparer.TextEquals(envelope.Origin, BridgeProtocolNames.BrowserOrigin))
        {
            return CreateBridgeValidationFailure("bridge.invalid-origin", "The bridge origin token is invalid.");
        }

        if (!IsBridgeKindValid(envelope.Kind))
        {
            return CreateBridgeValidationFailure("bridge.invalid-kind", "The bridge message kind is invalid.");
        }

        if (envelope.CorrelationId != null && (string.IsNullOrWhiteSpace(envelope.CorrelationId) || envelope.CorrelationId.Length > state.MaximumStringLength))
        {
            return CreateBridgeValidationFailure("bridge.invalid-correlation", "The bridge correlation identifier is invalid.");
        }

        int bodyCount = 0;
        bodyCount += envelope.Payload == null ? 0 : 1;
        bodyCount += envelope.Request == null ? 0 : 1;
        bodyCount += envelope.Response == null ? 0 : 1;
        bodyCount += envelope.Error == null ? 0 : 1;
        if (bodyCount > 1)
        {
            return CreateBridgeValidationFailure("bridge.ambiguous-payload", "A bridge envelope can contain only one payload form.");
        }

        if (IsRequestBridgeKind(envelope.Kind))
        {
            if (envelope.Request == null)
            {
                return CreateBridgeValidationFailure("bridge.missing-request", "The bridge request payload is required.");
            }

            BridgeValidationResult requestResult = ValidateBridgeRequest(envelope.Request, envelope.CorrelationId, state);
            if (!requestResult.Success)
            {
                return requestResult;
            }
        }
        else if (envelope.Request != null)
        {
            return CreateBridgeValidationFailure("bridge.unexpected-request", "The bridge request payload does not match the message kind.");
        }

        if (IsResponseBridgeKind(envelope.Kind))
        {
            if (envelope.Response == null)
            {
                return CreateBridgeValidationFailure("bridge.missing-response", "The bridge response payload is required.");
            }

            BridgeValidationResult responseResult = ValidateBridgeResponse(envelope.Response, envelope.CorrelationId, state);
            if (!responseResult.Success)
            {
                return responseResult;
            }
        }
        else if (envelope.Response != null)
        {
            return CreateBridgeValidationFailure("bridge.unexpected-response", "The bridge response payload does not match the message kind.");
        }

        if (envelope.Kind == UiBridgeMessageKind.Error)
        {
            if (envelope.Error == null)
            {
                return CreateBridgeValidationFailure("bridge.missing-error", "The bridge error payload is required.");
            }

            BridgeValidationResult errorResult = ValidateBridgeError(envelope.Error, envelope.CorrelationId, state, false);
            if (!errorResult.Success)
            {
                return errorResult;
            }
        }
        else if (envelope.Error != null)
        {
            return CreateBridgeValidationFailure("bridge.unexpected-error", "The bridge error payload does not match the message kind.");
        }

        if (envelope.Kind == UiBridgeMessageKind.Cancel && string.IsNullOrWhiteSpace(envelope.CorrelationId))
        {
            return CreateBridgeValidationFailure("bridge.missing-correlation", "A cancel message requires a correlation identifier.");
        }

        if (envelope.Payload != null)
        {
            BridgeValidationResult valueResult = ValidateBridgeValue(envelope.Payload, 0, state);
            if (!valueResult.Success)
            {
                return valueResult;
            }
        }

        return CreateBridgeValidationSuccess();
    }

    internal BridgeValidationResult ValidateBridgeOptions(UiBridgeOptions? options)
    {
        if (options == null)
        {
            return CreateBridgeValidationFailure("bridge.null-options", "Bridge options are required.");
        }

        if (options.Version < BridgeProtocolVersions.MinimumSupported || options.Version > BridgeProtocolVersions.Current)
        {
            return CreateBridgeValidationFailure("bridge.invalid-options-version", "The configured bridge version is unsupported.");
        }

        if (options.Origin == null || options.Origin.Length > 65536)
        {
            return CreateBridgeValidationFailure("bridge.invalid-options-origin", "The configured bridge origin exceeds its bound.");
        }

        if (options.MaximumMessageLength < 1 || options.MaximumPendingRequests < 1 || options.TimeoutMilliseconds < 1)
        {
            return CreateBridgeValidationFailure("bridge.invalid-options-limit", "Bridge resource limits must be positive.");
        }

        return CreateBridgeValidationSuccess();
    }

    internal BridgeValidationResult ValidateBridgeRequest(UiBridgeRequest request, string? envelopeCorrelation, BridgeValueValidationState state)
    {
        if (string.IsNullOrWhiteSpace(envelopeCorrelation) || string.IsNullOrWhiteSpace(request.CorrelationId) || !ValueComparer.TextEquals(request.CorrelationId, envelopeCorrelation))
        {
            return CreateBridgeValidationFailure("bridge.correlation-mismatch", "The request correlation identifier does not match its envelope.");
        }

        if (request.CorrelationId.Length > state.MaximumStringLength || string.IsNullOrWhiteSpace(request.Operation) || request.Operation.Length > state.MaximumStringLength)
        {
            return CreateBridgeValidationFailure("bridge.invalid-request", "The request correlation or operation token is invalid.");
        }

        if (request.Arguments == null || request.TimeoutMilliseconds < 0)
        {
            return CreateBridgeValidationFailure("bridge.invalid-request", "The request arguments or timeout are invalid.");
        }

        return ValidateBridgeArguments(request.Arguments, state);
    }

    internal BridgeValidationResult ValidateBridgeResponse(UiBridgeResponse response, string? envelopeCorrelation, BridgeValueValidationState state)
    {
        if (string.IsNullOrWhiteSpace(envelopeCorrelation) || string.IsNullOrWhiteSpace(response.CorrelationId) || !ValueComparer.TextEquals(response.CorrelationId, envelopeCorrelation) || response.CorrelationId.Length > state.MaximumStringLength)
        {
            return CreateBridgeValidationFailure("bridge.correlation-mismatch", "The response correlation identifier does not match its envelope.");
        }

        if (response.Success && response.Error != null)
        {
            return CreateBridgeValidationFailure("bridge.invalid-response", "A successful response cannot contain an error.");
        }

        if (!response.Success && response.Error == null)
        {
            return CreateBridgeValidationFailure("bridge.invalid-response", "A failed response requires an error.");
        }

        if (!response.Success && response.Value != null)
        {
            return CreateBridgeValidationFailure("bridge.invalid-response", "A failed response cannot contain a value.");
        }

        if (response.Value != null)
        {
            BridgeValidationResult valueResult = ValidateBridgeValue(response.Value, 0, state);
            if (!valueResult.Success)
            {
                return valueResult;
            }
        }

        if (response.Error != null)
        {
            return ValidateBridgeError(response.Error, response.CorrelationId, state, true);
        }

        return CreateBridgeValidationSuccess();
    }

    internal BridgeValidationResult ValidateBridgeError(UiBridgeError error, string? expectedCorrelation, BridgeValueValidationState state, bool requireCorrelation)
    {
        if (string.IsNullOrWhiteSpace(error.Code) || error.Code.Length > state.MaximumStringLength || error.Message == null || error.Message.Length > state.MaximumStringLength)
        {
            return CreateBridgeValidationFailure("bridge.invalid-error", "The bridge error code or message is invalid.");
        }

        if (requireCorrelation && string.IsNullOrWhiteSpace(error.CorrelationId))
        {
            return CreateBridgeValidationFailure("bridge.correlation-mismatch", "The bridge error requires a correlation identifier.");
        }

        if (error.CorrelationId != null && (string.IsNullOrWhiteSpace(error.CorrelationId) || error.CorrelationId.Length > state.MaximumStringLength))
        {
            return CreateBridgeValidationFailure("bridge.invalid-correlation", "The bridge error correlation identifier is invalid.");
        }

        if ((expectedCorrelation != null || error.CorrelationId != null) && !ValueComparer.TextEquals(expectedCorrelation, error.CorrelationId))
        {
            return CreateBridgeValidationFailure("bridge.correlation-mismatch", "The bridge error correlation identifier does not match its container.");
        }

        return CreateBridgeValidationSuccess();
    }

    internal BridgeValidationResult ValidateBridgeArguments(UiValueEntry[] arguments, BridgeValueValidationState state)
    {
        if (!AddBridgeCollectionCount(arguments.Length, state))
        {
            return CreateBridgeValidationFailure("bridge.payload-count-exceeded", "The bridge payload exceeds its aggregate item bound.");
        }

        int index = 0;
        while (index < arguments.Length)
        {
            UiValueEntry? argument = arguments[index];
            if (argument == null || string.IsNullOrWhiteSpace(argument.Key) || argument.Key.Length > state.MaximumStringLength)
            {
                return CreateBridgeValidationFailure("bridge.invalid-arguments", "Bridge request arguments require bounded unique keys.");
            }

            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (arguments[earlierIndex].Key == argument.Key)
                {
                    return CreateBridgeValidationFailure("bridge.invalid-arguments", "Bridge request argument keys must be unique.");
                }

                earlierIndex++;
            }

            BridgeValidationResult valueResult = ValidateBridgeValue(argument.Value, 0, state);
            if (!valueResult.Success)
            {
                return valueResult;
            }

            index++;
        }

        return CreateBridgeValidationSuccess();
    }

    internal BridgeValidationResult ValidateBridgeValue(UiValue? value, int depth, BridgeValueValidationState state)
    {
        if (value == null)
        {
            return CreateBridgeValidationFailure("bridge.invalid-value", "Bridge values cannot be null.");
        }

        if (depth > state.MaximumDepth)
        {
            return CreateBridgeValidationFailure("bridge.payload-depth-exceeded", "The bridge payload exceeds its nesting bound.");
        }

        if (!EnterBridgeValue(value, state))
        {
            return CreateBridgeValidationFailure("bridge.payload-cycle", "The bridge payload cannot contain cycles.");
        }

        bool hasReferenceValue = value.StringValue != null || value.DateTimeValue != null || value.BinaryValue != null || value.ArrayValue != null || value.ObjectValue != null;
        bool hasPrimitiveValue = value.BooleanValue || value.IntegerValue != 0L || value.FloatingPointValue != 0.0 || value.DecimalValue != 0m;
        BridgeValidationResult result = CreateBridgeValidationSuccess();
        switch (value.Kind)
        {
            case UiValueKind.Null:
                if (hasReferenceValue || hasPrimitiveValue)
                {
                    result = CreateBridgeValidationFailure("bridge.invalid-value", "A null bridge value contains tagged data.");
                }

                break;
            case UiValueKind.Boolean:
                if (hasReferenceValue || value.IntegerValue != 0L || value.FloatingPointValue != 0.0 || value.DecimalValue != 0m)
                {
                    result = CreateBridgeValidationFailure("bridge.invalid-value", "A Boolean bridge value contains data outside its tag.");
                }

                break;
            case UiValueKind.Integer:
                if (hasReferenceValue || value.BooleanValue || value.FloatingPointValue != 0.0 || value.DecimalValue != 0m)
                {
                    result = CreateBridgeValidationFailure("bridge.invalid-value", "An integer bridge value contains data outside its tag.");
                }

                break;
            case UiValueKind.FloatingPoint:
                if (hasReferenceValue || value.BooleanValue || value.IntegerValue != 0L || value.DecimalValue != 0m || !double.IsFinite(value.FloatingPointValue))
                {
                    result = CreateBridgeValidationFailure("bridge.invalid-value", "A floating-point bridge value is malformed or non-finite.");
                }

                break;
            case UiValueKind.Decimal:
                if (hasReferenceValue || value.BooleanValue || value.IntegerValue != 0L || value.FloatingPointValue != 0.0)
                {
                    result = CreateBridgeValidationFailure("bridge.invalid-value", "A decimal bridge value contains data outside its tag.");
                }

                break;
            case UiValueKind.String:
                if (value.StringValue == null || value.StringValue.Length > state.MaximumStringLength || value.DateTimeValue != null || value.BinaryValue != null || value.ArrayValue != null || value.ObjectValue != null || hasPrimitiveValue)
                {
                    result = CreateBridgeValidationFailure("bridge.invalid-value", "A string bridge value is malformed or exceeds its bound.");
                }

                break;
            case UiValueKind.DateTime:
                if (value.DateTimeValue == null || value.DateTimeValue.Length > state.MaximumStringLength || value.StringValue != null || value.BinaryValue != null || value.ArrayValue != null || value.ObjectValue != null || hasPrimitiveValue)
                {
                    result = CreateBridgeValidationFailure("bridge.invalid-value", "A date-time bridge value is malformed or exceeds its bound.");
                }

                break;
            case UiValueKind.Binary:
                if (value.BinaryValue == null || value.BinaryValue.Length > state.MaximumBinaryLength || value.StringValue != null || value.DateTimeValue != null || value.ArrayValue != null || value.ObjectValue != null || hasPrimitiveValue)
                {
                    result = CreateBridgeValidationFailure("bridge.invalid-value", "A binary bridge value is malformed or exceeds its bound.");
                }

                break;
            case UiValueKind.Array:
                result = ValidateBridgeArrayValue(value, depth, state, hasPrimitiveValue);
                break;
            case UiValueKind.Object:
                result = ValidateBridgeObjectValue(value, depth, state, hasPrimitiveValue);
                break;
            default:
                result = CreateBridgeValidationFailure("bridge.invalid-value-kind", "The bridge value kind is invalid.");
                break;
        }

        LeaveBridgeValue(state);
        return result;
    }

    internal BridgeValidationResult ValidateBridgeArrayValue(UiValue value, int depth, BridgeValueValidationState state, bool hasPrimitiveValue)
    {
        if (value.ArrayValue == null || value.StringValue != null || value.DateTimeValue != null || value.BinaryValue != null || value.ObjectValue != null || hasPrimitiveValue)
        {
            return CreateBridgeValidationFailure("bridge.invalid-value", "An array bridge value contains data outside its tag.");
        }

        if (!AddBridgeCollectionCount(value.ArrayValue.Length, state))
        {
            return CreateBridgeValidationFailure("bridge.payload-count-exceeded", "The bridge payload exceeds its aggregate item bound.");
        }

        int index = 0;
        while (index < value.ArrayValue.Length)
        {
            BridgeValidationResult itemResult = ValidateBridgeValue(value.ArrayValue[index], depth + 1, state);
            if (!itemResult.Success)
            {
                return itemResult;
            }

            index++;
        }

        return CreateBridgeValidationSuccess();
    }

    internal BridgeValidationResult ValidateBridgeObjectValue(UiValue value, int depth, BridgeValueValidationState state, bool hasPrimitiveValue)
    {
        if (value.ObjectValue == null || value.StringValue != null || value.DateTimeValue != null || value.BinaryValue != null || value.ArrayValue != null || hasPrimitiveValue)
        {
            return CreateBridgeValidationFailure("bridge.invalid-value", "An object bridge value contains data outside its tag.");
        }

        if (!AddBridgeCollectionCount(value.ObjectValue.Length, state))
        {
            return CreateBridgeValidationFailure("bridge.payload-count-exceeded", "The bridge payload exceeds its aggregate item bound.");
        }

        int index = 0;
        while (index < value.ObjectValue.Length)
        {
            UiValueEntry? entry = value.ObjectValue[index];
            if (entry == null || entry.Key == null || entry.Key.Length > state.MaximumStringLength)
            {
                return CreateBridgeValidationFailure("bridge.invalid-value", "Bridge object entries require bounded keys.");
            }

            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (value.ObjectValue[earlierIndex].Key == entry.Key)
                {
                    return CreateBridgeValidationFailure("bridge.invalid-value", "Bridge object entry keys must be unique.");
                }

                earlierIndex++;
            }

            BridgeValidationResult entryResult = ValidateBridgeValue(entry.Value, depth + 1, state);
            if (!entryResult.Success)
            {
                return entryResult;
            }

            index++;
        }

        return CreateBridgeValidationSuccess();
    }

    internal bool EnterBridgeValue(UiValue value, BridgeValueValidationState state)
    {
        int index = 0;
        while (index < state.Ancestors.Count)
        {
            if (object.ReferenceEquals(state.Ancestors[index], value))
            {
                return false;
            }

            index++;
        }

        state.Ancestors.Add(value);
        return true;
    }

    internal void LeaveBridgeValue(BridgeValueValidationState state)
    {
        state.Ancestors.RemoveAt(state.Ancestors.Count - 1);
    }

    internal bool AddBridgeCollectionCount(int count, BridgeValueValidationState state)
    {
        if (count > state.MaximumCollectionCount - state.CollectionCount)
        {
            return false;
        }

        state.CollectionCount += count;
        return true;
    }

    internal bool IsBridgeKindValid(UiBridgeMessageKind kind)
    {
        switch (kind)
        {
            case UiBridgeMessageKind.Initialize:
            case UiBridgeMessageKind.Ready:
            case UiBridgeMessageKind.RenderSnapshot:
            case UiBridgeMessageKind.DomPatch:
            case UiBridgeMessageKind.Event:
            case UiBridgeMessageKind.Command:
            case UiBridgeMessageKind.StateMutation:
            case UiBridgeMessageKind.Validation:
            case UiBridgeMessageKind.Navigation:
            case UiBridgeMessageKind.Focus:
            case UiBridgeMessageKind.Measurement:
            case UiBridgeMessageKind.VirtualizationRange:
            case UiBridgeMessageKind.VirtualizationResult:
            case UiBridgeMessageKind.Lifecycle:
            case UiBridgeMessageKind.Log:
            case UiBridgeMessageKind.Diagnostic:
            case UiBridgeMessageKind.HostCall:
            case UiBridgeMessageKind.HostResult:
            case UiBridgeMessageKind.Request:
            case UiBridgeMessageKind.Response:
            case UiBridgeMessageKind.Error:
            case UiBridgeMessageKind.Cancel:
            case UiBridgeMessageKind.Acknowledgement:
            case UiBridgeMessageKind.Heartbeat:
            case UiBridgeMessageKind.Shutdown:
            case UiBridgeMessageKind.Chunk:
                return true;
            default:
                return false;
        }
    }

    internal bool IsRequestBridgeKind(UiBridgeMessageKind kind)
    {
        return kind == UiBridgeMessageKind.Request || kind == UiBridgeMessageKind.HostCall;
    }

    internal bool IsResponseBridgeKind(UiBridgeMessageKind kind)
    {
        return kind == UiBridgeMessageKind.Response || kind == UiBridgeMessageKind.HostResult;
    }

    internal BridgeValidationResult CreateBridgeValidationSuccess()
    {
        return new BridgeValidationResult { Success = true };
    }

    internal BridgeValidationResult CreateBridgeValidationFailure(string code, string message)
    {
        return new BridgeValidationResult { Code = code, Message = message };
    }
}
