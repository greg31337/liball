using System;
using System.IO;
using System.Text;

namespace html;

internal class HtmlDecodedLocation
{
    internal int characterOffset;
    internal int line;
    internal int column;

    internal HtmlDecodedLocation(int characterOffset, int line, int column)
    {
        this.characterOffset = characterOffset;
        this.line = line;
        this.column = column;
    }

    internal int GetCharacterOffset()
    {
        return this.characterOffset;
    }

    internal int GetLine()
    {
        return this.line;
    }

    internal int GetColumn()
    {
        return this.column;
    }
}

internal class HtmlStreamDecoder
{
    internal const int DefaultBufferBytes = 4096;
    internal const int MaximumBufferBytes = 65536;

    internal Stream stream;
    internal bool leaveOpen;
    internal byte[] buffer;
    internal HtmlBudget budget;
    internal HtmlDiagnosticCollector diagnostics;
    internal string transportLabel;
    internal string defaultLabel;
    internal Encoding? explicitEncoding;
    internal bool hasExplicitEncoding;
    internal bool loaded;
    internal bool closed;
    internal bool limitExceeded;
    internal bool encodingRestarted;
    internal int bytesRead;
    internal string initialEncodingName;
    internal HtmlEncodingDetectionResult? detection;
    internal HtmlDecodedInput? decodedInput;

    internal HtmlStreamDecoder(Stream stream, bool leaveOpen, int bufferBytes, string transportLabel,
        string defaultLabel, Encoding? explicitEncoding, bool hasExplicitEncoding,
        HtmlBudget budget, HtmlDiagnosticCollector diagnostics)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (transportLabel == null)
        {
            throw new ArgumentNullException("transportLabel");
        }

        if (defaultLabel == null)
        {
            throw new ArgumentNullException("defaultLabel");
        }

        if (hasExplicitEncoding && explicitEncoding == null)
        {
            throw new ArgumentNullException("explicitEncoding");
        }

        if (budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }

        if (bufferBytes <= 0 || bufferBytes > MaximumBufferBytes)
        {
            throw new ArgumentOutOfRangeException("bufferBytes", bufferBytes,
                "The stream buffer size must be between 1 and 65536 bytes.");
        }

        this.stream = stream;
        this.leaveOpen = leaveOpen;
        this.buffer = new byte[bufferBytes];
        this.budget = budget;
        this.diagnostics = diagnostics;
        this.transportLabel = transportLabel;
        this.defaultLabel = defaultLabel;
        this.explicitEncoding = explicitEncoding;
        this.hasExplicitEncoding = hasExplicitEncoding;
        this.loaded = false;
        this.closed = false;
        this.limitExceeded = false;
        this.encodingRestarted = false;
        this.bytesRead = 0;
        this.initialEncodingName = "";
        this.detection = null;
        this.decodedInput = null;
    }

    internal static HtmlStreamDecoder Create(Stream stream, bool leaveOpen,
        HtmlBudget budget, HtmlDiagnosticCollector diagnostics)
    {
        return new HtmlStreamDecoder(stream, leaveOpen, DefaultBufferBytes, "", "windows-1252",
            null, false, budget, diagnostics);
    }

    internal static HtmlStreamDecoder Create(Stream stream, bool leaveOpen, int bufferBytes,
        string transportLabel, string defaultLabel, HtmlBudget budget, HtmlDiagnosticCollector diagnostics)
    {
        return new HtmlStreamDecoder(stream, leaveOpen, bufferBytes, transportLabel, defaultLabel,
            null, false, budget, diagnostics);
    }

    internal static HtmlStreamDecoder CreateWithEncoding(Stream stream, Encoding? encoding, bool leaveOpen,
        HtmlBudget budget, HtmlDiagnosticCollector diagnostics)
    {
        return new HtmlStreamDecoder(stream, leaveOpen, DefaultBufferBytes, "", "windows-1252",
            encoding, true, budget, diagnostics);
    }

    internal static HtmlStreamDecoder CreateWithEncoding(Stream stream, Encoding encoding, bool leaveOpen,
        int bufferBytes, HtmlBudget budget, HtmlDiagnosticCollector diagnostics)
    {
        return new HtmlStreamDecoder(stream, leaveOpen, bufferBytes, "", "windows-1252",
            encoding, true, budget, diagnostics);
    }

    internal HtmlDecodedInput Load()
    {
        this.EnsureOpen();
        if (this.loaded)
        {
            HtmlDecodedInput? loadedInput = this.decodedInput;
            if (loadedInput == null)
            {
                throw new InvalidOperationException(
                    "A loaded stream decoder must retain decoded input.");
            }

            return loadedInput;
        }

        ByteBuffer bytes = this.ReadBytes();
        this.budget.CheckCancellation();
        this.SelectEncodingAndDecode(bytes.GetSpan());
        this.budget.CheckCancellation();
        HtmlDecodedInput? decoded = this.decodedInput;
        if (decoded == null)
        {
            throw new InvalidOperationException(
                "Selecting an encoding must produce decoded input.");
        }

        if (!this.budget.TryConsume(HtmlLimitKind.InputCharacters,
            decoded.GetCharacterCount()))
        {
            this.limitExceeded = true;
            this.AddLimitDiagnostic("The decoded input character limit was exceeded.");
        }

        this.AddDecodingDiagnostics();
        if (!decoded.IsSupported())
        {
            this.AddUnsupportedEncodingDiagnostic();
        }

        if (this.limitExceeded && this.budget.GetFirstExceededLimit() == HtmlLimitKind.InputBytes)
        {
            this.AddLimitDiagnostic("The input byte limit was exceeded.");
        }

        this.loaded = true;
        return decoded;
    }

    internal HtmlDecodedInput GetDecodedInput()
    {
        return this.Load();
    }

    internal HtmlEncodingDetectionResult GetDetection()
    {
        this.Load();
        HtmlEncodingDetectionResult? result = this.detection;
        if (result == null)
        {
            throw new InvalidOperationException(
                "A loaded stream decoder must retain encoding detection.");
        }

        return result;
    }

    internal string GetInitialEncodingName()
    {
        this.Load();
        return this.initialEncodingName;
    }

    internal bool WasEncodingRestarted()
    {
        this.Load();
        return this.encodingRestarted;
    }

    internal bool WasLimitExceeded()
    {
        return this.limitExceeded;
    }

    internal bool IsLoaded()
    {
        return this.loaded;
    }

    internal bool GetLeaveOpen()
    {
        return this.leaveOpen;
    }

    internal int GetBufferSize()
    {
        return this.buffer.Length;
    }

    internal int GetBytesRead()
    {
        return this.bytesRead;
    }

    internal void Close()
    {
        if (this.closed)
        {
            return;
        }

        this.closed = true;
        if (!this.leaveOpen)
        {
            this.stream.Dispose();
        }
    }

    internal ByteBuffer ReadBytes()
    {
        int maximum = this.budget.GetLimits().GetMaximumInputBytes();
        int initial = this.buffer.Length < maximum ? this.buffer.Length : maximum;
        ByteBuffer bytes = ByteBuffer.CreateInput(this.budget, initial);
        while (bytes.GetLength() < maximum)
        {
            this.budget.CheckCancellation();
            int remaining = maximum - bytes.GetLength();
            int requested = this.buffer.Length < remaining ? this.buffer.Length : remaining;
            int read = this.stream.Read(this.buffer, 0, requested);
            this.budget.CheckCancellation();
            if (read < 0 || read > requested)
            {
                throw new InvalidDataException("Stream returned an invalid byte count.");
            }

            if (read == 0)
            {
                this.bytesRead = bytes.GetLength();
                return bytes;
            }

            if (!bytes.TryAppendSpan(new ReadOnlySpan<byte>(this.buffer, 0, read)))
            {
                this.limitExceeded = true;
                this.bytesRead = bytes.GetLength();
                return bytes;
            }
        }

        this.budget.CheckCancellation();
        int extra = this.stream.ReadByte();
        this.budget.CheckCancellation();
        if (extra >= 0)
        {
            this.budget.TryConsume(HtmlLimitKind.InputBytes, 1);
            this.limitExceeded = true;
        }

        this.bytesRead = bytes.GetLength();
        return bytes;
    }

    internal void SelectEncodingAndDecode(ReadOnlySpan<byte> bytes)
    {
        HtmlEncodingDetectionResult sniffed = HtmlEncodingDetector.Detect(bytes, this.transportLabel, this.defaultLabel);
        if (sniffed.GetSource() == HtmlEncodingSource.Bom)
        {
            this.initialEncodingName = sniffed.GetEncodingName();
            this.detection = sniffed;
            this.decodedInput = HtmlByteDecoder.Decode(bytes, sniffed);
            return;
        }

        if (this.hasExplicitEncoding)
        {
            Encoding? explicitEncoding = this.explicitEncoding;
            if (explicitEncoding == null)
            {
                throw new InvalidOperationException(
                    "Explicit encoding mode requires an encoding.");
            }

            this.initialEncodingName = explicitEncoding.WebName;
            this.detection = new HtmlEncodingDetectionResult(explicitEncoding.WebName,
                HtmlEncodingConfidence.Certain, HtmlEncodingSource.Explicit, 0, -1);
            this.decodedInput = DecodeExplicit(bytes, explicitEncoding);
            return;
        }

        string transportName = HtmlEncodingDetector.GetEncodingName(this.transportLabel);
        if (transportName.Length > 0)
        {
            this.initialEncodingName = transportName;
        }
        else
        {
            this.initialEncodingName = HtmlEncodingDetector.GetEncodingName(this.defaultLabel);
            if (this.initialEncodingName.Length == 0)
            {
                this.initialEncodingName = "windows-1252";
            }
        }

        this.detection = sniffed;
        this.encodingRestarted = (sniffed.GetSource() == HtmlEncodingSource.Meta
            || sniffed.GetSource() == HtmlEncodingSource.XmlDeclaration)
            && sniffed.GetEncodingName() != this.initialEncodingName;
        this.decodedInput = HtmlByteDecoder.Decode(bytes, sniffed);
    }

    internal void AddDecodingDiagnostics()
    {
        HtmlDecodedInput decoded = this.GetRequiredDecodedInput();
        HtmlDecodingError[] errors = decoded.GetErrors();
        int index = 0;
        while (index < errors.Length)
        {
            HtmlDecodingError error = errors[index];
            int byteStart = error.GetByteOffset();
            int byteEnd = byteStart + error.GetByteLength();
            int characterStart = decoded.GetCharacterOffset(byteStart);
            int characterEnd = decoded.GetCharacterOffset(byteEnd);
            HtmlSourcePosition start = this.CreatePosition(characterStart, byteStart);
            HtmlSourcePosition end = this.CreatePosition(characterEnd, byteEnd);
            this.diagnostics.AddDetails(HtmlDiagnosticCode.EncodingInvalidSequence, HtmlDiagnosticSeverity.Warning,
                "A malformed byte sequence was replaced with U+FFFD.", HtmlSourceSpan.Create(start, end));
            index = index + 1;
        }
    }

    internal void AddUnsupportedEncodingDiagnostic()
    {
        HtmlSourcePosition position = HtmlSourcePosition.CreateMappedWithByteOffset(0, 0, 1, 1, 0);
        this.diagnostics.AddDetails(HtmlDiagnosticCode.EncodingUnsupportedLabel, HtmlDiagnosticSeverity.Error,
            "The selected character encoding is not supported.", HtmlSourceSpan.Create(position, position));
    }

    internal void AddLimitDiagnostic(string message)
    {
        HtmlDecodedInput decoded = this.GetRequiredDecodedInput();
        int characterOffset = decoded.GetCharacterCount();
        int byteOffset = decoded.GetByteCount();
        HtmlSourcePosition position = this.CreatePosition(characterOffset, byteOffset);
        this.diagnostics.AddDetails(HtmlDiagnosticCode.InputLimitExceeded, HtmlDiagnosticSeverity.Error,
            message, HtmlSourceSpan.Create(position, position));
    }

    internal HtmlSourcePosition CreatePosition(int characterOffset, int byteOffset)
    {
        HtmlDecodedLocation location = GetLocation(
            this.GetRequiredDecodedInput().GetString(), characterOffset);
        return HtmlSourcePosition.CreateMappedWithByteOffset(location.GetCharacterOffset(),
            location.GetCharacterOffset(), location.GetLine(), location.GetColumn(), byteOffset);
    }

    internal static HtmlDecodedInput DecodeExplicit(ReadOnlySpan<byte> input, Encoding encoding)
    {
        string webName = encoding.WebName;
        if (webName == "utf-8" || webName == "utf-16" || webName == "utf-16BE")
        {
            return HtmlByteDecoder.Decode(input, webName);
        }

        Encoding copy = (Encoding)encoding.Clone();
        copy.DecoderFallback = new DecoderReplacementFallback("\uFFFD");
        Decoder decoder = copy.GetDecoder();
        int maximumCharacters = copy.GetMaxCharCount(input.Length);
        if (maximumCharacters < 8)
        {
            maximumCharacters = 8;
        }

        char[] output = new char[maximumCharacters];
        HtmlDecodedInputBuilder builder = new HtmlDecodedInputBuilder(webName, input.Length, 0);
        int position = 0;
        int pendingStart = 0;
        while (position < input.Length)
        {
            int count = decoder.GetChars(input.Slice(position, 1), output.AsSpan(), false);
            if (count > 0)
            {
                builder.AppendCharacters(new ReadOnlySpan<char>(output, 0, count), pendingStart, position + 1);
                AddExplicitErrors(builder, input, copy, output, count, pendingStart, position + 1);
                pendingStart = position + 1;
            }

            position = position + 1;
        }

        int finalCount = decoder.GetChars(ReadOnlySpan<byte>.Empty, output.AsSpan(), true);
        if (finalCount > 0)
        {
            builder.AppendCharacters(new ReadOnlySpan<char>(output, 0, finalCount), pendingStart, input.Length);
            AddExplicitErrors(builder, input, copy, output, finalCount, pendingStart, input.Length);
        }
        else if (pendingStart < input.Length)
        {
            builder.SkipBytes(pendingStart, input.Length);
        }

        return builder.Finish();
    }

    internal HtmlDecodedInput GetRequiredDecodedInput()
    {
        HtmlDecodedInput? result = this.decodedInput;
        if (result == null)
        {
            throw new InvalidOperationException(
                "This decoder has not produced decoded input.");
        }

        return result;
    }

    internal static void AddExplicitErrors(HtmlDecodedInputBuilder builder, ReadOnlySpan<byte> input,
        Encoding encoding, char[] output, int count, int byteStart, int byteEnd)
    {
        if (count == 1 && output[0] == '\uFFFD' && IsEncodedReplacement(input.Slice(byteStart, byteEnd - byteStart), encoding))
        {
            return;
        }

        int index = 0;
        while (index < count)
        {
            if (output[index] == '\uFFFD')
            {
                builder.AddError(byteStart, byteEnd);
            }

            index = index + 1;
        }
    }

    internal static bool IsEncodedReplacement(ReadOnlySpan<byte> bytes, Encoding encoding)
    {
        byte[] encoded = encoding.GetBytes("\uFFFD");
        return bytes.SequenceEqual(encoded);
    }

    internal static HtmlDecodedLocation GetLocation(string text, int characterOffset)
    {
        int line = 1;
        int column = 1;
        int index = 0;
        while (index < characterOffset)
        {
            if (text[index] == '\n')
            {
                line = line + 1;
                column = 1;
            }
            else
            {
                column = column + 1;
            }

            index = index + 1;
        }

        return new HtmlDecodedLocation(characterOffset, line, column);
    }

    internal void EnsureOpen()
    {
        if (this.closed)
        {
            throw new ObjectDisposedException("HtmlStreamDecoder");
        }
    }
}
