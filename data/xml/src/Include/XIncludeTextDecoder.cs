using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace xml;

internal class XIncludeTextDecoding
{
    internal string? Text;
    internal string? ResourceError;
    internal string? FatalError;

    internal XIncludeTextDecoding(string? text, string? resourceError, string? fatalError)
    {
        Text = text;
        ResourceError = resourceError;
        FatalError = fatalError;
    }
}

internal class XIncludeTextSource
{
    internal string? Characters;
    internal byte[]? Bytes;

    internal XIncludeTextSource(string? characters, byte[]? bytes)
    {
        Characters = characters;
        Bytes = bytes;
    }
}

internal class XIncludeEncodingChoice
{
    internal Encoding? Encoding;
    internal bool InterpretByteOrderMark;
    internal bool GenericUtf16;
    internal bool GenericUtf32;
    internal string? ErrorMessage;

    internal XIncludeEncodingChoice(Encoding? encoding, bool interpretByteOrderMark, bool genericUtf16, bool genericUtf32, string? errorMessage)
    {
        Encoding = encoding;
        InterpretByteOrderMark = interpretByteOrderMark;
        GenericUtf16 = genericUtf16;
        GenericUtf32 = genericUtf32;
        ErrorMessage = errorMessage;
    }
}

internal class XIncludeTextDecoder
{
    internal XmlInput Input;
    internal string? MediaType;
    internal string? RequestedEncoding;
    internal string? DefaultEncoding;
    internal XmlVersion Version;
    internal long MaximumBytes;
    internal bool NormalizeLines;

    internal XIncludeTextDecoder(XmlInput input, string? mediaType, string? requestedEncoding, string? defaultEncoding, XmlVersion version, long maximumBytes, bool normalizeLines)
    {
        Guard.NotNull(input, "input");
        Input = input;
        MediaType = mediaType;
        RequestedEncoding = requestedEncoding;
        DefaultEncoding = defaultEncoding;
        Version = version;
        MaximumBytes = maximumBytes;
        NormalizeLines = normalizeLines;
    }

    internal XIncludeTextDecoding DecodeXIncludeText()
    {
        Task<XIncludeTextSource> pending = Task.Run(() => ReadXIncludeTextSource());
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            return new XIncludeTextDecoding(null, XIncludeTaskError(pending.Exception, "The XInclude text resource could not be read."), null);
        }

        XIncludeTextSource source = pending.Result;
        string? text = source.Characters;
        if (text == null)
        {
            byte[]? bytes = source.Bytes;
            if (bytes == null)
            {
                return new XIncludeTextDecoding(null, "The XInclude text resource did not contain text or bytes.", null);
            }

            XIncludeEncodingChoice choice = SelectXIncludeTextEncoding(bytes);
            Encoding? encoding = choice.Encoding;
            if (encoding == null)
            {
                return new XIncludeTextDecoding(null, choice.ErrorMessage, null);
            }

            int offset = ApplyXIncludeByteOrderMark(bytes, choice);
            encoding = choice.Encoding;
            if (encoding == null)
            {
                return new XIncludeTextDecoding(null, choice.ErrorMessage, null);
            }

            Task<string> decoding = Task.Run(() => encoding.GetString(bytes, offset, bytes.Length - offset));
            ((IAsyncResult)decoding).AsyncWaitHandle.WaitOne();
            if (decoding.Status != TaskStatus.RanToCompletion)
            {
                return new XIncludeTextDecoding(null, null, XIncludeTaskError(decoding.Exception, "The XInclude text resource contains an invalid byte sequence."));
            }

            text = decoding.Result;
        }

        string? characterError = FindInvalidXIncludeTextCharacter(text);
        if (characterError != null)
        {
            return new XIncludeTextDecoding(null, null, characterError);
        }

        return new XIncludeTextDecoding(NormalizeLines ? NormalizeXIncludeTextLines(text) : text, null, null);
    }

    internal XIncludeTextSource ReadXIncludeTextSource()
    {
        if (Input.Kind == XmlInputKind.String)
        {
            if (Input.Text == null)
            {
                throw new InvalidDataException("A string XInclude input requires text.");
            }

            EnsureXIncludeTextResourceSize(Input.Text.Length);
            return new XIncludeTextSource(Input.Text, null);
        }

        if (Input.Kind == XmlInputKind.CharacterMemory)
        {
            EnsureXIncludeTextResourceSize(Input.Characters.Length);
            return new XIncludeTextSource(Input.Characters.ToString(), null);
        }

        if (Input.Kind == XmlInputKind.ByteMemory)
        {
            EnsureXIncludeTextResourceSize(Input.Bytes.Length);
            return new XIncludeTextSource(null, Input.Bytes.ToArray());
        }

        if (Input.Kind == XmlInputKind.TextReader)
        {
            TextReader? reader = Input.TextReader;
            if (reader == null)
            {
                throw new InvalidDataException("A text-reader XInclude input requires a reader.");
            }

            string text;
            if (Input.OwnsResource)
            {
                using (reader)
                {
                    text = ReadXIncludeTextReader(reader);
                }
            }
            else
            {
                text = ReadXIncludeTextReader(reader);
            }

            return new XIncludeTextSource(text, null);
        }

        if (Input.Kind == XmlInputKind.Stream)
        {
            Stream? stream = Input.ByteStream;
            if (stream == null)
            {
                throw new InvalidDataException("A stream XInclude input requires a byte stream.");
            }

            byte[] bytes;
            if (Input.OwnsResource)
            {
                using (stream)
                {
                    bytes = ReadXIncludeByteStream(stream);
                }
            }
            else
            {
                bytes = ReadXIncludeByteStream(stream);
            }

            return new XIncludeTextSource(null, bytes);
        }

        throw new InvalidDataException("The XInclude text resource uses an unsupported input kind.");
    }

    internal string ReadXIncludeTextReader(TextReader reader)
    {
        char[] buffer = new char[4096];
        StringBuilder result = new StringBuilder();
        while (true)
        {
            int count = reader.Read(buffer, 0, buffer.Length);
            if (count == 0)
            {
                return result.ToString();
            }

            if (count < 0 || count > buffer.Length)
            {
                throw new InvalidDataException("The XInclude text reader returned an invalid character count.");
            }

            result.Append(buffer, 0, count);
            EnsureXIncludeTextResourceSize(result.Length);
        }
    }

    internal byte[] ReadXIncludeByteStream(Stream stream)
    {
        byte[] buffer = new byte[4096];
        using (MemoryStream result = new MemoryStream())
        {
            while (true)
            {
                int count = stream.Read(buffer, 0, buffer.Length);
                if (count == 0)
                {
                    return result.ToArray();
                }

                if (count < 0 || count > buffer.Length)
                {
                    throw new InvalidDataException("The XInclude byte stream returned an invalid byte count.");
                }

                result.Write(buffer, 0, count);
                EnsureXIncludeTextResourceSize(result.Length);
            }
        }
    }

    internal void EnsureXIncludeTextResourceSize(long attempted)
    {
        if (attempted > MaximumBytes)
        {
            throw new InvalidDataException("The XInclude text resource size of " + attempted + " exceeds the configured limit of " + MaximumBytes + ".");
        }
    }

    internal XIncludeEncodingChoice SelectXIncludeTextEncoding(byte[] bytes)
    {
        if (Input.Encoding != null)
        {
            return ResolveXIncludeEncodingName(Input.Encoding.WebName);
        }

        if (Input.EncodingName != null && Input.EncodingName.Length > 0)
        {
            return ResolveXIncludeEncodingName(Input.EncodingName);
        }

        string? mediaEncoding = FindXIncludeMediaTypeEncoding(MediaType);
        if (mediaEncoding != null)
        {
            return ResolveXIncludeEncodingName(mediaEncoding);
        }

        if (IsXIncludeXmlMediaType(MediaType))
        {
            return SelectXIncludeXmlMediaEncoding(bytes);
        }

        if (RequestedEncoding != null)
        {
            return ResolveXIncludeEncodingName(RequestedEncoding);
        }

        if (DefaultEncoding != null && DefaultEncoding.Length > 0)
        {
            return ResolveXIncludeEncodingName(DefaultEncoding);
        }

        return ResolveXIncludeEncodingName("utf-8");
    }

    internal string? FindXIncludeMediaTypeEncoding(string? mediaType)
    {
        if (mediaType == null)
        {
            return null;
        }

        string[] parts = mediaType.Split(';');
        int index = 1;
        while (index < parts.Length)
        {
            string parameter = parts[index].Trim();
            int equals = parameter.IndexOf('=');
            if (equals > 0 && string.Equals(parameter.Substring(0, equals).Trim(), "charset", StringComparison.OrdinalIgnoreCase))
            {
                string value = parameter.Substring(equals + 1).Trim();
                if (value.Length >= 2 && (value[0] == '\'' && value[value.Length - 1] == '\'' || value[0] == '"' && value[value.Length - 1] == '"'))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                return value;
            }

            index = index + 1;
        }

        return null;
    }

    internal XIncludeEncodingChoice SelectXIncludeXmlMediaEncoding(byte[] bytes)
    {
        ByteOrderMarkKind byteOrderMark = EncodingDetector.DetectByteOrderMark(bytes);
        if (byteOrderMark == ByteOrderMarkKind.Utf8)
        {
            return new XIncludeEncodingChoice(new UTF8Encoding(false, true), true, false, false, null);
        }

        if (byteOrderMark == ByteOrderMarkKind.Utf16LittleEndian)
        {
            return new XIncludeEncodingChoice(new UnicodeEncoding(false, false, true), true, true, false, null);
        }

        if (byteOrderMark == ByteOrderMarkKind.Utf16BigEndian)
        {
            return new XIncludeEncodingChoice(new UnicodeEncoding(true, false, true), true, true, false, null);
        }

        if (byteOrderMark == ByteOrderMarkKind.Utf32LittleEndian)
        {
            return new XIncludeEncodingChoice(new UTF32Encoding(false, false, true), true, false, true, null);
        }

        if (byteOrderMark == ByteOrderMarkKind.Utf32BigEndian)
        {
            return new XIncludeEncodingChoice(new UTF32Encoding(true, false, true), true, false, true, null);
        }

        if (byteOrderMark != ByteOrderMarkKind.None)
        {
            return new XIncludeEncodingChoice(null, false, false, false, "The XInclude text resource uses an unsupported byte-order mark.");
        }

        InitialEncodingPatternKind pattern = EncodingDetector.DetectInitialEncodingPattern(bytes);
        if (pattern == InitialEncodingPatternKind.Utf16LittleEndian)
        {
            return new XIncludeEncodingChoice(new UnicodeEncoding(false, false, true), false, false, false, null);
        }

        if (pattern == InitialEncodingPatternKind.Utf16BigEndian)
        {
            return new XIncludeEncodingChoice(new UnicodeEncoding(true, false, true), false, false, false, null);
        }

        if (pattern == InitialEncodingPatternKind.Utf32LittleEndian)
        {
            return new XIncludeEncodingChoice(new UTF32Encoding(false, false, true), false, false, false, null);
        }

        if (pattern == InitialEncodingPatternKind.Utf32BigEndian)
        {
            return new XIncludeEncodingChoice(new UTF32Encoding(true, false, true), false, false, false, null);
        }

        if (pattern == InitialEncodingPatternKind.Utf32Unusual2143 || pattern == InitialEncodingPatternKind.Utf32Unusual3412 || pattern == InitialEncodingPatternKind.Ebcdic)
        {
            return new XIncludeEncodingChoice(null, false, false, false, "The XML media type uses an unsupported XInclude text encoding.");
        }

        string? declarationEncoding = FindXIncludeXmlDeclarationEncoding(bytes);
        if (declarationEncoding != null)
        {
            return ResolveXIncludeEncodingName(declarationEncoding);
        }

        return ResolveXIncludeEncodingName("utf-8");
    }

    internal string? FindXIncludeXmlDeclarationEncoding(byte[] bytes)
    {
        int maximum = bytes.Length < 1024 ? bytes.Length : 1024;
        StringBuilder prefix = new StringBuilder(maximum);
        int index = 0;
        while (index < maximum)
        {
            int value = bytes[index];
            if (value > 0x7F)
            {
                break;
            }

            prefix.Append((char)value);
            if (index > 0 && bytes[index - 1] == '?' && value == '>')
            {
                break;
            }

            index = index + 1;
        }

        string text = prefix.ToString();
        if (!text.StartsWith("<?xml", StringComparison.Ordinal))
        {
            return null;
        }

        int name = text.IndexOf("encoding", 5, StringComparison.Ordinal);
        if (name < 0)
        {
            return null;
        }

        int position = name + 8;
        while (position < text.Length && IsXIncludeEncodingSpace(text[position]))
        {
            position = position + 1;
        }

        if (position >= text.Length || text[position] != '=')
        {
            return null;
        }

        position = position + 1;
        while (position < text.Length && IsXIncludeEncodingSpace(text[position]))
        {
            position = position + 1;
        }

        if (position >= text.Length || text[position] != '\'' && text[position] != '"')
        {
            return null;
        }

        char quote = text[position];
        int start = position + 1;
        int end = text.IndexOf(quote, start);
        if (end < 0 || end == start)
        {
            return null;
        }

        return text.Substring(start, end - start);
    }

    internal bool IsXIncludeEncodingSpace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    internal XIncludeEncodingChoice ResolveXIncludeEncodingName(string name)
    {
        if (string.Equals(name, "utf-8", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "utf8", StringComparison.OrdinalIgnoreCase))
        {
            return new XIncludeEncodingChoice(new UTF8Encoding(false, true), true, false, false, null);
        }

        if (string.Equals(name, "utf-16", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "utf16", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "unicode", StringComparison.OrdinalIgnoreCase))
        {
            return new XIncludeEncodingChoice(new UnicodeEncoding(false, false, true), true, true, false, null);
        }

        if (string.Equals(name, "utf-16le", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "utf16le", StringComparison.OrdinalIgnoreCase))
        {
            return new XIncludeEncodingChoice(new UnicodeEncoding(false, false, true), false, false, false, null);
        }

        if (string.Equals(name, "utf-16be", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "utf16be", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "unicodefffe", StringComparison.OrdinalIgnoreCase))
        {
            return new XIncludeEncodingChoice(new UnicodeEncoding(true, false, true), false, false, false, null);
        }

        if (string.Equals(name, "utf-32", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "utf32", StringComparison.OrdinalIgnoreCase))
        {
            return new XIncludeEncodingChoice(new UTF32Encoding(false, false, true), true, false, true, null);
        }

        if (string.Equals(name, "utf-32le", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "utf32le", StringComparison.OrdinalIgnoreCase))
        {
            return new XIncludeEncodingChoice(new UTF32Encoding(false, false, true), false, false, false, null);
        }

        if (string.Equals(name, "utf-32be", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "utf32be", StringComparison.OrdinalIgnoreCase))
        {
            return new XIncludeEncodingChoice(new UTF32Encoding(true, false, true), false, false, false, null);
        }

        if (string.Equals(name, "us-ascii", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "ascii", StringComparison.OrdinalIgnoreCase))
        {
            return new XIncludeEncodingChoice(EncodingDetector.CreateStrictAsciiEncoding(), false, false, false, null);
        }

        if (string.Equals(name, "iso-8859-1", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "iso8859-1", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "latin1", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "latin-1", StringComparison.OrdinalIgnoreCase))
        {
            return new XIncludeEncodingChoice(EncodingDetector.CreateStrictLatin1Encoding(), false, false, false, null);
        }

        return new XIncludeEncodingChoice(null, false, false, false, "The XInclude text encoding is not supported: " + name + ".");
    }

    internal int ApplyXIncludeByteOrderMark(byte[] bytes, XIncludeEncodingChoice choice)
    {
        if (!choice.InterpretByteOrderMark)
        {
            return 0;
        }

        ByteOrderMarkKind byteOrderMark = EncodingDetector.DetectByteOrderMark(bytes);
        if (choice.GenericUtf16)
        {
            if (byteOrderMark == ByteOrderMarkKind.Utf16BigEndian)
            {
                choice.Encoding = new UnicodeEncoding(true, false, true);
                return 2;
            }

            if (byteOrderMark == ByteOrderMarkKind.Utf16LittleEndian)
            {
                choice.Encoding = new UnicodeEncoding(false, false, true);
                return 2;
            }

            return 0;
        }

        if (choice.GenericUtf32)
        {
            if (byteOrderMark == ByteOrderMarkKind.Utf32BigEndian)
            {
                choice.Encoding = new UTF32Encoding(true, false, true);
                return 4;
            }

            if (byteOrderMark == ByteOrderMarkKind.Utf32LittleEndian)
            {
                choice.Encoding = new UTF32Encoding(false, false, true);
                return 4;
            }

            return 0;
        }

        return byteOrderMark == ByteOrderMarkKind.Utf8 ? 3 : 0;
    }

    internal bool IsXIncludeXmlMediaType(string? mediaType)
    {
        if (mediaType == null)
        {
            return false;
        }

        int semicolon = mediaType.IndexOf(';');
        string value = semicolon < 0 ? mediaType : mediaType.Substring(0, semicolon);
        value = value.Trim();
        if (string.Equals(value, "text/xml", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "application/xml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        int slash = value.IndexOf('/');
        return slash > 0 && (value.StartsWith("text/", StringComparison.OrdinalIgnoreCase) || value.StartsWith("application/", StringComparison.OrdinalIgnoreCase)) && value.EndsWith("+xml", StringComparison.OrdinalIgnoreCase);
    }

    internal string? FindInvalidXIncludeTextCharacter(string text)
    {
        int offset = 0;
        while (offset < text.Length)
        {
            int scalar = text[offset];
            int count = 1;
            if (scalar >= 0xD800 && scalar <= 0xDBFF)
            {
                if (offset + 1 >= text.Length || text[offset + 1] < 0xDC00 || text[offset + 1] > 0xDFFF)
                {
                    return "The XInclude text resource contains an unpaired high surrogate at UTF-16 offset " + offset + ".";
                }

                scalar = 0x10000 + ((scalar - 0xD800) << 10) + (text[offset + 1] - 0xDC00);
                count = 2;
            }
            else if (scalar >= 0xDC00 && scalar <= 0xDFFF)
            {
                return "The XInclude text resource contains an unpaired low surrogate at UTF-16 offset " + offset + ".";
            }

            bool legal = Version == XmlVersion.Xml11 ? Xml11CharacterRules.IsDirectXml11Character(scalar) : Xml10CharacterRules.IsLegalXml10Character(scalar);
            if (!legal)
            {
                return "The XInclude text resource contains an illegal XML character U+" + scalar.ToString("X") + " at UTF-16 offset " + offset + ".";
            }

            offset = offset + count;
        }

        return null;
    }

    internal string NormalizeXIncludeTextLines(string text)
    {
        StringBuilder result = new StringBuilder(text.Length);
        int index = 0;
        while (index < text.Length)
        {
            int value = text[index];
            if (value == '\r')
            {
                if (index + 1 < text.Length && (text[index + 1] == '\n' || Version == XmlVersion.Xml11 && text[index + 1] == '\u0085'))
                {
                    index = index + 1;
                }

                result.Append('\n');
            }
            else if (Version == XmlVersion.Xml11 && (value == 0x85 || value == 0x2028))
            {
                result.Append('\n');
            }
            else
            {
                result.Append(text[index]);
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal string XIncludeTaskError(AggregateException? aggregate, string fallback)
    {
        if (aggregate != null && aggregate.InnerException != null && aggregate.InnerException.Message.Length > 0)
        {
            return aggregate.InnerException.Message;
        }

        return fallback;
    }
}
