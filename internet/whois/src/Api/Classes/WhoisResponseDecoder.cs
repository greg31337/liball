namespace whois;

/// <summary>
/// Produces a decoded text view while preserving the raw WHOIS response as the source of truth.
/// </summary>
public class WhoisResponseDecoder : IWhoisResponseDecoder
{
    /// <summary>
    /// Initializes the built-in lossless response decoder.
    /// </summary>
    public WhoisResponseDecoder()
    {
    }

    /// <summary>
    /// Decodes a raw response using explicit encoding, byte-order-mark, and configured fallback precedence.
    /// </summary>
    /// <param name="response">The raw response whose bytes remain the source of truth.</param>
    /// <param name="options">The response decoding options.</param>
    /// <returns>The decoded response view and status metadata.</returns>
    /// <exception cref="System.ArgumentNullException">The response or options are <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">The decoding mode is not supported.</exception>
    /// <exception cref="System.InvalidOperationException">The raw response contains a null body.</exception>
    public WhoisTextResponse DecodeResponse(WhoisRawResponse response, WhoisEncodingOptions options)
    {
        InputGuard.RequireReference(response, "response");
        InputGuard.RequireReference(options, "options");
        if (response.Body == null)
        {
            throw new System.InvalidOperationException("The raw response contains a null body.");
        }
        if (options.DecodingMode != WhoisDecodingMode.Strict && options.DecodingMode != WhoisDecodingMode.ReplaceInvalidUtf8 && options.DecodingMode != WhoisDecodingMode.Latin1Fallback)
        {
            throw new System.ArgumentOutOfRangeException("options.DecodingMode");
        }

        WhoisTextResponse result = new WhoisTextResponse();
        result.RawResponse = response;
        int offset = 0;
        bool automaticUtf8 = false;
        System.Text.Encoding selectedEncoding;

        if (options.ResponseEncoding != null)
        {
            selectedEncoding = CreateStrictEncoding(options.ResponseEncoding.CodePage);
        }
        else
        {
            automaticUtf8 = true;
            selectedEncoding = CreateStrictEncoding(65001);
            if (options.DetectByteOrderMark)
            {
                if (HasPrefix(response.Body, new byte[] { 0, 0, 254, 255 }))
                {
                    selectedEncoding = CreateStrictEncoding(12001);
                    offset = 4;
                    automaticUtf8 = false;
                    result.ByteOrderMarkDetected = true;
                }
                else if (HasPrefix(response.Body, new byte[] { 255, 254, 0, 0 }))
                {
                    selectedEncoding = CreateStrictEncoding(12000);
                    offset = 4;
                    automaticUtf8 = false;
                    result.ByteOrderMarkDetected = true;
                }
                else if (HasPrefix(response.Body, new byte[] { 239, 187, 191 }))
                {
                    offset = 3;
                    result.ByteOrderMarkDetected = true;
                }
                else if (HasPrefix(response.Body, new byte[] { 254, 255 }))
                {
                    selectedEncoding = CreateStrictEncoding(1201);
                    offset = 2;
                    automaticUtf8 = false;
                    result.ByteOrderMarkDetected = true;
                }
                else if (HasPrefix(response.Body, new byte[] { 255, 254 }))
                {
                    selectedEncoding = CreateStrictEncoding(1200);
                    offset = 2;
                    automaticUtf8 = false;
                    result.ByteOrderMarkDetected = true;
                }
            }
        }

        result.EncodingName = selectedEncoding.WebName;
        System.Threading.Tasks.Task<string> decodeTask = StartDecode(selectedEncoding, response.Body, offset);
        WaitForDecode(decodeTask);
        if (decodeTask.IsCompletedSuccessfully)
        {
            result.Text = decodeTask.Result;
            result.IsDecoded = true;
            return result;
        }

        result.Error = GetDecodeError(decodeTask);
        if (!automaticUtf8 || options.DecodingMode == WhoisDecodingMode.Strict)
        {
            return result;
        }

        System.Text.Encoding fallbackEncoding = System.Text.Encoding.GetEncoding(65001, System.Text.EncoderFallback.ExceptionFallback, new System.Text.DecoderReplacementFallback("�"));
        if (options.DecodingMode == WhoisDecodingMode.Latin1Fallback)
        {
            fallbackEncoding = System.Text.Encoding.GetEncoding(28591, System.Text.EncoderFallback.ExceptionFallback, System.Text.DecoderFallback.ExceptionFallback);
            result.FallbackUsed = true;
        }
        else
        {
            result.ReplacementUsed = true;
        }

        result.EncodingName = fallbackEncoding.WebName;
        System.Threading.Tasks.Task<string> fallbackTask = StartDecode(fallbackEncoding, response.Body, offset);
        WaitForDecode(fallbackTask);
        if (fallbackTask.IsCompletedSuccessfully)
        {
            result.Text = fallbackTask.Result;
            result.IsDecoded = true;
            result.Error = null;
        }
        else
        {
            result.Error = GetDecodeError(fallbackTask);
        }
        return result;
    }

    internal static System.Text.Encoding CreateStrictEncoding(int codePage)
    {
        return System.Text.Encoding.GetEncoding(codePage, System.Text.EncoderFallback.ExceptionFallback, System.Text.DecoderFallback.ExceptionFallback);
    }

    internal static bool HasPrefix(byte[] bytes, byte[] prefix)
    {
        if (bytes.Length < prefix.Length)
        {
            return false;
        }
        int index = 0;
        while (index < prefix.Length)
        {
            if (bytes[index] != prefix[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static System.Threading.Tasks.Task<string> StartDecode(System.Text.Encoding encoding, byte[] bytes, int offset)
    {
        return System.Threading.Tasks.Task.Run<string>(() => encoding.GetString(bytes, offset, bytes.Length - offset));
    }

    internal static void WaitForDecode(System.Threading.Tasks.Task<string> decodeTask)
    {
        System.Threading.Tasks.Task completedTask = System.Threading.Tasks.Task.WhenAny(new System.Threading.Tasks.Task[] { decodeTask }).Result;
        if (completedTask != decodeTask)
        {
            throw new System.InvalidOperationException("Response decoding observed an unexpected task.");
        }
    }

    internal static System.Exception GetDecodeError(System.Threading.Tasks.Task<string> decodeTask)
    {
        if (decodeTask.Exception != null && decodeTask.Exception.InnerException != null)
        {
            return decodeTask.Exception.InnerException;
        }
        if (decodeTask.Exception != null)
        {
            return decodeTask.Exception;
        }
        if (decodeTask.IsCanceled)
        {
            return new System.OperationCanceledException("Response decoding was cancelled.");
        }
        return new System.InvalidOperationException("Response decoding did not reach a terminal state.");
    }
}
