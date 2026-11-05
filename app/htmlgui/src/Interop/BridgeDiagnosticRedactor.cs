using System;

namespace htmlgui;

internal class BridgeDiagnosticRedactionResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal UiBridgeEnvelope? Envelope;
}

internal class BridgeDiagnosticRedactor
{
    internal const string RedactedText = "[redacted]";

    internal BridgeDiagnosticRedactionResult RedactBridgeDiagnostic(UiBridgeEnvelope? envelope, int maximumMessageLength)
    {
        BridgeValidationResult validation = new BridgeValidator().ValidateBridgePayload(envelope, maximumMessageLength);
        if (!validation.Success)
        {
            return new BridgeDiagnosticRedactionResult { Code = validation.Code };
        }

        UiBridgeEnvelope source = envelope ?? new UiBridgeEnvelope();
        UiBridgeEnvelope redacted = new UiBridgeEnvelope
        {
            Version = source.Version,
            Session = RedactedText,
            Sequence = source.Sequence,
            CorrelationId = source.CorrelationId,
            Origin = source.Origin,
            Kind = source.Kind,
            Payload = RedactDiagnosticValue(source.Payload, null),
            Request = RedactDiagnosticRequest(source.Request),
            Response = RedactDiagnosticResponse(source.Response),
            Error = CopyDiagnosticError(source.Error)
        };
        return new BridgeDiagnosticRedactionResult { Success = true, Envelope = redacted };
    }

    internal UiValue? RedactDiagnosticValue(UiValue? value, string? fieldName)
    {
        if (value == null)
        {
            return null;
        }

        if (IsSensitiveDiagnosticField(fieldName))
        {
            return new UiValue { Kind = UiValueKind.String, StringValue = RedactedText };
        }

        UiValue result = new UiValue
        {
            Kind = value.Kind,
            BooleanValue = value.BooleanValue,
            IntegerValue = value.IntegerValue,
            FloatingPointValue = value.FloatingPointValue,
            DecimalValue = value.DecimalValue,
            StringValue = value.StringValue,
            DateTimeValue = value.DateTimeValue
        };
        if (value.BinaryValue != null)
        {
            result.BinaryValue = new byte[value.BinaryValue.Length];
            Array.Copy(value.BinaryValue, result.BinaryValue, value.BinaryValue.Length);
        }

        if (value.ArrayValue != null)
        {
            result.ArrayValue = new UiValue[value.ArrayValue.Length];
            int arrayIndex = 0;
            while (arrayIndex < value.ArrayValue.Length)
            {
                result.ArrayValue[arrayIndex] = RedactDiagnosticValue(value.ArrayValue[arrayIndex], null) ?? new UiValue();
                arrayIndex++;
            }
        }

        if (value.ObjectValue != null)
        {
            result.ObjectValue = RedactDiagnosticEntries(value.ObjectValue);
        }

        return result;
    }

    internal UiValueEntry[] RedactDiagnosticEntries(UiValueEntry[] entries)
    {
        UiValueEntry[] result = new UiValueEntry[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            UiValueEntry entry = entries[index];
            result[index] = new UiValueEntry
            {
                Key = entry.Key,
                Value = RedactDiagnosticValue(entry.Value, entry.Key) ?? new UiValue()
            };
            index++;
        }

        return result;
    }

    internal UiBridgeRequest? RedactDiagnosticRequest(UiBridgeRequest? request)
    {
        if (request == null)
        {
            return null;
        }

        return new UiBridgeRequest
        {
            CorrelationId = request.CorrelationId,
            Operation = request.Operation,
            Arguments = RedactDiagnosticEntries(request.Arguments),
            TimeoutMilliseconds = request.TimeoutMilliseconds
        };
    }

    internal UiBridgeResponse? RedactDiagnosticResponse(UiBridgeResponse? response)
    {
        if (response == null)
        {
            return null;
        }

        return new UiBridgeResponse
        {
            CorrelationId = response.CorrelationId,
            Success = response.Success,
            Value = RedactDiagnosticValue(response.Value, null),
            Error = CopyDiagnosticError(response.Error)
        };
    }

    internal UiBridgeError? CopyDiagnosticError(UiBridgeError? error)
    {
        if (error == null)
        {
            return null;
        }

        return new UiBridgeError
        {
            Code = error.Code,
            Message = error.Message,
            CorrelationId = error.CorrelationId,
            IsRetryable = error.IsRetryable
        };
    }

    internal bool IsSensitiveDiagnosticField(string? fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return false;
        }

        string normalized = fieldName.ToLowerInvariant();
        return normalized.Contains("password") ||
            normalized.Contains("passwd") ||
            normalized.Contains("passcode") ||
            normalized.Contains("secret") ||
            normalized.Contains("token") ||
            normalized.Contains("authorization") ||
            normalized.Contains("credential") ||
            normalized.Contains("cookie") ||
            normalized.Contains("apikey") ||
            normalized.Contains("api_key") ||
            normalized.Contains("api-key") ||
            normalized.Contains("privatekey") ||
            normalized.Contains("private_key") ||
            normalized.Contains("private-key") ||
            ValueComparer.TextEquals(normalized, BridgeProtocolNames.Nonce) ||
            ValueComparer.TextEquals(normalized, BridgeProtocolNames.Session);
    }
}
