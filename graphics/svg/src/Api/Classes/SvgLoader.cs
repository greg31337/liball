namespace svg;

/// <summary>Provides synchronous, bounded SVG source acquisition entry points.</summary>
public class SvgLoader : ISvgLoader
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgLoader()
    {
    }

    /// <summary>Stages an immutable character string.</summary>
    /// <param name="value">The SVG or XML characters.</param>
    /// <param name="options">The caller-owned load options.</param>
    /// <returns>A bounded staged source or diagnostics.</returns>
    public SvgLoadResult LoadWithStringAndSvgLoadOptions(string? value, SvgLoadOptions options)
    {
        SvgLoadResult? invalid = new SvgLoader().ValidateOptions(options);
        if (invalid != null) return invalid;
        if (value == null) return new SvgLoader().Failure(SvgDiagnosticCode.InvalidArgument, "The character input is required.");
        if (value.Length > options.Limits.Input.MaxInputCharacters) return new SvgLoader().Failure(SvgDiagnosticCode.InputLimitExceeded, "The character input exceeds MaxInputCharacters.");
        SvgLoadSource source = new SvgLoadSource(SvgLoadSourceKind.Characters, value, null, null, options.BaseUri, true, true);
        return new SvgLoader().Success(source, options);
    }

    /// <summary>Copies and stages a character span.</summary>
    /// <param name="value">The SVG or XML characters.</param>
    /// <param name="options">The caller-owned load options.</param>
    /// <returns>A bounded staged source or diagnostics.</returns>
    public SvgLoadResult LoadWithReadOnlySpanOfCharAndSvgLoadOptions(System.ReadOnlySpan<char> value, SvgLoadOptions options)
    {
        SvgLoadResult? invalid = new SvgLoader().ValidateOptions(options);
        if (invalid != null) return invalid;
        if (value.Length > options.Limits.Input.MaxInputCharacters) return new SvgLoader().Failure(SvgDiagnosticCode.InputLimitExceeded, "The character input exceeds MaxInputCharacters.");
        string characters = value.ToString();
        SvgLoadSource source = new SvgLoadSource(SvgLoadSourceKind.Characters, characters, null, null, options.BaseUri, true, true);
        return new SvgLoader().Success(source, options);
    }

    /// <summary>Stages a caller byte array, copying it when configured.</summary>
    /// <param name="value">The encoded SVG or SVGZ bytes.</param>
    /// <param name="options">The caller-owned load options.</param>
    /// <returns>A bounded staged source or diagnostics.</returns>
    public SvgLoadResult LoadWithByteArrayAndSvgLoadOptions(byte[]? value, SvgLoadOptions options)
    {
        SvgLoadResult? invalid = new SvgLoader().ValidateOptions(options);
        if (invalid != null) return invalid;
        if (value == null) return new SvgLoader().Failure(SvgDiagnosticCode.InvalidArgument, "The byte input is required.");
        if (value.Length > options.Limits.Input.MaxInputBytes) return new SvgLoader().Failure(SvgDiagnosticCode.InputLimitExceeded, "The byte input exceeds MaxInputBytes.");
        byte[] bytes = options.CopyInputData ? new SvgLoader().CopyBytes(value) : value;
        SvgLoadSource source = new SvgLoadSource(SvgLoadSourceKind.Bytes, null, bytes, null, options.BaseUri, true, options.CopyInputData);
        return new SvgLoader().Success(source, options);
    }

    /// <summary>Copies and stages a byte span.</summary>
    /// <param name="value">The encoded SVG or SVGZ bytes.</param>
    /// <param name="options">The caller-owned load options.</param>
    /// <returns>A bounded staged source or diagnostics.</returns>
    public SvgLoadResult LoadWithReadOnlySpanOfByteAndSvgLoadOptions(System.ReadOnlySpan<byte> value, SvgLoadOptions options)
    {
        SvgLoadResult? invalid = new SvgLoader().ValidateOptions(options);
        if (invalid != null) return invalid;
        if (value.Length > options.Limits.Input.MaxInputBytes) return new SvgLoader().Failure(SvgDiagnosticCode.InputLimitExceeded, "The byte input exceeds MaxInputBytes.");
        byte[] bytes = new byte[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            bytes[index] = value[index];
            index = index + 1;
        }
        SvgLoadSource source = new SvgLoadSource(SvgLoadSourceKind.Bytes, null, bytes, null, options.BaseUri, true, true);
        return new SvgLoader().Success(source, options);
    }

    /// <summary>Reads a caller-owned stream from its current position without closing it.</summary>
    /// <param name="stream">The readable encoded input stream.</param>
    /// <param name="options">The caller-owned load options.</param>
    /// <returns>A bounded staged source or diagnostics.</returns>
    public SvgLoadResult LoadWithStreamAndOptions(System.IO.Stream? stream, SvgLoadOptions options)
    {
        SvgLoadResult? invalid = new SvgLoader().ValidateOptions(options);
        if (invalid != null) return invalid;
        if (stream == null || !stream.CanRead) return new SvgLoader().Failure(SvgDiagnosticCode.InvalidArgument, "A readable stream is required.");
        return new SvgLoader().ReadStream(stream, options, options.BaseUri, true);
    }

    /// <summary>Reads a caller-owned text reader without closing it.</summary>
    /// <param name="reader">The readable decoded character source.</param>
    /// <param name="options">The caller-owned load options.</param>
    /// <returns>A bounded staged source or diagnostics.</returns>
    public SvgLoadResult LoadWithTextReaderAndSvgLoadOptions(System.IO.TextReader? reader, SvgLoadOptions options)
    {
        SvgLoadResult? invalid = new SvgLoader().ValidateOptions(options);
        if (invalid != null) return invalid;
        if (reader == null) return new SvgLoader().Failure(SvgDiagnosticCode.InvalidArgument, "The text reader is required.");
        return new SvgLoader().ReadTextReader(reader, options, options.BaseUri);
    }

    /// <summary>Retains a caller-owned XML reader without advancing or closing it.</summary>
    /// <param name="reader">The XML reader positioned by the caller.</param>
    /// <param name="options">The caller-owned load options.</param>
    /// <returns>A staged reader or diagnostics.</returns>
    public SvgLoadResult LoadWithXmlReaderAndSvgLoadOptions(System.Xml.XmlReader? reader, SvgLoadOptions options)
    {
        SvgLoadResult? invalid = new SvgLoader().ValidateOptions(options);
        if (invalid != null) return invalid;
        if (reader == null) return new SvgLoader().Failure(SvgDiagnosticCode.InvalidArgument, "The XML reader is required.");
        string sourceUri = reader.BaseURI;
        if (sourceUri == null || sourceUri.Length == 0) sourceUri = options.BaseUri;
        SvgLoadSource source = new SvgLoadSource(SvgLoadSourceKind.XmlReader, null, null, reader, sourceUri, true, false);
        return new SvgLoader().Success(source, options);
    }

    /// <summary>Opens and stages a filesystem path using direct file I/O.</summary>
    /// <param name="path">The filesystem path.</param>
    /// <param name="options">The caller-owned load options.</param>
    /// <returns>A bounded staged source or diagnostics.</returns>
    public SvgLoadResult LoadPath(string? path, SvgLoadOptions options)
    {
        SvgLoadResult? invalid = new SvgLoader().ValidateOptions(options);
        if (invalid != null) return invalid;
        if (path == null || path.Length == 0) return new SvgLoader().Failure(SvgDiagnosticCode.InvalidArgument, "The filesystem path is required.");
        string fullPath = System.IO.Path.GetFullPath(path);
        if (!System.IO.File.Exists(fullPath)) return new SvgLoader().Failure(SvgDiagnosticCode.ResourceFailed, "The filesystem path does not identify a readable file.");
        using (System.IO.Stream stream = System.IO.File.OpenRead(fullPath))
        {
            return new SvgLoader().ReadStream(stream, options, fullPath, false);
        }
    }

    /// <summary>Requests and stages a root URI exclusively through the configured callback.</summary>
    /// <param name="uri">The caller-resolved root URI.</param>
    /// <param name="options">The caller-owned load options containing the callback.</param>
    /// <returns>A bounded staged source or diagnostics.</returns>
    public SvgLoadResult LoadUri(string? uri, SvgLoadOptions options)
    {
        SvgLoadResult? invalid = new SvgLoader().ValidateOptions(options);
        if (invalid != null) return invalid;
        if (uri == null || uri.Length == 0) return new SvgLoader().Failure(SvgDiagnosticCode.InvalidArgument, "The root URI is required.");
        if (uri.Length > options.Limits.Resources.MaxUriCharacters) return new SvgLoader().Failure(SvgDiagnosticCode.InputLimitExceeded, "The root URI exceeds MaxUriCharacters.");
        if (!options.ResourcePolicy.AllowCallerResolvedRootUri) return new SvgLoader().Failure(SvgDiagnosticCode.ResourceDenied, "The resource policy denied the root URI.");
        SvgResourceRequest request = new SvgResourceRequest(uri, SvgMimeTypes.Svg, options.CancellationToken);
        request.ResourceKind = SvgResourceKind.RootDocument;
        SvgResourceAcquisitionOptions acquisitionOptions = new SvgResourceAcquisitionOptions();
        acquisitionOptions.ResourceProvider = options.ResourceProvider;
        acquisitionOptions.Limits = options.Limits.Resources;
        acquisitionOptions.CancellationToken = options.CancellationToken;
        acquisitionOptions.CopyInputData = options.CopyInputData;
        acquisitionOptions.MaxBytes = options.Limits.Input.MaxInputBytes;
        acquisitionOptions.DecodeDataUrls = false;
        SvgResourceAcquisitionResult acquisition = new SvgResourceResolver().Resolve(request, acquisitionOptions);
        if (!acquisition.Success)
        {
            SvgLoadResult failed = new SvgLoadResult();
            new SvgLoader().AddResponseDiagnostics(failed, acquisition.Diagnostics);
            failed.Resources.RequestCount = acquisition.RequestCount;
            return failed;
        }
        SvgLoadSource source = new SvgLoadSource(SvgLoadSourceKind.Bytes, null, acquisition.Bytes, null, acquisition.FinalUri, true, acquisition.IsIndependentSnapshot);
        SvgLoadResult result = new SvgLoader().Success(source, options);
        new SvgLoader().AddResponseDiagnostics(result, acquisition.Diagnostics);
        result.Resources.RequestCount = acquisition.RequestCount;
        result.Resources.LoadedCount = acquisition.LoadedCount;
        result.Resources.EncodedBytes = acquisition.Bytes.Length;
        result.DetectedMediaType = acquisition.EffectiveMediaType;
        SvgDocumentRecognition.ApplyDetectedMediaType(result);
        return result;
    }
SvgLoadResult ReadStream(System.IO.Stream stream, SvgLoadOptions options, string sourceUri, bool callerRetainsOwnership)
    {
        long maximum = options.Limits.Input.MaxInputBytes;
        if (maximum > System.Int32.MaxValue) maximum = System.Int32.MaxValue;
        using (System.IO.MemoryStream collected = new System.IO.MemoryStream())
        {
            byte[] buffer = new byte[8192];
            while (true)
            {
                if (options.CancellationToken.IsCancellationRequested) return new SvgLoader().Failure(SvgDiagnosticCode.Cancelled, "Loading was cancelled.");
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read == 0) break;
                if (collected.Length > maximum - read) return new SvgLoader().Failure(SvgDiagnosticCode.InputLimitExceeded, "The stream input exceeds MaxInputBytes.");
                collected.Write(buffer, 0, read);
            }
            byte[] bytes = new byte[(int)collected.Length];
            System.Array.Copy(collected.GetBuffer(), 0, bytes, 0, bytes.Length);
            SvgLoadSource source = new SvgLoadSource(SvgLoadSourceKind.Bytes, null, bytes, null, sourceUri, callerRetainsOwnership, true);
            return new SvgLoader().Success(source, options);
        }
    }
SvgLoadResult ReadTextReader(System.IO.TextReader reader, SvgLoadOptions options, string sourceUri)
    {
        long maximum = options.Limits.Input.MaxInputCharacters;
        if (maximum > System.Int32.MaxValue) maximum = System.Int32.MaxValue;
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        char[] buffer = new char[4096];
        while (true)
        {
            if (options.CancellationToken.IsCancellationRequested) return new SvgLoader().Failure(SvgDiagnosticCode.Cancelled, "Loading was cancelled.");
            int read = reader.Read(buffer, 0, buffer.Length);
            if (read == 0) break;
            if (builder.Length > maximum - read) return new SvgLoader().Failure(SvgDiagnosticCode.InputLimitExceeded, "The text reader input exceeds MaxInputCharacters.");
            builder.Append(buffer, 0, read);
        }
        SvgLoadSource source = new SvgLoadSource(SvgLoadSourceKind.Characters, builder.ToString(), null, null, sourceUri, true, true);
        return new SvgLoader().Success(source, options);
    }
SvgLoadResult? ValidateOptions(SvgLoadOptions? options)
    {
        if (options == null) return new SvgLoader().Failure(SvgDiagnosticCode.InvalidArgument, "Load options are required.");
        if (options.Mode < SvgProcessingMode.Dynamic || options.Mode > SvgProcessingMode.SecureStatic) return new SvgLoader().Failure(SvgDiagnosticCode.InvalidArgument, "The processing mode is invalid.");
        if (options.BaseUri == null || options.ContentType == null || options.Limits == null || options.ResourcePolicy == null) return new SvgLoader().Failure(SvgDiagnosticCode.InvalidArgument, "Load option references must not be null.");
        if (options.DiagnosticBehavior < SvgDiagnosticBehavior.Strict || options.DiagnosticBehavior > SvgDiagnosticBehavior.Compatible) return new SvgLoader().Failure(SvgDiagnosticCode.InvalidArgument, "The diagnostic behavior is invalid.");
        if (options.DtdPolicy < SvgDtdPolicy.Prohibit || options.DtdPolicy > SvgDtdPolicy.PreserveOnly) return new SvgLoader().Failure(SvgDiagnosticCode.InvalidArgument, "The DTD policy is invalid.");
        SvgSecurityLimitValidation limits = options.Limits.Validate();
        if (!limits.Success) return new SvgLoader().Failure(SvgDiagnosticCode.InvalidArgument, limits.Message);
        if (options.BaseUri.Length > options.Limits.Resources.MaxUriCharacters) return new SvgLoader().Failure(SvgDiagnosticCode.InputLimitExceeded, "The base URI exceeds MaxUriCharacters.");
        if (options.CancellationToken.IsCancellationRequested) return new SvgLoader().Failure(SvgDiagnosticCode.Cancelled, "Loading was cancelled.");
        return null;
    }
byte[] CopyBytes(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        System.Array.Copy(value, 0, copy, 0, value.Length);
        return copy;
    }
SvgLoadResult Success(SvgLoadSource source, SvgLoadOptions options)
    {
        SvgLoadResult result = new SvgLoadResult();
        result.Source = source;
        result.DetectedMediaType = options.ContentType;
        result.Success = true;
        result = SvgContainerReader.Decode(result, options);
        return new SvgXmlTokenizer().TokenizeLoadResult(result, options);
    }
SvgLoadResult Failure(SvgDiagnosticCode code, string message)
    {
        SvgLoadResult result = new SvgLoadResult();
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, null));
        return result;
    }
void AddResponseDiagnostics(SvgLoadResult result, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            result.Diagnostics.Add(diagnostics[index]);
            index = index + 1;
        }
    }
}
