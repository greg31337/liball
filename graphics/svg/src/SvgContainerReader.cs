namespace svg;

/// <summary>Contains one internal bounded container-decoding outcome.</summary>
internal class SvgContainerWorkResult
{
    /// <summary>Whether decoding succeeded.</summary>
    public bool Success;
    /// <summary>The decoded XML bytes after optional decompression.</summary>
    public byte[]? Bytes;
    /// <summary>The decoded XML characters.</summary>
    public string? Text;
    /// <summary>The canonical detected encoding name.</summary>
    public string EncodingName;
    /// <summary>The stable diagnostic code after failure.</summary>
    public SvgDiagnosticCode DiagnosticCode;
    /// <summary>The failure message.</summary>
    public string Message;
    /// <summary>The decoded-character failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates an internal container outcome.</summary>
    /// <param name="success">Whether decoding succeeded.</param>
    /// <param name="bytes">The decoded XML bytes.</param>
    /// <param name="text">The decoded XML characters.</param>
    /// <param name="encodingName">The detected encoding name.</param>
    /// <param name="diagnosticCode">The failure diagnostic code.</param>
    /// <param name="message">The failure message.</param>
    /// <param name="errorOffset">The failure offset.</param>
    internal SvgContainerWorkResult(bool success, byte[]? bytes, string? text, string encodingName, SvgDiagnosticCode diagnosticCode, string message, int errorOffset)
    {
        Success = success;
        Bytes = bytes;
        Text = text;
        EncodingName = encodingName;
        DiagnosticCode = diagnosticCode;
        Message = message;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains an XML declaration encoding name and its source offset.</summary>
internal class SvgXmlEncodingDeclaration
{
    /// <summary>The declared encoding name, or an empty string.</summary>
    public string Name;
    /// <summary>The decoded-character name offset, or <c>-1</c>.</summary>
    public int Offset;

    /// <summary>Creates a declaration encoding result.</summary>
    /// <param name="name">The encoding name.</param>
    /// <param name="offset">The name offset.</param>
    internal SvgXmlEncodingDeclaration(string name, int offset)
    {
        Name = name;
        Offset = offset;
    }
}

/// <summary>Performs bounded XML and SVGZ container detection and decoding.</summary>
internal static class SvgContainerReader
{
    /// <summary>Decodes the staged source and populates detected metadata.</summary>
    /// <param name="result">The successful acquisition result.</param>
    /// <param name="options">The validated load options.</param>
    /// <returns>The same result after container decoding.</returns>
    internal static SvgLoadResult Decode(SvgLoadResult result, SvgLoadOptions options)
    {
        if ((result.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Kind == SvgLoadSourceKind.Characters)
        {
            result.DecodedCharacters = result.Source.Characters;
            result.DetectedEncoding = "utf-16";
            result.DetectedContainer = "xml";
            if (result.DetectedMediaType.Length == 0) result.DetectedMediaType = SvgMimeTypes.Svg;
            return result;
        }
        if (result.Source.Kind == SvgLoadSourceKind.XmlReader)
        {
            result.DetectedContainer = "xml-reader";
            if (result.DetectedMediaType.Length == 0) result.DetectedMediaType = SvgMimeTypes.Svg;
            return result;
        }
        byte[]? bytes = result.Source.Bytes;
        bool gzip = (bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length >= 2 && bytes[0] == 31 && bytes[1] == 139;
        SvgContainerWorkResult work;
        if (gzip)
        {
            work = RunGzip(bytes, options);
            result.DetectedContainer = "gzip";
        }
        else
        {
            work = DecodeXmlBytes(bytes, options);
            result.DetectedContainer = "xml";
        }
        if (!work.Success) return Fail(result, work.DiagnosticCode, work.Message, work.ErrorOffset);
        result.DecodedCharacters = work.Text;
        result.DetectedEncoding = work.EncodingName;
        if (result.DetectedMediaType.Length == 0 || result.DetectedMediaType == SvgMimeTypes.Gzip || result.DetectedMediaType == SvgMimeTypes.Binary) result.DetectedMediaType = SvgMimeTypes.Svg;
        return result;
    }

    static SvgContainerWorkResult RunGzip(byte[] compressed, SvgLoadOptions options)
    {
        System.Threading.Tasks.Task<SvgContainerWorkResult> task = System.Threading.Tasks.Task.Run(() => DecodeGzipUnsafe(compressed, options));
        System.IAsyncResult pending = task;
        pending.AsyncWaitHandle.WaitOne();
        if (task.IsFaulted)
        {
            System.AggregateException observed = task.Exception;
            if (observed != null) observed.GetBaseException();
            return Failure(SvgDiagnosticCode.InvalidContainer, "The gzip container is malformed or truncated.", -1);
        }
        if (task.IsCanceled) return Failure(SvgDiagnosticCode.Cancelled, "Container decoding was cancelled.", -1);
        return task.Result;
    }

    static SvgContainerWorkResult DecodeGzipUnsafe(byte[] compressed, SvgLoadOptions options)
    {
        long ratioMaximum;
        if (compressed.Length > System.Int64.MaxValue / options.Limits.Input.MaxGzipExpansionRatio) ratioMaximum = System.Int64.MaxValue;
        else ratioMaximum = compressed.Length * options.Limits.Input.MaxGzipExpansionRatio;
        long maximum = options.Limits.Input.MaxGzipOutputBytes < ratioMaximum ? options.Limits.Input.MaxGzipOutputBytes : ratioMaximum;
        if (maximum > System.Int32.MaxValue) maximum = System.Int32.MaxValue;
        using (System.IO.MemoryStream input = new System.IO.MemoryStream(compressed, false))
        using (System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress, false))
        using (System.IO.MemoryStream output = new System.IO.MemoryStream())
        {
            byte[] buffer = new byte[8192];
            while (true)
            {
                if (options.CancellationToken.IsCancellationRequested) return Failure(SvgDiagnosticCode.Cancelled, "Container decoding was cancelled.", -1);
                int read = gzip.Read(buffer, 0, buffer.Length);
                if (read == 0) break;
                if (output.Length > maximum - read) return Failure(SvgDiagnosticCode.InputLimitExceeded, "The gzip output exceeds its absolute or expansion-ratio limit.", -1);
                output.Write(buffer, 0, read);
            }
            byte[] decompressed = new byte[(int)output.Length];
            System.Array.Copy(output.GetBuffer(), 0, decompressed, 0, decompressed.Length);
            return DecodeXmlBytes(decompressed, options);
        }
    }

    static SvgContainerWorkResult DecodeXmlBytes(byte[] bytes, SvgLoadOptions options)
    {
        if (bytes.Length == 0) return Failure(SvgDiagnosticCode.InvalidContainer, "The XML container is empty.", 0);
        int bomLength = 0;
        string signatureName = "";
        System.Text.Encoding? signatureEncoding = null;
        if (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 254 && bytes[3] == 255)
        {
            bomLength = 4;
            signatureName = "utf-32be";
            signatureEncoding = new System.Text.UTF32Encoding(true, false, true);
        }
        else if (bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 254 && bytes[2] == 0 && bytes[3] == 0)
        {
            bomLength = 4;
            signatureName = "utf-32";
            signatureEncoding = new System.Text.UTF32Encoding(false, false, true);
        }
        else if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
        {
            bomLength = 3;
            signatureName = "utf-8";
            signatureEncoding = new System.Text.UTF8Encoding(false, true);
        }
        else if (bytes.Length >= 2 && bytes[0] == 254 && bytes[1] == 255)
        {
            bomLength = 2;
            signatureName = "utf-16be";
            signatureEncoding = new System.Text.UnicodeEncoding(true, false, true);
        }
        else if (bytes.Length >= 2 && bytes[0] == 255 && bytes[1] == 254)
        {
            bomLength = 2;
            signatureName = "utf-16";
            signatureEncoding = new System.Text.UnicodeEncoding(false, false, true);
        }
        else if (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 60)
        {
            signatureName = "utf-32be";
            signatureEncoding = new System.Text.UTF32Encoding(true, false, true);
        }
        else if (bytes.Length >= 4 && bytes[0] == 60 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 0)
        {
            signatureName = "utf-32";
            signatureEncoding = new System.Text.UTF32Encoding(false, false, true);
        }
        else if (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 60 && bytes[2] == 0 && bytes[3] == 63)
        {
            signatureName = "utf-16be";
            signatureEncoding = new System.Text.UnicodeEncoding(true, false, true);
        }
        else if (bytes.Length >= 4 && bytes[0] == 60 && bytes[1] == 0 && bytes[2] == 63 && bytes[3] == 0)
        {
            signatureName = "utf-16";
            signatureEncoding = new System.Text.UnicodeEncoding(false, false, true);
        }

        SvgXmlEncodingDeclaration rawDeclaration = ReadAsciiDeclaration(bytes, bomLength);
        System.Text.Encoding? encoding = signatureEncoding;
        string encodingName = signatureName;
        if (encoding == null)
        {
            if (rawDeclaration.Name.Length == 0)
            {
                encoding = new System.Text.UTF8Encoding(false, true);
                encodingName = "utf-8";
            }
            else
            {
                encoding = GetEncoding(rawDeclaration.Name);
                if (encoding == null) return Failure(SvgDiagnosticCode.UnsupportedEncoding, "The XML declaration specifies an unsupported encoding.", rawDeclaration.Offset);
                encodingName = encoding.WebName;
            }
        }
        else if (rawDeclaration.Name.Length != 0 && !EncodingMatches(encoding, rawDeclaration.Name)) return Failure(SvgDiagnosticCode.UnsupportedEncoding, "The XML declaration encoding conflicts with the byte-order mark.", rawDeclaration.Offset);

        SvgContainerWorkResult decoded = RunTextDecode(bytes, bomLength, encoding);
        if (!decoded.Success) return decoded;
        if ((decoded.Text ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length > options.Limits.Input.MaxInputCharacters) return Failure(SvgDiagnosticCode.InputLimitExceeded, "The decoded XML exceeds MaxInputCharacters.", options.Limits.Input.MaxInputCharacters > System.Int32.MaxValue ? System.Int32.MaxValue : (int)options.Limits.Input.MaxInputCharacters);
        SvgXmlEncodingDeclaration declaration = ReadTextDeclaration(decoded.Text);
        if (declaration.Name.Length != 0 && !EncodingMatches(encoding, declaration.Name)) return Failure(SvgDiagnosticCode.UnsupportedEncoding, "The XML declaration encoding conflicts with the detected byte encoding.", declaration.Offset);
        return new SvgContainerWorkResult(true, bytes, decoded.Text, encodingName, SvgDiagnosticCode.None, "", -1);
    }

    static SvgContainerWorkResult RunTextDecode(byte[] bytes, int start, System.Text.Encoding encoding)
    {
        System.Threading.Tasks.Task<string> task = System.Threading.Tasks.Task.Run(() => encoding.GetString(bytes, start, bytes.Length - start));
        System.IAsyncResult pending = task;
        pending.AsyncWaitHandle.WaitOne();
        if (task.IsFaulted)
        {
            System.AggregateException observed = task.Exception;
            if (observed != null) observed.GetBaseException();
            return Failure(SvgDiagnosticCode.UnsupportedEncoding, "The XML byte sequence is invalid for its detected encoding.", -1);
        }
        if (task.IsCanceled) return Failure(SvgDiagnosticCode.Cancelled, "Text decoding was cancelled.", -1);
        return new SvgContainerWorkResult(true, bytes, task.Result, encoding.WebName, SvgDiagnosticCode.None, "", -1);
    }

    static System.Text.Encoding? GetEncoding(string name)
    {
        System.Threading.Tasks.Task<System.Text.Encoding> task = System.Threading.Tasks.Task.Run(() => System.Text.Encoding.GetEncoding(name, System.Text.EncoderFallback.ExceptionFallback, System.Text.DecoderFallback.ExceptionFallback));
        System.IAsyncResult pending = task;
        pending.AsyncWaitHandle.WaitOne();
        if (task.IsFaulted)
        {
            System.AggregateException observed = task.Exception;
            if (observed != null) observed.GetBaseException();
            return null;
        }
        if (task.IsCanceled) return null;
        return task.Result;
    }

    static SvgXmlEncodingDeclaration ReadAsciiDeclaration(byte[] bytes, int start)
    {
        if (start + 5 > bytes.Length || bytes[start] != 60 || bytes[start + 1] != 63 || bytes[start + 2] != 120 || bytes[start + 3] != 109 || bytes[start + 4] != 108) return new SvgXmlEncodingDeclaration("", -1);
        int end = start + 5;
        int maximum = bytes.Length < start + 1024 ? bytes.Length : start + 1024;
        while (end + 1 < maximum && (bytes[end] != 63 || bytes[end + 1] != 62))
        {
            if (bytes[end] > 127) return new SvgXmlEncodingDeclaration("", -1);
            end = end + 1;
        }
        if (end + 1 >= maximum) return new SvgXmlEncodingDeclaration("", -1);
        char[] characters = new char[end + 2 - start];
        int index = 0;
        while (index < characters.Length)
        {
            characters[index] = (char)bytes[start + index];
            index = index + 1;
        }
        SvgXmlEncodingDeclaration declaration = ReadTextDeclaration(new string(characters));
        return declaration;
    }

    static SvgXmlEncodingDeclaration ReadTextDeclaration(string text)
    {
        if (text.Length < 5 || text[0] != '<' || text[1] != '?' || text[2] != 'x' || text[3] != 'm' || text[4] != 'l') return new SvgXmlEncodingDeclaration("", -1);
        int close = text.IndexOf("?>", System.StringComparison.Ordinal);
        if (close < 0 || close > 1024) return new SvgXmlEncodingDeclaration("", -1);
        int name = text.IndexOf("encoding", 5, close - 5, System.StringComparison.Ordinal);
        if (name < 0) return new SvgXmlEncodingDeclaration("", -1);
        int position = name + 8;
        while (position < close && IsXmlWhitespace(text[position])) position = position + 1;
        if (position >= close || text[position] != '=') return new SvgXmlEncodingDeclaration("", -1);
        position = position + 1;
        while (position < close && IsXmlWhitespace(text[position])) position = position + 1;
        if (position >= close || text[position] != '\'' && text[position] != '"') return new SvgXmlEncodingDeclaration("", -1);
        char quote = text[position];
        int start = position + 1;
        int end = start;
        while (end < close && text[end] != quote) end = end + 1;
        if (end >= close || end == start) return new SvgXmlEncodingDeclaration("", -1);
        return new SvgXmlEncodingDeclaration(text.Substring(start, end - start), start);
    }

    static bool EncodingMatches(System.Text.Encoding encoding, string declaredName)
    {
        System.Text.Encoding? declared = GetEncoding(declaredName);
        if (declared == null) return false;
        if (encoding.CodePage == declared.CodePage) return true;
        string lower = declaredName.ToLowerInvariant();
        if (lower == "utf-16") return encoding.CodePage == 1200 || encoding.CodePage == 1201;
        if (lower == "utf-32") return encoding.CodePage == 12000 || encoding.CodePage == 12001;
        return false;
    }

    static bool IsXmlWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    static SvgContainerWorkResult Failure(SvgDiagnosticCode code, string message, int offset)
    {
        return new SvgContainerWorkResult(false, null, null, "", code, message, offset);
    }

    static SvgLoadResult Fail(SvgLoadResult result, SvgDiagnosticCode code, string message, int offset)
    {
        SvgSourceUri sourceUri = new SvgSourceUri((result.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceUri);
        SvgSourceSpan span = offset < 0 ? SvgSourceSpan.Unknown(sourceUri) : new SvgSourceSpan(sourceUri, offset, 0, 0, offset, 0, 0);
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, span));
        result.Success = false;
        result.DecodedCharacters = null;
        result.DetectedEncoding = "";
        return result;
    }
}
