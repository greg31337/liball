using System;

namespace email;
/// <summary>
/// Represents an encoded word part.
/// </summary>
public class EmailEncodedWordPart : IEmailEncodedWordPartContract
{
    EmailEncodedWordPartKind kind;
    byte[] bytes;
    byte[] rawBytes;
    string? charset;
    string? language;
    bool languagePresent;
    EmailEncodedWordEncoding encoding;
    EmailSourcePosition sourcePosition;
    EmailEncodedWordPart(EmailEncodedWordPartKind kind, byte[] bytes, byte[] rawBytes, string? charset, string? language, bool languagePresent, EmailEncodedWordEncoding encoding, EmailSourcePosition sourcePosition)
    {
        this.kind = kind;
        this.bytes = new byte[bytes.Length];
        bytes.CopyTo(this.bytes, 0);
        this.rawBytes = new byte[rawBytes.Length];
        rawBytes.CopyTo(this.rawBytes, 0);
        this.charset = charset;
        this.language = language;
        this.languagePresent = languagePresent;
        this.encoding = encoding;
        this.sourcePosition = sourcePosition;
    }

    internal static EmailEncodedWordPart CreateLiteralImplementation(byte[] bytes, EmailSourcePosition? sourcePosition)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (bytes.Length == 0)
        {
            throw new ArgumentException("A literal part cannot be empty.", "bytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        return new EmailEncodedWordPart(EmailEncodedWordPartKind.Literal, bytes, bytes, null, null, false, EmailEncodedWordEncoding.None, sourcePosition);
    }

    internal static EmailEncodedWordPart CreateEncodedWordImplementation(byte[] decodedBytes, byte[] rawBytes, string charset, string? language, bool languagePresent, EmailEncodedWordEncoding encoding, EmailSourcePosition sourcePosition)
    {
        if (decodedBytes == null)
        {
            throw new ArgumentNullException("decodedBytes");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (rawBytes.Length == 0)
        {
            throw new ArgumentException("Encoded-word source bytes cannot be empty.", "rawBytes");
        }

        if (charset == null)
        {
            throw new ArgumentNullException("charset");
        }

        if (charset.Length == 0)
        {
            throw new ArgumentException("An encoded word must retain a charset.", "charset");
        }

        if (languagePresent && language == null)
        {
            throw new ArgumentNullException("language");
        }

        if (!languagePresent && language != null)
        {
            throw new ArgumentException("A language value requires the language-present flag.", "language");
        }

        if (encoding < EmailEncodedWordEncoding.Base64 || encoding > EmailEncodedWordEncoding.QuotedPrintable)
        {
            throw new ArgumentOutOfRangeException("encoding");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        return new EmailEncodedWordPart(EmailEncodedWordPartKind.EncodedWord, decodedBytes, rawBytes, charset, language, languagePresent, encoding, sourcePosition);
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailEncodedWordPartKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Determines whether the current value or state is encoded word.
    /// </summary>
    /// <returns><c>true</c> when the value or state is encoded word; otherwise, <c>false</c>.</returns>
    public bool IsEncodedWord()
    {
        return kind == EmailEncodedWordPartKind.EncodedWord;
    }

    /// <summary>
    /// Gets the bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current bytes.</returns>
    public byte[] GetBytes()
    {
        byte[] copy = new byte[bytes.Length];
        bytes.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    public byte[] GetRawBytes()
    {
        byte[] copy = new byte[rawBytes.Length];
        rawBytes.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    public long GetByteCount()
    {
        return bytes.Length;
    }

    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public long GetSourceByteCount()
    {
        return rawBytes.Length;
    }

    /// <summary>
    /// Determines whether the charset is present or available.
    /// </summary>
    /// <returns><c>true</c> when the charset is available; otherwise, <c>false</c>.</returns>
    public bool HasCharset()
    {
        return charset != null;
    }

    /// <summary>
    /// Gets the charset.
    /// </summary>
    /// <returns>The current charset.</returns>
    public string? GetCharset()
    {
        return charset;
    }

    /// <summary>
    /// Determines whether the language is present or available.
    /// </summary>
    /// <returns><c>true</c> when the language is available; otherwise, <c>false</c>.</returns>
    public bool HasLanguage()
    {
        return languagePresent;
    }

    /// <summary>
    /// Gets the language.
    /// </summary>
    /// <returns>The current language.</returns>
    public string? GetLanguage()
    {
        return language;
    }

    /// <summary>
    /// Gets the encoding.
    /// </summary>
    /// <returns>The current encoding.</returns>
    public EmailEncodedWordEncoding GetEncoding()
    {
        return encoding;
    }

    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition GetSourcePosition()
    {
        return sourcePosition;
    }
}
