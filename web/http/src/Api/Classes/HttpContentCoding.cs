using System.Collections.Generic;

namespace http;

/// <summary>
/// Applies identity, gzip, zlib-wrapped deflate, and Brotli content codings.
/// </summary>
public class HttpContentCoding : IHttpContentCoding
{
    /// <summary>Creates a content-coding service.</summary>
    public HttpContentCoding()
    {
    }

    /// <summary>Encodes content using one content coding.</summary>
    /// <param name="content">The content bytes to encode.</param>
    /// <param name="coding">The content-coding token.</param>
    /// <param name="maximumOutputSize">The maximum encoded size.</param>
    /// <returns>The encoding result.</returns>
    /// <exception cref="HttpContentCodingException">An argument is invalid.</exception>
    public HttpContentCodingResult EncodeContent(
        byte[] content,
        string coding,
        int maximumOutputSize)
    {
        ValidateOperation(content, coding, maximumOutputSize);
        if (!CompressionCodec.IsSupported(coding))
        {
            return CreateUnknown(content, coding);
        }

        return CompressionCodec.EncodeKnown(content, coding, maximumOutputSize);
    }

    /// <summary>Decodes content using one content coding.</summary>
    /// <param name="content">The coded content bytes.</param>
    /// <param name="coding">The content-coding token.</param>
    /// <param name="maximumOutputSize">The maximum decoded size.</param>
    /// <returns>The decoding result.</returns>
    /// <exception cref="HttpContentCodingException">An argument is invalid.</exception>
    public HttpContentCodingResult DecodeContent(
        byte[] content,
        string coding,
        int maximumOutputSize)
    {
        ValidateOperation(content, coding, maximumOutputSize);
        if (!CompressionCodec.IsSupported(coding))
        {
            return CreateUnknown(content, coding);
        }

        return CompressionCodec.DecodeKnown(content, coding, maximumOutputSize);
    }

    /// <summary>Applies content codings in their listed order.</summary>
    /// <param name="content">The original content bytes.</param>
    /// <param name="codings">The codings in application order.</param>
    /// <param name="maximumOutputSize">The maximum size of each transformed result.</param>
    /// <returns>The encoding result and coding progress.</returns>
    /// <exception cref="HttpContentCodingException">An argument is invalid.</exception>
    public HttpContentCodingResult EncodeCodingStack(
        byte[] content,
        List<string> codings,
        int maximumOutputSize)
    {
        ValidateStack(content, codings, maximumOutputSize);
        if (codings.Count == 0)
        {
            return CreateEmptyStack(content, maximumOutputSize);
        }

        HttpContentCodingResult result = CreateInitial(content);
        bool transformed = false;
        int index = 0;
        while (index < codings.Count)
        {
            string coding = codings[index];
            HttpContentCodingResult step;
            if (CompressionCodec.IsSupported(coding))
            {
                step = CompressionCodec.EncodeKnown(
                    result.Content,
                    coding,
                    maximumOutputSize);
            }
            else
            {
                step = CreateUnknown(result.Content, coding);
            }

            if (step.Outcome == HttpContentCodingOutcome.Unknown ||
                step.Outcome == HttpContentCodingOutcome.Malformed ||
                step.Outcome == HttpContentCodingOutcome.LimitExceeded)
            {
                result.Outcome = step.Outcome;
                result.Content = step.Content;
                result.Reason = step.Reason;
                AddRemainingForward(result.RemainingCodings, codings, index);
                return result;
            }

            result.Content = step.Content;
            result.AppliedCodings.Add(coding);
            if (step.Outcome == HttpContentCodingOutcome.Encoded)
            {
                transformed = true;
            }

            index = index + 1;
        }

        result.Outcome = transformed ?
            HttpContentCodingOutcome.Encoded : HttpContentCodingOutcome.Identity;
        return result;
    }

    /// <summary>Removes content codings in reverse application order.</summary>
    /// <param name="content">The coded content bytes.</param>
    /// <param name="codings">The codings in application order.</param>
    /// <param name="maximumOutputSize">The maximum size of each transformed result.</param>
    /// <returns>The decoding result and coding progress.</returns>
    /// <exception cref="HttpContentCodingException">An argument is invalid.</exception>
    public HttpContentCodingResult DecodeCodingStack(
        byte[] content,
        List<string> codings,
        int maximumOutputSize)
    {
        ValidateStack(content, codings, maximumOutputSize);
        if (codings.Count == 0)
        {
            return CreateEmptyStack(content, maximumOutputSize);
        }

        HttpContentCodingResult result = CreateInitial(content);
        bool transformed = false;
        int index = codings.Count - 1;
        while (index >= 0)
        {
            string coding = codings[index];
            HttpContentCodingResult step;
            if (CompressionCodec.IsSupported(coding))
            {
                step = CompressionCodec.DecodeKnown(
                    result.Content,
                    coding,
                    maximumOutputSize);
            }
            else
            {
                step = CreateUnknown(result.Content, coding);
            }

            if (step.Outcome == HttpContentCodingOutcome.Unknown ||
                step.Outcome == HttpContentCodingOutcome.Malformed ||
                step.Outcome == HttpContentCodingOutcome.LimitExceeded)
            {
                result.Outcome = step.Outcome;
                result.Content = step.Content;
                result.Reason = step.Reason;
                AddRemainingReverse(result.RemainingCodings, codings, index);
                return result;
            }

            result.Content = step.Content;
            result.AppliedCodings.Add(coding);
            if (step.Outcome == HttpContentCodingOutcome.Decoded)
            {
                transformed = true;
            }

            index = index - 1;
        }

        result.Outcome = transformed ?
            HttpContentCodingOutcome.Decoded : HttpContentCodingOutcome.Identity;
        return result;
    }

    internal static void ValidateOperation(byte[] content, string coding, int maximumOutputSize)
    {
        if (content == null)
        {
            throw CreateArgumentError(
                "The content byte collection is missing.",
                "invalid-content",
                coding);
        }

        if (!HeaderStore.IsValidName(coding))
        {
            throw CreateArgumentError(
                "The content coding is not a valid token.",
                "invalid-content-coding",
                coding);
        }

        if (maximumOutputSize < 0)
        {
            throw CreateArgumentError(
                "The maximum output size cannot be negative.",
                "invalid-output-limit",
                coding);
        }
    }

    internal static void ValidateStack(
        byte[] content,
        List<string> codings,
        int maximumOutputSize)
    {
        if (codings == null)
        {
            throw CreateArgumentError(
                "The content-coding stack is missing.",
                "invalid-content-coding-stack",
                "");
        }

        ValidateOperation(content, HttpContentCodings.Identity, maximumOutputSize);
        int index = 0;
        while (index < codings.Count)
        {
            if (!HeaderStore.IsValidName(codings[index]))
            {
                throw CreateArgumentError(
                    "The content-coding stack contains an invalid token.",
                    "invalid-content-coding",
                    codings[index]);
            }

            index = index + 1;
        }
    }

    internal static HttpContentCodingResult CreateInitial(byte[] content)
    {
        HttpContentCodingResult result = new HttpContentCodingResult();
        result.Outcome = HttpContentCodingOutcome.Identity;
        result.Content = CompressionCodec.CopyContent(content);
        return result;
    }

    internal static HttpContentCodingResult CreateEmptyStack(
        byte[] content,
        int maximumOutputSize)
    {
        HttpContentCodingResult result = CreateInitial(content);
        if (content.Length > maximumOutputSize)
        {
            result.Outcome = HttpContentCodingOutcome.LimitExceeded;
            result.Reason = "The unchanged content exceeds the configured output limit.";
        }

        return result;
    }

    internal static HttpContentCodingResult CreateUnknown(byte[] content, string coding)
    {
        HttpContentCodingResult result = new HttpContentCodingResult();
        result.Outcome = HttpContentCodingOutcome.Unknown;
        result.Content = CompressionCodec.CopyContent(content);
        result.RemainingCodings.Add(coding);
        result.Reason = "The content coding is not supported.";
        return result;
    }

    internal static void AddRemainingForward(
        List<string> destination,
        List<string> codings,
        int start)
    {
        int index = start;
        while (index < codings.Count)
        {
            destination.Add(codings[index]);
            index = index + 1;
        }
    }

    internal static void AddRemainingReverse(
        List<string> destination,
        List<string> codings,
        int start)
    {
        int index = start;
        while (index >= 0)
        {
            destination.Add(codings[index]);
            index = index - 1;
        }
    }

    internal static HttpContentCodingException CreateArgumentError(
        string message,
        string code,
        string coding)
    {
        return new HttpContentCodingException(message, code, coding == null ? "" : coding);
    }
}
