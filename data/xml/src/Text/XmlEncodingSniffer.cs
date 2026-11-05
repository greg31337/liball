using System;
using System.IO;
using System.Text;

namespace xml;

internal enum XmlEncodingEntityKind
{
    Document = 0,
    ExternalParsedEntity = 1
}

internal class XmlEncodingDeclarationInfo
{
    internal bool HasDeclaration;
    internal string? EncodingName;
    internal int CharacterCount;

    internal XmlEncodingDeclarationInfo(bool hasDeclaration, string? encodingName, int characterCount)
    {
        HasDeclaration = hasDeclaration;
        EncodingName = encodingName;
        CharacterCount = characterCount;
    }
}

internal class XmlDecodedInput
{
    internal string Text;
    internal string? EffectiveEncodingName;
    internal string? DeclaredEncodingName;
    internal ByteOrderMarkKind ByteOrderMark;
    internal InitialEncodingPatternKind InitialPattern;
    internal bool HasDeclaration;
    internal int DeclarationCharacterCount;
    internal bool IsCharacterInput;
    internal long[] RawByteOffsets;

    internal XmlDecodedInput(string text, string? effectiveEncodingName, string? declaredEncodingName, ByteOrderMarkKind byteOrderMark, InitialEncodingPatternKind initialPattern, bool hasDeclaration, int declarationCharacterCount, bool isCharacterInput, long[] rawByteOffsets)
    {
        Text = text;
        EffectiveEncodingName = effectiveEncodingName;
        DeclaredEncodingName = declaredEncodingName;
        ByteOrderMark = byteOrderMark;
        InitialPattern = initialPattern;
        HasDeclaration = hasDeclaration;
        DeclarationCharacterCount = declarationCharacterCount;
        IsCharacterInput = isCharacterInput;
        RawByteOffsets = rawByteOffsets;
    }
}

internal class XmlInputDecoder : IDisposable
{
    internal XmlInput Input;
    internal XmlEncodingEntityKind EntityKind;
    internal EncodingPolicyKind EncodingPolicy;
    internal int BufferSize;
    internal IByteSource? ByteSource;
    internal XmlDecodedInput? DecodedInput;
    internal ParserWorkLimiter? WorkLimiter;
    internal bool Closed;

    internal XmlInputDecoder(XmlInput input, XmlEncodingEntityKind entityKind, EncodingPolicyKind encodingPolicy, int bufferSize)
    {
        Guard.NotNull(input, "input");
        Guard.Positive(bufferSize, "bufferSize");
        EncodingDetector.ValidateEncodingPolicy(encodingPolicy);
        if (entityKind != XmlEncodingEntityKind.Document && entityKind != XmlEncodingEntityKind.ExternalParsedEntity)
        {
            throw new ArgumentOutOfRangeException("entityKind");
        }

        Input = input;
        EntityKind = entityKind;
        EncodingPolicy = encodingPolicy;
        BufferSize = bufferSize;
        ByteSource = null;
        DecodedInput = null;
        WorkLimiter = null;
        Closed = false;
        if (input.Kind == XmlInputKind.ByteMemory || input.Kind == XmlInputKind.Stream)
        {
            ByteSource = ByteSourceFactory.CreateXmlInputByteSource(input, bufferSize);
        }
        else if (input.Kind != XmlInputKind.String && input.Kind != XmlInputKind.CharacterMemory && input.Kind != XmlInputKind.TextReader)
        {
            throw new ArgumentException("The XML input kind is not supported.", "input");
        }
    }

    internal XmlDecodedInput DecodeXmlInput()
    {
        EnsureInputDecoderOpen();
        XmlDecodedInput? existing = DecodedInput;
        if (existing != null)
        {
            return existing;
        }

        XmlDecodedInput decoded;
        if (ByteSource != null)
        {
            decoded = DecodeByteXmlInput();
        }
        else
        {
            decoded = DecodeCharacterXmlInput();
        }

        DecodedInput = decoded;
        return decoded;
    }

    internal void ConfigureXmlInputWork(ParserWorkLimiter workLimiter)
    {
        Guard.NotNull(workLimiter, "workLimiter");
        WorkLimiter = workLimiter;
    }

    internal void CloseXmlInputDecoder()
    {
        if (Closed)
        {
            return;
        }

        Closed = true;
        IByteSource? source = ByteSource;
        if (source != null)
        {
            source.CloseByteSource();
            return;
        }

        if (Input.Kind == XmlInputKind.TextReader && Input.OwnsResource)
        {
            TextReader? reader = Input.TextReader;
            if (reader != null)
            {
                reader.Dispose();
            }
        }
    }

    public void Dispose()
    {
        CloseXmlInputDecoder();
    }

    internal XmlDecodedInput DecodeCharacterXmlInput()
    {
        string text;
        if (Input.Kind == XmlInputKind.String)
        {
            string? supplied = Input.Text;
            if (supplied == null)
            {
                throw new ArgumentException("A string input requires Text.", "input");
            }

            text = supplied;
        }
        else if (Input.Kind == XmlInputKind.CharacterMemory)
        {
            text = Input.Characters.ToString();
        }
        else
        {
            TextReader? reader = Input.TextReader;
            if (reader == null)
            {
                throw new ArgumentException("A text-reader input requires TextReader.", "input");
            }

            text = ReadXmlTextReader(reader);
        }

        if (Input.Kind == XmlInputKind.String || Input.Kind == XmlInputKind.CharacterMemory)
        {
            ObserveXmlCharacterInput(text.Length);
        }

        XmlEncodingDeclarationInfo declaration = XmlEncodingSniffer.ParseEncodingDeclaration(text);
        ValidateExternalTextDeclaration(declaration);
        long[] rawByteOffsets = XmlEncodingSniffer.CreateUnknownRawByteOffsets(text.Length);
        return new XmlDecodedInput(text, null, declaration.EncodingName, ByteOrderMarkKind.None, InitialEncodingPatternKind.Unknown, declaration.HasDeclaration, declaration.CharacterCount, true, rawByteOffsets);
    }

    internal string ReadXmlTextReader(TextReader reader)
    {
        Guard.NotNull(reader, "reader");
        char[] buffer = new char[BufferSize];
        ValueBuffer collected = new ValueBuffer(BufferSize);
        while (true)
        {
            int count = reader.Read(buffer, 0, buffer.Length);
            if (count == 0)
            {
                break;
            }

            if (count < 0 || count > buffer.Length)
            {
                throw new InvalidDataException("The text reader returned an invalid character count.");
            }

            ObserveXmlCharacterInput(count);
            collected.AppendCharacters(new ReadOnlySpan<char>(buffer, 0, count));
        }

        return collected.GetText();
    }

    internal XmlDecodedInput DecodeByteXmlInput()
    {
        IByteSource? optionalSource = ByteSource;
        if (optionalSource == null)
        {
            throw new InvalidOperationException("The input does not have a byte source.");
        }

        ByteOrderMarkKind byteOrderMark = optionalSource.InspectByteOrderMark();
        XmlEncodingSniffer.EnsureSupportedByteOrderMark(byteOrderMark);
        int byteOrderMarkLength = optionalSource.SourceByteOrderMarkLength();
        ObserveXmlByteInput(byteOrderMarkLength);
        byte[] bytes = ReadRemainingInputBytes(optionalSource);
        InitialEncodingPatternKind pattern = EncodingDetector.DetectInitialEncodingPattern(bytes);
        XmlEncodingSniffer.EnsureSupportedInitialPattern(pattern);

        Encoding? externalEncoding = XmlEncodingSniffer.ResolveExternalEncoding(Input, EncodingPolicy);
        bool hasExternalEncoding = externalEncoding != null;
        Encoding selected;
        if (byteOrderMark != ByteOrderMarkKind.None)
        {
            selected = XmlEncodingSniffer.CreateByteOrderMarkEncoding(byteOrderMark);
            XmlEncodingSniffer.ValidateExternalEncodingAgainstByteOrderMark(Input, externalEncoding, selected, byteOrderMark);
        }
        else if (externalEncoding != null)
        {
            selected = externalEncoding;
            XmlEncodingSniffer.ValidateEncodingAgainstInitialPattern(selected, pattern);
            XmlEncodingSniffer.ValidateGenericUtf16Signature(Input);
        }
        else
        {
            Encoding? patternEncoding = XmlEncodingSniffer.CreateInitialPatternEncoding(pattern);
            if (patternEncoding != null)
            {
                selected = patternEncoding;
            }
            else
            {
                string prefix = XmlEncodingSniffer.DecodeAsciiDeclarationPrefix(bytes);
                XmlEncodingDeclarationInfo preliminaryDeclaration = XmlEncodingSniffer.ParseEncodingDeclaration(prefix);
                string? preliminaryName = preliminaryDeclaration.EncodingName;
                if (preliminaryName != null)
                {
                    selected = EncodingDetector.ResolveXmlEncodingName(preliminaryName, EncodingPolicy);
                    XmlEncodingSniffer.ValidateEncodingAgainstInitialPattern(selected, pattern);
                }
                else
                {
                    selected = new UTF8Encoding(false, true);
                }
            }
        }

        string text = selected.GetString(bytes);
        ObserveXmlCharacterInput(text.Length);
        XmlEncodingDeclarationInfo declaration = XmlEncodingSniffer.ParseEncodingDeclaration(text);
        ValidateExternalTextDeclaration(declaration);
        XmlEncodingSniffer.ReconcileDeclaredEncoding(declaration, selected, byteOrderMark, pattern, hasExternalEncoding, EncodingPolicy);
        long[] rawByteOffsets = XmlEncodingSniffer.CreateDecodedByteOffsets(text, selected, byteOrderMarkLength, bytes.Length);
        return new XmlDecodedInput(text, selected.WebName, declaration.EncodingName, byteOrderMark, pattern, declaration.HasDeclaration, declaration.CharacterCount, false, rawByteOffsets);
    }

    internal byte[] ReadRemainingInputBytes(IByteSource source)
    {
        byte[] buffer = new byte[BufferSize];
        ByteBuffer collected = new ByteBuffer(BufferSize);
        while (true)
        {
            int count = source.ReadSourceBytes(buffer);
            if (count == 0)
            {
                break;
            }

            ObserveXmlByteInput(count);
            collected.AppendBytes(new ReadOnlySpan<byte>(buffer, 0, count));
        }

        byte[] result = new byte[collected.Count];
        if (collected.Count > 0)
        {
            Array.Copy(collected.Bytes, 0, result, 0, collected.Count);
        }

        return result;
    }

    internal void ObserveXmlCharacterInput(int count)
    {
        ParserWorkLimiter? limiter = WorkLimiter;
        if (limiter != null)
        {
            limiter.ObserveXmlInput(count, 0);
            limiter.AdvanceXmlParserWork(count);
        }
    }

    internal void ObserveXmlByteInput(int count)
    {
        ParserWorkLimiter? limiter = WorkLimiter;
        if (limiter != null)
        {
            limiter.ObserveXmlInput(0, count);
            limiter.AdvanceXmlParserWork(count);
        }
    }

    internal void ValidateExternalTextDeclaration(XmlEncodingDeclarationInfo declaration)
    {
        if (EntityKind == XmlEncodingEntityKind.ExternalParsedEntity && declaration.HasDeclaration && declaration.EncodingName == null)
        {
            throw new FormatException("An external parsed entity text declaration must specify an encoding.");
        }
    }

    internal void EnsureInputDecoderOpen()
    {
        if (Closed)
        {
            throw new ObjectDisposedException("XmlInputDecoder");
        }
    }
}

internal static class XmlEncodingSniffer
{
    internal static XmlEncodingDeclarationInfo ParseEncodingDeclaration(string text)
    {
        Guard.NotNull(text, "text");
        if (!text.StartsWith("<?xml", StringComparison.Ordinal))
        {
            return new XmlEncodingDeclarationInfo(false, null, 0);
        }

        if (text.Length > 5 && !IsDeclarationSpace(text[5]) && text[5] != '?')
        {
            return new XmlEncodingDeclarationInfo(false, null, 0);
        }

        int declarationEnd = text.IndexOf("?>", 5, StringComparison.Ordinal);
        if (declarationEnd < 0)
        {
            throw new FormatException("The XML or text declaration is not terminated.");
        }

        int position = 5;
        string? encodingName = null;
        while (position < declarationEnd)
        {
            while (position < declarationEnd && IsDeclarationSpace(text[position]))
            {
                position = position + 1;
            }

            int nameStart = position;
            while (position < declarationEnd && IsBootstrapNameCharacter(text[position]))
            {
                position = position + 1;
            }

            if (nameStart == position)
            {
                position = position + 1;
                continue;
            }

            string attributeName = text.Substring(nameStart, position - nameStart);
            while (position < declarationEnd && IsDeclarationSpace(text[position]))
            {
                position = position + 1;
            }

            if (position >= declarationEnd || text[position] != '=')
            {
                continue;
            }

            position = position + 1;
            while (position < declarationEnd && IsDeclarationSpace(text[position]))
            {
                position = position + 1;
            }

            if (position >= declarationEnd || text[position] != '\'' && text[position] != '"')
            {
                continue;
            }

            char quote = text[position];
            position = position + 1;
            int valueStart = position;
            while (position < declarationEnd && text[position] != quote)
            {
                position = position + 1;
            }

            if (position >= declarationEnd)
            {
                break;
            }

            if (string.Equals(attributeName, "encoding", StringComparison.Ordinal) && encodingName == null)
            {
                encodingName = text.Substring(valueStart, position - valueStart);
            }

            position = position + 1;
        }

        return new XmlEncodingDeclarationInfo(true, encodingName, declarationEnd + 2);
    }

    internal static string DecodeAsciiDeclarationPrefix(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 5 || bytes[0] != 0x3C || bytes[1] != 0x3F || bytes[2] != 0x78 || bytes[3] != 0x6D || bytes[4] != 0x6C)
        {
            return string.Empty;
        }

        char[] characters = new char[bytes.Length];
        int count = 0;
        while (count < bytes.Length)
        {
            byte value = bytes[count];
            if (value > 0x7F)
            {
                break;
            }

            characters[count] = (char)value;
            count = count + 1;
            if (count >= 2 && characters[count - 2] == '?' && characters[count - 1] == '>')
            {
                break;
            }
        }

        return new string(characters, 0, count);
    }

    internal static Encoding? ResolveExternalEncoding(XmlInput input, EncodingPolicyKind policy)
    {
        Encoding? supplied = input.Encoding;
        if (supplied != null)
        {
            return EncodingDetector.CreateStrictBuiltInEncoding(supplied, policy);
        }

        string? name = input.EncodingName;
        if (name != null && name.Length > 0)
        {
            return EncodingDetector.ResolveXmlEncodingName(name, policy);
        }

        return null;
    }

    internal static Encoding CreateByteOrderMarkEncoding(ByteOrderMarkKind byteOrderMark)
    {
        if (byteOrderMark == ByteOrderMarkKind.Utf8)
        {
            return new UTF8Encoding(false, true);
        }

        if (byteOrderMark == ByteOrderMarkKind.Utf16BigEndian)
        {
            return new UnicodeEncoding(true, false, true);
        }

        if (byteOrderMark == ByteOrderMarkKind.Utf16LittleEndian)
        {
            return new UnicodeEncoding(false, false, true);
        }

        if (byteOrderMark == ByteOrderMarkKind.Utf32BigEndian)
        {
            return new UTF32Encoding(true, false, true);
        }

        if (byteOrderMark == ByteOrderMarkKind.Utf32LittleEndian)
        {
            return new UTF32Encoding(false, false, true);
        }

        throw new ArgumentException("The byte-order mark does not select a supported encoding.", "byteOrderMark");
    }

    internal static Encoding? CreateInitialPatternEncoding(InitialEncodingPatternKind pattern)
    {
        if (pattern == InitialEncodingPatternKind.Utf16BigEndian)
        {
            return new UnicodeEncoding(true, false, true);
        }

        if (pattern == InitialEncodingPatternKind.Utf16LittleEndian)
        {
            return new UnicodeEncoding(false, false, true);
        }

        if (pattern == InitialEncodingPatternKind.Utf32BigEndian)
        {
            return new UTF32Encoding(true, false, true);
        }

        if (pattern == InitialEncodingPatternKind.Utf32LittleEndian)
        {
            return new UTF32Encoding(false, false, true);
        }

        return null;
    }

    internal static void EnsureSupportedByteOrderMark(ByteOrderMarkKind byteOrderMark)
    {
        if (byteOrderMark == ByteOrderMarkKind.Utf32Unusual2143 || byteOrderMark == ByteOrderMarkKind.Utf32Unusual3412)
        {
            throw new NotSupportedException("The detected unusual UTF-32 byte order is not supported.");
        }
    }

    internal static void EnsureSupportedInitialPattern(InitialEncodingPatternKind pattern)
    {
        if (pattern == InitialEncodingPatternKind.Utf32Unusual2143 || pattern == InitialEncodingPatternKind.Utf32Unusual3412)
        {
            throw new NotSupportedException("The detected unusual UTF-32 byte order is not supported.");
        }

        if (pattern == InitialEncodingPatternKind.Ebcdic)
        {
            throw new NotSupportedException("The detected EBCDIC encoding family is not supported.");
        }
    }

    internal static void ValidateExternalEncodingAgainstByteOrderMark(XmlInput input, Encoding? externalEncoding, Encoding byteOrderMarkEncoding, ByteOrderMarkKind byteOrderMark)
    {
        if (externalEncoding == null)
        {
            return;
        }

        if (input.Encoding == null && IsGenericUtf16Name(input.EncodingName) && (byteOrderMarkEncoding.CodePage == 1200 || byteOrderMarkEncoding.CodePage == 1201))
        {
            return;
        }

        if (input.Encoding == null && IsGenericUtf32Name(input.EncodingName) && (byteOrderMarkEncoding.CodePage == 12000 || byteOrderMarkEncoding.CodePage == 12001))
        {
            return;
        }

        if (externalEncoding.CodePage != byteOrderMarkEncoding.CodePage)
        {
            throw new FormatException("The caller-selected encoding conflicts with the byte-order mark: " + byteOrderMark + ".");
        }
    }

    internal static void ValidateEncodingAgainstInitialPattern(Encoding encoding, InitialEncodingPatternKind pattern)
    {
        Encoding? detected = CreateInitialPatternEncoding(pattern);
        if (detected != null && detected.CodePage != encoding.CodePage)
        {
            throw new FormatException("The selected encoding conflicts with the initial XML byte pattern.");
        }

        if (pattern == InitialEncodingPatternKind.AsciiCompatible && (encoding.CodePage == 1200 || encoding.CodePage == 1201 || encoding.CodePage == 12000 || encoding.CodePage == 12001))
        {
            throw new FormatException("The selected Unicode width conflicts with the initial XML byte pattern.");
        }
    }

    internal static void ValidateGenericUtf16Signature(XmlInput input)
    {
        if (input.Encoding == null && IsGenericUtf16Name(input.EncodingName))
        {
            throw new FormatException("A caller-selected UTF-16 encoding requires a byte-order mark.");
        }
    }

    internal static void ReconcileDeclaredEncoding(XmlEncodingDeclarationInfo declaration, Encoding selected, ByteOrderMarkKind byteOrderMark, InitialEncodingPatternKind pattern, bool hasExternalEncoding, EncodingPolicyKind policy)
    {
        string? name = declaration.EncodingName;
        if (name == null)
        {
            if (byteOrderMark == ByteOrderMarkKind.None && !hasExternalEncoding && CreateInitialPatternEncoding(pattern) != null)
            {
                throw new FormatException("A BOM-less non-UTF-8 XML entity requires an encoding declaration.");
            }

            return;
        }

        if (byteOrderMark == ByteOrderMarkKind.None && !hasExternalEncoding && IsGenericUtf16Name(name))
        {
            throw new FormatException("A UTF-16 encoding declaration requires a byte-order mark.");
        }

        if (IsGenericUtf16Name(name))
        {
            if (selected.CodePage == 1200 || selected.CodePage == 1201)
            {
                return;
            }
        }
        else if (IsGenericUtf32Name(name))
        {
            if (selected.CodePage == 12000 || selected.CodePage == 12001)
            {
                return;
            }
        }
        else
        {
            Encoding declared = EncodingDetector.ResolveXmlEncodingName(name, policy);
            if (declared.CodePage == selected.CodePage)
            {
                return;
            }
        }

        throw new FormatException("The declared encoding conflicts with the detected or caller-selected encoding: " + name + ".");
    }

    internal static bool IsGenericUtf16Name(string? encodingName)
    {
        return string.Equals(encodingName, "utf-16", StringComparison.OrdinalIgnoreCase) || string.Equals(encodingName, "utf16", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsGenericUtf32Name(string? encodingName)
    {
        return string.Equals(encodingName, "utf-32", StringComparison.OrdinalIgnoreCase) || string.Equals(encodingName, "utf32", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsDeclarationSpace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    internal static bool IsBootstrapNameCharacter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9' || value == '.' || value == '_' || value == '-';
    }

    internal static long[] CreateUnknownRawByteOffsets(int characterCount)
    {
        Guard.NonNegativeInt(characterCount, "characterCount");
        long[] offsets = new long[characterCount + 1];
        int index = 0;
        while (index < offsets.Length)
        {
            offsets[index] = -1;
            index = index + 1;
        }

        return offsets;
    }

    internal static long[] CreateDecodedByteOffsets(string text, Encoding encoding, int byteOrderMarkLength, int encodedByteLength)
    {
        Guard.NotNull(text, "text");
        Guard.NotNull(encoding, "encoding");
        Guard.NonNegativeInt(byteOrderMarkLength, "byteOrderMarkLength");
        Guard.NonNegativeInt(encodedByteLength, "encodedByteLength");
        long[] offsets = new long[text.Length + 1];
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(text.AsMemory());
        long byteOffset = byteOrderMarkLength;
        offsets[0] = byteOffset;
        while (!scanner.IsUnicodeScalarEnd())
        {
            UnicodeScalar scalar = scanner.ReadUnicodeScalar();
            int unitIndex = 0;
            while (unitIndex < scalar.Utf16CodeUnitCount)
            {
                offsets[scalar.Utf16Offset + unitIndex] = byteOffset;
                unitIndex = unitIndex + 1;
            }

            int byteCount = encoding.GetByteCount(text.AsSpan(scalar.Utf16Offset, scalar.Utf16CodeUnitCount));
            byteOffset = byteOffset + byteCount;
            offsets[scalar.Utf16Offset + scalar.Utf16CodeUnitCount] = byteOffset;
        }

        if (byteOffset != (long)byteOrderMarkLength + encodedByteLength)
        {
            throw new InvalidOperationException("The decoded character and raw byte lengths do not agree.");
        }

        return offsets;
    }
}
