namespace websocket;

internal class CloseReasonValidationResult
{
    internal bool Valid;
    internal bool TooLong;
    internal int ByteCount;
}

internal static class ClosePayloadValidator
{
    internal static CloseReasonValidationResult ValidateReason(string reason)
    {
        CloseReasonValidationResult result = new CloseReasonValidationResult();
        if (reason == null)
        {
            return result;
        }

        int index = 0;
        int byteCount = 0;
        while (index < reason.Length)
        {
            int value = reason[index];
            if (value <= 127)
            {
                byteCount = byteCount + 1;
            }
            else if (value <= 2047)
            {
                byteCount = byteCount + 2;
            }
            else if (value >= 55296 && value <= 56319)
            {
                if (index + 1 >= reason.Length)
                {
                    return result;
                }

                int nextValue = reason[index + 1];
                if (nextValue < 56320 || nextValue > 57343)
                {
                    return result;
                }

                byteCount = byteCount + 4;
                index = index + 1;
            }
            else if (value >= 56320 && value <= 57343)
            {
                return result;
            }
            else
            {
                byteCount = byteCount + 3;
            }

            if (byteCount > WebSocketProtocolConstants.CloseReasonLengthMaximum)
            {
                result.TooLong = true;
                return result;
            }

            index = index + 1;
        }

        result.Valid = true;
        result.ByteCount = byteCount;
        return result;
    }
}
