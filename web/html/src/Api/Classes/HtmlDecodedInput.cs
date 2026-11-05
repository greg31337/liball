using System;

namespace html;
/// <summary>Contains decoded text, byte mappings, encoding information, and recoverable decoding errors.</summary>
public class HtmlDecodedInput : IHtmlDecodedInput
{
    internal bool supported;
    internal string encodingName;
    internal string text;
    internal int byteCount;
    internal int[] characterByteOffsets;
    internal int[] byteCharacterOffsets;
    internal HtmlDecodingError[] errors;
    internal HtmlDecodedInput(bool supported, string encodingName, string text, int byteCount, int[] characterByteOffsets, int[] byteCharacterOffsets, HtmlDecodingError[] errors)
    {
        if (encodingName == null || text == null || characterByteOffsets == null || byteCharacterOffsets == null || errors == null)
        {
            throw new ArgumentNullException("A decoded-input value cannot be null.");
        }

        if (byteCount < 0 || byteCount == int.MaxValue || characterByteOffsets.Length - 1 != text.Length || byteCharacterOffsets.Length - 1 != byteCount)
        {
            throw new ArgumentException("Decoded-input mapping lengths are inconsistent.");
        }

        this.supported = supported;
        this.encodingName = encodingName;
        this.text = text;
        this.byteCount = byteCount;
        this.characterByteOffsets = Copy(characterByteOffsets);
        this.byteCharacterOffsets = Copy(byteCharacterOffsets);
        this.errors = Copy(errors);
    }

    /// <summary>Reports whether the requested encoding is supported by the decoder.</summary>
    /// <returns><c>true</c> when the requested encoding is supported; otherwise, <c>false</c>.</returns>
    public bool IsSupported()
    {
        return this.supported;
    }

    /// <summary>Reports whether decoding produced a complete supported result.</summary>
    /// <returns><c>true</c> when the requested encoding was supported; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return this.supported;
    }

    /// <summary>Reports whether decoding retained partial text.</summary>
    /// <returns><c>false</c>; unsupported decoding returns no text and malformed sequences are replaced completely.</returns>
    public bool IsPartial()
    {
        return false;
    }

    /// <summary>Reports whether cancellation stopped decoding.</summary>
    /// <returns><c>false</c>; byte decoding has no returned cancellation state.</returns>
    public bool WasCancelled()
    {
        return false;
    }

    /// <summary>Gets the stable code explaining decoding failure.</summary>
    /// <returns><see cref = "HtmlDiagnosticCode.EncodingUnsupportedLabel"/> for an unsupported decoder label; otherwise, <see cref = "HtmlDiagnosticCode.None"/>.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        return this.supported ? HtmlDiagnosticCode.None
            : HtmlDiagnosticCode.EncodingUnsupportedLabel;
    }

    /// <summary>Gets the canonical web encoding name requested for this decoding operation.</summary>
    /// <returns>The canonical encoding name, including when <see cref = "IsSupported"/> is <c>false</c>.</returns>
    public string GetEncodingName()
    {
        return this.encodingName;
    }

    /// <summary>Gets all decoded Unicode text produced from the input bytes.</summary>
    /// <returns>The decoded string, including replacement characters for recoverable malformed sequences.</returns>
    public string GetString()
    {
        return this.text;
    }

    /// <summary>Gets the number of UTF-16 code units in the decoded string.</summary>
    /// <returns>The same length reported by the string from <see cref = "GetString"/>.</returns>
    public int GetCharacterCount()
    {
        return this.text.Length;
    }

    /// <summary>Gets the length of the original byte input.</summary>
    /// <returns>The number of bytes supplied to the decoder.</returns>
    public int GetByteCount()
    {
        return this.byteCount;
    }

    /// <summary>Maps a decoded UTF-16 offset back to the original byte input.</summary>
    /// <param name = "characterOffset">A zero-based UTF-16 offset from <c>0</c> through <see cref = "GetCharacterCount"/>.</param>
    /// <returns>The byte offset at which that decoded character or the end of input begins.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an argument is outside its supported range.</exception>
    public int GetByteOffset(int characterOffset)
    {
        if (characterOffset < 0 || characterOffset > this.text.Length)
        {
            throw new ArgumentOutOfRangeException("characterOffset", characterOffset, "The character offset is outside the decoded input.");
        }

        return this.characterByteOffsets[characterOffset];
    }

    /// <summary>Maps an original byte offset to the decoded UTF-16 text.</summary>
    /// <param name = "byteOffset">The zero-based offset in the original byte input.</param>
    /// <returns>The offset of the decoded character produced from that byte; bytes inside one multibyte sequence map to the same character offset.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an argument is outside its supported range.</exception>
    public int GetCharacterOffset(int byteOffset)
    {
        if (byteOffset < 0 || byteOffset > this.byteCount)
        {
            throw new ArgumentOutOfRangeException("byteOffset", byteOffset, "The byte offset is outside the original input.");
        }

        return this.byteCharacterOffsets[byteOffset];
    }

    /// <summary>Indicates whether decoding replaced malformed or unmappable byte sequences.</summary>
    /// <returns><c>true</c> when decoding encountered malformed byte sequences; otherwise, <c>false</c>.</returns>
    public bool HadErrors()
    {
        return this.errors.Length > 0;
    }

    /// <summary>Gets how many malformed byte sequences were replaced during decoding.</summary>
    /// <returns>The length of the array returned by <see cref = "GetErrors"/>.</returns>
    public int GetErrorCount()
    {
        return this.errors.Length;
    }

    /// <summary>Gets how many decoding errors were retained.</summary>
    /// <returns>The length of the error snapshot.</returns>
    public int GetRetainedErrorCount()
    {
        return this.errors.Length;
    }

    /// <summary>Gets how many decoding errors were dropped.</summary>
    /// <returns><c>0</c>; decoding retains every malformed-sequence record it produces.</returns>
    public long GetDroppedErrorCount()
    {
        return 0;
    }

    /// <summary>Reports whether decoding error retention was truncated.</summary>
    /// <returns><c>false</c>; decoding retains every malformed-sequence record it produces.</returns>
    public bool WereErrorsTruncated()
    {
        return false;
    }

    /// <summary>Gets the bounded malformed-sequence records produced while decoding.</summary>
    /// <returns>A snapshot of the errors; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlDecodingError[] GetErrors()
    {
        return Copy(this.errors);
    }

    internal static int[] Copy(int[] source)
    {
        int[] result = new int[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    internal static HtmlDecodingError[] Copy(HtmlDecodingError[] source)
    {
        HtmlDecodingError[] result = new HtmlDecodingError[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }
}
