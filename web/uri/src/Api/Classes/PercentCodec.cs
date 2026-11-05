using System;

namespace uri;

/// <summary>
/// Provides bounded percent validation, encoding, decoding, and normalization operations.
/// </summary>
public class PercentCodec : IPercentCodec
{
    /// <summary>Initializes a stateless percent codec.</summary>
    public PercentCodec()
    {
    }

    /// <summary>Validates every percent triplet in text.</summary>
    /// <param name="text">The text to validate.</param>
    /// <param name="options">The processing options.</param>
    /// <returns>The first diagnostic, or <c>null</c> when all triplets are valid.</returns>
    public UriDiagnostic? ValidatePercentEncoding(string text, PercentCodecOptions options)
    {
        UriDiagnostic? optionFailure = GetCommonOptionsDiagnostic(options);
        if (optionFailure != null)
        {
            return optionFailure;
        }

        if (text == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.PercentEncoding);
        }

        UriDiagnostic? inputLimit = CheckInputLimit(text.Length, options);
        if (inputLimit != null)
        {
            return inputLimit;
        }

        return PercentScanner.ValidateFirstError(text.AsSpan(), 0, UriComponent.Input);
    }

    /// <summary>Encodes Unicode text as strict UTF-8 percent encoding.</summary>
    /// <param name="text">The Unicode text to encode.</param>
    /// <param name="options">The processing options.</param>
    /// <returns>The encoded text.</returns>
    /// <exception cref="PercentEncodingException">Encoding or a configured limit fails.</exception>
    public string EncodeText(string text, PercentCodecOptions options)
    {
        UriDiagnostic? optionFailure = GetEncodeOptionsDiagnostic(options);
        if (optionFailure != null)
        {
            throw new PercentEncodingException(optionFailure);
        }

        if (text == null)
        {
            throw new PercentEncodingException(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.PercentEncoding));
        }

        return EncodeTextCore(text.AsSpan(), options);
    }

    /// <summary>Percent-encodes arbitrary bytes without interpreting them as text.</summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="options">The processing options.</param>
    /// <returns>The encoded text.</returns>
    /// <exception cref="PercentEncodingException">Encoding or a configured limit fails.</exception>
    public string EncodeBytes(byte[] bytes, PercentCodecOptions options)
    {
        UriDiagnostic? optionFailure = GetEncodeOptionsDiagnostic(options);
        if (optionFailure != null)
        {
            throw new PercentEncodingException(optionFailure);
        }

        if (bytes == null)
        {
            throw new PercentEncodingException(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.PercentEncoding));
        }

        UriDiagnostic? inputLimit = CheckInputLimit(bytes.Length, options);
        if (inputLimit != null)
        {
            throw new PercentEncodingException(inputLimit);
        }

        PercentEncodeResult result = PercentEncoderCore.EncodeBytes(
            bytes,
            options.EncodeSet,
            options.Limits.MaxOutputCharacters);
        if (!result.Success)
        {
            throw CreateException(result.Diagnostic);
        }

        return result.Text;
    }

    /// <summary>Encodes a span of Unicode text as strict UTF-8 percent encoding.</summary>
    /// <param name="text">The Unicode text span to encode.</param>
    /// <param name="options">The processing options.</param>
    /// <returns>The encoded text.</returns>
    /// <exception cref="PercentEncodingException">Encoding or a configured limit fails.</exception>
    public string EncodeSpan(ReadOnlySpan<char> text, PercentCodecOptions options)
    {
        UriDiagnostic? optionFailure = GetEncodeOptionsDiagnostic(options);
        if (optionFailure != null)
        {
            throw new PercentEncodingException(optionFailure);
        }

        return EncodeTextCore(text, options);
    }

    /// <summary>Decodes percent-encoded data to bytes without choosing a character set.</summary>
    /// <param name="text">The percent-encoded text.</param>
    /// <param name="options">The processing options.</param>
    /// <returns>The byte-decoding outcome.</returns>
    public PercentDecodeResult DecodeBytes(string text, PercentCodecOptions options)
    {
        UriDiagnostic? optionFailure = GetCommonOptionsDiagnostic(options);
        if (optionFailure != null)
        {
            return CreateFailureResult(optionFailure);
        }

        if (text == null)
        {
            return CreateFailureResult(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.PercentEncoding));
        }

        UriDiagnostic? inputLimit = CheckInputLimit(text.Length, options);
        if (inputLimit != null)
        {
            return CreateFailureResult(inputLimit);
        }

        PercentByteDecodeResult result = PercentDecoderCore.DecodeBytes(
            text,
            options.Limits.MaxOutputCharacters,
            UriComponent.Input,
            0);
        if (!result.Success)
        {
            return CreateFailureResult(result.Diagnostic);
        }

        return new PercentDecodeResult(true, result.Bytes, "", false, null);
    }

    /// <summary>Decodes percent-encoded octets to Unicode according to the selected UTF-8 mode.</summary>
    /// <param name="text">The percent-encoded text.</param>
    /// <param name="options">The processing options.</param>
    /// <returns>The text-decoding outcome.</returns>
    public PercentDecodeResult DecodeText(string text, PercentCodecOptions options)
    {
        UriDiagnostic? optionFailure = GetDecodeOptionsDiagnostic(options);
        if (optionFailure != null)
        {
            return CreateFailureResult(optionFailure);
        }

        if (text == null)
        {
            return CreateFailureResult(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.PercentEncoding));
        }

        UriDiagnostic? inputLimit = CheckInputLimit(text.Length, options);
        if (inputLimit != null)
        {
            return CreateFailureResult(inputLimit);
        }

        PercentTextDecodeResult result;
        if (options.DecodeMode == PercentDecodeMode.StrictUtf8)
        {
            result = PercentDecoderCore.DecodeTextStrict(
                text,
                options.Limits.MaxOutputCharacters,
                UriComponent.Input,
                0);
        }
        else
        {
            result = PercentDecoderCore.DecodeTextWithReplacement(
                text,
                options.Limits.MaxOutputCharacters,
                UriComponent.Input,
                0);
        }

        if (!result.Success)
        {
            return CreateFailureResult(result.Diagnostic);
        }

        return new PercentDecodeResult(true, new byte[0], result.Text, result.UsedReplacement, null);
    }

    /// <summary>Uppercases triplet hex digits and decodes only percent-encoded unreserved characters.</summary>
    /// <param name="text">The percent-encoded text to normalize.</param>
    /// <param name="options">The processing options.</param>
    /// <returns>The normalized text.</returns>
    /// <exception cref="PercentEncodingException">Normalization or a configured limit fails.</exception>
    public string NormalizePercentEncoding(string text, PercentCodecOptions options)
    {
        UriDiagnostic? optionFailure = GetCommonOptionsDiagnostic(options);
        if (optionFailure != null)
        {
            throw new PercentEncodingException(optionFailure);
        }

        if (text == null)
        {
            throw new PercentEncodingException(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.PercentEncoding));
        }

        UriDiagnostic? inputLimit = CheckInputLimit(text.Length, options);
        if (inputLimit != null)
        {
            throw new PercentEncodingException(inputLimit);
        }

        PercentNormalizeResult result = PercentNormalizerCore.Transform(
            text.AsSpan(),
            options.Limits.MaxOutputCharacters,
            UriComponent.Input,
            0,
            true,
            true);
        if (!result.Success)
        {
            throw CreateException(result.Diagnostic);
        }

        return result.Text;
    }

    internal static string EncodeTextCore(ReadOnlySpan<char> text, PercentCodecOptions options)
    {
        UriDiagnostic? inputLimit = CheckInputLimit(text.Length, options);
        if (inputLimit != null)
        {
            throw new PercentEncodingException(inputLimit);
        }

        PercentEncodeResult result = PercentEncoderCore.EncodeSpan(
            text,
            options.EncodeSet,
            options.Limits.MaxOutputCharacters,
            UriComponent.Input,
            0);
        if (!result.Success)
        {
            throw CreateException(result.Diagnostic);
        }

        return result.Text;
    }

    internal static UriDiagnostic? GetCommonOptionsDiagnostic(PercentCodecOptions? options)
    {
        if (options == null || options.Limits == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.PercentEncoding);
        }

        return null;
    }

    internal static UriDiagnostic? GetEncodeOptionsDiagnostic(PercentCodecOptions? options)
    {
        if (options == null || options.Limits == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.PercentEncoding);
        }

        int encodeSet = (int)options.EncodeSet;
        if (encodeSet < 0 || encodeSet > 12)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.PercentEncoding);
        }

        return null;
    }

    internal static UriDiagnostic? GetDecodeOptionsDiagnostic(PercentCodecOptions? options)
    {
        if (options == null || options.Limits == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.PercentEncoding);
        }

        int decodeMode = (int)options.DecodeMode;
        if (decodeMode < 0 || decodeMode > 1)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.PercentEncoding);
        }

        return null;
    }

    internal static UriDiagnostic? CheckInputLimit(int inputLength, PercentCodecOptions options)
    {
        return LimitGuard.CheckInputCharacters(
            inputLength,
            options.Limits,
            DiagnosticDomain.PercentEncoding);
    }

    internal static PercentDecodeResult CreateFailureResult(UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.PercentEncoding);
        }
        else
        {
            failure = diagnostic;
        }

        return new PercentDecodeResult(false, new byte[0], "", false, failure);
    }

    internal static PercentEncodingException CreateException(UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.PercentEncoding);
        }
        else
        {
            failure = diagnostic;
        }

        return new PercentEncodingException(failure);
    }
}
