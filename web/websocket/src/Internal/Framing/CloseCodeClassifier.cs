namespace websocket;

internal enum CloseCodeKind
{
    OutOfRange = 0,
    Reserved = 1,
    ForbiddenPseudoCode = 2,
    RegisteredProtocol = 3,
    RegisteredApplication = 4,
    Application = 5,
    PrivateUse = 6
}

internal class CloseCodeClassification
{
    internal CloseCodeKind Kind = CloseCodeKind.OutOfRange;
    internal bool Valid;
    internal bool HasKnownCode;
    internal WebSocketCloseCode KnownCode;
}

internal static class CloseCodeClassifier
{
    internal static CloseCodeClassification Classify(int code)
    {
        CloseCodeClassification result = new CloseCodeClassification();
        if ((code >= WebSocketCloseCodes.NormalClosure && code <= WebSocketCloseCodes.TlsHandshake) ||
            code == WebSocketCloseCodes.Unauthorized ||
            code == WebSocketCloseCodes.Forbidden ||
            code == WebSocketCloseCodes.Timeout)
        {
            result.HasKnownCode = true;
            result.KnownCode = (WebSocketCloseCode)code;
        }

        if (code < WebSocketCloseCodes.NormalClosure ||
            code > WebSocketCloseCodes.PrivateUseRangeMaximum)
        {
            return result;
        }

        if (code == WebSocketCloseCodes.NoStatusReceived ||
            code == WebSocketCloseCodes.AbnormalClosure ||
            code == WebSocketCloseCodes.TlsHandshake)
        {
            result.Kind = CloseCodeKind.ForbiddenPseudoCode;
            return result;
        }

        if (code == WebSocketCloseCodes.Reserved ||
            (code > WebSocketCloseCodes.TlsHandshake &&
             code < WebSocketCloseCodes.RegisteredApplicationRangeMinimum))
        {
            result.Kind = CloseCodeKind.Reserved;
            return result;
        }

        result.Valid = true;
        if (code < WebSocketCloseCodes.RegisteredApplicationRangeMinimum)
        {
            result.Kind = CloseCodeKind.RegisteredProtocol;
        }
        else if (code == WebSocketCloseCodes.Unauthorized ||
                 code == WebSocketCloseCodes.Forbidden ||
                 code == WebSocketCloseCodes.Timeout)
        {
            result.Kind = CloseCodeKind.RegisteredApplication;
        }
        else if (code <= WebSocketCloseCodes.RegisteredApplicationRangeMaximum)
        {
            result.Kind = CloseCodeKind.Application;
        }
        else
        {
            result.Kind = CloseCodeKind.PrivateUse;
        }

        return result;
    }
}
